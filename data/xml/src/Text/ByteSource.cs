using System;
using System.IO;

namespace xml;

internal interface IByteSource
{
    int ReadSourceBytes(Span<byte> destination);
    ByteOrderMarkKind InspectByteOrderMark();
    int SourceByteOrderMarkLength();
    long SourceBytePosition();
    bool IsSourceEnd();
    void CloseByteSource();
}

internal class MemoryByteSource : IByteSource
{
    internal ReadOnlyMemory<byte> Memory;
    internal int Offset;
    internal ByteOrderMarkKind ByteOrderMark;
    internal int ByteOrderMarkLength;
    internal bool Closed;

    internal MemoryByteSource(ReadOnlyMemory<byte> memory)
    {
        Memory = memory;
        ByteOrderMark = EncodingDetector.DetectByteOrderMark(memory.Span);
        ByteOrderMarkLength = EncodingDetector.GetByteOrderMarkLength(ByteOrderMark);
        Offset = ByteOrderMarkLength;
        Closed = false;
    }

    public int ReadSourceBytes(Span<byte> destination)
    {
        EnsureMemorySourceOpen();
        int available = Memory.Length - Offset;
        int count = destination.Length < available ? destination.Length : available;
        if (count > 0)
        {
            Memory.Span.Slice(Offset, count).CopyTo(destination);
            Offset = Offset + count;
        }

        return count;
    }

    public ByteOrderMarkKind InspectByteOrderMark()
    {
        EnsureMemorySourceOpen();
        return ByteOrderMark;
    }

    public int SourceByteOrderMarkLength()
    {
        EnsureMemorySourceOpen();
        return ByteOrderMarkLength;
    }

    public long SourceBytePosition()
    {
        EnsureMemorySourceOpen();
        return Offset - ByteOrderMarkLength;
    }

    public bool IsSourceEnd()
    {
        EnsureMemorySourceOpen();
        return Offset >= Memory.Length;
    }

    public void CloseByteSource()
    {
        Closed = true;
    }

    internal void EnsureMemorySourceOpen()
    {
        if (Closed)
        {
            throw new ObjectDisposedException("MemoryByteSource");
        }
    }
}

internal class StreamByteSource : IByteSource
{
    internal Stream Input;
    internal bool OwnsInput;
    internal byte[] Buffer;
    internal int BufferOffset;
    internal int BufferCount;
    internal bool EndOfInput;
    internal bool ByteOrderMarkInspected;
    internal ByteOrderMarkKind ByteOrderMark;
    internal int ByteOrderMarkLength;
    internal long Position;
    internal bool Closed;

    internal StreamByteSource(Stream input, bool ownsInput, int bufferSize)
    {
        Guard.NotNull(input, "input");
        Guard.Positive(bufferSize, "bufferSize");
        Input = input;
        OwnsInput = ownsInput;
        Buffer = new byte[bufferSize < 4 ? 4 : bufferSize];
        BufferOffset = 0;
        BufferCount = 0;
        EndOfInput = false;
        ByteOrderMarkInspected = false;
        ByteOrderMark = ByteOrderMarkKind.None;
        ByteOrderMarkLength = 0;
        Position = 0;
        Closed = false;
    }

    public int ReadSourceBytes(Span<byte> destination)
    {
        EnsureStreamSourceOpen();
        EnsureByteOrderMarkInspected();
        int total = 0;
        while (total < destination.Length)
        {
            if (BufferOffset >= BufferCount)
            {
                FillStreamBuffer();
            }

            int available = BufferCount - BufferOffset;
            if (available == 0)
            {
                break;
            }

            int needed = destination.Length - total;
            int count = needed < available ? needed : available;
            new ReadOnlySpan<byte>(Buffer, BufferOffset, count).CopyTo(destination.Slice(total, count));
            BufferOffset = BufferOffset + count;
            total = total + count;
        }

        Position = Position + total;
        return total;
    }

    public ByteOrderMarkKind InspectByteOrderMark()
    {
        EnsureStreamSourceOpen();
        EnsureByteOrderMarkInspected();
        return ByteOrderMark;
    }

    public int SourceByteOrderMarkLength()
    {
        EnsureStreamSourceOpen();
        EnsureByteOrderMarkInspected();
        return ByteOrderMarkLength;
    }

    public long SourceBytePosition()
    {
        EnsureStreamSourceOpen();
        return Position;
    }

    public bool IsSourceEnd()
    {
        EnsureStreamSourceOpen();
        EnsureByteOrderMarkInspected();
        if (BufferOffset < BufferCount)
        {
            return false;
        }

        if (!EndOfInput)
        {
            FillStreamBuffer();
        }

        return BufferOffset >= BufferCount && EndOfInput;
    }

    public void CloseByteSource()
    {
        if (Closed)
        {
            return;
        }

        Closed = true;
        if (OwnsInput)
        {
            Input.Dispose();
        }
    }

    internal void EnsureStreamSourceOpen()
    {
        if (Closed)
        {
            throw new ObjectDisposedException("StreamByteSource");
        }
    }

    internal void EnsureByteOrderMarkInspected()
    {
        if (ByteOrderMarkInspected)
        {
            return;
        }

        while (BufferCount < 4 && !EndOfInput)
        {
            FillStreamBuffer();
        }

        ByteOrderMark = EncodingDetector.DetectByteOrderMark(new ReadOnlySpan<byte>(Buffer, 0, BufferCount));
        ByteOrderMarkLength = EncodingDetector.GetByteOrderMarkLength(ByteOrderMark);
        BufferOffset = ByteOrderMarkLength;
        ByteOrderMarkInspected = true;
    }

    internal void FillStreamBuffer()
    {
        if (EndOfInput)
        {
            return;
        }

        if (BufferOffset > 0)
        {
            int remaining = BufferCount - BufferOffset;
            if (remaining > 0)
            {
                Array.Copy(Buffer, BufferOffset, Buffer, 0, remaining);
            }

            BufferOffset = 0;
            BufferCount = remaining;
        }

        if (BufferCount >= Buffer.Length)
        {
            return;
        }

        int count = Input.Read(Buffer, BufferCount, Buffer.Length - BufferCount);
        if (count == 0)
        {
            EndOfInput = true;
        }
        else
        {
            BufferCount = BufferCount + count;
        }
    }
}

internal static class ByteSourceFactory
{
    internal static IByteSource CreateXmlInputByteSource(XmlInput input, int bufferSize)
    {
        Guard.NotNull(input, "input");
        if (input.Kind == XmlInputKind.ByteMemory)
        {
            return new MemoryByteSource(input.Bytes);
        }

        if (input.Kind == XmlInputKind.Stream)
        {
            Stream? stream = input.ByteStream;
            if (stream == null)
            {
                throw new ArgumentException("A stream input requires ByteStream.", "input");
            }

            return new StreamByteSource(stream, input.OwnsResource, bufferSize);
        }

        throw new ArgumentException("The XML input does not contain bytes.", "input");
    }
}
