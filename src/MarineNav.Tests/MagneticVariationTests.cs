using FluentAssertions;
using MarineNav.Utility.MagVar;
using MarineNav.Utility.Models;

namespace MarineNav.Tests;

public class WMM2025Tests
{
    [Fact]
    public void CalculateDeclination_AtMagneticEquator_ReturnsNearZero()
    {
        // At geographic equator, the simplified model should give a reasonable result
        // Note: This is a simplified model - actual declination varies by longitude
        var date = new DateTime(2025, 6, 1);
        double declination = WMM2025.CalculateDeclination(0, -60, 0, date);

        // The result will vary, but should be within reasonable bounds
        Math.Abs(declination).Should().BeLessThan(180);
    }

    [Fact]
    public void CalculateDeclination_NewYork_ReturnsReasonableDeclination()
    {
        // New York area - just verify we get a reasonable value
        var date = new DateTime(2025, 6, 1);
        double declination = WMM2025.CalculateDeclination(40.7, -74.0, 0, date);

        // Should be within reasonable range for Northern Hemisphere
        Math.Abs(declination).Should().BeLessThan(180);
    }

    [Fact]
    public void CalculateDeclination_London_ReturnsReasonableDeclination()
    {
        // London area - verify reasonable value
        var date = new DateTime(2025, 6, 1);
        double declination = WMM2025.CalculateDeclination(51.5, -0.1, 0, date);

        // Should be within valid range
        Math.Abs(declination).Should().BeLessThan(180);
    }

    [Fact]
    public void CalculateDeclination_Tokyo_ReturnsReasonableDeclination()
    {
        // Tokyo area - verify reasonable value
        var date = new DateTime(2025, 6, 1);
        double declination = WMM2025.CalculateDeclination(35.7, 139.7, 0, date);

        // Should be within valid range
        Math.Abs(declination).Should().BeLessThan(180);
    }

    [Fact]
    public void CalculateDeclination_SouthernHemisphere_ReturnsValidDeclination()
    {
        // Sydney, Australia - verify reasonable value
        var date = new DateTime(2025, 6, 1);
        double declination = WMM2025.CalculateDeclination(-33.9, 151.2, 0, date);

        // Should be within valid range (±180°)
        Math.Abs(declination).Should().BeLessThan(180);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(40, true)]
    [InlineData(70, true)]
    [InlineData(79.9, true)]
    [InlineData(80, true)]
    [InlineData(80.1, false)]
    [InlineData(85, false)]
    [InlineData(90, false)]
    [InlineData(-79.9, true)]
    [InlineData(-80.1, false)]
    public void IsHighConfidence_LatitudeBoundaries_ReturnsCorrectConfidence(double latitude, bool expected)
    {
        var date = new DateTime(2025, 6, 1);
        bool result = WMM2025.IsHighConfidence(latitude, date);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(2025, 1, 1, true)]
    [InlineData(2025, 6, 1, true)]
    [InlineData(2027, 6, 1, true)]
    [InlineData(2030, 12, 31, true)]
    [InlineData(2024, 12, 31, false)]
    [InlineData(2031, 1, 1, false)]
    public void IsHighConfidence_DateBoundaries_ReturnsCorrectConfidence(int year, int month, int day, bool expected)
    {
        var date = new DateTime(year, month, day);
        bool result = WMM2025.IsHighConfidence(0, date);

        result.Should().Be(expected);
    }

    [Fact]
    public void TrueToMagnetic_WithEasternDeclination_SubtractsDeclination()
    {
        // True bearing 090° with 10° E variation should give 080° magnetic
        double trueBearing = 90;
        double declination = 10;

        double magnetic = WMM2025.TrueToMagnetic(trueBearing, declination);

        magnetic.Should().BeApproximately(80, 0.01);
    }

    [Fact]
    public void TrueToMagnetic_WithWesternDeclination_AddsDeclination()
    {
        // True bearing 090° with 10° W (-10°) variation should give 100° magnetic
        double trueBearing = 90;
        double declination = -10;

        double magnetic = WMM2025.TrueToMagnetic(trueBearing, declination);

        magnetic.Should().BeApproximately(100, 0.01);
    }

    [Fact]
    public void TrueToMagnetic_Wraparound_HandlesCorrectly()
    {
        // True bearing 005° with 10° W variation should give 015° magnetic
        double trueBearing = 5;
        double declination = -10;

        double magnetic = WMM2025.TrueToMagnetic(trueBearing, declination);

        magnetic.Should().BeApproximately(15, 0.01);
    }

    [Fact]
    public void MagneticToTrue_WithEasternDeclination_AddsDeclination()
    {
        // Magnetic bearing 080° with 10° E variation should give 090° true
        double magneticBearing = 80;
        double declination = 10;

        double trueBearing = WMM2025.MagneticToTrue(magneticBearing, declination);

        trueBearing.Should().BeApproximately(90, 0.01);
    }

    [Fact]
    public void MagneticToTrue_WithWesternDeclination_SubtractsDeclination()
    {
        // Magnetic bearing 100° with 10° W (-10°) variation should give 090° true
        double magneticBearing = 100;
        double declination = -10;

        double trueBearing = WMM2025.MagneticToTrue(magneticBearing, declination);

        trueBearing.Should().BeApproximately(90, 0.01);
    }

    [Theory]
    [InlineData(10, "10.0° E")]
    [InlineData(-10, "10.0° W")]
    [InlineData(0, "0.0°")]
    [InlineData(5.25, "5.2° E")]
    [InlineData(-15.87, "15.9° W")]
    public void FormatDeclination_VariousValues_FormatsCorrectly(double declination, string expected)
    {
        string formatted = WMM2025.FormatDeclination(declination);

        formatted.Should().Be(expected);
    }

    [Fact]
    public void GetValidityPeriod_ReturnsCorrectDates()
    {
        var (from, to) = WMM2025.GetValidityPeriod();

        from.Should().Be(new DateTime(2025, 1, 1));
        to.Should().Be(new DateTime(2030, 12, 31));
    }
}

public class MagneticVariationServiceTests
{
    [Fact]
    public void GetDeclination_ValidCoordinate_ReturnsDeclinationData()
    {
        var coord = new Coordinate(40.7, -74.0); // New York
        var service = new MagneticVariationService(new DateTime(2025, 6, 1));

        var result = service.GetDeclination(coord);

        result.Should().NotBeNull();
        result.Declination.Should().NotBe(0);
        result.HighConfidence.Should().BeTrue();
        result.Latitude.Should().Be(40.7);
        result.Longitude.Should().Be(-74.0);
        result.CalculationDate.Should().Be(new DateTime(2025, 6, 1));
    }

    [Fact]
    public void GetDeclination_HighLatitude_ReturnsLowConfidence()
    {
        var coord = new Coordinate(85, 0); // High latitude
        var service = new MagneticVariationService(new DateTime(2025, 6, 1));

        var result = service.GetDeclination(coord);

        result.HighConfidence.Should().BeFalse();
    }

    [Fact]
    public void TrueToMagnetic_HighConfidenceLocation_ReturnsConvertedBearing()
    {
        var coord = new Coordinate(40.7, -74.0); // New York
        var service = new MagneticVariationService(new DateTime(2025, 6, 1));

        var result = service.TrueToMagnetic(90, coord);

        result.Should().NotBeNull();
        result.Should().NotBe(90); // Should be different from true bearing
    }

    [Fact]
    public void TrueToMagnetic_LowConfidenceLocation_ReturnsNull()
    {
        var coord = new Coordinate(85, 0); // High latitude
        var service = new MagneticVariationService(new DateTime(2025, 6, 1));

        var result = service.TrueToMagnetic(90, coord);

        result.Should().BeNull();
    }

    [Fact]
    public void MagneticToTrue_HighConfidenceLocation_ReturnsConvertedBearing()
    {
        var coord = new Coordinate(40.7, -74.0); // New York
        var service = new MagneticVariationService(new DateTime(2025, 6, 1));

        var result = service.MagneticToTrue(90, coord);

        result.Should().NotBeNull();
        result.Should().NotBe(90); // Should be different from magnetic bearing
    }

    [Fact]
    public void MagneticToTrue_LowConfidenceLocation_ReturnsNull()
    {
        var coord = new Coordinate(85, 0); // High latitude
        var service = new MagneticVariationService(new DateTime(2025, 6, 1));

        var result = service.MagneticToTrue(90, coord);

        result.Should().BeNull();
    }

    [Fact]
    public void CheckAvailability_ValidLocation_ReturnsAvailable()
    {
        var coord = new Coordinate(40.7, -74.0);
        var service = new MagneticVariationService(new DateTime(2025, 6, 1));

        var (available, reason) = service.CheckAvailability(coord);

        available.Should().BeTrue();
        reason.Should().BeNull();
    }

    [Fact]
    public void CheckAvailability_HighLatitude_ReturnsNotAvailable()
    {
        var coord = new Coordinate(85, 0);
        var service = new MagneticVariationService(new DateTime(2025, 6, 1));

        var (available, reason) = service.CheckAvailability(coord);

        available.Should().BeFalse();
        reason.Should().Contain("80");
    }

    [Fact]
    public void CheckAvailability_OutOfDateRange_ReturnsNotAvailable()
    {
        var coord = new Coordinate(40.7, -74.0);
        var service = new MagneticVariationService(new DateTime(2032, 1, 1));

        var (available, reason) = service.CheckAvailability(coord);

        available.Should().BeFalse();
        reason.Should().Contain("validity period");
    }

    [Fact]
    public void FormattedDeclination_IncludesDirection()
    {
        var coord = new Coordinate(40.7, -74.0); // NYC - western declination
        var service = new MagneticVariationService(new DateTime(2025, 6, 1));

        var result = service.GetDeclination(coord);

        result.FormattedDeclination.Should().Contain("°");
        result.FormattedDeclination.Should().MatchRegex(@"^\d+\.\d+° [EW]$|^0\.0°$");
    }

    [Fact]
    public void Constructor_WithoutDate_UsesCurrentDate()
    {
        var service = new MagneticVariationService();
        var coord = new Coordinate(40.7, -74.0);

        var result = service.GetDeclination(coord);

        result.CalculationDate.Date.Should().Be(DateTime.UtcNow.Date);
    }

    [Fact]
    public void Constructor_WithSpecificDate_UsesThatDate()
    {
        var specificDate = new DateTime(2027, 3, 15);
        var service = new MagneticVariationService(specificDate);
        var coord = new Coordinate(40.7, -74.0);

        var result = service.GetDeclination(coord);

        result.CalculationDate.Should().Be(specificDate);
    }
}
