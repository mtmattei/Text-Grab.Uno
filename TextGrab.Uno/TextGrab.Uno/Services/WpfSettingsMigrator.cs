#if WINDOWS
using System.Xml.Linq;
using Uno.Extensions.Configuration;

namespace TextGrab.Services;

/// <summary>
/// One-time migration of WPF Properties.Settings to Uno IWritableOptions.
/// Detects existing WPF user.config and imports values into appsettings.json.
/// </summary>
public static class WpfSettingsMigrator
{
    private const string MigrationDoneKey = "WpfSettingsMigrated";

    public static async Task MigrateIfNeededAsync(IWritableOptions<AppSettings> writableSettings, IOptions<AppSettings> currentSettings)
    {
        // Skip if already migrated or not first run
        if (currentSettings.Value?.FirstRun == false)
            return;

        // Look for WPF user.config
        var wpfConfigPath = FindWpfUserConfig();
        if (wpfConfigPath is null)
            return;

        try
        {
            var wpfSettings = ParseWpfConfig(wpfConfigPath);
            if (wpfSettings.Count == 0) return;

            await writableSettings.UpdateAsync(current => ApplyWpfSettings(current, wpfSettings));
        }
        catch
        {
            // Migration is best-effort — don't crash the app
        }
    }

    private static string? FindWpfUserConfig()
    {
        // WPF stores user settings in AppData\Local\<Company>\<App>\<Version>\user.config
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var searchPaths = new[]
        {
            Path.Combine(localAppData, "Text-Grab"),
            Path.Combine(localAppData, "Text_Grab"),
            Path.Combine(localAppData, "JoeFinApps"),
        };

        foreach (var basePath in searchPaths)
        {
            if (!Directory.Exists(basePath)) continue;

            // Search recursively for user.config
            var configs = Directory.GetFiles(basePath, "user.config", SearchOption.AllDirectories);
            if (configs.Length > 0)
            {
                // Return the most recently modified one
                return configs.OrderByDescending(File.GetLastWriteTime).First();
            }
        }

        return null;
    }

    private static Dictionary<string, string> ParseWpfConfig(string path)
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var doc = XDocument.Load(path);
            var settingElements = doc.Descendants("setting");

            foreach (var element in settingElements)
            {
                var name = element.Attribute("name")?.Value;
                var value = element.Element("value")?.Value;
                if (name is not null && value is not null)
                {
                    settings[name] = value;
                }
            }
        }
        catch
        {
            // Parse failure — return empty
        }

        return settings;
    }

    private static AppSettings ApplyWpfSettings(AppSettings current, Dictionary<string, string> wpf)
    {
        return current with
        {
            DefaultLaunch = wpf.GetValueOrDefault("DefaultLaunch", current.DefaultLaunch),
            AppTheme = wpf.GetValueOrDefault("AppTheme", current.AppTheme),
            ShowToast = GetBool(wpf, "ShowToast", current.ShowToast),
            RunInTheBackground = GetBool(wpf, "RunInTheBackground", current.RunInTheBackground),
            StartupOnLogin = GetBool(wpf, "StartupOnLogin", current.StartupOnLogin),
            ReadBarcodesOnGrab = GetBool(wpf, "TryToReadBarcodes", current.ReadBarcodesOnGrab),
            CorrectErrors = GetBool(wpf, "CorrectErrors", current.CorrectErrors),
            CorrectToLatin = GetBool(wpf, "CorrectToLatin", current.CorrectToLatin),
            NeverAutoUseClipboard = GetBool(wpf, "NeverAutoUseClipboard", current.NeverAutoUseClipboard),
            TryInsert = GetBool(wpf, "TryInsert", current.TryInsert),
            InsertDelay = GetDouble(wpf, "InsertDelay", current.InsertDelay),
            UseTesseract = GetBool(wpf, "UseTesseract", current.UseTesseract),
            TesseractPath = wpf.GetValueOrDefault("TesseractPath", current.TesseractPath),
            EditWindowIsWordWrapOn = GetBool(wpf, "EditWindowIsWordWrapOn", current.EditWindowIsWordWrapOn),
            FsgShadeOverlay = GetBool(wpf, "FsgShadeOverlay", current.FsgShadeOverlay),
            FsgSendEtwToggle = GetBool(wpf, "FsgSendEtwToggle", current.FsgSendEtwToggle),
            WebSearchUrl = wpf.GetValueOrDefault("WebSearchUrl", current.WebSearchUrl),
        };
    }

    private static bool GetBool(Dictionary<string, string> d, string key, bool fallback)
        => d.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : fallback;

    private static double GetDouble(Dictionary<string, string> d, string key, double fallback)
        => d.TryGetValue(key, out var v) && double.TryParse(v, out var n) ? n : fallback;
}
#endif
