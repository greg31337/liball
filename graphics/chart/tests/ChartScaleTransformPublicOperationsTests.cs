namespace chart.Tests;

[TestFixture]
public class ChartScaleTransformPublicOperationsTests
{
    [Test]
    public void Configuration_CoversEveryScaleKindChoice()
    {
        ChartScaleKind[] numericKinds = new ChartScaleKind[]
        {
            ChartScaleKind.Linear, ChartScaleKind.Log, ChartScaleKind.SymmetricLog, ChartScaleKind.Power,
            ChartScaleKind.SquareRoot, ChartScaleKind.Time, ChartScaleKind.UtcTime, ChartScaleKind.Diverging,
            ChartScaleKind.Radial, ChartScaleKind.Angle, ChartScaleKind.Color, ChartScaleKind.Size,
            ChartScaleKind.Opacity
        };
        ChartScaleKind[] specializedKinds = new ChartScaleKind[]
        {
            ChartScaleKind.CategoryBand, ChartScaleKind.CategoryPoint, ChartScaleKind.Ordinal,
            ChartScaleKind.Quantile, ChartScaleKind.Quantize, ChartScaleKind.Threshold, ChartScaleKind.Shape
        };
        ChartConfiguration configuration = new ChartConfiguration();
        int index = 0;
        while (index < numericKinds.Length)
        {
            configuration.SetScaleKind(numericKinds[index]);
            double minimum = numericKinds[index] == ChartScaleKind.Log ? 1.0 : 0.0;
            double value = numericKinds[index] == ChartScaleKind.Log ? 10.0 : 5.0;
            double maximum = numericKinds[index] == ChartScaleKind.Log ? 100.0 : 10.0;
            Assert.That(configuration.MapNumericValue(value, minimum, maximum, 0.0, 1.0), Is.InRange(0.0, 1.0), numericKinds[index].ToString());
            index = index + 1;
        }

        index = 0;
        while (index < specializedKinds.Length)
        {
            configuration.SetScaleKind(specializedKinds[index]);
            Assert.Throws<System.InvalidOperationException>(() => configuration.MapNumericValue(0.5, 0.0, 1.0, 0.0, 1.0), specializedKinds[index].ToString());
            index = index + 1;
        }
    }

    [Test]
    public void MutableScaleAccessors_UpdateMappingBehavior()
    {
        ChartLinearScale linear = new ChartLinearScale(0.0, 10.0, 0.0, 100.0);
        linear.SetDomainMinimum(-10.0);
        linear.SetDomainMaximum(10.0);
        linear.SetRangeMinimum(10.0);
        linear.SetRangeMaximum(30.0);
        Assert.That(linear.GetDomainMinimum(), Is.EqualTo(-10.0));
        Assert.That(linear.GetDomainMaximum(), Is.EqualTo(10.0));
        Assert.That(linear.GetRangeMinimum(), Is.EqualTo(10.0));
        Assert.That(linear.GetRangeMaximum(), Is.EqualTo(30.0));
        Assert.That(linear.Map(0.0), Is.EqualTo(20.0));

        ChartLogScale logarithmic = new ChartLogScale(1.0, 100.0, 0.0, 2.0, 10.0);
        logarithmic.SetDomainMinimum(1.0);
        logarithmic.SetDomainMaximum(1000.0);
        logarithmic.SetRangeMinimum(-1.0);
        logarithmic.SetRangeMaximum(2.0);
        logarithmic.SetBaseValue(10.0);
        Assert.That(logarithmic.GetDomainMinimum(), Is.EqualTo(1.0));
        Assert.That(logarithmic.GetDomainMaximum(), Is.EqualTo(1000.0));
        Assert.That(logarithmic.GetRangeMinimum(), Is.EqualTo(-1.0));
        Assert.That(logarithmic.GetRangeMaximum(), Is.EqualTo(2.0));
        Assert.That(logarithmic.GetBaseValue(), Is.EqualTo(10.0));
        Assert.That(logarithmic.Map(10.0), Is.EqualTo(0.0).Within(0.0000001));

        ChartTimeScale time = new ChartTimeScale(new System.DateTime(2024, 1, 1), new System.DateTime(2024, 1, 2), 0.0, 24.0);
        time.SetDomainStart(new System.DateTime(2024, 2, 1));
        time.SetDomainEnd(new System.DateTime(2024, 2, 3));
        time.SetRangeMinimum(10.0);
        time.SetRangeMaximum(30.0);
        Assert.That(time.GetDomainStart(), Is.EqualTo(new System.DateTime(2024, 2, 1)));
        Assert.That(time.GetDomainEnd(), Is.EqualTo(new System.DateTime(2024, 2, 3)));
        Assert.That(time.GetRangeMinimum(), Is.EqualTo(10.0));
        Assert.That(time.GetRangeMaximum(), Is.EqualTo(30.0));
        Assert.That(time.Map(new System.DateTime(2024, 2, 2)), Is.EqualTo(20.0));
    }

    [Test]
    public void CategoricalAndVisualScaleAccessors_UpdateMappingBehavior()
    {
        ChartCategoryBandScale bands = new ChartCategoryBandScale(new string[] { "A", "B" }, 0.0, 100.0, 0.1, 0.1);
        bands.SetCategories(new string[] { "C", "D", "E" });
        bands.SetRangeMinimum(10.0);
        bands.SetRangeMaximum(70.0);
        bands.SetInnerPadding(0.2);
        bands.SetOuterPadding(0.2);
        bands.SetStep(20.0);
        bands.SetBandwidth(16.0);
        Assert.That(bands.GetCategories(), Is.EqualTo(new string[] { "C", "D", "E" }));
        Assert.That(bands.GetRangeMinimum(), Is.EqualTo(10.0));
        Assert.That(bands.GetRangeMaximum(), Is.EqualTo(70.0));
        Assert.That(bands.GetInnerPadding(), Is.EqualTo(0.2));
        Assert.That(bands.GetOuterPadding(), Is.EqualTo(0.2));
        Assert.That(bands.GetStep(), Is.EqualTo(20.0));
        Assert.That(bands.GetBandwidth(), Is.EqualTo(16.0));
        Assert.That(bands.CategoryIndex("E"), Is.EqualTo(2));

        ChartCategoryPointScale points = new ChartCategoryPointScale(new string[] { "A", "B" }, 0.0, 10.0, 0.0);
        points.SetCategories(new string[] { "left", "middle", "right" });
        points.SetRangeMinimum(5.0);
        points.SetRangeMaximum(25.0);
        points.SetPadding(0.5);
        points.SetStep(5.0);
        Assert.That(points.GetCategories(), Is.EqualTo(new string[] { "left", "middle", "right" }));
        Assert.That(points.GetRangeMinimum(), Is.EqualTo(5.0));
        Assert.That(points.GetRangeMaximum(), Is.EqualTo(25.0));
        Assert.That(points.GetPadding(), Is.EqualTo(0.5));
        Assert.That(points.GetStep(), Is.EqualTo(5.0));
        Assert.That(points.CategoryIndex("middle"), Is.EqualTo(1));

        ChartOrdinalScale ordinal = new ChartOrdinalScale(new string[] { "A", "B" }, new double[] { 1.0, 2.0 });
        ordinal.SetCategories(new string[] { "C", "D" });
        ordinal.SetValues(new double[] { 3.0, 4.0 });
        Assert.That(ordinal.GetCategories(), Is.EqualTo(new string[] { "C", "D" }));
        Assert.That(ordinal.GetValues(), Is.EqualTo(new double[] { 3.0, 4.0 }));
        Assert.That(ordinal.Map("D"), Is.EqualTo(4.0));
    }

    [Test]
    public void ColorAndSizeScaleAccessors_UpdateStoredValuesAndClamp()
    {
        ChartColor start = ChartColor.FromRgb(10, 20, 30);
        ChartColor end = ChartColor.FromRgb(110, 120, 130);
        ChartColorScale color = new ChartColorScale(0.0, 10.0, start, end);
        color.SetDomainMinimum(-10.0);
        color.SetDomainMaximum(10.0);
        color.SetStartColor(ChartColor.Black());
        color.SetEndColor(ChartColor.White());
        color.SetClamp(true);
        ChartColor returnedStart = color.GetStartColor();
        returnedStart.SetR(200);
        Assert.That(color.GetDomainMinimum(), Is.EqualTo(-10.0));
        Assert.That(color.GetDomainMaximum(), Is.EqualTo(10.0));
        Assert.That(color.GetStartColor().GetR(), Is.EqualTo(200));
        Assert.That(color.GetEndColor().GetR(), Is.EqualTo(255));
        Assert.That(color.GetClamp(), Is.True);
        Assert.That(color.Map(20.0).GetR(), Is.EqualTo(255));

        ChartSizeScale size = new ChartSizeScale(0.0, 10.0, 2.0, 12.0);
        size.SetDomainMinimum(-10.0);
        size.SetDomainMaximum(10.0);
        size.SetSizeMinimum(1.0);
        size.SetSizeMaximum(5.0);
        size.SetClamp(true);
        Assert.That(size.GetDomainMinimum(), Is.EqualTo(-10.0));
        Assert.That(size.GetDomainMaximum(), Is.EqualTo(10.0));
        Assert.That(size.GetSizeMinimum(), Is.EqualTo(1.0));
        Assert.That(size.GetSizeMaximum(), Is.EqualTo(5.0));
        Assert.That(size.GetClamp(), Is.True);
        Assert.That(size.Map(20.0), Is.EqualTo(5.0));
    }

    [Test]
    public void CartesianAndPolarAccessors_ChangeCoordinateBehavior()
    {
        ChartCartesianTransform cartesian = new ChartCartesianTransform(new ChartRect(0.0, 0.0, 100.0, 100.0), new ChartRange(0.0, 10.0), new ChartRange(0.0, 10.0));
        cartesian.SetPlotArea(new ChartRect(10.0, 20.0, 200.0, 100.0));
        cartesian.SetXDomain(new ChartRange(-10.0, 10.0));
        cartesian.SetYDomain(new ChartRange(-5.0, 5.0));
        ChartPoint[] mapped = cartesian.MapPoints(new double[] { 0.0, 10.0 }, new double[] { 0.0, 5.0 });
        Assert.That(cartesian.GetPlotArea().GetX(), Is.EqualTo(10.0));
        Assert.That(cartesian.GetXDomain().GetMinimum(), Is.EqualTo(-10.0));
        Assert.That(cartesian.GetYDomain().GetMaximum(), Is.EqualTo(5.0));
        Assert.That(mapped[0].GetX(), Is.EqualTo(110.0));
        Assert.That(mapped[0].GetY(), Is.EqualTo(70.0));
        Assert.That(mapped[1].GetX(), Is.EqualTo(210.0));

        ChartPolarTransform polar = new ChartPolarTransform(0.0, 0.0, 10.0, 50.0, 0.0, 100.0, 0.0, true);
        polar.SetCenterX(5.0);
        polar.SetCenterY(6.0);
        polar.SetInnerRadius(20.0);
        polar.SetOuterRadius(60.0);
        polar.SetMinimumValue(-10.0);
        polar.SetMaximumValue(10.0);
        polar.SetStartAngle(System.Math.PI / 2.0);
        polar.SetClockwise(false);
        polar.SetClamp(true);
        Assert.That(polar.GetCenterX(), Is.EqualTo(5.0));
        Assert.That(polar.GetCenterY(), Is.EqualTo(6.0));
        Assert.That(polar.GetInnerRadius(), Is.EqualTo(20.0));
        Assert.That(polar.GetOuterRadius(), Is.EqualTo(60.0));
        Assert.That(polar.GetMinimumValue(), Is.EqualTo(-10.0));
        Assert.That(polar.GetMaximumValue(), Is.EqualTo(10.0));
        Assert.That(polar.GetStartAngle(), Is.EqualTo(System.Math.PI / 2.0));
        Assert.That(polar.GetClockwise(), Is.False);
        Assert.That(polar.GetClamp(), Is.True);
        Assert.That(polar.MapRadius(20.0), Is.EqualTo(60.0));
    }

    [Test]
    public void TransformCopiesAndProjectionAccessors_AreIndependentAndComplete()
    {
        ChartTernaryTransform ternary = new ChartTernaryTransform(new ChartPoint(0.0, 0.0), new ChartPoint(2.0, 0.0), new ChartPoint(0.0, 2.0));
        ChartPoint first = ternary.GetFirstVertex();
        first.SetX(100.0);
        Assert.That(ternary.GetFirstVertex().GetX(), Is.EqualTo(0.0));
        Assert.That(ternary.GetSecondVertex().GetX(), Is.EqualTo(2.0));
        Assert.That(ternary.GetThirdVertex().GetY(), Is.EqualTo(2.0));

        ChartGeographicProjection projection = new ChartGeographicProjection(ChartGeographicProjectionKind.AlbersLike, -96.0, 23.0, 29.5, 45.5);
        Assert.That(projection.GetKind(), Is.EqualTo(ChartGeographicProjectionKind.AlbersLike));
        Assert.That(projection.GetCentralLongitude(), Is.EqualTo(-96.0));
        Assert.That(projection.GetCentralLatitude(), Is.EqualTo(23.0));
        Assert.That(projection.GetFirstStandardParallel(), Is.EqualTo(29.5));
        Assert.That(projection.GetSecondStandardParallel(), Is.EqualTo(45.5));
    }

    [Test]
    public void ScaleAndTransformOperations_RejectInvalidConfiguration()
    {
        ChartCartesianTransform cartesian = new ChartCartesianTransform(new ChartRect(0.0, 0.0, 10.0, 10.0), new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0));
        Assert.Throws<System.ArgumentNullException>(() => cartesian.MapPoints(null!, new double[] { 0.0 }));
        Assert.Throws<System.ArgumentException>(() => cartesian.MapPoints(new double[] { 0.0 }, new double[] { 0.0, 1.0 }));
        Assert.Throws<System.ArgumentException>(() => cartesian.SetPlotArea(new ChartRect(0.0, 0.0, 0.0, 1.0)));
        Assert.Throws<System.ArgumentException>(() => new ChartLinearScale(0.0, 0.0, 0.0, 1.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartPolarTransform(0.0, 0.0, -1.0, 10.0, 0.0, 1.0, 0.0, true));
    }
}
