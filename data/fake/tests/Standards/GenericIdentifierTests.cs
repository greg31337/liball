using fake;

namespace fake.Tests;

[TestFixture]
public class GenericIdentifierTests
{
    [Test]
    public void BuiltInMasksProduceEveryGenericIdentifierShape()
    {
        IdentifierProvider identifiers = Fake.Create(5401).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.Sku(), Does.Match("^SKU-[A-Za-z0-9]{4}-[0-9]{4}$"));
            Assert.That(identifiers.Serial(), Does.Match("^SN-[A-Za-z0-9]{12}$"));
            Assert.That(identifiers.Lot(), Does.Match("^LOT-[0-9]{8}$"));
            Assert.That(identifiers.Order(), Does.Match("^ORD-[0-9]{10}$"));
            Assert.That(identifiers.Invoice(), Does.Match("^INV-[0-9]{8}$"));
            Assert.That(identifiers.Tracking(), Does.Match("^TRK-[A-Za-z0-9]{16}$"));
        });
    }

    [Test]
    public void BuiltInGenerationReplaysForTheSameSeed()
    {
        IdentifierProvider first = Fake.Create(5402).Identifier();
        IdentifierProvider second = Fake.Create(5402).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(first.Sku(), Is.EqualTo(second.Sku()));
            Assert.That(first.Serial(), Is.EqualTo(second.Serial()));
            Assert.That(first.Lot(), Is.EqualTo(second.Lot()));
            Assert.That(first.Order(), Is.EqualTo(second.Order()));
            Assert.That(first.Invoice(), Is.EqualTo(second.Invoice()));
            Assert.That(first.Tracking(), Is.EqualTo(second.Tracking()));
        });
    }

    [Test]
    public void ExplicitMasksCustomizeEveryIdentifierKind()
    {
        IdentifierProvider identifiers = Fake.Create(5403).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.SkuFromMask("S-##"), Does.Match("^S-[0-9]{2}$"));
            Assert.That(identifiers.SerialFromMask("R-????"), Does.Match("^R-[A-Za-z]{4}$"));
            Assert.That(identifiers.LotFromMask("L-****"), Does.Match("^L-[A-Za-z0-9]{4}$"));
            Assert.That(identifiers.OrderFromMask("O-#####"), Does.Match("^O-[0-9]{5}$"));
            Assert.That(identifiers.InvoiceFromMask("I-\\#-##"), Does.Match("^I-#-[0-9]{2}$"));
            Assert.That(identifiers.TrackingFromMask("T-********"), Does.Match("^T-[A-Za-z0-9]{8}$"));
        });
    }

    [Test]
    public void LocaleFieldsCustomizeAllIdentifierMasks()
    {
        Fake fake = Fake.Create("en-US", 5404L);
        fake.Locale().RegisterFromLocaleAndFields(
            "en-US",
            new Dictionary<string, object>
            {
                ["identifier.sku.masks"] = "US-SKU-####",
                ["identifier.serial.masks"] = "US-SN-????",
                ["identifier.lot.masks"] = "US-LOT-****",
                ["identifier.order.masks"] = "US-ORD-######",
                ["identifier.invoice.masks"] = "US-INV-#####",
                ["identifier.tracking.masks"] = "US-TRK-********"
            });

        IdentifierProvider identifiers = fake.Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.Sku(), Does.Match("^US-SKU-[0-9]{4}$"));
            Assert.That(identifiers.Serial(), Does.Match("^US-SN-[A-Za-z]{4}$"));
            Assert.That(identifiers.Lot(), Does.Match("^US-LOT-[A-Za-z0-9]{4}$"));
            Assert.That(identifiers.Order(), Does.Match("^US-ORD-[0-9]{6}$"));
            Assert.That(identifiers.Invoice(), Does.Match("^US-INV-[0-9]{5}$"));
            Assert.That(identifiers.Tracking(), Does.Match("^US-TRK-[A-Za-z0-9]{8}$"));
        });
    }

    [Test]
    public void IdentifierAndCommerceProvidersUseIndependentMaskFields()
    {
        Fake fake = Fake.Create("en-US", 5405L);
        fake.Locale().RegisterFromLocaleAndFields(
            "en-US",
            new Dictionary<string, object>
            {
                ["identifier.sku.masks"] = "ID-####",
                ["identifier.order.masks"] = "ID-ORD-####",
                ["commerce.sku.masks"] = "SHOP-????",
                ["commerce.order.masks"] = "SHOP-ORD-####",
                ["commerce.invoice.masks"] = "SHOP-INV-####",
                ["commerce.tracking.masks"] = "SHOP-TRK-********"
            });

        Assert.Multiple(() =>
        {
            Assert.That(fake.Identifier().Sku(), Does.Match("^ID-[0-9]{4}$"));
            Assert.That(fake.Identifier().Order(), Does.Match("^ID-ORD-[0-9]{4}$"));
            Assert.That(fake.Commerce().Sku(), Does.Match("^SHOP-[A-Za-z]{4}$"));
            Assert.That(fake.Commerce().Order(), Does.Match("^SHOP-ORD-[0-9]{4}$"));
            Assert.That(fake.Commerce().Invoice(), Does.Match("^SHOP-INV-[0-9]{4}$"));
            Assert.That(fake.Commerce().Tracking(), Does.Match("^SHOP-TRK-[A-Za-z0-9]{8}$"));
        });
    }

    [Test]
    public void LocaleParentFallbackAndMaskArraysAreDeterministic()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", 5406L);
        options.FallbackLocale = "en";
        Fake first = Fake.Create(options);
        Fake second = Fake.Create(options);
        string[] masks = new string[] { "PARENT-A-####", "PARENT-B-????", "PARENT-C-****" };
        first.Locale().RegisterFromLocaleAndFields("fr", new Dictionary<string, object> { ["identifier.sku.masks"] = masks });
        second.Locale().RegisterFromLocaleAndFields("fr", new Dictionary<string, object> { ["identifier.sku.masks"] = masks });

        int index = 0;
        while (index < 64)
        {
            string firstValue = first.Identifier().Sku();
            string secondValue = second.Identifier().Sku();
            Assert.That(firstValue, Is.EqualTo(secondValue));
            Assert.That(firstValue, Does.Match("^PARENT-[ABC]-[A-Za-z0-9]{4}$"));
            index++;
        }
    }

    [Test]
    public void RuntimeRegistrationTakesEffectAfterProviderCreation()
    {
        Fake fake = Fake.Create("en-US", 5407L);
        IdentifierProvider identifiers = fake.Identifier();

        Assert.That(identifiers.Lot(), Does.StartWith("LOT-"));

        fake.Locale().RegisterFromLocaleAndFields(
            "en-US",
            new Dictionary<string, object> { ["identifier.lot.masks"] = "CUSTOM-LOT-####" });

        Assert.That(identifiers.Lot(), Does.Match("^CUSTOM-LOT-[0-9]{4}$"));
    }

    [Test]
    public void ExplicitMasksOverrideResolvedLocaleMasks()
    {
        Fake fake = Fake.Create("en-US", 5408L);
        fake.Locale().RegisterFromLocaleAndFields(
            "en-US",
            new Dictionary<string, object> { ["commerce.invoice.masks"] = "LOCALE-####" });

        Assert.That(fake.Commerce().InvoiceFromMask("EXPLICIT-????"), Does.Match("^EXPLICIT-[A-Za-z]{4}$"));
    }

    [Test]
    public void ProviderRandomStreamsRemainIndependent()
    {
        Fake actual = Fake.Create(5409);
        Fake expected = Fake.Create(5409);

        actual.Identifier().Sku();
        actual.Identifier().Serial();
        actual.Identifier().Tracking();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Commerce().Sku(), Is.EqualTo(expected.Commerce().Sku()));
            Assert.That(actual.Commerce().Order(), Is.EqualTo(expected.Commerce().Order()));
        });
    }

    [Test]
    public void InvalidExplicitMasksAndNullsDoNotConsumeEntropy()
    {
        IdentifierProvider actual = Fake.Create(5410).Identifier();
        IdentifierProvider expected = Fake.Create(5410).Identifier();

        Assert.Throws<ArgumentNullException>(() => actual.SkuFromMask(null));
        Assert.Throws<ArgumentException>(() => actual.SerialFromMask("BAD\\q"));
        Assert.Throws<ArgumentException>(() => actual.LotFromMask("BAD\\"));

        Assert.That(actual.TrackingFromMask("T-********"), Is.EqualTo(expected.TrackingFromMask("T-********")));
    }

    [Test]
    public void InvalidLocaleMaskCollectionsFailBeforeConsumingEntropy()
    {
        Fake actual = Fake.Create("en-US", 5411L);
        Fake expected = Fake.Create("en-US", 5411L);
        actual.Locale().RegisterFromLocaleAndFields(
            "en-US",
            new Dictionary<string, object>
            {
                ["identifier.sku.masks"] = new string[] { "GOOD-####", "" }
            });

        FakeException exception = Assert.Throws<FakeException>(() => actual.Identifier().Sku());

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("identifier.sku.masks"));
            Assert.That(actual.Identifier().RandomValue.Int(), Is.EqualTo(expected.Identifier().RandomValue.Int()));
        });
    }

    [Test]
    public void LocaleMaskFieldMustBeAStringOrNonemptyStringArray()
    {
        Fake empty = Fake.Create("en-US", 5412L);
        empty.Locale().RegisterFromLocaleAndFields(
            "en-US",
            new Dictionary<string, object> { ["identifier.order.masks"] = new string[0] });
        Fake wrongType = Fake.Create("en-US", 5413L);
        wrongType.Locale().RegisterFromLocaleAndFields(
            "en-US",
            new Dictionary<string, object> { ["commerce.order.masks"] = 42 });

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<FakeException>(() => empty.Identifier().Order()).Message, Does.Contain("at least one mask"));
            Assert.That(Assert.Throws<FakeException>(() => wrongType.Commerce().Order()).Message, Does.Contain("string or a nonempty array"));
        });
    }

    [Test]
    public void OutputLimitsAreCheckedBeforeEntropyIsConsumed()
    {
        FakeOptions options = FakeOptions.Create(5414L);
        options.MaximumOutputSize = 8;
        IdentifierProvider actual = Fake.Create(options).Identifier();
        IdentifierProvider expected = Fake.Create(options).Identifier();

        Assert.Throws<ArgumentOutOfRangeException>(() => actual.Sku());
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.SerialFromMask("TOO-LONG-####"));

        Assert.That(actual.RandomValue.Int(), Is.EqualTo(expected.RandomValue.Int()));
    }

    [Test]
    public void StrictLocaleModeStillUsesGenericBuiltInFallbackMasks()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", 5415L);
        options.StrictnessMode = StrictnessMode.Strict;
        IdentifierProvider identifiers = Fake.Create(options).Identifier();

        Assert.That(identifiers.Invoice(), Does.Match("^INV-[0-9]{8}$"));
    }

    [Test]
    public void CryptographicModeProducesValidGenericShapes()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        Fake fake = Fake.Create(options);

        int index = 0;
        while (index < 128)
        {
            Assert.That(fake.Identifier().Sku(), Does.Match("^SKU-[A-Za-z0-9]{4}-[0-9]{4}$"));
            Assert.That(fake.Commerce().Tracking(), Does.Match("^TRK-[A-Za-z0-9]{16}$"));
            index++;
        }
    }
}
