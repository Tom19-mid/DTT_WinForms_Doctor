using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;
using Newtonsoft.Json;

namespace DTT.Doctor.UI.Forms
{
    public class MedicalHistoryForm : Form
    {
        private TextBox _txtSearch;
        private AntiFlickerDataGridView _gridHistory;
        private Label _lblStatus;

        private int _targetApptId = 0;
        private int _patientId = 0;
        private string _initialPatientName = "";

        public MedicalHistoryForm() : this("", 0, 0) { }

        public MedicalHistoryForm(string filterPatientName, int targetApptId = 0, int patientId = 0)
        {
            _targetApptId = targetApptId;
            _patientId = patientId;
            _initialPatientName = filterPatientName?.Trim() ?? "";
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(filterPatientName) && _txtSearch != null)
            {
                _txtSearch.Text = filterPatientName.Trim();
            }
            _ = LoadMedicalHistoryAsync();
        }

        private void InitializeComponent()
        {
            Text = "DTT Healthcare - Tra Cứu Hồ Sơ Bệnh Án Lâm Sàng";
            Size = new Size(1180, 780);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = ClinicalColors.GhostWhite;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

            // ── Header ───────────────────────────────────────────────────────
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
                Text = "LỊCH SỬ HỒ SƠ BỆNH ÁN LÂM SÀNG",
                Font = ClinicalColors.GetMainFont(13f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(78, 14),
                Size = new Size(600, 26),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblSubtitle = new Label
            {
                Text = "Tra cứu toàn bộ bệnh án cũ, đơn thuốc điện tử và kết quả khám lâm sàng bệnh nhân",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(78, 42),
                Size = new Size(700, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlHeader.Controls.Add(avatar);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // ── Filter Bar ───────────────────────────────────────────────────
            Panel pnlFilter = new Panel
            {
                Height = 60,
                BackColor = Color.White,
                Margin = Padding.Empty
            };

            Label lblSearch = new Label
            {
                Text = "Tìm kiếm:",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(20, 18),
                AutoSize = true
            };

            _txtSearch = new TextBox
            {
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                Location = new Point(95, 14),
                Size = new Size(320, 28),
                PlaceholderText = "Nhập Tên bệnh nhân, SĐT hoặc Mã ICD-10..."
            };
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) _ = LoadMedicalHistoryAsync(); };

            Button btnSearch = new Button
            {
                Text = "Tìm Kiếm",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ClinicalColors.PrimaryBlue,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 32),
                Location = new Point(425, 13),
                Cursor = Cursors.Hand
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += (s, e) => _ = LoadMedicalHistoryAsync();

            Button btnRefresh = new Button
            {
                Text = "Làm Mới",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                BackColor = Color.FromArgb(238, 242, 255),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 32),
                Location = new Point(543, 13),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(199, 210, 254);
            btnRefresh.Click += (s, e) => { ButtonFlashHelper.Flash(btnRefresh); _txtSearch.Text = ""; _ = LoadMedicalHistoryAsync(); };

            _lblStatus = new Label
            {
                Text = "Đang tải danh sách bệnh án...",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(660, 18),
                Size = new Size(480, 24),
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlFilter.Controls.Add(lblSearch);
            pnlFilter.Controls.Add(_txtSearch);
            pnlFilter.Controls.Add(btnSearch);
            pnlFilter.Controls.Add(btnRefresh);
            pnlFilter.Controls.Add(_lblStatus);

            // ── DataGridView ─────────────────────────────────────────────────
            _gridHistory = new AntiFlickerDataGridView
            {
                Margin = new Padding(16)
            };
            _gridHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", FillWeight = 30, MinimumWidth = 45 });
            _gridHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "NGÀY KHÁM", FillWeight = 90, MinimumWidth = 120 });
            _gridHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BỆNH NHÂN", FillWeight = 110, MinimumWidth = 140 });
            _gridHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SỐ ĐIỆN THOẠI", FillWeight = 85, MinimumWidth = 110 });
            _gridHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BÁC SĨ KHÁM", FillWeight = 110, MinimumWidth = 140 });
            _gridHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CHẨN ĐOÁN & ICD-10", FillWeight = 160, MinimumWidth = 180 });
            _gridHistory.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TOA THUỐC KÊ", FillWeight = 180, MinimumWidth = 200 });

            DataGridViewButtonColumn btnPrintCol = new DataGridViewButtonColumn
            {
                HeaderText = "IN THUỐC QR",
                Text = "In Toa QR",
                UseColumnTextForButtonValue = true,
                FillWeight = 85,
                MinimumWidth = 110
            };
            _gridHistory.Columns.Add(btnPrintCol);

            // Cột "THAO TÁC" — dùng đúng tên "ColAction"/chứa chữ "THAO TÁC" để tự động được
            // AntiFlickerDataGridView vẽ theo kiểu nút pill xanh có sẵn (xem CellPainting override).
            _gridHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "THAO TÁC",
                Name = "ColAction",
                FillWeight = 85,
                MinimumWidth = 100
            });

            _gridHistory.CellClick += OnGridCellClick;

            // ── Footer ───────────────────────────────────────────────────────
            Panel pnlFooter = new Panel
            {
                Height = 60,
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            Button btnClose = new Button
            {
                Text = "Đóng",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 38),
                Location = new Point(1040, 11),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = ClinicalColors.BorderGray;
            btnClose.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnClose);

            // Layout
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
            _gridHistory.Dock = DockStyle.Fill;
            pnlFooter.Dock = DockStyle.Fill;

            mainLayout.Controls.Add(pnlHeader, 0, 0);
            mainLayout.Controls.Add(pnlFilter, 0, 1);
            mainLayout.Controls.Add(_gridHistory, 0, 2);
            mainLayout.Controls.Add(pnlFooter, 0, 3);

            Controls.Add(mainLayout);
        }

        private async Task LoadMedicalHistoryAsync()
        {
            try
            {
                _lblStatus.Text = "⏳ Đang tải dữ liệu hồ sơ...";
                _gridHistory.Rows.Clear();

                var api = new ApiService();
                string search = _txtSearch.Text.Trim();
                string queryUrl = $"/api/MedicalRecords/all?search={Uri.EscapeDataString(search)}";
                if (_patientId > 0 && !string.IsNullOrWhiteSpace(search) && search.Equals(_initialPatientName, StringComparison.OrdinalIgnoreCase))
                {
                    queryUrl += $"&patientId={_patientId}";
                }
                else if (TokenVault.DoctorId > 0 && string.IsNullOrWhiteSpace(search))
                {
                    queryUrl += $"&doctorId={TokenVault.DoctorId}";
                }

                var json = await api.GetRawAsync(queryUrl);
                if (!string.IsNullOrEmpty(json))
                {
                    dynamic list = JsonConvert.DeserializeObject(json);
                    if (list != null)
                    {
                        int stt = 1;
                        foreach (var r in list)
                        {
                            string diag = (string)r.diagnosis;
                            string icd = (string)r.icdCode;
                            string diagFull = !string.IsNullOrEmpty(icd) ? $"[{icd}] {diag}" : diag;

                            int rowIdx = _gridHistory.Rows.Add(
                                stt++,
                                (string)r.examinationDate,
                                (string)r.patientName,
                                (string)r.phoneNumber,
                                (string)r.doctorName,
                                !string.IsNullOrEmpty(diagFull) ? diagFull : "—",
                                (string)r.prescriptionsSummary,
                                "", // Cột nút "In Toa QR" (DataGridViewButtonColumn tự vẽ, không cần gán Text ở đây)
                                "Xem Hồ Sơ"
                            );

                            _gridHistory.Rows[rowIdx].Tag = r;
                        }

                        if (_gridHistory.Rows.Count == 0)
                        {
                            _lblStatus.Text = string.IsNullOrWhiteSpace(search)
                                ? "Chưa có hồ sơ bệnh án nào."
                                : $"Không tìm thấy hồ sơ bệnh án nào của bệnh nhân \"{search}\".";
                        }
                        else
                        {
                            _lblStatus.Text = $"Đã tải thành công {_gridHistory.Rows.Count} hồ sơ bệnh án";
                        }

                        if (_targetApptId > 0 || !string.IsNullOrWhiteSpace(_txtSearch.Text))
                        {
                            DataGridViewRow targetRow = null;
                            bool matchedExactAppt = false;
                            if (_targetApptId > 0)
                            {
                                foreach (DataGridViewRow r in _gridHistory.Rows)
                                {
                                    if (r.Tag != null)
                                    {
                                        dynamic tag = r.Tag;
                                        int apptId = (int)(tag.appointmentId ?? 0);
                                        if (apptId == _targetApptId) { targetRow = r; matchedExactAppt = true; break; }
                                    }
                                }
                            }

                            if (targetRow == null && _gridHistory.Rows.Count > 0)
                                targetRow = _gridHistory.Rows[0];

                            if (targetRow != null)
                            {
                                targetRow.Selected = true;
                                _gridHistory.CurrentCell = targetRow.Cells[0];

                                if (matchedExactAppt && targetRow.Tag != null)
                                {
                                    var rowData = targetRow.Tag;
                                    BeginInvoke((Action)(() =>
                                    {
                                        using (var examForm = new PrintExamRecordForm(rowData))
                                        {
                                            examForm.ShowDialog(this);
                                        }
                                    }));
                                }
                            }
                        }
                        return;
                    }
                }

                _lblStatus.Text = "Chưa có hồ sơ bệnh án nào.";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Lỗi nạp hồ sơ: " + ex.Message;
            }
        }

        private void OnGridCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            // Check if Print button clicked (Column index 7)
            if (e.ColumnIndex == 7)
            {
                var rowTag = _gridHistory.Rows[e.RowIndex].Tag;
                if (rowTag == null) return;

                dynamic r = rowTag;

                var appt = new AppointmentModel
                {
                    AppointmentId = (int)(r.appointmentId ?? 0),
                    PatientId = (int)(r.patientId ?? 0),
                    PatientName = (string)(r.patientName ?? "Bệnh nhân"),
                    DoctorName = (string)(r.doctorName ?? TokenVault.FullName),
                    Date = (string)(r.examinationDate ?? DateTime.Now.ToString("dd/MM/yyyy")),
                    ClinicRoom = TokenVault.ClinicRoom
                };

                var presList = new List<PrescribedDrugItem>();
                string pSum = (string)r.prescriptionsSummary;
                if (!string.IsNullOrEmpty(pSum))
                {
                    // Backend trả prescriptionsSummary dạng "{Tên thuốc} ({Số lượng} {Đơn vị})" nối bằng ", "
                    // (xem MedicalRecordsController.cs GetAllMedicalRecords) — trước đây chỗ này bỏ qua phần
                    // số lượng/đơn vị thật và gán cứng "1 Liều" cho mọi thuốc khi in lại từ Lịch Sử Khám Bệnh,
                    // khiến đơn in ra sai lệch hoàn toàn so với đơn thật đã lưu (và đang hiện đúng bên App Mobile).
                    var parts = pSum.Split(',');
                    int drugIdx = 1;
                    foreach (var p in parts)
                    {
                        string itemStr = p.Trim();
                        if (string.IsNullOrEmpty(itemStr)) continue;

                        var match = System.Text.RegularExpressions.Regex.Match(itemStr, @"^(.*)\((\d+)\s+([^()]+)\)$");
                        string medName = itemStr;
                        int qty = 1;
                        string unit = "Liều";
                        if (match.Success)
                        {
                            medName = match.Groups[1].Value.Trim();
                            int.TryParse(match.Groups[2].Value, out qty);
                            if (qty <= 0) qty = 1;
                            unit = match.Groups[3].Value.Trim();
                        }

                        presList.Add(new PrescribedDrugItem
                        {
                            MedicineId = drugIdx++,
                            MedicineName = medName,
                            Quantity = qty,
                            Unit = unit,
                            UsageInstruction = "Theo chỉ dẫn của Bác sĩ điều trị"
                        });
                    }
                }

                var req = new SaveClinicalRecordRequest
                {
                    AppointmentId = appt.AppointmentId,
                    PatientId = appt.PatientId,
                    // Thiếu dữ liệu thật thì để RỖNG (form in sẽ hiện "—") — trước đây tự điền mạch 75, huyết áp 120/80,
                    // nhiệt độ 36.5, cân nặng 65, "Khám sức khỏe"... và DoctorId = 1 vào đơn thuốc in lại từ lịch sử.
                    DoctorId = TokenVault.DoctorId,
                    Pulse = (string)(r.pulse ?? ""),
                    BloodPressure = (string)(r.bloodPressure ?? ""),
                    Temperature = (string)(r.temperature ?? ""),
                    Weight = (string)(r.weight ?? ""),
                    Symptoms = (string)(r.symptoms ?? ""),
                    Diagnosis = (string)(r.diagnosis ?? ""),
                    TreatmentPlan = (string)(r.treatmentPlan ?? ""),
                    Prescriptions = presList
                };

                using (var printForm = new PrintPrescriptionForm(appt, req))
                {
                    printForm.ShowDialog(this);
                }
                return;
            }

            // Cột "THAO TÁC" (Column index 8) — mở Phiếu Khám của lượt khám này
            if (e.ColumnIndex == 8)
            {
                var rowTag = _gridHistory.Rows[e.RowIndex].Tag;
                if (rowTag == null) return;

                using (var examForm = new PrintExamRecordForm(rowTag))
                {
                    examForm.ShowDialog(this);
                }
            }
        }
    }
}
