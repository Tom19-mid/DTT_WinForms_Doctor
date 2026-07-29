using System;
using System.Drawing;
using System.Windows.Forms;
using DTT.Doctor.UI.Theme;

namespace DTT.Doctor.UI.Controls
{
    public class AntiFlickerDataGridView : DataGridView
    {
        private int _hoveredRow = -1;
        private int _hoveredCol = -1;

        public AntiFlickerDataGridView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            DoubleBuffered = true;

            // Modern Styling Defaults
            BackgroundColor = ClinicalColors.GhostWhite;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            EnableHeadersVisualStyles = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MultiSelect = false;
            ReadOnly = true;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            RowHeadersVisible = false;
            RowTemplate.Height = 46;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Header Style
            ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            ColumnHeadersDefaultCellStyle.ForeColor = ClinicalColors.TextDark;
            ColumnHeadersDefaultCellStyle.Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold);
            ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            ColumnHeadersHeight = 40;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // Rows Style
            DefaultCellStyle.BackColor = Color.White;
            DefaultCellStyle.ForeColor = ClinicalColors.TextDark;
            DefaultCellStyle.Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);
            DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 242, 255);
            DefaultCellStyle.SelectionForeColor = ClinicalColors.PrimaryBlue;
            DefaultCellStyle.Padding = new Padding(12, 0, 0, 0);

            AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
            GridColor = ClinicalColors.BorderGray;

            CellPainting += OnCellPainting;

            CellMouseMove += (s, e) => {
                if (_hoveredRow != e.RowIndex || _hoveredCol != e.ColumnIndex)
                {
                    int oldRow = _hoveredRow;
                    _hoveredRow = e.RowIndex;
                    _hoveredCol = e.ColumnIndex;
                    if (oldRow >= 0 && oldRow < Rows.Count) InvalidateRow(oldRow);
                    if (_hoveredRow >= 0 && _hoveredRow < Rows.Count) InvalidateRow(_hoveredRow);
                    Cursor = (e.RowIndex >= 0) ? Cursors.Hand : Cursors.Default;
                }
            };
            CellMouseLeave += (s, e) => {
                int oldRow = _hoveredRow;
                _hoveredRow = -1;
                _hoveredCol = -1;
                if (oldRow >= 0 && oldRow < Rows.Count) InvalidateRow(oldRow);
                Cursor = Cursors.Default;
            };
            MouseLeave += (s, e) => {
                int oldRow = _hoveredRow;
                _hoveredRow = -1;
                _hoveredCol = -1;
                if (oldRow >= 0 && oldRow < Rows.Count) InvalidateRow(oldRow);
                Cursor = Cursors.Default;
            };
        }

        private void OnCellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex == -1 && e.ColumnIndex >= 0)
            {
                e.Handled = true;
                using (var bgBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                {
                    e.Graphics.FillRectangle(bgBrush, e.CellBounds);
                }
                using (var dividerPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(dividerPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                }
                if (e.Value != null)
                {
                    using (var font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold))
                    using (var textBrush = new SolidBrush(Color.FromArgb(71, 85, 105)))
                    {
                        var format = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                        Rectangle textBounds = new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y, e.CellBounds.Width - 24, e.CellBounds.Height);
                        e.Graphics.DrawString(e.Value.ToString().ToUpper(), font, textBrush, textBounds, format);
                    }
                }
                return;
            }

            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                e.Handled = true;

                // 1. Paint clean row background (alternating soft neutral vs white)
                Color bgColor = (e.RowIndex % 2 == 1) ? Color.FromArgb(249, 250, 251) : Color.White;
                if (e.RowIndex == _hoveredRow)
                {
                    bgColor = Color.FromArgb(241, 245, 249); // Responsive interactive hover feedback!
                }
                if ((e.State & DataGridViewElementStates.Selected) != 0)
                {
                    bgColor = Color.FromArgb(238, 242, 255);
                }

                using (var bgBrush = new SolidBrush(bgColor))
                {
                    e.Graphics.FillRectangle(bgBrush, e.CellBounds);
                }

                // 2. Draw a delicate, subtle horizontal separator underneath every row
                using (var dividerPen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                {
                    e.Graphics.DrawLine(dividerPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                }

                // 3. Render content cleanly
                string colName = Columns[e.ColumnIndex].Name;

                if (colName == "ColStatus")
                {
                    string val = e.Value?.ToString() ?? "";
                    Color bg = Color.FromArgb(254, 243, 199); // Pastel amber
                    Color fg = Color.FromArgb(180, 83, 9);   // Amber text
                    string label = "Đang chờ";

                    if (val.Equals("InProgress", StringComparison.OrdinalIgnoreCase) || val.Contains("khám") || val.Equals("2"))
                    {
                        bg = Color.FromArgb(219, 234, 254); // Pastel blue
                        fg = Color.FromArgb(29, 78, 216);  // Blue text
                        label = "Đang khám";
                    }
                    else if (val.Equals("Completed", StringComparison.OrdinalIgnoreCase) || val.Contains("xong") || val.Equals("3") || val.Contains("hoàn thành"))
                    {
                        bg = Color.FromArgb(209, 250, 229); // Pastel emerald
                        fg = Color.FromArgb(4, 120, 87);   // Emerald text
                        label = "Đã hoàn thành";
                    }
                    else if (val.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) || val.Contains("hủy") || val.Contains("Hủy"))
                    {
                        bg = Color.FromArgb(254, 240, 138); // Warm yellow
                        fg = Color.FromArgb(133, 77, 14);   // Deep gold brown
                        label = "Hủy Lịch";
                    }

                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    int padY = (e.CellBounds.Height - 26) / 2;
                    int padX = 14;
                    int pillWidth = 112;
                    Rectangle pillRect = new Rectangle(e.CellBounds.X + padX, e.CellBounds.Y + padY, pillWidth, 26);

                    using (var path = CreateRoundedRectPath(pillRect, 13))
                    using (var brush = new SolidBrush(bg))
                    {
                        e.Graphics.FillPath(brush, path);
                    }

                    using (var font = ClinicalColors.GetMainFont(9f, FontStyle.Bold))
                    using (var textBrush = new SolidBrush(fg))
                    {
                        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString(label, font, textBrush, pillRect, format);
                    }
                }
                else if (colName == "ColAction")
                {
                    // Sleek rounded button for "Khám ▼" exactly as requested!
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    int btnWidth = 84;
                    int btnHeight = 30;
                    int btnX = e.CellBounds.X + (e.CellBounds.Width - btnWidth) / 2;
                    int btnY = e.CellBounds.Y + (e.CellBounds.Height - btnHeight) / 2;
                    Rectangle btnRect = new Rectangle(btnX, btnY, btnWidth, btnHeight);

                    bool isBtnHovered = (e.RowIndex == _hoveredRow && e.ColumnIndex == _hoveredCol);
                    Color btnBg = isBtnHovered ? ClinicalColors.PrimaryBlue : Color.FromArgb(239, 246, 255);
                    Color btnFg = isBtnHovered ? Color.White : ClinicalColors.PrimaryBlue;

                    using (var path = CreateRoundedRectPath(btnRect, 15))
                    {
                        using (var brush = new SolidBrush(btnBg))
                        {
                            e.Graphics.FillPath(brush, path);
                        }
                        using (var pen = new Pen(ClinicalColors.PrimaryBlue, 1f))
                        {
                            e.Graphics.DrawPath(pen, path);
                        }
                    }

                    using (var font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold))
                    using (var textBrush = new SolidBrush(btnFg))
                    {
                        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString("Khám ▼", font, textBrush, btnRect, format);
                    }
                }
                else if (e.Value != null)
                {
                    string text = e.Value.ToString();
                    Color textColor = ClinicalColors.TextDark;
                    bool isBold = false;
                    if (colName == "ColName" || colName == "ColId")
                    {
                        textColor = Color.FromArgb(15, 23, 42); // Strong dark slate
                        if (colName == "ColName") isBold = true;
                    }

                    using (var font = ClinicalColors.GetMainFont(10f, isBold ? FontStyle.Bold : FontStyle.Regular))
                    using (var textBrush = new SolidBrush(textColor))
                    {
                        var format = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                        Rectangle textBounds = new Rectangle(e.CellBounds.X + 12, e.CellBounds.Y, e.CellBounds.Width - 24, e.CellBounds.Height);
                        e.Graphics.DrawString(text, font, textBrush, textBounds, format);
                    }
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
