namespace chart.Tests;

[TestFixture]
public class ChartConfigurationTests
{
    [Test]
    public void Configuration_HasCoherentDefaultsAndIndependentCopies()
    {
        ChartConfiguration configuration = new ChartConfiguration();
        IChartConfiguration contract = configuration;

        Assert.That(contract.GetCoordinateUnit(), Is.EqualTo(ChartCoordinateUnit.Data));
        Assert.That(contract.GetDirection(), Is.EqualTo(ChartDirection.Normal));
        Assert.That(contract.GetMissingValuePolicy(), Is.EqualTo(ChartMissingValuePolicy.Gap));
        Assert.That(contract.GetOutputFormat(), Is.EqualTo(ChartOutputFormat.Png));
        Assert.That(contract.GetRenderBackend(), Is.EqualTo(ChartRenderBackend.Raster));
        Assert.That(contract.GetScaleKind(), Is.EqualTo(ChartScaleKind.Linear));
        Assert.That(contract.GetSeriesKind(), Is.EqualTo(ChartSeriesKind.Line));
        Assert.That(contract.GetSortKind(), Is.EqualTo(ChartSortKind.None));
        Assert.That(contract.GetTextAlignment(), Is.EqualTo(ChartTextAlignment.Left));
        Assert.That(contract.GetVerticalTextAlignment(), Is.EqualTo(ChartVerticalTextAlignment.Baseline));

        ChartConfiguration copy = configuration.Copy();
        copy.SetDirection(ChartDirection.Reversed);

        Assert.That(configuration.GetDirection(), Is.EqualTo(ChartDirection.Normal));
        Assert.That(copy.GetDirection(), Is.EqualTo(ChartDirection.Reversed));
    }

    [Test]
    public void Configuration_ValidatesEveryEnumSetting()
    {
        ChartConfiguration configuration = new ChartConfiguration();

        Assert.Throws<System.ArgumentOutOfRangeException>(() => configuration.SetCoordinateUnit((ChartCoordinateUnit)99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => configuration.SetDirection((ChartDirection)99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => configuration.SetMissingValuePolicy((ChartMissingValuePolicy)99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => configuration.SetOutputFormat((ChartOutputFormat)99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => configuration.SetRenderBackend((ChartRenderBackend)99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => configuration.SetScaleKind((ChartScaleKind)99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => configuration.SetSeriesKind((ChartSeriesKind)99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => configuration.SetSortKind((ChartSortKind)99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => configuration.SetTextAlignment((ChartTextAlignment)99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => configuration.SetVerticalTextAlignment((ChartVerticalTextAlignment)99));
    }

    [Test]
    public void Configuration_MapsLinearLogAndReversedScales()
    {
        ChartConfiguration configuration = new ChartConfiguration();

        Assert.That(configuration.MapNumericValue(5.0, 0.0, 10.0, 0.0, 100.0), Is.EqualTo(50.0));

        configuration.SetDirection(ChartDirection.Reversed);
        Assert.That(configuration.MapNumericValue(2.0, 0.0, 10.0, 0.0, 100.0), Is.EqualTo(80.0));

        configuration.SetDirection(ChartDirection.Normal);
        configuration.SetScaleKind(ChartScaleKind.Log);
        Assert.That(configuration.MapNumericValue(10.0, 1.0, 100.0, 0.0, 1.0), Is.EqualTo(0.5).Within(0.0000001));

        configuration.SetScaleKind(ChartScaleKind.CategoryBand);
        Assert.Throws<System.InvalidOperationException>(() => configuration.MapNumericValue(1.0, 0.0, 2.0, 0.0, 10.0));
    }

    [Test]
    public void Configuration_MapsCoordinateUnitsAndTextAlignment()
    {
        ChartConfiguration configuration = new ChartConfiguration();
        ChartRect bounds = new ChartRect(10.0, 20.0, 100.0, 200.0);
        ChartRange domain = new ChartRange(0.0, 10.0);

        ChartPoint data = configuration.MapPoint(new ChartPoint(5.0, 5.0), bounds, domain, domain);
        AssertPoint(data, 60.0, 120.0);

        configuration.SetCoordinateUnit(ChartCoordinateUnit.Plot);
        AssertPoint(configuration.MapPoint(new ChartPoint(3.0, 4.0), bounds, domain, domain), 13.0, 24.0);

        configuration.SetCoordinateUnit(ChartCoordinateUnit.Fraction);
        AssertPoint(configuration.MapPoint(new ChartPoint(0.25, 0.75), bounds, domain, domain), 35.0, 170.0);

        configuration.SetCoordinateUnit(ChartCoordinateUnit.Pixel);
        AssertPoint(configuration.MapPoint(new ChartPoint(3.0, 4.0), bounds, domain, domain), 3.0, 4.0);

        configuration.SetTextAlignment(ChartTextAlignment.Center);
        configuration.SetVerticalTextAlignment(ChartVerticalTextAlignment.Middle);
        AssertPoint(configuration.AlignText(new ChartPoint(100.0, 50.0), new ChartSize(20.0, 10.0)), 90.0, 45.0);
    }

    [Test]
    public void Configuration_ResolvesEveryMissingValuePolicy()
    {
        ChartConfiguration configuration = new ChartConfiguration();
        double[] values = new double[] { 1.0, double.NaN, double.PositiveInfinity, 4.0 };

        double[] gap = configuration.ResolveMissingValues(values);
        Assert.That(double.IsNaN(gap[1]), Is.True);
        Assert.That(double.IsPositiveInfinity(gap[2]), Is.True);

        configuration.SetMissingValuePolicy(ChartMissingValuePolicy.Skip);
        Assert.That(configuration.ResolveMissingValues(values), Is.EqualTo(new double[] { 1.0, 4.0 }));

        configuration.SetMissingValuePolicy(ChartMissingValuePolicy.Zero);
        Assert.That(configuration.ResolveMissingValues(values), Is.EqualTo(new double[] { 1.0, 0.0, 0.0, 4.0 }));

        configuration.SetMissingValuePolicy(ChartMissingValuePolicy.Interpolate);
        Assert.That(configuration.ResolveMissingValues(values), Is.EqualTo(new double[] { 1.0, 2.0, 3.0, 4.0 }));

        configuration.SetMissingValuePolicy(ChartMissingValuePolicy.CarryForward);
        Assert.That(configuration.ResolveMissingValues(new double[] { double.NaN, 2.0, double.NaN, 4.0 }), Is.EqualTo(new double[] { 0.0, 2.0, 2.0, 4.0 }));
        Assert.That(values[1], Is.NaN);
    }

    [Test]
    public void Configuration_SortsNumericTextAndUnsortedColumns()
    {
        ChartConfiguration configuration = new ChartConfiguration();
        ChartDataColumnFactory factory = new ChartDataColumnFactory();
        ChartDataColumn numbers = factory.CreateNumber("value", new double[] { 3.0, 1.0, 2.0 });
        ChartDataColumn categories = factory.CreateCategory("category", new string?[] { "b", "c", "a" });

        Assert.That(configuration.SortIndices(numbers, true), Is.EqualTo(new int[] { 0, 1, 2 }));

        configuration.SetSortKind(ChartSortKind.Value);
        Assert.That(configuration.SortIndices(numbers, true), Is.EqualTo(new int[] { 1, 2, 0 }));

        configuration.SetSortKind(ChartSortKind.Category);
        Assert.That(configuration.SortIndices(categories, false), Is.EqualTo(new int[] { 1, 0, 2 }));

        configuration.SetSortKind(ChartSortKind.Custom);
        Assert.Throws<System.InvalidOperationException>(() => configuration.SortIndices(numbers, true));
    }

    [Test]
    public void Configuration_CreatesSupportedSeriesKinds()
    {
        ChartConfiguration configuration = new ChartConfiguration();
        ChartRenderStyle lineStyle = new ChartRenderStyle();
        lineStyle.SetStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle fillStyle = new ChartRenderStyle();
        fillStyle.SetFill(ChartColor.Black());
        double[] x = new double[] { 0.0, 1.0, 2.0 };
        double[] y = new double[] { 1.0, 2.0, 3.0 };

        Assert.That(configuration.CreateCartesianSeries("line", x, y, 0.0, 4.0, lineStyle).Kind, Is.EqualTo(ChartCartesianSeriesKind.Line));

        configuration.SetSeriesKind(ChartSeriesKind.StepLine);
        Assert.That(configuration.CreateCartesianSeries("step", x, y, 0.0, 4.0, lineStyle).CommandAt(0).Points.Length, Is.EqualTo(7));

        configuration.SetSeriesKind(ChartSeriesKind.SplineLine);
        Assert.That(configuration.CreateCartesianSeries("spline", x, y, 0.0, 4.0, lineStyle).CommandAt(0).Points.Length, Is.GreaterThan(3));

        configuration.SetSeriesKind(ChartSeriesKind.Area);
        Assert.That(configuration.CreateCartesianSeries("area", x, y, 0.0, 4.0, fillStyle).Kind, Is.EqualTo(ChartCartesianSeriesKind.Area));

        configuration.SetSeriesKind(ChartSeriesKind.Scatter);
        Assert.That(configuration.CreateCartesianSeries("scatter", x, y, 0.0, 4.0, fillStyle).Kind, Is.EqualTo(ChartCartesianSeriesKind.Scatter));

        configuration.SetSeriesKind(ChartSeriesKind.Bar);
        Assert.That(configuration.CreateCartesianSeries("bar", x, y, 0.0, 0.5, fillStyle).Kind, Is.EqualTo(ChartCartesianSeriesKind.Bar));

        configuration.SetSeriesKind(ChartSeriesKind.Pie);
        Assert.Throws<System.InvalidOperationException>(() => configuration.CreateCartesianSeries("pie", x, y, 0.0, 1.0, fillStyle));
    }

    [Test]
    public void Configuration_ExportsEveryOutputAndHonorsBackend()
    {
        ChartConfiguration configuration = new ChartConfiguration();
        ChartRenderScene scene = new ChartRenderScene(2, 2);
        ChartAccessibilityMetadata accessibility = new ChartAccessibilityMetadata();
        scene.AddClear(ChartColor.White());

        byte[] png = configuration.ExportScene(scene, accessibility);
        Assert.That(new byte[] { png[0], png[1], png[2], png[3] }, Is.EqualTo(new byte[] { 137, 80, 78, 71 }));

        configuration.SetOutputFormat(ChartOutputFormat.Bmp);
        byte[] bmp = configuration.ExportScene(scene, accessibility);
        Assert.That(new byte[] { bmp[0], bmp[1] }, Is.EqualTo(new byte[] { 66, 77 }));

        configuration.SetOutputFormat(ChartOutputFormat.RawBitmap);
        Assert.That(configuration.ExportScene(scene, accessibility).Length, Is.EqualTo(16));

        configuration.SetRenderBackend(ChartRenderBackend.Svg);
        configuration.SetOutputFormat(ChartOutputFormat.Svg);
        string svg = System.Text.Encoding.UTF8.GetString(configuration.ExportScene(scene, accessibility));
        Assert.That(svg, Does.StartWith("<svg"));

        configuration.SetOutputFormat(ChartOutputFormat.Png);
        Assert.Throws<System.InvalidOperationException>(() => configuration.ExportScene(scene, accessibility));
    }

    [Test]
    public void Document_CopiesConfigurationAndUsesItForExport()
    {
        ChartDocument document = new ChartDocument(2.0, 2.0);
        ChartConfiguration configuration = new ChartConfiguration();
        configuration.SetOutputFormat(ChartOutputFormat.Bmp);
        document.SetConfiguration(configuration);
        configuration.SetOutputFormat(ChartOutputFormat.Png);

        byte[] output = document.ExportConfiguredScene(document.CreateScene());
        ChartConfiguration returned = document.GetConfiguration();
        returned.SetOutputFormat(ChartOutputFormat.RawBitmap);
        ChartDocument copy = document.Copy();

        Assert.That(new byte[] { output[0], output[1] }, Is.EqualTo(new byte[] { 66, 77 }));
        Assert.That(document.GetConfiguration().GetOutputFormat(), Is.EqualTo(ChartOutputFormat.Bmp));
        Assert.That(copy.GetConfiguration().GetOutputFormat(), Is.EqualTo(ChartOutputFormat.Bmp));
        Assert.Throws<System.ArgumentNullException>(() => document.SetConfiguration(null!));
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.Y, Is.EqualTo(y).Within(0.0000001));
    }
}
