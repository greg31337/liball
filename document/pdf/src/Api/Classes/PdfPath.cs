using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Builds a reusable vector path from line and cubic Bézier segments for stroking, filling, or clipping.
/// </summary>
/// <remarks>
/// The path stores geometry only. A canvas operation decides whether that geometry is stroked, filled, or used as a clipping boundary.
/// </remarks>
public class PdfPath : IPdfPath
{
    internal const double BezierCircleControl = 0.552284749831;
    internal List<PdfPathCommand> _commands;
    internal IReadOnlyList<PdfPathCommand> Commands;

    /// <summary>
    /// Initializes a new path.
    /// </summary>
    public PdfPath()
    {
        _commands = new List<PdfPathCommand>();
        Commands = _commands;
    }

    /// <summary>
    /// Moves the path's current point without drawing a segment.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <returns>The resulting path.</returns>
    public PdfPath MoveTo(double x, double y)
    {
        ValidateCoordinate(x, "x");
        ValidateCoordinate(y, "y");
        _commands.Add(new PdfPathCommand("m", x, y, 0, 0, 0, 0));
        return this;
    }

    /// <summary>
    /// Adds a straight segment from the current path point to the supplied point.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <returns>The resulting path.</returns>
    public PdfPath LineTo(double x, double y)
    {
        ValidateCoordinate(x, "x");
        ValidateCoordinate(y, "y");
        _commands.Add(new PdfPathCommand("l", x, y, 0, 0, 0, 0));
        return this;
    }

    /// <summary>
    /// Adds a cubic Bézier segment using two control points and an end point.
    /// </summary>
    /// <param name="x1">The first horizontal coordinate in PDF points.</param>
    /// <param name="y1">The first vertical coordinate in PDF points.</param>
    /// <param name="x2">The second horizontal coordinate in PDF points.</param>
    /// <param name="y2">The second vertical coordinate in PDF points.</param>
    /// <param name="x3">The third horizontal coordinate in PDF points.</param>
    /// <param name="y3">The third vertical coordinate in PDF points.</param>
    /// <returns>The resulting path.</returns>
    public PdfPath CurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
    {
        ValidateCoordinate(x1, "x1");
        ValidateCoordinate(y1, "y1");
        ValidateCoordinate(x2, "x2");
        ValidateCoordinate(y2, "y2");
        ValidateCoordinate(x3, "x3");
        ValidateCoordinate(y3, "y3");
        _commands.Add(new PdfPathCommand("c", x1, y1, x2, y2, x3, y3));
        return this;
    }

    /// <summary>
    /// Closes the current path by connecting its last point to its first point.
    /// </summary>
    /// <returns>The resulting path.</returns>
    public PdfPath Close()
    {
        _commands.Add(new PdfPathCommand("h", 0, 0, 0, 0, 0, 0));
        return this;
    }

    /// <summary>
    /// Determines whether the path contains at least one segment command.
    /// </summary>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    public bool HasCommands()
    {
        return _commands.Count > 0;
    }

    /// <summary>
    /// Creates a closed circular path from four cubic Bézier segments.
    /// </summary>
    /// <param name="centerX">The horizontal center coordinate in PDF points.</param>
    /// <param name="centerY">The vertical center coordinate in PDF points.</param>
    /// <param name="radius">The radius in PDF points; it must be positive unless stated otherwise.</param>
    /// <returns>The resulting path.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    internal static PdfPath Circle(double centerX, double centerY, double radius)
    {
        if (radius <= 0)
        {
            throw new ArgumentOutOfRangeException("radius", "Circle radius must be positive.");
        }

        return Ellipse(centerX, centerY, radius, radius);
    }

    /// <summary>
    /// Creates a closed elliptical path from four cubic Bézier segments.
    /// </summary>
    /// <param name="centerX">The horizontal center coordinate in PDF points.</param>
    /// <param name="centerY">The vertical center coordinate in PDF points.</param>
    /// <param name="radiusX">The horizontal radius in PDF points; it must be positive.</param>
    /// <param name="radiusY">The vertical radius in PDF points; it must be positive.</param>
    /// <returns>The resulting path.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    internal static PdfPath Ellipse(double centerX, double centerY, double radiusX, double radiusY)
    {
        if (radiusX <= 0)
        {
            throw new ArgumentOutOfRangeException("radiusX", "Ellipse radius must be positive.");
        }

        if (radiusY <= 0)
        {
            throw new ArgumentOutOfRangeException("radiusY", "Ellipse radius must be positive.");
        }

        PdfPath path = new PdfPath();
        double controlX = radiusX * BezierCircleControl;
        double controlY = radiusY * BezierCircleControl;

        path.MoveTo(centerX + radiusX, centerY);
        path.CurveTo(centerX + radiusX, centerY + controlY, centerX + controlX, centerY + radiusY, centerX, centerY + radiusY);
        path.CurveTo(centerX - controlX, centerY + radiusY, centerX - radiusX, centerY + controlY, centerX - radiusX, centerY);
        path.CurveTo(centerX - radiusX, centerY - controlY, centerX - controlX, centerY - radiusY, centerX, centerY - radiusY);
        path.CurveTo(centerX + controlX, centerY - radiusY, centerX + radiusX, centerY - controlY, centerX + radiusX, centerY);
        path.Close();

        return path;
    }

    /// <summary>
    /// Creates a closed rectangular path with Bézier-rounded corners and clamps oversized corner radii.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="radius">The radius in PDF points; it must be positive unless stated otherwise.</param>
    /// <returns>The resulting path.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    internal static PdfPath RoundedRectangle(double x, double y, double width, double height, double radius)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Rounded rectangle width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Rounded rectangle height must be positive.");
        }

        if (radius < 0)
        {
            throw new ArgumentOutOfRangeException("radius", "Rounded rectangle radius cannot be negative.");
        }

        double cornerRadius = ClampRadius(width, height, radius);
        double control = cornerRadius * BezierCircleControl;
        double right = x + width;
        double top = y + height;
        PdfPath path = new PdfPath();

        path.MoveTo(x + cornerRadius, y);
        path.LineTo(right - cornerRadius, y);
        path.CurveTo(right - cornerRadius + control, y, right, y + cornerRadius - control, right, y + cornerRadius);
        path.LineTo(right, top - cornerRadius);
        path.CurveTo(right, top - cornerRadius + control, right - cornerRadius + control, top, right - cornerRadius, top);
        path.LineTo(x + cornerRadius, top);
        path.CurveTo(x + cornerRadius - control, top, x, top - cornerRadius + control, x, top - cornerRadius);
        path.LineTo(x, y + cornerRadius);
        path.CurveTo(x, y + cornerRadius - control, x + cornerRadius - control, y, x + cornerRadius, y);
        path.Close();

        return path;
    }

    internal static double ClampRadius(double width, double height, double radius)
    {
        double maxRadius = width / 2;

        if (height / 2 < maxRadius)
        {
            maxRadius = height / 2;
        }

        if (radius > maxRadius)
        {
            return maxRadius;
        }

        return radius;
    }

    internal static void ValidateCoordinate(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(name, "Path coordinates must be finite.");
        }
    }
}
