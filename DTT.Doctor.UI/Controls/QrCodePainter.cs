using System.Drawing;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.Windows.Compatibility;

namespace DTT.Doctor.UI.Controls
{
    public static class QrCodePainter
    {
        // Sinh mã QR THẬT (ZXing) chứa đúng payload — quét được bằng App Mobile / bất kỳ ứng dụng đọc QR nào.
        // Trước đây hàm này chỉ vẽ các ô ngẫu nhiên theo hash của payload (hình giống QR nhưng không quét được).
        public static Bitmap GenerateQrBitmap(string payload, int size = 160)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Width = size,
                    Height = size,
                    Margin = 1,
                    CharacterSet = "UTF-8",
                    ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.M
                }
            };
            return writer.Write(payload);
        }
    }
}
