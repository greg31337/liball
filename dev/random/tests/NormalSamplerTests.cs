namespace random.Tests;

public class NormalSamplerTests
{
    const double TailBoundary = 3.442619855899;

    [Test]
    public void StandardConstructorMatchesExplicitLocationAndScale()
    {
        RandomGenerator standardGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator explicitGenerator = Create(42UL, RandomMathMode.Portable);
        byte[] before = standardGenerator.ExportState().ToBytes();
        NormalSampler standard = new NormalSampler(standardGenerator, 0.0, 1.0);
        NormalSampler explicitSampler = new NormalSampler(explicitGenerator, 0.0, 1.0);

        Assert.That(standardGenerator.ExportState().ToBytes(), Is.EqualTo(before));

        for (int index = 0; index < 100; index++)
        {
            Assert.That(PortableMath.DoubleBits(standard.Sample()), Is.EqualTo(PortableMath.DoubleBits(explicitSampler.Sample())));
        }

        Assert.That(standardGenerator.ExportState().ToBytes(), Is.EqualTo(explicitGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void LocationAndScaleTransformStandardSamples()
    {
        RandomGenerator actualGenerator = Create(987UL, RandomMathMode.Portable);
        RandomGenerator referenceGenerator = Create(987UL, RandomMathMode.Portable);
        NormalSampler actual = new NormalSampler(actualGenerator, -17.25, 3.5);
        NormalSampler reference = new NormalSampler(referenceGenerator, 0.0, 1.0);

        for (int index = 0; index < 100; index++)
        {
            double expected = -17.25 + 3.5 * reference.Sample();
            Assert.That(PortableMath.DoubleBits(actual.Sample()), Is.EqualTo(PortableMath.DoubleBits(expected)));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void FillMatchesRepeatedSamplesInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator fillGenerator = Create(123456UL, mode);
            RandomGenerator scalarGenerator = Create(123456UL, mode);
            NormalSampler fillSampler = new NormalSampler(fillGenerator, 11.0, 2.75);
            NormalSampler scalarSampler = new NormalSampler(scalarGenerator, 11.0, 2.75);
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

    [Test]
    public void ZeroDeviationReturnsExactMeanWithoutConsumption()
    {
        RandomGenerator generator = Create(71UL, RandomMathMode.Portable);
        NormalSampler sampler = new NormalSampler(generator, -91.5, -0.0);
        byte[] before = generator.ExportState().ToBytes();
        double[] values = new double[16];

        double scalar = sampler.Sample();
        sampler.Fill(values);
        sampler.Fill(Span<double>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(scalar), Is.EqualTo(PortableMath.DoubleBits(-91.5)));
            Assert.That(values, Is.All.EqualTo(-91.5));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidConstructorArgumentsAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullStandard = Assert.Throws<RandomArgumentException>(() => new NormalSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0));
        RandomArgumentException nullScaled = Assert.Throws<RandomArgumentException>(() => new NormalSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0));
        Assert.Multiple(() =>
        {
            Assert.That(nullStandard.ArgumentName(), Is.EqualTo("generator"));
            Assert.That(nullScaled.ArgumentName(), Is.EqualTo("generator"));
        });

        RandomGenerator generator = Create(19UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new NormalSampler(generator, double.NaN, 1.0)), "mean" },
            new object[] { (TestDelegate)(() => new NormalSampler(generator, double.PositiveInfinity, 1.0)), "mean" },
            new object[] { (TestDelegate)(() => new NormalSampler(generator, 0.0, double.NaN)), "standardDeviation" },
            new object[] { (TestDelegate)(() => new NormalSampler(generator, 0.0, double.PositiveInfinity)), "standardDeviation" },
            new object[] { (TestDelegate)(() => new NormalSampler(generator, 0.0, -double.Epsilon)), "standardDeviation" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void FastPathHasLockedPortableAndSystemSequence()
    {
        ulong[] expected =
        [
            0x3FD5E600D607DA2BUL,
            0xBFD08CB34ADFB94AUL,
            0x3FD10F606AA7973AUL,
            0x3FB99C1C0FC8ECDCUL,
            0x3FE64DED30D6EC7EUL,
            0x3FD0E6C05069F097UL,
            0x3FF10BA33DE9E3A3UL,
            0xBFEC05627B184349UL
        ];
        NormalSampler portable = new NormalSampler(Create(42UL, RandomMathMode.Portable), 0.0, 1.0);
        NormalSampler system = new NormalSampler(Create(42UL, RandomMathMode.System), 0.0, 1.0);

        foreach (ulong expectedBits in expected)
        {
            Assert.Multiple(() =>
            {
                Assert.That(PortableMath.DoubleBits(portable.Sample()), Is.EqualTo(expectedBits));
                Assert.That(PortableMath.DoubleBits(system.Sample()), Is.EqualTo(expectedBits));
            });
        }
    }

    [Test]
    public void TailAlgorithmHasLockedPositiveAndNegativePortableFixtures()
    {
        double positive = new NormalSampler(Create(1167UL, RandomMathMode.Portable), 0.0, 1.0).Sample();
        double negative = new NormalSampler(Create(4350UL, RandomMathMode.Portable), 0.0, 1.0).Sample();

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(positive), Is.EqualTo(0x400C9BF8F1B7974FUL));
            Assert.That(PortableMath.DoubleBits(negative), Is.EqualTo(0xC00B9801454A920AUL));
            Assert.That(positive, Is.GreaterThan(TailBoundary));
            Assert.That(negative, Is.LessThan(-TailBoundary));
        });
    }

    [Test]
    public void RejectionTailUsesTheSelectedMathMode()
    {
        double portable = new NormalSampler(Create(329310UL, RandomMathMode.Portable), 0.0, 1.0).Sample();
        RandomGenerator systemGenerator = Create(329310UL, RandomMathMode.System);
        RandomGenerator referenceGenerator = Create(329310UL, RandomMathMode.System);
        double system = new NormalSampler(systemGenerator, 0.0, 1.0).Sample();
        uint firstWord = referenceGenerator.NextUInt32();
        bool negative = (firstWord & 0x80000000U) != 0U;
        double distance;
        double height;
        do
        {
            distance = -Math.Log(referenceGenerator.NextDoubleOpenOpen()) / TailBoundary;
            height = -Math.Log(referenceGenerator.NextDoubleOpenOpen());
        }
        while (distance * distance > height + height);
        double expectedSystem = negative ? -TailBoundary - distance : TailBoundary + distance;

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(portable), Is.EqualTo(0x400C14CC76AA0D9BUL));
            Assert.That(firstWord & 127U, Is.EqualTo(0U));
            Assert.That(PortableMath.DoubleBits(system), Is.EqualTo(PortableMath.DoubleBits(expectedSystem)));
            Assert.That(systemGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            Assert.That(portable, Is.GreaterThan(TailBoundary));
            Assert.That(system, Is.GreaterThan(TailBoundary));
        });
    }

    [Test]
    public void DeterministicSampleCoversBothTailsAndIsSymmetric()
    {
        const int sampleCount = 200000;
        NormalSampler sampler = new NormalSampler(Create(123UL, RandomMathMode.Portable), 0.0, 1.0);
        double sum = 0.0;
        double minimum = double.PositiveInfinity;
        double maximum = double.NegativeInfinity;
        int positiveCount = 0;

        for (int index = 0; index < sampleCount; index++)
        {
            double value = sampler.Sample();
            sum += value;
            if (value > 0.0)
            {
                positiveCount++;
            }

            if (value < minimum)
            {
                minimum = value;
            }

            if (value > maximum)
            {
                maximum = value;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(minimum, Is.LessThan(-4.0));
            Assert.That(maximum, Is.GreaterThan(4.0));
            Assert.That(sum / sampleCount, Is.EqualTo(0.0).Within(0.01));
            Assert.That(positiveCount, Is.InRange(97000, 103000));
        });
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
