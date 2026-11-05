namespace chart.Tests;

[TestFixture]
public class ChartRasterPathTests
{
    [Test]
    public void ChartPath_StoresCommandsInOrder()
    {
        ChartPath path = new ChartPath();

        path.MoveTo(1.0, 2.0);
        path.LineTo(3.0, 4.0);
        path.QuadraticBezierTo(5.0, 6.0, 7.0, 8.0);
        path.CubicBezierTo(9.0, 10.0, 11.0, 12.0, 13.0, 14.0);
        path.Close();

        Assert.That(path.Commands.Count, Is.EqualTo(5));
        Assert.That(path.Commands[0].Kind, Is.EqualTo(ChartPathCommandKind.MoveTo));
        Assert.That(path.Commands[1].Kind, Is.EqualTo(ChartPathCommandKind.LineTo));
        Assert.That(path.Commands[2].Kind, Is.EqualTo(ChartPathCommandKind.QuadraticBezierTo));
        Assert.That(path.Commands[3].Kind, Is.EqualTo(ChartPathCommandKind.CubicBezierTo));
        Assert.That(path.Commands[4].Kind, Is.EqualTo(ChartPathCommandKind.Close));
    }

    [Test]
    public void Flatten_ConvertsQuadraticBezierToPolyline()
    {
        ChartPath path = new ChartPath();

        path.MoveTo(0.0, 0.0);
        path.QuadraticBezierTo(5.0, 10.0, 10.0, 0.0);

        ChartPathPolyline[] coarse = path.Flatten(5.0);
        ChartPathPolyline[] fine = path.Flatten(0.25);

        Assert.That(coarse.Length, Is.EqualTo(1));
        Assert.That(fine.Length, Is.EqualTo(1));
        Assert.That(fine[0].Closed, Is.False);
        Assert.That(fine[0].Points.Length, Is.GreaterThan(coarse[0].Points.Length));
        AssertPoint(fine[0].Points[0], 0.0, 0.0);
        AssertPoint(fine[0].Points[fine[0].Points.Length - 1], 10.0, 0.0);
        Assert.That(MaximumY(fine[0].Points), Is.GreaterThan(4.0));
    }

    [Test]
    public void Flatten_ConvertsCubicBezierToPolyline()
    {
        ChartPath path = new ChartPath();

        path.MoveTo(0.0, 0.0);
        path.CubicBezierTo(0.0, 10.0, 10.0, 10.0, 10.0, 0.0);

        ChartPathPolyline[] polylines = path.Flatten(0.25);

        Assert.That(polylines.Length, Is.EqualTo(1));
        Assert.That(polylines[0].Points.Length, Is.GreaterThan(4));
        AssertPoint(polylines[0].Points[0], 0.0, 0.0);
        AssertPoint(polylines[0].Points[polylines[0].Points.Length - 1], 10.0, 0.0);
        Assert.That(MaximumY(polylines[0].Points), Is.GreaterThan(7.0));
    }

    [Test]
    public void Flatten_CloseCreatesClosedPolylineAndRepeatsStartPoint()
    {
        ChartPath path = new ChartPath();

        path.MoveTo(1.0, 1.0);
        path.LineTo(4.0, 1.0);
        path.LineTo(4.0, 4.0);
        path.Close();

        ChartPathPolyline[] polylines = path.Flatten(0.25);

        Assert.That(polylines.Length, Is.EqualTo(1));
        Assert.That(polylines[0].Closed, Is.True);
        Assert.That(polylines[0].Points.Length, Is.EqualTo(4));
        AssertPoint(polylines[0].Points[0], 1.0, 1.0);
        AssertPoint(polylines[0].Points[3], 1.0, 1.0);
    }

    [Test]
    public void FillPath_RendersClosedPath()
    {
        RawBitmap bitmap = new RawBitmap(7, 7, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPath path = new ChartPath();

        path.MoveTo(1.0, 1.0);
        path.LineTo(6.0, 1.0);
        path.LineTo(6.0, 6.0);
        path.LineTo(1.0, 6.0);
        path.Close();

        canvas.FillPathWithTolerance(path, ChartColor.White(), ChartFillRule.Nonzero, 0.25, ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(3, 3), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(0, 0), 0, 0, 0, 0);
    }

    [Test]
    public void FillPath_IgnoresOpenSubpaths()
    {
        RawBitmap bitmap = new RawBitmap(7, 7, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPath path = new ChartPath();

        path.MoveTo(1.0, 1.0);
        path.LineTo(6.0, 1.0);
        path.LineTo(6.0, 6.0);

        canvas.FillPathWithTolerance(path, ChartColor.White(), ChartFillRule.Nonzero, 0.25, ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(3, 3), 0, 0, 0, 0);
    }

    [Test]
    public void StrokePath_RendersOpenBezierPath()
    {
        RawBitmap bitmap = new RawBitmap(9, 7, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPath path = new ChartPath();

        path.MoveTo(1.5, 5.5);
        path.QuadraticBezierTo(4.5, 0.5, 7.5, 5.5);

        canvas.StrokePathWithTolerance(path, 1.5, ChartColor.White(), ChartLineCap.Round, ChartLineJoin.Round, 0.25, ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(1, 5), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(4, 2), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(7, 5), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(4, 6), 0, 0, 0, 0);
    }

    [Test]
    public void StrokePath_RendersClosedPathWithoutEndpointCaps()
    {
        RawBitmap bitmap = new RawBitmap(7, 7, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPath path = new ChartPath();

        path.MoveTo(1.5, 1.5);
        path.LineTo(5.5, 1.5);
        path.LineTo(5.5, 5.5);
        path.LineTo(1.5, 5.5);
        path.Close();

        canvas.StrokePathWithTolerance(path, 1.0, ChartColor.White(), ChartLineCap.Round, ChartLineJoin.Miter, 0.25, ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(1, 1), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(5, 5), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(3, 3), 0, 0, 0, 0);
    }

    [Test]
    public void PathRendering_ValidatesInputs()
    {
        RawBitmap bitmap = new RawBitmap(4, 4, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPath path = new ChartPath();

        path.MoveTo(0.0, 0.0);
        path.LineTo(1.0, 1.0);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => path.Flatten(0.0));
        Assert.Throws<System.ArgumentNullException>(() => canvas.FillPathWithTolerance(null!, ChartColor.White(), ChartFillRule.Nonzero, 0.25, ChartBlendMode.Source));
        Assert.Throws<System.ArgumentNullException>(() => canvas.FillPathWithTolerance(path, null!, ChartFillRule.Nonzero, 0.25, ChartBlendMode.Source));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => canvas.StrokePathWithTolerance(path, 1.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Bevel, 0.0, ChartBlendMode.Source));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => canvas.StrokePathWithTolerance(path, 0.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Bevel, 0.25, ChartBlendMode.Source));
    }

    static double MaximumY(ChartPoint[] points)
    {
        double value = points[0].Y;
        int index = 1;
        while (index < points.Length)
        {
            value = System.Math.Max(value, points[index].Y);
            index = index + 1;
        }

        return value;
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.Y, Is.EqualTo(y).Within(0.0000001));
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo((byte)r));
        Assert.That(color.G, Is.EqualTo((byte)g));
        Assert.That(color.B, Is.EqualTo((byte)b));
        Assert.That(color.A, Is.EqualTo((byte)a));
    }
}
