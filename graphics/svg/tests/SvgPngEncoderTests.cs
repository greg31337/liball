namespace svg.Tests;

public class SvgPngEncoderTests
{
    [Test]
    public void EncodesDeterministicPngAndRoundTripsPremultipliedPixels()
    {
        SvgPixelBuffer pixels = Buffer(3, 2, SvgPixelColorSpace.Srgb);
        pixels.Write(0, 0, new SvgPremultipliedRgba8(255, 0, 0, 255));
        pixels.Write(1, 0, SvgPremultipliedRgba8.FromStraight(200, 100, 50, 128));
        pixels.Write(2, 0, new SvgPremultipliedRgba8(0, 0, 0, 0));
        pixels.Write(0, 1, new SvgPremultipliedRgba8(0, 255, 0, 255));
        pixels.Write(1, 1, new SvgPremultipliedRgba8(0, 0, 255, 255));
        pixels.Write(2, 1, new SvgPremultipliedRgba8(255, 255, 255, 255));

        SvgPngEncodeResult first = new SvgPngCodec().EncodeWithPixels(pixels);
        SvgPngEncodeResult second = new SvgPngCodec().EncodeWithPixels(pixels);
        SvgPngProbeResult probe = new SvgPngCodec().ProbeWithBytes(first.Bytes);
        SvgPngDecodeResult decoded = new SvgPngCodec().DecodeWithBytes(first.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Error, Is.EqualTo(SvgPngEncodeError.None));
            Assert.That(first.BytesWritten, Is.EqualTo(first.Bytes.Length));
            Assert.That(second.Bytes, Is.EqualTo(first.Bytes));
            Assert.That(probe.Success, Is.True);
            Assert.That(probe.Width, Is.EqualTo(3));
            Assert.That(probe.Height, Is.EqualTo(2));
            Assert.That(probe.BitDepth, Is.EqualTo(8));
            Assert.That(probe.ColorType, Is.EqualTo(6));
            Assert.That(probe.InterlaceMethod, Is.Zero);
            Assert.That(decoded.Success, Is.True);
        });
        AssertSamePixels(pixels, decoded.Pixels);
        byte[] scanlines = InflateIdat(first.Bytes);
        int rowBytes = pixels.Width * 4;
        int y = 0;
        while (y < pixels.Height)
        {
            Assert.That(scanlines[y * (rowBytes + 1)], Is.InRange(0, 4));
            y++;
        }
    }

    [TestCase(SvgPngFilterMode.None, 0)]
    [TestCase(SvgPngFilterMode.Sub, 1)]
    [TestCase(SvgPngFilterMode.Up, 2)]
    [TestCase(SvgPngFilterMode.Average, 3)]
    [TestCase(SvgPngFilterMode.Paeth, 4)]
    public void AppliesRequestedFixedFilterToEveryRow(SvgPngFilterMode mode, int expectedFilter)
    {
        SvgPixelBuffer pixels = PatternBuffer();
        SvgPngEncodeOptions options = new SvgPngEncodeOptions();
        options.FilterMode = mode;

        SvgPngEncodeResult encoded = new SvgPngCodec().EncodeWithPixelsAndOptions(pixels, options);
        SvgPngDecodeResult decoded = new SvgPngCodec().DecodeWithBytes(encoded.Bytes);
        byte[] scanlines = InflateIdat(encoded.Bytes);

        Assert.That(encoded.Success, Is.True);
        Assert.That(decoded.Success, Is.True);
        AssertSamePixels(pixels, decoded.Pixels);
        int rowBytes = pixels.Width * 4;
        int y = 0;
        while (y < pixels.Height)
        {
            Assert.That(scanlines[y * (rowBytes + 1)], Is.EqualTo(expectedFilter));
            y++;
        }
    }

    [TestCase(SvgPngCompressionMode.Fastest)]
    [TestCase(SvgPngCompressionMode.Balanced)]
    [TestCase(SvgPngCompressionMode.Smallest)]
    public void SupportsDeterministicCompressionModes(SvgPngCompressionMode mode)
    {
        SvgPixelBuffer pixels = PatternBuffer();
        SvgPngEncodeOptions options = new SvgPngEncodeOptions();
        options.CompressionMode = mode;

        SvgPngEncodeResult first = new SvgPngCodec().EncodeWithPixelsAndOptions(pixels, options);
        SvgPngEncodeResult second = new SvgPngCodec().EncodeWithPixelsAndOptions(pixels, options);
        SvgPngDecodeResult decoded = new SvgPngCodec().DecodeWithBytes(first.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(second.Bytes, Is.EqualTo(first.Bytes));
            Assert.That(decoded.Success, Is.True);
        });
        AssertSamePixels(pixels, decoded.Pixels);
    }

    [Test]
    public void WritesRequiredChunksColorAndPhysicalMetadataWithValidCrcs()
    {
        SvgPngEncodeOptions options = new SvgPngEncodeOptions();
        options.FilterMode = SvgPngFilterMode.None;
        options.PixelsPerMeterX = 3780;
        options.PixelsPerMeterY = 3780;

        SvgPngEncodeResult encoded = new SvgPngCodec().EncodeWithPixelsAndOptions(PatternBuffer(), options);
        List<string> chunks = ChunkNames(encoded.Bytes);
        int srgb = FindChunk(encoded.Bytes, "sRGB");
        int physical = FindChunk(encoded.Bytes, "pHYs");

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(chunks, Is.EqualTo(new[] { "IHDR", "sRGB", "pHYs", "IDAT", "IEND" }));
            Assert.That(ReadUInt32(encoded.Bytes, 8), Is.EqualTo(13));
            Assert.That(encoded.Bytes[24], Is.EqualTo(8));
            Assert.That(encoded.Bytes[25], Is.EqualTo(6));
            Assert.That(encoded.Bytes[srgb + 8], Is.Zero);
            Assert.That(ReadUInt32(encoded.Bytes, physical + 8), Is.EqualTo(3780));
            Assert.That(ReadUInt32(encoded.Bytes, physical + 12), Is.EqualTo(3780));
            Assert.That(encoded.Bytes[physical + 16], Is.EqualTo(1));
            Assert.That(AllChunkCrcsValid(encoded.Bytes), Is.True);
        });
    }

    [Test]
    public void ConvertsLinearLightPixelsToTaggedSrgb()
    {
        SvgPixelBuffer pixels = Buffer(1, 1, SvgPixelColorSpace.SrgbLinear);
        pixels.Write(0, 0, new SvgPremultipliedRgba8(55, 55, 55, 255));

        SvgPngEncodeResult encoded = new SvgPngCodec().EncodeWithPixels(pixels);
        SvgPngDecodeResult decoded = new SvgPngCodec().DecodeWithBytes(encoded.Bytes);
        SvgPixelReadResult pixel = decoded.Pixels.Read(0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(pixel.Pixel.Red, Is.EqualTo(128));
            Assert.That(pixel.Pixel.Green, Is.EqualTo(128));
            Assert.That(pixel.Pixel.Blue, Is.EqualTo(128));
            Assert.That(pixel.Pixel.Alpha, Is.EqualTo(255));
            Assert.That(ChunkNames(encoded.Bytes), Does.Contain("sRGB"));
        });
    }

    [Test]
    public void WritesStreamAndBase64DataUrlOutputs()
    {
        SvgPixelBuffer pixels = PatternBuffer();
        SvgPngEncodeResult bytes = new SvgPngCodec().EncodeWithPixels(pixels);
        using MemoryStream destination = new MemoryStream();

        SvgPngEncodeResult streamed = new SvgPngCodec().EncodeWithPixelsAndDestination(pixels, destination);
        SvgPngEncodeResult dataUrl = new SvgPngCodec().EncodeDataUrlWithPixels(pixels);
        SvgDataUrlResult parsed = new SvgDataUrl().ParseWithValueAndMaxDecodedBytes(dataUrl.DataUrl, dataUrl.Bytes.Length);

        Assert.Multiple(() =>
        {
            Assert.That(streamed.Success, Is.True);
            Assert.That(streamed.BytesWritten, Is.EqualTo(bytes.Bytes.Length));
            Assert.That(destination.ToArray(), Is.EqualTo(bytes.Bytes));
            Assert.That(dataUrl.Success, Is.True);
            Assert.That(dataUrl.DataUrl, Does.StartWith("data:image/png;base64,"));
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.IsBase64, Is.True);
            Assert.That(parsed.MediaType.Type, Is.EqualTo("image"));
            Assert.That(parsed.MediaType.Subtype, Is.EqualTo("png"));
            Assert.That(parsed.Bytes, Is.EqualTo(bytes.Bytes));
        });
    }

    [Test]
    public void RejectsInvalidBuffersOptionsLimitsAndStreams()
    {
        SvgPixelBuffer pixels = PatternBuffer();
        SvgPngEncodeOptions dimensions = new SvgPngEncodeOptions();
        dimensions.RasterLimits.MaxWidth = 1;
        SvgPngEncodeOptions filteredBytes = new SvgPngEncodeOptions();
        filteredBytes.RasterLimits.MaxPngDecodedBytes = 1;
        SvgPngEncodeOptions encodedBytes = new SvgPngEncodeOptions();
        encodedBytes.MaximumEncodedBytes = 50;
        SvgPngEncodeOptions physical = new SvgPngEncodeOptions();
        physical.PixelsPerMeterX = 96;
        SvgPixelBuffer invalidPixel = PatternBuffer();
        invalidPixel.Data[0] = 255;
        invalidPixel.Data[3] = 1;
        using MemoryStream readOnly = new MemoryStream(new byte[16], false);
        using ThrowingWriteStream throwing = new ThrowingWriteStream();

        SvgPngEncodeResult dimensionResult = new SvgPngCodec().EncodeWithPixelsAndOptions(pixels, dimensions);
        SvgPngEncodeResult filteredResult = new SvgPngCodec().EncodeWithPixelsAndOptions(pixels, filteredBytes);
        SvgPngEncodeResult encodedResult = new SvgPngCodec().EncodeWithPixelsAndOptions(pixels, encodedBytes);
        SvgPngEncodeResult physicalResult = new SvgPngCodec().EncodeWithPixelsAndOptions(pixels, physical);
        SvgPngEncodeResult pixelResult = new SvgPngCodec().EncodeWithPixels(invalidPixel);
        SvgPngEncodeResult readOnlyResult = new SvgPngCodec().EncodeWithPixelsAndDestination(pixels, readOnly);
        SvgPngEncodeResult throwingResult = new SvgPngCodec().EncodeWithPixelsAndDestination(pixels, throwing);

        Assert.Multiple(() =>
        {
            Assert.That(dimensionResult.Error, Is.EqualTo(SvgPngEncodeError.DimensionLimitExceeded));
            Assert.That(filteredResult.Error, Is.EqualTo(SvgPngEncodeError.ByteLimitExceeded));
            Assert.That(encodedResult.Error, Is.EqualTo(SvgPngEncodeError.ByteLimitExceeded));
            Assert.That(physicalResult.Error, Is.EqualTo(SvgPngEncodeError.InvalidArgument));
            Assert.That(pixelResult.Error, Is.EqualTo(SvgPngEncodeError.InvalidArgument));
            Assert.That(readOnlyResult.Error, Is.EqualTo(SvgPngEncodeError.InvalidArgument));
            Assert.That(throwingResult.Error, Is.EqualTo(SvgPngEncodeError.StreamWriteFailed));
        });
    }

    static SvgPixelBuffer PatternBuffer()
    {
        SvgPixelBuffer result = Buffer(4, 3, SvgPixelColorSpace.Srgb);
        int y = 0;
        while (y < result.Height)
        {
            int x = 0;
            while (x < result.Width)
            {
                byte alpha = (byte)(80 + x * 40 + y * 5);
                byte red = (byte)(30 + x * 35);
                byte green = (byte)(20 + y * 50);
                byte blue = (byte)(10 + x * 10 + y * 20);
                result.Write(x, y, SvgPremultipliedRgba8.FromStraight(red, green, blue, alpha));
                x++;
            }
            y++;
        }
        return result;
    }

    static SvgPixelBuffer Buffer(int width, int height, SvgPixelColorSpace colorSpace)
    {
        SvgPixelBufferResult created = SvgPixelBuffer.Create(width, height, colorSpace);
        Assert.That(created.Success, Is.True);
        return created.Buffer;
    }

    static void AssertSamePixels(SvgPixelBuffer expected, SvgPixelBuffer actual)
    {
        Assert.That(actual.Width, Is.EqualTo(expected.Width));
        Assert.That(actual.Height, Is.EqualTo(expected.Height));
        int y = 0;
        while (y < expected.Height)
        {
            int x = 0;
            while (x < expected.Width)
            {
                SvgPremultipliedRgba8 left = expected.Read(x, y).Pixel;
                SvgPremultipliedRgba8 right = actual.Read(x, y).Pixel;
                Assert.Multiple(() =>
                {
                    Assert.That(right.Red, Is.EqualTo(left.Red), $"red at {x},{y}");
                    Assert.That(right.Green, Is.EqualTo(left.Green), $"green at {x},{y}");
                    Assert.That(right.Blue, Is.EqualTo(left.Blue), $"blue at {x},{y}");
                    Assert.That(right.Alpha, Is.EqualTo(left.Alpha), $"alpha at {x},{y}");
                });
                x++;
            }
            y++;
        }
    }

    static byte[] InflateIdat(byte[] png)
    {
        using MemoryStream compressed = new MemoryStream();
        int offset = 8;
        while (offset < png.Length)
        {
            int length = ReadLength(png, offset);
            string type = System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
            if (type == "IDAT") compressed.Write(png, offset + 8, length);
            offset += 12 + length;
        }
        compressed.Position = 0;
        using System.IO.Compression.ZLibStream zlib = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionMode.Decompress);
        using MemoryStream output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    static List<string> ChunkNames(byte[] png)
    {
        List<string> result = new List<string>();
        int offset = 8;
        while (offset < png.Length)
        {
            int length = ReadLength(png, offset);
            result.Add(System.Text.Encoding.ASCII.GetString(png, offset + 4, 4));
            offset += 12 + length;
        }
        return result;
    }

    static int FindChunk(byte[] png, string target)
    {
        int offset = 8;
        while (offset < png.Length)
        {
            int length = ReadLength(png, offset);
            string type = System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
            if (type == target) return offset;
            offset += 12 + length;
        }
        return -1;
    }

    static bool AllChunkCrcsValid(byte[] png)
    {
        int offset = 8;
        while (offset < png.Length)
        {
            int length = ReadLength(png, offset);
            uint expected = ReadUInt32(png, offset + 8 + length);
            if (Crc(png, offset + 4, length + 4) != expected) return false;
            offset += 12 + length;
        }
        return offset == png.Length;
    }

    static int ReadLength(byte[] png, int offset)
    {
        uint value = ReadUInt32(png, offset);
        if (value > int.MaxValue) throw new OverflowException();
        return (int)value;
    }

    static uint Crc(byte[] bytes, int offset, int count)
    {
        uint crc = 0xffffffff;
        int index = offset;
        while (index < offset + count)
        {
            crc ^= bytes[index];
            int bit = 0;
            while (bit < 8)
            {
                crc = (crc & 1) == 0 ? crc >> 1 : 0xedb88320 ^ crc >> 1;
                bit++;
            }
            index++;
        }
        return crc ^ 0xffffffff;
    }

    static uint ReadUInt32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }

    class ThrowingWriteStream : MemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new IOException("write failed");
        }
    }
}
