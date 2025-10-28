using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MarineNav.App.ViewModels;

namespace MarineNav.App.Views;

public partial class RoutePlannerView
{
    public RoutePlannerView()
    {
        InitializeComponent();
        DataContext = new RoutePlannerViewModel();
    }
}

/// <summary>
/// Converter to map bool to ComboBox index (0 = true/Nautical, 1 = false/Statute).
/// </summary>
public class BoolToIndexConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && b ? 0 : 1;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int index && index == 0;
    }
}

/// <summary>
/// Converter to change background color based on enabled state.
/// </summary>
public class BoolToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool enabled)
        {
            if (enabled)
            {
                // Try to get the ColorSurface brush from resources, fallback to White
                if (Application.Current.Resources["ColorSurface"] is SolidColorBrush surfaceBrush)
                {
                    return surfaceBrush;
                }
                return Brushes.White;
            }
            else
            {
                // Try to get the ColorSecondaryBackground for disabled state
                if (Application.Current.Resources["ColorSecondaryBackground"] is SolidColorBrush disabledBrush)
                {
                    return disabledBrush;
                }
                return new SolidColorBrush(Color.FromRgb(240, 240, 240));
            }
        }
        
        // Default to ColorSurface or White
        if (Application.Current.Resources["ColorSurface"] is SolidColorBrush defaultBrush)
        {
            return defaultBrush;
        }
        return Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
