namespace pdf;

/// <summary>
/// Specifies the base direction used for Unicode bidirectional text layout.
/// </summary>
public enum PdfTextDirection
{
    /// <summary>
    /// Selects the base direction from the first strong directional character.
    /// </summary>
    Auto = 0,
    /// <summary>
    /// Uses a left-to-right base direction.
    /// </summary>
    LeftToRight = 1,
    /// <summary>
    /// Uses a right-to-left base direction.
    /// </summary>
    RightToLeft = 2
}
