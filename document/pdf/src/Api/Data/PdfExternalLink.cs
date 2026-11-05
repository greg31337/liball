using System;

namespace pdf;

/// <summary>
/// Represents an external link used by the PDF generation API.
/// </summary>
public class PdfExternalLink
{
    /// <summary>
    /// Stores the uri used by this object.
    /// </summary>
    public string Uri;
    /// <summary>
    /// Stores the rectangle used by this object.
    /// </summary>
    public PdfRectangle Rectangle;
    internal PdfAccessibilityNode? TagNode;
    internal int StructParent;

    /// <summary>
    /// Initializes a new external link.
    /// </summary>
    /// <param name="uri">The absolute or relative URI opened by the link.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfExternalLink(string uri, PdfRectangle rectangle)
    {
        if (uri == null)
        {
            throw new ArgumentNullException("uri");
        }

        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        PdfPage.ValidateUri(uri);
        PdfPage.ValidateLinkRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

        Uri = uri;
        Rectangle = new PdfRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        TagNode = null;
        StructParent = -1;
    }
}
