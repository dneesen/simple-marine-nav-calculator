using FluentAssertions;
using MarineNav.Utility.Geodesy;
using MarineNav.Utility.Models;

namespace MarineNav.Tests;

public class GeodesicCalculatorTests
{
    [Fact]
    public void CalculateInverse_SamePoint_ShouldReturnZeroDistance()
    {
        // Arrange
        var coord = new Coordinate(43.1234, -87.9876);

        // Act
        var result = GeodesicCalculator.CalculateInverse(coord, coord);

        // Assert
        result.DistanceMeters.Should().Be(0);
        result.Converged.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 0, 0, 1, 111319.49, 90.0)]  // 1 degree longitude at equator
    [InlineData(0, 0, 1, 0, 110574.39, 0.0)]   // 1 degree latitude at equator
    [InlineData(43.0, -87.0, 43.0, -86.0, 81540.0, 90.0)] // 1 degree longitude at 43°N
    [InlineData(45.0, 0.0, 45.0, 1.0, 78846.81, 90.0)] // 1 degree longitude at 45°N
    public void CalculateInverse_KnownDistances_ShouldBeAccurate(
        double lat1, double lon1, double lat2, double lon2, double expectedMeters, double expectedBearing)
    {
        // Arrange
        var from = new Coordinate(lat1, lon1);
        var to = new Coordinate(lat2, lon2);

        // Act
        var result = GeodesicCalculator.CalculateInverse(from, to);

        // Assert
        result.DistanceMeters.Should().BeApproximately(expectedMeters, 100); // Within 100 meters
        result.InitialAzimuthDeg.Should().BeApproximately(expectedBearing, 0.5); // Within 0.5 degrees
        result.Converged.Should().BeTrue();
    }

    [Fact]
    public void CalculateInverse_LongDistance_ShouldConverge()
    {
        // Arrange - New York to London
        var newYork = new Coordinate(40.7128, -74.0060);
        var london = new Coordinate(51.5074, -0.1278);

        // Act
        var result = GeodesicCalculator.CalculateInverse(newYork, london);

        // Assert
        result.DistanceMeters.Should().BeGreaterThan(5500000); // > 5500 km
        result.DistanceMeters.Should().BeLessThan(5600000); // < 5600 km
        result.DistanceNauticalMiles.Should().BeGreaterThan(2900); // > 2900 NM
        result.DistanceNauticalMiles.Should().BeLessThan(3100); // < 3100 NM
        result.Converged.Should().BeTrue();
        result.Method.Should().Contain("Vincenty");
    }

    [Fact]
    public void CalculateInverse_ShortDistance_ShouldBeAccurate()
    {
        // Arrange - Two points approximately 1 NM apart (1 NM = 1852m = 1/60 degree latitude)
        var from = new Coordinate(43.0, -87.0);
        var to = new Coordinate(43.0 + (1.0/60.0), -87.0); // 1 NM north

        // Act
        var result = GeodesicCalculator.CalculateInverse(from, to);

        // Assert
        result.DistanceNauticalMiles.Should().BeApproximately(1.0, 0.01);
        result.Converged.Should().BeTrue();
    }

    [Theory]
    [InlineData(43.0, -87.0, 44.0, -87.0, 0.0)]    // North
    [InlineData(43.0, -87.0, 43.0, -86.0, 90.0)]   // East
    [InlineData(43.0, -87.0, 42.0, -87.0, 180.0)]  // South
    [InlineData(43.0, -87.0, 43.0, -88.0, 270.0)]  // West
    public void CalculateInverse_CardinalDirections_ShouldReturnCorrectBearing(
        double lat1, double lon1, double lat2, double lon2, double expectedBearing)
    {
        // Arrange
        var from = new Coordinate(lat1, lon1);
        var to = new Coordinate(lat2, lon2);

        // Act
        var result = GeodesicCalculator.CalculateInverse(from, to);

        // Assert
        result.InitialAzimuthDeg.Should().BeApproximately(expectedBearing, 0.5);
        result.Converged.Should().BeTrue();
    }

    [Fact]
    public void CalculateInverse_NearAntipodalPoints_ShouldHandleCorrectly()
    {
        // Arrange - Points that are nearly opposite on the globe
        var north = new Coordinate(80.0, 0);
        var south = new Coordinate(-80.0, 179.0);

        // Act
        var result = GeodesicCalculator.CalculateInverse(north, south);

        // Assert - Should get a result (Vincenty or Haversine)
        result.DistanceMeters.Should().BeGreaterThan(15000000); // > 15,000 km
        result.Converged.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(45, 45)]
    [InlineData(90, 90)]
    [InlineData(180, 180)]
    [InlineData(270, 270)]
    [InlineData(359.9, 359.9)]
    [InlineData(-10, 350)]
    [InlineData(370, 10)]
    [InlineData(-90, 270)]
    public void NormalizeAzimuth_VariousInputs_ShouldNormalizeTo0_360(double input, double expected)
    {
        // Act
        double result = GeodesicCalculator.NormalizeAzimuth(input);

        // Assert
        result.Should().BeApproximately(expected, 0.001);
    }

    [Fact]
    public void CalculateInverse_RoundTrip_ShouldBeConsistent()
    {
        // Arrange
        var pointA = new Coordinate(43.1234, -87.9876);
        var pointB = new Coordinate(45.6789, -85.4321);

        // Act
        var resultAtoB = GeodesicCalculator.CalculateInverse(pointA, pointB);
        var resultBtoA = GeodesicCalculator.CalculateInverse(pointB, pointA);

        // Assert
        resultAtoB.DistanceMeters.Should().BeApproximately(resultBtoA.DistanceMeters, 0.01);
        
        // Reverse bearing should be approximately 180 degrees different
        double bearingDiff = Math.Abs(resultAtoB.InitialAzimuthDeg - resultBtoA.InitialAzimuthDeg);
        if (bearingDiff > 180)
            bearingDiff = 360 - bearingDiff;
        bearingDiff.Should().BeApproximately(180, 2.0); // Within 2 degrees due to geodesic curvature
    }
}
