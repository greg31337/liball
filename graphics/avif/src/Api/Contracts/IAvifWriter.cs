using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifWriter"/>.</summary>
public interface IAvifWriter
{
    /// <summary>Returns an independent copy of the active authoring controls.</summary>
    /// <returns>The active options, or null after closure.</returns>
    AvifEncoderOptions? GetOptions();

    /// <summary>Returns the result of the most recent writer operation.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Encodes and adds a visible image item.</summary>
    /// <param name="image">The image to snapshot and encode.</param>
    /// <returns>The allocated nonzero item identifier, or zero on failure.</returns>
    uint AddImage(AvifImage image);

    /// <summary>Encodes and adds a hidden image item.</summary>
    /// <param name="image">The image to snapshot and encode.</param>
    /// <returns>The allocated nonzero item identifier, or zero on failure.</returns>
    uint AddHiddenImage(AvifImage image);

    /// <summary>Adds a caller-supplied conforming AV1 item without decoding or re-encoding it.</summary>
    /// <param name="payload">The complete low-overhead AV1 OBU stream to copy.</param>
    /// <param name="configuration">The matching AV1 codec configuration.</param>
    /// <param name="width">The positive coded width.</param>
    /// <param name="height">The positive coded height.</param>
    /// <param name="hidden">Whether the item is hidden.</param>
    /// <returns>The allocated item identifier, or zero on failure.</returns>
    uint AddAv1Item(
        byte[] payload,
        Av1CodecConfiguration configuration,
        int width,
        int height,
        bool hidden);

    /// <summary>Adds an arbitrary copied non-AV1 item to the collection.</summary>
    /// <param name="itemType">The four-character item type.</param>
    /// <param name="name">The item name without embedded nulls.</param>
    /// <param name="contentType">The required MIME type for a <c>mime</c> item, otherwise empty.</param>
    /// <param name="contentEncoding">The optional MIME encoding, otherwise empty.</param>
    /// <param name="uriType">The required URI type for a <c>uri </c> item, otherwise empty.</param>
    /// <param name="payload">The item bytes to copy.</param>
    /// <param name="hidden">Whether the item is hidden.</param>
    /// <returns>The allocated item identifier, or zero on failure.</returns>
    uint AddItem(
        string itemType,
        string name,
        string contentType,
        string contentEncoding,
        string uriType,
        byte[] payload,
        bool hidden);

    /// <summary>Adds an arbitrary directional relationship between staged items.</summary>
    /// <param name="type">The four-character reference type.</param>
    /// <param name="fromItemId">The source item identifier.</param>
    /// <param name="targetItemIds">One or more distinct target identifiers.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    AvifResultCode AddItemReference(
        string type,
        uint fromItemId,
        uint[] targetItemIds);

    /// <summary>Adds an arbitrary entity group containing staged items.</summary>
    /// <param name="type">The four-character grouping type.</param>
    /// <param name="entityIds">One or more distinct item identifiers.</param>
    /// <returns>The allocated group identifier, or zero on failure.</returns>
    uint AddEntityGroup(string type, uint[] entityIds);

    /// <summary>Adds a visible grid whose populated cells are encoded as independent hidden items.</summary>
    /// <param name="grid">The complete authoring grid to snapshot.</param>
    /// <returns>The allocated grid item identifier, or zero on failure.</returns>
    uint AddGrid(AvifGrid grid);

    /// <summary>Adds one visible AV1 item containing shared progressive layer data.</summary>
    /// <param name="layeredImage">The ordered layers and selection signaling to snapshot.</param>
    /// <returns>The allocated item identifier, or zero on failure.</returns>
    uint AddLayeredImage(AvifLayeredImage layeredImage);

    /// <summary>Adds a tone-map derived image and its hidden gain-map dependency.</summary>
    /// <param name="itemId">The visible base image and backward-compatible fallback.</param>
    /// <param name="gainMap">The gain-map image, metadata, and alternate color information.</param>
    /// <returns>The allocated <c>tmap</c> item identifier, or zero on failure.</returns>
    uint AddGainMap(uint itemId, AvifGainMap gainMap);

    /// <summary>Adds a sample-transform derived image with encoded inputs and a visible fallback.</summary>
    /// <param name="transform">The expression, ordered inputs, and output depth to snapshot.</param>
    /// <returns>The allocated <c>sato</c> item identifier, or zero on failure.</returns>
    uint AddSampleTransform(AvifSampleTransform transform);

    /// <summary>Adds one timed AV1 image sequence and a still-image fallback.</summary>
    /// <param name="sequence">The frames, timing, repetition, alpha, and sample signaling.</param>
    /// <returns>The color track identifier, or zero on failure.</returns>
    uint AddSequence(AvifSequence sequence);

    /// <summary>Adds a hidden thumbnail linked to an existing image item.</summary>
    /// <param name="itemId">The master image item identifier.</param>
    /// <param name="image">The thumbnail image to snapshot and encode.</param>
    /// <returns>The allocated thumbnail item identifier, or zero on failure.</returns>
    uint AddThumbnail(uint itemId, AvifImage image);

    /// <summary>Adds a hidden alpha, depth, or caller-defined auxiliary image.</summary>
    /// <param name="itemId">The master image item identifier.</param>
    /// <param name="auxiliary">The auxiliary image and relationship to snapshot.</param>
    /// <returns>The allocated auxiliary item identifier, or zero on failure.</returns>
    uint AddAuxiliary(uint itemId, AvifAuxiliaryImage auxiliary);

    /// <summary>Adds an alternate-image entity group in preference order.</summary>
    /// <param name="itemIds">Two or more distinct visible image item identifiers.</param>
    /// <returns>The allocated group identifier, or zero on failure.</returns>
    uint AddAlternateGroup(uint[] itemIds);

    /// <summary>Adds an ordered left/right stereo entity group.</summary>
    /// <param name="leftItemId">The visible left-view image item identifier.</param>
    /// <param name="rightItemId">The visible right-view image item identifier.</param>
    /// <returns>The allocated group identifier, or zero on failure.</returns>
    uint AddStereoPair(uint leftItemId, uint rightItemId);

    /// <summary>Adds a metadata item describing an existing image item.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="metadata">The metadata payload to copy.</param>
    /// <returns>The allocated metadata item identifier, or zero on failure.</returns>
    uint AddMetadata(uint itemId, AvifMetadata metadata);

    /// <summary>Adds one metadata item describing multiple existing image items.</summary>
    /// <param name="itemIds">One or more distinct target image item identifiers.</param>
    /// <param name="metadata">The metadata payload and item information to copy.</param>
    /// <returns>The allocated metadata item identifier, or zero on failure.</returns>
    uint AddMetadataToItems(uint[] itemIds, AvifMetadata metadata);

    /// <summary>Adds a content-description relationship from existing metadata to an image item.</summary>
    /// <param name="metadataItemId">The staged metadata item identifier.</param>
    /// <param name="itemId">The target image item identifier.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    AvifResultCode AddMetadataAssociation(
        uint metadataItemId,
        uint itemId);

    /// <summary>Appends one opaque metadata OBU to a staged coded image item.</summary>
    /// <param name="itemId">The coded-image item identifier.</param>
    /// <param name="metadata">The unknown metadata value to snapshot.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    AvifResultCode AddItemMetadataObu(
        uint itemId,
        AvifMetadataObu metadata);

    /// <summary>Adds validated Exif metadata describing an existing image item.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="exif">The Exif metadata to snapshot.</param>
    /// <returns>The allocated metadata item identifier, or zero on failure.</returns>
    uint AddExif(uint itemId, AvifExif exif);

    /// <summary>Adds a validated XMP packet describing an existing image item.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="xmp">The XMP metadata to snapshot.</param>
    /// <returns>The allocated metadata item identifier, or zero on failure.</returns>
    uint AddXmp(uint itemId, AvifXmp xmp);

    /// <summary>Replaces the clean-aperture, rotation, mirror, and pixel-aspect properties.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="transform">The transform collection to snapshot.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetTransform(uint itemId, AvifTransform transform);

    /// <summary>Replaces the supported static HDR and viewing-environment properties.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="metadata">The HDR metadata to snapshot.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetHdrMetadata(
        uint itemId,
        AvifHdrMetadata metadata);

    /// <summary>Adds one validated unknown nonessential property to an image item.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="property">The opaque property to snapshot.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode AddOpaqueProperty(
        uint itemId,
        AvifOpaqueBox property);

    /// <summary>Appends a safe unknown top-level box after authored media.</summary>
    /// <param name="box">The complete unknown nonessential box to snapshot.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    AvifResultCode AddOpaqueTopLevelBox(AvifOpaqueBox box);

    /// <summary>Selects an existing visible image as the primary item.</summary>
    /// <param name="itemId">The image item identifier.</param>
    /// <returns>Success, InvalidArgument, InvalidState, or Closed.</returns>
    AvifResultCode SetPrimaryItem(uint itemId);

    /// <summary>Finalizes and validates the container.</summary>
    /// <returns>An independently owned encoded result; repeated calls return the same bytes.</returns>
    AvifEncodeResult Finish();

    /// <summary>Writes finalized bytes without closing the destination stream.</summary>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, InvalidState before finalization, InvalidArgument, or Closed.</returns>
    AvifResultCode Write(Stream destination);

    /// <summary>Returns the number of staged image and metadata items.</summary>
    /// <returns>The staged count, or zero after closure.</returns>
    int GetItemCount();

    /// <summary>Returns the selected primary item identifier.</summary>
    /// <returns>The nonzero identifier, or zero when none is selected.</returns>
    uint GetPrimaryItemId();

    /// <summary>Reports whether finalization completed successfully.</summary>
    /// <returns><c>true</c> when bytes are available for writing.</returns>
    bool IsFinished();

    /// <summary>Releases all staged and finalized data.</summary>
    /// <returns>Success; repeated calls are harmless.</returns>
    AvifResultCode Close();
}
