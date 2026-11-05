namespace uspto;

/// <summary>
/// Provides read-only access to TSDR case status, documents, bundles, and capability metadata.
/// </summary>
public interface ITrademarkClient
{
    /// <summary>Gets parsed trademark case status.</summary>
    /// <param name="identifier">The serial, registration, reference, or international identifier.</param>
    /// <param name="format">The JSON or XML representation to parse.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed case-status record with optional bounded raw content.</returns>
    TrademarkCaseStatus GetTrademarkStatus(string identifier, DocumentFormat format, RequestContext context);

    /// <summary>Gets trademark case status in a selected representation.</summary>
    /// <param name="identifier">The trademark identifier.</param>
    /// <param name="format">The requested representation.</param>
    /// <param name="request">The content destination and transfer settings.</param>
    /// <returns>The streamed or file-backed content result.</returns>
    DownloadResult GetTrademarkStatusContent(string identifier, DocumentFormat format, DownloadRequest request);

    /// <summary>Downloads a PDF or ZIP trademark case-status representation as an attachment.</summary>
    /// <param name="identifier">The trademark identifier.</param>
    /// <param name="format">The requested PDF or ZIP representation.</param>
    /// <param name="request">The download destination and transfer settings.</param>
    /// <returns>The streamed or file-backed download result.</returns>
    DownloadResult DownloadTrademarkStatus(string identifier, DocumentFormat format, DownloadRequest request);

    /// <summary>Gets trademark case document metadata.</summary>
    /// <param name="identifier">The trademark identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed document listing.</returns>
    TrademarkDocumentCollection GetTrademarkDocuments(string identifier, RequestContext context);

    /// <summary>Gets metadata for one trademark document.</summary>
    /// <param name="identifier">The trademark identifier.</param>
    /// <param name="documentIdentifier">The service document identifier.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed document metadata.</returns>
    TrademarkDocument GetTrademarkDocumentInfo(string identifier, string documentIdentifier, RequestContext context);

    /// <summary>Gets one trademark case document in PDF or ZIP format.</summary>
    /// <param name="identifier">The trademark identifier.</param>
    /// <param name="documentIdentifier">The service document identifier.</param>
    /// <param name="format">The requested PDF or ZIP representation.</param>
    /// <param name="request">The content destination and transfer settings.</param>
    /// <returns>The streamed or file-backed content result.</returns>
    DownloadResult GetTrademarkDocumentContent(string identifier, string documentIdentifier, DocumentFormat format, DownloadRequest request);

    /// <summary>Downloads one trademark case document.</summary>
    /// <param name="identifier">The trademark identifier.</param>
    /// <param name="documentIdentifier">The service document identifier.</param>
    /// <param name="format">The requested PDF or ZIP representation.</param>
    /// <param name="request">The download request.</param>
    /// <returns>The download result.</returns>
    DownloadResult DownloadTrademarkDocument(string identifier, string documentIdentifier, DocumentFormat format, DownloadRequest request);

    /// <summary>Downloads one trademark document page image.</summary>
    /// <param name="identifier">The trademark identifier.</param>
    /// <param name="documentIdentifier">The service document identifier.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="request">The download request.</param>
    /// <returns>The download result.</returns>
    DownloadResult DownloadTrademarkPageMedia(string identifier, string documentIdentifier, int pageNumber, DownloadRequest request);

    /// <summary>Gets all documents for one trademark case in PDF or ZIP format.</summary>
    /// <param name="identifier">The trademark identifier.</param>
    /// <param name="format">The requested PDF or ZIP representation.</param>
    /// <param name="request">The content destination and transfer settings.</param>
    /// <returns>The streamed or file-backed content result.</returns>
    DownloadResult GetTrademarkCaseDocumentsContent(string identifier, DocumentFormat format, DownloadRequest request);

    /// <summary>Downloads all documents for one trademark case in PDF or ZIP format.</summary>
    /// <param name="identifier">The trademark identifier.</param>
    /// <param name="format">The requested PDF or ZIP representation.</param>
    /// <param name="request">The download request.</param>
    /// <returns>The download result.</returns>
    DownloadResult DownloadTrademarkCaseDocuments(string identifier, DocumentFormat format, DownloadRequest request);

    /// <summary>Downloads the server-generated XML bundle for one trademark case.</summary>
    /// <param name="identifier">The trademark identifier.</param>
    /// <param name="request">The download request.</param>
    /// <returns>The download result.</returns>
    DownloadResult DownloadTrademarkCaseBundle(string identifier, DownloadRequest request);

    /// <summary>Downloads a selected multi-case trademark bundle.</summary>
    /// <param name="identifiers">The trademark identifiers.</param>
    /// <param name="format">The requested PDF, XML, or ZIP representation.</param>
    /// <param name="request">The download request.</param>
    /// <returns>The download result.</returns>
    DownloadResult DownloadTrademarkMultiCaseBundle(string[] identifiers, DocumentFormat format, DownloadRequest request);

    /// <summary>Gets the last case-status update information.</summary>
    /// <param name="identifier">The trademark identifier.</param>
    /// <param name="format">The requested JSON or XML representation.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed last-update information.</returns>
    TrademarkLastUpdateResult GetTrademarkLastUpdate(string identifier, DocumentFormat format, RequestContext context);

    /// <summary>Gets a bounded multi-case trademark status transaction.</summary>
    /// <param name="identifiers">The trademark identifiers.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed multi-case transaction.</returns>
    TrademarkMultiStatusResult GetTrademarkMultiStatus(string[] identifiers, RequestContext context);

    /// <summary>Gets a composite trademark dossier.</summary>
    /// <param name="identifier">The trademark identifier.</param>
    /// <param name="selection">The sections and bounded document content to include.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed composite dossier with per-section outcomes.</returns>
    TrademarkDossier GetTrademarkDossier(string identifier, TrademarkDossierSelection selection, RequestContext context);

    /// <summary>Gets current capability metadata for Trademark Decisions and Proceedings.</summary>
    /// <param name="context">The request context.</param>
    /// <returns>The typed availability state and generic bulk-discovery alternative.</returns>
    TrademarkDecisionCapabilityResult GetTrademarkDecisionCapability(RequestContext context);
}
