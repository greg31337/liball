using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the HtmlSrcset operations.</summary>
public class HtmlSrcsetApi : IHtmlSrcsetApi
{
    /// <summary>Executes the Parse operation.</summary>
    public HtmlSrcsetResult Parse(string value)
    {
        return HtmlSrcset.Parse(value);
    }

    /// <summary>Executes the ParseWithLimits operation.</summary>
    public HtmlSrcsetResult ParseWithLimits(string value, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlSrcset.Parse(value, limits, cancellationToken);
    }

    /// <summary>Executes the Serialize operation.</summary>
    public string Serialize(HtmlSrcsetCandidate[] candidates)
    {
        return HtmlSrcset.Serialize(candidates);
    }

    /// <summary>Serializes candidates with output limits and cancellation.</summary>
    public string SerializeWithLimits(HtmlSrcsetCandidate[] candidates,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlSrcset.SerializeWithLimits(candidates, limits,
            cancellationToken);
    }

    /// <summary>Creates a candidate without a descriptor.</summary>
    public HtmlSrcsetCandidate CreateCandidate(string url)
    {
        return HtmlSrcsetCandidate.Create(url);
    }

    /// <summary>Creates a candidate with a width descriptor.</summary>
    public HtmlSrcsetCandidate CreateWidthCandidate(string url, int width)
    {
        return HtmlSrcsetCandidate.CreateWidth(url, width);
    }

    /// <summary>Creates a candidate with a density descriptor.</summary>
    public HtmlSrcsetCandidate CreateDensityCandidate(string url,
        double density)
    {
        return HtmlSrcsetCandidate.CreateDensity(url, density);
    }
}
