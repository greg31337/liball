using System;
using System.Collections.Generic;

namespace avif;

internal class EntityGroupBoxParseResult
{
    internal AvifResultCode Code;
    internal List<ParsedEntityGroup> Groups;

    internal EntityGroupBoxParseResult(
        AvifResultCode code,
        List<ParsedEntityGroup> groups)
    {
        Code = code;
        Groups = groups;
    }
}

internal static class EntityGroupBox
{
    internal static EntityGroupBoxParseResult Parse(
        ReadOnlySpan<byte> source,
        IsoBox box,
        ResourceGuard resources)
    {
        List<ParsedEntityGroup> groups = new List<ParsedEntityGroup>();
        if (box == null || box.Type != "grpl" || resources == null)
        {
            return CreateResult(AvifResultCode.InvalidArgument, groups);
        }

        HashSet<uint> groupIds = new HashSet<uint>();
        HashSet<uint> alternateEntityIds = new HashSet<uint>();
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
                return CreateResult(AvifResultCode.InvalidContainer, groups);
            }

            IsoFullBox fullBox = IsoFullBox.Parse(source, child);
            if (fullBox.Code != AvifResultCode.Success)
            {
                return CreateResult(fullBox.Code, groups);
            }

            CheckedRangeResult range = CheckedMath.ResolveRange(
                fullBox.PayloadOffset,
                0,
                fullBox.PayloadSize,
                source.Length);
            if (!range.Success)
            {
                return CreateResult(range.Code, groups);
            }

            ReadOnlySpan<byte> payload = source.Slice(
                (int)fullBox.PayloadOffset,
                (int)fullBox.PayloadSize);
            ByteReader reader = new ByteReader(payload.Length);
            uint groupId = reader.ReadUInt32BigEndian(payload);
            uint entityCount = reader.ReadUInt32BigEndian(payload);
            if (reader.Code != AvifResultCode.Success)
            {
                return CreateResult(AvifResultCode.InvalidContainer, groups);
            }

            if (entityCount > int.MaxValue
                || entityCount > resources.Limits.MaxReferenceCount)
            {
                return CreateResult(AvifResultCode.LimitExceeded, groups);
            }

            AvifResultCode countCode = resources.CountReferences((int)entityCount);
            if (countCode != AvifResultCode.Success)
            {
                return CreateResult(countCode, groups);
            }

            if (!groupIds.Add(groupId))
            {
                return CreateResult(AvifResultCode.InvalidItem, groups);
            }

            ParsedEntityGroupKind kind = GetKind(child.Type);
            if (kind != ParsedEntityGroupKind.Unknown)
            {
                if (fullBox.Version != 0)
                {
                    return CreateResult(AvifResultCode.UnsupportedFeature, groups);
                }

                if (fullBox.Flags != 0)
                {
                    return CreateResult(AvifResultCode.InvalidContainer, groups);
                }
            }

            if (kind == ParsedEntityGroupKind.Stereo && entityCount != 2)
            {
                return CreateResult(AvifResultCode.InvalidItem, groups);
            }

            List<uint> entityIds = new List<uint>();
            HashSet<uint> entities = new HashSet<uint>();
            int entityIndex = 0;
            while (entityIndex < entityCount)
            {
                uint entityId = reader.ReadUInt32BigEndian(payload);
                if (reader.Code != AvifResultCode.Success)
                {
                    return CreateResult(AvifResultCode.InvalidContainer, groups);
                }

                if (entityId == 0 || !entities.Add(entityId))
                {
                    return CreateResult(AvifResultCode.InvalidItem, groups);
                }

                if (kind == ParsedEntityGroupKind.Alternate
                    && !alternateEntityIds.Add(entityId))
                {
                    return CreateResult(AvifResultCode.InvalidItem, groups);
                }

                entityIds.Add(entityId);
                entityIndex++;
            }

            byte[] rawData = System.Array.Empty<byte>();
            if (kind == ParsedEntityGroupKind.Unknown)
            {
                CheckedRangeResult rawRange = CheckedMath.ResolveRange(
                    child.Offset,
                    0,
                    child.Size,
                    source.Length);
                if (!rawRange.Success)
                {
                    return CreateResult(rawRange.Code, groups);
                }

                AvifResultCode reserveCode = resources.ReserveTemporaryBytes(child.Size);
                if (reserveCode != AvifResultCode.Success)
                {
                    return CreateResult(reserveCode, groups);
                }

                rawData = source.Slice((int)child.Offset, (int)child.Size).ToArray();
            }
            else if (reader.Remaining() != 0)
            {
                return CreateResult(AvifResultCode.InvalidContainer, groups);
            }

            groups.Add(new ParsedEntityGroup(
                kind,
                child.Type,
                child.TypeValue,
                fullBox.Version,
                fullBox.Flags,
                groupId,
                entityIds,
                new List<ParsedItem?>(),
                rawData));
            childIndex++;
        }

        int groupIndex = 0;
        while (groupIndex < groups.Count)
        {
            ParsedEntityGroup group = groups[groupIndex];
            int entityIndex = 0;
            while (entityIndex < group.GetEntityCount())
            {
                if (groupIds.Contains(group.GetEntityId(entityIndex)))
                {
                    return CreateResult(AvifResultCode.InvalidItem, groups);
                }

                entityIndex++;
            }

            groupIndex++;
        }

        return CreateResult(AvifResultCode.Success, groups);
    }

    internal static ParsedEntityGroupKind GetKind(string type)
    {
        if (type == "altr")
        {
            return ParsedEntityGroupKind.Alternate;
        }

        if (type == "ster")
        {
            return ParsedEntityGroupKind.Stereo;
        }

        return ParsedEntityGroupKind.Unknown;
    }

    internal static EntityGroupBoxParseResult CreateResult(
        AvifResultCode code,
        List<ParsedEntityGroup> groups)
    {
        return new EntityGroupBoxParseResult(code, groups);
    }
}
