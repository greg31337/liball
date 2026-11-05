namespace pptx;

/// <summary>
/// Identifies whether chart categories are stored as text or numeric values.
/// </summary>
public enum PptxChartCategoryKind
{
    /// <summary>
    /// Identifies text categories stored in a string cache.
    /// </summary>
    Text = 0,
    /// <summary>
    /// Identifies numeric categories stored in a numeric cache.
    /// </summary>
    Numeric = 1
}
