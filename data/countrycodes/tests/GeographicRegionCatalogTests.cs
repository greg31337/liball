using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class GeographicRegionCatalogTests
{
    [Test]
    public void EnumerationIsCompleteOrderedAndDetached()
    {
        IGeographicRegionCatalog catalog = new CountryCodeService();

        GeographicRegion[] first = catalog.GetGeographicRegions();
        Assert.That(first, Has.Length.EqualTo(30));
        Assert.That(first[0].M49Code, Is.EqualTo("001"));
        Assert.That(first[first.Length - 1].M49Code, Is.EqualTo("419"));
        Assert.That(
            first.Select(value => value.M49Code),
            Is.EqualTo(first.Select(value => value.M49Code).OrderBy(value => value)));
        first[0].M49Code = "999";
        first[0].LocalizedNames[0].Value = "changed";
        first[0].ChildM49Codes[0] = "999";
        first[0].CountryCodes[0] = "ZZ";

        GeographicRegion[] second = catalog.GetGeographicRegions();
        Assert.That(second[0].M49Code, Is.EqualTo("001"));
        Assert.That(second[0].LocalizedNames[0].Value, Is.Not.EqualTo("changed"));
        Assert.That(second[0].ChildM49Codes[0], Is.Not.EqualTo("999"));
        Assert.That(second[0].CountryCodes[0], Is.Not.EqualTo("ZZ"));
    }

    [Test]
    public void EnumerationByLevelReturnsOnlyAggregateRegions()
    {
        IGeographicRegionCatalog catalog = new CountryCodeService();

        AssertLevel(catalog, GeographicRegionLevel.World, 1);
        AssertLevel(catalog, GeographicRegionLevel.Region, 5);
        AssertLevel(catalog, GeographicRegionLevel.Subregion, 17);
        AssertLevel(catalog, GeographicRegionLevel.IntermediateRegion, 7);
        Assert.That(
            catalog.GetGeographicRegionsByLevel(GeographicRegionLevel.CountryOrArea),
            Is.Empty);
        Assert.That(
            catalog.GetGeographicRegionsByLevel((GeographicRegionLevel)999),
            Is.Empty);
    }

    [Test]
    public void LookupAcceptsOnlyKnownAggregateM49Codes()
    {
        IGeographicRegionCatalog catalog = new CountryCodeService();

        GeographicRegion? europe = catalog.FindGeographicRegionByM49("150");
        Assert.That(europe, Is.Not.Null);
        Assert.That(europe!.Name, Is.EqualTo("Europe"));
        Assert.That(europe.M49Value, Is.EqualTo(150));
        Assert.That(europe.Level, Is.EqualTo(GeographicRegionLevel.Region));
        Assert.That(europe.ParentM49Code, Is.EqualTo("001"));
        Assert.That(europe.ChildM49Codes, Is.EqualTo(new[] { "039", "151", "154", "155" }));
        europe.ChildM49Codes[0] = "999";

        Assert.That(
            catalog.FindGeographicRegionByM49("150")!.ChildM49Codes[0],
            Is.EqualTo("039"));
        Assert.That(catalog.FindGeographicRegionByM49(null!), Is.Null);
        Assert.That(catalog.FindGeographicRegionByM49(string.Empty), Is.Null);
        Assert.That(catalog.FindGeographicRegionByM49("15"), Is.Null);
        Assert.That(catalog.FindGeographicRegionByM49("15A"), Is.Null);
        Assert.That(catalog.FindGeographicRegionByM49("999"), Is.Null);
        Assert.That(catalog.FindGeographicRegionByM49("840"), Is.Null);
    }

    [Test]
    public void RegionAncestryRunsFromParentToWorld()
    {
        IGeographicRegionCatalog catalog = new CountryCodeService();

        GeographicRegion[] ancestry = catalog.GetGeographicRegionAncestry("018");
        Assert.That(
            ancestry.Select(value => value.M49Code),
            Is.EqualTo(new[] { "202", "002", "001" }));
        ancestry[0].M49Code = "999";
        Assert.That(
            catalog.GetGeographicRegionAncestry("018")[0].M49Code,
            Is.EqualTo("202"));

        Assert.That(catalog.GetGeographicRegionAncestry("001"), Is.Empty);
        Assert.That(catalog.GetGeographicRegionAncestry("999"), Is.Empty);
        Assert.That(catalog.GetGeographicRegionAncestry("840"), Is.Empty);
        Assert.That(catalog.GetGeographicRegionAncestry("01A"), Is.Empty);
    }

    [Test]
    public void CountryTraversalReturnsWorldToMostSpecificAggregate()
    {
        IGeographicRegionCatalog catalog = new CountryCodeService();

        AssertRegionCodes(
            catalog.GetGeographicRegionsForCountry("us"),
            new[] { "001", "019", "021" });
        GeographicRegion[] southAfrica =
            catalog.GetGeographicRegionsForCountry("ZA");
        AssertRegionCodes(
            southAfrica,
            new[] { "001", "002", "202", "018" });
        southAfrica[0].Name = "changed";
        Assert.That(
            catalog.GetGeographicRegionsForCountry("ZA")[0].Name,
            Is.EqualTo("World"));

        Assert.That(catalog.GetGeographicRegionsForCountry("TW"), Is.Empty);
        Assert.That(catalog.GetGeographicRegionsForCountry("ZZ"), Is.Empty);
        Assert.That(catalog.GetGeographicRegionsForCountry("001"), Is.Empty);
        Assert.That(catalog.GetGeographicRegionsForCountry(null!), Is.Empty);
    }

    [Test]
    public void RegionMembershipIncludesDescendantsInStableCountryOrder()
    {
        IGeographicRegionCatalog catalog = new CountryCodeService();

        Country[] northernAmerica = catalog.GetCountriesForGeographicRegion("021");
        Assert.That(
            northernAmerica.Select(value => value.Alpha2),
            Is.EqualTo(new[] { "BM", "CA", "GL", "PM", "US" }));
        northernAmerica[0].Alpha2 = "ZZ";
        Assert.That(
            catalog.GetCountriesForGeographicRegion("021")[0].Alpha2,
            Is.EqualTo("BM"));

        Country[] world = catalog.GetCountriesForGeographicRegion("001");
        Assert.That(world, Has.Length.EqualTo(248));
        Assert.That(world[0].Alpha2, Is.EqualTo("AD"));
        Assert.That(world[world.Length - 1].Alpha2, Is.EqualTo("ZW"));
        Assert.That(world.Select(value => value.Alpha2), Does.Not.Contain("TW"));

        Assert.That(catalog.GetCountriesForGeographicRegion("840"), Is.Empty);
        Assert.That(catalog.GetCountriesForGeographicRegion("999"), Is.Empty);
        Assert.That(catalog.GetCountriesForGeographicRegion("01A"), Is.Empty);
        Assert.That(catalog.GetCountriesForGeographicRegion(null!), Is.Empty);
    }

    private static void AssertLevel(
        IGeographicRegionCatalog catalog,
        GeographicRegionLevel level,
        int expectedCount)
    {
        GeographicRegion[] regions = catalog.GetGeographicRegionsByLevel(level);
        Assert.That(regions, Has.Length.EqualTo(expectedCount));
        Assert.That(regions.All(value => value.Level == level), Is.True);
    }

    private static void AssertRegionCodes(
        GeographicRegion[] regions,
        string[] expectedCodes)
    {
        Assert.That(
            regions.Select(value => value.M49Code),
            Is.EqualTo(expectedCodes));
    }
}
