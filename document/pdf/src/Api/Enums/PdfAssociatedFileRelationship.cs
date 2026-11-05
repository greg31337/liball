namespace pdf;

/// <summary>
/// Describes how an associated file relates to the containing PDF document.
/// </summary>
public enum PdfAssociatedFileRelationship
{
    /// <summary>The associated file is the source material used to create the PDF.</summary>
    Source = 1,
    /// <summary>The associated file contains data represented by the PDF.</summary>
    Data = 2,
    /// <summary>The associated file is an alternative representation of the PDF content.</summary>
    Alternative = 3,
    /// <summary>The associated file supplements the PDF content.</summary>
    Supplement = 4,
    /// <summary>The relationship is intentionally unspecified.</summary>
    Unspecified = 5
}
