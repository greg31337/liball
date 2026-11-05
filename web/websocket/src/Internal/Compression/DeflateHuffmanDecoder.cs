using System;

namespace websocket;

internal class DeflateHuffmanReadResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.NeedInput;
    internal int ConsumedCount;
    internal int Symbol = -1;
}

internal class DeflateHuffmanDecoder
{
    internal DeflateHuffmanTable Table;
    int CurrentCode;
    int CurrentCodeLength;
    bool Failed;

    internal DeflateHuffmanDecoder(DeflateHuffmanTable table)
    {
        if (table == null || !table.Valid || table.MaximumCodeLength <= 0)
        {
            throw new WebSocketStateException();
        }

        Table = table;
    }

    internal DeflateHuffmanReadResult ReadSymbol(
        DeflateBitReader reader,
        ReadOnlySpan<byte> source)
    {
        DeflateHuffmanReadResult result = new DeflateHuffmanReadResult();
        if (reader == null || Failed)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
            return result;
        }

        int sourceIndex = 0;
        while (CurrentCodeLength < Table.MaximumCodeLength)
        {
            DeflateBitReadResult bit = reader.ReadBits(
                source.Slice(sourceIndex),
                1);
            sourceIndex = sourceIndex + bit.ConsumedCount;
            if (bit.Status != WebSocketParseStatus.Complete)
            {
                result.Status = bit.Status;
                result.ConsumedCount = sourceIndex;
                return result;
            }

            CurrentCode = (CurrentCode << 1) | bit.Value;
            CurrentCodeLength = CurrentCodeLength + 1;
            int symbol = FindSymbol(CurrentCode, CurrentCodeLength);
            if (symbol >= 0)
            {
                result.Status = WebSocketParseStatus.Complete;
                result.ConsumedCount = sourceIndex;
                result.Symbol = symbol;
                CurrentCode = 0;
                CurrentCodeLength = 0;
                return result;
            }
        }

        Failed = true;
        result.Status = WebSocketParseStatus.ProtocolFailure;
        result.ConsumedCount = sourceIndex;
        return result;
    }

    internal void ResetHuffmanDecoder()
    {
        CurrentCode = 0;
        CurrentCodeLength = 0;
        Failed = false;
    }

    int FindSymbol(int code, int length)
    {
        int symbol = 0;
        while (symbol < Table.CodeLengths.Length)
        {
            if (Table.CodeLengths[symbol] == length &&
                Table.Codes[symbol] == code)
            {
                return symbol;
            }

            symbol = symbol + 1;
        }

        return -1;
    }
}
