using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class IdentityPolicyHashTests
{
    [Test]
    public void ConnectivityPolicy_IgnoresBondOrderChargeIsotopeHydrogenAndStereo()
    {
        Molecule exact = BuildCarbonOxygen(BondOrder.Single, 0, 0, 3, BondDirection.None);
        Molecule variant = BuildCarbonOxygen(BondOrder.Double, 1, 13, 0, BondDirection.Up);
        Canonicalizer canonicalizer = new Canonicalizer();

        Assert.That(canonicalizer.HasSameIdentity(exact, variant, IdentityPolicy.Exact), Is.False);
        Assert.That(canonicalizer.HasSameIdentity(exact, variant, IdentityPolicy.Connectivity), Is.True);
    }

    [Test]
    public void StereoInsensitivePolicy_IgnoresBondAndRecordStereoOnly()
    {
        Molecule up = BuildStereoMolecule(BondDirection.Up, Chirality.Clockwise);
        Molecule down = BuildStereoMolecule(BondDirection.Down, Chirality.CounterClockwise);
        Canonicalizer canonicalizer = new Canonicalizer();

        Assert.That(canonicalizer.HasSameIdentity(up, down, IdentityPolicy.Exact), Is.False);
        Assert.That(canonicalizer.HasSameIdentity(up, down, IdentityPolicy.StereoInsensitive), Is.True);
    }

    [Test]
    public void ChargeInsensitivePolicy_IgnoresFormalChargeButKeepsIsotope()
    {
        Molecule neutral = BuildSingleAtom(new AtomSpec(6, 12, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule charged = BuildSingleAtom(new AtomSpec(6, 12, 1, 0, 3, 0, 0, false, "", "C"));
        Molecule isotope = BuildSingleAtom(new AtomSpec(6, 13, 0, 0, 4, 0, 0, false, "", "C"));
        Canonicalizer canonicalizer = new Canonicalizer();

        Assert.That(canonicalizer.HasSameIdentity(neutral, charged, IdentityPolicy.ChargeInsensitive), Is.False);
        Assert.That(canonicalizer.HasSameIdentity(neutral, isotope, IdentityPolicy.ChargeInsensitive), Is.False);
    }

    [Test]
    public void ChargeInsensitivePolicy_IgnoresChargeWhenHydrogenCountsMatch()
    {
        Molecule neutral = BuildSingleAtom(new AtomSpec(6, 12, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule charged = BuildSingleAtom(new AtomSpec(6, 12, 1, 0, 4, 0, 0, false, "", "C"));
        Canonicalizer canonicalizer = new Canonicalizer();

        Assert.That(canonicalizer.HasSameIdentity(neutral, charged, IdentityPolicy.Exact), Is.False);
        Assert.That(canonicalizer.HasSameIdentity(neutral, charged, IdentityPolicy.ChargeInsensitive), Is.True);
    }

    [Test]
    public void IsotopeInsensitivePolicy_IgnoresIsotopeButKeepsCharge()
    {
        Molecule carbon12 = BuildSingleAtom(new AtomSpec(6, 12, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule carbon13 = BuildSingleAtom(new AtomSpec(6, 13, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule charged = BuildSingleAtom(new AtomSpec(6, 12, 1, 0, 4, 0, 0, false, "", "C"));
        Canonicalizer canonicalizer = new Canonicalizer();

        Assert.That(canonicalizer.HasSameIdentity(carbon12, carbon13, IdentityPolicy.Exact), Is.False);
        Assert.That(canonicalizer.HasSameIdentity(carbon12, carbon13, IdentityPolicy.IsotopeInsensitive), Is.True);
        Assert.That(canonicalizer.HasSameIdentity(carbon12, charged, IdentityPolicy.IsotopeInsensitive), Is.False);
    }

    [Test]
    public void ParentPolicies_NormalizeFragmentChargeStereoIsotopeAndTautomerBeforeHashing()
    {
        Canonicalizer canonicalizer = new Canonicalizer();
        Molecule neutral = BuildSingleAtom(new AtomSpec(6, 12, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule charged = BuildSingleAtom(new AtomSpec(6, 12, 1, 0, 4, 0, 0, false, "", "C"));
        Molecule isotope = BuildSingleAtom(new AtomSpec(6, 13, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule stereoUp = BuildStereoMolecule(BondDirection.Up, Chirality.Clockwise);
        Molecule stereoDown = BuildStereoMolecule(BondDirection.Down, Chirality.CounterClockwise);

        Assert.That(canonicalizer.HasSameIdentity(BuildKetoForm(), BuildKetoWithSodium(), IdentityPolicy.FragmentParent), Is.True);
        Assert.That(canonicalizer.HasSameIdentity(neutral, charged, IdentityPolicy.ChargeParent), Is.True);
        Assert.That(canonicalizer.HasSameIdentity(stereoUp, stereoDown, IdentityPolicy.StereoParent), Is.True);
        Assert.That(canonicalizer.HasSameIdentity(neutral, isotope, IdentityPolicy.IsotopeParent), Is.True);
        Assert.That(canonicalizer.HasSameIdentity(BuildKetoForm(), BuildEnolForm(), IdentityPolicy.TautomerParent), Is.True);
    }

    [Test]
    public void SuperParentPolicy_NormalizesAllParentDimensionsBeforeHashing()
    {
        Canonicalizer canonicalizer = new Canonicalizer();

        bool same = canonicalizer.HasSameIdentity(BuildKetoForm(), BuildDecoratedEnolWithSodium(), IdentityPolicy.SuperParent);
        string text = new StructuralHash().BuildCanonicalTextWithPolicy(BuildDecoratedEnolWithSodium(), IdentityPolicy.SuperParent);

        Assert.That(same, Is.True);
        Assert.That(text, Does.Contain("policy=super-parent"));
        Assert.That(text, Does.Contain("atoms=3"));
    }

    [Test]
    public void TautomerInsensitivePolicy_IgnoresHydrogenChargeAndBondOrderPlacement()
    {
        Molecule keto = BuildKetoForm();
        Molecule enol = BuildEnolForm();
        Canonicalizer canonicalizer = new Canonicalizer();

        Assert.That(canonicalizer.HasSameIdentity(keto, enol, IdentityPolicy.Exact), Is.False);
        Assert.That(canonicalizer.HasSameIdentity(keto, enol, IdentityPolicy.TautomerInsensitive), Is.True);
    }

    [Test]
    public void BuildCanonicalText_RecordsIdentityPolicyName()
    {
        Molecule molecule = BuildSingleAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        StructuralHash structuralHash = new StructuralHash();

        string exactText = structuralHash.BuildCanonicalTextWithPolicy(molecule, IdentityPolicy.Exact);
        string connectivityText = structuralHash.BuildCanonicalTextWithPolicy(molecule, IdentityPolicy.Connectivity);

        Assert.That(exactText, Does.Contain("policy=exact"));
        Assert.That(connectivityText, Does.Contain("policy=connectivity"));
        Assert.That(exactText, Is.Not.EqualTo(connectivityText));
    }

    static Molecule BuildCarbonOxygen(BondOrder order, int charge, int isotope, int hydrogenCount, BondDirection direction)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, isotope, charge, 0, hydrogenCount, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(order, false, BondStereo.Wedge, direction));
        return builder.Build();
    }

    static Molecule BuildStereoMolecule(BondDirection direction, Chirality chirality)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, 0, 0, 0, 0, 0, false, "", "Cl"));
        int bromine = builder.AddAtom(new AtomSpec(35, 0, 0, 0, 0, 0, 0, false, "", "Br"));
        int iodine = builder.AddAtom(new AtomSpec(53, 0, 0, 0, 0, 0, 0, false, "", "I"));
        builder.AddBond(carbon, fluorine, BondSpec.Create(BondOrder.Single, false, BondStereo.Wedge, direction));
        builder.AddBond(carbon, chlorine, new BondSpec());
        builder.AddBond(carbon, bromine, new BondSpec());
        builder.AddBond(carbon, iodine, new BondSpec());
        Molecule molecule = builder.Build();
        StereoRecord record = new StereoRecord();
        record.Id = 0;
        record.SetTetrahedral(carbon, fluorine, chlorine, bromine, iodine, chirality);
        molecule.StereoRecords.Add(record);
        return molecule;
    }

    static Molecule BuildSingleAtom(AtomSpec spec)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(spec);
        return builder.Build();
    }

    static Molecule BuildKetoForm()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, new BondSpec());
        builder.AddBond(carbonB, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildEnolForm()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonB, oxygen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildKetoWithSodium()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, new BondSpec());
        builder.AddBond(carbonB, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        return builder.Build();
    }

    static Molecule BuildDecoratedEnolWithSodium()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 1, 0, 2, 1, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 18, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, BondSpec.Create(BondOrder.Double, false, BondStereo.Wedge, BondDirection.Up));
        builder.AddBond(carbonB, oxygen, new BondSpec());
        builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        Molecule molecule = builder.Build();
        StereoRecord record = new StereoRecord();
        record.Id = 0;
        record.SetTetrahedral(carbonA, carbonB, oxygen, -1, -1, Chirality.Clockwise);
        molecule.StereoRecords.Add(record);
        return molecule;
    }
}
