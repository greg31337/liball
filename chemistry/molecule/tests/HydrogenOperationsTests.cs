using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class HydrogenOperationsTests
{
    [Test]
    public void AddHydrogens_ConvertsImplicitHydrogensToExplicitAtomsAndBonds()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule molecule = builder.Build();
        HydrogenCalculator hydrogens = new HydrogenCalculator();
        FormulaCalculator formula = new FormulaCalculator();

        int added = hydrogens.AddHydrogens(molecule);

        Assert.That(added, Is.EqualTo(4));
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(5));
        Assert.That(molecule.GetBondCount(), Is.EqualTo(4));
        Assert.That(molecule.Atoms[carbon].ImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(molecule.GetDegree(carbon), Is.EqualTo(4));
        Assert.That(formula.Formula(molecule), Is.EqualTo("CH4"));

        int atomIndex = 1;
        while (atomIndex < molecule.Atoms.Count)
        {
            Assert.That(molecule.Atoms[atomIndex].AtomicNumber, Is.EqualTo(1));
            Assert.That(molecule.Bonds[atomIndex - 1].BeginAtomId, Is.EqualTo(carbon));
            Assert.That(molecule.Bonds[atomIndex - 1].EndAtomId, Is.EqualTo(atomIndex));
            atomIndex = atomIndex + 1;
        }
    }

    [Test]
    public void AddCalculatedHydrogens_AssignsThenExpandsImplicitHydrogens()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        Molecule molecule = builder.Build();
        HydrogenCalculator hydrogens = new HydrogenCalculator();
        FormulaCalculator formula = new FormulaCalculator();

        int added = hydrogens.AddCalculatedHydrogens(molecule);

        Assert.That(added, Is.EqualTo(2));
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(4));
        Assert.That(molecule.GetBondCount(), Is.EqualTo(3));
        Assert.That(molecule.Atoms[carbon].ImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(molecule.Atoms[oxygen].ImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(formula.Formula(molecule), Is.EqualTo("CH2O"));
    }

    [Test]
    public void RemoveHydrogens_PreservesIsotopicHydrogensWhenRequested()
    {
        Molecule molecule = BuildCarbonWithHydrogenAndDeuterium();
        HydrogenCalculator hydrogens = new HydrogenCalculator();
        FormulaCalculator formula = new FormulaCalculator();

        int removed = hydrogens.RemoveHydrogens(molecule, true);

        Assert.That(removed, Is.EqualTo(1));
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(2));
        Assert.That(molecule.GetBondCount(), Is.EqualTo(1));
        Assert.That(molecule.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(molecule.Atoms[0].ImplicitHydrogenCount, Is.EqualTo(1));
        Assert.That(molecule.Atoms[1].AtomicNumber, Is.EqualTo(1));
        Assert.That(molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(2));
        Assert.That(molecule.Bonds[0].BeginAtomId, Is.EqualTo(0));
        Assert.That(molecule.Bonds[0].EndAtomId, Is.EqualTo(1));
        Assert.That(formula.Formula(molecule), Is.EqualTo("CH2"));
        Assert.That(formula.IsotopeFormula(molecule), Is.EqualTo("CH[2H]"));
    }

    [Test]
    public void RemoveHydrogens_RemovesIsotopicHydrogensWhenNotPreserved()
    {
        Molecule molecule = BuildCarbonWithHydrogenAndDeuterium();
        HydrogenCalculator hydrogens = new HydrogenCalculator();
        FormulaCalculator formula = new FormulaCalculator();

        int removed = hydrogens.RemoveHydrogens(molecule, false);

        Assert.That(removed, Is.EqualTo(2));
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(1));
        Assert.That(molecule.GetBondCount(), Is.EqualTo(0));
        Assert.That(molecule.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(molecule.Atoms[0].ImplicitHydrogenCount, Is.EqualTo(2));
        Assert.That(formula.Formula(molecule), Is.EqualTo("CH2"));
    }

    [Test]
    public void RemoveHydrogens_DoesNotRemoveChargedOrUnattachedHydrogens()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int chargedHydrogen = builder.AddAtom(new AtomSpec(1, 0, 1, 0, 0, 0, 0, false, "", "H+"));
        int hydrogenA = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        int hydrogenB = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        builder.AddBond(hydrogenA, hydrogenB, new BondSpec());
        Molecule molecule = builder.Build();
        HydrogenCalculator hydrogens = new HydrogenCalculator();

        int removed = hydrogens.RemoveHydrogens(molecule, false);

        Assert.That(removed, Is.EqualTo(0));
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(3));
        Assert.That(molecule.GetBondCount(), Is.EqualTo(1));
        Assert.That(molecule.Atoms[chargedHydrogen].FormalCharge, Is.EqualTo(1));
    }

    static Molecule BuildCarbonWithHydrogenAndDeuterium()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int hydrogen = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        int deuterium = builder.AddAtom(new AtomSpec(1, 2, 0, 0, 0, 0, 0, false, "", "D"));
        builder.AddBond(carbon, hydrogen, new BondSpec());
        builder.AddBond(carbon, deuterium, new BondSpec());
        return builder.Build();
    }
}
