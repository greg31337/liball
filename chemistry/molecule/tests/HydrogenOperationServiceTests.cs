using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class HydrogenOperationServiceTests
{
    [Test]
    public void CalculateAtomHydrogens_ReportsAtomAndValenceValidity()
    {
        Molecule molecule = BuildSingleCarbon();
        IHydrogenOperations operations = new HydrogenOperations();

        HydrogenCalculationResult calculation = operations.CalculateAtomHydrogens(molecule, 0);
        HydrogenCalculationResult missing = operations.CalculateAtomHydrogens(molecule, 4);

        Assert.That(calculation.IsValidAtom, Is.True);
        Assert.That(calculation.IsKnownElement, Is.True);
        Assert.That(calculation.IsValidValence, Is.True);
        Assert.That(calculation.CalculatedImplicitHydrogenCount, Is.EqualTo(4));
        Assert.That(missing.IsValidAtom, Is.False);
        Assert.That(missing.IsValidValence, Is.False);
    }

    [Test]
    public void CalculateImplicitHydrogens_AssignsCountsToCopyOnly()
    {
        Molecule source = BuildSingleCarbon();
        IHydrogenOperations operations = new HydrogenOperations();

        HydrogenOperationResult result = operations.CalculateImplicitHydrogens(source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.True);
        Assert.That(result.UpdatedImplicitHydrogenCount, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[0].ImplicitHydrogenCount, Is.EqualTo(4));
        Assert.That(source.Atoms[0].ImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(result.Calculations, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddExplicitHydrogens_PreservesAssociatedStorageOnCopy()
    {
        Molecule source = BuildSingleCarbon();
        Conformer conformer = new Conformer(1);
        conformer.Coordinates[0] = new Coordinate3D(1.0, 2.0, 3.0);
        source.Conformers.Add(conformer);
        Residue residue = new Residue();
        residue.Id = 8;
        residue.AtomIds.Add(0);
        source.Residues.Add(residue);
        MoleculeModel model = new MoleculeModel();
        model.Id = 2;
        model.AtomIds.Add(0);
        source.Models.Add(model);
        source.Atoms[0].ResidueId = 8;
        source.Atoms[0].ModelId = 2;
        IHydrogenOperations operations = new HydrogenOperations();

        HydrogenOperationResult result = operations.AddExplicitHydrogens(source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.AddedHydrogenCount, Is.EqualTo(4));
        Assert.That(result.Molecule.Atoms, Has.Count.EqualTo(5));
        Assert.That(result.Molecule.Bonds, Has.Count.EqualTo(4));
        Assert.That(result.Molecule.Atoms[0].ImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(result.Molecule.Conformers[0].Coordinates, Has.Count.EqualTo(5));
        Assert.That(result.Molecule.Residues[0].AtomIds, Is.EqualTo(new int[] { 0, 1, 2, 3, 4 }));
        Assert.That(result.Molecule.Models[0].AtomIds, Is.EqualTo(new int[] { 0, 1, 2, 3, 4 }));
        Assert.That(result.Molecule.Atoms[1].ResidueId, Is.EqualTo(8));
        Assert.That(result.Molecule.Atoms[1].ModelId, Is.EqualTo(2));
        Assert.That(source.Atoms, Has.Count.EqualTo(1));
    }

    [Test]
    public void RemoveExplicitHydrogens_PreservesIsotopesByDefault()
    {
        Molecule source = BuildCarbonWithHydrogenAndDeuterium();
        IHydrogenOperations operations = new HydrogenOperations();

        HydrogenOperationResult result = operations.RemoveExplicitHydrogens(source, null);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RemovedHydrogenCount, Is.EqualTo(1));
        Assert.That(result.PreservedIsotopicHydrogenCount, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms, Has.Count.EqualTo(2));
        Assert.That(result.Molecule.Atoms[0].ImplicitHydrogenCount, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(2));
        Assert.That(source.Atoms, Has.Count.EqualTo(3));
    }

    [Test]
    public void RemoveExplicitHydrogens_CanRemoveIsotopesWhenRequested()
    {
        Molecule source = BuildCarbonWithHydrogenAndDeuterium();
        HydrogenOperationOptions options = new HydrogenOperationOptions();
        options.PreserveIsotopicHydrogens = false;
        IHydrogenOperations operations = new HydrogenOperations();

        HydrogenOperationResult result = operations.RemoveExplicitHydrogens(source, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RemovedHydrogenCount, Is.EqualTo(2));
        Assert.That(result.PreservedIsotopicHydrogenCount, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms, Has.Count.EqualTo(1));
        Assert.That(result.Molecule.Atoms[0].ImplicitHydrogenCount, Is.EqualTo(2));
    }

    [Test]
    public void NormalizeHydrogens_PreservesIsotopesAndRecalculatesImplicitCount()
    {
        Molecule source = BuildCarbonWithHydrogenAndDeuterium();
        IHydrogenOperations operations = new HydrogenOperations();

        HydrogenOperationResult result = operations.NormalizeHydrogens(source, null);

        Assert.That(result.Success, Is.True);
        Assert.That(result.RemovedHydrogenCount, Is.EqualTo(1));
        Assert.That(result.PreservedIsotopicHydrogenCount, Is.EqualTo(1));
        Assert.That(result.UpdatedImplicitHydrogenCount, Is.EqualTo(1));
        Assert.That(result.Molecule.Atoms[0].ImplicitHydrogenCount, Is.EqualTo(3));
        Assert.That(result.Molecule.Atoms[1].IsotopeMassNumber, Is.EqualTo(2));
    }

    [Test]
    public void AddExplicitHydrogens_ReportsInvalidValenceWithoutMutation()
    {
        Molecule source = BuildPentavalentCarbon();
        IHydrogenOperations operations = new HydrogenOperations();

        HydrogenOperationResult result = operations.AddExplicitHydrogens(source);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Changed, Is.False);
        Assert.That(result.AddedHydrogenCount, Is.EqualTo(0));
        Assert.That(result.Molecule.Atoms, Has.Count.EqualTo(6));
        Assert.That(result.Report.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Report.Errors[0].Code, Is.EqualTo("HYDROGEN_INVALID_VALENCE"));
        Assert.That(result.Report.Errors[0].AtomId, Is.EqualTo(0));
        Assert.That(source.Atoms, Has.Count.EqualTo(6));
    }

    [Test]
    public void CalculateImplicitHydrogens_ReportsNullInput()
    {
        IHydrogenOperations operations = new HydrogenOperations();

        HydrogenOperationResult result = operations.CalculateImplicitHydrogens(null);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Molecule.Atoms, Is.Empty);
        Assert.That(result.Report.Errors[0].Code, Is.EqualTo("HYDROGEN_NULL_MOLECULE"));
    }

    [Test]
    public void EveryHydrogenEntryPoint_ReportsNullOrInvalidInputWithoutThrowing()
    {
        IHydrogenOperations operations = new HydrogenOperations();

        HydrogenCalculationResult nullAtom = operations.CalculateAtomHydrogens(null, 0);
        HydrogenCalculationResult negativeAtom = operations.CalculateAtomHydrogens(BuildSingleCarbon(), -1);
        HydrogenOperationResult add = operations.AddExplicitHydrogens(null);
        HydrogenOperationResult remove = operations.RemoveExplicitHydrogens(null, null);
        HydrogenOperationResult normalize = operations.NormalizeHydrogens(null, new HydrogenOperationOptions());

        Assert.That(nullAtom.IsValidAtom, Is.False);
        Assert.That(negativeAtom.IsValidAtom, Is.False);
        Assert.That(add.Success, Is.False);
        Assert.That(remove.Success, Is.False);
        Assert.That(normalize.Success, Is.False);
        Assert.That(add.Report.Errors[0].Code, Is.EqualTo("HYDROGEN_NULL_MOLECULE"));
        Assert.That(remove.Report.Errors[0].Code, Is.EqualTo("HYDROGEN_NULL_MOLECULE"));
        Assert.That(normalize.Report.Errors[0].Code, Is.EqualTo("HYDROGEN_NULL_MOLECULE"));
    }

    [Test]
    public void UnknownElementsProduceWarningAndAnUnchangedIndependentResult()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(999, 0, 0, 0, 0, 0, 0, false, "", "X"));
        Molecule source = builder.Build();
        IHydrogenOperations operations = new HydrogenOperations();

        HydrogenOperationResult result = operations.CalculateImplicitHydrogens(source);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Changed, Is.False);
        Assert.That(result.UpdatedImplicitHydrogenCount, Is.EqualTo(0));
        Assert.That(result.Report.Errors, Has.Count.EqualTo(1));
        Assert.That(result.Report.Errors[0].Code, Is.EqualTo("HYDROGEN_UNKNOWN_ELEMENT"));
        Assert.That(result.Report.Errors[0].Severity, Is.EqualTo(ErrorSeverity.Warning));
        Assert.That(result.Molecule, Is.Not.SameAs(source));
        Assert.That(result.Molecule.Atoms[0], Is.Not.SameAs(source.Atoms[0]));
    }

    static Molecule BuildSingleCarbon()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        return builder.Build();
    }

    static Molecule BuildCarbonWithHydrogenAndDeuterium()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int hydrogen = builder.AddAtom(new AtomSpec(1, 0, 0, 0, 0, 0, 0, false, "", "H"));
        int deuterium = builder.AddAtom(new AtomSpec(1, 2, 0, 0, 0, 0, 0, false, "", "D"));
        builder.AddBond(carbon, hydrogen, new BondSpec());
        builder.AddBond(carbon, deuterium, new BondSpec());
        return builder.Build();
    }

    static Molecule BuildPentavalentCarbon()
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int carbon = builder.AddAtom(new AtomSpec(6, 0, 0, 0, 0, 0, 0, false, "", "C"));
        int index = 0;
        while (index < 5)
        {
            int fluorine = builder.AddAtom(new AtomSpec(9, 0, 0, 0, 0, 0, 0, false, "", "F"));
            builder.AddBond(carbon, fluorine, new BondSpec());
            index = index + 1;
        }

        return builder.Build();
    }
}
