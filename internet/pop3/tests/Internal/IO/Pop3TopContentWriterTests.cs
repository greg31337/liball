using System.Text;

namespace pop3.Tests;

public class Pop3TopContentWriterTests
{
    [Test]
    public void WriterSelectsHeadersAndRequestedBodyLinesWithDotStuffing()
    {
        MemoryStream destination = new MemoryStream();
        Pop3TopContentWriter writer = CreateWriter(destination);
        MemoryStream source = new MemoryStream(Encoding.ASCII.GetBytes(
            "Header: value\n.Other: value\r\n\r\n.first\rsecond\nthird"));

        long octets = writer.WriteTopContent(
            source,
            2,
            64,
            null,
            3,
            false,
            CancellationToken.None);

        Assert.That(octets, Is.EqualTo(48));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo(
            "Header: value\r\n" +
            "..Other: value\r\n" +
            "\r\n" +
            "..first\r\n" +
            "second\r\n"));
        Assert.That(source.CanRead, Is.True);
    }

    [Test]
    public void HeaderOnlySourceReceivesTheRequiredBlankSeparator()
    {
        MemoryStream destination = new MemoryStream();
        Pop3TopContentWriter writer = CreateWriter(destination);
        MemoryStream source = new MemoryStream(Encoding.ASCII.GetBytes(
            "Header: value"));

        long octets = writer.WriteTopContent(
            source,
            0,
            64,
            null,
            1,
            false,
            CancellationToken.None);

        Assert.That(octets, Is.EqualTo(17));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()),
            Is.EqualTo("Header: value\r\n\r\n"));
    }

    [Test]
    public void WriterRejectsInvalidInputsBeforeReadingOrWriting()
    {
        MemoryStream destination = new MemoryStream();
        Pop3TopContentWriter writer = CreateWriter(destination);
        TrackingReadStream source = new TrackingReadStream();

        Assert.Throws<ArgumentNullException>(() =>
            new Pop3TopContentWriter(null!));
        Assert.Throws<ArgumentNullException>(() => writer.WriteTopContent(
            null!, 0, 64, null, 1, false, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => writer.WriteTopContent(
            new WriteOnlyStream(), 0, 64, null, 1, false, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteTopContent(
            source, -1, 64, null, 1, false, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteTopContent(
            source, 0, 2, null, 1, false, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteTopContent(
            source, 0, 64, null, 0, false, CancellationToken.None));

        Assert.That(source.ReadCallCount, Is.Zero);
        Assert.That(destination.ToArray(), Is.Empty);
    }

    [Test]
    public void Utf8ModeRejectsMalformedSelectedLines()
    {
        MemoryStream destination = new MemoryStream();
        Pop3TopContentWriter writer = CreateWriter(destination);
        MemoryStream source = new MemoryStream(new byte[] { 72, 58, 32, 195, 13 });

        Assert.Throws<InvalidDataException>(() => writer.WriteTopContent(
            source,
            0,
            64,
            null,
            1,
            true,
            CancellationToken.None));
        Assert.That(destination.ToArray(), Is.Empty);
    }

    static Pop3TopContentWriter CreateWriter(Stream destination)
    {
        Pop3BufferedOutput output = new Pop3BufferedOutput(destination, 8, 8);
        return new Pop3TopContentWriter(new Pop3LineWriter(output));
    }

    sealed class TrackingReadStream : MemoryStream
    {
        internal int ReadCallCount;

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCallCount++;
            return base.Read(buffer, offset, count);
        }
    }

    sealed class WriteOnlyStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }
    }
}
