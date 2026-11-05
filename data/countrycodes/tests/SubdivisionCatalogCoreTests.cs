using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class SubdivisionCatalogCoreTests
{
    [Test]
    public void SubdivisionEnumerationIsStableAndCountryFilteringNormalizesCase()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        Subdivision[] all = catalog.GetSubdivisions();
        Assert.That(all, Has.Length.EqualTo(5046));
        Assert.That(all[0].Code, Is.EqualTo("AD-02"));
        Assert.That(all[all.Length - 1].Code, Is.EqualTo("ZW-MW"));

        Subdivision[] unitedStates = catalog.GetSubdivisionsByCountry("us");
        Assert.That(unitedStates, Has.Length.EqualTo(57));
        Assert.That(unitedStates[0].Code, Is.EqualTo("US-AK"));
        Assert.That(unitedStates[unitedStates.Length - 1].Code, Is.EqualTo("US-WY"));
        Assert.That(unitedStates, Has.All.Matches<Subdivision>(value => value.CountryCode == "US"));

        Assert.That(catalog.GetSubdivisionsByCountry("AQ"), Is.Empty);
        Assert.That(catalog.GetSubdivisionsByCountry("ZZ"), Is.Empty);
        Assert.That(catalog.GetSubdivisionsByCountry("U1"), Is.Empty);
        Assert.That(catalog.GetSubdivisionsByCountry(null!), Is.Empty);
    }

    [Test]
    public void EnumeratedSubdivisionsAreDetachedFromLaterCalls()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();
        Subdivision[] first = catalog.GetSubdivisionsByCountry("GB");
        first[0].Code = "changed";
        first[0].Aliases = new[] { "changed" };

        Subdivision[] second = catalog.GetSubdivisionsByCountry("GB");

        Assert.That(second[0].Code, Is.EqualTo("GB-ABC"));
        Assert.That(second[0].Aliases, Does.Not.Contain("changed"));
    }

    [Test]
    public void FullCodeLookupsNormalizeAsciiCaseAndPreserveSpanParity()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        SubdivisionLookupResult text = catalog.FindSubdivisionByCode("gb-wls");
        SubdivisionLookupResult span = catalog.FindSubdivisionByCodeSpan("gb-wls");

        AssertFound(text, "GB-WLS");
        AssertFound(span, "GB-WLS");
        Assert.That(span.Subdivision, Is.Not.SameAs(text.Subdivision));
        Assert.That(text.Subdivision!.Name, Is.EqualTo("Wales [Cymru GB-CYM]"));

        AssertStatus(catalog.FindSubdivisionByCode("US-ZZ"), LookupStatus.NotFound);
        AssertStatus(catalog.FindSubdivisionByCodeSpan("US-ZZ"), LookupStatus.NotFound);
        AssertStatus(catalog.FindSubdivisionByCode("ZZ-AA"), LookupStatus.NotFound);
        AssertStatus(catalog.FindSubdivisionByCode(null!), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindSubdivisionByCode(string.Empty), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindSubdivisionByCode("US_CA"), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindSubdivisionByCode("USA-CA"), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindSubdivisionByCode("US-CALX"), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindSubdivisionByCode("U1-CA"), LookupStatus.InvalidInput);
    }

    [Test]
    public void CountryAndSuffixLookupBuildsOneCanonicalFullCode()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        AssertFound(catalog.FindSubdivisionByCountryAndSuffix("us", "ca"), "US-CA");
        AssertFound(catalog.FindSubdivisionByCountryAndSuffix("ad", "02"), "AD-02");
        AssertStatus(
            catalog.FindSubdivisionByCountryAndSuffix("US", "ZZ"),
            LookupStatus.NotFound);
        AssertStatus(
            catalog.FindSubdivisionByCountryAndSuffix("ZZ", "AA"),
            LookupStatus.NotFound);
        AssertStatus(
            catalog.FindSubdivisionByCountryAndSuffix("U1", "CA"),
            LookupStatus.InvalidInput);
        AssertStatus(
            catalog.FindSubdivisionByCountryAndSuffix("US", string.Empty),
            LookupStatus.InvalidInput);
        AssertStatus(
            catalog.FindSubdivisionByCountryAndSuffix("US", "C@"),
            LookupStatus.InvalidInput);
        AssertStatus(
            catalog.FindSubdivisionByCountryAndSuffix(null!, "CA"),
            LookupStatus.InvalidInput);
    }

    [Test]
    public void CountryResolutionUsesTheWellFormedSubdivisionPrefix()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        AssertCountryFound(catalog.ResolveCountryForSubdivision("us-ca"), "US");
        AssertCountryFound(catalog.ResolveCountryForSubdivision("us-zz"), "US");
        AssertCountryStatus(
            catalog.ResolveCountryForSubdivision("ZZ-AA"),
            LookupStatus.NotFound);
        AssertCountryStatus(
            catalog.ResolveCountryForSubdivision("US_CA"),
            LookupStatus.InvalidInput);
        AssertCountryStatus(
            catalog.ResolveCountryForSubdivision(null!),
            LookupStatus.InvalidInput);
    }

    [Test]
    public void RequiredSubdivisionLookupReturnsDetachedDataAndFocusedFailures()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        Subdivision first = catalog.GetRequiredSubdivisionByCode("gb-wls");
        Assert.That(first.Code, Is.EqualTo("GB-WLS"));
        first.Code = "changed";
        first.LocalizedNames[0].Value = "changed";

        Subdivision second = catalog.GetRequiredSubdivisionByCode("GB-WLS");
        Assert.That(second.Code, Is.EqualTo("GB-WLS"));
        Assert.That(second.LocalizedNames[0].Value, Is.EqualTo("Wales"));

        AssertInvalidRequired(
            () => catalog.GetRequiredSubdivisionByCode(null!),
            null,
            ValidationFailureKind.NullOrEmpty);
        AssertInvalidRequired(
            () => catalog.GetRequiredSubdivisionByCode("US-"),
            "US-",
            ValidationFailureKind.InvalidLength);
        AssertInvalidRequired(
            () => catalog.GetRequiredSubdivisionByCode("US_CA"),
            "US_CA",
            ValidationFailureKind.InvalidShape);
        AssertInvalidRequired(
            () => catalog.GetRequiredSubdivisionByCode("U1-CA"),
            "U1-CA",
            ValidationFailureKind.InvalidCharacter);

        SubdivisionNotFoundException notFound =
            Assert.Throws<SubdivisionNotFoundException>(
                () => catalog.GetRequiredSubdivisionByCode("us-zz"))!;
        Assert.That(notFound.Input, Is.EqualTo("us-zz"));
    }

    private static void AssertFound(SubdivisionLookupResult result, string code)
    {
        Assert.That(result.Status, Is.EqualTo(LookupStatus.Found));
        Assert.That(result.Subdivision, Is.Not.Null);
        Assert.That(result.Subdivision!.Code, Is.EqualTo(code));
        Assert.That(result.Candidates, Is.Empty);
    }

    private static void AssertStatus(SubdivisionLookupResult result, LookupStatus status)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.Subdivision, Is.Null);
        Assert.That(result.Candidates, Is.Empty);
    }

    private static void AssertCountryFound(CountryLookupResult result, string alpha2)
    {
        Assert.That(result.Status, Is.EqualTo(LookupStatus.Found));
        Assert.That(result.Country, Is.Not.Null);
        Assert.That(result.Country!.Alpha2, Is.EqualTo(alpha2));
        Assert.That(result.Candidates, Is.Empty);
    }

    private static void AssertCountryStatus(CountryLookupResult result, LookupStatus status)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.Country, Is.Null);
        Assert.That(result.Candidates, Is.Empty);
    }

    private static void AssertInvalidRequired(
        TestDelegate action,
        string? input,
        ValidationFailureKind failureKind)
    {
        InvalidSubdivisionCodeException exception =
            Assert.Throws<InvalidSubdivisionCodeException>(action)!;
        Assert.That(exception.Input, Is.EqualTo(input));
        Assert.That(exception.FailureKind, Is.EqualTo(failureKind));
    }
}
