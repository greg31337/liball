namespace pdf;

/// <summary>
/// Specifies line join values used by the PDF API.
/// </summary>
public enum PdfLineJoin
{
    /// <summary>
    /// Uses the miter option for line join.
    /// </summary>
    Miter = 0,
    /// <summary>
    /// Uses the round option for line join.
    /// </summary>
    Round = 1,
    /// <summary>
    /// Uses the bevel option for line join.
    /// </summary>
    Bevel = 2
}
