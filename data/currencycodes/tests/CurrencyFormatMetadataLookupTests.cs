namespace currencycodes.Tests;

public class CurrencyFormatMetadataLookupTests
{
    [Test]
    public void LookupNormalizesCodeAndReturnsResolvedCashMetadata()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyFormatMetadata metadata = store.GetCurrencyFormatMetadataSnapshot(" \u2003chf\u00A0 ");

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
            Assert.That(metadata.SourceVersion, Is.EqualTo("48.2"));
        });
    }

    [Test]
    public void LookupReturnsResolvedCldrOverridesSeparatelyFromIsoDigits()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyFormatMetadata metadata = store.GetCurrencyFormatMetadataSnapshot("AFN");

        Assert.Multiple(() =>
        {
            Assert.That(metadata.HasIsoMinorUnit, Is.True);
            Assert.That(metadata.IsoMinorUnitDigits, Is.EqualTo(2));
            Assert.That(metadata.IsoMinorUnitFactor, Is.EqualTo(100m));
            Assert.That(metadata.HasStandardFormat, Is.True);
            Assert.That(metadata.StandardFormatDigits, Is.Zero);
            Assert.That(metadata.StandardFormatFactor, Is.EqualTo(1m));
            Assert.That(metadata.HasCash, Is.True);
            Assert.That(metadata.CashDigits, Is.Zero);
            Assert.That(metadata.CashFactor, Is.EqualTo(1m));
        });
    }

    [Test]
    public void LookupPreservesUnknownIsoFactsWhileReturningInheritedCldrRules()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyFormatMetadata metadata = store.GetCurrencyFormatMetadataSnapshot("AFA");

        Assert.Multiple(() =>
        {
            Assert.That(metadata.CurrencyCode, Is.EqualTo("AFA"));
            Assert.That(metadata.HasIsoMinorUnit, Is.False);
            Assert.That(metadata.IsoMinorUnitDigits, Is.Zero);
            Assert.That(metadata.IsoMinorUnitFactor, Is.Zero);
            Assert.That(metadata.HasStandardFormat, Is.True);
            Assert.That(metadata.StandardFormatDigits, Is.EqualTo(2));
            Assert.That(metadata.StandardRoundingIncrement, Is.Zero);
            Assert.That(metadata.StandardFormatFactor, Is.EqualTo(100m));
            Assert.That(metadata.HasCash, Is.True);
            Assert.That(metadata.CashDigits, Is.EqualTo(2));
            Assert.That(metadata.CashRoundingIncrement, Is.Zero);
            Assert.That(metadata.CashFactor, Is.EqualTo(100m));
        });
    }

    [Test]
    public void LookupSupportsResolvedFourDigitRules()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        CurrencyFormatMetadata metadata = store.GetCurrencyFormatMetadataSnapshot("UYW");

        Assert.Multiple(() =>
        {
            Assert.That(metadata.HasIsoMinorUnit, Is.True);
            Assert.That(metadata.IsoMinorUnitDigits, Is.EqualTo(4));
            Assert.That(metadata.IsoMinorUnitFactor, Is.EqualTo(10000m));
            Assert.That(metadata.StandardFormatDigits, Is.EqualTo(4));
            Assert.That(metadata.StandardFormatFactor, Is.EqualTo(10000m));
            Assert.That(metadata.CashDigits, Is.EqualTo(4));
            Assert.That(metadata.CashFactor, Is.EqualTo(10000m));
        });
    }

    [Test]
    public void LookupRejectsMalformedCodesWithTheDedicatedException()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => store.GetCurrencyFormatMetadataSnapshot(null),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code is malformed."));
            Assert.That(
                () => store.GetCurrencyFormatMetadataSnapshot("US1"),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code is malformed."));
            Assert.That(
                () => store.GetCurrencyFormatMetadataSnapshot("EURO"),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code is malformed."));
            Assert.That(
                () => store.GetCurrencyFormatMetadataSnapshot("\u0130DR"),
                Throws.TypeOf<InvalidCurrencyCodeException>()
                    .With.Message.EqualTo("Currency alphabetic code is malformed."));
        });
    }

    [Test]
    public void LookupReportsWellFormedUnassignedCodesAsNotFound()
    {
        CurrencyDataStore store = new CurrencyDataStore();

        Assert.That(
            () => store.GetCurrencyFormatMetadataSnapshot("aaa"),
            Throws.TypeOf<CurrencyNotFoundException>()
                .With.Message.EqualTo("Currency format metadata for 'AAA' was not found."));
    }

    [Test]
    public void LookupReturnsDetachedMetadataAcrossCalls()
    {
        CurrencyDataStore store = new CurrencyDataStore();
        CurrencyFormatMetadata first = store.GetCurrencyFormatMetadataSnapshot("CHF");

        first.CurrencyCode = "XXX";
        first.HasIsoMinorUnit = false;
        first.IsoMinorUnitDigits = 0;
        first.CashRoundingIncrement = 99;
        first.SourceVersion = "changed";

        CurrencyFormatMetadata second = store.GetCurrencyFormatMetadataSnapshot("CHF");

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.CurrencyCode, Is.EqualTo("CHF"));
            Assert.That(second.HasIsoMinorUnit, Is.True);
            Assert.That(second.IsoMinorUnitDigits, Is.EqualTo(2));
            Assert.That(second.CashRoundingIncrement, Is.EqualTo(5));
            Assert.That(second.SourceVersion, Is.EqualTo("48.2"));
        });
    }
}
