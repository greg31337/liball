using System;
using System.Threading;

namespace html;
/// <summary>Defines language-tag parsing operations.</summary>
public interface IHtmlLanguageTagsApi
{
    /// <summary>Parses and normalizes a language tag.</summary>
    public HtmlLanguageTagResult Parse(string value);
    /// <summary>Parses with limits and cancellation.</summary>
    public HtmlLanguageTagResult ParseWithLimits(string value, HtmlLimits limits, CancellationToken cancellationToken);
}
