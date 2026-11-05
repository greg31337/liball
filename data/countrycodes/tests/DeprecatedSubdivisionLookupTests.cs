using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class DeprecatedSubdivisionLookupTests
{
    [Test]
    public void DeprecatedCodeLookupReturnsItsOwnLifecycleRecord()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        SubdivisionLookupResult text = catalog.FindSubdivisionByCode("cn-11");
        SubdivisionLookupResult span = catalog.FindSubdivisionByCodeSpan("cn-11");
        SubdivisionLookupResult separated =
            catalog.FindSubdivisionByCountryAndSuffix("cn", "11");

        AssertAlias(text, "CN-11", SubdivisionStatus.Deprecated, new[] { "CN-BJ" });
        AssertAlias(span, "CN-11", SubdivisionStatus.Deprecated, new[] { "CN-BJ" });
        AssertAlias(separated, "CN-11", SubdivisionStatus.Deprecated, new[] { "CN-BJ" });

        Subdivision required = catalog.GetRequiredSubdivisionByCode("CN-11");
        Assert.That(required.Code, Is.EqualTo("CN-11"));
        Assert.That(required.Status, Is.EqualTo(SubdivisionStatus.Deprecated));
        Assert.That(required.ReplacementCodes, Is.EqualTo(new[] { "CN-BJ" }));
    }

    [Test]
    public void OneToManyReplacementPreservesEveryCodeWithoutChoosingOne()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();
        string[] expected =
        {
            "NZ-AUK",
            "NZ-BOP",
            "NZ-GIS",
            "NZ-HKB",
            "NZ-MWT",
            "NZ-NTL",
            "NZ-TKI",
            "NZ-WGN",
            "NZ-WKO"
        };

        SubdivisionLookupResult first = catalog.FindSubdivisionByCode("NZ-N");
        AssertAlias(first, "NZ-N", SubdivisionStatus.Deprecated, expected);
        Assert.That(first.Subdivision!.Code, Is.Not.EqualTo(expected[0]));
        first.Subdivision.ReplacementCodes[0] = "changed";

        SubdivisionLookupResult second = catalog.FindSubdivisionByCode("NZ-N");
        AssertAlias(second, "NZ-N", SubdivisionStatus.Deprecated, expected);
    }

    [Test]
    public void ReplacementCodesRetainCountryAndUnavailableTargets()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        AssertAlias(
            catalog.FindSubdivisionByCode("FR-GF"),
            "FR-GF",
            SubdivisionStatus.Withdrawn,
            new[] { "GF" });
        AssertAlias(
            catalog.FindSubdivisionByCode("CN-15"),
            "CN-15",
            SubdivisionStatus.Deprecated,
            new[] { "CN-MN" });

        SubdivisionLookupResult currentOverlong = catalog.FindSubdivisionByCode("FI-01");
        Assert.That(currentOverlong.Status, Is.EqualTo(LookupStatus.Found));
        Assert.That(currentOverlong.Subdivision, Is.Not.Null);
        Assert.That(currentOverlong.Subdivision!.Name, Is.EqualTo("Landskapet Åland"));
        Assert.That(currentOverlong.Subdivision.Status, Is.EqualTo(SubdivisionStatus.Withdrawn));
        Assert.That(currentOverlong.Subdivision.ReplacementCodes, Is.EqualTo(new[] { "AX" }));
    }

    [Test]
    public void StatusFilteringIncludesCurrentAndAliasLifecycleRecordsInCodeOrder()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        Subdivision[] active = catalog.GetSubdivisionsByStatus(SubdivisionStatus.Active);
        Assert.That(active, Has.Length.EqualTo(5027));
        Assert.That(active[0].Code, Is.EqualTo("AD-02"));
        Assert.That(active[active.Length - 1].Code, Is.EqualTo("ZW-MW"));
        Assert.That(active, Has.All.Matches<Subdivision>(
            value => value.Status == SubdivisionStatus.Active));

        Subdivision[] deprecated =
            catalog.GetSubdivisionsByStatus(SubdivisionStatus.Deprecated);
        Assert.That(deprecated, Has.Length.EqualTo(120));
        Assert.That(deprecated[0].Code, Is.EqualTo("CN-11"));
        Assert.That(deprecated[deprecated.Length - 1].Code, Is.EqualTo("TW-TXQ"));
        Assert.That(deprecated, Has.All.Matches<Subdivision>(
            value => value.Status == SubdivisionStatus.Deprecated));

        Subdivision[] withdrawn =
            catalog.GetSubdivisionsByStatus(SubdivisionStatus.Withdrawn);
        Assert.That(withdrawn, Has.Length.EqualTo(27));
        Assert.That(withdrawn[0].Code, Is.EqualTo("CN-71"));
        Assert.That(withdrawn[withdrawn.Length - 1].Code, Is.EqualTo("US-VI"));
        Assert.That(withdrawn, Has.All.Matches<Subdivision>(
            value => value.Status == SubdivisionStatus.Withdrawn));

        Assert.That(
            catalog.GetSubdivisionsByStatus(SubdivisionStatus.Unknown),
            Is.Empty);
        Assert.That(
            catalog.GetSubdivisionsByStatus((SubdivisionStatus)99),
            Is.Empty);
    }

    [Test]
    public void StatusFilteredResultsAreDetachedFromLaterCalls()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();
        Subdivision[] first =
            catalog.GetSubdivisionsByStatus(SubdivisionStatus.Deprecated);
        first[0].Code = "changed";
        first[0].ReplacementCodes[0] = "changed";

        Subdivision[] second =
            catalog.GetSubdivisionsByStatus(SubdivisionStatus.Deprecated);
        Assert.That(second[0].Code, Is.EqualTo("CN-11"));
        Assert.That(second[0].ReplacementCodes, Is.EqualTo(new[] { "CN-BJ" }));
    }

    private static void AssertAlias(
        SubdivisionLookupResult result,
        string code,
        SubdivisionStatus status,
        string[] replacements)
    {
        Assert.That(result.Status, Is.EqualTo(LookupStatus.Found));
        Assert.That(result.Subdivision, Is.Not.Null);
        Assert.That(result.Subdivision!.Code, Is.EqualTo(code));
        Assert.That(result.Subdivision.CountryCode, Is.EqualTo(code.Substring(0, 2)));
        Assert.That(result.Subdivision.Suffix, Is.EqualTo(code.Substring(3)));
        Assert.That(result.Subdivision.Status, Is.EqualTo(status));
        Assert.That(result.Subdivision.ReplacementCodes, Is.EqualTo(replacements));
        Assert.That(result.Candidates, Is.Empty);
    }
}
