using MarineNav.Utility.Geodesy;
using MarineNav.Utility.MagVar;
using MarineNav.Utility.Models;

namespace MarineNav.Utility.Services;

/// <summary>
/// Service for calculating route legs with distance, bearing, and cardinal directions.
/// </summary>
public class RouteCalculationService
{
    /// <summary>
    /// Calculates all legs for a route from a list of waypoints.
    /// </summary>
    /// <param name="waypoints">Ordered list of waypoints</param>
    /// <param name="speedKnots">Vessel speed in knots (optional)</param>
    /// <param name="startTime">Start time for ETA calculations (optional)</param>
    /// <param name="useRhumbLine">Use rhumb line instead of great circle (default: false)</param>
    /// <returns>List of calculated legs</returns>
    public static List<Leg> CalculateLegs(
        List<Waypoint> waypoints,
        double? speedKnots = null,
        DateTimeOffset? startTime = null,
        bool useRhumbLine = false)
    {
        var legs = new List<Leg>();
        
        if (waypoints.Count < 2)
            return legs;

        DateTimeOffset? currentTime = startTime;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            var from = waypoints[i];
            var to = waypoints[i + 1];

            // Calculate distance and bearing (geodesic or rhumb line)
            var result = useRhumbLine
                ? RhumbLineCalculator.CalculateRhumbLine(from.Coord, to.Coord)
                : GeodesicCalculator.CalculateInverse(from.Coord, to.Coord);
            
            double distanceNm = result.DistanceNauticalMiles;
            double trueBearing = result.InitialAzimuthDeg;
            string cardinal = CardinalDirection.FromBearing(trueBearing);

            // Calculate magnetic variation at the starting point
            var magService = new MagneticVariationService(DateTime.UtcNow);
            var magVar = magService.GetDeclination(from.Coord);
            
            double? variationDeg = null;
            double? magneticCourseDeg = null;
            
            if (magVar.HighConfidence)
            {
                variationDeg = magVar.Declination;
                magneticCourseDeg = magService.TrueToMagnetic(trueBearing, from.Coord);
            }

            // Calculate time and ETA if speed is provided
            TimeSpan? legTime = null;
            DateTimeOffset? eta = null;

            if (speedKnots.HasValue && speedKnots.Value > 0)
            {
                double hours = distanceNm / speedKnots.Value;
                legTime = TimeSpan.FromHours(hours);

                if (currentTime.HasValue)
                {
                    currentTime = currentTime.Value.Add(legTime.Value);
                    eta = currentTime.Value;
                }
            }

            var leg = new Leg
            {
                From = from,
                To = to,
                DistanceNm = distanceNm,
                TrueBearingDeg = trueBearing,
                Cardinal16 = cardinal,
                VariationDeg = variationDeg,
                MagneticCourseDeg = magneticCourseDeg,
                LegTime = legTime,
                ETA = eta
            };

            legs.Add(leg);
        }

        return legs;
    }

    /// <summary>
    /// Creates a complete route with calculated legs.
    /// </summary>
    public static Route CreateRoute(
        string name,
        List<Waypoint> waypoints,
        double? speedKnots = null,
        DateTimeOffset? startTime = null)
    {
        var legs = CalculateLegs(waypoints, speedKnots, startTime);

        return new Route
        {
            Name = name,
            Waypoints = waypoints,
            Legs = legs
        };
    }

    /// <summary>
    /// Recalculates a route with new speed and/or start time.
    /// </summary>
    public static Route RecalculateRoute(
        Route route,
        double? newSpeedKnots = null,
        DateTimeOffset? newStartTime = null)
    {
        return CreateRoute(route.Name, route.Waypoints, newSpeedKnots, newStartTime);
    }

    /// <summary>
    /// Calculates the distance and bearing from a current position to a specific waypoint.
    /// </summary>
    public static (double distanceNm, double bearingDeg, string cardinal, DateTimeOffset? eta) CalculateToWaypoint(
        Coordinate currentPosition,
        Waypoint targetWaypoint,
        double? speedKnots = null,
        DateTimeOffset? currentTime = null)
    {
        var geodesic = GeodesicCalculator.CalculateInverse(
            new Coordinate(currentPosition.LatitudeDeg, currentPosition.LongitudeDeg),
            targetWaypoint.Coord);

        double distanceNm = geodesic.DistanceNauticalMiles;
        double bearing = geodesic.InitialAzimuthDeg;
        string cardinal = CardinalDirection.FromBearing(bearing);

        DateTimeOffset? eta = null;
        if (speedKnots.HasValue && speedKnots.Value > 0 && currentTime.HasValue)
        {
            double hours = distanceNm / speedKnots.Value;
            eta = currentTime.Value.AddHours(hours);
        }

        return (distanceNm, bearing, cardinal, eta);
    }
}
