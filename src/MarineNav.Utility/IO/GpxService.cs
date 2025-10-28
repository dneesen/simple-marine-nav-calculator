using System.Globalization;
using System.Xml.Linq;
using MarineNav.Utility.Models;

namespace MarineNav.Utility.IO;

/// <summary>
/// Service for importing and exporting waypoints to GPX 1.1 format.
/// </summary>
public static class GpxService
{
    private static readonly XNamespace GpxNamespace = "http://www.topografix.com/GPX/1/1";

    /// <summary>
    /// Imports waypoints from a GPX file.
    /// </summary>
    public static List<Waypoint> ImportWaypoints(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("GPX file not found", filePath);

        var waypoints = new List<Waypoint>();
        var doc = XDocument.Load(filePath);
        var ns = doc.Root?.Name.Namespace ?? GpxNamespace;

        var wptElements = doc.Descendants(ns + "wpt");
        foreach (var wptElement in wptElements)
        {
            try
            {
                double lat = double.Parse(wptElement.Attribute("lat")?.Value ?? "0", CultureInfo.InvariantCulture);
                double lon = double.Parse(wptElement.Attribute("lon")?.Value ?? "0", CultureInfo.InvariantCulture);
                string name = wptElement.Element(ns + "name")?.Value ?? "Waypoint";

                waypoints.Add(Waypoint.Create(name, new Coordinate(lat, lon)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Skipping invalid waypoint: {ex.Message}");
            }
        }

        // Also try to import from route points
        var rteptElements = doc.Descendants(ns + "rtept");
        foreach (var rteptElement in rteptElements)
        {
            try
            {
                double lat = double.Parse(rteptElement.Attribute("lat")?.Value ?? "0", CultureInfo.InvariantCulture);
                double lon = double.Parse(rteptElement.Attribute("lon")?.Value ?? "0", CultureInfo.InvariantCulture);
                string name = rteptElement.Element(ns + "name")?.Value ?? "Waypoint";

                // Avoid duplicates
                if (!waypoints.Any(w => w.Name == name && 
                    Math.Abs(w.Coord.LatitudeDeg - lat) < 0.0001 && 
                    Math.Abs(w.Coord.LongitudeDeg - lon) < 0.0001))
                {
                    waypoints.Add(Waypoint.Create(name, new Coordinate(lat, lon)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Skipping invalid route point: {ex.Message}");
            }
        }

        return waypoints;
    }

    /// <summary>
    /// Exports waypoints to a GPX 1.1 file.
    /// </summary>
    public static void ExportWaypoints(string filePath, List<Waypoint> waypoints, string routeName = "Marine Route")
    {
        var xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
        
        var gpx = new XElement(GpxNamespace + "gpx",
            new XAttribute("version", "1.1"),
            new XAttribute("creator", "Marine Navigation Utility"),
            new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName),
            new XAttribute(xsi + "schemaLocation", 
                "http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd"),
            
            new XElement(GpxNamespace + "metadata",
                new XElement(GpxNamespace + "name", routeName),
                new XElement(GpxNamespace + "time", DateTime.UtcNow.ToString("o"))
            )
        );

        // Add waypoints
        foreach (var wpt in waypoints)
        {
            gpx.Add(new XElement(GpxNamespace + "wpt",
                new XAttribute("lat", wpt.Coord.LatitudeDeg.ToString("F6", CultureInfo.InvariantCulture)),
                new XAttribute("lon", wpt.Coord.LongitudeDeg.ToString("F6", CultureInfo.InvariantCulture)),
                new XElement(GpxNamespace + "name", wpt.Name)
            ));
        }

        // Add route
        if (waypoints.Count > 1)
        {
            var rte = new XElement(GpxNamespace + "rte",
                new XElement(GpxNamespace + "name", routeName)
            );

            foreach (var wpt in waypoints)
            {
                rte.Add(new XElement(GpxNamespace + "rtept",
                    new XAttribute("lat", wpt.Coord.LatitudeDeg.ToString("F6", CultureInfo.InvariantCulture)),
                    new XAttribute("lon", wpt.Coord.LongitudeDeg.ToString("F6", CultureInfo.InvariantCulture)),
                    new XElement(GpxNamespace + "name", wpt.Name)
                ));
            }

            gpx.Add(rte);
        }

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            gpx
        );

        doc.Save(filePath);
    }

    /// <summary>
    /// Exports a complete route to GPX format.
    /// </summary>
    public static void ExportRoute(string filePath, Route route)
    {
        ExportWaypoints(filePath, route.Waypoints, route.Name);
    }
}
