using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;
using Newtonsoft.Json;

namespace DTT.Doctor.UI.Forms
{
    public class PrintPrescriptionForm : Form
    {
        private readonly AppointmentModel _appointment;
        private readonly SaveClinicalRecordRequest _recordRequest;
        private Panel _pnlPaper;

        public PrintPrescriptionForm(AppointmentModel appointment, SaveClinicalRecordRequest recordRequest)
        {
            _appointment = appointment ?? new AppointmentModel();
            _recordRequest = recordRequest ?? new SaveClinicalRecordRequest();

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "DTT Healthcare - Xem Trái Mẫu In Đơn Thuốc";
            Size = new Size(880, 920);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = ClinicalColors.GhostWhite;

            Panel pnlTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White
            };
            Panel pnlBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ClinicalColors.BorderGray
            };
            pnlTopBar.Controls.Add(pnlBorder);

            Label lblTitle = new Label
            {
                Text = "XEM TRƯỚC MẪU IN ĐƠN THUỐC (CÓ MÃ QR CODE)",
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(24, 18),
                AutoSize = true
            };

            Button btnPrintNow = new Button
            {
                Text = "In Ngay",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(16, 185, 129),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 38),
                Location = new Point(590, 11),
                Cursor = Cursors.Hand
            };
            btnPrintNow.FlatAppearance.BorderSize = 0;
            btnPrintNow.Click += (s, e) => ExecutePrintDocument();

            Button btnClose = new Button
            {
                Text = "Đóng",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 38),
                Location = new Point(735, 11),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = ClinicalColors.BorderGray;
            btnClose.Click += (s, e) => Close();

            pnlTopBar.Controls.Add(lblTitle);
            pnlTopBar.Controls.Add(btnPrintNow);
            pnlTopBar.Controls.Add(btnClose);

            Panel pnlScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(35, 20, 35, 20),
                BackColor = ClinicalColors.GhostWhite
            };

            _pnlPaper = new Panel
            {
                Size = new Size(770, 780),
                BackColor = Color.White,
                Location = new Point(40, 15)
            };

            BuildPaperContent(_pnlPaper);
            pnlScroll.Controls.Add(_pnlPaper);

            Controls.Add(pnlScroll);
            Controls.Add(pnlTopBar);
        }

        private void BuildPaperContent(Panel paper)
        {
            Label lblHospitalName = new Label
            {
                Text = "DTT HEALTHCARE HOSPITAL",
                Font = ClinicalColors.GetMainFont(12f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(30, 24),
                AutoSize = true
            };
            Label lblHospitalSub = new Label
            {
                Text = "Tầng 4, Tòa nhà DTT Medical Tower  •  Hotline: 1900 6868  •  Website: dtt-healthcare.vn",
                Font = ClinicalColors.GetMainFont(8.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(30, 48),
                AutoSize = true
            };

            Label lblHeaderRight = new Label
            {
                Text = "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM" + Environment.NewLine + "Độc lập - Tự do - Hạnh phúc",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(460, 24),
                Size = new Size(280, 40),
                TextAlign = ContentAlignment.TopCenter
            };

            Panel line1 = new Panel { Location = new Point(30, 75), Size = new Size(710, 2), BackColor = ClinicalColors.PrimaryBlue };

            Label lblRxTitle = new Label
            {
                Text = "ĐƠN THUỐC BỆNH ÁN ĐIỆN TỬ",
                Font = ClinicalColors.GetMainFont(15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(30, 88),
                Size = new Size(710, 32),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblRxCode = new Label
            {
                Text = string.Format("Mã đơn: RX-2026-{0:D4}", _appointment.AppointmentId),
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(30, 118),
                Size = new Size(710, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            string patientNameStr = _appointment.PatientName != null ? _appointment.PatientName.ToUpper() : "BỆNH NHÂN";
            string ageStr = _appointment.PatientAge > 0 ? _appointment.PatientAge.ToString() : "35";
            string bpStr = !string.IsNullOrEmpty(_recordRequest.BloodPressure) ? _recordRequest.BloodPressure : "120/80 mmHg";
            string pulseStr = !string.IsNullOrEmpty(_recordRequest.Pulse) ? _recordRequest.Pulse : "82 bpm";
            string tempStr = !string.IsNullOrEmpty(_recordRequest.Temperature) ? _recordRequest.Temperature : "36.8°C";

            Label lblPatientInfo = new Label
            {
                Text = string.Format("Họ tên bệnh nhân : {0}      Giới tính: {1}      Tuổi: {2}" + Environment.NewLine + "Chuyên khoa khám : {3}      Phòng khám: {4}" + Environment.NewLine + "Chỉ số sinh hiệu : Huyết áp: {5}  |  Mạch: {6}  |  Thân nhiệt: {7}",
                                     patientNameStr, _appointment.PatientGender, ageStr, _appointment.SpecialtyName, _appointment.ClinicRoom, bpStr, pulseStr, tempStr),
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(30, 145),
                Size = new Size(710, 56)
            };

            string diagStr = !string.IsNullOrEmpty(_recordRequest.Diagnosis) ? _recordRequest.Diagnosis : "M17.9 - Thoái hóa khớp gối không xác định";
            string planStr = !string.IsNullOrEmpty(_recordRequest.TreatmentPlan) ? _recordRequest.TreatmentPlan : "Nghỉ ngơi nhiều, hạn chế mang vác nặng, tái khám sau 7 ngày.";

            Label lblDiagnosis = new Label
            {
                Text = string.Format("Chẩn đoán chính (ICD-10):  {0}" + Environment.NewLine + "Lời dặn bác sĩ điều trị  :  {1}", diagStr, planStr),
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 58, 138),
                Location = new Point(30, 208),
                Size = new Size(710, 42)
            };

            Panel line2 = new Panel { Location = new Point(30, 256), Size = new Size(710, 1), BackColor = ClinicalColors.BorderGray };

            AntiFlickerDataGridView gridPrint = new AntiFlickerDataGridView
            {
                Location = new Point(30, 266),
                Size = new Size(710, 310)
            };
            gridPrint.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", FillWeight = 15 });
            gridPrint.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Thuốc / Hoạt Chất", FillWeight = 85 });
            gridPrint.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số Lượng", FillWeight = 30 });
            gridPrint.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cách Dùng Chi Tiết", FillWeight = 110 });

            var items = _recordRequest.Prescriptions;
            if (items == null) items = new List<PrescribedDrugItem>();

            if (items.Count == 0)
            {
                gridPrint.Rows.Add(1, "Theo dõi điều trị / Không kê đơn thuốc", "-", "Tái khám theo lịch hẹn của Bác sĩ");
            }
            else
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    string unitStr = !string.IsNullOrEmpty(item.Unit) ? item.Unit : "Viên";
                    gridPrint.Rows.Add(i + 1, item.MedicineName, $"{item.Quantity} {unitStr}", item.UsageInstruction);
                }
            }

            Panel pnlBottom = new Panel
            {
                Location = new Point(30, 585),
                Size = new Size(710, 180)
            };

            var qrPayload = new
            {
                type = "DTT_PRESCRIPTION",
                prescriptionId = _appointment.AppointmentId,
                patientName = _appointment.PatientName,
                doctorName = !string.IsNullOrEmpty(TokenVault.FullName) ? TokenVault.FullName : "BS. CKII Nguyễn Văn A",
                date = DateTime.Now.ToString("yyyy-MM-dd")
            };
            string payloadJson = JsonConvert.SerializeObject(qrPayload);

            PictureBox picQr = new PictureBox
            {
                Size = new Size(130, 130),
                Location = new Point(10, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = QrCodePainter.GenerateQrBitmap(payloadJson, 140)
            };

            Label lblQrNote = new Label
            {
                Text = "Quét mã QR Code này bằng App Mobile" + Environment.NewLine + "DTT Patients để lưu tự động đơn thuốc!",
                Font = ClinicalColors.GetMainFont(8f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(10, 144),
                Size = new Size(240, 32)
            };

            Label lblSignDate = new Label
            {
                Text = string.Format("Ngày {0:D2} tháng {1:D2} năm {2}", DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year),
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(450, 10),
                Size = new Size(250, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblDoctorTitle = new Label
            {
                Text = "BÁC SĨ ĐIỀU TRỊ" + Environment.NewLine + "(Đã ký bằng Chữ ký Số điện tử)",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(450, 34),
                Size = new Size(250, 36),
                TextAlign = ContentAlignment.TopCenter
            };

            Label lblDoctorName = new Label
            {
                Text = !string.IsNullOrEmpty(TokenVault.FullName) ? TokenVault.FullName : "BS. CKII Nguyễn Văn A",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(450, 130),
                Size = new Size(250, 24),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlBottom.Controls.Add(picQr);
            pnlBottom.Controls.Add(lblQrNote);
            pnlBottom.Controls.Add(lblSignDate);
            pnlBottom.Controls.Add(lblDoctorTitle);
            pnlBottom.Controls.Add(lblDoctorName);

            paper.Controls.Add(lblHospitalName);
            paper.Controls.Add(lblHospitalSub);
            paper.Controls.Add(lblHeaderRight);
            paper.Controls.Add(line1);
            paper.Controls.Add(lblRxTitle);
            paper.Controls.Add(lblRxCode);
            paper.Controls.Add(lblPatientInfo);
            paper.Controls.Add(lblDiagnosis);
            paper.Controls.Add(line2);
            paper.Controls.Add(gridPrint);
            paper.Controls.Add(pnlBottom);
        }

        private void ExecutePrintDocument()
        {
            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrintPage += (s, ev) =>
                {
                    Bitmap bmp = new Bitmap(_pnlPaper.Width, _pnlPaper.Height);
                    _pnlPaper.DrawToBitmap(bmp, new Rectangle(0, 0, _pnlPaper.Width, _pnlPaper.Height));
                    ev.Graphics.DrawImage(bmp, 0, 0);
                };

                PrintPreviewDialog previewDlg = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 900,
                    Height = 700
                };
                previewDlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Không thể thực hiện in đơn thuốc: {0}", ex.Message), "Lỗi In Ấn", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
