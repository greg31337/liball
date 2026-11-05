namespace chart.Tests;

[TestFixture]
public class ChartFoundationalPublicBehaviorTests
{
    [Test]
    public void GeometryAccessorsAndCopies_MutateIndependentlyAtBoundaries()
    {
        ChartPoint point = new ChartPoint(1.0, 2.0);
        ChartPoint pointCopy = point.Copy();
        point.SetX(-3.0);
        point.SetY(4.0);
        AssertPoint(point, -3.0, 4.0);
        AssertPoint(pointCopy, 1.0, 2.0);
        Assert.That(point.DistanceTo(point.Copy()), Is.Zero);

        ChartRect rect = new ChartRect(1.0, 2.0, 3.0, 4.0);
        rect.SetX(-1.0);
        rect.SetY(-2.0);
        rect.SetWidth(0.0);
        rect.SetHeight(-1.0);
        Assert.That(rect.GetX(), Is.EqualTo(-1.0));
        Assert.That(rect.GetY(), Is.EqualTo(-2.0));
        Assert.That(rect.GetWidth(), Is.Zero);
        Assert.That(rect.GetHeight(), Is.EqualTo(-1.0));
        Assert.That(rect.IsEmpty(), Is.True);

        ChartRange range = new ChartRange(2.0, 2.0);
        Assert.That(range.Normalize(100.0), Is.Zero);
        range.SetMinimum(-5.0);
        range.SetMaximum(5.0);
        Assert.That(range.GetMinimum(), Is.EqualTo(-5.0));
        Assert.That(range.GetMaximum(), Is.EqualTo(5.0));
        Assert.That(range.Contains(-5.0), Is.True);
        Assert.That(range.Contains(5.0), Is.True);

        ChartPadding padding = new ChartPadding(0.0, 0.0, 0.0, 0.0);
        padding.SetLeft(1.0);
        padding.SetTop(2.0);
        padding.SetRight(3.0);
        padding.SetBottom(4.0);
        Assert.That(padding.GetLeft(), Is.EqualTo(1.0));
        Assert.That(padding.GetTop(), Is.EqualTo(2.0));
        Assert.That(padding.GetRight(), Is.EqualTo(3.0));
        Assert.That(padding.GetBottom(), Is.EqualTo(4.0));
        Assert.That(padding.Horizontal(), Is.EqualTo(4.0));
        Assert.That(padding.Vertical(), Is.EqualTo(6.0));
    }

    [Test]
    public void MatrixAccessors_MutateEveryCoefficientAndPreserveInvertBoundary()
    {
        ChartMatrix matrix = new ChartMatrix(0.0, 0.0, 0.0, 0.0, 0.0, 0.0);
        matrix.SetM11(1.0);
        matrix.SetM12(2.0);
        matrix.SetM21(3.0);
        matrix.SetM22(4.0);
        matrix.SetOffsetX(5.0);
        matrix.SetOffsetY(6.0);

        Assert.That(matrix.GetM11(), Is.EqualTo(1.0));
        Assert.That(matrix.GetM12(), Is.EqualTo(2.0));
        Assert.That(matrix.GetM21(), Is.EqualTo(3.0));
        Assert.That(matrix.GetM22(), Is.EqualTo(4.0));
        Assert.That(matrix.GetOffsetX(), Is.EqualTo(5.0));
        Assert.That(matrix.GetOffsetY(), Is.EqualTo(6.0));
        AssertPoint(matrix.TransformPoint(new ChartPoint(1.0, 1.0)), 9.0, 12.0);
        Assert.That(new ChartMatrix(1.0, 2.0, 2.0, 4.0, 0.0, 0.0).Invert(), Is.Null);
    }

    [Test]
    public void ColorAccessorsAndCopy_CoverAllChannelsAndOpacityBoundaries()
    {
        ChartColor color = new ChartColor(1, 2, 3, 4);
        ChartColor copy = color.Copy();
        color.SetR(10);
        color.SetG(20);
        color.SetB(30);
        color.SetA(40);

        AssertColor(color, 10, 20, 30, 40);
        AssertColor(copy, 1, 2, 3, 4);
        Assert.That(color.EqualsColor(copy), Is.False);
        Assert.That(copy.EqualsColor(new ChartColor(1, 2, 3, 4)), Is.True);
        AssertColor(color.WithOpacity(0.0), 10, 20, 30, 0);
        AssertColor(color.WithOpacity(1.0), 10, 20, 30, 255);
    }

    [Test]
    public void PaletteColorMapAndThemeMutations_ExposeAndCopyConfiguredState()
    {
        ChartPalette palette = new ChartPalette("original", new ChartColor[] { ChartColor.Black(), ChartColor.White() });
        palette.SetName("changed");
        palette.SetColors(new ChartColor[] { new ChartColor(10, 20, 30, 40) });
        Assert.That(palette.GetName(), Is.EqualTo("changed"));
        Assert.That(palette.GetColors().Length, Is.EqualTo(1));
        AssertColor(palette.GetColor(-1), 10, 20, 30, 40);

        ChartColorMap map = new ChartColorMap("original", new ChartColor[] { ChartColor.Black(), ChartColor.White() });
        map.SetName("changed");
        map.SetStops(new ChartColor[] { new ChartColor(20, 30, 40, 50) });
        Assert.That(map.GetName(), Is.EqualTo("changed"));
        Assert.That(map.GetStops().Length, Is.EqualTo(1));
        AssertColor(map.GetColor(0.5), 20, 30, 40, 50);
        Assert.Throws<System.IndexOutOfRangeException>(() => map.GetStop(1));

        ChartTheme theme = new ChartTheme("theme");
        ChartStyle style = new ChartStyle();
        style.SetOpacity(0.5);
        theme.SetName("changed-theme");
        theme.SetStyle(style);
        theme.SetPalette(palette);
        ChartTheme themeCopy = theme.Copy();
        style.SetOpacity(0.25);
        palette.GetColors()[0].SetR(99);

        Assert.That(theme.GetName(), Is.EqualTo("changed-theme"));
        Assert.That(theme.GetStyle().GetOpacity(), Is.EqualTo(0.25));
        Assert.That(themeCopy.GetStyle().GetOpacity(), Is.EqualTo(0.5));
        AssertColor(theme.SeriesColor(0), 10, 20, 30, 40);
        AssertColor(themeCopy.SeriesColor(0), 10, 20, 30, 40);

        Assert.Throws<System.ArgumentNullException>(() => new ChartPalette(null!, new ChartColor[] { ChartColor.Black() }));
        Assert.Throws<System.ArgumentNullException>(() => new ChartPalette("palette", null!));
        Assert.Throws<System.ArgumentException>(() => new ChartPalette("palette", new ChartColor[0]));
        Assert.Throws<System.ArgumentException>(() => new ChartPalette("palette", new ChartColor[] { null! }));
        Assert.Throws<System.ArgumentNullException>(() => new ChartColorMap(null!, new ChartColor[] { ChartColor.Black() }));
        Assert.Throws<System.ArgumentNullException>(() => new ChartColorMap("map", null!));
        Assert.Throws<System.ArgumentException>(() => new ChartColorMap("map", new ChartColor[0]));
    }

    [Test]
    public void PathCommandMutationAndFlatten_UseCurrentCommandListAndValidateTolerance()
    {
        ChartPath path = new ChartPath();
        System.Collections.Generic.List<ChartPathCommand> commands = new System.Collections.Generic.List<ChartPathCommand>();
        ChartPathCommandFactory factory = new ChartPathCommandFactory();
        commands.Add(factory.MoveTo(1.0, 2.0));
        commands.Add(factory.LineTo(3.0, 4.0));
        path.SetCommands(commands);

        Assert.That(path.GetCommands(), Is.SameAs(commands));
        Assert.That(path.Flatten(0.5).Length, Is.EqualTo(1));
        commands.Add(factory.Close());
        Assert.That(path.Flatten(0.5)[0].Closed, Is.True);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => path.Flatten(0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => path.Flatten(double.NaN));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => path.Flatten(double.PositiveInfinity));
    }

    [Test]
    public void RawBitmapCopyAndMutableViews_AreIndependentOrAliasedAsDocumented()
    {
        RawBitmap bitmap = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);
        bitmap.SetPixel(0, 0, new ChartColor(1, 2, 3, 4));
        RawBitmap copy = bitmap.Copy();
        bitmap.GetBufferSpan()[0] = 99;

        Assert.That(bitmap.GetReadOnlyBufferSpan()[0], Is.EqualTo(99));
        Assert.That(copy.GetReadOnlyBufferSpan()[0], Is.EqualTo(1));
        Assert.That(bitmap.GetRowSpan(0).Length, Is.EqualTo(bitmap.GetStride()));
        Assert.That(bitmap.GetPixelRowSpan(0).Length, Is.EqualTo(8));
        Assert.That(bitmap.GetReadOnlyRowSpan(0).Length, Is.EqualTo(bitmap.GetStride()));
        Assert.That(bitmap.GetReadOnlyPixelRowSpan(0).Length, Is.EqualTo(8));

        bitmap.SetWidth(1);
        bitmap.SetHeight(1);
        bitmap.SetStride(4);
        bitmap.SetPixelFormat(ChartPixelFormat.Bgra32);
        bitmap.SetBuffer(new byte[] { 30, 20, 10, 40 });
        Assert.That(bitmap.GetWidth(), Is.EqualTo(1));
        Assert.That(bitmap.GetHeight(), Is.EqualTo(1));
        Assert.That(bitmap.GetStride(), Is.EqualTo(4));
        Assert.That(bitmap.GetPixelFormat(), Is.EqualTo(ChartPixelFormat.Bgra32));
        Assert.That(bitmap.GetBuffer(), Is.EqualTo(new byte[] { 30, 20, 10, 40 }));
        AssertColor(bitmap.GetPixel(0, 0), 10, 20, 30, 40);
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.GetX(), Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.GetY(), Is.EqualTo(y).Within(0.0000001));
    }

    static void AssertColor(ChartColor color, int red, int green, int blue, int alpha)
    {
        Assert.That(color.GetR(), Is.EqualTo((byte)red));
        Assert.That(color.GetG(), Is.EqualTo((byte)green));
        Assert.That(color.GetB(), Is.EqualTo((byte)blue));
        Assert.That(color.GetA(), Is.EqualTo((byte)alpha));
    }
}
