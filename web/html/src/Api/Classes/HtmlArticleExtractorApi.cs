using System;
using System.Collections.Generic;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the HtmlArticleExtractor operations.</summary>
public class HtmlArticleExtractorApi : IHtmlArticleExtractorApi
{
    /// <summary>Executes the Extract operation.</summary>
    public HtmlArticleResult Extract(HtmlDocument document, IHtmlArticleConfiguration options)
    {
        return HtmlArticleExtractor.Extract(document,
            CreateOptions(options));
    }

    /// <summary>Executes the ExtractWithCancellation operation.</summary>
    public HtmlArticleResult ExtractWithCancellation(HtmlDocument document, IHtmlArticleConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlArticleExtractor.Extract(document,
            CreateOptions(options), cancellationToken);
    }

    /// <summary>Executes the IsProbablyReadable operation.</summary>
    public bool IsProbablyReadable(HtmlDocument document, IHtmlArticleConfiguration options)
    {
        return HtmlArticleExtractor.IsProbablyReadable(document,
            CreateOptions(options));
    }

    /// <summary>Runs the readability preflight with cancellation.</summary>
    /// <param name = "document">The parsed document to inspect.</param>
    /// <param name = "options">The scoring and resource settings to copy.</param>
    /// <param name = "cancellationToken">The token checked during the preflight.</param>
    /// <returns><c>true</c> when the document passes the readability heuristic; otherwise, <c>false</c>.</returns>
    /// <exception cref = "OperationCanceledException">Thrown when cancellation is requested before the preflight completes.</exception>
    public bool IsProbablyReadableWithCancellation(HtmlDocument document,
        IHtmlArticleConfiguration options,
        CancellationToken cancellationToken)
    {
        return HtmlArticleExtractor.IsProbablyReadable(document,
            CreateOptions(options), cancellationToken);
    }

    internal static HtmlArticleOptions CreateOptions(
        IHtmlArticleConfiguration options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return options.Compile().CreateOptions();
    }
}
