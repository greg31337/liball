using System;
using System.Threading;

namespace html;
/// <summary>Defines instance-based access to the HtmlMetadataExtractor operations.</summary>
public interface IHtmlMetadataExtractorApi
{
    /// <summary>Executes the Extract operation.</summary>
    public HtmlMetadata Extract(HtmlDocument document);
    /// <summary>Extracts metadata using a defensive snapshot of the supplied options.</summary>
    public HtmlMetadata ExtractWithOptions(HtmlDocument document, IHtmlMetadataOptions options);
    /// <summary>Extracts metadata using bounded options and a cancellation token.</summary>
    public HtmlMetadata ExtractWithCancellation(HtmlDocument document, IHtmlMetadataOptions options, CancellationToken cancellationToken);
}
