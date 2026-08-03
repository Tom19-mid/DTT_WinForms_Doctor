using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;
using Panel = System.Windows.Forms.Panel;
using Button = System.Windows.Forms.Button;

namespace DTT.Doctor.UI.Forms
{
    /// <summary>
    /// Danh sách toàn bộ chỉ định Xét nghiệm/Siêu âm của 1 lượt khám — để Bác sĩ xem xét kết quả
    /// (kèm ảnh siêu âm) trước khi kê đơn & bấm "Hoàn Tất". Bấm vào 1 dòng để xem chi tiết.
    /// </summary>
    public class ClinicalResultsSummaryForm : Form
    {
        private readonly int _appointmentId;
        private List<ClinicalOrderQueueItem> _items = new List<ClinicalOrderQueueItem>();
        private AntiFlickerDataGridView _grid;

        public ClinicalResultsSummaryForm(int appointmentId)
        {
            _appointmentId = appointmentId;
            InitializeComponent();
            _ = LoadAsync();
        }

        private void InitializeComponent()
        {
            Text = "Kết Quả Cận Lâm Sàng (Xét Nghiệm / Siêu Âm)";
            Size = new Size(760, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ClinicalColors.GhostWhite;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

            Label lblTitle = new Label
            {
                Text = "🔬  Bấm vào 1 dòng để xem chi tiết kết quả (kèm ảnh siêu âm nếu có):",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(20, 14),
                Size = new Size(700, 24)
            };

            _grid = new AntiFlickerDataGridView
            {
                Location = new Point(20, 46),
                Size = new Size(700, 360),
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
                RowTemplate = { Height = 38 }
            };
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            _grid.ColumnHeadersDefaultCellStyle.Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold);
            _grid.ColumnHeadersHeight = 36;
            _grid.EnableHeadersVisualStyles = false;
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Loại", Name = "ColKind", FillWeight = 55 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Dịch Vụ", Name = "ColService", FillWeight = 180 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kết Quả Tóm Tắt", Name = "ColSummary", FillWeight = 220 });
            // Tên cột KHÔNG được đặt "ColStatus" — trùng tên đó sẽ bị AntiFlickerDataGridView tự động vẽ
            // đè bằng pill trạng thái LỊCH HẸN, bỏ qua text CLS thật (luôn hiện mặc định "Đang chờ").
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng Thái", Name = "ColClsStatus", FillWeight = 100 });
            _grid.CellClick += OnGridCellClick;

            Button btnClose = new Button
            {
                Text = "Đóng",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 40),
                Location = new Point(620, 416),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClose.FlatAppearance.BorderSize = 1;
            btnClose.Click += (s, e) => Close();

            Controls.Add(lblTitle);
            Controls.Add(_grid);
            Controls.Add(btnClose);
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            var api = new ApiService();
            _items = await api.GetClinicalOrdersByAppointmentAsync(_appointmentId);
            FillGrid();
        }

        private void FillGrid()
        {
            _grid.Rows.Clear();
            foreach (var it in _items)
            {
                string kind = it.Kind == "Test" ? "XN" : "SA";
                string summary = it.Status == "Pending"
                    ? "—"
                    : it.Kind == "Test"
                        ? $"{it.ResultValue} {it.Unit}".Trim()
                        : (it.Conclusion ?? "");
                string status = it.Status switch
                {
                    "Pending" => "Chờ thực hiện",
                    "Abnormal" => "Bất thường",
                    "Normal" => "Bình thường",
                    "Completed" => "Hoàn tất",
                    "Cancelled" => "Đã hủy",
                    _ => it.Status
                };

                int idx = _grid.Rows.Add(kind, it.ServiceName, summary, status);
                if (it.Status == "Pending")
                {
                    _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(161, 98, 7);
                }
                else if (it.Status == "Abnormal")
                {
                    _grid.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(185, 28, 28);
                }
            }
        }

        private void OnGridCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
            var item = _items[e.RowIndex];
            using var dialog = new ClinicalResultDialogForm(item);
            if (dialog.ShowDialog(this) == DialogResult.OK && dialog.WasModified)
            {
                _ = LoadAsync();
            }
        }
    }
}
