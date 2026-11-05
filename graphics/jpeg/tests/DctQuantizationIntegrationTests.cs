namespace jpeg.Tests;

internal class DctQuantizationIntegrationTests
{
    [Test]
    public void DctQuantizeDequantizeAndIdct_RoundTripsConstantSampleBlockExactly()
    {
        global::jpeg.JpegIntegerForwardDct dct = new global::jpeg.JpegIntegerForwardDct();
        global::jpeg.JpegCoefficientQuantizer quantizer = new global::jpeg.JpegCoefficientQuantizer();
        global::jpeg.JpegAccurateIntegerInverseDct idct = new global::jpeg.JpegAccurateIntegerInverseDct();
        global::jpeg.JpegQuantizationTable table = CoefficientQuantizerTests.CreateTable(8);
        byte[] samples = CreateSampleBlock(150);

        int[] coefficients = dct.ForwardTransformSamples(samples);
        int[] quantized = quantizer.QuantizeBlock(coefficients, table);
        int[] dequantized = quantizer.DequantizeBlock(quantized, table);
        byte[] decoded = idct.InverseTransformBlockToSamples(dequantized);

        Assert.That(coefficients[0], Is.EqualTo(176));
        Assert.That(quantized[0], Is.EqualTo(22));
        Assert.That(dequantized[0], Is.EqualTo(176));
        int index = 0;
        while (index < decoded.Length)
        {
            Assert.That(decoded[index], Is.EqualTo(150));
            index = index + 1;
        }
    }

    [Test]
    public void QuantizedAcCoefficient_DequantizesIntoScaledInverseDct()
    {
        global::jpeg.JpegCoefficientQuantizer quantizer = new global::jpeg.JpegCoefficientQuantizer();
        global::jpeg.JpegScaledIntegerInverseDct scaledIdct = new global::jpeg.JpegScaledIntegerInverseDct();
        global::jpeg.JpegQuantizationTable table = CoefficientQuantizerTests.CreateTable(5);
        int[] coefficients = new int[global::jpeg.JpegConstants.BlockSampleCount];
        coefficients[1] = 100;

        int[] quantized = quantizer.QuantizeBlock(coefficients, table);
        int[] dequantized = quantizer.DequantizeBlock(quantized, table);
        int[] scaled = scaledIdct.InverseTransformBlock(dequantized, 1, 2);
        byte[] scaledSamples = scaledIdct.InverseTransformBlockToSamples(dequantized, 1, 2);

        Assert.That(quantized[1], Is.EqualTo(20));
        Assert.That(dequantized[1], Is.EqualTo(100));
        Assert.That(scaled, Is.EqualTo(new int[]
        {
            16, 7, -7, -16,
            16, 7, -7, -16,
            16, 7, -7, -16,
            16, 7, -7, -16
        }));
        Assert.That(scaledSamples[0], Is.EqualTo(144));
        Assert.That(scaledSamples[3], Is.EqualTo(112));
    }

    [Test]
    public void ForwardDctUnitQuantizationAndAccurateIdct_RoundTripsAcPatternWithinTolerance()
    {
        global::jpeg.JpegIntegerForwardDct dct = new global::jpeg.JpegIntegerForwardDct();
        global::jpeg.JpegCoefficientQuantizer quantizer = new global::jpeg.JpegCoefficientQuantizer();
        global::jpeg.JpegAccurateIntegerInverseDct idct = new global::jpeg.JpegAccurateIntegerInverseDct();
        global::jpeg.JpegQuantizationTable table = CoefficientQuantizerTests.CreateTable(1);
        int[] centered = IntegerForwardDctTests.CreateHorizontalAcPattern();

        int[] coefficients = dct.ForwardTransformBlock(centered);
        int[] quantized = quantizer.QuantizeBlock(coefficients, table);
        int[] dequantized = quantizer.DequantizeBlock(quantized, table);
        int[] roundTrip = idct.InverseTransformBlock(dequantized);

        Assert.That(quantized[1], Is.EqualTo(100));
        Assert.That(dequantized[1], Is.EqualTo(100));
        int index = 0;
        while (index < centered.Length)
        {
            Assert.That(System.Math.Abs(roundTrip[index] - centered[index]), Is.LessThanOrEqualTo(1));
            index = index + 1;
        }
    }

    public static byte[] CreateSampleBlock(int value)
    {
        byte[] samples = new byte[global::jpeg.JpegConstants.BlockSampleCount];
        int index = 0;
        while (index < samples.Length)
        {
            samples[index] = (byte)value;
            index = index + 1;
        }

        return samples;
    }
}
