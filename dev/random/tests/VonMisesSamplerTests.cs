namespace random.Tests;

public class VonMisesSamplerTests
{
    const double Pi = 3.14159265358979323846;
    const double TwoPi = 6.28318530717958647693;

    [Test]
    public void StandardConstructorMatchesExplicitZeroDirection()
    {
        RandomGenerator firstGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator secondGenerator = Create(42UL, RandomMathMode.Portable);
        VonMisesSampler first = new VonMisesSampler(firstGenerator, 0.0, 2.5);
        VonMisesSampler second = new VonMisesSampler(secondGenerator, 0.0, 2.5);

        for (int index = 0; index < 128; index++)
        {
            Assert.That(Bits(first.Sample()), Is.EqualTo(Bits(second.Sample())));
        }

        Assert.That(firstGenerator.ExportState().ToBytes(), Is.EqualTo(secondGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void ZeroAndTinyConcentrationsMatchShiftedUniformLimit()
    {
        double[] concentrations = [0.0, -0.0, double.Epsilon, 9.999999e-9];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (double concentration in concentrations)
        {
            foreach (RandomMathMode mode in modes)
            {
                RandomGenerator actualGenerator = Create(51UL, mode);
                RandomGenerator referenceGenerator = Create(51UL, mode);
                VonMisesSampler sampler = new VonMisesSampler(actualGenerator, 2.75, concentration);

                for (int index = 0; index < 128; index++)
                {
                    double offset = Pi * (2.0 * referenceGenerator.NextDouble() - 1.0);
                    double expected = Normalize(Normalize(2.75) + offset);
                    Assert.That(Bits(sampler.Sample()), Is.EqualTo(Bits(expected)));
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void BestFisherRegimesMatchReferenceConstructionInBothMathModes()
    {
        double[] concentrations = [1.0e-8, 1.0e-7, 9.999999e-6, 1.0e-5, 0.1, 2.5, 1.0e6];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (double concentration in concentrations)
        {
            foreach (RandomMathMode mode in modes)
            {
                RandomGenerator actualGenerator = Create(61UL, mode);
                RandomGenerator referenceGenerator = Create(61UL, mode);
                VonMisesSampler sampler = new VonMisesSampler(actualGenerator, -2.75, concentration);

                for (int index = 0; index < 64; index++)
                {
                    double expected = ReferenceBestFisher(referenceGenerator, -2.75, concentration, mode);
                    Assert.That(Bits(sampler.Sample()), Is.EqualTo(Bits(expected)));
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void LargeConcentrationsMatchWrappedNormalLimit()
    {
        double[] concentrations = [1000000.0000001, 1.0e12, double.MaxValue];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (double concentration in concentrations)
        {
            foreach (RandomMathMode mode in modes)
            {
                RandomGenerator actualGenerator = Create(71UL, mode);
                RandomGenerator referenceGenerator = Create(71UL, mode);
                VonMisesSampler sampler = new VonMisesSampler(actualGenerator, 3.0, concentration);
                NormalSampler normal = new NormalSampler(referenceGenerator, 0.0, 1.0);

                for (int index = 0; index < 128; index++)
                {
                    double scale = Sqrt(1.0 / concentration, mode);
                    double expected = Normalize(Normalize(3.0) + scale * normal.Sample());
                    Assert.That(Bits(sampler.Sample()), Is.EqualTo(Bits(expected)));
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void InfiniteConcentrationReturnsNormalizedDirectionWithoutConsumption()
    {
        double[] directions = [0.0, Pi, -Pi, 19.25, -19.25, double.MaxValue, -double.MaxValue];
        foreach (double direction in directions)
        {
            RandomGenerator generator = Create(81UL, RandomMathMode.Portable);
            byte[] before = generator.ExportState().ToBytes();
            VonMisesSampler sampler = new VonMisesSampler(generator, direction, double.PositiveInfinity);
            double[] values = new double[16];

            double scalar = sampler.Sample();
            sampler.Fill(values);

            Assert.Multiple(() =>
            {
                Assert.That(Bits(scalar), Is.EqualTo(Bits(Normalize(direction))));
                Assert.That(values, Is.All.EqualTo(Normalize(direction)));
                Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            });
        }
    }

    [Test]
    public void FillMatchesRepeatedSamplesAcrossAlgorithmRegimes()
    {
        double[] concentrations = [0.0, 1.0e-7, 3.0, 1.0e8, double.PositiveInfinity];
        foreach (double concentration in concentrations)
        {
            RandomGenerator fillGenerator = Create(91UL, RandomMathMode.Portable);
            RandomGenerator scalarGenerator = Create(91UL, RandomMathMode.Portable);
            VonMisesSampler fillSampler = new VonMisesSampler(fillGenerator, 1.25, concentration);
            VonMisesSampler scalarSampler = new VonMisesSampler(scalarGenerator, 1.25, concentration);
            double[] actual = new double[129];
            double[] expected = new double[actual.Length];
            byte[] before = fillGenerator.ExportState().ToBytes();

            fillSampler.Fill(Span<double>.Empty);
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(before));
            fillSampler.Fill(actual);
            for (int index = 0; index < expected.Length; index++)
            {
                expected[index] = scalarSampler.Sample();
            }

            for (int index = 0; index < actual.Length; index++)
            {
                Assert.That(Bits(actual[index]), Is.EqualTo(Bits(expected[index])));
            }

            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void InvalidParametersAreRejectedWithoutConsumption()
    {
        Assert.That(Assert.Throws<RandomArgumentException>(() => new VonMisesSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new VonMisesSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[][] cases =
        [
            [(TestDelegate)(() => new VonMisesSampler(generator, double.NaN, 1.0)), "meanDirection"],
            [(TestDelegate)(() => new VonMisesSampler(generator, double.PositiveInfinity, 1.0)), "meanDirection"],
            [(TestDelegate)(() => new VonMisesSampler(generator, double.NegativeInfinity, 1.0)), "meanDirection"],
            [(TestDelegate)(() => new VonMisesSampler(generator, 0.0, double.NaN)), "concentration"],
            [(TestDelegate)(() => new VonMisesSampler(generator, 0.0, -double.Epsilon)), "concentration"],
            [(TestDelegate)(() => new VonMisesSampler(generator, 0.0, double.NegativeInfinity)), "concentration"]
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void EveryRegimeNormalizesHugeAndBoundaryDirections()
    {
        double[] directions = [-double.MaxValue, -Pi, Pi, double.MaxValue];
        double[] concentrations = [0.0, 1.0e-7, 2.0, 1.0e8, double.PositiveInfinity];
        foreach (double direction in directions)
        {
            foreach (double concentration in concentrations)
            {
                VonMisesSampler sampler = new VonMisesSampler(Create(101UL, RandomMathMode.Portable), direction, concentration);
                for (int index = 0; index < 128; index++)
                {
                    double value = sampler.Sample();
                    Assert.That(value, Is.GreaterThanOrEqualTo(-Pi).And.LessThan(Pi));
                }
            }
        }
    }

    [Test]
    public void DeterministicCircularMomentsDistinguishUniformAndConcentratedRegimes()
    {
        const int sampleCount = 120000;
        const double direction = 1.25;
        VonMisesSampler uniform = new VonMisesSampler(Create(111UL, RandomMathMode.Portable), direction, 0.0);
        VonMisesSampler concentrated = new VonMisesSampler(Create(112UL, RandomMathMode.Portable), direction, 8.0);
        double uniformCosineSum = 0.0;
        double uniformSineSum = 0.0;
        double concentratedCosineSum = 0.0;

        for (int index = 0; index < sampleCount; index++)
        {
            double uniformDifference = Normalize(uniform.Sample() - direction);
            double concentratedDifference = Normalize(concentrated.Sample() - direction);
            uniformCosineSum += Math.Cos(uniformDifference);
            uniformSineSum += Math.Sin(uniformDifference);
            concentratedCosineSum += Math.Cos(concentratedDifference);
        }

        Assert.Multiple(() =>
        {
            Assert.That(uniformCosineSum / sampleCount, Is.EqualTo(0.0).Within(0.01));
            Assert.That(uniformSineSum / sampleCount, Is.EqualTo(0.0).Within(0.01));
            Assert.That(concentratedCosineSum / sampleCount, Is.GreaterThan(0.9));
        });
    }

    static double ReferenceBestFisher(RandomGenerator generator, double direction, double concentration, RandomMathMode mathMode)
    {
        double scale;
        if (concentration < 1.0e-5)
        {
            scale = 1.0 / concentration + concentration;
        }
        else
        {
            double intermediate = 1.0 + Sqrt(1.0 + 4.0 * concentration * concentration, mathMode);
            double rho = (intermediate - Sqrt(2.0 * intermediate, mathMode)) / (2.0 * concentration);
            scale = (1.0 + rho * rho) / (2.0 * rho);
        }

        double cosine;
        while (true)
        {
            double firstUnit = generator.NextDouble();
            double proposal = Cos(Pi * firstUnit, mathMode);
            cosine = (1.0 + scale * proposal) / (scale + proposal);
            double acceptance = concentration * (scale - cosine);
            double secondUnit = generator.NextDouble();
            if (acceptance * (2.0 - acceptance) - secondUnit >= 0.0 || Log(acceptance / secondUnit, mathMode) + 1.0 - acceptance >= 0.0)
            {
                break;
            }
        }

        if (cosine < -1.0)
        {
            cosine = -1.0;
        }
        else if (cosine > 1.0)
        {
            cosine = 1.0;
        }

        double angle = Acos(cosine, mathMode);
        if (generator.NextDouble() < 0.5)
        {
            angle = -angle;
        }

        return Normalize(Normalize(direction) + angle);
    }

    static double Normalize(double angle)
    {
        double normalized = angle % TwoPi;
        if (normalized < -Pi)
        {
            normalized += TwoPi;
        }
        else if (normalized >= Pi)
        {
            normalized -= TwoPi;
        }

        return normalized;
    }

    static double Sqrt(double value, RandomMathMode mathMode)
    {
        return mathMode == RandomMathMode.Portable ? PortableMath.Sqrt(value) : Math.Sqrt(value);
    }

    static double Cos(double value, RandomMathMode mathMode)
    {
        return mathMode == RandomMathMode.Portable ? PortableMath.Cos(value) : Math.Cos(value);
    }

    static double Acos(double value, RandomMathMode mathMode)
    {
        return mathMode == RandomMathMode.Portable ? PortableMath.Acos(value) : Math.Acos(value);
    }

    static double Log(double value, RandomMathMode mathMode)
    {
        return mathMode == RandomMathMode.Portable ? PortableMath.Log(value) : Math.Log(value);
    }

    static ulong Bits(double value)
    {
        return PortableMath.DoubleBits(value);
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
