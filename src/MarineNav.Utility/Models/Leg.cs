namespace MarineNav.Utility.Models;

/// <summary>
/// Represents a leg between two waypoints with navigation data.
/// </summary>
public record Leg
{
    /// <summary>
    /// Starting waypoint of the leg.
    /// </summary>
    public required Waypoint From { get; init; }

    /// <summary>
    /// Ending waypoint of the leg.
    /// </summary>
    public required Waypoint To { get; init; }

    /// <summary>
    /// Distance in nautical miles.
    /// </summary>
    public required double DistanceNm { get; init; }

    /// <summary>
    /// True bearing in degrees (0-360).
    /// </summary>
    public required double TrueBearingDeg { get; init; }

    /// <summary>
    /// 16-point cardinal direction (e.g., "NNE", "SW").
    /// </summary>
    public required string Cardinal16 { get; init; }

    /// <summary>
    /// Magnetic variation in degrees (positive = East, negative = West).
    /// Null if not available or low confidence.
    /// </summary>
    public double? VariationDeg { get; init; }

    /// <summary>
    /// Magnetic course in degrees (0-360).
    /// Null if variation not available.
    /// </summary>
    public double? MagneticCourseDeg { get; init; }

    /// <summary>
    /// Time to complete this leg.
    /// Null if speed not provided.
    /// </summary>
    public TimeSpan? LegTime { get; init; }

    /// <summary>
    /// Estimated time of arrival at the destination waypoint.
    /// Null if speed or start time not provided.
    /// </summary>
    public DateTimeOffset? ETA { get; init; }
}
