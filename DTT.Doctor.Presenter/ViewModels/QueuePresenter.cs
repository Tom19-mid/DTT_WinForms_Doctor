using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;

namespace DTT.Doctor.Presenter.ViewModels
{
    public interface IQueueView
    {
        void ShowLoading(bool isLoading);
        void DisplayAppointments(List<AppointmentModel> appointments);
        void UpdateKpiCards(int total, int waiting, int inProgress, int completed);
        void OnError(string message);
        void OnNewAppointmentNotified(string patientName, string timeSlot, string specialtyName);
        // Bệnh nhân vừa có đủ kết quả Xét nghiệm/Siêu âm và quay lại hàng chờ Bác sĩ (status 9 → 3)
        void OnClinicalResultsReady(string patientName, string specialtyName);
    }

    public class QueuePresenter
    {
        private readonly IQueueView _view;
        private readonly ApiService _apiService;
        private List<AppointmentModel> _allAppointments = new List<AppointmentModel>();
        private int _lastMaxId = 0;
        private bool _isFirstLoad = true;
        private string _currentQuery = "";
        private string _currentStatusFilter = "Tất cả";
        private Dictionary<int, string> _previousStatuses = new Dictionary<int, string>();

        public QueuePresenter(IQueueView view, ApiService apiService = null)
        {
            _view = view;
            _apiService = apiService ?? new ApiService();
        }

        public AppointmentModel GetAppointmentById(int id)
        {
            return _allAppointments.FirstOrDefault(a => a.AppointmentId == id);
        }

        public async Task LoadQueueAsync(bool isSilent = false)
        {
            if (!isSilent) _view.ShowLoading(true);
            try
            {
                var newList = await _apiService.GetQueueAppointmentsAsync();

                // ── Lọc theo role ────────────────────────────────────────────────
                bool isDoctor = TokenVault.RoleCode == "DOCTOR" || TokenVault.RoleId == 2 ||
                                (!string.IsNullOrEmpty(TokenVault.RoleName) && TokenVault.RoleName.Contains("Bác sĩ"));
                bool isNurse  = TokenVault.RoleCode == "NURSE"  || TokenVault.RoleId == 5 ||
                                (!string.IsNullOrEmpty(TokenVault.RoleName) && TokenVault.RoleName.Contains("Điều dưỡng"));

                if (isDoctor)
                {
                    // Bác sĩ chỉ thấy bệnh nhân từ WaitingForDoctor (8) trở đi
                    // → CheckedIn (7) chưa đo sinh hiệu → Bác sĩ không thấy
                    // [Old code]:
                    // newList = newList.Where(a =>
                    //     a.Status == "WaitingForDoctor"     ||
                    //     a.Status == "InProgress"           ||
                    //     a.Status == "AwaitingTestResults"  ||
                    //     a.Status == "Completed"            ||
                    //     a.Status == "NoShow"               ||
                    //     a.Status == "Cancelled").ToList();

                    // [New code - thêm PendingDispensing để Bác sĩ thấy ca đang chờ Dược sĩ phát thuốc]:
                    newList = newList.Where(a =>
                        a.Status == "WaitingForDoctor"     ||
                        a.Status == "InProgress"           ||
                        a.Status == "AwaitingTestResults"  || // Đã chỉ định CLS, đang chờ KTV trả kết quả
                        a.Status == "PendingDispensing"    || // BS đã kê đơn → chờ Dược sĩ phát thuốc
                        a.Status == "Completed"            ||
                        a.Status == "NoShow"               ||
                        a.Status == "Cancelled").ToList();
                }
                else if (isNurse)
                {
                    // Điều dưỡng chỉ thấy CheckedIn (7) + WaitingForDoctor (8) đã đo hôm nay
                    newList = newList.Where(a =>
                        a.Status == "CheckedIn"        ||
                        a.Status == "WaitingForDoctor").ToList();
                }
                // Lễ tân thấy toàn bộ (không lọc)

                int currentMaxId = newList.Any() ? newList.Max(a => a.AppointmentId) : 0;
                if (!_isFirstLoad && currentMaxId > _lastMaxId)
                {
                    var newAppts = newList.Where(a => a.AppointmentId > _lastMaxId).ToList();
                    foreach (var appt in newAppts)
                    {
                        _view.OnNewAppointmentNotified(appt.PatientName ?? "Khách", appt.TimeSlot ?? "Trong ngày", appt.SpecialtyName ?? "Nội tổng quát");
                    }
                }

                // Phát hiện ca vừa có đủ kết quả CLS quay lại hàng chờ (status: AwaitingTestResults → InProgress)
                // để báo Bác sĩ chủ động, thay vì phải tự làm mới màn hình mới biết.
                if (!_isFirstLoad)
                {
                    foreach (var appt in newList)
                    {
                        if (_previousStatuses.TryGetValue(appt.AppointmentId, out var prevStatus) &&
                            prevStatus == "AwaitingTestResults" && appt.Status == "InProgress")
                        {
                            _view.OnClinicalResultsReady(appt.PatientName ?? "Bệnh nhân", appt.SpecialtyName ?? "Nội tổng quát");
                        }
                    }
                }
                _previousStatuses = newList.ToDictionary(a => a.AppointmentId, a => a.Status ?? "");

                _lastMaxId = Math.Max(_lastMaxId, currentMaxId);
                _isFirstLoad = false;
                _allAppointments = newList;

                if (!isSilent) _view.ShowLoading(false);
                FilterAndDisplay(_currentQuery, _currentStatusFilter);
            }
            catch (Exception ex)
            {
                if (!isSilent) _view.ShowLoading(false);
                if (!isSilent) _view.OnError("Lỗi đồng bộ dữ liệu hàng chờ: " + ex.Message);
            }
        }

        public async Task<bool> UpdateStatusAsync(int appointmentId, string status)
        {
            bool success = await _apiService.UpdateAppointmentStatusAsync(appointmentId, status);
            var target = _allAppointments.FirstOrDefault(a => a.AppointmentId == appointmentId);
            if (target != null)
            {
                target.Status = status;
            }
            FilterAndDisplay(_currentQuery, _currentStatusFilter);
            return success;
        }

        public void FilterAndDisplay(string query, string statusFilter = "Tất cả")
        {
            _currentQuery = query;
            _currentStatusFilter = statusFilter;
            var filtered = _allAppointments.AsEnumerable();

            // Filter by name or phone
            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.Trim().ToLower();
                filtered = filtered.Where(a => 
                    (a.PatientName != null && a.PatientName.ToLower().Contains(query)) ||
                    (a.SpecialtyName != null && a.SpecialtyName.ToLower().Contains(query)) ||
                    (a.QueueNumber.ToString().Equals(query)) ||
                    ($"P00{a.PatientId}".ToLower().Contains(query))
                );
            }

            // Filter by pill tab buttons
            if (statusFilter == "Đang chờ")
            {
                filtered = filtered.Where(a => a.Status == "Confirmed" || a.Status == "Đang chờ" || a.Status == "CheckedIn" || a.Status == "Scheduled" || a.Status == "WaitingForDoctor");
            }
            else if (statusFilter == "Đang khám")
            {
                filtered = filtered.Where(a => a.Status == "InProgress" || a.Status == "AwaitingTestResults" || a.Status == "Đang khám");
            }
            else if (statusFilter == "Đã xong")
            {
                // [Old code]: filtered = filtered.Where(a => a.Status == "Completed" || a.Status == "Đã xong" || a.Status == "Đã hoàn thành");
                // [New code]:
                filtered = filtered.Where(a => a.Status == "Completed" || a.Status == "Đã xong" || a.Status == "Đã hoàn thành" || a.Status == "PendingDispensing");
            }
            else if (statusFilter == "Hủy Lịch" || statusFilter == "Cancelled" || statusFilter == "Hủy lịch")
            {
                filtered = filtered.Where(a => a.Status == "Cancelled" || a.Status == "Đã hủy" || a.Status == "Hủy Lịch");
            }

            var list = filtered.OrderBy(a => a.QueueNumber).ToList();
            _view.DisplayAppointments(list);

            // Compute KPI counts from full list
            int total = _allAppointments.Count;
            int waiting = _allAppointments.Count(a => a.Status == "Confirmed" || a.Status == "Đang chờ" || a.Status == "CheckedIn" || a.Status == "Scheduled" || a.Status == "WaitingForDoctor");
            int inProgress = _allAppointments.Count(a => a.Status == "InProgress" || a.Status == "AwaitingTestResults" || a.Status == "Đang khám");
            // [Old code]: int completed = _allAppointments.Count(a => a.Status == "Completed" || a.Status == "Đã xong" || a.Status == "Đã hoàn thành");
            // [New code]:
            int completed = _allAppointments.Count(a => a.Status == "Completed" || a.Status == "Đã xong" || a.Status == "Đã hoàn thành" || a.Status == "PendingDispensing");

            _view.UpdateKpiCards(total, waiting, inProgress, completed);
        }
    }
}
