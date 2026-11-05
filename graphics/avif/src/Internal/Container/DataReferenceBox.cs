using System;
using System.Collections.Generic;

namespace avif;

internal class DataReferenceBoxParseResult
{
    internal AvifResultCode Code;
    internal int Version;
    internal int Flags;
    internal List<ParsedDataReference> References;

    internal DataReferenceBoxParseResult(
        AvifResultCode code,
        int version,
        int flags,
        List<ParsedDataReference> references)
    {
        Code = code;
        Version = version;
        Flags = flags;
        References = references;
    }
}

internal static class DataInformationBox
{
    internal static DataReferenceBoxParseResult Parse(
        ReadOnlySpan<byte> source,
        IsoBox box,
        ResourceGuard resources)
    {
        List<ParsedDataReference> references = new List<ParsedDataReference>();
        if (box == null || box.Type != "dinf" || resources == null)
        {
            return CreateResult(AvifResultCode.InvalidArgument, 0, 0, references);
        }

        IsoBox? dataReferenceBox = null;
        int childIndex = 0;
        while (childIndex < box.Children.Count)
        {
            IsoBox child = box.Children[childIndex];
            if (child.Type == "free" || child.Type == "skip")
            {
                childIndex++;
                continue;
            }

            if (child.Type != "dref")
            {
                childIndex++;
                continue;
            }

            if (dataReferenceBox != null)
            {
                return CreateResult(
                    AvifResultCode.InvalidContainer,
                    0,
                    0,
                    references);
            }

            dataReferenceBox = child;
            childIndex++;
        }

        if (dataReferenceBox == null)
        {
            return CreateResult(
                AvifResultCode.InvalidContainer,
                0,
                0,
                references);
        }

        return ParseReferences(source, dataReferenceBox, resources);
    }

    internal static DataReferenceBoxParseResult ParseReferences(
        ReadOnlySpan<byte> source,
        IsoBox box,
        ResourceGuard resources)
    {
        List<ParsedDataReference> references = new List<ParsedDataReference>();
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            return CreateResult(fullBox.Code, 0, 0, references);
        }

        if (fullBox.Version != 0)
        {
            return CreateResult(
                AvifResultCode.UnsupportedFeature,
                fullBox.Version,
                fullBox.Flags,
                references);
        }

        if (fullBox.Flags != 0 || fullBox.PayloadSize < 4)
        {
            return CreateResult(
                AvifResultCode.InvalidContainer,
                fullBox.Version,
                fullBox.Flags,
                references);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset,
            0,
            4,
            source.Length);
        if (!range.Success)
        {
            return CreateResult(
                range.Code,
                fullBox.Version,
                fullBox.Flags,
                references);
        }

        int countOffset = (int)fullBox.PayloadOffset;
        ByteReader reader = new ByteReader(4);
        uint entryCount = reader.ReadUInt32BigEndian(source.Slice(countOffset, 4));
        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(
                AvifResultCode.InvalidContainer,
                fullBox.Version,
                fullBox.Flags,
                references);
        }

        if (entryCount == 0
            || entryCount > int.MaxValue
            || entryCount > ushort.MaxValue
            || entryCount > resources.Limits.MaxReferenceCount)
        {
            return CreateResult(
                entryCount == 0
                    ? AvifResultCode.InvalidContainer
                    : AvifResultCode.LimitExceeded,
                fullBox.Version,
                fullBox.Flags,
                references);
        }

        AvifResultCode countCode = resources.CountReferences((int)entryCount);
        if (countCode != AvifResultCode.Success)
        {
            return CreateResult(
                countCode,
                fullBox.Version,
                fullBox.Flags,
                references);
        }

        int actualCount = 0;
        int childIndex = 0;
        while (childIndex < box.Children.Count)
        {
            IsoBox child = box.Children[childIndex];
            if (child.Type == "free" || child.Type == "skip")
            {
                childIndex++;
                continue;
            }

            if (child.Type != "url " && child.Type != "urn ")
            {
                childIndex++;
                continue;
            }

            actualCount++;
            ParsedDataReference? parsed = ParseEntry(
                source,
                child,
                actualCount);
            if (parsed == null)
            {
                return CreateResult(
                    GetEntryError(source, child),
                    fullBox.Version,
                    fullBox.Flags,
                    references);
            }

            references.Add(parsed);
            childIndex++;
        }

        if ((uint)actualCount != entryCount)
        {
            return CreateResult(
                AvifResultCode.InvalidContainer,
                fullBox.Version,
                fullBox.Flags,
                references);
        }

        return CreateResult(
            AvifResultCode.Success,
            fullBox.Version,
            fullBox.Flags,
            references);
    }

    internal static ParsedDataReference? ParseEntry(
        ReadOnlySpan<byte> source,
        IsoBox box,
        int index)
    {
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success
            || fullBox.Version != 0
            || (fullBox.Flags != 0 && fullBox.Flags != 1))
        {
            return null;
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset,
            0,
            fullBox.PayloadSize,
            source.Length);
        if (!range.Success)
        {
            return null;
        }

        ReadOnlySpan<byte> payload = source.Slice(
            (int)fullBox.PayloadOffset,
            (int)fullBox.PayloadSize);
        if (fullBox.Flags == 1)
        {
            if (box.Type != "url " || payload.Length != 0)
            {
                return null;
            }

            return new ParsedDataReference(
                index,
                ParsedDataReferenceKind.Url,
                box.Type,
                true,
                string.Empty,
                string.Empty);
        }

        ByteReader reader = new ByteReader(payload.Length);
        if (box.Type == "url ")
        {
            string location = reader.ReadNullTerminatedUtf8(payload);
            if (reader.Code != AvifResultCode.Success
                || reader.Remaining() != 0
                || location.Length == 0)
            {
                return null;
            }

            return new ParsedDataReference(
                index,
                ParsedDataReferenceKind.Url,
                box.Type,
                false,
                string.Empty,
                location);
        }

        string name = reader.ReadNullTerminatedUtf8(payload);
        string urnLocation = reader.ReadNullTerminatedUtf8(payload);
        if (reader.Code != AvifResultCode.Success
            || reader.Remaining() != 0
            || name.Length == 0)
        {
            return null;
        }

        return new ParsedDataReference(
            index,
            ParsedDataReferenceKind.Urn,
            box.Type,
            false,
            name,
            urnLocation);
    }

    internal static AvifResultCode GetEntryError(
        ReadOnlySpan<byte> source,
        IsoBox box)
    {
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            return fullBox.Code;
        }

        if (fullBox.Version != 0)
        {
            return AvifResultCode.UnsupportedFeature;
        }

        return AvifResultCode.InvalidContainer;
    }

    internal static DataReferenceBoxParseResult CreateResult(
        AvifResultCode code,
        int version,
        int flags,
        List<ParsedDataReference> references)
    {
        return new DataReferenceBoxParseResult(code, version, flags, references);
    }
}
