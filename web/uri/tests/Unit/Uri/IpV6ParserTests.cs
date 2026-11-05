namespace uri.Tests;

public class IpV6ParserTests
{
    [TestCase("1:2:3:4:5:6:7:8", "00010002000300040005000600070008")]
    [TestCase("::1:2:3:4:5:6:7", "00000001000200030004000500060007")]
    [TestCase("1::2:3:4:5:6:7", "00010000000200030004000500060007")]
    [TestCase("1:2::3:4:5:6:7", "00010002000000030004000500060007")]
    [TestCase("1:2:3::4:5:6:7", "00010002000300000004000500060007")]
    [TestCase("1:2:3:4::5:6:7", "00010002000300040000000500060007")]
    [TestCase("1:2:3:4:5::6:7", "00010002000300040005000000060007")]
    [TestCase("1:2:3:4:5:6::7", "00010002000300040005000600000007")]
    [TestCase("1:2:3:4:5:6:7::", "00010002000300040005000600070000")]
    public void EveryRfc3986Ipv6AlternativeExpandsToSixteenBytes(
        string text,
        string expectedHex)
    {
        AssertSuccess(text, expectedHex);
    }

    [TestCase("::", "00000000000000000000000000000000")]
    [TestCase("::1", "00000000000000000000000000000001")]
    [TestCase("1::", "00010000000000000000000000000000")]
    [TestCase("2001:db8::1", "20010DB8000000000000000000000001")]
    [TestCase("2001:db8:0:1::1", "20010DB8000000010000000000000001")]
    [TestCase("ABCD:ef01:2345:6789:aBcD:Ef01:2345:6789", "ABCDEf0123456789ABCDEF0123456789")]
    [TestCase("0001:0020:0300:4000:0000:000A:00B0:0C00", "00010020030040000000000A00B00C00")]
    public void HexGroupsAcceptAllLengthsAndAsciiHexCases(
        string text,
        string expectedHex)
    {
        AssertSuccess(text, expectedHex);
    }

    [TestCase("1:2:3:4:5:6:192.0.2.128", "000100020003000400050006C0000280")]
    [TestCase("::192.0.2.128", "000000000000000000000000C0000280")]
    [TestCase("::ffff:192.0.2.128", "00000000000000000000FFFFC0000280")]
    [TestCase("1:2:3:4:5::192.0.2.128", "000100020003000400050000C0000280")]
    [TestCase("1::192.0.2.128", "000100000000000000000000C0000280")]
    [TestCase("0:0:0:0:0:ffff:255.255.255.255", "00000000000000000000FFFFFFFFFFFF")]
    public void EmbeddedIpv4ConsumesTheFinalThirtyTwoBits(
        string text,
        string expectedHex)
    {
        AssertSuccess(text, expectedHex);
    }

    [TestCase("")]
    [TestCase(":")]
    [TestCase(":1:2:3:4:5:6:7")]
    [TestCase("1:2:3:4:5:6:7")]
    [TestCase("1:2:3:4:5:6:7:")]
    [TestCase("1:2:3:4:5:6:7:8:9")]
    [TestCase("1:2:3:4:5:6:7:8::")]
    [TestCase("1:2:3:4:5:6:7::8")]
    public void GroupCountMustRepresentExactlyOneHundredTwentyEightBits(string text)
    {
        AssertRejected(text);
    }

    [TestCase("1::2::3")]
    [TestCase(":::1")]
    [TestCase("1:::2")]
    [TestCase("1::::2")]
    [TestCase("1:2:::")]
    [TestCase("1:2:3:4:5:6:7:8::9")]
    public void ZeroCompressionOccursAtMostOnceAndMustReplaceAGroup(string text)
    {
        AssertRejected(text);
    }

    [TestCase("12345:2:3:4:5:6:7:8")]
    [TestCase("1:00000:3:4:5:6:7:8")]
    [TestCase("1:2:3:4:5:6:7:fffff")]
    [TestCase("g:2:3:4:5:6:7:8")]
    [TestCase("1:2:3:4:5:6:7:z")]
    [TestCase("1:2:3:4:5:6:7:+8")]
    [TestCase("1:2:3:4:5:6:7: 8")]
    [TestCase("1:2:3:4:5:6:7:\uff18")]
    [TestCase("[::1]")]
    public void HexGroupsRejectExcessDigitsAndNonGrammarCharacters(string text)
    {
        AssertRejected(text);
    }

    [TestCase("::ffff:192.0.2")]
    [TestCase("::ffff:192.0.2.1.5")]
    [TestCase("::ffff:192.0.02.1")]
    [TestCase("::ffff:256.0.0.1")]
    [TestCase("::ffff:0xC0.0.2.1")]
    [TestCase("::ffff:+192.0.2.1")]
    [TestCase("::ffff:192.0.2.1 ")]
    [TestCase("1:2:3:4:5:192.0.2.1")]
    [TestCase("1:2:3:4:5:6:7:192.0.2.1")]
    [TestCase("1:2:3:4:5:6:192.0.2.1:7")]
    public void EmbeddedIpv4UsesTheStrictIpv4GrammarAndMustBeFinal(string text)
    {
        AssertRejected(text);
    }

    [TestCase("", 10, 0)]
    [TestCase(":1:2:3:4:5:6:7", 10, 1)]
    [TestCase("1:2:3:4:5:6:7", 23, 0)]
    [TestCase("1:2:3:4:5:6:7:", 24, 0)]
    [TestCase("1:2:3:4:5:6:7:8:9", 26, 1)]
    [TestCase("1:2:3:4:5:6:7::8", 23, 2)]
    [TestCase("1::2::3", 14, 2)]
    [TestCase("12345:2:3:4:5:6:7:8", 14, 1)]
    [TestCase("1:2:3:4:5:6:7:g", 24, 1)]
    [TestCase("::ffff:256.0.0.1", 19, 1)]
    [TestCase("::ffff:192.0.02.1", 24, 1)]
    [TestCase("::ffff:192.0.2", 24, 0)]
    [TestCase("1:2:3:4:5:6:192.0.2.1:7", 31, 1)]
    public void InvalidGroupsReportTheExactOffendingOrMissingPosition(
        string text,
        int expectedOffset,
        int expectedLength)
    {
        IpV6ParseResult result = IpV6Parser.Parse(text.AsSpan(), 10);

        AssertFailure(result, expectedOffset, expectedLength);
    }

    [Test]
    public void FailureOffsetsUseRelativePositionsAndSaturateOverflow()
    {
        IpV6ParseResult relative = IpV6Parser.Parse("1:2:3".AsSpan(), -1);
        IpV6ParseResult saturated = IpV6Parser.Parse(
            "1:2:3:4:5:6:7:g".AsSpan(),
            int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(relative.Diagnostic, Is.Not.Null);
            Assert.That(relative.Diagnostic!.Range.Offset, Is.EqualTo(5));
            Assert.That(saturated.Diagnostic, Is.Not.Null);
            Assert.That(saturated.Diagnostic!.Range.Offset, Is.EqualTo(int.MaxValue));
        });
    }

    private static void AssertSuccess(string text, string expectedHex)
    {
        IpV6ParseResult result = IpV6Parser.Parse(text.AsSpan(), 25);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.AddressBytes, Has.Length.EqualTo(16), text);
            Assert.That(result.AddressBytes, Is.EqualTo(Convert.FromHexString(expectedHex)), text);
            Assert.That(result.Diagnostic, Is.Null, text);
        });
    }

    private static void AssertRejected(string text)
    {
        IpV6ParseResult result = IpV6Parser.Parse(text.AsSpan(), 0);

        Assert.That(result.Success, Is.False, text);
        Assert.That(result.AddressBytes, Is.Empty, text);
        Assert.That(result.Diagnostic, Is.Not.Null, text);
        Assert.That(
            result.Diagnostic!.Code,
            Is.EqualTo(UriDiagnosticCode.InvalidIpv6Address),
            text);
    }

    private static void AssertFailure(
        IpV6ParseResult result,
        int expectedOffset,
        int expectedLength)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.AddressBytes, Is.Empty);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidIpv6Address));
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Host));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(expectedOffset));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(expectedLength));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(DiagnosticFactory.InvalidIpv6AddressMessage));
        });
    }
}
