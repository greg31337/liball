namespace svg.Tests;

public class SvgPathMetricsTests
{
    [Test]
    public void MeasureIncludesCloseEdgesAndReportsOrderedSubpathLengths()
    {
        SvgPathMetricResult result = Parse("M0 0 L3 0 L3 4 Z M10 0 L10 2").Measure();

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.ActualLength, Is.EqualTo(14).Within(1e-12));
            Assert.That(result.CalibratedLength, Is.EqualTo(14).Within(1e-12));
            Assert.That(result.Scale, Is.EqualTo(1).Within(1e-12));
            Assert.That(result.SubpathLengths, Is.EqualTo(new double[] { 12, 2 }));
        });
    }

    [Test]
    public void MeasureHandlesLinesBeziersAndEllipticalArcs()
    {
        SvgPathMeasureOptions options = Options(0.00001);
        SvgPathMetricResult straightCurves = Parse("M0 0 Q5 0 10 0 C15 0 15 0 20 0").MeasureWithOptions(options);
        SvgPathMetricResult quarterCircle = Parse("M1 0 A1 1 0 0 1 0 1").MeasureWithOptions(options);

        Assert.Multiple(() =>
        {
            AssertSuccess(straightCurves);
            Assert.That(straightCurves.ActualLength, Is.EqualTo(20).Within(1e-12));
            AssertSuccess(quarterCircle);
            Assert.That(quarterCircle.ActualLength, Is.EqualTo(System.Math.PI / 2).Within(0.0001));
        });
    }

    [Test]
    public void LocationUsesForwardSegmentAtBoundariesAndReturnsAUnitFrame()
    {
        SvgPathData path = Parse("M0 0 L3 0 L3 4");

        SvgPathLocationResult interior = path.GetLocation(2);
        SvgPathLocationResult boundary = new SvgPathMetrics().GetLocationWithPathAndDistance(path, 3);
        SvgPathLocationResult clamped = path.GetLocation(100);

        Assert.Multiple(() =>
        {
            AssertLocation(interior, 2, 0, 1, 0, 0, 1);
            Assert.That(interior.SegmentIndex, Is.Zero);
            AssertLocation(boundary, 3, 0, 0, 1, -1, 0);
            Assert.That(boundary.SegmentIndex, Is.EqualTo(1));
            AssertLocation(clamped, 3, 4, 0, 1, -1, 0);
            Assert.That(clamped.ActualDistance, Is.EqualTo(7));
        });
    }

    [Test]
    public void PathLengthCalibrationScalesQueriesAndExtractedIntervals()
    {
        SvgPathData path = Parse("M0 0 L10 0");
        SvgPathMeasureOptions options = Options(0.01);
        options.PathLength = new SvgPathLength(100);

        SvgPathMetricResult metric = path.MeasureWithOptions(options);
        SvgPathLocationResult location = path.GetLocationWithOptions(50, options);
        SvgPathExtractResult extracted = path.ExtractWithOptions(25, 75, options);

        Assert.Multiple(() =>
        {
            AssertSuccess(metric);
            Assert.That(metric.ActualLength, Is.EqualTo(10));
            Assert.That(metric.CalibratedLength, Is.EqualTo(100));
            Assert.That(metric.Scale, Is.EqualTo(0.1));
            AssertLocation(location, 5, 0, 1, 0, 0, 1);
            Assert.That(location.ActualDistance, Is.EqualTo(5));
            AssertSuccess(extracted);
            Assert.That(extracted.PathData.Commands, Has.Count.EqualTo(2));
            AssertCommand(extracted.PathData.Commands[0], SvgPathCommandKind.MoveTo, 2.5, 0);
            AssertCommand(extracted.PathData.Commands[1], SvgPathCommandKind.LineTo, 7.5, 0);
        });
    }

    [Test]
    public void ExtractionPreservesCurveKindsAndSeparatesSubpaths()
    {
        SvgPathData curves = Parse("M0 0 Q5 10 10 0 C15 0 15 10 20 10 A5 5 0 0 1 25 15");
        SvgPathExtractResult allCurves = curves.Extract(0, double.MaxValue);
        SvgPathExtractResult acrossSubpaths = Parse("M0 0 L2 0 M10 0 L12 0").Extract(1, 3);

        Assert.Multiple(() =>
        {
            AssertSuccess(allCurves);
            Assert.That(allCurves.PathData.Commands.Select(command => command.Kind), Is.EqualTo(new SvgPathCommandKind[]
            {
                SvgPathCommandKind.MoveTo,
                SvgPathCommandKind.QuadraticCurveTo,
                SvgPathCommandKind.CubicCurveTo,
                SvgPathCommandKind.ArcTo
            }));
            AssertSuccess(acrossSubpaths);
            Assert.That(acrossSubpaths.PathData.Commands, Has.Count.EqualTo(4));
            AssertCommand(acrossSubpaths.PathData.Commands[0], SvgPathCommandKind.MoveTo, 1, 0);
            AssertCommand(acrossSubpaths.PathData.Commands[1], SvgPathCommandKind.LineTo, 2, 0);
            AssertCommand(acrossSubpaths.PathData.Commands[2], SvgPathCommandKind.MoveTo, 10, 0);
            AssertCommand(acrossSubpaths.PathData.Commands[3], SvgPathCommandKind.LineTo, 11, 0);
        });
    }

    [Test]
    public void PartialQuadraticExtractionSplitsAtTheMeasuredParameter()
    {
        SvgPathData curve = Parse("M0 0 Q5 10 10 0");
        SvgPathMeasureOptions options = Options(0.00001);
        double halfLength = curve.MeasureWithOptions(options).ActualLength / 2;

        SvgPathExtractResult firstHalf = curve.ExtractWithOptions(0, halfLength, options);

        Assert.Multiple(() =>
        {
            AssertSuccess(firstHalf);
            Assert.That(firstHalf.PathData.Commands, Has.Count.EqualTo(2));
            AssertCommand(firstHalf.PathData.Commands[0], SvgPathCommandKind.MoveTo, 0, 0);
            AssertCommand(firstHalf.PathData.Commands[1], SvgPathCommandKind.QuadraticCurveTo, 5, 5);
            Assert.That(firstHalf.PathData.Commands[1].Values[0], Is.EqualTo(2.5).Within(0.00001));
            Assert.That(firstHalf.PathData.Commands[1].Values[1], Is.EqualTo(5).Within(0.00001));
        });
    }

    [Test]
    public void ZeroLengthSegmentsDoNotDisturbLookupTrimOrPointExtraction()
    {
        SvgPathData path = Parse("M0 0 L0 0 L5 0 L5 0");

        SvgPathMetricResult metric = path.Measure();
        SvgPathLocationResult start = path.GetLocation(0);
        SvgPathLocationResult end = path.GetLocation(5);
        SvgPathExtractResult trim = path.Trim(0, 5);
        SvgPathExtractResult point = path.Extract(2, 2);
        SvgPathExtractResult allZero = Parse("M2 3 L2 3").Extract(0, 0);

        Assert.Multiple(() =>
        {
            AssertSuccess(metric);
            Assert.That(metric.ActualLength, Is.EqualTo(5));
            AssertLocation(start, 0, 0, 1, 0, 0, 1);
            AssertLocation(end, 5, 0, 1, 0, 0, 1);
            Assert.That(start.SegmentIndex, Is.EqualTo(1));
            Assert.That(end.SegmentIndex, Is.EqualTo(1));
            AssertSuccess(trim);
            Assert.That(trim.PathData.Commands, Has.Count.EqualTo(2));
            AssertCommand(trim.PathData.Commands[1], SvgPathCommandKind.LineTo, 5, 0);
            AssertSuccess(point);
            Assert.That(point.PathData.Commands, Has.Count.EqualTo(1));
            AssertCommand(point.PathData.Commands[0], SvgPathCommandKind.MoveTo, 2, 0);
            AssertSuccess(allZero);
            Assert.That(allZero.PathData.Commands, Has.Count.EqualTo(1));
            AssertCommand(allZero.PathData.Commands[0], SvgPathCommandKind.MoveTo, 2, 3);
        });
    }

    [Test]
    public void InvalidArgumentsCalibrationAndAdaptiveLimitsReturnStableFailures()
    {
        SvgPathData line = Parse("M0 0 L1 0 L2 0");
        SvgPathMeasureOptions invalidTolerance = Options(1);
        invalidTolerance.Tolerance = double.NaN;
        SvgPathMeasureOptions zeroCalibration = Options(1);
        zeroCalibration.PathLength = new SvgPathLength(0);
        SvgPathMeasureOptions segmentLimit = Options(1);
        segmentLimit.MaximumFlattenedSegments = 1;
        SvgPathMeasureOptions depthLimit = Options(0.001);
        depthLimit.MaximumSubdivisionDepth = 0;

        Assert.Multiple(() =>
        {
            AssertFailure(line.MeasureWithOptions(invalidTolerance), SvgPathMetricError.InvalidArgument);
            AssertFailure(line.MeasureWithOptions(zeroCalibration), SvgPathMetricError.InvalidArgument);
            AssertLocationFailure(line.GetLocationWithOptions(1, zeroCalibration), SvgPathMetricError.InvalidArgument);
            AssertExtractFailure(line.Extract(-1, 1), SvgPathMetricError.InvalidArgument);
            AssertExtractFailure(line.Extract(2, 1), SvgPathMetricError.InvalidArgument);
            AssertFailure(line.MeasureWithOptions(segmentLimit), SvgPathMetricError.SegmentLimitExceeded);
            AssertFailure(Parse("M0 0 Q5 10 10 0").MeasureWithOptions(depthLimit), SvgPathMetricError.SubdivisionLimitExceeded);
            AssertFailure(new SvgPathMetrics().MeasureWithPath(null), SvgPathMetricError.InvalidArgument);
        });
    }

    [Test]
    public void InvalidMutablePathsAndCoordinateOverflowFailWithoutPartialResults()
    {
        SvgPathData invalid = Parse("M0 0 L1 0");
        invalid.Commands[1].Values[0] = double.NaN;
        SvgPathData overflow = Parse("M0 0 L1 0");
        overflow.Commands[0].Values[0] = -double.MaxValue;
        overflow.Commands[1].Values[0] = double.MaxValue;

        SvgPathMetricResult invalidResult = invalid.Measure();
        SvgPathMetricResult overflowResult = overflow.Measure();

        Assert.Multiple(() =>
        {
            AssertFailure(invalidResult, SvgPathMetricError.InvalidPath);
            AssertFailure(overflowResult, SvgPathMetricError.NonFiniteResult);
            Assert.That(invalidResult.SubpathLengths, Is.Null);
            Assert.That(overflowResult.SubpathLengths, Is.Null);
        });
    }

    static SvgPathData Parse(string source)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(source, 128, 128);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static SvgPathMeasureOptions Options(double tolerance)
    {
        SvgPathMeasureOptions options = new SvgPathMeasureOptions();
        options.Tolerance = tolerance;
        return options;
    }

    static void AssertSuccess(SvgPathMetricResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgPathMetricError.None));
        Assert.That(result.SubpathLengths, Is.Not.Null);
    }

    static void AssertSuccess(SvgPathExtractResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgPathMetricError.None));
        Assert.That(result.PathData, Is.Not.Null);
    }

    static void AssertFailure(SvgPathMetricResult result, SvgPathMetricError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
    }

    static void AssertLocationFailure(SvgPathLocationResult result, SvgPathMetricError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Point, Is.Null);
    }

    static void AssertExtractFailure(SvgPathExtractResult result, SvgPathMetricError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.PathData, Is.Null);
    }

    static void AssertLocation(SvgPathLocationResult result, double x, double y, double tangentX, double tangentY, double normalX, double normalY)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgPathMetricError.None));
        Assert.That(result.Point.X, Is.EqualTo(x).Within(1e-10));
        Assert.That(result.Point.Y, Is.EqualTo(y).Within(1e-10));
        Assert.That(result.Tangent.X, Is.EqualTo(tangentX).Within(1e-10));
        Assert.That(result.Tangent.Y, Is.EqualTo(tangentY).Within(1e-10));
        Assert.That(result.Normal.X, Is.EqualTo(normalX).Within(1e-10));
        Assert.That(result.Normal.Y, Is.EqualTo(normalY).Within(1e-10));
    }

    static void AssertCommand(SvgPathCommand command, SvgPathCommandKind kind, double x, double y)
    {
        Assert.That(command.Kind, Is.EqualTo(kind));
        Assert.That(command.EndX, Is.EqualTo(x).Within(1e-10));
        Assert.That(command.EndY, Is.EqualTo(y).Within(1e-10));
    }
}
