using System;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the HtmlUrls operations.</summary>
public class HtmlUrlsApi : IHtmlUrlsApi
{
    /// <summary>Executes the ExtractScheme operation.</summary>
    public HtmlUrlSchemeResult ExtractScheme(string value)
    {
        return HtmlUrls.ExtractScheme(value);
    }

    /// <summary>Executes the ExtractSchemeWithLimits operation.</summary>
    public HtmlUrlSchemeResult ExtractSchemeWithLimits(string value, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlUrls.ExtractScheme(value, limits, cancellationToken);
    }

    /// <summary>Executes the Normalize operation.</summary>
    public HtmlUrlResult Normalize(string value)
    {
        return HtmlUrls.Normalize(value);
    }

    /// <summary>Executes the NormalizeWithLimits operation.</summary>
    public HtmlUrlResult NormalizeWithLimits(string value, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlUrls.Normalize(value, limits, cancellationToken);
    }

    /// <summary>Executes the Resolve operation.</summary>
    public HtmlUrlResult Resolve(string value, string baseUrl)
    {
        return HtmlUrls.Resolve(value, baseUrl);
    }

    /// <summary>Executes the ResolveWithLimits operation.</summary>
    public HtmlUrlResult ResolveWithLimits(string value, string baseUrl, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlUrls.Resolve(value, baseUrl, limits, cancellationToken);
    }
}
