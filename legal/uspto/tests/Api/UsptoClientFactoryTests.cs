using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class UsptoClientFactoryTests
{
    [Test]
    public void FactoryCreatesCompleteSuiteWithOneSharedComponentGraph()
    {
        using HttpClient transport = new HttpClient(new FactoryTrackingHandler());
        FakeApiKeyProvider provider = new FakeApiKeyProvider("rotating-key");
        UsptoClientOptions options = Options(transport, provider);

        UsptoClients clients = new UsptoClientFactory().CreateUsptoClients(options);
        ConfiguredClientComponents components = ((ConfiguredPatentClient)clients.PatentClient!).Components;

        Assert.Multiple(() =>
        {
            Assert.That(clients.Options, Is.SameAs(options));
            Assert.That(clients.ApiKeyProvider, Is.SameAs(provider));
            Assert.That(options.ApiKeyProvider, Is.SameAs(provider));
            Assert.That(options.HttpClient, Is.SameAs(transport));
            Assert.That(clients.IdentifierNormalizer, Is.TypeOf<IdentifierNormalizer>());
            Assert.That(clients.PatentClient, Is.Not.Null);
            Assert.That(clients.PetitionClient, Is.Not.Null);
            Assert.That(clients.PtabClient, Is.Not.Null);
            Assert.That(clients.OfficeActionClient, Is.Not.Null);
            Assert.That(clients.TrademarkClient, Is.Not.Null);
            Assert.That(clients.BulkCatalogClient, Is.Not.Null);
            Assert.That(clients.BulkTransferClient, Is.Not.Null);
            Assert.That(clients.BulkDataReader, Is.Not.Null);
            Assert.That(clients.DatasetMirror, Is.Not.Null);
            Assert.That(clients.LocalDatasetQuery, Is.Not.Null);
            Assert.That(clients.PatentsViewClient, Is.Not.Null);
            Assert.That(clients.Capabilities, Is.Not.Null);
            Assert.That(clients.DataFallbackClient, Is.TypeOf<DataFallbackClient>());
            Assert.That(clients.CitationGraphInputBuilder, Is.TypeOf<CitationGraphInputBuilder>());
            Assert.That(clients.Lifetime, Is.Not.Null);
            Assert.That(((ConfiguredPetitionClient)clients.PetitionClient!).Components, Is.SameAs(components));
            Assert.That(((ConfiguredPtabClient)clients.PtabClient!).Components, Is.SameAs(components));
            Assert.That(((ConfiguredOfficeActionClient)clients.OfficeActionClient!).Components, Is.SameAs(components));
            Assert.That(((ConfiguredTrademarkClient)clients.TrademarkClient!).Components, Is.SameAs(components));
            Assert.That(((ConfiguredBulkCatalogClient)clients.BulkCatalogClient!).Components, Is.SameAs(components));
            Assert.That(((ConfiguredBulkTransferClient)clients.BulkTransferClient!).Components, Is.SameAs(components));
            Assert.That(((ConfiguredBulkDataReader)clients.BulkDataReader!).Components, Is.SameAs(components));
            Assert.That(((ConfiguredDatasetMirror)clients.DatasetMirror!).Components, Is.SameAs(components));
            Assert.That(((ConfiguredLocalDatasetQuery)clients.LocalDatasetQuery!).Components, Is.SameAs(components));
            Assert.That(((ConfiguredPatentsViewClient)clients.PatentsViewClient!).Components, Is.SameAs(components));
            Assert.That(((ConfiguredUsptoCapabilities)clients.Capabilities!).Components, Is.SameAs(components));
            Assert.That(components.Fallback.Policy, Is.SameAs(options.Fallback));
            Assert.That(((DataFallbackClient)clients.DataFallbackClient!).Coordinator, Is.SameAs(components.Fallback));
        });

        ConfiguredPatentsViewClient patentsView = (ConfiguredPatentsViewClient)clients.PatentsViewClient!;
        ConfiguredBulkTransferClient bulk = (ConfiguredBulkTransferClient)clients.BulkTransferClient!;
        ConfiguredDatasetMirror mirror = (ConfiguredDatasetMirror)clients.DatasetMirror!;
        BulkMirrorFileTransfer mirrorTransfer = (BulkMirrorFileTransfer)mirror.Synchronizer.Transfer;
        Assert.Multiple(() =>
        {
            Assert.That(patentsView.Patents.Client, Is.SameAs(transport));
            Assert.That(patentsView.Entities.Transport, Is.SameAs(patentsView.Patents));
            Assert.That(patentsView.Classifications.Transport, Is.SameAs(patentsView.Patents));
            Assert.That(patentsView.Citations.Transport, Is.SameAs(patentsView.Patents));
            Assert.That(patentsView.Text.Transport, Is.SameAs(patentsView.Patents));
            Assert.That(patentsView.Patents.RateLimiter, Is.SameAs(components.RateLimiter));
            Assert.That(patentsView.Patents.RetryEngine, Is.SameAs(components.RetryEngine));
            Assert.That(patentsView.Patents.MemoryCache, Is.SameAs(components.MemoryCache));
            Assert.That(patentsView.Patents.FileCache, Is.SameAs(components.FileCache));
            Assert.That(patentsView.Capability.RateLimiter, Is.SameAs(components.RateLimiter));
            Assert.That(bulk.Resume.RateLimiter, Is.SameAs(components.RateLimiter));
            Assert.That(bulk.Resume.RetryEngine, Is.SameAs(components.RetryEngine));
            Assert.That(mirrorTransfer.Operation.RateLimiter, Is.SameAs(components.RateLimiter));
            Assert.That(mirrorTransfer.Operation.RetryEngine, Is.SameAs(components.RetryEngine));
        });

        clients.Lifetime!.Dispose();
    }

    [Test]
    public void ConfiguredCatalogUsesSharedProviderAndTransportAndBulkReaderIsOperational()
    {
        FactoryTrackingHandler handler = new FactoryTrackingHandler();
        using HttpClient transport = new HttpClient(handler);
        UsptoClients clients = new UsptoClientFactory().CreateUsptoClients(Options(transport, new FakeApiKeyProvider("provider-key")));

        BulkProductResponse catalog = clients.BulkCatalogClient!.SearchBulkProducts(new SearchRequest());
        using MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes("[{\"id\":1},{\"id\":2}]"));
        string[] records = clients.BulkDataReader!.ReadBulkJsonRecords(input, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(catalog.Products, Is.Empty);
            Assert.That(catalog.Count, Is.Zero);
            Assert.That(handler.CallCount, Is.EqualTo(1));
            Assert.That(handler.ApiKeys.Single(), Is.EqualTo("provider-key"));
            Assert.That(records, Is.EqualTo(new[] { "{\"id\":1}", "{\"id\":2}" }));
            Assert.That(clients.Capabilities!.GetSupportedDatasetFamilies(), Does.Contain(DatasetFamily.PatentsViewGrant));
            Assert.That(clients.Capabilities.GetSupportedFallbackAccessPaths(), Is.EqualTo(new[] { FallbackAccess.Online, FallbackAccess.Cache, FallbackAccess.Local }));
            Assert.That(clients.Capabilities.GetSupportedFallbackAuthorities(), Is.EqualTo(new[] { FallbackAuthority.AuthoritativeOnly, FallbackAuthority.ResearchOnly, FallbackAuthority.Either }));
        });

        clients.Lifetime!.Dispose();
    }

    [Test]
    public void CallerOwnedTransportRemainsUsableAfterSuiteLifetimeEnds()
    {
        FactoryTrackingHandler handler = new FactoryTrackingHandler();
        using HttpClient transport = new HttpClient(handler);
        UsptoClientOptions options = Options(transport, new FakeApiKeyProvider("key"));
        options.DisposeHttpClient = false;
        UsptoClients clients = new UsptoClientFactory().CreateUsptoClients(options);

        clients.Lifetime!.Dispose();
        using HttpResponseMessage response = transport.GetAsync("https://example.test/after-dispose").GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(handler.DisposeCount, Is.Zero);
        });
    }

    [Test]
    public void TransferredTransportOwnershipDisposesExactlyOnce()
    {
        FactoryTrackingHandler handler = new FactoryTrackingHandler();
        HttpClient transport = new HttpClient(handler);
        UsptoClientOptions options = Options(transport, new FakeApiKeyProvider("key"));
        options.DisposeHttpClient = true;
        UsptoClients clients = new UsptoClientFactory().CreateUsptoClients(options);

        clients.Lifetime!.Dispose();
        clients.Lifetime.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(handler.DisposeCount, Is.EqualTo(1));
            Assert.Throws<ObjectDisposedException>(() => transport.GetAsync("https://example.test/disposed").GetAwaiter().GetResult());
        });
    }

    [Test]
    public void FactoryOwnedTransportIsExposedForReuseAndDisposedWithTheSuite()
    {
        UsptoClientOptions options = new UsptoClientOptions();
        options.ApiKeyProvider = new FakeApiKeyProvider("key");
        UsptoClients clients = new UsptoClientFactory().CreateUsptoClients(options);
        HttpClient created = options.HttpClient!;

        clients.Lifetime!.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(created, Is.Not.Null);
            Assert.Throws<ObjectDisposedException>(() => created.GetAsync("https://example.test/disposed").GetAwaiter().GetResult());
        });
    }

    [Test]
    public void InvalidOptionsFailBeforeTransportOrProviderMutation()
    {
        UsptoClientOptions options = new UsptoClientOptions();
        options.HeaderTimeoutMilliseconds = 0;

        ValidationException failure = Assert.Throws<ValidationException>(() => new UsptoClientFactory().CreateUsptoClients(options))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.ErrorCode, Is.EqualTo("INVALID_TIMEOUT"));
            Assert.That(options.HttpClient, Is.Null);
            Assert.That(options.ApiKeyProvider, Is.Null);
        });
    }

    [TestCase("null-options", "REQUIRED")]
    [TestCase("odp-endpoint", "INVALID_BASE_URI")]
    [TestCase("office-action-endpoint", "INVALID_BASE_URI")]
    [TestCase("tsdr-endpoint", "INVALID_BASE_URI")]
    [TestCase("patents-view-endpoint", "INVALID_BASE_URI")]
    [TestCase("relative-endpoint", "INVALID_BASE_URI")]
    [TestCase("ftp-endpoint", "INVALID_BASE_URI")]
    [TestCase("endpoint-user-info", "INVALID_BASE_URI")]
    [TestCase("endpoint-fragment", "INVALID_BASE_URI")]
    [TestCase("request", "REQUIRED")]
    [TestCase("retry", "REQUIRED")]
    [TestCase("rate-limits", "REQUIRED")]
    [TestCase("cache", "REQUIRED")]
    [TestCase("fallback", "REQUIRED")]
    [TestCase("download", "REQUIRED")]
    [TestCase("extraction", "REQUIRED")]
    [TestCase("mirror", "REQUIRED")]
    [TestCase("limits", "REQUIRED")]
    [TestCase("permitted-hosts", "REQUIRED")]
    [TestCase("connect-timeout", "INVALID_TIMEOUT")]
    [TestCase("header-timeout", "INVALID_TIMEOUT")]
    [TestCase("body-timeout", "INVALID_TIMEOUT")]
    [TestCase("overall-timeout", "INVALID_TIMEOUT")]
    [TestCase("empty-user-agent", "INVALID_HEADER_VALUE")]
    [TestCase("blank-user-agent", "INVALID_HEADER_VALUE")]
    [TestCase("carriage-return-user-agent", "INVALID_HEADER_VALUE")]
    [TestCase("line-feed-user-agent", "INVALID_HEADER_VALUE")]
    public void FactoryRejectsInvalidTopLevelOptionsBeforeCreatingDefaults(string scenario, string errorCode)
    {
        UsptoClientOptions options = new UsptoClientOptions();
        ApplyInvalidTopLevelOption(options, scenario);
        if (scenario == "null-options")
        {
            options = null!;
        }

        ValidationException failure = Assert.Throws<ValidationException>(() => new UsptoClientFactory().CreateUsptoClients(options))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.ErrorCode, Is.EqualTo(errorCode));
            if (scenario != "null-options")
            {
                Assert.That(options.HttpClient, Is.Null);
                Assert.That(options.ApiKeyProvider, Is.Null);
            }
        });
    }

    [TestCase("rate-null-entry", "REQUIRED")]
    [TestCase("rate-service", "REQUIRED")]
    [TestCase("rate-allowance", "INVALID_RATE_LIMIT")]
    [TestCase("rate-window", "INVALID_RATE_LIMIT")]
    [TestCase("rate-queue", "INVALID_RATE_LIMIT")]
    [TestCase("rate-duplicate", "DUPLICATE_RATE_LIMIT")]
    [TestCase("retry-attempts", "INVALID_RETRY_POLICY")]
    [TestCase("retry-initial-delay", "INVALID_RETRY_POLICY")]
    [TestCase("retry-maximum-delay", "INVALID_RETRY_POLICY")]
    [TestCase("retry-budget", "INVALID_RETRY_POLICY")]
    [TestCase("cache-lifetime", "INVALID_CACHE_POLICY")]
    [TestCase("cache-memory-entries", "INVALID_CACHE_POLICY")]
    [TestCase("cache-entry-bytes", "INVALID_CACHE_POLICY")]
    [TestCase("cache-file-bytes", "INVALID_CACHE_POLICY")]
    [TestCase("cache-directory", "REQUIRED")]
    [TestCase("fallback-order", "DUPLICATE_FALLBACK_ACCESS")]
    public void FactoryRejectsInvalidNestedPoliciesBeforeCreatingDefaults(string scenario, string errorCode)
    {
        UsptoClientOptions options = new UsptoClientOptions();
        ApplyInvalidNestedPolicy(options, scenario);

        ValidationException failure = Assert.Throws<ValidationException>(() => new UsptoClientFactory().CreateUsptoClients(options))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(options.HttpClient, Is.Null);
            Assert.That(options.ApiKeyProvider, Is.Null);
        });
    }

    private static UsptoClientOptions Options(HttpClient transport, IApiKeyProvider provider)
    {
        return new UsptoClientOptions
        {
            HttpClient = transport,
            ApiKeyProvider = provider,
            OdpBaseUri = new Uri("https://example.test/root/"),
            OfficeActionBaseUri = new Uri("https://example.test/root/"),
            TsdrBaseUri = new Uri("https://example.test/tsdr/"),
            PatentsViewBaseUri = new Uri("https://example.test/patentsview/")
        };
    }

    private static void ApplyInvalidTopLevelOption(UsptoClientOptions options, string scenario)
    {
        switch (scenario)
        {
            case "null-options":
                break;
            case "odp-endpoint":
                options.OdpBaseUri = null!;
                break;
            case "office-action-endpoint":
                options.OfficeActionBaseUri = null!;
                break;
            case "tsdr-endpoint":
                options.TsdrBaseUri = null!;
                break;
            case "patents-view-endpoint":
                options.PatentsViewBaseUri = null!;
                break;
            case "relative-endpoint":
                options.OdpBaseUri = new Uri("relative", UriKind.Relative);
                break;
            case "ftp-endpoint":
                options.OdpBaseUri = new Uri("ftp://example.test/");
                break;
            case "endpoint-user-info":
                options.OdpBaseUri = new Uri("https://user@example.test/");
                break;
            case "endpoint-fragment":
                options.OdpBaseUri = new Uri("https://example.test/#fragment");
                break;
            case "request":
                options.Request = null!;
                break;
            case "retry":
                options.Retry = null!;
                break;
            case "rate-limits":
                options.RateLimits = null!;
                break;
            case "cache":
                options.Cache = null!;
                break;
            case "fallback":
                options.Fallback = null!;
                break;
            case "download":
                options.Download = null!;
                break;
            case "extraction":
                options.Extraction = null!;
                break;
            case "mirror":
                options.Mirror = null!;
                break;
            case "limits":
                options.Limits = null!;
                break;
            case "permitted-hosts":
                options.PermittedDownloadHosts = null!;
                break;
            case "connect-timeout":
                options.ConnectTimeoutMilliseconds = 0;
                break;
            case "header-timeout":
                options.HeaderTimeoutMilliseconds = 0;
                break;
            case "body-timeout":
                options.BodyTimeoutMilliseconds = 0;
                break;
            case "overall-timeout":
                options.OverallTimeoutMilliseconds = 0;
                break;
            case "empty-user-agent":
                options.UserAgent = string.Empty;
                break;
            case "blank-user-agent":
                options.UserAgent = "   ";
                break;
            case "carriage-return-user-agent":
                options.UserAgent = "uspto\rtest";
                break;
            case "line-feed-user-agent":
                options.UserAgent = "uspto\ntest";
                break;
            default:
                Assert.Fail("Unknown top-level option scenario: " + scenario);
                break;
        }
    }

    private static void ApplyInvalidNestedPolicy(UsptoClientOptions options, string scenario)
    {
        switch (scenario)
        {
            case "rate-null-entry":
                options.RateLimits = new RateLimitPolicy[] { null! };
                break;
            case "rate-service":
                options.RateLimits = new[] { new RateLimitPolicy { Service = ApiService.Unknown, RequestsPerWindow = 1 } };
                break;
            case "rate-allowance":
                options.RateLimits[0].RequestsPerWindow = 0;
                break;
            case "rate-window":
                options.RateLimits[0].WindowMilliseconds = 0;
                break;
            case "rate-queue":
                options.RateLimits[0].HasQueueCapacity = true;
                options.RateLimits[0].QueueCapacity = -1;
                break;
            case "rate-duplicate":
                options.RateLimits = new[]
                {
                    new RateLimitPolicy { Service = ApiService.Tsdr, RequestsPerWindow = 1 },
                    new RateLimitPolicy { Service = ApiService.Tsdr, RequestsPerWindow = 1 }
                };
                break;
            case "retry-attempts":
                options.Retry.AttemptCount = 0;
                break;
            case "retry-initial-delay":
                options.Retry.InitialDelayMilliseconds = -1;
                break;
            case "retry-maximum-delay":
                options.Retry.MaximumDelayMilliseconds = options.Retry.InitialDelayMilliseconds - 1;
                break;
            case "retry-budget":
                options.Retry.BudgetMilliseconds = -1;
                break;
            case "cache-lifetime":
                options.Cache.LifetimeSeconds = -1;
                break;
            case "cache-memory-entries":
                options.Cache.MaximumMemoryEntries = -1;
                break;
            case "cache-entry-bytes":
                options.Cache.MaximumEntryBytes = -1L;
                break;
            case "cache-file-bytes":
                options.Cache.HasMaximumFileBytes = true;
                options.Cache.MaximumFileBytes = -1L;
                break;
            case "cache-directory":
                options.Cache.Mode = CacheMode.File;
                options.Cache.Directory = null;
                break;
            case "fallback-order":
                options.Fallback.AccessOrder = new[] { FallbackAccess.Online, FallbackAccess.Online };
                break;
            default:
                Assert.Fail("Unknown nested-policy scenario: " + scenario);
                break;
        }
    }
}

internal class FactoryTrackingHandler : HttpMessageHandler
{
    internal int CallCount = 0;
    internal int DisposeCount = 0;
    internal List<string?> ApiKeys = new List<string?>();

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount += 1;
        string? key = null;
        IEnumerable<string>? values;
        if (request.Headers.TryGetValues(UsptoHeaders.OdpApiKey, out values) && values != null)
        {
            key = values.Single();
        }

        ApiKeys.Add(key);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"count\":0,\"bulkDataProductBag\":[]}", Encoding.UTF8, UsptoMediaTypes.Json)
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Send(request, cancellationToken));
    }

    protected override void Dispose(bool disposing)
    {
        DisposeCount += 1;
        base.Dispose(disposing);
    }
}
