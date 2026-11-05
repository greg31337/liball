namespace ttf;

/// <summary>Identifies an archival compatibility profile to assess.</summary>
public enum PdfArchivalProfile
{
    /// <summary>Do not request PDF/A-specific compatibility assessment.</summary>
    None = 0,

    /// <summary>Assess requirements associated with PDF/A-1.</summary>
    PdfA1 = 1,

    /// <summary>Assess requirements associated with PDF/A-2.</summary>
    PdfA2 = 2,

    /// <summary>Assess requirements associated with PDF/A-3.</summary>
    PdfA3 = 3,

    /// <summary>Assess requirements associated with PDF/A-4.</summary>
    PdfA4 = 4
}
