using raw;

namespace raw.Tests;

public class Cr2DecodeCorpusTests
{
    [TestCase(12, RawByteOrder.LittleEndian, "6")]
    [TestCase(12, RawByteOrder.BigEndian, "2,4")]
    [TestCase(14, RawByteOrder.LittleEndian, "1,2,3")]
    [TestCase(14, RawByteOrder.BigEndian, "3,3")]
    public void ClassicCorpusDecodesBitDepthByteOrderAndSliceLayout(int precision, RawByteOrder byteOrder, string sliceText)
    {
        int[] slices = ParseSlices(sliceText);
        uint[] expected = Sequence(100, 12);
        RawDocument document = CreateClassicDocument(precision, byteOrder, slices, expected);
        RawDecodeOptions options = new RawDecodeOptions();
        options.PreserveStoredSamples = true;

        RawSensorImage decoded = document.Decode(options);

        Assert.Multiple(() =>
        {
            Assert.That(((document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).ByteOrder, Is.EqualTo(byteOrder));
            Assert.That(decoded.Info.BitDepth, Is.EqualTo(precision));
            Assert.That((decoded.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedSamples, Is.EqualTo(expected));
            Assert.That(decoded.IntegerSamples[0], Is.EqualTo(Normalize(expected[0], precision)));
            Assert.That(decoded.IntegerSamples[decoded.IntegerSamples.Length - 1], Is.EqualTo(Normalize(expected[expected.Length - 1], precision)));
            Assert.That(decoded.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void EarlyFormatCorpusDecodesDimensionlessCr2AndHalfHeightTiff()
    {
        uint[] expected = Sequence(400, 8);
        RawDocument dimensionless = CreateEarlyDocument(false, expected);
        RawDocument halfHeight = CreateEarlyDocument(true, expected);
        RawDecodeOptions options = new RawDecodeOptions();
        options.PreserveStoredSamples = true;

        RawSensorImage first = dimensionless.Decode(options);
        RawSensorImage second = halfHeight.Decode(options);

        Assert.Multiple(() =>
        {
            Assert.That(dimensionless.Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(dimensionless.ProfileName, Is.EqualTo(CanonCr2MetadataParser.Eos20dProfile));
            Assert.That(((dimensionless.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Canon ?? throw new System.InvalidOperationException("A required value is unavailable.")).LosslessJpegCodedWidth, Is.EqualTo(2));
            Assert.That((first.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedSamples, Is.EqualTo(expected));
            Assert.That(halfHeight.Format, Is.EqualTo(RawFormat.Tiff));
            Assert.That(halfHeight.ProfileName, Is.EqualTo(CanonCr2MetadataParser.Eos1dProfile));
            Assert.That(((halfHeight.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Canon ?? throw new System.InvalidOperationException("A required value is unavailable.")).HalfHeightDoubleWidth, Is.True);
            Assert.That((halfHeight.FormatDetails.Canon ?? throw new System.InvalidOperationException("A required value is unavailable.")).LosslessJpegCodedHeight, Is.EqualTo(1));
            Assert.That((second.StoredImage ?? throw new System.InvalidOperationException("A required value is unavailable.")).UnsignedSamples, Is.EqualTo(expected));
        });
    }

    [TestCase(false, 0)]
    [TestCase(true, 0)]
    [TestCase(false, 1)]
    [TestCase(true, 1)]
    [TestCase(false, 2)]
    [TestCase(true, 2)]
    public void SubsampledCorpusDecodesSRawAndMRawAcrossGenerations(bool mraw, int generation)
    {
        SubsampledCorpusFixture fixture = CreateSubsampledDocument(mraw, generation);

        RawSensorImage decoded = fixture.Document.Decode(new RawDecodeOptions());
        ushort[] packed = CanonSRawDecoder.DecodePacked(fixture.Jpeg, (fixture.Document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Canon, new RawLimits(), CancellationToken.None);
        ushort[] expected = CanonSRawDecoder.Reconstruct(packed, fixture.Document.FormatDetails.Canon, new RawLimits(), CancellationToken.None);
        RawRenderOptions renderOptions = new RawRenderOptions();
        renderOptions.DemosaicMode = RawDemosaicMode.None;
        renderOptions.WhiteBalanceMode = RawWhiteBalanceMode.None;
        renderOptions.ColorSpace = RawColorSpace.RawCamera;
        renderOptions.TransferFunction = RawTransferFunction.Linear;
        renderOptions.OutputBitDepth = 32;
        renderOptions.ApplyOrientation = false;
        RawRgbImage rendered = new RawCodecService().Render(fixture.Document, renderOptions);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Document.ProfileName, Is.EqualTo(mraw ? CanonCr2MetadataParser.Eos6DMarkIiMRawProfile : CanonCr2MetadataParser.Eos6DMarkIiSRawProfile));
            Assert.That((fixture.Document.FormatDetails.Canon ?? throw new System.InvalidOperationException("A required value is unavailable.")).SRawGeneration, Is.EqualTo(generation));
            Assert.That(fixture.Document.FormatDetails.Canon.SubsamplingVertical, Is.EqualTo(mraw ? 2 : 1));
            Assert.That(decoded.IntegerSamples, Is.EqualTo(expected));
            Assert.That(decoded.Info.Photometric, Is.EqualTo(RawPhotometricInterpretation.Rgb));
            Assert.That(decoded.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(rendered.FloatingSamples[0], Is.EqualTo(decoded.IntegerSamples[0] / 65535.0F).Within(0.000001F));
            Assert.That(rendered.ChannelCount, Is.EqualTo(3));
        });
    }

    [TestCase(RawByteOrder.LittleEndian)]
    [TestCase(RawByteOrder.BigEndian)]
    public void ContainerCorpusRetainsEveryPreviewAndGatesUnknownProfiles(RawByteOrder byteOrder)
    {
        UnknownCr2CorpusFixture fixture = BuildUnknownCr2(byteOrder);
        RawOpenOptions options = new RawOpenOptions();
        options.ReadPreviews = true;

        RawDocument document = RawCodec.Open(fixture.Bytes, null, options);
        RawUnsupportedException? exception = Assert.Throws<RawUnsupportedException>(() => document.Decode(new RawDecodeOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(((document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).ByteOrder, Is.EqualTo(byteOrder));
            Assert.That(document.ProfileName, Is.Empty);
            Assert.That(document.Capabilities.Recognized, Is.True);
            Assert.That(document.Capabilities.Inspected, Is.True);
            Assert.That(document.Capabilities.Decoded, Is.False);
            Assert.That(document.FormatDetails.Canon, Is.Null);
            Assert.That(document.Thumbnails.Length, Is.EqualTo(3));
            Assert.That(document.Thumbnails.Any(item => item.EncodedBytes.SequenceEqual(fixture.FirstPreview)), Is.True);
            Assert.That(document.Thumbnails.Any(item => item.EncodedBytes.SequenceEqual(fixture.SecondPreview)), Is.True);
            Assert.That(document.Thumbnails.Any(item => item.EncodedBytes.SequenceEqual(fixture.JpegPreview)), Is.True);
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedCodec));
        });
    }

    [Test]
    public void TruncationCorpusRejectsIncompleteContainerAndLosslessPayload()
    {
        UnknownCr2CorpusFixture container = BuildUnknownCr2(RawByteOrder.LittleEndian);
        byte[] shortContainer = container.Bytes.AsSpan(0, 15).ToArray();
        uint[] expected = Sequence(800, 12);
        RawDocument complete = CreateClassicDocument(14, RawByteOrder.LittleEndian, new int[] { 2, 4 }, expected);
        byte[] shortPayload = complete.ExtractPayload((complete.Images ?? throw new System.InvalidOperationException("A required value is unavailable."))[0].Payload);
        System.Array.Resize(ref shortPayload, shortPayload.Length - 2);
        RawDocument payload = CreateClassicDocumentFromEncoded(14, RawByteOrder.LittleEndian, new int[] { 2, 4 }, shortPayload);

        RawFormatException? header = Assert.Throws<RawFormatException>(() => RawCodec.Open(shortContainer, null, new RawOpenOptions()));
        RawFormatException? image = Assert.Throws<RawFormatException>(() => payload.Decode(new RawDecodeOptions()));

        Assert.Multiple(() =>
        {
            Assert.That((header ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidOffset));
            Assert.That((image ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
        });
    }

    private static RawDocument CreateClassicDocument(int precision, RawByteOrder byteOrder, int[] slices, uint[] expected)
    {
        ushort[] sliced = Slice(expected, 6, 2, slices);
        LosslessJpegImage jpeg = ClassicJpeg(3, 2, precision, sliced);
        byte[] encoded = LosslessJpegEncoder.Encode(jpeg, new LosslessJpegEncodeParameters(1, 0, 0, true, true), new RawLimits(), CancellationToken.None);
        return CreateClassicDocumentFromEncoded(precision, byteOrder, slices, encoded);
    }

    private static RawDocument CreateClassicDocumentFromEncoded(int precision, RawByteOrder byteOrder, int[] slices, byte[] encoded)
    {
        string profile = precision == 12 ? CanonCr2MetadataParser.Eos20dProfile : CanonCr2MetadataParser.Eos6DMarkIiProfile;
        RawPayloadBlock payload = new RawPayloadBlock(RawPayloadKind.RawImage, "IFD3/Strips", 0, encoded.Length, 0, 0, 0, RawCompression.LosslessJpeg, true, false);
        RawImageInfo info = BayerInfo(0, RawImageKind.Raw, 6, 2, precision, payload, RawCfaLayout.Rggb);
        RawDocument document = new RawDocument(RawFormat.Cr2, profile, null, new RawImageInfo[] { info }, null,
            new RawPayloadMap(encoded.Length, new RawPayloadBlock[] { payload }), new RawCapabilities(true, true, true, false, false, false, false), null, true, 4, false, true);
        RawCanonCr2Metadata metadata = CanonMetadata(profile, info, slices, 3, 2, 2);
        document.AttachCanonCr2Metadata(metadata);
        document.AttachCr2Header(new RawCr2Header(byteOrder, 2, 0, 16, 128, "IFD3"));
        document.AttachSource(encoded, false, null);
        return document;
    }

    private static RawDocument CreateEarlyDocument(bool halfHeight, uint[] expected)
    {
        int codedWidth = halfHeight ? 4 : 2;
        int codedHeight = halfHeight ? 1 : 2;
        ushort[] flattened = new ushort[expected.Length];
        int index = 0;
        while (index < expected.Length)
        {
            flattened[index] = (ushort)expected[index];
            index++;
        }
        LosslessJpegImage jpeg = ClassicJpeg(codedWidth, codedHeight, 12, flattened);
        byte[] encoded = LosslessJpegEncoder.Encode(jpeg, new LosslessJpegEncodeParameters(1, 0, 0, true, true), new RawLimits(), CancellationToken.None);
        string profile = halfHeight ? CanonCr2MetadataParser.Eos1dProfile : CanonCr2MetadataParser.Eos20dProfile;
        RawFormat format = halfHeight ? RawFormat.Tiff : RawFormat.Cr2;
        RawPayloadBlock payload = new RawPayloadBlock(RawPayloadKind.RawImage, halfHeight ? "CanonMakerNote/RawDataOffset" : "IFD3/Strips", 0, encoded.Length, 0, 0, 0, RawCompression.LosslessJpeg, true, true);
        RawImageInfo info = BayerInfo(0, RawImageKind.Raw, 4, 2, 12, payload, halfHeight ? RawCfaLayout.Grbg : RawCfaLayout.Rggb);
        RawDocument document = new RawDocument(format, profile, null, new RawImageInfo[] { info }, null,
            new RawPayloadMap(encoded.Length, new RawPayloadBlock[] { payload }), new RawCapabilities(true, true, true, false, false, false, false), null, true, 1, false, true);
        RawCanonCr2Metadata metadata = CanonMetadata(profile, info, new int[] { 4 }, codedWidth, codedHeight, 2);
        metadata.HalfHeightDoubleWidth = halfHeight;
        metadata.RawDataOffset = 0;
        document.AttachCanonCr2Metadata(metadata);
        document.AttachSource(encoded, false, null);
        return document;
    }

    private static SubsampledCorpusFixture CreateSubsampledDocument(bool mraw, int generation)
    {
        int width = 4;
        int height = mraw ? 4 : 2;
        int vertical = mraw ? 2 : 1;
        int chromaHeight = height / vertical;
        LosslessJpegImage jpeg = new LosslessJpegImage();
        jpeg.Width = width;
        jpeg.Height = height;
        jpeg.Precision = 15;
        jpeg.Planes.Add(Plane(1, width, height, 2, vertical, Sequence16(1000, width * height)));
        jpeg.Planes.Add(Plane(2, width / 2, chromaHeight, 1, 1, Sequence16(16384, width / 2 * chromaHeight)));
        jpeg.Planes.Add(Plane(3, width / 2, chromaHeight, 1, 1, Sequence16(16000, width / 2 * chromaHeight)));
        byte[] encoded = LosslessJpegEncoder.Encode(jpeg, new LosslessJpegEncodeParameters(1, 0, 0, true, true), new RawLimits(), CancellationToken.None);
        string profile = mraw ? CanonCr2MetadataParser.Eos6DMarkIiMRawProfile : CanonCr2MetadataParser.Eos6DMarkIiSRawProfile;
        RawPayloadBlock payload = new RawPayloadBlock(RawPayloadKind.RawImage, "IFD3/Strips", 0, encoded.Length, 0, 0, 0, RawCompression.LosslessJpeg, true, false);
        RawImageInfo info = RgbInfo(width, height, payload);
        RawDocument document = new RawDocument(RawFormat.Cr2, profile, null, new RawImageInfo[] { info }, null,
            new RawPayloadMap(encoded.Length, new RawPayloadBlock[] { payload }), new RawCapabilities(true, true, true, true, false, false, false), null, true, 4, false, true);
        RawCanonCr2Metadata metadata = new RawCanonCr2Metadata();
        metadata.ProfileId = profile;
        metadata.IsSubsampled = true;
        metadata.SensorWidth = width;
        metadata.SensorHeight = height;
        metadata.ActiveArea = info.ActiveArea;
        metadata.SubsamplingHorizontal = 2;
        metadata.SubsamplingVertical = vertical;
        metadata.SRawGeneration = generation;
        metadata.SRawHue = ((2 * vertical) - 1) >> 1;
        metadata.SRawCoefficients = new int[] { 512, 1024, 2048 };
        metadata.SliceWidths = new int[] { CanonSRawDecoder.GetPackedWidth(metadata) };
        metadata.WhiteLevels = new double[] { 32767 };
        document.AttachCanonCr2Metadata(metadata);
        document.AttachCr2Header(new RawCr2Header(RawByteOrder.LittleEndian, 2, 0, 16, 128, "IFD3"));
        document.AttachSource(encoded, false, null);
        return new SubsampledCorpusFixture(document, jpeg);
    }

    private static UnknownCr2CorpusFixture BuildUnknownCr2(RawByteOrder byteOrder)
    {
        SyntheticTiffBuilder builder = new SyntheticTiffBuilder(byteOrder, false);
        builder.SetHeaderPadding(8);
        SyntheticTiffIfd ifd0 = builder.AddIfd();
        SyntheticTiffIfd ifd1 = builder.AddIfd();
        SyntheticTiffIfd ifd2 = builder.AddIfd();
        SyntheticTiffIfd ifd3 = builder.AddIfd();
        builder.SetNextIfd(ifd0, ifd1);
        builder.SetNextIfd(ifd1, ifd2);
        builder.SetNextIfd(ifd2, ifd3);

        byte[] firstPreview = new byte[] { 11, 12, 13, 14, 15 };
        byte[] secondPreview = new byte[] { 21, 22, 23, 24, 25 };
        byte[] jpegPreview = CreateJpegPreview();
        ushort[] rawSamples = Slice(Sequence(50, 12), 6, 2, new int[] { 6 });
        LosslessJpegImage rawJpeg = ClassicJpeg(3, 2, 12, rawSamples);
        byte[] rawBytes = LosslessJpegEncoder.Encode(rawJpeg, new LosslessJpegEncodeParameters(1, 0, 0, true, true), new RawLimits(), CancellationToken.None);

        SyntheticTiffEntry firstOffset = AddImage(builder, ifd0, byteOrder, 1, 1, 1, 8, firstPreview, out SyntheticTiffEntry firstData);
        SyntheticTiffEntry jpegOffset = AddLong(builder, ifd1, byteOrder, 513, 0);
        AddLong(builder, ifd1, byteOrder, 514, (uint)jpegPreview.Length);
        SyntheticTiffEntry jpegData = builder.AddField(ifd1, 65001, TiffFieldType.Undefined, (ulong)jpegPreview.Length, jpegPreview);
        SyntheticTiffEntry secondOffset = AddImage(builder, ifd2, byteOrder, 1, 1, 1, 8, secondPreview, out SyntheticTiffEntry secondData);
        SyntheticTiffEntry rawOffset = AddImage(builder, ifd3, byteOrder, 6, 2, 6, 12, rawBytes, out SyntheticTiffEntry rawData);
        builder.AddField(ifd3, 50752, TiffFieldType.Short, 3, Words(byteOrder, new ushort[] { 0, 0, 6 }));

        byte[] bytes = builder.Build();
        bytes[8] = 0x43;
        bytes[9] = 0x52;
        bytes[10] = 2;
        bytes[11] = 0;
        EndianWriter.WriteUInt32(bytes.AsSpan(12, 4), (uint)ifd3.Offset, byteOrder);
        PatchLong(bytes, ifd0, firstOffset, (uint)firstData.ValueOffset, byteOrder);
        PatchLong(bytes, ifd1, jpegOffset, (uint)jpegData.ValueOffset, byteOrder);
        PatchLong(bytes, ifd2, secondOffset, (uint)secondData.ValueOffset, byteOrder);
        PatchLong(bytes, ifd3, rawOffset, (uint)rawData.ValueOffset, byteOrder);
        return new UnknownCr2CorpusFixture(bytes, firstPreview, secondPreview, jpegPreview);
    }

    private static RawCanonCr2Metadata CanonMetadata(string profile, RawImageInfo info, int[] slices, int codedWidth, int codedHeight, int components)
    {
        RawCanonCr2Metadata metadata = new RawCanonCr2Metadata();
        metadata.ProfileId = profile;
        metadata.SensorWidth = info.Width;
        metadata.SensorHeight = info.Height;
        metadata.ActiveArea = info.ActiveArea;
        metadata.Cfa = info.Cfa;
        metadata.BlackLevels = new double[] { 10, 11, 12, 13 };
        metadata.WhiteLevels = new double[] { (1 << info.BitDepth) - 1 };
        metadata.SliceWidths = slices;
        metadata.LosslessJpegCodedWidth = codedWidth;
        metadata.LosslessJpegCodedHeight = codedHeight;
        metadata.LosslessJpegComponentCount = components;
        return metadata;
    }

    private static RawImageInfo BayerInfo(int index, RawImageKind kind, int width, int height, int precision, RawPayloadBlock payload, RawCfaLayout layout)
    {
        RawCfaPattern cfa = RawCfaPattern.CreateBayer(layout);
        return new RawImageInfo(index, kind, width, height, precision, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.LosslessJpeg, RawOrientation.Normal,
            new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) },
            new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, width, height, precision, width) }, cfa,
            new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), new RawArea(RawAreaKind.Active, 0, 0, width, height), new RawArea(RawAreaKind.DefaultCrop, 0, 0, width, height), payload);
    }

    private static RawImageInfo RgbInfo(int width, int height, RawPayloadBlock payload)
    {
        return new RawImageInfo(0, RawImageKind.Raw, width, height, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.Rgb, RawCompression.LosslessJpeg, RawOrientation.Normal,
            new RawChannelDescription[] { new RawChannelDescription("Red", RawChannelKind.Red), new RawChannelDescription("Green", RawChannelKind.Green), new RawChannelDescription("Blue", RawChannelKind.Blue) },
            new RawPlaneInfo[]
            {
                new RawPlaneInfo(0, "Red", RawChannelKind.Red, width, height, 16, width),
                new RawPlaneInfo(1, "Green", RawChannelKind.Green, width, height, 16, width),
                new RawPlaneInfo(2, "Blue", RawChannelKind.Blue, width, height, 16, width)
            }, null,
            new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), new RawArea(RawAreaKind.Active, 0, 0, width, height), new RawArea(RawAreaKind.DefaultCrop, 0, 0, width, height), payload);
    }

    private static LosslessJpegImage ClassicJpeg(int width, int height, int precision, ushort[] flattened)
    {
        LosslessJpegImage image = new LosslessJpegImage();
        image.Width = width;
        image.Height = height;
        image.Precision = precision;
        ushort[] first = new ushort[width * height];
        ushort[] second = new ushort[width * height];
        int index = 0;
        while (index < first.Length)
        {
            first[index] = flattened[index * 2];
            second[index] = flattened[index * 2 + 1];
            index++;
        }
        image.Planes.Add(Plane(1, width, height, 1, 1, first));
        image.Planes.Add(Plane(2, width, height, 1, 1, second));
        return image;
    }

    private static LosslessJpegPlane Plane(byte identifier, int width, int height, int horizontal, int vertical, ushort[] samples)
    {
        LosslessJpegPlane plane = new LosslessJpegPlane();
        plane.Identifier = identifier;
        plane.Width = width;
        plane.Height = height;
        plane.HorizontalSampling = horizontal;
        plane.VerticalSampling = vertical;
        plane.Predictor = 1;
        plane.PointTransform = 0;
        plane.Samples = samples;
        return plane;
    }

    private static ushort[] Slice(uint[] expected, int width, int height, int[] slices)
    {
        ushort[] result = new ushort[expected.Length];
        int destination = 0;
        int sliceX = 0;
        int sliceIndex = 0;
        while (sliceIndex < slices.Length)
        {
            int row = 0;
            while (row < height)
            {
                int x = 0;
                while (x < slices[sliceIndex])
                {
                    result[destination] = (ushort)expected[row * width + sliceX + x];
                    destination++;
                    x++;
                }
                row++;
            }
            sliceX += slices[sliceIndex];
            sliceIndex++;
        }
        return result;
    }

    private static int[] ParseSlices(string value)
    {
        string[] parts = value.Split(',');
        int[] result = new int[parts.Length];
        int index = 0;
        while (index < parts.Length)
        {
            result[index] = int.Parse(parts[index], System.Globalization.CultureInfo.InvariantCulture);
            index++;
        }
        return result;
    }

    private static uint[] Sequence(int start, int count)
    {
        uint[] result = new uint[count];
        int index = 0;
        while (index < count)
        {
            result[index] = (uint)(start + index);
            index++;
        }
        return result;
    }

    private static ushort[] Sequence16(int start, int count)
    {
        ushort[] result = new ushort[count];
        int index = 0;
        while (index < count)
        {
            result[index] = (ushort)(start + index);
            index++;
        }
        return result;
    }

    private static ushort Normalize(uint value, int precision)
    {
        uint maximum = (1U << precision) - 1U;
        return (ushort)((value * ushort.MaxValue + maximum / 2U) / maximum);
    }

    private static SyntheticTiffEntry AddImage(SyntheticTiffBuilder builder, SyntheticTiffIfd ifd, RawByteOrder byteOrder, uint width, uint height, ushort compression, ushort bits, byte[] payload, out SyntheticTiffEntry data)
    {
        AddLong(builder, ifd, byteOrder, 256, width);
        AddLong(builder, ifd, byteOrder, 257, height);
        AddShort(builder, ifd, byteOrder, 258, bits);
        AddShort(builder, ifd, byteOrder, 259, compression);
        AddShort(builder, ifd, byteOrder, 262, compression == 6 ? (ushort)32803 : (ushort)1);
        SyntheticTiffEntry offset = AddLong(builder, ifd, byteOrder, 273, 0);
        AddShort(builder, ifd, byteOrder, 277, 1);
        AddLong(builder, ifd, byteOrder, 278, height);
        AddLong(builder, ifd, byteOrder, 279, (uint)payload.Length);
        data = builder.AddField(ifd, 65000, TiffFieldType.Undefined, (ulong)payload.Length, payload);
        return offset;
    }

    private static SyntheticTiffEntry AddLong(SyntheticTiffBuilder builder, SyntheticTiffIfd ifd, RawByteOrder byteOrder, ushort tag, uint value)
    {
        byte[] bytes = new byte[4];
        EndianWriter.WriteUInt32(bytes, value, byteOrder);
        return builder.AddField(ifd, tag, TiffFieldType.Long, 1, bytes);
    }

    private static void AddShort(SyntheticTiffBuilder builder, SyntheticTiffIfd ifd, RawByteOrder byteOrder, ushort tag, ushort value)
    {
        byte[] bytes = new byte[2];
        EndianWriter.WriteUInt16(bytes, value, byteOrder);
        builder.AddField(ifd, tag, TiffFieldType.Short, 1, bytes);
    }

    private static byte[] Words(RawByteOrder byteOrder, ushort[] values)
    {
        byte[] bytes = new byte[values.Length * 2];
        int index = 0;
        while (index < values.Length)
        {
            EndianWriter.WriteUInt16(bytes.AsSpan(index * 2, 2), values[index], byteOrder);
            index++;
        }
        return bytes;
    }

    private static void PatchLong(byte[] bytes, SyntheticTiffIfd ifd, SyntheticTiffEntry entry, uint value, RawByteOrder byteOrder)
    {
        int entryIndex = ifd.Entries.IndexOf(entry);
        int slotOffset = checked((int)ifd.Offset + 2 + entryIndex * 12 + 8);
        EndianWriter.WriteUInt32(bytes.AsSpan(slotOffset, 4), value, byteOrder);
    }

    private static byte[] CreateJpegPreview()
    {
        BaselineJpegImage image = new BaselineJpegImage();
        image.Width = 8;
        image.Height = 8;
        image.Precision = 8;
        BaselineJpegPlane plane = new BaselineJpegPlane();
        plane.Identifier = 1;
        plane.Width = 8;
        plane.Height = 8;
        plane.HorizontalSampling = 1;
        plane.VerticalSampling = 1;
        plane.Samples = new byte[64];
        int index = 0;
        while (index < plane.Samples.Length)
        {
            plane.Samples[index] = 128;
            index++;
        }
        image.Planes.Add(plane);
        return BaselineJpegEncoder.Encode(image, new BaselineJpegEncodeParameters(80, 1, 1, 0), new RawLimits(), CancellationToken.None);
    }
}

internal class SubsampledCorpusFixture
{
    internal RawDocument Document;
    internal LosslessJpegImage Jpeg;

    internal SubsampledCorpusFixture(RawDocument document, LosslessJpegImage jpeg)
    {
        Document = document;
        Jpeg = jpeg;
    }
}

internal class UnknownCr2CorpusFixture
{
    internal byte[] Bytes;
    internal byte[] FirstPreview;
    internal byte[] SecondPreview;
    internal byte[] JpegPreview;

    internal UnknownCr2CorpusFixture(byte[] bytes, byte[] firstPreview, byte[] secondPreview, byte[] jpegPreview)
    {
        Bytes = bytes;
        FirstPreview = firstPreview;
        SecondPreview = secondPreview;
        JpegPreview = jpegPreview;
    }
}
