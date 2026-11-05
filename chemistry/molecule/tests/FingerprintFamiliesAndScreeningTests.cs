using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class FingerprintFamiliesAndScreeningTests
{
    [Test]
    public void Generator_ExposesBitCountSparseAndFoldedFormsForEveryFamily()
    {
        FingerprintGenerator generator = new FingerprintGenerator();
        FingerprintOptions options = CreateOptions();
        Molecule molecule = BuildEthanol();

        AssertFamilyForms(
            generator.CreatePathFingerprint(molecule, options),
            generator.CreatePathCountFingerprint(molecule, options),
            generator.CreatePathSparseFingerprint(molecule, options),
            generator.CreatePathFoldedFingerprint(molecule, options),
            512,
            32);
        AssertFamilyForms(
            generator.CreateCircularFingerprint(molecule, options),
            generator.CreateCircularCountFingerprint(molecule, options),
            generator.CreateCircularSparseFingerprint(molecule, options),
            generator.CreateCircularFoldedFingerprint(molecule, options),
            512,
            32);
        AssertFamilyForms(
            generator.CreatePatternFingerprint(molecule, options),
            generator.CreatePatternCountFingerprint(molecule, options),
            generator.CreatePatternSparseFingerprint(molecule, options),
            generator.CreatePatternFoldedFingerprint(molecule, options),
            512,
            32);
        AssertFamilyForms(
            generator.CreateMaccsFingerprint(molecule, options),
            generator.CreateMaccsCountFingerprint(molecule, options),
            generator.CreateMaccsSparseFingerprint(molecule, options),
            generator.CreateMaccsFoldedFingerprint(molecule, options),
            166,
            32);
    }

    [Test]
    public void FoldedForms_MapEverySourceBitByModulo()
    {
        FingerprintGenerator generator = new FingerprintGenerator();
        FingerprintOptions options = CreateOptions();
        Molecule molecule = BuildEthanol();

        AssertFolded(generator.CreatePathFingerprint(molecule, options), generator.CreatePathFoldedFingerprint(molecule, options));
        AssertFolded(generator.CreateCircularFingerprint(molecule, options), generator.CreateCircularFoldedFingerprint(molecule, options));
        AssertFolded(generator.CreatePatternFingerprint(molecule, options), generator.CreatePatternFoldedFingerprint(molecule, options));
        AssertFolded(generator.CreateMaccsFingerprint(molecule, options), generator.CreateMaccsFoldedFingerprint(molecule, options));
    }

    [Test]
    public void Generator_NullMoleculesReturnEmptyFormsForEveryFamily()
    {
        FingerprintGenerator generator = new FingerprintGenerator();
        FingerprintOptions options = CreateOptions();

        AssertEmptyFamilyForms(generator.CreatePathFingerprint(null, options), generator.CreatePathCountFingerprint(null, options), generator.CreatePathSparseFingerprint(null, options), generator.CreatePathFoldedFingerprint(null, options));
        AssertEmptyFamilyForms(generator.CreateCircularFingerprint(null, options), generator.CreateCircularCountFingerprint(null, options), generator.CreateCircularSparseFingerprint(null, options), generator.CreateCircularFoldedFingerprint(null, options));
        AssertEmptyFamilyForms(generator.CreatePatternFingerprint(null, options), generator.CreatePatternCountFingerprint(null, options), generator.CreatePatternSparseFingerprint(null, options), generator.CreatePatternFoldedFingerprint(null, options));
        AssertEmptyFamilyForms(generator.CreateMaccsFingerprint(null, options), generator.CreateMaccsCountFingerprint(null, options), generator.CreateMaccsSparseFingerprint(null, options), generator.CreateMaccsFoldedFingerprint(null, options));
    }

    [Test]
    public void FoldedForms_ClampNonpositiveOutputSizeToOneBit()
    {
        FingerprintOptions options = CreateOptions();
        options.FoldedBitCount = 0;
        FingerprintGenerator generator = new FingerprintGenerator();

        Assert.That(generator.CreatePathFoldedFingerprint(BuildEthanol(), options).Size, Is.EqualTo(1));
        Assert.That(generator.CreateCircularFoldedFingerprint(BuildEthanol(), options).Size, Is.EqualTo(1));
        Assert.That(generator.CreatePatternFoldedFingerprint(BuildEthanol(), options).Size, Is.EqualTo(1));
        Assert.That(generator.CreateMaccsFoldedFingerprint(BuildEthanol(), options).Size, Is.EqualTo(1));
    }

    [Test]
    public void MaccsFamily_UsesFixedBuiltInStructuralKeyPositions()
    {
        FingerprintGenerator generator = new FingerprintGenerator();
        FingerprintOptions options = CreateOptions();

        BitFingerprint bits = generator.CreateMaccsFingerprint(BuildEthanol(), options);
        CountFingerprint counts = generator.CreateMaccsCountFingerprint(BuildEthanol(), options);

        Assert.That(bits.Size, Is.EqualTo(166));
        Assert.That(bits.Bits[0], Is.True);
        Assert.That(bits.Bits[2], Is.True);
        Assert.That(bits.Bits[11], Is.True);
        Assert.That(bits.Bits[12], Is.True);
        Assert.That(bits.Bits[9], Is.False);
        Assert.That(counts.GetCount(0), Is.EqualTo(2));
        Assert.That(counts.GetCount(2), Is.EqualTo(1));
    }

    [Test]
    public void Screening_RejectsMissingFeaturesWithoutAttemptingExactMatch()
    {
        FingerprintScreening screening = new FingerprintScreening();

        FingerprintScreeningResult result = screening.FindFirst(BuildEthane(), BuildCarbonyl(), null);

        Assert.That(result.ScreenPassed, Is.False);
        Assert.That(result.ExactMatchAttempted, Is.False);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Match.Success, Is.False);
    }

    [Test]
    public void Screening_RunsExactMatchingAfterAConservativePass()
    {
        FingerprintScreening screening = new FingerprintScreening();

        FingerprintScreeningResult result = screening.FindFirst(BuildAcetamide(), BuildCarbonyl(), null);

        Assert.That(result.ScreenPassed, Is.True);
        Assert.That(result.ExactMatchAttempted, Is.True);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Match.AtomMapping, Is.EqualTo(new int[] { 1, 2 }));
    }

    [Test]
    public void Screening_UsesExactMatchingToRejectFingerprintCollisions()
    {
        FingerprintScreening screening = new FingerprintScreening();
        FingerprintOptions options = CreateOptions();
        options.BitCount = 1;

        FingerprintScreeningResult result = screening.FindFirst(BuildEthane(), BuildCarbonyl(), options);

        Assert.That(result.ScreenPassed, Is.True);
        Assert.That(result.ExactMatchAttempted, Is.True);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void Screening_ReportsNullInputsWithoutAttemptingExactMatch()
    {
        FingerprintScreening screening = new FingerprintScreening();

        FingerprintScreeningResult result = screening.FindFirst(null, BuildCarbonyl(), null);

        Assert.That(result.ExactMatchAttempted, Is.False);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.HasErrors, Is.True);
        Assert.That(result.Report.Errors[0].Code, Is.EqualTo("FINGERPRINT_SCREEN_NULL_MOLECULE"));
    }

    [Test]
    public void Screening_RejectsOversizedQueryBeforeFingerprintOrExactMatching()
    {
        FingerprintScreeningResult result = new FingerprintScreening().FindFirst(BuildEthane(), BuildAcetamide(), null);

        Assert.That(result.ScreenPassed, Is.False);
        Assert.That(result.ExactMatchAttempted, Is.False);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetCount(), Is.Zero);
    }

    [Test]
    public void Screening_ClampsInvalidFingerprintDimensions()
    {
        FingerprintOptions options = new FingerprintOptions();
        options.BitCount = 0;
        options.MinimumPathLength = -2;
        options.MaximumPathLength = -1;

        FingerprintScreeningResult result = new FingerprintScreening().FindFirst(BuildEthane(), BuildEthane(), options);

        Assert.That(result.TargetFingerprint.Size, Is.EqualTo(1));
        Assert.That(result.QueryFingerprint.Size, Is.EqualTo(1));
        Assert.That(result.ScreenPassed, Is.True);
        Assert.That(result.ExactMatchAttempted, Is.True);
        Assert.That(result.Success, Is.True);
    }

    static void AssertFamilyForms(BitFingerprint bits, CountFingerprint counts, SparseFingerprint sparse, BitFingerprint folded, int sourceSize, int foldedSize)
    {
        Assert.That(bits.Size, Is.EqualTo(sourceSize));
        Assert.That(counts.Size, Is.EqualTo(sourceSize));
        Assert.That(sparse.Size, Is.EqualTo(sourceSize));
        Assert.That(folded.Size, Is.EqualTo(foldedSize));
        Assert.That(bits.CountOnBits(), Is.GreaterThan(0));
        Assert.That(counts.TotalCount(), Is.GreaterThanOrEqualTo(bits.CountOnBits()));
        Assert.That(sparse.Indices, Is.EqualTo(counts.ToSparseFingerprint().Indices));
    }

    static void AssertEmptyFamilyForms(BitFingerprint bits, CountFingerprint counts, SparseFingerprint sparse, BitFingerprint folded)
    {
        Assert.That(bits.CountOnBits(), Is.Zero);
        Assert.That(counts.TotalCount(), Is.Zero);
        Assert.That(sparse.Indices, Is.Empty);
        Assert.That(folded.CountOnBits(), Is.Zero);
    }

    static void AssertFolded(BitFingerprint source, BitFingerprint folded)
    {
        bool[] expected = new bool[folded.Size];
        int index = 0;
        while (index < source.Size)
        {
            if (source.Bits[index])
            {
                expected[index % folded.Size] = true;
            }

            index = index + 1;
        }

        Assert.That(folded.Bits, Is.EqualTo(expected));
    }

    static FingerprintOptions CreateOptions()
    {
        FingerprintOptions options = new FingerprintOptions();
        options.BitCount = 512;
        options.FoldedBitCount = 32;
        options.MinimumPathLength = 0;
        options.MaximumPathLength = 3;
        options.CircularRadius = 2;
        return options;
    }

    static Molecule BuildEthanol()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbonA = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonB = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(carbonA, carbonB, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonB, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildEthane()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int first = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int second = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        builder.AddBond(first, second, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildCarbonyl()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildAcetamide()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int methyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int carbonyl = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        builder.AddBond(methyl, carbonyl, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonyl, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonyl, nitrogen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }
}
