using System;

namespace webp;

internal static class WebpVp8LPrefixCodeParser
{
    internal static WebpVp8LPrefixCodeTable BuildTable(int[] codeLengths)
    {
        ArgumentNullException.ThrowIfNull(codeLengths);
        ValidateCodeLengths(codeLengths);

        WebpVp8LPrefixCodeTable table = new WebpVp8LPrefixCodeTable();
        table.AlphabetSize = codeLengths.Length;
        table.MaxCodeLength = FindMaxCodeLength(codeLengths);
        int symbolCount = CountSymbols(codeLengths);
        if (symbolCount == 1)
        {
            table.IsSingleSymbol = true;
        }

        int[] lengthCounts = CountLengths(codeLengths, table.MaxCodeLength);
        ValidateCompleteTree(lengthCounts, symbolCount, table.MaxCodeLength);
        int[] nextCodes = ComputeNextCodes(lengthCounts, table.MaxCodeLength);

        int symbol = 0;
        while (symbol < codeLengths.Length)
        {
            int length = codeLengths[symbol];
            if (length != 0)
            {
                WebpVp8LPrefixCodeEntry entry = new WebpVp8LPrefixCodeEntry();
                entry.Symbol = symbol;
                entry.CodeLength = length;
                entry.CanonicalCode = nextCodes[length];
                entry.ReversedCode = ReverseBits(entry.CanonicalCode, length);
                table.Entries.Add(entry);
                nextCodes[length] = nextCodes[length] + 1;
            }

            symbol = symbol + 1;
        }

        return table;
    }

    internal static WebpVp8LCodeLengthsParseResult ParseCodeLengths(WebpVp8LBitReader reader, int alphabetSize)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ValidateAlphabetSize(alphabetSize);

        WebpVp8LCodeLengthsParseResult result = new WebpVp8LCodeLengthsParseResult();
        result.CodeLengths = new int[alphabetSize];
        if (reader.ReadBits(1) != 0)
        {
            ParseSimpleCodeLengths(result, reader, alphabetSize);
        }
        else
        {
            ParseNormalCodeLengths(result, reader, alphabetSize);
        }

        if (!result.HasErrors())
        {
            result.Table = BuildTable(result.CodeLengths);
        }

        Finish(result);
        return result;
    }

    internal static void ParseSimpleCodeLengths(WebpVp8LCodeLengthsParseResult result, WebpVp8LBitReader reader, int alphabetSize)
    {
        int numSymbols = (int)reader.ReadBits(1) + 1;
        int isFirst8Bits = (int)reader.ReadBits(1);
        int firstSymbolBits = 1 + 7 * isFirst8Bits;
        int symbol0 = (int)reader.ReadBits(firstSymbolBits);
        AddSimpleSymbol(result, symbol0, alphabetSize);

        if (numSymbols == 2)
        {
            int symbol1 = (int)reader.ReadBits(8);
            AddSimpleSymbol(result, symbol1, alphabetSize);
        }

        result.MaxSymbol = alphabetSize;
    }

    internal static void AddSimpleSymbol(WebpVp8LCodeLengthsParseResult result, int symbol, int alphabetSize)
    {
        if (symbol >= alphabetSize)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, "simple VP8L prefix-code symbol exceeds alphabet size");
            return;
        }

        result.CodeLengths[symbol] = 1;
    }

    internal static void ParseNormalCodeLengths(WebpVp8LCodeLengthsParseResult result, WebpVp8LBitReader reader, int alphabetSize)
    {
        int[] codeLengthCodeLengths = ReadCodeLengthCodeLengths(reader);
        WebpVp8LPrefixCodeTable codeLengthTable = BuildTable(codeLengthCodeLengths);
        int maxSymbol = ReadMaxSymbol(reader, alphabetSize);
        result.MaxSymbol = maxSymbol;
        if (maxSymbol > alphabetSize)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, "VP8L prefix-code max symbol exceeds alphabet size");
            return;
        }

        ReadNormalCodeLengths(result, reader, codeLengthTable, maxSymbol);
    }

    internal static int[] ReadCodeLengthCodeLengths(WebpVp8LBitReader reader)
    {
        int[] order = CreateCodeLengthCodeOrder();
        int[] lengths = new int[19];
        int numCodeLengths = 4 + (int)reader.ReadBits(4);
        int index = 0;
        while (index < numCodeLengths)
        {
            lengths[order[index]] = (int)reader.ReadBits(3);
            index = index + 1;
        }

        return lengths;
    }

    internal static int ReadMaxSymbol(WebpVp8LBitReader reader, int alphabetSize)
    {
        if (reader.ReadBits(1) == 0)
        {
            return alphabetSize;
        }

        int lengthBits = 2 + 2 * (int)reader.ReadBits(3);
        return 2 + (int)reader.ReadBits(lengthBits);
    }

    internal static void ReadNormalCodeLengths(WebpVp8LCodeLengthsParseResult result, WebpVp8LBitReader reader, WebpVp8LPrefixCodeTable codeLengthTable, int maxSymbol)
    {
        int symbol = 0;
        int codeCount = 0;
        int previousLength = 8;
        while (symbol < result.CodeLengths.Length && codeCount < maxSymbol)
        {
            int code = codeLengthTable.DecodeSymbol(reader);
            codeCount = codeCount + 1;
            if (code < 16)
            {
                result.CodeLengths[symbol] = code;
                if (code != 0)
                {
                    previousLength = code;
                }

                symbol = symbol + 1;
            }
            else
            {
                symbol = ApplyRepeatCode(result, reader, code, symbol, result.CodeLengths.Length, previousLength);
            }
        }
    }

    internal static int ApplyRepeatCode(WebpVp8LCodeLengthsParseResult result, WebpVp8LBitReader reader, int code, int symbol, int maxSymbol, int previousLength)
    {
        int repeatLength = 0;
        int repeatedValue = 0;
        if (code == 16)
        {
            repeatLength = 3 + (int)reader.ReadBits(2);
            repeatedValue = previousLength;
        }
        else if (code == 17)
        {
            repeatLength = 3 + (int)reader.ReadBits(3);
            repeatedValue = 0;
        }
        else
        {
            repeatLength = 11 + (int)reader.ReadBits(7);
            repeatedValue = 0;
        }

        if (symbol + repeatLength > maxSymbol)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, "VP8L prefix-code repeat exceeds max symbol");
            return maxSymbol;
        }

        int index = 0;
        while (index < repeatLength)
        {
            result.CodeLengths[symbol + index] = repeatedValue;
            index = index + 1;
        }

        return symbol + repeatLength;
    }

    internal static int[] CreateCodeLengthCodeOrder()
    {
        return new int[]
        {
            17, 18, 0, 1, 2, 3, 4, 5, 16, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15
        };
    }

    internal static void ValidateCodeLengths(int[] codeLengths)
    {
        if (codeLengths.Length == 0)
        {
            throw new ArgumentException("prefix-code alphabet must not be empty", "codeLengths");
        }

        int index = 0;
        while (index < codeLengths.Length)
        {
            if (codeLengths[index] < 0 || codeLengths[index] > 15)
            {
                throw new ArgumentOutOfRangeException("codeLengths");
            }

            index = index + 1;
        }
    }

    internal static void ValidateAlphabetSize(int alphabetSize)
    {
        if (alphabetSize <= 0)
        {
            throw new ArgumentOutOfRangeException("alphabetSize");
        }
    }

    internal static int FindMaxCodeLength(int[] codeLengths)
    {
        int max = 0;
        int index = 0;
        while (index < codeLengths.Length)
        {
            if (codeLengths[index] > max)
            {
                max = codeLengths[index];
            }

            index = index + 1;
        }

        return max;
    }

    internal static int CountSymbols(int[] codeLengths)
    {
        int count = 0;
        int index = 0;
        while (index < codeLengths.Length)
        {
            if (codeLengths[index] != 0)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static int[] CountLengths(int[] codeLengths, int maxCodeLength)
    {
        int[] counts = new int[maxCodeLength + 1];
        int index = 0;
        while (index < codeLengths.Length)
        {
            int length = codeLengths[index];
            if (length != 0)
            {
                counts[length] = counts[length] + 1;
            }

            index = index + 1;
        }

        return counts;
    }

    internal static void ValidateCompleteTree(int[] lengthCounts, int symbolCount, int maxCodeLength)
    {
        if (symbolCount == 0)
        {
            throw new ArgumentException("prefix code must contain at least one symbol", "codeLengths");
        }

        if (symbolCount == 1)
        {
            if (maxCodeLength != 1)
            {
                throw new ArgumentException("single-symbol prefix code must use code length 1", "codeLengths");
            }

            return;
        }

        int expected = 1 << maxCodeLength;
        int total = 0;
        int length = 1;
        while (length <= maxCodeLength)
        {
            total = total + (lengthCounts[length] << (maxCodeLength - length));
            length = length + 1;
        }

        if (total != expected)
        {
            throw new ArgumentException("prefix code lengths must form a complete binary tree", "codeLengths");
        }
    }

    internal static int[] ComputeNextCodes(int[] lengthCounts, int maxCodeLength)
    {
        int[] nextCodes = new int[maxCodeLength + 1];
        int code = 0;
        int length = 1;
        while (length <= maxCodeLength)
        {
            code = (code + lengthCounts[length - 1]) << 1;
            nextCodes[length] = code;
            length = length + 1;
        }

        return nextCodes;
    }

    internal static int ReverseBits(int code, int length)
    {
        int reversed = 0;
        int index = 0;
        while (index < length)
        {
            reversed = (reversed << 1) | ((code >> index) & 1);
            index = index + 1;
        }

        return reversed;
    }

    internal static void AddDiagnostic(WebpVp8LCodeLengthsParseResult result, WebpDiagnosticSeverity severity, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(severity, -1, WebpConstants.FourCcVp8LString, message));
    }

    internal static void Finish(WebpVp8LCodeLengthsParseResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
