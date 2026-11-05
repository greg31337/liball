namespace uspto;

internal interface IProsecutionTimelineSource
{
    string ResolveTimelineIdentifier(string identifier, RequestContext context);

    OfficeActionTextResponse GetTimelineOfficeActions(string applicationNumber, RequestContext context);

    PatentTransactionHistory GetTimelineTransactions(string applicationNumber, RequestContext context);

    PatentDocumentList GetTimelineDocuments(string applicationNumber, RequestContext context);
}

internal class ProsecutionTimelineSource : IProsecutionTimelineSource
{
    internal PatentApplicationRetrievalOperation Retrieval;
    internal OfficeActionTextOperation OfficeActions;

    internal ProsecutionTimelineSource(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        if (client == null)
        {
            throw new ValidationException("An HTTP client is required.", "client", "REQUIRED", null);
        }

        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        Retrieval = new PatentApplicationRetrievalOperation(client, options);
        OfficeActions = new OfficeActionTextOperation(client, options);
    }

    public string ResolveTimelineIdentifier(string identifier, RequestContext context)
    {
        return Retrieval.ResolveIdentifier(identifier, context);
    }

    public OfficeActionTextResponse GetTimelineOfficeActions(string applicationNumber, RequestContext context)
    {
        SearchRequest request = new SearchRequest();
        request.Query = OfficeActionQueryWriter.WriteApplicationCriteria(applicationNumber);
        request.Context = context;
        return OfficeActions.SearchAll(request);
    }

    public PatentTransactionHistory GetTimelineTransactions(string applicationNumber, RequestContext context)
    {
        return Retrieval.GetNormalizedTransactions(applicationNumber, context);
    }

    public PatentDocumentList GetTimelineDocuments(string applicationNumber, RequestContext context)
    {
        return Retrieval.GetNormalizedDocuments(applicationNumber, context);
    }
}

internal class ProsecutionTimelineDateValue
{
    internal global::System.DateTimeOffset Value = default(global::System.DateTimeOffset);
    internal bool HasValue = false;
}

internal class ProsecutionTimelineMatchResult
{
    internal int Index = -1;
    internal ProsecutionMatchConfidence Confidence = ProsecutionMatchConfidence.None;
    internal string? Basis = null;
}

internal class ProsecutionTimelineLoadResult
{
    internal object? Value = null;
    internal global::System.Exception? Failure = null;
    internal bool IsSuccessful = false;
}

internal class ProsecutionTimelineBuilder
{
    internal const int OfficeActionSection = 1;
    internal const int TransactionSection = 2;
    internal const int DocumentSection = 3;
    internal const string OfficeActionCategory = "officeAction";
    internal const string TransactionCategory = "transaction";
    internal const string DocumentCategory = "document";

    internal IProsecutionTimelineSource Source;

    internal ProsecutionTimelineBuilder(IProsecutionTimelineSource source)
    {
        if (source == null)
        {
            throw new ValidationException("A prosecution timeline source is required.", "source", "REQUIRED", null);
        }

        Source = source;
    }

    internal ProsecutionTimeline Build(string identifier, RequestContext context)
    {
        if (global::System.String.IsNullOrWhiteSpace(identifier))
        {
            throw new ValidationException("A prosecution timeline application identifier is required.", "identifier", "REQUIRED", null);
        }

        if (context == null)
        {
            throw new ValidationException("A prosecution timeline request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        string applicationNumber = Source.ResolveTimelineIdentifier(identifier, context);
        ProsecutionTimeline result = CreateTimeline(applicationNumber, context);
        global::System.Collections.Generic.List<SectionError> errors = new global::System.Collections.Generic.List<SectionError>();

        ProsecutionTimelineLoadResult officeActionLoad = LoadTimelineSection(OfficeActionSection, applicationNumber, context);
        ProsecutionTimelineLoadResult transactionLoad = LoadTimelineSection(TransactionSection, applicationNumber, context);
        ProsecutionTimelineLoadResult documentLoad = LoadTimelineSection(DocumentSection, applicationNumber, context);

        OfficeActionTextResponse officeActions = new OfficeActionTextResponse();
        PatentTransactionHistory transactions = new PatentTransactionHistory();
        PatentDocumentList documents = new PatentDocumentList();
        if (officeActionLoad.IsSuccessful)
        {
            officeActions = (OfficeActionTextResponse)(officeActionLoad.Value ?? new OfficeActionTextResponse());
            result.OfficeActionProvenance = OdpJsonParser.CopyProvenance(officeActions.Provenance);
            ProvenanceLabeler.AddPatentField(result.Provenance, "Events.OfficeAction", result.OfficeActionProvenance);
            result.LoadedSectionCount += 1;
        }
        else
        {
            AddTimelineFailure(result, errors, OfficeActionSection, officeActionLoad.Failure);
        }

        if (transactionLoad.IsSuccessful)
        {
            transactions = (PatentTransactionHistory)(transactionLoad.Value ?? new PatentTransactionHistory());
            result.TransactionProvenance = OdpJsonParser.CopyProvenance(transactions.Provenance);
            ProvenanceLabeler.AddPatentField(result.Provenance, "Events.Transaction", result.TransactionProvenance);
            result.LoadedSectionCount += 1;
        }
        else
        {
            AddTimelineFailure(result, errors, TransactionSection, transactionLoad.Failure);
        }

        if (documentLoad.IsSuccessful)
        {
            documents = (PatentDocumentList)(documentLoad.Value ?? new PatentDocumentList());
            result.DocumentProvenance = OdpJsonParser.CopyProvenance(documents.Provenance);
            ProvenanceLabeler.AddPatentField(result.Provenance, "Events.Document", result.DocumentProvenance);
            result.LoadedSectionCount += 1;
        }
        else
        {
            AddTimelineFailure(result, errors, DocumentSection, documentLoad.Failure);
        }

        result.Events = JoinTimelineRecords(officeActions, transactions, documents, result);
        result.Count = result.Events.Length;
        result.Errors = errors.ToArray();
        result.IsComplete = result.FailedSectionCount == 0 && officeActions.IsComplete;
        result.HasPartialSuccess = result.Count > 0 && !result.IsComplete;
        return result;
    }

    internal ProsecutionTimelineLoadResult LoadTimelineSection(int section, string applicationNumber, RequestContext context)
    {
        global::System.Threading.Tasks.Task<object> task = global::System.Threading.Tasks.Task.Run(() => ExecuteTimelineSection(section, applicationNumber, context));
        while (!task.IsCompleted)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            global::System.Threading.Thread.Yield();
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        ProsecutionTimelineLoadResult result = new ProsecutionTimelineLoadResult();
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
            result.Failure = new ValidationException("The prosecution timeline source did not complete.", "section", "SECTION_INCOMPLETE", null);
        }

        return result;
    }

    internal object ExecuteTimelineSection(int section, string applicationNumber, RequestContext context)
    {
        switch (section)
        {
            case OfficeActionSection:
                return Source.GetTimelineOfficeActions(applicationNumber, context);
            case TransactionSection:
                return Source.GetTimelineTransactions(applicationNumber, context);
            case DocumentSection:
                return Source.GetTimelineDocuments(applicationNumber, context);
            default:
                throw new ValidationException("The prosecution timeline section is not supported.", "section", "UNSUPPORTED_SECTION", null);
        }
    }

    internal static ProsecutionTimelineEvent[] JoinTimelineRecords(OfficeActionTextResponse officeActions, PatentTransactionHistory transactions, PatentDocumentList documents, ProsecutionTimeline result)
    {
        bool[] usedTransactions = new bool[transactions.Events.Length];
        bool[] usedDocuments = new bool[documents.Documents.Length];
        global::System.Collections.Generic.List<ProsecutionTimelineEvent> events = new global::System.Collections.Generic.List<ProsecutionTimelineEvent>();
        int index;
        for (index = 0; index < officeActions.Records.Length; index += 1)
        {
            OfficeActionTextRecord officeAction = officeActions.Records[index];
            ProsecutionTimelineMatchResult transactionMatch = FindTransactionMatch(officeAction, transactions.Events, usedTransactions);
            ProsecutionTimelineMatchResult documentMatch = FindDocumentMatch(officeAction, documents.Documents, usedDocuments);
            if (transactionMatch.Index >= 0)
            {
                usedTransactions[transactionMatch.Index] = true;
            }

            if (documentMatch.Index >= 0)
            {
                usedDocuments[documentMatch.Index] = true;
            }

            ProsecutionTimelineEvent item = CreateOfficeActionEvent(officeAction, transactionMatch, documentMatch, transactions, documents, result.Provenance);
            events.Add(item);
            if (item.IsDerivedMatch)
            {
                result.MatchedOfficeActionCount += 1;
            }
            else
            {
                result.UnmatchedOfficeActionCount += 1;
            }
        }

        for (index = 0; index < transactions.Events.Length; index += 1)
        {
            if (!usedTransactions[index])
            {
                events.Add(CreateTransactionEvent(transactions.Events[index], transactions.Provenance));
                result.UnmatchedTransactionCount += 1;
            }
        }

        for (index = 0; index < documents.Documents.Length; index += 1)
        {
            if (!usedDocuments[index])
            {
                events.Add(CreateDocumentEvent(documents.Documents[index], documents.Provenance));
                result.UnmatchedDocumentCount += 1;
            }
        }

        events.Sort((left, right) => CompareTimelineEvents(left, right));
        return events.ToArray();
    }

    internal static ProsecutionTimelineMatchResult FindTransactionMatch(OfficeActionTextRecord officeAction, PatentTransaction[] transactions, bool[] usedTransactions)
    {
        ProsecutionTimelineMatchResult best = new ProsecutionTimelineMatchResult();
        int index;
        for (index = 0; index < transactions.Length; index += 1)
        {
            if (usedTransactions[index])
            {
                continue;
            }

            PatentTransaction transaction = transactions[index];
            bool codeMatch = ContainsCode(officeAction.LegacyDocumentCodeIdentifiers, transaction.EventCode);
            bool dateMatch = officeAction.HasSubmissionDate && transaction.HasEventDate && DatesMatch(officeAction.SubmissionDate, transaction.EventDate);
            ProsecutionMatchConfidence confidence = ProsecutionMatchConfidence.None;
            string? basis = null;
            if (codeMatch && dateMatch)
            {
                confidence = ProsecutionMatchConfidence.High;
                basis = "dateAndCode";
            }
            else if (dateMatch)
            {
                confidence = ProsecutionMatchConfidence.Medium;
                basis = "date";
            }
            else if (codeMatch)
            {
                confidence = ProsecutionMatchConfidence.Low;
                basis = "code";
            }

            if ((int)confidence > (int)best.Confidence || (confidence == best.Confidence && confidence != ProsecutionMatchConfidence.None && CompareTransactions(transaction, transactions[best.Index]) < 0))
            {
                best.Index = index;
                best.Confidence = confidence;
                best.Basis = basis;
            }
        }

        return best;
    }

    internal static ProsecutionTimelineMatchResult FindDocumentMatch(OfficeActionTextRecord officeAction, PatentDocument[] documents, bool[] usedDocuments)
    {
        ProsecutionTimelineMatchResult best = new ProsecutionTimelineMatchResult();
        int index;
        for (index = 0; index < documents.Length; index += 1)
        {
            if (usedDocuments[index])
            {
                continue;
            }

            PatentDocument document = documents[index];
            bool identifierMatch = OfficeActionMatchesDocumentIdentifier(officeAction, document.DocumentIdentifier);
            bool codeMatch = ContainsCode(officeAction.LegacyDocumentCodeIdentifiers, document.Code);
            ProsecutionTimelineDateValue documentDate = ReadDocumentDate(document);
            bool dateMatch = officeAction.HasSubmissionDate && documentDate.HasValue && DatesMatch(officeAction.SubmissionDate, documentDate.Value);
            ProsecutionMatchConfidence confidence = ProsecutionMatchConfidence.None;
            string? basis = null;
            if (identifierMatch)
            {
                confidence = ProsecutionMatchConfidence.Exact;
                basis = "documentIdentifier";
            }
            else if (codeMatch && dateMatch)
            {
                confidence = ProsecutionMatchConfidence.High;
                basis = "dateAndCode";
            }
            else if (dateMatch)
            {
                confidence = ProsecutionMatchConfidence.Medium;
                basis = "date";
            }
            else if (codeMatch)
            {
                confidence = ProsecutionMatchConfidence.Low;
                basis = "code";
            }

            if ((int)confidence > (int)best.Confidence || (confidence == best.Confidence && confidence != ProsecutionMatchConfidence.None && CompareDocuments(document, documents[best.Index]) < 0))
            {
                best.Index = index;
                best.Confidence = confidence;
                best.Basis = basis;
            }
        }

        return best;
    }

    internal static ProsecutionTimelineEvent CreateOfficeActionEvent(OfficeActionTextRecord officeAction, ProsecutionTimelineMatchResult transactionMatch, ProsecutionTimelineMatchResult documentMatch, PatentTransactionHistory transactions, PatentDocumentList documents, PatentDataProvenance derivationProvenance)
    {
        ProsecutionTimelineEvent result = new ProsecutionTimelineEvent();
        result.SourceCategory = OfficeActionCategory;
        result.EventIdentifier = OfficeActionStableIdentifier(officeAction);
        result.OfficeAction = officeAction;
        result.TransactionMatchConfidence = transactionMatch.Confidence;
        result.TransactionMatchBasis = transactionMatch.Basis;
        result.DocumentMatchConfidence = documentMatch.Confidence;
        result.DocumentMatchBasis = documentMatch.Basis;
        result.Provenance = OdpJsonParser.CopyProvenance(officeAction.Provenance);
        if (transactionMatch.Index >= 0)
        {
            result.Transaction = transactions.Events[transactionMatch.Index];
        }

        if (documentMatch.Index >= 0)
        {
            result.Document = documents.Documents[documentMatch.Index];
        }

        result.IsDerivedMatch = result.Transaction != null || result.Document != null;
        if (result.IsDerivedMatch)
        {
            result.DerivationProvenance = OdpJsonParser.CopyProvenance(derivationProvenance);
        }

        ProsecutionTimelineDateValue date = ReadOfficeActionDate(officeAction);
        if (!date.HasValue && result.Transaction != null)
        {
            date = ReadTransactionDate(result.Transaction);
        }

        if (!date.HasValue && result.Document != null)
        {
            date = ReadDocumentDate(result.Document);
        }

        ApplyTimelineDate(result, date);
        return result;
    }

    internal static ProsecutionTimelineEvent CreateTransactionEvent(PatentTransaction transaction, PatentDataProvenance provenance)
    {
        ProsecutionTimelineEvent result = new ProsecutionTimelineEvent();
        result.SourceCategory = TransactionCategory;
        result.EventIdentifier = TransactionStableIdentifier(transaction);
        result.Transaction = transaction;
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        ApplyTimelineDate(result, ReadTransactionDate(transaction));
        return result;
    }

    internal static ProsecutionTimelineEvent CreateDocumentEvent(PatentDocument document, PatentDataProvenance provenance)
    {
        ProsecutionTimelineEvent result = new ProsecutionTimelineEvent();
        result.SourceCategory = DocumentCategory;
        result.EventIdentifier = DocumentStableIdentifier(document);
        result.Document = document;
        result.Provenance = OdpJsonParser.CopyProvenance(provenance);
        ApplyTimelineDate(result, ReadDocumentDate(document));
        return result;
    }

    internal static void ApplyTimelineDate(ProsecutionTimelineEvent result, ProsecutionTimelineDateValue date)
    {
        if (date.HasValue)
        {
            result.EventDate = date.Value;
            result.HasEventDate = true;
        }
    }

    internal static ProsecutionTimelineDateValue ReadOfficeActionDate(OfficeActionTextRecord officeAction)
    {
        ProsecutionTimelineDateValue result = new ProsecutionTimelineDateValue();
        if (officeAction.HasSubmissionDate)
        {
            result.Value = officeAction.SubmissionDate;
            result.HasValue = true;
        }

        return result;
    }

    internal static ProsecutionTimelineDateValue ReadTransactionDate(PatentTransaction transaction)
    {
        ProsecutionTimelineDateValue result = new ProsecutionTimelineDateValue();
        if (transaction.HasEventDate)
        {
            result.Value = transaction.EventDate;
            result.HasValue = true;
        }

        return result;
    }

    internal static ProsecutionTimelineDateValue ReadDocumentDate(PatentDocument document)
    {
        ProsecutionTimelineDateValue result = new ProsecutionTimelineDateValue();
        if (document.HasOfficialDate)
        {
            result.Value = document.OfficialDate;
            result.HasValue = true;
        }
        else if (document.HasFiledAt)
        {
            result.Value = document.FiledAt;
            result.HasValue = true;
        }
        else if (document.HasCreatedAt)
        {
            result.Value = document.CreatedAt;
            result.HasValue = true;
        }

        return result;
    }

    internal static bool DatesMatch(global::System.DateTimeOffset left, global::System.DateTimeOffset right)
    {
        global::System.DateTime leftDate = left.UtcDateTime.Date;
        global::System.DateTime rightDate = right.UtcDateTime.Date;
        return leftDate == rightDate;
    }

    internal static bool OfficeActionMatchesDocumentIdentifier(OfficeActionTextRecord officeAction, string? documentIdentifier)
    {
        if (global::System.String.IsNullOrWhiteSpace(documentIdentifier))
        {
            return false;
        }

        if (global::System.String.Equals(officeAction.Identifier, documentIdentifier, global::System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return ContainsCode(officeAction.ObsoleteDocumentIdentifiers, documentIdentifier);
    }

    internal static bool ContainsCode(string[] values, string? expected)
    {
        if (global::System.String.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        int index;
        for (index = 0; index < values.Length; index += 1)
        {
            if (global::System.String.Equals(values[index], expected, global::System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static int CompareTimelineEvents(ProsecutionTimelineEvent left, ProsecutionTimelineEvent right)
    {
        if (left.HasEventDate && !right.HasEventDate)
        {
            return -1;
        }

        if (!left.HasEventDate && right.HasEventDate)
        {
            return 1;
        }

        if (left.HasEventDate && right.HasEventDate)
        {
            int dateComparison = global::System.DateTimeOffset.Compare(left.EventDate, right.EventDate);
            if (dateComparison != 0)
            {
                return dateComparison;
            }
        }

        int categoryComparison = SourceCategoryRank(left.SourceCategory) - SourceCategoryRank(right.SourceCategory);
        if (categoryComparison != 0)
        {
            return categoryComparison;
        }

        return global::System.StringComparer.Ordinal.Compare(left.EventIdentifier ?? string.Empty, right.EventIdentifier ?? string.Empty);
    }

    internal static int SourceCategoryRank(string? category)
    {
        if (category == OfficeActionCategory)
        {
            return 0;
        }

        if (category == TransactionCategory)
        {
            return 1;
        }

        return 2;
    }

    internal static int CompareTransactions(PatentTransaction left, PatentTransaction right)
    {
        string leftIdentifier = TransactionStableIdentifier(left);
        string rightIdentifier = TransactionStableIdentifier(right);
        return global::System.StringComparer.Ordinal.Compare(leftIdentifier, rightIdentifier);
    }

    internal static int CompareDocuments(PatentDocument left, PatentDocument right)
    {
        return global::System.StringComparer.Ordinal.Compare(DocumentStableIdentifier(left), DocumentStableIdentifier(right));
    }

    internal static string OfficeActionStableIdentifier(OfficeActionTextRecord officeAction)
    {
        if (!global::System.String.IsNullOrWhiteSpace(officeAction.Identifier))
        {
            return officeAction.Identifier ?? string.Empty;
        }

        if (officeAction.ObsoleteDocumentIdentifiers.Length != 0)
        {
            return officeAction.ObsoleteDocumentIdentifiers[0];
        }

        return string.Empty;
    }

    internal static string TransactionStableIdentifier(PatentTransaction transaction)
    {
        string code = transaction.EventCode ?? string.Empty;
        if (transaction.HasEventSequenceNumber)
        {
            return code + ":" + transaction.EventSequenceNumber.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        }

        return code;
    }

    internal static string DocumentStableIdentifier(PatentDocument document)
    {
        if (!global::System.String.IsNullOrWhiteSpace(document.DocumentIdentifier))
        {
            return document.DocumentIdentifier ?? string.Empty;
        }

        return document.Code ?? string.Empty;
    }

    internal static void AddTimelineFailure(ProsecutionTimeline result, global::System.Collections.Generic.List<SectionError> errors, int section, global::System.Exception? failure)
    {
        global::System.Exception actual = failure ?? new ValidationException("The prosecution timeline source failed.", "section", "SECTION_FAILED", null);
        int dossierSection = DossierBuilder.ApplicationSection;
        if (section == TransactionSection)
        {
            dossierSection = DossierBuilder.TransactionsSection;
        }
        else if (section == DocumentSection)
        {
            dossierSection = DossierBuilder.DocumentsSection;
        }

        SectionError error = DossierBuilder.CreateSectionError(dossierSection, actual);
        error.Section = TimelineSectionName(section);
        errors.Add(error);
        result.FailedSectionCount += 1;
    }

    internal static string TimelineSectionName(int section)
    {
        switch (section)
        {
            case OfficeActionSection:
                return "officeActions";
            case TransactionSection:
                return "transactions";
            case DocumentSection:
                return "documents";
            default:
                return "unknown";
        }
    }

    internal static ProsecutionTimeline CreateTimeline(string applicationNumber, RequestContext context)
    {
        ProsecutionTimeline result = new ProsecutionTimeline();
        result.ApplicationNumber = applicationNumber;
        result.Provenance.Source = DataProvenance.ClientDerived;
        result.Provenance.RequestIdentifier = context.CorrelationIdentifier;
        result.Provenance.RetrievedAt = global::System.DateTimeOffset.UtcNow;
        result.Provenance.HasRetrievedAt = true;
        result.Provenance.IsAuthoritative = false;
        return result;
    }
}
