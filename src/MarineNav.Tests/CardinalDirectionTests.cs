using FluentAssertions;
using MarineNav.Utility.Geodesy;

namespace MarineNav.Tests;

public class CardinalDirectionTests
{
    [Theory]
    [InlineData(0, "N")]
    [InlineData(22.5, "NNE")]
    [InlineData(45, "NE")]
    [InlineData(67.5, "ENE")]
    [InlineData(90, "E")]
    [InlineData(112.5, "ESE")]
    [InlineData(135, "SE")]
    [InlineData(157.5, "SSE")]
    [InlineData(180, "S")]
    [InlineData(202.5, "SSW")]
    [InlineData(225, "SW")]
    [InlineData(247.5, "WSW")]
    [InlineData(270, "W")]
    [InlineData(292.5, "WNW")]
    [InlineData(315, "NW")]
    [InlineData(337.5, "NNW")]
    [InlineData(360, "N")]
    public void FromBearing_ExactCardinalPoints_ShouldReturnCorrectDirection(double bearing, string expected)
    {
        // Act
        string result = CardinalDirection.FromBearing(bearing);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(11.24, "N")]  // Just before NNE boundary
    [InlineData(11.26, "NNE")] // Just after NNE boundary
    [InlineData(33.74, "NNE")] // Just before NE boundary
    [InlineData(33.76, "NE")]  // Just after NE boundary
    public void FromBearing_BoundaryConditions_ShouldHandleCorrectly(double bearing, string expected)
    {
        // Act
        string result = CardinalDirection.FromBearing(bearing);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(-10, "N")]
    [InlineData(370, "N")]
    [InlineData(-45, "NW")]
    [InlineData(405, "NE")]
    public void FromBearing_OutOfRange_ShouldNormalize(double bearing, string expected)
    {
        // Act
        string result = CardinalDirection.FromBearing(bearing);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetAll16Points_ShouldReturn16Directions()
    {
        // Act
        var directions = CardinalDirection.GetAll16Points();

        // Assert
        directions.Should().HaveCount(16);
        directions.Should().Contain("N");
        directions.Should().Contain("NNE");
        directions.Should().Contain("NE");
        directions.Should().Contain("E");
        directions.Should().Contain("SE");
        directions.Should().Contain("S");
        directions.Should().Contain("SW");
        directions.Should().Contain("W");
        directions.Should().Contain("NW");
    }

    [Theory]
    [InlineData("N", 348.75, 11.25)]
    [InlineData("E", 78.75, 101.25)]
    [InlineData("S", 168.75, 191.25)]
    [InlineData("W", 258.75, 281.25)]
    [InlineData("NE", 33.75, 56.25)]
    [InlineData("SW", 213.75, 236.25)]
    public void GetBearingRange_ValidCardinal_ShouldReturnCorrectRange(string cardinal, double expectedMin, double expectedMax)
    {
        // Act
        var (min, max) = CardinalDirection.GetBearingRange(cardinal);

        // Assert
        min.Should().BeApproximately(expectedMin, 0.01);
        max.Should().BeApproximately(expectedMax, 0.01);
    }

    [Fact]
    public void GetBearingRange_InvalidCardinal_ShouldThrowException()
    {
        // Act & Assert
        Action act = () => CardinalDirection.GetBearingRange("INVALID");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(1.5, "N")]
    [InlineData(23.0, "NNE")]
    [InlineData(44.0, "NE")]
    [InlineData(91.0, "E")]
    [InlineData(179.0, "S")]
    [InlineData(269.0, "W")]
    public void FromBearing_RealWorldBearings_ShouldReturnCorrectCardinal(double bearing, string expected)
    {
        // Act
        string result = CardinalDirection.FromBearing(bearing);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FromBearing_AllDirections_ShouldCover360Degrees()
    {
        // Arrange
        var directions = new HashSet<string>();

        // Act - Test every degree
        for (int bearing = 0; bearing < 360; bearing++)
        {
            string direction = CardinalDirection.FromBearing(bearing);
            directions.Add(direction);
        }

        // Assert - Should have seen all 16 directions
        directions.Should().HaveCount(16);
    }
}
