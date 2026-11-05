namespace icc;

internal class ZlibDecodeResult
{
    internal bool Success;
    internal bool LimitExceeded;
    internal byte[] Bytes;
    internal string Error;

    internal ZlibDecodeResult()
    {
        Success = false;
        LimitExceeded = false;
        Bytes = new byte[0];
        Error = string.Empty;
    }
}

internal class DeflateBitReader
{
    internal System.ReadOnlyMemory<byte> Data;
    internal int BytePosition;
    internal int BitPosition;
    internal bool Failed;
    internal int LastRepeatExtra;
    internal int MaximumDistance;

    internal DeflateBitReader(System.ReadOnlyMemory<byte> data)
    {
        Data = data;
        BytePosition = 0;
        BitPosition = 0;
        Failed = false;
        LastRepeatExtra = 0;
        MaximumDistance = 32768;
    }

    internal int ReadBits(int count)
    {
        int value = 0;
        int bit = 0;
        while (bit < count)
        {
            if (BytePosition >= Data.Length)
            {
                Failed = true;
                return -1;
            }

            int current = (Data.Span[BytePosition] >> BitPosition) & 1;
            value = value | (current << bit);
            BitPosition = BitPosition + 1;
            if (BitPosition == 8)
            {
                BitPosition = 0;
                BytePosition = BytePosition + 1;
            }

            bit = bit + 1;
        }

        return value;
    }

    internal void AlignToByte()
    {
        if (BitPosition != 0)
        {
            BitPosition = 0;
            BytePosition = BytePosition + 1;
        }
    }

    internal int ReadLittleEndianUInt16()
    {
        if (BitPosition != 0 || BytePosition > Data.Length - 2)
        {
            Failed = true;
            return -1;
        }

        int value = Data.Span[BytePosition] | (Data.Span[BytePosition + 1] << 8);
        BytePosition = BytePosition + 2;
        return value;
    }
}

internal class DeflateOutput
{
    internal byte[] Buffer;
    internal int Count;
    internal int Maximum;
    internal bool LimitExceeded;
    internal bool Failed;

    internal DeflateOutput(int maximum)
    {
        Maximum = maximum;
        int initialSize = maximum < 4096 ? maximum : 4096;
        Buffer = new byte[initialSize];
        Count = 0;
        LimitExceeded = false;
        Failed = false;
    }

    internal void Append(byte value)
    {
        if (Count >= Maximum)
        {
            LimitExceeded = true;
            Failed = true;
            return;
        }

        EnsureCapacity(Count + 1);
        if (Failed)
        {
            return;
        }

        Buffer[Count] = value;
        Count = Count + 1;
    }

    internal void Copy(int distance, int length)
    {
        if (distance <= 0 || distance > Count)
        {
            Failed = true;
            return;
        }

        if (length > Maximum - Count)
        {
            LimitExceeded = true;
            Failed = true;
            return;
        }

        EnsureCapacity(Count + length);
        if (Failed)
        {
            return;
        }

        int copied = 0;
        while (copied < length)
        {
            Buffer[Count] = Buffer[Count - distance];
            Count = Count + 1;
            copied = copied + 1;
        }
    }

    internal void EnsureCapacity(int required)
    {
        if (required <= Buffer.Length)
        {
            return;
        }

        int expanded = Buffer.Length == 0 ? 1 : Buffer.Length;
        while (expanded < required)
        {
            int next = expanded > Maximum / 2 ? Maximum : expanded * 2;
            if (next <= expanded)
            {
                Failed = true;
                return;
            }

            expanded = next;
        }

        byte[] replacement = new byte[expanded];
        int index = 0;
        while (index < Count)
        {
            replacement[index] = Buffer[index];
            index = index + 1;
        }

        Buffer = replacement;
    }

    internal byte[] ToArray()
    {
        byte[] bytes = new byte[Count];
        int index = 0;
        while (index < Count)
        {
            bytes[index] = Buffer[index];
            index = index + 1;
        }

        return bytes;
    }
}

internal class DeflateHuffman
{
    internal int[] Lengths;
    internal int[] Codes;
    internal int[][] SymbolsByLength;
    internal int MaximumBits;
    internal bool IsValid;
    internal bool IsComplete;
    internal int SymbolCount;

    internal DeflateHuffman(int[] lengths)
    {
        Lengths = lengths;
        Codes = new int[lengths.Length];
        SymbolsByLength = new int[16][];
        int lookupIndex = 0;
        while (lookupIndex < SymbolsByLength.Length)
        {
            SymbolsByLength[lookupIndex] = new int[0];
            lookupIndex = lookupIndex + 1;
        }

        MaximumBits = 0;
        IsComplete = false;
        SymbolCount = 0;
        IsValid = BuildCodes();
    }

    internal bool BuildCodes()
    {
        int[] counts = new int[16];
        int symbol = 0;
        while (symbol < Lengths.Length)
        {
            int length = Lengths[symbol];
            if (length < 0 || length > 15)
            {
                return false;
            }

            if (length != 0)
            {
                counts[length] = counts[length] + 1;
                SymbolCount = SymbolCount + 1;
                if (length > MaximumBits)
                {
                    MaximumBits = length;
                }
            }

            symbol = symbol + 1;
        }

        if (MaximumBits == 0)
        {
            return false;
        }

        int available = 1;
        int bits = 1;
        while (bits <= 15)
        {
            available = (available << 1) - counts[bits];
            if (available < 0)
            {
                return false;
            }

            bits = bits + 1;
        }

        IsComplete = available == 0;

        int[] nextCodes = new int[16];
        int code = 0;
        bits = 1;
        while (bits <= 15)
        {
            code = (code + counts[bits - 1]) << 1;
            nextCodes[bits] = code;
            bits = bits + 1;
        }

        symbol = 0;
        while (symbol < Lengths.Length)
        {
            int length = Lengths[symbol];
            if (length != 0)
            {
                Codes[symbol] = ReverseBits(nextCodes[length], length);
                nextCodes[length] = nextCodes[length] + 1;
            }

            symbol = symbol + 1;
        }

        bits = 1;
        while (bits <= MaximumBits)
        {
            if (counts[bits] != 0)
            {
                int[] lookup = new int[1 << bits];
                int lookupIndex = 0;
                while (lookupIndex < lookup.Length)
                {
                    lookup[lookupIndex] = -1;
                    lookupIndex = lookupIndex + 1;
                }

                SymbolsByLength[bits] = lookup;
            }

            bits = bits + 1;
        }

        symbol = 0;
        while (symbol < Lengths.Length)
        {
            int length = Lengths[symbol];
            if (length != 0)
            {
                SymbolsByLength[length][Codes[symbol]] = symbol;
            }

            symbol = symbol + 1;
        }

        return true;
    }

    internal int Decode(DeflateBitReader reader)
    {
        int code = 0;
        int length = 1;
        while (length <= MaximumBits)
        {
            int bit = reader.ReadBits(1);
            if (bit < 0)
            {
                return -1;
            }

            code = code | (bit << (length - 1));
            int[] lookup = SymbolsByLength[length];
            if (code < lookup.Length)
            {
                int symbol = lookup[code];
                if (symbol >= 0)
                {
                    return symbol;
                }
            }

            length = length + 1;
        }

        return -1;
    }

    internal static int ReverseBits(int value, int count)
    {
        int reversed = 0;
        int index = 0;
        while (index < count)
        {
            reversed = (reversed << 1) | (value & 1);
            value = value >> 1;
            index = index + 1;
        }

        return reversed;
    }
}

internal static class ZlibCodec
{
    internal static readonly int[] LengthBases = new int[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258 };
    internal static readonly int[] LengthExtraBits = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 };
    internal static readonly int[] DistanceBases = new int[] { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577 };
    internal static readonly int[] DistanceExtraBits = new int[] { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };
    internal static readonly int[] CodeLengthOrder = new int[] { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
    internal static readonly DeflateHuffman FixedLiteralTree = CreateFixedLiteralTree();
    internal static readonly DeflateHuffman FixedDistanceTree = CreateFixedDistanceTree();

    internal static ZlibDecodeResult Decode(System.ReadOnlyMemory<byte> source, int maximumBytes)
    {
        ZlibDecodeResult result = new ZlibDecodeResult();
        if (maximumBytes < 0)
        {
            result.LimitExceeded = true;
            result.Error = "The configured decompression limit is negative.";
            return result;
        }

        if (source.Length < 6)
        {
            result.Error = "The zlib stream is shorter than its header and checksum.";
            return result;
        }

        int compressionMethod = source.Span[0];
        int flags = source.Span[1];
        int header = (compressionMethod << 8) | flags;
        if ((compressionMethod & 15) != 8 || (compressionMethod >> 4) > 7 || header % 31 != 0)
        {
            result.Error = "The zlib stream header is invalid or does not select DEFLATE.";
            return result;
        }

        if ((flags & 32) != 0)
        {
            result.Error = "Preset-dictionary zlib streams are not supported by ICC.2 text tags.";
            return result;
        }

        System.ReadOnlyMemory<byte> body = source.Slice(2, source.Length - 2);
        DeflateBitReader reader = new DeflateBitReader(body);
        reader.MaximumDistance = 1 << ((compressionMethod >> 4) + 8);
        DeflateOutput output = new DeflateOutput(maximumBytes);
        bool isFinal = false;
        while (!isFinal && !reader.Failed && !output.Failed)
        {
            int finalBit = reader.ReadBits(1);
            int blockType = reader.ReadBits(2);
            if (finalBit < 0 || blockType < 0)
            {
                break;
            }

            isFinal = finalBit != 0;
            if (blockType == 0)
            {
                DecodeStoredBlock(reader, output);
            }
            else if (blockType == 1)
            {
                DecodeCompressedBlock(reader, output, FixedLiteralTree, FixedDistanceTree);
            }
            else if (blockType == 2)
            {
                DecodeDynamicBlock(reader, output);
            }
            else
            {
                reader.Failed = true;
            }
        }

        reader.AlignToByte();
        if (output.LimitExceeded)
        {
            result.LimitExceeded = true;
            result.Error = $"The expanded zlib stream exceeds the configured maximum of {maximumBytes} bytes.";
            return result;
        }

        if (!isFinal || reader.Failed || output.Failed || reader.BytePosition != body.Length - 4)
        {
            result.Error = "The zlib DEFLATE stream is malformed or has trailing data.";
            return result;
        }

        uint expected = ReadBigEndianUInt32(body, reader.BytePosition);
        byte[] bytes = output.ToArray();
        uint actual = ComputeAdler32(bytes);
        if (actual != expected)
        {
            result.Error = "The zlib Adler-32 checksum does not match the expanded data.";
            return result;
        }

        result.Success = true;
        result.Bytes = bytes;
        return result;
    }

    internal static byte[] EncodeStored(byte[] source)
    {
        if (source == null)
        {
            throw new IccProfileWriteException("Bytes to compress cannot be null.");
        }

        int blockCount = source.Length == 0 ? 1 : ((source.Length - 1) / 65535) + 1;
        long encodedSize = 2L + source.Length + ((long)blockCount * 5L) + 4L;
        if (encodedSize > System.Array.MaxLength)
        {
            throw new IccProfileWriteException("The zlib stream exceeds the supported managed array size.");
        }

        byte[] encoded = new byte[(int)encodedSize];
        encoded[0] = 120;
        encoded[1] = 1;
        int sourceOffset = 0;
        int destinationOffset = 2;
        int block = 0;
        while (block < blockCount)
        {
            int remaining = source.Length - sourceOffset;
            int length = remaining > 65535 ? 65535 : remaining;
            bool isFinal = block == blockCount - 1;
            encoded[destinationOffset] = isFinal ? (byte)1 : (byte)0;
            encoded[destinationOffset + 1] = (byte)(length & 255);
            encoded[destinationOffset + 2] = (byte)((length >> 8) & 255);
            int complement = 65535 - length;
            encoded[destinationOffset + 3] = (byte)(complement & 255);
            encoded[destinationOffset + 4] = (byte)((complement >> 8) & 255);
            destinationOffset = destinationOffset + 5;
            int index = 0;
            while (index < length)
            {
                encoded[destinationOffset + index] = source[sourceOffset + index];
                index = index + 1;
            }

            sourceOffset = sourceOffset + length;
            destinationOffset = destinationOffset + length;
            block = block + 1;
        }

        uint checksum = ComputeAdler32(source);
        encoded[destinationOffset] = (byte)((checksum >> 24) & 255);
        encoded[destinationOffset + 1] = (byte)((checksum >> 16) & 255);
        encoded[destinationOffset + 2] = (byte)((checksum >> 8) & 255);
        encoded[destinationOffset + 3] = (byte)(checksum & 255);
        return encoded;
    }

    internal static void DecodeStoredBlock(DeflateBitReader reader, DeflateOutput output)
    {
        reader.AlignToByte();
        int length = reader.ReadLittleEndianUInt16();
        int complement = reader.ReadLittleEndianUInt16();
        if (length < 0 || complement < 0 || (length ^ 65535) != complement)
        {
            reader.Failed = true;
            return;
        }

        if (reader.BytePosition > reader.Data.Length - length)
        {
            reader.Failed = true;
            return;
        }

        int index = 0;
        while (index < length && !output.Failed)
        {
            output.Append(reader.Data.Span[reader.BytePosition + index]);
            index = index + 1;
        }

        reader.BytePosition = reader.BytePosition + length;
    }

    internal static void DecodeDynamicBlock(DeflateBitReader reader, DeflateOutput output)
    {
        int literalCountBits = reader.ReadBits(5);
        int distanceCountBits = reader.ReadBits(5);
        int codeLengthCountBits = reader.ReadBits(4);
        if (literalCountBits < 0 || distanceCountBits < 0 || codeLengthCountBits < 0)
        {
            reader.Failed = true;
            return;
        }

        int literalCount = literalCountBits + 257;
        int distanceCount = distanceCountBits + 1;
        int codeLengthCount = codeLengthCountBits + 4;
        if (literalCount > 286 || distanceCount > 32)
        {
            reader.Failed = true;
            return;
        }

        int[] codeLengthLengths = new int[19];
        int index = 0;
        while (index < codeLengthCount)
        {
            int length = reader.ReadBits(3);
            if (length < 0)
            {
                reader.Failed = true;
                return;
            }

            codeLengthLengths[CodeLengthOrder[index]] = length;
            index = index + 1;
        }

        DeflateHuffman codeLengthTree = new DeflateHuffman(codeLengthLengths);
        if (!codeLengthTree.IsValid || !codeLengthTree.IsComplete)
        {
            reader.Failed = true;
            return;
        }

        int totalCount = literalCount + distanceCount;
        int[] lengths = new int[totalCount];
        index = 0;
        while (index < totalCount)
        {
            int symbol = codeLengthTree.Decode(reader);
            if (symbol < 0)
            {
                reader.Failed = true;
                return;
            }

            if (symbol <= 15)
            {
                lengths[index] = symbol;
                index = index + 1;
            }
            else if (symbol == 16)
            {
                if (index == 0 || !RepeatCodeLength(reader, lengths, index, totalCount, lengths[index - 1], 3, 2))
                {
                    reader.Failed = true;
                    return;
                }

                index = index + 3 + reader.LastRepeatExtra;
            }
            else if (symbol == 17)
            {
                if (!RepeatCodeLength(reader, lengths, index, totalCount, 0, 3, 3))
                {
                    reader.Failed = true;
                    return;
                }

                index = index + 3 + reader.LastRepeatExtra;
            }
            else
            {
                if (!RepeatCodeLength(reader, lengths, index, totalCount, 0, 11, 7))
                {
                    reader.Failed = true;
                    return;
                }

                index = index + 11 + reader.LastRepeatExtra;
            }
        }

        int[] literalLengths = new int[literalCount];
        int[] distanceLengths = new int[distanceCount];
        index = 0;
        while (index < literalCount)
        {
            literalLengths[index] = lengths[index];
            index = index + 1;
        }

        index = 0;
        while (index < distanceCount)
        {
            distanceLengths[index] = lengths[literalCount + index];
            index = index + 1;
        }

        if (literalLengths[256] == 0)
        {
            reader.Failed = true;
            return;
        }

        DeflateHuffman literalTree = new DeflateHuffman(literalLengths);
        DeflateHuffman distanceTree = new DeflateHuffman(distanceLengths);
        if (!literalTree.IsValid || !distanceTree.IsValid
            || (!literalTree.IsComplete && literalTree.SymbolCount != 1)
            || (!distanceTree.IsComplete && distanceTree.SymbolCount != 1))
        {
            reader.Failed = true;
            return;
        }

        DecodeCompressedBlock(reader, output, literalTree, distanceTree);
    }

    internal static bool RepeatCodeLength(DeflateBitReader reader, int[] lengths, int start, int maximum, int value, int minimum, int extraBitCount)
    {
        int extra = reader.ReadBits(extraBitCount);
        if (extra < 0)
        {
            return false;
        }

        int count = minimum + extra;
        if (count > maximum - start)
        {
            return false;
        }

        int index = 0;
        while (index < count)
        {
            lengths[start + index] = value;
            index = index + 1;
        }

        reader.LastRepeatExtra = extra;
        return true;
    }

    internal static void DecodeCompressedBlock(DeflateBitReader reader, DeflateOutput output, DeflateHuffman literalTree, DeflateHuffman distanceTree)
    {
        bool ended = false;
        while (!ended && !reader.Failed && !output.Failed)
        {
            int symbol = literalTree.Decode(reader);
            if (symbol < 0)
            {
                reader.Failed = true;
            }
            else if (symbol < 256)
            {
                output.Append((byte)symbol);
            }
            else if (symbol == 256)
            {
                ended = true;
            }
            else if (symbol <= 285)
            {
                int lengthIndex = symbol - 257;
                int lengthExtra = reader.ReadBits(LengthExtraBits[lengthIndex]);
                int distanceSymbol = distanceTree.Decode(reader);
                if (lengthExtra < 0 || distanceSymbol < 0 || distanceSymbol >= 30)
                {
                    reader.Failed = true;
                    continue;
                }

                int distanceExtra = reader.ReadBits(DistanceExtraBits[distanceSymbol]);
                if (distanceExtra < 0)
                {
                    reader.Failed = true;
                    continue;
                }

                int length = LengthBases[lengthIndex] + lengthExtra;
                int distance = DistanceBases[distanceSymbol] + distanceExtra;
                if (distance > reader.MaximumDistance)
                {
                    reader.Failed = true;
                }
                else
                {
                    output.Copy(distance, length);
                }
            }
            else
            {
                reader.Failed = true;
            }
        }

        if (!ended)
        {
            reader.Failed = true;
        }
    }

    internal static DeflateHuffman CreateFixedLiteralTree()
    {
        int[] lengths = new int[288];
        int index = 0;
        while (index <= 143)
        {
            lengths[index] = 8;
            index = index + 1;
        }

        while (index <= 255)
        {
            lengths[index] = 9;
            index = index + 1;
        }

        while (index <= 279)
        {
            lengths[index] = 7;
            index = index + 1;
        }

        while (index <= 287)
        {
            lengths[index] = 8;
            index = index + 1;
        }

        return new DeflateHuffman(lengths);
    }

    internal static DeflateHuffman CreateFixedDistanceTree()
    {
        int[] lengths = new int[32];
        int index = 0;
        while (index < lengths.Length)
        {
            lengths[index] = 5;
            index = index + 1;
        }

        return new DeflateHuffman(lengths);
    }

    internal static uint ComputeAdler32(byte[] bytes)
    {
        uint first = 1;
        uint second = 0;
        int index = 0;
        while (index < bytes.Length)
        {
            first = (first + bytes[index]) % 65521;
            second = (second + first) % 65521;
            index = index + 1;
        }

        return (second << 16) | first;
    }

    internal static uint ReadBigEndianUInt32(System.ReadOnlyMemory<byte> bytes, int offset)
    {
        return ((uint)bytes.Span[offset] << 24)
            | ((uint)bytes.Span[offset + 1] << 16)
            | ((uint)bytes.Span[offset + 2] << 8)
            | bytes.Span[offset + 3];
    }
}
