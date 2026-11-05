namespace uspto;

/// <summary>
/// Specifies the source authority accepted by fallback policy.
/// </summary>
public enum FallbackAuthority
{
    /// <summary>Only authoritative USPTO data may satisfy the request.</summary>
    AuthoritativeOnly = 0,

    /// <summary>Only explicitly research-derived data may satisfy the request.</summary>
    ResearchOnly = 1,

    /// <summary>Either authority category is explicitly accepted by the caller.</summary>
    Either = 2
}
