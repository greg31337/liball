namespace random.Tests;

public class MultivariateSamplerBatchTests
{
    delegate void DoubleFill(int sampleCount, Span<double> destination);
    delegate void DoubleSampleInto(Span<double> destination);
    delegate void IntFill(int sampleCount, Span<int> destination);
    delegate void IntSampleInto(Span<int> destination);

    [Test]
    public void DirichletBatchMatchesRepeatedSamples()
    {
        RandomGenerator batchGenerator = Create(101UL);
        RandomGenerator repeatedGenerator = Create(101UL);
        IDirichletSampler batch = new DirichletSampler(batchGenerator, [0.5, 2.0, 4.0]);
        IDirichletSampler repeated = new DirichletSampler(repeatedGenerator, [0.5, 2.0, 4.0]);

        AssertDoubleBatch(
            batchGenerator,
            repeatedGenerator,
            batch.RequiredBatchOutputLength,
            batch.Fill,
            repeated.SampleInto,
            3);
    }

    [Test]
    public void MultinomialBatchMatchesRepeatedSamples()
    {
        RandomGenerator batchGenerator = Create(202UL);
        RandomGenerator repeatedGenerator = Create(202UL);
        IMultinomialSampler batch = new MultinomialSampler(batchGenerator, 25, [1.0, 2.0, 4.0]);
        IMultinomialSampler repeated = new MultinomialSampler(repeatedGenerator, 25, [1.0, 2.0, 4.0]);

        AssertIntBatch(
            batchGenerator,
            repeatedGenerator,
            batch.RequiredBatchOutputLength,
            batch.Fill,
            repeated.SampleInto,
            3);
    }

    [Test]
    public void MultivariateHypergeometricBatchMatchesRepeatedSamples()
    {
        RandomGenerator batchGenerator = Create(303UL);
        RandomGenerator repeatedGenerator = Create(303UL);
        IMultivariateHypergeometricSampler batch = new MultivariateHypergeometricSampler(batchGenerator, [10, 20, 30], 17);
        IMultivariateHypergeometricSampler repeated = new MultivariateHypergeometricSampler(repeatedGenerator, [10, 20, 30], 17);

        AssertIntBatch(
            batchGenerator,
            repeatedGenerator,
            batch.RequiredBatchOutputLength,
            batch.Fill,
            repeated.SampleInto,
            3);
    }

    [Test]
    public void MultivariateNormalBatchQueryMatchesRepeatedSamples()
    {
        RandomGenerator batchGenerator = Create(404UL);
        RandomGenerator repeatedGenerator = Create(404UL);
        double[] covariance = [2.0, 0.5, 0.5, 3.0];
        IMultivariateNormalSampler batch = new MultivariateNormalSampler(batchGenerator, [1.0, -2.0], covariance);
        IMultivariateNormalSampler repeated = new MultivariateNormalSampler(repeatedGenerator, [1.0, -2.0], covariance);

        AssertDoubleBatch(
            batchGenerator,
            repeatedGenerator,
            batch.RequiredBatchOutputLength,
            batch.Fill,
            repeated.SampleInto,
            2);
        Assert.That(batch.RequiredBatchOutputLength(11), Is.EqualTo(batch.RequiredOutputLength(11)));
    }

    [Test]
    public void WishartBatchMatchesRepeatedRowMajorMatrices()
    {
        RandomGenerator batchGenerator = Create(505UL);
        RandomGenerator repeatedGenerator = Create(505UL);
        double[] scale = [3.0, 0.5, 0.5, 2.0];
        IWishartSampler batch = new WishartSampler(batchGenerator, 8.0, scale, 2);
        IWishartSampler repeated = new WishartSampler(repeatedGenerator, 8.0, scale, 2);

        AssertDoubleBatch(
            batchGenerator,
            repeatedGenerator,
            batch.RequiredBatchOutputLength,
            batch.Fill,
            repeated.SampleInto,
            batch.RequiredOutputLength());
    }

    [Test]
    public void InverseWishartBatchMatchesRepeatedRowMajorMatrices()
    {
        RandomGenerator batchGenerator = Create(606UL);
        RandomGenerator repeatedGenerator = Create(606UL);
        double[] scale = [3.0, 0.5, 0.5, 2.0];
        IInverseWishartSampler batch = new InverseWishartSampler(batchGenerator, 8.0, scale, 2);
        IInverseWishartSampler repeated = new InverseWishartSampler(repeatedGenerator, 8.0, scale, 2);

        AssertDoubleBatch(
            batchGenerator,
            repeatedGenerator,
            batch.RequiredBatchOutputLength,
            batch.Fill,
            repeated.SampleInto,
            batch.RequiredOutputLength());
    }

    [Test]
    public void MatrixNormalBatchMatchesRepeatedRowMajorMatrices()
    {
        RandomGenerator batchGenerator = Create(707UL);
        RandomGenerator repeatedGenerator = Create(707UL);
        double[] mean = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0];
        double[] rowScale = [2.0, 0.5, 0.5, 3.0];
        double[] columnScale = [2.0, 0.25, 0.0, 0.25, 1.5, 0.2, 0.0, 0.2, 1.0];
        IMatrixNormalSampler batch = new MatrixNormalSampler(batchGenerator, mean, 2, 3, rowScale, columnScale);
        IMatrixNormalSampler repeated = new MatrixNormalSampler(repeatedGenerator, mean, 2, 3, rowScale, columnScale);

        AssertDoubleBatch(
            batchGenerator,
            repeatedGenerator,
            batch.RequiredBatchOutputLength,
            batch.Fill,
            repeated.SampleInto,
            batch.RequiredOutputLength());
    }

    [Test]
    public void NormalGammaBatchMatchesRepeatedValuePrecisionPairs()
    {
        RandomGenerator batchGenerator = Create(808UL);
        RandomGenerator repeatedGenerator = Create(808UL);
        INormalGammaSampler batch = new NormalGammaSampler(batchGenerator, 1.5, 2.0, 4.0, 3.0);
        INormalGammaSampler repeated = new NormalGammaSampler(repeatedGenerator, 1.5, 2.0, 4.0, 3.0);

        AssertDoubleBatch(
            batchGenerator,
            repeatedGenerator,
            batch.RequiredBatchOutputLength,
            batch.Fill,
            repeated.SampleInto,
            2);
    }

    static void AssertDoubleBatch(
        RandomGenerator batchGenerator,
        RandomGenerator repeatedGenerator,
        Func<int, int> requiredLength,
        DoubleFill fill,
        DoubleSampleInto sampleInto,
        int sampleLength)
    {
        const int sampleCount = 7;
        Assert.That(requiredLength(0), Is.EqualTo(0));
        Assert.That(requiredLength(sampleCount), Is.EqualTo(sampleCount * sampleLength));
        double[] actual = new double[requiredLength(sampleCount)];
        double[] expected = new double[actual.Length];

        fill(sampleCount, actual);
        for (int sample = 0; sample < sampleCount; sample++)
        {
            sampleInto(expected.AsSpan(sample * sampleLength, sampleLength));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(batchGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
        });

        byte[] beforeInvalid = batchGenerator.ExportState().ToBytes();
        double[] invalidDestination = new double[sampleLength * 2 - 1];
        Array.Fill(invalidDestination, 91.0);
        double[] unchangedDestination = (double[])invalidDestination.Clone();
        AssertArgument("sampleCount", () => requiredLength(-1));
        AssertArgument("sampleCount", () => requiredLength(int.MaxValue));
        AssertArgument("sampleCount", () => fill(-1, Span<double>.Empty));
        AssertArgument("destination", () => fill(2, invalidDestination));
        fill(0, Span<double>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(invalidDestination, Is.EqualTo(unchangedDestination));
            Assert.That(batchGenerator.ExportState().ToBytes(), Is.EqualTo(beforeInvalid));
        });
    }

    static void AssertIntBatch(
        RandomGenerator batchGenerator,
        RandomGenerator repeatedGenerator,
        Func<int, int> requiredLength,
        IntFill fill,
        IntSampleInto sampleInto,
        int sampleLength)
    {
        const int sampleCount = 7;
        Assert.That(requiredLength(0), Is.EqualTo(0));
        Assert.That(requiredLength(sampleCount), Is.EqualTo(sampleCount * sampleLength));
        int[] actual = new int[requiredLength(sampleCount)];
        int[] expected = new int[actual.Length];

        fill(sampleCount, actual);
        for (int sample = 0; sample < sampleCount; sample++)
        {
            sampleInto(expected.AsSpan(sample * sampleLength, sampleLength));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(batchGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
        });

        byte[] beforeInvalid = batchGenerator.ExportState().ToBytes();
        int[] invalidDestination = new int[sampleLength * 2 - 1];
        Array.Fill(invalidDestination, 91);
        int[] unchangedDestination = (int[])invalidDestination.Clone();
        AssertArgument("sampleCount", () => requiredLength(-1));
        AssertArgument("sampleCount", () => requiredLength(int.MaxValue));
        AssertArgument("sampleCount", () => fill(-1, Span<int>.Empty));
        AssertArgument("destination", () => fill(2, invalidDestination));
        fill(0, Span<int>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(invalidDestination, Is.EqualTo(unchangedDestination));
            Assert.That(batchGenerator.ExportState().ToBytes(), Is.EqualTo(beforeInvalid));
        });
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
