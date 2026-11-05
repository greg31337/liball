using System.Text;

namespace jmap.Tests;

public class ServerModelsTests
{
    [Test]
    public void ServerOptionsUseInteroperableProtocolDefaults()
    {
        JmapServerOptions options = new JmapServerOptions();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.MaxRequestBytes, Is.EqualTo(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest));
            Assert.That(options.MaxUploadBytes, Is.EqualTo(JmapProtocolLimits.SuggestedMinimumMaxSizeUpload));
            Assert.That(options.MaxConcurrentRequests, Is.EqualTo(JmapProtocolLimits.SuggestedMinimumMaxConcurrentRequests));
            Assert.That(options.MaxConcurrentUploads, Is.EqualTo(JmapProtocolLimits.SuggestedMinimumMaxConcurrentUpload));
            Assert.That(options.MaxCallsInRequest, Is.EqualTo(JmapProtocolLimits.SuggestedMinimumMaxCallsInRequest));
            Assert.That(options.MaxObjectsInGet, Is.EqualTo(JmapProtocolLimits.SuggestedMinimumMaxObjectsInGet));
            Assert.That(options.MaxObjectsInSet, Is.EqualTo(JmapProtocolLimits.SuggestedMinimumMaxObjectsInSet));
            Assert.That(options.UnreferencedBlobQuotaBytes, Is.GreaterThanOrEqualTo(options.MaxUploadBytes));
            Assert.That(options.UnreferencedBlobMinimumLifetimeSeconds, Is.EqualTo(JmapProtocolLimits.MinimumUnreferencedBlobLifetimeSeconds));
            Assert.That(options.MinimumEventSourcePingSeconds, Is.LessThanOrEqualTo(JmapProtocolLimits.MaximumEventSourceMinimumPingIntervalSeconds));
            Assert.That(options.MaximumEventSourcePingSeconds, Is.GreaterThanOrEqualTo(JmapProtocolLimits.MinimumEventSourceMaximumPingIntervalSeconds));
            Assert.That(options.MaximumPushSubscriptionLifetimeSeconds, Is.GreaterThanOrEqualTo(JmapProtocolLimits.MinimumPushSubscriptionMaximumLifetimeSeconds));
            Assert.That(options.AllowInsecureHttp, Is.False);
        }
    }

    [Test]
    public void RequestContextPreservesAuthenticationLocalizationAndHostValues()
    {
        JmapRequestContext context = new JmapRequestContext();
        context.UserId = "user-1";
        context.CredentialId = "credential-1";
        context.Authorization = "Bearer redacted-token";
        context.AcceptLanguage = "de;q=0.9,en;q=0.8";
        context.RemoteAddress = "192.0.2.10";
        context.IsSecure = true;
        context.Values.Add("tenant", "example");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.UserId, Is.EqualTo("user-1"));
            Assert.That(context.CredentialId, Is.EqualTo("credential-1"));
            Assert.That(context.Authorization, Does.StartWith("Bearer"));
            Assert.That(context.AcceptLanguage, Does.Contain("de"));
            Assert.That(context.RemoteAddress, Is.EqualTo("192.0.2.10"));
            Assert.That(context.IsSecure, Is.True);
            Assert.That(context.Values["tenant"], Is.EqualTo("example"));
        }
    }

    [Test]
    public void EndpointRequestCarriesRawStreamRoutingAndCaseInsensitiveHeaders()
    {
        MemoryStream body = new MemoryStream(Encoding.UTF8.GetBytes("{\"using\":[],\"methodCalls\":[]}"));
        JmapEndpointRequest request = new JmapEndpointRequest();
        request.Context.UserId = "user-1";
        request.Method = "POST";
        request.Url = "https://jmap.example/api";
        request.ContentType = JmapMediaTypes.Json;
        request.Body = body;
        request.ContentLength = body.Length;
        request.HasContentLength = true;
        request.Headers.Add("Accept-Language", "en-US");
        request.RouteValues.Add(JmapProperties.AccountId, "account-1");
        request.QueryParameters.Add(JmapProperties.Ping, "30");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Body, Is.SameAs(body));
            Assert.That(request.ContentLength, Is.EqualTo(body.Length));
            Assert.That(request.HasContentLength, Is.True);
            Assert.That(request.Headers["accept-language"], Is.EqualTo("en-US"));
            Assert.That(request.RouteValues[JmapProperties.AccountId], Is.EqualTo("account-1"));
            Assert.That(request.QueryParameters[JmapProperties.Ping], Is.EqualTo("30"));
            Assert.That(request.Context.UserId, Is.EqualTo("user-1"));
        }
    }

    [Test]
    public void EndpointResponseDistinguishesBufferedStreamingAndUnknownLengthBodies()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("{\"ok\":true}");
        JmapEndpointResponse buffered = new JmapEndpointResponse();
        buffered.StatusCode = 200;
        buffered.ContentType = JmapMediaTypes.Json;
        buffered.BodyBytes = bytes;
        buffered.ContentLength = bytes.Length;
        buffered.HasContentLength = true;
        buffered.Headers.Add("Content-Language", "en");

        MemoryStream stream = new MemoryStream(new byte[] { 0, 1, 2 });
        JmapEndpointResponse streaming = new JmapEndpointResponse();
        streaming.StatusCode = 200;
        streaming.ContentType = JmapMediaTypes.OctetStream;
        streaming.BodyStream = stream;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(buffered.BodyBytes, Is.SameAs(bytes));
            Assert.That(buffered.BodyStream, Is.Null);
            Assert.That(buffered.HasContentLength, Is.True);
            Assert.That(buffered.Headers["content-language"], Is.EqualTo("en"));
            Assert.That(streaming.BodyStream, Is.SameAs(stream));
            Assert.That(streaming.BodyBytes, Is.Null);
            Assert.That(streaming.ContentLength, Is.Zero);
            Assert.That(streaming.HasContentLength, Is.False);
        }
    }

    [Test]
    public void DownloadContentPreservesStreamSafetyMetadataAndImmutableCacheLifetime()
    {
        MemoryStream content = new MemoryStream(new byte[] { 10, 20, 30, 40 });
        JmapDownloadContent download = new JmapDownloadContent();
        download.Content = content;
        download.MediaType = "application/pdf";
        download.FileName = "report.pdf";
        download.Size = 4;
        download.HasSize = true;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(download.Content, Is.SameAs(content));
            Assert.That(download.MediaType, Is.EqualTo("application/pdf"));
            Assert.That(download.FileName, Is.EqualTo("report.pdf"));
            Assert.That(download.Size, Is.EqualTo(4));
            Assert.That(download.HasSize, Is.True);
            Assert.That(download.CacheLifetimeSeconds, Is.EqualTo(31536000));
        }
    }

    [Test]
    public void StatePublicationCarriesEventIdentityCoalescedStatesAndTypeFilter()
    {
        JmapStatePublication publication = new JmapStatePublication();
        publication.EventId = "event-42";
        publication.Types = new List<string> { JmapDataTypes.Mailbox, JmapDataTypes.Email };
        publication.StateChange.Changed.Add("account-1", new Dictionary<string, string>
        {
            { JmapDataTypes.Mailbox, "mailbox-state" },
            { JmapDataTypes.Email, "email-state" }
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(publication.EventId, Is.EqualTo("event-42"));
            Assert.That(publication.StateChange.Type, Is.EqualTo("StateChange"));
            Assert.That(publication.StateChange.Changed["account-1"][JmapDataTypes.Mailbox], Is.EqualTo("mailbox-state"));
            Assert.That(publication.StateChange.Changed["account-1"][JmapDataTypes.Email], Is.EqualTo("email-state"));
            Assert.That(publication.Types, Is.EqualTo(new[] { JmapDataTypes.Mailbox, JmapDataTypes.Email }));
        }
    }

    [Test]
    public void ServicesBundleRetainsDeterministicPolicyHooks()
    {
        FakeJmapClock clock = new FakeJmapClock();
        StubPushUrlPolicy policy = new StubPushUrlPolicy();
        StubStringLocalizer localizer = new StubStringLocalizer();
        JmapServerServices services = new JmapServerServices();
        services.Clock = clock;
        services.PushUrlPolicy = policy;
        services.StringLocalizer = localizer;

        JmapRequestContext context = new JmapRequestContext();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(services.Clock, Is.SameAs(clock));
            Assert.That(services.PushUrlPolicy, Is.SameAs(policy));
            Assert.That(services.StringLocalizer, Is.SameAs(localizer));
            Assert.That(services.Clock.GetCurrentUtcDateTime(), Is.EqualTo(new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero)));
            Assert.That(services.PushUrlPolicy.IsPushUrlAllowed("https://push.example", context), Is.True);
            Assert.That(services.PushUrlPolicy.IsPushUrlAllowed("http://push.example", context), Is.False);
            Assert.That(services.StringLocalizer.LocalizeJmapString("forbidden", "de"), Is.EqualTo("de:forbidden"));
        }
    }

    [Test]
    public void RequiredServerCollectionsAndNestedModelsAreIndependent()
    {
        JmapEndpointRequest firstRequest = new JmapEndpointRequest();
        JmapEndpointRequest secondRequest = new JmapEndpointRequest();
        firstRequest.Headers.Add("X-Test", "first");
        firstRequest.Context.Values.Add("tenant", "first");

        JmapEndpointResponse firstResponse = new JmapEndpointResponse();
        JmapEndpointResponse secondResponse = new JmapEndpointResponse();
        firstResponse.Headers.Add("X-Test", "first");

        JmapStatePublication firstPublication = new JmapStatePublication();
        JmapStatePublication secondPublication = new JmapStatePublication();
        firstPublication.StateChange.Changed.Add("account-1", new Dictionary<string, string>());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secondRequest.Headers, Is.Empty);
            Assert.That(secondRequest.Context.Values, Is.Empty);
            Assert.That(secondResponse.Headers, Is.Empty);
            Assert.That(secondPublication.StateChange.Changed, Is.Empty);
            Assert.That(firstRequest.Headers, Is.Not.SameAs(secondRequest.Headers));
            Assert.That(firstRequest.Context, Is.Not.SameAs(secondRequest.Context));
            Assert.That(firstResponse.Headers, Is.Not.SameAs(secondResponse.Headers));
            Assert.That(firstPublication.StateChange, Is.Not.SameAs(secondPublication.StateChange));
        }
    }

    private class StubPushUrlPolicy : IJmapPushUrlPolicy
    {
        public bool IsPushUrlAllowed(string url, JmapRequestContext context)
        {
            return url.StartsWith("https://", StringComparison.Ordinal);
        }
    }

    private class StubStringLocalizer : IJmapStringLocalizer
    {
        public string LocalizeJmapString(string key, string? acceptLanguage)
        {
            return $"{acceptLanguage}:{key}";
        }
    }
}
