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
using Newtonsoft.Json;

namespace DTT.Doctor.UI.Forms
{
    public class ClinicalStatsForm : Form
    {
        private Label _lblRevenue;
        private Label _lblTotalAppts;
        private Label _lblCompletedAppts;
        private Label _lblSuccessRate;
        private AntiFlickerDataGridView _gridStats;

        public ClinicalStatsForm()
        {
            InitializeComponent();
            _ = LoadStatsDataAsync();
        }

        private void InitializeComponent()
        {
            Text = "DTT Healthcare - Báo Cáo Thống Kê Hoạt Động Khám Bệnh";
            Size = new Size(1150, 780);
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
                Text = "BÁO CÁO THỐNG KÊ CA KHÁM & DOANH THU PHÒNG KHÁM",
                Font = ClinicalColors.GetMainFont(13f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(78, 14),
                Size = new Size(650, 26),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Label lblSubtitle = new Label
            {
                Text = "Tổng hợp ca khám, chỉ số hiệu suất phòng khám và tổng viện phí thu trong ngày",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(78, 42),
                Size = new Size(700, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlHeader.Controls.Add(avatar);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // KPI Panel
            Panel pnlKpis = new Panel
            {
                Height = 110,
                BackColor = ClinicalColors.GhostWhite,
                Padding = new Padding(16, 10, 16, 10)
            };

            int cardWidth = 250;
            int gap = 16;
            int startX = 16;

            Panel card1 = CreateKpiCard("Tổng Doanh Thu", "12.500.000 VNĐ", Color.FromArgb(16, 185, 129), out _lblRevenue);
            card1.Location = new Point(startX, 10);

            Panel card2 = CreateKpiCard("Ca Khám Tiếp Nhận", "15 Ca", ClinicalColors.PrimaryBlue, out _lblTotalAppts);
            card2.Location = new Point(startX + cardWidth + gap, 10);

            Panel card3 = CreateKpiCard("Ca Đã Hoàn Thành", "12 Ca", Color.FromArgb(99, 102, 241), out _lblCompletedAppts);
            card3.Location = new Point(startX + (cardWidth + gap) * 2, 10);

            Panel card4 = CreateKpiCard("Tỷ Lệ Hoàn Thành", "92.0%", Color.FromArgb(245, 158, 11), out _lblSuccessRate);
            card4.Location = new Point(startX + (cardWidth + gap) * 3, 10);

            pnlKpis.Controls.Add(card1);
            pnlKpis.Controls.Add(card2);
            pnlKpis.Controls.Add(card3);
            pnlKpis.Controls.Add(card4);

            // GridView
            _gridStats = new AntiFlickerDataGridView
            {
                Margin = new Padding(16)
            };
            _gridStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "STT", FillWeight = 30, MinimumWidth = 45 });
            _gridStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "MÃ CA KHÁM", FillWeight = 80, MinimumWidth = 100 });
            _gridStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BỆNH NHÂN", FillWeight = 110, MinimumWidth = 140 });
            _gridStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "CHUYÊN KHOA", FillWeight = 110, MinimumWidth = 140 });
            _gridStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "BÁC SĨ THỰC HIỆN", FillWeight = 110, MinimumWidth = 140 });
            _gridStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "VIỆN PHÍ TẠM THU", FillWeight = 90, MinimumWidth = 110 });
            _gridStats.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "TRẠNG THÁI KHÁM", FillWeight = 90, MinimumWidth = 110 });

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
                Location = new Point(1010, 11),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = ClinicalColors.BorderGray;
            btnClose.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnClose);

            // TableLayoutPanel
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
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));

            pnlHeader.Dock = DockStyle.Fill;
            pnlKpis.Dock = DockStyle.Fill;
            _gridStats.Dock = DockStyle.Fill;
            pnlFooter.Dock = DockStyle.Fill;

            mainLayout.Controls.Add(pnlHeader, 0, 0);
            mainLayout.Controls.Add(pnlKpis, 0, 1);
            mainLayout.Controls.Add(_gridStats, 0, 2);
            mainLayout.Controls.Add(pnlFooter, 0, 3);

            Controls.Add(mainLayout);
        }

        private Panel CreateKpiCard(string title, string defaultVal, Color themeColor, out Label valueLabel)
        {
            Panel pnl = new Panel
            {
                Size = new Size(250, 90),
                BackColor = Color.White
            };

            Panel bar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 5,
                BackColor = themeColor
            };

            Label lblT = new Label
            {
                Text = title,
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(16, 12),
                AutoSize = true
            };

            valueLabel = new Label
            {
                Text = defaultVal,
                Font = ClinicalColors.GetMainFont(14f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextDark,
                Location = new Point(16, 38),
                Size = new Size(225, 34),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnl.Controls.Add(bar);
            pnl.Controls.Add(lblT);
            pnl.Controls.Add(valueLabel);
            return pnl;
        }

        private async Task LoadStatsDataAsync()
        {
            try
            {
                _gridStats.Rows.Clear();
                var api = new ApiService();

                string queryUrl = "/api/Appointments";
                if (TokenVault.DoctorId > 0)
                {
                    queryUrl += $"?doctorId={TokenVault.DoctorId}";
                }
                var json = await api.GetRawAsync(queryUrl);
                if (!string.IsNullOrEmpty(json))
                {
                    dynamic list = JsonConvert.DeserializeObject(json);
                    if (list != null)
                    {
                        int stt = 1;
                        int total = 0;
                        int completed = 0;
                        decimal revenue = 0;

                        foreach (var appt in list)
                        {
                            total++;
                            string status = (string)appt.status;
                            bool isDone = status == "Completed" || status == "Đã hoàn thành";
                            if (isDone) completed++;

                            string feeRaw = (string)appt.fee;
                            decimal fee = 250000;
                            if (!string.IsNullOrEmpty(feeRaw))
                            {
                                string clean = feeRaw.Replace(".", "").Replace("đ", "").Replace("VNĐ", "").Trim();
                                if (decimal.TryParse(clean, out var parsedFee)) fee = parsedFee;
                            }
                            if (isDone) revenue += fee;

                            string sttText = isDone ? "Đã Khám Bệnh" : status == "Cancelled" ? "Hủy Lịch" : status == "NoShow" ? "Bỏ Khám" : "Đang Chờ";

                            _gridStats.Rows.Add(
                                stt++,
                                $"HD-2026-{(int)appt.appointmentId:D3}",
                                (string)appt.patientName,
                                (string)appt.specialtyName,
                                !string.IsNullOrEmpty((string)appt.doctorName) ? (string)appt.doctorName : TokenVault.FullName,
                                $"{fee:#,##0} VNĐ",
                                sttText
                            );
                        }

                        _lblTotalAppts.Text = $"{total} Ca";
                        _lblCompletedAppts.Text = $"{completed} Ca";
                        _lblRevenue.Text = $"{revenue:#,##0} VNĐ";

                        double rate = total > 0 ? (double)completed / total * 100 : 100.0;
                        _lblSuccessRate.Text = $"{rate:F1}%";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi nạp báo cáo thống kê: " + ex.Message);
            }
        }
    }
}
