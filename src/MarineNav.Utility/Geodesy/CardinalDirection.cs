namespace MarineNav.Utility.Geodesy;

/// <summary>
/// Converts bearings to 16-point cardinal directions.
/// </summary>
public static class CardinalDirection
{
    private static readonly string[] Directions16 =
    {
        "N", "NNE", "NE", "ENE",
        "E", "ESE", "SE", "SSE",
        "S", "SSW", "SW", "WSW",
        "W", "WNW", "NW", "NNW"
    };

    /// <summary>
    /// Converts a bearing in degrees to a 16-point cardinal direction.
    /// </summary>
    /// <param name="bearingDegrees">Bearing in degrees (0-360)</param>
    /// <returns>16-point cardinal direction string</returns>
    public static string FromBearing(double bearingDegrees)
    {
        // Normalize to 0-360
        bearingDegrees = GeodesicCalculator.NormalizeAzimuth(bearingDegrees);

        // Each cardinal direction covers 22.5 degrees (360 / 16)
        // Add 11.25 degrees to center the ranges on each direction
        int index = (int)Math.Floor((bearingDegrees + 11.25) / 22.5) % 16;

        return Directions16[index];
    }

    /// <summary>
    /// Gets all 16 cardinal direction names.
    /// </summary>
    public static IReadOnlyList<string> GetAll16Points() => Directions16;

    /// <summary>
    /// Gets the bearing range (min, max) for a given cardinal direction.
    /// </summary>
    public static (double min, double max) GetBearingRange(string cardinal)
    {
        int index = Array.IndexOf(Directions16, cardinal.ToUpper());
        if (index < 0)
            throw new ArgumentException($"Invalid cardinal direction: {cardinal}");

        double center = index * 22.5;
        double min = center - 11.25;
        double max = center + 11.25;

        // Handle wrapping for North
        if (min < 0)
            min += 360;
        if (max >= 360)
            max -= 360;

        return (min, max);
    }
}
