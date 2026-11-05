using System;

namespace pdf;

/// <summary>
/// Represents a layout context used by the PDF generation API.
/// </summary>
public class PdfLayoutContext
{
    /// <summary>
    /// Specifies the available width in PDF points.
    /// </summary>
    public double AvailableWidth;
    /// <summary>
    /// Specifies the available height in PDF points.
    /// </summary>
    public double AvailableHeight;

    /// <summary>
    /// Initializes a new layout context.
    /// </summary>
    /// <param name="availableWidth">The available width in PDF points.</param>
    /// <param name="availableHeight">The available height in PDF points.</param>
    public PdfLayoutContext(double availableWidth, double availableHeight)
    {
        ValidateNotNegative(availableWidth, "availableWidth");
        ValidateNotNegative(availableHeight, "availableHeight");

        AvailableWidth = availableWidth;
        AvailableHeight = availableHeight;
    }

    /// <summary>
    /// Creates a layout context from a rectangle's dimensions.
    /// </summary>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <returns>The resulting layout context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static PdfLayoutContext FromRectangle(PdfRectangle rectangle)
    {
        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        return new PdfLayoutContext(rectangle.Width, rectangle.Height);
    }

    /// <summary>
    /// Creates and returns a layout context from the supplied page content.
    /// </summary>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <returns>The resulting layout context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static PdfLayoutContext FromPageContent(PdfPage page)
    {
        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        return FromRectangle(page.GetContentArea());
    }

    internal static void ValidateNotNegative(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Available layout size must be finite and nonnegative.");
        }
    }
}
