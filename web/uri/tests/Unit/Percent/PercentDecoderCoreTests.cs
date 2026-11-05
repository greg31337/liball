using System.Globalization;
using System.Text;

namespace uri.Tests;

public class PercentDecoderCoreTests
{
    [Test]
    public void DecodeBytesAcceptsUpperAndLowerHexForEveryByte()
    {
        StringBuilder upperText = new StringBuilder();
        StringBuilder lowerText = new StringBuilder();
        byte[] expected = new byte[256];
        for (int value = 0; value <= 255; value++)
        {
            string hex = value.ToString("X2", CultureInfo.InvariantCulture);
            upperText.Append('%');
            upperText.Append(hex);
            lowerText.Append('%');
            lowerText.Append(hex.ToLowerInvariant());
            expected[value] = (byte)value;
        }

        PercentByteDecodeResult upper = PercentDecoderCore.DecodeBytes(
            upperText.ToString(),
            256,
            UriComponent.Input,
            0);
        PercentByteDecodeResult lower = PercentDecoderCore.DecodeBytes(
            lowerText.ToString(),
            256,
            UriComponent.Input,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(upper.Success, Is.True);
            Assert.That(upper.Bytes, Is.EqualTo(expected));
            Assert.That(upper.Diagnostic, Is.Null);
            Assert.That(lower.Success, Is.True);
            Assert.That(lower.Bytes, Is.EqualTo(expected));
            Assert.That(lower.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void DecodeBytesCopiesLiteralAsciiAndDecodesOnlyTriplets()
    {
        string text = "AZaz09-._~:/?#[]@!$&'()*+,;=%00%25%7f%80%ff";

        PercentByteDecodeResult result = PercentDecoderCore.DecodeBytes(
            text,
            100,
            UriComponent.Query,
            12);

        byte[] literal = Encoding.ASCII.GetBytes("AZaz09-._~:/?#[]@!$&'()*+,;=");
        byte[] expected = new byte[literal.Length + 5];
        Array.Copy(literal, expected, literal.Length);
        expected[literal.Length] = 0;
        expected[literal.Length + 1] = 37;
        expected[literal.Length + 2] = 127;
        expected[literal.Length + 3] = 128;
        expected[literal.Length + 4] = 255;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(expected));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void DecodeBytesDoesNotInterpretDecodedOctetsAsText()
    {
        PercentByteDecodeResult result = PercentDecoderCore.DecodeBytes(
            "%C3%28%F0%28%8C%BC",
            6,
            UriComponent.Path,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(new byte[] { 195, 40, 240, 40, 140, 188 }));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void TextAndSpanEntryPointsProduceIdenticalBytes()
    {
        string text = "a%2Fb%00%FE";

        PercentByteDecodeResult fromText = PercentDecoderCore.DecodeBytes(
            text,
            20,
            UriComponent.PathSegment,
            5);
        PercentByteDecodeResult fromSpan = PercentDecoderCore.DecodeSpan(
            text.AsSpan(),
            20,
            UriComponent.PathSegment,
            5);

        Assert.Multiple(() =>
        {
            Assert.That(fromText.Success, Is.True);
            Assert.That(fromSpan.Success, Is.True);
            Assert.That(fromText.Bytes, Is.EqualTo(new byte[] { 97, 47, 98, 0, 254 }));
            Assert.That(fromSpan.Bytes, Is.EqualTo(fromText.Bytes));
        });
    }

    [TestCase("%", 20, 1)]
    [TestCase("%0", 20, 1)]
    [TestCase("%G0", 21, 1)]
    [TestCase("%0G", 22, 1)]
    [TestCase("ok%20bad%XZ", 29, 1)]
    public void MalformedTripletReturnsExactDiagnosticAndNoPartialBytes(
        string text,
        int expectedOffset,
        int expectedLength)
    {
        PercentByteDecodeResult result = PercentDecoderCore.DecodeBytes(
            text,
            100,
            UriComponent.Fragment,
            20);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.Empty);
        AssertInvalidDiagnostic(
            result.Diagnostic,
            UriDiagnosticCode.InvalidPercentEncoding,
            UriComponent.Fragment,
            expectedOffset,
            expectedLength,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    [Test]
    public void LiteralNonAsciiIsRejectedInsteadOfChoosingACharacterEncoding()
    {
        PercentByteDecodeResult result = PercentDecoderCore.DecodeBytes(
            "a\u00e9%20",
            100,
            UriComponent.UserInfo,
            30);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.Empty);
        AssertInvalidDiagnostic(
            result.Diagnostic,
            UriDiagnosticCode.InvalidCharacter,
            UriComponent.UserInfo,
            31,
            1,
            DiagnosticFactory.InvalidCharacterMessage);
    }

    [Test]
    public void FirstLexicalErrorWinsBeforeOutputLimitValidation()
    {
        PercentByteDecodeResult nonAsciiFirst = PercentDecoderCore.DecodeBytes(
            "\u00e9%GG",
            0,
            UriComponent.Path,
            4);
        PercentByteDecodeResult percentFirst = PercentDecoderCore.DecodeBytes(
            "%GG\u00e9",
            0,
            UriComponent.Path,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(nonAsciiFirst.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidCharacter));
            Assert.That(nonAsciiFirst.Diagnostic.Range.Offset, Is.EqualTo(4));
            Assert.That(percentFirst.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidPercentEncoding));
            Assert.That(percentFirst.Diagnostic.Range.Offset, Is.EqualTo(5));
        });
    }

    [Test]
    public void OutputLimitReportsDecodedByteOverageAndReturnsNoPartialBytes()
    {
        PercentByteDecodeResult result = PercentDecoderCore.DecodeBytes(
            "A%20%FF",
            2,
            UriComponent.Input,
            0);

        AssertLimitFailure(result, 2, 1);
    }

    [Test]
    public void ExactAndEmptyOutputLimitsSucceed()
    {
        PercentByteDecodeResult exact = PercentDecoderCore.DecodeBytes(
            "A%20%FF",
            3,
            UriComponent.Input,
            0);
        PercentByteDecodeResult empty = PercentDecoderCore.DecodeBytes(
            "",
            0,
            UriComponent.Input,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Bytes, Is.EqualTo(new byte[] { 65, 32, 255 }));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Bytes, Is.Empty);
        });
    }

    [Test]
    public void NegativeOutputLimitFailsDeterministicallyForEmptyInput()
    {
        PercentByteDecodeResult result = PercentDecoderCore.DecodeBytes(
            "",
            -1,
            UriComponent.Input,
            0);

        AssertLimitFailure(result, 0, 0);
    }

    private static void AssertLimitFailure(PercentByteDecodeResult result, int offset, int length)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Bytes, Is.Empty);
        AssertInvalidDiagnostic(
            result.Diagnostic,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output,
            offset,
            length,
            LimitGuard.OutputLimitMessage);
    }

    private static void AssertInvalidDiagnostic(
        UriDiagnostic? diagnostic,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.PercentEncoding));
        Assert.That(diagnostic.Code, Is.EqualTo(code));
        Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(diagnostic.Component, Is.EqualTo(component));
        Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(diagnostic.Message, Is.EqualTo(message));
    }
}
