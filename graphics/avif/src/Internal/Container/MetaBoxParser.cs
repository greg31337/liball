using System;
using System.Collections.Generic;

namespace avif;

internal class MetaParseResult
{
    internal AvifResultCode Code;
    internal ParsedMeta? Meta;
    internal IsoBoxParseResult? Boxes;

    internal MetaParseResult(
        AvifResultCode code,
        ParsedMeta? meta,
        IsoBoxParseResult? boxes)
    {
        Code = code;
        Meta = meta;
        Boxes = boxes;
    }
}

internal static class MetaBoxParser
{
    internal static MetaParseResult Parse(
        ReadOnlySpan<byte> source,
        AvifSecurityLimits limits)
    {
        IsoBoxParseResult boxes = IsoBoxReader.Parse(source, limits, false);
        if (boxes.Code != AvifResultCode.Success)
        {
            return new MetaParseResult(boxes.Code, null, boxes);
        }

        IsoBox? metaBox = null;
        IsoBox? miniBox = null;
        IsoBox? fileTypeBox = null;
        int fileTypeCount = 0;
        int index = 0;
        while (index < boxes.Boxes.Count)
        {
            IsoBox candidate = boxes.Boxes[index];
            if (candidate.Type == "meta")
            {
                if (metaBox != null)
                {
                    Report(
                        boxes.Diagnostics,
                        AvifResultCode.InvalidContainer,
                        "The file contains more than one top-level MetaBox.",
                        candidate.Offset);
                    return new MetaParseResult(
                        AvifResultCode.InvalidContainer,
                        null,
                        boxes);
                }

                metaBox = candidate;
            }
            else if (candidate.Type == "mini")
            {
                if (miniBox != null)
                {
                    Report(
                        boxes.Diagnostics,
                        AvifResultCode.InvalidContainer,
                        "The file contains more than one top-level MinimizedImageBox.",
                        candidate.Offset);
                    return new MetaParseResult(
                        AvifResultCode.InvalidContainer,
                        null,
                        boxes);
                }

                miniBox = candidate;
            }
            else if (candidate.Type == "ftyp")
            {
                fileTypeCount++;
                if (fileTypeBox == null)
                {
                    fileTypeBox = candidate;
                }
            }

            index++;
        }

        if (metaBox != null && miniBox != null)
        {
            Report(
                boxes.Diagnostics,
                AvifResultCode.InvalidContainer,
                "A file cannot contain both MetaBox and MinimizedImageBox representations.",
                miniBox.Offset);
            return new MetaParseResult(AvifResultCode.InvalidContainer, null, boxes);
        }

        if (miniBox != null)
        {
            if (fileTypeBox == null || fileTypeCount != 1)
            {
                return new MetaParseResult(AvifResultCode.InvalidContainer, null, boxes);
            }

            FileTypeBox fileType = FileTypeBox.Parse(
                source,
                fileTypeBox,
                boxes.Resources);
            return MinimizedImageBox.ParseAndExpand(source, miniBox, boxes, fileType);
        }

        if (metaBox == null)
        {
            return new MetaParseResult(AvifResultCode.NotFound, null, boxes);
        }

        return ParseMeta(source, metaBox, boxes);
    }

    internal static MetaParseResult ParseMeta(
        ReadOnlySpan<byte> source,
        IsoBox? metaBox,
        IsoBoxParseResult boxes)
    {
        if (metaBox == null || metaBox.Type != "meta" || boxes == null)
        {
            return new MetaParseResult(AvifResultCode.InvalidArgument, null, boxes);
        }

        boxes.Diagnostics.Context.PushBox("meta");
        if (!metaBox.HasFullBoxFields || metaBox.Version != 0)
        {
            AvifResultCode versionCode = metaBox.HasFullBoxFields
                ? AvifResultCode.UnsupportedFeature
                : AvifResultCode.InvalidContainer;
            Report(
                boxes.Diagnostics,
                versionCode,
                "The MetaBox must use supported FullBox syntax.",
                metaBox.PayloadOffset);
            boxes.Diagnostics.Context.PopBox();
            return new MetaParseResult(versionCode, null, boxes);
        }

        if (metaBox.Flags != 0)
        {
            Report(
                boxes.Diagnostics,
                AvifResultCode.InvalidContainer,
                "The MetaBox flags must be zero.",
                metaBox.PayloadOffset);
            boxes.Diagnostics.Context.PopBox();
            return new MetaParseResult(AvifResultCode.InvalidContainer, null, boxes);
        }

        HandlerBox? handler = null;
        PrimaryItemBox? primaryItem = null;
        ItemInfoBoxParseResult? itemInfo = null;
        ItemLocationBoxParseResult? itemLocations = null;
        ItemPropertiesBoxParseResult? itemProperties = null;
        ItemReferenceBoxParseResult? itemReferences = null;
        DataReferenceBoxParseResult? dataInformation = null;
        EntityGroupBoxParseResult? entityGroups = null;
        IsoBox? itemDataBox = null;
        int childIndex = 0;
        while (childIndex < metaBox.Children.Count)
        {
            IsoBox child = metaBox.Children[childIndex];
            if (child.Type == "hdlr")
            {
                if (handler != null)
                {
                    return FailChild(
                        boxes,
                        child,
                        AvifResultCode.InvalidContainer,
                        "The MetaBox contains duplicate HandlerBox children.");
                }

                handler = HandlerBox.Parse(source, child);
                if (handler.Code != AvifResultCode.Success)
                {
                    return FailChild(
                        boxes,
                        child,
                        handler.Code,
                        "The HandlerBox is malformed or unsupported.");
                }
            }
            else if (child.Type == "pitm")
            {
                if (primaryItem != null)
                {
                    return FailChild(
                        boxes,
                        child,
                        AvifResultCode.InvalidContainer,
                        "The MetaBox contains duplicate PrimaryItemBox children.");
                }

                primaryItem = PrimaryItemBox.Parse(source, child);
                if (primaryItem.Code != AvifResultCode.Success)
                {
                    return FailChild(
                        boxes,
                        child,
                        primaryItem.Code,
                        "The PrimaryItemBox is malformed or unsupported.");
                }
            }
            else if (child.Type == "iinf")
            {
                if (itemInfo != null)
                {
                    return FailChild(
                        boxes,
                        child,
                        AvifResultCode.InvalidContainer,
                        "The MetaBox contains duplicate ItemInfoBox children.");
                }

                itemInfo = ItemInfoBox.Parse(source, child, boxes.Resources);
                if (itemInfo.Code != AvifResultCode.Success)
                {
                    return FailChild(
                        boxes,
                        child,
                        itemInfo.Code,
                        "The ItemInfoBox or one of its entries is malformed or unsupported.");
                }
            }
            else if (child.Type == "iloc")
            {
                if (itemLocations != null)
                {
                    return FailChild(
                        boxes,
                        child,
                        AvifResultCode.InvalidContainer,
                        "The MetaBox contains duplicate ItemLocationBox children.");
                }

                itemLocations = ItemLocationBox.Parse(
                    source,
                    child,
                    metaBox,
                    boxes.Resources);
                if (itemLocations.Code != AvifResultCode.Success)
                {
                    return FailChild(
                        boxes,
                        child,
                        itemLocations.Code,
                        "The ItemLocationBox or one of its extents is malformed or unsupported.");
                }
            }
            else if (child.Type == "iprp")
            {
                if (itemProperties != null)
                {
                    return FailChild(
                        boxes,
                        child,
                        AvifResultCode.InvalidContainer,
                        "The MetaBox contains duplicate ItemPropertiesBox children.");
                }

                itemProperties = ItemPropertiesBox.Parse(
                    source,
                    child,
                    boxes.Resources);
                if (itemProperties.Code != AvifResultCode.Success)
                {
                    return FailChild(
                        boxes,
                        child,
                        itemProperties.Code,
                        "The ItemPropertiesBox or one of its associations is malformed or unsupported.");
                }
            }
            else if (child.Type == "iref")
            {
                if (itemReferences != null)
                {
                    return FailChild(
                        boxes,
                        child,
                        AvifResultCode.InvalidContainer,
                        "The MetaBox contains duplicate ItemReferenceBox children.");
                }

                itemReferences = ItemReferenceBox.Parse(
                    source,
                    child,
                    boxes.Resources);
                if (itemReferences.Code != AvifResultCode.Success)
                {
                    return FailChild(
                        boxes,
                        child,
                        itemReferences.Code,
                        "The ItemReferenceBox or one of its typed references is malformed or unsupported.");
                }
            }
            else if (child.Type == "dinf")
            {
                if (dataInformation != null)
                {
                    return FailChild(
                        boxes,
                        child,
                        AvifResultCode.InvalidContainer,
                        "The MetaBox contains duplicate DataInformationBox children.");
                }

                dataInformation = DataInformationBox.Parse(
                    source,
                    child,
                    boxes.Resources);
                if (dataInformation.Code != AvifResultCode.Success)
                {
                    return FailChild(
                        boxes,
                        child,
                        dataInformation.Code,
                        "The DataInformationBox or its data references are malformed or unsupported.");
                }
            }
            else if (child.Type == "grpl")
            {
                if (entityGroups != null)
                {
                    return FailChild(
                        boxes,
                        child,
                        AvifResultCode.InvalidContainer,
                        "The MetaBox contains duplicate GroupsListBox children.");
                }

                entityGroups = EntityGroupBox.Parse(
                    source,
                    child,
                    boxes.Resources);
                if (entityGroups.Code != AvifResultCode.Success)
                {
                    return FailChild(
                        boxes,
                        child,
                        entityGroups.Code,
                        "The GroupsListBox or one of its entity groups is malformed or unsupported.");
                }
            }
            else if (child.Type == "idat")
            {
                if (itemDataBox != null)
                {
                    return FailChild(
                        boxes,
                        child,
                        AvifResultCode.InvalidContainer,
                        "The MetaBox contains duplicate ItemDataBox children.");
                }

                itemDataBox = child;
            }

            childIndex++;
        }

        if (handler == null)
        {
            Report(
                boxes.Diagnostics,
                AvifResultCode.InvalidContainer,
                "The MetaBox does not contain a HandlerBox.",
                metaBox.PayloadOffset);
            boxes.Diagnostics.Context.PopBox();
            return new MetaParseResult(AvifResultCode.InvalidContainer, null, boxes);
        }

        List<ParsedItem> items = itemInfo == null
            ? new List<ParsedItem>()
            : itemInfo.Items;
        List<ParsedDataReference> dataReferences = dataInformation == null
            ? new List<ParsedDataReference>()
            : dataInformation.References;
        if (itemLocations != null)
        {
            AvifResultCode resolveCode = ItemLocationBox.ResolveDataReferences(
                itemLocations.Locations,
                dataReferences,
                source.Length);
            if (resolveCode != AvifResultCode.Success)
            {
                IsoBox? ilocBox = FindChild(metaBox, "iloc");
                return FailChild(
                    boxes,
                    ilocBox,
                    resolveCode,
                    "An ItemLocationBox data reference is missing, invalid, or out of bounds.");
            }
        }

        if (itemLocations != null)
        {
            int locationIndex = 0;
            while (locationIndex < itemLocations.Locations.Count)
            {
                ParsedItemLocation location = itemLocations.Locations[locationIndex];
                int parsedItemIndex = FindItemIndex(items, location.GetItemId());
                if (parsedItemIndex < 0)
                {
                    IsoBox? ilocBox = FindChild(metaBox, "iloc");
                    return FailChild(
                        boxes,
                        ilocBox,
                        AvifResultCode.InvalidItem,
                        "An ItemLocationBox entry identifies an undeclared item.");
                }

                items[parsedItemIndex] = items[parsedItemIndex].WithLocation(location);
                locationIndex++;
            }
        }

        if (itemProperties != null)
        {
            int associationIndex = 0;
            while (associationIndex < itemProperties.Items.Count)
            {
                ParsedItemAssociationSet associations = itemProperties.Items[associationIndex];
                int parsedItemIndex = FindItemIndex(items, associations.ItemId);
                if (parsedItemIndex < 0)
                {
                    IsoBox? iprpBox = FindChild(metaBox, "iprp");
                    return FailChild(
                        boxes,
                        iprpBox,
                        AvifResultCode.InvalidItem,
                        "An ItemPropertyAssociationBox entry identifies an undeclared item.");
                }

                items[parsedItemIndex] = items[parsedItemIndex].WithProperties(
                    associations.Associations);
                AvifResultCode consistencyCode = ValidateAv1PropertyConsistency(
                    items[parsedItemIndex]);
                if (consistencyCode == AvifResultCode.Success)
                {
                    consistencyCode = ValidateHdrPropertyConsistency(
                        items[parsedItemIndex]);
                }
                if (consistencyCode != AvifResultCode.Success)
                {
                    IsoBox? iprpBox = FindChild(metaBox, "iprp");
                    return FailChild(
                        boxes,
                        iprpBox,
                        consistencyCode,
                        "Associated image metadata properties do not agree.");
                }

                associationIndex++;
            }
        }

        List<ParsedItemReference> references = itemReferences == null
            ? new List<ParsedItemReference>()
            : itemReferences.References;
        if (references.Count > 0)
        {
            Dictionary<uint, int> itemIndices = new Dictionary<uint, int>();
            List<List<ParsedItemReference>> outgoing = new List<List<ParsedItemReference>>();
            List<List<ParsedItemReference>> incoming = new List<List<ParsedItemReference>>();
            int itemIndex = 0;
            while (itemIndex < items.Count)
            {
                itemIndices.Add(items[itemIndex].GetItemId(), itemIndex);
                outgoing.Add(new List<ParsedItemReference>());
                incoming.Add(new List<ParsedItemReference>());
                itemIndex++;
            }

            IsoBox? irefBox = FindChild(metaBox, "iref");
            int referenceIndex = 0;
            while (referenceIndex < references.Count)
            {
                ParsedItemReference reference = references[referenceIndex];
                uint fromItemId = reference.GetFromItemId();
                if (!itemIndices.ContainsKey(fromItemId))
                {
                    return FailChild(
                        boxes,
                        irefBox,
                        AvifResultCode.InvalidItem,
                        "An ItemReferenceBox source identifies an undeclared item.");
                }

                outgoing[itemIndices[fromItemId]].Add(reference);
                int targetIndex = 0;
                while (targetIndex < reference.GetTargetCount())
                {
                    uint toItemId = reference.GetTargetItemId(targetIndex);
                    if (!itemIndices.ContainsKey(toItemId))
                    {
                        return FailChild(
                            boxes,
                            irefBox,
                            AvifResultCode.InvalidItem,
                            "An ItemReferenceBox target identifies an undeclared item.");
                    }

                    incoming[itemIndices[toItemId]].Add(reference);
                    targetIndex++;
                }

                referenceIndex++;
            }

            itemIndex = 0;
            while (itemIndex < items.Count)
            {
                items[itemIndex] = items[itemIndex].WithReferences(
                    outgoing[itemIndex],
                    incoming[itemIndex]);
                itemIndex++;
            }
        }

        List<ParsedEntityGroup> groups = entityGroups == null
            ? new List<ParsedEntityGroup>()
            : entityGroups.Groups;
        if (groups.Count > 0)
        {
            Dictionary<uint, int> itemIndices = new Dictionary<uint, int>();
            List<List<ParsedEntityGroup>> itemGroups = new List<List<ParsedEntityGroup>>();
            int itemIndex = 0;
            while (itemIndex < items.Count)
            {
                itemIndices.Add(items[itemIndex].GetItemId(), itemIndex);
                itemGroups.Add(new List<ParsedEntityGroup>());
                itemIndex++;
            }

            IsoBox? grplBox = FindChild(metaBox, "grpl");
            int groupIndex = 0;
            while (groupIndex < groups.Count)
            {
                ParsedEntityGroup group = groups[groupIndex];
                if (itemIndices.ContainsKey(group.GetGroupId()))
                {
                    return FailChild(
                        boxes,
                        grplBox,
                        AvifResultCode.InvalidItem,
                        "An entity group ID conflicts with a declared item ID.");
                }

                int entityIndex = 0;
                while (entityIndex < group.GetEntityCount())
                {
                    uint entityId = group.GetEntityId(entityIndex);
                    if (!itemIndices.ContainsKey(entityId)
                        && group.GetKind() == ParsedEntityGroupKind.Stereo)
                    {
                        return FailChild(
                            boxes,
                            grplBox,
                            AvifResultCode.InvalidItem,
                            "A stereo entity group identifies an undeclared image item.");
                    }

                    entityIndex++;
                }

                groupIndex++;
            }

            groupIndex = 0;
            while (groupIndex < groups.Count)
            {
                ParsedEntityGroup group = groups[groupIndex];
                int entityIndex = 0;
                while (entityIndex < group.GetEntityCount())
                {
                    uint entityId = group.GetEntityId(entityIndex);
                    if (itemIndices.ContainsKey(entityId))
                    {
                        itemGroups[itemIndices[entityId]].Add(group);
                    }

                    entityIndex++;
                }

                groupIndex++;
            }

            itemIndex = 0;
            while (itemIndex < items.Count)
            {
                items[itemIndex] = items[itemIndex].WithEntityGroups(itemGroups[itemIndex]);
                itemIndex++;
            }

            groupIndex = 0;
            while (groupIndex < groups.Count)
            {
                ParsedEntityGroup group = groups[groupIndex];
                List<ParsedItem?> resolvedItems = new List<ParsedItem?>();
                int entityIndex = 0;
                while (entityIndex < group.GetEntityCount())
                {
                    uint entityId = group.GetEntityId(entityIndex);
                    ParsedItem? resolvedItem = null;
                    if (itemIndices.ContainsKey(entityId))
                    {
                        resolvedItem = items[itemIndices[entityId]];
                    }

                    resolvedItems.Add(resolvedItem);
                    entityIndex++;
                }

                group.ResolveItems(resolvedItems);
                groupIndex++;
            }
        }

        bool hasPrimaryItem = primaryItem != null;
        uint primaryItemId = hasPrimaryItem ? (primaryItem ?? throw new InvalidOperationException("Invalid internal state.")).ItemId : 0;
        if (hasPrimaryItem && !ContainsItem(items, primaryItemId))
        {
            boxes.Diagnostics.Context.PushBox("pitm");
            Report(
                boxes.Diagnostics,
                AvifResultCode.InvalidItem,
                "The primary item ID does not identify an ItemInfoEntry.",
                metaBox.PayloadOffset);
            boxes.Diagnostics.Context.PopBox();
            boxes.Diagnostics.Context.PopBox();
            return new MetaParseResult(AvifResultCode.InvalidItem, null, boxes);
        }

        ItemDependencyGraphCreateResult dependencyResult =
            ParsedItemDependencyGraph.Create(items, boxes.Resources.Limits);
        if (dependencyResult.Code != AvifResultCode.Success)
        {
            IsoBox? dependencyBox = FindChild(metaBox, "iref");
            if (dependencyBox != null)
            {
                return FailChild(
                    boxes,
                    dependencyBox,
                    dependencyResult.Code,
                    "The item dependency graph contains a cycle or exceeds a configured limit.");
            }

            Report(
                boxes.Diagnostics,
                dependencyResult.Code,
                "The item dependency graph is invalid or exceeds a configured limit.",
                metaBox.PayloadOffset);
            boxes.Diagnostics.Context.PopBox();
            return new MetaParseResult(dependencyResult.Code, null, boxes);
        }

        ParsedMeta meta = new ParsedMeta(
            handler.HandlerType,
            handler.Name,
            hasPrimaryItem,
            primaryItemId,
            items,
            itemProperties == null
                ? new List<ParsedItemProperty>()
                : itemProperties.Properties,
            references,
            dataReferences,
            groups,
            itemDataBox == null
                ? null
                : new ParsedDataBox(
                    itemDataBox.Type,
                    itemDataBox.PayloadOffset,
                    itemDataBox.PayloadSize),
            GetMediaDataBoxes(boxes.Boxes),
            dependencyResult.Graph,
            boxes.Resources.Limits);
        boxes.Diagnostics.Context.PopBox();
        return new MetaParseResult(AvifResultCode.Success, meta, boxes);
    }

    internal static bool ContainsItem(List<ParsedItem> items, uint itemId)
    {
        int index = 0;
        while (index < items.Count)
        {
            if (items[index].GetItemId() == itemId)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static AvifResultCode ValidateAv1PropertyConsistency(ParsedItem item)
    {
        if (item == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        int configurationIndex = 0;
        while (configurationIndex < item.GetPropertyCount())
        {
            ParsedItemProperty configurationProperty = (item
                .GetPropertyAssociation(configurationIndex) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetProperty();
            ParsedAv1Configuration? configuration = configurationProperty.GetAv1Configuration();
            if (configuration != null)
            {
                int propertyIndex = 0;
                while (propertyIndex < item.GetPropertyCount())
                {
                    ParsedItemProperty associatedProperty = (item
                        .GetPropertyAssociation(propertyIndex) ?? throw new InvalidOperationException("Invalid internal state."))
                        .GetProperty();
                    ParsedPixelInformation? information = associatedProperty
                        .GetPixelInformation();
                    if (information != null)
                    {
                        AvifResultCode code = configuration.ReconcilePixelInformation(information);
                        if (code != AvifResultCode.Success)
                        {
                            return code;
                        }
                    }

                    ParsedColourInformation? colour = associatedProperty.GetColourInformation();
                    if (colour != null)
                    {
                        AvifResultCode code = configuration.ReconcileColourInformation(colour);
                        if (code != AvifResultCode.Success)
                        {
                            return code;
                        }
                    }

                    ParsedOperatingPointSelector? selector = associatedProperty
                        .GetOperatingPointSelector();
                    if (selector != null)
                    {
                        AvifResultCode code = configuration
                            .ReconcileOperatingPointSelector(selector);
                        if (code != AvifResultCode.Success)
                        {
                            return code;
                        }
                    }

                    propertyIndex++;
                }
            }

            configurationIndex++;
        }

        return AvifResultCode.Success;
    }

    internal static AvifResultCode ValidateHdrPropertyConsistency(ParsedItem item)
    {
        if (item == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        AvifHdrMetadata metadata = new AvifHdrMetadata();
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            ParsedItemProperty property = (item
                .GetPropertyAssociation(index) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetProperty();
            AvifResultCode code = MetadataResolver.Merge(
                metadata,
                HdrMetadataParser.ParseContainerProperty(property));
            if (code != AvifResultCode.Success)
            {
                return code;
            }
            index++;
        }
        return AvifResultCode.Success;
    }

    internal static int FindItemIndex(List<ParsedItem> items, uint itemId)
    {
        int index = 0;
        while (index < items.Count)
        {
            if (items[index].GetItemId() == itemId)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal static IsoBox? FindChild(IsoBox parent, string type)
    {
        int index = 0;
        while (index < parent.Children.Count)
        {
            if (parent.Children[index].Type == type)
            {
                return parent.Children[index];
            }

            index++;
        }

        return null;
    }

    internal static List<ParsedDataBox> GetMediaDataBoxes(List<IsoBox> boxes)
    {
        List<ParsedDataBox> mediaData = new List<ParsedDataBox>();
        int index = 0;
        while (index < boxes.Count)
        {
            IsoBox box = boxes[index];
            if (box.Type == "mdat")
            {
                mediaData.Add(new ParsedDataBox(
                    box.Type,
                    box.PayloadOffset,
                    box.PayloadSize));
            }

            index++;
        }

        return mediaData;
    }

    internal static MetaParseResult FailChild(
        IsoBoxParseResult boxes,
        IsoBox? child,
        AvifResultCode code,
        string message)
    {
        boxes.Diagnostics.Context.PushBox(IsoBoxReader.GetPathSegment((child ?? throw new InvalidOperationException("Invalid internal state.")).Type));
        Report(boxes.Diagnostics, code, message, child.PayloadOffset);
        boxes.Diagnostics.Context.PopBox();
        boxes.Diagnostics.Context.PopBox();
        return new MetaParseResult(code, null, boxes);
    }

    internal static void Report(
        DiagnosticLog diagnostics,
        AvifResultCode code,
        string message,
        long offset)
    {
        AvifDiagnosticCategory category;
        if (code == AvifResultCode.LimitExceeded)
        {
            category = AvifDiagnosticCategory.Resource;
        }
        else if (code == AvifResultCode.InvalidItem)
        {
            category = AvifDiagnosticCategory.Item;
        }
        else if (code == AvifResultCode.InvalidAv1Bitstream)
        {
            category = AvifDiagnosticCategory.Av1;
        }
        else if (code == AvifResultCode.NonConformingData)
        {
            category = AvifDiagnosticCategory.Validation;
        }
        else
        {
            category = AvifDiagnosticCategory.Container;
        }

        IsoBoxReader.Report(diagnostics, code, category, message, offset);
    }
}
