using System;
using DTT.Doctor.Services.Models;

namespace DTT.Doctor.Services.Core
{
    public static class TokenVault
    {
        public static string Token { get; private set; } = string.Empty;
        public static Guid UserId { get; private set; }
        public static int DoctorId { get; private set; }
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
            FullName = auth.FullName;
            Degree = auth.Degree;
            ClinicRoom = auth.ClinicRoom;
            SpecialtyId = auth.SpecialtyId;
            SpecialtyName = auth.SpecialtyName;
            AvatarUrl = auth.AvatarUrl ?? string.Empty;
        }

        public static void Clear()
        {
            Token = string.Empty;
            UserId = Guid.Empty;
            DoctorId = 0;
            FullName = string.Empty;
            Degree = string.Empty;
            ClinicRoom = string.Empty;
            SpecialtyId = 0;
            SpecialtyName = string.Empty;
            AvatarUrl = string.Empty;
        }
    }
}
