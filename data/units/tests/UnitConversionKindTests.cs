using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class UnitConversionKindTests
{
    [Test]
    public void UnitConversionKind_DefinesExpectedConversionCategories()
    {
        UnitConversionKind[] values = Enum.GetValues<UnitConversionKind>();

        Assert.That(values, Does.Contain(UnitConversionKind.Unsupported));
        Assert.That(values, Does.Contain(UnitConversionKind.Scale));
        Assert.That(values, Does.Contain(UnitConversionKind.Affine));
        Assert.That(values, Does.Contain(UnitConversionKind.Special));
    }

    [Test]
    public void UnitConversionKind_UsesStableValuesWithUnsupportedAsDefault()
    {
        Assert.That((int)UnitConversionKind.Unsupported, Is.EqualTo(0));
        Assert.That((int)UnitConversionKind.Scale, Is.EqualTo(1));
        Assert.That((int)UnitConversionKind.Affine, Is.EqualTo(2));
        Assert.That((int)UnitConversionKind.Special, Is.EqualTo(3));
    }
}
