using System.Diagnostics;
using System.Text;
using System.Threading;
using email;

namespace email.Tests;

public class EmailFoundationHardeningTests
{
    [Test]
    public void EveryLineChunkPartitionPreservesMixedNewlineBytes()
    {
        byte[] input = Encoding.ASCII.GetBytes("A\r\nB\nC\rD");
        int partitionCount = 1 << (input.Length - 1);
        int partition = 0;
        while (partition < partitionCount)
        {
            List<EmailLine> lines = ReadLinePartition(input, partition);

            Assert.That(Reassemble(lines), Is.EqualTo(input), "partition " + partition);
            Assert.That(lines, Has.Count.EqualTo(4), "partition " + partition);
            Assert.That(lines[0].GetLineEnding(), Is.EqualTo(EmailLineEnding.CrLf), "partition " + partition);
            Assert.That(lines[1].GetLineEnding(), Is.EqualTo(EmailLineEnding.Lf), "partition " + partition);
            Assert.That(lines[2].GetLineEnding(), Is.EqualTo(EmailLineEnding.Cr), "partition " + partition);
            Assert.That(lines[3].GetLineEnding(), Is.EqualTo(EmailLineEnding.None), "partition " + partition);
            partition++;
        }
    }

    [Test]
    public void EverySpoolingChunkPartitionPreservesBytesAndCleansTransferredFile()
    {
        byte[] input = new byte[] { 1, 2, 3, 4, 5, 6 };
        int partitionCount = 1 << (input.Length - 1);
        string directory = CreateTemporaryTestDirectory();
        try
        {
            int partition = 0;
            while (partition < partitionCount)
            {
                EmailTemporaryStore store = new EmailTemporaryStore(CreateStorageOptions(directory, 2, 20));
                WritePartition(store, input, partition);
                Assert.That(store.IsUsingTemporaryFile(), Is.True, "partition " + partition);

                EmailContent content = store.Complete();
                string path = content.GetFilePath()!;
                Assert.That(content.Materialize(input.Length), Is.EqualTo(input), "partition " + partition);
                Assert.That(File.Exists(path), Is.True, "partition " + partition);
                store.Close();
                Assert.That(File.Exists(path), Is.True, "transferred ownership " + partition);
                content.Close();
                Assert.That(File.Exists(path), Is.False, "content cleanup " + partition);
                Assert.That(Directory.GetFiles(directory), Is.Empty, "directory cleanup " + partition);
                partition++;
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ExplicitCloseCleansEveryPossibleSpoolingPrefix()
    {
        byte[] input = new byte[] { 1, 2, 3, 4, 5, 6 };
        string directory = CreateTemporaryTestDirectory();
        try
        {
            int prefixLength = 0;
            while (prefixLength <= input.Length)
            {
                EmailTemporaryStore store = new EmailTemporaryStore(CreateStorageOptions(directory, 2, 20));
                store.Write(new ReadOnlySpan<byte>(input, 0, prefixLength));
                store.Close();

                Assert.That(store.GetLength(), Is.EqualTo(prefixLength), "prefix " + prefixLength);
                Assert.That(store.IsClosed(), Is.True, "prefix " + prefixLength);
                Assert.That(Directory.GetFiles(directory), Is.Empty, "prefix cleanup " + prefixLength);
                prefixLength++;
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void OverflowFailuresPreserveFoundationObjectStateAndCleanup()
    {
        EmailByteCounter counter = new EmailByteCounterOperations().CreateWithInitialByteCount(long.MaxValue);
        Assert.Throws<OverflowException>(() => counter.Add(1));
        Assert.That(counter.GetByteCount(), Is.EqualTo(long.MaxValue));

        EmailProgress progress = new EmailProgressOperations().CreateWithInitialValues(new EmailProgressSnapshot(long.MaxValue, 0, 0, 0, 0, 0));
        Assert.Throws<OverflowException>(() => progress.AddSourceBytesRead(1));
        Assert.That(progress.GetSourceBytesRead(), Is.EqualTo(long.MaxValue));

        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(new EmailSourcePosition(long.MaxValue - 1, 1, 1));
        Assert.Throws<OverflowException>(() => tracker.Advance(new byte[] { 1, 2 }));
        Assert.That(tracker.GetByteOffset(), Is.EqualTo(long.MaxValue - 1));
        Assert.That(tracker.GetColumnNumber(), Is.EqualTo(1));

        EmailBuffer buffer = new EmailBufferOperations().CreateWithInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(0, 2, true);
        buffer.Append(new byte[] { 1, 2 });
        Assert.Throws<EmailLimitException>(() => buffer.AppendByte(3));
        Assert.That(buffer.ToArray(), Is.EqualTo(new byte[] { 1, 2 }));
        buffer.Close();
    }

    [Test]
    public void CancellationAndProgressCanCoordinateOneSynchronousLoop()
    {
        EmailCancellation cancellation = new EmailCancellation();
        EmailProgress progress = new EmailProgressOperations().CreateWith();
        ManualResetEventSlim started = new ManualResetEventSlim(false);
        Thread worker = new Thread(() =>
        {
            started.Set();
            while (!cancellation.IsCancellationRequested())
            {
                progress.AddOperationCount(1);
            }
        });

        worker.Start();
        bool didStart = started.Wait(5000);
        Stopwatch timeout = Stopwatch.StartNew();
        while (progress.GetOperationCount() == 0 && timeout.ElapsedMilliseconds < 5000)
        {
            Thread.Yield();
        }

        cancellation.Cancel();
        bool didStop = worker.Join(5000);
        started.Dispose();

        Assert.That(didStart, Is.True);
        Assert.That(didStop, Is.True);
        Assert.That(cancellation.IsCancellationRequested(), Is.True);
        Assert.That(progress.GetOperationCount(), Is.GreaterThan(0));
        Assert.That(progress.GetSnapshot().GetOperationCount(), Is.EqualTo(progress.GetOperationCount()));
    }

    [Test]
    public void OwnershipBoundariesRemainExplicitAcrossHashingAndClose()
    {
        MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes("owned by caller"));
        EmailContent borrowed = new EmailContentOperations().CreateStream(stream, EmailContentOwnership.Borrowed);

        EmailHashResult hash = new EmailHash().ComputeExactWithContentAndAlgorithmAndMaximumBytes(borrowed, EmailHashAlgorithm.Sha256, stream.Length);
        borrowed.Close();

        Assert.That(hash.GetHashedByteCount(), Is.EqualTo(stream.Length));
        Assert.That(stream.CanRead, Is.True);
        stream.Close();

        byte[] sensitive = new byte[] { 1, 2, 3 };
        EmailContent owned = new EmailContentOperations().CreateMemory(sensitive, EmailContentOwnership.Owned, true);
        owned.Close();
        Assert.That(sensitive, Is.All.Zero);
    }

    static List<EmailLine> ReadLinePartition(byte[] input, int partition)
    {
        EmailLineReader reader = new EmailLineReader(100);
        List<EmailLine> lines = new List<EmailLine>();
        int start = 0;
        int boundary = 0;
        while (boundary < input.Length - 1)
        {
            if ((partition & 1 << boundary) != 0)
            {
                FeedLineChunk(reader, input, start, boundary + 1 - start, false, lines);
                start = boundary + 1;
            }

            boundary++;
        }

        FeedLineChunk(reader, input, start, input.Length - start, true, lines);
        return lines;
    }

    static void FeedLineChunk(EmailLineReader reader, byte[] input, int start, int count, bool endOfInput, List<EmailLine> lines)
    {
        int offset = start;
        int end = start + count;
        int iterations = 0;
        while (offset < end)
        {
            EmailLineReadResult result = reader.Read(new ReadOnlySpan<byte>(input, offset, end - offset), endOfInput);
            offset += result.GetConsumedByteCount();
            if (result.HasLine())
            {
                lines.Add(result.GetLine()!);
            }

            if (result.GetStatus() == EmailLineReadStatus.NeedMoreData)
            {
                Assert.That(offset, Is.EqualTo(end));
                break;
            }

            iterations++;
            Assert.That(iterations, Is.LessThan(input.Length + 2));
        }

        if (endOfInput)
        {
            while (true)
            {
                EmailLineReadResult finalResult = reader.Read(Array.Empty<byte>(), true);
                if (finalResult.HasLine())
                {
                    lines.Add(finalResult.GetLine()!);
                }

                if (finalResult.GetStatus() == EmailLineReadStatus.EndOfInput)
                {
                    break;
                }
            }
        }
    }

    static byte[] Reassemble(List<EmailLine> lines)
    {
        using (MemoryStream output = new MemoryStream())
        {
            int index = 0;
            while (index < lines.Count)
            {
                EmailLine line = lines[index];
                byte[] content = line.GetContentBytes();
                output.Write(content, 0, content.Length);
                if (line.GetLineEnding() == EmailLineEnding.CrLf)
                {
                    output.WriteByte(13);
                    output.WriteByte(10);
                }
                else if (line.GetLineEnding() == EmailLineEnding.Cr)
                {
                    output.WriteByte(13);
                }
                else if (line.GetLineEnding() == EmailLineEnding.Lf)
                {
                    output.WriteByte(10);
                }

                index++;
            }

            return output.ToArray();
        }
    }

    static void WritePartition(EmailTemporaryStore store, byte[] input, int partition)
    {
        int start = 0;
        int boundary = 0;
        while (boundary < input.Length - 1)
        {
            if ((partition & 1 << boundary) != 0)
            {
                store.Write(new ReadOnlySpan<byte>(input, start, boundary + 1 - start));
                start = boundary + 1;
            }

            boundary++;
        }

        store.Write(new ReadOnlySpan<byte>(input, start, input.Length - start));
    }

    static EmailStorageOptions CreateStorageOptions(string directory, long threshold, long maximumTemporaryBytes)
    {
        EmailTemporaryStorageLimits limits = new EmailTemporaryStorageLimits();
        limits.SetMemoryThresholdBytes(threshold);
        limits.SetMaxTemporaryBytes(maximumTemporaryBytes);
        EmailStorageOptions options = new EmailStorageOptions();
        options.SetLimits(limits);
        options.SetTemporaryDirectory(directory);
        options.SetCopyBufferBytes(2);
        return options;
    }

    static string CreateTemporaryTestDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "email-foundation-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
