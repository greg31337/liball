using System;

namespace websocket;

internal class DeflateWindowWriteResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal int ConsumedCount;
    internal int ProducedCount;
    internal int RemainingCopyLength;
}

internal class DeflateSlidingWindow
{
    byte[] History;
    int WindowSize;
    int WriteIndex;
    int HistoryCount;
    int PendingDistance;
    int PendingLength;

    internal DeflateSlidingWindow(int windowBits)
    {
        if (windowBits < 8 || windowBits > 15)
        {
            throw new WebSocketStateException();
        }

        WindowSize = 1 << windowBits;
        History = new byte[WindowSize];
    }

    internal DeflateWindowWriteResult WriteBytes(
        ReadOnlySpan<byte> source,
        Span<byte> destination)
    {
        DeflateWindowWriteResult result = new DeflateWindowWriteResult();
        if (PendingLength != 0)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
            result.RemainingCopyLength = PendingLength;
            return result;
        }

        int writeCount = source.Length;
        if (writeCount > destination.Length)
        {
            writeCount = destination.Length;
        }

        source.Slice(0, writeCount).CopyTo(destination);
        int index = 0;
        while (index < writeCount)
        {
            AddHistoryByte(destination[index]);
            index = index + 1;
        }

        result.ConsumedCount = writeCount;
        result.ProducedCount = writeCount;
        if (writeCount < source.Length)
        {
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
        }

        return result;
    }

    internal WebSocketParseStatus BeginCopy(int distance, int length)
    {
        if (PendingLength != 0 ||
            distance <= 0 ||
            distance > WindowSize ||
            distance > HistoryCount ||
            length < 3 ||
            length > 258)
        {
            return WebSocketParseStatus.ProtocolFailure;
        }

        PendingDistance = distance;
        PendingLength = length;
        return WebSocketParseStatus.Complete;
    }

    internal DeflateWindowWriteResult ContinueCopy(Span<byte> destination)
    {
        DeflateWindowWriteResult result = new DeflateWindowWriteResult();
        int writeCount = PendingLength;
        if (writeCount > destination.Length)
        {
            writeCount = destination.Length;
        }

        int index = 0;
        while (index < writeCount)
        {
            int sourceIndex = WriteIndex - PendingDistance;
            if (sourceIndex < 0)
            {
                sourceIndex = sourceIndex + WindowSize;
            }

            byte value = History[sourceIndex];
            destination[index] = value;
            AddHistoryByte(value);
            PendingLength = PendingLength - 1;
            index = index + 1;
        }

        result.ProducedCount = writeCount;
        result.RemainingCopyLength = PendingLength;
        if (PendingLength != 0)
        {
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
        }
        else
        {
            PendingDistance = 0;
        }

        return result;
    }

    internal void ResetSlidingWindow(bool retainHistory)
    {
        PendingDistance = 0;
        PendingLength = 0;
        if (!retainHistory)
        {
            History.AsSpan().Clear();
            WriteIndex = 0;
            HistoryCount = 0;
        }
    }

    internal int GetWindowSize()
    {
        return WindowSize;
    }

    internal int GetHistoryCount()
    {
        return HistoryCount;
    }

    internal int GetPendingCopyLength()
    {
        return PendingLength;
    }

    void AddHistoryByte(byte value)
    {
        History[WriteIndex] = value;
        WriteIndex = WriteIndex + 1;
        if (WriteIndex == WindowSize)
        {
            WriteIndex = 0;
        }

        if (HistoryCount < WindowSize)
        {
            HistoryCount = HistoryCount + 1;
        }
    }
}
