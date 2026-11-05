namespace random.Tests;

public class WishartSamplerTests
{
    [Test]
    public void WishartUsesDocumentedRowMajorBartlettOrder()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(101UL, mode);
            RandomGenerator expectedGenerator = Create(101UL, mode);
            WishartSampler sampler = new WishartSampler(actualGenerator, 6.5, [4.0, 2.0, 2.0, 5.0], 2);
            ChiSampler firstDiagonal = new ChiSampler(expectedGenerator, 6.5);
            ChiSampler secondDiagonal = new ChiSampler(expectedGenerator, 5.5);
            NormalSampler normal = new NormalSampler(expectedGenerator, 0.0, 1.0);

            double first = firstDiagonal.Sample();
            double below = normal.Sample();
            double second = secondDiagonal.Sample();
            double transformed00 = 2.0 * first;
            double transformed10 = first + 2.0 * below;
            double transformed11 = 2.0 * second;
            double[] expected =
            [
                transformed00 * transformed00,
                transformed00 * transformed10,
                transformed00 * transformed10,
                transformed10 * transformed10 + transformed11 * transformed11
            ];

            Assert.Multiple(() =>
            {
                Assert.That(sampler.Sample(), Is.EqualTo(expected));
                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void InverseWishartUsesTheSameBartlettOrderAndTriangularSolve()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(202UL, mode);
            RandomGenerator expectedGenerator = Create(202UL, mode);
            InverseWishartSampler sampler = new InverseWishartSampler(actualGenerator, 7.5, [4.0, 2.0, 2.0, 5.0], 2);
            ChiSampler firstDiagonal = new ChiSampler(expectedGenerator, 7.5);
            ChiSampler secondDiagonal = new ChiSampler(expectedGenerator, 6.5);
            NormalSampler normal = new NormalSampler(expectedGenerator, 0.0, 1.0);

            double first = firstDiagonal.Sample();
            double below = normal.Sample();
            double second = secondDiagonal.Sample();
            double transformed00 = 2.0 / first;
            double transformed01 = -below * transformed00 / second;
            double transformed10 = 1.0 / first;
            double transformed11 = (2.0 - below * transformed10) / second;
            double[] expected =
            [
                transformed00 * transformed00 + transformed01 * transformed01,
                transformed00 * transformed10 + transformed01 * transformed11,
                transformed00 * transformed10 + transformed01 * transformed11,
                transformed10 * transformed10 + transformed11 * transformed11
            ];

            Assert.Multiple(() =>
            {
                Assert.That(sampler.Sample(), Is.EqualTo(expected));
                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void OneDimensionalSamplesReduceToScaledChiSquareLaws()
    {
        RandomGenerator wishartGenerator = Create(303UL, RandomMathMode.Portable);
        RandomGenerator wishartExpectedGenerator = Create(303UL, RandomMathMode.Portable);
        RandomGenerator inverseGenerator = Create(404UL, RandomMathMode.Portable);
        RandomGenerator inverseExpectedGenerator = Create(404UL, RandomMathMode.Portable);
        WishartSampler wishart = new WishartSampler(wishartGenerator, 5.0, [9.0], 1);
        InverseWishartSampler inverse = new InverseWishartSampler(inverseGenerator, 5.0, [9.0], 1);
        double wishartExpected = 9.0 * new ChiSquareSampler(wishartExpectedGenerator, 5.0).Sample();
        double inverseExpected = 9.0 / new ChiSquareSampler(inverseExpectedGenerator, 5.0).Sample();

        Assert.Multiple(() =>
        {
            Assert.That(wishart.Sample()[0], Is.EqualTo(wishartExpected).Within(1.0e-14));
            Assert.That(inverse.Sample()[0], Is.EqualTo(inverseExpected).Within(1.0e-14));
            Assert.That(wishartGenerator.ExportState().ToBytes(), Is.EqualTo(wishartExpectedGenerator.ExportState().ToBytes()));
            Assert.That(inverseGenerator.ExportState().ToBytes(), Is.EqualTo(inverseExpectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void AllocatingAndDestinationSamplesMatch()
    {
        double[] scale = [3.0, 0.5, 0.5, 2.0];
        WishartSampler wishartAllocating = new WishartSampler(Create(505UL, RandomMathMode.Portable), 8.0, scale, 2);
        WishartSampler wishartWriting = new WishartSampler(Create(505UL, RandomMathMode.Portable), 8.0, scale, 2);
        InverseWishartSampler inverseAllocating = new InverseWishartSampler(Create(606UL, RandomMathMode.Portable), 8.0, scale, 2);
        InverseWishartSampler inverseWriting = new InverseWishartSampler(Create(606UL, RandomMathMode.Portable), 8.0, scale, 2);

        for (int index = 0; index < 500; index++)
        {
            double[] wishartDestination = new double[4];
            double[] inverseDestination = new double[4];
            wishartWriting.SampleInto(wishartDestination);
            inverseWriting.SampleInto(inverseDestination);
            Assert.That(wishartAllocating.Sample(), Is.EqualTo(wishartDestination));
            Assert.That(inverseAllocating.Sample(), Is.EqualTo(inverseDestination));
        }
    }

    [Test]
    public void SamplesAreExactlySymmetricAndPositiveDefinite()
    {
        double[] scale = [4.0, 1.0, -0.5, 1.0, 3.0, 0.25, -0.5, 0.25, 2.0];
        WishartSampler wishart = new WishartSampler(Create(707UL, RandomMathMode.Portable), 9.0, scale, 3);
        InverseWishartSampler inverse = new InverseWishartSampler(Create(808UL, RandomMathMode.Portable), 9.0, scale, 3);

        for (int index = 0; index < 500; index++)
        {
            AssertPositiveDefinite(wishart.Sample(), 3);
            AssertPositiveDefinite(inverse.Sample(), 3);
        }
    }

    [Test]
    public void SampleMeansMatchWishartParameterizations()
    {
        const int sampleCount = 160000;
        const double degreesOfFreedom = 8.0;
        double[] scale = [2.0, 0.5, 0.5, 1.5];
        WishartSampler wishart = new WishartSampler(Create(909UL, RandomMathMode.Portable), degreesOfFreedom, scale, 2);
        InverseWishartSampler inverse = new InverseWishartSampler(Create(1001UL, RandomMathMode.Portable), degreesOfFreedom, scale, 2);
        double[] wishartSums = new double[4];
        double[] inverseSums = new double[4];
        double[] destination = new double[4];
        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            wishart.SampleInto(destination);
            Add(wishartSums, destination);
            inverse.SampleInto(destination);
            Add(inverseSums, destination);
        }

        double inverseDivisor = degreesOfFreedom - 3.0;
        Assert.Multiple(() =>
        {
            for (int index = 0; index < scale.Length; index++)
            {
                Assert.That(wishartSums[index] / sampleCount, Is.EqualTo(degreesOfFreedom * scale[index]).Within(0.055));
                Assert.That(inverseSums[index] / sampleCount, Is.EqualTo(scale[index] / inverseDivisor).Within(0.0045));
            }
        });
    }

    [Test]
    public void ScaleInputsAreDefensivelyCopied()
    {
        double[] wishartScale = [4.0, 1.0, 1.0, 3.0];
        double[] inverseScale = [4.0, 1.0, 1.0, 3.0];
        WishartSampler wishartActual = new WishartSampler(Create(1102UL, RandomMathMode.Portable), 8.0, wishartScale, 2);
        WishartSampler wishartExpected = new WishartSampler(Create(1102UL, RandomMathMode.Portable), 8.0, [4.0, 1.0, 1.0, 3.0], 2);
        InverseWishartSampler inverseActual = new InverseWishartSampler(Create(1203UL, RandomMathMode.Portable), 8.0, inverseScale, 2);
        InverseWishartSampler inverseExpected = new InverseWishartSampler(Create(1203UL, RandomMathMode.Portable), 8.0, [4.0, 1.0, 1.0, 3.0], 2);
        Array.Fill(wishartScale, double.NaN);
        Array.Fill(inverseScale, double.NaN);

        for (int index = 0; index < 100; index++)
        {
            Assert.That(wishartActual.Sample(), Is.EqualTo(wishartExpected.Sample()));
            Assert.That(inverseActual.Sample(), Is.EqualTo(inverseExpected.Sample()));
        }
    }

    [Test]
    public void InvalidConstructionInputsAreRejectedWithoutConsumingState()
    {
        AssertArgument("generator", () => new WishartSampler(NullReference.Value<global::random.IRandomSource>(), 2.0, [1.0], 1));
        AssertArgument("generator", () => new InverseWishartSampler(NullReference.Value<global::random.IRandomSource>(), 2.0, [1.0], 1));

        RandomGenerator generator = Create(1304UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        AssertArgument("degreesOfFreedom", () => new WishartSampler(generator, double.NaN, [1.0], 1));
        AssertArgument("degreesOfFreedom", () => new WishartSampler(generator, double.PositiveInfinity, [1.0], 1));
        AssertArgument("degreesOfFreedom", () => new WishartSampler(generator, 1.0, [1.0, 0.0, 0.0, 1.0], 2));
        AssertArgument("dimension", () => new WishartSampler(generator, 2.0, [], 0));
        AssertArgument("scale", () => new WishartSampler(generator, 2.0, NullReference.Value<double[]>(), 1));
        AssertArgument("scale", () => new WishartSampler(generator, 3.0, [1.0], 2));
        AssertArgument("scale", () => new WishartSampler(generator, 3.0, [1.0, double.NaN, double.NaN, 1.0], 2));
        AssertArgument("scale", () => new InverseWishartSampler(generator, 3.0, [2.0, 1.0, 0.0, 2.0], 2));
        AssertArgument("scale", () => new InverseWishartSampler(generator, 3.0, [1.0, 1.0, 1.0, 1.0], 2));
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    [Test]
    public void InvalidDestinationsAreAtomicAndMetadataIsExact()
    {
        RandomGenerator wishartGenerator = Create(1405UL, RandomMathMode.Portable);
        RandomGenerator inverseGenerator = Create(1506UL, RandomMathMode.Portable);
        WishartSampler wishart = new WishartSampler(wishartGenerator, 6.0, [2.0, 0.5, 0.5, 3.0], 2);
        InverseWishartSampler inverse = new InverseWishartSampler(inverseGenerator, 6.0, [2.0, 0.5, 0.5, 3.0], 2);
        byte[] wishartBefore = wishartGenerator.ExportState().ToBytes();
        byte[] inverseBefore = inverseGenerator.ExportState().ToBytes();
        double[] wishartDestination = [91.0, 92.0, 93.0];
        double[] inverseDestination = [81.0, 82.0, 83.0, 84.0, 85.0];

        AssertArgument("destination", () => wishart.SampleInto(wishartDestination));
        AssertArgument("destination", () => inverse.SampleInto(inverseDestination));

        Assert.Multiple(() =>
        {
            Assert.That(wishart.Dimension(), Is.EqualTo(2));
            Assert.That(wishart.RequiredOutputLength(), Is.EqualTo(4));
            Assert.That(inverse.Dimension(), Is.EqualTo(2));
            Assert.That(inverse.RequiredOutputLength(), Is.EqualTo(4));
            Assert.That(wishartDestination, Is.EqualTo(new double[] { 91.0, 92.0, 93.0 }));
            Assert.That(inverseDestination, Is.EqualTo(new double[] { 81.0, 82.0, 83.0, 84.0, 85.0 }));
            Assert.That(wishartGenerator.ExportState().ToBytes(), Is.EqualTo(wishartBefore));
            Assert.That(inverseGenerator.ExportState().ToBytes(), Is.EqualTo(inverseBefore));
        });
    }

    static void Add(double[] sums, double[] values)
    {
        for (int index = 0; index < sums.Length; index++)
        {
            sums[index] += values[index];
        }
    }

    static void AssertPositiveDefinite(double[] matrix, int dimension)
    {
        Assert.That(ArrayMatrix.IsSymmetric(matrix, dimension), Is.True);
        Assert.That(matrix.All(double.IsFinite), Is.True);
        Assert.DoesNotThrow(() => ArrayMatrix.Cholesky(matrix, dimension));
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
