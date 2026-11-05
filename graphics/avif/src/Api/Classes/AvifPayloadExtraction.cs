using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Owns one bounded payload extraction and exact output-to-source byte mappings.</summary>
public class AvifPayloadExtraction : IAvifPayloadExtraction
{
    AvifResultCode code;
    byte[] data;
    List<AvifPayloadSegment> segments;
    uint itemId;
    uint trackId;
    int firstSampleIndex;
    int lastSampleIndex;
    int obuType;
    ulong metadataType;
    bool metadataTypeKnown;

    internal AvifPayloadExtraction(
        AvifResultCode code,
        byte[] data,
        List<AvifPayloadSegment> segments,
        uint itemId,
        uint trackId,
        int firstSampleIndex,
        int lastSampleIndex,
        int obuType,
        ulong metadataType,
        bool metadataTypeKnown)
    {
        this.code = code;
        this.data = (byte[])data.Clone();
        this.segments = new List<AvifPayloadSegment>(segments);
        this.itemId = itemId;
        this.trackId = trackId;
        this.firstSampleIndex = firstSampleIndex;
        this.lastSampleIndex = lastSampleIndex;
        this.obuType = obuType;
        this.metadataType = metadataType;
        this.metadataTypeKnown = metadataTypeKnown;
    }

    /// <summary>Returns the extraction outcome.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns an independent copy of the extracted bytes.</summary>
    /// <returns>The payload, or an empty array after failure.</returns>
    public byte[] GetData()
    {
        return code == AvifResultCode.Success
            ? (byte[])data.Clone() : System.Array.Empty<byte>();
    }

    /// <summary>Returns the payload length without copying it.</summary>
    /// <returns>The byte count.</returns>
    public int GetDataLength()
    {
        return code == AvifResultCode.Success ? data.Length : 0;
    }

    /// <summary>Copies the payload into a caller-provided buffer.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>Success, BufferTooSmall, or the extraction failure code.</returns>
    public AvifResultCode CopyDataTo(Span<byte> destination)
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        if (destination.Length < data.Length)
        {
            return AvifResultCode.BufferTooSmall;
        }
        data.AsSpan().CopyTo(destination);
        return AvifResultCode.Success;
    }

    /// <summary>Returns the number of exact source segments represented by the payload.</summary>
    /// <returns>The source-segment count.</returns>
    public int GetSourceSegmentCount()
    {
        return code == AvifResultCode.Success ? segments.Count : 0;
    }

    /// <summary>Returns one output-to-source byte-range mapping.</summary>
    /// <param name="index">The zero-based segment index.</param>
    /// <returns>The segment, or null for an invalid index.</returns>
    public AvifPayloadSegment? GetSourceSegment(int index)
    {
        if (code != AvifResultCode.Success
            || index < 0 || index >= segments.Count)
        {
            return null;
        }
        AvifPayloadSegment segment = segments[index];
        return new AvifPayloadSegment(
            segment.GetPayloadOffset(),
            segment.GetSourceOffset(),
            segment.GetLength());
    }

    /// <summary>Returns the selected item identifier.</summary>
    /// <returns>The item identifier, or zero when not item-based.</returns>
    public uint GetItemId()
    {
        return itemId;
    }

    /// <summary>Returns the selected track identifier.</summary>
    /// <returns>The track identifier, or zero when not track-based.</returns>
    public uint GetTrackId()
    {
        return trackId;
    }

    /// <summary>Returns the first included sample index.</summary>
    /// <returns>The index, or minus one when not sample-based.</returns>
    public int GetFirstSampleIndex()
    {
        return firstSampleIndex;
    }

    /// <summary>Returns the last included sample index.</summary>
    /// <returns>The index, or minus one when not sample-based.</returns>
    public int GetLastSampleIndex()
    {
        return lastSampleIndex;
    }

    /// <summary>Returns the numeric AV1 OBU type.</summary>
    /// <returns>The OBU type, or zero when the extraction is not an OBU.</returns>
    public int GetObuType()
    {
        return obuType;
    }

    /// <summary>Returns the AV1 metadata type.</summary>
    /// <returns>The unsigned metadata type, or zero when unavailable.</returns>
    public ulong GetMetadataType()
    {
        return metadataType;
    }

    /// <summary>Reports whether the AV1 metadata type has defined parsed semantics.</summary>
    /// <returns><c>true</c> for a recognized metadata type.</returns>
    public bool IsMetadataTypeKnown()
    {
        return metadataTypeKnown;
    }
}
