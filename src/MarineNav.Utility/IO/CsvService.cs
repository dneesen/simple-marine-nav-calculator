using System.Globalization;
using System.Text;
using MarineNav.Utility.Models;
using MarineNav.Utility.Parsing;

namespace MarineNav.Utility.IO;

/// <summary>
/// Service for importing and exporting waypoints and routes to CSV format.
/// </summary>
public static class CsvService
{
    /// <summary>
    /// Imports waypoints from a CSV file.
    /// Expected format: Name,Latitude,Longitude
    /// Header row is optional and will be detected.
    /// </summary>
    public static List<Waypoint> ImportWaypoints(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("CSV file not found", filePath);

        var waypoints = new List<Waypoint>();
        var lines = File.ReadAllLines(filePath);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Skip header row if detected
            if (line.Contains("Name", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("Latitude", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = line.Split(',');
            if (parts.Length < 3)
                continue;

            try
            {
                string name = parts[0].Trim().Trim('"');
                string latStr = parts[1].Trim().Trim('"');
                string lonStr = parts[2].Trim().Trim('"');

                // Try parsing as decimal first, then as formatted coordinate
                double lat = TryParseCoordinate(latStr, isLatitude: true);
                double lon = TryParseCoordinate(lonStr, isLatitude: false);

                waypoints.Add(Waypoint.Create(name, new Coordinate(lat, lon)));
            }
            catch (Exception ex)
            {
                // Skip invalid lines
                Console.WriteLine($"Skipping invalid line: {line} - {ex.Message}");
            }
        }

        return waypoints;
    }

    /// <summary>
    /// Exports waypoints to a CSV file (simple format).
    /// </summary>
    public static void ExportWaypoints(string filePath, List<Waypoint> waypoints)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Name,Latitude,Longitude");

        foreach (var wpt in waypoints)
        {
            sb.AppendLine($"\"{wpt.Name}\",{wpt.Coord.LatitudeDeg:F6},{wpt.Coord.LongitudeDeg:F6}");
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    /// <summary>
    /// Exports a complete route with calculated legs to CSV.
    /// </summary>
    public static void ExportRoute(string filePath, Route route)
    {
        var sb = new StringBuilder();
        
        // Route header
        sb.AppendLine($"Route Name,{route.Name}");
        sb.AppendLine($"Total Distance (NM),{route.TotalDistanceNm:F1}");
        sb.AppendLine($"Total Time,{route.TotalTime?.TotalHours:F1} hours");
        sb.AppendLine();

        // Waypoints section
        sb.AppendLine("Waypoints");
        sb.AppendLine("Number,Name,Latitude,Longitude");
        for (int i = 0; i < route.Waypoints.Count; i++)
        {
            var wpt = route.Waypoints[i];
            sb.AppendLine($"{i + 1},\"{wpt.Name}\",{wpt.Coord.LatitudeDeg:F6},{wpt.Coord.LongitudeDeg:F6}");
        }
        sb.AppendLine();

        // Legs section
        sb.AppendLine("Legs");
        sb.AppendLine("Leg#,From,To,Distance (NM),True Bearing,Magnetic Bearing,Cardinal,Variation,Time (hrs),ETA");
        for (int i = 0; i < route.Legs.Count; i++)
        {
            var leg = route.Legs[i];
            string trueBearing = $"{leg.TrueBearingDeg:000}°T";
            string magBearing = leg.MagneticCourseDeg.HasValue ? $"{leg.MagneticCourseDeg.Value:000}°M" : "-";
            string variation = leg.VariationDeg.HasValue 
                ? $"{Math.Abs(leg.VariationDeg.Value):F1}°{(leg.VariationDeg.Value >= 0 ? "E" : "W")}"
                : "-";
            string time = leg.LegTime?.TotalHours.ToString("F2") ?? "-";
            string eta = leg.ETA?.ToString("yyyy-MM-dd HH:mm") ?? "-";

            sb.AppendLine($"{i + 1},\"{leg.From.Name}\",\"{leg.To.Name}\",{leg.DistanceNm:F1},{trueBearing},{magBearing},\"{leg.Cardinal16}\",{variation},{time},{eta}");
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    private static double TryParseCoordinate(string value, bool isLatitude)
    {
        // Try parsing as simple decimal number first
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
        {
            return result;
        }

        // Try parsing as formatted coordinate (e.g., "40° 30.5' N")
        return CoordinateParser.Parse(value, isLatitude);
    }
}
