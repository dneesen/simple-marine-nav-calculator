using System.Windows;

namespace MarineNav.App.Services;

/// <summary>
/// Service for managing application themes (Light/Dark).
/// </summary>
public class ThemeManager
{
    private const string LightThemeUri = "Themes/LightTheme.xaml";
    private const string DarkThemeUri = "Themes/DarkTheme.xaml";

    /// <summary>
    /// Available theme options.
    /// </summary>
    public enum Theme
    {
        Light,
        Dark
    }

    /// <summary>
    /// Gets the currently active theme.
    /// </summary>
    public static Theme CurrentTheme { get; private set; } = Theme.Light;

    /// <summary>
    /// Applies the specified theme to the application.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    public static void ApplyTheme(Theme theme)
    {
        CurrentTheme = theme;
        
        var themeUri = theme == Theme.Light ? LightThemeUri : DarkThemeUri;
        
        // Remove existing theme dictionaries
        var existingTheme = Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString?.Contains("Theme.xaml") == true);
        
        if (existingTheme != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(existingTheme);
        }
        
        // Add the new theme dictionary
        var newTheme = new ResourceDictionary
        {
            Source = new Uri(themeUri, UriKind.Relative)
        };
        
        Application.Current.Resources.MergedDictionaries.Add(newTheme);
    }

    /// <summary>
    /// Toggles between light and dark themes.
    /// </summary>
    public static void ToggleTheme()
    {
        ApplyTheme(CurrentTheme == Theme.Light ? Theme.Dark : Theme.Light);
    }

    /// <summary>
    /// Gets the theme enum from a string.
    /// </summary>
    public static Theme ParseTheme(string themeName)
    {
        return Enum.TryParse<Theme>(themeName, ignoreCase: true, out var theme) 
            ? theme 
            : Theme.Light;
    }
}
