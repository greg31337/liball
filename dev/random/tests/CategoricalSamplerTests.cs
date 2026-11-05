namespace random.Tests;

public class CategoricalSamplerTests
{
    [Test]
    public void CumulativeSamplingMatchesNormalizedProbabilities()
    {
        RandomGenerator actualGenerator = Create(42UL);
        RandomGenerator referenceGenerator = Create(42UL);
        byte[] before = actualGenerator.ExportState().ToBytes();
        CategoricalSampler sampler = new CategoricalSampler(actualGenerator, [0.0, 1.0, 2.0, 0.0, 7.0, 0.0]);
        int[] counts = new int[6];

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        for (int index = 0; index < 2000; index++)
        {
            double unit = referenceGenerator.NextDoubleClosedOpen();
            int expected;
            if (unit < 0.1)
            {
                expected = 1;
            }
            else if (unit < 0.3)
            {
                expected = 2;
            }
            else
            {
                expected = 4;
            }

            int actual = sampler.Sample();
            counts[actual]++;
            Assert.That(actual, Is.EqualTo(expected));
        }

        Assert.Multiple(() =>
        {
            Assert.That(counts[0], Is.Zero);
            Assert.That(counts[1], Is.GreaterThan(0));
            Assert.That(counts[2], Is.GreaterThan(0));
            Assert.That(counts[3], Is.Zero);
            Assert.That(counts[4], Is.GreaterThan(0));
            Assert.That(counts[5], Is.Zero);
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void RelativeProbabilitiesNormalizeWithoutOverflowOrUnderflow()
    {
        CategoricalSampler maximumWeights = new CategoricalSampler(Create(123UL), [double.MaxValue, double.MaxValue, 0.0, double.MaxValue]);
        CategoricalSampler unitWeights = new CategoricalSampler(Create(123UL), [1.0, 1.0, 0.0, 1.0]);
        CategoricalSampler minimumWeights = new CategoricalSampler(Create(456UL), [double.Epsilon, 0.0, double.Epsilon]);
        CategoricalSampler equalWeights = new CategoricalSampler(Create(456UL), [1.0, 0.0, 1.0]);

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(maximumWeights.Sample(), Is.EqualTo(unitWeights.Sample()));
            Assert.That(minimumWeights.Sample(), Is.EqualTo(equalWeights.Sample()));
        }
    }

    [Test]
    public void SinglePositiveCategoryConsumesNoState()
    {
        RandomGenerator generator = Create(789UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        CategoricalSampler sampler = new CategoricalSampler(generator, [0.0, 0.0, 12.5, 0.0]);
        int[] values = new int[32];

        int scalar = sampler.Sample();
        sampler.Fill(values);
        sampler.Fill(Span<int>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(scalar, Is.EqualTo(2));
            Assert.That(values, Is.All.EqualTo(2));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void ConstructorDefensivelyPreprocessesTheInputArray()
    {
        double[] probabilities = [1.0, 3.0, 0.0, 6.0];
        RandomGenerator actualGenerator = Create(654UL);
        RandomGenerator referenceGenerator = Create(654UL);
        CategoricalSampler actual = new CategoricalSampler(actualGenerator, probabilities);
        CategoricalSampler reference = new CategoricalSampler(referenceGenerator, [1.0, 3.0, 0.0, 6.0]);

        probabilities[0] = double.NaN;
        probabilities[1] = 0.0;
        probabilities[2] = double.PositiveInfinity;
        probabilities[3] = 0.0;

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(actual.Sample(), Is.EqualTo(reference.Sample()));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void FillMatchesRepeatedScalarSamplesExactly()
    {
        RandomGenerator fillGenerator = Create(987UL);
        RandomGenerator scalarGenerator = Create(987UL);
        CategoricalSampler fillSampler = new CategoricalSampler(fillGenerator, [5.0, 0.0, 2.0, 3.0]);
        CategoricalSampler scalarSampler = new CategoricalSampler(scalarGenerator, [5.0, 0.0, 2.0, 3.0]);
        int[] actual = new int[257];
        int[] expected = new int[actual.Length];
        byte[] beforeEmpty = fillGenerator.ExportState().ToBytes();

        fillSampler.Fill(Span<int>.Empty);
        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(beforeEmpty));
        fillSampler.Fill(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = scalarSampler.Sample();
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Has.None.EqualTo(1));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void InvalidArgumentsAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullGenerator = Assert.Throws<RandomArgumentException>(() => new CategoricalSampler(NullReference.Value<global::random.IRandomSource>(), [1.0]));
        Assert.That(nullGenerator.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(321UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new CategoricalSampler(generator, NullReference.Value<double[]>())), "probabilities" },
            new object[] { (TestDelegate)(() => new CategoricalSampler(generator, [])), "probabilities" },
            new object[] { (TestDelegate)(() => new CategoricalSampler(generator, [0.0, 0.0])), "probabilities" },
            new object[] { (TestDelegate)(() => new CategoricalSampler(generator, [1.0, -double.Epsilon])), "probabilities" },
            new object[] { (TestDelegate)(() => new CategoricalSampler(generator, [1.0, double.NaN])), "probabilities" },
            new object[] { (TestDelegate)(() => new CategoricalSampler(generator, [1.0, double.NegativeInfinity])), "probabilities" },
            new object[] { (TestDelegate)(() => new CategoricalSampler(generator, [1.0, double.PositiveInfinity])), "probabilities" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
