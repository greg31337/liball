namespace uspto;

internal class OfficeActionCitationOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;

    internal OfficeActionCitationOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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

    internal OfficeActionCitationResponse Search(SearchRequest request)
    {
        ValidateRequest(request);
        using (global::System.Net.Http.HttpRequestMessage message = OfficeActionQueryWriter.CreateFormRequest(Options.OfficeActionBaseUri, "oa_citations", "v2", request, Options.Limits))
        {
            return Send(message, request);
        }
    }

    internal OfficeActionCitationResponse SearchAll(SearchRequest request)
    {
        ValidateRequest(request);
        SearchRequest paginationRequest = OfficeActionTextOperation.CreatePaginationRequest(request);
        OfficeActionCitationOffsetPageSource source = new OfficeActionCitationOffsetPageSource(this, request);
        OffsetPaginationResult pagination = PaginationEngine.AggregateOffsetPages(source, paginationRequest, Options.Limits, "id");
        OfficeActionCitation[] citations = new OfficeActionCitation[pagination.Records.Length];
        int index;
        for (index = 0; index < pagination.Records.Length; index += 1)
        {
            citations[index] = (OfficeActionCitation)pagination.Records[index];
        }

        OfficeActionCitationResponse result = new OfficeActionCitationResponse();
        result.Request = request;
        result.Citations = citations;
        result.Count = citations.Length;
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

    internal OfficeActionCitationResponse Send(global::System.Net.Http.HttpRequestMessage message, SearchRequest request)
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
                throw ErrorMapper.MapResponse(response, ApiService.OfficeAction, "Office Action citation search", Options.Limits, secrets, context.CancellationToken);
            }

            BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, context, Options.Limits, secrets, timeout);
            string? rawResponse = null;
            if (buffered.HasRawText)
            {
                rawResponse = buffered.RawText;
            }

            return OfficeActionCitationParser.Parse(buffered.Text, request, buffered.Metadata, rawResponse);
        }
    }

    internal static void ValidateRequest(SearchRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("An Office Action citation search request is required.", "request", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("An Office Action citation search context is required.", "Context", "REQUIRED", null);
        }

        request.Context.CancellationToken.ThrowIfCancellationRequested();
        OfficeActionQueryWriter.ValidateSearchRequest(request);
        OfficeActionQueryWriter.ValidatePage(request);
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

internal class OfficeActionCitationOffsetPageSource : IOffsetPageSource
{
    internal OfficeActionCitationOperation Operation;
    internal SearchRequest Request;
    internal OfficeActionCitationResponse? FirstResponse = null;

    internal OfficeActionCitationOffsetPageSource(OfficeActionCitationOperation operation, SearchRequest request)
    {
        Operation = operation;
        Request = request;
    }

    public OffsetPageResult FetchOffsetPage(long offset, int limit, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SearchRequest pageRequest = CreatePageRequest(offset, limit, cancellationToken);
        OfficeActionCitationResponse response = Operation.Search(pageRequest);
        if (FirstResponse == null)
        {
            FirstResponse = response;
        }

        object[] records = new object[response.Citations.Length];
        string[] identifiers = new string[response.Citations.Length];
        int index;
        for (index = 0; index < response.Citations.Length; index += 1)
        {
            records[index] = response.Citations[index];
            identifiers[index] = response.Citations[index].Identifier ?? string.Empty;
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
        result.OffsetPage.Offset = offset;
        result.OffsetPage.Limit = limit;
        result.MaximumPages = Request.MaximumPages;
        result.MaximumResults = Request.MaximumResults;
        result.Context = PatentApplicationOffsetPageSource.CreateContext(Request.Context, cancellationToken);
        return result;
    }
}
