using System;
using System.Threading;

namespace html;
/// <summary>Defines instance-based access to the HtmlUrls operations.</summary>
public interface IHtmlUrlsApi
{
    /// <summary>Executes the ExtractScheme operation.</summary>
    public HtmlUrlSchemeResult ExtractScheme(string value);
    /// <summary>Executes the ExtractSchemeWithLimits operation.</summary>
    public HtmlUrlSchemeResult ExtractSchemeWithLimits(string value, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the Normalize operation.</summary>
    public HtmlUrlResult Normalize(string value);
    /// <summary>Executes the NormalizeWithLimits operation.</summary>
    public HtmlUrlResult NormalizeWithLimits(string value, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the Resolve operation.</summary>
    public HtmlUrlResult Resolve(string value, string baseUrl);
    /// <summary>Executes the ResolveWithLimits operation.</summary>
    public HtmlUrlResult ResolveWithLimits(string value, string baseUrl, HtmlLimits limits, CancellationToken cancellationToken);
}
