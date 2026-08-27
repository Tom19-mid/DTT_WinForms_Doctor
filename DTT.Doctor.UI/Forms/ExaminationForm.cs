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
        private ComboBox _cboIcdSuggest;
        private string _selectedIcdCode;
        private string _selectedIcdDescription;
        private MaterialTextBoxEdit _txtTreatmentPlan;

        private ComboBox _cboDrugSelect;
        private MaterialTextBoxEdit _txtQuantity;
        private MaterialTextBoxEdit _txtDosageInstruction;
        private AntiFlickerDataGridView _gridPrescription;

        private List<MedicineModel> _availableMedicines = new List<MedicineModel>();
        private Label _lblClsSummary;
        public bool IsSaved { get; private set; } = false;

        public ExaminationForm(AppointmentModel appointment)
        {
            _appointment = appointment ?? new AppointmentModel();
            InitializeComponent();
            PreFillNurseVitals(); // Auto-fill từ bộ sinh hiệu điều dưỡng đã đo
            _ = LoadIcdSuggestionsAsync();
            _ = LoadClsStatusAsync();

            // Panel 2 cột trái/phải (AntiFlickerPanel) bật cờ Windows WS_EX_COMPOSITED để chống nhấp
            // nháy — cờ này có thể khiến panel Footer (Hoàn tất & Lưu bệnh án/In đơn/Đóng), là control
            // anh em KHÔNG composited, không được vẽ ngay lần hiện đầu tiên cho tới khi có sự kiện vẽ
            // lại (kéo/resize cửa sổ...). Ép vẽ lại toàn bộ form ngay sau khi hiện xong để đảm bảo
            // Footer luôn hiển thị đúng ngay từ đầu.
            this.Shown += (s, e) => { this.Invalidate(true); this.Update(); };
        }

        // Tải trạng thái CLS THẬT từ server (không chỉ đếm trong phiên làm việc hiện tại) —
        // để Bác sĩ mở lại ca khám vẫn thấy đúng đã có kết quả Xét nghiệm/Siêu âm hay chưa.
        private async System.Threading.Tasks.Task LoadClsStatusAsync()
        {
            try
            {
                var api = new ApiService();
                var items = await api.GetClinicalOrdersByAppointmentAsync(_appointment.AppointmentId);
                if (items == null || items.Count == 0)
                {
                    _lblClsSummary.Text = "Chưa chỉ định CLS nào.";
                    _lblClsSummary.ForeColor = ClinicalColors.TextMuted;
                    return;
                }

                int pending = items.Count(i => i.Status == "Pending");
                int abnormal = items.Count(i => i.Status == "Abnormal");
                if (pending > 0)
                {
                    _lblClsSummary.Text = $"📋 {items.Count} chỉ định CLS — {items.Count - pending} đã có kết quả, {pending} đang chờ. (Bấm để xem)";
                    _lblClsSummary.ForeColor = Color.FromArgb(180, 83, 9);
                }
                else if (abnormal > 0)
                {
                    _lblClsSummary.Text = $"⚠️ {items.Count} chỉ định CLS đã có đủ kết quả — {abnormal} chỉ số BẤT THƯỜNG, vui lòng xem lại. (Bấm để xem)";
                    _lblClsSummary.ForeColor = Color.FromArgb(185, 28, 28);
                }
                else
                {
                    _lblClsSummary.Text = $"✅ {items.Count} chỉ định CLS đã có đủ kết quả, tất cả bình thường. (Bấm để xem)";
                    _lblClsSummary.ForeColor = Color.FromArgb(16, 185, 129);
                }
            }
            catch { }
        }

        // Tải danh mục ICD-10, ưu tiên hiện mã thuộc đúng chuyên khoa của bác sĩ đang đăng nhập lên đầu
        private async System.Threading.Tasks.Task LoadIcdSuggestionsAsync()
        {
            try
            {
                var api = new ApiService();
                var items = await api.GetIcd10CatalogAsync(TokenVault.SpecialtyId);
                if (items == null || items.Count == 0) return;

                _cboIcdSuggest.Items.Clear();
                foreach (var it in items) _cboIcdSuggest.Items.Add(it);

                var autoComplete = new AutoCompleteStringCollection();
                autoComplete.AddRange(items.Select(i => i.ToString()).ToArray());
                _cboIcdSuggest.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                _cboIcdSuggest.AutoCompleteSource = AutoCompleteSource.CustomSource;
                _cboIcdSuggest.AutoCompleteCustomSource = autoComplete;
            }
            catch { }
        }

        /// <summary>
        /// Đọc NurseNote (JSONB) từ appointment và điền sẵn vào các ô sinh hiệu.
        /// Bác sĩ vẫn có thể chỉnh sửa nếu cần.
        /// </summary>
        private void PreFillNurseVitals()
        {
            if (string.IsNullOrEmpty(_appointment.NurseNote)) return;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(_appointment.NurseNote);
                var root = doc.RootElement;

                // Nhịp tim
                if (root.TryGetProperty("heartRate", out var hr) && hr.GetInt32() > 0)
                    _txtPulse.Text = hr.GetInt32().ToString();

                // Huyết áp
                if (root.TryGetProperty("bloodPressure", out var bp) && !string.IsNullOrEmpty(bp.GetString()))
                    _txtBP.Text = bp.GetString()!;

                // Thân nhiệt
                if (root.TryGetProperty("temperature", out var tmp) && tmp.GetDouble() > 0)
                    _txtTemp.Text = tmp.GetDouble().ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

                // Cân nặng
                if (root.TryGetProperty("weight", out var wt) && wt.GetDouble() > 0)
                    _txtWeight.Text = wt.GetDouble().ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

                // Đổi màu ô để báo hiệu đã được điền từ ĐD
                foreach (var ctrl in new[] { _txtPulse, _txtBP, _txtTemp, _txtWeight })
                {
                    if (!string.IsNullOrEmpty(ctrl.Text))
                        ctrl.BackColor = Color.FromArgb(219, 234, 254); // light-blue badge
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PreFillNurseVitals error: " + ex.Message);
            }
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

            Label lblIcdHint = new Label
            {
                Text = "Gợi ý mã ICD-10 (ưu tiên đúng chuyên khoa của bạn) — gõ để tìm, chọn để tự điền:",
                Font = ClinicalColors.GetMainFont(8f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(16, 312),
                AutoSize = true
            };

            _cboIcdSuggest = new ComboBox
            {
                Location = new Point(16, 330),
                Size = new Size(464, 30),
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular)
            };
            _cboIcdSuggest.SelectedIndexChanged += (s, e) =>
            {
                if (_cboIcdSuggest.SelectedItem is Icd10Item item)
                {
                    _selectedIcdCode = item.IcdCode;
                    _selectedIcdDescription = item.DiseaseName;
                    _txtDiagnosis.Text = item.ToString();
                }
            };

            _txtTreatmentPlan = new MaterialTextBoxEdit
            {
                Location = new Point(16, 368),
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
            pnlLeft.Controls.Add(lblIcdHint);
            pnlLeft.Controls.Add(_cboIcdSuggest);
            pnlLeft.Controls.Add(_txtTreatmentPlan);

            // ── 4. Chỉ định Cận Lâm Sàng (Xét nghiệm / Siêu âm) ────────────────
            Label lblSec4 = new Label
            {
                Text = "4. CẬN LÂM SÀNG (XÉT NGHIỆM / SIÊU ÂM)",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(16, 424),
                AutoSize = true
            };

            RoundedButton btnOrderCls = new RoundedButton
            {
                Text = "🔬  Chỉ Định XN / SA",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(139, 92, 246), // Purple violet - đồng bộ màu WaitingForDoctor/AwaitingTestResults
                HoverBackColor = Color.FromArgb(124, 58, 237),
                ForeColor = Color.White,
                BorderRadius = 10,
                Size = new Size(170, 36),
                Location = new Point(16, 452),
                Cursor = Cursors.Hand
            };
            btnOrderCls.Click += OnOrderClsClick;

            _lblClsSummary = new Label
            {
                Text = "Đang tải trạng thái CLS...",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(196, 458),
                Size = new Size(284, 40),
                Cursor = Cursors.Hand
            };
            _lblClsSummary.Click += (s, e) => { using var f = new ClinicalResultsSummaryForm(_appointment.AppointmentId); f.ShowDialog(this); };

            pnlLeft.Controls.Add(lblSec4);
            pnlLeft.Controls.Add(btnOrderCls);
            pnlLeft.Controls.Add(_lblClsSummary);

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
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105), // Slate dark clear text (không mờ)
                BackColor = Color.FromArgb(241, 245, 249), // Soft button background
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 44),
                Location = new Point(1000, 10),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.Click += (s, e) => this.Close();

            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(btnPrint);
            pnlFooter.Controls.Add(btnCancel);

            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 70;

            pnlFooter.Dock = DockStyle.Bottom;
            pnlFooter.Height = 65;

            pnlBody.Dock = DockStyle.Fill;

            Controls.Add(pnlBody);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
        }

        private async void LoadMedicinesIntoCombo()
        {
            try
            {
                var api = new ApiService();
                var list = await api.GetMedicinesAsync();
                if (list != null && list.Count > 0)
                {
                    _availableMedicines = list;
                }
                else
                {
                    _availableMedicines = GetFallbackMedicines();
                }

                _cboDrugSelect.Items.Clear();
                foreach (var m in _availableMedicines)
                {
                    _cboDrugSelect.Items.Add(FormatDrugComboText(m));
                }

                _cboDrugSelect.SelectedIndexChanged -= OnDrugSelectedIndexChanged;
                _cboDrugSelect.SelectedIndexChanged += OnDrugSelectedIndexChanged;

                if (_cboDrugSelect.Items.Count > 0)
                {
                    _cboDrugSelect.SelectedIndex = 0;
                }
            }
            catch
            {
                _availableMedicines = GetFallbackMedicines();
                _cboDrugSelect.Items.Clear();
                foreach (var m in _availableMedicines)
                {
                    _cboDrugSelect.Items.Add(FormatDrugComboText(m));
                }

                _cboDrugSelect.SelectedIndexChanged -= OnDrugSelectedIndexChanged;
                _cboDrugSelect.SelectedIndexChanged += OnDrugSelectedIndexChanged;

                if (_cboDrugSelect.Items.Count > 0)
                {
                    _cboDrugSelect.SelectedIndex = 0;
                }
            }
        }

        private void OnDrugSelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = _cboDrugSelect.SelectedIndex;
            if (idx >= 0 && idx < _availableMedicines.Count)
            {
                var med = _availableMedicines[idx];
                if (!string.IsNullOrEmpty(med.DefaultUsage))
                {
                    _txtDosageInstruction.Text = med.DefaultUsage;
                }
            }
        }

        private List<MedicineModel> GetFallbackMedicines()
        {
            // StockQuantity = -1 nghĩa là "không rõ tồn kho" (danh sách dự phòng khi API lỗi, không
            // phải dữ liệu tồn kho thật) — KHÔNG được hiện "HẾT HÀNG"/chặn thêm thuốc dựa trên giá trị
            // mặc định 0 của các mục dự phòng này.
            return new List<MedicineModel>
            {
                new MedicineModel { MedicineId = 1, MedicineName = "Amoxicillin 500mg", Unit = "Viên", StockQuantity = -1, DefaultUsage = "Uống 1 viên/lần, 2 lần/ngày sau ăn sáng, tối" },
                new MedicineModel { MedicineId = 2, MedicineName = "Augmentin 1g", Unit = "Viên", StockQuantity = -1, DefaultUsage = "Uống 1 viên/lần, 2 lần/ngày sau ăn" },
                new MedicineModel { MedicineId = 6, MedicineName = "Paracetamol 500mg (Panadol Extra)", Unit = "Viên", StockQuantity = -1, DefaultUsage = "Uống 1-2 viên/lần khi sốt >38.5°C (cách 4-6h)" },
                new MedicineModel { MedicineId = 15, MedicineName = "Nexium mups 40mg", Unit = "Viên", StockQuantity = -1, DefaultUsage = "Uống 1 viên/lần/ngày trước ăn sáng 30 phút" },
                new MedicineModel { MedicineId = 25, MedicineName = "Vitamin C 1000mg", Unit = "Hộp", StockQuantity = -1, DefaultUsage = "Hòa 1 viên sủi vào 200ml nước uống mỗi sáng" }
            };
        }

        // "(Chai)" hoặc "(Chai) — CÒN 5" hoặc "(Chai) — HẾT HÀNG" tùy tồn kho thật từ API — trước đây
        // combo chỉ hiện tên/đơn vị, Bác sĩ không biết thuốc đã hết hàng cho tới lúc bấm Lưu bệnh án
        // (lúc đó tồn kho đã bị trừ về 0 một cách âm thầm ở phía Dược).
        private string FormatDrugComboText(MedicineModel m)
        {
            if (m.StockQuantity < 0) return $"{m.MedicineName} ({m.Unit})";
            if (m.StockQuantity <= 0) return $"{m.MedicineName} ({m.Unit}) — HẾT HÀNG";
            return $"{m.MedicineName} ({m.Unit}) — Còn {m.StockQuantity}";
        }

        private void OnAddDrugClick(object sender, EventArgs e)
        {
            int idx = _cboDrugSelect.SelectedIndex;
            MedicineModel selectedMed = (idx >= 0 && idx < _availableMedicines.Count) ? _availableMedicines[idx] : null;

            string drugName = selectedMed != null ? selectedMed.MedicineName : (_cboDrugSelect.SelectedItem?.ToString() ?? "Paracetamol 500mg");
            string unit = selectedMed != null ? selectedMed.Unit : "Viên";
            int medId = selectedMed != null ? selectedMed.MedicineId : (_prescriptions.Count + 1);

            int.TryParse(_txtQuantity.Text, out int qty);
            if (qty <= 0) qty = 10;

            // Chặn kê thuốc đã hết hàng (StockQuantity == 0, đọc thật từ API) — trước đây không kiểm
            // tra gì ở bước này, Bác sĩ chỉ biết thuốc hết hàng sau khi bấm "Hoàn tất & Lưu bệnh án".
            if (selectedMed != null && selectedMed.StockQuantity == 0)
            {
                MessageBox.Show(
                    $"Thuốc \"{drugName}\" hiện đã HẾT HÀNG tại Kho Dược. Vui lòng chọn thuốc thay thế hoặc liên hệ Dược sĩ để nhập thêm hàng trước khi kê đơn.",
                    "Thuốc hết hàng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (selectedMed != null && selectedMed.StockQuantity > 0 && qty > selectedMed.StockQuantity)
            {
                var proceed = MessageBox.Show(
                    $"Kho Dược chỉ còn {selectedMed.StockQuantity} {unit} \"{drugName}\", trong khi bạn đang kê {qty} {unit}.\n\nBạn có muốn tiếp tục kê số lượng này không? (Dược sĩ có thể không đủ hàng để cấp phát)",
                    "Số lượng vượt tồn kho", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (proceed != DialogResult.Yes) return;
            }

            string instruction = string.IsNullOrWhiteSpace(_txtDosageInstruction.Text) ? "Uống theo chỉ dẫn bác sĩ" : _txtDosageInstruction.Text;

            _prescriptions.Add(new PrescribedDrugItem
            {
                MedicineId = medId,
                MedicineName = drugName,
                Unit = unit,
                Quantity = qty,
                // [Old code]: Dosage = "Default",
                // [New code - Gán giá trị tiếng Việt chuẩn thay vì "Default"]:
                Dosage = "Theo chỉ định",
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

        // Mở dialog chọn Xét nghiệm/Siêu âm để chỉ định — bệnh nhân sẽ chuyển sang trạng thái
        // "Chờ kết quả CLS" và xuất hiện trong hàng đợi của Kỹ thuật viên (KTV Siêu âm/Xét nghiệm).
        private async void OnOrderClsClick(object sender, EventArgs e)
        {
            using (var picker = new ClinicalOrderPickerForm(_appointment.AppointmentId, _appointment.PatientId, TokenVault.DoctorId))
            {
                if (picker.ShowDialog(this) == DialogResult.OK && picker.OrderedCount > 0)
                {
                    await LoadClsStatusAsync();
                }
            }
        }

        private async void OnSaveClinicalRecordClick(object sender, EventArgs e)
        {
            var api = new ApiService();

            // Cảnh báo nếu ca khám này còn chỉ định Xét nghiệm/Siêu âm CHƯA có kết quả — tránh bấm
            // nhầm (nút "Hoàn Tất" hoặc phím tắt Ctrl+S) khiến ca khám kết thúc trước khi có kết quả CLS.
            var pendingOrders = await api.GetClinicalOrderQueueAsync(done: false);
            int stillPending = pendingOrders.Count(o => o.AppointmentId == _appointment.AppointmentId);
            if (stillPending > 0)
            {
                var confirm = MessageBox.Show(
                    $"Ca khám này còn {stillPending} chỉ định Xét nghiệm/Siêu âm CHƯA có kết quả.\n\nBạn có chắc muốn hoàn tất ca khám ngay bây giờ không?",
                    "Còn chỉ định CLS chưa hoàn tất", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;
            }

            var req = BuildCurrentRecordRequest();
            var (saveOk, insufficientStock) = await api.SaveClinicalRecordAsync(req);

            // KHÔNG được báo "Hoàn tất" / đóng form / đổi trạng thái appointment nếu hồ sơ THỰC SỰ
            // chưa lưu được (vd: phiên đăng nhập hết hạn, mất mạng) — trước đây luôn báo thành công
            // và đóng form bất kể saveOk, khiến bác sĩ tưởng đã lưu trong khi dữ liệu chưa vào DB.
            if (!saveOk)
            {
                MessageBox.Show(
                    "❌ LƯU HỒ SƠ THẤT BẠI!\n\nKhông kết nối được máy chủ (phiên đăng nhập có thể đã hết hạn). Hồ sơ khám bệnh và đơn thuốc CHƯA được ghi nhận.\n\nVui lòng đăng nhập lại và thử lưu lại.",
                    "Lưu Hồ Sơ Thất Bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // =========================================================================
            // [Old code - ghi đè thẳng sang Completed khiến ca khám không vào hàng đợi của Dược sĩ]:
            // await api.UpdateAppointmentStatusAsync(_appointment.AppointmentId, "Completed");
            // MessageBox.Show($"✅ HOÀN TẤT CA KHÁM VÀ LƯU HỒ SƠ Y TẾ!\n\n• Bệnh nhân: {_appointment.PatientName}\n• Chẩn đoán: {(!string.IsNullOrEmpty(_txtDiagnosis.Text) ? _txtDiagnosis.Text : "Khám sức khỏe")}\n• Số loại thuốc đã kê: {_prescriptions.Count} thuốc\n\nHồ sơ khám bệnh và đơn thuốc đã được ghi nhận thành công trên hệ thống.", "Hoàn Tất Ca Khám Lâm Sàng", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // =========================================================================

            // [New code - Backend tự gán StatusId=10 (PendingDispensing) nếu có đơn thuốc, thông báo rõ ràng chuyển Dược sĩ]:
            IsSaved = true;

            if (_prescriptions.Count > 0)
            {
                MessageBox.Show(
                    $"✅ HOÀN TẤT KHÁM & ĐÃ CHUYỂN ĐƠN THUỐC CHO DƯỢC SĨ!\n\n" +
                    $"• Bệnh nhân: {_appointment.PatientName}\n" +
                    $"• Chẩn đoán: {(!string.IsNullOrEmpty(_txtDiagnosis.Text) ? _txtDiagnosis.Text : "Khám sức khỏe")}\n" +
                    $"• Số loại thuốc đã kê: {_prescriptions.Count} loại thuốc\n\n" +
                    $"📋 Đơn thuốc đã được tự động chuyển sang Phân Hệ Dược Sĩ (Nhà Thuốc Bệnh Viện) để chuẩn bị và cấp phát thuốc cho người bệnh.",
                    "Đã Chuyển Đơn Sang Dược Sĩ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    $"✅ HOÀN TẤT CA KHÁM VÀ LƯU HỒ SƠ Y TẾ!\n\n" +
                    $"• Bệnh nhân: {_appointment.PatientName}\n" +
                    $"• Chẩn đoán: {(!string.IsNullOrEmpty(_txtDiagnosis.Text) ? _txtDiagnosis.Text : "Khám sức khỏe")}\n\n" +
                    $"Hồ sơ khám bệnh đã được ghi nhận thành công trên hệ thống.",
                    "Hoàn Tất Ca Khám Lâm Sàng", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            // Kho thuốc đã tự động trừ theo đơn vừa kê — nếu có thuốc không đủ hàng (đã trừ về 0),
            // cảnh báo riêng để Dược sĩ/Bác sĩ biết cần nhập thêm hàng.
            if (insufficientStock.Count > 0)
            {
                MessageBox.Show(
                    "⚠️ CÁC THUỐC SAU KHÔNG ĐỦ TỒN KHO (đã trừ về 0):\n\n" + string.Join("\n", insufficientStock) +
                    "\n\nVui lòng báo Nhà Thuốc Bệnh Viện nhập thêm hàng.",
                    "Cảnh Báo Hết Thuốc", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

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
                IcdCode = _selectedIcdCode,
                IcdDescription = _selectedIcdDescription,
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
