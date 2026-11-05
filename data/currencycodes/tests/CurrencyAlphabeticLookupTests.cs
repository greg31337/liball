namespace currencycodes.Tests;

public class CurrencyAlphabeticLookupTests
{
    [Test]
    public void LookupTrimsAndUppercasesAValidAsciiCode()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult result = store.FindCurrencyAlphabeticLookup(" \t\u2003usd\u00A0\r\n", CurrencySet.ActiveOnly);

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.True);
            Assert.That(result.Error, Is.EqualTo(LookupError.None));
            Assert.That(result.NormalizedQuery, Is.EqualTo("USD"));
            Assert.That(result.Match, Is.Not.Null);
            Assert.That(result.Match?.AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(result.Matches, Has.Length.EqualTo(1));
            Assert.That(result.Matches[0].Status, Is.EqualTo(CurrencyStatus.Active));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("US")]
    [TestCase("USDD")]
    [TestCase("U1D")]
    [TestCase("U SD")]
    [TestCase("UŚD")]
    [TestCase("ＵＳＤ")]
    public void MalformedInputReturnsAStructuredFailure(string? code)
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult result = store.FindCurrencyAlphabeticLookup(code, CurrencySet.All);

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
    public void WellFormedUnassignedCodePreservesItsNormalizedQuery()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult result = store.FindCurrencyAlphabeticLookup("zzz", CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.False);
            Assert.That(result.Error, Is.EqualTo(LookupError.Unassigned));
            Assert.That(result.NormalizedQuery, Is.EqualTo("ZZZ"));
            Assert.That(result.Match, Is.Null);
            Assert.That(result.Matches, Is.Empty);
        });
    }

    [Test]
    public void ExistingCodeOutsideTheRequestedScopeIsDistinctFromUnassigned()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult historical = store.FindCurrencyAlphabeticLookup("BGN", CurrencySet.ActiveOnly);
        CurrencyLookupResult active = store.FindCurrencyAlphabeticLookup("USD", CurrencySet.HistoricalOnly);

        Assert.Multiple(() =>
        {
            Assert.That(historical.Found, Is.False);
            Assert.That(historical.Error, Is.EqualTo(LookupError.OutOfScope));
            Assert.That(historical.NormalizedQuery, Is.EqualTo("BGN"));
            Assert.That(historical.Match, Is.Null);
            Assert.That(historical.Matches, Is.Empty);
            Assert.That(active.Found, Is.False);
            Assert.That(active.Error, Is.EqualTo(LookupError.OutOfScope));
            Assert.That(active.NormalizedQuery, Is.EqualTo("USD"));
        });
    }

    [Test]
    public void ScopeFilteringPreservesEveryMatchingDesignationInCanonicalOrder()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyLookupResult active = store.FindCurrencyAlphabeticLookup("EUR", CurrencySet.ActiveOnly);
        CurrencyLookupResult historical = store.FindCurrencyAlphabeticLookup("EUR", CurrencySet.HistoricalOnly);
        CurrencyLookupResult all = store.FindCurrencyAlphabeticLookup("EUR", CurrencySet.All);

        Assert.Multiple(() =>
        {
            Assert.That(active.Error, Is.EqualTo(LookupError.None));
            Assert.That(active.Matches, Has.Length.EqualTo(1));
            Assert.That(active.Matches[0].Status, Is.EqualTo(CurrencyStatus.Active));
            Assert.That(historical.Error, Is.EqualTo(LookupError.None));
            Assert.That(historical.Matches, Has.Length.EqualTo(1));
            Assert.That(historical.Matches[0].Status, Is.EqualTo(CurrencyStatus.Historical));
            Assert.That(all.Found, Is.True);
            Assert.That(all.Error, Is.EqualTo(LookupError.Ambiguous));
            Assert.That(all.Matches, Has.Length.EqualTo(2));
            Assert.That(all.Matches[0].Status, Is.EqualTo(CurrencyStatus.Active));
            Assert.That(all.Matches[1].Status, Is.EqualTo(CurrencyStatus.Historical));
        });
    }

    [Test]
    public void LookupResultOwnsItsPrimaryMatchArrayAndNestedDates()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CurrencyLookupResult first = store.FindCurrencyAlphabeticLookup("USD", CurrencySet.ActiveOnly);

        Assert.Multiple(() =>
        {
            Assert.That(first.Match, Is.Not.Null);
            Assert.That(first.Match, Is.Not.SameAs(first.Matches[0]));
            Assert.That(first.Match?.IntroductionDate, Is.Not.SameAs(first.Matches[0].IntroductionDate));
        });

        first.Match!.AlphabeticCode = "XXX";
        first.Matches[0].AlphabeticCode = "XTS";
        first.Matches[0].IntroductionDate.Year = 1;

        CurrencyLookupResult second = store.FindCurrencyAlphabeticLookup("USD", CurrencySet.ActiveOnly);

        Assert.Multiple(() =>
        {
            Assert.That(second.Match?.AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(second.Matches[0].AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(second.Matches[0].IntroductionDate.Year, Is.EqualTo(1792));
        });
    }

    [Test]
    public void RequiredLookupReturnsOneDetachedMatchAndThrowsForEveryFailureCategory()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyInfo required = store.GetRequiredCurrencyAlphabeticSnapshot(" usd ", CurrencySet.ActiveOnly);

        Assert.Multiple(() =>
        {
            Assert.That(required.AlphabeticCode, Is.EqualTo("USD"));
            Assert.That(
                () => store.GetRequiredCurrencyAlphabeticSnapshot("US$", CurrencySet.All),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code is malformed."));
            Assert.That(
                () => store.GetRequiredCurrencyAlphabeticSnapshot("ZZZ", CurrencySet.All),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code 'ZZZ' is not assigned."));
            Assert.That(
                () => store.GetRequiredCurrencyAlphabeticSnapshot("BGN", CurrencySet.ActiveOnly),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code 'BGN' is outside the requested scope."));
            Assert.That(
                () => store.GetRequiredCurrencyAlphabeticSnapshot("EUR", CurrencySet.All),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo(
                        "Currency alphabetic code 'EUR' identifies more than one designation in the requested scope."));
        });
    }

    [Test]
    public void InvalidScopeIsRejectedBeforeLookup()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => store.FindCurrencyAlphabeticLookup("USD", (CurrencySet)99),
                Throws.TypeOf<System.ArgumentException>());
            Assert.That(
                () => store.GetRequiredCurrencyAlphabeticSnapshot("USD", (CurrencySet)99),
                Throws.TypeOf<System.ArgumentException>());
        });
    }
}
