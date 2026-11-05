using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Defines instance-based access to the HtmlSrcset operations.</summary>
public interface IHtmlSrcsetApi
{
    /// <summary>Executes the Parse operation.</summary>
    public HtmlSrcsetResult Parse(string value);
    /// <summary>Executes the ParseWithLimits operation.</summary>
    public HtmlSrcsetResult ParseWithLimits(string value, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the Serialize operation.</summary>
    public string Serialize(HtmlSrcsetCandidate[] candidates);
    /// <summary>Serializes candidates with output limits and cancellation.</summary>
    public string SerializeWithLimits(HtmlSrcsetCandidate[] candidates, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Creates a candidate without a descriptor.</summary>
    public HtmlSrcsetCandidate CreateCandidate(string url);
    /// <summary>Creates a width candidate.</summary>
    public HtmlSrcsetCandidate CreateWidthCandidate(string url, int width);
    /// <summary>Creates a density candidate.</summary>
    public HtmlSrcsetCandidate CreateDensityCandidate(string url, double density);
}
