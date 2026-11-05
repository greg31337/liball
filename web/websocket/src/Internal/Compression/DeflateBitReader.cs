using System;

namespace websocket;

internal class DeflateBitReadResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.NeedInput;
    internal int ConsumedCount;
    internal int Value;
}

internal class DeflateBitReader
{
    uint Buffer;
    int BufferedBitCount;

    internal DeflateBitReader()
    {
    }

    internal DeflateBitReadResult ReadBits(
        ReadOnlySpan<byte> source,
        int bitCount)
    {
        DeflateBitReadResult result = new DeflateBitReadResult();
        if (bitCount < 0 || bitCount > 24)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
            return result;
        }

        int sourceIndex = 0;
        while (BufferedBitCount < bitCount && sourceIndex < source.Length)
        {
            Buffer = Buffer | ((uint)source[sourceIndex] << BufferedBitCount);
            BufferedBitCount = BufferedBitCount + 8;
            sourceIndex = sourceIndex + 1;
        }

        result.ConsumedCount = sourceIndex;
        if (BufferedBitCount < bitCount)
        {
            return result;
        }

        uint mask = 0;
        if (bitCount != 0)
        {
            mask = ((uint)1 << bitCount) - 1;
        }

        result.Value = (int)(Buffer & mask);
        Buffer = Buffer >> bitCount;
        BufferedBitCount = BufferedBitCount - bitCount;
        result.Status = WebSocketParseStatus.Complete;
        return result;
    }

    internal void AlignReaderToByte()
    {
        int discardedBitCount = BufferedBitCount % 8;
        Buffer = Buffer >> discardedBitCount;
        BufferedBitCount = BufferedBitCount - discardedBitCount;
    }

    internal int GetBufferedBitCount()
    {
        return BufferedBitCount;
    }

    internal void ResetBitReader()
    {
        Buffer = 0;
        BufferedBitCount = 0;
    }
}
