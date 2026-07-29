using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DTT.Doctor.UI.Controls
{
    public static class QrCodePainter
    {
        public static Bitmap GenerateQrBitmap(string payload, int size = 160)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                // Outer Frame Pen & Fill
                using (Pen borderPen = new Pen(Color.FromArgb(15, 23, 42), 3f))
                using (SolidBrush darkBrush = new SolidBrush(Color.FromArgb(15, 23, 42)))
                using (SolidBrush accentBrush = new SolidBrush(Color.FromArgb(37, 99, 235)))
                {
                    // Draw Finder Pattern (Top-Left)
                    DrawFinderPattern(g, 10, 10, 36, darkBrush, accentBrush);
                    // Draw Finder Pattern (Top-Right)
                    DrawFinderPattern(g, size - 46, 10, 36, darkBrush, accentBrush);
                    // Draw Finder Pattern (Bottom-Left)
                    DrawFinderPattern(g, 10, size - 46, 36, darkBrush, accentBrush);

                    // Matrix Data Points (pseudo-random deterministic grid based on payload hash)
                    int hash = payload.GetHashCode();
                    int gridCount = 17;
                    float cellSize = (size - 20) / (float)gridCount;

                    Random rnd = new Random(Math.Abs(hash));
                    for (int r = 0; r < gridCount; r++)
                    {
                        for (int c = 0; c < gridCount; c++)
                        {
                            // Skip finder areas
                            if ((r < 6 && c < 6) || (r < 6 && c > gridCount - 7) || (r > gridCount - 7 && c < 6))
                                continue;

                            if (rnd.Next(100) > 42)
                            {
                                float x = 10 + c * cellSize;
                                float y = 10 + r * cellSize;
                                g.FillRectangle(darkBrush, x + 0.5f, y + 0.5f, cellSize - 1f, cellSize - 1f);
                            }
                        }
                    }
                }
            }
            return bmp;
        }

        private static void DrawFinderPattern(Graphics g, float x, float y, float size, Brush darkBrush, Brush accentBrush)
        {
            using (Pen pen = new Pen(Color.FromArgb(15, 23, 42), 3.5f))
            {
                g.DrawRectangle(pen, x, y, size, size);
            }
            float innerMargin = size * 0.25f;
            float innerSize = size - (innerMargin * 2);
            g.FillRectangle(accentBrush, x + innerMargin, y + innerMargin, innerSize, innerSize);
        }
    }
}
