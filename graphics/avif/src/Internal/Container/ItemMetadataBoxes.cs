using System;
using System.Collections.Generic;

namespace avif;

internal class HandlerBox
{
    internal AvifResultCode Code;
    internal int Version;
    internal int Flags;
    internal string HandlerType;
    internal string Name;

    internal HandlerBox()
    {
        Code = AvifResultCode.Success;
        Version = 0;
        Flags = 0;
        HandlerType = string.Empty;
        Name = string.Empty;
    }

    internal static HandlerBox Parse(ReadOnlySpan<byte> source, IsoBox? box)
    {
        HandlerBox result = new HandlerBox();
        if (box == null || box.Type != "hdlr" || box.IsUuid)
        {
            result.Code = AvifResultCode.InvalidArgument;
            return result;
        }

        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        result.Version = fullBox.Version;
        result.Flags = fullBox.Flags;
        if (fullBox.Code != AvifResultCode.Success)
        {
            result.Code = fullBox.Code;
            return result;
        }

        if (fullBox.Version != 0)
        {
            result.Code = AvifResultCode.UnsupportedFeature;
            return result;
        }

        if (fullBox.Flags != 0 || fullBox.PayloadSize < 20)
        {
            result.Code = AvifResultCode.InvalidContainer;
            return result;
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset,
            0,
            fullBox.PayloadSize,
            source.Length);
        if (!range.Success)
        {
            result.Code = range.Code;
            return result;
        }

        ReadOnlySpan<byte> payload = source.Slice(
            (int)fullBox.PayloadOffset,
            (int)fullBox.PayloadSize);
        ByteReader reader = new ByteReader(payload.Length);
        uint predefined = reader.ReadUInt32BigEndian(payload);
        FourCc? handlerType = reader.ReadFourCc(payload);
        uint reserved0 = reader.ReadUInt32BigEndian(payload);
        uint reserved1 = reader.ReadUInt32BigEndian(payload);
        uint reserved2 = reader.ReadUInt32BigEndian(payload);
        if (reader.Code != AvifResultCode.Success)
        {
            result.Code = NormalizeReaderCode(reader.Code);
            return result;
        }

        if (predefined != 0 || reserved0 != 0 || reserved1 != 0 || reserved2 != 0)
        {
            result.Code = AvifResultCode.InvalidContainer;
            return result;
        }

        result.HandlerType = (handlerType ?? throw new InvalidOperationException("Invalid internal state.")).Text;
        if (reader.Remaining() > 0)
        {
            result.Name = reader.ReadNullTerminatedUtf8(payload);
            if (reader.Code != AvifResultCode.Success)
            {
                result.Code = NormalizeReaderCode(reader.Code);
                return result;
            }

            while (reader.Position < reader.Limit)
            {
                if (reader.ReadUInt8(payload) != 0)
                {
                    result.Code = AvifResultCode.InvalidContainer;
                    return result;
                }
            }
        }

        return result;
    }

    internal static AvifResultCode NormalizeReaderCode(AvifResultCode code)
    {
        if (code == AvifResultCode.TruncatedData)
        {
            return AvifResultCode.InvalidContainer;
        }

        return code;
    }
}

internal class PrimaryItemBox
{
    internal AvifResultCode Code;
    internal int Version;
    internal int Flags;
    internal uint ItemId;

    internal PrimaryItemBox()
    {
        Code = AvifResultCode.Success;
        Version = 0;
        Flags = 0;
        ItemId = 0;
    }

    internal static PrimaryItemBox Parse(ReadOnlySpan<byte> source, IsoBox box)
    {
        PrimaryItemBox result = new PrimaryItemBox();
        if (box == null || box.Type != "pitm" || box.IsUuid)
        {
            result.Code = AvifResultCode.InvalidArgument;
            return result;
        }

        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        result.Version = fullBox.Version;
        result.Flags = fullBox.Flags;
        if (fullBox.Code != AvifResultCode.Success)
        {
            result.Code = fullBox.Code;
            return result;
        }

        if (fullBox.Version != 0 && fullBox.Version != 1)
        {
            result.Code = AvifResultCode.UnsupportedFeature;
            return result;
        }

        int expectedSize = fullBox.Version == 0 ? 2 : 4;
        if (fullBox.Flags != 0 || fullBox.PayloadSize != expectedSize)
        {
            result.Code = AvifResultCode.InvalidContainer;
            return result;
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset,
            0,
            expectedSize,
            source.Length);
        if (!range.Success)
        {
            result.Code = range.Code;
            return result;
        }

        ReadOnlySpan<byte> payload = source.Slice((int)fullBox.PayloadOffset, expectedSize);
        ByteReader reader = new ByteReader(payload.Length);
        if (fullBox.Version == 0)
        {
            result.ItemId = reader.ReadUInt16BigEndian(payload);
        }
        else
        {
            result.ItemId = reader.ReadUInt32BigEndian(payload);
        }

        if (reader.Code != AvifResultCode.Success)
        {
            result.Code = HandlerBox.NormalizeReaderCode(reader.Code);
        }
        else if (result.ItemId == 0)
        {
            result.Code = AvifResultCode.InvalidItem;
        }

        return result;
    }
}

internal class ItemInfoEntryParseResult
{
    internal AvifResultCode Code;
    internal ParsedItem? Item;

    internal ItemInfoEntryParseResult(AvifResultCode code, ParsedItem? item)
    {
        Code = code;
        Item = item;
    }
}

internal static class ItemInfoEntryBox
{
    internal static ItemInfoEntryParseResult Parse(ReadOnlySpan<byte> source, IsoBox box)
    {
        if (box == null || box.Type != "infe" || box.IsUuid)
        {
            return new ItemInfoEntryParseResult(AvifResultCode.InvalidArgument, null);
        }

        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            return new ItemInfoEntryParseResult(fullBox.Code, null);
        }

        if (fullBox.Version != 2 && fullBox.Version != 3)
        {
            return new ItemInfoEntryParseResult(AvifResultCode.UnsupportedFeature, null);
        }

        if ((fullBox.Flags & -2) != 0)
        {
            return new ItemInfoEntryParseResult(AvifResultCode.InvalidItem, null);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset,
            0,
            fullBox.PayloadSize,
            source.Length);
        if (!range.Success)
        {
            return new ItemInfoEntryParseResult(range.Code, null);
        }

        ReadOnlySpan<byte> payload = source.Slice(
            (int)fullBox.PayloadOffset,
            (int)fullBox.PayloadSize);
        ByteReader reader = new ByteReader(payload.Length);
        uint itemId;
        if (fullBox.Version == 2)
        {
            itemId = reader.ReadUInt16BigEndian(payload);
        }
        else
        {
            itemId = reader.ReadUInt32BigEndian(payload);
        }

        ushort protectionIndex = reader.ReadUInt16BigEndian(payload);
        FourCc? itemTypeValue = reader.ReadFourCc(payload);
        if (reader.Code != AvifResultCode.Success)
        {
            return new ItemInfoEntryParseResult(
                HandlerBox.NormalizeReaderCode(reader.Code),
                null);
        }

        if (itemId == 0)
        {
            return new ItemInfoEntryParseResult(AvifResultCode.InvalidItem, null);
        }

        string itemType = (itemTypeValue ?? throw new InvalidOperationException("Invalid internal state.")).Text;
        string name = reader.ReadNullTerminatedUtf8(payload);
        string contentType = string.Empty;
        string contentEncoding = string.Empty;
        string itemUriType = string.Empty;
        if (itemType == "mime")
        {
            contentType = reader.ReadNullTerminatedUtf8(payload);
            contentEncoding = reader.ReadNullTerminatedUtf8(payload);
        }
        else if (itemType == "uri ")
        {
            itemUriType = reader.ReadNullTerminatedUtf8(payload);
        }

        if (reader.Code != AvifResultCode.Success)
        {
            return new ItemInfoEntryParseResult(
                HandlerBox.NormalizeReaderCode(reader.Code),
                null);
        }

        if (reader.Remaining() != 0)
        {
            return new ItemInfoEntryParseResult(AvifResultCode.InvalidItem, null);
        }

        ParsedItem item = new ParsedItem(
            itemId,
            fullBox.Version,
            (fullBox.Flags & 1) != 0,
            protectionIndex,
            itemType,
            name,
            contentType,
            contentEncoding,
            itemUriType);
        return new ItemInfoEntryParseResult(AvifResultCode.Success, item);
    }
}

internal class ItemInfoBoxParseResult
{
    internal AvifResultCode Code;
    internal int Version;
    internal uint DeclaredEntryCount;
    internal List<ParsedItem> Items;

    internal ItemInfoBoxParseResult(
        AvifResultCode code,
        int version,
        uint declaredEntryCount,
        List<ParsedItem> items)
    {
        Code = code;
        Version = version;
        DeclaredEntryCount = declaredEntryCount;
        Items = items;
    }
}

internal static class ItemInfoBox
{
    internal static ItemInfoBoxParseResult Parse(
        ReadOnlySpan<byte> source,
        IsoBox box,
        ResourceGuard resources)
    {
        List<ParsedItem> items = new List<ParsedItem>();
        if (box == null || box.Type != "iinf" || resources == null)
        {
            return new ItemInfoBoxParseResult(
                AvifResultCode.InvalidArgument,
                0,
                0,
                items);
        }

        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            return new ItemInfoBoxParseResult(fullBox.Code, 0, 0, items);
        }

        if (fullBox.Version != 0 && fullBox.Version != 1)
        {
            return new ItemInfoBoxParseResult(
                AvifResultCode.UnsupportedFeature,
                fullBox.Version,
                0,
                items);
        }

        int countSize = fullBox.Version == 0 ? 2 : 4;
        if (fullBox.Flags != 0 || fullBox.PayloadSize < countSize)
        {
            return new ItemInfoBoxParseResult(
                AvifResultCode.InvalidContainer,
                fullBox.Version,
                0,
                items);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset,
            0,
            countSize,
            source.Length);
        if (!range.Success)
        {
            return new ItemInfoBoxParseResult(
                range.Code,
                fullBox.Version,
                0,
                items);
        }

        ReadOnlySpan<byte> countBytes = source.Slice((int)fullBox.PayloadOffset, countSize);
        ByteReader reader = new ByteReader(countSize);
        uint entryCount;
        if (fullBox.Version == 0)
        {
            entryCount = reader.ReadUInt16BigEndian(countBytes);
        }
        else
        {
            entryCount = reader.ReadUInt32BigEndian(countBytes);
        }

        if (entryCount > int.MaxValue || entryCount > resources.Limits.MaxItemCount)
        {
            return new ItemInfoBoxParseResult(
                AvifResultCode.LimitExceeded,
                fullBox.Version,
                entryCount,
                items);
        }

        AvifResultCode itemCountCode = resources.CountItems((int)entryCount);
        if (itemCountCode != AvifResultCode.Success)
        {
            return new ItemInfoBoxParseResult(
                itemCountCode,
                fullBox.Version,
                entryCount,
                items);
        }

        HashSet<uint> itemIds = new HashSet<uint>();
        int childIndex = 0;
        while (childIndex < box.Children.Count)
        {
            IsoBox child = box.Children[childIndex];
            if (child.Type == "free" || child.Type == "skip")
            {
                childIndex++;
                continue;
            }

            if (child.Type != "infe")
            {
                return new ItemInfoBoxParseResult(
                    AvifResultCode.InvalidContainer,
                    fullBox.Version,
                    entryCount,
                    items);
            }

            ItemInfoEntryParseResult entry = ItemInfoEntryBox.Parse(source, child);
            if (entry.Code != AvifResultCode.Success)
            {
                return new ItemInfoBoxParseResult(
                    entry.Code,
                    fullBox.Version,
                    entryCount,
                    items);
            }

            if (!itemIds.Add((entry.Item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId()))
            {
                return new ItemInfoBoxParseResult(
                    AvifResultCode.InvalidItem,
                    fullBox.Version,
                    entryCount,
                    items);
            }

            items.Add(entry.Item);
            childIndex++;
        }

        if (items.Count != entryCount)
        {
            return new ItemInfoBoxParseResult(
                AvifResultCode.InvalidContainer,
                fullBox.Version,
                entryCount,
                items);
        }

        return new ItemInfoBoxParseResult(
            AvifResultCode.Success,
            fullBox.Version,
            entryCount,
            items);
    }
}
