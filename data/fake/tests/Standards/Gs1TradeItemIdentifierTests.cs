using fake;

namespace fake.Tests;

[TestFixture]
public class Gs1TradeItemIdentifierTests
{
    [Test]
    public void VersionedStructuralTableContainsTheFourSupportedGtinLengths()
    {
        IdentifierProvider identifiers = Fake.Create(5101).Identifier();

        int[] first = identifiers.GtinLengths();
        first[0] = 99;
        int[] second = identifiers.GtinLengths();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.Gs1GeneralSpecificationsVersion(), Is.EqualTo("26.0"));
            Assert.That(second, Is.EqualTo(new int[] { 8, 12, 13, 14 }));
            Assert.That(second, Is.Not.SameAs(first));
        });
    }

    [TestCase("12345670")]
    [TestCase("036000291452")]
    [TestCase("6291041500213")]
    [TestCase("10012345000017")]
    public void ReferenceCheckDigitVectorsValidateThroughTheIdentifierApi(string value)
    {
        IdentifierProvider identifiers = Fake.Create(5102).Identifier();

        Assert.That(identifiers.ValidateGtin(value), Is.True);
    }

    [TestCase(8)]
    [TestCase(12)]
    [TestCase(13)]
    [TestCase(14)]
    public void EverySupportedGtinLengthGeneratesDeterministicallyWithAValidCheckDigit(int length)
    {
        IdentifierProvider first = Fake.Create(5103).Identifier();
        IdentifierProvider second = Fake.Create(5103).Identifier();
        StandardIdentifierChecksum checksum = Fake.Create(5104).Checksum().Gtin();

        string value = first.GtinFromLength(length);

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(second.GtinFromLength(length)));
            Assert.That(value, Has.Length.EqualTo(length));
            Assert.That(value, Does.Match("^[0-9]+$"));
            Assert.That(first.ValidateGtin(value), Is.True);
            Assert.That(checksum.Validate(value), Is.True);
        });
    }

    [Test]
    public void Ean8AndUpcAAreNamedGtin8AndGtin12Conveniences()
    {
        IdentifierProvider named = Fake.Create(5105).Identifier();
        IdentifierProvider generic = Fake.Create(5105).Identifier();

        string ean8 = named.Ean8();
        string upcA = named.UpcA();

        Assert.Multiple(() =>
        {
            Assert.That(ean8, Is.EqualTo(generic.GtinFromLength(8)));
            Assert.That(upcA, Is.EqualTo(generic.GtinFromLength(12)));
            Assert.That(named.ValidateEan8(ean8), Is.True);
            Assert.That(named.ValidateUpcA(upcA), Is.True);
        });
    }

    [Test]
    public void DefaultGtinUsesTheUniformFourteenDigitShape()
    {
        IdentifierProvider identifiers = Fake.Create(5106).Identifier();

        string value = identifiers.Gtin();

        Assert.Multiple(() =>
        {
            Assert.That(value, Has.Length.EqualTo(14));
            Assert.That(identifiers.ValidateGtin(value), Is.True);
        });
    }

    [Test]
    public void NamedValidatorsRequireTheirExactLengthsAndAsciiDigits()
    {
        IdentifierProvider identifiers = Fake.Create(5107).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.ValidateEan8("12345670"), Is.True);
            Assert.That(identifiers.ValidateEan8("036000291452"), Is.False);
            Assert.That(identifiers.ValidateUpcA("036000291452"), Is.True);
            Assert.That(identifiers.ValidateUpcA("12345670"), Is.False);
            Assert.That(identifiers.ValidateGtin("12345671"), Is.False);
            Assert.That(identifiers.ValidateGtin("１２３４５６７０"), Is.False);
            Assert.That(identifiers.ValidateGtin("1234-5670"), Is.False);
        });
    }

    [TestCase("12345670", "00000012345670")]
    [TestCase("036000291452", "00036000291452")]
    [TestCase("6291041500213", "06291041500213")]
    [TestCase("10012345000017", "10012345000017")]
    public void GtinStorageNormalizationLeftPadsToFourteenDigits(string value, string expected)
    {
        IdentifierProvider identifiers = Fake.Create(5108).Identifier();

        string normalized = identifiers.Gtin14(value);

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Is.EqualTo(expected));
            Assert.That(identifiers.ValidateGtin(normalized), Is.True);
        });
    }

    [Test]
    public void GenerationSpansWriteOnlyTheRequestedSlices()
    {
        IdentifierProvider actual = Fake.Create(5109).Identifier();
        IdentifierProvider expected = Fake.Create(5109).Identifier();
        char[] ean = Enumerable.Repeat('#', 12).ToArray();
        char[] upc = Enumerable.Repeat('#', 16).ToArray();
        char[] gtin = Enumerable.Repeat('#', 18).ToArray();

        int eanWritten = actual.WriteEan8(ean);
        int upcWritten = actual.WriteUpcA(upc);
        int gtinWritten = actual.WriteGtin(gtin);

        Assert.Multiple(() =>
        {
            Assert.That(new string(ean, 0, eanWritten), Is.EqualTo(expected.Ean8()));
            Assert.That(new string(upc, 0, upcWritten), Is.EqualTo(expected.UpcA()));
            Assert.That(new string(gtin, 0, gtinWritten), Is.EqualTo(expected.Gtin()));
            Assert.That(ean.Skip(eanWritten), Has.All.EqualTo('#'));
            Assert.That(upc.Skip(upcWritten), Has.All.EqualTo('#'));
            Assert.That(gtin.Skip(gtinWritten), Has.All.EqualTo('#'));
        });
    }

    [Test]
    public void NormalizationSpanPreservesItsDestinationSuffix()
    {
        IdentifierProvider identifiers = Fake.Create(5110).Identifier();
        char[] destination = Enumerable.Repeat('#', 18).ToArray();

        int written = identifiers.WriteGtin14("12345670".AsSpan(), destination);

        Assert.Multiple(() =>
        {
            Assert.That(new string(destination, 0, written), Is.EqualTo("00000012345670"));
            Assert.That(destination.Skip(written), Has.All.EqualTo('#'));
        });
    }

    [Test]
    public void TooSmallDestinationsFailWithoutModificationOrEntropyConsumption()
    {
        IdentifierProvider actual = Fake.Create(5111).Identifier();
        IdentifierProvider expected = Fake.Create(5111).Identifier();
        char[] ean = Enumerable.Repeat('#', 7).ToArray();
        char[] upc = Enumerable.Repeat('#', 11).ToArray();
        char[] gtin = Enumerable.Repeat('#', 13).ToArray();

        Assert.Throws<ArgumentException>(() => actual.WriteEan8(ean));
        Assert.Throws<ArgumentException>(() => actual.WriteUpcA(upc));
        Assert.Throws<ArgumentException>(() => actual.WriteGtin(gtin));

        Assert.Multiple(() =>
        {
            Assert.That(ean, Has.All.EqualTo('#'));
            Assert.That(upc, Has.All.EqualTo('#'));
            Assert.That(gtin, Has.All.EqualTo('#'));
            Assert.That(actual.GtinFromLength(8), Is.EqualTo(expected.GtinFromLength(8)));
        });
    }

    [Test]
    public void InvalidLengthsAndOutputLimitsFailBeforeEntropyConsumption()
    {
        FakeOptions options = FakeOptions.Create(5112L);
        options.MaximumOutputSize = 13;
        IdentifierProvider actual = Fake.Create(options).Identifier();
        IdentifierProvider expected = Fake.Create(options).Identifier();

        Assert.Throws<ArgumentOutOfRangeException>(() => actual.GtinFromLength(7));
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.GtinFromLength(9));
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.GtinFromLength(14));
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.Gtin14("12345670"));

        Assert.That(actual.GtinFromLength(13), Is.EqualTo(expected.GtinFromLength(13)));
    }

    [Test]
    public void CountLimitsFailBeforeEntropyConsumption()
    {
        FakeOptions options = FakeOptions.Create(5113L);
        options.MaximumCount = 10;
        IdentifierProvider actual = Fake.Create(options).Identifier();
        IdentifierProvider expected = Fake.Create(options).Identifier();

        Assert.Throws<ArgumentOutOfRangeException>(() => actual.UpcA());
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.Gtin());

        Assert.That(actual.Ean8(), Is.EqualTo(expected.Ean8()));
    }

    [Test]
    public void InvalidNormalizationInputsAndNullsUseConsistentParameters()
    {
        IdentifierProvider identifiers = Fake.Create(5114).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.ValidateGtin((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.ValidateEan8((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.ValidateUpcA((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.Gtin14((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.Gtin14("12345671")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.Gtin14("1234567")).ParamName, Is.EqualTo("value"));
        });
    }

    [Test]
    public void CryptographicModeProducesChecksumValidShapesAtEveryLength()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        IdentifierProvider identifiers = Fake.Create(options).Identifier();
        int[] lengths = identifiers.GtinLengths();

        int iteration = 0;
        while (iteration < 64)
        {
            int index = 0;
            while (index < lengths.Length)
            {
                string value = identifiers.GtinFromLength(lengths[index]);
                Assert.That(identifiers.ValidateGtin(value), Is.True);
                index++;
            }
            iteration++;
        }
    }
}
