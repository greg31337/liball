namespace random.Tests;

public class CompositeFactoryConstructionTests
{
    [Test]
    public void UnweightedEmpiricalFactoryMatchesCanonicalIndexTraversalAndCopiesInput()
    {
        IRandomFactory factory = new RandomFactory();
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator expectedGenerator = Create(101UL);
        double[] observations = [-3.0, 1.5, 8.0, 21.0];
        EmpiricalSampler actual = factory.CreateUnweightedEmpiricalSampler(actualGenerator, observations);
        EmpiricalSampler expected = EmpiricalSampler.CreateUnweighted(expectedGenerator, [-3.0, 1.5, 8.0, 21.0]);
        observations[0] = 999.0;
        double[] actualValues = new double[257];
        double[] expectedValues = new double[257];

        actual.Fill(actualValues);
        expected.Fill(expectedValues);

        Assert.Multiple(() =>
        {
            Assert.That(actual.ObservationCount(), Is.EqualTo(4));
            Assert.That(actualValues, Is.EqualTo(expectedValues));
            Assert.That(actualValues, Has.None.EqualTo(999.0));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void IdentityRejectionFactoryPreservesTraversalAndDiagnostics()
    {
        IRandomFactory factory = new RandomFactory();
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator expectedGenerator = Create(202UL);
        int actualProposal = 0;
        int expectedProposal = 0;
        RejectionSampler actual = factory.CreateIdentityRejectionSampler(
            actualGenerator,
            () => ++actualProposal,
            candidate => candidate % 3.0 == 0.0 ? 1.0 : 0.35,
            100);
        RejectionSampler expected = RejectionSampler.CreateIdentity(
            expectedGenerator,
            () => ++expectedProposal,
            candidate => candidate % 3.0 == 0.0 ? 1.0 : 0.35,
            100);
        double[] actualValues = new double[64];
        double[] expectedValues = new double[64];

        actual.Fill(actualValues);
        expected.Fill(expectedValues);

        Assert.Multiple(() =>
        {
            Assert.That(actualValues, Is.EqualTo(expectedValues));
            Assert.That(actualProposal, Is.EqualTo(expectedProposal));
            Assert.That(actual.MaximumAttempts(), Is.EqualTo(expected.MaximumAttempts()));
            Assert.That(actual.LastAttemptCount(), Is.EqualTo(expected.LastAttemptCount()));
            Assert.That(actual.TotalAttemptCount(), Is.EqualTo(expected.TotalAttemptCount()));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void CholeskyFactoryPathsMatchCovarianceConstructorsAndCopyInputs()
    {
        IRandomFactory factory = new RandomFactory();
        double[] vectorMean = [1.0, -2.0];
        double[] vectorFactor = [2.0, 0.0, 0.5, 3.0];
        RandomGenerator vectorActualGenerator = Create(303UL);
        RandomGenerator vectorExpectedGenerator = Create(303UL);
        MultivariateNormalSampler vectorActual = factory.CreateMultivariateNormalFromCholesky(vectorActualGenerator, vectorMean, vectorFactor);
        MultivariateNormalSampler vectorExpected = new MultivariateNormalSampler(vectorExpectedGenerator, [1.0, -2.0], [4.0, 1.0, 1.0, 9.25]);
        vectorMean[0] = 999.0;
        vectorFactor[0] = 999.0;
        double[] actualVectors = new double[vectorActual.RequiredBatchOutputLength(31)];
        double[] expectedVectors = new double[vectorExpected.RequiredBatchOutputLength(31)];
        vectorActual.Fill(31, actualVectors);
        vectorExpected.Fill(31, expectedVectors);

        double[] matrixMean = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0];
        double[] rowFactor = [2.0, 0.0, 0.5, 1.5];
        double[] columnFactor = [1.0, 0.0, 0.0, 0.25, 2.0, 0.0, -0.5, 0.75, 1.25];
        RandomGenerator matrixActualGenerator = Create(404UL);
        RandomGenerator matrixExpectedGenerator = Create(404UL);
        MatrixNormalSampler matrixActual = factory.CreateMatrixNormalFromCholesky(
            matrixActualGenerator,
            matrixMean,
            2,
            3,
            rowFactor,
            columnFactor);
        MatrixNormalSampler matrixExpected = new MatrixNormalSampler(
            matrixExpectedGenerator,
            [1.0, 2.0, 3.0, 4.0, 5.0, 6.0],
            2,
            3,
            [4.0, 1.0, 1.0, 2.5],
            [1.0, 0.25, -0.5, 0.25, 4.0625, 1.375, -0.5, 1.375, 2.375]);
        matrixMean[0] = 999.0;
        rowFactor[0] = 999.0;
        columnFactor[0] = 999.0;
        double[] actualMatrices = new double[matrixActual.RequiredBatchOutputLength(17)];
        double[] expectedMatrices = new double[matrixExpected.RequiredBatchOutputLength(17)];
        matrixActual.Fill(17, actualMatrices);
        matrixExpected.Fill(17, expectedMatrices);

        Assert.Multiple(() =>
        {
            Assert.That(vectorActual.Dimension(), Is.EqualTo(2));
            Assert.That(actualVectors, Is.EqualTo(expectedVectors));
            Assert.That(vectorActualGenerator.ExportState().ToBytes(), Is.EqualTo(vectorExpectedGenerator.ExportState().ToBytes()));
            Assert.That(matrixActual.RowCount(), Is.EqualTo(2));
            Assert.That(matrixActual.ColumnCount(), Is.EqualTo(3));
            Assert.That(matrixActual.RequiredOutputLength(), Is.EqualTo(6));
            Assert.That(actualMatrices, Is.EqualTo(expectedMatrices));
            Assert.That(matrixActualGenerator.ExportState().ToBytes(), Is.EqualTo(matrixExpectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void SpecializedFactoryPathsValidateBeforeRandomConsumption()
    {
        IRandomFactory factory = new RandomFactory();
        RandomGenerator generator = Create(505UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        AssertArgument("observations", () => factory.CreateUnweightedEmpiricalSampler(generator, []));
        AssertArgument("proposal", () => factory.CreateIdentityRejectionSampler(generator, NullReference.Value<global::System.Func<double>>(), candidate => 1.0, 1));
        AssertArgument("maximumAttempts", () => factory.CreateIdentityRejectionSampler(generator, () => 1.0, candidate => 1.0, 0));
        AssertArgument("choleskyFactor", () => factory.CreateMultivariateNormalFromCholesky(generator, [0.0, 0.0], [1.0, 1.0, 0.0, 1.0]));
        AssertArgument("rowScaleFactor", () => factory.CreateMatrixNormalFromCholesky(generator, [0.0, 0.0, 0.0, 0.0], 2, 2, [1.0, 1.0, 0.0, 1.0], [1.0, 0.0, 0.0, 1.0]));

        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
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
