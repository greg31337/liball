using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class ChargeNormalizerTests
{
    [Test]
    public void NormalizeWithResult_NormalizesNeutralNitroDeterministically()
    {
        ChargeNormalizer normalizer = new ChargeNormalizer();
        Molecule molecule = BuildNeutralNitro();

        ChargeNormalizationResult result = normalizer.NormalizeWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.AppliedRuleCount, Is.EqualTo(1));
        Assert.That(result.AppliedRuleNames, Is.EqualTo(new string[] { "nitro" }));
        Assert.That(result.Molecule.Atoms[0].FormalCharge, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[1].FormalCharge, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[2].FormalCharge, Is.EqualTo(-1));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Double));
        Assert.That(result.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(molecule.Atoms[0].FormalCharge, Is.EqualTo(0));
        Assert.That(molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Double));
    }

    [Test]
    public void NormalizeWithResult_DoesNotChangeAlreadyNormalizedNitro()
    {
        ChargeNormalizer normalizer = new ChargeNormalizer();
        Molecule molecule = BuildNormalizedNitro();

        ChargeNormalizationResult result = normalizer.NormalizeWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.False);
        Assert.That(result.AppliedRuleCount, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[0].FormalCharge, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[2].FormalCharge, Is.EqualTo(-1));
    }

    [Test]
    public void NormalizeWithResult_NormalizesNOxide()
    {
        ChargeNormalizer normalizer = new ChargeNormalizer();
        Molecule molecule = BuildNOxide();

        ChargeNormalizationResult result = normalizer.NormalizeWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.AppliedRuleNames, Is.EqualTo(new string[] { "n-oxide" }));
        Assert.That(result.Molecule.Atoms[0].FormalCharge, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[1].FormalCharge, Is.EqualTo(-1));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Single));
    }

    [Test]
    public void NormalizeWithResult_NormalizesSulfoxide()
    {
        ChargeNormalizer normalizer = new ChargeNormalizer();
        Molecule molecule = BuildSulfoxide();

        ChargeNormalizationResult result = normalizer.NormalizeWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.AppliedRuleNames, Is.EqualTo(new string[] { "sulfoxide" }));
        Assert.That(result.Molecule.Atoms[0].FormalCharge, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[1].FormalCharge, Is.EqualTo(-1));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Single));
    }

    [Test]
    public void NormalizeWithResult_NormalizesPhosphateOxygens()
    {
        ChargeNormalizationResult result = new ChargeNormalizer().NormalizeWithResult(BuildPhosphate());

        Assert.That(result.Success, Is.True);
        Assert.That(result.AppliedRuleNames, Is.EqualTo(new string[] { "phosphate" }));
        Assert.That(result.Molecule.Atoms[1].FormalCharge, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[2].FormalCharge, Is.EqualTo(-1));
        Assert.That(result.Molecule.Atoms[3].FormalCharge, Is.EqualTo(-1));
        Assert.That(result.Molecule.Atoms[4].FormalCharge, Is.EqualTo(-1));
    }

    [Test]
    public void NormalizeWithResult_CanonicalizesCarboxylateBondOrders()
    {
        ChargeNormalizationResult result = new ChargeNormalizer().NormalizeWithResult(BuildReversedCarboxylate());

        Assert.That(result.Success, Is.True);
        Assert.That(result.AppliedRuleNames, Is.EqualTo(new string[] { "carboxylate" }));
        Assert.That(result.Molecule.Bonds[0].Order, Is.EqualTo(BondOrder.Single));
        Assert.That(result.Molecule.Bonds[1].Order, Is.EqualTo(BondOrder.Double));
    }

    [Test]
    public void NormalizeWithResult_NormalizesAzideAndDiazoChargeSeparation()
    {
        ChargeNormalizationResult azide = new ChargeNormalizer().NormalizeWithResult(BuildNeutralAzide());
        ChargeNormalizationResult diazo = new ChargeNormalizer().NormalizeWithResult(BuildNeutralDiazo());

        Assert.That(azide.AppliedRuleNames, Is.EqualTo(new string[] { "azide" }));
        Assert.That(azide.Molecule.Atoms[0].FormalCharge, Is.EqualTo(-1));
        Assert.That(azide.Molecule.Atoms[1].FormalCharge, Is.EqualTo(1));
        Assert.That(diazo.AppliedRuleNames, Is.EqualTo(new string[] { "diazo" }));
        Assert.That(diazo.Molecule.Atoms[0].FormalCharge, Is.EqualTo(-1));
        Assert.That(diazo.Molecule.Atoms[1].FormalCharge, Is.EqualTo(1));
    }

    [Test]
    public void NormalizeWithResult_TransfersAminoAcidProtonToZwitterion()
    {
        ChargeNormalizationResult result = new ChargeNormalizer().NormalizeWithResult(BuildNeutralAminoAcid());

        Assert.That(result.Success, Is.True);
        Assert.That(result.AppliedRuleNames, Is.EqualTo(new string[] { "amino-acid-zwitterion" }));
        Assert.That(result.Molecule.Atoms[0].FormalCharge, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[0].ImplicitHydrogenCount, Is.EqualTo(3));
        Assert.That(result.Molecule.Atoms[4].FormalCharge, Is.EqualTo(-1));
        Assert.That(result.Molecule.Atoms[4].ImplicitHydrogenCount, Is.EqualTo(0));
    }

    [Test]
    public void Normalize_ReturnsNormalizedMoleculeShortcut()
    {
        ChargeNormalizer normalizer = new ChargeNormalizer();

        Molecule molecule = normalizer.Normalize(BuildNeutralNitro());

        Assert.That(molecule.Atoms[0].FormalCharge, Is.EqualTo(1));
        Assert.That(molecule.Atoms[2].FormalCharge, Is.EqualTo(-1));
    }

    [Test]
    public void NormalizeWithResult_ReportsNullInput()
    {
        ChargeNormalizer normalizer = new ChargeNormalizer();

        ChargeNormalizationResult result = normalizer.NormalizeWithResult(null);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("CHARGE_NORMALIZER_NULL_MOLECULE"));
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(0));
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

    static Molecule BuildNormalizedNitro()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 1, 0, 0, 0, 0, false, "", "N"));
        int oxygenA = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int oxygenB = builder.AddAtom(new AtomSpec(8, 0, -1, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(nitrogen, oxygenA, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(nitrogen, oxygenB, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildNOxide()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(nitrogen, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildSulfoxide()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int sulfur = builder.AddAtom(new AtomSpec(16, 0, 0, 0, 0, 0, 0, false, "", "S"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddBond(sulfur, oxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(sulfur, carbon, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildPhosphate()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int phosphorus = builder.AddAtom(new AtomSpec(15, 0, 0, 0, 0, 0, 0, false, "", "P"));
        int doubleOxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int oxygenA = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int oxygenB = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int oxygenC = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(phosphorus, doubleOxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(phosphorus, oxygenA, new BondSpec());
        builder.AddBond(phosphorus, oxygenB, new BondSpec());
        builder.AddBond(phosphorus, oxygenC, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildReversedCarboxylate()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int negativeOxygen = builder.AddAtom(new AtomSpec(8, 0, -1, 0, 0, 0, 0, false, "", "O"));
        int neutralOxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbon, negativeOxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbon, neutralOxygen, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildNeutralAzide()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int terminalA = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        int center = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        int terminalB = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        builder.AddBond(center, terminalA, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(center, terminalB, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildNeutralDiazo()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int center = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        int terminal = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        builder.AddBond(carbon, center, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(center, terminal, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildNeutralAminoAcid()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        int alphaCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 0, 0, false, "", "C"));
        int carbonylCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int carbonylOxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int acidOxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        builder.AddBond(nitrogen, alphaCarbon, new BondSpec());
        builder.AddBond(alphaCarbon, carbonylCarbon, new BondSpec());
        builder.AddBond(carbonylCarbon, carbonylOxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbonylCarbon, acidOxygen, new BondSpec());
        return builder.Build();
    }
}
