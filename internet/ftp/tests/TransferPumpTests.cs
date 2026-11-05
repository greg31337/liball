namespace ftp.Tests;

public class TransferPumpTests
{
    [Test]
    public void Direct_pump_uses_bounded_reads_and_reports_defensive_progress_snapshots()
    {
        byte[] content = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
        TrackingReadStream source = new TrackingReadStream(content, 3, null, null);
        TrackingWriteStream destination = new TrackingWriteStream(null, null, null);
        List<FtpTransferProgress> snapshots = new List<FtpTransferProgress>();
        FtpTransferPumpRequest request = CreateRequest(source, destination, 4, true, true);
        request.Direction = FtpTransferDirection.Upload;
        request.Path = "/remote/file.bin";
        request.HasExpectedBytes = true;
        request.ExpectedBytes = 9;
        request.InitialLogicalBytes = 2;
        request.InitialWireBytes = 5;
        request.ProgressCallback = progress => snapshots.Add(progress);

        FtpTransferPumpResult result = TransferPump.Execute(request);

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(content));
            Assert.That(source.LargestRequestedRead, Is.LessThanOrEqualTo(4));
            Assert.That(result.SourceBytes, Is.EqualTo(content.Length));
            Assert.That(result.DestinationBytes, Is.EqualTo(content.Length));
            Assert.That(result.Progress.Direction, Is.EqualTo(FtpTransferDirection.Upload));
            Assert.That(result.Progress.Path, Is.EqualTo("/remote/file.bin"));
            Assert.That(result.Progress.HasExpectedBytes, Is.True);
            Assert.That(result.Progress.ExpectedBytes, Is.EqualTo(9));
            Assert.That(result.Progress.LogicalBytes, Is.EqualTo(9));
            Assert.That(result.Progress.WireBytes, Is.EqualTo(12));
            Assert.That(result.Progress.Completed, Is.True);
            Assert.That(snapshots.First().LogicalBytes, Is.EqualTo(2));
            Assert.That(snapshots.First().Completed, Is.False);
            Assert.That(snapshots.Last().Completed, Is.True);
            Assert.That(snapshots.Distinct().Count(), Is.EqualTo(snapshots.Count));
        });
    }

    [Test]
    public void Transform_can_report_different_logical_and_wire_counts_and_a_final_trailer()
    {
        byte[] content = new byte[] { 10, 20, 30 };
        MemoryStream destination = new MemoryStream();
        FtpTransferPumpRequest request = CreateRequest(
            new MemoryStream(content), destination, 2, true, true);
        request.Transform = new ExpandingTransform();

        FtpTransferPumpResult result = TransferPump.Execute(request);

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(),
                Is.EqualTo(new byte[] { 10, 10, 20, 20, 30, 30, 0xEE }));
            Assert.That(result.SourceBytes, Is.EqualTo(3));
            Assert.That(result.DestinationBytes, Is.EqualTo(7));
            Assert.That(result.Progress.LogicalBytes, Is.EqualTo(3));
            Assert.That(result.Progress.WireBytes, Is.EqualTo(7));
            Assert.That(result.Progress.Completed, Is.True);
        });
    }

    [Test]
    public void Cancellation_is_observed_between_chunks_and_cleanup_still_runs_in_order()
    {
        List<string> operations = new List<string>();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        TrackingReadStream source = new TrackingReadStream(
            new byte[] { 1, 2, 3, 4 }, 2, null, operations);
        TrackingWriteStream destination = new TrackingWriteStream(null, null, operations);
        FtpTransferPumpRequest request = CreateRequest(source, destination, 2, false, false);
        request.Options!.Timeouts.CancellationToken = cancellation.Token;
        request.ProgressCallback = progress =>
        {
            if (progress.LogicalBytes != 0)
            {
                cancellation.Cancel();
            }
        };

        Assert.Throws<OperationCanceledException>(() => TransferPump.Execute(request));

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(source.Disposed, Is.True);
            Assert.That(destination.Disposed, Is.True);
            Assert.That(operations.IndexOf("destination-flush"),
                Is.LessThan(operations.IndexOf("destination-dispose")));
            Assert.That(operations.IndexOf("destination-dispose"),
                Is.LessThan(operations.IndexOf("source-dispose")));
        });
    }

    [Test]
    public void Expired_deadline_prevents_reads_but_still_flushes_and_closes()
    {
        List<string> operations = new List<string>();
        TrackingReadStream source = new TrackingReadStream(
            new byte[] { 1 }, 1, null, operations);
        TrackingWriteStream destination = new TrackingWriteStream(null, null, operations);
        FtpTransferPumpRequest request = CreateRequest(source, destination, 8, false, false);
        request.DeadlineMilliseconds = 0;

        Assert.Throws<TimeoutException>(() => TransferPump.Execute(request));

        Assert.Multiple(() =>
        {
            Assert.That(source.ReadCalls, Is.Zero);
            Assert.That(destination.FlushCalls, Is.EqualTo(1));
            Assert.That(destination.Disposed, Is.True);
            Assert.That(source.Disposed, Is.True);
        });
    }

    [Test]
    public void Successful_transfer_flushes_then_closes_destination_then_source()
    {
        List<string> operations = new List<string>();
        TrackingReadStream source = new TrackingReadStream(
            new byte[] { 1, 2 }, 2, null, operations);
        TrackingWriteStream destination = new TrackingWriteStream(null, null, operations);

        TransferPump.Execute(CreateRequest(source, destination, 8, false, false));

        Assert.Multiple(() =>
        {
            Assert.That(operations.IndexOf("destination-flush"), Is.GreaterThanOrEqualTo(0));
            Assert.That(operations.IndexOf("destination-flush"),
                Is.LessThan(operations.IndexOf("destination-dispose")));
            Assert.That(operations.IndexOf("destination-dispose"),
                Is.LessThan(operations.IndexOf("source-dispose")));
        });
    }

    [Test]
    public void Caller_owned_streams_remain_open_while_destination_is_still_flushed()
    {
        TrackingReadStream source = new TrackingReadStream(
            new byte[] { 1, 2 }, 2, null, null);
        TrackingWriteStream destination = new TrackingWriteStream(null, null, null);

        TransferPump.Execute(CreateRequest(source, destination, 8, true, true));

        Assert.Multiple(() =>
        {
            Assert.That(source.Disposed, Is.False);
            Assert.That(destination.Disposed, Is.False);
            Assert.That(destination.FlushCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void Read_failure_has_priority_but_flush_and_both_closes_are_attempted()
    {
        IOException readFailure = new IOException("read failed");
        IOException flushFailure = new IOException("flush failed");
        TrackingReadStream source = new TrackingReadStream(
            new byte[] { 1 }, 1, readFailure, null);
        TrackingWriteStream destination = new TrackingWriteStream(flushFailure, null, null);

        FtpTransferException? error = Assert.Throws<FtpTransferException>(() =>
            TransferPump.Execute(CreateRequest(source, destination, 8, false, false)));

        Assert.Multiple(() =>
        {
            Assert.That(error!.InnerException, Is.SameAs(readFailure));
            Assert.That(destination.FlushCalls, Is.EqualTo(1));
            Assert.That(destination.Disposed, Is.True);
            Assert.That(source.Disposed, Is.True);
        });
    }

    [Test]
    public void Flush_failure_is_reported_after_source_and_destination_cleanup()
    {
        IOException flushFailure = new IOException("flush failed");
        TrackingReadStream source = new TrackingReadStream(
            new byte[] { 1 }, 1, null, null);
        TrackingWriteStream destination = new TrackingWriteStream(flushFailure, null, null);

        FtpTransferException? error = Assert.Throws<FtpTransferException>(() =>
            TransferPump.Execute(CreateRequest(source, destination, 8, false, false)));

        Assert.Multiple(() =>
        {
            Assert.That(error!.InnerException, Is.SameAs(flushFailure));
            Assert.That(destination.Disposed, Is.True);
            Assert.That(source.Disposed, Is.True);
        });
    }

    [Test]
    public void Progress_callback_failure_still_flushes_and_closes_owned_streams()
    {
        InvalidOperationException callbackFailure = new InvalidOperationException("observer failed");
        TrackingReadStream source = new TrackingReadStream(
            new byte[] { 1 }, 1, null, null);
        TrackingWriteStream destination = new TrackingWriteStream(null, null, null);
        FtpTransferPumpRequest request = CreateRequest(source, destination, 8, false, false);
        request.ProgressCallback = progress => throw callbackFailure;

        FtpTransferException? error = Assert.Throws<FtpTransferException>(() =>
            TransferPump.Execute(request));

        Assert.Multiple(() =>
        {
            Assert.That(error!.InnerException, Is.SameAs(callbackFailure));
            Assert.That(destination.Disposed, Is.True);
            Assert.That(source.Disposed, Is.True);
        });
    }

    [Test]
    public void Invalid_requests_transform_output_and_accounting_are_rejected()
    {
        MemoryStream same = new MemoryStream();
        FtpTransferPumpRequest sameRequest = CreateRequest(same, same, 8, true, true);
        FtpTransferPumpRequest invalidDeadline = CreateRequest(
            new MemoryStream(), new MemoryStream(), 8, true, true);
        invalidDeadline.DeadlineMilliseconds = -2;
        FtpTransferPumpRequest hiddenExpected = CreateRequest(
            new MemoryStream(), new MemoryStream(), 8, true, true);
        hiddenExpected.ExpectedBytes = 1;
        FtpTransferPumpRequest malformed = CreateRequest(
            new MemoryStream(new byte[] { 1 }), new MemoryStream(), 8, true, true);
        malformed.Transform = new MalformedTransform();
        FtpTransferPumpRequest overflow = CreateRequest(
            new MemoryStream(new byte[] { 1 }), new MemoryStream(), 8, true, true);
        overflow.InitialLogicalBytes = long.MaxValue;
        overflow.Transform = new LogicalByteTransform();

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => TransferPump.Execute(null));
            Assert.Throws<FtpConfigurationException>(() => TransferPump.Execute(sameRequest));
            Assert.Throws<FtpConfigurationException>(() => TransferPump.Execute(invalidDeadline));
            Assert.Throws<FtpConfigurationException>(() => TransferPump.Execute(hiddenExpected));
            Assert.Throws<FtpTransferException>(() => TransferPump.Execute(malformed));
            Assert.Throws<FtpTransferException>(() => TransferPump.Execute(overflow));
        });
    }

    [Test]
    public void Transfer_byte_ceiling_stops_data_before_the_excess_chunk_is_written()
    {
        TrackingReadStream source = new TrackingReadStream(
            new byte[] { 1, 2, 3, 4, 5 }, 3, null, null);
        MemoryStream destination = new MemoryStream();
        FtpTransferPumpRequest request = CreateRequest(
            source, destination, 3, true, true);
        request.HasMaximumBytes = true;
        request.MaximumBytes = 4;

        FtpTransferException? error = Assert.Throws<FtpTransferException>(
            () => TransferPump.Execute(request));

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(),
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(FtpResourceLimitPolicy.IsLimitFailure(error), Is.True);
        });
    }

    [Test]
    public void Bandwidth_ceiling_delays_delivery_to_the_configured_average_rate()
    {
        MemoryStream destination = new MemoryStream();
        FtpTransferPumpRequest request = CreateRequest(
            new MemoryStream(new byte[] { 1, 2 }),
            destination,
            2,
            true,
            true);
        request.HasBytesPerSecond = true;
        request.BytesPerSecond = 100;
        System.Diagnostics.Stopwatch elapsed =
            System.Diagnostics.Stopwatch.StartNew();

        TransferPump.Execute(request);
        elapsed.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(destination.Length, Is.EqualTo(2));
            Assert.That(elapsed.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(15));
            Assert.That(TransferPump.CalculateMinimumMilliseconds(2, 100),
                Is.EqualTo(20));
        });
    }

    [Test]
    public void Destination_storage_ceiling_is_enforced_independently_of_wire_bytes()
    {
        MemoryStream destination = new MemoryStream();
        FtpTransferPumpRequest request = CreateRequest(
            new MemoryStream(new byte[] { 1, 2, 3 }),
            destination,
            3,
            true,
            true);
        request.HasDestinationMaximumBytes = true;
        request.DestinationMaximumBytes = 2;

        FtpTransferException? error = Assert.Throws<FtpTransferException>(
            () => TransferPump.Execute(request));

        Assert.Multiple(() =>
        {
            Assert.That(destination.Length, Is.Zero);
            Assert.That(FtpResourceLimitPolicy.IsLimitFailure(error), Is.True);
        });
    }

    private static FtpTransferPumpRequest CreateRequest(
        Stream source,
        Stream destination,
        int bufferBytes,
        bool leaveSourceOpen,
        bool leaveDestinationOpen)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.BufferBytes = bufferBytes;
        FtpTransferPumpRequest request = new FtpTransferPumpRequest();
        request.Source = source;
        request.Destination = destination;
        request.Options = options;
        request.LeaveSourceOpen = leaveSourceOpen;
        request.LeaveDestinationOpen = leaveDestinationOpen;
        return request;
    }

    private class ExpandingTransform : IFtpTransferPumpTransform
    {
        public FtpTransferPumpChunk[] TransformChunk(byte[] data, int count, bool finalChunk)
        {
            if (finalChunk)
            {
                return new FtpTransferPumpChunk[]
                {
                    CreateChunk(new byte[] { 0xEE }, 0, 1)
                };
            }

            byte[] expanded = new byte[count * 2];
            int index = 0;
            while (index < count)
            {
                expanded[index * 2] = data[index];
                expanded[index * 2 + 1] = data[index];
                index++;
            }

            return new FtpTransferPumpChunk[]
            {
                CreateChunk(expanded, count, expanded.Length)
            };
        }
    }

    private class MalformedTransform : IFtpTransferPumpTransform
    {
        public FtpTransferPumpChunk[] TransformChunk(byte[] data, int count, bool finalChunk)
        {
            FtpTransferPumpChunk chunk = new FtpTransferPumpChunk();
            chunk.Data = new byte[0];
            chunk.Count = 1;
            return new FtpTransferPumpChunk[] { chunk };
        }
    }

    private class LogicalByteTransform : IFtpTransferPumpTransform
    {
        public FtpTransferPumpChunk[] TransformChunk(byte[] data, int count, bool finalChunk)
        {
            if (finalChunk)
            {
                return new FtpTransferPumpChunk[0];
            }

            return new FtpTransferPumpChunk[]
            {
                CreateChunk(new byte[] { data[0] }, 1, 1)
            };
        }
    }

    private static FtpTransferPumpChunk CreateChunk(
        byte[] data,
        long logicalBytes,
        long wireBytes)
    {
        FtpTransferPumpChunk chunk = new FtpTransferPumpChunk();
        chunk.Data = data;
        chunk.Count = data.Length;
        chunk.LogicalBytes = logicalBytes;
        chunk.WireBytes = wireBytes;
        return chunk;
    }

    private class TrackingReadStream : MemoryStream
    {
        private readonly int maximumChunk;
        private readonly Exception? readFailure;
        private readonly List<string>? operations;

        public bool Disposed;
        public int LargestRequestedRead;
        public int ReadCalls;

        public TrackingReadStream(
            byte[] content,
            int maximumChunk,
            Exception? readFailure,
            List<string>? operations)
            : base(content)
        {
            this.maximumChunk = maximumChunk;
            this.readFailure = readFailure;
            this.operations = operations;
            Disposed = false;
            LargestRequestedRead = 0;
            ReadCalls = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls++;
            LargestRequestedRead = Math.Max(LargestRequestedRead, count);
            operations?.Add("source-read");
            if (readFailure != null)
            {
                throw readFailure;
            }

            return base.Read(buffer, offset, Math.Min(count, maximumChunk));
        }

        protected override void Dispose(bool disposing)
        {
            operations?.Add("source-dispose");
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private class TrackingWriteStream : MemoryStream
    {
        private readonly Exception? flushFailure;
        private readonly Exception? disposeFailure;
        private readonly List<string>? operations;

        public bool Disposed;
        public int FlushCalls;

        public TrackingWriteStream(
            Exception? flushFailure,
            Exception? disposeFailure,
            List<string>? operations)
        {
            this.flushFailure = flushFailure;
            this.disposeFailure = disposeFailure;
            this.operations = operations;
            Disposed = false;
            FlushCalls = 0;
        }

        public override void Flush()
        {
            FlushCalls++;
            operations?.Add("destination-flush");
            if (flushFailure != null)
            {
                throw flushFailure;
            }

            base.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            operations?.Add("destination-dispose");
            Disposed = true;
            base.Dispose(disposing);
            if (disposeFailure != null)
            {
                throw disposeFailure;
            }
        }
    }
}
