using System;
using System.Collections.Generic;

namespace avif;

internal class ItemReferenceBoxParseResult
{
    internal AvifResultCode Code;
    internal int Version;
    internal int Flags;
    internal List<ParsedItemReference> References;

    internal ItemReferenceBoxParseResult(
        AvifResultCode code,
        int version,
        int flags,
        List<ParsedItemReference> references)
    {
        Code = code;
        Version = version;
        Flags = flags;
        References = references;
    }
}

internal static class ItemReferenceBox
{
    internal static ItemReferenceBoxParseResult Parse(
        ReadOnlySpan<byte> source,
        IsoBox box,
        ResourceGuard resources)
    {
        List<ParsedItemReference> references = new List<ParsedItemReference>();
        if (box == null || box.Type != "iref" || resources == null)
        {
            return CreateResult(AvifResultCode.InvalidArgument, 0, 0, references);
        }

        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            return CreateResult(fullBox.Code, 0, 0, references);
        }

        if (fullBox.Version != 0 && fullBox.Version != 1)
        {
            return CreateResult(
                AvifResultCode.UnsupportedFeature,
                fullBox.Version,
                fullBox.Flags,
                references);
        }

        if (fullBox.Flags != 0)
        {
            return CreateResult(
                AvifResultCode.InvalidContainer,
                fullBox.Version,
                fullBox.Flags,
                references);
        }

        HashSet<string> sources = new HashSet<string>();
        int childIndex = 0;
        while (childIndex < box.Children.Count)
        {
            IsoBox child = box.Children[childIndex];
            if (child.Type == "free" || child.Type == "skip")
            {
                childIndex++;
                continue;
            }

            if (child.IsUuid)
            {
                return CreateResult(
                    AvifResultCode.InvalidContainer,
                    fullBox.Version,
                    fullBox.Flags,
                    references);
            }

            CheckedRangeResult range = CheckedMath.ResolveRange(
                child.PayloadOffset,
                0,
                child.PayloadSize,
                source.Length);
            if (!range.Success)
            {
                return CreateResult(
                    range.Code,
                    fullBox.Version,
                    fullBox.Flags,
                    references);
            }

            ReadOnlySpan<byte> payload = source.Slice(
                (int)child.PayloadOffset,
                (int)child.PayloadSize);
            ByteReader reader = new ByteReader(payload.Length);
            uint fromItemId;
            if (fullBox.Version == 0)
            {
                fromItemId = reader.ReadUInt16BigEndian(payload);
            }
            else
            {
                fromItemId = reader.ReadUInt32BigEndian(payload);
            }

            ushort referenceCount = reader.ReadUInt16BigEndian(payload);
            if (reader.Code != AvifResultCode.Success)
            {
                return CreateResult(
                    AvifResultCode.InvalidContainer,
                    fullBox.Version,
                    fullBox.Flags,
                    references);
            }

            if (fromItemId == 0 || referenceCount == 0)
            {
                return CreateResult(
                    AvifResultCode.InvalidItem,
                    fullBox.Version,
                    fullBox.Flags,
                    references);
            }

            string sourceKey = $"{child.Type}:{fromItemId}";
            if (!sources.Add(sourceKey))
            {
                return CreateResult(
                    AvifResultCode.InvalidItem,
                    fullBox.Version,
                    fullBox.Flags,
                    references);
            }

            AvifResultCode countCode = resources.CountReferences(referenceCount);
            if (countCode != AvifResultCode.Success)
            {
                return CreateResult(
                    countCode,
                    fullBox.Version,
                    fullBox.Flags,
                    references);
            }

            List<uint> toItemIds = new List<uint>();
            HashSet<uint> targets = new HashSet<uint>();
            int targetIndex = 0;
            while (targetIndex < referenceCount)
            {
                uint toItemId;
                if (fullBox.Version == 0)
                {
                    toItemId = reader.ReadUInt16BigEndian(payload);
                }
                else
                {
                    toItemId = reader.ReadUInt32BigEndian(payload);
                }

                if (reader.Code != AvifResultCode.Success)
                {
                    return CreateResult(
                        AvifResultCode.InvalidContainer,
                        fullBox.Version,
                        fullBox.Flags,
                        references);
                }

                if (toItemId == 0 || toItemId == fromItemId || !targets.Add(toItemId))
                {
                    return CreateResult(
                        AvifResultCode.InvalidItem,
                        fullBox.Version,
                        fullBox.Flags,
                        references);
                }

                toItemIds.Add(toItemId);
                targetIndex++;
            }

            if (reader.Remaining() != 0)
            {
                return CreateResult(
                    AvifResultCode.InvalidContainer,
                    fullBox.Version,
                    fullBox.Flags,
                    references);
            }

            references.Add(new ParsedItemReference(
                GetKind(child.Type),
                child.Type,
                child.TypeValue,
                fromItemId,
                toItemIds));
            childIndex++;
        }

        return CreateResult(
            AvifResultCode.Success,
            fullBox.Version,
            fullBox.Flags,
            references);
    }

    internal static ParsedItemReferenceKind GetKind(string type)
    {
        if (type == "auxl")
        {
            return ParsedItemReferenceKind.Auxiliary;
        }

        if (type == "thmb")
        {
            return ParsedItemReferenceKind.Thumbnail;
        }

        if (type == "dimg")
        {
            return ParsedItemReferenceKind.DerivedImage;
        }

        if (type == "prem")
        {
            return ParsedItemReferenceKind.Premultiplied;
        }

        if (type == "cdsc")
        {
            return ParsedItemReferenceKind.ContentDescription;
        }

        return ParsedItemReferenceKind.Unknown;
    }

    internal static ItemReferenceBoxParseResult CreateResult(
        AvifResultCode code,
        int version,
        int flags,
        List<ParsedItemReference> references)
    {
        return new ItemReferenceBoxParseResult(code, version, flags, references);
    }
}
