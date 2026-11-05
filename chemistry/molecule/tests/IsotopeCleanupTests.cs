using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class IsotopeCleanupTests
{
    [Test]
    public void IsotopeCleanupPolicy_DefaultsToInvalidIsotopeCleanup()
    {
        StandardizationOptions options = new StandardizationOptions();

        Assert.That(options.EnableIsotopeCleanup, Is.False);
        Assert.That(options.IsotopeCleanup.RemoveAllIsotopes, Is.False);
        Assert.That(options.IsotopeCleanup.PreserveHydrogenIsotopes, Is.True);
        Assert.That(options.IsotopeCleanup.RemoveInvalidIsotopes, Is.True);
        Assert.That(options.IsotopeCleanup.RemoveMajorIsotopes, Is.False);
    }

    [Test]
    public void CleanupIsotopesWithResult_RemovesInvalidIsotopesAndPreservesSupportedIsotopes()
    {
        Standardizer standardizer = new Standardizer();
        Molecule molecule = BuildIsotopeMolecule();

        IsotopeCleanupResult result = standardizer.CleanupIsotopesWithResult(molecule, new IsotopeCleanupPolicy());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.ClearedIsotopeCount, Is.EqualTo(1));
        Assert.That(result.PreservedIsotopeCount, Is.EqualTo(2));
        Assert.That(result.ClearedAtomIds, Is.EqualTo(new int[] { 1 }));
        Assert.That(result.PreservedAtomIds, Is.EqualTo(new int[] { 0, 2 }));
        Assert.That(result.Molecule.Atoms[0].IsotopeMassNumber, Is.EqualTo(13));
        Assert.That(result.Molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[2].IsotopeMassNumber, Is.EqualTo(2));
        Assert.That(molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(99));
    }

    [Test]
    public void CleanupIsotopesWithResult_RemoveAllCanPreserveHydrogenIsotopes()
    {
        Standardizer standardizer = new Standardizer();
        IsotopeCleanupPolicy policy = new IsotopeCleanupPolicy();
        policy.ConfigureRemoveAllPreservingHydrogen();

        IsotopeCleanupResult result = standardizer.CleanupIsotopesWithResult(BuildIsotopeMolecule(), policy);

        Assert.That(result.Changed, Is.True);
        Assert.That(result.Molecule.Atoms[0].IsotopeMassNumber, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[2].IsotopeMassNumber, Is.EqualTo(2));
        Assert.That(result.ClearedIsotopeCount, Is.EqualTo(2));
        Assert.That(result.PreservedIsotopeCount, Is.EqualTo(1));
    }

    [Test]
    public void CleanupIsotopesWithResult_RemoveAllCanClearHydrogenIsotopes()
    {
        Standardizer standardizer = new Standardizer();
        IsotopeCleanupPolicy policy = new IsotopeCleanupPolicy();
        policy.ConfigureRemoveAll();

        IsotopeCleanupResult result = standardizer.CleanupIsotopesWithResult(BuildIsotopeMolecule(), policy);

        Assert.That(result.Changed, Is.True);
        Assert.That(result.Molecule.Atoms[0].IsotopeMassNumber, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[2].IsotopeMassNumber, Is.EqualTo(0));
        Assert.That(result.ClearedIsotopeCount, Is.EqualTo(3));
        Assert.That(result.PreservedIsotopeCount, Is.EqualTo(0));
    }

    [Test]
    public void CleanupIsotopesWithResult_RemoveMajorIsotopesClearsNaturalMajorLabels()
    {
        Standardizer standardizer = new Standardizer();
        IsotopeCleanupPolicy policy = new IsotopeCleanupPolicy();
        policy.RemoveMajorIsotopes = true;
        policy.RemoveInvalidIsotopes = false;
        Molecule molecule = BuildMajorAndMinorCarbonIsotopes();

        IsotopeCleanupResult result = standardizer.CleanupIsotopesWithResult(molecule, policy);

        Assert.That(result.Changed, Is.True);
        Assert.That(result.Molecule.Atoms[0].IsotopeMassNumber, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(13));
        Assert.That(result.ClearedIsotopeCount, Is.EqualTo(1));
        Assert.That(result.PreservedIsotopeCount, Is.EqualTo(1));
    }

    [Test]
    public void CleanupIsotopes_ReturnsCleanedMoleculeShortcut()
    {
        Standardizer standardizer = new Standardizer();
        IsotopeCleanupPolicy policy = new IsotopeCleanupPolicy();
        policy.ConfigureRemoveAll();

        Molecule molecule = standardizer.CleanupIsotopes(BuildIsotopeMolecule(), policy);

        Assert.That(molecule.Atoms[0].IsotopeMassNumber, Is.EqualTo(0));
        Assert.That(molecule.Atoms[2].IsotopeMassNumber, Is.EqualTo(0));
    }

    [Test]
    public void CleanupIsotopesWithResult_ReportsNullInput()
    {
        Standardizer standardizer = new Standardizer();

        IsotopeCleanupResult result = standardizer.CleanupIsotopesWithResult(null, new IsotopeCleanupPolicy());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("ISOTOPE_CLEANUP_NULL_MOLECULE"));
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(0));
    }

    static Molecule BuildIsotopeMolecule()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 13, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(8, 99, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddAtom(new AtomSpec(1, 2, 0, 0, 0, 0, 0, false, "", "H"));
        return builder.Build();
    }

    static Molecule BuildMajorAndMinorCarbonIsotopes()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 12, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(6, 13, 0, 0, 0, 0, 0, false, "", "C"));
        return builder.Build();
    }
}
