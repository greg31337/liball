using System;

namespace websocket;

internal class DeflateMatchHistory
{
    byte[] Bytes;
    int WriteIndex;
    int Count;

    internal DeflateMatchHistory(int windowBits)
    {
        if (windowBits < 8 || windowBits > 15)
        {
            throw new WebSocketStateException();
        }

        Bytes = new byte[1 << windowBits];
    }

    internal void AppendMatchBytes(ReadOnlySpan<byte> source)
    {
        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            Bytes[WriteIndex] = source[sourceIndex];
            WriteIndex = WriteIndex + 1;
            if (WriteIndex == Bytes.Length)
            {
                WriteIndex = 0;
            }

            if (Count < Bytes.Length)
            {
                Count = Count + 1;
            }

            sourceIndex = sourceIndex + 1;
        }
    }

    internal int CopyMatchHistory(Span<byte> destination)
    {
        if (destination.Length < Count)
        {
            return 0;
        }

        int readIndex = WriteIndex - Count;
        if (readIndex < 0)
        {
            readIndex = readIndex + Bytes.Length;
        }

        int destinationIndex = 0;
        while (destinationIndex < Count)
        {
            destination[destinationIndex] = Bytes[readIndex];
            readIndex = readIndex + 1;
            if (readIndex == Bytes.Length)
            {
                readIndex = 0;
            }

            destinationIndex = destinationIndex + 1;
        }

        return Count;
    }

    internal void ResetMatchHistory(bool retainHistory)
    {
        if (!retainHistory)
        {
            Bytes.AsSpan().Clear();
            WriteIndex = 0;
            Count = 0;
        }
    }

    internal int GetMatchHistoryCount()
    {
        return Count;
    }

    internal int GetMatchWindowSize()
    {
        return Bytes.Length;
    }
}
