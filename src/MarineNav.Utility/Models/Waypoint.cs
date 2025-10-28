namespace MarineNav.Utility.Models;

/// <summary>
/// Represents a named waypoint with a coordinate.
/// </summary>
/// <param name="Id">Unique identifier for the waypoint</param>
/// <param name="Name">Display name of the waypoint</param>
/// <param name="Coord">Geographic coordinate</param>
public record Waypoint(Guid Id, string Name, Coordinate Coord)
{
    /// <summary>
    /// Creates a new waypoint with a generated ID.
    /// </summary>
    public static Waypoint Create(string name, Coordinate coord) => new(Guid.NewGuid(), name, coord);
}
