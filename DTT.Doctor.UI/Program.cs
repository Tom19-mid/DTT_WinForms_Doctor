using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DTT.Doctor.Services.Core;
using DTT.Doctor.UI.Forms;

namespace DTT.Doctor.UI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            ApplyAppIconToAllForms();

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => ReportUnexpectedError("THREAD EXCEPTION", e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ReportUnexpectedError("UNHANDLED EXCEPTION", e.ExceptionObject as Exception);

            bool keepRunning = true;
            while (keepRunning)
            {
                using (var loginForm = new LoginForm())
                {
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {
                        // Kết nối kênh real-time (SignalR) nhận thông báo Admin — fire-and-forget để
                        // không chặn mở Dashboard nếu mạng chậm/server chưa sẵn sàng (real-time là
                        // tính năng tăng cường, không phải điều kiện bắt buộc để làm việc).
                        _ = NotificationHubService.ConnectAsync(new ApiService().BaseUrl);

                        using (var dashboard = new MainDashboardForm())
                        {
                            Application.Run(dashboard);
                        }

                        _ = NotificationHubService.DisconnectAsync();

                        // If TokenVault was cleared (logout), loop back to show LoginForm again
                        keepRunning = !TokenVault.IsAuthenticated;
                    }
                    else
                    {
                        keepRunning = false; // User clicked X on login form
                    }
                }
            }
        }

        // Lỗi không lường trước: ghi chi tiết kỹ thuật vào %LocalAppData%\DTT Healthcare\Logs (để gửi lại
        // cho người phát triển khi cần) và chỉ hiện cho người dùng thông báo ngắn gọn, không kèm stack trace.
        private static void ReportUnexpectedError(string kind, Exception ex)
        {
            string logPath = null;
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DTT Healthcare", "Logs");
                Directory.CreateDirectory(dir);
                logPath = Path.Combine(dir, "crash_log.txt");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {kind}:\n{ex}\n\n");
            }
            catch { logPath = null; }

            string msg = "Ứng dụng gặp một lỗi không mong muốn.\n\nBạn có thể thử lại thao tác vừa rồi. " +
                         "Nếu lỗi lặp lại, vui lòng kiểm tra kết nối Internet hoặc khởi động lại ứng dụng.";
            if (logPath != null) msg += "\n\nChi tiết kỹ thuật đã được ghi tại:\n" + logPath;
            MessageBox.Show(msg, "DTT Healthcare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // WinForms không tự dùng ApplicationIcon (icon của file .exe) cho cửa sổ — mỗi Form mặc định
        // hiện icon chung của WinForms trên thanh tiêu đề/taskbar. 22 form không có lớp cha chung nên
        // gắn icon tập trung tại đây: mỗi lần Application rảnh, form nào mới mở (kể cả hộp thoại
        // ShowDialog) mà chưa gắn icon và không tự tắt ShowIcon thì gắn logo DTT.
        private static void ApplyAppIconToAllForms()
        {
            Icon appIcon;
            try { appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { return; }
            if (appIcon == null) return;

            var handled = new HashSet<Form>();
            Application.Idle += (s, e) =>
            {
                foreach (Form form in Application.OpenForms)
                {
                    if (form.ShowIcon && handled.Add(form))
                        form.Icon = appIcon;
                }
                handled.RemoveWhere(f => f.IsDisposed);
            };
        }
    }
}