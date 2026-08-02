using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using DTT.Doctor.UI.Theme;

namespace DTT.Doctor.UI.Forms
{
    /// <summary>
    /// Xem lại Phiếu Khám (bệnh án) của 1 lượt khám cũ — bấm vào 1 dòng trong "Lịch Sử Hồ Sơ Bệnh Án"
    /// sẽ mở form này. Dữ liệu đến từ GET /api/MedicalRecords/all (dynamic JSON object).
    /// </summary>
    public class PrintExamRecordForm : Form
    {
        private readonly dynamic _record;
        private Panel _pnlPaper;

        public PrintExamRecordForm(dynamic record)
        {
            _record = record;
            InitializeComponent();
        }

        private static string S(dynamic value, string fallback = "")
        {
            try
            {
                string s = (string)value;
                return string.IsNullOrWhiteSpace(s) ? fallback : s;
            }
            catch { return fallback; }
        }

        private void InitializeComponent()
        {
            Text = "DTT Healthcare - Xem Phiếu Khám Bệnh";
            Size = new Size(880, 860);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = ClinicalColors.GhostWhite;

            Panel pnlTopBar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White };
            Panel pnlBorder = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = ClinicalColors.BorderGray };
            pnlTopBar.Controls.Add(pnlBorder);

            Label lblTitle = new Label
            {
                Text = "XEM LẠI PHIẾU KHÁM BỆNH",
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
                Size = new Size(770, 720),
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

            Label lblExamTitle = new Label
            {
                Text = "PHIẾU KHÁM BỆNH",
                Font = ClinicalColors.GetMainFont(15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(30, 88),
                Size = new Size(710, 32),
                TextAlign = ContentAlignment.MiddleCenter
            };

            string examDate = S(_record.examinationDate, DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            string recordCode = string.Format("Mã số: PK-{0}-{1:D2}", examDate.Replace("/", "").Replace(" ", "").Replace(":", ""), (int)(_record.medicalRecordId ?? 0));

            Label lblExamCode = new Label
            {
                Text = recordCode,
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(30, 118),
                Size = new Size(710, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            string patientName = S(_record.patientName, "Bệnh nhân").ToUpper();
            string phone = S(_record.phoneNumber, "—");
            string doctorName = S(_record.doctorName, TokenVaultFullNameOrDefault());

            Label lblPatientInfo = new Label
            {
                Text = string.Format("Họ tên bệnh nhân : {0}      SĐT: {1}" + Environment.NewLine + "Bác sĩ khám       : {2}      Ngày khám: {3}",
                                     patientName, phone, doctorName, examDate),
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(30, 145),
                Size = new Size(710, 40)
            };

            string bp = S(_record.bloodPressure, "—");
            string pulse = S(_record.pulse, "—");
            string temp = S(_record.temperature, "—");
            string weight = S(_record.weight, "—");
            string bmi = S(_record.bmi, "—");

            Label lblVitals = new Label
            {
                Text = string.Format("Chỉ số sinh hiệu  : Huyết áp: {0}  |  Mạch: {1} bpm  |  Thân nhiệt: {2}°C  |  Cân nặng: {3} kg  |  BMI: {4}",
                                     bp, pulse, temp, weight, bmi),
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(30, 190),
                Size = new Size(710, 22)
            };

            Panel line2 = new Panel { Location = new Point(30, 224), Size = new Size(710, 1), BackColor = ClinicalColors.BorderGray };

            string symptoms = S(_record.symptoms, "Khám định kỳ");
            string icdCode = S(_record.icdCode, "");
            string diagnosis = S(_record.diagnosis, "Khám sức khỏe tổng quát");
            string diagFull = !string.IsNullOrEmpty(icdCode) ? $"[{icdCode}] {diagnosis}" : diagnosis;
            string treatmentPlan = S(_record.treatmentPlan, "Nghỉ ngơi, theo dõi định kỳ.");

            Label lblSectionTitle = new Label
            {
                Text = "KẾT QUẢ KHÁM BỆNH:",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(30, 238),
                AutoSize = true
            };

            Label lblContent = new Label
            {
                Text = string.Format("- Triệu chứng lâm sàng / Lý do khám:{0}  {1}" + Environment.NewLine + Environment.NewLine +
                                     "- Chẩn đoán chính (ICD-10):{0}  {2}" + Environment.NewLine + Environment.NewLine +
                                     "- Hướng điều trị & Lời dặn:{0}  {3}",
                                     Environment.NewLine, symptoms, diagFull, treatmentPlan),
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(30, 268),
                Size = new Size(710, 220)
            };

            string prescriptionSummary = S(_record.prescriptionsSummary, "");
            Label lblRxSection = new Label
            {
                Text = "TOA THUỐC ĐIỆN TỬ ĐÃ KÊ:",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(30, 496),
                AutoSize = true
            };
            Label lblRxContent = new Label
            {
                Text = string.IsNullOrEmpty(prescriptionSummary) ? "Không kê đơn thuốc trong lượt khám này." : prescriptionSummary,
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(30, 522),
                Size = new Size(710, 60)
            };

            Panel pnlBottom = new Panel { Location = new Point(30, 600), Size = new Size(710, 100) };
            Label lblSignDate = new Label
            {
                Text = string.Format("Ngày {0:D2} tháng {1:D2} năm {2}", DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year),
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(460, 0),
                Size = new Size(250, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Label lblDoctorTitle = new Label
            {
                Text = "BÁC SĨ ĐIỀU TRỊ" + Environment.NewLine + "(Đã ký bằng Chữ ký Số điện tử)",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(460, 24),
                Size = new Size(250, 36),
                TextAlign = ContentAlignment.TopCenter
            };
            Label lblDoctorName = new Label
            {
                Text = doctorName,
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(460, 68),
                Size = new Size(250, 24),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlBottom.Controls.Add(lblSignDate);
            pnlBottom.Controls.Add(lblDoctorTitle);
            pnlBottom.Controls.Add(lblDoctorName);

            paper.Controls.Add(lblHospitalName);
            paper.Controls.Add(lblHospitalSub);
            paper.Controls.Add(lblHeaderRight);
            paper.Controls.Add(line1);
            paper.Controls.Add(lblExamTitle);
            paper.Controls.Add(lblExamCode);
            paper.Controls.Add(lblPatientInfo);
            paper.Controls.Add(lblVitals);
            paper.Controls.Add(line2);
            paper.Controls.Add(lblSectionTitle);
            paper.Controls.Add(lblContent);
            paper.Controls.Add(lblRxSection);
            paper.Controls.Add(lblRxContent);
            paper.Controls.Add(pnlBottom);
        }

        private static string TokenVaultFullNameOrDefault()
        {
            string name = DTT.Doctor.Services.Core.TokenVault.FullName;
            return string.IsNullOrEmpty(name) ? "BS. CKII Nguyễn Văn A" : name;
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
                MessageBox.Show(string.Format("Không thể in Phiếu Khám: {0}", ex.Message), "Lỗi In Ấn", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
