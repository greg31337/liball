using System.Globalization;

namespace currencycodes.Tests;

public class CurrencyOfficialNameLookupTests
{
    [Test]
    public void ExactLookupTrimsOuterWhitespaceAndPreservesQueryText()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult result = store.FindCurrencyOfficialNameLookup(
            " \t\u2003US Dollar\u00A0\r\n",
            NameMatchMode.ExactOrdinal,
            CurrencySet.ActiveOnly);

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.True);
            Assert.That(result.Error, Is.EqualTo(LookupError.None));
            Assert.That(result.NormalizedQuery, Is.EqualTo("US Dollar"));
            Assert.That(result.Match?.AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(result.Matches, Has.Length.EqualTo(1));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\u2003\u00A0")]
    public void NullOrEmptyNameReturnsMalformedInput(string? name)
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult result = store.FindCurrencyOfficialNameLookup(
            name,
            NameMatchMode.ExactOrdinal,
            CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.False);
            Assert.That(result.Error, Is.EqualTo(LookupError.MalformedInput));
            Assert.That(result.NormalizedQuery, Is.Empty);
            Assert.That(result.Match, Is.Null);
            Assert.That(result.Matches, Is.Empty);
        });
    }

    [Test]
    public void ExactAndIgnoreCaseModesHaveDistinctOrdinalBehavior()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult exact = store.FindCurrencyOfficialNameLookup(
            "afghani",
            NameMatchMode.ExactOrdinal,
            CurrencySet.All);
        CurrencyLookupResult ignoreCase = store.FindCurrencyOfficialNameLookup(
            " aFgHaNi ",
            NameMatchMode.ExactOrdinalIgnoreCase,
            CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Found, Is.False);
            Assert.That(exact.Error, Is.EqualTo(LookupError.Unassigned));
            Assert.That(exact.NormalizedQuery, Is.EqualTo("afghani"));
            Assert.That(ignoreCase.Found, Is.True);
            Assert.That(ignoreCase.Error, Is.EqualTo(LookupError.Ambiguous));
            Assert.That(ignoreCase.NormalizedQuery, Is.EqualTo("aFgHaNi"));
            Assert.That(
                Array.ConvertAll(ignoreCase.Matches, currency => currency.AlphabeticCode),
                Is.EqualTo(new[] { "AFA", "AFN" }));
        });
    }

    [Test]
    public void AmbiguousNameResultsRemainCanonicalAfterScopeFiltering()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult all = store.FindCurrencyOfficialNameLookup(
            "Afghani",
            NameMatchMode.ExactOrdinal,
            CurrencySet.All);
        CurrencyLookupResult active = store.FindCurrencyOfficialNameLookup(
            "Afghani",
            NameMatchMode.ExactOrdinal,
            CurrencySet.ActiveOnly);
        CurrencyLookupResult historical = store.FindCurrencyOfficialNameLookup(
            "Afghani",
            NameMatchMode.ExactOrdinal,
            CurrencySet.HistoricalOnly);

        Assert.Multiple(() =>
        {
            Assert.That(all.Error, Is.EqualTo(LookupError.Ambiguous));
            Assert.That(all.Match?.AlphabeticCode, Is.EqualTo("AFA"));
            Assert.That(
                Array.ConvertAll(all.Matches, currency => currency.AlphabeticCode),
                Is.EqualTo(new[] { "AFA", "AFN" }));
            Assert.That(active.Error, Is.EqualTo(LookupError.None));
            Assert.That(active.Match?.AlphabeticCode, Is.EqualTo("AFN"));
            Assert.That(historical.Error, Is.EqualTo(LookupError.None));
            Assert.That(historical.Match?.AlphabeticCode, Is.EqualTo("AFA"));
        });
    }

    [Test]
    public void IgnoreCaseLookupIsIndependentOfCurrentCulture()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

            CurrencyLookupResult result = store.FindCurrencyOfficialNameLookup(
                "iraqi dinar",
                NameMatchMode.ExactOrdinalIgnoreCase,
                CurrencySet.ActiveOnly);

            Assert.Multiple(() =>
            {
                Assert.That(result.Found, Is.True);
                Assert.That(result.Error, Is.EqualTo(LookupError.None));
                Assert.That(result.Match?.AlphabeticCode, Is.EqualTo("IQD"));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void LookupDoesNotFoldAccentsPunctuationOrInternalWhitespace()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult noAccent = store.FindCurrencyOfficialNameLookup(
            "Bolivar Soberano",
            NameMatchMode.ExactOrdinalIgnoreCase,
            CurrencySet.All);
        CurrencyLookupResult accented = store.FindCurrencyOfficialNameLookup(
            "Bolívar Soberano",
            NameMatchMode.ExactOrdinal,
            CurrencySet.All);
        CurrencyLookupResult extraSpace = store.FindCurrencyOfficialNameLookup(
            "US  Dollar",
            NameMatchMode.ExactOrdinalIgnoreCase,
            CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(noAccent.Error, Is.EqualTo(LookupError.Unassigned));
            Assert.That(accented.Found, Is.True);
            Assert.That(accented.Error, Is.EqualTo(LookupError.Ambiguous));
            Assert.That(
                Array.ConvertAll(accented.Matches, currency => currency.AlphabeticCode),
                Is.EqualTo(new[] { "VED", "VES" }));
            Assert.That(extraSpace.Error, Is.EqualTo(LookupError.Unassigned));
        });
    }

    [Test]
    public void ExistingNameOutsideTheScopeIsReportedSeparately()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult result = store.FindCurrencyOfficialNameLookup(
            "US Dollar",
            NameMatchMode.ExactOrdinal,
            CurrencySet.HistoricalOnly);

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.False);
            Assert.That(result.Error, Is.EqualTo(LookupError.OutOfScope));
            Assert.That(result.NormalizedQuery, Is.EqualTo("US Dollar"));
            Assert.That(result.Match, Is.Null);
            Assert.That(result.Matches, Is.Empty);
        });
    }

    [Test]
    public void NameLookupReturnsDetachedPrimaryAndMatchSnapshots()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CurrencyLookupResult first = store.FindCurrencyOfficialNameLookup(
            "Afghani",
            NameMatchMode.ExactOrdinal,
            CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(first.Match, Is.Not.Null);
            Assert.That(first.Match, Is.Not.SameAs(first.Matches[0]));
            Assert.That(first.Match?.WithdrawalDate, Is.Not.SameAs(first.Matches[0].WithdrawalDate));
        });

        first.Match!.AlphabeticCode = "XXX";
        first.Matches[0].AlphabeticCode = "XTS";
        first.Matches[0].WithdrawalDate.Year = 1;

        CurrencyLookupResult second = store.FindCurrencyOfficialNameLookup(
            "Afghani",
            NameMatchMode.ExactOrdinal,
            CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(second.Match?.AlphabeticCode, Is.EqualTo("AFA"));
            Assert.That(second.Matches[0].AlphabeticCode, Is.EqualTo("AFA"));
            Assert.That(second.Matches[0].WithdrawalDate.Year, Is.EqualTo(2003));
        });
    }

    [Test]
    public void InvalidNameModeOrScopeIsRejected()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => store.FindCurrencyOfficialNameLookup(
                    "US Dollar",
                    (NameMatchMode)99,
                    CurrencySet.All),
                Throws.TypeOf<System.ArgumentException>());
            Assert.That(
                () => store.FindCurrencyOfficialNameLookup(
                    "US Dollar",
                    NameMatchMode.ExactOrdinal,
                    (CurrencySet)99),
                Throws.TypeOf<System.ArgumentException>());
        });
    }
}
