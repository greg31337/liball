namespace chart.Tests;

[TestFixture]
public class ChartVisualMapTests
{
    [Test]
    public void ContinuousVisualMap_InterpolatesClampsAndBuildsGradientLegend()
    {
        ChartColorMap colorMap = new ChartColorMap(
            "gray",
            new ChartColor[]
            {
                ChartColor.Black(),
                ChartColor.White()
            });
        ChartVisualMap map = ChartVisualMap.CreateContinuous(ChartEncodingChannel.Color, 0.0, 100.0, colorMap);

        colorMap.Stops[0].R = 200;
        map.SetTitle("Intensity");
        ChartLegend legend = map.BuildLegend("colorbar");

        Assert.That(map.Kind, Is.EqualTo(ChartVisualMapKind.Continuous));
        AssertColor(map.MapNumber(-10.0), 0, 0, 0, 255);
        AssertColor(map.MapNumber(50.0), 128, 128, 128, 255);
        AssertColor(map.MapNumber(120.0), 255, 255, 255, 255);
        Assert.That(legend.Title, Is.EqualTo("Intensity"));
        Assert.That(legend.EntryCount, Is.EqualTo(1));
        Assert.That(legend.EntryAt(0).Kind, Is.EqualTo(ChartLegendEntryKind.Gradient));
        Assert.That(legend.EntryAt(0).Label, Is.EqualTo("0 - 100"));
        AssertColor(legend.EntryAt(0).GradientColors[0], 0, 0, 0, 255);
        Assert.That(legend.EntryAt(0).GradientOffsets, Is.EqualTo(new double[] { 0.0, 1.0 }));
    }

    [Test]
    public void BinnedVisualMap_MapsValuesToBinsAndBuildsSymbolLegend()
    {
        ChartVisualMap map = ChartVisualMap.CreateBinned(ChartEncodingChannel.Color, 0.0, 100.0, 4, ChartPalettesOperations.GrayscaleMap());
        ChartLegend legend = map.BuildLegend("bins");

        Assert.That(map.Kind, Is.EqualTo(ChartVisualMapKind.Binned));
        Assert.That(map.BinCount, Is.EqualTo(4));
        Assert.That(map.BinIndex(-1.0), Is.EqualTo(0));
        Assert.That(map.BinIndex(0.0), Is.EqualTo(0));
        Assert.That(map.BinIndex(24.999), Is.EqualTo(0));
        Assert.That(map.BinIndex(25.0), Is.EqualTo(1));
        Assert.That(map.BinIndex(100.0), Is.EqualTo(3));
        AssertColor(map.MapNumber(12.5), 32, 32, 32, 255);
        AssertColor(map.MapNumber(90.0), 223, 223, 223, 255);
        Assert.That(map.Bins[0].Label, Is.EqualTo("0 - 25"));
        Assert.That(map.Bins[3].Label, Is.EqualTo("75 - 100"));
        Assert.That(legend.EntryCount, Is.EqualTo(4));
        Assert.That(legend.EntryAt(0).Kind, Is.EqualTo(ChartLegendEntryKind.Symbol));
        Assert.That(legend.EntryAt(0).Label, Is.EqualTo("0 - 25"));
        AssertColor(legend.EntryAt(3).Style.FillColor, 223, 223, 223, 255);
    }

    [Test]
    public void ThresholdVisualMap_UsesStrictBreaksAndReadableLegendLabels()
    {
        ChartColor low = ChartColor.FromRgb(0, 0, 255);
        ChartColor middle = ChartColor.FromRgb(255, 255, 0);
        ChartColor high = ChartColor.FromRgb(255, 0, 0);
        ChartVisualMap map = ChartVisualMap.CreateThreshold(
            ChartEncodingChannel.Color,
            new double[] { 10.0, 20.0 },
            new ChartColor[] { low, middle, high });
        ChartLegend legend = map.BuildLegend("thresholds");

        low.B = 0;

        Assert.That(map.Kind, Is.EqualTo(ChartVisualMapKind.Threshold));
        Assert.That(map.ThresholdIndex(9.99), Is.EqualTo(0));
        Assert.That(map.ThresholdIndex(10.0), Is.EqualTo(1));
        Assert.That(map.ThresholdIndex(20.0), Is.EqualTo(2));
        AssertColor(map.MapNumber(9.99), 0, 0, 255, 255);
        AssertColor(map.MapNumber(10.0), 255, 255, 0, 255);
        AssertColor(map.MapNumber(20.0), 255, 0, 0, 255);
        Assert.That(legend.EntryCount, Is.EqualTo(3));
        Assert.That(legend.EntryAt(0).Label, Is.EqualTo("< 10"));
        Assert.That(legend.EntryAt(1).Label, Is.EqualTo("10 - 20"));
        Assert.That(legend.EntryAt(2).Label, Is.EqualTo(">= 20"));
    }

    [Test]
    public void CategoricalVisualMap_UsesPaletteOrderAndCyclesColors()
    {
        ChartPalette palette = new ChartPalette(
            "two",
            new ChartColor[]
            {
                ChartColor.FromRgb(255, 0, 0),
                ChartColor.FromRgb(0, 255, 0)
            });
        ChartVisualMap map = ChartVisualMap.CreateCategorical(
            ChartEncodingChannel.Color,
            new string[] { "North", "South", "West" },
            palette);
        ChartLegend legend = map.BuildLegend("categories");

        palette.Colors[0].R = 0;

        Assert.That(map.Kind, Is.EqualTo(ChartVisualMapKind.Categorical));
        Assert.That(map.CategoryCount, Is.EqualTo(3));
        Assert.That(map.CategoryIndex("South"), Is.EqualTo(1));
        AssertColor(map.MapCategory("North"), 255, 0, 0, 255);
        AssertColor(map.MapCategory("South"), 0, 255, 0, 255);
        AssertColor(map.MapCategory("West"), 255, 0, 0, 255);
        Assert.That(legend.EntryCount, Is.EqualTo(3));
        Assert.That(legend.EntryAt(2).Label, Is.EqualTo("West"));
        AssertColor(legend.EntryAt(2).Style.FillColor, 255, 0, 0, 255);
        Assert.Throws<System.ArgumentException>(() => map.MapCategory("Missing"));
    }

    [Test]
    public void VisualMap_CopyReturnsIndependentArraysAndColors()
    {
        ChartVisualMap map = ChartVisualMap.CreateBinned(ChartEncodingChannel.Color, 0.0, 10.0, 2, ChartPalettesOperations.GrayscaleMap());
        map.SetTitle("Bins");

        ChartVisualMap copy = map.Copy();
        copy.Bins[0].Color.R = 200;
        copy.Bins[0].Label = "Changed";
        copy.Title = "Copy";

        Assert.That(map.Title, Is.EqualTo("Bins"));
        Assert.That(map.Bins[0].Label, Is.EqualTo("0 - 5"));
        AssertColor(map.Bins[0].Color, 64, 64, 64, 255);
        AssertColor(copy.Bins[0].Color, 200, 64, 64, 255);
    }

    [Test]
    public void VisualMap_ValidatesInputsAndMapKinds()
    {
        ChartVisualMap continuous = ChartVisualMap.CreateContinuous(ChartEncodingChannel.Color, 0.0, 1.0, ChartPalettesOperations.GrayscaleMap());
        ChartVisualMap categorical = ChartVisualMap.CreateCategorical(ChartEncodingChannel.Color, new string[] { "A" }, ChartPalettesOperations.Categorical());
        ChartVisualMap threshold = ChartVisualMap.CreateThreshold(ChartEncodingChannel.Color, new double[] { 1.0 }, new ChartColor[] { ChartColor.Black(), ChartColor.White() });

        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChartVisualMap((ChartVisualMapKind)99, ChartEncodingChannel.Color));
        Assert.Throws<System.ArgumentException>(() => ChartVisualMap.CreateContinuous(ChartEncodingChannel.X, 0.0, 1.0, ChartPalettesOperations.GrayscaleMap()));
        Assert.Throws<System.ArgumentException>(() => ChartVisualMap.CreateContinuous(ChartEncodingChannel.Color, 1.0, 1.0, ChartPalettesOperations.GrayscaleMap()));
        Assert.Throws<System.ArgumentNullException>(() => ChartVisualMap.CreateContinuous(ChartEncodingChannel.Color, 0.0, 1.0, null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => ChartVisualMap.CreateBinned(ChartEncodingChannel.Color, 0.0, 1.0, 0, ChartPalettesOperations.GrayscaleMap()));
        Assert.Throws<System.ArgumentNullException>(() => ChartVisualMap.CreateThreshold(ChartEncodingChannel.Color, null!, new ChartColor[] { ChartColor.Black() }));
        Assert.Throws<System.ArgumentNullException>(() => ChartVisualMap.CreateThreshold(ChartEncodingChannel.Color, new double[] { 1.0 }, null!));
        Assert.Throws<System.ArgumentException>(() => ChartVisualMap.CreateThreshold(ChartEncodingChannel.Color, new double[] { 2.0, 1.0 }, new ChartColor[] { ChartColor.Black(), ChartColor.White(), ChartColor.Black() }));
        Assert.Throws<System.ArgumentException>(() => ChartVisualMap.CreateThreshold(ChartEncodingChannel.Color, new double[] { 1.0 }, new ChartColor[] { ChartColor.Black() }));
        Assert.Throws<System.ArgumentException>(() => ChartVisualMap.CreateThreshold(ChartEncodingChannel.Color, new double[] { 1.0 }, new ChartColor[] { ChartColor.Black(), null! }));
        Assert.Throws<System.ArgumentException>(() => ChartVisualMap.CreateCategorical(ChartEncodingChannel.Color, new string[] { "" }, ChartPalettesOperations.Categorical()));
        Assert.Throws<System.ArgumentNullException>(() => ChartVisualMap.CreateCategorical(ChartEncodingChannel.Color, new string[] { "A" }, null!));
        Assert.Throws<System.ArgumentNullException>(() => continuous.SetTitle(null!));
        Assert.Throws<System.ArgumentNullException>(() => continuous.BuildLegend(null!));
        Assert.Throws<System.InvalidOperationException>(() => continuous.MapCategory("A"));
        Assert.Throws<System.InvalidOperationException>(() => categorical.MapNumber(1.0));
        Assert.Throws<System.InvalidOperationException>(() => continuous.BinIndex(0.5));
        Assert.Throws<System.InvalidOperationException>(() => continuous.ThresholdIndex(0.5));
        Assert.Throws<System.InvalidOperationException>(() => threshold.CategoryIndex("A"));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => threshold.ThresholdLabel(2));
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo((byte)r));
        Assert.That(color.G, Is.EqualTo((byte)g));
        Assert.That(color.B, Is.EqualTo((byte)b));
        Assert.That(color.A, Is.EqualTo((byte)a));
    }
}
