using System.Security.Cryptography;

namespace ftp.Tests;

public class TransferPumpContractTests
{
    [Test]
    public void Partial_reads_and_multi_chunk_writes_preserve_order_counts_progress_and_digest()
    {
        byte[] content = Enumerable.Range(1, 17)
            .Select(value => (byte)value)
            .ToArray();
        PartialReadStream source = new PartialReadStream(
            content, new[] { 1, 3, 2, 5, 1, 4, 1 });
        TrackingDestination destination = new TrackingDestination(
            null, null, null);
        SplittingTransform transform = new SplittingTransform();
        List<FtpTransferProgress> progress =
            new List<FtpTransferProgress>();
        FtpTransferPumpRequest request = CreateRequest(
            source, destination, transform, true, true);
        request.InitialLogicalBytes = 11;
        request.InitialWireBytes = 23;
        request.HasExpectedBytes = true;
        request.ExpectedBytes = content.Length + 11;
        request.ProgressCallback = snapshot => progress.Add(snapshot);

        FtpTransferPumpResult result = TransferPump.Execute(request);
        byte[] expected = Expand(content);
        FtpTransferHashRequest hashRequest = new FtpTransferHashRequest();
        hashRequest.Source = new MemoryStream(destination.ToArray());
        hashRequest.Algorithm = "SHA-256";
        hashRequest.BufferBytes = 3;
        hashRequest.LeaveOpen = false;
        FtpTransferHashResult digest = TransferHash.Calculate(hashRequest);
        string expectedDigest = Convert.ToHexString(
            SHA256.HashData(expected));

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(expected));
            Assert.That(source.ReadCalls, Is.GreaterThan(3));
            Assert.That(source.LargestRequestedRead, Is.EqualTo(4));
            Assert.That(destination.WriteCalls, Is.EqualTo(expected.Length));
            Assert.That(destination.LargestWrite, Is.EqualTo(1));
            Assert.That(result.SourceBytes, Is.EqualTo(content.Length));
            Assert.That(result.DestinationBytes, Is.EqualTo(expected.Length));
            Assert.That(result.Progress.LogicalBytes,
                Is.EqualTo(content.Length + 11));
            Assert.That(result.Progress.WireBytes,
                Is.EqualTo(expected.Length + 23));
            Assert.That(progress.First().LogicalBytes, Is.EqualTo(11));
            Assert.That(progress.First().WireBytes, Is.EqualTo(23));
            Assert.That(progress.Last().Completed, Is.True);
            Assert.That(progress.Select(item => item.LogicalBytes),
                Is.Ordered.Ascending);
            Assert.That(progress.Select(item => item.WireBytes),
                Is.Ordered.Ascending);
            Assert.That(digest.BytesRead, Is.EqualTo(expected.Length));
            Assert.That(digest.Checksum, Is.EqualTo(expectedDigest));
        });
    }

    [Test]
    public void Cancellation_between_partial_writes_stops_before_the_next_chunk_and_cleans_up()
    {
        using CancellationTokenSource cancellation =
            new CancellationTokenSource();
        List<string> operations = new List<string>();
        PartialReadStream source = new PartialReadStream(
            new byte[] { 1, 2, 3, 4 }, new[] { 4 }, operations);
        TrackingDestination destination = new TrackingDestination(
            cancellation, null, operations);
        FtpTransferPumpRequest request = CreateRequest(
            source, destination, new SplittingTransform(), false, false);
        request.Options!.Timeouts.CancellationToken = cancellation.Token;

        OperationCanceledException? failure =
            Assert.Throws<OperationCanceledException>(() =>
                TransferPump.Execute(request));

        Assert.Multiple(() =>
        {
            Assert.That(failure!.CancellationToken,
                Is.EqualTo(cancellation.Token));
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1 }));
            Assert.That(destination.WriteCalls, Is.EqualTo(1));
            Assert.That(destination.FlushCalls, Is.EqualTo(1));
            Assert.That(destination.Disposed, Is.True);
            Assert.That(source.Disposed, Is.True);
            Assert.That(operations.IndexOf("destination-flush"),
                Is.LessThan(operations.IndexOf("destination-dispose")));
            Assert.That(operations.IndexOf("destination-dispose"),
                Is.LessThan(operations.IndexOf("source-dispose")));
        });
    }

    [Test]
    public void Deadline_expiry_between_partial_writes_stops_before_the_next_chunk()
    {
        List<string> operations = new List<string>();
        PartialReadStream source = new PartialReadStream(
            new byte[] { 1, 2, 3, 4 }, new[] { 4 });
        TrackingDestination destination = new TrackingDestination(
            null, TimeSpan.FromMilliseconds(50), operations);
        FtpTransferPumpRequest request = CreateRequest(
            source, destination, new SplittingTransform(), false, false);
        request.DeadlineMilliseconds = 5;

        Assert.Throws<TimeoutException>(() =>
            TransferPump.Execute(request));

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1 }));
            Assert.That(destination.WriteCalls, Is.EqualTo(1));
            Assert.That(destination.FlushCalls, Is.EqualTo(1));
            Assert.That(destination.Disposed, Is.True);
            Assert.That(source.Disposed, Is.True);
        });
    }

    [TestCase("initial", 0)]
    [TestCase("progress", 1)]
    [TestCase("completed", 5)]
    public void Observer_failures_at_each_phase_are_isolated_after_cleanup(
        string phase,
        int expectedDestinationBytes)
    {
        InvalidOperationException observerFailure =
            new InvalidOperationException(phase + " observer failed");
        PartialReadStream source = new PartialReadStream(
            new byte[] { 1, 2 }, new[] { 2 });
        TrackingDestination destination = new TrackingDestination(
            null, null, null);
        FtpTransferPumpRequest request = CreateRequest(
            source, destination, new SplittingTransform(), false, false);
        int callbacks = 0;
        request.ProgressCallback = progress =>
        {
            callbacks++;
            if ((phase == "initial" && callbacks == 1) ||
                (phase == "progress" && !progress.Completed &&
                    progress.LogicalBytes != 0) ||
                (phase == "completed" && progress.Completed))
            {
                throw observerFailure;
            }
        };

        FtpTransferException? failure =
            Assert.Throws<FtpTransferException>(() =>
                TransferPump.Execute(request));

        Assert.Multiple(() =>
        {
            Assert.That(failure!.InnerException,
                Is.SameAs(observerFailure));
            Assert.That(destination.BytesWritten,
                Is.EqualTo(expectedDestinationBytes));
            Assert.That(destination.FlushCalls, Is.EqualTo(1));
            Assert.That(destination.Disposed, Is.True);
            Assert.That(source.Disposed, Is.True);
        });
    }

    private static FtpTransferPumpRequest CreateRequest(
        Stream source,
        Stream destination,
        IFtpTransferPumpTransform transform,
        bool leaveSourceOpen,
        bool leaveDestinationOpen)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.BufferBytes = 4;
        FtpTransferPumpRequest request = new FtpTransferPumpRequest();
        request.Source = source;
        request.Destination = destination;
        request.Options = options;
        request.Transform = transform;
        request.Direction = FtpTransferDirection.Upload;
        request.Path = "/partial.bin";
        request.LeaveSourceOpen = leaveSourceOpen;
        request.LeaveDestinationOpen = leaveDestinationOpen;
        return request;
    }

    private static byte[] Expand(byte[] content)
    {
        byte[] expanded = new byte[content.Length * 2 + 1];
        for (int index = 0; index < content.Length; index++)
        {
            expanded[index * 2] = content[index];
            expanded[index * 2 + 1] = (byte)(content[index] ^ 0xFF);
        }
        expanded[expanded.Length - 1] = 0xEE;
        return expanded;
    }

    private static FtpTransferPumpChunk CreateChunk(
        byte value,
        long logicalBytes)
    {
        FtpTransferPumpChunk chunk = new FtpTransferPumpChunk();
        chunk.Data = new byte[] { value };
        chunk.Count = 1;
        chunk.LogicalBytes = logicalBytes;
        chunk.WireBytes = 1;
        return chunk;
    }

    private class SplittingTransform : IFtpTransferPumpTransform
    {
        public FtpTransferPumpChunk[] TransformChunk(
            byte[] data,
            int count,
            bool finalChunk)
        {
            if (finalChunk)
            {
                return new[] { CreateChunk(0xEE, 0) };
            }
            FtpTransferPumpChunk[] chunks =
                new FtpTransferPumpChunk[count * 2];
            for (int index = 0; index < count; index++)
            {
                chunks[index * 2] = CreateChunk(data[index], 1);
                chunks[index * 2 + 1] = CreateChunk(
                    (byte)(data[index] ^ 0xFF), 0);
            }
            return chunks;
        }
    }

    private class PartialReadStream : MemoryStream
    {
        private readonly int[] Chunks;
        private int ChunkIndex;
        private readonly List<string>? Operations;

        internal bool Disposed;
        internal int LargestRequestedRead;
        internal int ReadCalls;

        internal PartialReadStream(byte[] content, int[] chunks)
            : base(content)
        {
            Chunks = chunks;
            ChunkIndex = 0;
            Operations = null;
            Disposed = false;
            LargestRequestedRead = 0;
            ReadCalls = 0;
        }

        internal PartialReadStream(
            byte[] content,
            int[] chunks,
            List<string> operations)
            : base(content)
        {
            Chunks = chunks;
            ChunkIndex = 0;
            Operations = operations;
            Disposed = false;
            LargestRequestedRead = 0;
            ReadCalls = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls++;
            LargestRequestedRead = Math.Max(LargestRequestedRead, count);
            int maximum = Chunks[ChunkIndex % Chunks.Length];
            ChunkIndex++;
            return base.Read(buffer, offset, Math.Min(count, maximum));
        }

        protected override void Dispose(bool disposing)
        {
            Operations?.Add("source-dispose");
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private class TrackingDestination : MemoryStream
    {
        private readonly CancellationTokenSource? Cancellation;
        private readonly TimeSpan? FirstWriteDelay;
        private readonly List<string>? Operations;

        internal bool Disposed;
        internal int BytesWritten;
        internal int FlushCalls;
        internal int LargestWrite;
        internal int WriteCalls;

        internal TrackingDestination(
            CancellationTokenSource? cancellation,
            TimeSpan? firstWriteDelay,
            List<string>? operations)
        {
            Cancellation = cancellation;
            FirstWriteDelay = firstWriteDelay;
            Operations = operations;
            Disposed = false;
            BytesWritten = 0;
            FlushCalls = 0;
            LargestWrite = 0;
            WriteCalls = 0;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteCalls++;
            BytesWritten += count;
            LargestWrite = Math.Max(LargestWrite, count);
            base.Write(buffer, offset, count);
            if (WriteCalls == 1)
            {
                if (FirstWriteDelay != null)
                {
                    Thread.Sleep(FirstWriteDelay.Value);
                }
                Cancellation?.Cancel();
            }
        }

        public override void Flush()
        {
            FlushCalls++;
            Operations?.Add("destination-flush");
            base.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            Operations?.Add("destination-dispose");
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
