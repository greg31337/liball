using System;

namespace pdf;

/// <summary>
/// Records a figure or table entry for generated publishing lists.
/// </summary>
public class PdfPublishingMarker
{
    /// <summary>Stores whether this entry represents a figure or table.</summary>
    public PdfPublishingMarkerKind Kind;
    /// <summary>Stores the entry title.</summary>
    public string Title;
    /// <summary>Stores the target page.</summary>
    public PdfPage Page;
    /// <summary>Specifies the horizontal target coordinate.</summary>
    public double X;
    /// <summary>Specifies the vertical target coordinate.</summary>
    public double Y;
    /// <summary>Stores the generated named-destination identifier.</summary>
    public string DestinationName;

    /// <summary>
    /// Creates a figure or table publishing marker.
    /// </summary>
    /// <param name="kind">The marker kind.</param>
    /// <param name="title">The entry title.</param>
    /// <param name="page">The target page.</param>
    /// <param name="x">The horizontal target coordinate.</param>
    /// <param name="y">The vertical target coordinate.</param>
    /// <param name="destinationName">The generated destination identifier.</param>
    public PdfPublishingMarker(PdfPublishingMarkerKind kind, string title, PdfPage page, double x, double y, string destinationName)
    {
        if (title == null || page == null || destinationName == null)
        {
            throw new ArgumentNullException("Publishing marker values cannot be null.");
        }

        Kind = kind;
        Title = title;
        Page = page;
        X = x;
        Y = y;
        DestinationName = destinationName;
    }
}
