using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace avif;

internal class AvifWriterTimedSample
{
    internal byte[] Data;
    internal uint Duration;
    internal bool KeyFrame;
    internal int ForwardKeyFrameDistance;
    internal bool MultipleFrames;
    internal bool SwitchFrame;
    internal List<uint> MetadataGroupParameters;

    internal AvifWriterTimedSample(byte[] data, AvifFrame frame)
    {
        Data = (byte[])data.Clone();
        Duration = frame.GetDuration();
        KeyFrame = frame.IsKeyFrame();
        ForwardKeyFrameDistance = frame.GetForwardKeyFrameDistance();
        MultipleFrames = frame.HasMultipleFrames();
        SwitchFrame = frame.IsSwitchFrame();
        MetadataGroupParameters = new List<uint>();
        int index = 0;
        while (index < frame.GetMetadataGroupCount())
        {
            MetadataGroupParameters.Add(frame.GetMetadataGroupParameter(index));
            index++;
        }
    }
}

internal class AvifWriterTimedTrack
{
    internal int Width;
    internal int Height;
    internal byte[] Av1Configuration;
    internal List<byte[]> ColorProperties;
    internal List<AvifWriterTimedSample> Samples;
    internal bool Alpha;

    internal AvifWriterTimedTrack()
    {
        Av1Configuration = System.Array.Empty<byte>();
        ColorProperties = new List<byte[]>();
        Samples = new List<AvifWriterTimedSample>();
    }
}

internal class AvifWriterTimedSequence
{
    internal uint Timescale;
    internal bool InfiniteRepetition;
    internal ulong RepetitionCount;
    internal AvifWriterTimedTrack? ColorTrack;
    internal AvifWriterTimedTrack? AlphaTrack;
}

internal class CanonicalTimedMovieWriteResult
{
    internal AvifResultCode Code;
    internal byte[] Data;

    internal CanonicalTimedMovieWriteResult(
        AvifResultCode code,
        byte[] data)
    {
        Code = code;
        Data = data;
    }
}

internal static class CanonicalTimedMovieWriter
{
    internal static CanonicalTimedMovieWriteResult Append(
        byte[] itemContainer,
        AvifWriterTimedSequence sequence,
        long maximumBytes)
    {
        if (itemContainer == null || sequence == null
            || sequence.ColorTrack == null || sequence.Timescale == 0
            || maximumBytes < itemContainer.LongLength)
        {
            return new CanonicalTimedMovieWriteResult(
                AvifResultCode.InvalidArgument,
                System.Array.Empty<byte>());
        }
        long mediaPayloadLength = SumSampleBytes(sequence.ColorTrack);
        if (sequence.AlphaTrack != null)
        {
            mediaPayloadLength = AddLength(
                mediaPayloadLength,
                SumSampleBytes(sequence.AlphaTrack));
        }
        if (mediaPayloadLength < 0 || mediaPayloadLength > uint.MaxValue - 8
            || itemContainer.LongLength > maximumBytes - 8 - mediaPayloadLength)
        {
            return new CanonicalTimedMovieWriteResult(
                AvifResultCode.LimitExceeded,
                System.Array.Empty<byte>());
        }

        long remaining = maximumBytes - itemContainer.LongLength;
        int initialCapacity = remaining > 1048576 ? 1048576 : (int)remaining;
        IsoBoxWriter writer = new IsoBoxWriter(initialCapacity, remaining);
        List<ulong> colorOffsets = new List<ulong>();
        List<ulong> alphaOffsets = new List<ulong>();
        ulong runningOffset = (ulong)itemContainer.LongLength + 8;
        writer.BeginBox("mdat");
        WriteSamples(writer, sequence.ColorTrack, colorOffsets, runningOffset);
        runningOffset += (ulong)SumSampleBytes(sequence.ColorTrack);
        if (sequence.AlphaTrack != null)
        {
            WriteSamples(writer, sequence.AlphaTrack, alphaOffsets, runningOffset);
        }
        writer.EndBox();
        WriteMovie(writer, sequence, colorOffsets, alphaOffsets);
        IsoBoxWriteResult extension = writer.Finish();
        writer.Close();
        if (extension.Code != AvifResultCode.Success
            || itemContainer.LongLength > int.MaxValue - extension.Data.LongLength)
        {
            return new CanonicalTimedMovieWriteResult(
                extension.Code == AvifResultCode.Success
                    ? AvifResultCode.LimitExceeded : extension.Code,
                System.Array.Empty<byte>());
        }
        byte[] result = new byte[itemContainer.Length + extension.Data.Length];
        itemContainer.CopyTo(result, 0);
        extension.Data.CopyTo(result, itemContainer.Length);
        return new CanonicalTimedMovieWriteResult(
            AvifResultCode.Success,
            result);
    }

    static void WriteMovie(
        IsoBoxWriter writer,
        AvifWriterTimedSequence sequence,
        List<ulong> colorOffsets,
        List<ulong> alphaOffsets)
    {
        ulong mediaDuration = SumDurations(sequence.ColorTrack);
        ulong presentationDuration = GetPresentationDuration(
            mediaDuration,
            sequence.InfiniteRepetition,
            sequence.RepetitionCount);
        writer.BeginBox("moov");
        WriteMovieHeader(
            writer,
            sequence.Timescale,
            presentationDuration,
            sequence.AlphaTrack == null ? 2U : 3U);
        WriteTrack(
            writer,
            1,
            0,
            sequence.Timescale,
            mediaDuration,
            presentationDuration,
            sequence.InfiniteRepetition,
            sequence.RepetitionCount,
            sequence.ColorTrack,
            colorOffsets);
        if (sequence.AlphaTrack != null)
        {
            WriteTrack(
                writer,
                2,
                1,
                sequence.Timescale,
                mediaDuration,
                presentationDuration,
                sequence.InfiniteRepetition,
                sequence.RepetitionCount,
                sequence.AlphaTrack,
                alphaOffsets);
        }
        writer.EndBox();
    }

    static void WriteMovieHeader(
        IsoBoxWriter writer,
        uint timescale,
        ulong duration,
        uint nextTrackId)
    {
        byte[] payload = new byte[108];
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(16, 4), timescale);
        BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(20, 8), duration);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(28, 4), 65536);
        BinaryPrimitives.WriteUInt16BigEndian(payload.AsSpan(32, 2), 256);
        WriteIdentityMatrix(payload, 44);
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(104, 4), nextTrackId);
        writer.BeginFullBox("mvhd", 1, 0);
        writer.Write(payload);
        writer.EndBox();
    }

    static void WriteTrack(
        IsoBoxWriter writer,
        uint trackId,
        uint auxiliaryMasterTrackId,
        uint timescale,
        ulong mediaDuration,
        ulong presentationDuration,
        bool infiniteRepetition,
        ulong repetitionCount,
        AvifWriterTimedTrack? track,
        List<ulong> offsets)
    {
        writer.BeginBox("trak");
        WriteTrackHeader(
            writer,
            trackId,
            presentationDuration,
            (track ?? throw new InvalidOperationException("Invalid internal state.")).Width,
            track.Height);
        if (auxiliaryMasterTrackId != 0)
        {
            writer.BeginBox("tref");
            writer.BeginBox("auxl");
            WriteUInt32(writer, auxiliaryMasterTrackId);
            writer.EndBox();
            writer.EndBox();
        }
        WriteEditList(
            writer,
            mediaDuration,
            infiniteRepetition,
            repetitionCount);
        writer.BeginBox("mdia");
        WriteMediaHeader(writer, timescale, mediaDuration);
        WriteHandler(writer);
        writer.BeginBox("minf");
        WriteVideoMediaHeader(writer);
        WriteDataInformation(writer);
        writer.BeginBox("stbl");
        WriteSampleDescription(writer, track);
        WriteTimeToSample(writer, track);
        WriteSampleToChunk(writer);
        WriteSampleSizes(writer, track);
        WriteChunkOffsets(writer, offsets);
        WriteSyncSamples(writer, track);
        WriteDependencies(writer, track);
        WriteSampleGroups(writer, track);
        writer.EndBox();
        writer.EndBox();
        writer.EndBox();
        writer.EndBox();
    }

    static void WriteTrackHeader(
        IsoBoxWriter writer,
        uint trackId,
        ulong duration,
        int width,
        int height)
    {
        byte[] payload = new byte[92];
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(16, 4), trackId);
        BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(24, 8), duration);
        WriteIdentityMatrix(payload, 48);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(84, 4),
            (uint)width << 16);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(88, 4),
            (uint)height << 16);
        writer.BeginFullBox("tkhd", 1, 3);
        writer.Write(payload);
        writer.EndBox();
    }

    static void WriteEditList(
        IsoBoxWriter writer,
        ulong mediaDuration,
        bool infiniteRepetition,
        ulong repetitionCount)
    {
        byte[] payload = new byte[24];
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(0, 4), 1);
        BinaryPrimitives.WriteUInt64BigEndian(
            payload.AsSpan(4, 8),
            mediaDuration);
        BinaryPrimitives.WriteInt64BigEndian(payload.AsSpan(12, 8), 0);
        BinaryPrimitives.WriteInt16BigEndian(payload.AsSpan(20, 2), 1);
        writer.BeginBox("edts");
        writer.BeginFullBox(
            "elst",
            1,
            infiniteRepetition || repetitionCount > 0 ? 1 : 0);
        writer.Write(payload);
        writer.EndBox();
        writer.EndBox();
    }

    static void WriteMediaHeader(
        IsoBoxWriter writer,
        uint timescale,
        ulong duration)
    {
        byte[] payload = new byte[32];
        BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(16, 4), timescale);
        BinaryPrimitives.WriteUInt64BigEndian(payload.AsSpan(20, 8), duration);
        BinaryPrimitives.WriteUInt16BigEndian(
            payload.AsSpan(28, 2),
            PackLanguage("und"));
        writer.BeginFullBox("mdhd", 1, 0);
        writer.Write(payload);
        writer.EndBox();
    }

    static void WriteHandler(IsoBoxWriter writer)
    {
        byte[] name = Encoding.UTF8.GetBytes("PictureHandler");
        byte[] payload = new byte[21 + name.Length];
        WriteFourCc(payload, 4, "pict");
        name.CopyTo(payload, 20);
        writer.BeginFullBox("hdlr", 0, 0);
        writer.Write(payload);
        writer.EndBox();
    }

    static void WriteVideoMediaHeader(IsoBoxWriter writer)
    {
        writer.BeginFullBox("vmhd", 0, 1);
        writer.Write(new byte[8]);
        writer.EndBox();
    }

    static void WriteDataInformation(IsoBoxWriter writer)
    {
        writer.BeginBox("dinf");
        writer.BeginFullBox("dref", 0, 0);
        WriteUInt32(writer, 1);
        writer.BeginFullBox("url ", 0, 1);
        writer.EndBox();
        writer.EndBox();
        writer.EndBox();
    }

    static void WriteSampleDescription(
        IsoBoxWriter writer,
        AvifWriterTimedTrack track)
    {
        writer.BeginFullBox("stsd", 0, 0);
        WriteUInt32(writer, 1);
        writer.BeginBox("av01");
        byte[] fields = new byte[78];
        BinaryPrimitives.WriteUInt16BigEndian(fields.AsSpan(6, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(
            fields.AsSpan(24, 2),
            (ushort)track.Width);
        BinaryPrimitives.WriteUInt16BigEndian(
            fields.AsSpan(26, 2),
            (ushort)track.Height);
        BinaryPrimitives.WriteUInt32BigEndian(
            fields.AsSpan(28, 4),
            72U << 16);
        BinaryPrimitives.WriteUInt32BigEndian(
            fields.AsSpan(32, 4),
            72U << 16);
        BinaryPrimitives.WriteUInt16BigEndian(fields.AsSpan(40, 2), 1);
        fields[42] = 3;
        fields[43] = (byte)'A';
        fields[44] = (byte)'V';
        fields[45] = (byte)'1';
        BinaryPrimitives.WriteUInt16BigEndian(fields.AsSpan(74, 2), 24);
        BinaryPrimitives.WriteUInt16BigEndian(
            fields.AsSpan(76, 2),
            ushort.MaxValue);
        writer.Write(fields);
        writer.Write(track.Av1Configuration);
        int propertyIndex = 0;
        while (propertyIndex < track.ColorProperties.Count)
        {
            writer.Write(track.ColorProperties[propertyIndex]);
            propertyIndex++;
        }
        if (track.Alpha)
        {
            writer.BeginFullBox("auxi", 0, 0);
            writer.Write(Encoding.UTF8.GetBytes(
                "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha"));
            writer.Write(new byte[] { 0 });
            writer.EndBox();
        }
        writer.EndBox();
        writer.EndBox();
    }

    static void WriteTimeToSample(
        IsoBoxWriter writer,
        AvifWriterTimedTrack track)
    {
        List<uint> counts = new List<uint>();
        List<uint> durations = new List<uint>();
        int index = 0;
        while (index < track.Samples.Count)
        {
            uint duration = track.Samples[index].Duration;
            if (durations.Count != 0
                && durations[durations.Count - 1] == duration)
            {
                counts[counts.Count - 1]++;
            }
            else
            {
                counts.Add(1);
                durations.Add(duration);
            }
            index++;
        }
        writer.BeginFullBox("stts", 0, 0);
        WriteUInt32(writer, (uint)counts.Count);
        index = 0;
        while (index < counts.Count)
        {
            WriteUInt32(writer, counts[index]);
            WriteUInt32(writer, durations[index]);
            index++;
        }
        writer.EndBox();
    }

    static void WriteSampleToChunk(IsoBoxWriter writer)
    {
        writer.BeginFullBox("stsc", 0, 0);
        WriteUInt32(writer, 1);
        WriteUInt32(writer, 1);
        WriteUInt32(writer, 1);
        WriteUInt32(writer, 1);
        writer.EndBox();
    }

    static void WriteSampleSizes(
        IsoBoxWriter writer,
        AvifWriterTimedTrack track)
    {
        writer.BeginFullBox("stsz", 0, 0);
        WriteUInt32(writer, 0);
        WriteUInt32(writer, (uint)track.Samples.Count);
        int index = 0;
        while (index < track.Samples.Count)
        {
            WriteUInt32(writer, (uint)track.Samples[index].Data.Length);
            index++;
        }
        writer.EndBox();
    }

    static void WriteChunkOffsets(
        IsoBoxWriter writer,
        List<ulong> offsets)
    {
        writer.BeginFullBox("co64", 0, 0);
        WriteUInt32(writer, (uint)offsets.Count);
        int index = 0;
        while (index < offsets.Count)
        {
            WriteUInt64(writer, offsets[index]);
            index++;
        }
        writer.EndBox();
    }

    static void WriteSyncSamples(
        IsoBoxWriter writer,
        AvifWriterTimedTrack track)
    {
        int syncCount = 0;
        int index = 0;
        while (index < track.Samples.Count)
        {
            if (track.Samples[index].KeyFrame)
            {
                syncCount++;
            }
            index++;
        }
        if (syncCount == track.Samples.Count)
        {
            return;
        }
        writer.BeginFullBox("stss", 0, 0);
        WriteUInt32(writer, (uint)syncCount);
        index = 0;
        while (index < track.Samples.Count)
        {
            if (track.Samples[index].KeyFrame)
            {
                WriteUInt32(writer, (uint)index + 1);
            }
            index++;
        }
        writer.EndBox();
    }

    static void WriteDependencies(
        IsoBoxWriter writer,
        AvifWriterTimedTrack track)
    {
        byte[] payload = new byte[track.Samples.Count];
        int index = 0;
        while (index < track.Samples.Count)
        {
            payload[index] = track.Samples[index].KeyFrame
                ? (byte)32 : (byte)16;
            index++;
        }
        writer.BeginFullBox("sdtp", 0, 0);
        writer.Write(payload);
        writer.EndBox();
    }

    static void WriteSampleGroups(
        IsoBoxWriter writer,
        AvifWriterTimedTrack track)
    {
        WriteForwardGroups(writer, track);
        WriteBooleanGroup(writer, track, "av1m", 0);
        WriteBooleanGroup(writer, track, "av1s", 1);
        WriteMetadataGroups(writer, track);
    }

    static void WriteForwardGroups(
        IsoBoxWriter writer,
        AvifWriterTimedTrack track)
    {
        List<int> distances = new List<int>();
        int index = 0;
        while (index < track.Samples.Count)
        {
            int distance = track.Samples[index].ForwardKeyFrameDistance;
            if (distance >= 0 && FindInt(distances, distance) < 0)
            {
                distances.Add(distance);
            }
            index++;
        }
        if (distances.Count == 0)
        {
            return;
        }
        writer.BeginFullBox("sgpd", 1, 0);
        WriteFourCc(writer, "av1f");
        WriteUInt32(writer, 1);
        WriteUInt32(writer, (uint)distances.Count);
        index = 0;
        while (index < distances.Count)
        {
            writer.Write(new byte[] { (byte)distances[index] });
            index++;
        }
        writer.EndBox();
        List<uint> memberships = new List<uint>();
        index = 0;
        while (index < track.Samples.Count)
        {
            int distance = track.Samples[index].ForwardKeyFrameDistance;
            int description = FindInt(distances, distance);
            memberships.Add(description < 0 ? 0U : (uint)description + 1);
            index++;
        }
        WriteMapping(writer, "av1f", false, 0, memberships);
    }

    static void WriteBooleanGroup(
        IsoBoxWriter writer,
        AvifWriterTimedTrack track,
        string type,
        int selector)
    {
        List<uint> memberships = new List<uint>();
        bool any = false;
        int index = 0;
        while (index < track.Samples.Count)
        {
            bool member = selector == 0
                ? track.Samples[index].MultipleFrames
                : track.Samples[index].SwitchFrame;
            memberships.Add(member ? 1U : 0U);
            any = any || member;
            index++;
        }
        if (!any)
        {
            return;
        }
        writer.BeginFullBox("sgpd", 1, 0);
        WriteFourCc(writer, type);
        WriteUInt32(writer, 0);
        WriteUInt32(writer, 1);
        WriteUInt32(writer, 0);
        writer.EndBox();
        WriteMapping(writer, type, false, 0, memberships);
    }

    static void WriteMetadataGroups(
        IsoBoxWriter writer,
        AvifWriterTimedTrack track)
    {
        List<uint> parameters = new List<uint>();
        int sampleIndex = 0;
        while (sampleIndex < track.Samples.Count)
        {
            int parameterIndex = 0;
            while (parameterIndex
                < track.Samples[sampleIndex].MetadataGroupParameters.Count)
            {
                uint parameter = track.Samples[sampleIndex]
                    .MetadataGroupParameters[parameterIndex];
                if (FindUInt(parameters, parameter) < 0)
                {
                    parameters.Add(parameter);
                }
                parameterIndex++;
            }
            sampleIndex++;
        }
        if (parameters.Count == 0)
        {
            return;
        }
        writer.BeginFullBox("sgpd", 1, 0);
        WriteFourCc(writer, "av1M");
        WriteUInt32(writer, 0);
        WriteUInt32(writer, 1);
        WriteUInt32(writer, 0);
        writer.EndBox();
        int index = 0;
        while (index < parameters.Count)
        {
            List<uint> memberships = new List<uint>();
            sampleIndex = 0;
            while (sampleIndex < track.Samples.Count)
            {
                memberships.Add(FindUInt(
                        track.Samples[sampleIndex].MetadataGroupParameters,
                        parameters[index]) >= 0
                    ? 1U : 0U);
                sampleIndex++;
            }
            WriteMapping(
                writer,
                "av1M",
                true,
                parameters[index],
                memberships);
            index++;
        }
    }

    static void WriteMapping(
        IsoBoxWriter writer,
        string type,
        bool hasParameter,
        uint parameter,
        List<uint> memberships)
    {
        List<uint> counts = new List<uint>();
        List<uint> values = new List<uint>();
        int index = 0;
        while (index < memberships.Count)
        {
            uint value = memberships[index];
            if (values.Count != 0 && values[values.Count - 1] == value)
            {
                counts[counts.Count - 1]++;
            }
            else
            {
                counts.Add(1);
                values.Add(value);
            }
            index++;
        }
        writer.BeginFullBox("sbgp", hasParameter ? 1 : 0, 0);
        WriteFourCc(writer, type);
        if (hasParameter)
        {
            WriteUInt32(writer, parameter);
        }
        WriteUInt32(writer, (uint)counts.Count);
        index = 0;
        while (index < counts.Count)
        {
            WriteUInt32(writer, counts[index]);
            WriteUInt32(writer, values[index]);
            index++;
        }
        writer.EndBox();
    }

    static void WriteSamples(
        IsoBoxWriter writer,
        AvifWriterTimedTrack track,
        List<ulong> offsets,
        ulong firstOffset)
    {
        ulong offset = firstOffset;
        int index = 0;
        while (index < track.Samples.Count)
        {
            offsets.Add(offset);
            writer.Write(track.Samples[index].Data);
            offset += (uint)track.Samples[index].Data.Length;
            index++;
        }
    }

    static long SumSampleBytes(AvifWriterTimedTrack track)
    {
        if (track == null)
        {
            return -1;
        }
        long total = 0;
        int index = 0;
        while (index < track.Samples.Count)
        {
            total = AddLength(total, track.Samples[index].Data.LongLength);
            if (total < 0)
            {
                return -1;
            }
            index++;
        }
        return total;
    }

    static long AddLength(long first, long second)
    {
        return first < 0 || second < 0 || first > long.MaxValue - second
            ? -1 : first + second;
    }

    static ulong SumDurations(AvifWriterTimedTrack? track)
    {
        ulong total = 0;
        int index = 0;
        while (index < (track ?? throw new InvalidOperationException("Invalid internal state.")).Samples.Count)
        {
            total += track.Samples[index].Duration;
            index++;
        }
        return total;
    }

    static ulong GetPresentationDuration(
        ulong mediaDuration,
        bool infinite,
        ulong repetitionCount)
    {
        if (infinite)
        {
            return ulong.MaxValue;
        }
        ulong presentationCount = repetitionCount + 1;
        return presentationCount == 0
            || mediaDuration > ulong.MaxValue / presentationCount
            ? ulong.MaxValue : mediaDuration * presentationCount;
    }

    static void WriteIdentityMatrix(byte[] payload, int offset)
    {
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset, 4),
            65536);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset + 16, 4),
            65536);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(offset + 32, 4),
            1073741824);
    }

    static ushort PackLanguage(string language)
    {
        return (ushort)(((language[0] - 96) << 10)
            | ((language[1] - 96) << 5)
            | (language[2] - 96));
    }

    static int FindInt(List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    static int FindUInt(List<uint> values, uint value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    static void WriteFourCc(
        byte[] destination,
        int offset,
        string value)
    {
        destination[offset] = (byte)value[0];
        destination[offset + 1] = (byte)value[1];
        destination[offset + 2] = (byte)value[2];
        destination[offset + 3] = (byte)value[3];
    }

    static void WriteFourCc(IsoBoxWriter writer, string value)
    {
        byte[] data = new byte[4];
        WriteFourCc(data, 0, value);
        writer.Write(data);
    }

    static void WriteUInt32(IsoBoxWriter writer, uint value)
    {
        byte[] data = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(data, value);
        writer.Write(data);
    }

    static void WriteUInt64(IsoBoxWriter writer, ulong value)
    {
        byte[] data = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(data, value);
        writer.Write(data);
    }
}
