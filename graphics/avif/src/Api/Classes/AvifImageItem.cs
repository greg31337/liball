using System;
using System.Collections.Generic;

namespace avif;

/// <summary>
/// Describes one parsed HEIF item, its storage, roles, properties, and relationships.
/// </summary>
public class AvifImageItem : IAvifImageItem
{
    uint itemId;
    int itemInfoVersion;
    string itemType;
    string name;
    string contentType;
    string contentEncoding;
    string uriType;
    bool hidden;
    ushort protectionIndex;
    bool primary;
    bool codedImage;
    bool derivedImage;
    bool metadataItem;
    bool auxiliaryImage;
    AvifAuxiliaryKind auxiliaryKind;
    string auxiliaryTypeName;
    bool thumbnail;
    bool selfContained;
    bool location;
    int constructionMethod;
    ushort dataReferenceIndex;
    ulong baseOffset;
    bool knownPayloadLength;
    ulong payloadLength;
    List<AvifItemExtent> extents;
    List<AvifItemProperty> properties;
    List<AvifItemReference> outgoingReferences;
    List<AvifItemReference> incomingReferences;
    List<uint> groupIds;
    List<uint> auxiliaryMasterIds;
    List<uint> associatedAuxiliaryItemIds;
    AvifHdrMetadata hdrMetadata;
    AvifTransform transform;
    uint pixelWidth;
    uint pixelHeight;
    int pixelBitDepth;

    internal AvifImageItem(
        ParsedItem item,
        ParsedItemRole? role,
        bool selfContained,
        List<AvifItemExtent> extents,
        List<AvifItemProperty> properties,
        List<AvifItemReference> outgoingReferences,
        List<AvifItemReference> incomingReferences,
        List<uint> groupIds)
    {
        itemId = item.GetItemId();
        itemInfoVersion = item.GetItemInfoVersion();
        itemType = item.GetItemType();
        name = item.GetName();
        contentType = item.GetContentType();
        contentEncoding = item.GetContentEncoding();
        uriType = item.GetItemUriType();
        hidden = item.IsHidden();
        protectionIndex = item.GetProtectionIndex();
        primary = role != null && role.IsPrimary();
        codedImage = role != null && role.IsCodedImage();
        derivedImage = role != null && role.IsDerivedImage();
        metadataItem = role != null && role.IsMetadataItem();
        auxiliaryImage = role != null && role.IsAuxiliaryImage();
        auxiliaryKind = GetAuxiliaryKind(role);
        auxiliaryTypeName = role == null ? string.Empty : role.GetAuxiliaryType();
        thumbnail = role != null && role.IsThumbnail();
        this.selfContained = selfContained;
        location = item.HasLocation();
        constructionMethod = -1;
        dataReferenceIndex = 0;
        baseOffset = 0;
        knownPayloadLength = false;
        payloadLength = 0;
        if (location)
        {
            ParsedItemLocation? parsedLocation = item.GetLocation();
            constructionMethod = (parsedLocation ?? throw new InvalidOperationException("Invalid internal state.")).GetConstructionMethod();
            dataReferenceIndex = parsedLocation.GetDataReferenceIndex();
            baseOffset = parsedLocation.GetBaseOffset();
            knownPayloadLength = parsedLocation.HasKnownTotalLength();
            payloadLength = parsedLocation.GetTotalLength();
        }

        this.extents = new List<AvifItemExtent>(extents);
        this.properties = new List<AvifItemProperty>(properties);
        this.outgoingReferences = new List<AvifItemReference>(outgoingReferences);
        this.incomingReferences = new List<AvifItemReference>(incomingReferences);
        this.groupIds = new List<uint>(groupIds);
        auxiliaryMasterIds = new List<uint>();
        associatedAuxiliaryItemIds = new List<uint>();
        ReadAuxiliaryLinks(role);
        hdrMetadata = new AvifHdrMetadata();
        transform = new AvifTransform();
        pixelWidth = 0;
        pixelHeight = 0;
        pixelBitDepth = -1;
        int propertyIndex = 0;
        while (propertyIndex < properties.Count)
        {
            MetadataResolver.Merge(
                hdrMetadata,
                properties[propertyIndex].GetHdrMetadataValue());
            transform.Merge(properties[propertyIndex].GetTransformValue());
            ReadPixelDescription(properties[propertyIndex]);
            propertyIndex++;
        }
    }

    /// <summary>Returns the item identifier.</summary>
    /// <returns>The item identifier.</returns>
    public uint GetItemId()
    {
        return itemId;
    }

    /// <summary>Returns the ItemInfoEntry version.</summary>
    /// <returns>The entry version.</returns>
    public int GetItemInfoVersion()
    {
        return itemInfoVersion;
    }

    /// <summary>Returns the four-character item type.</summary>
    /// <returns>The item type.</returns>
    public string GetItemType()
    {
        return itemType;
    }

    /// <summary>Returns the item name.</summary>
    /// <returns>The name.</returns>
    public string GetName()
    {
        return name;
    }

    /// <summary>Returns the MIME content type.</summary>
    /// <returns>The content type, or an empty string.</returns>
    public string GetContentType()
    {
        return contentType;
    }

    /// <summary>Returns the MIME content encoding.</summary>
    /// <returns>The content encoding, or an empty string.</returns>
    public string GetContentEncoding()
    {
        return contentEncoding;
    }

    /// <summary>Returns the URI item type.</summary>
    /// <returns>The URI type, or an empty string.</returns>
    public string GetUriType()
    {
        return uriType;
    }

    /// <summary>Reports whether the item is hidden.</summary>
    /// <returns><c>true</c> when hidden.</returns>
    public bool IsHidden()
    {
        return hidden;
    }

    /// <summary>Returns the protection information index.</summary>
    /// <returns>The protection index.</returns>
    public ushort GetProtectionIndex()
    {
        return protectionIndex;
    }

    /// <summary>Reports whether this is the primary item.</summary>
    /// <returns><c>true</c> for the primary item.</returns>
    public bool IsPrimary()
    {
        return primary;
    }

    /// <summary>Reports whether this is a coded image item.</summary>
    /// <returns><c>true</c> for a coded image item.</returns>
    public bool IsCodedImage()
    {
        return codedImage;
    }

    /// <summary>Reports whether this is a derived image item.</summary>
    /// <returns><c>true</c> for a derived image item.</returns>
    public bool IsDerivedImage()
    {
        return derivedImage;
    }

    /// <summary>Reports whether this is a metadata item.</summary>
    /// <returns><c>true</c> for a metadata item.</returns>
    public bool IsMetadataItem()
    {
        return metadataItem;
    }

    /// <summary>Reports whether this is an auxiliary image item.</summary>
    /// <returns><c>true</c> for an auxiliary item.</returns>
    public bool IsAuxiliaryImage()
    {
        return auxiliaryImage;
    }

    /// <summary>Returns the interpreted auxiliary kind.</summary>
    /// <returns>The auxiliary kind, or <c>None</c> for a master item.</returns>
    public AvifAuxiliaryKind GetAuxiliaryKind()
    {
        return auxiliaryKind;
    }

    /// <summary>Returns the exact auxiliary type identifier.</summary>
    /// <returns>The type identifier, or an empty string.</returns>
    public string GetAuxiliaryTypeName()
    {
        return auxiliaryTypeName;
    }

    /// <summary>Returns the number of master items served by this auxiliary.</summary>
    /// <returns>The auxiliary-master count.</returns>
    public int GetAuxiliaryMasterCount()
    {
        return auxiliaryMasterIds.Count;
    }

    /// <summary>Returns one master item identifier.</summary>
    /// <param name="index">The zero-based master index.</param>
    /// <returns>The item identifier, or zero for an invalid index.</returns>
    public uint GetAuxiliaryMasterItemId(int index)
    {
        if (index < 0 || index >= auxiliaryMasterIds.Count)
        {
            return 0;
        }

        return auxiliaryMasterIds[index];
    }

    /// <summary>Returns the number of auxiliary items associated with this master.</summary>
    /// <returns>The associated auxiliary count.</returns>
    public int GetAssociatedAuxiliaryItemCount()
    {
        return associatedAuxiliaryItemIds.Count;
    }

    /// <summary>Returns one associated auxiliary item identifier.</summary>
    /// <param name="index">The zero-based association index.</param>
    /// <returns>The item identifier, or zero for an invalid index.</returns>
    public uint GetAssociatedAuxiliaryItemId(int index)
    {
        if (index < 0 || index >= associatedAuxiliaryItemIds.Count)
        {
            return 0;
        }

        return associatedAuxiliaryItemIds[index];
    }

    /// <summary>Returns the item pixel width reported by its spatial-extents property.</summary>
    /// <returns>The width, or zero when unavailable.</returns>
    public uint GetPixelWidth()
    {
        return pixelWidth;
    }

    /// <summary>Returns the item pixel height reported by its spatial-extents property.</summary>
    /// <returns>The height, or zero when unavailable.</returns>
    public uint GetPixelHeight()
    {
        return pixelHeight;
    }

    /// <summary>Returns the item bit depth reported by AV1 or pixel information.</summary>
    /// <returns>The bit depth, or minus one when unavailable.</returns>
    public int GetPixelBitDepth()
    {
        return pixelBitDepth;
    }

    /// <summary>Reports whether this item is a thumbnail.</summary>
    /// <returns><c>true</c> for a thumbnail item.</returns>
    public bool IsThumbnail()
    {
        return thumbnail;
    }

    /// <summary>Reports whether the complete dependency chain is self-contained.</summary>
    /// <returns><c>true</c> when self-contained.</returns>
    public bool IsSelfContained()
    {
        return selfContained;
    }

    /// <summary>Reports whether an item location is available.</summary>
    /// <returns><c>true</c> when location data was parsed.</returns>
    public bool HasLocation()
    {
        return location;
    }

    /// <summary>Returns the item construction method.</summary>
    /// <returns>The method, or minus one when no location exists.</returns>
    public int GetConstructionMethod()
    {
        return constructionMethod;
    }

    /// <summary>Returns the data-reference index.</summary>
    /// <returns>The reference index.</returns>
    public ushort GetDataReferenceIndex()
    {
        return dataReferenceIndex;
    }

    /// <summary>Returns the item base offset.</summary>
    /// <returns>The unsigned base offset.</returns>
    public ulong GetBaseOffset()
    {
        return baseOffset;
    }

    /// <summary>Reports whether the total payload length is known.</summary>
    /// <returns><c>true</c> when the length is known.</returns>
    public bool HasKnownPayloadLength()
    {
        return knownPayloadLength;
    }

    /// <summary>Returns the total item payload length.</summary>
    /// <returns>The length, or zero when unavailable.</returns>
    public ulong GetPayloadLength()
    {
        return payloadLength;
    }

    /// <summary>Returns the number of item extents.</summary>
    /// <returns>The extent count.</returns>
    public int GetExtentCount()
    {
        return extents.Count;
    }

    /// <summary>Returns one item extent.</summary>
    /// <param name="index">The zero-based extent index.</param>
    /// <returns>The extent, or null for an invalid index.</returns>
    public AvifItemExtent? GetExtent(int index)
    {
        if (index < 0 || index >= extents.Count)
        {
            return null;
        }

        return extents[index];
    }

    /// <summary>Returns the number of associated properties.</summary>
    /// <returns>The property count.</returns>
    public int GetPropertyCount()
    {
        return properties.Count;
    }

    /// <summary>Returns one associated property.</summary>
    /// <param name="index">The zero-based association index.</param>
    /// <returns>The property, or null for an invalid index.</returns>
    public AvifItemProperty? GetProperty(int index)
    {
        if (index < 0 || index >= properties.Count)
        {
            return null;
        }

        return properties[index];
    }

    /// <summary>Returns the number of outgoing references.</summary>
    /// <returns>The outgoing reference count.</returns>
    public int GetOutgoingReferenceCount()
    {
        return outgoingReferences.Count;
    }

    /// <summary>Returns one outgoing reference.</summary>
    /// <param name="index">The zero-based reference index.</param>
    /// <returns>The reference, or null for an invalid index.</returns>
    public AvifItemReference? GetOutgoingReference(int index)
    {
        if (index < 0 || index >= outgoingReferences.Count)
        {
            return null;
        }

        return outgoingReferences[index];
    }

    /// <summary>Returns the number of incoming references.</summary>
    /// <returns>The incoming reference count.</returns>
    public int GetIncomingReferenceCount()
    {
        return incomingReferences.Count;
    }

    /// <summary>Returns one incoming reference.</summary>
    /// <param name="index">The zero-based reference index.</param>
    /// <returns>The reference, or null for an invalid index.</returns>
    public AvifItemReference? GetIncomingReference(int index)
    {
        if (index < 0 || index >= incomingReferences.Count)
        {
            return null;
        }

        return incomingReferences[index];
    }

    /// <summary>Returns the number of entity groups containing this item.</summary>
    /// <returns>The group count.</returns>
    public int GetGroupCount()
    {
        return groupIds.Count;
    }

    /// <summary>Returns one containing group identifier.</summary>
    /// <param name="index">The zero-based group index.</param>
    /// <returns>The group identifier, or zero for an invalid index.</returns>
    public uint GetGroupId(int index)
    {
        if (index < 0 || index >= groupIds.Count)
        {
            return 0;
        }

        return groupIds[index];
    }

    /// <summary>Returns reconciled HDR and viewing metadata from associated properties.</summary>
    /// <returns>A metadata copy that can also reconcile AV1 metadata OBU payloads.</returns>
    public AvifHdrMetadata GetHdrMetadata()
    {
        return AvifHdrMetadata.CreateCopy(hdrMetadata);
    }

    /// <summary>Returns aggregated clean-aperture, orientation, and pixel-aspect metadata.</summary>
    /// <returns>An independent transform copy.</returns>
    public AvifTransform GetTransform()
    {
        return AvifTransform.CreateCopy(transform);
    }

    internal static AvifAuxiliaryKind GetAuxiliaryKind(ParsedItemRole? role)
    {
        if (role == null || !role.IsAuxiliaryImage())
        {
            return AvifAuxiliaryKind.None;
        }
        if (role.IsAlphaImage())
        {
            return AvifAuxiliaryKind.Alpha;
        }
        if (role.IsDepthImage())
        {
            return AvifAuxiliaryKind.Depth;
        }
        return AvifAuxiliaryKind.Other;
    }

    void ReadAuxiliaryLinks(ParsedItemRole? role)
    {
        if (role == null)
        {
            return;
        }

        int index = 0;
        while (index < role.GetAuxiliaryMasterCount())
        {
            auxiliaryMasterIds.Add((role.GetAuxiliaryMaster(index) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId());
            index++;
        }

        index = 0;
        while (index < role.GetAssociatedAuxiliaryItemCount())
        {
            associatedAuxiliaryItemIds.Add(
                (role.GetAssociatedAuxiliaryItem(index) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId());
            index++;
        }
    }

    void ReadPixelDescription(AvifItemProperty property)
    {
        if (property.GetImageWidth() != 0 && property.GetImageHeight() != 0)
        {
            pixelWidth = property.GetImageWidth();
            pixelHeight = property.GetImageHeight();
        }

        if (property.GetBitDepth() > 0)
        {
            pixelBitDepth = property.GetBitDepth();
        }
        else if (pixelBitDepth < 0 && property.GetPixelChannelCount() > 0)
        {
            pixelBitDepth = property.GetPixelChannelBitDepth(0);
        }
    }
}
