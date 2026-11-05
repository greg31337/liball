namespace random.Tests;

public class PolytopeSamplerTests
{
    [Test]
    public void TriangulationCoversConvexAndConcavePolygonsExactly()
    {
        double[][] polygons =
        [
            [0.0, 0.0, 2.0, 0.0, 2.0, 2.0, 0.0, 2.0],
            [0.0, 0.0, 2.0, 0.0, 2.0, 1.0, 1.0, 1.0, 1.0, 2.0, 0.0, 2.0],
            [0.0, 2.0, 1.0, 2.0, 1.0, 1.0, 2.0, 1.0, 2.0, 0.0, 0.0, 0.0]
        ];

        foreach (double[] polygon in polygons)
        {
            int[] triangles = PolytopeSampler.Triangulate(polygon);
            Assert.Multiple(() =>
            {
                Assert.That(triangles, Has.Length.EqualTo((polygon.Length / 2 - 2) * 3));
                Assert.That(triangles, Is.All.InRange(0, polygon.Length / 2 - 1));
                Assert.That(TriangulationArea(polygon, triangles), Is.EqualTo(Math.Abs(PolygonArea(polygon))).Within(1e-13));
            });
        }
    }

    [Test]
    public void TriangulationAcceptsClockwiseAndCollinearBoundaryVertices()
    {
        double[] counterclockwise = [0.0, 0.0, 1.0, 0.0, 2.0, 0.0, 2.0, 1.0, 0.0, 1.0];
        double[] clockwise = ReverseVertices(counterclockwise);

        int[] forward = PolytopeSampler.Triangulate(counterclockwise);
        int[] reverse = PolytopeSampler.Triangulate(clockwise);

        Assert.Multiple(() =>
        {
            Assert.That(forward, Has.Length.EqualTo(9));
            Assert.That(reverse, Has.Length.EqualTo(9));
            Assert.That(TriangulationArea(counterclockwise, forward), Is.EqualTo(2.0).Within(1e-14));
            Assert.That(TriangulationArea(clockwise, reverse), Is.EqualTo(2.0).Within(1e-14));
            Assert.That(TriangleOrientation(counterclockwise, forward, 0), Is.Positive);
            Assert.That(TriangleOrientation(clockwise, reverse, 0), Is.Negative);
        });
    }

    [Test]
    public void SamplingMatchesAreaSelectionAndReflectedTriangleCoordinatesExactly()
    {
        double[] polygon = [0.0, 0.0, 2.0, 0.0, 2.0, 1.0, 1.0, 1.0, 1.0, 2.0, 0.0, 2.0];
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator referenceGenerator = Create(101UL);
        PolytopeSampler sampler = new PolytopeSampler(actualGenerator, polygon);
        int[] triangles = sampler.TriangleIndexes();

        for (int sampleIndex = 0; sampleIndex < 1000; sampleIndex++)
        {
            double[] expected = ReferenceSample(referenceGenerator, polygon, triangles);
            double[] actual = sampler.Sample();
            Assert.That(actual, Is.EqualTo(expected));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void APolygonThatIsOneTriangleUsesOnlyTwoCoordinateDraws()
    {
        double[] polygon = [0.0, 0.0, 1.0, 0.0, 0.0, 1.0];
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator referenceGenerator = Create(202UL);
        PolytopeSampler sampler = new PolytopeSampler(actualGenerator, polygon);
        double first = referenceGenerator.NextDoubleClosedOpen();
        double second = referenceGenerator.NextDoubleClosedOpen();
        if (first + second > 1.0)
        {
            first = 1.0 - first;
            second = 1.0 - second;
        }

        double[] actual = sampler.Sample();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(new double[] { first, second }));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ConcavePolygonSamplesStayInsideAndMatchItsCentroid()
    {
        const int sampleCount = 50000;
        double[] polygon = [0.0, 0.0, 2.0, 0.0, 2.0, 1.0, 1.0, 1.0, 1.0, 2.0, 0.0, 2.0];
        PolytopeSampler sampler = new PolytopeSampler(Create(303UL), polygon);
        double xTotal = 0.0;
        double yTotal = 0.0;
        int sampleIndex = 0;
        while (sampleIndex < sampleCount)
        {
            double[] point = sampler.Sample();
            Assert.That(IsInside(polygon, point[0], point[1]), Is.True);
            xTotal += point[0];
            yTotal += point[1];
            sampleIndex++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(xTotal / sampleCount, Is.EqualTo(5.0 / 6.0).Within(0.01));
            Assert.That(yTotal / sampleCount, Is.EqualTo(5.0 / 6.0).Within(0.01));
        });
    }

    [Test]
    public void BatchFillMatchesRepeatedSamplesExactly()
    {
        double[] polygon = [-1.0, -1.0, 2.0, -1.0, 3.0, 1.0, 1.0, 3.0, -2.0, 1.0];
        RandomGenerator fillGenerator = Create(404UL);
        RandomGenerator repeatedGenerator = Create(404UL);
        PolytopeSampler fillSampler = new PolytopeSampler(fillGenerator, polygon);
        PolytopeSampler repeatedSampler = new PolytopeSampler(repeatedGenerator, polygon);
        double[] actual = new double[fillSampler.RequiredOutputLength(257)];
        double[] expected = new double[actual.Length];

        fillSampler.Fill(257, actual);
        for (int sampleIndex = 0; sampleIndex < 257; sampleIndex++)
        {
            repeatedSampler.SampleInto(expected.AsSpan(sampleIndex * 2, 2));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
            Assert.That(fillSampler.VertexCount(), Is.EqualTo(5));
            Assert.That(fillSampler.TriangleCount(), Is.EqualTo(3));
        });
    }

    [Test]
    public void ConstructionAndIndexResultsAreDefensiveCopies()
    {
        double[] polygon = [0.0, 0.0, 2.0, 0.0, 2.0, 2.0, 0.0, 2.0];
        double[] original = (double[])polygon.Clone();
        RandomGenerator actualGenerator = Create(505UL);
        RandomGenerator referenceGenerator = Create(505UL);
        PolytopeSampler actual = new PolytopeSampler(actualGenerator, polygon);
        PolytopeSampler expected = new PolytopeSampler(referenceGenerator, original);
        int[] indexes = actual.TriangleIndexes();
        Array.Fill(polygon, double.NaN);
        Array.Fill(indexes, -1);

        for (int sampleIndex = 0; sampleIndex < 100; sampleIndex++)
        {
            Assert.That(actual.Sample(), Is.EqualTo(expected.Sample()));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual.TriangleIndexes(), Is.EqualTo(expected.TriangleIndexes()));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ExtremeFiniteCoordinatesRemainFinite()
    {
        double[] polygon =
        [
            -double.MaxValue, -double.MaxValue,
            double.MaxValue, -double.MaxValue,
            double.MaxValue, double.MaxValue,
            -double.MaxValue, double.MaxValue
        ];
        PolytopeSampler sampler = new PolytopeSampler(Create(606UL), polygon);
        int sampleIndex = 0;
        while (sampleIndex < 1000)
        {
            double[] point = sampler.Sample();
            Assert.Multiple(() =>
            {
                Assert.That(point.All(double.IsFinite), Is.True);
                Assert.That(point[0], Is.InRange(-double.MaxValue, double.MaxValue));
                Assert.That(point[1], Is.InRange(-double.MaxValue, double.MaxValue));
            });
            sampleIndex++;
        }
    }

    [Test]
    public void InvalidPolygonsAreRejectedBeforeRandomConsumption()
    {
        RandomGenerator generator = Create(707UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[][] invalid =
        [
            [],
            [0.0, 0.0, 1.0, 0.0, 0.0],
            [0.0, 0.0, 1.0, 0.0],
            [0.0, 0.0, 1.0, 0.0, double.NaN, 1.0],
            [0.0, 0.0, 1.0, 0.0, 0.0, 0.0],
            [0.0, 0.0, 1.0, 0.0, 2.0, 0.0],
            [0.0, 0.0, 2.0, 2.0, 0.0, 2.0, 2.0, 0.0],
            [0.0, 0.0, 2.0, 0.0, 1.0, 0.0, 1.0, 1.0]
        ];

        Assert.That(Assert.Throws<RandomArgumentException>(() => new PolytopeSampler(NullReference.Value<global::random.IRandomSource>(), [0.0, 0.0, 1.0, 0.0, 0.0, 1.0]))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new PolytopeSampler(generator, NullReference.Value<double[]>()))?.ArgumentName(), Is.EqualTo("vertices"));
        foreach (double[] polygon in invalid)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => new PolytopeSampler(generator, polygon));
            Assert.That(error.ArgumentName(), Is.EqualTo("vertices"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void InvalidOutputRequestsDoNotConsumeStateOrMutateDestinations()
    {
        RandomGenerator generator = Create(808UL);
        PolytopeSampler sampler = new PolytopeSampler(generator, [0.0, 0.0, 1.0, 0.0, 0.0, 1.0]);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
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

    static double[] ReferenceSample(RandomGenerator generator, double[] polygon, int[] triangles)
    {
        double[] areas = new double[triangles.Length / 3];
        for (int triangle = 0; triangle < areas.Length; triangle++)
        {
            areas[triangle] = Math.Abs(TriangleOrientation(polygon, triangles, triangle));
        }

        CategoricalProbabilityTable table = new CategoricalProbabilityTable(areas, "probabilities");
        int selected = 0;
        if (areas.Length > 1)
        {
            double unit = generator.NextDoubleClosedOpen();
            while (selected < areas.Length - 1 && unit >= table.CumulativeProbability(selected))
            {
                selected++;
            }
        }

        double firstWeight = generator.NextDoubleClosedOpen();
        double secondWeight = generator.NextDoubleClosedOpen();
        if (firstWeight + secondWeight > 1.0)
        {
            firstWeight = 1.0 - firstWeight;
            secondWeight = 1.0 - secondWeight;
        }

        double originWeight = 1.0 - firstWeight - secondWeight;
        int offset = selected * 3;
        int first = triangles[offset];
        int second = triangles[offset + 1];
        int third = triangles[offset + 2];
        return
        [
            originWeight * polygon[first * 2] + firstWeight * polygon[second * 2] + secondWeight * polygon[third * 2],
            originWeight * polygon[first * 2 + 1] + firstWeight * polygon[second * 2 + 1] + secondWeight * polygon[third * 2 + 1]
        ];
    }

    static double TriangulationArea(double[] polygon, int[] triangles)
    {
        double area = 0.0;
        for (int triangle = 0; triangle < triangles.Length / 3; triangle++)
        {
            area += Math.Abs(TriangleOrientation(polygon, triangles, triangle)) * 0.5;
        }

        return area;
    }

    static double TriangleOrientation(double[] polygon, int[] triangles, int triangle)
    {
        int offset = triangle * 3;
        int first = triangles[offset];
        int second = triangles[offset + 1];
        int third = triangles[offset + 2];
        double firstX = polygon[second * 2] - polygon[first * 2];
        double firstY = polygon[second * 2 + 1] - polygon[first * 2 + 1];
        double secondX = polygon[third * 2] - polygon[first * 2];
        double secondY = polygon[third * 2 + 1] - polygon[first * 2 + 1];
        return firstX * secondY - firstY * secondX;
    }

    static double PolygonArea(double[] polygon)
    {
        double area = 0.0;
        for (int vertex = 0; vertex < polygon.Length / 2; vertex++)
        {
            int next = (vertex + 1) % (polygon.Length / 2);
            area += polygon[vertex * 2] * polygon[next * 2 + 1] - polygon[next * 2] * polygon[vertex * 2 + 1];
        }

        return area * 0.5;
    }

    static bool IsInside(double[] polygon, double x, double y)
    {
        bool inside = false;
        int vertexCount = polygon.Length / 2;
        int previous = vertexCount - 1;
        for (int vertex = 0; vertex < vertexCount; vertex++)
        {
            double currentX = polygon[vertex * 2];
            double currentY = polygon[vertex * 2 + 1];
            double previousX = polygon[previous * 2];
            double previousY = polygon[previous * 2 + 1];
            if ((currentY > y) != (previousY > y) &&
                x < (previousX - currentX) * (y - currentY) / (previousY - currentY) + currentX)
            {
                inside = !inside;
            }

            previous = vertex;
        }

        return inside;
    }

    static double[] ReverseVertices(double[] polygon)
    {
        double[] result = new double[polygon.Length];
        int vertexCount = polygon.Length / 2;
        for (int vertex = 0; vertex < vertexCount; vertex++)
        {
            int source = vertexCount - 1 - vertex;
            result[vertex * 2] = polygon[source * 2];
            result[vertex * 2 + 1] = polygon[source * 2 + 1];
        }

        return result;
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
