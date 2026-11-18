using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TailscaleClient;

public partial class App : Application
{
    public static MainWindow MainWindow { get; set; }

    public static bool CanCloseWindow { get; set; }

    public App()
    {
        InitializeComponent();

        Core.WindowPersistence.Initialize();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var initSuccess = Core.API.Init();
        Core.Utils.InitializeColors();

        Page content = initSuccess ? new Views.AppSkeleton() : new Views.Error();
        MainWindow = new MainWindow(content);
        MainWindow.ApplyTheme(Core.ThemePreference.Load());

        MainWindow.Activate();
    }

    public static new void Exit()
    {
        Current.Exit();
    }
}
