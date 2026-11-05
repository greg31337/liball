using System.Globalization;

namespace currencycodes.Tests;

public class TerritoryLookupTests
{
    [Test]
    public void AlphaCodeLookupsTrimAndUppercaseAsciiInput()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        TerritoryLookupResult alpha2 = store.FindTerritoryAlpha2Lookup(" \u2003bg\u00A0 ");
        TerritoryLookupResult alpha3 = store.FindTerritoryAlpha3Lookup(" bGr ");

        Assert.Multiple(() =>
        {
            Assert.That(alpha2.Found, Is.True);
            Assert.That(alpha2.Error, Is.EqualTo(LookupError.None));
            Assert.That(alpha2.NormalizedQuery, Is.EqualTo("BG"));
            Assert.That(alpha2.Match?.CanonicalName, Is.EqualTo("Bulgaria"));
            Assert.That(alpha2.Match?.Alpha3Code, Is.EqualTo("BGR"));
            Assert.That(alpha3.Found, Is.True);
            Assert.That(alpha3.Error, Is.EqualTo(LookupError.None));
            Assert.That(alpha3.NormalizedQuery, Is.EqualTo("BGR"));
            Assert.That(alpha3.Match?.Alpha2Code, Is.EqualTo("BG"));
        });
    }

    [Test]
    public void AlphaCodeLookupsRejectMalformedAndNonAsciiInput()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        TerritoryLookupResult nullAlpha2 = store.FindTerritoryAlpha2Lookup(null);
        TerritoryLookupResult shortAlpha2 = store.FindTerritoryAlpha2Lookup("B");
        TerritoryLookupResult digitAlpha2 = store.FindTerritoryAlpha2Lookup("B1");
        TerritoryLookupResult nonAsciiAlpha2 = store.FindTerritoryAlpha2Lookup("BĞ");
        TerritoryLookupResult longAlpha3 = store.FindTerritoryAlpha3Lookup("BGRR");
        TerritoryLookupResult spaceAlpha3 = store.FindTerritoryAlpha3Lookup("BG R");

        Assert.Multiple(() =>
        {
            AssertMalformed(nullAlpha2);
            AssertMalformed(shortAlpha2);
            AssertMalformed(digitAlpha2);
            AssertMalformed(nonAsciiAlpha2);
            AssertMalformed(longAlpha3);
            AssertMalformed(spaceAlpha3);
        });
    }

    [Test]
    public void NumericLookupRequiresCanonicalAsciiDigitsAndPreservesLeadingZeroes()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        TerritoryLookupResult valid = store.FindTerritoryNumericLookup(" \t004\r\n ");
        TerritoryLookupResult shortCode = store.FindTerritoryNumericLookup("4");
        TerritoryLookupResult signed = store.FindTerritoryNumericLookup("+04");
        TerritoryLookupResult localized = store.FindTerritoryNumericLookup("٠٠٤");
        TerritoryLookupResult fullWidth = store.FindTerritoryNumericLookup("００４");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Found, Is.True);
            Assert.That(valid.Error, Is.EqualTo(LookupError.None));
            Assert.That(valid.NormalizedQuery, Is.EqualTo("004"));
            Assert.That(valid.Match?.CanonicalName, Is.EqualTo("Afghanistan"));
            Assert.That(valid.Match?.NumericCodeValue, Is.EqualTo(4));
            AssertMalformed(shortCode);
            AssertMalformed(signed);
            AssertMalformed(localized);
            AssertMalformed(fullWidth);
        });
    }

    [Test]
    public void WellFormedUnassignedCodesReturnStructuredFailures()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        TerritoryLookupResult alpha2 = store.FindTerritoryAlpha2Lookup("aa");
        TerritoryLookupResult alpha3 = store.FindTerritoryAlpha3Lookup("aaa");
        TerritoryLookupResult numeric = store.FindTerritoryNumericLookup("000");

        Assert.Multiple(() =>
        {
            AssertUnassigned(alpha2, "AA");
            AssertUnassigned(alpha3, "AAA");
            AssertUnassigned(numeric, "000");
        });
    }

    [Test]
    public void ReusedNumericTerritoryCodesReturnCanonicalAmbiguityResults()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        TerritoryLookupResult result = store.FindTerritoryNumericLookup("104");

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.True);
            Assert.That(result.Error, Is.EqualTo(LookupError.Ambiguous));
            Assert.That(result.NormalizedQuery, Is.EqualTo("104"));
            Assert.That(result.Match?.CanonicalName, Is.EqualTo("Burma"));
            Assert.That(
                Array.ConvertAll(result.Matches, territory => territory.CanonicalName),
                Is.EqualTo(new[] { "Burma", "Myanmar" }));
            Assert.That(
                Array.ConvertAll(result.Matches, territory => territory.Alpha2Code),
                Is.EqualTo(new[] { "BU", "MM" }));
        });
    }

    [Test]
    public void OfficialNameLookupSupportsExactAndOrdinalIgnoreCaseModes()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        TerritoryLookupResult exact = store.FindTerritoryOfficialNameLookup(
            "Bulgaria",
            NameMatchMode.ExactOrdinal);
        TerritoryLookupResult exactMiss = store.FindTerritoryOfficialNameLookup(
            "bulgaria",
            NameMatchMode.ExactOrdinal);
        TerritoryLookupResult ignoreCase = store.FindTerritoryOfficialNameLookup(
            " \u2003bUlGaRiA ",
            NameMatchMode.ExactOrdinalIgnoreCase);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Found, Is.True);
            Assert.That(exact.Error, Is.EqualTo(LookupError.None));
            Assert.That(exact.NormalizedQuery, Is.EqualTo("Bulgaria"));
            Assert.That(exact.Match?.Alpha2Code, Is.EqualTo("BG"));
            Assert.That(exactMiss.Error, Is.EqualTo(LookupError.Unassigned));
            Assert.That(exactMiss.NormalizedQuery, Is.EqualTo("bulgaria"));
            Assert.That(ignoreCase.Found, Is.True);
            Assert.That(ignoreCase.Error, Is.EqualTo(LookupError.None));
            Assert.That(ignoreCase.NormalizedQuery, Is.EqualTo("bUlGaRiA"));
            Assert.That(ignoreCase.Match?.Alpha2Code, Is.EqualTo("BG"));
        });
    }

    [Test]
    public void TerritoryNameLookupIsCultureIndependentAndDoesNotFoldText()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

            TerritoryLookupResult iraq = store.FindTerritoryOfficialNameLookup(
                "iraq",
                NameMatchMode.ExactOrdinalIgnoreCase);
            TerritoryLookupResult noAccent = store.FindTerritoryOfficialNameLookup(
                "Aland Islands",
                NameMatchMode.ExactOrdinalIgnoreCase);
            TerritoryLookupResult extraSpace = store.FindTerritoryOfficialNameLookup(
                "United  Kingdom of Great Britain and Northern Ireland",
                NameMatchMode.ExactOrdinalIgnoreCase);

            Assert.Multiple(() =>
            {
                Assert.That(iraq.Found, Is.True);
                Assert.That(iraq.Match?.Alpha2Code, Is.EqualTo("IQ"));
                Assert.That(noAccent.Error, Is.EqualTo(LookupError.Unassigned));
                Assert.That(extraSpace.Error, Is.EqualTo(LookupError.Unassigned));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void NullOrEmptyTerritoryNameIsMalformed()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        TerritoryLookupResult nullName = store.FindTerritoryOfficialNameLookup(
            null,
            NameMatchMode.ExactOrdinal);
        TerritoryLookupResult emptyName = store.FindTerritoryOfficialNameLookup(
            " \u2003 ",
            NameMatchMode.ExactOrdinalIgnoreCase);

        Assert.Multiple(() =>
        {
            AssertMalformed(nullName);
            AssertMalformed(emptyName);
        });
    }

    [Test]
    public void TerritoryLookupResultsOwnTheirPrimaryAndMatchSnapshots()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        TerritoryLookupResult first = store.FindTerritoryNumericLookup("104");

        Assert.Multiple(() =>
        {
            Assert.That(first.Match, Is.Not.Null);
            Assert.That(first.Match, Is.Not.SameAs(first.Matches[0]));
            Assert.That(first.Matches, Has.Length.EqualTo(2));
        });

        first.Match!.CanonicalName = "changed";
        first.Matches[0].CanonicalName = "changed again";
        first.Matches[0].Alpha2Code = "XX";

        TerritoryLookupResult second = store.FindTerritoryNumericLookup("104");

        Assert.Multiple(() =>
        {
            Assert.That(second.Match?.CanonicalName, Is.EqualTo("Burma"));
            Assert.That(second.Matches[0].CanonicalName, Is.EqualTo("Burma"));
            Assert.That(second.Matches[0].Alpha2Code, Is.EqualTo("BU"));
        });
    }

    [Test]
    public void InvalidTerritoryNameModeIsRejected()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.That(
            () => store.FindTerritoryOfficialNameLookup("Bulgaria", (NameMatchMode)99),
            Throws.TypeOf<System.ArgumentException>());
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

    private static void AssertUnassigned(TerritoryLookupResult result, string normalized)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.False);
            Assert.That(result.Error, Is.EqualTo(LookupError.Unassigned));
            Assert.That(result.NormalizedQuery, Is.EqualTo(normalized));
            Assert.That(result.Match, Is.Null);
            Assert.That(result.Matches, Is.Empty);
        });
    }
}
