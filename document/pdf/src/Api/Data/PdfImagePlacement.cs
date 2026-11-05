using System;

namespace pdf;

/// <summary>
/// Represents an image placement used by the PDF generation API.
/// </summary>
public class PdfImagePlacement
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
    /// Initializes a new image placement.
    /// </summary>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a coordinate is not finite or a dimension is not finite and positive.</exception>
    public PdfImagePlacement(double x, double y, double width, double height)
    {
        if (double.IsNaN(x) || double.IsInfinity(x))
        {
            throw new ArgumentOutOfRangeException("x", "Image placement x coordinate must be finite.");
        }

        if (double.IsNaN(y) || double.IsInfinity(y))
        {
            throw new ArgumentOutOfRangeException("y", "Image placement y coordinate must be finite.");
        }

        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Image placement width must be finite and positive.");
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Image placement height must be finite and positive.");
        }

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}
