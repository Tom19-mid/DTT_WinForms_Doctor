using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DTT.Doctor.UI.Controls
{
    // Thay cho các toast góc màn hình tự vẽ (nền tối, viền màu) trước đây — dùng thẳng balloon tip
    // của Windows (NotifyIcon) để thông báo trông giản dị, đúng phong cách hệ điều hành, không màu mè.
    internal static class SystemNotifier
    {
        public static void Show(ref NotifyIcon notifyIcon, Form owner, string title, string message, Color accentColor)
        {
            if (owner == null || owner.IsDisposed) return;

            if (notifyIcon == null)
            {
                notifyIcon = new NotifyIcon
                {
                    Icon = owner.Icon ?? SystemIcons.Application,
                    Text = "DTT Clinic",
                    Visible = true
                };
            }

            notifyIcon.BalloonTipIcon = ToolTipIconFor(accentColor);
            notifyIcon.BalloonTipTitle = CleanTitle(title);
            notifyIcon.BalloonTipText = message;
            notifyIcon.ShowBalloonTip(5000);
        }

        private static ToolTipIcon ToolTipIconFor(Color c)
        {
            if (c.R > 200 && c.G < 120 && c.B < 120) return ToolTipIcon.Error;
            if (c.R > 200 && c.G >= 120 && c.B < 100) return ToolTipIcon.Warning;
            return ToolTipIcon.Info;
        }

        private static string CleanTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return title;

            var sb = new StringBuilder(title.Length);
            for (int i = 0; i < title.Length; i++)
            {
                char c = title[i];
                if (char.IsHighSurrogate(c) && i + 1 < title.Length && char.IsLowSurrogate(title[i + 1]))
                {
                    i++; // bỏ cả cặp surrogate (emoji ngoài BMP, vd 🔔 🔬 🚫 🔄)
                    continue;
                }
                if (c >= '←' && c <= '⯿') continue; // mũi tên/dingbat/ký hiệu (vd ✅ ❌ ⏰)
                sb.Append(c);
            }
            return sb.ToString().Trim();
        }
    }
}
