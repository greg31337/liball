namespace jpeg.Tests;

internal class AccurateIntegerInverseDctTests
{
    [Test]
    public void InverseDct_TransformsDcOnlyBlockToConstantValues()
    {
        global::jpeg.JpegAccurateIntegerInverseDct idct = new global::jpeg.JpegAccurateIntegerInverseDct();
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
    public void InverseDct_TransformsHorizontalAcCoefficient()
    {
        global::jpeg.JpegAccurateIntegerInverseDct idct = new global::jpeg.JpegAccurateIntegerInverseDct();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[1] = 100;

        int[] transformed = idct.InverseTransformBlock(coefficients);

        Assert.That(transformed[0], Is.EqualTo(17));
        Assert.That(transformed[1], Is.EqualTo(15));
        Assert.That(transformed[2], Is.EqualTo(10));
        Assert.That(transformed[3], Is.EqualTo(3));
        Assert.That(transformed[4], Is.EqualTo(-3));
        Assert.That(transformed[5], Is.EqualTo(-10));
        Assert.That(transformed[6], Is.EqualTo(-15));
        Assert.That(transformed[7], Is.EqualTo(-17));
        Assert.That(transformed[8], Is.EqualTo(17));
        Assert.That(transformed[15], Is.EqualTo(-17));
    }

    [Test]
    public void InverseDct_TransformsVerticalAcCoefficient()
    {
        global::jpeg.JpegAccurateIntegerInverseDct idct = new global::jpeg.JpegAccurateIntegerInverseDct();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[8] = 100;

        int[] transformed = idct.InverseTransformBlock(coefficients);

        Assert.That(transformed[0], Is.EqualTo(17));
        Assert.That(transformed[7], Is.EqualTo(17));
        Assert.That(transformed[8], Is.EqualTo(15));
        Assert.That(transformed[16], Is.EqualTo(10));
        Assert.That(transformed[24], Is.EqualTo(3));
        Assert.That(transformed[32], Is.EqualTo(-3));
        Assert.That(transformed[40], Is.EqualTo(-10));
        Assert.That(transformed[48], Is.EqualTo(-15));
        Assert.That(transformed[56], Is.EqualTo(-17));
        Assert.That(transformed[63], Is.EqualTo(-17));
    }

    [Test]
    public void InverseDct_TransformsTwoDimensionalAcCoefficient()
    {
        global::jpeg.JpegAccurateIntegerInverseDct idct = new global::jpeg.JpegAccurateIntegerInverseDct();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[9] = 100;

        int[] transformed = idct.InverseTransformBlock(coefficients);

        Assert.That(transformed[0], Is.EqualTo(24));
        Assert.That(transformed[1], Is.EqualTo(20));
        Assert.That(transformed[9], Is.EqualTo(17));
        Assert.That(transformed[18], Is.EqualTo(8));
        Assert.That(transformed[27], Is.EqualTo(1));
        Assert.That(transformed[36], Is.EqualTo(1));
        Assert.That(transformed[45], Is.EqualTo(8));
        Assert.That(transformed[54], Is.EqualTo(17));
        Assert.That(transformed[63], Is.EqualTo(24));
    }

    [Test]
    public void InverseDct_ConvertsCenteredBlockToClampedSamples()
    {
        global::jpeg.JpegAccurateIntegerInverseDct idct = new global::jpeg.JpegAccurateIntegerInverseDct();
        int[] high = new int[global::jpeg.JpegConstants.BlockSampleCount];
        int[] low = new int[global::jpeg.JpegConstants.BlockSampleCount];
        high[0] = 1024;
        low[0] = -2048;

        byte[] highSamples = idct.InverseTransformBlockToSamples(high);
        byte[] lowSamples = idct.InverseTransformBlockToSamples(low);

        Assert.That(highSamples[0], Is.EqualTo(255));
        Assert.That(highSamples[63], Is.EqualTo(255));
        Assert.That(lowSamples[0], Is.EqualTo(0));
        Assert.That(lowSamples[63], Is.EqualTo(0));
        Assert.That(idct.ToSampleByte(0), Is.EqualTo(128));
        Assert.That(idct.ToSampleByte(127), Is.EqualTo(255));
        Assert.That(idct.ToSampleByte(-128), Is.EqualTo(0));
        Assert.That(idct.ToSampleByte(int.MaxValue), Is.EqualTo(255));
        Assert.That(idct.ToSampleByte(int.MinValue), Is.EqualTo(0));
    }

    [Test]
    public void InverseDct_CopyMethodsWriteOnlyFirstBlock()
    {
        global::jpeg.JpegAccurateIntegerInverseDct idct = new global::jpeg.JpegAccurateIntegerInverseDct();
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
    public void InverseDct_ExposesBasisValuesAndRounding()
    {
        global::jpeg.JpegAccurateIntegerInverseDct idct = new global::jpeg.JpegAccurateIntegerInverseDct();

        Assert.That(idct.GetBasisValue(0, 0), Is.EqualTo(2896));
        Assert.That(idct.GetBasisValue(0, 1), Is.EqualTo(4017));
        Assert.That(idct.GetBasisValue(7, 7), Is.EqualTo(-799));
        Assert.That(idct.RoundScaledLong(3, 1), Is.EqualTo(2));
        Assert.That(idct.RoundScaledLong(-3, 1), Is.EqualTo(-2));
        Assert.That(idct.RoundScaledLong(long.MaxValue, 1), Is.EqualTo(int.MaxValue));
        Assert.That(idct.RoundScaledLong(long.MinValue, 1), Is.EqualTo(int.MinValue));
    }

    [Test]
    public void InverseDct_RejectsInvalidInputs()
    {
        global::jpeg.JpegAccurateIntegerInverseDct idct = new global::jpeg.JpegAccurateIntegerInverseDct();
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        byte[] samples = new byte[global::jpeg.JpegConstants.BlockSampleCount];

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
                idct.CopyInverseTransformBlockToSamples(coefficients, null!);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                idct.CopyInverseTransformBlockToSamples(coefficients, new byte[1]);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                idct.TransformCoefficientAt(coefficients, -1, 0);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                idct.GetBasisValue(0, 8);
            });

        idct.CopyInverseTransformBlockToSamples(coefficients, samples);
        Assert.That(samples[0], Is.EqualTo(128));
    }
}
