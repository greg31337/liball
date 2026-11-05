namespace svg.Tests;

public class SvgPathTransformerTests
{
    [Test]
    public void TransformNormalizesAndMapsEveryNonArcSegmentPoint()
    {
        SvgPathData source = Parse("m1 2 h3 Q5 3 6 4 C7 5 8 6 9 7 z");
        SvgMatrix matrix = new SvgMatrix(2, 0, 0, 3, 10, -5);

        SvgPathTransformResult transformed = source.Transform(matrix);

        Assert.Multiple(() =>
        {
            AssertSuccess(transformed, 5);
            AssertKinds(transformed.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.QuadraticCurveTo, SvgPathCommandKind.CubicCurveTo, SvgPathCommandKind.ClosePath);
            AssertCommand(transformed.PathData.Commands[0], new double[] { 12, 1 });
            AssertCommand(transformed.PathData.Commands[1], new double[] { 18, 1 });
            AssertCommand(transformed.PathData.Commands[2], new double[] { 20, 4, 22, 7 });
            AssertCommand(transformed.PathData.Commands[3], new double[] { 24, 10, 26, 13, 28, 16 });
            Assert.That(transformed.PathData.Commands.All(command => !command.Relative && command.Explicit), Is.True);
            Assert.That(source.Commands[0].Relative, Is.True);
        });
    }

    [Test]
    public void OrthogonalAxisScalingPreservesArcCommandsExactly()
    {
        SvgPathData source = Parse("M1 0 A1 1 0 0 1 0 1");

        SvgPathTransformResult transformed = source.Transform(new SvgMatrix(2, 0, 0, 3, 0, 0));

        Assert.Multiple(() =>
        {
            AssertSuccess(transformed, 2);
            AssertKinds(transformed.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.ArcTo);
            AssertCommand(transformed.PathData.Commands[0], new double[] { 2, 0 });
            AssertCommand(transformed.PathData.Commands[1], new double[] { 2, 3, 0, 0, 1, 0, 3 });
        });
    }

    [Test]
    public void RotationAndReflectionPreserveArcsAndUpdateOrientation()
    {
        SvgPathData source = Parse("M1 0 A1 1 0 0 1 0 1");

        SvgPathTransformResult rotated = source.Transform(new SvgMatrix(0, 1, -1, 0, 0, 0));
        SvgPathTransformResult reflected = source.Transform(new SvgMatrix(-1, 0, 0, 1, 0, 0));

        Assert.Multiple(() =>
        {
            AssertSuccess(rotated, 2);
            AssertKinds(rotated.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.ArcTo);
            Assert.That(rotated.PathData.Commands[1].Values[0], Is.EqualTo(1));
            Assert.That(rotated.PathData.Commands[1].Values[1], Is.EqualTo(1));
            Assert.That(rotated.PathData.Commands[1].Values[2], Is.EqualTo(90).Within(1e-12));
            Assert.That(rotated.PathData.Commands[1].Values[4], Is.EqualTo(1));
            Assert.That(rotated.PathData.Commands[1].EndX, Is.EqualTo(-1));
            Assert.That(rotated.PathData.Commands[1].EndY, Is.Zero);
            AssertSuccess(reflected, 2);
            Assert.That(reflected.PathData.Commands[1].Kind, Is.EqualTo(SvgPathCommandKind.ArcTo));
            Assert.That(reflected.PathData.Commands[1].Values[2], Is.EqualTo(180).Within(1e-12));
            Assert.That(reflected.PathData.Commands[1].Values[4], Is.Zero);
            Assert.That(reflected.PathData.Commands[1].EndX, Is.Zero);
            Assert.That(reflected.PathData.Commands[1].EndY, Is.EqualTo(1));
        });
    }

    [Test]
    public void ShearConvertsArcToTransformedCubicSegments()
    {
        SvgPathData source = Parse("M1 0 A1 1 0 0 1 0 1");
        double control = 4.0 / 3.0 * System.Math.Tan(System.Math.PI / 8);

        SvgPathTransformResult transformed = source.Transform(new SvgMatrix(1, 0, 1, 1, 0, 0));

        Assert.Multiple(() =>
        {
            AssertSuccess(transformed, 2);
            AssertKinds(transformed.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.CubicCurveTo);
            AssertCommandWithin(transformed.PathData.Commands[1], new double[] { 1 + control, control, 1 + control, 1, 1, 1 });
            Assert.That(transformed.PathData.Commands[0].EndX, Is.EqualTo(1));
            Assert.That(transformed.PathData.Commands[0].EndY, Is.Zero);
        });
    }

    [Test]
    public void ArcPreservationCanBeDisabledAndSingularMapsRemainFinite()
    {
        SvgPathData source = Parse("M1 0 A1 1 0 0 1 0 1");
        SvgPathTransformOptions options = new SvgPathTransformOptions();
        options.PreserveArcs = false;

        SvgPathTransformResult cubic = source.TransformWithOptions(new SvgMatrix(1, 0, 0, 1, 0, 0), options);
        SvgPathTransformResult collapsed = source.Transform(new SvgMatrix(1, 0, 0, 0, 0, 0));

        Assert.Multiple(() =>
        {
            AssertSuccess(cubic, 2);
            Assert.That(cubic.PathData.Commands[1].Kind, Is.EqualTo(SvgPathCommandKind.CubicCurveTo));
            AssertSuccess(collapsed, 2);
            Assert.That(collapsed.PathData.Commands[1].Kind, Is.EqualTo(SvgPathCommandKind.CubicCurveTo));
            Assert.That(collapsed.PathData.Commands[1].Values[1], Is.Zero);
            Assert.That(collapsed.PathData.Commands[1].Values[3], Is.Zero);
            Assert.That(collapsed.PathData.Commands[1].Values[5], Is.Zero);
        });
    }

    [Test]
    public void DegenerateArcsBecomeLinesOrNoOpCommands()
    {
        SvgPathData source = Parse("M0 0 A0 2 0 0 1 6 3 A2 2 0 0 1 6 3 L8 4");

        SvgPathTransformResult transformed = source.Transform(new SvgMatrix(2, 0, 0, 2, 1, -1));

        Assert.Multiple(() =>
        {
            AssertSuccess(transformed, 3);
            AssertKinds(transformed.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.LineTo);
            AssertCommand(transformed.PathData.Commands[0], new double[] { 1, -1 });
            AssertCommand(transformed.PathData.Commands[1], new double[] { 13, 5 });
            AssertCommand(transformed.PathData.Commands[2], new double[] { 17, 7 });
        });
    }

    [Test]
    public void ExpandedArcCommandsHonorTheOutputLimit()
    {
        SvgPathData source = Parse("M1 0 A1 1 0 1 1 0 1");
        SvgPathTransformOptions limited = new SvgPathTransformOptions();
        limited.MaximumCommands = 3;
        SvgPathTransformOptions exact = new SvgPathTransformOptions();
        exact.MaximumCommands = 4;

        SvgPathTransformResult failure = source.TransformWithOptions(new SvgMatrix(1, 0, 1, 1, 0, 0), limited);
        SvgPathTransformResult success = source.TransformWithOptions(new SvgMatrix(1, 0, 1, 1, 0, 0), exact);

        Assert.Multiple(() =>
        {
            AssertFailure(failure, SvgPathTransformError.CommandLimitExceeded);
            AssertSuccess(success, 4);
            Assert.That(success.PathData.Commands.Skip(1).All(command => command.Kind == SvgPathCommandKind.CubicCurveTo), Is.True);
        });
    }

    [Test]
    public void TransformRejectsInvalidPathMatrixOptionsAndOverflow()
    {
        SvgPathData recovered = new SvgPathParser().Parse("M0 0 X L1 1", 8, 32).PathData;
        SvgMatrix invalidMatrix = new SvgMatrix(1, 0, 0, 1, 0, 0);
        invalidMatrix.A = double.NaN;
        SvgPathTransformOptions invalidOptions = new SvgPathTransformOptions();
        invalidOptions.MaximumCommands = 0;
        SvgPathData huge = Parse("M1e308 0 L1e308 1");

        SvgPathTransformResult invalidPath = new SvgPathTransformer().TransformWithPathAndMatrix(recovered, new SvgMatrix(1, 0, 0, 1, 0, 0));
        SvgPathTransformResult invalid = new SvgPathTransformer().TransformWithPathAndMatrix(Parse("M0 0"), invalidMatrix);
        SvgPathTransformResult overflow = huge.Transform(new SvgMatrix(2, 0, 0, 1, 0, 0));

        Assert.Multiple(() =>
        {
            AssertFailure(invalidPath, SvgPathTransformError.InvalidPath);
            AssertFailure(invalid, SvgPathTransformError.InvalidArgument);
            AssertFailure(overflow, SvgPathTransformError.NonFiniteResult);
            AssertFailure(new SvgPathTransformer().TransformWithPathAndMatrix(null, new SvgMatrix(1, 0, 0, 1, 0, 0)), SvgPathTransformError.InvalidArgument);
            AssertFailure(new SvgPathTransformer().TransformWithPathAndMatrixAndOptions(Parse("M0 0"), new SvgMatrix(1, 0, 0, 1, 0, 0), invalidOptions), SvgPathTransformError.InvalidArgument);
        });
    }

    static SvgPathData Parse(string source)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(source, 64, 64);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static void AssertSuccess(SvgPathTransformResult result, int count)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgPathTransformError.None));
        Assert.That(result.PathData, Is.Not.Null);
        Assert.That(result.PathData.Commands, Has.Count.EqualTo(count));
        Assert.That(result.PathData.IsValid, Is.True);
    }

    static void AssertFailure(SvgPathTransformResult result, SvgPathTransformError error)
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
    }

    static void AssertCommandWithin(SvgPathCommand command, double[] values)
    {
        Assert.That(command.Values, Has.Length.EqualTo(values.Length));
        int index = 0;
        while (index < values.Length)
        {
            Assert.That(command.Values[index], Is.EqualTo(values[index]).Within(1e-12));
            index++;
        }
    }
}
