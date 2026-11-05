namespace uspto;

internal class PatentStatusCodeOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;

    internal PatentStatusCodeOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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

    internal PatentStatusCodeResponse SearchGet(SearchRequest request)
    {
        ValidateStatusRequest(request);
        global::System.Collections.Generic.Dictionary<string, string?> query = OdpSearchRequestWriter.WriteGetQuery(request, Options.Limits);
        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "status-codes" }, query))
        {
            if (message.RequestUri == null)
            {
                throw new ValidationException("The patent status-code search URI is required.", "RequestUri", "REQUIRED", null);
            }

            OdpSearchRequestWriter.ValidateGetRequestUri(message.RequestUri, Options.Limits);
            return SendSearchRequest(message, request);
        }
    }

    internal PatentStatusCodeResponse SearchPost(SearchRequest request)
    {
        ValidateStatusRequest(request);
        string body = OdpSearchRequestWriter.WritePostBody(request, Options.Limits);
        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateJsonPostRequest(Options.OdpBaseUri, new string[] { "api", "v1", "patent", "status-codes" }, new global::System.Collections.Generic.Dictionary<string, string?>(), body))
        {
            return SendSearchRequest(message, request);
        }
    }

    internal PatentStatusCodeResponse SearchAll(SearchRequest request)
    {
        return SearchAllPages(request, true);
    }

    internal PatentStatusCodeResponse SearchAllGet(SearchRequest request)
    {
        return SearchAllPages(request, false);
    }

    internal PatentStatusCodeResponse SearchAllPost(SearchRequest request)
    {
        return SearchAllPages(request, true);
    }

    internal PatentStatusCodeResponse SearchAllPages(SearchRequest request, bool usePost)
    {
        ValidateStatusRequest(request);
        SearchRequest paginationRequest = CreatePaginationRequest(request);
        PatentStatusCodeOffsetPageSource source = new PatentStatusCodeOffsetPageSource(this, paginationRequest, usePost);
        OffsetPaginationResult pagination = PaginationEngine.AggregateOffsetPages(source, paginationRequest, Options.Limits, "applicationStatusCode");
        PatentStatusCode[] statusCodes = new PatentStatusCode[pagination.Records.Length];
        int index;
        for (index = 0; index < pagination.Records.Length; index += 1)
        {
            statusCodes[index] = (PatentStatusCode)pagination.Records[index];
        }

        PatentStatusCodeResponse result = new PatentStatusCodeResponse();
        result.Request = request;
        result.StatusCodes = statusCodes;
        result.DescriptionsByCode = OdpPatentStatusCodeParser.BuildDescriptionMap(statusCodes);
        result.Count = statusCodes.Length;
        result.Total = pagination.Total;
        result.HasTotal = pagination.HasTotal;
        result.Page = OdpJsonParser.CopyOffsetPage(request.OffsetPage);
        result.IsComplete = pagination.Completed;
        if (source.FirstResponse != null)
        {
            result.RequestIdentifier = source.FirstResponse.RequestIdentifier;
            result.Provenance = OdpJsonParser.CopyProvenance(source.FirstResponse.Provenance);
            result.RawResponse = source.FirstResponse.RawResponse;
            result.UnknownFields = source.FirstResponse.UnknownFields;
        }

        return result;
    }

    internal static SearchRequest CreatePaginationRequest(SearchRequest request)
    {
        SearchRequest paginationRequest = new SearchRequest();
        paginationRequest.Query = request.Query;
        paginationRequest.Sorts = new SearchSort[] { new SearchSort { Field = "applicationStatusCode", Direction = SortDirection.Ascending } };
        paginationRequest.OffsetPage = OdpJsonParser.CopyOffsetPage(request.OffsetPage);
        paginationRequest.MaximumPages = request.MaximumPages;
        paginationRequest.MaximumResults = request.MaximumResults;
        paginationRequest.Context = request.Context;
        return paginationRequest;
    }

    internal PatentStatusCodeResponse SendSearchRequest(global::System.Net.Http.HttpRequestMessage message, SearchRequest request)
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
                throw ErrorMapper.MapResponse(response, ApiService.Odp, "patent status-code search", Options.Limits, secrets, context.CancellationToken);
            }

            BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, context, Options.Limits, secrets, timeout);
            string? rawResponse = null;
            if (buffered.HasRawText)
            {
                rawResponse = buffered.RawText;
            }

            return OdpPatentStatusCodeParser.Parse(buffered.Text, request, buffered.Metadata, rawResponse);
        }
    }

    internal static void ValidateStatusRequest(SearchRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A patent status-code search request is required.", "request", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("A patent status-code search context is required.", "Context", "REQUIRED", null);
        }

        request.Context.CancellationToken.ThrowIfCancellationRequested();
        if (request.Filters == null || request.Ranges == null || request.Sorts == null || request.FieldSelection == null || request.Facets == null)
        {
            throw new ValidationException("Patent status-code search collections are required.", "request", "REQUIRED", null);
        }

        if (request.FieldSelection.Fields == null)
        {
            throw new ValidationException("Patent status-code selected fields are required.", "FieldSelection.Fields", "REQUIRED", null);
        }

        if (request.Filters.Length != 0 || request.Ranges.Length != 0 || request.Sorts.Length != 0 || request.FieldSelection.Fields.Length != 0 || request.Facets.Length != 0 || request.UseCursorPage)
        {
            throw new ValidationException("Patent status-code search supports only query text and offset pagination.", "request", "UNSUPPORTED_STATUS_CODE_SEARCH_OPTION", null);
        }
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

internal class PatentStatusCodeOffsetPageSource : IOffsetPageSource
{
    internal PatentStatusCodeOperation Operation;
    internal SearchRequest Request;
    internal bool UsePost;
    internal PatentStatusCodeResponse? FirstResponse = null;

    internal PatentStatusCodeOffsetPageSource(PatentStatusCodeOperation operation, SearchRequest request, bool usePost)
    {
        Operation = operation;
        Request = request;
        UsePost = usePost;
    }

    public OffsetPageResult FetchOffsetPage(long offset, int limit, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SearchRequest pageRequest = CreatePageRequest(offset, limit, cancellationToken);
        PatentStatusCodeResponse response;
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

        object[] records = new object[response.StatusCodes.Length];
        string[] identifiers = new string[response.StatusCodes.Length];
        int index;
        for (index = 0; index < response.StatusCodes.Length; index += 1)
        {
            records[index] = response.StatusCodes[index];
            identifiers[index] = response.StatusCodes[index].Code.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
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
        pageRequest.OffsetPage.Offset = offset;
        pageRequest.OffsetPage.Limit = limit;
        pageRequest.MaximumPages = Request.MaximumPages;
        pageRequest.MaximumResults = Request.MaximumResults;
        pageRequest.Context = PatentApplicationOffsetPageSource.CreateContext(Request.Context, cancellationToken);
        return pageRequest;
    }
}
