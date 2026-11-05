using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class SmartsQueryModelTests
{
    [Test]
    public void QueryAtom_DefaultMatchesAnyAtom()
    {
        QueryAtom query = new QueryAtom();
        Atom atom = new Atom(0, new AtomSpec(8, 0, -1, 0, 1, 0, 0, false, "", "O"));

        Assert.That(query.Matches(atom), Is.True);
    }

    [Test]
    public void QueryAtom_MatchesAtomicNumberListsAndNegation()
    {
        QueryAtom query = new QueryAtom();
        Atom carbon = new Atom(0, new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        Atom oxygen = new Atom(1, new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", "O"));

        query.AddAtomicNumber(6);
        query.AddAtomicNumber(7);

        Assert.That(query.Matches(carbon), Is.True);
        Assert.That(query.Matches(oxygen), Is.False);

        query.NegateAtomicNumber = true;

        Assert.That(query.Matches(carbon), Is.False);
        Assert.That(query.Matches(oxygen), Is.True);
    }

    [Test]
    public void QueryAtom_MatchesChargeIsotopeHydrogenAromaticityDegreeAndRingCount()
    {
        Molecule ring = BuildCyclopropane();
        ring.Atoms[0].IsotopeMassNumber = 13;
        ring.Atoms[0].FormalCharge = 1;
        ring.Atoms[0].ImplicitHydrogenCount = 1;
        ring.Atoms[0].IsAromatic = true;
        QueryAtom query = new QueryAtom();
        query.SetAtomicNumber(6);
        query.SetIsotopeMassNumber(13);
        query.SetFormalCharge(1);
        query.SetHydrogenCount(1);
        query.SetAromaticity(true);
        query.SetDegree(2);
        query.SetRingCount(1);

        Assert.That(query.MatchesAtomInMolecule(ring, 0), Is.True);
        Assert.That(query.MatchesAtomInMolecule(ring, 1), Is.False);
    }

    [Test]
    public void QueryBond_DefaultAndOrderConstraintsMatchBonds()
    {
        Bond single = new Bond(0, 0, 1, new BondSpec());
        BondSpec doubleSpec = BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None);
        Bond doubleBond = new Bond(1, 0, 1, doubleSpec);
        QueryBond any = new QueryBond();
        QueryBond doubleQuery = new QueryBond();
        doubleQuery.SetOrder(BondOrder.Double);

        Assert.That(any.Matches(single), Is.True);
        Assert.That(doubleQuery.Matches(doubleBond), Is.True);
        Assert.That(doubleQuery.Matches(single), Is.False);
    }

    [Test]
    public void QueryBond_MatchesAromaticDirectionStereoDativeAndRingConstraints()
    {
        Molecule ring = BuildCyclopropane();
        ring.Bonds[0].IsAromatic = true;
        ring.Bonds[0].Direction = BondDirection.Up;
        ring.Bonds[0].Stereo = BondStereo.Wedge;
        ring.Bonds[0].IsDative = true;
        QueryBond query = new QueryBond();
        query.SetAromaticity(true);
        query.SetDirection(BondDirection.Up);
        query.SetStereo(BondStereo.Wedge);
        query.SetDative(true);
        query.SetRingBond();

        Assert.That(query.MatchesBondInMolecule(ring, 0), Is.True);
        Assert.That(query.MatchesBondInMolecule(ring, 99), Is.False);
    }

    [Test]
    public void QueryMolecule_AddsAtomsBondsAndBuildsAdjacency()
    {
        QueryMolecule query = new QueryMolecule();
        QueryAtom carbon = new QueryAtom();
        carbon.SetAtomicNumber(6);
        QueryAtom oxygen = new QueryAtom();
        oxygen.SetAtomicNumber(8);
        QueryBond bond = new QueryBond();
        bond.SetOrder(BondOrder.Double);

        int carbonId = query.AddAtom(carbon);
        int oxygenId = query.AddAtom(oxygen);
        int bondId = query.AddBond(carbonId, oxygenId, bond);

        Assert.That(carbonId, Is.EqualTo(0));
        Assert.That(oxygenId, Is.EqualTo(1));
        Assert.That(bondId, Is.EqualTo(0));
        Assert.That(query.GetAtomCount(), Is.EqualTo(2));
        Assert.That(query.GetBondCount(), Is.EqualTo(1));
        Assert.That(query.GetAtom(0).AtomicNumber, Is.EqualTo(6));
        Assert.That(query.GetBond(0).Order, Is.EqualTo(BondOrder.Double));
        Assert.That(query.GetNeighborAtomIds(0), Is.EqualTo(new int[] { 1 }));
        Assert.That(query.GetNeighborBondIds(1), Is.EqualTo(new int[] { 0 }));
        Assert.That(query.GetBondIdBetween(0, 1), Is.EqualTo(0));
        Assert.That(query.GetDegree(0), Is.EqualTo(1));
    }

    [Test]
    public void QueryMolecule_RejectsInvalidBondsAndReturnsDefaultsForInvalidLookups()
    {
        QueryMolecule query = new QueryMolecule();
        query.AddAnyAtom();

        int bondId = query.AddAnyBond(0, 9);

        Assert.That(bondId, Is.EqualTo(-1));
        Assert.That(query.GetAtom(9).Id, Is.EqualTo(-1));
        Assert.That(query.GetBond(9).Id, Is.EqualTo(-1));
        Assert.That(query.GetNeighborAtomIds(9), Is.Empty);
    }

    static Molecule BuildCyclopropane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddBond(0, 1, new BondSpec());
        builder.AddBond(1, 2, new BondSpec());
        builder.AddBond(2, 0, new BondSpec());
        return builder.Build();
    }
}
