using System.Configuration;
using System.Data;
using System.Windows;
using MarineNav.App.Services;

namespace MarineNav.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static SettingsService.AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load settings
        Settings = SettingsService.LoadSettings();

        // Apply saved theme
        var theme = ThemeManager.ParseTheme(Settings.Theme);
        ThemeManager.ApplyTheme(theme);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Save settings on exit
        SettingsService.SaveSettings(Settings);
        base.OnExit(e);
    }
}

