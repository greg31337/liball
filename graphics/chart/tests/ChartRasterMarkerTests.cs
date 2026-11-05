namespace chart.Tests;

[TestFixture]
public class ChartRasterMarkerTests
{
    [Test]
    public void DrawMarker_RendersFilledCircleSquareDiamondAndTriangle()
    {
        RawBitmap circle = RenderMarker(ChartMarkerShape.Circle);
        RawBitmap square = RenderMarker(ChartMarkerShape.Square);
        RawBitmap diamond = RenderMarker(ChartMarkerShape.Diamond);
        RawBitmap triangle = RenderMarker(ChartMarkerShape.Triangle);

        AssertColor(circle.GetPixel(5, 5), 255, 255, 255, 255);
        AssertColor(circle.GetPixel(3, 3), 0, 0, 0, 0);
        AssertColor(square.GetPixel(3, 3), 255, 255, 255, 255);
        AssertColor(diamond.GetPixel(5, 4), 255, 255, 255, 255);
        AssertColor(diamond.GetPixel(3, 3), 0, 0, 0, 0);
        AssertColor(triangle.GetPixel(5, 4), 255, 255, 255, 255);
        AssertColor(triangle.GetPixel(3, 4), 0, 0, 0, 0);
    }

    [Test]
    public void DrawMarker_RendersCrossPlusAndLineStrokes()
    {
        RawBitmap cross = RenderMarker(ChartMarkerShape.Cross);
        RawBitmap plus = RenderMarker(ChartMarkerShape.Plus);
        RawBitmap line = RenderMarker(ChartMarkerShape.Line);

        AssertColor(cross.GetPixel(3, 3), 255, 255, 255, 255);
        AssertColor(cross.GetPixel(7, 3), 255, 255, 255, 255);
        AssertColor(cross.GetPixel(5, 5), 255, 255, 255, 255);
        AssertColor(plus.GetPixel(5, 3), 255, 255, 255, 255);
        AssertColor(plus.GetPixel(3, 5), 255, 255, 255, 255);
        AssertColor(plus.GetPixel(3, 3), 0, 0, 0, 0);
        AssertColor(line.GetPixel(3, 5), 255, 255, 255, 255);
        AssertColor(line.GetPixel(5, 3), 0, 0, 0, 0);
    }

    [Test]
    public void DrawMarker_RendersStar()
    {
        RawBitmap bitmap = RenderMarker(ChartMarkerShape.Star);

        AssertColor(bitmap.GetPixel(5, 4), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(5, 5), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(3, 7), 0, 0, 0, 0);
        Assert.That(ChartRasterCanvas.CreateStarPoints(5.5, 5.5, 2.0, 1.0).Length, Is.EqualTo(10));
    }

    [Test]
    public void DrawCustomMarker_FillsTransformedPath()
    {
        RawBitmap bitmap = new RawBitmap(11, 11, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPath path = CreateUnitDiamondPath();

        canvas.DrawCustomMarker(5.5, 5.5, 4.0, path, ChartColor.White(), ChartFillRule.Nonzero, ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(5, 4), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(5, 5), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(3, 3), 0, 0, 0, 0);
    }

    [Test]
    public void StrokeCustomMarker_StrokesTransformedPath()
    {
        RawBitmap bitmap = new RawBitmap(11, 11, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPath path = CreateUnitDiamondPath();

        canvas.StrokeCustomMarker(5.5, 5.5, 4.0, path, 1.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Miter, ChartBlendMode.Source);

        AssertColor(bitmap.GetPixel(5, 3), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(7, 5), 255, 255, 255, 255);
        AssertColor(bitmap.GetPixel(5, 5), 0, 0, 0, 0);
    }

    [Test]
    public void TransformMarkerPath_ScalesMarkerLocalCoordinatesAroundCenter()
    {
        ChartPath path = new ChartPath();
        path.MoveTo(-0.5, -0.5);
        path.LineTo(0.5, 0.5);

        ChartPath transformed = ChartRasterCanvas.TransformMarkerPath(path, 10.0, 20.0, 4.0);
        ChartPathPolyline[] polylines = transformed.Flatten(0.25);

        AssertPoint(polylines[0].Points[0], 8.0, 18.0);
        AssertPoint(polylines[0].Points[1], 12.0, 22.0);
    }

    [Test]
    public void DrawMarker_ValidatesInputs()
    {
        RawBitmap bitmap = new RawBitmap(5, 5, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        ChartPath path = CreateUnitDiamondPath();

        Assert.Throws<System.ArgumentOutOfRangeException>(() => canvas.DrawMarker(2.0, 2.0, 0.0, ChartMarkerShape.Circle, ChartColor.White(), ChartBlendMode.Source));
        Assert.Throws<System.ArgumentNullException>(() => canvas.DrawMarker(2.0, 2.0, 2.0, ChartMarkerShape.Circle, null!, ChartBlendMode.Source));
        Assert.Throws<System.ArgumentException>(() => canvas.DrawMarker(2.0, 2.0, 2.0, ChartMarkerShape.CustomPath, ChartColor.White(), ChartBlendMode.Source));
        Assert.Throws<System.ArgumentNullException>(() => canvas.DrawCustomMarker(2.0, 2.0, 2.0, null!, ChartColor.White(), ChartFillRule.Nonzero, ChartBlendMode.Source));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => canvas.StrokeCustomMarker(2.0, 2.0, 2.0, path, 0.0, ChartColor.White(), ChartLineCap.Butt, ChartLineJoin.Bevel, ChartBlendMode.Source));
    }

    static RawBitmap RenderMarker(ChartMarkerShape shape)
    {
        RawBitmap bitmap = new RawBitmap(11, 11, ChartPixelFormat.Rgba32);
        ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);
        canvas.DrawMarker(5.5, 5.5, 4.0, shape, ChartColor.White(), ChartBlendMode.Source);
        return bitmap;
    }

    static ChartPath CreateUnitDiamondPath()
    {
        ChartPath path = new ChartPath();
        path.MoveTo(0.0, -0.5);
        path.LineTo(0.5, 0.0);
        path.LineTo(0.0, 0.5);
        path.LineTo(-0.5, 0.0);
        path.Close();
        return path;
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
