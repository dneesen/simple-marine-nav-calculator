using MarineNav.Utility.Models;

namespace MarineNav.Utility.MagVar;

/// <summary>
/// Service for magnetic variation calculations and management.
/// </summary>
public class MagneticVariationService
{
    private readonly DateTime _calculationDate;

    public MagneticVariationService(DateTime? calculationDate = null)
    {
        _calculationDate = calculationDate ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Gets magnetic declination for a coordinate.
    /// </summary>
    public MagneticVariation GetDeclination(Coordinate coordinate, double altitudeMeters = 0)
    {
        double declination = WMM2025.CalculateDeclination(
            coordinate.LatitudeDeg,
            coordinate.LongitudeDeg,
            altitudeMeters,
            _calculationDate);

        bool highConfidence = WMM2025.IsHighConfidence(coordinate.LatitudeDeg, _calculationDate);

        return new MagneticVariation
        {
            Declination = declination,
            HighConfidence = highConfidence,
            CalculationDate = _calculationDate,
            Latitude = coordinate.LatitudeDeg,
            Longitude = coordinate.LongitudeDeg
        };
    }

    /// <summary>
    /// Converts true bearing to magnetic bearing.
    /// </summary>
    public double? TrueToMagnetic(double trueBearing, Coordinate coordinate)
    {
        var magVar = GetDeclination(coordinate);
        
        if (!magVar.HighConfidence)
            return null;

        return WMM2025.TrueToMagnetic(trueBearing, magVar.Declination);
    }

    /// <summary>
    /// Converts magnetic bearing to true bearing.
    /// </summary>
    public double? MagneticToTrue(double magneticBearing, Coordinate coordinate)
    {
        var magVar = GetDeclination(coordinate);
        
        if (!magVar.HighConfidence)
            return null;

        return WMM2025.MagneticToTrue(magneticBearing, magVar.Declination);
    }

    /// <summary>
    /// Checks if magnetic data is available and reliable for a location.
    /// </summary>
    public (bool available, string? reason) CheckAvailability(Coordinate coordinate)
    {
        if (Math.Abs(coordinate.LatitudeDeg) > 80)
            return (false, "Latitude > ±80° - reduced magnetic model accuracy");

        var (from, to) = WMM2025.GetValidityPeriod();
        if (_calculationDate < from || _calculationDate > to)
            return (false, $"Date outside WMM2025 validity period ({from:yyyy}-{to:yyyy})");

        return (true, null);
    }
}

/// <summary>
/// Represents magnetic variation data for a location.
/// </summary>
public record MagneticVariation
{
    /// <summary>
    /// Magnetic declination in degrees (positive = East, negative = West).
    /// </summary>
    public required double Declination { get; init; }

    /// <summary>
    /// Whether the calculation is within high confidence bounds.
    /// </summary>
    public required bool HighConfidence { get; init; }

    /// <summary>
    /// Date used for the calculation.
    /// </summary>
    public required DateTime CalculationDate { get; init; }

    /// <summary>
    /// Latitude where declination was calculated.
    /// </summary>
    public required double Latitude { get; init; }

    /// <summary>
    /// Longitude where declination was calculated.
    /// </summary>
    public required double Longitude { get; init; }

    /// <summary>
    /// Formatted declination string with direction.
    /// </summary>
    public string FormattedDeclination => WMM2025.FormatDeclination(Declination);
}
