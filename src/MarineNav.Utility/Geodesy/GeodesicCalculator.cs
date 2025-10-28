using MarineNav.Utility.Models;

namespace MarineNav.Utility.Geodesy;

/// <summary>
/// Geodesic calculations using Vincenty's formulae with Haversine fallback.
/// </summary>
public static class GeodesicCalculator
{
    private const int MaxIterations = 200;
    private const double ConvergenceThreshold = 1e-12;
    private const double DegreesToRadians = Math.PI / 180.0;
    private const double RadiansToDegrees = 180.0 / Math.PI;

    /// <summary>
    /// Calculates the geodesic distance and bearing between two points.
    /// Uses Vincenty inverse formula with Haversine fallback.
    /// </summary>
    public static GeodesicResult CalculateInverse(Coordinate from, Coordinate to)
    {
        // Try Vincenty first
        var result = VincentyInverse(from, to);
        
        if (!result.Converged)
        {
            // Fallback to Haversine
            result = HaversineCalculation(from, to);
        }

        return result;
    }

    /// <summary>
    /// Vincenty inverse formula for geodesic calculations on an ellipsoid.
    /// </summary>
    private static GeodesicResult VincentyInverse(Coordinate from, Coordinate to)
    {
        double lat1 = from.LatitudeDeg * DegreesToRadians;
        double lon1 = from.LongitudeDeg * DegreesToRadians;
        double lat2 = to.LatitudeDeg * DegreesToRadians;
        double lon2 = to.LongitudeDeg * DegreesToRadians;

        double L = lon2 - lon1;
        double U1 = Math.Atan((1 - WGS84.F) * Math.Tan(lat1));
        double U2 = Math.Atan((1 - WGS84.F) * Math.Tan(lat2));
        double sinU1 = Math.Sin(U1);
        double cosU1 = Math.Cos(U1);
        double sinU2 = Math.Sin(U2);
        double cosU2 = Math.Cos(U2);

        double lambda = L;
        double lambdaP;
        int iterLimit = MaxIterations;
        double cosSqAlpha = 0, sinSigma = 0, cos2SigmaM = 0, cosSigma = 0, sigma = 0;

        do
        {
            double sinLambda = Math.Sin(lambda);
            double cosLambda = Math.Cos(lambda);
            sinSigma = Math.Sqrt((cosU2 * sinLambda) * (cosU2 * sinLambda) +
                                 (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda) *
                                 (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda));

            if (Math.Abs(sinSigma) < 1e-12)
            {
                // Co-incident points
                return new GeodesicResult
                {
                    DistanceMeters = 0,
                    InitialAzimuthDeg = 0,
                    FinalAzimuthDeg = 0,
                    Converged = true,
                    Method = "Vincenty"
                };
            }

            cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
            sigma = Math.Atan2(sinSigma, cosSigma);
            double sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
            cosSqAlpha = 1 - sinAlpha * sinAlpha;
            cos2SigmaM = cosSigma - 2 * sinU1 * sinU2 / cosSqAlpha;

            if (double.IsNaN(cos2SigmaM))
                cos2SigmaM = 0; // Equatorial line

            double C = WGS84.F / 16 * cosSqAlpha * (4 + WGS84.F * (4 - 3 * cosSqAlpha));
            lambdaP = lambda;
            lambda = L + (1 - C) * WGS84.F * sinAlpha *
                     (sigma + C * sinSigma * (cos2SigmaM + C * cosSigma *
                     (-1 + 2 * cos2SigmaM * cos2SigmaM)));

        } while (Math.Abs(lambda - lambdaP) > ConvergenceThreshold && --iterLimit > 0);

        if (iterLimit == 0)
        {
            // Failed to converge
            return new GeodesicResult
            {
                DistanceMeters = 0,
                InitialAzimuthDeg = 0,
                FinalAzimuthDeg = 0,
                Converged = false,
                Method = "Vincenty (failed)"
            };
        }

        double uSq = cosSqAlpha * (WGS84.A * WGS84.A - WGS84.B * WGS84.B) / (WGS84.B * WGS84.B);
        double A = 1 + uSq / 16384 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
        double B = uSq / 1024 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));
        double deltaSigma = B * sinSigma * (cos2SigmaM + B / 4 * (cosSigma *
                           (-1 + 2 * cos2SigmaM * cos2SigmaM) - B / 6 * cos2SigmaM *
                           (-3 + 4 * sinSigma * sinSigma) * (-3 + 4 * cos2SigmaM * cos2SigmaM)));

        double s = WGS84.B * A * (sigma - deltaSigma);

        double alpha1 = Math.Atan2(cosU2 * Math.Sin(lambda),
                                   cosU1 * sinU2 - sinU1 * cosU2 * Math.Cos(lambda));
        double alpha2 = Math.Atan2(cosU1 * Math.Sin(lambda),
                                   -sinU1 * cosU2 + cosU1 * sinU2 * Math.Cos(lambda));

        // Normalize to 0-360
        alpha1 = NormalizeAzimuth(alpha1 * RadiansToDegrees);
        alpha2 = NormalizeAzimuth(alpha2 * RadiansToDegrees);

        return new GeodesicResult
        {
            DistanceMeters = s,
            InitialAzimuthDeg = alpha1,
            FinalAzimuthDeg = alpha2,
            Converged = true,
            Method = "Vincenty"
        };
    }

    /// <summary>
    /// Haversine formula for distance calculation (spherical approximation).
    /// Used as fallback when Vincenty doesn't converge.
    /// </summary>
    private static GeodesicResult HaversineCalculation(Coordinate from, Coordinate to)
    {
        double lat1 = from.LatitudeDeg * DegreesToRadians;
        double lon1 = from.LongitudeDeg * DegreesToRadians;
        double lat2 = to.LatitudeDeg * DegreesToRadians;
        double lon2 = to.LongitudeDeg * DegreesToRadians;

        double dLat = lat2 - lat1;
        double dLon = lon2 - lon1;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1) * Math.Cos(lat2) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double distance = WGS84.A * c;

        // Calculate bearing using spherical geometry
        double y = Math.Sin(dLon) * Math.Cos(lat2);
        double x = Math.Cos(lat1) * Math.Sin(lat2) -
                   Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
        double bearing = Math.Atan2(y, x) * RadiansToDegrees;
        bearing = NormalizeAzimuth(bearing);

        return new GeodesicResult
        {
            DistanceMeters = distance,
            InitialAzimuthDeg = bearing,
            FinalAzimuthDeg = bearing, // Approximate - same as initial for Haversine
            Converged = true,
            Method = "Haversine"
        };
    }

    /// <summary>
    /// Normalizes an azimuth to 0-360 degrees.
    /// </summary>
    public static double NormalizeAzimuth(double degrees)
    {
        degrees = degrees % 360;
        if (degrees < 0)
            degrees += 360;
        return degrees;
    }
}
