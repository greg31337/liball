using System.Text;

namespace sdp.Tests.Unit.Text;

public class SdpLineReaderTests
{
    [Test]
    public void CharacterReader_ReportsCrLfLinesAndExactOffsets()
    {
        string input = "v=0\r\ns=Example\r\n";

        SdpLineReadResult first = SdpLineReader.ReadCharacterLine(input.AsSpan(), 0, 1, 20, 10);
        SdpLineReadResult second = SdpLineReader.ReadCharacterLine(input.AsSpan(), first.NextOffset, 2, 20, 10);
        SdpLineReadResult terminal = SdpLineReader.ReadCharacterLine(input.AsSpan(), second.NextOffset, 3, 20, 10);

        Assert.Multiple(() =>
        {
            Assert.That(first.HasLine, Is.True);
            Assert.That(first.Line.Text, Is.EqualTo("v=0"));
            Assert.That(first.Line.LineEnding, Is.EqualTo(SdpLineEnding.CrLf));
            Assert.That(first.Line.Location.CharacterOffset, Is.EqualTo(0));
            Assert.That(first.Line.Location.ByteOffset, Is.EqualTo(-1));
            Assert.That(first.Line.Location.LineNumber, Is.EqualTo(1));
            Assert.That(first.Line.Location.ColumnNumber, Is.EqualTo(1));
            Assert.That(first.CharacterLength, Is.EqualTo(3));
            Assert.That(first.CharactersConsumed, Is.EqualTo(5));
            Assert.That(first.NextOffset, Is.EqualTo(5));
            Assert.That(first.EndOfInput, Is.False);

            Assert.That(second.HasLine, Is.True);
            Assert.That(second.Line.Text, Is.EqualTo("s=Example"));
            Assert.That(second.Line.LineEnding, Is.EqualTo(SdpLineEnding.CrLf));
            Assert.That(second.Line.Location.CharacterOffset, Is.EqualTo(5));
            Assert.That(second.Line.Location.LineNumber, Is.EqualTo(2));
            Assert.That(second.CharacterLength, Is.EqualTo(9));
            Assert.That(second.CharactersConsumed, Is.EqualTo(11));
            Assert.That(second.NextOffset, Is.EqualTo(input.Length));
            Assert.That(second.EndOfInput, Is.True);

            Assert.That(terminal.HasLine, Is.False);
            Assert.That(terminal.EndOfInput, Is.True);
            Assert.That(terminal.NextOffset, Is.EqualTo(input.Length));
        });
    }

    [Test]
    public void CharacterReader_AcceptsAlternateEndingsAndTerminalLine()
    {
        string input = "\nalpha\rbeta";

        SdpLineReadResult empty = SdpLineReader.ReadCharacterLine(input.AsSpan(), 0, 1, 10, 3);
        SdpLineReadResult alpha = SdpLineReader.ReadCharacterLine(input.AsSpan(), empty.NextOffset, 2, 10, 3);
        SdpLineReadResult beta = SdpLineReader.ReadCharacterLine(input.AsSpan(), alpha.NextOffset, 3, 10, 3);

        Assert.Multiple(() =>
        {
            Assert.That(empty.HasLine, Is.True);
            Assert.That(empty.Line.Text, Is.Empty);
            Assert.That(empty.Line.LineEnding, Is.EqualTo(SdpLineEnding.Lf));
            Assert.That(empty.CharactersConsumed, Is.EqualTo(1));
            Assert.That(empty.NextOffset, Is.EqualTo(1));

            Assert.That(alpha.Line.Text, Is.EqualTo("alpha"));
            Assert.That(alpha.Line.LineEnding, Is.EqualTo(SdpLineEnding.Cr));
            Assert.That(alpha.Line.Location.CharacterOffset, Is.EqualTo(1));
            Assert.That(alpha.CharactersConsumed, Is.EqualTo(6));
            Assert.That(alpha.NextOffset, Is.EqualTo(7));

            Assert.That(beta.Line.Text, Is.EqualTo("beta"));
            Assert.That(beta.Line.LineEnding, Is.EqualTo(SdpLineEnding.None));
            Assert.That(beta.Line.Location.CharacterOffset, Is.EqualTo(7));
            Assert.That(beta.CharactersConsumed, Is.EqualTo(4));
            Assert.That(beta.NextOffset, Is.EqualTo(input.Length));
            Assert.That(beta.EndOfInput, Is.True);
        });
    }

    [Test]
    public void CharacterReader_EnforcesLengthAndLineCountLimits()
    {
        string input = "1234\nnext";

        SdpLineReadResult boundary = SdpLineReader.ReadCharacterLine(input.AsSpan(), 0, 1, 4, 2);
        SdpLineReadResult overlong = SdpLineReader.ReadCharacterLine(input.AsSpan(), 0, 1, 3, 2);
        SdpLineReadResult tooMany = SdpLineReader.ReadCharacterLine(input.AsSpan(), 5, 2, 10, 1);

        Assert.Multiple(() =>
        {
            Assert.That(boundary.LineLengthExceeded, Is.False);
            Assert.That(boundary.Line.Text, Is.EqualTo("1234"));

            Assert.That(overlong.HasLine, Is.True);
            Assert.That(overlong.LineLengthExceeded, Is.True);
            Assert.That(overlong.Line.Text, Is.Empty);
            Assert.That(overlong.CharacterLength, Is.EqualTo(4));
            Assert.That(overlong.CharactersConsumed, Is.EqualTo(5));
            Assert.That(overlong.NextOffset, Is.EqualTo(5));

            Assert.That(tooMany.HasLine, Is.False);
            Assert.That(tooMany.LineCountExceeded, Is.True);
            Assert.That(tooMany.NextOffset, Is.EqualTo(5));
        });
    }

    [Test]
    public void Utf8Reader_ReportsExactByteAndCharacterPositions()
    {
        byte[] input = Encoding.UTF8.GetBytes("s=Café\r\nm=audio 9 RTP/AVP 0");

        SdpLineReadResult first = SdpLineReader.ReadUtf8Line(input.AsSpan(), 0, 10, 4, 30, 10);
        SdpLineReadResult second = SdpLineReader.ReadUtf8Line(
            input.AsSpan(),
            first.NextOffset,
            10 + first.CharactersConsumed,
            5,
            30,
            10);

        Assert.Multiple(() =>
        {
            Assert.That(first.HasLine, Is.True);
            Assert.That(first.Line.Text, Is.EqualTo("s=Café"));
            Assert.That(first.Line.LineEnding, Is.EqualTo(SdpLineEnding.CrLf));
            Assert.That(first.Line.Location.CharacterOffset, Is.EqualTo(10));
            Assert.That(first.Line.Location.ByteOffset, Is.EqualTo(0));
            Assert.That(first.Line.Location.LineNumber, Is.EqualTo(4));
            Assert.That(first.Line.Location.ColumnNumber, Is.EqualTo(1));
            Assert.That(first.CharacterLength, Is.EqualTo(6));
            Assert.That(first.ByteLength, Is.EqualTo(7));
            Assert.That(first.CharactersConsumed, Is.EqualTo(8));
            Assert.That(first.BytesConsumed, Is.EqualTo(9));
            Assert.That(first.NextOffset, Is.EqualTo(9));
            Assert.That(first.EndOfInput, Is.False);

            Assert.That(second.Line.Text, Is.EqualTo("m=audio 9 RTP/AVP 0"));
            Assert.That(second.Line.LineEnding, Is.EqualTo(SdpLineEnding.None));
            Assert.That(second.Line.Location.CharacterOffset, Is.EqualTo(18));
            Assert.That(second.Line.Location.ByteOffset, Is.EqualTo(9));
            Assert.That(second.Line.Location.LineNumber, Is.EqualTo(5));
            Assert.That(second.CharacterLength, Is.EqualTo(19));
            Assert.That(second.ByteLength, Is.EqualTo(19));
            Assert.That(second.CharactersConsumed, Is.EqualTo(19));
            Assert.That(second.BytesConsumed, Is.EqualTo(19));
            Assert.That(second.NextOffset, Is.EqualTo(input.Length));
            Assert.That(second.EndOfInput, Is.True);
        });
    }

    [Test]
    public void Utf8Reader_UsesByteLengthForTheLineLimit()
    {
        byte[] input = Encoding.UTF8.GetBytes("é\n");

        SdpLineReadResult boundary = SdpLineReader.ReadUtf8Line(input.AsSpan(), 0, 0, 1, 2, 1);
        SdpLineReadResult overlong = SdpLineReader.ReadUtf8Line(input.AsSpan(), 0, 0, 1, 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(boundary.LineLengthExceeded, Is.False);
            Assert.That(boundary.Line.Text, Is.EqualTo("é"));
            Assert.That(boundary.CharacterLength, Is.EqualTo(1));
            Assert.That(boundary.ByteLength, Is.EqualTo(2));
            Assert.That(boundary.CharactersConsumed, Is.EqualTo(2));
            Assert.That(boundary.BytesConsumed, Is.EqualTo(3));

            Assert.That(overlong.LineLengthExceeded, Is.True);
            Assert.That(overlong.Line.Text, Is.Empty);
            Assert.That(overlong.CharacterLength, Is.EqualTo(1));
            Assert.That(overlong.ByteLength, Is.EqualTo(2));
            Assert.That(overlong.NextOffset, Is.EqualTo(input.Length));
        });
    }

    [Test]
    public void Utf8Reader_AcceptsBareLfAndCrEndings()
    {
        byte[] input = Encoding.UTF8.GetBytes("a\nb\rc");

        SdpLineReadResult lf = SdpLineReader.ReadUtf8Line(input.AsSpan(), 0, 0, 1, 10, 3);
        SdpLineReadResult cr = SdpLineReader.ReadUtf8Line(input.AsSpan(), lf.NextOffset, lf.CharactersConsumed, 2, 10, 3);
        SdpLineReadResult final = SdpLineReader.ReadUtf8Line(
            input.AsSpan(),
            cr.NextOffset,
            lf.CharactersConsumed + cr.CharactersConsumed,
            3,
            10,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(lf.Line.Text, Is.EqualTo("a"));
            Assert.That(lf.Line.LineEnding, Is.EqualTo(SdpLineEnding.Lf));
            Assert.That(lf.NextOffset, Is.EqualTo(2));

            Assert.That(cr.Line.Text, Is.EqualTo("b"));
            Assert.That(cr.Line.LineEnding, Is.EqualTo(SdpLineEnding.Cr));
            Assert.That(cr.NextOffset, Is.EqualTo(4));

            Assert.That(final.Line.Text, Is.EqualTo("c"));
            Assert.That(final.Line.LineEnding, Is.EqualTo(SdpLineEnding.None));
            Assert.That(final.Line.Location.CharacterOffset, Is.EqualTo(4));
            Assert.That(final.Line.Location.ByteOffset, Is.EqualTo(4));
            Assert.That(final.EndOfInput, Is.True);
        });
    }

    [Test]
    public void Readers_RejectInvalidPositionsAndLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SdpLineReader.ReadCharacterLine("x".AsSpan(), -1, 1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SdpLineReader.ReadCharacterLine("x".AsSpan(), 2, 1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SdpLineReader.ReadCharacterLine("x".AsSpan(), 0, 0, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SdpLineReader.ReadCharacterLine("x".AsSpan(), 0, 1, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SdpLineReader.ReadCharacterLine("x".AsSpan(), 0, 1, 1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SdpLineReader.ReadUtf8Line(new byte[1], 0, -1, 1, 1, 1));
        });
    }
}
