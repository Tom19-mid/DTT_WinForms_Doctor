using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace DTT.Doctor.UI.Controls
{
    public class CircularLogoControl : Control
    {
        private Image _logoImage;
        public int ShadowSpread { get; set; } = 8;

        public CircularLogoControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(110, 110);
        }

        public void LoadImage(string absolutePath)
        {
            try
            {
                if (File.Exists(absolutePath))
                {
                    _logoImage = Image.FromFile(absolutePath);
                    Invalidate();
                }
            }
            catch { }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            base.OnPaintBackground(pevent);
            if (Parent != null && BackColor == Color.Transparent)
            {
                using (var brush = new SolidBrush(Parent.BackColor))
                {
                    pevent.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int pad = ShadowSpread;
            Rectangle logoRect = new Rectangle(pad, pad, Width - pad * 2 - 1, Height - pad * 2 - 1);

            // Draw Soft Dark Shadow & Blur Ring outside the circle (viền mờ đen bên ngoài)
            int shadowLayers = ShadowSpread;
            for (int i = shadowLayers; i >= 1; i--)
            {
                int alpha = (int)(45 * ((float)(shadowLayers - i + 1) / shadowLayers));
                if (alpha > 255) alpha = 255;
                using (var pen = new Pen(Color.FromArgb(alpha, 15, 23, 42), 1.8f))
                {
                    e.Graphics.DrawEllipse(pen, logoRect.X - i, logoRect.Y - i, logoRect.Width + i * 2, logoRect.Height + i * 2);
                }
            }

            // Draw white base circle inside shadow
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(logoRect);
                using (var whiteBrush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillPath(whiteBrush, path);
                }

                // Clip and draw image inside circle
                e.Graphics.SetClip(path);
                if (_logoImage != null)
                {
                    e.Graphics.DrawImage(_logoImage, logoRect);
                }
                else
                {
                    // Fallback text if image missing
                    using (var font = new Font("Segoe UI", 9f, FontStyle.Bold))
                    using (var textBrush = new SolidBrush(Color.FromArgb(30, 58, 138)))
                    {
                        var stringFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString("DTT\nHEALTH", font, textBrush, logoRect, stringFormat);
                    }
                }
                e.Graphics.ResetClip();
            }

            // Draw an elegant inner dark ring to finalize the circular aesthetic
            using (var ringPen = new Pen(Color.FromArgb(80, 0, 0, 0), 1.5f))
            {
                e.Graphics.DrawEllipse(ringPen, logoRect);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _logoImage != null)
            {
                _logoImage.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
