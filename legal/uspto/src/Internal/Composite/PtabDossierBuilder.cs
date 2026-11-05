namespace uspto;

internal interface IPtabDossierSource
{
    PtabTrialProceeding GetDossierProceeding(string trialNumber, RequestContext context);

    PtabTrialDecisionResponse GetDossierDecisions(string trialNumber, RequestContext context);

    PtabTrialDocumentResponse GetDossierDocuments(string trialNumber, RequestContext context);

    DownloadResult DownloadDossierDocument(PtabCollection collection, string documentIdentifier, DownloadRequest request);
}

internal class PtabDossierSource : IPtabDossierSource
{
    internal PtabProceedingOperation Proceedings;
    internal PtabTrialDecisionOperation Decisions;
    internal PtabTrialDocumentOperation Documents;
    internal PtabDocumentDownloadOperation Downloads;

    internal PtabDossierSource(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        if (client == null)
        {
            throw new ValidationException("An HTTP client is required.", "client", "REQUIRED", null);
        }

        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        Proceedings = new PtabProceedingOperation(client, options);
        Decisions = new PtabTrialDecisionOperation(client, options);
        Documents = new PtabTrialDocumentOperation(client, options);
        Downloads = new PtabDocumentDownloadOperation(client, options);
    }

    public PtabTrialProceeding GetDossierProceeding(string trialNumber, RequestContext context)
    {
        return Proceedings.Get(trialNumber, context);
    }

    public PtabTrialDecisionResponse GetDossierDecisions(string trialNumber, RequestContext context)
    {
        return Decisions.GetByTrial(trialNumber, context);
    }

    public PtabTrialDocumentResponse GetDossierDocuments(string trialNumber, RequestContext context)
    {
        return Documents.GetByTrial(trialNumber, context);
    }

    public DownloadResult DownloadDossierDocument(PtabCollection collection, string documentIdentifier, DownloadRequest request)
    {
        return Downloads.Download(collection, documentIdentifier, request);
    }
}

internal class PtabDossierTaskResult
{
    internal object? Value = null;
    internal global::System.Exception? Failure = null;
    internal bool IsSuccessful = false;
}

internal class PtabDossierBuilder
{
    internal const int ProceedingSection = 1;
    internal const int DecisionsSection = 2;
    internal const int DocumentsSection = 3;

    internal IPtabDossierSource Source;

    internal PtabDossierBuilder(IPtabDossierSource source)
    {
        if (source == null)
        {
            throw new ValidationException("A PTAB dossier source is required.", "source", "REQUIRED", null);
        }

        Source = source;
    }

    internal PtabDossier Build(string trialNumber, PtabDossierSelection selection, RequestContext context)
    {
        ValidateRequest(trialNumber, selection, context);
        context.CancellationToken.ThrowIfCancellationRequested();
        PtabDossier result = CreateDossier(trialNumber, selection, context);
        global::System.Collections.Generic.List<PtabDossierSectionResult> sections = new global::System.Collections.Generic.List<PtabDossierSectionResult>();
        global::System.Collections.Generic.List<SectionError> errors = new global::System.Collections.Generic.List<SectionError>();

        if (selection.IncludeProceeding)
        {
            PtabDossierTaskResult proceedingLoad = LoadDossierSection(ProceedingSection, trialNumber, context);
            if (proceedingLoad.IsSuccessful && proceedingLoad.Value != null)
            {
                result.Proceeding = (PtabTrialProceeding)proceedingLoad.Value;
                AddSectionSuccess(result, sections, "proceeding", result.Proceeding.Provenance);
            }
            else
            {
                AddSectionFailure(result, sections, errors, "proceeding", proceedingLoad.Failure);
            }
        }

        bool loadDecisions = selection.IncludeDecisions || selection.Downloads.Length > 0;
        PtabDossierTaskResult decisionLoad = new PtabDossierTaskResult();
        PtabTrialDecisionResponse? decisionResponse = null;
        if (loadDecisions)
        {
            decisionLoad = LoadDossierSection(DecisionsSection, trialNumber, context);
            if (decisionLoad.IsSuccessful && decisionLoad.Value != null)
            {
                decisionResponse = (PtabTrialDecisionResponse)decisionLoad.Value;
                if (selection.IncludeDecisions)
                {
                    result.Decisions = decisionResponse.Decisions;
                    AddSectionSuccess(result, sections, "decisions", decisionResponse.Provenance);
                }
            }
            else if (selection.IncludeDecisions)
            {
                AddSectionFailure(result, sections, errors, "decisions", decisionLoad.Failure);
            }
        }

        bool loadDocuments = selection.IncludeDocuments || selection.Downloads.Length > 0;
        PtabDossierTaskResult documentLoad = new PtabDossierTaskResult();
        PtabTrialDocumentResponse? documentResponse = null;
        if (loadDocuments)
        {
            documentLoad = LoadDossierSection(DocumentsSection, trialNumber, context);
            if (documentLoad.IsSuccessful && documentLoad.Value != null)
            {
                documentResponse = (PtabTrialDocumentResponse)documentLoad.Value;
                if (selection.IncludeDocuments)
                {
                    result.TrialDocuments = documentResponse.Documents;
                    AddSectionSuccess(result, sections, "documents", documentResponse.Provenance);
                }
            }
            else if (selection.IncludeDocuments)
            {
                AddSectionFailure(result, sections, errors, "documents", documentLoad.Failure);
            }
        }

        result.Documents = BuildDeduplicatedDocuments(decisionResponse, documentResponse, selection.IncludeDecisions, selection.IncludeDocuments);
        PtabDossierDocument[] downloadableDocuments = BuildDeduplicatedDocuments(decisionResponse, documentResponse, true, true);
        result.Downloads = LoadRequestedDownloads(result, sections, errors, downloadableDocuments, selection.Downloads, context);
        result.Sections = sections.ToArray();
        result.Errors = errors.ToArray();
        result.IsComplete = result.CompletedSectionCount == result.RequestedSectionCount;
        result.HasPartialSuccess = result.CompletedSectionCount > 0 && result.FailedSectionCount > 0;
        return result;
    }

    internal PtabDossierTaskResult LoadDossierSection(int section, string trialNumber, RequestContext context)
    {
        global::System.Threading.Tasks.Task<object> task = global::System.Threading.Tasks.Task.Run(() => ExecuteDossierSection(section, trialNumber, context));
        return AwaitTask(task, context, "The PTAB dossier section did not complete.");
    }

    internal object ExecuteDossierSection(int section, string trialNumber, RequestContext context)
    {
        switch (section)
        {
            case ProceedingSection:
                return Source.GetDossierProceeding(trialNumber, context);
            case DecisionsSection:
                return Source.GetDossierDecisions(trialNumber, context);
            case DocumentsSection:
                return Source.GetDossierDocuments(trialNumber, context);
            default:
                throw new ValidationException("The PTAB dossier section is not supported.", "section", "UNSUPPORTED_SECTION", null);
        }
    }

    internal PtabDossierDownloadResult[] LoadRequestedDownloads(PtabDossier result, global::System.Collections.Generic.List<PtabDossierSectionResult> sections, global::System.Collections.Generic.List<SectionError> errors, PtabDossierDocument[] documents, PtabDossierDownloadRequest[] requests, RequestContext context)
    {
        PtabDossierDownloadResult[] outcomes = new PtabDossierDownloadResult[requests.Length];
        int index;
        for (index = 0; index < requests.Length; index += 1)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            PtabDossierDownloadRequest request = requests[index];
            string documentIdentifier = request.DocumentIdentifier ?? string.Empty;
            string section = "download:" + documentIdentifier;
            PtabDossierDocument? document = FindDocument(documents, documentIdentifier);
            PtabDossierDownloadResult outcome = new PtabDossierDownloadResult();
            outcome.DocumentIdentifier = documentIdentifier;
            if (document == null)
            {
                ValidationException missing = new ValidationException("The requested PTAB dossier document was not found in the aggregated records.", "DocumentIdentifier", "DOCUMENT_NOT_FOUND", null);
                outcome.Error = AddSectionFailure(result, sections, errors, section, missing);
            }
            else
            {
                outcome.Collection = SelectDownloadCollection(document);
                global::System.Uri? sourceUri = FindDownloadUri(document);
                if (sourceUri == null)
                {
                    ValidationException missingUri = new ValidationException("The requested PTAB dossier document has no download URI.", "DownloadUri", "DOWNLOAD_URI_REQUIRED", null);
                    outcome.Error = AddSectionFailure(result, sections, errors, section, missingUri);
                }
                else
                {
                    DownloadRequest download = CreateDownloadRequest(request.Download, sourceUri, document.Document, context);
                    PtabDossierTaskResult loaded = LoadDossierDownload(outcome.Collection, documentIdentifier, download, context);
                    if (loaded.IsSuccessful && loaded.Value != null)
                    {
                        outcome.Download = (DownloadResult)loaded.Value;
                        if (outcome.Download.State == DownloadState.Completed)
                        {
                            outcome.IsComplete = true;
                            AddSectionSuccess(result, sections, section, document.Document.Provenance);
                        }
                        else
                        {
                            outcome.Error = AddDownloadFailure(result, sections, errors, section, outcome.Download);
                        }
                    }
                    else
                    {
                        outcome.Error = AddSectionFailure(result, sections, errors, section, loaded.Failure);
                    }
                }
            }

            outcomes[index] = outcome;
        }

        return outcomes;
    }

    internal PtabDossierTaskResult LoadDossierDownload(PtabCollection collection, string documentIdentifier, DownloadRequest request, RequestContext context)
    {
        global::System.Threading.Tasks.Task<object> task = global::System.Threading.Tasks.Task.Run(() => (object)Source.DownloadDossierDocument(collection, documentIdentifier, request));
        return AwaitTask(task, context, "The PTAB dossier document download did not complete.");
    }

    internal static PtabDossierTaskResult AwaitTask(global::System.Threading.Tasks.Task<object> task, RequestContext context, string message)
    {
        while (!task.IsCompleted)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            global::System.Threading.Thread.Yield();
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        PtabDossierTaskResult result = new PtabDossierTaskResult();
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
            result.Failure = new ValidationException(message, "section", "SECTION_INCOMPLETE", null);
        }

        return result;
    }

    internal static PtabDossierDocument[] BuildDeduplicatedDocuments(PtabTrialDecisionResponse? decisions, PtabTrialDocumentResponse? documents, bool includeDecisions, bool includeDocuments)
    {
        global::System.Collections.Generic.List<PtabDossierDocument> result = new global::System.Collections.Generic.List<PtabDossierDocument>();
        global::System.Collections.Generic.Dictionary<string, PtabDossierDocument> identifiers = new global::System.Collections.Generic.Dictionary<string, PtabDossierDocument>(global::System.StringComparer.OrdinalIgnoreCase);
        if (includeDecisions && decisions != null)
        {
            int decisionIndex;
            for (decisionIndex = 0; decisionIndex < decisions.Decisions.Length; decisionIndex += 1)
            {
                AddDecisionDocument(result, identifiers, decisions.Decisions[decisionIndex]);
            }
        }

        if (includeDocuments && documents != null)
        {
            int documentIndex;
            for (documentIndex = 0; documentIndex < documents.Documents.Length; documentIndex += 1)
            {
                AddTrialDocument(result, identifiers, documents.Documents[documentIndex]);
            }
        }

        return result.ToArray();
    }

    internal static void AddDecisionDocument(global::System.Collections.Generic.List<PtabDossierDocument> result, global::System.Collections.Generic.Dictionary<string, PtabDossierDocument> identifiers, PtabTrialDecision decision)
    {
        string identifier = decision.Document.DocumentIdentifier ?? string.Empty;
        PtabDossierDocument? item = null;
        if (identifier.Length > 0 && identifiers.ContainsKey(identifier))
        {
            item = identifiers[identifier];
        }

        if (item == null)
        {
            item = new PtabDossierDocument();
            item.Document = decision.Document;
            result.Add(item);
            if (identifier.Length > 0)
            {
                identifiers.Add(identifier, item);
            }
        }

        item.DecisionRecord = decision;
        item.InDecisionCollection = true;
    }

    internal static void AddTrialDocument(global::System.Collections.Generic.List<PtabDossierDocument> result, global::System.Collections.Generic.Dictionary<string, PtabDossierDocument> identifiers, PtabTrialDocument document)
    {
        string identifier = document.Document.DocumentIdentifier ?? string.Empty;
        PtabDossierDocument? item = null;
        if (identifier.Length > 0 && identifiers.ContainsKey(identifier))
        {
            item = identifiers[identifier];
        }

        if (item == null)
        {
            item = new PtabDossierDocument();
            item.Document = document.Document;
            result.Add(item);
            if (identifier.Length > 0)
            {
                identifiers.Add(identifier, item);
            }
        }
        else
        {
            item.Document = document.Document;
        }

        item.TrialDocumentRecord = document;
        item.InDocumentCollection = true;
    }

    internal static PtabDossierDocument? FindDocument(PtabDossierDocument[] documents, string documentIdentifier)
    {
        int index;
        for (index = 0; index < documents.Length; index += 1)
        {
            if (global::System.String.Equals(documents[index].Document.DocumentIdentifier, documentIdentifier, global::System.StringComparison.OrdinalIgnoreCase))
            {
                return documents[index];
            }
        }

        return null;
    }

    internal static global::System.Uri? FindDownloadUri(PtabDossierDocument document)
    {
        if (document.Document.DownloadUri != null)
        {
            return document.Document.DownloadUri;
        }

        if (document.TrialDocumentRecord != null && document.TrialDocumentRecord.Document.DownloadUri != null)
        {
            return document.TrialDocumentRecord.Document.DownloadUri;
        }

        if (document.DecisionRecord != null)
        {
            return document.DecisionRecord.Document.DownloadUri;
        }

        return null;
    }

    internal static PtabCollection SelectDownloadCollection(PtabDossierDocument document)
    {
        if (document.InDocumentCollection)
        {
            return PtabCollection.TrialDocuments;
        }

        return PtabCollection.TrialDecisions;
    }

    internal static DownloadRequest CreateDownloadRequest(DownloadRequest source, global::System.Uri sourceUri, PtabDocument document, RequestContext context)
    {
        DownloadRequest result = new DownloadRequest();
        result.ExportFormat = source.ExportFormat;
        result.SourceUri = sourceUri;
        result.Output = source.Output;
        result.DestinationPath = source.DestinationPath;
        result.OverwriteExisting = source.OverwriteExisting;
        result.ResumePartial = source.ResumePartial;
        result.ExpectedSize = source.ExpectedSize;
        result.HasExpectedSize = source.HasExpectedSize;
        if (!result.HasExpectedSize && document.HasSize)
        {
            result.ExpectedSize = document.Size;
            result.HasExpectedSize = true;
        }

        result.ExpectedChecksums = source.ExpectedChecksums;
        result.Progress = source.Progress;
        result.Context = context;
        return result;
    }

    internal static void AddSectionSuccess(PtabDossier result, global::System.Collections.Generic.List<PtabDossierSectionResult> sections, string section, PtabDataProvenance provenance)
    {
        PtabDossierSectionResult item = new PtabDossierSectionResult();
        item.Section = section;
        item.IsComplete = true;
        item.Provenance = OdpPtabProceedingParser.CopyProvenance(provenance);
        sections.Add(item);
        AddPtabFieldSources(result.Provenance, section, item.Provenance);
        result.RequestedSectionCount += 1;
        result.CompletedSectionCount += 1;
    }

    internal static SectionError AddSectionFailure(PtabDossier result, global::System.Collections.Generic.List<PtabDossierSectionResult> sections, global::System.Collections.Generic.List<SectionError> errors, string section, global::System.Exception? failure)
    {
        global::System.Exception actual = failure ?? new ValidationException("The PTAB dossier section failed.", "section", "SECTION_FAILED", null);
        SectionError error = CreateSectionError(section, actual);
        PtabDossierSectionResult item = new PtabDossierSectionResult();
        item.Section = section;
        item.Error = error;
        sections.Add(item);
        errors.Add(error);
        result.RequestedSectionCount += 1;
        result.FailedSectionCount += 1;
        return error;
    }

    internal static SectionError AddDownloadFailure(PtabDossier result, global::System.Collections.Generic.List<PtabDossierSectionResult> sections, global::System.Collections.Generic.List<SectionError> errors, string section, DownloadResult download)
    {
        SectionError error = new SectionError();
        error.Section = section;
        error.ExceptionType = "DownloadException";
        error.Message = download.FailureMessage ?? "The PTAB dossier document download failed.";
        error.ErrorCode = download.FailureCode ?? "DOWNLOAD_FAILED";
        PtabDossierSectionResult item = new PtabDossierSectionResult();
        item.Section = section;
        item.Error = error;
        sections.Add(item);
        errors.Add(error);
        result.RequestedSectionCount += 1;
        result.FailedSectionCount += 1;
        return error;
    }

    internal static SectionError CreateSectionError(string section, global::System.Exception failure)
    {
        SectionError result = new SectionError();
        result.Section = section;
        result.ExceptionType = failure.GetType().Name;
        result.Message = "The PTAB dossier section failed.";
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

    internal static PtabDossier CreateDossier(string trialNumber, PtabDossierSelection selection, RequestContext context)
    {
        PtabDossier result = new PtabDossier();
        result.TrialNumber = trialNumber;
        result.Selection = CopySelection(selection);
        result.Provenance.Source = DataProvenance.ClientDerived;
        result.Provenance.RequestIdentifier = context.CorrelationIdentifier;
        result.Provenance.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.Provenance.HasRetrievedAt = true;
        result.Provenance.IsAuthoritative = false;
        return result;
    }

    internal static void AddPtabFieldSources(PtabDataProvenance target, string section, PtabDataProvenance source)
    {
        if (global::System.String.Equals(section, "proceeding", global::System.StringComparison.Ordinal))
        {
            ProvenanceLabeler.AddPtabField(target, "Proceeding", source);
        }
        else if (global::System.String.Equals(section, "decisions", global::System.StringComparison.Ordinal))
        {
            ProvenanceLabeler.AddPtabField(target, "Decisions", source);
            ProvenanceLabeler.AddPtabField(target, "Documents", source);
        }
        else if (global::System.String.Equals(section, "documents", global::System.StringComparison.Ordinal))
        {
            ProvenanceLabeler.AddPtabField(target, "TrialDocuments", source);
            ProvenanceLabeler.AddPtabField(target, "Documents", source);
        }
        else
        {
            ProvenanceLabeler.AddPtabField(target, "Downloads", source);
        }
    }

    internal static PtabDossierSelection CopySelection(PtabDossierSelection source)
    {
        PtabDossierSelection result = new PtabDossierSelection();
        result.IncludeProceeding = source.IncludeProceeding;
        result.IncludeDecisions = source.IncludeDecisions;
        result.IncludeDocuments = source.IncludeDocuments;
        result.Downloads = new PtabDossierDownloadRequest[source.Downloads.Length];
        int index;
        for (index = 0; index < source.Downloads.Length; index += 1)
        {
            PtabDossierDownloadRequest item = new PtabDossierDownloadRequest();
            item.DocumentIdentifier = source.Downloads[index].DocumentIdentifier;
            item.Download = CopySelectedDownload(source.Downloads[index].Download);
            result.Downloads[index] = item;
        }

        result.MaximumDownloads = source.MaximumDownloads;
        return result;
    }

    internal static DownloadRequest CopySelectedDownload(DownloadRequest source)
    {
        DownloadRequest result = new DownloadRequest();
        result.ExportFormat = source.ExportFormat;
        result.SourceUri = source.SourceUri;
        result.Output = source.Output;
        result.DestinationPath = source.DestinationPath;
        result.OverwriteExisting = source.OverwriteExisting;
        result.ResumePartial = source.ResumePartial;
        result.ExpectedSize = source.ExpectedSize;
        result.HasExpectedSize = source.HasExpectedSize;
        result.ExpectedChecksums = source.ExpectedChecksums;
        result.Progress = source.Progress;
        result.Context = source.Context;
        return result;
    }

    internal static void ValidateRequest(string trialNumber, PtabDossierSelection selection, RequestContext context)
    {
        if (global::System.String.IsNullOrWhiteSpace(trialNumber))
        {
            throw new ValidationException("A PTAB dossier trial number is required.", "trialNumber", "REQUIRED", null);
        }

        if (selection == null)
        {
            throw new ValidationException("A PTAB dossier selection is required.", "selection", "REQUIRED", null);
        }

        if (context == null)
        {
            throw new ValidationException("A PTAB dossier request context is required.", "context", "REQUIRED", null);
        }

        if (selection.Downloads == null)
        {
            throw new ValidationException("The PTAB dossier download selection is required.", "Downloads", "REQUIRED", null);
        }

        if (selection.MaximumDownloads < 0 || selection.MaximumDownloads > UsptoDefaults.MaximumResults)
        {
            throw new ValidationException("The PTAB dossier maximum download count is invalid.", "MaximumDownloads", "INVALID_LIMIT", null);
        }

        if (selection.Downloads.Length > selection.MaximumDownloads)
        {
            throw new ValidationException("The PTAB dossier download count exceeds the configured maximum.", "Downloads", "DOWNLOAD_LIMIT_EXCEEDED", null);
        }

        if (!selection.IncludeProceeding && !selection.IncludeDecisions && !selection.IncludeDocuments && selection.Downloads.Length == 0)
        {
            throw new ValidationException("At least one PTAB dossier section or download must be selected.", "selection", "SECTION_REQUIRED", null);
        }

        global::System.Collections.Generic.HashSet<string> identifiers = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.OrdinalIgnoreCase);
        int index;
        for (index = 0; index < selection.Downloads.Length; index += 1)
        {
            PtabDossierDownloadRequest request = selection.Downloads[index];
            if (request == null)
            {
                throw new ValidationException("A PTAB dossier download request is required.", "Downloads", "REQUIRED", null);
            }

            if (global::System.String.IsNullOrWhiteSpace(request.DocumentIdentifier))
            {
                throw new ValidationException("A PTAB dossier document identifier is required.", "DocumentIdentifier", "REQUIRED", null);
            }

            if (request.Download == null)
            {
                throw new ValidationException("A PTAB dossier document download request is required.", "Download", "REQUIRED", null);
            }

            if (!identifiers.Add(request.DocumentIdentifier ?? string.Empty))
            {
                throw new ValidationException("A PTAB dossier document can be downloaded only once per request.", "DocumentIdentifier", "DUPLICATE_DOWNLOAD", null);
            }
        }
    }
}
