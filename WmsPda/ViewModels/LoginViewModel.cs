using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WmsPda.Services;

namespace WmsPda.ViewModels;

/// <summary>
/// 登录页面 ViewModel，实现 MVVM 绑定
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _auth;

    [ObservableProperty]
    private string username = "";

    [ObservableProperty]
    private string password = "";

    [ObservableProperty]
    private string message = "";

    [ObservableProperty]
    private bool isError = false;

    [ObservableProperty]
    private bool isLoading = false;

    public LoginViewModel(AuthService auth)
    {
        _auth = auth;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            Message = "请输入用户名和密码";
            IsError = true;
            return;
        }

        IsLoading = true;
        Message = "";

        // 异步执行避免阻塞 UI
        await Task.Run(() =>
        {
            var (success, msg) = _auth.Login(Username, Password);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsLoading = false;
                Message = msg;
                IsError = !success;

                if (success)
                {
                    // 登录成功，跳转到主页
                    Shell.Current?.GoToAsync("//main");
                }
            });
        });
    }

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        await Shell.Current.GoToAsync("register");
    }
}
