using System;
using System.Collections.Generic;
using System.Text;

namespace avif;

internal class ParsedAv1SampleDescription
{
    internal int Index;
    internal string Format;
    internal int DataReferenceIndex;
    internal int Width;
    internal int Height;
    internal uint HorizontalResolutionFixed;
    internal uint VerticalResolutionFixed;
    internal string CompressorName;
    internal int Depth;
    internal int EntrySize;
    internal ParsedAv1Configuration? Configuration;
    internal ParsedAuxiliaryType? AuxiliaryType;
    internal List<ParsedColourInformation> ColourInformation;

    internal ParsedAv1SampleDescription()
    {
        Format = string.Empty;
        CompressorName = string.Empty;
        ColourInformation = new List<ParsedColourInformation>();
    }
}

internal class ParsedTrackSample
{
    internal int Index;
    internal int DescriptionIndex;
    internal ulong Offset;
    internal uint Size;
    internal ulong DecodeTime;
    internal uint Duration;
    internal bool Sync;
    internal int Leading;
    internal int DependsOn;
    internal int IsDependedOn;
    internal int Redundancy;
    internal bool ForwardKeyFrame;
    internal int ForwardKeyFrameDistance;
    internal int RecoverySampleIndex;
    internal bool MultipleFrames;
    internal bool SwitchFrame;
    internal List<ParsedAv1MetadataGroup> MetadataGroups;

    internal ParsedTrackSample(int index, uint size)
    {
        Index = index;
        Size = size;
        Sync = true;
        ForwardKeyFrameDistance = -1;
        RecoverySampleIndex = -1;
        MetadataGroups = new List<ParsedAv1MetadataGroup>();
    }
}

internal class ParsedAv1MetadataGroup
{
    internal bool HasParameter;
    internal uint Parameter;

    internal ParsedAv1MetadataGroup(bool hasParameter, uint parameter)
    {
        HasParameter = hasParameter;
        Parameter = parameter;
    }
}

internal class ParsedSampleTable
{
    internal List<ParsedAv1SampleDescription> Descriptions;
    internal List<ParsedTrackSample> Samples;
    internal List<ulong> ChunkOffsets;
    internal List<ParsedSampleToChunkEntry> ChunkEntries;

    internal ParsedSampleTable()
    {
        Descriptions = new List<ParsedAv1SampleDescription>();
        Samples = new List<ParsedTrackSample>();
        ChunkOffsets = new List<ulong>();
        ChunkEntries = new List<ParsedSampleToChunkEntry>();
    }
}

internal class ParsedSampleToChunkEntry
{
    internal uint FirstChunk;
    internal uint SamplesPerChunk;
    internal uint DescriptionIndex;

    internal ParsedSampleToChunkEntry(
        uint firstChunk,
        uint samplesPerChunk,
        uint descriptionIndex)
    {
        FirstChunk = firstChunk;
        SamplesPerChunk = samplesPerChunk;
        DescriptionIndex = descriptionIndex;
    }
}

internal class SampleTableChildLookup
{
    internal AvifResultCode Code;
    internal IsoBox? Box;

    internal SampleTableChildLookup(AvifResultCode code, IsoBox? box)
    {
        Code = code;
        Box = box;
    }
}

internal static class SampleTableBoxParser
{
    internal static AvifResultCode Parse(
        ReadOnlySpan<byte> source,
        IsoBox? sampleTableBox,
        List<IsoBox> topLevelBoxes,
        ResourceGuard resources,
        DiagnosticLog diagnostics,
        ParsedSampleTable table)
    {
        if (sampleTableBox == null || sampleTableBox.Type != "stbl"
            || topLevelBoxes == null || resources == null
            || diagnostics == null || table == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (sampleTableBox.Children.Count == 0)
        {
            return AvifResultCode.Success;
        }

        diagnostics.Context.PushBox("stbl");
        SampleTableChildLookup descriptions = FindChild(
            sampleTableBox, "stsd", true, diagnostics);
        SampleTableChildLookup timing = FindChild(
            sampleTableBox, "stts", true, diagnostics);
        SampleTableChildLookup chunks = FindChild(
            sampleTableBox, "stsc", true, diagnostics);
        SampleTableChildLookup sizes = FindChild(
            sampleTableBox, "stsz", true, diagnostics);
        SampleTableChildLookup offsets32 = FindChild(
            sampleTableBox, "stco", false, diagnostics);
        SampleTableChildLookup offsets64 = FindChild(
            sampleTableBox, "co64", false, diagnostics);
        SampleTableChildLookup sync = FindChild(
            sampleTableBox, "stss", false, diagnostics);
        SampleTableChildLookup dependencies = FindChild(
            sampleTableBox, "sdtp", false, diagnostics);
        AvifResultCode lookupCode = FirstError(
            descriptions,
            timing,
            chunks,
            sizes,
            offsets32,
            offsets64,
            sync,
            dependencies);
        if (lookupCode != AvifResultCode.Success)
        {
            diagnostics.Context.PopBox();
            return lookupCode;
        }
        if ((offsets32.Box == null) == (offsets64.Box == null))
        {
            AvifResultCode offsetCode = Report(
                diagnostics,
                AvifResultCode.InvalidTrack,
                "A SampleTableBox must contain exactly one stco or co64 box.",
                sampleTableBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return offsetCode;
        }

        AvifResultCode code = ParseDescriptions(
            source, descriptions.Box, resources, diagnostics, table);
        if (code == AvifResultCode.Success)
        {
            code = ParseSizes(source, sizes.Box, resources, diagnostics, table);
        }
        if (code == AvifResultCode.Success)
        {
            code = ParseTiming(source, timing.Box, diagnostics, table);
        }
        if (code == AvifResultCode.Success)
        {
            code = ParseChunkOffsets(
                source,
                offsets32.Box == null ? offsets64.Box : offsets32.Box,
                diagnostics,
                table);
        }
        if (code == AvifResultCode.Success)
        {
            code = ParseChunks(source, chunks.Box, diagnostics, table);
        }
        if (code == AvifResultCode.Success)
        {
            code = ExpandChunks(
                source.Length, topLevelBoxes, diagnostics, table);
        }
        if (code == AvifResultCode.Success && sync.Box != null)
        {
            code = ParseSyncSamples(source, sync.Box, diagnostics, table);
        }
        if (code == AvifResultCode.Success && dependencies.Box != null)
        {
            code = ParseDependencies(
                source, dependencies.Box, diagnostics, table);
        }
        if (code == AvifResultCode.Success)
        {
            code = Av1SampleGroupParser.Parse(
                source, sampleTableBox, resources, diagnostics, table);
        }
        diagnostics.Context.PopBox();
        return code;
    }

    static AvifResultCode ParseDescriptions(
        ReadOnlySpan<byte> source,
        IsoBox? box,
        ResourceGuard resources,
        DiagnosticLog diagnostics,
        ParsedSampleTable table)
    {
        diagnostics.Context.PushBox("stsd");
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (!IsVersionZero(fullBox) || fullBox.PayloadSize < 4)
        {
            AvifResultCode code = Report(
                diagnostics,
                FullBoxCode(fullBox),
                "The SampleDescriptionBox header is invalid.",
                (box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset, 0, fullBox.PayloadSize, source.Length);
        if (!range.Success)
        {
            AvifResultCode code = Report(
                diagnostics, Normalize(range.Code),
                "The SampleDescriptionBox payload is truncated.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        ReadOnlySpan<byte> payload = source.Slice(
            (int)fullBox.PayloadOffset, (int)fullBox.PayloadSize);
        ByteReader reader = new ByteReader(payload.Length);
        uint entryCount = reader.ReadUInt32BigEndian(payload);
        if (entryCount == 0 || entryCount > (uint)resources.Limits.MaxSampleCount)
        {
            AvifResultCode resultCode = entryCount == 0
                ? AvifResultCode.InvalidTrack : AvifResultCode.LimitExceeded;
            AvifResultCode code = Report(
                diagnostics, resultCode,
                "The sample-description count is invalid or exceeds its configured limit.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }

        uint entryIndex = 0;
        while (entryIndex < entryCount)
        {
            AvifResultCode code = ParseAv1SampleEntry(
                source,
                fullBox.PayloadOffset + reader.Position,
                fullBox.PayloadOffset + payload.Length,
                (int)entryIndex + 1,
                resources,
                diagnostics,
                table);
            if (code != AvifResultCode.Success)
            {
                diagnostics.Context.PopBox();
                return code;
            }
            ParsedAv1SampleDescription description =
                table.Descriptions[table.Descriptions.Count - 1];
            reader.Position += description.EntrySize;
            entryIndex++;
        }
        if (reader.Position != payload.Length)
        {
            AvifResultCode code = Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "The SampleDescriptionBox contains trailing or missing entry bytes.",
                fullBox.PayloadOffset + reader.Position);
            diagnostics.Context.PopBox();
            return code;
        }
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static AvifResultCode ParseAv1SampleEntry(
        ReadOnlySpan<byte> source,
        long entryOffset,
        long tableEnd,
        int index,
        ResourceGuard resources,
        DiagnosticLog diagnostics,
        ParsedSampleTable table)
    {
        if (entryOffset < 0 || tableEnd - entryOffset < 8)
        {
            return Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "The visual sample-entry header is truncated.", entryOffset);
        }
        int baseOffset = (int)entryOffset;
        uint size32 = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(
            source.Slice(baseOffset, 4));
        string format = new FourCc(
            System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(
                source.Slice(baseOffset + 4, 4))).Text;
        int headerSize = 8;
        ulong entrySize = size32;
        if (size32 == 1)
        {
            if (tableEnd - entryOffset < 16)
            {
                return Report(
                    diagnostics, AvifResultCode.InvalidTrack,
                    "The large visual sample-entry header is truncated.", entryOffset);
            }
            headerSize = 16;
            entrySize = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(
                source.Slice(baseOffset + 8, 8));
        }
        if (size32 == 0 || entrySize < (ulong)(headerSize + 78)
            || entrySize > int.MaxValue
            || entryOffset + (long)entrySize > tableEnd)
        {
            return Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "The visual sample-entry size is invalid.", entryOffset);
        }
        if (format != "av01")
        {
            return Report(
                diagnostics, AvifResultCode.UnsupportedFeature,
                "Only AV1 visual sample entries are supported.", entryOffset);
        }

        diagnostics.Context.PushBox("av01");
        int bodyOffset = baseOffset + headerSize;
        ReadOnlySpan<byte> body = source.Slice(bodyOffset, 78);
        ByteReader reader = new ByteReader(body.Length);
        uint reservedSample0 = reader.ReadUInt32BigEndian(body);
        ushort reservedSample1 = reader.ReadUInt16BigEndian(body);
        ushort dataReferenceIndex = reader.ReadUInt16BigEndian(body);
        ushort predefined = reader.ReadUInt16BigEndian(body);
        ushort reserved = reader.ReadUInt16BigEndian(body);
        bool predefinedZero = true;
        int predefinedIndex = 0;
        while (predefinedIndex < 3)
        {
            if (reader.ReadUInt32BigEndian(body) != 0)
            {
                predefinedZero = false;
            }
            predefinedIndex++;
        }
        ushort width = reader.ReadUInt16BigEndian(body);
        ushort height = reader.ReadUInt16BigEndian(body);
        uint horizontalResolution = reader.ReadUInt32BigEndian(body);
        uint verticalResolution = reader.ReadUInt32BigEndian(body);
        uint reserved32 = reader.ReadUInt32BigEndian(body);
        ushort frameCount = reader.ReadUInt16BigEndian(body);
        byte compressorLength = reader.ReadUInt8(body);
        byte[] compressorBytes = new byte[compressorLength <= 31 ? compressorLength : 0];
        int compressorIndex = 0;
        bool compressorPaddingZero = compressorLength <= 31;
        while (compressorIndex < 31)
        {
            byte value = reader.ReadUInt8(body);
            if (compressorIndex < compressorBytes.Length)
            {
                compressorBytes[compressorIndex] = value;
            }
            else if (value != 0)
            {
                compressorPaddingZero = false;
            }
            compressorIndex++;
        }
        ushort depth = reader.ReadUInt16BigEndian(body);
        ushort predefinedLast = reader.ReadUInt16BigEndian(body);
        if (reader.Code != AvifResultCode.Success
            || reservedSample0 != 0 || reservedSample1 != 0
            || dataReferenceIndex == 0 || predefined != 0 || reserved != 0
            || !predefinedZero || width == 0 || height == 0
            || horizontalResolution != 4718592
            || verticalResolution != 4718592
            || reserved32 != 0 || frameCount != 1 || depth != 24
            || !compressorPaddingZero || predefinedLast != ushort.MaxValue)
        {
            AvifResultCode code = Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "The AV1 visual sample entry contains invalid fixed fields.",
                entryOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        AvifResultCode dimensionCode = resources.CheckImageDimensions(width, height);
        if (dimensionCode != AvifResultCode.Success)
        {
            AvifResultCode code = Report(
                diagnostics, dimensionCode,
                "The AV1 sample-entry dimensions exceed configured limits.",
                entryOffset);
            diagnostics.Context.PopBox();
            return code;
        }

        long childStart = entryOffset + headerSize + 78;
        long entryEnd = entryOffset + (long)entrySize;
        List<IsoBox> children = new List<IsoBox>();
        AvifResultCode childCode = IsoBoxReader.ParseRange(
            source,
            childStart,
            entryEnd,
            source.Length,
            children,
            resources,
            diagnostics,
            false,
            "av01");
        if (childCode != AvifResultCode.Success)
        {
            diagnostics.Context.PopBox();
            return childCode;
        }
        IsoBox? configurationBox = null;
        int configurationCount = 0;
        IsoBox? auxiliaryTypeBox = null;
        int auxiliaryTypeCount = 0;
        int childIndex = 0;
        while (childIndex < children.Count)
        {
            if (children[childIndex].Type == "av1C")
            {
                configurationCount++;
                if (configurationBox == null)
                {
                    configurationBox = children[childIndex];
                }
            }
            else if (children[childIndex].Type == "auxi")
            {
                auxiliaryTypeCount++;
                if (auxiliaryTypeBox == null)
                {
                    auxiliaryTypeBox = children[childIndex];
                }
            }
            childIndex++;
        }
        if (configurationCount != 1)
        {
            AvifResultCode code = Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "An AV1 sample entry must contain exactly one av1C box.",
                childStart);
            diagnostics.Context.PopBox();
            return code;
        }
        if (auxiliaryTypeCount > 1)
        {
            AvifResultCode code = Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "An AV1 sample entry may contain at most one auxi box.",
                childStart);
            diagnostics.Context.PopBox();
            return code;
        }
        Av1ItemConfigurationParseResult configuration =
            Av1ItemConfigurationBox.Parse(source, configurationBox);
        if (configuration.Code != AvifResultCode.Success)
        {
            AvifResultCode code = Report(
                diagnostics, Normalize(configuration.Code),
                "The AV1 sample-entry configuration is invalid.",
                (configurationBox ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        ParsedAuxiliaryType? auxiliaryType = null;
        if (auxiliaryTypeBox != null)
        {
            AdditionalItemPropertyParseResult auxiliaryResult =
                AdditionalItemPropertyBox.ParseAuxiliaryType(
                    source, auxiliaryTypeBox);
            if (auxiliaryResult.Code != AvifResultCode.Success)
            {
                AvifResultCode code = Report(
                    diagnostics, Normalize(auxiliaryResult.Code),
                    "The AuxiliaryTypeInfoBox is invalid.",
                    auxiliaryTypeBox.PayloadOffset);
                diagnostics.Context.PopBox();
                return code;
            }
            auxiliaryType = auxiliaryResult.AuxiliaryType;
        }

        List<ParsedColourInformation> colourInformation =
            new List<ParsedColourInformation>();
        childIndex = 0;
        while (childIndex < children.Count)
        {
            IsoBox child = children[childIndex];
            if (child.Type == "colr")
            {
                AdditionalItemPropertyParseResult colourResult =
                    AdditionalItemPropertyBox.Parse(source, child, resources);
                if (colourResult.Code != AvifResultCode.Success)
                {
                    AvifResultCode code = Report(
                        diagnostics, Normalize(colourResult.Code),
                        "A ColourInformationBox in the AV1 sample entry is invalid.",
                        child.PayloadOffset);
                    diagnostics.Context.PopBox();
                    return code;
                }
                colourInformation.Add(colourResult.ColourInformation
                    ?? throw new InvalidOperationException("Invalid internal state."));
            }
            childIndex++;
        }
        AvifResultCode colourCode = ColorMetadataResolver.Validate(
            colourInformation);
        if (colourCode != AvifResultCode.Success)
        {
            AvifResultCode code = Report(
                diagnostics, Normalize(colourCode),
                "ColourInformationBox values in the AV1 sample entry conflict.",
                childStart);
            diagnostics.Context.PopBox();
            return code;
        }

        ParsedAv1SampleDescription description = new ParsedAv1SampleDescription();
        description.Index = index;
        description.Format = format;
        description.DataReferenceIndex = dataReferenceIndex;
        description.Width = width;
        description.Height = height;
        description.HorizontalResolutionFixed = horizontalResolution;
        description.VerticalResolutionFixed = verticalResolution;
        description.CompressorName = Encoding.UTF8.GetString(compressorBytes);
        description.Depth = depth;
        description.EntrySize = (int)entrySize;
        description.Configuration = configuration.Configuration;
        description.AuxiliaryType = auxiliaryType;
        description.ColourInformation = colourInformation;
        table.Descriptions.Add(description);
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static AvifResultCode ParseSizes(
        ReadOnlySpan<byte> source,
        IsoBox? box,
        ResourceGuard resources,
        DiagnosticLog diagnostics,
        ParsedSampleTable table)
    {
        diagnostics.Context.PushBox("stsz");
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (!IsVersionZero(fullBox) || fullBox.PayloadSize < 8)
        {
            AvifResultCode code = Report(
                diagnostics, FullBoxCode(fullBox),
                "The SampleSizeBox header is invalid.", (box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        ReadOnlySpan<byte> payload = GetPayload(
            source, fullBox, diagnostics, "The SampleSizeBox payload is truncated.");
        if (payload.Length == 0)
        {
            diagnostics.Context.PopBox();
            return diagnostics.Code;
        }
        ByteReader reader = new ByteReader(payload.Length);
        uint defaultSize = reader.ReadUInt32BigEndian(payload);
        uint sampleCount = reader.ReadUInt32BigEndian(payload);
        ulong expectedSize = 8;
        if (defaultSize == 0)
        {
            expectedSize += (ulong)sampleCount * 4;
        }
        if (sampleCount == 0 || sampleCount > int.MaxValue
            || expectedSize != (ulong)payload.Length)
        {
            AvifResultCode code = Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "The SampleSizeBox sample count or payload size is invalid.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        AvifResultCode countCode = resources.CountSamples((int)sampleCount);
        if (countCode != AvifResultCode.Success)
        {
            AvifResultCode code = Report(
                diagnostics, countCode,
                "The media sample count exceeds its configured limit.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        int sampleIndex = 0;
        while (sampleIndex < (int)sampleCount)
        {
            uint size = defaultSize == 0
                ? reader.ReadUInt32BigEndian(payload) : defaultSize;
            if (size == 0)
            {
                AvifResultCode code = Report(
                    diagnostics, AvifResultCode.InvalidTrack,
                    "An AV1 media sample must have a positive encoded size.",
                    fullBox.PayloadOffset + reader.Position);
                diagnostics.Context.PopBox();
                return code;
            }
            table.Samples.Add(new ParsedTrackSample(sampleIndex, size));
            sampleIndex++;
        }
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static AvifResultCode ParseTiming(
        ReadOnlySpan<byte> source,
        IsoBox? box,
        DiagnosticLog diagnostics,
        ParsedSampleTable table)
    {
        diagnostics.Context.PushBox("stts");
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        ReadOnlySpan<byte> payload = GetVersionZeroPayload(
            source, fullBox, diagnostics, box, "The TimeToSampleBox is invalid.");
        if (payload.Length < 4)
        {
            diagnostics.Context.PopBox();
            return diagnostics.Code;
        }
        ByteReader reader = new ByteReader(payload.Length);
        uint entryCount = reader.ReadUInt32BigEndian(payload);
        if (entryCount == 0
            || 4 + (ulong)entryCount * 8 != (ulong)payload.Length)
        {
            AvifResultCode code = Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "The TimeToSampleBox entry count or payload size is invalid.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        int sampleIndex = 0;
        ulong decodeTime = 0;
        uint entryIndex = 0;
        while (entryIndex < entryCount)
        {
            uint count = reader.ReadUInt32BigEndian(payload);
            uint delta = reader.ReadUInt32BigEndian(payload);
            if (count == 0 || delta == 0 || count > int.MaxValue
                || sampleIndex > table.Samples.Count - (int)count)
            {
                AvifResultCode code = Report(
                    diagnostics, AvifResultCode.InvalidTrack,
                    "A TimeToSampleBox entry has an invalid count or duration.",
                    fullBox.PayloadOffset + reader.Position);
                diagnostics.Context.PopBox();
                return code;
            }
            uint repetition = 0;
            while (repetition < count)
            {
                if (decodeTime > ulong.MaxValue - delta)
                {
                    AvifResultCode code = Report(
                        diagnostics, AvifResultCode.LimitExceeded,
                        "The expanded sample decode time overflows.",
                        fullBox.PayloadOffset + reader.Position);
                    diagnostics.Context.PopBox();
                    return code;
                }
                ParsedTrackSample sample = table.Samples[sampleIndex];
                sample.DecodeTime = decodeTime;
                sample.Duration = delta;
                decodeTime += delta;
                sampleIndex++;
                repetition++;
            }
            entryIndex++;
        }
        if (sampleIndex != table.Samples.Count)
        {
            AvifResultCode code = Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "Time-to-sample entries do not cover every media sample.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static AvifResultCode ParseChunkOffsets(
        ReadOnlySpan<byte> source,
        IsoBox? box,
        DiagnosticLog diagnostics,
        ParsedSampleTable table)
    {
        diagnostics.Context.PushBox((box ?? throw new InvalidOperationException("Invalid internal state.")).Type);
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        ReadOnlySpan<byte> payload = GetVersionZeroPayload(
            source, fullBox, diagnostics, box, "The ChunkOffsetBox is invalid.");
        if (payload.Length < 4)
        {
            diagnostics.Context.PopBox();
            return diagnostics.Code;
        }
        ByteReader reader = new ByteReader(payload.Length);
        uint entryCount = reader.ReadUInt32BigEndian(payload);
        int offsetSize = box.Type == "co64" ? 8 : 4;
        if (entryCount == 0 || entryCount > int.MaxValue
            || 4 + (ulong)entryCount * (ulong)offsetSize != (ulong)payload.Length)
        {
            AvifResultCode code = Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "The ChunkOffsetBox entry count or payload size is invalid.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        uint index = 0;
        while (index < entryCount)
        {
            ulong offset = box.Type == "co64"
                ? reader.ReadUInt64BigEndian(payload)
                : reader.ReadUInt32BigEndian(payload);
            table.ChunkOffsets.Add(offset);
            index++;
        }
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static AvifResultCode ParseChunks(
        ReadOnlySpan<byte> source,
        IsoBox? box,
        DiagnosticLog diagnostics,
        ParsedSampleTable table)
    {
        table.ChunkEntries.Clear();
        diagnostics.Context.PushBox("stsc");
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        ReadOnlySpan<byte> payload = GetVersionZeroPayload(
            source, fullBox, diagnostics, box, "The SampleToChunkBox is invalid.");
        if (payload.Length < 4)
        {
            diagnostics.Context.PopBox();
            return diagnostics.Code;
        }
        ByteReader reader = new ByteReader(payload.Length);
        uint entryCount = reader.ReadUInt32BigEndian(payload);
        if (entryCount == 0
            || 4 + (ulong)entryCount * 12 != (ulong)payload.Length)
        {
            AvifResultCode code = Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "The SampleToChunkBox entry count or payload size is invalid.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        uint previousFirstChunk = 0;
        uint entryIndex = 0;
        while (entryIndex < entryCount)
        {
            uint firstChunk = reader.ReadUInt32BigEndian(payload);
            uint samplesPerChunk = reader.ReadUInt32BigEndian(payload);
            uint descriptionIndex = reader.ReadUInt32BigEndian(payload);
            if (firstChunk == 0 || firstChunk <= previousFirstChunk
                || firstChunk > (uint)table.ChunkOffsets.Count
                || samplesPerChunk == 0
                || descriptionIndex == 0
                || descriptionIndex > (uint)table.Descriptions.Count
                || (entryIndex == 0 && firstChunk != 1))
            {
                AvifResultCode code = Report(
                    diagnostics, AvifResultCode.InvalidTrack,
                    "A SampleToChunkBox entry contains an invalid mapping.",
                    fullBox.PayloadOffset + reader.Position);
                diagnostics.Context.PopBox();
                return code;
            }
            table.ChunkEntries.Add(new ParsedSampleToChunkEntry(
                firstChunk, samplesPerChunk, descriptionIndex));
            previousFirstChunk = firstChunk;
            entryIndex++;
        }
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static AvifResultCode ExpandChunks(
        int sourceLength,
        List<IsoBox> topLevelBoxes,
        DiagnosticLog diagnostics,
        ParsedSampleTable table)
    {
        int sampleIndex = 0;
        int entryIndex = 0;
        while (entryIndex < table.ChunkEntries.Count)
        {
            ParsedSampleToChunkEntry entry = table.ChunkEntries[entryIndex];
            uint endChunk = entryIndex + 1 < table.ChunkEntries.Count
                ? table.ChunkEntries[entryIndex + 1].FirstChunk - 1
                : (uint)table.ChunkOffsets.Count;
            uint chunkNumber = entry.FirstChunk;
            while (chunkNumber <= endChunk)
            {
                ulong sampleOffset = table.ChunkOffsets[(int)chunkNumber - 1];
                uint chunkSample = 0;
                while (chunkSample < entry.SamplesPerChunk)
                {
                    if (sampleIndex >= table.Samples.Count)
                    {
                        return Report(
                            diagnostics, AvifResultCode.InvalidTrack,
                            "Sample-to-chunk mappings contain more samples than SampleSizeBox.",
                            (long)sampleOffset);
                    }
                    ParsedTrackSample sample = table.Samples[sampleIndex];
                    if (sampleOffset > long.MaxValue)
                    {
                        return Report(
                            diagnostics, AvifResultCode.InvalidTrack,
                            "A chunk offset is outside the supported source range.",
                            AvifDiagnostic.UnknownByteOffset);
                    }
                    CheckedRangeResult range = CheckedMath.ResolveRange(
                        (long)sampleOffset, 0, sample.Size, sourceLength);
                    if (!range.Success)
                    {
                        return Report(
                            diagnostics, Normalize(range.Code),
                            "An expanded media sample range is outside the source.",
                            (long)sampleOffset);
                    }
                    if (!IsMediaDataRange(
                        topLevelBoxes, sampleOffset, sample.Size))
                    {
                        return Report(
                            diagnostics, AvifResultCode.InvalidTrack,
                            "An expanded media sample range is outside every MediaDataBox.",
                            (long)sampleOffset);
                    }
                    sample.DescriptionIndex = (int)entry.DescriptionIndex;
                    sample.Offset = sampleOffset;
                    if (sampleOffset > ulong.MaxValue - sample.Size)
                    {
                        return Report(
                            diagnostics, AvifResultCode.LimitExceeded,
                            "An expanded media sample offset overflows.",
                            (long)sampleOffset);
                    }
                    sampleOffset += sample.Size;
                    sampleIndex++;
                    chunkSample++;
                }
                chunkNumber++;
            }
            entryIndex++;
        }
        if (sampleIndex != table.Samples.Count)
        {
            return Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "Sample-to-chunk mappings do not cover every SampleSizeBox entry.",
                AvifDiagnostic.UnknownByteOffset);
        }
        return AvifResultCode.Success;
    }

    static bool IsMediaDataRange(
        List<IsoBox> topLevelBoxes,
        ulong offset,
        uint size)
    {
        if (offset > long.MaxValue)
        {
            return false;
        }
        long start = (long)offset;
        if (start > long.MaxValue - size)
        {
            return false;
        }
        long end = start + size;
        int index = 0;
        while (index < topLevelBoxes.Count)
        {
            IsoBox box = topLevelBoxes[index];
            if (box.Type == "mdat"
                && start >= box.PayloadOffset && end <= box.EndOffset)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static AvifResultCode ParseSyncSamples(
        ReadOnlySpan<byte> source,
        IsoBox box,
        DiagnosticLog diagnostics,
        ParsedSampleTable table)
    {
        diagnostics.Context.PushBox("stss");
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        ReadOnlySpan<byte> payload = GetVersionZeroPayload(
            source, fullBox, diagnostics, box, "The SyncSampleBox is invalid.");
        if (payload.Length < 4)
        {
            diagnostics.Context.PopBox();
            return diagnostics.Code;
        }
        ByteReader reader = new ByteReader(payload.Length);
        uint entryCount = reader.ReadUInt32BigEndian(payload);
        if (4 + (ulong)entryCount * 4 != (ulong)payload.Length)
        {
            AvifResultCode code = Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "The SyncSampleBox entry count or payload size is invalid.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        int sampleIndex = 0;
        while (sampleIndex < table.Samples.Count)
        {
            table.Samples[sampleIndex].Sync = false;
            sampleIndex++;
        }
        uint previous = 0;
        uint entryIndex = 0;
        while (entryIndex < entryCount)
        {
            uint sampleNumber = reader.ReadUInt32BigEndian(payload);
            if (sampleNumber == 0 || sampleNumber <= previous
                || sampleNumber > (uint)table.Samples.Count)
            {
                AvifResultCode code = Report(
                    diagnostics, AvifResultCode.InvalidTrack,
                    "The SyncSampleBox contains an invalid sample number.",
                    fullBox.PayloadOffset + reader.Position);
                diagnostics.Context.PopBox();
                return code;
            }
            table.Samples[(int)sampleNumber - 1].Sync = true;
            previous = sampleNumber;
            entryIndex++;
        }
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static AvifResultCode ParseDependencies(
        ReadOnlySpan<byte> source,
        IsoBox box,
        DiagnosticLog diagnostics,
        ParsedSampleTable table)
    {
        diagnostics.Context.PushBox("sdtp");
        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        ReadOnlySpan<byte> payload = GetVersionZeroPayload(
            source, fullBox, diagnostics, box,
            "The SampleDependencyTypeBox is invalid.");
        if (payload.Length != table.Samples.Count)
        {
            AvifResultCode code = Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "The SampleDependencyTypeBox must describe every sample.",
                fullBox.PayloadOffset);
            diagnostics.Context.PopBox();
            return code;
        }
        int index = 0;
        while (index < table.Samples.Count)
        {
            int value = payload[index];
            int leading = value >> 6 & 3;
            int dependsOn = value >> 4 & 3;
            int isDependedOn = value >> 2 & 3;
            int redundancy = value & 3;
            if (leading == 3 || dependsOn == 3
                || isDependedOn == 3 || redundancy == 3)
            {
                AvifResultCode code = Report(
                    diagnostics, AvifResultCode.InvalidTrack,
                    "A sample dependency entry uses a reserved value.",
                    fullBox.PayloadOffset + index);
                diagnostics.Context.PopBox();
                return code;
            }
            ParsedTrackSample sample = table.Samples[index];
            sample.Leading = leading;
            sample.DependsOn = dependsOn;
            sample.IsDependedOn = isDependedOn;
            sample.Redundancy = redundancy;
            index++;
        }
        diagnostics.Context.PopBox();
        return AvifResultCode.Success;
    }

    static ReadOnlySpan<byte> GetVersionZeroPayload(
        ReadOnlySpan<byte> source,
        IsoFullBox fullBox,
        DiagnosticLog diagnostics,
        IsoBox? box,
        string message)
    {
        if (!IsVersionZero(fullBox))
        {
            Report(diagnostics, FullBoxCode(fullBox), message, (box ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset);
            return ReadOnlySpan<byte>.Empty;
        }
        return GetPayload(source, fullBox, diagnostics, message);
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
                range.Success ? AvifResultCode.LimitExceeded : Normalize(range.Code),
                message,
                fullBox.PayloadOffset);
            return ReadOnlySpan<byte>.Empty;
        }
        return source.Slice((int)fullBox.PayloadOffset, (int)fullBox.PayloadSize);
    }

    static bool IsVersionZero(IsoFullBox fullBox)
    {
        return fullBox.Code == AvifResultCode.Success
            && fullBox.Version == 0 && fullBox.Flags == 0;
    }

    static AvifResultCode FullBoxCode(IsoFullBox fullBox)
    {
        if (fullBox.Code != AvifResultCode.Success)
        {
            return Normalize(fullBox.Code);
        }
        return fullBox.Version == 0
            ? AvifResultCode.InvalidTrack : AvifResultCode.UnsupportedFeature;
    }

    static SampleTableChildLookup FindChild(
        IsoBox parent,
        string type,
        bool required,
        DiagnosticLog diagnostics)
    {
        IsoBox? found = null;
        int count = 0;
        int index = 0;
        while (index < parent.Children.Count)
        {
            if (parent.Children[index].Type == type)
            {
                count++;
                if (found == null)
                {
                    found = parent.Children[index];
                }
            }
            index++;
        }
        if (count > 1 || (required && count != 1))
        {
            AvifResultCode code = Report(
                diagnostics, AvifResultCode.InvalidTrack,
                "The SampleTableBox must contain "
                    + (required ? "exactly one " : "at most one ") + type + " box.",
                parent.PayloadOffset);
            return new SampleTableChildLookup(code, null);
        }
        return new SampleTableChildLookup(AvifResultCode.Success, found);
    }

    static AvifResultCode FirstError(
        SampleTableChildLookup first,
        SampleTableChildLookup second,
        SampleTableChildLookup third,
        SampleTableChildLookup fourth,
        SampleTableChildLookup fifth,
        SampleTableChildLookup sixth,
        SampleTableChildLookup seventh,
        SampleTableChildLookup eighth)
    {
        SampleTableChildLookup[] values = new SampleTableChildLookup[]
        {
            first, second, third, fourth, fifth, sixth, seventh, eighth
        };
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Code != AvifResultCode.Success)
            {
                return values[index].Code;
            }
            index++;
        }
        return AvifResultCode.Success;
    }

    static AvifResultCode Normalize(AvifResultCode code)
    {
        return code == AvifResultCode.UnsupportedFeature
            || code == AvifResultCode.LimitExceeded
            ? code : AvifResultCode.InvalidTrack;
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
        diagnostics.Context.SetStandardReference("ISO/IEC 14496-12");
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
