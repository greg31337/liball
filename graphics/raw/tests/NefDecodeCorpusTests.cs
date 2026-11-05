using raw;

namespace raw.Tests;

public class NefDecodeCorpusTests
{
    [TestCase(12, RawPacking.Raw12)]
    [TestCase(14, RawPacking.Raw14)]
    public void UncompressedCorpusDecodesNamedPackedTwelveAndFourteenBitRows(int precision, RawPacking packing)
    {
        ushort maximum = (ushort)((1 << precision) - 1);
        ushort[] expected = new ushort[] { 0, 1, 31, 255, 256, 1023, (ushort)(maximum - 1), maximum };
        uint[] values = ToUInt32(expected);
        byte[] source = new byte[BitPacking.GetNamedPackedByteCount(values.Length, packing)];
        BitPacking.PackNamed(values, packing, source);
        TiffDataBlock block = new TiffDataBlock(0, (ulong)source.Length, false, 0, 0, 0);
        NefDecodeFixture fixture = NefUncompressedDecoderTests.CreateFixture(source, 4, 2, precision, packing, precision, 1, RawByteOrder.LittleEndian, 1, new TiffDataBlock[] { block }, 2, 0, 0, Array.Empty<string>());
        fixture.Options.PreserveStoredSamples = true;

        RawSensorImage image = fixture.DecodeDocument();

        Assert.Multiple(() =>
        {
            Assert.That(image.Info.BitDepth, Is.EqualTo(precision));
            Assert.That((image.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedSamples, Is.EqualTo(values));
            Assert.That(image.IntegerSamples, Is.EqualTo(Normalize(expected, precision)));
            Assert.That(image.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [TestCase(12, true)]
    [TestCase(14, true)]
    [TestCase(12, false)]
    [TestCase(14, false)]
    public void CompressedCorpusDecodesLosslessAndLossyTwelveAndFourteenBitTrees(int precision, bool lossless)
    {
        ushort[] predictors = new ushort[] { 100, 200, 300, 400 };
        int tree = lossless ? precision == 12 ? 2 : 5 : precision == 12 ? 0 : 3;
        byte[] entropy = NikonCompressionCodecTests.EncodeDifferences(tree, new int[] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ushort maximum = (ushort)((1 << precision) - 1);
        byte[] parameters = lossless
            ? NikonCompressionCodecTests.CreateLosslessParameters(precision, predictors)
            : NikonCompressionCodecTests.CreateLossyParameters(predictors, new ushort[] { 0, (ushort)(maximum / 2), maximum }, 0);
        NikonCompressedFixture fixture = NikonCompressionCodecTests.CreateFixture(entropy, parameters, 4, 2, precision, lossless);
        fixture.Options.PreserveStoredSamples = true;

        RawSensorImage image = fixture.DecodeDocument();
        NikonCompressionInfo parsed = NikonCompressionCodec.ParseInfo(parameters, RawByteOrder.LittleEndian, precision, 2);
        ushort[] raw = new ushort[] { 100, 300, 100, 300, 200, 400, 200, 400 };
        ushort[] expected = MapCurve(raw, parsed.Curve);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Lossless, Is.EqualTo(lossless));
            Assert.That(image.Info.Compression, Is.EqualTo(lossless ? RawCompression.NikonLossless : RawCompression.NikonLossy));
            Assert.That((image.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedSamples, Is.EqualTo(ToUInt32(expected)));
            Assert.That(image.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [TestCase(12, 0, 1)]
    [TestCase(14, 3, 4)]
    public void LossyCorpusSwitchesTreesAtTheDeclaredSplitRow(int precision, int firstTree, int secondTree)
    {
        ushort maximum = (ushort)((1 << precision) - 1);
        byte[] parameters = NikonCompressionCodecTests.CreateLossyParameters(new ushort[] { 100, 200, 300, 400 }, new ushort[] { 0, (ushort)(maximum / 2), maximum }, 1);
        byte[] entropy = NikonCompressionCodecTests.EncodeRows(new int[] { 0, 0, 0, 0 }, new int[] { 0, 0, 0, 0 }, firstTree, secondTree);
        NikonCompressedFixture fixture = NikonCompressionCodecTests.CreateFixture(entropy, parameters, 4, 2, precision, false);
        fixture.Options.PreserveStoredSamples = true;

        RawSensorImage image = fixture.DecodeDocument();
        NikonCompressionInfo parsed = NikonCompressionCodec.ParseInfo(parameters, RawByteOrder.LittleEndian, precision, 2);
        ushort[] raw = new ushort[] { 100, 300, 100, 300, 200, 400, 200, 400 };

        Assert.Multiple(() =>
        {
            Assert.That(parsed.SplitRow, Is.EqualTo(1));
            Assert.That((image.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedSamples, Is.EqualTo(ToUInt32(MapCurve(raw, parsed.Curve))));
        });
    }

    [TestCase(12)]
    [TestCase(14)]
    public void CurveCorpusParsesQuarterDomainAndExtendedTableVersions(int precision)
    {
        byte[] quarterBytes = CreateQuarterCurve(precision);
        byte[] extendedBytes = CreateExtendedCurve();

        NikonCompressionInfo quarter = NikonCompressionCodec.ParseInfo(quarterBytes, RawByteOrder.LittleEndian, precision, 4);
        NikonCompressionInfo extended = NikonCompressionCodec.ParseInfo(extendedBytes, RawByteOrder.LittleEndian, precision, 4);

        Assert.Multiple(() =>
        {
            Assert.That(quarter.Lossless, Is.False);
            Assert.That(quarter.Curve, Has.Length.EqualTo((1 << precision) / 4));
            Assert.That(quarter.Curve[0], Is.Zero);
            Assert.That(quarter.Curve[quarter.Curve.Length - 1], Is.LessThanOrEqualTo((1 << precision) - 1));
            Assert.That(extended.Lossless, Is.False);
            Assert.That(extended.Predictors, Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(extended.Curve, Is.EqualTo(new ushort[] { 11, 22, 33, 44 }));
        });
    }

    [TestCase(1, RawPhotometricInterpretation.BlackIsZero, 1, 12)]
    [TestCase(2, RawPhotometricInterpretation.LinearRaw, 1, 14)]
    [TestCase(3, RawPhotometricInterpretation.Rgb, 3, 14)]
    [TestCase(4, RawPhotometricInterpretation.YCbCr, 3, 12)]
    public void LinearVariantCorpusDecodesOnlyExplicitNonCfaProfiles(int kindValue, RawPhotometricInterpretation photometric, int channels, int precision)
    {
        ushort[] expected = new ushort[3 * 2 * channels];
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = (ushort)(50 + index * 29);
        }
        NefVariantFixture fixture = NefVariantDecoderTests.CreateLinearFixture((NefVariantKind)kindValue, photometric, 3, 2, precision, channels, expected);
        fixture.Options.PreserveStoredSamples = true;

        RawSensorImage image = fixture.DecodeDocument();

        Assert.Multiple(() =>
        {
            Assert.That(image.Info.Photometric, Is.EqualTo(photometric));
            Assert.That(image.Info.Cfa, Is.Null);
            Assert.That(image.Info.GetChannelCount(), Is.EqualTo(channels));
            Assert.That((image.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedSamples, Is.EqualTo(ToUInt32(expected)));
            Assert.That(image.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void OlderAndSmallVariantCorpusDecodesContinuousRgbAndSubsampledYCbCr()
    {
        ushort[] olderExpected = new ushort[18];
        for (int index = 0; index < olderExpected.Length; index++)
        {
            olderExpected[index] = (ushort)(700 + index * 101);
        }
        NefVariantFixture older = NefVariantDecoderTests.CreateLinearFixture(NefVariantKind.Rgb, RawPhotometricInterpretation.Rgb, 3, 2, 14, 3, olderExpected);
        uint[] continuousValues = ToUInt32(olderExpected);
        byte[] continuous = new byte[BitPacking.GetPackedByteCount(continuousValues.Length, 14)];
        BitPacking.PackUnsigned(continuousValues, 14, RawBitOrder.MostSignificantBitFirst, continuous);
        older.Source = continuous;
        older.Graph.ContainerLength = continuous.Length;
        older.Descriptor.RowsPerStrip = 2;
        older.Descriptor.Blocks = new TiffDataBlock[] { new TiffDataBlock(0, (ulong)continuous.Length, false, 0, 0, 0) };
        older.Profile.Codec.Quirks = new string[] { NefVariantDecoder.RgbQuirk, NefUncompressedDecoder.PackedMostSignificantBitFirstQuirk, NefUncompressedDecoder.BlockBitstreamQuirk };

        ushort[] smallComponents = new ushort[] { 100, 200, 1000, 3000, 300, 400, 2000, 1000 };
        NefVariantFixture small = NefVariantDecoderTests.CreateSmallFixture(NefVariantDecoderTests.PackSmall(smallComponents), 4, 1);

        RawSensorImage olderImage = older.DecodeDocument();
        RawSensorImage smallImage = small.DecodeDocument();

        Assert.Multiple(() =>
        {
            Assert.That(olderImage.IntegerSamples, Is.EqualTo(Normalize(olderExpected, 14)));
            Assert.That(smallImage.Info.Photometric, Is.EqualTo(RawPhotometricInterpretation.YCbCr));
            Assert.That(smallImage.IntegerSamples, Is.EqualTo(Normalize(new ushort[] { 100, 1000, 3000, 200, 1500, 2000, 300, 2000, 1000, 400, 2000, 1000 }, 12)));
        });
    }

    [Test]
    public void PreviewCorpusRetainsAndDecodesEveryDisplayImage()
    {
        NefFixture fixture = NefParserTests.BuildNef();

        RawDocument document = RawCodec.OpenPreviews(fixture.Bytes, null, new RawOpenOptions());

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(document.Thumbnails.Select(item => item.EncodedBytes), Is.EqualTo(new byte[][] { fixture.Preview, fixture.Thumbnail }));
            Assert.That(document.Thumbnails.Select(item => item.Decode(new RawLimits(), CancellationToken.None).ByteSamples), Is.EqualTo(new byte[][] { fixture.Preview, fixture.Thumbnail }));
        });
    }

    [TestCase(13, RawCompression.NikonHe, NikonHeRecognition.HeMessage)]
    [TestCase(14, RawCompression.NikonHeStar, NikonHeRecognition.HeStarMessage)]
    public void HighEfficiencyCorpusIsInspectableButNeverGuessesPixels(int makerCode, RawCompression compression, string message)
    {
        NikonHeFixture fixture = NikonHeRecognitionTests.BuildNef((ushort)makerCode);

        RawDocument document = RawCodec.Open(fixture.Bytes, null, new RawOpenOptions());
        RawImageInfo? rawImage = (document.Images ?? throw new System.InvalidOperationException("A required value is unavailable.")).Single(item => item.Kind == RawImageKind.Raw);
        RawUnsupportedException? exception = Assert.Throws<RawUnsupportedException>(() => document.Decode(new RawDecodeOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(((document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Nikon ?? throw new System.InvalidOperationException("A required value is unavailable.")).Compression, Is.EqualTo(compression));
            Assert.That(document.Capabilities.Recognized, Is.True);
            Assert.That(document.Capabilities.Inspected, Is.True);
            Assert.That(document.Capabilities.Decoded, Is.False);
            Assert.That(document.ExtractPayload(rawImage.Payload), Is.EqualTo(fixture.Raw));
            Assert.That(document.Diagnostics.Single(item => item.Code == RawDiagnosticCode.UnsupportedCodec).Message, Is.EqualTo(message));
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedCodec));
            Assert.That(exception.Message, Is.EqualTo(message));
        });
    }

    [Test]
    public void CorruptionCorpusRejectsTruncatedContainerEntropyCurveAndUncompressedRows()
    {
        NefFixture container = NefParserTests.BuildNef();
        byte[] shortContainer = container.Bytes.AsSpan(0, 15).ToArray();
        byte[] parameters = NikonCompressionCodecTests.CreateLosslessParameters(12, new ushort[] { 100, 200, 300, 400 });
        byte[] completeEntropy = NikonCompressionCodecTests.EncodeDifferences(2, new int[] { 1, -1, 2, -2 });
        NikonCompressedFixture entropy = NikonCompressionCodecTests.CreateFixture(completeEntropy[..^1], parameters, 4, 1, 12, true);
        byte[] shortRow = new byte[5];
        NefDecodeFixture row = NefUncompressedDecoderTests.CreateFixture(shortRow, 4, 1, 12, RawPacking.Raw12, 12, 1, RawByteOrder.LittleEndian, 1, new TiffDataBlock[] { new TiffDataBlock(0, 5, false, 0, 0, 0) }, 1, 0, 0, Array.Empty<string>());
        byte[] shortCurve = CreateExtendedCurve().AsSpan(0, 2125).ToArray();

        RawFormatException? containerError = Assert.Throws<RawFormatException>(() => RawCodec.Open(shortContainer, null, new RawOpenOptions()));
        RawFormatException? entropyError = Assert.Catch<RawFormatException>(() => entropy.Decode());
        RawFormatException? rowError = Assert.Throws<RawFormatException>(() => row.Decode());
        RawFormatException? curveError = Assert.Throws<RawFormatException>(() => NikonCompressionCodec.ParseInfo(shortCurve, RawByteOrder.LittleEndian, 12, 4));

        Assert.Multiple(() =>
        {
            Assert.That((containerError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidOffset));
            Assert.That((entropyError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((rowError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((curveError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidMetadata));
        });
    }

    static byte[] CreateQuarterCurve(int precision)
    {
        int domain = (1 << precision) / 4;
        byte[] bytes = new byte[564];
        bytes[0] = 0x44;
        bytes[1] = 0x40;
        WriteWords(bytes, 2, new ushort[] { 100, 200, 300, 400 });
        EndianWriter.WriteUInt16(bytes.AsSpan(10, 2), 3, RawByteOrder.LittleEndian);
        WriteWords(bytes, 12, new ushort[] { 0, (ushort)(domain / 2), (ushort)(domain - 1) });
        return bytes;
    }

    static byte[] CreateExtendedCurve()
    {
        byte[] bytes = new byte[2130];
        bytes[0] = 0x49;
        WriteWords(bytes, 2112, new ushort[] { 0, 1, 2, 3 });
        EndianWriter.WriteUInt16(bytes.AsSpan(2120, 2), 4, RawByteOrder.LittleEndian);
        WriteWords(bytes, 2122, new ushort[] { 11, 22, 33, 44 });
        return bytes;
    }

    static void WriteWords(byte[] bytes, int offset, ushort[] values)
    {
        for (int index = 0; index < values.Length; index++)
        {
            EndianWriter.WriteUInt16(bytes.AsSpan(offset + index * 2, 2), values[index], RawByteOrder.LittleEndian);
        }
    }

    static ushort[] MapCurve(ushort[] values, ushort[] curve)
    {
        ushort[] result = new ushort[values.Length];
        for (int index = 0; index < values.Length; index++)
        {
            result[index] = curve[values[index]];
        }
        return result;
    }

    static uint[] ToUInt32(ushort[] values)
    {
        uint[] result = new uint[values.Length];
        for (int index = 0; index < values.Length; index++)
        {
            result[index] = values[index];
        }
        return result;
    }

    static ushort[] Normalize(ushort[] values, int precision)
    {
        uint maximum = (1U << precision) - 1U;
        ushort[] result = new ushort[values.Length];
        for (int index = 0; index < values.Length; index++)
        {
            result[index] = (ushort)(((uint)values[index] * ushort.MaxValue + maximum / 2U) / maximum);
        }
        return result;
    }
}
