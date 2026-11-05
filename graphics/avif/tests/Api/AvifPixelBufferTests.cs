namespace avif.Tests;

public class AvifPixelBufferTests
{
    [Test]
    public void EveryIntegerPackedLayoutSupportsEveryAdvertisedDepth()
    {
        AvifPixelFormat[] formats = new AvifPixelFormat[]
        {
            AvifPixelFormat.Gray,
            AvifPixelFormat.GrayAlpha,
            AvifPixelFormat.Rgb,
            AvifPixelFormat.Rgba,
            AvifPixelFormat.Bgr,
            AvifPixelFormat.Bgra,
            AvifPixelFormat.Argb,
            AvifPixelFormat.Abgr
        };
        int[] depths = new int[] { 8, 10, 12, 16 };
        for (int formatIndex = 0; formatIndex < formats.Length; formatIndex++)
        {
            for (int depthIndex = 0; depthIndex < depths.Length; depthIndex++)
            {
                AvifPixelBuffer pixels = AvifPixelBuffer.Create(
                    3, 2, formats[formatIndex], depths[depthIndex], false);
                int channels = GetChannelCount(formats[formatIndex]);
                int bytesPerComponent = depths[depthIndex] > 8 ? 2 : 1;

                Assert.Multiple(() =>
                {
                    Assert.That(pixels.GetCode(), Is.EqualTo(AvifResultCode.Success));
                    Assert.That(pixels.GetChannelCount(), Is.EqualTo(channels));
                    Assert.That(
                        pixels.GetBytesPerPixel(),
                        Is.EqualTo(channels * bytesPerComponent));
                    Assert.That(
                        pixels.GetRowByteCount(),
                        Is.EqualTo(3 * channels * bytesPerComponent));
                    Assert.That(pixels.GetBitDepth(), Is.EqualTo(depths[depthIndex]));
                    Assert.That(
                        pixels.HasAlpha(),
                        Is.EqualTo(HasAlpha(formats[formatIndex])));
                    Assert.That(pixels.IsFloatingPoint(), Is.False);
                });

                pixels.Close();
            }
        }

        AvifPixelBuffer rgb565 = AvifPixelBuffer.Create(
            3, 2, AvifPixelFormat.Rgb565, 8, false);
        Assert.Multiple(() =>
        {
            Assert.That(rgb565.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(rgb565.GetChannelCount(), Is.EqualTo(3));
            Assert.That(rgb565.GetBytesPerPixel(), Is.EqualTo(2));
            Assert.That(rgb565.GetRowByteCount(), Is.EqualTo(6));
        });
        rgb565.Close();
    }

    [TestCase(AvifPixelFormat.Rgb, 6)]
    [TestCase(AvifPixelFormat.Rgba, 8)]
    public void LinearHalfFloatRgbLayoutsRemainByteAddressable(
        AvifPixelFormat format,
        int expectedBytesPerPixel)
    {
        AvifPixelBuffer pixels = AvifPixelBuffer.Create(
            2, 1, format, 16, true);
        byte[] halfValues = new byte[2 * expectedBytesPerPixel];
        for (int index = 0; index < halfValues.Length; index++)
        {
            halfValues[index] = (byte)(index + 1);
        }
        halfValues.AsSpan().CopyTo(pixels.GetRowSpan(0));

        Assert.Multiple(() =>
        {
            Assert.That(pixels.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(pixels.IsFloatingPoint(), Is.True);
            Assert.That(pixels.GetBitDepth(), Is.EqualTo(16));
            Assert.That(pixels.GetBytesPerPixel(), Is.EqualTo(expectedBytesPerPixel));
            Assert.That(pixels.GetRowSpan(0).ToArray(), Is.EqualTo(halfValues));
        });

        pixels.Close();
    }

    [Test]
    public void AllocatedViewsAndCopiesPreserveStrideAndVisiblePixels()
    {
        AvifPixelBuffer owner = AvifPixelBuffer.Create(
            3,
            2,
            AvifPixelFormat.Rgba,
            16,
            false,
            16,
            new AvifSecurityLimits());
        byte[] first = CreateSequence(24, 1);
        byte[] second = CreateSequence(24, 31);
        first.AsSpan().CopyTo(owner.GetRowSpan(0));
        second.AsSpan().CopyTo(owner.GetRowSpan(1));
        AvifPixelBuffer view = owner.CreateView(1, 0, 2, 2);
        AvifPixelBuffer copy = view.CreateCopy();
        view.GetRowSpan(0)[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(owner.GetStride(), Is.EqualTo(32));
            Assert.That(owner.GetSpan().Length, Is.EqualTo(56));
            Assert.That(owner.OwnsStorage(), Is.True);
            Assert.That(view.GetStride(), Is.EqualTo(32));
            Assert.That(view.GetRowByteCount(), Is.EqualTo(16));
            Assert.That(view.OwnsStorage(), Is.False);
            Assert.That(owner.GetRowSpan(0)[8], Is.EqualTo(99));
            Assert.That(copy.GetRowSpan(0).ToArray(),
                Is.EqualTo(first.AsSpan(8, 16).ToArray()));
            Assert.That(copy.GetRowSpan(1).ToArray(),
                Is.EqualTo(second.AsSpan(8, 16).ToArray()));
            Assert.That(copy.OwnsStorage(), Is.True);
        });

        owner.Close();
        Assert.Multiple(() =>
        {
            Assert.That(view.GetCode(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(copy.GetCode(), Is.EqualTo(AvifResultCode.Success));
        });
        view.Close();
        copy.Close();
    }

    [Test]
    public void CallerOwnedBuffersShareBytesWithoutTransferringOwnership()
    {
        byte[] data = new byte[64];
        AvifPixelBuffer pixels = AvifPixelBuffer.Create(
            data,
            4,
            40,
            3,
            2,
            16,
            AvifPixelFormat.Rgb,
            8,
            false);
        AvifPixelBuffer view = pixels.CreateView(1, 0, 2, 2);
        pixels.GetRowSpan(0)[0] = 7;
        view.GetRowSpan(1)[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(pixels.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(pixels.OwnsStorage(), Is.False);
            Assert.That(pixels.GetRowByteCount(), Is.EqualTo(9));
            Assert.That(pixels.GetSpan().Length, Is.EqualTo(25));
            Assert.That(data[4], Is.EqualTo(7));
            Assert.That(data[23], Is.EqualTo(9));
            Assert.That(view.GetRowByteCount(), Is.EqualTo(6));
        });

        pixels.Close();
        Assert.Multiple(() =>
        {
            Assert.That(view.GetCode(), Is.EqualTo(AvifResultCode.Closed));
            Assert.That(data[4], Is.EqualTo(7));
            Assert.That(data[23], Is.EqualTo(9));
        });
        view.Close();
    }

    [Test]
    public void OverlappingPackedViewsCopyThroughScratchStorage()
    {
        byte[] data = new byte[]
        {
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12
        };
        AvifPixelBuffer owner = AvifPixelBuffer.Create(
            data,
            0,
            data.Length,
            2,
            3,
            4,
            AvifPixelFormat.GrayAlpha,
            8,
            false);
        AvifPixelBuffer source = owner.CreateView(0, 0, 2, 2);
        AvifPixelBuffer destination = owner.CreateView(0, 1, 2, 2);

        AvifResultCode result = destination.CopyFrom(source);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(data, Is.EqualTo(new byte[]
            {
                1, 2, 3, 4,
                1, 2, 3, 4,
                5, 6, 7, 8
            }));
        });

        source.Close();
        destination.Close();
        owner.Close();
    }

    [Test]
    public void PackedBuffersRejectInvalidFormatsBoundsAndLimits()
    {
        AvifSecurityLimits limited = new AvifSecurityLimits();
        limited.MaxDecodedBytes = 31;
        AvifSecurityLimits oversizedLimits = new AvifSecurityLimits();
        oversizedLimits.MaxWidth = 30000;
        oversizedLimits.MaxHeight = 30000;
        oversizedLimits.MaxPixels = 900000000;
        oversizedLimits.MaxDecodedBytes = long.MaxValue;
        AvifPixelBuffer byteLimited = AvifPixelBuffer.Create(
            4, 2, AvifPixelFormat.Rgba, 8, false, 1, limited);
        AvifPixelBuffer oversized = AvifPixelBuffer.Create(
            30000,
            30000,
            AvifPixelFormat.Rgba,
            16,
            false,
            1,
            oversizedLimits);
        AvifPixelBuffer badFloatLayout = AvifPixelBuffer.Create(
            2, 2, AvifPixelFormat.Bgr, 16, true);
        AvifPixelBuffer badFloatDepth = AvifPixelBuffer.Create(
            2, 2, AvifPixelFormat.Rgb, 32, true);
        AvifPixelBuffer bad565Depth = AvifPixelBuffer.Create(
            2, 2, AvifPixelFormat.Rgb565, 16, false);
        AvifPixelBuffer tooSmall = AvifPixelBuffer.Create(
            new byte[20],
            0,
            20,
            3,
            2,
            16,
            AvifPixelFormat.Rgb,
            8,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(byteLimited.GetCode(), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(oversized.GetCode(), Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(badFloatLayout.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(badFloatDepth.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(bad565Depth.GetCode(), Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(tooSmall.GetCode(), Is.EqualTo(AvifResultCode.BufferTooSmall));
            Assert.That(tooSmall.GetRowSpan(0).Length, Is.Zero);
        });

        byteLimited.Close();
        oversized.Close();
        badFloatLayout.Close();
        badFloatDepth.Close();
        bad565Depth.Close();
        tooSmall.Close();
    }

    static int GetChannelCount(AvifPixelFormat format)
    {
        if (format == AvifPixelFormat.Gray)
        {
            return 1;
        }
        if (format == AvifPixelFormat.GrayAlpha)
        {
            return 2;
        }
        if (format == AvifPixelFormat.Rgb || format == AvifPixelFormat.Bgr)
        {
            return 3;
        }
        return 4;
    }

    static bool HasAlpha(AvifPixelFormat format)
    {
        return format == AvifPixelFormat.GrayAlpha
            || format == AvifPixelFormat.Rgba
            || format == AvifPixelFormat.Bgra
            || format == AvifPixelFormat.Argb
            || format == AvifPixelFormat.Abgr;
    }

    static byte[] CreateSequence(int length, int start)
    {
        byte[] values = new byte[length];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = (byte)(start + index);
        }
        return values;
    }
}
