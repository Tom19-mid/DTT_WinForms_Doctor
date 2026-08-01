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
                        using (var dashboard = new MainDashboardForm())
                        {
                            Application.Run(dashboard);
                        }

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