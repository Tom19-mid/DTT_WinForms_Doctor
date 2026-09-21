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

        // Thanh menu bên trái + thanh tiêu đề phía trên: xanh navy bệnh viện — trang trọng, đáng tin, chữ trắng đọc rõ (thay nền trắng chói).
        // Muốn đổi tông cả 2 thanh: chỉ cần sửa 6 màu Nav* dưới đây. Các tông đã thử:
        //   slate  : Base #1E293B, Deep #0F172A, Light #334155, Active #0F766E, Text #CBD5E1, Accent #5EEAD4
        //   teal   : Base #0F4C5C, Deep #0B3A47, Light #156072, Active #0E7490, Text #CBE2EA, Accent #99F6E4
        public static readonly Color NavBase = Color.FromArgb(18, 53, 91);        // #12355B - Sidebar & top header background
        public static readonly Color NavDeep = Color.FromArgb(12, 37, 68);        // #0C2544 - Dividers
        public static readonly Color NavLight = Color.FromArgb(29, 74, 122);      // #1D4A7A - User card / hover
        public static readonly Color NavActive = Color.FromArgb(30, 111, 184);    // #1E6FB8 - Selected nav item (chữ trắng đạt tương phản ~5:1)
        public static readonly Color OnNavText = Color.FromArgb(203, 220, 235);   // #CBDCEB - Normal text on navy
        public static readonly Color OnNavAccent = Color.FromArgb(125, 211, 252); // #7DD3FC - Accent text/bar on navy

        // Background Tones
        public static readonly Color GhostWhite = Color.FromArgb(248, 250, 252);  // #F8FAFC - App main background
        public static readonly Color CardBackground = Color.White;                // #FFFFFF - Card containers
        // Viền/đường phân cách dùng xuyên suốt app (thẻ, panel, nút, lưới...). Trước đây #E2E8F0 quá nhạt, khó phân biệt ranh giới
        // giữa các vùng — đậm hơn thành #CBD5E1 để nhìn rõ chỗ nào với chỗ nào. BorderStrong dành cho đường cần nổi bật hơn
        // (vd đường dưới hàng tiêu đề của bảng).
        public static readonly Color BorderGray = Color.FromArgb(203, 213, 225);  // #CBD5E1 - Clear thin dividers/borders
        public static readonly Color BorderStrong = Color.FromArgb(148, 163, 184);// #94A3B8 - Emphasised borders

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
            // Màu chủ đạo của các control Material (nút "Đăng nhập", gạch chân/nhãn ô nhập khi focus, thanh tiến trình...) theo
            // bảng màu Nav* (navy) để khớp nền màn đăng nhập và thanh menu — trước đây là indigo #4338CA.
            manager.ColorScheme = new MaterialColorScheme(
                NavActive,                    // Primary (#1E6FB8 - xanh dương nổi trên thẻ trắng)
                NavBase,                      // Dark Primary (#12355B)
                NavLight,                     // Light Primary (#1D4A7A)
                NavActive,                    // Accent (#1E6FB8)
                MaterialTextShade.WHITE
            );
        }
    }
}
