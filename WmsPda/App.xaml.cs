namespace WmsPda;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var loginPage = _serviceProvider.GetRequiredService<Views.LoginPage>();
        var navPage = new NavigationPage(loginPage)
        {
            BarBackgroundColor = Color.FromArgb("#1565C0"),
            BarTextColor = Colors.White
        };
        return new Window(navPage);
    }
}
