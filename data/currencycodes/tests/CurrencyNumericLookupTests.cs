namespace currencycodes.Tests;

public class CurrencyNumericLookupTests
{
    [Test]
    public void LookupTrimsButPreservesCanonicalLeadingZeroes()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult result = store.FindCurrencyNumericLookup(" \t\u2003008\u00A0\r\n", CurrencySet.ActiveOnly);

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.True);
            Assert.That(result.Error, Is.EqualTo(LookupError.None));
            Assert.That(result.NormalizedQuery, Is.EqualTo("008"));
            Assert.That(result.Match?.AlphabeticCode, Is.EqualTo("ALL"));
            Assert.That(result.Match?.NumericCode, Is.EqualTo("008"));
            Assert.That(result.Matches, Has.Length.EqualTo(1));
            Assert.That(result.Matches[0].NumericCodeValue, Is.EqualTo(8));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("32")]
    [TestCase("0032")]
    [TestCase("+32")]
    [TestCase("-32")]
    [TestCase("03 2")]
    [TestCase("03A")]
    [TestCase("032.0")]
    [TestCase("٠٣٢")]
    [TestCase("０３２")]
    public void StrictCanonicalValidationRejectsMalformedNumericText(string? code)
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult result = store.FindCurrencyNumericLookup(code, CurrencySet.All);

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
    public void CanonicalButUnassignedNumericCodeReturnsStructuredFailure()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult result = store.FindCurrencyNumericLookup("000", CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.False);
            Assert.That(result.Error, Is.EqualTo(LookupError.Unassigned));
            Assert.That(result.NormalizedQuery, Is.EqualTo("000"));
            Assert.That(result.Match, Is.Null);
            Assert.That(result.Matches, Is.Empty);
        });
    }

    [Test]
    public void ScopeFilteringDistinguishesOutOfScopeFromUnassigned()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult activeOutsideHistory = store.FindCurrencyNumericLookup("840", CurrencySet.HistoricalOnly);
        CurrencyLookupResult historyOutsideActive = store.FindCurrencyNumericLookup("975", CurrencySet.ActiveOnly);

        Assert.Multiple(() =>
        {
            Assert.That(activeOutsideHistory.Found, Is.False);
            Assert.That(activeOutsideHistory.Error, Is.EqualTo(LookupError.OutOfScope));
            Assert.That(activeOutsideHistory.NormalizedQuery, Is.EqualTo("840"));
            Assert.That(activeOutsideHistory.Matches, Is.Empty);
            Assert.That(historyOutsideActive.Found, Is.False);
            Assert.That(historyOutsideActive.Error, Is.EqualTo(LookupError.OutOfScope));
            Assert.That(historyOutsideActive.NormalizedQuery, Is.EqualTo("975"));
            Assert.That(historyOutsideActive.Matches, Is.Empty);
        });
    }

    [Test]
    public void HistoricalNumericReuseReturnsEveryMatchInCanonicalOrder()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult all = store.FindCurrencyNumericLookup("032", CurrencySet.All);
        CurrencyLookupResult active = store.FindCurrencyNumericLookup("032", CurrencySet.ActiveOnly);
        CurrencyLookupResult historical = store.FindCurrencyNumericLookup("032", CurrencySet.HistoricalOnly);

        Assert.Multiple(() =>
        {
            Assert.That(all.Found, Is.True);
            Assert.That(all.Error, Is.EqualTo(LookupError.None));
            Assert.That(all.Match?.AlphabeticCode, Is.EqualTo("ARA"));
            Assert.That(
                Array.ConvertAll(all.Matches, currency => currency.AlphabeticCode),
                Is.EqualTo(new[] { "ARA", "ARP", "ARS", "ARY" }));
            Assert.That(active.Error, Is.EqualTo(LookupError.None));
            Assert.That(
                Array.ConvertAll(active.Matches, currency => currency.AlphabeticCode),
                Is.EqualTo(new[] { "ARS" }));
            Assert.That(historical.Error, Is.EqualTo(LookupError.None));
            Assert.That(
                Array.ConvertAll(historical.Matches, currency => currency.AlphabeticCode),
                Is.EqualTo(new[] { "ARA", "ARP", "ARY" }));
        });
    }

    [Test]
    public void ResultGraphIsDetachedForMultipleNumericMatches()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CurrencyLookupResult first = store.FindCurrencyNumericLookup("032", CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(first.Match, Is.Not.Null);
            Assert.That(first.Match, Is.Not.SameAs(first.Matches[0]));
            Assert.That(first.Match?.WithdrawalDate, Is.Not.SameAs(first.Matches[0].WithdrawalDate));
        });

        first.Match!.AlphabeticCode = "XXX";
        first.Matches[0].AlphabeticCode = "XTS";
        first.Matches[0].WithdrawalDate.Year = 1;

        CurrencyLookupResult second = store.FindCurrencyNumericLookup("032", CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(second.Match?.AlphabeticCode, Is.EqualTo("ARA"));
            Assert.That(second.Matches[0].AlphabeticCode, Is.EqualTo("ARA"));
            Assert.That(second.Matches[0].WithdrawalDate.Year, Is.EqualTo(1992));
        });
    }

    [Test]
    public void InvalidScopeIsRejectedBeforeNumericValidation()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.That(
            () => store.FindCurrencyNumericLookup("032", (CurrencySet)99),
            Throws.TypeOf<System.ArgumentException>());
    }
}
