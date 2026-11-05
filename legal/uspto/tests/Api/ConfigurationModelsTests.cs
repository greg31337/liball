using uspto;

namespace uspto.Tests;

public class ConfigurationModelsTests
{
    [Test]
    public void ClientOptionsUseConservativeServiceDefaults()
    {
        UsptoClientOptions options = new UsptoClientOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.OdpBaseUri.AbsoluteUri, Is.EqualTo(UsptoEndpoints.OdpBaseUri));
            Assert.That(options.OfficeActionBaseUri.AbsoluteUri, Is.EqualTo(UsptoEndpoints.OdpBaseUri));
            Assert.That(options.TsdrBaseUri.AbsoluteUri, Is.EqualTo(UsptoEndpoints.TsdrBaseUri));
            Assert.That(options.PatentsViewBaseUri.AbsoluteUri, Is.EqualTo(UsptoEndpoints.PatentsViewBaseUri));
            Assert.That(options.ApiKeyProvider, Is.Null);
            Assert.That(options.OdpApiKey, Is.Null);
            Assert.That(options.TsdrApiKey, Is.Null);
            Assert.That(options.PatentsViewApiKey, Is.Null);
            Assert.That(options.HttpClient, Is.Null);
            Assert.That(options.DisposeHttpClient, Is.False);
            Assert.That(options.PermitInsecureTestEndpoints, Is.False);
            Assert.That(options.Cache.Mode, Is.EqualTo(CacheMode.Disabled));
            Assert.That(options.Fallback.AccessOrder, Is.EqualTo(new[] { FallbackAccess.Online, FallbackAccess.Cache, FallbackAccess.Local }));
            Assert.That(options.Fallback.Authority, Is.EqualTo(FallbackAuthority.AuthoritativeOnly));
            Assert.That(options.Fallback.HasMaximumCacheAge, Is.True);
            Assert.That(options.Fallback.HasMaximumLocalAge, Is.True);
            Assert.That(options.Mirror.DryRun, Is.True);
            Assert.That(options.Mirror.VerifyChecksums, Is.True);
            Assert.That(options.Download.AllowResume, Is.True);
            Assert.That(options.Extraction.RejectDuplicateNames, Is.True);
        });
    }

    [Test]
    public void MutableDefaultsAreIndependentForEveryOptionsInstance()
    {
        UsptoClientOptions first = new UsptoClientOptions();
        UsptoClientOptions second = new UsptoClientOptions();

        first.Retry.AttemptCount = 99;
        first.RateLimits[0].RequestsPerWindow = 1;
        first.Mirror.DatasetFamilies = new DatasetFamily[] { DatasetFamily.OfficeAction };
        first.Fallback.AccessOrder[0] = FallbackAccess.Local;
        first.PermittedDownloadHosts = new string[] { "downloads.example.test" };

        Assert.Multiple(() =>
        {
            Assert.That(first.Retry, Is.Not.SameAs(second.Retry));
            Assert.That(first.RateLimits, Is.Not.SameAs(second.RateLimits));
            Assert.That(first.RateLimits[0], Is.Not.SameAs(second.RateLimits[0]));
            Assert.That(second.Retry.AttemptCount, Is.EqualTo(UsptoDefaults.HttpAttemptCount));
            Assert.That(second.RateLimits[0].RequestsPerWindow, Is.EqualTo(UsptoDefaults.TsdrGeneralRequestsPerMinute));
            Assert.That(second.Mirror.DatasetFamilies, Is.Empty);
            Assert.That(second.Fallback.AccessOrder[0], Is.EqualTo(FallbackAccess.Online));
            Assert.That(second.PermittedDownloadHosts, Is.Empty);
        });
    }

    [Test]
    public void DefaultRateBucketsMatchPublishedIndependentLimits()
    {
        RateLimitPolicy[] policies = new UsptoClientOptions().RateLimits;

        Assert.Multiple(() =>
        {
            Assert.That(policies, Has.Length.EqualTo(3));
            Assert.That(policies[0].Service, Is.EqualTo(ApiService.Tsdr));
            Assert.That(policies[0].RequestsPerWindow, Is.EqualTo(60));
            Assert.That(policies[1].Service, Is.EqualTo(ApiService.TsdrPdfAndZip));
            Assert.That(policies[1].RequestsPerWindow, Is.EqualTo(4));
            Assert.That(policies[2].Service, Is.EqualTo(ApiService.PatentsView));
            Assert.That(policies[2].RequestsPerWindow, Is.EqualTo(45));
            Assert.That(policies[0].WindowMilliseconds, Is.EqualTo(60000));
            Assert.That(policies[1].WindowMilliseconds, Is.EqualTo(60000));
            Assert.That(policies[2].WindowMilliseconds, Is.EqualTo(60000));
        });
    }

    [Test]
    public void OptionalValueTypesStartAbsentWithDefaultValues()
    {
        ApiCredentialResult credential = new ApiCredentialResult();
        RequestContext request = new RequestContext();
        RateLimitPolicy rate = new RateLimitPolicy();
        CachePolicy cache = new CachePolicy();
        DownloadPolicy download = new DownloadPolicy();
        MirrorPolicy mirror = new MirrorPolicy();

        Assert.Multiple(() =>
        {
            Assert.That(credential.HasExpiresAt, Is.False);
            Assert.That(credential.ExpiresAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(request.HasDeadline, Is.False);
            Assert.That(request.Deadline, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(request.Fallback, Is.Null);
            Assert.That(rate.HasQueueCapacity, Is.False);
            Assert.That(rate.QueueCapacity, Is.Zero);
            Assert.That(cache.HasMaximumFileBytes, Is.False);
            Assert.That(cache.MaximumFileBytes, Is.Zero);
            Assert.That(download.HasMaximumBytes, Is.False);
            Assert.That(download.MaximumBytes, Is.Zero);
            Assert.That(mirror.HasHighWaterMark, Is.False);
            Assert.That(mirror.HighWaterMark, Is.EqualTo(default(DateTimeOffset)));
        });
    }

    [Test]
    public void SafetyLimitsArePositiveAndInternallyConsistent()
    {
        UsptoClientOptions options = new UsptoClientOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Request.MaximumRawResponseBytes, Is.Positive);
            Assert.That(options.Limits.MaximumRequestBytes, Is.Positive);
            Assert.That(options.Limits.MaximumResponseBytes, Is.Positive);
            Assert.That(options.Limits.MaximumRawResponseBytes, Is.LessThanOrEqualTo(options.Limits.MaximumResponseBytes));
            Assert.That(options.Limits.MaximumPages, Is.Positive);
            Assert.That(options.Limits.MaximumResults, Is.Positive);
            Assert.That(options.Download.BufferSize, Is.Positive);
            Assert.That(options.Download.TimeoutMilliseconds, Is.Positive);
            Assert.That(options.ConnectTimeoutMilliseconds, Is.Positive);
            Assert.That(options.HeaderTimeoutMilliseconds, Is.Positive);
            Assert.That(options.BodyTimeoutMilliseconds, Is.Positive);
            Assert.That(options.OverallTimeoutMilliseconds, Is.Positive);
            Assert.That(options.Extraction.MaximumEntries, Is.Positive);
            Assert.That(options.Extraction.MaximumEntryBytes, Is.LessThanOrEqualTo(options.Extraction.MaximumTotalBytes));
            Assert.That(options.Retry.AttemptCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(options.Retry.InitialDelayMilliseconds, Is.LessThanOrEqualTo(options.Retry.MaximumDelayMilliseconds));
            Assert.That(options.Retry.MaximumDelayMilliseconds, Is.LessThanOrEqualTo(options.Retry.BudgetMilliseconds));
        });
    }

    [Test]
    public void CredentialModelsRetainServiceAndNonSecretSourceMetadata()
    {
        ApiCredentialRequest request = new ApiCredentialRequest
        {
            Service = ApiService.PatentsView,
            AllowAnonymous = false
        };
        ApiCredentialResult result = new ApiCredentialResult
        {
            Request = request,
            ApiKey = "test-key",
            Source = UsptoEnvironmentVariables.PatentsViewApiKey,
            IsAvailable = true,
            ExpiresAt = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero),
            HasExpiresAt = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Request, Is.SameAs(request));
            Assert.That(result.Request.Service, Is.EqualTo(ApiService.PatentsView));
            Assert.That(result.Source, Is.EqualTo("PATENTSVIEW_API_KEY"));
            Assert.That(result.IsAvailable, Is.True);
            Assert.That(result.HasExpiresAt, Is.True);
            Assert.That(result.ExpiresAt.Year, Is.EqualTo(2030));
        });
    }
}
