using System;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;

namespace TailscaleClient.Core;

internal static class ThemePreference
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TailscaleClient", "theme.txt");
    private static readonly string NavigationStylePath = Path.Combine(Path.GetDirectoryName(FilePath), "navigation-style.txt");

    public static ElementTheme Load() => ReadPreference(FilePath) switch
    {
        nameof(ElementTheme.Light) => ElementTheme.Light,
        nameof(ElementTheme.Dark) => ElementTheme.Dark,
        _ => ElementTheme.Default
    };

    public static bool LoadTopNavigation() => ReadPreference(NavigationStylePath) == "Top";

    public static bool TrySave(ElementTheme theme) => TryWritePreference(FilePath, theme.ToString());

    public static bool TrySaveNavigationStyle(bool useTop) =>
        TryWritePreference(NavigationStylePath, useTop ? "Top" : "Left");

    private static string ReadPreference(string path)
    {
        try
        {
            return File.ReadAllText(path).Trim();
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Could not read {path}: {error}");
            return null;
        }
    }

    private static bool TryWritePreference(string path, string value)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, value);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Could not write {path}: {error}");
            return false;
        }
    }
}
