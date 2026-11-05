using whois;

namespace whois.Tests;

public class ApiLookupDataTests
{
    [Test]
    public void ReferralRetainsAdvertisedAndParsedMetadata()
    {
        WhoisEndpoint endpoint = new WhoisEndpoint();
        WhoisReferral referral = new WhoisReferral();

        endpoint.Scheme = WhoisReferralScheme.RWhois;
        endpoint.Host = "rwhois.example";
        endpoint.Port = WhoisDefaults.RWhoisPort;
        endpoint.AdvertisedValue = "rwhois://rwhois.example:4321/auth-area=example";
        referral.SourceField = WhoisFieldNames.ReferralDirective;
        referral.SourceLine = 17;
        referral.AdvertisedValue = endpoint.AdvertisedValue;
        referral.Scheme = WhoisReferralScheme.RWhois;
        referral.Kind = WhoisReferralKind.RWhois;
        referral.Endpoint = endpoint;
        referral.PathSuffix = "/auth-area=example";
        referral.IsValid = true;

        Assert.That(referral.SourceField, Is.EqualTo("%referral"));
        Assert.That(referral.SourceLine, Is.EqualTo(17));
        Assert.That(referral.AdvertisedValue, Is.EqualTo(endpoint.AdvertisedValue));
        Assert.That(referral.Scheme, Is.EqualTo(WhoisReferralScheme.RWhois));
        Assert.That(referral.Kind, Is.EqualTo(WhoisReferralKind.RWhois));
        Assert.That(referral.FollowPriority, Is.EqualTo(-1));
        Assert.That(referral.Endpoint, Is.SameAs(endpoint));
        Assert.That(referral.PathSuffix, Is.EqualTo("/auth-area=example"));
        Assert.That(referral.IsValid, Is.True);
        Assert.That(referral.RejectionReason, Is.Empty);
    }

    [Test]
    public void LookupHopHasSafeEmptyProvenanceDefaults()
    {
        WhoisLookupHop hop = new WhoisLookupHop();

        Assert.That(hop.Selection, Is.Not.Null);
        Assert.That(hop.RawResponse, Is.Not.Null);
        Assert.That(hop.TextResponse, Is.Not.Null);
        Assert.That(hop.ParsedResponse, Is.Not.Null);
        Assert.That(hop.Referrals, Is.Empty);
        Assert.That(hop.SelectedReferral, Is.Null);
        Assert.That(new WhoisReferral().FollowPriority, Is.EqualTo(-1));
    }

    [Test]
    public void LookupResultPreservesOrderedHopsAndPartialFailure()
    {
        WhoisLookupHop discoveryHop = new WhoisLookupHop();
        WhoisLookupHop registryHop = new WhoisLookupHop();
        WhoisReferral referral = new WhoisReferral();
        WhoisEndpoint registryEndpoint = new WhoisEndpoint();
        WhoisLookupResult result = new WhoisLookupResult();
        IOException error = new IOException("registry read failed");

        result.Query.OriginalText = "Example.COM.";
        result.Query.NormalizedText = "example.com";
        result.Query.RoutingKey = "domain:example.com";
        result.Query.Kind = WhoisQueryKind.Domain;
        result.Query.IsValid = true;

        discoveryHop.Selection.Endpoint.Host = WhoisDefaults.IanaHost;
        discoveryHop.Selection.Source = WhoisEndpointSource.InitialSelection;
        discoveryHop.RawResponse.Body = new byte[] { 114, 101, 102, 101, 114 };
        discoveryHop.RawResponse.Status = WhoisExchangeStatus.Complete;
        discoveryHop.TextResponse.RawResponse = discoveryHop.RawResponse;
        discoveryHop.TextResponse.Text = "refer: whois.registry.example\r\n";
        discoveryHop.TextResponse.IsDecoded = true;
        discoveryHop.ParsedResponse.TextResponse = discoveryHop.TextResponse;
        discoveryHop.ParsedResponse.IsParsed = true;

        referral.SourceField = WhoisFieldNames.Refer;
        referral.SourceLine = 1;
        referral.AdvertisedValue = "whois.registry.example";
        referral.Scheme = WhoisReferralScheme.Whois;
        referral.Kind = WhoisReferralKind.Iana;
        registryEndpoint.Host = "whois.registry.example";
        referral.Endpoint = registryEndpoint;
        referral.IsValid = true;
        discoveryHop.Referrals = new[] { referral };
        discoveryHop.SelectedReferral = referral;

        registryHop.Selection.Endpoint = registryEndpoint;
        registryHop.Selection.Source = WhoisEndpointSource.Referral;
        registryHop.RawResponse.Body = new byte[] { 112, 97, 114, 116, 105, 97, 108 };
        registryHop.RawResponse.Status = WhoisExchangeStatus.ReadFailed;
        registryHop.RawResponse.Stage = WhoisOperationStage.Read;
        registryHop.RawResponse.Error = error;
        registryHop.TextResponse.RawResponse = registryHop.RawResponse;
        registryHop.ParsedResponse.TextResponse = registryHop.TextResponse;

        result.Hops = new[] { discoveryHop, registryHop };
        result.IsComplete = false;
        result.FailedStage = WhoisOperationStage.Read;
        result.Error = error;

        Assert.That(result.Query.NormalizedText, Is.EqualTo("example.com"));
        Assert.That(result.Hops, Is.EqualTo(new[] { discoveryHop, registryHop }));
        Assert.That(result.Hops[0].RawResponse.Body, Is.EqualTo(new byte[] { 114, 101, 102, 101, 114 }));
        Assert.That(result.Hops[0].TextResponse.RawResponse, Is.SameAs(discoveryHop.RawResponse));
        Assert.That(result.Hops[0].ParsedResponse.TextResponse, Is.SameAs(discoveryHop.TextResponse));
        Assert.That(result.Hops[0].SelectedReferral, Is.SameAs(referral));
        Assert.That(result.Hops[1].Selection.Endpoint, Is.SameAs(registryEndpoint));
        Assert.That(result.Hops[1].RawResponse.Body, Is.EqualTo(new byte[] { 112, 97, 114, 116, 105, 97, 108 }));
        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.FailedStage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(result.Error, Is.SameAs(error));
    }
}
