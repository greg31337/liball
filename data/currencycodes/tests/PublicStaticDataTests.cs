namespace currencycodes.Tests;

public class PublicStaticDataTests
{
    [Test]
    public void CodeConstantsMatchCanonicalLengthsAndDecimalScale()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CurrencyCodeConstants.CurrencyAlphabeticCodeLength, Is.EqualTo(3));
            Assert.That(CurrencyCodeConstants.CurrencyNumericCodeLength, Is.EqualTo(3));
            Assert.That(CurrencyCodeConstants.TerritoryAlpha2CodeLength, Is.EqualTo(2));
            Assert.That(CurrencyCodeConstants.TerritoryAlpha3CodeLength, Is.EqualTo(3));
            Assert.That(CurrencyCodeConstants.TerritoryNumericCodeLength, Is.EqualTo(3));
            Assert.That(CurrencyCodeConstants.MaximumFractionDigits, Is.EqualTo(28));
        });
    }

    [Test]
    public void DataVersionsMatchPinnedGeneratedSnapshot()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CurrencyDataVersions.IsoPublicationDate, Is.EqualTo("2026-01-01"));
            Assert.That(CurrencyDataVersions.CldrVersion, Is.EqualTo("48.2"));
            Assert.That(CurrencyDataVersions.GeneratorSchemaVersion, Is.EqualTo("1"));
            Assert.That(
                CurrencyDataVersions.SnapshotIdentifier,
                Is.EqualTo("sha256:45c22d81e49767588f1509e6028b964f988a2c83f813184c00994bae30204a92"));
        });
    }
}
