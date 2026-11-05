using System.Buffers.Binary;

namespace avif.Tests;

public class AvifPackedOneCallTests
{
    [Test]
    public void PackedEncodeAndDecodeCarrySelectedDepthColorAlphaAndLayout()
    {
        IAvifCodec codec = new AvifCodec();
        AvifPixelBuffer source = CreateRgbaPixels();
        AvifPackedEncodeOptions encodeOptions = CreateEncodeOptions();

        AvifEncodeResult encoded = codec.EncodePacked(source, encodeOptions);
        AvifDecodeResult planar = codec.DecodePlanarBytes(
            encoded.GetData(),
            new AvifDecoderOptions());
        AvifPackedDecodeOptions eightBitOptions =
            new AvifPackedDecodeOptions();
        AvifPixelBuffer eightBit = codec.DecodePackedBytes(
            encoded.GetData(),
            eightBitOptions);
        AvifPackedDecodeOptions twelveBitOptions =
            new AvifPackedDecodeOptions();
        twelveBitOptions.PixelFormat = AvifPixelFormat.Bgra;
        twelveBitOptions.BitDepth = 12;
        AvifPixelBuffer twelveBit = codec.DecodePackedSpan(
            encoded.GetData().AsSpan(),
            twelveBitOptions);
        AvifImage decodedImage = planar.GetImage()
            ?? throw new InvalidOperationException("Invalid internal state.");
        AvifColorInfo decodedColor = decodedImage.GetColorInfo()
            ?? throw new InvalidOperationException("Invalid internal state.");

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(planar.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(decodedImage.GetBitDepth(), Is.EqualTo(10));
            Assert.That(decodedImage.GetChromaFormat(),
                Is.EqualTo(AvifChromaFormat.Yuv444));
            Assert.That(decodedImage.GetAlphaMode(),
                Is.EqualTo(AvifAlphaMode.Straight));
            Assert.That(decodedColor.ColorPrimaries,
                Is.EqualTo(AvifColorPrimaries.Bt709));
            Assert.That(decodedColor.TransferCharacteristics,
                Is.EqualTo(AvifTransferCharacteristics.Srgb));
            Assert.That(decodedColor.MatrixCoefficients,
                Is.EqualTo(AvifMatrixCoefficients.Identity));
            Assert.That(decodedColor.Range, Is.EqualTo(AvifRange.Full));
            Assert.That(eightBit.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(eightBit.GetPixelFormat(),
                Is.EqualTo(AvifPixelFormat.Rgba));
            Assert.That(eightBit.GetBitDepth(), Is.EqualTo(8));
            Assert.That(eightBit.GetRowSpan(0).ToArray(),
                Is.EqualTo(source.GetRowSpan(0).ToArray()).Within(1));
            Assert.That(twelveBit.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(twelveBit.GetPixelFormat(),
                Is.EqualTo(AvifPixelFormat.Bgra));
            Assert.That(twelveBit.GetBitDepth(), Is.EqualTo(12));
            Assert.That(twelveBit.GetBytesPerPixel(), Is.EqualTo(8));
            Assert.That(ReadUInt16(twelveBit.GetRowSpan(0), 6),
                Is.EqualTo(514).Within(3));
        });

        decodedImage.Close();
        eightBit.Close();
        twelveBit.Close();
        source.Close();
    }

    [Test]
    public void PackedDecodeSupportsLinearHalfFloatAndRenderingAlphaPolicy()
    {
        IAvifCodec codec = new AvifCodec();
        AvifPixelBuffer source = CreateRgbaPixels();
        AvifEncodeResult encoded = codec.EncodePacked(
            source,
            CreateEncodeOptions());
        AvifPackedDecodeOptions linearOptions =
            new AvifPackedDecodeOptions();
        linearOptions.PixelFormat = AvifPixelFormat.Rgba;
        linearOptions.BitDepth = 16;
        linearOptions.FloatingPoint = true;
        linearOptions.RenderOptions.PreserveAlpha = false;
        AvifPixelBuffer linear = codec.DecodePackedBytes(
            encoded.GetData(),
            linearOptions);

        Assert.Multiple(() =>
        {
            Assert.That(linear.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(linear.IsFloatingPoint(), Is.True);
            Assert.That(linear.GetBitDepth(), Is.EqualTo(16));
            Assert.That(ReadHalf(linear.GetRowSpan(0), 0),
                Is.EqualTo(1).Within(0.002));
            Assert.That(ReadHalf(linear.GetRowSpan(0), 2),
                Is.EqualTo(0).Within(0.002));
            Assert.That(ReadHalf(linear.GetRowSpan(0), 6),
                Is.EqualTo(1).Within(0.002));
        });

        linear.Close();
        source.Close();
    }

    [Test]
    public void PackedDecodeAppliesCallerSelectedItemTransforms()
    {
        AvifImage image = new AvifImage(
            2,
            1,
            8,
            AvifChromaFormat.Monochrome,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.None,
            1,
            new AvifSecurityLimits());
        (image.GetPlane(AvifPlaneKind.Y)
            ?? throw new InvalidOperationException("Invalid internal state."))
            .GetRowSpan(0).Fill(80);
        AvifWriter writer = new AvifWriter(new AvifEncoderOptions());
        uint itemId = writer.AddImage(image);
        AvifTransform transform = new AvifTransform();
        transform.SetRotation(1);
        writer.SetTransform(itemId, transform);
        byte[] encoded = writer.Finish().GetData();
        IAvifCodec codec = new AvifCodec();
        AvifPackedDecodeOptions transformedOptions =
            new AvifPackedDecodeOptions();
        transformedOptions.PixelFormat = AvifPixelFormat.Gray;
        AvifPackedDecodeOptions rawOptions = new AvifPackedDecodeOptions();
        rawOptions.PixelFormat = AvifPixelFormat.Gray;
        rawOptions.RenderOptions.ApplyRotation = false;

        AvifPixelBuffer transformed = codec.DecodePackedBytes(
            encoded,
            transformedOptions);
        AvifPixelBuffer raw = codec.DecodePackedBytes(encoded, rawOptions);

        Assert.Multiple(() =>
        {
            Assert.That(transformed.GetCode(),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(transformed.GetWidth(), Is.EqualTo(1));
            Assert.That(transformed.GetHeight(), Is.EqualTo(2));
            Assert.That(raw.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(raw.GetWidth(), Is.EqualTo(2));
            Assert.That(raw.GetHeight(), Is.EqualTo(1));
        });

        transformed.Close();
        raw.Close();
        writer.Close();
        image.Close();
    }

    [Test]
    public void PackedStreamAndFileOperationsRoundTripWithoutTakingOwnership()
    {
        IAvifCodec codec = new AvifCodec();
        AvifPixelBuffer source = CreateRgbaPixels();
        AvifPackedEncodeOptions encodeOptions = CreateEncodeOptions();
        MemoryStream destination = new MemoryStream();
        AvifResultCode streamWrite = codec.WritePackedStream(
            destination,
            source,
            encodeOptions);
        destination.Position = 0;
        AvifPixelBuffer streamDecoded = codec.DecodePackedStream(
            destination,
            new AvifPackedDecodeOptions());
        string directory = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "packed-one-call-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "packed.avif");
        Directory.CreateDirectory(directory);
        AvifPixelBuffer fileDecoded = AvifPixelBuffer.Create(
            AvifResultCode.InvalidState);

        try
        {
            AvifResultCode fileWrite = codec.WritePackedFile(
                path,
                source,
                encodeOptions);
            fileDecoded.Close();
            fileDecoded = codec.DecodePackedFile(
                path,
                new AvifPackedDecodeOptions());
            using (FileStream exclusive = new FileStream(
                path,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None))
            {
                Assert.That(exclusive.CanWrite, Is.True);
            }

            Assert.Multiple(() =>
            {
                Assert.That(streamWrite, Is.EqualTo(AvifResultCode.Success));
                Assert.That(destination.CanRead, Is.True);
                Assert.That(streamDecoded.GetCode(),
                    Is.EqualTo(AvifResultCode.Success));
                Assert.That(fileWrite, Is.EqualTo(AvifResultCode.Success));
                Assert.That(fileDecoded.GetCode(),
                    Is.EqualTo(AvifResultCode.Success));
            });
        }
        finally
        {
            fileDecoded.Close();
            streamDecoded.Close();
            destination.Dispose();
            source.Close();
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void PackedOperationsHonorCancellationLimitsAndFormatValidation()
    {
        IAvifCodec codec = new AvifCodec();
        AvifPixelBuffer source = CreateRgbaPixels();
        AvifPackedEncodeOptions encodeOptions = CreateEncodeOptions();
        byte[] encoded = codec.EncodePacked(source, encodeOptions).GetData();
        AvifPackedDecodeOptions cancelledDecode =
            new AvifPackedDecodeOptions();
        cancelledDecode.Cancellation.Cancel();
        AvifPackedEncodeOptions cancelledEncode = CreateEncodeOptions();
        cancelledEncode.Cancellation.Cancel();
        AvifPackedDecodeOptions limitedDecode =
            new AvifPackedDecodeOptions();
        limitedDecode.Limits.MaxWidth = 1;
        AvifPackedEncodeOptions limitedEncode = CreateEncodeOptions();
        limitedEncode.Limits.MaxWidth = 1;
        AvifPackedEncodeOptions invalidEncoder = CreateEncodeOptions();
        invalidEncoder.EncoderOptions.WorkerCount = 0;
        AvifPackedDecodeOptions invalidFloat =
            new AvifPackedDecodeOptions();
        invalidFloat.PixelFormat = AvifPixelFormat.Gray;
        invalidFloat.BitDepth = 16;
        invalidFloat.FloatingPoint = true;

        AvifPixelBuffer cancelledPixels = codec.DecodePackedBytes(
            encoded,
            cancelledDecode);
        AvifEncodeResult cancelledBytes = codec.EncodePacked(
            source,
            cancelledEncode);
        AvifPixelBuffer limitedPixels = codec.DecodePackedBytes(
            encoded,
            limitedDecode);
        AvifEncodeResult limitedBytes = codec.EncodePacked(
            source,
            limitedEncode);
        AvifEncodeResult invalidEncoderBytes = codec.EncodePacked(
            source,
            invalidEncoder);
        AvifPixelBuffer invalidPixels = codec.DecodePackedBytes(
            encoded,
            invalidFloat);

        Assert.Multiple(() =>
        {
            Assert.That(cancelledPixels.GetCode(),
                Is.EqualTo(AvifResultCode.Cancelled));
            Assert.That(cancelledBytes.GetCode(),
                Is.EqualTo(AvifResultCode.Cancelled));
            Assert.That(limitedPixels.GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(limitedBytes.GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(invalidEncoderBytes.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(invalidPixels.GetCode(),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(codec.DecodePackedBytes(null,
                    new AvifPackedDecodeOptions()).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(codec.WritePackedStream(null,
                    source,
                    encodeOptions),
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        cancelledPixels.Close();
        limitedPixels.Close();
        invalidPixels.Close();
        source.Close();
    }

    static AvifPixelBuffer CreateRgbaPixels()
    {
        AvifPixelBuffer pixels = new AvifPixelBuffer(
            2,
            1,
            AvifPixelFormat.Rgba,
            8,
            false,
            1,
            new AvifSecurityLimits());
        new byte[]
        {
            255, 0, 0, 32,
            0, 255, 0, 192
        }.AsSpan().CopyTo(pixels.GetRowSpan(0));
        return pixels;
    }

    static AvifPackedEncodeOptions CreateEncodeOptions()
    {
        AvifPackedEncodeOptions options = new AvifPackedEncodeOptions();
        options.BitDepth = 10;
        options.ChromaFormat = AvifChromaFormat.Yuv444;
        options.ChromaSamplePosition = AvifChromaSamplePosition.Unknown;
        options.ColorInfo = new AvifColorInfo(
            AvifColorPrimaries.Bt709,
            AvifTransferCharacteristics.Srgb,
            AvifMatrixCoefficients.Identity,
            AvifRange.Full);
        options.AlphaMode = AvifAlphaMode.Straight;
        return options;
    }

    static int ReadUInt16(Span<byte> row, int offset)
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(row.Slice(offset, 2));
    }

    static double ReadHalf(Span<byte> row, int offset)
    {
        ushort bits = BinaryPrimitives.ReadUInt16LittleEndian(
            row.Slice(offset, 2));
        return (double)BitConverter.UInt16BitsToHalf(bits);
    }
}
