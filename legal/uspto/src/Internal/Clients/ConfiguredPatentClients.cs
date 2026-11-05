namespace uspto;

internal class ConfiguredPatentClient : IPatentClient
{
    internal ConfiguredClientComponents Components;
    internal PatentApplicationSearchOperation Search;
    internal PatentApplicationExportOperation Export;
    internal PatentApplicationRetrievalOperation Retrieval;
    internal PatentDocumentDownloadOperation Documents;
    internal PatentStatusCodeOperation StatusCodes;
    internal PatentFullTextOperation FullText;
    internal DossierBuilder Dossier;
    internal PatentPortfolioLoader Portfolio;

    internal ConfiguredPatentClient(ConfiguredClientComponents components, IPatentsViewClient patentsView)
    {
        Components = components;
        Search = new PatentApplicationSearchOperation(components.Client, components.Options);
        Export = new PatentApplicationExportOperation(components.Client, components.Options);
        Retrieval = new PatentApplicationRetrievalOperation(components.Client, components.Options);
        Documents = new PatentDocumentDownloadOperation(components.Client, components.Options);
        StatusCodes = new PatentStatusCodeOperation(components.Client, components.Options);
        FullText = new PatentFullTextOperation(components.Client, components.Options);
        Dossier = new DossierBuilder(new PatentDossierSectionSource(components.Client, components.Options, patentsView));
        Portfolio = new PatentPortfolioLoader(new PatentPortfolioSource(components.Client, components.Options, patentsView));
    }

    public PatentApplicationResponse SearchPatentApplications(SearchRequest request)
    {
        return Search.SearchGet(request);
    }

    public PatentApplicationResponse GetAllPatentApplications(SearchRequest request)
    {
        return Search.SearchAll(request);
    }

    public DownloadResult DownloadPatentApplicationSearch(SearchRequest search, DownloadRequest download)
    {
        return Export.DownloadSearch(search, download);
    }

    public string ResolvePatentApplicationIdentifier(string identifier, RequestContext context)
    {
        return Retrieval.ResolveIdentifier(identifier, context);
    }

    public PatentFileWrapper GetPatentApplication(string identifier, RequestContext context)
    {
        return Retrieval.GetApplication(identifier, context);
    }

    public PatentApplicationMetadata GetPatentMetadata(string identifier, RequestContext context)
    {
        return Retrieval.GetApplication(identifier, context).Metadata;
    }

    public PatentTransactionHistory GetPatentTransactions(string identifier, RequestContext context)
    {
        return Retrieval.GetTransactions(identifier, context);
    }

    public PatentDocumentList GetPatentDocuments(string identifier, RequestContext context)
    {
        return Retrieval.GetDocuments(identifier, context);
    }

    public DownloadResult DownloadPatentDocument(string documentIdentifier, DownloadRequest request)
    {
        return Documents.Download(documentIdentifier, request);
    }

    public PatentAssociatedDocumentList GetPatentAssociatedDocuments(string identifier, RequestContext context)
    {
        return Retrieval.GetAssociatedDocuments(identifier, context);
    }

    public PatentContinuity GetPatentContinuity(string identifier, RequestContext context)
    {
        return Retrieval.GetContinuity(identifier, context);
    }

    public PatentForeignPriorityList GetPatentForeignPriorities(string identifier, RequestContext context)
    {
        return Retrieval.GetForeignPriorities(identifier, context);
    }

    public PatentAssignmentHistory GetPatentAssignments(string identifier, RequestContext context)
    {
        return Retrieval.GetAssignments(identifier, context);
    }

    public PatentAttorneyRecord GetPatentAttorneys(string identifier, RequestContext context)
    {
        return Retrieval.GetAttorneys(identifier, context);
    }

    public PatentTermAdjustment GetPatentTermAdjustment(string identifier, RequestContext context)
    {
        return Retrieval.GetTermAdjustment(identifier, context);
    }

    public PatentStatusCodeResponse SearchPatentStatusCodes(SearchRequest request)
    {
        return StatusCodes.SearchGet(request);
    }

    public PatentStatusCodeResponse GetAllPatentStatusCodes(SearchRequest request)
    {
        return StatusCodes.SearchAll(request);
    }

    public PatentFullText GetPatentFullText(string identifier, RequestContext context)
    {
        return FullText.Get(identifier, context);
    }

    public PatentDossier GetPatentDossier(string identifier, PatentDossierSelection selection, RequestContext context)
    {
        return Dossier.Build(identifier, selection, context);
    }

    public PatentPortfolio GetPatentPortfolio(PatentPortfolioRequest request)
    {
        return Portfolio.Load(request);
    }
}

internal class ConfiguredPetitionClient : IPetitionClient
{
    internal ConfiguredClientComponents Components;
    internal PetitionDecisionOperation Decisions;
    internal PetitionDecisionExportOperation Export;
    internal PetitionDocumentDownloadOperation Documents;

    internal ConfiguredPetitionClient(ConfiguredClientComponents components)
    {
        Components = components;
        Decisions = new PetitionDecisionOperation(components.Client, components.Options);
        Export = new PetitionDecisionExportOperation(components.Client, components.Options);
        Documents = new PetitionDocumentDownloadOperation(components.Client, components.Options);
    }

    public PetitionDecisionResponse SearchPetitionDecisions(SearchRequest request)
    {
        return Decisions.SearchGet(request);
    }

    public PetitionDecisionResponse GetAllPetitionDecisions(SearchRequest request)
    {
        return Decisions.SearchAll(request);
    }

    public DownloadResult DownloadPetitionDecisionSearch(PetitionDecisionExportRequest search, DownloadRequest download)
    {
        return Export.Download(search, download);
    }

    public PetitionDecision GetPetitionDecision(string recordIdentifier, bool includeDocuments, RequestContext context)
    {
        return Decisions.Get(recordIdentifier, includeDocuments, context);
    }

    public DownloadResult DownloadPetitionDocument(string documentIdentifier, DownloadRequest request)
    {
        return Documents.Download(documentIdentifier, request);
    }
}

internal class ConfiguredOfficeActionClient : IOfficeActionClient
{
    internal ConfiguredClientComponents Components;
    internal OfficeActionFieldOperation Fields;
    internal OfficeActionTextOperation Text;
    internal OfficeActionCitationOperation Citations;
    internal OfficeActionRejectionOperation Rejections;
    internal OfficeActionEnrichedCitationOperation Enriched;
    internal ProsecutionTimelineBuilder Timeline;

    internal ConfiguredOfficeActionClient(ConfiguredClientComponents components)
    {
        Components = components;
        Fields = new OfficeActionFieldOperation(components.Client, components.Options);
        Text = new OfficeActionTextOperation(components.Client, components.Options);
        Citations = new OfficeActionCitationOperation(components.Client, components.Options);
        Rejections = new OfficeActionRejectionOperation(components.Client, components.Options);
        Enriched = new OfficeActionEnrichedCitationOperation(components.Client, components.Options);
        Timeline = new ProsecutionTimelineBuilder(new ProsecutionTimelineSource(components.Client, components.Options));
    }

    public OfficeActionFieldResponse DiscoverOfficeActionFields(string datasetName, RequestContext context)
    {
        return Fields.Discover(datasetName, context);
    }

    public OfficeActionTextResponse SearchOfficeActions(SearchRequest request)
    {
        return Text.Search(request);
    }

    public OfficeActionTextResponse GetAllOfficeActions(SearchRequest request)
    {
        return Text.SearchAll(request);
    }

    public OfficeActionCitationResponse SearchOfficeActionCitations(SearchRequest request)
    {
        return Citations.Search(request);
    }

    public OfficeActionCitationResponse GetAllOfficeActionCitations(SearchRequest request)
    {
        return Citations.SearchAll(request);
    }

    public OfficeActionRejectionResponse SearchOfficeActionRejections(SearchRequest request)
    {
        return Rejections.Search(request);
    }

    public OfficeActionRejectionResponse GetAllOfficeActionRejections(SearchRequest request)
    {
        return Rejections.SearchAll(request);
    }

    public OfficeActionEnrichedCitationResponse SearchOfficeActionEnrichedCitations(SearchRequest request)
    {
        return Enriched.Search(request);
    }

    public OfficeActionEnrichedCitationResponse GetAllOfficeActionEnrichedCitations(SearchRequest request)
    {
        return Enriched.SearchAll(request);
    }

    public ProsecutionTimeline GetProsecutionTimeline(string applicationIdentifier, RequestContext context)
    {
        return Timeline.Build(applicationIdentifier, context);
    }
}
