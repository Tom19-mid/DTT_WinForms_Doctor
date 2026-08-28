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
        /// <summary>Trạng thái thanh toán THẬT từ invoices.payment_status ("unpaid" | "partial" | "paid" — đúng theo DB constraint chk_payment_status) — dùng để hiển thị đúng trên màn Thanh Toán, không suy ra từ Status.</summary>
        public string PaymentStatus { get; set; } = "unpaid";
        public string? PaymentMethod { get; set; }
        public int QueueNumber { get; set; }
        public string ClinicRoom { get; set; } = string.Empty;
        public string Fee { get; set; } = "250.000đ";
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        /// <summary>JSON sinh hiệu do Điều dưỡng đo. Null nếu chưa đo.</summary>
        public string? NurseNote { get; set; }
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
        public string? IcdCode { get; set; }
        public string? IcdDescription { get; set; }
        public string TreatmentPlan { get; set; } = string.Empty;
        public System.Collections.Generic.List<PrescribedDrugItem> Prescriptions { get; set; } = new System.Collections.Generic.List<PrescribedDrugItem>();
    }

    public class Icd10Item
    {
        [Newtonsoft.Json.JsonProperty("icdCode")]
        public string IcdCode { get; set; } = string.Empty;
        [Newtonsoft.Json.JsonProperty("diseaseName")]
        public string DiseaseName { get; set; } = string.Empty;
        [Newtonsoft.Json.JsonProperty("chapterName")]
        public string ChapterName { get; set; } = string.Empty;
        [Newtonsoft.Json.JsonProperty("matchesSpecialty")]
        public bool MatchesSpecialty { get; set; }

        public override string ToString() => $"{IcdCode} - {DiseaseName}";
    }

    // Dịch vụ Xét nghiệm/Siêu âm từ danh mục clinical_services — dùng khi Bác sĩ chỉ định CLS
    public class ClinicalOrderServiceItem
    {
        [Newtonsoft.Json.JsonProperty("serviceId")]
        public int ServiceId { get; set; }
        [Newtonsoft.Json.JsonProperty("serviceName")]
        public string ServiceName { get; set; } = string.Empty;
        // Backend (ClinicalOrdersController.DisplayCategory) dịch clinical_services.service_type
        // ('Laboratory'/'Imaging'/'Functional') sang 'Test'/'Ultrasound' trên wire — categoryType
        // ở đây nhận đúng 2 giá trị đó, không phải service_type gốc.
        [Newtonsoft.Json.JsonProperty("categoryType")]
        public string CategoryType { get; set; } = string.Empty; // 'Test' | 'Ultrasound'
        [Newtonsoft.Json.JsonProperty("price")]
        public decimal Price { get; set; }

        public override string ToString() => $"{ServiceName} — {Price:N0}đ";
    }

    // 1 mục trong hàng đợi của Kỹ thuật viên CLS (Xét nghiệm hoặc Siêu âm đang chờ thực hiện)
    public class ClinicalOrderQueueItem
    {
        [Newtonsoft.Json.JsonProperty("kind")]
        public string Kind { get; set; } = string.Empty; // "Test" | "Ultrasound"
        [Newtonsoft.Json.JsonProperty("id")]
        public int Id { get; set; }
        [Newtonsoft.Json.JsonProperty("medicalRecordId")]
        public int MedicalRecordId { get; set; }
        [Newtonsoft.Json.JsonProperty("appointmentId")]
        public int AppointmentId { get; set; }
        [Newtonsoft.Json.JsonProperty("patientId")]
        public int PatientId { get; set; }
        [Newtonsoft.Json.JsonProperty("patientName")]
        public string PatientName { get; set; } = string.Empty;
        [Newtonsoft.Json.JsonProperty("patientAge")]
        public int PatientAge { get; set; }
        [Newtonsoft.Json.JsonProperty("patientGender")]
        public string PatientGender { get; set; } = string.Empty;
        [Newtonsoft.Json.JsonProperty("doctorName")]
        public string DoctorName { get; set; } = string.Empty;
        [Newtonsoft.Json.JsonProperty("serviceName")]
        public string ServiceName { get; set; } = string.Empty;
        [Newtonsoft.Json.JsonProperty("status")]
        public string Status { get; set; } = "Pending";
        [Newtonsoft.Json.JsonProperty("isUrgent")]
        public bool IsUrgent { get; set; }
        [Newtonsoft.Json.JsonProperty("clinicalNote")]
        public string? ClinicalNote { get; set; }
        [Newtonsoft.Json.JsonProperty("imageCount")]
        public int ImageCount { get; set; }
        [Newtonsoft.Json.JsonProperty("orderedAt")]
        public DateTime OrderedAt { get; set; }

        // Chỉ có giá trị khi Status != "Pending" — Bác sĩ xem lại kết quả trong phiếu khám
        [Newtonsoft.Json.JsonProperty("resultValue")]
        public string? ResultValue { get; set; }
        [Newtonsoft.Json.JsonProperty("unit")]
        public string? Unit { get; set; }
        [Newtonsoft.Json.JsonProperty("referenceRange")]
        public string? ReferenceRange { get; set; }
        [Newtonsoft.Json.JsonProperty("description")]
        public string? Description { get; set; }
        [Newtonsoft.Json.JsonProperty("conclusion")]
        public string? Conclusion { get; set; }
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
        // "patient" = hồ sơ chính; "family_member" = hồ sơ người thân (verify qua endpoint riêng)
        [Newtonsoft.Json.JsonProperty("recordType")]
        public string RecordType { get; set; } = "patient";
        [Newtonsoft.Json.JsonProperty("relationship")]
        public string Relationship { get; set; } = "Bản thân";
    }

    // ── Phân hệ Dược Sĩ (Pharmacy Models) ──────────────────────────────────

    public class PharmacyDrugItem
    {
        [Newtonsoft.Json.JsonProperty("prescriptionDetailId")]
        public int PrescriptionDetailId { get; set; }

        [Newtonsoft.Json.JsonProperty("medicineId")]
        public int MedicineId { get; set; }

        [Newtonsoft.Json.JsonProperty("medicineName")]
        public string MedicineName { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("unit")]
        public string Unit { get; set; } = "Viên";

        [Newtonsoft.Json.JsonProperty("quantity")]
        public int Quantity { get; set; }

        [Newtonsoft.Json.JsonProperty("dosage")]
        public string Dosage { get; set; } = "500mg";

        [Newtonsoft.Json.JsonProperty("frequency")]
        public string Frequency { get; set; } = "2 lần/ngày";

        [Newtonsoft.Json.JsonProperty("duration")]
        public string Duration { get; set; } = "7 ngày";

        [Newtonsoft.Json.JsonProperty("usageInstruction")]
        public string? UsageInstruction { get; set; }

        [Newtonsoft.Json.JsonProperty("stockQuantity")]
        public int StockQuantity { get; set; }

        [Newtonsoft.Json.JsonProperty("stockStatus")]
        public string StockStatus { get; set; } = "Khả dụng"; // "Khả dụng" | "Hết hàng"
    }

    public class PharmacyQueueItem
    {
        [Newtonsoft.Json.JsonProperty("appointmentId")]
        public int AppointmentId { get; set; }

        [Newtonsoft.Json.JsonProperty("medicalRecordId")]
        public int MedicalRecordId { get; set; }

        [Newtonsoft.Json.JsonProperty("prescriptionId")]
        public int PrescriptionId { get; set; }

        [Newtonsoft.Json.JsonProperty("patientId")]
        public int PatientId { get; set; }

        [Newtonsoft.Json.JsonProperty("patientName")]
        public string PatientName { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("patientAge")]
        public int PatientAge { get; set; } = 30;

        [Newtonsoft.Json.JsonProperty("patientGender")]
        public string PatientGender { get; set; } = "Nam";

        [Newtonsoft.Json.JsonProperty("doctorName")]
        public string DoctorName { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("doctorDegree")]
        public string DoctorDegree { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("diagnosis")]
        public string Diagnosis { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("symptoms")]
        public string Symptoms { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("prescriptionNote")]
        public string PrescriptionNote { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [Newtonsoft.Json.JsonProperty("timeSlot")]
        public string TimeSlot { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("items")]
        public System.Collections.Generic.List<PharmacyDrugItem> Items { get; set; } = new System.Collections.Generic.List<PharmacyDrugItem>();

        public int DrugCount => Items?.Count ?? 0;
    }

    public class PharmacyHistoryItem
    {
        [Newtonsoft.Json.JsonProperty("prescriptionId")]
        public int PrescriptionId { get; set; }

        [Newtonsoft.Json.JsonProperty("medicalRecordId")]
        public int MedicalRecordId { get; set; }

        [Newtonsoft.Json.JsonProperty("appointmentId")]
        public int AppointmentId { get; set; }

        [Newtonsoft.Json.JsonProperty("patientId")]
        public int PatientId { get; set; }

        [Newtonsoft.Json.JsonProperty("patientName")]
        public string PatientName { get; set; } = string.Empty;

        // [New code - Hỗ trợ tìm kiếm và hiển thị SĐT & Mã đơn]:
        [Newtonsoft.Json.JsonProperty("patientPhone")]
        public string PatientPhone { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("prescriptionCode")]
        public string PrescriptionCode { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("patientAge")]
        public int PatientAge { get; set; } = 30;

        [Newtonsoft.Json.JsonProperty("patientGender")]
        public string PatientGender { get; set; } = "Nam";

        [Newtonsoft.Json.JsonProperty("doctorName")]
        public string DoctorName { get; set; } = string.Empty;

        // [New code]: Thêm chức danh bác sĩ, ghi chú đơn thuốc và danh sách chi tiết các loại thuốc đã phát
        [Newtonsoft.Json.JsonProperty("doctorDegree")]
        public string DoctorDegree { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("prescriptionNote")]
        public string PrescriptionNote { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("diagnosis")]
        public string Diagnosis { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("symptoms")]
        public string Symptoms { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("dispensedAt")]
        public DateTime DispensedAt { get; set; }

        [Newtonsoft.Json.JsonProperty("dispensedByName")]
        public string DispensedByName { get; set; } = "Dược sĩ";

        [Newtonsoft.Json.JsonProperty("note")]
        public string Note { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("itemCount")]
        public int ItemCount { get; set; }

        [Newtonsoft.Json.JsonProperty("summary")]
        public string Summary { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("items")]
        public System.Collections.Generic.List<PharmacyDrugItem> Items { get; set; } = new System.Collections.Generic.List<PharmacyDrugItem>();
    }

    public class DispenseRequestModel
    {
        public Guid? PharmacistUserId { get; set; }
        public string? PharmacistName { get; set; }
        public string? PharmacistNote { get; set; }
    }

    // ── Các Model hỗ trợ đa phương thức thanh toán Lễ Tân (Tiền mặt, VNPAY, Chuyển khoản VietQR) ──
    public class VietQrResponse
    {
        [Newtonsoft.Json.JsonProperty("success")]
        public bool Success { get; set; }

        [Newtonsoft.Json.JsonProperty("appointmentId")]
        public int AppointmentId { get; set; }

        [Newtonsoft.Json.JsonProperty("patientName")]
        public string PatientName { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("examFee")]
        public decimal ExamFee { get; set; }

        [Newtonsoft.Json.JsonProperty("servicesFee")]
        public decimal ServicesFee { get; set; }

        [Newtonsoft.Json.JsonProperty("medsFee")]
        public decimal MedsFee { get; set; }

        [Newtonsoft.Json.JsonProperty("totalAmount")]
        public decimal TotalAmount { get; set; }

        [Newtonsoft.Json.JsonProperty("bankId")]
        public string BankId { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("bankName")]
        public string BankName { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("accountNo")]
        public string AccountNo { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("accountName")]
        public string AccountName { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("transferContent")]
        public string TransferContent { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("qrUrl")]
        public string QrUrl { get; set; } = string.Empty;
    }

    public class VnPayUrlResponse
    {
        [Newtonsoft.Json.JsonProperty("success")]
        public bool Success { get; set; }

        [Newtonsoft.Json.JsonProperty("paymentUrl")]
        public string PaymentUrl { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("totalAmount")]
        public decimal TotalAmount { get; set; }

        [Newtonsoft.Json.JsonProperty("appointmentId")]
        public int AppointmentId { get; set; }

        [Newtonsoft.Json.JsonProperty("patientName")]
        public string PatientName { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("txnRef")]
        public string TxnRef { get; set; } = string.Empty;
    }

    public class PaymentStatusResponse
    {
        [Newtonsoft.Json.JsonProperty("success")]
        public bool Success { get; set; }

        [Newtonsoft.Json.JsonProperty("appointmentId")]
        public int AppointmentId { get; set; }

        [Newtonsoft.Json.JsonProperty("isPaid")]
        public bool IsPaid { get; set; }

        [Newtonsoft.Json.JsonProperty("paymentStatus")]
        public string PaymentStatus { get; set; } = "unpaid";

        [Newtonsoft.Json.JsonProperty("paymentMethod")]
        public string PaymentMethod { get; set; } = "cash";

        [Newtonsoft.Json.JsonProperty("paidAmount")]
        public decimal PaidAmount { get; set; }

        [Newtonsoft.Json.JsonProperty("totalAmount")]
        public decimal TotalAmount { get; set; }

        [Newtonsoft.Json.JsonProperty("invoiceId")]
        public int InvoiceId { get; set; }
    }

    public class PaypalInfoResponse
    {
        [Newtonsoft.Json.JsonProperty("success")]
        public bool Success { get; set; }

        [Newtonsoft.Json.JsonProperty("appointmentId")]
        public int AppointmentId { get; set; }

        [Newtonsoft.Json.JsonProperty("patientName")]
        public string PatientName { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("examFee")]
        public decimal ExamFee { get; set; }

        [Newtonsoft.Json.JsonProperty("servicesFee")]
        public decimal ServicesFee { get; set; }

        [Newtonsoft.Json.JsonProperty("medsFee")]
        public decimal MedsFee { get; set; }

        [Newtonsoft.Json.JsonProperty("totalVnd")]
        public decimal TotalVnd { get; set; }

        [Newtonsoft.Json.JsonProperty("totalUsd")]
        public decimal TotalUsd { get; set; }

        [Newtonsoft.Json.JsonProperty("checkoutUrl")]
        public string CheckoutUrl { get; set; } = string.Empty;

        [Newtonsoft.Json.JsonProperty("qrUrl")]
        public string QrUrl { get; set; } = string.Empty;
    }

    public class InvoiceEstimateResponse
    {
        [Newtonsoft.Json.JsonProperty("success")]
        public bool Success { get; set; }

        [Newtonsoft.Json.JsonProperty("appointmentId")]
        public int AppointmentId { get; set; }

        [Newtonsoft.Json.JsonProperty("isPackage")]
        public bool IsPackage { get; set; }

        [Newtonsoft.Json.JsonProperty("examFee")]
        public decimal ExamFee { get; set; }

        [Newtonsoft.Json.JsonProperty("servicesFee")]
        public decimal ServicesFee { get; set; }

        [Newtonsoft.Json.JsonProperty("medsFee")]
        public decimal MedsFee { get; set; }

        [Newtonsoft.Json.JsonProperty("totalAmount")]
        public decimal TotalAmount { get; set; }

        [Newtonsoft.Json.JsonProperty("hasPrescription")]
        public bool HasPrescription { get; set; }
    }
}
