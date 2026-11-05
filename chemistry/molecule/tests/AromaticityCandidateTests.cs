using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class AromaticityCandidateTests
{
    [Test]
    public void FindCandidates_MarksAlternatingBenzeneRingAsCandidate()
    {
        Molecule molecule = BuildAlternatingRing(6, 0);
        AromaticityModel model = new AromaticityModel();

        AromaticityCandidateSet candidates = model.FindCandidates(molecule);

        Assert.That(candidates.RingCandidates.Count, Is.EqualTo(1));
        Assert.That(candidates.RingCandidates[0].IsCandidate, Is.True);
        Assert.That(candidates.RingCandidates[0].ElectronCount, Is.EqualTo(6));
        Assert.That(candidates.CandidateRingIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(candidates.CandidateAtomIds.Count, Is.EqualTo(6));
        Assert.That(candidates.CandidateBondIds.Count, Is.EqualTo(6));
        Assert.That(candidates.AtomIsCandidate(0), Is.True);
        Assert.That(candidates.BondIsCandidate(0), Is.True);
        Assert.That(molecule.Atoms[0].IsAromatic, Is.False);
        Assert.That(molecule.Bonds[0].IsAromatic, Is.False);
    }

    [Test]
    public void FindCandidates_RejectsSaturatedRing()
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

            builder.AddBond(index, next, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
            index = index + 1;
        }

        AromaticityModel model = new AromaticityModel();

        AromaticityCandidateSet candidates = model.FindCandidates(builder.Build());

        Assert.That(candidates.RingCandidates.Count, Is.EqualTo(1));
        Assert.That(candidates.RingCandidates[0].IsCandidate, Is.False);
        Assert.That(candidates.RingCandidates[0].RejectionReason, Is.EqualTo("atom cannot contribute pi electrons"));
        Assert.That(candidates.CandidateRingIds, Is.Empty);
    }

    [Test]
    public void FindCandidates_RejectsFourElectronRing()
    {
        Molecule molecule = BuildAlternatingRing(4, 0);
        AromaticityModel model = new AromaticityModel();

        AromaticityCandidateSet candidates = model.FindCandidates(molecule);

        Assert.That(candidates.RingCandidates.Count, Is.EqualTo(1));
        Assert.That(candidates.RingCandidates[0].IsCandidate, Is.False);
        Assert.That(candidates.RingCandidates[0].ElectronCount, Is.EqualTo(4));
        Assert.That(candidates.RingCandidates[0].RejectionReason, Is.EqualTo("electron count is not 4n+2"));
    }

    [Test]
    public void FindCandidates_AcceptsPyridineLikeRing()
    {
        Molecule molecule = BuildAlternatingRing(6, 7);
        AromaticityModel model = new AromaticityModel();

        AromaticityCandidateSet candidates = model.FindCandidates(molecule);

        Assert.That(candidates.RingCandidates[0].IsCandidate, Is.True);
        Assert.That(candidates.RingCandidates[0].ElectronCount, Is.EqualTo(6));
        Assert.That(candidates.RingCandidates[0].AtomCandidates[0].AtomicNumber, Is.EqualTo(7));
        Assert.That(candidates.RingCandidates[0].AtomCandidates[0].ElectronContribution, Is.EqualTo(1));
    }

    [Test]
    public void FindCandidates_AcceptsFuranLikeRingWithLonePairAtom()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        builder.AddBond(0, 1, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(1, 2, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(2, 3, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(3, 4, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(4, 0, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        AromaticityModel model = new AromaticityModel();

        AromaticityCandidateSet candidates = model.FindCandidates(builder.Build());

        Assert.That(candidates.RingCandidates[0].IsCandidate, Is.True);
        Assert.That(candidates.RingCandidates[0].ElectronCount, Is.EqualTo(6));
        Assert.That(candidates.RingCandidates[0].AtomCandidates[0].AtomicNumber, Is.EqualTo(8));
        Assert.That(candidates.RingCandidates[0].AtomCandidates[0].ElectronContribution, Is.EqualTo(2));
        Assert.That(candidates.RingCandidates[0].AtomCandidates[0].Reason, Is.EqualTo("lone pair atom"));
    }

    [Test]
    public void FindCandidates_ReturnsEmptySetForNullMolecule()
    {
        AromaticityModel model = new AromaticityModel();
        Molecule? molecule = null;

        AromaticityCandidateSet candidates = model.FindCandidates(molecule);

        Assert.That(candidates.RingCandidates, Is.Empty);
        Assert.That(candidates.CandidateRingIds, Is.Empty);
        Assert.That(candidates.AtomIsCandidate(0), Is.False);
    }

    static Molecule BuildAlternatingRing(int size, int firstAtomicNumber)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < size)
        {
            int atomicNumber = 6;
            string label = "C";
            if (index == 0 && firstAtomicNumber > 0)
            {
                atomicNumber = firstAtomicNumber;
                label = "N";
            }

            builder.AddAtom(new AtomSpec(atomicNumber, 0, 0, 0, 0, 0, 0, false, "", label));
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
}
