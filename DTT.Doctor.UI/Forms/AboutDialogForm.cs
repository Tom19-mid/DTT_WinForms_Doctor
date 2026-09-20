using System;
using System.Drawing;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;

namespace DTT.Doctor.UI.Forms
{
    // Hộp thoại "Thông tin": tên + phiên bản phần mềm, người đang đăng nhập, máy chủ đang dùng
    // và trạng thái kết nối tới máy chủ (hữu ích khi backend trên Render đang "ngủ" hoặc mất mạng).
    public class AboutDialogForm : Form
    {
        private readonly string _serverUrl;
        private Label _lblStatus;
        private RoundedButton _btnCheck;

        public AboutDialogForm()
        {
            _serverUrl = new ApiService().BaseUrl;
            InitializeComponent();
            Shown += async (s, e) => await CheckConnectionAsync();
        }

        private void InitializeComponent()
        {
            Text = "Thông tin phần mềm";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(460, 500); // đặt sau FormBorderStyle để kích thước vùng nội dung đúng như thiết kế
            BackColor = Color.White;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

            var logo = new CircularLogoControl { Size = new Size(120, 120), Location = new Point((ClientSize.Width - 120) / 2, 20) };
            logo.LoadAppLogo();

            var lblName = new Label
            {
                Text = "DTT Healthcare",
                Font = ClinicalColors.GetMainFont(17f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 152),
                Size = new Size(ClientSize.Width, 32)
            };
            var lblSub = new Label
            {
                Text = "Phần mềm quản lý phòng khám — Doctor Desktop",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 184),
                Size = new Size(ClientSize.Width, 22)
            };

            var divider = new Panel { BackColor = ClinicalColors.BorderGray, Location = new Point(30, 218), Size = new Size(ClientSize.Width - 60, 1) };

            int y = 232;
            AddRow("Phiên bản", GetVersionText(), ref y);
            AddRow("Người dùng", string.IsNullOrEmpty(TokenVault.FullName) ? "—" : TokenVault.FullName, ref y);
            AddRow("Vai trò", string.IsNullOrEmpty(TokenVault.RoleName) ? "—" : TokenVault.RoleName, ref y);
            AddRow("Máy chủ", _serverUrl, ref y);

            var lblStatusKey = MakeKeyLabel("Kết nối", y);
            _lblStatus = new Label
            {
                Text = "● Đang kiểm tra...",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(140, y),
                Size = new Size(ClientSize.Width - 170, 54),
                AutoSize = false
            };
            y += 58;

            _btnCheck = new RoundedButton
            {
                Text = "Kiểm tra lại",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                BackColor = Color.FromArgb(241, 245, 249),
                HoverBackColor = Color.FromArgb(226, 232, 240),
                BorderRadius = 10,
                Size = new Size(130, 38),
                Location = new Point(ClientSize.Width / 2 - 140, y)
            };
            _btnCheck.Click += async (s, e) => await CheckConnectionAsync();

            var btnClose = new RoundedButton
            {
                Text = "Đóng",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ClinicalColors.PrimaryBlue,
                HoverBackColor = Color.FromArgb(37, 99, 235),
                BorderRadius = 10,
                Size = new Size(130, 38),
                Location = new Point(ClientSize.Width / 2 + 10, y),
                DialogResult = DialogResult.Cancel
            };
            CancelButton = btnClose;

            var lblFooter = new Label
            {
                Text = "© 2026 DTT Healthcare",
                Font = ClinicalColors.GetMainFont(8.5f, FontStyle.Italic),
                ForeColor = ClinicalColors.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, ClientSize.Height - 34),
                Size = new Size(ClientSize.Width, 22)
            };

            Controls.AddRange(new Control[] { logo, lblName, lblSub, divider, lblStatusKey, _lblStatus, _btnCheck, btnClose, lblFooter });
        }

        private void AddRow(string key, string value, ref int y)
        {
            Controls.Add(MakeKeyLabel(key, y));
            Controls.Add(new Label
            {
                Text = value,
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(140, y),
                Size = new Size(ClientSize.Width - 170, 22),
                AutoEllipsis = true
            });
            y += 30;
        }

        private static Label MakeKeyLabel(string key, int y) => new Label
        {
            Text = key,
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
            ForeColor = ClinicalColors.TextMuted,
            Location = new Point(30, y),
            Size = new Size(105, 22)
        };

        private static string GetVersionText()
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v == null ? "1.0.0" : v.ToString(3);
        }

        // Coi máy chủ là "đã kết nối" khi nhận được bất kỳ phản hồi HTTP nào dưới 500 (kể cả 401/404 —
        // nghĩa là máy chủ đang chạy). Lỗi mạng, hết thời gian chờ hoặc 5xx (Render đang khởi động) là chưa kết nối.
        private async Task CheckConnectionAsync()
        {
            _btnCheck.Enabled = false;
            _lblStatus.ForeColor = ClinicalColors.TextMuted;
            _lblStatus.Text = "● Đang kiểm tra...";
            try
            {
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
                {
                    var resp = await http.GetAsync(_serverUrl.TrimEnd('/') + "/");
                    if (IsDisposed) return;
                    if ((int)resp.StatusCode < 500)
                    {
                        _lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
                        _lblStatus.Text = "● Đã kết nối tới máy chủ";
                    }
                    else
                    {
                        ShowDisconnected();
                    }
                }
            }
            catch
            {
                if (!IsDisposed) ShowDisconnected();
            }
            finally
            {
                if (!IsDisposed) _btnCheck.Enabled = true;
            }
        }

        private void ShowDisconnected()
        {
            _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
            _lblStatus.Text = "● Không kết nối được. Máy chủ có thể đang khởi động, vui lòng thử lại sau ít phút.";
        }
    }
}
