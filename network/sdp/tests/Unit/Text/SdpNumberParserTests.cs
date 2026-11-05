using System.Text;

namespace sdp.Tests.Unit.Text;

public class SdpNumberParserTests
{
    [Test]
    public void SignedDecimalParsers_AcceptInt64Boundaries()
    {
        SdpNumberParseResult characterMaximum = SdpNumberParser.ParseCharacterSignedDecimal("9223372036854775807".AsSpan());
        SdpNumberParseResult characterMinimum = SdpNumberParser.ParseCharacterSignedDecimal("-9223372036854775808".AsSpan());
        SdpNumberParseResult utf8Positive = SdpNumberParser.ParseUtf8SignedDecimal(Encoding.ASCII.GetBytes("+27"));
        SdpNumberParseResult utf8Negative = SdpNumberParser.ParseUtf8SignedDecimal(Encoding.ASCII.GetBytes("-27"));

        Assert.Multiple(() =>
        {
            Assert.That(characterMaximum.Success, Is.True);
            Assert.That(characterMaximum.SignedValue, Is.EqualTo(long.MaxValue));
            Assert.That(characterMaximum.DiagnosticCode, Is.Empty);

            Assert.That(characterMinimum.Success, Is.True);
            Assert.That(characterMinimum.SignedValue, Is.EqualTo(long.MinValue));
            Assert.That(characterMinimum.DiagnosticCode, Is.Empty);

            Assert.That(utf8Positive.Success, Is.True);
            Assert.That(utf8Positive.SignedValue, Is.EqualTo(27));
            Assert.That(utf8Negative.Success, Is.True);
            Assert.That(utf8Negative.SignedValue, Is.EqualTo(-27));
        });
    }

    [Test]
    public void SignedDecimalParsers_DistinguishInvalidInputFromOverflow()
    {
        SdpNumberParseResult positiveOverflow = SdpNumberParser.ParseCharacterSignedDecimal("9223372036854775808".AsSpan());
        SdpNumberParseResult negativeOverflow = SdpNumberParser.ParseUtf8SignedDecimal(Encoding.ASCII.GetBytes("-9223372036854775809"));
        SdpNumberParseResult invalidCharacters = SdpNumberParser.ParseCharacterSignedDecimal("12x".AsSpan());
        SdpNumberParseResult empty = SdpNumberParser.ParseUtf8SignedDecimal(new byte[0]);
        SdpNumberParseResult signOnly = SdpNumberParser.ParseCharacterSignedDecimal("-".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(positiveOverflow.Success, Is.False);
            Assert.That(positiveOverflow.Overflow, Is.True);
            Assert.That(positiveOverflow.Invalid, Is.False);
            Assert.That(positiveOverflow.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));

            Assert.That(negativeOverflow.Success, Is.False);
            Assert.That(negativeOverflow.Overflow, Is.True);
            Assert.That(negativeOverflow.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));

            Assert.That(invalidCharacters.Success, Is.False);
            Assert.That(invalidCharacters.Invalid, Is.True);
            Assert.That(invalidCharacters.Overflow, Is.False);
            Assert.That(invalidCharacters.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));

            Assert.That(empty.Invalid, Is.True);
            Assert.That(empty.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(signOnly.Invalid, Is.True);
        });
    }

    [Test]
    public void UnsignedAndNtpParsers_AcceptUInt64BoundaryAndRejectOtherForms()
    {
        string maximum = "18446744073709551615";
        string overflow = "18446744073709551616";

        SdpNumberParseResult characterMaximum = SdpNumberParser.ParseCharacterUnsignedDecimal(maximum.AsSpan());
        SdpNumberParseResult utf8Maximum = SdpNumberParser.ParseUtf8UnsignedDecimal(Encoding.ASCII.GetBytes(maximum));
        SdpNumberParseResult characterNtp = SdpNumberParser.ParseCharacterNtpValue(maximum.AsSpan());
        SdpNumberParseResult utf8Ntp = SdpNumberParser.ParseUtf8NtpValue(Encoding.ASCII.GetBytes(maximum));
        SdpNumberParseResult characterOverflow = SdpNumberParser.ParseCharacterNtpValue(overflow.AsSpan());
        SdpNumberParseResult utf8Overflow = SdpNumberParser.ParseUtf8UnsignedDecimal(Encoding.ASCII.GetBytes(overflow));
        SdpNumberParseResult negative = SdpNumberParser.ParseCharacterUnsignedDecimal("-1".AsSpan());
        SdpNumberParseResult plus = SdpNumberParser.ParseUtf8UnsignedDecimal(Encoding.ASCII.GetBytes("+1"));

        Assert.Multiple(() =>
        {
            Assert.That(characterMaximum.Success, Is.True);
            Assert.That(characterMaximum.UnsignedValue, Is.EqualTo(ulong.MaxValue));
            Assert.That(utf8Maximum.Success, Is.True);
            Assert.That(utf8Maximum.UnsignedValue, Is.EqualTo(ulong.MaxValue));
            Assert.That(characterNtp.Success, Is.True);
            Assert.That(characterNtp.UnsignedValue, Is.EqualTo(ulong.MaxValue));
            Assert.That(utf8Ntp.Success, Is.True);
            Assert.That(utf8Ntp.UnsignedValue, Is.EqualTo(ulong.MaxValue));

            Assert.That(characterOverflow.Overflow, Is.True);
            Assert.That(characterOverflow.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));
            Assert.That(utf8Overflow.Overflow, Is.True);
            Assert.That(utf8Overflow.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));

            Assert.That(negative.Invalid, Is.True);
            Assert.That(negative.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(plus.Invalid, Is.True);
            Assert.That(plus.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
        });
    }

    [Test]
    public void PortAndPayloadParsers_EnforceProtocolRanges()
    {
        SdpNumberParseResult zeroPort = SdpNumberParser.ParseCharacterPort("0".AsSpan());
        SdpNumberParseResult maximumPort = SdpNumberParser.ParseCharacterPort("65535".AsSpan());
        SdpNumberParseResult utf8Port = SdpNumberParser.ParseUtf8Port(Encoding.ASCII.GetBytes("443"));
        SdpNumberParseResult portOverflow = SdpNumberParser.ParseUtf8Port(Encoding.ASCII.GetBytes("65536"));
        SdpNumberParseResult maximumPayload = SdpNumberParser.ParseCharacterPayloadType("127".AsSpan());
        SdpNumberParseResult utf8Payload = SdpNumberParser.ParseUtf8PayloadType(Encoding.ASCII.GetBytes("96"));
        SdpNumberParseResult payloadOverflow = SdpNumberParser.ParseCharacterPayloadType("128".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(zeroPort.Success, Is.True);
            Assert.That(zeroPort.Int32Value, Is.EqualTo(0));
            Assert.That(maximumPort.Success, Is.True);
            Assert.That(maximumPort.Int32Value, Is.EqualTo(65535));
            Assert.That(utf8Port.Success, Is.True);
            Assert.That(utf8Port.Int32Value, Is.EqualTo(443));
            Assert.That(portOverflow.Success, Is.False);
            Assert.That(portOverflow.Overflow, Is.True);
            Assert.That(portOverflow.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));

            Assert.That(maximumPayload.Success, Is.True);
            Assert.That(maximumPayload.Int32Value, Is.EqualTo(127));
            Assert.That(utf8Payload.Success, Is.True);
            Assert.That(utf8Payload.Int32Value, Is.EqualTo(96));
            Assert.That(payloadOverflow.Success, Is.False);
            Assert.That(payloadOverflow.Overflow, Is.True);
            Assert.That(payloadOverflow.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));
        });
    }

    [Test]
    public void SsrcAndPriorityParsers_EnforceUInt32Range()
    {
        string maximum = "4294967295";
        string overflow = "4294967296";

        SdpNumberParseResult characterSsrc = SdpNumberParser.ParseCharacterSsrc(maximum.AsSpan());
        SdpNumberParseResult utf8Ssrc = SdpNumberParser.ParseUtf8Ssrc(Encoding.ASCII.GetBytes(maximum));
        SdpNumberParseResult characterPriority = SdpNumberParser.ParseCharacterPriority(maximum.AsSpan());
        SdpNumberParseResult utf8Priority = SdpNumberParser.ParseUtf8Priority(Encoding.ASCII.GetBytes(maximum));
        SdpNumberParseResult ssrcOverflow = SdpNumberParser.ParseCharacterSsrc(overflow.AsSpan());
        SdpNumberParseResult priorityOverflow = SdpNumberParser.ParseUtf8Priority(Encoding.ASCII.GetBytes(overflow));

        Assert.Multiple(() =>
        {
            Assert.That(characterSsrc.Success, Is.True);
            Assert.That(characterSsrc.UInt32Value, Is.EqualTo(uint.MaxValue));
            Assert.That(utf8Ssrc.Success, Is.True);
            Assert.That(utf8Ssrc.UInt32Value, Is.EqualTo(uint.MaxValue));
            Assert.That(characterPriority.Success, Is.True);
            Assert.That(characterPriority.UInt32Value, Is.EqualTo(uint.MaxValue));
            Assert.That(utf8Priority.Success, Is.True);
            Assert.That(utf8Priority.UInt32Value, Is.EqualTo(uint.MaxValue));

            Assert.That(ssrcOverflow.Success, Is.False);
            Assert.That(ssrcOverflow.Overflow, Is.True);
            Assert.That(ssrcOverflow.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));
            Assert.That(priorityOverflow.Success, Is.False);
            Assert.That(priorityOverflow.Overflow, Is.True);
            Assert.That(priorityOverflow.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));
        });
    }
}
