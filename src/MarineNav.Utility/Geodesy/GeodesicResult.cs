namespace MarineNav.Utility.Geodesy;

/// <summary>
/// Result of a geodesic inverse calculation.
/// </summary>
public record GeodesicResult
{
    /// <summary>
    /// Distance between points in meters.
    /// </summary>
    public required double DistanceMeters { get; init; }

    /// <summary>
    /// Initial azimuth (bearing) from first point in degrees (0-360).
    /// </summary>
    public required double InitialAzimuthDeg { get; init; }

    /// <summary>
    /// Final azimuth (bearing) at second point in degrees (0-360).
    /// </summary>
    public required double FinalAzimuthDeg { get; init; }

    /// <summary>
    /// Indicates if the calculation converged successfully.
    /// </summary>
    public required bool Converged { get; init; }

    /// <summary>
    /// Calculation method used.
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Distance in nautical miles.
    /// </summary>
    public double DistanceNauticalMiles => DistanceMeters * WGS84.MetersToNauticalMiles;
}
