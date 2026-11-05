using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class MoleculeAdjacencyTests
{
    [Test]
    public void NeighborLookup_BuildsAdjacencyCacheInBondOrder()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int hydrogenA = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        int hydrogenB = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        builder.AddBond(oxygen, hydrogenA, new BondSpec());
        builder.AddBond(oxygen, hydrogenB, new BondSpec());
        Molecule molecule = builder.Build();

        List<int> oxygenNeighbors = molecule.GetNeighborAtomIds(oxygen);
        List<int> oxygenBonds = molecule.GetNeighborBondIds(oxygen);

        Assert.That(molecule.IsAdjacencyCacheValid, Is.True);
        Assert.That(molecule.NeighborAtomIdsByAtomId.Count, Is.EqualTo(3));
        Assert.That(oxygenNeighbors, Is.EqualTo(new int[] { hydrogenA, hydrogenB }));
        Assert.That(oxygenBonds, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(molecule.GetNeighborAtomIds(hydrogenA), Is.EqualTo(new int[] { oxygen }));
        Assert.That(molecule.GetNeighborBondIds(hydrogenA), Is.EqualTo(new int[] { 0 }));
        Assert.That(molecule.GetDegree(oxygen), Is.EqualTo(2));
        Assert.That(molecule.GetDegree(hydrogenA), Is.EqualTo(1));
    }

    [Test]
    public void BondLookup_FindsBondsInEitherAtomOrder()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        int bondId = builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        Molecule molecule = builder.Build();

        Bond? forward = molecule.GetBondBetween(carbon, oxygen);
        Bond? reverse = molecule.GetBondBetween(oxygen, carbon);

        Assert.That(molecule.GetBondIdBetween(carbon, oxygen), Is.EqualTo(bondId));
        Assert.That(molecule.GetBondIdBetween(oxygen, carbon), Is.EqualTo(bondId));
        Assert.That(forward, Is.SameAs(molecule.Bonds[bondId]));
        Assert.That(reverse, Is.SameAs(molecule.Bonds[bondId]));
        Assert.That(molecule.GetBondBetween(carbon, nitrogen), Is.Null);
        Assert.That(molecule.GetBondIdBetween(carbon, nitrogen), Is.EqualTo(-1));
    }

    [Test]
    public void InvalidAtomIds_ReturnEmptyLookupResults()
    {
        Molecule molecule = new Molecule();

        Assert.That(molecule.GetNeighborAtomIds(-1), Is.Empty);
        Assert.That(molecule.GetNeighborBondIds(12), Is.Empty);
        Assert.That(molecule.GetDegree(12), Is.EqualTo(0));
        Assert.That(molecule.GetBondBetween(0, 1), Is.Null);
        Assert.That(molecule.GetBondIdBetween(0, 1), Is.EqualTo(-1));
    }

    [Test]
    public void InvalidateAdjacencyCache_ForcesRebuildOnNextLookup()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int atom0 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int atom1 = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        Molecule molecule = builder.Build();

        Assert.That(molecule.GetDegree(atom0), Is.EqualTo(0));
        molecule.Atoms.Add(Atom.CreateWithAtomicNumber(2, 8));
        molecule.Bonds.Add(Bond.CreateWithOrder(0, atom0, atom1, BondOrder.Single));
        molecule.InvalidateAdjacencyCache();

        Assert.That(molecule.IsAdjacencyCacheValid, Is.False);
        Assert.That(molecule.GetDegree(atom0), Is.EqualTo(1));
        Assert.That(molecule.GetNeighborAtomIds(atom0), Is.EqualTo(new int[] { atom1 }));
        Assert.That(molecule.IsAdjacencyCacheValid, Is.True);
    }

    [Test]
    public void BuilderMutations_ClearAdjacencyCache()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int atom0 = builder.AddAtom(AtomSpec.CreateEmpty());
        int atom1 = builder.AddAtom(AtomSpec.CreateEmpty());
        builder.AddBond(atom0, atom1, new BondSpec());

        Molecule molecule = builder.Build();
        Assert.That(molecule.GetDegree(atom0), Is.EqualTo(1));
        Assert.That(molecule.IsAdjacencyCacheValid, Is.True);

        builder.RemoveBond(0);

        Assert.That(molecule.IsAdjacencyCacheValid, Is.False);
        Assert.That(molecule.GetDegree(atom0), Is.EqualTo(0));
    }
}
