using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxUnitsTests
{
    [Test]
    public void InchPointAndCentimeterConversionsUseOpenXmlEmuConstants()
    {
        Assert.That(PptxUnitsCore.InchesToEmu(1.0), Is.EqualTo(914400L));
        Assert.That(PptxUnitsCore.EmuToInches(457200L), Is.EqualTo(0.5).Within(0.000001));
        Assert.That(PptxUnitsCore.PointsToEmu(72.0), Is.EqualTo(914400L));
        Assert.That(PptxUnitsCore.EmuToPoints(12700L), Is.EqualTo(1.0).Within(0.000001));
        Assert.That(PptxUnitsCore.CentimetersToEmu(2.54), Is.EqualTo(914400L));
        Assert.That(PptxUnitsCore.EmuToCentimeters(914400L), Is.EqualTo(2.54).Within(0.000001));
    }

    [Test]
    public void PixelConversionsUseProvidedDpi()
    {
        Assert.That(PptxUnitsCore.PixelsToEmu(96.0, 96.0), Is.EqualTo(914400L));
        Assert.That(PptxUnitsCore.PixelsToEmu(300.0, 300.0), Is.EqualTo(914400L));
        Assert.That(PptxUnitsCore.EmuToPixels(914400L, 96.0), Is.EqualTo(96.0).Within(0.000001));
        Assert.That(PptxUnitsCore.EmuToRoundedPixels(457200L, 96.0), Is.EqualTo(48));
    }

    [Test]
    public void AngleConversionsUseSixtyThousandUnitsPerDegree()
    {
        Assert.That(PptxUnitsCore.DegreesToAngleUnits(90.0), Is.EqualTo(5400000));
        Assert.That(PptxUnitsCore.DegreesToAngleUnits(-45.0), Is.EqualTo(-2700000));
        Assert.That(PptxUnitsCore.AngleUnitsToDegrees(1800000), Is.EqualTo(30.0).Within(0.000001));
    }

    [Test]
    public void PercentageConversionsUseOpenXmlPercentageUnits()
    {
        Assert.That(PptxUnitsCore.PercentToPercentageUnits(100.0), Is.EqualTo(100000));
        Assert.That(PptxUnitsCore.PercentToPercentageUnits(12.5), Is.EqualTo(12500));
        Assert.That(PptxUnitsCore.PercentageUnitsToPercent(25000), Is.EqualTo(25.0).Within(0.000001));
        Assert.That(PptxUnitsCore.FractionToPercentageUnits(1.0), Is.EqualTo(100000));
        Assert.That(PptxUnitsCore.FractionToPercentageUnits(0.25), Is.EqualTo(25000));
        Assert.That(PptxUnitsCore.PercentageUnitsToFraction(50000), Is.EqualTo(0.5).Within(0.000001));
    }

    [Test]
    public void ConversionsRoundAwayFromZero()
    {
        Assert.That(PptxUnitsCore.InchesToEmu(0.000000547025), Is.EqualTo(1L));
        Assert.That(PptxUnitsCore.InchesToEmu(-0.000000547025), Is.EqualTo(-1L));
        Assert.That(PptxUnitsCore.DegreesToAngleUnits(0.00000833334), Is.EqualTo(1));
        Assert.That(PptxUnitsCore.PercentToPercentageUnits(0.0005), Is.EqualTo(1));
    }

    [Test]
    public void PixelConversionsRejectInvalidDpi()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => PptxUnitsCore.PixelsToEmu(10.0, 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => PptxUnitsCore.EmuToPixels(10L, -96.0));
        Assert.Throws<System.ArgumentException>(() => PptxUnitsCore.PixelsToEmu(10.0, double.NaN));
        Assert.Throws<System.ArgumentException>(() => PptxUnitsCore.EmuToPixels(10L, double.PositiveInfinity));
    }
}
