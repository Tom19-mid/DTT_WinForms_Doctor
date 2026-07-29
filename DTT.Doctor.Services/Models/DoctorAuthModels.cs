using System;

namespace DTT.Doctor.Services.Models
{
    public class LoginRequestDto
    {
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class DoctorAuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public int DoctorId { get; set; }
        public int RoleId { get; set; } = 2;
        public string RoleCode { get; set; } = "DOCTOR";
        public string RoleName { get; set; } = "Bác sĩ";
        public string FullName { get; set; } = string.Empty;
        public string Degree { get; set; } = string.Empty;
        public string ClinicRoom { get; set; } = string.Empty;
        public int SpecialtyId { get; set; }
        public string SpecialtyName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsSuccess => !string.IsNullOrEmpty(Token);
    }
}
