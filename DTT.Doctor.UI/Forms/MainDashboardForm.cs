using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using DTT.Doctor.Presenter.ViewModels;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using Panel = System.Windows.Forms.Panel;
using Button = System.Windows.Forms.Button;

namespace DTT.Doctor.UI.Forms
{
    public partial class MainDashboardForm : Form, IQueueView
    {
        private class ClinicalNotifItem
        {
            public string PatientName { get; set; }
            public string Action { get; set; }
            public string TimeSlot { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool IsRead { get; set; }
        }

        public static MainDashboardForm Instance { get; private set; }
        private QueuePresenter _presenter;
        private readonly List<ClinicalNotifItem> _notificationList = new List<ClinicalNotifItem>();
        private AntiFlickerDataGridView _gridQueue;
        private MaterialTextBoxEdit _txtSearch;
        private Label _lblStatusMsg;
        private FlowLayoutPanel _pnlKpiContainer;
        private KpiCardControl _cardTotal, _cardWaiting, _cardInProgress, _cardCompleted;
        private Button _btnTabAll, _btnTabWaiting, _btnTabInProgress, _btnTabCompleted, _btnTabCancelled;
        private string _currentTabFilter = "Tất cả";
        private System.Windows.Forms.Timer _autoRefreshTimer;
        private Label _lblBell;
        private Label _lblBellBadge;
        private Label _lblPageTitle;
        private int _unreadDoctorNotifs = 0;
        private ReceptionCashierForm _receptionChildForm;
        private NurseWorkstationForm _nurseChildForm;
        private LabTechWorkstationForm _labTechChildForm;
        private PharmacistWorkstationForm _pharmacistChildForm;
        private int _lastSeenAdminNotificationId = 0;
        private NotifyIcon _notifyIcon;

        private bool _isBorderlessFullscreen = false;
        private FormBorderStyle _savedBorderStyle;
        private FormWindowState _savedWindowState;
        private Rectangle _savedBounds;
        private SidebarPanel _pnlSidebar;
        private SidebarPanel _pnlSidebarBottom;

        private void RefreshSidebarPaint()
        {
            try
            {
                _pnlSidebar?.Invalidate(true);
                _pnlSidebarBottom?.Invalidate(true);
                _pnlSidebar?.Update();
            }
            catch { }
        }

        // F11 — bật/tắt toàn màn hình KHÔNG viền (phủ kín cả vùng taskbar), khác Maximized thông
        // thường (Maximized vẫn giữ thanh tiêu đề + chừa taskbar). Lưu lại trạng thái cũ để khôi phục
        // đúng khi tắt, thay vì cố định về 1 kích thước mặc định.
        private void ToggleBorderlessFullscreen()
        {
            if (!_isBorderlessFullscreen)
            {
                _savedBorderStyle = FormBorderStyle;
                _savedWindowState = WindowState;
                _savedBounds = Bounds;

                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Normal;
                Bounds = Screen.FromControl(this).Bounds;
                _isBorderlessFullscreen = true;
            }
            else
            {
                FormBorderStyle = _savedBorderStyle;
                WindowState = _savedWindowState;
                if (_savedWindowState == FormWindowState.Normal) Bounds = _savedBounds;
                _isBorderlessFullscreen = false;
            }
            RefreshSidebarPaint(); // đổi viền/kích thước cửa sổ → vẽ lại sạch thanh bên
        }

        public MainDashboardForm()
        {
            Instance = this;
            _presenter = new QueuePresenter(this);
            InitializeComponent();
            // Thông báo Admin real-time (SignalR) — trước đây không có cách nào Bác sĩ nhận được
            // thông báo do Admin tạo trên Web Admin. NotificationHubService là static singleton nên
            // PHẢI unsubscribe ở FormClosed, nếu không handler cũ vẫn tồn tại sau khi form bị Dispose.
            NotificationHubService.NotificationsChanged += OnAdminNotificationsChanged;
            this.Load += async (s, e) => {
                // Ghi nhận mốc thông báo hiện có TRƯỚC khi lắng nghe real-time — nếu không, lần đầu
                // Admin phát thông báo mới trong phiên này sẽ toast lại TOÀN BỘ thông báo cũ chưa đọc
                // từ trước (vì _lastSeenAdminNotificationId mặc định = 0).
                try
                {
                    var baseline = await new ApiService().GetMyNotificationsAsync();
                    foreach (var n in baseline)
                    {
                        int id = (int)(n.notificationId ?? 0);
                        if (id > _lastSeenAdminNotificationId) _lastSeenAdminNotificationId = id;
                    }
                }
                catch { /* không chặn màn hình chính nếu lỗi tải mốc thông báo */ }

                bool isReceptionist = TokenVault.RoleId == 4 || TokenVault.RoleCode == "RECEPTIONIST" || (!string.IsNullOrEmpty(TokenVault.RoleName) && TokenVault.RoleName.Contains("ễ tân"));
                bool isNurse        = TokenVault.RoleId == 5 || TokenVault.RoleCode == "NURSE"          || (!string.IsNullOrEmpty(TokenVault.RoleName) && TokenVault.RoleName.Contains("Điều dưỡng"));
                bool isLabTech      = TokenVault.RoleId == 6 || TokenVault.RoleCode == "LAB_TECH"       || (!string.IsNullOrEmpty(TokenVault.RoleName) && TokenVault.RoleName.Contains("Kỹ thuật"));
                bool isPharmacist   = TokenVault.RoleId == 7 || TokenVault.RoleCode == "PHARMACIST"     || (!string.IsNullOrEmpty(TokenVault.RoleName) && TokenVault.RoleName.Contains("Dược sĩ"));
                try
                {
                    if (isReceptionist)
                    {
                        if (_receptionChildForm != null)
                        {
                            await _receptionChildForm.LoadDataPublicAsync();
                            _receptionChildForm.StartAutoRefresh();
                        }
                    }
                    else if (isNurse)
                    {
                        if (_nurseChildForm != null)
                        {
                            await _nurseChildForm.LoadDataAsync();
                            _nurseChildForm.StartAutoRefresh();
                        }
                    }
                    else if (isLabTech)
                    {
                        if (_labTechChildForm != null)
                        {
                            await _labTechChildForm.LoadDataAsync();
                            _labTechChildForm.StartAutoRefresh();
                        }
                    }
                    else if (isPharmacist)
                    {
                        if (_pharmacistChildForm != null)
                        {
                            await _pharmacistChildForm.LoadDataAsync();
                            _pharmacistChildForm.StartAutoRefresh();
                        }
                    }
                    else
                    {
                        await _presenter.LoadQueueAsync(false);
                        _autoRefreshTimer = new System.Windows.Forms.Timer { Interval = 1500 };
                        _autoRefreshTimer.Tick += async (ts, te) => await _presenter.LoadQueueAsync(true);
                        _autoRefreshTimer.Start();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("MainDashboardForm Load error: " + ex.Message);
                }
            };
            this.FormClosed += (s, e) => {
                // Các form con nhúng (TopLevel=false) phải được đóng/dispose tường minh — nếu không, timer
                // 1.5s của chúng vẫn chạy ngầm sau khi Đăng xuất, cộng dồn thêm mỗi lần đăng nhập lại.
                foreach (var child in new Form[] { _receptionChildForm, _nurseChildForm, _labTechChildForm, _pharmacistChildForm })
                {
                    try { child?.Dispose(); } catch { }
                }
                _autoRefreshTimer?.Stop();
                _autoRefreshTimer?.Dispose();
                NotificationHubService.NotificationsChanged -= OnAdminNotificationsChanged;
                _notifyIcon?.Dispose();
                if (Instance == this) Instance = null;
            };
        }

        // Sự kiện SignalR bắn ra trên thread nền — phải BeginInvoke để quay lại UI thread trước khi
        // đụng vào bất kỳ Control nào. Chỉ toast những thông báo CHƯA từng thấy trong phiên làm việc
        // này (dedupe theo notificationId lớn nhất đã hiện) để tránh spam lại thông báo cũ mỗi lần
        // Hub tự động reconnect.
        private void OnAdminNotificationsChanged()
        {
            if (!IsHandleCreated || IsDisposed) return;
            try
            {
                BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        var api = new ApiService();
                        var list = await api.GetMyNotificationsAsync();
                        var freshOnes = list
                            .Where(n => (int)(n.notificationId ?? 0) > _lastSeenAdminNotificationId)
                            .OrderBy(n => (int)(n.notificationId ?? 0))
                            .ToList();

                        foreach (var n in freshOnes)
                        {
                            int id = (int)(n.notificationId ?? 0);
                            string title = (string)(n.title ?? "Thông báo mới");
                            string content = (string)(n.content ?? "");
                            PushNotification("🔔 " + title.ToUpper(), "Ban Giám Đốc", content, DateTime.Now.ToString("HH:mm"), ClinicalColors.PrimaryBlue, true);
                            if (id > _lastSeenAdminNotificationId) _lastSeenAdminNotificationId = id;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("OnAdminNotificationsChanged error: " + ex.Message);
                    }
                }));
            }
            catch { /* form đang đóng giữa chừng — bỏ qua */ }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F5)
            {
                _ = _presenter?.LoadQueueAsync(true);
                ShowCornerToast("🔄 ĐỒNG BỘ NGHỆP VỤ", "Đang tải lại toàn bộ hàng chờ lâm sàng từ Server CSDL...", ClinicalColors.PrimaryBlue);
                return true;
            }
            if (keyData == Keys.F2)
            {
                _txtSearch?.Focus();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitializeComponent()
        {
            Text = $"DTT Healthcare Desktop • Bác sĩ Trực: {TokenVault.FullName} [{TokenVault.ClinicRoom}]";
            Size = new Size(1380, 840);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1100, 720);
            BackColor = ClinicalColors.GhostWhite;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);
            KeyPreview = true;

            // Mở sẵn ở trạng thái Maximized — trước đây mở ở kích thước cố định 1380x840 giữa màn
            // hình, người dùng phải tự kéo/double-click title bar để phóng to mỗi lần mở app. Toàn bộ
            // layout bên trong (sidebar Dock=Left, các form con Dock=Fill, SplitContainer trong
            // ReceptionCashierForm...) đã dùng Dock/Anchor nên tự co giãn đúng theo kích thước cửa sổ
            // thật, không cần chỉnh gì thêm để việc mở Maximized có tác dụng.
            WindowState = FormWindowState.Maximized;

            // F11: chuyển qua lại chế độ toàn màn hình KHÔNG viền (phủ kín cả vùng taskbar) — hữu ích khi
            // trình bày/demo, không phải chỉ Maximized thông thường (vẫn còn thanh tiêu đề + taskbar).
            KeyDown += (s, e) => { if (e.KeyCode == Keys.F11) ToggleBorderlessFullscreen(); };

            // ── Left Navigation Sidebar (Clean Light Theme with Border Divider) ───────
            SidebarPanel pnlSidebar = new SidebarPanel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = ClinicalColors.NavBase // teal y tế đậm thay cho nền trắng chói
            };
            _pnlSidebar = pnlSidebar;

            // Ép vẽ lại thanh bên mỗi khi cửa sổ hiện xong / đổi kích thước / được kích hoạt lại — xóa các vệt
            // điểm ảnh còn sót của nút bo góc (xem SidebarPanel).
            this.Shown += (s, e) => RefreshSidebarPaint();
            this.Activated += (s, e) => RefreshSidebarPaint();
            this.SizeChanged += (s, e) => RefreshSidebarPaint();

            Panel pnlRightBorder = new Panel
            {
                Dock = DockStyle.Right,
                Width = 1,
                BackColor = ClinicalColors.NavDeep // đường ngăn giữa menu và nội dung
            };
            pnlSidebar.Controls.Add(pnlRightBorder);

            Panel pnlLogoBox = new Panel
            {
                Size = new Size(260, 110),
                Location = new Point(0, 0),
                BackColor = ClinicalColors.NavBase
            };
            CircularLogoControl circSidebarLogo = new CircularLogoControl
            {
                Size = new Size(88, 88),
                Location = new Point(86, 12),
                ShadowSpread = 4
            };
            circSidebarLogo.LoadAppLogo();
            pnlLogoBox.Controls.Add(circSidebarLogo);

            Panel pnlUserCard = new Panel
            {
                Size = new Size(228, 72),
                Location = new Point(16, 110),
                BackColor = ClinicalColors.NavLight // thẻ người dùng: teal sáng hơn nền một bậc
            };
            AvatarBoxControl sidebarAvatar = new AvatarBoxControl(42)
            {
                Location = new Point(10, 15)
            };
            Label lblUserDoc = new Label
            {
                Text = TokenVault.GetFormattedTitleName(),
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Size = new Size(160, 24),
                Location = new Point(58, 12),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            Label lblSpec = new Label
            {
                Text = !string.IsNullOrEmpty(TokenVault.RoleName) ? TokenVault.RoleName : (TokenVault.RoleId == 4 ? "Lễ tân tiếp đón" : "Nhân viên Bệnh viện"),
                Font = ClinicalColors.GetMainFont(8.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.OnNavAccent, // chữ vai trò: xanh ngọc nhạt, nổi trên nền teal
                Size = new Size(160, 22),
                Location = new Point(58, 36),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlUserCard.Controls.Add(sidebarAvatar);
            pnlUserCard.Controls.Add(lblUserDoc);
            pnlUserCard.Controls.Add(lblSpec);

            bool isReceptionist = TokenVault.RoleId == 4 || TokenVault.RoleCode == "RECEPTIONIST" || (!string.IsNullOrEmpty(TokenVault.RoleName) && TokenVault.RoleName.Contains("ễ tân"));
            bool isNurse        = TokenVault.RoleId == 5 || TokenVault.RoleCode == "NURSE"          || (!string.IsNullOrEmpty(TokenVault.RoleName) && TokenVault.RoleName.Contains("Điều dưỡng"));
            bool isLabTech      = TokenVault.RoleId == 6 || TokenVault.RoleCode == "LAB_TECH"       || (!string.IsNullOrEmpty(TokenVault.RoleName) && TokenVault.RoleName.Contains("Kỹ thuật"));
            bool isPharmacist   = TokenVault.RoleId == 7 || TokenVault.RoleCode == "PHARMACIST"     || (!string.IsNullOrEmpty(TokenVault.RoleName) && TokenVault.RoleName.Contains("Dược sĩ"));

            int navY = 200;
            List<Button> sidebarNavButtons = new List<Button>();

            if (isReceptionist)
            {
                Button btnNavCheckIn = CreateNavButton("🌐  Tiếp Đón & Check-in", navY, true);
                Button btnNavCashier = CreateNavButton("💳  Thanh Toán", navY += 46, false);
                Button btnNavApprove = CreateNavButton("📑  Xác Thực Hồ Sơ", navY += 46, false);
                Button btnNavWalkIn  = CreateNavButton("➕  Đăng Ký Hồ Sơ", navY += 46, false);
                Button btnNavDirect  = CreateNavButton("🏥  Khám Trực Tiếp", navY += 46, false);
                Button btnNavChat    = CreateNavButton("💬  Chat Hỗ Trợ", navY += 46, false);

                btnNavCheckIn.Click += async (s, e) => { SetActiveNavButton(btnNavCheckIn, sidebarNavButtons); _receptionChildForm?.SelectTab(0); if (_receptionChildForm != null) await _receptionChildForm.LoadDataPublicAsync(); if (_lblPageTitle != null) _lblPageTitle.Text = "Tiếp Đón & Check-in"; };
                btnNavCashier.Click += async (s, e) => { SetActiveNavButton(btnNavCashier, sidebarNavButtons); _receptionChildForm?.SelectTab(1); if (_receptionChildForm != null) await _receptionChildForm.LoadDataPublicAsync(); if (_lblPageTitle != null) _lblPageTitle.Text = "Thanh Toán"; };
                btnNavApprove.Click += async (s, e) => { SetActiveNavButton(btnNavApprove, sidebarNavButtons); _receptionChildForm?.SelectTab(2); if (_receptionChildForm != null) await _receptionChildForm.LoadDataPublicAsync(); if (_lblPageTitle != null) _lblPageTitle.Text = "Xác thực hồ sơ"; };
                btnNavWalkIn.Click  += (s, e) => { SetActiveNavButton(btnNavWalkIn,  sidebarNavButtons); _receptionChildForm?.SelectTab(3); if (_lblPageTitle != null) _lblPageTitle.Text = "Đăng Ký Hồ Sơ"; };
                btnNavDirect.Click  += (s, e) => { SetActiveNavButton(btnNavDirect,  sidebarNavButtons); _receptionChildForm?.SelectTab(4); if (_lblPageTitle != null) _lblPageTitle.Text = "Khám Trực Tiếp"; };
                btnNavChat.Click    += (s, e) => { SetActiveNavButton(btnNavChat,    sidebarNavButtons); _receptionChildForm?.SelectTab(5); if (_lblPageTitle != null) _lblPageTitle.Text = "Chat Hỗ Trợ"; };

                sidebarNavButtons.Add(btnNavCheckIn);
                sidebarNavButtons.Add(btnNavCashier);
                sidebarNavButtons.Add(btnNavApprove);
                sidebarNavButtons.Add(btnNavWalkIn);
                sidebarNavButtons.Add(btnNavDirect);
                sidebarNavButtons.Add(btnNavChat);

                pnlSidebar.Controls.Add(btnNavCheckIn);
                pnlSidebar.Controls.Add(btnNavCashier);
                pnlSidebar.Controls.Add(btnNavApprove);
                pnlSidebar.Controls.Add(btnNavWalkIn);
                pnlSidebar.Controls.Add(btnNavDirect);
                pnlSidebar.Controls.Add(btnNavChat);
            }
            else if (isNurse)
            {
                Button btnNavVitals     = CreateNavButton("🩺  Đo Sinh Hiệu Bệnh Nhân", navY,       true);
                Button btnNavHistory    = CreateNavButton("📝  Lịch Sử Ca Đo Hôm Nay",  navY += 46, false);
                Button btnNavLabTests   = CreateNavButton("🔬  Danh Sách Xét Nghiệm",   navY += 46, false);
                Button btnNavUltrasound = CreateNavButton("📡  Danh Sách Siêu Âm",     navY += 46, false);

                sidebarNavButtons.Add(btnNavVitals);
                sidebarNavButtons.Add(btnNavHistory);
                sidebarNavButtons.Add(btnNavLabTests);
                sidebarNavButtons.Add(btnNavUltrasound);

                btnNavVitals.Click     += async (s, e) => { SetActiveNavButton(btnNavVitals,     sidebarNavButtons); _nurseChildForm?.SelectTab(0); if (_nurseChildForm != null) await _nurseChildForm.LoadDataAsync(); if (_lblPageTitle != null) _lblPageTitle.Text = "Đo Sinh Hiệu Bệnh Nhân"; };
                btnNavHistory.Click    += async (s, e) => { SetActiveNavButton(btnNavHistory,    sidebarNavButtons); _nurseChildForm?.SelectTab(1); if (_nurseChildForm != null) await _nurseChildForm.LoadDataAsync(); if (_lblPageTitle != null) _lblPageTitle.Text = "Lịch Sử Ca Đo Hôm Nay"; };
                btnNavLabTests.Click   += async (s, e) => { SetActiveNavButton(btnNavLabTests,   sidebarNavButtons); _nurseChildForm?.SelectTab(2); if (_nurseChildForm != null) await _nurseChildForm.LoadDataAsync(); if (_lblPageTitle != null) _lblPageTitle.Text = "Danh Sách Chỉ Định Xét Nghiệm"; };
                btnNavUltrasound.Click += async (s, e) => { SetActiveNavButton(btnNavUltrasound, sidebarNavButtons); _nurseChildForm?.SelectTab(3); if (_nurseChildForm != null) await _nurseChildForm.LoadDataAsync(); if (_lblPageTitle != null) _lblPageTitle.Text = "Danh Sách Chỉ Định Siêu Âm"; };

                pnlSidebar.Controls.Add(btnNavVitals);
                pnlSidebar.Controls.Add(btnNavHistory);
                pnlSidebar.Controls.Add(btnNavLabTests);
                pnlSidebar.Controls.Add(btnNavUltrasound);
            }
            else if (isLabTech)
            {
                Button btnNavClsWaiting = CreateNavButton("🔬  Chờ Thực Hiện",         navY,       true);
                Button btnNavClsDone    = CreateNavButton("✅  Đã Thực Hiện Hôm Nay", navY += 46, false);

                sidebarNavButtons.Add(btnNavClsWaiting);
                sidebarNavButtons.Add(btnNavClsDone);

                btnNavClsWaiting.Click += async (s, e) => { SetActiveNavButton(btnNavClsWaiting, sidebarNavButtons); _labTechChildForm?.SelectTab(0); if (_labTechChildForm != null) await _labTechChildForm.LoadDataAsync(); if (_lblPageTitle != null) _lblPageTitle.Text = "Chờ Thực Hiện CLS"; };
                btnNavClsDone.Click    += async (s, e) => { SetActiveNavButton(btnNavClsDone,    sidebarNavButtons); _labTechChildForm?.SelectTab(1); if (_labTechChildForm != null) await _labTechChildForm.LoadDataAsync(); if (_lblPageTitle != null) _lblPageTitle.Text = "Đã Thực Hiện Hôm Nay"; };

                pnlSidebar.Controls.Add(btnNavClsWaiting);
                pnlSidebar.Controls.Add(btnNavClsDone);
            }
            else if (isPharmacist)
            {
                Button btnNavPharmWaiting = CreateNavButton("💊  Chờ Phát Thuốc",         navY,       true);
                Button btnNavPharmDone    = CreateNavButton("✅  Lịch Sử Đã Cấp Phát",        navY += 46, false);
                Button btnNavPharmMeds    = CreateNavButton("💊  Danh Mục & Thuốc",       navY += 46, false);

                sidebarNavButtons.Add(btnNavPharmWaiting);
                sidebarNavButtons.Add(btnNavPharmDone);
                sidebarNavButtons.Add(btnNavPharmMeds);

                btnNavPharmWaiting.Click += async (s, e) => { SetActiveNavButton(btnNavPharmWaiting, sidebarNavButtons); _pharmacistChildForm?.SelectTab(0); if (_pharmacistChildForm != null) await _pharmacistChildForm.LoadDataAsync(); if (_lblPageTitle != null) _lblPageTitle.Text = "Chờ Phát Thuốc"; };
                btnNavPharmDone.Click    += async (s, e) => { SetActiveNavButton(btnNavPharmDone,    sidebarNavButtons); _pharmacistChildForm?.SelectTab(1); if (_pharmacistChildForm != null) await _pharmacistChildForm.LoadDataAsync(); if (_lblPageTitle != null) _lblPageTitle.Text = "Đã Phát Hôm Nay"; };
                btnNavPharmMeds.Click    += (s, e) => { SetActiveNavButton(btnNavPharmMeds, sidebarNavButtons); using (var f = new MedicineCatalogForm()) f.ShowDialog(this); };

                pnlSidebar.Controls.Add(btnNavPharmWaiting);
                pnlSidebar.Controls.Add(btnNavPharmDone);
                pnlSidebar.Controls.Add(btnNavPharmMeds);
            }
            else
            {
                Button btnNavQueue    = CreateNavButton("📋  Hàng Chờ Lâm Sàng", navY,       true);
                Button btnNavSchedule = CreateNavButton("📅  Lịch Làm Việc",       navY += 46, false);
                Button btnNavHistory  = CreateNavButton("🗂  Hồ Sơ Bệnh Án",      navY += 46, false);
                Button btnNavMeds     = CreateNavButton("💊  Danh Mục & Thuốc",    navY += 46, false);
                Button btnNavStats    = CreateNavButton("📊  Thống Kê Ca Khám",   navY += 46, false);

                sidebarNavButtons.Add(btnNavQueue);
                sidebarNavButtons.Add(btnNavSchedule);
                sidebarNavButtons.Add(btnNavHistory);
                sidebarNavButtons.Add(btnNavMeds);
                sidebarNavButtons.Add(btnNavStats);

                btnNavQueue.Click    += (s, e) => { SetActiveNavButton(btnNavQueue, sidebarNavButtons); };
                btnNavSchedule.Click += (s, e) => { SetActiveNavButton(btnNavSchedule, sidebarNavButtons); using (var f = new DoctorScheduleForm()) f.ShowDialog(this); SetActiveNavButton(btnNavQueue, sidebarNavButtons); };
                btnNavHistory.Click  += (s, e) => { SetActiveNavButton(btnNavHistory,  sidebarNavButtons); using (var f = new MedicalHistoryForm()) f.ShowDialog(this); SetActiveNavButton(btnNavQueue, sidebarNavButtons); };
                btnNavMeds.Click     += (s, e) => { SetActiveNavButton(btnNavMeds,     sidebarNavButtons); using (var f = new MedicineCatalogForm()) f.ShowDialog(this); SetActiveNavButton(btnNavQueue, sidebarNavButtons); };
                btnNavStats.Click    += (s, e) => { SetActiveNavButton(btnNavStats,    sidebarNavButtons); using (var f = new ClinicalStatsForm()) f.ShowDialog(this); SetActiveNavButton(btnNavQueue, sidebarNavButtons); };

                pnlSidebar.Controls.Add(btnNavQueue);
                pnlSidebar.Controls.Add(btnNavSchedule);
                pnlSidebar.Controls.Add(btnNavHistory);
                pnlSidebar.Controls.Add(btnNavMeds);
                pnlSidebar.Controls.Add(btnNavStats);
            }

            SidebarPanel pnlSidebarBottom = new SidebarPanel
            {
                Dock = DockStyle.Bottom,
                Height = 128,
                BackColor = ClinicalColors.NavBase
            };
            _pnlSidebarBottom = pnlSidebarBottom;
            RoundedButton btnAbout = new RoundedButton
            {
                Text = "ℹ  Thông Tin",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ClinicalColors.NavLight,
                HoverBackColor = ClinicalColors.NavActive,
                BorderRadius = 12,
                Size = new Size(228, 42),
                Location = new Point(16, 14)
            };
            btnAbout.Click += (s, e) => {
                using (var f = new AboutDialogForm()) f.ShowDialog(this);
            };
            RoundedButton btnLogout = new RoundedButton
            {
                Text = "🚪  Đăng Xuất",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 38, 38),
                HoverBackColor = Color.FromArgb(185, 28, 28),
                BorderRadius = 12,
                Size = new Size(228, 42),
                Location = new Point(16, 68)
            };
            btnLogout.Click += (s, e) => {
                TokenVault.Clear();
                this.Close();
            };
            pnlSidebarBottom.Controls.Add(btnAbout);
            pnlSidebarBottom.Controls.Add(btnLogout);

            pnlSidebar.Controls.Add(pnlLogoBox);
            pnlSidebar.Controls.Add(pnlUserCard);
            pnlSidebar.Controls.Add(pnlSidebarBottom);

            // ── Top Header Bar (Matching Homescreen.png with Avatar & Bell) ───────────────────
            int headerWidth = this.ClientSize.Width - 260;
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Width = headerWidth,
                Height = 72,
                BackColor = ClinicalColors.NavBase // cùng tông teal với menu bên trái
            };
            Panel pnlHeaderDivider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 2,
                BackColor = ClinicalColors.NavDeep
            };
            pnlHeader.Controls.Add(pnlHeaderDivider);

            _lblPageTitle = new Label
            {
                Text = isReceptionist ? "Phân hệ Lễ Tân Tiếp Đón & Thu Ngân" : isLabTech ? "Chờ Thực Hiện CLS" : isPharmacist ? "Phân Hệ Dược Sĩ & Cấp Phát Thuốc" : "Quản lý bệnh nhân",
                Font = ClinicalColors.GetMainFont(18f, FontStyle.Bold),
                ForeColor = Color.White,
                Size = new Size(500, 36),
                Location = new Point(24, 8),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            Label lblSubtitle = new Label
            {
                Text = isReceptionist ? $"📅  Hôm nay: {DateHelper.GetVietnameseDateString(DateTime.Now)}  •  Tiếp nhận, đối chiếu CCCD & thu viện phí"
                     : isLabTech ? $"📅  Hôm nay: {DateHelper.GetVietnameseDateString(DateTime.Now)}  •  Xét nghiệm & Siêu âm chỉ định từ Bác sĩ"
                     : isPharmacist ? $"📅  Hôm nay: {DateHelper.GetVietnameseDateString(DateTime.Now)}  •  Kiểm tra đơn thuốc & cấp phát thuốc theo toa"
                     : $"📅  Hôm nay: {DateHelper.GetVietnameseDateString(DateTime.Now)}  •  Xem danh sách đặt lịch & tiếp nhận bệnh nhân",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = ClinicalColors.OnNavAccent,
                Size = new Size(650, 24),
                Location = new Point(26, 42),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };

            _lblBell = new Label
            {
                Text = "🔔",
                Font = ClinicalColors.GetMainFont(15f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Size = new Size(38, 38),
                Location = new Point(headerWidth - 340, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _lblBell.Click += (s, e) => ShowNotificationPopup();
            _lblBell.MouseEnter += (s, e) => { if (_unreadDoctorNotifs == 0) _lblBell.ForeColor = ClinicalColors.OnNavAccent; };
            _lblBell.MouseLeave += (s, e) => UpdateBellBadge();

            _lblBellBadge = new Label
            {
                Text = "0",
                Font = ClinicalColors.GetMainFont(7.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Size = new Size(20, 20),
                Location = new Point(headerWidth - 322, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false,
                Cursor = Cursors.Hand
            };
            _lblBellBadge.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(Color.FromArgb(239, 68, 68)))
                {
                    pe.Graphics.FillEllipse(brush, 0, 0, _lblBellBadge.Width - 1, _lblBellBadge.Height - 1);
                }
                TextRenderer.DrawText(pe.Graphics, _lblBellBadge.Text, _lblBellBadge.Font,
                    new Rectangle(0, 0, _lblBellBadge.Width, _lblBellBadge.Height),
                    Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            _lblBellBadge.Click += (s, e) => ShowNotificationPopup();

            Label lblTopDoctor = new Label
            {
                Text = $"{TokenVault.GetFormattedTitleName()}\n{(!string.IsNullOrEmpty(TokenVault.RoleName) ? TokenVault.RoleName : (TokenVault.RoleId == 4 ? "Lễ tân tiếp đón" : "Bác sĩ khám bệnh"))}",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Size = new Size(220, 42),
                Location = new Point(headerWidth - 285, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleRight,
                UseMnemonic = false
            };

            AvatarBoxControl topAvatar = new AvatarBoxControl(44)
            {
                Location = new Point(headerWidth - 55, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            pnlHeader.Controls.Add(_lblPageTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(_lblBell);
            pnlHeader.Controls.Add(_lblBellBadge);
            _lblBellBadge.BringToFront();
            pnlHeader.Controls.Add(lblTopDoctor);
            pnlHeader.Controls.Add(topAvatar);

            // ── Main Content Container ────────────────────────────────────────
            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                BackColor = ClinicalColors.GhostWhite
            };

            // 1. KPI Cards Row
            _pnlKpiContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 115,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            _cardTotal = new KpiCardControl("Tổng Ca Hôm Nay", "0", "👥", ClinicalColors.TotalPillText, ClinicalColors.TotalPillBg) { Margin = new Padding(0, 0, 16, 0) };
            _cardWaiting = new KpiCardControl("Đang Chờ Khám", "0", "⏳", ClinicalColors.StatusWaitingText, ClinicalColors.StatusWaitingBg) { Margin = new Padding(0, 0, 16, 0) };
            _cardInProgress = new KpiCardControl("Đang Khám / Làm Bệnh", "0", "🩺", ClinicalColors.StatusInProgressText, ClinicalColors.StatusInProgressBg) { Margin = new Padding(0, 0, 16, 0) };
            _cardCompleted = new KpiCardControl("Đã Hoàn Thành", "0", "✅", ClinicalColors.StatusCompletedText, ClinicalColors.StatusCompletedBg);

            _pnlKpiContainer.Controls.Add(_cardTotal);
            _pnlKpiContainer.Controls.Add(_cardWaiting);
            _pnlKpiContainer.Controls.Add(_cardInProgress);
            _pnlKpiContainer.Controls.Add(_cardCompleted);

            // 2. Filter Bar & Search Box Row
            Panel pnlFilterBar = new Panel
            {
                Dock = DockStyle.Top,
                Width = headerWidth - 48,
                Height = 65,
                BackColor = Color.Transparent
            };

            _btnTabAll = CreateTabButton("Tất cả", 0, true);
            _btnTabWaiting = CreateTabButton("Đang chờ", 108, false);
            _btnTabInProgress = CreateTabButton("Đang khám", 216, false);
            _btnTabCompleted = CreateTabButton("Đã xong", 324, false);
            _btnTabCancelled = CreateTabButton("Hủy Lịch", 432, false);

            _btnTabAll.Click += (s, e) => SelectTabFilter("Tất cả", _btnTabAll);
            _btnTabWaiting.Click += (s, e) => SelectTabFilter("Đang chờ", _btnTabWaiting);
            _btnTabInProgress.Click += (s, e) => SelectTabFilter("Đang khám", _btnTabInProgress);
            _btnTabCompleted.Click += (s, e) => SelectTabFilter("Đã xong", _btnTabCompleted);
            _btnTabCancelled.Click += (s, e) => SelectTabFilter("Hủy Lịch", _btnTabCancelled);

            _txtSearch = new MaterialTextBoxEdit
            {
                Location = new Point(545, 10),
                Size = new Size(300, 48),
                Hint = "🔍 (F2) Tìm theo Tên, SĐT hoặc STT...",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular)
            };
            _txtSearch.TextChanged += (s, e) => _presenter.FilterAndDisplay(_txtSearch.Text, _currentTabFilter);

            _lblStatusMsg = new Label { Visible = false }; // Bỏ dòng load api, hoàn tất v.v trên homescreen

            RoundedButton btnReloadLive = new RoundedButton
            {
                Text = "Làm Mới",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129), // Vibrant emerald green
                HoverBackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                BorderRadius = 12,
                Size = new Size(130, 38),
                Location = new Point(860, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            btnReloadLive.Click += async (s, e) => { btnReloadLive.Flash(); await _presenter.LoadQueueAsync(false); };

            pnlFilterBar.Controls.Add(_btnTabAll);
            pnlFilterBar.Controls.Add(_btnTabWaiting);
            pnlFilterBar.Controls.Add(_btnTabInProgress);
            pnlFilterBar.Controls.Add(_btnTabCompleted);
            pnlFilterBar.Controls.Add(_btnTabCancelled);
            pnlFilterBar.Controls.Add(_txtSearch);
            pnlFilterBar.Controls.Add(btnReloadLive);

            // 3. Queue Table Container (Anti-flicker DataGridView)
            AntiFlickerPanel pnlTableCard = new AntiFlickerPanel
            {
                Dock = DockStyle.Fill,
                BorderRadius = 10,
                BorderColor = ClinicalColors.BorderGray,
                BackColor = Color.White,
                Padding = new Padding(12)
            };

            _gridQueue = new AntiFlickerDataGridView
            {
                Dock = DockStyle.Fill
            };
            _gridQueue.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", DataPropertyName = "QueueNumber", FillWeight = 25 });
            _gridQueue.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Họ và Tên Bệnh Nhân", DataPropertyName = "PatientName", FillWeight = 85 });
            _gridQueue.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tuổi/Giới tính", DataPropertyName = "AgeGender", FillWeight = 55 });
            _gridQueue.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Chuyên Khoa / Lý do", DataPropertyName = "SpecialtyName", FillWeight = 95 });
            _gridQueue.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Giờ hẹn", DataPropertyName = "TimeSlot", FillWeight = 45 });
            _gridQueue.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng Thái", Name = "ColStatus", DataPropertyName = "Status", FillWeight = 60 });
            _gridQueue.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thao Tác", Name = "ColAction", FillWeight = 45 });
            _gridQueue.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", Name = "ColAppointmentId", DataPropertyName = "AppointmentId", Visible = false });

            _gridQueue.CellClick += OnQueueGridCellClick;
            pnlTableCard.Controls.Add(_gridQueue);

            if (isReceptionist)
            {
                pnlMain.Controls.Clear();
                pnlMain.Padding = new Padding(0);
                _receptionChildForm = new ReceptionCashierForm();
                _receptionChildForm.TopLevel = false;
                _receptionChildForm.FormBorderStyle = FormBorderStyle.None;
                _receptionChildForm.Dock = DockStyle.Fill;
                pnlMain.Controls.Add(_receptionChildForm);
                _receptionChildForm.Show();
            }
            else if (isNurse)
            {
                pnlMain.Controls.Clear();
                pnlMain.Padding = new Padding(0);
                _nurseChildForm = new NurseWorkstationForm();
                _nurseChildForm.TopLevel = false;
                _nurseChildForm.FormBorderStyle = FormBorderStyle.None;
                _nurseChildForm.Dock = DockStyle.Fill;
                pnlMain.Controls.Add(_nurseChildForm);
                _nurseChildForm.Show();
            }
            else if (isLabTech)
            {
                pnlMain.Controls.Clear();
                pnlMain.Padding = new Padding(0);
                _labTechChildForm = new LabTechWorkstationForm();
                _labTechChildForm.TopLevel = false;
                _labTechChildForm.FormBorderStyle = FormBorderStyle.None;
                _labTechChildForm.Dock = DockStyle.Fill;
                pnlMain.Controls.Add(_labTechChildForm);
                _labTechChildForm.Show();
            }
            else if (isPharmacist)
            {
                pnlMain.Controls.Clear();
                pnlMain.Padding = new Padding(0);
                _pharmacistChildForm = new PharmacistWorkstationForm();
                _pharmacistChildForm.TopLevel = false;
                _pharmacistChildForm.FormBorderStyle = FormBorderStyle.None;
                _pharmacistChildForm.Dock = DockStyle.Fill;
                pnlMain.Controls.Add(_pharmacistChildForm);
                _pharmacistChildForm.Show();
            }
            else
            {
                pnlMain.Controls.Add(pnlTableCard);
                pnlMain.Controls.Add(pnlFilterBar);
                pnlMain.Controls.Add(_pnlKpiContainer);
            }

            Controls.Add(pnlMain);
            Controls.Add(pnlHeader);
            Controls.Add(pnlSidebar);
        }

        private void OnQueueGridCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _gridQueue.Rows.Count) return;
            var row = _gridQueue.Rows[e.RowIndex];
            string patientName = row.Cells.Count > 1 ? (row.Cells[1].Value?.ToString() ?? "Bệnh nhân") : "Bệnh nhân";
            string status = row.Cells.Count > 5 ? (row.Cells[5].Value?.ToString() ?? "Confirmed") : "Confirmed";
            int apptId = 0;
            if (row.Cells.Count > 7 && row.Cells[7].Value != null)
            {
                int.TryParse(row.Cells[7].Value.ToString(), out apptId);
            }

            if (row.Cells.Count > 6 && e.ColumnIndex == 6)
            {
                string st = row.Cells.Count > 5 ? (row.Cells[5].Value?.ToString() ?? "") : "";

                // Lock state modification for Cancelled, NoShow, or Completed
                if (st.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) || st.Equals("5") || st.Contains("Hủy") || st.Contains("hủy"))
                {
                    ShowCornerToast("🚫 TRẠNG THÁI ĐÃ KHÓA", $"Lịch khám của {patientName} đã HỦY. Trạng thái đã cố định, không thể chỉnh sửa!", Color.FromArgb(239, 68, 68));
                    return;
                }
                if (st.Equals("NoShow", StringComparison.OrdinalIgnoreCase) || st.Equals("6") || st.Equals("Expired", StringComparison.OrdinalIgnoreCase) || st.Contains("Quá hạn") || st.Contains("Bỏ khám") || st.Contains("Không đến"))
                {
                    ShowCornerToast("⏰ TRẠNG THÁI ĐÃ KHÓA", $"Ca khám của {patientName} đã ghi nhận KHÔNG ĐẾN KHÁM. Trạng thái đã cố định, không thể chỉnh sửa!", Color.FromArgb(245, 158, 11));
                    return;
                }
                // [Old code]:
                // if (st.Equals("Completed", StringComparison.OrdinalIgnoreCase) || st.Equals("4") || st.Contains("hoàn thành"))
                // [New code - hỗ trợ mở Hồ sơ bệnh án cho cả ca đang Chờ thanh toán / Chờ Dược sĩ phát thuốc]:
                if (st.Equals("Completed", StringComparison.OrdinalIgnoreCase) || st.Equals("4") || st.Contains("hoàn thành") || st.Equals("PendingDispensing", StringComparison.OrdinalIgnoreCase) || st.Contains("Chờ Dược sĩ") || st.Contains("Chờ phát thuốc") || st.Equals("PendingPayment", StringComparison.OrdinalIgnoreCase) || st.Contains("Chờ thanh toán") || st.Contains("Chờ viện phí") || st.Equals("11"))
                {
                    var targetAppt = _presenter.GetAppointmentById(apptId);
                    int targetPatientId = targetAppt?.PatientId ?? 0;
                    using (var f = new MedicalHistoryForm(patientName, apptId, targetPatientId))
                    {
                        f.ShowDialog(this);
                    }
                    return;
                }
                var dropdown = new ContextMenuStrip
                {
                    Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                    BackColor = Color.White,
                    ShowImageMargin = false,
                    Cursor = Cursors.Hand,
                    Renderer = new ModernDropdownRenderer(),
                    Padding = new Padding(6, 8, 6, 8),
                    Width = 175
                };

                Padding itemPad = new Padding(12, 8, 12, 8);
                Padding itemMarg = new Padding(2, 2, 2, 2);

                var itemExam = new ToolStripMenuItem("🩺 Khám lâm sàng")
                {
                    Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                    ForeColor = ClinicalColors.PrimaryBlue,
                    Padding = itemPad,
                    Margin = itemMarg
                };
                itemExam.Click += async (s, ev) => {
                    // Kiểm tra bệnh nhân TRƯỚC khi đổi trạng thái sang InProgress — nếu chặn sau thì ca khám
                    // bị kẹt "Đang khám" mà không mở được form. Không đoán/bịa PatientId: hồ sơ bệnh án,
                    // đơn thuốc sẽ ghi vào sai người trên dữ liệu thật.
                    var appt = _presenter.GetAppointmentById(apptId);
                    if (appt == null || appt.PatientId <= 0)
                    {
                        MessageBox.Show(
                            "Không xác định được bệnh nhân của lịch hẹn này nên không thể mở phiếu khám.\nVui lòng bấm \"Làm Mới\" để tải lại danh sách rồi thử lại.",
                            "Không thể khám lâm sàng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Server có thể TỪ CHỐI (vd ca chưa qua Điều dưỡng đo sinh hiệu, hoặc mất kết nối) — khi đó không được
                    // hiện "Đang khám" và mở phiếu khám như thể đã chuyển trạng thái thành công.
                    if (!await _presenter.UpdateStatusAsync(apptId, "InProgress"))
                    {
                        MessageBox.Show(
                            "Không thể chuyển ca này sang \"Đang khám\" — máy chủ từ chối hoặc mất kết nối.\nCa khám phải được Check-in và Điều dưỡng đo sinh hiệu trước khi Bác sĩ khám.\n\nBấm \"Làm Mới\" để tải lại danh sách rồi thử lại.",
                            "Không thể bắt đầu khám", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    row.Cells[5].Value = "InProgress";
                    _gridQueue.InvalidateRow(e.RowIndex);
                    _presenter.FilterAndDisplay(_txtSearch.Text, _currentTabFilter);

                    // Trì hoãn ShowDialog sang vòng lặp thông điệp kế tiếp bằng BeginInvoke:
                    // nếu gọi ShowDialog ngay sau await từ trong menu ngữ cảnh (ToolStripMenuItem.Click),
                    // menu chưa đóng hẳn khiến form mới không nhận WM_PAINT đầu tiên — phải Alt+Tab mới hiện nội dung.
                    BeginInvoke(new Action(async () =>
                    {
                        using (var examForm = new ExaminationForm(appt))
                        {
                            examForm.ShowDialog(this);
                            if (examForm.IsSaved)
                            {
                                await _presenter.LoadQueueAsync(false);
                            }
                        }
                    }));
                };

                var itemHistory = new ToolStripMenuItem("📋 Hồ sơ bệnh án")
                {
                    ForeColor = Color.FromArgb(71, 85, 105),
                    Padding = itemPad,
                    Margin = itemMarg
                };
                itemHistory.Click += (s, ev) => {
                    ShowCornerToast("📋 HỒ SƠ BỆNH ÁN", $"Đang mở lịch sử khám và bệnh án của {patientName}...", Color.FromArgb(71, 85, 105));
                    BeginInvoke((Action)(() => {
                        var targetAppt = _presenter.GetAppointmentById(apptId);
                        int targetPatientId = targetAppt?.PatientId ?? 0;
                        using (var historyForm = new MedicalHistoryForm(patientName, apptId, targetPatientId))
                        {
                            historyForm.ShowDialog(this);
                        }
                    }));
                };

                var itemStatus = new ToolStripMenuItem("✔ Đã hoàn thành")
                {
                    ForeColor = Color.FromArgb(16, 185, 129),
                    Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                    Padding = itemPad,
                    Margin = itemMarg
                };
                itemStatus.Click += async (s, ev) => {
                    // Cảnh báo nếu ca này còn chỉ định Xét nghiệm/Siêu âm CHƯA có kết quả — tránh đánh dấu
                    // hoàn thành ngay từ hàng chờ (đường tắt này trước đây bỏ qua hẳn cảnh báo trong ExaminationForm).
                    var api = new ApiService();
                    var pendingOrders = await api.GetClinicalOrderQueueAsync(done: false);
                    int stillPending = pendingOrders.Count(o => o.AppointmentId == apptId);
                    if (stillPending > 0)
                    {
                        var confirm = MessageBox.Show(
                            $"Ca khám của {patientName} còn {stillPending} chỉ định Xét nghiệm/Siêu âm CHƯA có kết quả.\n\nBạn có chắc muốn đánh dấu hoàn thành ngay bây giờ không?",
                            "Còn chỉ định CLS chưa hoàn tất", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (confirm != DialogResult.Yes) return;
                    }

                    if (!await _presenter.UpdateStatusAsync(apptId, "Completed"))
                    {
                        MessageBox.Show(
                            "Không thể đánh dấu hoàn thành — máy chủ từ chối hoặc mất kết nối.\nBấm \"Làm Mới\" để tải lại danh sách rồi thử lại.",
                            "Không thể hoàn thành ca khám", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    row.Cells[5].Value = "Completed";
                    _gridQueue.InvalidateRow(e.RowIndex);
                    _presenter.FilterAndDisplay(_txtSearch.Text, _currentTabFilter);
                    ShowCornerToast("✅ ĐÃ XONG CA KHÁM", $"Đã xác nhận hoàn thành khám cho {patientName}. App Mobile của bệnh nhân đã đồng bộ!", Color.FromArgb(16, 185, 129));
                };

                // Note: Bác sĩ không có quyền Hủy lịch hay Bỏ khám.
                // Bệnh nhân đã có mặt vật lý tại phòng khám, lễ tân quản lý việc đó.
                var itemTransfer = new ToolStripMenuItem("📞 Chuyển / Tái khám")
                {
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Padding = itemPad,
                    Margin = itemMarg
                };
                itemTransfer.Click += (s, ev) =>
                {
                    // Trước đây chỉ hiện toast "đang phát triển", không thực sự tạo lịch hẹn nào —
                    // giờ mở dialog đặt Tái khám thật (cùng Bác sĩ đang đăng nhập) qua API đã fix.
                    // Không đoán/bịa PatientId khi không xác định được: đây là dữ liệu thật, đặt nhầm người là sai hồ sơ.
                    var appt = _presenter.GetAppointmentById(apptId);
                    if (appt == null || appt.PatientId <= 0)
                    {
                        MessageBox.Show(
                            "Không xác định được bệnh nhân của lịch hẹn này nên không thể đặt lịch tái khám.\nVui lòng bấm \"Làm Mới\" để tải lại danh sách rồi thử lại.",
                            "Không thể đặt tái khám", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // MemberId khác null = lịch hẹn gốc đặt cho hồ sơ người thân → tái khám cũng phải đặt đúng hồ sơ đó,
                    // không thì API gán về chủ tài khoản (PatientId).
                    using (var followUpForm = new FollowUpBookingForm(
                        appt.PatientId, patientName, TokenVault.DoctorId, TokenVault.FullName, TokenVault.SpecialtyName, appt.MemberId))
                    {
                        followUpForm.ShowDialog(this);
                    }
                };

                dropdown.Items.Add(itemExam);
                dropdown.Items.Add(itemHistory);
                dropdown.Items.Add(new ToolStripSeparator());
                dropdown.Items.Add(itemStatus);
                dropdown.Items.Add(itemTransfer);

                Rectangle cellDisplayRect = _gridQueue.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                Point dropdownPoint = _gridQueue.PointToScreen(new Point(cellDisplayRect.Left + 5, cellDisplayRect.Bottom));
                dropdown.Show(dropdownPoint);
            }
        }

        private void SetActiveNavButton(Button activeBtn, List<Button> allButtons)
        {
            foreach (var b in allButtons)
            {
                b.BackColor = Color.Transparent;
                b.ForeColor = ClinicalColors.OnNavText; // chữ xanh nhạt trên nền teal
                b.Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);
                b.Invalidate();
            }
            activeBtn.BackColor = ClinicalColors.NavActive; // mục đang chọn: teal sáng
            activeBtn.ForeColor = Color.White;
            activeBtn.Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold);
            activeBtn.Invalidate();
        }

        private Button CreateNavButton(string text, int y, bool active)
        {
            Button btn = new Button
            {
                Text = text,
                Font = ClinicalColors.GetMainFont(10f, active ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = active ? Color.White : ClinicalColors.OnNavText,
                BackColor = active ? ClinicalColors.NavActive : Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(260, 44),
                Location = new Point(0, y),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Paint += (s, e) =>
            {
                if (btn.Font.Bold)
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (var brush = new SolidBrush(ClinicalColors.OnNavAccent)) // thanh nhấn bên trái của mục đang chọn
                    {
                        e.Graphics.FillRectangle(brush, 0, 4, 4, btn.Height - 8);
                    }
                }
            };
            btn.MouseEnter += (s, e) => {
                if (btn.BackColor != ClinicalColors.NavActive) // mục đang chọn giữ nguyên
                {
                    btn.BackColor = ClinicalColors.NavLight; // hover: teal sáng hơn nền một bậc
                    btn.ForeColor = Color.White;
                }
            };
            btn.MouseLeave += (s, e) => {
                if (btn.Font.Bold)
                {
                    btn.BackColor = ClinicalColors.NavActive;
                    btn.ForeColor = Color.White;
                }
                else
                {
                    btn.BackColor = Color.Transparent;
                    btn.ForeColor = ClinicalColors.OnNavText;
                }
            };
            return btn;
        }

        private Button CreateTabButton(string text, int x, bool active)
        {
            Button btn = new Button
            {
                Text = text,
                Font = ClinicalColors.GetMainFont(9.5f, active ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = active ? Color.White : ClinicalColors.TextDark,
                BackColor = active ? ClinicalColors.PrimaryBlue : Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(102, 36),
                Location = new Point(x, 15),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btn.FlatAppearance.BorderColor = ClinicalColors.BorderGray;
            btn.MouseEnter += (s, e) => {
                if (_currentTabFilter != text) btn.BackColor = Color.FromArgb(241, 245, 249);
            };
            btn.MouseLeave += (s, e) => {
                if (_currentTabFilter != text) btn.BackColor = Color.White;
            };
            return btn;
        }

        private void SelectTabFilter(string filter, Button activeBtn)
        {
            _currentTabFilter = filter;
            foreach (var btn in new[] { _btnTabAll, _btnTabWaiting, _btnTabInProgress, _btnTabCompleted, _btnTabCancelled })
            {
                if (btn == null) continue;
                btn.BackColor = Color.White;
                btn.ForeColor = ClinicalColors.TextDark;
                btn.Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular);
            }
            if (activeBtn == _btnTabCancelled)
            {
                activeBtn.BackColor = Color.FromArgb(245, 158, 11); // Golden warm yellow/amber for Cancelled tab!
            }
            else
            {
                activeBtn.BackColor = ClinicalColors.PrimaryBlue;
            }
            activeBtn.ForeColor = Color.White;
            activeBtn.Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold);
            _presenter.FilterAndDisplay(_txtSearch.Text, _currentTabFilter);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.F5)
            {
                _ = _presenter.LoadQueueAsync();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F2)
            {
                _txtSearch.Focus();
                _txtSearch.SelectAll();
                e.Handled = true;
            }
        }

        public void ShowLoading(bool isLoading)
        {
            // Bỏ dòng load api, hoàn tất v.v trên homescreen theo yêu cầu
        }

        public void DisplayAppointments(List<AppointmentModel> appointments)
        {
            if (_gridQueue == null || _gridQueue.IsDisposed) return;
            var displayList = new List<object>();
            foreach (var a in appointments)
            {
                string genderVn = a.PatientGender ?? "";
                if (genderVn.Equals("Male", StringComparison.OrdinalIgnoreCase)) genderVn = "Nam";
                else if (genderVn.Equals("Female", StringComparison.OrdinalIgnoreCase)) genderVn = "Nữ";

                string ageGenderDisplay = "";
                if (a.PatientAge > 0 && !string.IsNullOrEmpty(genderVn) && genderVn != "---") 
                {
                    ageGenderDisplay = $"{a.PatientAge} / {genderVn}";
                }
                else if (a.PatientAge > 0)
                {
                    ageGenderDisplay = $"{a.PatientAge} tuổi";
                }
                else if (!string.IsNullOrEmpty(genderVn) && genderVn != "---")
                {
                    ageGenderDisplay = genderVn;
                }

                string actionText = "Khám ▼";
                string st = a.Status?.ToString() ?? "";
                // [Old code]:
                // if (st.Equals("Completed", StringComparison.OrdinalIgnoreCase) || st.Equals("4") || st.Contains("hoàn thành"))
                // [New code]:
                if (st.Equals("Completed", StringComparison.OrdinalIgnoreCase) || st.Equals("4") || st.Contains("hoàn thành") || st.Equals("PendingDispensing", StringComparison.OrdinalIgnoreCase) || st.Contains("Chờ Dược sĩ") || st.Contains("Chờ phát thuốc") || st.Equals("PendingPayment", StringComparison.OrdinalIgnoreCase) || st.Contains("Chờ thanh toán") || st.Contains("Chờ viện phí") || st.Equals("11"))
                {
                    actionText = "Xem Hồ Sơ";
                }
                else if (st.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) || st.Equals("5") || st.Contains("Hủy") || st.Contains("hủy"))
                {
                    actionText = "Đã hủy";
                }
                else if (st.Equals("NoShow", StringComparison.OrdinalIgnoreCase) || st.Equals("6") || st.Equals("Expired", StringComparison.OrdinalIgnoreCase) || st.Contains("Quá hạn") || st.Contains("Bỏ khám") || st.Contains("Không đến"))
                {
                    actionText = "Không đến";
                }

                displayList.Add(new {
                    QueueNumber = a.QueueNumber,
                    PatientName = a.PatientName,
                    AgeGender = ageGenderDisplay,
                    SpecialtyName = a.SpecialtyName,
                    TimeSlot = a.TimeSlot,
                    Status = a.Status,
                    ActionText = actionText,
                    AppointmentId = a.AppointmentId
                });
            }
            int savedScroll = -1;
            try { savedScroll = _gridQueue.FirstDisplayedScrollingRowIndex; } catch { }
            int savedSelectedRow = _gridQueue.SelectedRows.Count > 0 ? _gridQueue.SelectedRows[0].Index : -1;

            _gridQueue.DataSource = null;
            _gridQueue.Rows.Clear();
            foreach (dynamic item in displayList)
            {
                _gridQueue.Rows.Add(item.QueueNumber, item.PatientName, item.AgeGender, item.SpecialtyName, item.TimeSlot, item.Status, item.ActionText, item.AppointmentId);
            }

            if (savedSelectedRow >= 0 && savedSelectedRow < _gridQueue.Rows.Count)
            {
                _gridQueue.ClearSelection();
                _gridQueue.Rows[savedSelectedRow].Selected = true;
            }
            if (savedScroll >= 0 && savedScroll < _gridQueue.Rows.Count)
            {
                try { _gridQueue.FirstDisplayedScrollingRowIndex = savedScroll; } catch { }
            }
        }

        public void PushNotification(string title, string patientName, string action, string timeSlot, Color accentColor, bool showToast = true)
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => PushNotification(title, patientName, action, timeSlot, accentColor, showToast)));
                return;
            }

            _unreadDoctorNotifs++;
            _notificationList.Insert(0, new ClinicalNotifItem
            {
                PatientName = !string.IsNullOrWhiteSpace(patientName) ? patientName : "Bệnh nhân",
                Action = action,
                TimeSlot = !string.IsNullOrWhiteSpace(timeSlot) ? timeSlot : DateTime.Now.ToString("HH:mm"),
                CreatedAt = DateTime.Now,
                IsRead = false
            });

            UpdateBellBadge();

            if (showToast)
            {
                ShowCornerToast(title, $"Bệnh nhân: {patientName}\n{action}", accentColor);
            }
        }

        public void OnNewAppointmentNotified(string patientName, string timeSlot, string specialtyName)
        {
            PushNotification("🔔  LỊCH KHÁM MỚI TỪ MOBILE", patientName, $"Vừa đặt lịch khám {specialtyName}", timeSlot, Color.FromArgb(16, 185, 129), true);
        }

        public void OnClinicalResultsReady(string patientName, string specialtyName)
        {
            PushNotification("🔬  ĐÃ CÓ KẾT QUẢ CẬN LÂM SÀNG", patientName, $"Đã có kết quả Xét nghiệm/Siêu âm ({specialtyName}) — mời vào phòng khám", DateTime.Now.ToString("HH:mm"), Color.FromArgb(139, 92, 246), true);
        }

        public void OnVitalsRecorded(string patientName, string timeSlot)
        {
            PushNotification("🩺  ĐÃ CÓ SINH HIỆU", patientName, "Điều dưỡng đã đo xong sinh hiệu — sẵn sàng vào phòng khám", timeSlot, Color.FromArgb(16, 185, 129), true);
        }

        private void UpdateBellBadge()
        {
            if (_lblBell == null || _lblBell.IsDisposed) return;
            _lblBell.BackColor = Color.Transparent;
            if (_unreadDoctorNotifs > 0)
            {
                _lblBell.Text = "🔔";
                _lblBell.Font = ClinicalColors.GetMainFont(16f, FontStyle.Bold);
                _lblBell.ForeColor = Color.FromArgb(252, 129, 129); // đỏ sáng khi có thông báo chưa đọc — đủ tương phản trên nền teal
                if (_lblBellBadge != null && !_lblBellBadge.IsDisposed)
                {
                    _lblBellBadge.Text = _unreadDoctorNotifs > 99 ? "99+" : _unreadDoctorNotifs.ToString();
                    _lblBellBadge.Visible = true;
                    _lblBellBadge.BringToFront();
                    _lblBellBadge.Invalidate();
                }
            }
            else
            {
                _lblBell.Text = "🔔";
                _lblBell.Font = ClinicalColors.GetMainFont(15f, FontStyle.Bold);
                _lblBell.ForeColor = Color.White; // bình thường: trắng trên nền teal
                if (_lblBellBadge != null && !_lblBellBadge.IsDisposed)
                {
                    _lblBellBadge.Visible = false;
                }
            }
        }

        private void ShowNotificationPopup()
        {
            // Reset unread counter and update badge
            _unreadDoctorNotifs = 0;
            UpdateBellBadge();

            ToolStripDropDown dropDown = new ToolStripDropDown
            {
                AutoClose = true,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                DropShadowEnabled = true
            };

            Panel pnlCard = new Panel
            {
                Size = new Size(350, 380),
                BackColor = Color.White,
                Padding = new Padding(0)
            };

            // Top Header of Notification Card
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(248, 250, 252)
            };
            Panel pnlHeaderBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ClinicalColors.BorderGray
            };
            pnlHeader.Controls.Add(pnlHeaderBorder);

            bool isReceptionist = TokenVault.RoleId == 4 || TokenVault.RoleCode == "RECEPTIONIST" || (!string.IsNullOrEmpty(TokenVault.RoleName) && TokenVault.RoleName.Contains("Lễ tân"));
            bool isNurseRole    = TokenVault.RoleId == 5 || TokenVault.RoleCode == "NURSE"          || (!string.IsNullOrEmpty(TokenVault.RoleName) && TokenVault.RoleName.Contains("Điều dưỡng"));
            bool isPharmacist   = TokenVault.RoleId == 7 || TokenVault.RoleCode == "PHARMACIST"     || (!string.IsNullOrEmpty(TokenVault.RoleName) && TokenVault.RoleName.Contains("Dược sĩ"));

            string headerTitle = isReceptionist ? "🔔   Thông Báo Lễ Tân & Thu Ngân" 
                               : isNurseRole ? "🔔   Thông Báo Trạm Điều Dưỡng" 
                               : isPharmacist ? "🔔   Thông Báo Nhà Thuốc Bệnh Viện" 
                               : "🔔   Thông Báo Phòng Khám Bác Sĩ";

            Label lblNotifTitle = new Label
            {
                Text = headerTitle,
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(14, 12),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblNotifTitle);

            if (_notificationList.Count > 0)
            {
                Label lblClear = new Label
                {
                    Text = "Xóa tất cả",
                    Font = ClinicalColors.GetMainFont(9f, FontStyle.Underline),
                    ForeColor = ClinicalColors.TextMuted,
                    Location = new Point(275, 15),
                    AutoSize = true,
                    Cursor = Cursors.Hand,
                    UseMnemonic = false
                };
                lblClear.Click += (s, e) => {
                    _notificationList.Clear();
                    dropDown.Close();
                    ShowNotificationPopup();
                };
                pnlHeader.Controls.Add(lblClear);
            }

            pnlCard.Controls.Add(pnlHeader);

            // Notification List or Empty State Container
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            if (_notificationList.Count == 0)
            {
                // Empty State
                Panel pnlEmpty = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White
                };
                Label lblIcon = new Label
                {
                    Text = "📭",
                    Font = ClinicalColors.GetMainFont(36f, FontStyle.Regular),
                    Size = new Size(350, 60),
                    Location = new Point(0, 80),
                    TextAlign = ContentAlignment.MiddleCenter,
                    UseMnemonic = false
                };
                Label lblEmptyTitle = new Label
                {
                    Text = isNurseRole ? "Trạm Điều Dưỡng Đã Sẵn Sàng" : (isReceptionist ? "Hòm thư thông báo tiếp đón trống" : (isPharmacist ? "Hòm thư thông báo nhà thuốc trống" : "Hòm thư thông báo trống")),
                    Font = ClinicalColors.GetMainFont(11f, FontStyle.Bold),
                    ForeColor = ClinicalColors.TextDark,
                    Size = new Size(350, 30),
                    Location = new Point(0, 150),
                    TextAlign = ContentAlignment.MiddleCenter,
                    UseMnemonic = false
                };
                Label lblEmptySub = new Label
                {
                    Text = isNurseRole 
                        ? "Bệnh nhân đã Check-in & bệnh nhân vãng lai\nsẽ tự động xuất hiện tại màn hình Trạm Điều Dưỡng\nđể bạn tiến hành đo sinh hiệu."
                        : (isReceptionist 
                            ? "Các thông báo lịch hẹn mới\nvà hồ sơ chờ đối chiếu CCCD sẽ xuất hiện tại đây."
                            : "Các thông báo ca khám mới từ bệnh nhân\nsẽ xuất hiện tự động tại đây."),
                    Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                    ForeColor = ClinicalColors.TextMuted,
                    Size = new Size(330, 60),
                    Location = new Point(10, 180),
                    TextAlign = ContentAlignment.TopCenter,
                    UseMnemonic = false
                };
                pnlEmpty.Controls.Add(lblIcon);
                pnlEmpty.Controls.Add(lblEmptyTitle);
                pnlEmpty.Controls.Add(lblEmptySub);
                pnlBody.Controls.Add(pnlEmpty);
            }
            else
            {
                FlowLayoutPanel pnlList = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    Padding = new Padding(8, 8, 8, 8)
                };

                foreach (var notif in _notificationList)
                {
                    Panel pnlItem = new Panel
                    {
                        Size = new Size(318, 70),
                        Margin = new Padding(0, 0, 0, 8),
                        BackColor = notif.IsRead ? Color.FromArgb(248, 250, 252) : Color.FromArgb(239, 246, 255)
                    };

                    // Left Accent Strip (Image 2 style)
                    Panel pnlAccent = new Panel
                    {
                        Dock = DockStyle.Left,
                        Width = 4,
                        BackColor = notif.IsRead ? Color.Transparent : ClinicalColors.PrimaryBlue
                    };

                    AvatarBoxControl avatar = new AvatarBoxControl(36)
                    {
                        Location = new Point(12, 16)
                    };

                    Label lblName = new Label
                    {
                        Text = notif.PatientName,
                        Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                        ForeColor = ClinicalColors.PrimaryBlue,
                        Location = new Point(56, 12),
                        Size = new Size(240, 20),
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    Label lblAction = new Label
                    {
                        Text = $"{notif.Action} • {notif.TimeSlot}",
                        Font = ClinicalColors.GetMainFont(9f, FontStyle.Regular),
                        ForeColor = ClinicalColors.TextDark,
                        Location = new Point(56, 34),
                        Size = new Size(240, 22),
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    pnlItem.Controls.Add(pnlAccent);
                    pnlItem.Controls.Add(avatar);
                    pnlItem.Controls.Add(lblName);
                    pnlItem.Controls.Add(lblAction);

                    pnlList.Controls.Add(pnlItem);

                    // Mark as read after rendering
                    notif.IsRead = true;
                }

                pnlBody.Controls.Add(pnlList);
            }

            pnlCard.Controls.Add(pnlBody);
            pnlBody.BringToFront();

            ToolStripControlHost host = new ToolStripControlHost(pnlCard)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false,
                Size = pnlCard.Size
            };

            dropDown.Items.Add(host);

            Point pt = _lblBell.PointToScreen(new Point(0, _lblBell.Height + 4));
            dropDown.Show(pt);
        }

        private void ShowCornerToast(string title, string message, Color accentColor)
        {
            if (this.IsDisposed || !this.Visible) return;
            SystemNotifier.Show(ref _notifyIcon, this, title, message, accentColor);
        }

        public void UpdateKpiCards(int total, int waiting, int inProgress, int completed)
        {
            _cardTotal.Value = total.ToString();
            _cardWaiting.Value = waiting.ToString();
            _cardInProgress.Value = inProgress.ToString();
            _cardCompleted.Value = completed.ToString();
        }

        public void OnError(string message)
        {
            _lblStatusMsg.Text = "❌ " + message;
            _lblStatusMsg.ForeColor = Color.Red;
        }
    }

    public class ModernDropdownRenderer : ToolStripProfessionalRenderer
    {
        public ModernDropdownRenderer() : base(new ModernDropdownColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if (e.Item.Selected)
            {
                using (var brush = new SolidBrush(Color.FromArgb(241, 245, 249)))
                using (var path = CreateRoundPath(new Rectangle(4, 2, e.Item.Width - 8, e.Item.Height - 4), 6))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }
            else
            {
                using (var brush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
                }
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath CreateRoundPath(Rectangle r, int rad)
        {
            var p = new System.Drawing.Drawing2D.GraphicsPath();
            int d = rad * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (var pen = new Pen(Color.FromArgb(241, 245, 249), 1f))
            {
                int y = e.Item.Height / 2;
                e.Graphics.DrawLine(pen, 12, y, e.Item.Width - 12, y);
            }
        }
    }

    public class ModernDropdownColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.White;
        public override Color ImageMarginGradientBegin => Color.White;
        public override Color ImageMarginGradientMiddle => Color.White;
        public override Color ImageMarginGradientEnd => Color.White;
        public override Color MenuBorder => Color.FromArgb(203, 213, 225);
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Color.FromArgb(241, 245, 249);
        public override Color MenuStripGradientBegin => Color.White;
        public override Color MenuStripGradientEnd => Color.White;
    }

    public static class DateHelper
    {
        public static string GetVietnameseDateString(DateTime dt)
        {
            string dayName = dt.DayOfWeek switch
            {
                DayOfWeek.Monday => "Thứ Hai",
                DayOfWeek.Tuesday => "Thứ Ba",
                DayOfWeek.Wednesday => "Thứ Tư",
                DayOfWeek.Thursday => "Thứ Năm",
                DayOfWeek.Friday => "Thứ Sáu",
                DayOfWeek.Saturday => "Thứ Bảy",
                DayOfWeek.Sunday => "Chủ Nhật",
                _ => ""
            };
            return $"{dayName}, ngày {dt:dd/MM/yyyy}";
        }
    }
}
