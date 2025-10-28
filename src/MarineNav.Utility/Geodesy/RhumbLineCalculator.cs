using MarineNav.Utility.Models;

namespace MarineNav.Utility.Geodesy;

/// <summary>
/// Rhumb line (loxodrome) calculations for constant bearing navigation.
/// A rhumb line is a path of constant bearing, appearing as a straight line on Mercator projection.
/// </summary>
public static class RhumbLineCalculator
{
    private const double DegreesToRadians = Math.PI / 180.0;
    private const double RadiansToDegrees = 180.0 / Math.PI;

    /// <summary>
    /// Calculates the rhumb line distance and bearing between two points.
    /// </summary>
    public static GeodesicResult CalculateRhumbLine(Coordinate from, Coordinate to)
    {
        double lat1 = from.LatitudeDeg * DegreesToRadians;
        double lon1 = from.LongitudeDeg * DegreesToRadians;
        double lat2 = to.LatitudeDeg * DegreesToRadians;
        double lon2 = to.LongitudeDeg * DegreesToRadians;

        double deltaLat = lat2 - lat1;
        double deltaLon = lon2 - lon1;

        // Handle dateline crossing
        if (Math.Abs(deltaLon) > Math.PI)
        {
            deltaLon = deltaLon > 0 ? -(2 * Math.PI - deltaLon) : (2 * Math.PI + deltaLon);
        }

        // Calculate delta psi (difference in latitude on Mercator projection)
        double deltaPsi = Math.Log(Math.Tan(lat2 / 2 + Math.PI / 4) / Math.Tan(lat1 / 2 + Math.PI / 4));

        // Calculate bearing (constant along rhumb line)
        double bearing = Math.Atan2(deltaLon, deltaPsi);

        // Handle special case: E-W line
        if (Math.Abs(deltaPsi) < 1e-12)
        {
            bearing = Math.Atan2(deltaLon, Math.Cos(lat1) * deltaLat);
        }

        // Calculate distance
        double distance;
        if (Math.Abs(deltaLat) < 1e-12)
        {
            // E-W line
            distance = Math.Abs(deltaLon) * Math.Cos(lat1);
        }
        else if (Math.Abs(deltaPsi) < 1e-12)
        {
            // N-S line
            distance = Math.Abs(deltaLat);
        }
        else
        {
            // General case
            distance = deltaLat / Math.Cos(bearing);
        }

        // Convert distance to meters (multiply by Earth radius)
        double distanceMeters = Math.Abs(distance) * WGS84.A;

        // Normalize bearing to 0-360 degrees
        double bearingDeg = (bearing * RadiansToDegrees + 360) % 360;

        return new GeodesicResult
        {
            DistanceMeters = distanceMeters,
            InitialAzimuthDeg = bearingDeg,
            FinalAzimuthDeg = bearingDeg, // Constant bearing along rhumb line
            Converged = true,
            Method = "Rhumb Line"
        };
    }

    /// <summary>
    /// Calculates the destination point given start point, bearing, and distance along a rhumb line.
    /// </summary>
    public static Coordinate CalculateDestination(Coordinate start, double bearingDeg, double distanceMeters)
    {
        double lat1 = start.LatitudeDeg * DegreesToRadians;
        double lon1 = start.LongitudeDeg * DegreesToRadians;
        double bearing = bearingDeg * DegreesToRadians;
        double angularDistance = distanceMeters / WGS84.A;

        double deltaLat = angularDistance * Math.Cos(bearing);
        double lat2 = lat1 + deltaLat;

        // Check for poles
        if (Math.Abs(lat2) > Math.PI / 2)
        {
            lat2 = lat2 > 0 ? Math.PI / 2 : -Math.PI / 2;
        }

        double deltaPsi = Math.Log(Math.Tan(lat2 / 2 + Math.PI / 4) / Math.Tan(lat1 / 2 + Math.PI / 4));
        double q = Math.Abs(deltaPsi) > 1e-12 ? deltaLat / deltaPsi : Math.Cos(lat1);

        double deltaLon = angularDistance * Math.Sin(bearing) / q;
        double lon2 = lon1 + deltaLon;

        // Normalize longitude to -180 to +180
        lon2 = ((lon2 + 3 * Math.PI) % (2 * Math.PI)) - Math.PI;

        return new Coordinate(lat2 * RadiansToDegrees, lon2 * RadiansToDegrees);
    }
}
