namespace svg.Tests;

public class SvgPathCleanupTests
{
    [Test]
    public void EmptySubpathsAndZeroLengthSegmentsAreRemoved()
    {
        SvgPathCleanupResult result = Parse("M0 0 M1 1 M2 2 L2 2 M3 3 Z").Cleanup();

        Assert.Multiple(() =>
        {
            AssertCleanupSuccess(result);
            Assert.That(result.PathData.Commands, Is.Empty);
            Assert.That(result.OriginalCommandCount, Is.EqualTo(6));
            Assert.That(result.OutputCommandCount, Is.Zero);
            Assert.That(result.RemovedCommandCount, Is.EqualTo(6));
        });
    }

    [Test]
    public void ForwardCollinearLinesMergeButBacktrackingIsPreserved()
    {
        SvgPathCleanupResult forward = Parse("M0 0 L2 0 L5 0 L5 0 L10 0").Cleanup();
        SvgPathCleanupResult diagonal = Parse("M0 0 L2 2 L5 5").Cleanup();
        SvgPathCleanupResult backtracking = Parse("M0 0 L10 0 L5 0").Cleanup();

        Assert.Multiple(() =>
        {
            AssertCleanupSuccess(forward);
            Assert.That(forward.PathData.ToMinifiedText(), Is.EqualTo("M0 0 10 0"));
            Assert.That(forward.CompactedCommandCount, Is.EqualTo(2));
            Assert.That(forward.RemovedCommandCount, Is.EqualTo(3));
            AssertCleanupSuccess(diagonal);
            Assert.That(diagonal.PathData.ToMinifiedText(), Is.EqualTo("M0 0 5 5"));
            AssertCleanupSuccess(backtracking);
            Assert.That(backtracking.PathData.Commands, Has.Count.EqualTo(3));
            Assert.That(backtracking.PathData.Commands[2].EndX, Is.EqualTo(5));
        });
    }

    [Test]
    public void ExplicitAndRepeatedCloseGeometryCompactsToOneClosure()
    {
        SvgPathCleanupResult result = Parse("M0 0 L10 0 L10 10 L0 10 L0 0 L0 0 Z Z").Cleanup();
        SvgPathCleanupResult returnAlongLine = Parse("M0 0 L10 0 L5 0 Z").Cleanup();

        Assert.Multiple(() =>
        {
            AssertCleanupSuccess(result);
            Assert.That(result.PathData.Commands, Has.Count.EqualTo(5));
            Assert.That(result.PathData.Commands.Count(command => command.Kind == SvgPathCommandKind.ClosePath), Is.EqualTo(1));
            Assert.That(result.PathData.Commands[^1].Kind, Is.EqualTo(SvgPathCommandKind.ClosePath));
            AssertCleanupSuccess(returnAlongLine);
            Assert.That(returnAlongLine.PathData.ToMinifiedText(), Is.EqualTo("M0 0 10 0Z"));
        });
    }

    [Test]
    public void ExactlyLinearCurvesAndDegenerateArcsBecomeLines()
    {
        SvgPathCleanupResult quadratic = Parse("M0 0 Q5 0 10 0").Cleanup();
        SvgPathCleanupResult cubic = Parse("M0 0 C2 0 8 0 10 0").Cleanup();
        SvgPathCleanupResult lineArc = Parse("M0 0 A0 5 0 0 1 10 0").Cleanup();
        SvgPathCleanupResult pointArc = Parse("M2 3 A5 5 0 1 1 2 3").Cleanup();

        Assert.Multiple(() =>
        {
            AssertLineResult(quadratic, 10, 0);
            AssertLineResult(cubic, 10, 0);
            AssertLineResult(lineArc, 10, 0);
            AssertCleanupSuccess(pointArc);
            Assert.That(pointArc.PathData.Commands, Is.Empty);
        });
    }

    [Test]
    public void ReversingControlsAndGenuineCurvesRemainCurves()
    {
        SvgPathCleanupResult quadratic = Parse("M0 0 Q15 0 10 0").Cleanup();
        SvgPathCleanupResult cubic = Parse("M0 0 C8 0 2 0 10 0").Cleanup();
        SvgPathCleanupResult curved = Parse("M0 0 C2 5 8 5 10 0 A4 3 0 0 1 14 3").Cleanup();

        Assert.Multiple(() =>
        {
            AssertCleanupSuccess(quadratic);
            Assert.That(quadratic.PathData.Commands[1].Kind, Is.EqualTo(SvgPathCommandKind.QuadraticCurveTo));
            AssertCleanupSuccess(cubic);
            Assert.That(cubic.PathData.Commands[1].Kind, Is.EqualTo(SvgPathCommandKind.CubicCurveTo));
            AssertCleanupSuccess(curved);
            Assert.That(curved.PathData.Commands[1].Kind, Is.EqualTo(SvgPathCommandKind.CubicCurveTo));
            Assert.That(curved.PathData.Commands[2].Kind, Is.EqualTo(SvgPathCommandKind.ArcTo));
        });
    }

    [Test]
    public void CleanupNormalizesEquivalentRelativeAndShorthandCommands()
    {
        SvgPathCleanupResult result = Parse("m1 1 h4 v4 l-4 0 z").Cleanup();

        Assert.Multiple(() =>
        {
            AssertCleanupSuccess(result);
            Assert.That(result.PathData.Commands.All(command => command.Relative == false), Is.True);
            Assert.That(result.PathData.Commands.Any(command => command.Kind == SvgPathCommandKind.HorizontalLineTo || command.Kind == SvgPathCommandKind.VerticalLineTo), Is.False);
            Assert.That(result.PathData.ToMinifiedText(), Is.EqualTo("M1 1 5 1 5 5 1 5Z"));
        });
    }

    [Test]
    public void CleanupPreservesFillGeometryForRedundantClosedPaths()
    {
        SvgPathData source = Parse("M0 0 L5 0 L10 0 L10 5 L10 10 L0 10 L0 0 Z");
        SvgPathCleanupResult result = source.Cleanup();

        SvgFillContainmentResult sourceInside = source.ClassifyFill(new SvgPoint(5, 5), SvgFillRule.Nonzero);
        SvgFillContainmentResult cleanedInside = result.PathData.ClassifyFill(new SvgPoint(5, 5), SvgFillRule.Nonzero);
        SvgFillContainmentResult sourceOutside = source.ClassifyFill(new SvgPoint(15, 5), SvgFillRule.Nonzero);
        SvgFillContainmentResult cleanedOutside = result.PathData.ClassifyFill(new SvgPoint(15, 5), SvgFillRule.Nonzero);

        Assert.Multiple(() =>
        {
            AssertCleanupSuccess(result);
            Assert.That(result.PathData.Commands, Has.Count.EqualTo(5));
            Assert.That(cleanedInside.Contains, Is.EqualTo(sourceInside.Contains));
            Assert.That(cleanedOutside.Contains, Is.EqualTo(sourceOutside.Contains));
        });
    }

    [Test]
    public void InvalidMutationAndCommandLimitsReturnStableFailures()
    {
        SvgPathData path = Parse("M0 0 L1 1 L2 2");
        SvgPathData mutated = Parse("M0 0 L1 1");
        mutated.Commands[1].Values[0] = double.NaN;
        SvgPathCleanupOptions invalid = new SvgPathCleanupOptions();
        invalid.MaximumCommands = 0;
        SvgPathCleanupOptions limited = new SvgPathCleanupOptions();
        limited.MaximumCommands = 2;

        Assert.Multiple(() =>
        {
            AssertCleanupFailure(new SvgPathCleanup().CleanWithPath(null), SvgPathCleanupError.InvalidArgument);
            AssertCleanupFailure(path.CleanupWithOptions(null), SvgPathCleanupError.InvalidArgument);
            AssertCleanupFailure(path.CleanupWithOptions(invalid), SvgPathCleanupError.InvalidArgument);
            AssertCleanupFailure(path.CleanupWithOptions(limited), SvgPathCleanupError.CommandLimitExceeded);
            AssertCleanupFailure(mutated.Cleanup(), SvgPathCleanupError.InvalidPath);
        });
    }

    static SvgPathData Parse(string value)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(value, 1000, 1000);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static void AssertCleanupSuccess(SvgPathCleanupResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgPathCleanupError.None));
        Assert.That(result.PathData, Is.Not.Null);
        Assert.That(result.OutputCommandCount, Is.EqualTo(result.PathData.Commands.Count));
    }

    static void AssertCleanupFailure(SvgPathCleanupResult result, SvgPathCleanupError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.PathData, Is.Null);
    }

    static void AssertLineResult(SvgPathCleanupResult result, double x, double y)
    {
        AssertCleanupSuccess(result);
        Assert.That(result.PathData.Commands, Has.Count.EqualTo(2));
        Assert.That(result.PathData.Commands[1].Kind, Is.EqualTo(SvgPathCommandKind.LineTo));
        Assert.That(result.PathData.Commands[1].EndX, Is.EqualTo(x));
        Assert.That(result.PathData.Commands[1].EndY, Is.EqualTo(y));
    }
}
