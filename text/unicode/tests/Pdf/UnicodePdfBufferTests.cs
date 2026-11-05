using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfBufferTests
{
    [Test]
    public void EncodeTextStringBuffer_WritesPdfDocEncodingWithoutPartialWrites()
    {
        IUnicodePdfEncoding encoding = new UnicodePdfEncoding();
        byte[] insufficientDestination = new byte[] { 0xCC, 0xCC };

        UnicodeBufferResult insufficient = encoding.EncodeTextStringBuffer(
            "A\u20AC\uFB02".AsSpan(),
            insufficientDestination);

        Assert.That(insufficient.Success, Is.False);
        Assert.That(insufficient.RequiredLength, Is.EqualTo(3));
        Assert.That(insufficient.WrittenLength, Is.Zero);
        Assert.That(insufficientDestination, Is.EqualTo(new byte[] { 0xCC, 0xCC }));

        byte[] destination = new byte[3];
        UnicodeBufferResult complete = encoding.EncodeTextStringBuffer(
            "A\u20AC\uFB02".AsSpan(),
            destination);

        Assert.That(complete.Success, Is.True);
        Assert.That(complete.RequiredLength, Is.EqualTo(3));
        Assert.That(complete.WrittenLength, Is.EqualTo(3));
        Assert.That(destination, Is.EqualTo(new byte[] { 0x41, 0xA0, 0x94 }));
    }

    [Test]
    public void EncodeTextStringBuffer_FallsBackToUtf16BeAndPreservesSupplementaryScalars()
    {
        IUnicodePdfEncoding encoding = new UnicodePdfEncoding();
        byte[] destination = new byte[8];

        UnicodeBufferResult result = encoding.EncodeTextStringBuffer(
            "A\U0001F600".AsSpan(),
            destination);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RequiredLength, Is.EqualTo(8));
        Assert.That(result.WrittenLength, Is.EqualTo(8));
        Assert.That(destination, Is.EqualTo(new byte[]
        {
            0xFE,
            0xFF,
            0x00,
            0x41,
            0xD8,
            0x3D,
            0xDE,
            0x00,
        }));
    }

    [Test]
    public void EncodeUtf16BeWithBomBuffer_ReplacesMalformedUtf16WithoutPartialWrites()
    {
        IUnicodePdfEncoding encoding = new UnicodePdfEncoding();
        byte[] insufficientDestination = new byte[] { 0xCC, 0xCC, 0xCC, 0xCC, 0xCC };

        UnicodeBufferResult insufficient = encoding.EncodeUtf16BeWithBomBuffer(
            "\uD800A".AsSpan(),
            insufficientDestination);

        Assert.That(insufficient.Success, Is.False);
        Assert.That(insufficient.RequiredLength, Is.EqualTo(6));
        Assert.That(insufficient.WrittenLength, Is.Zero);
        Assert.That(insufficientDestination, Is.All.EqualTo(0xCC));

        byte[] destination = new byte[6];
        UnicodeBufferResult complete = encoding.EncodeUtf16BeWithBomBuffer(
            "\uD800A".AsSpan(),
            destination);

        Assert.That(complete.Success, Is.True);
        Assert.That(destination, Is.EqualTo(new byte[] { 0xFE, 0xFF, 0xFF, 0xFD, 0x00, 0x41 }));
    }

    [Test]
    public void EncodePdfDocEncodingBuffer_WritesExactMappingsAndRejectsUnmappableText()
    {
        IUnicodePdfEncoding encoding = new UnicodePdfEncoding();
        byte[] destination = new byte[2];

        UnicodeBufferResult result = encoding.EncodePdfDocEncodingBuffer(
            "A\u20AC".AsSpan(),
            destination);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RequiredLength, Is.EqualTo(2));
        Assert.That(result.WrittenLength, Is.EqualTo(2));
        Assert.That(destination, Is.EqualTo(new byte[] { 0x41, 0xA0 }));
        Assert.Throws<ArgumentException>(() => encoding.EncodePdfDocEncodingBuffer(
            "\u0100".AsSpan(),
            destination));
        Assert.Throws<ArgumentException>(() => encoding.EncodePdfDocEncodingBuffer(
            "\uD800".AsSpan(),
            destination));
    }

    [Test]
    public void EscapeLiteralStringBuffer_WritesAsciiEscapesWithoutPartialWrites()
    {
        IUnicodePdfText pdfText = new UnicodePdfText();
        byte[] bytes = new byte[] { 0x41, 0x28, 0x00, 0x0A, 0x5C, 0xFF };
        byte[] insufficientDestination = new byte[14];
        int index = 0;
        while (index < insufficientDestination.Length)
        {
            insufficientDestination[index] = 0xCC;
            index = index + 1;
        }

        UnicodeBufferResult insufficient = pdfText.EscapeLiteralStringBuffer(
            bytes,
            insufficientDestination);

        Assert.That(insufficient.Success, Is.False);
        Assert.That(insufficient.RequiredLength, Is.EqualTo(15));
        Assert.That(insufficient.WrittenLength, Is.Zero);
        Assert.That(insufficientDestination, Is.All.EqualTo(0xCC));

        byte[] destination = new byte[15];
        UnicodeBufferResult complete = pdfText.EscapeLiteralStringBuffer(bytes, destination);

        Assert.That(complete.Success, Is.True);
        Assert.That(complete.RequiredLength, Is.EqualTo(15));
        Assert.That(complete.WrittenLength, Is.EqualTo(15));
        Assert.That(System.Text.Encoding.ASCII.GetString(destination), Is.EqualTo("A\\(\\000\\n\\\\\\377"));
        Assert.That(pdfText.EscapeLiteralString(bytes), Is.EqualTo("A\\(\\000\\n\\\\\\377"));
    }

    [Test]
    public void ToHexStringBuffer_WritesUppercaseAsciiWithoutPartialWrites()
    {
        IUnicodePdfText pdfText = new UnicodePdfText();
        byte[] bytes = new byte[] { 0x00, 0x0F, 0xA0, 0xFF };
        byte[] insufficientDestination = new byte[] { 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC };

        UnicodeBufferResult insufficient = pdfText.ToHexStringBuffer(bytes, insufficientDestination);

        Assert.That(insufficient.Success, Is.False);
        Assert.That(insufficient.RequiredLength, Is.EqualTo(8));
        Assert.That(insufficient.WrittenLength, Is.Zero);
        Assert.That(insufficientDestination, Is.All.EqualTo(0xCC));

        byte[] destination = new byte[8];
        UnicodeBufferResult complete = pdfText.ToHexStringBuffer(bytes, destination);

        Assert.That(complete.Success, Is.True);
        Assert.That(complete.RequiredLength, Is.EqualTo(8));
        Assert.That(complete.WrittenLength, Is.EqualTo(8));
        Assert.That(System.Text.Encoding.ASCII.GetString(destination), Is.EqualTo("000FA0FF"));
        Assert.That(pdfText.ToHexString(bytes), Is.EqualTo("000FA0FF"));
    }

    [Test]
    public void PdfBufferMethods_HandleEmptyInputs()
    {
        IUnicodePdfEncoding encoding = new UnicodePdfEncoding();
        IUnicodePdfText pdfText = new UnicodePdfText();

        UnicodeBufferResult textString = encoding.EncodeTextStringBuffer(
            System.ReadOnlySpan<char>.Empty,
            System.Span<byte>.Empty);
        UnicodeBufferResult utf16 = encoding.EncodeUtf16BeWithBomBuffer(
            System.ReadOnlySpan<char>.Empty,
            new byte[2]);
        UnicodeBufferResult pdfDocEncoding = encoding.EncodePdfDocEncodingBuffer(
            System.ReadOnlySpan<char>.Empty,
            System.Span<byte>.Empty);
        UnicodeBufferResult literal = pdfText.EscapeLiteralStringBuffer(
            System.ReadOnlySpan<byte>.Empty,
            System.Span<byte>.Empty);
        UnicodeBufferResult hexadecimal = pdfText.ToHexStringBuffer(
            System.ReadOnlySpan<byte>.Empty,
            System.Span<byte>.Empty);

        Assert.That(textString.Success, Is.True);
        Assert.That(textString.RequiredLength, Is.Zero);
        Assert.That(utf16.Success, Is.True);
        Assert.That(utf16.RequiredLength, Is.EqualTo(2));
        Assert.That(pdfDocEncoding.Success, Is.True);
        Assert.That(literal.Success, Is.True);
        Assert.That(hexadecimal.Success, Is.True);
    }
}
