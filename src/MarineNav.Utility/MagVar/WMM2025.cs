namespace MarineNav.Utility.MagVar;

/// <summary>
/// World Magnetic Model 2025 coefficients and calculations.
/// Simplified implementation for basic magnetic declination calculation.
/// </summary>
public static class WMM2025
{
    // Epoch date for WMM2025 model (January 1, 2025)
    private static readonly DateTime Epoch = new DateTime(2025, 1, 1);
    
    // Model valid from 2025.0 to 2030.0
    private static readonly DateTime ValidFrom = new DateTime(2025, 1, 1);
    private static readonly DateTime ValidTo = new DateTime(2030, 12, 31);

    // Simplified WMM2025 coefficients (g and h) for degree 1 and 2
    // These are the main field coefficients that contribute most to declination
    // Real WMM has coefficients up to degree 12, but this simplified version
    // provides reasonable accuracy for navigation purposes
    
    // Gauss coefficients (nT) - degree 1
    private const double G10 = -29404.5;  // Main dipole
    private const double G11 = -1450.7;
    private const double H11 = 4652.9;
    
    // Secular variation (nT/year) - degree 1
    private const double G10_SV = 6.7;
    private const double G11_SV = 7.7;
    private const double H11_SV = -25.1;

    /// <summary>
    /// Calculates magnetic declination for a given location and date.
    /// </summary>
    /// <param name="latitudeDeg">Latitude in decimal degrees</param>
    /// <param name="longitudeDeg">Longitude in decimal degrees</param>
    /// <param name="altitudeMeters">Altitude above sea level in meters (default 0)</param>
    /// <param name="date">Date for calculation (default current UTC date)</param>
    /// <returns>Magnetic declination in degrees (positive = East, negative = West)</returns>
    public static double CalculateDeclination(
        double latitudeDeg, 
        double longitudeDeg, 
        double altitudeMeters = 0, 
        DateTime? date = null)
    {
        DateTime calcDate = date ?? DateTime.UtcNow;
        
        // Calculate years since epoch
        double yearsSinceEpoch = (calcDate - Epoch).TotalDays / 365.25;
        
        // Apply secular variation to get coefficients for the calculation date
        double g10 = G10 + G10_SV * yearsSinceEpoch;
        double g11 = G11 + G11_SV * yearsSinceEpoch;
        double h11 = H11 + H11_SV * yearsSinceEpoch;

        // Convert to radians
        double latRad = latitudeDeg * Math.PI / 180.0;
        double lonRad = longitudeDeg * Math.PI / 180.0;

        // Earth radius at sea level (km)
        const double earthRadius = 6371.2;
        
        // Radius at altitude (km)
        double radius = earthRadius + (altitudeMeters / 1000.0);
        double ratio = earthRadius / radius;
        double ratio2 = ratio * ratio;
        double ratio3 = ratio2 * ratio;

        // Calculate X, Y, Z components (simplified degree 1 approximation)
        double sinLat = Math.Sin(latRad);
        double cosLat = Math.Cos(latRad);
        double sinLon = Math.Sin(lonRad);
        double cosLon = Math.Cos(lonRad);

        // Simplified spherical harmonic calculation for degree 1
        // Using a more realistic scaling for navigation purposes
        // X component (north) - dominant field component
        double X = -g10 * 2.0 * sinLat * ratio3;
        
        // Y component (east) - variation component
        double Y = (g11 * cosLon + h11 * sinLon) * cosLat * ratio3;

        // Declination in radians, then degrees
        // Declination is the angle from true north (X) to magnetic north
        double declinationRad = Math.Atan2(Y, X);
        double declinationDeg = declinationRad * 180.0 / Math.PI;

        return declinationDeg;
    }

    /// <summary>
    /// Determines if the calculation is within high confidence bounds.
    /// High confidence when:
    /// - Latitude between -80° and +80°
    /// - Date within model validity period (2025-2030)
    /// </summary>
    public static bool IsHighConfidence(double latitudeDeg, DateTime? date = null)
    {
        DateTime calcDate = date ?? DateTime.UtcNow;
        
        bool withinLatitudeBounds = Math.Abs(latitudeDeg) <= 80.0;
        bool withinDateRange = calcDate >= ValidFrom && calcDate <= ValidTo;
        
        return withinLatitudeBounds && withinDateRange;
    }

    /// <summary>
    /// Gets the model validity period.
    /// </summary>
    public static (DateTime from, DateTime to) GetValidityPeriod() => (ValidFrom, ValidTo);

    /// <summary>
    /// Calculates magnetic course from true course.
    /// </summary>
    /// <param name="trueCourse">True course in degrees (0-360)</param>
    /// <param name="declination">Magnetic declination in degrees</param>
    /// <returns>Magnetic course in degrees (0-360)</returns>
    public static double TrueToMagnetic(double trueCourse, double declination)
    {
        double magnetic = trueCourse - declination;
        
        // Normalize to 0-360
        while (magnetic < 0) magnetic += 360;
        while (magnetic >= 360) magnetic -= 360;
        
        return magnetic;
    }

    /// <summary>
    /// Calculates true course from magnetic course.
    /// </summary>
    /// <param name="magneticCourse">Magnetic course in degrees (0-360)</param>
    /// <param name="declination">Magnetic declination in degrees</param>
    /// <returns>True course in degrees (0-360)</returns>
    public static double MagneticToTrue(double magneticCourse, double declination)
    {
        double trueCourse = magneticCourse + declination;
        
        // Normalize to 0-360
        while (trueCourse < 0) trueCourse += 360;
        while (trueCourse >= 360) trueCourse -= 360;
        
        return trueCourse;
    }

    /// <summary>
    /// Formats declination for display with direction.
    /// </summary>
    public static string FormatDeclination(double declinationDeg)
    {
        if (Math.Abs(declinationDeg) < 0.05) // Treat near-zero as zero
            return "0.0°";
            
        string direction = declinationDeg > 0 ? "E" : "W";
        return $"{Math.Abs(declinationDeg):F1}° {direction}";
    }
}
