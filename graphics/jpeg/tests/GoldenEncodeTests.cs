using System.Collections.Generic;

namespace jpeg.Tests;

internal class GoldenEncodeTests
{
    [Test]
    public void GoldenEncode_BaselineGrayscaleFile()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);

        byte[] data = global::jpeg.Jpeg.Write(source);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(data);
        global::jpeg.JpegMarkerSegment[] markers = global::jpeg.JpegInspector.ReadMarkers(data);

        Assert.That(data[0], Is.EqualTo(0xFF));
        Assert.That(data[1], Is.EqualTo(0xD8));
        Assert.That(data[data.Length - 2], Is.EqualTo(0xFF));
        Assert.That(data[data.Length - 1], Is.EqualTo(0xD9));
        Assert.That(markers[0].Marker, Is.EqualTo(global::jpeg.JpegMarker.StartOfImage));
        Assert.That(markers[markers.Length - 1].Marker, Is.EqualTo(global::jpeg.JpegMarker.EndOfImage));
        Assert.That(info.Process, Is.EqualTo(global::jpeg.JpegProcess.BaselineDct));
        Assert.That(info.Width, Is.EqualTo(8));
        Assert.That(info.Height, Is.EqualTo(8));
        Assert.That(info.ComponentCount, Is.EqualTo(1));
        Assert.That(info.QuantizationTableCount, Is.EqualTo(1));
        Assert.That(info.HuffmanTableCount, Is.EqualTo(2));
        Assert.That(info.ScanCount, Is.EqualTo(1));
        Assert.That(decoded.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.L8));
        Assert.That(decoded.Pixels[0], Is.EqualTo(138));
        Assert.That(decoded.Pixels[63], Is.EqualTo(138));
    }

    [Test]
    public void GoldenEncode_RgbFileRoundTripsThroughRgbOutput()
    {
        global::jpeg.JpegImage source = CreateRgbImage(8, 8, 140, 140, 140);
        global::jpeg.JpegReadOptions readOptions = new global::jpeg.JpegReadOptions();
        readOptions.ColorPolicy = global::jpeg.JpegColorPolicy.ConvertToRgb;

        byte[] data = global::jpeg.Jpeg.Write(source);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(data, readOptions);

        Assert.That(info.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCbCr));
        Assert.That(info.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio444));
        Assert.That(info.ComponentCount, Is.EqualTo(3));
        Assert.That(info.QuantizationTableCount, Is.EqualTo(2));
        Assert.That(info.HuffmanTableCount, Is.EqualTo(4));
        Assert.That(decoded.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.Rgb24));
        Assert.That(decoded.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.RGB));
        AssertRgbPixel(decoded.Pixels, 0, 140, 140, 140);
        AssertRgbPixel(decoded.Pixels, 63, 140, 140, 140);
    }

    [Test]
    public void GoldenEncode_YCbCrFilePreservesRawColorSamples()
    {
        global::jpeg.JpegImage source = CreateYCbCrImage(8, 8, 144, 145, 162);

        byte[] data = global::jpeg.Jpeg.Write(source);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(data);

        Assert.That(info.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCbCr));
        Assert.That(info.Subsampling, Is.EqualTo(global::jpeg.JpegSubsampling.Ratio444));
        Assert.That(decoded.PixelFormat, Is.EqualTo(global::jpeg.JpegPixelFormat.YCbCr24));
        Assert.That(decoded.ColorSpace, Is.EqualTo(global::jpeg.JpegColorSpace.YCbCr));
        AssertRgbPixel(decoded.Pixels, 0, 144, 145, 162);
        AssertRgbPixel(decoded.Pixels, 63, 144, 145, 162);
    }

    [Test]
    public void GoldenEncode_RestartMarkersAndOptimizedHuffmanTables()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateTwoBlockGrayscaleImage();
        global::jpeg.JpegWriteOptions options = new global::jpeg.JpegWriteOptions();
        options.RestartInterval = 1;
        options.OptimizeHuffmanTables = true;

        byte[] data = global::jpeg.Jpeg.Write(source, options);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(data);
        global::jpeg.JpegMarkerSegment[] markers = global::jpeg.JpegInspector.ReadMarkers(data);

        Assert.That(info.HasRestartInterval, Is.True);
        Assert.That(info.RestartInterval, Is.EqualTo(1));
        Assert.That(info.RestartMarkerCount, Is.EqualTo(1));
        Assert.That(info.HuffmanTableCount, Is.EqualTo(2));
        Assert.That(ContainsMarker(markers, global::jpeg.JpegMarker.DefineRestartInterval), Is.True);
        Assert.That(ContainsMarker(markers, global::jpeg.JpegMarker.Restart0), Is.True);
        Assert.That(decoded.Pixels[0], Is.EqualTo(138));
        Assert.That(decoded.Pixels[8], Is.EqualTo(143));
    }

    [Test]
    public void GoldenEncode_MetadataFilePreservesIccExifAndComments()
    {
        global::jpeg.JpegImage source = JpegWriteApiTests.CreateGrayscaleImage(8, 8, 138);
        byte[] iccPayload = IccProfileChunkParserTests.CreateIccPayload(1, 1, new byte[] { 9, 8, 7 });
        byte[] exifPayload = ExifMetadataParserTests.CreateExifPayload(ExifTagReaderTests.CreateCommonExifTiff());
        byte[] commentPayload = System.Text.Encoding.Latin1.GetBytes("encoded golden note");
        source.Metadata.RawMarkers.Add(MetadataWriterOrderingTests.CreateRawMarker(global::jpeg.JpegMarker.App2, global::jpeg.JpegConstants.MarkerApp2, iccPayload));
        source.Metadata.RawMarkers.Add(MetadataWriterOrderingTests.CreateRawMarker(global::jpeg.JpegMarker.App1, global::jpeg.JpegConstants.MarkerApp1, exifPayload));
        source.Metadata.RawMarkers.Add(MetadataWriterOrderingTests.CreateRawMarker(global::jpeg.JpegMarker.Comment, global::jpeg.JpegConstants.MarkerComment, commentPayload));

        byte[] data = global::jpeg.Jpeg.Write(source);
        global::jpeg.JpegInfo info = global::jpeg.Jpeg.Identify(data);
        global::jpeg.JpegImage decoded = global::jpeg.Jpeg.Read(data);
        global::jpeg.JpegMarkerSegment[] markers = global::jpeg.JpegInspector.ReadMarkers(data);

        Assert.That(info.Metadata.HasIccProfile, Is.True);
        Assert.That(info.Metadata.IccProfile, Is.EqualTo(new byte[] { 9, 8, 7 }));
        Assert.That(info.Metadata.HasExif, Is.True);
        Assert.That(info.Metadata.ExifMetadata.Orientation, Is.EqualTo(6));
        Assert.That(info.Metadata.ExifMetadata.Make, Is.EqualTo("CameraCo"));
        Assert.That(info.CommentCount, Is.EqualTo(1));
        Assert.That(info.Metadata.Comments, Is.EqualTo(new string[] { "encoded golden note" }));
        Assert.That(FindMarkerIndex(markers, global::jpeg.JpegMarker.App2), Is.LessThan(FindMarkerIndex(markers, global::jpeg.JpegMarker.App1)));
        Assert.That(FindMarkerIndex(markers, global::jpeg.JpegMarker.App1), Is.LessThan(FindMarkerIndex(markers, global::jpeg.JpegMarker.Comment)));
        Assert.That(decoded.Metadata.HasIccProfile, Is.True);
        Assert.That(decoded.Metadata.HasExif, Is.True);
        Assert.That(decoded.Metadata.Comments, Is.EqualTo(new string[] { "encoded golden note" }));
        Assert.That(decoded.Pixels[0], Is.EqualTo(138));
    }

    [Test]
    public void GoldenEncode_ProgressiveEncoderProducesDecodableScans()
    {
        global::jpeg.JpegFrameHeader frame = ProgressiveSof2FullImageDecoderTests.CreateProgressiveFrame(16, 8);
        frame.Components.Add(BaselineSof0FullImageDecoderTests.CreateFrameComponent(1, 1, 1, 0));
        frame.ComponentCount = 1;
        global::jpeg.JpegTableStore tables = BaselineSof0FullImageDecoderTests.CreateTableStore(40);
        global::jpeg.JpegPlanarImageBuffer source = BaselineSof0EncoderTests.CreatePlanarSource(frame);
        BaselineSof0EncoderTests.FillBlock(source.Components[0], 0, 0, 138);
        BaselineSof0EncoderTests.FillBlock(source.Components[0], 8, 0, 143);
        global::jpeg.JpegProgressiveSof2Encoder encoder = new global::jpeg.JpegProgressiveSof2Encoder();
        global::jpeg.JpegProgressiveSof2FullImageDecoder decoder = new global::jpeg.JpegProgressiveSof2FullImageDecoder();

        List<global::jpeg.JpegProgressiveScanData> scans = encoder.Encode(frame, tables, source);
        global::jpeg.JpegPlanarImageBuffer decoded = decoder.Decode(frame, scans, tables);

        Assert.That(frame.Progressive, Is.True);
        Assert.That(scans.Count, Is.EqualTo(2));
        Assert.That(scans[0].Scan.SpectralStart, Is.EqualTo(0));
        Assert.That(scans[0].Scan.SpectralEnd, Is.EqualTo(0));
        Assert.That(scans[0].EntropyData.Length, Is.GreaterThan(0));
        Assert.That(scans[1].Scan.SpectralStart, Is.EqualTo(1));
        Assert.That(scans[1].Scan.SpectralEnd, Is.EqualTo(63));
        Assert.That(scans[1].EntropyData.Length, Is.GreaterThan(0));
        Assert.That(decoded.Components[0].Samples[0], Is.EqualTo(138));
        Assert.That(decoded.Components[0].Samples[8], Is.EqualTo(143));
    }

    [Test]
    public void GoldenEncode_PublicWriterSupportsProgressiveAndRejectsCmykAndYcck()
    {
        global::jpeg.JpegWriteOptions progressiveOptions = new global::jpeg.JpegWriteOptions();
        progressiveOptions.Progressive = true;

        byte[] progressive = global::jpeg.Jpeg.Write(JpegWriteApiTests.CreateGrayscaleImage(8, 8, 128), progressiveOptions);
        Assert.That(global::jpeg.Jpeg.Identify(progressive).Process, Is.EqualTo(global::jpeg.JpegProcess.ProgressiveDct));
        Assert.Throws<global::jpeg.JpegUnsupportedException>(
            delegate
            {
                global::jpeg.Jpeg.Write(CmykToRgbConverterTests.CreateCmykImage());
            });
        Assert.Throws<global::jpeg.JpegUnsupportedException>(
            delegate
            {
                global::jpeg.Jpeg.Write(YcckToCmykConverterTests.CreateYcckImage());
            });
    }

    public static global::jpeg.JpegImage CreateRgbImage(int width, int height, int red, int green, int blue)
    {
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = width;
        image.Height = height;
        image.Precision = 8;
        image.ComponentCount = 3;
        image.PixelFormat = global::jpeg.JpegPixelFormat.Rgb24;
        image.ColorSpace = global::jpeg.JpegColorSpace.RGB;
        image.Subsampling = global::jpeg.JpegSubsampling.Ratio444;
        image.Pixels = new byte[width * height * 3];
        FillInterleavedPixels(image.Pixels, red, green, blue);
        return image;
    }

    public static global::jpeg.JpegImage CreateYCbCrImage(int width, int height, int luminance, int cb, int cr)
    {
        global::jpeg.JpegImage image = new global::jpeg.JpegImage();
        image.Width = width;
        image.Height = height;
        image.Precision = 8;
        image.ComponentCount = 3;
        image.PixelFormat = global::jpeg.JpegPixelFormat.YCbCr24;
        image.ColorSpace = global::jpeg.JpegColorSpace.YCbCr;
        image.Subsampling = global::jpeg.JpegSubsampling.Ratio444;
        image.Pixels = new byte[width * height * 3];
        FillInterleavedPixels(image.Pixels, luminance, cb, cr);
        return image;
    }

    public static void FillInterleavedPixels(byte[] pixels, int first, int second, int third)
    {
        int index = 0;
        while (index < pixels.Length)
        {
            pixels[index] = (byte)first;
            pixels[index + 1] = (byte)second;
            pixels[index + 2] = (byte)third;
            index = index + 3;
        }
    }

    public static void AssertRgbPixel(byte[] pixels, int pixelIndex, int first, int second, int third)
    {
        int offset = pixelIndex * 3;
        Assert.That(pixels[offset], Is.EqualTo(first));
        Assert.That(pixels[offset + 1], Is.EqualTo(second));
        Assert.That(pixels[offset + 2], Is.EqualTo(third));
    }

    public static bool ContainsMarker(global::jpeg.JpegMarkerSegment[] markers, global::jpeg.JpegMarker marker)
    {
        int index = 0;
        while (index < markers.Length)
        {
            if (markers[index].Marker == marker)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    public static int FindMarkerIndex(global::jpeg.JpegMarkerSegment[] markers, global::jpeg.JpegMarker marker)
    {
        int index = 0;
        while (index < markers.Length)
        {
            if (markers[index].Marker == marker)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }
}
