namespace jmap;

/// <summary>
/// Defines media types used by JMAP Core and JMAP Mail endpoints.
/// </summary>
public static class JmapMediaTypes
{
    /// <summary>The JSON media type used by JMAP API and push requests.</summary>
    public const string Json = "application/json";

    /// <summary>The problem-details JSON media type.</summary>
    public const string ProblemJson = "application/problem+json";

    /// <summary>The fallback media type for arbitrary binary data.</summary>
    public const string OctetStream = "application/octet-stream";

    /// <summary>The Server-Sent Events media type.</summary>
    public const string EventStream = "text/event-stream";
}
