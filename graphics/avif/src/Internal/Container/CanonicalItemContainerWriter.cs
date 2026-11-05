using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace avif;

internal class CanonicalItemPayload
{
    internal uint ItemId;
    internal string? ItemType;
    internal string Name;
    internal string? ContentType;
    internal string ContentEncoding;
    internal string ItemUriType;
    internal bool Hidden;
    internal long Length;
    internal bool HasData;
    internal byte[] Data;
    internal uint SharedDataItemId;
    internal List<CanonicalItemPropertyAssociation> Properties;

    internal static CanonicalItemPayload FromData(
        uint itemId,
        string? itemType,
        string name,
        bool hidden,
        byte[] data)
    {
        CanonicalItemPayload result = new CanonicalItemPayload(
            itemId,
            itemType == null ? string.Empty : itemType,
            name,
            hidden,
            data == null ? -1 : data.Length);
        result.ItemType = itemType;
        result.HasData = data != null;
        result.Data = data == null
            ? System.Array.Empty<byte>() : (byte[])data.Clone();
        return result;
    }

    internal CanonicalItemPayload(
        uint itemId,
        string itemType,
        string name,
        bool hidden,
        long measuredLength)
    {
        ItemId = itemId;
        ItemType = itemType;
        Name = name;
        ContentType = string.Empty;
        ContentEncoding = string.Empty;
        ItemUriType = string.Empty;
        Hidden = hidden;
        Length = measuredLength;
        HasData = false;
        Data = System.Array.Empty<byte>();
        SharedDataItemId = 0;
        Properties = new List<CanonicalItemPropertyAssociation>();
    }

    internal static CanonicalItemPayload Create(
        uint itemId,
        string? itemType,
        string name,
        bool hidden,
        byte[] data)
    {
        return FromData(itemId, itemType, name, hidden, data);
    }

    internal static CanonicalItemPayload Create(
        uint itemId,
        string itemType,
        string name,
        bool hidden,
        long measuredLength)
    {
        return new CanonicalItemPayload(
            itemId,
            itemType,
            name,
            hidden,
            measuredLength);
    }
}

internal class CanonicalItemPropertyAssociation
{
    internal byte[] RawProperty;
    internal bool Essential;

    internal CanonicalItemPropertyAssociation(byte[] rawProperty, bool essential)
    {
        RawProperty = rawProperty == null
            ? System.Array.Empty<byte>()
            : (byte[])rawProperty.Clone();
        Essential = essential;
    }
}

internal class CanonicalItemReference
{
    internal string Type;
    internal uint FromItemId;
    internal List<uint>? ToItemIds;

    internal CanonicalItemReference(
        string type,
        uint fromItemId,
        List<uint> toItemIds)
    {
        Type = type;
        FromItemId = fromItemId;
        ToItemIds = toItemIds == null ? null : new List<uint>(toItemIds);
    }
}

internal class CanonicalEntityGroup
{
    internal string Type;
    internal uint GroupId;
    internal List<uint>? EntityIds;

    internal CanonicalEntityGroup(
        string type,
        uint groupId,
        List<uint> entityIds)
    {
        Type = type;
        GroupId = groupId;
        EntityIds = entityIds == null ? null : new List<uint>(entityIds);
    }
}

internal class CanonicalPropertyProbe
{
    internal AvifResultCode Code;
    internal string Type;

    internal CanonicalPropertyProbe(AvifResultCode code, string type)
    {
        Code = code;
        Type = type;
    }
}

internal class CanonicalItemContainerOptions
{
    internal AvifHeaderFormat HeaderFormat;
    internal bool UseItemData;
    internal bool Force64BitOffsets;
    internal bool ForceLargeBoxHeaders;
    internal long MaximumOutputBytes;

    internal CanonicalItemContainerOptions()
    {
        HeaderFormat = AvifHeaderFormat.Full;
        UseItemData = false;
        Force64BitOffsets = false;
        ForceLargeBoxHeaders = false;
        MaximumOutputBytes = AvifSecurityLimits.DefaultMaxSourceBytes;
    }
}

internal class CanonicalItemContainerRequest
{
    internal string MajorBrand;
    internal uint MinorVersion;
    internal List<string>? CompatibleBrands;
    internal uint PrimaryItemId;
    internal List<CanonicalItemPayload>? Items;
    internal List<CanonicalItemReference>? References;
    internal List<CanonicalEntityGroup>? Groups;
    internal CanonicalItemContainerOptions Options;
    internal bool TimedSequence;

    internal static CanonicalItemContainerRequest Create(
        string majorBrand,
        uint minorVersion,
        List<string> compatibleBrands,
        uint primaryItemId,
        List<CanonicalItemPayload> items,
        CanonicalItemContainerOptions options)
    {
        return new CanonicalItemContainerRequest(
            majorBrand,
            minorVersion,
            compatibleBrands,
            primaryItemId,
            items,
            new List<CanonicalItemReference>(),
            new List<CanonicalEntityGroup>(),
            options);
    }

    internal CanonicalItemContainerRequest(
        string majorBrand,
        uint minorVersion,
        List<string> compatibleBrands,
        uint primaryItemId,
        List<CanonicalItemPayload>? items,
        List<CanonicalItemReference>? references,
        List<CanonicalEntityGroup>? groups,
        CanonicalItemContainerOptions options)
    {
        MajorBrand = majorBrand;
        MinorVersion = minorVersion;
        CompatibleBrands = compatibleBrands == null
            ? null
            : new List<string>(compatibleBrands);
        PrimaryItemId = primaryItemId;
        Items = items == null ? null : new List<CanonicalItemPayload>(items);
        References = references == null
            ? null
            : new List<CanonicalItemReference>(references);
        Groups = groups == null ? null : new List<CanonicalEntityGroup>(groups);
        Options = options;
        TimedSequence = false;
    }

    internal static CanonicalItemContainerRequest Create(
        string majorBrand,
        uint minorVersion,
        List<string> compatibleBrands,
        uint primaryItemId,
        List<CanonicalItemPayload>? items,
        List<CanonicalItemReference>? references,
        List<CanonicalEntityGroup>? groups,
        CanonicalItemContainerOptions options)
    {
        return new CanonicalItemContainerRequest(
            majorBrand,
            minorVersion,
            compatibleBrands,
            primaryItemId,
            items,
            references,
            groups,
            options);
    }
}

internal class CanonicalItemContainerLayout
{
    internal AvifResultCode Code;
    internal long FileTypeSize;
    internal long MetaSize;
    internal long MinimizedImageSize;
    internal long MediaBoxSize;
    internal long FileSize;
    internal long MediaPayloadOffset;
    internal int ItemLocationVersion;
    internal int OffsetSize;
    internal int LengthSize;
    internal bool UsesItemData;
    internal bool UsesMinimizedFormat;
    internal bool UsesLargeMetaBox;
    internal bool UsesLargeMediaBox;
    internal List<long> ItemOffsets;

    internal CanonicalItemContainerLayout(AvifResultCode code)
    {
        Code = code;
        FileTypeSize = 0;
        MetaSize = 0;
        MinimizedImageSize = 0;
        MediaBoxSize = 0;
        FileSize = 0;
        MediaPayloadOffset = 0;
        ItemLocationVersion = 0;
        OffsetSize = 0;
        LengthSize = 0;
        UsesItemData = false;
        UsesMinimizedFormat = false;
        UsesLargeMetaBox = false;
        UsesLargeMediaBox = false;
        ItemOffsets = new List<long>();
    }

    internal long GetItemOffset(int index)
    {
        if (index < 0 || index >= ItemOffsets.Count)
        {
            return -1;
        }

        return ItemOffsets[index];
    }
}

internal class CanonicalItemContainerWriteResult
{
    internal AvifResultCode Code;
    internal byte[] Data;
    internal CanonicalItemContainerLayout Layout;
    internal AvifContainerValidationResult? SelfValidation;

    internal CanonicalItemContainerWriteResult(
        AvifResultCode code,
        byte[] data,
        CanonicalItemContainerLayout layout)
    {
        Code = code;
        Data = data;
        Layout = layout;
        SelfValidation = null;
    }
}

internal static class CanonicalItemContainerWriter
{
    internal static CanonicalItemContainerLayout Measure(
        CanonicalItemContainerRequest request)
    {
        CanonicalItemContainerLayout full = MeasureFull(request);
        if (full.Code != AvifResultCode.Success)
        {
            return full;
        }

        MinimizedImageWriteModel minimized = MinimizedImageBoxWriter.CreateModel(request);
        if (!minimized.Representable)
        {
            return full;
        }

        CanonicalItemContainerLayout compact = MinimizedImageBoxWriter.Measure(
            minimized,
            request.Options.MaximumOutputBytes);
        if (compact.Code != AvifResultCode.Success)
        {
            return request.Options.HeaderFormat == AvifHeaderFormat.Minimized
                ? compact
                : full;
        }

        if (request.Options.HeaderFormat == AvifHeaderFormat.Minimized
            || (request.Options.HeaderFormat == AvifHeaderFormat.Automatic
                && compact.FileSize < full.FileSize))
        {
            return compact;
        }

        return full;
    }

    internal static CanonicalItemContainerLayout MeasureFull(
        CanonicalItemContainerRequest request)
    {
        AvifResultCode validationCode = ValidateRequest(request, false);
        if (validationCode != AvifResultCode.Success)
        {
            return new CanonicalItemContainerLayout(validationCode);
        }

        CanonicalItemContainerOptions options = request.Options;
        List<CanonicalItemPayload> items = SortItems(request.Items);
        List<CanonicalItemReference> references = SortReferences(request.References);
        List<CanonicalEntityGroup> groups = SortGroups(request.Groups);
        List<byte[]> properties = InternProperties(items);
        CanonicalBrandSelection brands = CanonicalBrandSelector.Choose(
            items,
            request.MajorBrand,
            request.CompatibleBrands,
            request.TimedSequence);
        if (brands.Code != AvifResultCode.Success)
        {
            return new CanonicalItemContainerLayout(brands.Code);
        }

        int itemCount = items.Count;
        int locationVersion = RequiresWideItemIds(items) ? 2 : 1;
        int idSize = locationVersion == 2 ? 4 : 2;
        int countSize = locationVersion == 2 ? 4 : 2;
        int lengthSize = options.Force64BitOffsets || HasWideLength(items) ? 8 : 4;
        long payloadSize = SumPayloadLengths(items);
        if (payloadSize < 0)
        {
            return new CanonicalItemContainerLayout(AvifResultCode.LimitExceeded);
        }

        CheckedIntegerResult fileTypeSizeResult = CheckedMath.AddInt64(
            16,
            (long)brands.CompatibleBrands.Count * 4);
        if (!fileTypeSizeResult.Success)
        {
            return new CanonicalItemContainerLayout(fileTypeSizeResult.Code);
        }

        long fileTypeSize = fileTypeSizeResult.Value;
        long itemInfoSize = MeasureItemInfo(items);
        if (itemInfoSize < 0)
        {
            return new CanonicalItemContainerLayout(AvifResultCode.LimitExceeded);
        }

        long handlerSize = 33;
        long primarySize = request.PrimaryItemId <= ushort.MaxValue ? 14 : 16;
        long propertiesSize = MeasureProperties(items, properties);
        long referencesSize = MeasureReferences(references);
        long groupsSize = MeasureGroups(groups);
        if (propertiesSize < 0 || referencesSize < 0 || groupsSize < 0)
        {
            return new CanonicalItemContainerLayout(AvifResultCode.LimitExceeded);
        }
        bool largeMedia = options.ForceLargeBoxHeaders
            || payloadSize > uint.MaxValue - 8;
        int mediaHeaderSize = largeMedia ? 16 : 8;
        long mediaBoxSize = AddOrFail(payloadSize, mediaHeaderSize);
        if (mediaBoxSize < 0)
        {
            return new CanonicalItemContainerLayout(AvifResultCode.LimitExceeded);
        }

        int offsetSize = options.Force64BitOffsets ? 8 : 4;
        bool largeMeta = options.ForceLargeBoxHeaders;
        long metaSize = 0;
        long mediaPayloadOffset = 0;
        int iteration = 0;
        while (iteration < 4)
        {
            long locationSize = MeasureItemLocation(
                itemCount,
                locationVersion,
                idSize,
                countSize,
                offsetSize,
                lengthSize);
            if (locationSize < 0)
            {
                return new CanonicalItemContainerLayout(AvifResultCode.LimitExceeded);
            }

            int metaHeaderSize = largeMeta ? 20 : 12;
            long childrenSize = AddOrFail(handlerSize, primarySize);
            childrenSize = AddOrFail(childrenSize, locationSize);
            childrenSize = AddOrFail(childrenSize, itemInfoSize);
            childrenSize = AddOrFail(childrenSize, referencesSize);
            childrenSize = AddOrFail(childrenSize, groupsSize);
            childrenSize = AddOrFail(childrenSize, propertiesSize);
            if (childrenSize < 0)
            {
                return new CanonicalItemContainerLayout(AvifResultCode.LimitExceeded);
            }

            if (options.UseItemData)
            {
                childrenSize = AddOrFail(childrenSize, mediaBoxSize);
            }

            metaSize = AddOrFail(childrenSize, metaHeaderSize);
            if (metaSize < 0)
            {
                return new CanonicalItemContainerLayout(AvifResultCode.LimitExceeded);
            }

            bool requiredLargeMeta = options.ForceLargeBoxHeaders || metaSize > uint.MaxValue;
            if (requiredLargeMeta != largeMeta)
            {
                largeMeta = requiredLargeMeta;
                iteration++;
                continue;
            }

            mediaPayloadOffset = options.UseItemData
                ? 0
                : AddOrFail(AddOrFail(fileTypeSize, metaSize), mediaHeaderSize);
            if (mediaPayloadOffset < 0)
            {
                return new CanonicalItemContainerLayout(AvifResultCode.LimitExceeded);
            }

            long maximumOffset = GetMaximumItemOffset(
                items,
                mediaPayloadOffset);
            if (maximumOffset < 0)
            {
                return new CanonicalItemContainerLayout(AvifResultCode.LimitExceeded);
            }

            int requiredOffsetSize = options.Force64BitOffsets
                || (ulong)maximumOffset > uint.MaxValue
                ? 8
                : 4;
            if (requiredOffsetSize == offsetSize)
            {
                break;
            }

            offsetSize = requiredOffsetSize;
            iteration++;
        }

        long fileSize = options.UseItemData
            ? AddOrFail(fileTypeSize, metaSize)
            : AddOrFail(AddOrFail(fileTypeSize, metaSize), mediaBoxSize);
        if (fileSize < 0 || fileSize > options.MaximumOutputBytes)
        {
            return new CanonicalItemContainerLayout(AvifResultCode.LimitExceeded);
        }

        CanonicalItemContainerLayout layout = new CanonicalItemContainerLayout(
            AvifResultCode.Success);
        layout.FileTypeSize = fileTypeSize;
        layout.MetaSize = metaSize;
        layout.MediaBoxSize = mediaBoxSize;
        layout.FileSize = fileSize;
        layout.MediaPayloadOffset = mediaPayloadOffset;
        layout.ItemLocationVersion = locationVersion;
        layout.OffsetSize = offsetSize;
        layout.LengthSize = lengthSize;
        layout.UsesItemData = options.UseItemData;
        layout.UsesLargeMetaBox = largeMeta;
        layout.UsesLargeMediaBox = largeMedia;
        long runningOffset = mediaPayloadOffset;
        int itemIndex = 0;
        while (itemIndex < items.Count)
        {
            CanonicalItemPayload item = items[itemIndex];
            layout.ItemOffsets.Add(
                item.SharedDataItemId == 0 ? runningOffset : -1);
            if (item.SharedDataItemId == 0)
            {
                runningOffset = AddOrFail(runningOffset, item.Length);
                if (runningOffset < 0)
                {
                    return new CanonicalItemContainerLayout(
                        AvifResultCode.LimitExceeded);
                }
            }

            itemIndex++;
        }
        itemIndex = 0;
        while (itemIndex < items.Count)
        {
            CanonicalItemPayload item = items[itemIndex];
            if (item.SharedDataItemId != 0)
            {
                int ownerIndex = FindItemIndex(items, item.SharedDataItemId);
                if (ownerIndex < 0)
                {
                    return new CanonicalItemContainerLayout(
                        AvifResultCode.InvalidArgument);
                }
                layout.ItemOffsets[itemIndex] =
                    layout.ItemOffsets[ownerIndex];
            }
            itemIndex++;
        }

        return layout;
    }

    internal static CanonicalItemContainerWriteResult Write(
        CanonicalItemContainerRequest request)
    {
        CanonicalItemContainerLayout layout = Measure(request);
        if (layout.Code != AvifResultCode.Success)
        {
            return new CanonicalItemContainerWriteResult(
                layout.Code,
                System.Array.Empty<byte>(),
                layout);
        }

        AvifResultCode requestCode = ValidateRequest(request, true);
        if (requestCode != AvifResultCode.Success)
        {
            return new CanonicalItemContainerWriteResult(
                requestCode,
                System.Array.Empty<byte>(),
                layout);
        }

        if (layout.FileSize > System.Array.MaxLength)
        {
            return new CanonicalItemContainerWriteResult(
                AvifResultCode.LimitExceeded,
                System.Array.Empty<byte>(),
                layout);
        }

        IsoBoxWriter writer = new IsoBoxWriter(0, request.Options.MaximumOutputBytes);
        AvifResultCode code = WriteContainer(writer, request, layout);
        if (code != AvifResultCode.Success)
        {
            writer.Close();
            return new CanonicalItemContainerWriteResult(
                code,
                System.Array.Empty<byte>(),
                layout);
        }

        IsoBoxWriteResult finished = writer.Finish();
        writer.Close();
        if (finished.Code != AvifResultCode.Success
            || finished.Data.LongLength != layout.FileSize)
        {
            AvifResultCode finishCode = finished.Code == AvifResultCode.Success
                ? AvifResultCode.InvalidState
                : finished.Code;
            return new CanonicalItemContainerWriteResult(
                finishCode,
                System.Array.Empty<byte>(),
                layout);
        }

        AvifContainerValidationResult? validation = null;
        if (!request.TimedSequence)
        {
            validation = ValidateAuthoredFile(
                finished.Data,
                request);
            if (validation.Code != AvifResultCode.Success)
            {
                CanonicalItemContainerWriteResult failed =
                    new CanonicalItemContainerWriteResult(
                        validation.Code,
                        System.Array.Empty<byte>(),
                        layout);
                failed.SelfValidation = validation;
                return failed;
            }
        }

        CanonicalItemContainerWriteResult result = new CanonicalItemContainerWriteResult(
            AvifResultCode.Success,
            finished.Data,
            layout);
        result.SelfValidation = validation;
        return result;
    }

    internal static AvifResultCode Write(
        Stream destination,
        CanonicalItemContainerRequest request,
        bool leaveOpen)
    {
        if (destination == null || !destination.CanWrite)
        {
            return AvifResultCode.InvalidArgument;
        }

        CanonicalItemContainerWriteResult staged = Write(request);
        if (staged.Code != AvifResultCode.Success)
        {
            return staged.Code;
        }

        StreamOutput output = new StreamOutput(destination, leaveOpen);
        AvifResultCode writeCode = output.Write(staged.Data);
        if (writeCode == AvifResultCode.Success)
        {
            writeCode = output.Flush();
        }

        output.Close();
        return writeCode;
    }

    internal static AvifResultCode WriteContainer(
        IsoBoxWriter writer,
        CanonicalItemContainerRequest request,
        CanonicalItemContainerLayout layout)
    {
        if (layout.UsesMinimizedFormat)
        {
            MinimizedImageWriteModel minimized = MinimizedImageBoxWriter.CreateModel(request);
            if (!minimized.Representable)
            {
                return AvifResultCode.InvalidState;
            }

            return MinimizedImageBoxWriter.Write(writer, minimized);
        }

        List<string> sortedBrands = SortBrands(request.CompatibleBrands);
        List<CanonicalItemPayload> items = SortItems(request.Items);
        List<CanonicalItemReference> references = SortReferences(request.References);
        List<CanonicalEntityGroup> groups = SortGroups(request.Groups);
        List<byte[]> properties = InternProperties(items);
        CanonicalBrandSelection selection = CanonicalBrandSelector.Choose(
            items,
            request.MajorBrand,
            sortedBrands,
            request.TimedSequence);
        if (selection.Code != AvifResultCode.Success)
        {
            return selection.Code;
        }

        string[] compatibleBrands = SortBrands(selection.CompatibleBrands).ToArray();
        AvifResultCode code = FileTypeBox.Write(
            writer,
            selection.MajorBrand,
            request.MinorVersion,
            compatibleBrands);
        if (code != AvifResultCode.Success)
        {
            return code;
        }

        code = layout.UsesLargeMetaBox
            ? writer.BeginLargeFullBox("meta", 0, 0)
            : writer.BeginFullBox("meta", 0, 0);
        if (code != AvifResultCode.Success)
        {
            return code;
        }

        code = WriteHandler(writer);
        if (code == AvifResultCode.Success)
        {
            code = WritePrimaryItem(writer, request.PrimaryItemId);
        }

        if (code == AvifResultCode.Success)
        {
            code = WriteItemLocations(writer, items, layout);
        }

        if (code == AvifResultCode.Success)
        {
            code = WriteItemInfo(writer, items);
        }

        if (code == AvifResultCode.Success && references.Count > 0)
        {
            code = WriteReferences(writer, references);
        }

        if (code == AvifResultCode.Success && groups.Count > 0)
        {
            code = WriteGroups(writer, groups);
        }

        if (code == AvifResultCode.Success)
        {
            code = WriteProperties(writer, items, properties);
        }

        if (code == AvifResultCode.Success && layout.UsesItemData)
        {
            code = WriteMediaBox(writer, "idat", items, layout.UsesLargeMediaBox);
        }

        if (code == AvifResultCode.Success)
        {
            code = writer.EndBox();
        }

        if (code == AvifResultCode.Success && !layout.UsesItemData)
        {
            code = WriteMediaBox(writer, "mdat", items, layout.UsesLargeMediaBox);
        }

        return code;
    }

    internal static AvifResultCode WriteHandler(IsoBoxWriter writer)
    {
        AvifResultCode code = writer.BeginFullBox("hdlr", 0, 0);
        if (code != AvifResultCode.Success)
        {
            return code;
        }

        byte[] fields = new byte[21];
        WriteFourCc(fields.AsSpan(4, 4), "pict");
        code = writer.Write(fields);
        return code == AvifResultCode.Success ? writer.EndBox() : code;
    }

    internal static AvifResultCode WritePrimaryItem(
        IsoBoxWriter writer,
        uint primaryItemId)
    {
        int version = primaryItemId <= ushort.MaxValue ? 0 : 1;
        AvifResultCode code = writer.BeginFullBox("pitm", version, 0);
        if (code != AvifResultCode.Success)
        {
            return code;
        }

        if (version == 0)
        {
            code = WriteUInt16(writer, (ushort)primaryItemId);
        }
        else
        {
            code = WriteUInt32(writer, primaryItemId);
        }

        return code == AvifResultCode.Success ? writer.EndBox() : code;
    }

    internal static AvifResultCode WriteItemLocations(
        IsoBoxWriter writer,
        List<CanonicalItemPayload> items,
        CanonicalItemContainerLayout layout)
    {
        AvifResultCode code = writer.BeginFullBox(
            "iloc",
            layout.ItemLocationVersion,
            0);
        if (code != AvifResultCode.Success)
        {
            return code;
        }

        byte[] sizes = new byte[2];
        sizes[0] = (byte)(layout.OffsetSize << 4 | layout.LengthSize);
        sizes[1] = 0;
        code = writer.Write(sizes);
        if (code != AvifResultCode.Success)
        {
            return code;
        }

        if (layout.ItemLocationVersion == 2)
        {
            code = WriteUInt32(writer, (uint)items.Count);
        }
        else
        {
            code = WriteUInt16(writer, (ushort)items.Count);
        }

        int itemIndex = 0;
        while (code == AvifResultCode.Success && itemIndex < items.Count)
        {
            CanonicalItemPayload item = items[itemIndex];
            if (layout.ItemLocationVersion == 2)
            {
                code = WriteUInt32(writer, item.ItemId);
            }
            else
            {
                code = WriteUInt16(writer, (ushort)item.ItemId);
            }

            if (code == AvifResultCode.Success)
            {
                code = WriteUInt16(writer, (ushort)(layout.UsesItemData ? 1 : 0));
            }

            if (code == AvifResultCode.Success)
            {
                code = WriteUInt16(writer, 0);
            }

            if (code == AvifResultCode.Success)
            {
                code = WriteUInt16(writer, 1);
            }

            if (code == AvifResultCode.Success)
            {
                code = WriteSizedInteger(
                    writer,
                    (ulong)layout.GetItemOffset(itemIndex),
                    layout.OffsetSize);
            }

            if (code == AvifResultCode.Success)
            {
                code = WriteSizedInteger(
                    writer,
                    (ulong)item.Length,
                    layout.LengthSize);
            }

            itemIndex++;
        }

        return code == AvifResultCode.Success ? writer.EndBox() : code;
    }

    internal static AvifResultCode WriteItemInfo(
        IsoBoxWriter writer,
        List<CanonicalItemPayload> items)
    {
        AvifResultCode code = writer.BeginFullBox("iinf", 1, 0);
        if (code != AvifResultCode.Success)
        {
            return code;
        }

        code = WriteUInt32(writer, (uint)items.Count);
        int itemIndex = 0;
        while (code == AvifResultCode.Success && itemIndex < items.Count)
        {
            CanonicalItemPayload item = items[itemIndex];
            code = writer.BeginFullBox("infe", 3, item.Hidden ? 1 : 0);
            if (code == AvifResultCode.Success)
            {
                code = WriteUInt32(writer, item.ItemId);
            }

            if (code == AvifResultCode.Success)
            {
                code = WriteUInt16(writer, 0);
            }

            if (code == AvifResultCode.Success)
            {
                byte[] type = new byte[4];
                WriteFourCc(type, item.ItemType);
                code = writer.Write(type);
            }

            if (code == AvifResultCode.Success)
            {
                code = writer.Write(Encoding.UTF8.GetBytes(item.Name));
            }

            if (code == AvifResultCode.Success)
            {
                code = writer.Write(new byte[] { 0 });
            }

            if (code == AvifResultCode.Success && item.ItemType == "mime")
            {
                code = WriteNullTerminatedString(writer, item.ContentType);
                if (code == AvifResultCode.Success)
                {
                    code = WriteNullTerminatedString(writer, item.ContentEncoding);
                }
            }

            if (code == AvifResultCode.Success && item.ItemType == "uri ")
            {
                code = WriteNullTerminatedString(writer, item.ItemUriType);
            }

            if (code == AvifResultCode.Success)
            {
                code = writer.EndBox();
            }

            itemIndex++;
        }

        return code == AvifResultCode.Success ? writer.EndBox() : code;
    }

    internal static AvifResultCode WriteProperties(
        IsoBoxWriter writer,
        List<CanonicalItemPayload> items,
        List<byte[]> properties)
    {
        AvifResultCode code = writer.BeginBox("iprp");
        if (code == AvifResultCode.Success)
        {
            code = writer.BeginBox("ipco");
        }

        int propertyIndex = 0;
        while (code == AvifResultCode.Success && propertyIndex < properties.Count)
        {
            code = writer.Write(properties[propertyIndex]);
            propertyIndex++;
        }

        if (code == AvifResultCode.Success)
        {
            code = writer.EndBox();
        }

        int associatedItemCount = CountAssociatedItems(items);
        if (code == AvifResultCode.Success && associatedItemCount > 0)
        {
            int version = RequiresWideAssociationItemIds(items) ? 1 : 0;
            int flags = properties.Count > 127 ? 1 : 0;
            code = writer.BeginFullBox("ipma", version, flags);
            if (code == AvifResultCode.Success)
            {
                code = WriteUInt32(writer, (uint)associatedItemCount);
            }

            int itemIndex = 0;
            while (code == AvifResultCode.Success && itemIndex < items.Count)
            {
                CanonicalItemPayload item = items[itemIndex];
                if (item.Properties.Count > 0)
                {
                    code = version == 0
                        ? WriteUInt16(writer, (ushort)item.ItemId)
                        : WriteUInt32(writer, item.ItemId);
                    if (code == AvifResultCode.Success)
                    {
                        code = WriteUInt8(writer, (byte)item.Properties.Count);
                    }

                    int associationIndex = 0;
                    while (code == AvifResultCode.Success
                        && associationIndex < item.Properties.Count)
                    {
                        CanonicalItemPropertyAssociation association =
                            item.Properties[associationIndex];
                        int index = FindPropertyIndex(properties, association.RawProperty);
                        if (flags == 0)
                        {
                            byte value = (byte)(index | (association.Essential ? 128 : 0));
                            code = WriteUInt8(writer, value);
                        }
                        else
                        {
                            ushort value = (ushort)(index
                                | (association.Essential ? 32768 : 0));
                            code = WriteUInt16(writer, value);
                        }

                        associationIndex++;
                    }
                }

                itemIndex++;
            }

            if (code == AvifResultCode.Success)
            {
                code = writer.EndBox();
            }
        }

        if (code == AvifResultCode.Success)
        {
            code = writer.EndBox();
        }

        return code;
    }

    internal static AvifResultCode WriteReferences(
        IsoBoxWriter writer,
        List<CanonicalItemReference> references)
    {
        int version = RequiresWideReferenceIds(references) ? 1 : 0;
        AvifResultCode code = writer.BeginFullBox("iref", version, 0);
        int referenceIndex = 0;
        while (code == AvifResultCode.Success && referenceIndex < references.Count)
        {
            CanonicalItemReference reference = references[referenceIndex];
            code = writer.BeginBox(reference.Type);
            if (code == AvifResultCode.Success)
            {
                code = version == 0
                    ? WriteUInt16(writer, (ushort)reference.FromItemId)
                    : WriteUInt32(writer, reference.FromItemId);
            }

            if (code == AvifResultCode.Success)
            {
                code = WriteUInt16(writer, (ushort)(reference.ToItemIds ?? throw new InvalidOperationException("Invalid internal state.")).Count);
            }

            int targetIndex = 0;
            while (code == AvifResultCode.Success
                && targetIndex < (reference.ToItemIds ?? throw new InvalidOperationException("Invalid internal state.")).Count)
            {
                uint target = reference.ToItemIds[targetIndex];
                code = version == 0
                    ? WriteUInt16(writer, (ushort)target)
                    : WriteUInt32(writer, target);
                targetIndex++;
            }

            if (code == AvifResultCode.Success)
            {
                code = writer.EndBox();
            }

            referenceIndex++;
        }

        return code == AvifResultCode.Success ? writer.EndBox() : code;
    }

    internal static AvifResultCode WriteGroups(
        IsoBoxWriter writer,
        List<CanonicalEntityGroup> groups)
    {
        AvifResultCode code = writer.BeginBox("grpl");
        int groupIndex = 0;
        while (code == AvifResultCode.Success && groupIndex < groups.Count)
        {
            CanonicalEntityGroup group = groups[groupIndex];
            code = writer.BeginFullBox(group.Type, 0, 0);
            if (code == AvifResultCode.Success)
            {
                code = WriteUInt32(writer, group.GroupId);
            }

            if (code == AvifResultCode.Success)
            {
                code = WriteUInt32(writer, (uint)(group.EntityIds ?? throw new InvalidOperationException("Invalid internal state.")).Count);
            }

            int entityIndex = 0;
            while (code == AvifResultCode.Success
                && entityIndex < (group.EntityIds ?? throw new InvalidOperationException("Invalid internal state.")).Count)
            {
                code = WriteUInt32(writer, group.EntityIds[entityIndex]);
                entityIndex++;
            }

            if (code == AvifResultCode.Success)
            {
                code = writer.EndBox();
            }

            groupIndex++;
        }

        return code == AvifResultCode.Success ? writer.EndBox() : code;
    }

    internal static AvifResultCode WriteMediaBox(
        IsoBoxWriter writer,
        string type,
        List<CanonicalItemPayload> items,
        bool large)
    {
        AvifResultCode code = large
            ? writer.BeginLargeBox(type)
            : writer.BeginBox(type);
        int itemIndex = 0;
        while (code == AvifResultCode.Success && itemIndex < items.Count)
        {
            if (items[itemIndex].SharedDataItemId == 0)
            {
                code = writer.Write(items[itemIndex].Data);
            }
            itemIndex++;
        }

        return code == AvifResultCode.Success ? writer.EndBox() : code;
    }

    internal static AvifContainerValidationResult ValidateAuthoredFile(
        byte[] data,
        CanonicalItemContainerRequest request)
    {
        AvifValidationOptions options = new AvifValidationOptions();
        options.Mode = AvifValidationMode.Strict;
        options.AllowUnknownNonEssentialData = true;
        options.ValidateEncodedPayload = false;
        options.DecodePixelData = false;
        options.CollectInformation = false;
        options.CollectWarnings = true;
        options.StopAfterFirstError = false;
        options.Limits.MaxSourceBytes = data.LongLength;
        options.Limits.MaxBoxCount = int.MaxValue;
        options.Limits.MaxItemCount = (request.Items ?? throw new InvalidOperationException("Invalid internal state.")).Count > 0
            ? request.Items.Count
            : 1;
        int propertyCount = InternProperties(request.Items).Count;
        options.Limits.MaxPropertyCount = propertyCount > 0 ? propertyCount : 1;
        options.Limits.MaxReferenceCount = int.MaxValue;
        options.Limits.MaxWidth = int.MaxValue;
        options.Limits.MaxHeight = int.MaxValue;
        options.Limits.MaxPixels = long.MaxValue;
        options.Limits.MaxTemporaryBytes = data.LongLength > 0 ? data.LongLength : 1;
        return AvifContainerValidator.Validate(data, options);
    }

    internal static AvifResultCode ValidateRequest(
        CanonicalItemContainerRequest request,
        bool requireData)
    {
        if (request == null
            || request.Options == null
            || request.CompatibleBrands == null
            || request.Items == null
            || request.References == null
            || request.Groups == null
            || request.Options.MaximumOutputBytes < 0
            || (request.Options.HeaderFormat != AvifHeaderFormat.Automatic
                && request.Options.HeaderFormat != AvifHeaderFormat.Full
                && request.Options.HeaderFormat != AvifHeaderFormat.Minimized
                && request.Options.HeaderFormat != AvifHeaderFormat.PreserveExisting)
            || !IsoBoxType.IsValid(request.MajorBrand)
            || request.MajorBrand == "uuid"
            || request.Items.Count == 0
            || request.Items.Count > int.MaxValue)
        {
            return AvifResultCode.InvalidArgument;
        }

        HashSet<string> brands = new HashSet<string>();
        int brandIndex = 0;
        while (brandIndex < request.CompatibleBrands.Count)
        {
            if (!IsoBoxType.IsValid(request.CompatibleBrands[brandIndex])
                || request.CompatibleBrands[brandIndex] == "uuid"
                || !brands.Add(request.CompatibleBrands[brandIndex]))
            {
                return AvifResultCode.InvalidArgument;
            }

            brandIndex++;
        }

        HashSet<uint> itemIds = new HashSet<uint>();
        bool primaryFound = false;
        int itemIndex = 0;
        while (itemIndex < request.Items.Count)
        {
            CanonicalItemPayload item = request.Items[itemIndex];
            if (item == null
                || item.ItemId == 0
                || !itemIds.Add(item.ItemId)
                || !IsoBoxType.IsValid(item.ItemType)
                || item.ItemType == "uuid"
                || item.Name == null
                || item.Name.IndexOf('\0') >= 0
                || item.ContentType == null
                || item.ContentType.IndexOf('\0') >= 0
                || item.ContentEncoding == null
                || item.ContentEncoding.IndexOf('\0') >= 0
                || item.ItemUriType == null
                || item.ItemUriType.IndexOf('\0') >= 0
                || (item.ItemType == "mime" && item.ContentType.Length == 0)
                || (item.ItemType == "uri " && item.ItemUriType.Length == 0)
                || (item.ItemType != "mime"
                    && (item.ContentType.Length != 0 || item.ContentEncoding.Length != 0))
                || (item.ItemType != "uri " && item.ItemUriType.Length != 0)
                || item.Length < 0
                || item.Properties == null
                || (item.SharedDataItemId != 0 && item.HasData)
                || (requireData
                    && item.SharedDataItemId == 0
                    && (!item.HasData
                        || item.Data.LongLength != item.Length)))
            {
                return AvifResultCode.InvalidArgument;
            }

            if (item.ItemId == request.PrimaryItemId)
            {
                primaryFound = true;
            }

            int associationIndex = 0;
            while (associationIndex < item.Properties.Count)
            {
                CanonicalItemPropertyAssociation association =
                    item.Properties[associationIndex];
                if (association == null || association.RawProperty == null)
                {
                    return AvifResultCode.InvalidArgument;
                }

                CanonicalPropertyProbe probe = ProbeProperty(association.RawProperty);
                if (probe.Code != AvifResultCode.Success
                    || (association.Essential
                        && !AvifContainerValidator.IsSupportedProperty(probe.Type)))
                {
                    return AvifResultCode.InvalidArgument;
                }

                int previousIndex = 0;
                while (previousIndex < associationIndex)
                {
                    if (RawPropertiesEqual(
                        item.Properties[previousIndex].RawProperty,
                        association.RawProperty))
                    {
                        return AvifResultCode.InvalidArgument;
                    }

                    previousIndex++;
                }

                associationIndex++;
            }

            if (item.Properties.Count > byte.MaxValue)
            {
                return AvifResultCode.InvalidArgument;
            }

            itemIndex++;
        }

        itemIndex = 0;
        while (itemIndex < request.Items.Count)
        {
            CanonicalItemPayload item = request.Items[itemIndex];
            if (item.SharedDataItemId != 0)
            {
                int ownerIndex = FindItemIndex(
                    request.Items,
                    item.SharedDataItemId);
                CanonicalItemPayload? owner = ownerIndex < 0
                    ? null : request.Items[ownerIndex];
                if (owner == null
                    || owner == item
                    || owner.SharedDataItemId != 0
                    || owner.Length != item.Length)
                {
                    return AvifResultCode.InvalidArgument;
                }
            }
            itemIndex++;
        }

        if (!primaryFound || InternProperties(request.Items).Count > 32767)
        {
            return AvifResultCode.InvalidArgument;
        }

        HashSet<string> referenceSources = new HashSet<string>();
        int referenceIndex = 0;
        while (referenceIndex < request.References.Count)
        {
            CanonicalItemReference reference = request.References[referenceIndex];
            if (reference == null
                || !IsoBoxType.IsValid(reference.Type)
                || reference.Type == "uuid"
                || reference.Type == "free"
                || reference.Type == "skip"
                || reference.FromItemId == 0
                || !itemIds.Contains(reference.FromItemId)
                || reference.ToItemIds == null
                || reference.ToItemIds.Count == 0
                || reference.ToItemIds.Count > ushort.MaxValue
                || !referenceSources.Add(reference.Type + ":" + reference.FromItemId))
            {
                return AvifResultCode.InvalidArgument;
            }

            HashSet<uint> targets = new HashSet<uint>();
            int targetIndex = 0;
            while (targetIndex < reference.ToItemIds.Count)
            {
                uint target = reference.ToItemIds[targetIndex];
                if (target == 0
                    || target == reference.FromItemId
                    || !itemIds.Contains(target)
                    || !targets.Add(target))
                {
                    return AvifResultCode.InvalidArgument;
                }

                targetIndex++;
            }

            referenceIndex++;
        }

        HashSet<uint> groupIds = new HashSet<uint>();
        HashSet<uint> alternateIds = new HashSet<uint>();
        int groupIndex = 0;
        while (groupIndex < request.Groups.Count)
        {
            CanonicalEntityGroup group = request.Groups[groupIndex];
            if (group == null
                || !IsoBoxType.IsValid(group.Type)
                || group.Type == "uuid"
                || group.Type == "free"
                || group.Type == "skip"
                || group.GroupId == 0
                || itemIds.Contains(group.GroupId)
                || !groupIds.Add(group.GroupId)
                || group.EntityIds == null
                || group.EntityIds.Count == 0
                || (group.Type == "ster" && group.EntityIds.Count != 2))
            {
                return AvifResultCode.InvalidArgument;
            }

            HashSet<uint> entities = new HashSet<uint>();
            int entityIndex = 0;
            while (entityIndex < group.EntityIds.Count)
            {
                uint entityId = group.EntityIds[entityIndex];
                if (entityId == 0
                    || !itemIds.Contains(entityId)
                    || !entities.Add(entityId)
                    || (group.Type == "altr" && !alternateIds.Add(entityId)))
                {
                    return AvifResultCode.InvalidArgument;
                }

                entityIndex++;
            }

            groupIndex++;
        }

        groupIndex = 0;
        while (groupIndex < request.Groups.Count)
        {
            CanonicalEntityGroup group = request.Groups[groupIndex];
            int entityIndex = 0;
            while (entityIndex < (group.EntityIds ?? throw new InvalidOperationException("Invalid internal state.")).Count)
            {
                if (groupIds.Contains(group.EntityIds[entityIndex]))
                {
                    return AvifResultCode.InvalidArgument;
                }

                entityIndex++;
            }

            groupIndex++;
        }

        return AvifResultCode.Success;
    }

    static int FindItemIndex(
        List<CanonicalItemPayload> items,
        uint itemId)
    {
        int index = 0;
        while (index < items.Count)
        {
            if (items[index].ItemId == itemId)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    internal static List<string> SortBrands(List<string>? brands)
    {
        List<string> sorted = brands == null
            ? new List<string>() : new List<string>(brands);
        int index = 1;
        while (index < sorted.Count)
        {
            string value = sorted[index];
            int insertion = index;
            while (insertion > 0
                && string.CompareOrdinal(sorted[insertion - 1], value) > 0)
            {
                sorted[insertion] = sorted[insertion - 1];
                insertion--;
            }

            sorted[insertion] = value;
            index++;
        }

        return sorted;
    }

    internal static List<CanonicalItemPayload> SortItems(
        List<CanonicalItemPayload>? items)
    {
        List<CanonicalItemPayload> sorted = items == null
            ? new List<CanonicalItemPayload>()
            : new List<CanonicalItemPayload>(items);
        int index = 1;
        while (index < sorted.Count)
        {
            CanonicalItemPayload value = sorted[index];
            int insertion = index;
            while (insertion > 0 && sorted[insertion - 1].ItemId > value.ItemId)
            {
                sorted[insertion] = sorted[insertion - 1];
                insertion--;
            }

            sorted[insertion] = value;
            index++;
        }

        return sorted;
    }

    internal static List<CanonicalItemReference> SortReferences(
        List<CanonicalItemReference>? references)
    {
        List<CanonicalItemReference> sorted = references == null
            ? new List<CanonicalItemReference>()
            : new List<CanonicalItemReference>(references);
        int index = 1;
        while (index < sorted.Count)
        {
            CanonicalItemReference value = sorted[index];
            int insertion = index;
            while (insertion > 0
                && CompareReferences(sorted[insertion - 1], value) > 0)
            {
                sorted[insertion] = sorted[insertion - 1];
                insertion--;
            }

            sorted[insertion] = value;
            index++;
        }

        return sorted;
    }

    internal static int CompareReferences(
        CanonicalItemReference left,
        CanonicalItemReference right)
    {
        int typeComparison = string.CompareOrdinal(left.Type, right.Type);
        if (typeComparison != 0)
        {
            return typeComparison;
        }

        if (left.FromItemId < right.FromItemId)
        {
            return -1;
        }

        if (left.FromItemId > right.FromItemId)
        {
            return 1;
        }

        return 0;
    }

    internal static List<CanonicalEntityGroup> SortGroups(
        List<CanonicalEntityGroup>? groups)
    {
        List<CanonicalEntityGroup> sorted = groups == null
            ? new List<CanonicalEntityGroup>()
            : new List<CanonicalEntityGroup>(groups);
        int index = 1;
        while (index < sorted.Count)
        {
            CanonicalEntityGroup value = sorted[index];
            int insertion = index;
            while (insertion > 0
                && CompareGroups(sorted[insertion - 1], value) > 0)
            {
                sorted[insertion] = sorted[insertion - 1];
                insertion--;
            }

            sorted[insertion] = value;
            index++;
        }

        return sorted;
    }

    internal static int CompareGroups(
        CanonicalEntityGroup left,
        CanonicalEntityGroup right)
    {
        if (left.GroupId < right.GroupId)
        {
            return -1;
        }

        if (left.GroupId > right.GroupId)
        {
            return 1;
        }

        return string.CompareOrdinal(left.Type, right.Type);
    }

    internal static List<byte[]> InternProperties(List<CanonicalItemPayload> items)
    {
        List<byte[]> properties = new List<byte[]>();
        int itemIndex = 0;
        while (itemIndex < items.Count)
        {
            CanonicalItemPayload item = items[itemIndex];
            int associationIndex = 0;
            while (associationIndex < item.Properties.Count)
            {
                byte[] raw = item.Properties[associationIndex].RawProperty;
                if (FindPropertyIndex(properties, raw) == 0)
                {
                    int insertion = 0;
                    while (insertion < properties.Count
                        && CompareRawProperties(properties[insertion], raw) < 0)
                    {
                        insertion++;
                    }

                    properties.Insert(insertion, raw);
                }

                associationIndex++;
            }

            itemIndex++;
        }

        return properties;
    }

    internal static int FindPropertyIndex(List<byte[]> properties, byte[] raw)
    {
        int index = 0;
        while (index < properties.Count)
        {
            if (RawPropertiesEqual(properties[index], raw))
            {
                return index + 1;
            }

            index++;
        }

        return 0;
    }

    internal static int CompareRawProperties(byte[] left, byte[] right)
    {
        int length = left.Length < right.Length ? left.Length : right.Length;
        int index = 0;
        while (index < length)
        {
            if (left[index] < right[index])
            {
                return -1;
            }

            if (left[index] > right[index])
            {
                return 1;
            }

            index++;
        }

        if (left.Length < right.Length)
        {
            return -1;
        }

        return left.Length > right.Length ? 1 : 0;
    }

    internal static bool RawPropertiesEqual(byte[] left, byte[] right)
    {
        return left != null
            && right != null
            && CompareRawProperties(left, right) == 0;
    }

    internal static CanonicalPropertyProbe ProbeProperty(byte[] raw)
    {
        if (raw == null || raw.Length < 8)
        {
            return new CanonicalPropertyProbe(
                AvifResultCode.InvalidArgument,
                string.Empty);
        }

        uint size32 = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(0, 4));
        long declaredSize = size32;
        int headerSize = 8;
        if (size32 == 1)
        {
            if (raw.Length < 16)
            {
                return new CanonicalPropertyProbe(
                    AvifResultCode.InvalidArgument,
                    string.Empty);
            }

            ulong size64 = BinaryPrimitives.ReadUInt64BigEndian(raw.AsSpan(8, 8));
            if (size64 > long.MaxValue)
            {
                return new CanonicalPropertyProbe(
                    AvifResultCode.InvalidArgument,
                    string.Empty);
            }

            declaredSize = (long)size64;
            headerSize = 16;
        }
        else if (size32 == 0)
        {
            return new CanonicalPropertyProbe(
                AvifResultCode.InvalidArgument,
                string.Empty);
        }

        string type = new string(new char[]
        {
            (char)raw[4],
            (char)raw[5],
            (char)raw[6],
            (char)raw[7]
        });
        if (!IsoBoxType.IsValid(type)
            || type == "free"
            || type == "skip"
            || declaredSize != raw.LongLength)
        {
            return new CanonicalPropertyProbe(
                AvifResultCode.InvalidArgument,
                string.Empty);
        }

        if (type == "uuid")
        {
            headerSize += 16;
        }

        if (declaredSize < headerSize)
        {
            return new CanonicalPropertyProbe(
                AvifResultCode.InvalidArgument,
                string.Empty);
        }

        return new CanonicalPropertyProbe(AvifResultCode.Success, type);
    }

    internal static int CountAssociatedItems(List<CanonicalItemPayload> items)
    {
        int count = 0;
        int index = 0;
        while (index < items.Count)
        {
            if (items[index].Properties.Count > 0)
            {
                count++;
            }

            index++;
        }

        return count;
    }

    internal static bool RequiresWideAssociationItemIds(
        List<CanonicalItemPayload> items)
    {
        int index = 0;
        while (index < items.Count)
        {
            if (items[index].Properties.Count > 0
                && items[index].ItemId > ushort.MaxValue)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static bool RequiresWideReferenceIds(
        List<CanonicalItemReference> references)
    {
        int referenceIndex = 0;
        while (referenceIndex < references.Count)
        {
            CanonicalItemReference reference = references[referenceIndex];
            if (reference.FromItemId > ushort.MaxValue)
            {
                return true;
            }

            int targetIndex = 0;
            while (targetIndex < (reference.ToItemIds ?? throw new InvalidOperationException("Invalid internal state.")).Count)
            {
                if (reference.ToItemIds[targetIndex] > ushort.MaxValue)
                {
                    return true;
                }

                targetIndex++;
            }

            referenceIndex++;
        }

        return false;
    }

    internal static long MeasureProperties(
        List<CanonicalItemPayload> items,
        List<byte[]> properties)
    {
        long propertyContainerSize = 8;
        int propertyIndex = 0;
        while (propertyIndex < properties.Count)
        {
            propertyContainerSize = AddOrFail(
                propertyContainerSize,
                properties[propertyIndex].LongLength);
            if (propertyContainerSize < 0)
            {
                return -1;
            }

            propertyIndex++;
        }

        long size = AddOrFail(8, propertyContainerSize);
        int associatedItemCount = CountAssociatedItems(items);
        if (associatedItemCount == 0)
        {
            return size;
        }

        int idSize = RequiresWideAssociationItemIds(items) ? 4 : 2;
        int associationSize = properties.Count > 127 ? 2 : 1;
        long associationBoxSize = 16;
        int itemIndex = 0;
        while (itemIndex < items.Count)
        {
            int associationCount = items[itemIndex].Properties.Count;
            if (associationCount > 0)
            {
                long entrySize = idSize + 1 + (long)associationCount * associationSize;
                associationBoxSize = AddOrFail(associationBoxSize, entrySize);
                if (associationBoxSize < 0)
                {
                    return -1;
                }
            }

            itemIndex++;
        }

        return AddOrFail(size, associationBoxSize);
    }

    internal static long MeasureReferences(List<CanonicalItemReference> references)
    {
        if (references.Count == 0)
        {
            return 0;
        }

        int idSize = RequiresWideReferenceIds(references) ? 4 : 2;
        long size = 12;
        int index = 0;
        while (index < references.Count)
        {
            long childSize = 8 + idSize + 2
                + (long)(references[index].ToItemIds ?? throw new InvalidOperationException("Invalid internal state.")).Count * idSize;
            size = AddOrFail(size, childSize);
            if (size < 0)
            {
                return -1;
            }

            index++;
        }

        return size;
    }

    internal static long MeasureGroups(List<CanonicalEntityGroup> groups)
    {
        if (groups.Count == 0)
        {
            return 0;
        }

        long size = 8;
        int index = 0;
        while (index < groups.Count)
        {
            long childSize = 20 + (long)(groups[index].EntityIds ?? throw new InvalidOperationException("Invalid internal state.")).Count * 4;
            size = AddOrFail(size, childSize);
            if (size < 0)
            {
                return -1;
            }

            index++;
        }

        return size;
    }

    internal static bool RequiresWideItemIds(List<CanonicalItemPayload> items)
    {
        if (items.Count > ushort.MaxValue)
        {
            return true;
        }

        int index = 0;
        while (index < items.Count)
        {
            if (items[index].ItemId > ushort.MaxValue)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static bool HasWideLength(List<CanonicalItemPayload> items)
    {
        int index = 0;
        while (index < items.Count)
        {
            if ((ulong)items[index].Length > uint.MaxValue)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static long SumPayloadLengths(List<CanonicalItemPayload> items)
    {
        long total = 0;
        int index = 0;
        while (index < items.Count)
        {
            if (items[index].SharedDataItemId == 0)
            {
                total = AddOrFail(total, items[index].Length);
                if (total < 0)
                {
                    return -1;
                }
            }

            index++;
        }

        return total;
    }

    internal static long MeasureItemInfo(List<CanonicalItemPayload> items)
    {
        long size = 16;
        int index = 0;
        while (index < items.Count)
        {
            long nameBytes = Encoding.UTF8.GetByteCount(
                items[index].Name ?? string.Empty);
            long entrySize = AddOrFail(23, nameBytes);
            if (items[index].ItemType == "mime")
            {
                entrySize = AddOrFail(
                    entrySize,
                    Encoding.UTF8.GetByteCount(
                        items[index].ContentType ?? string.Empty) + 1L);
                entrySize = AddOrFail(
                    entrySize,
                    Encoding.UTF8.GetByteCount(
                        items[index].ContentEncoding ?? string.Empty) + 1L);
            }
            else if (items[index].ItemType == "uri ")
            {
                entrySize = AddOrFail(
                    entrySize,
                    Encoding.UTF8.GetByteCount(items[index].ItemUriType) + 1L);
            }

            size = AddOrFail(size, entrySize);
            if (size < 0)
            {
                return -1;
            }

            index++;
        }

        return size;
    }

    internal static long MeasureItemLocation(
        int itemCount,
        int version,
        int idSize,
        int countSize,
        int offsetSize,
        int lengthSize)
    {
        long entrySize = idSize + 2 + 2 + 2 + offsetSize + lengthSize;
        CheckedIntegerResult entries = CheckedMath.MultiplyInt64(itemCount, entrySize);
        if (!entries.Success)
        {
            return -1;
        }

        long size = AddOrFail(14 + countSize, entries.Value);
        if (version != 1 && version != 2)
        {
            return -1;
        }

        return size;
    }

    internal static long GetMaximumItemOffset(
        List<CanonicalItemPayload> items,
        long firstOffset)
    {
        long maximum = firstOffset;
        long running = firstOffset;
        int index = 0;
        while (index < items.Count)
        {
            if (running > maximum)
            {
                maximum = running;
            }

            running = AddOrFail(running, items[index].Length);
            if (running < 0)
            {
                return -1;
            }

            index++;
        }

        return maximum;
    }

    internal static long AddOrFail(long left, long right)
    {
        CheckedIntegerResult result = CheckedMath.AddInt64(left, right);
        return result.Success ? result.Value : -1;
    }

    internal static AvifResultCode WriteSizedInteger(
        IsoBoxWriter writer,
        ulong value,
        int size)
    {
        if ((size == 4 && value > uint.MaxValue) || (size != 4 && size != 8))
        {
            return AvifResultCode.InvalidArgument;
        }

        byte[] bytes = new byte[size];
        if (size == 4)
        {
            BinaryPrimitives.WriteUInt32BigEndian(bytes, (uint)value);
        }
        else
        {
            BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        }

        return writer.Write(bytes);
    }

    internal static AvifResultCode WriteUInt16(IsoBoxWriter writer, ushort value)
    {
        byte[] bytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        return writer.Write(bytes);
    }

    internal static AvifResultCode WriteNullTerminatedString(
        IsoBoxWriter writer,
        string? value)
    {
        AvifResultCode code = writer.Write(Encoding.UTF8.GetBytes(
            value ?? string.Empty));
        if (code == AvifResultCode.Success)
        {
            code = writer.Write(new byte[] { 0 });
        }

        return code;
    }

    internal static AvifResultCode WriteUInt8(IsoBoxWriter writer, byte value)
    {
        return writer.Write(new byte[] { value });
    }

    internal static AvifResultCode WriteUInt32(IsoBoxWriter writer, uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        return writer.Write(bytes);
    }

    internal static void WriteFourCc(Span<byte> destination, string? value)
    {
        destination[0] = (byte)(value ?? throw new InvalidOperationException("Invalid internal state."))[0];
        destination[1] = (byte)value[1];
        destination[2] = (byte)value[2];
        destination[3] = (byte)value[3];
    }
}
