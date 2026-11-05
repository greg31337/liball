using System.IO;

namespace png;

internal class PngFrameControlParser
{
    internal const int DataLength = 26;

    internal static PngFrameControl Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Parse(data, 0, data.Length);
    }

    internal static PngFrameControl Parse(byte[] data, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(data);

        ValidateRange(data, offset, length);
        ValidateLength(length);
        ValidateDimensions(data, offset);
        ValidateDisposeOperation(data[offset + 24]);
        ValidateBlendOperation(data[offset + 25]);

        PngFrameControl control = new PngFrameControl();
        control.SequenceNumber = PngBinaryReader.ReadUInt32(data, offset);
        control.Width = PngBinaryReader.ReadUInt32(data, offset + 4);
        control.Height = PngBinaryReader.ReadUInt32(data, offset + 8);
        control.XOffset = PngBinaryReader.ReadUInt32(data, offset + 12);
        control.YOffset = PngBinaryReader.ReadUInt32(data, offset + 16);
        control.DelayNumerator = PngBinaryReader.ReadUInt16(data, offset + 20);
        control.DelayDenominator = PngBinaryReader.ReadUInt16(data, offset + 22);
        control.DisposeOperation = (PngApngDisposeOperation)data[offset + 24];
        control.BlendOperation = (PngApngBlendOperation)data[offset + 25];
        return control;
    }

    internal static PngFrameControl Parse(PngChunk chunk)
    {
        ValidateChunk(chunk);
        return Parse(chunk.Data);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (chunk.Type != PngConstants.ChunkFrameControl)
        {
            throw new InvalidDataException("Chunk type must be fcTL.");
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
            throw new InvalidDataException("fcTL data must contain exactly 26 bytes.");
        }
    }

    internal static void ValidateDimensions(byte[] data, int offset)
    {
        uint width = PngBinaryReader.ReadUInt32(data, offset + 4);
        uint height = PngBinaryReader.ReadUInt32(data, offset + 8);
        if (width == 0 || height == 0)
        {
            throw new InvalidDataException("fcTL width and height must be greater than 0.");
        }
    }

    internal static void ValidateDisposeOperation(byte disposeOperation)
    {
        if (disposeOperation > PngConstants.ApngDisposeOperationPrevious)
        {
            throw new InvalidDataException("fcTL dispose operation is invalid.");
        }
    }

    internal static void ValidateBlendOperation(byte blendOperation)
    {
        if (blendOperation > PngConstants.ApngBlendOperationOver)
        {
            throw new InvalidDataException("fcTL blend operation is invalid.");
        }
    }
}

internal class PngFrameControlWriter
{
    internal static byte[] WriteData(PngFrameControl control)
    {
        byte[] data = new byte[PngFrameControlParser.DataLength];
        WriteData(data, 0, control);
        return data;
    }

    internal static void WriteData(byte[] data, int offset, PngFrameControl control)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(control);

        ValidateWriteRange(data, offset, PngFrameControlParser.DataLength);
        ValidateDimensions(control);
        ValidateDisposeOperation(control.DisposeOperation);
        ValidateBlendOperation(control.BlendOperation);

        PngBinaryWriter.WriteUInt32(data, offset, control.SequenceNumber);
        PngBinaryWriter.WriteUInt32(data, offset + 4, control.Width);
        PngBinaryWriter.WriteUInt32(data, offset + 8, control.Height);
        PngBinaryWriter.WriteUInt32(data, offset + 12, control.XOffset);
        PngBinaryWriter.WriteUInt32(data, offset + 16, control.YOffset);
        PngBinaryWriter.WriteUInt16(data, offset + 20, control.DelayNumerator);
        PngBinaryWriter.WriteUInt16(data, offset + 22, control.DelayDenominator);
        data[offset + 24] = (byte)control.DisposeOperation;
        data[offset + 25] = (byte)control.BlendOperation;
    }

    internal static PngChunk WriteChunk(PngFrameControl control)
    {
        return CreateChunk(WriteData(control));
    }

    internal static void ValidateDimensions(PngFrameControl control)
    {
        if (control.Width == 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (control.Height == 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }
    }

    internal static void ValidateDisposeOperation(PngApngDisposeOperation disposeOperation)
    {
        int value = (int)disposeOperation;
        if (value < PngConstants.ApngDisposeOperationNone || value > PngConstants.ApngDisposeOperationPrevious)
        {
            throw new ArgumentOutOfRangeException("DisposeOperation");
        }
    }

    internal static void ValidateBlendOperation(PngApngBlendOperation blendOperation)
    {
        int value = (int)blendOperation;
        if (value < PngConstants.ApngBlendOperationSource || value > PngConstants.ApngBlendOperationOver)
        {
            throw new ArgumentOutOfRangeException("BlendOperation");
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
        chunk.Type = PngConstants.ChunkFrameControl;
        chunk.Data = data;
        chunk.Length = (uint)chunk.Data.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = false;
        chunk.IsPublic = false;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
    }
}
