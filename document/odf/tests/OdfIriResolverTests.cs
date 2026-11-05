namespace odf.Tests;

public class OdfIriResolverTests
{
    [TestCase("https://example.com/path?query=1#fragment")]
    [TestCase("urn:oasis:names:tc:opendocument:xmlns:office:1.0")]
    [TestCase("mailto:user@example.com")]
    [TestCase("https://example.com/a%20document")]
    public void AbsoluteUriAcceptsWellFormedAsciiUris(string value)
    {
        Assert.That(OdfIriResolver.IsAbsoluteUri(value), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("relative/path")]
    [TestCase("https://example.com/has space")]
    [TestCase("https://example.com/bad%2")]
    [TestCase("https://例え.テスト/文書")]
    public void AbsoluteUriRejectsRelativeMalformedAndNonAsciiValues(string? value)
    {
        Assert.That(OdfIriResolver.IsAbsoluteUri(value), Is.False);
    }

    [Test]
    public void AbsoluteIriAcceptsUnicodeWhileIriReferencesAcceptRelativeForms()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdfIriResolver.IsAbsoluteIri("https://例え.テスト/文書"), Is.True);
            Assert.That(OdfIriResolver.IsAbsoluteIri("relative/文書"), Is.False);
            Assert.That(OdfIriResolver.IsIriReference("relative/文書"), Is.True);
            Assert.That(OdfIriResolver.IsIriReference("../Pictures/image%20one.png#view"), Is.True);
            Assert.That(OdfIriResolver.IsIriReference("#bookmark"), Is.True);
            Assert.That(OdfIriResolver.IsIriReference(""), Is.True);
            Assert.That(OdfIriResolver.IsIriReference("bad percent%2"), Is.False);
            Assert.That(OdfIriResolver.IsIriReference("has space"), Is.False);
            Assert.That(OdfIriResolver.IsIriReference("back\\slash"), Is.False);
        });
    }

    [Test]
    public void IriValidationRejectsMalformedUtf16AndNoncharacters()
    {
        string malformed = new string([(char)0xD800]);
        string noncharacter = new string([(char)0xFDD0]);

        Assert.Multiple(() =>
        {
            Assert.That(OdfIriResolver.IsIriReference(malformed), Is.False);
            Assert.That(OdfIriResolver.IsIriReference(noncharacter), Is.False);
        });
    }

    [TestCase("Pictures/image.png")]
    [TestCase("../Pictures/image.png")]
    [TestCase("./content.xml")]
    [TestCase("#bookmark")]
    [TestCase("?view=print")]
    [TestCase("")]
    public void PackageRelativeReferenceAcceptsRelativeIriReferences(string value)
    {
        Assert.That(OdfIriResolver.IsPackageRelativeReference(value), Is.True);
    }

    [TestCase(null)]
    [TestCase("/Pictures/image.png")]
    [TestCase("//example.com/image.png")]
    [TestCase("https://example.com/image.png")]
    [TestCase("Pictures\\image.png")]
    [TestCase("Pictures/image 1.png")]
    public void PackageRelativeReferenceRejectsAbsoluteAndMalformedReferences(string? value)
    {
        Assert.That(OdfIriResolver.IsPackageRelativeReference(value), Is.False);
    }

    [TestCase("content.xml", "Pictures/image.png", "Pictures/image.png")]
    [TestCase("Object 1/content.xml", "../Pictures/image.png#view", "")]
    [TestCase("Object%201/content.xml", "../Pictures/image.png#view", "Pictures/image.png#view")]
    [TestCase("Object%201/content.xml", "styles.xml", "Object%201/styles.xml")]
    [TestCase("Object%201/content.xml", "./styles.xml", "Object%201/styles.xml")]
    [TestCase("Object%201/content.xml", "#bookmark", "Object%201/content.xml#bookmark")]
    [TestCase("Object%201/content.xml", "?mode=print", "Object%201/content.xml?mode=print")]
    [TestCase("Object%201/content.xml", "", "Object%201/content.xml")]
    [TestCase("content.xml", "../outside.xml", "")]
    [TestCase("content.xml", "../../outside.xml", "")]
    public void PackageReferenceResolutionNormalizesDotSegmentsWithoutEscapingRoot(
        string basePart,
        string reference,
        string expected)
    {
        Assert.That(OdfIriResolver.ResolvePackageReference(basePart, reference), Is.EqualTo(expected));
    }

    [TestCase("text:p", "text", "p")]
    [TestCase("dc:title", "dc", "title")]
    [TestCase(":term", "", "term")]
    [TestCase("_:node1", "_", "node1")]
    [TestCase("ex:resource/path", "ex", "resource/path")]
    [TestCase("no-prefix", "", "no-prefix")]
    [TestCase("prefix:", "prefix", "")]
    [TestCase("", "", "")]
    public void CurieParserPreservesPrefixAndReference(string lexical, string prefix, string reference)
    {
        OdfCurieValue result = OdfIriResolver.ParseCurie(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsSafe, Is.False);
            Assert.That(result.LexicalValue, Is.EqualTo(lexical));
            Assert.That(result.Prefix, Is.EqualTo(prefix));
            Assert.That(result.Reference, Is.EqualTo(reference));
            Assert.That(OdfIriResolver.FormatCurie(result), Is.EqualTo(lexical));
        });
    }

    [TestCase("[text:p]", "text", "p")]
    [TestCase("[:term]", "", "term")]
    [TestCase("[ex:resource/path]", "ex", "resource/path")]
    [TestCase("[no-prefix]", "", "no-prefix")]
    [TestCase("[prefix:]", "prefix", "")]
    [TestCase("[]", "", "")]
    public void SafeCurieParserRequiresBrackets(string lexical, string prefix, string reference)
    {
        OdfCurieValue result = OdfIriResolver.ParseSafeCurie(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsSafe, Is.True);
            Assert.That(result.Prefix, Is.EqualTo(prefix));
            Assert.That(result.Reference, Is.EqualTo(reference));
            Assert.That(OdfIriResolver.FormatCurie(result), Is.EqualTo(lexical));
        });
    }

    [TestCase(null)]
    [TestCase("bad prefix:term")]
    [TestCase("prefix:has space")]
    [TestCase("[text:p]")]
    public void CurieParserRejectsMalformedValues(string? lexical)
    {
        Assert.That(OdfIriResolver.ParseCurie(lexical).IsValid, Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("text:p")]
    [TestCase("[bad prefix:term]")]
    public void SafeCurieParserRejectsMalformedValues(string? lexical)
    {
        Assert.That(OdfIriResolver.ParseSafeCurie(lexical).IsValid, Is.False);
    }
}
