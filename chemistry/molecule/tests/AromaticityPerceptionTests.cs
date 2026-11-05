using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class AromaticityPerceptionTests
{
    [Test]
    public void Perceive_ClonesMoleculeAndAssignsAromaticBenzeneBonds()
    {
        Molecule molecule = BuildAlternatingCarbonRing(6);
        AromaticityModel model = new AromaticityModel();

        AromaticityPerceptionResult result = model.Perceive(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetCount(), Is.EqualTo(0));
        Assert.That(result.Molecule, Is.Not.SameAs(molecule));
        Assert.That(result.RingIsAromatic(0), Is.True);
        Assert.That(result.AromaticAtomIds.Count, Is.EqualTo(6));
        Assert.That(result.AromaticBondIds.Count, Is.EqualTo(6));
        Assert.That(result.AtomIsAromatic(0), Is.True);
        Assert.That(result.BondIsAromatic(0), Is.True);
        AssertAllRingAtomsAndBondsAreAromatic(result.Molecule);
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Aromatic));
        Assert.That(result.Molecule.IsRingCacheValid, Is.False);
        Assert.That(molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(molecule.Bonds[0].IsAromatic, Is.False);
    }

    [Test]
    public void PerceiveInPlace_AssignsFuranLikeRingAsAromatic()
    {
        Molecule molecule = BuildFuranLikeRing();
        AromaticityModel model = new AromaticityModel();

        AromaticityPerceptionResult result = model.PerceiveInPlace(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetCount(), Is.EqualTo(0));
        Assert.That(result.RingIsAromatic(0), Is.True);
        Assert.That(result.Candidates.RingCandidates[0].ElectronCount, Is.EqualTo(6));
        AssertAllRingAtomsAndBondsAreAromatic(molecule);
        Assert.That(molecule.Atoms[0].AtomicNumber, Is.EqualTo(8));
        Assert.That(molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Aromatic));
        Assert.That(molecule.Bonds[4].Order, Is.EqualTo(BondOrder.Aromatic));
    }

    [Test]
    public void Perceive_LeavesSaturatedRingNonAromatic()
    {
        Molecule molecule = BuildSaturatedCarbonRing(6);
        AromaticityModel model = new AromaticityModel();

        AromaticityPerceptionResult result = model.Perceive(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetCount(), Is.EqualTo(0));
        Assert.That(result.AromaticAtomIds, Is.Empty);
        Assert.That(result.AromaticBondIds, Is.Empty);
        Assert.That(result.Molecule.Atoms[0].IsAromatic, Is.False);
        Assert.That(result.Molecule.Bonds[0].IsAromatic, Is.False);
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Single));
    }

    [Test]
    public void Perceive_ClearsRejectedAromaticRingAndReportsWarning()
    {
        Molecule molecule = BuildAssignedAromaticCarbonRing(5);
        AromaticityModel model = new AromaticityModel();

        AromaticityPerceptionResult result = model.Perceive(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Report.GetWarningCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("AROMATICITY_REJECTED"));
        Assert.That(result.Report.Get(0).BondId, Is.EqualTo(0));
        Assert.That(result.AromaticRingIds, Is.Empty);
        Assert.That(result.Molecule.Atoms[0].IsAromatic, Is.False);
        Assert.That(result.Molecule.Bonds[0].IsAromatic, Is.False);
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Aromatic));
    }

    [Test]
    public void Perceive_ReturnsErrorForNullMolecule()
    {
        AromaticityModel model = new AromaticityModel();
        Molecule? molecule = null;

        AromaticityPerceptionResult result = model.Perceive(molecule);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("NULL_MOLECULE"));
    }

    static Molecule BuildAlternatingCarbonRing(int size)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < size)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
            index = index + 1;
        }

        index = 0;
        while (index < size)
        {
            int next = index + 1;
            if (next == size)
            {
                next = 0;
            }

            BondOrder order = BondOrder.Single;
            if (index % 2 == 0)
            {
                order = BondOrder.Double;
            }

            builder.AddBond(index, next, BondSpec.Create(order, false, BondStereo.None, BondDirection.None));
            index = index + 1;
        }

        return builder.Build();
    }

    static Molecule BuildFuranLikeRing()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddBond(0, 1, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(1, 2, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(2, 3, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(3, 4, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(4, 0, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildSaturatedCarbonRing(int size)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < size)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
            index = index + 1;
        }

        AddSingleRingBonds(builder, size);
        return builder.Build();
    }

    static Molecule BuildAssignedAromaticCarbonRing(int size)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < size)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, true, "", "C"));
            index = index + 1;
        }

        index = 0;
        while (index < size)
        {
            int next = index + 1;
            if (next == size)
            {
                next = 0;
            }

            builder.AddBond(index, next, BondSpec.Create(BondOrder.Aromatic, true, BondStereo.None, BondDirection.None));
            index = index + 1;
        }

        return builder.Build();
    }

    static void AddSingleRingBonds(MoleculeBuilder builder, int size)
    {
        int index = 0;
        while (index < size)
        {
            int next = index + 1;
            if (next == size)
            {
                next = 0;
            }

            builder.AddBond(index, next, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
            index = index + 1;
        }
    }

    static void AssertAllRingAtomsAndBondsAreAromatic(Molecule molecule)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Assert.That(molecule.Atoms[atomIndex].IsAromatic, Is.True);
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Assert.That(molecule.Bonds[bondIndex].IsAromatic, Is.True);
            Assert.That(molecule.Bonds[bondIndex].Order, Is.EqualTo(BondOrder.Aromatic));
            bondIndex = bondIndex + 1;
        }
    }
}
