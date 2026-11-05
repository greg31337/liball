namespace jpeg;

internal class JpegBitWriter
{
    internal JpegByteWriter Writer = new JpegByteWriter();
    internal int BitBuffer;
    internal int BitsInBuffer;
    internal long BitsWritten;
    internal int StuffedByteCount;

    internal JpegBitWriter()
    {
        Writer = new JpegByteWriter();
        Initialize();
    }

    internal static JpegBitWriter WithCapacity(int capacity)
    {
        return FromWriter(JpegByteWriter.WithCapacity(capacity));
    }

    internal static JpegBitWriter FromStream(System.IO.Stream stream)
    {
        return FromWriter(JpegByteWriter.FromStream(stream));
    }

    internal static JpegBitWriter FromWriter(JpegByteWriter writer)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        JpegBitWriter bitWriter = new JpegBitWriter();
        bitWriter.Writer = writer;
        bitWriter.Initialize();
        return bitWriter;
    }

    internal void Initialize()
    {
        BitBuffer = 0;
        BitsInBuffer = 0;
        BitsWritten = 0;
        StuffedByteCount = 0;
    }

    internal void WriteBit(int bit)
    {
        ValidateBit(bit);
        BitBuffer = (BitBuffer << 1) | bit;
        BitsInBuffer = BitsInBuffer + 1;
        BitsWritten = BitsWritten + 1;
        if (BitsInBuffer == 8)
        {
            FlushFullByte();
        }
    }

    internal void WriteBits(int value, int count)
    {
        ValidateBitCount(count);
        ValidateBitsValue(value, count);

        int index = count - 1;
        while (index >= 0)
        {
            int bit = (value >> index) & 1;
            WriteBit(bit);
            index = index - 1;
        }
    }

    internal void FlushToByteBoundary()
    {
        FlushToByteBoundaryWithPadBit(1);
    }

    internal void FlushToByteBoundaryWithPadBit(int padBit)
    {
        ValidateBit(padBit);
        while (BitsInBuffer > 0)
        {
            WriteBit(padBit);
        }
    }

    internal void WriteMarkerAfterByteAlign(int markerValue)
    {
        FlushToByteBoundary();
        JpegMarkerSegmentWriter markerWriter = new JpegMarkerSegmentWriter();
        markerWriter.ValidateMarkerValue(markerValue);
        if (markerWriter.MarkerHasSegmentLength(markerValue))
        {
            throw JpegWriteValidationException.Create("Length-bearing markers cannot be written inside entropy data.");
        }

        Writer.WriteUInt16(markerValue);
    }

    internal byte[] ToArray()
    {
        return Writer.ToArray();
    }

    internal void FlushFullByte()
    {
        int value = BitBuffer & 255;
        Writer.WriteByte(value);
        if (value == JpegConstants.MarkerPrefix)
        {
            Writer.WriteByte(JpegConstants.MarkerStuffedByte);
            StuffedByteCount = StuffedByteCount + 1;
        }

        BitBuffer = 0;
        BitsInBuffer = 0;
    }

    internal void ValidateBit(int bit)
    {
        if (bit != 0 && bit != 1)
        {
            throw new System.ArgumentOutOfRangeException("bit");
        }
    }

    internal void ValidateBitCount(int count)
    {
        if (count < 0 || count > 24)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }
    }

    internal void ValidateBitsValue(int value, int count)
    {
        if (value < 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        if (count == 0)
        {
            if (value != 0)
            {
                throw new System.ArgumentOutOfRangeException("value");
            }

            return;
        }

        int maximum = (1 << count) - 1;
        if (value > maximum)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }
    }
}
