using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;
using DTT.Doctor.UI.Controls;
using DTT.Doctor.UI.Theme;
using Panel = System.Windows.Forms.Panel;
using Button = System.Windows.Forms.Button;

namespace DTT.Doctor.UI.Forms
{
    /// <summary>
    /// Cửa sổ riêng để KTV nhập kết quả 1 chỉ định Xét nghiệm/Siêu âm — 
    /// giao diện Clinical Dashboard hiện đại với xem trước thumbnail ảnh siêu âm và điều khiển nhập liệu bo góc.
    /// </summary>
    public class ClinicalResultDialogForm : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        private readonly ApiService _api = new ApiService();
        private readonly ClinicalOrderQueueItem _item;
        private readonly bool _isTest;

        private List<string> _imageUrls = new List<string>();
        private FlowLayoutPanel _flowImages;
        private Label _lblImagesEmpty;

        private TextBox _txtResultValue, _txtUnit, _txtReferenceRange;
        private ComboBox _cboResultStatus;
        private TextBox _txtDescription, _txtConclusion;

        private Label _lblStatus;
        private RoundedButton _btnSave;
        private RoundedButton _btnCancelOrder;
        private RoundedButton _btnClose;

        public bool WasModified { get; private set; } = false;

        public ClinicalResultDialogForm(ClinicalOrderQueueItem item)
        {
            _item = item;
            _isTest = item.Kind == "Test";
            InitializeComponent();
            if (!_isTest) _ = LoadUltrasoundDetailAsync();
        }

        private void InitializeComponent()
        {
            Text = _isTest ? "Nhập Kết Quả Xét Nghiệm" : "Nhập Kết Quả Siêu Âm";
            Size = new Size(720, _isTest ? 600 : 710);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ClinicalColors.GhostWhite;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);
            DoubleBuffered = true;

            // ── Header Panel ───────────────────────────────────────────────
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Color.White };
            Panel pnlHeaderBorder = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = ClinicalColors.BorderGray };
            pnlHeader.Controls.Add(pnlHeaderBorder);

            Label lblPatient = new Label
            {
                Text = $"{(_isTest ? "🔬" : "📡")}  {_item.PatientName.ToUpper()}" + (_item.IsUrgent ? "   🚨 KHẨN" : ""),
                Font = ClinicalColors.GetMainFont(13.5f, FontStyle.Bold),
                ForeColor = _item.IsUrgent ? Color.FromArgb(185, 28, 28) : ClinicalColors.PrimaryBlue,
                Location = new Point(20, 12),
                Size = new Size(660, 28),
                UseMnemonic = false
            };
            Label lblService = new Label
            {
                Text = $"{_item.ServiceName}  •  BS chỉ định: {_item.DoctorName}",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(20, 42),
                Size = new Size(660, 22),
                UseMnemonic = false
            };
            Label lblNote = new Label
            {
                Text = string.IsNullOrWhiteSpace(_item.ClinicalNote) ? "" : $"📝 Lý do chỉ định: {_item.ClinicalNote}",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Italic),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(20, 64),
                Size = new Size(660, 22),
                UseMnemonic = false
            };
            pnlHeader.Controls.Add(lblPatient);
            pnlHeader.Controls.Add(lblService);
            pnlHeader.Controls.Add(lblNote);

            // ── Body Panel ─────────────────────────────────────────────────
            Panel pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 16), AutoScroll = true };

            int y = 0;
            Label MkLbl(string text, int yy) => new Label
            {
                Text = text,
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(0, yy),
                AutoSize = true
            };

            var bodyControls = new List<System.Windows.Forms.Control>();

            if (_isTest)
            {
                var lbl1 = MkLbl("Giá trị kết quả xét nghiệm", y); y += 22;
                Panel pnlRes = CreateInputContainer(660, 52, out _txtResultValue, "Ví dụ: Hồng cầu 4.5 T/L, Bạch cầu 7.2 G/L");
                pnlRes.Location = new Point(0, y);
                _txtResultValue.Text = _item.ResultValue ?? "";
                y += 62;

                var lblU = MkLbl("Đơn vị đo", y);
                var lblR = MkLbl("Khoảng tham chiếu chuẩn", y); lblR.Location = new Point(340, y);
                y += 22;

                Panel pnlUnit = CreateInputContainer(320, 44, out _txtUnit, "Ví dụ: mg/dL");
                pnlUnit.Location = new Point(0, y);
                _txtUnit.Text = _item.Unit ?? "";

                Panel pnlRef = CreateInputContainer(320, 44, out _txtReferenceRange, "Ví dụ: 3.8 - 5.5");
                pnlRef.Location = new Point(340, y);
                _txtReferenceRange.Text = _item.ReferenceRange ?? "";
                y += 56;

                var lblStatusHdr = MkLbl("Đánh giá kết quả", y); y += 22;
                _cboResultStatus = new ComboBox
                {
                    Location = new Point(0, y),
                    Size = new Size(660, 32),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular)
                };
                _cboResultStatus.Items.Add("Normal — Bình thường");
                _cboResultStatus.Items.Add("Abnormal — Bất thường");
                _cboResultStatus.SelectedIndex = _item.Status == "Abnormal" ? 1 : 0;
                y += 44;

                bodyControls.AddRange(new System.Windows.Forms.Control[] { lbl1, pnlRes, lblU, pnlUnit, lblR, pnlRef, lblStatusHdr, _cboResultStatus });
            }
            else
            {
                var lblD = MkLbl("Mô tả chi tiết hình ảnh siêu âm", y); y += 22;
                Panel pnlDesc = CreateInputContainer(660, 68, out _txtDescription, "Ví dụ: Tuyến giáp kích thước bình thường, không có nhân bất thường...");
                pnlDesc.Location = new Point(0, y);
                y += 76;

                var lblC = MkLbl("Kết luận chẩn đoán", y); y += 22;
                Panel pnlConcl = CreateInputContainer(660, 58, out _txtConclusion, "Ví dụ: Không phát hiện bất thường / Nang tuyến giáp 2 bên...", isBold: true);
                pnlConcl.Location = new Point(0, y);
                y += 68;

                Panel pnlImgBar = new Panel { Location = new Point(0, y), Size = new Size(660, 36), BackColor = Color.Transparent };
                Label lblImg = new Label
                {
                    Text = "📷  Ảnh siêu âm đính kèm",
                    Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(51, 65, 85),
                    Location = new Point(0, 8),
                    AutoSize = true
                };

                RoundedButton btnAttach = new RoundedButton
                {
                    Text = "📎  Đính Kèm Ảnh Mới",
                    Font = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                    BackColor = Color.FromArgb(238, 242, 255),
                    HoverBackColor = Color.FromArgb(224, 231, 255),
                    ForeColor = ClinicalColors.PrimaryBlue,
                    BorderRadius = 8,
                    Location = new Point(480, 0),
                    Size = new Size(180, 34),
                    Cursor = Cursors.Hand
                };
                btnAttach.Click += async (s, e) => await OnAttachImageClickAsync();
                pnlImgBar.Controls.Add(lblImg);
                pnlImgBar.Controls.Add(btnAttach);
                y += 42;

                _lblImagesEmpty = new Label
                {
                    Text = "Chưa có ảnh nào được đính kèm.",
                    Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Italic),
                    ForeColor = ClinicalColors.TextMuted,
                    Location = new Point(0, y),
                    Size = new Size(660, 24)
                };

                _flowImages = new FlowLayoutPanel
                {
                    Location = new Point(0, y),
                    Size = new Size(660, 160),
                    AutoScroll = true,
                    WrapContents = true,
                    FlowDirection = FlowDirection.LeftToRight,
                    BackColor = Color.Transparent
                };
                y += 166;

                bodyControls.AddRange(new System.Windows.Forms.Control[] { lblD, pnlDesc, lblC, pnlConcl, pnlImgBar, _lblImagesEmpty, _flowImages });
            }

            _lblStatus = new Label
            {
                Text = "",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                Location = new Point(0, y + 4),
                Size = new Size(660, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };
            bodyControls.Add(_lblStatus);

            foreach (var c in bodyControls) pnlBody.Controls.Add(c);

            // ── Footer Panel ───────────────────────────────────────────────
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Color.White };
            Panel pnlFooterBorder = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = ClinicalColors.BorderGray };
            pnlFooter.Controls.Add(pnlFooterBorder);

            _btnCancelOrder = new RoundedButton
            {
                Text = "❌  Hủy Chỉ Định",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                BackColor = Color.White,
                HoverBackColor = Color.FromArgb(254, 242, 242),
                BorderColor = Color.FromArgb(252, 165, 165),
                BorderSize = 1,
                ForeColor = Color.FromArgb(185, 28, 28),
                BorderRadius = 8,
                Location = new Point(20, 12),
                Size = new Size(160, 40),
                Cursor = Cursors.Hand
            };
            _btnCancelOrder.Click += async (s, e) => await OnCancelOrderClickAsync();

            _btnClose = new RoundedButton
            {
                Text = "Đóng cửa sổ",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(241, 245, 249),
                HoverBackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(71, 85, 105),
                BorderRadius = 8,
                Size = new Size(125, 40),
                Location = new Point(365, 12),
                Cursor = Cursors.Hand
            };
            _btnClose.Click += (s, e) => { DialogResult = WasModified ? DialogResult.OK : DialogResult.Cancel; Close(); };

            _btnSave = new RoundedButton
            {
                Text = "✅  Lưu Kết Quả",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129),
                HoverBackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                BorderRadius = 8,
                Size = new Size(175, 40),
                Location = new Point(505, 12),
                Cursor = Cursors.Hand
            };
            _btnSave.Click += async (s, e) => await OnSaveClickAsync();

            pnlFooter.Controls.Add(_btnCancelOrder);
            pnlFooter.Controls.Add(_btnClose);
            pnlFooter.Controls.Add(_btnSave);

            Controls.Add(pnlBody);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
        }

        private Panel CreateInputContainer(int width, int height, out TextBox txt, string placeholder, bool isBold = false)
        {
            bool isFocused = false;
            Panel container = new Panel
            {
                Size = new Size(width, height),
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 8)
            };

            container.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, container.Width - 1, container.Height - 1);
                Color border = isFocused ? ClinicalColors.PrimaryBlue : Color.FromArgb(203, 213, 225);
                using (var path = CreateRoundedPath(rect, 6))
                using (var pen = new Pen(border, isFocused ? 1.8f : 1f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            txt = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = height > 42,
                BorderStyle = BorderStyle.None,
                Font = ClinicalColors.GetMainFont(isBold ? 10.5f : 10f, isBold ? FontStyle.Bold : FontStyle.Regular),
                ScrollBars = ScrollBars.None
            };

            TextBox localTxt = txt;
            txt.GotFocus += (s, e) => { isFocused = true; container.Invalidate(); };
            txt.LostFocus += (s, e) => { isFocused = false; container.Invalidate(); };

            if (!string.IsNullOrEmpty(placeholder))
            {
                txt.HandleCreated += (s, e) =>
                {
                    SendMessage(localTxt.Handle, EM_SETCUEBANNER, 1, placeholder);
                };
            }

            container.Controls.Add(txt);
            return container;
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

        // ── Ultrasound: tải mô tả/kết luận/ảnh hiện có khi mở cửa sổ ─────────
        private async Task LoadUltrasoundDetailAsync()
        {
            var (desc, concl, urls) = await _api.GetUltrasoundDetailAsync(_item.Id);
            _txtDescription.Text = desc;
            _txtConclusion.Text = concl;
            _imageUrls = urls;
            RenderImageGallery();
        }

        private void RenderImageGallery()
        {
            _flowImages.Controls.Clear();
            _lblImagesEmpty.Visible = _imageUrls.Count == 0;

            for (int i = 0; i < _imageUrls.Count; i++)
            {
                int index = i;
                string fullUrl = _api.BaseUrl + _imageUrls[i];

                Panel card = new Panel
                {
                    Size = new Size(116, 142),
                    Margin = new Padding(6),
                    BackColor = Color.White
                };
                card.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                    using (var path = CreateRoundedPath(rect, 8))
                    using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                };

                string currentUrl = fullUrl;
                PictureBox pb = new PictureBox
                {
                    Size = new Size(104, 100),
                    Location = new Point(6, 6),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.FromArgb(248, 250, 252),
                    Cursor = Cursors.Hand
                };
                try { pb.LoadAsync(fullUrl); } catch { }
                pb.Click += (s, e) => OpenFullImageViewer(currentUrl, index + 1, _imageUrls.Count);

                RoundedButton btnRemove = new RoundedButton
                {
                    Text = "🗑️  Xóa ảnh",
                    Font = ClinicalColors.GetMainFont(8.5f, FontStyle.Bold),
                    BackColor = Color.FromArgb(254, 226, 226),
                    HoverBackColor = Color.FromArgb(252, 165, 165),
                    ForeColor = Color.FromArgb(185, 28, 28),
                    BorderRadius = 6,
                    Location = new Point(6, 110),
                    Size = new Size(104, 26),
                    Cursor = Cursors.Hand
                };
                btnRemove.Click += async (s, e) => await OnRemoveImageClickAsync(index);

                card.Controls.Add(pb);
                card.Controls.Add(btnRemove);
                _flowImages.Controls.Add(card);
            }
        }

        private void OpenFullImageViewer(string imageUrl, int imgIndex, int totalImages)
        {
            using Form viewer = new Form
            {
                Text = $"🔎 Xem Ảnh Siêu Âm ({imgIndex}/{totalImages}) — {_item.PatientName}",
                Size = new Size(960, 720),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(15, 23, 42),
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular),
                MinimizeBox = false,
                MaximizeBox = true
            };

            Panel pnlViewHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(16, 0, 16, 0)
            };

            Label lblInfo = new Label
            {
                Text = $"📷 Ảnh siêu âm ({imgIndex}/{totalImages})  •  Bệnh nhân: {_item.PatientName}  •  Chỉ định: {_item.ServiceName}",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(16, 15),
                AutoSize = true,
                UseMnemonic = false
            };

            RoundedButton btnCloseViewer = new RoundedButton
            {
                Text = "✕  Đóng (Esc)",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(51, 65, 85),
                HoverBackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                BorderRadius = 6,
                Size = new Size(120, 34),
                Location = new Point(viewer.ClientSize.Width - 136, 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnCloseViewer.Click += (s, e) => viewer.Close();

            pnlViewHeader.Controls.Add(lblInfo);
            pnlViewHeader.Controls.Add(btnCloseViewer);

            PictureBox pbFull = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(15, 23, 42)
            };
            try { pbFull.LoadAsync(imageUrl); } catch { }

            viewer.Controls.Add(pbFull);
            viewer.Controls.Add(pnlViewHeader);

            viewer.KeyPreview = true;
            viewer.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) viewer.Close();
            };

            viewer.ShowDialog(this);
        }

        private async Task OnAttachImageClickAsync()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Ảnh (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Chọn ảnh siêu âm để đính kèm"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            _lblStatus.Text = "⏳ Đang tải ảnh lên...";
            _lblStatus.ForeColor = ClinicalColors.TextMuted;
            var (success, url) = await _api.UploadUltrasoundImageAsync(_item.Id, ofd.FileName);

            if (success)
            {
                _imageUrls.Add(url);
                RenderImageGallery();
                WasModified = true;
                _lblStatus.Text = "✅ Đã đính kèm ảnh thành công.";
                _lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
            }
            else
            {
                _lblStatus.Text = "❌ Lỗi! Không thể upload ảnh. Kiểm tra kết nối API.";
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
            }
        }

        private async Task OnRemoveImageClickAsync(int index)
        {
            if (index < 0 || index >= _imageUrls.Count) return;
            bool success = await _api.RemoveUltrasoundImageAsync(_item.Id, index);
            if (success)
            {
                _imageUrls.RemoveAt(index);
                RenderImageGallery();
                WasModified = true;
                _lblStatus.Text = "✅ Đã gỡ ảnh thành công.";
                _lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
            }
            else
            {
                _lblStatus.Text = "❌ Lỗi! Không thể gỡ ảnh.";
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
            }
        }

        private async Task OnSaveClickAsync()
        {
            if (_isTest && string.IsNullOrWhiteSpace(_txtResultValue.Text))
            {
                _lblStatus.Text = "⚠️ Vui lòng nhập Kết quả xét nghiệm!";
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                return;
            }
            if (!_isTest && string.IsNullOrWhiteSpace(_txtConclusion.Text))
            {
                _lblStatus.Text = "⚠️ Vui lòng nhập Kết luận siêu âm!";
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                return;
            }

            _btnSave.Enabled = false;
            _lblStatus.Text = "⏳ Đang lưu...";
            _lblStatus.ForeColor = ClinicalColors.TextMuted;

            bool success;
            if (_isTest)
            {
                string status = _cboResultStatus.SelectedIndex == 1 ? "Abnormal" : "Normal";
                success = await _api.SubmitTestResultAsync(_item.Id, _txtResultValue.Text.Trim(), _txtUnit.Text.Trim(), _txtReferenceRange.Text.Trim(), status);
            }
            else
            {
                success = await _api.SubmitUltrasoundResultAsync(_item.Id, _txtDescription.Text.Trim(), _txtConclusion.Text.Trim());
            }

            if (success)
            {
                WasModified = true;
                _lblStatus.Text = "✅ Đã lưu kết quả thành công!";
                _lblStatus.ForeColor = Color.FromArgb(22, 163, 74);
                await Task.Delay(900);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _btnSave.Enabled = true;
                _lblStatus.Text = "❌ Lỗi! Không thể lưu kết quả. Kiểm tra kết nối API.";
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
            }
        }

        private async Task OnCancelOrderClickAsync()
        {
            var confirm = MessageBox.Show(
                $"Hủy chỉ định \"{_item.ServiceName}\" của bệnh nhân {_item.PatientName}?\nThao tác này không thể hoàn tác.",
                "Xác nhận hủy chỉ định", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            _btnCancelOrder.Enabled = false;
            bool success = await _api.CancelClinicalOrderAsync(_item.Kind, _item.Id);

            if (success)
            {
                WasModified = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                _btnCancelOrder.Enabled = true;
                MessageBox.Show("Không thể hủy chỉ định này (có thể đã có kết quả). Vui lòng làm mới và thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
