namespace svg.Tests;

public class SvgPathSimplifierTests
{
    [Test]
    public void CurvesAndArcsBecomeBoundedLineGeometryWithLossReport()
    {
        SvgPathData path = Parse("M0 0 Q5 10 10 0 C12 -3 18 -3 20 0 A5 5 0 0 1 25 5");
        SvgPathSimplifyOptions options = Options(0.05, 4);

        SvgPathSimplifyResult result = path.SimplifyWithOptions(options);

        Assert.Multiple(() =>
        {
            AssertSimplifySuccess(result);
            Assert.That(result.PathData.Commands.All(command => command.Kind == SvgPathCommandKind.MoveTo || command.Kind == SvgPathCommandKind.LineTo || command.Kind == SvgPathCommandKind.ClosePath), Is.True);
            Assert.That(result.Report.IsLossy, Is.True);
            Assert.That(result.Report.FlattenedCurveCount, Is.EqualTo(3));
            Assert.That(result.Report.MaximumErrorBound, Is.LessThanOrEqualTo(options.Tolerance));
            Assert.That(result.Report.CurveFlatteningErrorBound, Is.EqualTo(options.Tolerance / 4));
            Assert.That(result.Report.OutputPointCount, Is.LessThanOrEqualTo(result.Report.OriginalPointCount));
        });
    }

    [Test]
    public void LooseToleranceRemovesMorePolylinePointsThanTightTolerance()
    {
        SvgPathData path = Parse("M0 0 L1 0.02 L2 -0.02 L3 0.02 L4 0");

        SvgPathSimplifyResult tight = path.SimplifyWithOptions(Options(0.01, 6));
        SvgPathSimplifyResult loose = path.SimplifyWithOptions(Options(0.1, 6));

        Assert.Multiple(() =>
        {
            AssertSimplifySuccess(tight);
            AssertSimplifySuccess(loose);
            Assert.That(loose.Report.OutputPointCount, Is.LessThan(tight.Report.OutputPointCount));
            Assert.That(loose.Report.RemovedPointCount, Is.GreaterThan(tight.Report.RemovedPointCount));
            Assert.That(tight.Report.MaximumErrorBound, Is.LessThanOrEqualTo(0.01));
            Assert.That(loose.Report.MaximumErrorBound, Is.LessThanOrEqualTo(0.1));
        });
    }

    [Test]
    public void CoordinateRoundingIsMeasuredAndReported()
    {
        SvgPathData path = Parse("M0.12344 0.56744 L5.12344 5.56744");
        SvgPathSimplifyOptions options = Options(0.001, 3);

        SvgPathSimplifyResult result = path.SimplifyWithOptions(options);

        Assert.Multiple(() =>
        {
            AssertSimplifySuccess(result);
            Assert.That(result.PathData.Commands[0].EndX, Is.EqualTo(0.123));
            Assert.That(result.PathData.Commands[0].EndY, Is.EqualTo(0.567));
            Assert.That(result.Report.RoundedCoordinateCount, Is.EqualTo(4));
            Assert.That(result.Report.MeasuredPolylineError, Is.GreaterThan(0));
            Assert.That(result.Report.MaximumErrorBound, Is.LessThanOrEqualTo(options.Tolerance));
        });
    }

    [Test]
    public void CoarseRoundingFailsWithExplicitMeasuredLossReport()
    {
        SvgPathSimplifyOptions options = Options(0.1, 0);

        SvgPathSimplifyResult result = Parse("M0.49 0 L1.49 0").SimplifyWithOptions(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgPathSimplifyError.ErrorLimitExceeded));
            Assert.That(result.PathData, Is.Null);
            Assert.That(result.Report, Is.Not.Null);
            Assert.That(result.Report.RoundedCoordinateCount, Is.EqualTo(2));
            Assert.That(result.Report.MaximumErrorBound, Is.GreaterThan(options.Tolerance));
            Assert.That(result.Report.ErrorLimit, Is.EqualTo(options.Tolerance));
        });
    }

    [Test]
    public void ClosedContoursRemainClosedAndWithinErrorLimit()
    {
        SvgPathData circle = Parse("M10 0 A10 10 0 1 1 -10 0 A10 10 0 1 1 10 0 Z");
        SvgPathSimplifyOptions options = Options(0.05, 4);

        SvgPathSimplifyResult result = circle.SimplifyWithOptions(options);
        SvgFillContainmentResult center = result.PathData.ClassifyFill(new SvgPoint(0, 0), SvgFillRule.Nonzero);
        SvgFillContainmentResult outside = result.PathData.ClassifyFill(new SvgPoint(11, 0), SvgFillRule.Nonzero);

        Assert.Multiple(() =>
        {
            AssertSimplifySuccess(result);
            Assert.That(result.PathData.Commands[^1].Kind, Is.EqualTo(SvgPathCommandKind.ClosePath));
            Assert.That(result.PathData.GetSubpathCount(), Is.EqualTo(1));
            Assert.That(center.Contains, Is.True);
            Assert.That(outside.Contains, Is.False);
            Assert.That(result.Report.MaximumErrorBound, Is.LessThanOrEqualTo(options.Tolerance));
        });
    }

    [Test]
    public void EmptyAndMoveOnlyPathsRemainDeterministic()
    {
        SvgPathSimplifyResult empty = Parse("").SimplifyWithOptions(Options(0.01, 3));
        SvgPathSimplifyResult moves = Parse("M0.1234 0 M2.1234 2").SimplifyWithOptions(Options(0.001, 3));

        Assert.Multiple(() =>
        {
            AssertSimplifySuccess(empty);
            Assert.That(empty.PathData.Commands, Is.Empty);
            Assert.That(empty.Report.IsLossy, Is.False);
            AssertSimplifySuccess(moves);
            Assert.That(moves.PathData.Commands, Has.Count.EqualTo(2));
            Assert.That(moves.PathData.Commands[0].EndX, Is.EqualTo(0.123));
            Assert.That(moves.Report.RoundedCoordinateCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void InvalidMutationAndAdaptiveLimitsReturnStableFailures()
    {
        SvgPathData line = Parse("M0 0 L1 0 L2 0");
        SvgPathData curve = Parse("M0 0 Q5 10 10 0");
        SvgPathData mutated = Parse("M0 0 L1 1");
        mutated.Commands[1].Values[0] = double.NaN;
        SvgPathSimplifyOptions invalidPrecision = Options(1, 16);
        SvgPathSimplifyOptions invalidTolerance = Options(1, 3);
        invalidTolerance.Tolerance = double.NaN;
        SvgPathSimplifyOptions commandLimit = Options(1, 3);
        commandLimit.MaximumCommands = 2;
        SvgPathSimplifyOptions outputCommandLimit = Options(0.1, 3);
        outputCommandLimit.MaximumCommands = 2;
        SvgPathSimplifyOptions pointLimit = Options(1, 3);
        pointLimit.MaximumPoints = 2;
        SvgPathSimplifyOptions depthLimit = Options(0.00001, 6);
        depthLimit.MaximumSubdivisionDepth = 0;
        SvgPathSimplifyOptions operationLimit = Options(1, 3);
        operationLimit.MaximumOperations = 1;

        Assert.Multiple(() =>
        {
            AssertSimplifyFailure(new SvgPathSimplifier().SimplifyWithPath(null), SvgPathSimplifyError.InvalidArgument);
            AssertSimplifyFailure(line.SimplifyWithOptions(null), SvgPathSimplifyError.InvalidArgument);
            AssertSimplifyFailure(line.SimplifyWithOptions(invalidPrecision), SvgPathSimplifyError.InvalidArgument);
            AssertSimplifyFailure(line.SimplifyWithOptions(invalidTolerance), SvgPathSimplifyError.InvalidArgument);
            AssertSimplifyFailure(mutated.Simplify(), SvgPathSimplifyError.InvalidPath);
            AssertSimplifyFailure(line.SimplifyWithOptions(commandLimit), SvgPathSimplifyError.CommandLimitExceeded);
            AssertSimplifyFailure(curve.SimplifyWithOptions(outputCommandLimit), SvgPathSimplifyError.CommandLimitExceeded);
            AssertSimplifyFailure(Parse("M0 0 M1 1 M2 2").SimplifyWithOptions(pointLimit), SvgPathSimplifyError.PointLimitExceeded);
            AssertSimplifyFailure(curve.SimplifyWithOptions(depthLimit), SvgPathSimplifyError.SubdivisionLimitExceeded);
            AssertSimplifyFailure(line.SimplifyWithOptions(operationLimit), SvgPathSimplifyError.OperationLimitExceeded);
        });
    }

    static SvgPathData Parse(string value)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(value, 10000, 1000);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static SvgPathSimplifyOptions Options(double tolerance, int decimalPlaces)
    {
        SvgPathSimplifyOptions options = new SvgPathSimplifyOptions();
        options.Tolerance = tolerance;
        options.DecimalPlaces = decimalPlaces;
        return options;
    }

    static void AssertSimplifySuccess(SvgPathSimplifyResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgPathSimplifyError.None));
        Assert.That(result.PathData, Is.Not.Null);
        Assert.That(result.Report, Is.Not.Null);
    }

    static void AssertSimplifyFailure(SvgPathSimplifyResult result, SvgPathSimplifyError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.PathData, Is.Null);
    }
}
