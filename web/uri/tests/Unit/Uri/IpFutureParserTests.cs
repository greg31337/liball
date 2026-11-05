namespace uri.Tests;

public class IpFutureParserTests
{
    [TestCase("v1.a", "1", "a")]
    [TestCase("V1.a", "1", "a")]
    [TestCase("vfF.A:b", "fF", "A:b")]
    [TestCase("VABCDEF.x-y_z~!$&'()*+,;=:", "ABCDEF", "x-y_z~!$&'()*+,;=:")]
    [TestCase("v0000000000000000..", "0000000000000000", ".")]
    [TestCase("v0.:", "0", ":")]
    public void ValidIpvFutureTextPreservesVersionAndAddressBoundaries(
        string text,
        string version,
        string address)
    {
        IpFutureParseResult result = IpFutureParser.Parse(text.AsSpan(), 40);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(
                text.Substring(result.VersionOffset, result.VersionLength),
                Is.EqualTo(version));
            Assert.That(
                text.Substring(result.AddressOffset, result.AddressLength),
                Is.EqualTo(address));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void EveryAllowedAddressCharacterIsAccepted()
    {
        string allowed =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~!$&'()*+,;=:";
        int index = 0;
        while (index < allowed.Length)
        {
            string text = "vA." + allowed[index];

            IpFutureParseResult result = IpFutureParser.Parse(text.AsSpan(), 0);

            Assert.That(result.Success, Is.True, text);
            index = index + 1;
        }
    }

    [Test]
    public void EveryOtherAsciiAddressCharacterIsRejected()
    {
        string allowed =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~!$&'()*+,;=:";
        int value = 0;
        while (value <= 127)
        {
            char character = (char)value;
            if (allowed.IndexOf(character) < 0)
            {
                string text = "v1." + character;

                IpFutureParseResult result = IpFutureParser.Parse(text.AsSpan(), 0);

                Assert.That(result.Success, Is.False, value.ToString());
                Assert.That(result.Diagnostic, Is.Not.Null, value.ToString());
                Assert.That(
                    result.Diagnostic!.Range.Offset,
                    Is.EqualTo(3),
                    value.ToString());
            }

            value = value + 1;
        }
    }

    [Test]
    public void VersionLengthIsNotLimitedByIntegerStorage()
    {
        string version = new string('A', 4096);
        string text = "v" + version + ".address";

        IpFutureParseResult result = IpFutureParser.Parse(text.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.VersionOffset, Is.EqualTo(1));
            Assert.That(result.VersionLength, Is.EqualTo(version.Length));
            Assert.That(result.AddressOffset, Is.EqualTo(version.Length + 2));
            Assert.That(result.AddressLength, Is.EqualTo(7));
        });
    }

    [TestCase("")]
    [TestCase("v")]
    [TestCase("v.")]
    [TestCase("v1")]
    [TestCase("v1.")]
    [TestCase("x1.address")]
    [TestCase("vv1.address")]
    [TestCase("vG.address")]
    [TestCase("v1x.address")]
    [TestCase("v1.address/path")]
    [TestCase("v1.address?query")]
    [TestCase("v1.address#fragment")]
    [TestCase("v1.user@host")]
    [TestCase("v1.%41")]
    [TestCase("v1.\u00e9")]
    public void MissingElementsAndNonGrammarCharactersAreRejected(string text)
    {
        IpFutureParseResult result = IpFutureParser.Parse(text.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, text);
            Assert.That(result.VersionOffset, Is.Zero, text);
            Assert.That(result.VersionLength, Is.Zero, text);
            Assert.That(result.AddressOffset, Is.Zero, text);
            Assert.That(result.AddressLength, Is.Zero, text);
            Assert.That(result.Diagnostic, Is.Not.Null, text);
            Assert.That(
                result.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidIpvFutureAddress),
                text);
        });
    }

    [TestCase("", 10, 0)]
    [TestCase("x1.a", 10, 1)]
    [TestCase("v", 11, 0)]
    [TestCase("v.a", 11, 1)]
    [TestCase("vG.a", 11, 1)]
    [TestCase("v1", 12, 0)]
    [TestCase("v1x.a", 12, 1)]
    [TestCase("v1.", 13, 0)]
    [TestCase("v1.a/b", 14, 1)]
    [TestCase("v1.%41", 13, 1)]
    public void FailuresReportTheExactOffendingOrMissingPosition(
        string text,
        int expectedOffset,
        int expectedLength)
    {
        IpFutureParseResult result = IpFutureParser.Parse(text.AsSpan(), 10);

        AssertFailure(result, expectedOffset, expectedLength);
    }

    [Test]
    public void FailureOffsetsUseRelativePositionsAndSaturateOverflow()
    {
        IpFutureParseResult relative = IpFutureParser.Parse("v1.%".AsSpan(), -1);
        IpFutureParseResult saturated = IpFutureParser.Parse(
            "v1.%".AsSpan(),
            int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(relative.Diagnostic, Is.Not.Null);
            Assert.That(relative.Diagnostic!.Range.Offset, Is.EqualTo(3));
            Assert.That(saturated.Diagnostic, Is.Not.Null);
            Assert.That(saturated.Diagnostic!.Range.Offset, Is.EqualTo(int.MaxValue));
        });
    }

    private static void AssertFailure(
        IpFutureParseResult result,
        int expectedOffset,
        int expectedLength)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidIpvFutureAddress));
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Host));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(expectedOffset));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(expectedLength));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(DiagnosticFactory.InvalidIpvFutureAddressMessage));
        });
    }
}
