namespace random.Tests;

public class LatinHypercubeSamplerTests
{
    [Test]
    public void RandomizedSampleMatchesDeterministicPermutationReferenceExactly()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator referenceGenerator = Create(101UL);
        LatinHypercubeSampler sampler = new LatinHypercubeSampler(actualGenerator, 4, false);
        double[] expected = ReferencePlain(referenceGenerator, 19, 4, false);

        double[] actual = sampler.Sample(19);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            Assert.That(sampler.Dimension(), Is.EqualTo(4));
            Assert.That(sampler.IsCentered(), Is.False);
        });
    }

    [Test]
    public void CenteredSampleMatchesDeterministicPermutationReferenceExactly()
    {
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator referenceGenerator = Create(202UL);
        LatinHypercubeSampler sampler = new LatinHypercubeSampler(actualGenerator, 5, true);
        double[] expected = ReferencePlain(referenceGenerator, 23, 5, true);

        double[] actual = sampler.Sample(23);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            Assert.That(sampler.IsCentered(), Is.True);
        });
    }

    [Test]
    public void EveryPlainMarginalOccupiesEveryStratumExactlyOnce()
    {
        const int pointCount = 37;
        LatinHypercubeSampler sampler = new LatinHypercubeSampler(Create(303UL), 9, false);
        double[] points = sampler.Sample(pointCount);

        AssertLatinMarginals(points, pointCount, sampler.Dimension());
    }

    [Test]
    public void AllocatingAndDestinationSamplesMatchExactly()
    {
        LatinHypercubeSampler allocating = new LatinHypercubeSampler(Create(404UL), 6, false);
        LatinHypercubeSampler destination = new LatinHypercubeSampler(Create(404UL), 6, false);
        double[] expected = new double[destination.RequiredOutputLength(31)];

        double[] actual = allocating.Sample(31);
        destination.SampleInto(31, expected);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void OrthogonalSampleHasLatinMarginalsAndStrengthTwoCoarseCells()
    {
        const int prime = 5;
        const int pointCount = prime * prime;
        LatinHypercubeSampler sampler = new LatinHypercubeSampler(Create(505UL), prime + 1, false);
        double[] points = sampler.SampleOrthogonal(pointCount);

        AssertLatinMarginals(points, pointCount, sampler.Dimension());
        for (int firstCoordinate = 0; firstCoordinate < sampler.Dimension(); firstCoordinate++)
        {
            for (int secondCoordinate = firstCoordinate + 1; secondCoordinate < sampler.Dimension(); secondCoordinate++)
            {
                bool[] cells = new bool[pointCount];
                for (int point = 0; point < pointCount; point++)
                {
                    int firstCell = (int)(points[point * sampler.Dimension() + firstCoordinate] * prime);
                    int secondCell = (int)(points[point * sampler.Dimension() + secondCoordinate] * prime);
                    int cell = firstCell * prime + secondCell;
                    Assert.That(cells[cell], Is.False, $"coordinates {firstCoordinate},{secondCoordinate}, cell {cell}");
                    cells[cell] = true;
                }

                Assert.That(cells, Is.All.True);
            }
        }
    }

    [Test]
    public void CenteredOrthogonalSampleUsesFineStratumCenters()
    {
        const int pointCount = 49;
        LatinHypercubeSampler sampler = new LatinHypercubeSampler(Create(606UL), 5, true);
        double[] points = sampler.SampleOrthogonal(pointCount);

        foreach (double value in points)
        {
            double scaled = value * pointCount - 0.5;
            Assert.That(scaled, Is.EqualTo(Math.Round(scaled)).Within(1.0e-12));
        }

        AssertLatinMarginals(points, pointCount, sampler.Dimension());
    }

    [Test]
    public void OrthogonalAllocatingAndDestinationSamplesReplayExactly()
    {
        LatinHypercubeSampler allocating = new LatinHypercubeSampler(Create(707UL), 4, false);
        LatinHypercubeSampler destination = new LatinHypercubeSampler(Create(707UL), 4, false);
        double[] expected = new double[100];

        double[] actual = allocating.SampleOrthogonal(25);
        destination.SampleOrthogonalInto(25, expected);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void InvalidConstructionAndPlainRequestsAreAtomic()
    {
        RandomArgumentException nullError = Assert.Throws<RandomArgumentException>(() => new LatinHypercubeSampler(NullReference.Value<global::random.IRandomSource>(), 2, false));
        Assert.That(nullError.ArgumentName(), Is.EqualTo("generator"));
        RandomArgumentException dimensionError = Assert.Throws<RandomArgumentException>(() => new LatinHypercubeSampler(Create(800UL), 0, false));
        Assert.That(dimensionError.ArgumentName(), Is.EqualTo("dimension"));

        RandomGenerator generator = Create(808UL);
        generator.NextBoolean();
        LatinHypercubeSampler sampler = new LatinHypercubeSampler(generator, 3, false);
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = [2.0, 3.0, 5.0];
        object[] cases =
        [
            new object[] { (TestDelegate)(() => sampler.SampleInto(0, Span<double>.Empty)), "pointCount" },
            new object[] { (TestDelegate)(() => sampler.SampleInto(-1, Span<double>.Empty)), "pointCount" },
            new object[] { (TestDelegate)(() => sampler.SampleInto(2, destination)), "destination" },
            new object[] { (TestDelegate)(() => sampler.RequiredOutputLength(int.MaxValue)), "pointCount" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(destination, Is.EqualTo(new double[] { 2.0, 3.0, 5.0 }));
        }
    }

    [Test]
    public void UnsupportedOrthogonalShapesAreAtomic()
    {
        RandomGenerator generator = Create(909UL);
        LatinHypercubeSampler sampler = new LatinHypercubeSampler(generator, 4, false);
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = new double[16];
        Array.Fill(destination, 11.0);
        object[] cases =
        [
            new object[] { (TestDelegate)(() => sampler.SampleOrthogonalInto(1, new double[4])), "pointCount" },
            new object[] { (TestDelegate)(() => sampler.SampleOrthogonalInto(8, new double[32])), "pointCount" },
            new object[] { (TestDelegate)(() => sampler.SampleOrthogonalInto(16, new double[64])), "pointCount" },
            new object[] { (TestDelegate)(() => sampler.SampleOrthogonalInto(4, destination)), "dimension" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(destination, Is.All.EqualTo(11.0));
        }
    }

    static double[] ReferencePlain(RandomGenerator generator, int pointCount, int dimension, bool centered)
    {
        double[] result = new double[pointCount * dimension];
        for (int coordinate = 0; coordinate < dimension; coordinate++)
        {
            int[] strata = Enumerable.Range(0, pointCount).ToArray();
            RandomCollections.Shuffle(generator, strata);
            for (int point = 0; point < pointCount; point++)
            {
                double offset = centered ? 0.5 : generator.NextDoubleClosedOpen();
                result[point * dimension + coordinate] = (strata[point] + offset) / pointCount;
            }
        }

        return result;
    }

    static void AssertLatinMarginals(double[] points, int pointCount, int dimension)
    {
        for (int coordinate = 0; coordinate < dimension; coordinate++)
        {
            bool[] occupied = new bool[pointCount];
            for (int point = 0; point < pointCount; point++)
            {
                double value = points[point * dimension + coordinate];
                Assert.That(value, Is.GreaterThanOrEqualTo(0.0));
                Assert.That(value, Is.LessThan(1.0));
                int stratum = (int)(value * pointCount);
                Assert.That(occupied[stratum], Is.False, $"coordinate {coordinate}, stratum {stratum}");
                occupied[stratum] = true;
            }

            Assert.That(occupied, Is.All.True);
        }
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
