using System.Text;
using email;

namespace email.Tests;

public class EmailLineReaderTests
{
    [Test]
    public void MixedLineEndingsArePreservedAtEveryTwoChunkSplit()
    {
        byte[] input = Encoding.ASCII.GetBytes("a\r\nbb\n\rccc\rd");
        int split = 0;
        while (split <= input.Length)
        {
            List<EmailLine> lines = ReadWithSplit(input, split, 64);
            AssertMixedLines(lines, "split " + split);
            split++;
        }
    }

    [Test]
    public void MixedLineEndingsArePreservedWithSingleByteChunks()
    {
        byte[] input = Encoding.ASCII.GetBytes("a\r\nbb\n\rccc\rd");
        EmailLineReader reader = new EmailLineReader(64);
        List<EmailLine> lines = new List<EmailLine>();
        int offset = 0;
        while (offset < input.Length)
        {
            int chunkEnd = offset + 1;
            bool endOfInput = chunkEnd == input.Length;
            ReadSegment(reader, input, ref offset, chunkEnd, endOfInput, lines);
        }

        DrainEnd(reader, lines);
        AssertMixedLines(lines, "single-byte chunks");
    }

    [Test]
    public void EmptyInputHasNoLineAndEmptyTerminatedLinesAreRetained()
    {
        EmailLineReader emptyReader = new EmailLineReader(10);
        EmailLineReadResult emptyResult = emptyReader.Read(Array.Empty<byte>(), true);
        Assert.That(emptyResult.GetStatus(), Is.EqualTo(EmailLineReadStatus.EndOfInput));
        Assert.That(emptyResult.GetConsumedByteCount(), Is.Zero);
        Assert.That(emptyResult.HasLine(), Is.False);
        Assert.That(emptyResult.GetLine(), Is.Null);

        List<EmailLine> lines = ReadWithSplit(Encoding.ASCII.GetBytes("\r\n\n\r"), 1, 10);
        Assert.That(lines, Has.Count.EqualTo(3));
        AssertLine(lines[0], string.Empty, EmailLineEnding.CrLf, "first empty line");
        AssertLine(lines[1], string.Empty, EmailLineEnding.Lf, "second empty line");
        AssertLine(lines[2], string.Empty, EmailLineEnding.Cr, "third empty line");
    }

    [Test]
    public void UnterminatedFinalLineReportsNoEnding()
    {
        List<EmailLine> lines = ReadWithSplit(Encoding.ASCII.GetBytes("final"), 2, 10);

        Assert.That(lines, Has.Count.EqualTo(1));
        AssertLine(lines[0], "final", EmailLineEnding.None, "final line");
        Assert.That(lines[0].HasLineEnding(), Is.False);
        Assert.That(lines[0].GetLineEndingByteCount(), Is.Zero);
        Assert.That(lines[0].GetTotalByteCount(), Is.EqualTo(5));
    }

    [Test]
    public void CrAtChunkEndWaitsForClassification()
    {
        EmailLineReader reader = new EmailLineReader(10);
        EmailLineReadResult first = reader.Read(new byte[] { 65, 13 }, false);
        EmailLineReadResult emptyPoll = reader.Read(Array.Empty<byte>(), false);
        EmailLineReadResult completed = reader.Read(new byte[] { 10 }, true);

        Assert.That(first.GetStatus(), Is.EqualTo(EmailLineReadStatus.NeedMoreData));
        Assert.That(first.GetConsumedByteCount(), Is.EqualTo(2));
        Assert.That(emptyPoll.GetStatus(), Is.EqualTo(EmailLineReadStatus.NeedMoreData));
        Assert.That(emptyPoll.GetConsumedByteCount(), Is.Zero);
        AssertLine(completed.GetLine()!, "A", EmailLineEnding.CrLf, "split CRLF");
        Assert.That(completed.GetLine()!.GetLineEndingByteCount(), Is.EqualTo(2));
        Assert.That(completed.GetConsumedByteCount(), Is.EqualTo(1));
        Assert.That(reader.Read(Array.Empty<byte>(), true).GetStatus(), Is.EqualTo(EmailLineReadStatus.EndOfInput));
    }

    [Test]
    public void BareCrDoesNotConsumeFollowingByte()
    {
        EmailLineReader reader = new EmailLineReader(10);
        EmailLineReadResult first = reader.Read(new byte[] { 65, 13 }, false);
        Assert.That(first.GetStatus(), Is.EqualTo(EmailLineReadStatus.NeedMoreData));

        EmailLineReadResult crLine = reader.Read(new byte[] { 66, 10 }, true);
        AssertLine(crLine.GetLine()!, "A", EmailLineEnding.Cr, "bare CR");
        Assert.That(crLine.GetConsumedByteCount(), Is.Zero);

        EmailLineReadResult lfLine = reader.Read(new byte[] { 66, 10 }, true);
        AssertLine(lfLine.GetLine()!, "B", EmailLineEnding.Lf, "following LF line");
        Assert.That(lfLine.GetConsumedByteCount(), Is.EqualTo(2));
    }

    [Test]
    public void BoundCountsContentButNotLineEndingBytes()
    {
        EmailLineReader exact = new EmailLineReader(3);
        EmailLineReadResult exactResult = exact.Read(Encoding.ASCII.GetBytes("abc\r\n"), true);
        AssertLine(exactResult.GetLine()!, "abc", EmailLineEnding.CrLf, "exact bound");

        EmailLineReader overflow = new EmailLineReader(3);
        EmailLimitException exception = Assert.Throws<EmailLimitException>(() => overflow.Read(Encoding.ASCII.GetBytes("abcd"), false));
        Assert.That(exception.GetLimitName(), Is.EqualTo("MaxLineBytes"));
        Assert.That(exception.GetLimit(), Is.EqualTo(3));
        Assert.That(exception.GetObserved(), Is.EqualTo(4));
        Assert.That(overflow.GetBufferedByteCount(), Is.EqualTo(3));
    }

    [Test]
    public void ResetClearsBufferedPendingAndEndedState()
    {
        EmailLineReader reader = new EmailLineReader(5);
        reader.Read(new byte[] { 65, 13 }, false);
        reader.Reset();

        EmailLineReadResult line = reader.Read(new byte[] { 66, 10 }, true);
        AssertLine(line.GetLine()!, "B", EmailLineEnding.Lf, "after pending reset");
        reader.Reset();
        Assert.That(reader.GetBufferedByteCount(), Is.Zero);
        Assert.That(reader.Read(Encoding.ASCII.GetBytes("C"), true).GetLine()!.GetContentBytes(), Is.EqualTo(new byte[] { 67 }));
    }

    [Test]
    public void ReaderRejectsInvalidConstructionAndInputAfterEnd()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailLineReader(0));
        EmailLineReader reader = new EmailLineReader(10);
        Assert.That(reader.GetMaxLineBytes(), Is.EqualTo(10));
        reader.Read(Array.Empty<byte>(), true);
        Assert.Throws<InvalidOperationException>(() => reader.Read(new byte[] { 1 }, true));
        Assert.That(reader.Read(Array.Empty<byte>(), false).GetStatus(), Is.EqualTo(EmailLineReadStatus.EndOfInput));
    }

    [Test]
    public void LineAndResultDefensivelyCopyAndValidateInput()
    {
        byte[] content = new byte[] { 65 };
        EmailLine line = new EmailLine(content, EmailLineEnding.Lf);
        content[0] = 66;
        byte[] returned = line.GetContentBytes();
        returned[0] = 67;

        Assert.That(line.GetContentBytes(), Is.EqualTo(new byte[] { 65 }));
        Assert.That(line.GetContentLength(), Is.EqualTo(1));
        Assert.That(line.GetLineEndingByteCount(), Is.EqualTo(1));
        Assert.That(line.GetTotalByteCount(), Is.EqualTo(2));
        Assert.Throws<ArgumentNullException>(() => new EmailLine(null!, EmailLineEnding.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailLine(Array.Empty<byte>(), (EmailLineEnding)10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailLineReadResultOperations().CreateNeedMoreData(-1));
        Assert.Throws<ArgumentNullException>(() => new EmailLineReadResultOperations().CreateLine(null!, 0));
    }

    [Test]
    public void LimitExceptionValidatesAndReportsContext()
    {
        EmailLimitException exception = new EmailLimitException("ExampleLimit", 5, 6);

        Assert.That(exception.Message, Does.Contain("ExampleLimit"));
        Assert.That(exception.GetLimitName(), Is.EqualTo("ExampleLimit"));
        Assert.That(exception.GetLimit(), Is.EqualTo(5));
        Assert.That(exception.GetObserved(), Is.EqualTo(6));
        Assert.Throws<ArgumentNullException>(() => new EmailLimitException(null!, 1, 2));
        Assert.Throws<ArgumentException>(() => new EmailLimitException(string.Empty, 1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailLimitException("limit", -1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailLimitException("limit", 1, -2));
    }

    static List<EmailLine> ReadWithSplit(byte[] input, int split, int maxLineBytes)
    {
        EmailLineReader reader = new EmailLineReader(maxLineBytes);
        List<EmailLine> lines = new List<EmailLine>();
        int offset = 0;
        ReadSegment(reader, input, ref offset, split, false, lines);
        ReadSegment(reader, input, ref offset, input.Length, true, lines);
        DrainEnd(reader, lines);
        return lines;
    }

    static void ReadSegment(EmailLineReader reader, byte[] input, ref int offset, int endOffset, bool endOfInput, List<EmailLine> lines)
    {
        int iterationCount = 0;
        while (offset < endOffset)
        {
            EmailLineReadResult result = reader.Read(new ReadOnlySpan<byte>(input, offset, endOffset - offset), endOfInput);
            offset += result.GetConsumedByteCount();
            if (result.HasLine())
            {
                lines.Add(result.GetLine()!);
            }

            if (result.GetStatus() == EmailLineReadStatus.NeedMoreData)
            {
                Assert.That(offset, Is.EqualTo(endOffset));
                break;
            }

            iterationCount++;
            Assert.That(iterationCount, Is.LessThan(input.Length + 2), "reader made no progress");
        }
    }

    static void DrainEnd(EmailLineReader reader, List<EmailLine> lines)
    {
        int iterationCount = 0;
        while (true)
        {
            EmailLineReadResult result = reader.Read(Array.Empty<byte>(), true);
            if (result.HasLine())
            {
                lines.Add(result.GetLine()!);
            }

            if (result.GetStatus() == EmailLineReadStatus.EndOfInput)
            {
                return;
            }

            iterationCount++;
            Assert.That(iterationCount, Is.LessThan(3), "end-of-input did not settle");
        }
    }

    static void AssertMixedLines(List<EmailLine> lines, string context)
    {
        Assert.That(lines, Has.Count.EqualTo(5), context);
        AssertLine(lines[0], "a", EmailLineEnding.CrLf, context + " line 1");
        AssertLine(lines[1], "bb", EmailLineEnding.Lf, context + " line 2");
        AssertLine(lines[2], string.Empty, EmailLineEnding.Cr, context + " line 3");
        AssertLine(lines[3], "ccc", EmailLineEnding.Cr, context + " line 4");
        AssertLine(lines[4], "d", EmailLineEnding.None, context + " line 5");
    }

    static void AssertLine(EmailLine line, string content, EmailLineEnding ending, string context)
    {
        Assert.That(Encoding.ASCII.GetString(line.GetContentBytes()), Is.EqualTo(content), context + " content");
        Assert.That(line.GetLineEnding(), Is.EqualTo(ending), context + " ending");
        Assert.That(line.HasLineEnding(), Is.EqualTo(ending != EmailLineEnding.None), context + " ending presence");
    }
}
