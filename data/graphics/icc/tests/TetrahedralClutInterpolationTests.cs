using icc;

namespace icc.Tests;

public class TetrahedralClutInterpolationTests
{
    [TestCase(0.8, 0.5, 0.2, 25.4)]
    [TestCase(0.8, 0.2, 0.5, 22.1)]
    [TestCase(0.5, 0.2, 0.8, 17.6)]
    [TestCase(0.5, 0.8, 0.2, 21.8)]
    [TestCase(0.2, 0.8, 0.5, 13.7)]
    [TestCase(0.2, 0.5, 0.8, 12.8)]
    public void TetrahedralInterpolationHandlesAllSixFractionOrderings(double first, double second, double third, double expected)
    {
        IccClut clut = CreateThreeDimensionalClut(2);
        double[] outputs = new double[2];

        ClutInterpolator.InterpolateTetrahedral(clut, new double[] { first, second, third }, outputs, new int[6], new double[3]);

        Assert.Multiple(() =>
        {
            Assert.That(outputs[0], Is.EqualTo(expected).Within(1e-14));
            Assert.That(outputs[1], Is.EqualTo(100.0 + (2.0 * expected)).Within(1e-13));
        });
    }

    [Test]
    public void ExplicitSelectionDistinguishesTetrahedralFromMultilinearInterpolation()
    {
        IccClut clut = new IccClut
        {
            InputChannels = 3,
            OutputChannels = 1,
            GridPoints = new int[] { 2, 2, 2 },
            Values = new double[] { 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 0.0 },
            Interpolation = IccInterpolationKind.Tetrahedral
        };
        double[] inputs = new double[] { 0.8, 0.5, 0.2 };
        double[] output = new double[1];
        int[] indexes = new int[6];
        double[] weights = new double[3];

        ClutInterpolator.InterpolateClut(clut, inputs, output, indexes, weights, IccInterpolationKind.Multilinear);
        double multilinear = output[0];
        clut.Interpolation = IccInterpolationKind.Multilinear;
        ClutInterpolator.InterpolateClut(clut, inputs, output, indexes, weights, IccInterpolationKind.Tetrahedral);

        Assert.Multiple(() =>
        {
            Assert.That(multilinear, Is.EqualTo(0.08).Within(1e-14));
            Assert.That(output[0], Is.Zero);
        });
    }

    [Test]
    public void TetrahedralSelectionFallsBackToMultilinearForNonThreeDimensionalCluts()
    {
        IccClut clut = new IccClut
        {
            InputChannels = 2,
            OutputChannels = 2,
            GridPoints = new int[] { 2, 2 },
            Values = new double[]
            {
                0.0, 100.0,
                10.0, 110.0,
                20.0, 120.0,
                30.0, 130.0
            }
        };
        double[] selected = new double[2];
        double[] multilinear = new double[2];

        ClutInterpolator.InterpolateClut(clut, new double[] { 0.25, 0.75 }, selected, new int[4], new double[2], IccInterpolationKind.Tetrahedral);
        ClutInterpolator.InterpolateMultilinear(clut, new double[] { 0.25, 0.75 }, multilinear, new int[4], new double[2]);

        Assert.That(selected, Is.EqualTo(multilinear).Within(1e-15));
    }

    [Test]
    public void TetrahedralInterpolationReturnsExactVerticesAndClipsTheUnitDomain()
    {
        IccClut clut = CreateThreeDimensionalClut(1);
        clut.Values[0] = -0.0;
        double[] output = new double[1];
        int[] indexes = new int[6];
        double[] weights = new double[3];

        ClutInterpolator.InterpolateTetrahedral(clut, new double[] { 0.0, 0.0, 0.0 }, output, indexes, weights);
        long zeroBits = BitConverter.DoubleToInt64Bits(output[0]);
        ClutInterpolator.InterpolateTetrahedral(clut, new double[] { -2.0, 4.0, -1.0 }, output, indexes, weights);

        Assert.Multiple(() =>
        {
            Assert.That(zeroBits, Is.EqualTo(BitConverter.DoubleToInt64Bits(-0.0)));
            Assert.That(output[0], Is.EqualTo(4.0));
        });
    }

    [Test]
    public void SelectedLinearInterpolationSupportsOneDimensionalCluts()
    {
        IccClut clut = new IccClut
        {
            InputChannels = 1,
            OutputChannels = 1,
            GridPoints = new int[] { 2 },
            Values = new double[] { 10.0, 30.0 }
        };
        double[] output = new double[1];

        ClutInterpolator.InterpolateClut(clut, new double[] { 0.25 }, output, new int[2], new double[1], IccInterpolationKind.Linear);

        Assert.That(output[0], Is.EqualTo(15.0));
    }

    [Test]
    public void InvalidTetrahedralAndSelectionRequestsAreRejected()
    {
        IccClut twoDimensional = new IccClut
        {
            InputChannels = 2,
            OutputChannels = 1,
            GridPoints = new int[] { 2, 2 },
            Values = new double[] { 0.0, 1.0, 2.0, 3.0 }
        };
        IccClut nonFinite = CreateThreeDimensionalClut(1);
        nonFinite.Values[4] = double.NaN;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateTetrahedral(twoDimensional, new double[2], new double[1], new int[4], new double[2]));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateClut(twoDimensional, new double[2], new double[1], new int[4], new double[2], IccInterpolationKind.Linear));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateClut(twoDimensional, new double[2], new double[1], new int[4], new double[2], (IccInterpolationKind)99));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateTetrahedral(nonFinite, new double[] { 1.0, 0.0, 0.0 }, new double[1], new int[6], new double[3]));
        });
    }

    private static IccClut CreateThreeDimensionalClut(int outputChannels)
    {
        double[] scalarValues = new double[] { 0.0, 1.0, 4.0, 9.0, 16.0, 25.0, 36.0, 49.0 };
        double[] values = new double[scalarValues.Length * outputChannels];
        int vertex = 0;
        while (vertex < scalarValues.Length)
        {
            values[vertex * outputChannels] = scalarValues[vertex];
            if (outputChannels > 1)
            {
                values[(vertex * outputChannels) + 1] = 100.0 + (2.0 * scalarValues[vertex]);
            }
            vertex++;
        }

        return new IccClut
        {
            InputChannels = 3,
            OutputChannels = outputChannels,
            GridPoints = new int[] { 2, 2, 2 },
            Values = values
        };
    }
}
