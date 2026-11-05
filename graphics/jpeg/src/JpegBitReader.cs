namespace jpeg;

internal class JpegBitReader
{
    internal JpegByteReader Reader = new JpegByteReader(new byte[0]);
    internal long BitBuffer;
    internal int BitsAvailable;
    internal bool HasMarker;
    internal int MarkerValue;
    internal int MarkerCode;
    internal long MarkerOffset;
    internal long EntropyStartOffset;
    internal long EntropyBytesRead;
    internal int StuffedByteCount;

    internal JpegBitReader(byte[] data)
    {
        Reader = new JpegByteReader(data);
        Initialize();
    }

    internal static JpegBitReader FromStream(System.IO.Stream stream)
    {
        return FromReader(JpegByteReader.FromStream(stream));
    }

    internal static JpegBitReader FromReader(JpegByteReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        JpegBitReader bitReader = new JpegBitReader(new byte[0]);
        bitReader.Reader = reader;
        bitReader.Initialize();
        return bitReader;
    }

    internal void Initialize()
    {
        BitBuffer = 0;
        BitsAvailable = 0;
        HasMarker = false;
        MarkerValue = 0;
        MarkerCode = 0;
        MarkerOffset = -1;
        EntropyStartOffset = Reader.AbsoluteOffset;
        EntropyBytesRead = 0;
        StuffedByteCount = 0;
    }

    internal int ReadBit()
    {
        return ReadBits(1);
    }

    internal int ReadBits(int count)
    {
        ValidateBitCount(count);
        if (!EnsureBits(count))
        {
            throw JpegInvalidDataException.Create("JPEG entropy marker reached before enough bits were available.");
        }

        int value = PeekBits(count);
        DropBits(count);
        return value;
    }

    internal int PeekBits(int count)
    {
        ValidateBitCount(count);
        if (!EnsureBits(count))
        {
            throw JpegInvalidDataException.Create("JPEG entropy marker reached before enough bits were available.");
        }

        int shift = BitsAvailable - count;
        long mask = (1L << count) - 1L;
        return (int)((BitBuffer >> shift) & mask);
    }

    internal void DropBits(int count)
    {
        ValidateDropBitCount(count);
        BitsAvailable = BitsAvailable - count;
        if (BitsAvailable == 0)
        {
            BitBuffer = 0;
            return;
        }

        long mask = (1L << BitsAvailable) - 1L;
        BitBuffer = BitBuffer & mask;
    }

    internal void ByteAlign()
    {
        int paddingBits = BitsAvailable & 7;
        if (paddingBits > 0)
        {
            DropBits(paddingBits);
        }
    }

    internal bool EnsureBits(int count)
    {
        ValidateBitCount(count);
        while (BitsAvailable < count)
        {
            if (!FillByte())
            {
                return false;
            }
        }

        return true;
    }

    internal bool FillByte()
    {
        if (HasMarker)
        {
            return false;
        }

        long offset = Reader.AbsoluteOffset;
        int value = Reader.ReadByte();
        if (value == JpegConstants.MarkerPrefix)
        {
            int code = Reader.ReadByte();
            while (code == JpegConstants.MarkerPrefix)
            {
                code = Reader.ReadByte();
            }

            if (code != JpegConstants.MarkerStuffedByte)
            {
                SetMarkerLookahead(code, offset);
                return false;
            }

            value = JpegConstants.MarkerPrefix;
            StuffedByteCount = StuffedByteCount + 1;
        }

        BitBuffer = (BitBuffer << 8) | (long)value;
        BitsAvailable = BitsAvailable + 8;
        EntropyBytesRead = Reader.AbsoluteOffset - EntropyStartOffset;
        return true;
    }

    internal bool AdvanceToMarker()
    {
        ByteAlign();
        while (!HasMarker)
        {
            if (!Reader.UsesStream && Reader.Offset >= Reader.Length)
            {
                return false;
            }

            if (!FillByte())
            {
                return HasMarker;
            }

            BitsAvailable = 0;
            BitBuffer = 0;
        }

        return true;
    }

    internal JpegScannedMarker GetMarkerLookahead()
    {
        if (!HasMarker)
        {
            throw new System.InvalidOperationException("No JPEG marker lookahead is available.");
        }

        JpegMarkerScanner scanner = new JpegMarkerScanner(new byte[] { 0xFF, (byte)MarkerCode });
        return scanner.CreateMarker(MarkerValue, MarkerOffset, true, EntropyBytesRead);
    }

    internal JpegScannedMarker ConsumeMarkerLookahead()
    {
        JpegScannedMarker marker = GetMarkerLookahead();
        ClearMarkerLookahead();
        return marker;
    }

    internal void ClearMarkerLookahead()
    {
        HasMarker = false;
        MarkerValue = 0;
        MarkerCode = 0;
        MarkerOffset = -1;
        BitsAvailable = 0;
        BitBuffer = 0;
        EntropyStartOffset = Reader.AbsoluteOffset;
        EntropyBytesRead = 0;
        StuffedByteCount = 0;
    }

    internal void SetMarkerLookahead(int code, long offset)
    {
        HasMarker = true;
        MarkerCode = code;
        MarkerValue = (JpegConstants.MarkerPrefix << JpegConstants.MarkerPrefixShift) | code;
        MarkerOffset = offset;
        EntropyBytesRead = MarkerOffset - EntropyStartOffset;
        BitsAvailable = 0;
        BitBuffer = 0;
    }

    internal void ValidateBitCount(int count)
    {
        if (count < 0 || count > 24)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }
    }

    internal void ValidateDropBitCount(int count)
    {
        if (count < 0 || count > BitsAvailable)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }
    }
}
