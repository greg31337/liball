namespace pdf;

/// <summary>
/// Specifies whether pagination may break between rows connected by a row span.
/// </summary>
public enum PdfTableRowSplitPolicy
{
    /// <summary>Keeps every connected row-span group in one flow area when possible.</summary>
    Prevent = 0,
    /// <summary>Allows pagination between physical rows even when a row span connects them.</summary>
    Allow = 1
}
