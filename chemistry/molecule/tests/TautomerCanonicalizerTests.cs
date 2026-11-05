using System.Collections.Generic;
using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class TautomerCanonicalizerTests
{
    [Test]
    public void CanonicalizeWithResult_SelectsKetoWhenStartingFromKeto()
    {
        TautomerCanonicalizer canonicalizer = new TautomerCanonicalizer();

        TautomerCanonicalizationResult result = canonicalizer.CanonicalizeWithResult(BuildKeto(), new TautomerEnumerationOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.TautomerCount, Is.EqualTo(2));
        Assert.That(result.SelectedIndex, Is.EqualTo(0));
        Assert.That(result.BestScore.CarbonylBondCount, Is.EqualTo(1));
        Assert.That(result.BestScore.HeteroHydrogenPenalty, Is.EqualTo(0));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(result.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Single));
    }

    [Test]
    public void CanonicalizeWithResult_SelectsKetoWhenStartingFromEnol()
    {
        TautomerCanonicalizer canonicalizer = new TautomerCanonicalizer();

        TautomerCanonicalizationResult result = canonicalizer.CanonicalizeWithResult(BuildEnol(), new TautomerEnumerationOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.TautomerCount, Is.EqualTo(2));
        Assert.That(result.SelectedIndex, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[1].ImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[2].ImplicitHydrogenCount, Is.EqualTo(3));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(result.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Single));
    }

    [Test]
    public void ScoreMolecule_FavorsCarbonylAndPenalizesHeteroHydrogen()
    {
        TautomerCanonicalizer canonicalizer = new TautomerCanonicalizer();

        TautomerScore keto = canonicalizer.ScoreMolecule(BuildKeto());
        TautomerScore enol = canonicalizer.ScoreMolecule(BuildEnol());

        Assert.That(keto.CarbonylBondCount, Is.EqualTo(1));
        Assert.That(keto.HeteroDoubleBondCount, Is.EqualTo(1));
        Assert.That(keto.HeteroHydrogenPenalty, Is.EqualTo(0));
        Assert.That(enol.CarbonylBondCount, Is.EqualTo(0));
        Assert.That(enol.HeteroHydrogenPenalty, Is.EqualTo(1));
        Assert.That(keto.TotalScore, Is.GreaterThan(enol.TotalScore));
    }

    [Test]
    public void SelectCanonicalTautomerWithResult_UsesDeterministicKeyForTies()
    {
        TautomerCanonicalizer canonicalizer = new TautomerCanonicalizer();
        DeterministicSerializer serializer = new DeterministicSerializer();
        Molecule carbon = BuildSingleAtom(6, "C");
        Molecule nitrogen = BuildSingleAtom(7, "N");
        List<Molecule> tautomers = new List<Molecule>();
        tautomers.Add(carbon);
        tautomers.Add(nitrogen);

        TautomerCanonicalizationResult result = canonicalizer.SelectCanonicalTautomerWithResult(tautomers);

        string carbonKey = serializer.Serialize(carbon);
        string nitrogenKey = serializer.Serialize(nitrogen);
        if (string.CompareOrdinal(carbonKey, nitrogenKey) < 0)
        {
            Assert.That(result.SelectedIndex, Is.EqualTo(0));
        }
        else
        {
            Assert.That(result.SelectedIndex, Is.EqualTo(1));
        }
    }

    [Test]
    public void Canonicalize_ReturnsCanonicalMoleculeShortcut()
    {
        TautomerCanonicalizer canonicalizer = new TautomerCanonicalizer();

        Molecule molecule = canonicalizer.Canonicalize(BuildEnol(), new TautomerEnumerationOptions());

        Assert.That(molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(molecule.Atoms[1].ImplicitHydrogenCount, Is.EqualTo(0));
    }

    [Test]
    public void SelectCanonicalTautomerWithResult_ReportsEmptyInput()
    {
        TautomerCanonicalizer canonicalizer = new TautomerCanonicalizer();

        TautomerCanonicalizationResult result = canonicalizer.SelectCanonicalTautomerWithResult(new List<Molecule>());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("TAUTOMER_CANONICALIZER_NO_TAUTOMERS"));
    }

    [Test]
    public void CanonicalizeWithResult_ReportsNullInput()
    {
        TautomerCanonicalizer canonicalizer = new TautomerCanonicalizer();

        TautomerCanonicalizationResult result = canonicalizer.CanonicalizeWithResult(null, new TautomerEnumerationOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("TAUTOMER_CANONICALIZER_NULL_MOLECULE"));
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(0));
    }

    [Test]
    public void CanonicalizeWithResult_ReportsWhenOptionsPermitNoCandidates()
    {
        TautomerEnumerationOptions options = new TautomerEnumerationOptions();
        options.MaximumTautomerCount = 0;

        TautomerCanonicalizationResult result = new TautomerCanonicalizer().CanonicalizeWithResult(BuildKeto(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.TautomerCount, Is.Zero);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("TAUTOMER_CANONICALIZER_NO_TAUTOMERS"));
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
}
