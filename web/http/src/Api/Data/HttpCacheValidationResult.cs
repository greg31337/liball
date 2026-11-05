using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents the result of merging validation metadata into stored responses.
/// </summary>
public class HttpCacheValidationResult
{
    /// <summary>The merged response, or <c>null</c> when validation did not produce one.</summary>
    public HttpResponse? Response;
    /// <summary>The stored responses affected by the validation operation.</summary>
    public List<HttpResponse> AffectedResponses = new List<HttpResponse>();
    /// <summary>Indicates that validation metadata was merged successfully.</summary>
    public bool WasMerged;
    /// <summary>A caller-readable result explanation, or <c>null</c> when omitted.</summary>
    public string? Reason;
}
