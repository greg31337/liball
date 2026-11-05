namespace barcode.Tests;

public class ImageIngestionPreprocessingMatrixTests
{
    [TestCase(BarcodePixelFormat.Gray8)]
    [TestCase(BarcodePixelFormat.Rgb24)]
    [TestCase(BarcodePixelFormat.Bgr24)]
    [TestCase(BarcodePixelFormat.Rgba32)]
    [TestCase(BarcodePixelFormat.Bgra32)]
    public void EveryBufferLayoutFlowsThroughEveryThresholdAndPolarity(
        BarcodePixelFormat format)
    {
        bool[] negativeStrides = new bool[] { false, true };
        BarcodeBinarizationMode[] modes = new BarcodeBinarizationMode[]
        {
            BarcodeBinarizationMode.Global,
            BarcodeBinarizationMode.Otsu,
            BarcodeBinarizationMode.Row,
            BarcodeBinarizationMode.Adaptive,
            BarcodeBinarizationMode.Hybrid,
            BarcodeBinarizationMode.Automatic
        };
        BarcodePolarity[] polarities = new BarcodePolarity[]
        {
            BarcodePolarity.DarkOnLight,
            BarcodePolarity.LightOnDark,
            BarcodePolarity.Automatic
        };

        int strideIndex = 0;
        while (strideIndex < negativeStrides.Length)
        {
            BarcodeImage input = CreatePatternImage(
                format,
                negativeStrides[strideIndex]);
            ManagedGrayImage ingested = ImageIngestion.ConvertToGray8(input);
            BarcodeRectangle region = new BarcodeRectangle();
            region.X = 1.2;
            region.Y = 0.6;
            region.Width = 14.1;
            region.Height = 8.1;
            ImageCropResult crop = ImageCropper.CropToRegion(
                ingested,
                region);
            int[] originalSamples = crop.Image.CopySamples();

            Assert.Multiple(() =>
            {
                Assert.That(ingested.Width, Is.EqualTo(18));
                Assert.That(ingested.Height, Is.EqualTo(10));
                Assert.That(ingested.ReadSample(0, 0), Is.EqualTo(32));
                Assert.That(ingested.ReadSample(2, 0), Is.EqualTo(224));
                Assert.That(crop.Image.Width, Is.EqualTo(15));
                Assert.That(crop.Image.Height, Is.EqualTo(9));
                Assert.That(crop.PixelBounds.X, Is.EqualTo(1.0));
                Assert.That(crop.PixelBounds.Y, Is.EqualTo(0.0));
                Assert.That(crop.PixelBounds.Width, Is.EqualTo(15.0));
                Assert.That(crop.PixelBounds.Height, Is.EqualTo(9.0));
                Assert.That(crop.Transform.OffsetX, Is.EqualTo(1.0));
                Assert.That(crop.Transform.OffsetY, Is.EqualTo(0.0));
            });

            int modeIndex = 0;
            while (modeIndex < modes.Length)
            {
                int polarityIndex = 0;
                while (polarityIndex < polarities.Length)
                {
                    BarcodeBinarizationMode requestedMode = modes[modeIndex];
                    BarcodePolarity requestedPolarity = polarities[polarityIndex];
                    ImageBinarizationResult result = ImageBinarizer.Binarize(
                        crop.Image,
                        requestedMode,
                        requestedPolarity);
                    int foregroundCount = CountForeground(result.Modules);
                    string context = "format=" + format
                        + ", negativeStride=" + negativeStrides[strideIndex]
                        + ", mode=" + requestedMode
                        + ", polarity=" + requestedPolarity;

                    Assert.Multiple(() =>
                    {
                        Assert.That(
                            result.Modules.Width,
                            Is.EqualTo(crop.Image.Width),
                            context);
                        Assert.That(
                            result.Modules.Height,
                            Is.EqualTo(crop.Image.Height),
                            context);
                        Assert.That(
                            foregroundCount,
                            Is.InRange(
                                1,
                                (crop.Image.Width * crop.Image.Height) - 1),
                            context);
                        Assert.That(
                            crop.Image.CopySamples(),
                            Is.EqualTo(originalSamples),
                            context);
                        if (requestedMode == BarcodeBinarizationMode.Automatic)
                        {
                            Assert.That(
                                result.Mode,
                                Is.Not.EqualTo(BarcodeBinarizationMode.Automatic),
                                context);
                        }
                        else
                        {
                            Assert.That(result.Mode, Is.EqualTo(requestedMode), context);
                        }

                        if (requestedPolarity == BarcodePolarity.Automatic)
                        {
                            Assert.That(
                                result.Polarity,
                                Is.Not.EqualTo(BarcodePolarity.Automatic),
                                context);
                        }
                        else
                        {
                            Assert.That(
                                result.Polarity,
                                Is.EqualTo(requestedPolarity),
                                context);
                        }
                    });

                    polarityIndex = polarityIndex + 1;
                }

                modeIndex = modeIndex + 1;
            }

            strideIndex = strideIndex + 1;
        }
    }

    [TestCase(BarcodePixelFormat.Gray8)]
    [TestCase(BarcodePixelFormat.Rgb24)]
    [TestCase(BarcodePixelFormat.Bgr24)]
    [TestCase(BarcodePixelFormat.Rgba32)]
    [TestCase(BarcodePixelFormat.Bgra32)]
    public void EveryPixelFormatRejectsTruncatedRowsInBothStrideDirections(
        BarcodePixelFormat format)
    {
        BarcodeImage positive = CreateTruncatedImage(format, false);
        BarcodeImage negative = CreateTruncatedImage(format, true);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => ImageIngestion.ConvertToGray8(positive),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageIngestion.ConvertToGray8(negative),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [TestCase(0, 200, 100, 100)]
    [TestCase(1, 111, 111, 100)]
    [TestCase(2, 255, 89, 100)]
    [TestCase(3, 255, 84, 128)]
    public void EveryPreprocessingFilterProducesItsExpectedIndependentImage(
        int preprocessingValue,
        int expectedCenter,
        int expectedNeighbor,
        int expectedCorner)
    {
        ManagedGrayImage source = CreateImpulseImage();
        ImagePreprocessingKind preprocessing =
            (ImagePreprocessingKind)preprocessingValue;

        ManagedGrayImage prepared = ImageFallbackPipeline.PrepareImage(
            source,
            preprocessing);
        source.WriteSample(4, 4, 0);

        Assert.Multiple(() =>
        {
            Assert.That(prepared, Is.Not.SameAs(source));
            Assert.That(prepared.Width, Is.EqualTo(9));
            Assert.That(prepared.Height, Is.EqualTo(9));
            Assert.That(prepared.ReadSample(4, 4), Is.EqualTo(expectedCenter));
            Assert.That(prepared.ReadSample(3, 4), Is.EqualTo(expectedNeighbor));
            Assert.That(prepared.ReadSample(0, 0), Is.EqualTo(expectedCorner));
        });
    }

    static BarcodeImage CreatePatternImage(
        BarcodePixelFormat format,
        bool negativeStride)
    {
        int width = 18;
        int height = 10;
        int bytesPerPixel = ResolveBytesPerPixel(format);
        int rowBytes = width * bytesPerPixel;
        int absoluteStride = rowBytes + 5;
        int prefix = 3;
        int offset = negativeStride
            ? prefix + (absoluteStride * (height - 1))
            : prefix;
        int stride = negativeStride ? -absoluteStride : absoluteStride;
        byte[] pixels = new byte[
            prefix + (absoluteStride * (height - 1)) + rowBytes + 4];
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                int sample = (((column / 2) + (row / 2)) % 2) == 0
                    ? 32
                    : 224;
                int pixelIndex = offset
                    + (row * stride)
                    + (column * bytesPerPixel);
                WriteNeutralPixel(
                    pixels,
                    pixelIndex,
                    format,
                    sample,
                    (column * 13) + row);
                column = column + 1;
            }

            row = row + 1;
        }

        BarcodeImage image = new BarcodeImage();
        image.Pixels = pixels;
        image.Width = width;
        image.Height = height;
        image.Stride = stride;
        image.Offset = offset;
        image.PixelFormat = format;
        image.DpiX = 203.0;
        image.DpiY = 300.0;
        return image;
    }

    static BarcodeImage CreateTruncatedImage(
        BarcodePixelFormat format,
        bool negativeStride)
    {
        int width = 4;
        int height = 3;
        int bytesPerPixel = ResolveBytesPerPixel(format);
        int rowBytes = width * bytesPerPixel;
        int absoluteStride = rowBytes + 2;
        int prefix = 1;
        int requiredLength = prefix
            + (absoluteStride * (height - 1))
            + rowBytes;
        BarcodeImage image = new BarcodeImage();
        image.Pixels = new byte[requiredLength - 1];
        image.Width = width;
        image.Height = height;
        image.Stride = negativeStride ? -absoluteStride : absoluteStride;
        image.Offset = negativeStride
            ? prefix + (absoluteStride * (height - 1))
            : prefix;
        image.PixelFormat = format;
        return image;
    }

    static ManagedGrayImage CreateImpulseImage()
    {
        ManagedGrayImage image = new ManagedGrayImage(9, 9);
        int row = 0;
        while (row < image.Height)
        {
            int column = 0;
            while (column < image.Width)
            {
                image.WriteSample(column, row, 100);
                column = column + 1;
            }

            row = row + 1;
        }

        image.WriteSample(4, 4, 200);
        return image;
    }

    static int CountForeground(ModuleMatrix modules)
    {
        int count = 0;
        int row = 0;
        while (row < modules.Height)
        {
            int column = 0;
            while (column < modules.Width)
            {
                if (modules.GetModule(column, row))
                {
                    count = count + 1;
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return count;
    }

    static int ResolveBytesPerPixel(BarcodePixelFormat format)
    {
        if (format == BarcodePixelFormat.Gray8)
        {
            return 1;
        }

        if (format == BarcodePixelFormat.Rgb24
            || format == BarcodePixelFormat.Bgr24)
        {
            return 3;
        }

        return 4;
    }

    static void WriteNeutralPixel(
        byte[] pixels,
        int index,
        BarcodePixelFormat format,
        int sample,
        int alpha)
    {
        pixels[index] = (byte)sample;
        if (format == BarcodePixelFormat.Gray8)
        {
            return;
        }

        pixels[index + 1] = (byte)sample;
        pixels[index + 2] = (byte)sample;
        if (format == BarcodePixelFormat.Rgba32
            || format == BarcodePixelFormat.Bgra32)
        {
            pixels[index + 3] = (byte)(alpha % 256);
        }
    }
}
