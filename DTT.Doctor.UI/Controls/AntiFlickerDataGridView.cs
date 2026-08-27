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
                using (var bgBrush = new SolidBrush(Color.FromArgb(250, 250, 250))) // Antd header background #FAFAFA
                {
                    e.Graphics.FillRectangle(bgBrush, e.CellBounds);
                }
                using (var dividerPen = new Pen(Color.FromArgb(240, 240, 240), 1f)) // Antd border #F0F0F0
                {
                    e.Graphics.DrawLine(dividerPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                }
                if (e.Value != null)
                {
                    using (var font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold))
                    using (var textBrush = new SolidBrush(Color.FromArgb(38, 38, 38))) // Antd dark text #262626
                    {
                        var format = new StringFormat { 
                            Alignment = StringAlignment.Near, 
                            LineAlignment = StringAlignment.Center,
                            FormatFlags = StringFormatFlags.NoWrap,
                            Trimming = StringTrimming.EllipsisCharacter
                        };
                        Rectangle textBounds = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y, e.CellBounds.Width - 12, e.CellBounds.Height);
                        e.Graphics.DrawString(e.Value.ToString().ToUpper(), font, textBrush, textBounds, format);
                    }
                }
                return;
            }

            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                e.Handled = true;

                // 1. Paint clean row background (alternating soft neutral vs white)
                Color bgColor = (e.RowIndex % 2 == 1) ? Color.FromArgb(250, 250, 250) : Color.White;
                if (e.RowIndex == _hoveredRow)
                {
                    bgColor = Color.FromArgb(245, 245, 245); // Antd row hover feedback
                }
                if ((e.State & DataGridViewElementStates.Selected) != 0)
                {
                    bgColor = Color.FromArgb(230, 244, 255); // Antd active selection #E6F4FF
                }

                using (var bgBrush = new SolidBrush(bgColor))
                {
                    e.Graphics.FillRectangle(bgBrush, e.CellBounds);
                }

                // 2. Draw a delicate, subtle horizontal separator underneath every row
                using (var dividerPen = new Pen(Color.FromArgb(240, 240, 240), 1f))
                {
                    e.Graphics.DrawLine(dividerPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                }

                // 3. Render content cleanly
                string colName = Columns[e.ColumnIndex].Name;
                string headerText = Columns[e.ColumnIndex].HeaderText ?? "";
                string rawVal = e.Value?.ToString() ?? "";

                // Ant Design Tag Pill System for Status & Priority columns
                // ColPriority RỖNG nghĩa là "không khẩn" (ưu tiên bình thường) — KHÁC với cột Trạng Thái
                // (rỗng = chưa có trạng thái/đang chờ). Trước đây dùng chung 1 nhánh nên MỌI ca không khẩn
                // đều bị vẽ nhầm badge "Đang chờ" dù đã hoàn tất từ lâu, khiến KTV tưởng trạng thái không
                // đổi dù đã nhập kết quả xong — chỉ vào nhánh pill khi ColPriority THẬT SỰ có giá trị (khẩn).
                bool isPriorityWithValue = colName == "ColPriority" && !string.IsNullOrWhiteSpace(rawVal);
                if (colName == "ColStatus" || headerText.Contains("TRẠNG THÁI") || headerText.Contains("ĐÁNH GIÁ") || isPriorityWithValue)
                {
                    Color bg = Color.FromArgb(255, 251, 230);    // Antd Warning Gold bg #FFFBE6
                    Color border = Color.FromArgb(255, 229, 143);// Antd Warning Gold border #FFE58F
                    Color fg = Color.FromArgb(212, 136, 6);      // Antd Warning Gold text #D48806
                    string label = string.IsNullOrWhiteSpace(rawVal) ? "Đang chờ" : rawVal;

                    if (rawVal.Equals("AwaitingTestResults", StringComparison.OrdinalIgnoreCase) || rawVal.Contains("Chờ Kết Quả") || rawVal.Contains("Chờ kết quả"))
                    {
                        bg = Color.FromArgb(249, 240, 255);     // Antd Purple bg #F9F0FF
                        border = Color.FromArgb(211, 173, 247); // Antd Purple border #D3ADF7
                        fg = Color.FromArgb(114, 46, 209);      // Antd Purple text #722ED1
                        label = "Chờ Kết Quả CLS";
                    }
                    // [New badge - Trạng thái chờ Dược sĩ phát thuốc]:
                    else if (rawVal.Equals("PendingDispensing", StringComparison.OrdinalIgnoreCase) || rawVal.Contains("Chờ Dược sĩ") || rawVal.Contains("Chờ phát thuốc") || rawVal.Contains("Chờ cấp thuốc") || rawVal.Equals("10"))
                    {
                        bg = Color.FromArgb(243, 232, 255);     // Light Purple bg #F3E8FF
                        border = Color.FromArgb(192, 132, 252); // Purple border #C084FC
                        fg = Color.FromArgb(126, 34, 206);      // Purple text #7E22CE
                        label = "Chờ Dược Sĩ";
                    }
                    else if (rawVal.Equals("InProgress", StringComparison.OrdinalIgnoreCase) || rawVal.Contains("Đang khám") || rawVal.Contains("Đang tư vấn") || rawVal.Contains("Đang đo") || rawVal.Equals("2"))
                    {
                        bg = Color.FromArgb(230, 244, 255);     // Antd Processing Blue bg #E6F4FF
                        border = Color.FromArgb(145, 202, 255); // Antd Processing Blue border #91CAFF
                        fg = Color.FromArgb(22, 119, 255);      // Antd Processing Blue text #1677FF
                        label = rawVal.Equals("2") ? "Đang khám" : rawVal;
                    }
                    else if (rawVal.Equals("Completed", StringComparison.OrdinalIgnoreCase) || rawVal.Contains("Đã khám") || rawVal.Contains("Hoàn tất") || rawVal.Contains("Bình thường") || rawVal.Equals("Normal", StringComparison.OrdinalIgnoreCase) || rawVal.Contains("Đã thanh toán") || rawVal.Contains("Đã duyệt") || rawVal.Equals("3"))
                    {
                        bg = Color.FromArgb(246, 255, 237);     // Antd Success Green bg #F6FFED
                        border = Color.FromArgb(183, 235, 143); // Antd Success Green border #B7EB8F
                        fg = Color.FromArgb(82, 196, 26);       // Antd Success Green text #52C41A
                        label = rawVal.Equals("3") ? "Đã khám" : rawVal;
                    }
                    else if (rawVal.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) || rawVal.Contains("Hủy") || rawVal.Contains("hủy"))
                    {
                        bg = Color.FromArgb(255, 241, 240);     // Antd Error Red bg #FFF1F0
                        border = Color.FromArgb(255, 204, 199); // Antd Error Red border #FFCCC7
                        fg = Color.FromArgb(255, 77, 79);       // Antd Error Red text #FF4D4F
                        label = rawVal.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ? "Hủy Lịch" : rawVal;
                    }
                    else if (rawVal.Equals("NoShow", StringComparison.OrdinalIgnoreCase) || rawVal.Equals("Expired", StringComparison.OrdinalIgnoreCase) || rawVal.Contains("Quá hạn") || rawVal.Contains("Bỏ khám") || rawVal.Contains("Bất thường") || rawVal.Equals("Abnormal", StringComparison.OrdinalIgnoreCase) || rawVal.Contains("Khẩn"))
                    {
                        bg = Color.FromArgb(255, 241, 240);     // Antd Error Red bg #FFF1F0
                        border = Color.FromArgb(255, 204, 199); // Antd Error Red border #FFCCC7
                        fg = Color.FromArgb(255, 77, 79);       // Antd Error Red text #FF4D4F
                        label = rawVal.Equals("NoShow", StringComparison.OrdinalIgnoreCase) ? "Bỏ Khám" : rawVal;
                    }

                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        int padY = (e.CellBounds.Height - 26) / 2;
                        int padX = 8;
                        int pillWidth = e.CellBounds.Width - 16;
                        if (pillWidth > 115) pillWidth = 115;
                        if (pillWidth < 60) pillWidth = 60;
                        Rectangle pillRect = new Rectangle(e.CellBounds.X + padX, e.CellBounds.Y + padY, pillWidth, 26);

                        using (var path = CreateRoundedRectPath(pillRect, 6)) // Antd 6px rounded tag corners
                        {
                            using (var brush = new SolidBrush(bg))
                            {
                                e.Graphics.FillPath(brush, path);
                            }
                            using (var pen = new Pen(border, 1f))
                            {
                                e.Graphics.DrawPath(pen, path);
                            }
                        }

                        using (var font = ClinicalColors.GetMainFont(8.5f, FontStyle.Bold))
                        using (var textBrush = new SolidBrush(fg))
                        {
                            var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            e.Graphics.DrawString(label, font, textBrush, pillRect, format);
                        }
                    }
                }
                else if (colName == "ColAction" || (Columns[e.ColumnIndex].HeaderText != null && (Columns[e.ColumnIndex].HeaderText.Contains("THAO TÁC") || Columns[e.ColumnIndex].HeaderText.Contains("HỦY"))))
                {
                    string actionText = e.Value != null ? e.Value.ToString() : "";

                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    int btnWidth = e.CellBounds.Width - 16;
                    if (btnWidth > 105) btnWidth = 105;
                    int btnHeight = 24; // Thu gọn chiều cao xuống 24px gọn gàng, dịu mắt
                    int btnX = e.CellBounds.X + (e.CellBounds.Width - btnWidth) / 2;
                    int btnY = e.CellBounds.Y + (e.CellBounds.Height - btnHeight) / 2;
                    Rectangle btnRect = new Rectangle(btnX, btnY, btnWidth, btnHeight);

                    bool isBtnHovered = (e.RowIndex == _hoveredRow && e.ColumnIndex == _hoveredCol);
                    
                    Color btnBg = Color.FromArgb(239, 246, 255);
                    Color btnBorder = ClinicalColors.PrimaryBlue;
                    Color btnFg = ClinicalColors.PrimaryBlue;

                    if (actionText.Contains("Check-in"))
                    {
                        if (actionText.Contains("Đã Check-in"))
                        {
                            // Trạng thái đã Check-in rồi: Khóa nút màu xám xanh chìm nhẹ, dịu mắt
                            btnBg = Color.FromArgb(241, 245, 249);
                            btnBorder = Color.FromArgb(226, 232, 240);
                            btnFg = Color.FromArgb(148, 163, 184);
                        }
                        else
                        {
                            btnBg = isBtnHovered ? Color.FromArgb(16, 185, 129) : Color.FromArgb(236, 253, 245);
                            btnBorder = Color.FromArgb(16, 185, 129);
                            btnFg = isBtnHovered ? Color.White : Color.FromArgb(16, 185, 129);
                        }
                    }
                    else if (actionText.Contains("Tiếp nhận"))
                    {
                        btnBg = isBtnHovered ? Color.FromArgb(79, 70, 229) : Color.FromArgb(238, 242, 255);
                        btnBorder = Color.FromArgb(79, 70, 229);
                        btnFg = isBtnHovered ? Color.White : Color.FromArgb(67, 56, 202);
                    }
                    else if (actionText.Contains("Mở lại"))
                    {
                        btnBg = isBtnHovered ? Color.FromArgb(16, 185, 129) : Color.FromArgb(236, 253, 245);
                        btnBorder = Color.FromArgb(16, 185, 129);
                        btnFg = isBtnHovered ? Color.White : Color.FromArgb(4, 120, 87);
                    }
                    else if (actionText.Contains("Tùy chọn"))
                    {
                        btnBg = isBtnHovered ? Color.FromArgb(241, 245, 249) : Color.White;
                        btnBorder = Color.FromArgb(203, 213, 225);
                        btnFg = Color.FromArgb(71, 85, 105);
                    }

                    using (var path = CreateRoundedRectPath(btnRect, 5))
                    {
                        using (var brush = new SolidBrush(btnBg))
                        {
                            e.Graphics.FillPath(brush, path);
                        }
                        using (var pen = new Pen(btnBorder, 1f))
                        {
                            e.Graphics.DrawPath(pen, path);
                        }
                    }

                    using (var font = ClinicalColors.GetMainFont(8f, FontStyle.Bold)) // Font 8pt dịu vừa vặn
                    using (var textBrush = new SolidBrush(btnFg))
                    {
                        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString(actionText, font, textBrush, btnRect, format);
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
                        var format = new StringFormat { 
                            Alignment = StringAlignment.Near, 
                            LineAlignment = StringAlignment.Center,
                            FormatFlags = StringFormatFlags.NoWrap,
                            Trimming = StringTrimming.EllipsisCharacter
                        };
                        if (colName == "ColDelete" || text.Contains("Xóa"))
                        {
                            format.Alignment = StringAlignment.Center;
                        }
                        Rectangle textBounds = new Rectangle(e.CellBounds.X + 4, e.CellBounds.Y, e.CellBounds.Width - 8, e.CellBounds.Height);
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
