namespace molecule;


/// <summary>
/// Calculates and edits explicit and implicit hydrogens on independent molecule copies.
/// </summary>
public class HydrogenOperations : IHydrogenOperations
{
    internal HydrogenCalculator Calculator;

    /// <summary>
    /// Initializes the public hydrogen operation service.
    /// </summary>
    public HydrogenOperations()
    {
        Calculator = new HydrogenCalculator();
    }

    /// <summary>
    /// Calculates hydrogen and valence information for one atom without changing the molecule.
    /// </summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The calculated hydrogen information, including atom and valence validity.</returns>
    public HydrogenCalculationResult CalculateAtomHydrogens(Molecule? molecule, int atomId)
    {
        return Calculator.CalculateAtomHydrogens(molecule, atomId);
    }

    /// <summary>
    /// Calculates and assigns implicit hydrogen counts on an independent molecule copy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The copied molecule, per-atom calculations, changes, and diagnostics.</returns>
    public HydrogenOperationResult CalculateImplicitHydrogens(Molecule? molecule)
    {
        HydrogenOperationResult result = Prepare(molecule);
        if (!result.Success)
        {
            return result;
        }

        Molecule original = MoleculeBuilder.CloneMolecule(result.Molecule);
        AssignCalculatedImplicitHydrogens(result.Molecule, result.Calculations);
        Complete(result, original);
        return result;
    }

    /// <summary>
    /// Calculates missing implicit hydrogens and expands them into explicit graph atoms on an independent copy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The copied molecule, per-atom calculations, changes, and diagnostics.</returns>
    public HydrogenOperationResult AddExplicitHydrogens(Molecule? molecule)
    {
        HydrogenOperationResult result = Prepare(molecule);
        if (!result.Success)
        {
            return result;
        }

        Molecule original = MoleculeBuilder.CloneMolecule(result.Molecule);
        AssignCalculatedImplicitHydrogens(result.Molecule, result.Calculations);
        result.AddedHydrogenCount = Calculator.AddHydrogens(result.Molecule);
        Complete(result, original);
        return result;
    }

    /// <summary>
    /// Removes eligible explicit graph hydrogen atoms on an independent copy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">Options controlling isotope preservation; <c>null</c> uses safe defaults.</param>
    /// <returns>The copied molecule, per-atom calculations, changes, and diagnostics.</returns>
    public HydrogenOperationResult RemoveExplicitHydrogens(Molecule? molecule, HydrogenOperationOptions? options)
    {
        HydrogenOperationResult result = Prepare(molecule);
        if (!result.Success)
        {
            return result;
        }

        HydrogenOperationOptions safeOptions = NormalizeOptions(options);
        Molecule original = MoleculeBuilder.CloneMolecule(result.Molecule);
        result.PreservedIsotopicHydrogenCount = CountPreservedIsotopicHydrogens(result.Molecule, safeOptions);
        result.RemovedHydrogenCount = Calculator.RemoveHydrogens(result.Molecule, safeOptions.PreserveIsotopicHydrogens);
        Complete(result, original);
        return result;
    }

    /// <summary>
    /// Converts eligible explicit graph hydrogens to calculated implicit counts on an independent copy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">Options controlling isotope preservation; <c>null</c> uses safe defaults.</param>
    /// <returns>The copied molecule, per-atom calculations, changes, and diagnostics.</returns>
    public HydrogenOperationResult NormalizeHydrogens(Molecule? molecule, HydrogenOperationOptions? options)
    {
        HydrogenOperationResult result = Prepare(molecule);
        if (!result.Success)
        {
            return result;
        }

        HydrogenOperationOptions safeOptions = NormalizeOptions(options);
        Molecule original = MoleculeBuilder.CloneMolecule(result.Molecule);
        result.PreservedIsotopicHydrogenCount = CountPreservedIsotopicHydrogens(result.Molecule, safeOptions);
        result.RemovedHydrogenCount = Calculator.RemoveHydrogens(result.Molecule, safeOptions.PreserveIsotopicHydrogens);
        AssignCurrentImplicitHydrogens(result.Molecule);
        Complete(result, original);
        return result;
    }

    internal HydrogenOperationResult Prepare(Molecule? molecule)
    {
        HydrogenOperationResult result = new HydrogenOperationResult();
        if (molecule == null)
        {
            result.Success = false;
            result.Report.AddError("HYDROGEN_NULL_MOLECULE", "Cannot perform a hydrogen operation on a null molecule.");
            return result;
        }

        result.Molecule = MoleculeBuilder.CloneMolecule(molecule);
        int atomIndex = 0;
        while (atomIndex < result.Molecule.Atoms.Count)
        {
            HydrogenCalculationResult calculation = Calculator.CalculateAtomHydrogens(result.Molecule, atomIndex);
            result.Calculations.Add(calculation);
            if (!calculation.IsKnownElement)
            {
                result.Report.Add(new MoleculeError("HYDROGEN_UNKNOWN_ELEMENT", "Hydrogen count cannot be calculated for an unknown element.", DiagnosticLocation.CreateEmpty(), atomIndex, -1, ErrorSeverity.Warning));
            }
            else if (!calculation.IsValidValence)
            {
                result.Success = false;
                result.Report.Add(new MoleculeError("HYDROGEN_INVALID_VALENCE", "Hydrogen calculation would require an invalid atom valence.", DiagnosticLocation.CreateEmpty(), atomIndex, -1, ErrorSeverity.Error));
            }

            atomIndex = atomIndex + 1;
        }

        return result;
    }

    internal void AssignCalculatedImplicitHydrogens(Molecule molecule, System.Collections.Generic.List<HydrogenCalculationResult> calculations)
    {
        int index = 0;
        while (index < calculations.Count)
        {
            HydrogenCalculationResult calculation = calculations[index];
            if (calculation.IsKnownElement && calculation.AtomicNumber != 1 && calculation.IsValidValence)
            {
                molecule.Atoms[calculation.AtomId].ImplicitHydrogenCount = calculation.CalculatedImplicitHydrogenCount;
            }

            index = index + 1;
        }
    }

    internal HydrogenOperationOptions NormalizeOptions(HydrogenOperationOptions? options)
    {
        HydrogenOperationOptions safeOptions = new HydrogenOperationOptions();
        if (options != null)
        {
            safeOptions.PreserveIsotopicHydrogens = options.PreserveIsotopicHydrogens;
        }

        return safeOptions;
    }

    internal void AssignCurrentImplicitHydrogens(Molecule molecule)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            HydrogenCalculationResult calculation = Calculator.CalculateAtomHydrogens(molecule, atomIndex);
            if (calculation.IsKnownElement && calculation.AtomicNumber != 1 && calculation.IsValidValence)
            {
                molecule.Atoms[atomIndex].ImplicitHydrogenCount = calculation.CalculatedImplicitHydrogenCount;
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal int CountPreservedIsotopicHydrogens(Molecule molecule, HydrogenOperationOptions options)
    {
        if (!options.PreserveIsotopicHydrogens)
        {
            return 0;
        }

        int count = 0;
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[atomIndex];
            if (atom.AtomicNumber == 1 && atom.IsotopeMassNumber > 0 && Calculator.CanRemoveHydrogen(molecule, atomIndex, false))
            {
                count = count + 1;
            }

            atomIndex = atomIndex + 1;
        }

        return count;
    }

    internal void Complete(HydrogenOperationResult result, Molecule original)
    {
        result.UpdatedImplicitHydrogenCount = CountImplicitChanges(original, result.Molecule);
        result.Changed = result.AddedHydrogenCount > 0 || result.RemovedHydrogenCount > 0 || result.UpdatedImplicitHydrogenCount > 0;
        if (result.Changed)
        {
            result.Molecule.ClearCaches();
        }
    }

    internal int CountImplicitChanges(Molecule original, Molecule result)
    {
        int count = 0;
        int originalIndex = 0;
        int resultIndex = 0;
        while (originalIndex < original.Atoms.Count)
        {
            if (original.Atoms[originalIndex].AtomicNumber != 1)
            {
                while (resultIndex < result.Atoms.Count && result.Atoms[resultIndex].AtomicNumber == 1)
                {
                    resultIndex = resultIndex + 1;
                }

                if (resultIndex < result.Atoms.Count && original.Atoms[originalIndex].ImplicitHydrogenCount != result.Atoms[resultIndex].ImplicitHydrogenCount)
                {
                    count = count + 1;
                }

                resultIndex = resultIndex + 1;
            }

            originalIndex = originalIndex + 1;
        }

        return count;
    }
}
