using whois;

namespace whois.Tests;

public class IanaWhoisServerSelectorTests
{
    [TestCase(WhoisQueryKind.Domain)]
    [TestCase(WhoisQueryKind.IPv4Address)]
    [TestCase(WhoisQueryKind.IPv6Address)]
    [TestCase(WhoisQueryKind.IPv4Prefix)]
    [TestCase(WhoisQueryKind.IPv6Prefix)]
    [TestCase(WhoisQueryKind.AutonomousSystem)]
    [TestCase(WhoisQueryKind.ReverseDomain)]
    public void RoutesEveryClassifiedResourceKindToIana(WhoisQueryKind kind)
    {
        WhoisQuery query = CreateQuery(kind, "normalized-resource");
        WhoisLookupRequest request = new WhoisLookupRequest();
        IWhoisServerSelector selector = new IanaWhoisServerSelector();

        WhoisServerSelection selection = selector.SelectServer(query, request);

        Assert.That(selection.Endpoint.Scheme, Is.EqualTo(WhoisReferralScheme.Whois));
        Assert.That(selection.Endpoint.Host, Is.EqualTo(WhoisDefaults.IanaHost));
        Assert.That(selection.Endpoint.Port, Is.EqualTo(WhoisDefaults.WhoisPort));
        Assert.That(selection.Endpoint.AdvertisedValue, Is.EqualTo(WhoisDefaults.IanaHost));
        Assert.That(selection.Source, Is.EqualTo(WhoisEndpointSource.InitialSelection));
        Assert.That(selection.QueryOverride, Is.Null);
        Assert.That(selection.Reason, Is.EqualTo("Selected the IANA WHOIS bootstrap server."));
    }

    [Test]
    public void NormalizesAndCopiesCallerEndpointOverride()
    {
        WhoisQuery query = CreateQuery(WhoisQueryKind.Domain, "example.test");
        WhoisEndpoint endpointOverride = new WhoisEndpoint();
        endpointOverride.Host = "MÜNICH.Example.";
        endpointOverride.Port = 4343;
        endpointOverride.AdvertisedValue = "caller endpoint";
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.EndpointOverride = endpointOverride;
        IWhoisServerSelector selector = new IanaWhoisServerSelector();

        WhoisServerSelection selection = selector.SelectServer(query, request);

        Assert.That(selection.Endpoint, Is.Not.SameAs(endpointOverride));
        Assert.That(selection.Endpoint.Scheme, Is.EqualTo(WhoisReferralScheme.Whois));
        Assert.That(selection.Endpoint.Host, Is.EqualTo("xn--mnich-kva.example"));
        Assert.That(selection.Endpoint.Port, Is.EqualTo(4343));
        Assert.That(selection.Endpoint.AdvertisedValue, Is.EqualTo("caller endpoint"));
        Assert.That(selection.Source, Is.EqualTo(WhoisEndpointSource.Direct));
        Assert.That(selection.QueryOverride, Is.Null);
        Assert.That(selection.Reason, Is.EqualTo("Selected the caller-provided endpoint override."));
        Assert.That(endpointOverride.Host, Is.EqualTo("MÜNICH.Example."));
    }

    [Test]
    public void CallerOverrideRoutesFreeFormQueryWithoutAutomaticGuessing()
    {
        WhoisQuery query = CreateQuery(WhoisQueryKind.FreeForm, "custom search");
        WhoisEndpoint endpointOverride = new WhoisEndpoint();
        endpointOverride.Host = "whois.custom.example";
        endpointOverride.Port = 1043;
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.EndpointOverride = endpointOverride;
        IWhoisServerSelector selector = new IanaWhoisServerSelector();

        WhoisServerSelection selection = selector.SelectServer(query, request);

        Assert.That(selection.Endpoint.Host, Is.EqualTo("whois.custom.example"));
        Assert.That(selection.Endpoint.Port, Is.EqualTo(1043));
        Assert.That(selection.Source, Is.EqualTo(WhoisEndpointSource.Direct));
    }

    [Test]
    public void FreeFormAutomaticSelectionThrowsTypedNoServerFailure()
    {
        WhoisQuery query = CreateQuery(WhoisQueryKind.FreeForm, "custom search");
        WhoisLookupRequest request = new WhoisLookupRequest();
        IWhoisServerSelector selector = new IanaWhoisServerSelector();

        WhoisServerNotFoundException exception = Assert.Throws<WhoisServerNotFoundException>(() => selector.SelectServer(query, request))!;

        Assert.That(exception.Message, Is.EqualTo("No automatic WHOIS server is available for the classified query."));
        Assert.That(exception.Query, Is.SameAs(query));
        Assert.That(exception.InnerException, Is.Null);
    }

    [Test]
    public void InvalidCallerOverrideThrowsQueryFailureWithEndpointContext()
    {
        WhoisQuery query = CreateQuery(WhoisQueryKind.Domain, "example.test");
        WhoisEndpoint endpointOverride = new WhoisEndpoint();
        endpointOverride.Host = "whois.invalid.example";
        endpointOverride.Port = 0;
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.EndpointOverride = endpointOverride;
        IWhoisServerSelector selector = new IanaWhoisServerSelector();

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => selector.SelectServer(query, request))!;

        Assert.That(exception.InvalidParameter, Is.EqualTo("Endpoint.Port"));
        Assert.That(exception.QueryText, Is.EqualTo("example.test"));
        Assert.That(exception.QueryKind, Is.EqualTo(WhoisQueryKind.Domain));
        Assert.That(exception.Endpoint, Is.SameAs(endpointOverride));
    }

    [Test]
    public void RejectsNullSelectorInputs()
    {
        IWhoisServerSelector selector = new IanaWhoisServerSelector();
        WhoisQuery query = CreateQuery(WhoisQueryKind.Domain, "example.test");
        WhoisLookupRequest request = new WhoisLookupRequest();

        Assert.Throws<ArgumentNullException>(() => selector.SelectServer(null!, request));
        Assert.Throws<ArgumentNullException>(() => selector.SelectServer(query, null!));
    }

    [TestCase("Example.COM.", WhoisQueryKind.Domain, "example.com")]
    [TestCase("BÜCHER.Example.", WhoisQueryKind.Domain, "xn--bcher-kva.example")]
    [TestCase("192.0.2.129", WhoisQueryKind.IPv4Address, "192.0.2.129")]
    [TestCase("2001:0DB8::1", WhoisQueryKind.IPv6Address, "2001:db8::1")]
    [TestCase("192.0.2.129/24", WhoisQueryKind.IPv4Prefix, "192.0.2.0/24")]
    [TestCase("2001:db8::1234/64", WhoisQueryKind.IPv6Prefix, "2001:db8::/64")]
    [TestCase("AS00123", WhoisQueryKind.AutonomousSystem, "AS123")]
    [TestCase("1.0.0.127.IN-ADDR.ARPA.", WhoisQueryKind.ReverseDomain, "1.0.0.127.in-addr.arpa")]
    [TestCase("8.B.D.0.1.0.0.2.IP6.ARPA.", WhoisQueryKind.ReverseDomain, "8.b.d.0.1.0.0.2.ip6.arpa")]
    public void ClassifiesAndRoutesSupportedInputsThroughIana(string input, WhoisQueryKind expectedKind, string expectedNormalized)
    {
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = input;
        IWhoisQueryClassifier classifier = new WhoisQueryClassifier();
        IWhoisServerSelector selector = new IanaWhoisServerSelector();

        WhoisQuery query = classifier.Classify(request);
        WhoisServerSelection selection = selector.SelectServer(query, request);

        Assert.That(query.Kind, Is.EqualTo(expectedKind));
        Assert.That(query.NormalizedText, Is.EqualTo(expectedNormalized));
        Assert.That(selection.Endpoint.Host, Is.EqualTo(WhoisDefaults.IanaHost));
        Assert.That(selection.Endpoint.Port, Is.EqualTo(WhoisDefaults.WhoisPort));
        Assert.That(selection.Source, Is.EqualTo(WhoisEndpointSource.InitialSelection));
        Assert.That(selection.QueryOverride, Is.Null);
    }

    [TestCase("WHOIS.Override.Example.", 43, "whois.override.example")]
    [TestCase("BÜCHER.Override.Example.", 4343, "xn--bcher-kva.override.example")]
    [TestCase("192.0.2.44", 1043, "192.0.2.44")]
    [TestCase("2001:0DB8::44", 43, "2001:db8::44")]
    [TestCase("[2001:0DB8::45]", 4343, "2001:db8::45")]
    public void NormalizesDnsIdnAndAddressEndpointOverrides(string overrideHost, int overridePort, string expectedHost)
    {
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "example.test";
        request.EndpointOverride = new WhoisEndpoint();
        request.EndpointOverride.Host = overrideHost;
        request.EndpointOverride.Port = overridePort;
        request.EndpointOverride.AdvertisedValue = overrideHost;
        WhoisQuery query = new WhoisQueryClassifier().Classify(request);
        IWhoisServerSelector selector = new IanaWhoisServerSelector();

        WhoisServerSelection selection = selector.SelectServer(query, request);

        Assert.That(selection.Endpoint.Host, Is.EqualTo(expectedHost));
        Assert.That(selection.Endpoint.Port, Is.EqualTo(overridePort));
        Assert.That(selection.Endpoint.AdvertisedValue, Is.EqualTo(overrideHost));
        Assert.That(selection.Endpoint.Scheme, Is.EqualTo(WhoisReferralScheme.Whois));
        Assert.That(selection.Source, Is.EqualTo(WhoisEndpointSource.Direct));
        Assert.That(selection.QueryOverride, Is.Null);
    }

    [Test]
    public void BuiltInSelectionsLeaveQueryOverrideUnsetForNormalFormatting()
    {
        WhoisLookupRequest automaticRequest = new WhoisLookupRequest();
        automaticRequest.Text = "example.test";
        WhoisLookupRequest callerRequest = new WhoisLookupRequest();
        callerRequest.Text = "example.test";
        callerRequest.EndpointOverride = new WhoisEndpoint();
        callerRequest.EndpointOverride.Host = "whois.caller.example";
        IWhoisQueryClassifier classifier = new WhoisQueryClassifier();
        IWhoisServerSelector selector = new IanaWhoisServerSelector();

        WhoisServerSelection automatic = selector.SelectServer(classifier.Classify(automaticRequest), automaticRequest);
        WhoisServerSelection caller = selector.SelectServer(classifier.Classify(callerRequest), callerRequest);

        Assert.That(automatic.QueryOverride, Is.Null);
        Assert.That(caller.QueryOverride, Is.Null);
    }

    [Test]
    public void ClassifiedFreeFormInputRetainsContextInNoServerFailure()
    {
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = "domain example.test";
        IWhoisQueryClassifier classifier = new WhoisQueryClassifier();
        IWhoisServerSelector selector = new IanaWhoisServerSelector();
        WhoisQuery query = classifier.Classify(request);

        WhoisServerNotFoundException exception = Assert.Throws<WhoisServerNotFoundException>(() => selector.SelectServer(query, request))!;

        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.FreeForm));
        Assert.That(exception.Query, Is.SameAs(query));
        Assert.That(exception.Query.OriginalText, Is.EqualTo("domain example.test"));
        Assert.That(exception.Query.NormalizedText, Is.EqualTo("domain example.test"));
    }

    private static WhoisQuery CreateQuery(WhoisQueryKind kind, string normalizedText)
    {
        WhoisQuery query = new WhoisQuery();
        query.OriginalText = normalizedText;
        query.NormalizedText = normalizedText;
        query.RoutingKey = normalizedText.ToLowerInvariant();
        query.Kind = kind;
        query.IsValid = true;
        return query;
    }
}
