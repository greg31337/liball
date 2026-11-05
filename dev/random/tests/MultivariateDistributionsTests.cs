namespace random.Tests;

public class MultivariateDistributionsTests
{
    [Test]
    public void OutputLengthMetadataChecksShapesAndOverflow()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MultivariateDistributions.RequiredVectorOutputLength(0, 3), Is.EqualTo(0));
            Assert.That(MultivariateDistributions.RequiredVectorOutputLength(4, 3), Is.EqualTo(12));
            Assert.That(MultivariateDistributions.RequiredMatrixOutputLength(3, 2, 4), Is.EqualTo(24));
        });

        AssertArgument("sampleCount", () => MultivariateDistributions.RequiredVectorOutputLength(-1, 2));
        AssertArgument("componentCount", () => MultivariateDistributions.RequiredVectorOutputLength(1, 0));
        AssertArgument("sampleCount", () => MultivariateDistributions.RequiredVectorOutputLength(int.MaxValue, 2));
        AssertArgument("rows", () => MultivariateDistributions.RequiredMatrixOutputLength(1, 0, 2));
        AssertArgument("columns", () => MultivariateDistributions.RequiredMatrixOutputLength(1, 2, 0));
        AssertArgument("rows", () => MultivariateDistributions.RequiredMatrixOutputLength(1, int.MaxValue, 2));
    }

    [Test]
    public void SingleSampleFacadeMethodsMatchReusableSamplers()
    {
        AssertSingleDirichlet();
        AssertSingleMultivariateNormal();
        AssertSingleMultivariateNormalFactor();
        AssertSingleWishart();
        AssertSingleInverseWishart();
        AssertSingleMatrixNormal();
        AssertSingleMatrixNormalFactor();
        AssertSingleNormalGamma();
    }

    [Test]
    public void BatchFacadeMethodsMatchRepeatedSamplerCalls()
    {
        AssertBatchDirichlet();
        AssertBatchMultivariateNormal();
        AssertBatchMultivariateNormalFactor();
        AssertBatchWishart();
        AssertBatchInverseWishart();
        AssertBatchMatrixNormal();
        AssertBatchMatrixNormalFactor();
        AssertBatchNormalGamma();
    }

    [Test]
    public void DestinationsMayAliasInputArrays()
    {
        RandomGenerator dirichletActualGenerator = Create(1701UL);
        RandomGenerator dirichletExpectedGenerator = Create(1701UL);
        double[] concentrationsAndDestination = [0.5, 1.5, 3.0];
        double[] expectedDirichlet = new DirichletSampler(dirichletExpectedGenerator, (double[])concentrationsAndDestination.Clone()).Sample();
        MultivariateDistributions.Dirichlet(dirichletActualGenerator, concentrationsAndDestination, concentrationsAndDestination);
        AssertEquivalent(concentrationsAndDestination, expectedDirichlet, dirichletActualGenerator, dirichletExpectedGenerator);

        RandomGenerator normalActualGenerator = Create(1702UL);
        RandomGenerator normalExpectedGenerator = Create(1702UL);
        double[] meanAndDestination = [1.0, 2.0];
        double[] covariance = [2.0, 0.5, 0.5, 3.0];
        double[] expectedNormal = new MultivariateNormalSampler(normalExpectedGenerator, (double[])meanAndDestination.Clone(), covariance).Sample();
        MultivariateDistributions.MultivariateNormal(normalActualGenerator, meanAndDestination, covariance, meanAndDestination);
        AssertEquivalent(meanAndDestination, expectedNormal, normalActualGenerator, normalExpectedGenerator);

        RandomGenerator wishartActualGenerator = Create(1703UL);
        RandomGenerator wishartExpectedGenerator = Create(1703UL);
        double[] wishartScaleAndDestination = [2.0, 0.5, 0.5, 3.0];
        double[] expectedWishart = new WishartSampler(wishartExpectedGenerator, 6.0, (double[])wishartScaleAndDestination.Clone(), 2).Sample();
        MultivariateDistributions.Wishart(wishartActualGenerator, 6.0, wishartScaleAndDestination, 2, wishartScaleAndDestination);
        AssertEquivalent(wishartScaleAndDestination, expectedWishart, wishartActualGenerator, wishartExpectedGenerator);

        RandomGenerator inverseActualGenerator = Create(1704UL);
        RandomGenerator inverseExpectedGenerator = Create(1704UL);
        double[] inverseScaleAndDestination = [2.0, 0.5, 0.5, 3.0];
        double[] expectedInverse = new InverseWishartSampler(inverseExpectedGenerator, 6.0, (double[])inverseScaleAndDestination.Clone(), 2).Sample();
        MultivariateDistributions.InverseWishart(inverseActualGenerator, 6.0, inverseScaleAndDestination, 2, inverseScaleAndDestination);
        AssertEquivalent(inverseScaleAndDestination, expectedInverse, inverseActualGenerator, inverseExpectedGenerator);

        RandomGenerator matrixActualGenerator = Create(1705UL);
        RandomGenerator matrixExpectedGenerator = Create(1705UL);
        double[] matrixMeanAndDestination = [1.0, 2.0, 3.0, 4.0];
        double[] rowScale = [2.0, 0.5, 0.5, 3.0];
        double[] columnScale = [1.0, 0.25, 0.25, 1.5];
        double[] expectedMatrix = new MatrixNormalSampler(matrixExpectedGenerator, (double[])matrixMeanAndDestination.Clone(), 2, 2, rowScale, columnScale).Sample();
        MultivariateDistributions.MatrixNormal(matrixActualGenerator, matrixMeanAndDestination, 2, 2, rowScale, columnScale, matrixMeanAndDestination);
        AssertEquivalent(matrixMeanAndDestination, expectedMatrix, matrixActualGenerator, matrixExpectedGenerator);

        RandomGenerator factorActualGenerator = Create(1706UL);
        RandomGenerator factorExpectedGenerator = Create(1706UL);
        double[] rowFactorAndDestination = [2.0, 0.0, 1.0, 3.0];
        double[] columnFactor = [1.0, 0.0, 0.5, 2.0];
        double[] expectedFactorMatrix = MatrixNormalSampler.FromCholesky(factorExpectedGenerator, [1.0, 2.0, 3.0, 4.0], 2, 2, (double[])rowFactorAndDestination.Clone(), columnFactor).Sample();
        MultivariateDistributions.MatrixNormalFromCholesky(factorActualGenerator, [1.0, 2.0, 3.0, 4.0], 2, 2, rowFactorAndDestination, columnFactor, rowFactorAndDestination);
        AssertEquivalent(rowFactorAndDestination, expectedFactorMatrix, factorActualGenerator, factorExpectedGenerator);
    }

    [Test]
    public void InvalidAndEmptyBatchRequestsAreAtomic()
    {
        RandomGenerator generator = Create(1801UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = [91.0, 92.0, 93.0];

        AssertArgument("sampleCount", () => MultivariateDistributions.FillDirichlet(generator, [1.0, 2.0], -1, Array.Empty<double>()));
        AssertArgument("destination", () => MultivariateDistributions.FillWishart(generator, 5.0, [2.0, 0.5, 0.5, 3.0], 2, 1, destination));
        AssertArgument("destination", () => MultivariateDistributions.FillMatrixNormal(generator, [0.0, 0.0], 1, 2, [1.0], [1.0, 0.0, 0.0, 1.0], 2, destination));
        Assert.That(destination, Is.EqualTo(new double[] { 91.0, 92.0, 93.0 }));
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));

        MultivariateDistributions.FillDirichlet(generator, [1.0, 2.0], 0, Array.Empty<double>());
        MultivariateDistributions.FillWishart(generator, 5.0, [2.0, 0.5, 0.5, 3.0], 2, 0, Array.Empty<double>());
        MultivariateDistributions.FillMatrixNormal(generator, [0.0, 0.0], 1, 2, [1.0], [1.0, 0.0, 0.0, 1.0], 0, Array.Empty<double>());
        MultivariateDistributions.FillNormalGamma(generator, 0.0, 1.0, 2.0, 3.0, 0, Array.Empty<double>());
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    [Test]
    public void FacadePreservesSamplerMatrixShapeValidation()
    {
        RandomGenerator generator = Create(1901UL);
        byte[] before = generator.ExportState().ToBytes();
        AssertArgument("scale", () => MultivariateDistributions.Wishart(generator, 5.0, [1.0], 2, new double[4]));
        AssertArgument("rowScale", () => MultivariateDistributions.MatrixNormal(generator, [0.0, 0.0], 2, 1, [1.0], [1.0], new double[2]));
        AssertArgument("columnScaleFactor", () => MultivariateDistributions.MatrixNormalFromCholesky(generator, [0.0, 0.0], 1, 2, [1.0], [1.0], new double[2]));
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    static void AssertSingleDirichlet()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator expectedGenerator = Create(101UL);
        double[] actual = new double[3];
        MultivariateDistributions.Dirichlet(actualGenerator, [0.5, 1.5, 3.0], actual);
        AssertEquivalent(actual, new DirichletSampler(expectedGenerator, [0.5, 1.5, 3.0]).Sample(), actualGenerator, expectedGenerator);
    }

    static void AssertSingleMultivariateNormal()
    {
        RandomGenerator actualGenerator = Create(102UL);
        RandomGenerator expectedGenerator = Create(102UL);
        double[] actual = new double[2];
        MultivariateDistributions.MultivariateNormal(actualGenerator, [1.0, 2.0], [2.0, 0.5, 0.5, 3.0], actual);
        AssertEquivalent(actual, new MultivariateNormalSampler(expectedGenerator, [1.0, 2.0], [2.0, 0.5, 0.5, 3.0]).Sample(), actualGenerator, expectedGenerator);
    }

    static void AssertSingleMultivariateNormalFactor()
    {
        RandomGenerator actualGenerator = Create(103UL);
        RandomGenerator expectedGenerator = Create(103UL);
        double[] actual = new double[2];
        MultivariateDistributions.MultivariateNormalFromCholesky(actualGenerator, [1.0, 2.0], [2.0, 0.0, 1.0, 3.0], actual);
        AssertEquivalent(actual, MultivariateNormalSampler.FromCholesky(expectedGenerator, [1.0, 2.0], [2.0, 0.0, 1.0, 3.0]).Sample(), actualGenerator, expectedGenerator);
    }

    static void AssertSingleWishart()
    {
        RandomGenerator actualGenerator = Create(104UL);
        RandomGenerator expectedGenerator = Create(104UL);
        double[] actual = new double[4];
        MultivariateDistributions.Wishart(actualGenerator, 6.0, [2.0, 0.5, 0.5, 3.0], 2, actual);
        AssertEquivalent(actual, new WishartSampler(expectedGenerator, 6.0, [2.0, 0.5, 0.5, 3.0], 2).Sample(), actualGenerator, expectedGenerator);
    }

    static void AssertSingleInverseWishart()
    {
        RandomGenerator actualGenerator = Create(105UL);
        RandomGenerator expectedGenerator = Create(105UL);
        double[] actual = new double[4];
        MultivariateDistributions.InverseWishart(actualGenerator, 6.0, [2.0, 0.5, 0.5, 3.0], 2, actual);
        AssertEquivalent(actual, new InverseWishartSampler(expectedGenerator, 6.0, [2.0, 0.5, 0.5, 3.0], 2).Sample(), actualGenerator, expectedGenerator);
    }

    static void AssertSingleMatrixNormal()
    {
        RandomGenerator actualGenerator = Create(106UL);
        RandomGenerator expectedGenerator = Create(106UL);
        double[] actual = new double[4];
        MultivariateDistributions.MatrixNormal(actualGenerator, [1.0, 2.0, 3.0, 4.0], 2, 2, [2.0, 0.5, 0.5, 3.0], [1.0, 0.25, 0.25, 1.5], actual);
        AssertEquivalent(actual, new MatrixNormalSampler(expectedGenerator, [1.0, 2.0, 3.0, 4.0], 2, 2, [2.0, 0.5, 0.5, 3.0], [1.0, 0.25, 0.25, 1.5]).Sample(), actualGenerator, expectedGenerator);
    }

    static void AssertSingleMatrixNormalFactor()
    {
        RandomGenerator actualGenerator = Create(107UL);
        RandomGenerator expectedGenerator = Create(107UL);
        double[] actual = new double[4];
        MultivariateDistributions.MatrixNormalFromCholesky(actualGenerator, [1.0, 2.0, 3.0, 4.0], 2, 2, [2.0, 0.0, 1.0, 3.0], [1.0, 0.0, 0.5, 2.0], actual);
        AssertEquivalent(actual, MatrixNormalSampler.FromCholesky(expectedGenerator, [1.0, 2.0, 3.0, 4.0], 2, 2, [2.0, 0.0, 1.0, 3.0], [1.0, 0.0, 0.5, 2.0]).Sample(), actualGenerator, expectedGenerator);
    }

    static void AssertSingleNormalGamma()
    {
        RandomGenerator actualGenerator = Create(108UL);
        RandomGenerator expectedGenerator = Create(108UL);
        double[] actual = new double[2];
        MultivariateDistributions.NormalGamma(actualGenerator, 1.0, 2.0, 3.0, 4.0, actual);
        AssertEquivalent(actual, new NormalGammaSampler(expectedGenerator, 1.0, 2.0, 3.0, 4.0).Sample(), actualGenerator, expectedGenerator);
    }

    static void AssertBatchDirichlet()
    {
        RandomGenerator actualGenerator = Create(201UL);
        RandomGenerator expectedGenerator = Create(201UL);
        double[] actual = new double[15];
        MultivariateDistributions.FillDirichlet(actualGenerator, [0.5, 1.5, 3.0], 5, actual);
        AssertEquivalent(actual, Repeat(5, 3, new DirichletSampler(expectedGenerator, [0.5, 1.5, 3.0]).Sample), actualGenerator, expectedGenerator);
    }

    static void AssertBatchMultivariateNormal()
    {
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator expectedGenerator = Create(202UL);
        double[] actual = new double[10];
        MultivariateDistributions.FillMultivariateNormal(actualGenerator, [1.0, 2.0], [2.0, 0.5, 0.5, 3.0], 5, actual);
        AssertEquivalent(actual, Repeat(5, 2, new MultivariateNormalSampler(expectedGenerator, [1.0, 2.0], [2.0, 0.5, 0.5, 3.0]).Sample), actualGenerator, expectedGenerator);
    }

    static void AssertBatchMultivariateNormalFactor()
    {
        RandomGenerator actualGenerator = Create(203UL);
        RandomGenerator expectedGenerator = Create(203UL);
        double[] actual = new double[10];
        MultivariateDistributions.FillMultivariateNormalFromCholesky(actualGenerator, [1.0, 2.0], [2.0, 0.0, 1.0, 3.0], 5, actual);
        MultivariateNormalSampler expectedSampler = MultivariateNormalSampler.FromCholesky(expectedGenerator, [1.0, 2.0], [2.0, 0.0, 1.0, 3.0]);
        AssertEquivalent(actual, Repeat(5, 2, expectedSampler.Sample), actualGenerator, expectedGenerator);
    }

    static void AssertBatchWishart()
    {
        RandomGenerator actualGenerator = Create(204UL);
        RandomGenerator expectedGenerator = Create(204UL);
        double[] actual = new double[20];
        MultivariateDistributions.FillWishart(actualGenerator, 6.0, [2.0, 0.5, 0.5, 3.0], 2, 5, actual);
        AssertEquivalent(actual, Repeat(5, 4, new WishartSampler(expectedGenerator, 6.0, [2.0, 0.5, 0.5, 3.0], 2).Sample), actualGenerator, expectedGenerator);
    }

    static void AssertBatchInverseWishart()
    {
        RandomGenerator actualGenerator = Create(205UL);
        RandomGenerator expectedGenerator = Create(205UL);
        double[] actual = new double[20];
        MultivariateDistributions.FillInverseWishart(actualGenerator, 6.0, [2.0, 0.5, 0.5, 3.0], 2, 5, actual);
        AssertEquivalent(actual, Repeat(5, 4, new InverseWishartSampler(expectedGenerator, 6.0, [2.0, 0.5, 0.5, 3.0], 2).Sample), actualGenerator, expectedGenerator);
    }

    static void AssertBatchMatrixNormal()
    {
        RandomGenerator actualGenerator = Create(206UL);
        RandomGenerator expectedGenerator = Create(206UL);
        double[] actual = new double[20];
        MultivariateDistributions.FillMatrixNormal(actualGenerator, [1.0, 2.0, 3.0, 4.0], 2, 2, [2.0, 0.5, 0.5, 3.0], [1.0, 0.25, 0.25, 1.5], 5, actual);
        MatrixNormalSampler expectedSampler = new MatrixNormalSampler(expectedGenerator, [1.0, 2.0, 3.0, 4.0], 2, 2, [2.0, 0.5, 0.5, 3.0], [1.0, 0.25, 0.25, 1.5]);
        AssertEquivalent(actual, Repeat(5, 4, expectedSampler.Sample), actualGenerator, expectedGenerator);
    }

    static void AssertBatchMatrixNormalFactor()
    {
        RandomGenerator actualGenerator = Create(207UL);
        RandomGenerator expectedGenerator = Create(207UL);
        double[] actual = new double[20];
        MultivariateDistributions.FillMatrixNormalFromCholesky(actualGenerator, [1.0, 2.0, 3.0, 4.0], 2, 2, [2.0, 0.0, 1.0, 3.0], [1.0, 0.0, 0.5, 2.0], 5, actual);
        MatrixNormalSampler expectedSampler = MatrixNormalSampler.FromCholesky(expectedGenerator, [1.0, 2.0, 3.0, 4.0], 2, 2, [2.0, 0.0, 1.0, 3.0], [1.0, 0.0, 0.5, 2.0]);
        AssertEquivalent(actual, Repeat(5, 4, expectedSampler.Sample), actualGenerator, expectedGenerator);
    }

    static void AssertBatchNormalGamma()
    {
        RandomGenerator actualGenerator = Create(208UL);
        RandomGenerator expectedGenerator = Create(208UL);
        double[] actual = new double[10];
        MultivariateDistributions.FillNormalGamma(actualGenerator, 1.0, 2.0, 3.0, 4.0, 5, actual);
        AssertEquivalent(actual, Repeat(5, 2, new NormalGammaSampler(expectedGenerator, 1.0, 2.0, 3.0, 4.0).Sample), actualGenerator, expectedGenerator);
    }

    static double[] Repeat(int sampleCount, int componentCount, Func<double[]> sample)
    {
        double[] result = new double[sampleCount * componentCount];
        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            Array.Copy(sample(), 0, result, sampleIndex * componentCount, componentCount);
        }

        return result;
    }

    static void AssertEquivalent(double[] actual, double[] expected, RandomGenerator actualGenerator, RandomGenerator expectedGenerator)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
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
