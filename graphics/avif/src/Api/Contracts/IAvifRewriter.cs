using System;
using System.Collections.Generic;
using System.IO;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifRewriter"/>.</summary>
public interface IAvifRewriter
{
    /// <summary>Returns an independent copy of the active rewrite options.</summary>
    /// <returns>The current rewrite and preservation options.</returns>
    AvifRewriteOptions? GetOptions();

    /// <summary>Opens and validates one complete source copy.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode Open(byte[] source);

    /// <summary>Returns the result of the most recent rewrite operation.</summary>
    /// <returns>The stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Selects an existing visible image as the rewritten primary item.</summary>
    /// <param name="itemId">The image item identifier.</param>
    /// <returns>Success, InvalidArgument, InvalidState, or Closed.</returns>
    AvifResultCode SetPrimaryItem(uint itemId);

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

    /// <summary>Adds an arbitrary copied non-AV1 item to the remuxed collection.</summary>
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

    /// <summary>Adds an arbitrary directional relationship between retained or staged items.</summary>
    /// <param name="type">The four-character reference type.</param>
    /// <param name="fromItemId">The source item identifier.</param>
    /// <param name="targetItemIds">One or more distinct target identifiers.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    AvifResultCode AddItemReference(
        string type,
        uint fromItemId,
        uint[] targetItemIds);

    /// <summary>Adds an arbitrary entity group containing retained or staged items.</summary>
    /// <param name="type">The four-character grouping type.</param>
    /// <param name="entityIds">One or more distinct item identifiers.</param>
    /// <returns>The allocated group identifier, or zero on failure.</returns>
    uint AddEntityGroup(string type, uint[] entityIds);

    /// <summary>Adds one validated unknown nonessential property to any retained or staged item.</summary>
    /// <param name="itemId">The target item identifier.</param>
    /// <param name="property">The opaque property to snapshot.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    AvifResultCode AddOpaqueProperty(
        uint itemId,
        AvifOpaqueBox property);

    /// <summary>Appends a safe unknown top-level box after remuxed media.</summary>
    /// <param name="box">The complete unknown nonessential box to snapshot.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    AvifResultCode AddOpaqueTopLevelBox(AvifOpaqueBox box);

    /// <summary>Adds a copied metadata item describing an existing image item.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="metadata">The metadata payload to copy.</param>
    /// <returns>The allocated metadata item identifier, or zero on failure.</returns>
    uint AddMetadata(uint itemId, AvifMetadata metadata);

    /// <summary>Adds one copied metadata item describing multiple existing image items.</summary>
    /// <param name="itemIds">One or more distinct target image item identifiers.</param>
    /// <param name="metadata">The metadata payload and item information to copy.</param>
    /// <returns>The allocated metadata item identifier, or zero on failure.</returns>
    uint AddMetadataToItems(uint[] itemIds, AvifMetadata metadata);

    /// <summary>Adds a content-description relationship from metadata to an image item.</summary>
    /// <param name="metadataItemId">An existing or staged metadata item identifier.</param>
    /// <param name="itemId">The target image item identifier.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    AvifResultCode AddMetadataAssociation(
        uint metadataItemId,
        uint itemId);

    /// <summary>Appends one opaque metadata OBU without re-encoding a coded image item.</summary>
    /// <param name="itemId">The coded-image item identifier.</param>
    /// <param name="metadata">The unknown metadata value to snapshot.</param>
    /// <returns>Success, InvalidArgument, or LimitExceeded.</returns>
    AvifResultCode AddItemMetadataObu(
        uint itemId,
        AvifMetadataObu metadata);

    /// <summary>Removes metadata of one kind from an image while retaining shared associations.</summary>
    /// <param name="itemId">The described image item identifier.</param>
    /// <param name="kind">The metadata representation to remove.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode RemoveMetadata(
        uint itemId,
        AvifMetadataKind kind);

    /// <summary>Replaces supported item transform properties without touching encoded pixels.</summary>
    /// <param name="itemId">The target image item identifier.</param>
    /// <param name="transform">The transform collection to snapshot.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetTransform(
        uint itemId,
        AvifTransform transform);

    /// <summary>Adds a compatible brand to canonical rewritten output.</summary>
    /// <param name="brand">The four-character brand.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode AddCompatibleBrand(string brand);

    /// <summary>Removes a non-required compatible brand from rewritten output.</summary>
    /// <param name="brand">The four-character brand.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode RemoveCompatibleBrand(string brand);

    /// <summary>Requests canonical ordering, brands, structure, and payload placement.</summary>
    /// <returns>Success, InvalidState, or Closed.</returns>
    AvifResultCode Normalize();

    /// <summary>Finalizes and validates rewritten output.</summary>
    /// <returns>An independently owned encoded result.</returns>
    AvifEncodeResult Finish();

    /// <summary>Copies finalized rewritten bytes into a caller-provided buffer.</summary>
    /// <param name="destination">The destination buffer, which may be larger than the result.</param>
    /// <returns>Success, BufferTooSmall, InvalidState, or Closed.</returns>
    AvifResultCode CopyResultTo(Span<byte> destination);

    /// <summary>Returns the finalized rewrite byte count without copying it.</summary>
    /// <returns>The result length, or zero before successful finalization or after closure.</returns>
    int GetResultLength();

    /// <summary>Writes finalized bytes without closing the destination stream.</summary>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, InvalidState, InvalidArgument, or Closed.</returns>
    AvifResultCode Write(Stream destination);

    /// <summary>Reports whether a source has been opened successfully.</summary>
    /// <returns><c>true</c> after a successful open and before closure.</returns>
    bool IsOpen();

    /// <summary>Reports whether finalization completed successfully.</summary>
    /// <returns><c>true</c> when rewritten bytes are available.</returns>
    bool IsFinished();

    /// <summary>Releases retained source, edits, and output bytes.</summary>
    /// <returns>Success; repeated calls are harmless.</returns>
    AvifResultCode Close();
}
