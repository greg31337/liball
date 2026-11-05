using System;
using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents the result of applying or removing HTTP content codings.
/// </summary>
public class HttpContentCodingResult
{
    /// <summary>
    /// The content-coding outcome.
    /// </summary>
    public HttpContentCodingOutcome Outcome;

    /// <summary>
    /// The owned transformed content bytes.
    /// </summary>
    public byte[] Content = Array.Empty<byte>();

    /// <summary>
    /// The coding names successfully applied or removed in operation order.
    /// </summary>
    public List<string> AppliedCodings = new List<string>();

    /// <summary>
    /// The coding names not processed after an unsupported or failed operation.
    /// </summary>
    public List<string> RemainingCodings = new List<string>();

    /// <summary>
    /// A caller-readable result explanation, or <c>null</c> when omitted.
    /// </summary>
    public string? Reason;
}
