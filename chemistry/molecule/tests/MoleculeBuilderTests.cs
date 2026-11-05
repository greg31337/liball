using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class MoleculeBuilderTests
{
    [Test]
    public void CreateFrom_CopiesInputWithoutSharingGraphObjects()
    {
        MoleculeBuilder source = new MoleculeBuilder();
        source.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));

        MoleculeBuilder copy = MoleculeBuilder.CreateFrom(source.Build());
        source.Molecule.Atoms[0].AtomicNumber = 8;

        Assert.That(copy.Build(), Is.Not.SameAs(source.Build()));
        Assert.That(copy.Build().Atoms[0], Is.Not.SameAs(source.Build().Atoms[0]));
        Assert.That(copy.Build().Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(MoleculeBuilder.CreateFrom(null).Build().Atoms, Is.Empty);
    }

    [Test]
    public void AddAtomAndAddBond_CreateSequentialIdsAndStoredObjects()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        AtomSpec carbon = new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C");
        AtomSpec oxygen = new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", "O");
        BondSpec doubleBond = BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None);

        int carbonId = builder.AddAtom(carbon);
        int oxygenId = builder.AddAtom(oxygen);
        int bondId = builder.AddBond(carbonId, oxygenId, doubleBond);
        int invalidBondId = builder.AddBond(0, 99, doubleBond);

        Molecule molecule = builder.Build();
        Assert.That(carbonId, Is.EqualTo(0));
        Assert.That(oxygenId, Is.EqualTo(1));
        Assert.That(bondId, Is.EqualTo(0));
        Assert.That(invalidBondId, Is.EqualTo(-1));
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(2));
        Assert.That(molecule.GetBondCount(), Is.EqualTo(1));
        Assert.That(molecule.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(molecule.Atoms[1].AtomicNumber, Is.EqualTo(8));
        Assert.That(molecule.Bonds[0].BeginAtomId, Is.EqualTo(0));
        Assert.That(molecule.Bonds[0].EndAtomId, Is.EqualTo(1));
        Assert.That(molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
    }

    [Test]
    public void RemoveAtom_RemovesIncidentBondsAndRenumbersRemainingGraph()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int atom0 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int atom1 = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        int atom2 = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        BondSpec single = new BondSpec();
        builder.AddBond(atom0, atom1, single);
        builder.AddBond(atom1, atom2, single);
        builder.AddBond(atom0, atom2, single);

        builder.RemoveAtom(atom0);

        Molecule molecule = builder.Build();
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(2));
        Assert.That(molecule.Atoms[0].Id, Is.EqualTo(0));
        Assert.That(molecule.Atoms[0].AtomicNumber, Is.EqualTo(7));
        Assert.That(molecule.Atoms[1].Id, Is.EqualTo(1));
        Assert.That(molecule.Atoms[1].AtomicNumber, Is.EqualTo(8));
        Assert.That(molecule.GetBondCount(), Is.EqualTo(1));
        Assert.That(molecule.Bonds[0].Id, Is.EqualTo(0));
        Assert.That(molecule.Bonds[0].BeginAtomId, Is.EqualTo(0));
        Assert.That(molecule.Bonds[0].EndAtomId, Is.EqualTo(1));
    }

    [Test]
    public void RemoveBond_RenumbersRemainingBonds()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int atom0 = builder.AddAtom(AtomSpec.CreateEmpty());
        int atom1 = builder.AddAtom(AtomSpec.CreateEmpty());
        int atom2 = builder.AddAtom(AtomSpec.CreateEmpty());
        builder.AddBond(atom0, atom1, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(atom1, atom2, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));

        builder.RemoveBond(0);

        Molecule molecule = builder.Build();
        Assert.That(molecule.GetBondCount(), Is.EqualTo(1));
        Assert.That(molecule.Bonds[0].Id, Is.EqualTo(0));
        Assert.That(molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(molecule.Bonds[0].BeginAtomId, Is.EqualTo(1));
        Assert.That(molecule.Bonds[0].EndAtomId, Is.EqualTo(2));
    }

    [Test]
    public void ReplaceAtom_ReplacesSpecFieldsAndPreservesCoordinates()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int atomId = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.Molecule.Atoms[atomId].HasCoordinates = true;
        builder.Molecule.Atoms[atomId].X = 1.25;
        builder.Molecule.Atoms[atomId].Y = 2.5;
        builder.Molecule.Atoms[atomId].Z = 3.75;

        builder.ReplaceAtom(atomId, new AtomSpec(8, 18, -1, 0, 1, 0, 5, true, "oxo", "O"));

        Atom atom = builder.Build().Atoms[atomId];
        Assert.That(atom.Id, Is.EqualTo(atomId));
        Assert.That(atom.AtomicNumber, Is.EqualTo(8));
        Assert.That(atom.IsotopeMassNumber, Is.EqualTo(18));
        Assert.That(atom.FormalCharge, Is.EqualTo(-1));
        Assert.That(atom.AtomMapNumber, Is.EqualTo(5));
        Assert.That(atom.IsAromatic, Is.True);
        Assert.That(atom.Alias, Is.EqualTo("oxo"));
        Assert.That(atom.Label, Is.EqualTo("O"));
        Assert.That(atom.HasCoordinates, Is.True);
        Assert.That(atom.X, Is.EqualTo(1.25));
        Assert.That(atom.Y, Is.EqualTo(2.5));
        Assert.That(atom.Z, Is.EqualTo(3.75));
    }

    [Test]
    public void Clone_CreatesIndependentAtomAndBondCopies()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonId = builder.AddAtom(new AtomSpec(6, 13, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygenId = builder.AddAtom(new AtomSpec(8, 0, -1, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbonId, oxygenId, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));

        Molecule clone = builder.Clone();
        builder.Molecule.Atoms[0].AtomicNumber = 14;
        builder.Molecule.Bonds[0].Order = BondOrder.Single;

        Assert.That(clone, Is.Not.SameAs(builder.Molecule));
        Assert.That(clone.Atoms[0], Is.Not.SameAs(builder.Molecule.Atoms[0]));
        Assert.That(clone.Bonds[0], Is.Not.SameAs(builder.Molecule.Bonds[0]));
        Assert.That(clone.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(clone.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
    }

    [Test]
    public void Merge_AppendsAtomsAndOffsetsBondAtomIds()
    {
        MoleculeBuilder left = new MoleculeBuilder();
        int leftAtom0 = left.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int leftAtom1 = left.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        left.AddBond(leftAtom0, leftAtom1, new BondSpec());

        MoleculeBuilder right = new MoleculeBuilder();
        int rightAtom0 = right.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int rightAtom1 = right.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        right.AddBond(rightAtom0, rightAtom1, BondSpec.Create(BondOrder.Single, false, BondStereo.Hash, BondDirection.Down));

        left.Merge(right.Build());

        Molecule molecule = left.Build();
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(4));
        Assert.That(molecule.GetBondCount(), Is.EqualTo(2));
        Assert.That(molecule.Atoms[2].AtomicNumber, Is.EqualTo(8));
        Assert.That(molecule.Atoms[3].AtomicNumber, Is.EqualTo(1));
        Assert.That(molecule.Bonds[1].Id, Is.EqualTo(1));
        Assert.That(molecule.Bonds[1].BeginAtomId, Is.EqualTo(2));
        Assert.That(molecule.Bonds[1].EndAtomId, Is.EqualTo(3));
        Assert.That(molecule.Bonds[1].Stereo, Is.EqualTo(BondStereo.Hash));
    }

    [Test]
    public void SplitComponents_ReturnsDisconnectedMoleculesInAtomOrder()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int hydrogen = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddBond(oxygen, hydrogen, new BondSpec());

        List<Molecule> components = builder.SplitComponents();

        Assert.That(components.Count, Is.EqualTo(2));
        Assert.That(components[0].GetAtomCount(), Is.EqualTo(2));
        Assert.That(components[0].GetBondCount(), Is.EqualTo(1));
        Assert.That(components[0].Atoms[0].AtomicNumber, Is.EqualTo(8));
        Assert.That(components[0].Atoms[1].AtomicNumber, Is.EqualTo(1));
        Assert.That(components[0].Bonds[0].BeginAtomId, Is.EqualTo(0));
        Assert.That(components[0].Bonds[0].EndAtomId, Is.EqualTo(1));
        Assert.That(components[1].GetAtomCount(), Is.EqualTo(1));
        Assert.That(components[1].GetBondCount(), Is.EqualTo(0));
        Assert.That(components[1].Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(carbon, Is.EqualTo(2));
    }

    [Test]
    public void InvalidEditsAndNullMerge_LeaveBuilderContentsUnchanged()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule before = builder.Clone();

        builder.RemoveAtom(-1);
        builder.RemoveAtom(99);
        builder.RemoveBond(-1);
        builder.RemoveBond(99);
        builder.ReplaceAtom(99, new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", "O"));
        builder.Merge(null);

        Assert.That(builder.Build().Atoms, Has.Count.EqualTo(1));
        Assert.That(builder.Build().Atoms[0].AtomicNumber, Is.EqualTo(before.Atoms[0].AtomicNumber));
        Assert.That(builder.Build().Bonds, Is.Empty);
    }

    [Test]
    public void BuildReturnsLiveGraphWhileCloneAndInitializeUseIndependentCopies()
    {
        Molecule source = new Molecule();
        source.Atoms.Add(new Atom(0, new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C")));
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.Initialize(source);

        Molecule live = builder.Build();
        Molecule clone = builder.Clone();
        live.Atoms[0].AtomicNumber = 8;

        Assert.That(builder.Build().Atoms[0].AtomicNumber, Is.EqualTo(8));
        Assert.That(clone.Atoms[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(source.Atoms[0].AtomicNumber, Is.EqualTo(6));

        builder.Initialize(null);

        Assert.That(builder.Build().Atoms, Is.Empty);
    }
}
