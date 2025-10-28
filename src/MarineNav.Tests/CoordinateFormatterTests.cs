using FluentAssertions;
using MarineNav.Utility.Models;
using MarineNav.Utility.Parsing;

namespace MarineNav.Tests;

public class CoordinateFormatterTests
{
    private readonly CoordinateFormatter _formatter = new();

    [Theory]
    [InlineData(43.1234, true, "43.1234° N")]
    [InlineData(-43.1234, true, "43.1234° S")]
    [InlineData(87.9876, false, "87.9876° E")]
    [InlineData(-87.9876, false, "87.9876° W")]
    [InlineData(0, true, "0.0000° N")]
    [InlineData(90, true, "90.0000° N")]
    [InlineData(-90, true, "90.0000° S")]
    public void Format_DecimalDegrees_ShouldFormatCorrectly(double value, bool isLatitude, string expected)
    {
        // Act
        string result = _formatter.Format(value, isLatitude, CoordinateFormat.DecimalDegrees);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(43.1234, true, "043° 07.404′ N")]
    [InlineData(-43.1234, true, "043° 07.404′ S")]
    [InlineData(87.9876, false, "087° 59.256′ E")]
    [InlineData(-87.9876, false, "087° 59.256′ W")]
    [InlineData(0, true, "000° 00.000′ N")]
    [InlineData(0.5, true, "000° 30.000′ N")]
    [InlineData(45.5, true, "045° 30.000′ N")]
    public void Format_DegreesDecimalMinutes_ShouldFormatCorrectly(double value, bool isLatitude, string expected)
    {
        // Act
        string result = _formatter.Format(value, isLatitude, CoordinateFormat.DegreesDecimalMinutes);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(43.1234, true, "043° 07′ 24.2″ N")]
    [InlineData(-43.1234, true, "043° 07′ 24.2″ S")]
    [InlineData(87.9876, false, "087° 59′ 15.4″ E")]
    [InlineData(-87.9876, false, "087° 59′ 15.4″ W")]
    [InlineData(0, true, "000° 00′ 0.0″ N")]
    [InlineData(0.5, true, "000° 30′ 0.0″ N")]
    [InlineData(45.508333, true, "045° 30′ 30.0″ N")]
    public void Format_DegreesMinutesSeconds_ShouldFormatCorrectly(double value, bool isLatitude, string expected)
    {
        // Act
        string result = _formatter.Format(value, isLatitude, CoordinateFormat.DegreesMinutesSeconds);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Format_Coordinate_ShouldFormatBothLatitudeAndLongitude()
    {
        // Arrange
        var coord = new Coordinate(43.1234, -87.9876);

        // Act
        var (lat, lon) = _formatter.Format(coord, CoordinateFormat.DecimalDegrees);

        // Assert
        lat.Should().Be("43.1234° N");
        lon.Should().Be("87.9876° W");
    }

    [Fact]
    public void Format_WithoutHemisphere_ShouldOmitHemisphere()
    {
        // Act
        string result = _formatter.Format(43.1234, true, CoordinateFormat.DecimalDegrees, includeHemisphere: false);

        // Assert
        result.Should().Be("43.1234°");
    }

    [Theory]
    [InlineData(89.999999, true, CoordinateFormat.DegreesMinutesSeconds, "090° 00′ 0.0″ N")] // Rounding edge case
    public void Format_EdgeCaseRounding_ShouldHandleCorrectly(double value, bool isLatitude, CoordinateFormat format, string expected)
    {
        // Act
        string result = _formatter.Format(value, isLatitude, format);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Convert_ShouldParseAndReformat()
    {
        // Arrange
        string input = "43 7 24.24";

        // Act
        string result = _formatter.Convert(input, true, CoordinateFormat.DecimalDegrees);

        // Assert
        result.Should().Be("43.1234° N");
    }

    [Theory]
    [InlineData(0.00001, true, CoordinateFormat.DecimalDegrees, "0.0000° N")] // Very small positive
    [InlineData(-0.00001, true, CoordinateFormat.DecimalDegrees, "0.0000° S")] // Very small negative
    public void Format_VerySmallValues_ShouldFormatCorrectly(double value, bool isLatitude, CoordinateFormat format, string expected)
    {
        // Act
        string result = _formatter.Format(value, isLatitude, format);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Format_CustomPrecision_ShouldRespectSettings()
    {
        // Arrange
        var precision = new CoordinatePrecision
        {
            DecimalDegrees = 2,
            DecimalMinutes = 1,
            Seconds = 0
        };
        var formatter = new CoordinateFormatter(precision);

        // Act
        string dd = formatter.Format(43.1234, true, CoordinateFormat.DecimalDegrees);
        string dm = formatter.Format(43.1234, true, CoordinateFormat.DegreesDecimalMinutes);
        string dms = formatter.Format(43.1234, true, CoordinateFormat.DegreesMinutesSeconds);

        // Assert
        dd.Should().Be("43.12° N");
        dm.Should().Be("043° 07.4′ N");
        dms.Should().Be("043° 07′ 24″ N");
    }

    [Fact]
    public void Format_RoundTrip_ShouldMaintainPrecision()
    {
        // Arrange
        double originalLat = 43.123456;
        double originalLon = -87.987654;

        // Act - format and parse back
        string formattedLat = _formatter.Format(originalLat, true, CoordinateFormat.DecimalDegrees);
        string formattedLon = _formatter.Format(originalLon, false, CoordinateFormat.DecimalDegrees);
        
        double parsedLat = CoordinateParser.Parse(formattedLat, true);
        double parsedLon = CoordinateParser.Parse(formattedLon, false);

        // Assert - should be within the precision limits
        parsedLat.Should().BeApproximately(originalLat, 1e-4);
        parsedLon.Should().BeApproximately(originalLon, 1e-4);
    }
}
