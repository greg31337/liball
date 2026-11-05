using System.IO;

namespace gif;

internal interface IGifByteWriter
{
    bool WriteByte(int value);
    bool Write(byte[] data);
    bool WriteRange(byte[] data, int offset, int count);
    bool WriteUInt16LittleEndian(int value);
    bool Flush();
    long GetByteOffset();
}

internal class GifBinaryWriter
{
    internal static bool WriteUInt16LittleEndian(byte[] destination, int offset, int value)
    {
        if (destination == null)
        {
            return false;
        }

        if (offset < 0)
        {
            return false;
        }

        if (offset >= destination.Length)
        {
            return false;
        }

        if (offset + 1 >= destination.Length)
        {
            return false;
        }

        if (value < 0)
        {
            return false;
        }

        if (value > 65535)
        {
            return false;
        }

        destination[offset] = (byte)(value & 255);
        destination[offset + 1] = (byte)((value >> 8) & 255);
        return true;
    }

    internal static byte[] CreateUInt16LittleEndian(int value)
    {
        if (value < 0)
        {
            return new byte[0];
        }

        if (value > 65535)
        {
            return new byte[0];
        }

        byte[] data = new byte[2];
        WriteUInt16LittleEndian(data, 0, value);
        return data;
    }
}

internal class GifStreamByteWriter : IGifByteWriter
{
    internal Stream Output;
    internal long ByteOffset;
    internal long MaxBytes;
    internal bool Failed;
    internal bool LimitExceeded;

    internal GifStreamByteWriter(Stream output)
    {
        if (output == null)
        {
            Output = Stream.Null;
            Failed = true;
        }
        else
        {
            Output = output;
            Failed = false;
        }

        ByteOffset = 0;
        MaxBytes = long.MaxValue;
        LimitExceeded = false;
    }

    public bool WriteByte(int value)
    {
        if (value < 0)
        {
            return false;
        }

        if (value > 255)
        {
            return false;
        }

        if (ByteOffset >= MaxBytes)
        {
            Failed = true;
            LimitExceeded = true;
            return false;
        }

        if (!Output.CanWrite)
        {
            Failed = true;
            return false;
        }

        if (Failed)
        {
            return false;
        }

        Output.WriteByte((byte)value);
        ByteOffset = ByteOffset + 1;
        return true;
    }

    public bool Write(byte[] data)
    {
        if (data == null)
        {
            return false;
        }

        return WriteRange(data, 0, data.Length);
    }

    public bool WriteRange(byte[] data, int offset, int count)
    {
        if (data == null)
        {
            return false;
        }

        if (offset < 0)
        {
            return false;
        }

        if (count < 0)
        {
            return false;
        }

        if (offset > data.Length)
        {
            return false;
        }

        if (count > data.Length - offset)
        {
            return false;
        }

        if ((long)count > MaxBytes - ByteOffset)
        {
            Failed = true;
            LimitExceeded = true;
            return false;
        }

        if (!Output.CanWrite)
        {
            Failed = true;
            return false;
        }

        if (Failed)
        {
            return false;
        }

        if (count == 0)
        {
            return true;
        }

        Output.Write(data, offset, count);
        ByteOffset = ByteOffset + count;
        return true;
    }

    public bool WriteUInt16LittleEndian(int value)
    {
        byte[] data = GifBinaryWriter.CreateUInt16LittleEndian(value);
        if (data.Length != 2)
        {
            return false;
        }

        return WriteRange(data, 0, 2);
    }

    public bool Flush()
    {
        if (!Output.CanWrite)
        {
            Failed = true;
            return false;
        }

        if (Failed)
        {
            return false;
        }

        Output.Flush();
        return true;
    }

    public long GetByteOffset()
    {
        return ByteOffset;
    }
}

internal class GifSubBlockWriteResult
{
    internal bool Success;
    internal byte[] EncodedData;
    internal int BlockCount;
    internal int TotalBytes;
    internal bool LimitExceeded;
    internal GifErrorDiagnostic Error;

    internal GifSubBlockWriteResult()
    {
        Success = false;
        EncodedData = new byte[0];
        BlockCount = 0;
        TotalBytes = 0;
        LimitExceeded = false;
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifSubBlockWriter
{
    internal int MaxBytes;

    internal GifSubBlockWriter(int maxBytes)
    {
        MaxBytes = maxBytes;
    }

    internal GifSubBlockWriteResult Write(IGifByteWriter writer, byte[] data)
    {
        GifSubBlockWriteResult result = new GifSubBlockWriteResult();

        if (writer == null)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Missing sub-block writer.", new GifDiagnosticPosition(-1, -1, "DataSubBlocks"));
            return result;
        }

        byte[] payload = data;
        if (payload == null)
        {
            payload = new byte[0];
        }

        if (payload.Length > MaxBytes)
        {
            result.LimitExceeded = true;
            result.Error = new GifErrorDiagnostic(GifErrorCode.ByteLimitExceeded, "Data sub-block byte limit exceeded.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "DataSubBlocks"));
            return result;
        }

        int offset = 0;
        while (offset < payload.Length)
        {
            int chunk = GifConstants.MaximumDataSubBlockSize;
            if (payload.Length - offset < chunk)
            {
                chunk = payload.Length - offset;
            }

            if (!writer.WriteByte(chunk))
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write data sub-block size.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            if (!writer.WriteRange(payload, offset, chunk))
            {
                result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write data sub-block payload.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "DataSubBlocks"));
                return result;
            }

            result.BlockCount = result.BlockCount + 1;
            result.TotalBytes = result.TotalBytes + chunk;
            offset = offset + chunk;
        }

        if (!writer.WriteByte(GifConstants.BlockTerminator))
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DestinationWriteFailed, "Failed to write data sub-block terminator.", new GifDiagnosticPosition(writer.GetByteOffset(), -1, "DataSubBlocks"));
            return result;
        }

        result.Success = true;
        return result;
    }

    internal GifSubBlockWriteResult Create(byte[] data)
    {
        MemoryStream stream = new MemoryStream();
        IGifByteWriter writer = new GifStreamByteWriter(stream);
        GifSubBlockWriteResult result = Write(writer, data);
        if (result.Success)
        {
            result.EncodedData = stream.ToArray();
        }

        return result;
    }
}
