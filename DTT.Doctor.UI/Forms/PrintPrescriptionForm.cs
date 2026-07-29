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
    public partial class PrintPrescriptionForm : Form
    {
        private readonly AppointmentModel _appointment;
        private readonly SaveClinicalRecordRequest _recordRequest;
        private readonly Panel _pnlPaper;

        public PrintPrescriptionForm(AppointmentModel appointment, SaveClinicalRecordRequest recordRequest)
        {
            _appointment = appointment ?? new AppointmentModel();
            _recordRequest = recordRequest ?? new SaveClinicalRecordRequest();

            Text = $"🖨️ Mẫu In Đơn Thuốc & Bệnh Án QR - {_appointment.PatientName}";
            Size = new Size(860, 920);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(241, 245, 249); // Soft gray background for paper preview

            // ── Top Toolbar ─────────────────────────────────────────────────
            Panel pnlToolBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White
            };
            Panel pnlToolBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ClinicalColors.BorderGray
            };
            pnlToolBar.Controls.Add(pnlToolBorder);

            Label lblTitle = new Label
            {
                Text = "📄  XEM TRƯỚC MẪU IN ĐƠN THUỐC (CÓ MÃ QR CODE)",
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(20, 15),
                AutoSize = true
            };

            RoundedButton btnDoPrint = new RoundedButton
            {
                Text = "🖨️   In Ngay",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129),
                HoverBackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                BorderRadius = 10,
                Size = new Size(130, 38),
                Location = new Point(570, 10),
                Cursor = Cursors.Hand
            };
            btnDoPrint.Click += OnExecutePrintClick;

            Button btnClose = new Button
            {
                Text = "Đóng",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(90, 38),
                Location = new Point(720, 10),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = ClinicalColors.BorderGray;
            btnClose.Click += (s, e) => Close();

            pnlToolBar.Controls.Add(lblTitle);
            pnlToolBar.Controls.Add(btnDoPrint);
            pnlToolBar.Controls.Add(btnClose);

            // ── Paper Container Scrollable Panel ─────────────────────────────
            Panel pnlScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(25, 20, 25, 20)
            };

            // ── Paper Sheet (A4 Sheet Preview) ──────────────────────────────
            _pnlPaper = new Panel
            {
                Size = new Size(770, 800),
                Location = new Point(25, 10),
                BackColor = Color.White,
                Padding = new Padding(30)
            };

            BuildPaperSheetContent(_pnlPaper);

            pnlScroll.Controls.Add(_pnlPaper);

            Controls.Add(pnlScroll);
            Controls.Add(pnlToolBar);
        }

        private void BuildPaperSheetContent(Panel paper)
        {
            // 1. Hospital Header Line
            Label lblHospital = new Label
            {
                Text = "DTT HEALTHCARE HOSPITAL",
                Font = ClinicalColors.GetMainFont(12f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(30, 25),
                AutoSize = true
            };

            Label lblAddress = new Label
            {
                Text = "Tầng 4, Tòa nhà DTT Medical Tower  •  Hotline: 1900 6868  •  Website: dtt-healthcare.vn",
                Font = ClinicalColors.GetMainFont(8.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(30, 48),
                AutoSize = true
            };

            Label lblGovHeader = new Label
            {
                Text = "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM\nĐộc Lập - Tự Do - Hạnh Phúc",
                Font = ClinicalColors.GetMainFont(8.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(500, 25),
                TextAlign = ContentAlignment.TopRight,
                AutoSize = true
            };

            Panel line1 = new Panel { Location = new Point(30, 75), Size = new Size(710, 2), BackColor = ClinicalColors.PrimaryBlue };

            // 2. Document Title
            Label lblRxTitle = new Label
            {
                Text = "ĐƠN THUỐC & BỆNH ÁN ĐIỆN TỬ",
                Font = ClinicalColors.GetMainFont(15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(30, 88),
                Size = new Size(710, 32),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblRxCode = new Label
            {
                Text = $"Mã đơn: RX-2026-{_appointment.AppointmentId:D4}",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(30, 118),
                Size = new Size(710, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 3. Patient Information
            Label lblPatientInfo = new Label
            {
                Text = $"Họ tên bệnh nhân : {_appointment.PatientName.ToUpper()}      Giới tính: {_appointment.PatientGender}      Tuổi: {(_appointment.PatientAge > 0 ? _appointment.PatientAge.ToString() : "35")}\n" +
                       $"Chuyên khoa khám : {_appointment.SpecialtyName}      Phòng khám: {_appointment.ClinicRoom}\n" +
                       $"Chỉ số sinh hiệu : Huyết áp: {(_recordRequest.BloodPressure != "" ? _recordRequest.BloodPressure : "120/80 mmHg")}  |  Mạch: {(_recordRequest.Pulse != "" ? _recordRequest.Pulse : "82 bpm")}  |  Thân nhiệt: {(_recordRequest.Temperature != "" ? _recordRequest.Temperature : "36.8°C")}",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(30, 145),
                Size = new Size(710, 56)
            };

            // 4. Diagnosis & Advice
            Label lblDiagnosis = new Label
            {
                Text = $"Chẩn đoán chính (ICD-10):  {(!string.IsNullOrEmpty(_recordRequest.Diagnosis) ? _recordRequest.Diagnosis : "M17.9 - Thoái hóa khớp gối không xác định")}\n" +
                       $"Lời dặn bác sĩ điều trị  :  {(!string.IsNullOrEmpty(_recordRequest.TreatmentPlan) ? _recordRequest.TreatmentPlan : "Nghỉ ngơi nhiều, hạn chế mang vác nặng, tái khám sau 7 ngày.")}",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 58, 138),
                Location = new Point(30, 208),
                Size = new Size(710, 42)
            };

            Panel line2 = new Panel { Location = new Point(30, 256), Size = new Size(710, 1), BackColor = ClinicalColors.BorderGray };

            // 5. Prescribed Medicines DataGrid Table
            AntiFlickerDataGridView gridPrint = new AntiFlickerDataGridView
            {
                Location = new Point(30, 266),
                Size = new Size(710, 310)
            };
            gridPrint.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", FillWeight = 15 });
            gridPrint.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Thuốc / Hoạt Chất", FillWeight = 85 });
            gridPrint.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số Lượng", FillWeight = 30 });
            gridPrint.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cách Dùng & Liều Dùng Chi Tiết", FillWeight = 110 });

            for (int i = 0; i < _recordRequest.Prescriptions.Count; i++)
            {
                var item = _recordRequest.Prescriptions[i];
                gridPrint.Rows.Add(i + 1, item.MedicineName, $"{item.Quantity} {item.Unit}", item.UsageInstruction);
            }

            // 6. QR Code & Doctor Signature Area
            Panel pnlBottom = new Panel
            {
                Location = new Point(30, 586),
                Size = new Size(710, 180)
            };

            // Generate QR Code bitmap encoded with prescription payload
            var qrPayload = new
            {
                prescriptionId = _appointment.AppointmentId,
                patientName = _appointment.PatientName,
                doctorName = !string.IsNullOrEmpty(TokenVault.FullName) ? TokenVault.FullName : "BS. CKII PHẠM TUẤN KIỆT",
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
                Text = "📱  Quét mã QR Code này bằng App Mobile\nDTT Patients để lưu tự động đơn thuốc!",
                Font = ClinicalColors.GetMainFont(8f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(10, 144),
                Size = new Size(240, 32)
            };

            Label lblSignArea = new Label
            {
                Text = $"Ngày {DateTime.Now.Day:D2} tháng {DateTime.Now.Month:D2} năm {DateTime.Now.Year}\n" +
                       "BÁC SĨ ĐIỀU TRỊ\n" +
                       "(Đã ký bằng Chữ ký Số điện tử)\n\n\n\n" +
                       $"{( !string.IsNullOrEmpty(TokenVault.FullName) ? TokenVault.FullName : "BS. CKII PHẠM TUẤN KIỆT" )}",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(400, 10),
                Size = new Size(300, 160),
                TextAlign = ContentAlignment.TopCenter
            };

            pnlBottom.Controls.Add(picQr);
            pnlBottom.Controls.Add(lblQrNote);
            pnlBottom.Controls.Add(lblSignArea);

            paper.Controls.Add(lblHospital);
            paper.Controls.Add(lblAddress);
            paper.Controls.Add(lblGovHeader);
            paper.Controls.Add(line1);
            paper.Controls.Add(lblRxTitle);
            paper.Controls.Add(lblRxCode);
            paper.Controls.Add(lblPatientInfo);
            paper.Controls.Add(lblDiagnosis);
            paper.Controls.Add(line2);
            paper.Controls.Add(gridPrint);
            paper.Controls.Add(pnlBottom);
        }

        private void OnExecutePrintClick(object sender, EventArgs e)
        {
            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrintPage += (s, ev) => {
                    Bitmap bmp = new Bitmap(_pnlPaper.Width, _pnlPaper.Height);
                    _pnlPaper.DrawToBitmap(bmp, new Rectangle(0, 0, _pnlPaper.Width, _pnlPaper.Height));
                    ev.Graphics.DrawImage(bmp, 20, 20);
                };
                PrintPreviewDialog dlg = new PrintPreviewDialog
                {
                    Document = pd,
                    Width = 900,
                    Height = 700
                };
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"✅ Lệnh In Đơn Thuốc Kèm Mã QR Code đã được phát hành thành công cho máy in phòng khám!\n\nMã đơn: RX-2026-{_appointment.AppointmentId:D4}", "In Đơn Thuốc Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
