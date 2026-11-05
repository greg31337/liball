namespace avif.Tests;

public class AvifColorConverterTests
{
    static IEnumerable<TestCaseData> PlanarCases()
    {
        AvifChromaFormat[] formats = Enum.GetValues<AvifChromaFormat>();
        int[] depths = new int[] { 8, 10, 12, 16 };
        AvifRange[] ranges = new AvifRange[]
        {
            AvifRange.Full,
            AvifRange.Limited
        };
        foreach (AvifChromaFormat format in formats)
        {
            foreach (int depth in depths)
            {
                foreach (AvifRange range in ranges)
                {
                    yield return new TestCaseData(format, depth, range);
                }
            }
        }
    }

    [TestCaseSource(nameof(PlanarCases))]
    public void YuvToRgbHandlesEveryDepthLayoutAndRange(
        AvifChromaFormat format,
        int depth,
        AvifRange range)
    {
        AvifImage image = CreateImage(3, 3, depth, format, range, false);
        int minimum = range == AvifRange.Full ? 0 : 16 << (depth - 8);
        int maximum = range == AvifRange.Full
            ? (1 << depth) - 1 : 235 << (depth - 8);
        int neutral = 1 << (depth - 1);
        FillPlane(image.GetPlane(AvifPlaneKind.Y), minimum);
        WriteSample(image.GetPlane(AvifPlaneKind.Y), 1, 0, maximum);
        FillPlane(image.GetPlane(AvifPlaneKind.U), neutral);
        FillPlane(image.GetPlane(AvifPlaneKind.V), neutral);

        AvifPixelBuffer pixels = AvifColorConverter.Create().Convert(
            image, AvifPixelFormat.Rgb);

        Assert.Multiple(() =>
        {
            Assert.That(pixels.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(pixels.GetRowSpan(0).Slice(0, 3).ToArray(),
                Is.EqualTo(new byte[] { 0, 0, 0 }));
            Assert.That(pixels.GetRowSpan(0).Slice(3, 3).ToArray(),
                Is.EqualTo(new byte[] { 255, 255, 255 }));
        });

        pixels.Close();
        image.Close();
    }

    [Test]
    public void IdentityConversionWritesEveryPackedOrderAndPreservesAlpha()
    {
        AvifImage image = CreateImage(
            1, 1, 8, AvifChromaFormat.Yuv444, AvifRange.Full, true);
        image.SetColorInfo(AvifColorInfo.Create(
            AvifColorPrimaries.Bt709,
            AvifTransferCharacteristics.Srgb,
            AvifMatrixCoefficients.Identity,
            AvifRange.Full));
        WriteSample(image.GetPlane(AvifPlaneKind.Y), 0, 0, 0);
        WriteSample(image.GetPlane(AvifPlaneKind.U), 0, 0, 0);
        WriteSample(image.GetPlane(AvifPlaneKind.V), 0, 0, 255);
        WriteSample(image.GetPlane(AvifPlaneKind.Alpha), 0, 0, 128);
        AvifColorConverter converter = AvifColorConverter.Create();

        AssertPacked(converter.Convert(image, AvifPixelFormat.Gray),
            new byte[] { 54 });
        AssertPacked(converter.Convert(image, AvifPixelFormat.GrayAlpha),
            new byte[] { 54, 128 });
        AssertPacked(converter.Convert(image, AvifPixelFormat.Rgb),
            new byte[] { 255, 0, 0 });
        AssertPacked(converter.Convert(image, AvifPixelFormat.Rgba),
            new byte[] { 255, 0, 0, 128 });
        AssertPacked(converter.Convert(image, AvifPixelFormat.Bgr),
            new byte[] { 0, 0, 255 });
        AssertPacked(converter.Convert(image, AvifPixelFormat.Bgra),
            new byte[] { 0, 0, 255, 128 });
        AssertPacked(converter.Convert(image, AvifPixelFormat.Argb),
            new byte[] { 128, 255, 0, 0 });
        AssertPacked(converter.Convert(image, AvifPixelFormat.Abgr),
            new byte[] { 128, 0, 0, 255 });
        AssertPacked(converter.Convert(image, AvifPixelFormat.Rgb565),
            new byte[] { 0, 248 });

        AvifPixelBuffer rgba16 = converter.ConvertWithDepth(
            image, AvifPixelFormat.Rgba, 16, false);
        Assert.That(ReadInteger(rgba16.GetRowSpan(0), 6), Is.EqualTo(32896));
        rgba16.Close();
        image.Close();
    }

    [Test]
    public void FloatingPointOutputContainsLinearRgbAndAlpha()
    {
        AvifImage image = CreateImage(
            1, 1, 8, AvifChromaFormat.Yuv444, AvifRange.Full, true);
        image.SetColorInfo(AvifColorInfo.Create(
            AvifColorPrimaries.Bt709,
            AvifTransferCharacteristics.Srgb,
            AvifMatrixCoefficients.Identity,
            AvifRange.Full));
        FillPlane(image.GetPlane(AvifPlaneKind.Y), 128);
        FillPlane(image.GetPlane(AvifPlaneKind.U), 128);
        FillPlane(image.GetPlane(AvifPlaneKind.V), 128);
        FillPlane(image.GetPlane(AvifPlaneKind.Alpha), 64);

        AvifPixelBuffer pixels = AvifColorConverter.Create().ConvertWithDepth(
            image, AvifPixelFormat.Rgba, 16, true);

        Assert.Multiple(() =>
        {
            Assert.That(ReadHalf(pixels.GetRowSpan(0), 0),
                Is.EqualTo(0.2159).Within(0.001));
            Assert.That(ReadHalf(pixels.GetRowSpan(0), 2),
                Is.EqualTo(0.2159).Within(0.001));
            Assert.That(ReadHalf(pixels.GetRowSpan(0), 4),
                Is.EqualTo(0.2159).Within(0.001));
            Assert.That(ReadHalf(pixels.GetRowSpan(0), 6),
                Is.EqualTo(64.0 / 255).Within(0.001));
        });

        pixels.Close();
        image.Close();
    }

    [TestCaseSource(nameof(PlanarCases))]
    public void RgbToYuvHandlesEveryDepthLayoutAndRange(
        AvifChromaFormat format,
        int depth,
        AvifRange range)
    {
        AvifPixelBuffer pixels = AvifPixelBuffer.Create(
            3, 3, AvifPixelFormat.Rgb, 8, false);
        for (int y = 0; y < 3; y++)
        {
            Span<byte> row = pixels.GetRowSpan(y);
            for (int x = 0; x < 3; x++)
            {
                byte value = x == 1 ? (byte)255 : (byte)0;
                row[x * 3] = value;
                row[x * 3 + 1] = value;
                row[x * 3 + 2] = value;
            }
        }
        AvifImage image = CreateImage(3, 3, depth, format, range, false);

        AvifResultCode result = AvifColorConverter.Create().ConvertToImage(
            pixels, image);
        int minimum = range == AvifRange.Full ? 0 : 16 << (depth - 8);
        int maximum = range == AvifRange.Full
            ? (1 << depth) - 1 : 235 << (depth - 8);
        int neutral = 1 << (depth - 1);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(image.GetPlane(AvifPlaneKind.Y), 0, 0),
                Is.EqualTo(minimum));
            Assert.That(ReadSample(image.GetPlane(AvifPlaneKind.Y), 1, 0),
                Is.EqualTo(maximum));
            if (format != AvifChromaFormat.Monochrome)
            {
                Assert.That(ReadSample(
                    image.GetPlane(AvifPlaneKind.U), 0, 0),
                    Is.EqualTo(neutral).Within(1));
                Assert.That(ReadSample(
                    image.GetPlane(AvifPlaneKind.V), 0, 0),
                    Is.EqualTo(neutral).Within(1));
            }
        });

        image.Close();
        pixels.Close();
    }

    [Test]
    public void RgbRoundTripPreservesColorAndAlphaAtHighDepth()
    {
        AvifPixelBuffer source = AvifPixelBuffer.Create(
            3, 1, AvifPixelFormat.Rgba, 8, false);
        new byte[]
        {
            255, 0, 0, 32,
            0, 255, 0, 128,
            0, 0, 255, 255
        }.AsSpan().CopyTo(source.GetRowSpan(0));
        AvifImage image = CreateImage(
            3, 1, 12, AvifChromaFormat.Yuv444, AvifRange.Full, true);
        AvifColorConverter converter = AvifColorConverter.Create();

        AvifResultCode toYuv = converter.ConvertToImage(source, image);
        AvifPixelBuffer restored = converter.Convert(
            image, AvifPixelFormat.Rgba);

        Assert.Multiple(() =>
        {
            Assert.That(toYuv, Is.EqualTo(AvifResultCode.Success));
            Assert.That(restored.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(restored.GetRowSpan(0).ToArray(),
                Is.EqualTo(source.GetRowSpan(0).ToArray()).Within(1));
        });

        restored.Close();
        image.Close();
        source.Close();
    }

    [Test]
    public void PremultipliedAssociationIsConvertedInThePackedSampleDomain()
    {
        AvifImage image = AvifImage.Create(
            1,
            1,
            8,
            AvifChromaFormat.Yuv444,
            AvifRange.Full,
            AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.Premultiplied,
            1,
            new AvifSecurityLimits());
        image.SetColorInfo(AvifColorInfo.Create(
            AvifColorPrimaries.Bt709,
            AvifTransferCharacteristics.Srgb,
            AvifMatrixCoefficients.Identity,
            AvifRange.Full));
        FillPlane(image.GetPlane(AvifPlaneKind.Y), 0);
        FillPlane(image.GetPlane(AvifPlaneKind.U), 0);
        FillPlane(image.GetPlane(AvifPlaneKind.V), 128);
        FillPlane(image.GetPlane(AvifPlaneKind.Alpha), 128);
        AvifColorConverter straightConverter = AvifColorConverter.Create();
        AvifColorConversionOptions premultipliedOptions =
            new AvifColorConversionOptions();
        premultipliedOptions.OutputPremultipliedAlpha = true;
        AvifColorConverter premultipliedConverter =
            AvifColorConverter.Create(premultipliedOptions);

        AvifPixelBuffer straight = straightConverter.Convert(
            image, AvifPixelFormat.Rgba);
        AvifPixelBuffer premultiplied = premultipliedConverter.Convert(
            image, AvifPixelFormat.Rgba);

        Assert.Multiple(() =>
        {
            Assert.That(straight.GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 255, 0, 0, 128 }));
            Assert.That(premultiplied.GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 128, 0, 0, 128 }));
        });

        straight.Close();
        premultiplied.Close();
        image.Close();
    }

    [Test]
    public void ConverterOptionsAreIndependentSnapshots()
    {
        AvifColorConversionOptions requested =
            new AvifColorConversionOptions();
        requested.ResamplingFilter = AvifResamplingFilter.Lanczos3;
        requested.DitherMode = AvifDitherMode.Ordered;
        requested.WorkerCount = 3;
        AvifColorConverter converter = AvifColorConverter.Create(requested);
        requested.WorkerCount = 0;
        AvifColorConversionOptions? first = converter.GetOptions();
        (first ?? throw new InvalidOperationException("Invalid internal state.")).ResamplingFilter = AvifResamplingFilter.Nearest;
        first.WorkerCount = 1;
        AvifColorConversionOptions? second = converter.GetOptions();

        Assert.Multiple(() =>
        {
            Assert.That((second ?? throw new InvalidOperationException("Invalid internal state.")).ResamplingFilter,
                Is.EqualTo(AvifResamplingFilter.Lanczos3));
            Assert.That(second.DitherMode, Is.EqualTo(AvifDitherMode.Ordered));
            Assert.That(second.WorkerCount, Is.EqualTo(3));
            Assert.That(converter.SetOptions(null),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(AvifColorConverter.Create(null).GetOptions(), Is.Null);
        });
    }

    [Test]
    public void ConversionValidatesStateDimensionsOptionsAndIdentityLayout()
    {
        AvifColorConverter converter = AvifColorConverter.Create();
        AvifImage image = CreateImage(
            2, 2, 8, AvifChromaFormat.Yuv444, AvifRange.Full, false);
        AvifPixelBuffer wrongSize = AvifPixelBuffer.Create(
            1, 1, AvifPixelFormat.Rgb, 8, false);
        AvifColorConversionOptions invalid = new AvifColorConversionOptions();
        invalid.WorkerCount = 0;
        AvifImage identity420 = CreateImage(
            2, 2, 8, AvifChromaFormat.Yuv420, AvifRange.Full, false);
        identity420.SetColorInfo(AvifColorInfo.Create(
            AvifColorPrimaries.Bt709,
            AvifTransferCharacteristics.Srgb,
            AvifMatrixCoefficients.Identity,
            AvifRange.Full));
        AvifPixelBuffer rgb = AvifPixelBuffer.Create(
            2, 2, AvifPixelFormat.Rgb, 8, false);

        Assert.Multiple(() =>
        {
            Assert.That(converter.ConvertToPixelBuffer(image, wrongSize),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(converter.SetOptions(invalid),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(converter.ConvertToPixelBuffer(identity420, rgb),
                Is.EqualTo(AvifResultCode.InvalidData));
        });

        image.Close();
        Assert.That(converter.ConvertToPixelBuffer(image, rgb),
            Is.EqualTo(AvifResultCode.Closed));
        rgb.Close();
        identity420.Close();
        wrongSize.Close();
    }

    static AvifImage CreateImage(
        int width,
        int height,
        int depth,
        AvifChromaFormat format,
        AvifRange range,
        bool alpha)
    {
        AvifImage image = AvifImage.Create(
            width,
            height,
            depth,
            format,
            range,
            AvifChromaSamplePosition.Colocated,
            alpha ? AvifAlphaMode.Straight : AvifAlphaMode.None,
            1,
            new AvifSecurityLimits());
        image.SetColorInfo(AvifColorInfo.Create(
            AvifColorPrimaries.Bt709,
            AvifTransferCharacteristics.Srgb,
            AvifMatrixCoefficients.Bt709,
            range));
        return image;
    }

    static void AssertPacked(AvifPixelBuffer pixels, byte[] expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(pixels.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(pixels.GetRowSpan(0).ToArray(), Is.EqualTo(expected));
        });
        pixels.Close();
    }

    static void FillPlane(AvifPlane? plane, int value)
    {
        if (plane == null)
        {
            return;
        }
        for (int y = 0; y < plane.GetHeight(); y++)
        {
            for (int x = 0; x < plane.GetWidth(); x++)
            {
                WriteSample(plane, x, y, value);
            }
        }
    }

    static void WriteSample(AvifPlane? plane, int x, int y, int value)
    {
        Span<byte> row = (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(y);
        if (plane.GetBitDepth() <= 8)
        {
            row[x] = (byte)value;
            return;
        }
        int offset = x * 2;
        row[offset] = (byte)value;
        row[offset + 1] = (byte)(value >> 8);
    }

    static int ReadSample(AvifPlane? plane, int x, int y)
    {
        Span<byte> row = (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(y);
        if (plane.GetBitDepth() <= 8)
        {
            return row[x];
        }
        int offset = x * 2;
        return row[offset] | row[offset + 1] << 8;
    }

    static int ReadInteger(Span<byte> row, int offset)
    {
        return row[offset] | row[offset + 1] << 8;
    }

    static double ReadHalf(Span<byte> row, int offset)
    {
        ushort bits = (ushort)(row[offset] | row[offset + 1] << 8);
        return (double)BitConverter.UInt16BitsToHalf(bits);
    }
}
