namespace random.Tests;

public class GeometryStatisticalContractTests
{
    [Test]
    public void CartesianAndRadialSamplersPopulateEqualMeasureCells()
    {
        const int sampleCount = 32000;
        int[] lineCells = new int[8];
        int[] boxCells = new int[16];
        int[] circleCells = new int[8];
        int[] diskCells = new int[32];
        int[] sphereCells = new int[32];
        int[] ballCells = new int[16];
        RandomGenerator lineGenerator = Create(101UL);
        RandomGenerator boxGenerator = Create(102UL);
        RandomGenerator circleGenerator = Create(103UL);
        RandomGenerator diskGenerator = Create(104UL);
        RandomGenerator sphereGenerator = Create(105UL);
        RandomGenerator ballGenerator = Create(106UL);

        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            double[] line = RandomGeometry.Line(lineGenerator, [0.0], [1.0]);
            double[] box = RandomGeometry.Box(boxGenerator, [0.0, 0.0], [1.0, 1.0]);
            double[] circle = RandomGeometry.UnitCircle(circleGenerator);
            double[] disk = RandomGeometry.UnitDisk(diskGenerator);
            double[] sphere = RandomGeometry.UnitSphere(sphereGenerator);
            double[] ball = RandomGeometry.UnitBall(ballGenerator, 3);
            lineCells[UnitIntervalCell(line[0], 8)]++;
            boxCells[UnitIntervalCell(box[0], 4) * 4 + UnitIntervalCell(box[1], 4)]++;
            circleCells[AngleCell(circle[0], circle[1], 8)]++;
            double diskRadiusSquared = SquaredNorm(disk);
            diskCells[UnitIntervalCell(diskRadiusSquared, 4) * 8 + AngleCell(disk[0], disk[1], 8)]++;
            sphereCells[AxialCell(sphere[2], 4) * 8 + AngleCell(sphere[0], sphere[1], 8)]++;
            double ballRadiusSquared = SquaredNorm(ball);
            double ballVolumeCoordinate = ballRadiusSquared * Math.Sqrt(ballRadiusSquared);
            ballCells[UnitIntervalCell(ballVolumeCoordinate, 2) * 8 + Orthant(ball)]++;
        }

        Assert.Multiple(() =>
        {
            AssertUniformCells("line", lineCells, sampleCount, 300);
            AssertUniformCells("box", boxCells, sampleCount, 220);
            AssertUniformCells("circle", circleCells, sampleCount, 300);
            AssertUniformCells("disk", diskCells, sampleCount, 150);
            AssertUniformCells("sphere", sphereCells, sampleCount, 150);
            AssertUniformCells("ball", ballCells, sampleCount, 220);
        });
    }

    [Test]
    public void SimplexAndPolygonSamplersPopulateEqualAreaCells()
    {
        const int sampleCount = 32000;
        int[] triangleCells = new int[4];
        int[] squareCells = new int[16];
        int[] concaveCells = new int[3];
        RandomGenerator triangleGenerator = Create(201UL);
        PolytopeSampler square = new PolytopeSampler(
            Create(202UL),
            [0.0, 0.0, 1.0, 0.0, 1.0, 1.0, 0.0, 1.0]);
        PolytopeSampler concave = new PolytopeSampler(
            Create(203UL),
            [0.0, 0.0, 2.0, 0.0, 2.0, 1.0, 1.0, 1.0, 1.0, 2.0, 0.0, 2.0]);

        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            double[] triangle = RandomGeometry.Triangle(
                triangleGenerator,
                [0.0, 0.0],
                [1.0, 0.0],
                [0.0, 1.0]);
            double[] squarePoint = square.Sample();
            triangleCells[TriangleCell(triangle)]++;
            squareCells[UnitIntervalCell(squarePoint[0], 4) * 4 + UnitIntervalCell(squarePoint[1], 4)]++;
        }

        const int concaveSampleCount = 36000;
        for (int sampleIndex = 0; sampleIndex < concaveSampleCount; sampleIndex++)
        {
            double[] point = concave.Sample();
            int cell;
            if (point[0] >= 1.0)
            {
                cell = 0;
            }
            else if (point[1] >= 1.0)
            {
                cell = 1;
            }
            else
            {
                cell = 2;
            }

            concaveCells[cell]++;
        }

        Assert.Multiple(() =>
        {
            AssertUniformCells("triangle", triangleCells, sampleCount, 400);
            AssertUniformCells("square polygon", squareCells, sampleCount, 220);
            AssertUniformCells("concave polygon", concaveCells, concaveSampleCount, 450);
        });
    }

    [Test]
    public void RotationSamplersPopulateSymmetricOrientationCells()
    {
        const int sampleCount = 32000;
        int[] quaternionCells = new int[16];
        int[] rotationCells = new int[32];
        int[] orthogonalCells = new int[16];
        RandomGenerator quaternionGenerator = Create(301UL);
        RandomGenerator rotationGenerator = Create(302UL);
        OrthogonalMatrixSampler orthogonal = new OrthogonalMatrixSampler(Create(303UL), 4);

        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            double[] quaternion = RandomGeometry.Quaternion(quaternionGenerator);
            double[] rotation = RandomGeometry.Rotation3D(rotationGenerator);
            double[] matrix = orthogonal.Sample();
            quaternionCells[Orthant(quaternion)]++;
            rotationCells[AxialCell(rotation[6], 4) * 8 + AngleCell(rotation[0], rotation[3], 8)]++;
            double[] firstColumn = [matrix[0], matrix[4], matrix[8], matrix[12]];
            orthogonalCells[Orthant(firstColumn)]++;
        }

        Assert.Multiple(() =>
        {
            AssertUniformCells("quaternion", quaternionCells, sampleCount, 220);
            AssertUniformCells("rotation", rotationCells, sampleCount, 150);
            AssertUniformCells("orthogonal matrix", orthogonalCells, sampleCount, 220);
        });
    }

    [Test]
    public void EveryGeometryFamilySatisfiesItsDefiningInvariant()
    {
        RandomGenerator generator = Create(401UL);
        PolytopeSampler polygon = new PolytopeSampler(
            generator,
            [0.0, 0.0, 2.0, 0.0, 2.0, 1.0, 1.0, 1.0, 1.0, 2.0, 0.0, 2.0]);
        OrthogonalMatrixSampler orthogonal = new OrthogonalMatrixSampler(generator, 5);

        for (int sampleIndex = 0; sampleIndex < 256; sampleIndex++)
        {
            double[] line = RandomGeometry.Line(generator, [1.0, 2.0, 3.0], [3.0, 6.0, 9.0]);
            double[] box = RandomGeometry.Box(generator, [-2.0, 3.0], [4.0, 7.0]);
            double[] circle = RandomGeometry.UnitCircle(generator);
            double[] disk = RandomGeometry.UnitDisk(generator);
            double[] sphere = RandomGeometry.UnitSphere(generator);
            double[] ball = RandomGeometry.UnitBall(generator, 7);
            double[] simplex = RandomGeometry.Simplex(
                generator,
                [0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0],
                3);
            double[] triangle = RandomGeometry.Triangle(
                generator,
                [1.0, 2.0, 3.0],
                [3.0, 2.0, 5.0],
                [1.0, 6.0, 7.0]);
            double[] tetrahedron = RandomGeometry.Tetrahedron(
                generator,
                [0.0, 0.0, 0.0, 9.0],
                [1.0, 0.0, 0.0, 9.0],
                [0.0, 1.0, 0.0, 9.0],
                [0.0, 0.0, 1.0, 9.0]);
            double[] polygonPoint = polygon.Sample();
            double[] quaternion = RandomGeometry.Quaternion(generator);
            double[] rotation = RandomGeometry.Rotation3D(generator);
            double[] matrix = orthogonal.Sample();

            Assert.Multiple(() =>
            {
                Assert.That(line[1], Is.EqualTo(2.0 * line[0]).Within(2.0e-15));
                Assert.That(line[2], Is.EqualTo(3.0 * line[0]).Within(4.0e-15));
                Assert.That(box[0], Is.InRange(-2.0, 4.0));
                Assert.That(box[1], Is.InRange(3.0, 7.0));
                Assert.That(SquaredNorm(circle), Is.EqualTo(1.0).Within(3.0e-15));
                Assert.That(SquaredNorm(disk), Is.LessThanOrEqualTo(1.0));
                Assert.That(SquaredNorm(sphere), Is.EqualTo(1.0).Within(3.0e-15));
                Assert.That(SquaredNorm(ball), Is.LessThanOrEqualTo(1.0));
                Assert.That(simplex, Is.All.GreaterThanOrEqualTo(0.0));
                Assert.That(simplex.Sum(), Is.LessThanOrEqualTo(1.0));
                Assert.That(triangle[2], Is.EqualTo(triangle[0] + triangle[1]).Within(2.0e-14));
                Assert.That(tetrahedron[3], Is.EqualTo(9.0));
                Assert.That(IsInsideConcavePolygon(polygonPoint), Is.True);
                Assert.That(SquaredNorm(quaternion), Is.EqualTo(1.0).Within(3.0e-15));
                AssertOrthogonal(rotation, 3, 5.0e-15);
                Assert.That(Determinant3(rotation), Is.EqualTo(1.0).Within(5.0e-15));
                AssertOrthogonal(matrix, 5, 3.0e-13);
            });
        }
    }

    [Test]
    public void DegenerateGeometryIsRejectedBeforeStateConsumption()
    {
        RandomGenerator generator = Create(501UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = [91.0, 92.0, 93.0, 94.0];
        object[] cases =
        [
            new object[] { (TestDelegate)(() => RandomGeometry.Box(generator, [1.0, 0.0], [0.0, 1.0], destination.AsSpan(0, 2))), "maximum" },
            new object[] { (TestDelegate)(() => RandomGeometry.UnitBall(generator, 0)), "dimension" },
            new object[] { (TestDelegate)(() => RandomGeometry.Simplex(generator, [0.0, 0.0, 1.0, 1.0, 2.0, 2.0], 2, destination.AsSpan(0, 2))), "vertices" },
            new object[] { (TestDelegate)(() => RandomGeometry.Triangle(generator, [0.0, 0.0], [1.0, 1.0], [2.0, 2.0], destination.AsSpan(0, 2))), "third" },
            new object[] { (TestDelegate)(() => RandomGeometry.Tetrahedron(generator, [0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [1.0, 1.0, 0.0], destination.AsSpan(0, 3))), "fourth" },
            new object[] { (TestDelegate)(() => RandomGeometry.Polygon(generator, [0.0, 0.0, 2.0, 2.0, 0.0, 2.0, 2.0, 0.0], destination.AsSpan(0, 2))), "vertices" },
            new object[] { (TestDelegate)(() => RandomGeometry.OrthogonalMatrix(generator, 0)), "dimension" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.Multiple(() =>
            {
                Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
                Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
                Assert.That(destination, Is.EqualTo(new double[] { 91.0, 92.0, 93.0, 94.0 }));
            });
        }
    }

    static int UnitIntervalCell(double value, int cellCount)
    {
        return Math.Min(cellCount - 1, (int)(value * cellCount));
    }

    static int AxialCell(double axial, int cellCount)
    {
        return UnitIntervalCell((axial + 1.0) * 0.5, cellCount);
    }

    static int AngleCell(double x, double y, int cellCount)
    {
        double angle = Math.Atan2(y, x);
        if (angle < 0.0)
        {
            angle += Math.Tau;
        }

        return UnitIntervalCell(angle / Math.Tau, cellCount);
    }

    static int Orthant(double[] values)
    {
        int result = 0;
        for (int index = 0; index < values.Length; index++)
        {
            if (values[index] >= 0.0)
            {
                result |= 1 << index;
            }
        }

        return result;
    }

    static int TriangleCell(double[] point)
    {
        if (point[0] >= 0.5)
        {
            return 0;
        }

        if (point[1] >= 0.5)
        {
            return 1;
        }

        if (point[0] + point[1] <= 0.5)
        {
            return 2;
        }

        return 3;
    }

    static void AssertUniformCells(string name, int[] counts, int sampleCount, int tolerance)
    {
        int expected = sampleCount / counts.Length;
        for (int index = 0; index < counts.Length; index++)
        {
            Assert.That(
                counts[index],
                Is.InRange(expected - tolerance, expected + tolerance),
                $"{name} cell {index}");
        }
    }

    static bool IsInsideConcavePolygon(double[] point)
    {
        return point[0] >= 0.0 && point[1] >= 0.0 && point[0] <= 2.0 && point[1] <= 2.0 &&
            (point[0] <= 1.0 || point[1] <= 1.0);
    }

    static double SquaredNorm(double[] values)
    {
        double result = 0.0;
        for (int index = 0; index < values.Length; index++)
        {
            result += values[index] * values[index];
        }

        return result;
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

    static double Determinant3(double[] matrix)
    {
        return matrix[0] * (matrix[4] * matrix[8] - matrix[5] * matrix[7]) -
            matrix[1] * (matrix[3] * matrix[8] - matrix[5] * matrix[6]) +
            matrix[2] * (matrix[3] * matrix[7] - matrix[4] * matrix[6]);
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
