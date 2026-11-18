using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace TailscaleClient.Assets;

public sealed record DeviceDetail(string Label, string Value);

public sealed partial class DeviceCard : UserControl
{
    private static SolidColorBrush GetStatusColor(bool state)
    {
        return Core.Utils.colors[state ? "StatusSuccess" : "StatusFail"];
    }

    private static string FormatDaysUntilExpiry(int daysUntilExpiry)
    {
        var daysText = daysUntilExpiry switch
        {
            1 => "day",
            _ => "days",
        };

        return daysUntilExpiry switch
        {
            <0 => "Expired",
            0 => "Key expiring today",
            _ => $"Expiring in {daysUntilExpiry} {daysText}"
        };
    }

    private static Windows.UI.Color FormatColorForExpiry(int daysUntilExpiry)
    {
        return daysUntilExpiry switch
        {
            <=0 => Windows.UI.Color.FromArgb(255, 255, 0, 0),
            _ => Windows.UI.Color.FromArgb(255, 255, 150, 0),
        };
    }

    public string DeviceKey { get; }
    public string Hostname { get; }
    public bool IsOwnedByCurrentUser { get; }
    public bool IsExpanded
    {
        get => DeviceDetails.IsExpanded;
        set => DeviceDetails.IsExpanded = value;
    }

    private readonly string _systemHostname;
    private readonly string IPv4;
    private readonly string IPv6;
    private readonly string TailnetDNS;
    private readonly string Summary;
    private readonly List<DeviceDetail> Details;
    private readonly IconElement PlatformIcon;
    private readonly SolidColorBrush StatusColor;

    public DeviceCard(string deviceKey, Core.Types.PeerInfo device, Dictionary<string, Core.Types.User> users, long currentUserId)
    {
        DeviceKey = deviceKey;
        IsOwnedByCurrentUser = device.UserID == currentUserId;
        _systemHostname = device.HostName;
        TailnetDNS = device.DNSName?.TrimEnd('.') ?? "";
        var nickname = TailnetDNS.Split('.')[0];
        Hostname = nickname.Length > 0 ? nickname : device.HostName ?? "Unknown device";

        var addresses = device.TailscaleIPs ?? [];
        IPv4 = addresses.FirstOrDefault(address => !string.IsNullOrEmpty(address) && !address.Contains(':'));
        IPv6 = addresses.FirstOrDefault(address => address?.Contains(':') == true);
        var status = device.Online ? "Online" : device.LastSeen.Year >= 2010 ? "Offline" : "Action required";
        var primaryAddress = IPv4 ?? IPv6;
        Summary = primaryAddress == null ? status : $"{status} - {primaryAddress}";
        StatusColor = GetStatusColor(device.Online);

        Details =
        [
            new("Device name", Hostname),
            new("IPv4", IPv4 ?? "Unavailable"),
            new("IPv6", IPv6 ?? "Unavailable"),
            new("DNS name", TailnetDNS.Length == 0 ? "Unavailable" : TailnetDNS),
            new("Status", status)
        ];
        if (!string.IsNullOrEmpty(_systemHostname) && !string.Equals(Hostname, _systemHostname, StringComparison.OrdinalIgnoreCase))
            Details.Add(new("Host name", _systemHostname));
        if (users != null && users.TryGetValue(device.UserID.ToString(), out var owner))
        {
            var ownerName = string.IsNullOrWhiteSpace(owner.DisplayName) ? owner.LoginName : owner.DisplayName;
            if (!string.IsNullOrWhiteSpace(ownerName))
                Details.Add(new("Owner", ownerName));
        }
        if (!device.Online && device.LastSeen.Year >= 2010)
            Details.Add(new("Last seen", device.LastSeen.ToLocalTime().ToString("g")));

        var deviceType = device.OS switch
        {
            "linux" => "Linux",
            "windows" => "Windows",
            "macOS" => "Apple",
            "iOS" => "Apple",
            "android" => "Android",
            _ => "Unknown",
        };
        PlatformIcon = deviceType == "Windows"
            ? new PathIcon
            {
                Width = 20,
                Height = 20,
                Data = new GeometryGroup
                {
                    Children =
                    {
                        new RectangleGeometry { Rect = new Rect(0, 0, 9, 9) },
                        new RectangleGeometry { Rect = new Rect(11, 0, 9, 9) },
                        new RectangleGeometry { Rect = new Rect(0, 11, 9, 9) },
                        new RectangleGeometry { Rect = new Rect(11, 11, 9, 9) }
                    }
                }
            }
            : new BitmapIcon
            {
                Width = 20,
                Height = 20,
                UriSource = new Uri($"ms-appx:///Assets/Platforms/{deviceType}.png")
            };

        InitializeComponent();

        if (device.KeyExpiry == DateTime.MinValue)
        {
            TagSection.Children.Add(new FeatureTag(
                "Key expiry disabled",
                "The key for this device will never expire: you can change this in the admin panel."));
        }
        else
        {
            var days = (device.KeyExpiry - DateTime.Now).TotalDays;
            if (days <= 14)
            {
                var roundedDays = (int)Math.Ceiling(days);
                var expiryTag = new FeatureTag(FormatDaysUntilExpiry(roundedDays),
                                               "To refresh the key, reauthenticate on the device.",
                                               FormatColorForExpiry(roundedDays));
                TagSection.Children.Add(expiryTag);
            }
        }

        if (device.Online && device.sshHostKeys != null)
        {
            TagSection.Children.Add(new FeatureTag("SSH", "Tailscale SSH is enabled on this device.",
                                                   Windows.UI.Color.FromArgb(255, 0, 255, 0)));
        }

        if (device.Online && device.PrimaryRoutes != null)
        {
            var routes = "This device exposes subnet routes.\n\n";
            foreach (var route in device.PrimaryRoutes)
            {
                routes += " •  " + route + "\n";
            }

            TagSection.Children.Add(new FeatureTag("Subnets", routes.TrimEnd('\n'),
                                                   Windows.UI.Color.FromArgb(255, 0, 200, 255)));
        }

        if (device.Online && device.ExitNodeOption)
        {
            TagSection.Children.Add(new FeatureTag("Exit node",
                                                   "This device is advertising an exit node.",
                                                   Windows.UI.Color.FromArgb(255, 0, 200, 255)));
        }

        if (TagSection.Children.Count > 0)
            TagSection.Visibility = Visibility.Visible;
    }

    internal bool Matches(string query)
    {
        return query.Length == 0 ||
            Hostname.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            _systemHostname?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
            TailnetDNS.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            IPv4?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
            IPv6?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
    }

    private void CopyDetails(object sender, RoutedEventArgs e)
    {
        var text = string.Join(Environment.NewLine, Details.Select(detail => $"{detail.Label}: {detail.Value}"));
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
    }
}
