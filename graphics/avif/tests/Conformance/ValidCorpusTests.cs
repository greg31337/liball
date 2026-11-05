using System.Buffers.Binary;
using System.Text;

namespace avif.Tests;

public class ValidCorpusTests
{
    [TestCase(0, 8, AvifChromaFormat.Monochrome, AvifRange.Full, 1, 1)]
    [TestCase(0, 8, AvifChromaFormat.Monochrome, AvifRange.Limited, 2, 2)]
    [TestCase(0, 8, AvifChromaFormat.Yuv420, AvifRange.Full, 7, 5)]
    [TestCase(0, 8, AvifChromaFormat.Yuv420, AvifRange.Limited, 8, 6)]
    [TestCase(2, 8, AvifChromaFormat.Yuv422, AvifRange.Full, 9, 3)]
    [TestCase(2, 8, AvifChromaFormat.Yuv422, AvifRange.Limited, 10, 4)]
    [TestCase(1, 8, AvifChromaFormat.Yuv444, AvifRange.Full, 3, 9)]
    [TestCase(1, 8, AvifChromaFormat.Yuv444, AvifRange.Limited, 4, 10)]
    [TestCase(0, 10, AvifChromaFormat.Monochrome, AvifRange.Full, 5, 7)]
    [TestCase(0, 10, AvifChromaFormat.Monochrome, AvifRange.Limited, 6, 8)]
    [TestCase(0, 10, AvifChromaFormat.Yuv420, AvifRange.Full, 11, 5)]
    [TestCase(0, 10, AvifChromaFormat.Yuv420, AvifRange.Limited, 12, 6)]
    [TestCase(2, 10, AvifChromaFormat.Yuv422, AvifRange.Full, 5, 11)]
    [TestCase(2, 10, AvifChromaFormat.Yuv422, AvifRange.Limited, 6, 12)]
    [TestCase(1, 10, AvifChromaFormat.Yuv444, AvifRange.Full, 13, 7)]
    [TestCase(1, 10, AvifChromaFormat.Yuv444, AvifRange.Limited, 14, 8)]
    [TestCase(2, 12, AvifChromaFormat.Monochrome, AvifRange.Full, 7, 13)]
    [TestCase(2, 12, AvifChromaFormat.Monochrome, AvifRange.Limited, 8, 14)]
    [TestCase(2, 12, AvifChromaFormat.Yuv420, AvifRange.Full, 15, 9)]
    [TestCase(2, 12, AvifChromaFormat.Yuv420, AvifRange.Limited, 16, 10)]
    [TestCase(2, 12, AvifChromaFormat.Yuv422, AvifRange.Full, 9, 15)]
    [TestCase(2, 12, AvifChromaFormat.Yuv422, AvifRange.Limited, 10, 16)]
    [TestCase(2, 12, AvifChromaFormat.Yuv444, AvifRange.Full, 17, 11)]
    [TestCase(2, 12, AvifChromaFormat.Yuv444, AvifRange.Limited, 18, 12)]
    public void StillCorpusRoundTripsEveryProfileDepthChromaRangeAndDimension(
        int expectedProfile,
        int bitDepth,
        AvifChromaFormat chromaFormat,
        AvifRange range,
        int width,
        int height)
    {
        AvifImage source = CreateImage(
            width,
            height,
            bitDepth,
            chromaFormat,
            range,
            1);
        AvifEncoderOptions options = CreateLosslessOptions();
        options.HeaderFormat = AvifHeaderFormat.Full;
        AvifWriter writer = AvifWriter.Create(options);
        uint itemId = writer.AddImage(source);
        AvifEncodeResult encoded = writer.Finish();
        AvifProbeResult probe = Avif.Probe(encoded.GetData());
        AvifValidationResult validation = Avif.Validate(encoded.GetData());
        AvifReader reader = AvifReader.Create(encoded.GetData());
        AvifResultCode parsed = reader.Parse();
        AvifImageItem? item = (reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        AvifItemProperty? configuration = FindProperty(item, "av1C");
        AvifDecodeResult decoded = reader.DecodePrimaryImage();

        Assert.Multiple(() =>
        {
            Assert.That(itemId, Is.EqualTo(1));
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(probe.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(validation.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(validation.GetFindingCount(), Is.Zero);
            Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("avif"), Is.True);
            Assert.That(((reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("mif1"), Is.True);
            Assert.That(((reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("miaf"), Is.True);
            Assert.That((configuration ?? throw new InvalidOperationException("Invalid internal state.")).GetAv1Profile(), Is.EqualTo(expectedProfile));
            Assert.That(configuration.GetBitDepth(), Is.EqualTo(bitDepth));
            Assert.That(configuration.GetChannelCount(),
                Is.EqualTo(chromaFormat == AvifChromaFormat.Monochrome ? 1 : 3));
            Assert.That((item ?? throw new InvalidOperationException("Invalid internal state.")).GetPixelWidth(), Is.EqualTo((uint)width));
            Assert.That(item.GetPixelHeight(), Is.EqualTo((uint)height));
            Assert.That(item.GetPixelBitDepth(), Is.EqualTo(bitDepth));
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetChromaFormat(), Is.EqualTo(chromaFormat));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetRange(), Is.EqualTo(range));
            Assert.That(((reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("MA1B"),
                Is.EqualTo(expectedProfile == 0));
            Assert.That(((reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("MA1A"),
                Is.EqualTo(expectedProfile == 1));
        });
        AssertImagesEqual(source, decoded.GetImage());

        (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
        writer.Close();
        source.Close();
    }

    [Test]
    public void HeaderAndSequenceCorpusCoversMinimizedAndTimedBrands()
    {
        AvifImage still = CreateImage(
            5, 3, 8, AvifChromaFormat.Monochrome, AvifRange.Full, 2);
        AvifEncoderOptions minimizedOptions = CreateLosslessOptions();
        minimizedOptions.HeaderFormat = AvifHeaderFormat.Minimized;
        AvifEncodeResult minimized = Avif.Encode(still, minimizedOptions);
        AvifReadResult minimizedRead = Avif.Read(minimized.GetData());
        AvifDecodeResult minimizedDecode = Avif.Decode(minimized.GetData());

        AvifImage first = CreateImage(
            5, 3, 8, AvifChromaFormat.Monochrome, AvifRange.Full, 3);
        AvifImage second = CreateImage(
            5, 3, 8, AvifChromaFormat.Monochrome, AvifRange.Full, 4);
        AvifSequence sequence = AvifSequence.Create(1000);
        sequence.AddFrame(AvifFrame.Create(first, 400));
        sequence.AddFrame(AvifFrame.Create(second, 600));
        AvifWriter sequenceWriter = AvifWriter.Create(CreateLosslessOptions());
        uint trackId = sequenceWriter.AddSequence(sequence);
        AvifEncodeResult timed = sequenceWriter.Finish();
        AvifValidationResult timedValidation = Avif.Validate(timed.GetData());
        AvifReader timedReader = AvifReader.Create(timed.GetData());
        AvifResultCode timedParsed = timedReader.Parse();
        AvifDecodeResult firstDecoded = timedReader.DecodeFrame(0);
        AvifDecodeResult secondDecoded = timedReader.DecodeFrame(1);
        AvifFileInfo? timedInfo = (timedReader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo();

        Assert.Multiple(() =>
        {
            Assert.That(minimized.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(minimizedRead.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(((minimizedRead.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).GetMajorBrand(),
                Is.EqualTo("mif3"));
            Assert.That(minimizedDecode.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(trackId, Is.EqualTo(1));
            Assert.That(timed.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(timedValidation.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(timedParsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That((timedInfo ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("avis"), Is.True);
            Assert.That(timedInfo.HasBrand("avio"), Is.True);
            Assert.That(timedInfo.HasBrand("msf1"), Is.True);
            Assert.That(timedInfo.HasBrand("miaf"), Is.True);
            Assert.That(firstDecoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondDecoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
        });
        AssertImagesEqual(still, minimizedDecode.GetImage());
        AssertImagesEqual(first, firstDecoded.GetImage());
        AssertImagesEqual(second, secondDecoded.GetImage());

        (minimizedDecode.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (firstDecoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (secondDecoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        sequenceWriter.Close();
        timedReader.Close();
        still.Close();
        first.Close();
        second.Close();
    }

    [Test]
    public void ExplicitTileCorpusRoundTripsTwoByTwoAv1Tiles()
    {
        AvifImage source = CreateImage(
            128, 128, 10, AvifChromaFormat.Yuv420, AvifRange.Limited, 5);
        AvifEncoderOptions options = CreateLosslessOptions();
        options.AutoTiling = false;
        options.TileRowsLog2 = 1;
        options.TileColumnsLog2 = 1;
        options.WorkerCount = 1;
        AvifEncodeResult encoded = Avif.Encode(source, options);
        AvifValidationResult validation = Avif.Validate(encoded.GetData());
        AvifDecodeResult decoded = Avif.Decode(encoded.GetData());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(validation.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(128));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight(), Is.EqualTo(128));
        });
        AssertImagesEqual(source, decoded.GetImage());

        (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        source.Close();
    }

    [Test]
    public void DerivedImageCorpusRoundTripsOddGridAndProgressiveLayers()
    {
        AvifImage topLeft = CreateImage(
            3, 2, 8, AvifChromaFormat.Monochrome, AvifRange.Full, 6);
        AvifImage topRight = CreateImage(
            3, 2, 8, AvifChromaFormat.Monochrome, AvifRange.Full, 7);
        AvifImage bottomLeft = CreateImage(
            3, 2, 8, AvifChromaFormat.Monochrome, AvifRange.Full, 8);
        AvifImage bottomRight = CreateImage(
            3, 2, 8, AvifChromaFormat.Monochrome, AvifRange.Full, 9);
        AvifGrid grid = AvifGrid.Create(2, 2, 5, 3);
        grid.SetCell(0, topLeft);
        grid.SetCell(1, topRight);
        grid.SetCell(2, bottomLeft);
        grid.SetCell(3, bottomRight);
        AvifWriter gridWriter = AvifWriter.Create(CreateLosslessOptions());
        uint gridId = gridWriter.AddGrid(grid);
        AvifEncodeResult gridData = gridWriter.Finish();
        AvifValidationResult gridValidation = Avif.Validate(gridData.GetData());
        AvifReader gridReader = AvifReader.Create(gridData.GetData());
        gridReader.Parse();
        AvifDecodeResult gridDecoded = gridReader.DecodePrimaryImage();

        AvifImage preview = CreateImage(
            2, 2, 8, AvifChromaFormat.Monochrome, AvifRange.Full, 10);
        AvifImage middle = CreateImage(
            3, 3, 8, AvifChromaFormat.Monochrome, AvifRange.Full, 11);
        AvifImage final = CreateImage(
            4, 4, 8, AvifChromaFormat.Monochrome, AvifRange.Full, 12);
        AvifLayeredImage layered = AvifLayeredImage.Create(2, true);
        layered.AddLayer(preview);
        layered.AddLayer(middle);
        layered.AddLayer(final);
        AvifWriter layerWriter = AvifWriter.Create(CreateLosslessOptions());
        uint layerId = layerWriter.AddLayeredImage(layered);
        AvifEncodeResult layerData = layerWriter.Finish();
        AvifValidationResult layerValidation = Avif.Validate(layerData.GetData());
        AvifReader layerReader = AvifReader.Create(layerData.GetData());
        layerReader.Parse();
        AvifDecodeResult previewDecoded = layerReader.DecodeLayer(0);
        AvifDecodeResult middleDecoded = layerReader.DecodeLayer(1);
        AvifDecodeResult finalDecoded = layerReader.DecodeLayer(2);
        AvifImageItem? layeredItem = (layerReader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(layerId);

        Assert.Multiple(() =>
        {
            Assert.That(gridId, Is.EqualTo(5));
            Assert.That(gridData.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(gridValidation.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(gridDecoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((gridDecoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(5));
            Assert.That((gridDecoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight(), Is.EqualTo(3));
            Assert.That(layerId, Is.EqualTo(1));
            Assert.That(layerData.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(layerValidation.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((FindProperty(layeredItem, "a1op") ?? throw new InvalidOperationException("Invalid internal state.")).IsEssential(), Is.True);
            Assert.That((FindProperty(layeredItem, "a1lx") ?? throw new InvalidOperationException("Invalid internal state.")).GetLayerCount(), Is.EqualTo(3));
            Assert.That((FindProperty(layeredItem, "lsel") ?? throw new InvalidOperationException("Invalid internal state.")).GetLayerId(), Is.EqualTo(65535));
        });
        AssertGridValues(gridDecoded.GetImage(), 6, 7, 8, 9);
        AssertImagesEqual(preview, previewDecoded.GetImage());
        AssertImagesEqual(middle, middleDecoded.GetImage());
        AssertImagesEqual(final, finalDecoded.GetImage());

        (gridDecoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (previewDecoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (middleDecoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (finalDecoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        gridReader.Close();
        layerReader.Close();
        gridWriter.Close();
        layerWriter.Close();
        topLeft.Close();
        topRight.Close();
        bottomLeft.Close();
        bottomRight.Close();
        preview.Close();
        middle.Close();
        final.Close();
    }

    [Test]
    public void TransformAndMetadataCorpusPreservesEveryAuthoredForm()
    {
        AvifImage image = CreateImage(
            4, 4, 8, AvifChromaFormat.Yuv444, AvifRange.Full, 13);
        AvifIccProfile icc = AvifIccProfile.Create(BuildMinimalIccProfile());
        image.SetIccProfile(icc);
        image.SetColorInfo(AvifColorInfo.Create(
            AvifColorPrimaries.Bt2020,
            AvifTransferCharacteristics.Smpte2084,
            AvifMatrixCoefficients.Bt2020NonConstantLuminance,
            AvifRange.Full));
        AvifTransform transform = new AvifTransform();
        transform.SetCleanAperture(2, 1, 2, 1, -1, 1, -1, 1);
        transform.SetRotation(1);
        transform.SetMirror(0);
        transform.SetPixelAspectRatio(2, 2);
        AvifHdrMetadata hdr = new AvifHdrMetadata();
        hdr.SetContentLightLevel(1000, 400);
        hdr.SetMasteringDisplay(
            new int[] { 34000, 13250, 7500 },
            new int[] { 16000, 34500, 3000 },
            15635,
            16450,
            10000000,
            50);
        AvifExif exif = AvifExif.Create(BuildEmptyExif());
        AvifXmp xmp = AvifXmp.Create(Encoding.UTF8.GetBytes(CreateXmpPacket()));
        AvifMetadata custom = AvifMetadata.Create(
            "mime", "application/example", new byte[] { 9, 8, 7 });
        AvifWriter writer = AvifWriter.Create(CreateLosslessOptions());
        uint itemId = writer.AddImage(image);
        AvifResultCode transformCode = writer.SetTransform(itemId, transform);
        AvifResultCode hdrCode = writer.SetHdrMetadata(itemId, hdr);
        uint exifId = writer.AddExif(itemId, exif);
        uint xmpId = writer.AddXmp(itemId, xmp);
        uint customId = writer.AddMetadata(itemId, custom);
        AvifEncodeResult encoded = writer.Finish();
        AvifValidationResult validation = Avif.Validate(encoded.GetData());
        AvifReader reader = AvifReader.Create(encoded.GetData());
        AvifResultCode parsed = reader.Parse();
        AvifImageItem? item = (reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(itemId);
        AvifTransform parsedTransform = (item ?? throw new InvalidOperationException("Invalid internal state.")).GetTransform();
        AvifHdrMetadata parsedHdr = item.GetHdrMetadata();
        AvifDecodeResult decoded = reader.DecodePrimaryImage();

        Assert.Multiple(() =>
        {
            Assert.That(transformCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(hdrCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(exifId, Is.EqualTo(2));
            Assert.That(xmpId, Is.EqualTo(3));
            Assert.That(customId, Is.EqualTo(4));
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(validation.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed, Is.EqualTo(AvifResultCode.Success));
            Assert.That(FindColorProperty(item, "nclx"), Is.Not.Null);
            Assert.That(FindColorProperty(item, "prof"), Is.Not.Null);
            Assert.That(FindProperty(item, "clap"), Is.Not.Null);
            Assert.That(FindProperty(item, "irot"), Is.Not.Null);
            Assert.That(FindProperty(item, "imir"), Is.Not.Null);
            Assert.That(FindProperty(item, "pasp"), Is.Not.Null);
            Assert.That(FindProperty(item, "clli"), Is.Not.Null);
            Assert.That(FindProperty(item, "mdcv"), Is.Not.Null);
            Assert.That(parsedTransform.GetRotationQuarterTurnsCounterClockwise(),
                Is.EqualTo(1));
            Assert.That(parsedTransform.GetMirrorAxis(), Is.Zero);
            Assert.That((parsedHdr.GetContentLightLevel() ?? throw new InvalidOperationException("Invalid internal state."))
                .GetMaximumContentLightLevel(), Is.EqualTo(1000));
            Assert.That(((reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(exifId) ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType(),
                Is.EqualTo("Exif"));
            Assert.That(((reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(xmpId) ?? throw new InvalidOperationException("Invalid internal state.")).GetContentType(),
                Is.EqualTo("application/rdf+xml"));
            Assert.That(((reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(customId) ?? throw new InvalidOperationException("Invalid internal state.")).GetContentType(),
                Is.EqualTo("application/example"));
            Assert.That(reader.GetEncodedItem(customId), Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
        });
        AssertImagesEqual(image, decoded.GetImage());

        (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        reader.Close();
        writer.Close();
        image.Close();
    }

    static AvifEncoderOptions CreateLosslessOptions()
    {
        AvifEncoderOptions options = new AvifEncoderOptions();
        options.Mode = AvifEncoderMode.Lossless;
        options.Preset = AvifEncoderPreset.Fastest;
        options.ValidateOutput = true;
        return options;
    }

    static AvifImage CreateImage(
        int width,
        int height,
        int bitDepth,
        AvifChromaFormat chromaFormat,
        AvifRange range,
        int seed)
    {
        AvifImage image = AvifImage.Create(
            width,
            height,
            bitDepth,
            chromaFormat,
            range,
            chromaFormat == AvifChromaFormat.Yuv420
                ? AvifChromaSamplePosition.Vertical
                : AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.None,
            1,
            new AvifSecurityLimits());
        int kind = 0;
        while (kind <= (int)AvifPlaneKind.V)
        {
            AvifPlane? plane = image.GetPlane((AvifPlaneKind)kind);
            if (plane != null)
            {
                int row = 0;
                while (row < plane.GetHeight())
                {
                    int column = 0;
                    while (column < plane.GetWidth())
                    {
                        int baseValue = kind == 0 ? 64 : kind == 1 ? 96 : 160;
                        int value = (baseValue + seed + row + column) << (bitDepth - 8);
                        WriteSample(plane, row, column, bitDepth, value);
                        column++;
                    }
                    row++;
                }
            }
            kind++;
        }
        return image;
    }

    static void WriteSample(
        AvifPlane plane,
        int row,
        int column,
        int bitDepth,
        int value)
    {
        Span<byte> bytes = plane.GetRowSpan(row);
        if (bitDepth <= 8)
        {
            bytes[column] = (byte)value;
        }
        else
        {
            BinaryPrimitives.WriteUInt16LittleEndian(
                bytes.Slice(column * 2, 2),
                (ushort)value);
        }
    }

    static void AssertImagesEqual(AvifImage expected, AvifImage? actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That((actual ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(expected.GetWidth()));
            Assert.That(actual.GetHeight(), Is.EqualTo(expected.GetHeight()));
            Assert.That(actual.GetBitDepth(), Is.EqualTo(expected.GetBitDepth()));
            Assert.That(actual.GetChromaFormat(), Is.EqualTo(expected.GetChromaFormat()));
            Assert.That(actual.GetRange(), Is.EqualTo(expected.GetRange()));
        });
        int kind = 0;
        while (kind <= (int)AvifPlaneKind.Alpha)
        {
            AvifPlane? expectedPlane = expected.GetPlane((AvifPlaneKind)kind);
            AvifPlane? actualPlane = (actual ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane((AvifPlaneKind)kind);
            Assert.That(actualPlane == null, Is.EqualTo(expectedPlane == null));
            if (expectedPlane != null)
            {
                int row = 0;
                while (row < expectedPlane.GetHeight())
                {
                    Assert.That((actualPlane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(row).ToArray(),
                        Is.EqualTo(expectedPlane.GetRowSpan(row).ToArray()));
                    row++;
                }
            }
            kind++;
        }
    }

    static void AssertGridValues(
        AvifImage? image,
        int topLeft,
        int topRight,
        int bottomLeft,
        int bottomRight)
    {
        AvifPlane? plane = (image ?? throw new InvalidOperationException("Invalid internal state.")).GetPlane(AvifPlaneKind.Y);
        int row = 0;
        while (row < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight())
        {
            int column = 0;
            while (column < plane.GetWidth())
            {
                int seed = row < 2
                    ? column < 3 ? topLeft : topRight
                    : column < 3 ? bottomLeft : bottomRight;
                int localRow = row < 2 ? row : row - 2;
                int localColumn = column < 3 ? column : column - 3;
                int expected = 64 + seed + localRow + localColumn;
                Assert.That(plane.GetRowSpan(row)[column], Is.EqualTo(expected));
                column++;
            }
            row++;
        }
    }

    static AvifItemProperty? FindProperty(AvifImageItem? item, string type)
    {
        int index = 0;
        while (index < (item ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount())
        {
            AvifItemProperty? property = item.GetProperty(index);
            if ((property ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName() == type)
            {
                return property;
            }
            index++;
        }
        return null;
    }

    static AvifItemProperty? FindColorProperty(
        AvifImageItem item,
        string colorType)
    {
        int index = 0;
        while (index < item.GetPropertyCount())
        {
            AvifItemProperty? property = item.GetProperty(index);
            byte[] raw = (property ?? throw new InvalidOperationException("Invalid internal state.")).GetRawData();
            if (property.GetTypeName() == "colr" && raw.Length >= 12
                && Encoding.ASCII.GetString(raw, 8, 4) == colorType)
            {
                return property;
            }
            index++;
        }
        return null;
    }

    static byte[] BuildMinimalIccProfile()
    {
        byte[] data = new byte[132];
        BinaryPrimitives.WriteUInt32BigEndian(data, 132);
        data[8] = 4;
        data[9] = 64;
        WriteFourCc(data, 12, "mntr");
        WriteFourCc(data, 16, "RGB ");
        WriteFourCc(data, 20, "XYZ ");
        WriteFourCc(data, 36, "acsp");
        return data;
    }

    static byte[] BuildEmptyExif()
    {
        byte[] data = new byte[18];
        BinaryPrimitives.WriteUInt32BigEndian(data, 0);
        data[4] = 73;
        data[5] = 73;
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(6, 2), 42);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, 4), 0);
        return data;
    }

    static string CreateXmpPacket()
    {
        return "<x:xmpmeta xmlns:x='adobe:ns:meta/' "
            + "xmlns:rdf='" + AvifXmp.RdfNamespace + "' "
            + "xmlns:dc='" + AvifXmp.DublinCoreNamespace + "'>"
            + "<rdf:RDF><rdf:Description rdf:about='' "
            + "dc:format='image/avif'/></rdf:RDF></x:xmpmeta>";
    }

    static void WriteFourCc(byte[] data, int offset, string value)
    {
        data[offset] = (byte)value[0];
        data[offset + 1] = (byte)value[1];
        data[offset + 2] = (byte)value[2];
        data[offset + 3] = (byte)value[3];
    }
}
