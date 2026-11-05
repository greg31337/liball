using fake;

namespace fake.Tests;

[TestFixture]
public class ImeiTests
{
    [Test]
    public void Ts06LuhnExampleComposesFromTacAndSerialFields()
    {
        IdentifierProvider identifiers = Fake.Create(5201).Identifier();

        string value = identifiers.ImeiFromTacAndSerialNumber("49015420", "323751");

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo("490154203237518"));
            Assert.That(identifiers.ValidateImei(value), Is.True);
            Assert.That(identifiers.ImeiTac(value), Is.EqualTo("49015420"));
            Assert.That(identifiers.ImeiSerialNumber(value), Is.EqualTo("323751"));
            Assert.That(identifiers.GsmaTs06Version(), Is.EqualTo("28.1"));
        });
    }

    [Test]
    public void RandomImeiGenerationIsDeterministicAndLuhnValid()
    {
        IdentifierProvider first = Fake.Create(5202).Identifier();
        IdentifierProvider second = Fake.Create(5202).Identifier();
        LuhnChecksum luhn = Fake.Create(5203).Checksum().Luhn();

        string firstValue = first.Imei();
        string nextValue = first.Imei();

        Assert.Multiple(() =>
        {
            Assert.That(firstValue, Is.EqualTo(second.Imei()));
            Assert.That(nextValue, Is.EqualTo(second.Imei()));
            Assert.That(firstValue, Does.Match("^[0-9]{15}$"));
            Assert.That(luhn.Validate(firstValue), Is.True);
            Assert.That(luhn.Validate(nextValue), Is.True);
        });
    }

    [Test]
    public void SuppliedTacIsPreservedWhileSerialNumberIsGenerated()
    {
        IdentifierProvider first = Fake.Create(5204).Identifier();
        IdentifierProvider second = Fake.Create(5204).Identifier();

        string value = first.ImeiFromTac("12345678");

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(second.ImeiFromTac("12345678")));
            Assert.That(value, Does.StartWith("12345678"));
            Assert.That(first.ValidateImei(value), Is.True);
            Assert.That(first.ImeiSerialNumber(value), Does.Match("^[0-9]{6}$"));
        });
    }

    [Test]
    public void ImeiValidationRequiresExactAsciiStructureAndCheckDigit()
    {
        IdentifierProvider identifiers = Fake.Create(5205).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.ValidateImei("490154203237518"), Is.True);
            Assert.That(identifiers.ValidateImei("490154203237519"), Is.False);
            Assert.That(identifiers.ValidateImei("49015420323751"), Is.False);
            Assert.That(identifiers.ValidateImei("4901542032375180"), Is.False);
            Assert.That(identifiers.ValidateImei("４９０１５４２０３２３７５１８"), Is.False);
            Assert.That(identifiers.ValidateImei("49015420-323751"), Is.False);
        });
    }

    [Test]
    public void ImeiSvReplacesTheCheckDigitWithSoftwareVersion()
    {
        IdentifierProvider identifiers = Fake.Create(5206).Identifier();

        string imeiSv = identifiers.ImeiSvFromImeiAndSoftwareVersion("490154203237518", 0);

        Assert.Multiple(() =>
        {
            Assert.That(imeiSv, Is.EqualTo("4901542032375100"));
            Assert.That(identifiers.ValidateImeiSv(imeiSv), Is.True);
            Assert.That(identifiers.ImeiSvSoftwareVersion(imeiSv), Is.EqualTo(0));
            Assert.That(identifiers.ImeiFromImeiSv(imeiSv), Is.EqualTo("490154203237518"));
        });
    }

    [TestCase(0, "00")]
    [TestCase(9, "09")]
    [TestCase(10, "10")]
    [TestCase(98, "98")]
    public void ExplicitSoftwareVersionUsesTwoDigitsAcrossTheAllowedRange(int softwareVersion, string expectedSuffix)
    {
        IdentifierProvider identifiers = Fake.Create(5207).Identifier();

        string value = identifiers.ImeiSvFromTacAndSerialNumberAndSoftwareVersion("49015420", "323751", softwareVersion);

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo("49015420323751" + expectedSuffix));
            Assert.That(identifiers.ValidateImeiSv(value), Is.True);
            Assert.That(identifiers.ImeiSvSoftwareVersion(value), Is.EqualTo(softwareVersion));
        });
    }

    [Test]
    public void SoftwareVersionNinetyNineIsReservedAndRejected()
    {
        IdentifierProvider identifiers = Fake.Create(5208).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => identifiers.ImeiSvFromSoftwareVersion(-1)).ParamName, Is.EqualTo("softwareVersion"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => identifiers.ImeiSvFromSoftwareVersion(99)).ParamName, Is.EqualTo("softwareVersion"));
            Assert.That(identifiers.ValidateImeiSv("4901542032375198"), Is.True);
            Assert.That(identifiers.ValidateImeiSv("4901542032375199"), Is.False);
        });
    }

    [Test]
    public void ImeiSvValidationDoesNotTreatItsLastDigitAsLuhn()
    {
        IdentifierProvider identifiers = Fake.Create(5209).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.ValidateImeiSv("4901542032375101"), Is.True);
            Assert.That(identifiers.ValidateImei("490154203237510"), Is.False);
            Assert.That(identifiers.ValidateImeiSv("49015420323751A1"), Is.False);
            Assert.That(identifiers.ValidateImeiSv("490154203237510"), Is.False);
        });
    }

    [Test]
    public void RandomImeiSvGenerationIsDeterministicAndNeverUsesReservedVersion()
    {
        IdentifierProvider first = Fake.Create(5210).Identifier();
        IdentifierProvider second = Fake.Create(5210).Identifier();

        int index = 0;
        while (index < 512)
        {
            string value = first.ImeiSv();
            Assert.That(value, Is.EqualTo(second.ImeiSv()));
            Assert.That(first.ValidateImeiSv(value), Is.True);
            Assert.That(first.ImeiSvSoftwareVersion(value), Is.InRange(0, 98));
            index++;
        }
    }

    [Test]
    public void InternationalTestMarkerShapeUses0044TacPrefixAndValidLuhn()
    {
        IdentifierProvider identifiers = Fake.Create(5211).Identifier();

        string value = identifiers.InternationalTestImeiShape();

        Assert.Multiple(() =>
        {
            Assert.That(value, Does.Match("^0044[0-9]{11}$"));
            Assert.That(identifiers.ValidateImei(value), Is.True);
            Assert.That(identifiers.IsInternationalTestImeiShape(value), Is.True);
            Assert.That(identifiers.IsNorthAmericanTestImeiShape(value), Is.False);
        });
    }

    [Test]
    public void NorthAmericanTestMarkerShapeUses001Mmm00TacPatternAndValidLuhn()
    {
        IdentifierProvider identifiers = Fake.Create(5212).Identifier();

        string value = identifiers.NorthAmericanTestImeiShape();

        Assert.Multiple(() =>
        {
            Assert.That(value, Does.Match("^001[0-9]{3}00[0-9]{7}$"));
            Assert.That(identifiers.ValidateImei(value), Is.True);
            Assert.That(identifiers.IsNorthAmericanTestImeiShape(value), Is.True);
            Assert.That(identifiers.IsInternationalTestImeiShape(value), Is.False);
        });
    }

    [Test]
    public void MarkerDetectionStillRequiresAValidCheckDigit()
    {
        IdentifierProvider identifiers = Fake.Create(5213).Identifier();
        string international = identifiers.InternationalTestImeiShape();
        string northAmerican = identifiers.NorthAmericanTestImeiShape();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.IsInternationalTestImeiShape(ChangeLast(international)), Is.False);
            Assert.That(identifiers.IsNorthAmericanTestImeiShape(ChangeLast(northAmerican)), Is.False);
            Assert.That(identifiers.IsInternationalTestImeiShape("490154203237518"), Is.False);
            Assert.That(identifiers.IsNorthAmericanTestImeiShape("490154203237518"), Is.False);
        });
    }

    [Test]
    public void SpanGenerationWritesOnlyRequiredSlices()
    {
        IdentifierProvider actual = Fake.Create(5214).Identifier();
        IdentifierProvider expected = Fake.Create(5214).Identifier();
        char[] imei = Enumerable.Repeat('#', 19).ToArray();
        char[] imeiSv = Enumerable.Repeat('#', 20).ToArray();

        int imeiWritten = actual.WriteImei(imei);
        int imeiSvWritten = actual.WriteImeiSv(imeiSv);

        Assert.Multiple(() =>
        {
            Assert.That(new string(imei, 0, imeiWritten), Is.EqualTo(expected.Imei()));
            Assert.That(new string(imeiSv, 0, imeiSvWritten), Is.EqualTo(expected.ImeiSv()));
            Assert.That(imei.Skip(imeiWritten), Has.All.EqualTo('#'));
            Assert.That(imeiSv.Skip(imeiSvWritten), Has.All.EqualTo('#'));
        });
    }

    [Test]
    public void ComposedSpanUsesFieldsAndPreservesDestinationSuffix()
    {
        IdentifierProvider identifiers = Fake.Create(5215).Identifier();
        char[] destination = Enumerable.Repeat('#', 19).ToArray();

        int written = identifiers.WriteImeiFromTacAndSerialNumberAndDestination("49015420".AsSpan(), "323751".AsSpan(), destination);

        Assert.Multiple(() =>
        {
            Assert.That(new string(destination, 0, written), Is.EqualTo("490154203237518"));
            Assert.That(destination.Skip(written), Has.All.EqualTo('#'));
        });
    }

    [Test]
    public void TooSmallDestinationsAndInvalidVersionsDoNotConsumeEntropy()
    {
        IdentifierProvider actual = Fake.Create(5216).Identifier();
        IdentifierProvider expected = Fake.Create(5216).Identifier();
        char[] imei = Enumerable.Repeat('#', 14).ToArray();
        char[] imeiSv = Enumerable.Repeat('#', 15).ToArray();

        Assert.Throws<ArgumentException>(() => actual.WriteImei(imei));
        Assert.Throws<ArgumentException>(() => actual.WriteImeiSv(imeiSv));
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.ImeiSvFromSoftwareVersion(99));

        Assert.Multiple(() =>
        {
            Assert.That(imei, Has.All.EqualTo('#'));
            Assert.That(imeiSv, Has.All.EqualTo('#'));
            Assert.That(actual.Imei(), Is.EqualTo(expected.Imei()));
        });
    }

    [Test]
    public void OutputAndCountLimitsAreCheckedBeforeRandomDigits()
    {
        FakeOptions outputOptions = FakeOptions.Create(5217L);
        outputOptions.MaximumOutputSize = 15;
        IdentifierProvider outputActual = Fake.Create(outputOptions).Identifier();
        IdentifierProvider outputExpected = Fake.Create(outputOptions).Identifier();

        Assert.Throws<ArgumentOutOfRangeException>(() => outputActual.ImeiSv());
        Assert.That(outputActual.Imei(), Is.EqualTo(outputExpected.Imei()));

        FakeOptions countOptions = FakeOptions.Create(5218L);
        countOptions.MaximumCount = 14;
        IdentifierProvider countActual = Fake.Create(countOptions).Identifier();
        IdentifierProvider countExpected = Fake.Create(countOptions).Identifier();

        Assert.Throws<ArgumentOutOfRangeException>(() => countActual.ImeiSv());
        Assert.That(countActual.Imei(), Is.EqualTo(countExpected.Imei()));
    }

    [Test]
    public void InvalidFieldsAndNullValuesUseConsistentParameters()
    {
        IdentifierProvider identifiers = Fake.Create(5219).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.ImeiFromTac((string)null)).ParamName, Is.EqualTo("tac"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.ValidateImei((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.ImeiFromTac("1234567")).ParamName, Is.EqualTo("tac"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.ImeiFromTac("１２３４５６７８")).ParamName, Is.EqualTo("tac"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.ImeiFromTacAndSerialNumber("12345678", "12345A")).ParamName, Is.EqualTo("serialNumber"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.ImeiTac("490154203237519")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.ImeiFromImeiSv("4901542032375199")).ParamName, Is.EqualTo("value"));
        });
    }

    [Test]
    public void CryptographicModeProducesValidImeiAndImeiSvShapes()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        IdentifierProvider identifiers = Fake.Create(options).Identifier();

        int index = 0;
        while (index < 128)
        {
            Assert.That(identifiers.ValidateImei(identifiers.Imei()), Is.True);
            Assert.That(identifiers.ValidateImeiSv(identifiers.ImeiSv()), Is.True);
            index++;
        }
    }

    static string ChangeLast(string value)
    {
        char[] characters = value.ToCharArray();
        characters[characters.Length - 1] = characters[characters.Length - 1] == '0' ? '1' : '0';
        return new string(characters);
    }
}
