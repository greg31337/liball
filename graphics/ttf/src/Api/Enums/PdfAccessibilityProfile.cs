namespace ttf;

/// <summary>Identifies an accessibility compatibility profile to assess.</summary>
public enum PdfAccessibilityProfile
{
    /// <summary>Do not request PDF/UA-specific compatibility assessment.</summary>
    None = 0,

    /// <summary>Assess requirements associated with PDF/UA-1.</summary>
    PdfUa1 = 1,

    /// <summary>Assess requirements associated with PDF/UA-2.</summary>
    PdfUa2 = 2
}
