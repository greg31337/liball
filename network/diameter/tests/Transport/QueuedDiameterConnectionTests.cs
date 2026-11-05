using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace diameter.Tests;

public class QueuedDiameterConnectionTests
{
    [Test]
    public void EgressQueueOwnsBuffersReportsSaturationAndPreservesOrder()
    {
        ControlledDiameterConnection owned = new ControlledDiameterConnection();
        QueuedDiameterConnection queued = new QueuedDiameterConnection(
            owned,
            2,
            1,
            32,
            1024);
        byte[] firstBytes = new byte[] { 1, 2, 3 };
        byte[] secondBytes = new byte[] { 4, 5, 6 };

        Task<DiameterTransportIoResult> first = Task.Run(() =>
            queued.SendDiameterConnectionBytes(firstBytes, 5000));
        Assert.That(owned.SendEntered.Wait(1000), Is.True);
        firstBytes[0] = 99;

        Task<DiameterTransportIoResult> second = Task.Run(() =>
            queued.SendDiameterConnectionBytes(secondBytes, 5000));
        Assert.That(SpinWait.SpinUntil(() => queued.EgressQueue.Count == 1, 1000), Is.True);

        DiameterTransportIoResult saturated = queued.SendDiameterConnectionBytes(
            new byte[] { 7, 8, 9 },
            1000);
        owned.ReleaseSends.Set();
        Assert.That(Task.WaitAll(new Task[] { first, second }, 2000), Is.True);
        DiameterTransportOperationResult close = queued.CloseDiameterConnection();
        DiameterTransportOperationResult repeatedClose = queued.CloseDiameterConnection();

        Assert.Multiple(() =>
        {
            Assert.That(first.Result.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(first.Result.TransferredCount, Is.EqualTo(3));
            Assert.That(second.Result.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(second.Result.TransferredCount, Is.EqualTo(3));
            Assert.That(saturated.Status, Is.EqualTo(DiameterTransportStatus.QueueFull));
            Assert.That(saturated.Code, Is.EqualTo("egress-queue-full"));
            Assert.That(owned.SentBuffers, Has.Count.EqualTo(2));
            Assert.That(owned.SentBuffers[0], Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(owned.SentBuffers[1], Is.EqualTo(secondBytes));
            Assert.That(close.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(repeatedClose.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(owned.CloseCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void EgressWorkerCompletesPartialUnderlyingWritesWithinOneDeadline()
    {
        PartialWriteDiameterConnection owned = new PartialWriteDiameterConnection();
        owned.MaximumWriteSize = 2;
        QueuedDiameterConnection queued = new QueuedDiameterConnection(
            owned,
            1,
            1,
            32,
            1024);

        DiameterTransportIoResult sent = queued.SendDiameterConnectionBytes(
            new byte[] { 1, 2, 3, 4, 5 },
            1000);
        DiameterTransportOperationResult close = queued.CloseDiameterConnection();

        Assert.Multiple(() =>
        {
            Assert.That(sent.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(sent.TransferredCount, Is.EqualTo(5));
            Assert.That(owned.SendCount, Is.EqualTo(3));
            Assert.That(owned.SentBytes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(close.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(owned.CloseCount, Is.EqualTo(1));
            Assert.That(queued.EgressWorker.IsAlive, Is.False);
        });
    }

    [Test]
    public void IngressQueuePreservesPartialChunksAndDrainsBeforeEndOfStream()
    {
        ControlledDiameterConnection owned = new ControlledDiameterConnection();
        owned.ReleaseSends.Set();
        byte[] firstFrame = CreateFrame(1U, 20);
        byte[] secondFrame = CreateFrame(2U, 20);
        owned.EnqueueReceiveData(firstFrame.AsSpan(0, 7).ToArray());
        owned.EnqueueReceiveData(firstFrame.AsSpan(7).ToArray());
        owned.EnqueueReceiveData(secondFrame);
        owned.EnqueueReceiveTerminal(DiameterTransportStatus.EndOfStream);
        QueuedDiameterConnection queued = new QueuedDiameterConnection(
            owned,
            1,
            1,
            32,
            1024);
        byte[] first = new byte[8];
        byte[] second = new byte[16];
        byte[] third = new byte[20];

        DiameterTransportIoResult firstResult =
            queued.ReceiveDiameterConnectionBytes(first, 1000);
        DiameterTransportIoResult secondResult =
            queued.ReceiveDiameterConnectionBytes(second, 1000);
        DiameterTransportIoResult thirdResult =
            queued.ReceiveDiameterConnectionBytes(third, 1000);
        DiameterTransportIoResult terminal =
            queued.ReceiveDiameterConnectionBytes(new byte[4], 1000);
        queued.CloseDiameterConnection();

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(firstResult.TransferredCount, Is.EqualTo(8));
            Assert.That(first, Is.EqualTo(firstFrame.AsSpan(0, 8).ToArray()));
            Assert.That(secondResult.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(secondResult.TransferredCount, Is.EqualTo(12));
            Assert.That(second.AsSpan(0, 12).ToArray(),
                Is.EqualTo(firstFrame.AsSpan(8).ToArray()));
            Assert.That(thirdResult.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(thirdResult.TransferredCount, Is.EqualTo(20));
            Assert.That(third, Is.EqualTo(secondFrame));
            Assert.That(terminal.Status, Is.EqualTo(DiameterTransportStatus.EndOfStream));
            Assert.That(terminal.EndOfStream, Is.True);
            Assert.That(terminal.Code, Is.EqualTo("clean-end-of-stream"));
            Assert.That(queued.IngressQueue.Count, Is.Zero);
        });
    }

    [Test]
    public void IngressQueueBackpressuresAndResumesWithoutDroppingFrames()
    {
        ControlledDiameterConnection owned = new ControlledDiameterConnection();
        owned.ReleaseSends.Set();
        byte[] firstFrame = CreateFrame(41U, 20);
        byte[] secondFrame = CreateFrame(42U, 20);
        byte[] coalesced = new byte[40];
        Array.Copy(firstFrame, 0, coalesced, 0, 20);
        Array.Copy(secondFrame, 0, coalesced, 20, 20);
        owned.EnqueueReceiveData(coalesced);
        owned.EnqueueReceiveTerminal(DiameterTransportStatus.EndOfStream);
        QueuedDiameterConnection queued = new QueuedDiameterConnection(
            owned,
            1,
            1,
            40,
            1024);
        Assert.That(SpinWait.SpinUntil(
            () => queued.IngressQueue.Count == 1,
            1000), Is.True);

        byte[] first = new byte[20];
        DiameterTransportIoResult firstResult =
            queued.ReceiveDiameterConnectionBytes(first, 1000);
        byte[] second = new byte[20];
        DiameterTransportIoResult secondResult =
            queued.ReceiveDiameterConnectionBytes(second, 1000);
        DiameterTransportIoResult terminal =
            queued.ReceiveDiameterConnectionBytes(new byte[20], 1000);
        queued.CloseDiameterConnection();

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(first, Is.EqualTo(firstFrame));
            Assert.That(secondResult.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(second, Is.EqualTo(secondFrame));
            Assert.That(terminal.Status, Is.EqualTo(DiameterTransportStatus.EndOfStream));
            Assert.That(terminal.EndOfStream, Is.True);
            Assert.That(queued.IngressQueue.Count, Is.Zero);
            Assert.That(queued.IngressWorker.IsAlive, Is.False);
        });
    }

    [Test]
    public void ReceiveTimeoutAndCloseUnblockWaitersAndTransferOwnershipOnce()
    {
        ControlledDiameterConnection owned = new ControlledDiameterConnection();
        owned.ReleaseSends.Set();
        QueuedDiameterConnection queued = new QueuedDiameterConnection(
            owned,
            1,
            1,
            16,
            1024);
        DiameterConnectionMetadata firstMetadata = queued.ReadDiameterConnectionMetadata();
        firstMetadata.ConnectionId = "mutated";
        firstMetadata.Security.PeerCertificateDnsNames.Add("mutated.example");

        DiameterTransportIoResult timeout = queued.ReceiveDiameterConnectionBytes(
            new byte[4],
            50);
        Task<DiameterTransportIoResult> waitingReceive = Task.Run(() =>
            queued.ReceiveDiameterConnectionBytes(new byte[4], 5000));
        Assert.That(owned.ReceiveEntered.Wait(1000), Is.True);
        Task<DiameterTransportOperationResult> firstClose = Task.Run(() =>
            queued.CloseDiameterConnection());
        Task<DiameterTransportOperationResult> secondClose = Task.Run(() =>
            queued.CloseDiameterConnection());
        Assert.That(Task.WaitAll(
            new Task[] { waitingReceive, firstClose, secondClose },
            2000), Is.True);

        DiameterTransportIoResult closedSend = queued.SendDiameterConnectionBytes(
            new byte[] { 1 },
            1000);
        DiameterTransportIoResult closedReceive = queued.ReceiveDiameterConnectionBytes(
            new byte[1],
            1000);
        DiameterConnectionMetadata finalMetadata = queued.ReadDiameterConnectionMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(timeout.Status, Is.EqualTo(DiameterTransportStatus.TimedOut));
            Assert.That(timeout.Code, Is.EqualTo("receive-timeout"));
            Assert.That(waitingReceive.Result.Status,
                Is.EqualTo(DiameterTransportStatus.Closed));
            Assert.That(firstClose.Result.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(secondClose.Result.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(owned.CloseCount, Is.EqualTo(1));
            Assert.That(closedSend.Status, Is.EqualTo(DiameterTransportStatus.Closed));
            Assert.That(closedReceive.Status, Is.EqualTo(DiameterTransportStatus.Closed));
            Assert.That(finalMetadata.ConnectionId, Is.EqualTo("owned"));
            Assert.That(finalMetadata.Security.PeerCertificateDnsNames,
                Is.EqualTo(new[] { "peer.example" }));
            Assert.That(finalMetadata.Open, Is.False);
            Assert.That(queued.IngressWorker.IsAlive, Is.False);
            Assert.That(queued.EgressWorker.IsAlive, Is.False);
        });
    }

    [Test]
    public void CloseUnblocksActiveAndQueuedEgressOperations()
    {
        ControlledDiameterConnection owned = new ControlledDiameterConnection();
        QueuedDiameterConnection queued = new QueuedDiameterConnection(
            owned,
            1,
            1,
            8,
            1024);
        Task<DiameterTransportIoResult> active = Task.Run(() =>
            queued.SendDiameterConnectionBytes(new byte[] { 1 }, 5000));
        Assert.That(owned.SendEntered.Wait(1000), Is.True);
        Task<DiameterTransportIoResult> waiting = Task.Run(() =>
            queued.SendDiameterConnectionBytes(new byte[] { 2 }, 5000));
        Assert.That(SpinWait.SpinUntil(() => queued.EgressQueue.Count == 1, 1000), Is.True);

        DiameterTransportOperationResult close = queued.CloseDiameterConnection();
        Assert.That(Task.WaitAll(new Task[] { active, waiting }, 2000), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(close.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(active.Result.Status, Is.EqualTo(DiameterTransportStatus.Closed));
            Assert.That(waiting.Result.Status, Is.EqualTo(DiameterTransportStatus.Closed));
            Assert.That(owned.SentBuffers, Is.Empty);
            Assert.That(owned.CloseCount, Is.EqualTo(1));
            Assert.That(queued.IngressWorker.IsAlive, Is.False);
            Assert.That(queued.EgressWorker.IsAlive, Is.False);
        });
    }

    [Test]
    public void QueueBoundariesAndTimeoutArgumentsAreRejectedWithoutStartingIo()
    {
        ControlledDiameterConnection owned = new ControlledDiameterConnection();
        owned.ReleaseSends.Set();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new QueuedDiameterConnection(null!, 1, 1, 1, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new QueuedDiameterConnection(owned, 0, 1, 1, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new QueuedDiameterConnection(owned, 1, 0, 1, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new QueuedDiameterConnection(owned, 1, 1, 0, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new QueuedDiameterConnection(owned, 1, 1, 1, 19));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new QueuedDiameterConnection(owned, 1, 1, 1, 21));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new QueuedDiameterConnection(
                    owned,
                    1,
                    1,
                    1,
                    DiameterProtocolConstants.MaximumMessageLength + 4));
        });

        QueuedDiameterConnection queued = new QueuedDiameterConnection(
            owned,
            1,
            1,
            8,
            1024);
        DiameterTransportIoResult send = queued.SendDiameterConnectionBytes(
            new byte[] { 1 },
            0);
        DiameterTransportIoResult receive = queued.ReceiveDiameterConnectionBytes(
            new byte[1],
            -1);
        queued.CloseDiameterConnection();

        Assert.Multiple(() =>
        {
            Assert.That(send.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(send.Code, Is.EqualTo("invalid-timeout"));
            Assert.That(receive.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(receive.Code, Is.EqualTo("invalid-timeout"));
            Assert.That(owned.SentBuffers, Is.Empty);
        });
    }

    [Test]
    public void CoalescedFramesAreQueuedAsIndependentCompleteItems()
    {
        ControlledDiameterConnection owned = new ControlledDiameterConnection();
        owned.ReleaseSends.Set();
        byte[] firstFrame = CreateFrame(11U, 20);
        byte[] secondFrame = CreateFrame(12U, 24);
        byte[] coalesced = new byte[firstFrame.Length + secondFrame.Length];
        Array.Copy(firstFrame, 0, coalesced, 0, firstFrame.Length);
        Array.Copy(secondFrame, 0, coalesced, firstFrame.Length, secondFrame.Length);
        owned.EnqueueReceiveData(coalesced);
        owned.EnqueueReceiveTerminal(DiameterTransportStatus.EndOfStream);
        QueuedDiameterConnection queued = new QueuedDiameterConnection(
            owned,
            2,
            1,
            64,
            1024);
        byte[] firstDestination = new byte[64];
        byte[] secondDestination = new byte[64];

        DiameterTransportIoResult first = queued.ReceiveDiameterConnectionBytes(
            firstDestination,
            1000);
        DiameterTransportIoResult second = queued.ReceiveDiameterConnectionBytes(
            secondDestination,
            1000);
        DiameterTransportIoResult end = queued.ReceiveDiameterConnectionBytes(
            new byte[64],
            1000);
        queued.CloseDiameterConnection();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(first.TransferredCount, Is.EqualTo(firstFrame.Length));
            Assert.That(firstDestination.AsSpan(0, first.TransferredCount).ToArray(),
                Is.EqualTo(firstFrame));
            Assert.That(second.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(second.TransferredCount, Is.EqualTo(secondFrame.Length));
            Assert.That(secondDestination.AsSpan(0, second.TransferredCount).ToArray(),
                Is.EqualTo(secondFrame));
            Assert.That(end.Status, Is.EqualTo(DiameterTransportStatus.EndOfStream));
            Assert.That(end.Code, Is.EqualTo("clean-end-of-stream"));
        });
    }

    [Test]
    public void TruncatedCloseDrainsCompleteFramesThenReportsThePartialTail()
    {
        ControlledDiameterConnection owned = new ControlledDiameterConnection();
        owned.ReleaseSends.Set();
        byte[] completeFrame = CreateFrame(21U, 20);
        byte[] truncatedFrame = CreateFrame(22U, 24);
        byte[] receivedBytes = new byte[completeFrame.Length + 9];
        Array.Copy(completeFrame, 0, receivedBytes, 0, completeFrame.Length);
        Array.Copy(truncatedFrame, 0, receivedBytes, completeFrame.Length, 9);
        owned.EnqueueReceiveData(receivedBytes);
        owned.EnqueueReceiveTerminal(DiameterTransportStatus.EndOfStream);
        QueuedDiameterConnection queued = new QueuedDiameterConnection(
            owned,
            1,
            1,
            64,
            1024);
        byte[] destination = new byte[64];

        DiameterTransportIoResult complete = queued.ReceiveDiameterConnectionBytes(
            destination,
            1000);
        DiameterTransportIoResult truncated = queued.ReceiveDiameterConnectionBytes(
            destination,
            1000);
        queued.CloseDiameterConnection();

        Assert.Multiple(() =>
        {
            Assert.That(complete.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(complete.TransferredCount, Is.EqualTo(completeFrame.Length));
            Assert.That(destination.AsSpan(0, complete.TransferredCount).ToArray(),
                Is.EqualTo(completeFrame));
            Assert.That(truncated.Status, Is.EqualTo(DiameterTransportStatus.Failed));
            Assert.That(truncated.EndOfStream, Is.True);
            Assert.That(truncated.Code, Is.EqualTo("truncated-frame-header"));
        });
    }

    [Test]
    public void DeclaredFrameAboveConnectionLimitFailsBeforeBodyAllocation()
    {
        ControlledDiameterConnection owned = new ControlledDiameterConnection();
        owned.ReleaseSends.Set();
        byte[] oversized = CreateFrame(31U, 24);
        owned.EnqueueReceiveData(oversized.AsSpan(0, 20).ToArray());
        QueuedDiameterConnection queued = new QueuedDiameterConnection(
            owned,
            1,
            1,
            32,
            20);

        DiameterTransportIoResult result = queued.ReceiveDiameterConnectionBytes(
            new byte[32],
            1000);
        queued.CloseDiameterConnection();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterTransportStatus.Failed));
            Assert.That(result.EndOfStream, Is.False);
            Assert.That(result.TransferredCount, Is.Zero);
            Assert.That(result.Code, Is.EqualTo("configured-frame-size-limit"));
        });
    }

    private static byte[] CreateFrame(uint identifier, int length)
    {
        byte[] frame = new byte[length];
        frame[0] = DiameterProtocolConstants.Version;
        frame[1] = (byte)(length >> 16);
        frame[2] = (byte)(length >> 8);
        frame[3] = (byte)length;
        frame[12] = (byte)(identifier >> 24);
        frame[13] = (byte)(identifier >> 16);
        frame[14] = (byte)(identifier >> 8);
        frame[15] = (byte)identifier;
        return frame;
    }
}

public class ControlledDiameterConnection : IDiameterConnection
{
    public object Sync = new object();
    public Queue<ControlledReceiveItem> ReceiveItems = new Queue<ControlledReceiveItem>();
    public List<byte[]> SentBuffers = new List<byte[]>();
    public ManualResetEventSlim SendEntered = new ManualResetEventSlim(false);
    public ManualResetEventSlim ReceiveEntered = new ManualResetEventSlim(false);
    public ManualResetEventSlim ReleaseSends = new ManualResetEventSlim(false);
    public int CloseCount;
    public bool Closed;

    public DiameterTransportIoResult SendDiameterConnectionBytes(
        ReadOnlySpan<byte> source,
        int timeoutMilliseconds)
    {
        SendEntered.Set();
        if (!ReleaseSends.Wait(timeoutMilliseconds))
        {
            DiameterTransportIoResult timeout = new DiameterTransportIoResult();
            timeout.Status = DiameterTransportStatus.TimedOut;
            return timeout;
        }

        lock (Sync)
        {
            if (Closed)
            {
                DiameterTransportIoResult closed = new DiameterTransportIoResult();
                closed.Status = DiameterTransportStatus.Closed;
                return closed;
            }

            SentBuffers.Add(source.ToArray());
        }

        DiameterTransportIoResult result = new DiameterTransportIoResult();
        result.TransferredCount = source.Length;
        return result;
    }

    public DiameterTransportIoResult ReceiveDiameterConnectionBytes(
        Span<byte> destination,
        int timeoutMilliseconds)
    {
        ReceiveEntered.Set();
        long deadline = Environment.TickCount64 + timeoutMilliseconds;
        lock (Sync)
        {
            while (ReceiveItems.Count == 0 && !Closed)
            {
                long remaining = deadline - Environment.TickCount64;
                if (remaining <= 0L)
                {
                    DiameterTransportIoResult timeout = new DiameterTransportIoResult();
                    timeout.Status = DiameterTransportStatus.TimedOut;
                    return timeout;
                }

                Monitor.Wait(Sync, (int)Math.Min(remaining, int.MaxValue));
            }

            if (Closed)
            {
                DiameterTransportIoResult closed = new DiameterTransportIoResult();
                closed.Status = DiameterTransportStatus.Closed;
                return closed;
            }

            ControlledReceiveItem item = ReceiveItems.Dequeue();
            if (item.Status != DiameterTransportStatus.Success)
            {
                DiameterTransportIoResult terminal = new DiameterTransportIoResult();
                terminal.Status = item.Status;
                terminal.EndOfStream = item.Status == DiameterTransportStatus.EndOfStream;
                return terminal;
            }

            int count = Math.Min(destination.Length, item.Data.Length);
            item.Data.AsSpan(0, count).CopyTo(destination);
            DiameterTransportIoResult result = new DiameterTransportIoResult();
            result.TransferredCount = count;
            return result;
        }
    }

    public DiameterConnectionMetadata ReadDiameterConnectionMetadata()
    {
        DiameterConnectionMetadata metadata = new DiameterConnectionMetadata();
        metadata.ConnectionId = "owned";
        metadata.Open = !Closed;
        metadata.Security.PeerCertificateDnsNames.Add("peer.example");
        return metadata;
    }

    public DiameterTransportOperationResult CloseDiameterConnection()
    {
        lock (Sync)
        {
            if (!Closed)
            {
                Closed = true;
                CloseCount = CloseCount + 1;
                ReleaseSends.Set();
                Monitor.PulseAll(Sync);
            }
        }

        return new DiameterTransportOperationResult();
    }

    public void EnqueueReceiveData(byte[] data)
    {
        ControlledReceiveItem item = new ControlledReceiveItem();
        item.Data = data;
        lock (Sync)
        {
            ReceiveItems.Enqueue(item);
            Monitor.PulseAll(Sync);
        }
    }

    public void EnqueueReceiveTerminal(DiameterTransportStatus status)
    {
        ControlledReceiveItem item = new ControlledReceiveItem();
        item.Status = status;
        lock (Sync)
        {
            ReceiveItems.Enqueue(item);
            Monitor.PulseAll(Sync);
        }
    }
}

public class ControlledReceiveItem
{
    public DiameterTransportStatus Status;
    public byte[] Data = new byte[0];
}

public class PartialWriteDiameterConnection : IDiameterConnection
{
    public List<byte> SentBytes = new List<byte>();
    public int MaximumWriteSize = 1;
    public int SendCount;
    public int CloseCount;
    public bool Closed;

    public DiameterTransportIoResult SendDiameterConnectionBytes(
        ReadOnlySpan<byte> source,
        int timeoutMilliseconds)
    {
        DiameterTransportIoResult result = new DiameterTransportIoResult();
        if (Closed)
        {
            result.Status = DiameterTransportStatus.Closed;
            return result;
        }

        int count = Math.Min(MaximumWriteSize, source.Length);
        int index = 0;
        while (index < count)
        {
            SentBytes.Add(source[index]);
            index = index + 1;
        }

        SendCount = SendCount + 1;
        result.TransferredCount = count;
        return result;
    }

    public DiameterTransportIoResult ReceiveDiameterConnectionBytes(
        Span<byte> destination,
        int timeoutMilliseconds)
    {
        DiameterTransportIoResult result = new DiameterTransportIoResult();
        result.Status = DiameterTransportStatus.TimedOut;
        return result;
    }

    public DiameterConnectionMetadata ReadDiameterConnectionMetadata()
    {
        DiameterConnectionMetadata result = new DiameterConnectionMetadata();
        result.ConnectionId = "partial-writer";
        result.Open = !Closed;
        return result;
    }

    public DiameterTransportOperationResult CloseDiameterConnection()
    {
        if (!Closed)
        {
            Closed = true;
            CloseCount = CloseCount + 1;
        }

        return new DiameterTransportOperationResult();
    }
}
