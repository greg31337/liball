using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class SubdivisionNameLookupTests
{
    [Test]
    public void NameLookupSupportsExactAndNormalizedCanonicalNames()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        AssertFound(catalog.FindSubdivisionByName("california"), "US-CA");
        AssertFound(catalog.FindSubdivisionByName("  Sao-Paulo  "), "BR-SP");
        AssertFound(
            catalog.FindSubdivisionByName("Wales Cymru GB CYM"),
            "GB-WLS");
    }

    [Test]
    public void ExactSpellingTakesPriorityOverNormalizedCollisions()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        AssertFound(catalog.FindSubdivisionByName("Beja"), "PT-02");
        AssertFound(catalog.FindSubdivisionByName("Béja"), "TN-31");

        SubdivisionLookupResult normalized = catalog.FindSubdivisionByName("Beja ");
        AssertAmbiguous(normalized, new[] { "PT-02", "TN-31" });
    }

    [Test]
    public void GlobalNameLookupReturnsEveryCandidateInStableCodeOrder()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        SubdivisionLookupResult first = catalog.FindSubdivisionByName("Florida");
        AssertAmbiguous(first, new[] { "US-FL", "UY-FD" });
        first.Candidates[0].Code = "changed";

        SubdivisionLookupResult second = catalog.FindSubdivisionByName("Florida");
        AssertAmbiguous(second, new[] { "US-FL", "UY-FD" });
    }

    [Test]
    public void CountryConstraintNormalizesCaseAndPreservesInCountryAmbiguity()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        AssertFound(
            catalog.FindSubdivisionByNameInCountry("us", "Florida"),
            "US-FL");
        AssertFound(
            catalog.FindSubdivisionByNameInCountry("uy", "Florida"),
            "UY-FD");
        AssertAmbiguous(
            catalog.FindSubdivisionByNameInCountry("bd", "Barishal"),
            new[] { "BD-06", "BD-A" });
        AssertStatus(
            catalog.FindSubdivisionByNameInCountry("CA", "Florida"),
            LookupStatus.NotFound);
        AssertStatus(
            catalog.FindSubdivisionByNameInCountry("ZZ", "Florida"),
            LookupStatus.NotFound);
    }

    [Test]
    public void NameLookupDistinguishesInvalidInputFromUnknownNames()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        AssertStatus(catalog.FindSubdivisionByName(null!), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindSubdivisionByName(string.Empty), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindSubdivisionByName("---"), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindSubdivisionByName("\uD800"), LookupStatus.InvalidInput);
        AssertStatus(
            catalog.FindSubdivisionByName("Unknown Province"),
            LookupStatus.NotFound);

        AssertStatus(
            catalog.FindSubdivisionByNameInCountry(null!, "Florida"),
            LookupStatus.InvalidInput);
        AssertStatus(
            catalog.FindSubdivisionByNameInCountry("U1", "Florida"),
            LookupStatus.InvalidInput);
        AssertStatus(
            catalog.FindSubdivisionByNameInCountry("US", string.Empty),
            LookupStatus.InvalidInput);
        AssertStatus(
            catalog.FindSubdivisionByNameInCountry("US", "\uD800"),
            LookupStatus.InvalidInput);
    }

    private static void AssertFound(SubdivisionLookupResult result, string code)
    {
        Assert.That(result.Status, Is.EqualTo(LookupStatus.Found));
        Assert.That(result.Subdivision, Is.Not.Null);
        Assert.That(result.Subdivision!.Code, Is.EqualTo(code));
        Assert.That(result.Candidates, Is.Empty);
    }

    private static void AssertAmbiguous(
        SubdivisionLookupResult result,
        string[] expectedCodes)
    {
        Assert.That(result.Status, Is.EqualTo(LookupStatus.Ambiguous));
        Assert.That(result.Subdivision, Is.Null);
        Assert.That(
            global::System.Array.ConvertAll(result.Candidates, value => value.Code),
            Is.EqualTo(expectedCodes));
    }

    private static void AssertStatus(SubdivisionLookupResult result, LookupStatus status)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.Subdivision, Is.Null);
        Assert.That(result.Candidates, Is.Empty);
    }
}
