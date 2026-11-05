namespace jpeg.Tests;

internal class IntegerForwardDctTests
{
    [Test]
    public void ForwardDct_TransformsConstantCenteredBlockToDcCoefficient()
    {
        global::jpeg.JpegIntegerForwardDct dct = new global::jpeg.JpegIntegerForwardDct();
        int[] samples = CreateCenteredBlock(10);

        int[] coefficients = dct.ForwardTransformBlock(samples);

        Assert.That(coefficients[0], Is.EqualTo(80));
        AssertAllZeroExcept(coefficients, 0, 80);
    }

    [Test]
    public void ForwardDct_TransformsCenteredHorizontalAcPattern()
    {
        global::jpeg.JpegIntegerForwardDct dct = new global::jpeg.JpegIntegerForwardDct();
        int[] samples = CreateHorizontalAcPattern();

        int[] coefficients = dct.ForwardTransformBlock(samples);

        Assert.That(coefficients[0], Is.EqualTo(0));
        Assert.That(coefficients[1], Is.EqualTo(100));
        Assert.That(coefficients[3], Is.EqualTo(-1));
        Assert.That(coefficients[5], Is.EqualTo(-2));
        Assert.That(coefficients[7], Is.EqualTo(1));
        Assert.That(coefficients[8], Is.EqualTo(0));
        Assert.That(coefficients[63], Is.EqualTo(0));
    }

    [Test]
    public void ForwardDct_TransformsByteSamplesWithLevelShift()
    {
        global::jpeg.JpegIntegerForwardDct dct = new global::jpeg.JpegIntegerForwardDct();
        byte[] samples = new byte[global::jpeg.JpegConstants.BlockSampleCount];
        int index = 0;
        while (index < global::jpeg.JpegConstants.BlockSampleCount)
        {
            samples[index] = 138;
            index = index + 1;
        }

        int[] coefficients = dct.ForwardTransformSamples(samples);

        Assert.That(dct.CenterSampleByte(128), Is.EqualTo(0));
        Assert.That(dct.CenterSampleByte(138), Is.EqualTo(10));
        Assert.That(coefficients[0], Is.EqualTo(80));
        AssertAllZeroExcept(coefficients, 0, 80);
    }

    [Test]
    public void ForwardDct_RoundTripsAccurateIdctWithinIntegerTolerance()
    {
        global::jpeg.JpegAccurateIntegerInverseDct idct = new global::jpeg.JpegAccurateIntegerInverseDct();
        global::jpeg.JpegIntegerForwardDct dct = new global::jpeg.JpegIntegerForwardDct();
        int[] coefficients = CreateMixedCoefficientBlock();

        int[] samples = idct.InverseTransformBlock(coefficients);
        int[] roundTrip = dct.ForwardTransformBlock(samples);

        AssertClose(roundTrip[0], 120, 0);
        AssertClose(roundTrip[1], 36, 0);
        AssertClose(roundTrip[2], -18, 0);
        AssertClose(roundTrip[8], 28, 0);
        AssertClose(roundTrip[9], 14, 0);
        AssertClose(roundTrip[18], -9, 0);
        AssertClose(roundTrip[27], 5, 0);
        AssertClose(roundTrip[40], -3, 0);
        Assert.That(System.Math.Abs(roundTrip[17]), Is.LessThanOrEqualTo(1));
        Assert.That(System.Math.Abs(roundTrip[39]), Is.LessThanOrEqualTo(1));
        Assert.That(System.Math.Abs(roundTrip[55]), Is.LessThanOrEqualTo(1));
    }

    [Test]
    public void ForwardDct_CopyMethodsWriteOnlyFirstBlock()
    {
        global::jpeg.JpegIntegerForwardDct dct = new global::jpeg.JpegIntegerForwardDct();
        int[] samples = CreateCenteredBlock(10);
        byte[] byteSamples = new byte[global::jpeg.JpegConstants.BlockSampleCount];
        int[] coefficients = new int[70];
        int[] byteCoefficients = new int[70];
        int index = 0;
        while (index < global::jpeg.JpegConstants.BlockSampleCount)
        {
            byteSamples[index] = 138;
            index = index + 1;
        }

        coefficients[64] = 777;
        byteCoefficients[64] = 888;

        dct.CopyForwardTransformBlock(samples, coefficients);
        dct.CopyForwardTransformSamples(byteSamples, byteCoefficients);

        Assert.That(coefficients[0], Is.EqualTo(80));
        Assert.That(coefficients[64], Is.EqualTo(777));
        Assert.That(byteCoefficients[0], Is.EqualTo(80));
        Assert.That(byteCoefficients[64], Is.EqualTo(888));
    }

    [Test]
    public void ForwardDct_RejectsInvalidInputs()
    {
        global::jpeg.JpegIntegerForwardDct dct = new global::jpeg.JpegIntegerForwardDct();
        int[] samples = new int[global::jpeg.JpegConstants.BlockSampleCount];
        byte[] byteSamples = new byte[global::jpeg.JpegConstants.BlockSampleCount];

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                dct.ForwardTransformBlock(null!);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                dct.ForwardTransformBlock(new int[1]);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                dct.ForwardTransformSamples(null!);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                dct.CopyForwardTransformSamples(new byte[1], samples);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                dct.CopyForwardTransformSamples(byteSamples, null!);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                dct.TransformCoefficientAt(samples, 8, 0);
            });
    }

    public static int[] CreateCenteredBlock(int value)
    {
        int[] samples = new int[global::jpeg.JpegConstants.BlockSampleCount];
        int index = 0;
        while (index < global::jpeg.JpegConstants.BlockSampleCount)
        {
            samples[index] = value;
            index = index + 1;
        }

        return samples;
    }

    public static int[] CreateHorizontalAcPattern()
    {
        int[] row = new int[] { 17, 15, 10, 3, -3, -10, -15, -17 };
        int[] samples = new int[global::jpeg.JpegConstants.BlockSampleCount];
        int y = 0;
        while (y < global::jpeg.JpegConstants.BlockHeight)
        {
            int x = 0;
            while (x < global::jpeg.JpegConstants.BlockWidth)
            {
                samples[y * global::jpeg.JpegConstants.BlockWidth + x] = row[x];
                x = x + 1;
            }

            y = y + 1;
        }

        return samples;
    }

    public static int[] CreateMixedCoefficientBlock()
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

    public static void AssertAllZeroExcept(int[] coefficients, int exceptIndex, int expectedExceptValue)
    {
        int index = 0;
        while (index < global::jpeg.JpegConstants.BlockSampleCount)
        {
            if (index == exceptIndex)
            {
                Assert.That(coefficients[index], Is.EqualTo(expectedExceptValue));
            }
            else
            {
                Assert.That(coefficients[index], Is.EqualTo(0));
            }

            index = index + 1;
        }
    }

    public static void AssertClose(int actual, int expected, int tolerance)
    {
        Assert.That(System.Math.Abs(actual - expected), Is.LessThanOrEqualTo(tolerance));
    }
}
