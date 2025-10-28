namespace MarineNav.Utility.Parsing;

/// <summary>
/// Coordinate display format options.
/// </summary>
public enum CoordinateFormat
{
    /// <summary>
    /// Decimal degrees (e.g., 43.1234°)
    /// </summary>
    DecimalDegrees,

    /// <summary>
    /// Degrees and decimal minutes (e.g., 43° 07.404′)
    /// </summary>
    DegreesDecimalMinutes,

    /// <summary>
    /// Degrees, minutes, and seconds (e.g., 43° 07′ 24.2″)
    /// </summary>
    DegreesMinutesSeconds
}

/// <summary>
/// Precision settings for coordinate formatting.
/// </summary>
public record CoordinatePrecision
{
    /// <summary>
    /// Decimal places for decimal degrees format.
    /// </summary>
    public int DecimalDegrees { get; init; } = 4;

    /// <summary>
    /// Decimal places for decimal minutes in DMS format.
    /// </summary>
    public int DecimalMinutes { get; init; } = 3;

    /// <summary>
    /// Decimal places for seconds in DMS format.
    /// </summary>
    public int Seconds { get; init; } = 1;

    /// <summary>
    /// Default precision settings.
    /// </summary>
    public static CoordinatePrecision Default => new();
}
