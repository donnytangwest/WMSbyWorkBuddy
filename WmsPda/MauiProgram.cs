using WmsPda.Services;

namespace WmsPda;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // 使用系统默认字体，无需额外字体文件
            });

        // 注册服务
        builder.Services.AddSingleton<AuthService>();

        // 注册页面和 ViewModel
        builder.Services.AddTransient<Views.LoginPage>();
        builder.Services.AddTransient<ViewModels.LoginViewModel>();

        return builder.Build();
    }
}
