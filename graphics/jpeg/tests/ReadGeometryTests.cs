namespace jpeg.Tests;

internal class ReadGeometryTests
{
    [Test]
    public void Read_DefaultGeometryReportsCompleteFullScaleOutput()
    {
        byte[] data = global::jpeg.Jpeg.Write(CreateBlockImage(16, 8));

        global::jpeg.JpegImage image = global::jpeg.Jpeg.Read(data);

        Assert.That(image.Width, Is.EqualTo(16));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.SourceWidth, Is.EqualTo(16));
        Assert.That(image.SourceHeight, Is.EqualTo(8));
        Assert.That(image.AppliedIdctScaleRatio, Is.EqualTo(global::jpeg.JpegIdctScaleRatio.Full));
        Assert.That(image.AppliedIdctScaleNumerator, Is.EqualTo(1));
        Assert.That(image.AppliedIdctScaleDenominator, Is.EqualTo(1));
        AssertRectangle(image.DecodedRegion, 0, 0, 16, 8);
        AssertRectangle(image.OutputRectangle, 0, 0, 16, 8);
        Assert.That(image.FastMcuCropApplied, Is.False);
    }

    [Test]
    public void Read_TargetDimensionsChooseLargestSupportedIdctRatioWithinBounds()
    {
        byte[] data = global::jpeg.Jpeg.Write(CreateBlockImage(32, 16));
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.TargetWidth = 12;
        options.TargetHeight = 12;

        global::jpeg.JpegImage image = global::jpeg.Jpeg.Read(data, options);

        Assert.That(image.Width, Is.EqualTo(12));
        Assert.That(image.Height, Is.EqualTo(6));
        Assert.That(image.AppliedIdctScaleRatio, Is.EqualTo(global::jpeg.JpegIdctScaleRatio.ThreeEighths));
        Assert.That(image.AppliedIdctScaleNumerator, Is.EqualTo(3));
        Assert.That(image.AppliedIdctScaleDenominator, Is.EqualTo(8));
        AssertRectangle(image.OutputRectangle, 0, 0, 12, 6);
        Assert.That(options.IdctScaleNumerator, Is.EqualTo(1));
        Assert.That(options.IdctScaleDenominator, Is.EqualTo(1));
    }

    [Test]
    public void Read_NamedAndExplicitSupportedRatiosProduceReducedIdctDimensions()
    {
        byte[] data = global::jpeg.Jpeg.Write(CreateBlockImage(17, 9));
        global::jpeg.JpegReadOptions namedOptions = new global::jpeg.JpegReadOptions();
        namedOptions.IdctScaleRatio = global::jpeg.JpegIdctScaleRatio.OneHalf;
        global::jpeg.JpegReadOptions explicitOptions = new global::jpeg.JpegReadOptions();
        explicitOptions.IdctScaleNumerator = 3;
        explicitOptions.IdctScaleDenominator = 4;

        global::jpeg.JpegImage half = global::jpeg.Jpeg.Read(data, namedOptions);
        global::jpeg.JpegImage threeQuarters = global::jpeg.Jpeg.Read(data, explicitOptions);

        Assert.That(half.Width, Is.EqualTo(9));
        Assert.That(half.Height, Is.EqualTo(5));
        Assert.That(half.Pixels[0], Is.EqualTo(138));
        Assert.That(half.Pixels[4], Is.EqualTo(143));
        Assert.That(half.AppliedIdctScaleRatio, Is.EqualTo(global::jpeg.JpegIdctScaleRatio.OneHalf));
        Assert.That(threeQuarters.Width, Is.EqualTo(13));
        Assert.That(threeQuarters.Height, Is.EqualTo(7));
        Assert.That(threeQuarters.AppliedIdctScaleRatio, Is.EqualTo(global::jpeg.JpegIdctScaleRatio.ThreeQuarters));
    }

    [Test]
    public void Read_ReducedIdctScalesThreeComponentOutputPlanesTogether()
    {
        global::jpeg.JpegImage source = new global::jpeg.JpegImage();
        source.Width = 16;
        source.Height = 16;
        source.Precision = 8;
        source.ComponentCount = 3;
        source.PixelFormat = global::jpeg.JpegPixelFormat.Rgb24;
        source.ColorSpace = global::jpeg.JpegColorSpace.RGB;
        source.Pixels = new byte[16 * 16 * 3];
        int index = 0;
        while (index < source.Pixels.Length)
        {
            source.Pixels[index] = 100;
            source.Pixels[index + 1] = 150;
            source.Pixels[index + 2] = 200;
            index = index + 3;
        }

        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.IdctScaleRatio = global::jpeg.JpegIdctScaleRatio.OneQuarter;
        options.ColorPolicy = global::jpeg.JpegColorPolicy.ConvertToRgb;
        options.OutputPixelFormat = global::jpeg.JpegPixelFormat.Rgb24;

        global::jpeg.JpegImage image = global::jpeg.Jpeg.Read(global::jpeg.Jpeg.Write(source), options);

        Assert.That(image.Width, Is.EqualTo(4));
        Assert.That(image.Height, Is.EqualTo(4));
        Assert.That(image.Pixels.Length, Is.EqualTo(4 * 4 * 3));
        Assert.That(image.ComponentCount, Is.EqualTo(3));
        Assert.That(image.AppliedIdctScaleRatio, Is.EqualTo(global::jpeg.JpegIdctScaleRatio.OneQuarter));
    }

    [Test]
    public void Read_ExactRegionCropsScaledPixelsAndReportsBothCoordinateSpaces()
    {
        byte[] data = global::jpeg.Jpeg.Write(CreateBlockImage(24, 16));
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.IdctScaleRatio = global::jpeg.JpegIdctScaleRatio.OneHalf;
        options.DecodeRegion.X = 8;
        options.DecodeRegion.Y = 0;
        options.DecodeRegion.Width = 16;
        options.DecodeRegion.Height = 16;

        global::jpeg.JpegImage image = global::jpeg.Jpeg.Read(data, options);

        Assert.That(image.Width, Is.EqualTo(8));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.Pixels[0], Is.EqualTo(143));
        Assert.That(image.Pixels[4], Is.EqualTo(133));
        AssertRectangle(image.DecodedRegion, 8, 0, 16, 16);
        AssertRectangle(image.OutputRectangle, 4, 0, 8, 8);
        Assert.That(image.FastMcuCropApplied, Is.False);
    }

    [Test]
    public void Read_FastCropExpandsRequestedRegionToMcuBoundaries()
    {
        byte[] data = global::jpeg.Jpeg.Write(CreateBlockImage(24, 16));
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.DecodeRegion.X = 9;
        options.DecodeRegion.Y = 1;
        options.DecodeRegion.Width = 5;
        options.DecodeRegion.Height = 6;
        options.FastMcuCrop = true;

        global::jpeg.JpegImage image = global::jpeg.Jpeg.Read(data, options);

        Assert.That(image.Width, Is.EqualTo(8));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.Pixels, Is.All.EqualTo(143));
        AssertRectangle(image.DecodedRegion, 8, 0, 8, 8);
        AssertRectangle(image.OutputRectangle, 8, 0, 8, 8);
        Assert.That(image.FastMcuCropApplied, Is.True);
    }

    [Test]
    public void Read_ExactRegionUsesTheRequestedVerticalOffset()
    {
        byte[] data = global::jpeg.Jpeg.Write(CreateBlockImage(16, 16));
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.DecodeRegion.X = 0;
        options.DecodeRegion.Y = 8;
        options.DecodeRegion.Width = 8;
        options.DecodeRegion.Height = 8;

        global::jpeg.JpegImage image = global::jpeg.Jpeg.Read(data, options);

        Assert.That(image.Width, Is.EqualTo(8));
        Assert.That(image.Height, Is.EqualTo(8));
        Assert.That(image.Pixels, Is.All.EqualTo(128));
        AssertRectangle(image.OutputRectangle, 0, 8, 8, 8);
    }

    [Test]
    public void Read_ProgressivePreviewsUseTheResolvedTargetGeometry()
    {
        byte[] data = CallbackStreamingApiTests.CreateProgressiveGrayscaleJpeg(143);
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.TargetWidth = 4;
        options.TargetHeight = 4;
        TrackingProgressivePreviews callback = new TrackingProgressivePreviews();

        global::jpeg.JpegImage image = new global::jpeg.JpegReader().ReadProgressivePreviewsWithOptionsAndCancellation(
            data,
            options,
            callback,
            default(System.Threading.CancellationToken));

        Assert.That(callback.ScanIndexes, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(image.Width, Is.EqualTo(4));
        Assert.That(image.Height, Is.EqualTo(4));
        Assert.That(image.AppliedIdctScaleRatio, Is.EqualTo(global::jpeg.JpegIdctScaleRatio.OneHalf));
        AssertRectangle(image.OutputRectangle, 0, 0, 4, 4);
    }

    [Test]
    public void Read_RejectsInvalidTargetsRatiosAndRegions()
    {
        byte[] data = global::jpeg.Jpeg.Write(CreateBlockImage(16, 16));
        global::jpeg.JpegReadOptions negativeTarget = new global::jpeg.JpegReadOptions();
        negativeTarget.TargetWidth = -1;
        global::jpeg.JpegReadOptions badRatio = new global::jpeg.JpegReadOptions();
        badRatio.IdctScaleRatio = (global::jpeg.JpegIdctScaleRatio)9;
        global::jpeg.JpegReadOptions incompleteRegion = new global::jpeg.JpegReadOptions();
        incompleteRegion.DecodeRegion.Width = 8;
        global::jpeg.JpegReadOptions outsideRegion = new global::jpeg.JpegReadOptions();
        outsideRegion.DecodeRegion.X = 12;
        outsideRegion.DecodeRegion.Y = 0;
        outsideRegion.DecodeRegion.Width = 8;
        outsideRegion.DecodeRegion.Height = 8;

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate { global::jpeg.Jpeg.Read(data, negativeTarget); });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate { global::jpeg.Jpeg.Read(data, badRatio); });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate { global::jpeg.Jpeg.Read(data, incompleteRegion); });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate { global::jpeg.Jpeg.Read(data, outsideRegion); });
    }

    internal static global::jpeg.JpegImage CreateBlockImage(int width, int height)
    {
        global::jpeg.JpegImage image = JpegWriteApiTests.CreateGrayscaleImage(width, height, 138);
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int blockX = x / 8;
                int blockY = y / 8;
                int value = 138;
                if (blockX == 1 && blockY == 0)
                {
                    value = 143;
                }
                else if (blockX == 2 && blockY == 0)
                {
                    value = 133;
                }
                else if (blockY == 1)
                {
                    value = 128;
                }

                image.Pixels[y * width + x] = (byte)value;
                x = x + 1;
            }

            y = y + 1;
        }

        return image;
    }

    internal static void AssertRectangle(global::jpeg.JpegRectangle rectangle, int x, int y, int width, int height)
    {
        Assert.That(rectangle.X, Is.EqualTo(x));
        Assert.That(rectangle.Y, Is.EqualTo(y));
        Assert.That(rectangle.Width, Is.EqualTo(width));
        Assert.That(rectangle.Height, Is.EqualTo(height));
    }
}
