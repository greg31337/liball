namespace chart.Tests;

[TestFixture]
public class ChartPolarChartsTests
{
    [Test]
    public void PolarTransform_MapsAnglesAndRadii()
    {
        ChartPolarTransform transform = new ChartPolarTransform(50.0, 50.0, 10.0, 30.0, 0.0, 100.0, 0.0, true);
        ChartPoint point = transform.PointAt(90.0, 50.0);

        Assert.That(transform.MapRadius(50.0), Is.EqualTo(20.0).Within(0.0000001));
        Assert.That(transform.MapAngle(90.0), Is.EqualTo(90.0).Within(0.0000001));
        AssertPoint(point, 70.0, 50.0);

        transform.SetClamp(true);
        Assert.That(transform.MapRadius(-10.0), Is.EqualTo(10.0).Within(0.0000001));
        Assert.That(transform.MapRadius(120.0), Is.EqualTo(30.0).Within(0.0000001));
    }

    [Test]
    public void PolarLineScatterAndBars_AddRetainedCommands()
    {
        ChartRenderScene scene = new ChartRenderScene(100, 100);
        ChartPolarTransform transform = new ChartPolarTransform(50.0, 50.0, 0.0, 40.0, 0.0, 10.0, 0.0, true);
        ChartRenderStyle line = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(200, 0, 0));

        ChartPolarChartsOperations.AddPolarLine(scene, "polar", new double[] { 0.0, 90.0 }, new double[] { 10.0, 10.0 }, transform, true, line);
        ChartPolarChartsOperations.AddPolarScatter(scene, "polar", new double[] { 180.0 }, new double[] { 5.0 }, transform, ChartMarkerShape.Circle, 6.0, fill);
        ChartPolarChartsOperations.AddPolarBars(scene, "polar", new double[] { 90.0 }, new double[] { 10.0 }, transform, 90.0, 0.0, fill);

        Assert.That(scene.CommandCount, Is.EqualTo(3));
        Assert.That(scene.CommandAt(0).Kind, Is.EqualTo(ChartRenderCommandKind.Polyline));
        Assert.That(scene.CommandAt(0).Id, Is.EqualTo("polar-line"));
        Assert.That(scene.CommandAt(0).Points.Length, Is.EqualTo(3));
        AssertPoint(scene.CommandAt(0).Points[0], 50.0, 10.0);
        AssertPoint(scene.CommandAt(0).Points[1], 90.0, 50.0);
        AssertPoint(scene.CommandAt(0).Points[2], 50.0, 10.0);
        Assert.That(scene.CommandAt(1).Id, Is.EqualTo("polar-point-0"));
        AssertPoint(new ChartPoint(scene.CommandAt(1).X1, scene.CommandAt(1).Y1), 50.0, 70.0);
        Assert.That(scene.CommandAt(2).Kind, Is.EqualTo(ChartRenderCommandKind.Path));
        Assert.That(scene.CommandAt(2).Id, Is.EqualTo("polar-bar-0"));
        Assert.That(scene.ToSvgString(), Does.Contain("id=\"polar-bar-0\""));
    }

    [Test]
    public void RadarGaugeAndBullet_AddExpectedGeometry()
    {
        ChartRenderScene scene = new ChartRenderScene(160, 160);
        ChartPolarTransform transform = new ChartPolarTransform(80.0, 80.0, 0.0, 40.0, 0.0, 10.0, 0.0, true);
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(50, 100, 150));
        ChartRenderStyle background = ChartRenderStyle.CreateFill(ChartColor.FromRgb(230, 230, 230));

        ChartPolarChartsOperations.AddRadarSpokes(scene, "radar", 4, transform, 10.0, stroke);
        ChartPolarChartsOperations.AddRadar(scene, "radar", new double[] { 10.0, 5.0, 10.0, 5.0 }, transform, fill);
        ChartPolarChartsOperations.AddGauge(scene, "gauge", 80.0, 80.0, 30.0, 45.0, 0.0, 100.0, 50.0, -90.0, 90.0, background, fill, stroke);
        ChartPolarChartsOperations.AddBullet(scene, "bullet", 10.0, 120.0, 100.0, 20.0, 0.0, 100.0, 60.0, 80.0, ChartOrientation.Horizontal, background, fill, stroke);

        Assert.That(scene.CommandCount, Is.EqualTo(11));
        Assert.That(scene.CommandAt(0).Id, Is.EqualTo("radar-spoke-0"));
        AssertPoint(new ChartPoint(scene.CommandAt(0).X2, scene.CommandAt(0).Y2), 80.0, 40.0);
        Assert.That(scene.CommandAt(4).Kind, Is.EqualTo(ChartRenderCommandKind.Polygon));
        Assert.That(scene.CommandAt(4).Id, Is.EqualTo("radar-radar"));
        AssertPoint(scene.CommandAt(4).Points[1], 100.0, 80.0);
        Assert.That(scene.CommandAt(7).Id, Is.EqualTo("gauge-needle"));
        AssertPoint(new ChartPoint(scene.CommandAt(7).X2, scene.CommandAt(7).Y2), 80.0, 35.0);
        Assert.That(scene.CommandAt(9).Id, Is.EqualTo("bullet-value"));
        Assert.That(scene.CommandAt(9).Width, Is.EqualTo(60.0).Within(0.0000001));
        Assert.That(scene.CommandAt(10).Id, Is.EqualTo("bullet-target"));
        Assert.That(scene.CommandAt(10).X1, Is.EqualTo(90.0).Within(0.0000001));
    }

    [Test]
    public void Bullet_SupportsVerticalOrientationAndMinimumValue()
    {
        ChartRenderScene scene = new ChartRenderScene(100, 120);
        ChartRenderStyle background = ChartRenderStyle.CreateFill(ChartColor.FromRgb(230, 230, 230));
        ChartRenderStyle fill = ChartRenderStyle.CreateFill(ChartColor.FromRgb(50, 100, 150));
        ChartRenderStyle stroke = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);

        ChartPolarChartsOperations.AddBullet(scene, "vertical", 20.0, 10.0, 20.0, 100.0, 0.0, 100.0, 75.0, 25.0, ChartOrientation.Vertical, background, fill, stroke);
        ChartPolarChartsOperations.AddBullet(scene, "zero", 60.0, 10.0, 20.0, 100.0, 0.0, 100.0, 0.0, 50.0, ChartOrientation.Vertical, background, fill, stroke);

        Assert.That(scene.CommandCount, Is.EqualTo(5));
        Assert.That(scene.CommandAt(1).Id, Is.EqualTo("vertical-value"));
        Assert.That(scene.CommandAt(1).Y1, Is.EqualTo(35.0).Within(0.0000001));
        Assert.That(scene.CommandAt(1).Height, Is.EqualTo(75.0).Within(0.0000001));
        Assert.That(scene.CommandAt(2).Id, Is.EqualTo("vertical-target"));
        Assert.That(scene.CommandAt(2).Y1, Is.EqualTo(85.0).Within(0.0000001));
        Assert.That(scene.CommandAt(4).Id, Is.EqualTo("zero-target"));
    }

    [Test]
    public void PolarCharts_ValidateInputs()
    {
        ChartRenderScene scene = new ChartRenderScene(100, 100);
        ChartPolarTransform transform = new ChartPolarTransform(50.0, 50.0, 0.0, 40.0, 0.0, 10.0, 0.0, true);
        ChartRenderStyle style = ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartPolarTransform(0.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, true));
        Assert.Throws<System.ArgumentException>(() => new ChartPolarTransform(0.0, 0.0, 5.0, 5.0, 0.0, 1.0, 0.0, true));
        Assert.Throws<System.ArgumentException>(() => new ChartPolarTransform(0.0, 0.0, 0.0, 1.0, 1.0, 1.0, 0.0, true));
        Assert.Throws<System.ArgumentNullException>(() => ChartPolarChartsOperations.AddPolarLine(null!, "polar", new double[] { 0.0, 1.0 }, new double[] { 1.0, 1.0 }, transform, false, style));
        Assert.Throws<System.ArgumentException>(() => ChartPolarChartsOperations.AddPolarLine(scene, "", new double[] { 0.0, 1.0 }, new double[] { 1.0, 1.0 }, transform, false, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartPolarChartsOperations.AddPolarLine(scene, "polar", null!, new double[] { 1.0, 1.0 }, transform, false, style));
        Assert.Throws<System.ArgumentException>(() => ChartPolarChartsOperations.AddPolarLine(scene, "polar", new double[] { 0.0 }, new double[] { 1.0 }, transform, false, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartPolarChartsOperations.AddPolarLine(scene, "polar", new double[] { 0.0, 1.0 }, new double[] { 1.0, 1.0 }, null!, false, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartPolarChartsOperations.AddPolarLine(scene, "polar", new double[] { 0.0, 1.0 }, new double[] { 1.0, 1.0 }, transform, false, null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartPolarChartsOperations.AddPolarScatter(scene, "polar", new double[] { 0.0 }, new double[] { 1.0 }, transform, (ChartMarkerShape)99, 4.0, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartPolarChartsOperations.AddPolarScatter(scene, "polar", new double[] { 0.0 }, new double[] { 1.0 }, transform, ChartMarkerShape.Circle, 0.0, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartPolarChartsOperations.AddPolarBars(scene, "polar", new double[] { 0.0 }, new double[] { 1.0 }, transform, 0.0, 0.0, style));
        Assert.Throws<System.ArgumentException>(() => ChartPolarChartsOperations.AddRadar(scene, "radar", new double[] { 1.0, 2.0 }, transform, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartPolarChartsOperations.AddRadarSpokes(scene, "radar", 2, transform, 10.0, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartPolarChartsOperations.AddGauge(null!, "gauge", 50.0, 50.0, 10.0, 20.0, 0.0, 100.0, 50.0, -90.0, 90.0, style, style, style));
        Assert.Throws<System.ArgumentException>(() => ChartPolarChartsOperations.AddGauge(scene, "gauge", 50.0, 50.0, 10.0, 10.0, 0.0, 100.0, 50.0, -90.0, 90.0, style, style, style));
        Assert.Throws<System.ArgumentNullException>(() => ChartPolarChartsOperations.AddBullet(null!, "bullet", 0.0, 0.0, 10.0, 10.0, 0.0, 100.0, 50.0, 60.0, ChartOrientation.Horizontal, style, style, style));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartPolarChartsOperations.AddBullet(scene, "bullet", 0.0, 0.0, 10.0, 10.0, 0.0, 100.0, 50.0, 60.0, ChartOrientation.Radial, style, style, style));
        Assert.That(ChartPolarChartsOperations.GaugeAmount(0.0, 100.0, -10.0), Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(ChartPolarChartsOperations.GaugeAmount(0.0, 100.0, 120.0), Is.EqualTo(1.0).Within(0.0000001));
    }

    static void AssertPoint(ChartPoint point, double x, double y)
    {
        Assert.That(point.X, Is.EqualTo(x).Within(0.0000001));
        Assert.That(point.Y, Is.EqualTo(y).Within(0.0000001));
    }
}
