namespace random.Tests;

public class BasicDiscreteSamplerTests
{
    [Test]
    public void BernoulliSamplesMatchExactProbabilityBooleans()
    {
        RandomGenerator actualGenerator = Create(42UL);
        RandomGenerator referenceGenerator = Create(42UL);
        byte[] before = actualGenerator.ExportState().ToBytes();
        BernoulliSampler sampler = new BernoulliSampler(actualGenerator, 0.375);

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        for (int index = 0; index < 257; index++)
        {
            Assert.That(sampler.Sample(), Is.EqualTo(referenceGenerator.NextBooleanWithDoubleProbability(0.375)));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void RademacherSamplesMatchGeneratorValues()
    {
        RandomGenerator actualGenerator = Create(123UL);
        RandomGenerator referenceGenerator = Create(123UL);
        RademacherSampler sampler = new RademacherSampler(actualGenerator);

        for (int index = 0; index < 257; index++)
        {
            Assert.That(sampler.Sample(), Is.EqualTo(referenceGenerator.NextRademacher()));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void InclusiveUniformMatchesBoundedUnsignedOffsets()
    {
        RandomGenerator actualGenerator = Create(456UL);
        RandomGenerator referenceGenerator = Create(456UL);
        DiscreteUniformSampler sampler = new DiscreteUniformSampler(actualGenerator, -17, 29);
        bool sawMinimum = false;
        bool sawMaximum = false;

        for (int index = 0; index < 2000; index++)
        {
            int expected = -17 + (int)referenceGenerator.NextUInt32Bounded(47U);
            int actual = sampler.Sample();
            Assert.That(actual, Is.EqualTo(expected));
            if (actual == -17)
            {
                sawMinimum = true;
            }

            if (actual == 29)
            {
                sawMaximum = true;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(sawMinimum, Is.True);
            Assert.That(sawMaximum, Is.True);
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void InclusiveUniformSupportsTheEntireSignedDomain()
    {
        RandomGenerator actualGenerator = Create(789UL);
        RandomGenerator referenceGenerator = Create(789UL);
        DiscreteUniformSampler sampler = new DiscreteUniformSampler(actualGenerator, int.MinValue, int.MaxValue);

        for (int index = 0; index < 257; index++)
        {
            uint raw = referenceGenerator.NextUInt32();
            int expected = (int)((long)int.MinValue + (long)raw);
            Assert.That(sampler.Sample(), Is.EqualTo(expected));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void FillsMatchRepeatedScalarSamplesExactly()
    {
        RandomGenerator bernoulliFillGenerator = Create(11UL);
        RandomGenerator bernoulliScalarGenerator = Create(11UL);
        BernoulliSampler bernoulliFill = new BernoulliSampler(bernoulliFillGenerator, 0.625);
        BernoulliSampler bernoulliScalar = new BernoulliSampler(bernoulliScalarGenerator, 0.625);
        bool[] bernoulliActual = new bool[129];
        bool[] bernoulliExpected = new bool[bernoulliActual.Length];
        bernoulliFill.Fill(bernoulliActual);
        for (int index = 0; index < bernoulliExpected.Length; index++)
        {
            bernoulliExpected[index] = bernoulliScalar.Sample();
        }

        RandomGenerator rademacherFillGenerator = Create(22UL);
        RandomGenerator rademacherScalarGenerator = Create(22UL);
        RademacherSampler rademacherFill = new RademacherSampler(rademacherFillGenerator);
        RademacherSampler rademacherScalar = new RademacherSampler(rademacherScalarGenerator);
        int[] rademacherActual = new int[129];
        int[] rademacherExpected = new int[rademacherActual.Length];
        rademacherFill.Fill(rademacherActual);
        for (int index = 0; index < rademacherExpected.Length; index++)
        {
            rademacherExpected[index] = rademacherScalar.Sample();
        }

        RandomGenerator uniformFillGenerator = Create(33UL);
        RandomGenerator uniformScalarGenerator = Create(33UL);
        DiscreteUniformSampler uniformFill = new DiscreteUniformSampler(uniformFillGenerator, int.MinValue, int.MaxValue);
        DiscreteUniformSampler uniformScalar = new DiscreteUniformSampler(uniformScalarGenerator, int.MinValue, int.MaxValue);
        int[] uniformActual = new int[129];
        int[] uniformExpected = new int[uniformActual.Length];
        uniformFill.Fill(uniformActual);
        for (int index = 0; index < uniformExpected.Length; index++)
        {
            uniformExpected[index] = uniformScalar.Sample();
        }

        Assert.Multiple(() =>
        {
            Assert.That(bernoulliActual, Is.EqualTo(bernoulliExpected));
            Assert.That(bernoulliFillGenerator.ExportState().ToBytes(), Is.EqualTo(bernoulliScalarGenerator.ExportState().ToBytes()));
            Assert.That(rademacherActual, Is.EqualTo(rademacherExpected));
            Assert.That(rademacherFillGenerator.ExportState().ToBytes(), Is.EqualTo(rademacherScalarGenerator.ExportState().ToBytes()));
            Assert.That(uniformActual, Is.EqualTo(uniformExpected));
            Assert.That(uniformFillGenerator.ExportState().ToBytes(), Is.EqualTo(uniformScalarGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void DeterministicAndEmptyOperationsConsumeNoState()
    {
        RandomGenerator generator = Create(999UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        BernoulliSampler never = new BernoulliSampler(generator, 0.0);
        BernoulliSampler always = new BernoulliSampler(generator, 1.0);
        DiscreteUniformSampler point = new DiscreteUniformSampler(generator, -123, -123);
        RademacherSampler rademacher = new RademacherSampler(generator);
        bool[] neverValues = new bool[8];
        bool[] alwaysValues = new bool[8];
        int[] pointValues = new int[8];

        bool neverScalar = never.Sample();
        bool alwaysScalar = always.Sample();
        int pointScalar = point.Sample();
        never.Fill(neverValues);
        always.Fill(alwaysValues);
        point.Fill(pointValues);
        never.Fill(Span<bool>.Empty);
        rademacher.Fill(Span<int>.Empty);
        point.Fill(Span<int>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(neverScalar, Is.False);
            Assert.That(alwaysScalar, Is.True);
            Assert.That(pointScalar, Is.EqualTo(-123));
            Assert.That(neverValues, Is.All.False);
            Assert.That(alwaysValues, Is.All.True);
            Assert.That(pointValues, Is.All.EqualTo(-123));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidArgumentsAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullBernoulli = Assert.Throws<RandomArgumentException>(() => new BernoulliSampler(NullReference.Value<global::random.IRandomSource>(), 0.5));
        RandomArgumentException nullRademacher = Assert.Throws<RandomArgumentException>(() => new RademacherSampler(NullReference.Value<global::random.IRandomSource>()));
        RandomArgumentException nullUniform = Assert.Throws<RandomArgumentException>(() => new DiscreteUniformSampler(NullReference.Value<global::random.IRandomSource>(), 0, 1));
        Assert.Multiple(() =>
        {
            Assert.That(nullBernoulli.ArgumentName(), Is.EqualTo("generator"));
            Assert.That(nullRademacher.ArgumentName(), Is.EqualTo("generator"));
            Assert.That(nullUniform.ArgumentName(), Is.EqualTo("generator"));
        });

        RandomGenerator generator = Create(321UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new BernoulliSampler(generator, -double.Epsilon)), "probability" },
            new object[] { (TestDelegate)(() => new BernoulliSampler(generator, PortableMath.NextUp(1.0))), "probability" },
            new object[] { (TestDelegate)(() => new BernoulliSampler(generator, double.NaN)), "probability" },
            new object[] { (TestDelegate)(() => new BernoulliSampler(generator, double.PositiveInfinity)), "probability" },
            new object[] { (TestDelegate)(() => new DiscreteUniformSampler(generator, 2, 1)), "maximum" }
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
