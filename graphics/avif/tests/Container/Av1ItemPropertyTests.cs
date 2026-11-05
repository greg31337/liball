using System.Buffers.Binary;

namespace avif.Tests;

public class Av1ItemPropertyTests
{
    [Test]
    public void ParserExposesAv1ConfigurationSpatialExtentsAndClassicPixelInformation()
    {
        byte[] source = BuildDocument(writer =>
        {
            WriteAv1Configuration(writer, CreateConfiguration(0, 4, 0, true, false, false, true, true, 2, true, 3, System.Array.Empty<byte>()));
            WriteSpatialExtents(writer, 1920, 1080, 0, 0, System.Array.Empty<byte>());
            WriteClassicPixelInformation(writer, new byte[] { 10, 10, 10 });
        }, new int[] { 1, 2, 3 });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedAv1Configuration? configuration = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetAv1Configuration();
        ParsedImageSpatialExtents? extents = (result.Meta.GetProperty(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetSpatialExtents();
        ParsedPixelInformation? pixels = (result.Meta.GetProperty(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetPixelInformation();
        source.AsSpan().Clear();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((configuration ?? throw new InvalidOperationException("Invalid internal state.")).GetSequenceProfile(), Is.Zero);
            Assert.That(configuration.GetSequenceLevelIndex(), Is.EqualTo(4));
            Assert.That(configuration.GetBitDepth(), Is.EqualTo(10));
            Assert.That(configuration.GetChannelCount(), Is.EqualTo(3));
            Assert.That(configuration.GetChromaSamplePosition(), Is.EqualTo(2));
            Assert.That(configuration.HasInitialPresentationDelay(), Is.True);
            Assert.That(configuration.GetInitialPresentationDelayMinusOne(), Is.EqualTo(3));
            Assert.That((extents ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(1920));
            Assert.That(extents.GetHeight(), Is.EqualTo(1080));
            Assert.That((pixels ?? throw new InvalidOperationException("Invalid internal state.")).IsExtended(), Is.False);
            Assert.That(pixels.GetChannelCount(), Is.EqualTo(3));
            Assert.That((pixels.GetChannel(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetBitDepth(), Is.EqualTo(10));
            Assert.That(pixels.GetChannel(-1), Is.Null);
        });
    }

    [Test]
    public void ExtendedPixelInformationParsesChannelMetadataSubsamplingAndLabels()
    {
        byte[] source = BuildDocument(writer =>
        {
            writer.BeginFullBox("pixi", 0, 1);
            writer.Write(new byte[]
            {
                3,
                10, 10, 10,
                1, (byte)'Y', 0,
                2, 0x22,
                3, 0x24, (byte)'C', (byte)'r', 0
            });
            writer.EndBox();
        }, new int[] { 1 });

        MetaParseResult result = MetaBoxParser.Parse(source, CreateLimits());
        ParsedPixelInformation? pixels = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetPixelInformation();
        ParsedPixelChannel? luma = (pixels ?? throw new InvalidOperationException("Invalid internal state.")).GetChannel(0);
        ParsedPixelChannel? chroma = pixels.GetChannel(1);
        ParsedPixelChannel? labeledChroma = pixels.GetChannel(2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(pixels.IsExtended(), Is.True);
            Assert.That((luma ?? throw new InvalidOperationException("Invalid internal state.")).IsSubsampled(), Is.False);
            Assert.That(luma.GetSubsamplingType(), Is.EqualTo(-1));
            Assert.That(luma.GetLabel(), Is.EqualTo("Y"));
            Assert.That((chroma ?? throw new InvalidOperationException("Invalid internal state.")).GetChannelIdentifier(), Is.Zero);
            Assert.That(chroma.GetComponentFormat(), Is.Zero);
            Assert.That(chroma.IsSubsampled(), Is.True);
            Assert.That(chroma.GetSubsamplingType(), Is.EqualTo(2));
            Assert.That(chroma.GetSubsamplingLocation(), Is.EqualTo(2));
            Assert.That((labeledChroma ?? throw new InvalidOperationException("Invalid internal state.")).GetSubsamplingLocation(), Is.EqualTo(4));
            Assert.That(labeledChroma.GetLabel(), Is.EqualTo("Cr"));
        });
    }

    [Test]
    public void SpatialExtentsEnforceFullBoxSyntaxDimensionsAndLimits()
    {
        MetaParseResult version = ParseSingleProperty(writer =>
            WriteSpatialExtents(writer, 64, 32, 1, 0, System.Array.Empty<byte>()));
        MetaParseResult flags = ParseSingleProperty(writer =>
            WriteSpatialExtents(writer, 64, 32, 0, 1, System.Array.Empty<byte>()));
        MetaParseResult zero = ParseSingleProperty(writer =>
            WriteSpatialExtents(writer, 0, 32, 0, 0, System.Array.Empty<byte>()));
        MetaParseResult trailing = ParseSingleProperty(writer =>
            WriteSpatialExtents(writer, 64, 32, 0, 0, new byte[] { 1 }));
        byte[] limitedSource = BuildDocument(writer =>
        {
            WriteSpatialExtents(writer, 101, 32, 0, 0, System.Array.Empty<byte>());
        }, new int[] { 1 });
        AvifSecurityLimits limits = CreateLimits();
        limits.MaxWidth = 100;
        MetaParseResult limited = MetaBoxParser.Parse(limitedSource, limits);

        Assert.Multiple(() =>
        {
            Assert.That(version.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((version.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetSpatialExtents(), Is.Null);
            Assert.That(flags.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(zero.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(trailing.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(limited.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    [Test]
    public void PixelInformationRejectsInvalidFullBoxAndChannelSyntax()
    {
        MetaParseResult version = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("pixi", 1, 0);
            writer.Write(new byte[] { 1, 8 });
            writer.EndBox();
        });
        MetaParseResult flags = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("pixi", 0, 2);
            writer.Write(new byte[] { 1, 8 });
            writer.EndBox();
        });
        MetaParseResult zeroChannels = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("pixi", 0, 0);
            writer.Write(new byte[] { 0 });
            writer.EndBox();
        });
        MetaParseResult zeroDepth = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("pixi", 0, 0);
            writer.Write(new byte[] { 1, 0 });
            writer.EndBox();
        });
        MetaParseResult reserved = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("pixi", 0, 1);
            writer.Write(new byte[] { 1, 8, 0x10 });
            writer.EndBox();
        });
        MetaParseResult badSubsampling = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("pixi", 0, 1);
            writer.Write(new byte[] { 1, 8, 2, 0x50 });
            writer.EndBox();
        });
        MetaParseResult missingLabelEnd = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("pixi", 0, 1);
            writer.Write(new byte[] { 1, 8, 1, (byte)'x' });
            writer.EndBox();
        });
        MetaParseResult duplicateAlpha = ParseSingleProperty(writer =>
        {
            writer.BeginFullBox("pixi", 0, 1);
            writer.Write(new byte[] { 2, 8, 8, 0x20, 0x20 });
            writer.EndBox();
        });

        Assert.Multiple(() =>
        {
            Assert.That(version.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((version.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetPixelInformation(), Is.Null);
            Assert.That(flags.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(zeroChannels.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(zeroDepth.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(reserved.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(badSubsampling.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(missingLabelEnd.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(duplicateAlpha.Code, Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    [Test]
    public void Av1ConfigurationRejectsFixedFieldAndSemanticViolations()
    {
        byte[] marker = CreateConfiguration(0, 4, 0, false, false, false, true, true, 0, false, 0, System.Array.Empty<byte>());
        marker[0] = 1;
        byte[] reserved = CreateConfiguration(0, 4, 0, false, false, false, true, true, 0, false, 0, System.Array.Empty<byte>());
        reserved[3] = 0x20;
        byte[] reservedDelay = CreateConfiguration(0, 4, 0, false, false, false, true, true, 0, false, 0, System.Array.Empty<byte>());
        reservedDelay[3] = 1;
        byte[] twelveWithoutHigh = CreateConfiguration(2, 4, 0, false, true, false, true, false, 0, false, 0, System.Array.Empty<byte>());
        byte[] tierAtLowLevel = CreateConfiguration(0, 4, 1, false, false, false, true, true, 0, false, 0, System.Array.Empty<byte>());
        byte[] reservedLevel = CreateConfiguration(0, 24, 0, false, false, false, true, true, 0, false, 0, System.Array.Empty<byte>());
        byte[] maximumParametersLevel = CreateConfiguration(0, 31, 0, false, false, false, true, true, 0, false, 0, System.Array.Empty<byte>());

        MetaParseResult markerResult = ParseConfiguration(marker);
        MetaParseResult reservedResult = ParseConfiguration(reserved);
        MetaParseResult delayResult = ParseConfiguration(reservedDelay);
        MetaParseResult depthResult = ParseConfiguration(twelveWithoutHigh);
        MetaParseResult tierResult = ParseConfiguration(tierAtLowLevel);
        MetaParseResult reservedLevelResult = ParseConfiguration(reservedLevel);
        MetaParseResult maximumLevelResult = ParseConfiguration(maximumParametersLevel);

        Assert.Multiple(() =>
        {
            Assert.That(markerResult.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(reservedResult.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(delayResult.Code, Is.EqualTo(AvifResultCode.InvalidContainer));
            Assert.That(depthResult.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(tierResult.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(reservedLevelResult.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(maximumLevelResult.Code, Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void EmbeddedReducedSequenceHeaderIsParsedAndReconciledWithItemData()
    {
        byte[] sequenceObu = BuildReducedSequenceObu(4, 64, 32);
        byte[] configuration = CreateConfiguration(
            0, 4, 0, true, false, false, true, true, 2, false, 0, sequenceObu);
        MetaParseResult result = ParseConfiguration(configuration);
        ParsedAv1Configuration? parsed = ((result.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetAv1Configuration();
        ParsedAv1SequenceHeader? header = (parsed ?? throw new InvalidOperationException("Invalid internal state.")).GetConfigSequenceHeader();
        byte[] unsizedItemObu = BuildObu(1, sequenceObu[2..], false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.HasConfigSequenceHeader(), Is.True);
            Assert.That((header ?? throw new InvalidOperationException("Invalid internal state.")).HasReducedStillPictureHeader(), Is.True);
            Assert.That(header.IsStillPicture(), Is.True);
            Assert.That(header.GetMaximumWidth(), Is.EqualTo(64));
            Assert.That(header.GetMaximumHeight(), Is.EqualTo(32));
            Assert.That(header.GetBitDepth(), Is.EqualTo(10));
            Assert.That(header.GetChromaSamplePosition(), Is.EqualTo(2));
            Assert.That(parsed.ReconcileSequenceHeader(sequenceObu), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.ReconcileSequenceHeader(unsizedItemObu), Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void GeneralSequenceHeaderOperatingPointFieldsAreReconciled()
    {
        byte[] sequenceObu = BuildGeneralSequenceObu();
        byte[] configuration = CreateConfiguration(
            1, 8, 1, false, false, false, false, false, 0, false, 0, sequenceObu);
        MetaParseResult result = ParseConfiguration(configuration);
        ParsedAv1SequenceHeader? header = (((result.Meta ?? throw new InvalidOperationException("Invalid internal state."))
            .GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state."))
            .GetAv1Configuration() ?? throw new InvalidOperationException("Invalid internal state."))
            .GetConfigSequenceHeader();

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((header ?? throw new InvalidOperationException("Invalid internal state.")).HasReducedStillPictureHeader(), Is.False);
            Assert.That(header.GetSequenceProfile(), Is.EqualTo(1));
            Assert.That(header.GetSequenceLevelIndex(), Is.EqualTo(8));
            Assert.That(header.GetSequenceTier(), Is.EqualTo(1));
            Assert.That(header.GetMaximumWidth(), Is.EqualTo(128));
            Assert.That(header.GetMaximumHeight(), Is.EqualTo(64));
            Assert.That(header.GetColorPrimaries(), Is.EqualTo(1));
            Assert.That(header.GetTransferCharacteristics(), Is.EqualTo(13));
            Assert.That(header.GetMatrixCoefficients(), Is.Zero);
            Assert.That(header.IsFullRange(), Is.True);
        });
    }

    [Test]
    public void InBandSequenceHeaderReconcilesContainerColorWithoutConfigObus()
    {
        byte[] sequence = BuildGeneralSequenceObu();
        byte[] configurationData = CreateConfiguration(
            1,
            8,
            1,
            false,
            false,
            false,
            false,
            false,
            0,
            false,
            0,
            System.Array.Empty<byte>());
        ParsedAv1Configuration? configuration = ((ParseConfiguration(configurationData)
            .Meta ?? throw new InvalidOperationException("Invalid internal state."))
            .GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state."))
            .GetAv1Configuration();
        Av1SequenceHeaderParseResult reconciled = (configuration ?? throw new InvalidOperationException("Invalid internal state."))
            .ReconcileSequenceHeaderResult(sequence);
        ParsedColourInformation matching = new ParsedColourInformation(
            "nclx", true, true, 1, 13, 0, true, System.Array.Empty<byte>());
        ParsedColourInformation conflicting = new ParsedColourInformation(
            "nclx", true, true, 9, 13, 0, true, System.Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            Assert.That(reconciled.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reconciled.Header, Is.Not.Null);
            Assert.That(configuration.ReconcileColourInformationWithSequenceHeader(
                    matching,
                    reconciled.Header),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(configuration.ReconcileColourInformationWithSequenceHeader(
                    conflicting,
                    reconciled.Header),
                Is.EqualTo(AvifResultCode.NonConformingData));
        });
    }

    [Test]
    public void ConfigurationObusRequireSizedFirstAndUniqueSequenceHeader()
    {
        byte[] sequence = BuildReducedSequenceObu(4, 64, 32);
        byte[] payload = sequence[2..];
        byte[] unsizedSequence = BuildObu(1, payload, false);
        byte[] metadata = BuildObu(5, System.Array.Empty<byte>(), true);
        byte[] notFirst = Combine(metadata, sequence);
        byte[] duplicate = Combine(sequence, sequence);
        byte[] paddedSize = new byte[sequence.Length + 1];
        paddedSize[0] = sequence[0];
        paddedSize[1] = (byte)(sequence[1] | 128);
        paddedSize[2] = 0;
        sequence.AsSpan(2).CopyTo(paddedSize.AsSpan(3));

        MetaParseResult unsized = ParseConfiguration(CreateConfiguration(
            0, 4, 0, true, false, false, true, true, 2, false, 0, unsizedSequence));
        MetaParseResult ordered = ParseConfiguration(CreateConfiguration(
            0, 4, 0, true, false, false, true, true, 2, false, 0, notFirst));
        MetaParseResult repeated = ParseConfiguration(CreateConfiguration(
            0, 4, 0, true, false, false, true, true, 2, false, 0, duplicate));
        MetaParseResult padded = ParseConfiguration(CreateConfiguration(
            0, 4, 0, true, false, false, true, true, 2, false, 0, paddedSize));

        Assert.Multiple(() =>
        {
            Assert.That(unsized.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(ordered.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(repeated.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(padded.Code, Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void ReconciliationRejectsRecordHeaderPayloadAndPixelInformationMismatches()
    {
        byte[] sequence = BuildReducedSequenceObu(4, 64, 32);
        MetaParseResult recordMismatch = ParseConfiguration(CreateConfiguration(
            0, 5, 0, true, false, false, true, true, 2, false, 0, sequence));

        MetaParseResult valid = ParseConfiguration(CreateConfiguration(
            0, 4, 0, true, false, false, true, true, 2, false, 0, sequence));
        ParsedAv1Configuration? configuration = ((valid.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetAv1Configuration();
        byte[] differentDimensions = BuildReducedSequenceObu(4, 63, 32);
        byte[] differentLevel = BuildReducedSequenceObu(5, 64, 32);
        byte[] duplicateSequence = Combine(sequence, sequence);

        byte[] pixelMismatchData = BuildDocument(writer =>
        {
            WriteAv1Configuration(writer, CreateConfiguration(
                0, 4, 0, true, false, false, true, true, 2, false, 0, System.Array.Empty<byte>()));
            WriteClassicPixelInformation(writer, new byte[] { 8, 8, 8 });
        }, new int[] { 1, 2 });
        MetaParseResult pixelMismatch = MetaBoxParser.Parse(pixelMismatchData, CreateLimits());
        byte[] limitedData = BuildDocument(writer =>
        {
            WriteAv1Configuration(writer, CreateConfiguration(
                0, 4, 0, true, false, false, true, true, 2, false, 0, sequence));
        }, new int[] { 1 });
        AvifSecurityLimits limitedResources = CreateLimits();
        limitedResources.MaxTemporaryBytes = 1;
        MetaParseResult limited = MetaBoxParser.Parse(limitedData, limitedResources);

        Assert.Multiple(() =>
        {
            Assert.That(recordMismatch.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That((configuration ?? throw new InvalidOperationException("Invalid internal state.")).ReconcileSequenceHeader(differentDimensions), Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(configuration.ReconcileSequenceHeader(differentLevel), Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(configuration.ReconcileSequenceHeader(duplicateSequence), Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(pixelMismatch.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(limited.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    private static MetaParseResult ParseSingleProperty(Action<IsoBoxWriter> writeProperty)
    {
        byte[] source = BuildDocument(writeProperty, new int[] { 1 });
        return MetaBoxParser.Parse(source, CreateLimits());
    }

    private static MetaParseResult ParseConfiguration(byte[] configuration)
    {
        return ParseSingleProperty(writer => WriteAv1Configuration(writer, configuration));
    }

    private static byte[] BuildDocument(
        Action<IsoBoxWriter> writeProperties,
        int[] propertyIndices)
    {
        IsoBoxWriter writer = new IsoBoxWriter(0, 65536);
        writer.BeginFullBox("meta", 0, 0);
        WriteHandler(writer);
        WriteItemInfo(writer);
        writer.BeginBox("iprp");
        writer.BeginBox("ipco");
        writeProperties(writer);
        writer.EndBox();
        writer.BeginFullBox("ipma", 0, 0);
        WriteUInt32(writer, 1);
        WriteUInt16(writer, 1);
        writer.Write(new byte[] { (byte)propertyIndices.Length });
        int index = 0;
        while (index < propertyIndices.Length)
        {
            writer.Write(new byte[] { (byte)propertyIndices[index] });
            index++;
        }

        writer.EndBox();
        writer.EndBox();
        writer.EndBox();
        IsoBoxWriteResult result = writer.Finish();
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        return result.Data;
    }

    private static void WriteHandler(IsoBoxWriter writer)
    {
        writer.BeginFullBox("hdlr", 0, 0);
        byte[] fields = new byte[21];
        WriteFourCc(fields.AsSpan(4, 4), "pict");
        writer.Write(fields);
        writer.EndBox();
    }

    private static void WriteItemInfo(IsoBoxWriter writer)
    {
        writer.BeginFullBox("iinf", 1, 0);
        WriteUInt32(writer, 1);
        writer.BeginFullBox("infe", 3, 0);
        byte[] entry = new byte[11];
        BinaryPrimitives.WriteUInt32BigEndian(entry.AsSpan(0, 4), 1);
        WriteFourCc(entry.AsSpan(6, 4), "av01");
        writer.Write(entry);
        writer.EndBox();
        writer.EndBox();
    }

    private static void WriteAv1Configuration(IsoBoxWriter writer, byte[] configuration)
    {
        writer.BeginBox("av1C");
        writer.Write(configuration);
        writer.EndBox();
    }

    private static void WriteSpatialExtents(
        IsoBoxWriter writer,
        uint width,
        uint height,
        int version,
        int flags,
        byte[] trailing)
    {
        writer.BeginFullBox("ispe", version, flags);
        WriteUInt32(writer, width);
        WriteUInt32(writer, height);
        writer.Write(trailing);
        writer.EndBox();
    }

    private static void WriteClassicPixelInformation(
        IsoBoxWriter writer,
        byte[] bitDepths)
    {
        writer.BeginFullBox("pixi", 0, 0);
        writer.Write(new byte[] { (byte)bitDepths.Length });
        writer.Write(bitDepths);
        writer.EndBox();
    }

    private static byte[] CreateConfiguration(
        int profile,
        int level,
        int tier,
        bool highBitDepth,
        bool twelveBit,
        bool monochrome,
        bool subsamplingX,
        bool subsamplingY,
        int chromaSamplePosition,
        bool delayPresent,
        int delayMinusOne,
        byte[] configObus)
    {
        byte[] configuration = new byte[4 + configObus.Length];
        configuration[0] = 0x81;
        configuration[1] = (byte)(profile << 5 | level);
        configuration[2] = (byte)(tier << 7
            | (highBitDepth ? 64 : 0)
            | (twelveBit ? 32 : 0)
            | (monochrome ? 16 : 0)
            | (subsamplingX ? 8 : 0)
            | (subsamplingY ? 4 : 0)
            | chromaSamplePosition);
        configuration[3] = (byte)((delayPresent ? 16 : 0) | delayMinusOne);
        configObus.CopyTo(configuration, 4);
        return configuration;
    }

    private static byte[] BuildReducedSequenceObu(
        int level,
        int width,
        int height)
    {
        byte[] payload = new byte[32];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, (uint)level, 5);
        int widthBits = GetBitCount(width - 1);
        int heightBits = GetBitCount(height - 1);
        writer.WriteBits(payload, (uint)(widthBits - 1), 4);
        writer.WriteBits(payload, (uint)(heightBits - 1), 4);
        writer.WriteBits(payload, (uint)(width - 1), widthBits);
        writer.WriteBits(payload, (uint)(height - 1), heightBits);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 2, 2);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteTrailingBits(payload);
        Assert.That(writer.Code, Is.EqualTo(AvifResultCode.Success));
        byte[] sizedPayload = payload[..(int)(writer.BitPosition / 8)];
        return BuildObu(1, sizedPayload, true);
    }

    private static byte[] BuildGeneralSequenceObu()
    {
        byte[] payload = new byte[64];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBits(payload, 1, 3);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBits(payload, 0, 5);
        writer.WriteBits(payload, 0, 12);
        writer.WriteBits(payload, 8, 5);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 6, 4);
        writer.WriteBits(payload, 5, 4);
        writer.WriteBits(payload, 127, 7);
        writer.WriteBits(payload, 63, 6);
        writer.WriteBit(payload, 0);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBits(payload, 0, 4);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 1, 8);
        writer.WriteBits(payload, 13, 8);
        writer.WriteBits(payload, 0, 8);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteTrailingBits(payload);
        Assert.That(writer.Code, Is.EqualTo(AvifResultCode.Success));
        byte[] sizedPayload = payload[..(int)(writer.BitPosition / 8)];
        return BuildObu(1, sizedPayload, true);
    }

    private static byte[] BuildObu(int type, byte[] payload, bool hasSize)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add((byte)(type << 3 | (hasSize ? 2 : 0)));
        if (hasSize)
        {
            Assert.That(payload.Length, Is.LessThan(128));
            bytes.Add((byte)payload.Length);
        }

        bytes.AddRange(payload);
        return bytes.ToArray();
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }

    private static int GetBitCount(int value)
    {
        int count = 1;
        int remaining = value;
        while (remaining > 1)
        {
            count++;
            remaining >>= 1;
        }

        return count;
    }

    private static void WriteUInt16(IsoBoxWriter writer, ushort value)
    {
        byte[] bytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static void WriteUInt32(IsoBoxWriter writer, uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        writer.Write(bytes);
    }

    private static void WriteFourCc(Span<byte> destination, string value)
    {
        destination[0] = (byte)value[0];
        destination[1] = (byte)value[1];
        destination[2] = (byte)value[2];
        destination[3] = (byte)value[3];
    }

    private static AvifSecurityLimits CreateLimits()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSourceBytes = 131072;
        limits.MaxBoxDepth = 16;
        limits.MaxBoxCount = 512;
        limits.MaxItemCount = 64;
        limits.MaxPropertyCount = 256;
        limits.MaxReferenceCount = 256;
        limits.MaxTemporaryBytes = 65536;
        return limits;
    }
}
