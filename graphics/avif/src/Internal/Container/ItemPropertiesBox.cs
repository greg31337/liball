using System;
using System.Collections.Generic;

namespace avif;

internal class ParsedItemAssociationSet
{
    internal uint ItemId;
    internal List<ParsedPropertyAssociation> Associations;

    internal ParsedItemAssociationSet(
        uint itemId,
        List<ParsedPropertyAssociation> associations)
    {
        ItemId = itemId;
        Associations = associations;
    }
}

internal class ItemPropertyAssociationBoxParseResult
{
    internal AvifResultCode Code;
    internal int Version;
    internal int Flags;
    internal List<ParsedItemAssociationSet> Items;

    internal ItemPropertyAssociationBoxParseResult(
        AvifResultCode code,
        int version,
        int flags,
        List<ParsedItemAssociationSet> items)
    {
        Code = code;
        Version = version;
        Flags = flags;
        Items = items;
    }
}

internal class ItemPropertiesBoxParseResult
{
    internal AvifResultCode Code;
    internal List<ParsedItemProperty> Properties;
    internal List<ParsedItemAssociationSet> Items;

    internal ItemPropertiesBoxParseResult(
        AvifResultCode code,
        List<ParsedItemProperty> properties,
        List<ParsedItemAssociationSet> items)
    {
        Code = code;
        Properties = properties;
        Items = items;
    }
}

internal class ItemPropertyVersionProbe
{
    internal AvifResultCode Code;
    internal bool Supported;

    internal ItemPropertyVersionProbe(AvifResultCode code, bool supported)
    {
        Code = code;
        Supported = supported;
    }
}

internal static class ItemPropertiesBox
{
    internal static ItemPropertiesBoxParseResult Parse(
        ReadOnlySpan<byte> source,
        IsoBox box,
        ResourceGuard resources)
    {
        List<ParsedItemProperty> properties = new List<ParsedItemProperty>();
        List<ParsedItemAssociationSet> items = new List<ParsedItemAssociationSet>();
        if (box == null || box.Type != "iprp" || resources == null)
        {
            return new ItemPropertiesBoxParseResult(
                AvifResultCode.InvalidArgument,
                properties,
                items);
        }

        IsoBox? propertyContainer = null;
        List<IsoBox> associationBoxes = new List<IsoBox>();
        bool associationSeen = false;
        int childIndex = 0;
        while (childIndex < box.Children.Count)
        {
            IsoBox child = box.Children[childIndex];
            if (child.Type == "free" || child.Type == "skip")
            {
                childIndex++;
                continue;
            }

            if (child.Type == "ipco")
            {
                if (propertyContainer != null || associationSeen)
                {
                    return new ItemPropertiesBoxParseResult(
                        AvifResultCode.InvalidContainer,
                        properties,
                        items);
                }

                propertyContainer = child;
            }
            else if (child.Type == "ipma")
            {
                associationSeen = true;
                associationBoxes.Add(child);
            }
            else
            {
                return new ItemPropertiesBoxParseResult(
                    AvifResultCode.InvalidContainer,
                    properties,
                    items);
            }

            childIndex++;
        }

        if (propertyContainer == null)
        {
            return new ItemPropertiesBoxParseResult(
                AvifResultCode.InvalidContainer,
                properties,
                items);
        }

        AvifResultCode propertyCode = ParsePropertyContainer(
            source,
            propertyContainer,
            resources,
            properties);
        if (propertyCode != AvifResultCode.Success)
        {
            return new ItemPropertiesBoxParseResult(propertyCode, properties, items);
        }

        HashSet<int> versionFlagPairs = new HashSet<int>();
        HashSet<uint> associatedItemIds = new HashSet<uint>();
        int associationBoxIndex = 0;
        while (associationBoxIndex < associationBoxes.Count)
        {
            IsoBox associationBox = associationBoxes[associationBoxIndex];
            ItemPropertyAssociationBoxParseResult parsed = ParseAssociations(
                source,
                associationBox,
                properties,
                resources);
            if (parsed.Code != AvifResultCode.Success)
            {
                return new ItemPropertiesBoxParseResult(parsed.Code, properties, items);
            }

            int pair = parsed.Version * 2 + parsed.Flags;
            if (!versionFlagPairs.Add(pair))
            {
                return new ItemPropertiesBoxParseResult(
                    AvifResultCode.InvalidContainer,
                    properties,
                    items);
            }

            int itemIndex = 0;
            while (itemIndex < parsed.Items.Count)
            {
                ParsedItemAssociationSet item = parsed.Items[itemIndex];
                if (!associatedItemIds.Add(item.ItemId))
                {
                    return new ItemPropertiesBoxParseResult(
                        AvifResultCode.InvalidItem,
                        properties,
                        items);
                }

                items.Add(item);
                itemIndex++;
            }

            associationBoxIndex++;
        }

        return new ItemPropertiesBoxParseResult(
            AvifResultCode.Success,
            properties,
            items);
    }

    internal static AvifResultCode ParsePropertyContainer(
        ReadOnlySpan<byte> source,
        IsoBox box,
        ResourceGuard resources,
        List<ParsedItemProperty> properties)
    {
        int propertyCount = 0;
        int childIndex = 0;
        while (childIndex < box.Children.Count)
        {
            string type = box.Children[childIndex].Type;
            if (type != "free" && type != "skip")
            {
                propertyCount++;
            }

            childIndex++;
        }

        if (propertyCount > 32767 || propertyCount > resources.Limits.MaxPropertyCount)
        {
            return AvifResultCode.LimitExceeded;
        }

        AvifResultCode countCode = resources.CountProperties(propertyCount);
        if (countCode != AvifResultCode.Success)
        {
            return countCode;
        }

        childIndex = 0;
        int propertyIndex = 1;
        while (childIndex < box.Children.Count)
        {
            IsoBox child = box.Children[childIndex];
            if (child.Type == "free" || child.Type == "skip")
            {
                childIndex++;
                continue;
            }

            CheckedRangeResult range = CheckedMath.ResolveRange(
                child.Offset,
                0,
                child.Size,
                source.Length);
            if (!range.Success)
            {
                return range.Code;
            }

            ReadOnlySpan<byte> raw = source.Slice((int)child.Offset, (int)child.Size);
            ParsedItemProperty? property = FindDuplicate(properties, raw);
            if (property == null)
            {
                ItemPropertyVersionProbe version = ProbeVersion(source, child);
                if (version.Code != AvifResultCode.Success)
                {
                    return version.Code;
                }

                ParsedAv1Configuration? av1Configuration = null;
                ParsedImageSpatialExtents? spatialExtents = null;
                ParsedPixelInformation? pixelInformation = null;
                AdditionalItemPropertyParseResult? additionalProperties = null;
                HdrCameraPropertyParseResult? hdrCameraProperties = null;
                if (child.Type == "av1C")
                {
                    Av1ItemConfigurationParseResult parsed = Av1ItemConfigurationBox.Parse(
                        source,
                        child);
                    if (parsed.Code != AvifResultCode.Success)
                    {
                        return parsed.Code;
                    }

                    av1Configuration = parsed.Configuration;
                    AvifResultCode typedReserveCode = resources.ReserveTemporaryBytes(
                        (av1Configuration ?? throw new InvalidOperationException("Invalid internal state.")).GetRetainedByteCount());
                    if (typedReserveCode != AvifResultCode.Success)
                    {
                        return typedReserveCode;
                    }
                }
                else if (child.Type == "ispe" && version.Supported)
                {
                    ImageSpatialExtentsParseResult parsed = ImageSpatialExtentsProperty.Parse(
                        source,
                        child,
                        resources);
                    if (parsed.Code != AvifResultCode.Success)
                    {
                        return parsed.Code;
                    }

                    spatialExtents = parsed.Extents;
                }
                else if (child.Type == "pixi" && version.Supported)
                {
                    PixelInformationParseResult parsed = PixelInformationProperty.Parse(
                        source,
                        child);
                    if (parsed.Code != AvifResultCode.Success)
                    {
                        return parsed.Code;
                    }

                    pixelInformation = parsed.Information;
                }
                else if (AdditionalItemPropertyBox.IsSupportedType(child.Type)
                    && version.Supported)
                {
                    additionalProperties = AdditionalItemPropertyBox.Parse(
                        source,
                        child,
                        resources);
                    if (additionalProperties.Code != AvifResultCode.Success)
                    {
                        return additionalProperties.Code;
                    }

                    long retainedBytes = 0;
                    if (additionalProperties.ColourInformation != null)
                    {
                        retainedBytes += additionalProperties
                            .ColourInformation
                            .GetRetainedByteCount();
                    }

                    if (additionalProperties.AuxiliaryType != null)
                    {
                        retainedBytes += additionalProperties
                            .AuxiliaryType
                            .GetRetainedByteCount();
                    }

                    AvifResultCode typedReserveCode = resources.ReserveTemporaryBytes(
                        retainedBytes);
                    if (typedReserveCode != AvifResultCode.Success)
                    {
                        return typedReserveCode;
                    }
                }
                else if (HdrCameraPropertyBox.IsSupportedType(child.Type)
                    && version.Supported)
                {
                    hdrCameraProperties = HdrCameraPropertyBox.Parse(source, child);
                    if (hdrCameraProperties.Code != AvifResultCode.Success)
                    {
                        return hdrCameraProperties.Code;
                    }
                }

                AvifResultCode reserveCode = resources.ReserveTemporaryBytes(child.Size);
                if (reserveCode != AvifResultCode.Success)
                {
                    return reserveCode;
                }

                property = new ParsedItemProperty(
                    propertyIndex,
                    child.Type,
                    child.TypeValue,
                    child.IsUuid,
                    child.UserType,
                    raw.ToArray());
                property.SetAv1Configuration(av1Configuration);
                property.SetSpatialExtents(spatialExtents);
                property.SetPixelInformation(pixelInformation);
                property.SetAdditionalProperties(additionalProperties);
                property.SetHdrCameraProperties(hdrCameraProperties);
            }

            properties.Add(property);
            propertyIndex++;
            childIndex++;
        }

        return AvifResultCode.Success;
    }

    internal static ItemPropertyVersionProbe ProbeVersion(
        ReadOnlySpan<byte> source,
        IsoBox box)
    {
        bool versioned = box.Type == "ispe"
            || box.Type == "pixi"
            || box.Type == "auxC"
            || box.Type == "reve"
            || box.Type == "ndwt"
            || box.Type == "cmin"
            || box.Type == "cmex";
        if (!versioned)
        {
            return new ItemPropertyVersionProbe(AvifResultCode.Success, true);
        }

        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            return new ItemPropertyVersionProbe(fullBox.Code, false);
        }

        return new ItemPropertyVersionProbe(
            AvifResultCode.Success,
            fullBox.Version == 0);
    }

    internal static ParsedItemProperty? FindDuplicate(
        List<ParsedItemProperty> properties,
        ReadOnlySpan<byte> raw)
    {
        int index = 0;
        while (index < properties.Count)
        {
            ParsedItemProperty property = properties[index];
            if (property.RawDataEquals(raw))
            {
                return property;
            }

            index++;
        }

        return null;
    }

    internal static ItemPropertyAssociationBoxParseResult ParseAssociations(
        ReadOnlySpan<byte> source,
        IsoBox box,
        List<ParsedItemProperty> properties,
        ResourceGuard resources)
    {
        List<ParsedItemAssociationSet> items = new List<ParsedItemAssociationSet>();
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            return CreateAssociationResult(fullBox.Code, 0, 0, items);
        }

        if (fullBox.Version != 0 && fullBox.Version != 1)
        {
            return CreateAssociationResult(
                AvifResultCode.UnsupportedFeature,
                fullBox.Version,
                fullBox.Flags,
                items);
        }

        if (fullBox.Flags != 0 && fullBox.Flags != 1)
        {
            return CreateAssociationResult(
                AvifResultCode.InvalidContainer,
                fullBox.Version,
                fullBox.Flags,
                items);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset,
            0,
            fullBox.PayloadSize,
            source.Length);
        if (!range.Success)
        {
            return CreateAssociationResult(
                range.Code,
                fullBox.Version,
                fullBox.Flags,
                items);
        }

        ReadOnlySpan<byte> payload = source.Slice(
            (int)fullBox.PayloadOffset,
            (int)fullBox.PayloadSize);
        ByteReader reader = new ByteReader(payload.Length);
        uint entryCount = reader.ReadUInt32BigEndian(payload);
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateAssociationResult(
                AvifResultCode.InvalidContainer,
                fullBox.Version,
                fullBox.Flags,
                items);
        }

        if (entryCount > int.MaxValue || entryCount > resources.Limits.MaxItemCount)
        {
            return CreateAssociationResult(
                AvifResultCode.LimitExceeded,
                fullBox.Version,
                fullBox.Flags,
                items);
        }

        uint previousItemId = 0;
        uint entryIndex = 0;
        while (entryIndex < entryCount)
        {
            uint itemId;
            if (fullBox.Version == 0)
            {
                itemId = reader.ReadUInt16BigEndian(payload);
            }
            else
            {
                itemId = reader.ReadUInt32BigEndian(payload);
            }

            byte associationCount = reader.ReadUInt8(payload);
            if (reader.Code != AvifResultCode.Success)
            {
                return CreateAssociationResult(
                    AvifResultCode.InvalidContainer,
                    fullBox.Version,
                    fullBox.Flags,
                    items);
            }

            if (itemId == 0 || (entryIndex > 0 && itemId <= previousItemId))
            {
                return CreateAssociationResult(
                    AvifResultCode.InvalidItem,
                    fullBox.Version,
                    fullBox.Flags,
                    items);
            }

            previousItemId = itemId;
            AvifResultCode countCode = resources.CountReferences(associationCount);
            if (countCode != AvifResultCode.Success)
            {
                return CreateAssociationResult(
                    countCode,
                    fullBox.Version,
                    fullBox.Flags,
                    items);
            }

            List<ParsedPropertyAssociation> associations = new List<ParsedPropertyAssociation>();
            int associationIndex = 0;
            while (associationIndex < associationCount)
            {
                bool essential;
                int propertyIndex;
                if (fullBox.Flags == 0)
                {
                    byte association = reader.ReadUInt8(payload);
                    essential = (association & 128) != 0;
                    propertyIndex = association & 127;
                }
                else
                {
                    ushort association = reader.ReadUInt16BigEndian(payload);
                    essential = (association & 32768) != 0;
                    propertyIndex = association & 32767;
                }

                if (reader.Code != AvifResultCode.Success)
                {
                    return CreateAssociationResult(
                        AvifResultCode.InvalidContainer,
                        fullBox.Version,
                        fullBox.Flags,
                        items);
                }

                if (propertyIndex == 0)
                {
                    if (essential)
                    {
                        return CreateAssociationResult(
                            AvifResultCode.InvalidItem,
                            fullBox.Version,
                            fullBox.Flags,
                            items);
                    }

                    associationIndex++;
                    continue;
                }

                if (propertyIndex > properties.Count)
                {
                    return CreateAssociationResult(
                        AvifResultCode.InvalidItem,
                        fullBox.Version,
                        fullBox.Flags,
                        items);
                }

                ParsedItemProperty property = properties[propertyIndex - 1];
                ParsedPropertyAssociation? duplicate = FindAssociation(associations, property);
                if (duplicate != null)
                {
                    if (duplicate.IsEssential() != essential)
                    {
                        return CreateAssociationResult(
                            AvifResultCode.InvalidItem,
                            fullBox.Version,
                            fullBox.Flags,
                            items);
                    }
                }
                else
                {
                    associations.Add(new ParsedPropertyAssociation(
                        propertyIndex,
                        essential,
                        property));
                }

                associationIndex++;
            }

            items.Add(new ParsedItemAssociationSet(itemId, associations));
            entryIndex++;
        }

        if (reader.Code != AvifResultCode.Success || reader.Remaining() != 0)
        {
            return CreateAssociationResult(
                AvifResultCode.InvalidContainer,
                fullBox.Version,
                fullBox.Flags,
                items);
        }

        return CreateAssociationResult(
            AvifResultCode.Success,
            fullBox.Version,
            fullBox.Flags,
            items);
    }

    internal static ParsedPropertyAssociation? FindAssociation(
        List<ParsedPropertyAssociation> associations,
        ParsedItemProperty property)
    {
        int index = 0;
        while (index < associations.Count)
        {
            ParsedPropertyAssociation association = associations[index];
            if (object.ReferenceEquals(association.GetProperty(), property))
            {
                return association;
            }

            index++;
        }

        return null;
    }

    internal static ItemPropertyAssociationBoxParseResult CreateAssociationResult(
        AvifResultCode code,
        int version,
        int flags,
        List<ParsedItemAssociationSet> items)
    {
        return new ItemPropertyAssociationBoxParseResult(
            code,
            version,
            flags,
            items);
    }
}
