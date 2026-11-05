using System.Text;

namespace dns.Tests.Api;

public class DnsResolverConfigurationTests
{
    [Test]
    public void ParsesServersSearchDomainsAndSupportedOptions()
    {
        string source = """
            # generated resolver configuration
            nameserver 192.0.2.53
            nameserver 2001:db8::53
            search CORP.Example services.example.
            options ndots:3 timeout:7 attempts:4 rotate use-vc trust-ad
            """;
        DnsStubResolver resolver = new DnsStubResolver();

        DnsResolverConfiguration result =
            resolver.ParseResolverConfiguration(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Servers, Has.Count.EqualTo(2));
            Assert.That(result.Servers[0].Endpoint.AddressFamily,
                Is.EqualTo(4));
            Assert.That(result.Servers[0].Endpoint.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 53 }));
            Assert.That(result.Servers[0].Endpoint.Port,
                Is.EqualTo(DnsPorts.Dns));
            Assert.That(result.Servers[0].Priority, Is.Zero);
            Assert.That(result.Servers[1].Endpoint.AddressFamily,
                Is.EqualTo(6));
            Assert.That(result.Servers[1].Endpoint.Address,
                Has.Length.EqualTo(16));
            Assert.That(result.Servers[1].Priority, Is.EqualTo(1));
            Assert.That(Text(result.SearchDomains[0]),
                Is.EqualTo("corp.example."));
            Assert.That(Text(result.SearchDomains[1]),
                Is.EqualTo("services.example."));
            Assert.That(result.NameDots, Is.EqualTo(3));
            Assert.That(result.TimeoutMilliseconds, Is.EqualTo(7000));
            Assert.That(result.Attempts, Is.EqualTo(4));
            Assert.That(result.RotateServers, Is.True);
            Assert.That(result.UseTcp, Is.True);
            Assert.That(result.TrustAuthenticData, Is.True);
        });
    }

    [Test]
    public void LastSearchOrDomainDirectiveWinsAndUnknownInputIsIgnored()
    {
        string source = """
            nameserver 203.0.113.7 ; local forwarding resolver
            domain old.example
            search first.example second.example
            sortlist 192.0.2.0/24
            domain FINAL.Example # final suffix
            options debug unknown-option tcp
            """;
        DnsResolverConfiguration result = new DnsStubResolver()
            .ParseResolverConfiguration(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.SearchDomains, Has.Count.EqualTo(1));
            Assert.That(Text(result.SearchDomains[0]),
                Is.EqualTo("final.example."));
            Assert.That(result.UseTcp, Is.True);
            Assert.That(result.Servers, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ParserAppliesPortableBoundsAndLoopbackDefault()
    {
        DnsStubResolver resolver = new DnsStubResolver();
        DnsResolverConfiguration bounded = resolver.ParseResolverConfiguration(
            "options ndots:999 timeout:999 attempts:999\n");
        DnsResolverConfiguration minimum = resolver.ParseResolverConfiguration(
            "options ndots:0 timeout:0 attempts:0\n");

        Assert.Multiple(() =>
        {
            Assert.That(bounded.NameDots, Is.EqualTo(15));
            Assert.That(bounded.TimeoutMilliseconds, Is.EqualTo(30000));
            Assert.That(bounded.Attempts, Is.EqualTo(5));
            Assert.That(bounded.Servers, Has.Count.EqualTo(1));
            Assert.That(bounded.Servers[0].Endpoint.Address,
                Is.EqualTo(new byte[] { 127, 0, 0, 1 }));
            Assert.That(minimum.NameDots, Is.Zero);
            Assert.That(minimum.TimeoutMilliseconds, Is.EqualTo(1000));
            Assert.That(minimum.Attempts, Is.EqualTo(1));
        });
    }

    [Test]
    public void ParserBoundsHugeNumericOptionsWithoutOverflow()
    {
        string digits = new string('9', 200);
        string source = "options ndots:" + digits +
            " timeout:" + digits +
            " attempts:" + digits + "\r\n";

        DnsResolverConfiguration result = new DnsStubResolver()
            .ParseResolverConfiguration(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.NameDots, Is.EqualTo(15));
            Assert.That(result.TimeoutMilliseconds, Is.EqualTo(30000));
            Assert.That(result.Attempts, Is.EqualTo(5));
            Assert.That(result.Servers, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ParserRetainsOnlyThePortableNameserverLimit()
    {
        string source = """
            nameserver 192.0.2.1
            nameserver 192.0.2.2
            nameserver 192.0.2.3
            nameserver 192.0.2.4
            """;

        DnsResolverConfiguration result = new DnsStubResolver()
            .ParseResolverConfiguration(source);

        Assert.That(result.Servers.Select(server => server.Endpoint.Address[3]),
            Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void ExplicitValidationNormalizesAndIndependentlyOwnsValues()
    {
        DnsResolverConfiguration source = ExplicitConfiguration();
        source.SearchDomains.Add(Name("CORP", "Example"));
        DnsResolverServer server = source.Servers[0];
        server.Endpoint.HasAddress = false;
        server.Endpoint.Address = [];
        server.Endpoint.AddressFamily = 0;
        server.Endpoint.Host = "Resolver.EXAMPLE";
        server.Endpoint.Port = DnsPorts.DnsOverTls;
        server.Transport = DnsTransportKind.Tls;
        server.AuthenticationName = "resolver.example";
        server.Priority = 2;

        DnsResolverConfiguration result = new DnsStubResolver()
            .ValidateResolverConfiguration(source);
        source.SearchDomains[0].Labels[0][0] = (byte)'x';
        server.Endpoint.Host = "changed.example";

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.SameAs(source));
            Assert.That(result.Servers[0], Is.Not.SameAs(server));
            Assert.That(result.Servers[0].Endpoint, Is.Not.SameAs(server.Endpoint));
            Assert.That(result.Servers[0].Endpoint.Host,
                Is.EqualTo("resolver.example"));
            Assert.That(result.Servers[0].Endpoint.Port,
                Is.EqualTo(DnsPorts.DnsOverTls));
            Assert.That(result.Servers[0].Transport,
                Is.EqualTo(DnsTransportKind.Tls));
            Assert.That(result.Servers[0].AuthenticationName,
                Is.EqualTo("resolver.example"));
            Assert.That(result.Servers[0].Priority, Is.EqualTo(2));
            Assert.That(Text(result.SearchDomains[0]),
                Is.EqualTo("corp.example."));
        });
    }

    [Test]
    public void ExplicitValidationRejectsInvalidBoundsAndEndpoints()
    {
        DnsStubResolver resolver = new DnsStubResolver();
        DnsResolverConfiguration ndots = ExplicitConfiguration();
        ndots.NameDots = -1;
        DnsResolverConfiguration attempts = ExplicitConfiguration();
        attempts.Attempts = 6;
        DnsResolverConfiguration timeout = ExplicitConfiguration();
        timeout.TimeoutMilliseconds = 0;
        DnsResolverConfiguration port = ExplicitConfiguration();
        port.Servers[0].Endpoint.Port = 70000;
        DnsResolverConfiguration address = ExplicitConfiguration();
        address.Servers[0].Endpoint.Address = [1, 2, 3];
        DnsResolverConfiguration disabled = ExplicitConfiguration();
        disabled.Servers[0].Enabled = false;

        Assert.Multiple(() =>
        {
            Assert.That(() => resolver.ValidateResolverConfiguration(ndots),
                Throws.TypeOf<DnsResolutionException>());
            Assert.That(() => resolver.ValidateResolverConfiguration(attempts),
                Throws.TypeOf<DnsResolutionException>());
            Assert.That(() => resolver.ValidateResolverConfiguration(timeout),
                Throws.TypeOf<DnsResolutionException>());
            Assert.That(() => resolver.ValidateResolverConfiguration(port),
                Throws.TypeOf<DnsResolutionException>());
            Assert.That(() => resolver.ValidateResolverConfiguration(address),
                Throws.TypeOf<DnsResolutionException>());
            Assert.That(() => resolver.ValidateResolverConfiguration(disabled),
                Throws.TypeOf<DnsResolutionException>());
        });
    }

    [Test]
    public void ParserRejectsMalformedRecognizedDirectives()
    {
        string[] invalidSources =
        [
            "nameserver\n",
            "nameserver resolver.example\n",
            "nameserver 192.0.2.1 extra\n",
            "search\n",
            "search bad..example\n",
            "domain first.example second.example\n",
            "options ndots:not-a-number\n",
            "options timeout:\n",
            "options attempts:-1\n"
        ];
        DnsStubResolver resolver = new DnsStubResolver();

        int index = 0;
        while (index < invalidSources.Length)
        {
            string source = invalidSources[index];
            Assert.That(() => resolver.ParseResolverConfiguration(source),
                Throws.TypeOf<DnsResolutionException>(), source);
            index = index + 1;
        }
    }

    [Test]
    public void ParserFailureIdentifiesTheRecognizedDirectiveLine()
    {
        string source = """
            # first line is a comment
            nameserver 192.0.2.53
            options ndots:not-a-number
            """;
        DnsStubResolver resolver = new DnsStubResolver();

        DnsResolutionException exception = Assert.Throws<DnsResolutionException>(
            () => resolver.ParseResolverConfiguration(source))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message,
                Does.Contain("Resolver configuration line 3"));
            Assert.That(exception.Message, Does.Contain("ndots"));
        });
    }

    static DnsResolverConfiguration ExplicitConfiguration()
    {
        DnsResolverConfiguration result = new DnsResolverConfiguration();
        DnsResolverServer server = new DnsResolverServer();
        server.Endpoint.HasAddress = true;
        server.Endpoint.Address = [192, 0, 2, 53];
        server.Endpoint.AddressFamily = 4;
        server.Endpoint.Port = DnsPorts.Dns;
        result.Servers.Add(server);
        return result;
    }

    static DnsName Name(string first, string second)
    {
        DnsName result = new DnsName();
        result.Labels.Add(Encoding.ASCII.GetBytes(first));
        result.Labels.Add(Encoding.ASCII.GetBytes(second));
        return result;
    }

    static string Text(DnsName name)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < name.Labels.Count)
        {
            if (index > 0)
            {
                result.Append('.');
            }

            result.Append(Encoding.ASCII.GetString(name.Labels[index]));
            index = index + 1;
        }

        if (name.IsAbsolute)
        {
            result.Append('.');
        }

        return result.ToString();
    }
}
