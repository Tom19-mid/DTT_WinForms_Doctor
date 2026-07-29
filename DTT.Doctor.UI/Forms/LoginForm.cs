using System;
using System.Drawing;
using System.Windows.Forms;
using DTT.Doctor.Presenter.ViewModels;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using Panel = System.Windows.Forms.Panel;

namespace DTT.Doctor.UI.Forms
{
    public partial class LoginForm : MaterialForm, ILoginView
    {
        private MaterialTextBoxEdit _txtPhone;
        private MaterialTextBoxEdit _txtPassword;
        private MaterialButton _btnLogin;
        private Label _lblError;
        private MaterialProgressBar _progLoading;
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
            MaximizeBox = false;
            Sizable = false;
            BackColor = Color.White;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);
            ClinicalColors.ConfigureMaterialSkin(this);

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
                Size = new Size(420, 40),
                Location = new Point(40, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblSubWelcome = new Label
            {
                Text = "Vui lòng sử dụng tài khoản chuyên gia để bắt đầu trực khám",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                AutoSize = false,
                Size = new Size(420, 25),
                Location = new Point(40, 80),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _txtPhone = new MaterialTextBoxEdit
            {
                Location = new Point(40, 125),
                Size = new Size(400, 48),
                Hint = "Số điện thoại / Tên tài khoản",
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Regular),
                Text = "0901111111"
            };

            _txtPassword = new MaterialTextBoxEdit
            {
                Location = new Point(40, 190),
                Size = new Size(400, 48),
                Hint = "Mật khẩu bảo mật",
                PasswordChar = '●',
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Regular),
                Text = "Doctor@123"
            };

            _lblError = new Label
            {
                Text = "",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(40, 248),
                Size = new Size(400, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _progLoading = new MaterialProgressBar
            {
                Location = new Point(40, 275),
                Size = new Size(400, 5),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            _btnLogin = new MaterialButton
            {
                Text = "ĐĂNG NHẬP HỆ THỐNG",
                Type = MaterialButton.MaterialButtonType.Contained,
                UseAccentColor = false,
                Location = new Point(40, 295),
                Size = new Size(400, 44),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            _btnLogin.Click += async (s, e) => await _presenter.AttemptLoginAsync(_txtPhone.Text, _txtPassword.Text);

            // Demo Shortcut buttons & CSDL Guidance
            Label lblDemoTip = new Label
            {
                Text = "💡 Hỗ trợ toàn bộ 10 Bác sĩ trong CSDL (0901111111 ➔ 0910000000):",
                Font = ClinicalColors.GetMainFont(8.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(40, 365),
                AutoSize = true
            };

            MaterialButton btnDemoDoc1 = new MaterialButton
            {
                Text = "BS. NỘI (A)",
                Type = MaterialButton.MaterialButtonType.Outlined,
                Density = MaterialButton.MaterialButtonDensity.Dense,
                Location = new Point(40, 395),
                Size = new Size(125, 36),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnDemoDoc1.Click += (s, e) => { _txtPhone.Text = "0901111111"; _txtPassword.Text = "Doctor@123"; };

            MaterialButton btnDemoDoc2 = new MaterialButton
            {
                Text = "BS. TIM (C)",
                Type = MaterialButton.MaterialButtonType.Outlined,
                Density = MaterialButton.MaterialButtonDensity.Dense,
                Location = new Point(178, 395),
                Size = new Size(125, 36),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnDemoDoc2.Click += (s, e) => { _txtPhone.Text = "0903333333"; _txtPassword.Text = "Doctor@123"; };

            MaterialButton btnDemoDoc3 = new MaterialButton
            {
                Text = "BS. NHI (E)",
                Type = MaterialButton.MaterialButtonType.Outlined,
                Density = MaterialButton.MaterialButtonDensity.Dense,
                Location = new Point(315, 395),
                Size = new Size(125, 36),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnDemoDoc3.Click += (s, e) => { _txtPhone.Text = "0905555555"; _txtPassword.Text = "Doctor@123"; };

            pnlRight.Controls.Add(lblWelcome);
            pnlRight.Controls.Add(lblSubWelcome);
            pnlRight.Controls.Add(_txtPhone);
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
