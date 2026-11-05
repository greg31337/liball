using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class CanonicalSmilesWriterTests
{
    [Test]
    public void WriteCanonicalIsomeric_DelegatesToIsomericEntryPoint()
    {
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();
        Molecule molecule = BuildTetrahedralMolecule(Chirality.Clockwise);

        string alias = writer.WriteCanonicalIsomeric(molecule);
        string direct = writer.WriteIsomeric(molecule);

        Assert.That(alias, Is.EqualTo(direct));
        Assert.That(alias, Does.Contain("@"));
        Assert.That(writer.WriteCanonicalIsomeric(null), Is.Empty);
    }

    [Test]
    public void Write_ReturnsStableSmilesUnderAtomAndBondRenumbering()
    {
        Molecule forward = BuildAsymmetricAmide();
        Molecule reversed = BuildAsymmetricAmideReversed();
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();

        string forwardSmiles = writer.Write(forward);
        string reversedSmiles = writer.Write(reversed);

        Assert.That(forwardSmiles, Is.EqualTo(reversedSmiles));
    }

    [Test]
    public void Write_EmitsBondSymbolsForMultipleBonds()
    {
        Molecule molecule = BuildCarbonyl();
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();

        string smiles = writer.Write(molecule);

        Assert.That(smiles, Does.Contain("="));
        Assert.That(smiles, Does.Contain("O"));
        Assert.That(smiles, Does.Contain("C"));
    }

    [Test]
    public void Write_EmitsBracketAtomsForIsotopeChargeAndHydrogen()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 13, 1, 0, 3, 0, 0, false, "", "C"));
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();

        string smiles = writer.Write(builder.Build());

        Assert.That(smiles, Is.EqualTo("[13CH3+]"));
    }

    [Test]
    public void Write_EmitsBranchesForBranchedMolecules()
    {
        Molecule molecule = BuildIsobutane();
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();

        string smiles = writer.Write(molecule);

        Assert.That(smiles, Does.Contain("("));
        Assert.That(smiles, Does.Contain(")"));
        Assert.That(CountChar(smiles, 'C'), Is.EqualTo(4));
    }

    [Test]
    public void Write_EmitsRingClosuresForRings()
    {
        Molecule molecule = BuildCyclohexane();
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();

        string smiles = writer.Write(molecule);

        Assert.That(CountChar(smiles, '1'), Is.EqualTo(2));
        Assert.That(CountChar(smiles, 'C'), Is.EqualTo(6));
    }

    [Test]
    public void Write_SeparatesDisconnectedComponents()
    {
        Molecule molecule = BuildDisconnectedCarbonOxygen();
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();

        string smiles = writer.Write(molecule);

        Assert.That(smiles, Does.Contain("."));
        Assert.That(smiles, Does.Contain("C"));
        Assert.That(smiles, Does.Contain("O"));
    }

    [Test]
    public void Write_OmitsStereoMarkers()
    {
        Molecule molecule = BuildDirectedBond();
        CanonicalSmilesWriter canonicalWriter = new CanonicalSmilesWriter();
        SmilesWriter smilesWriter = new SmilesWriter();

        string canonical = canonicalWriter.Write(molecule);
        string delegated = smilesWriter.Write(molecule);

        Assert.That(canonical, Does.Not.Contain("@"));
        Assert.That(canonical, Does.Not.Contain("/"));
        Assert.That(canonical, Does.Not.Contain("\\"));
        Assert.That(delegated, Does.Not.Contain("@"));
        Assert.That(delegated, Does.Not.Contain("/"));
        Assert.That(delegated, Does.Not.Contain("\\"));
    }

    [Test]
    public void Write_ReturnsEmptyStringForNullOrEmptyMolecule()
    {
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();

        Assert.That(writer.Write(null), Is.EqualTo(""));
        Assert.That(writer.Write(new Molecule()), Is.EqualTo(""));
    }

    [Test]
    public void WriteIsomeric_EmitsTetrahedralChiralityMarkers()
    {
        Molecule molecule = BuildTetrahedralMolecule(Chirality.Clockwise);
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();

        string nonIsomeric = writer.Write(molecule);
        string isomeric = writer.WriteIsomeric(molecule);

        Assert.That(nonIsomeric, Does.Not.Contain("@"));
        Assert.That(isomeric, Does.Contain("@"));
    }

    [Test]
    public void WriteIsomeric_EmitsCounterClockwiseAsDoubleAt()
    {
        Molecule molecule = BuildTetrahedralMolecule(Chirality.CounterClockwise);
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();

        string isomeric = writer.WriteIsomeric(molecule);

        Assert.That(isomeric, Does.Contain("@@"));
    }

    [Test]
    public void WriteIsomeric_EmitsDirectionalBondMarkers()
    {
        Molecule molecule = BuildDirectedBond();
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();

        string nonIsomeric = writer.Write(molecule);
        string isomeric = writer.WriteIsomeric(molecule);

        Assert.That(nonIsomeric, Does.Not.Contain("/"));
        Assert.That(isomeric, Does.Contain("/"));
    }

    [Test]
    public void WriteIsomeric_IsStableUnderAtomRenumbering()
    {
        Molecule forward = BuildTetrahedralMolecule(Chirality.Clockwise);
        Molecule reversed = BuildTetrahedralMoleculeReversed(Chirality.Clockwise);
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();

        string forwardSmiles = writer.WriteIsomeric(forward);
        string reversedSmiles = writer.WriteIsomeric(reversed);

        Assert.That(forwardSmiles, Is.EqualTo(reversedSmiles));
    }

    [Test]
    public void WriteIsomeric_SmilesWriterAlsoWritesStereo()
    {
        Molecule molecule = BuildTetrahedralMolecule(Chirality.Clockwise);
        SmilesWriter smilesWriter = new SmilesWriter();

        Assert.That(smilesWriter.WriteIsomeric(molecule), Does.Contain("@"));
    }

    static Molecule BuildAsymmetricAmide()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int methylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        builder.AddBond(methylCarbon, carbonylCarbon, new BondSpec());
        builder.AddBond(carbonylCarbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonylCarbon, nitrogen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildAsymmetricAmideReversed()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        int carbonylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int methylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(carbonylCarbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(nitrogen, carbonylCarbon, new BondSpec());
        builder.AddBond(methylCarbon, carbonylCarbon, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildCarbonyl()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildIsobutane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int center = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        int branchA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int branchB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int branchC = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(center, branchA, new BondSpec());
        builder.AddBond(center, branchB, new BondSpec());
        builder.AddBond(center, branchC, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildCyclohexane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < 6)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
            index = index + 1;
        }

        index = 0;
        while (index < 6)
        {
            int next = index + 1;
            if (next == 6)
            {
                next = 0;
            }

            builder.AddBond(index, next, new BondSpec());
            index = index + 1;
        }

        return builder.Build();
    }

    static Molecule BuildDisconnectedCarbonOxygen()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", "O"));
        return builder.Build();
    }

    static Molecule BuildDirectedBond()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        builder.AddBond(carbon, nitrogen, BondSpec.Create(BondOrder.Single, false, BondStereo.Wedge, BondDirection.Up));
        return builder.Build();
    }

    static Molecule BuildTetrahedralMolecule(Chirality chirality)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int bromine = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        int iodine = builder.AddAtom(new AtomSpec(53, 0, 0, 0, 0, 0, 0, false, "", "I"));
        builder.AddBond(carbon, fluorine, new BondSpec());
        builder.AddBond(carbon, chlorine, new BondSpec());
        builder.AddBond(carbon, bromine, new BondSpec());
        builder.AddBond(carbon, iodine, new BondSpec());
        Molecule molecule = builder.Build();
        StereoRecord record = new StereoRecord();
        record.Id = 0;
        record.SetTetrahedral(carbon, fluorine, chlorine, bromine, iodine, chirality);
        molecule.StereoRecords.Add(record);
        return molecule;
    }

    static Molecule BuildTetrahedralMoleculeReversed(Chirality chirality)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int iodine = builder.AddAtom(new AtomSpec(53, 0, 0, 0, 0, 0, 0, false, "", "I"));
        int bromine = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddBond(carbon, iodine, new BondSpec());
        builder.AddBond(carbon, bromine, new BondSpec());
        builder.AddBond(carbon, chlorine, new BondSpec());
        builder.AddBond(carbon, fluorine, new BondSpec());
        Molecule molecule = builder.Build();
        StereoRecord record = new StereoRecord();
        record.Id = 0;
        record.SetTetrahedral(carbon, fluorine, chlorine, bromine, iodine, chirality);
        molecule.StereoRecords.Add(record);
        return molecule;
    }

    static int CountChar(string text, char character)
    {
        int count = 0;
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == character)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
