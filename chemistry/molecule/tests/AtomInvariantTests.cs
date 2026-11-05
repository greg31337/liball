using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class AtomInvariantTests
{
    [Test]
    public void CalculateInvariants_ReturnsStableMultisetUnderAtomRenumbering()
    {
        Molecule forward = BuildEthanolForward();
        Molecule reversed = BuildEthanolReversed();
        AtomRanker ranker = new AtomRanker();

        AtomInvariantSet forwardInvariants = ranker.CalculateInvariants(forward);
        AtomInvariantSet reversedInvariants = ranker.CalculateInvariants(reversed);

        long[] forwardValues = GetSortedInvariantValues(forwardInvariants);
        long[] reversedValues = GetSortedInvariantValues(reversedInvariants);

        Assert.That(forwardValues, Is.EqualTo(reversedValues));
    }

    [Test]
    public void CalculateAtomInvariant_ExposesCoreAtomAndBondFeatures()
    {
        Molecule molecule = BuildEthanolForward();
        AtomRanker ranker = new AtomRanker();

        AtomInvariantSet invariants = ranker.CalculateInvariants(molecule);
        AtomInvariant carbon = invariants.GetInvariant(0);
        AtomInvariant oxygen = invariants.GetInvariant(2);

        Assert.That(carbon.AtomId, Is.EqualTo(0));
        Assert.That(carbon.AtomicNumber, Is.EqualTo(6));
        Assert.That(carbon.Degree, Is.EqualTo(1));
        Assert.That(carbon.HydrogenCount, Is.EqualTo(3));
        Assert.That(carbon.BondOrderSum, Is.EqualTo(1));
        Assert.That(carbon.NeighborScore, Is.GreaterThan(0));
        Assert.That(oxygen.AtomicNumber, Is.EqualTo(8));
        Assert.That(oxygen.HydrogenCount, Is.EqualTo(1));
        Assert.That(oxygen.Value, Is.Not.EqualTo(carbon.Value));
    }

    [Test]
    public void CalculateInvariants_DistinguishesIsotopeChargeAndRadicalState()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon12 = builder.AddAtom(new AtomSpec(6, 12, 0, 0, 4, 0, 0, false, "", "C"));
        int carbon13 = builder.AddAtom(new AtomSpec(6, 13, 0, 0, 4, 0, 0, false, "", "C"));
        int cation = builder.AddAtom(new AtomSpec(6, 12, 1, 0, 3, 0, 0, false, "", "C"));
        int radical = builder.AddAtom(new AtomSpec(6, 12, 0, 0, 3, 1, 0, false, "", "C"));
        AtomRanker ranker = new AtomRanker();

        AtomInvariantSet invariants = ranker.CalculateInvariants(builder.Build());

        Assert.That(invariants.GetInvariant(carbon12).Value, Is.Not.EqualTo(invariants.GetInvariant(carbon13).Value));
        Assert.That(invariants.GetInvariant(carbon12).Value, Is.Not.EqualTo(invariants.GetInvariant(cation).Value));
        Assert.That(invariants.GetInvariant(carbon12).Value, Is.Not.EqualTo(invariants.GetInvariant(radical).Value));
    }

    [Test]
    public void CalculateInvariants_IncludesRingAndAromaticFeatures()
    {
        Molecule aromatic = BuildRing(6, true);
        Molecule aliphatic = BuildRing(6, false);
        AtomRanker ranker = new AtomRanker();

        AtomInvariant aromaticAtom = ranker.CalculateInvariants(aromatic).GetInvariant(0);
        AtomInvariant aliphaticAtom = ranker.CalculateInvariants(aliphatic).GetInvariant(0);

        Assert.That(aromaticAtom.RingCount, Is.EqualTo(1));
        Assert.That(aromaticAtom.AromaticBondCount, Is.EqualTo(2));
        Assert.That(aliphaticAtom.RingCount, Is.EqualTo(1));
        Assert.That(aliphaticAtom.AromaticBondCount, Is.EqualTo(0));
        Assert.That(aromaticAtom.Value, Is.Not.EqualTo(aliphaticAtom.Value));
    }

    [Test]
    public void CalculateInvariants_GivesEquivalentAtomsSameInvariant()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int chlorineA = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int chlorineB = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        builder.AddBond(carbon, chlorineA, new BondSpec());
        builder.AddBond(carbon, chlorineB, new BondSpec());
        AtomRanker ranker = new AtomRanker();

        AtomInvariantSet invariants = ranker.CalculateInvariants(builder.Build());

        Assert.That(invariants.GetInvariant(chlorineA).Value, Is.EqualTo(invariants.GetInvariant(chlorineB).Value));
    }

    [Test]
    public void CalculateInvariants_UsesNeighborDescriptorsToDistinguishCarbonylAndAlcoholCarbons()
    {
        Molecule molecule = BuildCarbonylAndAlcohol();
        AtomRanker ranker = new AtomRanker();

        AtomInvariantSet invariants = ranker.CalculateInvariants(molecule);
        AtomInvariant carbonylCarbon = invariants.GetInvariant(0);
        AtomInvariant alcoholCarbon = invariants.GetInvariant(2);

        Assert.That(carbonylCarbon.AtomicNumber, Is.EqualTo(alcoholCarbon.AtomicNumber));
        Assert.That(carbonylCarbon.NeighborScore, Is.GreaterThan(alcoholCarbon.NeighborScore));
        Assert.That(carbonylCarbon.Value, Is.Not.EqualTo(alcoholCarbon.Value));
    }

    [Test]
    public void CalculateInvariants_ReturnsEmptyOrDefaultForNullAndInvalidAtoms()
    {
        AtomRanker ranker = new AtomRanker();
        Molecule molecule = BuildEthanolForward();

        AtomInvariantSet set = ranker.CalculateInvariants(null);
        AtomInvariant invalid = ranker.CalculateAtomInvariant(molecule, new RingSet(), 99);

        Assert.That(set.Invariants, Is.Empty);
        Assert.That(invalid.AtomId, Is.EqualTo(-1));
        Assert.That(invalid.Value, Is.EqualTo(0));
    }

    static Molecule BuildEthanolForward()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, new BondSpec());
        builder.AddBond(carbonB, oxygen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildEthanolReversed()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(oxygen, carbonB, new BondSpec());
        builder.AddBond(carbonB, carbonA, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildRing(int size, bool aromatic)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < size)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, aromatic, "", "C"));
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
            if (aromatic)
            {
                order = BondOrder.Aromatic;
            }

            builder.AddBond(index, next, BondSpec.Create(order, aromatic, BondStereo.None, BondDirection.None));
            index = index + 1;
        }

        return builder.Build();
    }

    static Molecule BuildCarbonylAndAlcohol()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int carbonylOxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int alcoholCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        int alcoholOxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbonylCarbon, carbonylOxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(alcoholCarbon, alcoholOxygen, new BondSpec());
        return builder.Build();
    }

    static long[] GetSortedInvariantValues(AtomInvariantSet set)
    {
        long[] values = new long[set.Invariants.Count];
        int index = 0;
        while (index < set.Invariants.Count)
        {
            values[index] = set.Invariants[index].Value;
            index = index + 1;
        }

        SortLongs(values);
        return values;
    }

    static void SortLongs(long[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            long value = values[index];
            int scan = index - 1;
            while (scan >= 0 && values[scan] > value)
            {
                values[scan + 1] = values[scan];
                scan = scan - 1;
            }

            values[scan + 1] = value;
            index = index + 1;
        }
    }
}
