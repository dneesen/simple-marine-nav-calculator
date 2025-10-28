using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarineNav.App.Services;

/// <summary>
/// Service for persisting and loading application settings.
/// </summary>
public class SettingsService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MarineNavigationUtility"
    );
    
    private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.json");

    /// <summary>
    /// Application settings.
    /// </summary>
    public class AppSettings
    {
        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "Light";

        [JsonPropertyName("defaultSpeedKnots")]
        public double DefaultSpeedKnots { get; set; } = 6.0;

        [JsonPropertyName("coordinatePrecision")]
        public int CoordinatePrecision { get; set; } = 2; // Decimal places for seconds

        [JsonPropertyName("lastImportDirectory")]
        public string? LastImportDirectory { get; set; }

        [JsonPropertyName("lastExportDirectory")]
        public string? LastExportDirectory { get; set; }

        [JsonPropertyName("windowWidth")]
        public double WindowWidth { get; set; } = 1200;

        [JsonPropertyName("windowHeight")]
        public double WindowHeight { get; set; } = 800;

        [JsonPropertyName("windowMaximized")]
        public bool WindowMaximized { get; set; } = false;

        [JsonPropertyName("use12HourFormat")]
        public bool Use12HourFormat { get; set; } = false;

        [JsonPropertyName("useRhumbLine")]
        public bool UseRhumbLine { get; set; } = false;
    }

    /// <summary>
    /// Loads settings from disk. Returns default settings if file doesn't exist.
    /// </summary>
    public static AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load settings: {ex.Message}");
            return new AppSettings();
        }
    }

    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    public static void SaveSettings(AppSettings settings)
    {
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(AppDataFolder);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the settings file path for display purposes.
    /// </summary>
    public static string GetSettingsFilePath() => SettingsFilePath;
}
