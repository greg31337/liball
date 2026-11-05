namespace http;

/// <summary>
/// Represents an HTTP language range and its selection metadata.
/// </summary>
public class HttpLanguageRange
{
    /// <summary>
    /// The language range without a wildcard marker.
    /// </summary>
    public string Range = "";

    /// <summary>
    /// Indicates that the language range is the wildcard.
    /// </summary>
    public bool IsWildcard;

    /// <summary>
    /// The quality value in thousandths from zero through one thousand.
    /// </summary>
    public int Quality = 1000;

    /// <summary>
    /// The zero-based position in the source field value.
    /// </summary>
    public int SourceOrder;

    /// <summary>
    /// The calculated matching specificity.
    /// </summary>
    public int Specificity;
}
