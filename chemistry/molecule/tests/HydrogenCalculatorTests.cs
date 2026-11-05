using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class HydrogenCalculatorTests
{
    [Test]
    public void CalculateImplicitHydrogenCount_FillsNeutralOrganicValence()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddBond(carbonA, carbonB, new BondSpec());
        HydrogenCalculator calculator = new HydrogenCalculator();

        HydrogenCalculationResult resultA = calculator.CalculateAtomHydrogens(builder.Build(), carbonA);
        HydrogenCalculationResult resultB = calculator.CalculateAtomHydrogens(builder.Build(), carbonB);

        Assert.That(resultA.BondValence, Is.EqualTo(1));
        Assert.That(resultA.TargetValence, Is.EqualTo(4));
        Assert.That(resultA.CalculatedImplicitHydrogenCount, Is.EqualTo(3));
        Assert.That(resultB.CalculatedImplicitHydrogenCount, Is.EqualTo(3));
    }

    [Test]
    public void CalculateImplicitHydrogenCount_HandlesDoubleBonds()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        HydrogenCalculator calculator = new HydrogenCalculator();

        Assert.That(calculator.CalculateImplicitHydrogenCount(builder.Build(), carbon), Is.EqualTo(2));
        Assert.That(calculator.CalculateImplicitHydrogenCount(builder.Build(), oxygen), Is.EqualTo(0));
    }

    [Test]
    public void CalculateImplicitHydrogenCount_UsesChargedTargets()
    {
        MoleculeBuilder ammonium = new MoleculeBuilder();
        int nitrogen = ammonium.AddAtom(new AtomSpec(7, 0, 1, 0, 0, 0, 0, false, "", "N"));

        MoleculeBuilder hydroxide = new MoleculeBuilder();
        int oxygen = hydroxide.AddAtom(new AtomSpec(8, 0, -1, 0, 0, 0, 0, false, "", "O"));

        MoleculeBuilder chloride = new MoleculeBuilder();
        int chlorine = chloride.AddAtom(new AtomSpec(17, 0, -1, 0, 0, 0, 0, false, "", "Cl"));

        HydrogenCalculator calculator = new HydrogenCalculator();

        Assert.That(calculator.CalculateImplicitHydrogenCount(ammonium.Build(), nitrogen), Is.EqualTo(4));
        Assert.That(calculator.CalculateImplicitHydrogenCount(hydroxide.Build(), oxygen), Is.EqualTo(1));
        Assert.That(calculator.CalculateImplicitHydrogenCount(chloride.Build(), chlorine), Is.EqualTo(0));
    }

    [Test]
    public void CalculateImplicitHydrogenCount_UsesRadicalAdjustedTargets()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 1, 0, false, "", "C"));
        HydrogenCalculator calculator = new HydrogenCalculator();

        HydrogenCalculationResult result = calculator.CalculateAtomHydrogens(builder.Build(), carbon);

        Assert.That(result.TargetValence, Is.EqualTo(3));
        Assert.That(result.CalculatedImplicitHydrogenCount, Is.EqualTo(3));
    }

    [Test]
    public void CalculateImplicitHydrogenCount_UsesAromaticTargets()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int center = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, true, "", "c"));
        int left = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, true, "", "c"));
        int right = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, true, "", "c"));
        builder.AddBond(center, left, BondSpec.Create(BondOrder.Aromatic, true, BondStereo.None, BondDirection.None));
        builder.AddBond(center, right, BondSpec.Create(BondOrder.Aromatic, true, BondStereo.None, BondDirection.None));
        HydrogenCalculator calculator = new HydrogenCalculator();

        HydrogenCalculationResult result = calculator.CalculateAtomHydrogens(builder.Build(), center);

        Assert.That(result.BondValence, Is.EqualTo(2));
        Assert.That(result.TargetValence, Is.EqualTo(3));
        Assert.That(result.CalculatedImplicitHydrogenCount, Is.EqualTo(1));
    }

    [Test]
    public void CalculateAtomHydrogens_CountsSpecAndGraphExplicitHydrogensSeparately()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 1, 0, 0, 0, false, "", "C"));
        int hydrogen = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        builder.AddBond(carbon, hydrogen, new BondSpec());
        HydrogenCalculator calculator = new HydrogenCalculator();

        HydrogenCalculationResult result = calculator.CalculateAtomHydrogens(builder.Build(), carbon);

        Assert.That(result.SpecExplicitHydrogenCount, Is.EqualTo(1));
        Assert.That(result.GraphExplicitHydrogenCount, Is.EqualTo(1));
        Assert.That(result.BondValence, Is.EqualTo(1));
        Assert.That(result.TargetValence, Is.EqualTo(4));
        Assert.That(result.CalculatedImplicitHydrogenCount, Is.EqualTo(2));
        Assert.That(result.TotalHydrogenCount, Is.EqualTo(4));
    }

    [Test]
    public void AssignImplicitHydrogens_WritesCalculatedValuesToAtoms()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, new BondSpec());
        Molecule molecule = builder.Build();
        HydrogenCalculator calculator = new HydrogenCalculator();

        calculator.AssignImplicitHydrogens(molecule);

        Assert.That(molecule.Atoms[carbon].ImplicitHydrogenCount, Is.EqualTo(3));
        Assert.That(molecule.Atoms[oxygen].ImplicitHydrogenCount, Is.EqualTo(1));
    }
}
