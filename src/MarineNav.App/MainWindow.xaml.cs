using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MarineNav.App.Services;
using MarineNav.App.ViewModels;

namespace MarineNav.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Apply saved window size
        var settings = App.Settings;
        if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
        {
            Width = settings.WindowWidth;
            Height = settings.WindowHeight;
            
            if (settings.WindowMaximized)
            {
                WindowState = WindowState.Maximized;
            }
        }
        
        // Save window size on close
        Closing += (s, e) =>
        {
            App.Settings.WindowWidth = ActualWidth;
            App.Settings.WindowHeight = ActualHeight;
            App.Settings.WindowMaximized = WindowState == WindowState.Maximized;
        };
    }

    private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        ThemeManager.ToggleTheme();
        App.Settings.Theme = ThemeManager.CurrentTheme.ToString();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void NewRoute_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        // Switch to Route Planner tab and clear route
        var tabControl = FindVisualChild<TabControl>(this);
        if (tabControl != null)
        {
            tabControl.SelectedIndex = 1;
        }
        GetRoutePlannerViewModel()?.ClearRouteCommand?.Execute(null);
    }

    private void ImportCsv_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var tabControl = FindVisualChild<TabControl>(this);
        if (tabControl != null)
        {
            tabControl.SelectedIndex = 1;
        }
        GetRoutePlannerViewModel()?.ImportCsvCommand?.Execute(null);
    }

    private void ImportGpx_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var tabControl = FindVisualChild<TabControl>(this);
        if (tabControl != null)
        {
            tabControl.SelectedIndex = 1;
        }
        GetRoutePlannerViewModel()?.ImportGpxCommand?.Execute(null);
    }

    private void ExportCsv_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        GetRoutePlannerViewModel()?.ExportCsvCommand?.Execute(null);
    }

    private void ExportGpx_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        GetRoutePlannerViewModel()?.ExportGpxCommand?.Execute(null);
    }

    private void Print_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        GetRoutePlannerViewModel()?.PrintCommand?.Execute(null);
    }

    private RoutePlannerViewModel? GetRoutePlannerViewModel()
    {
        // Find the Route Planner view and get its DataContext
        var routePlannerView = FindVisualChild<UserControl>(this);
        if (routePlannerView?.GetType().Name == "RoutePlannerView")
        {
            return routePlannerView.DataContext as RoutePlannerViewModel;
        }
        return null;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            
            if (child is T typedChild)
            {
                return typedChild;
            }
            
            var result = FindVisualChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }
}

/// <summary>
/// Converter to show/hide UI elements based on null values.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}