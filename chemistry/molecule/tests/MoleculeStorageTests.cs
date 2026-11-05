using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class MoleculeStorageTests
{
    [Test]
    public void Constructor_InitializesAllStorageContainers()
    {
        Molecule molecule = new Molecule();

        Assert.That(molecule.Atoms, Is.Not.Null);
        Assert.That(molecule.Bonds, Is.Not.Null);
        Assert.That(molecule.Metadata, Is.Not.Null);
        Assert.That(molecule.Conformers, Is.Not.Null);
        Assert.That(molecule.StereoRecords, Is.Not.Null);
        Assert.That(molecule.NeighborAtomIdsByAtomId, Is.Not.Null);
        Assert.That(molecule.NeighborBondIdsByAtomId, Is.Not.Null);
        Assert.That(molecule.ComponentIdsByAtomId, Is.Not.Null);
        Assert.That(molecule.RingCache, Is.Not.Null);
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(0));
        Assert.That(molecule.GetBondCount(), Is.EqualTo(0));
        Assert.That(molecule.IsAdjacencyCacheValid, Is.False);
        Assert.That(molecule.IsComponentCacheValid, Is.False);
        Assert.That(molecule.IsRingCacheValid, Is.False);
    }

    [Test]
    public void AtomAndBondAccessors_ReadFromStorageLists()
    {
        Molecule molecule = new Molecule();
        Atom oxygen = Atom.CreateWithAtomicNumber(0, 8);
        Atom hydrogen = Atom.CreateWithAtomicNumber(1, 1);
        Bond bond = Bond.CreateWithOrder(0, 0, 1, BondOrder.Single);

        molecule.Atoms.Add(oxygen);
        molecule.Atoms.Add(hydrogen);
        molecule.Bonds.Add(bond);

        Assert.That(molecule.GetAtomCount(), Is.EqualTo(2));
        Assert.That(molecule.GetBondCount(), Is.EqualTo(1));
        Assert.That(molecule.GetAtom(0), Is.SameAs(oxygen));
        Assert.That(molecule.GetAtom(1), Is.SameAs(hydrogen));
        Assert.That(molecule.GetBond(0), Is.SameAs(bond));
        Assert.That(molecule.GetAtoms(), Is.SameAs(molecule.Atoms));
        Assert.That(molecule.GetBonds(), Is.SameAs(molecule.Bonds));
    }

    [Test]
    public void ConformerAndStereoAccessors_ReadFromStorageLists()
    {
        Molecule molecule = new Molecule();
        Conformer conformer = Conformer.CreateEmpty();
        StereoRecord stereoRecord = new StereoRecord();

        molecule.Conformers.Add(conformer);
        molecule.StereoRecords.Add(stereoRecord);

        Assert.That(molecule.GetConformers(), Is.SameAs(molecule.Conformers));
        Assert.That(molecule.GetStereoRecords(), Is.SameAs(molecule.StereoRecords));
        Assert.That(molecule.GetConformers()[0], Is.SameAs(conformer));
        Assert.That(molecule.GetStereoRecords()[0], Is.SameAs(stereoRecord));
    }

    [Test]
    public void ClearCaches_RemovesCacheDataAndInvalidatesFlags()
    {
        Molecule molecule = new Molecule();
        RingSet originalRingCache = molecule.RingCache;

        molecule.NeighborAtomIdsByAtomId.Add(new List<int> { 1, 2 });
        molecule.NeighborBondIdsByAtomId.Add(new List<int> { 0 });
        molecule.ComponentIdsByAtomId.Add(7);
        molecule.IsAdjacencyCacheValid = true;
        molecule.IsComponentCacheValid = true;
        molecule.IsRingCacheValid = true;

        molecule.ClearCaches();

        Assert.That(molecule.NeighborAtomIdsByAtomId, Is.Empty);
        Assert.That(molecule.NeighborBondIdsByAtomId, Is.Empty);
        Assert.That(molecule.ComponentIdsByAtomId, Is.Empty);
        Assert.That(molecule.RingCache, Is.Not.SameAs(originalRingCache));
        Assert.That(molecule.IsAdjacencyCacheValid, Is.False);
        Assert.That(molecule.IsComponentCacheValid, Is.False);
        Assert.That(molecule.IsRingCacheValid, Is.False);
    }
}
