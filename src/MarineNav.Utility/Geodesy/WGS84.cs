namespace MarineNav.Utility.Geodesy;

/// <summary>
/// WGS84 ellipsoid constants for geodetic calculations.
/// </summary>
public static class WGS84
{
    /// <summary>
    /// Semi-major axis (equatorial radius) in meters.
    /// </summary>
    public const double A = 6378137.0;

    /// <summary>
    /// Flattening.
    /// </summary>
    public const double F = 1.0 / 298.257223563;

    /// <summary>
    /// Semi-minor axis (polar radius) in meters.
    /// </summary>
    public static readonly double B = A * (1 - F);

    /// <summary>
    /// Converts meters to nautical miles.
    /// </summary>
    public const double MetersToNauticalMiles = 1.0 / 1852.0;

    /// <summary>
    /// Converts nautical miles to meters.
    /// </summary>
    public const double NauticalMilesToMeters = 1852.0;
}
