using uspto;

namespace uspto.Tests;

public class ClientContractsTests
{
    [Test]
    public void ClientSuiteStartsUnconfigured()
    {
        UsptoClients clients = new UsptoClients();

        Assert.Multiple(() =>
        {
            Assert.That(clients.Options, Is.Not.Null);
            Assert.That(clients.ApiKeyProvider, Is.Null);
            Assert.That(clients.IdentifierNormalizer, Is.Null);
            Assert.That(clients.PatentClient, Is.Null);
            Assert.That(clients.PetitionClient, Is.Null);
            Assert.That(clients.PtabClient, Is.Null);
            Assert.That(clients.OfficeActionClient, Is.Null);
            Assert.That(clients.TrademarkClient, Is.Null);
            Assert.That(clients.BulkCatalogClient, Is.Null);
            Assert.That(clients.BulkTransferClient, Is.Null);
            Assert.That(clients.BulkDataReader, Is.Null);
            Assert.That(clients.DatasetMirror, Is.Null);
            Assert.That(clients.LocalDatasetQuery, Is.Null);
            Assert.That(clients.PatentsViewClient, Is.Null);
            Assert.That(clients.Capabilities, Is.Null);
            Assert.That(clients.Lifetime, Is.Null);
        });
    }

    [Test]
    public void ClientSuiteOptionDefaultsAreIndependent()
    {
        UsptoClients first = new UsptoClients();
        UsptoClients second = new UsptoClients();

        first.Options.Retry.AttemptCount = 9;
        first.Options.PermittedDownloadHosts = new string[] { "downloads.example.test" };

        Assert.Multiple(() =>
        {
            Assert.That(first.Options, Is.Not.SameAs(second.Options));
            Assert.That(first.Options.Retry, Is.Not.SameAs(second.Options.Retry));
            Assert.That(second.Options.Retry.AttemptCount, Is.EqualTo(UsptoDefaults.HttpAttemptCount));
            Assert.That(second.Options.PermittedDownloadHosts, Is.Empty);
        });
    }

    [Test]
    public void FactoryContractCanComposeCredentialAndIdentifierComponents()
    {
        UsptoClientOptions options = new UsptoClientOptions
        {
            OdpApiKey = "explicit-key"
        };
        FakeApiKeyProvider provider = new FakeApiKeyProvider("rotated-key");
        FakeIdentifierNormalizer normalizer = new FakeIdentifierNormalizer();
        IUsptoClientFactory factory = new FakeUsptoClientFactory(provider, normalizer);

        UsptoClients clients = factory.CreateUsptoClients(options);
        ApiCredentialResult credential = clients.ApiKeyProvider!.GetApiKey(new ApiCredentialRequest
        {
            Service = ApiService.Odp
        });
        string normalizedPatent = clients.IdentifierNormalizer!.NormalizePatentIdentifier(" us 123 ");
        string normalizedTrademark = clients.IdentifierNormalizer.NormalizeTrademarkIdentifier(" sn 123 ");

        Assert.Multiple(() =>
        {
            Assert.That(clients.Options, Is.SameAs(options));
            Assert.That(clients.ApiKeyProvider, Is.SameAs(provider));
            Assert.That(clients.IdentifierNormalizer, Is.SameAs(normalizer));
            Assert.That(credential.Request.Service, Is.EqualTo(ApiService.Odp));
            Assert.That(credential.ApiKey, Is.EqualTo("rotated-key"));
            Assert.That(credential.IsAvailable, Is.True);
            Assert.That(normalizedPatent, Is.EqualTo("US123"));
            Assert.That(normalizedTrademark, Is.EqualTo("SN123"));
        });
    }
}

internal class FakeApiKeyProvider : IApiKeyProvider
{
    private string key;

    public FakeApiKeyProvider(string key)
    {
        this.key = key;
    }

    public ApiCredentialResult GetApiKey(ApiCredentialRequest request)
    {
        return new ApiCredentialResult
        {
            Request = request,
            ApiKey = key,
            Source = "test",
            IsAvailable = true
        };
    }
}

internal class FakeIdentifierNormalizer : IIdentifierNormalizer
{
    public string NormalizePatentIdentifier(string input)
    {
        return input.Replace(" ", string.Empty).ToUpperInvariant();
    }

    public string? TryNormalizePatentIdentifier(string input)
    {
        return NormalizePatentIdentifier(input);
    }

    public string NormalizeTrademarkIdentifier(string input)
    {
        return input.Replace(" ", string.Empty).ToUpperInvariant();
    }

    public string? TryNormalizeTrademarkIdentifier(string input)
    {
        return NormalizeTrademarkIdentifier(input);
    }
}

internal class FakeUsptoClientFactory : IUsptoClientFactory
{
    private IApiKeyProvider provider;
    private IIdentifierNormalizer normalizer;

    public FakeUsptoClientFactory(IApiKeyProvider provider, IIdentifierNormalizer normalizer)
    {
        this.provider = provider;
        this.normalizer = normalizer;
    }

    public UsptoClients CreateUsptoClients(UsptoClientOptions options)
    {
        return new UsptoClients
        {
            Options = options,
            ApiKeyProvider = provider,
            IdentifierNormalizer = normalizer
        };
    }
}
