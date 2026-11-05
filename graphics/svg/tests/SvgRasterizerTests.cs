namespace svg.Tests;

public class SvgRasterizerTests
{
    [Test]
    public void RasterizesIntegerAlignedFillWithDeterministicCoverageAndCounts()
    {
        SvgPixelBuffer surface = SvgPixelBuffer.Create(4, 4, SvgPixelColorSpace.Srgb).Buffer;
        SvgPremultipliedRgba8 red = new SvgPremultipliedRgba8(200, 0, 0, 200);

        SvgRasterResult result = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRule(Parse("M1 1 H3 V3 H1 Z"), surface, red, SvgFillRule.Nonzero);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.Bounds.X, Is.EqualTo(1));
            Assert.That(result.Bounds.Y, Is.EqualTo(1));
            Assert.That(result.Bounds.Width, Is.EqualTo(2));
            Assert.That(result.Bounds.Height, Is.EqualTo(2));
            Assert.That(result.EdgeCount, Is.EqualTo(4));
            Assert.That(result.WorkUnits, Is.EqualTo(256));
            Assert.That(result.PixelsWritten, Is.EqualTo(4));
            AssertPixel(surface, 1, 1, 200, 0, 0, 200);
            AssertPixel(surface, 2, 2, 200, 0, 0, 200);
            AssertPixel(surface, 0, 0, 0, 0, 0, 0);
            AssertPixel(surface, 3, 3, 0, 0, 0, 0);
        });
    }

    [Test]
    public void SupersamplingProducesRepeatablePremultipliedFractionalCoverage()
    {
        SvgPathData path = Parse("M0.5 0.5 H1.5 V1.5 H0.5 Z");
        SvgPremultipliedRgba8 color = new SvgPremultipliedRgba8(128, 64, 32, 128);
        SvgPixelBuffer first = SvgPixelBuffer.Create(2, 2, SvgPixelColorSpace.Srgb).Buffer;
        SvgPixelBuffer second = SvgPixelBuffer.Create(2, 2, SvgPixelColorSpace.Srgb).Buffer;

        SvgRasterResult firstResult = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRule(path, first, color, SvgFillRule.Nonzero);
        SvgRasterResult secondResult = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRule(path, second, color, SvgFillRule.Nonzero);

        Assert.Multiple(() =>
        {
            AssertSuccess(firstResult);
            AssertSuccess(secondResult);
            Assert.That(first.Data, Is.EqualTo(second.Data));
            AssertPixel(first, 0, 0, 32, 16, 8, 32);
            AssertPixel(first, 1, 0, 32, 16, 8, 32);
            AssertPixel(first, 0, 1, 32, 16, 8, 32);
            AssertPixel(first, 1, 1, 32, 16, 8, 32);
        });
    }

    [Test]
    public void NonzeroAndEvenOddRulesDifferForSameDirectionContours()
    {
        SvgPathData nested = Parse("M0 0 H5 V5 H0 Z M1 1 H4 V4 H1 Z");
        SvgPremultipliedRgba8 white = new SvgPremultipliedRgba8(255, 255, 255, 255);
        SvgPixelBuffer nonzero = SvgPixelBuffer.Create(5, 5, SvgPixelColorSpace.Srgb).Buffer;
        SvgPixelBuffer evenOdd = SvgPixelBuffer.Create(5, 5, SvgPixelColorSpace.Srgb).Buffer;

        SvgRasterResult nonzeroResult = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRule(nested, nonzero, white, SvgFillRule.Nonzero);
        SvgRasterResult evenOddResult = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRule(nested, evenOdd, white, SvgFillRule.EvenOdd);

        Assert.Multiple(() =>
        {
            AssertSuccess(nonzeroResult);
            AssertSuccess(evenOddResult);
            AssertPixel(nonzero, 2, 2, 255, 255, 255, 255);
            AssertPixel(evenOdd, 2, 2, 0, 0, 0, 0);
            AssertPixel(evenOdd, 0, 0, 255, 255, 255, 255);
            Assert.That(nonzeroResult.EdgeCount, Is.EqualTo(8));
            Assert.That(evenOddResult.EdgeCount, Is.EqualTo(8));
        });
    }

    [Test]
    public void ClipsToSurfaceAndExplicitRectangleWithoutChangingPadding()
    {
        byte[] bytes = new byte[44];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = 77;
            index++;
        }
        ZeroActiveBytes(bytes, 4, 3, 12);
        SvgPixelBuffer surface = SvgPixelBuffer.Wrap(bytes, 4, 2, 3, 12, SvgPixelColorSpace.Srgb).Buffer;
        SvgRasterOptions options = new SvgRasterOptions();
        options.Clip = new SvgPixelRect(1, 1, 1, 1);

        SvgRasterResult result = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRuleAndOptions(Parse("M-10 -10 H10 V10 H-10 Z"), surface, new SvgPremultipliedRgba8(0, 90, 0, 100), SvgFillRule.Nonzero, options);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.Bounds.X, Is.EqualTo(1));
            Assert.That(result.Bounds.Y, Is.EqualTo(1));
            Assert.That(result.Bounds.Width, Is.EqualTo(1));
            Assert.That(result.Bounds.Height, Is.EqualTo(1));
            Assert.That(result.PixelsWritten, Is.EqualTo(1));
            AssertPixel(surface, 1, 1, 0, 90, 0, 100);
            AssertPixel(surface, 0, 0, 0, 0, 0, 0);
            Assert.That(bytes[0], Is.EqualTo(77));
            Assert.That(bytes[3], Is.EqualTo(77));
            Assert.That(bytes[12], Is.EqualTo(77));
            Assert.That(bytes[13], Is.EqualTo(77));
            Assert.That(bytes[24], Is.EqualTo(77));
            Assert.That(bytes[25], Is.EqualTo(77));
            Assert.That(bytes[40], Is.EqualTo(77));
        });
    }

    [Test]
    public void AppliesOutputTransformAndImplicitlyClosesOpenSubpaths()
    {
        SvgPixelBuffer transformed = SvgPixelBuffer.Create(5, 5, SvgPixelColorSpace.Srgb).Buffer;
        SvgRasterOptions options = new SvgRasterOptions();
        options.Transform = new SvgMatrix(2, 0, 0, 2, 1, 1);
        SvgPixelBuffer open = SvgPixelBuffer.Create(3, 3, SvgPixelColorSpace.Srgb).Buffer;
        SvgPremultipliedRgba8 blue = new SvgPremultipliedRgba8(0, 0, 255, 255);

        SvgRasterResult transformedResult = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRuleAndOptions(Parse("M0 0 H1 V1 H0 Z"), transformed, blue, SvgFillRule.Nonzero, options);
        SvgRasterResult openResult = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRule(Parse("M0 0 L3 0 L0 3"), open, blue, SvgFillRule.Nonzero);

        Assert.Multiple(() =>
        {
            AssertSuccess(transformedResult);
            Assert.That(transformedResult.Bounds.X, Is.EqualTo(1));
            Assert.That(transformedResult.Bounds.Y, Is.EqualTo(1));
            Assert.That(transformedResult.Bounds.Width, Is.EqualTo(2));
            Assert.That(transformedResult.Bounds.Height, Is.EqualTo(2));
            AssertPixel(transformed, 1, 1, 0, 0, 255, 255);
            AssertPixel(transformed, 0, 0, 0, 0, 0, 0);
            AssertSuccess(openResult);
            Assert.That(openResult.EdgeCount, Is.EqualTo(3));
            AssertPixel(open, 0, 0, 0, 0, 255, 255);
            AssertPixel(open, 2, 2, 0, 0, 0, 0);
        });
    }

    [Test]
    public void CurvesFlattenWithinConfiguredTolerance()
    {
        SvgPixelBuffer surface = SvgPixelBuffer.Create(5, 5, SvgPixelColorSpace.Srgb).Buffer;
        SvgRasterOptions options = new SvgRasterOptions();
        options.Tolerance = 0.01;

        SvgRasterResult result = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRuleAndOptions(Parse("M0 4 Q2 0 4 4 Z"), surface, new SvgPremultipliedRgba8(80, 40, 20, 100), SvgFillRule.Nonzero, options);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.EdgeCount, Is.GreaterThan(3));
            Assert.That(result.PixelsWritten, Is.GreaterThan(0));
            Assert.That(surface.Read(2, 3).Pixel.Alpha, Is.GreaterThan(0));
            AssertPixel(surface, 2, 0, 0, 0, 0, 0);
        });
    }

    [Test]
    public void EdgeWorkAndSubdivisionLimitsFailBeforeSurfaceMutation()
    {
        SvgPremultipliedRgba8 white = new SvgPremultipliedRgba8(255, 255, 255, 255);
        SvgPixelBuffer edgeSurface = SvgPixelBuffer.Create(4, 4, SvgPixelColorSpace.Srgb).Buffer;
        SvgRasterOptions edgeOptions = new SvgRasterOptions();
        edgeOptions.MaximumEdges = 3;
        SvgPixelBuffer workSurface = SvgPixelBuffer.Create(4, 4, SvgPixelColorSpace.Srgb).Buffer;
        SvgRasterOptions workOptions = new SvgRasterOptions();
        workOptions.MaximumWork = 100;
        SvgPixelBuffer depthSurface = SvgPixelBuffer.Create(4, 4, SvgPixelColorSpace.Srgb).Buffer;
        SvgRasterOptions depthOptions = new SvgRasterOptions();
        depthOptions.Tolerance = 0.001;
        depthOptions.MaximumSubdivisionDepth = 0;

        SvgRasterResult edge = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRuleAndOptions(Parse("M0 0 H4 V4 H0 Z"), edgeSurface, white, SvgFillRule.Nonzero, edgeOptions);
        SvgRasterResult work = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRuleAndOptions(Parse("M0 0 H4 V4 H0 Z"), workSurface, white, SvgFillRule.Nonzero, workOptions);
        SvgRasterResult depth = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRuleAndOptions(Parse("M0 4 Q2 0 4 4 Z"), depthSurface, white, SvgFillRule.Nonzero, depthOptions);

        Assert.Multiple(() =>
        {
            AssertFailure(edge, SvgRasterError.EdgeLimitExceeded);
            AssertFailure(work, SvgRasterError.WorkLimitExceeded);
            Assert.That(work.WorkUnits, Is.EqualTo(1024));
            AssertFailure(depth, SvgRasterError.SubdivisionLimitExceeded);
            Assert.That(edgeSurface.Data, Is.All.Zero);
            Assert.That(workSurface.Data, Is.All.Zero);
            Assert.That(depthSurface.Data, Is.All.Zero);
        });
    }

    [Test]
    public void RejectsInvalidMutableInputsAndHandlesEmptyGeometry()
    {
        SvgPixelBuffer surface = SvgPixelBuffer.Create(2, 2, SvgPixelColorSpace.Srgb).Buffer;
        SvgPremultipliedRgba8 color = new SvgPremultipliedRgba8(10, 10, 10, 10);
        SvgRasterOptions badSamples = new SvgRasterOptions();
        badSamples.SamplesPerAxis = 17;
        SvgRasterOptions badClip = new SvgRasterOptions();
        badClip.Clip = new SvgPixelRect(0, 0, 1, 1);
        badClip.Clip.Right = 2;
        SvgPathData invalidPath = Parse("M0 0 L1 1");
        invalidPath.Commands[1].Values[0] = double.NaN;
        SvgPremultipliedRgba8 invalidColor = new SvgPremultipliedRgba8(1, 1, 1, 1);
        invalidColor.Red = 2;
        SvgPixelBuffer invalidSurface = SvgPixelBuffer.Create(1, 1, SvgPixelColorSpace.Srgb).Buffer;
        invalidSurface.Width = -1;

        SvgRasterResult empty = new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRule(Parse("M1 1"), surface, color, SvgFillRule.Nonzero);

        Assert.Multiple(() =>
        {
            AssertSuccess(empty);
            Assert.That(empty.Bounds.IsEmpty, Is.True);
            Assert.That(empty.EdgeCount, Is.Zero);
            Assert.That(empty.PixelsWritten, Is.Zero);
            AssertFailure(new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRule(null, surface, color, SvgFillRule.Nonzero), SvgRasterError.InvalidArgument);
            AssertFailure(new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRule(Parse("M0 0"), surface, color, (SvgFillRule)99), SvgRasterError.InvalidArgument);
            AssertFailure(new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRuleAndOptions(Parse("M0 0"), surface, color, SvgFillRule.Nonzero, badSamples), SvgRasterError.InvalidArgument);
            AssertFailure(new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRuleAndOptions(Parse("M0 0"), surface, color, SvgFillRule.Nonzero, badClip), SvgRasterError.InvalidArgument);
            AssertFailure(new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRule(invalidPath, surface, color, SvgFillRule.Nonzero), SvgRasterError.InvalidPath);
            AssertFailure(new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRule(Parse("M0 0"), surface, invalidColor, SvgFillRule.Nonzero), SvgRasterError.InvalidArgument);
            AssertFailure(new SvgRasterizer().FillWithPathAndSurfaceAndColorAndFillRule(Parse("M0 0"), invalidSurface, color, SvgFillRule.Nonzero), SvgRasterError.InvalidArgument);
            Assert.That(surface.Data, Is.All.Zero);
        });
    }

    static SvgPathData Parse(string source)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(source, 256, 256);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static void ZeroActiveBytes(byte[] bytes, int offset, int height, int stride)
    {
        int y = 0;
        while (y < height)
        {
            int index = offset + y * stride;
            int end = index + 8;
            while (index < end)
            {
                bytes[index] = 0;
                index++;
            }
            y++;
        }
    }

    static void AssertPixel(SvgPixelBuffer surface, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        SvgPixelReadResult read = surface.Read(x, y);
        Assert.That(read.Success, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(read.Pixel.Red, Is.EqualTo(red));
            Assert.That(read.Pixel.Green, Is.EqualTo(green));
            Assert.That(read.Pixel.Blue, Is.EqualTo(blue));
            Assert.That(read.Pixel.Alpha, Is.EqualTo(alpha));
        });
    }

    static void AssertSuccess(SvgRasterResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgRasterError.None));
    }

    static void AssertFailure(SvgRasterResult result, SvgRasterError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.PixelsWritten, Is.Zero);
    }
}
