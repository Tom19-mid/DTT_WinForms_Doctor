using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;

namespace DTT.Doctor.UI.Forms
{
    public class DoctorScheduleForm : Form
    {
        private DateTimePicker _dtpDate;
        private AntiFlickerDataGridView _gridSchedule;
        private Label lblHeaderInfo;
        private Label lblShiftStatus;

        public DoctorScheduleForm()
        {
            InitializeComponent();
            LoadDoctorScheduleForDate(DateTime.Today);
        }

        private void InitializeComponent()
        {
            Text = $"DTT Healthcare - Lịch Làm Việc & Ca Trực Bác Sĩ: {TokenVault.FullName}";
            Size = new Size(1100, 750);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = ClinicalColors.GhostWhite;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

            // Top Header
            Panel pnlHeader = new Panel
            {
                Height = 80,
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            Panel pnlHeaderBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ClinicalColors.BorderGray
            };
            pnlHeader.Controls.Add(pnlHeaderBorder);

            AvatarBoxControl avatar = new AvatarBoxControl(46)
            {
                Location = new Point(20, 16)
            };

            Label lblTitle = new Label
            {
                Text = $"📅  LỊCH TRỰC CÁ NHÂN: {TokenVault.FullName.ToUpper()}",
                Font = ClinicalColors.GetMainFont(13f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(78, 14),
                Size = new Size(650, 26),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblHeaderInfo = new Label
            {
                Text = $"🔒 Quyền Hạn: [{TokenVault.RoleName}]  •  {TokenVault.ClinicRoom}  •  {TokenVault.SpecialtyName} (Chế độ Xem Read-Only)",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(78, 42),
                Size = new Size(750, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlHeader.Controls.Add(avatar);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblHeaderInfo);

            // Filter Bar
            Panel pnlFilter = new Panel
            {
                Height = 60,
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            Label lblDateSelect = new Label
            {
                Text = "Chọn ngày xem lịch:",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(20, 18),
                AutoSize = true
            };

            _dtpDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                Location = new Point(175, 14),
                Size = new Size(140, 30)
            };

            Label lblDayOfWeek = new Label
            {
                Text = $"({GetVietnameseDayName(DateTime.Today)})",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(322, 19),
                AutoSize = true
            };

            _dtpDate.ValueChanged += (s, e) =>
            {
                lblDayOfWeek.Text = $"({GetVietnameseDayName(_dtpDate.Value)})";
                LoadDoctorScheduleForDate(_dtpDate.Value);
            };

            lblShiftStatus = new Label
            {
                Text = "Trạng thái: Đang tải ca trực...",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(440, 18),
                Size = new Size(400, 26),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlFilter.Controls.Add(lblDateSelect);
            pnlFilter.Controls.Add(_dtpDate);
            pnlFilter.Controls.Add(lblDayOfWeek);
            pnlFilter.Controls.Add(lblShiftStatus);

            // DataGridView for slots
            _gridSchedule = new AntiFlickerDataGridView
            {
                Margin = new Padding(16)
            };
            _gridSchedule.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", FillWeight = 30, MinimumWidth = 50 });
            _gridSchedule.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "KHUNG GIỜ KHÁM", FillWeight = 120, MinimumWidth = 160 });
            _gridSchedule.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CA LÀM VIỆC", FillWeight = 100, MinimumWidth = 130 });
            _gridSchedule.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TRẠNG THÁI KHUNG GIỜ", FillWeight = 120, MinimumWidth = 160 });
            _gridSchedule.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "GHI CHÚ PHÂN LỊCH", FillWeight = 180, MinimumWidth = 220 });

            // Footer
            Panel pnlFooter = new Panel
            {
                Height = 60,
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            Label lblNotice = new Label
            {
                Text = "💡 Lịch trực được Phòng Quản Lý Nhân Sự phân bổ 30 ngày. Bác sĩ không thể tự sửa ca trực trên ứng dụng.",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Italic),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(20, 18),
                AutoSize = true
            };
            Button btnClose = new Button
            {
                Text = "Đóng",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 38),
                Location = new Point(960, 11),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = ClinicalColors.BorderGray;
            btnClose.Click += (s, e) => this.Close();

            pnlFooter.Controls.Add(lblNotice);
            pnlFooter.Controls.Add(btnClose);

            // TableLayoutPanel 4 rows
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                BackColor = ClinicalColors.GhostWhite
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));

            pnlHeader.Dock = DockStyle.Fill;
            pnlFilter.Dock = DockStyle.Fill;
            _gridSchedule.Dock = DockStyle.Fill;
            pnlFooter.Dock = DockStyle.Fill;

            mainLayout.Controls.Add(pnlHeader, 0, 0);
            mainLayout.Controls.Add(pnlFilter, 0, 1);
            mainLayout.Controls.Add(_gridSchedule, 0, 2);
            mainLayout.Controls.Add(pnlFooter, 0, 3);

            Controls.Add(mainLayout);
        }

        private async void LoadDoctorScheduleForDate(DateTime targetDate)
        {
            try
            {
                _gridSchedule.Rows.Clear();
                int docId = TokenVault.DoctorId > 0 ? TokenVault.DoctorId : 1;

                var api = new ApiService();
                var data = await api.GetDoctorSchedulesAsync(docId, targetDate.ToString("yyyy-MM-dd"));

                if (data != null && data.Count > 0)
                {
                    var first = data[0];
                    bool isWorking = (bool)(first.isWorking ?? false);
                    string statusText = (string)(first.statusText ?? "Nghỉ phép");

                    if (!isWorking)
                    {
                        lblShiftStatus.Text = "Trạng thái: NGÀY NGHỈ OFF (Nghỉ phép)";
                        lblShiftStatus.ForeColor = Color.FromArgb(239, 68, 68);

                        _gridSchedule.Rows.Add(1, "08:00 - 12:00", "Ca Sáng", "OFF (Nghỉ)", "Không có ca trực được phân công");
                        _gridSchedule.Rows.Add(2, "13:30 - 17:30", "Ca Chiều", "OFF (Nghỉ)", "Không có ca trực được phân công");
                        return;
                    }

                    lblShiftStatus.Text = $"Trạng thái: {statusText}";
                    lblShiftStatus.ForeColor = Color.FromArgb(16, 185, 129);

                    int stt = 1;
                    if (first.timeSlots != null)
                    {
                        foreach (var slot in first.timeSlots)
                        {
                            string time = slot.ToString();
                            string shiftName = time.StartsWith("08") || time.StartsWith("09") || time.StartsWith("10") || time.StartsWith("11") ? "Ca Sáng" : "Ca Chiều";
                            _gridSchedule.Rows.Add(stt++, time, shiftName, "Trống (Khả dụng)", "Sẵn sàng tiếp nhận bệnh nhân");
                        }
                    }
                    // Bác sĩ CÓ làm việc ngày này nhưng đã hết slot trống (đã kín lịch khám) — dữ liệu
                    // thật giờ có thể trả về timeSlots rỗng trong trường hợp này (trước đây generator giả
                    // luôn trả đúng 4 slot nên nhánh này chưa từng chạy tới). Không được hiện lại 2 dòng
                    // "08:00-12:00 / 13:30-17:30 Trống (Khả dụng)" giả — sẽ báo NHẦM là còn chỗ trống.
                    if (_gridSchedule.Rows.Count == 0)
                    {
                        _gridSchedule.Rows.Add(1, "—", "—", "Đã kín lịch", "Bác sĩ đã hết khung giờ trống trong ngày này.");
                    }
                    return;
                }

                // Không tải được lịch trực thật (API lỗi/mất kết nối/hết hạn đăng nhập) — trước đây
                // ở đây tự bịa ra một lịch trực giả bằng công thức xác định (dựa trên docId/ngày),
                // hiển thị y hệt dữ liệu thật (kể cả nhãn "Đã Có Đặt Lịch") không có cảnh báo gì, có
                // thể xảy ra ngay giữa phiên làm việc thật chỉ vì 1 lần gọi API lỗi. Giờ báo rõ ràng
                // là KHÔNG có dữ liệu, không tự bịa lịch trực.
                lblShiftStatus.Text = "Trạng thái: KHÔNG TẢI ĐƯỢC LỊCH TRỰC";
                lblShiftStatus.ForeColor = Color.FromArgb(239, 68, 68);
                _gridSchedule.Rows.Add(1, "—", "—", "Lỗi kết nối", "Không tải được lịch trực từ máy chủ. Vui lòng kiểm tra kết nối mạng/đăng nhập lại và thử lại.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi nạp lịch trực: " + ex.Message);
                lblShiftStatus.Text = "Trạng thái: KHÔNG TẢI ĐƯỢC LỊCH TRỰC";
                lblShiftStatus.ForeColor = Color.FromArgb(239, 68, 68);
            }
        }

        private static string GetVietnameseDayName(DateTime dt)
        {
            return dt.DayOfWeek switch
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
        }
    }
}
