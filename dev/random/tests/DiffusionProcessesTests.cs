namespace random.Tests;

public class DiffusionProcessesTests
{
    [Test]
    public void BrownianIncrementsMatchScaledStandardNormalsExactly()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator referenceGenerator = Create(101UL);
        NormalSampler normal = new NormalSampler(referenceGenerator, 0.0, 1.0);
        double[] expected = new double[40];
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = 0.5 * normal.Sample();
        }

        double[] actual = RandomProcesses.BrownianIncrements(actualGenerator, 0.25, 40);

        AssertExact(actual, expected);
        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void BrownianMotionMatchesDriftAndDiffusionRecurrenceExactly()
    {
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator referenceGenerator = Create(202UL);
        NormalSampler normal = new NormalSampler(referenceGenerator, 0.0, 1.0);
        double[] expected = new double[33];
        expected[0] = -1.5;
        for (int step = 1; step < expected.Length; step++)
        {
            expected[step] = expected[step - 1] + 0.3 * 0.25 + 0.8 * 0.5 * normal.Sample();
        }

        double[] actual = RandomProcesses.BrownianMotion(actualGenerator, -1.5, 0.3, 0.8, 0.25, 32);

        AssertExact(actual, expected);
        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void BrownianBridgeMatchesConditionalRecurrenceAndPinsEndpoints()
    {
        const int stepCount = 12;
        RandomGenerator actualGenerator = Create(303UL);
        RandomGenerator referenceGenerator = Create(303UL);
        NormalSampler normal = new NormalSampler(referenceGenerator, 0.0, 1.0);
        double[] expected = new double[stepCount + 1];
        expected[0] = -2.0;
        for (int step = 1; step < stepCount; step++)
        {
            int remaining = stepCount - step + 1;
            double conditionalMean = (remaining - 1.0) / remaining * expected[step - 1] + 3.0 / remaining;
            double conditionalScale = 1.25 * PortableMath.Sqrt(0.2 * (remaining - 1.0) / remaining);
            expected[step] = conditionalMean + conditionalScale * normal.Sample();
        }

        expected[stepCount] = 3.0;
        double[] actual = RandomProcesses.BrownianBridge(actualGenerator, -2.0, 3.0, 1.25, 0.2, stepCount);

        AssertExact(actual, expected);
        Assert.Multiple(() =>
        {
            Assert.That(actual[0], Is.EqualTo(-2.0));
            Assert.That(actual[^1], Is.EqualTo(3.0));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void GeometricBrownianMotionUsesExactExponentialTransition()
    {
        RandomGenerator actualGenerator = Create(404UL);
        RandomGenerator referenceGenerator = Create(404UL);
        NormalSampler normal = new NormalSampler(referenceGenerator, 0.0, 1.0);
        double[] expected = new double[25];
        expected[0] = 50.0;
        double exponentDrift = (0.07 - 0.5 * 0.3 * 0.3) * 0.25;
        double exponentScale = 0.3 * 0.5;
        for (int step = 1; step < expected.Length; step++)
        {
            expected[step] = expected[step - 1] * PortableMath.Exp(exponentDrift + exponentScale * normal.Sample());
        }

        double[] actual = RandomProcesses.GeometricBrownianMotion(actualGenerator, 50.0, 0.07, 0.3, 0.25, 24);

        AssertExact(actual, expected);
        Assert.That(actual, Is.All.Positive);
        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void OrnsteinUhlenbeckUsesExactGaussianTransition()
    {
        RandomGenerator actualGenerator = Create(505UL);
        RandomGenerator referenceGenerator = Create(505UL);
        NormalSampler normal = new NormalSampler(referenceGenerator, 0.0, 1.0);
        double decay = PortableMath.Exp(-0.7 * 0.125);
        double varianceTime = -PortableMath.Expm1(-2.0 * 0.7 * 0.125) / (2.0 * 0.7);
        double scale = 0.4 * PortableMath.Sqrt(varianceTime);
        double[] expected = new double[29];
        expected[0] = -3.0;
        for (int step = 1; step < expected.Length; step++)
        {
            expected[step] = decay * expected[step - 1] + (1.0 - decay) * 2.0 + scale * normal.Sample();
        }

        double[] actual = RandomProcesses.OrnsteinUhlenbeck(actualGenerator, -3.0, 2.0, 0.7, 0.4, 0.125, 28);

        AssertExact(actual, expected);
        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void EveryBatchMatchesRepeatedSinglePathsInPathMajorOrder()
    {
        AssertBatch(
            generator => RandomProcesses.BrownianIncrements(generator, 0.5, 4, 9),
            generator => RandomProcesses.BrownianIncrements(generator, 0.5, 9),
            9,
            601UL);
        AssertBatch(
            generator => RandomProcesses.BrownianMotion(generator, 1.0, -0.2, 0.7, 0.5, 4, 9),
            generator => RandomProcesses.BrownianMotion(generator, 1.0, -0.2, 0.7, 0.5, 9),
            10,
            602UL);
        AssertBatch(
            generator => RandomProcesses.BrownianBridge(generator, -1.0, 2.0, 0.7, 0.5, 4, 9),
            generator => RandomProcesses.BrownianBridge(generator, -1.0, 2.0, 0.7, 0.5, 9),
            10,
            603UL);
        AssertBatch(
            generator => RandomProcesses.GeometricBrownianMotion(generator, 10.0, 0.03, 0.2, 0.5, 4, 9),
            generator => RandomProcesses.GeometricBrownianMotion(generator, 10.0, 0.03, 0.2, 0.5, 9),
            10,
            604UL);
        AssertBatch(
            generator => RandomProcesses.OrnsteinUhlenbeck(generator, 1.0, -2.0, 0.3, 0.6, 0.5, 4, 9),
            generator => RandomProcesses.OrnsteinUhlenbeck(generator, 1.0, -2.0, 0.3, 0.6, 0.5, 9),
            10,
            605UL);
    }

    [Test]
    public void DegenerateDiffusionsAreDeterministicAndConsumeNoState()
    {
        RandomGenerator generator = Create(707UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        double[] brownian = RandomProcesses.BrownianMotion(generator, 1.0, 2.0, 0.0, 0.5, 4);
        double[] bridge = RandomProcesses.BrownianBridge(generator, -2.0, 3.0, 0.0, 0.25, 5);
        double[] geometric = RandomProcesses.GeometricBrownianMotion(generator, 2.0, 0.1, 0.0, 0.5, 3);
        double[] ornstein = RandomProcesses.OrnsteinUhlenbeck(generator, -4.0, 2.0, 0.5, 0.0, 1.0, 3);
        double[] directBridge = [-2.0, 3.0];

        Assert.Multiple(() =>
        {
            Assert.That(brownian, Is.EqualTo(new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 }));
            Assert.That(bridge, Is.EqualTo(new double[] { -2.0, -1.0, 0.0, 1.0, 2.0, 3.0 }));
            Assert.That(geometric[1], Is.EqualTo(2.0 * PortableMath.Exp(0.05)));
            Assert.That(geometric[2], Is.EqualTo(geometric[1] * PortableMath.Exp(0.05)));
            Assert.That(ornstein[0], Is.EqualTo(-4.0));
            Assert.That(ornstein[^1], Is.GreaterThan(ornstein[0]));
            Assert.That(RandomProcesses.BrownianBridge(generator, -2.0, 3.0, 1.0, 0.25, 1), Is.EqualTo(directBridge));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void ZeroMeanReversionMatchesScaledDriftlessBrownianMotion()
    {
        RandomGenerator ornsteinGenerator = Create(808UL);
        RandomGenerator brownianGenerator = Create(808UL);

        double[] ornstein = RandomProcesses.OrnsteinUhlenbeck(ornsteinGenerator, 3.0, -100.0, 0.0, 0.75, 0.25, 30);
        double[] brownian = RandomProcesses.BrownianMotion(brownianGenerator, 3.0, 0.0, 0.75, 0.25, 30);

        AssertExact(ornstein, brownian);
        Assert.That(ornsteinGenerator.ExportState().ToBytes(), Is.EqualTo(brownianGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void DestinationOverloadsReplayAndInvalidRequestsAreAtomic()
    {
        RandomGenerator allocating = Create(909UL);
        RandomGenerator destinationGenerator = Create(909UL);
        double[] destination = new double[RandomProcesses.RandomWalkBatchOutputLength(3, 7)];
        double[] expected = RandomProcesses.OrnsteinUhlenbeck(allocating, 1.0, 2.0, 0.4, 0.3, 0.2, 3, 7);
        RandomProcesses.OrnsteinUhlenbeck(destinationGenerator, 1.0, 2.0, 0.4, 0.3, 0.2, 3, 7, destination);
        AssertExact(destination, expected);
        Assert.That(destinationGenerator.ExportState().ToBytes(), Is.EqualTo(allocating.ExportState().ToBytes()));

        RandomGenerator generator = Create(910UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] unchanged = [2.0, 3.0];
        AssertArgument("generator", () => RandomProcesses.BrownianIncrements(NullReference.Value<global::random.RandomGenerator>(), 1.0, 1));
        AssertArgument("timeStep", () => RandomProcesses.BrownianMotion(generator, 0.0, 0.0, 1.0, 0.0, 1));
        AssertArgument("stepCount", () => RandomProcesses.BrownianBridge(generator, 0.0, 1.0, 1.0, 1.0, 0));
        AssertArgument("initialState", () => RandomProcesses.GeometricBrownianMotion(generator, 0.0, 0.1, 0.2, 1.0, 1));
        AssertArgument("meanReversion", () => RandomProcesses.OrnsteinUhlenbeck(generator, 0.0, 1.0, -0.1, 1.0, 1.0, 1));
        AssertArgument("volatility", () => RandomProcesses.BrownianMotion(generator, 0.0, 0.0, -0.1, 1.0, 1));
        AssertArgument("destination", () => RandomProcesses.BrownianIncrements(generator, 1.0, 1, unchanged));

        Assert.Multiple(() =>
        {
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(unchanged, Is.EqualTo(new double[] { 2.0, 3.0 }));
            Assert.That(RandomProcesses.BrownianIncrementOutputLength(7), Is.EqualTo(7));
            Assert.That(RandomProcesses.BrownianIncrementBatchOutputLength(3, 7), Is.EqualTo(21));
        });
    }

    static void AssertBatch(Func<RandomGenerator, double[]> batchCall, Func<RandomGenerator, double[]> singleCall, int valuesPerPath, ulong seed)
    {
        RandomGenerator batchGenerator = Create(seed);
        RandomGenerator repeatedGenerator = Create(seed);
        double[] actual = batchCall(batchGenerator);
        double[] expected = new double[4 * valuesPerPath];
        for (int path = 0; path < 4; path++)
        {
            singleCall(repeatedGenerator).CopyTo(expected, path * valuesPerPath);
        }

        AssertExact(actual, expected);
        Assert.That(batchGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
    }

    static void AssertExact(double[] actual, double[] expected)
    {
        Assert.That(actual, Has.Length.EqualTo(expected.Length));
        for (int index = 0; index < actual.Length; index++)
        {
            Assert.That(PortableMath.DoubleBits(actual[index]), Is.EqualTo(PortableMath.DoubleBits(expected[index])), $"index {index}");
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
