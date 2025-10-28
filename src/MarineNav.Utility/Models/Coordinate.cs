namespace MarineNav.Utility.Models;

/// <summary>
/// Represents a geographic coordinate with latitude and longitude in decimal degrees.
/// </summary>
/// <param name="LatitudeDeg">Latitude in decimal degrees (-90 to +90, negative = South)</param>
/// <param name="LongitudeDeg">Longitude in decimal degrees (-180 to +180, negative = West)</param>
public record Coordinate(double LatitudeDeg, double LongitudeDeg)
{
    /// <summary>
    /// Validates that the coordinate is within valid ranges.
    /// </summary>
    public bool IsValid => LatitudeDeg >= -90 && LatitudeDeg <= 90 &&
                           LongitudeDeg >= -180 && LongitudeDeg <= 180;

    /// <summary>
    /// Gets the latitude hemisphere (N/S).
    /// </summary>
    public char LatitudeHemisphere => LatitudeDeg >= 0 ? 'N' : 'S';

    /// <summary>
    /// Gets the longitude hemisphere (E/W).
    /// </summary>
    public char LongitudeHemisphere => LongitudeDeg >= 0 ? 'E' : 'W';

    /// <summary>
    /// Gets the absolute latitude value.
    /// </summary>
    public double AbsLatitude => Math.Abs(LatitudeDeg);

    /// <summary>
    /// Gets the absolute longitude value.
    /// </summary>
    public double AbsLongitude => Math.Abs(LongitudeDeg);
}
