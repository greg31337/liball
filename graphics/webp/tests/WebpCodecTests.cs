using webp;

namespace webp.Tests;

[TestFixture]
public class WebpCodecTests
{
    [Test]
    public void CodecImplementsNarrowPublicInterfaces()
    {
        WebpCodec codec = new WebpCodec();

        Assert.That(codec, Is.InstanceOf<IWebpFormatDetector>());
        Assert.That(codec, Is.InstanceOf<IWebpDecoder>());
        Assert.That(codec, Is.InstanceOf<IWebpEncoder>());
        Assert.That(codec, Is.InstanceOf<IWebpInfoReader>());
        Assert.That(codec, Is.InstanceOf<IWebpValidator>());
        Assert.That(codec, Is.InstanceOf<IWebpPixelUtilities>());
        Assert.That(codec, Is.InstanceOf<IWebpMux>());
    }

    [Test]
    public void FormatDetectorRecognizesWebpSignaturesFromArraysAndSpans()
    {
        IWebpFormatDetector detector = new WebpCodec();
        byte[] valid = WebpRiffWriter.Write("WEBP", new List<WebpRiffChunk>());
        byte[] wrongForm = WebpRiffWriter.Write("AVI ", new List<WebpRiffChunk>());

        Assert.That(detector.HasRiffWebpSignature(valid), Is.True);
        Assert.That(detector.HasRiffWebpSignatureSpan(valid.AsSpan()), Is.True);
        Assert.That(detector.HasRiffWebpSignature(wrongForm), Is.False);
        Assert.That(detector.HasRiffWebpSignatureSpan(new byte[] { 1, 2, 3 }), Is.False);
    }

    [Test]
    public void FormatDetectorRecognizesMimeTypesAndFileExtensionsWithoutCaseSensitivity()
    {
        IWebpFormatDetector detector = new WebpCodec();

        Assert.That(detector.IsWebpMimeType("image/webp"), Is.True);
        Assert.That(detector.IsWebpMimeType("IMAGE/WEBP"), Is.True);
        Assert.That(detector.IsWebpMimeType("image/png"), Is.False);
        Assert.That(detector.IsWebpFileExtension(".webp"), Is.True);
        Assert.That(detector.IsWebpFileExtension("WEBP"), Is.True);
        Assert.That(detector.IsWebpFileExtension(".png"), Is.False);
    }

    [Test]
    public void FormatDetectorRejectsNullReferenceInputs()
    {
        IWebpFormatDetector detector = new WebpCodec();

        Assert.Throws<ArgumentNullException>(() => detector.HasRiffWebpSignature(null!));
        Assert.Throws<ArgumentNullException>(() => detector.IsWebpMimeType(null!));
        Assert.Throws<ArgumentNullException>(() => detector.IsWebpFileExtension(null!));
    }

    [Test]
    public void InterfaceApiSupportsCompleteStillImageWorkflow()
    {
        WebpCodec codec = new WebpCodec();
        IWebpEncoder encoder = codec;
        IWebpDecoder decoder = codec;
        IWebpInfoReader infoReader = codec;
        IWebpValidator validator = codec;
        IWebpPixelUtilities pixelUtilities = codec;
        IWebpMux mux = codec;
        WebpPixelBuffer source = NewBuffer();
        WebpEncodeOptions encodeOptions = new WebpEncodeOptions();
        encodeOptions.Mode = WebpEncodeMode.Lossless;

        byte[] data = encoder.Encode(source, encodeOptions);
        WebpInfo info = infoReader.Read(data, new WebpDecodeOptions());
        WebpValidationResult validation = validator.Validate(data, new WebpDecodeOptions());
        WebpDecodeResult decoded = decoder.Decode(data, new WebpDecodeOptions());
        byte[] bgra = pixelUtilities.CopyRgbaToBgra(source);
        WebpMetadata metadata = new WebpMetadata();
        metadata.Exif = new byte[] { 7 };
        WebpMetadata extracted = mux.GetMetadata(mux.ReplaceMetadata(data, metadata));

        Assert.That(info.Width, Is.EqualTo(2));
        Assert.That(info.Height, Is.EqualTo(1));
        Assert.That(validation.IsValid, Is.True);
        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(source.Rgba));
        Assert.That(bgra[0], Is.EqualTo(source.Rgba[2]));
        Assert.That(extracted.Exif, Is.EqualTo(new byte[] { 7 }));
    }

    [Test]
    public void ConsumerCanUseInfoReaderTestDoubleWithoutParsingWebp()
    {
        StubInfoReader stub = new StubInfoReader();
        stub.Result.Width = 640;
        stub.Result.Height = 480;
        WebpDecodeOptions options = new WebpDecodeOptions();

        WebpInfo result = Inspect(stub, new byte[] { 1, 2, 3 }, options);

        Assert.That(result.Width, Is.EqualTo(640));
        Assert.That(result.Height, Is.EqualTo(480));
        Assert.That(stub.CallCount, Is.EqualTo(1));
        Assert.That(stub.ReceivedOptions, Is.SameAs(options));
    }

    static WebpInfo Inspect(IWebpInfoReader reader, byte[] data, WebpDecodeOptions options)
    {
        return reader.Read(data, options);
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

public class StubInfoReader : IWebpInfoReader
{
    public int CallCount;
    public WebpDecodeOptions ReceivedOptions;
    public WebpInfo Result;

    public StubInfoReader()
    {
        CallCount = 0;
        ReceivedOptions = null!;
        Result = new WebpInfo();
    }

    public WebpInfo Read(byte[] data, WebpDecodeOptions options)
    {
        CallCount = CallCount + 1;
        ReceivedOptions = options;
        return Result;
    }

    public WebpInfo ReadSpan(ReadOnlySpan<byte> data, WebpDecodeOptions options)
    {
        CallCount = CallCount + 1;
        ReceivedOptions = options;
        return Result;
    }

    public WebpInfo ReadStream(Stream stream, WebpDecodeOptions options)
    {
        CallCount = CallCount + 1;
        ReceivedOptions = options;
        return Result;
    }

    public WebpInfo ReadFile(string path, WebpDecodeOptions options)
    {
        CallCount = CallCount + 1;
        ReceivedOptions = options;
        return Result;
    }
}
