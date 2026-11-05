using System.Collections.Generic;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class TautomerEnumeratorTests
{
    [Test]
    public void StandardizationOptions_DefaultsTautomerEnumerationOptions()
    {
        StandardizationOptions options = new StandardizationOptions();

        Assert.That(options.EnableTautomerEnumeration, Is.False);
        Assert.That(options.TautomerEnumeration.MaximumTautomerCount, Is.EqualTo(100));
        Assert.That(options.TautomerEnumeration.MaximumTransformCount, Is.EqualTo(1000));
        Assert.That(options.TautomerEnumeration.IncludeInputMolecule, Is.True);
    }

    [Test]
    public void EnumerateWithResult_GeneratesEnolFromKeto()
    {
        TautomerEnumerator enumerator = new TautomerEnumerator();
        Molecule molecule = BuildKeto();

        TautomerEnumerationResult result = enumerator.EnumerateWithResult(molecule, new TautomerEnumerationOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tautomers.Count, Is.EqualTo(2));
        Assert.That(result.AppliedRuleNames, Is.EqualTo(new string[] { "keto-enol" }));
        Assert.That(result.Tautomers[1].Atoms[1].ImplicitHydrogenCount, Is.EqualTo(1));
        Assert.That(result.Tautomers[1].Atoms[2].ImplicitHydrogenCount, Is.EqualTo(2));
        Assert.That(result.Tautomers[1].Bonds[0].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(result.Tautomers[1].Bonds[1].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(molecule.Atoms[1].ImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
    }

    [Test]
    public void EnumerateWithResult_GeneratesKetoFromEnol()
    {
        TautomerEnumerator enumerator = new TautomerEnumerator();

        TautomerEnumerationResult result = enumerator.EnumerateWithResult(BuildEnol(), new TautomerEnumerationOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tautomers.Count, Is.EqualTo(2));
        Assert.That(result.Tautomers[1].Atoms[1].ImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(result.Tautomers[1].Atoms[2].ImplicitHydrogenCount, Is.EqualTo(3));
        Assert.That(result.Tautomers[1].Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(result.Tautomers[1].Bonds[1].Order, Is.EqualTo(BondOrder.Single));
    }

    [Test]
    public void EnumerateWithResult_DoesNotAddDuplicateReturnToInput()
    {
        TautomerEnumerator enumerator = new TautomerEnumerator();
        TautomerEnumerationOptions options = new TautomerEnumerationOptions();
        options.MaximumTautomerCount = 10;
        options.MaximumTransformCount = 10;

        TautomerEnumerationResult result = enumerator.EnumerateWithResult(BuildKeto(), options);

        Assert.That(result.Tautomers.Count, Is.EqualTo(2));
        Assert.That(result.AppliedRuleNames.Count, Is.EqualTo(1));
        Assert.That(result.TransformCount, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void EnumerateWithResult_RespectsMaximumTautomerLimit()
    {
        TautomerEnumerator enumerator = new TautomerEnumerator();
        TautomerEnumerationOptions options = new TautomerEnumerationOptions();
        options.MaximumTautomerCount = 1;

        TautomerEnumerationResult result = enumerator.EnumerateWithResult(BuildKeto(), options);

        Assert.That(result.Tautomers.Count, Is.EqualTo(1));
        Assert.That(result.TautomerLimitReached, Is.True);
        Assert.That(result.TransformCount, Is.EqualTo(1));
    }

    [Test]
    public void EnumerateWithResult_RespectsMaximumTransformLimit()
    {
        TautomerEnumerator enumerator = new TautomerEnumerator();
        TautomerEnumerationOptions options = new TautomerEnumerationOptions();
        options.MaximumTransformCount = 0;

        TautomerEnumerationResult result = enumerator.EnumerateWithResult(BuildKeto(), options);

        Assert.That(result.Tautomers.Count, Is.EqualTo(1));
        Assert.That(result.TransformCount, Is.EqualTo(0));
        Assert.That(result.TransformLimitReached, Is.True);
    }

    [Test]
    public void Enumerate_ReturnsTautomerListShortcut()
    {
        TautomerEnumerator enumerator = new TautomerEnumerator();

        List<Molecule> tautomers = enumerator.Enumerate(BuildKeto(), new TautomerEnumerationOptions());

        Assert.That(tautomers.Count, Is.EqualTo(2));
    }

    [Test]
    public void EnumerateWithResult_ReportsNullInput()
    {
        TautomerEnumerator enumerator = new TautomerEnumerator();

        TautomerEnumerationResult result = enumerator.EnumerateWithResult(null, new TautomerEnumerationOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("TAUTOMER_ENUMERATOR_NULL_MOLECULE"));
        Assert.That(result.Tautomers.Count, Is.EqualTo(0));
    }

    [Test]
    public void EnumerateWithResult_CanExcludeInputAndReturnOnlyGeneratedTautomers()
    {
        TautomerEnumerationOptions options = new TautomerEnumerationOptions();
        options.IncludeInputMolecule = false;
        Canonicalizer canonicalizer = new Canonicalizer();
        Molecule keto = BuildKeto();

        TautomerEnumerationResult result = new TautomerEnumerator().EnumerateWithResult(keto, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tautomers, Is.Not.Empty);
        Assert.That(result.TransformCount, Is.GreaterThan(0));
        Assert.That(result.Tautomers[0].Bonds[0].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(canonicalizer.HasSameIdentity(result.Tautomers[0], keto, IdentityPolicy.Exact), Is.False);
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
}
