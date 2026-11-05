namespace random.Tests;

public class SimplexSamplerTests
{
    [TestCase(RandomMathMode.Portable)]
    [TestCase(RandomMathMode.System)]
    public void StandardSimplexMatchesExponentialBarycentricReferenceExactly(RandomMathMode mathMode)
    {
        double[] vertices =
        [
            0.0, 0.0, 0.0,
            1.0, 0.0, 0.0,
            0.0, 1.0, 0.0,
            0.0, 0.0, 1.0
        ];
        RandomGenerator actualGenerator = Create(101UL, mathMode);
        RandomGenerator referenceGenerator = Create(101UL, mathMode);

        double[] actual = SimplexSampler.Simplex(actualGenerator, vertices, 3);
        double[] weights = ReferenceWeights(referenceGenerator, 4, mathMode);
        double[] expected = [weights[1], weights[2], weights[3]];

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.All(value => value >= 0.0), Is.True);
            Assert.That(actual.Sum(), Is.LessThanOrEqualTo(1.0));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [TestCase(RandomMathMode.Portable)]
    [TestCase(RandomMathMode.System)]
    public void TriangleAndTetrahedronMatchBarycentricReferenceExactly(RandomMathMode mathMode)
    {
        RandomGenerator triangleGenerator = Create(202UL, mathMode);
        RandomGenerator triangleReference = Create(202UL, mathMode);
        RandomGenerator tetrahedronGenerator = Create(303UL, mathMode);
        RandomGenerator tetrahedronReference = Create(303UL, mathMode);

        double[] triangle = SimplexSampler.Triangle(
            triangleGenerator,
            [0.0, 0.0],
            [1.0, 0.0],
            [0.0, 1.0]);
        double[] triangleWeights = ReferenceWeights(triangleReference, 3, mathMode);
        double[] tetrahedron = SimplexSampler.Tetrahedron(
            tetrahedronGenerator,
            [0.0, 0.0, 0.0],
            [1.0, 0.0, 0.0],
            [0.0, 1.0, 0.0],
            [0.0, 0.0, 1.0]);
        double[] tetrahedronWeights = ReferenceWeights(tetrahedronReference, 4, mathMode);

        Assert.Multiple(() =>
        {
            Assert.That(triangle, Is.EqualTo(new double[] { triangleWeights[1], triangleWeights[2] }));
            Assert.That(tetrahedron, Is.EqualTo(new double[] { tetrahedronWeights[1], tetrahedronWeights[2], tetrahedronWeights[3] }));
            Assert.That(triangleGenerator.ExportState().ToBytes(), Is.EqualTo(triangleReference.ExportState().ToBytes()));
            Assert.That(tetrahedronGenerator.ExportState().ToBytes(), Is.EqualTo(tetrahedronReference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void DestinationOverloadsMatchAllocatingFormsAndDoNotMutateVertices()
    {
        double[] vertices = [0.0, 0.0, 2.0, 0.0, 0.0, 3.0];
        double[] snapshot = (double[])vertices.Clone();
        RandomGenerator allocatingGenerator = Create(404UL, RandomMathMode.Portable);
        RandomGenerator destinationGenerator = Create(404UL, RandomMathMode.Portable);
        double[] simplexDestination = new double[2];
        double[] triangleDestination = new double[3];
        double[] tetrahedronDestination = new double[4];

        double[] simplex = SimplexSampler.Simplex(allocatingGenerator, vertices, 2);
        SimplexSampler.Simplex(destinationGenerator, vertices, 2, simplexDestination);
        double[] triangle = SimplexSampler.Triangle(allocatingGenerator, [0.0, 0.0, 0.0], [2.0, 0.0, 0.0], [0.0, 3.0, 0.0]);
        SimplexSampler.Triangle(destinationGenerator, [0.0, 0.0, 0.0], [2.0, 0.0, 0.0], [0.0, 3.0, 0.0], triangleDestination);
        double[] tetrahedron = SimplexSampler.Tetrahedron(
            allocatingGenerator,
            [0.0, 0.0, 0.0, 0.0],
            [1.0, 0.0, 0.0, 0.0],
            [0.0, 1.0, 0.0, 0.0],
            [0.0, 0.0, 1.0, 0.0]);
        SimplexSampler.Tetrahedron(
            destinationGenerator,
            [0.0, 0.0, 0.0, 0.0],
            [1.0, 0.0, 0.0, 0.0],
            [0.0, 1.0, 0.0, 0.0],
            [0.0, 0.0, 1.0, 0.0],
            tetrahedronDestination);

        Assert.Multiple(() =>
        {
            Assert.That(simplexDestination, Is.EqualTo(simplex));
            Assert.That(triangleDestination, Is.EqualTo(triangle));
            Assert.That(tetrahedronDestination, Is.EqualTo(tetrahedron));
            Assert.That(vertices, Is.EqualTo(snapshot));
            Assert.That(allocatingGenerator.ExportState().ToBytes(), Is.EqualTo(destinationGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void UniformSamplesHaveTheExpectedCentroids()
    {
        const int sampleCount = 30000;
        RandomGenerator triangleGenerator = Create(505UL, RandomMathMode.Portable);
        RandomGenerator tetrahedronGenerator = Create(506UL, RandomMathMode.Portable);
        RandomGenerator simplexGenerator = Create(507UL, RandomMathMode.Portable);
        double[] triangleSum = new double[2];
        double[] tetrahedronSum = new double[3];
        double[] simplexSum = new double[4];
        double[] simplexVertices =
        [
            0.0, 0.0, 0.0, 0.0,
            1.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            0.0, 0.0, 0.0, 1.0
        ];
        int sampleIndex = 0;
        while (sampleIndex < sampleCount)
        {
            double[] triangle = SimplexSampler.Triangle(triangleGenerator, [0.0, 0.0], [1.0, 0.0], [0.0, 1.0]);
            double[] tetrahedron = SimplexSampler.Tetrahedron(
                tetrahedronGenerator,
                [0.0, 0.0, 0.0],
                [1.0, 0.0, 0.0],
                [0.0, 1.0, 0.0],
                [0.0, 0.0, 1.0]);
            double[] simplex = SimplexSampler.Simplex(simplexGenerator, simplexVertices, 4);
            Add(triangleSum, triangle);
            Add(tetrahedronSum, tetrahedron);
            Add(simplexSum, simplex);
            sampleIndex++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(triangleSum.Select(value => value / sampleCount), Is.All.EqualTo(1.0 / 3.0).Within(0.006));
            Assert.That(tetrahedronSum.Select(value => value / sampleCount), Is.All.EqualTo(0.25).Within(0.006));
            Assert.That(simplexSum.Select(value => value / sampleCount), Is.All.EqualTo(0.2).Within(0.006));
        });
    }

    [Test]
    public void EmbeddedShapesRemainInsideTheirAffineHull()
    {
        RandomGenerator generator = Create(606UL, RandomMathMode.Portable);
        int index = 0;
        while (index < 1000)
        {
            double[] triangle = SimplexSampler.Triangle(
                generator,
                [1.0, 2.0, 3.0],
                [3.0, 2.0, 5.0],
                [1.0, 6.0, 7.0]);
            double[] tetrahedron = SimplexSampler.Tetrahedron(
                generator,
                [0.0, 0.0, 0.0, 9.0],
                [1.0, 0.0, 0.0, 9.0],
                [0.0, 1.0, 0.0, 9.0],
                [0.0, 0.0, 1.0, 9.0]);
            Assert.Multiple(() =>
            {
                Assert.That(triangle[2], Is.EqualTo(triangle[0] + triangle[1]).Within(2e-14));
                Assert.That(tetrahedron[3], Is.EqualTo(9.0));
                Assert.That(tetrahedron.Take(3).Sum(), Is.LessThanOrEqualTo(1.0 + 2e-15));
                Assert.That(tetrahedron.Take(3), Is.All.GreaterThanOrEqualTo(0.0));
            });
            index++;
        }
    }

    [Test]
    public void ExtremeFiniteCoordinatesProduceFiniteConvexPoints()
    {
        RandomGenerator generator = Create(707UL, RandomMathMode.Portable);
        int index = 0;
        while (index < 1000)
        {
            double[] point = SimplexSampler.Triangle(
                generator,
                [-double.MaxValue, -double.MaxValue],
                [double.MaxValue, -double.MaxValue],
                [0.0, double.MaxValue]);
            Assert.Multiple(() =>
            {
                Assert.That(point.All(double.IsFinite), Is.True);
                Assert.That(point[0], Is.InRange(-double.MaxValue, double.MaxValue));
                Assert.That(point[1], Is.InRange(-double.MaxValue, double.MaxValue));
            });
            index++;
        }
    }

    [Test]
    public void DegenerateShapesAreRejectedBeforeRandomConsumption()
    {
        RandomGenerator generator = Create(808UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = [91.0, 92.0, 93.0];
        object[] cases =
        [
            new object[] { (TestDelegate)(() => SimplexSampler.Simplex(generator, [0.0, 0.0, 1.0, 1.0, 2.0, 2.0], 2)), "vertices" },
            new object[] { (TestDelegate)(() => SimplexSampler.Triangle(generator, [0.0, 0.0], [1.0, 1.0], [2.0, 2.0])), "third" },
            new object[] { (TestDelegate)(() => SimplexSampler.Triangle(generator, [0.0], [1.0], [2.0])), "third" },
            new object[] { (TestDelegate)(() => SimplexSampler.Tetrahedron(generator, [0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [1.0, 1.0, 0.0])), "fourth" },
            new object[] { (TestDelegate)(() => SimplexSampler.Tetrahedron(generator, [0.0, 0.0], [1.0, 0.0], [0.0, 1.0], [1.0, 1.0])), "fourth" },
            new object[] { (TestDelegate)(() => SimplexSampler.Triangle(generator, [0.0, 0.0], [1.0, 1.0], [2.0, 2.0], destination)), "third" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(destination, Is.EqualTo(new double[] { 91.0, 92.0, 93.0 }));
        }
    }

    [Test]
    public void InvalidArgumentsFailBeforeRandomConsumptionOrDestinationMutation()
    {
        RandomGenerator generator = Create(909UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = [81.0, 82.0, 83.0];
        object[] cases =
        [
            new object[] { (TestDelegate)(() => SimplexSampler.Simplex(NullReference.Value<global::random.IRandomSource>(), [0.0, 1.0], 1)), "generator" },
            new object[] { (TestDelegate)(() => SimplexSampler.Simplex(generator, NullReference.Value<double[]>(), 1)), "vertices" },
            new object[] { (TestDelegate)(() => SimplexSampler.Simplex(generator, [0.0, 1.0], 0)), "dimension" },
            new object[] { (TestDelegate)(() => SimplexSampler.Simplex(generator, [0.0, 1.0, 2.0], 1)), "vertices" },
            new object[] { (TestDelegate)(() => SimplexSampler.Simplex(generator, [0.0, double.NaN], 1)), "vertices" },
            new object[] { (TestDelegate)(() => SimplexSampler.Simplex(generator, [0.0, 0.0, 1.0, 0.0, 0.0, 1.0], 2, destination)), "destination" },
            new object[] { (TestDelegate)(() => SimplexSampler.Triangle(generator, NullReference.Value<double[]>(), [1.0, 0.0], [0.0, 1.0])), "first" },
            new object[] { (TestDelegate)(() => SimplexSampler.Triangle(generator, [0.0, 0.0], [1.0], [0.0, 1.0])), "second" },
            new object[] { (TestDelegate)(() => SimplexSampler.Triangle(generator, [0.0, 0.0], [1.0, 0.0], [double.PositiveInfinity, 1.0])), "third" },
            new object[] { (TestDelegate)(() => SimplexSampler.Tetrahedron(generator, [0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], NullReference.Value<double[]>())), "fourth" },
            new object[] { (TestDelegate)(() => SimplexSampler.Tetrahedron(generator, [0.0, 0.0, 0.0], [1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0], destination.AsSpan(0, 2))), "destination" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(destination, Is.EqualTo(new double[] { 81.0, 82.0, 83.0 }));
        }
    }

    static double[] ReferenceWeights(RandomGenerator generator, int count, RandomMathMode mathMode)
    {
        double[] weights = new double[count];
        double total = 0.0;
        double compensation = 0.0;
        for (int index = 0; index < count; index++)
        {
            double unit = generator.NextDoubleOpenOpen();
            double exponential = -(mathMode == RandomMathMode.Portable ? PortableMath.Log(unit) : Math.Log(unit));
            weights[index] = exponential;
            double adjusted = exponential - compensation;
            double next = total + adjusted;
            compensation = (next - total) - adjusted;
            total = next;
        }

        double prefix = 0.0;
        for (int index = 0; index < count - 1; index++)
        {
            weights[index] /= total;
            prefix += weights[index];
        }

        if (prefix > 1.0)
        {
            double scale = PortableMath.NextDown(1.0) / prefix;
            prefix = 0.0;
            for (int index = 0; index < count - 1; index++)
            {
                weights[index] *= scale;
                prefix += weights[index];
            }
        }

        weights[count - 1] = 1.0 - prefix;
        return weights;
    }

    static void Add(double[] sums, double[] values)
    {
        for (int index = 0; index < sums.Length; index++)
        {
            sums[index] += values[index];
        }
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mathMode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mathMode);
    }
}
