using System;
using System.Collections.Generic;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifImageItem"/>.</summary>
public interface IAvifImageItem
{
    /// <summary>Returns the item identifier.</summary>
    /// <returns>The item identifier.</returns>
    uint GetItemId();

    /// <summary>Returns the ItemInfoEntry version.</summary>
    /// <returns>The entry version.</returns>
    int GetItemInfoVersion();

    /// <summary>Returns the four-character item type.</summary>
    /// <returns>The item type.</returns>
    string GetItemType();

    /// <summary>Returns the item name.</summary>
    /// <returns>The name.</returns>
    string GetName();

    /// <summary>Returns the MIME content type.</summary>
    /// <returns>The content type, or an empty string.</returns>
    string GetContentType();

    /// <summary>Returns the MIME content encoding.</summary>
    /// <returns>The content encoding, or an empty string.</returns>
    string GetContentEncoding();

    /// <summary>Returns the URI item type.</summary>
    /// <returns>The URI type, or an empty string.</returns>
    string GetUriType();

    /// <summary>Reports whether the item is hidden.</summary>
    /// <returns><c>true</c> when hidden.</returns>
    bool IsHidden();

    /// <summary>Returns the protection information index.</summary>
    /// <returns>The protection index.</returns>
    ushort GetProtectionIndex();

    /// <summary>Reports whether this is the primary item.</summary>
    /// <returns><c>true</c> for the primary item.</returns>
    bool IsPrimary();

    /// <summary>Reports whether this is a coded image item.</summary>
    /// <returns><c>true</c> for a coded image item.</returns>
    bool IsCodedImage();

    /// <summary>Reports whether this is a derived image item.</summary>
    /// <returns><c>true</c> for a derived image item.</returns>
    bool IsDerivedImage();

    /// <summary>Reports whether this is a metadata item.</summary>
    /// <returns><c>true</c> for a metadata item.</returns>
    bool IsMetadataItem();

    /// <summary>Reports whether this is an auxiliary image item.</summary>
    /// <returns><c>true</c> for an auxiliary item.</returns>
    bool IsAuxiliaryImage();

    /// <summary>Returns the interpreted auxiliary kind.</summary>
    /// <returns>The auxiliary kind, or <c>None</c> for a master item.</returns>
    AvifAuxiliaryKind GetAuxiliaryKind();

    /// <summary>Returns the exact auxiliary type identifier.</summary>
    /// <returns>The type identifier, or an empty string.</returns>
    string GetAuxiliaryTypeName();

    /// <summary>Returns the number of master items served by this auxiliary.</summary>
    /// <returns>The auxiliary-master count.</returns>
    int GetAuxiliaryMasterCount();

    /// <summary>Returns one master item identifier.</summary>
    /// <param name="index">The zero-based master index.</param>
    /// <returns>The item identifier, or zero for an invalid index.</returns>
    uint GetAuxiliaryMasterItemId(int index);

    /// <summary>Returns the number of auxiliary items associated with this master.</summary>
    /// <returns>The associated auxiliary count.</returns>
    int GetAssociatedAuxiliaryItemCount();

    /// <summary>Returns one associated auxiliary item identifier.</summary>
    /// <param name="index">The zero-based association index.</param>
    /// <returns>The item identifier, or zero for an invalid index.</returns>
    uint GetAssociatedAuxiliaryItemId(int index);

    /// <summary>Returns the item pixel width reported by its spatial-extents property.</summary>
    /// <returns>The width, or zero when unavailable.</returns>
    uint GetPixelWidth();

    /// <summary>Returns the item pixel height reported by its spatial-extents property.</summary>
    /// <returns>The height, or zero when unavailable.</returns>
    uint GetPixelHeight();

    /// <summary>Returns the item bit depth reported by AV1 or pixel information.</summary>
    /// <returns>The bit depth, or minus one when unavailable.</returns>
    int GetPixelBitDepth();

    /// <summary>Reports whether this item is a thumbnail.</summary>
    /// <returns><c>true</c> for a thumbnail item.</returns>
    bool IsThumbnail();

    /// <summary>Reports whether the complete dependency chain is self-contained.</summary>
    /// <returns><c>true</c> when self-contained.</returns>
    bool IsSelfContained();

    /// <summary>Reports whether an item location is available.</summary>
    /// <returns><c>true</c> when location data was parsed.</returns>
    bool HasLocation();

    /// <summary>Returns the item construction method.</summary>
    /// <returns>The method, or minus one when no location exists.</returns>
    int GetConstructionMethod();

    /// <summary>Returns the data-reference index.</summary>
    /// <returns>The reference index.</returns>
    ushort GetDataReferenceIndex();

    /// <summary>Returns the item base offset.</summary>
    /// <returns>The unsigned base offset.</returns>
    ulong GetBaseOffset();

    /// <summary>Reports whether the total payload length is known.</summary>
    /// <returns><c>true</c> when the length is known.</returns>
    bool HasKnownPayloadLength();

    /// <summary>Returns the total item payload length.</summary>
    /// <returns>The length, or zero when unavailable.</returns>
    ulong GetPayloadLength();

    /// <summary>Returns the number of item extents.</summary>
    /// <returns>The extent count.</returns>
    int GetExtentCount();

    /// <summary>Returns one item extent.</summary>
    /// <param name="index">The zero-based extent index.</param>
    /// <returns>The extent, or null for an invalid index.</returns>
    AvifItemExtent? GetExtent(int index);

    /// <summary>Returns the number of associated properties.</summary>
    /// <returns>The property count.</returns>
    int GetPropertyCount();

    /// <summary>Returns one associated property.</summary>
    /// <param name="index">The zero-based association index.</param>
    /// <returns>The property, or null for an invalid index.</returns>
    AvifItemProperty? GetProperty(int index);

    /// <summary>Returns the number of outgoing references.</summary>
    /// <returns>The outgoing reference count.</returns>
    int GetOutgoingReferenceCount();

    /// <summary>Returns one outgoing reference.</summary>
    /// <param name="index">The zero-based reference index.</param>
    /// <returns>The reference, or null for an invalid index.</returns>
    AvifItemReference? GetOutgoingReference(int index);

    /// <summary>Returns the number of incoming references.</summary>
    /// <returns>The incoming reference count.</returns>
    int GetIncomingReferenceCount();

    /// <summary>Returns one incoming reference.</summary>
    /// <param name="index">The zero-based reference index.</param>
    /// <returns>The reference, or null for an invalid index.</returns>
    AvifItemReference? GetIncomingReference(int index);

    /// <summary>Returns the number of entity groups containing this item.</summary>
    /// <returns>The group count.</returns>
    int GetGroupCount();

    /// <summary>Returns one containing group identifier.</summary>
    /// <param name="index">The zero-based group index.</param>
    /// <returns>The group identifier, or zero for an invalid index.</returns>
    uint GetGroupId(int index);

    /// <summary>Returns reconciled HDR and viewing metadata from associated properties.</summary>
    /// <returns>A metadata copy that can also reconcile AV1 metadata OBU payloads.</returns>
    AvifHdrMetadata GetHdrMetadata();

    /// <summary>Returns aggregated clean-aperture, orientation, and pixel-aspect metadata.</summary>
    /// <returns>An independent transform copy.</returns>
    AvifTransform GetTransform();
}
