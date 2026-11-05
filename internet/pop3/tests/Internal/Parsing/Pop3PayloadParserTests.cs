using System.Text;

namespace pop3.Tests;

public class Pop3PayloadParserTests
{
    [Test]
    public void StatParserReadsRequiredFieldsAndIgnoresPermittedTrailingText()
    {
        Pop3ListParser parser = new Pop3ListParser();
        Pop3MailboxStatus ordinary = parser.ParseMailboxStatus(Ascii("2 320"));
        Pop3MailboxStatus extended = parser.ParseMailboxStatus(Ascii("0 0 messages ready"));

        Assert.That(ordinary.MessageCount, Is.EqualTo(2));
        Assert.That(ordinary.TotalOctets, Is.EqualTo(320));
        Assert.That(extended.MessageCount, Is.Zero);
        Assert.That(extended.TotalOctets, Is.Zero);
    }

    [Test]
    public void ListParserReadsOneEntryAndPermittedTrailingText()
    {
        Pop3MessageInfo message = new Pop3ListParser().ParseListEntry(Ascii("7 4096 extra information"));

        Assert.That(message.MessageNumber, Is.EqualTo(7));
        Assert.That(message.OctetCount, Is.EqualTo(4096));
        Assert.That(message.IsDeleted, Is.False);
        Assert.That(message.IsUtf8Representation, Is.False);
    }

    [Test]
    public void ListParserReadsTheRfc1939MultilineExample()
    {
        Pop3MailboxStatus expected = Status(2, 320);

        Pop3MessageInfo[] messages = new Pop3ListParser().ParseListEntries(
            Lines("1 120", "2 200"),
            expected);

        Assert.That(messages.Select(message => message.MessageNumber), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(messages.Select(message => message.OctetCount), Is.EqualTo(new long[] { 120, 200 }));
    }

    [Test]
    public void EmptyListMatchesAnEmptyMailboxStatus()
    {
        Pop3MessageInfo[] messages = new Pop3ListParser().ParseListEntries(
            Array.Empty<byte[]>(),
            Status(0, 0));

        Assert.That(messages, Is.Empty);
    }

    [Test]
    public void ListCollectionPreservesServerOrderAndAllowsDeletedNumberGaps()
    {
        Pop3MailboxStatus expected = new Pop3MailboxStatus();
        expected.MessageCount = 3;
        expected.TotalOctets = 600;

        Pop3MessageInfo[] messages = new Pop3ListParser().ParseListEntries(
            Lines("3 300", "1 100", "5 200"),
            expected);

        Assert.That(messages.Select(message => message.MessageNumber), Is.EqualTo(new[] { 3, 1, 5 }));
        Assert.That(messages.Select(message => message.OctetCount), Is.EqualTo(new long[] { 300, 100, 200 }));
    }

    [Test]
    public void ListCollectionChecksUniquenessAndStatConsistency()
    {
        Pop3ListParser parser = new Pop3ListParser();
        Pop3MailboxStatus countMismatch = Status(1, 300);
        Pop3MailboxStatus totalMismatch = Status(2, 301);

        Assert.Throws<InvalidDataException>(() => parser.ParseListEntries(Lines("1 100", "1 200"), null));
        Assert.Throws<InvalidDataException>(() => parser.ParseListEntries(Lines("1 100", "2 200"), countMismatch));
        Assert.Throws<InvalidDataException>(() => parser.ParseListEntries(Lines("1 100", "2 200"), totalMismatch));
        Assert.Throws<InvalidDataException>(() => parser.ParseListEntries(
            Lines("1 9223372036854775807", "2 1"), null));
    }

    [TestCase("")]
    [TestCase("1")]
    [TestCase("1 ")]
    [TestCase(" 1")]
    [TestCase("1  2")]
    [TestCase("+1 2")]
    [TestCase("-1 2")]
    [TestCase("0 2")]
    [TestCase("2147483648 2")]
    [TestCase("1 -2")]
    [TestCase("1 9223372036854775808")]
    [TestCase("one 2")]
    public void ListParserRejectsInvalidRequiredFields(string payload)
    {
        Assert.Throws<InvalidDataException>(() => new Pop3ListParser().ParseListEntry(Ascii(payload)));
    }

    [TestCase("")]
    [TestCase("1")]
    [TestCase("1 ")]
    [TestCase("1  2")]
    [TestCase("+1 2")]
    [TestCase("-1 2")]
    [TestCase("1 -2")]
    [TestCase("2147483648 2")]
    [TestCase("1 9223372036854775808")]
    public void StatParserRejectsInvalidRequiredFields(string payload)
    {
        Assert.Throws<InvalidDataException>(() => new Pop3ListParser().ParseMailboxStatus(Ascii(payload)));
    }

    [TestCaseSource(nameof(InvalidNumericTrailingText))]
    public void ListAndStatParsersRejectControlOctetsInTrailingText(byte[] payload)
    {
        Pop3ListParser parser = new Pop3ListParser();

        Assert.Throws<InvalidDataException>(() => parser.ParseListEntry(payload));
        Assert.Throws<InvalidDataException>(() => parser.ParseMailboxStatus(payload));
    }

    [Test]
    public void ListCollectionRejectsInvalidInputs()
    {
        Pop3ListParser parser = new Pop3ListParser();

        Assert.Throws<ArgumentNullException>(() => parser.ParseListEntries(null!, null));
        Assert.Throws<InvalidDataException>(() => parser.ParseListEntries(new byte[][] { null! }, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => parser.ParseListEntries(
            Array.Empty<byte[]>(), Status(-1, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => parser.ParseListEntries(
            Array.Empty<byte[]>(), Status(0, -1)));
    }

    [Test]
    public void UidlParserReadsTheRfcExamplesAndMaximumLengthValue()
    {
        Pop3UidlParser parser = new Pop3UidlParser();
        Pop3UniqueId[] examples = parser.ParseUniqueIds(
            Lines("1 whqtswO00WBw418f9t5JxYwZ", "2 QhdPYR:00WBw1Ph7x7"),
            new[] { Message(1), Message(2) });
        string maximumValue = new string('A', 70);
        Pop3UniqueId maximum = parser.ParseUniqueId(Ascii("2147483647 " + maximumValue));

        Assert.That(examples.Select(value => value.MessageNumber), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(examples.Select(value => value.Value), Is.EqualTo(new[]
        {
            "whqtswO00WBw418f9t5JxYwZ",
            "QhdPYR:00WBw1Ph7x7"
        }));
        Assert.That(maximum.MessageNumber, Is.EqualTo(int.MaxValue));
        Assert.That(maximum.Value, Is.EqualTo(maximumValue));
    }

    [Test]
    public void SingleUidlResponseAllowsPrintableTrailingResponseText()
    {
        Pop3UidlParser parser = new Pop3UidlParser();

        Pop3UniqueId uniqueId = parser.ParseSingleUniqueIdResponse(
            Ascii("2 stable-id Unique-id listing."));

        Assert.That(uniqueId.MessageNumber, Is.EqualTo(2));
        Assert.That(uniqueId.Value, Is.EqualTo("stable-id"));
        Assert.Throws<InvalidDataException>(() => parser.ParseSingleUniqueIdResponse(
            new byte[] { 50, 32, 115, 116, 97, 98, 108, 101, 45, 105, 100, 32, 1 }));
    }

    [Test]
    public void EmptyUidlListMatchesAnEmptyMessageList()
    {
        Pop3UniqueId[] uniqueIds = new Pop3UidlParser().ParseUniqueIds(
            Array.Empty<byte[]>(),
            Array.Empty<Pop3MessageInfo>());

        Assert.That(uniqueIds, Is.Empty);
    }

    [Test]
    public void UidlCollectionPreservesOrderAndAllowsIdenticalMessageContentIds()
    {
        Pop3MessageInfo[] messages = new[] { Message(2), Message(1) };

        Pop3UniqueId[] uniqueIds = new Pop3UidlParser().ParseUniqueIds(
            Lines("2 same-id", "1 same-id"),
            messages);

        Assert.That(uniqueIds.Select(value => value.MessageNumber), Is.EqualTo(new[] { 2, 1 }));
        Assert.That(uniqueIds.Select(value => value.Value), Is.EqualTo(new[] { "same-id", "same-id" }));
    }

    [Test]
    public void UidlCollectionChecksMessageNumberUniquenessAndListConsistency()
    {
        Pop3UidlParser parser = new Pop3UidlParser();

        Assert.Throws<InvalidDataException>(() => parser.ParseUniqueIds(Lines("1 first", "1 second"), null));
        Assert.Throws<InvalidDataException>(() => parser.ParseUniqueIds(
            Lines("1 first"), new[] { Message(1), Message(2) }));
        Assert.Throws<InvalidDataException>(() => parser.ParseUniqueIds(
            Lines("1 first", "3 third"), new[] { Message(1), Message(2) }));
    }

    [TestCase("")]
    [TestCase("1")]
    [TestCase("  value")]
    [TestCase("0 value")]
    [TestCase("2147483648 value")]
    [TestCase("+1 value")]
    [TestCase("1 ")]
    [TestCase("1  value")]
    [TestCase("1 value extra")]
    public void UidlParserRejectsInvalidSyntax(string payload)
    {
        Assert.Throws<InvalidDataException>(() => new Pop3UidlParser().ParseUniqueId(Ascii(payload)));
    }

    [Test]
    public void UidlParserRejectsInvalidValueOctetsAndLengths()
    {
        Pop3UidlParser parser = new Pop3UidlParser();

        Assert.Throws<InvalidDataException>(() => parser.ParseUniqueId(Ascii("1 " + new string('A', 71))));
        Assert.Throws<InvalidDataException>(() => parser.ParseUniqueId(new byte[] { 49, 32, 0 }));
        Assert.Throws<InvalidDataException>(() => parser.ParseUniqueId(new byte[] { 49, 32, 127 }));
        Assert.Throws<InvalidDataException>(() => parser.ParseUniqueId(new byte[] { 49, 32, 195, 169 }));
        Assert.Throws<ArgumentNullException>(() => parser.ParseUniqueIds(null!, null));
        Assert.Throws<InvalidDataException>(() => parser.ParseUniqueIds(new byte[][] { null! }, null));
        Assert.Throws<InvalidDataException>(() => parser.ParseUniqueIds(
            Lines("1 value"), new Pop3MessageInfo[] { null! }));
    }

    [Test]
    public void LanguageParserPreservesUtf8DescriptionsAndOptionalAbsence()
    {
        Pop3LanguageParser parser = new Pop3LanguageParser();
        Pop3Language described = parser.ParseLanguage(Encoding.UTF8.GetBytes("de Deutsch – Österreich"));
        Pop3Language bare = parser.ParseLanguage(Ascii("en-US"));

        Assert.That(described.Tag, Is.EqualTo("de"));
        Assert.That(described.Description, Is.EqualTo("Deutsch – Österreich"));
        Assert.That(bare.Tag, Is.EqualTo("en-US"));
        Assert.That(bare.Description, Is.Empty);
    }

    [Test]
    public void LanguageParserReadsTheRfc6856ListingExample()
    {
        Pop3Language[] languages = new Pop3LanguageParser().ParseLanguages(
            Lines(
                "en English",
                "en-boont English Boontling dialect",
                "de Deutsch",
                "it Italiano",
                "es Espanol",
                "sv Svenska"));

        Assert.That(languages.Select(language => language.Tag), Is.EqualTo(new[]
        {
            "en", "en-boont", "de", "it", "es", "sv"
        }));
        Assert.That(languages.Select(language => language.Description), Is.EqualTo(new[]
        {
            "English", "English Boontling dialect", "Deutsch", "Italiano", "Espanol", "Svenska"
        }));
    }

    [TestCase("en")]
    [TestCase("zh-Hant-CN")]
    [TestCase("zh-cmn-Hans-CN")]
    [TestCase("de-CH-1901")]
    [TestCase("sl-rozaj-biske-1994")]
    [TestCase("en-a-aaa-b-bbb-x-private")]
    [TestCase("x-private")]
    [TestCase("en-x-private-x-more")]
    [TestCase("i-klingon")]
    public void LanguageTagValidatorAcceptsWellFormedBcp47Tags(string tag)
    {
        Assert.That(new Pop3LanguageParser().IsValidLanguageTag(tag), Is.True);
    }

    [TestCase("i-madeup")]
    [TestCase("en-a")]
    [TestCase("en-x")]
    [TestCase("en-12")]
    [TestCase("en-abc1")]
    [TestCase("en-a-1")]
    [TestCase("en-US-US")]
    [TestCase("en-Latn-Latn")]
    [TestCase("de-1901-1901")]
    [TestCase("en-a-aaa-a-bbb")]
    public void LanguageTagValidatorRejectsMalformedBcp47Tags(string tag)
    {
        Pop3LanguageParser parser = new Pop3LanguageParser();

        Assert.That(parser.IsValidLanguageTag(tag), Is.False);
        Assert.Throws<InvalidDataException>(() => parser.ParseLanguage(Ascii(tag)));
    }

    [Test]
    public void LanguageCollectionPreservesOrderAndRejectsCaseInsensitiveDuplicates()
    {
        Pop3LanguageParser parser = new Pop3LanguageParser();
        Pop3Language[] languages = parser.ParseLanguages(Lines("en English", "de Deutsch", "x-private Custom"));

        Assert.That(languages.Select(language => language.Tag), Is.EqualTo(new[] { "en", "de", "x-private" }));
        Assert.Throws<InvalidDataException>(() => parser.ParseLanguages(Lines("en English", "EN English again")));
    }

    [TestCase("")]
    [TestCase(" en")]
    [TestCase("en-")]
    [TestCase("en--US")]
    [TestCase("1n English")]
    [TestCase("a Invalid")]
    [TestCase("x Invalid")]
    [TestCase("en_Us English")]
    [TestCase("englishxx Too long")]
    [TestCase("en ")]
    [TestCase("en  English")]
    public void LanguageParserRejectsInvalidListingSyntax(string payload)
    {
        Assert.Throws<InvalidDataException>(() => new Pop3LanguageParser().ParseLanguage(Ascii(payload)));
    }

    [TestCaseSource(nameof(MalformedUtf8Descriptions))]
    public void LanguageParserRejectsMalformedUtf8Descriptions(byte[] description)
    {
        byte[] prefix = Ascii("en ");
        byte[] payload = new byte[prefix.Length + description.Length];
        Array.Copy(prefix, payload, prefix.Length);
        Array.Copy(description, 0, payload, prefix.Length, description.Length);

        Assert.Throws<InvalidDataException>(() => new Pop3LanguageParser().ParseLanguage(payload));
    }

    [Test]
    public void LanguageParserEnforcesLineLimitAndRejectsInvalidCollections()
    {
        Pop3LanguageParser parser = new Pop3LanguageParser();
        byte[] exact = Ascii("en " + new string('a', 507));
        byte[] tooLong = Ascii("en " + new string('a', 508));

        Assert.That(parser.ParseLanguage(exact).Description, Has.Length.EqualTo(507));
        Assert.Throws<InvalidDataException>(() => parser.ParseLanguage(tooLong));
        Assert.Throws<ArgumentNullException>(() => parser.ParseLanguages(null!));
        Assert.Throws<InvalidDataException>(() => parser.ParseLanguages(new byte[][] { null! }));
    }

    static IEnumerable<TestCaseData> MalformedUtf8Descriptions()
    {
        yield return new TestCaseData(new byte[] { 195 });
        yield return new TestCaseData(new byte[] { 192, 128 });
        yield return new TestCaseData(new byte[] { 224, 128, 128 });
        yield return new TestCaseData(new byte[] { 237, 160, 128 });
        yield return new TestCaseData(new byte[] { 240, 128, 128, 128 });
        yield return new TestCaseData(new byte[] { 244, 144, 128, 128 });
        yield return new TestCaseData(new byte[] { 245, 128, 128, 128 });
        yield return new TestCaseData(new byte[] { 0 });
        yield return new TestCaseData(new byte[] { 127 });
    }

    static IEnumerable<TestCaseData> InvalidNumericTrailingText()
    {
        yield return new TestCaseData(new byte[] { 49, 32, 50, 32, 0 });
        yield return new TestCaseData(new byte[] { 49, 32, 50, 32, 31 });
        yield return new TestCaseData(new byte[] { 49, 32, 50, 32, 127 });
    }

    static byte[] Ascii(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static byte[][] Lines(params string[] values)
    {
        byte[][] lines = new byte[values.Length][];
        int index = 0;
        while (index < values.Length)
        {
            lines[index] = Ascii(values[index]);
            index++;
        }
        return lines;
    }

    static Pop3MailboxStatus Status(int count, long octets)
    {
        Pop3MailboxStatus status = new Pop3MailboxStatus();
        status.MessageCount = count;
        status.TotalOctets = octets;
        return status;
    }

    static Pop3MessageInfo Message(int number)
    {
        Pop3MessageInfo message = new Pop3MessageInfo();
        message.MessageNumber = number;
        return message;
    }
}
