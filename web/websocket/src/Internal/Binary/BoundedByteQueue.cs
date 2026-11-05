using System;

namespace websocket;

internal class BoundedByteQueue
{
    byte[] buffer;
    int head;
    int count;

    internal BoundedByteQueue(int capacity)
    {
        if (capacity <= 0)
        {
            throw new WebSocketLimitException();
        }

        this.buffer = new byte[capacity];
        this.head = 0;
        this.count = 0;
    }

    internal int GetByteCount()
    {
        return this.count;
    }

    internal int GetByteCapacity()
    {
        return this.buffer.Length;
    }

    internal int GetByteAvailableCapacity()
    {
        return this.buffer.Length - this.count;
    }

    internal int WriteBytes(ReadOnlySpan<byte> source)
    {
        int writableCount = source.Length;
        int availableCount = this.buffer.Length - this.count;
        if (writableCount > availableCount)
        {
            writableCount = availableCount;
        }

        int writeIndex;
        int tailLength = this.buffer.Length - this.head;
        if (this.count >= tailLength)
        {
            writeIndex = this.count - tailLength;
        }
        else
        {
            writeIndex = this.head + this.count;
        }

        int sourceIndex = 0;
        while (sourceIndex < writableCount)
        {
            this.buffer[writeIndex] = source[sourceIndex];
            writeIndex = writeIndex + 1;
            if (writeIndex == this.buffer.Length)
            {
                writeIndex = 0;
            }

            sourceIndex = sourceIndex + 1;
        }

        this.count = this.count + writableCount;
        return writableCount;
    }

    internal int PeekBytes(Span<byte> destination)
    {
        int readableCount = destination.Length;
        if (readableCount > this.count)
        {
            readableCount = this.count;
        }

        int readIndex = this.head;
        int destinationIndex = 0;
        while (destinationIndex < readableCount)
        {
            destination[destinationIndex] = this.buffer[readIndex];
            readIndex = readIndex + 1;
            if (readIndex == this.buffer.Length)
            {
                readIndex = 0;
            }

            destinationIndex = destinationIndex + 1;
        }

        return readableCount;
    }

    internal int ReadBytes(Span<byte> destination)
    {
        int readableCount = destination.Length;
        if (readableCount > this.count)
        {
            readableCount = this.count;
        }

        int destinationIndex = 0;
        while (destinationIndex < readableCount)
        {
            destination[destinationIndex] = this.buffer[this.head];
            this.buffer[this.head] = 0;
            this.head = this.head + 1;
            if (this.head == this.buffer.Length)
            {
                this.head = 0;
            }

            destinationIndex = destinationIndex + 1;
        }

        this.count = this.count - readableCount;
        if (this.count == 0)
        {
            this.head = 0;
        }

        return readableCount;
    }

    internal void ClearBytes()
    {
        int remainingCount = this.count;
        while (remainingCount > 0)
        {
            this.buffer[this.head] = 0;
            this.head = this.head + 1;
            if (this.head == this.buffer.Length)
            {
                this.head = 0;
            }

            remainingCount = remainingCount - 1;
        }

        this.head = 0;
        this.count = 0;
    }
}
