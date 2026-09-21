using System;
using DTT.Doctor.Services.Models;

namespace DTT.Doctor.Services.Core
{
    public static class TokenVault
    {
        public static string Token { get; private set; } = string.Empty;
        public static Guid UserId { get; private set; }
        public static int DoctorId { get; private set; }
        // Chưa đăng nhập = KHÔNG có vai trò (trước đây mặc định RoleId=2/"DOCTOR"/"Bác sĩ" → phiên rỗng vẫn bị coi là Bác sĩ,
        // và nếu server thiếu RoleCode thì tài khoản bất kỳ cũng bị gán quyền Bác sĩ).
        public static int RoleId { get; private set; }
        public static string RoleCode { get; private set; } = string.Empty;
        public static string RoleName { get; private set; } = string.Empty;
        public static string FullName { get; private set; } = string.Empty;
        public static string Degree { get; private set; } = string.Empty;
        public static string ClinicRoom { get; private set; } = string.Empty;
        public static int SpecialtyId { get; private set; }
        public static string SpecialtyName { get; private set; } = string.Empty;
        public static string AvatarUrl { get; private set; } = string.Empty;
        public static bool IsAuthenticated => !string.IsNullOrEmpty(Token);

        public static void StoreSession(DoctorAuthResponseDto auth)
        {
            if (auth == null) return;
            Token = auth.Token;
            UserId = auth.UserId;
            DoctorId = auth.DoctorId;
            RoleId = auth.RoleId;
            RoleCode = auth.RoleCode ?? string.Empty;
            RoleName = auth.RoleName ?? string.Empty;
            // Cùng lý do với RoleCode/RoleName ở trên: DoctorAuthResponseDto có default = string.Empty,
            // nhưng deserialize JSON với field null/thiếu sẽ GHI ĐÈ default đó thành null thật, khiến
            // mọi nơi gọi TokenVault.FullName.ToUpper() (vd. DoctorScheduleForm) crash NullReferenceException.
            FullName = auth.FullName ?? string.Empty;
            Degree = auth.Degree ?? string.Empty;
            ClinicRoom = auth.ClinicRoom ?? string.Empty;
            SpecialtyId = auth.SpecialtyId;
            SpecialtyName = auth.SpecialtyName ?? string.Empty;
            AvatarUrl = auth.AvatarUrl ?? string.Empty;
        }

        // Trước đây các nhánh dưới đây coi tên chứa chuỗi con "000" là "chưa có tên thật" và thay
        // bằng 1 tên nhân viên DEMO KHÁC (vd "Nguyễn Thị Minh Châu") — nhưng "000" có thể xuất hiện
        // trong SỐ ĐIỆN THOẠI THẬT của chính nhân viên đó (tên fallback tự sinh "{RoleName} {4 số
        // cuối SĐT}" ở AuthController.DoctorLogin), khiến nhân viên đăng nhập thấy TÊN NGƯỜI KHÁC
        // hiện lên như thể là mình. Chỉ coi là "chưa có tên thật" khi rỗng hoặc đúng bằng các placeholder
        // cố định đã biết (không suy diễn qua substring "000" nữa).
        public static string GetFormattedTitleName()
        {
            string fullName = !string.IsNullOrWhiteSpace(FullName) ? FullName.Trim() : string.Empty;

            bool isReceptionist = RoleId == 4 || RoleCode == "RECEPTIONIST" || (!string.IsNullOrEmpty(RoleName) && RoleName.Contains("Lễ tân"));
            bool isNurse        = RoleId == 5 || RoleCode == "NURSE"        || (!string.IsNullOrEmpty(RoleName) && RoleName.Contains("Điều dưỡng"));
            bool isLabTech      = RoleId == 6 || RoleCode == "LAB_TECH"     || (!string.IsNullOrEmpty(RoleName) && RoleName.Contains("Kỹ thuật"));
            bool isPharmacist   = RoleId == 7 || RoleCode == "PHARMACIST"   || (!string.IsNullOrEmpty(RoleName) && RoleName.Contains("Dược sĩ"));

            // Thiếu tên thật thì hiện đúng tên VAI TRÒ ("Lễ tân", "Điều dưỡng"...), KHÔNG được thay bằng tên người demo
            // ("Nguyễn Thị Minh Châu", "Phạm Thị Hồng Hạnh", "Trần Tuấn Kiệt", "Trịnh Mai Phương", "BS. CKII Trịnh Hoàng Minh")
            // — nhân viên đăng nhập sẽ thấy tên/chữ ký của người khác hiện lên như thể là mình.
            if (isReceptionist)
            {
                if (string.IsNullOrEmpty(fullName)) return "Lễ tân";
                return fullName.Replace("BS.", "").Replace("BS", "").Replace("ThS.", "").Replace("TS.", "").Replace("LT.", "").Trim();
            }

            if (isNurse)
            {
                if (string.IsNullOrEmpty(fullName)) return "Điều dưỡng";
                return fullName.Replace("BS.", "").Replace("BS", "").Replace("ĐD.", "").Trim();
            }

            if (isLabTech)
            {
                if (string.IsNullOrEmpty(fullName)) return "Kỹ thuật viên CLS";
                return fullName.Replace("KTV.", "").Replace("KTV", "").Trim();
            }

            if (isPharmacist)
            {
                if (string.IsNullOrEmpty(fullName)) return "Dược sĩ";
                return fullName.Replace("Ds.", "").Replace("DS.", "").Replace("DS", "").Trim();
            }

            if (string.IsNullOrEmpty(fullName))
            {
                return "Bác sĩ";
            }

            if (fullName.StartsWith("BS") || fullName.StartsWith("ThS") || fullName.StartsWith("TS") ||
                fullName.StartsWith("LT") || fullName.StartsWith("ĐD") || fullName.StartsWith("DS") || fullName.StartsWith("Ds") || fullName.StartsWith("KTV"))
            {
                return fullName;
            }

            if (RoleId == 2 || RoleCode == "DOCTOR" || (!string.IsNullOrEmpty(RoleName) && RoleName.Contains("Bác sĩ")))
                return (!string.IsNullOrEmpty(Degree) ? Degree + " " : "BS. ") + fullName;

            return fullName;
        }

        public static void Clear()
        {
            Token = string.Empty;
            UserId = Guid.Empty;
            DoctorId = 0;
            RoleId = 0;
            RoleCode = string.Empty;
            RoleName = string.Empty;
            FullName = string.Empty;
            Degree = string.Empty;
            ClinicRoom = string.Empty;
            SpecialtyId = 0;
            SpecialtyName = string.Empty;
            AvatarUrl = string.Empty;
        }
    }
}

