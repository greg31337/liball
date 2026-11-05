namespace raw;

internal class HuffmanTable
{
    internal int[] CodeLengthCounts;
    internal byte[] Symbols;
    internal int[] FirstCodes;
    internal int[] FirstSymbolIndexes;
    internal int[] FastSymbols;
    internal byte[] FastCodeLengths;
    internal uint[] EncodeCodes;
    internal byte[] EncodeCodeLengths;
    internal int FastBitCount;
    internal int MaxCodeLength;
    internal bool Complete;

    internal HuffmanTable(byte[]? codeLengthCounts, byte[]? symbols, int fastBitCount)
    {
        if (codeLengthCounts == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Huffman code-length counts are required.", -1);
        }
        if (symbols == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Huffman symbols are required.", -1);
        }
        if (codeLengthCounts.Length != 16)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A Huffman table must provide 16 code-length counts.", -1);
        }
        if (fastBitCount < 1 || fastBitCount > 16)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The Huffman fast lookup width must be between 1 and 16.", -1);
        }

        int symbolCount = 0;
        int length = 0;
        while (length < codeLengthCounts.Length)
        {
            symbolCount += codeLengthCounts[length];
            length++;
        }
        if (symbolCount == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A Huffman table must contain at least one symbol.", -1);
        }
        if (symbolCount > 256)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A byte-symbol Huffman table cannot contain more than 256 symbols.", -1);
        }
        if (symbols.Length != symbolCount)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Huffman symbol count does not match the code-length counts.", -1);
        }

        bool[] seenSymbols = new bool[256];
        int symbolIndex = 0;
        while (symbolIndex < symbols.Length)
        {
            byte symbol = symbols[symbolIndex];
            if (seenSymbols[symbol])
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A Huffman symbol appears more than once.", -1);
            }
            seenSymbols[symbol] = true;
            symbolIndex++;
        }

        int availableCodes = 1;
        length = 1;
        while (length <= 16)
        {
            availableCodes = availableCodes * 2 - codeLengthCounts[length - 1];
            if (availableCodes < 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The Huffman code-length counts are oversubscribed.", -1);
            }
            length++;
        }

        CodeLengthCounts = new int[17];
        Symbols = new byte[symbolCount];
        FirstCodes = new int[17];
        FirstSymbolIndexes = new int[17];
        EncodeCodes = new uint[256];
        EncodeCodeLengths = new byte[256];
        FastBitCount = fastBitCount;
        FastSymbols = new int[1 << fastBitCount];
        FastCodeLengths = new byte[1 << fastBitCount];
        System.Array.Fill(FastSymbols, -1);
        Complete = availableCodes == 0;
        MaxCodeLength = 0;

        length = 1;
        while (length <= 16)
        {
            CodeLengthCounts[length] = codeLengthCounts[length - 1];
            if (CodeLengthCounts[length] != 0)
            {
                MaxCodeLength = length;
            }
            length++;
        }
        symbolIndex = 0;
        while (symbolIndex < symbolCount)
        {
            Symbols[symbolIndex] = symbols[symbolIndex];
            symbolIndex++;
        }

        BuildCanonicalTables();
    }

    internal byte DecodeSymbol(MsbBitReader reader)
    {
        if (reader == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A Huffman bit reader is required.", -1);
        }

        long remaining = reader.GetRemainingBitCount();
        int availableFastBits = FastBitCount;
        if (remaining < availableFastBits)
        {
            availableFastBits = (int)remaining;
        }
        if (availableFastBits > 0)
        {
            uint prefix = reader.PeekBits(availableFastBits);
            int fastIndex = (int)(prefix << (FastBitCount - availableFastBits));
            int fastLength = FastCodeLengths[fastIndex];
            if (fastLength != 0 && fastLength <= availableFastBits)
            {
                reader.ConsumeBits(fastLength);
                return (byte)FastSymbols[fastIndex];
            }
        }

        int code = 0;
        int length = 1;
        while (length <= MaxCodeLength)
        {
            code = (code << 1) | (int)reader.ReadBits(1);
            int offset = code - FirstCodes[length];
            if (offset >= 0 && offset < CodeLengthCounts[length])
            {
                return Symbols[FirstSymbolIndexes[length] + offset];
            }
            length++;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The bit sequence does not identify a Huffman symbol.", reader.Core.GetCurrentByteOffset());
    }

    internal void EncodeSymbol(MsbBitWriter writer, byte symbol)
    {
        if (writer == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A Huffman bit writer is required.", -1);
        }
        int length = EncodeCodeLengths[symbol];
        if (length == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The symbol is not present in the Huffman table.", -1);
        }
        writer.WriteBits(EncodeCodes[symbol], length);
    }

    internal uint GetEncodeCode(byte symbol)
    {
        if (EncodeCodeLengths[symbol] == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The symbol is not present in the Huffman table.", -1);
        }
        return EncodeCodes[symbol];
    }

    internal int GetEncodeCodeLength(byte symbol)
    {
        int length = EncodeCodeLengths[symbol];
        if (length == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The symbol is not present in the Huffman table.", -1);
        }
        return length;
    }

    internal int GetSymbolCount()
    {
        return Symbols.Length;
    }

    internal int GetMaxCodeLength()
    {
        return MaxCodeLength;
    }

    internal bool IsComplete()
    {
        return Complete;
    }

    internal void BuildCanonicalTables()
    {
        int code = 0;
        int symbolIndex = 0;
        int length = 1;
        while (length <= 16)
        {
            code <<= 1;
            FirstCodes[length] = code;
            FirstSymbolIndexes[length] = symbolIndex;
            int indexWithinLength = 0;
            while (indexWithinLength < CodeLengthCounts[length])
            {
                int canonicalCode = code + indexWithinLength;
                byte symbol = Symbols[symbolIndex + indexWithinLength];
                EncodeCodes[symbol] = (uint)canonicalCode;
                EncodeCodeLengths[symbol] = (byte)length;
                if (length <= FastBitCount)
                {
                    int suffixBits = FastBitCount - length;
                    int fastStart = canonicalCode << suffixBits;
                    int fastCount = 1 << suffixBits;
                    int fastIndex = 0;
                    while (fastIndex < fastCount)
                    {
                        FastSymbols[fastStart + fastIndex] = symbol;
                        FastCodeLengths[fastStart + fastIndex] = (byte)length;
                        fastIndex++;
                    }
                }
                indexWithinLength++;
            }
            code += CodeLengthCounts[length];
            symbolIndex += CodeLengthCounts[length];
            length++;
        }
    }
}
