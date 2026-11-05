using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class CanonicalIdentityOptionsTests
{
    [Test]
    public void IdentityOptions_DefaultToExactIndependentParticipation()
    {
        IdentityOptions options = new IdentityOptions();
        Molecule molecule = BuildSingleAtom(6, 13, 1, 2, 7, true);
        ICanonicalizer canonicalizer = new Canonicalizer();

        string exact = canonicalizer.GetIdentityHash(molecule);
        string configured = canonicalizer.GetIdentityHashWithOptions(molecule, options);

        Assert.That(options.IncludeIsotopes, Is.True);
        Assert.That(options.IncludeCharges, Is.True);
        Assert.That(options.IncludeHydrogens, Is.True);
        Assert.That(options.IncludeAtomMaps, Is.True);
        Assert.That(options.IncludeAromaticity, Is.True);
        Assert.That(options.IncludeStereo, Is.True);
        Assert.That(configured, Is.EqualTo(exact));
    }

    [Test]
    public void IdentityOptions_IndependentlyControlIsotopeChargeHydrogenAndAtomMap()
    {
        ICanonicalizer canonicalizer = new Canonicalizer();
        Molecule baseline = BuildSingleAtom(6, 12, 0, 4, 0, false);
        Molecule isotope = BuildSingleAtom(6, 13, 0, 4, 0, false);
        Molecule charge = BuildSingleAtom(6, 12, 1, 4, 0, false);
        Molecule hydrogen = BuildSingleAtom(6, 12, 0, 3, 0, false);
        Molecule atomMap = BuildSingleAtom(6, 12, 0, 4, 42, false);
        IdentityOptions isotopeOptions = new IdentityOptions();
        isotopeOptions.IncludeIsotopes = false;
        IdentityOptions chargeOptions = new IdentityOptions();
        chargeOptions.IncludeCharges = false;
        IdentityOptions hydrogenOptions = new IdentityOptions();
        hydrogenOptions.IncludeHydrogens = false;
        IdentityOptions atomMapOptions = new IdentityOptions();
        atomMapOptions.IncludeAtomMaps = false;

        Assert.That(canonicalizer.HasSameIdentityWithOptions(baseline, isotope, null), Is.False);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(baseline, charge, null), Is.False);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(baseline, hydrogen, null), Is.False);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(baseline, atomMap, null), Is.False);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(baseline, isotope, isotopeOptions), Is.True);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(baseline, charge, chargeOptions), Is.True);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(baseline, hydrogen, hydrogenOptions), Is.True);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(baseline, atomMap, atomMapOptions), Is.True);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(baseline, isotope, chargeOptions), Is.False);
    }

    [Test]
    public void IdentityOptions_IndependentlyControlAromaticityAndStereo()
    {
        ICanonicalizer canonicalizer = new Canonicalizer();
        Molecule single = BuildTwoCarbonBond(false, false);
        Molecule aromatic = BuildTwoCarbonBond(true, false);
        Molecule plainStereo = BuildTwoCarbonBond(false, false);
        Molecule assignedStereo = BuildTwoCarbonBond(false, true);
        IdentityOptions aromaticityOptions = new IdentityOptions();
        aromaticityOptions.IncludeAromaticity = false;
        IdentityOptions stereoOptions = new IdentityOptions();
        stereoOptions.IncludeStereo = false;

        Assert.That(canonicalizer.HasSameIdentityWithOptions(single, aromatic, null), Is.False);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(single, aromatic, aromaticityOptions), Is.True);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(plainStereo, assignedStereo, null), Is.False);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(plainStereo, assignedStereo, stereoOptions), Is.True);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(single, aromatic, stereoOptions), Is.False);
    }

    [Test]
    public void AtomMapParticipation_RemainsCanonicalAcrossEquivalentAtomOrdering()
    {
        Molecule forward = BuildMappedCarbonBond(1, 2);
        Molecule reversed = BuildMappedCarbonBond(2, 1);
        Molecule different = BuildMappedCarbonBond(1, 3);
        IdentityOptions withoutMaps = new IdentityOptions();
        withoutMaps.IncludeAtomMaps = false;
        ICanonicalizer canonicalizer = new Canonicalizer();

        Assert.That(canonicalizer.HasSameIdentityWithOptions(forward, reversed, null), Is.True);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(forward, different, null), Is.False);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(forward, different, withoutMaps), Is.True);
    }

    [Test]
    public void ExcludingHydrogens_NormalizesExplicitAtomsAndStoredCounts()
    {
        Molecule implicitMethane = BuildSingleAtom(6, 0, 0, 4, 0, false);
        Molecule explicitMethane = BuildExplicitMethane();
        IdentityOptions options = new IdentityOptions();
        options.IncludeHydrogens = false;
        ICanonicalizer canonicalizer = new Canonicalizer();

        Assert.That(canonicalizer.HasSameIdentityWithOptions(implicitMethane, explicitMethane, null), Is.False);
        Assert.That(canonicalizer.HasSameIdentityWithOptions(implicitMethane, explicitMethane, options), Is.True);
    }

    [Test]
    public void StructuralHash_RecordsNondefaultParticipationOptions()
    {
        Molecule molecule = BuildSingleAtom(6, 0, 0, 4, 0, false);
        IdentityOptions options = new IdentityOptions();
        options.IncludeCharges = false;
        options.IncludeAtomMaps = false;
        IStructuralHash hash = new StructuralHash();

        string text = hash.BuildCanonicalTextWithOptions(molecule, options);
        string identifier = hash.ComputeWithOptions(molecule, options);

        Assert.That(text, Does.StartWith("molecule-identity-options-v1|"));
        Assert.That(text, Does.Contain("charge=0"));
        Assert.That(text, Does.Contain("atom-map=0"));
        Assert.That(identifier, Has.Length.EqualTo(64));
        Assert.That(identifier, Is.Not.EqualTo(hash.ComputeExact(molecule)));
    }

    [Test]
    public void GetComponentSortedIdentifiers_IsInvariantToMixtureComponentOrder()
    {
        Molecule forward = BuildMixture(false);
        Molecule reversed = BuildMixture(true);
        ICanonicalizer canonicalizer = new Canonicalizer();

        MixtureIdentityResult forwardResult = canonicalizer.GetComponentSortedIdentifiers(forward, null);
        MixtureIdentityResult reversedResult = canonicalizer.GetComponentSortedIdentifiers(reversed, null);

        Assert.That(forwardResult.Success, Is.True);
        Assert.That(forwardResult.ComponentCount, Is.EqualTo(3));
        Assert.That(forwardResult.ComponentIdentifiers, Is.EqualTo(reversedResult.ComponentIdentifiers));
        Assert.That(forwardResult.Identifier, Is.EqualTo(reversedResult.Identifier));
        Assert.That(forwardResult.Identifier, Has.Length.EqualTo(64));
        Assert.That(forwardResult.ComponentIdentifiers[0], Is.LessThanOrEqualTo(forwardResult.ComponentIdentifiers[1]));
        Assert.That(forwardResult.ComponentIdentifiers[1], Is.LessThanOrEqualTo(forwardResult.ComponentIdentifiers[2]));
        bool hasRepeatedIdentifier = forwardResult.ComponentIdentifiers[0] == forwardResult.ComponentIdentifiers[1]
            || forwardResult.ComponentIdentifiers[1] == forwardResult.ComponentIdentifiers[2];
        Assert.That(hasRepeatedIdentifier, Is.True);
    }

    [Test]
    public void ComponentSortedIdentifiers_ApplyOptionsBeforeSplittingAndReportNull()
    {
        Molecule mixture = BuildHydrogenMixture();
        IdentityOptions options = new IdentityOptions();
        options.IncludeHydrogens = false;
        ICanonicalizer canonicalizer = new Canonicalizer();

        MixtureIdentityResult normalized = canonicalizer.GetComponentSortedIdentifiers(mixture, options);
        MixtureIdentityResult nullResult = canonicalizer.GetComponentSortedIdentifiers(null, options);

        Assert.That(normalized.Success, Is.True);
        Assert.That(normalized.ComponentCount, Is.EqualTo(1));
        Assert.That(nullResult.Success, Is.False);
        Assert.That(nullResult.ComponentIdentifiers, Is.Empty);
        Assert.That(nullResult.Report.Errors[0].Code, Is.EqualTo("IDENTITY_NULL_MOLECULE"));
    }

    [Test]
    public void ComponentSortedIdentifiers_ReturnStableIdentityForEmptyMolecule()
    {
        ICanonicalizer canonicalizer = new Canonicalizer();

        MixtureIdentityResult first = canonicalizer.GetComponentSortedIdentifiers(new Molecule(), null);
        MixtureIdentityResult second = canonicalizer.GetComponentSortedIdentifiers(new Molecule(), new IdentityOptions());

        Assert.That(first.Success, Is.True);
        Assert.That(first.ComponentCount, Is.EqualTo(0));
        Assert.That(first.ComponentIdentifiers, Is.Empty);
        Assert.That(first.Identifier, Has.Length.EqualTo(64));
        Assert.That(second.Identifier, Is.EqualTo(first.Identifier));
    }

    [Test]
    public void DuplicateDetector_UsesConfiguredParticipationOptions()
    {
        IdentityOptions options = new IdentityOptions();
        options.IncludeAtomMaps = false;
        IDuplicateDetector detector = new DuplicateDetector();
        detector.Configure(options);
        Molecule unmapped = BuildSingleAtom(6, 0, 0, 4, 0, false);
        Molecule mapped = BuildSingleAtom(6, 0, 0, 4, 99, false);

        DuplicateResult first = detector.Add(unmapped);
        DuplicateResult second = detector.Add(mapped);

        Assert.That(first.IsDuplicate, Is.False);
        Assert.That(second.IsDuplicate, Is.True);
        Assert.That(second.Key, Is.EqualTo(first.Key));
    }

    static Molecule BuildSingleAtom(int atomicNumber, int isotope, int charge, int hydrogens, int atomMap, bool aromatic)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(atomicNumber, isotope, charge, 0, hydrogens, 0, atomMap, aromatic, "", ""));
        return builder.Build();
    }

    static Molecule BuildTwoCarbonBond(bool aromatic, bool stereo)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int left = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, aromatic, "", ""));
        int right = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, aromatic, "", ""));
        BondSpec spec = new BondSpec();
        if (aromatic)
        {
            spec.Order = BondOrder.Aromatic;
            spec.IsAromatic = true;
        }

        if (stereo)
        {
            spec.Stereo = BondStereo.Wedge;
            spec.Direction = BondDirection.Up;
        }

        builder.AddBond(left, right, spec);
        Molecule molecule = builder.Build();
        if (stereo)
        {
            StereoRecord record = new StereoRecord();
            record.Id = 0;
            record.SetTetrahedral(left, right, -1, -1, -1, Chirality.Clockwise);
            molecule.StereoRecords.Add(record);
        }

        return molecule;
    }

    static Molecule BuildExplicitMethane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", ""));
        int index = 0;
        while (index < 4)
        {
            int hydrogen = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", ""));
            builder.AddBond(carbon, hydrogen, new BondSpec());
            index = index + 1;
        }

        return builder.Build();
    }

    static Molecule BuildMappedCarbonBond(int leftMap, int rightMap)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int left = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, leftMap, false, "", ""));
        int right = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, rightMap, false, "", ""));
        builder.AddBond(left, right, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildMixture(bool reversed)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        if (reversed)
        {
            AddNitrogenComponent(builder);
            AddCarbonOxygenComponent(builder);
            AddNitrogenComponent(builder);
        }
        else
        {
            AddCarbonOxygenComponent(builder);
            AddNitrogenComponent(builder);
            AddNitrogenComponent(builder);
        }

        return builder.Build();
    }

    static void AddCarbonOxygenComponent(MoleculeBuilder builder)
    {
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", ""));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", ""));
        builder.AddBond(carbon, oxygen, new BondSpec());
    }

    static void AddNitrogenComponent(MoleculeBuilder builder)
    {
        builder.AddAtom(new AtomSpec(7, 0, 0, 0, 3, 0, 0, false, "", ""));
    }

    static Molecule BuildHydrogenMixture()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", ""));
        builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", ""));
        return builder.Build();
    }
}
