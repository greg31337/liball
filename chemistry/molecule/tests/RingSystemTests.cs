using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class RingSystemTests
{
    [Test]
    public void BuildRingSystems_CreatesSingleNonFusedSystemForSingleRing()
    {
        Molecule molecule = BuildSimpleRing(6);
        RingFinder finder = new RingFinder();

        RingSet rings = finder.FindRings(molecule);

        Assert.That(rings.GetRingCount(), Is.EqualTo(1));
        Assert.That(rings.GetRingSystemCount(), Is.EqualTo(1));
        Assert.That(rings.RingSystems[0].Id, Is.EqualTo(0));
        Assert.That(rings.RingSystems[0].RingIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(rings.RingSystems[0].AtomIds.Count, Is.EqualTo(6));
        Assert.That(rings.RingSystems[0].BondIds.Count, Is.EqualTo(6));
        Assert.That(rings.RingSystems[0].IsFused, Is.False);
        Assert.That(rings.GetRingSystemForRing(0).Id, Is.EqualTo(0));
        Assert.That(rings.GetRingSystemForAtom(0).Id, Is.EqualTo(0));
    }

    [Test]
    public void BuildRingSystems_MarksFusedRingsWhenTheyShareBond()
    {
        Molecule molecule = BuildFusedTriangles();
        RingFinder finder = new RingFinder();

        RingSet rings = finder.FindRings(molecule);

        Assert.That(rings.GetRingCount(), Is.EqualTo(2));
        Assert.That(rings.GetRingSystemCount(), Is.EqualTo(1));
        Assert.That(rings.RingSystems[0].RingIds, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(rings.RingSystems[0].IsFused, Is.True);
        Assert.That(rings.RingSystems[0].FusedBondIds, Is.EqualTo(new int[] { 4 }));
        Assert.That(rings.RingSystems[0].AtomIds, Is.EqualTo(new int[] { 0, 2, 1, 3 }));
        Assert.That(rings.GetRingSystemForRing(1).Id, Is.EqualTo(0));
    }

    [Test]
    public void BuildRingSystems_GroupsSpiroRingsWithoutMarkingFused()
    {
        Molecule molecule = BuildSpiroTriangles();
        RingFinder finder = new RingFinder();

        RingSet rings = finder.FindRings(molecule);

        Assert.That(rings.GetRingCount(), Is.EqualTo(2));
        Assert.That(rings.GetRingSystemCount(), Is.EqualTo(1));
        Assert.That(rings.RingSystems[0].RingIds, Is.EqualTo(new int[] { 0, 1 }));
        Assert.That(rings.RingSystems[0].IsFused, Is.False);
        Assert.That(rings.RingSystems[0].FusedBondIds, Is.Empty);
        Assert.That(rings.GetAtomRingCount(0), Is.EqualTo(2));
    }

    [Test]
    public void BuildRingSystems_KeepsDisconnectedRingsInSeparateSystems()
    {
        Molecule molecule = BuildDisconnectedTriangles();
        RingFinder finder = new RingFinder();

        RingSet rings = finder.FindRings(molecule);

        Assert.That(rings.GetRingCount(), Is.EqualTo(2));
        Assert.That(rings.GetRingSystemCount(), Is.EqualTo(2));
        Assert.That(rings.RingSystems[0].RingIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(rings.RingSystems[1].RingIds, Is.EqualTo(new int[] { 1 }));
        Assert.That(rings.RingSystems[0].IsFused, Is.False);
        Assert.That(rings.RingSystems[1].IsFused, Is.False);
        Assert.That(rings.GetRingSystemForAtom(0).Id, Is.EqualTo(0));
        Assert.That(rings.GetRingSystemForAtom(3).Id, Is.EqualTo(1));
        Assert.That(rings.GetRingSystemForAtom(99).Id, Is.EqualTo(-1));
    }

    static Molecule BuildSimpleRing(int size)
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

            builder.AddBond(index, next, new BondSpec());
            index = index + 1;
        }

        return builder.Build();
    }

    static Molecule BuildFusedTriangles()
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
        return builder.Build();
    }

    static Molecule BuildSpiroTriangles()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int atom0 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int atom1 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int atom2 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int atom3 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int atom4 = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddBond(atom0, atom1, new BondSpec());
        builder.AddBond(atom1, atom2, new BondSpec());
        builder.AddBond(atom2, atom0, new BondSpec());
        builder.AddBond(atom0, atom3, new BondSpec());
        builder.AddBond(atom3, atom4, new BondSpec());
        builder.AddBond(atom4, atom0, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildDisconnectedTriangles()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < 6)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
            index = index + 1;
        }

        builder.AddBond(0, 1, new BondSpec());
        builder.AddBond(1, 2, new BondSpec());
        builder.AddBond(2, 0, new BondSpec());
        builder.AddBond(3, 4, new BondSpec());
        builder.AddBond(4, 5, new BondSpec());
        builder.AddBond(5, 3, new BondSpec());
        return builder.Build();
    }
}
