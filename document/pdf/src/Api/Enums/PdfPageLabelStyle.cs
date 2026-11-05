namespace pdf;

/// <summary>
/// Selects the numbering style used by a PDF page-label range.
/// </summary>
public enum PdfPageLabelStyle
{
    /// <summary>Uses only the configured prefix.</summary>
    None = 0,
    /// <summary>Uses decimal Arabic numbers.</summary>
    Decimal = 1,
    /// <summary>Uses upper-case Roman numerals.</summary>
    UpperRoman = 2,
    /// <summary>Uses lower-case Roman numerals.</summary>
    LowerRoman = 3,
    /// <summary>Uses upper-case alphabetic numbering.</summary>
    UpperLetters = 4,
    /// <summary>Uses lower-case alphabetic numbering.</summary>
    LowerLetters = 5
}
