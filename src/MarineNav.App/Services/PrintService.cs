using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using MarineNav.Utility.Models;

namespace MarineNav.App.Services;

/// <summary>
/// Service for generating printable FlowDocuments from route data.
/// </summary>
public class PrintService
{
    /// <summary>
    /// Creates a FlowDocument containing a formatted route summary.
    /// </summary>
    /// <param name="route">The route to print.</param>
    /// <param name="routeName">Optional name for the route.</param>
    /// <returns>A FlowDocument ready for printing.</returns>
    public static FlowDocument CreateRouteDocument(Route route, string? routeName = null)
    {
        var document = new FlowDocument
        {
            PagePadding = new Thickness(50),
            ColumnWidth = double.PositiveInfinity,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12
        };

        // Title
        var title = new Paragraph(new Run(routeName ?? "Marine Navigation Route"))
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        };
        document.Blocks.Add(title);

        // Date and summary info
        var dateTime = DateTime.Now.ToString("MMMM dd, yyyy HH:mm");
        var info = new Paragraph(new Run($"Generated: {dateTime}"))
        {
            FontSize = 10,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 20)
        };
        document.Blocks.Add(info);

        // Waypoints section
        var waypointsHeading = new Paragraph(new Run("Waypoints"))
        {
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        };
        document.Blocks.Add(waypointsHeading);

        var waypointsTable = CreateWaypointsTable(route.Waypoints);
        document.Blocks.Add(waypointsTable);

        // Route legs section
        if (route.Legs.Count > 0)
        {
            var legsHeading = new Paragraph(new Run("Route Legs"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 20, 0, 10)
            };
            document.Blocks.Add(legsHeading);

            var legsTable = CreateLegsTable(route.Legs);
            document.Blocks.Add(legsTable);

            // Totals section
            var totalsHeading = new Paragraph(new Run("Route Summary"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 20, 0, 10)
            };
            document.Blocks.Add(totalsHeading);

            var totalDistance = route.Legs.Sum(l => l.DistanceNm);
            var totalTime = route.Legs
                .Where(l => l.LegTime.HasValue)
                .Sum(l => l.LegTime!.Value.TotalHours);
            
            var totals = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 10)
            };
            totals.Inlines.Add(new Run($"Total Distance: {totalDistance:F2} NM\n"));
            
            if (route.Legs.Any(l => l.LegTime.HasValue))
            {
                totals.Inlines.Add(new Run($"Total Time: {FormatTime(totalTime)}\n"));
            }
            
            // Show departure and arrival times if available
            var firstLeg = route.Legs.FirstOrDefault();
            var lastLeg = route.Legs.LastOrDefault();
            
            if (firstLeg?.ETA != null)
            {
                // Calculate start time from first leg ETA minus its leg time
                var startTime = firstLeg.ETA.Value - (firstLeg.LegTime ?? TimeSpan.Zero);
                totals.Inlines.Add(new Run($"Departure: {startTime:yyyy-MM-dd HH:mm}\n"));
            }
            
            if (lastLeg?.ETA != null)
            {
                totals.Inlines.Add(new Run($"Estimated Arrival: {lastLeg.ETA:yyyy-MM-dd HH:mm}"));
            }
            
            document.Blocks.Add(totals);
        }

        return document;
    }

    private static Table CreateWaypointsTable(List<Waypoint> waypoints)
    {
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1)
        };

        // Define columns
        table.Columns.Add(new TableColumn { Width = new GridLength(50) });  // #
        table.Columns.Add(new TableColumn { Width = new GridLength(150) }); // Name
        table.Columns.Add(new TableColumn { Width = new GridLength(200) }); // Latitude
        table.Columns.Add(new TableColumn { Width = new GridLength(200) }); // Longitude

        var rowGroup = new TableRowGroup();

        // Header row
        var headerRow = new TableRow
        {
            Background = Brushes.LightGray,
            FontWeight = FontWeights.Bold
        };
        headerRow.Cells.Add(CreateTableCell("#"));
        headerRow.Cells.Add(CreateTableCell("Name"));
        headerRow.Cells.Add(CreateTableCell("Latitude"));
        headerRow.Cells.Add(CreateTableCell("Longitude"));
        rowGroup.Rows.Add(headerRow);

        // Data rows
        for (int i = 0; i < waypoints.Count; i++)
        {
            var wp = waypoints[i];
            var row = new TableRow();
            row.Cells.Add(CreateTableCell((i + 1).ToString()));
            row.Cells.Add(CreateTableCell(wp.Name));
            row.Cells.Add(CreateTableCell(FormatCoordinate(wp.Coord.LatitudeDeg, true)));
            row.Cells.Add(CreateTableCell(FormatCoordinate(wp.Coord.LongitudeDeg, false)));
            rowGroup.Rows.Add(row);
        }

        table.RowGroups.Add(rowGroup);
        return table;
    }

    private static Table CreateLegsTable(List<Leg> legs)
    {
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            FontSize = 10
        };

        // Define columns
        table.Columns.Add(new TableColumn { Width = new GridLength(30) });  // #
        table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // From
        table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // To
        table.Columns.Add(new TableColumn { Width = new GridLength(60) });  // Distance
        table.Columns.Add(new TableColumn { Width = new GridLength(60) });  // True
        table.Columns.Add(new TableColumn { Width = new GridLength(60) });  // Magnetic
        table.Columns.Add(new TableColumn { Width = new GridLength(60) });  // Cardinal
        table.Columns.Add(new TableColumn { Width = new GridLength(50) });  // Var
        table.Columns.Add(new TableColumn { Width = new GridLength(60) });  // Time
        table.Columns.Add(new TableColumn { Width = new GridLength(100) }); // ETA

        var rowGroup = new TableRowGroup();

        // Header row
        var headerRow = new TableRow
        {
            Background = Brushes.LightGray,
            FontWeight = FontWeights.Bold
        };
        headerRow.Cells.Add(CreateTableCell("#"));
        headerRow.Cells.Add(CreateTableCell("From"));
        headerRow.Cells.Add(CreateTableCell("To"));
        headerRow.Cells.Add(CreateTableCell("Dist (NM)"));
        headerRow.Cells.Add(CreateTableCell("True"));
        headerRow.Cells.Add(CreateTableCell("Magnetic"));
        headerRow.Cells.Add(CreateTableCell("Cardinal"));
        headerRow.Cells.Add(CreateTableCell("Var"));
        headerRow.Cells.Add(CreateTableCell("Time"));
        headerRow.Cells.Add(CreateTableCell("ETA"));
        rowGroup.Rows.Add(headerRow);

        // Data rows
        for (int i = 0; i < legs.Count; i++)
        {
            var leg = legs[i];
            var row = new TableRow();
            row.Cells.Add(CreateTableCell((i + 1).ToString()));
            row.Cells.Add(CreateTableCell(leg.From.Name));
            row.Cells.Add(CreateTableCell(leg.To.Name));
            row.Cells.Add(CreateTableCell($"{leg.DistanceNm:F2}"));
            row.Cells.Add(CreateTableCell($"{leg.TrueBearingDeg:F1}°"));
            row.Cells.Add(CreateTableCell(leg.MagneticCourseDeg.HasValue 
                ? $"{leg.MagneticCourseDeg.Value:F1}°" 
                : "—"));
            row.Cells.Add(CreateTableCell(leg.Cardinal16));
            row.Cells.Add(CreateTableCell(leg.VariationDeg.HasValue
                ? $"{leg.VariationDeg.Value:F1}°"
                : "—"));
            row.Cells.Add(CreateTableCell(leg.LegTime.HasValue
                ? FormatTime(leg.LegTime.Value.TotalHours)
                : "—"));
            row.Cells.Add(CreateTableCell(leg.ETA?.ToString("HH:mm") ?? "—"));
            rowGroup.Rows.Add(row);
        }

        table.RowGroups.Add(rowGroup);
        return table;
    }

    private static TableCell CreateTableCell(string text)
    {
        return new TableCell(new Paragraph(new Run(text)))
        {
            Padding = new Thickness(5),
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5)
        };
    }

    private static string FormatCoordinate(double value, bool isLatitude)
    {
        var abs = Math.Abs(value);
        var degrees = (int)abs;
        var minutesDecimal = (abs - degrees) * 60;
        var minutes = (int)minutesDecimal;
        var seconds = (minutesDecimal - minutes) * 60;

        var direction = isLatitude
            ? (value >= 0 ? "N" : "S")
            : (value >= 0 ? "E" : "W");

        return $"{degrees}° {minutes:D2}' {seconds:F2}\" {direction}";
    }

    private static string FormatTime(double hours)
    {
        var totalMinutes = (int)Math.Round(hours * 60);
        var h = totalMinutes / 60;
        var m = totalMinutes % 60;
        return $"{h}h {m:D2}m";
    }
}
