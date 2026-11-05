using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents a weighted HTTP token or range and its matching metadata.
/// </summary>
public class HttpWeightedValue
{
    /// <summary>
    /// The token or range value.
    /// </summary>
    public string Value = "";

    /// <summary>
    /// The ordered parameters other than the quality parameter.
    /// </summary>
    public List<HttpParameter> Parameters = new List<HttpParameter>();

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
