using System.Drawing;
using System.Windows.Forms;

namespace DTT.Doctor.UI.Controls
{
    // Nhấp nháy nhanh cho các nút Button chuẩn (không phải RoundedButton) khi bấm — mô phỏng phản hồi
    // "F5 làm mới" trên Windows để người dùng biết thao tác đã thực sự được ghi nhận.
    internal static class ButtonFlashHelper
    {
        public static void Flash(Button button)
        {
            if (button == null || button.IsDisposed) return;

            Color original = button.BackColor;
            Color flash = ControlPaint.Light(original, 0.7f);
            int step = 0;
            const int totalSteps = 4; // sáng - tối - sáng - tối

            var timer = new Timer { Interval = 90 };
            timer.Tick += (s, e) =>
            {
                if (button.IsDisposed)
                {
                    timer.Stop();
                    timer.Dispose();
                    return;
                }
                button.BackColor = step % 2 == 0 ? flash : original;
                step++;
                if (step >= totalSteps)
                {
                    timer.Stop();
                    timer.Dispose();
                    button.BackColor = original;
                }
            };
            timer.Start();
        }
    }
}
