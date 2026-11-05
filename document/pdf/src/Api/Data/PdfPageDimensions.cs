using System;

namespace pdf;

/// <summary>
/// Stores a PDF page's width and height in points.
/// </summary>
public class PdfPageDimensions
{
    /// <summary>
    /// Specifies the width in PDF points.
    /// </summary>
    public double Width;
    /// <summary>
    /// Specifies the height in PDF points.
    /// </summary>
    public double Height;

    /// <summary>
    /// Initializes a new set of page dimensions.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a dimension is not finite and positive.</exception>
    public PdfPageDimensions(double width, double height)
    {
        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Page width must be finite and positive.");
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Page height must be finite and positive.");
        }

        Width = width;
        Height = height;
    }
}
