namespace random.Tests;

public class NormalGammaSamplerTests
{
    [Test]
    public void SamplingMatchesGammaThenConditionalNormalComposition()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(101UL, mode);
            RandomGenerator expectedGenerator = Create(101UL, mode);
            NormalGammaSampler sampler = new NormalGammaSampler(actualGenerator, 1.25, 3.5, 4.5, 2.25);
            GammaSampler gamma = new GammaSampler(expectedGenerator, 4.5, 1.0);
            NormalSampler normal = new NormalSampler(expectedGenerator, 0.0, 1.0);
            double precision = gamma.Sample() / 2.25;
            double rootMeanPrecision = mode == RandomMathMode.Portable ? PortableMath.Sqrt(3.5) : Math.Sqrt(3.5);
            double rootPrecision = mode == RandomMathMode.Portable ? PortableMath.Sqrt(precision) : Math.Sqrt(precision);
            double expectedValue = 1.25 + normal.Sample() / (rootMeanPrecision * rootPrecision);
            double[] destination = new double[2];

            sampler.SampleInto(destination);

            Assert.Multiple(() =>
            {
                Assert.That(destination, Is.EqualTo(new double[] { expectedValue, precision }));
                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void AllocatingAndDestinationSamplesMatchAndUseValuePrecisionLayout()
    {
        RandomGenerator allocatingGenerator = Create(202UL, RandomMathMode.Portable);
        RandomGenerator destinationGenerator = Create(202UL, RandomMathMode.Portable);
        NormalGammaSampler allocating = new NormalGammaSampler(allocatingGenerator, -3.0, 2.0, 5.0, 4.0);
        NormalGammaSampler writing = new NormalGammaSampler(destinationGenerator, -3.0, 2.0, 5.0, 4.0);

        for (int index = 0; index < 1000; index++)
        {
            double[] destination = new double[2];
            writing.SampleInto(destination);
            double[] actual = allocating.Sample();
            Assert.That(actual, Is.EqualTo(destination));
            Assert.That(actual[1], Is.GreaterThanOrEqualTo(0.0));
        }

        Assert.Multiple(() =>
        {
            Assert.That(allocating.ComponentCount(), Is.EqualTo(2));
            Assert.That(allocatingGenerator.ExportState().ToBytes(), Is.EqualTo(destinationGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void JointMomentsMatchShapeRateParameterization()
    {
        const int sampleCount = 200000;
        const double location = 1.5;
        const double meanPrecision = 3.0;
        const double shape = 5.0;
        const double rate = 2.0;
        NormalGammaSampler sampler = new NormalGammaSampler(
            Create(303UL, RandomMathMode.Portable),
            location,
            meanPrecision,
            shape,
            rate);
        double valueSum = 0.0;
        double valueSquareSum = 0.0;
        double precisionSum = 0.0;
        double precisionSquareSum = 0.0;
        double weightedDeviationSquareSum = 0.0;
        double[] sample = new double[2];
        for (int index = 0; index < sampleCount; index++)
        {
            sampler.SampleInto(sample);
            double deviation = sample[0] - location;
            valueSum += sample[0];
            valueSquareSum += sample[0] * sample[0];
            precisionSum += sample[1];
            precisionSquareSum += sample[1] * sample[1];
            weightedDeviationSquareSum += deviation * deviation * sample[1];
        }

        double valueMean = valueSum / sampleCount;
        double precisionMean = precisionSum / sampleCount;
        double valueVariance = valueSquareSum / sampleCount - valueMean * valueMean;
        double precisionVariance = precisionSquareSum / sampleCount - precisionMean * precisionMean;
        Assert.Multiple(() =>
        {
            Assert.That(valueMean, Is.EqualTo(location).Within(0.004));
            Assert.That(precisionMean, Is.EqualTo(shape / rate).Within(0.015));
            Assert.That(valueVariance, Is.EqualTo(rate / (meanPrecision * (shape - 1.0))).Within(0.005));
            Assert.That(precisionVariance, Is.EqualTo(shape / (rate * rate)).Within(0.025));
            Assert.That(weightedDeviationSquareSum / sampleCount, Is.EqualTo(1.0 / meanPrecision).Within(0.004));
        });
    }

    [Test]
    public void ExtremePositiveParametersFollowDocumentedRepresentationalLimits()
    {
        NormalGammaSampler infinitePrecision = new NormalGammaSampler(
            Create(404UL, RandomMathMode.Portable),
            7.0,
            double.MaxValue,
            2.0,
            double.Epsilon);
        double[] infinite = infinitePrecision.Sample();
        NormalGammaSampler zeroPrecision = new NormalGammaSampler(
            Create(505UL, RandomMathMode.Portable),
            7.0,
            1.0,
            double.Epsilon,
            double.MaxValue);
        double[] zero = zeroPrecision.Sample();

        Assert.Multiple(() =>
        {
            Assert.That(infinite[0], Is.EqualTo(7.0));
            Assert.That(infinite[1], Is.EqualTo(double.PositiveInfinity));
            Assert.That(zero[1], Is.EqualTo(0.0));
            Assert.That(double.IsInfinity(zero[0]), Is.True);
        });
    }

    [Test]
    public void InvalidParametersAreRejectedWithoutConsumingState()
    {
        AssertArgument("generator", () => new NormalGammaSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, 1.0, 1.0));
        RandomGenerator generator = Create(606UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        AssertArgument("location", () => new NormalGammaSampler(generator, double.NaN, 1.0, 1.0, 1.0));
        AssertArgument("location", () => new NormalGammaSampler(generator, double.PositiveInfinity, 1.0, 1.0, 1.0));
        AssertArgument("meanPrecision", () => new NormalGammaSampler(generator, 0.0, 0.0, 1.0, 1.0));
        AssertArgument("meanPrecision", () => new NormalGammaSampler(generator, 0.0, -1.0, 1.0, 1.0));
        AssertArgument("meanPrecision", () => new NormalGammaSampler(generator, 0.0, double.NaN, 1.0, 1.0));
        AssertArgument("shape", () => new NormalGammaSampler(generator, 0.0, 1.0, 0.0, 1.0));
        AssertArgument("shape", () => new NormalGammaSampler(generator, 0.0, 1.0, double.NegativeInfinity, 1.0));
        AssertArgument("rate", () => new NormalGammaSampler(generator, 0.0, 1.0, 1.0, 0.0));
        AssertArgument("rate", () => new NormalGammaSampler(generator, 0.0, 1.0, 1.0, double.PositiveInfinity));
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    [Test]
    public void InvalidDestinationIsAtomic()
    {
        RandomGenerator generator = Create(707UL, RandomMathMode.Portable);
        NormalGammaSampler sampler = new NormalGammaSampler(generator, 0.0, 1.0, 2.0, 3.0);
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = [91.0];

        AssertArgument("destination", () => sampler.SampleInto(destination));

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(new double[] { 91.0 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mathMode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mathMode);
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
