using System;
using System.Collections.Generic;

namespace avif;

internal enum ParsedAuxiliaryItemKind
{
    None = 0,
    Alpha = 1,
    Depth = 2,
    Other = 3
}

internal class ParsedItemRole
{
    ParsedItem item;
    bool primary;
    bool codedImage;
    bool derivedImage;
    bool metadataItem;
    ParsedAuxiliaryItemKind auxiliaryKind;
    string auxiliaryType;
    List<ParsedItem> auxiliaryMasters;
    List<ParsedItem> thumbnailMasters;
    List<ParsedItem> associatedAuxiliaryItems;
    List<ParsedItem> associatedThumbnailItems;

    internal ParsedItemRole(
        ParsedItem item,
        bool primary,
        bool derivedByReference,
        bool metadataByReference,
        string auxiliaryType,
        List<ParsedItem> auxiliaryMasters,
        List<ParsedItem> thumbnailMasters,
        List<ParsedItem> associatedAuxiliaryItems,
        List<ParsedItem> associatedThumbnailItems)
    {
        this.item = item;
        this.primary = primary;
        this.auxiliaryType = auxiliaryType;
        this.auxiliaryMasters = new List<ParsedItem>(auxiliaryMasters);
        this.thumbnailMasters = new List<ParsedItem>(thumbnailMasters);
        this.associatedAuxiliaryItems = new List<ParsedItem>(associatedAuxiliaryItems);
        this.associatedThumbnailItems = new List<ParsedItem>(associatedThumbnailItems);

        string itemType = item.GetItemType();
        codedImage = itemType == "av01";
        derivedImage = derivedByReference
            || itemType == "grid"
            || itemType == "tmap"
            || itemType == "sato";
        metadataItem = metadataByReference
            || itemType == "Exif"
            || itemType == "mime"
            || itemType == "uri ";

        if (this.auxiliaryMasters.Count == 0)
        {
            auxiliaryKind = ParsedAuxiliaryItemKind.None;
        }
        else if (auxiliaryType == "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha"
            || auxiliaryType == "urn:mpeg:hevc:2015:auxid:1")
        {
            auxiliaryKind = ParsedAuxiliaryItemKind.Alpha;
        }
        else if (auxiliaryType == "urn:mpeg:mpegB:cicp:systems:auxiliary:depth")
        {
            auxiliaryKind = ParsedAuxiliaryItemKind.Depth;
        }
        else
        {
            auxiliaryKind = ParsedAuxiliaryItemKind.Other;
        }
    }

    internal ParsedItem GetItem()
    {
        return item;
    }

    internal bool IsPrimary()
    {
        return primary;
    }

    internal bool IsHidden()
    {
        return item.IsHidden();
    }

    internal bool IsThumbnail()
    {
        return thumbnailMasters.Count > 0;
    }

    internal bool IsCodedImage()
    {
        return codedImage;
    }

    internal bool IsDerivedImage()
    {
        return derivedImage;
    }

    internal bool IsMetadataItem()
    {
        return metadataItem;
    }

    internal bool IsAuxiliaryImage()
    {
        return auxiliaryKind != ParsedAuxiliaryItemKind.None;
    }

    internal bool IsAlphaImage()
    {
        return auxiliaryKind == ParsedAuxiliaryItemKind.Alpha;
    }

    internal bool IsDepthImage()
    {
        return auxiliaryKind == ParsedAuxiliaryItemKind.Depth;
    }

    internal bool IsArbitraryAuxiliaryImage()
    {
        return auxiliaryKind == ParsedAuxiliaryItemKind.Other;
    }

    internal ParsedAuxiliaryItemKind GetAuxiliaryKind()
    {
        return auxiliaryKind;
    }

    internal string GetAuxiliaryType()
    {
        return auxiliaryType;
    }

    internal int GetAuxiliaryMasterCount()
    {
        return auxiliaryMasters.Count;
    }

    internal ParsedItem? GetAuxiliaryMaster(int index)
    {
        if (index < 0 || index >= auxiliaryMasters.Count)
        {
            return null;
        }

        return auxiliaryMasters[index];
    }

    internal int GetThumbnailMasterCount()
    {
        return thumbnailMasters.Count;
    }

    internal ParsedItem? GetThumbnailMaster(int index)
    {
        if (index < 0 || index >= thumbnailMasters.Count)
        {
            return null;
        }

        return thumbnailMasters[index];
    }

    internal int GetAssociatedAuxiliaryItemCount()
    {
        return associatedAuxiliaryItems.Count;
    }

    internal ParsedItem? GetAssociatedAuxiliaryItem(int index)
    {
        if (index < 0 || index >= associatedAuxiliaryItems.Count)
        {
            return null;
        }

        return associatedAuxiliaryItems[index];
    }

    internal int GetAssociatedThumbnailItemCount()
    {
        return associatedThumbnailItems.Count;
    }

    internal ParsedItem? GetAssociatedThumbnailItem(int index)
    {
        if (index < 0 || index >= associatedThumbnailItems.Count)
        {
            return null;
        }

        return associatedThumbnailItems[index];
    }
}

internal class ParsedItemDiscovery
{
    List<ParsedItemRole> roles;
    ParsedItemRole? primary;
    List<ParsedItem> hiddenItems;
    List<ParsedItem> thumbnailItems;
    List<ParsedItem> codedItems;
    List<ParsedItem> derivedItems;
    List<ParsedItem> metadataItems;
    List<ParsedItem> auxiliaryItems;
    List<ParsedItem> alphaItems;
    List<ParsedItem> depthItems;
    List<ParsedItem> arbitraryAuxiliaryItems;

    internal ParsedItemDiscovery()
    {
        roles = new List<ParsedItemRole>();
        primary = null;
        hiddenItems = new List<ParsedItem>();
        thumbnailItems = new List<ParsedItem>();
        codedItems = new List<ParsedItem>();
        derivedItems = new List<ParsedItem>();
        metadataItems = new List<ParsedItem>();
        auxiliaryItems = new List<ParsedItem>();
        alphaItems = new List<ParsedItem>();
        depthItems = new List<ParsedItem>();
        arbitraryAuxiliaryItems = new List<ParsedItem>();
    }

    internal static ParsedItemDiscovery Create(
        List<ParsedItem> items,
        bool hasPrimary,
        uint primaryItemId)
    {
        ParsedItemDiscovery discovery = new ParsedItemDiscovery();
        Dictionary<uint, ParsedItem> itemsById = new Dictionary<uint, ParsedItem>();
        int index = 0;
        while (index < items.Count)
        {
            ParsedItem item = items[index];
            itemsById.Add(item.GetItemId(), item);
            index++;
        }

        index = 0;
        while (index < items.Count)
        {
            ParsedItem item = items[index];
            List<ParsedItem> auxiliaryMasters = new List<ParsedItem>();
            List<ParsedItem> thumbnailMasters = new List<ParsedItem>();
            List<ParsedItem> associatedAuxiliaryItems = new List<ParsedItem>();
            List<ParsedItem> associatedThumbnailItems = new List<ParsedItem>();
            bool derivedByReference = false;
            bool metadataByReference = false;
            int referenceIndex = 0;
            while (referenceIndex < item.GetOutgoingReferenceCount())
            {
                ParsedItemReference? reference = item.GetOutgoingReference(referenceIndex);
                if ((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetKind() == ParsedItemReferenceKind.Auxiliary)
                {
                    AddTargets(reference, itemsById, auxiliaryMasters);
                }
                else if (reference.GetKind() == ParsedItemReferenceKind.Thumbnail)
                {
                    AddTargets(reference, itemsById, thumbnailMasters);
                }
                else if (reference.GetKind() == ParsedItemReferenceKind.DerivedImage)
                {
                    derivedByReference = true;
                }
                else if (reference.GetKind() == ParsedItemReferenceKind.ContentDescription)
                {
                    metadataByReference = true;
                }

                referenceIndex++;
            }

            referenceIndex = 0;
            while (referenceIndex < item.GetIncomingReferenceCount())
            {
                ParsedItemReference? reference = item.GetIncomingReference(referenceIndex);
                if ((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetKind() == ParsedItemReferenceKind.Auxiliary)
                {
                    AddSource(reference, itemsById, associatedAuxiliaryItems);
                }
                else if (reference.GetKind() == ParsedItemReferenceKind.Thumbnail)
                {
                    AddSource(reference, itemsById, associatedThumbnailItems);
                }

                referenceIndex++;
            }

            ParsedItemRole role = new ParsedItemRole(
                item,
                hasPrimary && item.GetItemId() == primaryItemId,
                derivedByReference,
                metadataByReference,
                FindAuxiliaryType(item),
                auxiliaryMasters,
                thumbnailMasters,
                associatedAuxiliaryItems,
                associatedThumbnailItems);
            discovery.AddRole(role);
            index++;
        }

        return discovery;
    }

    internal static void AddTargets(
        ParsedItemReference reference,
        Dictionary<uint, ParsedItem> itemsById,
        List<ParsedItem> destination)
    {
        int index = 0;
        while (index < reference.GetTargetCount())
        {
            uint itemId = reference.GetTargetItemId(index);
            if (itemsById.ContainsKey(itemId))
            {
                destination.Add(itemsById[itemId]);
            }

            index++;
        }
    }

    internal static void AddSource(
        ParsedItemReference reference,
        Dictionary<uint, ParsedItem> itemsById,
        List<ParsedItem> destination)
    {
        uint itemId = reference.GetFromItemId();
        if (itemsById.ContainsKey(itemId))
        {
            destination.Add(itemsById[itemId]);
        }
    }

    internal static string FindAuxiliaryType(ParsedItem item)
    {
        string type = string.Empty;
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            ParsedAuxiliaryType? auxiliary = (item
                .GetPropertyAssociation(index) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetProperty()
                .GetAuxiliaryType();
            if (auxiliary != null)
            {
                string candidate = auxiliary.GetTypeValue();
                if (type.Length == 0)
                {
                    type = candidate;
                }
                else if (type != candidate)
                {
                    return string.Empty;
                }
            }

            index++;
        }

        return type;
    }

    internal void AddRole(ParsedItemRole role)
    {
        roles.Add(role);
        ParsedItem item = role.GetItem();
        if (role.IsPrimary())
        {
            primary = role;
        }

        if (role.IsHidden())
        {
            hiddenItems.Add(item);
        }

        if (role.IsThumbnail())
        {
            thumbnailItems.Add(item);
        }

        if (role.IsCodedImage())
        {
            codedItems.Add(item);
        }

        if (role.IsDerivedImage())
        {
            derivedItems.Add(item);
        }

        if (role.IsMetadataItem())
        {
            metadataItems.Add(item);
        }

        if (role.IsAuxiliaryImage())
        {
            auxiliaryItems.Add(item);
        }

        if (role.IsAlphaImage())
        {
            alphaItems.Add(item);
        }

        if (role.IsDepthImage())
        {
            depthItems.Add(item);
        }

        if (role.IsArbitraryAuxiliaryImage())
        {
            arbitraryAuxiliaryItems.Add(item);
        }
    }

    internal int GetRoleCount()
    {
        return roles.Count;
    }

    internal ParsedItemRole? GetRole(int index)
    {
        if (index < 0 || index >= roles.Count)
        {
            return null;
        }

        return roles[index];
    }

    internal ParsedItemRole? FindRole(uint itemId)
    {
        int index = 0;
        while (index < roles.Count)
        {
            ParsedItemRole role = roles[index];
            if (role.GetItem().GetItemId() == itemId)
            {
                return role;
            }

            index++;
        }

        return null;
    }

    internal bool HasPrimaryItem()
    {
        return primary != null;
    }

    internal ParsedItem? GetPrimaryItem()
    {
        if (primary == null)
        {
            return null;
        }

        return primary.GetItem();
    }

    internal int GetHiddenItemCount()
    {
        return hiddenItems.Count;
    }

    internal ParsedItem? GetHiddenItem(int index)
    {
        return GetItem(hiddenItems, index);
    }

    internal int GetThumbnailItemCount()
    {
        return thumbnailItems.Count;
    }

    internal ParsedItem? GetThumbnailItem(int index)
    {
        return GetItem(thumbnailItems, index);
    }

    internal int GetCodedItemCount()
    {
        return codedItems.Count;
    }

    internal ParsedItem? GetCodedItem(int index)
    {
        return GetItem(codedItems, index);
    }

    internal int GetDerivedItemCount()
    {
        return derivedItems.Count;
    }

    internal ParsedItem? GetDerivedItem(int index)
    {
        return GetItem(derivedItems, index);
    }

    internal int GetMetadataItemCount()
    {
        return metadataItems.Count;
    }

    internal ParsedItem? GetMetadataItem(int index)
    {
        return GetItem(metadataItems, index);
    }

    internal int GetAuxiliaryItemCount()
    {
        return auxiliaryItems.Count;
    }

    internal ParsedItem? GetAuxiliaryItem(int index)
    {
        return GetItem(auxiliaryItems, index);
    }

    internal int GetAlphaItemCount()
    {
        return alphaItems.Count;
    }

    internal ParsedItem? GetAlphaItem(int index)
    {
        return GetItem(alphaItems, index);
    }

    internal int GetDepthItemCount()
    {
        return depthItems.Count;
    }

    internal ParsedItem? GetDepthItem(int index)
    {
        return GetItem(depthItems, index);
    }

    internal int GetArbitraryAuxiliaryItemCount()
    {
        return arbitraryAuxiliaryItems.Count;
    }

    internal ParsedItem? GetArbitraryAuxiliaryItem(int index)
    {
        return GetItem(arbitraryAuxiliaryItems, index);
    }

    internal static ParsedItem? GetItem(List<ParsedItem> items, int index)
    {
        if (index < 0 || index >= items.Count)
        {
            return null;
        }

        return items[index];
    }
}
