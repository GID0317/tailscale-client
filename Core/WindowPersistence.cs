using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using WinUIEx;

namespace TailscaleClient.Core;

internal static class WindowPersistence
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TailscaleClient", "window-placement.json");

    public static void Initialize()
    {
        var values = new Dictionary<string, object>();
        try
        {
            if (File.Exists(FilePath))
            {
                var saved = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FilePath));
                foreach (var entry in saved ?? [])
                    values[entry.Key] = entry.Value;
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            Debug.WriteLine($"Could not load window placement: {error}");
        }
        WindowManager.PersistenceStorage = values;
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            var saved = WindowManager.PersistenceStorage.ToDictionary(entry => entry.Key, entry => (string)entry.Value);
            File.WriteAllText(FilePath + ".tmp", JsonSerializer.Serialize(saved));
            File.Move(FilePath + ".tmp", FilePath, true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Could not save window placement: {error}");
        }
    }
}
