using whois;

namespace whois.Tests;

public class ClientEndpointSecurityTests
{
    [TestCase("10.0.0.7")]
    [TestCase("169.254.1.7")]
    [TestCase("192.0.2.7")]
    [TestCase("::1")]
    [TestCase("fc00::7")]
    [TestCase("2001:db8::7")]
    public void PublicPolicyRejectsPrivateAndSpecialReferralAddresses(string rejectedAddressText)
    {
        using LoopbackWhoisServer discoveryServer = CreateServer("refer: whois.private.example\r\n");
        System.Net.IPAddress rejectedAddress = System.Net.IPAddress.Parse(rejectedAddressText);
        FakeHostResolver resolver = new FakeHostResolver();
        resolver.ResolveHandler = (endpoint, preference, cancellationToken) =>
        {
            WhoisResolution resolution = new WhoisResolution();
            resolution.Status = WhoisExchangeStatus.Complete;
            if (endpoint.Host == discoveryServer.Endpoint.Host)
            {
                resolution.Addresses = new[] { System.Net.IPAddress.Loopback };
            }
            else
            {
                resolution.Addresses = new[] { rejectedAddress };
            }
            return resolution;
        };
        WhoisTcpTransport transport = new WhoisTcpTransport(
            new WhoisQueryCodec(),
            resolver,
            new WhoisAllowAllEndpointPolicy(),
            new WhoisPublicEndpointPolicy(),
            new WhoisTcpConnectionFactory(),
            WhoisAddressPreference.System);
        WhoisClient client = CreateClient(transport);
        WhoisLookupRequest request = CreateLookupRequest(discoveryServer.Endpoint);

        WhoisLookupResult result = client.TryLookup(request);
        discoveryServer.Completion.GetAwaiter().GetResult();

        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.Connect));
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error!.Message, Is.EqualTo("No resolved address passed endpoint policy."));
        Assert.That(result.Hops, Has.Length.EqualTo(2));
        Assert.That(result.Hops[0].RawResponse.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(result.Hops[0].SelectedReferral, Is.SameAs(result.Hops[0].Referrals[0]));
        Assert.That(result.Hops[1].Selection.Endpoint.Host, Is.EqualTo("whois.private.example"));
        Assert.That(result.Hops[1].Selection.Source, Is.EqualTo(WhoisEndpointSource.Referral));
        Assert.That(result.Hops[1].RawResponse.Status, Is.EqualTo(WhoisExchangeStatus.ConnectFailed));
        Assert.That(result.Hops[1].RawResponse.ApprovedAddress, Is.Null);
        Assert.That(resolver.Endpoints, Has.Count.EqualTo(2));
        Assert.That(discoveryServer.ConnectionsAccepted, Is.EqualTo(1));
        Assert.That(discoveryServer.Error, Is.Null);
    }

    [Test]
    public void AllowAllPolicyOptsIntoTrustedPrivateReferralTraversal()
    {
        using LoopbackWhoisServer registryServer = CreateServer("Domain Name: EXAMPLE.COM\r\n");
        using LoopbackWhoisServer discoveryServer = CreateServer("refer: whois.registry.internal:" + registryServer.Endpoint.Port + "\r\n");
        FakeHostResolver resolver = CreateLoopbackResolver();
        IWhoisEndpointPolicy allowAll = new WhoisAllowAllEndpointPolicy();
        WhoisTcpTransport transport = new WhoisTcpTransport(
            new WhoisQueryCodec(),
            resolver,
            allowAll,
            allowAll,
            new WhoisTcpConnectionFactory(),
            WhoisAddressPreference.System);
        WhoisClient client = CreateClient(transport);

        WhoisLookupResult result = client.TryLookup(CreateLookupRequest(discoveryServer.Endpoint));
        discoveryServer.Completion.GetAwaiter().GetResult();
        registryServer.Completion.GetAwaiter().GetResult();

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.Hops, Has.Length.EqualTo(2));
        Assert.That(result.Hops[1].Selection.Endpoint.Host, Is.EqualTo("whois.registry.internal"));
        Assert.That(result.Hops[1].RawResponse.ApprovedAddress, Is.EqualTo(System.Net.IPAddress.Loopback));
        Assert.That(registryServer.ReceivedQuery, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("example.com\r\n")));
        Assert.That(registryServer.ConnectionsAccepted, Is.EqualTo(1));
        Assert.That(registryServer.Error, Is.Null);
    }

    [Test]
    public void CustomPolicyOptsIntoOnlyTheTrustedAutomaticReferral()
    {
        using LoopbackWhoisServer registryServer = CreateServer(string.Empty);
        using LoopbackWhoisServer discoveryServer = CreateServer("refer: whois.registry.internal:" + registryServer.Endpoint.Port + "\r\n");
        FakeHostResolver resolver = CreateLoopbackResolver();
        FakeEndpointPolicy automaticPolicy = new FakeEndpointPolicy();
        automaticPolicy.AllowHandler = (endpoint, address, source) =>
            endpoint.Host == "whois.registry.internal" &&
            address.Equals(System.Net.IPAddress.Loopback) &&
            source == WhoisEndpointSource.Referral;
        WhoisTcpTransport transport = new WhoisTcpTransport(
            new WhoisQueryCodec(),
            resolver,
            new WhoisAllowAllEndpointPolicy(),
            automaticPolicy,
            new WhoisTcpConnectionFactory(),
            WhoisAddressPreference.System);
        WhoisClient client = CreateClient(transport);

        WhoisLookupResult result = client.TryLookup(CreateLookupRequest(discoveryServer.Endpoint));
        discoveryServer.Completion.GetAwaiter().GetResult();
        registryServer.Completion.GetAwaiter().GetResult();

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.Hops, Has.Length.EqualTo(2));
        Assert.That(automaticPolicy.Endpoints, Has.Count.EqualTo(1));
        Assert.That(automaticPolicy.Endpoints[0].Host, Is.EqualTo("whois.registry.internal"));
        Assert.That(automaticPolicy.Addresses, Is.EqualTo(new[] { System.Net.IPAddress.Loopback }));
        Assert.That(automaticPolicy.Sources, Is.EqualTo(new[] { WhoisEndpointSource.Referral }));
        Assert.That(registryServer.ConnectionsAccepted, Is.EqualTo(1));
        Assert.That(registryServer.Error, Is.Null);
    }

    [Test]
    public void PublicPolicyAllowsNonstandardPortForDirectClientQuery()
    {
        System.Net.IPAddress publicAddress = System.Net.IPAddress.Parse("8.8.8.8");
        FakeHostResolver resolver = new FakeHostResolver();
        resolver.Result.Addresses = new[] { publicAddress };
        resolver.Result.Status = WhoisExchangeStatus.Complete;
        FakeConnectionFactory factory = new FakeConnectionFactory();
        factory.Stream = new MemoryStream();
        IWhoisEndpointPolicy publicPolicy = new WhoisPublicEndpointPolicy();
        WhoisTcpTransport transport = new WhoisTcpTransport(
            new WhoisQueryCodec(),
            resolver,
            publicPolicy,
            publicPolicy,
            factory,
            WhoisAddressPreference.System);
        WhoisClient client = CreateClient(transport);
        WhoisRequest request = new WhoisRequest();
        request.Endpoint.Host = "whois.direct.example";
        request.Endpoint.Port = 1043;
        request.QueryText = "exact direct query";

        WhoisRawResponse response = client.TryQuery(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(response.Request.Source, Is.EqualTo(WhoisEndpointSource.Direct));
        Assert.That(response.Request.Endpoint.Port, Is.EqualTo(1043));
        Assert.That(response.ApprovedAddress, Is.SameAs(publicAddress));
        Assert.That(factory.Endpoints, Has.Count.EqualTo(1));
        Assert.That(factory.Endpoints[0].Port, Is.EqualTo(1043));
        Assert.That(factory.ApprovedAddresses, Is.EqualTo(new[] { publicAddress }));
    }

    [Test]
    public void BuiltInConnectionFactoryDialsApprovedLiteralWithoutRebindingHostname()
    {
        using LoopbackWhoisServer server = CreateServer("connected through approved address\r\n");
        FakeHostResolver resolver = CreateLoopbackResolver();
        IWhoisEndpointPolicy allowAll = new WhoisAllowAllEndpointPolicy();
        WhoisTcpTransport transport = new WhoisTcpTransport(
            new WhoisQueryCodec(),
            resolver,
            allowAll,
            allowAll,
            new WhoisTcpConnectionFactory(),
            WhoisAddressPreference.System);
        WhoisRequest request = new WhoisRequest();
        request.Endpoint.Host = "would-rebind.invalid";
        request.Endpoint.Port = server.Endpoint.Port;
        request.Source = WhoisEndpointSource.Referral;
        request.QueryText = "example.com";

        WhoisRawResponse response = transport.Exchange(request);
        server.Completion.GetAwaiter().GetResult();

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(response.ApprovedAddress, Is.EqualTo(System.Net.IPAddress.Loopback));
        Assert.That(response.Body, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("connected through approved address\r\n")));
        Assert.That(resolver.Endpoints, Has.Count.EqualTo(1));
        Assert.That(resolver.Endpoints[0].Host, Is.EqualTo("would-rebind.invalid"));
        Assert.That(server.ReceivedQuery, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("example.com\r\n")));
        Assert.That(server.ConnectionsAccepted, Is.EqualTo(1));
        Assert.That(server.Error, Is.Null);
    }

    [Test]
    public void CustomResolverAndConnectionHooksReceiveOnlyTheAllowedMixedCandidate()
    {
        System.Net.IPAddress privateAddress = System.Net.IPAddress.Parse("10.0.0.9");
        System.Net.IPAddress documentationAddress = System.Net.IPAddress.Parse("192.0.2.9");
        System.Net.IPAddress publicAddress = System.Net.IPAddress.Parse("1.1.1.1");
        FakeHostResolver resolver = new FakeHostResolver();
        resolver.Result.Addresses = new[] { privateAddress, documentationAddress, publicAddress };
        resolver.Result.Status = WhoisExchangeStatus.Complete;
        FakeConnectionFactory factory = new FakeConnectionFactory();
        factory.Stream = new MemoryStream();
        IWhoisEndpointPolicy publicPolicy = new WhoisPublicEndpointPolicy();
        WhoisTcpTransport transport = new WhoisTcpTransport(
            new WhoisQueryCodec(),
            resolver,
            publicPolicy,
            publicPolicy,
            factory,
            WhoisAddressPreference.IPv4First);
        WhoisRequest request = new WhoisRequest();
        request.Endpoint.Host = "whois.mixed.example";
        request.Endpoint.Port = WhoisDefaults.WhoisPort;
        request.Source = WhoisEndpointSource.Referral;
        request.QueryText = "example.com";

        WhoisRawResponse response = transport.Exchange(request);

        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(response.ApprovedAddress, Is.SameAs(publicAddress));
        Assert.That(resolver.Endpoints, Has.Count.EqualTo(1));
        Assert.That(resolver.Endpoints[0], Is.SameAs(request.Endpoint));
        Assert.That(resolver.Preferences, Is.EqualTo(new[] { WhoisAddressPreference.IPv4First }));
        Assert.That(resolver.CancellationTokens, Has.Count.EqualTo(1));
        Assert.That(factory.Endpoints, Has.Count.EqualTo(1));
        Assert.That(factory.Endpoints[0], Is.SameAs(request.Endpoint));
        Assert.That(factory.ApprovedAddresses, Is.EqualTo(new[] { publicAddress }));
        Assert.That(factory.TimeoutOptions, Has.Count.EqualTo(1));
        Assert.That(factory.TimeoutOptions[0], Is.SameAs(request.Timeouts));
        Assert.That(factory.CancellationTokens, Has.Count.EqualTo(1));
    }

    private static LoopbackWhoisServer CreateServer(string responseText)
    {
        byte[] response = System.Text.Encoding.ASCII.GetBytes(responseText);
        return new LoopbackWhoisServer(new[] { response }, TimeSpan.Zero, false);
    }

    private static FakeHostResolver CreateLoopbackResolver()
    {
        FakeHostResolver resolver = new FakeHostResolver();
        resolver.Result.Addresses = new[] { System.Net.IPAddress.Loopback };
        resolver.Result.Status = WhoisExchangeStatus.Complete;
        return resolver;
    }

    private static WhoisClient CreateClient(IWhoisTransport transport)
    {
        return new WhoisClient(
            new WhoisQueryClassifier(),
            new WhoisQueryFormatter(),
            new IanaWhoisServerSelector(),
            transport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser());
    }

    private static WhoisLookupRequest CreateLookupRequest(WhoisEndpoint endpoint)
    {
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "example.com";
        request.EndpointOverride = endpoint;
        return request;
    }
}
