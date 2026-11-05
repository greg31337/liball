using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class RadicalCleanupTests
{
    [Test]
    public void RadicalCleanupPolicy_DefaultsToInvalidRadicalValidationAndCleanup()
    {
        StandardizationOptions options = new StandardizationOptions();

        Assert.That(options.EnableRadicalCleanup, Is.False);
        Assert.That(options.RadicalCleanup.RemoveAllRadicals, Is.False);
        Assert.That(options.RadicalCleanup.RemoveInvalidRadicals, Is.True);
        Assert.That(options.RadicalCleanup.ValidateRadicalValence, Is.True);
        Assert.That(options.RadicalCleanup.MaximumRadicalElectronCount, Is.EqualTo(3));
    }

    [Test]
    public void CleanupRadicalsWithResult_PreservesValidRadicalsAndClearsInvalidRange()
    {
        Standardizer standardizer = new Standardizer();
        Molecule molecule = BuildValidAndRangeInvalidRadicals();

        RadicalCleanupResult result = standardizer.CleanupRadicalsWithResult(molecule, new RadicalCleanupPolicy());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.ClearedRadicalCount, Is.EqualTo(1));
        Assert.That(result.PreservedRadicalCount, Is.EqualTo(1));
        Assert.That(result.InvalidRadicalCount, Is.EqualTo(1));
        Assert.That(result.ClearedAtomIds, Is.EqualTo(new int[] { 1 }));
        Assert.That(result.PreservedAtomIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(result.InvalidAtomIds, Is.EqualTo(new int[] { 1 }));
        Assert.That(result.Molecule.Atoms[0].RadicalElectronCount, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[1].RadicalElectronCount, Is.EqualTo(0));
        Assert.That(molecule.Atoms[1].RadicalElectronCount, Is.EqualTo(4));
        Assert.That(result.Report.GetWarningCount(), Is.EqualTo(1));
    }

    [Test]
    public void CleanupRadicalsWithResult_ClearsValenceInvalidRadicals()
    {
        Standardizer standardizer = new Standardizer();
        Molecule molecule = BuildValenceInvalidRadical();

        RadicalCleanupResult result = standardizer.CleanupRadicalsWithResult(molecule, new RadicalCleanupPolicy());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.ClearedRadicalCount, Is.EqualTo(1));
        Assert.That(result.InvalidRadicalCount, Is.EqualTo(1));
        Assert.That(result.InvalidAtomIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(result.Molecule.Atoms[0].RadicalElectronCount, Is.EqualTo(0));
    }

    [Test]
    public void CleanupRadicalsWithResult_CanRemoveAllRadicals()
    {
        Standardizer standardizer = new Standardizer();
        RadicalCleanupPolicy policy = new RadicalCleanupPolicy();
        policy.ConfigureRemoveAll();

        RadicalCleanupResult result = standardizer.CleanupRadicalsWithResult(BuildValidAndRangeInvalidRadicals(), policy);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.ClearedRadicalCount, Is.EqualTo(2));
        Assert.That(result.PreservedRadicalCount, Is.EqualTo(0));
        Assert.That(result.InvalidRadicalCount, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[0].RadicalElectronCount, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[1].RadicalElectronCount, Is.EqualTo(0));
    }

    [Test]
    public void CleanupRadicalsWithResult_ValidateOnlyPreservesInvalidRadicalsAndFails()
    {
        Standardizer standardizer = new Standardizer();
        RadicalCleanupPolicy policy = new RadicalCleanupPolicy();
        policy.ConfigureValidateOnly();

        RadicalCleanupResult result = standardizer.CleanupRadicalsWithResult(BuildValenceInvalidRadical(), policy);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Changed, Is.False);
        Assert.That(result.ClearedRadicalCount, Is.EqualTo(0));
        Assert.That(result.PreservedRadicalCount, Is.EqualTo(1));
        Assert.That(result.InvalidRadicalCount, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[0].RadicalElectronCount, Is.EqualTo(1));
        Assert.That(result.Report.GetWarningCount(), Is.EqualTo(1));
    }

    [Test]
    public void CleanupRadicals_ReturnsCleanedMoleculeShortcut()
    {
        Standardizer standardizer = new Standardizer();
        RadicalCleanupPolicy policy = new RadicalCleanupPolicy();
        policy.ConfigureRemoveAll();

        Molecule molecule = standardizer.CleanupRadicals(BuildValidAndRangeInvalidRadicals(), policy);

        Assert.That(molecule.Atoms[0].RadicalElectronCount, Is.EqualTo(0));
        Assert.That(molecule.Atoms[1].RadicalElectronCount, Is.EqualTo(0));
    }

    [Test]
    public void CleanupRadicalsWithResult_ReportsNullInput()
    {
        Standardizer standardizer = new Standardizer();

        RadicalCleanupResult result = standardizer.CleanupRadicalsWithResult(null, new RadicalCleanupPolicy());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("RADICAL_CLEANUP_NULL_MOLECULE"));
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(0));
    }

    static Molecule BuildValidAndRangeInvalidRadicals()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 1, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 4, 0, false, "", "C"));
        return builder.Build();
    }

    static Molecule BuildValenceInvalidRadical()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 1, 0, false, "", "C"));
        return builder.Build();
    }
}
