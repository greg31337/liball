using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class RingFinderTests
{
    [Test]
    public void FindRings_ReturnsNoRingsForAcyclicMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int atom0 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int atom1 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int atom2 = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(atom0, atom1, new BondSpec());
        builder.AddBond(atom1, atom2, new BondSpec());
        Molecule molecule = builder.Build();
        RingFinder finder = new RingFinder();

        RingSet rings = finder.FindRings(molecule);

        Assert.That(rings.GetRingCount(), Is.EqualTo(0));
        Assert.That(rings.RingAtomIds, Is.Empty);
        Assert.That(rings.RingBondIds, Is.Empty);
        Assert.That(molecule.IsRingCacheValid, Is.True);
    }

    [Test]
    public void FindRings_FindsSingleSixMemberRingAndMembershipFlags()
    {
        Molecule molecule = BuildSimpleRing(6, false);
        RingFinder finder = new RingFinder();

        RingSet rings = finder.FindRings(molecule);

        Assert.That(rings.GetRingCount(), Is.EqualTo(1));
        Assert.That(rings.Rings[0].Size, Is.EqualTo(6));
        Assert.That(rings.Rings[0].BondIds.Count, Is.EqualTo(6));
        Assert.That(rings.Rings[0].IsAromatic, Is.False);
        Assert.That(finder.AtomIsInRing(molecule, 0), Is.True);
        Assert.That(finder.BondIsInRing(molecule, 0), Is.True);

        int index = 0;
        while (index < 6)
        {
            Assert.That(rings.AtomIsInRing(index), Is.True);
            Assert.That(rings.BondIsInRing(index), Is.True);
            Assert.That(rings.GetAtomRingCount(index), Is.EqualTo(1));
            Assert.That(rings.GetBondRingCount(index), Is.EqualTo(1));
            index = index + 1;
        }
    }

    [Test]
    public void FindRings_MarksRingAromaticWhenAllBondsAreAromatic()
    {
        Molecule molecule = BuildSimpleRing(6, true);
        RingFinder finder = new RingFinder();

        RingSet rings = finder.FindRings(molecule);

        Assert.That(rings.GetRingCount(), Is.EqualTo(1));
        Assert.That(rings.Rings[0].IsAromatic, Is.True);
    }

    [Test]
    public void FindRings_FindsDeterministicCycleBasisForFusedSmallGraph()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int atom0 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int atom1 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int atom2 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int atom3 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddBond(atom0, atom1, new BondSpec());
        builder.AddBond(atom1, atom2, new BondSpec());
        builder.AddBond(atom2, atom3, new BondSpec());
        builder.AddBond(atom3, atom0, new BondSpec());
        builder.AddBond(atom0, atom2, new BondSpec());
        Molecule molecule = builder.Build();
        RingFinder finder = new RingFinder();

        RingSet rings = finder.FindRings(molecule);

        Assert.That(rings.GetRingCount(), Is.EqualTo(2));
        Assert.That(rings.GetBondRingCount(4), Is.EqualTo(2));
        Assert.That(rings.GetAtomRingCount(0), Is.EqualTo(2));
        Assert.That(rings.GetAtomRingCount(2), Is.EqualTo(2));
        Assert.That(rings.Rings[0].Size, Is.EqualTo(3));
        Assert.That(rings.Rings[1].Size, Is.EqualTo(3));
    }

    [Test]
    public void EnsureRingCache_ReusesValidCacheAndBuilderMutationsInvalidateIt()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        Molecule molecule = BuildSimpleRing(3, false);
        builder.Molecule = molecule;
        RingFinder finder = new RingFinder();

        RingSet first = finder.EnsureRingCache(molecule);
        RingSet second = finder.EnsureRingCache(molecule);

        Assert.That(first, Is.SameAs(second));
        Assert.That(molecule.IsRingCacheValid, Is.True);

        int extraAtom = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddBond(0, extraAtom, new BondSpec());

        Assert.That(molecule.IsRingCacheValid, Is.False);
    }

    [Test]
    public void PublicRingQueries_ReturnEmptyValuesForNullAndInvalidIdentifiers()
    {
        IRingFinder finder = new RingFinder();
        Molecule molecule = BuildSimpleRing(6, false);

        Assert.That(finder.FindRings(null).Rings, Is.Empty);
        Assert.That(finder.AtomIsInRing(null, 0), Is.False);
        Assert.That(finder.BondIsInRing(null, 0), Is.False);
        Assert.That(finder.AtomIsInRing(molecule, -1), Is.False);
        Assert.That(finder.BondIsInRing(molecule, 99), Is.False);
    }

    static Molecule BuildSimpleRing(int size, bool aromatic)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < size)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, aromatic, "", "C"));
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

            BondSpec spec = BondSpec.Create(BondOrder.Single, aromatic, BondStereo.None, BondDirection.None);
            if (aromatic)
            {
                spec.Order = BondOrder.Aromatic;
            }

            builder.AddBond(index, next, spec);
            index = index + 1;
        }

        return builder.Build();
    }
}
