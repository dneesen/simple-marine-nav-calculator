using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using MarineNav.App.Services;
using MarineNav.Utility.Geodesy;
using MarineNav.Utility.Models;
using MarineNav.Utility.Parsing;
using MarineNav.Utility.Services;

namespace MarineNav.App.ViewModels;

/// <summary>
/// ViewModel for the Route Planner tab.
/// </summary>
public class RoutePlannerViewModel : INotifyPropertyChanged
{
    private string _routeName = "New Route";
    private double _speedKnots;
    private DateTime _startTime = DateTime.Now;
    private string _startTimeString = DateTime.Now.ToString("HH:mm");
    private DateTime? _endTime;
    private string _endTimeString = "";
    private bool _useNauticalUnits = true; // true = knots/NM, false = mph/SM
    private string _calculationMode = "Arrival"; // "Arrival", "Departure", or "Speed"
    private string? _errorMessage;
    private bool _use12HourFormat = false;
    private bool _useRhumbLine = false;

    public RoutePlannerViewModel()
    {
        // Load default speed from settings
        _speedKnots = App.Settings.DefaultSpeedKnots;
        _use12HourFormat = App.Settings.Use12HourFormat;
        _useRhumbLine = App.Settings.UseRhumbLine;
        
        Waypoints = new ObservableCollection<WaypointViewModel>();
        Legs = new ObservableCollection<LegViewModel>();
        
        // Initialize with sample waypoints for testing
        AddSampleWaypoints();
        
        AddWaypointCommand = new RelayCommand(AddWaypoint);
        DeleteWaypointCommand = new RelayCommand<WaypointViewModel>(DeleteWaypoint, canExecute: _ => Waypoints.Count > 0);
        MoveWaypointUpCommand = new RelayCommand<WaypointViewModel>(MoveWaypointUp, CanMoveWaypointUp);
        MoveWaypointDownCommand = new RelayCommand<WaypointViewModel>(MoveWaypointDown, CanMoveWaypointDown);
        CalculateRouteCommand = new RelayCommand(CalculateRoute, canExecute: () => Waypoints.Count >= 2);
        ClearRouteCommand = new RelayCommand(ClearRoute, canExecute: () => Waypoints.Count > 0);
        ImportCsvCommand = new RelayCommand(ImportCsv);
        ImportGpxCommand = new RelayCommand(ImportGpx);
        ExportCsvCommand = new RelayCommand(ExportCsv, canExecute: () => Waypoints.Count > 0);
        ExportGpxCommand = new RelayCommand(ExportGpx, canExecute: () => Waypoints.Count > 0);
        PrintCommand = new RelayCommand(Print, canExecute: () => Legs.Count > 0);
        CalculationModeCommand = new RelayCommand<string>(SetCalculationMode);
        RouteTypeCommand = new RelayCommand<string>(SetRouteType);
        
        // Subscribe to waypoint changes
        Waypoints.CollectionChanged += (s, e) => OnPropertyChanged(nameof(TotalWaypoints));
    }

    public ObservableCollection<WaypointViewModel> Waypoints { get; }
    public ObservableCollection<LegViewModel> Legs { get; }

    public string RouteName
    {
        get => _routeName;
        set { _routeName = value; OnPropertyChanged(); }
    }

    public double SpeedKnots
    {
        get => _speedKnots;
        set { _speedKnots = value; OnPropertyChanged(); }
    }

    public DateTime StartTime
    {
        get => _startTime;
        set 
        { 
            _startTime = value;
            OnPropertyChanged();
            
            // Update time string when date changes from DatePicker
            string format = _use12HourFormat ? "h:mm tt" : "HH:mm";
            _startTimeString = _startTime.ToString(format);
            OnPropertyChanged(nameof(StartTimeString));
        }
    }

    public string StartTimeString
    {
        get => _startTimeString;
        set 
        { 
            _startTimeString = value;
            OnPropertyChanged();
            
            // Try to parse the time and update only the time component
            if (TryParseTime(value, out TimeSpan timeSpan))
            {
                // Update only the time part, keep the date from the DatePicker
                var newDateTime = _startTime.Date.Add(timeSpan);
                if (_startTime != newDateTime)
                {
                    _startTime = newDateTime;
                    // Don't call OnPropertyChanged for StartTime to avoid DatePicker update loop
                }
            }
        }
    }

    public DateTime? EndTime
    {
        get => _endTime;
        set 
        { 
            _endTime = value;
            OnPropertyChanged();
            
            // Update time string when date changes from DatePicker
            if (_endTime.HasValue)
            {
                string format = _use12HourFormat ? "h:mm tt" : "HH:mm";
                _endTimeString = _endTime.Value.ToString(format);
                OnPropertyChanged(nameof(EndTimeString));
            }
        }
    }

    public string EndTimeString
    {
        get => _endTimeString;
        set 
        { 
            _endTimeString = value;
            OnPropertyChanged();
            
            // Try to parse the time and update EndTime
            if (!string.IsNullOrWhiteSpace(value) && TryParseTime(value, out TimeSpan timeSpan))
            {
                // Use the EndTime date if it exists, otherwise use StartTime date
                var date = _endTime?.Date ?? _startTime.Date;
                var newDateTime = date.Add(timeSpan);
                
                // Don't automatically adjust for next day here - let the user control this via the DatePicker
                // or let the calculation logic handle it when needed
                
                if (_endTime != newDateTime)
                {
                    _endTime = newDateTime;
                    // Don't call OnPropertyChanged for EndTime to avoid DatePicker update loop
                }
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                _endTime = null;
                OnPropertyChanged(nameof(EndTime));
            }
        }
    }

    public bool UseNauticalUnits
    {
        get => _useNauticalUnits;
        set 
        { 
            _useNauticalUnits = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SpeedLabel));
            OnPropertyChanged(nameof(DisplaySpeed));
            
            // Update leg displays
            foreach (var leg in Legs)
            {
                leg.UseNauticalUnits = value;
            }
        }
    }

    public bool Use12HourFormat
    {
        get => _use12HourFormat;
        set
        {
            _use12HourFormat = value;
            OnPropertyChanged();
            
            // Update time displays
            UpdateTimeStringFormat();
            
            // Save to settings
            App.Settings.Use12HourFormat = value;
            SettingsService.SaveSettings(App.Settings);
        }
    }

    public bool UseRhumbLine
    {
        get => _useRhumbLine;
        set
        {
            _useRhumbLine = value;
            OnPropertyChanged();
            
            // Save to settings
            App.Settings.UseRhumbLine = value;
            SettingsService.SaveSettings(App.Settings);
            
            // Recalculate route if we have waypoints
            if (Waypoints.Count >= 2)
            {
                CalculateRoute();
            }
        }
    }

    public string SpeedLabel => UseNauticalUnits ? "Speed (knots)" : "Speed (mph)";

    public string DisplaySpeed
    {
        get => UseNauticalUnits 
            ? $"{SpeedKnots:F1}" 
            : $"{SpeedKnots * 1.15078:F1}"; // Convert knots to mph
        set
        {
            if (double.TryParse(value, out double speed))
            {
                SpeedKnots = UseNauticalUnits ? speed : speed / 1.15078; // Convert mph to knots
                OnPropertyChanged();
                OnPropertyChanged(nameof(SpeedKnots));
            }
        }
    }

    public string CalculationMode
    {
        get => _calculationMode;
        set
        {
            _calculationMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsArrivalMode));
            OnPropertyChanged(nameof(IsDepartureMode));
            OnPropertyChanged(nameof(IsSpeedMode));
            OnPropertyChanged(nameof(SpeedEnabled));
            OnPropertyChanged(nameof(StartTimeEnabled));
            OnPropertyChanged(nameof(EndTimeEnabled));
        }
    }

    // Mode indicators
    public bool IsArrivalMode => CalculationMode == "Arrival";
    public bool IsDepartureMode => CalculationMode == "Departure";
    public bool IsSpeedMode => CalculationMode == "Speed";

    // Route type indicators
    public bool IsGreatCircleMode => !UseRhumbLine;
    public bool IsRhumbLineMode => UseRhumbLine;

    // Field enable/disable based on mode
    public bool SpeedEnabled => CalculationMode != "Speed";
    public bool StartTimeEnabled => CalculationMode != "Departure";
    public bool EndTimeEnabled => CalculationMode != "Arrival";

    public string ModeDescription
    {
        get
        {
            return CalculationMode switch
            {
                "Arrival" => "Enter your speed and departure time. The system will calculate when you'll arrive at each waypoint.",
                "Departure" => "Enter your speed and desired arrival time. The system will calculate when you need to depart.",
                "Speed" => "Enter your departure and arrival times. The system will calculate the speed needed to arrive on time.",
                _ => ""
            };
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public int TotalWaypoints => Waypoints.Count;

    public ICommand AddWaypointCommand { get; }
    public ICommand DeleteWaypointCommand { get; }
    public ICommand MoveWaypointUpCommand { get; }
    public ICommand MoveWaypointDownCommand { get; }
    public ICommand CalculateRouteCommand { get; }
    public ICommand ClearRouteCommand { get; }
    public ICommand ImportCsvCommand { get; }
    public ICommand ImportGpxCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand ExportGpxCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand CalculationModeCommand { get; }
    public ICommand RouteTypeCommand { get; }

    private void SetCalculationMode(string? mode)
    {
        if (!string.IsNullOrEmpty(mode))
        {
            CalculationMode = mode;
            OnPropertyChanged(nameof(ModeDescription));
        }
    }

    private void SetRouteType(string? routeType)
    {
        if (!string.IsNullOrEmpty(routeType))
        {
            UseRhumbLine = routeType == "RhumbLine";
            OnPropertyChanged(nameof(IsGreatCircleMode));
            OnPropertyChanged(nameof(IsRhumbLineMode));
        }
    }

    private void AddWaypoint()
    {
        var newWaypoint = new WaypointViewModel
        {
            Number = Waypoints.Count + 1,
            Name = $"WPT{Waypoints.Count + 1:D2}",
            LatitudeInput = "0° 00.000′ N",
            LongitudeInput = "0° 00.000′ E"
        };

        Waypoints.Add(newWaypoint);
        ErrorMessage = null;
    }

    private void DeleteWaypoint(WaypointViewModel? waypoint)
    {
        if (waypoint == null) return;
        
        Waypoints.Remove(waypoint);
        RenumberWaypoints();
        ErrorMessage = null;
    }

    private bool CanMoveWaypointUp(WaypointViewModel? waypoint)
    {
        if (waypoint == null) return false;
        int index = Waypoints.IndexOf(waypoint);
        return index > 0;
    }

    private void MoveWaypointUp(WaypointViewModel? waypoint)
    {
        if (waypoint == null) return;
        
        int index = Waypoints.IndexOf(waypoint);
        if (index > 0)
        {
            Waypoints.Move(index, index - 1);
            RenumberWaypoints();
        }
    }

    private bool CanMoveWaypointDown(WaypointViewModel? waypoint)
    {
        if (waypoint == null) return false;
        int index = Waypoints.IndexOf(waypoint);
        return index >= 0 && index < Waypoints.Count - 1;
    }

    private void MoveWaypointDown(WaypointViewModel? waypoint)
    {
        if (waypoint == null) return;
        
        int index = Waypoints.IndexOf(waypoint);
        if (index >= 0 && index < Waypoints.Count - 1)
        {
            Waypoints.Move(index, index + 1);
            RenumberWaypoints();
        }
    }

    private void RenumberWaypoints()
    {
        for (int i = 0; i < Waypoints.Count; i++)
        {
            Waypoints[i].Number = i + 1;
        }
    }

    private void CalculateRoute()
    {
        ErrorMessage = null;
        
        if (Waypoints.Count < 2)
        {
            ErrorMessage = "At least 2 waypoints required";
            return;
        }

        try
        {
            // Parse all waypoints
            var waypointList = new List<Waypoint>();
            
            foreach (var wptVM in Waypoints)
            {
                double lat = CoordinateParser.Parse(wptVM.LatitudeInput, isLatitude: true);
                double lon = CoordinateParser.Parse(wptVM.LongitudeInput, isLatitude: false);

                waypointList.Add(Waypoint.Create(wptVM.Name, new Coordinate(lat, lon)));
            }

            List<Leg> legs;

            switch (CalculationMode)
            {
                case "Arrival":
                    // Mode 1: Calculate arrival time from start time and speed
                    if (SpeedKnots <= 0)
                    {
                        ErrorMessage = "Speed must be greater than 0";
                        return;
                    }
                    
                    legs = RouteCalculationService.CalculateLegs(
                        waypointList,
                        SpeedKnots,
                        new DateTimeOffset(StartTime),
                        _useRhumbLine);
                    break;

                case "Departure":
                    // Mode 2: Calculate departure time from arrival time and speed
                    if (!EndTime.HasValue)
                    {
                        ErrorMessage = "Arrival time is required for Departure mode";
                        return;
                    }
                    if (SpeedKnots <= 0)
                    {
                        ErrorMessage = "Speed must be greater than 0";
                        return;
                    }

                    var totalDistance = CalculateTotalDistance(waypointList);
                    var totalTime = totalDistance / SpeedKnots;
                    var calculatedStartTime = EndTime!.Value.AddHours(-totalTime);
                    
                    StartTime = calculatedStartTime;
                    _startTimeString = StartTime.ToString("HH:mm");
                    OnPropertyChanged(nameof(StartTime));
                    OnPropertyChanged(nameof(StartTimeString));
                    
                    legs = RouteCalculationService.CalculateLegs(
                        waypointList,
                        SpeedKnots,
                        new DateTimeOffset(StartTime),
                        _useRhumbLine);
                    break;

                case "Speed":
                    // Mode 3: Calculate required speed from start and arrival times
                    if (!EndTime.HasValue)
                    {
                        ErrorMessage = "Arrival time is required for Speed mode";
                        return;
                    }

                    var distance = CalculateTotalDistance(waypointList);
                    var timeDiff = EndTime!.Value - StartTime;
                    
                    // If arrival is before departure on the same day, check if user meant next day
                    if (timeDiff.TotalHours <= 0)
                    {
                        // If both dates are the same and time is earlier, this is likely an error
                        if (EndTime.Value.Date == StartTime.Date)
                        {
                            ErrorMessage = "Arrival time is before departure time. If you meant the next day, please adjust the arrival date using the date picker.";
                            return;
                        }
                        else
                        {
                            // Dates are different but still negative (shouldn't happen with proper date selection)
                            ErrorMessage = "Arrival time must be after departure time";
                            return;
                        }
                    }

                    var requiredSpeed = distance / timeDiff.TotalHours;
                    SpeedKnots = requiredSpeed;
                    OnPropertyChanged(nameof(SpeedKnots));
                    OnPropertyChanged(nameof(DisplaySpeed));
                    
                    legs = RouteCalculationService.CalculateLegs(
                        waypointList,
                        requiredSpeed,
                        new DateTimeOffset(StartTime),
                        _useRhumbLine);
                    break;

                default:
                    ErrorMessage = "Invalid calculation mode";
                    return;
            }

            // Update UI
            Legs.Clear();
            int legNumber = 1;
            
            foreach (var leg in legs)
            {
                Legs.Add(new LegViewModel
                {
                    Number = legNumber++,
                    From = leg.From.Name,
                    To = leg.To.Name,
                    DistanceNm = leg.DistanceNm,
                    TrueBearingDeg = leg.TrueBearingDeg,
                    Cardinal = leg.Cardinal16,
                    VariationDeg = leg.VariationDeg,
                    MagneticCourseDeg = leg.MagneticCourseDeg,
                    LegTime = leg.LegTime,
                    ETA = leg.ETA,
                    UseNauticalUnits = UseNauticalUnits
                });
            }

            OnPropertyChanged(nameof(TotalDistance));
            OnPropertyChanged(nameof(TotalTime));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error calculating route: {ex.Message}";
        }
    }

    private double CalculateTotalDistance(List<Waypoint> waypoints)
    {
        double total = 0;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            var result = _useRhumbLine
                ? RhumbLineCalculator.CalculateRhumbLine(waypoints[i].Coord, waypoints[i + 1].Coord)
                : GeodesicCalculator.CalculateInverse(waypoints[i].Coord, waypoints[i + 1].Coord);
            total += result.DistanceMeters / 1852.0; // Convert meters to nautical miles
        }
        return total;
    }

    private void ClearRoute()
    {
        Waypoints.Clear();
        Legs.Clear();
        ErrorMessage = null;
    }

    private void ImportCsv()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "GPX Files (*.gpx)|*.gpx|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = "Import Waypoints from CSV"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var waypoints = MarineNav.Utility.IO.CsvService.ImportWaypoints(dialog.FileName);
                LoadWaypoints(waypoints);
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Import failed: {ex.Message}";
            }
        }
    }

    private void ImportGpx()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "GPX Files (*.gpx)|*.gpx|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = "Import Waypoints from GPX"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var waypoints = MarineNav.Utility.IO.GpxService.ImportWaypoints(dialog.FileName);
                LoadWaypoints(waypoints);
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Import failed: {ex.Message}";
            }
        }
    }

    private void LoadWaypoints(List<Waypoint> waypoints)
    {
        Waypoints.Clear();
        Legs.Clear();

        var formatter = new CoordinateFormatter();
        int number = 1;
        foreach (var wpt in waypoints)
        {
            Waypoints.Add(new WaypointViewModel
            {
                Number = number++,
                Name = wpt.Name,
                LatitudeInput = formatter.Format(wpt.Coord.LatitudeDeg, true, CoordinateFormat.DegreesDecimalMinutes),
                LongitudeInput = formatter.Format(wpt.Coord.LongitudeDeg, false, CoordinateFormat.DegreesDecimalMinutes)
            });
        }
    }

    private void ExportCsv()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "GPX Files (*.gpx)|*.gpx|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = "Export Route to CSV",
            FileName = $"{RouteName}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var waypoints = GetWaypointsFromViewModel();
                
                if (Legs.Count > 0)
                {
                    // Export full route with legs
                    var route = RouteCalculationService.CreateRoute(RouteName, waypoints, SpeedKnots, new DateTimeOffset(StartTime));
                    MarineNav.Utility.IO.CsvService.ExportRoute(dialog.FileName, route);
                }
                else
                {
                    // Export just waypoints
                    MarineNav.Utility.IO.CsvService.ExportWaypoints(dialog.FileName, waypoints);
                }
                
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Export failed: {ex.Message}";
            }
        }
    }

    private void ExportGpx()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "GPX Files (*.gpx)|*.gpx|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = "Export Route to GPX",
            FileName = $"{RouteName}.gpx"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var waypoints = GetWaypointsFromViewModel();
                MarineNav.Utility.IO.GpxService.ExportWaypoints(dialog.FileName, waypoints, RouteName);
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Export failed: {ex.Message}";
            }
        }
    }

    private List<Waypoint> GetWaypointsFromViewModel()
    {
        var waypoints = new List<Waypoint>();
        
        foreach (var wptVM in Waypoints)
        {
            try
            {
                double lat = CoordinateParser.Parse(wptVM.LatitudeInput, isLatitude: true);
                double lon = CoordinateParser.Parse(wptVM.LongitudeInput, isLatitude: false);
                waypoints.Add(Waypoint.Create(wptVM.Name, new Coordinate(lat, lon)));
            }
            catch
            {
                // Skip invalid waypoints
            }
        }

        return waypoints;
    }

    private void Print()
    {
        try
        {
            // Build the route from current waypoints and legs
            var waypoints = GetWaypointsFromViewModel();
            if (waypoints.Count < 2)
            {
                ErrorMessage = "Need at least 2 waypoints to print a route.";
                return;
            }

            // Create legs from LegViewModel collection
            var legs = new List<Leg>();
            foreach (var legVm in Legs)
            {
                var fromWaypoint = waypoints.FirstOrDefault(w => w.Name == legVm.From);
                var toWaypoint = waypoints.FirstOrDefault(w => w.Name == legVm.To);
                
                if (fromWaypoint != null && toWaypoint != null)
                {
                    legs.Add(new Leg
                    {
                        From = fromWaypoint,
                        To = toWaypoint,
                        DistanceNm = legVm.DistanceNm,
                        TrueBearingDeg = legVm.TrueBearingDeg,
                        Cardinal16 = legVm.Cardinal,
                        VariationDeg = legVm.VariationDeg,
                        MagneticCourseDeg = legVm.MagneticCourseDeg,
                        LegTime = legVm.LegTime,
                        ETA = legVm.ETA
                    });
                }
            }

            var route = new Route
            {
                Name = RouteName,
                Waypoints = waypoints,
                Legs = legs
            };

            // Generate the FlowDocument
            var document = PrintService.CreateRouteDocument(route, RouteName);

            // Create a print dialog
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Print the document
                var paginator = ((System.Windows.Documents.IDocumentPaginatorSource)document).DocumentPaginator;
                printDialog.PrintDocument(paginator, $"Route: {RouteName}");
                
                ErrorMessage = null; // Clear any previous error
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Print failed: {ex.Message}";
        }
    }

    private void AddSampleWaypoints()
    {
        // Add sample waypoints for testing (can be removed later)
        Waypoints.Add(new WaypointViewModel
        {
            Number = 1,
            Name = "Start",
            LatitudeInput = "40° 42.0′ N",
            LongitudeInput = "74° 00.0′ W"
        });

        Waypoints.Add(new WaypointViewModel
        {
            Number = 2,
            Name = "End",
            LatitudeInput = "51° 30.0′ N",
            LongitudeInput = "0° 06.0′ W"
        });
    }

    public double TotalDistance => Legs.Sum(l => l.DistanceNm);
    
    public string TotalTime
    {
        get
        {
            var totalTime = TimeSpan.FromTicks(Legs.Sum(l => l.LegTime?.Ticks ?? 0));
            return totalTime.TotalHours > 0 
                ? $"{(int)totalTime.TotalHours}h {totalTime.Minutes}m"
                : "-";
        }
    }

    /// <summary>
    /// Updates the time string format when switching between 12h and 24h formats.
    /// </summary>
    private void UpdateTimeStringFormat()
    {
        string format = _use12HourFormat ? "h:mm tt" : "HH:mm";
        _startTimeString = _startTime.ToString(format);
        OnPropertyChanged(nameof(StartTimeString));
        
        if (_endTime.HasValue)
        {
            _endTimeString = _endTime.Value.ToString(format);
            OnPropertyChanged(nameof(EndTimeString));
        }
    }

    /// <summary>
    /// Tries to parse a time string in either 12-hour or 24-hour format.
    /// Supports formats: "HH:mm", "H:mm", "h:mm tt", "h:mm TT", "hh:mm tt", etc.
    /// Also supports 4-digit format: "1645" = 16:45, "1670" = 17:10 (auto-corrects minutes >= 60)
    /// </summary>
    private bool TryParseTime(string timeString, out TimeSpan timeSpan)
    {
        timeSpan = TimeSpan.Zero;
        
        if (string.IsNullOrWhiteSpace(timeString))
            return false;

        // Try parsing 4-digit format first (e.g., "1645" -> 16:45)
        if (timeString.Length == 4 && int.TryParse(timeString, out int fourDigitTime))
        {
            int hours = fourDigitTime / 100;
            int minutes = fourDigitTime % 100;
            
            // Handle invalid minutes (e.g., 1670 -> 17:10)
            if (minutes >= 60)
            {
                int extraHours = minutes / 60;
                hours += extraHours;
                minutes = minutes % 60;
            }
            
            // Validate hours
            if (hours >= 0 && hours < 24)
            {
                timeSpan = new TimeSpan(hours, minutes, 0);
                return true;
            }
        }

        // Try standard TimeSpan.TryParse (handles "HH:mm" format)
        if (TimeSpan.TryParse(timeString, out timeSpan))
            return true;

        // Try parsing as 12-hour format with AM/PM
        string[] formats = { "h:mm tt", "hh:mm tt", "h:mmtt", "hh:mmtt", "h:mm TT", "hh:mm TT" };
        if (DateTime.TryParseExact(timeString, formats, null, System.Globalization.DateTimeStyles.None, out DateTime dt))
        {
            timeSpan = dt.TimeOfDay;
            return true;
        }

        return false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// ViewModel for a single waypoint in the route.
/// </summary>
public class WaypointViewModel : INotifyPropertyChanged
{
    private int _number;
    private string _name = string.Empty;
    private string _latitudeInput = string.Empty;
    private string _longitudeInput = string.Empty;

    public int Number
    {
        get => _number;
        set { _number = value; OnPropertyChanged(); }
    }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string LatitudeInput
    {
        get => _latitudeInput;
        set { _latitudeInput = value; OnPropertyChanged(); }
    }

    public string LongitudeInput
    {
        get => _longitudeInput;
        set { _longitudeInput = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// ViewModel for a calculated leg in the route.
/// </summary>
public class LegViewModel : INotifyPropertyChanged
{
    private bool _useNauticalUnits = true;

    public int Number { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public double DistanceNm { get; set; }
    public double TrueBearingDeg { get; set; }
    public string Cardinal { get; set; } = string.Empty;
    public double? VariationDeg { get; set; }
    public double? MagneticCourseDeg { get; set; }
    public TimeSpan? LegTime { get; set; }
    public DateTimeOffset? ETA { get; set; }

    public bool UseNauticalUnits
    {
        get => _useNauticalUnits;
        set
        {
            _useNauticalUnits = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayDistance));
        }
    }

    public string DisplayDistance => UseNauticalUnits 
        ? $"{DistanceNm:F1} NM" 
        : $"{DistanceNm * 1.15078:F1} SM"; // Convert NM to statute miles
    
    public string DisplayTrueBearing => $"{TrueBearingDeg:000}°T";
    
    public string DisplayMagneticBearing => MagneticCourseDeg.HasValue
        ? $"{MagneticCourseDeg.Value:000}°M"
        : "-";
    
    public string DisplayVariation => VariationDeg.HasValue
        ? $"{Math.Abs(VariationDeg.Value):F1}°{(VariationDeg.Value >= 0 ? "E" : "W")}"
        : "-";
    
    public string DisplayTime => LegTime.HasValue
        ? $"{(int)LegTime.Value.TotalHours}h {LegTime.Value.Minutes}m"
        : "-";
    
    public string DisplayETA => ETA.HasValue
        ? ETA.Value.ToString("ddd HH:mm")
        : "-";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
