using System;

namespace websocket;

internal class DeflateBlockHeaderResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.NeedInput;
    internal int ConsumedCount;
    internal bool FinalBlock;
    internal int BlockType;
}

internal class DeflateBlockHeaderReader
{
    internal DeflateBitReader Reader;

    internal DeflateBlockHeaderReader(DeflateBitReader reader)
    {
        if (reader == null)
        {
            throw new WebSocketStateException();
        }

        Reader = reader;
    }

    internal DeflateBlockHeaderResult ReadBlockHeader(
        ReadOnlySpan<byte> source)
    {
        DeflateBlockHeaderResult result = new DeflateBlockHeaderResult();
        DeflateBitReadResult bits = Reader.ReadBits(source, 3);
        result.Status = bits.Status;
        result.ConsumedCount = bits.ConsumedCount;
        if (bits.Status != WebSocketParseStatus.Complete)
        {
            return result;
        }

        result.FinalBlock = (bits.Value & 1) != 0;
        result.BlockType = (bits.Value >> 1) & 3;
        if (result.BlockType == 3)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
        }

        return result;
    }
}

internal class DeflateStoredBlockHeaderResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.NeedInput;
    internal int ConsumedCount;
    internal int Length;
}

internal class DeflateStoredBlockHeaderReader
{
    internal DeflateBitReader Reader;
    bool Aligned;
    bool HasLength;
    int Length;
    bool Failed;

    internal DeflateStoredBlockHeaderReader(DeflateBitReader reader)
    {
        if (reader == null)
        {
            throw new WebSocketStateException();
        }

        Reader = reader;
    }

    internal DeflateStoredBlockHeaderResult ReadStoredBlockHeader(
        ReadOnlySpan<byte> source)
    {
        DeflateStoredBlockHeaderResult result =
            new DeflateStoredBlockHeaderResult();
        if (Failed)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
            return result;
        }

        if (!Aligned)
        {
            Reader.AlignReaderToByte();
            Aligned = true;
        }

        int sourceIndex = 0;
        if (!HasLength)
        {
            DeflateBitReadResult lengthResult = Reader.ReadBits(source, 16);
            sourceIndex = sourceIndex + lengthResult.ConsumedCount;
            if (lengthResult.Status != WebSocketParseStatus.Complete)
            {
                result.Status = lengthResult.Status;
                result.ConsumedCount = sourceIndex;
                return result;
            }

            Length = lengthResult.Value;
            HasLength = true;
        }

        DeflateBitReadResult complementResult = Reader.ReadBits(
            source.Slice(sourceIndex),
            16);
        sourceIndex = sourceIndex + complementResult.ConsumedCount;
        result.Status = complementResult.Status;
        result.ConsumedCount = sourceIndex;
        if (complementResult.Status != WebSocketParseStatus.Complete)
        {
            return result;
        }

        if (((Length ^ complementResult.Value) & 65535) != 65535)
        {
            Failed = true;
            result.Status = WebSocketParseStatus.ProtocolFailure;
            return result;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.Length = Length;
        return result;
    }

    internal void ResetStoredBlockHeader()
    {
        Aligned = false;
        HasLength = false;
        Length = 0;
        Failed = false;
    }
}

internal class DeflateStoredBlockWriteResult
{
    internal WebSocketParseStatus Status =
        WebSocketParseStatus.NeedOutputCapacity;
    internal int ProducedCount;
}

internal static class DeflateStoredBlockHeaderWriter
{
    internal static DeflateStoredBlockWriteResult WriteStoredBlockHeader(
        int length,
        Span<byte> destination)
    {
        DeflateStoredBlockWriteResult result =
            new DeflateStoredBlockWriteResult();
        if (length < 0 || length > 65535)
        {
            result.Status = WebSocketParseStatus.ProtocolFailure;
            return result;
        }

        if (destination.Length < 4)
        {
            return result;
        }

        int complement = length ^ 65535;
        destination[0] = (byte)(length & 255);
        destination[1] = (byte)((length >> 8) & 255);
        destination[2] = (byte)(complement & 255);
        destination[3] = (byte)((complement >> 8) & 255);
        result.Status = WebSocketParseStatus.Complete;
        result.ProducedCount = 4;
        return result;
    }
}
