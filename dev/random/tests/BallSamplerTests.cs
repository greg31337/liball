namespace random.Tests;

public class BallSamplerTests
{
    [TestCase(RandomMathMode.Portable)]
    [TestCase(RandomMathMode.System)]
    public void LineUsesOneSharedUniformInterpolationValue(RandomMathMode mathMode)
    {
        double[] start = [-10.0, 20.0, 3.5, 100.0];
        double[] end = [30.0, -40.0, 3.5, 101.0];
        RandomGenerator actualGenerator = Create(101UL, mathMode);
        RandomGenerator referenceGenerator = Create(101UL, mathMode);
        double unit = referenceGenerator.NextDoubleClosedOpen();
        double[] expected = start.Zip(end, (first, second) => first + (second - first) * unit).ToArray();

        double[] actual = BallSampler.Line(actualGenerator, start, end);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            Assert.That(start, Is.EqualTo(new double[] { -10.0, 20.0, 3.5, 100.0 }));
            Assert.That(end, Is.EqualTo(new double[] { 30.0, -40.0, 3.5, 101.0 }));
        });
    }

    [Test]
    public void DegenerateLineReturnsItsEndpointWithoutConsumption()
    {
        RandomGenerator generator = Create(202UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] point = [-double.MaxValue, 0.0, double.MaxValue];
        double[] destination = new double[3];

        double[] allocated = BallSampler.Line(generator, point, point);
        BallSampler.Line(generator, point, point, destination);

        Assert.Multiple(() =>
        {
            Assert.That(allocated, Is.EqualTo(point));
            Assert.That(destination, Is.EqualTo(point));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void BoxMatchesIndependentFiniteRangeDrawsExactly()
    {
        double[] minimum = [-double.MaxValue, -4.0, 7.0, 0.0];
        double[] maximum = [double.MaxValue, 6.0, 7.0, double.Epsilon];
        RandomGenerator actualGenerator = Create(303UL, RandomMathMode.Portable);
        RandomGenerator referenceGenerator = Create(303UL, RandomMathMode.Portable);
        double[] expected = new double[minimum.Length];
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = referenceGenerator.NextDoubleInRange(minimum[index], maximum[index]);
        }

        double[] actual = BallSampler.Box(actualGenerator, minimum, maximum);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.All(double.IsFinite), Is.True);
            Assert.That(actual[0], Is.InRange(minimum[0], maximum[0]));
            Assert.That(actual[1], Is.InRange(minimum[1], maximum[1]));
            Assert.That(actual[2], Is.EqualTo(7.0));
            Assert.That(actual[3], Is.InRange(minimum[3], maximum[3]));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [TestCase(RandomMathMode.Portable)]
    [TestCase(RandomMathMode.System)]
    public void FixedUnitShapesMatchTheirCoordinateTransformsExactly(RandomMathMode mathMode)
    {
        RandomGenerator circleGenerator = Create(404UL, mathMode);
        RandomGenerator circleReference = Create(404UL, mathMode);
        RandomGenerator diskGenerator = Create(505UL, mathMode);
        RandomGenerator diskReference = Create(505UL, mathMode);
        RandomGenerator sphereGenerator = Create(606UL, mathMode);
        RandomGenerator sphereReference = Create(606UL, mathMode);

        double circleAngle = Math.Tau * circleReference.NextDoubleClosedOpen();
        double[] expectedCircle = [Cos(mathMode, circleAngle), Sin(mathMode, circleAngle)];
        double diskRadius = Sqrt(mathMode, diskReference.NextDoubleClosedOpen());
        double diskAngle = Math.Tau * diskReference.NextDoubleClosedOpen();
        double[] expectedDisk = [diskRadius * Cos(mathMode, diskAngle), diskRadius * Sin(mathMode, diskAngle)];
        double axial = 2.0 * sphereReference.NextDoubleClosedOpen() - 1.0;
        double sphereAngle = Math.Tau * sphereReference.NextDoubleClosedOpen();
        double planar = Sqrt(mathMode, Math.Max(0.0, 1.0 - axial * axial));
        double[] expectedSphere = [planar * Cos(mathMode, sphereAngle), planar * Sin(mathMode, sphereAngle), axial];

        double[] circle = BallSampler.UnitCircle(circleGenerator);
        double[] disk = BallSampler.UnitDisk(diskGenerator);
        double[] sphere = BallSampler.UnitSphere(sphereGenerator);

        Assert.Multiple(() =>
        {
            Assert.That(circle, Is.EqualTo(expectedCircle));
            Assert.That(disk, Is.EqualTo(expectedDisk));
            Assert.That(sphere, Is.EqualTo(expectedSphere));
            Assert.That(SquaredNorm(circle), Is.EqualTo(1.0).Within(3e-15));
            Assert.That(SquaredNorm(disk), Is.LessThanOrEqualTo(1.0));
            Assert.That(SquaredNorm(sphere), Is.EqualTo(1.0).Within(3e-15));
            Assert.That(circleGenerator.ExportState().ToBytes(), Is.EqualTo(circleReference.ExportState().ToBytes()));
            Assert.That(diskGenerator.ExportState().ToBytes(), Is.EqualTo(diskReference.ExportState().ToBytes()));
            Assert.That(sphereGenerator.ExportState().ToBytes(), Is.EqualTo(sphereReference.ExportState().ToBytes()));
        });
    }

    [TestCase(RandomMathMode.Portable)]
    [TestCase(RandomMathMode.System)]
    public void AllocatingAndDestinationUnitBallsMatchExactly(RandomMathMode mathMode)
    {
        RandomGenerator allocatingGenerator = Create(707UL, mathMode);
        RandomGenerator destinationGenerator = Create(707UL, mathMode);
        for (int dimension = 1; dimension <= 17; dimension++)
        {
            double[] destination = new double[dimension];
            double[] allocated = BallSampler.UnitBall(allocatingGenerator, dimension);
            BallSampler.UnitBall(destinationGenerator, destination);
            Assert.Multiple(() =>
            {
                Assert.That(allocated, Is.EqualTo(destination));
                Assert.That(SquaredNorm(allocated), Is.LessThanOrEqualTo(1.0));
            });
        }

        Assert.That(allocatingGenerator.ExportState().ToBytes(), Is.EqualTo(destinationGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void UnitShapeMomentsMatchUniformGeometry()
    {
        const int sampleCount = 40000;
        RandomGenerator circleGenerator = Create(808UL, RandomMathMode.Portable);
        RandomGenerator diskGenerator = Create(809UL, RandomMathMode.Portable);
        RandomGenerator sphereGenerator = Create(810UL, RandomMathMode.Portable);
        RandomGenerator ballGenerator = Create(811UL, RandomMathMode.Portable);
        double circleX = 0.0;
        double diskSquaredRadius = 0.0;
        double sphereZ = 0.0;
        double sphereZSquared = 0.0;
        double ballSquaredRadius = 0.0;
        int index = 0;
        while (index < sampleCount)
        {
            double[] circle = BallSampler.UnitCircle(circleGenerator);
            double[] disk = BallSampler.UnitDisk(diskGenerator);
            double[] sphere = BallSampler.UnitSphere(sphereGenerator);
            double[] ball = BallSampler.UnitBall(ballGenerator, 5);
            circleX += circle[0];
            diskSquaredRadius += SquaredNorm(disk);
            sphereZ += sphere[2];
            sphereZSquared += sphere[2] * sphere[2];
            ballSquaredRadius += SquaredNorm(ball);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(circleX / sampleCount, Is.EqualTo(0.0).Within(0.01));
            Assert.That(diskSquaredRadius / sampleCount, Is.EqualTo(0.5).Within(0.01));
            Assert.That(sphereZ / sampleCount, Is.EqualTo(0.0).Within(0.01));
            Assert.That(sphereZSquared / sampleCount, Is.EqualTo(1.0 / 3.0).Within(0.01));
            Assert.That(ballSquaredRadius / sampleCount, Is.EqualTo(5.0 / 7.0).Within(0.01));
        });
    }

    [Test]
    public void FixedShapeDestinationOverloadsMatchAllocatingForms()
    {
        RandomGenerator allocatedGenerator = Create(901UL, RandomMathMode.Portable);
        RandomGenerator destinationGenerator = Create(901UL, RandomMathMode.Portable);
        double[] circle = BallSampler.UnitCircle(allocatedGenerator);
        double[] disk = BallSampler.UnitDisk(allocatedGenerator);
        double[] sphere = BallSampler.UnitSphere(allocatedGenerator);
        double[] circleDestination = new double[2];
        double[] diskDestination = new double[2];
        double[] sphereDestination = new double[3];
        BallSampler.UnitCircle(destinationGenerator, circleDestination);
        BallSampler.UnitDisk(destinationGenerator, diskDestination);
        BallSampler.UnitSphere(destinationGenerator, sphereDestination);

        Assert.Multiple(() =>
        {
            Assert.That(circleDestination, Is.EqualTo(circle));
            Assert.That(diskDestination, Is.EqualTo(disk));
            Assert.That(sphereDestination, Is.EqualTo(sphere));
            Assert.That(allocatedGenerator.ExportState().ToBytes(), Is.EqualTo(destinationGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void InvalidInputsFailBeforeStateOrDestinationMutation()
    {
        RandomGenerator generator = Create(1001UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = [91.0, 92.0, 93.0, 94.0];
        object[] cases =
        [
            new object[] { (TestDelegate)(() => BallSampler.Line(NullReference.Value<global::random.IRandomSource>(), [0.0], [1.0])), "generator" },
            new object[] { (TestDelegate)(() => BallSampler.Line(generator, NullReference.Value<double[]>(), [1.0])), "start" },
            new object[] { (TestDelegate)(() => BallSampler.Line(generator, [], [])), "start" },
            new object[] { (TestDelegate)(() => BallSampler.Line(generator, [0.0], [1.0, 2.0])), "end" },
            new object[] { (TestDelegate)(() => BallSampler.Line(generator, [double.NaN], [1.0])), "start" },
            new object[] { (TestDelegate)(() => BallSampler.Line(generator, [0.0], [double.PositiveInfinity])), "end" },
            new object[] { (TestDelegate)(() => BallSampler.Line(generator, [0.0], [1.0], destination)), "destination" },
            new object[] { (TestDelegate)(() => BallSampler.Box(generator, [0.0], [-1.0])), "maximum" },
            new object[] { (TestDelegate)(() => BallSampler.Box(generator, [0.0], [1.0], destination)), "destination" },
            new object[] { (TestDelegate)(() => BallSampler.UnitCircle(generator, destination)), "destination" },
            new object[] { (TestDelegate)(() => BallSampler.UnitDisk(generator, destination)), "destination" },
            new object[] { (TestDelegate)(() => BallSampler.UnitSphere(generator, destination)), "destination" },
            new object[] { (TestDelegate)(() => BallSampler.UnitBall(NullReference.Value<global::random.IRandomSource>(), 3)), "generator" },
            new object[] { (TestDelegate)(() => BallSampler.UnitBall(generator, 0)), "dimension" },
            new object[] { (TestDelegate)(() => BallSampler.UnitBall(generator, Span<double>.Empty)), "destination" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(destination, Is.EqualTo(new double[] { 91.0, 92.0, 93.0, 94.0 }));
        }
    }

    static double SquaredNorm(double[] values)
    {
        return values.Sum(value => value * value);
    }

    static double Sin(RandomMathMode mathMode, double value)
    {
        return mathMode == RandomMathMode.Portable ? PortableMath.Sin(value) : Math.Sin(value);
    }

    static double Cos(RandomMathMode mathMode, double value)
    {
        return mathMode == RandomMathMode.Portable ? PortableMath.Cos(value) : Math.Cos(value);
    }

    static double Sqrt(RandomMathMode mathMode, double value)
    {
        return mathMode == RandomMathMode.Portable ? PortableMath.Sqrt(value) : Math.Sqrt(value);
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mathMode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mathMode);
    }
}
