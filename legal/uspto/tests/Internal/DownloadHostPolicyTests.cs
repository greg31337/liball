using System.Net;
using uspto;

namespace uspto.Tests;

public class DownloadHostPolicyTests
{
    [TestCase("https://api.uspto.gov/file")]
    [TestCase("https://tsdrapi.uspto.gov/file")]
    [TestCase("https://bulkdata.uspto.gov/file")]
    [TestCase("https://search.patentsview.org/file")]
    public void OfficialDownloadHostsAreAcceptedWhenTheyResolvePublicly(string uriText)
    {
        FakeDownloadHostResolver resolver = new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") });
        DownloadHostPolicy policy = new DownloadHostPolicy(resolver);

        DownloadHostValidation result = policy.Validate(new Uri(uriText), new UsptoClientOptions(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsOfficialHost, Is.True);
            Assert.That(result.IsLoopbackTestEndpoint, Is.False);
            Assert.That(result.Addresses, Is.EqualTo(new IPAddress[] { IPAddress.Parse("8.8.8.8") }));
            Assert.That(resolver.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ConfiguredHostMatchingIsExactCaseInsensitiveAndAllowsATrailingDot()
    {
        FakeDownloadHostResolver resolver = new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("1.1.1.1") });
        DownloadHostPolicy policy = new DownloadHostPolicy(resolver);
        UsptoClientOptions options = new UsptoClientOptions
        {
            PermittedDownloadHosts = new string[] { "  DOWNLOAD.EXAMPLE.TEST.  " }
        };

        DownloadHostValidation result = policy.Validate(new Uri("https://download.example.test/file?part=1"), options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Host, Is.EqualTo("download.example.test"));
            Assert.That(result.IsOfficialHost, Is.False);
            Assert.That(resolver.LastHost, Is.EqualTo("download.example.test"));
        });
    }

    [TestCase("https://example.test/file")]
    [TestCase("https://evil.bulkdata.uspto.gov/file")]
    [TestCase("https://bulkdata.uspto.gov.example.test/file")]
    public void UnapprovedHostsAreRejectedBeforeDnsResolution(string uriText)
    {
        FakeDownloadHostResolver resolver = new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") });
        DownloadHostPolicy policy = new DownloadHostPolicy(resolver);

        ValidationException exception = Assert.Throws<ValidationException>(() => policy.Validate(new Uri(uriText), new UsptoClientOptions(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("DOWNLOAD_HOST_NOT_ALLOWED"));
            Assert.That(resolver.CallCount, Is.Zero);
        });
    }

    [TestCase("127.0.0.1")]
    [TestCase("10.20.30.40")]
    public void OfficialHostResolvingToANonPublicAddressIsRejected(string address)
    {
        FakeDownloadHostResolver resolver = new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse(address) });
        DownloadHostPolicy policy = new DownloadHostPolicy(resolver);

        ValidationException exception = Assert.Throws<ValidationException>(() => policy.Validate(new Uri("https://bulkdata.uspto.gov/file"), new UsptoClientOptions(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("DOWNLOAD_ADDRESS_REJECTED"));
            Assert.That(resolver.CallCount, Is.EqualTo(1));
        });
    }

    [TestCase("https://user:password@bulkdata.uspto.gov/file", "DOWNLOAD_USERINFO_REJECTED")]
    [TestCase("https://bulkdata.uspto.gov/file#section", "DOWNLOAD_FRAGMENT_REJECTED")]
    [TestCase("ftp://bulkdata.uspto.gov/file", "DOWNLOAD_SCHEME_REJECTED")]
    [TestCase("file:///tmp/file", "DOWNLOAD_SCHEME_REJECTED")]
    public void UnsafeUriComponentsAreRejected(string uriText, string errorCode)
    {
        DownloadHostPolicy policy = new DownloadHostPolicy(new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") }));

        ValidationException exception = Assert.Throws<ValidationException>(() => policy.Validate(new Uri(uriText), new UsptoClientOptions(), CancellationToken.None));

        Assert.That(exception.ErrorCode, Is.EqualTo(errorCode));
    }

    [Test]
    public void RelativeDownloadUriIsRejected()
    {
        DownloadHostPolicy policy = new DownloadHostPolicy(new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") }));

        ValidationException exception = Assert.Throws<ValidationException>(() => policy.Validate(new Uri("relative/file", UriKind.Relative), new UsptoClientOptions(), CancellationToken.None));

        Assert.That(exception.ErrorCode, Is.EqualTo("ABSOLUTE_URI_REQUIRED"));
    }

    [Test]
    public void PublicHttpEndpointIsRejectedEvenWhenItsHostIsConfigured()
    {
        FakeDownloadHostResolver resolver = new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") });
        DownloadHostPolicy policy = new DownloadHostPolicy(resolver);
        UsptoClientOptions options = new UsptoClientOptions
        {
            PermittedDownloadHosts = new string[] { "download.example.test" },
            PermitInsecureTestEndpoints = true
        };

        ValidationException exception = Assert.Throws<ValidationException>(() => policy.Validate(new Uri("http://download.example.test/file"), options, CancellationToken.None));

        Assert.That(exception.ErrorCode, Is.EqualTo("DOWNLOAD_HTTPS_REQUIRED"));
    }

    [Test]
    public void ExplicitLoopbackTestEndpointCanUseHttp()
    {
        FakeDownloadHostResolver resolver = new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") });
        DownloadHostPolicy policy = new DownloadHostPolicy(resolver);
        UsptoClientOptions options = new UsptoClientOptions
        {
            PermittedDownloadHosts = new string[] { "127.0.0.1" },
            PermitInsecureTestEndpoints = true
        };

        DownloadHostValidation result = policy.Validate(new Uri("http://127.0.0.1:5042/file"), options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsLoopbackTestEndpoint, Is.True);
            Assert.That(result.Addresses, Is.EqualTo(new IPAddress[] { IPAddress.Loopback }));
            Assert.That(resolver.CallCount, Is.Zero);
        });
    }

    [Test]
    public void ConfiguredLoopbackNameRequiresOnlyLoopbackDnsAnswers()
    {
        FakeDownloadHostResolver resolver = new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback });
        DownloadHostPolicy policy = new DownloadHostPolicy(resolver);
        UsptoClientOptions options = new UsptoClientOptions
        {
            PermittedDownloadHosts = new string[] { "localhost" },
            PermitInsecureTestEndpoints = true
        };

        DownloadHostValidation result = policy.Validate(new Uri("http://localhost:5042/file"), options, CancellationToken.None);

        Assert.That(result.IsLoopbackTestEndpoint, Is.True);
    }

    [Test]
    public void LoopbackExceptionDoesNotPermitMixedDnsAnswers()
    {
        FakeDownloadHostResolver resolver = new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Loopback, IPAddress.Parse("8.8.8.8") });
        DownloadHostPolicy policy = new DownloadHostPolicy(resolver);
        UsptoClientOptions options = new UsptoClientOptions
        {
            PermittedDownloadHosts = new string[] { "mixed.example.test" },
            PermitInsecureTestEndpoints = true
        };

        ValidationException exception = Assert.Throws<ValidationException>(() => policy.Validate(new Uri("https://mixed.example.test/file"), options, CancellationToken.None));

        Assert.That(exception.ErrorCode, Is.EqualTo("DOWNLOAD_ADDRESS_REJECTED"));
    }

    [Test]
    public void LoopbackFlagDoesNotApproveAnUnlistedHost()
    {
        FakeDownloadHostResolver resolver = new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Loopback });
        DownloadHostPolicy policy = new DownloadHostPolicy(resolver);
        UsptoClientOptions options = new UsptoClientOptions { PermitInsecureTestEndpoints = true };

        ValidationException exception = Assert.Throws<ValidationException>(() => policy.Validate(new Uri("http://localhost:5042/file"), options, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("DOWNLOAD_HOST_NOT_ALLOWED"));
            Assert.That(resolver.CallCount, Is.Zero);
        });
    }

    [TestCase("0.0.0.1")]
    [TestCase("10.0.0.1")]
    [TestCase("100.64.0.1")]
    [TestCase("127.0.0.1")]
    [TestCase("169.254.1.1")]
    [TestCase("172.16.0.1")]
    [TestCase("192.168.0.1")]
    [TestCase("192.0.2.1")]
    [TestCase("198.18.0.1")]
    [TestCase("198.51.100.1")]
    [TestCase("203.0.113.1")]
    [TestCase("224.0.0.1")]
    [TestCase("255.255.255.255")]
    public void NonPublicIpv4TargetsAreRejected(string address)
    {
        DownloadHostPolicy policy = new DownloadHostPolicy(new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") }));
        UsptoClientOptions options = new UsptoClientOptions { PermittedDownloadHosts = new string[] { address } };

        ValidationException exception = Assert.Throws<ValidationException>(() => policy.Validate(new Uri("https://" + address + "/file"), options, CancellationToken.None));

        Assert.That(exception.ErrorCode, Is.EqualTo("DOWNLOAD_ADDRESS_REJECTED"));
    }

    [TestCase("::")]
    [TestCase("::1")]
    [TestCase("fe80::1")]
    [TestCase("fc00::1")]
    [TestCase("2001:db8::1")]
    [TestCase("2001::1")]
    [TestCase("2002:0a00:0001::1")]
    public void NonPublicIpv6TargetsAreRejected(string address)
    {
        DownloadHostPolicy policy = new DownloadHostPolicy(new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") }));
        UsptoClientOptions options = new UsptoClientOptions { PermittedDownloadHosts = new string[] { address } };

        ValidationException exception = Assert.Throws<ValidationException>(() => policy.Validate(new Uri("https://[" + address + "]/file"), options, CancellationToken.None));

        Assert.That(exception.ErrorCode, Is.EqualTo("DOWNLOAD_ADDRESS_REJECTED"));
    }

    [Test]
    public void PublicIpv6LiteralIsAcceptedWithoutDnsResolution()
    {
        FakeDownloadHostResolver resolver = new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Loopback });
        DownloadHostPolicy policy = new DownloadHostPolicy(resolver);
        UsptoClientOptions options = new UsptoClientOptions { PermittedDownloadHosts = new string[] { "2606:4700:4700::1111" } };

        DownloadHostValidation result = policy.Validate(new Uri("https://[2606:4700:4700::1111]/file"), options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Addresses, Has.Length.EqualTo(1));
            Assert.That(result.Addresses[0], Is.EqualTo(IPAddress.Parse("2606:4700:4700::1111")));
            Assert.That(resolver.CallCount, Is.Zero);
        });
    }

    [Test]
    public void EmptyDnsResolutionIsRejected()
    {
        DownloadHostPolicy policy = new DownloadHostPolicy(new FakeDownloadHostResolver(Array.Empty<IPAddress>()));
        UsptoClientOptions options = new UsptoClientOptions { PermittedDownloadHosts = new string[] { "download.example.test" } };

        ValidationException exception = Assert.Throws<ValidationException>(() => policy.Validate(new Uri("https://download.example.test/file"), options, CancellationToken.None));

        Assert.That(exception.ErrorCode, Is.EqualTo("DOWNLOAD_HOST_RESOLUTION_EMPTY"));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("*.example.test")]
    [TestCase("https://example.test")]
    [TestCase("example.test:443")]
    [TestCase("user@example.test")]
    [TestCase("example test")]
    [TestCase("_")]
    public void InvalidConfiguredHostsAreRejected(string configuredHost)
    {
        DownloadHostPolicy policy = new DownloadHostPolicy(new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") }));
        UsptoClientOptions options = new UsptoClientOptions { PermittedDownloadHosts = new string[] { configuredHost } };

        ValidationException exception = Assert.Throws<ValidationException>(() => policy.Validate(new Uri("https://download.example.test/file"), options, CancellationToken.None));

        Assert.That(exception.ErrorCode, Is.EqualTo("INVALID_DOWNLOAD_HOST"));
    }

    [Test]
    public void EntireConfiguredHostListIsValidatedEvenAfterAMatch()
    {
        DownloadHostPolicy policy = new DownloadHostPolicy(new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") }));
        UsptoClientOptions options = new UsptoClientOptions
        {
            PermittedDownloadHosts = new string[] { "download.example.test", "*.invalid.test" }
        };

        ValidationException exception = Assert.Throws<ValidationException>(() => policy.Validate(new Uri("https://download.example.test/file"), options, CancellationToken.None));

        Assert.That(exception.ErrorCode, Is.EqualTo("INVALID_DOWNLOAD_HOST"));
    }

    [Test]
    public void CallerCancellationStopsValidationBeforeDnsResolution()
    {
        FakeDownloadHostResolver resolver = new FakeDownloadHostResolver(new IPAddress[] { IPAddress.Parse("8.8.8.8") });
        DownloadHostPolicy policy = new DownloadHostPolicy(resolver);
        using CancellationTokenSource caller = new CancellationTokenSource();
        caller.Cancel();

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() => policy.Validate(new Uri(UsptoEndpoints.BulkDataBaseUri), new UsptoClientOptions(), caller.Token));

        Assert.Multiple(() =>
        {
            Assert.That(exception.CancellationToken, Is.EqualTo(caller.Token));
            Assert.That(resolver.CallCount, Is.Zero);
        });
    }
}

internal class FakeDownloadHostResolver : IDownloadHostResolver
{
    internal IPAddress[] Addresses;
    internal int CallCount = 0;
    internal string? LastHost = null;

    internal FakeDownloadHostResolver(IPAddress[] addresses)
    {
        Addresses = addresses;
    }

    public IPAddress[] Resolve(string host, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CallCount += 1;
        LastHost = host;
        return Addresses;
    }
}
