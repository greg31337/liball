using System;

namespace pdf;

/// <summary>
/// Records an index term and its navigation target.
/// </summary>
public class PdfIndexMarker
{
    /// <summary>Stores the primary index term.</summary>
    public string Term;
    /// <summary>Stores the optional secondary index term.</summary>
    public string Subterm;
    /// <summary>Stores the target page.</summary>
    public PdfPage Page;
    /// <summary>Specifies the horizontal target coordinate.</summary>
    public double X;
    /// <summary>Specifies the vertical target coordinate.</summary>
    public double Y;
    /// <summary>Stores the generated named-destination identifier.</summary>
    public string DestinationName;

    /// <summary>
    /// Creates an index marker and its destination metadata.
    /// </summary>
    /// <param name="term">The primary index term.</param>
    /// <param name="subterm">The optional secondary index term.</param>
    /// <param name="page">The target page.</param>
    /// <param name="x">The horizontal target coordinate.</param>
    /// <param name="y">The vertical target coordinate.</param>
    /// <param name="destinationName">The generated destination identifier.</param>
    public PdfIndexMarker(string term, string subterm, PdfPage page, double x, double y, string destinationName)
    {
        if (term == null || subterm == null || page == null || destinationName == null)
        {
            throw new ArgumentNullException("Index marker values cannot be null.");
        }

        Term = term;
        Subterm = subterm;
        Page = page;
        X = x;
        Y = y;
        DestinationName = destinationName;
    }
}
