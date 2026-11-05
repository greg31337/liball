namespace uspto;

internal class ConfiguredPtabClient : IPtabClient
{
    internal ConfiguredClientComponents Components;
    internal PtabProceedingOperation Proceedings;
    internal PtabProceedingExportOperation ProceedingExport;
    internal PtabTrialDecisionOperation TrialDecisions;
    internal PtabTrialDecisionExportOperation TrialDecisionExport;
    internal PtabTrialDocumentOperation TrialDocuments;
    internal PtabTrialDocumentExportOperation TrialDocumentExport;
    internal PtabAppealDecisionOperation Appeals;
    internal PtabAppealDecisionExportOperation AppealExport;
    internal PtabInterferenceDecisionOperation Interferences;
    internal PtabInterferenceDecisionExportOperation InterferenceExport;
    internal PtabDocumentDownloadOperation Documents;
    internal PtabDossierBuilder Dossier;

    internal ConfiguredPtabClient(ConfiguredClientComponents components)
    {
        Components = components;
        Proceedings = new PtabProceedingOperation(components.Client, components.Options);
        ProceedingExport = new PtabProceedingExportOperation(components.Client, components.Options);
        TrialDecisions = new PtabTrialDecisionOperation(components.Client, components.Options);
        TrialDecisionExport = new PtabTrialDecisionExportOperation(components.Client, components.Options);
        TrialDocuments = new PtabTrialDocumentOperation(components.Client, components.Options);
        TrialDocumentExport = new PtabTrialDocumentExportOperation(components.Client, components.Options);
        Appeals = new PtabAppealDecisionOperation(components.Client, components.Options);
        AppealExport = new PtabAppealDecisionExportOperation(components.Client, components.Options);
        Interferences = new PtabInterferenceDecisionOperation(components.Client, components.Options);
        InterferenceExport = new PtabInterferenceDecisionExportOperation(components.Client, components.Options);
        Documents = new PtabDocumentDownloadOperation(components.Client, components.Options);
        Dossier = new PtabDossierBuilder(new PtabDossierSource(components.Client, components.Options));
    }

    public PtabTrialProceedingResponse SearchPtabProceedings(SearchRequest request)
    {
        return Proceedings.SearchGet(request);
    }

    public PtabTrialProceedingResponse GetAllPtabProceedings(SearchRequest request)
    {
        return Proceedings.SearchAll(request);
    }

    public DownloadResult DownloadPtabProceedingSearch(SearchRequest search, DownloadRequest download)
    {
        return ProceedingExport.Download(search, download);
    }

    public PtabTrialProceeding GetPtabProceeding(string trialNumber, RequestContext context)
    {
        return Proceedings.Get(trialNumber, context);
    }

    public PtabTrialDecisionResponse SearchPtabTrialDecisions(SearchRequest request)
    {
        return TrialDecisions.SearchGet(request);
    }

    public PtabTrialDecisionResponse GetAllPtabTrialDecisions(SearchRequest request)
    {
        return TrialDecisions.SearchAll(request);
    }

    public DownloadResult DownloadPtabTrialDecisionSearch(SearchRequest search, DownloadRequest download)
    {
        return TrialDecisionExport.Download(search, download);
    }

    public PtabTrialDecision GetPtabTrialDecision(string documentIdentifier, RequestContext context)
    {
        return TrialDecisions.GetDocument(documentIdentifier, context);
    }

    public PtabTrialDecisionResponse GetPtabTrialDecisionsByTrial(string trialNumber, RequestContext context)
    {
        return TrialDecisions.GetByTrial(trialNumber, context);
    }

    public PtabTrialDocumentResponse SearchPtabTrialDocuments(SearchRequest request)
    {
        return TrialDocuments.SearchGet(request);
    }

    public PtabTrialDocumentResponse GetAllPtabTrialDocuments(SearchRequest request)
    {
        return TrialDocuments.SearchAll(request);
    }

    public DownloadResult DownloadPtabTrialDocumentSearch(SearchRequest search, DownloadRequest download)
    {
        return TrialDocumentExport.Download(search, download);
    }

    public PtabTrialDocument GetPtabTrialDocument(string documentIdentifier, RequestContext context)
    {
        return TrialDocuments.GetDocument(documentIdentifier, context);
    }

    public PtabTrialDocumentResponse GetPtabTrialDocumentsByTrial(string trialNumber, RequestContext context)
    {
        return TrialDocuments.GetByTrial(trialNumber, context);
    }

    public PtabAppealDecisionResponse SearchPtabAppealDecisions(SearchRequest request)
    {
        return Appeals.SearchGet(request);
    }

    public PtabAppealDecisionResponse GetAllPtabAppealDecisions(SearchRequest request)
    {
        return Appeals.SearchAll(request);
    }

    public DownloadResult DownloadPtabAppealDecisionSearch(SearchRequest search, DownloadRequest download)
    {
        return AppealExport.Download(search, download);
    }

    public PtabAppealDecision GetPtabAppealDecision(string documentIdentifier, RequestContext context)
    {
        return Appeals.GetDocument(documentIdentifier, context);
    }

    public PtabAppealDecisionResponse GetPtabAppealDecisionsByAppeal(string appealNumber, RequestContext context)
    {
        return Appeals.GetByAppeal(appealNumber, context);
    }

    public PtabInterferenceDecisionResponse SearchPtabInterferenceDecisions(SearchRequest request)
    {
        return Interferences.SearchGet(request);
    }

    public PtabInterferenceDecisionResponse GetAllPtabInterferenceDecisions(SearchRequest request)
    {
        return Interferences.SearchAll(request);
    }

    public DownloadResult DownloadPtabInterferenceDecisionSearch(SearchRequest search, DownloadRequest download)
    {
        return InterferenceExport.Download(search, download);
    }

    public PtabInterferenceDecision GetPtabInterferenceDecision(string documentIdentifier, RequestContext context)
    {
        return Interferences.GetDocument(documentIdentifier, context);
    }

    public PtabInterferenceDecisionResponse GetPtabInterferenceDecisionsByInterference(string interferenceNumber, RequestContext context)
    {
        return Interferences.GetByInterference(interferenceNumber, context);
    }

    public DownloadResult DownloadPtabDocument(PtabCollection collection, string documentIdentifier, DownloadRequest request)
    {
        return Documents.Download(collection, documentIdentifier, request);
    }

    public PtabDossier GetPtabDossier(string trialNumber, PtabDossierSelection selection, RequestContext context)
    {
        return Dossier.Build(trialNumber, selection, context);
    }
}
