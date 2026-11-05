using System;
using System.Collections.Generic;
using System.Threading;

namespace html;
/// <summary>Defines instance-based access to the HtmlArticleExtractor operations.</summary>
public interface IHtmlArticleExtractorApi
{
    /// <summary>Executes the Extract operation.</summary>
    public HtmlArticleResult Extract(HtmlDocument document, IHtmlArticleConfiguration options);
    /// <summary>Executes the ExtractWithCancellation operation.</summary>
    public HtmlArticleResult ExtractWithCancellation(HtmlDocument document, IHtmlArticleConfiguration options, CancellationToken cancellationToken);
    /// <summary>Executes the IsProbablyReadable operation.</summary>
    public bool IsProbablyReadable(HtmlDocument document, IHtmlArticleConfiguration options);
    /// <summary>Runs the readability preflight with cancellation.</summary>
    public bool IsProbablyReadableWithCancellation(HtmlDocument document, IHtmlArticleConfiguration options, CancellationToken cancellationToken);
}
