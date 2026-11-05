using System.Text;

namespace sdp.Tests.Conformance.Rfc8866;

public class SdpLowLevelConformanceTests
{
    [Test]
    public void BomUnicodeAndMixedLineEndings_PreserveExactLineBoundaries()
    {
        string body = "s=Résumé\r\n\na=recvonly\rt=0 0";
        byte[] input = Encoding.UTF8.GetBytes("\uFEFF" + body);

        SdpUtf8DecodeResult decoded = SdpUtf8Decoder.DecodeUtf8(input.AsSpan(), true);
        SdpLineReadResult sessionName = SdpLineReader.ReadUtf8Line(input.AsSpan(), 3, 0, 1, 100, 10);
        SdpLineReadResult empty = SdpLineReader.ReadUtf8Line(
            input.AsSpan(),
            sessionName.NextOffset,
            sessionName.CharactersConsumed,
            2,
            100,
            10);
        SdpLineReadResult attribute = SdpLineReader.ReadUtf8Line(
            input.AsSpan(),
            empty.NextOffset,
            sessionName.CharactersConsumed + empty.CharactersConsumed,
            3,
            100,
            10);
        SdpLineReadResult timing = SdpLineReader.ReadUtf8Line(
            input.AsSpan(),
            attribute.NextOffset,
            sessionName.CharactersConsumed + empty.CharactersConsumed + attribute.CharactersConsumed,
            4,
            100,
            10);
        SdpLineReadResult terminal = SdpLineReader.ReadUtf8Line(
            input.AsSpan(),
            timing.NextOffset,
            sessionName.CharactersConsumed + empty.CharactersConsumed + attribute.CharactersConsumed + timing.CharactersConsumed,
            5,
            100,
            10);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.HasByteOrderMark, Is.True);
            Assert.That(decoded.Text, Is.EqualTo(body));

            Assert.That(sessionName.Line.Text, Is.EqualTo("s=Résumé"));
            Assert.That(sessionName.Line.LineEnding, Is.EqualTo(SdpLineEnding.CrLf));
            Assert.That(sessionName.Line.Location.ByteOffset, Is.EqualTo(3));
            Assert.That(sessionName.Line.Location.CharacterOffset, Is.EqualTo(0));

            Assert.That(empty.Line.Text, Is.Empty);
            Assert.That(empty.Line.LineEnding, Is.EqualTo(SdpLineEnding.Lf));
            Assert.That(empty.Line.Location.LineNumber, Is.EqualTo(2));

            Assert.That(attribute.Line.Text, Is.EqualTo("a=recvonly"));
            Assert.That(attribute.Line.LineEnding, Is.EqualTo(SdpLineEnding.Cr));
            Assert.That(attribute.Line.Location.LineNumber, Is.EqualTo(3));

            Assert.That(timing.Line.Text, Is.EqualTo("t=0 0"));
            Assert.That(timing.Line.LineEnding, Is.EqualTo(SdpLineEnding.None));
            Assert.That(timing.EndOfInput, Is.True);
            Assert.That(terminal.HasLine, Is.False);
            Assert.That(terminal.EndOfInput, Is.True);
        });
    }

    [Test]
    public void Utf8ScalarBoundarySequences_DecodeWithoutNormalization()
    {
        byte[] input = new byte[]
        {
            194, 160,
            224, 160, 128,
            237, 159, 191,
            238, 128, 128,
            240, 144, 128, 128,
            244, 143, 191, 191
        };

        SdpUtf8DecodeResult result = SdpUtf8Decoder.DecodeUtf8(input.AsSpan(), false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.InvalidUtf8, Is.False);
            Assert.That(result.BytesConsumed, Is.EqualTo(input.Length));
            Assert.That(result.CharacterCount, Is.EqualTo(8));
            Assert.That(result.Text.Length, Is.EqualTo(8));
            Assert.That(Encoding.UTF8.GetBytes(result.Text), Is.EqualTo(input));
        });
    }

    [Test]
    public void TokenSlices_FlowIntoProtocolNumericParsersAtBoundaries()
    {
        string input = "65535 127 4294967295 18446744073709551615";
        int offset = 0;

        SdpTokenReadResult portToken = SdpTokenReader.ReadCharacterDecimalToken(input.AsSpan(), offset, 20);
        SdpNumberParseResult port = SdpNumberParser.ParseCharacterPort(input.AsSpan(portToken.StartOffset, portToken.Length));
        offset = SdpTokenReader.SkipCharacterSpaces(input.AsSpan(), portToken.NextOffset);

        SdpTokenReadResult payloadToken = SdpTokenReader.ReadCharacterDecimalToken(input.AsSpan(), offset, 20);
        SdpNumberParseResult payload = SdpNumberParser.ParseCharacterPayloadType(input.AsSpan(payloadToken.StartOffset, payloadToken.Length));
        offset = SdpTokenReader.SkipCharacterSpaces(input.AsSpan(), payloadToken.NextOffset);

        SdpTokenReadResult ssrcToken = SdpTokenReader.ReadCharacterDecimalToken(input.AsSpan(), offset, 20);
        SdpNumberParseResult ssrc = SdpNumberParser.ParseCharacterSsrc(input.AsSpan(ssrcToken.StartOffset, ssrcToken.Length));
        offset = SdpTokenReader.SkipCharacterSpaces(input.AsSpan(), ssrcToken.NextOffset);

        SdpTokenReadResult ntpToken = SdpTokenReader.ReadCharacterDecimalToken(input.AsSpan(), offset, 20);
        SdpNumberParseResult ntp = SdpNumberParser.ParseCharacterNtpValue(input.AsSpan(ntpToken.StartOffset, ntpToken.Length));

        Assert.Multiple(() =>
        {
            Assert.That(portToken.Success, Is.True);
            Assert.That(port.Success, Is.True);
            Assert.That(port.Int32Value, Is.EqualTo(65535));
            Assert.That(payload.Success, Is.True);
            Assert.That(payload.Int32Value, Is.EqualTo(127));
            Assert.That(ssrc.Success, Is.True);
            Assert.That(ssrc.UInt32Value, Is.EqualTo(uint.MaxValue));
            Assert.That(ntp.Success, Is.True);
            Assert.That(ntp.UnsignedValue, Is.EqualTo(ulong.MaxValue));
            Assert.That(ntpToken.NextOffset, Is.EqualTo(input.Length));
        });
    }

    [Test]
    public void TypedTimeSlices_ParseAndFormatEveryRfcUnit()
    {
        string input = "7d 1h 30m 45s 0";
        string[] expected = new string[] { "7d", "1h", "30m", "45s", "0" };
        int offset = 0;

        for (int index = 0; index < expected.Length; index++)
        {
            SdpTokenReadResult token = SdpTokenReader.ReadCharacterNonWhitespaceToken(input.AsSpan(), offset, 10);
            SdpTypedTimeParseResult parsed = SdpTypedTimeParser.ParseCharacterTypedTime(input.AsSpan(token.StartOffset, token.Length));
            SdpTypedTimeFormatResult formatted = SdpTypedTimeParser.FormatCanonicalTypedTime(parsed.Value);

            Assert.Multiple(() =>
            {
                Assert.That(token.Success, Is.True);
                Assert.That(parsed.Success, Is.True);
                Assert.That(formatted.Success, Is.True);
                Assert.That(formatted.Text, Is.EqualTo(expected[index]));
            });

            offset = SdpTokenReader.SkipCharacterSpaces(input.AsSpan(), token.NextOffset);
        }

        Assert.That(offset, Is.EqualTo(input.Length));
    }

    [Test]
    public void InvalidInputLimitsAndRecovery_ReturnStableDiagnosticsWithoutLosingBoundary()
    {
        SdpUtf8DecodeResult invalidUtf8 = SdpUtf8Decoder.DecodeUtf8(new byte[] { 226, 40, 161 }, false);
        SdpUtf8DecodeResult nul = SdpUtf8Decoder.DecodeUtf8(new byte[] { 65, 0 }, false);
        SdpUtf8DecodeResult control = SdpUtf8Decoder.DecodeUtf8(new byte[] { 65, 9 }, false);
        SdpLineReadResult lengthBoundary = SdpLineReader.ReadCharacterLine("1234\nm=x".AsSpan(), 0, 1, 4, 2);
        SdpLineReadResult overlong = SdpLineReader.ReadCharacterLine("12345\nm=x".AsSpan(), 0, 1, 4, 2);
        SdpNumberParseResult numberOverflow = SdpNumberParser.ParseCharacterUnsignedDecimal("18446744073709551616".AsSpan());
        SdpTypedTimeParseResult typedTimeOverflow = SdpTypedTimeParser.ParseCharacterTypedTime("9223372036854775808d".AsSpan());
        SdpRecoveryResult recovery = SdpRecovery.RecoverCharacterLine("broken\r\nm=x".AsSpan(), 0, 1, 20, 2, true);

        Assert.Multiple(() =>
        {
            Assert.That(invalidUtf8.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidUtf8));
            Assert.That(nul.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNulCharacterNotAllowed));
            Assert.That(control.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseControlCharacterNotAllowed));

            Assert.That(lengthBoundary.LineLengthExceeded, Is.False);
            Assert.That(lengthBoundary.Line.Text, Is.EqualTo("1234"));
            Assert.That(overlong.LineLengthExceeded, Is.True);
            Assert.That(overlong.Line.Text, Is.Empty);

            Assert.That(numberOverflow.Overflow, Is.True);
            Assert.That(numberOverflow.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));
            Assert.That(typedTimeOverflow.Overflow, Is.True);
            Assert.That(typedTimeOverflow.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));

            Assert.That(recovery.Recovered, Is.True);
            Assert.That(recovery.Retained, Is.True);
            Assert.That(recovery.NextOffset, Is.EqualTo("broken\r\n".Length));
            Assert.That(recovery.NextLineIsMediaBoundary, Is.True);
            Assert.That(recovery.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseRecoveredMalformedLine));
        });
    }
}
