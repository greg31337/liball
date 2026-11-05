namespace svg.Tests;

public class SvgPathBuilderTests
{
    [Test]
    public void BuildsEveryAbsoluteAndRelativeCommandWithResolvedBookkeeping()
    {
        SvgPathBuilder builder = new SvgPathBuilder();
        SvgPathBuilder returned = builder
            .MoveTo(1, 2)
            .LineBy(2, 3)
            .HorizontalTo(10)
            .VerticalBy(2)
            .CubicTo(11, 8, 12, 9, 13, 10)
            .SmoothCubicBy(1, 2, 3, 4)
            .QuadraticTo(18, 16, 20, 18)
            .SmoothQuadraticBy(2, 3)
            .ArcTo(4, 5, 30, true, false, 30, 25)
            .ArcBy(2, 3, 0, false, true, 5, -5)
            .Close();

        SvgPathBuildResult result = builder.Build();
        SvgPathData path = result.PathData;

        Assert.Multiple(() =>
        {
            Assert.That(returned, Is.SameAs(builder));
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(builder.GetCommandCount(), Is.EqualTo(11));
            Assert.That(path.Commands.Select(item => item.Kind), Is.EqualTo(new[]
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
                SvgPathCommandKind.ArcTo,
                SvgPathCommandKind.ClosePath
            }));
            Assert.That(path.Commands.Select(item => item.Relative), Is.EqualTo(new[] { false, true, false, true, false, true, false, true, false, true, false }));
            Assert.That(new[] { path.Commands[1].EndX, path.Commands[1].EndY }, Is.EqualTo(new[] { 3d, 5d }));
            Assert.That(new[] { path.Commands[3].EndX, path.Commands[3].EndY }, Is.EqualTo(new[] { 10d, 7d }));
            Assert.That(new[] { path.Commands[5].EndX, path.Commands[5].EndY }, Is.EqualTo(new[] { 16d, 14d }));
            Assert.That(new[] { path.Commands[7].EndX, path.Commands[7].EndY }, Is.EqualTo(new[] { 22d, 21d }));
            Assert.That(new[] { path.Commands[9].EndX, path.Commands[9].EndY }, Is.EqualTo(new[] { 35d, 20d }));
            Assert.That(new[] { path.Commands[10].EndX, path.Commands[10].EndY }, Is.EqualTo(new[] { 1d, 2d }));
            Assert.That(new SvgPathParser().Parse(path.ToMinifiedText(), 100, 100).Success, Is.True);
        });
    }

    [Test]
    public void ShapeHelpersProduceIndependentClosedAndOpenSubpaths()
    {
        SvgPathBuilder builder = new SvgPathBuilder();
        builder.Line(0, 0, 1, 1)
            .Rectangle(2, 3, 4, 5)
            .RoundedRectangle(10, 20, 8, 6, 2, 1)
            .Circle(30, 40, 5)
            .Ellipse(50, 60, 4, 2)
            .Polyline(new double[] { 70, 0, 71, 1, 72, 0 })
            .Polygon(new double[] { 80, 0, 81, 1, 82, 0 });

        SvgPathBuildResult result = builder.Build();
        SvgPathData path = result.PathData;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(path.GetSubpathCount(), Is.EqualTo(7));
            Assert.That(path.Commands.Count(item => item.Kind == SvgPathCommandKind.ClosePath), Is.EqualTo(5));
            Assert.That(path.Commands.Count(item => item.Kind == SvgPathCommandKind.ArcTo), Is.EqualTo(8));
            Assert.That(path.Commands[0].Kind, Is.EqualTo(SvgPathCommandKind.MoveTo));
            Assert.That(path.Commands[1].Kind, Is.EqualTo(SvgPathCommandKind.LineTo));
            Assert.That(path.Commands.Any(item => item.Kind == SvgPathCommandKind.ArcTo && item.Values[0] == 2 && item.Values[1] == 1), Is.True);
            Assert.That(path.Commands[^1].Kind, Is.EqualTo(SvgPathCommandKind.ClosePath));
            Assert.That(path.IsValid, Is.True);
        });
    }

    [Test]
    public void CenterArcHelpersSplitFullTurnsAndRetainRotatedEllipseParameters()
    {
        SvgPathBuilder builder = new SvgPathBuilder();
        builder.Arc(0, 0, 10, 0, 360)
            .EllipseArc(20, 30, 8, 4, 45, 90, -270);

        SvgPathBuildResult result = builder.Build();
        SvgPathData path = result.PathData;
        List<SvgPathCommand> arcs = path.Commands.Where(item => item.Kind == SvgPathCommandKind.ArcTo).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(path.GetSubpathCount(), Is.EqualTo(2));
            Assert.That(arcs, Has.Count.EqualTo(4));
            Assert.That(arcs.Take(2).All(item => item.Values[0] == 10 && item.Values[1] == 10 && item.Values[4] == 1), Is.True);
            Assert.That(arcs.Skip(2).All(item => item.Values[0] == 8 && item.Values[1] == 4 && item.Values[2] == 45 && item.Values[4] == 0), Is.True);
            Assert.That(path.Commands[0].EndX, Is.EqualTo(10).Within(1e-12));
            Assert.That(path.Commands[0].EndY, Is.Zero.Within(1e-12));
            Assert.That(arcs[1].EndX, Is.EqualTo(10).Within(1e-12));
            Assert.That(arcs[1].EndY, Is.Zero.Within(1e-12));
        });
    }

    [Test]
    public void AppendReverseAndTransformComposeWithoutMutatingSources()
    {
        SvgPathData source = Parse("m1 2 c1 0 2 1 3 1 q2 2 4 0 a2 3 10 0 1 5 4 z");
        string normalizedSource = source.Normalize().PathData.ToText();
        SvgPathBuilder reversed = new SvgPathBuilder();
        reversed.Append(source).Reverse();
        SvgPathData once = reversed.ToPathData();
        reversed.Reverse();
        SvgPathData twice = reversed.ToPathData();

        SvgPathBuilder suffix = new SvgPathBuilder();
        suffix.MoveTo(0, 0).LineTo(2, 0);
        SvgPathBuilder composed = new SvgPathBuilder();
        composed.Append(source).AppendBuilder(suffix).Transform(new SvgMatrix(2, 0, 0, 3, 10, -5));
        SvgPathBuildResult transformed = composed.Build();

        Assert.Multiple(() =>
        {
            Assert.That(once.Commands[0].EndX, Is.EqualTo(source.Normalize().PathData.Commands[^2].EndX));
            Assert.That(once.Commands.Any(item => item.Kind == SvgPathCommandKind.ArcTo && item.Values[4] == 0), Is.True);
            Assert.That(twice.ToText(), Is.EqualTo(normalizedSource));
            Assert.That(source.Commands[0].Relative, Is.True);
            Assert.That(suffix.GetCommandCount(), Is.EqualTo(2));
            Assert.That(transformed.Success, Is.True, transformed.Message);
            Assert.That(transformed.PathData.GetSubpathCount(), Is.EqualTo(2));
            Assert.That(new[] { transformed.PathData.Commands[0].EndX, transformed.PathData.Commands[0].EndY }, Is.EqualTo(new[] { 12d, 1d }));
            Assert.That(new[] { transformed.PathData.Commands[^1].EndX, transformed.PathData.Commands[^1].EndY }, Is.EqualTo(new[] { 14d, -5d }));
        });
    }

    [Test]
    public void ConvertsToDetachedOrAttachedPathElementsWithIndependentSnapshots()
    {
        SvgPathBuilder pathBuilder = new SvgPathBuilder();
        pathBuilder.Rectangle(1, 2, 3, 4);

        SvgPathBuildResult detached = pathBuilder.ToPathElement();
        SvgBuilder documentBuilder = new SvgBuilder();
        documentBuilder.BeginElement(SvgElementKind.G).SetAttribute("id", "paths");
        SvgPathBuildResult attached = pathBuilder.AppendTo(documentBuilder);
        detached.PathData.Commands[0].Values[0] = 999;
        SvgPathData fresh = pathBuilder.ToPathData();

        Assert.Multiple(() =>
        {
            Assert.That(detached.Success, Is.True, detached.Message);
            Assert.That(detached.Element.ElementKind, Is.EqualTo(SvgElementKind.Path));
            Assert.That(detached.Element.OwnerDocument, Is.Null);
            Assert.That(Parse(Attribute(detached.Element, "d")).Commands, Has.Count.EqualTo(5));
            Assert.That(attached.Success, Is.True, attached.Message);
            Assert.That(attached.Element.OwnerDocument, Is.SameAs(documentBuilder.Document));
            Assert.That(documentBuilder.Current.Children.Single(), Is.SameAs(attached.Element));
            Assert.That(fresh.Commands[0].Values[0], Is.EqualTo(1));
            Assert.That(fresh.Commands[0], Is.Not.SameAs(detached.PathData.Commands[0]));
        });
    }

    [Test]
    public void ValidationAndLimitsReportStickyAtomicFailuresAndExplicitRecovery()
    {
        SvgPathBuilder builder = SvgPathBuilder.Create(4);
        builder.LineTo(1, 1).MoveTo(0, 0);
        Assert.Multiple(() =>
        {
            Assert.That(builder.Error, Is.EqualTo(SvgPathBuilderError.ExpectedMoveTo));
            Assert.That(builder.GetCommandCount(), Is.Zero);
            Assert.That(builder.Build().Success, Is.False);
        });

        builder.Clear().MoveTo(double.NaN, 0);
        Assert.That(builder.Error, Is.EqualTo(SvgPathBuilderError.InvalidArgument));

        builder.Clear().MoveTo(0, 0).ArcTo(-1, 2, 0, false, true, 1, 1);
        Assert.That(builder.Error, Is.EqualTo(SvgPathBuilderError.InvalidArgument));
        Assert.That(builder.GetCommandCount(), Is.EqualTo(1));

        builder.Clear().Rectangle(0, 0, 10, 10);
        Assert.Multiple(() =>
        {
            Assert.That(builder.Error, Is.EqualTo(SvgPathBuilderError.CommandLimitExceeded));
            Assert.That(builder.GetCommandCount(), Is.Zero);
        });

        SvgPathBuilder overflowingArc = new SvgPathBuilder();
        overflowingArc.EllipseArc(double.MaxValue, 0, double.MaxValue, 1, 0, 180, 180);
        Assert.Multiple(() =>
        {
            Assert.That(overflowingArc.Error, Is.EqualTo(SvgPathBuilderError.NonFiniteResult));
            Assert.That(overflowingArc.GetCommandCount(), Is.Zero);
        });

        builder.Clear().MoveTo(0, 0).LineTo(1, 1);
        builder.MaximumCommands = 0;
        Assert.That(builder.Validate().Error, Is.EqualTo(SvgPathBuilderError.InvalidArgument));

        builder.MaximumCommands = 4;
        SvgPathData invalid = Parse("M0 0L1 1");
        invalid.Commands[1].Values[0] = double.NaN;
        builder.Clear().Append(invalid);
        Assert.That(builder.Error, Is.EqualTo(SvgPathBuilderError.InvalidPath));

        SvgMatrix invalidMatrix = new SvgMatrix(1, 0, 0, 1, 0, 0);
        invalidMatrix.A = double.NaN;
        builder.Clear().MoveTo(0, 0).Transform(invalidMatrix);
        Assert.Multiple(() =>
        {
            Assert.That(builder.Error, Is.EqualTo(SvgPathBuilderError.TransformFailed));
            Assert.That(builder.TransformError, Is.EqualTo(SvgPathTransformError.InvalidArgument));
        });

        builder.ClearError().Clear().MoveBy(2, 3);
        Assert.Multiple(() =>
        {
            Assert.That(builder.Succeeded(), Is.True, builder.Message);
            Assert.That(builder.ToPathData().Commands.Single().Relative, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => SvgPathBuilder.Create(0));
        });
    }

    static SvgPathData Parse(string source)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(source, 1000, 1000);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static string Attribute(SvgNode element, string localName)
    {
        return element.Attributes.Single(item => item.NamespaceUri.Length == 0 && item.LocalName == localName).Value;
    }
}
