using System.Drawing;
using ReaLTaiizor.Colors;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Util;

namespace DTT.Doctor.UI.Theme
{
    public static class ClinicalColors
    {
        // Primary Brand & Sidebar Tones (#4338CA Indigo Theme)
        public static readonly Color DeepNavy = Color.FromArgb(67, 56, 202);      // #4338CA - Modern Healthcare Indigo
        public static readonly Color SidebarDark = Color.FromArgb(49, 46, 129);   // #312E81 - Hover / Active sidebar button
        public static readonly Color PrimaryBlue = Color.FromArgb(67, 56, 202);   // #4338CA - Action buttons and active tabs

        // Background Tones
        public static readonly Color GhostWhite = Color.FromArgb(248, 250, 252);  // #F8FAFC - App main background
        public static readonly Color CardBackground = Color.White;                // #FFFFFF - Card containers
        public static readonly Color BorderGray = Color.FromArgb(226, 232, 240);  // #E2E8F0 - Smooth thin dividers

        // Text & Typography
        public static readonly Color TextDark = Color.FromArgb(15, 23, 42);       // #0F172A - Main titles and headings
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);   // #64748B - Subtitles and labels
        public static readonly Color TextWhite = Color.White;

        // Status & KPI Pills
        public static readonly Color StatusWaitingBg = Color.FromArgb(254, 243, 199);   // Amber background (#FEF3C7)
        public static readonly Color StatusWaitingText = Color.FromArgb(180, 83, 9);    // Amber text
        
        public static readonly Color StatusInProgressBg = Color.FromArgb(219, 234, 254); // Blue background (#DBEAFE)
        public static readonly Color StatusInProgressText = Color.FromArgb(29, 78, 216); // Blue text

        public static readonly Color StatusCompletedBg = Color.FromArgb(209, 250, 229);  // Emerald background (#D1FAE5)
        public static readonly Color StatusCompletedText = Color.FromArgb(4, 120, 87);   // Emerald text

        public static readonly Color TotalPillBg = Color.FromArgb(243, 232, 255);        // Purple background (#F3E8FF)
        public static readonly Color TotalPillText = Color.FromArgb(107, 33, 168);       // Purple text

        // Font Helper
        public static Font GetMainFont(float size, FontStyle style = FontStyle.Regular)
        {
            return new Font("Segoe UI", size, style, GraphicsUnit.Point);
        }

        // ReaLTaiizor Theme Configuration Helper
        public static void ConfigureMaterialSkin(System.Windows.Forms.Form form = null)
        {
            var manager = MaterialSkinManager.Instance;
            if (form is MaterialForm mf)
            {
                manager.AddFormToManage(mf);
            }
            manager.Theme = MaterialSkinManager.Themes.LIGHT;
            manager.ColorScheme = new MaterialColorScheme(
                DeepNavy,                     // Primary (#4338CA - Modern Healthcare Indigo)
                SidebarDark,                  // Dark Primary (#312E81)
                PrimaryBlue,                  // Light Primary (#4338CA)
                Color.FromArgb(16, 185, 129), // Accent (Emerald #10B981)
                MaterialTextShade.WHITE
            );
        }
    }
}
