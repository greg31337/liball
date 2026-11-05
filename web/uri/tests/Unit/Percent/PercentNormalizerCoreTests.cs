using System.Globalization;
using System.Text;

namespace uri.Tests;

public class PercentNormalizerCoreTests
{
    [Test]
    public void NormalizeHexCaseUppercasesEveryPossibleOctet()
    {
        StringBuilder input = new StringBuilder();
        StringBuilder expected = new StringBuilder();
        for (int value = 0; value <= 255; value++)
        {
            string upper = value.ToString("X2", CultureInfo.InvariantCulture);
            string lower = upper.ToLowerInvariant();
            input.Append('%');
            input.Append(value % 2 == 0 ? lower : upper);
            expected.Append('%');
            expected.Append(upper);
        }

        PercentNormalizeResult result = PercentNormalizerCore.NormalizeHexCase(
            input.ToString(),
            expected.Length,
            UriComponent.Input,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected.ToString()));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void NormalizeHexCaseChangesOnlyTripletDigits()
    {
        string input = "literal-aAfF \u00e4 \ud83d\ude00 %aF%2f%7e";

        PercentNormalizeResult result = PercentNormalizerCore.NormalizeHexCase(
            input,
            input.Length,
            UriComponent.Fragment,
            12);

        Assert.That(result.Text, Is.EqualTo("literal-aAfF \u00e4 \ud83d\ude00 %AF%2F%7E"));
    }

    [Test]
    public void DecodeUnreservedDecodesExactlyTheAsciiUnreservedOctets()
    {
        StringBuilder input = new StringBuilder();
        StringBuilder expected = new StringBuilder();
        for (int value = 0; value <= 255; value++)
        {
            string lower = value.ToString("x2", CultureInfo.InvariantCulture);
            input.Append('%');
            input.Append(lower);
            if (value <= 127 && Ascii.IsUnreserved((char)value))
            {
                expected.Append((char)value);
            }
            else
            {
                expected.Append('%');
                expected.Append(lower);
            }
        }

        PercentNormalizeResult result = PercentNormalizerCore.DecodeUnreserved(
            input.ToString(),
            expected.Length,
            UriComponent.Input,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected.ToString()));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void DecodeUnreservedPreservesReservedDelimiterSemanticsAndHexCase()
    {
        string input = "%3a%2f%3f%23%5b%5d%40%21%24%26%27%28%29%2a%2b%2c%3b%3d";

        PercentNormalizeResult result = PercentNormalizerCore.DecodeUnreserved(
            input,
            input.Length,
            UriComponent.Path,
            0);

        Assert.That(result.Text, Is.EqualTo(input));
    }

    [Test]
    public void DecodeUnreservedPreservesUtf8OctetsAndDoubleEncodingBoundaries()
    {
        PercentNormalizeResult result = PercentNormalizerCore.DecodeUnreserved(
            "%C3%A4%252F%257e%2F%7e",
            100,
            UriComponent.PathSegment,
            0);

        Assert.That(result.Text, Is.EqualTo("%C3%A4%252F%257e%2F~"));
    }

    [Test]
    public void HexNormalizationThenUnreservedDecodingProducesRfcSyntaxForm()
    {
        PercentNormalizeResult hex = PercentNormalizerCore.NormalizeHexCase(
            "a%2fb%7ec%41%25%ff",
            100,
            UriComponent.Path,
            0);
        PercentNormalizeResult decoded = PercentNormalizerCore.DecodeUnreserved(
            hex.Text,
            100,
            UriComponent.Path,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(hex.Text, Is.EqualTo("a%2Fb%7Ec%41%25%FF"));
            Assert.That(decoded.Text, Is.EqualTo("a%2Fb~cA%25%FF"));
        });
    }

    [Test]
    public void BothTransformationsAreIdempotent()
    {
        PercentNormalizeResult firstHex = PercentNormalizerCore.NormalizeHexCase(
            "%2f%7e%C3%a4",
            100,
            UriComponent.Input,
            0);
        PercentNormalizeResult secondHex = PercentNormalizerCore.NormalizeHexCase(
            firstHex.Text,
            100,
            UriComponent.Input,
            0);
        PercentNormalizeResult firstDecode = PercentNormalizerCore.DecodeUnreserved(
            "%2f%7e%C3%a4",
            100,
            UriComponent.Input,
            0);
        PercentNormalizeResult secondDecode = PercentNormalizerCore.DecodeUnreserved(
            firstDecode.Text,
            100,
            UriComponent.Input,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(secondHex.Text, Is.EqualTo(firstHex.Text));
            Assert.That(secondDecode.Text, Is.EqualTo(firstDecode.Text));
        });
    }

    [Test]
    public void StringAndSpanEntryPointsProduceIdenticalResults()
    {
        string input = "a%2fb%7Ec";

        PercentNormalizeResult hexText = PercentNormalizerCore.NormalizeHexCase(
            input,
            100,
            UriComponent.Query,
            5);
        PercentNormalizeResult hexSpan = PercentNormalizerCore.NormalizeHexCaseSpan(
            input.AsSpan(),
            100,
            UriComponent.Query,
            5);
        PercentNormalizeResult decodeText = PercentNormalizerCore.DecodeUnreserved(
            input,
            100,
            UriComponent.Query,
            5);
        PercentNormalizeResult decodeSpan = PercentNormalizerCore.DecodeUnreservedSpan(
            input.AsSpan(),
            100,
            UriComponent.Query,
            5);

        Assert.Multiple(() =>
        {
            Assert.That(hexSpan.Text, Is.EqualTo(hexText.Text));
            Assert.That(decodeSpan.Text, Is.EqualTo(decodeText.Text));
        });
    }

    [TestCase("%", 20, 1)]
    [TestCase("%0", 20, 1)]
    [TestCase("%G0", 21, 1)]
    [TestCase("%0G", 22, 1)]
    [TestCase("ok%20bad%XZ", 29, 1)]
    public void MalformedTripletFailsBothTransformationsWithoutPartialText(
        string input,
        int expectedOffset,
        int expectedLength)
    {
        PercentNormalizeResult hex = PercentNormalizerCore.NormalizeHexCase(
            input,
            100,
            UriComponent.Host,
            20);
        PercentNormalizeResult decoded = PercentNormalizerCore.DecodeUnreserved(
            input,
            100,
            UriComponent.Host,
            20);

        AssertMalformedFailure(hex, expectedOffset, expectedLength);
        AssertMalformedFailure(decoded, expectedOffset, expectedLength);
    }

    [Test]
    public void OutputLimitsUseTheTransformedLengthAndReturnNoPartialText()
    {
        PercentNormalizeResult hex = PercentNormalizerCore.NormalizeHexCase(
            "%7e%41",
            5,
            UriComponent.Input,
            0);
        PercentNormalizeResult decoded = PercentNormalizerCore.DecodeUnreserved(
            "%7e%41",
            1,
            UriComponent.Input,
            0);

        AssertLimitFailure(hex, 5, 1);
        AssertLimitFailure(decoded, 1, 1);
    }

    [Test]
    public void ExactAndEmptyOutputLimitsSucceed()
    {
        PercentNormalizeResult hex = PercentNormalizerCore.NormalizeHexCase(
            "%7e",
            3,
            UriComponent.Input,
            0);
        PercentNormalizeResult decoded = PercentNormalizerCore.DecodeUnreserved(
            "%7e",
            1,
            UriComponent.Input,
            0);
        PercentNormalizeResult empty = PercentNormalizerCore.DecodeUnreserved(
            "",
            0,
            UriComponent.Input,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(hex.Success, Is.True);
            Assert.That(hex.Text, Is.EqualTo("%7E"));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Text, Is.EqualTo("~"));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Text, Is.Empty);
        });
    }

    [Test]
    public void LexicalErrorTakesPrecedenceOverOutputLimit()
    {
        PercentNormalizeResult result = PercentNormalizerCore.DecodeUnreserved(
            "%GG",
            0,
            UriComponent.Input,
            0);

        Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidPercentEncoding));
    }

    [Test]
    public void NegativeOutputLimitFailsDeterministicallyForEmptyInput()
    {
        PercentNormalizeResult result = PercentNormalizerCore.NormalizeHexCase(
            "",
            -1,
            UriComponent.Input,
            0);

        AssertLimitFailure(result, 0, 0);
    }

    private static void AssertMalformedFailure(
        PercentNormalizeResult result,
        int offset,
        int length)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.PercentEncoding));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPercentEncoding));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Host));
        Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(result.Diagnostic.Message, Is.EqualTo(DiagnosticFactory.InvalidPercentEncodingMessage));
    }

    private static void AssertLimitFailure(PercentNormalizeResult result, int offset, int length)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.PercentEncoding));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Output));
        Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(result.Diagnostic.Message, Is.EqualTo(LimitGuard.OutputLimitMessage));
    }
}
