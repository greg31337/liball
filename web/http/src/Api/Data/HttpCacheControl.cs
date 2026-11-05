using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents parsed HTTP Cache-Control directives and preserved extensions.
/// </summary>
public class HttpCacheControl
{
    /// <summary>Indicates that the no-cache directive is present.</summary>
    public bool NoCache;
    /// <summary>Field names carried by a qualified no-cache directive.</summary>
    public List<string> NoCacheFields = new List<string>();
    /// <summary>Indicates that the no-store directive is present.</summary>
    public bool NoStore;
    /// <summary>Indicates that the no-transform directive is present.</summary>
    public bool NoTransform;
    /// <summary>Indicates that the only-if-cached directive is present.</summary>
    public bool OnlyIfCached;
    /// <summary>Indicates that the must-revalidate directive is present.</summary>
    public bool MustRevalidate;
    /// <summary>Indicates that the proxy-revalidate directive is present.</summary>
    public bool ProxyRevalidate;
    /// <summary>Indicates that the must-understand directive is present.</summary>
    public bool MustUnderstand;
    /// <summary>Indicates that the private directive is present.</summary>
    public bool Private;
    /// <summary>Field names carried by a qualified private directive.</summary>
    public List<string> PrivateFields = new List<string>();
    /// <summary>Indicates that the public directive is present.</summary>
    public bool Public;
    /// <summary>Indicates that the immutable directive is present.</summary>
    public bool Immutable;
    /// <summary>Indicates that max-age has a parsed value.</summary>
    public bool HasMaxAge;
    /// <summary>The max-age delta-seconds value.</summary>
    public long MaxAge;
    /// <summary>Indicates that s-maxage has a parsed value.</summary>
    public bool HasSharedMaxAge;
    /// <summary>The s-maxage delta-seconds value.</summary>
    public long SharedMaxAge;
    /// <summary>Indicates that max-stale is present.</summary>
    public bool HasMaxStale;
    /// <summary>Indicates that max-stale includes a delta-seconds value.</summary>
    public bool HasMaxStaleValue;
    /// <summary>The max-stale delta-seconds value.</summary>
    public long MaxStale;
    /// <summary>Indicates that min-fresh has a parsed value.</summary>
    public bool HasMinFresh;
    /// <summary>The min-fresh delta-seconds value.</summary>
    public long MinFresh;
    /// <summary>Indicates that stale-if-error has a parsed value.</summary>
    public bool HasStaleIfError;
    /// <summary>The stale-if-error delta-seconds value.</summary>
    public long StaleIfError;
    /// <summary>Indicates that stale-while-revalidate has a parsed value.</summary>
    public bool HasStaleWhileRevalidate;
    /// <summary>The stale-while-revalidate delta-seconds value.</summary>
    public long StaleWhileRevalidate;
    /// <summary>Indicates that duplicate directives had conflicting values.</summary>
    public bool HasConflicts;
    /// <summary>The ordered unknown or extension directives.</summary>
    public List<HttpParameter> Extensions = new List<HttpParameter>();
}
