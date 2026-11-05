namespace svg.Tests;

public class SvgShapePathConverterTests
{
    [Test]
    public void LineConversionRetainsCoincidentEndpoints()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><line x1='1' y1='2' x2='3' y2='4'/><line x1='5' y1='6' x2='5' y2='6'/></svg>");

        SvgShapePathResult line = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[0]).ToPath();
        SvgShapePathResult pointLine = new SvgShapePathConverter().ConvertWithShape(new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[1]));

        Assert.Multiple(() =>
        {
            AssertSuccess(line, 2);
            AssertCommand(line.PathData.Commands[0], SvgPathCommandKind.MoveTo, new double[] { 1, 2 });
            AssertCommand(line.PathData.Commands[1], SvgPathCommandKind.LineTo, new double[] { 3, 4 });
            AssertSuccess(pointLine, 2);
            AssertCommand(pointLine.PathData.Commands[0], SvgPathCommandKind.MoveTo, new double[] { 5, 6 });
            AssertCommand(pointLine.PathData.Commands[1], SvgPathCommandKind.LineTo, new double[] { 5, 6 });
        });
    }

    [Test]
    public void PlainRectangleConversionProducesClosedCornerPath()
    {
        SvgShapeResult rectangle = Resolve("<rect x='1' y='2' width='3' height='4'/>");

        SvgShapePathResult converted = rectangle.ToPath();

        Assert.Multiple(() =>
        {
            AssertSuccess(converted, 5);
            AssertKinds(converted.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.ClosePath);
            AssertCommand(converted.PathData.Commands[0], SvgPathCommandKind.MoveTo, new double[] { 1, 2 });
            AssertCommand(converted.PathData.Commands[1], SvgPathCommandKind.LineTo, new double[] { 4, 2 });
            AssertCommand(converted.PathData.Commands[2], SvgPathCommandKind.LineTo, new double[] { 4, 6 });
            AssertCommand(converted.PathData.Commands[3], SvgPathCommandKind.LineTo, new double[] { 1, 6 });
            Assert.That(converted.PathData.Commands[4].EndX, Is.EqualTo(1));
            Assert.That(converted.PathData.Commands[4].EndY, Is.EqualTo(2));
        });
    }

    [Test]
    public void RoundedRectangleConversionUsesFourClockwiseCornerArcs()
    {
        SvgShapeResult rectangle = Resolve("<rect x='10' y='20' width='100' height='50' rx='10' ry='5'/>");

        SvgShapePathResult converted = rectangle.ToPath();

        Assert.Multiple(() =>
        {
            AssertSuccess(converted, 10);
            AssertKinds(converted.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.ArcTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.ArcTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.ArcTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.ArcTo, SvgPathCommandKind.ClosePath);
            AssertCommand(converted.PathData.Commands[0], SvgPathCommandKind.MoveTo, new double[] { 20, 20 });
            AssertCommand(converted.PathData.Commands[1], SvgPathCommandKind.LineTo, new double[] { 100, 20 });
            AssertCommand(converted.PathData.Commands[2], SvgPathCommandKind.ArcTo, new double[] { 10, 5, 0, 0, 1, 110, 25 });
            AssertCommand(converted.PathData.Commands[4], SvgPathCommandKind.ArcTo, new double[] { 10, 5, 0, 0, 1, 100, 70 });
            AssertCommand(converted.PathData.Commands[6], SvgPathCommandKind.ArcTo, new double[] { 10, 5, 0, 0, 1, 10, 65 });
            AssertCommand(converted.PathData.Commands[8], SvgPathCommandKind.ArcTo, new double[] { 10, 5, 0, 0, 1, 20, 20 });
        });
    }

    [Test]
    public void CircleAndEllipseConversionUseTwoClosedHalfArcs()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><circle cx='10' cy='20' r='5'/><ellipse cx='3' cy='4' rx='2' ry='1'/></svg>");

        SvgShapePathResult circle = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[0]).ToPath();
        SvgShapePathResult ellipse = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[1]).ToPath();

        Assert.Multiple(() =>
        {
            AssertSuccess(circle, 4);
            AssertKinds(circle.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.ArcTo, SvgPathCommandKind.ArcTo, SvgPathCommandKind.ClosePath);
            AssertCommand(circle.PathData.Commands[0], SvgPathCommandKind.MoveTo, new double[] { 15, 20 });
            AssertCommand(circle.PathData.Commands[1], SvgPathCommandKind.ArcTo, new double[] { 5, 5, 0, 0, 1, 5, 20 });
            AssertCommand(circle.PathData.Commands[2], SvgPathCommandKind.ArcTo, new double[] { 5, 5, 0, 0, 1, 15, 20 });
            AssertSuccess(ellipse, 4);
            AssertCommand(ellipse.PathData.Commands[0], SvgPathCommandKind.MoveTo, new double[] { 5, 4 });
            AssertCommand(ellipse.PathData.Commands[1], SvgPathCommandKind.ArcTo, new double[] { 2, 1, 0, 0, 1, 1, 4 });
            AssertCommand(ellipse.PathData.Commands[2], SvgPathCommandKind.ArcTo, new double[] { 2, 1, 0, 0, 1, 5, 4 });
        });
    }

    [Test]
    public void PointListConversionPreservesOrderAndPolygonClosure()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><polyline points='1,2 3,4 5,6'/><polygon points='1,2 3,4 5,6'/><polyline points='7,8'/><polygon points='9,10'/><polyline/></svg>");

        SvgShapePathResult polyline = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[0]).ToPath();
        SvgShapePathResult polygon = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[1]).ToPath();
        SvgShapePathResult pointPolyline = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[2]).ToPath();
        SvgShapePathResult pointPolygon = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[3]).ToPath();
        SvgShapePathResult empty = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[4]).ToPath();

        Assert.Multiple(() =>
        {
            AssertSuccess(polyline, 3);
            AssertKinds(polyline.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.LineTo);
            AssertCommand(polyline.PathData.Commands[2], SvgPathCommandKind.LineTo, new double[] { 5, 6 });
            AssertSuccess(polygon, 4);
            AssertKinds(polygon.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.LineTo, SvgPathCommandKind.ClosePath);
            Assert.That(polygon.PathData.Commands[3].EndX, Is.EqualTo(1));
            Assert.That(polygon.PathData.Commands[3].EndY, Is.EqualTo(2));
            AssertSuccess(pointPolyline, 1);
            AssertKinds(pointPolyline.PathData, SvgPathCommandKind.MoveTo);
            AssertSuccess(pointPolygon, 2);
            AssertKinds(pointPolygon.PathData, SvgPathCommandKind.MoveTo, SvgPathCommandKind.ClosePath);
            AssertSuccess(empty, 0);
        });
    }

    [Test]
    public void ZeroAreaClosedShapesProduceValidEmptyPaths()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect width='0' height='10'/><circle r='0'/><ellipse rx='2' ry='0'/></svg>");

        SvgShapePathResult rectangle = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[0]).ToPath();
        SvgShapePathResult circle = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[1]).ToPath();
        SvgShapePathResult ellipse = new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[2]).ToPath();

        Assert.Multiple(() =>
        {
            AssertSuccess(rectangle, 0);
            AssertSuccess(circle, 0);
            AssertSuccess(ellipse, 0);
            Assert.That(rectangle.PathData.GetSubpathCount(), Is.Zero);
            Assert.That(circle.PathData.ToText(), Is.Empty);
            Assert.That(ellipse.PathData.IsValid, Is.True);
        });
    }

    [Test]
    public void ConversionClampsMutatedRectangleRadiiToUsedBounds()
    {
        SvgShapeResult rectangle = Resolve("<rect width='20' height='10'/>");
        rectangle.Rx = 100;
        rectangle.Ry = 100;

        SvgShapePathResult converted = rectangle.ToPath();

        Assert.Multiple(() =>
        {
            AssertSuccess(converted, 10);
            AssertCommand(converted.PathData.Commands[0], SvgPathCommandKind.MoveTo, new double[] { 10, 0 });
            AssertCommand(converted.PathData.Commands[2], SvgPathCommandKind.ArcTo, new double[] { 10, 5, 0, 0, 1, 20, 5 });
        });
    }

    [Test]
    public void ConversionReportsLimitsInvalidStateOverflowAndUnsupportedPaths()
    {
        SvgShapeResult line = Resolve("<line x1='1' y1='2' x2='3' y2='4'/>");
        SvgShapePathResult limited = line.ToPathWithLimit(1);
        line.X1 = double.NaN;
        SvgShapePathResult invalid = line.ToPath();
        SvgShapeResult circle = Resolve("<circle r='1'/>");
        circle.Cx = double.MaxValue;
        circle.R = double.MaxValue;
        SvgShapePathResult overflow = circle.ToPath();
        SvgShapeResult points = Resolve("<polyline points='1,2 3,4'/>");
        points.PointCount = 1;
        SvgShapePathResult mismatchedPoints = points.ToPath();
        SvgShapePathResult path = Resolve("<path d='M0 0L1 1'/>").ToPath();

        Assert.Multiple(() =>
        {
            AssertFailure(limited, SvgShapePathError.CommandLimitExceeded);
            AssertFailure(invalid, SvgShapePathError.InvalidArgument);
            AssertFailure(overflow, SvgShapePathError.NonFiniteResult);
            AssertFailure(mismatchedPoints, SvgShapePathError.InvalidArgument);
            AssertFailure(path, SvgShapePathError.UnsupportedShape);
            AssertFailure(new SvgShapePathConverter().ConvertWithShape(null), SvgShapePathError.InvalidArgument);
            AssertFailure(new SvgShapePathConverter().ConvertWithShapeAndMaximumCommands(points, 0), SvgShapePathError.InvalidArgument);
        });
    }

    static SvgShapeResult Resolve(string element)
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'>" + element + "</svg>");
        return new SvgShapeResolver().ResolveWithNode(document.DocumentElement.Children[0]);
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static void AssertSuccess(SvgShapePathResult result, int commandCount)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgShapePathError.None));
        Assert.That(result.PathData, Is.Not.Null);
        Assert.That(result.PathData.IsValid, Is.True);
        Assert.That(result.PathData.Commands, Has.Count.EqualTo(commandCount));
    }

    static void AssertFailure(SvgShapePathResult result, SvgShapePathError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.PathData, Is.Null);
    }

    static void AssertKinds(SvgPathData path, params SvgPathCommandKind[] kinds)
    {
        Assert.That(path.Commands.Select(command => command.Kind), Is.EqualTo(kinds));
    }

    static void AssertCommand(SvgPathCommand command, SvgPathCommandKind kind, double[] values)
    {
        Assert.That(command.Kind, Is.EqualTo(kind));
        Assert.That(command.Relative, Is.False);
        Assert.That(command.Explicit, Is.True);
        Assert.That(command.Values, Is.EqualTo(values));
        Assert.That(command.IsValid, Is.True);
    }
}
