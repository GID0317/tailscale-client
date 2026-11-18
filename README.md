# Tailscale Fluent

Tailscale Fluent is a community-maintained WinUI 3 desktop frontend for [Tailscale](https://tailscale.com/) on Windows. It continues [Tailscale Client](https://github.com/uwu/tailscale-client) by xirreal.

Install [Tailscale for Windows](https://tailscale.com/download/windows) and keep its service running. Tailscale Fluent uses that service to manage your connection and tailnet.

## Features
- [x] Account switching
- [x] Full login flow, supporting custom control servers such as headscale instances
- [x] QR code logins
- [x] Netmap view (devices page)
- [x] Device details and copy (such as IP, domain, etc.)
- [x] Exit nodes
- [x] Network preferences for incoming connections, Tailscale DNS, and subnet routes
- [x] Light, dark, and system themes with top or left navigation
- [ ] Taildrop

## Portable preview

Choose the x64 or ARM64 ZIP from [Tailscale Fluent releases](https://github.com/GID0317/tailscale-client/releases). Extract the entire archive into a folder, then run `TailscaleClient.exe` from that folder. Download and extract a newer archive to update.

## Building and running
0. Prerequisites
   - Visual Studio 2022
   - Windows 10 21H1 or later
   - Windows SDK 22000.194 or later
   - Windows App SDK 1.7 (restored by NuGet)
   - .NET 8.0 SDK
1. Clone the repository
2. Open `TailscaleClient.csproj` in Visual Studio 2022
3. Build the project and run

To build a portable folder with .NET and the Windows App Runtime included:

```powershell
dotnet publish TailscaleClient.csproj -p:Platform=x64 -c Release -r win-x64 --self-contained true -o ./dist/x64/
```

For ARM64, use `Platform=arm64`, `win-arm64`, and `./dist/arm64/`. Distribute the entire published folder, including its DLLs and resource files. The Tailscale daemon must still be installed separately.

## Contributing
Contributions are welcome! Please open an issue or pull request if you have any suggestions or changes.

## License
This project is licensed under the [MIT License](LICENSE.txt). See [third-party notices](THIRD-PARTY-NOTICES.txt) for the WinUI Gallery CopyButton attribution.

## Acknowledgements
- [Tailscale](https://tailscale.com), for making Tailscale open-source
- [QRCoder](https://github.com/codebude/QRCoder), used to generate the QRs for login
