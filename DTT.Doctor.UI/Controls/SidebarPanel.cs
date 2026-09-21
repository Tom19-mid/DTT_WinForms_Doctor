using System.Windows.Forms;

namespace DTT.Doctor.UI.Controls
{
    /// <summary>
    /// Panel thường (KHÔNG bật WS_EX_COMPOSITED như AntiFlickerPanel) nhưng double-buffer và tự vẽ lại toàn bộ
    /// khi đổi kích thước — dùng cho thanh bên của MainDashboardForm. Trước đây là Panel trần: khi cửa sổ được
    /// mở tối đa / bật-tắt toàn màn hình (F11) / kích hoạt lại, các nút bo góc (RoundedButton dùng Region) dịch vị
    /// trí hoặc đổi kích thước mà panel cha không được vẽ lại đủ, để lại vệt điểm ảnh cũ (vd đường viền đỏ nằm
    /// ngay trên nút "Thông Tin", là vết còn sót của nút "Đăng Xuất").
    /// </summary>
    public class SidebarPanel : Panel
    {
        public SidebarPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            UpdateStyles();
            DoubleBuffered = true;
        }
    }
}
