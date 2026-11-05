namespace barcode.Tests;

public class ImageFilterMorphologyTests
{
    [Test]
    public void BoxSmoothingUsesClippedWindowsAndRoundedMeans()
    {
        ManagedGrayImage image = CreateGrayImage(
            3,
            3,
            new int[]
            {
                0, 10, 20,
                30, 40, 50,
                60, 70, 80
            });

        ManagedGrayImage smoothed = ImageFilter.BoxSmooth(image, 1);
        ManagedGrayImage copied = ImageFilter.BoxSmooth(image, 0);
        image.WriteSample(0, 0, 255);

        Assert.Multiple(() =>
        {
            Assert.That(smoothed.CopySamples(), Is.EqualTo(new int[]
            {
                20, 25, 30,
                35, 40, 45,
                50, 55, 60
            }));
            Assert.That(copied.CopySamples(), Is.EqualTo(new int[]
            {
                0, 10, 20,
                30, 40, 50,
                60, 70, 80
            }));
            Assert.That(copied, Is.Not.SameAs(image));
        });
    }

    [Test]
    public void UnsharpEnhancementStrengthensEdgesAndClampsSamples()
    {
        ManagedGrayImage image = CreateGrayImage(
            3,
            3,
            new int[]
            {
                100, 100, 100,
                100, 200, 100,
                100, 100, 100
            });

        ManagedGrayImage sharpened = ImageFilter.UnsharpEnhance(
            image,
            1,
            1.0);
        ManagedGrayImage unchanged = ImageFilter.UnsharpEnhance(
            image,
            1,
            0.0);

        Assert.Multiple(() =>
        {
            Assert.That(sharpened.CopySamples(), Is.EqualTo(new int[]
            {
                75, 83, 75,
                83, 255, 83,
                75, 83, 75
            }));
            Assert.That(unchanged.CopySamples(),
                Is.EqualTo(image.CopySamples()));
            Assert.That(unchanged, Is.Not.SameAs(image));
        });
    }

    [Test]
    public void IlluminationNormalizationCentersLocalLinearBrightness()
    {
        ManagedGrayImage image = CreateGrayImage(
            5,
            1,
            new int[] { 80, 100, 120, 140, 160 });

        ManagedGrayImage normalized =
            ImageNormalizer.NormalizeIllumination(image, 1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.CopySamples(),
                Is.EqualTo(new int[] { 108, 128, 128, 128, 148 }));
            Assert.That(image.CopySamples(),
                Is.EqualTo(new int[] { 80, 100, 120, 140, 160 }));
        });
    }

    [Test]
    public void IlluminationNormalizationAmplifiesLocalMarksAndClamps()
    {
        ManagedGrayImage image = CreateFilledGrayImage(3, 3, 200);
        image.WriteSample(1, 1, 0);

        ManagedGrayImage normalized =
            ImageNormalizer.NormalizeIllumination(image, 1, 4);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.ReadSample(1, 1), Is.Zero);
            Assert.That(normalized.ReadSample(0, 0),
                Is.GreaterThan(128));
            Assert.That(normalized.CopySamples(),
                Is.All.InRange(0, 255));
        });
    }

    [Test]
    public void BinaryDilationAndErosionUseABoundedDiskKernel()
    {
        ModuleMatrix point = new ModuleMatrix(5, 5);
        point.SetModule(2, 2, true);

        ModuleMatrix dilated = Morphology.Dilate(point, 1);
        ModuleMatrix restored = Morphology.Erode(dilated, 1);

        Assert.Multiple(() =>
        {
            Assert.That(CountForeground(dilated), Is.EqualTo(5));
            Assert.That(dilated.GetModule(2, 2), Is.True);
            Assert.That(dilated.GetModule(2, 1), Is.True);
            Assert.That(dilated.GetModule(1, 2), Is.True);
            Assert.That(dilated.GetModule(1, 1), Is.False);
            Assert.That(restored.CopyModules(),
                Is.EqualTo(point.CopyModules()));
        });
    }

    [Test]
    public void OpeningRemovesAnIsolatedPointAndClosingFillsAHole()
    {
        ModuleMatrix point = new ModuleMatrix(5, 5);
        point.SetModule(2, 2, true);
        ModuleMatrix hole = new ModuleMatrix(5, 5);
        hole.SetAllModules(true);
        hole.SetModule(2, 2, false);

        ModuleMatrix opened = Morphology.Open(point, 1);
        ModuleMatrix closed = Morphology.Close(hole, 1);

        Assert.Multiple(() =>
        {
            Assert.That(CountForeground(opened), Is.Zero);
            Assert.That(closed.GetModule(2, 2), Is.True);
            Assert.That(closed.GetModule(1, 1), Is.True);
            Assert.That(closed.GetModule(0, 0), Is.False);
        });
    }

    [Test]
    public void MajorityAndInversionReturnIndependentBinaryImages()
    {
        ModuleMatrix cross = new ModuleMatrix(3, 3);
        cross.SetModule(1, 0, true);
        cross.SetModule(0, 1, true);
        cross.SetModule(1, 1, true);
        cross.SetModule(2, 1, true);
        cross.SetModule(1, 2, true);

        ModuleMatrix majority = Morphology.MajorityFilter(cross, 1);
        ModuleMatrix inverted = Morphology.Invert(cross);
        ModuleMatrix restored = Morphology.Invert(inverted);
        cross.ClearModules();

        Assert.Multiple(() =>
        {
            Assert.That(majority.CopyModules(), Is.All.EqualTo(true));
            Assert.That(CountForeground(inverted), Is.EqualTo(4));
            Assert.That(CountForeground(restored), Is.EqualTo(5));
        });
    }

    [Test]
    public void GrayscaleMorphologySelectsLocalDiskExtrema()
    {
        ManagedGrayImage highPoint = CreateFilledGrayImage(3, 3, 10);
        highPoint.WriteSample(1, 1, 200);
        ManagedGrayImage lowPoint = CreateFilledGrayImage(3, 3, 200);
        lowPoint.WriteSample(1, 1, 10);

        ManagedGrayImage dilated = Morphology.DilateGray(highPoint, 1);
        ManagedGrayImage eroded = Morphology.ErodeGray(lowPoint, 1);

        Assert.Multiple(() =>
        {
            Assert.That(dilated.CopySamples(), Is.EqualTo(new int[]
            {
                10, 200, 10,
                200, 200, 200,
                10, 200, 10
            }));
            Assert.That(eroded.CopySamples(), Is.EqualTo(new int[]
            {
                200, 10, 200,
                10, 10, 10,
                200, 10, 200
            }));
        });
    }

    [Test]
    public void InvalidFilterNormalizerAndMorphologySettingsAreRejected()
    {
        ManagedGrayImage gray = CreateFilledGrayImage(2, 2, 100);
        ModuleMatrix binary = new ModuleMatrix(2, 2);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => ImageFilter.BoxSmooth(null, 1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageFilter.BoxSmooth(gray, -1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageFilter.BoxSmooth(
                    gray,
                    ImageFilter.MaximumBoxRadius + 1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageFilter.UnsharpEnhance(
                    gray,
                    1,
                    double.NaN),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageFilter.UnsharpEnhance(
                    gray,
                    1,
                    ImageFilter.MaximumUnsharpAmount + 0.1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageNormalizer.NormalizeIllumination(gray, 0, 1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ImageNormalizer.NormalizeIllumination(
                    gray,
                    1,
                    ImageNormalizer.MaximumContrastGain + 1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Morphology.Dilate(null, 1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Morphology.Erode(binary, 0),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Morphology.MajorityFilter(
                    binary,
                    Morphology.MaximumRadius + 1),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => Morphology.DilateGray(gray, 0),
                Throws.TypeOf<BarcodeValidationException>());
        });
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

    static ManagedGrayImage CreateFilledGrayImage(
        int width,
        int height,
        int value)
    {
        ManagedGrayImage image = new ManagedGrayImage(width, height);
        int index = 0;
        while (index < image.Samples.Length)
        {
            image.Samples[index] = value;
            index = index + 1;
        }

        return image;
    }

    static int CountForeground(ModuleMatrix source)
    {
        int count = 0;
        bool[] modules = source.CopyModules();
        int index = 0;
        while (index < modules.Length)
        {
            if (modules[index])
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
