namespace uspto;

internal class PatentApplicationSearchOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;

    internal PatentApplicationSearchOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
    {
        if (client == null)
        {
            throw new ValidationException("An HTTP client is required.", "client", "REQUIRED", null);
        }

        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        Client = client;
        Options = options;
    }

    internal PatentApplicationResponse SearchGet(SearchRequest request)
    {
        ValidateSearchRequest(request);
        global::System.Collections.Generic.Dictionary<string, string?> query = OdpSearchRequestWriter.WriteGetQuery(request, Options.Limits);
        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "applications", "search" }, query))
        {
            if (message.RequestUri == null)
            {
                throw new ValidationException("The patent application search URI is required.", "RequestUri", "REQUIRED", null);
            }

            OdpSearchRequestWriter.ValidateGetRequestUri(message.RequestUri, Options.Limits);
            return SendSearchRequest(message, request);
        }
    }

    internal PatentApplicationResponse SearchPost(SearchRequest request)
    {
        ValidateSearchRequest(request);
        string body = OdpSearchRequestWriter.WritePostBody(request, Options.Limits);
        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateJsonPostRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "applications", "search" }, new global::System.Collections.Generic.Dictionary<string, string?>(), body))
        {
            return SendSearchRequest(message, request);
        }
    }

    internal PatentApplicationResponse SearchAll(SearchRequest request)
    {
        return SearchAllPages(request, true);
    }

    internal PatentApplicationResponse SearchAllGet(SearchRequest request)
    {
        return SearchAllPages(request, false);
    }

    internal PatentApplicationResponse SearchAllPost(SearchRequest request)
    {
        return SearchAllPages(request, true);
    }

    internal PatentApplicationResponse SearchAllPages(SearchRequest request, bool usePost)
    {
        ValidateSearchRequest(request);
        PatentApplicationOffsetPageSource source = new PatentApplicationOffsetPageSource(this, request, usePost);
        OffsetPaginationResult pagination = PaginationEngine.AggregateOffsetPages(source, request, Options.Limits, "applicationNumberText");
        PatentApplicationSummary[] summaries = new PatentApplicationSummary[pagination.Records.Length];
        global::System.Collections.Generic.List<PatentFileWrapper> applications = new global::System.Collections.Generic.List<PatentFileWrapper>();
        int index;
        for (index = 0; index < pagination.Records.Length; index += 1)
        {
            PatentApplicationPageRecord record = (PatentApplicationPageRecord)pagination.Records[index];
            summaries[index] = record.Summary;
            if (record.Application != null)
            {
                applications.Add(record.Application);
            }
        }

        PatentApplicationResponse result = new PatentApplicationResponse();
        result.Request = request;
        result.Summaries = summaries;
        result.Applications = applications.ToArray();
        result.Count = summaries.Length;
        result.Total = pagination.Total;
        result.HasTotal = pagination.HasTotal;
        result.Page = OdpJsonParser.CopyOffsetPage(request.OffsetPage);
        result.IsComplete = pagination.Completed;
        if (source.FirstResponse != null)
        {
            result.Facets = source.FirstResponse.Facets;
            result.RequestIdentifier = source.FirstResponse.RequestIdentifier;
            result.Provenance = OdpJsonParser.CopyProvenance(source.FirstResponse.Provenance);
            result.RawResponse = source.FirstResponse.RawResponse;
        }

        return result;
    }

    internal PatentApplicationResponse SendSearchRequest(global::System.Net.Http.HttpRequestMessage message, SearchRequest request)
    {
        RequestContext context = CreateRequestContext(request.Context);
        RequestHeaderPolicy.Apply(message, ApiService.Odp, Options, false, DocumentFormat.Json);
        HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, false);
        using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, message, timeout))
        {
            string[] secrets = CredentialSecrets();
            int statusCode = (int)response.StatusCode;
            if (statusCode < 200 || statusCode >= 300)
            {
                throw ErrorMapper.MapResponse(response, ApiService.Odp, "patent application search", Options.Limits, secrets, context.CancellationToken);
            }

            BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, context, Options.Limits, secrets, timeout);
            string? rawResponse = null;
            if (buffered.HasRawText)
            {
                rawResponse = buffered.RawText;
            }

            return OdpJsonParser.ParsePatentApplicationSearch(buffered.Text, request, buffered.Metadata, rawResponse);
        }
    }

    internal static void ValidateSearchRequest(SearchRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A patent application search request is required.", "request", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("A patent application search context is required.", "Context", "REQUIRED", null);
        }

        request.Context.CancellationToken.ThrowIfCancellationRequested();
    }

    internal static RequestContext CreateRequestContext(RequestContext source)
    {
        RequestContext context = new RequestContext();
        context.Service = ApiService.Odp;
        context.CancellationToken = source.CancellationToken;
        context.RetainRawResponse = source.RetainRawResponse;
        context.MaximumRawResponseBytes = source.MaximumRawResponseBytes;
        context.Deadline = source.Deadline;
        context.HasDeadline = source.HasDeadline;
        context.CorrelationIdentifier = source.CorrelationIdentifier;
        context.Fallback = source.Fallback;
        return context;
    }

    internal string[] CredentialSecrets()
    {
        if (Options.OdpApiKey == null)
        {
            return new string[0];
        }

        return new string[] { Options.OdpApiKey };
    }
}

internal class PatentApplicationPageRecord
{
    internal PatentApplicationSummary Summary = new PatentApplicationSummary();
    internal PatentFileWrapper? Application = null;
}

internal class PatentApplicationOffsetPageSource : IOffsetPageSource
{
    internal PatentApplicationSearchOperation Operation;
    internal SearchRequest Request;
    internal bool UsePost;
    internal PatentApplicationResponse? FirstResponse = null;

    internal PatentApplicationOffsetPageSource(PatentApplicationSearchOperation operation, SearchRequest request, bool usePost)
    {
        Operation = operation;
        Request = request;
        UsePost = usePost;
    }

    public OffsetPageResult FetchOffsetPage(long offset, int limit, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SearchRequest pageRequest = CreatePageRequest(offset, limit, cancellationToken);
        PatentApplicationResponse response;
        if (UsePost)
        {
            response = Operation.SearchPost(pageRequest);
        }
        else
        {
            response = Operation.SearchGet(pageRequest);
        }

        if (FirstResponse == null)
        {
            FirstResponse = response;
        }

        if (response.Applications.Length != 0 && response.Applications.Length != response.Summaries.Length)
        {
            throw new ValidationException("The patent application page has mismatched summary and wrapper counts.", "Applications", "PAGE_COUNT_MISMATCH", null);
        }

        object[] records = new object[response.Summaries.Length];
        string[] identifiers = new string[response.Summaries.Length];
        int index;
        for (index = 0; index < response.Summaries.Length; index += 1)
        {
            PatentApplicationPageRecord record = new PatentApplicationPageRecord();
            record.Summary = response.Summaries[index];
            if (response.Applications.Length != 0)
            {
                record.Application = response.Applications[index];
            }

            records[index] = record;
            identifiers[index] = response.Summaries[index].ApplicationNumber ?? string.Empty;
        }

        OffsetPageResult page = new OffsetPageResult();
        page.Records = records;
        page.RecordIdentifiers = identifiers;
        page.Metadata.Request = pageRequest;
        page.Metadata.ReturnedCount = response.Count;
        page.Metadata.OffsetPage = OdpJsonParser.CopyOffsetPage(response.Page);
        page.Metadata.HasOffsetPage = true;
        page.Metadata.Total = response.Total;
        page.Metadata.HasTotal = response.HasTotal;
        page.Metadata.RequestIdentifier = response.RequestIdentifier;
        page.Metadata.Provenance = response.Provenance.Source;
        page.Metadata.FromCache = response.Provenance.FromCache;
        return page;
    }

    internal SearchRequest CreatePageRequest(long offset, int limit, global::System.Threading.CancellationToken cancellationToken)
    {
        SearchRequest pageRequest = new SearchRequest();
        pageRequest.Query = Request.Query;
        pageRequest.Filters = Request.Filters;
        pageRequest.Ranges = Request.Ranges;
        pageRequest.Sorts = Request.Sorts;
        pageRequest.FieldSelection = CreateFieldSelection(Request.FieldSelection);
        pageRequest.Facets = Request.Facets;
        pageRequest.OffsetPage.Offset = offset;
        pageRequest.OffsetPage.Limit = limit;
        pageRequest.CursorPage = Request.CursorPage;
        pageRequest.UseCursorPage = Request.UseCursorPage;
        pageRequest.MaximumPages = Request.MaximumPages;
        pageRequest.MaximumResults = Request.MaximumResults;
        pageRequest.Context = CreateContext(Request.Context, cancellationToken);
        return pageRequest;
    }

    internal static SearchFieldSelection CreateFieldSelection(SearchFieldSelection selection)
    {
        return CreateFieldSelectionFor(selection, "applicationNumberText");
    }

    internal static SearchFieldSelection CreateFieldSelectionFor(SearchFieldSelection selection, string stableField)
    {
        if (selection == null)
        {
            throw new ValidationException("An ODP field selection is required.", "FieldSelection", "REQUIRED", null);
        }

        if (selection.Fields == null)
        {
            throw new ValidationException("ODP selected fields are required.", "FieldSelection.Fields", "REQUIRED", null);
        }

        if (selection.Fields.Length == 0)
        {
            return selection;
        }

        int index;
        for (index = 0; index < selection.Fields.Length; index += 1)
        {
            if (global::System.String.Equals(selection.Fields[index], stableField, global::System.StringComparison.OrdinalIgnoreCase))
            {
                SearchFieldSelection copiedSelection = new SearchFieldSelection();
                copiedSelection.Fields = CopyStrings(selection.Fields);
                return copiedSelection;
            }
        }

        SearchFieldSelection augmentedSelection = new SearchFieldSelection();
        augmentedSelection.Fields = new string[selection.Fields.Length + 1];
        global::System.Array.Copy(selection.Fields, augmentedSelection.Fields, selection.Fields.Length);
        augmentedSelection.Fields[selection.Fields.Length] = stableField;
        return augmentedSelection;
    }

    internal static RequestContext CreateContext(RequestContext source, global::System.Threading.CancellationToken cancellationToken)
    {
        if (source == null)
        {
            throw new ValidationException("A patent application search context is required.", "Context", "REQUIRED", null);
        }

        RequestContext context = new RequestContext();
        context.Service = source.Service;
        context.CancellationToken = cancellationToken;
        context.RetainRawResponse = source.RetainRawResponse;
        context.MaximumRawResponseBytes = source.MaximumRawResponseBytes;
        context.Deadline = source.Deadline;
        context.HasDeadline = source.HasDeadline;
        context.CorrelationIdentifier = source.CorrelationIdentifier;
        context.Fallback = source.Fallback;
        return context;
    }

    internal static string[] CopyStrings(string[] values)
    {
        string[] copy = new string[values.Length];
        global::System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
