using System;
using System.Collections.Generic;

namespace avif;

internal class ParsedItem
{
    uint itemId;
    int itemInfoVersion;
    bool hidden;
    ushort protectionIndex;
    string itemType;
    string name;
    string contentType;
    string contentEncoding;
    string itemUriType;
    ParsedItemLocation? location;
    List<ParsedPropertyAssociation> properties;
    List<ParsedItemReference> outgoingReferences;
    List<ParsedItemReference> incomingReferences;
    List<ParsedEntityGroup> entityGroups;

    internal ParsedItem(
        uint itemId,
        int itemInfoVersion,
        bool hidden,
        ushort protectionIndex,
        string itemType,
        string name,
        string contentType,
        string contentEncoding,
        string itemUriType)
    {
        this.itemId = itemId;
        this.itemInfoVersion = itemInfoVersion;
        this.hidden = hidden;
        this.protectionIndex = protectionIndex;
        this.itemType = itemType;
        this.name = name;
        this.contentType = contentType;
        this.contentEncoding = contentEncoding;
        this.itemUriType = itemUriType;
        location = null;
        properties = new List<ParsedPropertyAssociation>();
        outgoingReferences = new List<ParsedItemReference>();
        incomingReferences = new List<ParsedItemReference>();
        entityGroups = new List<ParsedEntityGroup>();
    }

    internal uint GetItemId()
    {
        return itemId;
    }

    internal int GetItemInfoVersion()
    {
        return itemInfoVersion;
    }

    internal bool IsHidden()
    {
        return hidden;
    }

    internal ushort GetProtectionIndex()
    {
        return protectionIndex;
    }

    internal string GetItemType()
    {
        return itemType;
    }

    internal string GetName()
    {
        return name;
    }

    internal string GetContentType()
    {
        return contentType;
    }

    internal string GetContentEncoding()
    {
        return contentEncoding;
    }

    internal string GetItemUriType()
    {
        return itemUriType;
    }

    internal bool HasLocation()
    {
        return location != null;
    }

    internal ParsedItemLocation? GetLocation()
    {
        return location;
    }

    internal ParsedItem WithLocation(ParsedItemLocation itemLocation)
    {
        ParsedItem copy = new ParsedItem(
            itemId,
            itemInfoVersion,
            hidden,
            protectionIndex,
            itemType,
            name,
            contentType,
            contentEncoding,
            itemUriType);
        copy.location = itemLocation;
        copy.properties = new List<ParsedPropertyAssociation>(properties);
        copy.outgoingReferences = new List<ParsedItemReference>(outgoingReferences);
        copy.incomingReferences = new List<ParsedItemReference>(incomingReferences);
        copy.entityGroups = new List<ParsedEntityGroup>(entityGroups);
        return copy;
    }

    internal int GetPropertyCount()
    {
        return properties.Count;
    }

    internal ParsedPropertyAssociation? GetPropertyAssociation(int index)
    {
        if (index < 0 || index >= properties.Count)
        {
            return null;
        }

        return properties[index];
    }

    internal ParsedItem WithProperties(List<ParsedPropertyAssociation> itemProperties)
    {
        ParsedItem copy = new ParsedItem(
            itemId,
            itemInfoVersion,
            hidden,
            protectionIndex,
            itemType,
            name,
            contentType,
            contentEncoding,
            itemUriType);
        copy.location = location;
        copy.properties = new List<ParsedPropertyAssociation>(itemProperties);
        copy.outgoingReferences = new List<ParsedItemReference>(outgoingReferences);
        copy.incomingReferences = new List<ParsedItemReference>(incomingReferences);
        copy.entityGroups = new List<ParsedEntityGroup>(entityGroups);
        return copy;
    }

    internal int GetOutgoingReferenceCount()
    {
        return outgoingReferences.Count;
    }

    internal ParsedItemReference? GetOutgoingReference(int index)
    {
        if (index < 0 || index >= outgoingReferences.Count)
        {
            return null;
        }

        return outgoingReferences[index];
    }

    internal int GetIncomingReferenceCount()
    {
        return incomingReferences.Count;
    }

    internal ParsedItemReference? GetIncomingReference(int index)
    {
        if (index < 0 || index >= incomingReferences.Count)
        {
            return null;
        }

        return incomingReferences[index];
    }

    internal ParsedItem WithReferences(
        List<ParsedItemReference> outgoing,
        List<ParsedItemReference> incoming)
    {
        ParsedItem copy = new ParsedItem(
            itemId,
            itemInfoVersion,
            hidden,
            protectionIndex,
            itemType,
            name,
            contentType,
            contentEncoding,
            itemUriType);
        copy.location = location;
        copy.properties = new List<ParsedPropertyAssociation>(properties);
        copy.outgoingReferences = new List<ParsedItemReference>(outgoing);
        copy.incomingReferences = new List<ParsedItemReference>(incoming);
        copy.entityGroups = new List<ParsedEntityGroup>(entityGroups);
        return copy;
    }

    internal int GetEntityGroupCount()
    {
        return entityGroups.Count;
    }

    internal ParsedEntityGroup? GetEntityGroup(int index)
    {
        if (index < 0 || index >= entityGroups.Count)
        {
            return null;
        }

        return entityGroups[index];
    }

    internal ParsedItem WithEntityGroups(List<ParsedEntityGroup> groups)
    {
        ParsedItem copy = new ParsedItem(
            itemId,
            itemInfoVersion,
            hidden,
            protectionIndex,
            itemType,
            name,
            contentType,
            contentEncoding,
            itemUriType);
        copy.location = location;
        copy.properties = new List<ParsedPropertyAssociation>(properties);
        copy.outgoingReferences = new List<ParsedItemReference>(outgoingReferences);
        copy.incomingReferences = new List<ParsedItemReference>(incomingReferences);
        copy.entityGroups = new List<ParsedEntityGroup>(groups);
        return copy;
    }
}

internal class ParsedMeta
{
    string handlerType;
    string handlerName;
    bool hasPrimaryItem;
    uint primaryItemId;
    List<ParsedItem> items;
    List<ParsedItemProperty> properties;
    List<ParsedItemReference> references;
    List<ParsedDataReference> dataReferences;
    List<ParsedEntityGroup> entityGroups;
    ParsedDataBox? itemData;
    List<ParsedDataBox> mediaData;
    ParsedItemDiscovery discovery;
    ParsedItemDependencyGraph? dependencyGraph;
    ParsedItemPayloadResolver payloadResolver;

    internal ParsedMeta(
        string handlerType,
        string handlerName,
        bool hasPrimaryItem,
        uint primaryItemId,
        List<ParsedItem> items,
        List<ParsedItemProperty> properties,
        List<ParsedItemReference> references,
        List<ParsedDataReference> dataReferences,
        List<ParsedEntityGroup> entityGroups,
        ParsedDataBox? itemData,
        List<ParsedDataBox> mediaData,
        ParsedItemDependencyGraph? dependencyGraph,
        AvifSecurityLimits limits)
    {
        this.handlerType = handlerType;
        this.handlerName = handlerName;
        this.hasPrimaryItem = hasPrimaryItem;
        this.primaryItemId = primaryItemId;
        this.items = new List<ParsedItem>(items);
        this.properties = new List<ParsedItemProperty>(properties);
        this.references = new List<ParsedItemReference>(references);
        this.dataReferences = new List<ParsedDataReference>(dataReferences);
        this.entityGroups = new List<ParsedEntityGroup>(entityGroups);
        this.itemData = itemData;
        this.mediaData = new List<ParsedDataBox>(mediaData);
        this.dependencyGraph = dependencyGraph;
        discovery = ParsedItemDiscovery.Create(
            this.items,
            hasPrimaryItem,
            primaryItemId);
        payloadResolver = new ParsedItemPayloadResolver(
            this.items,
            limits.MaxTemporaryBytes,
            limits.MaxReferenceCount);
    }

    internal string GetHandlerType()
    {
        return handlerType;
    }

    internal string GetHandlerName()
    {
        return handlerName;
    }

    internal bool HasPrimaryItem()
    {
        return hasPrimaryItem;
    }

    internal uint GetPrimaryItemId()
    {
        return primaryItemId;
    }

    internal int GetItemCount()
    {
        return items.Count;
    }

    internal ParsedItem? GetItem(int index)
    {
        if (index < 0 || index >= items.Count)
        {
            return null;
        }

        return items[index];
    }

    internal ParsedItem? FindItem(uint itemId)
    {
        int index = 0;
        while (index < items.Count)
        {
            ParsedItem item = items[index];
            if (item.GetItemId() == itemId)
            {
                return item;
            }

            index++;
        }

        return null;
    }

    internal ParsedItem? GetPrimaryItem()
    {
        if (!hasPrimaryItem)
        {
            return null;
        }

        return FindItem(primaryItemId);
    }

    internal ParsedItemDiscovery GetItemDiscovery()
    {
        return discovery;
    }

    internal ParsedItemDependencyGraph? GetItemDependencyGraph()
    {
        return dependencyGraph;
    }

    internal ParsedItemPayloadResolver GetItemPayloadResolver()
    {
        return payloadResolver;
    }

    internal ItemPayloadResolveResult ResolveItemPayload(uint itemId)
    {
        return payloadResolver.Resolve(itemId);
    }

    internal EncodedItemExtractionResult ExtractEncodedItem(
        ReadOnlySpan<byte> source,
        uint itemId)
    {
        return payloadResolver.Extract(source, itemId);
    }

    internal int GetPropertyCount()
    {
        return properties.Count;
    }

    internal ParsedItemProperty? GetProperty(int index)
    {
        if (index < 0 || index >= properties.Count)
        {
            return null;
        }

        return properties[index];
    }

    internal int GetReferenceCount()
    {
        return references.Count;
    }

    internal ParsedItemReference? GetReference(int index)
    {
        if (index < 0 || index >= references.Count)
        {
            return null;
        }

        return references[index];
    }

    internal int GetDataReferenceCount()
    {
        return dataReferences.Count;
    }

    internal ParsedDataReference? GetDataReference(int index)
    {
        if (index < 0 || index >= dataReferences.Count)
        {
            return null;
        }

        return dataReferences[index];
    }

    internal int GetEntityGroupCount()
    {
        return entityGroups.Count;
    }

    internal ParsedEntityGroup? GetEntityGroup(int index)
    {
        if (index < 0 || index >= entityGroups.Count)
        {
            return null;
        }

        return entityGroups[index];
    }

    internal ParsedEntityGroup? FindEntityGroup(uint groupId)
    {
        int index = 0;
        while (index < entityGroups.Count)
        {
            if (entityGroups[index].GetGroupId() == groupId)
            {
                return entityGroups[index];
            }

            index++;
        }

        return null;
    }

    internal bool HasItemDataBox()
    {
        return itemData != null;
    }

    internal ParsedDataBox? GetItemDataBox()
    {
        return itemData;
    }

    internal int GetMediaDataBoxCount()
    {
        return mediaData.Count;
    }

    internal ParsedDataBox? GetMediaDataBox(int index)
    {
        if (index < 0 || index >= mediaData.Count)
        {
            return null;
        }

        return mediaData[index];
    }
}
