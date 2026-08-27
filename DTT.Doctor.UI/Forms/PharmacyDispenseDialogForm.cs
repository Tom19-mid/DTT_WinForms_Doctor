using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
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
    /// Cửa sổ chi tiết đơn thuốc và xác nhận phát thuốc của Dược sĩ.
    /// Hiển thị thông tin bệnh nhân, danh sách thuốc chi tiết, tình trạng tồn kho ("Khả dụng" / "Hết hàng"),
    /// và nút bấm Xác Nhận Phát Thuốc.
    /// </summary>
    public class PharmacyDispenseDialogForm : Form
    {
        private readonly ApiService _api = new ApiService();
        private readonly PharmacyQueueItem _item;

        private AntiFlickerDataGridView _gridDrugs;
        private TextBox _txtPharmacistNote;
        private RoundedButton _btnConfirmDispense;
        private RoundedButton _btnClose;
        private Label _lblStatus;

        private readonly bool _isHistoryView = false;

        public bool WasModified { get; private set; } = false;

        // [Old constructor]:
        // public PharmacyDispenseDialogForm(PharmacyQueueItem item)
        // {
        //     _item = item ?? new PharmacyQueueItem();
        //     InitializeComponent();
        //     LoadData();
        // }

        // [New constructors]:
        public PharmacyDispenseDialogForm(PharmacyQueueItem item) : this(item, false) { }

        public PharmacyDispenseDialogForm(PharmacyQueueItem item, bool isHistoryView)
        {
            _item = item ?? new PharmacyQueueItem();
            _isHistoryView = isHistoryView;
            InitializeComponent();
            if (_isHistoryView)
            {
                Text = $"Chi Tiết Đơn Thuốc Đã Cấp Phát — {_item.PatientName}";
                _txtPharmacistNote.ReadOnly = true;
                _btnConfirmDispense.Visible = false;
                _lblStatus.Text = "✓ Đơn thuốc đã được cấp phát hoàn tất";
                _lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
            }
            LoadData();
        }

        public PharmacyDispenseDialogForm(PharmacyHistoryItem history)
        {
            _isHistoryView = true;
            _item = new PharmacyQueueItem
            {
                AppointmentId = history.AppointmentId,
                MedicalRecordId = history.MedicalRecordId,
                PrescriptionId = history.PrescriptionId,
                PatientId = history.PatientId,
                PatientName = history.PatientName,
                PatientAge = history.PatientAge,
                PatientGender = history.PatientGender,
                DoctorName = history.DoctorName,
                DoctorDegree = history.DoctorDegree,
                Diagnosis = history.Diagnosis,
                // [Old code]: thiếu Symptoms
                // [New code - Gán đầy đủ Symptoms từ history để hiển thị trên thông tin bệnh nhân]:
                Symptoms = history.Symptoms,
                PrescriptionNote = history.PrescriptionNote,
                CreatedAt = history.DispensedAt,
                Items = history.Items ?? new List<PharmacyDrugItem>()
            };
            InitializeComponent();
            Text = $"Chi Tiết Đơn Thuốc Đã Cấp Phát — {_item.PatientName}";
            
            // [Old code]: _txtPharmacistNote.Text = history.Note;
            // [New code - Chỉ hiển thị nội dung ghi chú thực tế của Dược sĩ, loại bỏ tiền tố hoặc lời dặn của bác sĩ]:
            string pNote = history.Note ?? "";
            if (pNote.Contains("[") && pNote.Contains("]:"))
            {
                int colonIdx = pNote.IndexOf("]:");
                pNote = pNote.Substring(colonIdx + 2).Trim();
            }
            else if (pNote.StartsWith("[Đã phát bởi"))
            {
                pNote = "";
            }
            _txtPharmacistNote.Text = pNote;
            _txtPharmacistNote.ReadOnly = true;
            _btnConfirmDispense.Visible = false;
            _lblStatus.Text = $"✓ Đã phát lúc {history.DispensedAt:HH:mm dd/MM/yyyy} bởi {history.DispensedByName}";
            _lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = $"Chi Tiết Đơn Thuốc — {_item.PatientName}";
            // [Old size]: Size = new Size(880, 680); Size = new Size(960, 720); Size = new Size(820, 600); Size = new Size(980, 620);
            // [New size - Kích thước lớn 1400px x 800px, hỗ trợ phóng to toàn màn hình MaximizeBox = true]:
            Size = new Size(1400, 800);
            MinimumSize = new Size(1100, 650);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = ClinicalColors.GhostWhite;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

            // ── 1. Header Bar ───────────────────────────────────────────────
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(24, 12, 24, 12)
            };
            // [Old code - Thanh viền đáy header trước đây nằm đè lên chữ phụ đề]:
            // Panel pnlHeaderBorder = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = ClinicalColors.BorderGray };
            // pnlHeader.Controls.Add(pnlHeaderBorder);

            Label lblTitle = new Label
            {
                Text = $"ĐƠN THUỐC ĐIỆN TỬ — {_item.PatientName.ToUpperInvariant()}",
                Font = ClinicalColors.GetMainFont(12.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(24, 12),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = $"Mã ca khám: #{_item.AppointmentId}  •  Đơn thuốc ID: #{_item.PrescriptionId}  •  Thời gian kê: {_item.CreatedAt:HH:mm dd/MM/yyyy}",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(24, 40),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // =========================================================================
            // [Old code - khai báo pnlBody và pnlPatientInfo thừa cũ]:
            // Panel pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 14, 20, 10), AutoScroll = true };
            // Panel pnlPatientInfo = BuildPatientInfoCard();
            // pnlPatientInfo.Dock = DockStyle.Top;
            // =========================================================================

            // ── 2. Table Section ────────────────────────────────────────────
            Panel pnlTableSection = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 4, 20, 0),
                BackColor = ClinicalColors.GhostWhite
            };

            Label lblTableTitle = new Label
            {
                Text = "DANH SÁCH THUỐC CẦN CẤP PHÁT",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Dock = DockStyle.Top,
                Height = 28
            };

            _gridDrugs = new AntiFlickerDataGridView
            {
                Dock = DockStyle.Fill,
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
                RowTemplate = { Height = 36 },
                ColumnHeadersVisible = true,
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false
            };

            _gridDrugs.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            _gridDrugs.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
            _gridDrugs.ColumnHeadersDefaultCellStyle.Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold);

            // [New code - Cấu hình độ rộng tối ưu, hiển thị đầy đủ 10/10 cột và tiêu đề không bị co cụm chữ]:
            _gridDrugs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", Width = 50, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _gridDrugs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tên Thuốc & Hoạt Chất", FillWeight = 160, MinimumWidth = 160 });
            _gridDrugs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ĐVT", Width = 60, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _gridDrugs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SL", Width = 50, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _gridDrugs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Liều Dùng", Width = 115, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _gridDrugs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tần Suất", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _gridDrugs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Thời Gian", Width = 90, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _gridDrugs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hướng Dẫn Sử Dụng", FillWeight = 170, MinimumWidth = 160 });
            _gridDrugs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tồn Kho", Width = 80, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            _gridDrugs.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng Thái Kho", Width = 115, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });

            // [Fix]: Khóa tính năng Sort khi bấm vào tiêu đề STT/cột (giữ nguyên thứ tự 1, 2, 3 không bị xáo trộn di chuyển dòng)
            foreach (DataGridViewColumn col in _gridDrugs.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            pnlTableSection.Controls.Add(_gridDrugs);
            pnlTableSection.Controls.Add(lblTableTitle);
            // [Fix]: Đưa _gridDrugs lên Front để Dock.Fill tính toán chuẩn xác bên dưới lblTableTitle (không bị che khuất thanh Header STT, Tên Thuốc...)
            _gridDrugs.BringToFront();

            // ── 3. Note Container ───────────────────────────────────────────
            Panel pnlNoteContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 2, 20, 0),
                BackColor = ClinicalColors.GhostWhite
            };

            Label lblNoteTitle = new Label
            {
                Text = "Ghi chú của Dược sĩ:",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Dock = DockStyle.Top,
                Height = 20
            };

            _txtPharmacistNote = new TextBox
            {
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                Dock = DockStyle.Top,
                Height = 28
            };

            pnlNoteContainer.Controls.Add(_txtPharmacistNote);
            pnlNoteContainer.Controls.Add(lblNoteTitle);

            // ── 4. Patient Info Card ────────────────────────────────────────
            Panel pnlPatientInfo = BuildPatientInfoCard();
            pnlPatientInfo.Dock = DockStyle.Fill;
            pnlPatientInfo.Padding = new Padding(20, 6, 20, 0);

            // ── 5. Footer Bar ───────────────────────────────────────────────
            Panel pnlFooter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24, 8, 24, 8)
            };
            Panel pnlFooterBorder = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = ClinicalColors.BorderGray
            };
            pnlFooter.Controls.Add(pnlFooterBorder);

            _lblStatus = new Label
            {
                Text = "",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Italic),
                ForeColor = ClinicalColors.PrimaryBlue,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                Padding = new Padding(8, 0, 0, 0)
            };

            _btnClose = new RoundedButton
            {
                Text = "Đóng",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                BackColor = Color.FromArgb(241, 245, 249),
                HoverBackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                BorderRadius = 8,
                // [Old position]: Location = new Point(570, 11), Anchor = AnchorStyles.Top | AnchorStyles.Right
                Size = new Size(115, 42),
                Margin = new Padding(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            _btnClose.Click += (s, e) => { DialogResult = WasModified ? DialogResult.OK : DialogResult.Cancel; Close(); };

            _btnConfirmDispense = new RoundedButton
            {
                Text = "✅  XÁC NHẬN PHÁT THUỐC",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129),
                HoverBackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                BorderRadius = 8,
                // [Old position]: Location = new Point(695, 11), Anchor = AnchorStyles.Top | AnchorStyles.Right
                Size = new Size(240, 42),
                Margin = new Padding(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            _btnConfirmDispense.Click += async (s, e) => await OnConfirmDispenseClickAsync();

            // [New code - FlowLayoutPanel căn phải để nút luôn hiển thị đúng bất kể kích thước form]:
            FlowLayoutPanel flowButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 8, 12, 8)
            };
            flowButtons.Controls.Add(_btnClose);
            flowButtons.Controls.Add(_btnConfirmDispense);

            pnlFooter.Controls.Add(_lblStatus);
            pnlFooter.Controls.Add(flowButtons);

            // [New size - Rộng 1100px, Cao 720px để bảng rộng rãi, đáy form thấy trọn vẹn nút Đóng và Xác nhận]:
            Size = new Size(1100, 720);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ClinicalColors.GhostWhite;

            // [New code - Dùng TableLayoutPanel 5 hàng cố định: Header (70px), Patient (85px), Table (Fill), Note (60px), Footer (75px)]:
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                BackColor = ClinicalColors.GhostWhite
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f)); // Row 0: Header (70px thoáng đãng, không bị gạch ngang)
            // [Old code]: mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 85f));
            // [New code - Tăng lên 95px để hiển thị thêm dòng Triệu chứng lâm sàng / Lý do khám]:
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 95f)); // Row 1: Patient Card
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Row 2: Table
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f)); // Row 3: Note
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 75f)); // Row 4: Footer (75px giúp 2 nút Đóng & Xác nhận hiện trọn vẹn 100%)

            pnlHeader.Dock = DockStyle.Fill;
            pnlPatientInfo.Dock = DockStyle.Fill;
            pnlTableSection.Dock = DockStyle.Fill;
            pnlNoteContainer.Dock = DockStyle.Fill;
            pnlFooter.Dock = DockStyle.Fill;

            mainLayout.Controls.Add(pnlHeader, 0, 0);
            mainLayout.Controls.Add(pnlPatientInfo, 0, 1);
            mainLayout.Controls.Add(pnlTableSection, 0, 2);
            mainLayout.Controls.Add(pnlNoteContainer, 0, 3);
            mainLayout.Controls.Add(pnlFooter, 0, 4);

            Controls.Add(mainLayout);
        }

        private Panel BuildPatientInfoCard()
        {
            Panel card = new Panel
            {
                // [Old height]: Height = 85,
                // [New height]:
                Height = 95,
                BackColor = Color.FromArgb(240, 249, 255),
                Padding = new Padding(16, 8, 16, 8),
                Margin = new Padding(0, 0, 0, 6)
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(186, 230, 253), 1f))
                {
                    var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };

            string genderStr = _item.PatientGender == "Male" || _item.PatientGender == "Nam" ? "Nam ♂" : (_item.PatientGender == "Female" || _item.PatientGender == "Nữ" ? "Nữ ♀" : "—");
            string docName = !string.IsNullOrEmpty(_item.DoctorDegree) ? $"{_item.DoctorDegree} {_item.DoctorName}" : _item.DoctorName;
            string symptomsStr = !string.IsNullOrWhiteSpace(_item.Symptoms) ? _item.Symptoms : "—";

            // [Old code - Chưa có dòng Triệu chứng]:
            // Label lblCol1 = new Label { Text = $"Bệnh nhân:  {_item.PatientName}\nTuổi / Giới:  {_item.PatientAge} tuổi  •  {genderStr}", ... };
            // Label lblCol2 = new Label { Text = $"Bác sĩ kê đơn:  {docName}\nChẩn đoán:       {_item.Diagnosis}", ... };

            // [New code - Bổ sung thông tin Triệu chứng lâm sàng / Lý do khám]:
            Label lblCol1 = new Label
            {
                Text = $"Bệnh nhân:    {_item.PatientName}\nTuổi / Giới:    {_item.PatientAge} tuổi  •  {genderStr}\nTriệu chứng:  {symptomsStr}",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(16, 10),
                Size = new Size(360, 75)
            };

            Label lblCol2 = new Label
            {
                Text = $"Bác sĩ kê đơn:  {docName}\nChẩn đoán:       {_item.Diagnosis}",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(390, 10),
                Size = new Size(670, 75),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            card.Controls.Add(lblCol1);
            card.Controls.Add(lblCol2);
            return card;
        }

        private void LoadData()
        {
            _gridDrugs.Rows.Clear();
            if (_item.Items == null || _item.Items.Count == 0) return;

            for (int i = 0; i < _item.Items.Count; i++)
            {
                var drug = _item.Items[i];
                string stockStatus = drug.StockStatus ?? (drug.StockQuantity >= drug.Quantity ? "Khả dụng" : (drug.StockQuantity > 0 ? $"Còn {drug.StockQuantity}" : "Hết hàng"));
                
                // [Old code]: drug.Dosage
                // [New code - Hiển thị "Theo chỉ định" thay vì để chữ tiếng Anh "Default"]:
                string dosageDisplay = string.IsNullOrWhiteSpace(drug.Dosage) || drug.Dosage.Equals("Default", StringComparison.OrdinalIgnoreCase)
                    ? "Theo chỉ định"
                    : drug.Dosage;

                int idx = _gridDrugs.Rows.Add(
                    i + 1,
                    drug.MedicineName,
                    drug.Unit,
                    drug.Quantity,
                    dosageDisplay,
                    drug.Frequency,
                    drug.Duration,
                    drug.UsageInstruction ?? "—",
                    drug.StockQuantity.ToString(),
                    stockStatus
                );

                var row = _gridDrugs.Rows[idx];

                // Highlight warning if stock is out or low
                if (stockStatus == "Hết hàng" || drug.StockQuantity < drug.Quantity)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(185, 28, 28);
                    row.DefaultCellStyle.Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold);
                }
                else
                {
                    row.DefaultCellStyle.BackColor = i % 2 == 0 ? Color.White : Color.FromArgb(248, 250, 252);
                }
            }
        }

        private async Task OnConfirmDispenseClickAsync()
        {
            _btnConfirmDispense.Enabled = false;
            _lblStatus.Text = "Đang xử lý phát thuốc...";
            _lblStatus.ForeColor = ClinicalColors.PrimaryBlue;

            try
            {
                var (success, msg) = await _api.DispensePrescriptionAsync(_item.AppointmentId, _txtPharmacistNote.Text);
                if (success)
                {
                    WasModified = true;
                    // [Old code]: _lblStatus.Text = "✅ " + msg;
                    // [New code - Hiển thị thông báo tiếng Việt chuẩn không bị lỗi mã ký tự]:
                    _lblStatus.Text = "✅ Xác nhận phát thuốc thành công!";
                    _lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
                    MessageBox.Show(
                        $"Đã xác nhận phát thuốc thành công cho bệnh nhân {_item.PatientName}!\n\nHồ sơ khám bệnh đã hoàn tất.",
                        "Phát Thuốc Thành Công",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    _lblStatus.Text = "❌ " + msg;
                    _lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                    MessageBox.Show(msg, "Lỗi Phát Thuốc", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "❌ " + ex.Message;
                _lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnConfirmDispense.Enabled = true;
            }
        }
    }
}
