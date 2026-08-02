using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Drawing.Printing;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;
using Newtonsoft.Json;

namespace DTT.Doctor.UI.Forms
{
    public class ReceptionCashierForm : Form
    {
        private TabControl _tabControl;
        private AntiFlickerDataGridView _gridCheckIn;
        private AntiFlickerDataGridView _gridBilling;
        private AntiFlickerDataGridView _gridApproveMobile;
        private TextBox _txtSearchPatient;
        private TextBox _txtSearchBilling;
        private ComboBox _cboSpecialtyFilter;
        private const string SpecialtyFilterAll = "Tất cả chuyên khoa";

        // Reception Tab Controls & KPI Labels
        private Label _lblKpiTotal;
        private Label _lblKpiCheckedIn;
        private Label _lblKpiPending;
        private TabPage _tabCashier; // reference to update badge

        // Billing detail controls
        private Label _lblPatientDetail;
        private Label _lblFeeExam;
        private Label _lblFeeServices;
        private Label _lblFeeMeds;
        private Label _lblTotalAmount;
        private Button _btnConfirmPayment;
        private Button _btnPrintInvoice;

        // CCCD Verification Controls
        private TextBox _txtVerifyCccdInput;
        private Label _lblSelectedMobilePatient;
        private Button _btnExecuteCccdApprove;

        // Walk-in form controls
        private TextBox _txtWalkinName;
        private TextBox _txtWalkinPhone;
        private TextBox _txtWalkinCccd;
        private TextBox _txtWalkinBhyt;
        private TextBox _txtWalkinAddress;
        private DateTimePicker _dtpWalkinDob;
        private DateTimePicker _dtpWalkinExamDate;
        private ComboBox _cboWalkinGender;
        private ComboBox _cboWalkinSpecialty;

        // Store patient IDs for API calls
        private Dictionary<int, int> _patientRowIdMap = new Dictionary<int, int>();
        // Trạng thái THẬT (Confirmed/CheckedIn/InProgress/WaitingForDoctor/Completed/Cancelled/NoShow)
        // của từng dòng trên lưới Tiếp Đón — cột hiển thị "TRẠNG THÁI LỊCH HẸN" luôn ghi cứng
        // "Đã xác nhận" nên không dùng để biết ca đã khám xong hay chưa; dùng map này thay thế.
        private Dictionary<int, string> _checkInRowStatusMap = new Dictionary<int, string>();
        private Dictionary<int, int> _mobilePatientRowIdMap = new Dictionary<int, int>();
        private Dictionary<int, string> _mobileRecordTypeMap = new Dictionary<int, string>();
        // Store specialty info per combo index for walk-in registration
        private Dictionary<int, (int DoctorId, string SpecialtyName)> _walkinSpecialtyMap = new Dictionary<int, (int, string)>();

        // Tab 5 — Khám Trực Tiếp (bệnh nhân đã có hồ sơ: tạo qua Lễ Tân / chưa xác thực / đã xác thực qua App)
        private TextBox _txtDirectSearchPhone;
        private Panel _pnlDirectResult;
        private Label _lblDirectName;
        private Label _lblDirectStatusBadge;
        private Label _lblDirectInfo;
        private Label _lblDirectWarning;
        private Panel _pnlDirectBookingBox;
        private TextBox _txtDirectCccd;
        private ComboBox _cboDirectSpecialty;
        private Button _btnDirectBookNow;
        private Dictionary<int, (int DoctorId, string SpecialtyName)> _directSpecialtyMap = new Dictionary<int, (int, string)>();
        private PatientSimpleModel _directFoundPatient;
        private AntiFlickerDataGridView _gridDirectPatients;
        private List<PatientSimpleModel> _allDirectPatients = new List<PatientSimpleModel>();

        public ReceptionCashierForm()
        {
            InitializeComponent();
            this.Shown += async (s, e) => await LoadDataPublicAsync();
            this.VisibleChanged += async (s, e) => { if (this.Visible) await LoadDataPublicAsync(); };
        }

        public void SelectTab(int index)
        {
            if (_tabControl != null && index >= 0 && index < _tabControl.TabPages.Count)
            {
                _tabControl.SelectedIndex = index;
            }
        }

        private void InitializeComponent()
        {
            Text = "DTT Healthcare - Phân Hệ Lễ Tân Tiếp Đón, Đối Chiếu CCCD & Bàn Thu Ngân Viện Phí";
            Size = new Size(1280, 820);
            StartPosition = FormStartPosition.CenterScreen;
            MinimizeBox = true;
            MaximizeBox = true;
            BackColor = ClinicalColors.GhostWhite;

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White,
                Name = "pnlHeader"
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

            Label lblTitle = new Label
            {
                Text = "[*]  PHÂN HỆ LỄ TÂN TIẾP ĐÓN, ĐỐI CHIẾU CCCD & THU NGÂN VIỆN PHÍ",
                Font = ClinicalColors.GetMainFont(12.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(78, 12),
                Size = new Size(800, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblHeaderInfo = new Label
            {
                Text = string.Format("[*] Nhân viên: {0} [{1}]  •  Bàn Tiếp Đón & Quầy Thu Ngân #01  •  Hệ thống Bệnh viện Điện tử DTT Healthcare",
                                     !string.IsNullOrEmpty(TokenVault.FullName) ? TokenVault.FullName : "Nguyễn Thị Minh Châu",
                                     !string.IsNullOrEmpty(TokenVault.RoleName) ? TokenVault.RoleName : "Lễ tân tiếp đón"),
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(78, 38),
                Size = new Size(850, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Button btnLogoutHeader = new Button
            {
                Text = "[*] Đăng Xuất",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 38, 38),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 36),
                Location = new Point(1120, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnLogoutHeader.FlatAppearance.BorderSize = 0;
            btnLogoutHeader.Click += (s, e) =>
            {
                TokenVault.Clear();
                this.Hide();
                new LoginForm().Show();
            };

            pnlHeader.Controls.Add(avatar);
            pnlHeader.Visible = false; // Hủy bỏ header trùng lặp vì MainDashboardForm đã có Header bar & Sidebar

            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                Appearance = TabAppearance.FlatButtons,
                ItemSize = new Size(0, 1),
                SizeMode = TabSizeMode.Fixed
            };

            TabPage tabReception = new TabPage("[*] 1. TIẾP ĐÓN & CHECK-IN STT") { BackColor = ClinicalColors.GhostWhite };
            TabPage tabCashier = new TabPage("[*] 2. BÀN THU NGÂN & IN HÓA ĐƠN") { BackColor = ClinicalColors.GhostWhite };
            TabPage tabApprove = new TabPage("[*] 3. ĐỐI CHIẾU CCCD & DUYỆT APP MOBILE") { BackColor = ClinicalColors.GhostWhite };
            TabPage tabWalkIn = new TabPage("➕ 4. TẠO HỒ SƠ KHÁCH VÃNG LAI") { BackColor = ClinicalColors.GhostWhite };
            TabPage tabDirect = new TabPage("🏥 5. KHÁM TRỰC TIẾP CHO HỒ SƠ CÓ SẴN") { BackColor = ClinicalColors.GhostWhite };

            BuildReceptionTab(tabReception);
            BuildCashierTab(tabCashier);
            BuildApproveTab(tabApprove);
            BuildWalkInTab(tabWalkIn);
            BuildDirectExamTab(tabDirect);
            _tabCashier = tabCashier; // store reference for badge updates

            _tabControl.TabPages.Add(tabReception);
            _tabControl.TabPages.Add(tabCashier);
            _tabControl.TabPages.Add(tabApprove);
            _tabControl.TabPages.Add(tabWalkIn);
            _tabControl.TabPages.Add(tabDirect);

            Controls.Add(_tabControl);
        }

        private void BuildReceptionTab(TabPage tab)
        {
            // --- 1. Top KPI Summary Cards Panel (Doctor UI Card Style) ---
            Panel pnlKpi = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = ClinicalColors.GhostWhite,
                Padding = new Padding(15, 8, 15, 8)
            };

            Panel cardTotal = CreateKpiCard("TỔNG LỊCH HẸN HÔM NAY", "0", Color.FromArgb(37, 99, 235), out _lblKpiTotal);
            cardTotal.Location = new Point(15, 8);
            cardTotal.Size = new Size(290, 56);

            Panel cardCheckedIn = CreateKpiCard("ĐÃ CHECK-IN (TẠI QUẦY)", "0", Color.FromArgb(16, 185, 129), out _lblKpiCheckedIn);
            cardCheckedIn.Location = new Point(320, 8);
            cardCheckedIn.Size = new Size(290, 56);

            Panel cardPending = CreateKpiCard("CHỜ CHECK-IN TẠI QUẦY", "0", Color.FromArgb(245, 158, 11), out _lblKpiPending);
            cardPending.Location = new Point(625, 8);
            cardPending.Size = new Size(290, 56);

            pnlKpi.Controls.Add(cardTotal);
            pnlKpi.Controls.Add(cardCheckedIn);
            pnlKpi.Controls.Add(cardPending);

            // --- 2. Toolbar & Real-time Search Panel ---
            Panel pnlSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.White,
                Padding = new Padding(15, 10, 15, 10)
            };

            Label lblSearch = new Label
            {
                Text = "Tìm bệnh nhân / SĐT / Mã lịch hẹn:",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(15, 16),
                AutoSize = true,
                UseMnemonic = false
            };

            _txtSearchPatient = new TextBox
            {
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular),
                Location = new Point(275, 12),
                Size = new Size(280, 30)
            };
            _txtSearchPatient.TextChanged += (s, e) => FilterReceptionGrid();

            Label lblSpecFilter = new Label
            {
                Text = "Chuyên khoa:",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(570, 16),
                AutoSize = true,
                UseMnemonic = false
            };

            _cboSpecialtyFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                Location = new Point(685, 12),
                Size = new Size(200, 30),
                DropDownWidth = 240,
                IntegralHeight = false,
                MaxDropDownItems = 10
            };
            _cboSpecialtyFilter.Items.Add(SpecialtyFilterAll);
            _cboSpecialtyFilter.SelectedIndex = 0;
            _cboSpecialtyFilter.SelectedIndexChanged += (s, e) => FilterReceptionGrid();

            Button btnReload = new Button
            {
                Text = "Tải lại dữ liệu",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(125, 32),
                Location = new Point(895, 11),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnReload.FlatAppearance.BorderSize = 1;
            btnReload.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnReload.Click += async (s, e) => await LoadDataPublicAsync();

            Button btnCheckInAction = new Button
            {
                Text = " Check-in",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(16, 185, 129),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(125, 32),
                Location = new Point(1030, 11),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnCheckInAction.FlatAppearance.BorderSize = 0;
            btnCheckInAction.Click += (s, e) => ExecuteCheckInSelected();

            pnlSearch.Controls.Add(lblSearch);
            pnlSearch.Controls.Add(_txtSearchPatient);
            pnlSearch.Controls.Add(lblSpecFilter);
            pnlSearch.Controls.Add(_cboSpecialtyFilter);
            pnlSearch.Controls.Add(btnReload);
            pnlSearch.Controls.Add(btnCheckInAction);

            // --- 3. DataGridView Grid ---
            _gridCheckIn = new AntiFlickerDataGridView { Dock = DockStyle.Fill };
            _gridCheckIn.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", FillWeight = 25 });
            _gridCheckIn.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "MÃ LỊCH HẸN", FillWeight = 45 });
            _gridCheckIn.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "HỌ VÀ TÊN BỆNH NHÂN", FillWeight = 85 });
            _gridCheckIn.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CHUYÊN KHOA ĐẶT HẸN", FillWeight = 80 });
            _gridCheckIn.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "GIỜ HẸN KHÁM", FillWeight = 40 });
            _gridCheckIn.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TRẠNG THÁI LỊCH HẸN", FillWeight = 60 });
            _gridCheckIn.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TRẠNG THÁI CHECK-IN", FillWeight = 75 });
            _gridCheckIn.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "THAO TÁC TRỰC TIẾP", FillWeight = 65 });
            _gridCheckIn.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "HỦY / BỎ KHÁM", FillWeight = 55 });

            _gridCheckIn.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _gridCheckIn.Rows.Count) return;
                var row = _gridCheckIn.Rows[e.RowIndex];
                if (row.IsNewRow) return;

                if (e.ColumnIndex == 7)
                {
                    string status = row.Cells[6].Value != null ? row.Cells[6].Value.ToString() : "";
                    string pName = row.Cells[2].Value != null ? row.Cells[2].Value.ToString() : "";
                    string code = row.Cells[1].Value != null ? row.Cells[1].Value.ToString() : "";
                    string spec = row.Cells[3].Value != null ? row.Cells[3].Value.ToString() : "";
                    string slot = row.Cells[4].Value != null ? row.Cells[4].Value.ToString() : "";

                    if (status.Contains("Đã Check-in"))
                    {
                        // Đã Check-in rồi thì không thực hiện lại nữa để tránh conflict dữ liệu
                        return;
                    }
                    else
                    {
                        ExecuteCheckInRow(row);
                    }
                }
                else if (e.ColumnIndex == 8)
                {
                    ShowCancelNoShowMenu(row, e.RowIndex);
                }
            };

            tab.Controls.Add(_gridCheckIn);
            tab.Controls.Add(pnlSearch);
            tab.Controls.Add(pnlKpi);
        }

        private Panel CreateKpiCard(string title, string defaultVal, Color themeColor, out Label valLabel)
        {
            Panel card = new Panel
            {
                Size = new Size(290, 56),
                BackColor = Color.White
            };

            Panel bar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 5,
                BackColor = themeColor
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = ClinicalColors.GetMainFont(8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(14, 8),
                AutoSize = true,
                UseMnemonic = false
            };

            valLabel = new Label
            {
                Text = defaultVal,
                Font = ClinicalColors.GetMainFont(15f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(14, 26),
                AutoSize = true,
                UseMnemonic = false
            };

            card.Controls.Add(bar);
            card.Controls.Add(lblTitle);
            card.Controls.Add(valLabel);
            return card;
        }

        private void BuildCashierTab(TabPage tab)
        {
            SplitContainer split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = ClinicalColors.BorderGray
            };

            tab.SizeChanged += (s, e) =>
            {
                try
                {
                    if (split.Width > 500)
                    {
                        split.SplitterDistance = Math.Max(250, split.Width - 360);
                    }
                }
                catch { }
            };

            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Panel pnlSearch = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White, Padding = new Padding(0, 8, 0, 8) };

            Label lblSearch = new Label
            {
                Text = "Tìm hóa đơn viện phí:",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                Location = new Point(16, 14),
                AutoSize = true,
                UseMnemonic = false
            };
            _txtSearchBilling = new TextBox
            {
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular),
                Location = new Point(175, 11),
                Size = new Size(240, 28)
            };
            _txtSearchBilling.TextChanged += (s, e) => FilterBillingGrid();

            pnlSearch.Controls.Add(lblSearch);
            pnlSearch.Controls.Add(_txtSearchBilling);

            _gridBilling = new AntiFlickerDataGridView { Dock = DockStyle.Fill };
            _gridBilling.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", FillWeight = 25 });
            _gridBilling.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BENH NHAN", FillWeight = 85 });
            _gridBilling.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CHUYEN KHOA KHAM", FillWeight = 95 });
            _gridBilling.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TONG VIEN PHI", FillWeight = 55 });
            _gridBilling.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TRANG THAI THANH TOAN", FillWeight = 80 });

            _gridBilling.SelectionChanged += (s, e) => OnBillingRowSelected();

            pnlLeft.Controls.Add(_gridBilling);
            pnlLeft.Controls.Add(pnlSearch);

            Panel pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20) };

            Label lblCardTitle = new Label
            {
                Text = "[*] BẢNG KÊ VIỆN PHÍ & HÓA ĐƠN TÀI CHÍNH",
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Dock = DockStyle.Top,
                Height = 35,
                UseMnemonic = false
            };

            _lblPatientDetail = new Label
            {
                Text = "Bệnh nhân: Chọn một ca khám từ danh sách bên trái",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Dock = DockStyle.Top,
                Height = 45,
                UseMnemonic = false
            };

            Panel pnlDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = ClinicalColors.BorderGray };

            Panel pnlItems = new Panel { Dock = DockStyle.Top, Height = 180, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(15) };

            _lblFeeExam = new Label { Text = "1. Công khám lâm sàng chuyên khoa  :  250.000 VNĐ", Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular), Location = new Point(15, 15), AutoSize = true, UseMnemonic = false };
            _lblFeeServices = new Label { Text = "2. Phí dịch vụ Cận lâm sàng (CLS)       :  0 VNĐ", Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular), Location = new Point(15, 45), AutoSize = true, UseMnemonic = false };
            _lblFeeMeds = new Label { Text = "3. Phí thuốc theo Đơn thuốc điện tử  :  0 VNĐ", Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular), Location = new Point(15, 75), AutoSize = true, UseMnemonic = false };

            Panel lineSub = new Panel { Location = new Point(15, 110), Size = new Size(350, 1), BackColor = ClinicalColors.BorderGray };

            _lblTotalAmount = new Label
            {
                Text = "TỔNG THANH TOÁN :  0 VNĐ",
                Font = ClinicalColors.GetMainFont(12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(15, 125),
                AutoSize = true,
                UseMnemonic = false
            };

            pnlItems.Controls.Add(_lblFeeExam);
            pnlItems.Controls.Add(_lblFeeServices);
            pnlItems.Controls.Add(_lblFeeMeds);
            pnlItems.Controls.Add(lineSub);
            pnlItems.Controls.Add(_lblTotalAmount);

            Panel pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 130, BackColor = Color.White };

            _btnConfirmPayment = new Button
            {
                Text = "[*] XÁC NHẬN ĐÃ THU TIỀN TẠI BỆNH VIỆN",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(16, 185, 129),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(340, 48),
                Location = new Point(10, 10),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            _btnConfirmPayment.FlatAppearance.BorderSize = 0;
            _btnConfirmPayment.Click += (s, e) => ExecuteConfirmPayment();

            _btnPrintInvoice = new Button
            {
                Text = "[*] IN HÓA ĐƠN TÀI CHÍNH A4/A5",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(340, 42),
                Location = new Point(10, 68),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            _btnPrintInvoice.FlatAppearance.BorderColor = ClinicalColors.PrimaryBlue;
            _btnPrintInvoice.Click += (s, e) => ExecutePrintInvoice();

            pnlActions.Controls.Add(_btnConfirmPayment);
            pnlActions.Controls.Add(_btnPrintInvoice);

            pnlRight.Controls.Add(pnlActions);
            pnlRight.Controls.Add(pnlItems);
            pnlRight.Controls.Add(pnlDivider);
            pnlRight.Controls.Add(_lblPatientDetail);
            pnlRight.Controls.Add(lblCardTitle);

            split.Panel1.Controls.Add(pnlLeft);
            split.Panel2.Controls.Add(pnlRight);

            tab.Controls.Add(split);
        }

        private void BuildApproveTab(TabPage tab)
        {
            SplitContainer split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = ClinicalColors.BorderGray
            };

            tab.SizeChanged += (s, e) =>
            {
                try
                {
                    if (split.Width > 500)
                    {
                        split.SplitterDistance = Math.Max(250, split.Width - 380);
                    }
                }
                catch { }
            };

            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.White, Padding = new Padding(15, 12, 15, 12) };

            Label lblTitle = new Label
            {
                Text = "[*] HỒ SƠ ĐĂNG KÝ APP MOBILE CHỜ LỄ TÂN ĐỐI CHIẾU THẺ CCCD THỰC TẾ",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(15, 16),
                AutoSize = true,
                UseMnemonic = false
            };
            pnlTop.Controls.Add(lblTitle);

            _gridApproveMobile = new AntiFlickerDataGridView { Dock = DockStyle.Fill };
            _gridApproveMobile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", FillWeight = 25 });
            _gridApproveMobile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "HỌ VÀ TÊN BỆNH NHÂN", FillWeight = 90 });
            _gridApproveMobile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "MỐI QUAN HỆ", FillWeight = 55 });
            _gridApproveMobile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SỐ ĐIỆN THOẠI", FillWeight = 60 });
            _gridApproveMobile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SỐ CCCD HIỆN TẠI", FillWeight = 75 });
            _gridApproveMobile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "MÃ THẺ BHYT", FillWeight = 75 });
            _gridApproveMobile.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TRẠNG THÁI ĐỊNH DANH", FillWeight = 75 });

            _gridApproveMobile.SelectionChanged += (s, e) => OnMobilePatientRowSelected();

            pnlLeft.Controls.Add(_gridApproveMobile);
            pnlLeft.Controls.Add(pnlTop);

            Panel pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20) };

            Label lblBoxTitle = new Label
            {
                Text = "[*] ĐỐI CHIẾU THẺ CCCD THỰC TẾ",
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Dock = DockStyle.Top,
                Height = 35,
                UseMnemonic = false
            };

            _lblSelectedMobilePatient = new Label
            {
                Text = "Bệnh nhân: Chọn một hồ sơ chờ duyệt từ bảng bên trái",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Dock = DockStyle.Top,
                Height = 45,
                UseMnemonic = false
            };

            Panel pnlDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = ClinicalColors.BorderGray };

            Panel pnlCccdInputBox = new Panel { Dock = DockStyle.Top, Height = 200, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(15) };

            Label lblCccdPrompt = new Label
            {
                Text = "[*] Nhập/Kiểm tra 12 số CCCD trên thẻ cứng bệnh nhân mang tới Quầy:",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(15, 15),
                AutoSize = true,
                UseMnemonic = false
            };

            _txtVerifyCccdInput = new TextBox
            {
                Font = ClinicalColors.GetMainFont(12f, FontStyle.Bold),
                Location = new Point(15, 45),
                Size = new Size(320, 34),
                MaxLength = 12
            };

            Label lblInstruction = new Label
            {
                Text = " Quy trình chuẩn Y tế: Lễ tân bắt buộc phải đối chiếu khớp thông tin trên thẻ CCCD thực tế với bệnh nhân trước khi nhấn Duyệt kích hoạt tài khoản App Mobile.",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 83, 9),
                Location = new Point(15, 95),
                Size = new Size(340, 70),
                UseMnemonic = false
            };

            pnlCccdInputBox.Controls.Add(lblCccdPrompt);
            pnlCccdInputBox.Controls.Add(_txtVerifyCccdInput);
            pnlCccdInputBox.Controls.Add(lblInstruction);

            Panel pnlAction = new Panel { Dock = DockStyle.Bottom, Height = 100, BackColor = Color.White };

            _btnExecuteCccdApprove = new Button
            {
                Text = " XÁC NHẬN ĐỐI CHIẾU CCCD & DUYỆT HỒ SƠ",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(16, 185, 129),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(340, 50),
                Location = new Point(10, 15),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            _btnExecuteCccdApprove.FlatAppearance.BorderSize = 0;
            _btnExecuteCccdApprove.Click += (s, e) => ExecuteVerifyCccdAndApprove();

            pnlAction.Controls.Add(_btnExecuteCccdApprove);

            pnlRight.Controls.Add(pnlAction);
            pnlRight.Controls.Add(pnlCccdInputBox);
            pnlRight.Controls.Add(pnlDivider);
            pnlRight.Controls.Add(_lblSelectedMobilePatient);
            pnlRight.Controls.Add(lblBoxTitle);

            split.Panel1.Controls.Add(pnlLeft);
            split.Panel2.Controls.Add(pnlRight);

            tab.Controls.Add(split);
        }

        private void BuildWalkInTab(TabPage tab)
        {
            Panel pnlForm = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(30),
                AutoScroll = true
            };

            Label lblTitle = new Label
            {
                Text = "➕ ĐĂNG KÝ HỒ SƠ KHÁCH HÀNG VÃNG LAI (KHÁM TRỰC TIẾP TẠI BỆNH VIỆN)",
                Font = ClinicalColors.GetMainFont(12f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(30, 20),
                AutoSize = true,
                UseMnemonic = false
            };

            int y = 70;

            Label lblName = new Label { Text = "Họ và tên Bệnh nhân (*):", Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold), Location = new Point(30, y), AutoSize = true, UseMnemonic = false };
            _txtWalkinName = new TextBox { Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular), Location = new Point(230, y - 4), Size = new Size(300, 30) };

            Label lblPhone = new Label { Text = "Số điện thoại (*):", Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold), Location = new Point(560, y), AutoSize = true, UseMnemonic = false };
            _txtWalkinPhone = new TextBox { Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular), Location = new Point(710, y - 4), Size = new Size(250, 30), MaxLength = 10 };

            y += 50;

            Label lblDob = new Label { Text = "Ngày tháng năm sinh:", Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold), Location = new Point(30, y), AutoSize = true, UseMnemonic = false };
            _dtpWalkinDob = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular), Location = new Point(230, y - 4), Size = new Size(180, 30) };

            Label lblGender = new Label { Text = "Giới tính:", Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold), Location = new Point(560, y), AutoSize = true, UseMnemonic = false };
            _cboWalkinGender = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular), Location = new Point(710, y - 4), Size = new Size(150, 30) };
            _cboWalkinGender.Items.AddRange(new object[] { "Nam", "Nữ", "Khác" });
            _cboWalkinGender.SelectedIndex = 0;

            y += 50;

            Label lblCccd = new Label { Text = "Số CCCD nhập (*):", Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold), Location = new Point(30, y), AutoSize = true, UseMnemonic = false };
            _txtWalkinCccd = new TextBox { Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular), Location = new Point(230, y - 4), Size = new Size(300, 30), MaxLength = 12 };

            Label lblBhyt = new Label { Text = "Mã số thẻ BHYT:", Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold), Location = new Point(560, y), AutoSize = true, UseMnemonic = false };
            _txtWalkinBhyt = new TextBox { Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular), Location = new Point(710, y - 4), Size = new Size(250, 30) };

            y += 50;

            Label lblAddress = new Label { Text = "Địa chỉ thường trú:", Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold), Location = new Point(30, y), AutoSize = true, UseMnemonic = false };
            _txtWalkinAddress = new TextBox { Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular), Location = new Point(230, y - 4), Size = new Size(730, 30) };

            y += 50;

            Label lblExamDate = new Label { Text = "Ngày đăng ký khám (*):", Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold), Location = new Point(30, y), AutoSize = true, UseMnemonic = false };
            _dtpWalkinExamDate = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular), Location = new Point(230, y - 4), Size = new Size(200, 30), Value = DateTime.Today };
            _dtpWalkinExamDate.ValueChanged += (s, e) => FilterDoctorsByExamDate();

            y += 50;

            Label lblSpec = new Label { Text = "Đăng ký Chuyên khoa (*):", Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold), Location = new Point(30, y), AutoSize = true, UseMnemonic = false };
            _cboWalkinSpecialty = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular), Location = new Point(230, y - 4), Size = new Size(600, 30), DropDownWidth = 650, IntegralHeight = false, MaxDropDownItems = 10 };

            FilterDoctorsByExamDate();

            y += 65;

            Button btnSaveWalkIn = new Button
            {
                Text = " Tạo hồ sơ & Cấp số STT khám vãng lai",
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(16, 185, 129),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(450, 48),
                Location = new Point(230, y),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnSaveWalkIn.FlatAppearance.BorderSize = 0;
            btnSaveWalkIn.Click += (s, e) => ExecuteCreateWalkInPatient();

            pnlForm.Controls.Add(lblTitle);
            pnlForm.Controls.Add(lblName); pnlForm.Controls.Add(_txtWalkinName);
            pnlForm.Controls.Add(lblPhone); pnlForm.Controls.Add(_txtWalkinPhone);
            pnlForm.Controls.Add(lblDob); pnlForm.Controls.Add(_dtpWalkinDob);
            pnlForm.Controls.Add(lblGender); pnlForm.Controls.Add(_cboWalkinGender);
            pnlForm.Controls.Add(lblCccd); pnlForm.Controls.Add(_txtWalkinCccd);
            pnlForm.Controls.Add(lblBhyt); pnlForm.Controls.Add(_txtWalkinBhyt);
            pnlForm.Controls.Add(lblAddress); pnlForm.Controls.Add(_txtWalkinAddress);
            pnlForm.Controls.Add(lblExamDate); pnlForm.Controls.Add(_dtpWalkinExamDate);
            pnlForm.Controls.Add(lblSpec); pnlForm.Controls.Add(_cboWalkinSpecialty);
            pnlForm.Controls.Add(btnSaveWalkIn);

            tab.Controls.Add(pnlForm);
        }

        // ── Tab 5: Khám Trực Tiếp — dành cho bệnh nhân ĐÃ CÓ hồ sơ trong hệ thống
        // (tạo qua Lễ Tân trước đây / tự đăng ký qua App nhưng chưa xác thực CCCD / đã xác thực)
        // nhưng CHƯA đặt hẹn trước, đến khám trực tiếp tại quầy hôm nay.
        private void BuildDirectExamTab(TabPage tab)
        {
            Panel pnlForm = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(30),
                AutoScroll = true
            };

            Label lblTitle = new Label
            {
                Text = "🏥 KHÁM TRỰC TIẾP CHO BỆNH NHÂN ĐÃ CÓ HỒ SƠ (CHƯA ĐẶT HẸN TRƯỚC)",
                Font = ClinicalColors.GetMainFont(12f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(30, 20),
                AutoSize = true,
                UseMnemonic = false
            };

            Label lblSubtitle = new Label
            {
                Text = "Dùng cho: bệnh nhân đã được Lễ Tân tạo hồ sơ trước đây  •  bệnh nhân tự đăng ký qua App nhưng chưa xác thực CCCD  •  bệnh nhân đã xác thực qua App — nay đến khám trực tiếp không có lịch hẹn.",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(30, 50),
                Size = new Size(1000, 20),
                UseMnemonic = false
            };

            int y = 90;

            Label lblSearchLabel = new Label { Text = "Tìm theo Tên hoặc SĐT:", Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold), Location = new Point(30, y), AutoSize = true, UseMnemonic = false };
            _txtDirectSearchPhone = new TextBox { Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular), Location = new Point(230, y - 4), Size = new Size(320, 30) };
            _txtDirectSearchPhone.TextChanged += (s, e) => FilterDirectPatientsGrid();

            Button btnDirectSearch = new Button
            {
                Text = " Tải lại danh sách",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ClinicalColors.PrimaryBlue,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(160, 32),
                Location = new Point(565, y - 5),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnDirectSearch.FlatAppearance.BorderSize = 0;
            btnDirectSearch.Click += async (s, e) => await LoadDirectPatientsListAsync();

            y += 45;

            // --- Danh sách toàn bộ bệnh nhân — chọn 1 dòng để khám trực tiếp ---
            _gridDirectPatients = new AntiFlickerDataGridView
            {
                Location = new Point(30, y),
                Size = new Size(1000, 260)
            };
            _gridDirectPatients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", FillWeight = 20 });
            _gridDirectPatients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "HỌ VÀ TÊN", FillWeight = 90 });
            _gridDirectPatients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SỐ ĐIỆN THOẠI", FillWeight = 60 });
            _gridDirectPatients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SỐ CCCD", FillWeight = 70 });
            _gridDirectPatients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TRẠNG THÁI ĐỊNH DANH", FillWeight = 75 });
            _gridDirectPatients.SelectionChanged += (s, e) => OnDirectPatientRowSelected();

            y += 270;

            // --- Khu vực hiển thị kết quả tìm kiếm ---
            _pnlDirectResult = new Panel
            {
                Location = new Point(30, y),
                Size = new Size(1000, 90),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };

            _lblDirectName = new Label
            {
                Font = ClinicalColors.GetMainFont(11.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(15, 12),
                Size = new Size(500, 24),
                UseMnemonic = false
            };
            _lblDirectStatusBadge = new Label
            {
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                Location = new Point(15, 40),
                Size = new Size(500, 22),
                UseMnemonic = false
            };
            _lblDirectInfo = new Label
            {
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(15, 64),
                Size = new Size(900, 20),
                UseMnemonic = false
            };
            _pnlDirectResult.Controls.Add(_lblDirectName);
            _pnlDirectResult.Controls.Add(_lblDirectStatusBadge);
            _pnlDirectResult.Controls.Add(_lblDirectInfo);

            y += 105;

            _lblDirectWarning = new Label
            {
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(30, y),
                Size = new Size(1000, 24),
                Visible = false,
                UseMnemonic = false
            };

            y += 35;

            // --- Khu vực chọn chuyên khoa & xác nhận CCCD để khám ngay ---
            _pnlDirectBookingBox = new Panel
            {
                Location = new Point(30, y),
                Size = new Size(1000, 150),
                Visible = false
            };

            Label lblDirectCccd = new Label { Text = "Số CCCD (đối chiếu tại quầy) (*):", Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold), Location = new Point(0, 5), AutoSize = true, UseMnemonic = false };
            _txtDirectCccd = new TextBox { Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular), Location = new Point(0, 30), Size = new Size(300, 30), MaxLength = 12 };

            Label lblDirectSpec = new Label { Text = "Đăng ký Chuyên khoa khám hôm nay (*):", Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold), Location = new Point(0, 72), AutoSize = true, UseMnemonic = false };
            _cboDirectSpecialty = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular), Location = new Point(0, 97), Size = new Size(600, 30) };

            _btnDirectBookNow = new Button
            {
                Text = " Đặt Khám Ngay & Check-in",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(16, 185, 129),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(280, 42),
                Location = new Point(650, 95),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            _btnDirectBookNow.FlatAppearance.BorderSize = 0;
            _btnDirectBookNow.Click += async (s, e) => await ExecuteDirectBookNowAsync();

            _pnlDirectBookingBox.Controls.Add(lblDirectCccd);
            _pnlDirectBookingBox.Controls.Add(_txtDirectCccd);
            _pnlDirectBookingBox.Controls.Add(lblDirectSpec);
            _pnlDirectBookingBox.Controls.Add(_cboDirectSpecialty);
            _pnlDirectBookingBox.Controls.Add(_btnDirectBookNow);

            pnlForm.Controls.Add(lblTitle);
            pnlForm.Controls.Add(lblSubtitle);
            pnlForm.Controls.Add(lblSearchLabel);
            pnlForm.Controls.Add(_txtDirectSearchPhone);
            pnlForm.Controls.Add(btnDirectSearch);
            pnlForm.Controls.Add(_gridDirectPatients);
            pnlForm.Controls.Add(_pnlDirectResult);
            pnlForm.Controls.Add(_lblDirectWarning);
            pnlForm.Controls.Add(_pnlDirectBookingBox);

            tab.Controls.Add(pnlForm);
        }

        // Tải toàn bộ danh sách bệnh nhân (hồ sơ chính) vào lưới để Lễ Tân xem/chọn nhanh,
        // thay vì bắt buộc phải gõ đúng SĐT như trước đây.
        private async Task LoadDirectPatientsListAsync()
        {
            if (_gridDirectPatients == null) return;
            try
            {
                var api = new ApiService();
                var allPatients = await api.GetPatientsAsync();
                _allDirectPatients = allPatients ?? new List<PatientSimpleModel>();
                FilterDirectPatientsGrid();
            }
            catch { }
        }

        // Lọc theo tên HOẶC SĐT ngay khi gõ (không cần bấm nút) — chỉ lọc trên dữ liệu đã tải sẵn
        private void FilterDirectPatientsGrid()
        {
            if (_gridDirectPatients == null) return;
            string q = (_txtDirectSearchPhone?.Text ?? "").Trim().ToLower();

            var filtered = string.IsNullOrEmpty(q)
                ? _allDirectPatients
                : _allDirectPatients.Where(p =>
                    (!string.IsNullOrEmpty(p.FullName) && p.FullName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(p.Phone) && p.Phone.Replace(" ", "").Contains(q.Replace(" ", "")))
                  ).ToList();

            _gridDirectPatients.Rows.Clear();
            int stt = 1;
            foreach (var p in filtered)
            {
                bool isVerified = p.VerificationStatus == "verified";
                string statusText = isVerified ? "Đã xác thực CCCD" : "Chưa xác thực CCCD";
                string cccdText = !string.IsNullOrEmpty(p.Cccd) ? p.Cccd : "Chưa có";
                int rowIdx = _gridDirectPatients.Rows.Add(stt++, p.FullName.ToUpper(), p.Phone, cccdText, statusText);
                _gridDirectPatients.Rows[rowIdx].Tag = p;
                if (isVerified) _gridDirectPatients.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(236, 253, 245);
            }
        }

        private async void OnDirectPatientRowSelected()
        {
            if (_gridDirectPatients.SelectedRows.Count == 0) return;
            var found = _gridDirectPatients.SelectedRows[0].Tag as PatientSimpleModel;
            if (found == null) return;

            await ShowDirectPatientDetailsAsync(found);
        }

        // Hiển thị thông tin bệnh nhân đã chọn (từ lưới hoặc tìm theo SĐT) + kiểm tra lịch hôm nay
        private async Task ShowDirectPatientDetailsAsync(PatientSimpleModel found)
        {
            _pnlDirectResult.Visible = false;
            _lblDirectWarning.Visible = false;
            _pnlDirectBookingBox.Visible = false;
            _directFoundPatient = found;

            _lblDirectName.Text = found.FullName.ToUpper();
            if (found.VerificationStatus == "verified")
            {
                _lblDirectStatusBadge.Text = "✓ Đã xác thực CCCD qua App/Quầy";
                _lblDirectStatusBadge.ForeColor = Color.FromArgb(16, 185, 129);
            }
            else
            {
                _lblDirectStatusBadge.Text = "⚠ Chưa xác thực CCCD — sẽ được xác thực khi đặt khám tại đây";
                _lblDirectStatusBadge.ForeColor = Color.FromArgb(245, 158, 11);
            }
            _lblDirectInfo.Text = string.Format("SĐT: {0}   •   CCCD hiện có: {1}   •   Ngày sinh: {2}   •   Giới tính: {3}",
                found.Phone,
                string.IsNullOrEmpty(found.Cccd) ? "(chưa có)" : found.Cccd,
                string.IsNullOrEmpty(found.Dob) ? "(chưa có)" : found.Dob,
                string.IsNullOrEmpty(found.Gender) ? "(chưa có)" : found.Gender);
            _pnlDirectResult.Visible = true;

            // Kiểm tra bệnh nhân có lịch hẹn HÔM NAY còn ĐANG XỬ LÝ (chưa khám xong/chưa hủy) không —
            // trước đây chỉ cần CÓ lịch hôm nay là chặn, kể cả khi lịch đó đã khám xong & thanh toán
            // xong rồi, khiến bệnh nhân quay lại khám thêm ca khác trong cùng ngày bị chặn oan.
            string[] finishedStatuses = { "Completed", "Cancelled", "NoShow" };
            var api = new ApiService();
            var todayAppointments = await api.GetQueueAppointmentsAsync();
            bool hasActiveApptToday = todayAppointments != null && todayAppointments.Any(a =>
                a.PatientId == found.Id && !finishedStatuses.Contains(a.Status));

            if (hasActiveApptToday)
            {
                _lblDirectWarning.Text = "⚠ Bệnh nhân này đang có lịch hẹn CHƯA HOÀN TẤT cho HÔM NAY. Vui lòng dùng tab \"1. TIẾP ĐÓN & CHECK-IN\" để check-in thay vì tạo lịch mới.";
                _lblDirectWarning.Visible = true;
                _pnlDirectBookingBox.Visible = false;
                return;
            }

            _txtDirectCccd.Text = found.Cccd ?? "";
            PopulateSpecialtyComboForDate(_cboDirectSpecialty, _directSpecialtyMap, DateTime.Today);
            _pnlDirectBookingBox.Visible = true;
        }

        private async Task ExecuteDirectBookNowAsync()
        {
            if (_directFoundPatient == null) return;

            string cccd = _txtDirectCccd.Text.Trim();
            if (string.IsNullOrEmpty(cccd))
            {
                MessageBox.Show("Vui lòng nhập/đối chiếu Số CCCD trước khi đặt khám!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtDirectCccd.Focus();
                return;
            }

            int selectedIdx = _cboDirectSpecialty.SelectedIndex;
            if (selectedIdx < 0 || !_directSpecialtyMap.TryGetValue(selectedIdx, out var specInfo))
            {
                MessageBox.Show("Không có bác sĩ trực để nhận khám hôm nay. Vui lòng chọn chuyên khoa khác.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnDirectBookNow.Enabled = false;
            _btnDirectBookNow.Text = " Đang xử lý...";

            try
            {
                var api = new ApiService();
                var result = await api.RegisterWalkInAsync(
                    _directFoundPatient.FullName,
                    _directFoundPatient.Phone,
                    cccd,
                    _directFoundPatient.Dob,
                    _directFoundPatient.Gender,
                    _directFoundPatient.Bhyt,
                    address: null,
                    doctorId: specInfo.DoctorId,
                    specialtyName: specInfo.SpecialtyName);

                if (result.Success)
                {
                    MessageBox.Show(
                        $"Đã đặt khám trực tiếp thành công cho {_directFoundPatient.FullName.ToUpper()}!\nChuyên khoa: {specInfo.SpecialtyName}\n\nBệnh nhân đã được Check-in, chuyển sang tab \"1. TIẾP ĐÓN & CHECK-IN\" để xem.",
                        "Đặt khám thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    _txtDirectSearchPhone.Text = "";
                    _pnlDirectResult.Visible = false;
                    _pnlDirectBookingBox.Visible = false;
                    _directFoundPatient = null;

                    await LoadDataPublicAsync();
                    SelectTab(0);
                }
                else
                {
                    MessageBox.Show("Không thể đặt khám lúc này. Vui lòng kiểm tra kết nối server và thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _btnDirectBookNow.Enabled = true;
                _btnDirectBookNow.Text = " Đặt Khám Ngay & Check-in";
            }
        }

        public async Task LoadDataPublicAsync()
        {
            try
            {
                if (_gridCheckIn == null || _gridCheckIn.IsDisposed) return;
                var api = new ApiService();

                // --- Tab 1 & 2: Load appointments + billing from API ---
                var appointments = await api.GetQueueAppointmentsAsync();

                _gridCheckIn.Rows.Clear();
                _gridBilling.Rows.Clear();
                _patientRowIdMap.Clear();
                _checkInRowStatusMap.Clear();

                if (appointments != null && appointments.Count > 0)
                {
                    string[] specialties = { "Nội tổng quát", "Tim mạch", "Cơ xương khớp", "Nhi khoa", "Nội tổng quát", "Tim mạch", "Cơ xương khớp", "Nhi khoa" };

                    // Gán tên chuyên khoa (kèm fallback demo) rồi SẮP XẾP theo chuyên khoa → giờ hẹn,
                    // để bệnh nhân cùng chuyên khoa hiển thị gần nhau, dễ điều phối khi đông bệnh nhân.
                    var appointmentsWithSpec = appointments
                        .Select((appt, idx) => (
                            Appt: appt,
                            Spec: !string.IsNullOrEmpty(appt.SpecialtyName)
                                ? appt.SpecialtyName.Replace("Goi Kham ", "").Replace("Goi Tam Soat ", "")
                                : specialties[idx % specialties.Length]
                        ))
                        .OrderBy(x => x.Spec, StringComparer.CurrentCultureIgnoreCase)
                        .ThenBy(x => x.Appt.TimeSlot)
                        .ToList();

                    // Cập nhật danh sách chuyên khoa trong ComboBox lọc — liệt kê TOÀN BỘ chuyên khoa của
                    // bệnh viện (nguồn: _allDoctorSchedules), không chỉ những khoa đang có bệnh nhân hôm nay,
                    // để lễ tân luôn thấy đủ danh sách kể cả khi khoa đó chưa có ai check-in.
                    if (_cboSpecialtyFilter != null)
                    {
                        string previousSelection = _cboSpecialtyFilter.SelectedItem?.ToString() ?? SpecialtyFilterAll;
                        var allSpecs = _allDoctorSchedules.Select(d => d.SpecialtyName).Distinct().OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase).ToList();
                        // Phòng trường hợp có bệnh nhân thuộc chuyên khoa lạ không nằm trong danh sách bác sĩ mẫu
                        var extraSpecs = appointmentsWithSpec.Select(x => x.Spec).Distinct().Except(allSpecs, StringComparer.CurrentCultureIgnoreCase);
                        var allSpecsFull = allSpecs.Concat(extraSpecs).OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase).ToList();

                        _cboSpecialtyFilter.Items.Clear();
                        _cboSpecialtyFilter.Items.Add(SpecialtyFilterAll);
                        foreach (var s in allSpecsFull) _cboSpecialtyFilter.Items.Add(s);

                        int restoreIndex = _cboSpecialtyFilter.Items.IndexOf(previousSelection);
                        _cboSpecialtyFilter.SelectedIndex = restoreIndex >= 0 ? restoreIndex : 0;
                    }

                    for (int i = 0; i < appointmentsWithSpec.Count; i++)
                    {
                        var appt = appointmentsWithSpec[i].Appt;
                        string spec = appointmentsWithSpec[i].Spec;
                        // isCompleted = Bac si da hoan tat kham, cho le tan thu tien
                        bool isCompleted = appt.Status == "Completed" || appt.Status == "Da xong";
                        // isCheckedIn = da check-in tai quay le tan (chua kham xong)
                        bool isCheckedIn = appt.Status == "CheckedIn" || appt.Status == "InProgress" || isCompleted;

                        string checkInStatus = isCheckedIn
                            ? string.Format("Đã Check-in (STT {0:D2})", appt.QueueNumber)
                            : "Chờ Check-in";
                        string code = string.Format("RX-{0:0000}-{1:D4}", DateTime.Now.Year, appt.AppointmentId > 0 ? appt.AppointmentId : i + 1);
                        string pName = !string.IsNullOrEmpty(appt.PatientName) ? appt.PatientName.ToUpper() : string.Format("BỆNH NHÂN #{0}", appt.PatientId);
                        string slot = !string.IsNullOrEmpty(appt.TimeSlot) ? appt.TimeSlot : string.Format("{0}:00", 8 + i);

                        string actionText = isCheckedIn ? "Đã Check-in ✓" : "Check-in";
                        _gridCheckIn.Rows.Add(i + 1, code, pName, spec, slot, "Đã xác nhận", checkInStatus, actionText, "Tùy chọn ▼");
                        if (isCheckedIn) _gridCheckIn.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(236, 253, 245);

                        string fee = !string.IsNullOrEmpty(appt.Fee) ? appt.Fee : "250.000d";
                        // Phân loại trạng thái thanh toán theo invoices.payment_status THẬT (appt.PaymentStatus)
                        // — KHÔNG dùng appt.Status/StatusId nữa, vì StatusId có thể bị ghi đè bởi các thao tác
                        // lâm sàng khác (vd: bác sĩ lưu lại bệnh án → status_id=4) khiến hóa đơn đã thanh toán
                        // hiện nhầm lại thành "chờ thu phí" mỗi khi tải lại dữ liệu.
                        bool isPaid = appt.PaymentStatus == "paid";
                        string billingStatus;
                        Color billingRowColor;
                        if (isPaid)
                        {
                            billingStatus = "Da thanh toan";
                            billingRowColor = Color.FromArgb(236, 253, 245); // xanh la - da thanh toan
                            _gridBilling.Rows.Add(i + 1, pName, spec, fee, billingStatus);
                            _gridBilling.Rows[_gridBilling.Rows.Count - 1].DefaultCellStyle.BackColor = billingRowColor;
                        }
                        else if (isCompleted)
                        {
                            billingStatus = "[!] CHO THU PHI";
                            billingRowColor = Color.FromArgb(255, 237, 213); // cam nhat - cho thu phi
                            _gridBilling.Rows.Add(i + 1, pName, spec, fee, billingStatus);
                            _gridBilling.Rows[_gridBilling.Rows.Count - 1].DefaultCellStyle.BackColor = billingRowColor;
                        }

                        // Luu appointmentId (khong phai patientId) de goi API check-in
                        _patientRowIdMap[i] = appt.AppointmentId;
                        _checkInRowStatusMap[i] = appt.Status;
                    }
                }
                else
                {
                    // Trống danh sách khi chưa có dữ liệu hôm nay
                }

                UpdateKpiSummaryCards();

                // --- Tab 3: Load real patients + hồ sơ người thân pending CCCD verification ---
                var allPatients = await api.GetPatientsAsync();
                _gridApproveMobile.Rows.Clear();
                _mobilePatientRowIdMap.Clear();
                _mobileRecordTypeMap.Clear();

                if (allPatients != null && allPatients.Count > 0)
                {
                    int rowIdx = 0;
                    foreach (var p in allPatients)
                    {
                        bool isVerified = p.VerificationStatus == "verified";
                        string statusText = isVerified ? "Đã xác thực CCCD (Đã duyệt)" : "Chờ đem CCCD tới Quầy";
                        string cccdText = !string.IsNullOrEmpty(p.Cccd) ? p.Cccd : "Chưa nhập CCCD";
                        string bhytText = !string.IsNullOrEmpty(p.Bhyt) ? p.Bhyt : "—";
                        string relationship = !string.IsNullOrEmpty(p.Relationship) ? p.Relationship : "Bản thân";
                        _gridApproveMobile.Rows.Add(rowIdx + 1, p.FullName.ToUpper(), relationship, p.Phone, cccdText, bhytText, statusText);
                        if (isVerified) _gridApproveMobile.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(236, 253, 245);
                        _mobilePatientRowIdMap[rowIdx] = p.Id;
                        _mobileRecordTypeMap[rowIdx] = p.RecordType;
                        rowIdx++;
                    }
                }
                else
                {
                    // Fallback: dữ liệu thật từ patients.csv
                    _gridApproveMobile.Rows.Add(1, "DAWN", "Bản thân", "0938110220", "Chưa nhập CCCD", "—", "Chờ đem CCCD tới Quầy");
                    _gridApproveMobile.Rows.Add(2, "DAVID JOHNS", "Bản thân", "0934123456", "Chưa nhập CCCD", "—", "Chờ đem CCCD tới Quầy");
                    _gridApproveMobile.Rows.Add(3, "PETE HAWKS", "Bản thân", "0909123456", "Đã xác thực", "—", "Đã xác thực CCCD (Đã duyệt)");
                    _gridApproveMobile.Rows.Add(4, "HONG", "Bản thân", "0912345557", "Đã xác thực", "—", "Đã xác thực CCCD (Đã duyệt)");
                    _gridApproveMobile.Rows[2].DefaultCellStyle.BackColor = Color.FromArgb(236, 253, 245);
                    _gridApproveMobile.Rows[3].DefaultCellStyle.BackColor = Color.FromArgb(236, 253, 245);
                    _mobilePatientRowIdMap[0] = 4; // Dawn patient_id=4
                    _mobilePatientRowIdMap[1] = 2; // David patient_id=2
                    _mobilePatientRowIdMap[2] = 3; // Pete patient_id=3
                    _mobilePatientRowIdMap[3] = 5; // Hong patient_id=5
                    _mobileRecordTypeMap[0] = "patient";
                    _mobileRecordTypeMap[1] = "patient";
                    _mobileRecordTypeMap[2] = "patient";
                    _mobileRecordTypeMap[3] = "patient";
                }

                // --- Tab 5: Đồng bộ danh sách bệnh nhân cho "Khám Trực Tiếp" (dùng lại dữ liệu vừa tải,
                // chỉ lấy hồ sơ chính — không gồm hồ sơ người thân) ---
                _allDirectPatients = (allPatients ?? new List<PatientSimpleModel>())
                    .Where(p => p.RecordType != "family_member")
                    .ToList();
                FilterDirectPatientsGrid();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadDataAsync error: " + ex.Message);
            }

            // --- Tab 4: Filter specialties + doctors theo Ngày đăng ký khám ---
            try
            {
                FilterDoctorsByExamDate();
            }
            catch { }
        }

        private class WalkInDoctorSchedule
        {
            public int DoctorId { get; set; }
            public string DoctorName { get; set; }
            public string SpecialtyName { get; set; }
            public string WorkingDays { get; set; }
            public string Room { get; set; }
        }

        private List<WalkInDoctorSchedule> _allDoctorSchedules = new List<WalkInDoctorSchedule>
        {
            new WalkInDoctorSchedule { DoctorId = 5, DoctorName = "BS. CKII Nguyễn Văn A", SpecialtyName = "Nội tổng quát", WorkingDays = "Thứ Hai, Tư, Sáu & Chủ Nhật", Room = "Phòng 101" },
            new WalkInDoctorSchedule { DoctorId = 12, DoctorName = "BS. CKII Trịnh Hoàng Minh", SpecialtyName = "Nội tổng quát", WorkingDays = "Thứ Ba, Năm, Bảy & Chủ Nhật", Room = "Phòng 103" },
            new WalkInDoctorSchedule { DoctorId = 2, DoctorName = "BS. CKI Lê Thị B", SpecialtyName = "Nhi khoa", WorkingDays = "Thứ Ba, Năm, Bảy", Room = "Phòng 102" },
            new WalkInDoctorSchedule { DoctorId = 13, DoctorName = "ThS. BS Nguyễn Mai Chi", SpecialtyName = "Nhi khoa", WorkingDays = "Thứ Hai, Tư, Sáu, Bảy", Room = "Phòng 104" },
            new WalkInDoctorSchedule { DoctorId = 3, DoctorName = "ThS. BS Trần Văn C", SpecialtyName = "Tim mạch", WorkingDays = "Thứ Hai, Ba, Năm, Sáu", Room = "Phòng 201" },
            new WalkInDoctorSchedule { DoctorId = 6, DoctorName = "BS. CKI Phạm Thị D", SpecialtyName = "Da liễu", WorkingDays = "Thứ Tư, Sáu, Bảy & Chủ Nhật", Room = "Phòng 202" },
            new WalkInDoctorSchedule { DoctorId = 8, DoctorName = "TS. BS Đỗ Phương Hạnh", SpecialtyName = "Phụ & Sản khoa", WorkingDays = "Thứ Hai, Tư, Năm, Bảy", Room = "Phòng 301" },
            new WalkInDoctorSchedule { DoctorId = 9, DoctorName = "BS. CKII Phạm Tuấn Kiệt", SpecialtyName = "Cơ xương khớp", WorkingDays = "Thứ Ba, Tư, Sáu, Chủ Nhật", Room = "Phòng 302" },
            new WalkInDoctorSchedule { DoctorId = 10, DoctorName = "ThS. BS Vũ Bích Ngọc", SpecialtyName = "Thần kinh", WorkingDays = "Thứ Hai, Ba, Sáu, Bảy", Room = "Phòng 401" },
            new WalkInDoctorSchedule { DoctorId = 11, DoctorName = "BS. CKI Hoàng Văn Long", SpecialtyName = "Chẩn đoán hình ảnh", WorkingDays = "Thứ Hai đến Thứ Sáu", Room = "Phòng 402" }
        };

        private void FilterDoctorsByExamDate()
        {
            if (_cboWalkinSpecialty == null || _dtpWalkinExamDate == null) return;
            PopulateSpecialtyComboForDate(_cboWalkinSpecialty, _walkinSpecialtyMap, _dtpWalkinExamDate.Value.Date);
        }

        // Dùng chung cho cả tab "Đăng Ký Hồ Sơ" (chọn ngày bất kỳ) và tab "Khám Trực Tiếp"
        // (luôn dùng ngày hôm nay) — liệt kê bác sĩ có lịch trực đúng ngày được chọn.
        private void PopulateSpecialtyComboForDate(ComboBox combo, Dictionary<int, (int DoctorId, string SpecialtyName)> map, DateTime selectedDate)
        {
            DayOfWeek dow = selectedDate.DayOfWeek;

            List<string> matchingKeywords = new List<string>();
            switch (dow)
            {
                case DayOfWeek.Monday: matchingKeywords.Add("Hai"); matchingKeywords.Add("Thứ Hai"); break;
                case DayOfWeek.Tuesday: matchingKeywords.Add("Ba"); matchingKeywords.Add("Thứ Ba"); break;
                case DayOfWeek.Wednesday: matchingKeywords.Add("Tư"); matchingKeywords.Add("Thứ Tư"); break;
                case DayOfWeek.Thursday: matchingKeywords.Add("Năm"); matchingKeywords.Add("Thứ Năm"); break;
                case DayOfWeek.Friday: matchingKeywords.Add("Sáu"); matchingKeywords.Add("Thứ Sáu"); break;
                case DayOfWeek.Saturday: matchingKeywords.Add("Bảy"); matchingKeywords.Add("Thứ Bảy"); break;
                case DayOfWeek.Sunday: matchingKeywords.Add("Chủ Nhật"); break;
            }

            combo.Items.Clear();
            map.Clear();

            int itemIdx = 0;
            foreach (var doc in _allDoctorSchedules)
            {
                bool isWorkingToday = false;
                if (doc.WorkingDays.Contains("Thứ Hai đến Thứ Sáu"))
                {
                    if (dow >= DayOfWeek.Monday && dow <= DayOfWeek.Friday) isWorkingToday = true;
                }
                else
                {
                    foreach (var kw in matchingKeywords)
                    {
                        if (doc.WorkingDays.Contains(kw))
                        {
                            isWorkingToday = true;
                            break;
                        }
                    }
                }

                if (isWorkingToday)
                {
                    string dayText = selectedDate.ToString("dd/MM/yyyy");
                    string displayText = $"{doc.SpecialtyName} — {doc.DoctorName} [{doc.Room}] (Lịch trực ngày {dayText})";
                    combo.Items.Add(displayText);
                    map[itemIdx] = (doc.DoctorId, doc.SpecialtyName);
                    itemIdx++;
                }
            }

            if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = 0;
            }
            else
            {
                combo.Items.Add(" Không có Bác sĩ trực vào ngày này");
                combo.SelectedIndex = 0;
            }
        }

        private void FilterReceptionGrid()
        {
            if (_txtSearchPatient == null || _gridCheckIn == null) return;
            string q = _txtSearchPatient.Text.ToLower().Trim();
            string specFilter = _cboSpecialtyFilter != null && _cboSpecialtyFilter.SelectedItem != null
                ? _cboSpecialtyFilter.SelectedItem.ToString()
                : SpecialtyFilterAll;

            foreach (DataGridViewRow row in _gridCheckIn.Rows)
            {
                if (row.IsNewRow) continue;
                string pName = row.Cells[2].Value != null ? row.Cells[2].Value.ToString().ToLower() : "";
                string code = row.Cells[1].Value != null ? row.Cells[1].Value.ToString().ToLower() : "";
                string spec = row.Cells[3].Value != null ? row.Cells[3].Value.ToString() : "";

                bool matchesSearch = string.IsNullOrEmpty(q) || pName.Contains(q) || code.Contains(q);
                bool matchesSpecialty = specFilter == SpecialtyFilterAll || spec == specFilter;
                row.Visible = matchesSearch && matchesSpecialty;
            }
        }

        private void FilterBillingGrid()
        {
            string q = _txtSearchBilling.Text.ToLower().Trim();
            foreach (DataGridViewRow row in _gridBilling.Rows)
            {
                if (row.IsNewRow) continue;
                string pName = row.Cells[1].Value != null ? row.Cells[1].Value.ToString().ToLower() : "";
                string spec = row.Cells[2].Value != null ? row.Cells[2].Value.ToString().ToLower() : "";
                row.Visible = string.IsNullOrEmpty(q) || pName.Contains(q) || spec.Contains(q);
            }
        }

        private void UpdateKpiSummaryCards()
        {
            if (_gridCheckIn == null || _lblKpiTotal == null) return;
            int total = 0;
            int checkedIn = 0;
            int pending = 0;

            foreach (DataGridViewRow row in _gridCheckIn.Rows)
            {
                if (row.IsNewRow) continue;
                total++;
                string status = row.Cells[6].Value != null ? row.Cells[6].Value.ToString() : "";
                if (status.Contains("Da Check-in") || status.Contains("Check-in"))
                    checkedIn++;
                else if (status.Contains("Da huy lich") || status.Contains("Bo kham"))
                    { /* Không tính vào "Chờ check-in" — đã hủy/bỏ khám, không còn chờ nữa */ }
                else
                    pending++;
            }

            _lblKpiTotal.Text = total.ToString();
            _lblKpiCheckedIn.Text = checkedIn.ToString();
            _lblKpiPending.Text = pending.ToString();

            // Dem so ca cho thu phi de hien badge tren tab Thu Ngan
            if (_gridBilling != null && _tabCashier != null)
            {
                int pendingPayment = 0;
                foreach (DataGridViewRow row in _gridBilling.Rows)
                {
                    if (row.IsNewRow) continue;
                    string bs = row.Cells[4].Value != null ? row.Cells[4].Value.ToString() : "";
                    if (bs.Contains("CHO THU PHI") || bs.Contains("Cho thu phi"))
                        pendingPayment++;
                }
                _tabCashier.Text = pendingPayment > 0
                    ? string.Format("[*] 2. BAN THU NGAN ({0} cho thu phi)", pendingPayment)
                    : "[*] 2. BAN THU NGAN & IN HOA DON";
            }
        }

        private async void ExecuteCheckInRow(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow) return;
            int rowIndex = row.Index;
            string pName = row.Cells[2].Value != null ? row.Cells[2].Value.ToString() : "Bệnh nhân";
            string code = row.Cells[1].Value != null ? row.Cells[1].Value.ToString() : "";

            bool apiSuccess = false;
            try
            {
                if (_patientRowIdMap.TryGetValue(rowIndex, out int apptId) && apptId > 0)
                {
                    var api = new ApiService();
                    apiSuccess = await api.CheckInAppointmentAsync(apptId);
                }
                else
                {
                    var parts = code.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int parsedId) && parsedId > 0)
                    {
                        var api = new ApiService();
                        apiSuccess = await api.CheckInAppointmentAsync(parsedId);
                    }
                }
            }
            catch { }

            row.Cells[6].Value = string.Format("Da Check-in (STT {0:D2})", rowIndex + 1);
            if (row.Cells.Count > 7) row.Cells[7].Value = "[In Phieu STT]";
            row.DefaultCellStyle.BackColor = Color.FromArgb(236, 253, 245);

            UpdateKpiSummaryCards();

            string syncMsg = apiSuccess
                ? "Da dong bo len Server - Benh nhan da xuat hien trong Hang cho lam sang cua Bac si!"
                : "Da cap nhat giao dien. (Se dong bo khi ket noi server)";

            ShowReceptionNotification(
                "XAC NHAN CHECK-IN STT THANH CONG",
                $"Benh nhan: {pName}\n" +
                $"Ma lich hen: {code}\n" +
                $"Da cap So Thu Tu: STT-{rowIndex + 1:D2}\n\n" +
                $"{syncMsg}",
                true);
        }

        // ── Hủy lịch hẹn / Đánh dấu Bỏ khám — dành riêng cho Lễ Tân, khác luồng bệnh nhân tự hủy trên App ──
        private void ShowCancelNoShowMenu(DataGridViewRow row, int rowIndex)
        {
            string checkInStatus = row.Cells[6].Value?.ToString() ?? "";
            bool isCheckedIn = checkInStatus.Contains("Đã Check-in");

            // Dùng trạng thái THẬT (không phải chữ hiển thị cứng ở cột "TRẠNG THÁI LỊCH HẸN")
            // để quyết định có được hủy/bỏ khám nữa hay không.
            string realStatus = _checkInRowStatusMap.TryGetValue(rowIndex, out string rs) ? rs : "";
            bool alreadyLocked = checkInStatus.Contains("Hủy") || checkInStatus.Contains("Bỏ khám")
                || realStatus == "Cancelled" || realStatus == "NoShow";
            bool alreadyCompleted = realStatus == "Completed";

            if (alreadyLocked)
            {
                MessageBox.Show("Lịch hẹn này đã được xử lý (đã hủy hoặc đã ghi nhận bỏ khám) trước đó.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (alreadyCompleted)
            {
                MessageBox.Show("Bệnh nhân đã khám xong (Completed). Không thể hủy lịch hẹn đã hoàn tất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var menu = new ContextMenuStrip
            {
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                BackColor = Color.White,
                ShowImageMargin = false,
                Cursor = Cursors.Hand,
                Renderer = new ModernDropdownRenderer(),
                Padding = new Padding(4, 6, 4, 6)
            };
            Padding itemPad = new Padding(12, 6, 12, 6);

            var itemCancel = new ToolStripMenuItem("Hủy lịch hẹn này")
            {
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = itemPad
            };
            itemCancel.Click += async (s, e) => await ExecuteCancelAppointmentAsync(row, rowIndex);
            menu.Items.Add(itemCancel);

            var itemNoShow = new ToolStripMenuItem("Đánh dấu Bỏ khám")
            {
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 158, 11),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = itemPad
            };
            itemNoShow.Click += async (s, e) => await ExecuteMarkNoShowAsync(row, rowIndex);
            menu.Items.Add(itemNoShow);

            Rectangle cellDisplayRect = _gridCheckIn.GetCellDisplayRectangle(8, rowIndex, false);
            Point dropdownPoint = _gridCheckIn.PointToScreen(new Point(cellDisplayRect.Left, cellDisplayRect.Bottom + 2));
            menu.Show(dropdownPoint);
        }

        // Hộp thoại nhỏ để Lễ Tân nhập lý do hủy lịch (bắt buộc, để lưu vào appointments.cancel_reason)
        private string ShowCancelReasonDialog(string patientName)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Lý do hủy lịch hẹn";
                dlg.Size = new Size(420, 230);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.White;
                dlg.Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

                var lbl = new Label
                {
                    Text = $"Nhập lý do hủy lịch hẹn của {patientName}:",
                    Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                    Location = new Point(20, 20),
                    Size = new Size(370, 20),
                    UseMnemonic = false
                };
                var txt = new TextBox
                {
                    Location = new Point(20, 45),
                    Size = new Size(370, 80),
                    Multiline = true,
                    Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular)
                };
                var btnOk = new Button
                {
                    Text = "Xác nhận Hủy",
                    Location = new Point(190, 140),
                    Size = new Size(200, 36),
                    BackColor = Color.FromArgb(220, 38, 38),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.OK,
                    UseMnemonic = false
                };
                btnOk.FlatAppearance.BorderSize = 0;
                var btnCancel = new Button
                {
                    Text = "Đóng",
                    Location = new Point(20, 140),
                    Size = new Size(150, 36),
                    ForeColor = ClinicalColors.TextMuted,
                    FlatStyle = FlatStyle.Flat,
                    DialogResult = DialogResult.Cancel,
                    UseMnemonic = false
                };

                dlg.Controls.Add(lbl);
                dlg.Controls.Add(txt);
                dlg.Controls.Add(btnOk);
                dlg.Controls.Add(btnCancel);
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                return dlg.ShowDialog(this) == DialogResult.OK ? txt.Text.Trim() : null;
            }
        }

        private async Task ExecuteCancelAppointmentAsync(DataGridViewRow row, int rowIndex)
        {
            string pName = row.Cells[2].Value?.ToString() ?? "Bệnh nhân";
            string reason = ShowCancelReasonDialog(pName);
            if (reason == null) return; // Lễ tân bấm Đóng, không hủy

            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Vui lòng nhập lý do hủy lịch trước khi xác nhận!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_patientRowIdMap.TryGetValue(rowIndex, out int apptId) || apptId <= 0) return;

            var api = new ApiService();
            bool success = await api.CancelAppointmentWithReasonAsync(apptId, reason);

            if (success)
            {
                row.Cells[5].Value = "Da huy lich";
                row.Cells[6].Value = "Da huy lich";
                row.Cells[7].Value = "-";
                row.DefaultCellStyle.BackColor = Color.FromArgb(254, 226, 226);
                UpdateKpiSummaryCards();
                ShowReceptionNotification("ĐÃ HỦY LỊCH HẸN", $"Đã hủy lịch hẹn của {pName}.\nLý do: {reason}\n\nBệnh nhân sẽ nhận thông báo trên App Mobile.", true);
            }
            else
            {
                MessageBox.Show("Không thể hủy lịch lúc này. Vui lòng kiểm tra kết nối server và thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ExecuteMarkNoShowAsync(DataGridViewRow row, int rowIndex)
        {
            string pName = row.Cells[2].Value?.ToString() ?? "Bệnh nhân";
            var confirm = MessageBox.Show(
                $"Xác nhận đánh dấu BỎ KHÁM cho bệnh nhân {pName}?\nChỉ dùng khi bệnh nhân đã đặt hẹn hôm nay nhưng không đến quầy tiếp đón.",
                "Xác nhận Bỏ khám", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            if (!_patientRowIdMap.TryGetValue(rowIndex, out int apptId) || apptId <= 0) return;

            var api = new ApiService();
            bool success = await api.UpdateAppointmentStatusAsync(apptId, "NoShow");

            if (success)
            {
                row.Cells[5].Value = "Bo kham";
                row.Cells[6].Value = "Bo kham";
                row.Cells[7].Value = "-";
                row.DefaultCellStyle.BackColor = Color.FromArgb(254, 243, 199);
                UpdateKpiSummaryCards();
                ShowReceptionNotification("ĐÃ GHI NHẬN BỎ KHÁM", $"Đã đánh dấu {pName} bỏ khám hôm nay. Trạng thái này sẽ hiện trong báo cáo thống kê của Bác sĩ.", true);
            }
            else
            {
                MessageBox.Show("Không thể cập nhật lúc này. Vui lòng kiểm tra kết nối server và thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowPrintSttTicketDialog(string pName, string code, string spec, string slot, int stt)
        {
            Form dlg = new Form
            {
                Text = "IN PHIEU XAC NHAN CHECK-IN & STT KHAM LAM SANG",
                Size = new Size(420, 520),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            Panel pnlTicket = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(25)
            };

            Label lblHeader = new Label
            {
                Text = "BỆNH VIỆN DTT HEALTHCARE\n-------------------------------------",
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 45
            };

            Label lblSttTitle = new Label
            {
                Text = "SỐ THỨ TỰ KHÁM LÂM SÀNG",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 25
            };

            Label lblSttNum = new Label
            {
                Text = string.Format("{0:D2}", stt),
                Font = ClinicalColors.GetMainFont(36f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 70
            };

            Label lblInfo = new Label
            {
                Text = string.Format(
                    "Mã lịch hẹn: {0}\n" +
                    "Họ và tên: {1}\n" +
                    "Chuyên khoa: {2}\n" +
                    "Khung giờ: {3}\n" +
                    "Ngày khám: {4:dd/MM/yyyy}\n" +
                    "-------------------------------------\n" +
                    "Vui long mang phieu nay toi phong kham chuyen khoa!",
                    code, pName, spec, slot, DateTime.Today),
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85),
                Dock = DockStyle.Top,
                Height = 160
            };

            Button btnPrint = new Button
            {
                Text = "In phieu so thu tu ngay",
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(37, 99, 235),
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Bottom,
                Height = 45,
                Cursor = Cursors.Hand
            };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += (s, e) =>
            {
                MessageBox.Show("Da gui lenh in phieu so thu tu ra may in nhiet tai Quay Le Tan!", "In phieu STT", MessageBoxButtons.OK, MessageBoxIcon.Information);
                dlg.Close();
            };

            pnlTicket.Controls.Add(btnPrint);
            pnlTicket.Controls.Add(lblInfo);
            pnlTicket.Controls.Add(lblSttNum);
            pnlTicket.Controls.Add(lblSttTitle);
            pnlTicket.Controls.Add(lblHeader);

            dlg.Controls.Add(pnlTicket);
            dlg.ShowDialog();
        }

        private void ExecuteCheckInSelected()
        {
            if (_gridCheckIn.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn 1 bệnh nhân từ danh sách để thực hiện Check-in!", "Thông báo Lễ Tân", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ExecuteCheckInRow(_gridCheckIn.SelectedRows[0]);
        }

        private void OnBillingRowSelected()
        {
            if (_gridBilling.SelectedRows.Count == 0)
            {
                _lblPatientDetail.Text = "Bệnh nhân: Chọn một ca khám từ danh sách bên trái";
                _lblFeeExam.Text = "1. Công khám lâm sàng chuyên khoa  :  0 VNĐ";
                _lblFeeServices.Text = "2. Phí dịch vụ Cận lâm sàng (CLS)       :  0 VNĐ";
                _lblFeeMeds.Text = "3. Phí thuốc theo Đơn thuốc điện tử  :  0 VNĐ";
                _lblTotalAmount.Text = "TỔNG THANH TOÁN :  0 VNĐ";
                return;
            }
            var row = _gridBilling.SelectedRows[0];
            int rowIndex = row.Index;
            string pName = row.Cells[1].Value != null ? row.Cells[1].Value.ToString() : "";
            string spec = row.Cells[2].Value != null ? row.Cells[2].Value.ToString() : "";
            string status = row.Cells[4].Value != null ? row.Cells[4].Value.ToString() : "";

            // Hiển thị tạm thời trong lúc chờ gọi API lấy phí thuốc thật từ đơn thuốc điện tử
            _lblPatientDetail.Text = string.Format("Bệnh nhân: {0}" + Environment.NewLine + "Dịch vụ: {1}", pName, spec);
            _lblFeeExam.Text = "1. Công khám lâm sàng chuyên khoa  :  250.000 VNĐ";
            _lblFeeServices.Text = "2. Phí dịch vụ Cận lâm sàng (CLS)       :  0 VNĐ";
            _lblFeeMeds.Text = "3. Phí thuốc theo Đơn thuốc điện tử  :  Đang tính...";
            _lblTotalAmount.Text = "TỔNG THANH TOÁN :  Đang tính...";

            if (_patientRowIdMap.TryGetValue(rowIndex, out int apptIdForEstimate) && apptIdForEstimate > 0)
            {
                RefreshBillingEstimateAsync(rowIndex, apptIdForEstimate);
            }

            if (status.Contains("Da thanh toan") || status.Contains("Đã thanh toán"))
            {
                _btnConfirmPayment.Enabled = false;
                _btnConfirmPayment.Text = "[OK] DA THANH TOAN TAI BENH VIEN";
                _btnConfirmPayment.BackColor = Color.FromArgb(148, 163, 184);
            }
            else if (status.Contains("CHO THU PHI") || status.Contains("Cho thu phi"))
            {
                // Bac si da hoan tat - cho le tan thu tien
                _btnConfirmPayment.Enabled = true;
                _btnConfirmPayment.Text = "[!] BAC SI DA XONG - THU TIEN NGAY";
                _btnConfirmPayment.BackColor = Color.FromArgb(234, 88, 12); // cam dam - urgent
            }
            else
            {
                // Chua kham xong
                _btnConfirmPayment.Enabled = false;
                _btnConfirmPayment.Text = "Dang kham - Chua the thu phi";
                _btnConfirmPayment.BackColor = Color.FromArgb(148, 163, 184);
            }
        }

        // Gọi API lấy phí thuốc THẬT tính từ đơn thuốc điện tử (prescription_details x medicines.unit_price)
        // và cập nhật lại bảng kê viện phí — tránh hiển thị "0 VNĐ" như trước đây (vốn chỉ đoán từ cột phí tĩnh).
        private async void RefreshBillingEstimateAsync(int rowIndex, int appointmentId)
        {
            var api = new ApiService();
            var estimate = await api.GetInvoiceEstimateAsync(appointmentId);

            // Bỏ qua nếu người dùng đã chọn sang dòng khác trong lúc chờ API phản hồi
            if (_gridBilling.SelectedRows.Count == 0 || _gridBilling.SelectedRows[0].Index != rowIndex) return;

            // Giá gói chỉ bao gồm các hạng mục CÓ SẴN trong gói — nếu bác sĩ kê thêm thuốc ngoài
            // phạm vi gói (vd: phát sinh chẩn đoán khác), vẫn phải cộng thêm phí thuốc thật, không
            // được coi là miễn phí.
            _lblFeeExam.Text = estimate.IsPackage
                ? string.Format("1. Trọn gói khám sức khỏe  :  {0:N0} VNĐ", estimate.ExamFee)
                : string.Format("1. Công khám lâm sàng chuyên khoa  :  {0:N0} VNĐ", estimate.ExamFee);
            _lblFeeServices.Text = string.Format("2. Phí dịch vụ Cận lâm sàng (CLS)       :  {0:N0} VNĐ", estimate.ServicesFee);
            _lblFeeMeds.Text = string.Format("3. Phí thuốc theo Đơn thuốc điện tử  :  {0:N0} VNĐ", estimate.MedsFee);
            _lblTotalAmount.Text = string.Format("TỔNG THANH TOÁN :  {0:N0} VNĐ", estimate.Total);
        }

        private async void ExecuteConfirmPayment()
        {
            if (_gridBilling.SelectedRows.Count == 0) return;
            var row = _gridBilling.SelectedRows[0];
            int rowIndex = row.Index;
            string pName = row.Cells[1].Value != null ? row.Cells[1].Value.ToString() : "";

            // Lấy appointmentId từ _patientRowIdMap (giống check-in)
            bool apiSuccess = false;
            int invoiceId = 0;
            decimal totalCharged = 250000m;
            try
            {
                if (_patientRowIdMap.TryGetValue(rowIndex, out int apptId) && apptId > 0)
                {
                    var api = new ApiService();
                    // Lấy phí khám + phí thuốc THẬT từ đơn thuốc điện tử trước khi xác nhận thu tiền,
                    // thay vì luôn gửi medsFee=0 như trước đây khiến hóa đơn thiếu tiền thuốc.
                    var estimate = await api.GetInvoiceEstimateAsync(apptId);
                    var result = await api.ConfirmPaymentAsync(apptId, 0, estimate.ExamFee, estimate.ServicesFee, estimate.MedsFee);
                    apiSuccess = result.Success;
                    invoiceId = result.InvoiceId;
                    totalCharged = estimate.Total;
                }
            }
            catch { }

            row.Cells[4].Value = "Da thanh toan";
            row.DefaultCellStyle.BackColor = Color.FromArgb(236, 253, 245);
            OnBillingRowSelected();
            // Cap nhat badge tren tab
            UpdateKpiSummaryCards();

            string syncLine = apiSuccess
                ? string.Format(" Hoa don #HD-{0} da duoc tao trong CSDL va day len App Mobile cua benh nhan!", invoiceId)
                : " Da cap nhat giao dien. Hoa don se dong bo khi ket noi server.";

            ShowReceptionNotification(
                "[OK] XAC NHAN THU TIEN THANH CONG",
                string.Format("Benh nhan: {0}\nVien phi: {1:N0}d\nTrang thai: DA THANH TOAN TAI BENH VIEN\n\n{2}", pName, totalCharged, syncLine),
                true);
        }

        private void ExecutePrintInvoice()
        {
            if (_gridBilling.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn 1 hóa đơn để thực hiện in!", "Thu Ngân Bệnh Viện", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = _gridBilling.SelectedRows[0];
            string pName = row.Cells[1].Value != null ? row.Cells[1].Value.ToString() : "";
            string spec = row.Cells[2].Value != null ? row.Cells[2].Value.ToString() : "";
            string total = row.Cells[3].Value != null ? row.Cells[3].Value.ToString() : "250.000đ";

            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrintPage += (s, ev) =>
                {
                    Graphics g = ev.Graphics;
                    Font fontBold = ClinicalColors.GetMainFont(12f, FontStyle.Bold);
                    Font fontReg = ClinicalColors.GetMainFont(10f, FontStyle.Regular);
                    Font fontTitle = ClinicalColors.GetMainFont(15f, FontStyle.Bold);

                    g.DrawString("DTT HEALTHCARE HOSPITAL", fontBold, Brushes.Navy, 50, 40);
                    g.DrawString("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM" + Environment.NewLine + "Độc lập - Tự do - Hạnh phúc", fontBold, Brushes.Black, 450, 40);
                    g.DrawLine(Pens.Navy, 50, 85, 750, 85);

                    g.DrawString("HÓA ĐƠN TÀI CHÍNH VIỆN PHÍ", fontTitle, Brushes.Black, 240, 105);
                    g.DrawString(string.Format("Họ và tên bệnh nhân : {0}", pName), fontReg, Brushes.Black, 50, 155);
                    g.DrawString(string.Format("Nội dung thanh toán  : {0}", spec), fontReg, Brushes.Black, 50, 185);
                    g.DrawString("Hình thức thanh toán : Thu tiền mặt / Thẻ POS tại Quầy thu ngân Bệnh viện", fontReg, Brushes.Black, 50, 215);

                    g.DrawLine(Pens.Gray, 50, 250, 750, 250);
                    g.DrawString(string.Format("TỔNG TIỀN ĐÃ THU  : {0}", total), fontTitle, Brushes.Red, 50, 270);
                    g.DrawString("Trạng thái          : ĐÃ THANH TOÁN TẠI QUẦY BỆNH VIỆN", fontBold, Brushes.Green, 50, 310);

                    g.DrawString("Người lập hóa đơn" + Environment.NewLine + "(Ký và ghi rõ họ tên)", fontBold, Brushes.Black, 550, 365);
                    g.DrawString(!string.IsNullOrEmpty(TokenVault.FullName) ? TokenVault.FullName : "Nguyễn Thị Minh Châu", fontBold, Brushes.Navy, 550, 440);
                };

                PrintPreviewDialog preview = new PrintPreviewDialog { Document = pd, Width = 850, Height = 650 };
                preview.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể in hóa đơn: " + ex.Message, "Lỗi In Ấn", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnMobilePatientRowSelected()
        {
            if (_gridApproveMobile.SelectedRows.Count == 0) return;
            var row = _gridApproveMobile.SelectedRows[0];
            string pName = row.Cells[1].Value != null ? row.Cells[1].Value.ToString() : "";
            string relationship = row.Cells[2].Value != null ? row.Cells[2].Value.ToString() : "Bản thân";
            string currentCccd = row.Cells[4].Value != null ? row.Cells[4].Value.ToString() : "";
            string status = row.Cells[6].Value != null ? row.Cells[6].Value.ToString() : "";

            string labelSubject = relationship == "Bản thân" ? pName : $"{pName} ({relationship})";
            _lblSelectedMobilePatient.Text = string.Format("Bệnh nhân: {0}" + Environment.NewLine + "Trạng thái: {1}", labelSubject, status);
            _txtVerifyCccdInput.Text = currentCccd.Contains("Chưa") ? "" : currentCccd;

            if (status.Contains("Đã xác thực"))
            {
                _btnExecuteCccdApprove.Enabled = false;
                _btnExecuteCccdApprove.Text = "[*] ĐÃ XÁC THỰC CCCD THỰC TẾ";
                _btnExecuteCccdApprove.BackColor = Color.FromArgb(148, 163, 184);
            }
            else
            {
                _btnExecuteCccdApprove.Enabled = true;
                _btnExecuteCccdApprove.Text = " XÁC NHẬN ĐỐI CHIẾU CCCD & DUYỆT HỒ SƠ";
                _btnExecuteCccdApprove.BackColor = Color.FromArgb(16, 185, 129);
            }
        }

        private void ShowReceptionNotification(string title, string message, bool isSuccess = true)
        {
            using (Form dialog = new Form())
            {
                dialog.Size = new Size(540, 380);
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.FormBorderStyle = FormBorderStyle.None;
                dialog.BackColor = Color.White;
                dialog.ShowInTaskbar = false;

                Color themeColor = isSuccess ? Color.FromArgb(16, 185, 129) : Color.FromArgb(220, 38, 38);

                Panel pnlHeader = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 56,
                    BackColor = themeColor
                };

                Label lblTitle = new Label
                {
                    Text = title,
                    Font = ClinicalColors.GetMainFont(11.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(20, 14),
                    Size = new Size(450, 28),
                    TextAlign = ContentAlignment.MiddleLeft,
                    UseMnemonic = false
                };

                Button btnClose = new Button
                {
                    Text = "✕",
                    Font = ClinicalColors.GetMainFont(12f, FontStyle.Bold),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(36, 36),
                    Location = new Point(485, 10),
                    Cursor = Cursors.Hand,
                    UseMnemonic = false
                };
                btnClose.FlatAppearance.BorderSize = 0;
                btnClose.Click += (s, e) => dialog.Close();

                pnlHeader.Controls.Add(lblTitle);
                pnlHeader.Controls.Add(btnClose);

                Panel pnlBody = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(24, 20, 24, 75),
                    BackColor = Color.White
                };

                Label lblMsg = new Label
                {
                    Text = message,
                    Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                    ForeColor = ClinicalColors.TextDark,
                    Dock = DockStyle.Fill,
                    UseMnemonic = false
                };

                Button btnOk = new Button
                {
                    Text = isSuccess ? " XÁC NHẬN / ĐÃ HIỂU" : "✕ ĐÓNG & KIỂM TRA LẠI",
                    Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = themeColor,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(240, 44),
                    Location = new Point(150, 316),
                    Cursor = Cursors.Hand,
                    UseMnemonic = false
                };
                btnOk.FlatAppearance.BorderSize = 0;
                btnOk.Click += (s, e) => dialog.Close();

                pnlBody.Controls.Add(lblMsg);
                dialog.Controls.Add(btnOk);
                dialog.Controls.Add(pnlBody);
                dialog.Controls.Add(pnlHeader);

                dialog.Paint += (s, e) => {
                    using (Pen p = new Pen(themeColor, 3))
                    {
                        e.Graphics.DrawRectangle(p, 0, 0, dialog.Width - 1, dialog.Height - 1);
                    }
                };

                dialog.ShowDialog(this);
            }
        }

        private async void ExecuteVerifyCccdAndApprove()
        {
            if (_gridApproveMobile.SelectedRows.Count == 0)
            {
                ShowReceptionNotification(" CHƯA CHỌN HỒ SƠ", "Vui lòng chọn 1 hồ sơ bệnh nhân chờ đối chiếu từ bảng bên trái!", false);
                return;
            }

            string cccdEntered = _txtVerifyCccdInput.Text.Trim();
            if (string.IsNullOrEmpty(cccdEntered) || !System.Text.RegularExpressions.Regex.IsMatch(cccdEntered, @"^[0-9]{12}$"))
            {
                ShowReceptionNotification(
                    " TỪ CHỐI DUYỆT: SỐ CCCD KHÔNG ĐỦ 12 SỐ",
                    "Hệ thống từ chối đối chiếu & duyệt hồ sơ Mobile!\n\n" +
                    "[*] Lý do: Số CCCD nhập không đủ hoặc sai định dạng 12 chữ số.\n" +
                    "[*] Yêu cầu: Lễ tân bắt buộc phải đối chiếu thẻ cứng CCCD thực tế và nhập đủ ĐÚNG 12 CHỮ SỐ.\n" +
                    "[*] Ví dụ chuẩn: 036099001234",
                    false);
                _txtVerifyCccdInput.Focus();
                return;
            }

            var row = _gridApproveMobile.SelectedRows[0];
            int rowIndex = row.Index;
            string pName = row.Cells[1].Value != null ? row.Cells[1].Value.ToString() : "";
            string relationship = row.Cells[2].Value != null ? row.Cells[2].Value.ToString() : "Bản thân";
            string phone = row.Cells[3].Value != null ? row.Cells[3].Value.ToString() : "";

            // Gọi đúng API: hồ sơ chính (patients) hay hồ sơ người thân (family_members)
            bool apiSuccess = false;
            try
            {
                if (_mobilePatientRowIdMap.TryGetValue(rowIndex, out int recordId) && recordId > 0)
                {
                    var api = new ApiService();
                    string recordType = _mobileRecordTypeMap.TryGetValue(rowIndex, out string rt) ? rt : "patient";
                    apiSuccess = recordType == "family_member"
                        ? await api.VerifyFamilyMemberCccdAsync(recordId, cccdEntered)
                        : await api.VerifyPatientCccdAsync(recordId, cccdEntered);
                }
            }
            catch { }

            row.Cells[4].Value = cccdEntered;
            row.Cells[6].Value = "Đã xác thực CCCD (Đã duyệt)";
            row.DefaultCellStyle.BackColor = Color.FromArgb(236, 253, 245);
            OnMobilePatientRowSelected();

            string subjectLine = relationship == "Bản thân" ? pName : $"{pName} (Người thân - {relationship})";
            ShowReceptionNotification(
                " ĐỐI CHIẾU THẺ CCCD THỰC TẾ & DUYỆT HỒ SƠ THÀNH CÔNG",
                $"Hồ sơ: {subjectLine}\n" +
                $"SĐT liên hệ: {phone}\n" +
                $"Số CCCD (Verified): {cccdEntered}\n\n" +
                "Lễ Tân đã đối chiếu khớp thông tin thẻ cứng CCCD thực tế! Hồ sơ đã chính thức được Xác thực (Verified) thành công!" +
                (relationship != "Bản thân" ? "\n\nChủ tài khoản sẽ nhận thông báo trên App Mobile." : ""),
                true);
        }

        private async void ExecuteCreateWalkInPatient()
        {
            string name = _txtWalkinName.Text.Trim();
            string phone = _txtWalkinPhone.Text.Trim();
            string cccd = _txtWalkinCccd.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                ShowReceptionNotification(" CHƯA NHẬP HỌ TÊN", "Vui lòng nhập đầy đủ Họ và tên của bệnh nhân vãng lai!", false);
                _txtWalkinName.Focus();
                return;
            }

            // Validate Phone: must be 10 digits starting with 0
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0[0-9]{9}$"))
            {
                ShowReceptionNotification(
                    " TỪ CHỐI TẠO HỒ SƠ: SĐT SAI ĐỊNH DẠNG",
                    "Hệ thống từ chối đăng ký hồ sơ bệnh nhân vãng lai!\n\n" +
                    "[*] Lý do: Số điện thoại không đúng định dạng chuẩn Y tế.\n" +
                    "[*] Yêu cầu: Phải nhập đúng 10 chữ số bắt đầu bằng số 0.\n" +
                    "[*] Ví dụ chuẩn: 0912345678",
                    false);
                _txtWalkinPhone.Focus();
                return;
            }

            // Validate CCCD: must be exactly 12 numeric digits
            if (!System.Text.RegularExpressions.Regex.IsMatch(cccd, @"^[0-9]{12}$"))
            {
                ShowReceptionNotification(
                    " TỪ CHỐI TẠO HỒ SƠ: SỐ CCCD KHÔNG ĐỦ 12 SỐ",
                    "Hệ thống từ chối đăng ký hồ sơ bệnh nhân vãng lai!\n\n" +
                    "[*] Lý do: Số thẻ CCCD nhập không đủ hoặc sai định dạng 12 chữ số.\n" +
                    "[*] Yêu cầu: Lễ tân bắt buộc phải đối chiếu thẻ cứng và nhập đủ ĐÚNG 12 CHỮ SỐ.\n" +
                    "[*] Ví dụ chuẩn: 036099001234",
                    false);
                _txtWalkinCccd.Focus();
                return;
            }

            // Lấy thông tin chuyên khoa + doctorId từ _walkinSpecialtyMap
            int selectedIdx = _cboWalkinSpecialty.SelectedIndex;
            int doctorId = 0;
            string specName = "Nội tổng quát";
            if (_walkinSpecialtyMap.TryGetValue(selectedIdx, out var specData))
            {
                doctorId = specData.DoctorId;
                specName = specData.SpecialtyName;
            }

            string dob = _dtpWalkinDob.Value.ToString("yyyy-MM-dd");
            string gender = _cboWalkinGender.SelectedItem?.ToString() ?? "Nam";
            string bhyt = _txtWalkinBhyt.Text.Trim();
            string address = _txtWalkinAddress.Text.Trim();

            // Goi API tạo hồ sơ vãng lai trong DB
            bool apiSuccess = false;
            string tempPwd = $"DTT@{phone.Substring(phone.Length - 4)}";
            int newPatientId = 0, newApptId = 0;
            try
            {
                var api = new ApiService();
                var result = await api.RegisterWalkInAsync(name, phone, cccd, dob, gender, bhyt, address, doctorId, specName);
                apiSuccess = result.Success;
                if (!string.IsNullOrEmpty(result.TempPassword)) tempPwd = result.TempPassword;
                newPatientId = result.PatientId;
                newApptId = result.AppointmentId;
            }
            catch { }

            // Cập nhật giao diện Tab Tiếp Đón
            int newStt = _gridCheckIn.Rows.Count + 1;
            string newCode = string.Format("RX-{0}-{1:D4}", DateTime.Now.Year, newApptId > 0 ? newApptId : 100 + newStt);
            _gridCheckIn.Rows.Add(newStt, newCode, name.ToUpper(), specName, DateTime.Now.ToString("HH:mm"), "Đã xác nhận", string.Format(" Đã Check-in (STT {0:D2})", newStt));
            _gridCheckIn.Rows[_gridCheckIn.Rows.Count - 1].DefaultCellStyle.BackColor = Color.FromArgb(236, 253, 245);

            ShowReceptionNotification(
                "[*] TẠO HỒ SƠ & BẮN SMS KÍCH HOẠT THÀNH CÔNG",
                $"Bệnh nhân: {name.ToUpper()}\n" +
                $"SĐT: {phone}\n" +
                $"Số CCCD (Đã duyệt): {cccd}\n" +
                $"Chuyên khoa: {specName}\n" +
                $"Mã lịch hẹn: {newCode}\n" +
                $"Số Thứ Tự: STT-{newStt:D2}\n\n" +
                $"[*] TIN NHẮN SMS TỰ ĐỘNG BẮN VỀ SĐT {phone}:\n" +
                $"   • SĐT đăng nhập App Mobile: {phone}\n" +
                $"   • Mật khẩu tạm thời: {tempPwd}\n" +
                $"   • Trạng thái hồ sơ: Đã xác thực CCCD (Verified)\n" +
                $"   • Đã gửi lịch khám STT-{newStt:D2} sang Hàng chờ Bác sĩ!",
                true);

            _txtWalkinName.Text = "";
            _txtWalkinPhone.Text = "";
            _txtWalkinCccd.Text = "";
            _txtWalkinBhyt.Text = "";
            _txtWalkinAddress.Text = "";
            await LoadDataPublicAsync();
            _tabControl.SelectedIndex = 0;
        }
    }
}
