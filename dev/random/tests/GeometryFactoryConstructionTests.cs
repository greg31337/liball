namespace random.Tests;

public class GeometryFactoryConstructionTests
{
    [Test]
    public void PublicScramblerFactoriesMatchCanonicalConstructionAndReplay()
    {
        IRandomFactory factory = new RandomFactory();
        RandomGenerator[] actualGenerators = [Create(101UL), Create(102UL), Create(103UL)];
        RandomGenerator[] expectedGenerators = [Create(101UL), Create(102UL), Create(103UL)];
        DigitalScrambler[] actual =
        [
            factory.CreateDigitalShift(actualGenerators[0], 3),
            factory.CreateLinearMatrix(actualGenerators[1], 3),
            factory.CreateLinearMatrixWithDigitalShift(actualGenerators[2], 3)
        ];
        DigitalScrambler[] expected =
        [
            DigitalScrambler.CreateDigitalShift(expectedGenerators[0], 3),
            DigitalScrambler.CreateLinearMatrix(expectedGenerators[1], 3),
            DigitalScrambler.CreateLinearMatrixWithDigitalShift(expectedGenerators[2], 3)
        ];
        ulong[] values = [0UL, 1UL, 0x0123456789ABCDEFUL, ulong.MaxValue];

        for (int scramblerIndex = 0; scramblerIndex < actual.Length; scramblerIndex++)
        {
            for (int coordinate = 0; coordinate < 3; coordinate++)
            {
                foreach (ulong value in values)
                {
                    Assert.That(actual[scramblerIndex].Scramble(coordinate, value), Is.EqualTo(expected[scramblerIndex].Scramble(coordinate, value)));
                }
            }

            Assert.Multiple(() =>
            {
                Assert.That(actual[scramblerIndex].Dimension(), Is.EqualTo(3));
                Assert.That(actual[scramblerIndex].HasLinearMatrix(), Is.EqualTo(expected[scramblerIndex].HasLinearMatrix()));
                Assert.That(actual[scramblerIndex].HasDigitalShift(), Is.EqualTo(expected[scramblerIndex].HasDigitalShift()));
                Assert.That(actualGenerators[scramblerIndex].ExportState().ToBytes(), Is.EqualTo(expectedGenerators[scramblerIndex].ExportState().ToBytes()));
            });
        }

        DigitalScrambler identity = factory.CreateIdentityScrambler(3);
        Assert.Multiple(() =>
        {
            Assert.That(identity.HasLinearMatrix(), Is.False);
            Assert.That(identity.HasDigitalShift(), Is.False);
            Assert.That(identity.Scramble(2, 0xFEDCBA9876543210UL), Is.EqualTo(0xFEDCBA9876543210UL));
        });
    }

    [Test]
    public void PublicUnscrambledSequenceFactoriesMatchLifecycleReferences()
    {
        IRandomFactory factory = new RandomFactory();
        SobolSequence sobol = factory.CreateUnscrambledSobol(3);
        SobolSequence expectedSobol = SobolSequence.CreateUnscrambled(3);
        HaltonSequence halton = factory.CreateUnscrambledHalton(3);
        HaltonSequence expectedHalton = HaltonSequence.CreateUnscrambled(3);

        Assert.That(sobol.Fill(16), Is.EqualTo(expectedSobol.Fill(16)));
        Assert.That(halton.Fill(16), Is.EqualTo(expectedHalton.Fill(16)));
        sobol.Skip(17UL);
        expectedSobol.Skip(17UL);
        halton.Skip(17UL);
        expectedHalton.Skip(17UL);
        Assert.That(sobol.Next(), Is.EqualTo(expectedSobol.Next()));
        Assert.That(halton.Next(), Is.EqualTo(expectedHalton.Next()));
        sobol.Reset();
        expectedSobol.Reset();
        halton.Reset();
        expectedHalton.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(sobol.Dimension(), Is.EqualTo(3));
            Assert.That(sobol.Index(), Is.Zero);
            Assert.That(sobol.Next(), Is.EqualTo(expectedSobol.Next()));
            Assert.That(halton.Dimension(), Is.EqualTo(3));
            Assert.That(halton.Bases(), Is.EqualTo(new int[] { 2, 3, 5 }));
            Assert.That(halton.Index(), Is.Zero);
            Assert.That(halton.Next(), Is.EqualTo(expectedHalton.Next()));
        });
    }

    [Test]
    public void PublicStratifiedFactoryPathsMatchCanonicalGridsAndCopyShape()
    {
        IRandomFactory factory = new RandomFactory();
        RandomGenerator inferredGenerator = Create(201UL);
        RandomGenerator inferredReferenceGenerator = Create(201UL);
        StratifiedSampler inferred = factory.CreateInferredStratifiedSampler(inferredGenerator, 3, false);
        StratifiedSampler inferredReference = StratifiedSampler.CreateInferred(inferredReferenceGenerator, 3, false);
        int[] shape = [2, 3, 4];
        RandomGenerator configuredGenerator = Create(202UL);
        RandomGenerator configuredReferenceGenerator = Create(202UL);
        StratifiedSampler configured = factory.CreateStratifiedSamplerFromStrata(configuredGenerator, shape, false);
        StratifiedSampler configuredReference = StratifiedSampler.FromStrata(configuredReferenceGenerator, [2, 3, 4], false);
        shape[0] = 99;

        double[] inferredPoints = inferred.Sample(27);
        double[] expectedInferredPoints = inferredReference.Sample(27);
        double[] configuredPoints = configured.Sample(24);
        double[] expectedConfiguredPoints = configuredReference.Sample(24);

        Assert.Multiple(() =>
        {
            Assert.That(inferred.Dimension(), Is.EqualTo(3));
            Assert.That(inferred.ConfiguredPointCount(), Is.Zero);
            Assert.That(inferredPoints, Is.EqualTo(expectedInferredPoints));
            Assert.That(inferredGenerator.ExportState().ToBytes(), Is.EqualTo(inferredReferenceGenerator.ExportState().ToBytes()));
            Assert.That(configured.Dimension(), Is.EqualTo(3));
            Assert.That(configured.ConfiguredPointCount(), Is.EqualTo(24));
            Assert.That(configuredPoints, Is.EqualTo(expectedConfiguredPoints));
            Assert.That(configuredGenerator.ExportState().ToBytes(), Is.EqualTo(configuredReferenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void PublicPolygonTriangulationMatchesCanonicalResultWithoutMutation()
    {
        IRandomFactory factory = new RandomFactory();
        double[] vertices = [0.0, 0.0, 2.0, 0.0, 2.0, 1.0, 1.0, 1.0, 1.0, 2.0, 0.0, 2.0];
        double[] before = (double[])vertices.Clone();

        int[] actual = factory.TriangulatePolygon(vertices);
        int[] expected = PolytopeSampler.Triangulate(before);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Has.Length.EqualTo(12));
            Assert.That(actual.Distinct().Count(), Is.GreaterThanOrEqualTo(4));
            Assert.That(vertices, Is.EqualTo(before));
        });
    }

    [Test]
    public void PublicGeometryFactoryFailuresAreAtomic()
    {
        IRandomFactory factory = new RandomFactory();
        RandomGenerator generator = Create(301UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        int[] shape = [2, 0];

        AssertArgument("generator", () => factory.CreateDigitalShift(NullReference.Value<global::random.IRandomSource>(), 1));
        AssertArgument("dimension", () => factory.CreateLinearMatrix(generator, 0));
        AssertArgument("dimension", () => factory.CreateLinearMatrixWithDigitalShift(generator, 21202));
        AssertArgument("dimension", () => factory.CreateIdentityScrambler(0));
        AssertArgument("dimension", () => factory.CreateUnscrambledSobol(0));
        AssertArgument("dimension", () => factory.CreateUnscrambledHalton(0));
        AssertArgument("dimension", () => factory.CreateInferredStratifiedSampler(generator, 0, false));
        AssertArgument("strataPerDimension", () => factory.CreateStratifiedSamplerFromStrata(generator, shape, false));
        AssertArgument("vertices", () => factory.TriangulatePolygon([0.0, 0.0, 1.0, 0.0]));

        Assert.Multiple(() =>
        {
            Assert.That(shape, Is.EqualTo(new int[] { 2, 0 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }

    static void AssertArgument(string argumentName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(argumentName));
    }
}
