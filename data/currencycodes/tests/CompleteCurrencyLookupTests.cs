using System.Collections.Generic;

namespace currencycodes.Tests;

public class CompleteCurrencyLookupTests
{
    private static readonly CurrencySet[] CurrencySets = new[]
    {
        CurrencySet.ActiveOnly,
        CurrencySet.HistoricalOnly,
        CurrencySet.All
    };

    [Test]
    public void AlphabeticLookupCoversEveryDesignationInEveryScope()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        HashSet<string> testedCodes = new HashSet<string>(StringComparer.Ordinal);

        for (int rowIndex = 0; rowIndex < GeneratedCurrencyRows.Rows.Length; rowIndex++)
        {
            string code = GeneratedCurrencyRows.Rows[rowIndex].AlphabeticCode;
            if (!testedCodes.Add(code))
            {
                continue;
            }

            for (int setIndex = 0; setIndex < CurrencySets.Length; setIndex++)
            {
                CurrencySet currencySet = CurrencySets[setIndex];
                CurrencyRow[] expected = FindAlphabeticRows(code, currencySet);
                CurrencyLookupResult actual = catalog.FindCurrencyByAlphabeticCode(
                    " \t" + code.ToLowerInvariant() + "\u00A0 ",
                    currencySet);

                AssertLookup(actual, expected, code, true);
            }
        }

        Assert.That(testedCodes, Has.Count.GreaterThan(0));
    }

    [Test]
    public void NumericLookupCoversEveryAssignedNumericCodeInEveryScope()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        HashSet<string> testedCodes = new HashSet<string>(StringComparer.Ordinal);

        for (int rowIndex = 0; rowIndex < GeneratedCurrencyRows.Rows.Length; rowIndex++)
        {
            CurrencyRow row = GeneratedCurrencyRows.Rows[rowIndex];
            if (!row.HasNumericCode || !testedCodes.Add(row.NumericCode))
            {
                continue;
            }

            for (int setIndex = 0; setIndex < CurrencySets.Length; setIndex++)
            {
                CurrencySet currencySet = CurrencySets[setIndex];
                CurrencyRow[] expected = FindNumericRows(row.NumericCode, currencySet);
                CurrencyLookupResult actual = catalog.FindCurrenciesByNumericCode(
                    " \t" + row.NumericCode + "\u00A0 ",
                    currencySet);

                AssertLookup(actual, expected, row.NumericCode, false);
            }
        }

        Assert.That(testedCodes, Has.Count.GreaterThan(0));
    }

    [Test]
    public void ExactOfficialNameLookupCoversEveryNamedDesignationInEveryScope()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        HashSet<string> testedNames = new HashSet<string>(StringComparer.Ordinal);

        for (int rowIndex = 0; rowIndex < GeneratedCurrencyRows.Rows.Length; rowIndex++)
        {
            string name = AsciiCodeText.TrimWhitespace(
                GeneratedCurrencyRows.Rows[rowIndex].OfficialName);
            if (name.Length == 0 || !testedNames.Add(name))
            {
                continue;
            }

            for (int setIndex = 0; setIndex < CurrencySets.Length; setIndex++)
            {
                CurrencySet currencySet = CurrencySets[setIndex];
                CurrencyRow[] expected = FindNameRows(name, currencySet, false);
                CurrencyLookupResult actual = catalog.FindCurrenciesByOfficialName(
                    " \t" + name + "\u00A0 ",
                    NameMatchMode.ExactOrdinal,
                    currencySet);

                AssertLookup(actual, expected, name, true);
            }
        }

        Assert.That(testedNames, Has.Count.GreaterThan(0));
    }

    [Test]
    public void IgnoreCaseOfficialNameLookupCoversEveryNamedDesignationInEveryScope()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();
        HashSet<string> testedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int rowIndex = 0; rowIndex < GeneratedCurrencyRows.Rows.Length; rowIndex++)
        {
            string name = AsciiCodeText.TrimWhitespace(
                GeneratedCurrencyRows.Rows[rowIndex].OfficialName);
            if (name.Length == 0 || !testedNames.Add(name))
            {
                continue;
            }

            string query = SwapAsciiLetterCase(name);
            for (int setIndex = 0; setIndex < CurrencySets.Length; setIndex++)
            {
                CurrencySet currencySet = CurrencySets[setIndex];
                CurrencyRow[] expected = FindNameRows(name, currencySet, true);
                CurrencyLookupResult actual = catalog.FindCurrenciesByOfficialName(
                    " \t" + query + "\u00A0 ",
                    NameMatchMode.ExactOrdinalIgnoreCase,
                    currencySet);

                AssertLookup(actual, expected, query, true);
            }
        }

        Assert.That(testedNames, Has.Count.GreaterThan(0));
    }

    [Test]
    public void LeadingZeroReuseDuplicateAndTrimmedSourceNameCasesStayExplicit()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();

        CurrencyLookupResult leadingZero = catalog.FindCurrenciesByNumericCode(
            "008",
            CurrencySet.ActiveOnly);
        CurrencyLookupResult reusedNumeric = catalog.FindCurrenciesByNumericCode(
            "032",
            CurrencySet.All);
        CurrencyLookupResult duplicateName = catalog.FindCurrenciesByOfficialName(
            "Afghani",
            NameMatchMode.ExactOrdinal,
            CurrencySet.All);
        CurrencyLookupResult activePaddedName = catalog.FindCurrenciesByOfficialName(
            "Comorian Franc",
            NameMatchMode.ExactOrdinal,
            CurrencySet.ActiveOnly);
        CurrencyLookupResult historicalPaddedName = catalog.FindCurrenciesByOfficialName(
            "new romanian leu",
            NameMatchMode.ExactOrdinalIgnoreCase,
            CurrencySet.HistoricalOnly);

        Assert.Multiple(() =>
        {
            Assert.That(leadingZero.NormalizedQuery, Is.EqualTo("008"));
            Assert.That(leadingZero.Match?.AlphabeticCode, Is.EqualTo("ALL"));
            Assert.That(
                Array.ConvertAll(reusedNumeric.Matches, currency => currency.AlphabeticCode),
                Is.EqualTo(new[] { "ARA", "ARP", "ARS", "ARY" }));
            Assert.That(duplicateName.Error, Is.EqualTo(LookupError.Ambiguous));
            Assert.That(
                Array.ConvertAll(duplicateName.Matches, currency => currency.AlphabeticCode),
                Is.EqualTo(new[] { "AFA", "AFN" }));
            Assert.That(activePaddedName.Match?.AlphabeticCode, Is.EqualTo("KMF"));
            Assert.That(activePaddedName.Match?.OfficialName, Is.EqualTo("Comorian Franc "));
            Assert.That(historicalPaddedName.Match?.AlphabeticCode, Is.EqualTo("RON"));
            Assert.That(historicalPaddedName.Match?.OfficialName, Is.EqualTo("New Romanian Leu "));
        });
    }

    [Test]
    public void PublicLookupsReturnEveryStructuredInputFailure()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();

        for (int index = 0; index < CurrencySets.Length; index++)
        {
            CurrencySet currencySet = CurrencySets[index];
            AssertMalformed(catalog.FindCurrencyByAlphabeticCode(null!, currencySet));
            AssertMalformed(catalog.FindCurrencyByAlphabeticCode("U1D", currencySet));
            AssertMalformed(catalog.FindCurrenciesByNumericCode(null!, currencySet));
            AssertMalformed(catalog.FindCurrenciesByNumericCode("84", currencySet));
            AssertMalformed(catalog.FindCurrenciesByOfficialName(
                null!,
                NameMatchMode.ExactOrdinal,
                currencySet));
            AssertMalformed(catalog.FindCurrenciesByOfficialName(
                " \u2003 ",
                NameMatchMode.ExactOrdinalIgnoreCase,
                currencySet));
        }

        Assert.Multiple(() =>
        {
            AssertUnassigned(catalog.FindCurrencyByAlphabeticCode("ZZZ", CurrencySet.All), "ZZZ");
            AssertUnassigned(catalog.FindCurrenciesByNumericCode("000", CurrencySet.All), "000");
            AssertUnassigned(
                catalog.FindCurrenciesByOfficialName(
                    "Not a currency",
                    NameMatchMode.ExactOrdinal,
                    CurrencySet.All),
                "Not a currency");
            Assert.That(
                () => catalog.FindCurrencyByAlphabeticCode("USD", (CurrencySet)99),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => catalog.FindCurrenciesByNumericCode("840", (CurrencySet)99),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => catalog.FindCurrenciesByOfficialName(
                    "US Dollar",
                    (NameMatchMode)99,
                    CurrencySet.All),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void RequiredLookupReturnsOneDesignationAndThrowsForEveryFailureCategory()
    {
        ICurrencyCatalog catalog = new CurrencyCodeService();

        CurrencyInfo currency = catalog.GetRequiredCurrencyByAlphabeticCode(
            " usd ",
            CurrencySet.ActiveOnly);

        Assert.Multiple(() =>
        {
            Assert.That(currency.AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(
                () => catalog.GetRequiredCurrencyByAlphabeticCode(null!, CurrencySet.All),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code is malformed."));
            Assert.That(
                () => catalog.GetRequiredCurrencyByAlphabeticCode("ZZZ", CurrencySet.All),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code 'ZZZ' is not assigned."));
            Assert.That(
                () => catalog.GetRequiredCurrencyByAlphabeticCode("BGN", CurrencySet.ActiveOnly),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo(
                        "Currency alphabetic code 'BGN' is outside the requested scope."));
            Assert.That(
                () => catalog.GetRequiredCurrencyByAlphabeticCode("EUR", CurrencySet.All),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo(
                        "Currency alphabetic code 'EUR' identifies more than one designation in the requested scope."));
        });
    }

    private static CurrencyRow[] FindAlphabeticRows(string code, CurrencySet currencySet)
    {
        return Array.FindAll(
            GeneratedCurrencyRows.Rows,
            row => row.AlphabeticCode == code && IncludesStatus(row, currencySet));
    }

    private static CurrencyRow[] FindNumericRows(string code, CurrencySet currencySet)
    {
        return Array.FindAll(
            GeneratedCurrencyRows.Rows,
            row => row.HasNumericCode
                && row.NumericCode == code
                && IncludesStatus(row, currencySet));
    }

    private static CurrencyRow[] FindNameRows(
        string name,
        CurrencySet currencySet,
        bool ignoreCase)
    {
        StringComparison comparison = ignoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return Array.FindAll(
            GeneratedCurrencyRows.Rows,
            row => AsciiCodeText.TrimWhitespace(row.OfficialName).Length > 0
                && string.Equals(
                    AsciiCodeText.TrimWhitespace(row.OfficialName),
                    name,
                    comparison)
                && IncludesStatus(row, currencySet));
    }

    private static bool IncludesStatus(CurrencyRow row, CurrencySet currencySet)
    {
        return currencySet == CurrencySet.All
            || (currencySet == CurrencySet.ActiveOnly && row.Status == CurrencyStatus.Active)
            || (currencySet == CurrencySet.HistoricalOnly
                && row.Status == CurrencyStatus.Historical);
    }

    private static void AssertLookup(
        CurrencyLookupResult actual,
        CurrencyRow[] expected,
        string normalizedQuery,
        bool ambiguityIsAnError)
    {
        string message = normalizedQuery;
        if (expected.Length == 0)
        {
            Assert.Multiple(() =>
            {
                Assert.That(actual.Found, Is.False, message);
                Assert.That(actual.Error, Is.EqualTo(LookupError.OutOfScope), message);
                Assert.That(actual.NormalizedQuery, Is.EqualTo(normalizedQuery), message);
                Assert.That(actual.Match, Is.Null, message);
                Assert.That(actual.Matches, Is.Empty, message);
            });
            return;
        }

        LookupError expectedError = ambiguityIsAnError && expected.Length > 1
            ? LookupError.Ambiguous
            : LookupError.None;
        Assert.Multiple(() =>
        {
            Assert.That(actual.Found, Is.True, message);
            Assert.That(actual.Error, Is.EqualTo(expectedError), message);
            Assert.That(actual.NormalizedQuery, Is.EqualTo(normalizedQuery), message);
            Assert.That(actual.Match, Is.Not.Null, message);
            Assert.That(actual.Matches, Has.Length.EqualTo(expected.Length), message);
        });
        AssertCurrencyMatchesRow(actual.Match!, expected[0], message);
        for (int index = 0; index < expected.Length; index++)
        {
            AssertCurrencyMatchesRow(actual.Matches[index], expected[index], message);
        }
    }

    private static void AssertCurrencyMatchesRow(
        CurrencyInfo actual,
        CurrencyRow expected,
        string message)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.AlphabeticCode, Is.EqualTo(expected.AlphabeticCode), message);
            Assert.That(actual.HasNumericCode, Is.EqualTo(expected.HasNumericCode), message);
            Assert.That(actual.NumericCode, Is.EqualTo(expected.NumericCode), message);
            Assert.That(actual.NumericCodeValue, Is.EqualTo(expected.NumericCodeValue), message);
            Assert.That(actual.OfficialName, Is.EqualTo(expected.OfficialName), message);
            Assert.That(actual.HasIsoMinorUnit, Is.EqualTo(expected.HasIsoMinorUnit), message);
            Assert.That(actual.IsoMinorUnitDigits, Is.EqualTo(expected.IsoMinorUnitDigits), message);
            Assert.That(actual.Status, Is.EqualTo(expected.Status), message);
            Assert.That(actual.Kind, Is.EqualTo(expected.Kind), message);
            Assert.That(actual.IdentityFromIso, Is.EqualTo(expected.IdentityFromIso), message);
            Assert.That(actual.HistoricalStatusFromIso, Is.EqualTo(expected.HistoricalStatusFromIso), message);
            Assert.That(actual.FundClassificationFromIso, Is.EqualTo(expected.FundClassificationFromIso), message);
            Assert.That(actual.EnrichedFromCldr, Is.EqualTo(expected.EnrichedFromCldr), message);
            AssertPartialDate(actual.IntroductionDate, expected.IntroductionDate, message);
            AssertPartialDate(actual.WithdrawalDate, expected.WithdrawalDate, message);
        });
    }

    private static void AssertPartialDate(
        PartialDate actual,
        PartialDate expected,
        string message)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.IsKnown, Is.EqualTo(expected.IsKnown), message);
            Assert.That(actual.Year, Is.EqualTo(expected.Year), message);
            Assert.That(actual.Month, Is.EqualTo(expected.Month), message);
            Assert.That(actual.Day, Is.EqualTo(expected.Day), message);
            Assert.That(actual.Precision, Is.EqualTo(expected.Precision), message);
        });
    }

    private static void AssertMalformed(CurrencyLookupResult result)
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

    private static void AssertUnassigned(CurrencyLookupResult result, string normalizedQuery)
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
