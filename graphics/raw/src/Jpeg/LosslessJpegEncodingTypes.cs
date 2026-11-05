namespace raw;

internal class LosslessJpegEncodeParameters
{
    internal int Predictor;
    internal int PointTransform;
    internal int RestartInterval;
    internal bool InterleavedScan;
    internal bool OptimizeHuffmanTable;

    internal LosslessJpegEncodeParameters(int predictor, int pointTransform, int restartInterval, bool interleavedScan, bool optimizeHuffmanTable)
    {
        Predictor = predictor;
        PointTransform = pointTransform;
        RestartInterval = restartInterval;
        InterleavedScan = interleavedScan;
        OptimizeHuffmanTable = optimizeHuffmanTable;
    }
}

internal class LosslessJpegHuffmanDefinition
{
    internal byte[]? CodeLengthCounts;
    internal byte[]? Symbols;
    internal HuffmanTable? Table;
}

internal class LosslessJpegOutput
{
    internal byte[]? Destination;
    internal int Offset;
    internal uint BitBuffer;
    internal int BitCount;

    internal LosslessJpegOutput(byte[]? destination)
    {
        Destination = destination;
        Offset = 0;
        BitBuffer = 0;
        BitCount = 0;
    }

    internal void WriteRawByte(byte value)
    {
        if (Offset == int.MaxValue)
        {
            throw new RawWriteException(RawDiagnosticCode.ArithmeticOverflow, "Lossless JPEG output exceeds the maximum managed array length.", Offset);
        }
        if (Destination != null)
        {
            if (Offset >= Destination.Length)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidLength, "Lossless JPEG output exceeds its planned length.", Offset);
            }
            Destination[Offset] = value;
        }
        Offset++;
    }

    internal void WriteRawUInt16(int value)
    {
        WriteRawByte(BitUtility.WrapUInt8(value >> 8));
        WriteRawByte(BitUtility.WrapUInt8(value));
    }

    internal void WriteBits(uint value, int count)
    {
        BitUtility.ValidateBitCount(count);
        if (count < 32 && value > BitUtility.Mask(count))
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A lossless JPEG entropy value does not fit its bit count.", Offset);
        }
        int bit = count - 1;
        while (bit >= 0)
        {
            BitBuffer = (BitBuffer << 1) | ((value >> bit) & 1U);
            BitCount++;
            if (BitCount == 8)
            {
                WriteEntropyByte((byte)BitBuffer);
                BitBuffer = 0;
                BitCount = 0;
            }
            bit--;
        }
    }

    internal void FinishEntropy()
    {
        while (BitCount != 0)
        {
            WriteBits(1, 1);
        }
    }

    internal void WriteEntropyByte(byte value)
    {
        WriteRawByte(value);
        if (value == 0xff)
        {
            WriteRawByte(0x00);
        }
    }
}
