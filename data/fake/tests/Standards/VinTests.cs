using fake;

namespace fake.Tests;

[TestFixture]
public class VinTests
{
    [Test]
    public void Part565ExampleUsesThePublishedWeightsAndTransliteration()
    {
        VinChecksum checksum = Fake.Create(5301).Checksum().Vin();
        string source = "1G4AH59H05G118341";

        Assert.Multiple(() =>
        {
            Assert.That(checksum.Calculate(source), Is.EqualTo(4));
            Assert.That(checksum.Apply(source), Is.EqualTo("1G4AH59H45G118341"));
            Assert.That(checksum.Validate("1G4AH59H45G118341"), Is.True);
            Assert.That(checksum.Validate(source), Is.False);
        });
    }

    [Test]
    public void RemainderTenUsesUppercaseXAsTheCheckDigit()
    {
        VinChecksum checksum = Fake.Create(5302).Checksum().Vin();

        Assert.Multiple(() =>
        {
            Assert.That(checksum.Calculate("1M8GDM9A0KP042788"), Is.EqualTo(10));
            Assert.That(checksum.Apply("1M8GDM9A0KP042788"), Is.EqualTo("1M8GDM9AXKP042788"));
            Assert.That(checksum.Validate("1M8GDM9AXKP042788"), Is.True);
        });
    }

    [Test]
    public void ChecksumProviderReturnsItsCachedVinEngine()
    {
        ChecksumProvider checksum = Fake.Create(5303).Checksum();
        VinChecksum vin = checksum.Vin();

        Assert.That(checksum.Vin(), Is.SameAs(vin));
    }

    [Test]
    public void ValidationRejectsForbiddenCharactersCaseLengthAndWrongCheckDigit()
    {
        IdentifierProvider identifiers = Fake.Create(5304).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.ValidateVin("1G4AH59H45G118341"), Is.True);
            Assert.That(identifiers.ValidateVin("1G4AH59I45G118341"), Is.False);
            Assert.That(identifiers.ValidateVin("1G4AH59O45G118341"), Is.False);
            Assert.That(identifiers.ValidateVin("1G4AH59Q45G118341"), Is.False);
            Assert.That(identifiers.ValidateVin("1g4AH59H45G118341"), Is.False);
            Assert.That(identifiers.ValidateVin("1G4AH59H45G11834"), Is.False);
            Assert.That(identifiers.ValidateVin("1G4AH59H55G118341"), Is.False);
        });
    }

    [Test]
    public void GeneratedVinsAreDeterministicAndStructurallyValid()
    {
        IdentifierProvider first = Fake.Create(5305).Identifier();
        IdentifierProvider second = Fake.Create(5305).Identifier();

        string firstValue = first.VinFromModelYear(2025);
        string nextValue = first.VinFromModelYear(2025);

        Assert.Multiple(() =>
        {
            Assert.That(firstValue, Is.EqualTo(second.VinFromModelYear(2025)));
            Assert.That(nextValue, Is.EqualTo(second.VinFromModelYear(2025)));
            Assert.That(firstValue, Does.Match("^[A-HJ-NPR-Z0-9]{17}$"));
            Assert.That(firstValue.Substring(11), Does.Match("^[0-9]{6}$"));
            Assert.That(first.ValidateVin(firstValue), Is.True);
            Assert.That(first.ValidateVin(nextValue), Is.True);
        });
    }

    [Test]
    public void DefaultGenerationUsesTheCapturedReferenceYear()
    {
        FakeOptions options = FakeOptions.Create(5306L);
        options.ReferenceTime = new DateTimeOffset(2034, 6, 7, 8, 9, 10, TimeSpan.FromHours(-7));
        IdentifierProvider identifiers = Fake.Create(options).Identifier();

        string value = identifiers.Vin();

        Assert.Multiple(() =>
        {
            Assert.That(value[9], Is.EqualTo('4'));
            Assert.That(identifiers.VinModelYear(value), Is.EqualTo(2034));
            Assert.That(identifiers.ValidateVin(value), Is.True);
        });
    }

    [TestCase(1980, 'A')]
    [TestCase(2000, 'Y')]
    [TestCase(2001, '1')]
    [TestCase(2009, '9')]
    [TestCase(2010, 'A')]
    [TestCase(2030, 'Y')]
    [TestCase(2031, '1')]
    [TestCase(2039, '9')]
    public void ModelYearCodesCoverBothThirtyYearCycles(int modelYear, char code)
    {
        IdentifierProvider identifiers = Fake.Create(5307).Identifier();

        string value = identifiers.VinFromModelYear(modelYear);

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.VinModelYearCode(modelYear), Is.EqualTo(code));
            Assert.That(value[9], Is.EqualTo(code));
            Assert.That(identifiers.VinModelYear(value), Is.EqualTo(modelYear));
            Assert.That(value[6] >= '0' && value[6] <= '9', Is.EqualTo(modelYear <= 2009));
        });
    }

    [Test]
    public void SuppliedWmiIsPreservedWithoutClaimingAllocation()
    {
        IdentifierProvider identifiers = Fake.Create(5308).Identifier();

        string value = identifiers.VinFromWmiAndModelYear("1G4", 2005);

        Assert.Multiple(() =>
        {
            Assert.That(value, Does.StartWith("1G4"));
            Assert.That(identifiers.VinWmi(value), Is.EqualTo("1G4"));
            Assert.That(identifiers.VinModelYear(value), Is.EqualTo(2005));
            Assert.That(identifiers.ValidateVin(value), Is.True);
        });
    }

    [Test]
    public void SpanGenerationWritesOnlyTheVinAndPreservesTheSuffix()
    {
        IdentifierProvider actual = Fake.Create(5309).Identifier();
        IdentifierProvider expected = Fake.Create(5309).Identifier();
        char[] generated = Enumerable.Repeat('#', 21).ToArray();
        char[] supplied = Enumerable.Repeat('#', 21).ToArray();

        int generatedCount = actual.WriteVinFromDestinationAndModelYear(generated, 2028);
        int suppliedCount = actual.WriteVinFromWmiAndDestinationAndModelYear("WVW".AsSpan(), supplied, 1999);

        Assert.Multiple(() =>
        {
            Assert.That(new string(generated, 0, generatedCount), Is.EqualTo(expected.VinFromModelYear(2028)));
            Assert.That(new string(supplied, 0, suppliedCount), Is.EqualTo(expected.VinFromWmiAndModelYear("WVW", 1999)));
            Assert.That(generated.Skip(generatedCount), Has.All.EqualTo('#'));
            Assert.That(supplied.Skip(suppliedCount), Has.All.EqualTo('#'));
        });
    }

    [Test]
    public void ChecksumSpanApplyWritesOnlyTheVinAndPreservesTheSuffix()
    {
        VinChecksum checksum = Fake.Create(5310).Checksum().Vin();
        char[] destination = Enumerable.Repeat('#', 21).ToArray();

        int written = checksum.ApplyFromValueAndDestination("1G4AH59H05G118341".AsSpan(), destination);

        Assert.Multiple(() =>
        {
            Assert.That(new string(destination, 0, written), Is.EqualTo("1G4AH59H45G118341"));
            Assert.That(destination.Skip(written), Has.All.EqualTo('#'));
        });
    }

    [Test]
    public void InvalidInputsAndSmallDestinationsDoNotConsumeEntropyOrMutateBuffers()
    {
        IdentifierProvider actual = Fake.Create(5311).Identifier();
        IdentifierProvider expected = Fake.Create(5311).Identifier();
        char[] destination = Enumerable.Repeat('#', 16).ToArray();

        Assert.Throws<ArgumentException>(() => actual.WriteVinFromDestinationAndModelYear(destination, 2025));
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.VinFromModelYear(1979));
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.VinFromModelYear(2040));
        Assert.Throws<ArgumentException>(() => actual.VinFromWmiAndModelYear("1I4", 2025));

        Assert.Multiple(() =>
        {
            Assert.That(destination, Has.All.EqualTo('#'));
            Assert.That(actual.VinFromModelYear(2025), Is.EqualTo(expected.VinFromModelYear(2025)));
        });
    }

    [Test]
    public void ConfiguredLimitsAreCheckedBeforeGenerationConsumesEntropy()
    {
        FakeOptions outputOptions = FakeOptions.Create(5312L);
        outputOptions.MaximumOutputSize = 16;
        IdentifierProvider outputActual = Fake.Create(outputOptions).Identifier();
        IdentifierProvider outputExpected = Fake.Create(outputOptions).Identifier();

        Assert.Throws<ArgumentOutOfRangeException>(() => outputActual.VinFromModelYear(2025));
        Assert.That(outputActual.RandomValue.Int(), Is.EqualTo(outputExpected.RandomValue.Int()));

        FakeOptions countOptions = FakeOptions.Create(5313L);
        countOptions.MaximumCount = 16;
        IdentifierProvider countActual = Fake.Create(countOptions).Identifier();
        IdentifierProvider countExpected = Fake.Create(countOptions).Identifier();

        Assert.Throws<ArgumentOutOfRangeException>(() => countActual.VinFromModelYear(2025));
        Assert.That(countActual.RandomValue.Int(), Is.EqualTo(countExpected.RandomValue.Int()));
    }

    [Test]
    public void InvalidTextInputsUseConsistentParameters()
    {
        IdentifierProvider identifiers = Fake.Create(5314).Identifier();
        VinChecksum checksum = Fake.Create(5314).Checksum().Vin();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.VinFromWmiAndModelYear((string)null, 2025)).ParamName, Is.EqualTo("wmi"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.ValidateVin((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.VinModelYear((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.VinWmi((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => checksum.Calculate((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.VinFromWmiAndModelYear("AB", 2025)).ParamName, Is.EqualTo("wmi"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.VinFromWmiAndModelYear("ab1", 2025)).ParamName, Is.EqualTo("wmi"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Calculate("1G4AH59I05G118341")).ParamName, Is.EqualTo("value"));
        });
    }

    [Test]
    public void CryptographicModeProducesValidVinShapes()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        IdentifierProvider identifiers = Fake.Create(options).Identifier();

        int index = 0;
        while (index < 128)
        {
            string value = identifiers.VinFromModelYear(2026);
            Assert.That(identifiers.ValidateVin(value), Is.True);
            Assert.That(identifiers.VinModelYear(value), Is.EqualTo(2026));
            index++;
        }
    }
}
