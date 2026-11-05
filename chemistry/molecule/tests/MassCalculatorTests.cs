using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class MassCalculatorTests
{
    [Test]
    public void MassCalculator_CalculatesAverageExactAndNominalMasses()
    {
        Molecule molecule = BuildEthanol();
        MassCalculator calculator = new MassCalculator();

        Assert.That(calculator.MolecularWeight(molecule), Is.EqualTo(46.069).Within(0.001));
        Assert.That(calculator.ExactMass(molecule), Is.EqualTo(46.041865).Within(0.000001));
        Assert.That(calculator.NominalMass(molecule), Is.EqualTo(46));
    }

    [Test]
    public void MassCalculator_UsesExactIsotopeMassesForSpecifiedIsotopes()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 13, 0, 0, 4, 0, 0, false, "", "C13"));
        builder.AddAtom(new AtomSpec(1, 2, 0, 0, 0, 0, 0, false, "", "D"));
        builder.AddAtom(new AtomSpec(8, 18, 0, 0, 0, 0, 0, false, "", "O18"));
        Molecule molecule = builder.Build();
        MassCalculator calculator = new MassCalculator();

        Assert.That(calculator.MolecularWeight(molecule), Is.EqualTo(37.048617).Within(0.000001));
        Assert.That(calculator.ExactMass(molecule), Is.EqualTo(37.047917).Within(0.000001));
        Assert.That(calculator.NominalMass(molecule), Is.EqualTo(37));
    }

    [Test]
    public void BasicDescriptors_CalculatesFormulaMassesAndAtomCounts()
    {
        Molecule molecule = BuildEthanol();
        BasicDescriptors descriptors = new BasicDescriptors();

        DescriptorSet set = descriptors.Calculate(molecule);

        Assert.That(set.Formula, Is.EqualTo("C2H6O"));
        Assert.That(set.MolecularWeight, Is.EqualTo(46.069).Within(0.001));
        Assert.That(set.ExactMass, Is.EqualTo(46.041865).Within(0.000001));
        Assert.That(set.NominalMass, Is.EqualTo(46));
        Assert.That(set.ExplicitAtomCount, Is.EqualTo(3));
        Assert.That(set.AtomCount, Is.EqualTo(9));
        Assert.That(set.HeavyAtomCount, Is.EqualTo(3));
        Assert.That(set.HydrogenAtomCount, Is.EqualTo(6));
        Assert.That(set.HeteroAtomCount, Is.EqualTo(1));
        Assert.That(set.CarbonAtomCount, Is.EqualTo(2));
        Assert.That(set.FormalCharge, Is.EqualTo(0));
    }

    [Test]
    public void BasicDescriptors_CountsExplicitHydrogenAtomsAndFormalCharge()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(7, 0, 1, 0, 4, 0, 0, false, "", "N"));
        builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        Molecule molecule = builder.Build();
        BasicDescriptors descriptors = new BasicDescriptors();

        DescriptorSet set = descriptors.Calculate(molecule);

        Assert.That(set.Formula, Is.EqualTo("H5N"));
        Assert.That(set.ExplicitAtomCount, Is.EqualTo(2));
        Assert.That(set.AtomCount, Is.EqualTo(6));
        Assert.That(set.HeavyAtomCount, Is.EqualTo(1));
        Assert.That(set.HydrogenAtomCount, Is.EqualTo(5));
        Assert.That(set.HeteroAtomCount, Is.EqualTo(1));
        Assert.That(set.FormalCharge, Is.EqualTo(1));
    }

    [Test]
    public void MassAndDescriptorCalculators_ReturnZeroValuesForNullOrUnknownMolecules()
    {
        MassCalculator masses = new MassCalculator();
        BasicDescriptors descriptors = new BasicDescriptors();
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(999, 0, 0, 0, 0, 0, 0, false, "", "Xx"));

        Assert.That(masses.MolecularWeight(null), Is.EqualTo(0.0));
        Assert.That(masses.ExactMass(null), Is.EqualTo(0.0));
        Assert.That(masses.NominalMass(null), Is.EqualTo(0));
        Assert.That(masses.MolecularWeight(builder.Build()), Is.EqualTo(0.0));
        Assert.That(masses.ExactMass(builder.Build()), Is.EqualTo(0.0));
        Assert.That(masses.NominalMass(builder.Build()), Is.EqualTo(0));

        DescriptorSet empty = descriptors.Calculate(null);
        Assert.That(empty.Formula, Is.EqualTo(""));
        Assert.That(empty.AtomCount, Is.EqualTo(0));
        Assert.That(empty.MolecularWeight, Is.EqualTo(0.0));
    }

    [Test]
    public void MassCalculator_UsesMassNumberFallbackForUnknownIsotope()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 99, 0, 0, 0, 0, 0, false, "", "C"));
        Molecule molecule = builder.Build();
        MassCalculator calculator = new MassCalculator();

        Assert.That(calculator.MolecularWeight(molecule), Is.EqualTo(99.0));
        Assert.That(calculator.ExactMass(molecule), Is.EqualTo(99.0));
        Assert.That(calculator.NominalMass(molecule), Is.EqualTo(99));
    }

    static Molecule BuildEthanol()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        return builder.Build();
    }
}
