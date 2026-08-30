using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DTT.Doctor.UI.Theme;

namespace DTT.Doctor.UI.Controls
{
    public class RoundedButton : Button
    {
        public int BorderRadius { get; set; } = 16;
        public Color BorderColor { get; set; } = Color.Transparent;
        public int BorderSize { get; set; } = 0;
        public Color HoverBackColor { get; set; } = Color.FromArgb(37, 99, 235);
        public Color NormalBackColor { get; set; } = ClinicalColors.PrimaryBlue;
        private bool _isHovered = false;
        private bool _isFlashing = false;
        private Color _flashColor;

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            UseMnemonic = false;
            DoubleBuffered = true;
            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
        }

        // Nhấp nháy nhanh khi bấm — mô phỏng phản hồi "F5 làm mới" trên Windows để người dùng biết
        // thao tác đã thực sự được ghi nhận, không chỉ im lặng chờ dữ liệu tải xong.
        public void Flash()
        {
            if (IsDisposed) return;
            _flashColor = ControlPaint.Light(BackColor, 0.7f);
            int step = 0;
            const int totalSteps = 4; // sáng - tối - sáng - tối
            var timer = new System.Windows.Forms.Timer { Interval = 90 };
            timer.Tick += (s, e) =>
            {
                if (IsDisposed) { timer.Stop(); timer.Dispose(); return; }
                _isFlashing = step % 2 == 0;
                Invalidate();
                step++;
                if (step >= totalSteps)
                {
                    timer.Stop();
                    timer.Dispose();
                    _isFlashing = false;
                    Invalidate();
                }
            };
            timer.Start();
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            if (Parent != null)
            {
                using (var brush = new SolidBrush(Parent.BackColor))
                {
                    pevent.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            Color currentBack = _isFlashing ? _flashColor : (_isHovered && Enabled ? HoverBackColor : BackColor);

            using (var path = CreateRoundedPath(rect, BorderRadius))
            {
                var oldReg = this.Region;
                this.Region = new Region(path);
                oldReg?.Dispose();

                using (var brush = new SolidBrush(currentBack))
                {
                    pevent.Graphics.FillPath(brush, path);
                }
                if (BorderSize > 0 && BorderColor != Color.Transparent)
                {
                    using (var pen = new Pen(BorderColor, BorderSize))
                    {
                        pevent.Graphics.DrawPath(pen, path);
                    }
                }
            }

            TextRenderer.DrawText(pevent.Graphics, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        private GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
