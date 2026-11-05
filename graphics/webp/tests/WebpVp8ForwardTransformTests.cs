using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8ForwardTransformTests
{
    [Test]
    public void ForwardDct4X4_ZeroResidualMatchesReferenceFixedPointRounding()
    {
        byte[] source = NewFilled(16, 128);
        byte[] prediction = NewFilled(16, 128);

        int[] coefficients = WebpVp8ForwardTransform.ForwardDct4X4(source, 0, 4, prediction, 0, 4);

        Assert.That(coefficients[1], Is.EqualTo(1));
        int index = 0;
        while (index < coefficients.Length)
        {
            if (index != 1)
            {
                Assert.That(coefficients[index], Is.EqualTo(0));
            }

            index = index + 1;
        }
    }

    [Test]
    public void ForwardDct4X4_ConstantResidualProducesExpectedDcCoefficient()
    {
        byte[] source = NewFilled(16, 133);
        byte[] prediction = NewFilled(16, 128);

        int[] coefficients = WebpVp8ForwardTransform.ForwardDct4X4(source, 0, 4, prediction, 0, 4);

        Assert.That(coefficients[0], Is.EqualTo(40));
        int acSum = 0;
        int index = 1;
        while (index < coefficients.Length)
        {
            if (coefficients[index] < 0)
            {
                acSum = acSum - coefficients[index];
            }
            else
            {
                acSum = acSum + coefficients[index];
            }

            index = index + 1;
        }

        Assert.That(acSum, Is.LessThanOrEqualTo(4));
        int[] residual = WebpVp8InverseTransform.InverseDct4X4(coefficients);
        Assert.That(residual[0], Is.EqualTo(5));
    }

    [Test]
    public void ForwardWalsh4X4_ConstantDcBlockProducesDcOnlyCoefficient()
    {
        int[] dcCoefficients = NewFilledInt(16, 3);

        int[] coefficients = WebpVp8ForwardTransform.ForwardWalsh4X4(dcCoefficients);

        Assert.That(coefficients[0], Is.EqualTo(24));
        int index = 1;
        while (index < coefficients.Length)
        {
            Assert.That(coefficients[index], Is.EqualTo(0));
            index = index + 1;
        }

        int[] residual = WebpVp8InverseTransform.InverseWalsh4X4DcOnly(coefficients[0]);
        Assert.That(residual, Is.All.EqualTo(3));
    }

    [Test]
    public void ForwardTransform_RejectsInvalidInputs()
    {
        byte[] block = new byte[16];
        int[] coefficients = new int[16];

        Assert.Throws<ArgumentNullException>(() => WebpVp8ForwardTransform.ForwardDct4X4(null!, 0, 4, block, 0, 4));
        Assert.Throws<ArgumentNullException>(() => WebpVp8ForwardTransform.ForwardDct4X4(block, 0, 4, null!, 0, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8ForwardTransform.ForwardDct4X4(block, -1, 4, block, 0, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8ForwardTransform.ForwardDct4X4(block, 0, 3, block, 0, 4));
        Assert.Throws<ArgumentException>(() => WebpVp8ForwardTransform.ForwardDct4X4(new byte[4], 0, 4, block, 0, 4));
        Assert.Throws<ArgumentNullException>(() => WebpVp8ForwardTransform.ForwardWalsh4X4(null!));
        Assert.Throws<ArgumentException>(() => WebpVp8ForwardTransform.ForwardWalsh4X4(new int[15]));

        coefficients[0] = 1;
        Assert.That(WebpVp8ForwardTransform.ForwardWalsh4X4(coefficients).Length, Is.EqualTo(16));
    }

    static byte[] NewFilled(int length, byte value)
    {
        byte[] data = new byte[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = value;
            index = index + 1;
        }

        return data;
    }

    static int[] NewFilledInt(int length, int value)
    {
        int[] data = new int[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = value;
            index = index + 1;
        }

        return data;
    }
}
