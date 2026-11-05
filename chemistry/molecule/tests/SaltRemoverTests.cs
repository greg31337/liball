using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class SaltRemoverTests
{
    [Test]
    public void RemoveSaltsWithResult_RemovesKnownSaltFragmentsAndKeepsLargestParent()
    {
        SaltRemover remover = new SaltRemover();
        Molecule molecule = BuildEthanolSodiumChloride();

        SaltRemovalResult result = remover.RemoveSaltsWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.InputFragmentCount, Is.EqualTo(3));
        Assert.That(result.MatchedSaltFragmentCount, Is.EqualTo(2));
        Assert.That(result.RemovedFragmentCount, Is.EqualTo(2));
        Assert.That(result.RetainedFragmentCount, Is.EqualTo(1));
        Assert.That(result.Changed, Is.True);
        Assert.That(result.RemovedFragmentNames, Is.EqualTo(new string[] { "sodium", "chloride" }));
        Assert.That(result.Parent.GetAtomCount(), Is.EqualTo(3));
        Assert.That(new CanonicalSmilesWriter().Write(result.Parent), Is.EqualTo("OCC"));
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(5));
    }

    [Test]
    public void RemoveSaltsWithResult_DoesNotDeleteEveryFragmentWhenAllAreSalts()
    {
        SaltRemover remover = new SaltRemover();
        Molecule molecule = BuildSodiumChloride();

        SaltRemovalResult result = remover.RemoveSaltsWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.InputFragmentCount, Is.EqualTo(2));
        Assert.That(result.MatchedSaltFragmentCount, Is.EqualTo(2));
        Assert.That(result.RemovedFragmentCount, Is.EqualTo(0));
        Assert.That(result.RetainedFragmentCount, Is.EqualTo(2));
        Assert.That(result.AllFragmentsMatchedSalts, Is.True);
        Assert.That(result.Changed, Is.False);
        Assert.That(result.Parent.GetAtomCount(), Is.EqualTo(2));
    }

    [Test]
    public void RemoveSaltsWithResult_UsesFragmentChooserWhenNoKnownSaltIsPresent()
    {
        SmilesParser parser = new SmilesParser();
        SaltRemover remover = new SaltRemover();
        Molecule molecule = parser.Parse("CC.CCO");

        SaltRemovalResult result = remover.RemoveSaltsWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.InputFragmentCount, Is.EqualTo(2));
        Assert.That(result.MatchedSaltFragmentCount, Is.EqualTo(0));
        Assert.That(result.RemovedFragmentCount, Is.EqualTo(0));
        Assert.That(result.RetainedFragmentCount, Is.EqualTo(2));
        Assert.That(result.Changed, Is.True);
        Assert.That(new CanonicalSmilesWriter().Write(result.Parent), Is.EqualTo("OCC"));
    }

    [Test]
    public void IsKnownSaltFragment_MatchesCanonicalizedCommonFragments()
    {
        SaltRemover remover = new SaltRemover();

        Assert.That(remover.IsKnownSaltFragment(BuildSingleAtom(17, -1, "Cl")), Is.True);
        Assert.That(remover.FindKnownSaltName(BuildSingleAtom(11, 1, "Na")), Is.EqualTo("sodium"));
        Assert.That(remover.IsKnownSaltFragment(new SmilesParser().Parse("CCO")), Is.False);
    }

    [Test]
    public void ParentMoleculeFactory_CreatesFragmentParentByRemovingSalts()
    {
        ParentMoleculeFactory factory = new ParentMoleculeFactory();
        Molecule molecule = BuildEthanolSodiumChloride();

        Molecule parent = factory.CreateParent(molecule, IdentityPolicy.FragmentParent);

        Assert.That(new CanonicalSmilesWriter().Write(parent), Is.EqualTo("OCC"));
        Assert.That(parent.GetAtomCount(), Is.EqualTo(3));
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(5));
    }

    [Test]
    public void RemoveSaltsWithResult_ReportsNullInput()
    {
        SaltRemover remover = new SaltRemover();

        SaltRemovalResult result = remover.RemoveSaltsWithResult(null);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("SALT_REMOVAL_NULL_MOLECULE"));
        Assert.That(result.Parent.GetAtomCount(), Is.EqualTo(0));
    }

    [Test]
    public void RemoveSalts_EmptyMoleculeReturnsIndependentEmptyParent()
    {
        SaltRemover remover = new SaltRemover();
        Molecule source = new Molecule();

        Molecule parent = remover.RemoveSalts(source);
        SaltRemovalResult detailed = remover.RemoveSaltsWithResult(source);

        Assert.That(parent, Is.Not.SameAs(source));
        Assert.That(parent.GetAtomCount(), Is.Zero);
        Assert.That(detailed.Success, Is.True);
        Assert.That(detailed.InputFragmentCount, Is.Zero);
        Assert.That(detailed.RetainedFragmentCount, Is.Zero);
        Assert.That(detailed.Changed, Is.False);
    }

    static Molecule BuildEthanolSodiumChloride()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonB, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        builder.AddAtom(new AtomSpec(17, 0, -1, 0, 0, 0, 0, false, "", "Cl"));
        return builder.Build();
    }

    static Molecule BuildSodiumChloride()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        builder.AddAtom(new AtomSpec(17, 0, -1, 0, 0, 0, 0, false, "", "Cl"));
        return builder.Build();
    }

    static Molecule BuildSingleAtom(int atomicNumber, int formalCharge, string label)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(atomicNumber, 0, formalCharge, 0, 0, 0, 0, false, "", label));
        return builder.Build();
    }
}
