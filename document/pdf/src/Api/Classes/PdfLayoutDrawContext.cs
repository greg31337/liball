using System;

namespace pdf;

/// <summary>
/// Represents a layout draw context used by the PDF generation API.
/// </summary>
public class PdfLayoutDrawContext : IPdfLayoutDrawContext
{
    /// <summary>
    /// Stores the page used by this object.
    /// </summary>
    internal PdfPage Page;
    /// <summary>
    /// Specifies the x in PDF points.
    /// </summary>
    internal double X;
    /// <summary>
    /// Specifies the y in PDF points.
    /// </summary>
    internal double Y;
    /// <summary>
    /// Specifies the available width in PDF points.
    /// </summary>
    internal double AvailableWidth;
    /// <summary>
    /// Specifies the available height in PDF points.
    /// </summary>
    internal double AvailableHeight;

    /// <summary>
    /// Initializes a new layout draw context.
    /// </summary>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="availableWidth">The available width in PDF points.</param>
    /// <param name="availableHeight">The available height in PDF points.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutDrawContext(PdfPage page, double x, double y, double availableWidth, double availableHeight)
    {
        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        ValidateNotNegative(availableWidth, "availableWidth");
        ValidateNotNegative(availableHeight, "availableHeight");
        ValidateFinite(x, "x");
        ValidateFinite(y, "y");

        Page = page;
        X = x;
        Y = y;
        AvailableWidth = availableWidth;
        AvailableHeight = availableHeight;
    }

    /// <summary>
    /// Converts the object's current state to a layout context.
    /// </summary>
    /// <returns>The resulting layout context.</returns>
    public PdfLayoutContext ToLayoutContext()
    {
        return new PdfLayoutContext(AvailableWidth, AvailableHeight);
    }

    /// <summary>
    /// Creates and returns a layout draw context from the supplied page content.
    /// </summary>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <returns>The resulting layout draw context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static PdfLayoutDrawContext FromPageContent(PdfPage page)
    {
        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        PdfRectangle content = page.GetContentArea();
        return new PdfLayoutDrawContext(page, content.X, content.Y, content.Width, content.Height);
    }

    internal static void ValidateNotNegative(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Available layout size must be finite and nonnegative.");
        }
    }

    internal static void ValidateFinite(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(name, "Layout coordinate must be finite.");
        }
    }
}
