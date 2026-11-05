namespace uspto;

internal class ConfiguredTrademarkClient : ITrademarkClient
{
    internal ConfiguredClientComponents Components;
    internal TrademarkStatusOperation Status;
    internal TrademarkStatusContentOperation StatusContent;
    internal TrademarkDocumentOperation Documents;
    internal TrademarkCaseDocumentTransferOperation Cases;
    internal TrademarkMultiCaseBundleOperation MultiCases;
    internal TrademarkTransactionOperation Transactions;
    internal TrademarkDossierBuilder Dossier;

    internal ConfiguredTrademarkClient(ConfiguredClientComponents components)
    {
        Components = components;
        Status = new TrademarkStatusOperation(components.Client, components.Options);
        StatusContent = new TrademarkStatusContentOperation(components.Client, components.Options);
        Documents = new TrademarkDocumentOperation(components.Client, components.Options);
        Cases = new TrademarkCaseDocumentTransferOperation(components.Client, components.Options);
        MultiCases = new TrademarkMultiCaseBundleOperation(components.Client, components.Options);
        Transactions = new TrademarkTransactionOperation(components.Client, components.Options);
        Dossier = new TrademarkDossierBuilder(new TrademarkDossierSource(components.Client, components.Options));
    }

    public TrademarkCaseStatus GetTrademarkStatus(string identifier, DocumentFormat format, RequestContext context)
    {
        return Status.GetStatus(identifier, format, context);
    }

    public DownloadResult GetTrademarkStatusContent(string identifier, DocumentFormat format, DownloadRequest request)
    {
        return StatusContent.GetContent(identifier, format, request);
    }

    public DownloadResult DownloadTrademarkStatus(string identifier, DocumentFormat format, DownloadRequest request)
    {
        return StatusContent.Download(identifier, format, request);
    }

    public TrademarkDocumentCollection GetTrademarkDocuments(string identifier, RequestContext context)
    {
        return Documents.GetDocuments(identifier, context);
    }

    public TrademarkDocument GetTrademarkDocumentInfo(string identifier, string documentIdentifier, RequestContext context)
    {
        return Documents.GetDocument(identifier, documentIdentifier, context);
    }

    public DownloadResult GetTrademarkDocumentContent(string identifier, string documentIdentifier, DocumentFormat format, DownloadRequest request)
    {
        return Documents.GetContent(identifier, documentIdentifier, format, request);
    }

    public DownloadResult DownloadTrademarkDocument(string identifier, string documentIdentifier, DocumentFormat format, DownloadRequest request)
    {
        return Documents.Download(identifier, documentIdentifier, format, request);
    }

    public DownloadResult DownloadTrademarkPageMedia(string identifier, string documentIdentifier, int pageNumber, DownloadRequest request)
    {
        return Documents.DownloadPageMedia(identifier, documentIdentifier, pageNumber, request);
    }

    public DownloadResult GetTrademarkCaseDocumentsContent(string identifier, DocumentFormat format, DownloadRequest request)
    {
        return Cases.GetContent(identifier, format, request);
    }

    public DownloadResult DownloadTrademarkCaseDocuments(string identifier, DocumentFormat format, DownloadRequest request)
    {
        return Cases.DownloadDocuments(identifier, format, request);
    }

    public DownloadResult DownloadTrademarkCaseBundle(string identifier, DownloadRequest request)
    {
        return Cases.DownloadBundle(identifier, request);
    }

    public DownloadResult DownloadTrademarkMultiCaseBundle(string[] identifiers, DocumentFormat format, DownloadRequest request)
    {
        return MultiCases.Download(identifiers, format, request);
    }

    public TrademarkLastUpdateResult GetTrademarkLastUpdate(string identifier, DocumentFormat format, RequestContext context)
    {
        return Transactions.GetLastUpdate(identifier, format, context);
    }

    public TrademarkMultiStatusResult GetTrademarkMultiStatus(string[] identifiers, RequestContext context)
    {
        return Transactions.GetMultiStatus(identifiers, context);
    }

    public TrademarkDossier GetTrademarkDossier(string identifier, TrademarkDossierSelection selection, RequestContext context)
    {
        return Dossier.Build(identifier, selection, context);
    }

    public TrademarkDecisionCapabilityResult GetTrademarkDecisionCapability(RequestContext context)
    {
        return TrademarkDecisionCapabilityProvider.GetCapability(context);
    }
}
