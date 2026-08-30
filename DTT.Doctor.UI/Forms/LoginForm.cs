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
using CheckBox = System.Windows.Forms.CheckBox;
using DTT.Doctor.Services.Core;

namespace DTT.Doctor.UI.Forms
{
    public partial class LoginForm : Form, ILoginView
    {
        public class RecentUser
        {
            public string Phone { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string RoleCode { get; set; } = string.Empty;
            public string RoleName { get; set; } = string.Empty;
        }

        private const string RecentLoginsFile = "recent_logins.json";
        private List<RecentUser> _recentUsers = new List<RecentUser>();
        private AntiFlickerPanel _pnlMainCard;
        private MaterialTextBoxEdit _txtPhone;
        private MaterialTextBoxEdit _txtPassword;
        private CheckBox _chkRememberPassword;
        private MaterialButton _btnLogin;
        private Label _lblError;
        private MaterialProgressBar _progLoading;
        private LoginPresenter _presenter;

        public LoginForm()
        {
            _presenter = new LoginPresenter(this);
            InitializeComponent();
            SeedDefaultRecentUsersIfEmpty();
            LoadRecentUsers();
            LoadSavedCredentialsIfAny();
        }

        /// Nếu người dùng từng bật "Ghi nhớ mật khẩu", điền sẵn SĐT + mật khẩu đã lưu (đã mã hóa
        /// bằng DPAPI) để không phải gõ lại mỗi lần mở app.
        private void LoadSavedCredentialsIfAny()
        {
            var saved = CredentialVault.TryLoad();
            if (!saved.Found) return;
            _txtPhone.Text = saved.Phone;
            _txtPassword.Text = saved.Password;
            _chkRememberPassword.Checked = true;
        }

        /// <summary>
        /// Lần đầu khởi chạy (chưa có file recent_logins.json) → seed sẵn 5 tài khoản demo
        /// để mục "Gần đây" luôn hiển thị đủ các role cho người dùng thử.
        /// </summary>
        private void SeedDefaultRecentUsersIfEmpty()
        {
            var defaults = new List<RecentUser>
            {
                new RecentUser { Phone = "0900000004", Name = "Nguyễn Thị Minh Châu", RoleCode = "RECEPTIONIST", RoleName = "Lễ tân tiếp đón" },
                new RecentUser { Phone = "0900000006", Name = "KTV. Trần Tuấn Kiệt", RoleCode = "LAB_TECH",     RoleName = "Kỹ thuật viên CLS" },
                new RecentUser { Phone = "0900000005", Name = "Phạm Thị Hồng Hạnh",  RoleCode = "NURSE",         RoleName = "Điều dưỡng" },
                new RecentUser { Phone = "0900000007", Name = "Ds. Trịnh Mai Phương", RoleCode = "PHARMACIST",  RoleName = "Dược sĩ" },
                new RecentUser { Phone = "0901111111", Name = "BS. CKII Nguyễn Văn A", RoleCode = "DOCTOR",    RoleName = "Bác sĩ" },
                new RecentUser { Phone = "0906666666", Name = "BS. CKII Phạm Tuấn Kiệt", RoleCode = "DOCTOR",  RoleName = "Bác sĩ" }
            };

            if (!File.Exists(RecentLoginsFile))
            {
                try { File.WriteAllText(RecentLoginsFile, JsonSerializer.Serialize(defaults)); }
                catch { }
            }
        }

        private void InitializeComponent()
        {
            Text = "DTT Healthcare - CỔNG ĐĂNG NHẬP CHUYÊN KHOA";
            Size = new Size(940, 600);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None; // Flat Borderless
            MaximizeBox = false;
            BackColor = ClinicalColors.DeepNavy; // Vibrant #4338CA
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);
            ClinicalColors.ConfigureMaterialSkin(this);

            _pnlMainCard = new AntiFlickerPanel
            {
                Size = new Size(800, 504),
                Location = new Point((this.ClientSize.Width - 800) / 2, (this.ClientSize.Height - 504) / 2),
                BackColor = Color.White,
                BorderRadius = 16,
                BorderColor = Color.Transparent
            };

            // Custom Close Button
            Button btnClose = new Button
            {
                Text = "✕",
                Font = ClinicalColors.GetMainFont(13f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 40),
                Location = new Point(755, 5),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                Environment.Exit(0);
            };

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
                MaxLength = 10,
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

            _chkRememberPassword = new CheckBox
            {
                Text = "Ghi nhớ mật khẩu",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(390, 292),
                AutoSize = true,
                UseMnemonic = false
            };

            _lblError = new Label
            {
                Text = "",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Italic),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(390, 324),
                Size = new Size(360, 24),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _progLoading = new MaterialProgressBar
            {
                Location = new Point(390, 352),
                Size = new Size(360, 5),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            _btnLogin = new MaterialButton
            {
                Text = "ĐĂNG NHẬP HỆ THỐNG",
                Type = MaterialButton.MaterialButtonType.Contained,
                UseAccentColor = false,
                Location = new Point(390, 374),
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
            _pnlMainCard.Controls.Add(_chkRememberPassword);
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
            // Save to recent logins
            try 
            {
                var newUser = new RecentUser {
                    Phone = _txtPhone.Text,
                    Name = TokenVault.FullName,
                    RoleCode = TokenVault.RoleCode,
                    RoleName = TokenVault.RoleName
                };
                _recentUsers.RemoveAll(u => u.Phone == newUser.Phone);
                _recentUsers.Insert(0, newUser);
                if (_recentUsers.Count > 6) _recentUsers.RemoveAt(_recentUsers.Count - 1);
                File.WriteAllText(RecentLoginsFile, JsonSerializer.Serialize(_recentUsers));
            }
            catch { }

            // Ghi nhớ mật khẩu: chỉ lưu (mã hóa DPAPI) khi người dùng chủ động bật checkbox; nếu
            // tắt (hoặc từng bật rồi tắt lại), xóa hẳn file đã lưu thay vì để sót mật khẩu cũ.
            if (_chkRememberPassword.Checked)
            {
                CredentialVault.Save(_txtPhone.Text.Trim(), _txtPassword.Text);
            }
            else
            {
                CredentialVault.Clear();
            }

            // [Đã gỡ bỏ] Trước đây có cảnh báo "Chế Độ Demo Ngoại Tuyến" khi response.Token ==
            // "demo_jwt_token_doctor_2026" — token demo này được ApiService cấp khi không kết nối
            // được server lúc đăng nhập. Tính năng đăng nhập demo ngoại tuyến đó đã bị gỡ khỏi
            // ApiService (đăng nhập thất bại rõ ràng khi không gọi được API thay vì cấp phiên demo),
            // nên chuỗi token này không bao giờ còn được tạo ra nữa — nhánh kiểm tra đã trở thành
            // dead code và được gỡ bỏ cùng dialog cảnh báo mà nó bảo vệ.

            // Transition to Unified Main Dashboard via Program.cs lifecycle
            this.DialogResult = DialogResult.OK;
            this.Close();
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

            // Đảm bảo đủ các tài khoản mẫu cho người dùng chọn nhanh nếu cần
            var defaultsList = new List<RecentUser>
            {
                new RecentUser { Phone = "0900000004", Name = "Nguyễn Thị Minh Châu", RoleCode = "RECEPTIONIST", RoleName = "Lễ tân tiếp đón" },
                new RecentUser { Phone = "0900000006", Name = "KTV. Trần Tuấn Kiệt", RoleCode = "LAB_TECH",     RoleName = "Kỹ thuật viên CLS" },
                new RecentUser { Phone = "0900000005", Name = "Phạm Thị Hồng Hạnh",  RoleCode = "NURSE",         RoleName = "Điều dưỡng" },
                new RecentUser { Phone = "0900000007", Name = "Ds. Trịnh Mai Phương", RoleCode = "PHARMACIST",  RoleName = "Dược sĩ" },
                new RecentUser { Phone = "0901111111", Name = "BS. CKII Nguyễn Văn A", RoleCode = "DOCTOR",    RoleName = "Bác sĩ" },
                new RecentUser { Phone = "0906666666", Name = "BS. CKII Phạm Tuấn Kiệt", RoleCode = "DOCTOR",  RoleName = "Bác sĩ" }
            };

            foreach (var def in defaultsList)
            {
                if (!_recentUsers.Any(u => u.Phone == def.Phone) && _recentUsers.Count < 6)
                {
                    _recentUsers.Add(def);
                }
            }
            
            if (_recentUsers.Any())
            {
                Label lblRecent = new Label
                {
                    Text = "Gần đây:",
                    Font = ClinicalColors.GetMainFont(9f, FontStyle.Italic),
                    ForeColor = ClinicalColors.TextMuted,
                    Location = new Point(390, 434),
                    AutoSize = true
                };
                _pnlMainCard.Controls.Add(lblRecent);

                FlowLayoutPanel flowRecent = new FlowLayoutPanel
                {
                    Location = new Point(445, 428),
                    Size = new Size(330, 68),
                    BackColor = Color.Transparent,
                    WrapContents = true,
                    AutoScroll = false
                };

                foreach (var user in _recentUsers.Take(6))
                {
                    string displayName = FormatShortDoctorName(user);
                    Button btn = new Button
                    {
                        Text = displayName,
                        Font = ClinicalColors.GetMainFont(8.5f, FontStyle.Bold),
                        BackColor = Color.FromArgb(241, 245, 249),
                        ForeColor = ClinicalColors.TextDark,
                        FlatStyle = FlatStyle.Flat,
                        Size = new Size(102, 28),
                        Margin = new Padding(0, 0, 6, 6),
                        Cursor = Cursors.Hand,
                        UseMnemonic = false
                    };
                    btn.FlatAppearance.BorderColor = ClinicalColors.BorderGray;
                    btn.Click += (s, e) => { _txtPhone.Text = user.Phone; _txtPassword.Focus(); };
                    flowRecent.Controls.Add(btn);
                }

                _pnlMainCard.Controls.Add(flowRecent);
            }
        }

        private static string FormatShortDoctorName(RecentUser user)
        {
            if (user == null) return "Tài khoản";
            string fullName = !string.IsNullOrWhiteSpace(user.Name) ? user.Name.Trim() : string.Empty;
            string roleCode = user.RoleCode ?? string.Empty;
            string roleName = user.RoleName ?? string.Empty;
            string phone = user.Phone ?? string.Empty;

            if (phone == "0900000005" || roleCode == "NURSE" || roleName.Contains("Điều dưỡng"))
            {
                string cleanName = fullName.Replace("ĐD.", "").Replace("ĐD", "").Trim();
                if (string.IsNullOrEmpty(cleanName) || cleanName.Contains("000")) cleanName = "Phạm Thị Hồng Hạnh";
                var partsN = cleanName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                return "ĐD. " + (partsN.Length > 0 ? partsN[partsN.Length - 1] : cleanName);
            }
            if (phone == "0900000004" || roleCode == "RECEPTIONIST" || roleName.Contains("Lễ tân"))
            {
                string cleanName = fullName.Replace("LT.", "").Replace("LT", "").Trim();
                if (string.IsNullOrEmpty(cleanName) || cleanName.Contains("000")) cleanName = "Nguyễn Thị Minh Châu";
                var partsL = cleanName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                return "LT. " + (partsL.Length > 0 ? partsL[partsL.Length - 1] : cleanName);
            }
            if (phone == "0900000006" || roleCode == "LAB_TECH" || roleName.Contains("Kỹ thuật") || fullName.Contains("Kiệt"))
            {
                return "KTV. Kiệt";
            }
            if (phone == "0900000007" || roleCode == "PHARMACIST" || roleName.Contains("Dược sĩ") || fullName.Contains("Phương"))
            {
                return "DS. Phương";
            }

            var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var nameWords = parts.Where(p => {
                string u = p.ToUpperInvariant().TrimEnd('.');
                return u != "THS" && u != "BS" && u != "TS" && u != "CKI" && u != "CKII" && 
                       u != "PGS" && u != "GS" && u != "KTV" && u != "DS" && u != "LT" && u != "ĐD" && u != "ĐIỀU" && u != "TRỊ";
            }).ToList();

            string shortName = fullName;
            if (nameWords.Count >= 2)
            {
                shortName = $"{nameWords[nameWords.Count - 2]} {nameWords[nameWords.Count - 1]}";
            }
            else if (nameWords.Count == 1)
            {
                shortName = nameWords[0];
            }

            if (roleCode == "RECEPTIONIST" || roleName.Contains("Lễ tân")) return $"LT. {shortName}";
            if (roleCode == "NURSE" || roleName.Contains("Điều dưỡng")) return $"ĐD. {shortName}";
            if (roleCode == "DOCTOR" || roleName.Contains("Bác sĩ")) return $"BS. {shortName}";
            if (roleCode == "PHARMACIST" || roleName.Contains("Dược sĩ")) return $"DS. {shortName}";
            if (roleCode == "LAB_TECH" || roleName.Contains("Kỹ thuật")) return $"KTV. {shortName}";

            return $"BS. {shortName}";
        }
    }
}

