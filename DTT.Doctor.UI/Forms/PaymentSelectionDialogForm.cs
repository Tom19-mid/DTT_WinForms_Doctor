using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTT.Doctor.UI.Theme;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;

namespace DTT.Doctor.UI.Forms
{
    public class PaymentSelectionDialogForm : Form
    {
        private readonly int _appointmentId;
        private readonly int _patientId;
        private readonly string _patientName;
        private readonly ApiService _api;

        // UI Controls
        private Label _lblTotalHighlight;
        private Label _lblBreakdown;
        private Panel _pnlContent;

        // Tabs
        private Button _btnTabCash;
        private Button _btnTabVietQr;
        private Button _btnTabPaypal;

        // Active state
        private string _activeTab = "cash"; // "cash", "vietqr", "paypal"
        private decimal _examFee = 250000m;
        private decimal _servicesFee = 0m;
        private decimal _medsFee = 0m;
        private decimal _totalAmount = 250000m;
        private bool _estimateLoadFailed = false;

        // VietQR state
        private VietQrResponse? _vietQrInfo;
        private PictureBox? _picVietQr;

        // PayPal state
        private PaypalInfoResponse? _paypalInfo;
        private PictureBox? _picPaypalQr;
        private System.Windows.Forms.Timer? _pollingTimer;

        public bool PaymentSuccess { get; private set; } = false;
        public int ResultInvoiceId { get; private set; } = 0;
        public string SelectedMethod { get; private set; } = "cash";
        public decimal ResultExamFee => _examFee;
        public decimal ResultServicesFee => _servicesFee;
        public decimal ResultMedsFee => _medsFee;
        public decimal ResultTotalAmount => _totalAmount;

        public PaymentSelectionDialogForm(int appointmentId, int patientId, string patientName, ApiService api)
        {
            _appointmentId = appointmentId;
            _patientId = patientId;
            _patientName = patientName;
            _api = api;

            InitializeComponent();
            LoadDataAsync();
        }

        private void InitializeComponent()
        {
            Text = $"Thanh Toán Viện Phí — {_patientName} (Ca #{_appointmentId})";
            Size = new Size(1360, 750);
            MinimumSize = new Size(1320, 700);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(248, 250, 252);
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

            // ── TOP HEADER ───────────────────────────────────────────────────
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.White,
                Padding = new Padding(24, 12, 24, 12)
            };

            Label lblTitle = new Label
            {
                Text = $"THU VIỆN PHÍ & CHỌN PHƯƠNG THỨC THANH TOÁN",
                Font = ClinicalColors.GetMainFont(13f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(24, 12),
                AutoSize = true
            };

            Label lblPatient = new Label
            {
                Text = $"Bệnh nhân: {_patientName}   •   Mã ca khám: #{_appointmentId}",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(24, 42),
                AutoSize = true
            };

            _lblTotalHighlight = new Label
            {
                Text = "250.000 VNĐ",
                Font = ClinicalColors.GetMainFont(18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                Width = 400
            };

            _lblBreakdown = new Label
            {
                Text = "Công khám: 250.000đ  |  CLS: 0đ  |  Thuốc: 0đ",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(900, 52),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            Panel pnlHeaderBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ClinicalColors.BorderGray
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblPatient);
            pnlHeader.Controls.Add(_lblTotalHighlight);
            pnlHeader.Controls.Add(_lblBreakdown);
            pnlHeader.Controls.Add(pnlHeaderBorder);

            // ── MAIN BODY (LEFT TABS + RIGHT CONTENT) ─────────────────────────
            Panel pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            // Left Navigation Tabs (Rộng 320px để không bị rớt dòng chữ)
            Panel pnlTabs = new Panel
            {
                Dock = DockStyle.Left,
                Width = 320,
                BackColor = Color.White,
                Padding = new Padding(12)
            };

            Label lblSelectMethod = new Label
            {
                Text = "CHỌN HÌNH THỨC:",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Top,
                Height = 30
            };

            _btnTabCash = BuildTabButton("💵  1. Tiền Mặt (Tại Quầy)", "cash", 0);
            // [Old code - Phương thức chuyển khoản VietQR]:
            // _btnTabVietQr = BuildTabButton("🏦  2. Chuyển Khoản (VietQR)", "vietqr", 1);
            // [New code - Chỉ giữ 2 hình thức: Tiền mặt và PayPal]:
            _btnTabPaypal = BuildTabButton("🅿️  2. PayPal (Quét Mã QR)", "paypal", 1);

            _btnTabCash.Click += (s, e) => SwitchTab("cash");
            // _btnTabVietQr.Click += (s, e) => SwitchTab("vietqr");
            _btnTabPaypal.Click += (s, e) => SwitchTab("paypal");

            pnlTabs.Controls.Add(_btnTabPaypal);
            // pnlTabs.Controls.Add(_btnTabVietQr);
            pnlTabs.Controls.Add(_btnTabCash);
            pnlTabs.Controls.Add(lblSelectMethod);

            // Right Content Area
            _pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24),
                Margin = new Padding(16, 0, 0, 0)
            };

            pnlBody.Controls.Add(_pnlContent);
            pnlBody.Controls.Add(pnlTabs);

            Controls.Add(pnlBody);
            Controls.Add(pnlHeader);

            FormClosing += (s, e) => StopPolling();
        }

        private Button BuildTabButton(string text, string tabKey, int index)
        {
            return new Button
            {
                Text = text,
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = tabKey == _activeTab ? ClinicalColors.PrimaryBlue : Color.FromArgb(51, 65, 85),
                BackColor = tabKey == _activeTab ? Color.FromArgb(238, 242, 255) : Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                Dock = DockStyle.Top,
                Height = 56,
                Cursor = Cursors.Hand,
                Tag = tabKey,
                Margin = new Padding(0, 0, 0, 8)
            };
        }

        private async void LoadDataAsync()
        {
            try
            {
                var estimate = await _api.GetInvoiceEstimateAsync(_appointmentId);
                _examFee = estimate.ExamFee;
                _servicesFee = estimate.ServicesFee;
                _medsFee = estimate.MedsFee;
                _totalAmount = estimate.Total;

                if (!estimate.Success)
                {
                    // Không tải được số tiền hóa đơn THẬT — trước đây vẫn hiển thị và cho thu 250.000đ
                    // bịa cứng như thể đó là số tiền thật. Giờ báo lỗi rõ ràng và chặn hẳn việc thu
                    // tiền cho tới khi tải lại thành công (xem cờ _estimateLoadFailed trong ProcessPaymentAsync).
                    _estimateLoadFailed = true;
                    _lblTotalHighlight.Text = "LỖI TẢI DỮ LIỆU";
                    _lblTotalHighlight.ForeColor = Color.FromArgb(220, 38, 38);
                    _lblBreakdown.Text = "Không thể tải số tiền hóa đơn — vui lòng đóng cửa sổ này và thử lại.";
                    MessageBox.Show(
                        "Không thể tải số tiền hóa đơn — vui lòng thử lại.\n\nHệ thống sẽ KHÔNG cho phép xác nhận thu tiền cho tới khi tải lại thành công, để tránh thu sai số tiền.",
                        "Lỗi tải dữ liệu hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    _lblTotalHighlight.Text = $"{_totalAmount:N0} VNĐ";
                    _lblBreakdown.Text = $"Công khám: {_examFee:N0}đ  |  CLS: {_servicesFee:N0}đ  |  Thuốc: {_medsFee:N0}đ";
                }

                // Mặc định mở tab Tiền mặt
                SwitchTab("cash");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PaymentSelectionDialogForm.LoadDataAsync] Error: {ex.Message}");
            }
        }

        private void SwitchTab(string tabKey)
        {
            _activeTab = tabKey;
            StopPolling();

            // Cập nhật màu các nút Tab
            _btnTabCash.BackColor = tabKey == "cash" ? Color.FromArgb(238, 242, 255) : Color.Transparent;
            _btnTabCash.ForeColor = tabKey == "cash" ? ClinicalColors.PrimaryBlue : Color.FromArgb(51, 65, 85);

            // [Old code - VietQR tab color]:
            // _btnTabVietQr.BackColor = tabKey == "vietqr" ? Color.FromArgb(238, 242, 255) : Color.Transparent;
            // _btnTabVietQr.ForeColor = tabKey == "vietqr" ? ClinicalColors.PrimaryBlue : Color.FromArgb(51, 65, 85);

            _btnTabPaypal.BackColor = tabKey == "paypal" ? Color.FromArgb(238, 242, 255) : Color.Transparent;
            _btnTabPaypal.ForeColor = tabKey == "paypal" ? ClinicalColors.PrimaryBlue : Color.FromArgb(51, 65, 85);

            _pnlContent.Controls.Clear();

            switch (tabKey)
            {
                case "cash":
                    RenderCashTab();
                    break;
                // [Old code - RenderVietQrTab]:
                // case "vietqr":
                //     RenderVietQrTab();
                //     break;
                case "paypal":
                    RenderPaypalTab();
                    break;
            }
        }

        // ── 1. RENDER CASH TAB ───────────────────────────────────────────────
        private void RenderCashTab()
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24)
            };

            Label lblHeading = new Label
            {
                Text = "💵  THANH TOÁN BẰNG TIỀN MẶT TẠI QUẦY",
                Font = ClinicalColors.GetMainFont(13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(24, 24),
                AutoSize = true
            };

            Label lblDesc = new Label
            {
                Text = "Lễ Tân nhận tiền mặt trực tiếp từ bệnh nhân hoặc người nhà, sau đó bấm xác nhận để hoàn tất thu phí.",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(24, 60),
                Size = new Size(880, 35)
            };

            Panel pnlAmountBox = new Panel
            {
                BackColor = Color.White,
                Location = new Point(24, 110),
                Size = new Size(880, 130),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblBoxTitle = new Label
            {
                Text = "SỐ TIỀN CẦN THU:",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(24, 18),
                AutoSize = true
            };

            Label lblBoxAmount = new Label
            {
                Text = $"{_totalAmount:N0} VNĐ",
                Font = ClinicalColors.GetMainFont(26f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                Location = new Point(24, 48),
                AutoSize = true
            };

            pnlAmountBox.Controls.Add(lblBoxTitle);
            pnlAmountBox.Controls.Add(lblBoxAmount);

            Button btnConfirmCash = new Button
            {
                Text = "✔  XÁC NHẬN ĐÃ THU ĐỦ TIỀN MẶT",
                Font = ClinicalColors.GetMainFont(12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(16, 185, 129),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Location = new Point(24, 265),
                Size = new Size(880, 56),
                Cursor = Cursors.Hand
            };
            btnConfirmCash.Click += async (s, e) =>
            {
                btnConfirmCash.Enabled = false;
                btnConfirmCash.Text = "Đang xử lý thu tiền...";
                await ProcessPaymentAsync("cash");
                btnConfirmCash.Enabled = true;
            };

            card.Controls.Add(btnConfirmCash);
            card.Controls.Add(pnlAmountBox);
            card.Controls.Add(lblDesc);
            card.Controls.Add(lblHeading);

            _pnlContent.Controls.Add(card);
        }

        // ── 2. [OLD CODE] RENDER VIETQR TAB (ĐÃ LOẠI BỎ THEO YÊU CẦU) ───────
        /*
        private async void RenderVietQrTab()
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(24)
            };

            Label lblHeading = new Label
            {
                Text = "🏦  CHUYỂN KHOẢN NGÂN HÀNG (MÃ VIETQR CHUẨN NAPAS 247)",
                Font = ClinicalColors.GetMainFont(13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(24, 20),
                AutoSize = true
            };

            Label lblDesc = new Label
            {
                Text = "Bệnh nhân dùng bất kỳ App Ngân Hàng nào (VCB, MB, Techcombank, BIDV, Momo...) quét mã để chuyển khoản chính xác.",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(24, 52),
                Size = new Size(880, 25)
            };

            _picVietQr = new PictureBox
            {
                Location = new Point(24, 85),
                Size = new Size(300, 300),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Panel pnlBankInfo = new Panel
            {
                Location = new Point(345, 85),
                Size = new Size(540, 300),
                BackColor = Color.White,
                Padding = new Padding(20),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblBankName = new Label { Text = "Ngân hàng: MB Bank (Quân Đội)", Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(20, 15), AutoSize = true };
            Label lblAccNo = new Label { Text = "STK: 0904444444", Font = ClinicalColors.GetMainFont(12f, FontStyle.Bold), ForeColor = ClinicalColors.PrimaryBlue, Location = new Point(20, 45), AutoSize = true };
            Label lblAccName = new Label { Text = "Chủ TK: PHONG KHAM DA KHOA DTT", Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Location = new Point(20, 75), AutoSize = true };
            Label lblAmount = new Label { Text = $"Số tiền: {_totalAmount:N0} VNĐ", Font = ClinicalColors.GetMainFont(12f, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129), Location = new Point(20, 105), AutoSize = true };
            Label lblContent = new Label { Text = $"Nội dung: DTT CA{_appointmentId}", Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(220, 38, 38), Location = new Point(20, 135), AutoSize = true };

            Button btnConfirmTransfer = new Button
            {
                Text = "✔ XÁC NHẬN ĐÃ NHẬN CHUYỂN KHOẢN",
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(16, 185, 129),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Location = new Point(20, 210),
                Size = new Size(500, 56),
                Cursor = Cursors.Hand
            };

            pnlBankInfo.Controls.Add(btnConfirmTransfer);
            pnlBankInfo.Controls.Add(lblContent);
            pnlBankInfo.Controls.Add(lblAmount);
            pnlBankInfo.Controls.Add(lblAccName);
            pnlBankInfo.Controls.Add(lblAccNo);
            pnlBankInfo.Controls.Add(lblBankName);

            card.Controls.Add(pnlBankInfo);
            card.Controls.Add(_picVietQr);
            card.Controls.Add(lblDesc);
            card.Controls.Add(lblHeading);

            _pnlContent.Controls.Add(card);

            // Tải thông tin VietQR từ backend
            _vietQrInfo = await _api.GetVietQrInfoAsync(_appointmentId);
            if (_vietQrInfo != null)
            {
                lblBankName.Text = $"Ngân hàng: {_vietQrInfo.BankName}";
                lblAccNo.Text = $"STK: {_vietQrInfo.AccountNo}";
                lblAccName.Text = $"Chủ TK: {_vietQrInfo.AccountName}";
                lblAmount.Text = $"Số tiền: {_vietQrInfo.TotalAmount:N0} VNĐ";
                lblContent.Text = $"Nội dung: {_vietQrInfo.TransferContent}";

                if (!string.IsNullOrEmpty(_vietQrInfo.QrUrl))
                {
                    LoadImageAsync(_picVietQr, _vietQrInfo.QrUrl);
                }
            }

            btnConfirmTransfer.Click += async (s, e) =>
            {
                btnConfirmTransfer.Enabled = false;
                btnConfirmTransfer.Text = "Đang xử lý...";
                await ProcessPaymentAsync("bank_transfer");
                btnConfirmTransfer.Enabled = true;
            };
        }
        */

        // ── 3. RENDER PAYPAL TAB (QUÉT MÃ QR BẰNG ĐIỆN THOẠI) ────────────────
        private async void RenderPaypalTab()
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(30, 20, 30, 20)
            };

            Label lblHeading = new Label
            {
                Text = "🅿️  THANH TOÁN PAYPAL (BỆNH NHÂN QUÉT MÃ QR TRÊN ĐIỆN THOẠI)",
                Font = ClinicalColors.GetMainFont(13.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 48, 135),
                Location = new Point(24, 18),
                AutoSize = true
            };

            Label lblDesc = new Label
            {
                Text = "Bệnh nhân mở ứng dụng Camera hoặc PayPal trên điện thoại để quét mã QR bên dưới thanh toán viện phí trực tuyến.",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(24, 48),
                Size = new Size(900, 24)
            };

            // Khung chứa Mã QR bên trái
            Panel pnlQrContainer = new Panel
            {
                Location = new Point(24, 80),
                Size = new Size(330, 440),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            _picPaypalQr = new PictureBox
            {
                Location = new Point(25, 20),
                Size = new Size(280, 280),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };

            Label lblQrCaption = new Label
            {
                Text = "QUÉT MÃ BẰNG\nCAMERA ĐIỆN THOẠI",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 48, 135),
                Location = new Point(10, 315),
                Size = new Size(310, 48),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };

            Label lblQrSub = new Label
            {
                Text = "Hỗ trợ tài khoản PayPal &\nThẻ Quốc tế (Visa / Master)",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(10, 370),
                Size = new Size(310, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };

            pnlQrContainer.Controls.Add(lblQrSub);
            pnlQrContainer.Controls.Add(lblQrCaption);
            pnlQrContainer.Controls.Add(_picPaypalQr);

            // Khung chứa Thông tin chi phí & Trạng thái thanh toán bên phải
            Panel pnlPaypalInfo = new Panel
            {
                Location = new Point(370, 80),
                Size = new Size(545, 440),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(24)
            };

            Label lblPaypalAmtTitle = new Label { Text = "SỐ TIỀN VIỆN PHÍ (USD):", Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(24, 16), AutoSize = true };
            Label lblPaypalAmt = new Label { Text = "$10.00 USD", Font = ClinicalColors.GetMainFont(24f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 112, 186), Location = new Point(24, 38), Size = new Size(495, 60), Padding = new Padding(0, 2, 0, 10), TextAlign = ContentAlignment.MiddleLeft, AutoSize = false };
            Label lblPaypalVnd = new Label { Text = $"≈ {_totalAmount:N0} VNĐ", Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular), ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(24, 104), AutoSize = true };

            Panel pnlDivider = new Panel
            {
                Location = new Point(24, 134),
                Size = new Size(495, 1),
                BackColor = Color.FromArgb(226, 232, 240)
            };

            Label lblGuideTitle = new Label { Text = "[+] HƯỚNG DẪN BỆNH NHÂN THANH TOÁN:", Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59), Location = new Point(24, 146), AutoSize = true };
            Label lblStep1 = new Label { Text = "• Bước 1: Mở ứng dụng Camera điện thoại và quét mã QR bên trái.", Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(24, 174), Size = new Size(495, 22), AutoSize = false };
            Label lblStep2 = new Label { Text = "• Bước 2: Nhấp vào liên kết để mở trang Checkout PayPal bảo mật.", Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(24, 202), Size = new Size(495, 22), AutoSize = false };
            Label lblStep3 = new Label { Text = "• Bước 3: Đăng nhập tài khoản PayPal hoặc chọn Thẻ (Visa / Master).", Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular), ForeColor = Color.FromArgb(51, 65, 85), Location = new Point(24, 230), Size = new Size(495, 22), AutoSize = false };
            Label lblStep4 = new Label { Text = "• Bước 4: Xác nhận thanh toán trên điện thoại để hoàn tất.", Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold), ForeColor = Color.FromArgb(16, 185, 129), Location = new Point(24, 258), Size = new Size(495, 22), AutoSize = false };

            // Khung trạng thái chờ thời gian thực
            Panel pnlStatusBox = new Panel
            {
                Location = new Point(24, 294),
                Size = new Size(495, 120),
                BackColor = Color.FromArgb(255, 247, 237),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblStatusPaypal = new Label
            {
                Text = "⏳ Đang chờ bệnh nhân quét mã và thanh toán trên điện thoại...\n(Hệ thống tự động đồng bộ và xác nhận viện phí ngay khi hoàn tất giao dịch)",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(194, 65, 12),
                Location = new Point(14, 12),
                Size = new Size(467, 95),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };

            pnlStatusBox.Controls.Add(lblStatusPaypal);

            // [Old code - Nút mở trình duyệt & Nút xác nhận thủ công đã được loại bỏ để chỉ cho phép thanh toán qua quét mã QR]:
            /*
            Button btnOpenPaypalBrowser = new Button
            {
                Text = "🌐 Mở Trang Checkout Trình Duyệt",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 112, 186),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Location = new Point(20, 150),
                Size = new Size(500, 48),
                Cursor = Cursors.Hand
            };

            Button btnConfirmManualPaypal = new Button
            {
                Text = "✔ Đã Hoàn Tất Thanh Toán PayPal",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(16, 185, 129),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Location = new Point(20, 210),
                Size = new Size(500, 54),
                Cursor = Cursors.Hand
            };
            */

            pnlPaypalInfo.Controls.Add(pnlStatusBox);
            pnlPaypalInfo.Controls.Add(lblStep4);
            pnlPaypalInfo.Controls.Add(lblStep3);
            pnlPaypalInfo.Controls.Add(lblStep2);
            pnlPaypalInfo.Controls.Add(lblStep1);
            pnlPaypalInfo.Controls.Add(lblGuideTitle);
            pnlPaypalInfo.Controls.Add(pnlDivider);
            pnlPaypalInfo.Controls.Add(lblPaypalVnd);
            pnlPaypalInfo.Controls.Add(lblPaypalAmt);
            pnlPaypalInfo.Controls.Add(lblPaypalAmtTitle);

            card.Controls.Add(pnlPaypalInfo);
            card.Controls.Add(pnlQrContainer);
            card.Controls.Add(lblDesc);
            card.Controls.Add(lblHeading);

            _pnlContent.Controls.Add(card);

            // Tải thông tin PayPal & Mã QR từ backend
            _paypalInfo = await _api.GetPaypalInfoAsync(_appointmentId);
            if (_paypalInfo != null)
            {
                lblPaypalAmt.Text = $"${_paypalInfo.TotalUsd:F2} USD";
                lblPaypalVnd.Text = $"≈ {_paypalInfo.TotalVnd:N0} VNĐ";

                if (!string.IsNullOrEmpty(_paypalInfo.QrUrl))
                {
                    LoadImageAsync(_picPaypalQr, _paypalInfo.QrUrl);
                }

                // [Old code - Click open browser]:
                /*
                btnOpenPaypalBrowser.Click += (s, e) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = _paypalInfo.CheckoutUrl,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không thể mở trình duyệt: " + ex.Message, "PayPal", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                */

                // Bật đồng hồ tự động kiểm tra trạng thái thanh toán sau mỗi 2.5 giây
                StartPolling(lblStatusPaypal);
            }

            // [Old code - Click manual confirm]:
            /*
            btnConfirmManualPaypal.Click += async (s, e) =>
            {
                btnConfirmManualPaypal.Enabled = false;
                await ProcessPaymentAsync("paypal");
                btnConfirmManualPaypal.Enabled = true;
            };
            */
        }

        // ── PAYMENT EXECUTION ────────────────────────────────────────────────
        private async Task ProcessPaymentAsync(string method)
        {
            if (_estimateLoadFailed)
            {
                // Chặn thu tiền khi chưa tải được số tiền hóa đơn THẬT — tránh thu 0đ hoặc một số
                // tiền không có căn cứ. Lễ Tân cần đóng cửa sổ này và mở lại để API thử tải lại.
                MessageBox.Show(
                    "Không thể tải số tiền hóa đơn — vui lòng thử lại.\nVui lòng đóng cửa sổ này và mở lại để tải lại số tiền trước khi thu.",
                    "Lỗi tải dữ liệu hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var result = await _api.ConfirmPaymentAsync(_appointmentId, _patientId, _examFee, _servicesFee, _medsFee, method);
                if (result.Success)
                {
                    PaymentSuccess = true;
                    ResultInvoiceId = result.InvoiceId;
                    SelectedMethod = method;

                    string methodLabel = method switch
                    {
                        "paypal" => "Cổng PayPal (Quốc tế)",
                        "bank_transfer" => "Chuyển khoản Ngân hàng (VietQR)",
                        _ => "Tiền mặt tại quầy"
                    };

                    // [Old code - Hiện popup MessageBox thứ nhất gây trùng lặp với thông báo xanh của Lễ Tân]:
                    /*
                    MessageBox.Show(
                        $"Đã xác nhận thu viện phí thành công!\n\n" +
                        $"Bệnh nhân: {_patientName}\n" +
                        $"Số tiền: {_totalAmount:N0} VNĐ\n" +
                        $"Hình thức: {methodLabel}\n" +
                        $"Hóa đơn: #HD-{result.InvoiceId}",
                        "Thu Ngân Bệnh Viện",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    */

                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show("Xác nhận thanh toán thất bại. Vui lòng kiểm tra lại kết nối mạng!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xử lý thanh toán: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── POLLING PAYPAL STATUS (TỰ ĐỘNG BẮT KHI BỆNH NHÂN THANH TOÁN TRÊN ĐIỆN THOẠI) ──
        private void StartPolling(Label lblStatus)
        {
            StopPolling();
            _pollingTimer = new System.Windows.Forms.Timer { Interval = 2500 };
            _pollingTimer.Tick += async (s, e) =>
            {
                var status = await _api.GetPaymentStatusAsync(_appointmentId);
                if (status != null && status.IsPaid)
                {
                    StopPolling();
                    PaymentSuccess = true;
                    ResultInvoiceId = status.InvoiceId;
                    SelectedMethod = status.PaymentMethod;

                    lblStatus.Text = "✔ BỆNH NHÂN ĐÃ THANH TOÁN PAYPAL THÀNH CÔNG TRÊN ĐIỆN THOẠI!";
                    lblStatus.ForeColor = Color.FromArgb(16, 185, 129);

                    // [Old code - Hiện popup MessageBox thứ nhất gây trùng lặp với thông báo xanh của Lễ Tân]:
                    /*
                    MessageBox.Show(
                        $"Bệnh nhân đã quét mã và thanh toán PayPal thành công!\n\n" +
                        $"Bệnh nhân: {_patientName}\n" +
                        $"Số tiền: {status.TotalAmount:N0} VNĐ\n" +
                        $"Phương thức: PayPal (Quốc tế)\n" +
                        $"Hóa đơn: #HD-{status.InvoiceId}",
                        "Thanh Toán PayPal Thành Công",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    */

                    DialogResult = DialogResult.OK;
                    Close();
                }
            };
            _pollingTimer.Start();
        }

        private void StopPolling()
        {
            if (_pollingTimer != null)
            {
                _pollingTimer.Stop();
                _pollingTimer.Dispose();
                _pollingTimer = null;
            }
        }

        private async void LoadImageAsync(PictureBox? pic, string url)
        {
            if (pic == null || string.IsNullOrEmpty(url)) return;
            try
            {
                using var client = new HttpClient();
                var bytes = await client.GetByteArrayAsync(url);
                using var ms = new MemoryStream(bytes);
                pic.Image = Image.FromStream(ms);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadImageAsync] Error: {ex.Message}");
            }
        }
    }
}
