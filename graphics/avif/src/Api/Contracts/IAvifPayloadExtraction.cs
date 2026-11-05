using System;

namespace avif;

/// <summary>Defines an independently owned bounded payload extraction and its source mapping.</summary>
public interface IAvifPayloadExtraction
{
    /// <summary>Returns the extraction outcome.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns an independent copy of the extracted bytes.</summary>
    /// <returns>The payload, or an empty array after failure.</returns>
    byte[] GetData();

    /// <summary>Returns the payload length without copying it.</summary>
    /// <returns>The byte count.</returns>
    int GetDataLength();

    /// <summary>Copies the payload into a caller-provided buffer.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>Success, BufferTooSmall, or the extraction failure code.</returns>
    AvifResultCode CopyDataTo(Span<byte> destination);

    /// <summary>Returns the number of exact source segments represented by the payload.</summary>
    /// <returns>The source-segment count.</returns>
    int GetSourceSegmentCount();

    /// <summary>Returns one output-to-source byte-range mapping.</summary>
    /// <param name="index">The zero-based segment index.</param>
    /// <returns>The segment, or null for an invalid index.</returns>
    AvifPayloadSegment? GetSourceSegment(int index);

    /// <summary>Returns the selected item identifier.</summary>
    /// <returns>The item identifier, or zero when not item-based.</returns>
    uint GetItemId();

    /// <summary>Returns the selected track identifier.</summary>
    /// <returns>The track identifier, or zero when not track-based.</returns>
    uint GetTrackId();

    /// <summary>Returns the first included sample index.</summary>
    /// <returns>The index, or minus one when not sample-based.</returns>
    int GetFirstSampleIndex();

    /// <summary>Returns the last included sample index.</summary>
    /// <returns>The index, or minus one when not sample-based.</returns>
    int GetLastSampleIndex();

    /// <summary>Returns the numeric AV1 OBU type.</summary>
    /// <returns>The OBU type, or zero when the extraction is not an OBU.</returns>
    int GetObuType();

    /// <summary>Returns the AV1 metadata type.</summary>
    /// <returns>The unsigned metadata type, or zero when unavailable.</returns>
    ulong GetMetadataType();

    /// <summary>Reports whether the AV1 metadata type has defined parsed semantics.</summary>
    /// <returns><c>true</c> for a recognized metadata type.</returns>
    bool IsMetadataTypeKnown();
}
