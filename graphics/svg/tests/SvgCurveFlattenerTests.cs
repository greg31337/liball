namespace svg.Tests;

public class SvgCurveFlattenerTests
{
    [Test]
    public void LinesEmitExactOrderedEndpoints()
    {
        SvgLineSegment line = new SvgLineSegment(Point(2, 3), Point(8, 9));

        SvgFlattenResult flattened = line.Flatten(Options(0.1));

        Assert.Multiple(() =>
        {
            AssertSuccess(flattened);
            Assert.That(flattened.SegmentCount, Is.EqualTo(1));
            Assert.That(flattened.Points, Has.Count.EqualTo(2));
            AssertFlattenedPoint(flattened.Points[0], 2, 3, 0);
            AssertFlattenedPoint(flattened.Points[1], 8, 9, 1);
        });
    }

    [Test]
    public void QuadraticFlatteningAdaptsToToleranceAndMeetsChordDeviation()
    {
        SvgQuadraticBezier quadratic = new SvgQuadraticBezier(Point(0, 0), Point(10, 20), Point(20, 0));

        SvgFlattenResult loose = quadratic.Flatten(Options(25));
        SvgFlattenResult tight = new SvgCurveFlattener().FlattenWithQuadraticAndOptions(quadratic, Options(0.25));

        Assert.Multiple(() =>
        {
            AssertSuccess(loose);
            Assert.That(loose.SegmentCount, Is.EqualTo(1));
            AssertSuccess(tight);
            Assert.That(tight.SegmentCount, Is.GreaterThan(1));
            AssertMonotonic(tight);
            AssertDeviation(quadratic, tight, 0.25);
        });
    }

    [Test]
    public void CubicFlatteningHandlesOpposingControlsAndPreservesEndpoints()
    {
        SvgCubicBezier cubic = new SvgCubicBezier(Point(0, 0), Point(0, 12), Point(12, -12), Point(12, 0));

        SvgFlattenResult flattened = cubic.Flatten(Options(0.2));

        Assert.Multiple(() =>
        {
            AssertSuccess(flattened);
            Assert.That(flattened.SegmentCount, Is.GreaterThan(2));
            AssertFlattenedPoint(flattened.Points[0], 0, 0, 0);
            AssertFlattenedPoint(flattened.Points[^1], 12, 0, 1);
            AssertMonotonic(flattened);
            AssertDeviation(cubic, flattened, 0.2);
        });
    }

    [Test]
    public void ArcFlatteningSplitsLargeSweepsAndAdaptsFurtherForTolerance()
    {
        SvgArc arc = Arc(Point(1, 0), Point(0, 1), true, true);

        SvgFlattenResult angular = arc.Flatten(Options(100));
        SvgFlattenResult tight = arc.Flatten(Options(0.01));

        Assert.Multiple(() =>
        {
            AssertSuccess(angular);
            Assert.That(angular.SegmentCount, Is.EqualTo(4));
            AssertSuccess(tight);
            Assert.That(tight.SegmentCount, Is.GreaterThan(angular.SegmentCount));
            AssertMonotonic(tight);
            AssertFlattenedPoint(tight.Points[0], 1, 0, 0);
            AssertFlattenedPoint(tight.Points[^1], 0, 1, 1);
            AssertDeviation(arc, tight, 0.01);
        });
    }

    [Test]
    public void DeviceTransformControlsToleranceWithoutChangingOutputCoordinates()
    {
        SvgQuadraticBezier quadratic = new SvgQuadraticBezier(Point(0, 0), Point(5, 1), Point(10, 0));
        SvgFlattenOptions absoluteOptions = Options(2);
        SvgFlattenOptions deviceOptions = Options(2);
        deviceOptions.DeviceTransform = new SvgMatrix(1, 0, 0, 100, 0, 0);

        SvgFlattenResult absolute = quadratic.Flatten(absoluteOptions);
        SvgFlattenResult device = quadratic.Flatten(deviceOptions);

        Assert.Multiple(() =>
        {
            AssertSuccess(absolute);
            Assert.That(absolute.SegmentCount, Is.EqualTo(1));
            AssertSuccess(device);
            Assert.That(device.SegmentCount, Is.GreaterThan(absolute.SegmentCount));
            Assert.That(device.Points.Max(point => point.Point.Y), Is.LessThanOrEqualTo(0.5));
            AssertFlattenedPoint(device.Points[0], 0, 0, 0);
            AssertFlattenedPoint(device.Points[^1], 10, 0, 1);
        });
    }

    [Test]
    public void SegmentAndSubdivisionLimitsFailBeforeReturningPartialOutput()
    {
        SvgQuadraticBezier quadratic = new SvgQuadraticBezier(Point(0, 0), Point(10, 20), Point(20, 0));
        SvgFlattenOptions segmentOptions = Options(0.01);
        segmentOptions.MaximumSegments = 2;
        SvgFlattenOptions depthOptions = Options(0.01);
        depthOptions.MaximumSubdivisionDepth = 0;

        SvgFlattenResult segmentLimit = quadratic.Flatten(segmentOptions);
        SvgFlattenResult depthLimit = quadratic.Flatten(depthOptions);

        Assert.Multiple(() =>
        {
            AssertFailure(segmentLimit, SvgFlattenError.SegmentLimitExceeded);
            AssertFailure(depthLimit, SvgFlattenError.SubdivisionLimitExceeded);
        });
    }

    [Test]
    public void DegenerateArcsProducePointOrLineOutputDeterministically()
    {
        SvgArc point = new SvgArcGeometry().FromEndpoint(Point(2, 3), Point(2, 3), 4, 5, 0, false, true).Arc;
        SvgArc line = new SvgArcGeometry().FromEndpoint(Point(0, 0), Point(6, 3), 0, 5, 0, false, true).Arc;

        SvgFlattenResult pointResult = point.Flatten(Options(0.1));
        SvgFlattenResult lineResult = line.Flatten(Options(0.1));

        Assert.Multiple(() =>
        {
            AssertSuccess(pointResult);
            Assert.That(pointResult.SegmentCount, Is.Zero);
            Assert.That(pointResult.Points, Has.Count.EqualTo(1));
            AssertSuccess(lineResult);
            Assert.That(lineResult.SegmentCount, Is.EqualTo(1));
            AssertFlattenedPoint(lineResult.Points[1], 6, 3, 1);
        });
    }

    [Test]
    public void FlatteningRejectsInvalidMutationOptionsAndDeviceOverflow()
    {
        SvgQuadraticBezier quadratic = new SvgQuadraticBezier(Point(0, 0), Point(1, 2), Point(2, 0));
        SvgFlattenOptions invalidTolerance = Options(1);
        invalidTolerance.Tolerance = double.NaN;
        SvgFlattenOptions invalidDepth = Options(1);
        invalidDepth.MaximumSubdivisionDepth = -1;
        SvgFlattenOptions invalidMatrix = Options(1);
        invalidMatrix.DeviceTransform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        invalidMatrix.DeviceTransform.A = double.PositiveInfinity;
        SvgFlattenOptions overflowOptions = Options(1);
        overflowOptions.DeviceTransform = new SvgMatrix(2, 0, 0, 1, 0, 0);
        SvgLineSegment huge = new SvgLineSegment(Point(double.MaxValue, 0), Point(double.MaxValue, 1));
        quadratic.Control.X = double.NaN;

        Assert.Multiple(() =>
        {
            AssertFailure(new SvgCurveFlattener().FlattenWithQuadraticAndOptions(quadratic, Options(1)), SvgFlattenError.InvalidArgument);
            AssertFailure(new SvgLineSegment(Point(0, 0), Point(1, 1)).Flatten(invalidTolerance), SvgFlattenError.InvalidArgument);
            AssertFailure(new SvgLineSegment(Point(0, 0), Point(1, 1)).Flatten(invalidDepth), SvgFlattenError.InvalidArgument);
            AssertFailure(new SvgLineSegment(Point(0, 0), Point(1, 1)).Flatten(invalidMatrix), SvgFlattenError.InvalidArgument);
            AssertFailure(huge.Flatten(overflowOptions), SvgFlattenError.NonFiniteResult);
            AssertFailure(new SvgCurveFlattener().FlattenWithLineAndOptions((SvgLineSegment)null, Options(1)), SvgFlattenError.InvalidArgument);
        });
    }

    static SvgFlattenOptions Options(double tolerance)
    {
        SvgFlattenOptions options = new SvgFlattenOptions();
        options.Tolerance = tolerance;
        return options;
    }

    static SvgPoint Point(double x, double y)
    {
        return new SvgPoint(x, y);
    }

    static SvgArc Arc(SvgPoint start, SvgPoint end, bool largeArc, bool sweep)
    {
        SvgArcOperationResult result = new SvgArcGeometry().FromEndpoint(start, end, 1, 1, 0, largeArc, sweep);
        Assert.That(result.Success, Is.True);
        return result.Arc;
    }

    static void AssertSuccess(SvgFlattenResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgFlattenError.None));
        Assert.That(result.Points, Is.Not.Null);
        Assert.That(result.Points.Count, Is.EqualTo(result.SegmentCount + 1));
    }

    static void AssertFailure(SvgFlattenResult result, SvgFlattenError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Points, Is.Null);
        Assert.That(result.SegmentCount, Is.Zero);
    }

    static void AssertMonotonic(SvgFlattenResult result)
    {
        int index = 1;
        while (index < result.Points.Count)
        {
            Assert.That(result.Points[index].Amount, Is.GreaterThan(result.Points[index - 1].Amount));
            index++;
        }
    }

    static void AssertFlattenedPoint(SvgFlattenedPoint point, double x, double y, double amount)
    {
        Assert.That(point.Point.X, Is.EqualTo(x).Within(1e-12));
        Assert.That(point.Point.Y, Is.EqualTo(y).Within(1e-12));
        Assert.That(point.Amount, Is.EqualTo(amount).Within(1e-12));
    }

    static void AssertDeviation(SvgQuadraticBezier curve, SvgFlattenResult result, double tolerance)
    {
        int index = 1;
        while (index < result.Points.Count)
        {
            SvgFlattenedPoint first = result.Points[index - 1];
            SvgFlattenedPoint second = result.Points[index];
            SvgPoint midpoint = curve.Evaluate((first.Amount + second.Amount) / 2).Point;
            Assert.That(Distance(midpoint, first.Point, second.Point), Is.LessThanOrEqualTo(tolerance + 1e-12));
            index++;
        }
    }

    static void AssertDeviation(SvgCubicBezier curve, SvgFlattenResult result, double tolerance)
    {
        int index = 1;
        while (index < result.Points.Count)
        {
            SvgFlattenedPoint first = result.Points[index - 1];
            SvgFlattenedPoint second = result.Points[index];
            SvgPoint midpoint = curve.Evaluate((first.Amount + second.Amount) / 2).Point;
            Assert.That(Distance(midpoint, first.Point, second.Point), Is.LessThanOrEqualTo(tolerance + 1e-12));
            index++;
        }
    }

    static void AssertDeviation(SvgArc curve, SvgFlattenResult result, double tolerance)
    {
        int index = 1;
        while (index < result.Points.Count)
        {
            SvgFlattenedPoint first = result.Points[index - 1];
            SvgFlattenedPoint second = result.Points[index];
            SvgPoint midpoint = curve.Evaluate((first.Amount + second.Amount) / 2).Point;
            Assert.That(Distance(midpoint, first.Point, second.Point), Is.LessThanOrEqualTo(tolerance + 1e-12));
            index++;
        }
    }

    static double Distance(SvgPoint point, SvgPoint start, SvgPoint end)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double length = System.Math.Sqrt(dx * dx + dy * dy);
        if (length == 0) return System.Math.Sqrt((point.X - start.X) * (point.X - start.X) + (point.Y - start.Y) * (point.Y - start.Y));
        return System.Math.Abs(dx * (point.Y - start.Y) - dy * (point.X - start.X)) / length;
    }
}
