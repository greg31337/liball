namespace jpeg.Tests;

internal class FastIntegerInverseDctTests
{
    [Test]
    public void FastInverseDct_TransformsDcOnlyBlockToConstantValues()
    {
        global::jpeg.JpegFastIntegerInverseDct idct = new global::jpeg.JpegFastIntegerInverseDct();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = 80;

        int[] transformed = idct.InverseTransformBlock(coefficients);

        int index = 0;
        while (index < global::jpeg.JpegConstants.BlockSampleCount)
        {
            Assert.That(transformed[index], Is.EqualTo(10));
            index = index + 1;
        }
    }

    [Test]
    public void FastInverseDct_StaysCloseToAccurateTransformForMixedBlock()
    {
        global::jpeg.JpegAccurateIntegerInverseDct accurate = new global::jpeg.JpegAccurateIntegerInverseDct();
        global::jpeg.JpegFastIntegerInverseDct fast = new global::jpeg.JpegFastIntegerInverseDct();
        int[] coefficients = CreateMixedBlock();

        int[] accurateSamples = accurate.InverseTransformBlock(coefficients);
        int[] fastSamples = fast.InverseTransformBlock(coefficients);

        int index = 0;
        while (index < global::jpeg.JpegConstants.BlockSampleCount)
        {
            Assert.That(System.Math.Abs(fastSamples[index] - accurateSamples[index]), Is.LessThanOrEqualTo(2));
            index = index + 1;
        }
    }

    [Test]
    public void FastInverseDct_UsesTwoPassRounding()
    {
        global::jpeg.JpegFastIntegerInverseDct idct = new global::jpeg.JpegFastIntegerInverseDct();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[1] = 100;

        int[] transformed = idct.InverseTransformBlock(coefficients);

        Assert.That(transformed[0], Is.EqualTo(17));
        Assert.That(transformed[3], Is.EqualTo(4));
        Assert.That(transformed[4], Is.EqualTo(-4));
        Assert.That(transformed[7], Is.EqualTo(-17));
        Assert.That(idct.RoundScaledLongToLong(long.MaxValue, 1), Is.EqualTo(long.MaxValue / 2 + 1));
        Assert.That(idct.RoundScaledLongToLong(long.MinValue, 1), Is.EqualTo(long.MinValue / 2));
    }

    [Test]
    public void FastInverseDct_ConvertsToSamplesAndCopiesOnlyFirstBlock()
    {
        global::jpeg.JpegFastIntegerInverseDct idct = new global::jpeg.JpegFastIntegerInverseDct();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        int[] transformed = new int[70];
        byte[] samples = new byte[70];
        coefficients[0] = 80;
        transformed[64] = 777;
        samples[64] = 99;

        idct.CopyInverseTransformBlock(coefficients, transformed);
        idct.CopyInverseTransformBlockToSamples(coefficients, samples);

        Assert.That(transformed[0], Is.EqualTo(10));
        Assert.That(transformed[63], Is.EqualTo(10));
        Assert.That(transformed[64], Is.EqualTo(777));
        Assert.That(samples[0], Is.EqualTo(138));
        Assert.That(samples[63], Is.EqualTo(138));
        Assert.That(samples[64], Is.EqualTo(99));
    }

    [Test]
    public void InverseDctTransformer_SelectsMethodFromReadOptions()
    {
        global::jpeg.JpegInverseDctTransformer transformer = new global::jpeg.JpegInverseDctTransformer();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[1] = 100;

        int[] accurate = transformer.InverseTransformBlock(coefficients, options);
        options.InverseDctMethod = global::jpeg.JpegInverseDctMethod.Fast;
        int[] fast = transformer.InverseTransformBlock(coefficients, options);

        Assert.That(accurate[3], Is.EqualTo(3));
        Assert.That(fast[3], Is.EqualTo(4));
        Assert.That(transformer.ResolveMethod(options), Is.EqualTo(global::jpeg.JpegInverseDctMethod.Fast));
    }

    [Test]
    public void InverseDctTransformer_WritesSamplesForSelectedMethod()
    {
        global::jpeg.JpegInverseDctTransformer transformer = new global::jpeg.JpegInverseDctTransformer();
        global::jpeg.JpegReadOptions options = new global::jpeg.JpegReadOptions();
        options.InverseDctMethod = global::jpeg.JpegInverseDctMethod.Fast;
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = 80;

        byte[] samples = transformer.InverseTransformBlockToSamples(coefficients, options);

        Assert.That(samples[0], Is.EqualTo(138));
        Assert.That(samples[63], Is.EqualTo(138));
    }

    [Test]
    public void FastInverseDct_RejectsInvalidInputs()
    {
        global::jpeg.JpegFastIntegerInverseDct idct = new global::jpeg.JpegFastIntegerInverseDct();
        global::jpeg.JpegInverseDctTransformer transformer = new global::jpeg.JpegInverseDctTransformer();
        global::jpeg.JpegReadOptions badOptions = new global::jpeg.JpegReadOptions();
        badOptions.InverseDctMethod = (global::jpeg.JpegInverseDctMethod)99;

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                idct.InverseTransformBlock(null!);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                idct.InverseTransformBlock(new int[1]);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                idct.TransformWorkspaceAt(null!, 0, 0);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                idct.TransformWorkspaceAt(new long[1], 0, 0);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                transformer.InverseTransformBlock(new int[global::jpeg.JpegConstants.BlockSampleCount], null!);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                transformer.ResolveMethod(badOptions);
            });
    }

    public static int[] CreateMixedBlock()
    {
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[0] = 120;
        coefficients[1] = 36;
        coefficients[2] = -18;
        coefficients[8] = 28;
        coefficients[9] = 14;
        coefficients[18] = -9;
        coefficients[27] = 5;
        coefficients[40] = -3;
        return coefficients;
    }
}
