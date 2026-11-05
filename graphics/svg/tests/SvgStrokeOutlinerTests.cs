namespace svg.Tests;

public class SvgStrokeOutlinerTests
{
    [Test]
    public void DashNormalizationRepeatsOddPatternsAndCanonicalizesOffsets()
    {
        SvgStrokeDashPatternResult odd = new SvgStrokeDashes().Normalize(new List<double> { 2, 1, 3 }, -1);
        SvgStrokeDashPatternResult zero = new SvgStrokeDashes().Normalize(new List<double> { 0, 0 }, 50);
        SvgStrokeDashPatternResult invalid = new SvgStrokeDashes().Normalize(new List<double> { 2, -1 }, 0);

        Assert.Multiple(() =>
        {
            Assert.That(odd.Success, Is.True);
            Assert.That(odd.Pattern, Is.EqualTo(new double[] { 2, 1, 3, 2, 1, 3 }));
            Assert.That(odd.PatternLength, Is.EqualTo(12));
            Assert.That(odd.Offset, Is.EqualTo(11));
            Assert.That(odd.IsSolid, Is.False);
            Assert.That(zero.Success, Is.True);
            Assert.That(zero.IsSolid, Is.True);
            Assert.That(zero.Pattern, Is.Empty);
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Error, Is.EqualTo(SvgStrokeError.InvalidArgument));
            Assert.That(invalid.Pattern, Is.Null);
        });
    }

    [Test]
    public void ButtStrokeProducesAClosedRectangleAtTheRequestedWidth()
    {
        SvgStrokeStyle style = Style(2);

        SvgStrokeOutlineResult result = Parse("M0 0 L10 0").StrokeOutline(style);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.PathData.Commands, Has.Count.EqualTo(5));
            Assert.That(result.PathData.Commands[^1].Kind, Is.EqualTo(SvgPathCommandKind.ClosePath));
            AssertBounds(result.PathData, 0, -1, 10, 1);
            Assert.That(result.Dashes, Has.Count.EqualTo(1));
            AssertPlacement(result.Dashes[0], 0, 10, false);
        });
    }

    [Test]
    public void RoundAndSquareCapsExtendByHalfTheStrokeWidth()
    {
        SvgStrokeStyle square = Style(2);
        square.LineCap = SvgStrokeLineCap.Square;
        SvgStrokeStyle round = Style(2);
        round.LineCap = SvgStrokeLineCap.Round;
        round.Tolerance = 0.01;

        SvgStrokeOutlineResult squareResult = Parse("M0 0 L10 0").StrokeOutline(square);
        SvgStrokeOutlineResult roundResult = Parse("M0 0 L10 0").StrokeOutline(round);

        Assert.Multiple(() =>
        {
            AssertSuccess(squareResult);
            AssertBounds(squareResult.PathData, -1, -1, 11, 1);
            AssertSuccess(roundResult);
            AssertBounds(roundResult.PathData, -1, -1, 11, 1, 0.000001);
            Assert.That(roundResult.PathData.Commands.Count, Is.GreaterThan(squareResult.PathData.Commands.Count));
        });
    }

    [Test]
    public void ZeroLengthSubpathsUseCapSpecificGeometry()
    {
        SvgStrokeStyle butt = Style(4);
        SvgStrokeStyle round = Style(4);
        round.LineCap = SvgStrokeLineCap.Round;
        SvgStrokeStyle square = Style(4);
        square.LineCap = SvgStrokeLineCap.Square;

        SvgStrokeOutlineResult buttResult = Parse("M2 3 L2 3").StrokeOutline(butt);
        SvgStrokeOutlineResult roundResult = Parse("M2 3 L2 3").StrokeOutline(round);
        SvgStrokeOutlineResult squareResult = Parse("M2 3 L2 3").StrokeOutline(square);

        Assert.Multiple(() =>
        {
            AssertSuccess(buttResult);
            Assert.That(buttResult.PathData.Commands, Is.Empty);
            AssertSuccess(roundResult);
            AssertBounds(roundResult.PathData, 0, 1, 4, 5, 0.000001);
            AssertSuccess(squareResult);
            AssertBounds(squareResult.PathData, 0, 1, 4, 5);
            Assert.That(roundResult.Dashes, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void MiterLimitSelectsMiterOrBevelCornerCoverage()
    {
        SvgPathData corner = Parse("M0 0 L10 0 L10 10");
        SvgStrokeStyle miter = Style(2);
        SvgStrokeStyle bevel = Style(2);
        bevel.LineJoin = SvgStrokeLineJoin.Bevel;
        SvgStrokeStyle limited = Style(2);
        limited.MiterLimit = 1;

        SvgStrokeOutlineResult miterResult = corner.StrokeOutline(miter);
        SvgStrokeOutlineResult bevelResult = corner.StrokeOutline(bevel);
        SvgStrokeOutlineResult limitedResult = corner.StrokeOutline(limited);
        SvgPoint cornerPoint = new SvgPoint(10.9, -0.9);

        Assert.Multiple(() =>
        {
            AssertSuccess(miterResult);
            Assert.That(Contains(miterResult.PathData, cornerPoint), Is.True);
            AssertSuccess(bevelResult);
            Assert.That(Contains(bevelResult.PathData, cornerPoint), Is.False);
            AssertSuccess(limitedResult);
            Assert.That(Contains(limitedResult.PathData, cornerPoint), Is.False);
        });
    }

    [Test]
    public void EveryJoinKindConstructsBoundedClosedGeometry()
    {
        SvgPathData corner = Parse("M0 0 L10 0 L10 10");

        foreach (SvgStrokeLineJoin join in Enum.GetValues<SvgStrokeLineJoin>())
        {
            SvgStrokeStyle style = Style(2);
            style.LineJoin = join;
            style.MiterLimit = 1;

            SvgStrokeOutlineResult result = corner.StrokeOutline(style);

            AssertSuccess(result);
            Assert.That(result.PathData.Commands.Count, Is.GreaterThanOrEqualTo(14), join.ToString());
            Assert.That(result.PathData.Commands.Count(command => command.Kind == SvgPathCommandKind.ClosePath), Is.GreaterThanOrEqualTo(3), join.ToString());
        }
    }

    [Test]
    public void DashesRestartPerSubpathAndRespectPositiveOffsets()
    {
        SvgStrokeStyle style = Style(2);
        style.DashArray.Add(2);
        style.DashArray.Add(2);
        style.DashOffset = 1;

        SvgStrokeOutlineResult result = Parse("M0 0 L10 0 M0 10 L5 10").StrokeOutline(style);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.Dashes, Has.Count.EqualTo(5));
            AssertPlacement(result.Dashes[0], 0, 1, false);
            AssertPlacement(result.Dashes[1], 3, 5, false);
            AssertPlacement(result.Dashes[2], 7, 9, false);
            Assert.That(result.Dashes[3].SubpathIndex, Is.EqualTo(1));
            AssertPlacement(result.Dashes[3], 0, 1, false);
            AssertPlacement(result.Dashes[4], 3, 5, false);
        });
    }

    [Test]
    public void ClosedDashFragmentsMergeAcrossTheSubpathSeam()
    {
        SvgStrokeStyle style = Style(1);
        style.DashArray.Add(3);
        style.DashArray.Add(2);

        SvgStrokeOutlineResult result = Parse("M0 0 L3 0 L3 3 L0 3 Z").StrokeOutline(style);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.Dashes, Has.Count.EqualTo(2));
            Assert.That(result.Dashes.Any(dash => dash.StartDistance == 10 && dash.EndDistance == 15), Is.True);
            Assert.That(result.Dashes.Any(dash => dash.StartDistance == 5 && dash.EndDistance == 8), Is.True);
            Assert.That(result.Dashes.All(dash => !dash.Closed), Is.True);
        });
    }

    [Test]
    public void CurvesFlattenAndClosedSolidSubpathsHaveNoCaps()
    {
        SvgStrokeStyle style = Style(2);
        style.LineCap = SvgStrokeLineCap.Round;
        style.LineJoin = SvgStrokeLineJoin.Round;
        style.Tolerance = 0.05;

        SvgStrokeOutlineResult curve = Parse("M0 0 Q5 10 10 0").StrokeOutline(style);
        SvgStrokeOutlineResult closed = Parse("M0 0 L4 0 L4 4 Z").StrokeOutline(style);

        Assert.Multiple(() =>
        {
            AssertSuccess(curve);
            Assert.That(curve.PathData.Commands.Count, Is.GreaterThan(20));
            AssertSuccess(closed);
            Assert.That(closed.Dashes, Has.Count.EqualTo(1));
            Assert.That(closed.Dashes[0].Closed, Is.True);
        });
    }

    [Test]
    public void InvalidStylesMutationAndSecurityLimitsReturnStableFailures()
    {
        SvgPathData line = Parse("M0 0 L10 0");
        SvgStrokeStyle invalidWidth = Style(1);
        invalidWidth.Width = double.NaN;
        SvgStrokeStyle invalidDash = Style(1);
        invalidDash.DashArray.Add(-1);
        SvgStrokeStyle depth = Style(1);
        depth.Tolerance = 0.0001;
        depth.MaximumSubdivisionDepth = 0;
        SvgStrokeStyle dashLimit = Style(1);
        dashLimit.DashArray.Add(1);
        dashLimit.DashArray.Add(1);
        dashLimit.MaximumDashFragments = 2;
        SvgStrokeStyle outputLimit = Style(2);
        outputLimit.MaximumOutputCommands = 4;
        SvgPathData invalidPath = Parse("M0 0 L1 0");
        invalidPath.Commands[1].Values[0] = double.NaN;

        Assert.Multiple(() =>
        {
            AssertFailure(new SvgStrokeOutliner().Create(null, Style(1)), SvgStrokeError.InvalidArgument);
            AssertFailure(line.StrokeOutline(invalidWidth), SvgStrokeError.InvalidArgument);
            AssertFailure(line.StrokeOutline(invalidDash), SvgStrokeError.InvalidArgument);
            AssertFailure(Parse("M0 0 Q5 10 10 0").StrokeOutline(depth), SvgStrokeError.SubdivisionLimitExceeded);
            AssertFailure(line.StrokeOutline(dashLimit), SvgStrokeError.DashLimitExceeded);
            AssertFailure(line.StrokeOutline(outputLimit), SvgStrokeError.OutputLimitExceeded);
            AssertFailure(invalidPath.StrokeOutline(Style(1)), SvgStrokeError.InvalidPath);
        });
    }

    static SvgStrokeStyle Style(double width)
    {
        SvgStrokeStyle style = new SvgStrokeStyle();
        style.Width = width;
        style.Tolerance = 0.01;
        return style;
    }

    static SvgPathData Parse(string source)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(source, 128, 128);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static bool Contains(SvgPathData path, SvgPoint point)
    {
        SvgFillContainmentOptions options = new SvgFillContainmentOptions();
        options.Tolerance = 0.00001;
        return path.ClassifyFillWithOptions(point, SvgFillRule.Nonzero, options).Contains;
    }

    static void AssertSuccess(SvgStrokeOutlineResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgStrokeError.None));
        Assert.That(result.PathData, Is.Not.Null);
        Assert.That(result.DashPattern, Is.Not.Null);
        Assert.That(result.Dashes, Is.Not.Null);
    }

    static void AssertFailure(SvgStrokeOutlineResult result, SvgStrokeError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.PathData, Is.Null);
        Assert.That(result.DashPattern, Is.Null);
        Assert.That(result.Dashes, Is.Null);
    }

    static void AssertPlacement(SvgStrokeDashPlacement placement, double start, double end, bool closed)
    {
        Assert.That(placement.StartDistance, Is.EqualTo(start).Within(1e-10));
        Assert.That(placement.EndDistance, Is.EqualTo(end).Within(1e-10));
        Assert.That(placement.Closed, Is.EqualTo(closed));
    }

    static void AssertBounds(SvgPathData path, double minimumX, double minimumY, double maximumX, double maximumY, double tolerance = 1e-10)
    {
        Assert.That(path.Commands.Min(command => command.EndX), Is.EqualTo(minimumX).Within(tolerance));
        Assert.That(path.Commands.Min(command => command.EndY), Is.EqualTo(minimumY).Within(tolerance));
        Assert.That(path.Commands.Max(command => command.EndX), Is.EqualTo(maximumX).Within(tolerance));
        Assert.That(path.Commands.Max(command => command.EndY), Is.EqualTo(maximumY).Within(tolerance));
    }
}
