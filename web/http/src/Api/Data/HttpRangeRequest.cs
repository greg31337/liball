using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents a parsed range request and its normalization policy.
/// </summary>
public class HttpRangeRequest
{
    /// <summary>
    /// The requested range unit.
    /// </summary>
    public string Unit = "";

    /// <summary>
    /// The requested ranges in source order.
    /// </summary>
    public List<HttpByteRange> Ranges = new List<HttpByteRange>();

    /// <summary>
    /// The ordered raw range specifiers for an extension range unit.
    /// </summary>
    public List<byte[]> OtherRanges = new List<byte[]>();

    /// <summary>
    /// The shared resource limits applied to range processing.
    /// </summary>
    public HttpLimits Limits = new HttpLimits();

    /// <summary>
    /// Indicates that overlapping ranges can be coalesced.
    /// </summary>
    public bool CoalesceOverlaps = true;

    /// <summary>
    /// Indicates that adjacent ranges can be coalesced.
    /// </summary>
    public bool CoalesceAdjacent = true;
}
