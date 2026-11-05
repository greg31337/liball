using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class MetalDisconnectorTests
{
    [Test]
    public void DisconnectWithResult_RemovesMetalOxygenBondAndAdjustsCharges()
    {
        MetalDisconnector disconnector = new MetalDisconnector();
        Molecule molecule = BuildSodiumMethoxide();

        MetalDisconnectionResult result = disconnector.DisconnectWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.DisconnectionCount, Is.EqualTo(1));
        Assert.That(result.RemovedBondIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(result.MetalAtomIds, Is.EqualTo(new int[] { 0 }));
        Assert.That(result.LigandAtomIds, Is.EqualTo(new int[] { 1 }));
        Assert.That(result.Molecule.GetBondCount(), Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[0].FormalCharge, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[1].FormalCharge, Is.EqualTo(-1));
        Assert.That(molecule.GetBondCount(), Is.EqualTo(2));
        Assert.That(molecule.Atoms[0].FormalCharge, Is.EqualTo(0));
    }

    [Test]
    public void DisconnectWithResult_DisconnectsOrganometallicCarbonBond()
    {
        MetalDisconnector disconnector = new MetalDisconnector();
        Molecule molecule = BuildIronCarbonBond();

        MetalDisconnectionResult result = disconnector.DisconnectWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.DisconnectionCount, Is.EqualTo(1));
        Assert.That(result.Molecule.GetBondCount(), Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[0].FormalCharge, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[1].FormalCharge, Is.EqualTo(-1));
    }

    [Test]
    public void DisconnectWithResult_RemovesDativeBondWithoutChargeAdjustment()
    {
        MetalDisconnector disconnector = new MetalDisconnector();
        Molecule molecule = BuildDativeIronNitrogenBond();

        MetalDisconnectionResult result = disconnector.DisconnectWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.DisconnectionCount, Is.EqualTo(1));
        Assert.That(result.Molecule.GetBondCount(), Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[0].FormalCharge, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms[1].FormalCharge, Is.EqualTo(0));
    }

    [Test]
    public void DisconnectWithResult_KeepsNonMetalAndMetalMetalBonds()
    {
        MetalDisconnector disconnector = new MetalDisconnector();
        Molecule molecule = BuildCarbonOxygenAndSodiumPotassium();

        MetalDisconnectionResult result = disconnector.DisconnectWithResult(molecule);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.False);
        Assert.That(result.DisconnectionCount, Is.EqualTo(0));
        Assert.That(result.Molecule.GetBondCount(), Is.EqualTo(2));
        Assert.That(result.Molecule.Bonds[0].BeginAtomId, Is.EqualTo(0));
        Assert.That(result.Molecule.Bonds[1].BeginAtomId, Is.EqualTo(2));
    }

    [Test]
    public void DisconnectWithResult_RenumbersKeptBondsAfterRemoval()
    {
        MetalDisconnector disconnector = new MetalDisconnector();
        Molecule molecule = BuildSodiumMethoxide();

        MetalDisconnectionResult result = disconnector.DisconnectWithResult(molecule);

        Assert.That(result.Molecule.GetBondCount(), Is.EqualTo(1));
        Assert.That(result.Molecule.Bonds[0].Id, Is.EqualTo(0));
        Assert.That(result.Molecule.Bonds[0].BeginAtomId, Is.EqualTo(1));
        Assert.That(result.Molecule.Bonds[0].EndAtomId, Is.EqualTo(2));
    }

    [Test]
    public void DisconnectWithResult_ReportsNullInput()
    {
        MetalDisconnector disconnector = new MetalDisconnector();

        MetalDisconnectionResult result = disconnector.DisconnectWithResult(null);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Report.GetErrorCount(), Is.EqualTo(1));
        Assert.That(result.Report.Get(0).Code, Is.EqualTo("METAL_DISCONNECTOR_NULL_MOLECULE"));
        Assert.That(result.Molecule.GetAtomCount(), Is.EqualTo(0));
    }

    [Test]
    public void Disconnect_ReturnsDisconnectedCopyShortcutWithoutMutatingSource()
    {
        Molecule source = BuildSodiumMethoxide();

        Molecule disconnected = new MetalDisconnector().Disconnect(source);

        Assert.That(disconnected, Is.Not.SameAs(source));
        Assert.That(disconnected.GetBondCount(), Is.EqualTo(1));
        Assert.That(source.GetBondCount(), Is.EqualTo(2));
    }

    static Molecule BuildSodiumMethoxide()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int sodium = builder.AddAtom(new AtomSpec(11, 0, 0, 0, 0, 0, 0, false, "", "Na"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddBond(sodium, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(oxygen, carbon, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildIronCarbonBond()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int iron = builder.AddAtom(new AtomSpec(26, 0, 0, 0, 0, 0, 0, false, "", "Fe"));
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddBond(iron, carbon, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }

    static Molecule BuildDativeIronNitrogenBond()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int iron = builder.AddAtom(new AtomSpec(26, 0, 0, 0, 0, 0, 0, false, "", "Fe"));
        int nitrogen = builder.AddAtom(new AtomSpec(7, 0, 0, 0, 0, 0, 0, false, "", "N"));
        BondSpec spec = BondSpec.Create(BondOrder.Dative, false, BondStereo.None, BondDirection.None);
        spec.IsDative = true;
        builder.AddBond(iron, nitrogen, spec);
        return builder.Build();
    }

    static Molecule BuildCarbonOxygenAndSodiumPotassium()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int sodium = builder.AddAtom(new AtomSpec(11, 0, 0, 0, 0, 0, 0, false, "", "Na"));
        int potassium = builder.AddAtom(new AtomSpec(19, 0, 0, 0, 0, 0, 0, false, "", "K"));
        builder.AddBond(carbon, oxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        builder.AddBond(sodium, potassium, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        return builder.Build();
    }
}
