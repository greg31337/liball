using System.Collections.Generic;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class StandardizerPipelineTests
{
    [Test]
    public void Standardize_DefaultOptionsCloneWithoutRunningSteps()
    {
        Standardizer standardizer = new Standardizer();
        Molecule molecule = BuildDecoratedEnolWithSalt();

        StandardizationResult result = standardizer.Standardize(molecule, new StandardizationOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.False);
        Assert.That(result.StepCount, Is.EqualTo(0));
        Assert.That(result.MutationCount, Is.EqualTo(0));
        Assert.That(result.Molecule, Is.Not.SameAs(molecule));
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(molecule.GetAtomCount()));
        Assert.That(result.Molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(18));
        Assert.That(molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(18));
    }

    [Test]
    public void Standardize_NullOptionsUseTheDocumentedNoOpPolicy()
    {
        Standardizer standardizer = new Standardizer();
        Molecule source = BuildPlainCarbon();

        StandardizationResult result = standardizer.Standardize(source, null);

        Assert.That(standardizer.GetDefaultPolicy(), Is.EqualTo(StandardizationPolicy.None));
        Assert.That(result.Success, Is.True);
        Assert.That(result.StepCount, Is.Zero);
        Assert.That(result.Changed, Is.False);
        Assert.That(result.Molecule, Is.Not.SameAs(source));
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(1));
    }

    [Test]
    public void Standardize_RunsEnabledStepsInDeterministicOrderAndReportsMutations()
    {
        Standardizer standardizer = new Standardizer();
        StandardizationOptions options = BuildPipelineOptions();
        Molecule molecule = BuildDecoratedEnolWithSalt();

        StandardizationResult result = standardizer.Standardize(molecule, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.StepCount, Is.EqualTo(7));
        Assert.That(result.MutationCount, Is.EqualTo(4));
        Assert.That(GetStepNames(result), Is.EqualTo(new string[] { "salt-removal", "charge-normalization", "reionization", "isotope-cleanup", "radical-cleanup", "tautomer-canonicalization", "stereo-cleanup" }));
        Assert.That(GetChangedStepNames(result), Is.EqualTo(new string[] { "salt-removal", "isotope-cleanup", "radical-cleanup", "tautomer-canonicalization" }));
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(3));
        Assert.That(result.Molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[2].RadicalElectronCount, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[1].ImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[2].ImplicitHydrogenCount, Is.EqualTo(3));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(result.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(result.Report.GetCount(), Is.GreaterThan(0));
        Assert.That(molecule.GetAtomCount(), Is.EqualTo(4));
        Assert.That(molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(18));
        Assert.That(molecule.Atoms[2].RadicalElectronCount, Is.EqualTo(1));
    }

    [Test]
    public void StandardizeMolecule_ReturnsPipelineMoleculeShortcut()
    {
        Standardizer standardizer = new Standardizer();

        Molecule molecule = standardizer.StandardizeMolecule(BuildDecoratedEnolWithSalt(), BuildPipelineOptions());

        Assert.That(molecule.GetAtomCount(), Is.EqualTo(3));
        Assert.That(molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(0));
    }

    [Test]
    public void Standardize_ReportsNullInput()
    {
        Standardizer standardizer = new Standardizer();

        StandardizationResult result = standardizer.Standardize(null, new StandardizationOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("STANDARDIZER_NULL_MOLECULE"));
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(0));
    }

    [Test]
    public void StandardizationOptions_EnableParentCleanupEnablesOrderedParentSteps()
    {
        StandardizationOptions options = new StandardizationOptions();

        options.EnableParentCleanup();

        Assert.That(options.EnableSaltRemoval, Is.True);
        Assert.That(options.EnableMetalDisconnection, Is.True);
        Assert.That(options.EnableChargeNormalization, Is.True);
        Assert.That(options.EnableReionization, Is.True);
        Assert.That(options.EnableIsotopeCleanup, Is.True);
        Assert.That(options.EnableRadicalCleanup, Is.True);
        Assert.That(options.EnableTautomerEnumeration, Is.True);
        Assert.That(options.EnableTautomerCanonicalization, Is.True);
        Assert.That(options.EnableStereoCleanup, Is.True);
        Assert.That(options.EnableExplicitHydrogenNormalization, Is.True);
        Assert.That(options.EnableAromaticityNormalization, Is.True);
        Assert.That(options.EnableKekulizationFallback, Is.True);
    }

    [Test]
    public void Standardize_ParentCleanupExecutesEveryStageInPipelineOrder()
    {
        StandardizationOptions options = new StandardizationOptions();
        options.EnableParentCleanup();

        StandardizationResult result = new Standardizer().Standardize(BuildPlainCarbon(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.StepCount, Is.EqualTo(10));
        Assert.That(GetStepNames(result), Is.EqualTo(new string[]
        {
            "salt-removal",
            "metal-disconnection",
            "explicit-hydrogen-normalization",
            "charge-normalization",
            "reionization",
            "isotope-cleanup",
            "radical-cleanup",
            "tautomer-canonicalization",
            "aromaticity-normalization",
            "stereo-cleanup"
        }));
    }

    [Test]
    public void Standardize_ReplacesNullNestedOptionsBeforeEnabledStagesRun()
    {
        StandardizationOptions options = new StandardizationOptions();
        options.IsotopeCleanup = null!;
        options.RadicalCleanup = null!;
        options.TautomerEnumeration = null!;
        options.HydrogenNormalization = null!;
        options.AromaticityNormalization = null!;
        options.KekulizationFallback = null!;
        options.EnableParentCleanup();

        StandardizationResult result = new Standardizer().Standardize(BuildPlainCarbon(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(options.IsotopeCleanup, Is.Not.Null);
        Assert.That(options.RadicalCleanup, Is.Not.Null);
        Assert.That(options.TautomerEnumeration, Is.Not.Null);
        Assert.That(options.HydrogenNormalization, Is.Not.Null);
        Assert.That(options.AromaticityNormalization, Is.Not.Null);
        Assert.That(options.KekulizationFallback, Is.Not.Null);
    }

    [Test]
    public void Standardize_NormalizesExplicitHydrogenThenChargeThenAromaticity()
    {
        StandardizationOptions options = new StandardizationOptions();
        options.EnableExplicitHydrogenNormalization = true;
        options.EnableChargeNormalization = true;
        options.EnableAromaticityNormalization = true;

        StandardizationResult result = new Standardizer().Standardize(BuildKekuleBenzeneWithExplicitHydrogen(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(GetStepNames(result), Is.EqualTo(new string[] { "explicit-hydrogen-normalization", "charge-normalization", "aromaticity-normalization" }));
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(6));
        Assert.That(result.Molecule.Atoms[0].ImplicitHydrogenCount, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[0].IsAromatic, Is.True);
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Aromatic));
        Assert.That(result.Mutations[0].AtomIds, Is.EqualTo(new int[] { 0, 6 }));
        Assert.That(result.Mutations[0].BondIds, Is.EqualTo(new int[] { 6 }));
        Assert.That(result.Mutations[2].AtomMutationCount, Is.EqualTo(6));
        Assert.That(result.Mutations[2].BondMutationCount, Is.EqualTo(6));
        Assert.That(result.AtomMutationCount, Is.EqualTo(8));
        Assert.That(result.BondMutationCount, Is.EqualTo(7));
    }

    [Test]
    public void Standardize_UsesKekuleFallbackForRejectedAromaticRing()
    {
        StandardizationOptions options = new StandardizationOptions();
        options.EnableAromaticityNormalization = true;
        options.EnableKekulizationFallback = true;

        StandardizationResult result = new Standardizer().Standardize(BuildAromaticFiveMemberCarbonRing(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(GetStepNames(result), Is.EqualTo(new string[] { "aromaticity-normalization", "kekule-fallback" }));
        Assert.That(result.Molecule.Atoms[0].IsAromatic, Is.False);
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(result.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(result.Report.GetWarningCount(), Is.EqualTo(2));
    }

    [Test]
    public void Standardize_ReportsAppliedRulesAndExactChargeMutations()
    {
        StandardizationOptions options = new StandardizationOptions();
        options.EnableChargeNormalization = true;

        StandardizationResult result = new Standardizer().Standardize(BuildNeutralNitro(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Mutations[0].RuleNames, Is.EqualTo(new string[] { "nitro" }));
        Assert.That(result.Mutations[0].AtomIds, Is.EqualTo(new int[] { 0, 2 }));
        Assert.That(result.Mutations[0].BondIds, Is.EqualTo(new int[] { 1 }));
        Assert.That(result.AtomMutationCount, Is.EqualTo(2));
        Assert.That(result.BondMutationCount, Is.EqualTo(1));
    }

    static StandardizationOptions BuildPipelineOptions()
    {
        StandardizationOptions options = new StandardizationOptions();
        options.EnableSaltRemoval = true;
        options.EnableChargeNormalization = true;
        options.EnableReionization = true;
        options.EnableIsotopeCleanup = true;
        options.EnableRadicalCleanup = true;
        options.EnableTautomerCanonicalization = true;
        options.EnableStereoCleanup = true;
        options.IsotopeCleanup.ConfigureRemoveAll();
        options.RadicalCleanup.ConfigureRemoveAll();
        return options;
    }

    static Molecule BuildDecoratedEnolWithSalt()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 18, 0, 0, 1, 0, 0, false, "", "O"));
        int terminalCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 1, 0, false, "", "C"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbon, terminalCarbon, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        return builder.Build();
    }

    static Molecule BuildKekuleBenzeneWithExplicitHydrogen()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < 6)
        {
            int implicitHydrogens = 1;
            if (index == 0)
            {
                implicitHydrogens = 0;
            }

            builder.AddAtom(new AtomSpec(6, 0, 0, 0, implicitHydrogens, 0, 0, false, "", "C"));
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

            BondOrder order = BondOrder.Single;
            if (index % 2 == 0)
            {
                order = BondOrder.Double;
            }

            builder.AddBond(index, next, BondSpec.Create(order, false, BondStereo.None, BondDirection.None));
            index = index + 1;
        }

        int hydrogen = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        builder.AddBond(0, hydrogen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildAromaticFiveMemberCarbonRing()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int index = 0;
        while (index < 5)
        {
            builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 0, 0, true, "", "C"));
            index = index + 1;
        }

        index = 0;
        while (index < 5)
        {
            int next = index + 1;
            if (next == 5)
            {
                next = 0;
            }

            builder.AddBond(index, next, BondSpec.Create(BondOrder.Aromatic, true, BondStereo.None, BondDirection.None));
            index = index + 1;
        }

        return builder.Build();
    }

    static Molecule BuildNeutralNitro()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        int oxygenA = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int oxygenB = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(nitrogen, oxygenA, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(nitrogen, oxygenB, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildPlainCarbon()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        return builder.Build();
    }

    static List<string> GetStepNames(StandardizationResult result)
    {
        List<string> names = new List<string>();
        int index = 0;
        while (index < result.Mutations.Count)
        {
            names.Add(result.Mutations[index].StepName);
            index = index + 1;
        }

        return names;
    }

    static List<string> GetChangedStepNames(StandardizationResult result)
    {
        List<string> names = new List<string>();
        int index = 0;
        while (index < result.Mutations.Count)
        {
            if (result.Mutations[index].Changed)
            {
                names.Add(result.Mutations[index].StepName);
            }

            index = index + 1;
        }

        return names;
    }
}
