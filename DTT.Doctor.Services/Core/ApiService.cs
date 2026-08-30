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
                // Trước đây có một fallback đăng nhập ngoại tuyến bằng mật khẩu demo cố định
                // (Demo@2026) cho ~14 số điện thoại hardcode — nghĩa là chỉ cần API mất kết nối
                // là đăng nhập được vào bất kỳ tài khoản nào trong danh sách mà không cần xác thực
                // thật. Đã bỏ hoàn toàn: khi không gọi được API, đăng nhập phải thất bại rõ ràng
                // thay vì cấp một phiên hợp lệ không qua xác thực server.
                return new DoctorAuthResponseDto { Message = $"Không thể kết nối ({BaseUrl}). Lỗi: " + ex.Message };
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
                // Không kết nối được server (mất mạng/server tắt hẳn) — KHÔNG hiện danh sách hàng chờ
                // demo giả ("David Johns", "Pete Hawks", "Test", "tester"...) vì trông giống bệnh nhân
                // thật và được dùng chung bởi cả Bác sĩ/Điều dưỡng/Lễ Tân. Trả về rỗng để giao diện
                // hiện "không có dữ liệu" / lỗi kết nối thay vì dữ liệu giả.
            }

            return new List<AppointmentModel>();
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
        public async Task<(decimal ExamFee, decimal ServicesFee, decimal MedsFee, decimal Total, bool IsPackage, bool Success)> GetInvoiceEstimateAsync(int appointmentId)
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync($"/api/Invoices/estimate/{appointmentId}");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    var dto = JsonConvert.DeserializeObject<InvoiceEstimateResponse>(json);
                    if (dto != null && dto.Success)
                    {
                        decimal total = dto.TotalAmount > 0 ? dto.TotalAmount : (dto.ExamFee + dto.ServicesFee + dto.MedsFee);
                        return (dto.ExamFee, dto.ServicesFee, dto.MedsFee, total, dto.IsPackage, true);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("GetInvoiceEstimateAsync error: " + ex.Message);
            }
            // Gọi API thất bại — KHÔNG trả về mức phí khám bịa cứng 250.000đ trông giống số tiền thật,
            // vì nơi gọi hàm này (màn Thanh Toán) dùng trực tiếp số tiền để thu tiền mặt thật từ bệnh
            // nhân. Trả về 0đ kèm Success = false để nơi gọi biết rõ đây là "chưa tải được dữ liệu"
            // và phải chặn thao tác thu tiền thay vì âm thầm hiển thị/thu một số tiền không có thật.
            return (0m, 0m, 0m, 0m, false, false);
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

        // [New code - Lấy thông tin và mã ảnh VietQR chuẩn Napas 247]:
        public async Task<VietQrResponse?> GetVietQrInfoAsync(int appointmentId)
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync($"/api/Invoices/vietqr/{appointmentId}");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<VietQrResponse>(json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetVietQrInfoAsync] Error: {ex.Message}");
            }
            return null;
        }

        // [New code - Tạo URL cổng thanh toán VNPAY Sandbox]:
        // [Old code - Cổng VNPAY]:
        // public async Task<VnPayUrlResponse?> CreateVnPayPaymentUrlAsync(int appointmentId)
        // {
        //     AttachBearerToken();
        //     try
        //     {
        //         var payload = new { AppointmentId = appointmentId };
        //         var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        //         var res = await _httpClient.PostAsync("/api/Invoices/vnpay-create-payment-url", content);
        //         if (res.IsSuccessStatusCode)
        //         {
        //             var json = await res.Content.ReadAsStringAsync();
        //             return JsonConvert.DeserializeObject<VnPayUrlResponse>(json);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         System.Diagnostics.Debug.WriteLine($"[CreateVnPayPaymentUrlAsync] Error: {ex.Message}");
        //     }
        //     return null;
        // }

        // [New code - Cổng thanh toán quốc tế PayPal REST API v2 & quét mã QR điện thoại]:
        public async Task<PaypalInfoResponse?> GetPaypalInfoAsync(int appointmentId)
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync($"/api/Invoices/paypal-info/{appointmentId}");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<PaypalInfoResponse>(json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetPaypalInfoAsync] Error: {ex.Message}");
            }
            return null;
        }

        // [New code - Kiểm tra trạng thái thanh toán của ca khám]:
        public async Task<PaymentStatusResponse?> GetPaymentStatusAsync(int appointmentId)
        {
            AttachBearerToken();
            try
            {
                var res = await _httpClient.GetAsync($"/api/Invoices/status/{appointmentId}");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<PaymentStatusResponse>(json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetPaymentStatusAsync] Error: {ex.Message}");
            }
            return null;
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
            // Gọi API thất bại — KHÔNG hiện danh sách chuyên khoa/bác sĩ bịa cứng (4 bác sĩ mẫu
            // với DoctorId 1-4 không chắc tồn tại trong DB thật) vì có thể khiến hồ sơ đăng ký
            // vãng lai bị gán nhầm vào một bác sĩ không có thật. Trả về rỗng để giao diện hiện
            // "không có bác sĩ" thay vì dữ liệu giả.
            return new List<(string, int, string)>();
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

            // Gọi API thất bại hoặc trả về rỗng — KHÔNG hiện danh sách bệnh nhân demo giả
            // ("David Johns", "Test"...) vì trông giống dữ liệu thật, dễ khiến Lễ Tân tưởng
            // nhầm là danh sách bệnh nhân thật. Trả về rỗng để giao diện hiện "không có dữ liệu"
            // / thông báo lỗi kết nối thay vì dữ liệu giả.
            return new List<PatientSimpleModel>();
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
