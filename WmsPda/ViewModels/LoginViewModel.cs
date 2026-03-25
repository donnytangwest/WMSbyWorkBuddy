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
    private readonly IServiceProvider _serviceProvider;

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

    public LoginViewModel(AuthService auth, IServiceProvider serviceProvider)
    {
        _auth = auth;
        _serviceProvider = serviceProvider;
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

        await Task.Run(() =>
        {
            var (success, msg) = _auth.Login(Username, Password);
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                IsLoading = false;
                Message = msg;
                IsError = !success;

                if (success)
                {
                    // 登录成功，跳转主页（使用 NavigationPage 推入新页面）
                    var mainPage = new ContentPage
                    {
                        BackgroundColor = Color.FromArgb("#F5F7FA"),
                        Content = new VerticalStackLayout
                        {
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.Center,
                            Spacing = 16,
                            Children =
                            {
                                new Label
                                {
                                    Text = "✅ 登录成功",
                                    FontSize = 28,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Color.FromArgb("#1565C0"),
                                    HorizontalOptions = LayoutOptions.Center
                                },
                                new Label
                                {
                                    Text = $"欢迎，{Username}",
                                    FontSize = 20,
                                    TextColor = Color.FromArgb("#333333"),
                                    HorizontalOptions = LayoutOptions.Center
                                },
                                new Label
                                {
                                    Text = "WMS 仓库管理系统",
                                    FontSize = 16,
                                    TextColor = Color.FromArgb("#888888"),
                                    HorizontalOptions = LayoutOptions.Center
                                }
                            }
                        }
                    };
                    NavigationPage.SetHasNavigationBar(mainPage, false);

                    // 获取当前 NavigationPage 并 Push
                    if (Application.Current?.MainPage is NavigationPage navPage)
                    {
                        await navPage.PushAsync(mainPage);
                    }
                }
            });
        });
    }

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        // 暂时提示，后续可扩展注册页
        Message = "注册功能即将上线，请联系管理员创建账号";
        IsError = false;
        await Task.CompletedTask;
    }
}
