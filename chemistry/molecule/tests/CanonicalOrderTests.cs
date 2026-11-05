using System.Text;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class CanonicalOrderTests
{
    [Test]
    public void FromMolecule_BuildsAtomAndBondInverseMaps()
    {
        Molecule molecule = BuildAsymmetricAmide();

        CanonicalOrder order = CanonicalOrder.FromMolecule(molecule);

        Assert.That(order.AtomIdsByCanonicalIndex.Count, Is.EqualTo(molecule.Atoms.Count));
        Assert.That(order.CanonicalAtomIndexByAtomId.Count, Is.EqualTo(molecule.Atoms.Count));
        Assert.That(order.BondIdsByCanonicalIndex.Count, Is.EqualTo(molecule.Bonds.Count));
        Assert.That(order.CanonicalBondIndexByBondId.Count, Is.EqualTo(molecule.Bonds.Count));

        int atomId = 0;
        while (atomId < molecule.Atoms.Count)
        {
            int canonicalAtomIndex = order.GetCanonicalAtomIndex(atomId);
            Assert.That(canonicalAtomIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(order.GetOriginalAtomId(canonicalAtomIndex), Is.EqualTo(atomId));
            atomId = atomId + 1;
        }

        int bondId = 0;
        while (bondId < molecule.Bonds.Count)
        {
            int canonicalBondIndex = order.GetCanonicalBondIndex(bondId);
            Assert.That(canonicalBondIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(order.GetOriginalBondId(canonicalBondIndex), Is.EqualTo(bondId));
            bondId = bondId + 1;
        }
    }

    [Test]
    public void FromMolecule_ProducesStableCanonicalSignatureUnderAtomRenumbering()
    {
        Molecule forward = BuildAsymmetricAmide();
        Molecule reversed = BuildAsymmetricAmideReversed();

        string forwardSignature = BuildCanonicalSignature(forward);
        string reversedSignature = BuildCanonicalSignature(reversed);

        Assert.That(forwardSignature, Is.EqualTo(reversedSignature));
    }

    [Test]
    public void FromMolecule_SortsBondsByCanonicalEndpointPair()
    {
        Molecule molecule = BuildShuffledBondOrderMolecule();

        CanonicalOrder order = CanonicalOrder.FromMolecule(molecule);

        int index = 1;
        while (index < order.BondIdsByCanonicalIndex.Count)
        {
            Assert.That(CompareCanonicalBondPairs(order, index - 1, index), Is.LessThanOrEqualTo(0));
            index = index + 1;
        }
    }

    [Test]
    public void FromMolecule_StoresCanonicalBondEndpointsInForwardOrder()
    {
        Molecule molecule = BuildShuffledBondOrderMolecule();

        CanonicalOrder order = CanonicalOrder.FromMolecule(molecule);

        int index = 0;
        while (index < order.BondIdsByCanonicalIndex.Count)
        {
            Assert.That(order.GetCanonicalBondBeginAtomIndex(index), Is.LessThanOrEqualTo(order.GetCanonicalBondEndAtomIndex(index)));
            index = index + 1;
        }
    }

    [Test]
    public void FromMolecule_ReturnsEmptyOrderForNullMolecule()
    {
        CanonicalOrder order = CanonicalOrder.FromMolecule(null);

        Assert.That(order.AtomIdsByCanonicalIndex, Is.Empty);
        Assert.That(order.BondIdsByCanonicalIndex, Is.Empty);
        Assert.That(order.GetOriginalAtomId(0), Is.EqualTo(-1));
        Assert.That(order.GetCanonicalAtomIndex(0), Is.EqualTo(-1));
        Assert.That(order.GetOriginalBondId(0), Is.EqualTo(-1));
        Assert.That(order.GetCanonicalBondIndex(0), Is.EqualTo(-1));
        Assert.That(order.GetCanonicalBondBeginAtomIndex(0), Is.EqualTo(-1));
        Assert.That(order.GetCanonicalBondEndAtomIndex(0), Is.EqualTo(-1));
    }

    [Test]
    public void CanonicalizerFacade_ReturnsCanonicalOrderAndConsistentExactIdentity()
    {
        Molecule molecule = BuildAsymmetricAmide();
        Molecule clone = MoleculeBuilder.CloneMolecule(molecule);
        ICanonicalizer canonicalizer = new Canonicalizer();

        CanonicalOrder order = canonicalizer.GetCanonicalOrder(molecule);
        CanonicalOrder nullOrder = canonicalizer.GetCanonicalOrder(null);
        string exact = canonicalizer.GetIdentityHash(molecule);
        string policyExact = canonicalizer.GetIdentityHashWithPolicy(molecule, IdentityPolicy.Exact);

        Assert.That(order.AtomIdsByCanonicalIndex, Has.Count.EqualTo(molecule.Atoms.Count));
        Assert.That(nullOrder.AtomIdsByCanonicalIndex, Is.Empty);
        Assert.That(exact, Has.Length.EqualTo(64));
        Assert.That(policyExact, Is.EqualTo(exact));
        Assert.That(canonicalizer.HasSameIdentity(molecule, clone, IdentityPolicy.Exact), Is.True);
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

    static Molecule BuildShuffledBondOrderMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        builder.AddBond(chlorine, carbon, new BondSpec());
        builder.AddBond(carbon, nitrogen, new BondSpec());
        builder.AddBond(oxygen, carbon, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static string BuildCanonicalSignature(Molecule molecule)
    {
        CanonicalOrder order = CanonicalOrder.FromMolecule(molecule);
        StringBuilder builder = new StringBuilder();

        int atomIndex = 0;
        while (atomIndex < order.AtomIdsByCanonicalIndex.Count)
        {
            Atom atom = molecule.Atoms[order.GetOriginalAtomId(atomIndex)];
            builder.Append("A");
            builder.Append(atom.AtomicNumber);
            builder.Append(":");
            builder.Append(atom.FormalCharge);
            builder.Append(":");
            builder.Append(atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount);
            builder.Append(":");
            builder.Append(molecule.GetDegree(atom.Id));
            builder.Append(";");
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < order.BondIdsByCanonicalIndex.Count)
        {
            Bond bond = molecule.Bonds[order.GetOriginalBondId(bondIndex)];
            builder.Append("B");
            builder.Append(order.GetCanonicalBondBeginAtomIndex(bondIndex));
            builder.Append("-");
            builder.Append(order.GetCanonicalBondEndAtomIndex(bondIndex));
            builder.Append(":");
            builder.Append((int)bond.Order);
            builder.Append(":");
            builder.Append(bond.IsAromatic);
            builder.Append(";");
            bondIndex = bondIndex + 1;
        }

        return builder.ToString();
    }

    static int CompareCanonicalBondPairs(CanonicalOrder order, int leftIndex, int rightIndex)
    {
        int comparison = CompareInt(order.GetCanonicalBondBeginAtomIndex(leftIndex), order.GetCanonicalBondBeginAtomIndex(rightIndex));
        if (comparison != 0)
        {
            return comparison;
        }

        return CompareInt(order.GetCanonicalBondEndAtomIndex(leftIndex), order.GetCanonicalBondEndAtomIndex(rightIndex));
    }

    static int CompareInt(int left, int right)
    {
        if (left < right)
        {
            return -1;
        }

        if (left > right)
        {
            return 1;
        }

        return 0;
    }
}
