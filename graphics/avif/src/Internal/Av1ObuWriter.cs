using System.Collections.Generic;

namespace avif;

internal class Av1WriteResult
{
    internal AvifResultCode Code;
    internal byte[] Data;

    internal Av1WriteResult(AvifResultCode code, byte[] data)
    {
        Code = code;
        Data = data == null ? System.Array.Empty<byte>() : (byte[])data.Clone();
    }
}

// AV1 specification sections 5.2 through 5.3 and Annex B.
internal static class Av1ObuWriter
{
    internal static Av1WriteResult WriteObu(
        Av1ObuType type,
        byte[] payload,
        bool hasExtension,
        int temporalId,
        int spatialId)
    {
        return WriteObu(type, payload, hasExtension, temporalId, spatialId, true);
    }

    internal static Av1WriteResult WriteObu(
        Av1ObuType type,
        byte[] payload,
        bool hasExtension,
        int temporalId,
        int spatialId,
        bool hasSizeField)
    {
        if (payload == null || !IsValidType(type)
            || temporalId < 0 || temporalId >= Av1SyntaxConstants.MaxTemporalLayers
            || spatialId < 0 || spatialId >= Av1SyntaxConstants.MaxSpatialLayers
            || (!hasExtension && (temporalId != 0 || spatialId != 0))
            || (hasExtension && (type == Av1ObuType.SequenceHeader
                || type == Av1ObuType.TemporalDelimiter
                || type == Av1ObuType.TileList)))
        {
            return Fail(AvifResultCode.InvalidArgument);
        }

        byte[] size = hasSizeField
            ? WriteLeb128((ulong)payload.Length)
            : System.Array.Empty<byte>();
        int headerSize = hasExtension ? 2 : 1;
        byte[] data = new byte[headerSize + size.Length + payload.Length];
        data[0] = (byte)((int)type << 3
            | (hasExtension ? 4 : 0)
            | (hasSizeField ? 2 : 0));
        if (hasExtension)
        {
            data[1] = (byte)(temporalId << 5 | spatialId << 3);
        }

        size.CopyTo(data, headerSize);
        payload.CopyTo(data, headerSize + size.Length);
        return new Av1WriteResult(AvifResultCode.Success, data);
    }

    internal static Av1WriteResult WriteTemporalDelimiter()
    {
        return WriteObu(
            Av1ObuType.TemporalDelimiter,
            new byte[] { 128 },
            false,
            0,
            0,
            true);
    }

    internal static Av1WriteResult WriteLengthDelimitedFrame(byte[][] obus)
    {
        if (obus == null || obus.Length == 0)
        {
            return Fail(AvifResultCode.InvalidArgument);
        }

        long payloadLength = 0;
        int index = 0;
        while (index < obus.Length)
        {
            if (obus[index] == null || obus[index].Length == 0)
            {
                return Fail(AvifResultCode.InvalidArgument);
            }

            payloadLength += WriteLeb128((ulong)obus[index].Length).Length;
            payloadLength += obus[index].Length;
            if (payloadLength > int.MaxValue)
            {
                return Fail(AvifResultCode.LimitExceeded);
            }

            index++;
        }

        byte[] data = new byte[(int)payloadLength];
        int cursor = 0;
        index = 0;
        while (index < obus.Length)
        {
            byte[] length = WriteLeb128((ulong)obus[index].Length);
            length.CopyTo(data, cursor);
            cursor += length.Length;
            obus[index].CopyTo(data, cursor);
            cursor += obus[index].Length;
            index++;
        }

        return new Av1WriteResult(AvifResultCode.Success, data);
    }

    internal static Av1WriteResult WriteLengthDelimitedTemporalUnit(
        byte[][] frameUnits)
    {
        if (frameUnits == null || frameUnits.Length == 0)
        {
            return Fail(AvifResultCode.InvalidArgument);
        }

        long payloadLength = 0;
        int index = 0;
        while (index < frameUnits.Length)
        {
            if (frameUnits[index] == null || frameUnits[index].Length == 0)
            {
                return Fail(AvifResultCode.InvalidArgument);
            }

            payloadLength += WriteLeb128((ulong)frameUnits[index].Length).Length;
            payloadLength += frameUnits[index].Length;
            if (payloadLength > int.MaxValue)
            {
                return Fail(AvifResultCode.LimitExceeded);
            }

            index++;
        }

        byte[] temporalLength = WriteLeb128((ulong)payloadLength);
        byte[] data = new byte[temporalLength.Length + (int)payloadLength];
        temporalLength.CopyTo(data, 0);
        int cursor = temporalLength.Length;
        index = 0;
        while (index < frameUnits.Length)
        {
            byte[] length = WriteLeb128((ulong)frameUnits[index].Length);
            length.CopyTo(data, cursor);
            cursor += length.Length;
            frameUnits[index].CopyTo(data, cursor);
            cursor += frameUnits[index].Length;
            index++;
        }

        return new Av1WriteResult(AvifResultCode.Success, data);
    }

    internal static byte[] WriteLeb128(ulong value)
    {
        List<byte> bytes = new List<byte>();
        do
        {
            byte next = (byte)(value & 127);
            value >>= 7;
            if (value != 0)
            {
                next = (byte)(next | 128);
            }

            bytes.Add(next);
        }
        while (value != 0);

        return bytes.ToArray();
    }

    static bool IsValidType(Av1ObuType type)
    {
        int value = (int)type;
        return value >= (int)Av1ObuType.SequenceHeader
            && value <= (int)Av1ObuType.TileList
            || type == Av1ObuType.Padding;
    }

    static Av1WriteResult Fail(AvifResultCode code)
    {
        return new Av1WriteResult(code, System.Array.Empty<byte>());
    }
}
