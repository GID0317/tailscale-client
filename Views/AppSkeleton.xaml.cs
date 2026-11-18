using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TailscaleClient.Core;

namespace TailscaleClient.Views;

public sealed partial class AppSkeleton : Page, INotifyPropertyChanged
{
    private bool _synchronizingNavigation;
    internal bool UsesTopNavigation => NavigationViewControl.PaneDisplayMode == NavigationViewPaneDisplayMode.Top;

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private string _loadingMessage = "Loading...";
    public string LoadingMessage
    {
        get => _loadingMessage;
        set
        {
            _loadingMessage = value;
            OnPropertyChanged();
        }
    }

    public AppSkeleton()
    {
        InitializeComponent();
        ApplyNavigationStyle(ThemePreference.LoadTopNavigation());

        var status = Core.API.GetStatus();
        if (status.BackendState == "NeedsLogin")
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[1];
        }
        else
        {
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
        }

        Core.Messaging.Instance.MessageReceived += (sender, e) =>
        {
            if (e.Kind == Core.Messaging.MessageKind.HealthUpdate)
            {
                var warnings = Core.Types.Warning.ParseWarningsFromJson(e.Value);
                DispatcherQueue.TryEnqueue(() => UpdateWarnings(warnings));
            }
        };
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        App.MainWindow.SetTitleBar(AppTitleBar);
    }

    private void OnPaneToggleRequested(TitleBar sender, object args)
    {
        if (NavigationViewControl.IsEnabled)
            NavigationViewControl.IsPaneOpen = !NavigationViewControl.IsPaneOpen;
    }

    internal void ApplyNavigationStyle(bool useTop)
    {
        NavigationViewControl.PaneDisplayMode = useTop
            ? NavigationViewPaneDisplayMode.Top : NavigationViewPaneDisplayMode.Auto;
        AppTitleBar.IsPaneToggleButtonVisible = !useTop;
    }

    private void OnBackRequested(TitleBar sender, object args)
    {
        if (NavigationViewControl.IsEnabled && ContentFrame.CanGoBack)
            ContentFrame.GoBack();
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs args)
    {
        _synchronizingNavigation = true;
        try
        {
            NavigationViewControl.SelectedItem = args.SourcePageType == typeof(Settings)
                ? NavigationViewControl.SettingsItem
                : NavigationViewControl.MenuItems[args.SourcePageType == typeof(Accounts) ? 1 : 0];
        }
        finally
        {
            _synchronizingNavigation = false;
        }
        UpdateBackButton();
    }

    private void UpdateBackButton()
    {
        AppTitleBar.IsBackButtonVisible = ContentFrame.CanGoBack;
        AppTitleBar.IsBackButtonEnabled = ContentFrame.CanGoBack && NavigationViewControl.IsEnabled;
    }

    private void OnNavigate(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_synchronizingNavigation)
            return;

        var page = args.IsSettingsSelected ? typeof(Settings) : (args.SelectedItem as NavigationViewItem)?.Tag switch
        {
            "Home" => typeof(Home),
            "Accounts" => typeof(Accounts),
            _ => null
        };

        if (page != null && ContentFrame.CurrentSourcePageType != page)
            ContentFrame.Navigate(page, null, args.RecommendedNavigationTransitionInfo);
    }

    public void DisableUI()
    {
        DisableUI("Loading...");
    }

    public void DisableUI(string message)
    {
        NavigationViewControl.IsEnabled = false;
        ContentFrame.IsEnabled = false;
        UpdateBackButton();

        LoadingMessage = message;

        LoadingBackground.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
    }

    public void EnableUI()
    {
        NavigationViewControl.IsEnabled = true;
        ContentFrame.IsEnabled = true;
        UpdateBackButton();

        LoadingBackground.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    public void UpdateWarnings(List<Core.Types.Warning> warnings)
    {
        if (warnings.Count > 0)
        {
            InfoBarContent.Children.Clear();
            foreach (var warning in warnings)
            {
                InfoBarContent.Children.Add(warning.GetWarningComponent());
            }
            InfoBar.IsOpen = true;
            InfoBar.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            InfoBar.Title = $"{warnings.Count} warning{(warnings.Count != 1 ? "s" : "")}";
        }
        else
        {
            HideInfoBar();
        }
    }

    private void HideInfoBar()
    {
        InfoBar.IsOpen = false;
        InfoBarContent.Children.Clear();
        InfoBar.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private void HideInfoBar(InfoBar _, object __)
    {
        HideInfoBar();
    }
}
