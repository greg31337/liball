using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8InverseTransformTests
{
    [Test]
    public void InverseDct4X4_DcOnlyProducesUniformResidual()
    {
        int[] coefficients = new int[16];
        coefficients[0] = 16;

        int[] residual = WebpVp8InverseTransform.InverseDct4X4(coefficients);

        Assert.That(residual, Is.EqualTo(NewRepeatedBlock(2)));
        Assert.That(WebpVp8InverseTransform.InverseDct4X4DcOnly(16), Is.EqualTo(residual));
    }

    [Test]
    public void InverseDct4X4_MatchesReferenceFixedPointRoundingForVerticalCoefficient()
    {
        int[] coefficients = new int[16];
        coefficients[4] = 16;

        int[] residual = WebpVp8InverseTransform.InverseDct4X4(coefficients);

        Assert.That(residual, Is.EqualTo(new int[]
        {
            3, 3, 3, 3,
            1, 1, 1, 1,
            -1, -1, -1, -1,
            -2, -2, -2, -2
        }));
    }

    [Test]
    public void InverseWalsh4X4_DcOnlyProducesUniformY2Residual()
    {
        int[] coefficients = new int[16];
        coefficients[0] = 32;

        int[] residual = WebpVp8InverseTransform.InverseWalsh4X4(coefficients);

        Assert.That(residual, Is.EqualTo(NewRepeatedBlock(4)));
        Assert.That(WebpVp8InverseTransform.InverseWalsh4X4DcOnly(32), Is.EqualTo(residual));
    }

    [Test]
    public void InverseWalsh4X4_MatchesReferenceRoundingForColumnCoefficient()
    {
        int[] coefficients = new int[16];
        coefficients[4] = 16;

        int[] residual = WebpVp8InverseTransform.InverseWalsh4X4(coefficients);

        Assert.That(residual, Is.EqualTo(new int[]
        {
            2, 2, 2, 2,
            2, 2, 2, 2,
            -2, -2, -2, -2,
            -2, -2, -2, -2
        }));
    }

    [Test]
    public void AddResidualToBlock_AddsPredictionAndClampsToByteRange()
    {
        byte[] prediction = new byte[36];
        byte[] destination = new byte[36];
        int index = 0;
        while (index < prediction.Length)
        {
            prediction[index] = 100;
            index = index + 1;
        }

        int[] residual = new int[]
        {
            -150, 0, 20, 200,
            -1, 1, 2, 3,
            4, 5, 6, 7,
            8, 9, 10, 11
        };

        WebpVp8InverseTransform.AddResidualToBlock(destination, 7, 6, prediction, 7, 6, residual);

        Assert.That(destination[7], Is.EqualTo(0));
        Assert.That(destination[8], Is.EqualTo(100));
        Assert.That(destination[9], Is.EqualTo(120));
        Assert.That(destination[10], Is.EqualTo(255));
        Assert.That(destination[25], Is.EqualTo(108));
        Assert.That(destination[28], Is.EqualTo(111));
    }

    [Test]
    public void Transform_RejectsInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => WebpVp8InverseTransform.InverseDct4X4(null!));
        Assert.Throws<ArgumentException>(() => WebpVp8InverseTransform.InverseDct4X4(new int[15]));
        Assert.Throws<ArgumentNullException>(() => WebpVp8InverseTransform.InverseWalsh4X4(null!));
        Assert.Throws<ArgumentException>(() => WebpVp8InverseTransform.InverseWalsh4X4(new int[17]));
        Assert.Throws<ArgumentNullException>(() => WebpVp8InverseTransform.AddResidualToBlock(null!, 0, 4, new byte[16], 0, 4, new int[16]));
        Assert.Throws<ArgumentException>(() => WebpVp8InverseTransform.AddResidualToBlock(new byte[16], 0, 4, new byte[16], 0, 4, new int[15]));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8InverseTransform.AddResidualToBlock(new byte[16], -1, 4, new byte[16], 0, 4, new int[16]));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8InverseTransform.AddResidualToBlock(new byte[16], 0, 3, new byte[16], 0, 4, new int[16]));
        Assert.Throws<ArgumentException>(() => WebpVp8InverseTransform.AddResidualToBlock(new byte[15], 0, 4, new byte[16], 0, 4, new int[16]));
    }

    static int[] NewRepeatedBlock(int value)
    {
        int[] block = new int[16];
        int index = 0;
        while (index < block.Length)
        {
            block[index] = value;
            index = index + 1;
        }

        return block;
    }
}
