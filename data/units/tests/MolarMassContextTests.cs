using NUnit.Framework;

namespace units.Tests;

[TestFixture]
public class MolarMassContextTests
{
    [Test]
    public void Constructor_StoresPositiveScaleOnlyMassPerAmountQuantity()
    {
        UnitDefinition gramsPerMole = new StandardUnits().Gram().Divide(new StandardUnits().Mole());
        QuantityValue molarMass = new QuantityValue(18.01528d, gramsPerMole);

        MolarMassContext context = new MolarMassContext(molarMass);
        QuantityValue stored = context.GetMolarMass();
        QuantityValue secondRead = context.GetMolarMass();

        Assert.That(stored.GetValue(), Is.EqualTo(18.01528d));
        Assert.That(stored.GetUnit(), Is.SameAs(gramsPerMole));
        Assert.That(stored, Is.Not.SameAs(molarMass));
        Assert.That(secondRead, Is.Not.SameAs(stored));
    }

    [Test]
    public void Constructor_RejectsMissingOrWrongDimensionQuantity()
    {
        Assert.Throws<System.ArgumentNullException>(() => new MolarMassContext(null!));
        Assert.Throws<System.ArgumentException>(() => new MolarMassContext(new QuantityValue(1.0d, new StandardUnits().Kilogram())));
    }

    [Test]
    public void Constructor_RejectsNonpositiveOrNonfiniteMolarMass()
    {
        UnitDefinition unit = new StandardUnits().Kilogram().Divide(new StandardUnits().Mole());

        Assert.Throws<System.ArgumentException>(() => new MolarMassContext(new QuantityValue(0.0d, unit)));
        Assert.Throws<System.ArgumentException>(() => new MolarMassContext(new QuantityValue(-1.0d, unit)));
        Assert.Throws<System.ArgumentException>(() => new MolarMassContext(new QuantityValue(double.NaN, unit)));
        Assert.Throws<System.ArgumentException>(() => new MolarMassContext(new QuantityValue(double.PositiveInfinity, unit)));
    }

    [Test]
    public void Constructor_RejectsAffineMolarMassUnit()
    {
        UnitDefinition affineUnit = new UnitDefinition(
            "affine_molar_mass",
            "affine molar mass",
            "affine molar masses",
            "amm",
            new string[0],
            new StandardDimensions().Mass().Divide(new StandardDimensions().AmountOfSubstance()),
            new RationalNumber(1, 1),
            new RationalNumber(1, 1),
            false,
            UnitConversionKind.Affine);

        Assert.Throws<System.ArgumentException>(() => new MolarMassContext(new QuantityValue(1.0d, affineUnit)));
    }
}
