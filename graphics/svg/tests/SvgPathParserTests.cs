namespace svg.Tests;

public class SvgPathParserTests
{
    [Test]
    public void ParsesEveryAbsoluteAndRelativePathCommand()
    {
        string input = "M1 2 L3 4 H5 V6 C7 8 9 10 11 12 S13 14 15 16 Q17 18 19 20 T21 22 A2 3 45 0 1 23 24 Z m1 1 l2 2 h3 v4 c1 2 3 4 5 6 s1 2 3 4 q1 2 3 4 t5 6 a7 8 9 1 0 10 11 z";
        SvgPathParseResult result = new SvgPathParser().Parse(input, 32, 32);

        SvgPathCommandKind[] expectedKinds =
        {
            SvgPathCommandKind.MoveTo,
            SvgPathCommandKind.LineTo,
            SvgPathCommandKind.HorizontalLineTo,
            SvgPathCommandKind.VerticalLineTo,
            SvgPathCommandKind.CubicCurveTo,
            SvgPathCommandKind.SmoothCubicCurveTo,
            SvgPathCommandKind.QuadraticCurveTo,
            SvgPathCommandKind.SmoothQuadraticCurveTo,
            SvgPathCommandKind.ArcTo,
            SvgPathCommandKind.ClosePath,
            SvgPathCommandKind.MoveTo,
            SvgPathCommandKind.LineTo,
            SvgPathCommandKind.HorizontalLineTo,
            SvgPathCommandKind.VerticalLineTo,
            SvgPathCommandKind.CubicCurveTo,
            SvgPathCommandKind.SmoothCubicCurveTo,
            SvgPathCommandKind.QuadraticCurveTo,
            SvgPathCommandKind.SmoothQuadraticCurveTo,
            SvgPathCommandKind.ArcTo,
            SvgPathCommandKind.ClosePath
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.PathData.Commands.Select(command => command.Kind), Is.EqualTo(expectedKinds));
            Assert.That(result.PathData.Commands.Take(10).All(command => !command.Relative), Is.True);
            Assert.That(result.PathData.Commands.Skip(10).All(command => command.Relative), Is.True);
            Assert.That(result.PathData.Commands[8].Values, Is.EqualTo(new[] { 2.0, 3.0, 45.0, 0.0, 1.0, 23.0, 24.0 }));
            Assert.That(result.PathData.Commands[18].EndX, Is.EqualTo(33));
            Assert.That(result.PathData.Commands[18].EndY, Is.EqualTo(40));
            Assert.That(result.PathData.Commands[19].EndX, Is.EqualTo(2));
            Assert.That(result.PathData.Commands[19].EndY, Is.EqualTo(3));
            Assert.That(result.PathData.GetSubpathCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void RepeatedParameterGroupsCreateImplicitCommands()
    {
        SvgPathParseResult result = new SvgPathParser().Parse("M0 0 10 10 20 20 L30 30,40 40 H50 60 V70 80", 16, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.PathData.Commands, Has.Count.EqualTo(9));
            Assert.That(result.PathData.Commands.Select(command => command.Kind), Is.EqualTo(new[]
            {
                SvgPathCommandKind.MoveTo,
                SvgPathCommandKind.LineTo,
                SvgPathCommandKind.LineTo,
                SvgPathCommandKind.LineTo,
                SvgPathCommandKind.LineTo,
                SvgPathCommandKind.HorizontalLineTo,
                SvgPathCommandKind.HorizontalLineTo,
                SvgPathCommandKind.VerticalLineTo,
                SvgPathCommandKind.VerticalLineTo
            }));
            Assert.That(result.PathData.Commands[0].Explicit, Is.True);
            Assert.That(result.PathData.Commands[1].Explicit, Is.False);
            Assert.That(result.PathData.Commands[3].Explicit, Is.True);
            Assert.That(result.PathData.Commands[4].Explicit, Is.False);
            Assert.That(result.PathData.Commands[8].EndX, Is.EqualTo(60));
            Assert.That(result.PathData.Commands[8].EndY, Is.EqualTo(80));
        });
    }

    [Test]
    public void EveryCurveAndArcCommandSupportsRepeatedParameterGroups()
    {
        string input = "M0 0 C1 2 3 4 5 6 7 8 9 10 11 12 S1 2 3 4 5 6 7 8 Q1 2 3 4 5 6 7 8 T1 2 3 4 A1 2 3 0 1 4 5 6 7 8 1 0 9 10";
        SvgPathParseResult result = new SvgPathParser().Parse(input, 16, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.PathData.Commands.Select(command => command.Kind), Is.EqualTo(new[]
            {
                SvgPathCommandKind.MoveTo,
                SvgPathCommandKind.CubicCurveTo,
                SvgPathCommandKind.CubicCurveTo,
                SvgPathCommandKind.SmoothCubicCurveTo,
                SvgPathCommandKind.SmoothCubicCurveTo,
                SvgPathCommandKind.QuadraticCurveTo,
                SvgPathCommandKind.QuadraticCurveTo,
                SvgPathCommandKind.SmoothQuadraticCurveTo,
                SvgPathCommandKind.SmoothQuadraticCurveTo,
                SvgPathCommandKind.ArcTo,
                SvgPathCommandKind.ArcTo
            }));
            Assert.That(result.PathData.Commands[2].Explicit, Is.False);
            Assert.That(result.PathData.Commands[10].Explicit, Is.False);
            Assert.That(result.PathData.Commands[10].Values, Is.EqualTo(new[] { 6.0, 7.0, 8.0, 1.0, 0.0, 9.0, 10.0 }));
        });
    }

    [Test]
    public void CompactSignsDecimalPointsAndExponentsSeparateNumbers()
    {
        SvgPathParseResult result = new SvgPathParser().Parse("M.5.6L10-20.5-.25e2-3", 8, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.PathData.Commands, Has.Count.EqualTo(3));
            Assert.That(result.PathData.Commands[0].Values, Is.EqualTo(new[] { 0.5, 0.6 }));
            Assert.That(result.PathData.Commands[1].Values, Is.EqualTo(new[] { 10.0, -20.5 }));
            Assert.That(result.PathData.Commands[2].Values, Is.EqualTo(new[] { -25.0, -3.0 }));
        });
    }

    [Test]
    public void ArcFlagsAndFollowingCoordinatesMayUseCompactSyntax()
    {
        SvgPathParseResult result = new SvgPathParser().Parse("M0 0A5 6 45 0110-20a7 8 0 10.5.6", 8, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.PathData.Commands, Has.Count.EqualTo(3));
            Assert.That(result.PathData.Commands[1].Values, Is.EqualTo(new[] { 5.0, 6.0, 45.0, 0.0, 1.0, 10.0, -20.0 }));
            Assert.That(result.PathData.Commands[2].Values, Is.EqualTo(new[] { 7.0, 8.0, 0.0, 1.0, 0.0, 0.5, 0.6 }));
            Assert.That(result.PathData.Commands[2].EndX, Is.EqualTo(10.5));
            Assert.That(result.PathData.Commands[2].EndY, Is.EqualTo(-19.4).Within(1e-12));
        });
    }

    [Test]
    public void ClosePathAndRelativeMoveTrackEachSubpathStart()
    {
        SvgPathParseResult result = new SvgPathParser().Parse("M10 10 l5 0 z m2 3 l4 5 z", 12, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.PathData.GetSubpathCount(), Is.EqualTo(2));
            Assert.That(result.PathData.Commands[2].StartX, Is.EqualTo(15));
            Assert.That(result.PathData.Commands[2].EndX, Is.EqualTo(10));
            Assert.That(result.PathData.Commands[3].StartX, Is.EqualTo(10));
            Assert.That(result.PathData.Commands[3].EndX, Is.EqualTo(12));
            Assert.That(result.PathData.Commands[3].EndY, Is.EqualTo(13));
            Assert.That(result.PathData.Commands[5].EndX, Is.EqualTo(12));
            Assert.That(result.PathData.Commands[5].EndY, Is.EqualTo(13));
            Assert.That(result.PathData.Commands[5].SubpathIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void MalformedGroupsRecoverAtLaterCommandsWithoutPartialSegments()
    {
        string input = "M0 0 X1 2 L3 3 4 nope Q4 5 6 7 A1 1 0 2 0 9 9 T8 9";
        SvgPathParseResult result = new SvgPathParser().Parse(input, 16, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgPathParseError.UnknownCommand));
            Assert.That(result.ErrorOffset, Is.EqualTo(input.IndexOf('X')));
            Assert.That(result.PathData.Commands.Select(command => command.Kind), Is.EqualTo(new[]
            {
                SvgPathCommandKind.MoveTo,
                SvgPathCommandKind.LineTo,
                SvgPathCommandKind.QuadraticCurveTo,
                SvgPathCommandKind.SmoothQuadraticCurveTo
            }));
            Assert.That(result.PathData.Issues.Select(issue => issue.Error), Is.EqualTo(new[]
            {
                SvgPathParseError.UnknownCommand,
                SvgPathParseError.ExpectedParameter,
                SvgPathParseError.InvalidArcFlag
            }));
            Assert.That(result.NextOffset, Is.EqualTo(input.Length));
        });
    }

    [Test]
    public void CommandsBeforeTheFirstMoveAreRejectedAndRecoveryContinues()
    {
        SvgPathParseResult result = new SvgPathParser().Parse("L1 2 H3 M4 5 L6 7", 8, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgPathParseError.ExpectedMoveTo));
            Assert.That(result.PathData.Commands.Select(command => command.Kind), Is.EqualTo(new[] { SvgPathCommandKind.MoveTo, SvgPathCommandKind.LineTo }));
            Assert.That(result.PathData.Commands[0].Values, Is.EqualTo(new[] { 4.0, 5.0 }));
        });
    }

    [Test]
    public void InvalidArcRadiiFlagsAndTrailingSeparatorsReportStableFailures()
    {
        SvgPathParseResult negativeRadius = new SvgPathParser().Parse("M0 0 A-1 2 0 0 0 3 4 L5 6", 8, 32);
        SvgPathParseResult invalidFlag = new SvgPathParser().Parse("M0 0 A1 2 0 0 3 4 5 L6 7", 8, 32);
        SvgPathParseResult trailing = new SvgPathParser().Parse("M0 0,", 8, 32);

        Assert.Multiple(() =>
        {
            Assert.That(negativeRadius.Error, Is.EqualTo(SvgPathParseError.NegativeArcRadius));
            Assert.That(negativeRadius.PathData.Commands.Select(command => command.Kind), Is.EqualTo(new[] { SvgPathCommandKind.MoveTo, SvgPathCommandKind.LineTo }));
            Assert.That(invalidFlag.Error, Is.EqualTo(SvgPathParseError.InvalidArcFlag));
            Assert.That(invalidFlag.PathData.Commands.Select(command => command.Kind), Is.EqualTo(new[] { SvgPathCommandKind.MoveTo, SvgPathCommandKind.LineTo }));
            Assert.That(trailing.Error, Is.EqualTo(SvgPathParseError.ExpectedParameter));
            Assert.That(trailing.PathData.Commands, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void SegmentAndTokenBoundsReturnValidPrefixes()
    {
        SvgPathParseResult segmentLimit = new SvgPathParser().Parse("M0 0 L1 1 2 2 3 3", 3, 32);
        SvgPathParseResult tokenLimit = new SvgPathParser().Parse("M12345 0 L1 1", 4, 4);
        SvgPathParseResult resolvedOverflow = new SvgPathParser().Parse("M1e308 0 l1e308 0 L2 2", 8, 32);

        Assert.Multiple(() =>
        {
            Assert.That(segmentLimit.Error, Is.EqualTo(SvgPathParseError.SegmentLimitExceeded));
            Assert.That(segmentLimit.PathData.Commands, Has.Count.EqualTo(3));
            Assert.That(segmentLimit.NextOffset, Is.LessThan("M0 0 L1 1 2 2 3 3".Length));
            Assert.That(tokenLimit.Error, Is.EqualTo(SvgPathParseError.TokenTooLong));
            Assert.That(tokenLimit.PathData.Commands, Is.Empty);
            Assert.That(resolvedOverflow.Error, Is.EqualTo(SvgPathParseError.InvalidNumber));
            Assert.That(resolvedOverflow.PathData.Commands.Select(command => command.Kind), Is.EqualTo(new[] { SvgPathCommandKind.MoveTo, SvgPathCommandKind.LineTo }));
            Assert.That(resolvedOverflow.PathData.Commands[1].EndX, Is.EqualTo(2));
        });
    }

    [Test]
    public void EmptyPathsAreValidAndInvalidArgumentsAreRejected()
    {
        SvgPathParseResult empty = new SvgPathParser().Parse(" \t\r\n", 4, 16);
        SvgPathParseResult nullValue = new SvgPathParser().Parse(null, 4, 16);
        SvgPathParseResult itemBound = new SvgPathParser().Parse("M0 0", 0, 16);
        SvgPathParseResult tokenBound = new SvgPathParser().Parse("M0 0", 4, 0);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.PathData.Commands, Is.Empty);
            Assert.That(empty.PathData.GetSubpathCount(), Is.Zero);
            Assert.That(nullValue.Error, Is.EqualTo(SvgPathParseError.InvalidArgument));
            Assert.That(itemBound.Error, Is.EqualTo(SvgPathParseError.InvalidArgument));
            Assert.That(tokenBound.Error, Is.EqualTo(SvgPathParseError.InvalidArgument));
        });
    }
}
