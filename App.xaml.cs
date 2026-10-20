using System.Globalization;
using Microsoft.UI.Xaml;

namespace IdermaAdministracion;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        var cultura = CultureInfo.GetCultureInfo("es-PE");
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
