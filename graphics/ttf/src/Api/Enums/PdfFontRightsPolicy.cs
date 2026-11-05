namespace ttf;

/// <summary>Controls the response to font embedding-right restrictions.</summary>
public enum PdfFontRightsPolicy
{
    /// <summary>Reject a resource that conflicts with interpreted rights.</summary>
    Reject = 0,

    /// <summary>Report restrictions without changing the requested mode.</summary>
    Report = 1,

    /// <summary>Use full embedding when only subsetting is prohibited.</summary>
    /// <remarks>
    /// This does not override restricted embedding or bitmap-only restrictions.
    /// </remarks>
    EmbedFullFont = 2
}
