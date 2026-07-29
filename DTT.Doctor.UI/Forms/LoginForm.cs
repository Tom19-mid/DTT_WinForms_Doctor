using System;
using System.Drawing;
using System.Windows.Forms;
using DTT.Doctor.Presenter.ViewModels;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;

namespace DTT.Doctor.UI.Forms
{
    public partial class LoginForm : Form, ILoginView
    {
        private TextBox _txtPhone;
        private TextBox _txtPassword;
        private Button _btnLogin;
        private Label _lblError;
        private ProgressBar _progLoading;
        private LoginPresenter _presenter;

        public LoginForm()
        {
            _presenter = new LoginPresenter(this);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "DTT Healthcare - Cổng Đăng Nhập Không Gian Bác Sĩ";
            Size = new Size(860, 540);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

            // Left Branding Panel (Deep Navy)
            Panel pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 360,
                BackColor = ClinicalColors.DeepNavy
            };

            CircularLogoControl picLogo = new CircularLogoControl
            {
                Size = new Size(122, 122),
                Location = new Point(119, 36),
                ShadowSpread = 10
            };
            picLogo.LoadImage(@"D:\DoAnTotNghiep\Chức năng của app bệnh nhân\Logo\DTT HEALTHCARE.png");

            Label lblLogo = new Label
            {
                Text = "DTT HEALTHCARE",
                Font = ClinicalColors.GetMainFont(22f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(320, 45),
                Location = new Point(20, 170),
                TextAlign = ContentAlignment.MiddleCenter,
                UseMnemonic = false
            };

            Label lblTagline = new Label
            {
                Text = "Hệ Thống Quản Lý Lâm Sàng & Hàng Chờ Bác Sĩ\n\nKiến trúc MVP 3 Lớp • Bảo mật JWT Token\nKết nối Đồng bộ Thời gian thực",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                AutoSize = false,
                Size = new Size(320, 100),
                Location = new Point(20, 230),
                TextAlign = ContentAlignment.TopCenter,
                UseMnemonic = false
            };

            Label lblVersion = new Label
            {
                Text = "Version 2026 • Build 2.4-Enterprise",
                Font = ClinicalColors.GetMainFont(8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = false,
                Size = new Size(320, 30),
                Location = new Point(20, 460),
                TextAlign = ContentAlignment.MiddleCenter,
                UseMnemonic = false
            };

            pnlLeft.Controls.Add(picLogo);
            pnlLeft.Controls.Add(lblLogo);
            pnlLeft.Controls.Add(lblTagline);
            pnlLeft.Controls.Add(lblVersion);

            // Right Login Container Panel
            Panel pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            Label lblWelcome = new Label
            {
                Text = "Đăng Nhập Không Gian Bác Sĩ",
                Font = ClinicalColors.GetMainFont(18f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                AutoSize = false,
                Size = new Size(400, 40),
                Location = new Point(50, 60),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblSubWelcome = new Label
            {
                Text = "Vui lòng sử dụng tài khoản chuyên gia để bắt đầu trực khám",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                AutoSize = false,
                Size = new Size(400, 25),
                Location = new Point(50, 100),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Username field
            Label lblPhone = new Label
            {
                Text = "Số điện thoại / Tên tài khoản",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(50, 155),
                AutoSize = true
            };

            _txtPhone = new TextBox
            {
                Location = new Point(50, 180),
                Size = new Size(380, 32),
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Regular),
                Text = "0901111111" // Default Demo Doctor A
            };

            // Password field
            Label lblPass = new Label
            {
                Text = "Mật khẩu bảo mật",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(50, 230),
                AutoSize = true
            };

            _txtPassword = new TextBox
            {
                Location = new Point(50, 255),
                Size = new Size(380, 32),
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Regular),
                PasswordChar = '●',
                Text = "Doctor@123"
            };

            _lblError = new Label
            {
                Text = "",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(50, 295),
                Size = new Size(380, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _progLoading = new ProgressBar
            {
                Location = new Point(50, 325),
                Size = new Size(380, 4),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            _btnLogin = new Button
            {
                Text = "Đăng nhập",
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Bold),
                BackColor = ClinicalColors.PrimaryBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(50, 340),
                Size = new Size(380, 44),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            _btnLogin.FlatAppearance.BorderSize = 0;
            _btnLogin.Click += async (s, e) => await _presenter.AttemptLoginAsync(_txtPhone.Text, _txtPassword.Text);

            // Demo Shortcut buttons & CSDL Guidance
            Label lblDemoTip = new Label
            {
                Text = "💡 Hỗ trợ toàn bộ 10 Bác sĩ trong CSDL (0901111111 ➔ 0910000000):",
                Font = ClinicalColors.GetMainFont(8.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(50, 400),
                AutoSize = true
            };

            Button btnDemoDoc1 = new Button
            {
                Text = "👨‍⚕️ BS. Nội (A)",
                Font = ClinicalColors.GetMainFont(8f, FontStyle.Regular),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = ClinicalColors.TextDark,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(50, 425),
                Size = new Size(120, 32),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnDemoDoc1.FlatAppearance.BorderColor = ClinicalColors.BorderGray;
            btnDemoDoc1.Click += (s, e) => { _txtPhone.Text = "0901111111"; _txtPassword.Text = "Doctor@123"; };

            Button btnDemoDoc2 = new Button
            {
                Text = "👨‍⚕️ BS. Tim (C)",
                Font = ClinicalColors.GetMainFont(8f, FontStyle.Regular),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = ClinicalColors.TextDark,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(180, 425),
                Size = new Size(120, 32),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnDemoDoc2.FlatAppearance.BorderColor = ClinicalColors.BorderGray;
            btnDemoDoc2.Click += (s, e) => { _txtPhone.Text = "0902222222"; _txtPassword.Text = "Doctor@123"; };

            Button btnDemoDoc3 = new Button
            {
                Text = "👩‍⚕️ BS. Sản (Hạnh)",
                Font = ClinicalColors.GetMainFont(8f, FontStyle.Regular),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = ClinicalColors.TextDark,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(310, 425),
                Size = new Size(120, 32),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnDemoDoc3.FlatAppearance.BorderColor = ClinicalColors.BorderGray;
            btnDemoDoc3.Click += (s, e) => { _txtPhone.Text = "0905555555"; _txtPassword.Text = "Doctor@123"; };

            pnlRight.Controls.Add(lblWelcome);
            pnlRight.Controls.Add(lblSubWelcome);
            pnlRight.Controls.Add(lblPhone);
            pnlRight.Controls.Add(_txtPhone);
            pnlRight.Controls.Add(lblPass);
            pnlRight.Controls.Add(_txtPassword);
            pnlRight.Controls.Add(_lblError);
            pnlRight.Controls.Add(_progLoading);
            pnlRight.Controls.Add(_btnLogin);
            pnlRight.Controls.Add(lblDemoTip);
            pnlRight.Controls.Add(btnDemoDoc1);
            pnlRight.Controls.Add(btnDemoDoc2);
            pnlRight.Controls.Add(btnDemoDoc3);

            Controls.Add(pnlRight);
            Controls.Add(pnlLeft);

            AcceptButton = _btnLogin;
        }

        public void ShowLoading(bool isLoading)
        {
            _btnLogin.Enabled = !isLoading;
            _txtPhone.Enabled = !isLoading;
            _txtPassword.Enabled = !isLoading;
            _progLoading.Visible = isLoading;
            if (isLoading) _lblError.Text = "";
        }

        public void OnLoginSuccess(DoctorAuthResponseDto response)
        {
            _lblError.Text = "";
            
            // Transition to Main Dashboard
            this.Hide();
            var dashboard = new MainDashboardForm();
            dashboard.FormClosed += (s, e) => this.Close();
            dashboard.Show();
        }

        public void OnLoginFailure(string errorMessage)
        {
            _lblError.ForeColor = Color.FromArgb(220, 38, 38);
            _lblError.Text = "❌ " + errorMessage;
        }
    }
}
