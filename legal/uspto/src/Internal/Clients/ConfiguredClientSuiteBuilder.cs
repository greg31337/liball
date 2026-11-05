namespace uspto;

internal class ConfiguredClientSuiteLifetime : global::System.IDisposable
{
    internal global::System.Net.Http.HttpClient Client;
    internal bool OwnsClient = false;
    internal bool IsDisposed = false;

    internal ConfiguredClientSuiteLifetime(global::System.Net.Http.HttpClient client, bool ownsClient)
    {
        Client = client;
        OwnsClient = ownsClient;
    }

    public void Dispose()
    {
        if (!IsDisposed)
        {
            IsDisposed = true;
            if (OwnsClient)
            {
                Client.Dispose();
            }
        }
    }
}

internal class ConfiguredClientComponents
{
    internal UsptoClientOptions Options;
    internal global::System.Net.Http.HttpClient Client;
    internal IApiKeyProvider ApiKeyProvider;
    internal IIdentifierNormalizer IdentifierNormalizer;
    internal RateLimiter RateLimiter;
    internal RetryEngine RetryEngine;
    internal MemoryResponseCache MemoryCache;
    internal FileResponseCache FileCache;
    internal DataFallbackCoordinator Fallback;
    internal DownloadHostPolicy DownloadHostPolicy;
    internal ConfiguredClientSuiteLifetime Lifetime;

    internal ConfiguredClientComponents(UsptoClientOptions options)
    {
        ConfiguredClientSuiteBuilder.ValidateFactoryOptions(options);
        Options = options;
        IdentifierNormalizer = new IdentifierNormalizer();
        RateLimiter = new RateLimiter(options.RateLimits);
        RetryEngine = global::uspto.RetryEngine.CreateDefault(options.Retry);
        MemoryCache = new MemoryResponseCache(options.Cache);
        FileCache = new FileResponseCache(options.Cache);
        Fallback = new DataFallbackCoordinator(options.Fallback);
        DownloadHostPolicy = DownloadHostPolicy.CreateDefault();
        ApiKeyProvider = options.ApiKeyProvider ?? new EnvironmentApiKeyProvider();
        bool createdClient = options.HttpClient == null;
        Client = options.HttpClient ?? new global::System.Net.Http.HttpClient();
        options.ApiKeyProvider = ApiKeyProvider;
        options.HttpClient = Client;
        Lifetime = new ConfiguredClientSuiteLifetime(Client, createdClient || options.DisposeHttpClient);
    }

    internal PatentsViewPatentOperation CreateSharedPatentsViewTransport()
    {
        PatentsViewPatentOperation operation = new PatentsViewPatentOperation(Client, Options);
        operation.RateLimiter = RateLimiter;
        operation.RetryEngine = RetryEngine;
        operation.MemoryCache = MemoryCache;
        operation.FileCache = FileCache;
        return operation;
    }

    internal PatentsViewCapabilityOperation CreateSharedPatentsViewCapability()
    {
        PatentsViewCapabilityOperation operation = new PatentsViewCapabilityOperation(Client, Options);
        operation.RateLimiter = RateLimiter;
        operation.RetryEngine = RetryEngine;
        operation.MemoryCache = MemoryCache;
        operation.FileCache = FileCache;
        return operation;
    }

    internal BulkFileResumeOperation CreateSharedBulkResume()
    {
        BulkFileResumeOperation operation = new BulkFileResumeOperation(Client, Options, DownloadHostPolicy);
        operation.RateLimiter = RateLimiter;
        operation.RetryEngine = RetryEngine;
        return operation;
    }
}

internal static class ConfiguredClientSuiteBuilder
{
    internal static UsptoClients CreateConfiguredClients(UsptoClientOptions options)
    {
        ConfiguredClientComponents components = new ConfiguredClientComponents(options);
        ConfiguredPatentsViewClient patentsView = new ConfiguredPatentsViewClient(components);
        UsptoClients result = new UsptoClients();
        result.Options = options;
        result.ApiKeyProvider = components.ApiKeyProvider;
        result.IdentifierNormalizer = components.IdentifierNormalizer;
        result.PatentClient = new ConfiguredPatentClient(components, patentsView);
        result.PetitionClient = new ConfiguredPetitionClient(components);
        result.PtabClient = new ConfiguredPtabClient(components);
        result.OfficeActionClient = new ConfiguredOfficeActionClient(components);
        result.TrademarkClient = new ConfiguredTrademarkClient(components);
        result.BulkCatalogClient = new ConfiguredBulkCatalogClient(components);
        result.BulkTransferClient = new ConfiguredBulkTransferClient(components);
        result.BulkDataReader = new ConfiguredBulkDataReader(components);
        result.DatasetMirror = new ConfiguredDatasetMirror(components);
        result.LocalDatasetQuery = new ConfiguredLocalDatasetQuery(components);
        result.PatentsViewClient = patentsView;
        result.Capabilities = new ConfiguredUsptoCapabilities(components);
        DataFallbackClient fallback = new DataFallbackClient(options.Fallback);
        fallback.Coordinator = components.Fallback;
        result.DataFallbackClient = fallback;
        result.CitationGraphInputBuilder = new CitationGraphInputBuilder();
        result.Lifetime = components.Lifetime;
        return result;
    }

    internal static void ValidateFactoryOptions(UsptoClientOptions options)
    {
        if (options == null)
        {
            throw new ValidationException("Client options are required.", "options", "REQUIRED", null);
        }

        if (!IsValidEndpoint(options.OdpBaseUri) || !IsValidEndpoint(options.OfficeActionBaseUri) || !IsValidEndpoint(options.TsdrBaseUri) || !IsValidEndpoint(options.PatentsViewBaseUri))
        {
            throw new ValidationException("Every configured service endpoint must be an absolute HTTP URI.", "options", "INVALID_BASE_URI", null);
        }

        if (options.Request == null || options.Retry == null || options.RateLimits == null || options.Cache == null || options.Fallback == null || options.Download == null || options.Extraction == null || options.Mirror == null || options.Limits == null || options.PermittedDownloadHosts == null)
        {
            throw new ValidationException("Client policy objects are required.", "options", "REQUIRED", null);
        }

        DataFallbackCoordinator.ValidatePolicy(options.Fallback);

        if (options.ConnectTimeoutMilliseconds <= 0 || options.HeaderTimeoutMilliseconds <= 0 || options.BodyTimeoutMilliseconds <= 0 || options.OverallTimeoutMilliseconds <= 0)
        {
            throw new ValidationException("Client timeouts must be positive.", "options", "INVALID_TIMEOUT", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(options.UserAgent) || options.UserAgent.IndexOf('\r') >= 0 || options.UserAgent.IndexOf('\n') >= 0)
        {
            throw new ValidationException("A valid client user agent is required.", "UserAgent", "INVALID_HEADER_VALUE", null);
        }
    }

    internal static bool IsValidEndpoint(global::System.Uri endpoint)
    {
        if (endpoint == null || !endpoint.IsAbsoluteUri || endpoint.UserInfo.Length != 0 || endpoint.Fragment.Length != 0)
        {
            return false;
        }

        return global::System.String.Equals(endpoint.Scheme, global::System.Uri.UriSchemeHttps, global::System.StringComparison.OrdinalIgnoreCase) || global::System.String.Equals(endpoint.Scheme, global::System.Uri.UriSchemeHttp, global::System.StringComparison.OrdinalIgnoreCase);
    }
}
