namespace svg.Tests;

public class SvgPixelBufferTests
{
    [Test]
    public void AllocatesTightTransparentBuffersWithExplicitMetadata()
    {
        SvgPixelBufferResult created = SvgPixelBuffer.Create(3, 2, SvgPixelColorSpace.Srgb);
        SvgPixelBufferResult empty = SvgPixelBuffer.Create(0, 5, SvgPixelColorSpace.SrgbLinear);

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Error, Is.EqualTo(SvgPixelBufferError.None));
            Assert.That(created.Buffer.Width, Is.EqualTo(3));
            Assert.That(created.Buffer.Height, Is.EqualTo(2));
            Assert.That(created.Buffer.RowBytes, Is.EqualTo(12));
            Assert.That(created.Buffer.Stride, Is.EqualTo(12));
            Assert.That(created.Buffer.Offset, Is.Zero);
            Assert.That(created.Buffer.ByteLength, Is.EqualTo(24));
            Assert.That(created.Buffer.Data, Has.Length.EqualTo(24));
            Assert.That(created.Buffer.Data, Is.All.Zero);
            Assert.That(created.Buffer.ColorSpace, Is.EqualTo(SvgPixelColorSpace.Srgb));
            Assert.That(created.Buffer.OwnsData, Is.True);
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Buffer.Stride, Is.Zero);
            Assert.That(empty.Buffer.ByteLength, Is.Zero);
            Assert.That(empty.Buffer.Data, Is.Empty);
            Assert.That(empty.Buffer.ColorSpace, Is.EqualTo(SvgPixelColorSpace.SrgbLinear));
        });
    }

    [Test]
    public void PremultipliesWritesAndReadsIndependentPixels()
    {
        SvgPremultipliedRgba8 pixel = SvgPremultipliedRgba8.FromStraight(255, 128, 1, 128);
        SvgPremultipliedRgba8 transparent = SvgPremultipliedRgba8.FromStraight(255, 200, 100, 0);
        SvgPixelBuffer buffer = SvgPixelBuffer.Create(2, 1, SvgPixelColorSpace.Srgb).Buffer;

        SvgPixelOperationResult written = buffer.Write(1, 0, pixel);
        SvgPixelReadResult read = buffer.Read(1, 0);
        read.Pixel.Red = 0;
        SvgPixelReadResult reread = buffer.Read(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(pixel.Red, Is.EqualTo(128));
            Assert.That(pixel.Green, Is.EqualTo(64));
            Assert.That(pixel.Blue, Is.EqualTo(1));
            Assert.That(pixel.Alpha, Is.EqualTo(128));
            Assert.That(transparent.Red, Is.Zero);
            Assert.That(transparent.Green, Is.Zero);
            Assert.That(transparent.Blue, Is.Zero);
            Assert.That(written.Success, Is.True);
            Assert.That(written.PixelsWritten, Is.EqualTo(1));
            Assert.That(read.Success, Is.True);
            Assert.That(reread.Pixel.Red, Is.EqualTo(128));
            Assert.That(reread.Pixel.Green, Is.EqualTo(64));
            Assert.That(reread.Pixel.Blue, Is.EqualTo(1));
            Assert.That(reread.Pixel.Alpha, Is.EqualTo(128));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgPremultipliedRgba8(2, 0, 0, 1));
        });
    }

    [Test]
    public void WrapsPaddedStridesAndClearsOnlyActiveRectangleBytes()
    {
        byte[] data = new byte[30];
        data[10] = 91;
        data[11] = 92;
        data[12] = 93;
        data[13] = 94;
        SvgPixelBufferResult wrapped = SvgPixelBuffer.Wrap(data, 2, 2, 2, 12, SvgPixelColorSpace.Srgb);
        SvgPremultipliedRgba8 fill = new SvgPremultipliedRgba8(80, 20, 0, 100);

        SvgPixelOperationResult cleared = wrapped.Buffer.ClearRectangle(new SvgPixelRect(1, 0, 1, 2), fill);

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.Success, Is.True);
            Assert.That(wrapped.Buffer.OwnsData, Is.False);
            Assert.That(wrapped.Buffer.Offset, Is.EqualTo(2));
            Assert.That(wrapped.Buffer.RowBytes, Is.EqualTo(8));
            Assert.That(wrapped.Buffer.Stride, Is.EqualTo(12));
            Assert.That(wrapped.Buffer.ByteLength, Is.EqualTo(20));
            Assert.That(cleared.Success, Is.True);
            Assert.That(cleared.PixelsWritten, Is.EqualTo(2));
            Assert.That(wrapped.Buffer.Read(0, 0).Pixel.Alpha, Is.Zero);
            AssertPixel(wrapped.Buffer.Read(1, 0), 80, 20, 0, 100);
            AssertPixel(wrapped.Buffer.Read(1, 1), 80, 20, 0, 100);
            Assert.That(data.Skip(10).Take(4), Is.EqualTo(new byte[] { 91, 92, 93, 94 }));
        });
    }

    [Test]
    public void SubsurfacesShareStorageAndPreserveParentStrideAndColorSpace()
    {
        SvgPixelBuffer root = SvgPixelBuffer.Create(4, 3, SvgPixelColorSpace.SrgbLinear).Buffer;
        root.ClearWithPixel(new SvgPremultipliedRgba8(0, 0, 0, 255));

        SvgPixelBufferResult viewResult = root.Subsurface(new SvgPixelRect(1, 1, 2, 2));
        SvgPixelBufferResult emptyResult = root.Subsurface(new SvgPixelRect(4, 3, 0, 0));
        SvgPixelOperationResult cleared = viewResult.Buffer.ClearWithPixel(new SvgPremultipliedRgba8(0, 0, 128, 128));

        Assert.Multiple(() =>
        {
            Assert.That(viewResult.Success, Is.True);
            Assert.That(viewResult.Buffer.Data, Is.SameAs(root.Data));
            Assert.That(viewResult.Buffer.Offset, Is.EqualTo(20));
            Assert.That(viewResult.Buffer.Stride, Is.EqualTo(16));
            Assert.That(viewResult.Buffer.RowBytes, Is.EqualTo(8));
            Assert.That(viewResult.Buffer.ByteLength, Is.EqualTo(24));
            Assert.That(viewResult.Buffer.ColorSpace, Is.EqualTo(SvgPixelColorSpace.SrgbLinear));
            Assert.That(viewResult.Buffer.OwnsData, Is.False);
            Assert.That(cleared.PixelsWritten, Is.EqualTo(4));
            AssertPixel(root.Read(1, 1), 0, 0, 128, 128);
            AssertPixel(root.Read(2, 2), 0, 0, 128, 128);
            AssertPixel(root.Read(0, 0), 0, 0, 0, 255);
            Assert.That(emptyResult.Success, Is.True);
            Assert.That(emptyResult.Buffer.ByteLength, Is.Zero);
            Assert.That(emptyResult.Buffer.Data, Is.SameAs(root.Data));
        });
    }

    [Test]
    public void CopiesAcrossStridesAndHandlesHorizontalAndVerticalOverlap()
    {
        byte[] sourceData = new byte[24];
        SvgPixelBuffer source = SvgPixelBuffer.Wrap(sourceData, 0, 2, 2, 12, SvgPixelColorSpace.Srgb).Buffer;
        source.Write(0, 0, Gray(10));
        source.Write(1, 0, Gray(20));
        source.Write(0, 1, Gray(30));
        source.Write(1, 1, Gray(40));
        SvgPixelBuffer destination = SvgPixelBuffer.Create(3, 3, SvgPixelColorSpace.Srgb).Buffer;

        SvgPixelOperationResult copied = destination.CopyFrom(source, 1, 1);

        SvgPixelBuffer horizontal = SvgPixelBuffer.Create(4, 1, SvgPixelColorSpace.Srgb).Buffer;
        WriteFirstRow(horizontal, 10, 20, 30, 40);
        SvgPixelOperationResult horizontalCopy = horizontal.CopyRectangleFrom(horizontal, new SvgPixelRect(0, 0, 3, 1), 1, 0);

        SvgPixelBuffer vertical = SvgPixelBuffer.Create(2, 3, SvgPixelColorSpace.Srgb).Buffer;
        WriteRow(vertical, 0, 10, 11);
        WriteRow(vertical, 1, 20, 21);
        WriteRow(vertical, 2, 30, 31);
        SvgPixelOperationResult verticalCopy = vertical.CopyRectangleFrom(vertical, new SvgPixelRect(0, 0, 2, 2), 0, 1);

        byte[] sharedData = new byte[36];
        SvgPixelBuffer compactView = SvgPixelBuffer.Wrap(sharedData, 0, 2, 3, 8, SvgPixelColorSpace.Srgb).Buffer;
        WriteRow(compactView, 0, 10, 11);
        WriteRow(compactView, 1, 20, 21);
        WriteRow(compactView, 2, 30, 31);
        SvgPixelBuffer paddedView = SvgPixelBuffer.Wrap(sharedData, 0, 2, 3, 12, SvgPixelColorSpace.Srgb).Buffer;
        SvgPixelOperationResult differingStrideCopy = paddedView.CopyFrom(compactView, 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(copied.Success, Is.True);
            Assert.That(copied.PixelsWritten, Is.EqualTo(4));
            AssertGray(destination, 1, 1, 10);
            AssertGray(destination, 2, 1, 20);
            AssertGray(destination, 1, 2, 30);
            AssertGray(destination, 2, 2, 40);
            Assert.That(horizontalCopy.Success, Is.True);
            Assert.That(ReadRow(horizontal, 0), Is.EqualTo(new byte[] { 10, 10, 20, 30 }));
            Assert.That(verticalCopy.Success, Is.True);
            Assert.That(ReadRow(vertical, 0), Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(ReadRow(vertical, 1), Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(ReadRow(vertical, 2), Is.EqualTo(new byte[] { 20, 21 }));
            Assert.That(differingStrideCopy.Success, Is.True);
            Assert.That(ReadRow(paddedView, 0), Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(ReadRow(paddedView, 1), Is.EqualTo(new byte[] { 20, 21 }));
            Assert.That(ReadRow(paddedView, 2), Is.EqualTo(new byte[] { 30, 31 }));
        });
    }

    [Test]
    public void RejectsDimensionByteStrideBoundsAndColorSpaceViolations()
    {
        SvgRasterLimits dimensions = new SvgRasterLimits();
        dimensions.MaxWidth = 2;
        SvgRasterLimits pixels = new SvgRasterLimits();
        pixels.MaxSurfacePixels = 3;
        SvgRasterLimits bytes = new SvgRasterLimits();
        bytes.MaxPixelBytes = 15;
        SvgRasterLimits large = new SvgRasterLimits();
        large.MaxWidth = int.MaxValue;
        large.MaxHeight = int.MaxValue;
        large.MaxSurfacePixels = long.MaxValue;
        large.MaxPixelBytes = long.MaxValue;
        byte[] invalidPremultiplied = new byte[] { 2, 0, 0, 1 };

        SvgPixelBufferResult tooWide = SvgPixelBuffer.Create(3, 1, SvgPixelColorSpace.Srgb, dimensions);
        SvgPixelBufferResult tooManyPixels = SvgPixelBuffer.Create(2, 2, SvgPixelColorSpace.Srgb, pixels);
        SvgPixelBufferResult tooManyBytes = SvgPixelBuffer.Create(2, 2, SvgPixelColorSpace.Srgb, bytes);
        SvgPixelBufferResult runtimeBytes = SvgPixelBuffer.Create(int.MaxValue, 1, SvgPixelColorSpace.Srgb, large);
        SvgPixelBufferResult arrayBytes = SvgPixelBuffer.Create(System.Array.MaxLength / 4 + 1, 1, SvgPixelColorSpace.Srgb, large);
        SvgPixelBufferResult badSpace = SvgPixelBuffer.Create(1, 1, (SvgPixelColorSpace)99);
        SvgPixelBufferResult badStride = SvgPixelBuffer.Wrap(new byte[8], 0, 2, 1, 7, SvgPixelColorSpace.Srgb);
        SvgPixelBufferResult shortData = SvgPixelBuffer.Wrap(new byte[7], 0, 2, 1, 8, SvgPixelColorSpace.Srgb);
        SvgPixelBufferResult badPixel = SvgPixelBuffer.Wrap(invalidPremultiplied, 0, 1, 1, 4, SvgPixelColorSpace.Srgb);

        Assert.Multiple(() =>
        {
            Assert.That(tooWide.Error, Is.EqualTo(SvgPixelBufferError.DimensionLimitExceeded));
            Assert.That(tooManyPixels.Error, Is.EqualTo(SvgPixelBufferError.DimensionLimitExceeded));
            Assert.That(tooManyBytes.Error, Is.EqualTo(SvgPixelBufferError.ByteLimitExceeded));
            Assert.That(runtimeBytes.Error, Is.EqualTo(SvgPixelBufferError.ByteLimitExceeded));
            Assert.That(arrayBytes.Error, Is.EqualTo(SvgPixelBufferError.ByteLimitExceeded));
            Assert.That(badSpace.Error, Is.EqualTo(SvgPixelBufferError.InvalidArgument));
            Assert.That(badStride.Error, Is.EqualTo(SvgPixelBufferError.InvalidArgument));
            Assert.That(shortData.Error, Is.EqualTo(SvgPixelBufferError.BoundsExceeded));
            Assert.That(badPixel.Error, Is.EqualTo(SvgPixelBufferError.InvalidPremultipliedColor));
            Assert.That(new SvgPixelRect(-1, -2, 3, 4).Right, Is.EqualTo(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgPixelRect(int.MaxValue, 0, 1, 1));
        });
    }

    [Test]
    public void OperationsFailWithoutMutationForInvalidRegionsSpacesAndMutableState()
    {
        SvgPixelBuffer srgb = SvgPixelBuffer.Create(2, 2, SvgPixelColorSpace.Srgb).Buffer;
        SvgPixelBuffer linear = SvgPixelBuffer.Create(1, 1, SvgPixelColorSpace.SrgbLinear).Buffer;
        linear.Write(0, 0, Gray(50));

        SvgPixelOperationResult badClear = srgb.ClearRectangle(new SvgPixelRect(-1, 0, 1, 1), Gray(20));
        SvgPixelOperationResult badCopy = srgb.CopyFrom(linear, 0, 0);
        SvgPixelBufferResult badView = srgb.Subsurface(new SvgPixelRect(1, 1, 2, 1));
        SvgPixelReadResult badRead = srgb.Read(2, 0);
        SvgPixelOperationResult badWrite = srgb.Write(0, -1, Gray(10));
        SvgPixelBuffer mutable = SvgPixelBuffer.Create(1, 1, SvgPixelColorSpace.Srgb).Buffer;
        mutable.Width = -1;
        SvgPixelOperationResult mutatedClear = mutable.Clear();
        SvgPixelRect mutableRectangle = new SvgPixelRect(0, 0, 1, 1);
        mutableRectangle.Width = 2;
        SvgPixelOperationResult mutatedRectangleClear = srgb.ClearRectangle(mutableRectangle, Gray(10));

        Assert.Multiple(() =>
        {
            Assert.That(badClear.Error, Is.EqualTo(SvgPixelBufferError.BoundsExceeded));
            Assert.That(badCopy.Error, Is.EqualTo(SvgPixelBufferError.ColorSpaceMismatch));
            Assert.That(badView.Error, Is.EqualTo(SvgPixelBufferError.BoundsExceeded));
            Assert.That(badRead.Error, Is.EqualTo(SvgPixelBufferError.BoundsExceeded));
            Assert.That(badWrite.Error, Is.EqualTo(SvgPixelBufferError.BoundsExceeded));
            Assert.That(mutatedClear.Error, Is.EqualTo(SvgPixelBufferError.InvalidArgument));
            Assert.That(mutatedRectangleClear.Error, Is.EqualTo(SvgPixelBufferError.InvalidArgument));
            Assert.That(srgb.Data, Is.All.Zero);
        });
    }

    static SvgPremultipliedRgba8 Gray(byte value)
    {
        return new SvgPremultipliedRgba8(value, value, value, 255);
    }

    static void WriteFirstRow(SvgPixelBuffer buffer, params byte[] values)
    {
        WriteRow(buffer, 0, values);
    }

    static void WriteRow(SvgPixelBuffer buffer, int y, params byte[] values)
    {
        int x = 0;
        while (x < values.Length)
        {
            Assert.That(buffer.Write(x, y, Gray(values[x])).Success, Is.True);
            x++;
        }
    }

    static byte[] ReadRow(SvgPixelBuffer buffer, int y)
    {
        byte[] values = new byte[buffer.Width];
        int x = 0;
        while (x < buffer.Width)
        {
            values[x] = buffer.Read(x, y).Pixel.Red;
            x++;
        }
        return values;
    }

    static void AssertGray(SvgPixelBuffer buffer, int x, int y, byte value)
    {
        AssertPixel(buffer.Read(x, y), value, value, value, 255);
    }

    static void AssertPixel(SvgPixelReadResult result, byte red, byte green, byte blue, byte alpha)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Pixel.Red, Is.EqualTo(red));
        Assert.That(result.Pixel.Green, Is.EqualTo(green));
        Assert.That(result.Pixel.Blue, Is.EqualTo(blue));
        Assert.That(result.Pixel.Alpha, Is.EqualTo(alpha));
    }
}
