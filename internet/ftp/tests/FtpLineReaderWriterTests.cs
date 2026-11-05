using System.Text;

namespace ftp.Tests;

public class FtpLineReaderWriterTests
{
    [Test]
    public void Reader_reassembles_fragmented_lines_and_retains_coalesced_lines()
    {
        FtpLineReader reader = new FtpLineReader(32, 4);

        AppendAscii(reader, "USER guest\r");
        Assert.That(reader.GetPendingLineCount(), Is.Zero);
        Assert.That(reader.HasPartialLine(), Is.True);

        AppendAscii(reader, "\nNO");
        Assert.That(ReadAscii(reader), Is.EqualTo("USER guest"));
        Assert.That(reader.HasPartialLine(), Is.True);

        AppendAscii(reader, "OP\r\nQUIT\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(reader.GetPendingLineCount(), Is.EqualTo(2));
            Assert.That(ReadAscii(reader), Is.EqualTo("NOOP"));
            Assert.That(ReadAscii(reader), Is.EqualTo("QUIT"));
            Assert.That(reader.ReadLine(), Is.Null);
            Assert.That(reader.HasPartialLine(), Is.False);
        });
    }

    [Test]
    public void Reader_preserves_empty_and_multiple_lines_from_one_input_buffer()
    {
        FtpLineReader reader = new FtpLineReader(8, 3);

        AppendAscii(reader, "A\r\n\r\nB\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(reader.GetPendingLineCount(), Is.EqualTo(3));
            Assert.That(ReadAscii(reader), Is.EqualTo("A"));
            Assert.That(ReadAscii(reader), Is.Empty);
            Assert.That(ReadAscii(reader), Is.EqualTo("B"));
        });
    }

    [Test]
    public void Reader_enforces_encoded_line_and_pending_line_limits()
    {
        FtpLineReader exact = new FtpLineReader(6, 2);
        AppendAscii(exact, "ABCD\r\n");

        FtpLineReader oversized = new FtpLineReader(6, 2);
        FtpProtocolException? sizeFailure = Assert.Throws<FtpProtocolException>(() => AppendAscii(oversized, "ABCDE"));

        FtpLineReader queued = new FtpLineReader(4, 2);
        FtpProtocolException? queueFailure = Assert.Throws<FtpProtocolException>(() => AppendAscii(queued, "A\r\nB\r\nC\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(ReadAscii(exact), Is.EqualTo("ABCD"));
            Assert.That(sizeFailure!.Message, Does.Contain("byte limit"));
            Assert.That(oversized.Failed, Is.True);
            Assert.That(queueFailure!.Message, Does.Contain("pending FTP line limit"));
            Assert.That(queued.GetPendingLineCount(), Is.EqualTo(2));
            Assert.That(ReadAscii(queued), Is.EqualTo("A"));
            Assert.That(ReadAscii(queued), Is.EqualTo("B"));
        });
    }

    [Test]
    public void Reader_rejects_bare_or_malformed_terminators()
    {
        FtpLineReader bareLineFeed = new FtpLineReader(16, 2);
        FtpLineReader malformedCarriageReturn = new FtpLineReader(16, 2);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => AppendAscii(bareLineFeed, "NOOP\n"));
            Assert.Throws<FtpProtocolException>(() => AppendAscii(malformedCarriageReturn, "NOOP\rX"));
            Assert.That(bareLineFeed.Failed, Is.True);
            Assert.That(malformedCarriageReturn.Failed, Is.True);
        });
    }

    [Test]
    public void Reader_accepts_clean_completion_and_rejects_partial_end_of_input()
    {
        FtpLineReader complete = new FtpLineReader(16, 2);
        AppendAscii(complete, "NOOP\r\n");
        complete.CompleteInput();
        complete.CompleteInput();

        FtpLineReader partial = new FtpLineReader(16, 2);
        AppendAscii(partial, "NOOP\r");

        Assert.Multiple(() =>
        {
            Assert.That(complete.Completed, Is.True);
            Assert.That(ReadAscii(complete), Is.EqualTo("NOOP"));
            Assert.Throws<FtpProtocolException>(() => partial.CompleteInput());
            Assert.That(partial.Failed, Is.True);
        });
    }

    [Test]
    public void Writer_emits_canonical_crlf_for_bounded_lines()
    {
        FtpLineWriter writer = new FtpLineWriter(8);
        using MemoryStream destination = new MemoryStream();

        writer.WriteLine(destination, Encoding.ASCII.GetBytes("NOOP"));
        writer.WriteLine(destination, Array.Empty<byte>());

        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo("NOOP\r\n\r\n"));
    }

    [Test]
    public void Writer_validates_before_writing_oversized_or_embedded_terminators()
    {
        FtpLineWriter writer = new FtpLineWriter(6);
        using MemoryStream destination = new MemoryStream();

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => writer.WriteLine(destination, Encoding.ASCII.GetBytes("ABCDE")));
            Assert.Throws<FtpProtocolException>(() => writer.WriteLine(destination, Encoding.ASCII.GetBytes("A\rB")));
            Assert.Throws<FtpProtocolException>(() => writer.WriteLine(destination, Encoding.ASCII.GetBytes("A\nB")));
            Assert.That(destination.Length, Is.Zero);
        });
    }

    private static void AppendAscii(FtpLineReader reader, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        reader.AppendBytes(bytes, 0, bytes.Length);
    }

    private static string? ReadAscii(FtpLineReader reader)
    {
        byte[]? line = reader.ReadLine();
        if (line == null)
        {
            return null;
        }

        return Encoding.ASCII.GetString(line);
    }
}
