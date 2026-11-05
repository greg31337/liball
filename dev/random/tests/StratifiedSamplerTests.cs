namespace random.Tests;

public class StratifiedSamplerTests
{
    [Test]
    public void RectangularGridMatchesLexicographicReferenceExactly()
    {
        int[] shape = [2, 3, 4];
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator referenceGenerator = Create(101UL);
        StratifiedSampler sampler = StratifiedSampler.FromStrata(actualGenerator, shape, false);
        shape[0] = 99;

        double[] actual = sampler.Sample(24);
        double[] expected = Reference(referenceGenerator, [2, 3, 4], false);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            Assert.That(sampler.Dimension(), Is.EqualTo(3));
            Assert.That(sampler.ConfiguredPointCount(), Is.EqualTo(24));
            Assert.That(sampler.IsCentered(), Is.False);
        });
    }

    [Test]
    public void CenteredGridUsesEveryCellCenterWithoutConsumingState()
    {
        RandomGenerator generator = Create(202UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        StratifiedSampler sampler = StratifiedSampler.FromStrata(generator, [2, 3], true);

        double[] actual = sampler.Sample(6);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(new double[]
            {
                0.25, 1.0 / 6.0,
                0.25, 0.5,
                0.25, 5.0 / 6.0,
                0.75, 1.0 / 6.0,
                0.75, 0.5,
                0.75, 5.0 / 6.0
            }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(sampler.IsCentered(), Is.True);
        });
    }

    [Test]
    public void InferredEqualGridOccupiesEveryCellExactlyOnce()
    {
        const int dimension = 3;
        const int levels = 3;
        const int pointCount = levels * levels * levels;
        StratifiedSampler sampler = StratifiedSampler.CreateInferred(Create(303UL), dimension, false);
        double[] points = sampler.Sample(pointCount);
        bool[] occupied = new bool[pointCount];

        for (int point = 0; point < pointCount; point++)
        {
            int cell = 0;
            for (int coordinate = 0; coordinate < dimension; coordinate++)
            {
                double value = points[point * dimension + coordinate];
                Assert.That(value, Is.InRange(0.0, Math.BitDecrement(1.0)));
                cell = cell * levels + (int)(value * levels);
            }

            Assert.That(occupied[cell], Is.False);
            occupied[cell] = true;
        }

        Assert.That(occupied, Is.All.True);
        Assert.That(sampler.ConfiguredPointCount(), Is.Zero);
    }

    [Test]
    public void UniformConfiguredAndInferredGridsReplayExactly()
    {
        StratifiedSampler configured = new StratifiedSampler(Create(404UL), 4, 3, false);
        StratifiedSampler inferred = StratifiedSampler.CreateInferred(Create(404UL), 4, false);
        double[] destination = new double[configured.RequiredOutputLength(81)];

        double[] actual = configured.Sample(81);
        inferred.SampleInto(81, destination);

        Assert.That(actual, Is.EqualTo(destination));
    }

    [Test]
    public void InvalidConstructionAndRequestsAreAtomic()
    {
        AssertArgument("generator", () => StratifiedSampler.CreateInferred(NullReference.Value<global::random.IRandomSource>(), 2, false));
        AssertArgument("dimension", () => StratifiedSampler.CreateInferred(Create(501UL), 0, false));
        AssertArgument("strataPerDimension", () => StratifiedSampler.FromStrata(Create(502UL), Array.Empty<int>(), false));
        AssertArgument("strataPerDimension", () => StratifiedSampler.FromStrata(Create(503UL), [2, 0], false));
        AssertArgument("strataPerDimension", () => StratifiedSampler.FromStrata(Create(504UL), [int.MaxValue, 2], false));

        RandomGenerator generator = Create(505UL);
        generator.NextBoolean();
        StratifiedSampler inferred = StratifiedSampler.CreateInferred(generator, 2, false);
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = [2.0, 3.0, 5.0, 7.0];
        AssertArgument("pointCount", () => inferred.SampleInto(3, new double[6]));
        AssertArgument("pointCount", () => inferred.SampleInto(0, Span<double>.Empty));
        AssertArgument("destination", () => inferred.SampleInto(4, destination));

        StratifiedSampler configured = StratifiedSampler.FromStrata(generator, [2, 3], false);
        AssertArgument("pointCount", () => configured.SampleInto(4, new double[8]));
        Assert.Multiple(() =>
        {
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(destination, Is.EqualTo(new double[] { 2.0, 3.0, 5.0, 7.0 }));
        });
    }

    static double[] Reference(RandomGenerator generator, int[] strata, bool centered)
    {
        int pointCount = strata.Aggregate(1, (product, count) => product * count);
        int dimension = strata.Length;
        int[] cell = new int[dimension];
        double[] result = new double[pointCount * dimension];
        for (int point = 0; point < pointCount; point++)
        {
            for (int coordinate = 0; coordinate < dimension; coordinate++)
            {
                double offset = centered ? 0.5 : generator.NextDoubleClosedOpen();
                result[point * dimension + coordinate] = (cell[coordinate] + offset) / strata[coordinate];
            }

            for (int coordinate = dimension - 1; coordinate >= 0; coordinate--)
            {
                cell[coordinate]++;
                if (cell[coordinate] < strata[coordinate])
                {
                    break;
                }

                cell[coordinate] = 0;
            }
        }

        return result;
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
