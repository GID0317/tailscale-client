using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using System.Threading.Tasks;
using Velopack;

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

    private static async Task CheckForUpdatesAsync(InfoBar updateError)
    {
        try
        {
            var mgr = new UpdateManager("https://tsc.xirreal.dev");
            if (!mgr.IsInstalled || mgr.IsPortable)
                return;
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null)
            {
                return;
            }

            await mgr.DownloadUpdatesAsync(newVersion);
            mgr.ApplyUpdatesAndRestart(newVersion);
        } catch (Exception e)
        {
            Debug.WriteLine($"Update failed: {e}");
            updateError.IsOpen = true;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var initSuccess = Core.API.Init();
        Core.Utils.InitializeColors();

        var updateError = new InfoBar
        {
            Severity = InfoBarSeverity.Warning,
            Title = "Could not update the app",
            Message = "You can continue using the app. Updates will be checked again next time you open it.",
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(8)
        };
        Page content = initSuccess ? new Views.AppSkeleton() : new Views.Error();
        Grid.SetRowSpan(updateError, 2);
        Canvas.SetZIndex(updateError, 200);
        ((Grid)content.Content).Children.Add(updateError);
        MainWindow = new MainWindow(content);
        MainWindow.ApplyTheme(Core.ThemePreference.Load());

        MainWindow.Activate();
        MainWindow.DispatcherQueue.TryEnqueue(async () => await CheckForUpdatesAsync(updateError));
    }

    public static new void Exit()
    {
        Current.Exit();
    }
}
