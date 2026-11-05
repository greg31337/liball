using whois;

namespace whois.Tests;

public class ClientTryLookupTests
{
    [Test]
    public void BuildsCompleteOrderedInitialHopWithAllDerivedProvenance()
    {
        FakeTransport transport = new FakeTransport();
        WhoisRawResponse raw = new WhoisRawResponse();
        raw.Body = System.Text.Encoding.UTF8.GetBytes("refer: whois.registry.example\r\n");
        raw.Status = WhoisExchangeStatus.Complete;
        transport.ExchangeHandler = (exchangeRequest) =>
        {
            raw.Request = exchangeRequest;
            return raw;
        };
        WhoisClient client = CreateClient(new IanaWhoisServerSelector(), transport);
        WhoisLookupRequest request = CreateLookupRequest();
        request.Options.FollowReferrals = false;

        WhoisLookupResult result = client.TryLookup(request);

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.None));
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Query.OriginalText, Is.EqualTo("Example.COM."));
        Assert.That(result.Query.NormalizedText, Is.EqualTo("example.com"));
        Assert.That(result.Query.Kind, Is.EqualTo(WhoisQueryKind.Domain));
        Assert.That(result.Hops, Has.Length.EqualTo(1));
        WhoisLookupHop hop = result.Hops[0];
        Assert.That(hop.Selection.Endpoint.Host, Is.EqualTo(WhoisDefaults.IanaHost));
        Assert.That(hop.Selection.Endpoint.Port, Is.EqualTo(WhoisDefaults.WhoisPort));
        Assert.That(hop.Selection.Source, Is.EqualTo(WhoisEndpointSource.InitialSelection));
        Assert.That(hop.RawResponse, Is.SameAs(raw));
        Assert.That(hop.RawResponse.Request.QueryText, Is.EqualTo("example.com"));
        Assert.That(hop.RawResponse.Request.Source, Is.EqualTo(WhoisEndpointSource.InitialSelection));
        Assert.That(hop.RawResponse.Request.Encoding, Is.Not.SameAs(request.Options.Encoding));
        Assert.That(hop.RawResponse.Request.Timeouts, Is.Not.SameAs(request.Options.Timeouts));
        Assert.That(hop.RawResponse.Request.Limits, Is.Not.SameAs(request.Options.Limits));
        Assert.That(hop.RawResponse.Request.Timeouts.ReadIdle, Is.EqualTo(TimeSpan.FromSeconds(11)));
        Assert.That(hop.RawResponse.Request.Limits.MaximumResponseBytes, Is.EqualTo(303));
        Assert.That(hop.TextResponse.RawResponse, Is.SameAs(raw));
        Assert.That(hop.TextResponse.Text, Is.EqualTo("refer: whois.registry.example\r\n"));
        Assert.That(hop.TextResponse.IsDecoded, Is.True);
        Assert.That(hop.ParsedResponse.TextResponse, Is.SameAs(hop.TextResponse));
        Assert.That(hop.ParsedResponse.IsParsed, Is.True);
        Assert.That(hop.ParsedResponse.Fields, Has.Length.EqualTo(1));
        Assert.That(hop.Referrals, Has.Length.EqualTo(1));
        Assert.That(hop.Referrals[0].SourceField, Is.EqualTo("refer"));
        Assert.That(hop.Referrals[0].Endpoint!.Host, Is.EqualTo("whois.registry.example"));
        Assert.That(hop.SelectedReferral, Is.Null);
        Assert.That(transport.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public void AppliesSelectionQueryOverrideBeforeTheExchange()
    {
        FakeServerSelector selector = new FakeServerSelector();
        selector.Result.Endpoint.Host = "whois.custom.example";
        selector.Result.Endpoint.Port = 4343;
        selector.Result.Source = WhoisEndpointSource.InitialSelection;
        selector.Result.QueryOverride = "CUSTOM search syntax";
        selector.Result.Reason = "Custom selector template.";
        FakeTransport transport = new FakeTransport();
        transport.ExchangeHandler = (exchangeRequest) =>
        {
            WhoisRawResponse raw = new WhoisRawResponse();
            raw.Request = exchangeRequest;
            raw.Status = WhoisExchangeStatus.Complete;
            return raw;
        };
        WhoisClient client = CreateClient(selector, transport);
        WhoisLookupRequest request = CreateLookupRequest();

        WhoisLookupResult result = client.TryLookup(request);

        Assert.That(result.IsComplete, Is.True);
        Assert.That(selector.Queries, Has.Count.EqualTo(1));
        Assert.That(selector.Requests, Has.Count.EqualTo(1));
        Assert.That(result.Hops[0].Selection.QueryOverride, Is.EqualTo("CUSTOM search syntax"));
        Assert.That(result.Hops[0].Selection.Reason, Is.EqualTo("Custom selector template."));
        Assert.That(result.Hops[0].RawResponse.Request.QueryText, Is.EqualTo("CUSTOM search syntax"));
        Assert.That(result.Hops[0].RawResponse.Request.Endpoint.Host, Is.EqualTo("whois.custom.example"));
        Assert.That(result.Hops[0].RawResponse.Request.Endpoint.Port, Is.EqualTo(4343));
    }

    [Test]
    public void RetainsPartialInitialHopWhenTransportFails()
    {
        FakeTransport transport = new FakeTransport();
        IOException error = new IOException("partial read failed");
        WhoisRawResponse partial = new WhoisRawResponse();
        partial.Body = new byte[] { 112, 97, 114, 116 };
        partial.Status = WhoisExchangeStatus.ReadFailed;
        partial.Stage = WhoisOperationStage.Read;
        partial.Error = error;
        transport.ExchangeHandler = (exchangeRequest) =>
        {
            partial.Request = exchangeRequest;
            return partial;
        };
        WhoisClient client = CreateClient(new IanaWhoisServerSelector(), transport);

        WhoisLookupResult result = client.TryLookup(CreateLookupRequest());

        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(result.Error, Is.SameAs(error));
        Assert.That(result.Hops, Has.Length.EqualTo(1));
        Assert.That(result.Hops[0].RawResponse, Is.SameAs(partial));
        Assert.That(result.Hops[0].RawResponse.Body, Is.EqualTo(new byte[] { 112, 97, 114, 116 }));
        Assert.That(result.Hops[0].TextResponse.RawResponse, Is.SameAs(partial));
        Assert.That(result.Hops[0].TextResponse.IsDecoded, Is.False);
        Assert.That(result.Hops[0].Referrals, Is.Empty);
    }

    [Test]
    public void ReturnsSelectionFailureWithoutStartingAnExchange()
    {
        FakeServerSelector selector = new FakeServerSelector();
        WhoisServerNotFoundException expected = new WhoisServerNotFoundException("No server.", new WhoisQuery(), null);
        selector.SelectHandler = (query, request) => throw expected;
        FakeTransport transport = new FakeTransport();
        WhoisClient client = CreateClient(selector, transport);

        WhoisLookupResult result = client.TryLookup(CreateLookupRequest());

        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.Select));
        Assert.That(result.Error, Is.SameAs(expected));
        Assert.That(result.Query.NormalizedText, Is.EqualTo("example.com"));
        Assert.That(result.Hops, Is.Empty);
        Assert.That(transport.Requests, Is.Empty);
    }

    private static WhoisClient CreateClient(IWhoisServerSelector selector, IWhoisTransport transport)
    {
        return new WhoisClient(
            new WhoisQueryClassifier(),
            new WhoisQueryFormatter(),
            selector,
            transport,
            new WhoisResponseDecoder(),
            new WhoisResponseParser(),
            new WhoisReferralParser());
    }

    private static WhoisLookupRequest CreateLookupRequest()
    {
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "Example.COM.";
        request.Options.Timeouts.Resolve = TimeSpan.FromSeconds(6);
        request.Options.Timeouts.Connect = TimeSpan.FromSeconds(7);
        request.Options.Timeouts.Write = TimeSpan.FromSeconds(8);
        request.Options.Timeouts.ReadIdle = TimeSpan.FromSeconds(11);
        request.Options.Timeouts.Overall = TimeSpan.FromSeconds(30);
        request.Options.Limits.MaximumResponseBytes = 303;
        return request;
    }
}
