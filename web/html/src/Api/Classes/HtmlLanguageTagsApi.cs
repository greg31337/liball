using System;
using System.Threading;

namespace html;
/// <summary>Provides parsing and normalization for HTML language tags.</summary>
public class HtmlLanguageTagsApi : IHtmlLanguageTagsApi
{
    /// <summary>Parses and normalizes a language tag.</summary>
    public HtmlLanguageTagResult Parse(string value)
    {
        return HtmlLanguageTags.Parse(value, HtmlLimits.CreateDefault(),
            CancellationToken.None);
    }

    /// <summary>Parses and normalizes a language tag with limits and cancellation.</summary>
    public HtmlLanguageTagResult ParseWithLimits(string value,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlLanguageTags.Parse(value, limits, cancellationToken);
    }
}
