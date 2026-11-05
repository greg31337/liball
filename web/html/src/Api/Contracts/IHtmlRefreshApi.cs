using System;
using System.Threading;

namespace html;
/// <summary>Defines instance-based access to the HtmlRefresh operations.</summary>
public interface IHtmlRefreshApi
{
    /// <summary>Executes the Parse operation.</summary>
    public HtmlRefreshResult Parse(string value);
    /// <summary>Executes the ParseWithLimits operation.</summary>
    public HtmlRefreshResult ParseWithLimits(string value, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Serializes a refresh delay and optional URL.</summary>
    public string Serialize(long delaySeconds, string url);
    /// <summary>Serializes with output limits and cancellation.</summary>
    public string SerializeWithLimits(long delaySeconds, string url, HtmlLimits limits, CancellationToken cancellationToken);
}
