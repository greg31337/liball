using fake;

namespace fake.Tests;

[TestFixture]
public class UuidTests
{
    [Test]
    public void Rfc9562Version4VectorOverwritesOnlyVersionAndVariantBits()
    {
        byte[] value = Convert.FromHexString("919108F752D133205BACF847DB4148A8");

        UuidGenerator.ApplyVersion4(value);

        Assert.Multiple(() =>
        {
            Assert.That(Convert.ToHexString(value), Is.EqualTo("919108F752D143209BACF847DB4148A8"));
            Assert.That(Fake.Create(4501).Identifier().FormatUuid(value), Is.EqualTo("919108f7-52d1-4320-9bac-f847db4148a8"));
        });
    }

    [Test]
    public void DeterministicTextAndBinaryGenerationUseTheSameNetworkOrder()
    {
        IdentifierProvider textProvider = Fake.Create(4502).Identifier();
        IdentifierProvider binaryProvider = Fake.Create(4502).Identifier();

        string text = textProvider.Uuid4();
        byte[] bytes = binaryProvider.Uuid4Bytes();

        Assert.Multiple(() =>
        {
            Assert.That(binaryProvider.FormatUuid(bytes), Is.EqualTo(text));
            Assert.That(binaryProvider.ParseUuid(text), Is.EqualTo(bytes));
            Assert.That(binaryProvider.ValidateUuid4(text), Is.True);
            Assert.That(binaryProvider.ValidateUuid4FromValueUsingReadOnlySpanOfByte(bytes), Is.True);
        });
    }

    [Test]
    public void EveryGeneratedValueHasVersionFourAndRfcVariantBits()
    {
        IdentifierProvider identifiers = Fake.Create(4503).Identifier();

        int index = 0;
        while (index < 2048)
        {
            byte[] value = identifiers.Uuid4Bytes();
            Assert.That(value[6] & 0xf0, Is.EqualTo(0x40));
            Assert.That(value[8] & 0xc0, Is.EqualTo(0x80));
            Assert.That(identifiers.ValidateUuid4FromValueUsingReadOnlySpanOfByte(value), Is.True);
            index++;
        }
    }

    [Test]
    public void NilAndMaxValuesMatchRfc9562SpecialValues()
    {
        IdentifierProvider identifiers = Fake.Create(4504).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.UuidNil(), Is.EqualTo("00000000-0000-0000-0000-000000000000"));
            Assert.That(identifiers.UuidNilBytes(), Has.All.EqualTo(0));
            Assert.That(identifiers.UuidMax(), Is.EqualTo("ffffffff-ffff-ffff-ffff-ffffffffffff"));
            Assert.That(identifiers.UuidMaxBytes(), Has.All.EqualTo(byte.MaxValue));
            Assert.That(identifiers.ValidateUuid(identifiers.UuidNil()), Is.True);
            Assert.That(identifiers.ValidateUuid(identifiers.UuidMax()), Is.True);
            Assert.That(identifiers.ValidateUuid4(identifiers.UuidNil()), Is.False);
            Assert.That(identifiers.ValidateUuid4(identifiers.UuidMax()), Is.False);
        });
    }

    [Test]
    public void ParsingAcceptsEitherHexCaseAndFormattingUsesLowercase()
    {
        IdentifierProvider identifiers = Fake.Create(4505).Identifier();
        string uppercase = "919108F7-52D1-4320-9BAC-F847DB4148A8";
        byte[] expected = Convert.FromHexString("919108F752D143209BACF847DB4148A8");

        byte[] parsed = identifiers.ParseUuid(uppercase);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.EqualTo(expected));
            Assert.That(identifiers.FormatUuid(parsed), Is.EqualTo(uppercase.ToLowerInvariant()));
            Assert.That(identifiers.ValidateUuid(uppercase), Is.True);
            Assert.That(identifiers.ValidateUuid4(uppercase), Is.True);
        });
    }

    [TestCase("")]
    [TestCase("919108f752d143209bacf847db4148a8")]
    [TestCase("{919108f7-52d1-4320-9bac-f847db4148a8}")]
    [TestCase("919108f7_52d1-4320-9bac-f847db4148a8")]
    [TestCase("919108f7-52d1-4320-9bac-f847db4148a")]
    [TestCase("919108f7-52d1-4320-9bac-f847db4148ag")]
    [TestCase("919108f7-52d1-4320-9bac-f847db4148a80")]
    public void ParsingRejectsNoncanonicalOrMalformedText(string value)
    {
        IdentifierProvider identifiers = Fake.Create(4506).Identifier();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => identifiers.ParseUuid(value));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ParamName, Is.EqualTo("value"));
            Assert.That(identifiers.ValidateUuid(value), Is.False);
            Assert.That(identifiers.ValidateUuid4(value), Is.False);
        });
    }

    [Test]
    public void VersionFourValidationRejectsWrongVersionOrVariant()
    {
        IdentifierProvider identifiers = Fake.Create(4507).Identifier();
        string wrongVersion = "919108f7-52d1-7320-9bac-f847db4148a8";
        string wrongVariant = "919108f7-52d1-4320-7bac-f847db4148a8";
        byte[] wrongVersionBytes = identifiers.ParseUuid(wrongVersion);
        byte[] wrongVariantBytes = identifiers.ParseUuid(wrongVariant);

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.ValidateUuid(wrongVersion), Is.True);
            Assert.That(identifiers.ValidateUuid(wrongVariant), Is.True);
            Assert.That(identifiers.ValidateUuid4(wrongVersion), Is.False);
            Assert.That(identifiers.ValidateUuid4(wrongVariant), Is.False);
            Assert.That(identifiers.ValidateUuid4FromValueUsingReadOnlySpanOfByte(wrongVersionBytes), Is.False);
            Assert.That(identifiers.ValidateUuid4FromValueUsingReadOnlySpanOfByte(wrongVariantBytes), Is.False);
            Assert.That(identifiers.ValidateUuid4FromValueUsingReadOnlySpanOfByte(new byte[15]), Is.False);
        });
    }

    [Test]
    public void DestinationSpanOperationsWriteOnlyTheirRequiredSlices()
    {
        IdentifierProvider textProvider = Fake.Create(4508).Identifier();
        IdentifierProvider expectedTextProvider = Fake.Create(4508).Identifier();
        IdentifierProvider byteProvider = Fake.Create(4509).Identifier();
        IdentifierProvider expectedByteProvider = Fake.Create(4509).Identifier();
        char[] textDestination = Enumerable.Repeat('#', 40).ToArray();
        byte[] byteDestination = Enumerable.Repeat((byte)0xee, 20).ToArray();
        char[] formatDestination = Enumerable.Repeat('#', 40).ToArray();
        byte[] parseDestination = Enumerable.Repeat((byte)0xee, 20).ToArray();
        byte[] vector = Convert.FromHexString("919108F752D143209BACF847DB4148A8");

        int textWritten = textProvider.WriteUuid4(textDestination);
        int bytesWritten = byteProvider.WriteUuid4Bytes(byteDestination);
        int formatWritten = textProvider.WriteUuid(vector, formatDestination);
        int parseWritten = textProvider.ParseUuidFromValueAndDestination("919108f7-52d1-4320-9bac-f847db4148a8".AsSpan(), parseDestination);

        Assert.Multiple(() =>
        {
            Assert.That(textWritten, Is.EqualTo(36));
            Assert.That(new string(textDestination, 0, 36), Is.EqualTo(expectedTextProvider.Uuid4()));
            Assert.That(textDestination.Skip(36), Has.All.EqualTo('#'));
            Assert.That(bytesWritten, Is.EqualTo(16));
            Assert.That(byteDestination.Take(16), Is.EqualTo(expectedByteProvider.Uuid4Bytes()));
            Assert.That(byteDestination.Skip(16), Has.All.EqualTo(0xee));
            Assert.That(formatWritten, Is.EqualTo(36));
            Assert.That(new string(formatDestination, 0, 36), Is.EqualTo("919108f7-52d1-4320-9bac-f847db4148a8"));
            Assert.That(formatDestination.Skip(36), Has.All.EqualTo('#'));
            Assert.That(parseWritten, Is.EqualTo(16));
            Assert.That(parseDestination.Take(16), Is.EqualTo(vector));
            Assert.That(parseDestination.Skip(16), Has.All.EqualTo(0xee));
        });
    }

    [Test]
    public void TooSmallDestinationsFailWithoutModificationOrEntropyUse()
    {
        IdentifierProvider actual = Fake.Create(4510).Identifier();
        IdentifierProvider expectedPosition = Fake.Create(4510).Identifier();
        char[] textDestination = Enumerable.Repeat('#', 35).ToArray();
        byte[] byteDestination = Enumerable.Repeat((byte)0xee, 15).ToArray();

        ArgumentException textException = Assert.Throws<ArgumentException>(() => actual.WriteUuid4(textDestination));
        ArgumentException byteException = Assert.Throws<ArgumentException>(() => actual.WriteUuid4Bytes(byteDestination));
        ArgumentException formatException = Assert.Throws<ArgumentException>(() => actual.WriteUuid(new byte[16], textDestination));
        ArgumentException parseException = Assert.Throws<ArgumentException>(() => actual.ParseUuidFromValueAndDestination("919108f7-52d1-4320-9bac-f847db4148a8".AsSpan(), byteDestination));

        Assert.Multiple(() =>
        {
            Assert.That(textException.ParamName, Is.EqualTo("destination"));
            Assert.That(byteException.ParamName, Is.EqualTo("destination"));
            Assert.That(formatException.ParamName, Is.EqualTo("destination"));
            Assert.That(parseException.ParamName, Is.EqualTo("destination"));
            Assert.That(textDestination, Has.All.EqualTo('#'));
            Assert.That(byteDestination, Has.All.EqualTo(0xee));
            Assert.That(actual.Uuid4Bytes(), Is.EqualTo(expectedPosition.Uuid4Bytes()));
        });
    }

    [Test]
    public void InvalidArgumentsUseConsistentParameterNames()
    {
        IdentifierProvider identifiers = Fake.Create(4511).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.FormatUuid((byte[])null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.ParseUuid((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.ValidateUuid((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.ValidateUuid4((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.FormatUuid(new byte[15])).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => UuidGenerator.ApplyVersion4(new byte[17])).ParamName, Is.EqualTo("value"));
        });
    }

    [Test]
    public void OutputLimitsAreCheckedBeforeVersionFourEntropyIsConsumed()
    {
        FakeOptions options = FakeOptions.Create(4512L);
        options.MaximumOutputSize = 20;
        IdentifierProvider actual = Fake.Create(options).Identifier();
        IdentifierProvider expectedPosition = Fake.Create(options).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.Uuid4()).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.UuidNil()).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.UuidMax()).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => actual.FormatUuid(new byte[16])).ParamName, Is.EqualTo("value"));
        });

        Assert.That(actual.Uuid4Bytes(), Is.EqualTo(expectedPosition.Uuid4Bytes()));
    }

    [Test]
    public void CryptographicModeStillProducesStructurallyValidVersionFourValues()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        IdentifierProvider identifiers = Fake.Create(options).Identifier();

        int index = 0;
        while (index < 64)
        {
            Assert.That(identifiers.ValidateUuid4(identifiers.Uuid4()), Is.True);
            index++;
        }
    }

    [Test]
    public void ConstantsParsingFormattingAndValidationConsumeNoEntropy()
    {
        IdentifierProvider actual = Fake.Create(4513).Identifier();
        IdentifierProvider expectedPosition = Fake.Create(4513).Identifier();
        byte[] value = actual.ParseUuid("919108f7-52d1-4320-9bac-f847db4148a8");

        actual.UuidNil();
        actual.UuidNilBytes();
        actual.UuidMax();
        actual.UuidMaxBytes();
        actual.FormatUuid(value);
        actual.ValidateUuid("919108f7-52d1-4320-9bac-f847db4148a8");
        actual.ValidateUuid4FromValueUsingReadOnlySpanOfByte(value);

        Assert.That(actual.Uuid4Bytes(), Is.EqualTo(expectedPosition.Uuid4Bytes()));
    }
}
