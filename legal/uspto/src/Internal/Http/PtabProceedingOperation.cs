namespace uspto;

internal class PtabProceedingOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;

    internal PtabProceedingOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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

    internal PtabTrialProceedingResponse SearchGet(SearchRequest request)
    {
        ValidateSearchRequest(request);
        global::System.Collections.Generic.Dictionary<string, string?> query = OdpSearchRequestWriter.WriteGetQuery(request, Options.Limits);
        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "proceedings", "search" }, query))
        {
            if (message.RequestUri == null)
            {
                throw new ValidationException("The PTAB proceeding search URI is required.", "RequestUri", "REQUIRED", null);
            }

            OdpSearchRequestWriter.ValidateGetRequestUri(message.RequestUri, Options.Limits);
            return SendSearchRequest(message, request);
        }
    }

    internal PtabTrialProceedingResponse SearchPost(SearchRequest request)
    {
        ValidateSearchRequest(request);
        string body = OdpSearchRequestWriter.WritePostBody(request, Options.Limits);
        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateJsonPostRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "proceedings", "search" }, new global::System.Collections.Generic.Dictionary<string, string?>(), body))
        {
            return SendSearchRequest(message, request);
        }
    }

    internal PtabTrialProceedingResponse SearchAll(SearchRequest request)
    {
        ValidateSearchRequest(request);
        PtabProceedingOffsetPageSource source = new PtabProceedingOffsetPageSource(this, request);
        OffsetPaginationResult pagination = PaginationEngine.AggregateOffsetPages(source, request, Options.Limits, "trialNumber");
        PtabTrialProceeding[] proceedings = new PtabTrialProceeding[pagination.Records.Length];
        int index;
        for (index = 0; index < pagination.Records.Length; index += 1)
        {
            proceedings[index] = (PtabTrialProceeding)pagination.Records[index];
        }

        PtabTrialProceedingResponse result = new PtabTrialProceedingResponse();
        result.Request = request;
        result.Proceedings = proceedings;
        result.Count = proceedings.Length;
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

    internal PtabTrialProceeding Get(string trialNumber, RequestContext context)
    {
        ValidateRetrievalRequest(trialNumber, context);
        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "proceedings", trialNumber }, new global::System.Collections.Generic.Dictionary<string, string?>()))
        {
            RequestContext requestContext = PatentApplicationSearchOperation.CreateRequestContext(context);
            RequestHeaderPolicy.Apply(message, ApiService.Odp, Options, false, DocumentFormat.Json);
            HttpTimeoutScope timeout = new HttpTimeoutScope(Options, requestContext, false);
            using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, message, timeout))
            {
                string[] secrets = CredentialSecrets();
                int statusCode = (int)response.StatusCode;
                if (statusCode < 200 || statusCode >= 300)
                {
                    throw ErrorMapper.MapResponse(response, ApiService.Odp, "PTAB proceeding retrieval", Options.Limits, secrets, requestContext.CancellationToken);
                }

                BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, requestContext, Options.Limits, secrets, timeout);
                return OdpPtabProceedingParser.ParseSingle(buffered.Text, trialNumber, buffered.Metadata);
            }
        }
    }

    internal PtabTrialProceedingResponse SendSearchRequest(global::System.Net.Http.HttpRequestMessage message, SearchRequest request)
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
                throw ErrorMapper.MapResponse(response, ApiService.Odp, "PTAB proceeding search", Options.Limits, secrets, context.CancellationToken);
            }

            BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, context, Options.Limits, secrets, timeout);
            string? rawResponse = null;
            if (buffered.HasRawText)
            {
                rawResponse = buffered.RawText;
            }

            return OdpPtabProceedingParser.ParseSearch(buffered.Text, request, buffered.Metadata, rawResponse);
        }
    }

    internal static void ValidateSearchRequest(SearchRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A PTAB proceeding search request is required.", "request", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("A PTAB proceeding search context is required.", "Context", "REQUIRED", null);
        }

        request.Context.CancellationToken.ThrowIfCancellationRequested();
    }

    internal static void ValidateRetrievalRequest(string trialNumber, RequestContext context)
    {
        if (global::System.String.IsNullOrWhiteSpace(trialNumber))
        {
            throw new ValidationException("A PTAB trial number is required.", "trialNumber", "REQUIRED", null);
        }

        if (context == null)
        {
            throw new ValidationException("A PTAB proceeding request context is required.", "context", "REQUIRED", null);
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

internal class PtabProceedingOffsetPageSource : IOffsetPageSource
{
    internal PtabProceedingOperation Operation;
    internal SearchRequest Request;
    internal PtabTrialProceedingResponse? FirstResponse = null;

    internal PtabProceedingOffsetPageSource(PtabProceedingOperation operation, SearchRequest request)
    {
        Operation = operation;
        Request = request;
    }

    public OffsetPageResult FetchOffsetPage(long offset, int limit, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SearchRequest pageRequest = CreatePageRequest(offset, limit, cancellationToken);
        PtabTrialProceedingResponse response = Operation.SearchPost(pageRequest);
        if (FirstResponse == null)
        {
            FirstResponse = response;
        }

        object[] records = new object[response.Proceedings.Length];
        string[] identifiers = new string[response.Proceedings.Length];
        int index;
        for (index = 0; index < response.Proceedings.Length; index += 1)
        {
            records[index] = response.Proceedings[index];
            identifiers[index] = response.Proceedings[index].TrialNumber ?? string.Empty;
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
        pageRequest.FieldSelection = PatentApplicationOffsetPageSource.CreateFieldSelectionFor(Request.FieldSelection, "trialNumber");
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
