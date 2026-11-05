using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class CanonicalizationInvarianceTests
{
    [Test]
    public void CanonicalSmilesAndIdentityHash_AreInvariantUnderAtomRenumbering()
    {
        Molecule original = BuildBranchedAmide();
        Molecule renumbered = RenumberAtoms(original, new int[] { 4, 2, 0, 5, 1, 3 });
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();
        Canonicalizer canonicalizer = new Canonicalizer();

        Assert.That(writer.Write(original), Is.EqualTo(writer.Write(renumbered)));
        Assert.That(canonicalizer.GetIdentityHash(original), Is.EqualTo(canonicalizer.GetIdentityHash(renumbered)));
    }

    [Test]
    public void CanonicalSmilesAndIdentityHash_AreInvariantUnderMultipleAtomRenumberings()
    {
        Molecule original = BuildCyclohexanol();
        Molecule shifted = RenumberAtoms(original, new int[] { 3, 4, 5, 6, 0, 1, 2 });
        Molecule reversed = RenumberAtoms(original, new int[] { 6, 5, 4, 3, 2, 1, 0 });
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();
        Canonicalizer canonicalizer = new Canonicalizer();

        string originalSmiles = writer.Write(original);
        string originalHash = canonicalizer.GetIdentityHash(original);

        Assert.That(writer.Write(shifted), Is.EqualTo(originalSmiles));
        Assert.That(writer.Write(reversed), Is.EqualTo(originalSmiles));
        Assert.That(canonicalizer.GetIdentityHash(shifted), Is.EqualTo(originalHash));
        Assert.That(canonicalizer.GetIdentityHash(reversed), Is.EqualTo(originalHash));
    }

    [Test]
    public void CanonicalSmilesAndIdentityHash_AreInvariantUnderComponentReordering()
    {
        Molecule firstOrder = BuildDisconnectedComponents(false);
        Molecule secondOrder = BuildDisconnectedComponents(true);
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();
        Canonicalizer canonicalizer = new Canonicalizer();

        Assert.That(writer.Write(firstOrder), Is.EqualTo(writer.Write(secondOrder)));
        Assert.That(canonicalizer.GetIdentityHash(firstOrder), Is.EqualTo(canonicalizer.GetIdentityHash(secondOrder)));
    }

    [Test]
    public void CanonicalSmilesAndIdentityHash_AreInvariantUnderComponentReorderingAndAtomRenumbering()
    {
        Molecule original = BuildDisconnectedComponents(false);
        Molecule reordered = BuildDisconnectedComponents(true);
        Molecule renumbered = RenumberAtoms(reordered, new int[] { 5, 4, 3, 2, 1, 0 });
        CanonicalSmilesWriter writer = new CanonicalSmilesWriter();
        Canonicalizer canonicalizer = new Canonicalizer();

        Assert.That(writer.Write(renumbered), Is.EqualTo(writer.Write(original)));
        Assert.That(canonicalizer.GetIdentityHash(renumbered), Is.EqualTo(canonicalizer.GetIdentityHash(original)));
    }

    static Molecule BuildBranchedAmide()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int methyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 1, 0, 0, false, "", "N"));
        int ethylA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int ethylB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(methyl, carbonyl, new BondSpec());
        builder.AddBond(carbonyl, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonyl, nitrogen, new BondSpec());
        builder.AddBond(nitrogen, ethylA, new BondSpec());
        builder.AddBond(ethylA, ethylB, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildCyclohexanol()
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

            builder.AddBond(index, next, new BondSpec());
            index = index + 1;
        }

        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(0, oxygen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildDisconnectedComponents(bool reversed)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        if (reversed)
        {
            AddWaterComponent(builder);
            AddAcetaldehydeComponent(builder);
        }
        else
        {
            AddAcetaldehydeComponent(builder);
            AddWaterComponent(builder);
        }

        return builder.Build();
    }

    static void AddAcetaldehydeComponent(MoleculeBuilder builder)
    {
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonyl, new BondSpec());
        builder.AddBond(carbonyl, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
    }

    static void AddWaterComponent(MoleculeBuilder builder)
    {
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", "O"));
        int hydrogenA = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        int hydrogenB = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        builder.AddBond(oxygen, hydrogenA, new BondSpec());
        builder.AddBond(oxygen, hydrogenB, new BondSpec());
    }

    static Molecule RenumberAtoms(Molecule source, int[] newToOldAtomIds)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int[] oldToNewAtomIds = new int[source.Atoms.Count];
        int index = 0;
        while (index < oldToNewAtomIds.Length)
        {
            oldToNewAtomIds[index] = -1;
            index = index + 1;
        }

        index = 0;
        while (index < newToOldAtomIds.Length)
        {
            int oldAtomId = newToOldAtomIds[index];
            oldToNewAtomIds[oldAtomId] = index;
            builder.AddAtom(CreateAtomSpec(source.Atoms[oldAtomId]));
            index = index + 1;
        }

        int bondIndex = 0;
        while (bondIndex < source.Bonds.Count)
        {
            Bond sourceBond = source.Bonds[bondIndex];
            int beginAtomId = oldToNewAtomIds[sourceBond.BeginAtomId];
            int endAtomId = oldToNewAtomIds[sourceBond.EndAtomId];
            builder.AddBond(beginAtomId, endAtomId, CreateBondSpec(sourceBond));
            bondIndex = bondIndex + 1;
        }

        return builder.Build();
    }

    static AtomSpec CreateAtomSpec(Atom atom)
    {
        AtomSpec spec = new AtomSpec(atom.AtomicNumber, atom.IsotopeMassNumber, atom.FormalCharge, atom.ExplicitHydrogenCount, atom.ImplicitHydrogenCount, atom.RadicalElectronCount, atom.AtomMapNumber, atom.IsAromatic, atom.Alias, atom.Label);
        spec.QueryOperation = atom.QueryOperation;
        spec.QueryValue = atom.QueryValue;
        return spec;
    }

    static BondSpec CreateBondSpec(Bond bond)
    {
        BondSpec spec = BondSpec.Create(bond.Order, bond.IsAromatic, bond.Stereo, bond.Direction);
        spec.IsDative = bond.IsDative;
        spec.QueryOperation = bond.QueryOperation;
        spec.QueryValue = bond.QueryValue;
        spec.SourceFlags = bond.SourceFlags;
        return spec;
    }
}
