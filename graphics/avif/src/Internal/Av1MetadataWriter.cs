using System;

namespace avif;

// AV1 specification sections 5.8 and 6.7.
internal static class Av1MetadataWriter
{
    const int ScalabilityStructureMode = 14;

    internal static Av1WriteResult WritePayload(ParsedAv1Metadata metadata)
    {
        if (metadata == null)
        {
            return Fail(AvifResultCode.InvalidArgument);
        }

        long capacity = CalculateCapacity(metadata);
        if (capacity < 1 || capacity > int.MaxValue)
        {
            return Fail(capacity > int.MaxValue
                ? AvifResultCode.LimitExceeded
                : AvifResultCode.InvalidArgument);
        }

        byte[] payload = new byte[(int)capacity];
        byte[] type = Av1ObuWriter.WriteLeb128(metadata.MetadataType);
        type.CopyTo(payload, 0);
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.BitPosition = (long)type.Length * 8;
        if (metadata.MetadataType == (ulong)Av1MetadataType.HdrContentLightLevel)
        {
            WriteContentLightLevel(payload, writer, metadata.ContentLightLevel);
        }
        else if (metadata.MetadataType
            == (ulong)Av1MetadataType.HdrMasteringDisplayColorVolume)
        {
            WriteMasteringDisplayColorVolume(
                payload,
                writer,
                metadata.MasteringDisplayColorVolume);
        }
        else if (metadata.MetadataType == (ulong)Av1MetadataType.Scalability)
        {
            WriteScalability(payload, writer, metadata.Scalability);
        }
        else if (metadata.MetadataType == (ulong)Av1MetadataType.ItuTT35)
        {
            WriteItuT35(payload, writer, metadata.ItuT35);
        }
        else if (metadata.MetadataType == (ulong)Av1MetadataType.Timecode)
        {
            WriteTimecode(payload, writer, metadata.Timecode);
        }
        else
        {
            WriteOpaque(payload, writer, metadata.OpaquePayload);
        }

        if (writer.Code != AvifResultCode.Success)
        {
            return Fail(writer.Code);
        }

        writer.WriteTrailingBits(payload);
        if (writer.Code != AvifResultCode.Success)
        {
            return Fail(writer.Code);
        }

        int byteCount = (int)(writer.BitPosition / 8);
        byte[] compact = new byte[byteCount];
        payload.AsSpan(0, byteCount).CopyTo(compact);
        return new Av1WriteResult(AvifResultCode.Success, compact);
    }

    internal static Av1WriteResult WriteObu(
        ParsedAv1Metadata metadata,
        bool hasExtension,
        int temporalId,
        int spatialId)
    {
        Av1WriteResult payload = WritePayload(metadata);
        if (payload.Code != AvifResultCode.Success)
        {
            return payload;
        }

        return Av1ObuWriter.WriteObu(
            Av1ObuType.Metadata,
            payload.Data,
            hasExtension,
            temporalId,
            spatialId);
    }

    static void WriteContentLightLevel(
        byte[] payload,
        BitWriter writer,
        Av1ContentLightLevelMetadata? content)
    {
        if (content == null
            || !FitsUnsigned(content.MaximumContentLightLevel, 16)
            || !FitsUnsigned(content.MaximumFrameAverageLightLevel, 16))
        {
            writer.Code = AvifResultCode.InvalidArgument;
            return;
        }

        writer.WriteBits(payload, (ulong)content.MaximumContentLightLevel, 16);
        writer.WriteBits(
            payload,
            (ulong)content.MaximumFrameAverageLightLevel,
            16);
    }

    static void WriteMasteringDisplayColorVolume(
        byte[] payload,
        BitWriter writer,
        Av1MasteringDisplayColorVolumeMetadata? volume)
    {
        if (volume == null || volume.PrimaryChromaticityX == null
            || volume.PrimaryChromaticityY == null
            || volume.PrimaryChromaticityX.Length != 3
            || volume.PrimaryChromaticityY.Length != 3
            || !FitsUnsigned(volume.WhitePointChromaticityX, 16)
            || !FitsUnsigned(volume.WhitePointChromaticityY, 16))
        {
            writer.Code = AvifResultCode.InvalidArgument;
            return;
        }

        int index = 0;
        while (index < 3)
        {
            if (!FitsUnsigned(volume.PrimaryChromaticityX[index], 16)
                || !FitsUnsigned(volume.PrimaryChromaticityY[index], 16))
            {
                writer.Code = AvifResultCode.InvalidArgument;
                return;
            }

            writer.WriteBits(payload, (ulong)volume.PrimaryChromaticityX[index], 16);
            writer.WriteBits(payload, (ulong)volume.PrimaryChromaticityY[index], 16);
            index++;
        }

        writer.WriteBits(payload, (ulong)volume.WhitePointChromaticityX, 16);
        writer.WriteBits(payload, (ulong)volume.WhitePointChromaticityY, 16);
        writer.WriteBits(payload, volume.MaximumLuminance, 32);
        writer.WriteBits(payload, volume.MinimumLuminance, 32);
    }

    static void WriteScalability(
        byte[] payload,
        BitWriter writer,
        Av1ScalabilityMetadata? scalability)
    {
        if (scalability == null || !FitsUnsigned(scalability.ModeIdc, 8))
        {
            writer.Code = AvifResultCode.InvalidArgument;
            return;
        }

        writer.WriteBits(payload, (ulong)scalability.ModeIdc, 8);
        if (scalability.ModeIdc != ScalabilityStructureMode)
        {
            return;
        }

        if (scalability.SpatialLayerCount < 1 || scalability.SpatialLayerCount > 4
            || (scalability.SpatialLayerDimensionsPresent
                && !HasLayerValues(
                    scalability.SpatialLayerMaximumWidths,
                    scalability.SpatialLayerCount,
                    16))
            || (scalability.SpatialLayerDimensionsPresent
                && !HasLayerValues(
                    scalability.SpatialLayerMaximumHeights,
                    scalability.SpatialLayerCount,
                    16))
            || (scalability.SpatialLayerDescriptionsPresent
                && !HasLayerValues(
                    scalability.SpatialLayerReferenceIds,
                    scalability.SpatialLayerCount,
                    8))
            || (scalability.TemporalGroupDescriptionsPresent
                && (scalability.TemporalGroups == null
                    || scalability.TemporalGroups.Length > 255)))
        {
            writer.Code = AvifResultCode.InvalidArgument;
            return;
        }

        writer.WriteBits(payload, (ulong)(scalability.SpatialLayerCount - 1), 2);
        writer.WriteBit(payload, scalability.SpatialLayerDimensionsPresent ? 1 : 0);
        writer.WriteBit(payload, scalability.SpatialLayerDescriptionsPresent ? 1 : 0);
        writer.WriteBit(payload, scalability.TemporalGroupDescriptionsPresent ? 1 : 0);
        writer.WriteBits(payload, 0, 3);
        int index = 0;
        if (scalability.SpatialLayerDimensionsPresent)
        {
            while (index < scalability.SpatialLayerCount)
            {
                writer.WriteBits(
                    payload,
                    (ulong)scalability.SpatialLayerMaximumWidths[index],
                    16);
                writer.WriteBits(
                    payload,
                    (ulong)scalability.SpatialLayerMaximumHeights[index],
                    16);
                index++;
            }
        }

        if (scalability.SpatialLayerDescriptionsPresent)
        {
            index = 0;
            while (index < scalability.SpatialLayerCount)
            {
                writer.WriteBits(
                    payload,
                    (ulong)scalability.SpatialLayerReferenceIds[index],
                    8);
                index++;
            }
        }

        if (scalability.TemporalGroupDescriptionsPresent)
        {
            writer.WriteBits(payload, (ulong)scalability.TemporalGroups.Length, 8);
            index = 0;
            while (index < scalability.TemporalGroups.Length)
            {
                WriteTemporalGroup(payload, writer, scalability.TemporalGroups[index]);
                if (writer.Code != AvifResultCode.Success)
                {
                    return;
                }

                index++;
            }
        }
    }

    static void WriteTemporalGroup(
        byte[] payload,
        BitWriter writer,
        Av1TemporalGroupMetadata group)
    {
        if (group == null || !FitsUnsigned(group.TemporalId, 3)
            || group.ReferencePictureDifferences == null
            || group.ReferencePictureDifferences.Length > 7
            || !HasLayerValues(
                group.ReferencePictureDifferences,
                group.ReferencePictureDifferences.Length,
                8))
        {
            writer.Code = AvifResultCode.InvalidArgument;
            return;
        }

        writer.WriteBits(payload, (ulong)group.TemporalId, 3);
        writer.WriteBit(payload, group.TemporalSwitchingUpPoint ? 1 : 0);
        writer.WriteBit(payload, group.SpatialSwitchingUpPoint ? 1 : 0);
        writer.WriteBits(payload, (ulong)group.ReferencePictureDifferences.Length, 3);
        int index = 0;
        while (index < group.ReferencePictureDifferences.Length)
        {
            writer.WriteBits(
                payload,
                (ulong)group.ReferencePictureDifferences[index],
                8);
            index++;
        }
    }

    static void WriteItuT35(
        byte[] payload,
        BitWriter writer,
        Av1ItuT35Metadata? itu)
    {
        if (itu == null || !FitsUnsigned(itu.CountryCode, 8)
            || itu.Payload == null
            || (itu.CountryCode == 255
                && !FitsUnsigned(itu.CountryCodeExtension, 8)))
        {
            writer.Code = AvifResultCode.InvalidArgument;
            return;
        }

        writer.WriteBits(payload, (ulong)itu.CountryCode, 8);
        if (itu.CountryCode == 255)
        {
            writer.WriteBits(payload, (ulong)itu.CountryCodeExtension, 8);
        }

        WriteBytes(payload, writer, itu.Payload);
    }

    static void WriteTimecode(
        byte[] payload,
        BitWriter writer,
        Av1TimecodeMetadata? timecode)
    {
        if (timecode == null || !FitsUnsigned(timecode.CountingType, 5)
            || !FitsUnsigned(timecode.FrameCount, 9)
            || timecode.TimeOffsetLength < 0 || timecode.TimeOffsetLength > 31
            || (timecode.TimeOffsetLength < 64
                && timecode.TimeOffsetValue >> timecode.TimeOffsetLength != 0)
            || (timecode.SecondsPresent
                && (timecode.Seconds < 0 || timecode.Seconds > 59))
            || (timecode.MinutesPresent
                && (timecode.Minutes < 0 || timecode.Minutes > 59))
            || (timecode.HoursPresent
                && (timecode.Hours < 0 || timecode.Hours > 23)))
        {
            writer.Code = AvifResultCode.InvalidArgument;
            return;
        }

        writer.WriteBits(payload, (ulong)timecode.CountingType, 5);
        writer.WriteBit(payload, timecode.FullTimestamp ? 1 : 0);
        writer.WriteBit(payload, timecode.Discontinuity ? 1 : 0);
        writer.WriteBit(payload, timecode.CountDropped ? 1 : 0);
        writer.WriteBits(payload, (ulong)timecode.FrameCount, 9);
        if (timecode.FullTimestamp)
        {
            writer.WriteBits(payload, (ulong)timecode.Seconds, 6);
            writer.WriteBits(payload, (ulong)timecode.Minutes, 6);
            writer.WriteBits(payload, (ulong)timecode.Hours, 5);
        }
        else
        {
            writer.WriteBit(payload, timecode.SecondsPresent ? 1 : 0);
            if (timecode.SecondsPresent)
            {
                writer.WriteBits(payload, (ulong)timecode.Seconds, 6);
                writer.WriteBit(payload, timecode.MinutesPresent ? 1 : 0);
                if (timecode.MinutesPresent)
                {
                    writer.WriteBits(payload, (ulong)timecode.Minutes, 6);
                    writer.WriteBit(payload, timecode.HoursPresent ? 1 : 0);
                    if (timecode.HoursPresent)
                    {
                        writer.WriteBits(payload, (ulong)timecode.Hours, 5);
                    }
                }
            }
        }

        writer.WriteBits(payload, (ulong)timecode.TimeOffsetLength, 5);
        if (timecode.TimeOffsetLength > 0)
        {
            writer.WriteBits(
                payload,
                timecode.TimeOffsetValue,
                timecode.TimeOffsetLength);
        }
    }

    static void WriteOpaque(byte[] payload, BitWriter writer, byte[] opaque)
    {
        if (opaque == null)
        {
            writer.Code = AvifResultCode.InvalidArgument;
            return;
        }

        WriteBytes(payload, writer, opaque);
    }

    static void WriteBytes(byte[] payload, BitWriter writer, byte[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            writer.WriteBits(payload, values[index], 8);
            index++;
        }
    }

    static long CalculateCapacity(ParsedAv1Metadata metadata)
    {
        long size = 128 + Av1ObuWriter.WriteLeb128(metadata.MetadataType).Length;
        if (metadata.OpaquePayload != null)
        {
            size += metadata.OpaquePayload.LongLength;
        }

        if (metadata.ItuT35 != null && metadata.ItuT35.Payload != null)
        {
            size += metadata.ItuT35.Payload.LongLength;
        }

        if (metadata.Scalability != null)
        {
            size += (long)metadata.Scalability.SpatialLayerCount * 8;
            if (metadata.Scalability.TemporalGroups != null)
            {
                int index = 0;
                while (index < metadata.Scalability.TemporalGroups.Length)
                {
                    Av1TemporalGroupMetadata group =
                        metadata.Scalability.TemporalGroups[index];
                    size += 8;
                    if (group != null && group.ReferencePictureDifferences != null)
                    {
                        size += group.ReferencePictureDifferences.LongLength;
                    }

                    index++;
                }
            }
        }

        return size;
    }

    static bool HasLayerValues(int[] values, int count, int bitCount)
    {
        if (values == null || values.Length < count)
        {
            return false;
        }

        int index = 0;
        while (index < count)
        {
            if (!FitsUnsigned(values[index], bitCount))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool FitsUnsigned(int value, int bitCount)
    {
        return value >= 0 && (ulong)value < (1UL << bitCount);
    }

    static Av1WriteResult Fail(AvifResultCode code)
    {
        return new Av1WriteResult(code, System.Array.Empty<byte>());
    }
}
