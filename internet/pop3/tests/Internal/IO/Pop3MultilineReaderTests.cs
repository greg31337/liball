using System.Text;

namespace pop3.Tests;

public class Pop3MultilineReaderTests
{
    [Test]
    public void CopyStreamsFragmentedLinesUnstuffsOneDotAndPreservesCrlf()
    {
        byte[] wire = Encoding.ASCII.GetBytes("first\r\n..dot\r\n...two\r\n\r\n.\r\n+OK next\r\n");
        FragmentingReadStream source = new FragmentingReadStream(
            new MemoryStream(wire),
            1);
        Pop3BufferedInput input = new Pop3BufferedInput(source, 3, 64);
        Pop3LineReader lineReader = new Pop3LineReader(input);
        Pop3MultilineReader reader = new Pop3MultilineReader(lineReader);
        MemoryStream destination = new MemoryStream();

        long copied = reader.CopyMultilineData(
            destination,
            64,
            null,
            Pop3TransferKind.Retrieve,
            3,
            -1,
            CancellationToken.None);

        byte[] expected = Encoding.ASCII.GetBytes("first\r\n.dot\r\n..two\r\n\r\n");
        Assert.That(copied, Is.EqualTo(expected.Length));
        Assert.That(destination.ToArray(), Is.EqualTo(expected));
        Assert.That(lineReader.ReadResponseLine(CancellationToken.None), Is.EqualTo(Encoding.ASCII.GetBytes("+OK next")));
        Assert.That(destination.CanWrite, Is.True);
        Assert.That(source.ReadCallCount, Is.EqualTo(wire.Length));
    }

    [Test]
    public void TerminatorMustBeACompleteSingleDotLine()
    {
        Pop3MultilineReader reader = CreateReader("..\r\n...\r\n.\r\n");
        MemoryStream destination = new MemoryStream();

        long copied = reader.CopyMultilineData(
            destination,
            32,
            null,
            Pop3TransferKind.ExtensionData,
            0,
            -1,
            CancellationToken.None);

        Assert.That(copied, Is.EqualTo(7));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo(".\r\n..\r\n"));
    }

    [Test]
    public void CopySupportsDestinationsWithSmallMaximumWrites()
    {
        Pop3MultilineReader reader = CreateReader("a\r\n..x\r\n.\r\n");
        ShortWriteStream destination = new ShortWriteStream(2);

        long copied = reader.CopyMultilineData(
            destination,
            8,
            null,
            Pop3TransferKind.ExtensionData,
            0,
            -1,
            CancellationToken.None);

        Assert.That(copied, Is.EqualTo(7));
        Assert.That(
            Encoding.ASCII.GetString(destination.ToArray()),
            Is.EqualTo("a\r\n.x\r\n"));
        Assert.That(destination.WriteCallCount, Is.EqualTo(4));
    }

    [TestCase("")]
    [TestCase("line\r\n")]
    [TestCase("partial")]
    [TestCase(".\r")]
    public void CopyRejectsEofBeforeTheMultilineTerminator(string wire)
    {
        Pop3MultilineReader reader = CreateReader(wire);

        Assert.Throws<EndOfStreamException>(() => reader.CopyMultilineData(
            new MemoryStream(),
            64,
            null,
            Pop3TransferKind.Retrieve,
            1,
            -1,
            CancellationToken.None));
    }

    [Test]
    public void InjectedEofRejectsAnOtherwiseCompleteFollowingTerminator()
    {
        byte[] completeWire = Encoding.ASCII.GetBytes("one\r\n.\r\n+OK next\r\n");
        InjectedEofStream source = new InjectedEofStream(
            new MemoryStream(completeWire),
            6);
        Pop3MultilineReader reader = CreateReader(source);
        MemoryStream destination = new MemoryStream();

        Assert.Throws<EndOfStreamException>(() => reader.CopyMultilineData(
            destination,
            64,
            null,
            Pop3TransferKind.Retrieve,
            1,
            -1,
            CancellationToken.None));
        Assert.That(
            Encoding.ASCII.GetString(destination.ToArray()),
            Is.EqualTo("one\r\n"));
        Assert.That(source.ReadCallCount, Is.GreaterThan(1));
    }

    [TestCase("line\n.\r\n")]
    [TestCase("line\rX.\r\n")]
    public void CopyRejectsBareLineTerminators(string wire)
    {
        Pop3MultilineReader reader = CreateReader(wire);

        Assert.Throws<InvalidDataException>(() => reader.CopyMultilineData(
            new MemoryStream(),
            64,
            null,
            Pop3TransferKind.Retrieve,
            1,
            -1,
            CancellationToken.None));
    }

    [Test]
    public void CopyEnforcesTheConfiguredWireLineLimit()
    {
        Pop3MultilineReader exact = CreateReader("..a\r\n.\r\n");
        Pop3MultilineReader tooLong = CreateReader("..ab\r\n.\r\n");
        MemoryStream destination = new MemoryStream();

        Assert.That(exact.CopyMultilineData(
            destination,
            5,
            null,
            Pop3TransferKind.Retrieve,
            1,
            -1,
            CancellationToken.None), Is.EqualTo(4));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo(".a\r\n"));
        Assert.Throws<InvalidDataException>(() => tooLong.CopyMultilineData(
            new MemoryStream(),
            5,
            null,
            Pop3TransferKind.Retrieve,
            1,
            -1,
            CancellationToken.None));
    }

    [Test]
    public void BoundedReadAcceptsItsExactMemoryCeiling()
    {
        Pop3MultilineReader reader = CreateReader("abc\r\n\r\n.\r\n");

        byte[] result = reader.ReadMultilineData(
            64,
            7,
            null,
            Pop3TransferKind.Top,
            4,
            7,
            CancellationToken.None);

        Assert.That(Encoding.ASCII.GetString(result), Is.EqualTo("abc\r\n\r\n"));
    }

    [Test]
    public void BoundedReadRejectsPayloadBeyondItsMemoryCeiling()
    {
        Pop3MultilineReader reader = CreateReader("abc\r\n\r\n.\r\n");

        Assert.Throws<InvalidDataException>(() => reader.ReadMultilineData(
            64,
            6,
            null,
            Pop3TransferKind.Top,
            4,
            -1,
            CancellationToken.None));
    }

    [Test]
    public void EmptyPayloadFitsAZeroByteMemoryCeiling()
    {
        Pop3MultilineReader reader = CreateReader(".\r\n");

        byte[] result = reader.ReadMultilineData(
            3,
            0,
            null,
            Pop3TransferKind.List,
            0,
            0,
            CancellationToken.None);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ObserverReceivesPerLineAndCompletionProgress()
    {
        Pop3MultilineReader reader = CreateReader("one\r\ntwo\r\n.\r\n");
        ProgressObserver observer = new ProgressObserver();

        long copied = reader.CopyMultilineData(
            new MemoryStream(),
            64,
            observer,
            Pop3TransferKind.Retrieve,
            8,
            999,
            CancellationToken.None);

        Assert.That(copied, Is.EqualTo(10));
        Assert.That(observer.Reports, Has.Count.EqualTo(3));
        AssertProgress(observer.Reports[0], Pop3TransferKind.Retrieve, 8, 5, 999, false);
        AssertProgress(observer.Reports[1], Pop3TransferKind.Retrieve, 8, 10, 999, false);
        AssertProgress(observer.Reports[2], Pop3TransferKind.Retrieve, 8, 10, 999, true);
    }

    [Test]
    public void EmptyPayloadStillReportsSuccessfulCompletion()
    {
        Pop3MultilineReader reader = CreateReader(".\r\n");
        ProgressObserver observer = new ProgressObserver();

        reader.CopyMultilineData(
            new MemoryStream(),
            3,
            observer,
            Pop3TransferKind.Uidl,
            0,
            -1,
            CancellationToken.None);

        Assert.That(observer.Reports, Has.Count.EqualTo(1));
        AssertProgress(observer.Reports[0], Pop3TransferKind.Uidl, 0, 0, -1, true);
    }

    [Test]
    public void PreCanceledCopyDoesNotReadWriteOrReport()
    {
        FragmentingReadStream source = new FragmentingReadStream(
            new MemoryStream(Encoding.ASCII.GetBytes("data\r\n.\r\n")),
            8);
        Pop3MultilineReader reader = CreateReader(source);
        TrackingWriteStream destination = new TrackingWriteStream();
        ProgressObserver observer = new ProgressObserver();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => reader.CopyMultilineData(
            destination,
            64,
            observer,
            Pop3TransferKind.ExtensionData,
            0,
            -1,
            cancellation.Token));
        Assert.That(source.ReadCallCount, Is.Zero);
        Assert.That(destination.WriteCallCount, Is.Zero);
        Assert.That(observer.Reports, Is.Empty);
    }

    [Test]
    public void CancellationAfterAProgressReportStopsBeforeTheNextLine()
    {
        Pop3MultilineReader reader = CreateReader("first\r\nsecond\r\n.\r\n");
        MemoryStream destination = new MemoryStream();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        CancelingObserver observer = new CancelingObserver(cancellation);

        Assert.Throws<OperationCanceledException>(() => reader.CopyMultilineData(
            destination,
            64,
            observer,
            Pop3TransferKind.Retrieve,
            2,
            -1,
            cancellation.Token));
        Assert.That(Encoding.ASCII.GetString(destination.ToArray()), Is.EqualTo("first\r\n"));
        Assert.That(observer.ReportCount, Is.EqualTo(1));
    }

    [Test]
    public void ReaderRejectsInvalidDependenciesAndTransferMetadataBeforeReading()
    {
        FragmentingReadStream source = new FragmentingReadStream(
            new MemoryStream(Encoding.ASCII.GetBytes(".\r\n")),
            8);
        Pop3MultilineReader reader = CreateReader(source);
        MemoryStream destination = new MemoryStream();

        Assert.Throws<ArgumentNullException>(() => new Pop3MultilineReader(null!));
        Assert.Throws<ArgumentNullException>(() => reader.CopyMultilineData(
            null!, 2, null, Pop3TransferKind.List, 0, -1, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.CopyMultilineData(
            destination, 1, null, Pop3TransferKind.List, 0, -1, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.CopyMultilineData(
            destination, 2, null, Pop3TransferKind.List, 0, -1, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => reader.CopyMultilineData(
            new MemoryStream(new byte[0], false), 3, null, Pop3TransferKind.List, 0, -1, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.CopyMultilineData(
            destination, 2, null, (Pop3TransferKind)99, 0, -1, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.CopyMultilineData(
            destination, 2, null, Pop3TransferKind.List, -1, -1, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.CopyMultilineData(
            destination, 2, null, Pop3TransferKind.List, 0, -2, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadMultilineData(
            2, -1, null, Pop3TransferKind.List, 0, -1, CancellationToken.None));
        Assert.That(source.ReadCallCount, Is.Zero);
    }

    static Pop3MultilineReader CreateReader(string wire)
    {
        return CreateReader(new FragmentingReadStream(
            new MemoryStream(Encoding.ASCII.GetBytes(wire)),
            2));
    }

    static Pop3MultilineReader CreateReader(Stream stream)
    {
        Pop3BufferedInput input = new Pop3BufferedInput(stream, 4, 128);
        return new Pop3MultilineReader(new Pop3LineReader(input));
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

    private class ProgressObserver : IPop3TransferObserver
    {
        public List<Pop3TransferProgress> Reports = new List<Pop3TransferProgress>();

        public void Report(Pop3TransferProgress progress)
        {
            Reports.Add(progress);
        }
    }

    private class CancelingObserver : IPop3TransferObserver
    {
        CancellationTokenSource Cancellation;
        public int ReportCount;

        public CancelingObserver(CancellationTokenSource cancellation)
        {
            Cancellation = cancellation;
        }

        public void Report(Pop3TransferProgress progress)
        {
            ReportCount++;
            Cancellation.Cancel();
        }
    }

    private class TrackingWriteStream : MemoryStream
    {
        public int WriteCallCount;

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteCallCount++;
            base.Write(buffer, offset, count);
        }
    }
}
