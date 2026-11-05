using System.Buffers.Binary;

namespace avif.Tests;

public class SampleTableBoxTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void ParserExpandsChunkOffsetsTimingSyncAndDependencies(bool use64BitOffsets)
    {
        byte[] source = CreateSampleTable(use64BitOffsets, false, 0);

        ParsedSampleTable table = Parse(
            source, new AvifSecurityLimits(), AvifResultCode.Success);
        ParsedMovieTrack parsedTrack = new ParsedMovieTrack();
        parsedTrack.SampleTable = table;
        AvifTrack track = new AvifTrack(parsedTrack, 1000);
        AvifSampleDescription? description = track.GetSampleDescription(0);

        Assert.Multiple(() =>
        {
            Assert.That(track.GetSampleDescriptionCount(), Is.EqualTo(1));
            Assert.That((description ?? throw new InvalidOperationException("Invalid internal state.")).GetIndex(), Is.EqualTo(1));
            Assert.That((description ?? throw new InvalidOperationException("Invalid internal state.")).GetFormat(), Is.EqualTo("av01"));
            Assert.That(description.GetDataReferenceIndex(), Is.EqualTo(1));
            Assert.That(description.GetWidth(), Is.EqualTo(640));
            Assert.That(description.GetHeight(), Is.EqualTo(360));
            Assert.That(description.GetHorizontalResolution(), Is.EqualTo(72));
            Assert.That(description.GetVerticalResolution(), Is.EqualTo(72));
            Assert.That(description.GetCompressorName(), Is.EqualTo("AV1"));
            Assert.That(description.GetDepth(), Is.EqualTo(24));
            Assert.That(description.GetAv1Profile(), Is.Zero);
            Assert.That(description.GetAv1Tier(), Is.Zero);
            Assert.That(description.GetBitDepth(), Is.EqualTo(8));
            Assert.That(description.GetChannelCount(), Is.EqualTo(3));
            Assert.That(description.GetConfigObus(), Is.Empty);
            Assert.That(track.GetSampleCount(), Is.EqualTo(3));
            Assert.That((track.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetDescriptionIndex(), Is.EqualTo(1));
            Assert.That((track.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetOffset(), Is.EqualTo(8));
            Assert.That((track.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetSize(), Is.EqualTo(3));
            Assert.That((track.GetSample(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetOffset(), Is.EqualTo(11));
            Assert.That((track.GetSample(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetOffset(), Is.EqualTo(13));
            Assert.That((track.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetDecodeTime(), Is.Zero);
            Assert.That((track.GetSample(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetDecodeTime(), Is.EqualTo(100));
            Assert.That((track.GetSample(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetDecodeTime(), Is.EqualTo(200));
            Assert.That((track.GetSample(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetDuration(), Is.EqualTo(200));
            Assert.That((track.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).IsSync(), Is.True);
            Assert.That((track.GetSample(1) ?? throw new InvalidOperationException("Invalid internal state.")).IsSync(), Is.False);
            Assert.That((track.GetSample(2) ?? throw new InvalidOperationException("Invalid internal state.")).IsSync(), Is.True);
            Assert.That((track.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetDependsOn(), Is.EqualTo(2));
            Assert.That((track.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetLeadingDependency(), Is.Zero);
            Assert.That((track.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetRedundancy(), Is.Zero);
            Assert.That((track.GetSample(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetDependsOn(), Is.EqualTo(1));
            Assert.That((track.GetSample(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetIsDependedOn(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ParserExpandsConstantSampleSizes()
    {
        byte[] source = CreateSampleTable(false, true, 0);

        ParsedSampleTable table = Parse(
            source, new AvifSecurityLimits(), AvifResultCode.Success);

        Assert.Multiple(() =>
        {
            Assert.That(table.Samples[0].Size, Is.EqualTo(3));
            Assert.That(table.Samples[1].Size, Is.EqualTo(3));
            Assert.That(table.Samples[2].Size, Is.EqualTo(3));
            Assert.That(table.Samples[2].Offset, Is.EqualTo(14));
        });
    }

    [Test]
    public void ParserExposesAuxiliaryTypeInfoFromSampleEntry()
    {
        string alphaType = "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha";
        byte[] source = CreateSampleTable(false, false, 0, 0, alphaType);

        ParsedSampleTable table = Parse(
            source, new AvifSecurityLimits(), AvifResultCode.Success);
        AvifTrack track = new AvifTrack(new ParsedMovieTrack
        {
            SampleTable = table
        }, 1000);

        Assert.Multiple(() =>
        {
            Assert.That((track.GetSampleDescription(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetAuxiliaryType(),
                Is.EqualTo(alphaType));
            Assert.That(track.GetAuxiliaryType(), Is.EqualTo(alphaType));
        });
    }

    [Test]
    public void ParserRejectsDuplicateAuxiliaryTypeInfoBoxes()
    {
        byte[] source = CreateSampleTable(
            false, false, 0, 0, "alpha", 2);

        Parse(source, new AvifSecurityLimits(), AvifResultCode.InvalidTrack);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(10)]
    public void ParserRejectsMalformedDescriptionsAndSampleMappings(int fault)
    {
        byte[] source = CreateSampleTable(false, false, fault);

        Parse(source, new AvifSecurityLimits(), AvifResultCode.InvalidTrack);
    }

    [Test]
    public void ParserHonorsConfiguredSampleLimit()
    {
        byte[] source = CreateSampleTable(false, false, 0);
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSampleCount = 2;

        Parse(source, limits, AvifResultCode.LimitExceeded);
    }

    [Test]
    public void ParserAppliesAv1RandomAccessFrameAndMetadataGroups()
    {
        byte[] source = CreateSampleTable(false, false, 0, 1);

        ParsedSampleTable table = Parse(
            source, new AvifSecurityLimits(), AvifResultCode.Success);
        ParsedMovieTrack parsedTrack = new ParsedMovieTrack();
        parsedTrack.SampleTable = table;
        AvifTrack track = new AvifTrack(parsedTrack, 1000);
        AvifSample? first = track.GetSample(0);
        AvifSample? second = track.GetSample(1);
        AvifSample? third = track.GetSample(2);
        AvifSampleMetadataGroup? unknownMetadata = (first ?? throw new InvalidOperationException("Invalid internal state.")).GetMetadataGroup(0);
        AvifSampleMetadataGroup? typedMetadata = (third ?? throw new InvalidOperationException("Invalid internal state.")).GetMetadataGroup(0);
        AvifSampleMetadataGroup? secondTypedMetadata = third.GetMetadataGroup(1);

        Assert.Multiple(() =>
        {
            Assert.That(first.IsSwitchFrame(), Is.True);
            Assert.That(first.GetMetadataGroupCount(), Is.EqualTo(1));
            Assert.That((unknownMetadata ?? throw new InvalidOperationException("Invalid internal state.")).HasParameter(), Is.False);
            Assert.That(unknownMetadata.GetMetadataType(), Is.EqualTo(-1));
            Assert.That((second ?? throw new InvalidOperationException("Invalid internal state.")).IsForwardKeyFrame(), Is.True);
            Assert.That(second.GetForwardKeyFrameDistance(), Is.Zero);
            Assert.That(second.GetRecoverySampleIndex(), Is.EqualTo(2));
            Assert.That(third.HasMultipleFrames(), Is.True);
            Assert.That(third.GetMetadataGroupCount(), Is.EqualTo(2));
            Assert.That((typedMetadata ?? throw new InvalidOperationException("Invalid internal state.")).HasParameter(), Is.True);
            Assert.That(typedMetadata.GetParameter(), Is.EqualTo(0x04112233));
            Assert.That(typedMetadata.GetMetadataType(), Is.EqualTo(4));
            Assert.That(typedMetadata.GetMetadataSpecificParameters(), Is.EqualTo(0x112233));
            Assert.That((secondTypedMetadata ?? throw new InvalidOperationException("Invalid internal state.")).GetMetadataType(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ParsedTableDrivesTimedSequenceQueriesAndIndexedInterFrameDecode()
    {
        byte[] bytes = CreateSampleTable(false, false, 0, 1);
        ParsedSampleTable table = Parse(
            bytes, new AvifSecurityLimits(), AvifResultCode.Success);
        ParsedMovieTrack parsedTrack = new ParsedMovieTrack();
        parsedTrack.TrackId = 1;
        parsedTrack.Flags = 3;
        parsedTrack.Duration = 400;
        parsedTrack.Width = 640;
        parsedTrack.Height = 360;
        parsedTrack.MediaTimescale = 1000;
        parsedTrack.MediaDuration = 400;
        parsedTrack.HandlerType = "pict";
        parsedTrack.SampleTable = table;
        ParsedMovie parsedMovie = new ParsedMovie();
        parsedMovie.Timescale = 1000;
        parsedMovie.Duration = 400;
        parsedMovie.NextTrackId = 2;
        parsedMovie.Tracks.Add(parsedTrack);
        AvifMovie movie = new AvifMovie(parsedMovie);
        AvifImageSource source = new AvifImageSource(movie, movie.FindTrack(1));
        AvifSequence? sequence = source.CreateSequence();

        Assert.That((sequence ?? throw new InvalidOperationException("Invalid internal state.")).DecodeFrame(1), Is.EqualTo(AvifResultCode.Success));
        Assert.Multiple(() =>
        {
            Assert.That((source.GetFrameTiming(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetDuration(), Is.EqualTo(100));
            Assert.That((source.GetFrameTiming(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetPresentationTime(), Is.EqualTo(100));
            Assert.That((source.GetFrameTiming(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetPresentationTime(), Is.EqualTo(200));
            Assert.That((source.GetFrameTiming(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetDuration(), Is.EqualTo(200));
            Assert.That(source.IsFrameKeyFrame(0), Is.True);
            Assert.That(source.IsFrameKeyFrame(1), Is.False);
            Assert.That(source.IsFrameKeyFrame(2), Is.True);
            Assert.That(source.GetNearestKeyFrameIndex(1), Is.Zero);
            Assert.That(sequence.GetLastDecodeStartIndex(), Is.Zero);
            Assert.That(sequence.GetLastDecodeFrameCount(), Is.EqualTo(2));
            Assert.That(((source.GetSequenceTrack() ?? throw new InvalidOperationException("Invalid internal state.")).GetSample(2) ?? throw new InvalidOperationException("Invalid internal state.")).HasMultipleFrames(), Is.True);
            Assert.That(((source.GetSequenceTrack() ?? throw new InvalidOperationException("Invalid internal state.")).GetSample(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetMetadataGroupCount(),
                Is.EqualTo(2));
            Assert.That((((source.GetSequenceTrack() ?? throw new InvalidOperationException("Invalid internal state.")).GetSample(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetMetadataGroup(0) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetMetadataType(), Is.EqualTo(4));
        });

        Assert.That(sequence.DecodeFrame(2), Is.EqualTo(AvifResultCode.Success));
        Assert.Multiple(() =>
        {
            Assert.That(sequence.GetLastDecodeStartIndex(), Is.EqualTo(2));
            Assert.That(sequence.GetLastDecodeFrameCount(), Is.EqualTo(1));
        });
    }

    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    public void ParserRejectsMalformedAv1SampleGroups(int groupScenario)
    {
        byte[] source = CreateSampleTable(false, false, 0, groupScenario);

        Parse(source, new AvifSecurityLimits(), AvifResultCode.InvalidTrack);
    }

    static ParsedSampleTable Parse(
        byte[] source,
        AvifSecurityLimits limits,
        AvifResultCode expectedCode)
    {
        IsoBoxParseResult boxes = IsoBoxReader.Parse(source, limits, false);
        Assert.That(boxes.Code, Is.EqualTo(AvifResultCode.Success));
        IsoBox? sampleTable = FindBox(boxes.Boxes, "stbl");
        ParsedSampleTable table = new ParsedSampleTable();

        AvifResultCode code = SampleTableBoxParser.Parse(
            source,
            sampleTable,
            boxes.Boxes,
            boxes.Resources,
            boxes.Diagnostics,
            table);

        Assert.That(code, Is.EqualTo(expectedCode));
        return table;
    }

    static byte[] CreateSampleTable(
        bool use64BitOffsets,
        bool constantSize,
        int fault,
        int groupScenario = 0,
        string auxiliaryType = "",
        int auxiliaryTypeCount = 1)
    {
        uint[] sizes = constantSize
            ? new uint[] { 3, 3, 3 }
            : fault == 10
                ? new uint[] { 3, 0, 4 }
                : new uint[] { 3, 2, 4 };
        IsoBoxWriter writer = new IsoBoxWriter(1024, 1024 * 1024);
        writer.BeginBox("mdat");
        int sampleIndex = 0;
        while (sampleIndex < sizes.Length)
        {
            writer.Write(new byte[sizes[sampleIndex]]);
            sampleIndex++;
        }
        writer.EndBox();
        writer.BeginBox("stbl");
        WriteSampleDescription(
            writer, fault == 1, auxiliaryType, auxiliaryTypeCount);
        WriteTimeToSample(writer, fault == 2, fault == 9);
        WriteSampleToChunk(writer, fault == 7);
        WriteSampleSizes(writer, sizes, constantSize);
        ulong secondChunkOffset = 8 + sizes[0] + sizes[1];
        WriteChunkOffsets(
            writer,
            use64BitOffsets,
            fault == 3 ? 1000000UL : fault == 8 ? 0UL : 8,
            secondChunkOffset);
        if (fault == 6)
        {
            WriteChunkOffsets(writer, true, 8, secondChunkOffset);
        }
        WriteSyncSamples(writer, fault == 4);
        WriteDependencies(writer, fault == 5);
        WriteAv1SampleGroups(writer, groupScenario);
        writer.EndBox();
        IsoBoxWriteResult result = writer.Finish();
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.Data;
    }

    static void WriteSampleDescription(
        IsoBoxWriter writer,
        bool invalidConfiguration,
        string auxiliaryType,
        int auxiliaryTypeCount)
    {
        writer.BeginFullBox("stsd", 0, 0);
        WriteUInt32(writer, 1);
        writer.BeginBox("av01");
        byte[] fields = new byte[78];
        BinaryPrimitives.WriteUInt16BigEndian(fields.AsSpan(6, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(fields.AsSpan(24, 2), 640);
        BinaryPrimitives.WriteUInt16BigEndian(fields.AsSpan(26, 2), 360);
        BinaryPrimitives.WriteUInt32BigEndian(fields.AsSpan(28, 4), 72U << 16);
        BinaryPrimitives.WriteUInt32BigEndian(fields.AsSpan(32, 4), 72U << 16);
        BinaryPrimitives.WriteUInt16BigEndian(fields.AsSpan(40, 2), 1);
        fields[42] = 3;
        fields[43] = (byte)'A';
        fields[44] = (byte)'V';
        fields[45] = (byte)'1';
        BinaryPrimitives.WriteUInt16BigEndian(fields.AsSpan(74, 2), 24);
        BinaryPrimitives.WriteUInt16BigEndian(fields.AsSpan(76, 2), ushort.MaxValue);
        writer.Write(fields);
        writer.BeginBox("av1C");
        writer.Write(new byte[]
        {
            invalidConfiguration ? (byte)1 : (byte)129,
            0,
            14,
            0
        });
        writer.EndBox();
        int auxiliaryTypeIndex = 0;
        while (auxiliaryTypeIndex < auxiliaryTypeCount
            && auxiliaryType.Length != 0)
        {
            writer.BeginFullBox("auxi", 0, 0);
            writer.Write(System.Text.Encoding.UTF8.GetBytes(auxiliaryType));
            writer.Write(new byte[] { 0 });
            writer.EndBox();
            auxiliaryTypeIndex++;
        }
        writer.EndBox();
        writer.EndBox();
    }

    static void WriteTimeToSample(
        IsoBoxWriter writer,
        bool wrongCount,
        bool zeroDuration)
    {
        writer.BeginFullBox("stts", 0, 0);
        WriteUInt32(writer, 2);
        WriteUInt32(writer, wrongCount ? 1U : 2U);
        WriteUInt32(writer, zeroDuration ? 0U : 100U);
        WriteUInt32(writer, 1);
        WriteUInt32(writer, 200);
        writer.EndBox();
    }

    static void WriteSampleToChunk(IsoBoxWriter writer, bool badDescription)
    {
        writer.BeginFullBox("stsc", 0, 0);
        WriteUInt32(writer, 2);
        WriteUInt32(writer, 1);
        WriteUInt32(writer, 2);
        WriteUInt32(writer, 1);
        WriteUInt32(writer, 2);
        WriteUInt32(writer, 1);
        WriteUInt32(writer, badDescription ? 2U : 1U);
        writer.EndBox();
    }

    static void WriteSampleSizes(
        IsoBoxWriter writer,
        uint[] sizes,
        bool constantSize)
    {
        writer.BeginFullBox("stsz", 0, 0);
        WriteUInt32(writer, constantSize ? sizes[0] : 0);
        WriteUInt32(writer, (uint)sizes.Length);
        if (!constantSize)
        {
            int index = 0;
            while (index < sizes.Length)
            {
                WriteUInt32(writer, sizes[index]);
                index++;
            }
        }
        writer.EndBox();
    }

    static void WriteChunkOffsets(
        IsoBoxWriter writer,
        bool use64BitOffsets,
        ulong first,
        ulong second)
    {
        writer.BeginFullBox(use64BitOffsets ? "co64" : "stco", 0, 0);
        WriteUInt32(writer, 2);
        if (use64BitOffsets)
        {
            WriteUInt64(writer, first);
            WriteUInt64(writer, second);
        }
        else
        {
            WriteUInt32(writer, (uint)first);
            WriteUInt32(writer, (uint)second);
        }
        writer.EndBox();
    }

    static void WriteSyncSamples(IsoBoxWriter writer, bool duplicate)
    {
        writer.BeginFullBox("stss", 0, 0);
        WriteUInt32(writer, 2);
        WriteUInt32(writer, 1);
        WriteUInt32(writer, duplicate ? 1U : 3U);
        writer.EndBox();
    }

    static void WriteDependencies(IsoBoxWriter writer, bool reservedValue)
    {
        writer.BeginFullBox("sdtp", 0, 0);
        writer.Write(new byte[]
        {
            32,
            reservedValue ? (byte)48 : (byte)16,
            36
        });
        writer.EndBox();
    }

    static void WriteAv1SampleGroups(IsoBoxWriter writer, int scenario)
    {
        if (scenario == 0)
        {
            return;
        }
        if (scenario == 1)
        {
            WriteGroupDescription(writer, "av1f", 0, false, 1);
            WriteGroupDescription(writer, "av1m", 0, false, 1);
            WriteGroupDescription(writer, "av1s", 0, false, 0);
            WriteGroupDescription(writer, "av1M", 0, false, 1);
            WriteSampleGroupMapping(
                writer, "av1f", false, 0,
                new uint[] { 1, 1, 1 }, new uint[] { 0, 1, 0 });
            WriteSampleGroupMapping(
                writer, "av1m", false, 0,
                new uint[] { 2, 1 }, new uint[] { 0, 1 });
            WriteSampleGroupMapping(
                writer, "av1s", false, 0,
                new uint[] { 1, 2 }, new uint[] { 1, 0 });
            WriteSampleGroupMapping(
                writer, "av1M", true, 0x04112233,
                new uint[] { 2, 1 }, new uint[] { 0, 1 });
            WriteSampleGroupMapping(
                writer, "av1M", false, 0,
                new uint[] { 1, 2 }, new uint[] { 1, 0 });
            WriteSampleGroupMapping(
                writer, "av1M", true, 0x01000000,
                new uint[] { 2, 1 }, new uint[] { 0, 1 });
        }
        else if (scenario == 2)
        {
            WriteGroupDescription(writer, "av1f", 0, false, 1);
            WriteSampleGroupMapping(
                writer, "av1f", false, 0,
                new uint[] { 2, 1 }, new uint[] { 0, 1 });
        }
        else if (scenario == 3)
        {
            WriteGroupDescription(writer, "av1f", 0, true, 1);
        }
        else if (scenario == 4)
        {
            WriteGroupDescription(writer, "av1m", 0, false, 1);
            WriteSampleGroupMapping(
                writer, "av1m", false, 0,
                new uint[] { 2 }, new uint[] { 1 });
        }
        else if (scenario == 5)
        {
            WriteGroupDescription(writer, "av1m", 0, false, 1);
            WriteSampleGroupMapping(
                writer, "av1m", false, 0,
                new uint[] { 3 }, new uint[] { 2 });
        }
        else if (scenario == 6)
        {
            WriteGroupDescription(writer, "av1s", 0, false, 1);
            WriteSampleGroupMapping(
                writer, "av1s", true, 0,
                new uint[] { 3 }, new uint[] { 1 });
        }
        else if (scenario == 7)
        {
            WriteGroupDescription(writer, "av1M", 0, false, 1);
            WriteSampleGroupMapping(
                writer, "av1M", true, 0x01000000,
                new uint[] { 3 }, new uint[] { 1 });
            WriteSampleGroupMapping(
                writer, "av1M", true, 0x01000000,
                new uint[] { 3 }, new uint[] { 1 });
        }
        else if (scenario == 8)
        {
            WriteSampleGroupMapping(
                writer, "av1m", false, 0,
                new uint[] { 3 }, new uint[] { 1 });
        }
        else
        {
            WriteGroupDescription(writer, "av1m", 0, false, 1);
            WriteGroupDescription(writer, "av1m", 0, false, 1);
        }
    }

    static void WriteGroupDescription(
        IsoBoxWriter writer,
        string groupingType,
        int forwardDistance,
        bool invalidLength,
        int version)
    {
        writer.BeginFullBox("sgpd", version, 0);
        WriteFourCc(writer, groupingType);
        if (version >= 1)
        {
            WriteUInt32(writer, groupingType == "av1f" && !invalidLength ? 1U : 0U);
        }
        WriteUInt32(writer, 1);
        if (version >= 1 && (groupingType != "av1f" || invalidLength))
        {
            WriteUInt32(writer, 0);
        }
        if (groupingType == "av1f" && !invalidLength)
        {
            writer.Write(new byte[] { (byte)forwardDistance });
        }
        writer.EndBox();
    }

    static void WriteSampleGroupMapping(
        IsoBoxWriter writer,
        string groupingType,
        bool hasParameter,
        uint parameter,
        uint[] sampleCounts,
        uint[] descriptionIndices)
    {
        writer.BeginFullBox("sbgp", hasParameter ? 1 : 0, 0);
        WriteFourCc(writer, groupingType);
        if (hasParameter)
        {
            WriteUInt32(writer, parameter);
        }
        WriteUInt32(writer, (uint)sampleCounts.Length);
        int index = 0;
        while (index < sampleCounts.Length)
        {
            WriteUInt32(writer, sampleCounts[index]);
            WriteUInt32(writer, descriptionIndices[index]);
            index++;
        }
        writer.EndBox();
    }

    static void WriteFourCc(IsoBoxWriter writer, string value)
    {
        writer.Write(new byte[]
        {
            (byte)value[0],
            (byte)value[1],
            (byte)value[2],
            (byte)value[3]
        });
    }

    static void WriteUInt32(IsoBoxWriter writer, uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }

    static void WriteUInt64(IsoBoxWriter writer, ulong value)
    {
        byte[] bytes = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        writer.Write(bytes);
    }

    static IsoBox? FindBox(System.Collections.Generic.List<IsoBox> boxes, string type)
    {
        int index = 0;
        while (index < boxes.Count)
        {
            if (boxes[index].Type == type)
            {
                return boxes[index];
            }
            IsoBox? child = FindBox(boxes[index].Children, type);
            if (child != null)
            {
                return child;
            }
            index++;
        }
        return null;
    }
}
