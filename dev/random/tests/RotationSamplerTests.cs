namespace random.Tests;

public class RotationSamplerTests
{
    [TestCase(RandomMathMode.Portable)]
    [TestCase(RandomMathMode.System)]
    public void QuaternionMatchesShoemakeReferenceExactly(RandomMathMode mathMode)
    {
        RandomGenerator actualGenerator = Create(101UL, mathMode);
        RandomGenerator referenceGenerator = Create(101UL, mathMode);

        double[] actual = RotationSampler.Quaternion(actualGenerator);
        double[] expected = ReferenceQuaternion(referenceGenerator, mathMode);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(SquaredNorm(actual), Is.EqualTo(1.0).Within(3e-15));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [TestCase(RandomMathMode.Portable)]
    [TestCase(RandomMathMode.System)]
    public void RotationMatrixMatchesQuaternionConversionExactly(RandomMathMode mathMode)
    {
        RandomGenerator matrixGenerator = Create(202UL, mathMode);
        RandomGenerator quaternionGenerator = Create(202UL, mathMode);

        double[] matrix = RotationSampler.Rotation3D(matrixGenerator);
        double[] quaternion = RotationSampler.Quaternion(quaternionGenerator);
        double[] expected = QuaternionMatrix(quaternion);

        Assert.Multiple(() =>
        {
            Assert.That(matrix, Is.EqualTo(expected));
            AssertOrthogonal(matrix, 3, 4e-15);
            Assert.That(Determinant3(matrix), Is.EqualTo(1.0).Within(5e-15));
            Assert.That(matrixGenerator.ExportState().ToBytes(), Is.EqualTo(quaternionGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void DestinationOverloadsMatchAllocatingFormsExactly()
    {
        RandomGenerator allocatingGenerator = Create(303UL, RandomMathMode.Portable);
        RandomGenerator destinationGenerator = Create(303UL, RandomMathMode.Portable);
        double[] quaternion = RotationSampler.Quaternion(allocatingGenerator);
        double[] matrix = RotationSampler.Rotation3D(allocatingGenerator);
        double[] quaternionDestination = new double[4];
        double[] matrixDestination = new double[9];
        RotationSampler.Quaternion(destinationGenerator, quaternionDestination);
        RotationSampler.Rotation3D(destinationGenerator, matrixDestination);

        Assert.Multiple(() =>
        {
            Assert.That(quaternionDestination, Is.EqualTo(quaternion));
            Assert.That(matrixDestination, Is.EqualTo(matrix));
            Assert.That(allocatingGenerator.ExportState().ToBytes(), Is.EqualTo(destinationGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void QuaternionAndRotationMomentsMatchHaarRotationLaw()
    {
        const int sampleCount = 40000;
        RandomGenerator quaternionGenerator = Create(404UL, RandomMathMode.Portable);
        RandomGenerator matrixGenerator = Create(405UL, RandomMathMode.Portable);
        double[] quaternionSums = new double[4];
        double[] quaternionSquares = new double[4];
        double matrixEntrySum = 0.0;
        double matrixEntrySquares = 0.0;
        int sampleIndex = 0;
        while (sampleIndex < sampleCount)
        {
            double[] quaternion = RotationSampler.Quaternion(quaternionGenerator);
            double[] matrix = RotationSampler.Rotation3D(matrixGenerator);
            for (int component = 0; component < quaternion.Length; component++)
            {
                quaternionSums[component] += quaternion[component];
                quaternionSquares[component] += quaternion[component] * quaternion[component];
            }

            matrixEntrySum += matrix[0];
            matrixEntrySquares += matrix[0] * matrix[0];
            sampleIndex++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(quaternionSums.Select(value => value / sampleCount), Is.All.EqualTo(0.0).Within(0.01));
            Assert.That(quaternionSquares.Select(value => value / sampleCount), Is.All.EqualTo(0.25).Within(0.008));
            Assert.That(matrixEntrySum / sampleCount, Is.EqualTo(0.0).Within(0.01));
            Assert.That(matrixEntrySquares / sampleCount, Is.EqualTo(1.0 / 3.0).Within(0.008));
        });
    }

    [Test]
    public void InvalidArgumentsDoNotConsumeStateOrMutateDestinations()
    {
        RandomGenerator generator = Create(505UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = [91.0, 92.0, 93.0];
        object[] cases =
        [
            new object[] { (TestDelegate)(() => RotationSampler.Quaternion(NullReference.Value<global::random.IRandomSource>())), "generator" },
            new object[] { (TestDelegate)(() => RotationSampler.Quaternion(generator, destination)), "destination" },
            new object[] { (TestDelegate)(() => RotationSampler.Rotation3D(NullReference.Value<global::random.IRandomSource>())), "generator" },
            new object[] { (TestDelegate)(() => RotationSampler.Rotation3D(generator, destination)), "destination" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(destination, Is.EqualTo(new double[] { 91.0, 92.0, 93.0 }));
        }
    }

    static double[] ReferenceQuaternion(RandomGenerator generator, RandomMathMode mathMode)
    {
        double radialUnit = generator.NextDoubleClosedOpen();
        double firstAngle = Math.Tau * generator.NextDoubleClosedOpen();
        double secondAngle = Math.Tau * generator.NextDoubleClosedOpen();
        double firstRadius = Sqrt(mathMode, 1.0 - radialUnit);
        double secondRadius = Sqrt(mathMode, radialUnit);
        double[] result =
        [
            firstRadius * Sin(mathMode, firstAngle),
            firstRadius * Cos(mathMode, firstAngle),
            secondRadius * Sin(mathMode, secondAngle),
            secondRadius * Cos(mathMode, secondAngle)
        ];
        double inverseNorm = 1.0 / Sqrt(mathMode, SquaredNorm(result));
        for (int index = 0; index < result.Length; index++)
        {
            result[index] *= inverseNorm;
        }

        return result;
    }

    static double[] QuaternionMatrix(double[] quaternion)
    {
        double x = quaternion[0];
        double y = quaternion[1];
        double z = quaternion[2];
        double w = quaternion[3];
        return
        [
            1.0 - 2.0 * (y * y + z * z),
            2.0 * (x * y - z * w),
            2.0 * (x * z + y * w),
            2.0 * (x * y + z * w),
            1.0 - 2.0 * (x * x + z * z),
            2.0 * (y * z - x * w),
            2.0 * (x * z - y * w),
            2.0 * (y * z + x * w),
            1.0 - 2.0 * (x * x + y * y)
        ];
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

    static double SquaredNorm(double[] values)
    {
        return values.Sum(value => value * value);
    }

    static double Sqrt(RandomMathMode mathMode, double value)
    {
        return mathMode == RandomMathMode.Portable ? PortableMath.Sqrt(value) : Math.Sqrt(value);
    }

    static double Sin(RandomMathMode mathMode, double value)
    {
        return mathMode == RandomMathMode.Portable ? PortableMath.Sin(value) : Math.Sin(value);
    }

    static double Cos(RandomMathMode mathMode, double value)
    {
        return mathMode == RandomMathMode.Portable ? PortableMath.Cos(value) : Math.Cos(value);
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mathMode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mathMode);
    }
}
