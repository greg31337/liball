using icc;

namespace icc.Tests;

public class MultilinearClutInterpolationTests
{
    [Test]
    public void BilinearInterpolationUsesIccGridOrderAndInterleavedOutputs()
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
        double[] outputs = new double[3] { -1.0, -1.0, 777.0 };
        int[] indexes = new int[ClutInterpolator.GetMultilinearIndexScratchLength(2)];
        double[] weights = new double[ClutInterpolator.GetMultilinearWeightScratchLength(2)];

        ClutInterpolator.InterpolateMultilinear(clut, new double[] { 0.25, 0.75 }, outputs, indexes, weights);

        Assert.Multiple(() =>
        {
            Assert.That(outputs[0], Is.EqualTo(12.5).Within(1e-14));
            Assert.That(outputs[1], Is.EqualTo(112.5).Within(1e-14));
            Assert.That(outputs[2], Is.EqualTo(777.0));
        });
    }

    [Test]
    public void UnevenGridReturnsExactKnotsAndClipsInputsToTheUnitDomain()
    {
        IccClut clut = new IccClut
        {
            InputChannels = 2,
            OutputChannels = 1,
            GridPoints = new int[] { 2, 3 },
            Values = new double[] { -0.0, 1.0, 2.0, 10.0, 11.0, 12.0 }
        };
        int[] indexes = new int[4];
        double[] weights = new double[2];
        double[] outputs = new double[1];

        ClutInterpolator.InterpolateMultilinear(clut, new double[] { 0.0, 0.0 }, outputs, indexes, weights);
        long firstBits = BitConverter.DoubleToInt64Bits(outputs[0]);
        ClutInterpolator.InterpolateMultilinear(clut, new double[] { 1.0, 0.5 }, outputs, indexes, weights);
        double exactKnot = outputs[0];
        ClutInterpolator.InterpolateMultilinear(clut, new double[] { -4.0, 7.0 }, outputs, indexes, weights);

        Assert.Multiple(() =>
        {
            Assert.That(firstBits, Is.EqualTo(BitConverter.DoubleToInt64Bits(-0.0)));
            Assert.That(exactKnot, Is.EqualTo(11.0));
            Assert.That(outputs[0], Is.EqualTo(2.0));
        });
    }

    [Test]
    public void OneDimensionalClutSupportsArbitraryOutputChannelCounts()
    {
        const int outputChannels = 19;
        double[] values = new double[outputChannels * 2];
        int output = 0;
        while (output < outputChannels)
        {
            values[output] = output;
            values[outputChannels + output] = 100.0 + output;
            output++;
        }

        IccClut clut = new IccClut
        {
            InputChannels = 1,
            OutputChannels = outputChannels,
            GridPoints = new int[] { 2 },
            Values = values
        };
        double[] outputs = new double[outputChannels];

        ClutInterpolator.InterpolateMultilinear(clut, new double[] { 0.2 }, outputs, new int[2], new double[1]);

        output = 0;
        while (output < outputChannels)
        {
            Assert.That(outputs[output], Is.EqualTo(20.0 + output).Within(1e-14));
            output++;
        }
    }

    [Test]
    public void FifteenDimensionalInterpolationUsesAllInputs()
    {
        const int inputChannels = 15;
        const int vertexCount = 1 << inputChannels;
        double[] values = new double[vertexCount];
        int vertex = 0;
        while (vertex < vertexCount)
        {
            double value = 0.0;
            int dimension = 0;
            while (dimension < inputChannels)
            {
                int bit = 1 << (inputChannels - dimension - 1);
                if ((vertex & bit) != 0)
                {
                    value += dimension + 1;
                }
                dimension++;
            }
            values[vertex] = value;
            vertex++;
        }

        IccClut clut = new IccClut
        {
            InputChannels = inputChannels,
            OutputChannels = 1,
            GridPoints = new int[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 },
            Values = values
        };
        double[] inputs = new double[] { 0.05, 0.1, 0.15, 0.2, 0.25, 0.3, 0.35, 0.4, 0.45, 0.5, 0.55, 0.6, 0.65, 0.7, 0.75 };
        double expected = 0.0;
        int index = 0;
        while (index < inputChannels)
        {
            expected += inputs[index] * (index + 1);
            index++;
        }
        double[] outputs = new double[1];

        ClutInterpolator.InterpolateMultilinear(clut, inputs, outputs, new int[inputChannels * 2], new double[inputChannels]);

        Assert.That(outputs[0], Is.EqualTo(expected).Within(1e-10));
    }

    [Test]
    public void CallerOwnedScratchCanBeReusedWithoutPerSampleAllocation()
    {
        IccClut clut = new IccClut
        {
            InputChannels = 2,
            OutputChannels = 1,
            GridPoints = new int[] { 2, 2 },
            Values = new double[] { 0.0, 1.0, 2.0, 3.0 }
        };
        double[] inputs = new double[] { 0.25, 0.75 };
        double[] outputs = new double[1];
        int[] indexes = new int[4];
        double[] weights = new double[2];
        ClutInterpolator.InterpolateMultilinear(clut, inputs, outputs, indexes, weights);

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        int sample = 0;
        while (sample < 100)
        {
            inputs[0] = sample / 100.0;
            ClutInterpolator.InterpolateMultilinear(clut, inputs, outputs, indexes, weights);
            sample++;
        }
        long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();

        Assert.Multiple(() =>
        {
            Assert.That(allocatedAfter, Is.EqualTo(allocatedBefore));
            Assert.That(outputs[0], Is.EqualTo(2.73).Within(1e-14));
        });
    }

    [Test]
    public void InvalidDimensionsBuffersInputsAndReferencedValuesAreRejected()
    {
        IccClut valid = new IccClut
        {
            InputChannels = 2,
            OutputChannels = 1,
            GridPoints = new int[] { 2, 2 },
            Values = new double[] { 0.0, 1.0, 2.0, 3.0 }
        };
        IccClut tooManyInputs = new IccClut
        {
            InputChannels = 16,
            OutputChannels = 1,
            GridPoints = new int[16],
            Values = new double[0]
        };
        IccClut overflowing = new IccClut
        {
            InputChannels = 5,
            OutputChannels = 1,
            GridPoints = new int[] { 255, 255, 255, 255, 255 },
            Values = new double[0]
        };
        IccClut nonFinite = new IccClut
        {
            InputChannels = 1,
            OutputChannels = 1,
            GridPoints = new int[] { 2 },
            Values = new double[] { double.NaN, 1.0 }
        };

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateMultilinear(null!, new double[1], new double[1], new int[2], new double[1]));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateMultilinear(tooManyInputs, new double[16], new double[1], new int[32], new double[16]));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateMultilinear(overflowing, new double[5], new double[1], new int[10], new double[5]));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateMultilinear(valid, new double[1], new double[1], new int[4], new double[2]));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateMultilinear(valid, new double[2], new double[0], new int[4], new double[2]));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateMultilinear(valid, new double[2], new double[1], new int[3], new double[2]));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateMultilinear(valid, new double[2], new double[1], new int[4], new double[1]));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateMultilinear(valid, new double[] { double.PositiveInfinity, 0.0 }, new double[1], new int[4], new double[2]));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.InterpolateMultilinear(nonFinite, new double[] { 0.0 }, new double[1], new int[2], new double[1]));
            Assert.Throws<IccProfileValidationException>(() => ClutInterpolator.GetMultilinearIndexScratchLength(0));
        });
    }
}
