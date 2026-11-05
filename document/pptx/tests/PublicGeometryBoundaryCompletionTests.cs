using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PublicGeometryBoundaryCompletionTests
{
    [Test]
    public void UnitsOperations_ConvertEveryPublicUnitFamilyAtBoundaries()
    {
        PptxUnitsOperations units = new PptxUnitsOperations();

        Assert.That(units.InchesToEmu(1.0), Is.EqualTo(914400L));
        Assert.That(units.EmuToInches(914400L), Is.EqualTo(1.0));
        Assert.That(units.PointsToEmu(72.0), Is.EqualTo(914400L));
        Assert.That(units.EmuToPoints(914400L), Is.EqualTo(72.0));
        Assert.That(units.CentimetersToEmu(2.54), Is.EqualTo(914400L));
        Assert.That(units.EmuToCentimeters(914400L), Is.EqualTo(2.54).Within(0.0000001));
        Assert.That(units.PixelsToEmu(96.0, 96.0), Is.EqualTo(914400L));
        Assert.That(units.EmuToPixels(914400L, 96.0), Is.EqualTo(96.0));
        Assert.That(units.EmuToRoundedPixels(914400L, 96.0), Is.EqualTo(96));
        Assert.That(units.DegreesToAngleUnits(90.0), Is.EqualTo(5400000));
        Assert.That(units.AngleUnitsToDegrees(5400000), Is.EqualTo(90.0));
        Assert.That(units.PercentToPercentageUnits(50.0), Is.EqualTo(50000));
        Assert.That(units.PercentageUnitsToPercent(50000), Is.EqualTo(50.0));
        Assert.That(units.FractionToPercentageUnits(0.5), Is.EqualTo(50000));
        Assert.That(units.PercentageUnitsToFraction(50000), Is.EqualTo(0.5));
        Assert.That(units.RoundToLong(-1.5), Is.EqualTo(-2L));
        Assert.That(units.RoundToInt(1.5), Is.EqualTo(2));
        Assert.That(units.RoundToLong(-9223372036854775808.0), Is.EqualTo(long.MinValue));
        Assert.That(units.RoundToInt(2147483647.0), Is.EqualTo(int.MaxValue));
        Assert.That(units.RoundToInt(-2147483648.0), Is.EqualTo(int.MinValue));
    }

    [Test]
    public void UnitsOperations_RejectFiniteValuesOutsideIntegerCapacity()
    {
        PptxUnitsOperations units = new PptxUnitsOperations();

        Assert.Throws<ArgumentOutOfRangeException>(() => units.RoundToLong(9223372036854775808.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => units.RoundToLong(-9223372036854777856.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => units.RoundToInt(2147483648.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => units.RoundToInt(-2147483649.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => units.InchesToEmu(11000000000000.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => units.DegreesToAngleUnits(40000.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => units.PercentToPercentageUnits(3000000.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => units.FractionToPercentageUnits(30000.0));
    }

    [Test]
    public void BoundsAndTransformOperations_RejectScaledCoordinatesOutsideEmuCapacity()
    {
        PptxBoundsOperations bounds = new PptxBoundsOperations();
        PptxTransformOperations transforms = new PptxTransformOperations();
        PptxBounds source = new PptxBounds(1L, 2L, 3L, 4L);

        PptxBounds doubled = bounds.Scale(source, 2.0, 3.0);

        Assert.That(doubled.X, Is.EqualTo(2L));
        Assert.That(doubled.Y, Is.EqualTo(6L));
        Assert.That(doubled.Width, Is.EqualTo(6L));
        Assert.That(doubled.Height, Is.EqualTo(12L));
        Assert.Throws<ArgumentOutOfRangeException>(() => bounds.Scale(source, double.MaxValue, 1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => transforms.ScaleCoordinate(long.MaxValue, 2L, 1L));
        Assert.That(transforms.ScaleCoordinate(long.MinValue, 1L, 1L), Is.EqualTo(long.MinValue));
    }
}
