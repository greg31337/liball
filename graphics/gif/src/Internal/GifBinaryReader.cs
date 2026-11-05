using System;
using System.IO;

namespace gif;

internal interface IGifByteReader
{
    int ReadByte();
    int Read(byte[] buffer, int offset, int count);
    bool Advance(int count);
    int RemainingCount();
    long GetByteOffset();
}

internal class GifBinaryReader
{
    internal static int ReadUInt16LittleEndian(byte lowByte, byte highByte)
    {
        int low = lowByte;
        int high = highByte;
        return low | (high << 8);
    }

    internal static int ReadUInt16LittleEndian(byte[] data)
    {
        return ReadUInt16LittleEndian(data, 0);
    }

    internal static int ReadUInt16LittleEndian(byte[] data, int offset)
    {
        if (data == null)
        {
            return -1;
        }

        if (offset < 0)
        {
            return -1;
        }

        if (offset >= data.Length)
        {
            return -1;
        }

        if (offset + 1 >= data.Length)
        {
            return -1;
        }

        return ReadUInt16LittleEndian(data[offset], data[offset + 1]);
    }
}

internal class GifMemoryByteReader : IGifByteReader
{
    internal ReadOnlyMemory<byte> Data;
    internal int ByteOffset;
    internal bool EndOfData;

    internal GifMemoryByteReader(ReadOnlyMemory<byte> data)
    {
        Data = data;
        ByteOffset = 0;
        EndOfData = false;
    }

    public int RemainingCount()
    {
        int remaining = Data.Length - ByteOffset;
        if (remaining < 0)
        {
            return 0;
        }

        return remaining;
    }

    public int ReadByte()
    {
        if (ByteOffset >= Data.Length)
        {
            EndOfData = true;
            return -1;
        }

        ReadOnlySpan<byte> span = Data.Span;
        int value = span[ByteOffset];
        ByteOffset = ByteOffset + 1;
        return value;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        if (buffer == null)
        {
            return -1;
        }

        if (offset < 0)
        {
            return -1;
        }

        if (count < 0)
        {
            return -1;
        }

        if (offset > buffer.Length)
        {
            return -1;
        }

        if (count > buffer.Length - offset)
        {
            return -1;
        }

        if (count == 0)
        {
            return 0;
        }

        if (count > RemainingCount())
        {
            EndOfData = true;
            return -1;
        }

        ReadOnlySpan<byte> span = Data.Span;
        int index = 0;
        while (index < count)
        {
            buffer[offset + index] = span[ByteOffset + index];
            index = index + 1;
        }

        ByteOffset = ByteOffset + count;
        return count;
    }

    public bool Advance(int count)
    {
        if (count < 0)
        {
            return false;
        }

        if (count > RemainingCount())
        {
            EndOfData = true;
            return false;
        }

        ByteOffset = ByteOffset + count;
        return true;
    }

    internal int ReadUInt16LittleEndian()
    {
        int low = ReadByte();
        if (low < 0)
        {
            return -1;
        }

        int high = ReadByte();
        if (high < 0)
        {
            return -1;
        }

        return GifBinaryReader.ReadUInt16LittleEndian((byte)low, (byte)high);
    }

    public long GetByteOffset()
    {
        return ByteOffset;
    }
}

internal class GifStreamReadTracker
{
    internal long ByteOffset;
    internal bool EndOfStream;

    internal GifStreamReadTracker()
    {
        ByteOffset = 0;
        EndOfStream = false;
    }

    internal int ReadByte(Stream stream)
    {
        if (stream == null)
        {
            return -1;
        }

        int value = stream.ReadByte();
        if (value < 0)
        {
            EndOfStream = true;
            return -1;
        }

        ByteOffset = ByteOffset + 1;
        return value;
    }

    internal int Read(Stream stream, byte[] buffer, int offset, int count)
    {
        if (stream == null)
        {
            return -1;
        }

        if (buffer == null)
        {
            return -1;
        }

        if (offset < 0)
        {
            return -1;
        }

        if (count < 0)
        {
            return -1;
        }

        if (offset > buffer.Length)
        {
            return -1;
        }

        if (count > buffer.Length - offset)
        {
            return -1;
        }

        if (count == 0)
        {
            return 0;
        }

        int bytesRead = stream.Read(buffer, offset, count);
        if (bytesRead == 0)
        {
            EndOfStream = true;
            return 0;
        }

        ByteOffset = ByteOffset + bytesRead;
        return bytesRead;
    }

    internal int ReadUInt16LittleEndian(Stream stream)
    {
        int low = ReadByte(stream);
        if (low < 0)
        {
            return -1;
        }

        int high = ReadByte(stream);
        if (high < 0)
        {
            return -1;
        }

        return GifBinaryReader.ReadUInt16LittleEndian((byte)low, (byte)high);
    }
}

internal class GifStreamByteReader : IGifByteReader
{
    internal Stream Input;
    internal GifStreamReadTracker Tracker;
    internal long ByteOffset;
    internal bool EndOfStream;

    internal GifStreamByteReader(Stream input)
    {
        if (input == null)
        {
            Input = Stream.Null;
        }
        else
        {
            Input = input;
        }

        Tracker = new GifStreamReadTracker();
        ByteOffset = 0;
        EndOfStream = false;
    }

    public int RemainingCount()
    {
        if (!Input.CanSeek)
        {
            return 0;
        }

        long remaining = Input.Length - Input.Position;
        if (remaining <= 0)
        {
            return 0;
        }

        if (remaining > 2147483647)
        {
            return 2147483647;
        }

        return (int)remaining;
    }

    public int ReadByte()
    {
        int value = Tracker.ReadByte(Input);
        ByteOffset = Tracker.ByteOffset;
        EndOfStream = Tracker.EndOfStream;
        return value;
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        if (buffer == null)
        {
            return -1;
        }

        if (offset < 0)
        {
            return -1;
        }

        if (count < 0)
        {
            return -1;
        }

        if (offset > buffer.Length)
        {
            return -1;
        }

        if (count > buffer.Length - offset)
        {
            return -1;
        }

        if (count == 0)
        {
            return 0;
        }

        int totalRead = 0;
        while (totalRead < count)
        {
            int bytesRead = Tracker.Read(Input, buffer, offset + totalRead, count - totalRead);
            ByteOffset = Tracker.ByteOffset;
            EndOfStream = Tracker.EndOfStream;

            if (bytesRead <= 0)
            {
                return -1;
            }

            totalRead = totalRead + bytesRead;
        }

        return totalRead;
    }

    public bool Advance(int count)
    {
        if (count < 0)
        {
            return false;
        }

        byte[] buffer = new byte[256];
        int remaining = count;
        while (remaining > 0)
        {
            int chunk = 256;
            if (remaining < chunk)
            {
                chunk = remaining;
            }

            int bytesRead = Read(buffer, 0, chunk);
            if (bytesRead < 0)
            {
                return false;
            }

            remaining = remaining - bytesRead;
        }

        return true;
    }

    internal int ReadUInt16LittleEndian()
    {
        int value = Tracker.ReadUInt16LittleEndian(Input);
        ByteOffset = Tracker.ByteOffset;
        EndOfStream = Tracker.EndOfStream;
        return value;
    }

    public long GetByteOffset()
    {
        return ByteOffset;
    }
}

internal class GifSubBlockReadResult
{
    internal bool Success;
    internal byte[] Data;
    internal int BlockCount;
    internal int TotalBytes;
    internal bool Unterminated;
    internal bool LimitExceeded;
    internal GifErrorDiagnostic Error;

    internal GifSubBlockReadResult()
    {
        Success = false;
        Data = new byte[0];
        BlockCount = 0;
        TotalBytes = 0;
        Unterminated = false;
        LimitExceeded = false;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifSubBlockReader
{
    internal int MaxSubBlocks;
    internal int MaxBytes;
    internal int MaxAdditionalBytes;
    internal int MaxMemoryBytes;
    internal long MaxInputBytes;
    internal GifErrorCode AdditionalByteLimitErrorCode;

    internal GifSubBlockReader(int maxSubBlocks, int maxBytes)
    {
        MaxSubBlocks = maxSubBlocks;
        MaxBytes = maxBytes;
        MaxAdditionalBytes = 2147483647;
        MaxMemoryBytes = 2147483647;
        MaxInputBytes = long.MaxValue;
        AdditionalByteLimitErrorCode = GifErrorCode.SubBlockByteLimitExceeded;
    }

    internal GifSubBlockReadResult Read(IGifByteReader reader)
    {
        GifSubBlockReadResult result = new GifSubBlockReadResult();
        MemoryStream payload = new MemoryStream();

        if (reader == null)
        {
            result.Unterminated = true;
            result.Error = new GifErrorDiagnostic(GifErrorCode.UnterminatedSubBlocks, "Missing sub-block reader.", new GifDiagnosticPosition(-1, -1, "DataSubBlocks"));
            return result;
        }

        while (true)
        {
            if (reader.GetByteOffset() >= MaxInputBytes)
            {
                result.LimitExceeded = true;
                result.Data = payload.ToArray();
                result.Error = new GifErrorDiagnostic(GifErrorCode.InputByteLimitExceeded, "GIF input byte limit was reached before the data sub-block chain ended.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            int size = reader.ReadByte();
            if (size < 0)
            {
                result.Unterminated = true;
                result.Data = payload.ToArray();
                result.Error = new GifErrorDiagnostic(GifErrorCode.UnterminatedSubBlocks, "Data sub-block chain is missing its terminator.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            if (size == 0)
            {
                result.Success = true;
                result.Data = payload.ToArray();
                return result;
            }

            if (result.BlockCount >= MaxSubBlocks)
            {
                result.LimitExceeded = true;
                result.Data = payload.ToArray();
                result.Error = new GifErrorDiagnostic(GifErrorCode.SubBlockCountLimitExceeded, "Data sub-block count limit exceeded.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            if (size > MaxBytes - result.TotalBytes)
            {
                result.LimitExceeded = true;
                result.Data = payload.ToArray();
                result.Error = new GifErrorDiagnostic(GifErrorCode.SubBlockByteLimitExceeded, "Data sub-block byte limit exceeded.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            if (size > MaxAdditionalBytes - result.TotalBytes)
            {
                result.LimitExceeded = true;
                result.Data = payload.ToArray();
                result.Error = new GifErrorDiagnostic(AdditionalByteLimitErrorCode, "Additional data sub-block byte limit exceeded.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            if (size > MaxMemoryBytes - result.TotalBytes)
            {
                result.LimitExceeded = true;
                result.Data = payload.ToArray();
                result.Error = new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "Data sub-block buffering exceeds the configured memory limit.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            if ((long)size > MaxInputBytes - reader.GetByteOffset())
            {
                result.LimitExceeded = true;
                result.Data = payload.ToArray();
                result.Error = new GifErrorDiagnostic(GifErrorCode.InputByteLimitExceeded, "Data sub-block payload exceeds the remaining GIF input byte limit.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            byte[] block = new byte[size];
            int bytesRead = reader.Read(block, 0, size);
            if (bytesRead < 0)
            {
                result.Unterminated = true;
                result.Data = payload.ToArray();
                result.Error = new GifErrorDiagnostic(GifErrorCode.UnterminatedSubBlocks, "Data sub-block ended before its declared size.", new GifDiagnosticPosition(reader.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            payload.Write(block, 0, size);
            result.BlockCount = result.BlockCount + 1;
            result.TotalBytes = result.TotalBytes + size;
        }
    }

}
