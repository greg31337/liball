using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class DetachedMapperTests
{
    [Test]
    public void EveryGeneratedEntityMapsToACompletePublicSnapshot()
    {
        foreach (GeneratedCountryRow row in CountryTable.Rows)
        {
            Country country = CountryMapper.MapCountry(row);
            Assert.That(country.ShortName, Is.Not.Empty, row.Alpha2);
            Assert.That(country.CanonicalCode, Is.EqualTo(row.Alpha2));
        }

        foreach (GeneratedSubdivisionRow row in SubdivisionTable.Rows)
        {
            Subdivision subdivision = SubdivisionMapper.MapSubdivision(row);
            Assert.That(subdivision.Name, Is.Not.Empty, row.Code);
            Assert.That(subdivision.Code, Is.EqualTo(row.Code));
        }

        foreach (GeneratedHistoricalCountryRow row in HistoricalCountryTable.Rows)
        {
            HistoricalCountry country = HistoricalMapper.MapHistoricalCountry(row);
            Assert.That(country.Name, Is.Not.Empty, row.Alpha4);
            Assert.That(country.Changes, Has.Length.EqualTo(1), row.Alpha4);
        }

        foreach (GeneratedGeographicRegionRow row in GeographicRegionTable.Rows)
        {
            GeographicRegion region = RegionMapper.MapRegion(row);
            Assert.That(region.Name, Is.Not.Empty, row.M49Code);
            Assert.That(region.LocalizedNames, Has.Length.EqualTo(6), row.M49Code);
        }
    }

    [Test]
    public void CountryMapperBuildsCompleteDetachedSnapshots()
    {
        GeneratedCountryRow source = CountryIndex.FindCountryByAlpha2("CI")!;
        Country mapped = CountryMapper.MapCountry(source);

        Assert.That(mapped.Alpha2, Is.EqualTo("CI"));
        Assert.That(mapped.Alpha3, Is.EqualTo("CIV"));
        Assert.That(mapped.NumericText, Is.EqualTo("384"));
        Assert.That(mapped.NumericValue, Is.EqualTo(384));
        Assert.That(mapped.CanonicalCode, Is.EqualTo("CI"));
        Assert.That(mapped.AssignmentStatus, Is.EqualTo(CodeAssignmentStatus.Assigned));
        Assert.That(mapped.ShortName, Is.EqualTo("Côte d'Ivoire"));
        Assert.That(mapped.OfficialName, Is.EqualTo("Republic of Côte d'Ivoire"));
        Assert.That(mapped.Aliases, Is.EqualTo(new[] { "Ivory Coast" }));
        Assert.That(mapped.LocalizedNames, Is.Not.Empty);
        Assert.That(
            mapped.RegionM49Codes,
            Is.EqualTo(new[] { "001", "002", "202", "011" }));
        Assert.That(mapped.FlagEmoji, Is.EqualTo("🇨🇮"));

        mapped.Aliases[0] = "changed";
        mapped.LocalizedNames[0].Value = "changed";
        mapped.RegionM49Codes[0] = "999";
        mapped.Alpha2 = "XX";

        Country repeated = CountryMapper.MapCountry(source);
        Assert.That(repeated, Is.Not.SameAs(mapped));
        Assert.That(repeated.Alpha2, Is.EqualTo("CI"));
        Assert.That(repeated.Aliases, Is.EqualTo(new[] { "Ivory Coast" }));
        Assert.That(repeated.LocalizedNames[0].Value, Is.Not.EqualTo("changed"));
        Assert.That(repeated.RegionM49Codes[0], Is.EqualTo("001"));
    }

    [Test]
    public void CountryMapperDetachesEveryObjectInMappedArrays()
    {
        GeneratedCountryRow[] rows =
        {
            CountryIndex.FindCountryByAlpha2("DE")!,
            CountryIndex.FindCountryByAlpha2("FR")!
        };
        Country[] first = CountryMapper.MapCountries(rows);
        Country[] second = CountryMapper.MapCountries(rows);

        Assert.That(first, Is.Not.SameAs(second));
        Assert.That(first[0], Is.Not.SameAs(second[0]));
        Assert.That(first[0].LocalizedNames, Is.Not.SameAs(second[0].LocalizedNames));
        first[0].LocalizedNames[0].Value = "changed";
        first[1] = first[0];

        Assert.That(second[0].Alpha2, Is.EqualTo("DE"));
        Assert.That(second[0].LocalizedNames[0].Value, Is.Not.EqualTo("changed"));
        Assert.That(second[1].Alpha2, Is.EqualTo("FR"));
    }

    [Test]
    public void SubdivisionMapperDetachesNamesAliasesAndReplacementCodes()
    {
        GeneratedSubdivisionRow walesRow =
            SubdivisionIndex.FindSubdivisionByCode("GB-WLS")!;
        Subdivision wales = SubdivisionMapper.MapSubdivision(walesRow);

        Assert.That(wales.Code, Is.EqualTo("GB-WLS"));
        Assert.That(wales.Name, Is.EqualTo("Wales [Cymru GB-CYM]"));
        Assert.That(wales.Type, Is.EqualTo("Country"));
        Assert.That(wales.Status, Is.EqualTo(SubdivisionStatus.Active));
        Assert.That(wales.LocalizedNames, Has.Length.EqualTo(1));
        Assert.That(wales.LocalizedNames[0].Value, Is.EqualTo("Wales"));

        GeneratedSubdivisionRow beijingRow =
            SubdivisionIndex.FindSubdivisionByCode("CN-BJ")!;
        Subdivision beijing = SubdivisionMapper.MapSubdivision(beijingRow);
        Assert.That(beijing.Aliases, Does.Contain("CN-11"));

        GeneratedSubdivisionRow alandRow =
            SubdivisionIndex.FindSubdivisionByCode("FI-01")!;
        Subdivision aland = SubdivisionMapper.MapSubdivision(alandRow);
        Assert.That(aland.ReplacementCodes, Is.EqualTo(new[] { "AX" }));

        wales.LocalizedNames[0].Value = "changed";
        beijing.Aliases[0] = "changed";
        aland.ReplacementCodes[0] = "changed";
        Assert.That(
            SubdivisionMapper.MapSubdivision(walesRow).LocalizedNames[0].Value,
            Is.EqualTo("Wales"));
        Assert.That(
            SubdivisionMapper.MapSubdivision(beijingRow).Aliases,
            Does.Contain("CN-11"));
        Assert.That(
            SubdivisionMapper.MapSubdivision(alandRow).ReplacementCodes,
            Is.EqualTo(new[] { "AX" }));
    }

    [Test]
    public void HistoricalMapperDetachesTransitionGraphs()
    {
        GeneratedHistoricalCountryRow source =
            HistoricalIndex.FindHistoricalCountryByAlpha4("ANHH")!;
        HistoricalCountry mapped = HistoricalMapper.MapHistoricalCountry(source);

        Assert.That(mapped.Alpha2, Is.EqualTo("AN"));
        Assert.That(mapped.Alpha3, Is.EqualTo("ANT"));
        Assert.That(mapped.Alpha4, Is.EqualTo("ANHH"));
        Assert.That(mapped.NumericText, Is.EqualTo("530"));
        Assert.That(mapped.Name, Is.EqualTo("Netherlands Antilles"));
        Assert.That(mapped.PredecessorIdentifiers, Is.EqualTo(new[] { "ANHH" }));
        Assert.That(mapped.SuccessorIdentifiers, Is.EqualTo(new[] { "BQ", "CW", "SX" }));
        Assert.That(mapped.Changes, Has.Length.EqualTo(1));
        Assert.That(mapped.Changes[0].Kind, Is.EqualTo(CountryChangeKind.Split));
        Assert.That(
            mapped.Changes[0].SuccessorIdentifiers,
            Is.EqualTo(new[] { "BQ", "CW", "SX" }));

        mapped.PredecessorIdentifiers[0] = "changed";
        mapped.SuccessorIdentifiers[0] = "changed";
        mapped.Changes[0].SuccessorIdentifiers[0] = "changed";
        mapped.Changes[0].Alpha4 = "XXXX";

        HistoricalCountry repeated = HistoricalMapper.MapHistoricalCountry(source);
        Assert.That(repeated.PredecessorIdentifiers[0], Is.EqualTo("ANHH"));
        Assert.That(repeated.SuccessorIdentifiers[0], Is.EqualTo("BQ"));
        Assert.That(repeated.Changes[0].Alpha4, Is.EqualTo("ANHH"));
        Assert.That(repeated.Changes[0].SuccessorIdentifiers[0], Is.EqualTo("BQ"));
    }

    [Test]
    public void RegionMapperDetachesNamesHierarchyAndMembership()
    {
        GeneratedGeographicRegionRow source = RegionIndex.FindRegionByM49Code("150")!;
        GeographicRegion mapped = RegionMapper.MapRegion(source);

        Assert.That(mapped.M49Code, Is.EqualTo("150"));
        Assert.That(mapped.M49Value, Is.EqualTo(150));
        Assert.That(mapped.Name, Is.EqualTo("Europe"));
        Assert.That(mapped.Level, Is.EqualTo(GeographicRegionLevel.Region));
        Assert.That(mapped.ParentM49Code, Is.EqualTo("001"));
        Assert.That(mapped.LocalizedNames, Has.Length.EqualTo(6));
        Assert.That(mapped.ChildM49Codes, Is.EqualTo(new[] { "039", "151", "154", "155" }));
        Assert.That(mapped.CountryCodes, Does.Contain("DE"));
        Assert.That(mapped.CountryCodes, Does.Contain("FR"));

        mapped.LocalizedNames[0].Value = "changed";
        mapped.ChildM49Codes[0] = "999";
        mapped.CountryCodes[0] = "ZZ";
        GeographicRegion repeated = RegionMapper.MapRegion(source);

        Assert.That(repeated.LocalizedNames[0].Value, Is.Not.EqualTo("changed"));
        Assert.That(repeated.ChildM49Codes[0], Is.EqualTo("039"));
        Assert.That(repeated.CountryCodes[0], Is.Not.EqualTo("ZZ"));
    }

    [Test]
    public void MapperArrayResultsCanBeMutatedWithoutAffectingLaterResults()
    {
        GeneratedSubdivisionRow[] subdivisionRows =
        {
            SubdivisionIndex.FindSubdivisionByCode("AD-02")!,
            SubdivisionIndex.FindSubdivisionByCode("AD-03")!
        };
        Subdivision[] subdivisions = SubdivisionMapper.MapSubdivisions(subdivisionRows);
        subdivisions[0] = subdivisions[1];
        Assert.That(
            SubdivisionMapper.MapSubdivisions(subdivisionRows)[0].Code,
            Is.EqualTo("AD-02"));

        GeneratedHistoricalCountryRow[] historicalRows =
            HistoricalIndex.FindHistoricalCountriesByAlpha2("CS");
        HistoricalCountry[] historical =
            HistoricalMapper.MapHistoricalCountries(historicalRows);
        historical[0] = historical[1];
        Assert.That(
            HistoricalMapper.MapHistoricalCountries(historicalRows)[0].Alpha4,
            Is.EqualTo("CSHH"));

        GeneratedGeographicRegionRow[] regionRows =
        {
            RegionIndex.FindRegionByM49Code("001")!,
            RegionIndex.FindRegionByM49Code("002")!
        };
        GeographicRegion[] regions = RegionMapper.MapRegions(regionRows);
        regions[0] = regions[1];
        Assert.That(RegionMapper.MapRegions(regionRows)[0].M49Code, Is.EqualTo("001"));
    }
}
