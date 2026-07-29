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
using Panel = System.Windows.Forms.Panel;
using Button = System.Windows.Forms.Button;

namespace DTT.Doctor.UI.Forms
{
    public partial class ExaminationForm : Form
    {
        private readonly AppointmentModel _appointment;
        private readonly List<PrescribedDrugItem> _prescriptions = new List<PrescribedDrugItem>();

        private MaterialTextBoxEdit _txtPulse;
        private MaterialTextBoxEdit _txtBP;
        private MaterialTextBoxEdit _txtTemp;
        private MaterialTextBoxEdit _txtWeight;
        private MaterialTextBoxEdit _txtSymptoms;
        private MaterialTextBoxEdit _txtDiagnosis;
        private MaterialTextBoxEdit _txtTreatmentPlan;

        private ComboBox _cboDrugSelect;
        private MaterialTextBoxEdit _txtQuantity;
        private MaterialTextBoxEdit _txtDosageInstruction;
        private AntiFlickerDataGridView _gridPrescription;

        public bool IsSaved { get; private set; } = false;

        public ExaminationForm(AppointmentModel appointment)
        {
            _appointment = appointment ?? new AppointmentModel();
            InitializeComponent();
            LoadDefaultSampleData();
        }

        private void InitializeComponent()
        {
            Text = $"DTT Healthcare - Phiếu Khám Lâm Sàng: {_appointment.PatientName}";
            Size = new Size(1160, 790);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = ClinicalColors.GhostWhite;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);
            KeyPreview = true;

            // ── Top Header ───────────────────────────────────────────────────
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White
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
                Location = new Point(20, 12)
            };

            Label lblPatientTitle = new Label
            {
                Text = $"🩺  KHÁM LÂM SÀNG: {_appointment.PatientName.ToUpper()}",
                Font = ClinicalColors.GetMainFont(13f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(78, 12),
                Size = new Size(650, 26),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblPatientSub = new Label
            {
                Text = $"Mã ca: #{_appointment.AppointmentId}  •  Chuyên khoa: {_appointment.SpecialtyName}  •  Phòng: {_appointment.ClinicRoom}  •  Giờ hẹn: {_appointment.TimeSlot}",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(78, 38),
                Size = new Size(700, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlHeader.Controls.Add(avatar);
            pnlHeader.Controls.Add(lblPatientTitle);
            pnlHeader.Controls.Add(lblPatientSub);

            // ── Main Body Container (2 Columns) ───────────────────────────────
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 12)
            };

            // Left Panel: Vitals & Diagnosis
            AntiFlickerPanel pnlLeft = new AntiFlickerPanel
            {
                Size = new Size(510, 525),
                Location = new Point(16, 12),
                BackColor = Color.White,
                BorderRadius = 12,
                BorderColor = ClinicalColors.BorderGray,
                Padding = new Padding(16)
            };

            Label lblSec1 = new Label
            {
                Text = "1. CHỈ SỐ SINH HIỆU & KHÁM LÂM SÀNG",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(16, 12),
                AutoSize = true
            };

            _txtPulse = new MaterialTextBoxEdit { Location = new Point(16, 42), Size = new Size(225, 48), Hint = "Nhịp tim (bpm)" };
            _txtBP = new MaterialTextBoxEdit { Location = new Point(255, 42), Size = new Size(225, 48), Hint = "Huyết áp (mmHg)" };
            _txtTemp = new MaterialTextBoxEdit { Location = new Point(16, 100), Size = new Size(225, 48), Hint = "Thân nhiệt (°C)" };
            _txtWeight = new MaterialTextBoxEdit { Location = new Point(255, 100), Size = new Size(225, 48), Hint = "Cân nặng (kg)" };

            Label lblSec2 = new Label
            {
                Text = "2. CHẨN ĐOÁN & HƯỚNG ĐIỀU TRỊ",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(16, 165),
                AutoSize = true
            };

            _txtSymptoms = new MaterialTextBoxEdit
            {
                Location = new Point(16, 195),
                Size = new Size(464, 48),
                Hint = "Triệu chứng lâm sàng / Lý do khám"
            };

            _txtDiagnosis = new MaterialTextBoxEdit
            {
                Location = new Point(16, 260),
                Size = new Size(464, 48),
                Hint = "Chẩn đoán chính (Mã ICD-10)"
            };

            _txtTreatmentPlan = new MaterialTextBoxEdit
            {
                Location = new Point(16, 325),
                Size = new Size(464, 48),
                Hint = "Lời khuyên & Hướng điều trị"
            };

            pnlLeft.Controls.Add(lblSec1);
            pnlLeft.Controls.Add(_txtPulse);
            pnlLeft.Controls.Add(_txtBP);
            pnlLeft.Controls.Add(_txtTemp);
            pnlLeft.Controls.Add(_txtWeight);
            pnlLeft.Controls.Add(lblSec2);
            pnlLeft.Controls.Add(_txtSymptoms);
            pnlLeft.Controls.Add(_txtDiagnosis);
            pnlLeft.Controls.Add(_txtTreatmentPlan);

            // Right Panel: Prescription & Drugs
            AntiFlickerPanel pnlRight = new AntiFlickerPanel
            {
                Size = new Size(560, 525),
                Location = new Point(540, 12),
                BackColor = Color.White,
                BorderRadius = 12,
                BorderColor = ClinicalColors.BorderGray,
                Padding = new Padding(16)
            };

            Label lblSec3 = new Label
            {
                Text = "3. KÊ ĐƠN THUỐC ĐIỆN TỬ",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(16, 12),
                AutoSize = true
            };

            Label lblDrugLabel = new Label
            {
                Text = "Tên Thuốc & Nồng Độ:",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(16, 42),
                AutoSize = true
            };

            _cboDrugSelect = new ComboBox
            {
                Location = new Point(16, 62),
                Size = new Size(360, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular)
            };
            LoadMedicinesIntoCombo();

            _txtQuantity = new MaterialTextBoxEdit
            {
                Location = new Point(390, 44),
                Size = new Size(130, 48),
                Hint = "Số lượng",
                Text = "20"
            };

            _txtDosageInstruction = new MaterialTextBoxEdit
            {
                Location = new Point(16, 105),
                Size = new Size(360, 48),
                Hint = "Cách dùng & Liều lượng",
                Text = "Uống ngày 2 lần, mỗi lần 1 viên sau ăn"
            };

            RoundedButton btnAddDrug = new RoundedButton
            {
                Text = "➕  Thêm",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                BackColor = ClinicalColors.PrimaryBlue,
                ForeColor = Color.White,
                BorderRadius = 10,
                Size = new Size(130, 40),
                Location = new Point(390, 112),
                Cursor = Cursors.Hand
            };
            btnAddDrug.Click += OnAddDrugClick;

            _gridPrescription = new AntiFlickerDataGridView
            {
                Location = new Point(16, 168),
                Size = new Size(528, 340)
            };
            _gridPrescription.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", FillWeight = 25, MinimumWidth = 45 });
            _gridPrescription.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Thuốc", FillWeight = 110, MinimumWidth = 140 });
            _gridPrescription.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Số Lượng", FillWeight = 60, MinimumWidth = 90 });
            _gridPrescription.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cách Dùng & Chỉ Dẫn", FillWeight = 140, MinimumWidth = 180 });
            _gridPrescription.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thao Tác", Name = "ColDelete", FillWeight = 50, MinimumWidth = 80 });

            _gridPrescription.CellClick += (s, e) => {
                if (e.RowIndex >= 0 && e.RowIndex < _prescriptions.Count)
                {
                    if (e.ColumnIndex == 4 || (_gridPrescription.Columns.Contains("ColDelete") && e.ColumnIndex == _gridPrescription.Columns["ColDelete"].Index))
                    {
                        _prescriptions.RemoveAt(e.RowIndex);
                        RefreshPrescriptionGrid();
                    }
                    else if (e.ColumnIndex == 2) // Column Số Lượng
                    {
                        var menu = new ContextMenuStrip
                        {
                            ShowImageMargin = false,
                            Cursor = Cursors.Hand,
                            Renderer = new ModernDropdownRenderer(),
                            Padding = new Padding(4)
                        };

                        int[] quickQtys = new int[] { 1, 5, 10, 14, 20, 30, 60 };
                        int rowIndex = e.RowIndex;
                        string unit = _prescriptions[rowIndex].Unit;

                        foreach (int q in quickQtys)
                        {
                            int targetQty = q;
                            var item = new ToolStripMenuItem($"{targetQty} {unit}")
                            {
                                Font = ClinicalColors.GetMainFont(9.5f, targetQty == _prescriptions[rowIndex].Quantity ? FontStyle.Bold : FontStyle.Regular),
                                ForeColor = targetQty == _prescriptions[rowIndex].Quantity ? ClinicalColors.PrimaryBlue : ClinicalColors.TextDark,
                                Padding = new Padding(10, 6, 10, 6)
                            };
                            item.Click += (sm, ev) => {
                                _prescriptions[rowIndex].Quantity = targetQty;
                                RefreshPrescriptionGrid();
                            };
                            menu.Items.Add(item);
                        }

                        Rectangle displayRect = _gridPrescription.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                        Point menuPt = _gridPrescription.PointToScreen(new Point(displayRect.Left, displayRect.Bottom));
                        menu.Show(menuPt);
                    }
                    else if (e.ColumnIndex == 3) // Column Cách Dùng
                    {
                        var menu = new ContextMenuStrip
                        {
                            ShowImageMargin = false,
                            Cursor = Cursors.Hand,
                            Renderer = new ModernDropdownRenderer(),
                            Padding = new Padding(4)
                        };

                        string[] quickUsage = new string[] {
                            "Uống 1 viên sau ăn sáng, 1 viên sau ăn tối",
                            "Uống ngày 2 lần, mỗi lần 1 viên sau ăn",
                            "Uống 1 viên khi sốt cao > 38.5°C",
                            "Uống 1 viên trước khi đi ngủ",
                            "Pha 1 viên với 200ml nước ấm"
                        };
                        int rowIndex = e.RowIndex;

                        foreach (string uStr in quickUsage)
                        {
                            string targetUsage = uStr;
                            var item = new ToolStripMenuItem(targetUsage)
                            {
                                Font = ClinicalColors.GetMainFont(9.5f, targetUsage == _prescriptions[rowIndex].UsageInstruction ? FontStyle.Bold : FontStyle.Regular),
                                ForeColor = targetUsage == _prescriptions[rowIndex].UsageInstruction ? ClinicalColors.PrimaryBlue : ClinicalColors.TextDark,
                                Padding = new Padding(10, 6, 10, 6)
                            };
                            item.Click += (sm, ev) => {
                                _prescriptions[rowIndex].UsageInstruction = targetUsage;
                                RefreshPrescriptionGrid();
                            };
                            menu.Items.Add(item);
                        }

                        Rectangle displayRect = _gridPrescription.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                        Point menuPt = _gridPrescription.PointToScreen(new Point(displayRect.Left, displayRect.Bottom));
                        menu.Show(menuPt);
                    }
                }
            };

            pnlRight.Controls.Add(lblSec3);
            pnlRight.Controls.Add(lblDrugLabel);
            pnlRight.Controls.Add(_cboDrugSelect);
            pnlRight.Controls.Add(_txtQuantity);
            pnlRight.Controls.Add(_txtDosageInstruction);
            pnlRight.Controls.Add(btnAddDrug);
            pnlRight.Controls.Add(_gridPrescription);

            pnlBody.Controls.Add(pnlLeft);
            pnlBody.Controls.Add(pnlRight);

            // ── Bottom Action Footer ─────────────────────────────────────────
            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 65,
                BackColor = Color.White
            };
            Panel pnlFooterBorder = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = ClinicalColors.BorderGray
            };
            pnlFooter.Controls.Add(pnlFooterBorder);

            RoundedButton btnSave = new RoundedButton
            {
                Text = "💾   HOÀN TẤT & LƯU BỆNH ÁN",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129), // Emerald green
                HoverBackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                BorderRadius = 12,
                Size = new Size(260, 44),
                Location = new Point(540, 10),
                Cursor = Cursors.Hand
            };
            btnSave.Click += OnSaveClinicalRecordClick;

            RoundedButton btnPrint = new RoundedButton
            {
                Text = "🖨️   IN ĐƠN THUỐC QR",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                BackColor = ClinicalColors.PrimaryBlue,
                HoverBackColor = Color.FromArgb(29, 78, 216),
                ForeColor = Color.White,
                BorderRadius = 12,
                Size = new Size(180, 44),
                Location = new Point(812, 10),
                Cursor = Cursors.Hand
            };
            btnPrint.Click += (s, e) => {
                var req = BuildCurrentRecordRequest();
                using (var printForm = new PrintPrescriptionForm(_appointment, req))
                {
                    printForm.ShowDialog(this);
                }
            };

            Button btnCancel = new Button
            {
                Text = "Đóng",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(90, 44),
                Location = new Point(1005, 10),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = ClinicalColors.BorderGray;
            btnCancel.Click += (s, e) => this.Close();

            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(btnPrint);
            pnlFooter.Controls.Add(btnCancel);

            pnlHeader.Dock = DockStyle.Fill;
            pnlHeader.Margin = new Padding(0);

            pnlBody.Dock = DockStyle.Fill;
            pnlBody.Margin = new Padding(0);

            pnlFooter.Dock = DockStyle.Fill;
            pnlFooter.Margin = new Padding(0);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(0),
                Margin = new Padding(0),
                BackColor = ClinicalColors.GhostWhite
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));  // Row 0: Top Header (70px)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Row 1: Middle Body
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65f));  // Row 2: Bottom Footer (65px)

            mainLayout.Controls.Add(pnlHeader, 0, 0);
            mainLayout.Controls.Add(pnlBody, 0, 1);
            mainLayout.Controls.Add(pnlFooter, 0, 2);

            Controls.Add(mainLayout);
        }

        private async void LoadMedicinesIntoCombo()
        {
            try
            {
                var api = new ApiService();
                var list = await api.GetMedicinesAsync();
                _cboDrugSelect.Items.Clear();
                foreach (var m in list)
                {
                    _cboDrugSelect.Items.Add($"{m.MedicineName} ({m.Unit})");
                }
                if (_cboDrugSelect.Items.Count > 0) _cboDrugSelect.SelectedIndex = 0;
            }
            catch
            {
                _cboDrugSelect.Items.Clear();
                _cboDrugSelect.Items.Add("Amoxicillin 500mg (Viên)");
                _cboDrugSelect.Items.Add("Paracetamol 500mg (Viên)");
                _cboDrugSelect.Items.Add("Vitamin C 1000mg (Hộp)");
                _cboDrugSelect.SelectedIndex = 0;
            }
        }

        private void LoadDefaultSampleData()
        {
            // Add sample drug items matching PostgreSQL database
            _prescriptions.Add(new PrescribedDrugItem
            {
                MedicineId = 1,
                MedicineName = "Amoxicillin 500mg",
                Unit = "Viên",
                Quantity = 20,
                Dosage = "500mg",
                Frequency = "2 lần/ngày",
                UsageInstruction = "Uống 1 viên sau ăn sáng, 1 viên sau ăn tối"
            });
            _prescriptions.Add(new PrescribedDrugItem
            {
                MedicineId = 2,
                MedicineName = "Paracetamol 500mg",
                Unit = "Viên",
                Quantity = 10,
                Dosage = "500mg",
                Frequency = "Khi sốt",
                UsageInstruction = "Uống 1 viên khi sốt cao > 38.5°C"
            });
            RefreshPrescriptionGrid();
        }

        private void OnAddDrugClick(object sender, EventArgs e)
        {
            string selectedDrug = _cboDrugSelect.SelectedItem?.ToString() ?? "Paracetamol 500mg";
            int.TryParse(_txtQuantity.Text, out int qty);
            if (qty <= 0) qty = 10;
            string instruction = string.IsNullOrWhiteSpace(_txtDosageInstruction.Text) ? "Uống theo chỉ dẫn bác sĩ" : _txtDosageInstruction.Text;

            _prescriptions.Add(new PrescribedDrugItem
            {
                MedicineId = _prescriptions.Count + 1,
                MedicineName = selectedDrug,
                Unit = "Viên",
                Quantity = qty,
                Dosage = "Default",
                UsageInstruction = instruction
            });

            RefreshPrescriptionGrid();
        }

        private void RefreshPrescriptionGrid()
        {
            _gridPrescription.Rows.Clear();
            for (int i = 0; i < _prescriptions.Count; i++)
            {
                var item = _prescriptions[i];
                _gridPrescription.Rows.Add(i + 1, item.MedicineName, $"{item.Quantity} {item.Unit}", item.UsageInstruction, "❌ Xóa");
            }
        }

        private async void OnSaveClinicalRecordClick(object sender, EventArgs e)
        {
            var req = BuildCurrentRecordRequest();

            var api = new ApiService();
            await api.SaveClinicalRecordAsync(req);
            await api.UpdateAppointmentStatusAsync(_appointment.AppointmentId, "Completed");

            IsSaved = true;
            MessageBox.Show($"✅ ĐÃ LƯU BỆNH ÁN & HOÀN TẤT CA KHÁM!\n\nBệnh nhân: {_appointment.PatientName}\nChẩn đoán: {(!string.IsNullOrEmpty(_txtDiagnosis.Text) ? _txtDiagnosis.Text : "Khám sức khỏe")}\nSố loại thuốc đã kê: {_prescriptions.Count} thuốc\n\nApp Mobile của bệnh nhân đã tự động nhận được Bệnh án & Đơn Thuốc Điện Tử!", "Hoàn Tất Ca Khám Lâm Sàng", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private SaveClinicalRecordRequest BuildCurrentRecordRequest()
        {
            return new SaveClinicalRecordRequest
            {
                AppointmentId = _appointment.AppointmentId,
                PatientId = _appointment.PatientId,
                DoctorId = TokenVault.DoctorId > 0 ? TokenVault.DoctorId : 1,
                Pulse = _txtPulse.Text,
                BloodPressure = _txtBP.Text,
                Temperature = _txtTemp.Text,
                Weight = _txtWeight.Text,
                Symptoms = _txtSymptoms.Text,
                Diagnosis = _txtDiagnosis.Text,
                TreatmentPlan = _txtTreatmentPlan.Text,
                Prescriptions = _prescriptions
            };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                OnSaveClinicalRecordClick(this, EventArgs.Empty);
                return true;
            }
            if (keyData == (Keys.Control | Keys.P))
            {
                var req = BuildCurrentRecordRequest();
                using (var printForm = new PrintPrescriptionForm(_appointment, req))
                {
                    printForm.ShowDialog(this);
                }
                return true;
            }
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
