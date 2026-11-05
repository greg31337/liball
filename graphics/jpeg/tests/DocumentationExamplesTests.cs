using System.Collections.Generic;

namespace jpeg.Tests;

internal class DocumentationExamplesTests
{
    [Test]
    public void Examples_IdentifyReadAndWriteWorkflows()
    {
        byte[] data = global::jpeg.Jpeg.Write(GoldenEncodeTests.CreateRgbImage(8, 8, 140, 140, 140));
        global::jpeg.JpegReadOptions readOptions = new global::jpeg.JpegReadOptions();
        readOptions.ColorPolicy = global::jpeg.JpegColorPolicy.ConvertToRgb;
        readOptions.OutputPixelFormat = global::jpeg.JpegPixelFormat.Rgb24;

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);
        global::jpeg.JpegImage image = global::jpeg.Jpeg.Read(data, readOptions);
        global::jpeg.JpegImage writable = global::jpeg.Jpeg.CreateImageFromPixels(CreateRgbPixels(8, 8, 128, 128, 128), 8, 8, global::jpeg.JpegPixelFormat.Rgb24);
        writable.Metadata.Comments.Add("created by jpeg");
        global::jpeg.JpegWriteOptions writeOptions = new global::jpeg.JpegWriteOptions();
        writeOptions.Quality = 85;
        writeOptions.RestartInterval = 8;
        byte[] output = global::jpeg.Jpeg.Write(writable, writeOptions);
        global::jpeg.JpegInfo outputInfo = global::jpeg.Jpeg.Identify(output);

        Assert.That(info.Width, Is.EqualTo(8));
        Assert.That(info.Height, Is.EqualTo(8));
        Assert.That(info.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCbCr));
        Assert.That(image.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(image.Pixels[0], Is.EqualTo(140));
        Assert.That(outputInfo.CommentCount, Is.EqualTo(1));
        Assert.That(outputInfo.Metadata.Comments[0], Is.EqualTo("created by jpeg"));
        Assert.That(outputInfo.HasRestartInterval, Is.True);
        Assert.That(outputInfo.RestartInterval, Is.EqualTo(8));
    }

    [Test]
    public void Examples_MetadataPreservationWorkflow()
    {
        byte[] data = JpegMetadataUpdaterTests.CreateMetadataUpdateSource();
        List<string> comments = new List<string>();
        comments.Add("reviewed");
        byte[] exifPayload = ExifMetadataParserTests.CreateExifPayload(ExifTagReaderTests.CreateCommonExifTiff());

        byte[] withComments = global::jpeg.Jpeg.SetComments(data, comments);
        byte[] withIcc = global::jpeg.Jpeg.SetIccProfile(withComments, new byte[] { 9, 8, 7 });
        byte[] withExif = global::jpeg.Jpeg.SetExif(withIcc, exifPayload);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(withExif);

        Assert.That(info.Metadata.Comments, Is.EqualTo(new string[] { "reviewed" }));
        Assert.That(info.Metadata.HasIccProfile, Is.True);
        Assert.That(info.Metadata.IccProfile, Is.EqualTo(new byte[] { 9, 8, 7 }));
        Assert.That(info.Metadata.HasExif, Is.True);
        Assert.That(info.Metadata.Exif, Is.EqualTo(exifPayload));
        Assert.That(JpegMetadataUpdaterTests.ContainsRawMarker(info.Metadata, 0xFFF0, new byte[] { 0x77 }), Is.True);
    }

    [Test]
    public void Examples_CmykHandlingWorkflow()
    {
        global::jpeg.JpegInfo cmykInfo = global::jpeg.Jpeg.Identify(CreateFourComponentJpeg(global::jpeg.JpegConstants.AdobeTransformUnknown));
        global::jpeg.JpegInfo ycckInfo = global::jpeg.Jpeg.Identify(CreateFourComponentJpeg(global::jpeg.JpegConstants.AdobeTransformYCCK));
        global::jpeg.JpegCmykToRgbConverter cmykConverter = new global::jpeg.JpegCmykToRgbConverter();
        global::jpeg.JpegYcckToCmykConverter ycckConverter = new global::jpeg.JpegYcckToCmykConverter();

        global::jpeg.JpegImage rgb = cmykConverter.ConvertImageToRgbImage(CmykToRgbConverterTests.CreateCmykImage());
        global::jpeg.JpegImage cmyk = ycckConverter.ConvertImageToCmykImage(YcckToCmykConverterTests.CreateYcckImage());

        Assert.That(cmykInfo.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.CMYK));
        Assert.That(ycckInfo.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCCK));
        Assert.That(rgb.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(rgb.Pixels, Is.EqualTo(CmykToRgbConverterTests.CreateExpectedRgbPixels()));
        Assert.That(cmyk.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Cmyk32));
        Assert.That(cmyk.Pixels, Is.EqualTo(YcckToCmykConverterTests.CreateExpectedCmykPixels()));
    }

    [Test]
    public void Examples_ProgressiveOutputWorkflow()
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame2, IdentifierTests.CreateSofPayload(global::jpeg.JpegConstants.MarkerStartOfFrame2, 1));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, IdentifierTests.CreateSosPayload(1));
        writer.WriteEndOfImage();
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.Progressive = true;
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(16, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = BaselineSof0EncoderTests.CreatePlanarSource(frame);
        BaselineSof0EncoderTests.FillBlock(source.Components[0], 0, 0, 138);
        BaselineSof0EncoderTests.FillBlock(source.Components[0], 8, 0, 138);
        global::jpeg.JpegProgressiveSof2Encoder encoder = new global::jpeg.JpegProgressiveSof2Encoder();

        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(writer.ToArray());
        List<global::jpeg.JpegProgressiveScanData> scans = encoder.Encode(frame, tables, source);

        Assert.That(info.Progressive, Is.True);
        byte[] progressiveOutput = global::jpeg.Jpeg.Write(JpegWriteApiTests.CreateGrayscaleImage(8, 8, 128), options);
        Assert.That(global::jpeg.Jpeg.Identify(progressiveOutput).Progressive, Is.True);
        Assert.That(scans.Count, Is.GreaterThan(0));
        Assert.That(scans[0].EntropyData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void Examples_LosslessTransformWorkflow()
    {
        byte[] data = JpegLosslessTransformerTests.CreateMetadataCoefficientJpeg();

        byte[] rotated = global::jpeg.Jpeg.Rotate90(data);
        byte[] flipped = global::jpeg.Jpeg.FlipHorizontal(data);
        byte[] cropped = global::jpeg.Jpeg.Crop(data, 0, 0, 8, 8);

        JpegLosslessTransformerTests.AssertPreservedMetadata(rotated, 16, 16);
        JpegLosslessTransformerTests.AssertPreservedMetadata(flipped, 16, 16);
        JpegLosslessTransformerTests.AssertPreservedMetadata(cropped, 8, 8);
    }

    public static byte[] CreateRgbPixels(int width, int height, int red, int green, int blue)
    {
        byte[] pixels = new byte[width * height * 3];
        int index = 0;
        while (index < pixels.Length)
        {
            pixels[index] = (byte)red;
            pixels[index + 1] = (byte)green;
            pixels[index + 2] = (byte)blue;
            index = index + 3;
        }

        return pixels;
    }

    public static byte[] CreateFourComponentJpeg(int adobeTransform)
    {
        global::jpeg.JpegMarkerSegmentWriter writer = new global::jpeg.JpegMarkerSegmentWriter();
        writer.WriteStartOfImage();
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.App14, AdobeMetadataParserTests.CreateAdobePayload(100, 0, 0, adobeTransform));
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfFrame0, CreateFourComponentSofPayload());
        writer.WriteMarkerSegment(global::jpeg.JpegMarker.StartOfScan, CreateFourComponentSosPayload());
        writer.WriteEndOfImage();
        return writer.ToArray();
    }

    public static byte[] CreateFourComponentSofPayload()
    {
        return new byte[]
        {
            8, 0, 8, 0, 8, 4,
            1, 0x11, 0,
            2, 0x11, 0,
            3, 0x11, 0,
            4, 0x11, 0
        };
    }

    public static byte[] CreateFourComponentSosPayload()
    {
        return new byte[]
        {
            4,
            1, 0,
            2, 0,
            3, 0,
            4, 0,
            0, 63, 0
        };
    }
}
