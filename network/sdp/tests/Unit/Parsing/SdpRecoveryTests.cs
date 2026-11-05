using System.Text;

namespace sdp.Tests.Unit.Parsing;

public class SdpRecoveryTests
{
    [Test]
    public void CharacterRecovery_RetainsOneMalformedLineAndStopsBeforeMediaBoundary()
    {
        string input = "a=broken value\r\nm=audio 9 RTP/AVP 0";
        int expectedNextOffset = "a=broken value\r\n".Length;

        SdpRecoveryResult recovery = SdpRecovery.RecoverCharacterLine(input.AsSpan(), 0, 5, 100, 10, true);
        SdpRawLine line = recovery.Line ?? new SdpRawLine();

        Assert.Multiple(() =>
        {
            Assert.That(recovery.Recovered, Is.True);
            Assert.That(recovery.Retained, Is.True);
            Assert.That(recovery.Line, Is.Not.Null);
            Assert.That(recovery.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseRecoveredMalformedLine));
            Assert.That(recovery.NextOffset, Is.EqualTo(expectedNextOffset));
            Assert.That(recovery.NextCharacterOffset, Is.EqualTo(expectedNextOffset));
            Assert.That(recovery.NextByteOffset, Is.EqualTo(-1));
            Assert.That(recovery.NextLineNumber, Is.EqualTo(6));
            Assert.That(recovery.EndOfInput, Is.False);
            Assert.That(recovery.NextLineIsFieldBoundary, Is.True);
            Assert.That(recovery.NextLineIsMediaBoundary, Is.True);
            Assert.That(recovery.NextLineIsSessionBoundary, Is.False);

            Assert.That(line.Text, Is.EqualTo("a=broken value"));
            Assert.That(line.FieldName, Is.EqualTo('a'));
            Assert.That(line.Value, Is.EqualTo("broken value"));
            Assert.That(line.IsMalformed, Is.True);
            Assert.That(line.LineEnding, Is.EqualTo(SdpLineEnding.CrLf));
            Assert.That(line.Location.CharacterOffset, Is.EqualTo(0));
            Assert.That(line.Location.LineNumber, Is.EqualTo(5));
        });
    }

    [Test]
    public void CharacterRecovery_PreservesFollowingSessionBoundary()
    {
        string input = "not a field\nv=0";

        SdpRecoveryResult recovery = SdpRecovery.RecoverCharacterLine(input.AsSpan(), 0, 1, 100, 10, true);
        SdpRawLine line = recovery.Line ?? new SdpRawLine();

        Assert.Multiple(() =>
        {
            Assert.That(recovery.Recovered, Is.True);
            Assert.That(recovery.NextOffset, Is.EqualTo("not a field\n".Length));
            Assert.That(recovery.NextLineIsFieldBoundary, Is.True);
            Assert.That(recovery.NextLineIsSessionBoundary, Is.True);
            Assert.That(recovery.NextLineIsMediaBoundary, Is.False);
            Assert.That(line.Text, Is.EqualTo("not a field"));
            Assert.That(line.FieldName, Is.EqualTo('\0'));
            Assert.That(line.Value, Is.Empty);
            Assert.That(line.IsMalformed, Is.True);
            Assert.That(line.LineEnding, Is.EqualTo(SdpLineEnding.Lf));
        });
    }

    [Test]
    public void CharacterRecovery_CanAdvanceWithoutRetainingInputText()
    {
        string input = "bad\rs=name";

        SdpRecoveryResult recovery = SdpRecovery.RecoverCharacterLine(input.AsSpan(), 0, 2, 100, 10, false);

        Assert.Multiple(() =>
        {
            Assert.That(recovery.Recovered, Is.True);
            Assert.That(recovery.Retained, Is.False);
            Assert.That(recovery.Line, Is.Null);
            Assert.That(recovery.NextOffset, Is.EqualTo(4));
            Assert.That(recovery.NextLineNumber, Is.EqualTo(3));
            Assert.That(recovery.NextLineIsFieldBoundary, Is.True);
            Assert.That(recovery.NextLineIsMediaBoundary, Is.False);
            Assert.That(recovery.NextLineIsSessionBoundary, Is.False);
        });
    }

    [Test]
    public void Utf8Recovery_TracksByteAndCharacterOffsetsAcrossInternationalText()
    {
        byte[] input = Encoding.UTF8.GetBytes("é-invalid\r\nm=audio 9 RTP/AVP 0");
        int expectedNextByteOffset = Encoding.UTF8.GetByteCount("é-invalid\r\n");
        int expectedNextCharacterOffset = 100 + "é-invalid\r\n".Length;

        SdpRecoveryResult recovery = SdpRecovery.RecoverUtf8Line(input.AsSpan(), 0, 100, 7, 100, 10, true);
        SdpRawLine line = recovery.Line ?? new SdpRawLine();

        Assert.Multiple(() =>
        {
            Assert.That(recovery.Recovered, Is.True);
            Assert.That(recovery.Retained, Is.True);
            Assert.That(recovery.NextOffset, Is.EqualTo(expectedNextByteOffset));
            Assert.That(recovery.NextByteOffset, Is.EqualTo(expectedNextByteOffset));
            Assert.That(recovery.NextCharacterOffset, Is.EqualTo(expectedNextCharacterOffset));
            Assert.That(recovery.NextLineNumber, Is.EqualTo(8));
            Assert.That(recovery.NextLineIsMediaBoundary, Is.True);
            Assert.That(line.Text, Is.EqualTo("é-invalid"));
            Assert.That(line.IsMalformed, Is.True);
            Assert.That(line.Location.ByteOffset, Is.EqualTo(0));
            Assert.That(line.Location.CharacterOffset, Is.EqualTo(100));
            Assert.That(line.Location.LineNumber, Is.EqualTo(7));
        });
    }

    [Test]
    public void Recovery_HandlesTerminalLineWithoutCreatingAnotherLine()
    {
        string input = "broken";

        SdpRecoveryResult recovered = SdpRecovery.RecoverCharacterLine(input.AsSpan(), 0, 1, 20, 10, true);
        SdpRecoveryResult terminal = SdpRecovery.RecoverCharacterLine(input.AsSpan(), recovered.NextOffset, 2, 20, 10, true);

        Assert.Multiple(() =>
        {
            Assert.That(recovered.Recovered, Is.True);
            Assert.That(recovered.EndOfInput, Is.True);
            Assert.That(recovered.NextOffset, Is.EqualTo(input.Length));
            Assert.That(recovered.NextLineNumber, Is.EqualTo(2));
            Assert.That(recovered.NextLineIsFieldBoundary, Is.False);

            Assert.That(terminal.Recovered, Is.False);
            Assert.That(terminal.Retained, Is.False);
            Assert.That(terminal.EndOfInput, Is.True);
            Assert.That(terminal.NextOffset, Is.EqualTo(input.Length));
            Assert.That(terminal.NextLineNumber, Is.EqualTo(2));
            Assert.That(terminal.DiagnosticCode, Is.Empty);
        });
    }

    [Test]
    public void Recovery_EnforcesLineLengthWithoutConsumingFollowingBoundary()
    {
        string input = "12345\nm=x";

        SdpRecoveryResult recovery = SdpRecovery.RecoverCharacterLine(input.AsSpan(), 0, 1, 4, 10, true);

        Assert.Multiple(() =>
        {
            Assert.That(recovery.Recovered, Is.True);
            Assert.That(recovery.LineLengthExceeded, Is.True);
            Assert.That(recovery.Retained, Is.False);
            Assert.That(recovery.Line, Is.Null);
            Assert.That(recovery.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseLineLengthLimitExceeded));
            Assert.That(recovery.NextOffset, Is.EqualTo(6));
            Assert.That(recovery.NextLineIsMediaBoundary, Is.True);
        });
    }

    [Test]
    public void Recovery_StopsAtLineCountLimitWithoutAdvancing()
    {
        byte[] input = Encoding.ASCII.GetBytes("bad\nm=x");

        SdpRecoveryResult recovery = SdpRecovery.RecoverUtf8Line(input.AsSpan(), 0, 0, 2, 20, 1, true);

        Assert.Multiple(() =>
        {
            Assert.That(recovery.Recovered, Is.False);
            Assert.That(recovery.Retained, Is.False);
            Assert.That(recovery.LineCountExceeded, Is.True);
            Assert.That(recovery.NextOffset, Is.EqualTo(0));
            Assert.That(recovery.NextByteOffset, Is.EqualTo(0));
            Assert.That(recovery.NextCharacterOffset, Is.EqualTo(0));
            Assert.That(recovery.NextLineNumber, Is.EqualTo(2));
            Assert.That(recovery.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseLineCountLimitExceeded));
        });
    }
}
