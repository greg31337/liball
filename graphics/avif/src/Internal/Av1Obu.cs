using System;
using System.Collections.Generic;

namespace avif;

// AV1 specification sections 5.2 through 5.3 and Annex B.
internal class Av1ObuHeader
{
    internal Av1ObuType Type;
    internal bool HasExtension;
    internal bool HasSizeField;
    internal int TemporalId;
    internal int SpatialId;
    internal int HeaderSize;

    internal Av1ObuHeader(
        Av1ObuType type,
        bool hasExtension,
        bool hasSizeField,
        int temporalId,
        int spatialId,
        int headerSize)
    {
        Type = type;
        HasExtension = hasExtension;
        HasSizeField = hasSizeField;
        TemporalId = temporalId;
        SpatialId = spatialId;
        HeaderSize = headerSize;
    }
}

internal class Av1Obu
{
    internal Av1ObuHeader Header;
    internal int Offset;
    internal int SizeFieldSize;
    internal int PayloadOffset;
    internal int PayloadSize;
    internal int TotalSize;
    internal bool InOperatingPoint;
    byte[] payload;

    internal Av1Obu(
        Av1ObuHeader header,
        int offset,
        int sizeFieldSize,
        int payloadOffset,
        int payloadSize,
        int totalSize,
        bool inOperatingPoint,
        ReadOnlySpan<byte> payload)
    {
        Header = header;
        Offset = offset;
        SizeFieldSize = sizeFieldSize;
        PayloadOffset = payloadOffset;
        PayloadSize = payloadSize;
        TotalSize = totalSize;
        InOperatingPoint = inOperatingPoint;
        this.payload = payload.ToArray();
    }

    internal byte[] GetPayload()
    {
        return (byte[])payload.Clone();
    }

    // payloadBitCount is the number of semantic payload bits consumed before
    // trailing_bits. The remaining bits must be one followed only by zeroes.
    internal AvifResultCode ValidateTrailingBits(long payloadBitCount)
    {
        long totalBits = (long)payload.Length * 8;
        if (payloadBitCount < 0 || payloadBitCount > totalBits)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (totalBits == 0)
        {
            return payloadBitCount == 0
                ? AvifResultCode.Success
                : AvifResultCode.InvalidArgument;
        }

        if (payloadBitCount == totalBits)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        if (ReadPayloadBit(payloadBitCount) != 1)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        long bitIndex = payloadBitCount + 1;
        while (bitIndex < totalBits)
        {
            if (ReadPayloadBit(bitIndex) != 0)
            {
                return AvifResultCode.InvalidAv1Bitstream;
            }

            bitIndex++;
        }

        return AvifResultCode.Success;
    }

    int ReadPayloadBit(long bitIndex)
    {
        int byteIndex = (int)(bitIndex / 8);
        int shift = 7 - (int)(bitIndex % 8);
        return payload[byteIndex] >> shift & 1;
    }
}

internal class Av1FrameUnit
{
    internal int Offset;
    internal int Size;
    internal List<Av1Obu> Obus;

    internal Av1FrameUnit(int offset, int size)
    {
        Offset = offset;
        Size = size;
        Obus = new List<Av1Obu>();
    }
}

internal class Av1TemporalUnit
{
    internal int Offset;
    internal int Size;
    internal List<Av1FrameUnit> FrameUnits;
    internal List<Av1Obu> Obus;

    internal Av1TemporalUnit(int offset, int size)
    {
        Offset = offset;
        Size = size;
        FrameUnits = new List<Av1FrameUnit>();
        Obus = new List<Av1Obu>();
    }
}

internal class Av1ObuParseResult
{
    internal AvifResultCode Code;
    internal List<Av1TemporalUnit> TemporalUnits;
    internal List<Av1Obu> Obus;
    internal int DroppedObuCount;
    internal ResourceGuard Resources;
    internal DiagnosticLog Diagnostics;

    internal Av1ObuParseResult(
        AvifResultCode code,
        List<Av1TemporalUnit> temporalUnits,
        List<Av1Obu> obus,
        int droppedObuCount,
        ResourceGuard resources,
        DiagnosticLog diagnostics)
    {
        Code = code;
        TemporalUnits = temporalUnits;
        Obus = obus;
        DroppedObuCount = droppedObuCount;
        Resources = resources;
        Diagnostics = diagnostics;
    }

    internal List<Av1Obu> GetIncludedObus()
    {
        List<Av1Obu> included = new List<Av1Obu>();
        int index = 0;
        while (index < Obus.Count)
        {
            if (Obus[index].InOperatingPoint)
            {
                included.Add(Obus[index]);
            }

            index++;
        }

        return included;
    }

    internal List<Av1Obu> GetIncludedObusForLayer(
        Av1LayerSelection selection)
    {
        List<Av1Obu> included = new List<Av1Obu>();
        int index = 0;
        while (index < Obus.Count)
        {
            Av1Obu obu = Obus[index];
            if (obu.InOperatingPoint
                && Av1LayerSelector.ShouldDecodeObu(selection, obu.Header))
            {
                included.Add(obu);
            }
            index++;
        }
        return included;
    }
}
