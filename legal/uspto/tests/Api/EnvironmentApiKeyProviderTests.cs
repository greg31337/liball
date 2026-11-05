using uspto;

namespace uspto.Tests;

[NonParallelizable]
public class EnvironmentApiKeyProviderTests
{
    private string? originalOdpKey;
    private string? originalTsdrKey;
    private string? originalPatentsViewKey;

    [SetUp]
    public void SaveAndClearCredentialEnvironment()
    {
        originalOdpKey = Environment.GetEnvironmentVariable(UsptoEnvironmentVariables.OdpApiKey);
        originalTsdrKey = Environment.GetEnvironmentVariable(UsptoEnvironmentVariables.TsdrApiKey);
        originalPatentsViewKey = Environment.GetEnvironmentVariable(UsptoEnvironmentVariables.PatentsViewApiKey);
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.OdpApiKey, null);
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.TsdrApiKey, null);
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.PatentsViewApiKey, null);
    }

    [TearDown]
    public void RestoreCredentialEnvironment()
    {
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.OdpApiKey, originalOdpKey);
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.TsdrApiKey, originalTsdrKey);
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.PatentsViewApiKey, originalPatentsViewKey);
    }

    [Test]
    public void MapsEveryServiceToItsExplicitEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.OdpApiKey, "odp-key");
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.TsdrApiKey, "tsdr-key");
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.PatentsViewApiKey, "patentsview-key");
        EnvironmentApiKeyProvider provider = new EnvironmentApiKeyProvider();

        ApiCredentialResult odp = provider.GetApiKey(new ApiCredentialRequest { Service = ApiService.Odp });
        ApiCredentialResult officeAction = provider.GetApiKey(new ApiCredentialRequest { Service = ApiService.OfficeAction });
        ApiCredentialResult tsdr = provider.GetApiKey(new ApiCredentialRequest { Service = ApiService.Tsdr });
        ApiCredentialResult tsdrPdf = provider.GetApiKey(new ApiCredentialRequest { Service = ApiService.TsdrPdfAndZip });
        ApiCredentialResult patentsView = provider.GetApiKey(new ApiCredentialRequest { Service = ApiService.PatentsView });

        Assert.Multiple(() =>
        {
            Assert.That(odp.ApiKey, Is.EqualTo("odp-key"));
            Assert.That(odp.Source, Is.EqualTo(UsptoEnvironmentVariables.OdpApiKey));
            Assert.That(officeAction.ApiKey, Is.EqualTo("odp-key"));
            Assert.That(officeAction.Source, Is.EqualTo(UsptoEnvironmentVariables.OdpApiKey));
            Assert.That(tsdr.ApiKey, Is.EqualTo("tsdr-key"));
            Assert.That(tsdr.Source, Is.EqualTo(UsptoEnvironmentVariables.TsdrApiKey));
            Assert.That(tsdrPdf.ApiKey, Is.EqualTo("tsdr-key"));
            Assert.That(tsdrPdf.Source, Is.EqualTo(UsptoEnvironmentVariables.TsdrApiKey));
            Assert.That(patentsView.ApiKey, Is.EqualTo("patentsview-key"));
            Assert.That(patentsView.Source, Is.EqualTo(UsptoEnvironmentVariables.PatentsViewApiKey));
            Assert.That(odp.IsAvailable, Is.True);
            Assert.That(patentsView.IsAvailable, Is.True);
        });
    }

    [Test]
    public void ReadsEnvironmentAgainForEveryCredentialRequest()
    {
        EnvironmentApiKeyProvider provider = new EnvironmentApiKeyProvider();
        ApiCredentialRequest request = new ApiCredentialRequest { Service = ApiService.Odp };
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.OdpApiKey, "first-key");

        ApiCredentialResult first = provider.GetApiKey(request);
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.OdpApiKey, "rotated-key");
        ApiCredentialResult second = provider.GetApiKey(request);

        Assert.Multiple(() =>
        {
            Assert.That(first.ApiKey, Is.EqualTo("first-key"));
            Assert.That(second.ApiKey, Is.EqualTo("rotated-key"));
            Assert.That(first.Request, Is.SameAs(request));
            Assert.That(second.Request, Is.SameAs(request));
        });
    }

    [Test]
    public void AnonymousRequestReturnsUnavailableResultForMissingOrBlankKey()
    {
        EnvironmentApiKeyProvider provider = new EnvironmentApiKeyProvider();
        ApiCredentialRequest request = new ApiCredentialRequest
        {
            Service = ApiService.PatentsView,
            AllowAnonymous = true
        };

        ApiCredentialResult missing = provider.GetApiKey(request);
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.PatentsViewApiKey, " \t ");
        ApiCredentialResult blank = provider.GetApiKey(request);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Request, Is.SameAs(request));
            Assert.That(missing.ApiKey, Is.Null);
            Assert.That(missing.Source, Is.EqualTo(UsptoEnvironmentVariables.PatentsViewApiKey));
            Assert.That(missing.IsAvailable, Is.False);
            Assert.That(blank.ApiKey, Is.Null);
            Assert.That(blank.IsAvailable, Is.False);
        });
    }

    [Test]
    public void RequiredBlankKeyThrowsCredentialFreeAuthenticationException()
    {
        const string secretMarker = "must-not-appear";
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.TsdrApiKey, " \t ");
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.OdpApiKey, secretMarker);
        EnvironmentApiKeyProvider provider = new EnvironmentApiKeyProvider();

        AuthenticationException exception = Assert.Throws<AuthenticationException>(() => provider.GetApiKey(new ApiCredentialRequest
        {
            Service = ApiService.Tsdr,
            AllowAnonymous = false
        }));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Service, Is.EqualTo(ApiService.Tsdr));
            Assert.That(exception.StatusCode, Is.Zero);
            Assert.That(exception.HasStatusCode, Is.False);
            Assert.That(exception.RequestIdentifier, Is.Null);
            Assert.That(exception.Message, Does.Not.Contain(secretMarker));
            Assert.That(exception.ToString(), Does.Not.Contain(secretMarker));
        });
    }

    [Test]
    public void InvalidRequestFailsWithStructuredValidationContext()
    {
        const string secretMarker = "environment-secret";
        Environment.SetEnvironmentVariable(UsptoEnvironmentVariables.OdpApiKey, secretMarker);
        EnvironmentApiKeyProvider provider = new EnvironmentApiKeyProvider();

        ValidationException missing = Assert.Throws<ValidationException>(() => provider.GetApiKey(null!));
        ValidationException unsupported = Assert.Throws<ValidationException>(() => provider.GetApiKey(new ApiCredentialRequest
        {
            Service = ApiService.Unknown
        }));

        Assert.Multiple(() =>
        {
            Assert.That(missing.FieldName, Is.EqualTo("request"));
            Assert.That(missing.ErrorCode, Is.EqualTo("REQUIRED"));
            Assert.That(missing.Message, Does.Not.Contain(secretMarker));
            Assert.That(unsupported.FieldName, Is.EqualTo("Service"));
            Assert.That(unsupported.ErrorCode, Is.EqualTo("UNSUPPORTED_SERVICE"));
            Assert.That(unsupported.Message, Does.Not.Contain(secretMarker));
        });
    }
}
