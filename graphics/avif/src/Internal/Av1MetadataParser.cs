using System;

namespace avif;

// AV1 specification sections 5.8 and 6.7.
internal static class Av1MetadataParser
{
    const int ScalabilityStructureMode = 14;

    internal static Av1MetadataParseResult ParseObu(Av1Obu? obu)
    {
        if (obu == null || obu.Header == null || obu.Header.Type != Av1ObuType.Metadata)
        {
            return CreateResult(AvifResultCode.InvalidArgument, null);
        }

        return ParsePayload(obu.GetPayload());
    }

    internal static Av1MetadataParseResult ParsePayload(ReadOnlySpan<byte> payload)
    {
        ByteReader byteReader = new ByteReader(payload.Length);
        Av1Leb128ParseResult type = Av1SequenceHeaderParser.ReadLeb128(
            payload,
            byteReader);
        if (type.Code != AvifResultCode.Success)
        {
            return CreateResult(type.Code, null);
        }

        ParsedAv1Metadata metadata = new ParsedAv1Metadata(type.Value);
        BitReader bitReader = BitReader.Create(payload.Length);
        bitReader.BitPosition = (long)byteReader.Position * 8;
        if (type.Value == (ulong)Av1MetadataType.HdrContentLightLevel)
        {
            metadata.ContentLightLevel = ReadContentLightLevel(payload, bitReader);
        }
        else if (type.Value == (ulong)Av1MetadataType.HdrMasteringDisplayColorVolume)
        {
            metadata.MasteringDisplayColorVolume =
                ReadMasteringDisplayColorVolume(payload, bitReader);
        }
        else if (type.Value == (ulong)Av1MetadataType.Scalability)
        {
            metadata.Scalability = ReadScalability(payload, bitReader);
        }
        else if (type.Value == (ulong)Av1MetadataType.ItuTT35)
        {
            AvifResultCode code = ReadItuT35(payload, bitReader, metadata);
            if (code != AvifResultCode.Success)
            {
                return CreateResult(code, null);
            }
        }
        else if (type.Value == (ulong)Av1MetadataType.Timecode)
        {
            metadata.Timecode = ReadTimecode(payload, bitReader);
        }
        else
        {
            AvifResultCode code = ReadOpaquePayload(payload, bitReader, metadata);
            if (code != AvifResultCode.Success)
            {
                return CreateResult(code, null);
            }
        }

        if (bitReader.Code != AvifResultCode.Success)
        {
            return CreateResult(NormalizeCode(bitReader.Code), null);
        }

        AvifResultCode trailingCode = Av1SequenceHeaderParser.ReadTrailingBits(
            payload,
            bitReader);
        if (trailingCode != AvifResultCode.Success)
        {
            return CreateResult(NormalizeCode(trailingCode), null);
        }

        return CreateResult(AvifResultCode.Success, metadata);
    }

    static Av1ContentLightLevelMetadata ReadContentLightLevel(
        ReadOnlySpan<byte> payload,
        BitReader reader)
    {
        Av1ContentLightLevelMetadata content = new Av1ContentLightLevelMetadata();
        content.MaximumContentLightLevel = (int)reader.ReadBits(payload, 16);
        content.MaximumFrameAverageLightLevel = (int)reader.ReadBits(payload, 16);
        return content;
    }

    static Av1MasteringDisplayColorVolumeMetadata ReadMasteringDisplayColorVolume(
        ReadOnlySpan<byte> payload,
        BitReader reader)
    {
        Av1MasteringDisplayColorVolumeMetadata volume =
            new Av1MasteringDisplayColorVolumeMetadata();
        int index = 0;
        while (index < 3)
        {
            volume.PrimaryChromaticityX[index] = (int)reader.ReadBits(payload, 16);
            volume.PrimaryChromaticityY[index] = (int)reader.ReadBits(payload, 16);
            index++;
        }

        volume.WhitePointChromaticityX = (int)reader.ReadBits(payload, 16);
        volume.WhitePointChromaticityY = (int)reader.ReadBits(payload, 16);
        volume.MaximumLuminance = (uint)reader.ReadBits(payload, 32);
        volume.MinimumLuminance = (uint)reader.ReadBits(payload, 32);
        return volume;
    }

    static Av1ScalabilityMetadata ReadScalability(
        ReadOnlySpan<byte> payload,
        BitReader reader)
    {
        Av1ScalabilityMetadata scalability = new Av1ScalabilityMetadata();
        scalability.ModeIdc = (int)reader.ReadBits(payload, 8);
        if (scalability.ModeIdc != ScalabilityStructureMode)
        {
            return scalability;
        }

        scalability.HasScalabilityStructure = true;
        scalability.SpatialLayerCount = (int)reader.ReadBits(payload, 2) + 1;
        scalability.SpatialLayerDimensionsPresent = reader.ReadBit(payload) != 0;
        scalability.SpatialLayerDescriptionsPresent = reader.ReadBit(payload) != 0;
        scalability.TemporalGroupDescriptionsPresent = reader.ReadBit(payload) != 0;
        if (reader.ReadBits(payload, 3) != 0)
        {
            reader.Code = AvifResultCode.InvalidData;
            return scalability;
        }

        if (scalability.SpatialLayerDimensionsPresent)
        {
            scalability.SpatialLayerMaximumWidths =
                new int[scalability.SpatialLayerCount];
            scalability.SpatialLayerMaximumHeights =
                new int[scalability.SpatialLayerCount];
            int index = 0;
            while (index < scalability.SpatialLayerCount)
            {
                scalability.SpatialLayerMaximumWidths[index] =
                    (int)reader.ReadBits(payload, 16);
                scalability.SpatialLayerMaximumHeights[index] =
                    (int)reader.ReadBits(payload, 16);
                index++;
            }
        }

        if (scalability.SpatialLayerDescriptionsPresent)
        {
            scalability.SpatialLayerReferenceIds =
                new int[scalability.SpatialLayerCount];
            int index = 0;
            while (index < scalability.SpatialLayerCount)
            {
                scalability.SpatialLayerReferenceIds[index] =
                    (int)reader.ReadBits(payload, 8);
                index++;
            }
        }

        if (scalability.TemporalGroupDescriptionsPresent)
        {
            int groupCount = (int)reader.ReadBits(payload, 8);
            scalability.TemporalGroups = new Av1TemporalGroupMetadata[groupCount];
            int index = 0;
            while (index < groupCount)
            {
                Av1TemporalGroupMetadata group = new Av1TemporalGroupMetadata();
                group.TemporalId = (int)reader.ReadBits(payload, 3);
                group.TemporalSwitchingUpPoint = reader.ReadBit(payload) != 0;
                group.SpatialSwitchingUpPoint = reader.ReadBit(payload) != 0;
                int referenceCount = (int)reader.ReadBits(payload, 3);
                group.ReferencePictureDifferences = new int[referenceCount];
                int reference = 0;
                while (reference < referenceCount)
                {
                    group.ReferencePictureDifferences[reference] =
                        (int)reader.ReadBits(payload, 8);
                    reference++;
                }

                scalability.TemporalGroups[index] = group;
                index++;
            }
        }

        return scalability;
    }

    static AvifResultCode ReadItuT35(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1Metadata metadata)
    {
        if (!reader.IsByteAligned())
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        int dataStart = (int)(reader.BitPosition / 8);
        int trailingIndex = FindTrailingByte(payload, dataStart);
        if (trailingIndex < 0 || dataStart >= trailingIndex)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        Av1ItuT35Metadata itu = new Av1ItuT35Metadata();
        itu.CountryCode = payload[dataStart];
        dataStart++;
        if (itu.CountryCode == 255)
        {
            if (dataStart >= trailingIndex)
            {
                return AvifResultCode.InvalidAv1Bitstream;
            }

            itu.HasCountryCodeExtension = true;
            itu.CountryCodeExtension = payload[dataStart];
            dataStart++;
        }

        itu.Payload = payload.Slice(dataStart, trailingIndex - dataStart).ToArray();
        ReadHdr10PlusEnvelope(itu);
        metadata.ItuT35 = itu;
        reader.BitPosition = (long)trailingIndex * 8;
        return AvifResultCode.Success;
    }

    static void ReadHdr10PlusEnvelope(Av1ItuT35Metadata itu)
    {
        if (itu.CountryCode != 181 || itu.Payload.Length < 6)
        {
            return;
        }

        itu.TerminalProviderCode = itu.Payload[0] << 8 | itu.Payload[1];
        itu.TerminalProviderOrientedCode = itu.Payload[2] << 8 | itu.Payload[3];
        itu.ApplicationIdentifier = itu.Payload[4];
        itu.ApplicationMode = itu.Payload[5];
        itu.HasHdr10PlusEnvelope = itu.TerminalProviderCode == 60
            && itu.TerminalProviderOrientedCode == 1
            && itu.ApplicationIdentifier == 4
            && itu.ApplicationMode == 1;
        if (itu.HasHdr10PlusEnvelope)
        {
            itu.Hdr10PlusPayload = itu.Payload.AsSpan(6).ToArray();
        }
    }

    static Av1TimecodeMetadata ReadTimecode(
        ReadOnlySpan<byte> payload,
        BitReader reader)
    {
        Av1TimecodeMetadata timecode = new Av1TimecodeMetadata();
        timecode.CountingType = (int)reader.ReadBits(payload, 5);
        timecode.FullTimestamp = reader.ReadBit(payload) != 0;
        timecode.Discontinuity = reader.ReadBit(payload) != 0;
        timecode.CountDropped = reader.ReadBit(payload) != 0;
        timecode.FrameCount = (int)reader.ReadBits(payload, 9);
        if (timecode.FullTimestamp)
        {
            timecode.SecondsPresent = true;
            timecode.Seconds = (int)reader.ReadBits(payload, 6);
            timecode.MinutesPresent = true;
            timecode.Minutes = (int)reader.ReadBits(payload, 6);
            timecode.HoursPresent = true;
            timecode.Hours = (int)reader.ReadBits(payload, 5);
        }
        else
        {
            timecode.SecondsPresent = reader.ReadBit(payload) != 0;
            if (timecode.SecondsPresent)
            {
                timecode.Seconds = (int)reader.ReadBits(payload, 6);
                timecode.MinutesPresent = reader.ReadBit(payload) != 0;
                if (timecode.MinutesPresent)
                {
                    timecode.Minutes = (int)reader.ReadBits(payload, 6);
                    timecode.HoursPresent = reader.ReadBit(payload) != 0;
                    if (timecode.HoursPresent)
                    {
                        timecode.Hours = (int)reader.ReadBits(payload, 5);
                    }
                }
            }
        }

        timecode.TimeOffsetLength = (int)reader.ReadBits(payload, 5);
        if (timecode.TimeOffsetLength > 0)
        {
            timecode.TimeOffsetValue = reader.ReadBits(
                payload,
                timecode.TimeOffsetLength);
        }

        if ((timecode.SecondsPresent && timecode.Seconds > 59)
            || (timecode.MinutesPresent && timecode.Minutes > 59)
            || (timecode.HoursPresent && timecode.Hours > 23))
        {
            reader.Code = AvifResultCode.InvalidData;
        }

        return timecode;
    }

    static AvifResultCode ReadOpaquePayload(
        ReadOnlySpan<byte> payload,
        BitReader reader,
        ParsedAv1Metadata metadata)
    {
        if (!reader.IsByteAligned())
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        int dataStart = (int)(reader.BitPosition / 8);
        int trailingIndex = FindTrailingByte(payload, dataStart);
        if (trailingIndex < 0)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        metadata.OpaquePayload = payload.Slice(
            dataStart,
            trailingIndex - dataStart).ToArray();
        reader.BitPosition = (long)trailingIndex * 8;
        return AvifResultCode.Success;
    }

    static int FindTrailingByte(ReadOnlySpan<byte> payload, int minimumIndex)
    {
        int index = payload.Length - 1;
        while (index >= minimumIndex && payload[index] == 0)
        {
            index--;
        }

        if (index < minimumIndex || payload[index] != 128)
        {
            return -1;
        }

        return index;
    }

    static AvifResultCode NormalizeCode(AvifResultCode code)
    {
        return code == AvifResultCode.TruncatedData
            ? AvifResultCode.TruncatedData
            : AvifResultCode.InvalidAv1Bitstream;
    }

    static Av1MetadataParseResult CreateResult(
        AvifResultCode code,
        ParsedAv1Metadata? metadata)
    {
        return new Av1MetadataParseResult(code, metadata);
    }
}
