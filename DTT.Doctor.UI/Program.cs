using System;
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

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => {
                string msg = "THREAD EXCEPTION:\n" + e.Exception?.ToString();
                try { File.WriteAllText(@"D:\DoAnTotNghiep\crash_log.txt", msg); } catch { }
                MessageBox.Show(msg, "LỖI ỨNG DỤNG WINFORMS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                string msg = "UNHANDLED EXCEPTION:\n" + e.ExceptionObject?.ToString();
                try { File.WriteAllText(@"D:\DoAnTotNghiep\crash_log.txt", msg); } catch { }
                MessageBox.Show(msg, "LỖI CHƯƠNG TRÌNH", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

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
    }
}