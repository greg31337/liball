using System;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the HtmlMetadataExtractor operations.</summary>
public class HtmlMetadataExtractorApi : IHtmlMetadataExtractorApi
{
    /// <summary>Executes the Extract operation.</summary>
    public HtmlMetadata Extract(HtmlDocument document)
    {
        return HtmlMetadataExtractor.Extract(document);
    }

    /// <summary>Extracts metadata using a defensive snapshot of the supplied options.</summary>
    /// <param name = "document">The parsed document to inspect.</param>
    /// <param name = "options">The field, source, and limit settings to copy.</param>
    /// <returns>Bounded, precedence-resolved metadata with candidate provenance.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public HtmlMetadata ExtractWithOptions(HtmlDocument document,
        IHtmlMetadataOptions options)
    {
        return HtmlMetadataExtractor.ExtractWithOptions(document, options,
            CancellationToken.None);
    }

    /// <summary>Extracts metadata using bounded options and a cancellation token.</summary>
    /// <param name = "document">The parsed document to inspect.</param>
    /// <param name = "options">The field, source, and limit settings to copy.</param>
    /// <param name = "cancellationToken">The token checked while extraction is running.</param>
    /// <returns>Bounded, precedence-resolved metadata with candidate provenance.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "OperationCanceledException">Thrown when cancellation is requested before extraction completes.</exception>
    public HtmlMetadata ExtractWithCancellation(HtmlDocument document,
        IHtmlMetadataOptions options, CancellationToken cancellationToken)
    {
        return HtmlMetadataExtractor.ExtractWithOptions(document, options,
            cancellationToken);
    }
}
