# 🧭 Simple Marine Navigation Calculator

A lightweight, offline Windows desktop application for marine navigation calculations and route planning.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=.net)
![WPF](https://img.shields.io/badge/WPF-Windows-0078D4?logo=windows)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

### 📍 Coordinate Conversion
- **Multiple Input Formats**: 
  - DDD° MM′ SS.S″ (Degrees, Minutes, Seconds)
  - DDD° MM.MMM′ (Degrees, Decimal Minutes)
  - DDD.DDDD° (Decimal Degrees)
- **Auto-Format Detection**: Paste coordinates in any format and get instant conversion
- **Hemisphere Validation**: Automatic validation for latitude (N/S) and longitude (E/W)
- **Batch Conversion**: Convert multiple coordinates at once

### 🗺️ Route Planning
- **Multi-Waypoint Routes**: Create routes with unlimited waypoints
- **Dual Calculation Methods**:
  - **Great Circle**: Shortest distance between points (geodesic)
  - **Rhumb Line**: Constant bearing navigation
- **Comprehensive Leg Information**:
  - Distance (Nautical Miles or Statute Miles)
  - True bearing and 16-point cardinal direction
  - Magnetic course with variation calculation
  - Estimated time and arrival for each leg
  
### ⏱️ Time and Speed Calculations
Calculate any of three variables given the other two:
- **Arrival Time Mode**: Calculate ETA from departure time and speed
- **Departure Time Mode**: Calculate required departure from arrival time and speed
- **Required Speed Mode**: Calculate speed needed from departure and arrival times

### 🧲 Magnetic Variation
- **Automatic Calculation**: Uses WMM2020 (World Magnetic Model)
- **Real-Time Updates**: Variation calculated for current date
- **Confidence Indicators**: Shows reliability of magnetic calculations

### 📁 Import/Export
- **CSV Support**: Import/export waypoints and routes
- **GPX Format**: Compatible with GPS devices and mapping software
- **Print Functionality**: Generate professional route summaries

### 🎨 User Interface
- **Light/Dark Themes**: Switch between themes for comfortable viewing
- **Keyboard Friendly**: Full keyboard navigation support
- **Unit Toggle**: Quick switch between nautical (knots/NM) and statute (mph/SM) units
- **12/24 Hour Format**: Flexible time display options

## Installation

### Requirements
- Windows 10 or Windows 11
- No additional software required (self-contained)

### Download & Run
1. Download the latest release from the [Releases](https://github.com/dneesen/simple-marine-nav-calculator/releases) page
2. Choose the version for your system:
   - `win-x64` - 64-bit Windows (most common)
   - `win-x86` - 32-bit Windows
   - `win-arm64` - ARM64 Windows (Surface Pro X, etc.)
3. Extract the ZIP file
4. Run `MarineNav.App.exe`

No installation required - just run the executable!

## Usage

### Coordinate Converter
1. Navigate to the **Converter** tab
2. Enter coordinates in any supported format
3. View instant conversion to all three formats
4. Copy results to clipboard or use in route planning

### Route Planner
1. Navigate to the **Route Planner** tab
2. Add waypoints by entering coordinates
3. Choose calculation mode (Arrival Time, Departure Time, or Required Speed)
4. Select route type (Great Circle or Rhumb Line)
5. Enter known values (speed, departure/arrival times)
6. Click **Calculate Route** to see detailed leg information
7. Export to CSV/GPX or print the route summary

## Technology

- **Framework**: .NET 8 with WPF
- **Architecture**: MVVM pattern with clean separation
- **Geodesy**: Custom geodesic and rhumb line calculations
- **Magnetic Model**: WMM2020 integration
- **Testing**: Comprehensive unit test coverage with xUnit

## Project Structure

```
src/
├── MarineNav.App/          # WPF Application
│   ├── ViewModels/         # MVVM ViewModels
│   ├── Views/              # XAML Views
│   ├── Services/           # App Services
│   └── Themes/             # Light/Dark themes
├── MarineNav.Utility/      # Core Business Logic
│   ├── Geodesy/            # Navigation calculations
│   ├── MagVar/             # Magnetic variation
│   ├── Parsing/            # Coordinate parsing
│   └── Services/           # Route calculations
└── MarineNav.Tests/        # Unit Tests
```

## Building from Source

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11

### Build Commands
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run application
dotnet run --project src/MarineNav.App/MarineNav.App.csproj

# Run tests
dotnet test

# Publish (self-contained)
dotnet publish src/MarineNav.App/MarineNav.App.csproj -c Release -r win-x64 --self-contained -o publish/win-x64 -p:PublishSingleFile=true
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- **World Magnetic Model**: Uses WMM2020 from NOAA/NCEI
- **Geodesic Algorithms**: Based on Vincenty's formulae and Karney's geodesic calculations
- **Marine Navigation Standards**: Follows standard nautical conventions and practices

## Support

For issues, questions, or contributions, please visit the [GitHub Issues](https://github.com/dneesen/simple-marine-nav-calculator/issues) page.

---

**Note**: This application is designed for navigation planning and education. Always use official nautical charts and navigation equipment for actual marine navigation.
