using System;
using System.Drawing;
using System.Windows.Forms;
using DTT.Doctor.UI.Theme;

namespace DTT.Doctor.UI.Controls
{
    public class AntiFlickerPanel : Panel
    {
        private int _borderRadius = 0;
        private Color _borderColor = Color.Empty;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = value; Invalidate(); }
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        public AntiFlickerPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);
            UpdateStyles();
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // WS_EX_COMPOSITED (0x02000000): Automatically paints children in bottom-to-top order with double buffering
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_borderRadius > 0)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (var path = CreateRoundedRectPath(ClientRectangle, _borderRadius))
                {
                    // Physically clip window region to removing any external square corners
                    if (this.Region != null) this.Region.Dispose();
                    this.Region = new Region(path);

                    using (var brush = new SolidBrush(BackColor == Color.Transparent ? Color.White : BackColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    if (_borderColor != Color.Empty && _borderColor != Color.Transparent)
                    {
                        // Inset boundary by 1px so stroke is crisp and fully inside the clipped region
                        Rectangle borderRect = new Rectangle(0, 0, Width - 1, Height - 1);
                        using (var borderPath = CreateRoundedRectPath(borderRect, _borderRadius))
                        using (var pen = new Pen(_borderColor, 1.5f))
                        {
                            e.Graphics.DrawPath(pen, borderPath);
                        }
                    }
                }
            }
            else if (BackColor != Color.Transparent)
            {
                if (this.Region != null) { this.Region.Dispose(); this.Region = null; }
                using (var brush = new SolidBrush(BackColor))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d - 1, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d - 1, rect.Bottom - d - 1, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d - 1, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
