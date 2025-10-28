using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MarineNav.Utility.Models;
using MarineNav.Utility.Parsing;

namespace MarineNav.App.ViewModels;

public class ConverterViewModel : INotifyPropertyChanged
{
    private readonly CoordinateFormatter _formatter = new();
    
    private string _latitudeInput = string.Empty;
    private string _longitudeInput = string.Empty;
    private string _latitudeDecimal = string.Empty;
    private string _latitudeDM = string.Empty;
    private string _latitudeDMS = string.Empty;
    private string _longitudeDecimal = string.Empty;
    private string _longitudeDM = string.Empty;
    private string _longitudeDMS = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _hasError;

    public ConverterViewModel()
    {
        ConvertCommand = new RelayCommand(Convert);
        CopyCommand = new RelayCommand<string>(CopyToClipboard);
        SwapLatitudeHemisphereCommand = new RelayCommand(SwapLatitudeHemisphere);
        SwapLongitudeHemisphereCommand = new RelayCommand(SwapLongitudeHemisphere);
    }

    public string LatitudeInput
    {
        get => _latitudeInput;
        set
        {
            _latitudeInput = value;
            OnPropertyChanged();
            ClearError();
        }
    }

    public string LongitudeInput
    {
        get => _longitudeInput;
        set
        {
            _longitudeInput = value;
            OnPropertyChanged();
            ClearError();
        }
    }

    public string LatitudeDecimal
    {
        get => _latitudeDecimal;
        private set { _latitudeDecimal = value; OnPropertyChanged(); }
    }

    public string LatitudeDM
    {
        get => _latitudeDM;
        private set { _latitudeDM = value; OnPropertyChanged(); }
    }

    public string LatitudeDMS
    {
        get => _latitudeDMS;
        private set { _latitudeDMS = value; OnPropertyChanged(); }
    }

    public string LongitudeDecimal
    {
        get => _longitudeDecimal;
        private set { _longitudeDecimal = value; OnPropertyChanged(); }
    }

    public string LongitudeDM
    {
        get => _longitudeDM;
        private set { _longitudeDM = value; OnPropertyChanged(); }
    }

    public string LongitudeDMS
    {
        get => _longitudeDMS;
        private set { _longitudeDMS = value; OnPropertyChanged(); }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set { _errorMessage = value; OnPropertyChanged(); }
    }

    public bool HasError
    {
        get => _hasError;
        private set { _hasError = value; OnPropertyChanged(); }
    }

    public ICommand ConvertCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand SwapLatitudeHemisphereCommand { get; }
    public ICommand SwapLongitudeHemisphereCommand { get; }

    private void Convert()
    {
        try
        {
            if (!CoordinateParser.TryParseCoordinate(LatitudeInput, LongitudeInput, out Coordinate? coord))
            {
                SetError("Invalid coordinate format");
                ClearOutputs();
                return;
            }

            if (coord == null || !coord.IsValid)
            {
                SetError("Coordinate is out of valid range");
                ClearOutputs();
                return;
            }

            // Format in all three formats
            LatitudeDecimal = _formatter.Format(coord.LatitudeDeg, true, CoordinateFormat.DecimalDegrees);
            LatitudeDM = _formatter.Format(coord.LatitudeDeg, true, CoordinateFormat.DegreesDecimalMinutes);
            LatitudeDMS = _formatter.Format(coord.LatitudeDeg, true, CoordinateFormat.DegreesMinutesSeconds);

            LongitudeDecimal = _formatter.Format(coord.LongitudeDeg, false, CoordinateFormat.DecimalDegrees);
            LongitudeDM = _formatter.Format(coord.LongitudeDeg, false, CoordinateFormat.DegreesDecimalMinutes);
            LongitudeDMS = _formatter.Format(coord.LongitudeDeg, false, CoordinateFormat.DegreesMinutesSeconds);

            ClearError();
        }
        catch (Exception ex)
        {
            SetError($"Error: {ex.Message}");
            ClearOutputs();
        }
    }

    private void SwapLatitudeHemisphere()
    {
        try
        {
            double lat = CoordinateParser.Parse(LatitudeInput, true);
            lat = -lat;
            LatitudeInput = _formatter.Format(lat, true, CoordinateFormat.DecimalDegrees);
            Convert();
        }
        catch
        {
            // Ignore errors during swap
        }
    }

    private void SwapLongitudeHemisphere()
    {
        try
        {
            double lon = CoordinateParser.Parse(LongitudeInput, false);
            lon = -lon;
            LongitudeInput = _formatter.Format(lon, false, CoordinateFormat.DecimalDegrees);
            Convert();
        }
        catch
        {
            // Ignore errors during swap
        }
    }

    private void CopyToClipboard(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            Clipboard.SetText(text);
        }
    }

    private void ClearOutputs()
    {
        LatitudeDecimal = string.Empty;
        LatitudeDM = string.Empty;
        LatitudeDMS = string.Empty;
        LongitudeDecimal = string.Empty;
        LongitudeDM = string.Empty;
        LongitudeDMS = string.Empty;
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// Simple RelayCommand implementation
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    
    public void Execute(object? parameter) => _execute();

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
    
    public void Execute(object? parameter) => _execute((T?)parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
