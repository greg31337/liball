using System;

namespace pdf;

/// <summary>
/// Records a named cross-reference target and its display text.
/// </summary>
public class PdfCrossReferenceMarker
{
    /// <summary>Stores the caller-defined marker name.</summary>
    public string Name;
    /// <summary>Stores the text used when formatting a reference.</summary>
    public string Text;
    /// <summary>Stores the target page.</summary>
    public PdfPage Page;
    /// <summary>Specifies the horizontal target coordinate.</summary>
    public double X;
    /// <summary>Specifies the vertical target coordinate.</summary>
    public double Y;
    /// <summary>Stores the generated named-destination identifier.</summary>
    public string DestinationName;

    /// <summary>
    /// Creates a named cross-reference marker.
    /// </summary>
    /// <param name="name">The caller-defined marker name.</param>
    /// <param name="text">The text used when formatting a reference.</param>
    /// <param name="page">The target page.</param>
    /// <param name="x">The horizontal target coordinate.</param>
    /// <param name="y">The vertical target coordinate.</param>
    /// <param name="destinationName">The generated destination identifier.</param>
    public PdfCrossReferenceMarker(string name, string text, PdfPage page, double x, double y, string destinationName)
    {
        if (name == null || text == null || page == null || destinationName == null)
        {
            throw new ArgumentNullException("Cross-reference marker values cannot be null.");
        }

        Name = name;
        Text = text;
        Page = page;
        X = x;
        Y = y;
        DestinationName = destinationName;
    }
}
