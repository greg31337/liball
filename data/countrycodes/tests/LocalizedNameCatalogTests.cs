using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class LocalizedNameCatalogTests
{
    [Test]
    public void SupportedLocalesAreCanonicalOrderedAndDetached()
    {
        ILocalizedNameCatalog catalog = new CountryCodeService();

        string[] first = catalog.GetSupportedLocales();
        Assert.That(first, Is.EqualTo(new[] { "ar", "en", "es", "fr", "ru", "zh" }));
        first[0] = "changed";

        Assert.That(
            catalog.GetSupportedLocales(),
            Is.EqualTo(new[] { "ar", "en", "es", "fr", "ru", "zh" }));
    }

    [Test]
    public void LocaleAvailabilityRequiresAnExactCanonicalSupportedTag()
    {
        ILocalizedNameCatalog catalog = new CountryCodeService();

        foreach (string locale in catalog.GetSupportedLocales())
        {
            Assert.That(catalog.IsLocaleAvailable(locale), Is.True, locale);
        }

        Assert.That(catalog.IsLocaleAvailable("EN"), Is.False);
        Assert.That(catalog.IsLocaleAvailable("en-US"), Is.False);
        Assert.That(catalog.IsLocaleAvailable("de"), Is.False);
        Assert.That(catalog.IsLocaleAvailable("iw"), Is.False);
        Assert.That(catalog.IsLocaleAvailable("invalid_tag"), Is.False);
        Assert.That(catalog.IsLocaleAvailable(null!), Is.False);
    }

    [Test]
    public void FallbackChainsAreCanonicalDeterministicAndDetached()
    {
        ILocalizedNameCatalog catalog = new CountryCodeService();

        string[] first = catalog.GetLocaleFallbackChain("FR-ca");
        Assert.That(first, Is.EqualTo(new[] { "fr-CA", "fr" }));
        first[0] = "changed";
        Assert.That(
            catalog.GetLocaleFallbackChain("FR-ca"),
            Is.EqualTo(new[] { "fr-CA", "fr" }));
        Assert.That(
            catalog.GetLocaleFallbackChain("iw-IL"),
            Is.EqualTo(new[] { "he-IL", "he" }));
        Assert.That(catalog.GetLocaleFallbackChain("invalid_tag"), Is.Empty);
        Assert.That(catalog.GetLocaleFallbackChain(null!), Is.Empty);
    }

    [Test]
    public void CountryNamesUseCanonicalLocalesAndPinnedDisplayPrecedence()
    {
        ILocalizedNameCatalog catalog = new CountryCodeService();

        Assert.That(
            catalog.GetLocalizedCountryName("de", "FR", false),
            Is.EqualTo("Allemagne"));
        Assert.That(
            catalog.GetLocalizedCountryName("US", "en", false),
            Is.EqualTo("United States"));
        Assert.That(
            catalog.GetLocalizedCountryName("BQ", "en", false),
            Is.EqualTo("Caribbean Netherlands"));
        Assert.That(
            catalog.GetLocalizedCountryName("TW", "en", false),
            Is.EqualTo("Taiwan"));
    }

    [Test]
    public void CountryNameFallbackIsExplicitAndMissingDataRemainsMissing()
    {
        ILocalizedNameCatalog catalog = new CountryCodeService();

        Assert.That(
            catalog.GetLocalizedCountryName("DE", "fr-CA", false),
            Is.Null);
        Assert.That(
            catalog.GetLocalizedCountryName("DE", "fr-CA", true),
            Is.EqualTo("Allemagne"));
        Assert.That(
            catalog.GetLocalizedCountryName("TW", "fr", true),
            Is.Null);
        Assert.That(
            catalog.GetLocalizedCountryName("ZZ", "en", true),
            Is.Null);
        Assert.That(
            catalog.GetLocalizedCountryName("DE", "invalid_tag", true),
            Is.Null);
        Assert.That(
            catalog.GetLocalizedCountryName(null!, "en", true),
            Is.Null);
    }

    [Test]
    public void SubdivisionNamesPreferLocalizedRowsAndUseExplicitFallback()
    {
        ILocalizedNameCatalog catalog = new CountryCodeService();

        Assert.That(
            catalog.GetLocalizedSubdivisionName("us-ca", "EN", false),
            Is.EqualTo("California"));
        Assert.That(
            catalog.GetLocalizedSubdivisionName("gb-wls", "en", false),
            Is.EqualTo("Wales"));
        Assert.That(
            catalog.GetLocalizedSubdivisionName("GB-WLS", "en-US", false),
            Is.Null);
        Assert.That(
            catalog.GetLocalizedSubdivisionName("GB-WLS", "en-US", true),
            Is.EqualTo("Wales"));
        Assert.That(
            catalog.GetLocalizedSubdivisionName("GB-WLS", "fr", true),
            Is.Null);
        Assert.That(
            catalog.GetLocalizedSubdivisionName("US_CA", "en", true),
            Is.Null);
        Assert.That(
            catalog.GetLocalizedSubdivisionName("US-ZZZ", "en", true),
            Is.Null);
    }

    [Test]
    public void RegionNamesUseOnlyAggregateRegionsAndExplicitFallback()
    {
        ILocalizedNameCatalog catalog = new CountryCodeService();

        Assert.That(
            catalog.GetLocalizedRegionName("001", "zh", false),
            Is.EqualTo("世界"));
        Assert.That(
            catalog.GetLocalizedRegionName("001", "zh-Hant-TW", false),
            Is.Null);
        Assert.That(
            catalog.GetLocalizedRegionName("001", "zh-Hant-TW", true),
            Is.EqualTo("世界"));
        Assert.That(
            catalog.GetLocalizedRegionName("150", "FR", false),
            Is.EqualTo("Europe"));
        Assert.That(
            catalog.GetLocalizedRegionName("840", "en", true),
            Is.Null);
        Assert.That(
            catalog.GetLocalizedRegionName("999", "en", true),
            Is.Null);
        Assert.That(
            catalog.GetLocalizedRegionName(null!, "en", true),
            Is.Null);
    }
}
