using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class SmilesWriterTests
{
    [Test]
    public void Write_PreservesInputOrderForChain()
    {
        Molecule molecule = BuildOxygenCarbonNitrogenChain();
        SmilesWriter writer = new SmilesWriter();

        string smiles = writer.Write(molecule);

        Assert.That(smiles, Is.EqualTo("OCN"));
    }

    [Test]
    public void Write_PreservesInputOrderForDisconnectedComponents()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", "O"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        SmilesWriter writer = new SmilesWriter();

        string smiles = writer.Write(builder.Build());

        Assert.That(smiles, Is.EqualTo("O.C"));
    }

    [Test]
    public void Write_PreservesBondInsertionOrderForBranches()
    {
        Molecule molecule = BuildInputOrderedBranch();
        SmilesWriter writer = new SmilesWriter();

        string smiles = writer.Write(molecule);

        Assert.That(smiles, Is.EqualTo("C(O)(N)F"));
    }

    [Test]
    public void Write_DiffersWhenInputAtomOrderDiffers()
    {
        Molecule forward = BuildOxygenCarbonNitrogenChain();
        Molecule reversed = BuildNitrogenCarbonOxygenChain();
        SmilesWriter writer = new SmilesWriter();

        string forwardSmiles = writer.Write(forward);
        string reversedSmiles = writer.Write(reversed);

        Assert.That(forwardSmiles, Is.Not.EqualTo(reversedSmiles));
        Assert.That(reversedSmiles, Is.EqualTo("NCO"));
    }

    [Test]
    public void WriteNoncanonicalAliasesWrite()
    {
        Molecule molecule = BuildOxygenCarbonNitrogenChain();
        SmilesWriter writer = new SmilesWriter();

        Assert.That(writer.WriteNoncanonical(molecule), Is.EqualTo(writer.Write(molecule)));
    }

    [Test]
    public void WriteIsomeric_PreservesInputOrderAndStereoMarkers()
    {
        Molecule molecule = BuildDirectedInputOrderBond();
        SmilesWriter writer = new SmilesWriter();

        string nonIsomeric = writer.Write(molecule);
        string isomeric = writer.WriteIsomeric(molecule);

        Assert.That(nonIsomeric, Is.EqualTo("CN"));
        Assert.That(isomeric, Is.EqualTo("C/N"));
        Assert.That(writer.WriteNoncanonicalIsomeric(molecule), Is.EqualTo(isomeric));
    }

    [Test]
    public void Write_ReturnsEmptyStringForNullOrEmptyMolecule()
    {
        SmilesWriter writer = new SmilesWriter();

        Assert.That(writer.Write(null), Is.EqualTo(""));
        Assert.That(writer.Write(new Molecule()), Is.EqualTo(""));
    }

    static Molecule BuildOxygenCarbonNitrogenChain()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        builder.AddBond(oxygen, carbon, new BondSpec());
        builder.AddBond(carbon, nitrogen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildNitrogenCarbonOxygenChain()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(nitrogen, carbon, new BondSpec());
        builder.AddBond(carbon, oxygen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildInputOrderedBranch()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        builder.AddBond(carbon, oxygen, new BondSpec());
        builder.AddBond(carbon, nitrogen, new BondSpec());
        builder.AddBond(carbon, fluorine, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildDirectedInputOrderBond()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        builder.AddBond(carbon, nitrogen, BondSpec.Create(BondOrder.Single, false, BondStereo.Wedge, BondDirection.Up));
        return builder.Build();
    }
}
