# 🧭 Marine Navigation Utility  
**Development Specification Document**  
*Version 1.0 — Final*  

---

## 1. Project Overview

A lightweight, offline Windows desktop application for marine navigation calculations.  

### Core Capabilities
- Accept latitude/longitude in any of three formats (DDD° MM′ SS.S″, DDD° MM.MMM′, DDD.DDDD°).  
- Auto-convert among formats and validate hemispheres.  
- Create routes consisting of waypoints; compute:  
  - Distance (NM) between waypoints  
  - Initial true course and 16-point cardinal direction  
  - Magnetic course (when confidence high)  
  - Time per leg and cumulative ETA based on vessel speed  
- Operate fully offline; import/export routes and print summaries.  
- Clean, keyboard-friendly UI with light/dark themes.  

---

## 2. Non-Functional Requirements

| Parameter | Specification |
|------------|----------------|
| OS | Windows 10/11, x64 |
| Runtime | .NET 8 (WPF) |
| Packaging | Single-file EXE + WiX installer |
| Performance | < 10 ms/leg |
| Offline | Full functionality |
| Precision | NM ± 0.01, Bearing ± 0.1°, ETA ± 1 min |

---

## 3. Technology Stack

- **Language:** C# 10+  
- **UI:** WPF (XAML)  
- **Installer:** WiX Toolset (bootstrapper EXE)  
- **Unit Tests:** xUnit + FluentAssertions  
- **Version Control:** Git + GitHub Actions CI  
- **Publish Command:**  
  ```bash
  dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true
  ```

---

## 4. Core Algorithms

### 4.1 Geodesic Computations
- **Ellipsoid:** WGS84  
- **Method:** Vincenty Inverse  
- **Fallback:** Haversine if non-convergent  
- **Outputs:**  
  - Distance (NM = m/1852)  
  - True initial course (0–360°)  
  - 16-point cardinal direction  

### 4.2 Magnetic Variation
- Embedded **WMM2025** coefficients.  
- Input: lat, lon, alt = 0, UTC date.  
- Confidence = high if |lat| ≤ 80° and model within valid range.  
- Magnetic course = True − Declination (normalized 0–360°).  
- Display both: e.g., **045.1° T / 041.8° M**.

---

## 5. Coordinate Parsing & Formatting

### Accepted Inputs
```
43.1234N
-87.9876
43 07.404 N
087° 54′ 36.5″ W
43 7 24.2
```
- Hemispheres: N/S/E/W or sign (±)  
- Delimiters: whitespace, °, ′, ″, commas  
- Auto-segment:  
  - 1 token → DDD.DDDD  
  - 2 tokens → DDD MM.MMM  
  - 3 tokens → DDD MM SS.S  

### Default Output Precision
| Format | Precision |
|---------|------------|
| DDD.DDDD° | 4 decimals |
| DDD° MM.MMM′ | 3 decimals |
| DDD° MM′ SS.S″ | 1 decimal |

---

## 6. Data Model

```csharp
record Coordinate(double LatitudeDeg, double LongitudeDeg);

record Waypoint(Guid Id, string Name, Coordinate Coord);

record Leg
{
    Waypoint From, To;
    double DistanceNm;
    double TrueBearingDeg;
    string Cardinal16;
    double? VariationDeg;
    double? MagneticCourseDeg;
    TimeSpan? LegTime;
    DateTimeOffset? ETA;
}

record Route
{
    string Name;
    List<Waypoint> Waypoints;
    List<Leg> Legs;
    double TotalDistanceNm;
    TimeSpan? TotalTime;
    DateTimeOffset? FinalETA;
}
```

---

## 7. Speed / Time / ETA

- **Speed:** global knots value  
- **Time Zone:** default = system local (user-selectable)  
- **Start Time:** datetime picker  
- **ETA Rounding:** nearest minute  
- ETAs computed cumulatively per leg.

---

## 8. UI / Workflow

### Layout
Single window with tabbed layout:

#### 🔹 Converter Tab
- Inputs: Latitude, Longitude (any format)  
- Outputs: all three formats  
- Copy buttons & hemisphere swap  

#### 🔹 Route Planner Tab
**Waypoints Grid**
| Name | Latitude | Longitude | Parsed | Display Fmt |
|------|-----------|------------|---------|-------------|

- Add/Delete/Reorder (+ / Del / Alt ↑↓)  
- Paste CSV/Clipboard coords  

**Route Settings**
- Speed (knots)  |  Start time  |  Time zone  |  Magnetic toggle  

**Legs Table**
| From→To | NM | True ° | Card | Magnetic ° | Time | ETA |
|----------|----|--------|------|-------------|------|-----|
| Shows both true/magnetic when available |

**Next Waypoint Panel**
- Enter current coords → distance, bearing, ETA to next  

**Actions**
- Import/Export (CSV, GPX)  
- Print Summary  
- Clear Route  

### Keyboard Shortcuts
| Action | Keys |
|--------|------|
| New Route | Ctrl + N |
| Import | Ctrl + O |
| Export | Ctrl + S |
| Print | Ctrl + P |
| Add WP | Ctrl + I |
| Delete WP | Del |
| Move Up/Down | Alt ↑/↓ |
| Swap Hemisphere | F10 |

### Themes & Accessibility
- Light/Dark switch  
- Adjustable font sizes  
- Validation tooltips + status bar  

---

## 9. File I/O

### CSV
**Import** columns: `Name, Latitude, Longitude`  
**Export** columns: all three format variants per coordinate  

### GPX 1.1
Supports both `<wpt>` and `<rtept>` elements.  

### Persistence
`%AppData%\\MarineNavUtility\\settings.json`  
(stores theme, precision, speed, timezone, last route path, window geometry)

### Print
- WPF FlowDocument  
- Header: Route name, date, speed, start time, timezone, mag model status  
- Tables: Waypoints & Legs  
- Footer: page # + disclaimer  

---

## 10. Validation & Error Handling

- Live per-cell validation; red outline + tooltip  
- Magnetic toggle auto-disabled with reason (out of range, stale model)  
- Vincenty non-convergence → fallback + “reduced confidence” indicator  
- Import partial success summary  

---

## 11. Computation Reference

### WGS84
```
a = 6378137.0 m
f = 1/298.257223563
b = a (1−f)
```

### Distance and Bearing
Vincenty Inverse with Haversine fallback.  

### Cardinal Mapping
```
idx = floor((bearing + 11.25) / 22.5) % 16
```

### ETA
```
LegTime = DistanceNm / SpeedKts
ETA = Start + Σ(LegTimes)
```

---

## 12. Settings

| Setting | Default | Options |
|----------|----------|----------|
| Output Precision | 4/3/1 | adjustable |
| Time Zone | System local | selectable |
| Magnetic Show | Enabled if valid | toggle |
| Theme | Light | Dark |
| Units | Nautical Miles | future: SM/km |

---

## 13. Acceptance Criteria

1. Coordinate parser accepts all input styles within 1e-6 deg.  
2. Format converter round-trip accuracy within precision.  
3. Route legs auto-compute distance, bearing, cardinal, magnetic, ETA.  
4. Magnetic data appears only within model bounds.  
5. ETAs rounded to nearest minute.  
6. Next-waypoint calc returns correct NM/bearing/ETA.  
7. CSV/GPX import/export preserves order and names.  
8. Printable summary matches on-screen results.  
9. All features function offline.  

---

## 14. Test Plan (Excerpt)

- **Parser:** 50+ inputs covering delimiters, hemispheres, edge limits.  
- **Vincenty:** validate vs NOAA test pairs.  
- **Cardinal:** boundary cases (11.25°, 33.75°, etc.)  
- **MagVar:** compare against reference declinations within ±0.1°.  
- **UI:** keyboard flows, theme persistence, import/export cycle.  
- **Performance:** 1,000+ waypoints < 500 ms compute.  

---

## 15. Security & Privacy

- No network access or telemetry.  
- Data stored locally only.  
- Optional code-signing for installer.  

---

## 16. Project Structure

```
/src
 ├─ MarineNav.Utility
 │   ├─ Geodesy/
 │   ├─ MagVar/
 │   ├─ Parsing/
 │   ├─ Models/
 │   ├─ IO/
 │   └─ Services/
 ├─ MarineNav.App (WPF)
 │   ├─ Views/
 │   ├─ ViewModels/
 │   ├─ Resources/
 │   └─ Assets/
 └─ MarineNav.Tests
```

---

## 17. Implementation Snippets

### Route Computation
```csharp
foreach (var (from, to) in route.Waypoints.Pairwise())
{
    var inv = VincentyInverse(from.Coord, to.Coord);
    double nm = inv.DistanceMeters / 1852.0;
    double bearingTrue = Normalize(inv.InitialAzimuthDeg);
    string cardinal = BearingToCardinal16(bearingTrue);

    double? decl = MagVarService.GetDeclination(from.Coord, dateUtc);
    bool high = MagVarService.IsHighConfidence(from.Coord, dateUtc);
    double? mag = high ? Normalize(bearingTrue - decl!.Value) : (double?)null;

    legs.Add(new Leg { From = from, To = to, ... });
}
```

### Current → Next Waypoint
```csharp
var inv = VincentyInverse(current, next);
double nm = inv.DistanceMeters / 1852.0;
double hours = nm / speedKts;
var eta = start + TimeSpan.FromHours(hours);
```

---

## 18. File Examples

### CSV
```csv
Name,Latitude,Longitude
Start,43.1234 N,087 54.608 W
Mid,43 07 24.2,-87.8901
End,43.0000,-88.0000
```

### GPX
```xml
<gpx version="1.1" creator="MarineNavUtility">
  <rte>
    <name>Lake Route</name>
    <rtept lat="43.1234" lon="-87.9101"><name>Start</name></rtept>
    <rtept lat="43.1233" lon="-87.8901"><name>Mid</name></rtept>
    <rtept lat="43.0000" lon="-88.0000"><name>End</name></rtept>
  </rte>
</gpx>
```

---

## 19. Milestones

| Milestone | Deliverables |
|------------|--------------|
| **M1** | Parser + Converter UI + Tests |
| **M2** | Vincenty + Leg Calc + Cardinal |
| **M3** | Speed/ETA + MagVar |
| **M4** | Import/Export + Print |
| **M5** | Theming + Shortcuts + QA |
| **Release 1.0** | Signed Installer + Portable EXE |

---

## 20. Future Enhancements
- Per-leg speed/layovers  
- Rhumb line option  
- Multi-route projects  
- Magnetic drift per leg  
- KML support + PDF export  
- Optional online geocoding  
- Tide/current data  
- Grouped waypoints  
