using System;
using DTT.Doctor.Services.Models;

namespace DTT.Doctor.Services.Core
{
    public static class TokenVault
    {
        public static string Token { get; private set; } = string.Empty;
        public static Guid UserId { get; private set; }
        public static int DoctorId { get; private set; }
        public static int RoleId { get; private set; } = 2;
        public static string RoleCode { get; private set; } = "DOCTOR";
        public static string RoleName { get; private set; } = "Bác sĩ";
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
            RoleId = auth.RoleId > 0 ? auth.RoleId : 2;
            RoleCode = !string.IsNullOrEmpty(auth.RoleCode) ? auth.RoleCode : "DOCTOR";
            RoleName = !string.IsNullOrEmpty(auth.RoleName) ? auth.RoleName : "Bác sĩ";
            FullName = auth.FullName;
            Degree = auth.Degree;
            ClinicRoom = auth.ClinicRoom;
            SpecialtyId = auth.SpecialtyId;
            SpecialtyName = auth.SpecialtyName;
            AvatarUrl = auth.AvatarUrl ?? string.Empty;
        }

        public static string GetFormattedTitleName()
        {
            string fullName = !string.IsNullOrWhiteSpace(FullName) ? FullName.Trim() : string.Empty;

            bool isReceptionist = RoleId == 4 || RoleCode == "RECEPTIONIST" || (!string.IsNullOrEmpty(RoleName) && RoleName.Contains("Lễ tân"));
            bool isNurse        = RoleId == 5 || RoleCode == "NURSE"        || (!string.IsNullOrEmpty(RoleName) && RoleName.Contains("Điều dưỡng"));
            bool isLabTech      = RoleId == 6 || RoleCode == "LAB_TECH"     || (!string.IsNullOrEmpty(RoleName) && RoleName.Contains("Kỹ thuật"));
            bool isPharmacist   = RoleId == 7 || RoleCode == "PHARMACIST"   || (!string.IsNullOrEmpty(RoleName) && RoleName.Contains("Dược sĩ"));

            if (isReceptionist)
            {
                if (string.IsNullOrEmpty(fullName) || fullName.Contains("Điều trị") || fullName.Contains("Bác sĩ") || fullName.Contains("000")) return "Nguyễn Thị Minh Châu";
                return fullName.Replace("BS.", "").Replace("BS", "").Replace("ThS.", "").Replace("TS.", "").Replace("LT.", "").Trim();
            }

            if (isNurse)
            {
                if (string.IsNullOrEmpty(fullName) || fullName.Contains("Điều trị") || fullName.Contains("000")) return "Phạm Thị Hồng Hạnh";
                return fullName.Replace("BS.", "").Replace("BS", "").Replace("ĐD.", "").Trim();
            }

            if (isLabTech)
            {
                if (string.IsNullOrEmpty(fullName) || fullName.Contains("000")) return "Trần Tuấn Kiệt";
                return fullName.Replace("KTV.", "").Replace("KTV", "").Trim();
            }

            if (isPharmacist)
            {
                if (string.IsNullOrEmpty(fullName) || fullName.Contains("000")) return "Trịnh Mai Phương";
                return fullName.Replace("Ds.", "").Replace("DS.", "").Replace("DS", "").Trim();
            }

            if (string.IsNullOrEmpty(fullName) || fullName == "BS. Điều trị" || fullName == "Điều trị" || fullName.Contains("000"))
            {
                return "BS. CKII Trịnh Hoàng Minh";
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
            RoleId = 2;
            RoleCode = "DOCTOR";
            RoleName = "Bác sĩ";
            FullName = string.Empty;
            Degree = string.Empty;
            ClinicRoom = string.Empty;
            SpecialtyId = 0;
            SpecialtyName = string.Empty;
            AvatarUrl = string.Empty;
        }
    }
}

