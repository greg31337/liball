using System;

namespace pdf;

/// <summary>
/// Represents a named destination used by the PDF generation API.
/// </summary>
public class PdfNamedDestination
{
    /// <summary>
    /// Stores the name used by this object.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the page used by this object.
    /// </summary>
    public PdfPage Page;
    /// <summary>
    /// Specifies the x in PDF points.
    /// </summary>
    public double X;
    /// <summary>
    /// Specifies the y in PDF points.
    /// </summary>
    public double Y;
    internal PdfAccessibilityNode? StructureTarget;

    /// <summary>
    /// Initializes a new named destination.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfNamedDestination(string name, PdfPage page, double x, double y)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        PdfDocument.ValidateDestinationName(name, "name");
        ValidateCoordinates(x, y);

        Name = name;
        Page = page;
        X = x;
        Y = y;
        StructureTarget = null;
    }

    internal static void ValidateCoordinates(double x, double y)
    {
        if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
        {
            throw new ArgumentOutOfRangeException("x", "Navigation target coordinates must be finite.");
        }
    }

    internal void SetStructureTarget(PdfAccessibilityNode target)
    {
        StructureTarget = target;
    }
}
