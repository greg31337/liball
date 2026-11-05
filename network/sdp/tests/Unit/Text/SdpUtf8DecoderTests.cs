using System.Text;

namespace sdp.Tests.Unit.Text;

public class SdpUtf8DecoderTests
{
    [Test]
    public void DecodeUtf8_DecodesInternationalTextAndTracksUtf16Characters()
    {
        string text = "s=Résumé 世界 \U0001F600\r\n";
        byte[] input = Encoding.UTF8.GetBytes(text);

        SdpUtf8DecodeResult result = SdpUtf8Decoder.DecodeUtf8(input.AsSpan(), false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(text));
            Assert.That(result.CharacterCount, Is.EqualTo(text.Length));
            Assert.That(result.BytesConsumed, Is.EqualTo(input.Length));
            Assert.That(result.ByteOffset, Is.EqualTo(-1));
            Assert.That(result.CharacterOffset, Is.EqualTo(-1));
            Assert.That(result.DiagnosticCode, Is.Empty);
        });
    }

    [Test]
    public void DecodeUtf8_EnforcesLeadingByteOrderMarkPolicy()
    {
        byte[] input = Encoding.UTF8.GetBytes("\uFEFFv=0");

        SdpUtf8DecodeResult rejected = SdpUtf8Decoder.DecodeUtf8(input.AsSpan(), false);
        SdpUtf8DecodeResult accepted = SdpUtf8Decoder.DecodeUtf8(input.AsSpan(), true);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.HasByteOrderMark, Is.True);
            Assert.That(rejected.ByteOrderMarkNotAllowed, Is.True);
            Assert.That(rejected.ByteOffset, Is.EqualTo(0));
            Assert.That(rejected.CharacterOffset, Is.EqualTo(0));
            Assert.That(rejected.Text, Is.Empty);
            Assert.That(rejected.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseByteOrderMarkNotAllowed));

            Assert.That(accepted.Success, Is.True);
            Assert.That(accepted.HasByteOrderMark, Is.True);
            Assert.That(accepted.ByteOrderMarkNotAllowed, Is.False);
            Assert.That(accepted.Text, Is.EqualTo("v=0"));
            Assert.That(accepted.CharacterCount, Is.EqualTo(3));
            Assert.That(accepted.BytesConsumed, Is.EqualTo(input.Length));
        });
    }

    [Test]
    public void DecodeUtf8_RejectsMalformedSequencesWithoutProducingText()
    {
        byte[][] malformedInputs = new byte[][]
        {
            new byte[] { 128 },
            new byte[] { 192, 128 },
            new byte[] { 224, 128, 128 },
            new byte[] { 237, 160, 128 },
            new byte[] { 240, 128, 128, 128 },
            new byte[] { 244, 144, 128, 128 },
            new byte[] { 245, 128, 128, 128 },
            new byte[] { 226, 130 }
        };

        foreach (byte[] input in malformedInputs)
        {
            SdpUtf8DecodeResult result = SdpUtf8Decoder.DecodeUtf8(input.AsSpan(), false);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.InvalidUtf8, Is.True);
                Assert.That(result.Text, Is.Empty);
                Assert.That(result.ByteOffset, Is.EqualTo(0));
                Assert.That(result.CharacterOffset, Is.EqualTo(0));
                Assert.That(result.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidUtf8));
            });
        }
    }

    [Test]
    public void DecodeUtf8_ReportsExactOffsetsAfterMultibyteCharacters()
    {
        byte[] afterBmpCharacter = new byte[] { 195, 169, 240, 40, 140, 188 };
        byte[] afterSupplementaryCharacter = new byte[] { 240, 159, 152, 128, 255 };

        SdpUtf8DecodeResult bmpResult = SdpUtf8Decoder.DecodeUtf8(afterBmpCharacter.AsSpan(), false);
        SdpUtf8DecodeResult supplementaryResult = SdpUtf8Decoder.DecodeUtf8(afterSupplementaryCharacter.AsSpan(), false);

        Assert.Multiple(() =>
        {
            Assert.That(bmpResult.InvalidUtf8, Is.True);
            Assert.That(bmpResult.ByteOffset, Is.EqualTo(2));
            Assert.That(bmpResult.CharacterOffset, Is.EqualTo(1));
            Assert.That(bmpResult.BytesConsumed, Is.EqualTo(2));
            Assert.That(bmpResult.CharacterCount, Is.EqualTo(1));

            Assert.That(supplementaryResult.InvalidUtf8, Is.True);
            Assert.That(supplementaryResult.ByteOffset, Is.EqualTo(4));
            Assert.That(supplementaryResult.CharacterOffset, Is.EqualTo(2));
            Assert.That(supplementaryResult.BytesConsumed, Is.EqualTo(4));
            Assert.That(supplementaryResult.CharacterCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void DecodeUtf8_RejectsNulAndControlCharactersButAllowsLineEndings()
    {
        SdpUtf8DecodeResult nul = SdpUtf8Decoder.DecodeUtf8(new byte[] { 65, 0, 66 }, false);
        SdpUtf8DecodeResult tab = SdpUtf8Decoder.DecodeUtf8(new byte[] { 65, 9, 66 }, false);
        SdpUtf8DecodeResult delete = SdpUtf8Decoder.DecodeUtf8(new byte[] { 65, 127, 66 }, false);
        SdpUtf8DecodeResult c1Control = SdpUtf8Decoder.DecodeUtf8(new byte[] { 65, 194, 133, 66 }, false);
        SdpUtf8DecodeResult lineEndings = SdpUtf8Decoder.DecodeUtf8(Encoding.ASCII.GetBytes("a\r\nb\nc\r"), false);

        Assert.Multiple(() =>
        {
            Assert.That(nul.NulCharacterNotAllowed, Is.True);
            Assert.That(nul.ControlCharacterNotAllowed, Is.False);
            Assert.That(nul.ByteOffset, Is.EqualTo(1));
            Assert.That(nul.CharacterOffset, Is.EqualTo(1));
            Assert.That(nul.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNulCharacterNotAllowed));

            Assert.That(tab.ControlCharacterNotAllowed, Is.True);
            Assert.That(tab.ByteOffset, Is.EqualTo(1));
            Assert.That(tab.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseControlCharacterNotAllowed));
            Assert.That(delete.ControlCharacterNotAllowed, Is.True);
            Assert.That(c1Control.ControlCharacterNotAllowed, Is.True);
            Assert.That(c1Control.ByteOffset, Is.EqualTo(1));
            Assert.That(c1Control.CharacterOffset, Is.EqualTo(1));

            Assert.That(lineEndings.Success, Is.True);
            Assert.That(lineEndings.Text, Is.EqualTo("a\r\nb\nc\r"));
        });
    }

    [Test]
    public void CharacterClasses_ImplementRfcTokenAndStringBoundaries()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SdpCharacterClasses.IsTokenCharacter('a'), Is.True);
            Assert.That(SdpCharacterClasses.IsTokenCharacter('9'), Is.True);
            Assert.That(SdpCharacterClasses.IsTokenCharacter('~'), Is.True);
            Assert.That(SdpCharacterClasses.IsTokenCharacter('/'), Is.False);
            Assert.That(SdpCharacterClasses.IsTokenCharacter(':'), Is.False);
            Assert.That(SdpCharacterClasses.IsTokenCharacter('é'), Is.False);
            Assert.That(SdpCharacterClasses.IsTokenByte(95), Is.True);
            Assert.That(SdpCharacterClasses.IsTokenByte(64), Is.False);

            Assert.That(SdpCharacterClasses.IsNonWhitespaceCharacter('!'), Is.True);
            Assert.That(SdpCharacterClasses.IsNonWhitespaceCharacter('世'), Is.True);
            Assert.That(SdpCharacterClasses.IsNonWhitespaceCharacter(' '), Is.False);
            Assert.That(SdpCharacterClasses.IsNonWhitespaceByte(128), Is.True);
            Assert.That(SdpCharacterClasses.IsNonWhitespaceByte(127), Is.False);

            Assert.That(SdpCharacterClasses.IsByteStringCharacter('\t'), Is.True);
            Assert.That(SdpCharacterClasses.IsByteStringCharacter('\0'), Is.False);
            Assert.That(SdpCharacterClasses.IsByteStringCharacter('\r'), Is.False);
            Assert.That(SdpCharacterClasses.IsByteStringCharacter('\n'), Is.False);
            Assert.That(SdpCharacterClasses.IsByteStringByte(255), Is.True);
            Assert.That(SdpCharacterClasses.IsByteStringByte(0), Is.False);
            Assert.That(SdpCharacterClasses.IsByteStringByte(10), Is.False);
            Assert.That(SdpCharacterClasses.IsByteStringByte(13), Is.False);
        });
    }
}
