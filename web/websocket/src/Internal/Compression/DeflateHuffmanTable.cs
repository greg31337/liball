using System;

namespace websocket;

internal class DeflateHuffmanTable
{
    internal bool Valid;
    internal int MaximumCodeLength;
    internal int[] Codes = Array.Empty<int>();
    internal int[] CodeLengths = Array.Empty<int>();
}

internal class DeflateHuffmanBuildResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal DeflateHuffmanTable Table = new DeflateHuffmanTable();
}

internal static class DeflateHuffmanBuilder
{
    internal static DeflateHuffmanBuildResult BuildTable(
        int[] codeLengths,
        int maximumCodeLength,
        bool allowEmpty,
        bool allowSingleSymbol)
    {
        DeflateHuffmanBuildResult result = new DeflateHuffmanBuildResult();
        if (codeLengths == null ||
            maximumCodeLength <= 0 ||
            maximumCodeLength > 15)
        {
            return result;
        }

        int[] counts = new int[maximumCodeLength + 1];
        int usedSymbolCount = 0;
        int largestLength = 0;
        int symbolIndex = 0;
        while (symbolIndex < codeLengths.Length)
        {
            int length = codeLengths[symbolIndex];
            if (length < 0 || length > maximumCodeLength)
            {
                return result;
            }

            if (length != 0)
            {
                counts[length] = counts[length] + 1;
                usedSymbolCount = usedSymbolCount + 1;
                if (length > largestLength)
                {
                    largestLength = length;
                }
            }

            symbolIndex = symbolIndex + 1;
        }

        if (usedSymbolCount == 0)
        {
            if (allowEmpty)
            {
                DeflateHuffmanTable empty = new DeflateHuffmanTable();
                empty.Valid = true;
                empty.Codes = new int[codeLengths.Length];
                empty.CodeLengths = CopyLengths(codeLengths);
                result.Status = WebSocketParseStatus.Complete;
                result.Table = empty;
            }

            return result;
        }

        int remainingCodes = 1;
        int bitCount = 1;
        while (bitCount <= maximumCodeLength)
        {
            remainingCodes = (remainingCodes << 1) - counts[bitCount];
            if (remainingCodes < 0)
            {
                return result;
            }

            bitCount = bitCount + 1;
        }

        bool permittedSingle = allowSingleSymbol &&
            usedSymbolCount == 1 &&
            largestLength == 1;
        if (remainingCodes != 0 && !permittedSingle)
        {
            return result;
        }

        int[] nextCodes = new int[maximumCodeLength + 1];
        int code = 0;
        bitCount = 1;
        while (bitCount <= maximumCodeLength)
        {
            code = (code + counts[bitCount - 1]) << 1;
            nextCodes[bitCount] = code;
            bitCount = bitCount + 1;
        }

        DeflateHuffmanTable table = new DeflateHuffmanTable();
        table.Valid = true;
        table.MaximumCodeLength = largestLength;
        table.Codes = new int[codeLengths.Length];
        table.CodeLengths = CopyLengths(codeLengths);
        symbolIndex = 0;
        while (symbolIndex < codeLengths.Length)
        {
            int length = codeLengths[symbolIndex];
            if (length != 0)
            {
                table.Codes[symbolIndex] = nextCodes[length];
                nextCodes[length] = nextCodes[length] + 1;
            }

            symbolIndex = symbolIndex + 1;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.Table = table;
        return result;
    }

    static int[] CopyLengths(int[] source)
    {
        int[] copy = new int[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }
}

internal static class DeflateHuffmanTables
{
    internal static DeflateHuffmanTable CreateFixedLiteralLengthTable()
    {
        int[] lengths = new int[288];
        int symbol = 0;
        while (symbol <= 143)
        {
            lengths[symbol] = 8;
            symbol = symbol + 1;
        }

        while (symbol <= 255)
        {
            lengths[symbol] = 9;
            symbol = symbol + 1;
        }

        while (symbol <= 279)
        {
            lengths[symbol] = 7;
            symbol = symbol + 1;
        }

        while (symbol <= 287)
        {
            lengths[symbol] = 8;
            symbol = symbol + 1;
        }

        return DeflateHuffmanBuilder.BuildTable(
            lengths,
            15,
            false,
            false).Table;
    }

    internal static DeflateHuffmanTable CreateFixedDistanceTable()
    {
        int[] lengths = new int[32];
        int symbol = 0;
        while (symbol < lengths.Length)
        {
            lengths[symbol] = 5;
            symbol = symbol + 1;
        }

        return DeflateHuffmanBuilder.BuildTable(
            lengths,
            15,
            false,
            false).Table;
    }
}
