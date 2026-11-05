using System.Globalization;
using System.Text;
using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class GeneratedGeographicRegionRowsTests
{
    [Test]
    public void AggregateRowsAreOrderedHierarchicalAndDistinctFromCountries()
    {
        GeneratedGeographicRegionRow[] rows = GeographicRegionTable.Rows;
        Assert.That(rows, Has.Length.EqualTo(30));

        HashSet<string> countryNumericCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedCountryRow country in CountryTable.Rows)
        {
            countryNumericCodes.Add(country.NumericText);
        }

        Dictionary<string, GeneratedGeographicRegionRow> byCode =
            new Dictionary<string, GeneratedGeographicRegionRow>(StringComparer.Ordinal);
        int worldCount = 0;
        int regionCount = 0;
        int subregionCount = 0;
        int intermediateCount = 0;
        string previous = string.Empty;
        foreach (GeneratedGeographicRegionRow row in rows)
        {
            Assert.That(string.CompareOrdinal(previous, row.M49Code), Is.LessThan(0), row.M49Code);
            Assert.That(IsNumeric3(row.M49Code), Is.True, row.M49Code);
            Assert.That(
                row.M49Value,
                Is.EqualTo(int.Parse(row.M49Code, CultureInfo.InvariantCulture)));
            Assert.That(countryNumericCodes.Contains(row.M49Code), Is.False, row.M49Code);
            Assert.That(byCode.TryAdd(row.M49Code, row), Is.True, row.M49Code);
            Assert.That(row.HasParent, Is.EqualTo(row.ParentM49Code.Length != 0));
            Assert.That(row.SourceId, Is.EqualTo("un-m49"));
            Assert.That(row.SourceRevision, Is.Not.Empty);
            Assert.That(row.UpstreamKey, Is.Not.Empty);
            Assert.That(row.EffectiveDate, Is.Not.Empty);
            Assert.That(row.SourceField, Does.EndWith("_code"));

            if (row.Level == GeneratedGeographicRegionLevel.World)
            {
                worldCount += 1;
                Assert.That(row.HasParent, Is.False);
            }
            else if (row.Level == GeneratedGeographicRegionLevel.Region)
            {
                regionCount += 1;
            }
            else if (row.Level == GeneratedGeographicRegionLevel.Subregion)
            {
                subregionCount += 1;
            }
            else
            {
                intermediateCount += 1;
            }

            previous = row.M49Code;
        }

        Assert.That(worldCount, Is.EqualTo(1));
        Assert.That(regionCount, Is.EqualTo(5));
        Assert.That(subregionCount, Is.EqualTo(17));
        Assert.That(intermediateCount, Is.EqualTo(7));
        Assert.That(byCode["001"].Level, Is.EqualTo(GeneratedGeographicRegionLevel.World));
        Assert.That(byCode["001"].HasParent, Is.False);

        foreach (GeneratedGeographicRegionRow row in rows)
        {
            if (!row.HasParent)
            {
                continue;
            }

            Assert.That(byCode.TryGetValue(row.ParentM49Code, out GeneratedGeographicRegionRow? parent), Is.True);
            Assert.That(parent, Is.Not.Null);
            Assert.That((int)parent!.Level + 1, Is.EqualTo((int)row.Level), row.M49Code);
        }
    }

    [Test]
    public void LocalizedNamesCoverEveryAggregateAndLocale()
    {
        GeneratedGeographicRegionNameRow[] rows = GeographicRegionTable.Names;
        Assert.That(rows, Has.Length.EqualTo(180));
        string[] expectedLocales = { "ar", "en", "es", "fr", "ru", "zh" };

        HashSet<string> regionCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (GeneratedGeographicRegionRow region in GeographicRegionTable.Rows)
        {
            regionCodes.Add(region.M49Code);
        }

        HashSet<string> locales = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, int> namesPerRegion = new Dictionary<string, int>(StringComparer.Ordinal);
        GeneratedGeographicRegionNameRow? previous = null;
        foreach (GeneratedGeographicRegionNameRow row in rows)
        {
            Assert.That(regionCodes.Contains(row.RegionM49Code), Is.True, row.RegionM49Code);
            Assert.That(row.Name, Is.Not.Empty);
            Assert.That(row.Name.IsNormalized(NormalizationForm.FormC), Is.True, row.Name);
            Assert.That(row.SourceId, Is.EqualTo("un-m49"));
            Assert.That(row.SourceRevision, Is.Not.Empty);
            Assert.That(row.UpstreamKey, Is.Not.Empty);
            Assert.That(row.EffectiveDate, Is.Not.Empty);
            Assert.That(row.SourceField, Does.EndWith("_name"));
            if (previous != null)
            {
                Assert.That(CompareNames(previous, row), Is.LessThan(0));
            }

            previous = row;
            locales.Add(row.Locale);
            namesPerRegion[row.RegionM49Code] =
                namesPerRegion.GetValueOrDefault(row.RegionM49Code) + 1;
        }

        Assert.That(locales, Is.EquivalentTo(expectedLocales));
        foreach (string regionCode in regionCodes)
        {
            Assert.That(namesPerRegion[regionCode], Is.EqualTo(expectedLocales.Length), regionCode);
        }

        Assert.That(FindName("001", "en").Name, Is.EqualTo("World"));
        Assert.That(FindName("001", "zh").Name, Is.EqualTo("世界"));
        Assert.That(
            FindName("419", "en").Name,
            Is.EqualTo("Latin America and the Caribbean"));
    }

    [Test]
    public void CountryMappingsMatchIsoRowsAndPreserveCompleteAncestry()
    {
        GeneratedCountryRegionMappingRow[] rows = GeographicRegionTable.CountryMappings;
        Assert.That(rows, Has.Length.EqualTo(248));

        Dictionary<string, GeneratedCountryRow> countries =
            new Dictionary<string, GeneratedCountryRow>(StringComparer.Ordinal);
        foreach (GeneratedCountryRow country in CountryTable.Rows)
        {
            countries.Add(country.Alpha2, country);
        }

        Dictionary<string, GeneratedGeographicRegionRow> regions =
            new Dictionary<string, GeneratedGeographicRegionRow>(StringComparer.Ordinal);
        foreach (GeneratedGeographicRegionRow region in GeographicRegionTable.Rows)
        {
            regions.Add(region.M49Code, region);
        }

        HashSet<string> mappedCountries = new HashSet<string>(StringComparer.Ordinal);
        string previous = string.Empty;
        foreach (GeneratedCountryRegionMappingRow row in rows)
        {
            Assert.That(string.CompareOrdinal(previous, row.CountryAlpha2), Is.LessThan(0));
            Assert.That(countries.TryGetValue(row.CountryAlpha2, out GeneratedCountryRow? country), Is.True);
            Assert.That(country, Is.Not.Null);
            Assert.That(row.CountryAlpha3, Is.EqualTo(country!.Alpha3));
            Assert.That(row.CountryNumericText, Is.EqualTo(country.NumericText));
            Assert.That(row.CountryNumericValue, Is.EqualTo(country.NumericValue));
            Assert.That(mappedCountries.Add(row.CountryAlpha2), Is.True);
            Assert.That(row.WorldM49Code, Is.EqualTo("001"));
            Assert.That(row.HasRegion, Is.EqualTo(row.RegionM49Code.Length != 0));
            Assert.That(row.HasSubregion, Is.EqualTo(row.SubregionM49Code.Length != 0));
            Assert.That(
                row.HasIntermediateRegion,
                Is.EqualTo(row.IntermediateRegionM49Code.Length != 0));
            Assert.That(row.HasSubregion && !row.HasRegion, Is.False, row.CountryAlpha2);
            Assert.That(
                row.HasIntermediateRegion && !row.HasSubregion,
                Is.False,
                row.CountryAlpha2);
            AssertMappingChain(row, regions);
            Assert.That(row.SourceId, Is.EqualTo("un-m49"));
            Assert.That(row.SourceRevision, Is.Not.Empty);
            Assert.That(row.UpstreamKey, Is.EqualTo(row.CountryNumericText));
            Assert.That(row.EffectiveDate, Is.Not.Empty);
            previous = row.CountryAlpha2;
        }

        Assert.That(countries.Keys.Except(mappedCountries), Is.EqualTo(new[] { "TW" }));
        AssertMapping("US", "019", "021", string.Empty);
        AssertMapping("GB", "150", "154", string.Empty);
        AssertMapping("ZA", "002", "202", "018");
    }

    private static void AssertMappingChain(
        GeneratedCountryRegionMappingRow mapping,
        IReadOnlyDictionary<string, GeneratedGeographicRegionRow> regions)
    {
        if (mapping.HasRegion)
        {
            Assert.That(
                regions[mapping.RegionM49Code].ParentM49Code,
                Is.EqualTo(mapping.WorldM49Code));
        }

        if (mapping.HasSubregion)
        {
            Assert.That(
                regions[mapping.SubregionM49Code].ParentM49Code,
                Is.EqualTo(mapping.RegionM49Code));
        }
        if (mapping.HasIntermediateRegion)
        {
            Assert.That(
                regions[mapping.IntermediateRegionM49Code].ParentM49Code,
                Is.EqualTo(mapping.SubregionM49Code));
        }
    }

    private static void AssertMapping(
        string alpha2,
        string region,
        string subregion,
        string intermediateRegion)
    {
        GeneratedCountryRegionMappingRow mapping = FindMapping(alpha2);
        Assert.That(mapping.RegionM49Code, Is.EqualTo(region));
        Assert.That(mapping.SubregionM49Code, Is.EqualTo(subregion));
        Assert.That(mapping.IntermediateRegionM49Code, Is.EqualTo(intermediateRegion));
        Assert.That(mapping.HasIntermediateRegion, Is.EqualTo(intermediateRegion.Length != 0));
    }

    private static GeneratedCountryRegionMappingRow FindMapping(string alpha2)
    {
        foreach (GeneratedCountryRegionMappingRow row in GeographicRegionTable.CountryMappings)
        {
            if (string.Equals(row.CountryAlpha2, alpha2, StringComparison.Ordinal))
            {
                return row;
            }
        }

        Assert.Fail($"Missing country-region mapping {alpha2}.");
        return GeographicRegionTable.CountryMappings[0];
    }

    private static GeneratedGeographicRegionNameRow FindName(string code, string locale)
    {
        foreach (GeneratedGeographicRegionNameRow row in GeographicRegionTable.Names)
        {
            if (string.Equals(row.RegionM49Code, code, StringComparison.Ordinal) &&
                string.Equals(row.Locale, locale, StringComparison.Ordinal))
            {
                return row;
            }
        }

        Assert.Fail($"Missing geographic-region name {code}:{locale}.");
        return GeographicRegionTable.Names[0];
    }

    private static int CompareNames(
        GeneratedGeographicRegionNameRow left,
        GeneratedGeographicRegionNameRow right)
    {
        int comparison = string.CompareOrdinal(left.RegionM49Code, right.RegionM49Code);
        return comparison == 0 ? string.CompareOrdinal(left.Locale, right.Locale) : comparison;
    }

    private static bool IsNumeric3(string value)
    {
        if (value.Length != 3)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character < '0' || character > '9')
            {
                return false;
            }
        }

        return true;
    }
}
