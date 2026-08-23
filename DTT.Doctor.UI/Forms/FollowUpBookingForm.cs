using System;
using System.Drawing;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;

namespace DTT.Doctor.UI.Forms
{
    /// <summary>
    /// Dialog đặt lịch Tái khám thật cho bệnh nhân với CHÍNH bác sĩ đang đăng nhập —
    /// thay cho toast "đang phát triển" trước đây (mục "📞 Chuyển / Tái khám" trên hàng chờ).
    /// Chỉ hiện các khung giờ thật còn trống của bác sĩ (đọc từ GET /api/Doctors/schedules),
    /// rồi tạo lịch hẹn thật qua POST /api/Appointments.
    /// </summary>
    public class FollowUpBookingForm : Form
    {
        private readonly ApiService _api = new ApiService();
        private readonly int _patientId;
        private readonly string _patientName;
        private readonly int _doctorId;
        private readonly string _doctorName;
        private readonly string _specialtyName;

        private DateTimePicker _dtDate;
        private ComboBox _cboTimeSlot;
        private Label _lblSlotStatus;
        private TextBox _txtReason;
        private RoundedButton _btnConfirm;
        private RoundedButton _btnCancel;

        public FollowUpBookingForm(int patientId, string patientName, int doctorId, string doctorName, string specialtyName)
        {
            _patientId = patientId;
            _patientName = patientName;
            _doctorId = doctorId;
            _doctorName = doctorName;
            _specialtyName = specialtyName;
            InitializeComponent();
            _ = LoadTimeSlotsAsync();
        }

        private void InitializeComponent()
        {
            Text = "Đặt Lịch Tái Khám";
            Size = new Size(460, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ClinicalColors.GhostWhite;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.White };
            Panel pnlHeaderBorder = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = ClinicalColors.BorderGray };
            pnlHeader.Controls.Add(pnlHeaderBorder);
            Label lblTitle = new Label
            {
                Text = $"📞  Tái khám cho {_patientName.ToUpper()}",
                Font = ClinicalColors.GetMainFont(12.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(20, 12),
                Size = new Size(420, 26),
                UseMnemonic = false
            };
            Label lblSub = new Label
            {
                Text = $"Bác sĩ: {_doctorName}  •  {_specialtyName}",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(20, 38),
                Size = new Size(420, 20),
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            Panel pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 16) };

            Label MkLbl(string text, int yy) => new Label
            {
                Text = text,
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(0, yy),
                AutoSize = true
            };

            int y = 0;
            var lblDate = MkLbl("Ngày tái khám", y); y += 22;
            _dtDate = new DateTimePicker
            {
                Location = new Point(0, y),
                Size = new Size(400, 30),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dddd, dd/MM/yyyy",
                MinDate = DateTime.Today,
                Value = DateTime.Today.AddDays(7),
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular)
            };
            _dtDate.ValueChanged += async (s, e) => await LoadTimeSlotsAsync();
            y += 40;

            var lblSlot = MkLbl("Khung giờ trống của Bác sĩ", y); y += 22;
            _cboTimeSlot = new ComboBox
            {
                Location = new Point(0, y),
                Size = new Size(400, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular)
            };
            y += 34;
            _lblSlotStatus = new Label
            {
                Text = "Đang tải khung giờ trống...",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Italic),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(0, y),
                Size = new Size(400, 20),
                UseMnemonic = false
            };
            y += 32;

            var lblReason = MkLbl("Ghi chú / Lý do tái khám", y); y += 22;
            _txtReason = new TextBox
            {
                Location = new Point(0, y),
                Size = new Size(400, 60),
                Multiline = true,
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                Text = "Tái khám theo chỉ định của Bác sĩ"
            };

            pnlBody.Controls.Add(lblDate);
            pnlBody.Controls.Add(_dtDate);
            pnlBody.Controls.Add(lblSlot);
            pnlBody.Controls.Add(_cboTimeSlot);
            pnlBody.Controls.Add(_lblSlotStatus);
            pnlBody.Controls.Add(lblReason);
            pnlBody.Controls.Add(_txtReason);

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Color.White };
            Panel pnlFooterBorder = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = ClinicalColors.BorderGray };
            pnlFooter.Controls.Add(pnlFooterBorder);

            // Vị trí nút tính theo ClientSize của Form (đã biết trước = 460) — KHÔNG dùng pnlFooter.Width
            // vì panel chưa qua layout Dock=Bottom tại thời điểm này nên Width vẫn là giá trị mặc định.
            int footerRight = ClientSize.Width - 20;
            _btnConfirm = new RoundedButton
            {
                Text = "✅ Xác Nhận Đặt Lịch",
                Size = new Size(190, 38),
                Location = new Point(footerRight - 190, 13),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                NormalBackColor = ClinicalColors.PrimaryBlue,
                ForeColor = Color.White,
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                Enabled = false
            };
            _btnConfirm.Click += async (s, e) => await OnConfirmClickAsync();

            _btnCancel = new RoundedButton
            {
                Text = "Đóng",
                Size = new Size(110, 38),
                Location = new Point(footerRight - 190 - 10 - 110, 13),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                NormalBackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold)
            };
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlFooter.Controls.Add(_btnCancel);
            pnlFooter.Controls.Add(_btnConfirm);

            Controls.Add(pnlBody);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
        }

        private async System.Threading.Tasks.Task LoadTimeSlotsAsync()
        {
            _cboTimeSlot.Items.Clear();
            _cboTimeSlot.Enabled = false;
            _btnConfirm.Enabled = false;
            _lblSlotStatus.ForeColor = ClinicalColors.TextMuted;
            _lblSlotStatus.Text = "Đang tải khung giờ trống...";

            try
            {
                string dateStr = _dtDate.Value.ToString("yyyy-MM-dd");
                var data = await _api.GetDoctorSchedulesAsync(_doctorId, dateStr);

                if (data == null || data.Count == 0)
                {
                    _lblSlotStatus.ForeColor = Color.FromArgb(239, 68, 68);
                    _lblSlotStatus.Text = "Không tải được lịch làm việc. Vui lòng thử lại.";
                    return;
                }

                var first = data[0];
                bool isWorking = (bool)(first.isWorking ?? false);
                if (!isWorking)
                {
                    _lblSlotStatus.ForeColor = Color.FromArgb(239, 68, 68);
                    _lblSlotStatus.Text = "Bác sĩ không có lịch làm việc vào ngày này.";
                    return;
                }

                if (first.timeSlots != null)
                {
                    foreach (var slot in first.timeSlots)
                    {
                        _cboTimeSlot.Items.Add(slot.ToString());
                    }
                }

                if (_cboTimeSlot.Items.Count == 0)
                {
                    _lblSlotStatus.ForeColor = Color.FromArgb(245, 158, 11);
                    _lblSlotStatus.Text = "Bác sĩ đã kín lịch khám vào ngày này. Vui lòng chọn ngày khác.";
                    return;
                }

                _cboTimeSlot.Enabled = true;
                _cboTimeSlot.SelectedIndex = 0;
                _btnConfirm.Enabled = true;
                _lblSlotStatus.ForeColor = Color.FromArgb(16, 185, 129);
                _lblSlotStatus.Text = $"{_cboTimeSlot.Items.Count} khung giờ trống.";
            }
            catch (Exception ex)
            {
                _lblSlotStatus.ForeColor = Color.FromArgb(239, 68, 68);
                _lblSlotStatus.Text = "Lỗi khi tải khung giờ: " + ex.Message;
            }
        }

        private async System.Threading.Tasks.Task OnConfirmClickAsync()
        {
            if (_cboTimeSlot.SelectedItem == null) return;

            _btnConfirm.Enabled = false;
            _btnConfirm.Text = "Đang xử lý...";

            string dateStr = _dtDate.Value.ToString("yyyy-MM-dd");
            string timeSlot = _cboTimeSlot.SelectedItem.ToString();
            string reason = string.IsNullOrWhiteSpace(_txtReason.Text) ? "Tái khám" : _txtReason.Text.Trim();

            var (success, message) = await _api.CreateAppointmentAsync(_patientId, _doctorId, _doctorName, _specialtyName, dateStr, timeSlot, reason);

            if (success)
            {
                MessageBox.Show(message, "Đặt lịch tái khám thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(message, "Không thể đặt lịch tái khám", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _btnConfirm.Enabled = true;
                _btnConfirm.Text = "✅ Xác Nhận Đặt Lịch";
            }
        }
    }
}
