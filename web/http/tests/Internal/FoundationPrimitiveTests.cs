using System.Text;

namespace http.Tests;

public class FoundationPrimitiveTests
{
    [Test]
    public void AsciiCaseOperationsFoldOnlyUppercaseAscii()
    {
        byte[] original = Encoding.ASCII.GetBytes("HeAdEr-123");
        byte[] folded = Ascii.ToLowercaseBytes(original);

        Assert.Multiple(() =>
        {
            Assert.That(folded, Is.EqualTo(Encoding.ASCII.GetBytes("header-123")));
            Assert.That(original, Is.EqualTo(Encoding.ASCII.GetBytes("HeAdEr-123")));
            Assert.That(Ascii.FoldByteToLower(0x40), Is.EqualTo(0x40));
            Assert.That(Ascii.FoldByteToLower(0x5b), Is.EqualTo(0x5b));
            Assert.That(Ascii.FoldByteToLower(0x80), Is.EqualTo(0x80));
        });
    }

    [Test]
    public void AsciiComparisonsIgnoreCaseAndPreserveOrdinalOrdering()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Ascii.EqualsBytesIgnoreCase("Content-Type"u8, "content-type"u8), Is.True);
            Assert.That(Ascii.EqualsBytesIgnoreCase("Content"u8, "contents"u8), Is.False);
            Assert.That(Ascii.EqualsBytesIgnoreCase(new byte[] { 0x80 }, new byte[] { 0x81 }), Is.False);
            Assert.That(Ascii.CompareBytesIgnoreCase("Accept"u8, "accept"u8), Is.Zero);
            Assert.That(Ascii.CompareBytesIgnoreCase("a"u8, "B"u8), Is.LessThan(0));
            Assert.That(Ascii.CompareBytesIgnoreCase("Header"u8, "headers"u8), Is.LessThan(0));
            Assert.That(Ascii.EqualsStringsIgnoreCase("ETag", "etag"), Is.True);
            Assert.That(Ascii.EqualsStringsIgnoreCase("ETag", "ETags"), Is.False);
        });
    }

    [Test]
    public void AsciiValidationRejectsNonAsciiOctets()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Ascii.IsValid(new byte[] { 0x00, 0x20, 0x7f }), Is.True);
            Assert.That(Ascii.IsValid(new byte[] { 0x7f, 0x80 }), Is.False);
        });
    }

    [Test]
    public void AsciiWritingIsBoundedAndAllOrNothing()
    {
        byte[] success = [0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc];
        byte[] tooSmall = [0xcc, 0xcc, 0xcc];
        byte[] nonAscii = [0xcc, 0xcc, 0xcc, 0xcc];

        int written = Ascii.WriteString("GET", success, 1);
        int insufficientResult = Ascii.WriteString("GET", tooSmall, 1);
        int invalidResult = Ascii.WriteString("A\u0080", nonAscii, 1);
        int lineEndingResult = Ascii.WriteCarriageReturnLineFeed(success, 4);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(3));
            Assert.That(success, Is.EqualTo(new byte[] { 0xcc, 0x47, 0x45, 0x54, 0x0d, 0x0a }));
            Assert.That(insufficientResult, Is.EqualTo(-1));
            Assert.That(tooSmall, Is.EqualTo(new byte[] { 0xcc, 0xcc, 0xcc }));
            Assert.That(invalidResult, Is.EqualTo(-1));
            Assert.That(nonAscii, Is.EqualTo(new byte[] { 0xcc, 0xcc, 0xcc, 0xcc }));
            Assert.That(lineEndingResult, Is.EqualTo(2));
        });
    }

    [Test]
    public void TokenClassifierMatchesTheHttpTokenAlphabet()
    {
        string punctuation = "!#$%&'*+-.^_`|~";

        for (int value = 0; value <= byte.MaxValue; value++)
        {
            char character = (char)value;
            bool expected = character >= '0' && character <= '9' ||
                character >= 'A' && character <= 'Z' ||
                character >= 'a' && character <= 'z' ||
                punctuation.Contains(character);

            Assert.That(HttpGrammar.IsTokenCharacter((byte)value), Is.EqualTo(expected),
                $"Unexpected classification for octet {value}.");
        }
    }

    [Test]
    public void QuotedTextClassifierAcceptsQdtextAndObsTextOnly()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HttpGrammar.IsQuotedTextCharacter(0x09), Is.True);
            Assert.That(HttpGrammar.IsQuotedTextCharacter(0x20), Is.True);
            Assert.That(HttpGrammar.IsQuotedTextCharacter(0x21), Is.True);
            Assert.That(HttpGrammar.IsQuotedTextCharacter(0x22), Is.False);
            Assert.That(HttpGrammar.IsQuotedTextCharacter(0x5b), Is.True);
            Assert.That(HttpGrammar.IsQuotedTextCharacter(0x5c), Is.False);
            Assert.That(HttpGrammar.IsQuotedTextCharacter(0x5d), Is.True);
            Assert.That(HttpGrammar.IsQuotedTextCharacter(0x7e), Is.True);
            Assert.That(HttpGrammar.IsQuotedTextCharacter(0x7f), Is.False);
            Assert.That(HttpGrammar.IsQuotedTextCharacter(0x80), Is.True);
        });
    }

    [Test]
    public void QuotedPairClassifierAcceptsPermittedEscapedOctets()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HttpGrammar.IsQuotedPairCharacter(0x08), Is.False);
            Assert.That(HttpGrammar.IsQuotedPairCharacter(0x09), Is.True);
            Assert.That(HttpGrammar.IsQuotedPairCharacter(0x20), Is.True);
            Assert.That(HttpGrammar.IsQuotedPairCharacter(0x22), Is.True);
            Assert.That(HttpGrammar.IsQuotedPairCharacter(0x5c), Is.True);
            Assert.That(HttpGrammar.IsQuotedPairCharacter(0x7e), Is.True);
            Assert.That(HttpGrammar.IsQuotedPairCharacter(0x7f), Is.False);
            Assert.That(HttpGrammar.IsQuotedPairCharacter(0xff), Is.True);
        });
    }

    [Test]
    public void DecimalParserAcceptsFullNonnegativeSignedRange()
    {
        IntegerParseResult zero = IntegerParser.ParseDecimal("0"u8);
        IntegerParseResult maximum = IntegerParser.ParseDecimal("9223372036854775807"u8);

        Assert.Multiple(() =>
        {
            Assert.That(zero.IsValid, Is.True);
            Assert.That(zero.Value, Is.Zero);
            Assert.That(zero.Consumed, Is.EqualTo(1));
            Assert.That(maximum.IsValid, Is.True);
            Assert.That(maximum.Value, Is.EqualTo(long.MaxValue));
            Assert.That(maximum.Consumed, Is.EqualTo(19));
        });
    }

    [Test]
    public void DecimalParserReportsMalformedAndOverflowInput()
    {
        IntegerParseResult empty = IntegerParser.ParseDecimal(ReadOnlySpan<byte>.Empty);
        IntegerParseResult malformed = IntegerParser.ParseDecimal("12x"u8);
        IntegerParseResult overflow = IntegerParser.ParseDecimal("9223372036854775808"u8);

        Assert.Multiple(() =>
        {
            Assert.That(empty.IsValid, Is.False);
            Assert.That(empty.Overflow, Is.False);
            Assert.That(empty.Consumed, Is.Zero);
            Assert.That(malformed.IsValid, Is.False);
            Assert.That(malformed.Overflow, Is.False);
            Assert.That(malformed.Consumed, Is.EqualTo(2));
            Assert.That(overflow.IsValid, Is.False);
            Assert.That(overflow.Overflow, Is.True);
            Assert.That(overflow.Consumed, Is.EqualTo(18));
        });
    }

    [Test]
    public void HexadecimalParserAcceptsBothCasesAndReportsOverflow()
    {
        IntegerParseResult mixedCase = IntegerParser.ParseHexadecimal("7fAa"u8);
        IntegerParseResult maximum = IntegerParser.ParseHexadecimal("7fffffffffffffff"u8);
        IntegerParseResult overflow = IntegerParser.ParseHexadecimal("8000000000000000"u8);
        IntegerParseResult malformed = IntegerParser.ParseHexadecimal("fG"u8);

        Assert.Multiple(() =>
        {
            Assert.That(mixedCase.IsValid, Is.True);
            Assert.That(mixedCase.Value, Is.EqualTo(0x7faa));
            Assert.That(maximum.Value, Is.EqualTo(long.MaxValue));
            Assert.That(overflow.IsValid, Is.False);
            Assert.That(overflow.Overflow, Is.True);
            Assert.That(overflow.Consumed, Is.EqualTo(15));
            Assert.That(malformed.IsValid, Is.False);
            Assert.That(malformed.Consumed, Is.EqualTo(1));
        });
    }

    [Test]
    public void SaturatingAdditionAndSubtractionClampAtSignedBounds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SaturatingArithmetic.Add(20, 22), Is.EqualTo(42));
            Assert.That(SaturatingArithmetic.Add(long.MaxValue, 1), Is.EqualTo(long.MaxValue));
            Assert.That(SaturatingArithmetic.Add(long.MinValue, -1), Is.EqualTo(long.MinValue));
            Assert.That(SaturatingArithmetic.Subtract(50, 8), Is.EqualTo(42));
            Assert.That(SaturatingArithmetic.Subtract(long.MinValue, 1), Is.EqualTo(long.MinValue));
            Assert.That(SaturatingArithmetic.Subtract(long.MaxValue, -1), Is.EqualTo(long.MaxValue));
        });
    }

    [Test]
    public void SaturatingMultiplicationClampsWithTheCorrectSign()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SaturatingArithmetic.Multiply(6, 7), Is.EqualTo(42));
            Assert.That(SaturatingArithmetic.Multiply(long.MaxValue, 2), Is.EqualTo(long.MaxValue));
            Assert.That(SaturatingArithmetic.Multiply(long.MinValue, -1), Is.EqualTo(long.MaxValue));
            Assert.That(SaturatingArithmetic.Multiply(long.MinValue, 2), Is.EqualTo(long.MinValue));
            Assert.That(SaturatingArithmetic.Multiply(long.MaxValue, -2), Is.EqualTo(long.MinValue));
            Assert.That(SaturatingArithmetic.Multiply(-4, -5), Is.EqualTo(20));
            Assert.That(SaturatingArithmetic.Multiply(0, long.MinValue), Is.Zero);
        });
    }
}
