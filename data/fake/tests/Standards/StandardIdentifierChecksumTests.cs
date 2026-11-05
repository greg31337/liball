using fake;

namespace fake.Tests;

[TestFixture]
public class StandardIdentifierChecksumTests
{
    [Test]
    public void ProviderReturnsCachedFormatEngines()
    {
        ChecksumProvider checksum = Fake.Create(4401).Checksum();
        StandardIdentifierChecksum isbn10 = checksum.Isbn10();
        StandardIdentifierChecksum isbn13 = checksum.Isbn13();
        StandardIdentifierChecksum issn = checksum.Issn();
        StandardIdentifierChecksum ean8 = checksum.Ean8();
        StandardIdentifierChecksum upcA = checksum.UpcA();
        StandardIdentifierChecksum gtin = checksum.Gtin();

        Assert.Multiple(() =>
        {
            Assert.That(checksum.Isbn10(), Is.SameAs(isbn10));
            Assert.That(checksum.Isbn13(), Is.SameAs(isbn13));
            Assert.That(checksum.Issn(), Is.SameAs(issn));
            Assert.That(checksum.Ean8(), Is.SameAs(ean8));
            Assert.That(checksum.UpcA(), Is.SameAs(upcA));
            Assert.That(checksum.Gtin(), Is.SameAs(gtin));
        });
    }

    [TestCase("030640615", 2, "0306406152")]
    [TestCase("097522980", 10, "097522980X")]
    public void Isbn10MatchesDigitAndXReferenceVectors(string payload, int check, string complete)
    {
        StandardIdentifierChecksum isbn = Fake.Create(4402).Checksum().Isbn10();

        Assert.Multiple(() =>
        {
            Assert.That(isbn.Calculate(payload), Is.EqualTo(check));
            Assert.That(isbn.Append(payload), Is.EqualTo(complete));
            Assert.That(isbn.Validate(complete), Is.True);
            Assert.That(isbn.Validate(ChangeLast(complete)), Is.False);
        });
    }

    [TestCase("978929505512", 4, "9789295055124")]
    [TestCase("978030640615", 7, "9780306406157")]
    public void Isbn13MatchesAgencyAndConversionExamples(string payload, int check, string complete)
    {
        StandardIdentifierChecksum isbn = Fake.Create(4403).Checksum().Isbn13();

        Assert.Multiple(() =>
        {
            Assert.That(isbn.Calculate(payload), Is.EqualTo(check));
            Assert.That(isbn.Append(payload), Is.EqualTo(complete));
            Assert.That(isbn.Validate(complete), Is.True);
            Assert.That(isbn.Validate(ChangeLast(complete)), Is.False);
        });
    }

    [TestCase("0317847", 1, "03178471")]
    [TestCase("2434561", 10, "2434561X")]
    public void IssnMatchesInternationalCentreVectors(string payload, int check, string complete)
    {
        StandardIdentifierChecksum issn = Fake.Create(4404).Checksum().Issn();

        Assert.Multiple(() =>
        {
            Assert.That(issn.Calculate(payload), Is.EqualTo(check));
            Assert.That(issn.Append(payload), Is.EqualTo(complete));
            Assert.That(issn.Validate(complete), Is.True);
            Assert.That(issn.Validate(ChangeLast(complete)), Is.False);
        });
    }

    [Test]
    public void Ean8AndUpcAMatchCommonExamples()
    {
        ChecksumProvider checksum = Fake.Create(4405).Checksum();

        Assert.Multiple(() =>
        {
            Assert.That(checksum.Ean8().Calculate("9638507"), Is.EqualTo(4));
            Assert.That(checksum.Ean8().Append("9638507"), Is.EqualTo("96385074"));
            Assert.That(checksum.Ean8().Validate("96385074"), Is.True);
            Assert.That(checksum.UpcA().Calculate("03600029145"), Is.EqualTo(2));
            Assert.That(checksum.UpcA().Append("03600029145"), Is.EqualTo("036000291452"));
            Assert.That(checksum.UpcA().Validate("036000291452"), Is.True);
        });
    }

    [TestCase("1234567", "12345670")]
    [TestCase("03600029145", "036000291452")]
    [TestCase("629104150021", "6291041500213")]
    [TestCase("1001234500001", "10012345000017")]
    public void GtinSupportsAllFourStandardLengths(string payload, string complete)
    {
        StandardIdentifierChecksum gtin = Fake.Create(4406).Checksum().Gtin();

        Assert.Multiple(() =>
        {
            Assert.That(gtin.Append(payload), Is.EqualTo(complete));
            Assert.That(gtin.Validate(complete), Is.True);
            Assert.That(gtin.Validate(ChangeLast(complete)), Is.False);
        });
    }

    [Test]
    public void Isbn13RequiresCurrentBooklandPrefixes()
    {
        StandardIdentifierChecksum isbn = Fake.Create(4407).Checksum().Isbn13();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentException>(() => isbn.Calculate("977030640615")).ParamName, Is.EqualTo("value"));
            Assert.That(isbn.Validate("9770306406158"), Is.False);
            Assert.That(isbn.Append("979123456789"), Does.StartWith("979123456789"));
        });
    }

    [Test]
    public void CompactSyntaxAndExactLengthsAreEnforced()
    {
        ChecksumProvider checksum = Fake.Create(4408).Checksum();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Isbn10().Calculate("30640615")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Issn().Calculate("0317-847")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Ean8().Calculate("１２３４５６７")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.UpcA().Calculate("0360002914A")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => checksum.Gtin().Calculate("12345678")).ParamName, Is.EqualTo("value"));
            Assert.That(checksum.Isbn10().Validate("0-306-40615-2"), Is.False);
            Assert.That(checksum.Issn().Validate("2434561x"), Is.False);
            Assert.That(checksum.Ean8().Validate("9638507"), Is.False);
            Assert.That(checksum.UpcA().Validate("0360002914520"), Is.False);
            Assert.That(checksum.Gtin().Validate("123456789"), Is.False);
        });
    }

    [Test]
    public void SpanAppendAndValidationLeaveDestinationSuffixUntouched()
    {
        ChecksumProvider checksum = Fake.Create(4409).Checksum();
        char[] source = "!978929505512?".ToCharArray();
        char[] destination = Enumerable.Repeat('#', 16).ToArray();

        int written = checksum.Isbn13().AppendFromValueAndDestination(
            source.AsSpan(1, 12),
            destination.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(13));
            Assert.That(new string(destination), Is.EqualTo("9789295055124###"));
            Assert.That(checksum.Isbn13().CalculateFromValue(source.AsSpan(1, 12)), Is.EqualTo(4));
            Assert.That(checksum.Isbn13().ValidateFromValue(destination.AsSpan(0, written)), Is.True);
        });
    }

    [Test]
    public void TooSmallDestinationFailsWithoutModification()
    {
        StandardIdentifierChecksum isbn = Fake.Create(4410).Checksum().Isbn10();
        char[] destination = Enumerable.Repeat('#', 9).ToArray();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => isbn.AppendFromValueAndDestination("030640615".AsSpan(), destination.AsSpan()));

        Assert.That(exception.ParamName, Is.EqualTo("destination"));
        Assert.That(destination, Has.All.EqualTo('#'));
    }

    [Test]
    public void NullAndConfiguredLimitsAreEnforced()
    {
        FakeOptions options = FakeOptions.Create(4411L);
        options.MaximumCount = 12;
        options.MaximumOutputSize = 13;
        ChecksumProvider checksum = Fake.Create(options).Checksum();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => checksum.Isbn10().Calculate((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => checksum.Gtin().Validate((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(checksum.Isbn13().Append("978929505512"), Is.EqualTo("9789295055124"));
            Assert.That(checksum.Isbn13().Validate("9789295055124"), Is.True);
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => checksum.Gtin().Calculate("1001234500001")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => checksum.Gtin().Append("1001234500001")).ParamName, Is.EqualTo("value"));
        });
    }

    [Test]
    public void StandardIdentifierChecksConsumeNoRandomEntropy()
    {
        ChecksumProvider actual = Fake.Create(4412).Checksum();
        ChecksumProvider expectedPosition = Fake.Create(4412).Checksum();

        actual.Isbn10().Append("030640615");
        actual.Isbn13().Validate("9789295055124");
        actual.Issn().Calculate("0317847");
        actual.Ean8().Validate("96385074");
        actual.UpcA().Append("03600029145");
        actual.Gtin().Validate("10012345000017");

        Assert.That(actual.RandomValue.Int(), Is.EqualTo(expectedPosition.RandomValue.Int()));
    }

    static string ChangeLast(string value)
    {
        char[] characters = value.ToCharArray();
        char last = characters[characters.Length - 1];
        characters[characters.Length - 1] = last == '0' ? '1' : '0';
        return new string(characters);
    }
}
