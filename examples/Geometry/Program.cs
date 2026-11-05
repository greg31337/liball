using System.Globalization;
using geo;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var wkt = new WktCodec(new WktReadOptions(), new WktWriteOptions());
var measures = new GeometryMeasures();
var predicates = new GeometryPredicates();

// Coordinates are on a flat plane in metres, not longitude and latitude.
var garden = wkt.ReadWkt("POLYGON ((0 0, 8 0, 8 5, 0 5, 0 0))");
var inside = wkt.ReadWkt("POINT (3 2)");
var outside = wkt.ReadWkt("POINT (10 2)");
var centroid = measures.Centroid(garden) ?? throw new InvalidOperationException("No centroid.");
Console.WriteLine("An 8 m by 5 m garden");
Console.WriteLine($"WKT:       {wkt.WriteWkt(garden)}");
Console.WriteLine($"Area:      {measures.Area(garden):F1} square metres");
Console.WriteLine($"Perimeter: {measures.Perimeter(garden):F1} metres");
Console.WriteLine($"Centroid:  ({centroid.X:F1}, {centroid.Y:F1})");
Console.WriteLine($"Contains (3, 2):  {predicates.Contains(garden, inside)}");
Console.WriteLine($"Contains (10, 2): {predicates.Contains(garden, outside)}");
Console.WriteLine($"Distance from (10, 2) to garden: {measures.Distance(garden, outside):F1} metres");

// Compare geometry meaning rather than the spelling of the serialized WKT.
var roundTrip = wkt.ReadWkt(wkt.WriteWkt(garden));
if (!predicates.AreTopologicallyEqual(garden, roundTrip))
{
    throw new InvalidOperationException("Geometry changed during WKT round trip.");
}
