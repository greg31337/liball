using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class StandardDimensionsTests
{
    [Test]
    public void Dimensionless_ReturnsDimensionlessVector()
    {
        Assert.That(new StandardDimensions().Dimensionless().IsDimensionless(), Is.True);
    }

    [Test]
    public void BaseDimensionFactories_ReturnExpectedDimensionVectors()
    {
        Assert.That(new StandardDimensions().Length().EqualsDimension(DimensionVector.Length()), Is.True);
        Assert.That(new StandardDimensions().Mass().EqualsDimension(DimensionVector.Mass()), Is.True);
        Assert.That(new StandardDimensions().Time().EqualsDimension(DimensionVector.Time()), Is.True);
        Assert.That(new StandardDimensions().ElectricCurrent().EqualsDimension(DimensionVector.ElectricCurrent()), Is.True);
        Assert.That(new StandardDimensions().ThermodynamicTemperature().EqualsDimension(DimensionVector.ThermodynamicTemperature()), Is.True);
        Assert.That(new StandardDimensions().AmountOfSubstance().EqualsDimension(DimensionVector.AmountOfSubstance()), Is.True);
        Assert.That(new StandardDimensions().LuminousIntensity().EqualsDimension(DimensionVector.LuminousIntensity()), Is.True);
        Assert.That(new StandardDimensions().PlaneAngle().EqualsDimension(DimensionVector.PlaneAngle()), Is.True);
        Assert.That(new StandardDimensions().SolidAngle().EqualsDimension(DimensionVector.SolidAngle()), Is.True);
        Assert.That(new StandardDimensions().Information().EqualsDimension(DimensionVector.Information()), Is.True);
        Assert.That(new StandardDimensions().Currency().EqualsDimension(DimensionVector.Currency()), Is.True);
        Assert.That(new StandardDimensions().Count().EqualsDimension(DimensionVector.Count()), Is.True);
    }

    [Test]
    public void BaseDimensionFactories_ReturnIndependentInstances()
    {
        DimensionVector first = new StandardDimensions().Length();
        DimensionVector second = new StandardDimensions().Length();

        Assert.That(first, Is.Not.SameAs(second));
        Assert.That(first.EqualsDimension(second), Is.True);
    }
}
