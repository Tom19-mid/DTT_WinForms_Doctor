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
                
                int currentMaxId = newList.Any() ? newList.Max(a => a.AppointmentId) : 0;
                if (!_isFirstLoad && currentMaxId > _lastMaxId)
                {
                    var newAppts = newList.Where(a => a.AppointmentId > _lastMaxId).ToList();
                    foreach (var appt in newAppts)
                    {
                        _view.OnNewAppointmentNotified(appt.PatientName ?? "Khách", appt.TimeSlot ?? "Trong ngày", appt.SpecialtyName ?? "Nội tổng quát");
                    }
                }

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
                filtered = filtered.Where(a => a.Status == "Confirmed" || a.Status == "Đang chờ");
            }
            else if (statusFilter == "Đang khám")
            {
                filtered = filtered.Where(a => a.Status == "InProgress" || a.Status == "Đang khám");
            }
            else if (statusFilter == "Đã xong")
            {
                filtered = filtered.Where(a => a.Status == "Completed" || a.Status == "Đã xong" || a.Status == "Đã hoàn thành");
            }
            else if (statusFilter == "Hủy Lịch" || statusFilter == "Cancelled" || statusFilter == "Hủy lịch")
            {
                filtered = filtered.Where(a => a.Status == "Cancelled" || a.Status == "Đã hủy" || a.Status == "Hủy Lịch");
            }

            var list = filtered.OrderBy(a => a.QueueNumber).ToList();
            _view.DisplayAppointments(list);

            // Compute KPI counts from full list
            int total = _allAppointments.Count;
            int waiting = _allAppointments.Count(a => a.Status == "Confirmed" || a.Status == "Đang chờ");
            int inProgress = _allAppointments.Count(a => a.Status == "InProgress" || a.Status == "Đang khám");
            int completed = _allAppointments.Count(a => a.Status == "Completed" || a.Status == "Đã xong" || a.Status == "Đã hoàn thành");

            _view.UpdateKpiCards(total, waiting, inProgress, completed);
        }
    }
}
