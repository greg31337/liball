namespace currencycodes.Tests;

public class CurrencyFormatMetadataTests
{
    [Test]
    public void DefaultStateKeepsEveryRuleFamilyUnavailable()
    {
        CurrencyFormatMetadata metadata = new CurrencyFormatMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(metadata.CurrencyCode, Is.Empty);
            Assert.That(metadata.HasIsoMinorUnit, Is.False);
            Assert.That(metadata.IsoMinorUnitDigits, Is.Zero);
            Assert.That(metadata.IsoMinorUnitFactor, Is.Zero);
            Assert.That(metadata.HasStandardFormat, Is.False);
            Assert.That(metadata.StandardFormatDigits, Is.Zero);
            Assert.That(metadata.StandardRoundingIncrement, Is.Zero);
            Assert.That(metadata.StandardFormatFactor, Is.Zero);
            Assert.That(metadata.HasCash, Is.False);
            Assert.That(metadata.CashDigits, Is.Zero);
            Assert.That(metadata.CashRoundingIncrement, Is.Zero);
            Assert.That(metadata.CashFactor, Is.Zero);
            Assert.That(metadata.SourceVersion, Is.Empty);
        });
    }

    [Test]
    public void FieldsKeepIsoStandardAndCashRulesSeparate()
    {
        CurrencyFormatMetadata metadata = new CurrencyFormatMetadata
        {
            CurrencyCode = "CHF",
            HasIsoMinorUnit = true,
            IsoMinorUnitDigits = 2,
            IsoMinorUnitFactor = 100m,
            HasStandardFormat = true,
            StandardFormatDigits = 2,
            StandardRoundingIncrement = 0,
            StandardFormatFactor = 100m,
            HasCash = true,
            CashDigits = 2,
            CashRoundingIncrement = 5,
            CashFactor = 100m,
            SourceVersion = "CLDR 48.2"
        };

        Assert.Multiple(() =>
        {
            Assert.That(metadata.CurrencyCode, Is.EqualTo("CHF"));
            Assert.That(metadata.HasIsoMinorUnit, Is.True);
            Assert.That(metadata.IsoMinorUnitDigits, Is.EqualTo(2));
            Assert.That(metadata.IsoMinorUnitFactor, Is.EqualTo(100m));
            Assert.That(metadata.HasStandardFormat, Is.True);
            Assert.That(metadata.StandardFormatDigits, Is.EqualTo(2));
            Assert.That(metadata.StandardRoundingIncrement, Is.Zero);
            Assert.That(metadata.StandardFormatFactor, Is.EqualTo(100m));
            Assert.That(metadata.HasCash, Is.True);
            Assert.That(metadata.CashDigits, Is.EqualTo(2));
            Assert.That(metadata.CashRoundingIncrement, Is.EqualTo(5));
            Assert.That(metadata.CashFactor, Is.EqualTo(100m));
            Assert.That(metadata.SourceVersion, Is.EqualTo("CLDR 48.2"));
        });
    }

    [Test]
    public void DecimalFactorSupportsMaximumPlannedFractionScale()
    {
        CurrencyFormatMetadata metadata = new CurrencyFormatMetadata
        {
            HasStandardFormat = true,
            StandardFormatDigits = 28,
            StandardFormatFactor = 10000000000000000000000000000m
        };

        Assert.That(
            metadata.StandardFormatFactor,
            Is.EqualTo(10000000000000000000000000000m));
    }
}
