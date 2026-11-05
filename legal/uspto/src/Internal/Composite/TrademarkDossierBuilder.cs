namespace uspto;

internal interface ITrademarkDossierSource
{
    TrademarkCaseStatus GetTrademarkDossierStatus(string identifier, DocumentFormat format, RequestContext context);

    TrademarkDocumentCollection GetTrademarkDossierDocuments(string identifier, RequestContext context);

    DownloadResult GetTrademarkDossierContent(string identifier, string documentIdentifier, DocumentFormat format, DownloadRequest request);
}

internal class TrademarkDossierSource : ITrademarkDossierSource
{
    internal TrademarkStatusOperation Status;
    internal TrademarkDocumentOperation Documents;

    internal TrademarkDossierSource(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        if (client == null)
        {
            throw new ValidationException("An HTTP client is required.", "client", "REQUIRED", null);
        }

        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        Status = new TrademarkStatusOperation(client, options);
        Documents = new TrademarkDocumentOperation(client, options);
    }

    public TrademarkCaseStatus GetTrademarkDossierStatus(string identifier, DocumentFormat format, RequestContext context)
    {
        return Status.GetStatus(identifier, format, context);
    }

    public TrademarkDocumentCollection GetTrademarkDossierDocuments(string identifier, RequestContext context)
    {
        return Documents.GetDocuments(identifier, context);
    }

    public DownloadResult GetTrademarkDossierContent(string identifier, string documentIdentifier, DocumentFormat format, DownloadRequest request)
    {
        return Documents.GetContent(identifier, documentIdentifier, format, request);
    }
}

internal class TrademarkDossierTaskResult
{
    internal object? Value = null;
    internal global::System.Exception? Failure = null;
    internal bool IsSuccessful = false;
}

internal class TrademarkDossierBuilder
{
    internal const int StatusSection = 1;
    internal const int DocumentsSection = 2;

    internal ITrademarkDossierSource Source;

    internal TrademarkDossierBuilder(ITrademarkDossierSource source)
    {
        if (source == null)
        {
            throw new ValidationException("A trademark dossier source is required.", "source", "REQUIRED", null);
        }

        Source = source;
    }

    internal TrademarkDossier Build(string identifier, TrademarkDossierSelection selection, RequestContext context)
    {
        ValidateRequest(identifier, selection, context);
        context.CancellationToken.ThrowIfCancellationRequested();
        TrademarkIdentifierResult parsed = TrademarkIdentifierParser.ParseTrademarkIdentifier(identifier);
        TrademarkDossier result = CreateDossier(identifier, parsed, selection, context);
        global::System.Collections.Generic.List<TrademarkDossierSectionResult> sections = new global::System.Collections.Generic.List<TrademarkDossierSectionResult>();
        global::System.Collections.Generic.List<SectionError> errors = new global::System.Collections.Generic.List<SectionError>();

        if (selection.IncludeStatus)
        {
            TrademarkDossierTaskResult statusLoad = LoadDossierSection(StatusSection, identifier, selection.StatusFormat, context);
            if (statusLoad.IsSuccessful && statusLoad.Value != null)
            {
                TrademarkCaseStatus status = (TrademarkCaseStatus)statusLoad.Value;
                ApplyStatus(result, status);
                AddSectionSuccess(result, sections, "status", status.Provenance);
            }
            else
            {
                AddSectionFailure(result, sections, errors, "status", statusLoad.Failure);
            }
        }

        bool loadDocuments = selection.IncludeDocuments || selection.SelectedContent.Length > 0;
        TrademarkDocumentCollection? documentCollection = null;
        if (loadDocuments)
        {
            TrademarkDossierTaskResult documentLoad = LoadDossierSection(DocumentsSection, identifier, DocumentFormat.Xml, context);
            if (documentLoad.IsSuccessful && documentLoad.Value != null)
            {
                documentCollection = (TrademarkDocumentCollection)documentLoad.Value;
                if (selection.IncludeDocuments)
                {
                    result.DocumentCollection = documentCollection;
                    result.Documents = documentCollection.Documents;
                    AddSectionSuccess(result, sections, "documents", documentCollection.Provenance);
                }
            }
            else if (selection.IncludeDocuments)
            {
                AddSectionFailure(result, sections, errors, "documents", documentLoad.Failure);
            }
        }

        result.SelectedContent = LoadSelectedContent(result, sections, errors, identifier, documentCollection, selection.SelectedContent, context);
        result.Sections = sections.ToArray();
        result.Errors = errors.ToArray();
        result.IsComplete = result.CompletedSectionCount == result.RequestedSectionCount;
        result.HasPartialSuccess = result.CompletedSectionCount > 0 && result.FailedSectionCount > 0;
        return result;
    }

    internal TrademarkDossierTaskResult LoadDossierSection(int section, string identifier, DocumentFormat format, RequestContext context)
    {
        global::System.Threading.Tasks.Task<object> task = global::System.Threading.Tasks.Task.Run(() => ExecuteDossierSection(section, identifier, format, context));
        return AwaitTask(task, context, "The trademark dossier section did not complete.");
    }

    internal object ExecuteDossierSection(int section, string identifier, DocumentFormat format, RequestContext context)
    {
        switch (section)
        {
            case StatusSection:
                return Source.GetTrademarkDossierStatus(identifier, format, context);
            case DocumentsSection:
                return Source.GetTrademarkDossierDocuments(identifier, context);
            default:
                throw new ValidationException("The trademark dossier section is not supported.", "section", "UNSUPPORTED_SECTION", null);
        }
    }

    internal TrademarkDossierContentResult[] LoadSelectedContent(TrademarkDossier result, global::System.Collections.Generic.List<TrademarkDossierSectionResult> sections, global::System.Collections.Generic.List<SectionError> errors, string identifier, TrademarkDocumentCollection? documents, TrademarkDossierContentRequest[] requests, RequestContext context)
    {
        TrademarkDossierContentResult[] outcomes = new TrademarkDossierContentResult[requests.Length];
        int index;
        for (index = 0; index < requests.Length; index += 1)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            TrademarkDossierContentRequest request = requests[index];
            string documentIdentifier = (request.DocumentIdentifier ?? string.Empty).Trim();
            string section = "content:" + documentIdentifier + ":" + FormatName(request.Format);
            TrademarkDossierContentResult outcome = new TrademarkDossierContentResult();
            outcome.DocumentIdentifier = documentIdentifier;
            outcome.Format = request.Format;
            outcome.Document = FindDocument(documents, documentIdentifier);
            if (documents != null && outcome.Document == null)
            {
                ValidationException missing = new ValidationException("The requested trademark dossier document was not found in the case document listing.", "DocumentIdentifier", "DOCUMENT_NOT_FOUND", null);
                outcome.Error = AddSectionFailure(result, sections, errors, section, missing);
            }
            else
            {
                DownloadRequest downloadRequest = CopyContentDownload(request.Download, context);
                TrademarkDossierTaskResult loaded = LoadDossierContent(identifier, documentIdentifier, request.Format, downloadRequest, context);
                if (loaded.IsSuccessful && loaded.Value != null)
                {
                    outcome.Download = (DownloadResult)loaded.Value;
                    if (outcome.Download.State == DownloadState.Completed)
                    {
                        outcome.IsComplete = true;
                        AddSectionSuccess(result, sections, section, ContentProvenance(outcome, context));
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

            outcomes[index] = outcome;
        }

        return outcomes;
    }

    internal TrademarkDossierTaskResult LoadDossierContent(string identifier, string documentIdentifier, DocumentFormat format, DownloadRequest request, RequestContext context)
    {
        global::System.Threading.Tasks.Task<object> task = global::System.Threading.Tasks.Task.Run(() => (object)Source.GetTrademarkDossierContent(identifier, documentIdentifier, format, request));
        return AwaitTask(task, context, "The trademark dossier content transfer did not complete.");
    }

    internal static TrademarkDossierTaskResult AwaitTask(global::System.Threading.Tasks.Task<object> task, RequestContext context, string message)
    {
        while (!task.IsCompleted)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            global::System.Threading.Thread.Yield();
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        TrademarkDossierTaskResult result = new TrademarkDossierTaskResult();
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

    internal static void ApplyStatus(TrademarkDossier result, TrademarkCaseStatus source)
    {
        result.CaseStatus = source;
        result.Status = source.Status;
        result.Mark = source.Mark;
        result.Events = source.ProsecutionEvents;
        TrademarkDossierParties parties = new TrademarkDossierParties();
        parties.CurrentOwners = source.CurrentOwners;
        parties.PriorOwners = source.PriorOwners;
        parties.Attorneys = source.Attorneys;
        parties.Correspondence = source.Correspondence;
        result.Parties = parties;
        result.GoodsAndServices = source.GoodsAndServices;
        if (!global::System.String.IsNullOrWhiteSpace(source.CanonicalIdentifier))
        {
            result.CanonicalIdentifier = source.CanonicalIdentifier;
            result.IdentifierKind = source.IdentifierKind;
        }
    }

    internal static TrademarkDocument? FindDocument(TrademarkDocumentCollection? collection, string documentIdentifier)
    {
        if (collection == null)
        {
            return null;
        }

        int index;
        for (index = 0; index < collection.Documents.Length; index += 1)
        {
            if (global::System.String.Equals(collection.Documents[index].DocumentIdentifier, documentIdentifier, global::System.StringComparison.OrdinalIgnoreCase))
            {
                return collection.Documents[index];
            }
        }

        return null;
    }

    internal static PatentDataProvenance ContentProvenance(TrademarkDossierContentResult content, RequestContext context)
    {
        if (content.Document != null)
        {
            return content.Document.Provenance;
        }

        PatentDataProvenance result = new PatentDataProvenance();
        result.Source = DataProvenance.Tsdr;
        result.DatasetIdentifier = "TSDR trademark document content";
        result.RequestIdentifier = context.CorrelationIdentifier;
        result.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.HasRetrievedAt = true;
        result.IsAuthoritative = true;
        if (content.Download != null)
        {
            result.SourceUri = content.Download.Content.FinalUri;
        }

        ProvenanceLabeler.AddPatentField(result, "*", result);
        return result;
    }

    internal static void AddSectionSuccess(TrademarkDossier result, global::System.Collections.Generic.List<TrademarkDossierSectionResult> sections, string section, PatentDataProvenance provenance)
    {
        TrademarkDossierSectionResult item = new TrademarkDossierSectionResult();
        item.Section = section;
        item.IsComplete = true;
        item.Provenance = OdpJsonParser.CopyProvenance(provenance);
        sections.Add(item);
        AddTrademarkFieldSources(result.Provenance, section, item.Provenance);
        result.RequestedSectionCount += 1;
        result.CompletedSectionCount += 1;
    }

    internal static SectionError AddSectionFailure(TrademarkDossier result, global::System.Collections.Generic.List<TrademarkDossierSectionResult> sections, global::System.Collections.Generic.List<SectionError> errors, string section, global::System.Exception? failure)
    {
        global::System.Exception actual = failure ?? new ValidationException("The trademark dossier section failed.", "section", "SECTION_FAILED", null);
        SectionError error = CreateSectionError(section, actual);
        TrademarkDossierSectionResult item = new TrademarkDossierSectionResult();
        item.Section = section;
        item.Error = error;
        sections.Add(item);
        errors.Add(error);
        result.RequestedSectionCount += 1;
        result.FailedSectionCount += 1;
        return error;
    }

    internal static SectionError AddDownloadFailure(TrademarkDossier result, global::System.Collections.Generic.List<TrademarkDossierSectionResult> sections, global::System.Collections.Generic.List<SectionError> errors, string section, DownloadResult download)
    {
        SectionError error = new SectionError();
        error.Section = section;
        error.ExceptionType = "DownloadException";
        error.Message = download.FailureMessage ?? "The trademark dossier document content transfer failed.";
        error.ErrorCode = download.FailureCode ?? "DOWNLOAD_FAILED";
        TrademarkDossierSectionResult item = new TrademarkDossierSectionResult();
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
        result.Message = "The trademark dossier section failed.";
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

    internal static TrademarkDossier CreateDossier(string identifier, TrademarkIdentifierResult parsed, TrademarkDossierSelection selection, RequestContext context)
    {
        TrademarkDossier result = new TrademarkDossier();
        result.RequestedIdentifier = identifier;
        result.CanonicalIdentifier = parsed.CanonicalNumber;
        result.IdentifierKind = parsed.Kind;
        result.Selection = CopySelection(selection);
        result.Provenance.Source = DataProvenance.ClientDerived;
        result.Provenance.DatasetIdentifier = "TSDR composite trademark dossier";
        result.Provenance.RequestIdentifier = context.CorrelationIdentifier;
        result.Provenance.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.Provenance.HasRetrievedAt = true;
        result.Provenance.IsAuthoritative = false;
        return result;
    }

    internal static void AddTrademarkFieldSources(PatentDataProvenance target, string section, PatentDataProvenance source)
    {
        if (global::System.String.Equals(section, "status", global::System.StringComparison.Ordinal))
        {
            ProvenanceLabeler.AddPatentField(target, "CaseStatus", source);
            ProvenanceLabeler.AddPatentField(target, "Status", source);
            ProvenanceLabeler.AddPatentField(target, "Mark", source);
            ProvenanceLabeler.AddPatentField(target, "Events", source);
            ProvenanceLabeler.AddPatentField(target, "Parties", source);
            ProvenanceLabeler.AddPatentField(target, "GoodsAndServices", source);
        }
        else if (global::System.String.Equals(section, "documents", global::System.StringComparison.Ordinal))
        {
            ProvenanceLabeler.AddPatentField(target, "DocumentCollection", source);
            ProvenanceLabeler.AddPatentField(target, "Documents", source);
        }
        else
        {
            ProvenanceLabeler.AddPatentField(target, "SelectedContent", source);
        }
    }

    internal static TrademarkDossierSelection CopySelection(TrademarkDossierSelection source)
    {
        TrademarkDossierSelection result = new TrademarkDossierSelection();
        result.IncludeStatus = source.IncludeStatus;
        result.StatusFormat = source.StatusFormat;
        result.IncludeDocuments = source.IncludeDocuments;
        result.SelectedContent = new TrademarkDossierContentRequest[source.SelectedContent.Length];
        int index;
        for (index = 0; index < source.SelectedContent.Length; index += 1)
        {
            TrademarkDossierContentRequest item = new TrademarkDossierContentRequest();
            item.DocumentIdentifier = source.SelectedContent[index].DocumentIdentifier;
            item.Format = source.SelectedContent[index].Format;
            item.Download = CopyContentDownload(source.SelectedContent[index].Download, source.SelectedContent[index].Download.Context);
            result.SelectedContent[index] = item;
        }

        result.MaximumSelectedContent = source.MaximumSelectedContent;
        return result;
    }

    internal static DownloadRequest CopyContentDownload(DownloadRequest source, RequestContext context)
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
        result.Context = context;
        return result;
    }

    internal static string FormatName(DocumentFormat format)
    {
        if (format == DocumentFormat.Pdf)
        {
            return "pdf";
        }

        return "zip";
    }

    internal static void ValidateRequest(string identifier, TrademarkDossierSelection selection, RequestContext context)
    {
        if (selection == null)
        {
            throw new ValidationException("A trademark dossier selection is required.", "selection", "REQUIRED", null);
        }

        if (context == null)
        {
            throw new ValidationException("A trademark dossier request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        TrademarkIdentifierResult parsed = TrademarkIdentifierParser.ParseTrademarkIdentifier(identifier);
        if (!parsed.IsValid)
        {
            throw new ValidationException("The trademark identifier is invalid.", "identifier", parsed.FailureCode, null);
        }

        if (selection.IncludeStatus && selection.StatusFormat != DocumentFormat.Json && selection.StatusFormat != DocumentFormat.Xml)
        {
            throw new ValidationException("Trademark dossier status requires JSON or XML.", "StatusFormat", "UNSUPPORTED_FORMAT", null);
        }

        if (selection.SelectedContent == null)
        {
            throw new ValidationException("The trademark dossier content selection is required.", "SelectedContent", "REQUIRED", null);
        }

        if (selection.MaximumSelectedContent < 0 || selection.MaximumSelectedContent > UsptoDefaults.MaximumResults)
        {
            throw new ValidationException("The trademark dossier maximum selected content count is invalid.", "MaximumSelectedContent", "INVALID_LIMIT", null);
        }

        if (selection.SelectedContent.Length > selection.MaximumSelectedContent)
        {
            throw new ValidationException("The trademark dossier selected content count exceeds the configured maximum.", "SelectedContent", "CONTENT_LIMIT_EXCEEDED", null);
        }

        if (!selection.IncludeStatus && !selection.IncludeDocuments && selection.SelectedContent.Length == 0)
        {
            throw new ValidationException("At least one trademark dossier section or content transfer must be selected.", "selection", "SECTION_REQUIRED", null);
        }

        global::System.Collections.Generic.HashSet<string> contentKeys = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.OrdinalIgnoreCase);
        int index;
        for (index = 0; index < selection.SelectedContent.Length; index += 1)
        {
            TrademarkDossierContentRequest request = selection.SelectedContent[index];
            if (request == null)
            {
                throw new ValidationException("A trademark dossier content request is required.", "SelectedContent", "REQUIRED", null);
            }

            if (global::System.String.IsNullOrWhiteSpace(request.DocumentIdentifier))
            {
                throw new ValidationException("A trademark dossier document identifier is required.", "DocumentIdentifier", "REQUIRED", null);
            }

            if (request.Format != DocumentFormat.Pdf && request.Format != DocumentFormat.Zip)
            {
                throw new ValidationException("Trademark dossier document content requires PDF or ZIP.", "Format", "UNSUPPORTED_DOCUMENT_FORMAT", null);
            }

            if (request.Download == null)
            {
                throw new ValidationException("A trademark dossier content download request is required.", "Download", "REQUIRED", null);
            }

            string contentKey = request.DocumentIdentifier.Trim() + "|" + ((int)request.Format).ToString(global::System.Globalization.CultureInfo.InvariantCulture);
            if (!contentKeys.Add(contentKey))
            {
                throw new ValidationException("A trademark dossier document representation can be selected only once per request.", "SelectedContent", "DUPLICATE_CONTENT", null);
            }
        }
    }
}
