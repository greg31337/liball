namespace uspto;

internal interface IPatentDossierSectionSource
{
    string ResolveDossierIdentifier(string identifier, RequestContext context);

    PatentFileWrapper GetDossierApplication(string applicationNumber, RequestContext context);

    PatentTransactionHistory GetDossierTransactions(string applicationNumber, RequestContext context);

    PatentDocumentList GetDossierDocuments(string applicationNumber, RequestContext context);

    PatentAssociatedDocumentList GetDossierAssociatedDocuments(string applicationNumber, RequestContext context);

    PatentContinuity GetDossierContinuity(string applicationNumber, RequestContext context);

    PatentForeignPriorityList GetDossierForeignPriorities(string applicationNumber, RequestContext context);

    PatentAssignmentHistory GetDossierAssignments(string applicationNumber, RequestContext context);

    PatentAttorneyRecord GetDossierAttorneys(string applicationNumber, RequestContext context);

    PatentTermAdjustment GetDossierTermAdjustment(string applicationNumber, RequestContext context);

    PatentFullText GetDossierFullText(PatentAssociatedDocumentList documents, RequestContext context);

    PatentDossierResearchData GetDossierResearch(PatentFileWrapper application, RequestContext context);
}

internal class PatentDossierSectionSource : IPatentDossierSectionSource
{
    internal PatentApplicationRetrievalOperation Retrieval;
    internal PatentFullTextOperation FullText;
    internal IPatentsViewClient? ResearchClient;

    internal PatentDossierSectionSource(global::System.Net.Http.HttpClient client, UsptoClientOptions options, IPatentsViewClient? researchClient)
    {
        Retrieval = new PatentApplicationRetrievalOperation(client, options);
        FullText = new PatentFullTextOperation(client, options);
        ResearchClient = researchClient;
    }

    public string ResolveDossierIdentifier(string identifier, RequestContext context)
    {
        return Retrieval.ResolveIdentifier(identifier, context);
    }

    public PatentFileWrapper GetDossierApplication(string applicationNumber, RequestContext context)
    {
        return Retrieval.GetNormalizedApplication(applicationNumber, context);
    }

    public PatentTransactionHistory GetDossierTransactions(string applicationNumber, RequestContext context)
    {
        return Retrieval.GetNormalizedTransactions(applicationNumber, context);
    }

    public PatentDocumentList GetDossierDocuments(string applicationNumber, RequestContext context)
    {
        return Retrieval.GetNormalizedDocuments(applicationNumber, context);
    }

    public PatentAssociatedDocumentList GetDossierAssociatedDocuments(string applicationNumber, RequestContext context)
    {
        return Retrieval.GetNormalizedAssociatedDocuments(applicationNumber, context);
    }

    public PatentContinuity GetDossierContinuity(string applicationNumber, RequestContext context)
    {
        return Retrieval.GetNormalizedContinuity(applicationNumber, context);
    }

    public PatentForeignPriorityList GetDossierForeignPriorities(string applicationNumber, RequestContext context)
    {
        return Retrieval.GetNormalizedForeignPriorities(applicationNumber, context);
    }

    public PatentAssignmentHistory GetDossierAssignments(string applicationNumber, RequestContext context)
    {
        return Retrieval.GetNormalizedAssignments(applicationNumber, context);
    }

    public PatentAttorneyRecord GetDossierAttorneys(string applicationNumber, RequestContext context)
    {
        return Retrieval.GetNormalizedAttorneys(applicationNumber, context);
    }

    public PatentTermAdjustment GetDossierTermAdjustment(string applicationNumber, RequestContext context)
    {
        return Retrieval.GetNormalizedTermAdjustment(applicationNumber, context);
    }

    public PatentFullText GetDossierFullText(PatentAssociatedDocumentList documents, RequestContext context)
    {
        return FullText.GetFromAssociatedDocuments(documents, context);
    }

    public PatentDossierResearchData GetDossierResearch(PatentFileWrapper application, RequestContext context)
    {
        if (ResearchClient == null)
        {
            throw new ValidationException("A PatentsView client is required for the research dossier section.", "ResearchClient", "RESEARCH_NOT_CONFIGURED", null);
        }

        PatentDossierResearchData result = new PatentDossierResearchData();
        bool found = false;
        if (!global::System.String.IsNullOrWhiteSpace(application.Metadata.PatentNumber))
        {
            result.PatentRecord = ResearchClient.GetPatentsViewPatent(application.Metadata.PatentNumber ?? string.Empty, context);
            found = true;
        }

        if (application.Metadata.PublicationNumbers.Length > 0 && !global::System.String.IsNullOrWhiteSpace(application.Metadata.PublicationNumbers[0]))
        {
            result.PublicationRecord = ResearchClient.GetPatentsViewPublication(application.Metadata.PublicationNumbers[0], context);
            found = true;
        }

        if (!found)
        {
            throw new NotFoundException("The application has no patent or publication identifier for PatentsView lookup.", ApiService.PatentsView, application.ApplicationNumber, 0, false, null, null);
        }

        result.Provenance.Source = DataProvenance.PatentsView;
        result.Provenance.DatasetIdentifier = "PatentsView PatentSearch";
        result.Provenance.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.Provenance.HasRetrievedAt = true;
        result.Provenance.IsAuthoritative = false;
        bool allFromCache = true;
        if (result.PatentRecord != null)
        {
            ProvenanceLabeler.AddPatentField(result.Provenance, "PatentRecord", result.PatentRecord.Provenance);
            allFromCache = result.PatentRecord.Provenance.FromCache;
        }

        if (result.PublicationRecord != null)
        {
            ProvenanceLabeler.AddPatentField(result.Provenance, "PublicationRecord", result.PublicationRecord.Provenance);
            allFromCache = allFromCache && result.PublicationRecord.Provenance.FromCache;
        }

        result.Provenance.FromCache = allFromCache;
        return result;
    }
}

internal class PatentDossierTaskResult
{
    internal object? Value = null;
    internal global::System.Exception? Failure = null;
    internal bool IsSuccessful = false;
}

internal class DossierBuilder
{
    internal const int ApplicationSection = 1;
    internal const int TransactionsSection = 2;
    internal const int DocumentsSection = 3;
    internal const int AssociatedDocumentsSection = 4;
    internal const int ContinuitySection = 5;
    internal const int ForeignPrioritiesSection = 6;
    internal const int AssignmentsSection = 7;
    internal const int AttorneysSection = 8;
    internal const int TermAdjustmentSection = 9;
    internal const int FullTextSection = 10;
    internal const int ResearchSection = 11;

    internal IPatentDossierSectionSource Source;

    internal DossierBuilder(IPatentDossierSectionSource source)
    {
        if (source == null)
        {
            throw new ValidationException("A patent dossier section source is required.", "source", "REQUIRED", null);
        }

        Source = source;
    }

    internal PatentDossier Build(string identifier, PatentDossierSelection selection, RequestContext context)
    {
        if (global::System.String.IsNullOrWhiteSpace(identifier))
        {
            throw new ValidationException("A patent dossier identifier is required.", "identifier", "REQUIRED", null);
        }

        if (selection == null)
        {
            throw new ValidationException("A patent dossier section selection is required.", "selection", "REQUIRED", null);
        }

        if (context == null)
        {
            throw new ValidationException("A patent dossier request context is required.", "context", "REQUIRED", null);
        }

        if (CountSelectedSections(selection) == 0)
        {
            throw new ValidationException("At least one patent dossier section must be selected.", "selection", "SECTION_REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        string applicationNumber = Source.ResolveDossierIdentifier(identifier, context);
        PatentDossier result = CreateDossier(identifier, applicationNumber, selection, context);
        global::System.Collections.Generic.List<PatentDossierSectionResult> sections = new global::System.Collections.Generic.List<PatentDossierSectionResult>();
        global::System.Collections.Generic.List<SectionError> errors = new global::System.Collections.Generic.List<SectionError>();

        bool needsApplication = selection.IncludeApplication || selection.IncludeResearch;
        PatentDossierTaskResult applicationLoad = new PatentDossierTaskResult();
        PatentFileWrapper? application = null;
        if (needsApplication)
        {
            applicationLoad = LoadSection(ApplicationSection, applicationNumber, null, null, context);
            if (applicationLoad.IsSuccessful)
            {
                application = (PatentFileWrapper)(applicationLoad.Value ?? new PatentFileWrapper());
                if (selection.IncludeApplication)
                {
                    AssignSectionValue(result, ApplicationSection, application);
                    AddSectionSuccess(result, sections, ApplicationSection, application);
                }
            }
            else if (selection.IncludeApplication)
            {
                AddSectionFailure(result, sections, errors, ApplicationSection, applicationLoad.Failure);
            }
        }

        LoadOptionalSection(result, sections, errors, TransactionsSection, selection.IncludeTransactions, applicationNumber, null, null, context);
        LoadOptionalSection(result, sections, errors, DocumentsSection, selection.IncludeDocuments, applicationNumber, null, null, context);

        bool needsAssociatedDocuments = selection.IncludeAssociatedDocuments || selection.IncludeFullText;
        PatentDossierTaskResult associatedLoad = new PatentDossierTaskResult();
        PatentAssociatedDocumentList? associatedDocuments = null;
        if (needsAssociatedDocuments)
        {
            associatedLoad = LoadSection(AssociatedDocumentsSection, applicationNumber, null, null, context);
            if (associatedLoad.IsSuccessful)
            {
                associatedDocuments = (PatentAssociatedDocumentList)(associatedLoad.Value ?? new PatentAssociatedDocumentList());
                if (selection.IncludeAssociatedDocuments)
                {
                    AssignSectionValue(result, AssociatedDocumentsSection, associatedDocuments);
                    AddSectionSuccess(result, sections, AssociatedDocumentsSection, associatedDocuments);
                }
            }
            else if (selection.IncludeAssociatedDocuments)
            {
                AddSectionFailure(result, sections, errors, AssociatedDocumentsSection, associatedLoad.Failure);
            }
        }

        LoadOptionalSection(result, sections, errors, ContinuitySection, selection.IncludeContinuity, applicationNumber, null, null, context);
        LoadOptionalSection(result, sections, errors, ForeignPrioritiesSection, selection.IncludeForeignPriorities, applicationNumber, null, null, context);
        LoadOptionalSection(result, sections, errors, AssignmentsSection, selection.IncludeAssignments, applicationNumber, null, null, context);
        LoadOptionalSection(result, sections, errors, AttorneysSection, selection.IncludeAttorneys, applicationNumber, null, null, context);
        LoadOptionalSection(result, sections, errors, TermAdjustmentSection, selection.IncludeTermAdjustment, applicationNumber, null, null, context);

        if (selection.IncludeFullText)
        {
            if (associatedLoad.IsSuccessful && associatedDocuments != null)
            {
                LoadOptionalSection(result, sections, errors, FullTextSection, true, applicationNumber, associatedDocuments, null, context);
            }
            else
            {
                AddSectionFailure(result, sections, errors, FullTextSection, associatedLoad.Failure);
            }
        }

        if (selection.IncludeResearch)
        {
            if (applicationLoad.IsSuccessful && application != null)
            {
                LoadOptionalSection(result, sections, errors, ResearchSection, true, applicationNumber, null, application, context);
            }
            else
            {
                AddSectionFailure(result, sections, errors, ResearchSection, applicationLoad.Failure);
            }
        }

        result.Sections = sections.ToArray();
        result.Errors = errors.ToArray();
        result.IsComplete = result.CompletedSectionCount == result.RequestedSectionCount;
        result.HasPartialSuccess = result.CompletedSectionCount > 0 && result.FailedSectionCount > 0;
        return result;
    }

    internal void LoadOptionalSection(PatentDossier result, global::System.Collections.Generic.List<PatentDossierSectionResult> sections, global::System.Collections.Generic.List<SectionError> errors, int section, bool selected, string applicationNumber, PatentAssociatedDocumentList? associatedDocuments, PatentFileWrapper? application, RequestContext context)
    {
        if (!selected)
        {
            return;
        }

        PatentDossierTaskResult loaded = LoadSection(section, applicationNumber, associatedDocuments, application, context);
        if (loaded.IsSuccessful && loaded.Value != null)
        {
            AssignSectionValue(result, section, loaded.Value);
            AddSectionSuccess(result, sections, section, loaded.Value);
        }
        else
        {
            AddSectionFailure(result, sections, errors, section, loaded.Failure);
        }
    }

    internal PatentDossierTaskResult LoadSection(int section, string applicationNumber, PatentAssociatedDocumentList? associatedDocuments, PatentFileWrapper? application, RequestContext context)
    {
        global::System.Threading.Tasks.Task<object> task = global::System.Threading.Tasks.Task.Run(() => ExecuteSection(section, applicationNumber, associatedDocuments, application, context));
        while (!task.IsCompleted)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            global::System.Threading.Thread.Yield();
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PatentDossierTaskResult result = new PatentDossierTaskResult();
        if (task.IsCompletedSuccessfully)
        {
            result.Value = task.Result;
            result.IsSuccessful = true;
        }
        else if (task.Exception != null)
        {
            result.Failure = task.Exception.GetBaseException();
        }
        else
        {
            result.Failure = new ValidationException("The patent dossier section did not complete.", "section", "SECTION_INCOMPLETE", null);
        }

        return result;
    }

    internal object ExecuteSection(int section, string applicationNumber, PatentAssociatedDocumentList? associatedDocuments, PatentFileWrapper? application, RequestContext context)
    {
        switch (section)
        {
            case ApplicationSection:
                return Source.GetDossierApplication(applicationNumber, context);
            case TransactionsSection:
                return Source.GetDossierTransactions(applicationNumber, context);
            case DocumentsSection:
                return Source.GetDossierDocuments(applicationNumber, context);
            case AssociatedDocumentsSection:
                return Source.GetDossierAssociatedDocuments(applicationNumber, context);
            case ContinuitySection:
                return Source.GetDossierContinuity(applicationNumber, context);
            case ForeignPrioritiesSection:
                return Source.GetDossierForeignPriorities(applicationNumber, context);
            case AssignmentsSection:
                return Source.GetDossierAssignments(applicationNumber, context);
            case AttorneysSection:
                return Source.GetDossierAttorneys(applicationNumber, context);
            case TermAdjustmentSection:
                return Source.GetDossierTermAdjustment(applicationNumber, context);
            case FullTextSection:
                if (associatedDocuments == null)
                {
                    throw new ValidationException("Associated documents are required to hydrate patent full text.", "associatedDocuments", "REQUIRED", null);
                }

                return Source.GetDossierFullText(associatedDocuments, context);
            case ResearchSection:
                if (application == null)
                {
                    throw new ValidationException("Application metadata is required to hydrate research data.", "application", "REQUIRED", null);
                }

                return Source.GetDossierResearch(application, context);
            default:
                throw new ValidationException("The patent dossier section is not supported.", "section", "UNSUPPORTED_SECTION", null);
        }
    }

    internal static void AssignSectionValue(PatentDossier result, int section, object value)
    {
        switch (section)
        {
            case ApplicationSection:
                result.Official.Application = (PatentFileWrapper)value;
                break;
            case TransactionsSection:
                result.Official.Transactions = (PatentTransactionHistory)value;
                break;
            case DocumentsSection:
                result.Official.Documents = (PatentDocumentList)value;
                break;
            case AssociatedDocumentsSection:
                result.Official.AssociatedDocuments = (PatentAssociatedDocumentList)value;
                break;
            case ContinuitySection:
                result.Official.Continuity = (PatentContinuity)value;
                break;
            case ForeignPrioritiesSection:
                result.Official.ForeignPriorities = (PatentForeignPriorityList)value;
                break;
            case AssignmentsSection:
                result.Official.Assignments = (PatentAssignmentHistory)value;
                break;
            case AttorneysSection:
                result.Official.Attorneys = (PatentAttorneyRecord)value;
                break;
            case TermAdjustmentSection:
                result.Official.TermAdjustment = (PatentTermAdjustment)value;
                break;
            case FullTextSection:
                result.Official.FullText = (PatentFullText)value;
                break;
            case ResearchSection:
                result.Research = (PatentDossierResearchData)value;
                break;
        }
    }

    internal static void AddSectionSuccess(PatentDossier result, global::System.Collections.Generic.List<PatentDossierSectionResult> sections, int section, object value)
    {
        PatentDossierSectionResult item = new PatentDossierSectionResult();
        item.Section = SectionName(section);
        item.IsResearch = section == ResearchSection;
        item.IsComplete = true;
        item.Provenance = ReadSectionProvenance(section, value);
        sections.Add(item);
        ProvenanceLabeler.AddPatentField(result.Provenance, SectionFieldPath(section), item.Provenance);
        if (section == ResearchSection)
        {
            PatentDossierResearchData research = (PatentDossierResearchData)value;
            if (research.PatentRecord != null)
            {
                ProvenanceLabeler.AddPatentField(result.Provenance, "Research.PatentRecord", research.PatentRecord.Provenance);
            }

            if (research.PublicationRecord != null)
            {
                ProvenanceLabeler.AddPatentField(result.Provenance, "Research.PublicationRecord", research.PublicationRecord.Provenance);
            }
        }

        result.RequestedSectionCount += 1;
        result.CompletedSectionCount += 1;
    }

    internal static void AddSectionFailure(PatentDossier result, global::System.Collections.Generic.List<PatentDossierSectionResult> sections, global::System.Collections.Generic.List<SectionError> errors, int section, global::System.Exception? failure)
    {
        global::System.Exception actual = failure ?? new ValidationException("The patent dossier section failed.", "section", "SECTION_FAILED", null);
        SectionError error = CreateSectionError(section, actual);
        PatentDossierSectionResult item = new PatentDossierSectionResult();
        item.Section = SectionName(section);
        item.IsResearch = section == ResearchSection;
        item.Error = error;
        sections.Add(item);
        errors.Add(error);
        result.RequestedSectionCount += 1;
        result.FailedSectionCount += 1;
    }

    internal static SectionError CreateSectionError(int section, global::System.Exception failure)
    {
        SectionError result = new SectionError();
        result.Section = SectionName(section);
        result.ExceptionType = failure.GetType().Name;
        result.Message = "The patent dossier section failed.";
        string typeName = failure.GetType().FullName ?? string.Empty;
        if (typeName.Equals("uspto.ValidationException", global::System.StringComparison.Ordinal))
        {
            ValidationException exception = (ValidationException)failure;
            result.Message = exception.Message;
            result.ErrorCode = exception.ErrorCode;
        }
        else if (typeName.Equals("uspto.ApiException", global::System.StringComparison.Ordinal))
        {
            ApiException exception = (ApiException)failure;
            result.Message = exception.Message;
            result.ErrorCode = exception.ErrorCode;
            result.Service = exception.Service;
            result.StatusCode = exception.StatusCode;
            result.HasStatusCode = exception.HasStatusCode;
            result.RequestIdentifier = exception.RequestIdentifier;
            result.IsRetryable = exception.IsRetryable;
        }
        else if (typeName.Equals("uspto.NotFoundException", global::System.StringComparison.Ordinal))
        {
            NotFoundException exception = (NotFoundException)failure;
            result.Message = exception.Message;
            result.ErrorCode = "NOT_FOUND";
            result.Service = exception.Service;
            result.StatusCode = exception.StatusCode;
            result.HasStatusCode = exception.HasStatusCode;
            result.RequestIdentifier = exception.RequestIdentifier;
        }
        else if (typeName.Equals("uspto.DownloadException", global::System.StringComparison.Ordinal))
        {
            DownloadException exception = (DownloadException)failure;
            result.Message = exception.Message;
            result.ErrorCode = "DOWNLOAD_FAILED";
            result.StatusCode = exception.StatusCode;
            result.HasStatusCode = exception.HasStatusCode;
            result.IsRetryable = exception.IsRetryable;
        }
        else if (typeName.Equals("uspto.AuthenticationException", global::System.StringComparison.Ordinal))
        {
            AuthenticationException exception = (AuthenticationException)failure;
            result.Message = exception.Message;
            result.ErrorCode = "AUTHENTICATION_FAILED";
            result.Service = exception.Service;
            result.StatusCode = exception.StatusCode;
            result.HasStatusCode = exception.HasStatusCode;
            result.RequestIdentifier = exception.RequestIdentifier;
        }
        else if (typeName.Equals("uspto.RateLimitException", global::System.StringComparison.Ordinal))
        {
            RateLimitException exception = (RateLimitException)failure;
            result.Message = exception.Message;
            result.ErrorCode = "RATE_LIMITED";
            result.Service = exception.Service;
            result.StatusCode = exception.StatusCode;
            result.HasStatusCode = exception.HasStatusCode;
            result.RequestIdentifier = exception.RequestIdentifier;
            result.IsRetryable = true;
        }
        else if (typeName.Equals("uspto.ServiceUnavailableException", global::System.StringComparison.Ordinal))
        {
            ServiceUnavailableException exception = (ServiceUnavailableException)failure;
            result.Message = exception.Message;
            result.ErrorCode = "SERVICE_UNAVAILABLE";
            result.Service = exception.Service;
            result.StatusCode = exception.StatusCode;
            result.HasStatusCode = exception.HasStatusCode;
            result.RequestIdentifier = exception.RequestIdentifier;
            result.IsRetryable = true;
        }
        else if (typeName.Equals("uspto.ParseException", global::System.StringComparison.Ordinal))
        {
            result.Message = failure.Message;
            result.ErrorCode = "PARSE_FAILED";
        }

        return result;
    }

    internal static PatentDataProvenance ReadSectionProvenance(int section, object value)
    {
        PatentDataProvenance? provenance = null;
        switch (section)
        {
            case ApplicationSection:
                provenance = ((PatentFileWrapper)value).Provenance;
                break;
            case TransactionsSection:
                provenance = ((PatentTransactionHistory)value).Provenance;
                break;
            case DocumentsSection:
                provenance = ((PatentDocumentList)value).Provenance;
                break;
            case AssociatedDocumentsSection:
                provenance = ((PatentAssociatedDocumentList)value).Provenance;
                break;
            case ContinuitySection:
                provenance = ((PatentContinuity)value).Provenance;
                break;
            case ForeignPrioritiesSection:
                provenance = ((PatentForeignPriorityList)value).Provenance;
                break;
            case AssignmentsSection:
                provenance = ((PatentAssignmentHistory)value).Provenance;
                break;
            case AttorneysSection:
                provenance = ((PatentAttorneyRecord)value).Provenance;
                break;
            case TermAdjustmentSection:
                provenance = ((PatentTermAdjustment)value).Provenance;
                break;
            case FullTextSection:
                provenance = ((PatentFullText)value).Provenance;
                break;
            case ResearchSection:
                provenance = ((PatentDossierResearchData)value).Provenance;
                break;
        }

        if (provenance == null)
        {
            return new PatentDataProvenance();
        }

        return OdpJsonParser.CopyProvenance(provenance);
    }

    internal static PatentDossier CreateDossier(string identifier, string applicationNumber, PatentDossierSelection selection, RequestContext context)
    {
        PatentDossier result = new PatentDossier();
        result.RequestedIdentifier = identifier;
        result.ApplicationNumber = applicationNumber;
        result.Selection = CopySelection(selection);
        result.Provenance.Source = DataProvenance.ClientDerived;
        result.Provenance.RequestIdentifier = context.CorrelationIdentifier;
        result.Provenance.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.Provenance.HasRetrievedAt = true;
        result.Provenance.IsAuthoritative = false;
        return result;
    }

    internal static PatentDossierSelection CopySelection(PatentDossierSelection source)
    {
        PatentDossierSelection result = new PatentDossierSelection();
        result.IncludeApplication = source.IncludeApplication;
        result.IncludeTransactions = source.IncludeTransactions;
        result.IncludeDocuments = source.IncludeDocuments;
        result.IncludeAssociatedDocuments = source.IncludeAssociatedDocuments;
        result.IncludeContinuity = source.IncludeContinuity;
        result.IncludeForeignPriorities = source.IncludeForeignPriorities;
        result.IncludeAssignments = source.IncludeAssignments;
        result.IncludeAttorneys = source.IncludeAttorneys;
        result.IncludeTermAdjustment = source.IncludeTermAdjustment;
        result.IncludeFullText = source.IncludeFullText;
        result.IncludeResearch = source.IncludeResearch;
        return result;
    }

    internal static string SectionFieldPath(int section)
    {
        switch (section)
        {
            case ApplicationSection:
                return "Official.Application";
            case TransactionsSection:
                return "Official.Transactions";
            case DocumentsSection:
                return "Official.Documents";
            case AssociatedDocumentsSection:
                return "Official.AssociatedDocuments";
            case ContinuitySection:
                return "Official.Continuity";
            case ForeignPrioritiesSection:
                return "Official.ForeignPriorities";
            case AssignmentsSection:
                return "Official.Assignments";
            case AttorneysSection:
                return "Official.Attorneys";
            case TermAdjustmentSection:
                return "Official.TermAdjustment";
            case FullTextSection:
                return "Official.FullText";
            case ResearchSection:
                return "Research";
            default:
                return "Unknown";
        }
    }

    internal static int CountSelectedSections(PatentDossierSelection selection)
    {
        int count = 0;
        if (selection.IncludeApplication)
        {
            count += 1;
        }

        if (selection.IncludeTransactions)
        {
            count += 1;
        }

        if (selection.IncludeDocuments)
        {
            count += 1;
        }

        if (selection.IncludeAssociatedDocuments)
        {
            count += 1;
        }

        if (selection.IncludeContinuity)
        {
            count += 1;
        }

        if (selection.IncludeForeignPriorities)
        {
            count += 1;
        }

        if (selection.IncludeAssignments)
        {
            count += 1;
        }

        if (selection.IncludeAttorneys)
        {
            count += 1;
        }

        if (selection.IncludeTermAdjustment)
        {
            count += 1;
        }

        if (selection.IncludeFullText)
        {
            count += 1;
        }

        if (selection.IncludeResearch)
        {
            count += 1;
        }

        return count;
    }

    internal static string SectionName(int section)
    {
        switch (section)
        {
            case ApplicationSection:
                return "application";
            case TransactionsSection:
                return "transactions";
            case DocumentsSection:
                return "documents";
            case AssociatedDocumentsSection:
                return "associatedDocuments";
            case ContinuitySection:
                return "continuity";
            case ForeignPrioritiesSection:
                return "foreignPriorities";
            case AssignmentsSection:
                return "assignments";
            case AttorneysSection:
                return "attorneys";
            case TermAdjustmentSection:
                return "termAdjustment";
            case FullTextSection:
                return "fullText";
            case ResearchSection:
                return "research";
            default:
                return "unknown";
        }
    }
}
