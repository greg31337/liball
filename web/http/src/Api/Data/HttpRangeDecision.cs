using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents the result of parsing or evaluating an HTTP range request.
/// </summary>
public class HttpRangeDecision
{
    /// <summary>
    /// The range-processing outcome.
    /// </summary>
    public HttpRangeOutcome Outcome;

    /// <summary>
    /// The range request that was evaluated, or <c>null</c> when unavailable.
    /// </summary>
    public HttpRangeRequest? Request;

    /// <summary>
    /// The requested or normalized ranges in processing order.
    /// </summary>
    public List<HttpByteRange> Ranges = new List<HttpByteRange>();

    /// <summary>
    /// The Content-Range values corresponding to normalized ranges.
    /// </summary>
    public List<HttpContentRange> ContentRanges = new List<HttpContentRange>();

    /// <summary>
    /// The assembled multipart result, or <c>null</c> when multipart output was not requested.
    /// </summary>
    public HttpMultipartRange? Multipart;

    /// <summary>
    /// A caller-readable decision explanation, or <c>null</c> when omitted.
    /// </summary>
    public string? Reason;
}
