using uspto;

namespace uspto.Tests;

[Category("LiveIntegration")]
[NonParallelizable]
public class LiveUsptoIntegrationTests
{
    private const string OptInVariable = "USPTO_RUN_LIVE_INTEGRATION_TESTS";

    [Test]
    public void OdpPatentSearchReturnsOneBoundedAuthoritativePage()
    {
        RequireLiveService(UsptoEnvironmentVariables.OdpApiKey, "ODP Patent");
        UsptoClients suite = CreateLiveSuite();
        using IDisposable lifetime = suite.Lifetime!;

        PatentApplicationResponse result = RunOrSkipUnavailable(
            "ODP Patent",
            () => suite.PatentClient!.SearchPatentApplications(OnePageRequest(ApiService.Odp)));

        Assert.Multiple(() =>
        {
            Assert.That(result.Summaries, Has.Length.LessThanOrEqualTo(1));
            Assert.That(result.Count, Is.EqualTo(result.Summaries.Length));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.RawResponse, Is.Null);
        });
    }

    [Test]
    public void OdpPetitionSearchReturnsOneBoundedAuthoritativePage()
    {
        RequireLiveService(UsptoEnvironmentVariables.OdpApiKey, "ODP Petition");
        UsptoClients suite = CreateLiveSuite();
        using IDisposable lifetime = suite.Lifetime!;

        PetitionDecisionResponse result = RunOrSkipUnavailable(
            "ODP Petition",
            () => suite.PetitionClient!.SearchPetitionDecisions(OnePageRequest(ApiService.Odp)));

        Assert.Multiple(() =>
        {
            Assert.That(result.Decisions, Has.Length.LessThanOrEqualTo(1));
            Assert.That(result.Count, Is.EqualTo(result.Decisions.Length));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.RawResponse, Is.Null);
        });
    }

    [Test]
    public void OdpPtabSearchReturnsOneBoundedAuthoritativePage()
    {
        RequireLiveService(UsptoEnvironmentVariables.OdpApiKey, "ODP PTAB");
        UsptoClients suite = CreateLiveSuite();
        using IDisposable lifetime = suite.Lifetime!;

        PtabTrialProceedingResponse result = RunOrSkipUnavailable(
            "ODP PTAB",
            () => suite.PtabClient!.SearchPtabProceedings(OnePageRequest(ApiService.Odp)));

        Assert.Multiple(() =>
        {
            Assert.That(result.Proceedings, Has.Length.LessThanOrEqualTo(1));
            Assert.That(result.Count, Is.EqualTo(result.Proceedings.Length));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.RawResponse, Is.Null);
        });
    }

    [Test]
    public void OdpOfficeActionFieldDiscoveryReturnsPublishedMetadata()
    {
        RequireLiveService(UsptoEnvironmentVariables.OdpApiKey, "ODP Office Action");
        UsptoClients suite = CreateLiveSuite();
        using IDisposable lifetime = suite.Lifetime!;

        OfficeActionFieldResponse result = RunOrSkipUnavailable(
            "ODP Office Action",
            () => suite.OfficeActionClient!.DiscoverOfficeActionFields("oa_actions", LiveContext(ApiService.OfficeAction)));

        Assert.Multiple(() =>
        {
            Assert.That(result.Metadata.DatasetName, Is.EqualTo("oa_actions"));
            Assert.That(result.Count, Is.EqualTo(result.Fields.Length));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.RawResponse, Is.Null);
            Assert.That(result.IsComplete, Is.True);
        });
    }

    [Test]
    public void OdpBulkCatalogReturnsOneBoundedCredentialFreePage()
    {
        RequireLiveService(UsptoEnvironmentVariables.OdpApiKey, "ODP Bulk");
        UsptoClients suite = CreateLiveSuite();
        using IDisposable lifetime = suite.Lifetime!;

        BulkProductResponse result = RunOrSkipUnavailable(
            "ODP Bulk",
            () => suite.BulkCatalogClient!.SearchBulkProducts(OnePageRequest(ApiService.Odp)));

        Assert.Multiple(() =>
        {
            Assert.That(result.Products, Has.Length.LessThanOrEqualTo(1));
            Assert.That(result.ReturnedCount, Is.EqualTo(result.Products.Length));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Odp));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.RawResponse, Is.Null);
        });

        Assert.That(CatalogUrisExcludeConfiguredCredential(result.Products), Is.True);
    }

    [Test]
    public void TsdrStatusReadsDocumentedSampleWithoutRetainingRawContent()
    {
        RequireLiveService(UsptoEnvironmentVariables.TsdrApiKey, "TSDR");
        UsptoClients suite = CreateLiveSuite();
        using IDisposable lifetime = suite.Lifetime!;

        TrademarkCaseStatus result = RunOrSkipUnavailable(
            "TSDR",
            () => suite.TrademarkClient!.GetTrademarkStatus("SN 78/787,878", DocumentFormat.Xml, LiveContext(ApiService.Tsdr)));

        Assert.Multiple(() =>
        {
            Assert.That(result.CanonicalIdentifier, Is.EqualTo("sn78787878"));
            Assert.That(result.RawFormat, Is.EqualTo(DocumentFormat.Xml));
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.Tsdr));
            Assert.That(result.Provenance.IsAuthoritative, Is.True);
            Assert.That(result.RawResponse, Is.Null);
            Assert.That(result.IsComplete, Is.True);
        });
    }

    [Test]
    public void PatentsViewCapabilityGatesOneBoundedPatentQuery()
    {
        RequireLiveService(UsptoEnvironmentVariables.PatentsViewApiKey, "PatentsView");
        UsptoClients suite = CreateLiveSuite();
        using IDisposable lifetime = suite.Lifetime!;
        IPatentsViewClient client = suite.PatentsViewClient!;

        PatentsViewCapabilityResult capability = RunOrSkipUnavailable(
            "PatentsView capability probe",
            () => client.ProbePatentsViewCapability(LiveContext(ApiService.PatentsView)));
        if (!capability.IsAvailable || !capability.SupportsOnlineSearch)
        {
            Assert.Ignore("PatentsView online search is currently unavailable: " + (capability.UnavailabilityCode ?? "CAPABILITY_UNAVAILABLE"));
        }

        PatentsViewQueryRequest request = new PatentsViewQueryRequest();
        request.Search.Filters = new[]
        {
            new SearchFilter
            {
                Field = "patent_id",
                Operator = SearchOperator.Equal,
                Value = "10000000"
            }
        };
        request.Search.CursorPage.Size = 1;
        request.Search.MaximumPages = 1;
        request.Search.MaximumResults = 1;
        request.Search.Context = LiveContext(ApiService.PatentsView);

        PatentsViewPatentResponse result = RunOrSkipUnavailable(
            "PatentsView patent query",
            () => client.QueryPatentsViewPatents(request));

        Assert.Multiple(() =>
        {
            Assert.That(capability.RawResponse, Is.Null);
            Assert.That(capability.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(capability.Provenance.IsAuthoritative, Is.False);
            Assert.That(result.Patents, Has.Length.LessThanOrEqualTo(1));
            Assert.That(result.Metadata.Count, Is.EqualTo(result.Patents.Length));
            Assert.That(result.Metadata.Provenance.Source, Is.EqualTo(DataProvenance.PatentsView));
            Assert.That(result.Metadata.Provenance.IsAuthoritative, Is.False);
            Assert.That(result.Metadata.RawResponse, Is.Null);
        });
    }

    private static void RequireLiveService(string credentialVariable, string serviceName)
    {
        string? optIn = Environment.GetEnvironmentVariable(OptInVariable);
        bool enabled = string.Equals(optIn, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(optIn, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(optIn, "yes", StringComparison.OrdinalIgnoreCase);
        if (!enabled)
        {
            Assert.Ignore(serviceName + " live integration is opt-in; set " + OptInVariable + "=1 to enable it.");
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(credentialVariable)))
        {
            Assert.Ignore(serviceName + " live integration requires the " + credentialVariable + " environment variable.");
        }
    }

    private static UsptoClients CreateLiveSuite()
    {
        UsptoClientOptions options = new UsptoClientOptions();
        options.Cache.Mode = CacheMode.Disabled;
        options.ConnectTimeoutMilliseconds = 15000;
        options.HeaderTimeoutMilliseconds = 30000;
        options.BodyTimeoutMilliseconds = 30000;
        options.OverallTimeoutMilliseconds = 45000;
        options.Retry.AttemptCount = 2;
        options.Retry.BudgetMilliseconds = 5000;
        options.Limits.MaximumResponseBytes = 4L * 1024L * 1024L;
        options.Limits.MaximumRawResponseBytes = 4096L;
        options.Limits.MaximumPages = 1;
        options.Limits.MaximumResults = 1;
        options.Download.MaximumBytes = 1024L * 1024L;
        options.Download.HasMaximumBytes = true;
        return new UsptoClientFactory().CreateUsptoClients(options);
    }

    private static SearchRequest OnePageRequest(ApiService service)
    {
        return new SearchRequest
        {
            OffsetPage = new OffsetPage { Offset = 0L, Limit = 1 },
            MaximumPages = 1,
            MaximumResults = 1,
            Context = LiveContext(service)
        };
    }

    private static RequestContext LiveContext(ApiService service)
    {
        return new RequestContext
        {
            Service = service,
            RetainRawResponse = false,
            CorrelationIdentifier = "live-integration"
        };
    }

    private static T RunOrSkipUnavailable<T>(string serviceName, Func<T> operation)
    {
        try
        {
            return operation();
        }
        catch (ServiceUnavailableException exception)
        {
            string status = exception.HasStatusCode ? exception.StatusCode.ToString() : "unreported";
            Assert.Ignore(serviceName + " is currently unavailable (HTTP " + status + ").");
            throw;
        }
        catch (NotFoundException exception)
        {
            string status = exception.HasStatusCode ? exception.StatusCode.ToString() : "unreported";
            Assert.Ignore(serviceName + " has no currently available live resource (HTTP " + status + ").");
            throw;
        }
        catch (ApiException exception) when (IsCapabilityUnavailable(exception))
        {
            Assert.Ignore(serviceName + " is currently unavailable (HTTP " + exception.StatusCode + ").");
            throw;
        }
    }

    private static bool IsCapabilityUnavailable(ApiException exception)
    {
        return exception.HasStatusCode
            && (exception.StatusCode == 404
                || exception.StatusCode == 410
                || exception.StatusCode == 501
                || exception.StatusCode == 503);
    }

    private static bool CatalogUrisExcludeConfiguredCredential(BulkProduct[] products)
    {
        string credential = Environment.GetEnvironmentVariable(UsptoEnvironmentVariables.OdpApiKey) ?? string.Empty;
        int productIndex;
        for (productIndex = 0; productIndex < products.Length; productIndex += 1)
        {
            int fileIndex;
            for (fileIndex = 0; fileIndex < products[productIndex].Files.Length; fileIndex += 1)
            {
                Uri? uri = products[productIndex].Files[fileIndex].DownloadUri;
                if (uri != null && credential.Length > 0 && uri.AbsoluteUri.Contains(credential, StringComparison.Ordinal))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
