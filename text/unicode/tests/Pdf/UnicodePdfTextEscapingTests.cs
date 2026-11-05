using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfTextEscapingTests
{
    [Test]
    public void EscapeLiteralString_EscapesDelimitersAndBackslash()
    {
        byte[] bytes = new byte[] { 0x41, 0x28, 0x42, 0x29, 0x5C, 0x43 };

        Assert.That(new UnicodePdfText().EscapeLiteralString(bytes), Is.EqualTo("A\\(B\\)\\\\C"));
    }

    [Test]
    public void EscapeLiteralString_UsesNamedControlEscapes()
    {
        byte[] bytes = new byte[] { 0x08, 0x09, 0x0A, 0x0C, 0x0D };

        Assert.That(new UnicodePdfText().EscapeLiteralString(bytes), Is.EqualTo("\\b\\t\\n\\f\\r"));
    }

    [Test]
    public void EscapeLiteralString_UsesThreeDigitOctalForOtherNonPrintableBytes()
    {
        byte[] bytes = new byte[] { 0x00, 0x1F, 0x7F, 0x80, 0xFF };

        Assert.That(new UnicodePdfText().EscapeLiteralString(bytes), Is.EqualTo("\\000\\037\\177\\200\\377"));
    }

    [Test]
    public void EscapeLiteralString_LeavesPrintableAsciiUnescaped()
    {
        byte[] bytes = new byte[] { 0x20, 0x21, 0x41, 0x5B, 0x5D, 0x7E };

        Assert.That(new UnicodePdfText().EscapeLiteralString(bytes), Is.EqualTo(" !A[]~"));
    }

    [Test]
    public void EscapeLiteralString_EscapesPdfEncodedTextBytes()
    {
        byte[] bytes = new UnicodePdfEncoding().EncodeTextString("A\u20AC");

        Assert.That(bytes, Is.EqualTo(new byte[] { 0x41, 0xA0 }));
        Assert.That(new UnicodePdfText().EscapeLiteralString(bytes), Is.EqualTo("A\\240"));
    }

    [Test]
    public void ToHexString_ReturnsUppercaseTwoDigitPairs()
    {
        byte[] bytes = new byte[] { 0x00, 0x01, 0x0F, 0x10, 0xA0, 0xFF };

        Assert.That(new UnicodePdfText().ToHexString(bytes), Is.EqualTo("00010F10A0FF"));
    }

    [Test]
    public void EscapingHelpers_HandleEmptyInputs()
    {
        Assert.That(new UnicodePdfText().EscapeLiteralString(new byte[] { }), Is.EqualTo(""));
        Assert.That(new UnicodePdfText().ToHexString(new byte[] { }), Is.EqualTo(""));
    }

    [Test]
    public void EscapingHelpers_RejectInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfText().EscapeLiteralString(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfText().ToHexString(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfText.AppendLiteralByte(null!, 0));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfText.AppendOctalByte(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfText.ToHexDigit(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfText.ToHexDigit(16));
    }
}
