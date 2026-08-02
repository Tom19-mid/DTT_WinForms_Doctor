using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;
using ReaLTaiizor.Controls;
using Panel = System.Windows.Forms.Panel;
using Button = System.Windows.Forms.Button;
using TabPage = System.Windows.Forms.TabPage;
using TabControl = System.Windows.Forms.TabControl;

namespace DTT.Doctor.UI.Forms
{
    /// <summary>
    /// Phân hệ Điều dưỡng — Đo sinh hiệu tiền lâm sàng.
    /// Quy trình: Lễ tân Check-in (status=7) → ĐD đo sinh hiệu (status=8) → BS khám (status=3).
    /// </summary>
    public class NurseWorkstationForm : Form
    {
        // ── Fields ──────────────────────────────────────────────────────────
        private readonly ApiService _api = new ApiService();
        private TabControl _tabControl;
        private AntiFlickerDataGridView _gridWaiting;   // Tab 0: Chờ đo
        private AntiFlickerDataGridView _gridDone;      // Tab 1: Đã đo hôm nay
        private List<AppointmentModel> _waitingList = new List<AppointmentModel>();
        private List<AppointmentModel> _doneList    = new List<AppointmentModel>();
        private Label _lblKpiWaiting;
        private Label _lblKpiDone;

        // Tab 2/3: Xem (read-only) tiến độ Xét nghiệm/Siêu âm do Bác sĩ chỉ định — Điều dưỡng chỉ
        // xem để biết bệnh nhân đã làm CLS hay chưa, KHÔNG được sửa/nhập kết quả (việc đó thuộc KTV).
        private AntiFlickerDataGridView _gridLabTests;
        private AntiFlickerDataGridView _gridUltrasounds;
        private List<ClinicalOrderQueueItem> _labTestList = new List<ClinicalOrderQueueItem>();
        private List<ClinicalOrderQueueItem> _ultrasoundList = new List<ClinicalOrderQueueItem>();

        // Vitals input panel (inline, right side)
        private Panel      _pnlVitals;
        private Label      _lblVitalsTitle;
        private MaterialTextBoxEdit _txtBP;       // Huyết áp
        private MaterialTextBoxEdit _txtHR;       // Nhịp tim
        private MaterialTextBoxEdit _txtTemp;     // Thân nhiệt
        private MaterialTextBoxEdit _txtWeight;   // Cân nặng
        private MaterialTextBoxEdit _txtHeight;   // Chiều cao
        private Label      _lblBmi;               // BMI tự tính
        private MaterialTextBoxEdit _txtNote;     // Ghi chú tự do
        private Button     _btnSaveVitals;
        private Label      _lblVitalsStatus;
        private int        _selectedAppointmentId  = 0;
        private string     _selectedPatientName    = "";

        // Phát hiện bệnh nhân MỚI check-in để báo (toast) cho Điều dưỡng — trước đây màn này không
        // hề tự làm mới định kỳ (khác màn Bác sĩ đã có auto-refresh 10s), nên Điều dưỡng chỉ thấy
        // bệnh nhân mới khi tự bấm "Làm mới" hoặc chuyển tab qua lại, không có gì báo hiệu.
        private System.Windows.Forms.Timer _autoRefreshTimer;
        private HashSet<int> _seenWaitingIds = new HashSet<int>();
        private HashSet<string> _seenDoneClsKeys = new HashSet<string>();
        private bool _hasLoadedOnce = false;

        // ── Constructor ─────────────────────────────────────────────────────
        public NurseWorkstationForm()
        {
            InitializeComponent();
            this.Shown += async (s, e) =>
            {
                await RefreshAllAsync();
                if (_autoRefreshTimer == null)
                {
                    _autoRefreshTimer = new System.Windows.Forms.Timer { Interval = 10000 };
                    _autoRefreshTimer.Tick += async (ts, te) => await RefreshAllAsync();
                    _autoRefreshTimer.Start();
                }
            };
            this.VisibleChanged += async (s, e) => { if (this.Visible) await RefreshAllAsync(); };
            this.FormClosed += (s, e) =>
            {
                _autoRefreshTimer?.Stop();
                _autoRefreshTimer?.Dispose();
            };
        }

        // ── Public API ──────────────────────────────────────────────────────
        public async Task LoadDataAsync() => await RefreshAllAsync();

        public void SelectTab(int index)
        {
            if (_tabControl != null && index < _tabControl.TabPages.Count)
            {
                _tabControl.SelectedIndex = index;
                UpdateVitalsPanelVisibility();
            }
        }

        private void UpdateVitalsPanelVisibility()
        {
            if (_pnlVitals != null && _tabControl != null)
            {
                bool showVitals = _tabControl.SelectedIndex == 0 || _tabControl.SelectedIndex == 1;
                _pnlVitals.Visible = showVitals;
            }
        }

        // ── InitializeComponent ──────────────────────────────────────────────
        private void InitializeComponent()
        {
            Text        = "DTT Healthcare - Trạm Điều Dưỡng";
            BackColor   = ClinicalColors.GhostWhite;
            Font        = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

            // ── Top KPI strip ─────────────────────────────────────────────
            Panel pnlKpi = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 68,
                BackColor = Color.White,
                Padding   = new Padding(12, 6, 12, 0)
            };
            Panel pnlKpiBorder = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = ClinicalColors.BorderGray };
            pnlKpi.Controls.Add(pnlKpiBorder);

            Panel cardWaiting = BuildKpiCard("CHỜ ĐO SINH HIỆU", "0", Color.FromArgb(234, 179, 8), out _lblKpiWaiting);
            cardWaiting.Size     = new Size(210, 56);
            cardWaiting.Location = new Point(12, 6);

            Panel cardDone = BuildKpiCard("ĐÃ ĐO HÔM NAY", "0", Color.FromArgb(16, 185, 129), out _lblKpiDone);
            cardDone.Size     = new Size(210, 56);
            cardDone.Location = new Point(230, 6);

            Button btnRefresh = new Button
            {
                Text      = "🔄 Làm mới",
                Font      = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = ClinicalColors.PrimaryBlue,
                ForeColor = Color.White,
                Size      = new Size(110, 36),
                Location  = new Point(450, 16),
                Cursor    = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (s, e) => await RefreshAllAsync();

            pnlKpi.Controls.Add(cardWaiting);
            pnlKpi.Controls.Add(cardDone);
            pnlKpi.Controls.Add(btnRefresh);

            // ── Body: Left tab + Right vitals panel ──────────────────────
            Panel pnlBody = new Panel { Dock = DockStyle.Fill };

            // Right vitals panel (always visible, filled when patient selected)
            _pnlVitals = BuildVitalsPanel();
            _pnlVitals.Width = 360;
            _pnlVitals.Dock  = DockStyle.Right;

            // Left: TabControl
            _tabControl = new TabControl
            {
                Dock       = DockStyle.Fill,
                Font       = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ItemSize   = new Size(0, 1),
                SizeMode   = TabSizeMode.Fixed,
                Appearance = TabAppearance.FlatButtons
            };
            _tabControl.SelectedIndexChanged += (s, e) => UpdateVitalsPanelVisibility();

            TabPage tabWaiting     = new TabPage("Chờ Đo Sinh Hiệu") { BackColor = ClinicalColors.GhostWhite, Padding = new Padding(8) };
            TabPage tabDone        = new TabPage("Đã Đo Hôm Nay")    { BackColor = ClinicalColors.GhostWhite, Padding = new Padding(8) };
            TabPage tabLabTests    = new TabPage("Xét Nghiệm")       { BackColor = ClinicalColors.GhostWhite, Padding = new Padding(8) };
            TabPage tabUltrasounds = new TabPage("Siêu Âm")          { BackColor = ClinicalColors.GhostWhite, Padding = new Padding(8) };

            // Grid: Chờ đo
            _gridWaiting = BuildGrid();
            _gridWaiting.CellClick += OnWaitingGridCellClick;
            tabWaiting.Controls.Add(_gridWaiting);

            // Grid: Đã đo
            _gridDone = BuildGrid(isDone: true);
            tabDone.Controls.Add(_gridDone);

            // Grid: Xét nghiệm / Siêu âm — CHỈ XEM để biết bệnh nhân đã làm CLS hay chưa,
            // không có sự kiện CellClick nào để sửa/nhập kết quả (việc đó dành riêng cho KTV).
            _gridLabTests = BuildClsGrid();
            tabLabTests.Controls.Add(_gridLabTests);

            _gridUltrasounds = BuildClsGrid();
            tabUltrasounds.Controls.Add(_gridUltrasounds);

            _tabControl.TabPages.Add(tabWaiting);
            _tabControl.TabPages.Add(tabDone);
            _tabControl.TabPages.Add(tabLabTests);
            _tabControl.TabPages.Add(tabUltrasounds);

            pnlBody.Controls.Add(_tabControl);
            pnlBody.Controls.Add(_pnlVitals);

            Controls.Add(pnlBody);
            Controls.Add(pnlKpi);

            this.Load += async (s, e) => await RefreshAllAsync();
        }

        // ── Vitals Input Panel (Right Side) ─────────────────────────────────
        private Panel BuildVitalsPanel()
        {
            var pnl = new AntiFlickerPanel
            {
                BackColor    = Color.White,
                BorderRadius = 0,
                BorderColor  = ClinicalColors.BorderGray,
                Padding      = new Padding(16)
            };

            // Divider line on left
            Panel divider = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = ClinicalColors.BorderGray };
            pnl.Controls.Add(divider);

            _lblVitalsTitle = new Label
            {
                Text        = "CHỌN BỆNH NHÂN\nĐể bắt đầu đo sinh hiệu",
                Font        = ClinicalColors.GetMainFont(11f, FontStyle.Bold),
                ForeColor   = ClinicalColors.TextDark,
                Location    = new Point(20, 16),
                Size        = new Size(320, 50),
                TextAlign   = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };
            pnl.Controls.Add(_lblVitalsTitle);

            // ── Vitals inputs ────────────────────────────────────────────
            int y = 76;
            Label MkLbl(string text) => new Label
            {
                Text      = text,
                Font      = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                Location  = new Point(20, y),
                AutoSize  = true
            };

            Label lblBP = MkLbl("Huyết áp (mmHg)"); y += 18;
            _txtBP = new MaterialTextBoxEdit { Location = new Point(20, y), Size = new Size(320, 48), Hint = "Ví dụ: 120/80" };
            _txtBP.TextChanged += RecalcBmi;
            y += 54;

            Label lblHR = MkLbl("Nhịp tim (bpm)"); y += 18;
            _txtHR = new MaterialTextBoxEdit { Location = new Point(20, y), Size = new Size(320, 48), Hint = "Ví dụ: 75" };
            y += 54;

            Label lblTemp = MkLbl("Thân nhiệt (°C)"); y += 18;
            _txtTemp = new MaterialTextBoxEdit { Location = new Point(20, y), Size = new Size(320, 48), Hint = "Ví dụ: 36.5" };
            y += 54;

            Label lblW = MkLbl("Cân nặng (kg)"); y += 18;
            _txtWeight = new MaterialTextBoxEdit { Location = new Point(20, y), Size = new Size(155, 48), Hint = "Ví dụ: 60" };
            _txtWeight.TextChanged += RecalcBmi;

            Label lblH = MkLbl("Chiều cao (cm)");
            lblH.Location = new Point(185, y - 18);
            _txtHeight = new MaterialTextBoxEdit { Location = new Point(185, y), Size = new Size(155, 48), Hint = "Ví dụ: 165" };
            _txtHeight.TextChanged += RecalcBmi;
            y += 54;

            // BMI badge (Sleek pill design)
            _lblBmi = new Label
            {
                Text      = "BMI: --",
                Font      = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                BackColor = Color.FromArgb(238, 242, 255),
                Location  = new Point(20, y),
                Size      = new Size(320, 32),
                TextAlign = ContentAlignment.MiddleCenter,
                UseMnemonic = false
            };
            y += 40;

            Label lblNote = MkLbl("Ghi chú điều dưỡng"); y += 18;
            _txtNote = new MaterialTextBoxEdit { Location = new Point(20, y), Size = new Size(320, 48), Hint = "Tình trạng chung, ghi chú thêm..." };
            y += 56;

            // Status message
            _lblVitalsStatus = new Label
            {
                Text      = "",
                Font      = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                Location  = new Point(20, y),
                Size      = new Size(320, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                UseMnemonic = false
            };
            y += 26;

            // Save button
            _btnSaveVitals = new Button
            {
                Text      = "LƯU & CHUYỂN CHO BÁC SĨ",
                Font      = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                Location  = new Point(20, y),
                Size      = new Size(320, 44),
                Cursor    = Cursors.Hand,
                Enabled   = false,
                UseMnemonic = false
            };
            _btnSaveVitals.FlatAppearance.BorderSize = 0;
            _btnSaveVitals.Click += async (s, e) => await SaveVitalsAsync();

            foreach (var ctrl in new System.Windows.Forms.Control[]
            {
                _lblVitalsTitle,
                lblBP, _txtBP, lblHR, _txtHR, lblTemp, _txtTemp,
                lblW, _txtWeight, lblH, _txtHeight,
                _lblBmi, lblNote, _txtNote, _lblVitalsStatus, _btnSaveVitals
            })
                pnl.Controls.Add(ctrl);

            return pnl;
        }

        // ── Grid builder ────────────────────────────────────────────────────
        private AntiFlickerDataGridView BuildGrid(bool isDone = false)
        {
            var grid = new AntiFlickerDataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor       = Color.White,
                GridColor             = ClinicalColors.BorderGray,
                BorderStyle           = BorderStyle.None,
                Font                  = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                RowTemplate           = { Height = 40 }
            };

            // Style header
            grid.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(241, 245, 249);
            grid.ColumnHeadersDefaultCellStyle.ForeColor  = Color.FromArgb(30, 41, 59);
            grid.ColumnHeadersDefaultCellStyle.Font       = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold);
            grid.ColumnHeadersHeight                       = 38;
            grid.EnableHeadersVisualStyles                 = false;

            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT",         Name = "ColSTT",     FillWeight = 30 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Họ Tên BN",   Name = "ColName",    FillWeight = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tuổi / Giới", Name = "ColAge",     FillWeight = 60 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Chuyên Khoa", Name = "ColSpec",    FillWeight = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Giờ Hẹn",    Name = "ColSlot",    FillWeight = 60 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Phòng",       Name = "ColRoom",    FillWeight = 50 });
            if (!isDone)
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thao Tác", Name = "ColAction", FillWeight = 80 });
            else
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BMI",      Name = "ColBmi",    FillWeight = 50 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", Name = "ColApptId", Visible = false });

            return grid;
        }

        // ── CLS read-only grid builder (Xét nghiệm / Siêu âm) ────────────────
        // Điều dưỡng chỉ XEM tiến độ, không thao tác được (không CellClick, không panel nhập liệu).
        private AntiFlickerDataGridView BuildClsGrid()
        {
            var grid = new AntiFlickerDataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor       = Color.White,
                GridColor             = ClinicalColors.BorderGray,
                BorderStyle           = BorderStyle.None,
                Font                  = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                RowTemplate           = { Height = 40 }
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            grid.ColumnHeadersDefaultCellStyle.Font      = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold);
            grid.ColumnHeadersHeight        = 38;
            grid.EnableHeadersVisualStyles  = false;

            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT",              Name = "ColSTT",     FillWeight = 25 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Họ Tên BN",         Name = "ColName",    FillWeight = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Dịch Vụ Chỉ Định",  Name = "ColService", FillWeight = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BS Chỉ Định",       Name = "ColDoctor",  FillWeight = 100 });
            // Tên cột KHÔNG được đặt "ColStatus" — trùng tên đó sẽ bị AntiFlickerDataGridView tự động vẽ
            // đè bằng pill trạng thái LỊCH HẸN (InProgress/Completed/...), bỏ qua hẳn text CLS thật đã gán
            // (luôn hiện mặc định "Đang chờ" dù giá trị thật là "✅ Đã có kết quả").
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng Thái",        Name = "ColClsStatus",  FillWeight = 90 });

            return grid;
        }

        private void FillClsGrid(AntiFlickerDataGridView grid, List<ClinicalOrderQueueItem> list)
        {
            if (grid == null || grid.IsDisposed) return;
            grid.Rows.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i];
                int idx = grid.Rows.Add(i + 1, it.PatientName, it.ServiceName, it.DoctorName, FormatClsStatus(it.Status));
                var row = grid.Rows[idx];
                if (it.Status == "Pending")
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(254, 249, 195);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(161, 98, 7);
                }
                else if (it.Status == "Abnormal")
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(254, 226, 226);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(185, 28, 28);
                }
                else
                {
                    row.DefaultCellStyle.BackColor = i % 2 == 0 ? Color.White : Color.FromArgb(248, 250, 252);
                }
            }
        }

        private static string FormatClsStatus(string status)
        {
            switch (status)
            {
                case "Pending": return "⏳ Chưa làm";
                case "Abnormal": return "⚠️ Bất thường";
                case "Normal": return "✅ Bình thường";
                case "Completed": return "✅ Đã có kết quả";
                default: return status ?? "";
            }
        }

        // ── Data Loading ────────────────────────────────────────────────────
        private async Task RefreshAllAsync()
        {
            try
            {
                var all = await _api.GetQueueAppointmentsAsync();

                // Tab 0: Bệnh nhân đã tiếp đón/Check-in (status=7 hoặc CheckedIn/Confirmed) – chờ điều dưỡng đo
                _waitingList = all.FindAll(a => a.Status == "CheckedIn" || a.Status == "Confirmed" || a.Status == "Waiting");

                // Tab 1: Bệnh nhân đã WaitingForDoctor (status=8) – ĐD đã đo xong hôm nay
                _doneList = all.FindAll(a => a.Status == "WaitingForDoctor" || a.Status == "InProgress" || a.Status == "Completed");

                // Tab 2/3: Xem tiến độ CLS (chưa làm + đã có kết quả hôm nay) — chỉ để tham khảo
                var labTests = new List<ClinicalOrderQueueItem>();
                labTests.AddRange(await _api.GetClinicalOrderQueueAsync("Test", done: false));
                labTests.AddRange(await _api.GetClinicalOrderQueueAsync("Test", done: true));
                _labTestList = labTests;

                var ultrasounds = new List<ClinicalOrderQueueItem>();
                ultrasounds.AddRange(await _api.GetClinicalOrderQueueAsync("Ultrasound", done: false));
                ultrasounds.AddRange(await _api.GetClinicalOrderQueueAsync("Ultrasound", done: true));
                _ultrasoundList = ultrasounds;

                // Báo (toast) nếu có bệnh nhân MỚI vừa xuất hiện trong danh sách chờ đo — bỏ qua lần
                // tải đầu tiên (mở form) để không toast hàng loạt cho các ca đã có sẵn từ trước.
                var currentIds = new HashSet<int>(_waitingList.ConvertAll(a => a.AppointmentId));
                if (_hasLoadedOnce)
                {
                    var newArrivals = _waitingList.FindAll(a => !_seenWaitingIds.Contains(a.AppointmentId));
                    if (newArrivals.Count == 1)
                    {
                        ShowNurseToast("🔔 BỆNH NHÂN MỚI CHECK-IN",
                            $"{newArrivals[0].PatientName} vừa được Lễ Tân check-in — cần đo sinh hiệu.",
                            Color.FromArgb(16, 185, 129));
                    }
                    else if (newArrivals.Count > 1)
                    {
                        ShowNurseToast("🔔 BỆNH NHÂN MỚI CHECK-IN",
                            $"{newArrivals.Count} bệnh nhân vừa được Lễ Tân check-in — cần đo sinh hiệu.",
                            Color.FromArgb(16, 185, 129));
                    }
                }
                _seenWaitingIds = currentIds;

                // Báo (toast) khi có kết quả Xét nghiệm/Siêu âm MỚI vừa hoàn tất — Điều dưỡng cũng cần biết
                // để phối hợp gọi bệnh nhân quay lại phòng khám, không chỉ Bác sĩ mới được báo.
                var doneNow = new List<ClinicalOrderQueueItem>();
                doneNow.AddRange(_labTestList.FindAll(x => x.Status != "Pending"));
                doneNow.AddRange(_ultrasoundList.FindAll(x => x.Status != "Pending"));
                var doneNowKeys = new HashSet<string>(doneNow.ConvertAll(x => $"{x.Kind}-{x.Id}"));
                if (_hasLoadedOnce)
                {
                    var newlyDone = doneNow.FindAll(x => !_seenDoneClsKeys.Contains($"{x.Kind}-{x.Id}"));
                    if (newlyDone.Count == 1)
                    {
                        ShowNurseToast("🔬 CÓ KẾT QUẢ CLS MỚI",
                            $"{newlyDone[0].PatientName} vừa có kết quả {newlyDone[0].ServiceName} — mời quay lại phòng khám Bác sĩ.",
                            Color.FromArgb(139, 92, 246));
                    }
                    else if (newlyDone.Count > 1)
                    {
                        ShowNurseToast("🔬 CÓ KẾT QUẢ CLS MỚI",
                            $"{newlyDone.Count} kết quả Xét nghiệm/Siêu âm vừa hoàn tất.",
                            Color.FromArgb(139, 92, 246));
                    }
                }
                _seenDoneClsKeys = doneNowKeys;
                _hasLoadedOnce = true;

                // Refresh KPI
                if (_lblKpiWaiting != null) _lblKpiWaiting.Text = _waitingList.Count.ToString();
                if (_lblKpiDone    != null) _lblKpiDone.Text    = _doneList.Count.ToString();

                // Refresh grids
                FillGrid(_gridWaiting, _waitingList, isDone: false);
                FillGrid(_gridDone,    _doneList,    isDone: true);
                FillClsGrid(_gridLabTests,    _labTestList);
                FillClsGrid(_gridUltrasounds, _ultrasoundList);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("NurseWorkstationForm.RefreshAllAsync error: " + ex.Message);
            }
        }

        private void FillGrid(AntiFlickerDataGridView grid, List<AppointmentModel> list, bool isDone)
        {
            if (grid == null || grid.IsDisposed) return;
            grid.Rows.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var a   = list[i];
                string ageSex = a.PatientAge > 0
                    ? $"{a.PatientAge} tuổi / {(a.PatientGender == "Nam" ? "♂" : "♀")}"
                    : (a.PatientGender == "Nam" ? "♂" : a.PatientGender == "Nữ" ? "♀" : "—");
                string action = isDone ? ExtractBmi(a.NurseNote) : "▶ Đo sinh hiệu";

                int idx = grid.Rows.Add(i + 1, a.PatientName, ageSex, a.SpecialtyName, a.TimeSlot, a.ClinicRoom, action, a.AppointmentId);
                var row = grid.Rows[idx];

                if (isDone)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(236, 253, 245);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(15, 118, 110);
                }
                else
                {
                    // Alternating rows
                    row.DefaultCellStyle.BackColor = i % 2 == 0 ? Color.White : Color.FromArgb(248, 250, 252);
                }
            }
        }

        // ── Grid Events ─────────────────────────────────────────────────────
        private void OnWaitingGridCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _gridWaiting.Rows.Count) return;
            var row = _gridWaiting.Rows[e.RowIndex];

            if (row.Cells["ColApptId"].Value == null) return;
            _selectedAppointmentId = Convert.ToInt32(row.Cells["ColApptId"].Value);
            _selectedPatientName   = row.Cells["ColName"].Value?.ToString() ?? "Bệnh nhân";

            // Highlight row
            for (int i = 0; i < _gridWaiting.Rows.Count; i++)
                _gridWaiting.Rows[i].DefaultCellStyle.BackColor = i % 2 == 0 ? Color.White : Color.FromArgb(248, 250, 252);
            row.DefaultCellStyle.BackColor = Color.FromArgb(219, 234, 254); // light blue

            // Update vitals panel
            _lblVitalsTitle.Text = $"ĐO SINH HIỆU:\n{_selectedPatientName.ToUpper()}";
            _lblVitalsTitle.ForeColor = ClinicalColors.PrimaryBlue;

            // Clear old inputs
            _txtBP.Text = _txtHR.Text = _txtTemp.Text = _txtWeight.Text = _txtHeight.Text = _txtNote.Text = "";
            _lblBmi.Text = "BMI: --";
            _lblVitalsStatus.Text = "";
            _btnSaveVitals.Enabled = true;
            _btnSaveVitals.BackColor = Color.FromArgb(22, 163, 74);
        }

        // ── BMI Auto-Calculation ─────────────────────────────────────────────
        private void RecalcBmi(object sender, EventArgs e)
        {
            if (double.TryParse(_txtWeight.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double weight) &&
                double.TryParse(_txtHeight.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double height) &&
                height > 0 && weight > 0)
            {
                double bmi = weight / Math.Pow(height / 100.0, 2);
                bmi = Math.Round(bmi, 1);
                string category;
                Color bmiBgColor;
                Color bmiFgColor;

                if (bmi < 18.5)
                {
                    category = "Thiếu cân ⚠️";
                    bmiBgColor = Color.FromArgb(254, 249, 195); // Light Yellow/Gold tint (#FEF9C3)
                    bmiFgColor = Color.FromArgb(161, 98, 7);    // Dark Gold text (#A16207)
                }
                else if (bmi < 25.0)
                {
                    category = "Bình thường ✓";
                    bmiBgColor = Color.FromArgb(220, 252, 231); // Soft Emerald tint (#DCFCE7)
                    bmiFgColor = Color.FromArgb(21, 128, 61);   // Dark Emerald text (#15803D)
                }
                else if (bmi < 30.0)
                {
                    category = "Thừa cân ⚠️";
                    bmiBgColor = Color.FromArgb(255, 237, 213); // Soft Orange tint (#FFEDD5)
                    bmiFgColor = Color.FromArgb(194, 65, 12);   // Dark Orange text (#C2410C)
                }
                else
                {
                    category = "Béo phì 🚨";
                    bmiBgColor = Color.FromArgb(254, 226, 226); // Soft Red tint (#FEE2E2)
                    bmiFgColor = Color.FromArgb(185, 28, 28);   // Dark Red text (#B91C1C)
                }

                _lblBmi.Text      = $"BMI: {bmi}  —  {category}";
                _lblBmi.BackColor = bmiBgColor;
                _lblBmi.ForeColor = bmiFgColor;
            }
            else
            {
                _lblBmi.Text      = "BMI: --  (Nhập Cân nặng & Chiều cao)";
                _lblBmi.BackColor = Color.FromArgb(241, 245, 249); // Gentle Slate Tint
                _lblBmi.ForeColor = Color.FromArgb(100, 116, 139); // Subtle Slate Muted Text
            }
        }

        // ── Save Vitals ─────────────────────────────────────────────────────
        private async Task SaveVitalsAsync()
        {
            if (_selectedAppointmentId <= 0)
            {
                _lblVitalsStatus.Text      = "⚠️ Chưa chọn bệnh nhân!";
                _lblVitalsStatus.ForeColor = Color.FromArgb(220, 38, 38);
                return;
            }

            // Parse inputs
            if (!int.TryParse(_txtHR.Text.Trim(), out int hr)) hr = 0;
            if (!double.TryParse(_txtTemp.Text.Replace(",", ".").Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double temp)) temp = 0;
            if (!double.TryParse(_txtWeight.Text.Replace(",", ".").Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double weight)) weight = 0;
            if (!double.TryParse(_txtHeight.Text.Replace(",", ".").Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double height)) height = 0;
            string bp = _txtBP.Text.Trim();

            // Validate at least BP or HR
            if (string.IsNullOrEmpty(bp) && hr <= 0)
            {
                _lblVitalsStatus.Text      = "⚠️ Vui lòng nhập ít nhất Huyết áp hoặc Nhịp tim!";
                _lblVitalsStatus.ForeColor = Color.FromArgb(220, 38, 38);
                return;
            }

            _btnSaveVitals.Enabled = false;
            _btnSaveVitals.Text    = "⏳ Đang lưu...";
            _lblVitalsStatus.Text  = "";

            var (success, bmi) = await _api.SaveNurseVitalsAsync(
                _selectedAppointmentId, bp, hr, temp, weight, height, _txtNote.Text.Trim());

            if (success)
            {
                _lblVitalsStatus.Text      = $"✅ Đã lưu! BMI: {(bmi > 0 ? bmi.ToString("F1") : "--")}. Bệnh nhân chờ BS khám.";
                _lblVitalsStatus.ForeColor = Color.FromArgb(22, 163, 74);
                _btnSaveVitals.Text        = "✅  ĐÃ LƯU THÀNH CÔNG";
                _btnSaveVitals.BackColor   = ClinicalColors.TextMuted;

                // Auto-refresh after 1.5 seconds
                await Task.Delay(1500);
                _selectedAppointmentId = 0;
                _selectedPatientName   = "";
                _lblVitalsTitle.Text   = "CHỌN BỆNH NHÂN\nĐể bắt đầu đo sinh hiệu";
                _btnSaveVitals.Text    = "✅  LƯU & CHUYỂN CHO BÁC SĨ";
                _btnSaveVitals.Enabled = false;
                _lblVitalsStatus.Text  = "";
                await RefreshAllAsync();
            }
            else
            {
                _lblVitalsStatus.Text      = "❌ Lỗi! Bệnh nhân chưa Check-in hoặc kết nối API thất bại.";
                _lblVitalsStatus.ForeColor = Color.FromArgb(220, 38, 38);
                _btnSaveVitals.Enabled     = true;
                _btnSaveVitals.Text        = "✅  LƯU & CHUYỂN CHO BÁC SĨ";
            }
        }

        // Toast góc dưới-phải cho Điều dưỡng (phỏng theo mẫu ShowCornerToast của màn Bác sĩ)
        private void ShowNurseToast(string title, string message, Color accentColor)
        {
            if (this.IsDisposed || !this.Visible) return;

            AntiFlickerPanel toast = new AntiFlickerPanel
            {
                Size = new Size(360, 80),
                BackColor = Color.FromArgb(15, 23, 42),
                BorderColor = accentColor,
                BorderRadius = 10,
                Padding = new Padding(12, 10, 12, 10),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(this.ClientSize.Width - 380, this.ClientSize.Height - 95),
                Cursor = Cursors.Hand
            };

            Action dismiss = () => { if (!toast.IsDisposed && this.Controls.Contains(toast)) { this.Controls.Remove(toast); toast.Dispose(); } };
            toast.Click += (s, e) => dismiss();

            Panel strip = new Panel { Location = new Point(0, 0), Size = new Size(6, 80), BackColor = accentColor };

            Label lblClose = new Label
            {
                Text = "✕",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(332, 6),
                Size = new Size(20, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblClose.Click += (s, e) => dismiss();

            Label lblTitle = new Label
            {
                Text = title,
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(18, 10),
                Size = new Size(310, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblTitle.Click += (s, e) => dismiss();

            Label lblMsg = new Label
            {
                Text = message,
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(226, 232, 240),
                Location = new Point(18, 32),
                Size = new Size(330, 42),
                TextAlign = ContentAlignment.TopLeft
            };
            lblMsg.Click += (s, e) => dismiss();

            toast.Controls.Add(strip);
            toast.Controls.Add(lblClose);
            toast.Controls.Add(lblTitle);
            toast.Controls.Add(lblMsg);

            this.Controls.Add(toast);
            toast.BringToFront();

            try { System.Media.SystemSounds.Asterisk.Play(); } catch { }

            System.Windows.Forms.Timer toastTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            toastTimer.Tick += (s, e) =>
            {
                toastTimer.Stop();
                toastTimer.Dispose();
                dismiss();
            };
            toastTimer.Start();
        }

        // ── Helpers ─────────────────────────────────────────────────────────
        private static string ExtractBmi(string? nurseNoteJson)
        {
            if (string.IsNullOrEmpty(nurseNoteJson)) return "--";
            try
            {
                var doc = JsonDocument.Parse(nurseNoteJson);
                if (doc.RootElement.TryGetProperty("bmi", out var bmiEl))
                {
                    double bmi = bmiEl.GetDouble();
                    return bmi > 0 ? bmi.ToString("F1") : "--";
                }
            }
            catch { }
            return "--";
        }

        private Panel BuildKpiCard(string title, string value, Color accent, out Label valueLabel)
        {
            var pnl = new AntiFlickerPanel
            {
                Size         = new Size(210, 56),
                BackColor    = Color.White,
                BorderRadius = 10,
                BorderColor  = ClinicalColors.BorderGray
            };
            Panel accentBar = new Panel
            {
                Location  = new Point(6, 6),
                Size      = new Size(5, 44),
                BackColor = accent
            };
            Label lblTitle = new Label
            {
                Text        = title,
                Font        = ClinicalColors.GetMainFont(8f, FontStyle.Bold),
                ForeColor   = ClinicalColors.TextMuted,
                Location    = new Point(18, 5),
                AutoSize    = true,
                UseMnemonic = false
            };
            valueLabel = new Label
            {
                Text        = value,
                Font        = ClinicalColors.GetMainFont(16f, FontStyle.Bold),
                ForeColor   = accent,
                Location    = new Point(18, 22),
                AutoSize    = true,
                UseMnemonic = false
            };
            pnl.Controls.Add(accentBar);
            pnl.Controls.Add(lblTitle);
            pnl.Controls.Add(valueLabel);
            return pnl;
        }
    }
}
