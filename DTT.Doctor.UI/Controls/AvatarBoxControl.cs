using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.UI.Theme;

namespace DTT.Doctor.UI.Controls
{
    public class AvatarBoxControl : Panel
    {
        private PictureBox _picAvatar;
        private Label _lblInitials;

        public AvatarBoxControl(int size = 48)
        {
            Size = new Size(size, size);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            DoubleBuffered = true;

            _lblInitials = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = ClinicalColors.GetMainFont(size > 40 ? 13f : 10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ClinicalColors.PrimaryBlue
            };

            _picAvatar = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Visible = false
            };

            Controls.Add(_picAvatar);
            Controls.Add(_lblInitials);

            LoadDoctorAvatar();

            // Interactive hover effect
            MouseEnter += (s, e) => { Invalidate(); };
            MouseLeave += (s, e) => { Invalidate(); };
            _lblInitials.MouseEnter += (s, e) => { OnMouseEnter(e); };
            _lblInitials.MouseLeave += (s, e) => { OnMouseLeave(e); };
            _picAvatar.MouseEnter += (s, e) => { OnMouseEnter(e); };
            _picAvatar.MouseLeave += (s, e) => { OnMouseLeave(e); };
        }

        public void LoadDoctorAvatar()
        {
            string initials = "NV";
            string cleanName = !string.IsNullOrEmpty(TokenVault.FullName) ? TokenVault.FullName : TokenVault.GetFormattedTitleName();
            cleanName = cleanName.Replace("BS. CKII ", "").Replace("BS. CKI ", "").Replace("BS. ", "").Replace("ThS. BS ", "").Replace("TS. BS ", "").Replace("LT. ", "").Replace("ĐD. ", "").Replace("DS. ", "").Replace("KTV. ", "").Trim();

            if (!string.IsNullOrEmpty(cleanName))
            {
                var parts = cleanName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    initials = (parts[parts.Length - 2][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
                }
                else if (parts.Length == 1)
                {
                    initials = parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
                }
            }
            _lblInitials.Text = initials;

            string avatarUrl = TokenVault.AvatarUrl;
            // Lookup avatar from database doctors.csv if available
            string dbPath = @"D:\DoAnTotNghiep\Chức năng của app bệnh nhân\db\doctors.csv";
            if (File.Exists(dbPath))
            {
                try
                {
                    var lines = File.ReadAllLines(dbPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith(TokenVault.DoctorId.ToString() + ",") || line.StartsWith("\"" + TokenVault.DoctorId.ToString() + "\""))
                        {
                            var tokens = line.Split(',');
                            if (tokens.Length >= 13)
                            {
                                string url = tokens[12].Trim('\"', ' ');
                                if (!string.IsNullOrEmpty(url) && url != "NULL" && url != "null")
                                {
                                    avatarUrl = url;
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(avatarUrl) && File.Exists(avatarUrl))
            {
                try
                {
                    _picAvatar.Image = Image.FromFile(avatarUrl);
                    _picAvatar.Visible = true;
                    _lblInitials.Visible = false;
                }
                catch
                {
                    _picAvatar.Visible = false;
                    _lblInitials.Visible = true;
                }
            }
            else
            {
                _picAvatar.Visible = false;
                _lblInitials.Visible = true;
            }

            // Apply round mask
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, Width - 1, Height - 1);
            _lblInitials.Region = new Region(path);
            if (_picAvatar.Visible) _picAvatar.Region = new Region(path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Color.FromArgb(203, 213, 225), 2f))
            {
                e.Graphics.DrawEllipse(pen, 0, 0, Width - 2, Height - 2);
            }
        }
    }
}
