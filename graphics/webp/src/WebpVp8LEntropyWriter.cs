using System;
using System.Collections.Generic;

namespace webp;

internal static class WebpVp8LEntropyWriter
{
    internal static WebpVp8LEntropyCode BuildCode(int alphabetSize, int[] symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        ValidateAlphabetSize(alphabetSize);

        bool[] used = new bool[alphabetSize];
        int index = 0;
        while (index < symbols.Length)
        {
            int symbol = symbols[index];
            if (symbol < 0 || symbol >= alphabetSize)
            {
                throw new ArgumentOutOfRangeException("symbols");
            }

            used[symbol] = true;
            index = index + 1;
        }

        return BuildCodeFromUsedSymbols(alphabetSize, used);
    }

    internal static void WriteImageData(WebpBitWriter writer, List<WebpVp8LLz77Token> tokens, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(tokens);
        ValidateDimensions(width, height);

        WebpVp8LEntropyCodeGroup group = BuildCodeGroup(tokens);
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);
        WriteCode(writer, group.Green);
        WriteCode(writer, group.Red);
        WriteCode(writer, group.Blue);
        WriteCode(writer, group.Alpha);
        WriteCode(writer, group.Distance);
        WriteTokens(writer, tokens, group);
    }

    internal static void WriteSubresolutionImageData(WebpBitWriter writer, List<WebpVp8LLz77Token> tokens, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(tokens);
        ValidateDimensions(width, height);

        WebpVp8LEntropyCodeGroup group = BuildCodeGroup(tokens);
        writer.WriteBits(0, 1);
        WriteCode(writer, group.Green);
        WriteCode(writer, group.Red);
        WriteCode(writer, group.Blue);
        WriteCode(writer, group.Alpha);
        WriteCode(writer, group.Distance);
        WriteTokens(writer, tokens, group);
    }

    internal static void WriteCode(WebpBitWriter writer, WebpVp8LEntropyCode code)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(code);

        writer.WriteBits(0, 1);
        WebpVp8LEntropyCode codeLengthCode = BuildCodeLengthCode(code.CodeLengths);
        WriteCodeLengthCodeLengths(writer, codeLengthCode);
        writer.WriteBits(0, 1);
        int symbol = 0;
        while (symbol < code.AlphabetSize)
        {
            WriteSymbol(writer, codeLengthCode, code.CodeLengths[symbol]);
            symbol = symbol + 1;
        }
    }

    internal static void WriteSymbol(WebpBitWriter writer, WebpVp8LEntropyCode code, int symbol)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(code);
        WriteSymbol(writer, code.Table, symbol);
    }

    internal static void WriteSymbol(WebpBitWriter writer, WebpVp8LPrefixCodeTable table, int symbol)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(table);
        if (table.IsSingleSymbol)
        {
            return;
        }

        int index = 0;
        while (index < table.Entries.Count)
        {
            WebpVp8LPrefixCodeEntry entry = table.Entries[index];
            if (entry.Symbol == symbol)
            {
                writer.WriteBits((uint)entry.ReversedCode, entry.CodeLength);
                return;
            }

            index = index + 1;
        }

        throw new ArgumentException("symbol is missing from the prefix code", "symbol");
    }

    internal static WebpVp8LEntropyCodeGroup BuildCodeGroup(List<WebpVp8LLz77Token> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        WebpVp8LEntropyCodeGroup group = new WebpVp8LEntropyCodeGroup();
        group.Green = BuildCode(280, CollectGreenSymbols(tokens));
        group.Red = BuildCode(256, CollectChannelSymbols(tokens, 0));
        group.Blue = BuildCode(256, CollectChannelSymbols(tokens, 2));
        group.Alpha = BuildCode(256, CollectChannelSymbols(tokens, 3));
        group.Distance = BuildCode(40, CollectDistanceSymbols(tokens));
        return group;
    }

    internal static void WriteTokens(WebpBitWriter writer, List<WebpVp8LLz77Token> tokens, WebpVp8LEntropyCodeGroup group)
    {
        int index = 0;
        while (index < tokens.Count)
        {
            WebpVp8LLz77Token token = tokens[index];
            if (token.IsLiteral)
            {
                WriteSymbol(writer, group.Green, token.Color.Green);
                WriteSymbol(writer, group.Red, token.Color.Red);
                WriteSymbol(writer, group.Blue, token.Color.Blue);
                WriteSymbol(writer, group.Alpha, token.Color.Alpha);
            }
            else
            {
                WriteReferenceToken(writer, group, token);
            }

            index = index + 1;
        }
    }

    internal static void WriteReferenceToken(WebpBitWriter writer, WebpVp8LEntropyCodeGroup group, WebpVp8LLz77Token token)
    {
        WebpVp8LPrefixValue lengthValue = WebpVp8LLz77Encoder.EncodePrefixValue(token.Length);
        int greenSymbol = 256 + lengthValue.PrefixCode;
        WebpVp8LPrefixValue distanceValue = WebpVp8LLz77Encoder.EncodePrefixValue(token.DistanceCode);
        WriteSymbol(writer, group.Green, greenSymbol);
        writer.WriteBits((uint)lengthValue.ExtraValue, lengthValue.ExtraBitCount);
        WriteSymbol(writer, group.Distance, distanceValue.PrefixCode);
        writer.WriteBits((uint)distanceValue.ExtraValue, distanceValue.ExtraBitCount);
    }

    internal static WebpVp8LEntropyCode BuildCodeFromUsedSymbols(int alphabetSize, bool[] used)
    {
        int symbolCount = CountUsedSymbols(used);
        if (symbolCount == 0)
        {
            used[0] = true;
            symbolCount = 1;
        }

        WebpVp8LEntropyCode code = new WebpVp8LEntropyCode();
        code.AlphabetSize = alphabetSize;
        code.CodeLengths = new int[alphabetSize];
        if (symbolCount == 1)
        {
            AssignSingleSymbolLength(code.CodeLengths, used);
        }
        else
        {
            AssignCompleteCodeLengths(code.CodeLengths, used, symbolCount);
        }

        code.Table = WebpVp8LPrefixCodeParser.BuildTable(code.CodeLengths);
        return code;
    }

    internal static void AssignSingleSymbolLength(int[] codeLengths, bool[] used)
    {
        int index = 0;
        while (index < used.Length)
        {
            if (used[index])
            {
                codeLengths[index] = 1;
                return;
            }

            index = index + 1;
        }
    }

    internal static void AssignCompleteCodeLengths(int[] codeLengths, bool[] used, int symbolCount)
    {
        int longLength = CeilLog2(symbolCount);
        int shortCount = (1 << longLength) - symbolCount;
        int assignedShort = 0;
        int index = 0;
        while (index < used.Length)
        {
            if (used[index])
            {
                if (assignedShort < shortCount)
                {
                    codeLengths[index] = longLength - 1;
                    assignedShort = assignedShort + 1;
                }
                else
                {
                    codeLengths[index] = longLength;
                }
            }

            index = index + 1;
        }
    }

    internal static WebpVp8LEntropyCode BuildCodeLengthCode(int[] codeLengths)
    {
        bool[] used = new bool[19];
        int index = 0;
        while (index < codeLengths.Length)
        {
            used[codeLengths[index]] = true;
            index = index + 1;
        }

        return BuildCodeFromUsedSymbols(19, used);
    }

    internal static void WriteCodeLengthCodeLengths(WebpBitWriter writer, WebpVp8LEntropyCode codeLengthCode)
    {
        int[] order = CreateCodeLengthCodeOrder();
        int count = FindCodeLengthCodeCount(codeLengthCode.CodeLengths, order);
        writer.WriteBits((uint)(count - 4), 4);
        int index = 0;
        while (index < count)
        {
            writer.WriteBits((uint)codeLengthCode.CodeLengths[order[index]], 3);
            index = index + 1;
        }
    }

    internal static int FindCodeLengthCodeCount(int[] codeLengths, int[] order)
    {
        int count = 4;
        int index = 0;
        while (index < order.Length)
        {
            if (codeLengths[order[index]] != 0)
            {
                count = index + 1;
            }

            index = index + 1;
        }

        if (count < 4)
        {
            return 4;
        }

        return count;
    }

    internal static int[] CollectGreenSymbols(List<WebpVp8LLz77Token> tokens)
    {
        bool[] used = new bool[280];
        int index = 0;
        while (index < tokens.Count)
        {
            WebpVp8LLz77Token token = tokens[index];
            if (token.IsLiteral)
            {
                used[token.Color.Green] = true;
            }
            else
            {
                used[WebpVp8LLz77Encoder.EncodeLengthSymbol(token.Length)] = true;
            }

            index = index + 1;
        }

        return UsedSymbolsToArray(used);
    }

    internal static int[] CollectChannelSymbols(List<WebpVp8LLz77Token> tokens, int channel)
    {
        bool[] used = new bool[256];
        int index = 0;
        while (index < tokens.Count)
        {
            WebpVp8LLz77Token token = tokens[index];
            if (token.IsLiteral)
            {
                used[ChannelValue(token.Color, channel)] = true;
            }

            index = index + 1;
        }

        return UsedSymbolsToArray(used);
    }

    internal static int[] CollectDistanceSymbols(List<WebpVp8LLz77Token> tokens)
    {
        bool[] used = new bool[40];
        int index = 0;
        while (index < tokens.Count)
        {
            WebpVp8LLz77Token token = tokens[index];
            if (!token.IsLiteral)
            {
                WebpVp8LPrefixValue distanceValue = WebpVp8LLz77Encoder.EncodePrefixValue(token.DistanceCode);
                used[distanceValue.PrefixCode] = true;
            }

            index = index + 1;
        }

        return UsedSymbolsToArray(used);
    }

    internal static int[] UsedSymbolsToArray(bool[] used)
    {
        int count = CountUsedSymbols(used);
        int[] symbols = new int[count];
        int output = 0;
        int index = 0;
        while (index < used.Length)
        {
            if (used[index])
            {
                symbols[output] = index;
                output = output + 1;
            }

            index = index + 1;
        }

        return symbols;
    }

    internal static int ChannelValue(WebpVp8LColor color, int channel)
    {
        if (channel == 0)
        {
            return color.Red;
        }

        if (channel == 2)
        {
            return color.Blue;
        }

        return color.Alpha;
    }

    internal static int CountUsedSymbols(bool[] used)
    {
        int count = 0;
        int index = 0;
        while (index < used.Length)
        {
            if (used[index])
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static int CeilLog2(int value)
    {
        int length = 0;
        int limit = 1;
        while (limit < value)
        {
            limit = limit << 1;
            length = length + 1;
        }

        return length;
    }

    internal static int[] CreateCodeLengthCodeOrder()
    {
        return new int[]
        {
            17, 18, 0, 1, 2, 3, 4, 5, 16, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15
        };
    }

    internal static void ValidateAlphabetSize(int alphabetSize)
    {
        if (alphabetSize <= 0)
        {
            throw new ArgumentOutOfRangeException("alphabetSize");
        }
    }

    internal static void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }
    }
}
