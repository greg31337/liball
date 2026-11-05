using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class ParentMoleculeFactoryTests
{
    [Test]
    public void CreateTautomerParent_CanonicalizesEnolToKetoWithoutChangingSource()
    {
        ParentMoleculeFactory factory = new ParentMoleculeFactory();
        Molecule molecule = BuildEnol();

        Molecule parent = factory.CreateTautomerParent(molecule);

        Assert.That(parent, Is.Not.SameAs(molecule));
        Assert.That(parent.Atoms[1].ImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(parent.Atoms[2].ImplicitHydrogenCount, Is.EqualTo(3));
        Assert.That(parent.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(parent.Bonds[1].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(molecule.Atoms[1].ImplicitHydrogenCount, Is.EqualTo(1));
        Assert.That(molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Single));
    }

    [Test]
    public void CreateParent_UsesTautomerParentForTautomerPolicies()
    {
        ParentMoleculeFactory factory = new ParentMoleculeFactory();
        Molecule molecule = BuildEnol();

        Molecule tautomerParent = factory.CreateParent(molecule, IdentityPolicy.TautomerParent);
        Molecule insensitiveParent = factory.CreateParent(molecule, IdentityPolicy.TautomerInsensitive);

        Assert.That(tautomerParent.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(insensitiveParent.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
    }

    [Test]
    public void CreateChargeParent_ClearsFormalChargesWithoutChangingSource()
    {
        ParentMoleculeFactory factory = new ParentMoleculeFactory();
        Molecule molecule = BuildChargedSingleAtom();

        Molecule parent = factory.CreateParent(molecule, IdentityPolicy.ChargeParent);

        Assert.That(parent.Atoms[0].FormalCharge, Is.EqualTo(0));
        Assert.That(molecule.Atoms[0].FormalCharge, Is.EqualTo(1));
    }

    [Test]
    public void CreateIsotopeParent_ClearsEveryIsotopeWithoutChangingSource()
    {
        ParentMoleculeFactory factory = new ParentMoleculeFactory();
        Molecule molecule = BuildIsotopicMolecule();

        Molecule parent = factory.CreateIsotopeParent(molecule);
        Molecule policyParent = factory.CreateParentWithOptions(molecule, IdentityPolicy.IsotopeParent, new StandardizationOptions());
        Molecule insensitiveParent = factory.CreateParent(molecule, IdentityPolicy.IsotopeInsensitive);

        Assert.That(parent, Is.Not.SameAs(molecule));
        Assert.That(AllIsotopesAreZero(parent), Is.True);
        Assert.That(AllIsotopesAreZero(policyParent), Is.True);
        Assert.That(AllIsotopesAreZero(insensitiveParent), Is.True);
        Assert.That(parent.Atoms[0].FormalCharge, Is.EqualTo(1));
        Assert.That(molecule.Atoms[0].IsotopeMassNumber, Is.EqualTo(13));
        Assert.That(molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(2));
    }

    [Test]
    public void CreateSuperParent_RemovesSaltStereoChargeIsotopesRadicalsAndCanonicalizesTautomer()
    {
        ParentMoleculeFactory factory = new ParentMoleculeFactory();
        Molecule molecule = BuildDecoratedEnolWithSalt();

        Molecule parent = factory.CreateSuperParent(molecule);

        Assert.That(parent.GetAtomCount(), Is.EqualTo(3));
        Assert.That(parent.Atoms[1].ImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(parent.Atoms[2].ImplicitHydrogenCount, Is.EqualTo(3));
        Assert.That(parent.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(parent.Bonds[1].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(AllChargesAreZero(parent), Is.True);
        Assert.That(AllIsotopesAreZero(parent), Is.True);
        Assert.That(AllRadicalsAreZero(parent), Is.True);
        Assert.That(parent.StereoRecords, Is.Empty);
        Assert.That(parent.Bonds[0].Direction, Is.EqualTo(BondDirection.None));
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(4));
        Assert.That(molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(18));
        Assert.That(molecule.Atoms[2].FormalCharge, Is.EqualTo(-1));
        Assert.That(molecule.Atoms[2].RadicalElectronCount, Is.EqualTo(1));
        Assert.That(molecule.StereoRecords.Count, Is.EqualTo(1));
    }

    [Test]
    public void CreateParent_UsesSuperParentForSuperParentPolicy()
    {
        ParentMoleculeFactory factory = new ParentMoleculeFactory();

        Molecule parent = factory.CreateParent(BuildDecoratedEnolWithSalt(), IdentityPolicy.SuperParent);

        Assert.That(parent.GetAtomCount(), Is.EqualTo(3));
        Assert.That(parent.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(AllChargesAreZero(parent), Is.True);
        Assert.That(AllIsotopesAreZero(parent), Is.True);
        Assert.That(AllRadicalsAreZero(parent), Is.True);
    }

    [Test]
    public void CreateParent_DispatchesEveryRemainingIdentityPolicy()
    {
        ParentMoleculeFactory factory = new ParentMoleculeFactory();
        Molecule source = BuildDecoratedEnolWithSalt();

        Molecule exact = factory.CreateParentWithOptions(source, IdentityPolicy.Exact, null);
        Molecule connectivity = factory.CreateParent(source, IdentityPolicy.Connectivity);
        Molecule fragment = factory.CreateParent(source, IdentityPolicy.FragmentParent);
        Molecule chargeInsensitive = factory.CreateParent(source, IdentityPolicy.ChargeInsensitive);
        Molecule stereoInsensitive = factory.CreateParent(source, IdentityPolicy.StereoInsensitive);

        Assert.That(exact, Is.Not.SameAs(source));
        Assert.That(exact.GetAtomCount(), Is.EqualTo(4));
        Assert.That(connectivity.GetAtomCount(), Is.EqualTo(4));
        Assert.That(fragment.GetAtomCount(), Is.EqualTo(3));
        Assert.That(AllChargesAreZero(chargeInsensitive), Is.True);
        Assert.That(chargeInsensitive.GetAtomCount(), Is.EqualTo(4));
        Assert.That(stereoInsensitive.StereoRecords, Is.Empty);
        Assert.That(stereoInsensitive.Bonds[0].Stereo, Is.EqualTo(BondStereo.None));
        Assert.That(source.GetAtomCount(), Is.EqualTo(4));
        Assert.That(source.StereoRecords, Has.Count.EqualTo(1));
    }

    [Test]
    public void ParentEntryPoints_NormalizeNullToIndependentEmptyMolecules()
    {
        ParentMoleculeFactory factory = new ParentMoleculeFactory();

        Molecule stereo = factory.CreateStereoParentWithOptions(null, null);
        Molecule fragment = factory.CreateFragmentParentWithOptions(null, null);
        Molecule charge = factory.CreateChargeParentWithOptions(null, null);
        Molecule isotope = factory.CreateIsotopeParentWithOptions(null, null);
        Molecule tautomer = factory.CreateTautomerParentWithOptions(null, null);
        Molecule super = factory.CreateSuperParentWithOptions(null, null);

        Assert.That(stereo.GetAtomCount(), Is.Zero);
        Assert.That(fragment.GetAtomCount(), Is.Zero);
        Assert.That(charge.GetAtomCount(), Is.Zero);
        Assert.That(isotope.GetAtomCount(), Is.Zero);
        Assert.That(tautomer.GetAtomCount(), Is.Zero);
        Assert.That(super.GetAtomCount(), Is.Zero);
        Assert.That(stereo, Is.Not.SameAs(fragment));
    }

    static Molecule BuildEnol()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int terminalCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbon, terminalCarbon, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildChargedSingleAtom()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(7, 0, 1, 0, 4, 0, 0, false, "", "N"));
        return builder.Build();
    }

    static Molecule BuildIsotopicMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 13, 1, 0, 3, 0, 0, false, "", "C"));
        int deuterium = builder.AddAtom(new AtomSpec(1, 2, 0, 0, 0, 0, 0, false, "", "H"));
        builder.AddBond(carbon, deuterium, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildDecoratedEnolWithSalt()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 18, 0, 0, 1, 0, 0, false, "", "O"));
        int terminalCarbon = builder.AddAtom(new AtomSpec(6, 0, -1, 0, 2, 1, 0, false, "", "C"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.Wedge, BondDirection.Up));
        builder.AddBond(carbon, terminalCarbon, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        Molecule molecule = builder.Build();
        StereoRecord record = new StereoRecord();
        record.Id = 0;
        record.SetTetrahedral(carbon, oxygen, terminalCarbon, -1, -1, Chirality.Clockwise);
        molecule.StereoRecords.Add(record);
        return molecule;
    }

    static bool AllChargesAreZero(Molecule molecule)
    {
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            if (molecule.Atoms[index].FormalCharge != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool AllIsotopesAreZero(Molecule molecule)
    {
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            if (molecule.Atoms[index].IsotopeMassNumber != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool AllRadicalsAreZero(Molecule molecule)
    {
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            if (molecule.Atoms[index].RadicalElectronCount != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
