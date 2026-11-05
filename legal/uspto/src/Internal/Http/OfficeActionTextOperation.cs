namespace uspto;

internal class OfficeActionTextOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;

    internal OfficeActionTextOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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

    internal OfficeActionTextResponse Search(SearchRequest request)
    {
        ValidateRequest(request);
        using (global::System.Net.Http.HttpRequestMessage message = OfficeActionQueryWriter.CreateFormRequest(Options.OfficeActionBaseUri, "oa_actions", "v1", request, Options.Limits))
        {
            return Send(message, request);
        }
    }

    internal OfficeActionTextResponse SearchAll(SearchRequest request)
    {
        ValidateRequest(request);
        SearchRequest paginationRequest = CreatePaginationRequest(request);
        OfficeActionTextOffsetPageSource source = new OfficeActionTextOffsetPageSource(this, request);
        OffsetPaginationResult pagination = PaginationEngine.AggregateOffsetPages(source, paginationRequest, Options.Limits, "id");
        OfficeActionTextRecord[] records = new OfficeActionTextRecord[pagination.Records.Length];
        int index;
        for (index = 0; index < pagination.Records.Length; index += 1)
        {
            records[index] = (OfficeActionTextRecord)pagination.Records[index];
        }

        OfficeActionTextResponse result = new OfficeActionTextResponse();
        result.Request = request;
        result.Records = records;
        result.Count = records.Length;
        result.Total = pagination.Total;
        result.HasTotal = pagination.HasTotal;
        result.Start = request.OffsetPage.Offset;
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

    internal OfficeActionTextResponse Send(global::System.Net.Http.HttpRequestMessage message, SearchRequest request)
    {
        RequestContext context = PatentApplicationSearchOperation.CreateRequestContext(request.Context);
        context.Service = ApiService.OfficeAction;
        RequestHeaderPolicy.Apply(message, ApiService.OfficeAction, Options, false, DocumentFormat.Json);
        HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, false);
        using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, message, timeout))
        {
            string[] secrets = CredentialSecrets();
            int statusCode = (int)response.StatusCode;
            if (statusCode < 200 || statusCode >= 300)
            {
                throw ErrorMapper.MapResponse(response, ApiService.OfficeAction, "Office Action text search", Options.Limits, secrets, context.CancellationToken);
            }

            BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, context, Options.Limits, secrets, timeout);
            string? rawResponse = null;
            if (buffered.HasRawText)
            {
                rawResponse = buffered.RawText;
            }

            return OfficeActionTextParser.Parse(buffered.Text, request, buffered.Metadata, rawResponse);
        }
    }

    internal static void ValidateRequest(SearchRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("An Office Action text search request is required.", "request", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("An Office Action text search context is required.", "Context", "REQUIRED", null);
        }

        request.Context.CancellationToken.ThrowIfCancellationRequested();
        OfficeActionQueryWriter.ValidateSearchRequest(request);
        OfficeActionQueryWriter.ValidatePage(request);
    }

    internal static SearchRequest CreatePaginationRequest(SearchRequest request)
    {
        SearchRequest result = new SearchRequest();
        result.Query = request.Query;
        result.Filters = request.Filters;
        result.Ranges = request.Ranges;
        result.Sorts = new SearchSort[] { new SearchSort { Field = "id", Direction = SortDirection.Ascending } };
        result.FieldSelection = request.FieldSelection;
        result.Facets = request.Facets;
        result.OffsetPage = OdpJsonParser.CopyOffsetPage(request.OffsetPage);
        result.CursorPage = request.CursorPage;
        result.UseCursorPage = request.UseCursorPage;
        result.MaximumPages = request.MaximumPages;
        result.MaximumResults = request.MaximumResults;
        result.Context = request.Context;
        return result;
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

internal class OfficeActionTextOffsetPageSource : IOffsetPageSource
{
    internal OfficeActionTextOperation Operation;
    internal SearchRequest Request;
    internal OfficeActionTextResponse? FirstResponse = null;

    internal OfficeActionTextOffsetPageSource(OfficeActionTextOperation operation, SearchRequest request)
    {
        Operation = operation;
        Request = request;
    }

    public OffsetPageResult FetchOffsetPage(long offset, int limit, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SearchRequest pageRequest = CreatePageRequest(offset, limit, cancellationToken);
        OfficeActionTextResponse response = Operation.Search(pageRequest);
        if (FirstResponse == null)
        {
            FirstResponse = response;
        }

        object[] records = new object[response.Records.Length];
        string[] identifiers = new string[response.Records.Length];
        int index;
        for (index = 0; index < response.Records.Length; index += 1)
        {
            records[index] = response.Records[index];
            identifiers[index] = response.Records[index].Identifier ?? string.Empty;
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
        SearchRequest result = new SearchRequest();
        result.Query = Request.Query;
        result.Filters = Request.Filters;
        result.Ranges = Request.Ranges;
        result.Sorts = new SearchSort[0];
        result.FieldSelection = Request.FieldSelection;
        result.Facets = Request.Facets;
        result.OffsetPage.Offset = offset;
        result.OffsetPage.Limit = limit;
        result.MaximumPages = Request.MaximumPages;
        result.MaximumResults = Request.MaximumResults;
        result.Context = PatentApplicationOffsetPageSource.CreateContext(Request.Context, cancellationToken);
        return result;
    }
}
