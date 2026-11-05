using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class ReionizerTests
{
    [Test]
    public void ReionizeWithResult_MovesProtonFromStrongerAcidToWeakerAnion()
    {
        Reionizer reionizer = new Reionizer();
        Molecule molecule = BuildCarboxylicAcidAndAlkoxide();

        ReionizationResult result = reionizer.ReionizeWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.TransferCount, Is.EqualTo(1));
        Assert.That(result.TransferNames, Is.EqualTo(new string[] { "carboxylic-acid->alcohol" }));
        Assert.That(result.Molecule.Atoms[2].FormalCharge, Is.EqualTo(-1));
        Assert.That(result.Molecule.Atoms[2].ImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[4].FormalCharge, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[4].ImplicitHydrogenCount, Is.EqualTo(1));
        Assert.That(molecule.Atoms[2].FormalCharge, Is.EqualTo(0));
        Assert.That(molecule.Atoms[4].FormalCharge, Is.EqualTo(-1));
    }

    [Test]
    public void ReionizeWithResult_DoesNotMoveProtonFromWeakerAcidToStrongerAnion()
    {
        Reionizer reionizer = new Reionizer();
        Molecule molecule = BuildAlcoholAndCarboxylate();

        ReionizationResult result = reionizer.ReionizeWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.False);
        Assert.That(result.TransferCount, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[1].FormalCharge, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[1].ImplicitHydrogenCount, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[4].FormalCharge, Is.EqualTo(-1));
        Assert.That(result.Molecule.Atoms[4].ImplicitHydrogenCount, Is.EqualTo(0));
    }

    [Test]
    public void FindSite_ClassifiesCarboxylicAcidAndAlcoholByPriority()
    {
        Reionizer reionizer = new Reionizer();
        Molecule molecule = BuildCarboxylicAcidAndAlkoxide();

        AcidBaseSite carboxyl = reionizer.FindSite(molecule, 2);
        AcidBaseSite alcohol = reionizer.FindSite(molecule, 4);

        Assert.That(carboxyl.Found, Is.True);
        Assert.That(carboxyl.Rule.Name, Is.EqualTo("carboxylic-acid"));
        Assert.That(carboxyl.Rule.Priority, Is.EqualTo(30));
        Assert.That(alcohol.Found, Is.True);
        Assert.That(alcohol.Rule.Name, Is.EqualTo("alcohol"));
        Assert.That(alcohol.Rule.Priority, Is.EqualTo(80));
    }

    [Test]
    public void Reionize_ReturnsReionizedMoleculeShortcut()
    {
        Reionizer reionizer = new Reionizer();

        Molecule molecule = reionizer.Reionize(BuildCarboxylicAcidAndAlkoxide());

        Assert.That(molecule.Atoms[2].FormalCharge, Is.EqualTo(-1));
        Assert.That(molecule.Atoms[4].FormalCharge, Is.EqualTo(0));
    }

    [Test]
    public void ReionizeWithResult_ReportsNullInput()
    {
        Reionizer reionizer = new Reionizer();

        ReionizationResult result = reionizer.ReionizeWithResult(null);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("REIONIZER_NULL_MOLECULE"));
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(0));
    }

    [Test]
    public void ReionizeWithResult_NoRecognizedDonorReturnsUnchangedCopy()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        Molecule source = builder.Build();

        ReionizationResult result = new Reionizer().ReionizeWithResult(source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.False);
        Assert.That(result.TransferCount, Is.Zero);
        Assert.That(result.Molecule, Is.Not.SameAs(source));
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(1));
    }

    static Molecule BuildCarboxylicAcidAndAlkoxide()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int acidCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int acidDoubleOxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int acidHydroxyOxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int alcoholCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int alkoxideOxygen = builder.AddAtom(new AtomSpec(8, 0, -1, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(acidCarbon, acidDoubleOxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(acidCarbon, acidHydroxyOxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(alcoholCarbon, alkoxideOxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildAlcoholAndCarboxylate()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int alcoholCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int alcoholOxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int acidCarbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int acidDoubleOxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int carboxylateOxygen = builder.AddAtom(new AtomSpec(8, 0, -1, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(alcoholCarbon, alcoholOxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(acidCarbon, acidDoubleOxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(acidCarbon, carboxylateOxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }
}
