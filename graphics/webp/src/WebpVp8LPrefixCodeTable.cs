using System;
using System.Collections.Generic;

namespace webp;

internal class WebpVp8LPrefixCodeTable
{
    internal int AlphabetSize;
    internal int MaxCodeLength;
    internal bool IsSingleSymbol;
    internal List<WebpVp8LPrefixCodeEntry> Entries;

    internal WebpVp8LPrefixCodeTable()
    {
        AlphabetSize = 0;
        MaxCodeLength = 0;
        IsSingleSymbol = false;
        Entries = new List<WebpVp8LPrefixCodeEntry>();
    }

    internal int DecodeSymbol(WebpVp8LBitReader reader)
    {
        if (IsSingleSymbol)
        {
            return Entries[0].Symbol;
        }

        int code = 0;
        int length = 1;
        while (length <= MaxCodeLength)
        {
            int bit = reader.ReadBit();
            code = code | (bit << (length - 1));
            int symbol = FindSymbol(code, length);
            if (symbol >= 0)
            {
                return symbol;
            }

            length = length + 1;
        }

        throw new ArgumentException("prefix code did not match any symbol", "reader");
    }

    internal int FindSymbol(int code, int length)
    {
        int index = 0;
        while (index < Entries.Count)
        {
            WebpVp8LPrefixCodeEntry entry = Entries[index];
            if (entry.CodeLength == length && entry.ReversedCode == code)
            {
                return entry.Symbol;
            }

            index = index + 1;
        }

        return -1;
    }
}
