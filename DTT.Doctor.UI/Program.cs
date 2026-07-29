using System;
using System.Windows.Forms;
using DTT.Doctor.UI.Forms;

namespace DTT.Doctor.UI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new LoginForm());
        }
    }
}