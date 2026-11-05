using System;

namespace websocket;

internal class DeflateBitWriteResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.Complete;
    internal int ProducedCount;
    internal bool BitsAccepted;
}

internal class DeflateBitWriter
{
    uint Buffer;
    int BufferedBitCount;

    internal DeflateBitWriter()
    {
    }

    internal DeflateBitWriteResult WriteBits(
        int value,
        int bitCount,
        Span<byte> destination)
    {
        DeflateBitWriteResult result = new DeflateBitWriteResult();
        if (bitCount < 0 ||
            bitCount > 24 ||
            value < 0 ||
            (bitCount < 24 && value >= (1 << bitCount)) ||
            (bitCount == 24 && value >= 16777216))
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
            return result;
        }

        result.ProducedCount = FlushBytes(destination);
        if (BufferedBitCount >= 8)
        {
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
            return result;
        }

        Buffer = Buffer | ((uint)value << BufferedBitCount);
        BufferedBitCount = BufferedBitCount + bitCount;
        result.BitsAccepted = true;
        result.ProducedCount = result.ProducedCount + FlushBytes(
            destination.Slice(result.ProducedCount));
        if (BufferedBitCount >= 8)
        {
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
        }

        return result;
    }

    internal DeflateBitWriteResult WriteHuffmanCode(
        int canonicalCode,
        int bitCount,
        Span<byte> destination)
    {
        if (bitCount < 0 ||
            bitCount > 24 ||
            canonicalCode < 0 ||
            (bitCount < 24 && canonicalCode >= (1 << bitCount)) ||
            (bitCount == 24 && canonicalCode >= 16777216))
        {
            DeflateBitWriteResult invalid = new DeflateBitWriteResult();
            invalid.Status = WebSocketParseStatus.ProtocolFailure;
            return invalid;
        }

        int reversed = 0;
        int source = canonicalCode;
        int index = 0;
        while (index < bitCount)
        {
            reversed = (reversed << 1) | (source & 1);
            source = source >> 1;
            index = index + 1;
        }

        return WriteBits(reversed, bitCount, destination);
    }

    internal DeflateBitWriteResult DrainWriterBytes(Span<byte> destination)
    {
        DeflateBitWriteResult result = new DeflateBitWriteResult();
        result.BitsAccepted = true;
        result.ProducedCount = FlushBytes(destination);
        if (BufferedBitCount >= 8)
        {
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
        }

        return result;
    }

    internal DeflateBitWriteResult FinishWriterByte(Span<byte> destination)
    {
        DeflateBitWriteResult result = new DeflateBitWriteResult();
        result.BitsAccepted = true;
        result.ProducedCount = FlushBytes(destination);
        if (BufferedBitCount >= 8)
        {
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
            return result;
        }

        if (BufferedBitCount > 0 && BufferedBitCount < 8)
        {
            BufferedBitCount = 8;
        }

        result.ProducedCount = result.ProducedCount + FlushBytes(
            destination.Slice(result.ProducedCount));
        if (BufferedBitCount >= 8)
        {
            result.Status = WebSocketParseStatus.NeedOutputCapacity;
        }

        return result;
    }

    internal int GetPendingBitCount()
    {
        return BufferedBitCount;
    }

    internal void ResetBitWriter()
    {
        Buffer = 0;
        BufferedBitCount = 0;
    }

    int FlushBytes(Span<byte> destination)
    {
        int destinationIndex = 0;
        while (BufferedBitCount >= 8 && destinationIndex < destination.Length)
        {
            destination[destinationIndex] = (byte)(Buffer & 255);
            Buffer = Buffer >> 8;
            BufferedBitCount = BufferedBitCount - 8;
            destinationIndex = destinationIndex + 1;
        }

        return destinationIndex;
    }
}
