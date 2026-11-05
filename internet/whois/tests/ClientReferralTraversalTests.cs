using whois;

namespace whois.Tests;

public class ClientReferralTraversalTests
{
    [Test]
    public void ReferralDisablementRetainsDiscoveryWithoutFollowing()
    {
        FakeTransport transport = CreateSequencedTransport(new[]
        {
            "refer: whois.registry.example\r\n"
        });
        WhoisLookupRequest request = CreateRequest();
        request.Options.FollowReferrals = false;
        WhoisClient client = CreateClient(new IanaWhoisServerSelector(), transport);

        WhoisLookupResult result = client.TryLookup(request);

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.Hops, Has.Length.EqualTo(1));
        Assert.That(result.Hops[0].Referrals, Has.Length.EqualTo(1));
        Assert.That(result.Hops[0].SelectedReferral, Is.Null);
        Assert.That(transport.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public void SelectsLowestPriorityReferralWithStableSourceOrdering()
    {
        FakeTransport transport = CreateSequencedTransport(new[]
        {
            "ReferralServer: generic.example\r\n" +
                "Registrar WHOIS Server: registrar.example\r\n" +
                "refer: preferred.example\r\n" +
                "refer: equally-preferred-later.example\r\n",
            string.Empty
        });
        WhoisClient client = CreateClient(new IanaWhoisServerSelector(), transport);

        WhoisLookupResult result = client.TryLookup(CreateRequest());

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.Hops, Has.Length.EqualTo(2));
        WhoisLookupHop discovery = result.Hops[0];
        Assert.That(discovery.Referrals, Has.Length.EqualTo(4));
        Assert.That(discovery.SelectedReferral, Is.SameAs(discovery.Referrals[2]));
        Assert.That(discovery.SelectedReferral!.Endpoint!.Host, Is.EqualTo("preferred.example"));
        Assert.That(result.Hops[1].Selection.Endpoint.Host, Is.EqualTo("preferred.example"));
        Assert.That(result.Hops[1].Selection.Source, Is.EqualTo(WhoisEndpointSource.Referral));
        Assert.That(transport.Requests[1].Source, Is.EqualTo(WhoisEndpointSource.Referral));
        Assert.That(transport.Requests[1].QueryText, Is.EqualTo("example.com"));
    }

    [Test]
    public void DoesNotFollowRWhoisOrWebReferralsByDefault()
    {
        FakeTransport transport = CreateSequencedTransport(new[]
        {
            "ReferralServer: rwhois://rwhois.example:4321/auth-area=example\r\n" +
                "Referral Server: https://web.example/lookup\r\n"
        });
        WhoisClient client = CreateClient(new IanaWhoisServerSelector(), transport);

        WhoisLookupResult result = client.TryLookup(CreateRequest());

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.Hops, Has.Length.EqualTo(1));
        Assert.That(result.Hops[0].Referrals, Has.Length.EqualTo(2));
        Assert.That(result.Hops[0].Referrals[0].Kind, Is.EqualTo(WhoisReferralKind.RWhois));
        Assert.That(result.Hops[0].Referrals[1].Kind, Is.EqualTo(WhoisReferralKind.Web));
        Assert.That(result.Hops[0].SelectedReferral, Is.Null);
        Assert.That(transport.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public void EnforcesMaximumReferralHopCountBeforeAnotherExchange()
    {
        FakeTransport transport = CreateSequencedTransport(new[]
        {
            "refer: first.example\r\n",
            "refer: second.example\r\n"
        });
        WhoisLookupRequest request = CreateRequest();
        request.Options.Limits.MaximumReferralHops = 1;
        WhoisClient client = CreateClient(new IanaWhoisServerSelector(), transport);

        WhoisLookupResult result = client.TryLookup(request);

        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.Refer));
        Assert.That(result.Error!.Message, Is.EqualTo("The maximum WHOIS referral hop count was reached."));
        Assert.That(result.Hops, Has.Length.EqualTo(2));
        Assert.That(result.Hops[1].SelectedReferral, Is.SameAs(result.Hops[1].Referrals[0]));
        Assert.That(result.Hops[1].SelectedReferral!.Endpoint!.Host, Is.EqualTo("second.example"));
        Assert.That(transport.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public void DetectsEndpointAndQueryReferralLoopBeforeRepeatingExchange()
    {
        FakeServerSelector selector = new FakeServerSelector();
        selector.Result.Endpoint.Host = "whois.a.example";
        selector.Result.Endpoint.Port = 43;
        selector.Result.Source = WhoisEndpointSource.InitialSelection;
        FakeTransport transport = CreateSequencedTransport(new[]
        {
            "refer: whois.b.example\r\n",
            "refer: whois.a.example\r\n"
        });
        WhoisClient client = CreateClient(selector, transport);

        WhoisLookupResult result = client.TryLookup(CreateRequest());

        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.Refer));
        Assert.That(result.Error!.Message, Is.EqualTo("A WHOIS referral loop was detected."));
        Assert.That(result.Hops, Has.Length.EqualTo(2));
        Assert.That(result.Hops[1].SelectedReferral, Is.SameAs(result.Hops[1].Referrals[0]));
        Assert.That(transport.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public void DetectsSelfReferralBeforeRepeatingTheInitialExchange()
    {
        FakeServerSelector selector = new FakeServerSelector();
        selector.Result.Endpoint.Host = "whois.self.example";
        selector.Result.Endpoint.Port = 4343;
        selector.Result.Source = WhoisEndpointSource.InitialSelection;
        FakeTransport transport = CreateSequencedTransport(new[]
        {
            "refer: whois.self.example:4343\r\n"
        });
        WhoisClient client = CreateClient(selector, transport);

        WhoisLookupResult result = client.TryLookup(CreateRequest());

        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.Refer));
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error!.Message, Is.EqualTo("A WHOIS referral loop was detected."));
        Assert.That(result.Hops, Has.Length.EqualTo(1));
        Assert.That(result.Hops[0].Referrals, Has.Length.EqualTo(1));
        Assert.That(result.Hops[0].SelectedReferral, Is.SameAs(result.Hops[0].Referrals[0]));
        Assert.That(result.Hops[0].SelectedReferral!.Endpoint!.Host, Is.EqualTo("whois.self.example"));
        Assert.That(result.Hops[0].SelectedReferral!.Endpoint!.Port, Is.EqualTo(4343));
        Assert.That(transport.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public void RetainsStandaloneRWhoisReferralWithoutFollowingIt()
    {
        FakeTransport transport = CreateSequencedTransport(new[]
        {
            "ReferralServer: rwhois://rwhois.example:4321/auth-area=example\r\n"
        });
        WhoisClient client = CreateClient(new IanaWhoisServerSelector(), transport);

        WhoisLookupResult result = client.TryLookup(CreateRequest());

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.None));
        Assert.That(result.Hops, Has.Length.EqualTo(1));
        Assert.That(result.Hops[0].Referrals, Has.Length.EqualTo(1));
        WhoisReferral referral = result.Hops[0].Referrals[0];
        Assert.That(referral.IsValid, Is.True);
        Assert.That(referral.Scheme, Is.EqualTo(WhoisReferralScheme.RWhois));
        Assert.That(referral.Kind, Is.EqualTo(WhoisReferralKind.RWhois));
        Assert.That(referral.FollowPriority, Is.EqualTo(-1));
        Assert.That(referral.Endpoint!.Host, Is.EqualTo("rwhois.example"));
        Assert.That(referral.Endpoint.Port, Is.EqualTo(4321));
        Assert.That(referral.PathSuffix, Is.EqualTo("/auth-area=example"));
        Assert.That(result.Hops[0].SelectedReferral, Is.Null);
        Assert.That(transport.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public void RetainsWebOnlyReferralWithoutFollowingIt()
    {
        FakeTransport transport = CreateSequencedTransport(new[]
        {
            "Referral Server: https://web.example/lookup\r\n"
        });
        WhoisClient client = CreateClient(new IanaWhoisServerSelector(), transport);

        WhoisLookupResult result = client.TryLookup(CreateRequest());

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.None));
        Assert.That(result.Hops, Has.Length.EqualTo(1));
        Assert.That(result.Hops[0].Referrals, Has.Length.EqualTo(1));
        WhoisReferral referral = result.Hops[0].Referrals[0];
        Assert.That(referral.IsValid, Is.True);
        Assert.That(referral.Scheme, Is.EqualTo(WhoisReferralScheme.Https));
        Assert.That(referral.Kind, Is.EqualTo(WhoisReferralKind.Web));
        Assert.That(referral.FollowPriority, Is.EqualTo(-1));
        Assert.That(referral.Endpoint!.Host, Is.EqualTo("web.example"));
        Assert.That(referral.Endpoint.Port, Is.EqualTo(443));
        Assert.That(referral.PathSuffix, Is.EqualTo("/lookup"));
        Assert.That(result.Hops[0].SelectedReferral, Is.Null);
        Assert.That(transport.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public void CompletesAtAResponseWithoutAnyReferral()
    {
        FakeTransport transport = CreateSequencedTransport(new[]
        {
            "Domain Name: EXAMPLE.COM\r\nRegistrar: Example Registrar\r\n"
        });
        WhoisClient client = CreateClient(new IanaWhoisServerSelector(), transport);

        WhoisLookupResult result = client.TryLookup(CreateRequest());

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.None));
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Hops, Has.Length.EqualTo(1));
        Assert.That(result.Hops[0].ParsedResponse.Fields, Has.Length.EqualTo(2));
        Assert.That(result.Hops[0].Referrals, Is.Empty);
        Assert.That(result.Hops[0].SelectedReferral, Is.Null);
        Assert.That(transport.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public void RetainsMalformedReferralsAndCompletesWithoutFollowingThem()
    {
        FakeTransport transport = CreateSequencedTransport(new[]
        {
            "refer: missing-port.example:\r\n" +
                "ReferralServer: ftp://unsupported.example\r\n" +
                "%referral rwhois://\r\n"
        });
        WhoisClient client = CreateClient(new IanaWhoisServerSelector(), transport);

        WhoisLookupResult result = client.TryLookup(CreateRequest());

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.None));
        Assert.That(result.Hops, Has.Length.EqualTo(1));
        Assert.That(result.Hops[0].Referrals, Has.Length.EqualTo(3));
        Assert.That(result.Hops[0].Referrals[0].AdvertisedValue, Is.EqualTo("missing-port.example:"));
        Assert.That(result.Hops[0].Referrals[0].RejectionReason, Is.EqualTo("The endpoint port is invalid."));
        Assert.That(result.Hops[0].Referrals[1].Scheme, Is.EqualTo(WhoisReferralScheme.Unsupported));
        Assert.That(result.Hops[0].Referrals[1].RejectionReason, Is.EqualTo("The endpoint URI scheme is not supported."));
        Assert.That(result.Hops[0].Referrals[2].Scheme, Is.EqualTo(WhoisReferralScheme.RWhois));
        Assert.That(result.Hops[0].Referrals[2].RejectionReason, Is.EqualTo("The endpoint host is missing."));
        Assert.That(result.Hops[0].Referrals, Has.All.Matches<WhoisReferral>((referral) => !referral.IsValid));
        Assert.That(result.Hops[0].Referrals, Has.All.Matches<WhoisReferral>((referral) => referral.Endpoint == null));
        Assert.That(result.Hops[0].SelectedReferral, Is.Null);
        Assert.That(transport.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public void CanHideAutomaticallySelectedIanaDiscoveryHop()
    {
        FakeTransport transport = CreateSequencedTransport(new[]
        {
            "refer: whois.registry.example\r\n",
            "domain: example.com\r\n"
        });
        WhoisLookupRequest request = CreateRequest();
        request.Options.RetainDiscoveryResponses = false;
        WhoisClient client = CreateClient(new IanaWhoisServerSelector(), transport);

        WhoisLookupResult result = client.TryLookup(request);

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.Hops, Has.Length.EqualTo(1));
        Assert.That(result.Hops[0].Selection.Endpoint.Host, Is.EqualTo("whois.registry.example"));
        Assert.That(result.Hops[0].Selection.Source, Is.EqualTo(WhoisEndpointSource.Referral));
        Assert.That(transport.Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public void CallerOverrideStaysVisibleWhileReferralUsesAutomaticPolicySource()
    {
        FakeTransport transport = CreateSequencedTransport(new[]
        {
            "refer: whois.registry.example\r\n",
            string.Empty
        });
        WhoisLookupRequest request = CreateRequest();
        request.EndpointOverride = new WhoisEndpoint();
        request.EndpointOverride.Host = "whois.caller.example";
        request.Options.RetainDiscoveryResponses = false;
        WhoisClient client = CreateClient(new IanaWhoisServerSelector(), transport);

        WhoisLookupResult result = client.TryLookup(request);

        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.Hops, Has.Length.EqualTo(2));
        Assert.That(result.Hops[0].Selection.Source, Is.EqualTo(WhoisEndpointSource.Direct));
        Assert.That(result.Hops[1].Selection.Source, Is.EqualTo(WhoisEndpointSource.Referral));
        Assert.That(transport.Requests[0].Source, Is.EqualTo(WhoisEndpointSource.Direct));
        Assert.That(transport.Requests[1].Source, Is.EqualTo(WhoisEndpointSource.Referral));
    }

    private static FakeTransport CreateSequencedTransport(string[] bodies)
    {
        FakeTransport transport = new FakeTransport();
        transport.ExchangeHandler = (request) =>
        {
            int responseIndex = transport.Requests.Count - 1;
            WhoisRawResponse response = new WhoisRawResponse();
            response.Request = request;
            response.Body = System.Text.Encoding.UTF8.GetBytes(bodies[responseIndex]);
            response.Status = WhoisExchangeStatus.Complete;
            return response;
        };
        return transport;
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

    private static WhoisLookupRequest CreateRequest()
    {
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "example.com";
        return request;
    }
}
