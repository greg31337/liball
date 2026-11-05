using System.Collections.Generic;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class DuplicateDetectorTests
{
    [Test]
    public void Add_FindsExactDuplicateUsingCanonicalHash()
    {
        DuplicateDetector detector = new DuplicateDetector();

        DuplicateResult first = detector.Add(BuildAsymmetricAmide());
        DuplicateResult second = detector.Add(BuildAsymmetricAmideReversed());

        Assert.That(first.Success, Is.True);
        Assert.That(first.IsDuplicate, Is.False);
        Assert.That(first.InputIndex, Is.EqualTo(0));
        Assert.That(first.FirstIndex, Is.EqualTo(0));
        Assert.That(second.Success, Is.True);
        Assert.That(second.IsDuplicate, Is.True);
        Assert.That(second.InputIndex, Is.EqualTo(1));
        Assert.That(second.FirstIndex, Is.EqualTo(0));
        Assert.That(second.Key, Is.EqualTo(first.Key));
        Assert.That(detector.GetEntryCount(), Is.EqualTo(2));
    }

    [Test]
    public void Add_DoesNotMergeDifferentExactMolecules()
    {
        DuplicateDetector detector = new DuplicateDetector();

        DuplicateResult first = detector.Add(BuildKeto());
        DuplicateResult second = detector.Add(BuildEnol());

        Assert.That(first.IsDuplicate, Is.False);
        Assert.That(second.IsDuplicate, Is.False);
        Assert.That(second.Key, Is.Not.EqualTo(first.Key));
    }

    [Test]
    public void Add_UsesTautomerParentPolicyWhenConfigured()
    {
        IdentityOptions options = new IdentityOptions();
        options.Policy = IdentityPolicy.TautomerParent;
        DuplicateDetector detector = DuplicateDetector.Create(options);

        DuplicateResult first = detector.Add(BuildKeto());
        DuplicateResult second = detector.Add(BuildEnol());

        Assert.That(first.IsDuplicate, Is.False);
        Assert.That(second.IsDuplicate, Is.True);
        Assert.That(second.FirstIndex, Is.EqualTo(0));
        Assert.That(second.Key, Is.EqualTo(first.Key));
        Assert.That(second.Policy, Is.EqualTo(IdentityPolicy.TautomerParent));
    }

    [Test]
    public void Add_UsesIsotopeParentPolicyAndStoresNormalizedParent()
    {
        IdentityOptions options = new IdentityOptions();
        options.Policy = IdentityPolicy.IsotopeParent;
        DuplicateDetector detector = DuplicateDetector.Create(options);

        DuplicateResult first = detector.Add(BuildIsotopicCarbon(12));
        DuplicateResult second = detector.Add(BuildIsotopicCarbon(13));

        Assert.That(first.IsDuplicate, Is.False);
        Assert.That(second.IsDuplicate, Is.True);
        Assert.That(second.Policy, Is.EqualTo(IdentityPolicy.IsotopeParent));
        Assert.That(second.Molecule.Atoms[0].IsotopeMassNumber, Is.EqualTo(0));
    }

    [Test]
    public void AddMany_ReturnsResultsAndDuplicateGroups()
    {
        DuplicateDetector detector = new DuplicateDetector();
        List<Molecule> molecules = new List<Molecule>();
        molecules.Add(BuildSingleAtom(6, "C"));
        molecules.Add(BuildSingleAtom(8, "O"));
        molecules.Add(BuildSingleAtom(6, "C"));

        List<DuplicateResult> results = detector.AddMany(molecules);
        List<DuplicateGroup> groups = detector.GetDuplicateGroups();

        Assert.That(results.Count, Is.EqualTo(3));
        Assert.That(results[0].IsDuplicate, Is.False);
        Assert.That(results[1].IsDuplicate, Is.False);
        Assert.That(results[2].IsDuplicate, Is.True);
        Assert.That(results[2].FirstIndex, Is.EqualTo(0));
        Assert.That(groups.Count, Is.EqualTo(1));
        Assert.That(groups[0].FirstIndex, Is.EqualTo(0));
        Assert.That(groups[0].DuplicateIndices, Is.EqualTo(new int[] { 2 }));
    }

    [Test]
    public void Reset_ClearsDetectorState()
    {
        DuplicateDetector detector = new DuplicateDetector();
        detector.Add(BuildSingleAtom(6, "C"));

        detector.Reset();
        DuplicateResult result = detector.Add(BuildSingleAtom(6, "C"));

        Assert.That(detector.GetEntryCount(), Is.EqualTo(1));
        Assert.That(result.InputIndex, Is.EqualTo(0));
        Assert.That(result.IsDuplicate, Is.False);
    }

    [Test]
    public void Add_ReportsNullInputWithoutAddingEntry()
    {
        DuplicateDetector detector = new DuplicateDetector();

        DuplicateResult result = detector.Add(null);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("DUPLICATE_DETECTOR_NULL_MOLECULE"));
        Assert.That(detector.GetEntryCount(), Is.EqualTo(0));
    }

    [Test]
    public void AddString_UsesInjectedFormatService()
    {
        MoleculeFormats formats = new MoleculeFormats();
        FixedMoleculeReader reader = new FixedMoleculeReader();
        formats.RegisterReader(FormatKind.Smiles, reader);
        DuplicateDetector detector = DuplicateDetector.CreateWithFormats(new IdentityOptions(), formats);
        FormatOptions options = new FormatOptions();
        options.KindHint = FormatKind.Smiles;

        DuplicateResult first = detector.AddString("first", options);
        DuplicateResult second = detector.AddString("second", options);

        Assert.That(reader.ReadCount, Is.EqualTo(2));
        Assert.That(first.IsDuplicate, Is.False);
        Assert.That(second.IsDuplicate, Is.True);
    }

    [Test]
    public void Configure_CopiesPolicyAndClearsPreviouslyTrackedEntries()
    {
        DuplicateDetector detector = new DuplicateDetector();
        detector.Add(BuildSingleAtom(8, "O"));
        IdentityOptions options = new IdentityOptions();
        options.Policy = IdentityPolicy.ChargeInsensitive;

        detector.Configure(options);
        options.Policy = IdentityPolicy.Exact;
        DuplicateResult first = detector.Add(BuildChargedCarbon(1));
        DuplicateResult second = detector.Add(BuildChargedCarbon(0));

        Assert.That(first.InputIndex, Is.Zero);
        Assert.That(first.Policy, Is.EqualTo(IdentityPolicy.ChargeInsensitive));
        Assert.That(second.Policy, Is.EqualTo(IdentityPolicy.ChargeInsensitive));
        Assert.That(second.IsDuplicate, Is.True);
        Assert.That(detector.GetEntryCount(), Is.EqualTo(2));

        detector.ConfigureWithFormats(null, null);

        Assert.That(detector.GetEntryCount(), Is.Zero);
        Assert.That(detector.Add(BuildChargedCarbon(1)).Policy, Is.EqualTo(IdentityPolicy.Exact));
    }

    [Test]
    public void Add_HonorsConnectivityStereoAndFragmentParentPolicies()
    {
        IdentityOptions connectivityOptions = new IdentityOptions();
        connectivityOptions.Policy = IdentityPolicy.Connectivity;
        DuplicateDetector connectivity = DuplicateDetector.Create(connectivityOptions);
        connectivity.Add(BuildIsotopicChargedCarbon());

        IdentityOptions stereoOptions = new IdentityOptions();
        stereoOptions.Policy = IdentityPolicy.StereoInsensitive;
        DuplicateDetector stereo = DuplicateDetector.Create(stereoOptions);
        stereo.Add(BuildDirectionalBond(BondDirection.Up));

        IdentityOptions fragmentOptions = new IdentityOptions();
        fragmentOptions.Policy = IdentityPolicy.FragmentParent;
        DuplicateDetector fragment = DuplicateDetector.Create(fragmentOptions);
        fragment.Add(BuildCarbonWithSodium());

        Assert.That(connectivity.Add(BuildChargedCarbon(0)).IsDuplicate, Is.True);
        Assert.That(stereo.Add(BuildDirectionalBond(BondDirection.Down)).IsDuplicate, Is.True);
        Assert.That(fragment.Add(BuildChargedCarbon(0)).IsDuplicate, Is.True);
    }

    [Test]
    public void IsDuplicate_AddsEachProbeToDetectorState()
    {
        DuplicateDetector detector = new DuplicateDetector();

        bool first = detector.IsDuplicate(BuildSingleAtom(6, "C"));
        bool second = detector.IsDuplicate(BuildSingleAtom(6, "C"));

        Assert.That(first, Is.False);
        Assert.That(second, Is.True);
        Assert.That(detector.GetEntryCount(), Is.EqualTo(2));
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

    static Molecule BuildIsotopicCarbon(int isotope)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, isotope, 0, 0, 4, 0, 0, false, "", "C"));
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

    static Molecule BuildKeto()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int methylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(carbonylCarbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonylCarbon, methylCarbon, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
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

    static Molecule BuildSingleAtom(int atomicNumber, string label)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(atomicNumber, 0, 0, 0, 0, 0, 0, false, "", label));
        return builder.Build();
    }

    static Molecule BuildChargedCarbon(int charge)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, charge, 0, 4, 0, 0, false, "", "C"));
        return builder.Build();
    }

    static Molecule BuildIsotopicChargedCarbon()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 13, 1, 7, 2, 1, 1, true, "", "mapped"));
        return builder.Build();
    }

    static Molecule BuildDirectionalBond(BondDirection direction)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
        builder.AddBond(carbon, fluorine, BondSpec.Create(BondOrder.Single, false, BondStereo.Wedge, direction));
        return builder.Build();
    }

    static Molecule BuildCarbonWithSodium()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        return builder.Build();
    }

    class FixedMoleculeReader : IMoleculeReader
    {
        public int ReadCount;

        public Molecule Read(string? text)
        {
            ReadCount = ReadCount + 1;
            return BuildSingleAtom(6, "C");
        }
    }
}
