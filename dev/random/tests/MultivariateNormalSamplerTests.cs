namespace random.Tests;

public class MultivariateNormalSamplerTests
{
    [Test]
    public void CovarianceAndCholeskyInputsProduceIdenticalSamples()
    {
        double[] covariance = [4.0, 2.0, -2.0, 2.0, 5.0, 1.0, -2.0, 1.0, 6.0];
        double[] factor = ArrayMatrix.Cholesky(covariance, 3);
        RandomGenerator covarianceGenerator = Create(101UL);
        RandomGenerator factorGenerator = Create(101UL);
        MultivariateNormalSampler fromCovariance = new MultivariateNormalSampler(covarianceGenerator, [1.0, -2.0, 3.0], covariance);
        MultivariateNormalSampler fromFactor = MultivariateNormalSampler.FromCholesky(factorGenerator, [1.0, -2.0, 3.0], factor);

        double[] covarianceSamples = new double[fromCovariance.RequiredOutputLength(500)];
        double[] factorSamples = new double[fromFactor.RequiredOutputLength(500)];
        fromCovariance.Fill(500, covarianceSamples);
        fromFactor.Fill(500, factorSamples);

        Assert.Multiple(() =>
        {
            Assert.That(covarianceSamples, Is.EqualTo(factorSamples));
            Assert.That(covarianceGenerator.ExportState().ToBytes(), Is.EqualTo(factorGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void BatchFillUsesDeterministicRowMajorNormalThenTransformOrder()
    {
        double[] mean = [10.0, 20.0, 30.0];
        double[] factor = [2.0, 0.0, 0.0, -1.0, 3.0, 0.0, 0.5, 4.0, 5.0];
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator expectedGenerator = Create(202UL);
        MultivariateNormalSampler sampler = MultivariateNormalSampler.FromCholesky(actualGenerator, mean, factor);
        NormalSampler standardNormal = new NormalSampler(expectedGenerator, 0.0, 1.0);
        double[] actual = new double[12];
        double[] expected = new double[12];

        sampler.Fill(4, actual);
        for (int row = 0; row < 4; row++)
        {
            double first = standardNormal.Sample();
            double second = standardNormal.Sample();
            double third = standardNormal.Sample();
            int offset = row * 3;
            expected[offset] = mean[0] + factor[0] * first;
            expected[offset + 1] = mean[1] + factor[3] * first + factor[4] * second;
            expected[offset + 2] = mean[2] + factor[6] * first + factor[7] * second + factor[8] * third;
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void AllocatingDestinationAndBatchSamplesMatch()
    {
        double[] covariance = [2.0, 0.5, 0.5, 3.0];
        RandomGenerator allocatingGenerator = Create(303UL);
        RandomGenerator destinationGenerator = Create(303UL);
        RandomGenerator batchGenerator = Create(303UL);
        MultivariateNormalSampler allocating = new MultivariateNormalSampler(allocatingGenerator, [4.0, -7.0], covariance);
        MultivariateNormalSampler writing = new MultivariateNormalSampler(destinationGenerator, [4.0, -7.0], covariance);
        MultivariateNormalSampler batching = new MultivariateNormalSampler(batchGenerator, [4.0, -7.0], covariance);
        double[] batch = new double[batching.RequiredOutputLength(200)];
        batching.Fill(200, batch);

        for (int row = 0; row < 200; row++)
        {
            double[] allocated = allocating.Sample();
            double[] destination = new double[2];
            writing.SampleInto(destination);
            Assert.That(allocated, Is.EqualTo(destination));
            Assert.That(allocated, Is.EqualTo(new double[] { batch[row * 2], batch[row * 2 + 1] }));
        }

        Assert.Multiple(() =>
        {
            Assert.That(allocatingGenerator.ExportState().ToBytes(), Is.EqualTo(destinationGenerator.ExportState().ToBytes()));
            Assert.That(allocatingGenerator.ExportState().ToBytes(), Is.EqualTo(batchGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void SampleMomentsMatchConfiguredMeanAndCovariance()
    {
        const int sampleCount = 160000;
        double[] expectedMean = [1.0, -2.0, 3.0];
        double[] expectedCovariance = [4.0, 1.0, 0.5, 1.0, 3.0, -0.25, 0.5, -0.25, 2.0];
        MultivariateNormalSampler sampler = new MultivariateNormalSampler(Create(404UL), expectedMean, expectedCovariance);
        double[] sums = new double[3];
        double[] products = new double[9];
        double[] sample = new double[3];
        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            sampler.SampleInto(sample);
            for (int row = 0; row < 3; row++)
            {
                sums[row] += sample[row];
                for (int column = 0; column < 3; column++)
                {
                    products[row * 3 + column] += sample[row] * sample[column];
                }
            }
        }

        double[] actualMean = sums.Select(value => value / sampleCount).ToArray();
        Assert.Multiple(() =>
        {
            for (int row = 0; row < 3; row++)
            {
                Assert.That(actualMean[row], Is.EqualTo(expectedMean[row]).Within(0.018));
                for (int column = 0; column < 3; column++)
                {
                    double covariance = products[row * 3 + column] / sampleCount - actualMean[row] * actualMean[column];
                    Assert.That(covariance, Is.EqualTo(expectedCovariance[row * 3 + column]).Within(0.04));
                }
            }
        });
    }

    [Test]
    public void InputsAreDefensivelyCopied()
    {
        double[] mean = [1.0, 2.0];
        double[] covariance = [4.0, 1.0, 1.0, 3.0];
        double[] factor = ArrayMatrix.Cholesky(covariance, 2);
        RandomGenerator covarianceGenerator = Create(505UL);
        RandomGenerator factorGenerator = Create(505UL);
        MultivariateNormalSampler fromCovariance = new MultivariateNormalSampler(covarianceGenerator, mean, covariance);
        MultivariateNormalSampler fromFactor = MultivariateNormalSampler.FromCholesky(factorGenerator, mean, factor);

        Array.Fill(mean, double.NaN);
        Array.Fill(covariance, double.NaN);
        Array.Fill(factor, double.NaN);

        for (int index = 0; index < 100; index++)
        {
            Assert.That(fromCovariance.Sample(), Is.EqualTo(fromFactor.Sample()));
        }

        Assert.That(covarianceGenerator.ExportState().ToBytes(), Is.EqualTo(factorGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void OneDimensionAndEmptyBatchesFollowTheGeneralContract()
    {
        RandomGenerator generator = Create(606UL);
        MultivariateNormalSampler sampler = MultivariateNormalSampler.FromCholesky(generator, [7.0], [2.0]);
        RandomGenerator expectedGenerator = Create(606UL);
        double expected = 7.0 + 2.0 * new NormalSampler(expectedGenerator, 0.0, 1.0).Sample();
        Assert.That(sampler.Sample(), Is.EqualTo(new double[] { expected }));
        byte[] beforeEmpty = generator.ExportState().ToBytes();

        sampler.Fill(0, Array.Empty<double>());

        Assert.Multiple(() =>
        {
            Assert.That(sampler.Dimension(), Is.EqualTo(1));
            Assert.That(sampler.RequiredOutputLength(0), Is.EqualTo(0));
            Assert.That(sampler.RequiredOutputLength(9), Is.EqualTo(9));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(beforeEmpty));
        });
    }

    [Test]
    public void InvalidConstructionInputsAreRejectedWithoutConsumingState()
    {
        AssertArgument("generator", () => new MultivariateNormalSampler(NullReference.Value<global::random.IRandomSource>(), [0.0], [1.0]));
        AssertArgument("generator", () => MultivariateNormalSampler.FromCholesky(NullReference.Value<global::random.IRandomSource>(), [0.0], [1.0]));

        RandomGenerator generator = Create(707UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        AssertArgument("mean", () => new MultivariateNormalSampler(generator, NullReference.Value<double[]>(), [1.0]));
        AssertArgument("mean", () => new MultivariateNormalSampler(generator, [], []));
        AssertArgument("mean", () => new MultivariateNormalSampler(generator, [double.NaN], [1.0]));
        AssertArgument("covariance", () => new MultivariateNormalSampler(generator, [0.0, 0.0], [1.0]));
        AssertArgument("covariance", () => new MultivariateNormalSampler(generator, [0.0], [double.PositiveInfinity]));
        AssertArgument("covariance", () => new MultivariateNormalSampler(generator, [0.0, 0.0], [2.0, 1.0, 0.0, 2.0]));
        AssertArgument("covariance", () => new MultivariateNormalSampler(generator, [0.0, 0.0], [1.0, 1.0, 1.0, 1.0]));
        AssertArgument("choleskyFactor", () => MultivariateNormalSampler.FromCholesky(generator, [0.0, 0.0], [1.0]));
        AssertArgument("choleskyFactor", () => MultivariateNormalSampler.FromCholesky(generator, [0.0, 0.0], [1.0, double.NaN, 0.0, 1.0]));
        AssertArgument("choleskyFactor", () => MultivariateNormalSampler.FromCholesky(generator, [0.0, 0.0], [1.0, 0.25, 0.0, 1.0]));
        AssertArgument("choleskyFactor", () => MultivariateNormalSampler.FromCholesky(generator, [0.0, 0.0], [1.0, 0.0, 0.0, 0.0]));
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    [Test]
    public void InvalidOutputRequestsAreAtomic()
    {
        RandomGenerator generator = Create(808UL);
        generator.NextBoolean();
        MultivariateNormalSampler sampler = new MultivariateNormalSampler(generator, [1.0, 2.0], [2.0, 0.5, 0.5, 3.0]);
        byte[] before = generator.ExportState().ToBytes();
        double[] shortSample = [91.0];
        double[] shortBatch = [81.0, 82.0, 83.0];

        AssertArgument("destination", () => sampler.SampleInto(shortSample));
        AssertArgument("sampleCount", () => sampler.Fill(-1, Array.Empty<double>()));
        AssertArgument("destination", () => sampler.Fill(2, shortBatch));

        Assert.Multiple(() =>
        {
            Assert.That(shortSample, Is.EqualTo(new double[] { 91.0 }));
            Assert.That(shortBatch, Is.EqualTo(new double[] { 81.0, 82.0, 83.0 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
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
