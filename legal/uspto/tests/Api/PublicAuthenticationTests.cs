using System.Net;
using uspto;

namespace uspto.Tests;

public class PublicAuthenticationTests
{
    [Test]
    public void ConfiguredClientReadsProviderForEveryRequestAndSendsRotatedKeyOnlyInTheExactHeader()
    {
        using PublicClientTestHarness harness = new PublicClientTestHarness();
        harness.Handler.AddJsonResponse(HttpStatusCode.OK, "{\"count\":0,\"patentFileWrapperDataBag\":[]}");
        harness.Handler.AddJsonResponse(HttpStatusCode.OK, "{\"count\":0,\"patentFileWrapperDataBag\":[]}");
        PublicRotatingApiKeyProvider provider = new PublicRotatingApiKeyProvider("first-provider-key");
        UsptoClientOptions options = harness.CreateOptions();
        options.OdpApiKey = null;
        options.ApiKeyProvider = provider;
        UsptoClients clients = new UsptoClientFactory().CreateUsptoClients(options);

        clients.PatentClient!.SearchPatentApplications(new SearchRequest());
        provider.CurrentKey = "second-provider-key";
        clients.PatentClient.SearchPatentApplications(new SearchRequest());

        Assert.Multiple(() =>
        {
            Assert.That(harness.Handler.Requests[0].Headers[UsptoHeaders.OdpApiKey], Is.EqualTo(new[] { "first-provider-key" }));
            Assert.That(harness.Handler.Requests[1].Headers[UsptoHeaders.OdpApiKey], Is.EqualTo(new[] { "second-provider-key" }));
            Assert.That(harness.Handler.Requests[0].Headers.ContainsKey(UsptoHeaders.TsdrApiKey), Is.False);
            Assert.That(harness.Handler.Requests[0].Headers.Single(header => string.Equals(header.Key, UsptoHeaders.OdpApiKey, StringComparison.OrdinalIgnoreCase)).Key, Is.EqualTo(UsptoHeaders.OdpApiKey));
            Assert.That(provider.CallCount, Is.EqualTo(2));
            Assert.That(provider.LastRequest.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(provider.LastRequest.AllowAnonymous, Is.False);
        });

        clients.Lifetime!.Dispose();
    }

    [Test]
    public void ProviderCredentialCannotEscapeThroughPublicErrorResults()
    {
        const string secret = "public-provider-secret";
        using PublicClientTestHarness harness = new PublicClientTestHarness();
        harness.Handler.AddJsonResponse(HttpStatusCode.BadRequest, "{\"code\":\"INVALID\",\"details\":\"echo " + secret + "\",\"requestId\":\"request-" + secret + "\"}");
        UsptoClientOptions options = harness.CreateOptions();
        options.OdpApiKey = null;
        options.ApiKeyProvider = new PublicRotatingApiKeyProvider(secret);
        UsptoClients clients = new UsptoClientFactory().CreateUsptoClients(options);

        ApiException failure = Assert.Throws<ApiException>(() => clients.PatentClient!.SearchPatentApplications(new SearchRequest()))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.ErrorDetails, Does.Contain(SecretRedactor.Replacement));
            Assert.That(failure.ErrorDetails, Does.Not.Contain(secret));
            Assert.That(failure.RequestIdentifier, Does.Not.Contain(secret));
            Assert.That(failure.ToString(), Does.Not.Contain(secret));
        });

        clients.Lifetime!.Dispose();
    }
}

internal class PublicRotatingApiKeyProvider : IApiKeyProvider
{
    internal string CurrentKey;
    internal int CallCount = 0;
    internal ApiCredentialRequest LastRequest = new ApiCredentialRequest();

    internal PublicRotatingApiKeyProvider(string currentKey)
    {
        CurrentKey = currentKey;
    }

    public ApiCredentialResult GetApiKey(ApiCredentialRequest request)
    {
        CallCount += 1;
        LastRequest = request;
        return new ApiCredentialResult
        {
            Request = request,
            ApiKey = CurrentKey,
            Source = "rotating test provider",
            IsAvailable = true
        };
    }
}
