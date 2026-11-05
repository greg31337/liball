using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class InternalIndexTests
{
    [Test]
    public void CountryIndexResolvesEveryCanonicalCodeFormOrdinally()
    {
        Assert.That(CountryIndex.GetCountryCount(), Is.EqualTo(249));
        Assert.That(CountryIndex.GetCountryAt(0)!.Alpha2, Is.EqualTo("AD"));
        Assert.That(CountryIndex.GetCountryAt(248)!.Alpha2, Is.EqualTo("ZW"));
        Assert.That(CountryIndex.GetCountryAt(-1), Is.Null);
        Assert.That(CountryIndex.GetCountryAt(249), Is.Null);

        Assert.That(CountryIndex.FindCountryByAlpha2("FR")!.Alpha3, Is.EqualTo("FRA"));
        Assert.That(CountryIndex.FindCountryByAlpha3("FRA")!.Alpha2, Is.EqualTo("FR"));
        Assert.That(
            CountryIndex.FindCountryByNumericText("250")!.Alpha2,
            Is.EqualTo("FR"));
        Assert.That(CountryIndex.FindCountryByAlpha2("fr"), Is.Null);
        Assert.That(CountryIndex.FindCountryByAlpha2(null), Is.Null);
        Assert.That(CountryIndex.FindCountryByAlpha3("ZZZ"), Is.Null);

        GeneratedReservedCodeRow? reserved = CountryIndex.FindReservedCode(
            GeneratedCodeKind.Alpha2,
            "AN");
        Assert.That(reserved, Is.Not.Null);
        Assert.That(reserved!.AssignmentState, Is.EqualTo(GeneratedCodeAssignmentState.FormerlyAssigned));
    }

    [Test]
    public void SubdivisionIndexFindsCodesCountryRangesAliasesAndAllReplacements()
    {
        Assert.That(SubdivisionIndex.GetSubdivisionCount(), Is.EqualTo(5046));
        Assert.That(SubdivisionIndex.GetSubdivisionAt(0)!.Code, Is.EqualTo("AD-02"));
        Assert.That(SubdivisionIndex.GetSubdivisionAt(5046), Is.Null);
        Assert.That(
            SubdivisionIndex.FindSubdivisionByCode("GB-WLS")!.CountryCode,
            Is.EqualTo("GB"));
        Assert.That(SubdivisionIndex.FindSubdivisionByCode("gb-wls"), Is.Null);

        GeneratedSubdivisionRow[] andorra =
            SubdivisionIndex.GetSubdivisionsForCountry("AD");
        Assert.That(andorra, Has.Length.EqualTo(7));
        Assert.That(andorra[0].Code, Is.EqualTo("AD-02"));
        Assert.That(andorra[6].Code, Is.EqualTo("AD-08"));
        Assert.That(SubdivisionIndex.GetSubdivisionsForCountry("XX"), Is.Empty);

        GeneratedSubdivisionAliasRow? alias =
            SubdivisionIndex.FindSubdivisionAliasByCode("NZ-N");
        Assert.That(alias, Is.Not.Null);
        Assert.That(alias!.Reason, Is.EqualTo(GeneratedSubdivisionAliasReason.Deprecated));
        GeneratedSubdivisionReplacementRow[] replacements =
            SubdivisionIndex.GetReplacementsForAlias("NZ-N");
        Assert.That(replacements, Has.Length.EqualTo(9));
        Assert.That(replacements[0].ReplacementCode, Is.EqualTo("NZ-AUK"));
        Assert.That(replacements[8].ReplacementCode, Is.EqualTo("NZ-WKO"));

        replacements[0] = replacements[8];
        Assert.That(
            SubdivisionIndex.GetReplacementsForAlias("NZ-N")[0].ReplacementCode,
            Is.EqualTo("NZ-AUK"));
    }

    [Test]
    public void HistoricalIndexPreservesAmbiguityAndTransitionLinks()
    {
        Assert.That(HistoricalIndex.GetHistoricalCountryCount(), Is.EqualTo(31));
        Assert.That(HistoricalIndex.GetHistoricalCountryAt(0)!.Alpha4, Is.EqualTo("AIDJ"));
        Assert.That(HistoricalIndex.GetHistoricalCountryAt(31), Is.Null);

        GeneratedHistoricalCountryRow[] czechoslovakiaAndSerbiaMontenegro =
            HistoricalIndex.FindHistoricalCountriesByAlpha2("CS");
        Assert.That(czechoslovakiaAndSerbiaMontenegro, Has.Length.EqualTo(2));
        Assert.That(czechoslovakiaAndSerbiaMontenegro[0].Alpha4, Is.EqualTo("CSHH"));
        Assert.That(czechoslovakiaAndSerbiaMontenegro[1].Alpha4, Is.EqualTo("CSXX"));
        Assert.That(
            HistoricalIndex.FindHistoricalCountriesByAlpha3("SCG")[0].Alpha4,
            Is.EqualTo("CSXX"));
        Assert.That(
            HistoricalIndex.FindHistoricalCountriesByNumericText("200")[0].Alpha4,
            Is.EqualTo("CSHH"));
        Assert.That(
            HistoricalIndex.FindHistoricalCountryByAlpha4("YUCS")!.Alpha3,
            Is.EqualTo("YUG"));
        Assert.That(HistoricalIndex.FindHistoricalCountriesByAlpha2(null), Is.Empty);

        GeneratedCountryChangeRow? change =
            HistoricalIndex.FindCountryChangeByAlpha4("ANHH");
        Assert.That(change, Is.Not.Null);
        Assert.That(change!.Kind, Is.EqualTo(GeneratedCountryChangeKind.Split));
        GeneratedCountryChangeLinkRow[] successors =
            HistoricalIndex.GetSuccessorsForChange("ANHH");
        Assert.That(successors, Has.Length.EqualTo(3));
        Assert.That(successors[0].TargetCode, Is.EqualTo("BQ"));
        Assert.That(successors[1].TargetCode, Is.EqualTo("CW"));
        Assert.That(successors[2].TargetCode, Is.EqualTo("SX"));
        Assert.That(HistoricalIndex.GetPredecessorsForChange("missing"), Is.Empty);

        successors[0] = successors[2];
        Assert.That(
            HistoricalIndex.GetSuccessorsForChange("ANHH")[0].TargetCode,
            Is.EqualTo("BQ"));
    }

    [Test]
    public void NameIndexReturnsCompleteKindSpecificMatchesAndTargetGroups()
    {
        GeneratedCountryNameRow[] countryMatches =
            NameIndex.FindCurrentCountryNamesBySearchKey("cote d ivoire");
        Assert.That(countryMatches, Has.Length.EqualTo(5));
        foreach (GeneratedCountryNameRow match in countryMatches)
        {
            Assert.That(match.TargetCode, Is.EqualTo("CI"));
            Assert.That(match.TargetKind, Is.EqualTo(GeneratedCountryNameTargetKind.Country));
        }

        GeneratedSubdivisionNameRow[] subdivisionMatches =
            NameIndex.FindSubdivisionNamesBySearchKey("georgia");
        Assert.That(subdivisionMatches, Has.Length.EqualTo(1));
        Assert.That(subdivisionMatches[0].SubdivisionCode, Is.EqualTo("US-GA"));
        Assert.That(
            NameIndex.FindHistoricalCountryNamesBySearchKey("netherlands antilles")[0].TargetCode,
            Is.EqualTo("ANHH"));
        Assert.That(
            NameIndex.FindRegionNamesBySearchKey("world")[0].RegionM49Code,
            Is.EqualTo("001"));

        Assert.That(NameIndex.GetNamesForRegion("001"), Has.Length.EqualTo(6));
        Assert.That(NameIndex.GetNamesForSubdivision("GB-WLS"), Has.Length.EqualTo(2));
        Assert.That(NameIndex.GetNamesForHistoricalCountry("ANHH"), Has.Length.EqualTo(1));
        Assert.That(NameIndex.GetNamesForCountry("CI"), Is.Not.Empty);
        Assert.That(NameIndex.FindCurrentCountryNamesBySearchKey("missing name"), Is.Empty);
        Assert.That(NameIndex.GetNamesForCountry(null), Is.Empty);

        countryMatches[0] = countryMatches[countryMatches.Length - 1];
        Assert.That(
            NameIndex.FindCurrentCountryNamesBySearchKey("cote d ivoire")[0].SourceId,
            Is.EqualTo("debian-iso-codes"));
    }

    [Test]
    public void RegionIndexResolvesHierarchyAndCountryMembership()
    {
        Assert.That(RegionIndex.GetRegionCount(), Is.EqualTo(30));
        Assert.That(RegionIndex.GetRegionAt(0)!.M49Code, Is.EqualTo("001"));
        Assert.That(RegionIndex.GetRegionAt(30), Is.Null);
        Assert.That(
            RegionIndex.FindRegionByM49Code("419")!.Level,
            Is.EqualTo(GeneratedGeographicRegionLevel.Subregion));
        Assert.That(RegionIndex.FindRegionByM49Code("999"), Is.Null);

        GeneratedGeographicRegionRow[] worldChildren = RegionIndex.GetChildRegions("001");
        Assert.That(worldChildren, Has.Length.EqualTo(5));
        Assert.That(worldChildren[0].M49Code, Is.EqualTo("002"));
        Assert.That(worldChildren[4].M49Code, Is.EqualTo("150"));
        Assert.That(RegionIndex.GetChildRegions(null), Is.Empty);

        GeneratedCountryRegionMappingRow? unitedStates =
            RegionIndex.FindCountryRegionMapping("US");
        Assert.That(unitedStates, Is.Not.Null);
        Assert.That(unitedStates!.RegionM49Code, Is.EqualTo("019"));
        Assert.That(unitedStates.SubregionM49Code, Is.EqualTo("021"));
        Assert.That(RegionIndex.FindCountryRegionMapping("TW"), Is.Null);

        GeneratedCountryRegionMappingRow[] northernAmerica =
            RegionIndex.GetCountryMappingsForRegion("021");
        Assert.That(northernAmerica, Has.Length.EqualTo(5));
        Assert.That(northernAmerica[0].CountryAlpha2, Is.EqualTo("BM"));
        Assert.That(northernAmerica[4].CountryAlpha2, Is.EqualTo("US"));
        Assert.That(
            RegionIndex.GetCountryMappingsForRegion("001"),
            Has.Length.EqualTo(248));

        northernAmerica[0] = northernAmerica[4];
        Assert.That(
            RegionIndex.GetCountryMappingsForRegion("021")[0].CountryAlpha2,
            Is.EqualTo("BM"));
    }
}
