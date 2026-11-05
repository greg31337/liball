using System;
using System.Collections.Generic;
using System.IO;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifReader"/>.</summary>
public interface IAvifReader
{
    /// <summary>Returns the outcome of the most recent reader operation.</summary>
    /// <returns>The stable reader result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Parses and validates the retained complete source.</summary>
    /// <returns>The parse result; repeated successful calls are harmless.</returns>
    AvifResultCode Parse();

    /// <summary>Returns the parsed document after <c>Parse</c>.</summary>
    /// <returns>The document, or null before parsing or after closing.</returns>
    AvifDocument? GetDocument();

    /// <summary>Returns exact ranges unavailable during the most recent range-source operation.</summary>
    /// <returns>An independent range array, empty when no range is pending.</returns>
    AvifByteRange[] GetMissingRanges();

    /// <summary>Decodes the source selected by the configured item-or-sequence policy.</summary>
    /// <returns>A caller-owned result with associated outputs and the parsed document.</returns>
    AvifDecodeResult DecodeSelectedImage();

    /// <summary>Decodes the declared primary image item.</summary>
    /// <returns>A caller-owned image result.</returns>
    AvifDecodeResult DecodePrimaryImage();

    /// <summary>Decodes one image or supported derived-image item.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <returns>A caller-owned image result.</returns>
    AvifDecodeResult DecodeItem(uint itemId);

    /// <summary>Decodes one indexed auxiliary associated with an image item.</summary>
    /// <param name="masterItemId">The master image item identifier.</param>
    /// <param name="auxiliaryIndex">The zero-based auxiliary association index.</param>
    /// <returns>The selected caller-owned auxiliary image result.</returns>
    AvifDecodeResult DecodeAuxiliaryItem(
        uint masterItemId,
        int auxiliaryIndex);

    /// <summary>Decodes the first alpha auxiliary associated with an image item.</summary>
    /// <param name="masterItemId">The master image item identifier.</param>
    /// <returns>The selected caller-owned alpha result.</returns>
    AvifDecodeResult DecodeAlpha(uint masterItemId);

    /// <summary>Decodes the first depth auxiliary associated with an image item.</summary>
    /// <param name="masterItemId">The master image item identifier.</param>
    /// <returns>The selected caller-owned depth result.</returns>
    AvifDecodeResult DecodeDepth(uint masterItemId);

    /// <summary>Decodes the gain image and metadata referenced by a tone-map item.</summary>
    /// <param name="toneMapItemId">The tone-map derived item identifier.</param>
    /// <returns>A caller-owned result containing the gain-map association.</returns>
    AvifDecodeResult DecodeGainMap(uint toneMapItemId);

    /// <summary>Decodes a tone-map derived item and its base and gain-map alternatives.</summary>
    /// <param name="toneMapItemId">The tone-map derived item identifier.</param>
    /// <returns>A caller-owned result containing the tone-map association.</returns>
    AvifDecodeResult DecodeToneMap(uint toneMapItemId);

    /// <summary>Decodes one indexed frame from the selected timed image sequence.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>A caller-owned image result.</returns>
    AvifDecodeResult DecodeFrame(int frameIndex);

    /// <summary>Decodes one auxiliary track sample associated with a sequence frame.</summary>
    /// <param name="frameIndex">The zero-based sequence frame index.</param>
    /// <param name="auxiliaryIndex">The zero-based auxiliary-track index.</param>
    /// <returns>The selected caller-owned auxiliary frame result.</returns>
    AvifDecodeResult DecodeSequenceAuxiliary(
        int frameIndex,
        int auxiliaryIndex);

    /// <summary>Decodes one progressive layer of the primary coded item.</summary>
    /// <param name="layerIndex">The zero-based progressive layer index.</param>
    /// <returns>A caller-owned image result.</returns>
    AvifDecodeResult DecodeLayer(int layerIndex);

    /// <summary>Decodes one progressive layer from a selected coded item.</summary>
    /// <param name="itemId">The layered coded item identifier.</param>
    /// <param name="layerIndex">The zero-based progressive layer index.</param>
    /// <returns>A caller-owned layer image result.</returns>
    AvifDecodeResult DecodeItemLayer(uint itemId, int layerIndex);

    /// <summary>Decodes one row and column of a grid derived-image item.</summary>
    /// <param name="itemId">The grid item identifier.</param>
    /// <param name="row">The zero-based grid row.</param>
    /// <param name="column">The zero-based grid column.</param>
    /// <returns>A caller-owned cell image result.</returns>
    AvifDecodeResult DecodeTile(uint itemId, int row, int column);

    /// <summary>Returns timing metadata for one selected sequence frame.</summary>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>The frame timing, or null when unavailable.</returns>
    AvifFrameTiming? GetFrameTiming(int frameIndex);

    /// <summary>Returns one metadata item associated with an image selection.</summary>
    /// <param name="itemId">The described image item identifier.</param>
    /// <param name="metadataIndex">The zero-based associated metadata index.</param>
    /// <returns>An independently owned metadata payload, or null when unavailable.</returns>
    AvifMetadata? GetAssociatedMetadataItem(
        uint itemId,
        int metadataIndex);

    /// <summary>Extracts one item's encoded or derived-image payload.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <returns>An independent byte array, or an empty array on failure.</returns>
    byte[] GetEncodedItem(uint itemId);

    /// <summary>Extracts a complete resolved item payload under a caller byte bound.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned extraction with exact source-segment mappings.</returns>
    AvifPayloadExtraction ExtractItemPayload(
        uint itemId,
        long maximumBytes);

    /// <summary>Extracts one resolved source segment of an item payload.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <param name="segmentIndex">The zero-based resolved segment index.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned extraction retaining the exact source range.</returns>
    AvifPayloadExtraction ExtractItemPayloadSegment(
        uint itemId,
        int segmentIndex,
        long maximumBytes);

    /// <summary>Extracts one complete encoded track sample under a caller byte bound.</summary>
    /// <param name="trackId">The nonzero track identifier.</param>
    /// <param name="sampleIndex">The zero-based sample index.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned extraction retaining the exact sample source range.</returns>
    AvifPayloadExtraction ExtractTrackSamplePayload(
        uint trackId,
        int sampleIndex,
        long maximumBytes);

    /// <summary>Extracts the samples required from the nearest synchronization point through a selected sample.</summary>
    /// <param name="trackId">The nonzero track identifier.</param>
    /// <param name="sampleIndex">The zero-based final sample index.</param>
    /// <param name="maximumBytes">The maximum permitted combined byte count.</param>
    /// <returns>An owned extraction with one exact source segment per included sample.</returns>
    AvifPayloadExtraction ExtractSampleDependencyRange(
        uint trackId,
        int sampleIndex,
        long maximumBytes);

    /// <summary>Extracts one configuration OBU associated with an image item.</summary>
    /// <param name="itemId">The nonzero coded-image item identifier.</param>
    /// <param name="obuIndex">The zero-based configuration OBU index.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned complete OBU extraction with its exact property source range.</returns>
    AvifPayloadExtraction ExtractItemConfigurationObu(
        uint itemId,
        int obuIndex,
        long maximumBytes);

    /// <summary>Extracts one configuration OBU associated with a track sample description.</summary>
    /// <param name="trackId">The nonzero track identifier.</param>
    /// <param name="descriptionIndex">The one-based sample-description index.</param>
    /// <param name="obuIndex">The zero-based configuration OBU index.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned complete OBU extraction with its exact property source range.</returns>
    AvifPayloadExtraction ExtractTrackConfigurationObu(
        uint trackId,
        int descriptionIndex,
        int obuIndex,
        long maximumBytes);

    /// <summary>Extracts one metadata OBU from an encoded item payload.</summary>
    /// <param name="itemId">The nonzero coded-image item identifier.</param>
    /// <param name="metadataIndex">The zero-based metadata OBU index.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned complete OBU extraction with metadata type information.</returns>
    AvifPayloadExtraction ExtractItemMetadataObu(
        uint itemId,
        int metadataIndex,
        long maximumBytes);

    /// <summary>Extracts one metadata OBU from an encoded track sample.</summary>
    /// <param name="trackId">The nonzero track identifier.</param>
    /// <param name="sampleIndex">The zero-based sample index.</param>
    /// <param name="metadataIndex">The zero-based metadata OBU index.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned complete OBU extraction with metadata type information.</returns>
    AvifPayloadExtraction ExtractTrackMetadataObu(
        uint trackId,
        int sampleIndex,
        int metadataIndex,
        long maximumBytes);

    /// <summary>Extracts one independently decodable progressive-layer payload segment.</summary>
    /// <param name="itemId">The nonzero layered item identifier.</param>
    /// <param name="layerIndex">The zero-based layer index.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned extraction with exact source-segment mappings.</returns>
    AvifPayloadExtraction ExtractLayerPayload(
        uint itemId,
        int layerIndex,
        long maximumBytes);

    /// <summary>Extracts an opaque container metadata item without interpreting its bytes.</summary>
    /// <param name="itemId">The nonzero custom metadata item identifier.</param>
    /// <param name="maximumBytes">The maximum permitted output byte count.</param>
    /// <returns>An owned extraction with exact source-segment mappings.</returns>
    AvifPayloadExtraction ExtractUnknownMetadata(
        uint itemId,
        long maximumBytes);

    /// <summary>Copies one encoded item payload into a caller-provided buffer.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <param name="destination">The destination buffer, which may be larger than the payload.</param>
    /// <returns>Success, BufferTooSmall, NotFound, or the reader state code.</returns>
    AvifResultCode CopyEncodedItemTo(
        uint itemId,
        Span<byte> destination);

    /// <summary>Writes one encoded item payload without closing the destination stream.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, NotFound, InvalidArgument, or the reader state code.</returns>
    AvifResultCode WriteEncodedItemTo(
        uint itemId,
        Stream destination);

    /// <summary>Copies one complete parsed box into a caller-provided buffer.</summary>
    /// <param name="boxIndex">The zero-based depth-first box index.</param>
    /// <param name="destination">The destination buffer, which may be larger than the box.</param>
    /// <returns>Success, BufferTooSmall, NotFound, or the reader state code.</returns>
    AvifResultCode CopyBoxDataTo(
        int boxIndex,
        Span<byte> destination);

    /// <summary>Writes one complete parsed box without closing the destination stream.</summary>
    /// <param name="boxIndex">The zero-based depth-first box index.</param>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, NotFound, InvalidArgument, or the reader state code.</returns>
    AvifResultCode WriteBoxDataTo(
        int boxIndex,
        Stream destination);

    /// <summary>Copies one resolved item extent into a caller-provided buffer.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <param name="extentIndex">The zero-based extent index.</param>
    /// <param name="destination">The destination buffer, which may be larger than the extent.</param>
    /// <returns>Success, BufferTooSmall, NotFound, InvalidItem, or the reader state code.</returns>
    AvifResultCode CopyItemExtentTo(
        uint itemId,
        int extentIndex,
        Span<byte> destination);

    /// <summary>Writes one resolved item extent without closing the destination stream.</summary>
    /// <param name="itemId">The nonzero item identifier.</param>
    /// <param name="extentIndex">The zero-based extent index.</param>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, NotFound, InvalidItem, InvalidArgument, or the reader state code.</returns>
    AvifResultCode WriteItemExtentTo(
        uint itemId,
        int extentIndex,
        Stream destination);

    /// <summary>Copies one encoded track sample into a caller-provided buffer.</summary>
    /// <param name="trackId">The nonzero track identifier.</param>
    /// <param name="sampleIndex">The zero-based sample index.</param>
    /// <param name="destination">The destination buffer, which may be larger than the sample.</param>
    /// <returns>Success, BufferTooSmall, NotFound, InvalidTrack, or the reader state code.</returns>
    AvifResultCode CopySampleTo(
        uint trackId,
        int sampleIndex,
        Span<byte> destination);

    /// <summary>Writes one encoded track sample without closing the destination stream.</summary>
    /// <param name="trackId">The nonzero track identifier.</param>
    /// <param name="sampleIndex">The zero-based sample index.</param>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, NotFound, InvalidTrack, InvalidArgument, or the reader state code.</returns>
    AvifResultCode WriteSampleTo(
        uint trackId,
        int sampleIndex,
        Stream destination);

    /// <summary>Clears parse and decode state while retaining the buffered source.</summary>
    /// <returns>Success, or Closed after closing the reader.</returns>
    AvifResultCode Reset();

    /// <summary>Releases retained source and parse state without closing a caller stream.</summary>
    /// <returns>Success; repeated calls are harmless.</returns>
    AvifResultCode Close();
}
