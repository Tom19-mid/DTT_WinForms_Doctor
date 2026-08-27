using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;
using Panel = System.Windows.Forms.Panel;
using Button = System.Windows.Forms.Button;
using TabPage = System.Windows.Forms.TabPage;
using TabControl = System.Windows.Forms.TabControl;

namespace DTT.Doctor.UI.Forms
{
    /// <summary>
    /// Phân hệ Trạm Dược Sĩ (Nhà Thuốc Bệnh Viện).
    /// Quy trình: Bác sĩ khám và kê đơn (StatusId=10, "PendingDispensing") 
    /// → Đơn thuốc xuất hiện trong hàng đợi của Dược sĩ
    /// → Dược sĩ kiểm tra đơn thuốc, đối chiếu tồn kho và bấm "Xác Nhận Phát Thuốc"
    /// → Đơn thuốc chuyển sang StatusId=4 (Completed), lưu người phát & thời gian phát vào DB.
    /// </summary>
    public class PharmacistWorkstationForm : Form
    {
        private readonly ApiService _api = new ApiService();
        private TabControl _tabControl;
        private AntiFlickerDataGridView _gridWaiting; // Tab 0: Chờ phát thuốc
        private AntiFlickerDataGridView _gridDone;    // Tab 1: Đã phát hôm nay

        private List<PharmacyQueueItem> _waitingList = new List<PharmacyQueueItem>();
        private List<PharmacyHistoryItem> _doneList = new List<PharmacyHistoryItem>();
        // [New code]: Danh sách đơn đã phát riêng trong ngày hôm nay để đếm KPI
        private List<PharmacyHistoryItem> _doneTodayList = new List<PharmacyHistoryItem>();

        private Label _lblKpiWaiting;
        // [Old code]: private Label _lblKpiDone;
        private Label _lblKpiDoneToday;
        private Label _lblKpiDoneTotal;
        private Button _btnTabWaiting;
        private Button _btnTabDone;

        // [New code]: Các điều khiển lọc và tìm kiếm lịch sử phát thuốc
        private TextBox _txtSearchDone;
        private DateTimePicker _dtpFilterDone;
        private CheckBox _chkAllDatesDone;
        private Button _btnSearchDone;
        private Button _btnTodayDone;

        private System.Windows.Forms.Timer _autoRefreshTimer;

        public PharmacistWorkstationForm()
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

        public async Task LoadDataAsync() => await RefreshAllAsync();

        public void SelectTab(int index)
        {
            if (_tabControl != null && index < _tabControl.TabPages.Count)
            {
                _tabControl.SelectedIndex = index;
                UpdateTabButtons(index);
            }
        }

        private void InitializeComponent()
        {
            Text = "DTT Healthcare - Trạm Dược Sĩ / Nhà Thuốc Bệnh Viện";
            BackColor = ClinicalColors.GhostWhite;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

            // ── 1. Top KPI strip ─────────────────────────────────────────────
            Panel pnlKpi = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.White,
                Padding = new Padding(12, 6, 12, 0)
            };
            Panel pnlKpiBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ClinicalColors.BorderGray
            };
            pnlKpi.Controls.Add(pnlKpiBorder);

            // [Old code]:
            // Panel cardWaiting = BuildKpiCard("CHỜ PHÁT THUỐC", "0", Color.FromArgb(139, 92, 246), out _lblKpiWaiting);
            // Panel cardDone = BuildKpiCard("ĐÃ PHÁT HÔM NAY", "0", Color.FromArgb(16, 185, 129), out _lblKpiDone);

            // [New code]: Bổ sung 3 thẻ KPI rõ ràng: Chờ phát hôm nay, Đã phát hôm nay, Tổng lịch sử đã phát
            Panel cardWaiting = BuildKpiCard("CHỜ PHÁT HÔM NAY", "0", Color.FromArgb(139, 92, 246), out _lblKpiWaiting);
            cardWaiting.Size = new Size(195, 56);
            cardWaiting.Location = new Point(12, 6);

            Panel cardDoneToday = BuildKpiCard("ĐÃ PHÁT HÔM NAY", "0", Color.FromArgb(16, 185, 129), out _lblKpiDoneToday);
            cardDoneToday.Size = new Size(195, 56);
            cardDoneToday.Location = new Point(215, 6);

            Panel cardDoneTotal = BuildKpiCard("TỔNG LỊCH SỬ ĐÃ PHÁT", "0", Color.FromArgb(14, 165, 233), out _lblKpiDoneTotal);
            cardDoneTotal.Size = new Size(205, 56);
            cardDoneTotal.Location = new Point(418, 6);

            Button btnRefresh = new Button
            {
                Text = "🔄 Làm mới",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = ClinicalColors.PrimaryBlue,
                ForeColor = Color.White,
                Size = new Size(110, 36),
                Location = new Point(635, 16),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (s, e) => await RefreshAllAsync();

            pnlKpi.Controls.Add(cardWaiting);
            pnlKpi.Controls.Add(cardDoneToday);
            pnlKpi.Controls.Add(cardDoneTotal);
            pnlKpi.Controls.Add(btnRefresh);

            // ── 2. Tab switcher bar ──────────────────────────────────────────
            Panel pnlTabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.White,
                Padding = new Padding(12, 4, 12, 4)
            };
            Panel pnlTabBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ClinicalColors.BorderGray
            };
            pnlTabBar.Controls.Add(pnlTabBorder);

            // [Old code]:
            // _btnTabWaiting = new Button { Text = "📋 Chờ Phát Thuốc", ... };
            // _btnTabDone = new Button { Text = "✅ Đã Phát Hôm Nay", ... };

            // [New code]:
            _btnTabWaiting = new Button
            {
                Text = "📋 Chờ Phát Thuốc (Hôm Nay)",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(237, 233, 254),
                ForeColor = Color.FromArgb(109, 40, 217),
                // [Old size: 220px -> bị che chữ (Hôm Nay)]
                // [New size: 270px thoáng đãng, hiện trọn vẹn]:
                Size = new Size(270, 34),
                Location = new Point(12, 5),
                Cursor = Cursors.Hand
            };
            _btnTabWaiting.FlatAppearance.BorderSize = 0;
            _btnTabWaiting.Click += (s, e) => SelectTab(0);

            _btnTabDone = new Button
            {
                Text = "📜 Lịch Sử Đã Cấp Phát",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(71, 85, 105),
                // [Old size: 200px -> bị che chữ Đã Cấp Phát]
                // [New size: 240px, Location: Point(290, 5)]:
                Size = new Size(240, 34),
                Location = new Point(290, 5),
                Cursor = Cursors.Hand
            };
            _btnTabDone.FlatAppearance.BorderSize = 0;
            _btnTabDone.Click += (s, e) => SelectTab(1);

            pnlTabBar.Controls.Add(_btnTabWaiting);
            pnlTabBar.Controls.Add(_btnTabDone);

            // ── 3. Body: TabControl + Grids ──────────────────────────────────
            Panel pnlBody = new Panel { Dock = DockStyle.Fill };

            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ItemSize = new Size(0, 1),
                SizeMode = TabSizeMode.Fixed,
                Appearance = TabAppearance.FlatButtons
            };

            TabPage tabWaiting = new TabPage("Chờ Phát Thuốc") { BackColor = ClinicalColors.GhostWhite, Padding = new Padding(8) };
            TabPage tabDone = new TabPage("Lịch Sử Đã Phát") { BackColor = ClinicalColors.GhostWhite, Padding = new Padding(8) };

            _gridWaiting = BuildWaitingGrid();
            _gridWaiting.CellClick += async (s, e) => await OnWaitingGridCellClickAsync(e);
            tabWaiting.Controls.Add(_gridWaiting);

            // [New code]: Xây dựng thanh lọc và tìm kiếm cho Tab Lịch Sử
            Panel pnlFilterHistory = BuildHistoryFilterPanel();
            _gridDone = BuildDoneGrid();
            _gridDone.CellClick += async (s, e) => await OnDoneGridCellClickAsync(e);

            tabDone.Controls.Add(_gridDone);
            tabDone.Controls.Add(pnlFilterHistory); // Dock.Top will sit above _gridDone (Dock.Fill)

            _tabControl.TabPages.Add(tabWaiting);
            _tabControl.TabPages.Add(tabDone);

            pnlBody.Controls.Add(_tabControl);

            Controls.Add(pnlBody);
            Controls.Add(pnlTabBar);
            Controls.Add(pnlKpi);
        }

        private Panel BuildHistoryFilterPanel()
        {
            Panel pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.White,
                Padding = new Padding(12, 6, 12, 6),
                Margin = new Padding(0, 0, 0, 8)
            };

            _txtSearchDone = new TextBox
            {
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                Location = new Point(12, 10),
                Size = new Size(260, 26),
                PlaceholderText = "🔍 Tìm bệnh nhân, SĐT, mã ca..."
            };
            _txtSearchDone.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await LoadHistoryDataAsync(); };

            Label lblDate = new Label
            {
                Text = "Ngày phát:",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(285, 13),
                AutoSize = true
            };

            _dtpFilterDone = new DateTimePicker
            {
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Location = new Point(360, 10),
                Size = new Size(130, 26),
                Value = DateTime.Today
            };
            _dtpFilterDone.ValueChanged += async (s, e) => { if (!_chkAllDatesDone.Checked) await LoadHistoryDataAsync(); };

            _chkAllDatesDone = new CheckBox
            {
                Text = "Tất cả ngày",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(500, 12),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            _chkAllDatesDone.CheckedChanged += async (s, e) =>
            {
                _dtpFilterDone.Enabled = !_chkAllDatesDone.Checked;
                await LoadHistoryDataAsync();
            };

            _btnSearchDone = new Button
            {
                Text = "🔍 Tìm kiếm",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = ClinicalColors.PrimaryBlue,
                ForeColor = Color.White,
                Location = new Point(610, 8),
                Size = new Size(105, 30),
                Cursor = Cursors.Hand
            };
            _btnSearchDone.FlatAppearance.BorderSize = 0;
            _btnSearchDone.Click += async (s, e) => await LoadHistoryDataAsync();

            _btnTodayDone = new Button
            {
                Text = "🔄 Hôm nay",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(724, 8),
                Size = new Size(120, 30),
                Cursor = Cursors.Hand
            };
            _btnTodayDone.FlatAppearance.BorderSize = 0;
            _btnTodayDone.Click += async (s, e) =>
            {
                _chkAllDatesDone.Checked = false;
                _dtpFilterDone.Value = DateTime.Today;
                _txtSearchDone.Text = "";
                await LoadHistoryDataAsync();
            };

            pnl.Controls.Add(_txtSearchDone);
            pnl.Controls.Add(lblDate);
            pnl.Controls.Add(_dtpFilterDone);
            pnl.Controls.Add(_chkAllDatesDone);
            pnl.Controls.Add(_btnSearchDone);
            pnl.Controls.Add(_btnTodayDone);

            return pnl;
        }

        private void UpdateTabButtons(int activeIndex)
        {
            if (activeIndex == 0)
            {
                _btnTabWaiting.BackColor = Color.FromArgb(237, 233, 254);
                _btnTabWaiting.ForeColor = Color.FromArgb(109, 40, 217);
                _btnTabWaiting.Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold);

                _btnTabDone.BackColor = Color.Transparent;
                _btnTabDone.ForeColor = Color.FromArgb(71, 85, 105);
                _btnTabDone.Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);
            }
            else
            {
                _btnTabDone.BackColor = Color.FromArgb(236, 253, 245);
                _btnTabDone.ForeColor = Color.FromArgb(15, 118, 110);
                _btnTabDone.Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold);

                _btnTabWaiting.BackColor = Color.Transparent;
                _btnTabWaiting.ForeColor = Color.FromArgb(71, 85, 105);
                _btnTabWaiting.Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);
            }
        }

        // ── Grid Builders ───────────────────────────────────────────────────

        private AntiFlickerDataGridView BuildWaitingGrid()
        {
            var grid = new AntiFlickerDataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                GridColor = ClinicalColors.BorderGray,
                BorderStyle = BorderStyle.None,
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                RowTemplate = { Height = 40 }
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            grid.ColumnHeadersDefaultCellStyle.Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold);
            grid.ColumnHeadersHeight = 38;
            grid.EnableHeadersVisualStyles = false;

            // [New code - Cấu hình độ rộng tối ưu, hiển thị trọn vẹn mọi tiêu đề cột không bị cắt chữ]:
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", Name = "ColSTT", Width = 55, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Họ và Tên Bệnh Nhân", Name = "ColName", FillWeight = 85, MinimumWidth = 140 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tuổi / Giới", Name = "ColAge", Width = 135, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Bác Sĩ Kê Đơn", Name = "ColDoctor", FillWeight = 110, MinimumWidth = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Chẩn Đoán", Name = "ColDiagnosis", FillWeight = 135, MinimumWidth = 175 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số Thuốc", Name = "ColDrugCount", Width = 115, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Giờ Kê Đơn", Name = "ColTime", Width = 135, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thao Tác", Name = "ColAction", Width = 135, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "AppointmentId", Name = "ColApptId", Visible = false });

            // [Fix]: Khóa Sort khi click vào STT hoặc tiêu đề cột để danh sách không bị xáo trộn thứ tự
            foreach (DataGridViewColumn col in grid.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            return grid;
        }

        private AntiFlickerDataGridView BuildDoneGrid()
        {
            var grid = new AntiFlickerDataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                GridColor = ClinicalColors.BorderGray,
                BorderStyle = BorderStyle.None,
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                RowTemplate = { Height = 40 }
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            grid.ColumnHeadersDefaultCellStyle.Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold);
            grid.ColumnHeadersHeight = 38;
            grid.EnableHeadersVisualStyles = false;

            // [New code - Cấu hình độ rộng rộng rãi, hiển thị trọn vẹn mọi cột]:
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", Name = "ColSTT", Width = 55, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã Đơn / Ca", Name = "ColCode", Width = 130, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Họ và Tên Bệnh Nhân", Name = "ColName", FillWeight = 80, MinimumWidth = 135 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tuổi / Giới", Name = "ColAge", Width = 135, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Bác Sĩ Kê Đơn", Name = "ColDoctor", FillWeight = 95, MinimumWidth = 140 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Chẩn Đoán", Name = "ColDiagnosis", FillWeight = 105, MinimumWidth = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thuốc Đã Cấp Phát", Name = "ColSummary", FillWeight = 150, MinimumWidth = 195 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thời Gian Phát", Name = "ColTime", Width = 150, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Người Phát", Name = "ColPharmacist", Width = 160, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ghi Chú Dược Sĩ", Name = "ColNote", FillWeight = 95, MinimumWidth = 125 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thao Tác", Name = "ColAction", Width = 110, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });

            // [Fix]: Khóa Sort khi click vào STT hoặc tiêu đề cột để danh sách không bị xáo trộn thứ tự
            foreach (DataGridViewColumn col in grid.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            return grid;
        }

        private Panel BuildKpiCard(string title, string initialValue, Color accentColor, out Label lblValueOut)
        {
            Panel card = new Panel
            {
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12, 6, 12, 6)
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(ClinicalColors.BorderGray, 1f))
                {
                    var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
                using (var b = new SolidBrush(accentColor))
                {
                    e.Graphics.FillRectangle(b, 0, 0, 4, card.Height);
                }
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = ClinicalColors.GetMainFont(8f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(12, 6),
                AutoSize = true
            };

            Label lblVal = new Label
            {
                Text = initialValue,
                Font = ClinicalColors.GetMainFont(14f, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(12, 24),
                AutoSize = true
            };

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblVal);
            lblValueOut = lblVal;
            return card;
        }

        // ── Data Refreshing ─────────────────────────────────────────────────

        private async Task RefreshAllAsync()
        {
            try
            {
                // [New code]: Hàng chờ chỉ lấy hôm nay (todayOnly: true)
                _waitingList = await _api.GetPharmacyQueueAsync(todayOnly: true);
                _doneTodayList = await _api.GetPharmacyHistoryAsync(date: DateTime.Today);

                if (_lblKpiWaiting != null) _lblKpiWaiting.Text = _waitingList.Count.ToString();
                if (_lblKpiDoneToday != null) _lblKpiDoneToday.Text = _doneTodayList.Count.ToString();

                FillWaitingGrid();
                await LoadHistoryDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PharmacistWorkstationForm.RefreshAllAsync] Error: {ex.Message}");
            }
        }

        private async Task LoadHistoryDataAsync()
        {
            try
            {
                DateTime? filterDate = (_chkAllDatesDone != null && _chkAllDatesDone.Checked) ? (DateTime?)null : _dtpFilterDone?.Value ?? DateTime.Today;
                string search = _txtSearchDone?.Text ?? "";

                _doneList = await _api.GetPharmacyHistoryAsync(date: filterDate, search: search);

                if (_lblKpiDoneTotal != null)
                {
                    _lblKpiDoneTotal.Text = _doneList.Count.ToString();
                }

                FillDoneGrid();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PharmacistWorkstationForm.LoadHistoryDataAsync] Error: {ex.Message}");
            }
        }

        private void FillWaitingGrid()
        {
            if (_gridWaiting == null || _gridWaiting.IsDisposed) return;
            _gridWaiting.Rows.Clear();

            for (int i = 0; i < _waitingList.Count; i++)
            {
                var it = _waitingList[i];
                string genderSymbol = it.PatientGender == "Male" || it.PatientGender == "Nam" ? "♂" : (it.PatientGender == "Female" || it.PatientGender == "Nữ" ? "♀" : "—");
                string ageSex = it.PatientAge > 0 ? $"{it.PatientAge} / {genderSymbol}" : "—";
                string docName = !string.IsNullOrEmpty(it.DoctorDegree) ? $"{it.DoctorDegree} {it.DoctorName}" : it.DoctorName;
                string timeStr = !string.IsNullOrEmpty(it.TimeSlot) ? it.TimeSlot : it.CreatedAt.ToString("HH:mm");

                int idx = _gridWaiting.Rows.Add(
                    i + 1,
                    it.PatientName,
                    ageSex,
                    docName,
                    it.Diagnosis,
                    $"{it.DrugCount} loại",
                    timeStr,
                    "👉 Phát thuốc",
                    it.AppointmentId
                );

                var row = _gridWaiting.Rows[idx];
                row.DefaultCellStyle.BackColor = i % 2 == 0 ? Color.White : Color.FromArgb(248, 250, 252);
            }
        }

        private void FillDoneGrid()
        {
            if (_gridDone == null || _gridDone.IsDisposed) return;
            _gridDone.Rows.Clear();

            for (int i = 0; i < _doneList.Count; i++)
            {
                var it = _doneList[i];
                string genderSymbol = it.PatientGender == "Male" || it.PatientGender == "Nam" ? "♂" : (it.PatientGender == "Female" || it.PatientGender == "Nữ" ? "♀" : "—");
                string ageSex = it.PatientAge > 0 ? $"{it.PatientAge} / {genderSymbol}" : "—";
                string code = $"RX-{it.DispensedAt.Year:0000}-{it.PrescriptionId:D4}";
                string timeStr = it.DispensedAt.ToString("HH:mm dd/MM/yy");
                // [Old code]: string pharmacistNote = !string.IsNullOrEmpty(it.Note) ? it.Note : "—";
                // [New code - Trích xuất phần ghi chú thực tế của Dược sĩ]:
                string pharmacistNote = !string.IsNullOrEmpty(it.Note) ? it.Note : "—";
                if (pharmacistNote.Contains("[") && pharmacistNote.Contains("]:"))
                {
                    int colonIdx = pharmacistNote.IndexOf("]:");
                    pharmacistNote = pharmacistNote.Substring(colonIdx + 2).Trim();
                }
                else if (pharmacistNote.StartsWith("[Đã phát bởi"))
                {
                    pharmacistNote = "—";
                }
                if (string.IsNullOrWhiteSpace(pharmacistNote)) pharmacistNote = "—";

                int idx = _gridDone.Rows.Add(
                    i + 1,
                    code,
                    it.PatientName,
                    ageSex,
                    it.DoctorName,
                    it.Diagnosis,
                    it.Summary,
                    timeStr,
                    it.DispensedByName,
                    pharmacistNote,
                    "👁 Xem Đơn"
                );

                var row = _gridDone.Rows[idx];
                row.DefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            }
        }

        // ── Row Click: Open Dispense Dialog ─────────────────────────────────

        private async Task OnWaitingGridCellClickAsync(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _gridWaiting.Rows.Count) return;
            var row = _gridWaiting.Rows[e.RowIndex];
            if (row.Cells["ColApptId"].Value == null) return;

            int apptId = Convert.ToInt32(row.Cells["ColApptId"].Value);
            var item = _waitingList.Find(x => x.AppointmentId == apptId);
            if (item == null) return;

            using (var dlg = new PharmacyDispenseDialogForm(item))
            {
                var res = dlg.ShowDialog(this);
                if (res == DialogResult.OK || dlg.WasModified)
                {
                    await RefreshAllAsync();
                }
            }
        }

        // [New code]: Khi bấm vào dòng hoặc nút "Xem Đơn" trong tab Lịch sử -> Mở xem chi tiết đơn đã phát
        private async Task OnDoneGridCellClickAsync(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _doneList.Count) return;
            var item = _doneList[e.RowIndex];
            if (item == null) return;

            using (var dlg = new PharmacyDispenseDialogForm(item))
            {
                dlg.ShowDialog(this);
            }
            await Task.CompletedTask;
        }
    }
}
