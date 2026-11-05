using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class AtomTyperTests
{
    [Test]
    public void TypeAtom_ClassifiesElementCategories()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        int sodium = builder.AddAtom(new AtomSpec(11, 0, 1, 0, 0, 0, 0, false, "", "Na"));
        int chlorine = builder.AddAtom(new AtomSpec(17, 0, -1, 0, 0, 0, 0, false, "", "Cl"));
        AtomTyper typer = new AtomTyper();

        AtomTypeResult carbonType = typer.TypeAtom(builder.Build(), carbon);
        AtomTypeResult sodiumType = typer.TypeAtom(builder.Build(), sodium);
        AtomTypeResult chlorineType = typer.TypeAtom(builder.Build(), chlorine);

        Assert.That(carbonType.Symbol, Is.EqualTo("C"));
        Assert.That(carbonType.IsOrganicSubset, Is.True);
        Assert.That(carbonType.IsHeteroAtom, Is.False);
        Assert.That(carbonType.IsMetal, Is.False);
        Assert.That(sodiumType.IsMetal, Is.True);
        Assert.That(sodiumType.IsOrganicSubset, Is.False);
        Assert.That(chlorineType.IsHalogen, Is.True);
        Assert.That(chlorineType.IsOrganicSubset, Is.True);
        Assert.That(chlorineType.IsHeteroAtom, Is.True);
    }

    [Test]
    public void TypeAtom_ClassifiesDonorsAndAcceptors()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 2, 0, 0, false, "", "O"));
        int ammoniumNitrogen = builder.AddAtom(new AtomSpec(7, 0, 1, 0, 4, 0, 0, false, "", "N"));
        AtomTyper typer = new AtomTyper();

        AtomTypeResult oxygenType = typer.TypeAtom(builder.Build(), oxygen);
        AtomTypeResult ammoniumType = typer.TypeAtom(builder.Build(), ammoniumNitrogen);

        Assert.That(oxygenType.IsHydrogenBondDonor, Is.True);
        Assert.That(oxygenType.IsHydrogenBondAcceptor, Is.True);
        Assert.That(ammoniumType.IsHydrogenBondDonor, Is.True);
        Assert.That(ammoniumType.IsHydrogenBondAcceptor, Is.False);
    }

    [Test]
    public void TypeAtom_ClassifiesHydroxylAndThiolGroups()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int oxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 1, 0, 0, false, "", "O"));
        int sulfur = builder.AddAtom(new AtomSpec(16, 0, 0, 0, 1, 0, 0, false, "", "S"));
        builder.AddBond(carbon, oxygen, new BondSpec());
        builder.AddBond(carbon, sulfur, new BondSpec());
        AtomTyper typer = new AtomTyper();

        AtomTypeResult oxygenType = typer.TypeAtom(builder.Build(), oxygen);
        AtomTypeResult sulfurType = typer.TypeAtom(builder.Build(), sulfur);

        Assert.That(oxygenType.IsHydroxylOxygen, Is.True);
        Assert.That(oxygenType.FunctionalGroup, Is.EqualTo("hydroxyl"));
        Assert.That(sulfurType.IsThiolSulfur, Is.True);
        Assert.That(sulfurType.FunctionalGroup, Is.EqualTo("thiol"));
    }

    [Test]
    public void TypeAtom_ClassifiesCarbonylAndCarboxylGroups()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int doubleOxygen = builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int singleOxygen = builder.AddAtom(new AtomSpec(8, 0, -1, 0, 0, 0, 0, false, "", "O"));
        builder.AddBond(carbon, doubleOxygen, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        builder.AddBond(carbon, singleOxygen, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        AtomTyper typer = new AtomTyper();

        AtomTypeResult carbonType = typer.TypeAtom(builder.Build(), carbon);
        AtomTypeResult oxygenType = typer.TypeAtom(builder.Build(), doubleOxygen);

        Assert.That(carbonType.IsCarbonylCarbon, Is.True);
        Assert.That(carbonType.IsCarboxylCarbon, Is.True);
        Assert.That(carbonType.FunctionalGroup, Is.EqualTo("carboxyl"));
        Assert.That(oxygenType.IsHydrogenBondAcceptor, Is.True);
    }

    [Test]
    public void TypeAtom_ClassifiesAmineAndNitroNitrogen()
    {
        MoleculeBuilder amineBuilder = new MoleculeBuilder();
        int carbon = amineBuilder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 0, 0, false, "", "C"));
        int nitrogen = amineBuilder.AddAtom(new AtomSpec(7, 0, 0, 0, 2, 0, 0, false, "", "N"));
        amineBuilder.AddBond(carbon, nitrogen, new BondSpec());

        MoleculeBuilder nitroBuilder = new MoleculeBuilder();
        int nitroNitrogen = nitroBuilder.AddAtom(new AtomSpec(7, 0, 1, 0, 0, 0, 0, false, "", "N"));
        int oxygenA = nitroBuilder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        int oxygenB = nitroBuilder.AddAtom(new AtomSpec(8, 0, -1, 0, 0, 0, 0, false, "", "O"));
        nitroBuilder.AddBond(nitroNitrogen, oxygenA, BondSpec.Create(BondOrder.Double, false, BondStereo.None, BondDirection.None));
        nitroBuilder.AddBond(nitroNitrogen, oxygenB, BondSpec.Create(BondOrder.Single, false, BondStereo.None, BondDirection.None));
        AtomTyper typer = new AtomTyper();

        AtomTypeResult amineType = typer.TypeAtom(amineBuilder.Build(), nitrogen);
        AtomTypeResult nitroType = typer.TypeAtom(nitroBuilder.Build(), nitroNitrogen);

        Assert.That(amineType.IsAmineNitrogen, Is.True);
        Assert.That(amineType.IsHydrogenBondDonor, Is.True);
        Assert.That(amineType.FunctionalGroup, Is.EqualTo("amine"));
        Assert.That(nitroType.IsNitroNitrogen, Is.True);
        Assert.That(nitroType.FunctionalGroup, Is.EqualTo("nitro"));
    }

    [Test]
    public void TypeAtoms_ReturnsResultForEachAtom()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        builder.AddAtom(new AtomSpec(8, 0, 0, 0, 0, 0, 0, false, "", "O"));
        AtomTyper typer = new AtomTyper();

        List<AtomTypeResult> results = typer.TypeAtoms(builder.Build());

        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results[0].AtomicNumber, Is.EqualTo(6));
        Assert.That(results[1].AtomicNumber, Is.EqualTo(8));
    }

    [Test]
    public void PublicCalculationAndRadicalEntryPoints_HandleEveryRadicalStateAndInvalidIds()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int none = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 4, 0, 0, false, "", "C"));
        int doublet = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 3, 1, 0, false, "", "C"));
        int triplet = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 2, 2, 0, false, "", "C"));
        int quartet = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 1, 3, 0, false, "", "C"));
        Molecule molecule = builder.Build();
        IAtomTyper typer = new AtomTyper();

        HydrogenCalculationResult hydrogens = typer.CalculateHydrogens(molecule, none);
        ValenceResult valence = typer.CalculateValence(molecule, none);

        Assert.That(hydrogens.CalculatedImplicitHydrogenCount, Is.EqualTo(4));
        Assert.That(valence.TotalValence, Is.EqualTo(4));
        Assert.That(typer.GetRadicalState(molecule, none), Is.EqualTo(RadicalState.None));
        Assert.That(typer.GetRadicalState(molecule, doublet), Is.EqualTo(RadicalState.Doublet));
        Assert.That(typer.GetRadicalState(molecule, triplet), Is.EqualTo(RadicalState.Triplet));
        Assert.That(typer.GetRadicalState(molecule, quartet), Is.EqualTo(RadicalState.Quartet));
        Assert.That(typer.GetRadicalState(null, 0), Is.EqualTo(RadicalState.None));
        Assert.That(typer.TypeAtom(null, 0).AtomId, Is.EqualTo(-1));
        Assert.That(typer.TypeAtom(molecule, 99).AtomId, Is.EqualTo(-1));
        Assert.That(typer.TypeAtoms(null), Is.Empty);
    }
}
