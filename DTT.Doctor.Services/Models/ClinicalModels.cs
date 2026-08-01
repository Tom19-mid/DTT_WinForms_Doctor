using System;

namespace DTT.Doctor.Services.Models
{
    public class AppointmentModel
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientGender { get; set; } = "Nam";
        public int PatientAge { get; set; } = 35;
        public string DoctorName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string TimeSlot { get; set; } = string.Empty;
        public string Status { get; set; } = "Confirmed"; // Confirmed -> Đang chờ, InProgress -> Đang khám, Completed -> Đã xong
        public int QueueNumber { get; set; }
        public string ClinicRoom { get; set; } = string.Empty;
        public string Fee { get; set; } = "250.000đ";
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ClinicalServiceModel
    {
        public int ServiceId { get; set; }
        public string ServiceCode { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
    }

    public class Icd10CatalogModel
    {
        public string IcdCode { get; set; } = string.Empty;
        public string DiseaseName { get; set; } = string.Empty;
        public string ChapterName { get; set; } = string.Empty;
        public bool IsCommon { get; set; }
    }

    public class MedicineModel
    {
        public int MedicineId { get; set; }
        public int CategoryId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int StockQuantity { get; set; }
        public string DefaultUsage { get; set; } = string.Empty;
    }

    public class PrescribedDrugItem
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string Unit { get; set; } = "Viên";
        public int Quantity { get; set; } = 10;
        public string Dosage { get; set; } = "500mg";
        public string Frequency { get; set; } = "2 lần/ngày";
        public string UsageInstruction { get; set; } = "Uống sau ăn 30 phút";
    }

    public class SaveClinicalRecordRequest
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string Pulse { get; set; } = string.Empty;
        public string BloodPressure { get; set; } = string.Empty;
        public string Temperature { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;
        public string Height { get; set; } = string.Empty;
        public string Symptoms { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string TreatmentPlan { get; set; } = string.Empty;
        public System.Collections.Generic.List<PrescribedDrugItem> Prescriptions { get; set; } = new System.Collections.Generic.List<PrescribedDrugItem>();
    }

    public class PatientSimpleModel
    {
        [Newtonsoft.Json.JsonProperty("id")]
        public int Id { get; set; }
        [Newtonsoft.Json.JsonProperty("fullName")]
        public string FullName { get; set; } = string.Empty;
        [Newtonsoft.Json.JsonProperty("phone")]
        public string Phone { get; set; } = string.Empty;
        [Newtonsoft.Json.JsonProperty("cccd")]
        public string Cccd { get; set; } = string.Empty;
        [Newtonsoft.Json.JsonProperty("bhyt")]
        public string Bhyt { get; set; } = string.Empty;
        [Newtonsoft.Json.JsonProperty("verificationStatus")]
        public string VerificationStatus { get; set; } = "pending";
        [Newtonsoft.Json.JsonProperty("gender")]
        public string Gender { get; set; } = string.Empty;
        [Newtonsoft.Json.JsonProperty("dob")]
        public string Dob { get; set; } = string.Empty;
    }
}
