namespace random.Tests;

public class PoissonDiskSamplerTests
{
    [Test]
    public void SamplesMatchBoundedSequentialReferenceExactly()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator referenceGenerator = Create(101UL);
        PoissonDiskSampler sampler = new PoissonDiskSampler(actualGenerator, 2, 0.15, 200);
        long expectedAttempts;
        double[] expected = Reference(referenceGenerator, 18, 2, 0.15, 200, out expectedAttempts);

        double[] actual = sampler.Sample(18);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(sampler.Dimension(), Is.EqualTo(2));
            Assert.That(sampler.MinimumDistance(), Is.EqualTo(0.15));
            Assert.That(sampler.MaximumAttemptsPerPoint(), Is.EqualTo(200));
            Assert.That(sampler.LastAcceptedPointCount(), Is.EqualTo(18));
            Assert.That(sampler.LastAttemptCount(), Is.EqualTo(expectedAttempts));
            Assert.That(sampler.TotalAttemptCount(), Is.EqualTo(expectedAttempts));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void EveryPointIsInTheUnitHypercubeAndSeparated()
    {
        const int pointCount = 60;
        const int dimension = 3;
        const double minimumDistance = 0.12;
        PoissonDiskSampler sampler = new PoissonDiskSampler(Create(202UL), dimension, minimumDistance, 1000);
        double[] points = sampler.Sample(pointCount);

        foreach (double coordinate in points)
        {
            Assert.That(coordinate, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(coordinate, Is.LessThan(1.0));
        }

        AssertMinimumDistance(points, pointCount, dimension, minimumDistance);
    }

    [Test]
    public void AllocatingAndDestinationRequestsReplayWithMatchingDiagnostics()
    {
        PoissonDiskSampler allocating = new PoissonDiskSampler(Create(303UL), 2, 0.1, 500);
        PoissonDiskSampler destination = new PoissonDiskSampler(Create(303UL), 2, 0.1, 500);
        double[] expected = new double[destination.RequiredOutputLength(30)];

        double[] actual = allocating.Sample(30);
        destination.SampleInto(30, expected);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(allocating.LastAcceptedPointCount(), Is.EqualTo(destination.LastAcceptedPointCount()));
            Assert.That(allocating.LastAttemptCount(), Is.EqualTo(destination.LastAttemptCount()));
            Assert.That(allocating.TotalAttemptCount(), Is.EqualTo(destination.TotalAttemptCount()));
        });
    }

    [Test]
    public void ExhaustionPreservesPartialOutputAndReportsExactProgress()
    {
        RandomGenerator generator = Create(404UL);
        RandomGenerator reference = Create(404UL);
        PoissonDiskSampler sampler = new PoissonDiskSampler(generator, 1, 2.0, 3);
        double[] destination = [-1.0, -1.0];

        RandomExhaustedException error = Assert.Throws<RandomExhaustedException>(() => sampler.SampleInto(2, destination));
        for (int attempt = 0; attempt < 4; attempt++)
        {
            reference.NextDoubleClosedOpen();
        }

        Assert.Multiple(() =>
        {
            Assert.That(error.Message, Does.Contain("placed 1 of 2"));
            Assert.That(destination[0], Is.InRange(0.0, Math.BitDecrement(1.0)));
            Assert.That(destination[1], Is.EqualTo(-1.0));
            Assert.That(sampler.LastAcceptedPointCount(), Is.EqualTo(1));
            Assert.That(sampler.LastAttemptCount(), Is.EqualTo(4L));
            Assert.That(sampler.TotalAttemptCount(), Is.EqualTo(4L));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(reference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void DiagnosticsResetPerValidRequestAndAccumulateAcrossRequests()
    {
        PoissonDiskSampler sampler = new PoissonDiskSampler(Create(505UL), 2, 0.05, 50);
        sampler.Sample(5);
        long firstAttempts = sampler.LastAttemptCount();

        sampler.Sample(1);

        Assert.Multiple(() =>
        {
            Assert.That(sampler.LastAcceptedPointCount(), Is.EqualTo(1));
            Assert.That(sampler.LastAttemptCount(), Is.EqualTo(1L));
            Assert.That(sampler.TotalAttemptCount(), Is.EqualTo(firstAttempts + 1L));
        });
    }

    [Test]
    public void InvalidConstructionAndRequestsAreAtomic()
    {
        AssertArgument("generator", () => new PoissonDiskSampler(NullReference.Value<global::random.IRandomSource>(), 2, 0.1, 10));
        AssertArgument("dimension", () => new PoissonDiskSampler(Create(601UL), 0, 0.1, 10));
        AssertArgument("minimumDistance", () => new PoissonDiskSampler(Create(602UL), 2, double.NaN, 10));
        AssertArgument("minimumDistance", () => new PoissonDiskSampler(Create(603UL), 2, 0.0, 10));
        AssertArgument("minimumDistance", () => new PoissonDiskSampler(Create(604UL), 2, double.PositiveInfinity, 10));
        AssertArgument("maximumAttemptsPerPoint", () => new PoissonDiskSampler(Create(605UL), 2, 0.1, 0));

        RandomGenerator generator = Create(606UL);
        PoissonDiskSampler sampler = new PoissonDiskSampler(generator, 2, 0.1, 10);
        sampler.Sample(1);
        byte[] before = generator.ExportState().ToBytes();
        long attemptsBefore = sampler.LastAttemptCount();
        long totalBefore = sampler.TotalAttemptCount();
        double[] destination = [2.0, 3.0];
        AssertArgument("pointCount", () => sampler.SampleInto(0, Span<double>.Empty));
        AssertArgument("destination", () => sampler.SampleInto(2, destination));
        AssertArgument("pointCount", () => sampler.RequiredOutputLength(int.MaxValue));

        Assert.Multiple(() =>
        {
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(destination, Is.EqualTo(new double[] { 2.0, 3.0 }));
            Assert.That(sampler.LastAcceptedPointCount(), Is.EqualTo(1));
            Assert.That(sampler.LastAttemptCount(), Is.EqualTo(attemptsBefore));
            Assert.That(sampler.TotalAttemptCount(), Is.EqualTo(totalBefore));
        });
    }

    static double[] Reference(RandomGenerator generator, int pointCount, int dimension, double minimumDistance, int maximumAttempts, out long attemptCount)
    {
        double[] result = new double[pointCount * dimension];
        double[] candidate = new double[dimension];
        double minimumDistanceSquared = minimumDistance * minimumDistance;
        attemptCount = 0L;
        for (int point = 0; point < pointCount; point++)
        {
            bool accepted = false;
            for (int attempt = 0; attempt < maximumAttempts; attempt++)
            {
                attemptCount++;
                for (int coordinate = 0; coordinate < dimension; coordinate++)
                {
                    candidate[coordinate] = generator.NextDoubleClosedOpen();
                }

                accepted = true;
                for (int previous = 0; previous < point; previous++)
                {
                    double distanceSquared = 0.0;
                    for (int coordinate = 0; coordinate < dimension; coordinate++)
                    {
                        double delta = candidate[coordinate] - result[previous * dimension + coordinate];
                        distanceSquared += delta * delta;
                    }

                    if (distanceSquared < minimumDistanceSquared)
                    {
                        accepted = false;
                        break;
                    }
                }

                if (accepted)
                {
                    break;
                }
            }

            if (!accepted)
            {
                throw new AssertionException("The deterministic reference exhausted unexpectedly.");
            }

            Array.Copy(candidate, 0, result, point * dimension, dimension);
        }

        return result;
    }

    static void AssertMinimumDistance(double[] points, int pointCount, int dimension, double minimumDistance)
    {
        double minimumDistanceSquared = minimumDistance * minimumDistance;
        for (int first = 0; first < pointCount; first++)
        {
            for (int second = first + 1; second < pointCount; second++)
            {
                double distanceSquared = 0.0;
                for (int coordinate = 0; coordinate < dimension; coordinate++)
                {
                    double delta = points[first * dimension + coordinate] - points[second * dimension + coordinate];
                    distanceSquared += delta * delta;
                }

                Assert.That(distanceSquared, Is.GreaterThanOrEqualTo(minimumDistanceSquared));
            }
        }
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
