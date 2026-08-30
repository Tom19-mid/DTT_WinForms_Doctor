using System;
using System.Threading.Tasks;
using DTT.Doctor.Services.Core;
using DTT.Doctor.Services.Models;

namespace DTT.Doctor.Presenter.ViewModels
{
    public interface ILoginView
    {
        void ShowLoading(bool isLoading);
        void OnLoginSuccess(DoctorAuthResponseDto response);
        void OnLoginFailure(string errorMessage);
    }

    public class LoginPresenter
    {
        private readonly ILoginView _view;
        private readonly ApiService _apiService;

        public LoginPresenter(ILoginView view, ApiService apiService = null)
        {
            _view = view;
            _apiService = apiService ?? new ApiService();
        }

        public async Task AttemptLoginAsync(string phone, string password)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                _view.OnLoginFailure("Vui lòng nhập Số điện thoại.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                _view.OnLoginFailure("Vui lòng nhập mật khẩu");
                return;
            }

            _view.ShowLoading(true);
            try
            {
                var result = await _apiService.LoginDoctorAsync(phone.Trim(), password);
                _view.ShowLoading(false);

                if (result.IsSuccess)
                {
                    _view.OnLoginSuccess(result);
                }
                else
                {
                    _view.OnLoginFailure(string.IsNullOrEmpty(result.Message) ? "Đăng nhập thất bại. Kiểm tra lại thông tin." : result.Message);
                }
            }
            catch (Exception ex)
            {
                _view.ShowLoading(false);
                _view.OnLoginFailure("Lỗi hệ thống: " + ex.Message);
            }
        }
    }
}
