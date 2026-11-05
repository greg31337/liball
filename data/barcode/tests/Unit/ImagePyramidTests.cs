namespace barcode.Tests;

public class ImagePyramidTests
{
    [Test]
    public void IntegerRegionCropCopiesPixelsAndRecordsSourceOffset()
    {
        ManagedGrayImage source = CreateCoordinateImage(5, 4);
        BarcodeRectangle region = CreateRectangle(1.0, 1.0, 3.0, 2.0);

        ImageCropResult crop = ImageCropper.CropToRegion(source, region);
        source.WriteSample(1, 1, 255);
        BarcodePoint restored = crop.Transform.RestorePoint(
            CreatePoint(2.0, 1.0));

        Assert.Multiple(() =>
        {
            Assert.That(crop.Image.Width, Is.EqualTo(3));
            Assert.That(crop.Image.Height, Is.EqualTo(2));
            Assert.That(crop.Image.CopySamples(), Is.EqualTo(new int[]
            {
                11, 12, 13,
                21, 22, 23
            }));
            AssertRectangle(crop.PixelBounds, 1.0, 1.0, 3.0, 2.0);
            AssertPoint(restored, 3.0, 2.0);
        });
    }

    [Test]
    public void FractionalRegionUsesTheSmallestEnclosingPixelRectangle()
    {
        ManagedGrayImage source = CreateCoordinateImage(5, 4);
        BarcodeRectangle region = CreateRectangle(1.2, 0.5, 2.1, 2.0);

        ImageCropResult crop = ImageCropper.CropToRegion(source, region);

        Assert.Multiple(() =>
        {
            AssertRectangle(crop.PixelBounds, 1.0, 0.0, 3.0, 3.0);
            Assert.That(crop.Image.CopySamples(), Is.EqualTo(new int[]
            {
                1, 2, 3,
                11, 12, 13,
                21, 22, 23
            }));
        });
    }

    [Test]
    public void MissingRegionCopiesTheCompleteImage()
    {
        ManagedGrayImage source = CreateCoordinateImage(3, 2);

        ImageCropResult crop = ImageCropper.CropToRegion(source, null);
        crop.Image.WriteSample(0, 0, 200);

        Assert.Multiple(() =>
        {
            AssertRectangle(crop.PixelBounds, 0.0, 0.0, 3.0, 2.0);
            Assert.That(source.ReadSample(0, 0), Is.Zero);
            Assert.That(crop.Image, Is.Not.SameAs(source));
        });
    }

    [Test]
    public void DownsamplingAveragesTwoByTwoBlocksWithOddEdgeSupport()
    {
        ManagedGrayImage source = CreateGrayImage(
            5,
            3,
            new int[]
            {
                0, 10, 20, 30, 40,
                50, 60, 70, 80, 90,
                100, 110, 120, 130, 140
            });

        ManagedGrayImage reduced =
            ImagePyramidBuilder.DownsampleByTwo(source);

        Assert.Multiple(() =>
        {
            Assert.That(reduced.Width, Is.EqualTo(3));
            Assert.That(reduced.Height, Is.EqualTo(2));
            Assert.That(reduced.CopySamples(), Is.EqualTo(new int[]
            {
                30, 50, 65,
                105, 125, 140
            }));
        });
    }

    [Test]
    public void PyramidBuildsDeterministicLevelsScalesAndSamples()
    {
        ManagedGrayImage source = CreateGrayImage(
            6,
            4,
            new int[]
            {
                0, 10, 20, 30, 40, 50,
                60, 70, 80, 90, 100, 110,
                120, 130, 140, 150, 160, 170,
                180, 190, 200, 210, 220, 230
            });

        ImagePyramid pyramid = ImagePyramidBuilder.Build(
            source,
            null,
            4,
            1);
        source.WriteSample(0, 0, 255);

        Assert.Multiple(() =>
        {
            Assert.That(pyramid.Levels, Has.Length.EqualTo(3));
            Assert.That(pyramid.Levels[0].Scale, Is.EqualTo(1));
            Assert.That(pyramid.Levels[0].Image.Width, Is.EqualTo(6));
            Assert.That(pyramid.Levels[0].Image.Height, Is.EqualTo(4));
            Assert.That(pyramid.Levels[0].Image.ReadSample(0, 0), Is.Zero);
            Assert.That(pyramid.Levels[1].Scale, Is.EqualTo(2));
            Assert.That(pyramid.Levels[1].Image.CopySamples(),
                Is.EqualTo(new int[]
                {
                    35, 55, 75,
                    155, 175, 195
                }));
            Assert.That(pyramid.Levels[2].Scale, Is.EqualTo(4));
            Assert.That(pyramid.Levels[2].Image.CopySamples(),
                Is.EqualTo(new int[] { 105, 135 }));
        });
    }

    [Test]
    public void PyramidLevelTransformRestoresRoiCoordinates()
    {
        ManagedGrayImage source = CreateCoordinateImage(8, 6);
        BarcodeRectangle region = CreateRectangle(2.0, 1.0, 4.0, 4.0);
        ImagePyramid pyramid = ImagePyramidBuilder.Build(
            source,
            region,
            4,
            1);
        ImagePyramidLevel level = pyramid.Levels[2];

        BarcodePoint restored = level.Transform.RestorePoint(
            CreatePoint(0.5, 0.5));
        BarcodePoint mapped = level.Transform.MapSourcePoint(restored);
        BarcodeRectangle restoredRectangle = level.Transform.RestoreRectangle(
            CreateRectangle(0.25, 0.5, 0.5, 0.25));
        BarcodeRectangle mappedRectangle = level.Transform.MapSourceRectangle(
            restoredRectangle);

        Assert.Multiple(() =>
        {
            Assert.That(level.Level, Is.EqualTo(2));
            Assert.That(level.Scale, Is.EqualTo(4));
            Assert.That(level.Image.Width, Is.EqualTo(1));
            Assert.That(level.Image.Height, Is.EqualTo(1));
            AssertPoint(restored, 4.0, 3.0);
            AssertPoint(mapped, 0.5, 0.5);
            AssertRectangle(
                restoredRectangle,
                3.0,
                3.0,
                2.0,
                1.0);
            AssertRectangle(
                mappedRectangle,
                0.25,
                0.5,
                0.5,
                0.25);
        });
    }

    [Test]
    public void QuadrilateralRestorationCopiesCornersAndRecalculatesBounds()
    {
        ImageCoordinateTransform transform =
            new ImageCoordinateTransform(2.0, 3.0, 10.0, 20.0);
        BarcodeQuadrilateral local = GeometryMath.CreateQuadrilateral(
            CreatePoint(0.0, 0.0),
            CreatePoint(2.0, 0.0),
            CreatePoint(2.0, 1.0),
            CreatePoint(0.0, 1.0));

        BarcodeQuadrilateral restored =
            transform.RestoreQuadrilateral(local);
        local.TopLeft!.X = 99.0;

        Assert.Multiple(() =>
        {
            AssertPoint(restored.TopLeft!, 10.0, 20.0);
            AssertPoint(restored.TopRight!, 14.0, 20.0);
            AssertPoint(restored.BottomRight!, 14.0, 23.0);
            AssertPoint(restored.BottomLeft!, 10.0, 23.0);
            AssertRectangle(restored.Bounds!, 10.0, 20.0, 4.0, 3.0);
        });
    }

    [Test]
    public void ScaleSelectionPrefersInRangeLevelNearestTheTarget()
    {
        ImagePyramid pyramid = ImagePyramidBuilder.Build(
            CreateCoordinateImage(16, 16),
            null,
            4,
            2);

        ImagePyramidLevel tenPixels = ImagePyramidSelector.SelectLevel(
            pyramid,
            10.0,
            2.0,
            4.0);
        ImagePyramidLevel onePixel = ImagePyramidSelector.SelectLevel(
            pyramid,
            1.0,
            2.0,
            4.0);
        ImagePyramidLevel fortyPixels = ImagePyramidSelector.SelectLevel(
            pyramid,
            40.0,
            2.0,
            4.0);
        ImagePyramidLevel tied = ImagePyramidSelector.SelectLevel(
            pyramid,
            8.0,
            2.0,
            4.0);

        Assert.Multiple(() =>
        {
            Assert.That(tenPixels.Level, Is.EqualTo(2));
            Assert.That(onePixel.Level, Is.EqualTo(0));
            Assert.That(fortyPixels.Level, Is.EqualTo(3));
            Assert.That(tied.Level, Is.EqualTo(1));
        });
    }

    [Test]
    public void InvalidRegionsPyramidSettingsAndTransformsAreRejected()
    {
        ManagedGrayImage source = CreateCoordinateImage(4, 3);
        BarcodeRectangle zeroWidth = CreateRectangle(1.0, 1.0, 0.0, 1.0);
        BarcodeRectangle outside = CreateRectangle(3.0, 1.0, 2.0, 1.0);
        BarcodeRectangle negative = CreateRectangle(-0.1, 0.0, 1.0, 1.0);
        BarcodeRectangle notFinite = CreateRectangle(0.0, 0.0, 1.0, 1.0);
        notFinite.X = double.NaN;
        ImagePyramid pyramid = ImagePyramidBuilder.Build(
            source,
            null,
            2,
            1);
        ImagePyramid empty = new ImagePyramid();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => ImageCropper.CropToRegion(source, zeroWidth),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageCropper.CropToRegion(source, outside),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageCropper.CropToRegion(source, negative),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageCropper.CropToRegion(source, notFinite),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImagePyramidBuilder.Build(source, null, 0, 1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImagePyramidBuilder.Build(
                    source,
                    null,
                    ImagePyramidBuilder.MaximumLevelCount + 1,
                    1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImagePyramidBuilder.Build(source, null, 1, 0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImagePyramidBuilder.DownsampleByTwo(null),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImagePyramidSelector.SelectLevel(
                    null,
                    4.0,
                    2.0,
                    4.0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImagePyramidSelector.SelectLevel(
                    empty,
                    4.0,
                    2.0,
                    4.0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImagePyramidSelector.SelectLevel(
                    pyramid,
                    double.NaN,
                    2.0,
                    4.0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImagePyramidSelector.SelectLevel(
                    pyramid,
                    4.0,
                    5.0,
                    4.0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => new ImageCoordinateTransform(
                    0.0,
                    1.0,
                    0.0,
                    0.0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => new ImageCoordinateTransform(
                    1.0,
                    double.PositiveInfinity,
                    0.0,
                    0.0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => pyramid.Levels[0].Transform.RestorePoint(null),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => pyramid.Levels[0].Transform.RestoreRectangle(null),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => pyramid.Levels[0].Transform.RestoreQuadrilateral(null),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static ManagedGrayImage CreateCoordinateImage(int width, int height)
    {
        ManagedGrayImage image = new ManagedGrayImage(width, height);
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                image.WriteSample(column, row, (row * 10) + column);
                column = column + 1;
            }

            row = row + 1;
        }

        return image;
    }

    static ManagedGrayImage CreateGrayImage(
        int width,
        int height,
        int[] samples)
    {
        ManagedGrayImage image = new ManagedGrayImage(width, height);
        int index = 0;
        while (index < samples.Length)
        {
            image.Samples[index] = samples[index];
            index = index + 1;
        }

        return image;
    }

    static BarcodePoint CreatePoint(double x, double y)
    {
        return GeometryMath.CreatePoint(x, y);
    }

    static BarcodeRectangle CreateRectangle(
        double x,
        double y,
        double width,
        double height)
    {
        return GeometryMath.CreateRectangle(x, y, width, height);
    }

    static void AssertPoint(
        BarcodePoint point,
        double expectedX,
        double expectedY)
    {
        Assert.Multiple(() =>
        {
            Assert.That(point.X,
                Is.EqualTo(expectedX).Within(0.000000001));
            Assert.That(point.Y,
                Is.EqualTo(expectedY).Within(0.000000001));
        });
    }

    static void AssertRectangle(
        BarcodeRectangle rectangle,
        double expectedX,
        double expectedY,
        double expectedWidth,
        double expectedHeight)
    {
        Assert.Multiple(() =>
        {
            Assert.That(rectangle.X,
                Is.EqualTo(expectedX).Within(0.000000001));
            Assert.That(rectangle.Y,
                Is.EqualTo(expectedY).Within(0.000000001));
            Assert.That(rectangle.Width,
                Is.EqualTo(expectedWidth).Within(0.000000001));
            Assert.That(rectangle.Height,
                Is.EqualTo(expectedHeight).Within(0.000000001));
        });
    }
}
