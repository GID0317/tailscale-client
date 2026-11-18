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

    public static ElementTheme Load()
    {
        try
        {
            return File.ReadAllText(FilePath).Trim() switch
            {
                nameof(ElementTheme.Light) => ElementTheme.Light,
                nameof(ElementTheme.Dark) => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
        catch (FileNotFoundException) { return ElementTheme.Default; }
        catch (DirectoryNotFoundException) { return ElementTheme.Default; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Could not load the app theme: {error}");
            return ElementTheme.Default;
        }
    }

    public static bool TrySave(ElementTheme theme)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, theme.ToString());
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Could not save the app theme: {error}");
            return false;
        }
    }
}
