namespace random.Tests;

public class BetaSamplerTests
{
    [Test]
    public void FillMatchesRepeatedSamplesAcrossRegimesAndMathModes()
    {
        double[][] shapes = [[0.25, 0.75], [2.0, 5.0], [0.001, 1000.0]];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (double[] pair in shapes)
        {
            foreach (RandomMathMode mode in modes)
            {
                RandomGenerator fillGenerator = Create(123456UL, mode);
                RandomGenerator scalarGenerator = Create(123456UL, mode);
                BetaSampler fillSampler = new BetaSampler(fillGenerator, pair[0], pair[1]);
                BetaSampler scalarSampler = new BetaSampler(scalarGenerator, pair[0], pair[1]);
                double[] actual = new double[257];
                double[] expected = new double[actual.Length];
                byte[] beforeEmptyFill = fillGenerator.ExportState().ToBytes();

                fillSampler.Fill(Span<double>.Empty);
                Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(beforeEmptyFill));
                fillSampler.Fill(actual);
                for (int index = 0; index < expected.Length; index++)
                {
                    expected[index] = scalarSampler.Sample();
                }

                for (int index = 0; index < actual.Length; index++)
                {
                    Assert.That(PortableMath.DoubleBits(actual[index]), Is.EqualTo(PortableMath.DoubleBits(expected[index])));
                }

                Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void InvalidConstructorArgumentsAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullError = Assert.Throws<RandomArgumentException>(() => new BetaSampler(NullReference.Value<global::random.IRandomSource>(), 1.0, 1.0));
        Assert.That(nullError.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new BetaSampler(generator, double.NaN, 1.0)), "alpha" },
            new object[] { (TestDelegate)(() => new BetaSampler(generator, double.NegativeInfinity, 1.0)), "alpha" },
            new object[] { (TestDelegate)(() => new BetaSampler(generator, double.PositiveInfinity, 1.0)), "alpha" },
            new object[] { (TestDelegate)(() => new BetaSampler(generator, 0.0, 1.0)), "alpha" },
            new object[] { (TestDelegate)(() => new BetaSampler(generator, -double.Epsilon, 1.0)), "alpha" },
            new object[] { (TestDelegate)(() => new BetaSampler(generator, 1.0, double.NaN)), "beta" },
            new object[] { (TestDelegate)(() => new BetaSampler(generator, 1.0, double.PositiveInfinity)), "beta" },
            new object[] { (TestDelegate)(() => new BetaSampler(generator, 1.0, 0.0)), "beta" },
            new object[] { (TestDelegate)(() => new BetaSampler(generator, 1.0, -double.Epsilon)), "beta" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void MinimumSymmetricShapesRemainDefinedAndBalanced()
    {
        BetaSampler sampler = new BetaSampler(Create(42UL, RandomMathMode.Portable), double.Epsilon, double.Epsilon);
        int zeroCount = 0;
        int oneCount = 0;
        const int sampleCount = 10000;

        for (int index = 0; index < sampleCount; index++)
        {
            double value = sampler.Sample();
            if (value == 0.0)
            {
                zeroCount++;
            }
            else if (value == 1.0)
            {
                oneCount++;
            }
            else
            {
                Assert.That(value, Is.EqualTo(0.5));
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(zeroCount, Is.InRange(4500, 5500));
            Assert.That(oneCount, Is.InRange(4500, 5500));
        });
    }

    [Test]
    public void LargeAndHighlyAsymmetricShapesAvoidInvalidRatios()
    {
        RandomGenerator symmetricGenerator = Create(42UL, RandomMathMode.Portable);
        BetaSampler symmetric = new BetaSampler(symmetricGenerator, double.MaxValue, double.MaxValue);
        BetaSampler high = new BetaSampler(Create(42UL, RandomMathMode.Portable), double.MaxValue, double.Epsilon);
        BetaSampler low = new BetaSampler(Create(42UL, RandomMathMode.Portable), double.Epsilon, double.MaxValue);

        for (int index = 0; index < 32; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(PortableMath.DoubleBits(symmetric.Sample()), Is.EqualTo(0x3FE0000000000000UL));
                Assert.That(PortableMath.DoubleBits(high.Sample()), Is.EqualTo(0x3FF0000000000000UL));
                Assert.That(PortableMath.DoubleBits(low.Sample()), Is.EqualTo(0UL));
            });
        }
    }

    [Test]
    public void DeterministicMomentsCoverSmallLargeAndAsymmetricShapes()
    {
        double[][] shapes = [[0.25, 0.75], [2.0, 5.0], [100.0, 2.0]];
        double[] tolerances = [0.01, 0.01, 0.003];
        const int sampleCount = 100000;

        for (int shapeIndex = 0; shapeIndex < shapes.Length; shapeIndex++)
        {
            double alpha = shapes[shapeIndex][0];
            double beta = shapes[shapeIndex][1];
            BetaSampler sampler = new BetaSampler(Create(123UL, RandomMathMode.Portable), alpha, beta);
            double sum = 0.0;
            for (int index = 0; index < sampleCount; index++)
            {
                double value = sampler.Sample();
                Assert.That(value, Is.InRange(0.0, 1.0));
                sum += value;
            }

            double expected = alpha / (alpha + beta);
            Assert.That(sum / sampleCount, Is.EqualTo(expected).Within(tolerances[shapeIndex]));
        }
    }

    [Test]
    public void PortableSequencesLockSmallAndGammaRatioAlgorithms()
    {
        ulong[] smallExpected =
        [
            0x3FDA673331D0E690UL,
            0x3FBC8780510812D2UL,
            0x3FECE0477A56A2DCUL,
            0x3F888B9A7559C110UL
        ];
        ulong[] largeExpected =
        [
            0x3FD28B5F68B724A1UL,
            0x3FD15777FE27D1FEUL,
            0x3FC757BD69AD7BA7UL,
            0x3FD2D1B6FD9C4103UL
        ];
        BetaSampler small = new BetaSampler(Create(42UL, RandomMathMode.Portable), 0.25, 0.75);
        BetaSampler large = new BetaSampler(Create(42UL, RandomMathMode.Portable), 2.0, 5.0);

        foreach (ulong expectedBits in smallExpected)
        {
            Assert.That(PortableMath.DoubleBits(small.Sample()), Is.EqualTo(expectedBits));
        }

        foreach (ulong expectedBits in largeExpected)
        {
            Assert.That(PortableMath.DoubleBits(large.Sample()), Is.EqualTo(expectedBits));
        }
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
