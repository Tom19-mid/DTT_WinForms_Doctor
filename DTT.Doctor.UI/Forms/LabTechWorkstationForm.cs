using System;
using System.Collections.Generic;
using System.Drawing;
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
    /// Phân hệ Kỹ thuật viên Cận Lâm Sàng (Xét nghiệm / Siêu âm / X-Quang).
    /// Quy trình: Bác sĩ chỉ định CLS (status=9, "Chờ Kết Quả CLS") → KTV thực hiện + nhập kết quả
    /// → khi hết chỉ định 'Pending' của phiếu khám, bệnh nhân tự động quay lại hàng đợi Bác sĩ (status=3).
    /// Bấm vào 1 dòng để mở cửa sổ riêng nhập kết quả (ClinicalResultDialogForm) — tách khỏi màn chính
    /// để có đủ chỗ hiển thị ảnh siêu âm thật thay vì chỉ đếm số ảnh trong 1 panel chật hẹp.
    /// </summary>
    public class LabTechWorkstationForm : Form
    {
        private readonly ApiService _api = new ApiService();
        private TabControl _tabControl;
        private AntiFlickerDataGridView _gridWaiting; // Tab 0: Chờ thực hiện
        private AntiFlickerDataGridView _gridDone;    // Tab 1: Đã thực hiện hôm nay
        private List<ClinicalOrderQueueItem> _waitingList = new List<ClinicalOrderQueueItem>();
        private List<ClinicalOrderQueueItem> _doneList = new List<ClinicalOrderQueueItem>();
        private Label _lblKpiWaiting;
        private Label _lblKpiDone;

        private System.Windows.Forms.Timer _autoRefreshTimer;

        public LabTechWorkstationForm()
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
                _tabControl.SelectedIndex = index;
        }

        private void InitializeComponent()
        {
            Text = "DTT Healthcare - Trạm Kỹ Thuật Viên Cận Lâm Sàng";
            BackColor = ClinicalColors.GhostWhite;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

            // ── Top KPI strip ─────────────────────────────────────────────
            Panel pnlKpi = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = Color.White, Padding = new Padding(12, 6, 12, 0) };
            Panel pnlKpiBorder = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = ClinicalColors.BorderGray };
            pnlKpi.Controls.Add(pnlKpiBorder);

            Panel cardWaiting = BuildKpiCard("CHỜ THỰC HIỆN", "0", Color.FromArgb(139, 92, 246), out _lblKpiWaiting);
            cardWaiting.Size = new Size(210, 56);
            cardWaiting.Location = new Point(12, 6);

            Panel cardDone = BuildKpiCard("ĐÃ THỰC HIỆN HÔM NAY", "0", Color.FromArgb(16, 185, 129), out _lblKpiDone);
            cardDone.Size = new Size(210, 56);
            cardDone.Location = new Point(230, 6);

            Button btnRefresh = new Button
            {
                Text = "Làm mới",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = ClinicalColors.PrimaryBlue,
                ForeColor = Color.White,
                Size = new Size(110, 36),
                Location = new Point(450, 16),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (s, e) => { ButtonFlashHelper.Flash(btnRefresh); await RefreshAllAsync(); };

            pnlKpi.Controls.Add(cardWaiting);
            pnlKpi.Controls.Add(cardDone);
            pnlKpi.Controls.Add(btnRefresh);

            // ── Body: Tab + Grid (toàn bộ chiều rộng — không còn panel bên phải) ──
            Panel pnlBody = new Panel { Dock = DockStyle.Fill };

            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ItemSize = new Size(0, 1),
                SizeMode = TabSizeMode.Fixed,
                Appearance = TabAppearance.FlatButtons
            };

            TabPage tabWaiting = new TabPage("Chờ Thực Hiện") { BackColor = ClinicalColors.GhostWhite, Padding = new Padding(8) };
            TabPage tabDone = new TabPage("Đã Thực Hiện Hôm Nay") { BackColor = ClinicalColors.GhostWhite, Padding = new Padding(8) };

            _gridWaiting = BuildGrid();
            _gridWaiting.CellClick += async (s, e) => await OnGridCellClickAsync(_gridWaiting, _waitingList, e);
            tabWaiting.Controls.Add(_gridWaiting);

            _gridDone = BuildGrid(isDone: true);
            _gridDone.CellClick += async (s, e) => await OnGridCellClickAsync(_gridDone, _doneList, e);
            tabDone.Controls.Add(_gridDone);

            _tabControl.TabPages.Add(tabWaiting);
            _tabControl.TabPages.Add(tabDone);

            pnlBody.Controls.Add(_tabControl);

            Controls.Add(pnlBody);
            Controls.Add(pnlKpi);
        }

        // ── Grid builder ────────────────────────────────────────────────────
        private AntiFlickerDataGridView BuildGrid(bool isDone = false)
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
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                RowTemplate = { Height = 40 }
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            grid.ColumnHeadersDefaultCellStyle.Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold);
            grid.ColumnHeadersHeight = 38;
            grid.EnableHeadersVisualStyles = false;

            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", Name = "ColSTT", FillWeight = 25 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ưu Tiên", Name = "ColPriority", FillWeight = 45 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Họ Tên BN", Name = "ColName", FillWeight = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tuổi/Giới", Name = "ColAge", FillWeight = 55 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Loại", Name = "ColKind", FillWeight = 55 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Dịch Vụ Chỉ Định", Name = "ColService", FillWeight = 150 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BS Chỉ Định", Name = "ColDoctor", FillWeight = 95 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = isDone ? "Kết Quả" : "Thao Tác", Name = "ColAction", FillWeight = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kind", Name = "ColKindRaw", Visible = false });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", Name = "ColItemId", Visible = false });

            return grid;
        }

        // ── Data Loading ────────────────────────────────────────────────────
        private async Task RefreshAllAsync()
        {
            try
            {
                _waitingList = await _api.GetClinicalOrderQueueAsync(done: false);
                _doneList = await _api.GetClinicalOrderQueueAsync(done: true);

                if (_lblKpiWaiting != null) _lblKpiWaiting.Text = _waitingList.Count.ToString();
                if (_lblKpiDone != null) _lblKpiDone.Text = _doneList.Count.ToString();

                FillGrid(_gridWaiting, _waitingList, isDone: false);
                FillGrid(_gridDone, _doneList, isDone: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LabTechWorkstationForm.RefreshAllAsync error: " + ex.Message);
            }
        }

        private void FillGrid(AntiFlickerDataGridView grid, List<ClinicalOrderQueueItem> list, bool isDone)
        {
            if (grid == null || grid.IsDisposed) return;
            grid.Rows.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i];
                string ageSex = it.PatientAge > 0
                    ? $"{it.PatientAge} / {(it.PatientGender == "Male" ? "♂" : it.PatientGender == "Female" ? "♀" : "—")}"
                    : "—";
                string kindLabel = it.Kind == "Test" ? "XN" : "SA";
                string lastCol = isDone
                    ? (it.Status == "Abnormal" ? "Bất thường" : it.Status == "Normal" ? "Bình thường" : "Hoàn tất")
                    : "Nhập kết quả";
                string priorityLabel = it.IsUrgent ? "Khẩn" : "";

                int idx = grid.Rows.Add(i + 1, priorityLabel, it.PatientName, ageSex, kindLabel, it.ServiceName, it.DoctorName, lastCol, it.Kind, it.Id);
                var row = grid.Rows[idx];

                if (isDone && it.Status == "Abnormal")
                {
                    // Trước đây mọi dòng "Đã Thực Hiện" đều tô xanh lá đồng loạt, kể cả kết quả Bất
                    // thường — KTV vừa nhập xong không hề thấy cảnh báo gì, khác với màn Điều dưỡng
                    // (NurseWorkstationForm) đã tô đỏ đúng cho cùng loại dữ liệu này.
                    row.DefaultCellStyle.BackColor = Color.FromArgb(254, 226, 226);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(153, 27, 27);
                    row.DefaultCellStyle.Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold);
                }
                else if (isDone)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(236, 253, 245);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(15, 118, 110);
                }
                else if (it.IsUrgent)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(254, 226, 226);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(153, 27, 27);
                    row.DefaultCellStyle.Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold);
                }
                else
                {
                    row.DefaultCellStyle.BackColor = i % 2 == 0 ? Color.White : Color.FromArgb(248, 250, 252);
                }
            }
        }

        // ── Mở cửa sổ nhập kết quả riêng khi bấm vào 1 dòng ──────────────────
        private async Task OnGridCellClickAsync(AntiFlickerDataGridView grid, List<ClinicalOrderQueueItem> list, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count) return;
            var row = grid.Rows[e.RowIndex];
            if (row.Cells["ColItemId"].Value == null) return;

            int itemId = Convert.ToInt32(row.Cells["ColItemId"].Value);
            string kind = row.Cells["ColKindRaw"].Value?.ToString() ?? "Test";
            var item = list.Find(x => x.Id == itemId && x.Kind == kind);
            if (item == null) return;

            using var dialog = new ClinicalResultDialogForm(item);
            if (dialog.ShowDialog(this) == DialogResult.OK && dialog.WasModified)
            {
                await RefreshAllAsync();
            }
        }

        private Panel BuildKpiCard(string title, string value, Color accent, out Label valueLabel)
        {
            var pnl = new AntiFlickerPanel
            {
                Size = new Size(210, 56),
                BackColor = Color.White,
                BorderRadius = 10,
                BorderColor = ClinicalColors.BorderGray
            };
            Panel accentBar = new Panel { Location = new Point(6, 6), Size = new Size(5, 44), BackColor = accent };
            Label lblTitle = new Label
            {
                Text = title,
                Font = ClinicalColors.GetMainFont(8f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(18, 5),
                AutoSize = true,
                UseMnemonic = false
            };
            valueLabel = new Label
            {
                Text = value,
                Font = ClinicalColors.GetMainFont(16f, FontStyle.Bold),
                ForeColor = accent,
                Location = new Point(18, 22),
                AutoSize = true,
                UseMnemonic = false
            };
            pnl.Controls.Add(accentBar);
            pnl.Controls.Add(lblTitle);
            pnl.Controls.Add(valueLabel);
            return pnl;
        }
    }
}
