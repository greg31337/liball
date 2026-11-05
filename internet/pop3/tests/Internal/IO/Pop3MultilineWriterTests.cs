using System.Text;

namespace pop3.Tests;

public class Pop3MultilineWriterTests
{
    [Test]
    public void WriterStreamsFragmentedMixedLineEndingsAndDotStuffsEveryDataLine()
    {
        byte[] sourceBytes = Encoding.ASCII.GetBytes("first\n.second\rthird\r\n.\r\nlast");
        FragmentingReadStream source = new FragmentingReadStream(
            new MemoryStream(sourceBytes),
            1);
        MemoryStream destination = new MemoryStream();
        Pop3MultilineWriter writer = CreateWriter(destination, 3);

        long transmitted = writer.WriteMultilineData(source, 64, CancellationToken.None);

        Assert.That(transmitted, Is.EqualTo(32));
        Assert.That(
            Encoding.ASCII.GetString(destination.ToArray()),
            Is.EqualTo("first\r\n..second\r\nthird\r\n..\r\nlast\r\n.\r\n"));
        Assert.That(source.ReadCallCount, Is.EqualTo(sourceBytes.Length + 1));
        Assert.That(source.CanRead, Is.True);
    }

    [Test]
    public void EmptySourceWritesOnlyOneTerminator()
    {
        MemoryStream source = new MemoryStream();
        MemoryStream destination = new MemoryStream();

        long transmitted = CreateWriter(destination, 2).WriteMultilineData(source, 3, CancellationToken.None);

        Assert.That(transmitted, Is.Zero);
        Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 46, 13, 10 }));
        Assert.That(source.CanRead, Is.True);
    }

    [Test]
    public void ExistingSingleDotLineIsStuffedBeforeTheGeneratedTerminator()
    {
        MemoryStream source = new MemoryStream(Encoding.ASCII.GetBytes(".\r\n"));
        MemoryStream destination = new MemoryStream();

        long transmitted = CreateWriter(destination, 2).WriteMultilineData(source, 3, CancellationToken.None);

        Assert.That(transmitted, Is.EqualTo(3));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo("..\r\n.\r\n"));
    }

    [TestCase("line\r\n", "line\r\n.\r\n", 6)]
    [TestCase("line\n", "line\r\n.\r\n", 6)]
    [TestCase("line\r", "line\r\n.\r\n", 6)]
    [TestCase("line", "line\r\n.\r\n", 6)]
    [TestCase("\r\n", "\r\n.\r\n", 2)]
    [TestCase("\n", "\r\n.\r\n", 2)]
    [TestCase("\r", "\r\n.\r\n", 2)]
    [TestCase("\r\r\n", "\r\n\r\n.\r\n", 4)]
    public void WriterNormalizesEverySourceLineEnding(
        string sourceText,
        string expectedWireText,
        int expectedTransmittedOctets)
    {
        MemoryStream source = new MemoryStream(Encoding.ASCII.GetBytes(sourceText));
        MemoryStream destination = new MemoryStream();

        long transmitted = CreateWriter(destination, 4).WriteMultilineData(source, 32, CancellationToken.None);

        Assert.That(transmitted, Is.EqualTo(expectedTransmittedOctets));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo(expectedWireText));
    }

    [Test]
    public void TransmittedCountExcludesStuffingAndTerminatorOctets()
    {
        MemoryStream source = new MemoryStream(Encoding.ASCII.GetBytes(".a\n..b\n"));
        MemoryStream destination = new MemoryStream();

        long transmitted = CreateWriter(destination, 3).WriteMultilineData(source, 16, CancellationToken.None);

        Assert.That(transmitted, Is.EqualTo(9));
        Assert.That(destination.Length, Is.EqualTo(14));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo("..a\r\n...b\r\n.\r\n"));
    }

    [Test]
    public void WriterAcceptsTheExactNormalizedSourceLineLimitIncludingDotStuffingOverhead()
    {
        MemoryStream source = new MemoryStream(Encoding.ASCII.GetBytes(".ab\n"));
        MemoryStream destination = new MemoryStream();

        long transmitted = CreateWriter(destination, 2).WriteMultilineData(source, 5, CancellationToken.None);

        Assert.That(transmitted, Is.EqualTo(5));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo("..ab\r\n.\r\n"));
    }

    [Test]
    public void WriterRejectsAnOversizedSourceLineBeforeWritingThatLine()
    {
        MemoryStream firstLineTooLong = new MemoryStream(Encoding.ASCII.GetBytes("abcd\n"));
        MemoryStream laterLineTooLong = new MemoryStream(Encoding.ASCII.GetBytes("ok\nabcd\n"));
        MemoryStream firstDestination = new MemoryStream();
        MemoryStream laterDestination = new MemoryStream();

        Assert.Throws<InvalidDataException>(() => CreateWriter(firstDestination, 2).WriteMultilineData(
            firstLineTooLong, 5, CancellationToken.None));
        Assert.Throws<InvalidDataException>(() => CreateWriter(laterDestination, 2).WriteMultilineData(
            laterLineTooLong, 5, CancellationToken.None));
        Assert.That(firstDestination.ToArray(), Is.Empty);
        Assert.That(Encoding.ASCII.GetString(laterDestination.ToArray()), Is.EqualTo("ok\r\n"));
    }

    [Test]
    public void BufferedOutputHandlesDestinationsWithSmallMaximumWrites()
    {
        ShortWriteStream destination = new ShortWriteStream(2);
        MemoryStream source = new MemoryStream(Encoding.ASCII.GetBytes("alpha\n.beta"));

        long transmitted = CreateWriter(destination, 2).WriteMultilineData(source, 16, CancellationToken.None);

        Assert.That(transmitted, Is.EqualTo(14));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo("alpha\r\n..beta\r\n.\r\n"));
        Assert.That(destination.WriteCallCount, Is.GreaterThan(1));
        Assert.That(destination.FlushCallCount, Is.EqualTo(3));
    }

    [Test]
    public void CrLfSplitAcrossSourceReadsRemainsOneLineEnding()
    {
        FragmentingReadStream source = new FragmentingReadStream(
            new MemoryStream(Encoding.ASCII.GetBytes("a\r\nb")),
            2);
        MemoryStream destination = new MemoryStream();

        long transmitted = CreateWriter(destination, 3).WriteMultilineData(source, 8, CancellationToken.None);

        Assert.That(transmitted, Is.EqualTo(6));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo("a\r\nb\r\n.\r\n"));
    }

    [Test]
    public void PreCanceledWriteDoesNotReadOrWrite()
    {
        FragmentingReadStream source = new FragmentingReadStream(
            new MemoryStream(Encoding.ASCII.GetBytes("data")),
            4);
        ShortWriteStream destination = new ShortWriteStream(4);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => CreateWriter(destination, 4).WriteMultilineData(
            source, 16, cancellation.Token));
        Assert.That(source.ReadCallCount, Is.Zero);
        Assert.That(destination.ToArray(), Is.Empty);
    }

    [Test]
    public void CancellationDuringSourceReadsStopsBeforeTerminator()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        FragmentingReadStream fragments = new FragmentingReadStream(
            new MemoryStream(Encoding.ASCII.GetBytes("one\nsecond\n")),
            4);
        CancellationInjectingStream source = new CancellationInjectingStream(
            fragments,
            cancellation,
            2,
            -1,
            true);
        MemoryStream destination = new MemoryStream();

        Assert.Throws<OperationCanceledException>(() =>
            CreateWriter(destination, 3).WriteMultilineData(
                source,
                16,
                cancellation.Token));
        Assert.That(
            Encoding.ASCII.GetString(destination.ToArray()),
            Is.EqualTo("one\r\n"));
        Assert.That(source.ReadCallCount, Is.EqualTo(2));
    }

    [Test]
    public void LargeFragmentedPayloadStreamsThroughWriterAndReaderExactly()
    {
        StringBuilder sourceText = new StringBuilder();
        StringBuilder normalizedText = new StringBuilder();
        int lineNumber = 0;
        while (lineNumber < 4096)
        {
            string prefix = lineNumber % 8 == 0 ? "." : "";
            string line = prefix + lineNumber.ToString("D4") + ":" +
                new string((char)('a' + lineNumber % 26), 240);
            sourceText.Append(line);
            normalizedText.Append(line);
            normalizedText.Append("\r\n");
            if (lineNumber % 3 == 0)
            {
                sourceText.Append('\n');
            }
            else if (lineNumber % 3 == 1)
            {
                sourceText.Append('\r');
            }
            else
            {
                sourceText.Append("\r\n");
            }
            lineNumber++;
        }

        byte[] sourceBytes = Encoding.ASCII.GetBytes(sourceText.ToString());
        byte[] expectedBytes = Encoding.ASCII.GetBytes(normalizedText.ToString());
        FragmentingReadStream source = new FragmentingReadStream(
            new MemoryStream(sourceBytes),
            13);
        MemoryStream wire = new MemoryStream();

        long written = CreateWriter(wire, 17).WriteMultilineData(
            source,
            260,
            CancellationToken.None);

        wire.Position = 0;
        FragmentingReadStream fragmentedWire = new FragmentingReadStream(
            wire,
            11,
            true);
        Pop3BufferedInput input = new Pop3BufferedInput(fragmentedWire, 19, 260);
        Pop3MultilineReader reader = new Pop3MultilineReader(
            new Pop3LineReader(input));
        MemoryStream destination = new MemoryStream();
        long read = reader.CopyMultilineData(
            destination,
            260,
            null,
            Pop3TransferKind.ExtensionData,
            0,
            -1,
            CancellationToken.None);

        Assert.That(sourceBytes.Length, Is.GreaterThan(1_000_000));
        Assert.That(written, Is.EqualTo(expectedBytes.Length));
        Assert.That(read, Is.EqualTo(expectedBytes.Length));
        Assert.That(destination.ToArray(), Is.EqualTo(expectedBytes));
        Assert.That(source.ReadCallCount, Is.GreaterThan(1000));
        Assert.That(fragmentedWire.ReadCallCount, Is.GreaterThan(1000));
    }

    [Test]
    public void SourceFailureDoesNotEmitTheMultilineTerminator()
    {
        ThrowingReadStream source = new ThrowingReadStream();
        MemoryStream destination = new MemoryStream();

        Assert.Throws<IOException>(() => CreateWriter(destination, 2).WriteMultilineData(
            source, 16, CancellationToken.None));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo("first\r\n"));
    }

    [Test]
    public void ObservedWriteReportsCompletedLinesBeforeFinalCompletion()
    {
        MemoryStream source = new MemoryStream(Encoding.ASCII.GetBytes("one\n.two"));
        MemoryStream destination = new MemoryStream();
        RecordingObserver observer = new RecordingObserver();
        Pop3MultilineWriter writer = CreateWriter(destination, 3);

        long transmitted = writer.WriteObservedMultilineData(
            source,
            16,
            observer,
            Pop3TransferKind.Retrieve,
            9,
            11,
            CancellationToken.None);

        Assert.That(transmitted, Is.EqualTo(11));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo("one\r\n..two\r\n.\r\n"));
        Assert.That(observer.Reports, Has.Count.EqualTo(3));
        AssertProgress(observer.Reports[0], Pop3TransferKind.Retrieve, 9, 5, 11, false);
        AssertProgress(observer.Reports[1], Pop3TransferKind.Retrieve, 9, 11, 11, false);
        AssertProgress(observer.Reports[2], Pop3TransferKind.Retrieve, 9, 11, 11, true);
        Assert.That(source.CanRead, Is.True);
    }

    [Test]
    public void ObservedEmptyWriteReportsCompletionAfterTheTerminator()
    {
        MemoryStream source = new MemoryStream();
        MemoryStream destination = new MemoryStream();
        CompletionObserver observer = new CompletionObserver(destination);

        long transmitted = CreateWriter(destination, 2).WriteObservedMultilineData(
            source,
            3,
            observer,
            Pop3TransferKind.ExtensionData,
            0,
            -1,
            CancellationToken.None);

        Assert.That(transmitted, Is.Zero);
        Assert.That(observer.CallCount, Is.EqualTo(1));
        Assert.That(observer.SawCompleteTerminator, Is.True);
        Assert.That(source.CanRead, Is.True);
    }

    [Test]
    public void ObservedWriteValidatesMetadataBeforeReadingTheCallerStream()
    {
        FragmentingReadStream source = new FragmentingReadStream(
            new MemoryStream(Encoding.ASCII.GetBytes("data")),
            4);
        MemoryStream destination = new MemoryStream();
        Pop3MultilineWriter writer = CreateWriter(destination, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteObservedMultilineData(
            source, 16, null, Pop3TransferKind.Retrieve, 0, -1, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteObservedMultilineData(
            source, 16, null, Pop3TransferKind.List, 0, -2, CancellationToken.None));
        Assert.That(source.ReadCallCount, Is.Zero);
        Assert.That(destination.ToArray(), Is.Empty);
    }

    [Test]
    public void WriterRejectsInvalidDependenciesAndArgumentsBeforeReading()
    {
        FragmentingReadStream source = new FragmentingReadStream(
            new MemoryStream(),
            1);
        MemoryStream destination = new MemoryStream();
        Pop3MultilineWriter writer = CreateWriter(destination, 2);

        Assert.Throws<ArgumentNullException>(() => new Pop3MultilineWriter(null!));
        Assert.Throws<ArgumentNullException>(() => writer.WriteMultilineData(null!, 3, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => writer.WriteMultilineData(
            new ShortWriteStream(1), 3, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.WriteMultilineData(source, 2, CancellationToken.None));
        Assert.That(source.ReadCallCount, Is.Zero);
    }

    static Pop3MultilineWriter CreateWriter(Stream destination, int bufferSize)
    {
        Pop3BufferedOutput output = new Pop3BufferedOutput(destination, bufferSize, bufferSize);
        return new Pop3MultilineWriter(new Pop3LineWriter(output));
    }

    static void AssertProgress(
        Pop3TransferProgress progress,
        Pop3TransferKind kind,
        int messageNumber,
        long bytesTransferred,
        long expectedBytes,
        bool isComplete)
    {
        Assert.That(progress.Kind, Is.EqualTo(kind));
        Assert.That(progress.MessageNumber, Is.EqualTo(messageNumber));
        Assert.That(progress.BytesTransferred, Is.EqualTo(bytesTransferred));
        Assert.That(progress.ExpectedBytes, Is.EqualTo(expectedBytes));
        Assert.That(progress.IsComplete, Is.EqualTo(isComplete));
    }

    private class RecordingObserver : IPop3TransferObserver
    {
        public List<Pop3TransferProgress> Reports = new List<Pop3TransferProgress>();

        public void Report(Pop3TransferProgress progress)
        {
            Reports.Add(progress);
        }
    }

    private class CompletionObserver : IPop3TransferObserver
    {
        MemoryStream Destination;
        public int CallCount;
        public bool SawCompleteTerminator;

        public CompletionObserver(MemoryStream destination)
        {
            Destination = destination;
        }

        public void Report(Pop3TransferProgress progress)
        {
            CallCount++;
            if (progress.IsComplete)
            {
                SawCompleteTerminator = Encoding.ASCII.GetString(Destination.ToArray()) == ".\r\n";
            }
        }
    }

    private class ThrowingReadStream : Stream
    {
        MemoryStream FirstRead = new MemoryStream(Encoding.ASCII.GetBytes("first\n"));
        bool HasRead;

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
            get { return FirstRead.Length; }
        }

        public override long Position
        {
            get { return FirstRead.Position; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (HasRead)
            {
                throw new IOException("source failed");
            }
            HasRead = true;
            return FirstRead.Read(buffer, offset, count);
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
