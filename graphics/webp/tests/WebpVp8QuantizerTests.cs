using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8QuantizerTests
{
    [Test]
    public void QuantizeBlock_RoundsSignedCoefficientsAndDequantizes()
    {
        int[] coefficients = new int[16];
        coefficients[0] = 14;
        coefficients[1] = 15;
        coefficients[2] = -15;
        coefficients[3] = 4;
        coefficients[4] = -4;

        int[] quantized = WebpVp8Quantizer.QuantizeBlock(coefficients, 10);
        int[] dequantized = WebpVp8Quantizer.DequantizeBlock(quantized, 10);

        Assert.That(quantized[0], Is.EqualTo(1));
        Assert.That(quantized[1], Is.EqualTo(2));
        Assert.That(quantized[2], Is.EqualTo(-2));
        Assert.That(quantized[3], Is.EqualTo(0));
        Assert.That(quantized[4], Is.EqualTo(0));
        Assert.That(dequantized[0], Is.EqualTo(10));
        Assert.That(dequantized[1], Is.EqualTo(20));
        Assert.That(dequantized[2], Is.EqualTo(-20));
    }

    [Test]
    public void Quantizer_RejectsInvalidInputs()
    {
        int[] coefficients = new int[16];

        Assert.Throws<ArgumentNullException>(() => WebpVp8Quantizer.QuantizeBlock(null!, 10));
        Assert.Throws<ArgumentException>(() => WebpVp8Quantizer.QuantizeBlock(new int[15], 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8Quantizer.QuantizeBlock(coefficients, 0));
        Assert.Throws<ArgumentNullException>(() => WebpVp8Quantizer.DequantizeBlock(null!, 10));
        Assert.Throws<ArgumentException>(() => WebpVp8Quantizer.DequantizeBlock(new int[15], 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8Quantizer.DequantizeBlock(coefficients, -1));
    }
}
