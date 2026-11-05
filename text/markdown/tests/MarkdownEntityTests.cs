using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownEntityTests
{
    [Test]
    public void Decode_DecodesNamedEntities()
    {
        string decoded = MarkdownEntity.Decode("&amp; &lt; &gt; &quot; &apos; &ouml; &copy;");

        Assert.That(decoded, Is.EqualTo("& < > \" ' \u00f6 \u00a9"));
    }

    [Test]
    public void Decode_DecodesDecimalAndHexNumericReferences()
    {
        string emoji = char.ConvertFromUtf32(128512);
        string decoded = MarkdownEntity.Decode("&#35; &#x23; &#X23; &#x1F600;");

        Assert.That(decoded, Is.EqualTo("# # # " + emoji));
    }

    [Test]
    public void Decode_ReplacesInvalidNumericCodePoints()
    {
        string replacement = MarkdownInputNormalizer.NullReplacementCharacter.ToString();
        string decoded = MarkdownEntity.Decode("&#0; &#xD800; &#x110000;");

        Assert.That(decoded, Is.EqualTo(replacement + " " + replacement + " " + replacement));
    }

    [Test]
    public void Decode_LeavesOverlongNumericReferencesLiteral()
    {
        string source =
            "&#9999999999999999999999999999999999999999; " +
            "&#xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF;";
        string decoded = MarkdownEntity.Decode(source);

        Assert.That(decoded, Is.EqualTo(source));
    }

    [Test]
    public void Decode_LeavesMalformedReferencesLiteral()
    {
        string decoded = MarkdownEntity.Decode("&unknown; &amp &notit; &#; &#x; &#123");

        Assert.That(decoded, Is.EqualTo("&unknown; &amp &notit; &#; &#x; &#123"));
    }

    [Test]
    public void Decode_ReturnsSameTextWhenNoAmpersandExists()
    {
        string source = "plain text";
        string decoded = MarkdownEntity.Decode(source);

        Assert.That(decoded, Is.EqualTo(source));
        Assert.That(object.ReferenceEquals(source, decoded), Is.True);
    }

    [Test]
    public void DecodeAt_ReturnsDecodedTextAndConsumedLength()
    {
        MarkdownEntityDecodeResult result = MarkdownEntity.DecodeAt("a &amp; b", 2);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Text, Is.EqualTo("&"));
        Assert.That(result.Length, Is.EqualTo(5));
    }

    [Test]
    public void DecodeAt_ReturnsNoMatchWhenIndexDoesNotStartEntity()
    {
        MarkdownEntityDecodeResult result = MarkdownEntity.DecodeAt("a &amp;", 0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.EqualTo(""));
        Assert.That(result.Length, Is.EqualTo(0));
    }

    [Test]
    public void EntityHelpers_ValidateArguments()
    {
        Assert.That(() => MarkdownEntity.Decode(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEntity.DecodeAt(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEntity.DecodeAt("", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownEntity.DecodeAt("", 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownEntityDecodeResult.Create(true, null!, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEntityDecodeResult.Create(true, "", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void DigitHelpers_ClassifySupportedNumericDigits()
    {
        Assert.That(MarkdownEntity.IsDigitForBase('9', 10), Is.True);
        Assert.That(MarkdownEntity.IsDigitForBase('a', 10), Is.False);
        Assert.That(MarkdownEntity.IsDigitForBase('f', 16), Is.True);
        Assert.That(MarkdownEntity.IsDigitForBase('F', 16), Is.True);
        Assert.That(MarkdownEntity.IsDigitForBase('g', 16), Is.False);
        Assert.That(MarkdownEntity.GetDigitValue('F'), Is.EqualTo(15));
        Assert.That(MarkdownEntity.GetDigitValue('g'), Is.EqualTo(-1));
    }
}
