namespace jpeg.Tests;

internal class ScaledIntegerInverseDctTests
{
    [Test]
    public void ScaledInverseDct_OneEighthAveragesFullBlock()
    {
        global::jpeg.JpegScaledIntegerInverseDct idct = new global::jpeg.JpegScaledIntegerInverseDct();
        int[] dc = new int[global::jpeg.JpegConstants.BlockSampleCount];
        int[] ac = new int[global::jpeg.JpegConstants.BlockSampleCount];
        dc[0] = 80;
        ac[1] = 100;

        int[] dcScaled = idct.InverseTransformBlock(dc, 1, 8);
        int[] acScaled = idct.InverseTransformBlock(ac, 1, 8);
        byte[] acSamples = idct.InverseTransformBlockToSamples(ac, 1, 8);

        Assert.That(idct.GetScaledBlockSize(1, 8), Is.EqualTo(1));
        Assert.That(idct.GetOutputSampleCount(1, 8), Is.EqualTo(1));
        Assert.That(dcScaled.Length, Is.EqualTo(1));
        Assert.That(dcScaled[0], Is.EqualTo(10));
        Assert.That(acScaled[0], Is.EqualTo(0));
        Assert.That(acSamples[0], Is.EqualTo(128));
    }

    [Test]
    public void ScaledInverseDct_OneHalfAveragesTwoByTwoRegions()
    {
        global::jpeg.JpegScaledIntegerInverseDct idct = new global::jpeg.JpegScaledIntegerInverseDct();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[1] = 100;

        int[] scaled = idct.InverseTransformBlock(coefficients, 1, 2);

        Assert.That(idct.GetScaledBlockSize(1, 2), Is.EqualTo(4));
        Assert.That(scaled.Length, Is.EqualTo(16));
        Assert.That(scaled[0], Is.EqualTo(16));
        Assert.That(scaled[1], Is.EqualTo(7));
        Assert.That(scaled[2], Is.EqualTo(-7));
        Assert.That(scaled[3], Is.EqualTo(-16));
        Assert.That(scaled[12], Is.EqualTo(16));
        Assert.That(scaled[15], Is.EqualTo(-16));
    }

    [Test]
    public void ScaledInverseDct_OneQuarterAveragesFourByFourRegions()
    {
        global::jpeg.JpegScaledIntegerInverseDct idct = new global::jpeg.JpegScaledIntegerInverseDct();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[1] = 100;

        int[] scaled = idct.InverseTransformBlock(coefficients, 1, 4);
        byte[] samples = idct.InverseTransformBlockToSamples(coefficients, 1, 4);

        Assert.That(idct.GetScaledBlockSize(1, 4), Is.EqualTo(2));
        Assert.That(scaled.Length, Is.EqualTo(4));
        Assert.That(scaled[0], Is.EqualTo(11));
        Assert.That(scaled[1], Is.EqualTo(-11));
        Assert.That(scaled[2], Is.EqualTo(11));
        Assert.That(scaled[3], Is.EqualTo(-11));
        Assert.That(samples[0], Is.EqualTo(139));
        Assert.That(samples[1], Is.EqualTo(117));
    }

    [Test]
    public void ScaledInverseDct_SupportsAdditionalReducedLibjpegRatios()
    {
        global::jpeg.JpegScaledIntegerInverseDct idct = new global::jpeg.JpegScaledIntegerInverseDct();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[1] = 100;

        int[] threeEighths = idct.InverseTransformBlock(coefficients, 3, 8);
        int[] fiveEighths = idct.InverseTransformBlock(coefficients, 5, 8);
        int[] threeQuarters = idct.InverseTransformBlock(coefficients, 3, 4);
        int[] sevenEighths = idct.InverseTransformBlock(coefficients, 7, 8);

        Assert.That(idct.GetScaledBlockSize(3, 8), Is.EqualTo(3));
        Assert.That(idct.GetScaledBlockSize(5, 8), Is.EqualTo(5));
        Assert.That(idct.GetScaledBlockSize(3, 4), Is.EqualTo(6));
        Assert.That(idct.GetScaledBlockSize(7, 8), Is.EqualTo(7));
        Assert.That(threeEighths.Length, Is.EqualTo(9));
        Assert.That(fiveEighths.Length, Is.EqualTo(25));
        Assert.That(threeQuarters.Length, Is.EqualTo(36));
        Assert.That(sevenEighths.Length, Is.EqualTo(49));
        Assert.That(threeEighths[0], Is.EqualTo(16));
        Assert.That(threeEighths[1], Is.EqualTo(3));
        Assert.That(threeEighths[2], Is.EqualTo(-14));
        Assert.That(fiveEighths[0], Is.EqualTo(17));
        Assert.That(fiveEighths[1], Is.EqualTo(13));
        Assert.That(fiveEighths[4], Is.EqualTo(-16));
        Assert.That(threeQuarters[0], Is.EqualTo(17));
        Assert.That(threeQuarters[2], Is.EqualTo(7));
        Assert.That(threeQuarters[5], Is.EqualTo(-16));
        Assert.That(sevenEighths[0], Is.EqualTo(17));
        Assert.That(sevenEighths[3], Is.EqualTo(3));
        Assert.That(sevenEighths[6], Is.EqualTo(-16));
    }

    [Test]
    public void ScaledInverseDct_NormalizedRatioMatchesEquivalentEighthRatio()
    {
        global::jpeg.JpegScaledIntegerInverseDct idct = new global::jpeg.JpegScaledIntegerInverseDct();
        int[] coefficients = IntegerForwardDctTests.CreateMixedCoefficientBlock();

        int[] oneQuarter = idct.InverseTransformBlock(coefficients, 1, 4);
        int[] twoEighths = idct.InverseTransformBlock(coefficients, 2, 8);
        int[] oneHalf = idct.InverseTransformBlock(coefficients, 1, 2);
        int[] fourEighths = idct.InverseTransformBlock(coefficients, 4, 8);

        Assert.That(twoEighths, Is.EqualTo(oneQuarter));
        Assert.That(fourEighths, Is.EqualTo(oneHalf));
    }

    [Test]
    public void ScaledInverseDct_MapsUnevenOutputRegionsAcrossFullBlock()
    {
        global::jpeg.JpegScaledIntegerInverseDct idct = new global::jpeg.JpegScaledIntegerInverseDct();
        int[] fullBlock = new int[global::jpeg.JpegConstants.BlockSampleCount];
        int index = 0;
        while (index < global::jpeg.JpegConstants.BlockSampleCount)
        {
            fullBlock[index] = index;
            index = index + 1;
        }

        Assert.That(idct.GetRegionStart(0, 3), Is.EqualTo(0));
        Assert.That(idct.GetRegionEnd(0, 3), Is.EqualTo(2));
        Assert.That(idct.GetRegionStart(1, 3), Is.EqualTo(2));
        Assert.That(idct.GetRegionEnd(1, 3), Is.EqualTo(5));
        Assert.That(idct.GetRegionStart(2, 3), Is.EqualTo(5));
        Assert.That(idct.GetRegionEnd(2, 3), Is.EqualTo(8));
        Assert.That(idct.AverageOutputRegion(fullBlock, 0, 0, 3), Is.EqualTo(5));
        Assert.That(idct.AverageOutputRegion(fullBlock, 1, 0, 3), Is.EqualTo(7));
        Assert.That(idct.AverageRectangle(fullBlock, 2, 0, 3, 2), Is.EqualTo(7));
    }

    [Test]
    public void ScaledInverseDct_FullScaleMatchesAccurateBlock()
    {
        global::jpeg.JpegAccurateIntegerInverseDct accurate = new global::jpeg.JpegAccurateIntegerInverseDct();
        global::jpeg.JpegScaledIntegerInverseDct scaled = new global::jpeg.JpegScaledIntegerInverseDct();
        int[] coefficients = IntegerForwardDctTests.CreateMixedCoefficientBlock();

        int[] full = accurate.InverseTransformBlock(coefficients);
        int[] scaledFull = scaled.InverseTransformBlock(coefficients, 1, 1);

        Assert.That(scaledFull.Length, Is.EqualTo(64));
        Assert.That(scaledFull, Is.EqualTo(full));
    }

    [Test]
    public void ScaledInverseDct_CopyMethodsWriteOnlyScaledOutput()
    {
        global::jpeg.JpegScaledIntegerInverseDct idct = new global::jpeg.JpegScaledIntegerInverseDct();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        int[] scaled = new int[20];
        byte[] samples = new byte[20];
        coefficients[0] = 80;
        scaled[16] = 777;
        samples[16] = 99;

        idct.CopyInverseTransformBlock(coefficients, scaled, 1, 2);
        idct.CopyInverseTransformBlockToSamples(coefficients, samples, 1, 2);

        Assert.That(scaled[0], Is.EqualTo(10));
        Assert.That(scaled[15], Is.EqualTo(10));
        Assert.That(scaled[16], Is.EqualTo(777));
        Assert.That(samples[0], Is.EqualTo(138));
        Assert.That(samples[15], Is.EqualTo(138));
        Assert.That(samples[16], Is.EqualTo(99));
    }

    [Test]
    public void InverseDctTransformer_UsesReadOptionScale()
    {
        global::jpeg.JpegInverseDctTransformer transformer = new global::jpeg.JpegInverseDctTransformer();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = 80;
        options.IdctScaleDenominator = 4;

        int[] scaled = transformer.InverseTransformScaledBlock(coefficients, options);
        byte[] samples = transformer.InverseTransformBlockToScaledSamples(coefficients, options);

        Assert.That(scaled.Length, Is.EqualTo(4));
        Assert.That(samples.Length, Is.EqualTo(4));
        Assert.That(scaled[0], Is.EqualTo(10));
        Assert.That(samples[0], Is.EqualTo(138));
    }

    [Test]
    public void ScaledInverseDct_RejectsInvalidInputs()
    {
        global::jpeg.JpegScaledIntegerInverseDct idct = new global::jpeg.JpegScaledIntegerInverseDct();
        global::jpeg.JpegInverseDctTransformer transformer = new global::jpeg.JpegInverseDctTransformer();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                idct.InverseTransformBlock(null!, 1, 2);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                idct.InverseTransformBlock(coefficients, 9, 8);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                idct.InverseTransformBlock(coefficients, 1, 3);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                idct.CopyInverseTransformBlock(coefficients, new int[3], 1, 4);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                idct.AverageRegion(coefficients, 7, 7, 2);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                transformer.InverseTransformScaledBlock(coefficients, null!);
            });

        options.IdctScaleDenominator = 3;
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                transformer.InverseTransformScaledBlock(coefficients, options);
            });
    }

    [Test]
    public void ScaledInverseDct_HandlesExtremeArithmeticInputs()
    {
        global::jpeg.JpegScaledIntegerInverseDct idct = new global::jpeg.JpegScaledIntegerInverseDct();

        Assert.That(idct.GetScaledBlockSize(int.MaxValue, int.MaxValue), Is.EqualTo(8));
        Assert.That(idct.RoundAverage(long.MaxValue, 1), Is.EqualTo(int.MaxValue));
        Assert.That(idct.RoundAverage(long.MinValue, 1), Is.EqualTo(int.MinValue));
    }
}
