using FluentAssertions;
using MarineNav.Utility.Models;
using MarineNav.Utility.Services;

namespace MarineNav.Tests;

public class RouteCalculationServiceTests
{
    [Fact]
    public void CalculateLegs_TwoWaypoints_ShouldCreateOneLeg()
    {
        // Arrange
        var waypoints = new List<Waypoint>
        {
            Waypoint.Create("Start", new Coordinate(43.0, -87.0)),
            Waypoint.Create("End", new Coordinate(44.0, -87.0))
        };

        // Act
        var legs = RouteCalculationService.CalculateLegs(waypoints);

        // Assert
        legs.Should().HaveCount(1);
        legs[0].From.Name.Should().Be("Start");
        legs[0].To.Name.Should().Be("End");
        legs[0].DistanceNm.Should().BeGreaterThan(50); // About 60 NM
        legs[0].TrueBearingDeg.Should().BeApproximately(0, 1); // Heading North
        legs[0].Cardinal16.Should().Be("N");
    }

    [Fact]
    public void CalculateLegs_ThreeWaypoints_ShouldCreateTwoLegs()
    {
        // Arrange
        var waypoints = new List<Waypoint>
        {
            Waypoint.Create("A", new Coordinate(43.0, -87.0)),
            Waypoint.Create("B", new Coordinate(43.0, -86.0)),
            Waypoint.Create("C", new Coordinate(44.0, -86.0))
        };

        // Act
        var legs = RouteCalculationService.CalculateLegs(waypoints);

        // Assert
        legs.Should().HaveCount(2);
        legs[0].From.Name.Should().Be("A");
        legs[0].To.Name.Should().Be("B");
        legs[1].From.Name.Should().Be("B");
        legs[1].To.Name.Should().Be("C");
    }

    [Fact]
    public void CalculateLegs_WithSpeed_ShouldCalculateLegTime()
    {
        // Arrange
        var waypoints = new List<Waypoint>
        {
            Waypoint.Create("Start", new Coordinate(43.0, -87.0)),
            Waypoint.Create("End", new Coordinate(43.0 + (1.0/60.0), -87.0)) // 1 NM north
        };
        double speedKnots = 6.0; // 6 knots

        // Act
        var legs = RouteCalculationService.CalculateLegs(waypoints, speedKnots);

        // Assert
        legs[0].LegTime.Should().NotBeNull();
        legs[0].LegTime!.Value.TotalMinutes.Should().BeApproximately(10, 1); // 1 NM at 6 knots = 10 minutes
    }

    [Fact]
    public void CalculateLegs_WithSpeedAndStartTime_ShouldCalculateETA()
    {
        // Arrange
        var waypoints = new List<Waypoint>
        {
            Waypoint.Create("Start", new Coordinate(43.0, -87.0)),
            Waypoint.Create("Mid", new Coordinate(43.0 + (1.0/60.0), -87.0)), // 1 NM
            Waypoint.Create("End", new Coordinate(43.0 + (2.0/60.0), -87.0))  // 2 NM total
        };
        double speedKnots = 6.0;
        var startTime = new DateTimeOffset(2025, 10, 27, 10, 0, 0, TimeSpan.Zero);

        // Act
        var legs = RouteCalculationService.CalculateLegs(waypoints, speedKnots, startTime);

        // Assert
        legs[0].ETA.Should().NotBeNull();
        legs[0].ETA!.Value.Should().BeCloseTo(startTime.AddMinutes(10), TimeSpan.FromSeconds(30));
        
        legs[1].ETA.Should().NotBeNull();
        legs[1].ETA!.Value.Should().BeCloseTo(startTime.AddMinutes(20), TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void CreateRoute_ShouldBuildCompleteRoute()
    {
        // Arrange
        var waypoints = new List<Waypoint>
        {
            Waypoint.Create("A", new Coordinate(43.0, -87.0)),
            Waypoint.Create("B", new Coordinate(44.0, -87.0)),
            Waypoint.Create("C", new Coordinate(44.0, -86.0))
        };

        // Act
        var route = RouteCalculationService.CreateRoute("Test Route", waypoints);

        // Assert
        route.Name.Should().Be("Test Route");
        route.Waypoints.Should().HaveCount(3);
        route.Legs.Should().HaveCount(2);
        route.TotalDistanceNm.Should().BeGreaterThan(100); // Sum of both legs
    }

    [Fact]
    public void CreateRoute_WithSpeedAndTime_ShouldIncludeTiming()
    {
        // Arrange
        var waypoints = new List<Waypoint>
        {
            Waypoint.Create("A", new Coordinate(43.0, -87.0)),
            Waypoint.Create("B", new Coordinate(44.0, -87.0))
        };
        var startTime = new DateTimeOffset(2025, 10, 27, 8, 0, 0, TimeSpan.Zero);

        // Act
        var route = RouteCalculationService.CreateRoute("Timed Route", waypoints, 10.0, startTime);

        // Assert
        route.TotalTime.Should().NotBeNull();
        route.FinalETA.Should().NotBeNull();
        route.FinalETA!.Value.Should().BeAfter(startTime);
    }

    [Fact]
    public void RecalculateRoute_WithNewSpeed_ShouldUpdateTiming()
    {
        // Arrange
        var waypoints = new List<Waypoint>
        {
            Waypoint.Create("A", new Coordinate(43.0, -87.0)),
            Waypoint.Create("B", new Coordinate(44.0, -87.0))
        };
        var route = RouteCalculationService.CreateRoute("Test", waypoints, 5.0);
        
        // Act
        var recalculated = RouteCalculationService.RecalculateRoute(route, 10.0);

        // Assert
        recalculated.Legs[0].LegTime.Should().NotBeNull();
        recalculated.Legs[0].LegTime!.Value.Should().BeLessThan(route.Legs[0].LegTime!.Value);
    }

    [Fact]
    public void CalculateToWaypoint_ShouldReturnDistanceAndBearing()
    {
        // Arrange
        var currentPosition = new Coordinate(43.0, -87.0);
        var targetWaypoint = Waypoint.Create("Target", new Coordinate(44.0, -87.0));

        // Act
        var (distance, bearing, cardinal, eta) = RouteCalculationService.CalculateToWaypoint(
            currentPosition, targetWaypoint);

        // Assert
        distance.Should().BeGreaterThan(50);
        bearing.Should().BeApproximately(0, 1);
        cardinal.Should().Be("N");
        eta.Should().BeNull(); // No speed/time provided
    }

    [Fact]
    public void CalculateToWaypoint_WithSpeedAndTime_ShouldCalculateETA()
    {
        // Arrange
        var currentPosition = new Coordinate(43.0, -87.0);
        var targetWaypoint = Waypoint.Create("Target", new Coordinate(43.0 + (1.0/60.0), -87.0)); // 1 NM
        var currentTime = new DateTimeOffset(2025, 10, 27, 12, 0, 0, TimeSpan.Zero);

        // Act
        var (distance, bearing, cardinal, eta) = RouteCalculationService.CalculateToWaypoint(
            currentPosition, targetWaypoint, 6.0, currentTime);

        // Assert
        distance.Should().BeApproximately(1.0, 0.01);
        eta.Should().NotBeNull();
        eta!.Value.Should().BeCloseTo(currentTime.AddMinutes(10), TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void CalculateLegs_EmptyWaypoints_ShouldReturnEmptyLegs()
    {
        // Arrange
        var waypoints = new List<Waypoint>();

        // Act
        var legs = RouteCalculationService.CalculateLegs(waypoints);

        // Assert
        legs.Should().BeEmpty();
    }

    [Fact]
    public void CalculateLegs_SingleWaypoint_ShouldReturnEmptyLegs()
    {
        // Arrange
        var waypoints = new List<Waypoint>
        {
            Waypoint.Create("Only", new Coordinate(43.0, -87.0))
        };

        // Act
        var legs = RouteCalculationService.CalculateLegs(waypoints);

        // Assert
        legs.Should().BeEmpty();
    }

    [Fact]
    public void Route_TotalDistanceNm_ShouldSumAllLegs()
    {
        // Arrange
        var waypoints = new List<Waypoint>
        {
            Waypoint.Create("A", new Coordinate(43.0, -87.0)),
            Waypoint.Create("B", new Coordinate(44.0, -87.0)),
            Waypoint.Create("C", new Coordinate(45.0, -87.0))
        };

        // Act
        var route = RouteCalculationService.CreateRoute("Test", waypoints);

        // Assert
        double expectedTotal = route.Legs.Sum(l => l.DistanceNm);
        route.TotalDistanceNm.Should().BeApproximately(expectedTotal, 0.01);
    }
}
