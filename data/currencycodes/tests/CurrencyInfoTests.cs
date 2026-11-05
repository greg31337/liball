namespace currencycodes.Tests;

public class CurrencyInfoTests
{
    [Test]
    public void DefaultStateKeepsOptionalFactsAbsent()
    {
        CurrencyInfo currency = new CurrencyInfo();

        Assert.Multiple(() =>
        {
            Assert.That(currency.AlphabeticCode, Is.Empty);
            Assert.That(currency.HasNumericCode, Is.False);
            Assert.That(currency.NumericCode, Is.Empty);
            Assert.That(currency.NumericCodeValue, Is.Zero);
            Assert.That(currency.OfficialName, Is.Empty);
            Assert.That(currency.HasIsoMinorUnit, Is.False);
            Assert.That(currency.IsoMinorUnitDigits, Is.Zero);
            Assert.That(currency.Status, Is.EqualTo(CurrencyStatus.Active));
            Assert.That(currency.Kind, Is.EqualTo(CurrencyKind.Unknown));
            Assert.That(currency.IdentityFromIso, Is.False);
            Assert.That(currency.HistoricalStatusFromIso, Is.False);
            Assert.That(currency.FundClassificationFromIso, Is.False);
            Assert.That(currency.EnrichedFromCldr, Is.False);
            Assert.That(currency.IntroductionDate, Is.Not.Null);
            Assert.That(currency.IntroductionDate.IsKnown, Is.False);
            Assert.That(currency.WithdrawalDate, Is.Not.Null);
            Assert.That(currency.WithdrawalDate.IsKnown, Is.False);
        });
    }

    [Test]
    public void FieldsPreserveCompleteCurrencyIdentityAndProvenance()
    {
        PartialDate introductionDate = new PartialDate
        {
            IsKnown = true,
            Year = 1985,
            Month = 1,
            Precision = PartialDatePrecision.Month
        };
        PartialDate withdrawalDate = new PartialDate
        {
            IsKnown = true,
            Year = 2002,
            Month = 2,
            Day = 28,
            Precision = PartialDatePrecision.Day
        };
        CurrencyInfo currency = new CurrencyInfo
        {
            AlphabeticCode = "BEF",
            HasNumericCode = true,
            NumericCode = "056",
            NumericCodeValue = 56,
            OfficialName = "Belgian Franc",
            HasIsoMinorUnit = true,
            IsoMinorUnitDigits = 2,
            Status = CurrencyStatus.Historical,
            Kind = CurrencyKind.MonetaryCurrency,
            IdentityFromIso = true,
            HistoricalStatusFromIso = true,
            FundClassificationFromIso = false,
            EnrichedFromCldr = true,
            IntroductionDate = introductionDate,
            WithdrawalDate = withdrawalDate
        };

        Assert.Multiple(() =>
        {
            Assert.That(currency.AlphabeticCode, Is.EqualTo("BEF"));
            Assert.That(currency.HasNumericCode, Is.True);
            Assert.That(currency.NumericCode, Is.EqualTo("056"));
            Assert.That(currency.NumericCodeValue, Is.EqualTo(56));
            Assert.That(currency.OfficialName, Is.EqualTo("Belgian Franc"));
            Assert.That(currency.HasIsoMinorUnit, Is.True);
            Assert.That(currency.IsoMinorUnitDigits, Is.EqualTo(2));
            Assert.That(currency.Status, Is.EqualTo(CurrencyStatus.Historical));
            Assert.That(currency.Kind, Is.EqualTo(CurrencyKind.MonetaryCurrency));
            Assert.That(currency.IdentityFromIso, Is.True);
            Assert.That(currency.HistoricalStatusFromIso, Is.True);
            Assert.That(currency.FundClassificationFromIso, Is.False);
            Assert.That(currency.EnrichedFromCldr, Is.True);
            Assert.That(currency.IntroductionDate, Is.SameAs(introductionDate));
            Assert.That(currency.WithdrawalDate, Is.SameAs(withdrawalDate));
        });
    }
}
