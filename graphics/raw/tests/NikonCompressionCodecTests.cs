using raw;

namespace raw.Tests;

public class NikonCompressionCodecTests
{
    [TestCase(12)]
    [TestCase(14)]
    public void LosslessEncoderRoundTripsSignedExtremesWithPaddedRows(int precision)
    {
        int maximum = (1 << precision) - 1;
        ushort[] samples = new ushort[]
        {
            0, (ushort)maximum, 1, (ushort)(maximum - 1), 7, 8,
            (ushort)maximum, 0, (ushort)(maximum - 1), 1, 9, 10,
            1, (ushort)(maximum - 1), 0, (ushort)maximum, 11, 12,
            (ushort)(maximum - 1), 1, (ushort)maximum, 0, 13, 14
        };

        NikonLosslessEncoding encoded = NikonCompressionCodec.EncodeLossless(samples, 4, 4, 6, precision, RawByteOrder.LittleEndian, new RawLimits(), CancellationToken.None);
        NikonCompressionInfo compression = NikonCompressionCodec.ParseInfo(encoded.CompressionInfo, RawByteOrder.LittleEndian, precision, 4);
        ushort[] decoded = new ushort[16];
        MsbBitReader reader = new MsbBitReader(new BoundedInput(encoded.EntropyBytes, 0, encoded.EntropyBytes.Length, new RawLimits()));
        NikonCompressionCodec.DecodeSamples(reader, decoded, 4, 4, compression, new RawDecodeOptions());
        reader.AlignToByteWithExpectedPadding(0);
        reader.RequireEnd();

        Assert.Multiple(() =>
        {
            Assert.That(compression.Lossless, Is.True);
            Assert.That(compression.TreeIndex, Is.EqualTo(precision == 14 ? 5 : 2));
            Assert.That(decoded, Is.EqualTo(new ushort[]
            {
                0, (ushort)maximum, 1, (ushort)(maximum - 1),
                (ushort)maximum, 0, (ushort)(maximum - 1), 1,
                1, (ushort)(maximum - 1), 0, (ushort)maximum,
                (ushort)(maximum - 1), 1, (ushort)maximum, 0
            }));
        });
    }

    [TestCase(12)]
    [TestCase(14)]
    public void LossyEncoderEmitsExplicitCurveAndStaysWithinDeclaredError(int precision)
    {
        int maximum = (1 << precision) - 1;
        ushort[] samples = new ushort[]
        {
            0, 1, 2, 3, 7, 8,
            (ushort)maximum, (ushort)(maximum - 1), 101, 102, 9, 10,
            511, 512, 409, 410, 11, 12,
            2047, 2048, (ushort)Math.Min(8191, maximum), (ushort)Math.Min(8192, maximum), 13, 14
        };

        NikonLossyEncoding encoded = NikonCompressionCodec.EncodeLossy(samples, 4, 4, 6, precision, RawByteOrder.LittleEndian, new RawLimits(), CancellationToken.None);
        NikonCompressionInfo compression = NikonCompressionCodec.ParseInfo(encoded.CompressionInfo, RawByteOrder.LittleEndian, precision, 4);
        ushort[] decoded = new ushort[16];
        MsbBitReader reader = new MsbBitReader(new BoundedInput(encoded.EntropyBytes, 0, encoded.EntropyBytes.Length, new RawLimits()));
        NikonCompressionCodec.DecodeSamples(reader, decoded, 4, 4, compression, new RawDecodeOptions());
        reader.AlignToByteWithExpectedPadding(0);
        reader.RequireEnd();
        ushort[] sourceRows = new ushort[]
        {
            0, 1, 2, 3,
            (ushort)maximum, (ushort)(maximum - 1), 101, 102,
            511, 512, 409, 410,
            2047, 2048, (ushort)Math.Min(8191, maximum), (ushort)Math.Min(8192, maximum)
        };

        Assert.Multiple(() =>
        {
            Assert.That(compression.Lossless, Is.False);
            Assert.That(compression.TreeIndex, Is.EqualTo(precision == 14 ? 3 : 0));
            Assert.That(compression.Curve, Has.Length.EqualTo((1 << (precision - 1)) + 1));
            Assert.That(compression.Curve[0], Is.Zero);
            Assert.That(compression.Curve[1], Is.EqualTo(2));
            Assert.That(compression.Curve[^1], Is.EqualTo(maximum));
            Assert.That(encoded.MaximumError, Is.EqualTo(1));
            Assert.That(decoded.Zip(sourceRows, (actual, source) => Math.Abs(actual - source)), Is.All.LessThanOrEqualTo(encoded.MaximumError));
        });
    }

    [TestCase(12, 2)]
    [TestCase(14, 5)]
    public void LosslessTreesReconstructAlternatingPredictorsAndSignedDeltas(int precision, int treeIndex)
    {
        int[] differences = new int[] { 1, -1, 2, -2, 3, -3, 4, -4 };
        byte[] entropy = EncodeDifferences(treeIndex, differences);
        byte[] parameters = CreateLosslessParameters(precision, new ushort[] { 100, 200, 300, 400 });
        NikonCompressedFixture fixture = CreateFixture(entropy, parameters, 4, 2, precision, true);
        fixture.Options.PreserveStoredSamples = true;

        RawSensorImage image = fixture.DecodeDocument();

        ushort[] expected = new ushort[] { 101, 299, 103, 297, 203, 397, 207, 393 };
        Assert.Multiple(() =>
        {
            Assert.That((image.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedSamples, Is.EqualTo(expected.Select(value => (uint)value)));
            Assert.That(image.IntegerSamples, Is.EqualTo(Normalize(expected, precision)));
            Assert.That(image.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [TestCase(12, 0, 1, 2048, 4095)]
    [TestCase(14, 3, 4, 8192, 16383)]
    public void LossySplitSwitchesTreeAndAppliesInterpolatedCurve(int precision, int firstTree, int secondTree, int middle, int maximum)
    {
        byte[] parameters = CreateLossyParameters(new ushort[] { 100, 200, 300, 400 }, new ushort[] { 0, (ushort)middle, (ushort)maximum }, 1);
        byte[] entropy = EncodeRows(new int[] { 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0 }, firstTree, secondTree);
        NikonCompressedFixture mappedFixture = CreateFixture(entropy, parameters, 4, 2, precision, false);
        mappedFixture.Options.PreserveStoredSamples = true;
        NikonCompressionInfo parsed = NikonCompressionCodec.ParseInfo(parameters, RawByteOrder.LittleEndian, precision, 2);

        RawSensorImage mapped = mappedFixture.Decode();

        ushort[] expectedMapped = new ushort[]
        {
            parsed.Curve[100], parsed.Curve[300], parsed.Curve[100], parsed.Curve[300],
            parsed.Curve[200], parsed.Curve[400], parsed.Curve[200], parsed.Curve[400]
        };
        Assert.Multiple(() =>
        {
            Assert.That(parsed.SplitRow, Is.EqualTo(1));
            Assert.That((mapped.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedSamples, Is.EqualTo(expectedMapped.Select(value => (uint)value)));
        });

        NikonCompressedFixture rawFixture = CreateFixture(entropy, parameters, 4, 2, precision, false);
        rawFixture.Options.ApplyLinearization = false;
        rawFixture.Options.PreserveStoredSamples = true;
        RawSensorImage raw = rawFixture.Decode();
        Assert.That((raw.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedSamples, Is.EqualTo(new uint[] { 100, 300, 100, 300, 200, 400, 200, 400 }));
    }

    [TestCase(0, -508)]
    [TestCase(63, 508)]
    public void ShiftedLossyDifferenceSymbolsReconstructQuantizedMidpoints(int payload, int expected)
    {
        NikonPrefixTable table = NikonCompressionCodec.CreateTable(1);
        byte[] buffer = new byte[2];
        MsbBitWriter writer = new MsbBitWriter(buffer);
        WriteSymbol(writer, table, 0x39);
        writer.WriteBits((uint)payload, 6);
        int byteCount = (int)writer.Finish(0);
        MsbBitReader reader = new MsbBitReader(new BoundedInput(buffer, 0, byteCount, new RawLimits()));

        int actual = NikonCompressionCodec.DecodeDifference(reader, table, 12);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void DecoderRejectsTruncatedMetadataEntropyAndTrailingBytes()
    {
        byte[] parameters = CreateLosslessParameters(12, new ushort[] { 100, 200, 300, 400 });
        byte[] entropy = EncodeDifferences(2, new int[] { 1, -1, 2, -2 });
        NikonCompressedFixture truncatedMetadata = CreateFixture(entropy, parameters[..^1], 4, 1, 12, true);
        NikonCompressedFixture truncatedEntropy = CreateFixture(entropy[..^1], parameters, 4, 1, 12, true);
        NikonCompressedFixture trailing = CreateFixture(entropy.Concat(new byte[] { 0 }).ToArray(), parameters, 4, 1, 12, true);

        RawFormatException? metadataError = Assert.Throws<RawFormatException>(() => truncatedMetadata.Decode());
        RawFormatException? entropyError = Assert.Catch<RawFormatException>(() => truncatedEntropy.Decode());
        RawFormatException? trailingError = Assert.Throws<RawFormatException>(() => trailing.Decode());

        Assert.Multiple(() =>
        {
            Assert.That((metadataError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
            Assert.That((entropyError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((trailingError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
        });
    }

    [Test]
    public void DecoderRejectsProfileModeAndPredictorRangeMismatches()
    {
        byte[] lossless = CreateLosslessParameters(12, new ushort[] { 4095, 4095, 4095, 4095 });
        byte[] plusOne = EncodeDifferences(2, new int[] { 1 });
        NikonCompressedFixture range = CreateFixture(plusOne, lossless, 1, 1, 12, true);
        byte[] lossy = CreateLossyParameters(new ushort[] { 100, 200, 300, 400 }, new ushort[] { 0, 2048, 4095 }, 0);
        NikonCompressedFixture mode = CreateFixture(EncodeDifferences(0, new int[] { 0 }), lossy, 1, 1, 12, true);

        RawFormatException? rangeError = Assert.Throws<RawFormatException>(() => range.Decode());
        RawProfileException? modeError = Assert.Throws<RawProfileException>(() => mode.Decode());

        Assert.Multiple(() =>
        {
            Assert.That((rangeError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((modeError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidProfile));
        });
    }

    [Test]
    public void MetadataExtractionPrefersCompressionInfoAndCopiesItsBytes()
    {
        byte[] primary = new byte[] { 0x46, 0, 1, 2 };
        byte[] fallback = new byte[] { 0x44, 0, 3, 4 };
        RawMakerNote note = new RawMakerNote(Array.Empty<byte>(), "NIKON CORPORATION", "NikonEmbeddedTiff", 100, 90, "IFD0/Exif", RawByteOrder.BigEndian, RawMakerNoteOffsetBase.EmbeddedTiffHeader, 110, 10, Array.Empty<RawMakerNoteOffsetReference>(), true);
        note.NikonTags = new RawNikonMakerNoteTag[]
        {
            new RawNikonMakerNoteTag(0x008c, "CompressionCurve", RawNikonMakerNoteVersion.Unknown, RawTagValue.CreateUndefined(fallback), 20, 30, false, true),
            new RawNikonMakerNoteTag(0x0096, "NEFCompressionInfo", RawNikonMakerNoteVersion.Unknown, RawTagValue.CreateUndefined(primary), 40, 50, false, true)
        };
        RawNikonNefMetadata metadata = new RawNikonNefMetadata();

        NikonNefMetadataParser.ReadCompressionInfo(note, metadata);
        primary[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(metadata.CompressionInfoTag, Is.EqualTo(0x0096));
            Assert.That(metadata.CompressionInfo, Is.EqualTo(new byte[] { 0x46, 0, 1, 2 }));
            Assert.That(metadata.CompressionInfoByteOrder, Is.EqualTo(RawByteOrder.BigEndian));
        });
    }

    internal static byte[] CreateLosslessParameters(int precision, ushort[] predictors)
    {
        byte[] bytes = new byte[12];
        bytes[0] = 0x46;
        WriteWords(bytes, 2, predictors);
        WriteUInt16(bytes, 10, 0);
        return bytes;
    }

    internal static byte[] CreateLossyParameters(ushort[] predictors, ushort[] anchors, int splitRow)
    {
        byte[] bytes = new byte[564];
        bytes[0] = 0x44;
        bytes[1] = 0x20;
        WriteWords(bytes, 2, predictors);
        WriteUInt16(bytes, 10, (ushort)anchors.Length);
        WriteWords(bytes, 12, anchors);
        WriteUInt16(bytes, 562, (ushort)splitRow);
        return bytes;
    }

    static void WriteWords(byte[] bytes, int offset, ushort[] values)
    {
        for (int index = 0; index < values.Length; index++)
        {
            WriteUInt16(bytes, offset + index * 2, values[index]);
        }
    }

    static void WriteUInt16(byte[] bytes, int offset, ushort value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
    }

    internal static byte[] EncodeRows(int[] first, int[] second, int firstTree, int secondTree)
    {
        byte[] buffer = new byte[128];
        MsbBitWriter writer = new MsbBitWriter(buffer);
        WriteDifferences(writer, firstTree, first);
        WriteDifferences(writer, secondTree, second);
        int length = (int)writer.Finish(0);
        return buffer[..length];
    }

    internal static byte[] EncodeDifferences(int treeIndex, int[] differences)
    {
        byte[] buffer = new byte[128];
        MsbBitWriter writer = new MsbBitWriter(buffer);
        WriteDifferences(writer, treeIndex, differences);
        int length = (int)writer.Finish(0);
        return buffer[..length];
    }

    static void WriteDifferences(MsbBitWriter writer, int treeIndex, int[] differences)
    {
        NikonPrefixTable table = NikonCompressionCodec.CreateTable(treeIndex);
        foreach (int difference in differences)
        {
            int magnitude = Math.Abs(difference);
            int length = 0;
            while (magnitude != 0)
            {
                length++;
                magnitude >>= 1;
            }
            WriteSymbol(writer, table, length);
            if (length != 0)
            {
                int bits = difference >= 0 ? difference : difference + (1 << length) - 1;
                writer.WriteBits((uint)bits, length);
            }
        }
    }

    static void WriteSymbol(MsbBitWriter writer, NikonPrefixTable table, int symbol)
    {
        int symbolIndex = Array.FindIndex(table.Symbols, value => value == symbol);
        Assert.That(symbolIndex, Is.GreaterThanOrEqualTo(0));
        int codeLength = 1;
        while (symbolIndex >= table.FirstIndexes[codeLength] + table.Counts[codeLength])
        {
            codeLength++;
        }
        int code = table.FirstCodes[codeLength] + symbolIndex - table.FirstIndexes[codeLength];
        writer.WriteBits((uint)code, codeLength);
    }

    static ushort[] Normalize(ushort[] values, int precision)
    {
        uint maximum = (1U << precision) - 1U;
        return values.Select(value => (ushort)(((uint)value * ushort.MaxValue + maximum / 2U) / maximum)).ToArray();
    }

    internal static NikonCompressedFixture CreateFixture(byte[] source, byte[] parameters, int width, int height, int precision, bool lossless)
    {
        TiffIfd ifd = new TiffIfd("IFD0/SubIFD0", 8, 8, 1, Array.Empty<TiffEntry>(), 0);
        TiffImageDescriptor descriptor = new TiffImageDescriptor(ifd.Path, ifd, 0);
        descriptor.Width = (ulong)width;
        descriptor.Height = (ulong)height;
        descriptor.SamplesPerPixel = 1;
        descriptor.BitsPerSample = new ulong[] { (ulong)precision };
        descriptor.CompressionCode = 34713;
        descriptor.Compression = lossless ? RawCompression.NikonLossless : RawCompression.NikonLossy;
        descriptor.PlanarConfiguration = TiffPlanarConfiguration.Chunky;
        descriptor.Predictor = TiffPredictor.None;
        descriptor.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        descriptor.StorageKind = TiffStorageKind.Strips;
        descriptor.RowsPerStrip = (ulong)height;
        descriptor.Blocks = new TiffDataBlock[] { new TiffDataBlock(0, (ulong)source.Length, false, 0, 0, 0) };
        TiffHeader header = new TiffHeader(RawByteOrder.LittleEndian, false, 42, 4, 8, 0, 8);
        TiffIfdGraph graph = new TiffIfdGraph(header, new TiffIfd[] { ifd }, Array.Empty<TiffIfdLink>());
        graph.Images = new TiffImageDescriptor[] { descriptor };
        graph.ContainerLength = source.Length;
        RawReadProfile profile = new RawReadProfile();
        profile.Id = "test.nikon.nef.compressed";
        profile.Identity.Format = RawFormat.Nef;
        profile.Fingerprint.BitDepth = precision;
        profile.Geometry.StoredWidth = width;
        profile.Geometry.StoredHeight = height;
        profile.Codec.Name = lossless ? NikonCompressionCodec.LosslessCodecName : NikonCompressionCodec.LossyCodecName;
        profile.Codec.Compression = 34713;
        profile.Codec.Packing = RawPacking.PackedBits;
        profile.Codec.StoredWordBits = precision;
        profile.Codec.Predictor = 0;
        profile.Codec.PlanarConfiguration = 1;
        RawNikonNefMetadata metadata = new RawNikonNefMetadata();
        metadata.ProfileId = profile.Id;
        metadata.StoredWidth = width;
        metadata.StoredHeight = height;
        metadata.Compression = lossless ? RawCompression.NikonLossless : RawCompression.NikonLossy;
        metadata.CompressionInfo = parameters;
        metadata.CompressionInfoByteOrder = RawByteOrder.LittleEndian;
        metadata.Cfa = RawCfaPattern.CreateBayer(RawCfaLayout.Rggb);
        metadata.BlackLevels = new double[] { 0 };
        metadata.WhiteLevels = new double[] { (1 << precision) - 1 };
        RawArea stored = new RawArea(RawAreaKind.StoredImage, 0, 0, width, height);
        RawArea active = new RawArea(RawAreaKind.Active, 0, 0, width, height);
        RawArea crop = new RawArea(RawAreaKind.DefaultCrop, 0, 0, width, height);
        RawImageInfo info = new RawImageInfo(0, RawImageKind.Raw, width, height, precision, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, metadata.Compression, RawOrientation.Normal,
            new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) },
            new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, width, height, precision, width) }, metadata.Cfa, stored, active, crop, null);
        return new NikonCompressedFixture(source, graph, descriptor, info, metadata, profile);
    }
}

internal class NikonCompressedFixture
{
    internal byte[] Source;
    internal TiffIfdGraph Graph;
    internal TiffImageDescriptor Descriptor;
    internal RawImageInfo Info;
    internal RawNikonNefMetadata Metadata;
    internal RawReadProfile Profile;
    internal RawDecodeOptions Options;

    internal NikonCompressedFixture(byte[] source, TiffIfdGraph graph, TiffImageDescriptor descriptor, RawImageInfo info, RawNikonNefMetadata metadata, RawReadProfile profile)
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
        return NikonCompressionCodec.Decode(Source, Graph, Descriptor, Info, Metadata, Profile, Options);
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
