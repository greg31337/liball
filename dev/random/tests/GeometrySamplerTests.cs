namespace random.Tests;

public class GeometrySamplerTests
{
    delegate void SpanAction(Span<double> destination);

    [Test]
    public void PublicScalarSurfaceMatchesTheGeometryAlgorithmsExactly()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator expectedGenerator = Create(101UL);
        IGeometrySampler sampler = new GeometrySampler(actualGenerator);
        double[] simplex = [0.0, 0.0, 1.0, 0.0, 0.0, 1.0];
        double[] polygon = [0.0, 0.0, 2.0, 0.0, 2.0, 1.0, 0.0, 1.0];

        Assert.That(sampler.Line([0.0, 1.0], [2.0, 3.0]), Is.EqualTo(RandomGeometry.Line(expectedGenerator, [0.0, 1.0], [2.0, 3.0])));
        Assert.That(sampler.Box([-1.0, 2.0], [1.0, 4.0]), Is.EqualTo(RandomGeometry.Box(expectedGenerator, [-1.0, 2.0], [1.0, 4.0])));
        Assert.That(sampler.UnitCircle(), Is.EqualTo(RandomGeometry.UnitCircle(expectedGenerator)));
        Assert.That(sampler.UnitDisk(), Is.EqualTo(RandomGeometry.UnitDisk(expectedGenerator)));
        Assert.That(sampler.UnitSphere(), Is.EqualTo(RandomGeometry.UnitSphere(expectedGenerator)));
        Assert.That(sampler.UnitVector(5), Is.EqualTo(RandomGeometry.UnitVector(expectedGenerator, 5)));
        Assert.That(sampler.UnitBall(5), Is.EqualTo(RandomGeometry.UnitBall(expectedGenerator, 5)));
        Assert.That(sampler.Simplex(simplex, 2), Is.EqualTo(RandomGeometry.Simplex(expectedGenerator, simplex, 2)));
        Assert.That(sampler.Triangle([0.0, 0.0], [1.0, 0.0], [0.0, 1.0]), Is.EqualTo(RandomGeometry.Triangle(expectedGenerator, [0.0, 0.0], [1.0, 0.0], [0.0, 1.0])));
        Assert.That(sampler.Tetrahedron([0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0]), Is.EqualTo(RandomGeometry.Tetrahedron(expectedGenerator, [0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0])));
        Assert.That(sampler.Quaternion(), Is.EqualTo(RandomGeometry.Quaternion(expectedGenerator)));
        Assert.That(sampler.Rotation3D(), Is.EqualTo(RandomGeometry.Rotation3D(expectedGenerator)));
        Assert.That(sampler.OrthogonalMatrix(4), Is.EqualTo(RandomGeometry.OrthogonalMatrix(expectedGenerator, 4)));
        Assert.That(sampler.Polygon(polygon), Is.EqualTo(RandomGeometry.Polygon(expectedGenerator, polygon)));

        Assert.Multiple(() =>
        {
            Assert.That(sampler.PolygonTriangulation(polygon), Is.EqualTo(RandomGeometry.PolygonTriangulation(polygon)));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void EveryDestinationFormMatchesTheInternalFacade()
    {
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator expectedGenerator = Create(202UL);
        IGeometrySampler sampler = new GeometrySampler(actualGenerator);
        double[] simplex = [0.0, 0.0, 1.0, 0.0, 0.0, 1.0];
        double[] polygon = [0.0, 0.0, 2.0, 0.0, 2.0, 1.0, 0.0, 1.0];

        CompareInto(2, values => sampler.LineInto([0.0, 1.0], [2.0, 3.0], values), values => RandomGeometry.Line(expectedGenerator, [0.0, 1.0], [2.0, 3.0], values));
        CompareInto(2, values => sampler.BoxInto([-1.0, 2.0], [1.0, 4.0], values), values => RandomGeometry.Box(expectedGenerator, [-1.0, 2.0], [1.0, 4.0], values));
        CompareInto(2, sampler.UnitCircleInto, values => RandomGeometry.UnitCircle(expectedGenerator, values));
        CompareInto(2, sampler.UnitDiskInto, values => RandomGeometry.UnitDisk(expectedGenerator, values));
        CompareInto(3, sampler.UnitSphereInto, values => RandomGeometry.UnitSphere(expectedGenerator, values));
        CompareInto(5, sampler.UnitVectorInto, values => RandomGeometry.UnitVectorInto(expectedGenerator, values));
        CompareInto(5, sampler.UnitBallInto, values => RandomGeometry.UnitBall(expectedGenerator, values));
        CompareInto(2, values => sampler.SimplexInto(simplex, 2, values), values => RandomGeometry.Simplex(expectedGenerator, simplex, 2, values));
        CompareInto(2, values => sampler.TriangleInto([0.0, 0.0], [1.0, 0.0], [0.0, 1.0], values), values => RandomGeometry.Triangle(expectedGenerator, [0.0, 0.0], [1.0, 0.0], [0.0, 1.0], values));
        CompareInto(3, values => sampler.TetrahedronInto([0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0], values), values => RandomGeometry.Tetrahedron(expectedGenerator, [0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0], values));
        CompareInto(4, sampler.QuaternionInto, values => RandomGeometry.Quaternion(expectedGenerator, values));
        CompareInto(9, sampler.Rotation3DInto, values => RandomGeometry.Rotation3D(expectedGenerator, values));
        CompareInto(16, values => sampler.OrthogonalMatrixInto(4, values), values => RandomGeometry.OrthogonalMatrix(expectedGenerator, 4, values));
        CompareInto(2, values => sampler.PolygonInto(polygon, values), values => RandomGeometry.Polygon(expectedGenerator, polygon, values));

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void EveryBatchFormIsSampleMajorAndMatchesRepeatedFacadeBehavior()
    {
        const int count = 7;
        RandomGenerator actualGenerator = Create(303UL);
        RandomGenerator expectedGenerator = Create(303UL);
        IGeometrySampler sampler = new GeometrySampler(actualGenerator);
        double[] simplex = [0.0, 0.0, 1.0, 0.0, 0.0, 1.0];
        double[] polygon = [0.0, 0.0, 2.0, 0.0, 2.0, 1.0, 0.0, 1.0];

        CompareBatch(2, count, values => sampler.FillLine([0.0, 1.0], [2.0, 3.0], count, values), values => RandomGeometry.FillLine(expectedGenerator, [0.0, 1.0], [2.0, 3.0], count, values));
        CompareBatch(2, count, values => sampler.FillBox([-1.0, 2.0], [1.0, 4.0], count, values), values => RandomGeometry.FillBox(expectedGenerator, [-1.0, 2.0], [1.0, 4.0], count, values));
        CompareBatch(2, count, values => sampler.FillUnitCircle(count, values), values => RandomGeometry.FillUnitCircle(expectedGenerator, count, values));
        CompareBatch(2, count, values => sampler.FillUnitDisk(count, values), values => RandomGeometry.FillUnitDisk(expectedGenerator, count, values));
        CompareBatch(3, count, values => sampler.FillUnitSphere(count, values), values => RandomGeometry.FillUnitSphere(expectedGenerator, count, values));
        CompareBatch(5, count, values => sampler.FillUnitVector(5, count, values), values => RandomGeometry.FillUnitVector(expectedGenerator, 5, count, values));
        CompareBatch(5, count, values => sampler.FillUnitBall(5, count, values), values => RandomGeometry.FillUnitBall(expectedGenerator, 5, count, values));
        CompareBatch(2, count, values => sampler.FillSimplex(simplex, 2, count, values), values => RandomGeometry.FillSimplex(expectedGenerator, simplex, 2, count, values));
        CompareBatch(2, count, values => sampler.FillTriangle([0.0, 0.0], [1.0, 0.0], [0.0, 1.0], count, values), values => RandomGeometry.FillTriangle(expectedGenerator, [0.0, 0.0], [1.0, 0.0], [0.0, 1.0], count, values));
        CompareBatch(3, count, values => sampler.FillTetrahedron([0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0], count, values), values => RandomGeometry.FillTetrahedron(expectedGenerator, [0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0], count, values));
        CompareBatch(4, count, values => sampler.FillQuaternions(count, values), values => RandomGeometry.FillQuaternions(expectedGenerator, count, values));
        CompareBatch(9, count, values => sampler.FillRotation3D(count, values), values => RandomGeometry.FillRotation3D(expectedGenerator, count, values));
        CompareBatch(16, count, values => sampler.FillOrthogonalMatrices(4, count, values), values => RandomGeometry.FillOrthogonalMatrices(expectedGenerator, 4, count, values));
        CompareBatch(2, count, values => sampler.FillPolygon(polygon, count, values), values => RandomGeometry.FillPolygon(expectedGenerator, polygon, count, values));

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void ArbitraryDimensionalUnitVectorsHaveTheExpectedSphericalMoments()
    {
        const int sampleCount = 30000;
        const int dimension = 5;
        IGeometrySampler sampler = new GeometrySampler(Create(404UL));
        double[] sums = new double[dimension];
        double[] squares = new double[dimension];
        double[] vector = new double[dimension];
        for (int sample = 0; sample < sampleCount; sample++)
        {
            sampler.UnitVectorInto(vector);
            Assert.That(SquaredNorm(vector), Is.EqualTo(1.0).Within(5e-15));
            for (int component = 0; component < dimension; component++)
            {
                sums[component] += vector[component];
                squares[component] += vector[component] * vector[component];
            }
        }

        Assert.Multiple(() =>
        {
            for (int component = 0; component < dimension; component++)
            {
                Assert.That(sums[component] / sampleCount, Is.EqualTo(0.0).Within(0.012));
                Assert.That(squares[component] / sampleCount, Is.EqualTo(0.2).Within(0.008));
            }
        });
    }

    [Test]
    public void InvalidRequestsAreAtomicAndEmptyBatchesConsumeNoState()
    {
        AssertArgument("generator", () => new GeometrySampler(NullReference.Value<global::random.RandomGenerator>()));
        RandomGenerator generator = Create(505UL);
        generator.NextBoolean();
        IGeometrySampler sampler = new GeometrySampler(generator);
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = [91.0, 92.0, 93.0];

        AssertArgument("dimension", () => sampler.UnitVector(0));
        AssertArgument("destination", () => sampler.UnitVectorInto(Span<double>.Empty));
        AssertArgument("sampleCount", () => sampler.FillUnitVector(3, -1, Span<double>.Empty));
        AssertArgument("destination", () => sampler.FillUnitVector(3, 1, destination.AsSpan(0, 2)));
        AssertArgument("start", () => sampler.FillLine([], [], 0, Span<double>.Empty));
        AssertArgument("third", () => sampler.FillTriangle([0.0, 0.0], [1.0, 1.0], [2.0, 2.0], 0, Span<double>.Empty));
        AssertArgument("vertices", () => sampler.FillPolygon([0.0, 0.0, 1.0, 1.0, 2.0, 2.0], 0, Span<double>.Empty));
        sampler.FillUnitVector(3, 0, Span<double>.Empty);
        sampler.FillUnitCircle(0, Span<double>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(new double[] { 91.0, 92.0, 93.0 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    static void CompareInto(int componentCount, SpanAction actualAction, SpanAction expectedAction)
    {
        double[] actual = new double[componentCount];
        double[] expected = new double[componentCount];
        actualAction(actual);
        expectedAction(expected);
        Assert.That(actual, Is.EqualTo(expected));
    }

    static void CompareBatch(int componentCount, int sampleCount, SpanAction actualAction, SpanAction expectedAction)
    {
        double[] actual = new double[componentCount * sampleCount];
        double[] expected = new double[actual.Length];
        actualAction(actual);
        expectedAction(expected);
        Assert.That(actual, Is.EqualTo(expected));
    }

    static double SquaredNorm(double[] values)
    {
        double result = 0.0;
        foreach (double value in values)
        {
            result += value * value;
        }

        return result;
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
