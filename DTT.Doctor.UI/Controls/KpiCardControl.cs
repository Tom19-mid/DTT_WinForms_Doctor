using System;
using System.Drawing;
using System.Windows.Forms;
using DTT.Doctor.UI.Theme;

namespace DTT.Doctor.UI.Controls
{
    public class KpiCardControl : AntiFlickerPanel
    {
        private Label _lblTitle;
        private Label _lblValue;
        private Panel _iconPanel;
        private Label _lblIcon;
        private Color _accentColor = ClinicalColors.PrimaryBlue;
        private Color _accentBgColor = Color.FromArgb(219, 234, 254);

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get => _lblTitle.Text;
            set => _lblTitle.Text = value;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Value
        {
            get => _lblValue.Text;
            set => _lblValue.Text = value;
        }

        public KpiCardControl(string title, string initialValue, string iconSymbol, Color accentText, Color accentBg)
        {
            _accentColor = accentText;
            _accentBgColor = accentBg;
            BorderRadius = 12;
            BorderColor = ClinicalColors.BorderGray;
            BackColor = Color.White;
            Size = new Size(240, 96);
            Padding = new Padding(16);

            _iconPanel = new Panel
            {
                Size = new Size(52, 52),
                Location = new Point(12, 22),
                BackColor = Color.Transparent
            };

            _lblIcon = new Label
            {
                Text = iconSymbol,
                Font = new Font("Segoe UI Emoji", 24f, FontStyle.Regular),
                ForeColor = _accentColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            _iconPanel.Controls.Add(_lblIcon);

            _lblValue = new Label
            {
                Text = initialValue,
                Font = ClinicalColors.GetMainFont(20f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(76, 20),
                Size = new Size(150, 32),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblTitle = new Label
            {
                Text = title,
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(76, 52),
                Size = new Size(150, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Controls.Add(_lblValue);
            Controls.Add(_lblTitle);
            Controls.Add(_iconPanel);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }
    }
}
