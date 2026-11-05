namespace uspto;

internal class OfficeActionEnrichedCitationOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;

    internal OfficeActionEnrichedCitationOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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

    internal OfficeActionEnrichedCitationResponse Search(SearchRequest request)
    {
        ValidateEnrichedRequest(request);
        using (global::System.Net.Http.HttpRequestMessage message = OfficeActionQueryWriter.CreateFormRequest(Options.OfficeActionBaseUri, "enriched_cited_reference_metadata", "v3", request, Options.Limits))
        {
            return SendEnrichedRequest(message, request);
        }
    }

    internal OfficeActionEnrichedCitationResponse SearchAll(SearchRequest request)
    {
        ValidateEnrichedRequest(request);
        SearchRequest paginationRequest = OfficeActionTextOperation.CreatePaginationRequest(request);
        OfficeActionEnrichedCitationOffsetPageSource source = new OfficeActionEnrichedCitationOffsetPageSource(this, request);
        OffsetPaginationResult pagination = PaginationEngine.AggregateOffsetPages(source, paginationRequest, Options.Limits, "id");
        OfficeActionEnrichedCitation[] citations = new OfficeActionEnrichedCitation[pagination.Records.Length];
        int index;
        for (index = 0; index < pagination.Records.Length; index += 1)
        {
            citations[index] = (OfficeActionEnrichedCitation)pagination.Records[index];
        }

        OfficeActionEnrichedCitationResponse result = new OfficeActionEnrichedCitationResponse();
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

    internal OfficeActionEnrichedCitationResponse SendEnrichedRequest(global::System.Net.Http.HttpRequestMessage message, SearchRequest request)
    {
        RequestContext context = PatentApplicationSearchOperation.CreateRequestContext(request.Context);
        context.Service = ApiService.OfficeAction;
        RequestHeaderPolicy.Apply(message, ApiService.OfficeAction, Options, false, DocumentFormat.Json);
        HttpTimeoutScope timeout = new HttpTimeoutScope(Options, context, false);
        using (global::System.Net.Http.HttpResponseMessage response = TimeoutController.SendHeaderPhase(Client, message, timeout))
        {
            string[] secrets = EnrichedCredentialSecrets();
            int statusCode = (int)response.StatusCode;
            if (statusCode < 200 || statusCode >= 300)
            {
                throw ErrorMapper.MapResponse(response, ApiService.OfficeAction, "enriched Office Action citation search", Options.Limits, secrets, context.CancellationToken);
            }

            BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, context, Options.Limits, secrets, timeout);
            string? rawResponse = null;
            if (buffered.HasRawText)
            {
                rawResponse = buffered.RawText;
            }

            return OfficeActionEnrichedCitationParser.Parse(buffered.Text, request, buffered.Metadata, rawResponse);
        }
    }

    internal static void ValidateEnrichedRequest(SearchRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("An enriched Office Action citation search request is required.", "request", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("An enriched Office Action citation search context is required.", "Context", "REQUIRED", null);
        }

        request.Context.CancellationToken.ThrowIfCancellationRequested();
        OfficeActionQueryWriter.ValidateSearchRequest(request);
        OfficeActionQueryWriter.ValidatePage(request);
    }

    internal string[] EnrichedCredentialSecrets()
    {
        if (Options.OdpApiKey == null)
        {
            return new string[0];
        }

        return new string[] { Options.OdpApiKey };
    }
}

internal class OfficeActionEnrichedCitationOffsetPageSource : IOffsetPageSource
{
    internal OfficeActionEnrichedCitationOperation Operation;
    internal SearchRequest Request;
    internal OfficeActionEnrichedCitationResponse? FirstResponse = null;

    internal OfficeActionEnrichedCitationOffsetPageSource(OfficeActionEnrichedCitationOperation operation, SearchRequest request)
    {
        Operation = operation;
        Request = request;
    }

    public OffsetPageResult FetchOffsetPage(long offset, int limit, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SearchRequest pageRequest = CreateEnrichedPageRequest(offset, limit, cancellationToken);
        OfficeActionEnrichedCitationResponse response = Operation.Search(pageRequest);
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

    internal SearchRequest CreateEnrichedPageRequest(long offset, int limit, global::System.Threading.CancellationToken cancellationToken)
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
