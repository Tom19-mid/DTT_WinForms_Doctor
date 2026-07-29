using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DTT.Doctor.Presenter.ViewModels;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;

namespace DTT.Doctor.UI.Forms
{
    public partial class MainDashboardForm : Form, IQueueView
    {
        private QueuePresenter _presenter;
        private AntiFlickerDataGridView _gridQueue;
        private TextBox _txtSearch;
        private Label _lblStatusMsg;
        private FlowLayoutPanel _pnlKpiContainer;
        private KpiCardControl _cardTotal, _cardWaiting, _cardInProgress, _cardCompleted;
        private Button _btnTabAll, _btnTabWaiting, _btnTabInProgress, _btnTabCompleted, _btnTabCancelled;
        private string _currentTabFilter = "Tất cả";
        private System.Windows.Forms.Timer _autoRefreshTimer;
        private Label _lblBell;
        private int _unreadDoctorNotifs = 0;

        public MainDashboardForm()
        {
            _presenter = new QueuePresenter(this);
            InitializeComponent();
            this.Load += async (s, e) => {
                await _presenter.LoadQueueAsync(false);
                _autoRefreshTimer = new System.Windows.Forms.Timer { Interval = 10000 };
                _autoRefreshTimer.Tick += async (ts, te) => await _presenter.LoadQueueAsync(true);
                _autoRefreshTimer.Start();
            };
            this.FormClosed += (s, e) => {
                _autoRefreshTimer?.Stop();
                _autoRefreshTimer?.Dispose();
            };
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

            // ── Left Navigation Sidebar (Deep Navy with Circular Logo & Soft Shadow) ───────
            Panel pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = ClinicalColors.DeepNavy
            };

            Panel pnlLogoBox = new Panel
            {
                Size = new Size(260, 120),
                Location = new Point(0, 0),
                BackColor = ClinicalColors.DeepNavy
            };
            CircularLogoControl circSidebarLogo = new CircularLogoControl
            {
                Size = new Size(94, 94),
                Location = new Point(83, 13),
                ShadowSpread = 8
            };
            circSidebarLogo.LoadImage(@"D:\DoAnTotNghiep\Chức năng của app bệnh nhân\Logo\DTT HEALTHCARE.png");
            pnlLogoBox.Controls.Add(circSidebarLogo);

            Panel pnlUserCard = new Panel
            {
                Size = new Size(228, 85),
                Location = new Point(16, 125),
                BackColor = ClinicalColors.SidebarDark
            };
            AvatarBoxControl sidebarAvatar = new AvatarBoxControl(46)
            {
                Location = new Point(10, 18)
            };
            Label lblUserDoc = new Label
            {
                Text = TokenVault.FullName,
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Size = new Size(160, 26),
                Location = new Point(62, 14),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            Label lblSpec = new Label
            {
                Text = "JWT: Đã xác thực",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = ClinicalColors.StatusCompletedText,
                Size = new Size(160, 24),
                Location = new Point(62, 42),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnlUserCard.Controls.Add(sidebarAvatar);
            pnlUserCard.Controls.Add(lblUserDoc);
            pnlUserCard.Controls.Add(lblSpec);

            int navY = 220;
            Button btnNavQueue = CreateNavButton("📋  Hàng Chờ Lâm Sàng", navY, true);
            Button btnNavHistory = CreateNavButton("🗂️  Hồ Sơ Bệnh Án", navY += 52, false);
            Button btnNavMeds = CreateNavButton("💊  Danh Mục && Thuốc", navY += 52, false);
            Button btnNavStats = CreateNavButton("📊  Thống Kê Ca Khám", navY += 52, false);

            Panel pnlSidebarBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = ClinicalColors.DeepNavy
            };
            RoundedButton btnLogout = new RoundedButton
            {
                Text = "🚪  Đăng Xuất",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 38, 38), // Red vibrant button as requested
                HoverBackColor = Color.FromArgb(185, 28, 28),
                BorderRadius = 16,
                Size = new Size(228, 44),
                Location = new Point(16, 16)
            };
            btnLogout.Click += (s, e) => {
                TokenVault.Clear();
                this.Hide();
                new LoginForm().Show();
            };
            pnlSidebarBottom.Controls.Add(btnLogout);

            pnlSidebar.Controls.Add(pnlLogoBox);
            pnlSidebar.Controls.Add(pnlUserCard);
            pnlSidebar.Controls.Add(btnNavQueue);
            pnlSidebar.Controls.Add(btnNavHistory);
            pnlSidebar.Controls.Add(btnNavMeds);
            pnlSidebar.Controls.Add(btnNavStats);
            pnlSidebar.Controls.Add(pnlSidebarBottom);

            // ── Top Header Bar (Matching Homescreen.png with Avatar & Bell) ───────────────────
            int headerWidth = this.ClientSize.Width - 260;
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Width = headerWidth,
                Height = 72,
                BackColor = Color.White
            };
            Panel pnlHeaderDivider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ClinicalColors.BorderGray
            };
            pnlHeader.Controls.Add(pnlHeaderDivider);

            Label lblPageTitle = new Label
            {
                Text = "Quản lý bệnh nhân",
                Font = ClinicalColors.GetMainFont(18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Size = new Size(400, 36),
                Location = new Point(24, 8),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            Label lblSubtitle = new Label
            {
                Text = "Xem danh sách đặt lịch và tiếp nhận bệnh nhân hôm nay",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Size = new Size(500, 24),
                Location = new Point(26, 42),
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };

            Button btnRefresh = new Button
            {
                Text = "🔄 Làm mới",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = ClinicalColors.TextDark,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(115, 36),
                Location = new Point(headerWidth - 555, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnRefresh.FlatAppearance.BorderColor = ClinicalColors.BorderGray;
            btnRefresh.Click += async (s, e) => await _presenter.LoadQueueAsync();
            btnRefresh.MouseEnter += (s, e) => { btnRefresh.BackColor = Color.FromArgb(226, 232, 240); };
            btnRefresh.MouseLeave += (s, e) => { btnRefresh.BackColor = Color.FromArgb(241, 245, 249); };

            _lblBell = new Label
            {
                Text = "🔔",
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                BackColor = Color.FromArgb(241, 245, 249),
                Size = new Size(60, 36),
                Location = new Point(headerWidth - 425, 17),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _lblBell.Click += (s, e) => {
                if (_unreadDoctorNotifs > 0)
                {
                    MessageBox.Show($"Bạn có {_unreadDoctorNotifs} ca khám mới từ App Bệnh Nhân vừa tự động cập nhật vào danh sách lâm sàng!", "Hòm Thư Ca Khám Mới", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _unreadDoctorNotifs = 0;
                    UpdateBellBadge();
                }
                else
                {
                    MessageBox.Show("Hiện tại không có thông báo ca khám mới nào chưa đọc.", "Thông Báo Lâm Sàng", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            Label lblTopDoctor = new Label
            {
                Text = $"{TokenVault.FullName}\nQuản trị viên",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Size = new Size(180, 42),
                Location = new Point(headerWidth - 355, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleRight,
                UseMnemonic = false
            };

            AvatarBoxControl topAvatar = new AvatarBoxControl(44)
            {
                Location = new Point(headerWidth - 165, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            RoundedButton btnHeaderLogout = new RoundedButton
            {
                Text = "Đăng xuất",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = Color.FromArgb(220, 38, 38),
                HoverBackColor = Color.FromArgb(220, 38, 38),
                BorderRadius = 14,
                Size = new Size(96, 36),
                Location = new Point(headerWidth - 110, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnHeaderLogout.MouseEnter += (s, e) => { btnHeaderLogout.ForeColor = Color.White; };
            btnHeaderLogout.MouseLeave += (s, e) => { btnHeaderLogout.ForeColor = Color.FromArgb(220, 38, 38); };
            btnHeaderLogout.Click += (s, e) => {
                TokenVault.Clear();
                this.Hide();
                new LoginForm().Show();
            };

            pnlHeader.Controls.Add(lblPageTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(btnRefresh);
            pnlHeader.Controls.Add(_lblBell);
            pnlHeader.Controls.Add(lblTopDoctor);
            pnlHeader.Controls.Add(topAvatar);
            pnlHeader.Controls.Add(btnHeaderLogout);

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
            _btnTabWaiting = CreateTabButton("Đang chờ", 95, false);
            _btnTabInProgress = CreateTabButton("Đang khám", 195, false);
            _btnTabCompleted = CreateTabButton("Đã xong", 295, false);
            _btnTabCancelled = CreateTabButton("Hủy Lịch", 395, false);

            _btnTabAll.Click += (s, e) => SelectTabFilter("Tất cả", _btnTabAll);
            _btnTabWaiting.Click += (s, e) => SelectTabFilter("Đang chờ", _btnTabWaiting);
            _btnTabInProgress.Click += (s, e) => SelectTabFilter("Đang khám", _btnTabInProgress);
            _btnTabCompleted.Click += (s, e) => SelectTabFilter("Đã xong", _btnTabCompleted);
            _btnTabCancelled.Click += (s, e) => SelectTabFilter("Hủy Lịch", _btnTabCancelled);

            Label lblSearchIcon = new Label
            {
                Text = "🔍 (F2):",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(510, 22),
                AutoSize = true
            };

            _txtSearch = new TextBox
            {
                Location = new Point(585, 17),
                Size = new Size(240, 30),
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular),
                PlaceholderText = "Tìm theo Tên hoặc STT..."
            };
            _txtSearch.TextChanged += (s, e) => _presenter.FilterAndDisplay(_txtSearch.Text, _currentTabFilter);
            _txtSearch.MouseEnter += (s, e) => { _txtSearch.BackColor = Color.FromArgb(248, 250, 252); };
            _txtSearch.MouseLeave += (s, e) => { _txtSearch.BackColor = Color.White; };

            _lblStatusMsg = new Label { Visible = false }; // Bỏ dòng load api, hoàn tất v.v trên homescreen

            RoundedButton btnReloadLive = new RoundedButton
            {
                Text = "🔄  Làm Mới",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129), // Vibrant emerald green
                HoverBackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                BorderRadius = 12,
                Size = new Size(130, 35),
                Location = new Point(845, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            btnReloadLive.Click += async (s, e) => await _presenter.LoadQueueAsync(false);

            pnlFilterBar.Controls.Add(_btnTabAll);
            pnlFilterBar.Controls.Add(_btnTabWaiting);
            pnlFilterBar.Controls.Add(_btnTabInProgress);
            pnlFilterBar.Controls.Add(_btnTabCompleted);
            pnlFilterBar.Controls.Add(_btnTabCancelled);
            pnlFilterBar.Controls.Add(lblSearchIcon);
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

            pnlMain.Controls.Add(pnlTableCard);
            pnlMain.Controls.Add(pnlFilterBar);
            pnlMain.Controls.Add(_pnlKpiContainer);

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
                itemExam.Click += (s, ev) => {
                    ShowCornerToast("🩺 KHÁM LÂM SÀNG", $"Chuẩn bị chuyển sang màn hình khám bệnh chuyên sâu cho {patientName} (Phase 2).", ClinicalColors.PrimaryBlue);
                };

                var itemHistory = new ToolStripMenuItem("📋 Hồ sơ bệnh án")
                {
                    ForeColor = Color.FromArgb(71, 85, 105),
                    Padding = itemPad,
                    Margin = itemMarg
                };
                itemHistory.Click += (s, ev) => {
                    ShowCornerToast("📋 HỒ SƠ BỆNH ÁN", $"Đang truy xuất lịch sử khám và bệnh án của {patientName}...", Color.FromArgb(71, 85, 105));
                };

                var itemStatus = new ToolStripMenuItem("✔️ Đã hoàn thành")
                {
                    ForeColor = Color.FromArgb(16, 185, 129),
                    Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                    Padding = itemPad,
                    Margin = itemMarg
                };
                itemStatus.Click += async (s, ev) => {
                    await _presenter.UpdateStatusAsync(apptId, "Completed");
                    row.Cells[5].Value = "Completed";
                    _gridQueue.InvalidateRow(e.RowIndex);
                    _presenter.FilterAndDisplay(_txtSearch.Text, _currentTabFilter);
                    ShowCornerToast("✅ ĐÃ XONG CA KHÁM", $"Đã xác nhận hoàn thành khám cho {patientName}. App Mobile của bệnh nhân đã đồng bộ!", Color.FromArgb(16, 185, 129));
                };

                var itemCancel = new ToolStripMenuItem("❌ Hủy lịch khám")
                {
                    ForeColor = Color.FromArgb(239, 68, 68),
                    Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                    Padding = itemPad,
                    Margin = itemMarg
                };
                itemCancel.Click += async (s, ev) => {
                    await _presenter.UpdateStatusAsync(apptId, "Cancelled");
                    row.Cells[5].Value = "Cancelled";
                    _gridQueue.InvalidateRow(e.RowIndex);
                    _presenter.FilterAndDisplay(_txtSearch.Text, _currentTabFilter);
                    ShowCornerToast("❌ ĐÃ HỦY LỊCH", $"Đã hủy lịch khám của {patientName}. Hệ thống vừa gửi thông báo qua App Mobile cho bệnh nhân!", Color.FromArgb(239, 68, 68));
                };

                dropdown.Items.Add(itemExam);
                dropdown.Items.Add(itemHistory);
                dropdown.Items.Add(new ToolStripSeparator());
                dropdown.Items.Add(itemStatus);
                dropdown.Items.Add(itemCancel);

                Rectangle cellDisplayRect = _gridQueue.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                Point dropdownPoint = _gridQueue.PointToScreen(new Point(cellDisplayRect.Left + 5, cellDisplayRect.Bottom));
                dropdown.Show(dropdownPoint);
            }
        }

        private Button CreateNavButton(string text, int y, bool active)
        {
            Button btn = new Button
            {
                Text = text,
                Font = ClinicalColors.GetMainFont(10.5f, active ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = active ? Color.White : Color.FromArgb(203, 213, 225),
                BackColor = active ? ClinicalColors.SidebarDark : Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(260, 48),
                Location = new Point(0, y),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btn.FlatAppearance.BorderSize = 0;
            if (!active)
            {
                btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(30, 41, 59); btn.ForeColor = Color.White; };
                btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; btn.ForeColor = Color.FromArgb(203, 213, 225); };
            }
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
                Size = new Size(92, 34),
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
            var displayList = new List<object>();
            foreach (var a in appointments)
            {
                string ageGenderDisplay = (a.PatientAge > 0 && !string.IsNullOrEmpty(a.PatientGender) && a.PatientGender != "---") 
                                          ? $"{a.PatientAge} / {a.PatientGender}" 
                                          : "";
                displayList.Add(new {
                    QueueNumber = a.QueueNumber,
                    PatientName = a.PatientName,
                    AgeGender = ageGenderDisplay,
                    SpecialtyName = a.SpecialtyName,
                    TimeSlot = a.TimeSlot,
                    Status = a.Status,
                    AppointmentId = a.AppointmentId
                });
            }
            _gridQueue.DataSource = null;
            _gridQueue.Rows.Clear();
            foreach (dynamic item in displayList)
            {
                _gridQueue.Rows.Add(item.QueueNumber, item.PatientName, item.AgeGender, item.SpecialtyName, item.TimeSlot, item.Status, "Khám ▼", item.AppointmentId);
            }
        }

        public void OnNewAppointmentNotified(string patientName, string timeSlot, string specialtyName)
        {
            if (this.IsDisposed || !this.Visible) return;
            _unreadDoctorNotifs++;
            UpdateBellBadge();
            ShowCornerToast("🔔  LỊCH KHÁM MỚI TỪ MOBILE", 
                            $"Bệnh nhân: {patientName}\nGiờ hẹn: {timeSlot}\nĐã tự động thêm vào danh sách!", 
                            Color.FromArgb(16, 185, 129));
        }

        private void UpdateBellBadge()
        {
            if (_lblBell == null || _lblBell.IsDisposed) return;
            if (_unreadDoctorNotifs > 0)
            {
                _lblBell.Text = $"🔔  {_unreadDoctorNotifs}";
                _lblBell.BackColor = Color.FromArgb(254, 226, 226);
                _lblBell.ForeColor = Color.FromArgb(220, 38, 38);
            }
            else
            {
                _lblBell.Text = "🔔";
                _lblBell.BackColor = Color.FromArgb(241, 245, 249);
                _lblBell.ForeColor = ClinicalColors.TextMuted;
            }
        }

        private void ShowCornerToast(string title, string message, Color accentColor)
        {
            if (this.IsDisposed || !this.Visible) return;

            AntiFlickerPanel toast = new AntiFlickerPanel
            {
                Size = new Size(330, 95),
                BackColor = Color.White,
                BorderColor = accentColor,
                BorderRadius = 12,
                Padding = new Padding(12, 8, 12, 8),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(this.ClientSize.Width - 350, this.ClientSize.Height - 115),
                Cursor = Cursors.Hand
            };
            Action dismiss = () => { if (!toast.IsDisposed && this.Controls.Contains(toast)) { this.Controls.Remove(toast); toast.Dispose(); } };
            toast.Click += (s, e) => { dismiss(); };

            Panel strip = new Panel { Dock = DockStyle.Left, Width = 5, BackColor = accentColor };
            Label lblTitle = new Label
            {
                Text = title,
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = accentColor,
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label lblMsg = new Label
            {
                Text = message,
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextDark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft
            };
            lblMsg.Click += (s, e) => { dismiss(); };
            lblTitle.Click += (s, e) => { dismiss(); };

            toast.Controls.Add(lblMsg);
            toast.Controls.Add(lblTitle);
            toast.Controls.Add(strip);

            this.Controls.Add(toast);
            toast.BringToFront();

            try { System.Media.SystemSounds.Asterisk.Play(); } catch { }

            System.Windows.Forms.Timer toastTimer = new System.Windows.Forms.Timer { Interval = 5500 };
            toastTimer.Tick += (s, e) => {
                toastTimer.Stop();
                toastTimer.Dispose();
                if (!toast.IsDisposed && this.Controls.Contains(toast))
                {
                    this.Controls.Remove(toast);
                    toast.Dispose();
                }
            };
            toastTimer.Start();
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
}
