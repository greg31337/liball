namespace uri.Tests;

public class Ipv6ZoneIdentifierTests
{
    [TestCase("fe80::1%eth0")]
    [TestCase("fe80::1%25eth0")]
    [TestCase("fe80::1%1")]
    [TestCase("::%zone")]
    [TestCase("::ffff:192.0.2.1%25interface")]
    public void CompleteIpv6AddressWithZoneSuffixIsRejectedExplicitly(string text)
    {
        int markerOffset = text.IndexOf('%');

        IpV6ParseResult result = IpV6Parser.Parse(text.AsSpan(), 20);

        AssertZoneFailure(result, markerOffset + 20);
    }

    [TestCase("[::1%zone")]
    [TestCase("12345::%zone")]
    [TestCase("::ffff:999.0.0.1%zone")]
    [TestCase("%zone")]
    public void EarlierIpv6SyntaxErrorsTakePrecedenceOverZoneDetection(string text)
    {
        IpV6ParseResult result = IpV6Parser.Parse(text.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidIpv6Address));
        });
    }

    [Test]
    public void PercentCharacterInIpvFutureIsNotTreatedAsAnIpv6Zone()
    {
        IpFutureParseResult result = IpFutureParser.Parse("v1.address%zone".AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(
                result.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidIpvFutureAddress));
        });
    }

    [Test]
    public void ZoneDiagnosticOffsetsUseRelativePositionsAndSaturateOverflow()
    {
        IpV6ParseResult relative = IpV6Parser.Parse("::1%zone".AsSpan(), -1);
        IpV6ParseResult saturated = IpV6Parser.Parse(
            "::1%zone".AsSpan(),
            int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(relative.Diagnostic, Is.Not.Null);
            Assert.That(relative.Diagnostic!.Range.Offset, Is.EqualTo(3));
            Assert.That(saturated.Diagnostic, Is.Not.Null);
            Assert.That(saturated.Diagnostic!.Range.Offset, Is.EqualTo(int.MaxValue));
        });
    }

    private static void AssertZoneFailure(IpV6ParseResult result, int expectedOffset)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.AddressBytes, Is.Empty);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.ZoneIdentifierNotAllowed));
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Host));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(expectedOffset));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(1));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(DiagnosticFactory.ZoneIdentifierNotAllowedMessage));
        });
    }
}
