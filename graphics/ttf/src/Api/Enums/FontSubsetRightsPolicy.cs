namespace ttf;

/// <summary>Controls the response to font embedding-right restrictions.</summary>
public enum FontSubsetRightsPolicy
{
    /// <summary>Reject an operation that conflicts with interpreted embedding rights.</summary>
    Reject = 0,

    /// <summary>Report restrictions without changing the requested embedding mode.</summary>
    Report = 1,

    /// <summary>Use full-font output when subsetting alone is prohibited.</summary>
    /// <remarks>
    /// This does not override restricted embedding or a bitmap-only restriction.
    /// </remarks>
    EmbedFullFont = 2
}
