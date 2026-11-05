namespace chart.Tests;

[TestFixture]
public class ChartAdvancedScalesTests
{
    [Test]
    public void TransformedScales_MapAndInvertAcrossZero()
    {
        ChartSymmetricLogScale symmetricLog = new ChartSymmetricLogScale(-9.0, 9.0, 0.0, 1.0, 1.0);
        ChartPowerScale power = new ChartPowerScale(-2.0, 2.0, 0.0, 1.0, 2.0);
        ChartSquareRootScale squareRoot = new ChartSquareRootScale(0.0, 100.0, 0.0, 1.0);

        Assert.That(symmetricLog.Map(0.0), Is.EqualTo(0.5).Within(0.0000001));
        Assert.That(symmetricLog.Map(-9.0), Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(symmetricLog.Invert(symmetricLog.Map(-3.0)), Is.EqualTo(-3.0).Within(0.0000001));
        Assert.That(power.Map(0.0), Is.EqualTo(0.5).Within(0.0000001));
        Assert.That(power.Map(1.0), Is.EqualTo(0.625).Within(0.0000001));
        Assert.That(power.Invert(power.Map(-1.5)), Is.EqualTo(-1.5).Within(0.0000001));
        Assert.That(squareRoot.Map(25.0), Is.EqualTo(0.5).Within(0.0000001));
        Assert.That(squareRoot.Invert(0.5), Is.EqualTo(25.0).Within(0.0000001));
    }

    [Test]
    public void UtcTimeScale_PreservesUtcKindWhenInverting()
    {
        System.DateTime start = new System.DateTime(2024, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        System.DateTime end = new System.DateTime(2024, 1, 11, 0, 0, 0, System.DateTimeKind.Utc);
        ChartUtcTimeScale scale = new ChartUtcTimeScale(start, end, 0.0, 100.0);

        System.DateTime inverted = scale.Invert(25.0);

        Assert.That(scale.Map(new System.DateTime(2024, 1, 6, 0, 0, 0, System.DateTimeKind.Utc)), Is.EqualTo(50.0));
        Assert.That(inverted, Is.EqualTo(new System.DateTime(2024, 1, 3, 12, 0, 0, System.DateTimeKind.Utc)));
        Assert.That(inverted.Kind, Is.EqualTo(System.DateTimeKind.Utc));
        Assert.Throws<System.ArgumentException>(() => scale.Map(new System.DateTime(2024, 1, 2)));
    }

    [Test]
    public void QuantileScale_ComputesR7ThresholdsAndClassifiesValues()
    {
        ChartQuantileScale scale = new ChartQuantileScale(
            new double[] { 9.0, 0.0, 8.0, 1.0, 7.0, 2.0, 6.0, 3.0, 5.0, 4.0 },
            new double[] { 10.0, 20.0, 30.0, 40.0 });

        Assert.That(scale.CopyThresholds(), Is.EqualTo(new double[] { 2.25, 4.5, 6.75 }));
        Assert.That(scale.Map(2.0), Is.EqualTo(10.0));
        Assert.That(scale.Map(2.25), Is.EqualTo(20.0));
        Assert.That(scale.Map(6.75), Is.EqualTo(40.0));
    }

    [Test]
    public void QuantizeScale_UsesEqualBucketsAndReturnsTheirExtents()
    {
        ChartQuantizeScale scale = new ChartQuantizeScale(0.0, 100.0, new double[] { 0.0, 1.0, 2.0, 3.0 });

        ChartRange extent = scale.InvertExtent(2);

        Assert.That(scale.Map(-10.0), Is.EqualTo(0.0));
        Assert.That(scale.Map(24.999), Is.EqualTo(0.0));
        Assert.That(scale.Map(25.0), Is.EqualTo(1.0));
        Assert.That(scale.Map(100.0), Is.EqualTo(3.0));
        Assert.That(extent.GetMinimum(), Is.EqualTo(50.0));
        Assert.That(extent.GetMaximum(), Is.EqualTo(75.0));
    }

    [Test]
    public void ThresholdScale_AssignsThresholdEqualityToTheUpperBucket()
    {
        ChartThresholdScale scale = new ChartThresholdScale(new double[] { 10.0, 20.0 }, new double[] { 1.0, 2.0, 3.0 });

        Assert.That(scale.Map(9.0), Is.EqualTo(1.0));
        Assert.That(scale.Map(10.0), Is.EqualTo(2.0));
        Assert.That(scale.Map(20.0), Is.EqualTo(3.0));
    }

    [Test]
    public void DivergingScale_UsesItsExplicitMidpoints()
    {
        ChartDivergingScale scale = new ChartDivergingScale(-10.0, 0.0, 20.0, 0.0, 0.5, 1.0);
        ChartDivergingScale reversed = new ChartDivergingScale(-10.0, 0.0, 20.0, 1.0, 0.5, 0.0);

        Assert.That(scale.Map(-5.0), Is.EqualTo(0.25));
        Assert.That(scale.Map(10.0), Is.EqualTo(0.75));
        Assert.That(scale.Invert(0.75), Is.EqualTo(10.0));
        Assert.That(reversed.Map(-5.0), Is.EqualTo(0.75));
        Assert.That(reversed.Invert(0.25), Is.EqualTo(10.0));
    }

    [Test]
    public void RadialAngleAndOpacityScales_MapTheirVisualChannels()
    {
        ChartRadialScale radial = new ChartRadialScale(0.0, 100.0, 0.0, 10.0);
        ChartAngleScale angle = new ChartAngleScale(0.0, 100.0, -System.Math.PI, System.Math.PI);
        ChartOpacityScale opacity = new ChartOpacityScale(0.0, 100.0, 0.2, 1.0);

        Assert.That(radial.Map(25.0), Is.EqualTo(5.0).Within(0.0000001));
        Assert.That(radial.Invert(5.0), Is.EqualTo(25.0).Within(0.0000001));
        Assert.That(angle.Map(50.0), Is.EqualTo(0.0).Within(0.0000001));
        Assert.That(angle.Invert(0.0), Is.EqualTo(50.0).Within(0.0000001));
        Assert.That(opacity.Map(50.0), Is.EqualTo(0.6).Within(0.0000001));
        Assert.That(opacity.Map(-10.0), Is.EqualTo(0.2));
        Assert.That(opacity.Map(110.0), Is.EqualTo(1.0));
        Assert.That(opacity.Invert(0.6), Is.EqualTo(50.0).Within(0.0000001));
    }

    [Test]
    public void ShapeScale_MapsCategoriesToMarkerShapes()
    {
        ChartShapeScale scale = new ChartShapeScale(
            new string[] { "actual", "forecast" },
            new ChartMarkerShape[] { ChartMarkerShape.Circle, ChartMarkerShape.Diamond });

        Assert.That(scale.Map("actual"), Is.EqualTo(ChartMarkerShape.Circle));
        Assert.That(scale.Map("forecast"), Is.EqualTo(ChartMarkerShape.Diamond));
        Assert.Throws<System.ArgumentException>(() => scale.Map("missing"));
    }

    [Test]
    public void AdvancedScales_ValidateInvalidDefinitions()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartSymmetricLogScale(-1.0, 1.0, 0.0, 1.0, 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartPowerScale(0.0, 1.0, 0.0, 1.0, -1.0));
        Assert.Throws<System.ArgumentException>(() => new ChartUtcTimeScale(new System.DateTime(2024, 1, 1), new System.DateTime(2024, 1, 2), 0.0, 1.0));
        Assert.Throws<System.ArgumentException>(() => new ChartThresholdScale(new double[] { 20.0, 10.0 }, new double[] { 1.0, 2.0, 3.0 }));
        Assert.Throws<System.ArgumentException>(() => new ChartDivergingScale(-1.0, -1.0, 1.0, 0.0, 0.5, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartRadialScale(0.0, 1.0, -1.0, 2.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartOpacityScale(0.0, 1.0, 0.0, 2.0));
        Assert.Throws<System.ArgumentException>(() => new ChartShapeScale(new string[] { "A", "A" }, new ChartMarkerShape[] { ChartMarkerShape.Circle, ChartMarkerShape.Square }));
    }

    [Test]
    public void Configuration_UsesDeclaredNumericAdvancedScaleKinds()
    {
        ChartConfiguration configuration = new ChartConfiguration();

        configuration.SetScaleKind(ChartScaleKind.SymmetricLog);
        Assert.That(configuration.MapNumericValue(0.0, -9.0, 9.0, 0.0, 1.0), Is.EqualTo(0.5).Within(0.0000001));
        configuration.SetScaleKind(ChartScaleKind.Power);
        Assert.That(configuration.MapNumericValue(5.0, 0.0, 10.0, 0.0, 1.0), Is.EqualTo(0.25).Within(0.0000001));
        configuration.SetScaleKind(ChartScaleKind.SquareRoot);
        Assert.That(configuration.MapNumericValue(25.0, 0.0, 100.0, 0.0, 1.0), Is.EqualTo(0.5).Within(0.0000001));
        configuration.SetScaleKind(ChartScaleKind.Radial);
        Assert.That(configuration.MapNumericValue(25.0, 0.0, 100.0, 0.0, 10.0), Is.EqualTo(5.0).Within(0.0000001));
        configuration.SetScaleKind(ChartScaleKind.Opacity);
        Assert.That(configuration.MapNumericValue(150.0, 0.0, 100.0, 0.2, 1.0), Is.EqualTo(1.0));
        configuration.SetScaleKind(ChartScaleKind.Shape);
        Assert.Throws<System.InvalidOperationException>(() => configuration.MapNumericValue(1.0, 0.0, 2.0, 0.0, 1.0));
    }
}
