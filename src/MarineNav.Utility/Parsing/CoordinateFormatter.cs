using System.Globalization;
using MarineNav.Utility.Models;

namespace MarineNav.Utility.Parsing;

/// <summary>
/// Formats coordinates into various display formats.
/// </summary>
public class CoordinateFormatter
{
    private readonly CoordinatePrecision _precision;

    public CoordinateFormatter(CoordinatePrecision? precision = null)
    {
        _precision = precision ?? CoordinatePrecision.Default;
    }

    /// <summary>
    /// Formats a coordinate component in the specified format.
    /// </summary>
    public string Format(double decimalDegrees, bool isLatitude, CoordinateFormat format, bool includeHemisphere = true)
    {
        char hemisphere = GetHemisphere(decimalDegrees, isLatitude);
        double absValue = Math.Abs(decimalDegrees);

        return format switch
        {
            CoordinateFormat.DecimalDegrees => FormatDecimalDegrees(absValue, hemisphere, includeHemisphere),
            CoordinateFormat.DegreesDecimalMinutes => FormatDegreesDecimalMinutes(absValue, hemisphere, includeHemisphere),
            CoordinateFormat.DegreesMinutesSeconds => FormatDegreesMinutesSeconds(absValue, hemisphere, includeHemisphere),
            _ => throw new ArgumentException($"Unknown format: {format}")
        };
    }

    /// <summary>
    /// Formats a full coordinate in the specified format.
    /// </summary>
    public (string latitude, string longitude) Format(Coordinate coord, CoordinateFormat format, bool includeHemisphere = true)
    {
        string lat = Format(coord.LatitudeDeg, true, format, includeHemisphere);
        string lon = Format(coord.LongitudeDeg, false, format, includeHemisphere);
        return (lat, lon);
    }

    /// <summary>
    /// Formats in Decimal Degrees format (e.g., "43.1234° N")
    /// </summary>
    private string FormatDecimalDegrees(double absValue, char hemisphere, bool includeHemisphere)
    {
        string formatted = absValue.ToString($"F{_precision.DecimalDegrees}", CultureInfo.InvariantCulture);
        if (includeHemisphere)
            return $"{formatted}° {hemisphere}";
        return $"{formatted}°";
    }

    /// <summary>
    /// Formats in Degrees Decimal Minutes format (e.g., "43° 07.404′ N")
    /// </summary>
    private string FormatDegreesDecimalMinutes(double absValue, char hemisphere, bool includeHemisphere)
    {
        int degrees = (int)Math.Floor(absValue);
        double minutes = (absValue - degrees) * 60.0;

        string formatted = $"{degrees:D3}° {minutes.ToString($"F{_precision.DecimalMinutes}", CultureInfo.InvariantCulture).PadLeft(_precision.DecimalMinutes + 3, '0')}′";
        
        if (includeHemisphere)
            return $"{formatted} {hemisphere}";
        return formatted;
    }

    /// <summary>
    /// Formats in Degrees Minutes Seconds format (e.g., "43° 07′ 24.2″ N")
    /// </summary>
    private string FormatDegreesMinutesSeconds(double absValue, char hemisphere, bool includeHemisphere)
    {
        int degrees = (int)Math.Floor(absValue);
        double remainder = (absValue - degrees) * 60.0;
        int minutes = (int)Math.Floor(remainder);
        double seconds = (remainder - minutes) * 60.0;

        // Handle rounding edge case where seconds rounds to 60
        if (seconds >= 60.0 - (0.5 / Math.Pow(10, _precision.Seconds)))
        {
            seconds = 0;
            minutes++;
            if (minutes >= 60)
            {
                minutes = 0;
                degrees++;
            }
        }

        string formatted = $"{degrees:D3}° {minutes:D2}′ {seconds.ToString($"F{_precision.Seconds}", CultureInfo.InvariantCulture)}″";
        
        if (includeHemisphere)
            return $"{formatted} {hemisphere}";
        return formatted;
    }

    /// <summary>
    /// Gets the hemisphere character for a coordinate value.
    /// </summary>
    private static char GetHemisphere(double value, bool isLatitude)
    {
        if (isLatitude)
            return value >= 0 ? 'N' : 'S';
        else
            return value >= 0 ? 'E' : 'W';
    }

    /// <summary>
    /// Converts from one format to another.
    /// </summary>
    public string Convert(string input, bool isLatitude, CoordinateFormat outputFormat)
    {
        double decimalDegrees = CoordinateParser.Parse(input, isLatitude);
        return Format(decimalDegrees, isLatitude, outputFormat);
    }
}
