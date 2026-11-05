namespace avif.Tests;

public class Av1WriterTests
{
    [Test]
    public void SymbolEncoderRoundTripsAdaptiveSymbolsLiteralsAndNsValues()
    {
        int[] symbols =
        [
            1, 0, 2, 2, 0, 2, 1, 0, 3, 0, 2,
            3, 2, 3, 2, 0, 3, 3, 3, 0, 0, 3,
            1, 2, 3, 1, 1, 3, 3, 1, 2, 0, 3
        ];
        ushort[] encoderCdf = [4096, 11264, 19328, 32768, 0];
        Av1SymbolEncoder encoder = new Av1SymbolEncoder(false);
        int index = 0;
        while (index < symbols.Length)
        {
            encoder.WriteSymbol(encoderCdf, symbols[index]);
            index++;
        }

        Assert.That(encoder.Exit(), Is.EqualTo(AvifResultCode.Success));
        byte[] encoded = encoder.GetOutput();
        ushort[] decoderCdf = [4096, 11264, 19328, 32768, 0];
        Av1SymbolDecoder decoder = new Av1SymbolDecoder(
            encoded, 0, encoded.Length, false);
        int[] decoded = new int[symbols.Length];
        index = 0;
        while (index < decoded.Length)
        {
            decoded[index] = decoder.ReadSymbol(decoderCdf);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(decoder.Exit(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded, Is.EqualTo(symbols));
            Assert.That(decoderCdf, Is.EqualTo(encoderCdf));
        });

        Av1SymbolEncoder literals = new Av1SymbolEncoder(false);
        literals.WriteLiteral(11, 4);
        literals.WriteNs(4, 5);
        Assert.That(literals.Exit(), Is.EqualTo(AvifResultCode.Success));
        byte[] literalData = literals.GetOutput();
        Av1SymbolDecoder literalDecoder = new Av1SymbolDecoder(
            literalData, 0, literalData.Length, false);
        Assert.Multiple(() =>
        {
            Assert.That(literalDecoder.ReadLiteral(4), Is.EqualTo(11));
            Assert.That(literalDecoder.ReadNs(5), Is.EqualTo(4));
            Assert.That(literalDecoder.Exit(), Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void ObuWriterProducesLowOverheadAndAnnexBStreams()
    {
        Av1WriteResult delimiter = Av1ObuWriter.WriteTemporalDelimiter();
        Av1WriteResult sequence = Av1ObuWriter.WriteObu(
            Av1ObuType.SequenceHeader,
            [0x80],
            false,
            0,
            0);
        Av1WriteResult metadata = Av1ObuWriter.WriteObu(
            Av1ObuType.Metadata,
            [0x80],
            true,
            2,
            1,
            false);
        byte[] lowOverhead = [.. delimiter.Data, .. sequence.Data];
        Av1ObuParseResult lowResult = Av1ObuParser.ParseLowOverhead(
            lowOverhead,
            new AvifSecurityLimits(),
            0);

        Av1WriteResult frame = Av1ObuWriter.WriteLengthDelimitedFrame(
            [delimiter.Data, sequence.Data]);
        Av1WriteResult temporal = Av1ObuWriter.WriteLengthDelimitedTemporalUnit(
            [frame.Data]);
        Av1ObuParseResult annexResult = Av1ObuParser.ParseLengthDelimited(
            temporal.Data,
            new AvifSecurityLimits(),
            0);

        Assert.Multiple(() =>
        {
            Assert.That(delimiter.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(sequence.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(metadata.Data, Is.EqualTo(new byte[] { 0x2C, 0x48, 0x80 }));
            Assert.That(lowResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(lowResult.Obus, Has.Count.EqualTo(2));
            Assert.That(annexResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(annexResult.TemporalUnits[0].FrameUnits, Has.Count.EqualTo(1));
            Assert.That(annexResult.Obus, Has.Count.EqualTo(2));
            Assert.That(Av1ObuWriter.WriteLeb128(128), Is.EqualTo(new byte[] { 0x80, 1 }));
        });
    }

    [Test]
    public void MetadataWriterRoundTripsStructuredAndOpaquePayloads()
    {
        ParsedAv1Metadata content = new ParsedAv1Metadata(
            (ulong)Av1MetadataType.HdrContentLightLevel);
        content.ContentLightLevel = new Av1ContentLightLevelMetadata();
        content.ContentLightLevel.MaximumContentLightLevel = 1000;
        content.ContentLightLevel.MaximumFrameAverageLightLevel = 400;
        Av1WriteResult contentWrite = Av1MetadataWriter.WritePayload(content);
        Av1MetadataParseResult contentRead =
            Av1MetadataParser.ParsePayload(contentWrite.Data);

        ParsedAv1Metadata opaque = new ParsedAv1Metadata(200);
        opaque.OpaquePayload = [9, 8, 7, 0];
        Av1WriteResult opaqueWrite = Av1MetadataWriter.WritePayload(opaque);
        Av1MetadataParseResult opaqueRead =
            Av1MetadataParser.ParsePayload(opaqueWrite.Data);

        Assert.Multiple(() =>
        {
            Assert.That(contentWrite.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(contentRead.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(
                ((contentRead.Metadata ?? throw new InvalidOperationException("Invalid internal state.")).ContentLightLevel ?? throw new InvalidOperationException("Invalid internal state.")).MaximumContentLightLevel,
                Is.EqualTo(1000));
            Assert.That(
                (contentRead.Metadata.ContentLightLevel ?? throw new InvalidOperationException("Invalid internal state.")).MaximumFrameAverageLightLevel,
                Is.EqualTo(400));
            Assert.That(opaqueRead.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((opaqueRead.Metadata ?? throw new InvalidOperationException("Invalid internal state.")).OpaquePayload,
                Is.EqualTo(new byte[] { 9, 8, 7, 0 }));
        });
    }

    [Test]
    public void MetadataWriterSerializesScalabilityItuT35AndTimecodeModels()
    {
        ParsedAv1Metadata scalabilityMetadata = new ParsedAv1Metadata(
            (ulong)Av1MetadataType.Scalability);
        Av1ScalabilityMetadata scalability = new Av1ScalabilityMetadata();
        scalability.ModeIdc = 14;
        scalability.SpatialLayerCount = 2;
        scalability.SpatialLayerDimensionsPresent = true;
        scalability.SpatialLayerDescriptionsPresent = true;
        scalability.TemporalGroupDescriptionsPresent = true;
        scalability.SpatialLayerMaximumWidths = [640, 1280];
        scalability.SpatialLayerMaximumHeights = [360, 720];
        scalability.SpatialLayerReferenceIds = [4, 9];
        Av1TemporalGroupMetadata group = new Av1TemporalGroupMetadata();
        group.TemporalId = 2;
        group.TemporalSwitchingUpPoint = true;
        group.ReferencePictureDifferences = [1, 3];
        scalability.TemporalGroups = [group];
        scalabilityMetadata.Scalability = scalability;
        Av1MetadataParseResult scalabilityRead = Av1MetadataParser.ParsePayload(
            Av1MetadataWriter.WritePayload(scalabilityMetadata).Data);

        ParsedAv1Metadata ituMetadata = new ParsedAv1Metadata(
            (ulong)Av1MetadataType.ItuTT35);
        ituMetadata.ItuT35 = new Av1ItuT35Metadata();
        ituMetadata.ItuT35.CountryCode = 255;
        ituMetadata.ItuT35.CountryCodeExtension = 7;
        ituMetadata.ItuT35.Payload = [0x12, 0x34, 0x80, 0];
        Av1MetadataParseResult ituRead = Av1MetadataParser.ParsePayload(
            Av1MetadataWriter.WritePayload(ituMetadata).Data);

        ParsedAv1Metadata timecodeMetadata = new ParsedAv1Metadata(
            (ulong)Av1MetadataType.Timecode);
        timecodeMetadata.Timecode = new Av1TimecodeMetadata();
        timecodeMetadata.Timecode.CountingType = 3;
        timecodeMetadata.Timecode.FullTimestamp = true;
        timecodeMetadata.Timecode.CountDropped = true;
        timecodeMetadata.Timecode.FrameCount = 100;
        timecodeMetadata.Timecode.Seconds = 59;
        timecodeMetadata.Timecode.Minutes = 58;
        timecodeMetadata.Timecode.Hours = 23;
        timecodeMetadata.Timecode.TimeOffsetLength = 5;
        timecodeMetadata.Timecode.TimeOffsetValue = 21;
        Av1MetadataParseResult timecodeRead = Av1MetadataParser.ParsePayload(
            Av1MetadataWriter.WritePayload(timecodeMetadata).Data);

        Assert.Multiple(() =>
        {
            Assert.That(scalabilityRead.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((scalabilityRead.Metadata ?? throw new InvalidOperationException("Invalid internal state.")).Scalability ?? throw new InvalidOperationException("Invalid internal state.")).SpatialLayerMaximumWidths,
                Is.EqualTo(new int[] { 640, 1280 }));
            Assert.That((scalabilityRead.Metadata.Scalability ?? throw new InvalidOperationException("Invalid internal state.")).TemporalGroups[0]
                .ReferencePictureDifferences, Is.EqualTo(new int[] { 1, 3 }));
            Assert.That(ituRead.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((ituRead.Metadata ?? throw new InvalidOperationException("Invalid internal state.")).ItuT35 ?? throw new InvalidOperationException("Invalid internal state.")).CountryCode, Is.EqualTo(255));
            Assert.That((ituRead.Metadata.ItuT35 ?? throw new InvalidOperationException("Invalid internal state.")).CountryCodeExtension, Is.EqualTo(7));
            Assert.That(ituRead.Metadata.ItuT35.Payload,
                Is.EqualTo(new byte[] { 0x12, 0x34, 0x80, 0 }));
            Assert.That(timecodeRead.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((timecodeRead.Metadata ?? throw new InvalidOperationException("Invalid internal state.")).Timecode ?? throw new InvalidOperationException("Invalid internal state.")).CountDropped, Is.True);
            Assert.That((timecodeRead.Metadata.Timecode ?? throw new InvalidOperationException("Invalid internal state.")).Seconds, Is.EqualTo(59));
            Assert.That(timecodeRead.Metadata.Timecode.TimeOffsetValue,
                Is.EqualTo(21));
        });
    }

    [Test]
    public void TileGroupWriterRoundTripsFullAndPartialGroups()
    {
        Av1TileInfo info = new Av1TileInfo();
        info.TileColumnsLog2 = 1;
        info.TileRowsLog2 = 1;
        info.TileColumns = 2;
        info.TileRows = 2;
        info.TileSizeBytes = 2;
        byte[][] allTiles = [[0xA1, 0xA2], [0xB1], [0xC1, 0xC2], [0xD1]];
        Av1WriteResult full = Av1TileGroupWriter.Write(info, 0, allTiles);
        Av1TileGroupParseResult fullRead = ParseTiles(full.Data, info);
        Av1WriteResult partial = Av1TileGroupWriter.Write(
            info,
            1,
            [[0x31], [0x41, 0x42]]);
        Av1TileGroupParseResult partialRead = ParseTiles(partial.Data, info);

        Assert.Multiple(() =>
        {
            Assert.That(full.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(fullRead.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((fullRead.Group ?? throw new InvalidOperationException("Invalid internal state.")).StartTile, Is.Zero);
            Assert.That(fullRead.Group.EndTile, Is.EqualTo(3));
            Assert.That(fullRead.Group.Tiles[2].GetData().ToArray(),
                Is.EqualTo(new byte[] { 0xC1, 0xC2 }));
            Assert.That(partialRead.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((partialRead.Group ?? throw new InvalidOperationException("Invalid internal state.")).StartTile, Is.EqualTo(1));
            Assert.That(partialRead.Group.EndTile, Is.EqualTo(2));
            Assert.That(partialRead.Group.Tiles[1].GetData().ToArray(),
                Is.EqualTo(new byte[] { 0x41, 0x42 }));
        });

        (fullRead.Group ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (partialRead.Group ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    [Test]
    public void HeaderWritersPreserveParsedSequenceAndFrameHeaderBits()
    {
        byte[] sequencePayload = BuildReducedSequenceHeader();
        Av1SequenceHeaderParseResult sequence =
            Av1SequenceHeaderParser.ParsePayload(sequencePayload);
        Av1WriteResult sequenceWrite =
            Av1SequenceHeaderWriter.WritePayload(sequence.Header);

        ParsedAv1FrameHeader frame = new ParsedAv1FrameHeader();
        frame.SourcePayload = [0b1010_0000];
        frame.BitsConsumed = 3;
        Av1WriteResult frameTrailing = Av1FrameHeaderWriter.WritePayload(frame, true);
        Av1WriteResult frameAligned = Av1FrameHeaderWriter.WritePayload(frame, false);

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(sequenceWrite.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(sequenceWrite.Data, Is.EqualTo(sequencePayload));
            Assert.That(frameTrailing.Data, Is.EqualTo(new byte[] { 0b1011_0000 }));
            Assert.That(frameAligned.Data, Is.EqualTo(new byte[] { 0b1010_0000 }));
        });
    }

    static Av1TileGroupParseResult ParseTiles(byte[] payload, Av1TileInfo info)
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.Tiles = info;
        ResourceGuard resources = new ResourceGuard(new AvifSecurityLimits());
        resources.BeginTileSet();
        return Av1TileGroupParser.Parse(payload, 0, header, resources);
    }

    static byte[] BuildReducedSequenceHeader()
    {
        byte[] payload = new byte[16];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 4, 5);
        writer.WriteBits(payload, 5, 4);
        writer.WriteBits(payload, 4, 4);
        writer.WriteBits(payload, 63, 6);
        writer.WriteBits(payload, 31, 5);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteTrailingBits(payload);
        return payload[..(int)(writer.BitPosition / 8)];
    }
}
