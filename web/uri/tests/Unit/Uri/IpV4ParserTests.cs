using System.Globalization;

namespace uri.Tests;

public class IpV4ParserTests
{
    [TestCase("0.0.0.0", 0, 0, 0, 0)]
    [TestCase("127.0.0.1", 127, 0, 0, 1)]
    [TestCase("192.168.1.254", 192, 168, 1, 254)]
    [TestCase("255.255.255.255", 255, 255, 255, 255)]
    [TestCase("1.20.100.200", 1, 20, 100, 200)]
    public void StrictDecimalDottedQuadsProduceFourAddressBytes(
        string text,
        int first,
        int second,
        int third,
        int fourth)
    {
        IpV4ParseResult result = IpV4Parser.Parse(text.AsSpan(), 14);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.AddressBytes, Is.EqualTo(new byte[]
            {
                (byte)first,
                (byte)second,
                (byte)third,
                (byte)fourth
            }));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void EveryDecimalOctetValueIsAcceptedInEveryPosition()
    {
        int value = 0;
        while (value <= 255)
        {
            int position = 0;
            while (position < 4)
            {
                int[] octets = new int[] { 17, 34, 51, 68 };
                octets[position] = value;
                string text = string.Join(
                    ".",
                    octets[0].ToString(CultureInfo.InvariantCulture),
                    octets[1].ToString(CultureInfo.InvariantCulture),
                    octets[2].ToString(CultureInfo.InvariantCulture),
                    octets[3].ToString(CultureInfo.InvariantCulture));

                IpV4ParseResult result = IpV4Parser.Parse(text.AsSpan(), 0);

                Assert.That(result.Success, Is.True, text);
                Assert.That(result.AddressBytes[position], Is.EqualTo(value), text);
                position = position + 1;
            }

            value = value + 1;
        }
    }

    [TestCase("")]
    [TestCase("1")]
    [TestCase("1.2")]
    [TestCase("1.2.3")]
    [TestCase("1.2.3.")]
    [TestCase(".1.2.3")]
    [TestCase("1..2.3")]
    [TestCase("1.2..3")]
    [TestCase("1.2.3..")]
    [TestCase("1.2.3.4.5")]
    public void AbbreviatedOrNonFourOctetFormsAreRejected(string text)
    {
        AssertRejected(text);
    }

    [TestCase("0x7f.0.0.1")]
    [TestCase("0X7F.0.0.1")]
    [TestCase("7f.0.0.1")]
    [TestCase("0.0x10.0.1")]
    [TestCase("0.0.0.0xffffffff")]
    public void HexadecimalFormsAreRejected(string text)
    {
        AssertRejected(text);
    }

    [TestCase("00.0.0.0")]
    [TestCase("01.0.0.0")]
    [TestCase("0177.0.0.1")]
    [TestCase("1.002.3.4")]
    [TestCase("1.2.3.000")]
    public void LeadingZeroAndOctalLikeFormsAreRejected(string text)
    {
        AssertRejected(text);
    }

    [TestCase("+1.2.3.4")]
    [TestCase("-1.2.3.4")]
    [TestCase("1.+2.3.4")]
    [TestCase("1.2.-3.4")]
    [TestCase("1.2.3.+4")]
    public void SignedFormsAreRejected(string text)
    {
        AssertRejected(text);
    }

    [TestCase(" 1.2.3.4")]
    [TestCase("1.2.3.4 ")]
    [TestCase("1 .2.3.4")]
    [TestCase("1. 2.3.4")]
    [TestCase("1.2\t.3.4")]
    [TestCase("1.2.3\r\n")]
    public void WhitespaceIsRejectedEverywhere(string text)
    {
        AssertRejected(text);
    }

    [TestCase("256.0.0.0")]
    [TestCase("999.0.0.0")]
    [TestCase("4294967295.0.0.0")]
    [TestCase("1.256.0.0")]
    [TestCase("1.2.999.0")]
    [TestCase("1.2.3.1000")]
    [TestCase("4294967297")]
    public void OverflowFormsAreRejectedWithoutIntegerWrapping(string text)
    {
        AssertRejected(text);
    }

    [TestCase("\uff11.2.3.4")]
    [TestCase("1.\u0662.3.4")]
    [TestCase("1.2.3.\u00b2")]
    public void OnlyAsciiDecimalDigitsAreAccepted(string text)
    {
        AssertRejected(text);
    }

    [TestCase("", 10, 0)]
    [TestCase("1.2.3", 15, 0)]
    [TestCase("1.2.3.", 16, 0)]
    [TestCase(".1.2.3", 10, 1)]
    [TestCase("1..2.3", 12, 1)]
    [TestCase("1.2.3.4.5", 17, 1)]
    [TestCase("01.2.3.4", 11, 1)]
    [TestCase("1.02.3.4", 13, 1)]
    [TestCase("256.0.0.0", 12, 1)]
    [TestCase("1234.0.0.0", 13, 1)]
    [TestCase("1.2.-3.4", 14, 1)]
    [TestCase("1.2.3.4 ", 17, 1)]
    public void FailuresReportTheExactOffendingCharacterOrMissingPosition(
        string text,
        int expectedOffset,
        int expectedLength)
    {
        IpV4ParseResult result = IpV4Parser.Parse(text.AsSpan(), 10);

        AssertFailure(result, expectedOffset, expectedLength);
    }

    [Test]
    public void FailureOffsetsUseRelativePositionsAndSaturateOverflow()
    {
        IpV4ParseResult relative = IpV4Parser.Parse("1.2.3".AsSpan(), -1);
        IpV4ParseResult saturated = IpV4Parser.Parse(
            "1.2.3.999".AsSpan(),
            int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(relative.Diagnostic, Is.Not.Null);
            Assert.That(relative.Diagnostic!.Range.Offset, Is.EqualTo(5));
            Assert.That(saturated.Diagnostic, Is.Not.Null);
            Assert.That(saturated.Diagnostic!.Range.Offset, Is.EqualTo(int.MaxValue));
        });
    }

    private static void AssertRejected(string text)
    {
        IpV4ParseResult result = IpV4Parser.Parse(text.AsSpan(), 0);

        Assert.That(result.Success, Is.False, text);
        Assert.That(result.AddressBytes, Is.Empty, text);
        Assert.That(result.Diagnostic, Is.Not.Null, text);
        Assert.That(
            result.Diagnostic!.Code,
            Is.EqualTo(UriDiagnosticCode.InvalidIpv4Address),
            text);
    }

    private static void AssertFailure(
        IpV4ParseResult result,
        int expectedOffset,
        int expectedLength)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.AddressBytes, Is.Empty);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidIpv4Address));
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Host));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(expectedOffset));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(expectedLength));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(DiagnosticFactory.InvalidIpv4AddressMessage));
        });
    }
}
