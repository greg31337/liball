namespace svg.Tests;

public class SvgPathNormalizerTests
{
    [Test]
    public void NormalizationMakesRelativeImplicitAndAxisCommandsAbsoluteExplicitLines()
    {
        SvgPathData source = Parse("m10 20 5 5 h10 v-5 l2 3 z");

        SvgPathNormalizeResult normalized = source.Normalize();

        Assert.Multiple(() =>
        {
            AssertSuccess(normalized, 6);
            AssertKinds(normalized.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.ClosePath);
            AssertCommand(normalized.PathData.Commands[0], new double[] { 10, 20 });
            AssertCommand(normalized.PathData.Commands[1], new double[] { 15, 25 });
            AssertCommand(normalized.PathData.Commands[2], new double[] { 25, 25 });
            AssertCommand(normalized.PathData.Commands[3], new double[] { 25, 20 });
            AssertCommand(normalized.PathData.Commands[4], new double[] { 27, 23 });
            Assert.That(normalized.PathData.Commands.All(command => !command.Relative && command.Explicit), Is.True);
            Assert.That(source.Commands[0].Relative, Is.True);
            Assert.That(source.Commands[1].Explicit, Is.False);
        });
    }

    [Test]
    public void SmoothCubicsReflectOnlyAContiguousCubicControl()
    {
        SvgPathData source = Parse("M10 10 C20 20 30 20 40 10 S60 0 70 10 s20 10 30 0 L110 10 S120 20 130 10");

        SvgPathNormalizeResult normalized = new SvgPathNormalizer().NormalizeWithPath(source);

        Assert.Multiple(() =>
        {
            AssertSuccess(normalized, 6);
            AssertKinds(normalized.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.CubicCurveTo, SvgPathCommandKind.CubicCurveTo, SvgPathCommandKind.CubicCurveTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.CubicCurveTo);
            AssertCommand(normalized.PathData.Commands[2], new double[] { 50, 0, 60, 0, 70, 10 });
            AssertCommand(normalized.PathData.Commands[3], new double[] { 80, 20, 90, 20, 100, 10 });
            AssertCommand(normalized.PathData.Commands[5], new double[] { 110, 10, 120, 20, 130, 10 });
        });
    }

    [Test]
    public void SmoothQuadraticsReflectControlsAndResetAfterOtherCommands()
    {
        SvgPathData source = Parse("M0 0 Q3 6 6 0 T12 0 t6 0 L20 0 T25 5");

        SvgPathNormalizeResult normalized = source.Normalize();

        Assert.Multiple(() =>
        {
            AssertSuccess(normalized, 6);
            AssertKinds(normalized.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.QuadraticCurveTo, SvgPathCommandKind.QuadraticCurveTo, SvgPathCommandKind.QuadraticCurveTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.QuadraticCurveTo);
            AssertCommand(normalized.PathData.Commands[1], new double[] { 3, 6, 6, 0 });
            AssertCommand(normalized.PathData.Commands[2], new double[] { 9, -6, 12, 0 });
            AssertCommand(normalized.PathData.Commands[3], new double[] { 15, 6, 18, 0 });
            AssertCommand(normalized.PathData.Commands[5], new double[] { 20, 0, 25, 5 });
        });
    }

    [Test]
    public void QuadraticConversionProducesEquivalentExplicitCubics()
    {
        SvgPathData source = Parse("M0 0 Q3 6 6 0 T12 0");

        SvgPathNormalizeResult normalized = source.NormalizeToCubics();

        Assert.Multiple(() =>
        {
            AssertSuccess(normalized, 3);
            AssertKinds(normalized.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.CubicCurveTo, SvgPathCommandKind.CubicCurveTo);
            AssertCommand(normalized.PathData.Commands[1], new double[] { 2, 4, 4, 4, 6, 0 });
            AssertCommand(normalized.PathData.Commands[2], new double[] { 8, -4, 10, -4, 12, 0 });
        });
    }

    [Test]
    public void RelativeArcsPreserveRadiiRotationAndFlags()
    {
        SvgPathData source = Parse("M10 20 a5 6 45 1 0 7 8");

        SvgPathNormalizeResult normalized = source.Normalize();

        Assert.Multiple(() =>
        {
            AssertSuccess(normalized, 2);
            AssertKinds(normalized.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.ArcTo);
            AssertCommand(normalized.PathData.Commands[1], new double[] { 5, 6, 45, 1, 0, 17, 28 });
            Assert.That(normalized.PathData.Commands[1].StartX, Is.EqualTo(10));
            Assert.That(normalized.PathData.Commands[1].EndX, Is.EqualTo(17));
        });
    }

    [Test]
    public void NormalizationRecomputesSubpathBookkeepingFromPayloads()
    {
        SvgPathData source = Parse("M10 10 l5 0 z m2 3 l4 5 z");
        foreach (SvgPathCommand command in source.Commands)
        {
            command.StartX = 999;
            command.StartY = 999;
            command.EndX = 999;
            command.EndY = 999;
            command.SubpathStartX = 999;
            command.SubpathStartY = 999;
            command.SubpathIndex = 99;
        }

        SvgPathNormalizeResult normalized = source.Normalize();

        Assert.Multiple(() =>
        {
            AssertSuccess(normalized, 6);
            Assert.That(normalized.PathData.GetSubpathCount(), Is.EqualTo(2));
            Assert.That(normalized.PathData.Commands[0].SubpathIndex, Is.Zero);
            Assert.That(normalized.PathData.Commands[2].StartX, Is.EqualTo(15));
            Assert.That(normalized.PathData.Commands[2].EndX, Is.EqualTo(10));
            Assert.That(normalized.PathData.Commands[3].StartX, Is.EqualTo(10));
            Assert.That(normalized.PathData.Commands[3].EndX, Is.EqualTo(12));
            Assert.That(normalized.PathData.Commands[3].EndY, Is.EqualTo(13));
            Assert.That(normalized.PathData.Commands[5].SubpathIndex, Is.EqualTo(1));
            Assert.That(normalized.PathData.Commands[5].EndX, Is.EqualTo(12));
            Assert.That(normalized.PathData.Commands[5].EndY, Is.EqualTo(13));
        });
    }

    [Test]
    public void QuadraticConversionAvoidsOverflowFromFiniteEquivalentControls()
    {
        double maximum = double.MaxValue;
        SvgPathCommand move = new SvgPathCommand(SvgPathCommandKind.MoveTo, false, new double[] { maximum, 0 }, 0, 1, true, 0, 0, maximum, 0, maximum, 0, 0);
        SvgPathCommand quadratic = new SvgPathCommand(SvgPathCommandKind.QuadraticCurveTo, false, new double[] { -maximum, 0, 0, 0 }, 1, 2, true, maximum, 0, 0, 0, maximum, 0, 0);
        SvgPathData source = Path(move, quadratic);

        SvgPathNormalizeResult normalized = source.NormalizeToCubics();

        Assert.Multiple(() =>
        {
            AssertSuccess(normalized, 2);
            Assert.That(normalized.PathData.Commands[1].Values[0], Is.EqualTo(-maximum / 3).Within(maximum * 1e-15));
            Assert.That(normalized.PathData.Commands[1].Values[2], Is.EqualTo(-maximum / 3 * 2).Within(maximum * 1e-15));
            Assert.That(double.IsFinite(normalized.PathData.Commands[1].Values[0]), Is.True);
            Assert.That(double.IsFinite(normalized.PathData.Commands[1].Values[2]), Is.True);
        });
    }

    [Test]
    public void EmptyPathNormalizesWhileLimitsInvalidStateAndOverflowFailStably()
    {
        SvgPathNormalizeResult empty = Parse("").Normalize();
        SvgPathNormalizeResult limited = new SvgPathNormalizer().NormalizeToCubicsWithPathAndMaximumCommands(Parse("M0 0L1 1"), 1);
        SvgPathData invalidPayload = Parse("M0 0L1 1");
        invalidPayload.Commands[1].Values = new double[] { 1 };
        SvgPathNormalizeResult invalid = invalidPayload.Normalize();
        SvgPathData recovered = new SvgPathParser().Parse("M0 0 X L1 1", 8, 32).PathData;
        SvgPathCommand move = new SvgPathCommand(SvgPathCommandKind.MoveTo, false, new double[] { double.MaxValue, 0 }, 0, 1, true, 0, 0, double.MaxValue, 0, double.MaxValue, 0, 0);
        SvgPathCommand relativeLine = new SvgPathCommand(SvgPathCommandKind.LineTo, true, new double[] { double.MaxValue, 0 }, 1, 2, true, double.MaxValue, 0, 0, 0, double.MaxValue, 0, 0);
        SvgPathNormalizeResult overflow = Path(move, relativeLine).Normalize();
        SvgPathNormalizeOptions badOptions = new SvgPathNormalizeOptions();
        badOptions.MaximumCommands = 0;

        Assert.Multiple(() =>
        {
            AssertSuccess(empty, 0);
            AssertFailure(limited, SvgPathNormalizeError.CommandLimitExceeded);
            AssertFailure(invalid, SvgPathNormalizeError.InvalidPath);
            AssertFailure(recovered.Normalize(), SvgPathNormalizeError.InvalidPath);
            AssertFailure(overflow, SvgPathNormalizeError.NonFiniteResult);
            AssertFailure(new SvgPathNormalizer().NormalizeWithPath(null), SvgPathNormalizeError.InvalidArgument);
            AssertFailure(new SvgPathNormalizer().NormalizeWithPathAndOptions(Parse("M0 0"), badOptions), SvgPathNormalizeError.InvalidArgument);
        });
    }

    static SvgPathData Parse(string source)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(source, 64, 64);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static SvgPathData Path(params SvgPathCommand[] commands)
    {
        return new SvgPathData(new List<SvgPathCommand>(commands), "", new List<SvgPathParseIssue>());
    }

    static void AssertSuccess(SvgPathNormalizeResult result, int commandCount)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgPathNormalizeError.None));
        Assert.That(result.PathData, Is.Not.Null);
        Assert.That(result.PathData.IsValid, Is.True);
        Assert.That(result.PathData.Commands, Has.Count.EqualTo(commandCount));
    }

    static void AssertFailure(SvgPathNormalizeResult result, SvgPathNormalizeError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.PathData, Is.Null);
    }

    static void AssertKinds(SvgPathData path, params SvgPathCommandKind[] kinds)
    {
        Assert.That(path.Commands.Select(command => command.Kind), Is.EqualTo(kinds));
    }

    static void AssertCommand(SvgPathCommand command, double[] values)
    {
        Assert.That(command.Values, Is.EqualTo(values));
        Assert.That(command.Relative, Is.False);
        Assert.That(command.Explicit, Is.True);
        Assert.That(command.IsValid, Is.True);
    }
}
