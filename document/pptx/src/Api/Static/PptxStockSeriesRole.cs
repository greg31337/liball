namespace pptx;

/// <summary>
/// Provides identifiers for stock-chart series roles.
/// </summary>
public static class PptxStockSeriesRole
{
    /// <summary>
    /// Identifies a volume series.
    /// </summary>
    public const string Volume = "volume";
    /// <summary>
    /// Identifies an opening-price series.
    /// </summary>
    public const string Open = "open";
    /// <summary>
    /// Identifies a high-price series.
    /// </summary>
    public const string High = "high";
    /// <summary>
    /// Identifies a low-price series.
    /// </summary>
    public const string Low = "low";
    /// <summary>
    /// Identifies a closing-price series.
    /// </summary>
    public const string Close = "close";
    /// <summary>
    /// Identifies a series without an assigned stock role.
    /// </summary>
    public const string Unknown = "unknown";
}
