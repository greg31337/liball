using System.Text;

namespace pop3.Tests;

public class Pop3LanguageParserTests
{
    [TestCase("en")]
    [TestCase("en-GB-oed")]
    [TestCase("ZH-MIN-NAN")]
    [TestCase("x-private")]
    [TestCase("zh-cmn-Hans-CN-variant-a-extend-x-private")]
    [TestCase("de-CH-1901")]
    [TestCase("es-419")]
    public void ValidLanguageTagFormsAreAccepted(string tag)
    {
        Pop3LanguageParser parser = new Pop3LanguageParser();

        Assert.That(parser.IsValidLanguageTag(tag), Is.True);
        Assert.That(
            parser.ParseLanguage(Encoding.ASCII.GetBytes(tag)).Tag,
            Is.EqualTo(tag));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("é")]
    [TestCase("en.")]
    [TestCase("-en")]
    [TestCase("en-")]
    [TestCase("abcdefghi")]
    [TestCase("1n")]
    [TestCase("a")]
    [TestCase("en-12A")]
    [TestCase("en-abcde-ABCDE")]
    [TestCase("en-a-one-a-two")]
    [TestCase("en-a")]
    [TestCase("en-x")]
    public void InvalidLanguageTagFormsAreRejected(string? tag)
    {
        Assert.That(new Pop3LanguageParser().IsValidLanguageTag(tag), Is.False);
    }

    [Test]
    public void ParserHandlesDescriptionsAndRejectsEveryMalformedListingShape()
    {
        Pop3LanguageParser parser = new Pop3LanguageParser();
        Pop3Language described = parser.ParseLanguage(
            Encoding.UTF8.GetBytes("fr Français"));

        Assert.That(described.Tag, Is.EqualTo("fr"));
        Assert.That(described.Description, Is.EqualTo("Français"));
        Assert.Throws<InvalidDataException>(() => parser.ParseLanguage(
            Array.Empty<byte>()));
        Assert.Throws<InvalidDataException>(() => parser.ParseLanguage(
            Encoding.ASCII.GetBytes("en ")));
        Assert.Throws<InvalidDataException>(() => parser.ParseLanguage(
            Encoding.ASCII.GetBytes("en  English")));
        Assert.Throws<InvalidDataException>(() => parser.ParseLanguage(
            new byte[] { 101, 110, 32, 195 }));
        Assert.Throws<InvalidDataException>(() => parser.ParseLanguage(
            new byte[] { 101, 110, 32, 127 }));
        Assert.Throws<InvalidDataException>(() => parser.ParseLanguage(
            Encoding.ASCII.GetBytes(new string('a', 511))));
    }

    [Test]
    public void LanguageCollectionsRejectNullLinesAndCaseInsensitiveDuplicates()
    {
        Pop3LanguageParser parser = new Pop3LanguageParser();

        Assert.Throws<ArgumentNullException>(() => parser.ParseLanguages(null!));
        Assert.Throws<InvalidDataException>(() => parser.ParseLanguages(
            new byte[][] { null! }));
        Assert.Throws<InvalidDataException>(() => parser.ParseLanguages(
            new[]
            {
                Encoding.ASCII.GetBytes("en English"),
                Encoding.ASCII.GetBytes("EN duplicate")
            }));
    }
}
