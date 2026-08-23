using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;
using ReaLTaiizor.Controls;
using Button = System.Windows.Forms.Button;
using CheckBox = System.Windows.Forms.CheckBox;

namespace DTT.Doctor.UI.Forms
{
    // Bác sĩ chọn Xét nghiệm và/hoặc Siêu âm để chỉ định ngay trong lúc "Đang Khám".
    // Có thể chọn 1 trong 2 loại, hoặc cả hai cùng lúc — mỗi mục được chọn sẽ tạo 1 chỉ định riêng.
    public partial class ClinicalOrderPickerForm : Form
    {
        private readonly int _appointmentId;
        private readonly int _patientId;
        private readonly int _doctorId;

        private CheckedListBox _clbTests;
        private CheckedListBox _clbUltrasounds;
        private Label _lblSummary;
        private Button _btnConfirm;
        private CheckBox _chkUrgent;
        private MaterialTextBoxEdit _txtClinicalNote;

        private List<ClinicalOrderServiceItem> _allServices = new List<ClinicalOrderServiceItem>();

        public int OrderedCount { get; private set; } = 0;

        public ClinicalOrderPickerForm(int appointmentId, int patientId, int doctorId)
        {
            _appointmentId = appointmentId;
            _patientId = patientId;
            _doctorId = doctorId;
            InitializeComponent();
            _ = LoadServicesAsync();
        }

        private void InitializeComponent()
        {
            Text = "Chỉ Định Cận Lâm Sàng (Xét Nghiệm / Siêu Âm)";
            Size = new Size(900, 680);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ClinicalColors.GhostWhite;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

            Label lblTitle = new Label
            {
                Text = "🔬  Chọn dịch vụ cần chỉ định (có thể chọn cả Xét nghiệm và Siêu âm):",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(20, 16),
                Size = new Size(840, 24)
            };

            Label lblTestHeader = new Label
            {
                Text = "XÉT NGHIỆM",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(20, 48),
                AutoSize = true
            };
            _clbTests = new CheckedListBox
            {
                Location = new Point(20, 72),
                Size = new Size(415, 340),
                CheckOnClick = true,
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular)
            };
            _clbTests.ItemCheck += (s, e) => BeginInvoke((Action)UpdateSummary);

            Label lblUltrasoundHeader = new Label
            {
                Text = "SIÊU ÂM / CHẨN ĐOÁN HÌNH ẢNH",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(455, 48),
                AutoSize = true
            };
            _clbUltrasounds = new CheckedListBox
            {
                Location = new Point(455, 72),
                Size = new Size(415, 340),
                CheckOnClick = true,
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular)
            };
            _clbUltrasounds.ItemCheck += (s, e) => BeginInvoke((Action)UpdateSummary);

            Label lblNote = new Label
            {
                Text = "Lý do chỉ định lâm sàng (tuỳ chọn — giúp KTV hiểu ngữ cảnh khi thực hiện):",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(20, 424),
                AutoSize = true
            };
            _txtClinicalNote = new MaterialTextBoxEdit
            {
                Location = new Point(20, 444),
                Size = new Size(850, 48),
                Hint = "Ví dụ: Nghi viêm gan B, cần xét nghiệm chức năng gan"
            };

            _chkUrgent = new CheckBox
            {
                Text = "🚨  Đánh dấu KHẨN (Cito) — ưu tiên xử lý trước trong hàng đợi KTV",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Location = new Point(20, 500),
                Size = new Size(550, 26),
                AutoSize = false
            };

            _lblSummary = new Label
            {
                Text = "Chưa chọn dịch vụ nào.",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(20, 532),
                Size = new Size(480, 24)
            };

            _btnConfirm = new RoundedButton
            {
                Text = "✅  Xác Nhận Chỉ Định",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129),
                HoverBackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                BorderRadius = 10,
                Size = new Size(220, 44),
                Location = new Point(650, 570),
                Cursor = Cursors.Hand
            };
            _btnConfirm.Click += OnConfirmClick;

            Button btnCancel = new Button
            {
                Text = "Hủy",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 44),
                Location = new Point(515, 570),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(lblTitle);
            Controls.Add(lblTestHeader);
            Controls.Add(_clbTests);
            Controls.Add(lblUltrasoundHeader);
            Controls.Add(_clbUltrasounds);
            Controls.Add(lblNote);
            Controls.Add(_txtClinicalNote);
            Controls.Add(_chkUrgent);
            Controls.Add(_lblSummary);
            Controls.Add(_btnConfirm);
            Controls.Add(btnCancel);
        }

        private async System.Threading.Tasks.Task LoadServicesAsync()
        {
            var api = new ApiService();
            _allServices = await api.GetClinicalServicesAsync();

            if (_allServices.Count == 0)
            {
                // Fallback tối thiểu nếu API lỗi/mất kết nối — khớp với update_hospital_services_and_prices.sql.
                // Giá tiền ở đây có thể LỆCH so với DB thật nếu đã thay đổi từ lúc migration này chạy — báo rõ
                // cho Bác sĩ biết đây là danh sách rút gọn dự phòng, không phải danh mục đầy đủ/giá mới nhất.
                _allServices = new List<ClinicalOrderServiceItem>
                {
                    new ClinicalOrderServiceItem { ServiceId = 4, ServiceName = "Xét nghiệm Công thức máu toàn bộ (CBC)", CategoryType = "Test", Price = 120000 },
                    new ClinicalOrderServiceItem { ServiceId = 5, ServiceName = "Xét nghiệm Đường huyết lúc đói (Glucose)", CategoryType = "Test", Price = 45000 },
                    new ClinicalOrderServiceItem { ServiceId = 10, ServiceName = "Siêu âm Bụng tổng quát", CategoryType = "Ultrasound", Price = 180000 },
                    new ClinicalOrderServiceItem { ServiceId = 13, ServiceName = "Siêu âm Tuyến giáp", CategoryType = "Ultrasound", Price = 150000 },
                };
                MessageBox.Show(
                    "Không tải được danh mục Xét nghiệm/Siêu âm đầy đủ từ máy chủ. Đang hiện danh sách rút gọn dự phòng (giá có thể không phải giá mới nhất). Vui lòng kiểm tra kết nối mạng và thử lại nếu cần đầy đủ danh mục.",
                    "Không tải được danh mục đầy đủ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _clbTests.Items.Clear();
            _clbUltrasounds.Items.Clear();
            foreach (var svc in _allServices.Where(s => s.CategoryType == "Test"))
                _clbTests.Items.Add(svc);
            foreach (var svc in _allServices.Where(s => s.CategoryType == "Ultrasound"))
                _clbUltrasounds.Items.Add(svc);
        }

        private void UpdateSummary()
        {
            int count = _clbTests.CheckedItems.Count + _clbUltrasounds.CheckedItems.Count;
            decimal total = _clbTests.CheckedItems.Cast<ClinicalOrderServiceItem>().Sum(s => s.Price)
                           + _clbUltrasounds.CheckedItems.Cast<ClinicalOrderServiceItem>().Sum(s => s.Price);
            _lblSummary.Text = count == 0
                ? "Chưa chọn dịch vụ nào."
                : $"Đã chọn {count} dịch vụ — Tạm tính: {total:N0}đ";
        }

        private async void OnConfirmClick(object sender, EventArgs e)
        {
            var selectedIds = _clbTests.CheckedItems.Cast<ClinicalOrderServiceItem>()
                .Concat(_clbUltrasounds.CheckedItems.Cast<ClinicalOrderServiceItem>())
                .Select(s => s.ServiceId)
                .ToList();

            if (selectedIds.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 dịch vụ Xét nghiệm hoặc Siêu âm để chỉ định.", "Chưa chọn dịch vụ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnConfirm.Enabled = false;
            var api = new ApiService();
            var (ok, message) = await api.CreateClinicalOrdersAsync(_appointmentId, _patientId, _doctorId, selectedIds, _chkUrgent.Checked, _txtClinicalNote.Text.Trim());
            _btnConfirm.Enabled = true;

            if (ok)
            {
                OrderedCount = selectedIds.Count;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(string.IsNullOrEmpty(message) ? "Không thể gửi chỉ định lên hệ thống. Vui lòng kiểm tra kết nối và thử lại." : message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Danh sách dịch vụ có thể đã đổi (ví dụ vừa chạy lại migration) — làm mới để chọn lại đúng ID hiện có.
                await LoadServicesAsync();
            }
        }
    }
}
