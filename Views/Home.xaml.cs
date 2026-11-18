using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TailscaleClient.Assets;
using TailscaleClient.Core;

namespace TailscaleClient.Views;

public sealed partial class Home : Page
{
    public Home()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Messaging.Instance.MessageReceived += OnDaemonMessage;
        UpdateUI();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Messaging.Instance.MessageReceived -= OnDaemonMessage;
    }

    private void OnDaemonMessage(object sender, Messaging.CoreMessagingEvent e)
    {
        if (e.Kind == Messaging.MessageKind.ProfileSwitch ||
            (e.Kind == Messaging.MessageKind.IPNBusUpdate && (e.Key == "NetMap" || e.Key == "State")))
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (IsLoaded)
                    UpdateUI();
            });
        }
    }

    private void UpdateUI()
    {
        var status = API.GetStatus();
        var self = status.Self;
        var isConnected = status.BackendState == "Running" && self != null;

        var previousSelf = MyDevice.Children.OfType<DeviceCard>().FirstOrDefault();
        var collapsedPeers = OtherDevices.Children.OfType<DeviceCard>()
            .Where(card => !card.IsExpanded).Select(card => card.DeviceKey).ToHashSet();

        MyDevice.Children.Clear();
        OtherDevices.Children.Clear();
        ConnectionNotice.IsOpen = !isConnected;
        PeerSection.Visibility = isConnected ? Visibility.Visible : Visibility.Collapsed;

        if (!isConnected)
        {
            (ConnectionNotice.Title, ConnectionNotice.Message) = status.BackendState switch
            {
                "Starting" => ("Connecting to Tailscale", "Devices will appear once the connection is ready."),
                "NeedsLogin" => ("Sign in to see your devices", "Open Accounts to sign in to Tailscale."),
                _ => ("Tailscale is disconnected", "Open Accounts to connect this device.")
            };
            return;
        }

        MyDevice.Children.Add(new DeviceCard(self.ID, self, status.User, self.UserID)
        {
            IsExpanded = previousSelf?.DeviceKey != self.ID || previousSelf?.IsExpanded != false
        });

        var cards = (status.Peer ?? [])
            .Select(peer => new DeviceCard(peer.Key, peer.Value, status.User, self.UserID))
            .OrderBy(card => card.Hostname, StringComparer.CurrentCultureIgnoreCase);
        foreach (var card in cards)
        {
            card.IsExpanded = !collapsedPeers.Contains(card.DeviceKey);
            OtherDevices.Children.Add(card);
        }

        ApplyDeviceFilter();
    }

    private void ApplyDeviceFilter()
    {
        if (OtherDevices == null || NoDevicesMessage == null)
            return;

        var query = DeviceSearch.Text.Trim();
        var onlyOwned = OwnedDevicesFilter.IsChecked == true;
        var visibleCount = 0;
        foreach (var card in OtherDevices.Children.OfType<DeviceCard>())
        {
            var visible = (!onlyOwned || card.IsOwnedByCurrentUser) && card.Matches(query);
            card.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (visible)
                visibleCount++;
        }

        NoDevicesMessage.Text = query.Length > 0 ? "No devices match your search."
            : onlyOwned ? "You don't own any other devices on this tailnet."
            : "There are no other devices on this tailnet.";
        NoDevicesMessage.Visibility = visibleCount == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SearchChanged(object sender, TextChangedEventArgs e) => ApplyDeviceFilter();

    private void OwnershipChanged(object sender, RoutedEventArgs e) => ApplyDeviceFilter();
}
