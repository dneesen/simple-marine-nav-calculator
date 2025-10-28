namespace MarineNav.Utility.Models;

/// <summary>
/// Represents a complete route with waypoints and calculated legs.
/// </summary>
public record Route
{
    /// <summary>
    /// Name of the route.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// List of waypoints in order.
    /// </summary>
    public required List<Waypoint> Waypoints { get; init; }

    /// <summary>
    /// Calculated legs between waypoints.
    /// </summary>
    public required List<Leg> Legs { get; init; }

    /// <summary>
    /// Total distance of the route in nautical miles.
    /// </summary>
    public double TotalDistanceNm => Legs.Sum(l => l.DistanceNm);

    /// <summary>
    /// Total time for the route.
    /// Null if any leg doesn't have time calculated.
    /// </summary>
    public TimeSpan? TotalTime
    {
        get
        {
            if (Legs.Any(l => l.LegTime == null))
                return null;
            return TimeSpan.FromTicks(Legs.Sum(l => l.LegTime!.Value.Ticks));
        }
    }

    /// <summary>
    /// Final ETA for the route.
    /// Null if any leg doesn't have ETA calculated.
    /// </summary>
    public DateTimeOffset? FinalETA => Legs.LastOrDefault()?.ETA;

    /// <summary>
    /// Creates an empty route.
    /// </summary>
    public static Route CreateEmpty(string name) => new()
    {
        Name = name,
        Waypoints = new List<Waypoint>(),
        Legs = new List<Leg>()
    };
}
