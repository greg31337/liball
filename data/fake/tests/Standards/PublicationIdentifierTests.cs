using fake;

namespace fake.Tests;

[TestFixture]
public class PublicationIdentifierTests
{
    [Test]
    public void DefaultDoiUsesTheDocumentedRestrictedShapeAndIsDeterministic()
    {
        IdentifierProvider first = Fake.Create(5001).Identifier();
        IdentifierProvider second = Fake.Create(5001).Identifier();

        string value = first.Doi();

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(second.Doi()));
            Assert.That(value, Does.Match("^10[.][0-9]{4}/[0-9a-z]{8}$"));
            Assert.That(first.ValidateDoi(value), Is.True);
            Assert.That(value, Has.Length.EqualTo(16));
        });
    }

    [Test]
    public void ConfiguredDoiLengthsControlRegistrantAndSuffixSegments()
    {
        IdentifierProvider identifiers = Fake.Create(5002).Identifier();

        string value = identifiers.DoiFromRegistrantDigitsAndSuffixLength(7, 19);

        Assert.Multiple(() =>
        {
            Assert.That(value, Does.Match("^10[.][0-9]{7}/[0-9a-z]{19}$"));
            Assert.That(value, Has.Length.EqualTo(30));
            Assert.That(identifiers.ValidateDoi(value), Is.True);
        });
    }

    [TestCase("10.1000/292", true)]
    [TestCase("10.5594/SMPTE.ST2067-21.2020", true)]
    [TestCase("10.1234/a_b-c.9", true)]
    [TestCase("11.1000/292", false)]
    [TestCase("10./292", false)]
    [TestCase("10.1000/", false)]
    [TestCase("10.1000/a/b", false)]
    [TestCase("10.1000/white space", false)]
    public void DoiValidationCoversTheRestrictedAsciiContract(string value, bool expected)
    {
        IdentifierProvider identifiers = Fake.Create(5003).Identifier();

        Assert.That(identifiers.ValidateDoi(value), Is.EqualTo(expected));
    }

    [Test]
    public void DoiUrlUsesTheCurrentHttpsResolverFormWithoutChangingTheName()
    {
        IdentifierProvider identifiers = Fake.Create(5004).Identifier();
        string value = "10.5594/SMPTE.ST2067-21.2020";

        string url = identifiers.DoiUrl(value);

        Assert.That(url, Is.EqualTo("https://doi.org/10.5594/SMPTE.ST2067-21.2020"));
    }

    [Test]
    public void DoiSpanOperationsPreserveSuffixesAndFailAtomically()
    {
        IdentifierProvider actual = Fake.Create(5005).Identifier();
        IdentifierProvider expected = Fake.Create(5005).Identifier();
        char[] tooSmall = Enumerable.Repeat('#', 15).ToArray();

        Assert.Throws<ArgumentException>(() => actual.WriteDoi(tooSmall));
        Assert.That(tooSmall, Has.All.EqualTo('#'));

        char[] value = Enumerable.Repeat('#', 20).ToArray();
        int written = actual.WriteDoi(value);
        char[] url = Enumerable.Repeat('#', 60).ToArray();
        int urlWritten = actual.WriteDoiUrl(value.AsSpan(0, written), url);

        Assert.Multiple(() =>
        {
            Assert.That(new string(value, 0, written), Is.EqualTo(expected.Doi()));
            Assert.That(value.Skip(written), Has.All.EqualTo('#'));
            Assert.That(new string(url, 0, urlWritten), Is.EqualTo("https://doi.org/" + new string(value, 0, written)));
            Assert.That(url.Skip(urlWritten), Has.All.EqualTo('#'));
        });
    }

    [Test]
    public void OfficialChecksumExamplesHaveAllBookIdentifierDisplayVariants()
    {
        IdentifierProvider identifiers = Fake.Create(5006).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.FormatIsbn10("0306406152", BookIdentifierStyle.Compact), Is.EqualTo("0306406152"));
            Assert.That(identifiers.FormatIsbn10("0306406152", BookIdentifierStyle.Hyphenated), Is.EqualTo("0-306-40615-2"));
            Assert.That(identifiers.FormatIsbn10("0306406152", BookIdentifierStyle.Spaced), Is.EqualTo("0 306 40615 2"));
            Assert.That(identifiers.FormatIsbn10("0306406152", BookIdentifierStyle.Labeled), Is.EqualTo("ISBN 0-306-40615-2"));
            Assert.That(identifiers.FormatIsbn13("9780306406157", BookIdentifierStyle.Hyphenated), Is.EqualTo("978-0-306-40615-7"));
            Assert.That(identifiers.FormatIsbn13("9780306406157", BookIdentifierStyle.Labeled), Is.EqualTo("ISBN 978-0-306-40615-7"));
            Assert.That(identifiers.FormatIssn("03178471", BookIdentifierStyle.Hyphenated), Is.EqualTo("0317-8471"));
            Assert.That(identifiers.FormatIssn("03178471", BookIdentifierStyle.Labeled), Is.EqualTo("ISSN 0317-8471"));
        });
    }

    [Test]
    public void XCheckCharactersArePreservedAcrossDisplayVariants()
    {
        IdentifierProvider identifiers = Fake.Create(5007).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(identifiers.FormatIsbn10("097522980X", BookIdentifierStyle.Hyphenated), Is.EqualTo("0-975-22980-X"));
            Assert.That(identifiers.FormatIssn("2434561X", BookIdentifierStyle.Labeled), Is.EqualTo("ISSN 2434-561X"));
        });
    }

    [Test]
    public void GeneratedCompactPublicationIdentifiersAreChecksumValidAndDeterministic()
    {
        IdentifierProvider first = Fake.Create(5008).Identifier();
        IdentifierProvider second = Fake.Create(5008).Identifier();
        ChecksumProvider checks = Fake.Create(5009).Checksum();

        string isbn10 = first.Isbn10();
        string isbn13 = first.Isbn13();
        string issn = first.Issn();

        Assert.Multiple(() =>
        {
            Assert.That(isbn10, Is.EqualTo(second.Isbn10()));
            Assert.That(isbn13, Is.EqualTo(second.Isbn13()));
            Assert.That(issn, Is.EqualTo(second.Issn()));
            Assert.That(checks.Isbn10().Validate(isbn10), Is.True);
            Assert.That(checks.Isbn13().Validate(isbn13), Is.True);
            Assert.That(checks.Issn().Validate(issn), Is.True);
            Assert.That(isbn13, Does.Match("^97[89][0-9]{10}$"));
        });
    }

    [Test]
    public void GeneratedDisplayStylesPreserveTheSameUnderlyingRandomValue()
    {
        IdentifierProvider compact = Fake.Create(5010).Identifier();
        IdentifierProvider displayed = Fake.Create(5010).Identifier();

        string isbn10 = compact.Isbn10();
        string isbn13 = compact.Isbn13();
        string issn = compact.Issn();

        Assert.Multiple(() =>
        {
            Assert.That(RemoveDisplay(displayed.Isbn10FromStyle(BookIdentifierStyle.Labeled)), Is.EqualTo(isbn10));
            Assert.That(RemoveDisplay(displayed.Isbn13FromStyle(BookIdentifierStyle.Spaced)), Is.EqualTo(isbn13));
            Assert.That(RemoveDisplay(displayed.IssnFromStyle(BookIdentifierStyle.Hyphenated)), Is.EqualTo(issn));
        });
    }

    [Test]
    public void PublicationSpanGenerationAndFormattingPreserveDestinationSuffixes()
    {
        IdentifierProvider actual = Fake.Create(5011).Identifier();
        IdentifierProvider expected = Fake.Create(5011).Identifier();
        char[] isbn10 = Enumerable.Repeat('#', 24).ToArray();
        char[] isbn13 = Enumerable.Repeat('#', 26).ToArray();
        char[] issn = Enumerable.Repeat('#', 18).ToArray();

        int isbn10Written = actual.WriteIsbn10(isbn10, BookIdentifierStyle.Labeled);
        int isbn13Written = actual.WriteIsbn13(isbn13, BookIdentifierStyle.Labeled);
        int issnWritten = actual.WriteIssn(issn, BookIdentifierStyle.Labeled);

        Assert.Multiple(() =>
        {
            Assert.That(new string(isbn10, 0, isbn10Written), Is.EqualTo(expected.Isbn10FromStyle(BookIdentifierStyle.Labeled)));
            Assert.That(new string(isbn13, 0, isbn13Written), Is.EqualTo(expected.Isbn13FromStyle(BookIdentifierStyle.Labeled)));
            Assert.That(new string(issn, 0, issnWritten), Is.EqualTo(expected.IssnFromStyle(BookIdentifierStyle.Labeled)));
            Assert.That(isbn10.Skip(isbn10Written), Has.All.EqualTo('#'));
            Assert.That(isbn13.Skip(isbn13Written), Has.All.EqualTo('#'));
            Assert.That(issn.Skip(issnWritten), Has.All.EqualTo('#'));
        });
    }

    [Test]
    public void FormattingSpanWritesOnlyTheSelectedVariant()
    {
        IdentifierProvider identifiers = Fake.Create(5012).Identifier();
        char[] destination = Enumerable.Repeat('#', 24).ToArray();

        int written = identifiers.WriteIsbn13FromValueAndDestinationAndStyle("9780306406157".AsSpan(), destination, BookIdentifierStyle.Hyphenated);

        Assert.Multiple(() =>
        {
            Assert.That(new string(destination, 0, written), Is.EqualTo("978-0-306-40615-7"));
            Assert.That(destination.Skip(written), Has.All.EqualTo('#'));
        });
    }

    [Test]
    public void InvalidCompactValuesAndStylesAreRejected()
    {
        IdentifierProvider identifiers = Fake.Create(5013).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.FormatIsbn10("0306406153", BookIdentifierStyle.Compact)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.FormatIsbn13("9780306406158", BookIdentifierStyle.Compact)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.FormatIssn("03178472", BookIdentifierStyle.Compact)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => identifiers.Isbn10FromStyle((BookIdentifierStyle)99)).ParamName, Is.EqualTo("style"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => identifiers.ValidateDoi((string)null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => identifiers.DoiUrl("10.1000/white space")).ParamName, Is.EqualTo("value"));
        });
    }

    [Test]
    public void InvalidLengthsDestinationsAndOutputLimitsDoNotConsumeEntropy()
    {
        FakeOptions options = FakeOptions.Create(5014L);
        options.MaximumOutputSize = 13;
        IdentifierProvider actual = Fake.Create(options).Identifier();
        IdentifierProvider expected = Fake.Create(options).Identifier();

        Assert.Throws<ArgumentOutOfRangeException>(() => actual.Doi());
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.DoiFromRegistrantDigitsAndSuffixLength(0, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.DoiFromRegistrantDigitsAndSuffixLength(4, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.Isbn10FromStyle(BookIdentifierStyle.Labeled));
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.IssnFromStyle(BookIdentifierStyle.Labeled));
        Assert.Throws<ArgumentException>(() => actual.WriteIsbn13(new char[12], BookIdentifierStyle.Compact));

        Assert.That(actual.Isbn13(), Is.EqualTo(expected.Isbn13()));
    }

    [Test]
    public void CountLimitFailuresAreCheckedBeforeRandomDigitsAreDrawn()
    {
        FakeOptions options = FakeOptions.Create(5015L);
        options.MaximumCount = 8;
        IdentifierProvider actual = Fake.Create(options).Identifier();
        IdentifierProvider expected = Fake.Create(options).Identifier();

        Assert.Throws<ArgumentOutOfRangeException>(() => actual.Isbn10());
        Assert.Throws<ArgumentOutOfRangeException>(() => actual.Isbn13());

        Assert.That(actual.Doi(), Is.EqualTo(expected.Doi()));
    }

    [Test]
    public void CryptographicModeProducesValidRestrictedAndChecksumShapes()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        IdentifierProvider identifiers = Fake.Create(options).Identifier();
        ChecksumProvider checks = Fake.Create(options).Checksum();

        int index = 0;
        while (index < 128)
        {
            Assert.That(identifiers.ValidateDoi(identifiers.Doi()), Is.True);
            Assert.That(checks.Isbn10().Validate(identifiers.Isbn10()), Is.True);
            Assert.That(checks.Isbn13().Validate(identifiers.Isbn13()), Is.True);
            Assert.That(checks.Issn().Validate(identifiers.Issn()), Is.True);
            index++;
        }
    }

    static string RemoveDisplay(string value)
    {
        return value.Replace("ISBN ", "").Replace("ISSN ", "").Replace("-", "").Replace(" ", "");
    }
}
