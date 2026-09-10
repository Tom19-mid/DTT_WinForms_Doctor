using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using DTT.Doctor.UI.Theme;
using Panel = System.Windows.Forms.Panel;
using Button = System.Windows.Forms.Button;
using Timer = System.Windows.Forms.Timer;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace DTT.Doctor.UI.Forms
{
    /// <summary>
    /// Quét mã QR ở mặt sau thẻ CCCD gắn chip qua webcam để tự động điền Số CCCD (và đối chiếu
    /// nhanh Họ tên/Ngày sinh/Giới tính hiển thị ngay trên màn hình quét) — hỗ trợ Lễ Tân nhập nhanh
    /// hơn gõ tay 12 số, KHÔNG thay thế bước đối chiếu thẻ cứng thực tế (Lễ Tân vẫn phải tự mắt nhìn
    /// và xác nhận khớp với thẻ, đây chỉ là công cụ nhập liệu nhanh).
    ///
    /// Định dạng dữ liệu QR CCCD gắn chip Việt Nam (công khai, phân cách bằng "|"):
    /// so_cccd|so_cmnd_cu|ho_va_ten|ngay_sinh(ddMMyyyy)|gioi_tinh|noi_thuong_tru|ngay_cap(ddMMyyyy)
    /// </summary>
    public class CccdQrScannerForm : Form
    {
        private PictureBox _pictureBox;
        private Label _lblStatus;
        private Label _lblPreview;
        private Button _btnCancel;

        private VideoCapture _capture;
        private Timer _timer;
        // ZXing thay cho OpenCvSharp.QRCodeDetector — bộ dò QR có sẵn của OpenCV yếu hơn hẳn với ảnh
        // thực tế (chụp nghiêng, hơi mờ, lóa mặt thẻ nhựa bóng...), thử nghiệm thật cho thấy đứng yên ở
        // "Đang quét..." dù mã QR đã rõ nét trong khung hình. ZXing là thư viện được dùng rộng rãi cho
        // đúng bài toán quét QR/mã vạch từ webcam, xử lý các điều kiện thực tế tốt hơn nhiều.
        private readonly BarcodeReader _qrReader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                TryInverted = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE },
            }
        };
        private bool _scanCompleted = false;

        public string ScannedCccdNumber { get; private set; }
        public string ScannedFullName { get; private set; }
        public string ScannedDob { get; private set; }
        public string ScannedGender { get; private set; }

        public CccdQrScannerForm()
        {
            InitializeComponent();
            this.Shown += async (s, e) => await StartCameraAsync();
            this.FormClosed += (s, e) => StopCamera();
        }

        private void InitializeComponent()
        {
            this.Text = "Quét QR CCCD";
            this.Size = new Size(560, 560);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            Label lblTitle = new Label
            {
                Text = "Đưa mã QR (mặt sau thẻ CCCD) vào vừa khung vuông màu xanh bên dưới",
                Font = ClinicalColors.GetMainFont(10.5f, FontStyle.Bold),
                ForeColor = ClinicalColors.PrimaryBlue,
                Location = new Point(20, 16),
                Size = new Size(500, 24),
                UseMnemonic = false
            };

            _pictureBox = new PictureBox
            {
                Location = new Point(20, 48),
                Size = new Size(500, 375),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };

            _lblStatus = new Label
            {
                Text = "Đang mở camera...",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Regular),
                ForeColor = ClinicalColors.TextMuted,
                Location = new Point(20, 430),
                Size = new Size(500, 20),
                UseMnemonic = false
            };

            _lblPreview = new Label
            {
                Text = "",
                Font = ClinicalColors.GetMainFont(9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 185, 129),
                Location = new Point(20, 452),
                Size = new Size(500, 20),
                UseMnemonic = false
            };

            _btnCancel = new Button
            {
                Text = "Hủy",
                Font = ClinicalColors.GetMainFont(10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(107, 114, 128),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(140, 38),
                Location = new Point(190, 480),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel,
                UseMnemonic = false
            };
            _btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.Add(lblTitle);
            this.Controls.Add(_pictureBox);
            this.Controls.Add(_lblStatus);
            this.Controls.Add(_lblPreview);
            this.Controls.Add(_btnCancel);
        }

        private async System.Threading.Tasks.Task StartCameraAsync()
        {
            try
            {
                // Mở camera trên background thread — VideoCapture(0) có thể mất 1-2 giây, tránh treo UI.
                // Ưu tiên backend DSHOW: trên nhiều máy, backend mặc định (MSMF) không thật sự áp dụng
                // yêu cầu đổi độ phân giải bên dưới dù không báo lỗi gì — DSHOW tuân thủ đáng tin cậy hơn.
                await System.Threading.Tasks.Task.Run(() =>
                {
                    try { _capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW); } catch { _capture = null; }
                    if (_capture == null || !_capture.IsOpened())
                    {
                        _capture?.Dispose();
                        _capture = new VideoCapture(0);
                    }
                });

                if (_capture == null || !_capture.IsOpened())
                {
                    _lblStatus.Text = "Không tìm thấy camera hoặc camera đang được ứng dụng khác sử dụng.";
                    _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
                    return;
                }

                // Yêu cầu độ phân giải cao hơn mặc định (nhiều webcam mặc định 640x480) — các ô nhỏ
                // (module) của mã QR CCCD cần đủ điểm ảnh mới đọc được rõ, đặc biệt khi thẻ không thể
                // đưa sát camera. Nếu camera không hỗ trợ 1280x720, driver sẽ tự chọn mức gần nhất.
                _capture.Set(VideoCaptureProperties.FrameWidth, 1280);
                _capture.Set(VideoCaptureProperties.FrameHeight, 720);

                _lblStatus.Text = "Đang quét...";
                _timer = new Timer { Interval = 120 };
                _timer.Tick += OnTimerTick;
                _timer.Start();
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Lỗi mở camera: " + ex.Message;
                _lblStatus.ForeColor = Color.FromArgb(220, 38, 38);
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (_scanCompleted || _capture == null || !_capture.IsOpened()) return;

            using var frame = new Mat();
            if (!_capture.Read(frame) || frame.Empty()) return;

            // Khung dẫn hướng: 1 ô vuông chiếm 70% cạnh ngắn hơn của khung hình, canh giữa — vừa là
            // vùng hiển thị cho người dùng canh thẻ vào, vừa là vùng thật sự đem đi giải mã (crop rồi
            // phóng to gấp đôi trước khi decode). Cắt bớt nền xung quanh + phóng to giúp ZXing đọc rõ
            // hơn các module nhỏ của mã QR so với dùng nguyên khung hình đầy đủ (thường có QR chỉ chiếm
            // 1 phần nhỏ, phần còn lại là nền/tay/bàn phím không liên quan).
            int side = (int)(Math.Min(frame.Width, frame.Height) * 0.7);
            int roiX = (frame.Width - side) / 2;
            int roiY = (frame.Height - side) / 2;
            var guideRect = new OpenCvSharp.Rect(roiX, roiY, side, side);

            using var cropped = new Mat(frame, guideRect);
            using var upscaled = new Mat();
            Cv2.Resize(cropped, upscaled, new OpenCvSharp.Size(cropped.Width * 2, cropped.Height * 2), interpolation: InterpolationFlags.Cubic);

            // Hiển thị khung hình đầy đủ kèm khung dẫn hướng vẽ đè lên (dispose bitmap cũ để tránh rò
            // rỉ bộ nhớ khi chạy liên tục nhiều phút).
            var oldImage = _pictureBox.Image;
            var displayBitmap = BitmapConverter.ToBitmap(frame);
            using (var g = Graphics.FromImage(displayBitmap))
            {
                using var pen = new Pen(Color.FromArgb(16, 185, 129), 4);
                g.DrawRectangle(pen, roiX, roiY, side, side);
            }
            _pictureBox.Image = displayBitmap;
            oldImage?.Dispose();

            try
            {
                // Ưu tiên decode vùng đã crop+phóng to trước (thường hiệu quả hơn); nếu không thấy,
                // thử lại trên toàn khung hình gốc phòng khi mã QR nằm lệch ra ngoài khung dẫn hướng.
                using var upscaledBitmap = BitmapConverter.ToBitmap(upscaled);
                var result = _qrReader.Decode(upscaledBitmap) ?? _qrReader.Decode(displayBitmap);
                if (result == null || string.IsNullOrWhiteSpace(result.Text)) return;

                TryHandleDecoded(result.Text);
            }
            catch
            {
                // Khung hình mờ/góc nghiêng khiến decode thất bại — bỏ qua, thử lại ở khung hình kế tiếp.
            }
        }

        private void TryHandleDecoded(string decoded)
        {
            var parts = decoded.Split('|');
            if (parts.Length == 0) return;

            string cccd = parts[0].Trim();
            if (!Regex.IsMatch(cccd, @"^\d{12}$"))
            {
                // Không phải đúng định dạng QR CCCD gắn chip (12 chữ số ở trường đầu tiên) — có thể
                // người dùng lỡ quét nhầm 1 mã QR khác, KHÔNG tự động chấp nhận.
                _lblPreview.ForeColor = Color.FromArgb(220, 38, 38);
                _lblPreview.Text = "Mã QR quét được không đúng định dạng CCCD. Vui lòng thử lại.";
                return;
            }

            ScannedCccdNumber = cccd;
            ScannedFullName = parts.Length > 2 ? parts[2].Trim() : null;
            ScannedGender = parts.Length > 4 ? parts[4].Trim() : null;

            if (parts.Length > 3 && DateTime.TryParseExact(parts[3].Trim(), "ddMMyyyy",
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dob))
            {
                ScannedDob = dob.ToString("dd/MM/yyyy");
            }

            _scanCompleted = true;
            _timer?.Stop();

            _lblPreview.ForeColor = Color.FromArgb(16, 185, 129);
            _lblPreview.Text = $"Đã quét: {ScannedCccdNumber} — {ScannedFullName ?? "(không đọc được tên)"}";
            _lblStatus.Text = "Quét thành công!";

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void StopCamera()
        {
            try
            {
                _timer?.Stop();
                _timer?.Dispose();
                _capture?.Release();
                _capture?.Dispose();
                _pictureBox?.Image?.Dispose();
            }
            catch { /* dọn tài nguyên khi đóng dialog, lỗi ở đây không quan trọng */ }
        }
    }
}
