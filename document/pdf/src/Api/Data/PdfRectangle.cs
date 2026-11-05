using System;

namespace pdf;

/// <summary>
/// Represents a rectangle used by the PDF generation API.
/// </summary>
public class PdfRectangle
{
    /// <summary>
    /// Specifies the x in PDF points.
    /// </summary>
    public double X;
    /// <summary>
    /// Specifies the y in PDF points.
    /// </summary>
    public double Y;
    /// <summary>
    /// Specifies the width in PDF points.
    /// </summary>
    public double Width;
    /// <summary>
    /// Specifies the height in PDF points.
    /// </summary>
    public double Height;

    /// <summary>
    /// Initializes a new rectangle.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a coordinate is not finite or a dimension is not finite and nonnegative.</exception>
    public PdfRectangle(double x, double y, double width, double height)
    {
        if (double.IsNaN(x) || double.IsInfinity(x))
        {
            throw new ArgumentOutOfRangeException("x", "Rectangle x coordinate must be finite.");
        }

        if (double.IsNaN(y) || double.IsInfinity(y))
        {
            throw new ArgumentOutOfRangeException("y", "Rectangle y coordinate must be finite.");
        }

        if (double.IsNaN(width) || double.IsInfinity(width) || width < 0)
        {
            throw new ArgumentOutOfRangeException("width", "Rectangle width must be finite and nonnegative.");
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height < 0)
        {
            throw new ArgumentOutOfRangeException("height", "Rectangle height must be finite and nonnegative.");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}
