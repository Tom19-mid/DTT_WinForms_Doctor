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
using ReaLTaiizor.Controls;
using Panel = System.Windows.Forms.Panel;
using Button = System.Windows.Forms.Button;

namespace DTT.Doctor.UI.Forms
{
    /// <summary>
    /// Cửa sổ riêng để KTV nhập kết quả 1 chỉ định Xét nghiệm/Siêu âm — tách khỏi panel dồn chung trước đây
    /// để có đủ chỗ hiển thị ẢNH SIÊU ÂM THẬT (xem trước dạng thumbnail, đính kèm/gỡ từng ảnh).
    /// </summary>
    public class ClinicalResultDialogForm : Form
    {
        private readonly ApiService _api = new ApiService();
        private readonly ClinicalOrderQueueItem _item;
        private readonly bool _isTest;

        private List<string> _imageUrls = new List<string>();
        private FlowLayoutPanel _flowImages;
        private Label _lblImagesEmpty;

        private MaterialTextBoxEdit _txtResultValue, _txtUnit, _txtReferenceRange;
        private ComboBox _cboResultStatus;
        private MaterialTextBoxEdit _txtDescription, _txtConclusion;

        private Label _lblStatus;
        private Button _btnSave;
        private Button _btnCancelOrder;

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
            Size = new Size(720, _isTest ? 590 : 720);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ClinicalColors.GhostWhite;
            Font = ClinicalColors.GetMainFont(10f, FontStyle.Regular);

            // ── Header ───────────────────────────────────────────────────
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Color.White };
            Panel pnlHeaderBorder = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = ClinicalColors.BorderGray };
            pnlHeader.Controls.Add(pnlHeaderBorder);

            Label lblPatient = new Label
            {
                Text = $"{(_isTest ? "🧪" : "📷")}  {_item.PatientName.ToUpper()}" + (_item.IsUrgent ? "   🚨 KHẨN" : ""),
                Font = ClinicalColors.GetMainFont(13f, FontStyle.Bold),
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

            // ── Body ─────────────────────────────────────────────────────
            Panel pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 16), AutoScroll = true };

            int y = 0;
            Label MkLbl(string text, int yy) => new Label
            {
                Text = text,
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(0, yy),
                AutoSize = true
            };

            var bodyControls = new List<System.Windows.Forms.Control>();

            if (_isTest)
            {
                var lbl1 = MkLbl("Kết quả", y); y += 18;
                _txtResultValue = new MaterialTextBoxEdit { Location = new Point(0, y), Size = new Size(660, 48), Hint = "Ví dụ: Hồng cầu 4.5 T/L, Bạch cầu 7.2 G/L", Text = _item.ResultValue ?? "" };
                y += 60;

                var lblU = MkLbl("Đơn vị", y);
                var lblR = MkLbl("Khoảng tham chiếu", y); lblR.Location = new Point(340, y);
                y += 18;
                _txtUnit = new MaterialTextBoxEdit { Location = new Point(0, y), Size = new Size(320, 48), Hint = "Ví dụ: mg/dL", Text = _item.Unit ?? "" };
                _txtReferenceRange = new MaterialTextBoxEdit { Location = new Point(340, y), Size = new Size(320, 48), Hint = "Ví dụ: 3.8 - 5.5", Text = _item.ReferenceRange ?? "" };
                y += 60;

                var lblStatusHdr = MkLbl("Đánh giá kết quả", y); y += 18;
                _cboResultStatus = new ComboBox
                {
                    Location = new Point(0, y),
                    Size = new Size(660, 32),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular)
                };
                _cboResultStatus.Items.Add("Normal — Bình thường");
                _cboResultStatus.Items.Add("Abnormal — Bất thường");
                _cboResultStatus.SelectedIndex = _item.Status == "Abnormal" ? 1 : 0;
                y += 40;

                bodyControls.AddRange(new System.Windows.Forms.Control[] { lbl1, _txtResultValue, lblU, _txtUnit, lblR, _txtReferenceRange, lblStatusHdr, _cboResultStatus });
            }
            else
            {
                var lblD = MkLbl("Mô tả hình ảnh", y); y += 18;
                _txtDescription = new MaterialTextBoxEdit { Location = new Point(0, y), Size = new Size(660, 56), Hint = "Ví dụ: Tuyến giáp kích thước bình thường..." };
                y += 66;

                var lblC = MkLbl("Kết luận", y); y += 18;
                _txtConclusion = new MaterialTextBoxEdit { Location = new Point(0, y), Size = new Size(660, 56), Hint = "Ví dụ: Không phát hiện bất thường" };
                y += 66;

                var lblImg = MkLbl("Ảnh siêu âm đính kèm", y); y += 22;

                Button btnAttach = new Button
                {
                    Text = "📎  Đính Kèm Ảnh Mới",
                    Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(224, 231, 255),
                    ForeColor = ClinicalColors.PrimaryBlue,
                    Location = new Point(0, y),
                    Size = new Size(180, 36),
                    Cursor = Cursors.Hand
                };
                btnAttach.FlatAppearance.BorderSize = 0;
                btnAttach.Click += async (s, e) => await OnAttachImageClickAsync();
                y += 46;

                _lblImagesEmpty = new Label
                {
                    Text = "Chưa có ảnh nào.",
                    Font = ClinicalColors.GetMainFont(9f, FontStyle.Regular),
                    ForeColor = ClinicalColors.TextMuted,
                    Location = new Point(0, y),
                    Size = new Size(660, 22)
                };

                _flowImages = new FlowLayoutPanel
                {
                    Location = new Point(0, y),
                    Size = new Size(660, 230),
                    AutoScroll = true,
                    WrapContents = true,
                    FlowDirection = FlowDirection.LeftToRight
                };
                y += 236;

                bodyControls.AddRange(new System.Windows.Forms.Control[] { lblD, _txtDescription, lblC, _txtConclusion, lblImg, btnAttach, _lblImagesEmpty, _flowImages });
            }

            _lblStatus = new Label
            {
                Text = "",
                Font = ClinicalColors.GetMainFont(9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                Location = new Point(0, y + 6),
                Size = new Size(660, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };
            bodyControls.Add(_lblStatus);

            foreach (var c in bodyControls) pnlBody.Controls.Add(c);

            // ── Footer ───────────────────────────────────────────────────
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Color.White };
            Panel pnlFooterBorder = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = ClinicalColors.BorderGray };
            pnlFooter.Controls.Add(pnlFooterBorder);

            _btnCancelOrder = new Button
            {
                Text = "❌  Hủy Chỉ Định",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(185, 28, 28),
                BackColor = Color.White,
                Location = new Point(20, 12),
                Size = new Size(160, 40),
                Cursor = Cursors.Hand
            };
            _btnCancelOrder.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            _btnCancelOrder.FlatAppearance.BorderSize = 1;
            _btnCancelOrder.Click += async (s, e) => await OnCancelOrderClickAsync();

            Button btnClose = new Button
            {
                Text = "Đóng",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 40),
                Location = new Point(390, 12),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnClose.FlatAppearance.BorderSize = 1;
            btnClose.Click += (s, e) => { DialogResult = WasModified ? DialogResult.OK : DialogResult.Cancel; Close(); };

            _btnSave = new RoundedButton
            {
                Text = "✅  Lưu Kết Quả",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 185, 129),
                HoverBackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                BorderRadius = 10,
                Size = new Size(185, 40),
                Location = new Point(515, 12),
                Cursor = Cursors.Hand
            };
            _btnSave.Click += async (s, e) => await OnSaveClickAsync();

            pnlFooter.Controls.Add(_btnCancelOrder);
            pnlFooter.Controls.Add(btnClose);
            pnlFooter.Controls.Add(_btnSave);

            Controls.Add(pnlBody);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
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

                Panel card = new Panel { Size = new Size(112, 140), Margin = new Padding(4) };
                PictureBox pb = new PictureBox
                {
                    Size = new Size(104, 104),
                    Location = new Point(4, 4),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.FromArgb(241, 245, 249),
                    BorderStyle = BorderStyle.FixedSingle
                };
                try { pb.LoadAsync(fullUrl); } catch { }

                Button btnRemove = new Button
                {
                    Text = "🗑️ Xóa",
                    Font = ClinicalColors.GetMainFont(8f, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.FromArgb(185, 28, 28),
                    BackColor = Color.FromArgb(254, 226, 226),
                    Location = new Point(4, 112),
                    Size = new Size(104, 24),
                    Cursor = Cursors.Hand
                };
                btnRemove.FlatAppearance.BorderSize = 0;
                btnRemove.Click += async (s, e) => await OnRemoveImageClickAsync(index);

                card.Controls.Add(pb);
                card.Controls.Add(btnRemove);
                _flowImages.Controls.Add(card);
            }
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
                _lblStatus.Text = "✅ Đã đính kèm ảnh.";
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
                _lblStatus.Text = "✅ Đã gỡ ảnh.";
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
