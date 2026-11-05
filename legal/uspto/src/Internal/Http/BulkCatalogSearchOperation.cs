namespace uspto;

internal class BulkCatalogSearchOperation
{
    internal global::System.Net.Http.HttpClient Client;
    internal UsptoClientOptions Options;

    internal BulkCatalogSearchOperation(global::System.Net.Http.HttpClient client, UsptoClientOptions options)
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

    internal BulkProductResponse Search(SearchRequest request)
    {
        ValidateRequest(request);
        global::System.Collections.Generic.Dictionary<string, string?> query = OdpSearchRequestWriter.WriteGetQuery(request, Options.Limits);
        using (global::System.Net.Http.HttpRequestMessage message = RequestBuilder.CreateGetRequest(Options.OdpBaseUri, new string[] { "api", "v1", "datasets", "products", "search" }, query))
        {
            if (message.RequestUri == null)
            {
                throw new ValidationException("The bulk catalog search URI is required.", "RequestUri", "REQUIRED", null);
            }

            OdpSearchRequestWriter.ValidateGetRequestUri(message.RequestUri, Options.Limits);
            return Send(message, request);
        }
    }

    internal BulkProductResponse SearchAll(SearchRequest request)
    {
        ValidateRequest(request);
        BulkCatalogOffsetPageSource source = new BulkCatalogOffsetPageSource(this, request);
        OffsetPaginationResult pagination = PaginationEngine.AggregateOffsetPages(source, request, Options.Limits, "productIdentifier");
        BulkProduct[] products = new BulkProduct[pagination.Records.Length];
        int index;
        for (index = 0; index < pagination.Records.Length; index += 1)
        {
            products[index] = (BulkProduct)pagination.Records[index];
        }

        BulkProductResponse result = new BulkProductResponse();
        result.Request = request;
        result.Products = products;
        result.Count = products.LongLength;
        result.HasCount = true;
        result.ReturnedCount = products.Length;
        result.Page = OdpJsonParser.CopyOffsetPage(request.OffsetPage);
        result.IsComplete = pagination.Completed;
        if (source.FirstResponse != null)
        {
            result.Facets = source.FirstResponse.Facets;
            result.RequestIdentifier = source.FirstResponse.RequestIdentifier;
            result.Provenance = OdpJsonParser.CopyProvenance(source.FirstResponse.Provenance);
            result.RawResponse = source.FirstResponse.RawResponse;
            result.UnknownFields = source.FirstResponse.UnknownFields;
        }

        return result;
    }

    internal BulkProductResponse Send(global::System.Net.Http.HttpRequestMessage message, SearchRequest request)
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
                throw ErrorMapper.MapResponse(response, ApiService.Odp, "bulk product search", Options.Limits, secrets, context.CancellationToken);
            }

            BufferedHttpResponse buffered = TimeoutController.ReadTextBodyPhase(response, DocumentFormat.Json, context, Options.Limits, secrets, timeout);
            string? rawResponse = null;
            if (buffered.HasRawText)
            {
                rawResponse = buffered.RawText;
            }

            return OdpBulkCatalogParser.ParseSearch(buffered.Text, request, buffered.Metadata, rawResponse);
        }
    }

    internal static void ValidateRequest(SearchRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("A bulk catalog search request is required.", "request", "REQUIRED", null);
        }

        if (request.Context == null)
        {
            throw new ValidationException("A bulk catalog search context is required.", "Context", "REQUIRED", null);
        }

        request.Context.CancellationToken.ThrowIfCancellationRequested();
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

internal class BulkCatalogOffsetPageSource : IOffsetPageSource
{
    internal BulkCatalogSearchOperation Operation;
    internal SearchRequest Request;
    internal BulkProductResponse? FirstResponse = null;

    internal BulkCatalogOffsetPageSource(BulkCatalogSearchOperation operation, SearchRequest request)
    {
        Operation = operation;
        Request = request;
    }

    public OffsetPageResult FetchOffsetPage(long offset, int limit, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SearchRequest pageRequest = CreatePageRequest(offset, limit, cancellationToken);
        BulkProductResponse response = Operation.Search(pageRequest);
        if (FirstResponse == null)
        {
            FirstResponse = response;
        }

        object[] records = new object[response.Products.Length];
        string[] identifiers = new string[response.Products.Length];
        int index;
        for (index = 0; index < response.Products.Length; index += 1)
        {
            records[index] = response.Products[index];
            identifiers[index] = response.Products[index].ProductIdentifier ?? string.Empty;
        }

        OffsetPageResult page = new OffsetPageResult();
        page.Records = records;
        page.RecordIdentifiers = identifiers;
        page.Metadata.Request = pageRequest;
        page.Metadata.ReturnedCount = response.Products.Length;
        page.Metadata.OffsetPage = OdpJsonParser.CopyOffsetPage(response.Page);
        page.Metadata.HasOffsetPage = true;
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
        result.Sorts = Request.Sorts;
        result.FieldSelection = PatentApplicationOffsetPageSource.CreateFieldSelectionFor(Request.FieldSelection, "productIdentifier");
        result.Facets = Request.Facets;
        result.OffsetPage.Offset = offset;
        result.OffsetPage.Limit = limit;
        result.CursorPage = Request.CursorPage;
        result.UseCursorPage = Request.UseCursorPage;
        result.MaximumPages = Request.MaximumPages;
        result.MaximumResults = Request.MaximumResults;
        result.Context = PatentApplicationOffsetPageSource.CreateContext(Request.Context, cancellationToken);
        return result;
    }
}
