using raw;

namespace raw.Tests;

public class NefVariantDecoderTests
{
    [TestCase(1, 1, RawPhotometricInterpretation.BlackIsZero, 12)]
    [TestCase(2, 1, RawPhotometricInterpretation.LinearRaw, 14)]
    [TestCase(2, 3, RawPhotometricInterpretation.LinearRaw, 12)]
    [TestCase(3, 3, RawPhotometricInterpretation.Rgb, 14)]
    [TestCase(4, 3, RawPhotometricInterpretation.YCbCr, 12)]
    public void ExplicitNonCfaProfilesDecodeInterleavedStripsWithoutBayerSemantics(int kindValue, int channels, RawPhotometricInterpretation photometric, int precision)
    {
        NefVariantKind kind = (NefVariantKind)kindValue;
        int width = 3;
        int height = 2;
        ushort[] expected = Enumerable.Range(0, width * height * channels).Select(index => (ushort)(50 + index * 31)).ToArray();
        NefVariantFixture fixture = CreateLinearFixture(kind, photometric, width, height, precision, channels, expected);
        fixture.Options.PreserveStoredSamples = true;

        RawSensorImage image = fixture.DecodeDocument();

        Assert.Multiple(() =>
        {
            Assert.That(image.Info.Photometric, Is.EqualTo(photometric));
            Assert.That(image.Info.Cfa, Is.Null);
            Assert.That(image.Info.GetChannelCount(), Is.EqualTo(channels));
            Assert.That(image.RowStrideSamples, Is.EqualTo(width * channels));
            Assert.That((image.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedSamples, Is.EqualTo(expected.Select(value => (uint)value)));
            Assert.That(image.IntegerSamples, Is.EqualTo(Normalize(expected, precision)));
            Assert.That(image.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void SmallNefUnpacksAndInterpolatesTwelveBitYCbCrPairs()
    {
        ushort[] packedComponents = new ushort[]
        {
            100, 200, 1000, 3000,
            300, 400, 2000, 1000
        };
        byte[] source = PackSmall(packedComponents);
        NefVariantFixture fixture = CreateSmallFixture(source, 4, 1);

        RawSensorImage image = fixture.DecodeDocument();

        ushort[] expected = new ushort[]
        {
            100, 1000, 3000,
            200, 1500, 2000,
            300, 2000, 1000,
            400, 2000, 1000
        };
        Assert.Multiple(() =>
        {
            Assert.That(image.Info.Photometric, Is.EqualTo(RawPhotometricInterpretation.YCbCr));
            Assert.That(image.Info.Cfa, Is.Null);
            Assert.That(image.Info.Channels.Select(channel => channel.Name), Is.EqualTo(new string[] { "Y", "Cb", "Cr" }));
            Assert.That(image.IntegerSamples, Is.EqualTo(Normalize(expected, 12)));
            Assert.That(image.StoredImage, Is.Null);
            Assert.That(image.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void OlderLinearProfileCanCarryPackedBitsAcrossRowBoundaries()
    {
        int width = 3;
        int height = 2;
        int channels = 3;
        int precision = 14;
        ushort[] expected = Enumerable.Range(0, width * height * channels).Select(index => (ushort)(700 + index * 101)).ToArray();
        NefVariantFixture fixture = CreateLinearFixture(NefVariantKind.Rgb, RawPhotometricInterpretation.Rgb, width, height, precision, channels, expected);
        uint[] values = expected.Select(value => (uint)value).ToArray();
        byte[] continuous = new byte[BitPacking.GetPackedByteCount(values.Length, precision)];
        BitPacking.PackUnsigned(values, precision, RawBitOrder.MostSignificantBitFirst, continuous);
        fixture.Source = continuous;
        fixture.Graph.ContainerLength = continuous.Length;
        fixture.Descriptor.RowsPerStrip = (ulong)height;
        fixture.Descriptor.Blocks = new TiffDataBlock[] { new TiffDataBlock(0, (ulong)continuous.Length, false, 0, 0, 0) };
        fixture.Profile.Codec.Quirks = new string[] { NefVariantDecoder.RgbQuirk, NefUncompressedDecoder.PackedMostSignificantBitFirstQuirk, NefUncompressedDecoder.BlockBitstreamQuirk };

        RawSensorImage image = fixture.Decode();

        Assert.That(image.IntegerSamples, Is.EqualTo(Normalize(expected, precision)));
    }

    [Test]
    public void VariantDecoderRejectsImplicitConflictingAndMalformedProfiles()
    {
        ushort[] values = new ushort[] { 1, 2, 3, 4, 5, 6 };
        NefVariantFixture implicitProfile = CreateLinearFixture(NefVariantKind.Rgb, RawPhotometricInterpretation.Rgb, 2, 1, 12, 3, values);
        implicitProfile.Profile.Codec.Name = NefUncompressedDecoder.CodecName;
        NefVariantFixture mismatch = CreateLinearFixture(NefVariantKind.Rgb, RawPhotometricInterpretation.YCbCr, 2, 1, 12, 3, values);
        NefVariantFixture shortSmall = CreateSmallFixture(new byte[11], 4, 1);
        NefVariantFixture preserveSmall = CreateSmallFixture(PackSmall(new ushort[] { 1, 2, 3, 4 }), 2, 1);
        preserveSmall.Options.PreserveStoredSamples = true;

        RawFormatException? implicitError = Assert.Throws<RawFormatException>(() => implicitProfile.Decode());
        RawProfileException? mismatchError = Assert.Throws<RawProfileException>(() => mismatch.Decode());
        RawFormatException? lengthError = Assert.Throws<RawFormatException>(() => shortSmall.Decode());
        RawUnsupportedException? preserveError = Assert.Throws<RawUnsupportedException>(() => preserveSmall.Decode());

        Assert.Multiple(() =>
        {
            Assert.That((implicitError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((mismatchError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidProfile));
            Assert.That((lengthError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That((preserveError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedFormat));
        });
    }

    [Test]
    public void NefRecognitionIncludesHighBitNonCfaRawButExcludesReducedAndEightBitImages()
    {
        TiffImageDescriptor highBit = RecognitionDescriptor(RawPhotometricInterpretation.Rgb, 3, 14, false);
        TiffImageDescriptor reduced = RecognitionDescriptor(RawPhotometricInterpretation.Rgb, 3, 14, true);
        TiffImageDescriptor eightBit = RecognitionDescriptor(RawPhotometricInterpretation.YCbCr, 3, 8, false);
        TiffImageDescriptor monochrome = RecognitionDescriptor(RawPhotometricInterpretation.BlackIsZero, 1, 12, false);

        Assert.Multiple(() =>
        {
            Assert.That(NefParser.IsRawImage(highBit), Is.True);
            Assert.That(NefParser.IsRawImage(monochrome), Is.True);
            Assert.That(NefParser.IsRawImage(reduced), Is.False);
            Assert.That(NefParser.IsRawImage(eightBit), Is.False);
        });
    }

    internal static NefVariantFixture CreateLinearFixture(NefVariantKind kind, RawPhotometricInterpretation photometric, int width, int height, int precision, int channels, ushort[] values)
    {
        int rowSamples = width * channels;
        int rowBytes = BitPacking.GetPackedByteCount(rowSamples, precision);
        byte[] source = new byte[rowBytes * height];
        for (int row = 0; row < height; row++)
        {
            uint[] samples = values.Skip(row * rowSamples).Take(rowSamples).Select(value => (uint)value).ToArray();
            BitPacking.PackUnsigned(samples, precision, RawBitOrder.MostSignificantBitFirst, source.AsSpan(row * rowBytes, rowBytes));
        }
        TiffDataBlock[] blocks = new TiffDataBlock[height];
        for (int index = 0; index < height; index++)
        {
            blocks[index] = new TiffDataBlock((ulong)(index * rowBytes), (ulong)rowBytes, false, index, 0, index);
        }
        string quirk = kind == NefVariantKind.Monochrome ? NefVariantDecoder.MonochromeQuirk : kind == NefVariantKind.Linear ? NefVariantDecoder.LinearQuirk : kind == NefVariantKind.Rgb ? NefVariantDecoder.RgbQuirk : NefVariantDecoder.YCbCrQuirk;
        return CreateFixture(source, width, height, precision, channels, photometric, NefVariantDecoder.LinearCodecName, new string[] { quirk, NefUncompressedDecoder.PackedMostSignificantBitFirstQuirk }, blocks, 1);
    }

    internal static NefVariantFixture CreateSmallFixture(byte[] source, int width, int height)
    {
        TiffDataBlock block = new TiffDataBlock(0, (ulong)source.Length, false, 0, 0, 0);
        return CreateFixture(source, width, height, 12, 3, RawPhotometricInterpretation.YCbCr, NefVariantDecoder.SmallYCbCrCodecName, Array.Empty<string>(), new TiffDataBlock[] { block }, height);
    }

    static NefVariantFixture CreateFixture(byte[] source, int width, int height, int precision, int channels, RawPhotometricInterpretation photometric, string codecName, string[] quirks, TiffDataBlock[] blocks, int rowsPerStrip)
    {
        TiffIfd ifd = new TiffIfd("IFD0/SubIFD0", 8, 8, 1, Array.Empty<TiffEntry>(), 0);
        TiffImageDescriptor descriptor = new TiffImageDescriptor(ifd.Path, ifd, 0);
        descriptor.Width = (ulong)width;
        descriptor.Height = (ulong)height;
        descriptor.SamplesPerPixel = (ulong)channels;
        descriptor.BitsPerSample = Enumerable.Repeat((ulong)precision, channels).ToArray();
        descriptor.CompressionCode = 1;
        descriptor.Compression = RawCompression.None;
        descriptor.Photometric = photometric;
        descriptor.FillOrder = 1;
        descriptor.PlanarConfiguration = TiffPlanarConfiguration.Chunky;
        descriptor.Predictor = TiffPredictor.None;
        descriptor.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        descriptor.RowsPerStrip = (ulong)rowsPerStrip;
        descriptor.Blocks = blocks;
        descriptor.StorageKind = TiffStorageKind.Strips;
        TiffHeader header = new TiffHeader(RawByteOrder.LittleEndian, false, 42, 4, 8, 0, 8);
        TiffIfdGraph graph = new TiffIfdGraph(header, new TiffIfd[] { ifd }, Array.Empty<TiffIfdLink>());
        graph.Images = new TiffImageDescriptor[] { descriptor };
        graph.ContainerLength = source.Length;
        RawReadProfile profile = new RawReadProfile();
        profile.Id = "test.nikon.variant";
        profile.Identity.Format = RawFormat.Nef;
        profile.Fingerprint.BitDepth = precision;
        profile.Fingerprint.SamplesPerPixel = channels;
        profile.Geometry.StoredWidth = width;
        profile.Geometry.StoredHeight = height;
        profile.Geometry.Cfa = null;
        profile.Codec.Name = codecName;
        profile.Codec.Compression = 1;
        profile.Codec.Packing = RawPacking.PackedBits;
        profile.Codec.StoredWordBits = precision;
        profile.Codec.Predictor = 0;
        profile.Codec.PlanarConfiguration = 1;
        profile.Codec.RowAlignment = 1;
        profile.Codec.Quirks = quirks;
        RawNikonNefMetadata metadata = new RawNikonNefMetadata();
        metadata.ProfileId = profile.Id;
        metadata.StoredWidth = width;
        metadata.StoredHeight = height;
        metadata.Compression = RawCompression.None;
        metadata.Cfa = null;
        metadata.BlackLevels = new double[] { 0 };
        metadata.WhiteLevels = new double[] { (1 << precision) - 1 };
        RawArea stored = new RawArea(RawAreaKind.StoredImage, 0, 0, width, height);
        RawArea active = new RawArea(RawAreaKind.Active, 0, 0, width, height);
        RawArea crop = new RawArea(RawAreaKind.DefaultCrop, 0, 0, width, height);
        RawChannelDescription[] initialChannels = Enumerable.Range(0, channels).Select(index => new RawChannelDescription("C" + index, RawChannelKind.Custom)).ToArray();
        RawPlaneInfo[] initialPlanes = Enumerable.Range(0, channels).Select(index => new RawPlaneInfo(index, "C" + index, RawChannelKind.Custom, width, height, precision, width)).ToArray();
        RawImageInfo info = new RawImageInfo(0, RawImageKind.Raw, width, height, precision, RawSampleKind.UnsignedInteger, photometric, RawCompression.None, RawOrientation.Normal, initialChannels, initialPlanes, null, stored, active, crop, null);
        NefVariantDecoder.ApplyToImage(info, profile);
        return new NefVariantFixture(source, graph, descriptor, info, metadata, profile);
    }

    internal static byte[] PackSmall(ushort[] components)
    {
        byte[] bytes = new byte[components.Length / 4 * 6];
        for (int index = 0; index < components.Length / 4; index++)
        {
            ushort first = components[index * 4];
            ushort second = components[index * 4 + 1];
            ushort cb = components[index * 4 + 2];
            ushort cr = components[index * 4 + 3];
            int offset = index * 6;
            bytes[offset] = (byte)first;
            bytes[offset + 1] = (byte)(first >> 8 | (second & 15) << 4);
            bytes[offset + 2] = (byte)(second >> 4);
            bytes[offset + 3] = (byte)cb;
            bytes[offset + 4] = (byte)(cb >> 8 | (cr & 15) << 4);
            bytes[offset + 5] = (byte)(cr >> 4);
        }
        return bytes;
    }

    static ushort[] Normalize(ushort[] values, int precision)
    {
        uint maximum = (1U << precision) - 1U;
        return values.Select(value => (ushort)(((uint)value * ushort.MaxValue + maximum / 2U) / maximum)).ToArray();
    }

    static TiffImageDescriptor RecognitionDescriptor(RawPhotometricInterpretation photometric, int channels, int precision, bool reduced)
    {
        TiffIfd ifd = new TiffIfd("IFD0/SubIFD0", 8, 8, 1, Array.Empty<TiffEntry>(), 0);
        TiffImageDescriptor descriptor = new TiffImageDescriptor(ifd.Path, ifd, 0);
        descriptor.Width = 2;
        descriptor.Height = 2;
        descriptor.SamplesPerPixel = (ulong)channels;
        descriptor.BitsPerSample = new ulong[] { (ulong)precision };
        descriptor.Photometric = photometric;
        descriptor.ReducedResolution = reduced;
        descriptor.Blocks = new TiffDataBlock[] { new TiffDataBlock(0, 1, false, 0, 0, 0) };
        return descriptor;
    }
}

internal class NefVariantFixture
{
    internal byte[] Source;
    internal TiffIfdGraph Graph;
    internal TiffImageDescriptor Descriptor;
    internal RawImageInfo Info;
    internal RawNikonNefMetadata Metadata;
    internal RawReadProfile Profile;
    internal RawDecodeOptions Options;

    internal NefVariantFixture(byte[] source, TiffIfdGraph graph, TiffImageDescriptor descriptor, RawImageInfo info, RawNikonNefMetadata metadata, RawReadProfile profile)
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
        return NefVariantDecoder.Decode(Source, Graph, Descriptor, Info, Metadata, Profile, Options);
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
