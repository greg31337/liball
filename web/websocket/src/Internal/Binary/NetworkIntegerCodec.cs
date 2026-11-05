using System;

namespace websocket;

internal class NetworkIntegerReadResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.NeedInput;
    internal int ConsumedCount;
    internal long Value;
}

internal class NetworkIntegerWriteResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.NeedOutputCapacity;
    internal int ProducedCount;
}

internal static class NetworkIntegerCodec
{
    internal static NetworkIntegerReadResult ReadUnsigned16(ReadOnlySpan<byte> source)
    {
        NetworkIntegerReadResult result = new NetworkIntegerReadResult();
        if (source.Length < 2)
        {
            return result;
        }

        int value = ((int)source[0] << 8) | (int)source[1];
        result.Status = WebSocketParseStatus.Complete;
        result.ConsumedCount = 2;
        result.Value = value;
        return result;
    }

    internal static NetworkIntegerReadResult ReadUnsigned63(ReadOnlySpan<byte> source)
    {
        NetworkIntegerReadResult result = new NetworkIntegerReadResult();
        if (source.Length < 8)
        {
            return result;
        }

        if ((source[0] & 128) != 0)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
            result.ConsumedCount = 8;
            return result;
        }

        long value = 0;
        int sourceIndex = 0;
        while (sourceIndex < 8)
        {
            value = (value << 8) | (long)source[sourceIndex];
            sourceIndex = sourceIndex + 1;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.ConsumedCount = 8;
        result.Value = value;
        return result;
    }

    internal static NetworkIntegerReadResult ReadCanonicalPayloadLength16(ReadOnlySpan<byte> source)
    {
        NetworkIntegerReadResult result = ReadUnsigned16(source);
        if (result.Status == WebSocketParseStatus.Complete &&
            result.Value < WebSocketProtocolConstants.Extended16PayloadLengthMarker)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
        }

        return result;
    }

    internal static NetworkIntegerReadResult ReadCanonicalPayloadLength63(ReadOnlySpan<byte> source)
    {
        NetworkIntegerReadResult result = ReadUnsigned63(source);
        if (result.Status == WebSocketParseStatus.Complete &&
            result.Value <= WebSocketProtocolConstants.Extended16PayloadLengthMaximum)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
        }

        return result;
    }

    internal static NetworkIntegerWriteResult WriteUnsigned16(int value, Span<byte> destination)
    {
        NetworkIntegerWriteResult result = new NetworkIntegerWriteResult();
        if (value < 0 || value > WebSocketProtocolConstants.Extended16PayloadLengthMaximum)
        {
            result.Status = WebSocketParseStatus.LimitRejected;
            return result;
        }

        if (destination.Length < 2)
        {
            return result;
        }

        destination[0] = (byte)((value >> 8) & 255);
        destination[1] = (byte)(value & 255);
        result.Status = WebSocketParseStatus.Complete;
        result.ProducedCount = 2;
        return result;
    }

    internal static NetworkIntegerWriteResult WriteUnsigned63(long value, Span<byte> destination)
    {
        NetworkIntegerWriteResult result = new NetworkIntegerWriteResult();
        if (value < 0)
        {
            result.Status = WebSocketParseStatus.LimitRejected;
            return result;
        }

        if (destination.Length < 8)
        {
            return result;
        }

        int destinationIndex = 7;
        long remainingValue = value;
        while (destinationIndex >= 0)
        {
            destination[destinationIndex] = (byte)(remainingValue & 255);
            remainingValue = remainingValue >> 8;
            destinationIndex = destinationIndex - 1;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.ProducedCount = 8;
        return result;
    }

    internal static NetworkIntegerWriteResult WriteCanonicalPayloadLength16(int value, Span<byte> destination)
    {
        if (value < WebSocketProtocolConstants.Extended16PayloadLengthMarker)
        {
            NetworkIntegerWriteResult rejectedResult = new NetworkIntegerWriteResult();
            rejectedResult.Status = WebSocketParseStatus.LimitRejected;
            return rejectedResult;
        }

        return WriteUnsigned16(value, destination);
    }

    internal static NetworkIntegerWriteResult WriteCanonicalPayloadLength63(long value, Span<byte> destination)
    {
        if (value <= WebSocketProtocolConstants.Extended16PayloadLengthMaximum)
        {
            NetworkIntegerWriteResult rejectedResult = new NetworkIntegerWriteResult();
            rejectedResult.Status = WebSocketParseStatus.LimitRejected;
            return rejectedResult;
        }

        return WriteUnsigned63(value, destination);
    }
}
