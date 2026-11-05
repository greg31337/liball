namespace random.Tests;

public class MatrixNormalSamplerTests
{
    [Test]
    public void CovarianceAndFactorPathsUseDocumentedRowMajorTraversal()
    {
        double[] mean = [10.0, 20.0, 30.0, 40.0, 50.0, 60.0];
        double[] rowScale = [4.0, 2.0, 2.0, 10.0];
        double[] columnScale = [1.0, 2.0, -1.0, 2.0, 8.0, 0.0, -1.0, 0.0, 11.0];
        double[] rowFactor = [2.0, 0.0, 1.0, 3.0];
        double[] columnFactor = [1.0, 0.0, 0.0, 2.0, 2.0, 0.0, -1.0, 1.0, 3.0];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator covarianceGenerator = Create(101UL, mode);
            RandomGenerator factorGenerator = Create(101UL, mode);
            RandomGenerator expectedGenerator = Create(101UL, mode);
            MatrixNormalSampler fromCovariance = new MatrixNormalSampler(covarianceGenerator, mean, 2, 3, rowScale, columnScale);
            MatrixNormalSampler fromFactor = MatrixNormalSampler.FromCholesky(factorGenerator, mean, 2, 3, rowFactor, columnFactor);
            NormalSampler normal = new NormalSampler(expectedGenerator, 0.0, 1.0);
            double[] standard = new double[6];
            normal.Fill(standard);

            double first00 = 2.0 * standard[0];
            double first01 = 2.0 * standard[1];
            double first02 = 2.0 * standard[2];
            double second00 = standard[0] + 3.0 * standard[3];
            double second01 = standard[1] + 3.0 * standard[4];
            double second02 = standard[2] + 3.0 * standard[5];
            double[] expected =
            [
                mean[0] + first00,
                mean[1] + 2.0 * first00 + 2.0 * first01,
                mean[2] - first00 + first01 + 3.0 * first02,
                mean[3] + second00,
                mean[4] + 2.0 * second00 + 2.0 * second01,
                mean[5] - second00 + second01 + 3.0 * second02
            ];
            double[] covarianceDestination = new double[6];
            fromCovariance.SampleInto(covarianceDestination);

            Assert.Multiple(() =>
            {
                Assert.That(covarianceDestination, Is.EqualTo(expected));
                Assert.That(fromFactor.Sample(), Is.EqualTo(expected));
                Assert.That(covarianceGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
                Assert.That(factorGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void AllocatingAndDestinationSamplesMatchForRectangularMatrices()
    {
        double[] mean = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0];
        double[] rowScale = [2.0, 0.25, 0.25, 1.5];
        double[] columnScale = [1.0, 0.2, -0.1, 0.2, 1.5, 0.3, -0.1, 0.3, 2.0];
        MatrixNormalSampler allocating = new MatrixNormalSampler(Create(202UL, RandomMathMode.Portable), mean, 2, 3, rowScale, columnScale);
        MatrixNormalSampler writing = new MatrixNormalSampler(Create(202UL, RandomMathMode.Portable), mean, 2, 3, rowScale, columnScale);

        for (int index = 0; index < 1000; index++)
        {
            double[] destination = new double[6];
            writing.SampleInto(destination);
            Assert.That(allocating.Sample(), Is.EqualTo(destination));
        }
    }

    [Test]
    public void SampleMeansAndCovariancesMatchMatrixNormalLaw()
    {
        const int sampleCount = 160000;
        double[] expectedMean = [1.0, -2.0, 3.0, 4.0];
        double[] rowScale = [2.0, 0.5, 0.5, 1.5];
        double[] columnScale = [1.0, 0.25, 0.25, 0.8];
        MatrixNormalSampler sampler = new MatrixNormalSampler(
            Create(303UL, RandomMathMode.Portable),
            expectedMean,
            2,
            2,
            rowScale,
            columnScale);
        double[] sums = new double[4];
        double[] products = new double[16];
        double[] sample = new double[4];
        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            sampler.SampleInto(sample);
            for (int first = 0; first < 4; first++)
            {
                sums[first] += sample[first];
                for (int second = 0; second < 4; second++)
                {
                    products[first * 4 + second] += sample[first] * sample[second];
                }
            }
        }

        double[] actualMean = sums.Select(value => value / sampleCount).ToArray();
        Assert.Multiple(() =>
        {
            for (int first = 0; first < 4; first++)
            {
                Assert.That(actualMean[first], Is.EqualTo(expectedMean[first]).Within(0.015));
                int firstRow = first / 2;
                int firstColumn = first % 2;
                for (int second = 0; second < 4; second++)
                {
                    int secondRow = second / 2;
                    int secondColumn = second % 2;
                    double expectedCovariance = rowScale[firstRow * 2 + secondRow] * columnScale[firstColumn * 2 + secondColumn];
                    double actualCovariance = products[first * 4 + second] / sampleCount - actualMean[first] * actualMean[second];
                    Assert.That(actualCovariance, Is.EqualTo(expectedCovariance).Within(0.035));
                }
            }
        });
    }

    [Test]
    public void MeanScalesAndFactorsAreDefensivelyCopied()
    {
        double[] mean = [1.0, 2.0, 3.0, 4.0];
        double[] rowScale = [4.0, 2.0, 2.0, 10.0];
        double[] columnScale = [1.0, 2.0, 2.0, 8.0];
        double[] rowFactor = [2.0, 0.0, 1.0, 3.0];
        double[] columnFactor = [1.0, 0.0, 2.0, 2.0];
        MatrixNormalSampler covarianceActual = new MatrixNormalSampler(Create(404UL, RandomMathMode.Portable), mean, 2, 2, rowScale, columnScale);
        MatrixNormalSampler covarianceExpected = new MatrixNormalSampler(Create(404UL, RandomMathMode.Portable), [1.0, 2.0, 3.0, 4.0], 2, 2, [4.0, 2.0, 2.0, 10.0], [1.0, 2.0, 2.0, 8.0]);
        MatrixNormalSampler factorActual = MatrixNormalSampler.FromCholesky(Create(505UL, RandomMathMode.Portable), mean, 2, 2, rowFactor, columnFactor);
        MatrixNormalSampler factorExpected = MatrixNormalSampler.FromCholesky(Create(505UL, RandomMathMode.Portable), [1.0, 2.0, 3.0, 4.0], 2, 2, [2.0, 0.0, 1.0, 3.0], [1.0, 0.0, 2.0, 2.0]);
        Array.Fill(mean, double.NaN);
        Array.Fill(rowScale, double.NaN);
        Array.Fill(columnScale, double.NaN);
        Array.Fill(rowFactor, double.NaN);
        Array.Fill(columnFactor, double.NaN);

        for (int index = 0; index < 100; index++)
        {
            Assert.That(covarianceActual.Sample(), Is.EqualTo(covarianceExpected.Sample()));
            Assert.That(factorActual.Sample(), Is.EqualTo(factorExpected.Sample()));
        }
    }

    [Test]
    public void InvalidScaleMatricesAreRejectedWithoutConsumingState()
    {
        AssertArgument("generator", () => new MatrixNormalSampler(NullReference.Value<global::random.IRandomSource>(), [0.0], 1, 1, [1.0], [1.0]));
        RandomGenerator generator = Create(606UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        AssertArgument("mean", () => new MatrixNormalSampler(generator, NullReference.Value<double[]>(), 1, 1, [1.0], [1.0]));
        AssertArgument("rows", () => new MatrixNormalSampler(generator, [], 0, 1, [], [1.0]));
        AssertArgument("columns", () => new MatrixNormalSampler(generator, [], 1, 0, [1.0], []));
        AssertArgument("mean", () => new MatrixNormalSampler(generator, [0.0], 1, 2, [1.0], [1.0, 0.0, 0.0, 1.0]));
        AssertArgument("mean", () => new MatrixNormalSampler(generator, [double.NaN], 1, 1, [1.0], [1.0]));
        AssertArgument("rowScale", () => new MatrixNormalSampler(generator, [0.0, 0.0], 2, 1, NullReference.Value<double[]>(), [1.0]));
        AssertArgument("rowScale", () => new MatrixNormalSampler(generator, [0.0, 0.0], 2, 1, [1.0], [1.0]));
        AssertArgument("rowScale", () => new MatrixNormalSampler(generator, [0.0, 0.0], 2, 1, [1.0, double.NaN, double.NaN, 1.0], [1.0]));
        AssertArgument("rowScale", () => new MatrixNormalSampler(generator, [0.0, 0.0], 2, 1, [2.0, 1.0, 0.0, 2.0], [1.0]));
        AssertArgument("rowScale", () => new MatrixNormalSampler(generator, [0.0, 0.0], 2, 1, [1.0, 1.0, 1.0, 1.0], [1.0]));
        AssertArgument("columnScale", () => new MatrixNormalSampler(generator, [0.0, 0.0], 1, 2, [1.0], NullReference.Value<double[]>()));
        AssertArgument("columnScale", () => new MatrixNormalSampler(generator, [0.0, 0.0], 1, 2, [1.0], [2.0, 1.0, 0.0, 2.0]));
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    [Test]
    public void InvalidScaleFactorsAreRejectedWithoutConsumingState()
    {
        AssertArgument("generator", () => MatrixNormalSampler.FromCholesky(NullReference.Value<global::random.IRandomSource>(), [0.0], 1, 1, [1.0], [1.0]));
        RandomGenerator generator = Create(707UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        AssertArgument("rowScaleFactor", () => MatrixNormalSampler.FromCholesky(generator, [0.0, 0.0], 2, 1, NullReference.Value<double[]>(), [1.0]));
        AssertArgument("rowScaleFactor", () => MatrixNormalSampler.FromCholesky(generator, [0.0, 0.0], 2, 1, [1.0], [1.0]));
        AssertArgument("rowScaleFactor", () => MatrixNormalSampler.FromCholesky(generator, [0.0, 0.0], 2, 1, [1.0, double.NaN, 0.0, 1.0], [1.0]));
        AssertArgument("rowScaleFactor", () => MatrixNormalSampler.FromCholesky(generator, [0.0, 0.0], 2, 1, [1.0, 0.25, 0.0, 1.0], [1.0]));
        AssertArgument("rowScaleFactor", () => MatrixNormalSampler.FromCholesky(generator, [0.0, 0.0], 2, 1, [1.0, 0.0, 0.0, 0.0], [1.0]));
        AssertArgument("columnScaleFactor", () => MatrixNormalSampler.FromCholesky(generator, [0.0, 0.0], 1, 2, [1.0], NullReference.Value<double[]>()));
        AssertArgument("columnScaleFactor", () => MatrixNormalSampler.FromCholesky(generator, [0.0, 0.0], 1, 2, [1.0], [1.0, 0.5, 0.0, 1.0]));
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    [Test]
    public void InvalidDestinationIsAtomicAndShapeMetadataIsExact()
    {
        RandomGenerator generator = Create(808UL, RandomMathMode.Portable);
        MatrixNormalSampler sampler = new MatrixNormalSampler(
            generator,
            [1.0, 2.0, 3.0, 4.0, 5.0, 6.0],
            2,
            3,
            [2.0, 0.25, 0.25, 1.5],
            [1.0, 0.2, -0.1, 0.2, 1.5, 0.3, -0.1, 0.3, 2.0]);
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = [91.0, 92.0, 93.0, 94.0, 95.0];

        AssertArgument("destination", () => sampler.SampleInto(destination));

        Assert.Multiple(() =>
        {
            Assert.That(sampler.RowCount(), Is.EqualTo(2));
            Assert.That(sampler.ColumnCount(), Is.EqualTo(3));
            Assert.That(sampler.RequiredOutputLength(), Is.EqualTo(6));
            Assert.That(destination, Is.EqualTo(new double[] { 91.0, 92.0, 93.0, 94.0, 95.0 }));
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
