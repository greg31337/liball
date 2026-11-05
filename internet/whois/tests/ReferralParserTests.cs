using whois;

namespace whois.Tests;

public class ReferralParserTests
{
    [Test]
    public void RecognizesEveryFieldLabelAcrossCaseAndSpacingVariants()
    {
        string text =
            "ReFeR: whois.iana.test\r\n" +
            "WHOIS: whois.registry.test\r\n" +
            "wHoIs   SeRvEr: whois.legacy-registrar.test\r\n" +
            "Registrar\t WHOIS   Server: whois.registrar.test\r\n" +
            "REFERRALSERVER: whois://whois.generic.test\r\n" +
            "Referral    Server: whois://whois.spaced.test\r\n";

        WhoisReferral[] referrals = ParseReferrals(text);

        Assert.That(referrals, Has.Length.EqualTo(6));
        Assert.That(referrals.Select((referral) => referral.SourceField), Is.EqualTo(new[]
        {
            "ReFeR",
            "WHOIS",
            "wHoIs   SeRvEr",
            "Registrar\t WHOIS   Server",
            "REFERRALSERVER",
            "Referral    Server"
        }));
        Assert.That(referrals.Select((referral) => referral.SourceLine), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));
        Assert.That(referrals.Select((referral) => referral.Kind), Is.EqualTo(new[]
        {
            WhoisReferralKind.Iana,
            WhoisReferralKind.Registry,
            WhoisReferralKind.Registrar,
            WhoisReferralKind.Registrar,
            WhoisReferralKind.Generic,
            WhoisReferralKind.Generic
        }));
        Assert.That(referrals, Has.All.Matches<WhoisReferral>((referral) => referral.IsValid));
    }

    [Test]
    public void InterleavesRawReferralDirectivesWithFieldsInSourceOrder()
    {
        string text =
            "refer: first.example.test\r\n" +
            "%ReFeRrAl   rwhois://rwhois.example.test:4321/auth-area=example.test\r\n" +
            "Registrar WHOIS Server: third.example.test\r\n" +
            "% referral: https://web.example.test/results\r\n";

        WhoisReferral[] referrals = ParseReferrals(text);

        Assert.That(referrals, Has.Length.EqualTo(4));
        Assert.That(referrals.Select((referral) => referral.SourceLine), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        Assert.That(referrals[1].SourceField, Is.EqualTo(WhoisFieldNames.ReferralDirective));
        Assert.That(referrals[1].AdvertisedValue, Is.EqualTo("rwhois://rwhois.example.test:4321/auth-area=example.test"));
        Assert.That(referrals[1].Scheme, Is.EqualTo(WhoisReferralScheme.RWhois));
        Assert.That(referrals[1].Kind, Is.EqualTo(WhoisReferralKind.RWhois));
        Assert.That(referrals[1].PathSuffix, Is.EqualTo("/auth-area=example.test"));
        Assert.That(referrals[3].Scheme, Is.EqualTo(WhoisReferralScheme.Https));
        Assert.That(referrals[3].Kind, Is.EqualTo(WhoisReferralKind.Web));
    }

    [Test]
    public void PreservesRawAdvertisedValueAndEndpointSourceMetadata()
    {
        WhoisReferral[] referrals = ParseReferrals("Registrar WHOIS Server:\tWhois.Example.Test:4343  \r\n");

        Assert.That(referrals, Has.Length.EqualTo(1));
        WhoisReferral referral = referrals[0];
        Assert.That(referral.SourceField, Is.EqualTo("Registrar WHOIS Server"));
        Assert.That(referral.SourceLine, Is.EqualTo(1));
        Assert.That(referral.AdvertisedValue, Is.EqualTo("Whois.Example.Test:4343  "));
        Assert.That(referral.Endpoint, Is.Not.Null);
        Assert.That(referral.Endpoint!.AdvertisedValue, Is.EqualTo(referral.AdvertisedValue));
        Assert.That(referral.Endpoint.Host, Is.EqualTo("whois.example.test"));
        Assert.That(referral.Endpoint.Port, Is.EqualTo(4343));
        Assert.That(referral.Scheme, Is.EqualTo(WhoisReferralScheme.Unspecified));
        Assert.That(referral.Kind, Is.EqualTo(WhoisReferralKind.Registrar));
    }

    [Test]
    public void RetainsRecognizedMalformedAndUnsupportedReferralsWithReasons()
    {
        string text =
            "refer: whois.example.test:\r\n" +
            "ReferralServer: ftp://files.example.test\r\n" +
            "%referral rwhois://\r\n";

        WhoisReferral[] referrals = ParseReferrals(text);

        Assert.That(referrals, Has.Length.EqualTo(3));
        Assert.That(referrals, Has.All.Matches<WhoisReferral>((referral) => !referral.IsValid));
        Assert.That(referrals, Has.All.Matches<WhoisReferral>((referral) => referral.Endpoint == null));
        Assert.That(referrals[0].Kind, Is.EqualTo(WhoisReferralKind.Iana));
        Assert.That(referrals[0].RejectionReason, Is.EqualTo("The endpoint port is invalid."));
        Assert.That(referrals[1].Scheme, Is.EqualTo(WhoisReferralScheme.Unsupported));
        Assert.That(referrals[1].Kind, Is.EqualTo(WhoisReferralKind.Generic));
        Assert.That(referrals[1].RejectionReason, Is.EqualTo("The endpoint URI scheme is not supported."));
        Assert.That(referrals[2].Scheme, Is.EqualTo(WhoisReferralScheme.RWhois));
        Assert.That(referrals[2].Kind, Is.EqualTo(WhoisReferralKind.RWhois));
        Assert.That(referrals[2].RejectionReason, Is.EqualTo("The endpoint host is missing."));
    }

    [Test]
    public void IgnoresUnrecognizedFieldsAndCommentDirectives()
    {
        string text =
            "Referral URL: https://not-a-recognized-label.example\r\n" +
            "%referrals whois.example.test\r\n" +
            "%see-also rwhois://rwhois.example.test\r\n";

        WhoisReferral[] referrals = ParseReferrals(text);

        Assert.That(referrals, Is.Empty);
    }

    [Test]
    public void RejectsNullParsedResponse()
    {
        IWhoisReferralParser parser = new WhoisReferralParser();

        Assert.Throws<ArgumentNullException>(() => parser.ParseReferrals(null!));
    }

    [Test]
    public void AssignsDeterministicFollowPriorityWithoutReorderingReferrals()
    {
        string text =
            "ReferralServer: whois://generic.example.test\r\n" +
            "Registrar WHOIS Server: registrar.example.test\r\n" +
            "whois: registry.example.test\r\n" +
            "refer: iana-referral.example.test\r\n";

        WhoisReferral[] referrals = ParseReferrals(text);

        Assert.That(referrals.Select((referral) => referral.Endpoint!.Host), Is.EqualTo(new[]
        {
            "generic.example.test",
            "registrar.example.test",
            "registry.example.test",
            "iana-referral.example.test"
        }));
        Assert.That(referrals.Select((referral) => referral.FollowPriority), Is.EqualTo(new[] { 2, 1, 2, 0 }));
    }

    [Test]
    public void RetainsWhoisProtocolAndRejectedReferralsIncludingDuplicatesAndPotentialSelfReferences()
    {
        string text =
            "whois: whois.current.example\r\n" +
            "whois: whois.current.example\r\n" +
            "ReferralServer: rwhois://rwhois.example:4321/auth-area=example\r\n" +
            "Referral Server: https://web.example/referral\r\n" +
            "Registrar WHOIS Server: missing-port.example:\r\n" +
            "ReferralServer: ftp://unsupported.example\r\n";

        WhoisReferral[] referrals = ParseReferrals(text);

        Assert.That(referrals, Has.Length.EqualTo(6));
        Assert.That(referrals[0].AdvertisedValue, Is.EqualTo(referrals[1].AdvertisedValue));
        Assert.That(referrals[0].Endpoint!.Host, Is.EqualTo("whois.current.example"));
        Assert.That(referrals[1].Endpoint!.Host, Is.EqualTo("whois.current.example"));
        Assert.That(referrals.Select((referral) => referral.Kind), Is.EqualTo(new[]
        {
            WhoisReferralKind.Registry,
            WhoisReferralKind.Registry,
            WhoisReferralKind.RWhois,
            WhoisReferralKind.Web,
            WhoisReferralKind.Registrar,
            WhoisReferralKind.Generic
        }));
        Assert.That(referrals.Select((referral) => referral.FollowPriority), Is.EqualTo(new[] { 2, 2, -1, -1, -1, -1 }));
        Assert.That(referrals.Select((referral) => referral.IsValid), Is.EqualTo(new[] { true, true, true, true, false, false }));
    }

    [Test]
    public void ParsesCapturedIanaReferResponse()
    {
        WhoisReferral[] referrals = ParseReferrals(WhoisResponseFixtures.Iana);

        Assert.That(referrals, Has.Length.EqualTo(1));
        WhoisReferral referral = referrals[0];
        Assert.That(referral.SourceField, Is.EqualTo("refer"));
        Assert.That(referral.SourceLine, Is.EqualTo(7));
        Assert.That(referral.AdvertisedValue, Is.EqualTo("whois.verisign-grs.com"));
        Assert.That(referral.Kind, Is.EqualTo(WhoisReferralKind.Iana));
        Assert.That(referral.FollowPriority, Is.EqualTo(0));
        Assert.That(referral.Endpoint!.Host, Is.EqualTo("whois.verisign-grs.com"));
        Assert.That(referral.Endpoint.Port, Is.EqualTo(WhoisDefaults.WhoisPort));
    }

    [Test]
    public void ParsesCapturedIcannRegistrarResponse()
    {
        WhoisReferral[] referrals = ParseReferrals(WhoisResponseFixtures.Gtld);

        Assert.That(referrals, Has.Length.EqualTo(1));
        WhoisReferral referral = referrals[0];
        Assert.That(referral.SourceField, Is.EqualTo("Registrar WHOIS Server"));
        Assert.That(referral.SourceLine, Is.EqualTo(3));
        Assert.That(referral.Kind, Is.EqualTo(WhoisReferralKind.Registrar));
        Assert.That(referral.FollowPriority, Is.EqualTo(1));
        Assert.That(referral.Endpoint!.Host, Is.EqualTo("whois.example-registrar.test"));
        Assert.That(referral.Endpoint.Port, Is.EqualTo(WhoisDefaults.WhoisPort));
    }

    [Test]
    public void ParsesCapturedArinReferralServerResponse()
    {
        WhoisReferral[] referrals = ParseReferrals(WhoisResponseFixtures.Arin);

        Assert.That(referrals, Has.Length.EqualTo(1));
        WhoisReferral referral = referrals[0];
        Assert.That(referral.SourceField, Is.EqualTo("ReferralServer"));
        Assert.That(referral.SourceLine, Is.EqualTo(10));
        Assert.That(referral.Scheme, Is.EqualTo(WhoisReferralScheme.Whois));
        Assert.That(referral.Kind, Is.EqualTo(WhoisReferralKind.Generic));
        Assert.That(referral.FollowPriority, Is.EqualTo(2));
        Assert.That(referral.Endpoint!.Host, Is.EqualTo("whois.example.test"));
        Assert.That(referral.Endpoint.Port, Is.EqualTo(43));
    }

    [Test]
    public void ParsesLegacyPlainHostWithNondefaultPort()
    {
        WhoisReferral[] referrals = ParseReferrals("Whois Server: Whois.Legacy.Example:4343\r\n");

        Assert.That(referrals, Has.Length.EqualTo(1));
        WhoisReferral referral = referrals[0];
        Assert.That(referral.SourceField, Is.EqualTo("Whois Server"));
        Assert.That(referral.Scheme, Is.EqualTo(WhoisReferralScheme.Unspecified));
        Assert.That(referral.Kind, Is.EqualTo(WhoisReferralKind.Registrar));
        Assert.That(referral.Endpoint!.Host, Is.EqualTo("whois.legacy.example"));
        Assert.That(referral.Endpoint.Port, Is.EqualTo(4343));
    }

    [Test]
    public void ParsesBracketedIpv6WhoisReferral()
    {
        WhoisReferral[] referrals = ParseReferrals("Referral Server: whois://[2001:0DB8:0:0::5]:4343\r\n");

        Assert.That(referrals, Has.Length.EqualTo(1));
        WhoisReferral referral = referrals[0];
        Assert.That(referral.IsValid, Is.True);
        Assert.That(referral.Scheme, Is.EqualTo(WhoisReferralScheme.Whois));
        Assert.That(referral.Endpoint!.Host, Is.EqualTo("2001:db8::5"));
        Assert.That(referral.Endpoint.Port, Is.EqualTo(4343));
        Assert.That(referral.FollowPriority, Is.EqualTo(2));
    }

    [Test]
    public void ParsesRWhoisDirectiveAuthorityPath()
    {
        WhoisReferral[] referrals = ParseReferrals("%referral rwhois://RWhois.Example:4322/auth-area=example.test\r\n");

        Assert.That(referrals, Has.Length.EqualTo(1));
        WhoisReferral referral = referrals[0];
        Assert.That(referral.SourceField, Is.EqualTo("%referral"));
        Assert.That(referral.Scheme, Is.EqualTo(WhoisReferralScheme.RWhois));
        Assert.That(referral.Kind, Is.EqualTo(WhoisReferralKind.RWhois));
        Assert.That(referral.Endpoint!.Host, Is.EqualTo("rwhois.example"));
        Assert.That(referral.Endpoint.Port, Is.EqualTo(4322));
        Assert.That(referral.PathSuffix, Is.EqualTo("/auth-area=example.test"));
        Assert.That(referral.FollowPriority, Is.EqualTo(-1));
    }

    [Test]
    public void ParsesWebReferralWithoutTreatingItAsWhois()
    {
        WhoisReferral[] referrals = ParseReferrals("ReferralServer: https://Web.Example:8443/lookup?q=one#result\r\n");

        Assert.That(referrals, Has.Length.EqualTo(1));
        WhoisReferral referral = referrals[0];
        Assert.That(referral.IsValid, Is.True);
        Assert.That(referral.Scheme, Is.EqualTo(WhoisReferralScheme.Https));
        Assert.That(referral.Kind, Is.EqualTo(WhoisReferralKind.Web));
        Assert.That(referral.Endpoint!.Scheme, Is.EqualTo(WhoisReferralScheme.Https));
        Assert.That(referral.Endpoint.Host, Is.EqualTo("web.example"));
        Assert.That(referral.Endpoint.Port, Is.EqualTo(8443));
        Assert.That(referral.PathSuffix, Is.EqualTo("/lookup?q=one#result"));
        Assert.That(referral.FollowPriority, Is.EqualTo(-1));
    }

    [Test]
    public void PreservesDuplicateReferralOccurrences()
    {
        string text =
            "Registrar WHOIS Server: whois.duplicate.example\r\n" +
            "Registrar WHOIS Server: whois.duplicate.example\r\n";

        WhoisReferral[] referrals = ParseReferrals(text);

        Assert.That(referrals, Has.Length.EqualTo(2));
        Assert.That(referrals.Select((referral) => referral.SourceLine), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(referrals.Select((referral) => referral.AdvertisedValue), Is.EqualTo(new[]
        {
            "whois.duplicate.example",
            "whois.duplicate.example"
        }));
        Assert.That(referrals[0], Is.Not.SameAs(referrals[1]));
        Assert.That(referrals[0].Endpoint, Is.Not.SameAs(referrals[1].Endpoint));
    }

    [TestCase("refer: ", "The endpoint value is empty.")]
    [TestCase("Registrar WHOIS Server: whois.example:", "The endpoint port is invalid.")]
    [TestCase("ReferralServer: whois://user@whois.example", "Endpoint user information is not permitted.")]
    [TestCase("Referral Server: 2001:db8::1", "An IPv6 endpoint must be enclosed in brackets.")]
    public void RetainsMalformedReferralValuesWithSpecificReasons(string line, string expectedReason)
    {
        WhoisReferral[] referrals = ParseReferrals(line + "\r\n");

        Assert.That(referrals, Has.Length.EqualTo(1));
        Assert.That(referrals[0].IsValid, Is.False);
        Assert.That(referrals[0].Endpoint, Is.Null);
        Assert.That(referrals[0].FollowPriority, Is.EqualTo(-1));
        Assert.That(referrals[0].RejectionReason, Is.EqualTo(expectedReason));
    }

    [Test]
    public void IgnoresReferralLabelsAndUrisEmbeddedInProse()
    {
        string text =
            WhoisResponseFixtures.ProseAndDisclaimer +
            "The refer: value is described in this sentence.\r\n" +
            "Look for Registrar WHOIS Server: whois.prose.example in documentation.\r\n" +
            "remarks: A ReferralServer: whois://whois.prose.example is only an example.\r\n" +
            "# Whois Server: whois.comment.example\r\n" +
            "% Documentation mentions %referral rwhois://rwhois.prose.example\r\n";

        WhoisReferral[] referrals = ParseReferrals(text);

        Assert.That(referrals, Is.Empty);
    }

    private static WhoisReferral[] ParseReferrals(string text)
    {
        WhoisTextResponse decoded = new WhoisTextResponse();
        decoded.Text = text;
        decoded.IsDecoded = true;
        WhoisParsedResponse parsed = new WhoisResponseParser().ParseResponse(decoded, new WhoisLimits());
        Assert.That(parsed.IsParsed, Is.True);
        IWhoisReferralParser parser = new WhoisReferralParser();
        return parser.ParseReferrals(parsed);
    }
}
