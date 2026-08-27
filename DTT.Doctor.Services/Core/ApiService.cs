using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DTT.Doctor.Services.Models;
using Newtonsoft.Json;

namespace DTT.Doctor.Services.Core
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        public string BaseUrl { get; set; } = "http://localhost:5000";

        public ApiService(string baseUrl = "http://localhost:5000")
        {
            BaseUrl = baseUrl;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(15)
            };
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private void AttachBearerToken()
        {
            if (TokenVault.IsAuthenticated)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenVault.Token);
            }
        }

        public async Task<DoctorAuthResponseDto> LoginDoctorAsync(string phone, string password)
        {
            try
            {
                var payload = new LoginRequestDto { Phone = phone, Password = password };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Auth/doctor-login", content);
                var resContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<DoctorAuthResponseDto>(resContent);
                    if (result != null && !string.IsNullOrEmpty(result.Token))
                    {
                        TokenVault.StoreSession(result);
                        return result;
                    }
                }

                // Try fallback message extraction
                try
                {
                    dynamic err = JsonConvert.DeserializeObject(resContent);
                    return new DoctorAuthResponseDto { Message = err?.message != null ? (string)err.message : "Đăng nhập thất bại." };
                }
                catch
                {
                    return new DoctorAuthResponseDto { Message = $"Lỗi kết nối ({response.StatusCode}): {resContent}" };
                }
            }
            catch (Exception ex)
            {
                // Complete Fallback matching ALL 10 real doctors in PostgreSQL database (doctors.csv / users.csv)
                var dbDoctors = new Dictionary<string, (int id, string name, string degree, string room, int specId, string specName, string email, int roleId, string roleCode, string roleName)>
                {
                    ["0900000004"] = (1, "Nguyễn Thị Minh Châu", "Cử nhân Quản trị Y tế", "Bàn Tiếp Đón & Thu Ngân #01", 1, "Tiếp Đón & Thu Ngân", "letan.minhchau@gmail.com", 4, "RECEPTIONIST", "Lễ tân tiếp đón"),
                    ["0900000005"] = (2, "Phạm Thị Hồng Hạnh", "Cử nhân Điều dưỡng Chính", "Trạm Đo Sinh Hiệu #02", 1, "Trạm Sinh Hiệu", "dieuduong.honghanh@gmail.com", 5, "NURSE", "Điều dưỡng"),
                    ["0900000006"] = (3, "KTV. Trần Tuấn Kiệt", "Cử nhân Chẩn đoán Hình ảnh", "Phòng Siêu âm / Xét nghiệm", 8, "Cận Lâm Sàng", "ktv.tuankiet@gmail.com", 6, "LAB_TECH", "Kỹ thuật viên CLS"),
                    ["0900000007"] = (4, "Ds. Trịnh Mai Phương", "Dược sĩ Đại học", "Nhà Thuốc Bệnh Viện #01", 1, "Nhà Thuốc Bệnh Viện", "duocsi.maiphuong@gmail.com", 7, "PHARMACIST", "Dược sĩ"),
                    ["0901111111"] = (5, "BS. CKII Nguyễn Văn A", "Chuyên khoa II Nội tổng quát", "Phòng 101", 1, "Nội tổng quát", "doctor1@gmail.com", 2, "DOCTOR", "Bác sĩ"),
                    ["0902222222"] = (3, "ThS. BS Trần Văn C", "Thạc sĩ Chuyên môn Tim mạch", "Phòng 201", 5, "Tim mạch", "doctor2@gmail.com", 2, "DOCTOR", "Bác sĩ"),
                    ["0903333333"] = (4, "BS. CKI Lê Hoàng Văn", "Bác sĩ Chuyên khoa Nhi", "Phòng 102", 2, "Nhi", "doctor3@gmail.com", 2, "DOCTOR", "Bác sĩ"),
                    ["0904444444"] = (6, "BS. CKI Phạm Thị D", "Bác sĩ Chuyên khoa Da liễu", "Phòng 202", 7, "Da liễu", "doctor4@gmail.com", 2, "DOCTOR", "Bác sĩ"),
                    ["0905555555"] = (8, "TS. BS Đỗ Phương Hạnh", "Tiến sĩ Chuyên môn Phụ & Sản khoa", "Phòng 301", 3, "Phụ & Sản khoa", "doctor5@gmail.com", 2, "DOCTOR", "Bác sĩ"),
                    ["0906666666"] = (9, "BS. CKII Phạm Tuấn Kiệt", "Chuyên khoa II Cơ xương khớp", "Phòng 302", 4, "Cơ xương khớp", "doctor6@gmail.com", 2, "DOCTOR", "Bác sĩ"),
                    ["0907777777"] = (10, "ThS. BS Vũ Bích Ngọc", "Thạc sĩ Bác sĩ Thần kinh", "Phòng 401", 6, "Thần kinh", "doctor7@gmail.com", 2, "DOCTOR", "Bác sĩ"),
                    ["0908888888"] = (11, "BS. CKI Hoàng Văn Long", "Chuyên khoa Chẩn đoán hình ảnh", "Phòng 402", 8, "Chẩn đoán hình ảnh", "doctor8@gmail.com", 2, "DOCTOR", "Bác sĩ"),
                    ["0909999999"] = (12, "BS. CKII Trịnh Hoàng Minh", "Bác sĩ Cố vấn Nội tổng quát", "Phòng 103", 1, "Nội tổng quát", "doctor9@gmail.com", 2, "DOCTOR", "Bác sĩ"),
                    ["0910000000"] = (13, "ThS. BS Nguyễn Mai Chi", "Thạc sĩ Chuyên khoa Nhi", "Phòng 104", 2, "Nhi", "doctor10@gmail.com", 2, "DOCTOR", "Bác sĩ")
                };

                // Chỉ cho phép fallback demo ngoại tuyến (khi không gọi được API) nếu mật khẩu khớp
                // đúng mật khẩu demo cố định — trước đây bất kỳ mật khẩu nào cũng được chấp nhận cho
                // ~14 số điện thoại liệt kê ở trên, tức là chỉ cần gây mất kết nối API (hoặc API tạm
                // down) là đăng nhập được vào bất kỳ tài khoản nào trong danh sách mà không cần biết
                // mật khẩu thật.
                const string offlineDemoPassword = "Demo@2026";
                if (password == offlineDemoPassword && (dbDoctors.TryGetValue(phone, out var doc) || phone == "admin" || phone == "demo"))
                {
                    if (phone == "admin" || phone == "demo") doc = dbDoctors["0901111111"];
                    var demo = new DoctorAuthResponseDto
                    {
                        Token = "demo_jwt_token_doctor_2026",
                        DoctorId = doc.id,
                        RoleId = doc.roleId,
                        RoleCode = doc.roleCode,
                        RoleName = doc.roleName,
                        FullName = doc.name,
                        Degree = doc.degree,
                        ClinicRoom = doc.room,
                        SpecialtyId = doc.specId,
                        SpecialtyName = doc.specName,
                        Phone = phone,
                        Email = doc.email
                    };
                    TokenVault.StoreSession(demo);
                    return demo;
                }

                return new DoctorAuthResponseDto { Message = $"Không thể kết nối Server API ({BaseUrl}). Lỗi: " + ex.Message };
            }
        }

        public async Task<List<AppointmentModel>> GetQueueAppointmentsAsync()
        {
            AttachBearerToken();
            try
            {
                // Nếu là Bác sĩ thì truyền doctorId để chỉ lấy ca khám của bác sĩ đó.
                // Nếu là Lễ Tân thì gọi /api/Appointments (không có doctorId) để xem toàn bộ bệnh nhân của tất cả Bác sĩ.
                string url = "/api/Appointments?todayOnly=true";
                bool isStaffViewAll = TokenVault.RoleId == 4 || TokenVault.RoleId == 5 || TokenVault.RoleId == 6 || TokenVault.RoleId == 7 ||
                                     TokenVault.RoleCode == "RECEPTIONIST" || TokenVault.RoleCode == "NURSE" || TokenVault.RoleCode == "LAB_TECH" || TokenVault.RoleCode == "PHARMACIST" ||
                                     (!string.IsNullOrEmpty(TokenVault.RoleName) && (TokenVault.RoleName.Contains("Lễ tân") || TokenVault.RoleName.Contains("Điều dưỡng") || TokenVault.RoleName.Contains("Kỹ thuật") || TokenVault.RoleName.Contains("Dược sĩ")));
                if (TokenVault.DoctorId > 0 && !isStaffViewAll)
                {
                    url = $"/api/Appointments?doctorId={TokenVault.DoctorId}&todayOnly=true";
                }

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<AppointmentModel>>(json);
                    if (list != null)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (string.IsNullOrEmpty(list[i].PatientName))
                            {
                                list[i].PatientName = $"Bệnh nhân #{list[i].PatientId}";
                            }
                            if (string.IsNullOrEmpty(list[i].PatientGender))
                            {
                                list[i].PatientGender = "Nam";
                            }
                            if (list[i].PatientAge < 0)
                            {
                                list[i].PatientAge = 0;
                            }
                        }
                        // Lễ Tân xem toàn bộ ca khám; Bác sĩ nếu chưa có DoctorId thì mới lọc theo specialty
                        // if (TokenVault.DoctorId <= 0 && TokenVault.RoleCode != "RECEPTIONIST" && !string.IsNullOrEmpty(TokenVault.SpecialtyName))
                        // Nhân viên toàn viện (Lễ tân, Điều dưỡng, Kỹ thuật viên, Dược sĩ) xem toàn bộ ca khám;
                        // Bác sĩ nếu chưa có DoctorId thì mới lọc theo chuyên khoa của mình
                        if (TokenVault.DoctorId <= 0 && !isStaffViewAll && !string.IsNullOrEmpty(TokenVault.SpecialtyName))
                        {
                            list = list.Where(a => !string.IsNullOrEmpty(a.SpecialtyName) &&
                                                   a.SpecialtyName.Trim().Equals(TokenVault.SpecialtyName.Trim(), StringComparison.OrdinalIgnoreCase))
                                       .ToList();
                        }

                        // Re-index STT sequentially: 1, 2, 3...
                        for (int i = 0; i < list.Count; i++)
                        {
                            list[i].QueueNumber = i + 1;
                        }

                        return list;
                    }
                }

                // Server PHẢN HỒI THẬT nhưng không thành công (vd: 401 do phiên đăng nhập hết hạn) —
                // KHÔNG hiện danh sách bệnh nhân demo giả ("David Johns", "Test"...) vì trông giống hàng
                // chờ thật, dễ khiến Lễ Tân/Bác sĩ tưởng nhầm không có ca khám nào thay vì nhận ra lỗi
                // đăng nhập. Trả về rỗng để giao diện hiện "không có dữ liệu" thay vì dữ liệu giả.
                return new List<AppointmentModel>();
            }
            catch
            {
                // Không kết nối được server (mất mạng/server tắt hẳn) — giữ hành vi demo ngoại tuyến cũ
                // để vẫn xem được giao diện khi demo đồ án không có server chạy.
            }

            return GetDemoQueueList();
        }

        // Lễ Tân xác nhận Check-in: chuyển appointment từ Confirmed → CheckedIn
        // Bệnh nhân sau đó mới xuất hiện trong Hàng chờ lâm sàng của Bác sĩ
        public async Task<bool> CheckInAppointmentAsync(int appointmentId)
        {
            AttachBearerToken();
            try
            {
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                var res = await _httpClient.PostAsync($"/api/Appointments/{appointmentId}/checkin", content);
                return res.IsSuccessStatusCode;
            }
            catch { }
            return false;
        }

        // Lấy phí khám + phí thuốc THẬT (tính từ đơn thuốc điện tử) để hiển thị đúng trên màn Thanh Toán trước khi thu tiền
        public async Task<(decimal ExamFee, decimal ServicesFee, decimal MedsFee, decimal Total, bool IsPackage)> GetInvoiceEstimateAsync(int appointmentId)
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync($"/api/Invoices/estimate/{appointmentId}");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    if (obj != null && (bool)(obj.success ?? false))
                    {
                        decimal exam = (decimal)(obj.examFee ?? 250000m);
                        decimal svc = (decimal)(obj.servicesFee ?? 0m);
                        decimal meds = (decimal)(obj.medsFee ?? 0m);
                        bool isPkg = (bool)(obj.isPackage ?? false);
                        return (exam, svc, meds, exam + svc + meds, isPkg);
                    }
                }
            }
            catch { }
            return (250000m, 0m, 0m, 250000m, false);
        }

        // Lễ Tân xác nhận thu tiền → Tạo Invoice trong DB + gửi thông báo App Mobile
        public async Task<(bool Success, int InvoiceId, decimal Total)> ConfirmPaymentAsync(int appointmentId, int patientId, decimal examFee = 250000m, decimal servicesFee = 0m, decimal medsFee = 0m, string method = "cash")
        {
            AttachBearerToken();
            try
            {
                var payload = new { AppointmentId = appointmentId, PatientId = patientId, ExamFee = examFee, ServicesFee = servicesFee, MedsFee = medsFee, PaymentMethod = method };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var res = await _httpClient.PostAsync("/api/Invoices/confirm-payment", content);
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    int invoiceId = (int)(obj?.invoiceId ?? 0);
                    decimal total = (decimal)(obj?.totalAmount ?? examFee);
                    return (true, invoiceId, total);
                }
            }
            catch { }
            return (false, 0, 0);
        }

        // Lễ Tân tạo hồ sơ bệnh nhân vãng lai → trả về mật khẩu tạm thời giả lập gửi SMS
        public async Task<(bool Success, string TempPassword, int PatientId, int AppointmentId)> RegisterWalkInAsync(
            string fullName, string phone, string cccd, string? dob, string? gender, string? bhyt, string? address, int doctorId, string? specialtyName)
        {
            AttachBearerToken();
            try
            {
                var payload = new { FullName = fullName, Phone = phone, CccdNumber = cccd, DateOfBirth = dob, Gender = gender ?? "Nam", BhytNumber = bhyt, Address = address, DoctorId = doctorId, SpecialtyName = specialtyName };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var res = await _httpClient.PostAsync("/api/Invoices/register-walkin", content);
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    string pwd = (string)(obj?.tempPassword ?? "DTT@0000");
                    int pid = (int)(obj?.patientId ?? 0);
                    int aid = (int)(obj?.appointmentId ?? 0);
                    return (true, pwd, pid, aid);
                }
            }
            catch { }
            return (false, "", 0, 0);
        }

        // Lấy danh sách chuyên khoa kèm tên bác sĩ từ DB (cho form đăng ký vãng lai)
        public async Task<List<(string DisplayName, int DoctorId, string SpecialtyName)>> GetSpecialtiesWithDoctorsAsync()
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync("/api/Specialties/with-doctors");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    var result = new List<(string, int, string)>();
                    if (obj?.specialties != null)
                    {
                        foreach (var s in obj.specialties)
                        {
                            string display = (string)(s.displayName ?? s.specialtyName ?? "");
                            int docId = (int)(s.doctorId ?? 0);
                            string specName = (string)(s.specialtyName ?? "");
                            if (!string.IsNullOrEmpty(display))
                                result.Add((display, docId, specName));
                        }
                    }
                    return result;
                }
            }
            catch { }
            // Fallback hardcoded nếu API lỗi
            return new List<(string, int, string)>
            {
                ("Nội tổng quát (BS. CKII Nguyễn Văn A)", 1, "Nội tổng quát"),
                ("Tim mạch (ThS. BS Trần Văn C)", 2, "Tim mạch"),
                ("Cơ xương khớp (BS. CKII Phạm Tuấn Kiệt)", 3, "Cơ xương khớp"),
                ("Nhi khoa (BS. CKI Lê Hoàng Văn)", 4, "Nhi khoa"),
            };
        }

        // ── Điều Dưỡng: Lưu sinh hiệu & chuyển trạng thái → WaitingForDoctor (8) ──
        public async Task<(bool Success, double Bmi)> SaveNurseVitalsAsync(
            int appointmentId,
            string bloodPressure,
            int heartRate,
            double temperature,
            double weight,
            double height,
            string nurseNote = "")
        {
            AttachBearerToken();
            try
            {
                var payload = new
                {
                    BloodPressure = bloodPressure,
                    HeartRate     = heartRate,
                    Temperature   = temperature,
                    Weight        = weight,
                    Height        = height,
                    NurseNote     = nurseNote
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var res = await _httpClient.PutAsync($"/api/Appointments/{appointmentId}/nurse-vitals", content);
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    double bmi = (double)(obj?.bmi ?? 0);
                    return (true, bmi);
                }
            }
            catch { }
            return (false, 0);
        }

        public async Task<bool> UpdateAppointmentStatusAsync(int appointmentId, string status)
        {
            AttachBearerToken();
            try
            {
                var payload = new { Status = status };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var res = await _httpClient.PutAsync($"/api/Appointments/{appointmentId}/status", content);
                if (!res.IsSuccessStatusCode && status == "Cancelled")
                {
                    await _httpClient.PutAsync($"/api/Appointments/{appointmentId}/cancel", content);
                }
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Lễ Tân hủy lịch hẹn tại quầy — kèm lý do (khác luồng bệnh nhân tự hủy trên App, không cần lý do)
        public async Task<bool> CancelAppointmentWithReasonAsync(int appointmentId, string reason)
        {
            AttachBearerToken();
            try
            {
                var payload = new
                {
                    CancelReason = reason,
                    CancelledBy = !string.IsNullOrEmpty(TokenVault.FullName) ? TokenVault.FullName : "Lễ tân tiếp đón"
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var res = await _httpClient.PutAsync($"/api/Appointments/{appointmentId}/cancel", content);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool Success, List<string> InsufficientStock)> SaveClinicalRecordAsync(SaveClinicalRecordRequest req)
        {
            AttachBearerToken();
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
                var res = await _httpClient.PostAsync("/api/MedicalRecords", content);
                if (!res.IsSuccessStatusCode) return (false, new List<string>());

                var json = await res.Content.ReadAsStringAsync();
                dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                var insufficient = new List<string>();
                if (obj?.insufficientStock != null)
                {
                    var list = JsonConvert.DeserializeObject<List<string>>(obj.insufficientStock.ToString());
                    if (list != null) insufficient = list;
                }
                return (true, insufficient);
            }
            catch
            {
                return (false, new List<string>());
            }
        }

        public static List<AppointmentModel> GetDemoQueueList()
        {
            string doctorSpecialty = !string.IsNullOrEmpty(TokenVault.SpecialtyName) ? TokenVault.SpecialtyName : "Khám tổng quát";
            string doctorRoom = !string.IsNullOrEmpty(TokenVault.ClinicRoom) ? TokenVault.ClinicRoom : "Phòng 101";

            return new List<AppointmentModel>
            {
                new AppointmentModel { AppointmentId = 101, QueueNumber = 1, PatientId = 2, PatientName = "David Johns", PatientAge = 0, PatientGender = "", SpecialtyName = doctorSpecialty, TimeSlot = "08:30", Status = "Confirmed", ClinicRoom = doctorRoom, Fee = "250.000đ" },
                new AppointmentModel { AppointmentId = 102, QueueNumber = 2, PatientId = 3, PatientName = "Pete Hawks", PatientAge = 0, PatientGender = "", SpecialtyName = doctorSpecialty, TimeSlot = "09:00", Status = "InProgress", ClinicRoom = doctorRoom, Fee = "300.000đ" },
                new AppointmentModel { AppointmentId = 103, QueueNumber = 3, PatientId = 4, PatientName = "Dawn", PatientAge = 0, PatientGender = "", SpecialtyName = doctorSpecialty, TimeSlot = "09:30", Status = "Confirmed", ClinicRoom = doctorRoom, Fee = "400.000đ" },
                new AppointmentModel { AppointmentId = 104, QueueNumber = 4, PatientId = 5, PatientName = "Hong", PatientAge = 0, PatientGender = "", SpecialtyName = doctorSpecialty, TimeSlot = "10:00", Status = "Completed", ClinicRoom = doctorRoom, Fee = "200.000đ" },
                new AppointmentModel { AppointmentId = 105, QueueNumber = 5, PatientId = 6, PatientName = "Minh Dang", PatientAge = 0, PatientGender = "", SpecialtyName = doctorSpecialty, TimeSlot = "10:30", Status = "Confirmed", ClinicRoom = doctorRoom, Fee = "250.000đ" },
                new AppointmentModel { AppointmentId = 106, QueueNumber = 6, PatientId = 7, PatientName = "DingDong", PatientAge = 0, PatientGender = "", SpecialtyName = doctorSpecialty, TimeSlot = "11:00", Status = "Completed", ClinicRoom = doctorRoom, Fee = "350.000đ" },
                new AppointmentModel { AppointmentId = 107, QueueNumber = 7, PatientId = 8, PatientName = "Test", PatientAge = 0, PatientGender = "", SpecialtyName = doctorSpecialty, TimeSlot = "13:30", Status = "Confirmed", ClinicRoom = doctorRoom, Fee = "250.000đ" },
                new AppointmentModel { AppointmentId = 108, QueueNumber = 8, PatientId = 9, PatientName = "tester", PatientAge = 0, PatientGender = "", SpecialtyName = doctorSpecialty, TimeSlot = "14:00", Status = "Confirmed", ClinicRoom = doctorRoom, Fee = "300.000đ" }
            };
        }

        public async Task<List<MedicineModel>> GetMedicinesAsync()
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync("/api/Medicines");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<MedicineModel>>(json);
                    if (list != null && list.Count > 0) return list;
                }
            }
            catch { }

            // Match exact CSDL PostgreSQL (medicines table: 3 items)
            // StockQuantity = -1 nghĩa là "không rõ tồn kho" (danh sách dự phòng khi API lỗi/rỗng) —
            // ExaminationForm dựa vào StockQuantity == 0 để chặn kê thuốc hết hàng, không được để mặc
            // định 0 của các mục dự phòng này bị hiểu nhầm thành "hết hàng thật".
            return new List<MedicineModel>
            {
                new MedicineModel { MedicineId = 1, MedicineName = "Amoxicillin 500mg", Unit = "Viên", StockQuantity = -1, DefaultUsage = "Uống 1 viên sau ăn 30 phút" },
                new MedicineModel { MedicineId = 2, MedicineName = "Paracetamol 500mg", Unit = "Viên", StockQuantity = -1, DefaultUsage = "Uống 1 viên khi sốt > 38.5°C" },
                new MedicineModel { MedicineId = 3, MedicineName = "Vitamin C 1000mg", Unit = "Hộp", StockQuantity = -1, DefaultUsage = "Pha 1 viên với 200ml nước ấm" }
            };
        }

        // Lấy danh mục ICD-10 — mã thuộc đúng chuyên khoa của bác sĩ đang đăng nhập được xếp lên đầu
        public async Task<List<Icd10Item>> GetIcd10CatalogAsync(int specialtyId, string search = "")
        {
            AttachBearerToken();
            try
            {
                string url = $"/api/MedicalRecords/icd10?specialtyId={specialtyId}";
                if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";

                var res = await _httpClient.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    if (obj != null && (bool)(obj.success ?? false) && obj.items != null)
                    {
                        var list = JsonConvert.DeserializeObject<List<Icd10Item>>(obj.items.ToString());
                        if (list != null) return list;
                    }
                }
            }
            catch { }
            return new List<Icd10Item>();
        }

        // Đặt lịch hẹn mới qua API — dùng cho chức năng "Chuyển / Tái khám" của Bác sĩ (trước đây chỉ
        // hiện toast "đang phát triển", chưa thực sự tạo lịch hẹn nào).
        public async Task<(bool Success, string Message)> CreateAppointmentAsync(int patientId, int doctorId, string doctorName, string specialtyName, string date, string timeSlot, string reason)
        {
            AttachBearerToken();
            try
            {
                var payload = new
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    DoctorName = doctorName,
                    SpecialtyName = specialtyName,
                    Date = date,
                    TimeSlot = timeSlot,
                    Reason = reason
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var res = await _httpClient.PostAsync("/api/Appointments", content);
                var json = await res.Content.ReadAsStringAsync();
                if (res.IsSuccessStatusCode) return (true, "Đặt lịch tái khám thành công.");

                try
                {
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    string msg = obj?.message != null ? (string)obj.message : "Không thể đặt lịch tái khám. Vui lòng thử lại.";
                    return (false, msg);
                }
                catch
                {
                    return (false, "Không thể đặt lịch tái khám. Vui lòng thử lại.");
                }
            }
            catch (Exception ex)
            {
                return (false, "Lỗi kết nối: " + ex.Message);
            }
        }

        // Danh sách thông báo của CHÍNH tài khoản đang đăng nhập — backend tự lọc theo claim JWT khi
        // không truyền userId (xem NotificationsController.GetAllNotifications). Dùng cho toast khi
        // NotificationHubService báo "NotificationsChanged" (Admin vừa phát thông báo tới Bác sĩ).
        public async Task<List<dynamic>> GetMyNotificationsAsync()
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync("/api/Notifications");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<dynamic>>(json);
                    if (list != null) return list;
                }
            }
            catch { }
            return new List<dynamic>();
        }

        public async Task<dynamic> GetDoctorSchedulesAsync(int doctorId, string dateStr)
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync($"/api/Doctors/schedules?doctorId={doctorId}&dateStr={dateStr}");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<dynamic>(json);
                }
            }
            catch { }
            return null;
        }

        public async Task<string> GetRawAsync(string endpoint)
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync(endpoint);
                if (res.IsSuccessStatusCode)
                {
                    return await res.Content.ReadAsStringAsync();
                }
            }
            catch { }
            return string.Empty;
        }

        // Lấy toàn bộ danh sách bệnh nhân từ DB (dùng cho Lễ Tân)
        public async Task<List<PatientSimpleModel>> GetPatientsAsync()
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync("/api/Patients");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    var wrapper = JsonConvert.DeserializeObject<dynamic>(json);
                    if (wrapper != null && wrapper.patients != null)
                    {
                        var list = JsonConvert.DeserializeObject<List<PatientSimpleModel>>(wrapper.patients.ToString());
                        if (list != null && list.Count > 0) return list;
                    }
                }
            }
            catch { }

            // Fallback: dữ liệu thực từ DB (khớp với patients.csv)
            return new List<PatientSimpleModel>
            {
                new PatientSimpleModel { Id = 2, FullName = "David Johns", Phone = "0934123456", Cccd = "", Bhyt = "", VerificationStatus = "verified" },
                new PatientSimpleModel { Id = 3, FullName = "Pete Hawks", Phone = "0909123456", Cccd = "", Bhyt = "", VerificationStatus = "verified" },
                new PatientSimpleModel { Id = 4, FullName = "Dawn", Phone = "0938110220", Cccd = "", Bhyt = "", VerificationStatus = "pending" },
                new PatientSimpleModel { Id = 5, FullName = "Hong", Phone = "0912345557", Cccd = "", Bhyt = "", VerificationStatus = "verified" },
                new PatientSimpleModel { Id = 6, FullName = "Minh Dang", Phone = "0938000123", Cccd = "", Bhyt = "", VerificationStatus = "verified" },
                new PatientSimpleModel { Id = 7, FullName = "DingDong", Phone = "0900123000", Cccd = "", Bhyt = "", VerificationStatus = "verified" },
                new PatientSimpleModel { Id = 8, FullName = "Test", Phone = "0932800100", Cccd = "", Bhyt = "", VerificationStatus = "verified" },
                new PatientSimpleModel { Id = 9, FullName = "tester", Phone = "0431234551", Cccd = "", Bhyt = "", VerificationStatus = "verified" }
            };
        }

        // Xác thực CCCD bệnh nhân bởi Lễ Tân (PATCH /api/Patients/{id}/verify)
        public async Task<bool> VerifyPatientCccdAsync(int patientId, string cccd)
        {
            AttachBearerToken();
            try
            {
                var payload = JsonConvert.SerializeObject(new { cccdNumber = cccd });
                var content = new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                var res = await _httpClient.PatchAsync($"/api/Patients/{patientId}/verify", content);
                return res.IsSuccessStatusCode;
            }
            catch { }
            return false;
        }

        // ── Cận Lâm Sàng (Xét nghiệm / Siêu âm): Bác sĩ chỉ định + Kỹ thuật viên xử lý ──

        // Danh mục dịch vụ Xét nghiệm/Siêu âm để Bác sĩ chọn khi chỉ định
        public async Task<List<ClinicalOrderServiceItem>> GetClinicalServicesAsync()
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync("/api/ClinicalOrders/services");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    if (obj != null && (bool)(obj.success ?? false) && obj.items != null)
                    {
                        var list = JsonConvert.DeserializeObject<List<ClinicalOrderServiceItem>>(obj.items.ToString());
                        if (list != null) return list;
                    }
                }
            }
            catch { }
            return new List<ClinicalOrderServiceItem>();
        }

        // Bác sĩ chỉ định 1..N dịch vụ Xét nghiệm/Siêu âm cho ca khám đang diễn ra
        public async Task<(bool Success, string Message)> CreateClinicalOrdersAsync(int appointmentId, int patientId, int doctorId, List<int> serviceIds, bool isUrgent = false, string clinicalNote = "")
        {
            AttachBearerToken();
            try
            {
                var payload = new
                {
                    AppointmentId = appointmentId,
                    PatientId = patientId,
                    DoctorId = doctorId,
                    OrderedByUserId = TokenVault.UserId,
                    ServiceIds = serviceIds,
                    IsUrgent = isUrgent,
                    ClinicalNote = clinicalNote
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var res = await _httpClient.PostAsync("/api/ClinicalOrders", content);
                var json = await res.Content.ReadAsStringAsync();
                if (res.IsSuccessStatusCode) return (true, "");

                try
                {
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    string msg = (string)(obj?.message ?? "Không thể gửi chỉ định lên hệ thống.");
                    return (false, msg);
                }
                catch { return (false, "Không thể gửi chỉ định lên hệ thống."); }
            }
            catch (Exception ex)
            {
                return (false, "Lỗi kết nối: " + ex.Message);
            }
        }

        // Hủy 1 chỉ định Xét nghiệm/Siêu âm đã tạo nhầm (chỉ khi còn 'Pending')
        public async Task<bool> CancelClinicalOrderAsync(string kind, int id)
        {
            AttachBearerToken();
            try
            {
                string path = kind == "Test" ? $"/api/ClinicalOrders/tests/{id}/cancel" : $"/api/ClinicalOrders/ultrasound/{id}/cancel";
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                var res = await _httpClient.PutAsync(path, content);
                return res.IsSuccessStatusCode;
            }
            catch { }
            return false;
        }

        // Toàn bộ chỉ định CLS (mọi trạng thái) của 1 lượt khám — Bác sĩ xem lại kết quả trong Phiếu Khám
        public async Task<List<ClinicalOrderQueueItem>> GetClinicalOrdersByAppointmentAsync(int appointmentId)
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync($"/api/ClinicalOrders/by-appointment/{appointmentId}");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    if (obj != null && (bool)(obj.success ?? false) && obj.items != null)
                    {
                        var list = JsonConvert.DeserializeObject<List<ClinicalOrderQueueItem>>(obj.items.ToString());
                        if (list != null) return list;
                    }
                }
            }
            catch { }
            return new List<ClinicalOrderQueueItem>();
        }

        // Chi tiết 1 chỉ định siêu âm (mô tả/kết luận/ảnh đã đính kèm) — hiển thị khi KTV mở cửa sổ nhập kết quả
        public async Task<(string Description, string Conclusion, List<string> ImageUrls)> GetUltrasoundDetailAsync(int ultrasoundId)
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync($"/api/ClinicalOrders/ultrasound/{ultrasoundId}");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    if (obj != null && (bool)(obj.success ?? false))
                    {
                        string desc = (string)(obj.description ?? "");
                        string concl = (string)(obj.conclusion ?? "");
                        var urls = new List<string>();
                        if (obj.imageUrls != null)
                        {
                            var list = JsonConvert.DeserializeObject<List<string>>(obj.imageUrls.ToString());
                            if (list != null) urls = list;
                        }
                        return (desc, concl, urls);
                    }
                }
            }
            catch { }
            return ("", "", new List<string>());
        }

        // KTV đính kèm 1 ảnh siêu âm thật (chọn từ máy tính) — trả về URL ảnh vừa upload
        public async Task<(bool Success, string Url)> UploadUltrasoundImageAsync(int ultrasoundId, string filePath)
        {
            AttachBearerToken();
            try
            {
                using var form = new MultipartFormDataContent();
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                form.Add(fileContent, "file", System.IO.Path.GetFileName(filePath));

                var res = await _httpClient.PostAsync($"/api/ClinicalOrders/ultrasound/{ultrasoundId}/images", form);
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    string url = (string)(obj?.url ?? "");
                    return (true, url);
                }
            }
            catch { }
            return (false, "");
        }

        // Bỏ 1 ảnh siêu âm đính kèm nhầm (theo vị trí index trong danh sách ảnh hiện tại)
        public async Task<bool> RemoveUltrasoundImageAsync(int ultrasoundId, int index)
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.DeleteAsync($"/api/ClinicalOrders/ultrasound/{ultrasoundId}/images?index={index}");
                return res.IsSuccessStatusCode;
            }
            catch { }
            return false;
        }

        // Hàng đợi của Kỹ thuật viên CLS: done=false → còn 'Pending', done=true → đã có kết quả hôm nay
        public async Task<List<ClinicalOrderQueueItem>> GetClinicalOrderQueueAsync(string? type = null, bool done = false)
        {
            AttachBearerToken();
            try
            {
                string url = $"/api/ClinicalOrders/queue?done={done}" + (!string.IsNullOrEmpty(type) ? $"&type={type}" : "");
                var res = await _httpClient.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    if (obj != null && (bool)(obj.success ?? false) && obj.items != null)
                    {
                        var list = JsonConvert.DeserializeObject<List<ClinicalOrderQueueItem>>(obj.items.ToString());
                        if (list != null) return list;
                    }
                }
            }
            catch { }
            return new List<ClinicalOrderQueueItem>();
        }

        // KTV nộp kết quả Xét nghiệm
        public async Task<bool> SubmitTestResultAsync(int testId, string resultValue, string unit, string referenceRange, string resultStatus)
        {
            AttachBearerToken();
            try
            {
                var payload = new
                {
                    ResultValue = resultValue,
                    Unit = unit,
                    ReferenceRange = referenceRange,
                    ResultStatus = resultStatus,
                    PerformedByUserId = TokenVault.UserId
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var res = await _httpClient.PutAsync($"/api/ClinicalOrders/tests/{testId}/result", content);
                return res.IsSuccessStatusCode;
            }
            catch { }
            return false;
        }

        // KTV nộp kết quả Siêu âm
        public async Task<bool> SubmitUltrasoundResultAsync(int ultrasoundId, string description, string conclusion)
        {
            AttachBearerToken();
            try
            {
                var payload = new
                {
                    Description = description,
                    Conclusion = conclusion,
                    PerformedByUserId = TokenVault.UserId
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var res = await _httpClient.PutAsync($"/api/ClinicalOrders/ultrasound/{ultrasoundId}/result", content);
                return res.IsSuccessStatusCode;
            }
            catch { }
            return false;
        }

        // Xác thực CCCD hồ sơ NGƯỜI THÂN bởi Lễ Tân (PATCH /api/FamilyMembers/{id}/verify)
        public async Task<bool> VerifyFamilyMemberCccdAsync(int memberId, string cccd)
        {
            AttachBearerToken();
            try
            {
                var payload = JsonConvert.SerializeObject(new { cccdNumber = cccd });
                var content = new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json");
                var res = await _httpClient.PatchAsync($"/api/FamilyMembers/{memberId}/verify", content);
                return res.IsSuccessStatusCode;
            }
            catch { }
            return false;
        }

        // ── AI Symptom Checker + Escalate to Staff (Chat Hỗ Trợ) ──────────────

        // Hàng chờ tiếp nhận: status='Escalated' AND assigned_staff_id IS NULL
        public async Task<List<ChatQueueItem>> GetChatQueueAsync()
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync("/api/chat/staff/queue");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    if (obj != null && (bool)(obj.success ?? false) && obj.items != null)
                    {
                        var list = JsonConvert.DeserializeObject<List<ChatQueueItem>>(obj.items.ToString());
                        if (list != null) return list;
                    }
                }
            }
            catch { }
            return new List<ChatQueueItem>();
        }

        // Các phiên CHÍNH lễ tân đang đăng nhập đã tiếp nhận nhưng chưa đóng — dùng để tìm lại phiên
        // dở dang sau khi tắt/mở lại app (phiên đã claim không còn hiện trong /staff/queue nữa).
        public async Task<List<ChatQueueItem>> GetMyChatSessionsAsync()
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync("/api/chat/staff/my-sessions");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    if (obj != null && (bool)(obj.success ?? false) && obj.items != null)
                    {
                        var list = JsonConvert.DeserializeObject<List<ChatQueueItem>>(obj.items.ToString());
                        if (list != null) return list;
                    }
                }
            }
            catch { }
            return new List<ChatQueueItem>();
        }

        // Tiếp nhận 1 phiên chat — server chỉ cho gán nếu assigned_staff_id còn NULL (chống 2 lễ tân
        // cùng nhận 1 phiên); 409 Conflict nếu người khác đã tiếp nhận trước.
        public async Task<(bool Success, string Message)> ClaimChatSessionAsync(int sessionId)
        {
            AttachBearerToken();
            try
            {
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                var res = await _httpClient.PostAsync($"/api/chat/staff/sessions/{sessionId}/claim", content);
                var json = await res.Content.ReadAsStringAsync();
                if (res.IsSuccessStatusCode) return (true, "");

                try
                {
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    string msg = (string)(obj?.message ?? "Không thể tiếp nhận phiên chat.");
                    return (false, msg);
                }
                catch { return (false, "Không thể tiếp nhận phiên chat."); }
            }
            catch (Exception ex)
            {
                return (false, "Lỗi kết nối: " + ex.Message);
            }
        }

        // Lấy toàn bộ lịch sử tin nhắn của 1 phiên — dùng cả khi mở dialog lần đầu lẫn polling định kỳ
        public async Task<(bool Success, string Status, List<ChatMessageModel> Messages)> GetChatMessagesAsync(int sessionId)
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync($"/api/chat/sessions/{sessionId}/messages");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject<dynamic>(json);
                    if (obj != null && (bool)(obj.success ?? false))
                    {
                        string status = (string)(obj.status ?? "Escalated");
                        var list = obj.messages != null
                            ? JsonConvert.DeserializeObject<List<ChatMessageModel>>(obj.messages.ToString())
                            : new List<ChatMessageModel>();
                        return (true, status, list ?? new List<ChatMessageModel>());
                    }
                }
            }
            catch { }
            return (false, "", new List<ChatMessageModel>());
        }

        // Lễ tân gửi tin nhắn trả lời trực tiếp cho bệnh nhân
        public async Task<bool> SendStaffChatMessageAsync(int sessionId, string content)
        {
            AttachBearerToken();
            try
            {
                var payload = new { Content = content };
                var body = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var res = await _httpClient.PostAsync($"/api/chat/staff/sessions/{sessionId}/messages", body);
                return res.IsSuccessStatusCode;
            }
            catch { }
            return false;
        }

        // Lễ tân đóng phiên sau khi tư vấn xong
        public async Task<bool> CloseChatSessionAsync(int sessionId)
        {
            AttachBearerToken();
            try
            {
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                var res = await _httpClient.PostAsync($"/api/chat/sessions/{sessionId}/close", content);
                return res.IsSuccessStatusCode;
            }
            catch { }
            return false;
        }

        // ══════════════════════════════════════════════════════════════════════
        // ── PHÂN HỆ DƯỢC SĨ (PHARMACY API) ────────────────────────────────────
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Lấy danh sách các đơn thuốc đang chờ Dược sĩ phát (StatusId = 10 / PendingDispensing).
        /// </summary>
        // [Old code]: public async Task<List<PharmacyQueueItem>> GetPharmacyQueueAsync() { var response = await _httpClient.GetAsync("/api/MedicalRecords/pharmacy-queue"); ... }
        public async Task<List<PharmacyQueueItem>> GetPharmacyQueueAsync(bool todayOnly = true)
        {
            AttachBearerToken();
            try
            {
                // [New code]: Truyền todayOnly=true để lọc theo ngày hôm nay giống như Lễ Tân
                string url = todayOnly ? "/api/MedicalRecords/pharmacy-queue?todayOnly=true" : "/api/MedicalRecords/pharmacy-queue?todayOnly=false";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<PharmacyQueueItem>>(content);
                    return list ?? new List<PharmacyQueueItem>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetPharmacyQueueAsync] Error: {ex.Message}");
            }
            return new List<PharmacyQueueItem>();
        }

        /// <summary>
        /// Lấy danh sách lịch sử các đơn thuốc Dược sĩ đã phát (theo ngày và/hoặc tìm kiếm).
        /// </summary>
        // [Old code]: public async Task<List<PharmacyHistoryItem>> GetPharmacyHistoryAsync(DateTime? date = null) { ... }
        public async Task<List<PharmacyHistoryItem>> GetPharmacyHistoryAsync(DateTime? date = null, string search = "")
        {
            AttachBearerToken();
            try
            {
                string dateParam = date.HasValue ? date.Value.ToString("yyyy-MM-dd") : "all";
                string url = $"/api/MedicalRecords/pharmacy-history?date={dateParam}";
                if (!string.IsNullOrWhiteSpace(search))
                {
                    url += $"&search={Uri.EscapeDataString(search.Trim())}";
                }

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var list = JsonConvert.DeserializeObject<List<PharmacyHistoryItem>>(content);
                    return list ?? new List<PharmacyHistoryItem>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetPharmacyHistoryAsync] Error: {ex.Message}");
            }
            return new List<PharmacyHistoryItem>();
        }

        /// <summary>
        /// Dược sĩ xác nhận phát thuốc cho bệnh nhân (chuyển StatusId -> 4 / Completed).
        /// </summary>
        public async Task<(bool success, string message)> DispensePrescriptionAsync(int appointmentId, string pharmacistNote = "")
        {
            AttachBearerToken();
            try
            {
                string pharmName = !string.IsNullOrWhiteSpace(TokenVault.FullName) ? TokenVault.FullName.Trim() : TokenVault.GetFormattedTitleName();
                if (!string.IsNullOrWhiteSpace(pharmName) && !pharmName.StartsWith("DS") && !pharmName.StartsWith("Ds") && !pharmName.StartsWith("Dược sĩ"))
                {
                    pharmName = "DS. " + pharmName;
                }

                var payload = new DispenseRequestModel
                {
                    PharmacistUserId = TokenVault.UserId != Guid.Empty ? TokenVault.UserId : (Guid?)null,
                    PharmacistName = pharmName,
                    PharmacistNote = pharmacistNote
                };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/api/MedicalRecords/{appointmentId}/dispense", content);
                var resContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    dynamic resObj = JsonConvert.DeserializeObject(resContent);
                    string msg = resObj?.message != null ? (string)resObj.message : "Xác nhận phát thuốc thành công!";
                    return (true, msg);
                }
                else
                {
                    try
                    {
                        dynamic errObj = JsonConvert.DeserializeObject(resContent);
                        string errMsg = errObj?.message != null ? (string)errObj.message : $"Lỗi ({response.StatusCode})";
                        return (false, errMsg);
                    }
                    catch
                    {
                        return (false, $"Lỗi server: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối: {ex.Message}");
            }
        }
    }
}
