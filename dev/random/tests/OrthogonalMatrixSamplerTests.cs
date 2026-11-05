namespace random.Tests;

public class OrthogonalMatrixSamplerTests
{
    [TestCase(RandomMathMode.Portable)]
    [TestCase(RandomMathMode.System)]
    public void SamplesAreFiniteOrthogonalMatricesAcrossDimensions(RandomMathMode mathMode)
    {
        int[] dimensions = [1, 2, 3, 8, 32];
        foreach (int dimension in dimensions)
        {
            OrthogonalMatrixSampler sampler = new OrthogonalMatrixSampler(Create((ulong)(100 + dimension), mathMode), dimension);
            int repetitions = dimension < 10 ? 30 : 5;
            for (int repetition = 0; repetition < repetitions; repetition++)
            {
                double[] matrix = sampler.Sample();
                Assert.That(matrix.All(double.IsFinite), Is.True);
                AssertOrthogonal(matrix, dimension, 3e-12);
                Assert.That(Math.Abs(Determinant(matrix, dimension)), Is.EqualTo(1.0).Within(3e-11));
            }
        }
    }

    [TestCase(RandomMathMode.Portable)]
    [TestCase(RandomMathMode.System)]
    public void AllocatingDestinationAndBatchFormsMatchExactly(RandomMathMode mathMode)
    {
        RandomGenerator allocatingGenerator = Create(202UL, mathMode);
        RandomGenerator destinationGenerator = Create(202UL, mathMode);
        RandomGenerator batchGenerator = Create(202UL, mathMode);
        OrthogonalMatrixSampler allocating = new OrthogonalMatrixSampler(allocatingGenerator, 5);
        OrthogonalMatrixSampler writing = new OrthogonalMatrixSampler(destinationGenerator, 5);
        OrthogonalMatrixSampler batch = new OrthogonalMatrixSampler(batchGenerator, 5);
        double[] expected = new double[batch.RequiredOutputLength(31)];
        double[] destination = new double[25];
        for (int sampleIndex = 0; sampleIndex < 31; sampleIndex++)
        {
            double[] matrix = allocating.Sample();
            Array.Copy(matrix, 0, expected, sampleIndex * 25, 25);
            writing.SampleInto(destination);
            Assert.That(destination, Is.EqualTo(matrix));
        }

        double[] actual = new double[expected.Length];
        batch.Fill(31, actual);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(allocating.Dimension(), Is.EqualTo(5));
            Assert.That(allocatingGenerator.ExportState().ToBytes(), Is.EqualTo(destinationGenerator.ExportState().ToBytes()));
            Assert.That(allocatingGenerator.ExportState().ToBytes(), Is.EqualTo(batchGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void OneDimensionalMatricesMatchNormalSignsExactly()
    {
        RandomGenerator actualGenerator = Create(303UL, RandomMathMode.Portable);
        RandomGenerator referenceGenerator = Create(303UL, RandomMathMode.Portable);
        OrthogonalMatrixSampler sampler = new OrthogonalMatrixSampler(actualGenerator, 1);
        NormalSampler normal = new NormalSampler(referenceGenerator, 0.0, 1.0);

        for (int repetition = 0; repetition < 1000; repetition++)
        {
            double normalValue;
            do
            {
                normalValue = normal.Sample();
            }
            while (Math.Abs(normalValue) <= 1.0e-14);

            Assert.That(sampler.Sample()[0], Is.EqualTo(normalValue < 0.0 ? -1.0 : 1.0));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void HaarEntryMomentsAndDeterminantSignsAreBalanced()
    {
        const int sampleCount = 25000;
        OrthogonalMatrixSampler sampler = new OrthogonalMatrixSampler(Create(404UL, RandomMathMode.Portable), 4);
        double entrySum = 0.0;
        double entrySquares = 0.0;
        int positiveDeterminants = 0;
        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            double[] matrix = sampler.Sample();
            entrySum += matrix[0];
            entrySquares += matrix[0] * matrix[0];
            if (Determinant(matrix, 4) > 0.0)
            {
                positiveDeterminants++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(entrySum / sampleCount, Is.EqualTo(0.0).Within(0.01));
            Assert.That(entrySquares / sampleCount, Is.EqualTo(0.25).Within(0.008));
            Assert.That(positiveDeterminants, Is.InRange(12000, 13000));
        });
    }

    [Test]
    public void InvalidArgumentsDoNotConsumeStateOrMutateDestinations()
    {
        RandomGenerator generator = Create(505UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        Assert.That(Assert.Throws<RandomArgumentException>(() => new OrthogonalMatrixSampler(NullReference.Value<global::random.IRandomSource>(), 2))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new OrthogonalMatrixSampler(generator, 0))?.ArgumentName(), Is.EqualTo("dimension"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new OrthogonalMatrixSampler(generator, 50000))?.ArgumentName(), Is.EqualTo("dimension"));
        OrthogonalMatrixSampler sampler = new OrthogonalMatrixSampler(generator, 2);
        double[] destination = [91.0, 92.0, 93.0];
        object[] cases =
        [
            new object[] { (TestDelegate)(() => sampler.SampleInto(destination)), "destination" },
            new object[] { (TestDelegate)(() => sampler.RequiredOutputLength(-1)), "sampleCount" },
            new object[] { (TestDelegate)(() => sampler.RequiredOutputLength(int.MaxValue)), "sampleCount" },
            new object[] { (TestDelegate)(() => sampler.Fill(-1, destination)), "sampleCount" },
            new object[] { (TestDelegate)(() => sampler.Fill(1, destination)), "destination" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(destination, Is.EqualTo(new double[] { 91.0, 92.0, 93.0 }));
        }

        sampler.Fill(0, Span<double>.Empty);
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    static void AssertOrthogonal(double[] matrix, int dimension, double tolerance)
    {
        for (int first = 0; first < dimension; first++)
        {
            for (int second = 0; second < dimension; second++)
            {
                double rowDot = 0.0;
                double columnDot = 0.0;
                for (int coordinate = 0; coordinate < dimension; coordinate++)
                {
                    rowDot += matrix[first * dimension + coordinate] * matrix[second * dimension + coordinate];
                    columnDot += matrix[coordinate * dimension + first] * matrix[coordinate * dimension + second];
                }

                double expected = first == second ? 1.0 : 0.0;
                Assert.That(rowDot, Is.EqualTo(expected).Within(tolerance));
                Assert.That(columnDot, Is.EqualTo(expected).Within(tolerance));
            }
        }
    }

    static double Determinant(double[] matrix, int dimension)
    {
        double[] work = (double[])matrix.Clone();
        double determinant = 1.0;
        for (int column = 0; column < dimension; column++)
        {
            int pivot = column;
            for (int row = column + 1; row < dimension; row++)
            {
                if (Math.Abs(work[row * dimension + column]) > Math.Abs(work[pivot * dimension + column]))
                {
                    pivot = row;
                }
            }

            if (pivot != column)
            {
                for (int entry = column; entry < dimension; entry++)
                {
                    double temporary = work[column * dimension + entry];
                    work[column * dimension + entry] = work[pivot * dimension + entry];
                    work[pivot * dimension + entry] = temporary;
                }

                determinant = -determinant;
            }

            double diagonal = work[column * dimension + column];
            determinant *= diagonal;
            for (int row = column + 1; row < dimension; row++)
            {
                double factor = work[row * dimension + column] / diagonal;
                for (int entry = column + 1; entry < dimension; entry++)
                {
                    work[row * dimension + entry] -= factor * work[column * dimension + entry];
                }
            }
        }

        return determinant;
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mathMode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mathMode);
    }
}
