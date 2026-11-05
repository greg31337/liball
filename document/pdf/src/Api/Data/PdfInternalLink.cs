using System;

namespace pdf;

/// <summary>
/// Represents an internal link used by the PDF generation API.
/// </summary>
public class PdfInternalLink
{
    /// <summary>
    /// Stores the destination name used by this object.
    /// </summary>
    public string DestinationName;
    /// <summary>
    /// Stores the rectangle used by this object.
    /// </summary>
    public PdfRectangle Rectangle;
    internal PdfAccessibilityNode? StructureTarget;
    internal PdfAccessibilityNode? TagNode;
    internal int StructParent;

    /// <summary>
    /// Initializes a new internal link.
    /// </summary>
    /// <param name="destinationName">The destination name to use for this operation.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfInternalLink(string destinationName, PdfRectangle rectangle)
    {
        if (destinationName == null)
        {
            throw new ArgumentNullException("destinationName");
        }

        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        PdfPage.ValidateDestinationName(destinationName);
        PdfPage.ValidateLinkRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

        DestinationName = destinationName;
        Rectangle = new PdfRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        StructureTarget = null;
        TagNode = null;
        StructParent = -1;
    }

    internal void SetStructureTarget(PdfAccessibilityNode target)
    {
        StructureTarget = target;
    }
}
