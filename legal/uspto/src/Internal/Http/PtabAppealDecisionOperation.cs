namespace uspto;

internal class PtabAppealDecisionOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;

    internal PtabAppealDecisionOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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

    internal PtabAppealDecisionResponse SearchGet(SearchRequest request)
    {
        ValidateSearchRequest(request);
        global::System.Collections.Generic.Dictionary<string, string?> query = OdpSearchRequestWriter.WriteGetQuery(request, Options.Limits);
        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "appeals", "decisions", "search" }, query))
        {
            if (message.RequestUri == null)
            {
                throw new ValidationException("The PTAB appeal decision search URI is required.", "RequestUri", "REQUIRED", null);
            }

            OdpSearchRequestWriter.ValidateGetRequestUri(message.RequestUri, Options.Limits);
            return SendSearchRequest(message, request);
        }
    }

    internal PtabAppealDecisionResponse SearchPost(SearchRequest request)
    {
        ValidateSearchRequest(request);
        string body = OdpSearchRequestWriter.WritePostBody(request, Options.Limits);
        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateJsonPostRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "appeals", "decisions", "search" }, new global::System.Collections.Generic.Dictionary<string, string?>(), body))
        {
            return SendSearchRequest(message, request);
        }
    }

    internal PtabAppealDecisionResponse SearchAll(SearchRequest request)
    {
        ValidateSearchRequest(request);
        PtabAppealDecisionOffsetPageSource source = new PtabAppealDecisionOffsetPageSource(this, request);
        OffsetPaginationResult pagination = PaginationEngine.AggregateOffsetPages(source, request, Options.Limits, "documentData.documentIdentifier");
        PtabAppealDecision[] decisions = new PtabAppealDecision[pagination.Records.Length];
        int index;
        for (index = 0; index < pagination.Records.Length; index += 1)
        {
            decisions[index] = (PtabAppealDecision)pagination.Records[index];
        }

        PtabAppealDecisionResponse result = new PtabAppealDecisionResponse();
        result.Request = request;
        result.Decisions = decisions;
        result.Count = decisions.Length;
        result.Total = pagination.Total;
        result.HasTotal = pagination.HasTotal;
        result.Page = OdpJsonParser.CopyOffsetPage(request.OffsetPage);
        result.IsComplete = pagination.Completed;
        if (source.FirstResponse != null)
        {
            result.Facets = source.FirstResponse.Facets;
            result.RequestIdentifier = source.FirstResponse.RequestIdentifier;
            result.Provenance = OdpPtabProceedingParser.CopyProvenance(source.FirstResponse.Provenance);
            result.RawResponse = source.FirstResponse.RawResponse;
        }

        return result;
    }

    internal PtabAppealDecision GetDocument(string documentIdentifier, RequestContext context)
    {
        ValidateIdentifier(documentIdentifier, "documentIdentifier", "A PTAB appeal decision document identifier is required.");
        ValidateContext(context);
        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "appeals", "decisions", documentIdentifier }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestContext requestContext = PatentApplicationSearchOperation.CreateRequestContext(context);
            BufferedHttpResponse buffered = SendRetrievalRequest(message, requestContext, "PTAB appeal decision retrieval");
            return OdpPtabAppealDecisionParser.ParseSingle(buffered.Text, documentIdentifier, buffered.Metadata);
        }
    }

    internal PtabAppealDecisionResponse GetByAppeal(string appealNumber, RequestContext context)
    {
        ValidateIdentifier(appealNumber, "appealNumber", "A PTAB appeal number is required.");
        ValidateContext(context);
        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "appeals", appealNumber, "decisions" }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestContext requestContext = PatentApplicationSearchOperation.CreateRequestContext(context);
            BufferedHttpResponse buffered = SendRetrievalRequest(message, requestContext, "PTAB appeal decisions-by-appeal retrieval");
            return OdpPtabAppealDecisionParser.ParseByAppeal(buffered.Text, appealNumber, buffered.Metadata);
        }
    }

    internal PtabAppealDecisionResponse SendSearchRequest(global::System.Net.Http.HttpRequestMessage message, SearchRequest request)
    {
        RequestContext context = PatentApplicationSearchOperation.CreateRequestContext(request.Context);
        RequestHeaderPolicy.Apply(message, ApiService.Odp, Options, false, DocumentFormat.Json);
        HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, false);
        using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, message, timeout))
        {
            string[] secrets = CredentialSecrets();
            int statusCode = (int)response.StatusCode;
            if (statusCode < 200 || statusCode >= 300)
            {
                throw ErrorMapper.MapResponse(response, ApiService.Odp, "PTAB appeal decision search", Options.Limits, secrets, context.CancellationToken);
            }

            BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, context, Options.Limits, secrets, timeout);
            string? rawResponse = null;
            if (buffered.HasRawText)
            {
                rawResponse = buffered.RawText;
            }

            return OdpPtabAppealDecisionParser.ParseSearch(buffered.Text, request, buffered.Metadata, rawResponse);
        }
    }

    internal BufferedHttpResponse SendRetrievalRequest(global::System.Net.Http.HttpRequestMessage message, RequestContext context, string operationName)
    {
        RequestHeaderPolicy.Apply(message, ApiService.Odp, Options, false, DocumentFormat.Json);
        HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, false);
        using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, message, timeout))
        {
            string[] secrets = CredentialSecrets();
            int statusCode = (int)response.StatusCode;
            if (statusCode < 200 || statusCode >= 300)
            {
                throw ErrorMapper.MapResponse(response, ApiService.Odp, operationName, Options.Limits, secrets, context.CancellationToken);
            }

            return TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, context, Options.Limits, secrets, timeout);
        }
    }

    internal static void ValidateSearchRequest(SearchRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A PTAB appeal decision search request is required.", "request", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("A PTAB appeal decision search context is required.", "Context", "REQUIRED", null);
        }

        request.Context.CancellationToken.ThrowIfCancellationRequested();
    }

    internal static void ValidateIdentifier(string value, string fieldName, string message)
    {
        if (global::System.String.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException(message, fieldName, "REQUIRED", null);
        }
    }

    internal static void ValidateContext(RequestContext context)
    {
        if (context == null)
        {
            throw new ValidationException("A PTAB appeal decision request context is required.", "context", "REQUIRED", null);
        }

        context.CancellationToken.ThrowIfCancellationRequested();
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

internal class PtabAppealDecisionOffsetPageSource : IOffsetPageSource
{
    internal PtabAppealDecisionOperation Operation;
    internal SearchRequest Request;
    internal PtabAppealDecisionResponse? FirstResponse = null;

    internal PtabAppealDecisionOffsetPageSource(PtabAppealDecisionOperation operation, SearchRequest request)
    {
        Operation = operation;
        Request = request;
    }

    public OffsetPageResult FetchOffsetPage(long offset, int limit, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SearchRequest pageRequest = CreatePageRequest(offset, limit, cancellationToken);
        PtabAppealDecisionResponse response = Operation.SearchPost(pageRequest);
        if (FirstResponse == null)
        {
            FirstResponse = response;
        }

        object[] records = new object[response.Decisions.Length];
        string[] identifiers = new string[response.Decisions.Length];
        int index;
        for (index = 0; index < response.Decisions.Length; index += 1)
        {
            records[index] = response.Decisions[index];
            identifiers[index] = response.Decisions[index].Document.DocumentIdentifier ?? string.Empty;
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
        pageRequest.FieldSelection = PatentApplicationOffsetPageSource.CreateFieldSelectionFor(Request.FieldSelection, "documentData.documentIdentifier");
        pageRequest.Facets = Request.Facets;
        pageRequest.OffsetPage.Offset = offset;
        pageRequest.OffsetPage.Limit = limit;
        pageRequest.CursorPage = Request.CursorPage;
        pageRequest.UseCursorPage = Request.UseCursorPage;
        pageRequest.MaximumPages = Request.MaximumPages;
        pageRequest.MaximumResults = Request.MaximumResults;
        pageRequest.Context = PatentApplicationOffsetPageSource.CreateContext(Request.Context, cancellationToken);
        return pageRequest;
    }
}
