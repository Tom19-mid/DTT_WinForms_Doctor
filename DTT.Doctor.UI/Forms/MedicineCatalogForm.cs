using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;

namespace DTT.Doctor.UI.Forms
{
    public class MedicineCatalogForm : Form
    {
        private TextBox _txtSearch;
        private AntiFlickerDataGridView _gridMeds;
        private Label _lblStatus;

        public MedicineCatalogForm()
        {
            InitializeComponent();
            _ = LoadMedicinesAsync();
        }

        private void InitializeComponent()
        {
            Text = "DTT Healthcare - Danh Mục & Quản Lý Kho Thuốc Bệnh Viện";
            Size = new Size(1100, 750);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = ClinicalColors.GhostWhite;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

            // Header
            Panel pnlHeader = new Panel
            {
                Height = 80,
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            Panel pnlHeaderBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ClinicalColors.BorderGray
            };
            pnlHeader.Controls.Add(pnlHeaderBorder);

            AvatarBoxControl avatar = new AvatarBoxControl(46)
            {
                Location = new Point(20, 16)
            };

            Label lblTitle = new Label
            {
                Text = "DANH MỤC THUỐC & QUẢN LÝ TỒN KHO BỆNH VIỆN",
                Font = ClinicalColors.GetMainFont(13f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(78, 14),
                Size = new Size(650, 26),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblSubtitle = new Label
            {
                Text = "Theo dõi danh mục dược phẩm, số lượng tồn kho và đơn giá thuốc kê đơn",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(78, 42),
                Size = new Size(700, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlHeader.Controls.Add(avatar);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // Filter Bar
            Panel pnlFilter = new Panel
            {
                Height = 60,
                BackColor = Color.White,
                Margin = Padding.Empty
            };

            Label lblSearch = new Label
            {
                Text = "Tìm thuốc:",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(20, 18),
                AutoSize = true
            };

            _txtSearch = new TextBox
            {
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                Location = new Point(100, 14),
                Size = new Size(320, 28),
                PlaceholderText = "Nhập Tên thuốc, hoạt chất..."
            };
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) _ = LoadMedicinesAsync(); };

            Button btnSearch = new Button
            {
                Text = "Tìm Kiếm",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ClinicalColors.PrimaryBlue,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 32),
                Location = new Point(430, 13),
                Cursor = Cursors.Hand
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += (s, e) => _ = LoadMedicinesAsync();

            _lblStatus = new Label
            {
                Text = "Đang nạp danh mục thuốc...",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(600, 18),
                Size = new Size(460, 24),
                TextAlign = ContentAlignment.MiddleRight
            };

            pnlFilter.Controls.Add(lblSearch);
            pnlFilter.Controls.Add(_txtSearch);
            pnlFilter.Controls.Add(btnSearch);
            pnlFilter.Controls.Add(_lblStatus);

            // GridView
            _gridMeds = new AntiFlickerDataGridView
            {
                Margin = new Padding(16)
            };
            _gridMeds.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", FillWeight = 30, MinimumWidth = 45 });
            _gridMeds.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TÊN THUỐC / HOẠT CHẤT", FillWeight = 160, MinimumWidth = 180 });
            _gridMeds.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ĐƠN VỊ", FillWeight = 60, MinimumWidth = 80 });
            _gridMeds.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ĐƠN GIÁ", FillWeight = 90, MinimumWidth = 110 });
            _gridMeds.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SỐ LƯỢNG KHO", FillWeight = 90, MinimumWidth = 110 });
            _gridMeds.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "HƯỚNG DẪN SỬ DỤNG MẶC ĐỊNH", FillWeight = 200, MinimumWidth = 240 });
            _gridMeds.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TRẠNG THÁI", FillWeight = 90, MinimumWidth = 110 });

            // Footer
            Panel pnlFooter = new Panel
            {
                Height = 60,
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            Button btnClose = new Button
            {
                Text = "Đóng",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 38),
                Location = new Point(960, 11),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = ClinicalColors.BorderGray;
            btnClose.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnClose);

            // Layout
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                BackColor = ClinicalColors.GhostWhite
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));

            pnlHeader.Dock = DockStyle.Fill;
            pnlFilter.Dock = DockStyle.Fill;
            _gridMeds.Dock = DockStyle.Fill;
            pnlFooter.Dock = DockStyle.Fill;

            mainLayout.Controls.Add(pnlHeader, 0, 0);
            mainLayout.Controls.Add(pnlFilter, 0, 1);
            mainLayout.Controls.Add(_gridMeds, 0, 2);
            mainLayout.Controls.Add(pnlFooter, 0, 3);

            Controls.Add(mainLayout);
        }

        private async Task LoadMedicinesAsync()
        {
            try
            {
                _lblStatus.Text = "⏳ Đang tải kho thuốc...";
                _gridMeds.Rows.Clear();

                var api = new ApiService();
                var list = await api.GetMedicinesAsync();

                string search = _txtSearch.Text.Trim().ToLower();
                if (!string.IsNullOrEmpty(search))
                {
                    list = list.Where(m => m.MedicineName.ToLower().Contains(search) || m.DefaultUsage.ToLower().Contains(search)).ToList();
                }

                int stt = 1;
                foreach (var m in list)
                {
                    string priceStr = m.UnitPrice > 0 ? $"{m.UnitPrice:#,##0} VNĐ" : "15.000 VNĐ";
                    // Hiện ĐÚNG tồn kho thật (trước đây khi StockQuantity = 0 sẽ hiện giả "500", che mất
                    // việc thuốc đã thật sự hết hàng — khiến bác sĩ không bao giờ biết cần báo nhập thêm).
                    int stock = m.StockQuantity;
                    string trangThai = stock <= 0 ? "❌ Hết Hàng" : stock < 20 ? "⚠️ Sắp Hết" : "✅ Khả Dụng";

                    int rowIdx = _gridMeds.Rows.Add(
                        stt++,
                        m.MedicineName,
                        m.Unit,
                        priceStr,
                        $"{stock} {m.Unit}",
                        !string.IsNullOrEmpty(m.DefaultUsage) ? m.DefaultUsage : "Uống sau ăn 30 phút",
                        trangThai
                    );

                    if (stock <= 0)
                    {
                        _gridMeds.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(185, 28, 28);
                        _gridMeds.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242);
                    }
                    else if (stock < 20)
                    {
                        _gridMeds.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(161, 98, 7);
                        _gridMeds.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(255, 251, 235);
                    }
                }

                _lblStatus.Text = $"Tổng cộng có {_gridMeds.Rows.Count} loại thuốc trong kho";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Lỗi nạp kho thuốc: " + ex.Message;
            }
        }
    }
}
