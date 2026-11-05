using whois;

namespace whois.Tests;

public class QueryFormatterTests
{
    [TestCase(WhoisQueryKind.Domain, "example.com")]
    [TestCase(WhoisQueryKind.IPv4Address, "192.0.2.1")]
    [TestCase(WhoisQueryKind.IPv6Prefix, "2001:db8::/32")]
    [TestCase(WhoisQueryKind.AutonomousSystem, "AS64500")]
    [TestCase(WhoisQueryKind.ReverseDomain, "1.0.0.127.in-addr.arpa")]
    public void IanaQueriesPassThroughUnchanged(WhoisQueryKind kind, string normalized)
    {
        string formatted = Format(WhoisDefaults.IanaHost, kind, normalized);

        Assert.That(formatted, Is.EqualTo(normalized));
    }

    [TestCase(WhoisQueryKind.Domain, "example.com")]
    [TestCase(WhoisQueryKind.FreeForm, "custom search syntax")]
    [TestCase(WhoisQueryKind.AutonomousSystem, "AS64500")]
    public void OrdinaryServerQueriesPassThroughUnchanged(WhoisQueryKind kind, string normalized)
    {
        string formatted = Format("whois.registry.example", kind, normalized);

        Assert.That(formatted, Is.EqualTo(normalized));
    }

    [TestCase(WhoisQueryKind.IPv4Address, "192.0.2.1", "n + 192.0.2.1")]
    [TestCase(WhoisQueryKind.IPv6Address, "2001:db8::1", "n + 2001:db8::1")]
    [TestCase(WhoisQueryKind.IPv4Prefix, "192.0.2.0/24", "n + 192.0.2.0/24")]
    [TestCase(WhoisQueryKind.IPv6Prefix, "2001:db8::/32", "n + 2001:db8::/32")]
    [TestCase(WhoisQueryKind.AutonomousSystem, "AS64500", "a + AS64500")]
    public void ArinUsesNetworkAndAsnCommands(WhoisQueryKind kind, string normalized, string expected)
    {
        string formatted = Format("whois.arin.net", kind, normalized);

        Assert.That(formatted, Is.EqualTo(expected));
    }

    [TestCase(WhoisQueryKind.Domain, "example.com")]
    [TestCase(WhoisQueryKind.ReverseDomain, "1.0.0.127.in-addr.arpa")]
    [TestCase(WhoisQueryKind.FreeForm, "custom")]
    public void ArinPassesThroughUnrelatedKinds(WhoisQueryKind kind, string normalized)
    {
        string formatted = Format("whois.arin.net", kind, normalized);

        Assert.That(formatted, Is.EqualTo(normalized));
    }

    [Test]
    public void VerisignUsesExactDomainCommand()
    {
        string formatted = Format("whois.verisign-grs.com", WhoisQueryKind.Domain, "example.com");

        Assert.That(formatted, Is.EqualTo("domain example.com"));
    }

    [TestCase(WhoisQueryKind.IPv4Address, "192.0.2.1")]
    [TestCase(WhoisQueryKind.AutonomousSystem, "AS64500")]
    [TestCase(WhoisQueryKind.FreeForm, "custom")]
    public void VerisignPassesThroughNonDomainKinds(WhoisQueryKind kind, string normalized)
    {
        string formatted = Format("whois.verisign-grs.com", kind, normalized);

        Assert.That(formatted, Is.EqualTo(normalized));
    }

    [TestCase("whois.arin.net.example", WhoisQueryKind.IPv4Address, "192.0.2.1")]
    [TestCase("notwhois.arin.net", WhoisQueryKind.AutonomousSystem, "AS64500")]
    [TestCase("whois.verisign-grs.com.example", WhoisQueryKind.Domain, "example.com")]
    public void ServerRulesRequireExactHostMatch(string host, WhoisQueryKind kind, string normalized)
    {
        string formatted = Format(host, kind, normalized);

        Assert.That(formatted, Is.EqualTo(normalized));
    }

    [TestCase("server supplied query")]
    [TestCase("")]
    public void SelectionOverrideHasHighestPrecedence(string queryOverride)
    {
        WhoisQueryFormatter formatter = new WhoisQueryFormatter();
        WhoisQuery query = new WhoisQuery();
        WhoisServerSelection selection = new WhoisServerSelection();
        query.Kind = WhoisQueryKind.IPv4Address;
        query.NormalizedText = "192.0.2.1";
        selection.Endpoint.Host = "whois.arin.net";
        selection.QueryOverride = queryOverride;

        string formatted = formatter.FormatQuery(query, selection);

        Assert.That(formatted, Is.EqualTo(queryOverride));
    }

    [TestCase("WHOIS.ARIN.NET", WhoisQueryKind.IPv4Address, "192.0.2.1", "n + 192.0.2.1")]
    [TestCase("whois.arin.net.", WhoisQueryKind.IPv6Prefix, "2001:db8::/32", "n + 2001:db8::/32")]
    [TestCase("WhoIs.ArIn.NeT...", WhoisQueryKind.AutonomousSystem, "AS64500", "a + AS64500")]
    [TestCase("  whois.arin.net.  ", WhoisQueryKind.IPv4Prefix, "192.0.2.0/24", "n + 192.0.2.0/24")]
    public void ArinRuleNormalizesEndpointHostCasingAndRootDots(string host, WhoisQueryKind kind, string normalized, string expected)
    {
        string formatted = Format(host, kind, normalized);

        Assert.That(formatted, Is.EqualTo(expected));
    }

    [TestCase("WHOIS.VERISIGN-GRS.COM")]
    [TestCase("whois.verisign-grs.com.")]
    [TestCase("WhoIs.VeRiSiGn-GrS.CoM...")]
    [TestCase("  whois.verisign-grs.com.  ")]
    public void VerisignRuleNormalizesEndpointHostCasingAndRootDots(string host)
    {
        string formatted = Format(host, WhoisQueryKind.Domain, "example.com");

        Assert.That(formatted, Is.EqualTo("domain example.com"));
    }

    [TestCase("unknown.example", WhoisQueryKind.IPv4Address, "192.0.2.1")]
    [TestCase("ARIN.NET", WhoisQueryKind.AutonomousSystem, "AS64500")]
    [TestCase("verisign-grs.com", WhoisQueryKind.Domain, "example.com")]
    [TestCase("", WhoisQueryKind.Domain, "example.com")]
    public void UnknownServerPassesThroughRuleEligibleQueries(string host, WhoisQueryKind kind, string normalized)
    {
        string formatted = Format(host, kind, normalized);

        Assert.That(formatted, Is.EqualTo(normalized));
    }

    [Test]
    public void SelectionOverrideBypassesNormalizedServerRule()
    {
        WhoisQueryFormatter formatter = new WhoisQueryFormatter();
        WhoisQuery query = new WhoisQuery();
        WhoisServerSelection selection = new WhoisServerSelection();
        query.Kind = WhoisQueryKind.Domain;
        query.NormalizedText = "example.com";
        selection.Endpoint.Host = "WHOIS.VERISIGN-GRS.COM.";
        selection.QueryOverride = "=example.com";

        string formatted = formatter.FormatQuery(query, selection);

        Assert.That(formatted, Is.EqualTo("=example.com"));
    }

    private static string Format(string host, WhoisQueryKind kind, string normalized)
    {
        IWhoisQueryFormatter formatter = new WhoisQueryFormatter();
        WhoisQuery query = new WhoisQuery();
        WhoisServerSelection selection = new WhoisServerSelection();
        query.Kind = kind;
        query.NormalizedText = normalized;
        selection.Endpoint.Host = host;
        return formatter.FormatQuery(query, selection);
    }
}
