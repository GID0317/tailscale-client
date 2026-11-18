using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;
using WinUIEx;

namespace TailscaleClient;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow() : this(null) { }

    internal MainWindow(FrameworkElement content)
    {
        InitializeComponent();

        Title = Constants.AppDisplayName;
        if (content != null)
            Content = content;

        ExtendsContentIntoTitleBar = true;
        if (Content is FrameworkElement root)
        {
            root.ActualThemeChanged += Content_ActualThemeChanged;
            Content_ActualThemeChanged(root, null);
        }
    }

    internal void ApplyTheme(ElementTheme theme)
    {
        var root = (FrameworkElement)Content;
        // Mica follows Windows. Give explicit app themes a matching solid surface.
        if (theme == ElementTheme.Default)
            root.ClearValue(FrameworkElement.StyleProperty);
        else
            root.Style = (Style)Application.Current.Resources["AppThemePageStyle"];

        root.RequestedTheme = theme;
        UpdateCaptionColors(root);
    }

    private void Content_ActualThemeChanged(FrameworkElement sender, object args)
    {
        if (sender.ActualTheme == ElementTheme.Light)
        {
            AppWindow.SetIcon(Constants.AppIconLightAbsolutePath);
        }
        else
        {
            AppWindow.SetIcon(Constants.AppIconDarkAbsolutePath);
        }

        UpdateCaptionColors(sender);
    }

    private void UpdateCaptionColors(FrameworkElement root)
    {
        if (!AppWindowTitleBar.IsCustomizationSupported())
            return;

        var titleBar = AppWindow.TitleBar;
        var explicitTheme = root.RequestedTheme != ElementTheme.Default && !new AccessibilitySettings().HighContrast;
        var foreground = root.ActualTheme == ElementTheme.Light ? Colors.Black : Colors.White;
        titleBar.ButtonForegroundColor = explicitTheme ? foreground : null;
        titleBar.ButtonInactiveForegroundColor = explicitTheme ? Colors.Gray : null;
        titleBar.ButtonHoverForegroundColor = explicitTheme ? foreground : null;
        titleBar.ButtonPressedForegroundColor = explicitTheme ? foreground : null;
        titleBar.ButtonBackgroundColor = explicitTheme ? Colors.Transparent : null;
        titleBar.ButtonInactiveBackgroundColor = explicitTheme ? Colors.Transparent : null;
        titleBar.ButtonHoverBackgroundColor = explicitTheme ? ColorHelper.FromArgb(24, foreground.R, foreground.G, foreground.B) : null;
        titleBar.ButtonPressedBackgroundColor = explicitTheme ? ColorHelper.FromArgb(36, foreground.R, foreground.G, foreground.B) : null;
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        Core.WindowPersistence.Save();
        if (App.CanCloseWindow)
        {
            App.Exit();
        }
        else
        {
            args.Handled = true;
            this.Hide();
        }
    }
}
