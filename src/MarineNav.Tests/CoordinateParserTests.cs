using FluentAssertions;
using MarineNav.Utility.Models;
using MarineNav.Utility.Parsing;

namespace MarineNav.Tests;

public class CoordinateParserTests
{
    [Theory]
    [InlineData("43.1234", true, 43.1234)]
    [InlineData("-87.9876", false, -87.9876)]
    [InlineData("43.1234N", true, 43.1234)]
    [InlineData("43.1234S", true, -43.1234)]
    [InlineData("87.9876W", false, -87.9876)]
    [InlineData("87.9876E", false, 87.9876)]
    [InlineData("43.1234°", true, 43.1234)]
    [InlineData("43.1234° N", true, 43.1234)]
    [InlineData("0", true, 0)]
    [InlineData("90", true, 90)]
    [InlineData("-90", true, -90)]
    [InlineData("180", false, 180)]
    [InlineData("-180", false, -180)]
    public void Parse_DecimalDegrees_ShouldParseCorrectly(string input, bool isLatitude, double expected)
    {
        // Act
        double result = CoordinateParser.Parse(input, isLatitude);

        // Assert
        result.Should().BeApproximately(expected, 1e-6);
    }

    [Theory]
    [InlineData("43 07.404", 43.1234)]
    [InlineData("43° 07.404′", 43.1234)]
    [InlineData("43 07.404 N", 43.1234)]
    [InlineData("43° 07.404′ S", -43.1234)]
    [InlineData("87 59.256", 87.9876)]
    [InlineData("87° 59.256′ W", -87.9876)]
    [InlineData("0 0", 0)]
    [InlineData("0 30", 0.5)]
    [InlineData("45 30.0", 45.5)]
    [InlineData("90 0", 90)]
    public void Parse_DegreesDecimalMinutes_ShouldParseCorrectly(string input, double expected)
    {
        // Act
        double result = CoordinateParser.Parse(input, true);

        // Assert
        result.Should().BeApproximately(expected, 1e-6);
    }

    [Theory]
    [InlineData("43 7 24.24", 43.1234)]
    [InlineData("43° 07′ 24.24″", 43.1234)]
    [InlineData("43 7 24.24 N", 43.1234)]
    [InlineData("43° 07′ 24.24″ S", -43.1234)]
    [InlineData("87 59 15.36", 87.9876)]
    [InlineData("87° 59′ 15.36″ W", -87.9876)]
    [InlineData("0 0 0", 0)]
    [InlineData("0 0 30", 0.00833333)]
    [InlineData("45 30 0", 45.5)]
    [InlineData("45 30 30", 45.508333)]
    public void Parse_DegreesMinutesSeconds_ShouldParseCorrectly(string input, double expected)
    {
        // Act
        double result = CoordinateParser.Parse(input, true);

        // Assert
        result.Should().BeApproximately(expected, 1e-5);
    }

    [Theory]
    [InlineData("43 07.404 N", "087 54.608 W")]
    [InlineData("43.1234N", "87.9101W")]
    [InlineData("43° 07′ 24.2″ N", "87° 54′ 36.5″ W")]
    [InlineData("43 7 24.2", "-87 54 36.5")]
    public void TryParseCoordinate_ValidInputs_ShouldReturnTrue(string latInput, string lonInput)
    {
        // Act
        bool result = CoordinateParser.TryParseCoordinate(latInput, lonInput, out Coordinate? coord);

        // Assert
        result.Should().BeTrue();
        coord.Should().NotBeNull();
        coord!.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("abc")]
    public void Parse_InvalidInput_ShouldThrowFormatException(string input)
    {
        // Act & Assert
        Action act = () => CoordinateParser.Parse(input, true);
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("91", true)]  // Latitude > 90
    [InlineData("-91", true)] // Latitude < -90
    [InlineData("181", false)] // Longitude > 180
    [InlineData("-181", false)] // Longitude < -180
    [InlineData("100 0", true)] // Latitude > 90 in DM format
    public void Parse_OutOfRange_ShouldThrowFormatException(string input, bool isLatitude)
    {
        // Act & Assert
        Action act = () => CoordinateParser.Parse(input, isLatitude);
        act.Should().Throw<FormatException>().WithMessage("*out of range*");
    }

    [Theory]
    [InlineData("43 60", true)] // Minutes = 60
    [InlineData("43 -5", true)] // Negative minutes
    [InlineData("43 7 60", true)] // Seconds = 60
    [InlineData("43 7 -5", true)] // Negative seconds
    public void Parse_InvalidMinutesOrSeconds_ShouldThrowFormatException(string input, bool isLatitude)
    {
        // Act & Assert
        Action act = () => CoordinateParser.Parse(input, isLatitude);
        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("43,07,24.24", 43.1234)] // Comma delimiters
    [InlineData("43\t7\t24.24", 43.1234)] // Tab delimiters
    [InlineData("  43  7  24.24  ", 43.1234)] // Extra whitespace
    public void Parse_VariousDelimiters_ShouldParseCorrectly(string input, double expected)
    {
        // Act
        double result = CoordinateParser.Parse(input, true);

        // Assert
        result.Should().BeApproximately(expected, 1e-5);
    }

    [Theory]
    [InlineData("N43.1234", true, 43.1234)] // Leading hemisphere
    [InlineData("S43.1234", true, -43.1234)]
    [InlineData("E87.9876", false, 87.9876)]
    [InlineData("W87.9876", false, -87.9876)]
    public void Parse_LeadingHemisphere_ShouldParseCorrectly(string input, bool isLatitude, double expected)
    {
        // Act
        double result = CoordinateParser.Parse(input, isLatitude);

        // Assert
        result.Should().BeApproximately(expected, 1e-6);
    }

    [Theory]
    [InlineData("-43.1234N", 43.1234)] // Negative sign with N should give positive (N takes precedence)
    [InlineData("-43.1234S", -43.1234)] // Both negative sign and S give negative
    public void Parse_NegativeSignWithHemisphere_ShouldRespectHemisphere(string input, double expected)
    {
        // Act
        double result = CoordinateParser.Parse(input, true);

        // Assert
        result.Should().BeApproximately(expected, 1e-6);
    }

    [Fact]
    public void Parse_EdgeCases_ShouldHandleCorrectly()
    {
        // Zero values
        CoordinateParser.Parse("0", true).Should().Be(0);
        CoordinateParser.Parse("0 0", true).Should().Be(0);
        CoordinateParser.Parse("0 0 0", true).Should().Be(0);

        // Boundary values
        CoordinateParser.Parse("90", true).Should().Be(90);
        CoordinateParser.Parse("-90", true).Should().Be(-90);
        CoordinateParser.Parse("180", false).Should().Be(180);
        CoordinateParser.Parse("-180", false).Should().Be(-180);

        // High precision
        CoordinateParser.Parse("43.123456789", true).Should().BeApproximately(43.123456789, 1e-9);
    }
}
