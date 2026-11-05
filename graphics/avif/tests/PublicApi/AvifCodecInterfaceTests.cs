using System.IO;

namespace avif.Tests;

public class AvifCodecInterfaceTests
{
    [Test]
    public void DefaultFacadeMatchesStaticOneCallOperations()
    {
        AvifImage image = CreateImage();
        IAvifCodec codec = new AvifCodec();

        AvifEncodeResult encoded = codec.Encode(image, new AvifEncoderOptions());
        AvifProbeResult probe = codec.Probe(encoded.GetData());
        AvifReadResult read = codec.Read(
            encoded.GetData(),
            new AvifValidationOptions());
        AvifValidationResult validation = codec.Validate(
            encoded.GetData(),
            new AvifValidationOptions());
        AvifDecodeResult decoded = codec.Decode(
            encoded.GetData(),
            new AvifDecoderOptions());
        MemoryStream destination = new MemoryStream();
        AvifResultCode write = codec.Write(
            destination,
            image,
            new AvifEncoderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(probe.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(validation.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(2));
            Assert.That(write, Is.EqualTo(AvifResultCode.Success));
            Assert.That(destination.Length, Is.GreaterThan(0));
            Assert.That(destination.CanWrite, Is.True);
        });

        image.Close();
        (decoded.GetImage() ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        destination.Dispose();
    }

    [Test]
    public void ApplicationCanReplaceCodecWithAControlledTestDouble()
    {
        StubAvifCodec codec = new StubAvifCodec();
        codec.EncodeResult = new AvifEncodeResult(
            AvifResultCode.EncodeFailed,
            System.Array.Empty<byte>());
        AvifImage image = CreateImage();

        AvifResultCode code = EncodeForApplication(codec, image);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.EncodeFailed));
            Assert.That(codec.EncodeCallCount, Is.EqualTo(1));
            Assert.That(codec.LastImage, Is.SameAs(image));
        });

        image.Close();
    }

    static AvifResultCode EncodeForApplication(
        IAvifCodec codec,
        AvifImage image)
    {
        return codec.Encode(image, new AvifEncoderOptions()).GetCode();
    }

    static AvifImage CreateImage()
    {
        AvifImage image = AvifImage.Create(
            2,
            1,
            8,
            AvifChromaFormat.Monochrome,
            false);
        (image.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).Fill(41);
        return image;
    }
}

internal class StubAvifCodec : IAvifCodec
{
    internal AvifEncodeResult EncodeResult;
    internal int EncodeCallCount;
    internal AvifImage? LastImage;

    internal StubAvifCodec()
    {
        EncodeResult = new AvifEncodeResult(
            AvifResultCode.Success,
            System.Array.Empty<byte>());
        EncodeCallCount = 0;
        LastImage = null;
    }

    public AvifProbeResult Probe(ReadOnlySpan<byte> source)
    {
        return new AvifProbeResult(AvifResultCode.NotFound);
    }

    public AvifReadResult Read(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options)
    {
        return new AvifReadResult(AvifResultCode.NotFound, null);
    }

    public AvifValidationResult Validate(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options)
    {
        return new AvifValidationResult(AvifResultCode.NotFound, null);
    }

    public AvifDecodeResult Decode(
        ReadOnlySpan<byte> source,
        AvifDecoderOptions options)
    {
        return new AvifDecodeResult(AvifResultCode.NotFound, null, null);
    }

    public AvifEncodeResult Encode(
        AvifImage image,
        AvifEncoderOptions options)
    {
        EncodeCallCount++;
        LastImage = image;
        return EncodeResult;
    }

    public AvifResultCode Write(
        Stream destination,
        AvifImage image,
        AvifEncoderOptions options)
    {
        return AvifResultCode.NotFound;
    }

    public AvifProbeResult ProbeBytes(byte[]? source)
    {
        return new AvifProbeResult(AvifResultCode.NotFound);
    }

    public AvifProbeResult ProbeSpan(ReadOnlySpan<byte> source)
    {
        return new AvifProbeResult(AvifResultCode.NotFound);
    }

    public AvifProbeResult ProbeStream(Stream? source)
    {
        return new AvifProbeResult(AvifResultCode.NotFound);
    }

    public AvifProbeResult ProbeFile(string? path)
    {
        return new AvifProbeResult(AvifResultCode.NotFound);
    }

    public AvifReadResult InspectBytes(
        byte[]? source,
        AvifValidationOptions options)
    {
        return new AvifReadResult(AvifResultCode.NotFound, null);
    }

    public AvifReadResult InspectSpan(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options)
    {
        return new AvifReadResult(AvifResultCode.NotFound, null);
    }

    public AvifReadResult InspectStream(
        Stream? source,
        AvifValidationOptions options)
    {
        return new AvifReadResult(AvifResultCode.NotFound, null);
    }

    public AvifReadResult InspectFile(
        string? path,
        AvifValidationOptions options)
    {
        return new AvifReadResult(AvifResultCode.NotFound, null);
    }

    public AvifValidationResult ValidateBytes(
        byte[]? source,
        AvifValidationOptions options)
    {
        return new AvifValidationResult(AvifResultCode.NotFound, null);
    }

    public AvifValidationResult ValidateSpan(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options)
    {
        return new AvifValidationResult(AvifResultCode.NotFound, null);
    }

    public AvifValidationResult ValidateStream(
        Stream? source,
        AvifValidationOptions options)
    {
        return new AvifValidationResult(AvifResultCode.NotFound, null);
    }

    public AvifValidationResult ValidateFile(
        string? path,
        AvifValidationOptions options)
    {
        return new AvifValidationResult(AvifResultCode.NotFound, null);
    }

    public AvifDecodeResult DecodePlanarBytes(
        byte[]? source,
        AvifDecoderOptions options)
    {
        return new AvifDecodeResult(AvifResultCode.NotFound, null, null);
    }

    public AvifDecodeResult DecodePlanarSpan(
        ReadOnlySpan<byte> source,
        AvifDecoderOptions options)
    {
        return new AvifDecodeResult(AvifResultCode.NotFound, null, null);
    }

    public AvifDecodeResult DecodePlanarStream(
        Stream? source,
        AvifDecoderOptions options)
    {
        return new AvifDecodeResult(AvifResultCode.NotFound, null, null);
    }

    public AvifDecodeResult DecodePlanarFile(
        string? path,
        AvifDecoderOptions options)
    {
        return new AvifDecodeResult(AvifResultCode.NotFound, null, null);
    }

    public AvifEncodeResult EncodePlanar(
        AvifImage image,
        AvifEncoderOptions options)
    {
        return new AvifEncodeResult(
            AvifResultCode.NotFound,
            System.Array.Empty<byte>());
    }

    public AvifResultCode WritePlanarStream(
        Stream? destination,
        AvifImage image,
        AvifEncoderOptions options)
    {
        return AvifResultCode.NotFound;
    }

    public AvifResultCode WritePlanarFile(
        string? path,
        AvifImage image,
        AvifEncoderOptions options)
    {
        return AvifResultCode.NotFound;
    }

    public AvifEncodeResult TranscodeBytes(
        byte[]? source,
        AvifDecoderOptions decoderOptions,
        AvifEncoderOptions encoderOptions)
    {
        return new AvifEncodeResult(
            AvifResultCode.NotFound,
            System.Array.Empty<byte>());
    }

    public AvifEncodeResult TranscodeSpan(
        ReadOnlySpan<byte> source,
        AvifDecoderOptions decoderOptions,
        AvifEncoderOptions encoderOptions)
    {
        return new AvifEncodeResult(
            AvifResultCode.NotFound,
            System.Array.Empty<byte>());
    }

    public AvifEncodeResult TranscodeStream(
        Stream? source,
        AvifDecoderOptions decoderOptions,
        AvifEncoderOptions encoderOptions)
    {
        return new AvifEncodeResult(
            AvifResultCode.NotFound,
            System.Array.Empty<byte>());
    }

    public AvifEncodeResult TranscodeFile(
        string? path,
        AvifDecoderOptions decoderOptions,
        AvifEncoderOptions encoderOptions)
    {
        return new AvifEncodeResult(
            AvifResultCode.NotFound,
            System.Array.Empty<byte>());
    }

    public AvifPixelBuffer DecodePackedBytes(
        byte[]? source,
        AvifPackedDecodeOptions options)
    {
        return AvifPixelBuffer.Create(AvifResultCode.NotFound);
    }

    public AvifPixelBuffer DecodePackedSpan(
        ReadOnlySpan<byte> source,
        AvifPackedDecodeOptions options)
    {
        return AvifPixelBuffer.Create(AvifResultCode.NotFound);
    }

    public AvifPixelBuffer DecodePackedStream(
        Stream? source,
        AvifPackedDecodeOptions options)
    {
        return AvifPixelBuffer.Create(AvifResultCode.NotFound);
    }

    public AvifPixelBuffer DecodePackedFile(
        string? path,
        AvifPackedDecodeOptions options)
    {
        return AvifPixelBuffer.Create(AvifResultCode.NotFound);
    }

    public AvifEncodeResult EncodePacked(
        AvifPixelBuffer pixels,
        AvifPackedEncodeOptions options)
    {
        return new AvifEncodeResult(
            AvifResultCode.NotFound,
            System.Array.Empty<byte>());
    }

    public AvifResultCode WritePackedStream(
        Stream? destination,
        AvifPixelBuffer pixels,
        AvifPackedEncodeOptions options)
    {
        return AvifResultCode.NotFound;
    }

    public AvifResultCode WritePackedFile(
        string? path,
        AvifPixelBuffer pixels,
        AvifPackedEncodeOptions options)
    {
        return AvifResultCode.NotFound;
    }

    public AvifReader CreateSeekableStreamReader(
        Stream? source,
        AvifDecoderOptions options)
    {
        return new AvifReader(null, options);
    }

    public AvifReader CreateNonSeekableStreamReader(
        Stream? source,
        AvifDecoderOptions options)
    {
        return new AvifReader(null, options);
    }

    public AvifReader CreateRangeReader(
        IAvifRandomAccessSource? source,
        AvifDecoderOptions options)
    {
        return new AvifReader(null, options);
    }
}
