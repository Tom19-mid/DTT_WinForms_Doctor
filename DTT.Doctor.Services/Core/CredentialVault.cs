using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DTT.Doctor.Services.Core
{
    // Lưu Số điện thoại + mật khẩu đăng nhập khi người dùng bật "Ghi nhớ mật khẩu", để lần sau
    // mở app không phải gõ lại. Mật khẩu được mã hóa bằng Windows DPAPI (ProtectedData, gắn với
    // tài khoản Windows hiện đang đăng nhập) trước khi ghi ra đĩa — không lưu plaintext, và file
    // chỉ giải mã được trên đúng máy + đúng tài khoản Windows đã lưu nó.
    public static class CredentialVault
    {
        private const string FilePath = "saved_credentials.dat";

        private class SavedCredential
        {
            public string Phone { get; set; } = string.Empty;
            public string EncryptedPassword { get; set; } = string.Empty; // Base64(DPAPI-protected bytes)
        }

        public static void Save(string phone, string password)
        {
            try
            {
                var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(password), null, DataProtectionScope.CurrentUser);
                var payload = new SavedCredential { Phone = phone, EncryptedPassword = Convert.ToBase64String(protectedBytes) };
                File.WriteAllText(FilePath, JsonSerializer.Serialize(payload));
            }
            catch { }
        }

        public static (bool Found, string Phone, string Password) TryLoad()
        {
            try
            {
                if (!File.Exists(FilePath)) return (false, string.Empty, string.Empty);
                var payload = JsonSerializer.Deserialize<SavedCredential>(File.ReadAllText(FilePath));
                if (payload == null || string.IsNullOrEmpty(payload.EncryptedPassword)) return (false, string.Empty, string.Empty);
                var bytes = ProtectedData.Unprotect(Convert.FromBase64String(payload.EncryptedPassword), null, DataProtectionScope.CurrentUser);
                return (true, payload.Phone, Encoding.UTF8.GetString(bytes));
            }
            catch
            {
                // Giải mã thất bại (đổi máy, đổi tài khoản Windows, file hỏng...) — coi như chưa có gì lưu sẵn
                // thay vì crash màn đăng nhập.
                return (false, string.Empty, string.Empty);
            }
        }

        public static void Clear()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
        }
    }
}
