namespace random.Tests;

public class RandomGeometryTests
{
    delegate void SpanAction(Span<double> destination);
    delegate void GeneratorSpanAction(RandomGenerator generator, Span<double> destination);
    delegate void BatchAction(RandomGenerator generator, Span<double> destination);
    delegate void FixedFillAction(RandomGenerator generator, int sampleCount, Span<double> destination);

    [Test]
    public void ScalarFacadesMatchUnderlyingSamplersExactly()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator expectedGenerator = Create(101UL);
        double[] simplexVertices = [0.0, 0.0, 1.0, 0.0, 0.0, 1.0];
        double[] polygon = [0.0, 0.0, 2.0, 0.0, 2.0, 1.0, 0.0, 1.0];

        Assert.That(RandomGeometry.Line(actualGenerator, [0.0, 1.0], [2.0, 3.0]),
            Is.EqualTo(BallSampler.Line(expectedGenerator, [0.0, 1.0], [2.0, 3.0])));
        Assert.That(RandomGeometry.Box(actualGenerator, [-1.0, 2.0], [1.0, 4.0]),
            Is.EqualTo(BallSampler.Box(expectedGenerator, [-1.0, 2.0], [1.0, 4.0])));
        Assert.That(RandomGeometry.UnitCircle(actualGenerator), Is.EqualTo(BallSampler.UnitCircle(expectedGenerator)));
        Assert.That(RandomGeometry.UnitDisk(actualGenerator), Is.EqualTo(BallSampler.UnitDisk(expectedGenerator)));
        Assert.That(RandomGeometry.UnitSphere(actualGenerator), Is.EqualTo(BallSampler.UnitSphere(expectedGenerator)));
        Assert.That(RandomGeometry.UnitBall(actualGenerator, 5), Is.EqualTo(BallSampler.UnitBall(expectedGenerator, 5)));
        Assert.That(RandomGeometry.Simplex(actualGenerator, simplexVertices, 2),
            Is.EqualTo(SimplexSampler.Simplex(expectedGenerator, simplexVertices, 2)));
        Assert.That(RandomGeometry.Triangle(actualGenerator, [0.0, 0.0], [1.0, 0.0], [0.0, 1.0]),
            Is.EqualTo(SimplexSampler.Triangle(expectedGenerator, [0.0, 0.0], [1.0, 0.0], [0.0, 1.0])));
        Assert.That(RandomGeometry.Tetrahedron(actualGenerator, [0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0]),
            Is.EqualTo(SimplexSampler.Tetrahedron(expectedGenerator, [0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0])));
        Assert.That(RandomGeometry.Quaternion(actualGenerator), Is.EqualTo(RotationSampler.Quaternion(expectedGenerator)));
        Assert.That(RandomGeometry.Rotation3D(actualGenerator), Is.EqualTo(RotationSampler.Rotation3D(expectedGenerator)));
        Assert.That(RandomGeometry.OrthogonalMatrix(actualGenerator, 4),
            Is.EqualTo(new OrthogonalMatrixSampler(expectedGenerator, 4).Sample()));
        Assert.That(RandomGeometry.Polygon(actualGenerator, polygon),
            Is.EqualTo(new PolytopeSampler(expectedGenerator, polygon).Sample()));

        Assert.Multiple(() =>
        {
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
            Assert.That(RandomGeometry.PolygonTriangulation(polygon), Is.EqualTo(PolytopeSampler.Triangulate(polygon)));
        });
    }

    [Test]
    public void DestinationFacadesMatchAllocatingFormsExactly()
    {
        RandomGenerator allocatingGenerator = Create(202UL);
        RandomGenerator destinationGenerator = Create(202UL);
        double[] line = RandomGeometry.Line(allocatingGenerator, [0.0, 1.0], [2.0, 3.0]);
        double[] box = RandomGeometry.Box(allocatingGenerator, [0.0, 1.0], [2.0, 3.0]);
        double[] circle = RandomGeometry.UnitCircle(allocatingGenerator);
        double[] disk = RandomGeometry.UnitDisk(allocatingGenerator);
        double[] sphere = RandomGeometry.UnitSphere(allocatingGenerator);
        double[] ball = RandomGeometry.UnitBall(allocatingGenerator, 4);
        double[] simplex = RandomGeometry.Simplex(allocatingGenerator, [0.0, 0.0, 1.0, 0.0, 0.0, 1.0], 2);
        double[] triangle = RandomGeometry.Triangle(allocatingGenerator, [0.0, 0.0], [1.0, 0.0], [0.0, 1.0]);
        double[] tetrahedron = RandomGeometry.Tetrahedron(allocatingGenerator, [0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0]);
        double[] quaternion = RandomGeometry.Quaternion(allocatingGenerator);
        double[] rotation = RandomGeometry.Rotation3D(allocatingGenerator);
        double[] orthogonal = RandomGeometry.OrthogonalMatrix(allocatingGenerator, 3);
        double[] polygon = RandomGeometry.Polygon(allocatingGenerator, [0.0, 0.0, 1.0, 0.0, 0.0, 1.0]);

        AssertDestination(destinationGenerator, line, values => RandomGeometry.Line(destinationGenerator, [0.0, 1.0], [2.0, 3.0], values));
        AssertDestination(destinationGenerator, box, values => RandomGeometry.Box(destinationGenerator, [0.0, 1.0], [2.0, 3.0], values));
        AssertDestination(destinationGenerator, circle, values => RandomGeometry.UnitCircle(destinationGenerator, values));
        AssertDestination(destinationGenerator, disk, values => RandomGeometry.UnitDisk(destinationGenerator, values));
        AssertDestination(destinationGenerator, sphere, values => RandomGeometry.UnitSphere(destinationGenerator, values));
        AssertDestination(destinationGenerator, ball, values => RandomGeometry.UnitBall(destinationGenerator, values));
        AssertDestination(destinationGenerator, simplex, values => RandomGeometry.Simplex(destinationGenerator, [0.0, 0.0, 1.0, 0.0, 0.0, 1.0], 2, values));
        AssertDestination(destinationGenerator, triangle, values => RandomGeometry.Triangle(destinationGenerator, [0.0, 0.0], [1.0, 0.0], [0.0, 1.0], values));
        AssertDestination(destinationGenerator, tetrahedron, values => RandomGeometry.Tetrahedron(destinationGenerator, [0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0], values));
        AssertDestination(destinationGenerator, quaternion, values => RandomGeometry.Quaternion(destinationGenerator, values));
        AssertDestination(destinationGenerator, rotation, values => RandomGeometry.Rotation3D(destinationGenerator, values));
        AssertDestination(destinationGenerator, orthogonal, values => RandomGeometry.OrthogonalMatrix(destinationGenerator, 3, values));
        AssertDestination(destinationGenerator, polygon, values => RandomGeometry.Polygon(destinationGenerator, [0.0, 0.0, 1.0, 0.0, 0.0, 1.0], values));

        Assert.That(destinationGenerator.ExportState().ToBytes(), Is.EqualTo(allocatingGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void FixedShapeBatchesMatchRepeatedScalarCallsExactly()
    {
        AssertFixedBatch(301UL, 2, RandomGeometry.FillUnitCircle, RandomGeometry.UnitCircle);
        AssertFixedBatch(302UL, 2, RandomGeometry.FillUnitDisk, RandomGeometry.UnitDisk);
        AssertFixedBatch(303UL, 3, RandomGeometry.FillUnitSphere, RandomGeometry.UnitSphere);
        AssertFixedBatch(304UL, 4, RandomGeometry.FillQuaternions, RandomGeometry.Quaternion);
        AssertFixedBatch(305UL, 9, RandomGeometry.FillRotation3D, RandomGeometry.Rotation3D);
    }

    [Test]
    public void VariableShapeBatchesMatchRepeatedScalarCallsExactly()
    {
        const int sampleCount = 23;
        CompareBatch(
            401UL,
            3,
            sampleCount,
            (generator, destination) => RandomGeometry.FillLine(generator, [0.0, 1.0, 2.0], [3.0, 4.0, 5.0], sampleCount, destination),
            (generator, destination) => RandomGeometry.Line(generator, [0.0, 1.0, 2.0], [3.0, 4.0, 5.0], destination));
        CompareBatch(
            402UL,
            3,
            sampleCount,
            (generator, destination) => RandomGeometry.FillBox(generator, [-1.0, 0.0, 4.0], [1.0, 2.0, 4.0], sampleCount, destination),
            (generator, destination) => RandomGeometry.Box(generator, [-1.0, 0.0, 4.0], [1.0, 2.0, 4.0], destination));
        CompareBatch(
            403UL,
            5,
            sampleCount,
            (generator, destination) => RandomGeometry.FillUnitBall(generator, 5, sampleCount, destination),
            (generator, destination) => RandomGeometry.UnitBall(generator, destination));
        double[] simplexVertices = [0.0, 0.0, 1.0, 0.0, 0.0, 1.0];
        CompareBatch(
            404UL,
            2,
            sampleCount,
            (generator, destination) => RandomGeometry.FillSimplex(generator, simplexVertices, 2, sampleCount, destination),
            (generator, destination) => RandomGeometry.Simplex(generator, simplexVertices, 2, destination));
        CompareBatch(
            405UL,
            2,
            sampleCount,
            (generator, destination) => RandomGeometry.FillTriangle(generator, [0.0, 0.0], [1.0, 0.0], [0.0, 1.0], sampleCount, destination),
            (generator, destination) => RandomGeometry.Triangle(generator, [0.0, 0.0], [1.0, 0.0], [0.0, 1.0], destination));
        CompareBatch(
            406UL,
            3,
            sampleCount,
            (generator, destination) => RandomGeometry.FillTetrahedron(generator, [0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0], sampleCount, destination),
            (generator, destination) => RandomGeometry.Tetrahedron(generator, [0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0], destination));
    }

    [Test]
    public void PreprocessedBatchesMatchRepeatedFacadeCallsExactly()
    {
        const int sampleCount = 31;
        double[] polygonVertices = [0.0, 0.0, 2.0, 0.0, 2.0, 1.0, 1.0, 2.0, 0.0, 1.0];
        RandomGenerator polygonBatchGenerator = Create(501UL);
        RandomGenerator polygonScalarGenerator = Create(501UL);
        double[] polygonBatch = new double[sampleCount * 2];
        double[] polygonExpected = new double[polygonBatch.Length];
        RandomGeometry.FillPolygon(polygonBatchGenerator, polygonVertices, sampleCount, polygonBatch);
        for (int sample = 0; sample < sampleCount; sample++)
        {
            RandomGeometry.Polygon(polygonScalarGenerator, polygonVertices, polygonExpected.AsSpan(sample * 2, 2));
        }

        RandomGenerator matrixBatchGenerator = Create(502UL);
        RandomGenerator matrixScalarGenerator = Create(502UL);
        double[] matrixBatch = new double[sampleCount * 16];
        double[] matrixExpected = new double[matrixBatch.Length];
        RandomGeometry.FillOrthogonalMatrices(matrixBatchGenerator, 4, sampleCount, matrixBatch);
        for (int sample = 0; sample < sampleCount; sample++)
        {
            RandomGeometry.OrthogonalMatrix(matrixScalarGenerator, 4, matrixExpected.AsSpan(sample * 16, 16));
        }

        Assert.Multiple(() =>
        {
            Assert.That(polygonBatch, Is.EqualTo(polygonExpected));
            Assert.That(matrixBatch, Is.EqualTo(matrixExpected));
            Assert.That(polygonBatchGenerator.ExportState().ToBytes(), Is.EqualTo(polygonScalarGenerator.ExportState().ToBytes()));
            Assert.That(matrixBatchGenerator.ExportState().ToBytes(), Is.EqualTo(matrixScalarGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void SingularBatchAliasesMatchPluralFormsExactly()
    {
        double[] quaternionAlias = new double[40];
        double[] quaternionPlural = new double[40];
        double[] matrixAlias = new double[90];
        double[] matrixPlural = new double[90];
        RandomGeometry.FillQuaternion(Create(551UL), 10, quaternionAlias);
        RandomGeometry.FillQuaternions(Create(551UL), 10, quaternionPlural);
        RandomGeometry.FillOrthogonalMatrix(Create(552UL), 3, 10, matrixAlias);
        RandomGeometry.FillOrthogonalMatrices(Create(552UL), 3, 10, matrixPlural);

        Assert.Multiple(() =>
        {
            Assert.That(quaternionAlias, Is.EqualTo(quaternionPlural));
            Assert.That(matrixAlias, Is.EqualTo(matrixPlural));
        });
    }

    [Test]
    public void BatchMethodsDefensivelyCaptureAliasedGeometryInputs()
    {
        double[] simplexVertices = [0.0, 0.0, 1.0, 0.0, 0.0, 1.0];
        double[] simplexExpected = new double[6];
        RandomGeometry.FillSimplex(Create(601UL), (double[])simplexVertices.Clone(), 2, 3, simplexExpected);
        RandomGeometry.FillSimplex(Create(601UL), simplexVertices, 2, 3, simplexVertices);

        double[] polygonVertices = [0.0, 0.0, 2.0, 0.0, 2.0, 2.0, 0.0, 2.0];
        double[] polygonExpected = new double[8];
        RandomGeometry.FillPolygon(Create(602UL), (double[])polygonVertices.Clone(), 4, polygonExpected);
        RandomGeometry.FillPolygon(Create(602UL), polygonVertices, 4, polygonVertices);

        Assert.Multiple(() =>
        {
            Assert.That(simplexVertices, Is.EqualTo(simplexExpected));
            Assert.That(polygonVertices, Is.EqualTo(polygonExpected));
        });
    }

    [Test]
    public void FacadeOutputsSatisfyRepresentativeGeometryInvariants()
    {
        RandomGenerator generator = Create(701UL);
        for (int sample = 0; sample < 1000; sample++)
        {
            double[] circle = RandomGeometry.UnitCircle(generator);
            double[] disk = RandomGeometry.UnitDisk(generator);
            double[] sphere = RandomGeometry.UnitSphere(generator);
            double[] ball = RandomGeometry.UnitBall(generator, 6);
            double[] triangle = RandomGeometry.Triangle(generator, [0.0, 0.0], [1.0, 0.0], [0.0, 1.0]);
            double[] rotation = RandomGeometry.Rotation3D(generator);
            double[] orthogonal = RandomGeometry.OrthogonalMatrix(generator, 5);
            Assert.Multiple(() =>
            {
                Assert.That(SquaredNorm(circle), Is.EqualTo(1.0).Within(3e-15));
                Assert.That(SquaredNorm(disk), Is.LessThanOrEqualTo(1.0));
                Assert.That(SquaredNorm(sphere), Is.EqualTo(1.0).Within(3e-15));
                Assert.That(SquaredNorm(ball), Is.LessThanOrEqualTo(1.0));
                Assert.That(triangle, Is.All.GreaterThanOrEqualTo(0.0));
                Assert.That(triangle.Sum(), Is.LessThanOrEqualTo(1.0));
                AssertOrthogonal(rotation, 3, 5e-15);
                AssertOrthogonal(orthogonal, 5, 2e-13);
            });
        }
    }

    [Test]
    public void InvalidBatchRequestsAreAtomicAndZeroBatchesStillValidateGeometry()
    {
        RandomGenerator generator = Create(801UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = [91.0, 92.0, 93.0];
        object[] cases =
        [
            new object[] { (TestDelegate)(() => RandomGeometry.FillUnitCircle(generator, -1, destination)), "sampleCount" },
            new object[] { (TestDelegate)(() => RandomGeometry.FillUnitSphere(generator, 1, destination.AsSpan(0, 2))), "destination" },
            new object[] { (TestDelegate)(() => RandomGeometry.FillUnitBall(generator, 0, 0, Span<double>.Empty)), "dimension" },
            new object[] { (TestDelegate)(() => RandomGeometry.FillLine(generator, [], [], 0, Span<double>.Empty)), "start" },
            new object[] { (TestDelegate)(() => RandomGeometry.FillSimplex(generator, [0.0, 0.0], 1, 0, Span<double>.Empty)), "vertices" },
            new object[] { (TestDelegate)(() => RandomGeometry.FillTriangle(generator, [0.0, 0.0], [1.0, 1.0], [2.0, 2.0], 0, Span<double>.Empty)), "third" },
            new object[] { (TestDelegate)(() => RandomGeometry.FillPolygon(generator, [0.0, 0.0, 1.0, 1.0, 2.0, 2.0], 0, Span<double>.Empty)), "vertices" },
            new object[] { (TestDelegate)(() => RandomGeometry.FillOrthogonalMatrices(generator, 2, 1, destination)), "destination" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(destination, Is.EqualTo(new double[] { 91.0, 92.0, 93.0 }));
        }

        RandomGeometry.FillUnitCircle(generator, 0, Span<double>.Empty);
        RandomGeometry.FillUnitBall(generator, 3, 0, Span<double>.Empty);
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    static void CompareBatch(
        ulong seed,
        int componentCount,
        int sampleCount,
        BatchAction batchAction,
        GeneratorSpanAction scalarAction)
    {
        RandomGenerator batchGenerator = Create(seed);
        RandomGenerator scalarGenerator = Create(seed);
        double[] actual = new double[componentCount * sampleCount];
        double[] expected = new double[actual.Length];
        batchAction(batchGenerator, actual);
        for (int sample = 0; sample < sampleCount; sample++)
        {
            scalarAction(scalarGenerator, expected.AsSpan(sample * componentCount, componentCount));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(batchGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        });
    }

    static void AssertFixedBatch(
        ulong seed,
        int componentCount,
        FixedFillAction fill,
        GeneratorSpanAction scalar)
    {
        const int sampleCount = 29;
        RandomGenerator fillGenerator = Create(seed);
        RandomGenerator scalarGenerator = Create(seed);
        double[] actual = new double[sampleCount * componentCount];
        double[] expected = new double[actual.Length];
        fill(fillGenerator, sampleCount, actual);
        for (int sample = 0; sample < sampleCount; sample++)
        {
            scalar(scalarGenerator, expected.AsSpan(sample * componentCount, componentCount));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        });
    }

    static void AssertDestination(RandomGenerator generator, double[] expected, SpanAction action)
    {
        double[] actual = new double[expected.Length];
        action(actual);
        Assert.That(actual, Is.EqualTo(expected));
    }

    static double SquaredNorm(double[] values)
    {
        return values.Sum(value => value * value);
    }

    static void AssertOrthogonal(double[] matrix, int dimension, double tolerance)
    {
        for (int first = 0; first < dimension; first++)
        {
            for (int second = 0; second < dimension; second++)
            {
                double dot = 0.0;
                for (int coordinate = 0; coordinate < dimension; coordinate++)
                {
                    dot += matrix[first * dimension + coordinate] * matrix[second * dimension + coordinate];
                }

                Assert.That(dot, Is.EqualTo(first == second ? 1.0 : 0.0).Within(tolerance));
            }
        }
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
