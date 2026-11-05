using whois;

namespace whois.Tests;

public class ClientLoopbackTraversalTests
{
    [Test]
    public void PreservesOrderedProvenanceAndReformatsEveryLoopbackReferralHop()
    {
        string registrarText =
            "Domain Name: EXAMPLE.COM\r\n" +
            "Registrar: Example Registrar\r\n";
        byte[] registrarBytes = System.Text.Encoding.UTF8.GetBytes(registrarText);
        using LoopbackWhoisServer registrarServer = new LoopbackWhoisServer(new[] { registrarBytes }, TimeSpan.Zero, false);

        string registrarAddress = "whois.registrar.test:" + registrarServer.Endpoint.Port;
        string registryText =
            "Domain Name: EXAMPLE.COM\r\n" +
            "Registrar WHOIS Server: " + registrarAddress + "\r\n";
        byte[] registryBytes = System.Text.Encoding.UTF8.GetBytes(registryText);
        using LoopbackWhoisServer registryServer = new LoopbackWhoisServer(new[] { registryBytes }, TimeSpan.Zero, false);

        string registryAddress = "whois.verisign-grs.com:" + registryServer.Endpoint.Port;
        string ianaText =
            "domain: COM\r\n" +
            "refer: " + registryAddress + "\r\n";
        byte[] ianaBytes = System.Text.Encoding.UTF8.GetBytes(ianaText);
        using LoopbackWhoisServer ianaServer = new LoopbackWhoisServer(new[] { ianaBytes }, TimeSpan.Zero, false);

        FakeServerSelector selector = new FakeServerSelector();
        selector.Result.Endpoint.Host = WhoisDefaults.IanaHost;
        selector.Result.Endpoint.Port = ianaServer.Endpoint.Port;
        selector.Result.Source = WhoisEndpointSource.InitialSelection;
        selector.Result.Reason = "Selected IANA for automatic discovery.";

        FakeHostResolver resolver = new FakeHostResolver();
        resolver.Result.Addresses = new[] { System.Net.IPAddress.Loopback };
        resolver.Result.Status = WhoisExchangeStatus.Complete;
        IWhoisEndpointPolicy policy = new WhoisAllowAllEndpointPolicy();
        IWhoisTransport transport = new WhoisTcpTransport(
            new WhoisQueryCodec(),
            resolver,
            policy,
            policy,
            new WhoisTcpConnectionFactory(),
            WhoisAddressPreference.System);
        WhoisClient client = new WhoisClient(
            new WhoisQueryClassifier(),
            new WhoisQueryFormatter(),
            selector,
            transport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser());
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "Example.COM.";

        WhoisLookupResult result = client.TryLookup(request);
        ianaServer.Completion.GetAwaiter().GetResult();
        registryServer.Completion.GetAwaiter().GetResult();
        registrarServer.Completion.GetAwaiter().GetResult();

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.None));
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Query.OriginalText, Is.EqualTo("Example.COM."));
        Assert.That(result.Query.NormalizedText, Is.EqualTo("example.com"));
        Assert.That(result.Query.Kind, Is.EqualTo(WhoisQueryKind.Domain));
        Assert.That(result.Hops, Has.Length.EqualTo(3));

        AssertHop(
            result.Hops[0],
            WhoisDefaults.IanaHost,
            ianaServer.Endpoint.Port,
            WhoisEndpointSource.InitialSelection,
            "example.com",
            ianaBytes,
            ianaText,
            new[] { "domain", "refer" },
            new[] { "COM", registryAddress });
        AssertReferral(
            result.Hops[0],
            "refer",
            2,
            registryAddress,
            WhoisReferralKind.Iana,
            0,
            "whois.verisign-grs.com",
            registryServer.Endpoint.Port);

        AssertHop(
            result.Hops[1],
            "whois.verisign-grs.com",
            registryServer.Endpoint.Port,
            WhoisEndpointSource.Referral,
            "domain example.com",
            registryBytes,
            registryText,
            new[] { "Domain Name", "Registrar WHOIS Server" },
            new[] { "EXAMPLE.COM", registrarAddress });
        AssertReferral(
            result.Hops[1],
            "Registrar WHOIS Server",
            2,
            registrarAddress,
            WhoisReferralKind.Registrar,
            1,
            "whois.registrar.test",
            registrarServer.Endpoint.Port);

        AssertHop(
            result.Hops[2],
            "whois.registrar.test",
            registrarServer.Endpoint.Port,
            WhoisEndpointSource.Referral,
            "example.com",
            registrarBytes,
            registrarText,
            new[] { "Domain Name", "Registrar" },
            new[] { "EXAMPLE.COM", "Example Registrar" });
        Assert.That(result.Hops[2].Referrals, Is.Empty);
        Assert.That(result.Hops[2].SelectedReferral, Is.Null);

        Assert.That(ianaServer.ReceivedQuery, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("example.com\r\n")));
        Assert.That(registryServer.ReceivedQuery, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("domain example.com\r\n")));
        Assert.That(registrarServer.ReceivedQuery, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("example.com\r\n")));
        Assert.That(ianaServer.ConnectionsAccepted, Is.EqualTo(1));
        Assert.That(registryServer.ConnectionsAccepted, Is.EqualTo(1));
        Assert.That(registrarServer.ConnectionsAccepted, Is.EqualTo(1));
        Assert.That(ianaServer.Error, Is.Null);
        Assert.That(registryServer.Error, Is.Null);
        Assert.That(registrarServer.Error, Is.Null);

        Assert.That(resolver.Endpoints, Has.Count.EqualTo(3));
        Assert.That(resolver.Endpoints[0].Host, Is.EqualTo(WhoisDefaults.IanaHost));
        Assert.That(resolver.Endpoints[1].Host, Is.EqualTo("whois.verisign-grs.com"));
        Assert.That(resolver.Endpoints[2].Host, Is.EqualTo("whois.registrar.test"));
    }

    private static void AssertHop(
        WhoisLookupHop hop,
        string host,
        int port,
        WhoisEndpointSource source,
        string queryText,
        byte[] expectedBytes,
        string expectedText,
        string[] fieldNames,
        string[] fieldValues)
    {
        Assert.That(hop.Selection.Endpoint.Host, Is.EqualTo(host));
        Assert.That(hop.Selection.Endpoint.Port, Is.EqualTo(port));
        Assert.That(hop.Selection.Endpoint.Scheme, Is.EqualTo(WhoisReferralScheme.Whois));
        Assert.That(hop.Selection.Source, Is.EqualTo(source));
        Assert.That(hop.RawResponse.Request.Endpoint.Host, Is.EqualTo(host));
        Assert.That(hop.RawResponse.Request.Endpoint.Port, Is.EqualTo(port));
        Assert.That(hop.RawResponse.Request.Source, Is.EqualTo(source));
        Assert.That(hop.RawResponse.Request.QueryText, Is.EqualTo(queryText));
        Assert.That(hop.RawResponse.ApprovedAddress, Is.EqualTo(System.Net.IPAddress.Loopback));
        Assert.That(hop.RawResponse.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(hop.RawResponse.Stage, Is.EqualTo(WhoisOperationStage.None));
        Assert.That(hop.RawResponse.Body, Is.EqualTo(expectedBytes));
        Assert.That(hop.RawResponse.IsTruncated, Is.False);
        Assert.That(hop.RawResponse.Error, Is.Null);
        Assert.That(hop.TextResponse.RawResponse, Is.SameAs(hop.RawResponse));
        Assert.That(hop.TextResponse.Text, Is.EqualTo(expectedText));
        Assert.That(hop.TextResponse.IsDecoded, Is.True);
        Assert.That(hop.TextResponse.Error, Is.Null);
        Assert.That(hop.ParsedResponse.TextResponse, Is.SameAs(hop.TextResponse));
        Assert.That(hop.ParsedResponse.IsParsed, Is.True);
        Assert.That(hop.ParsedResponse.Error, Is.Null);
        Assert.That(hop.ParsedResponse.Lines, Has.Length.EqualTo(fieldNames.Length));
        Assert.That(hop.ParsedResponse.Fields, Has.Length.EqualTo(fieldNames.Length));
        Assert.That(hop.ParsedResponse.Records, Has.Length.EqualTo(1));
        Assert.That(hop.ParsedResponse.Records[0].Fields, Has.Length.EqualTo(fieldNames.Length));

        int index = 0;
        while (index < fieldNames.Length)
        {
            WhoisLine line = hop.ParsedResponse.Lines[index];
            WhoisField field = hop.ParsedResponse.Fields[index];
            Assert.That(line.LineNumber, Is.EqualTo(index + 1));
            Assert.That(line.Kind, Is.EqualTo(WhoisLineKind.Field));
            Assert.That(line.FieldIndex, Is.EqualTo(index));
            Assert.That(field.Index, Is.EqualTo(index));
            Assert.That(field.RecordIndex, Is.EqualTo(0));
            Assert.That(field.SourceLine, Is.EqualTo(index + 1));
            Assert.That(field.OriginalName, Is.EqualTo(fieldNames[index]));
            Assert.That(field.Value, Is.EqualTo(fieldValues[index]));
            Assert.That(hop.ParsedResponse.Records[0].Fields[index], Is.SameAs(field));
            Assert.That(hop.ParsedResponse.Records[0].Lines[index], Is.SameAs(line));
            index = index + 1;
        }
    }

    private static void AssertReferral(
        WhoisLookupHop hop,
        string sourceField,
        int sourceLine,
        string advertisedValue,
        WhoisReferralKind kind,
        int priority,
        string host,
        int port)
    {
        Assert.That(hop.Referrals, Has.Length.EqualTo(1));
        WhoisReferral referral = hop.Referrals[0];
        Assert.That(hop.SelectedReferral, Is.SameAs(referral));
        Assert.That(referral.SourceField, Is.EqualTo(sourceField));
        Assert.That(referral.SourceLine, Is.EqualTo(sourceLine));
        Assert.That(referral.AdvertisedValue, Is.EqualTo(advertisedValue));
        Assert.That(referral.Scheme, Is.EqualTo(WhoisReferralScheme.Unspecified));
        Assert.That(referral.Kind, Is.EqualTo(kind));
        Assert.That(referral.FollowPriority, Is.EqualTo(priority));
        Assert.That(referral.IsValid, Is.True);
        Assert.That(referral.RejectionReason, Is.Empty);
        Assert.That(referral.Endpoint, Is.Not.Null);
        Assert.That(referral.Endpoint!.Scheme, Is.EqualTo(WhoisReferralScheme.Whois));
        Assert.That(referral.Endpoint.Host, Is.EqualTo(host));
        Assert.That(referral.Endpoint.Port, Is.EqualTo(port));
        Assert.That(referral.Endpoint.AdvertisedValue, Is.EqualTo(advertisedValue));
    }
}
