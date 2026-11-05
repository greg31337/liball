namespace chart.Tests;

[TestFixture]
public class ChartPalettesTests
{
    [Test]
    public void ChartPalette_CyclesIndexesAndReturnsCopies()
    {
        ChartColor[] colors = new ChartColor[]
        {
            new ChartColor(10, 20, 30, 255),
            new ChartColor(40, 50, 60, 255)
        };

        ChartPalette palette = new ChartPalette("sample", colors);
        colors[0].R = 200;

        ChartColor first = palette.GetColor(0);
        ChartColor wrapped = palette.GetColor(2);
        ChartColor negative = palette.GetColor(-1);
        first.R = 99;

        Assert.That(palette.Name, Is.EqualTo("sample"));
        Assert.That(palette.Count(), Is.EqualTo(2));
        AssertColor(first, 99, 20, 30, 255);
        AssertColor(wrapped, 10, 20, 30, 255);
        AssertColor(negative, 40, 50, 60, 255);
        AssertColor(palette.GetColor(0), 10, 20, 30, 255);
    }

    [Test]
    public void ChartPalette_CopyColorsReturnsIndependentColors()
    {
        ChartPalette palette = ChartPalettesOperations.Categorical();
        ChartColor[] copy = palette.CopyColors();

        copy[0].R = 0;

        Assert.That(palette.Count(), Is.EqualTo(10));
        AssertColor(palette.GetColor(0), 31, 119, 180, 255);
        AssertColor(copy[0], 0, 119, 180, 255);
    }

    [Test]
    public void BuiltInPalettes_ProvideRequiredOutputFamilies()
    {
        ChartPalette categorical = ChartPalettesOperations.Categorical();
        ChartPalette grayscale = ChartPalettesOperations.Grayscale();
        ChartPalette colorblindSafe = ChartPalettesOperations.ColorblindSafe();
        ChartPalette highContrast = ChartPalettesOperations.HighContrast();

        Assert.That(categorical.Name, Is.EqualTo("categorical"));
        Assert.That(categorical.Count(), Is.EqualTo(10));
        Assert.That(grayscale.Name, Is.EqualTo("grayscale"));
        Assert.That(grayscale.Count(), Is.EqualTo(5));
        Assert.That(colorblindSafe.Name, Is.EqualTo("colorblind-safe"));
        Assert.That(colorblindSafe.Count(), Is.EqualTo(8));
        Assert.That(highContrast.Name, Is.EqualTo("high-contrast"));
        Assert.That(highContrast.Count(), Is.EqualTo(8));
        AssertColor(colorblindSafe.GetColor(0), 0, 114, 178, 255);
        AssertColor(highContrast.GetColor(2), 255, 255, 0, 255);
    }

    [Test]
    public void ChartColorMap_InterpolatesAndClampsStops()
    {
        ChartColorMap map = new ChartColorMap(
            "gray",
            new ChartColor[]
            {
                ChartColor.Black(),
                ChartColor.White()
            });

        Assert.That(map.Name, Is.EqualTo("gray"));
        Assert.That(map.Count(), Is.EqualTo(2));
        AssertColor(map.GetColor(-1.0), 0, 0, 0, 255);
        AssertColor(map.GetColor(0.0), 0, 0, 0, 255);
        AssertColor(map.GetColor(0.5), 128, 128, 128, 255);
        AssertColor(map.GetColor(1.0), 255, 255, 255, 255);
        AssertColor(map.GetColor(2.0), 255, 255, 255, 255);
    }

    [Test]
    public void ChartColorMap_UsesPiecewiseInterpolationAcrossMultipleStops()
    {
        ChartColorMap map = new ChartColorMap(
            "three",
            new ChartColor[]
            {
                new ChartColor(0, 0, 0, 255),
                new ChartColor(100, 0, 0, 255),
                new ChartColor(100, 100, 0, 255)
            });

        AssertColor(map.GetColor(0.25), 50, 0, 0, 255);
        AssertColor(map.GetColor(0.75), 100, 50, 0, 255);
        AssertColor(map.GetStop(1), 100, 0, 0, 255);
    }

    [Test]
    public void ChartColorMap_ReverseCreatesIndependentMap()
    {
        ChartColorMap map = ChartPalettesOperations.GrayscaleMap();
        ChartColorMap reversed = map.Reverse("gray-reversed");
        ChartColor first = reversed.GetStop(0);
        first.R = 0;

        Assert.That(reversed.Name, Is.EqualTo("gray-reversed"));
        AssertColor(reversed.GetStop(0), 255, 255, 255, 255);
        AssertColor(reversed.GetStop(1), 0, 0, 0, 255);
        AssertColor(map.GetStop(0), 0, 0, 0, 255);
    }

    [Test]
    public void BuiltInColorMaps_ProvideSequentialDivergingGrayscaleColorblindAndHighContrast()
    {
        ChartColorMap sequential = ChartPalettesOperations.Sequential();
        ChartColorMap diverging = ChartPalettesOperations.Diverging();
        ChartColorMap grayscale = ChartPalettesOperations.GrayscaleMap();
        ChartColorMap colorblindSafe = ChartPalettesOperations.ColorblindSafeMap();
        ChartColorMap highContrast = ChartPalettesOperations.HighContrastMap();

        Assert.That(sequential.Name, Is.EqualTo("sequential"));
        Assert.That(sequential.Count(), Is.EqualTo(5));
        Assert.That(diverging.Name, Is.EqualTo("diverging"));
        Assert.That(diverging.Count(), Is.EqualTo(5));
        Assert.That(grayscale.Name, Is.EqualTo("grayscale"));
        Assert.That(grayscale.Count(), Is.EqualTo(2));
        Assert.That(colorblindSafe.Name, Is.EqualTo("colorblind-safe"));
        Assert.That(colorblindSafe.Count(), Is.EqualTo(3));
        Assert.That(highContrast.Name, Is.EqualTo("high-contrast"));
        Assert.That(highContrast.Count(), Is.EqualTo(3));

        AssertColor(sequential.GetColor(0.0), 247, 251, 255, 255);
        AssertColor(diverging.GetColor(0.5), 247, 247, 247, 255);
        AssertColor(grayscale.GetColor(0.5), 128, 128, 128, 255);
        AssertColor(colorblindSafe.GetColor(1.0), 0, 114, 178, 255);
        AssertColor(highContrast.GetColor(0.5), 255, 255, 0, 255);
    }

    static void AssertColor(ChartColor color, int r, int g, int b, int a)
    {
        Assert.That(color.R, Is.EqualTo((byte)r));
        Assert.That(color.G, Is.EqualTo((byte)g));
        Assert.That(color.B, Is.EqualTo((byte)b));
        Assert.That(color.A, Is.EqualTo((byte)a));
    }
}
