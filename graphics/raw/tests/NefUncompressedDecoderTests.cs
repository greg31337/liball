using raw;

namespace raw.Tests;

public class NefUncompressedDecoderTests
{
    [Test]
    public void PackedTwelveBitStripsDecodeIndependentAlignedRowsAndPreserveStoredSamples()
    {
        ushort[] expected = new ushort[] { 0, 1, 4095, 17, 2048, 3000, 31, 63, 127 };
        int width = 3;
        int height = 3;
        int rowsPerStrip = 2;
        int rowStride = 8;
        byte[] source = new byte[rowStride * height];
        PackRows(source.AsSpan(0, rowStride * 2), expected.AsSpan(0, 6), width, 12, RawBitOrder.MostSignificantBitFirst, rowStride);
        PackRows(source.AsSpan(rowStride * 2, rowStride), expected.AsSpan(6, 3), width, 12, RawBitOrder.MostSignificantBitFirst, rowStride);
        TiffDataBlock[] blocks = new TiffDataBlock[]
        {
            new TiffDataBlock(0, (ulong)(rowStride * 2), false, 0, 0, 0),
            new TiffDataBlock((ulong)(rowStride * 2), (ulong)rowStride, false, 1, 0, 1)
        };
        NefDecodeFixture fixture = CreateFixture(source, width, height, 12, RawPacking.PackedBits, 12, 4, RawByteOrder.LittleEndian, 1, blocks, rowsPerStrip, 0, 0,
            new string[] { NefUncompressedDecoder.PackedMostSignificantBitFirstQuirk });
        fixture.Options.PreserveStoredSamples = true;

        RawSensorImage image = fixture.DecodeDocument();

        Assert.Multiple(() =>
        {
            Assert.That((image.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedSamples, Is.EqualTo(expected.Select(value => (uint)value)));
            Assert.That(image.IntegerSamples, Is.EqualTo(Normalize(expected, 12)));
            Assert.That(image.RowStrideSamples, Is.EqualTo(width));
            Assert.That((image.BlackLevel ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(2, 2, 0), Is.Zero);
            Assert.That(image.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void PackedFourteenBitTilesDecodeLsbRowsAndDiscardFullEdgeTilePadding()
    {
        int width = 5;
        int height = 3;
        int tileWidth = 3;
        int tileHeight = 2;
        ushort[] expected = Enumerable.Range(0, width * height).Select(index => (ushort)(100 + index * 37)).ToArray();
        List<byte> bytes = new List<byte>();
        List<TiffDataBlock> blocks = new List<TiffDataBlock>();
        int tilesAcross = 2;
        int tilesDown = 2;
        for (int tileY = 0; tileY < tilesDown; tileY++)
        {
            for (int tileX = 0; tileX < tilesAcross; tileX++)
            {
                ushort[] tile = new ushort[tileWidth * tileHeight];
                for (int y = 0; y < tileHeight; y++)
                {
                    for (int x = 0; x < tileWidth; x++)
                    {
                        int imageX = tileX * tileWidth + x;
                        int imageY = tileY * tileHeight + y;
                        tile[y * tileWidth + x] = imageX < width && imageY < height ? expected[imageY * width + imageX] : (ushort)16000;
                    }
                }
                int rowBytes = BitPacking.GetPackedByteCount(tileWidth, 14);
                byte[] encoded = new byte[rowBytes * tileHeight];
                PackRows(encoded, tile, tileWidth, 14, RawBitOrder.LeastSignificantBitFirst, rowBytes);
                int index = blocks.Count;
                blocks.Add(new TiffDataBlock((ulong)bytes.Count, (ulong)encoded.Length, true, index, 0, index));
                bytes.AddRange(encoded);
            }
        }
        NefDecodeFixture fixture = CreateFixture(bytes.ToArray(), width, height, 14, RawPacking.PackedBits, 14, 1, RawByteOrder.BigEndian, 1, blocks.ToArray(), 0, tileWidth, tileHeight,
            new string[] { NefUncompressedDecoder.PackedLeastSignificantBitFirstQuirk });

        RawSensorImage image = fixture.Decode();

        Assert.That(image.IntegerSamples, Is.EqualTo(Normalize(expected, 14)));
    }

    [Test]
    public void ContinuousPackedBlockCarriesBitsAcrossTwelveBitRowBoundaries()
    {
        ushort[] expected = new ushort[] { 1, 2, 3, 3000, 3500, 4095 };
        uint[] values = expected.Select(value => (uint)value).ToArray();
        byte[] source = new byte[BitPacking.GetPackedByteCount(values.Length, 12)];
        BitPacking.PackUnsigned(values, 12, RawBitOrder.MostSignificantBitFirst, source);
        TiffDataBlock block = new TiffDataBlock(0, (ulong)source.Length, false, 0, 0, 0);
        NefDecodeFixture fixture = CreateFixture(source, 3, 2, 12, RawPacking.PackedBits, 12, 1, RawByteOrder.LittleEndian, 1, new TiffDataBlock[] { block }, 2, 0, 0,
            new string[] { NefUncompressedDecoder.PackedMostSignificantBitFirstQuirk, NefUncompressedDecoder.BlockBitstreamQuirk });

        RawSensorImage image = fixture.Decode();

        Assert.That(image.IntegerSamples, Is.EqualTo(Normalize(expected, 12)));
    }

    [TestCase(RawPacking.Raw12, 12)]
    [TestCase(RawPacking.Raw14, 14)]
    public void NamedPackedByteLayoutsDecodePartialGroups(RawPacking packing, int precision)
    {
        ushort maximum = (ushort)((1 << precision) - 1);
        ushort[] expected = new ushort[] { 0, 1, 255, 256, maximum };
        uint[] values = expected.Select(value => (uint)value).ToArray();
        byte[] source = new byte[BitPacking.GetNamedPackedByteCount(values.Length, packing)];
        BitPacking.PackNamed(values, packing, source);
        TiffDataBlock block = new TiffDataBlock(0, (ulong)source.Length, false, 0, 0, 0);
        NefDecodeFixture fixture = CreateFixture(source, values.Length, 1, precision, packing, precision, 1, RawByteOrder.LittleEndian, 1, new TiffDataBlock[] { block }, 1, 0, 0, Array.Empty<string>());

        RawSensorImage image = fixture.Decode();

        Assert.That(image.IntegerSamples, Is.EqualTo(Normalize(expected, precision)));
    }

    [TestCase(12, RawByteOrder.LittleEndian, false)]
    [TestCase(14, RawByteOrder.BigEndian, true)]
    [TestCase(16, RawByteOrder.LittleEndian, false)]
    public void UnpackedWordsDecodeTwelveFourteenAndSixteenBitEndianAndAlignmentLayouts(int precision, RawByteOrder wordOrder, bool leftAligned)
    {
        ushort maximum = precision == 16 ? ushort.MaxValue : (ushort)((1 << precision) - 1);
        ushort[] expected = new ushort[] { 0, 1, (ushort)(maximum / 2), maximum, 29, 731 };
        int width = 3;
        int rowStride = 8;
        byte[] source = new byte[rowStride * 2];
        for (int index = 0; index < expected.Length; index++)
        {
            ushort word = leftAligned ? (ushort)(expected[index] << (16 - precision)) : expected[index];
            int row = index / width;
            int column = index % width;
            EndianWriter.WriteUInt16(source.AsSpan(row * rowStride + column * 2, 2), word, wordOrder);
        }
        string orderQuirk = wordOrder == RawByteOrder.LittleEndian ? NefUncompressedDecoder.WordLittleEndianQuirk : NefUncompressedDecoder.WordBigEndianQuirk;
        List<string> quirks = new List<string>() { orderQuirk };
        if (leftAligned)
        {
            quirks.Add(NefUncompressedDecoder.LeftAlignedWordQuirk);
        }
        RawByteOrder tiffOrder = wordOrder == RawByteOrder.LittleEndian ? RawByteOrder.BigEndian : RawByteOrder.LittleEndian;
        TiffDataBlock block = new TiffDataBlock(0, (ulong)source.Length, false, 0, 0, 0);
        NefDecodeFixture fixture = CreateFixture(source, width, 2, precision, RawPacking.ByteAligned, 16, 4, tiffOrder, 1, new TiffDataBlock[] { block }, 2, 0, 0, quirks.ToArray());

        RawSensorImage image = fixture.Decode();

        Assert.That(image.IntegerSamples, Is.EqualTo(Normalize(expected, precision)));
    }

    [Test]
    public void DecoderRejectsWrongBlockLengthAndNonzeroLeftAlignedWordPadding()
    {
        byte[] shortSource = new byte[7];
        TiffDataBlock shortBlock = new TiffDataBlock(0, 7, false, 0, 0, 0);
        NefDecodeFixture shortFixture = CreateFixture(shortSource, 3, 1, 12, RawPacking.ByteAligned, 16, 4, RawByteOrder.LittleEndian, 1, new TiffDataBlock[] { shortBlock }, 1, 0, 0,
            new string[] { NefUncompressedDecoder.WordLittleEndianQuirk });
        byte[] paddedSource = new byte[2];
        EndianWriter.WriteUInt16(paddedSource, 5, RawByteOrder.BigEndian);
        TiffDataBlock paddedBlock = new TiffDataBlock(0, 2, false, 0, 0, 0);
        NefDecodeFixture paddedFixture = CreateFixture(paddedSource, 1, 1, 14, RawPacking.ByteAligned, 16, 1, RawByteOrder.LittleEndian, 1, new TiffDataBlock[] { paddedBlock }, 1, 0, 0,
            new string[] { NefUncompressedDecoder.WordBigEndianQuirk, NefUncompressedDecoder.LeftAlignedWordQuirk });

        RawFormatException? length = Assert.Throws<RawFormatException>(() => shortFixture.Decode());
        RawFormatException? padding = Assert.Throws<RawFormatException>(() => paddedFixture.Decode());

        Assert.Multiple(() =>
        {
            Assert.That((length ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((padding ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
        });
    }

    static void PackRows(Span<byte> destination, ReadOnlySpan<ushort> samples, int width, int precision, RawBitOrder bitOrder, int rowStride)
    {
        int rowCount = samples.Length / width;
        for (int row = 0; row < rowCount; row++)
        {
            uint[] values = samples.Slice(row * width, width).ToArray().Select(value => (uint)value).ToArray();
            int rowBytes = BitPacking.GetPackedByteCount(width, precision);
            BitPacking.PackUnsigned(values, precision, bitOrder, destination.Slice(row * rowStride, rowBytes));
        }
    }

    static ushort[] Normalize(ushort[] values, int precision)
    {
        uint maximum = precision == 16 ? ushort.MaxValue : (1U << precision) - 1U;
        return values.Select(value => (ushort)(((uint)value * ushort.MaxValue + maximum / 2U) / maximum)).ToArray();
    }

    internal static NefDecodeFixture CreateFixture(byte[] source, int width, int height, int precision, RawPacking packing, int storedWordBits, int rowAlignment, RawByteOrder tiffByteOrder, ulong fillOrder, TiffDataBlock[] blocks, int rowsPerStrip, int tileWidth, int tileHeight, string[] quirks)
    {
        TiffIfd ifd = new TiffIfd("IFD0/SubIFD0", 8, 8, 1, Array.Empty<TiffEntry>(), 0);
        TiffImageDescriptor descriptor = new TiffImageDescriptor(ifd.Path, ifd, 0);
        descriptor.Width = (ulong)width;
        descriptor.Height = (ulong)height;
        descriptor.SamplesPerPixel = 1;
        descriptor.BitsPerSample = new ulong[] { (ulong)precision };
        descriptor.CompressionCode = 1;
        descriptor.Compression = RawCompression.None;
        descriptor.PhotometricCode = 32803;
        descriptor.Photometric = RawPhotometricInterpretation.ColorFilterArray;
        descriptor.FillOrder = fillOrder;
        descriptor.PlanarConfigurationCode = 1;
        descriptor.PlanarConfiguration = TiffPlanarConfiguration.Chunky;
        descriptor.PredictorCode = 1;
        descriptor.Predictor = TiffPredictor.None;
        descriptor.SampleFormatCodes = new ulong[] { 1 };
        descriptor.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        descriptor.RowsPerStrip = (ulong)rowsPerStrip;
        descriptor.TileWidth = (ulong)tileWidth;
        descriptor.TileHeight = (ulong)tileHeight;
        descriptor.Blocks = blocks;
        descriptor.StorageKind = tileWidth == 0 ? TiffStorageKind.Strips : TiffStorageKind.Tiles;
        TiffHeader header = new TiffHeader(tiffByteOrder, false, 42, 4, 8, 0, 8);
        TiffIfdGraph graph = new TiffIfdGraph(header, new TiffIfd[] { ifd }, Array.Empty<TiffIfdLink>());
        graph.Images = new TiffImageDescriptor[] { descriptor };
        graph.ContainerStart = 0;
        graph.ContainerLength = source.Length;
        RawReadProfile profile = new RawReadProfile();
        profile.Id = "test.nikon.nef.uncompressed";
        profile.Identity.Format = RawFormat.Nef;
        profile.Identity.Make = "NIKON CORPORATION";
        profile.Identity.Model = "TEST CAMERA";
        profile.Fingerprint.Width = width;
        profile.Fingerprint.Height = height;
        profile.Fingerprint.BitDepth = precision;
        profile.Fingerprint.Compression = 1;
        profile.Fingerprint.SamplesPerPixel = 1;
        profile.Geometry.StoredWidth = width;
        profile.Geometry.StoredHeight = height;
        profile.Geometry.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        profile.Codec.Name = NefUncompressedDecoder.CodecName;
        profile.Codec.Compression = 1;
        profile.Codec.Packing = packing;
        profile.Codec.StoredWordBits = storedWordBits;
        profile.Codec.Predictor = 0;
        profile.Codec.PlanarConfiguration = 1;
        profile.Codec.RowAlignment = rowAlignment;
        profile.Codec.Quirks = quirks;
        RawNikonNefMetadata metadata = new RawNikonNefMetadata();
        metadata.ProfileId = profile.Id;
        metadata.StoredWidth = width;
        metadata.StoredHeight = height;
        metadata.Compression = RawCompression.None;
        metadata.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        metadata.BlackLevels = new double[] { 0 };
        metadata.WhiteLevels = new double[] { precision == 16 ? ushort.MaxValue : (1 << precision) - 1 };
        RawArea stored = new RawArea(RawAreaKind.StoredImage, 0, 0, width, height);
        RawArea active = new RawArea(RawAreaKind.Active, 0, 0, width, height);
        RawArea defaultCrop = new RawArea(RawAreaKind.DefaultCrop, 0, 0, width, height);
        RawImageInfo info = new RawImageInfo(0, RawImageKind.Raw, width, height, precision, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.None, RawOrientation.Normal,
            new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) },
            new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, width, height, precision, width) },
            metadata.Cfa, stored, active, defaultCrop, null);
        return new NefDecodeFixture(source, graph, descriptor, info, metadata, profile);
    }
}

internal class NefDecodeFixture
{
    internal byte[] Source;
    internal TiffIfdGraph Graph;
    internal TiffImageDescriptor Descriptor;
    internal RawImageInfo Info;
    internal RawNikonNefMetadata Metadata;
    internal RawReadProfile Profile;
    internal RawDecodeOptions Options;

    internal NefDecodeFixture(byte[] source, TiffIfdGraph graph, TiffImageDescriptor descriptor, RawImageInfo info, RawNikonNefMetadata metadata, RawReadProfile profile)
    {
        Source = source;
        Graph = graph;
        Descriptor = descriptor;
        Info = info;
        Metadata = metadata;
        Profile = profile;
        Options = new RawDecodeOptions();
    }

    internal RawSensorImage Decode()
    {
        return NefUncompressedDecoder.Decode(Source, Graph, Descriptor, Info, Metadata, Profile, Options);
    }

    internal RawSensorImage DecodeDocument()
    {
        RawCapabilities capabilities = new RawCapabilities(true, true, true, false, false, false, false);
        RawDocument document = new RawDocument(RawFormat.Nef, Profile.Id, null, new RawImageInfo[] { Info }, null, new RawPayloadMap(Source.Length, null), capabilities, null, true, 1, false, true);
        document.AttachSource(Source, false, Graph);
        document.AttachNikonNefMetadata(Metadata);
        document.AttachReadProfile(Profile);
        return document.Decode(Options);
    }
}
