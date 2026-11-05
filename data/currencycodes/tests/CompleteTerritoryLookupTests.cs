using System.Collections.Generic;

namespace currencycodes.Tests;

public class CompleteTerritoryLookupTests
{
    [Test]
    public void Alpha2LookupCoversEveryKnownMapping()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        int count = 0;

        for (int index = 0; index < GeneratedTerritoryRows.Rows.Length; index++)
        {
            TerritoryRow row = GeneratedTerritoryRows.Rows[index];
            if (!row.HasAlpha2Code)
            {
                continue;
            }

            TerritoryLookupResult result = catalog.FindTerritoryByAlpha2Code(
                " \t" + row.Alpha2Code.ToLowerInvariant() + "\u00A0 ");
            AssertLookup(result, new[] { row }, row.Alpha2Code);
            count++;
        }

        Assert.That(count, Is.EqualTo(268));
    }

    [Test]
    public void Alpha3LookupCoversEveryKnownMapping()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        int count = 0;

        for (int index = 0; index < GeneratedTerritoryRows.Rows.Length; index++)
        {
            TerritoryRow row = GeneratedTerritoryRows.Rows[index];
            if (!row.HasAlpha3Code)
            {
                continue;
            }

            TerritoryLookupResult result = catalog.FindTerritoryByAlpha3Code(
                " \t" + row.Alpha3Code.ToLowerInvariant() + "\u00A0 ");
            AssertLookup(result, new[] { row }, row.Alpha3Code);
            count++;
        }

        Assert.That(count, Is.EqualTo(265));
    }

    [Test]
    public void NumericLookupCoversEveryKnownMappingAndAmbiguity()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        HashSet<string> testedCodes = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < GeneratedTerritoryRows.Rows.Length; index++)
        {
            TerritoryRow row = GeneratedTerritoryRows.Rows[index];
            if (!row.HasNumericCode || !testedCodes.Add(row.NumericCode))
            {
                continue;
            }

            TerritoryRow[] expected = Array.FindAll(
                GeneratedTerritoryRows.Rows,
                candidate => candidate.HasNumericCode
                    && candidate.NumericCode == row.NumericCode);
            TerritoryLookupResult result = catalog.FindTerritoriesByNumericCode(
                " \t" + row.NumericCode + "\u00A0 ");

            AssertLookup(result, expected, row.NumericCode);
        }

        Assert.That(testedCodes, Has.Count.EqualTo(258));
    }

    [Test]
    public void ExactNameLookupCoversEveryTerritoryAndNamedEntity()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        HashSet<string> testedNames = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < GeneratedTerritoryRows.Rows.Length; index++)
        {
            TerritoryRow row = GeneratedTerritoryRows.Rows[index];
            if (!testedNames.Add(row.CanonicalName))
            {
                continue;
            }

            TerritoryRow[] expected = FindNameRows(row.CanonicalName, false);
            TerritoryLookupResult result = catalog.FindTerritoriesByOfficialName(
                " \t" + row.CanonicalName + "\u00A0 ",
                NameMatchMode.ExactOrdinal);

            AssertLookup(result, expected, row.CanonicalName);
        }

        Assert.That(testedNames, Has.Count.EqualTo(287));
    }

    [Test]
    public void IgnoreCaseNameLookupCoversEveryTerritoryAndNamedEntity()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        HashSet<string> testedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < GeneratedTerritoryRows.Rows.Length; index++)
        {
            TerritoryRow row = GeneratedTerritoryRows.Rows[index];
            if (!testedNames.Add(row.CanonicalName))
            {
                continue;
            }

            string query = SwapAsciiLetterCase(row.CanonicalName);
            TerritoryRow[] expected = FindNameRows(row.CanonicalName, true);
            TerritoryLookupResult result = catalog.FindTerritoriesByOfficialName(
                " \t" + query + "\u00A0 ",
                NameMatchMode.ExactOrdinalIgnoreCase);

            AssertLookup(result, expected, query);
        }

        Assert.That(testedNames, Has.Count.EqualTo(287));
    }

    [Test]
    public void EveryEntityWithoutAlpha2MappingRemainsReachableByName()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        TerritoryRow[] namedEntities = Array.FindAll(
            GeneratedTerritoryRows.Rows,
            row => !row.HasAlpha2Code);

        for (int index = 0; index < namedEntities.Length; index++)
        {
            TerritoryRow row = namedEntities[index];
            TerritoryLookupResult result = catalog.FindTerritoriesByOfficialName(
                row.CanonicalName,
                NameMatchMode.ExactOrdinal);

            AssertLookup(result, new[] { row }, row.CanonicalName);
        }

        Assert.Multiple(() =>
        {
            Assert.That(namedEntities, Has.Length.EqualTo(19));
            Assert.That(
                Array.ConvertAll(namedEntities, row => row.CanonicalName),
                Does.Contain("International Monetary Fund (IMF)"));
            Assert.That(
                Array.ConvertAll(namedEntities, row => row.CanonicalName),
                Does.Contain("ISO 4217 Testing Code"));
            Assert.That(
                Array.ConvertAll(namedEntities, row => row.CanonicalName),
                Does.Contain("ISO 4217 No Currency"));
        });
    }

    [TestCase("Naoero", "NR", "NRU", "520")]
    [TestCase("Netherlands (Kingdom of the)", "NL", "NLD", "528")]
    [TestCase("State of Palestine", "PS", "PSE", "275")]
    [TestCase("Türkiye", "TR", "TUR", "792")]
    [TestCase("China, Hong Kong Special Administrative Region", "HK", "HKG", "344")]
    [TestCase("Saint Helena", "SH", "SHN", "654")]
    public void ReviewedAliasReconciliationProducesCanonicalMappings(
        string name,
        string alpha2,
        string alpha3,
        string numeric)
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();

        TerritoryLookupResult result = catalog.FindTerritoriesByOfficialName(
            name,
            NameMatchMode.ExactOrdinal);

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.True);
            Assert.That(result.Error, Is.EqualTo(LookupError.None));
            Assert.That(result.Match?.CanonicalName, Is.EqualTo(name));
            Assert.That(result.Match?.Alpha2Code, Is.EqualTo(alpha2));
            Assert.That(result.Match?.Alpha3Code, Is.EqualTo(alpha3));
            Assert.That(result.Match?.NumericCode, Is.EqualTo(numeric));
        });
    }

    [TestCase("104", "Burma", "Myanmar")]
    [TestCase("180", "Democratic Republic of the Congo", "Zaire")]
    [TestCase("626", "East Timor", "Timor-Leste")]
    [TestCase("891", "Serbia and Montenegro", "Yugoslavia")]
    public void ReusedNumericCodesReturnEveryCanonicalAmbiguousMatch(
        string code,
        string firstName,
        string secondName)
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();

        TerritoryLookupResult result = catalog.FindTerritoriesByNumericCode(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.True);
            Assert.That(result.Error, Is.EqualTo(LookupError.Ambiguous));
            Assert.That(result.NormalizedQuery, Is.EqualTo(code));
            Assert.That(result.Match?.CanonicalName, Is.EqualTo(firstName));
            Assert.That(
                Array.ConvertAll(result.Matches, territory => territory.CanonicalName),
                Is.EqualTo(new[] { firstName, secondName }));
        });
    }

    [Test]
    public void PublicTerritoryLookupsReturnStructuredMalformedAndUnassignedResults()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();

        Assert.Multiple(() =>
        {
            AssertMalformed(catalog.FindTerritoryByAlpha2Code(null!));
            AssertMalformed(catalog.FindTerritoryByAlpha2Code("B1"));
            AssertMalformed(catalog.FindTerritoryByAlpha2Code("BĞ"));
            AssertMalformed(catalog.FindTerritoryByAlpha3Code(null!));
            AssertMalformed(catalog.FindTerritoryByAlpha3Code("BG"));
            AssertMalformed(catalog.FindTerritoryByAlpha3Code("B1R"));
            AssertMalformed(catalog.FindTerritoriesByNumericCode(null!));
            AssertMalformed(catalog.FindTerritoriesByNumericCode("04"));
            AssertMalformed(catalog.FindTerritoriesByNumericCode("٠٠٤"));
            AssertMalformed(catalog.FindTerritoriesByOfficialName(
                null!,
                NameMatchMode.ExactOrdinal));
            AssertMalformed(catalog.FindTerritoriesByOfficialName(
                " \u2003 ",
                NameMatchMode.ExactOrdinalIgnoreCase));
            AssertUnassigned(catalog.FindTerritoryByAlpha2Code("AA"), "AA");
            AssertUnassigned(catalog.FindTerritoryByAlpha3Code("AAA"), "AAA");
            AssertUnassigned(catalog.FindTerritoriesByNumericCode("000"), "000");
            AssertUnassigned(
                catalog.FindTerritoriesByOfficialName(
                    "Not a territory",
                    NameMatchMode.ExactOrdinal),
                "Not a territory");
            Assert.That(
                () => catalog.FindTerritoriesByOfficialName(
                    "Bulgaria",
                    (NameMatchMode)99),
                Throws.TypeOf<ArgumentException>());
        });
    }

    private static TerritoryRow[] FindNameRows(string name, bool ignoreCase)
    {
        StringComparison comparison = ignoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return Array.FindAll(
            GeneratedTerritoryRows.Rows,
            row => string.Equals(row.CanonicalName, name, comparison));
    }

    private static void AssertLookup(
        TerritoryLookupResult actual,
        TerritoryRow[] expected,
        string normalizedQuery)
    {
        string message = normalizedQuery;
        LookupError expectedError = expected.Length == 1
            ? LookupError.None
            : LookupError.Ambiguous;
        Assert.Multiple(() =>
        {
            Assert.That(actual.Found, Is.True, message);
            Assert.That(actual.Error, Is.EqualTo(expectedError), message);
            Assert.That(actual.NormalizedQuery, Is.EqualTo(normalizedQuery), message);
            Assert.That(actual.Match, Is.Not.Null, message);
            Assert.That(actual.Matches, Has.Length.EqualTo(expected.Length), message);
        });
        AssertTerritoryMatchesRow(actual.Match!, expected[0], message);
        for (int index = 0; index < expected.Length; index++)
        {
            AssertTerritoryMatchesRow(actual.Matches[index], expected[index], message);
        }
    }

    private static void AssertTerritoryMatchesRow(
        TerritoryInfo actual,
        TerritoryRow expected,
        string message)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.CanonicalName, Is.EqualTo(expected.CanonicalName), message);
            Assert.That(actual.HasAlpha2Code, Is.EqualTo(expected.HasAlpha2Code), message);
            Assert.That(actual.Alpha2Code, Is.EqualTo(expected.Alpha2Code), message);
            Assert.That(actual.HasAlpha3Code, Is.EqualTo(expected.HasAlpha3Code), message);
            Assert.That(actual.Alpha3Code, Is.EqualTo(expected.Alpha3Code), message);
            Assert.That(actual.HasNumericCode, Is.EqualTo(expected.HasNumericCode), message);
            Assert.That(actual.NumericCode, Is.EqualTo(expected.NumericCode), message);
            Assert.That(actual.NumericCodeValue, Is.EqualTo(expected.NumericCodeValue), message);
            Assert.That(actual.Kind, Is.EqualTo(expected.Kind), message);
        });
    }

    private static void AssertMalformed(TerritoryLookupResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.False);
            Assert.That(result.Error, Is.EqualTo(LookupError.MalformedInput));
            Assert.That(result.NormalizedQuery, Is.Empty);
            Assert.That(result.Match, Is.Null);
            Assert.That(result.Matches, Is.Empty);
        });
    }

    private static void AssertUnassigned(TerritoryLookupResult result, string normalizedQuery)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.False);
            Assert.That(result.Error, Is.EqualTo(LookupError.Unassigned));
            Assert.That(result.NormalizedQuery, Is.EqualTo(normalizedQuery));
            Assert.That(result.Match, Is.Null);
            Assert.That(result.Matches, Is.Empty);
        });
    }

    private static string SwapAsciiLetterCase(string value)
    {
        char[] characters = value.ToCharArray();
        for (int index = 0; index < characters.Length; index++)
        {
            if (characters[index] >= 'A' && characters[index] <= 'Z')
            {
                characters[index] = (char)(characters[index] + 32);
            }
            else if (characters[index] >= 'a' && characters[index] <= 'z')
            {
                characters[index] = (char)(characters[index] - 32);
            }
        }

        return new string(characters);
    }
}
