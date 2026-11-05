namespace pop3.Tests;

public class Pop3LineIoTests
{
    [Test]
    public void ReaderHandlesFragmentedSequentialAndEmptyLines()
    {
        OneByteReadStream stream = new OneByteReadStream(new byte[]
        {
            65, 66, 13, 10,
            13, 10,
            0, 255, 13, 10
        });
        Pop3BufferedInput input = new Pop3BufferedInput(stream, 3, 512);
        Pop3LineReader reader = new Pop3LineReader(input);

        Assert.That(reader.ReadCommandLine(CancellationToken.None), Is.EqualTo(new byte[] { 65, 66 }));
        Assert.That(reader.ReadResponseLine(CancellationToken.None), Is.Empty);
        Assert.That(reader.ReadCapabilityLine(CancellationToken.None), Is.EqualTo(new byte[] { 0, 255 }));
        Assert.That(reader.ReadResponseLine(CancellationToken.None), Is.Null);
        Assert.That(stream.ReadCallCount, Is.EqualTo(11));
    }

    [Test]
    public void ReaderDistinguishesCleanEofFromTruncatedLines()
    {
        Pop3LineReader emptyReader = CreateReader(new byte[0]);
        Pop3LineReader contentReader = CreateReader(new byte[] { 65 });
        Pop3LineReader carriageReturnReader = CreateReader(new byte[] { 65, 13 });

        Assert.That(emptyReader.ReadCommandLine(CancellationToken.None), Is.Null);
        Assert.Throws<EndOfStreamException>(() => contentReader.ReadCommandLine(CancellationToken.None));
        Assert.Throws<EndOfStreamException>(() => carriageReturnReader.ReadCommandLine(CancellationToken.None));
    }

    [Test]
    public void ReaderRejectsBareLineTerminators()
    {
        Pop3LineReader lineFeedReader = CreateReader(new byte[] { 65, 10 });
        Pop3LineReader carriageReturnReader = CreateReader(new byte[] { 65, 13, 66, 13, 10 });

        Assert.Throws<InvalidDataException>(() => lineFeedReader.ReadCommandLine(CancellationToken.None));
        Assert.Throws<InvalidDataException>(() => carriageReturnReader.ReadCommandLine(CancellationToken.None));
    }

    [Test]
    public void ReaderEnforcesCommandResponseAndCapabilityBoundariesIncludingCrlf()
    {
        byte[] exactCommand = CreateWireLine(253, 65);
        byte[] longCommand = CreateWireLine(254, 65);
        byte[] exactResponse = CreateWireLine(510, 66);
        byte[] longCapability = CreateWireLine(511, 67);

        Assert.That(CreateReader(exactCommand).ReadCommandLine(CancellationToken.None), Has.Length.EqualTo(253));
        Assert.Throws<InvalidDataException>(() => CreateReader(longCommand).ReadCommandLine(CancellationToken.None));
        Assert.That(CreateReader(exactResponse).ReadResponseLine(CancellationToken.None), Has.Length.EqualTo(510));
        Assert.Throws<InvalidDataException>(() => CreateReader(longCapability).ReadCapabilityLine(CancellationToken.None));
    }

    [Test]
    public void ReaderSupportsConfiguredDataLineCeilings()
    {
        Assert.That(CreateReader(CreateWireLine(3, 1)).ReadDataLine(5, CancellationToken.None), Has.Length.EqualTo(3));
        Assert.Throws<InvalidDataException>(() => CreateReader(CreateWireLine(4, 1)).ReadDataLine(5, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateReader(new byte[0]).ReadDataLine(1, CancellationToken.None));
    }

    [Test]
    public void WriterEmitsExactCrlfAndFlushesCompleteLines()
    {
        MemoryStream stream = new MemoryStream();
        Pop3BufferedOutput output = new Pop3BufferedOutput(stream, 3, 512);
        Pop3LineWriter writer = new Pop3LineWriter(output);

        writer.WriteCommandLine(new byte[] { 65, 66 }, CancellationToken.None);
        writer.WriteResponseLine(new byte[0], CancellationToken.None);
        writer.WriteCapabilityLine(new byte[] { 67 }, CancellationToken.None);

        Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 65, 66, 13, 10, 13, 10, 67, 13, 10 }));
        Assert.That(output.GetBufferedCount(), Is.Zero);
    }

    [Test]
    public void WriterEnforcesAllLineBoundariesBeforeWriting()
    {
        MemoryStream stream = new MemoryStream();
        Pop3BufferedOutput output = new Pop3BufferedOutput(stream, 7, 512);
        Pop3LineWriter writer = new Pop3LineWriter(output);

        writer.WriteCommandLine(CreateContent(253, 65), CancellationToken.None);
        writer.WriteResponseLine(CreateContent(510, 66), CancellationToken.None);
        writer.WriteCapabilityLine(CreateContent(510, 67), CancellationToken.None);
        long acceptedLength = stream.Length;

        Assert.Throws<InvalidDataException>(() => writer.WriteCommandLine(CreateContent(254, 65), CancellationToken.None));
        Assert.Throws<InvalidDataException>(() => writer.WriteResponseLine(CreateContent(511, 66), CancellationToken.None));
        Assert.Throws<InvalidDataException>(() => writer.WriteCapabilityLine(CreateContent(511, 67), CancellationToken.None));
        Assert.That(stream.Length, Is.EqualTo(acceptedLength));
    }

    [Test]
    public void WriterRejectsEmbeddedTerminatorsAndInvalidDataLimits()
    {
        MemoryStream stream = new MemoryStream();
        Pop3LineWriter writer = new Pop3LineWriter(new Pop3BufferedOutput(stream, 2, 4));

        Assert.Throws<InvalidDataException>(() => writer.WriteCommandLine(new byte[] { 65, 13, 66 }, CancellationToken.None));
        Assert.Throws<InvalidDataException>(() => writer.WriteResponseLine(new byte[] { 65, 10, 66 }, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => writer.WriteCommandLine(null!, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteDataLine(new byte[0], 1, CancellationToken.None));
        Assert.That(stream.ToArray(), Is.Empty);
    }

    [Test]
    public void ReaderAndWriterHonorPreCanceledOperations()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        OneByteReadStream inputStream = new OneByteReadStream(new byte[] { 65, 13, 10 });
        Pop3LineReader reader = new Pop3LineReader(new Pop3BufferedInput(inputStream, 1, 255));
        MemoryStream outputStream = new MemoryStream();
        Pop3LineWriter writer = new Pop3LineWriter(new Pop3BufferedOutput(outputStream, 2, 255));
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => reader.ReadCommandLine(source.Token));
        Assert.Throws<OperationCanceledException>(() => writer.WriteCommandLine(new byte[] { 65 }, source.Token));
        Assert.That(inputStream.ReadCallCount, Is.Zero);
        Assert.That(outputStream.ToArray(), Is.Empty);
    }

    [Test]
    public void ReaderAndWriterRejectNullDependencies()
    {
        Assert.Throws<ArgumentNullException>(() => new Pop3LineReader(null!));
        Assert.Throws<ArgumentNullException>(() => new Pop3LineWriter(null!));
    }

    private static Pop3LineReader CreateReader(byte[] wireData)
    {
        OneByteReadStream stream = new OneByteReadStream(wireData);
        Pop3BufferedInput input = new Pop3BufferedInput(stream, 8, 1024);
        return new Pop3LineReader(input);
    }

    private static byte[] CreateWireLine(int contentLength, byte value)
    {
        byte[] line = new byte[contentLength + 2];
        int index = 0;
        while (index < contentLength)
        {
            line[index] = value;
            index++;
        }
        line[contentLength] = 13;
        line[contentLength + 1] = 10;
        return line;
    }

    private static byte[] CreateContent(int contentLength, byte value)
    {
        byte[] content = new byte[contentLength];
        int index = 0;
        while (index < contentLength)
        {
            content[index] = value;
            index++;
        }
        return content;
    }

    private class OneByteReadStream : Stream
    {
        byte[] Data;
        int PositionValue;
        public int ReadCallCount;

        public OneByteReadStream(byte[] data)
        {
            Data = data;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return Data.Length; }
        }

        public override long Position
        {
            get { return PositionValue; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCallCount++;
            if (PositionValue == Data.Length)
            {
                return 0;
            }
            buffer[offset] = Data[PositionValue];
            PositionValue++;
            return 1;
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
            throw new NotSupportedException();
        }
    }
}
