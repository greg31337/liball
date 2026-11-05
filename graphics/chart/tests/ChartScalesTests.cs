namespace chart.Tests;

[TestFixture]
public class ChartScalesTests
{
    [Test]
    public void LinearScale_MapsAndInvertsValues()
    {
        ChartLinearScale scale = new ChartLinearScale(0.0, 10.0, 100.0, 200.0);

        Assert.That(scale.Map(2.5), Is.EqualTo(125.0));
        Assert.That(scale.Invert(175.0), Is.EqualTo(7.5));
    }

    [Test]
    public void LogScale_MapsPositiveValues()
    {
        ChartLogScale scale = new ChartLogScale(1.0, 100.0, 0.0, 1.0, 10.0);

        Assert.That(scale.Map(10.0), Is.EqualTo(0.5).Within(0.0000001));
        Assert.That(scale.Map(100.0), Is.EqualTo(1.0).Within(0.0000001));
    }

    [Test]
    public void TimeScale_MapsDateTimesAndInvertsRangeValues()
    {
        System.DateTime start = new System.DateTime(2024, 1, 1);
        System.DateTime end = new System.DateTime(2024, 1, 11);
        ChartTimeScale scale = new ChartTimeScale(start, end, 0.0, 100.0);

        Assert.That(scale.Map(new System.DateTime(2024, 1, 6)), Is.EqualTo(50.0));
        Assert.That(scale.Invert(25.0), Is.EqualTo(new System.DateTime(2024, 1, 3, 12, 0, 0)));
    }

    [Test]
    public void CategoryBandScale_MapsCategoriesToBandStarts()
    {
        ChartCategoryBandScale scale = new ChartCategoryBandScale(new string[] { "A", "B", "C" }, 0.0, 300.0, 0.1, 0.2);

        Assert.That(scale.Step, Is.EqualTo(300.0 / 3.3).Within(0.0000001));
        Assert.That(scale.Bandwidth, Is.EqualTo((300.0 / 3.3) * 0.9).Within(0.0000001));
        Assert.That(scale.Map("A"), Is.EqualTo(scale.Step * 0.2).Within(0.0000001));
        Assert.That(scale.Map("C"), Is.EqualTo(scale.Step * 2.2).Within(0.0000001));
        Assert.That(scale.CopyCategories(), Is.EqualTo(new string[] { "A", "B", "C" }));
    }

    [Test]
    public void CategoryPointScale_MapsCategoriesToPoints()
    {
        ChartCategoryPointScale scale = new ChartCategoryPointScale(new string[] { "A", "B", "C" }, 0.0, 100.0, 0.5);
        ChartCategoryPointScale single = new ChartCategoryPointScale(new string[] { "Only" }, 10.0, 30.0, 0.5);

        Assert.That(scale.Step, Is.EqualTo(100.0 / 3.0).Within(0.0000001));
        Assert.That(scale.Map("A"), Is.EqualTo(100.0 / 6.0).Within(0.0000001));
        Assert.That(scale.Map("C"), Is.EqualTo(250.0 / 3.0).Within(0.0000001));
        Assert.That(single.Map("Only"), Is.EqualTo(20.0));
    }

    [Test]
    public void OrdinalColorAndSizeScales_MapValues()
    {
        ChartOrdinalScale ordinal = new ChartOrdinalScale(new string[] { "small", "large" }, new double[] { 1.0, 10.0 });
        ChartColorScale color = new ChartColorScale(0.0, 10.0, ChartColor.Black(), ChartColor.White());
        ChartSizeScale size = new ChartSizeScale(0.0, 100.0, 4.0, 20.0);

        ChartColor midpoint = color.Map(5.0);

        Assert.That(ordinal.Map("large"), Is.EqualTo(10.0));
        Assert.That(midpoint.R, Is.EqualTo(128));
        Assert.That(midpoint.G, Is.EqualTo(128));
        Assert.That(midpoint.B, Is.EqualTo(128));
        Assert.That(size.Map(25.0), Is.EqualTo(8.0));
    }

    [Test]
    public void LinearScale_ClampsReversesAndCopies()
    {
        ChartLinearScale scale = new ChartLinearScale(0.0, 10.0, 0.0, 100.0);

        ChartLinearScale clamped = scale.WithClamp(true);
        ChartLinearScale reversed = scale.ReverseRange();
        ChartLinearScale copy = clamped.Copy();

        Assert.That(scale.Map(-5.0), Is.EqualTo(-50.0));
        Assert.That(clamped.Map(-5.0), Is.EqualTo(0.0));
        Assert.That(clamped.Map(15.0), Is.EqualTo(100.0));
        Assert.That(clamped.Invert(200.0), Is.EqualTo(10.0));
        Assert.That(reversed.Map(0.0), Is.EqualTo(100.0));
        Assert.That(reversed.Map(10.0), Is.EqualTo(0.0));
        Assert.That(copy.Clamp, Is.True);
    }

    [Test]
    public void LinearScale_UpdatesExplicitDomainAndRange()
    {
        ChartLinearScale scale = new ChartLinearScale(0.0, 1.0, 0.0, 1.0);

        scale.SetDomain(10.0, 20.0);
        scale.SetRange(100.0, 200.0);

        Assert.That(scale.Map(15.0), Is.EqualTo(150.0));
        Assert.That(scale.WithDomain(0.0, 100.0).Map(25.0), Is.EqualTo(125.0));
        Assert.That(scale.WithRange(0.0, 10.0).Map(15.0), Is.EqualTo(5.0));
    }

    [Test]
    public void LinearScale_IncludesZeroPadsAndNicesDomain()
    {
        ChartLinearScale scale = new ChartLinearScale(3.2, 9.7, 0.0, 100.0);
        ChartLinearScale included = new ChartLinearScale(5.0, 15.0, 0.0, 1.0).IncludeZero();
        ChartLinearScale padded = new ChartLinearScale(5.0, 15.0, 0.0, 1.0).PadDomain(0.1);
        ChartLinearScale nice = scale.NiceDomain(5);

        Assert.That(included.DomainMinimum, Is.EqualTo(0.0));
        Assert.That(included.DomainMaximum, Is.EqualTo(15.0));
        Assert.That(padded.DomainMinimum, Is.EqualTo(4.0));
        Assert.That(padded.DomainMaximum, Is.EqualTo(16.0));
        Assert.That(nice.DomainMinimum, Is.EqualTo(2.0));
        Assert.That(nice.DomainMaximum, Is.EqualTo(10.0));
    }

    [Test]
    public void ColorAndSizeScales_CanClampMappedValues()
    {
        ChartColorScale color = new ChartColorScale(0.0, 10.0, ChartColor.Black(), ChartColor.White());
        ChartSizeScale size = new ChartSizeScale(0.0, 10.0, 2.0, 12.0);

        color.SetClamp(true);
        size.SetClamp(true);

        Assert.That(color.Map(-5.0).R, Is.EqualTo(0));
        Assert.That(color.Map(15.0).R, Is.EqualTo(255));
        Assert.That(size.Map(-5.0), Is.EqualTo(2.0));
        Assert.That(size.Map(15.0), Is.EqualTo(12.0));
    }

    [Test]
    public void ScaleUtilities_ValidateNicePaddingAndClampInputs()
    {
        Assert.That(ChartScaleUtilities.NiceNumber(2.6, true), Is.EqualTo(2.0));
        Assert.That(ChartScaleUtilities.NiceNumber(2.6, false), Is.EqualTo(5.0));
        Assert.That(ChartScaleUtilities.ClampUnit(-0.1), Is.EqualTo(0.0));
        Assert.That(ChartScaleUtilities.ClampUnit(1.1), Is.EqualTo(1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartScaleUtilities.PadRange(0.0, 1.0, -0.1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartScaleUtilities.NiceDomain(0.0, 1.0, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartScaleUtilities.NiceNumber(0.0, true));
    }

    [Test]
    public void Scales_ValidateInputs()
    {
        Assert.Throws<System.ArgumentException>(() => new ChartLinearScale(1.0, 1.0, 0.0, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartLinearScale(0.0, 1.0, double.NaN, 1.0));
        Assert.Throws<System.ArgumentException>(() => new ChartLogScale(0.0, 10.0, 0.0, 1.0, 10.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartLogScale(1.0, 10.0, 0.0, 1.0, 10.0).Map(0.0));
        Assert.Throws<System.ArgumentException>(() => new ChartTimeScale(new System.DateTime(2024, 1, 1), new System.DateTime(2024, 1, 1), 0.0, 1.0));
        Assert.Throws<System.ArgumentNullException>(() => new ChartCategoryBandScale(null!, 0.0, 1.0, 0.0, 0.0));
        Assert.Throws<System.ArgumentException>(() => new ChartCategoryBandScale(new string[] { "" }, 0.0, 1.0, 0.0, 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartCategoryBandScale(new string[] { "A" }, 0.0, 1.0, 1.0, 0.0));
        Assert.Throws<System.ArgumentException>(() => new ChartCategoryPointScale(new string[] { "A" }, 0.0, 1.0, 0.0).Map("B"));
        Assert.Throws<System.ArgumentException>(() => new ChartOrdinalScale(new string[] { "A" }, new double[] { 1.0, 2.0 }));
        Assert.Throws<System.ArgumentNullException>(() => new ChartColorScale(0.0, 1.0, null!, ChartColor.White()));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartSizeScale(0.0, 1.0, -1.0, 2.0));
    }
}
