namespace random.Tests;

public class DirichletSamplerTests
{
    [Test]
    public void AllocatingAndDestinationSamplesMatchInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator allocatingGenerator = Create(101UL, mode);
            RandomGenerator destinationGenerator = Create(101UL, mode);
            DirichletSampler allocating = new DirichletSampler(allocatingGenerator, [0.25, 1.5, 3.0, 7.0]);
            DirichletSampler writing = new DirichletSampler(destinationGenerator, [0.25, 1.5, 3.0, 7.0]);

            for (int sampleIndex = 0; sampleIndex < 1000; sampleIndex++)
            {
                double[] actual = allocating.Sample();
                double[] expected = new double[4];
                writing.SampleInto(expected);
                Assert.That(actual, Is.EqualTo(expected));
                AssertSimplex(actual);
            }

            Assert.That(allocatingGenerator.ExportState().ToBytes(), Is.EqualTo(destinationGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void TinyConcentrationsUseAnUnbiasedRepresentableVertexRace()
    {
        const int sampleCount = 14000;
        DirichletSampler sampler = new DirichletSampler(
            Create(202UL, RandomMathMode.Portable),
            [double.Epsilon, 2.0 * double.Epsilon, 4.0 * double.Epsilon]);
        int[] selected = new int[3];
        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            double[] sample = sampler.Sample();
            AssertSimplex(sample);
            Assert.That(sample.Count(value => value == 1.0), Is.EqualTo(1));
            Assert.That(sample.Count(value => value == 0.0), Is.EqualTo(2));
            selected[Array.IndexOf(sample, 1.0)]++;
        }

        Assert.Multiple(() =>
        {
            Assert.That((double)selected[0] / sampleCount, Is.EqualTo(1.0 / 7.0).Within(0.015));
            Assert.That((double)selected[1] / sampleCount, Is.EqualTo(2.0 / 7.0).Within(0.015));
            Assert.That((double)selected[2] / sampleCount, Is.EqualTo(4.0 / 7.0).Within(0.015));
        });
    }

    [Test]
    public void SparseAndMaximumConcentrationsRemainFiniteSimplexes()
    {
        double[][] concentrationSets =
        [
            [1.0e-300, 2.0e-300, 3.0e-300, 4.0e-300],
            [double.Epsilon, 1.0, double.MaxValue],
            [double.MaxValue, double.MaxValue]
        ];

        foreach (double[] concentrations in concentrationSets)
        {
            DirichletSampler sampler = new DirichletSampler(Create(303UL, RandomMathMode.Portable), concentrations);
            for (int sampleIndex = 0; sampleIndex < 1000; sampleIndex++)
            {
                AssertSimplex(sampler.Sample());
            }
        }
    }

    [Test]
    public void SampleMomentsMatchTheDirichletLaw()
    {
        const int sampleCount = 150000;
        double[] concentrations = [0.5, 1.5, 3.0];
        double totalConcentration = concentrations.Sum();
        DirichletSampler sampler = new DirichletSampler(Create(404UL, RandomMathMode.Portable), concentrations);
        double[] sums = new double[concentrations.Length];
        double[] products = new double[concentrations.Length * concentrations.Length];
        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            double[] sample = sampler.Sample();
            for (int first = 0; first < sample.Length; first++)
            {
                sums[first] += sample[first];
                for (int second = 0; second < sample.Length; second++)
                {
                    products[first * sample.Length + second] += sample[first] * sample[second];
                }
            }
        }

        double[] means = new double[concentrations.Length];
        for (int index = 0; index < means.Length; index++)
        {
            means[index] = sums[index] / sampleCount;
        }

        Assert.Multiple(() =>
        {
            for (int first = 0; first < concentrations.Length; first++)
            {
                double expectedMean = concentrations[first] / totalConcentration;
                Assert.That(means[first], Is.EqualTo(expectedMean).Within(0.003));
                for (int second = 0; second < concentrations.Length; second++)
                {
                    double numerator;
                    if (first == second)
                    {
                        numerator = concentrations[first] * (totalConcentration - concentrations[first]);
                    }
                    else
                    {
                        numerator = -concentrations[first] * concentrations[second];
                    }

                    double expectedCovariance = numerator /
                        (totalConcentration * totalConcentration * (totalConcentration + 1.0));
                    double covariance = products[first * concentrations.Length + second] / sampleCount -
                        means[first] * means[second];
                    Assert.That(covariance, Is.EqualTo(expectedCovariance).Within(0.0015));
                }
            }
        });
    }

    [Test]
    public void OneComponentIsDeterministicWithoutConsumption()
    {
        RandomGenerator generator = Create(505UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        DirichletSampler sampler = new DirichletSampler(generator, [double.Epsilon]);
        double[] destination = [0.0];

        double[] allocated = sampler.Sample();
        sampler.SampleInto(destination);

        Assert.Multiple(() =>
        {
            Assert.That(sampler.ComponentCount(), Is.EqualTo(1));
            Assert.That(allocated, Is.EqualTo(new double[] { 1.0 }));
            Assert.That(destination, Is.EqualTo(new double[] { 1.0 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void ConcentrationsAreDefensivelyCopied()
    {
        double[] input = [0.25, 1.5, 3.0];
        RandomGenerator actualGenerator = Create(606UL, RandomMathMode.Portable);
        RandomGenerator expectedGenerator = Create(606UL, RandomMathMode.Portable);
        DirichletSampler actual = new DirichletSampler(actualGenerator, input);
        DirichletSampler expected = new DirichletSampler(expectedGenerator, [0.25, 1.5, 3.0]);
        input[0] = double.NaN;
        input[1] = double.MaxValue;
        input[2] = double.Epsilon;

        for (int sampleIndex = 0; sampleIndex < 100; sampleIndex++)
        {
            Assert.That(actual.Sample(), Is.EqualTo(expected.Sample()));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void InvalidInputsAreRejectedBeforeStateOrDestinationChanges()
    {
        RandomArgumentException nullGenerator = Assert.Throws<RandomArgumentException>(() => new DirichletSampler(NullReference.Value<global::random.IRandomSource>(), [1.0]));
        Assert.That(nullGenerator.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(707UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[][] invalid =
        [
            [],
            [0.0],
            [-double.Epsilon, 1.0],
            [double.NaN],
            [double.NegativeInfinity],
            [double.PositiveInfinity]
        ];

        Assert.That(Assert.Throws<RandomArgumentException>(() => new DirichletSampler(generator, NullReference.Value<double[]>()))?.ArgumentName(), Is.EqualTo("concentrations"));
        foreach (double[] concentrations in invalid)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => new DirichletSampler(generator, concentrations));
            Assert.That(error.ArgumentName(), Is.EqualTo("concentrations"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }

        DirichletSampler sampler = new DirichletSampler(generator, [1.0, 2.0, 3.0]);
        double[] destination = [91.0, 92.0];
        RandomArgumentException destinationError = Assert.Throws<RandomArgumentException>(() => sampler.SampleInto(destination));
        Assert.Multiple(() =>
        {
            Assert.That(destinationError.ArgumentName(), Is.EqualTo("destination"));
            Assert.That(destination, Is.EqualTo(new double[] { 91.0, 92.0 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    static void AssertSimplex(double[] sample)
    {
        double total = 0.0;
        for (int index = 0; index < sample.Length; index++)
        {
            Assert.That(double.IsFinite(sample[index]), Is.True);
            Assert.That(sample[index], Is.InRange(0.0, 1.0));
            total += sample[index];
        }

        Assert.That(total, Is.EqualTo(1.0));
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
