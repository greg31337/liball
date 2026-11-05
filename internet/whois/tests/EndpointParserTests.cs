using whois;

namespace whois.Tests;

public class EndpointParserTests
{
    [Test]
    public void ParsesBareDnsHostWithImplicitWhoisDefaults()
    {
        EndpointParseResult result = EndpointParser.ParseValue("Whois.Example.COM");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Scheme, Is.EqualTo(WhoisReferralScheme.Unspecified));
        Assert.That(result.Endpoint, Is.Not.Null);
        Assert.That(result.Endpoint!.Scheme, Is.EqualTo(WhoisReferralScheme.Whois));
        Assert.That(result.Endpoint.Host, Is.EqualTo("whois.example.com"));
        Assert.That(result.Endpoint.Port, Is.EqualTo(43));
        Assert.That(result.Endpoint.AdvertisedValue, Is.EqualTo("Whois.Example.COM"));
        Assert.That(result.PathSuffix, Is.Empty);
        Assert.That(result.EqualityKey, Is.EqualTo("whois://whois.example.com:43"));
        Assert.That(result.RejectionReason, Is.Empty);
    }

    [Test]
    public void NormalizesIdnHostAndOptionalRootDot()
    {
        EndpointParseResult result = EndpointParser.ParseValue("MÜNICH.example.:4343");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Endpoint!.Host, Is.EqualTo("xn--mnich-kva.example"));
        Assert.That(result.Endpoint.Port, Is.EqualTo(4343));
        Assert.That(result.EqualityKey, Is.EqualTo("whois://xn--mnich-kva.example:4343"));
    }

    [Test]
    public void ParsesIpv4WithExplicitPort()
    {
        EndpointParseResult result = EndpointParser.ParseValue("192.0.2.10:4343");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Endpoint!.Host, Is.EqualTo("192.0.2.10"));
        Assert.That(result.Endpoint.Port, Is.EqualTo(4343));
        Assert.That(result.EqualityKey, Is.EqualTo("whois://192.0.2.10:4343"));
    }

    [Test]
    public void ParsesAndCanonicalizesBracketedIpv6()
    {
        EndpointParseResult result = EndpointParser.ParseValue("[2001:0DB8:0:0::1]:4343");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Endpoint!.Host, Is.EqualTo("2001:db8::1"));
        Assert.That(result.Endpoint.Port, Is.EqualTo(4343));
        Assert.That(result.EqualityKey, Is.EqualTo("whois://[2001:db8::1]:4343"));
    }

    [TestCase("whois://Whois.Example.COM", WhoisReferralScheme.Whois, 43, "", "whois://whois.example.com:43")]
    [TestCase("rwhois://RWhois.Example.COM/authority/area", WhoisReferralScheme.RWhois, 4321, "/authority/area", "rwhois://rwhois.example.com:4321")]
    [TestCase("http://Web.Example.COM/path?q=one#part", WhoisReferralScheme.Http, 80, "/path?q=one#part", "http://web.example.com:80")]
    [TestCase("https://Web.Example.COM:8443/lookup", WhoisReferralScheme.Https, 8443, "/lookup", "https://web.example.com:8443")]
    public void ParsesSupportedReferralUris(string value, WhoisReferralScheme expectedScheme, int expectedPort, string expectedSuffix, string expectedKey)
    {
        EndpointParseResult result = EndpointParser.ParseValue(value);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Scheme, Is.EqualTo(expectedScheme));
        Assert.That(result.Endpoint!.Scheme, Is.EqualTo(expectedScheme));
        Assert.That(result.Endpoint.Port, Is.EqualTo(expectedPort));
        Assert.That(result.PathSuffix, Is.EqualTo(expectedSuffix));
        Assert.That(result.EqualityKey, Is.EqualTo(expectedKey));
        Assert.That(result.RejectionReason, Is.Empty);
    }

    [Test]
    public void EquivalentAdvertisedFormsProduceTheSameEqualityKey()
    {
        EndpointParseResult bare = EndpointParser.ParseValue("whois.example.com");
        EndpointParseResult explicitUri = EndpointParser.ParseValue("WHOIS://WHOIS.EXAMPLE.COM.:43");

        Assert.That(bare.IsValid, Is.True);
        Assert.That(explicitUri.IsValid, Is.True);
        Assert.That(explicitUri.EqualityKey, Is.EqualTo(bare.EqualityKey));
    }

    [TestCase("whois://[2001:db8::1]", WhoisReferralScheme.Whois, 43, "")]
    [TestCase("whois://whois.example/", WhoisReferralScheme.Whois, 43, "/")]
    [TestCase("rwhois://rwhois.example:4322/authority", WhoisReferralScheme.RWhois, 4322, "/authority")]
    [TestCase("http://web.example?query=value", WhoisReferralScheme.Http, 80, "?query=value")]
    [TestCase("https://web.example#results", WhoisReferralScheme.Https, 443, "#results")]
    public void AcceptsValidEndpointBoundaryForms(string value, WhoisReferralScheme scheme, int port, string suffix)
    {
        EndpointParseResult result = EndpointParser.ParseValue(value);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Scheme, Is.EqualTo(scheme));
        Assert.That(result.Endpoint, Is.Not.Null);
        Assert.That(result.Endpoint!.Port, Is.EqualTo(port));
        Assert.That(result.PathSuffix, Is.EqualTo(suffix));
        Assert.That(result.RejectionReason, Is.Empty);
    }

    [TestCase("user@whois.example")]
    [TestCase("whois://user@whois.example")]
    [TestCase("https://user:password@web.example/path")]
    public void RejectsEndpointUserInformation(string value)
    {
        EndpointParseResult result = EndpointParser.ParseValue(value);

        AssertRejected(result, "Endpoint user information is not permitted.");
    }

    [TestCase("whois.\0example")]
    [TestCase("whois.\nexample")]
    [TestCase("whois.example\r")]
    public void RejectsControlCharacters(string value)
    {
        EndpointParseResult result = EndpointParser.ParseValue(value);

        AssertRejected(result, "The endpoint value contains a control character.");
    }

    [TestCase("whois.example:")]
    [TestCase("whois.example:abc")]
    [TestCase("whois.example:0")]
    [TestCase("whois.example:65536")]
    [TestCase("whois://whois.example:")]
    [TestCase("whois://whois.example:-1")]
    [TestCase("whois://[2001:db8::1]:")]
    [TestCase("whois://[2001:db8::1]:99999")]
    public void RejectsInvalidOrMissingPorts(string value)
    {
        EndpointParseResult result = EndpointParser.ParseValue(value);

        AssertRejected(result, "The endpoint port is invalid.");
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("whois://")]
    [TestCase("whois://:43")]
    [TestCase("https:///path")]
    public void RejectsMissingHosts(string value)
    {
        EndpointParseResult result = EndpointParser.ParseValue(value);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Endpoint, Is.Null);
        Assert.That(result.RejectionReason, Is.Not.Empty);
    }

    [TestCase("whois://whois.example/path")]
    [TestCase("whois://whois.example?query=value")]
    [TestCase("whois://whois.example#fragment")]
    public void RejectsWhoisPathsQueriesAndFragments(string value)
    {
        EndpointParseResult result = EndpointParser.ParseValue(value);

        AssertRejected(result, "A WHOIS endpoint URI must not contain a path, query, or fragment.");
    }

    [TestCase("rwhois://rwhois.example?query=value")]
    [TestCase("rwhois://rwhois.example/authority#fragment")]
    public void RejectsRWhoisQueriesAndFragments(string value)
    {
        EndpointParseResult result = EndpointParser.ParseValue(value);

        AssertRejected(result, "An RWhois endpoint URI must not contain a query or fragment.");
    }

    [TestCase("2001:db8::1")]
    [TestCase("2001:db8::1:43")]
    [TestCase("whois://2001:db8::1")]
    public void RejectsAmbiguousUnbracketedIpv6(string value)
    {
        EndpointParseResult result = EndpointParser.ParseValue(value);

        AssertRejected(result, "An IPv6 endpoint must be enclosed in brackets.");
    }

    [TestCase("ftp://ftp.example")]
    [TestCase("ldap://directory.example")]
    public void RejectsUnsupportedSchemes(string value)
    {
        EndpointParseResult result = EndpointParser.ParseValue(value);

        AssertRejected(result, "The endpoint URI scheme is not supported.");
        Assert.That(result.Scheme, Is.EqualTo(WhoisReferralScheme.Unsupported));
    }

    private static void AssertRejected(EndpointParseResult result, string reason)
    {
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Endpoint, Is.Null);
        Assert.That(result.EqualityKey, Is.Empty);
        Assert.That(result.RejectionReason, Is.EqualTo(reason));
    }
}
