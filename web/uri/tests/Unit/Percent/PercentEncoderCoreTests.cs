using System.Globalization;

namespace uri.Tests;

public class PercentEncoderCoreTests
{
    [Test]
    public void EncodeBytesUsesUppercaseHexForEveryByte()
    {
        for (int value = 0; value <= 255; value++)
        {
            byte[] input = new byte[] { (byte)value };
            string expected;
            if (value <= 127 && UriCharacterSets.Unreserved.Contains((char)value))
            {
                expected = new string((char)value, 1);
            }
            else
            {
                expected = "%" + value.ToString("X2", CultureInfo.InvariantCulture);
            }

            PercentEncodeResult result = PercentEncoderCore.EncodeBytes(
                input,
                PercentEncodeSet.Unreserved,
                3);

            Assert.That(result.Success, Is.True, $"byte {value}");
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Diagnostic, Is.Null);
        }
    }

    [Test]
    public void EveryEncodeSetPreservesOnlyItsAllowedAsciiBytes()
    {
        foreach (PercentEncodeSet encodeSet in Enum.GetValues<PercentEncodeSet>())
        {
            for (int value = 0; value <= 127; value++)
            {
                byte[] input = new byte[] { (byte)value };
                PercentEncodeResult result = PercentEncoderCore.EncodeBytes(input, encodeSet, 3);

                if (PercentAllowlist.IsAllowed((char)value, encodeSet))
                {
                    Assert.That(result.Text, Is.EqualTo(new string((char)value, 1)));
                }
                else
                {
                    Assert.That(result.Text, Does.Match("^%[0-9A-F]{2}$"));
                }
            }
        }
    }

    [Test]
    public void EncodeBytesDoesNotInterpretArbitraryBytesAsText()
    {
        byte[] input = new byte[] { 0, 65, 128, 255 };

        PercentEncodeResult result = PercentEncoderCore.EncodeBytes(
            input,
            PercentEncodeSet.Unreserved,
            10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo("%00A%80%FF"));
        });
    }

    [TestCase("AZaz09-._~", PercentEncodeSet.Unreserved, "AZaz09-._~")]
    [TestCase("a/b?c:@", PercentEncodeSet.Query, "a/b?c:@")]
    [TestCase("a/b?c:@", PercentEncodeSet.PathSegment, "a%2Fb%3Fc:@")]
    [TestCase("%2f", PercentEncodeSet.Query, "%252f")]
    [TestCase("hello world!", PercentEncodeSet.Unreserved, "hello%20world%21")]
    [TestCase("title value", PercentEncodeSet.WebLinkExtendedValue, "title%20value")]
    public void EncodeTextAppliesSelectedAsciiAllowlist(
        string text,
        PercentEncodeSet encodeSet,
        string expected)
    {
        PercentEncodeResult result = PercentEncoderCore.EncodeText(
            text,
            encodeSet,
            100,
            UriComponent.Input,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void EncodeTextUsesStrictUtf8ForAllScalarLengths()
    {
        string text = "A\u00a2\u20ac\ud83d\ude00";

        PercentEncodeResult result = PercentEncoderCore.EncodeText(
            text,
            PercentEncodeSet.Unreserved,
            40,
            UriComponent.Path,
            0);

        Assert.That(result.Text, Is.EqualTo("A%C2%A2%E2%82%AC%F0%9F%98%80"));
    }

    [Test]
    public void TextAndSpanEntryPointsProduceIdenticalResults()
    {
        string text = "path/\u00e4?x";

        PercentEncodeResult fromText = PercentEncoderCore.EncodeText(
            text,
            PercentEncodeSet.Path,
            100,
            UriComponent.Path,
            5);
        PercentEncodeResult fromSpan = PercentEncoderCore.EncodeSpan(
            text.AsSpan(),
            PercentEncodeSet.Path,
            100,
            UriComponent.Path,
            5);

        Assert.Multiple(() =>
        {
            Assert.That(fromText.Success, Is.True);
            Assert.That(fromSpan.Success, Is.True);
            Assert.That(fromText.Text, Is.EqualTo("path/%C3%A4%3Fx"));
            Assert.That(fromSpan.Text, Is.EqualTo(fromText.Text));
        });
    }

    [Test]
    public void MalformedUtf16ReturnsExactAbsoluteDiagnosticAndNoText()
    {
        string text = new string(new char[] { 'A', (char)55296, 'B' });

        PercentEncodeResult result = PercentEncoderCore.EncodeText(
            text,
            PercentEncodeSet.Unreserved,
            100,
            UriComponent.Fragment,
            20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.PercentEncoding));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidUnicode));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Fragment));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(21));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(1));
        });
    }

    [Test]
    public void MalformedUtf16TakesPrecedenceOverOutputLimit()
    {
        string text = new string(new char[] { (char)57343 });

        PercentEncodeResult result = PercentEncoderCore.EncodeText(
            text,
            PercentEncodeSet.Unreserved,
            0,
            UriComponent.Query,
            3);

        Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidUnicode));
    }

    [Test]
    public void OutputLimitReportsCompleteOverageAndReturnsNoPartialText()
    {
        PercentEncodeResult text = PercentEncoderCore.EncodeText(
            "A ",
            PercentEncodeSet.Unreserved,
            3,
            UriComponent.Path,
            0);
        PercentEncodeResult bytes = PercentEncoderCore.EncodeBytes(
            new byte[] { 0, 255 },
            PercentEncodeSet.Unreserved,
            4);

        AssertLimitFailure(text, 3, 1);
        AssertLimitFailure(bytes, 4, 2);
    }

    [Test]
    public void ExactAndEmptyOutputLimitsSucceed()
    {
        PercentEncodeResult exact = PercentEncoderCore.EncodeText(
            "A ",
            PercentEncodeSet.Unreserved,
            4,
            UriComponent.Path,
            0);
        PercentEncodeResult empty = PercentEncoderCore.EncodeBytes(
            new byte[0],
            PercentEncodeSet.Unreserved,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Text, Is.EqualTo("A%20"));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Text, Is.Empty);
        });
    }

    [Test]
    public void NegativeOutputLimitFailsDeterministicallyForEmptyInput()
    {
        PercentEncodeResult result = PercentEncoderCore.EncodeBytes(
            new byte[0],
            PercentEncodeSet.Unreserved,
            -1);

        AssertLimitFailure(result, 0, 0);
    }

    [Test]
    public void WritePercentByteWritesAtRequestedOffset()
    {
        char[] output = ".....".ToCharArray();

        int nextOffset = PercentEncoderCore.WritePercentByte(175, output, 1);

        Assert.Multiple(() =>
        {
            Assert.That(nextOffset, Is.EqualTo(4));
            Assert.That(new string(output), Is.EqualTo(".%AF."));
        });
    }

    private static void AssertLimitFailure(PercentEncodeResult result, int offset, int length)
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
