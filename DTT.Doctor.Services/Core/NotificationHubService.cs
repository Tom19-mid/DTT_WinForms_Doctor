using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace DTT.Doctor.Services.Core
{
    /// <summary>
    /// Kênh real-time (SignalR) tới DTT_API_Back_end/Hubs/NotificationHub.cs — trước đây Bác sĩ
    /// không có cách nào nhận thông báo do Admin tạo (không có endpoint/poll nào cho việc này), đúng
    /// như QA report "Admin tạo thông báo nhưng trên winforms bác sĩ không thấy thông báo đó".
    /// Static singleton (giống TokenVault) để MainDashboardForm gọi thẳng không cần DI container —
    /// khớp với cách ApiService/TokenVault đã được dùng trong toàn bộ project này.
    /// </summary>
    public static class NotificationHubService
    {
        private static HubConnection _connection;

        /// <summary>Bắn ra mỗi khi Hub báo "NotificationsChanged" — MainDashboardForm tự fetch lại
        /// GET /api/notifications để lấy nội dung mới nhất và hiện toast.</summary>
        public static event Action NotificationsChanged;

        public static async Task ConnectAsync(string baseUrl)
        {
            if (_connection != null) return;
            if (!TokenVault.IsAuthenticated) return;

            try
            {
                var hubUrl = baseUrl.TrimEnd('/') + "/hubs/notifications";
                _connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(TokenVault.Token);
                    })
                    .WithAutomaticReconnect()
                    .Build();

                _connection.On("NotificationsChanged", () => NotificationsChanged?.Invoke());

                await _connection.StartAsync();
            }
            catch (Exception ex)
            {
                // Real-time là tính năng tăng cường — lỗi kết nối (server chưa khởi động, mạng lỗi...)
                // KHÔNG được làm crash app hay chặn luồng đăng nhập/làm việc chính của Bác sĩ.
                Console.WriteLine("[SignalR] Connect failed (non-fatal): " + ex.Message);
                _connection = null;
            }
        }

        public static async Task DisconnectAsync()
        {
            if (_connection == null) return;
            try
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
            }
            catch { /* ignore */ }
            finally
            {
                _connection = null;
            }
        }
    }
}
