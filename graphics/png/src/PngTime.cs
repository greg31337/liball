using System.IO;

namespace png;

internal class PngTimeParser
{
    internal const int DataLength = 7;

    internal static PngTime Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngTime Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length);
        ValidateFields(data, offset);

        PngTime time = new PngTime();
        time.Year = PngBinaryReader.ReadUInt16(data, offset);
        time.Month = data[offset + 2];
        time.Day = data[offset + 3];
        time.Hour = data[offset + 4];
        time.Minute = data[offset + 5];
        time.Second = data[offset + 6];
        return time;
    }

    internal static PngTime Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkTime)
        {
            throw new InvalidDataException("Chunk type must be tIME.");
        }
    }

    internal static void ValidateRange(byte[] data, int offset, int length)
    {
        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length < 0 || length > data.Length - offset)
        {
            throw new ArgumentOutOfRangeException("length");
        }
    }

    internal static void ValidateLength(int length)
    {
        if (length != DataLength)
        {
            throw new InvalidDataException("tIME data must contain exactly 7 bytes.");
        }
    }

    internal static void ValidateFields(byte[] data, int offset)
    {
        ValidateRangeValue(data[offset + 2], 1, 12, "tIME month must be 1 through 12.");
        ValidateRangeValue(data[offset + 3], 1, 31, "tIME day must be 1 through 31.");
        ValidateRangeValue(data[offset + 4], 0, 23, "tIME hour must be 0 through 23.");
        ValidateRangeValue(data[offset + 5], 0, 59, "tIME minute must be 0 through 59.");
        ValidateRangeValue(data[offset + 6], 0, 60, "tIME second must be 0 through 60.");
    }

    internal static void ValidateRangeValue(byte value, int minimum, int maximum, string message)
    {
        if (value < minimum || value > maximum)
        {
            throw new InvalidDataException(message);
        }
    }
}

internal class PngTimeWriter
{
    internal static byte[] WriteData(PngTime time)
    {
        byte[] data = new byte[PngTimeParser.DataLength];
        WriteData(data, 0, time);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngTime time)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(time);

        ValidateWriteRange(data, offset, PngTimeParser.DataLength);
        ValidateFields(time);
        PngBinaryWriter.WriteUInt16(data, offset, time.Year);
        data[offset + 2] = time.Month;
        data[offset + 3] = time.Day;
        data[offset + 4] = time.Hour;
        data[offset + 5] = time.Minute;
        data[offset + 6] = time.Second;
    }

    internal static PngChunk WriteChunk(PngTime time)
    {
        return CreateChunk(WriteData(time));
    }

    internal static void ValidateFields(PngTime time)
    {
        ValidateRangeValue(time.Month, 1, 12, "Month");
        ValidateRangeValue(time.Day, 1, 31, "Day");
        ValidateRangeValue(time.Hour, 0, 23, "Hour");
        ValidateRangeValue(time.Minute, 0, 59, "Minute");
        ValidateRangeValue(time.Second, 0, 60, "Second");
    }

    internal static void ValidateRangeValue(byte value, int minimum, int maximum, string name)
    {
        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    internal static void ValidateWriteRange(byte[] data, int offset, int length)
    {
        if (offset < 0 || offset > data.Length - length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }
    }

    internal static PngChunk CreateChunk(byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkTime;
        chunk.Data = data;
        chunk.Length = (uint)chunk.Data.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = false;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
    }
}
