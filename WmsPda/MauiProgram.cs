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
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // 注册服务
        builder.Services.AddSingleton<AuthService>();

        // 注册页面和 ViewModel
        builder.Services.AddTransient<Views.LoginPage>();
        builder.Services.AddTransient<ViewModels.LoginViewModel>();

        return builder.Build();
    }
}
