using System;
using System.Collections.Generic;

namespace avif;

internal class ParsedAv1GroupDescription
{
    internal int ForwardDistance;

    internal ParsedAv1GroupDescription(int forwardDistance)
    {
        ForwardDistance = forwardDistance;
    }
}

internal class ParsedAv1GroupDescriptionSet
{
    internal string Type;
    internal uint DefaultIndex;
    internal List<ParsedAv1GroupDescription> Descriptions;

    internal ParsedAv1GroupDescriptionSet(string type)
    {
        Type = type;
        Descriptions = new List<ParsedAv1GroupDescription>();
    }
}

internal class ParsedAv1GroupMappingKey
{
    internal string Type;
    internal bool HasParameter;
    internal uint Parameter;

    internal ParsedAv1GroupMappingKey(
        string type,
        bool hasParameter,
        uint parameter)
    {
        Type = type;
        HasParameter = hasParameter;
        Parameter = parameter;
    }
}

internal static class Av1SampleGroupParser
{
    internal static AvifResultCode Parse(
        ReadOnlySpan<byte> source,
        IsoBox sampleTableBox,
        ResourceGuard resources,
        DiagnosticLog diagnostics,
        ParsedSampleTable table)
    {
        if (sampleTableBox == null || sampleTableBox.Type != "stbl"
            || resources == null || diagnostics == null || table == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        List<ParsedAv1GroupDescriptionSet> sets =
            new List<ParsedAv1GroupDescriptionSet>();
        int childIndex = 0;
        while (childIndex < sampleTableBox.Children.Count)
        {
            IsoBox child = sampleTableBox.Children[childIndex];
            if (child.Type == "sgpd")
            {
                AvifResultCode code = ParseDescriptions(
                    source, child, resources, diagnostics, sets);
                if (code != AvifResultCode.Success)
                {
                    return code;
                }
            }
            childIndex++;
        }

        List<ParsedAv1GroupMappingKey> keys =
            new List<ParsedAv1GroupMappingKey>();
        childIndex = 0;
        while (childIndex < sampleTableBox.Children.Count)
        {
            IsoBox child = sampleTableBox.Children[childIndex];
            if (child.Type == "sbgp")
            {
                AvifResultCode code = ParseMapping(
                    source, child, diagnostics, table, sets, keys);
                if (code != AvifResultCode.Success)
                {
                    return code;
                }
            }
            childIndex++;
        }
        return AvifResultCode.Success;
    }

    static AvifResultCode ParseDescriptions(
        ReadOnlySpan<byte> source,
        IsoBox box,
        ResourceGuard resources,
        DiagnosticLog diagnostics,
        List<ParsedAv1GroupDescriptionSet> sets)
    {
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success
            || fullBox.Flags != 0 || fullBox.Version < 0 || fullBox.Version > 2)
        {
            return Report(
                diagnostics,
                fullBox.Code == AvifResultCode.Success && fullBox.Version > 2
                    ? AvifResultCode.UnsupportedFeature : AvifResultCode.InvalidTrack,
                "The SampleGroupDescriptionBox header is invalid.",
                box.PayloadOffset);
        }
        ReadOnlySpan<byte> payload = GetPayload(
            source, fullBox, diagnostics,
            "The SampleGroupDescriptionBox payload is truncated.");
        if (payload.Length < 8)
        {
            return diagnostics.Code;
        }
        ByteReader reader = new ByteReader(payload.Length);
        FourCc? groupingTypeValue = reader.ReadFourCc(payload);
        string groupingType = groupingTypeValue == null
            ? string.Empty : groupingTypeValue.Text;
        if (!IsRecognized(groupingType))
        {
            return AvifResultCode.Success;
        }
        if (FindSet(sets, groupingType) != null)
        {
            return Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "An AV1 grouping type has multiple SampleGroupDescriptionBox instances.",
                box.PayloadOffset);
        }

        uint defaultLength = 0;
        uint defaultIndex = 0;
        if (fullBox.Version >= 1)
        {
            defaultLength = reader.ReadUInt32BigEndian(payload);
        }
        if (fullBox.Version >= 2)
        {
            defaultIndex = reader.ReadUInt32BigEndian(payload);
        }
        uint entryCount = reader.ReadUInt32BigEndian(payload);
        if (reader.Code != AvifResultCode.Success
            || entryCount > (uint)resources.Limits.MaxSampleCount)
        {
            return Report(
                diagnostics,
                entryCount > (uint)resources.Limits.MaxSampleCount
                    ? AvifResultCode.LimitExceeded : AvifResultCode.InvalidTrack,
                "The AV1 sample-group description count is invalid or exceeds its limit.",
                fullBox.PayloadOffset);
        }

        ParsedAv1GroupDescriptionSet set =
            new ParsedAv1GroupDescriptionSet(groupingType);
        set.DefaultIndex = defaultIndex;
        uint entryIndex = 0;
        while (entryIndex < entryCount)
        {
            uint descriptionLength;
            if (fullBox.Version == 0)
            {
                descriptionLength = groupingType == "av1f" ? 1U : 0U;
            }
            else if (defaultLength == 0)
            {
                descriptionLength = reader.ReadUInt32BigEndian(payload);
            }
            else
            {
                descriptionLength = defaultLength;
            }
            uint expectedLength = groupingType == "av1f" ? 1U : 0U;
            if (reader.Code != AvifResultCode.Success
                || descriptionLength != expectedLength)
            {
                return Report(
                    diagnostics, AvifResultCode.InvalidTrack,
                    "An AV1 sample-group description has an invalid length.",
                    fullBox.PayloadOffset + reader.Position);
            }
            int forwardDistance = groupingType == "av1f"
                ? reader.ReadUInt8(payload) : -1;
            if (reader.Code != AvifResultCode.Success)
            {
                return Report(
                    diagnostics, AvifResultCode.InvalidTrack,
                    "An AV1 sample-group description is truncated.",
                    fullBox.PayloadOffset + reader.Position);
            }
            set.Descriptions.Add(new ParsedAv1GroupDescription(forwardDistance));
            entryIndex++;
        }
        if (reader.Position != payload.Length
            || defaultIndex > (uint)set.Descriptions.Count)
        {
            return Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "The SampleGroupDescriptionBox has trailing data or an invalid default index.",
                fullBox.PayloadOffset + reader.Position);
        }
        sets.Add(set);
        return AvifResultCode.Success;
    }

    static AvifResultCode ParseMapping(
        ReadOnlySpan<byte> source,
        IsoBox box,
        DiagnosticLog diagnostics,
        ParsedSampleTable table,
        List<ParsedAv1GroupDescriptionSet> sets,
        List<ParsedAv1GroupMappingKey> keys)
    {
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success
            || fullBox.Flags != 0
            || (fullBox.Version != 0 && fullBox.Version != 1))
        {
            return Report(
                diagnostics,
                fullBox.Code == AvifResultCode.Success && fullBox.Version > 1
                    ? AvifResultCode.UnsupportedFeature : AvifResultCode.InvalidTrack,
                "The SampleToGroupBox header is invalid.",
                box.PayloadOffset);
        }
        ReadOnlySpan<byte> payload = GetPayload(
            source, fullBox, diagnostics,
            "The SampleToGroupBox payload is truncated.");
        if (payload.Length < (fullBox.Version == 0 ? 8 : 12))
        {
            return diagnostics.Code;
        }
        ByteReader reader = new ByteReader(payload.Length);
        FourCc? groupingTypeValue = reader.ReadFourCc(payload);
        string groupingType = groupingTypeValue == null
            ? string.Empty : groupingTypeValue.Text;
        if (!IsRecognized(groupingType))
        {
            return AvifResultCode.Success;
        }
        bool hasParameter = fullBox.Version == 1;
        uint parameter = hasParameter
            ? reader.ReadUInt32BigEndian(payload) : 0;
        if (hasParameter && groupingType != "av1M")
        {
            return Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "Only AV1 metadata sample groups define a grouping parameter.",
                fullBox.PayloadOffset);
        }
        if (HasMappingKey(keys, groupingType, hasParameter, parameter))
        {
            return Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "An AV1 sample-group mapping is duplicated.",
                box.PayloadOffset);
        }
        ParsedAv1GroupDescriptionSet? set = FindSet(sets, groupingType);
        if (set == null)
        {
            return Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "An AV1 SampleToGroupBox has no matching description box.",
                box.PayloadOffset);
        }
        keys.Add(new ParsedAv1GroupMappingKey(
            groupingType, hasParameter, parameter));

        uint entryCount = reader.ReadUInt32BigEndian(payload);
        if (reader.Code != AvifResultCode.Success
            || 8 + (ulong)fullBox.Version * 4
                + (ulong)entryCount * 8 != (ulong)payload.Length)
        {
            return Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "The SampleToGroupBox entry count or payload size is invalid.",
                fullBox.PayloadOffset);
        }
        int sampleIndex = 0;
        uint entryIndex = 0;
        while (entryIndex < entryCount)
        {
            uint sampleCount = reader.ReadUInt32BigEndian(payload);
            uint descriptionIndex = reader.ReadUInt32BigEndian(payload);
            if (sampleCount == 0 || sampleCount > int.MaxValue
                || sampleIndex > table.Samples.Count - (int)sampleCount)
            {
                return Report(
                    diagnostics, AvifResultCode.InvalidTrack,
                    "A SampleToGroupBox run has an invalid sample count.",
                    fullBox.PayloadOffset + reader.Position);
            }
            uint effectiveIndex = descriptionIndex == 0
                ? set.DefaultIndex : descriptionIndex;
            if (effectiveIndex > (uint)set.Descriptions.Count)
            {
                return Report(
                    diagnostics, AvifResultCode.InvalidTrack,
                    "A SampleToGroupBox references an unavailable group description.",
                    fullBox.PayloadOffset + reader.Position);
            }
            uint runIndex = 0;
            while (runIndex < sampleCount)
            {
                if (effectiveIndex != 0)
                {
                    AvifResultCode code = ApplyMembership(
                        table,
                        sampleIndex,
                        groupingType,
                        set.Descriptions[(int)effectiveIndex - 1],
                        hasParameter,
                        parameter,
                        diagnostics,
                        fullBox.PayloadOffset + reader.Position);
                    if (code != AvifResultCode.Success)
                    {
                        return code;
                    }
                }
                sampleIndex++;
                runIndex++;
            }
            entryIndex++;
        }
        if (sampleIndex != table.Samples.Count)
        {
            return Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "Sample-to-group runs do not cover every media sample.",
                fullBox.PayloadOffset);
        }
        return AvifResultCode.Success;
    }

    static AvifResultCode ApplyMembership(
        ParsedSampleTable table,
        int sampleIndex,
        string groupingType,
        ParsedAv1GroupDescription description,
        bool hasParameter,
        uint parameter,
        DiagnosticLog diagnostics,
        long offset)
    {
        ParsedTrackSample sample = table.Samples[sampleIndex];
        if (groupingType == "av1f")
        {
            int recoveryIndex = sampleIndex + description.ForwardDistance + 1;
            if (recoveryIndex >= table.Samples.Count)
            {
                return Report(
                    diagnostics, AvifResultCode.InvalidTrack,
                    "An AV1 forward-key-frame group points beyond the final sample.",
                    offset);
            }
            sample.ForwardKeyFrame = true;
            sample.ForwardKeyFrameDistance = description.ForwardDistance;
            sample.RecoverySampleIndex = recoveryIndex;
        }
        else if (groupingType == "av1m")
        {
            sample.MultipleFrames = true;
        }
        else if (groupingType == "av1s")
        {
            sample.SwitchFrame = true;
        }
        else
        {
            sample.MetadataGroups.Add(new ParsedAv1MetadataGroup(
                hasParameter, parameter));
        }
        return AvifResultCode.Success;
    }

    static bool HasMappingKey(
        List<ParsedAv1GroupMappingKey> keys,
        string type,
        bool hasParameter,
        uint parameter)
    {
        int index = 0;
        while (index < keys.Count)
        {
            ParsedAv1GroupMappingKey key = keys[index];
            if (key.Type == type
                && (type != "av1M"
                    || (key.HasParameter == hasParameter
                        && key.Parameter == parameter)))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static ParsedAv1GroupDescriptionSet? FindSet(
        List<ParsedAv1GroupDescriptionSet> sets,
        string type)
    {
        int index = 0;
        while (index < sets.Count)
        {
            if (sets[index].Type == type)
            {
                return sets[index];
            }
            index++;
        }
        return null;
    }

    static bool IsRecognized(string type)
    {
        return type == "av1f" || type == "av1m"
            || type == "av1s" || type == "av1M";
    }

    static ReadOnlySpan<byte> GetPayload(
        ReadOnlySpan<byte> source,
        IsoFullBox fullBox,
        DiagnosticLog diagnostics,
        string message)
    {
        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset, 0, fullBox.PayloadSize, source.Length);
        if (!range.Success || fullBox.PayloadSize > int.MaxValue)
        {
            Report(
                diagnostics,
                range.Success ? AvifResultCode.LimitExceeded : AvifResultCode.InvalidTrack,
                message,
                fullBox.PayloadOffset);
            return ReadOnlySpan<byte>.Empty;
        }
        return source.Slice((int)fullBox.PayloadOffset, (int)fullBox.PayloadSize);
    }

    static AvifResultCode Report(
        DiagnosticLog diagnostics,
        AvifResultCode code,
        string message,
        long offset)
    {
        if (offset >= 0)
        {
            diagnostics.Context.SetLocation(
                offset, AvifDiagnostic.UnknownBitOffset);
        }
        diagnostics.Context.SetStandardReference("AV1-ISOBMFF 1.3");
        diagnostics.Add(
            code,
            AvifDiagnosticSeverity.Error,
            code == AvifResultCode.LimitExceeded
                ? AvifDiagnosticCategory.Resource
                : AvifDiagnosticCategory.Track,
            message);
        diagnostics.Context.SetStandardReference(string.Empty);
        diagnostics.Context.ClearLocation();
        return code;
    }
}
