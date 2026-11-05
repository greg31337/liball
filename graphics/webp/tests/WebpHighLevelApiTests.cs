using webp;

namespace webp.Tests;

[TestFixture]
public class WebpHighLevelApiTests
{
    [Test]
    public void StillImageWorkflowCoversIdentifyDecodeEncodeValidateMuxAndFileStreamHelpers()
    {
        WebpPixelBuffer buffer = NewBuffer();
        WebpEncodeOptions encodeOptions = new WebpEncodeOptions();
        encodeOptions.Mode = WebpEncodeMode.Lossless;
        MemoryStream stream = new MemoryStream();

        WebpEncoder.Encode(buffer, stream, encodeOptions);
        byte[] encoded = stream.ToArray();
        WebpInfo encodedInfo = WebpInfoReader.Read(encoded);
        WebpValidationResult encodedValidation = WebpValidator.Validate(encoded.AsSpan());
        WebpDecodeResult streamDecoded = WebpDecoder.Decode(new MemoryStream(encoded));

        Assert.That(encodedInfo.Width, Is.EqualTo(2));
        Assert.That(encodedInfo.Height, Is.EqualTo(1));
        Assert.That(encodedInfo.BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(encodedInfo.HasAlpha, Is.True);
        Assert.That(encodedValidation.IsValid, Is.True);
        AssertDecodedPixels(streamDecoded, buffer);

        WebpMetadata metadata = new WebpMetadata();
        metadata.Exif = new byte[] { 1, 2, 3 };
        byte[] withMetadata = WebpMux.ReplaceMetadata(encoded, metadata);
        WebpInfo metadataInfo = WebpInfoReader.Read(withMetadata.AsSpan());
        WebpValidationResult metadataValidation = WebpValidator.Validate(withMetadata);
        WebpMetadata extractedMetadata = WebpMux.GetMetadata(withMetadata);
        WebpDecodeOptions identifyOptions = new WebpDecodeOptions();
        identifyOptions.IdentifyOnly = true;
        WebpDecodeResult identified = WebpDecoder.Decode(withMetadata, identifyOptions);

        Assert.That(metadataInfo.ContainerKind, Is.EqualTo(WebpContainerKind.ExtendedStill));
        Assert.That(metadataInfo.HasExif, Is.True);
        Assert.That(metadataValidation.IsValid, Is.True);
        Assert.That(extractedMetadata.Exif, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(identified.IsValid, Is.True);
        Assert.That(identified.Image.CanvasWidth, Is.EqualTo(2));
        Assert.That(identified.Image.CanvasHeight, Is.EqualTo(1));
        Assert.That(identified.Image.Metadata.Exif, Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(identified.Image.Frames[0].Pixels, Is.Null);
        Assert.That(identified.Image.Frames[0].EncodedBitstream, Is.Not.Null);

        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "high-level-" + Guid.NewGuid().ToString("N") + ".webp");
        try
        {
            WebpEncoder.Encode(buffer, path, encodeOptions);
            WebpDecodeResult fileDecoded = WebpDecoder.Decode(path);
            WebpInfo fileInfo = WebpInfoReader.Read(File.ReadAllBytes(path));

            AssertDecodedPixels(fileDecoded, buffer);
            Assert.That(fileInfo.Width, Is.EqualTo(2));
            Assert.That(fileInfo.Height, Is.EqualTo(1));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    static void AssertDecodedPixels(WebpDecodeResult result, WebpPixelBuffer expected)
    {
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(expected.Width));
        Assert.That(result.Image.CanvasHeight, Is.EqualTo(expected.Height));
        Assert.That(result.Image.Frames, Has.Count.EqualTo(1));
        Assert.That(result.Image.Frames[0].Pixels, Is.Not.Null);
        Assert.That(result.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(expected.Rgba));
    }

    static WebpPixelBuffer NewBuffer()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(2, 1);
        buffer.Rgba[0] = 10;
        buffer.Rgba[1] = 20;
        buffer.Rgba[2] = 30;
        buffer.Rgba[3] = 255;
        buffer.Rgba[4] = 40;
        buffer.Rgba[5] = 50;
        buffer.Rgba[6] = 60;
        buffer.Rgba[7] = 128;
        return buffer;
    }
}
