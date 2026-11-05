using System.Globalization;
using svg;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// The closing Z segment completes an 80 by 40 rectangle.
const string text = "M10 10 L90 10 L90 50 L10 50 Z";
var parsed = new SvgPathParser().Parse(text, 100, 1024);
if (!parsed.Success || parsed.PathData is null)
{
    throw new InvalidOperationException("SVG path parsing failed.");
}

var metrics = new SvgPathMetrics();
var measured = metrics.MeasureWithPath(parsed.PathData);
if (!measured.Success)
{
    throw new InvalidOperationException("SVG measurement failed.");
}

Console.WriteLine($"SVG path: {text}");
Console.WriteLine($"Perimeter: {measured.ActualLength:F1} units");

// Locations are measured along the path, including the closing segment.
foreach (double distance in new[] { 0d, 100d, 240d })
{
    var location = metrics.GetLocationWithPathAndDistance(parsed.PathData, distance);
    if (!location.Success || location.Point is null)
    {
        throw new InvalidOperationException("Could not locate point.");
    }

    Console.WriteLine($"At {distance,5:F1} units: ({location.Point.X:F1}, {location.Point.Y:F1})");
}
