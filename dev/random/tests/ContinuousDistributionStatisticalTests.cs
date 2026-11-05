namespace random.Tests;

public class ContinuousDistributionStatisticalTests
{
    [Test]
    public void EveryContinuousSamplerStaysWithinItsSupportForFixedSeeds()
    {
        AssertSupport("beta", new BetaSampler(Create(101UL), 2.0, 5.0).Sample, value => value >= 0.0 && value <= 1.0);
        AssertSupport("scaled beta", new BetaScaledSampler(Create(102UL), 2.0, 5.0, -3.0, 4.0).Sample, value => value >= -3.0 && value <= 4.0);
        AssertSupport("Burr", new BurrSampler(Create(103UL), -3.0, 2.0, 1.5, 3.0).Sample, value => value >= -3.0 && double.IsFinite(value));
        AssertSupport("Cauchy", new CauchySampler(Create(104UL), -1.0, 2.0).Sample, double.IsFinite);
        AssertSupport("chi", new ChiSampler(Create(105UL), 4.0).Sample, value => value >= 0.0 && double.IsFinite(value));
        AssertSupport("chi-square", new ChiSquareSampler(Create(106UL), 4.0).Sample, value => value >= 0.0 && double.IsFinite(value));
        AssertSupport("Erlang", new ErlangSampler(Create(107UL), 3, 2.0).Sample, value => value >= 0.0 && double.IsFinite(value));
        AssertSupport("exponential", new ExponentialSampler(Create(108UL), 2.0).Sample, value => value >= 0.0 && double.IsFinite(value));
        AssertSupport("F", new FisherSnedecorSampler(Create(109UL), 5.0, 9.0).Sample, value => value >= 0.0 && double.IsFinite(value));
        AssertSupport("gamma", new GammaSampler(Create(110UL), 3.0, 2.0).Sample, value => value >= 0.0 && double.IsFinite(value));
        AssertSupport("generalized normal", new GeneralizedNormalSampler(Create(111UL), -1.0, 2.0, 1.5).Sample, double.IsFinite);
        AssertSupport("Gumbel", new GumbelSampler(Create(112UL), -1.0, 2.0).Sample, double.IsFinite);
        AssertSupport("inverse gamma", new InverseGammaSampler(Create(113UL), 4.0, 2.0).Sample, value => value > 0.0 && double.IsFinite(value));
        AssertSupport("inverse Gaussian", new InverseGaussianSampler(Create(114UL), 2.0, 5.0).Sample, value => value > 0.0 && double.IsFinite(value));
        AssertSupport("Laplace", new LaplaceSampler(Create(115UL), -1.0, 2.0).Sample, double.IsFinite);
        AssertSupport("Levy", new LevySampler(Create(116UL), -3.0, 2.0).Sample, value => value >= -3.0 && double.IsFinite(value));
        AssertSupport("log-normal", new LogNormalSampler(Create(117UL), 0.5, 1.0).Sample, value => value >= 0.0 && double.IsFinite(value));
        AssertSupport("logistic", new LogisticSampler(Create(118UL), -1.0, 2.0).Sample, double.IsFinite);
        AssertSupport("Lomax", new LomaxSampler(Create(119UL), -3.0, 2.0, 3.0).Sample, value => value >= -3.0 && double.IsFinite(value));
        AssertSupport("noncentral chi-square", new NoncentralChiSquareSampler(Create(120UL), 4.0, 3.0).Sample, value => value >= 0.0 && double.IsFinite(value));
        AssertSupport("noncentral F", new NoncentralFSampler(Create(121UL), 5.0, 10.0, 3.0).Sample, value => value >= 0.0 && double.IsFinite(value));
        AssertSupport("normal", new NormalSampler(Create(122UL), -1.0, 2.0).Sample, double.IsFinite);
        AssertSupport("Pareto", new ParetoSampler(Create(123UL), 2.0, 3.0).Sample, value => value >= 2.0 && double.IsFinite(value));
        AssertSupport("power", new PowerSampler(Create(124UL), -3.0, 5.0, 2.0).Sample, value => value >= -3.0 && value <= 2.0);
        AssertSupport("Rayleigh", new RayleighSampler(Create(125UL), -3.0, 2.0).Sample, value => value >= -3.0 && double.IsFinite(value));
        AssertSupport("skewed generalized error", new SkewedGeneralizedErrorSampler(Create(126UL), -1.0, 2.0, 1.5, 1.7).Sample, double.IsFinite);
        AssertSupport("skewed generalized t", new SkewedGeneralizedTSampler(Create(127UL), -1.0, 2.0, 1.5, 4.0, 1.7).Sample, double.IsFinite);
        AssertSupport("stable", new StableSampler(Create(128UL), -1.0, 2.0, 1.5, 0.4).Sample, double.IsFinite);
        AssertSupport("Student t", new StudentTSampler(Create(129UL), 5.0).Sample, double.IsFinite);
        AssertSupport("triangular", new TriangularSampler(Create(130UL), -3.0, 4.0, 1.0).Sample, value => value >= -3.0 && value <= 4.0);
        AssertSupport("truncated normal", new TruncatedNormalSampler(Create(131UL), 1.0, 2.0, -3.0, 4.0).Sample, value => value >= -3.0 && value <= 4.0);
        AssertSupport("uniform", new UniformSampler(Create(132UL), -3.0, 4.0).Sample, value => value >= -3.0 && value < 4.0);
        AssertSupport("von Mises", new VonMisesSampler(Create(133UL), 1.0, 2.0).Sample, value => value >= -Math.PI && value < Math.PI);
        AssertSupport("Weibull", new WeibullSampler(Create(134UL), -3.0, 2.0, 1.5).Sample, value => value >= -3.0 && double.IsFinite(value));
    }

    [Test]
    public void FixedSeedMeansMatchFiniteMomentTheoryWithConservativeTolerances()
    {
        const int sampleCount = 30000;

        AssertMean("uniform", new UniformSampler(Create(201UL), -2.0, 4.0).Sample, sampleCount, 1.0, 0.04);
        AssertMean("normal", new NormalSampler(Create(202UL), 3.0, 2.0).Sample, sampleCount, 3.0, 0.05);
        AssertMean("exponential", new ExponentialSampler(Create(203UL), 2.0).Sample, sampleCount, 2.0, 0.05);
        AssertMean("gamma", new GammaSampler(Create(204UL), 3.0, 2.0).Sample, sampleCount, 6.0, 0.10);
        AssertMean("beta", new BetaSampler(Create(205UL), 2.0, 5.0).Sample, sampleCount, 2.0 / 7.0, 0.01);
        AssertMean("Laplace", new LaplaceSampler(Create(206UL), -1.0, 2.0).Sample, sampleCount, -1.0, 0.08);
        AssertMean("logistic", new LogisticSampler(Create(207UL), 1.5, 0.75).Sample, sampleCount, 1.5, 0.05);
        AssertMean("triangular", new TriangularSampler(Create(208UL), -2.0, 4.0, 1.0).Sample, sampleCount, 1.0, 0.04);
        AssertMean("unit Weibull with shape one", new WeibullSampler(Create(209UL), 0.0, 1.0, 1.0).Sample, sampleCount, 1.0, 0.04);
        AssertMean("Rayleigh", new RayleighSampler(Create(210UL), 0.0, 1.0).Sample, sampleCount, Math.Sqrt(Math.PI / 2.0), 0.03);
        AssertMean("power", new PowerSampler(Create(211UL), 0.0, 1.0, 3.0).Sample, sampleCount, 0.75, 0.02);
        AssertMean("inverse Gaussian", new InverseGaussianSampler(Create(212UL), 2.0, 5.0).Sample, sampleCount, 2.0, 0.06);
    }

    [Test]
    public void ClosedFormCdfsPopulateEveryDecileWithinWideFixedSeedBounds()
    {
        const int sampleCount = 20000;

        UniformSampler uniform = new UniformSampler(Create(301UL), -2.0, 4.0);
        AssertDeciles("uniform", uniform.Sample, value => (value + 2.0) / 6.0, sampleCount);

        ExponentialSampler exponential = new ExponentialSampler(Create(302UL), 2.0);
        AssertDeciles("exponential", exponential.Sample, value => 1.0 - Math.Exp(-value / 2.0), sampleCount);

        LogisticSampler logistic = new LogisticSampler(Create(303UL), -1.0, 1.5);
        AssertDeciles("logistic", logistic.Sample, value => 1.0 / (1.0 + Math.Exp(-(value + 1.0) / 1.5)), sampleCount);

        GumbelSampler gumbel = new GumbelSampler(Create(304UL), 0.5, 2.0);
        AssertDeciles("Gumbel", gumbel.Sample, value => Math.Exp(-Math.Exp(-(value - 0.5) / 2.0)), sampleCount);

        CauchySampler cauchy = new CauchySampler(Create(305UL), -1.0, 2.0);
        AssertDeciles("Cauchy", cauchy.Sample, value => 0.5 + Math.Atan((value + 1.0) / 2.0) / Math.PI, sampleCount);

        NormalSampler normal = new NormalSampler(Create(306UL), 1.0, 2.0);
        AssertDeciles("normal", normal.Sample, value => NormalCdf((value - 1.0) / 2.0), sampleCount);
    }

    [Test]
    public void FixedSeedHeavyTailFamiliesReachRemoteTailRegions()
    {
        const int sampleCount = 50000;
        NormalSampler normal = new NormalSampler(Create(401UL), 0.0, 1.0);
        CauchySampler cauchy = new CauchySampler(Create(402UL), 0.0, 1.0);
        StudentTSampler student = new StudentTSampler(Create(403UL), 2.0);
        ParetoSampler pareto = new ParetoSampler(Create(404UL), 1.0, 2.0);
        LevySampler levy = new LevySampler(Create(405UL), 0.0, 1.0);
        StableSampler stable = new StableSampler(Create(406UL), 0.0, 1.0, 1.2, 0.0);
        LogNormalSampler logNormal = new LogNormalSampler(Create(407UL), 0.0, 2.0);
        bool negativeNormal = false;
        bool positiveNormal = false;
        bool remoteCauchy = false;
        bool remoteStudent = false;
        bool remotePareto = false;
        bool remoteLevy = false;
        bool remoteStable = false;
        bool remoteLogNormal = false;

        for (int index = 0; index < sampleCount; index++)
        {
            double normalValue = normal.Sample();
            negativeNormal |= normalValue < -3.5;
            positiveNormal |= normalValue > 3.5;
            remoteCauchy |= Math.Abs(cauchy.Sample()) > 100.0;
            remoteStudent |= Math.Abs(student.Sample()) > 20.0;
            remotePareto |= pareto.Sample() > 20.0;
            remoteLevy |= levy.Sample() > 100.0;
            remoteStable |= Math.Abs(stable.Sample()) > 100.0;
            remoteLogNormal |= logNormal.Sample() > 100.0;
        }

        Assert.Multiple(() =>
        {
            Assert.That(negativeNormal, Is.True);
            Assert.That(positiveNormal, Is.True);
            Assert.That(remoteCauchy, Is.True);
            Assert.That(remoteStudent, Is.True);
            Assert.That(remotePareto, Is.True);
            Assert.That(remoteLevy, Is.True);
            Assert.That(remoteStable, Is.True);
            Assert.That(remoteLogNormal, Is.True);
        });
    }

    [Test]
    public void DegenerateContinuousConfigurationsAreExactAndConsumeNoState()
    {
        RandomGenerator generator = Create(501UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] actual =
        [
            new NormalSampler(generator, -9.0, 0.0).Sample(),
            new CauchySampler(generator, -9.0, 0.0).Sample(),
            new LaplaceSampler(generator, -9.0, 0.0).Sample(),
            new LogisticSampler(generator, -9.0, 0.0).Sample(),
            new GumbelSampler(generator, -9.0, 0.0).Sample(),
            new GeneralizedNormalSampler(generator, -9.0, 0.0, 1.5).Sample(),
            new SkewedGeneralizedErrorSampler(generator, -9.0, 0.0, 1.5, 2.0).Sample(),
            new SkewedGeneralizedTSampler(generator, -9.0, 0.0, 1.5, 4.0, 2.0).Sample(),
            new StableSampler(generator, -9.0, 0.0, 1.5, 0.5).Sample(),
            new ExponentialSampler(generator, 0.0).Sample(),
            new GammaSampler(generator, 2.0, 0.0).Sample(),
            new ErlangSampler(generator, 3, 0.0).Sample(),
            new BetaScaledSampler(generator, 2.0, 5.0, -9.0, -9.0).Sample(),
            new UniformSampler(generator, -9.0, -9.0).Sample(),
            new TriangularSampler(generator, -9.0, -9.0, -9.0).Sample(),
            new TruncatedNormalSampler(generator, 0.0, 1.0, -9.0, -9.0).Sample(),
            new WeibullSampler(generator, -9.0, 0.0, 1.5).Sample(),
            new RayleighSampler(generator, -9.0, 0.0).Sample(),
            new PowerSampler(generator, -9.0, 0.0, 1.5).Sample(),
            new BurrSampler(generator, -9.0, 0.0, 1.5, 3.0).Sample(),
            new LevySampler(generator, -9.0, 0.0).Sample(),
            new InverseGammaSampler(generator, 3.0, 0.0).Sample(),
            new LomaxSampler(generator, -9.0, 0.0, 3.0).Sample(),
            new LogNormalSampler(generator, 0.0, 0.0).Sample(),
            new VonMisesSampler(generator, 0.5, double.PositiveInfinity).Sample()
        ];
        double[] expected =
        [
            -9.0, -9.0, -9.0, -9.0, -9.0, -9.0, -9.0, -9.0, -9.0,
            0.0, 0.0, 0.0, -9.0, -9.0, -9.0, -9.0, -9.0, -9.0, -9.0,
            -9.0, -9.0, 0.0, -9.0, 1.0, 0.5
        ];

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void RepresentativeComplexFillsAreBitExactWithRepeatedScalars()
    {
        AssertFillReplay(
            generator => new NormalSampler(generator, -1.0, 2.0),
            (sampler, values) => sampler.Fill(values),
            sampler => sampler.Sample(),
            601UL);
        AssertFillReplay(
            generator => new BetaSampler(generator, 0.25, 0.75),
            (sampler, values) => sampler.Fill(values),
            sampler => sampler.Sample(),
            602UL);
        AssertFillReplay(
            generator => new TruncatedNormalSampler(generator, 0.0, 1.0, 3.0, 4.0),
            (sampler, values) => sampler.Fill(values),
            sampler => sampler.Sample(),
            603UL);
        AssertFillReplay(
            generator => new StableSampler(generator, -1.0, 2.0, 1.3, 0.4),
            (sampler, values) => sampler.Fill(values),
            sampler => sampler.Sample(),
            604UL);
        AssertFillReplay(
            generator => new VonMisesSampler(generator, 1.0, 2.0),
            (sampler, values) => sampler.Fill(values),
            sampler => sampler.Sample(),
            605UL);
        AssertFillReplay(
            generator => new InverseGaussianSampler(generator, 2.0, 5.0),
            (sampler, values) => sampler.Fill(values),
            sampler => sampler.Sample(),
            606UL);
    }

    static void AssertSupport(string name, Func<double> sample, Func<double, bool> predicate)
    {
        for (int index = 0; index < 512; index++)
        {
            double value = sample();
            Assert.That(predicate(value), Is.True, $"{name} produced {value:R} at draw {index}");
        }
    }

    static void AssertMean(string name, Func<double> sample, int sampleCount, double expected, double tolerance)
    {
        double sum = 0.0;
        for (int index = 0; index < sampleCount; index++)
        {
            sum += sample();
        }

        Assert.That(sum / sampleCount, Is.EqualTo(expected).Within(tolerance), name);
    }

    static void AssertDeciles(string name, Func<double> sample, Func<double, double> cdf, int sampleCount)
    {
        int[] counts = new int[10];
        for (int index = 0; index < sampleCount; index++)
        {
            double probability = cdf(sample());
            int bin = (int)(probability * counts.Length);
            if (bin < 0)
            {
                bin = 0;
            }
            else if (bin >= counts.Length)
            {
                bin = counts.Length - 1;
            }

            counts[bin]++;
        }

        int expected = sampleCount / counts.Length;
        int tolerance = expected / 8;
        for (int bin = 0; bin < counts.Length; bin++)
        {
            Assert.That(counts[bin], Is.InRange(expected - tolerance, expected + tolerance), $"{name} decile {bin}");
        }
    }

    static void AssertFillReplay<TSampler>(
        Func<RandomGenerator, TSampler> createSampler,
        Action<TSampler, double[]> fill,
        Func<TSampler, double> sample,
        ulong seed)
    {
        RandomGenerator fillGenerator = Create(seed);
        RandomGenerator scalarGenerator = Create(seed);
        TSampler fillSampler = createSampler(fillGenerator);
        TSampler scalarSampler = createSampler(scalarGenerator);
        double[] actual = new double[257];
        double[] expected = new double[actual.Length];
        fill(fillSampler, actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = sample(scalarSampler);
        }

        Assert.Multiple(() =>
        {
            for (int index = 0; index < actual.Length; index++)
            {
                Assert.That(PortableMath.DoubleBits(actual[index]), Is.EqualTo(PortableMath.DoubleBits(expected[index])));
            }

            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        });
    }

    static double NormalCdf(double value)
    {
        double absolute = Math.Abs(value);
        double scale = 1.0 / (1.0 + 0.2316419 * absolute);
        double polynomial = scale * (0.319381530 + scale * (-0.356563782 + scale * (1.781477937 + scale * (-1.821255978 + scale * 1.330274429))));
        double density = Math.Exp(-0.5 * absolute * absolute) / Math.Sqrt(2.0 * Math.PI);
        double upper = 1.0 - density * polynomial;
        if (value < 0.0)
        {
            return 1.0 - upper;
        }

        return upper;
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
