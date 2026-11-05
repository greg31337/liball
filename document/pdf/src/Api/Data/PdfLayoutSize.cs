using System;

namespace pdf;

/// <summary>
/// Represents a layout size used by the PDF generation API.
/// </summary>
public class PdfLayoutSize
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
    /// Initializes a new layout size.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public PdfLayoutSize(double width, double height)
    {
        if (double.IsNaN(width) || double.IsInfinity(width) || width < 0)
        {
            throw new ArgumentOutOfRangeException("width", "Layout width must be finite and nonnegative.");
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height < 0)
        {
            throw new ArgumentOutOfRangeException("height", "Layout height must be finite and nonnegative.");
        }

        Width = width;
        Height = height;
    }
}
