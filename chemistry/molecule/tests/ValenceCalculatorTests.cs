using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class ValenceCalculatorTests
{
    [Test]
    public void CalculateAtomValence_AllowsNormalNeutralValence()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        ValenceCalculator calculator = new ValenceCalculator();

        ValenceResult result = calculator.CalculateAtomValence(builder.Build(), carbon);

        Assert.That(result.AtomId, Is.EqualTo(carbon));
        Assert.That(result.AtomicNumber, Is.EqualTo(6));
        Assert.That(result.BondValence, Is.EqualTo(0));
        Assert.That(result.HydrogenValence, Is.EqualTo(4));
        Assert.That(result.TotalValence, Is.EqualTo(4));
        Assert.That(result.IsTableAllowed, Is.True);
        Assert.That(result.IsAllowed, Is.True);
    }

    [Test]
    public void CalculateAtomValence_AllowsCommonChargedValence()
    {
        MoleculeBuilder ammoniumBuilder = new MoleculeBuilder();
        int nitrogen = ammoniumBuilder.AddAtom(new AtomSpec(7, 0, 1, 0, 4, 0, 0, false, "", "N"));

        MoleculeBuilder hydroxideBuilder = new MoleculeBuilder();
        int oxygen = hydroxideBuilder.AddAtom(new AtomSpec(8, 0, -1, 0, 1, 0, 0, false, "", "O"));

        ValenceCalculator calculator = new ValenceCalculator();
        ValenceResult ammonium = calculator.CalculateAtomValence(ammoniumBuilder.Build(), nitrogen);
        ValenceResult hydroxide = calculator.CalculateAtomValence(hydroxideBuilder.Build(), oxygen);

        Assert.That(ammonium.TotalValence, Is.EqualTo(4));
        Assert.That(ammonium.IsChargeAdjustedAllowed, Is.True);
        Assert.That(ammonium.IsAllowed, Is.True);
        Assert.That(hydroxide.TotalValence, Is.EqualTo(1));
        Assert.That(hydroxide.IsChargeAdjustedAllowed, Is.True);
        Assert.That(hydroxide.IsAllowed, Is.True);
    }

    [Test]
    public void CalculateAtomValence_AllowsRadicalAdjustedValence()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 1, 0, false, "", "C"));
        ValenceCalculator calculator = new ValenceCalculator();

        ValenceResult result = calculator.CalculateAtomValence(builder.Build(), carbon);

        Assert.That(result.TotalValence, Is.EqualTo(3));
        Assert.That(result.ElectronValence, Is.EqualTo(4));
        Assert.That(result.RadicalElectronCount, Is.EqualTo(1));
        Assert.That(result.IsRadicalAdjustedAllowed, Is.True);
        Assert.That(result.IsAllowed, Is.True);
    }

    [Test]
    public void CalculateAtomValence_AllowsAromaticValence()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int center = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, true, "", "c"));
        int left = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, true, "", "c"));
        int right = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, true, "", "c"));
        builder.AddBond(center, left, BondSpec.Create(BondOrder.Aromatic, true, BondStereo.None, BondDirection.None));
        builder.AddBond(center, right, BondSpec.Create(BondOrder.Aromatic, true, BondStereo.None, BondDirection.None));
        ValenceCalculator calculator = new ValenceCalculator();

        ValenceResult result = calculator.CalculateAtomValence(builder.Build(), center);

        Assert.That(result.BondValence, Is.EqualTo(2));
        Assert.That(result.HydrogenValence, Is.EqualTo(1));
        Assert.That(result.TotalValence, Is.EqualTo(3));
        Assert.That(result.AromaticBondCount, Is.EqualTo(2));
        Assert.That(result.IsAromaticAllowed, Is.True);
        Assert.That(result.IsAllowed, Is.True);
    }

    [Test]
    public void CalculateAtomValence_TracksDativeBondsWithoutAddingOrdinaryValence()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 3, 0, 0, false, "", "N"));
        int iron = builder.AddAtom(new AtomSpec(26, 0, 0, 0, 0, 0, 0, false, "", "Fe"));
        BondSpec dative = BondSpec.Create(BondOrder.Dative, false, BondStereo.None, BondDirection.None);
        dative.IsDative = true;
        builder.AddBond(nitrogen, iron, dative);
        ValenceCalculator calculator = new ValenceCalculator();

        ValenceResult result = calculator.CalculateAtomValence(builder.Build(), nitrogen);

        Assert.That(result.DativeBondCount, Is.EqualTo(1));
        Assert.That(result.BondValence, Is.EqualTo(0));
        Assert.That(result.HydrogenValence, Is.EqualTo(3));
        Assert.That(result.TotalValence, Is.EqualTo(3));
        Assert.That(result.IsAllowed, Is.True);
        Assert.That(calculator.GetBondValence(builder.Build().Bonds[0]), Is.EqualTo(0));
    }

    [Test]
    public void CalculateAtomValence_AllowsHypervalentTableCases()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int sulfur = builder.AddAtom(new AtomSpec(16, 0, 0, 0, 0, 0, 0, false, "", "S"));
        int index = 0;
        while (index < 6)
        {
            int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
            builder.AddBond(sulfur, fluorine, new BondSpec());
            index = index + 1;
        }

        ValenceCalculator calculator = new ValenceCalculator();
        ValenceResult result = calculator.CalculateAtomValence(builder.Build(), sulfur);

        Assert.That(result.TotalValence, Is.EqualTo(6));
        Assert.That(result.MaximumKnownValence, Is.EqualTo(6));
        Assert.That(result.IsHypervalentAllowed, Is.True);
        Assert.That(result.IsAllowed, Is.True);
    }

    [Test]
    public void CalculateAtomValence_RejectsUnsupportedValence()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 5, 0, 0, false, "", "C"));
        ValenceCalculator calculator = new ValenceCalculator();

        ValenceResult result = calculator.CalculateAtomValence(builder.Build(), carbon);

        Assert.That(result.TotalValence, Is.EqualTo(5));
        Assert.That(result.IsAllowed, Is.False);
        Assert.That(calculator.CalculateAtomValence(builder.Build(), 99).IsAllowed, Is.False);
    }
}
