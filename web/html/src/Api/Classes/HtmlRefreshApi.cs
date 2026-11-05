using System;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the HtmlRefresh operations.</summary>
public class HtmlRefreshApi : IHtmlRefreshApi
{
    /// <summary>Executes the Parse operation.</summary>
    public HtmlRefreshResult Parse(string value)
    {
        return HtmlRefresh.Parse(value);
    }

    /// <summary>Executes the ParseWithLimits operation.</summary>
    public HtmlRefreshResult ParseWithLimits(string value, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlRefresh.Parse(value, limits, cancellationToken);
    }

    /// <summary>Serializes a refresh delay and optional URL.</summary>
    public string Serialize(long delaySeconds, string url)
    {
        return HtmlRefresh.Serialize(delaySeconds, url,
            HtmlLimits.CreateDefault(), CancellationToken.None);
    }

    /// <summary>Serializes a refresh delay and optional URL with output limits and cancellation.</summary>
    public string SerializeWithLimits(long delaySeconds, string url,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlRefresh.Serialize(delaySeconds, url, limits,
            cancellationToken);
    }
}
