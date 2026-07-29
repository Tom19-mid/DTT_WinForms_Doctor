using System;
using System.Drawing;
using System.Windows.Forms;
using DTT.Doctor.Presenter.ViewModels;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Panel = System.Windows.Forms.Panel;
using Button = System.Windows.Forms.Button;
using DTT.Doctor.Services.Core;

namespace DTT.Doctor.UI.Forms
{
    public partial class LoginForm : Form, ILoginView
    {
        public class RecentUser
        {
            public string Phone { get; set; }
            public string Name { get; set; }
        }

        private const string RecentLoginsFile = "recent_logins.json";
        private List<RecentUser> _recentUsers = new List<RecentUser>();
        private AntiFlickerPanel _pnlMainCard;
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
            LoadRecentUsers();
        }

        private void InitializeComponent()
        {
            Text = "DTT Healthcare - CỔNG ĐĂNG NHẬP CHUYÊN KHOA";
            Size = new Size(940, 580);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None; // Flat Borderless
            MaximizeBox = false;
            BackColor = ClinicalColors.DeepNavy; // Vibrant #4338CA
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);
            ClinicalColors.ConfigureMaterialSkin(this);

            _pnlMainCard = new AntiFlickerPanel
            {
                Size = new Size(800, 480),
                Location = new Point((this.ClientSize.Width - 800) / 2, (this.ClientSize.Height - 480) / 2),
                BackColor = Color.White,
                BorderRadius = 16,
                BorderColor = Color.Transparent
            };

            // Custom Close Button
            Button btnClose = new Button
            {
                Text = "X",
                Font = ClinicalColors.GetMainFont(12f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 40),
                Location = new Point(760, 0),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Application.Exit();

            // Form Dragging Logic
            bool dragging = false;
            Point dragCursorPoint = Point.Empty;
            Point dragFormPoint = Point.Empty;

            var mouseDown = new MouseEventHandler((s, e) => {
                if (e.Button == MouseButtons.Left) {
                    dragging = true;
                    dragCursorPoint = Cursor.Position;
                    dragFormPoint = this.Location;
                }
            });
            var mouseMove = new MouseEventHandler((s, e) => {
                if (dragging) {
                    Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                    this.Location = Point.Add(dragFormPoint, new Size(dif));
                }
            });
            var mouseUp = new MouseEventHandler((s, e) => { dragging = false; });
            this.MouseDown += mouseDown;
            this.MouseMove += mouseMove;
            this.MouseUp += mouseUp;
            _pnlMainCard.MouseDown += mouseDown;
            _pnlMainCard.MouseMove += mouseMove;
            _pnlMainCard.MouseUp += mouseUp;

            CircularLogoControl picLogo = new CircularLogoControl
            {
                Size = new Size(200, 200),
                Location = new Point(80, 140),
                ShadowSpread = 10
            };
            picLogo.LoadImage(@"D:\DoAnTotNghiep\Chức năng của app bệnh nhân\Logo\DTT HEALTHCARE.png");

            Label lblWelcome = new Label
            {
                Text = "Đăng Nhập",
                Font = ClinicalColors.GetMainFont(22f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                AutoSize = false,
                Size = new Size(360, 45),
                Location = new Point(390, 70),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblSubWelcome = new Label
            {
                Text = "Vui lòng đăng nhập để bắt đầu phiên làm việc",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                AutoSize = false,
                Size = new Size(360, 30),
                Location = new Point(390, 115),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _txtPhone = new MaterialTextBoxEdit
            {
                Location = new Point(390, 170),
                Size = new Size(360, 48),
                Hint = "Số điện thoại",
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Regular)
            };

            _txtPassword = new MaterialTextBoxEdit
            {
                Location = new Point(390, 240),
                Size = new Size(360, 48),
                Hint = "Mật khẩu bảo mật",
                PasswordChar = '●',
                Font = ClinicalColors.GetMainFont(11f, FontStyle.Regular)
            };

            _lblError = new Label
            {
                Text = "",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Italic),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(390, 300),
                Size = new Size(360, 24),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _progLoading = new MaterialProgressBar
            {
                Location = new Point(390, 328),
                Size = new Size(360, 5),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            _btnLogin = new MaterialButton
            {
                Text = "ĐĂNG NHẬP HỆ THỐNG",
                Type = MaterialButton.MaterialButtonType.Contained,
                UseAccentColor = false,
                Location = new Point(390, 350),
                Size = new Size(360, 48),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            _btnLogin.Click += async (s, e) => await _presenter.AttemptLoginAsync(_txtPhone.Text, _txtPassword.Text);

            _pnlMainCard.Controls.Add(btnClose);
            _pnlMainCard.Controls.Add(picLogo);
            _pnlMainCard.Controls.Add(lblWelcome);
            _pnlMainCard.Controls.Add(lblSubWelcome);
            _pnlMainCard.Controls.Add(_txtPhone);
            _pnlMainCard.Controls.Add(_txtPassword);
            _pnlMainCard.Controls.Add(_lblError);
            _pnlMainCard.Controls.Add(_progLoading);
            _pnlMainCard.Controls.Add(_btnLogin);

            Controls.Add(_pnlMainCard);

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
            
            // Save to recent logins
            try 
            {
                var newUser = new RecentUser { Phone = _txtPhone.Text, Name = TokenVault.FullName };
                _recentUsers.RemoveAll(u => u.Phone == newUser.Phone);
                _recentUsers.Insert(0, newUser);
                if (_recentUsers.Count > 3) _recentUsers.RemoveAt(_recentUsers.Count - 1);
                File.WriteAllText(RecentLoginsFile, JsonSerializer.Serialize(_recentUsers));
            } 
            catch { }
            
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

        private void LoadRecentUsers()
        {
            if (File.Exists(RecentLoginsFile))
            {
                try {
                    _recentUsers = JsonSerializer.Deserialize<List<RecentUser>>(File.ReadAllText(RecentLoginsFile)) ?? new List<RecentUser>();
                } catch { }
            }
            
            if (_recentUsers.Any())
            {
                Label lblRecent = new Label
                {
                    Text = "Gần đây:",
                    Font = ClinicalColors.GetMainFont(9f, FontStyle.Italic),
                    ForeColor = ClinicalColors.TextMuted,
                    Location = new Point(390, 415),
                    AutoSize = true
                };
                _pnlMainCard.Controls.Add(lblRecent);

                int currentX = 445;
                foreach (var user in _recentUsers)
                {
                    string displayName = FormatShortDoctorName(user.Name);
                    Button btn = new Button
                    {
                        Text = displayName,
                        Font = ClinicalColors.GetMainFont(8.5f, FontStyle.Bold),
                        BackColor = Color.FromArgb(241, 245, 249),
                        ForeColor = ClinicalColors.TextDark,
                        FlatStyle = FlatStyle.Flat,
                        Location = new Point(currentX, 410),
                        Size = new Size(105, 30),
                        Cursor = Cursors.Hand,
                        UseMnemonic = false
                    };
                    btn.FlatAppearance.BorderColor = ClinicalColors.BorderGray;
                    btn.Click += (s, e) => { _txtPhone.Text = user.Phone; _txtPassword.Focus(); };
                    _pnlMainCard.Controls.Add(btn);
                    currentX += 112;
                }
            }
        }

        private static string FormatShortDoctorName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "Bác sĩ";

            var parts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 1) return fullName;

            var degrees = new List<string>();
            var nameParts = new List<string>();

            foreach (var p in parts)
            {
                string upper = p.ToUpperInvariant().TrimEnd('.');
                if (upper == "THS" || upper == "BS" || upper == "TS" || 
                    upper == "CKI" || upper == "CKII" || upper == "PGS" || upper == "GS")
                {
                    degrees.Add(p);
                }
                else
                {
                    nameParts.Add(p);
                }
            }

            string degreeStr = degrees.Count > 0 ? string.Join(" ", degrees) : "BS.";
            string lastName = nameParts.Count > 0 ? nameParts[nameParts.Count - 1] : "";

            return $"{degreeStr} {lastName}".Trim();
        }
    }
}
