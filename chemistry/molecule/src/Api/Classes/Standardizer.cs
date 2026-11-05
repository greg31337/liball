namespace molecule;


/// <summary>
/// Applies configured cleanup operations to a copy of a molecule and reports every change.
/// </summary>
public class Standardizer : IStandardizer
{
    internal ElementTable Elements;
    internal ValenceCalculator ValenceCalculator;
    internal SaltRemover SaltRemover;
    internal MetalDisconnector MetalDisconnector;
    internal HydrogenOperations HydrogenOperations;
    internal ChargeNormalizer ChargeNormalizer;
    internal Reionizer Reionizer;
    internal TautomerCanonicalizer TautomerCanonicalizer;
    internal StereoCleaner StereoCleaner;
    internal AromaticityModel AromaticityModel;
    internal Kekulizer Kekulizer;
    internal DeterministicSerializer Serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Standardizer"/> class.
    /// </summary>
    public Standardizer()
    {
        Elements = new ElementTable();
        ValenceCalculator = new ValenceCalculator();
        SaltRemover = new SaltRemover();
        MetalDisconnector = new MetalDisconnector();
        HydrogenOperations = new HydrogenOperations();
        ChargeNormalizer = new ChargeNormalizer();
        Reionizer = new Reionizer();
        TautomerCanonicalizer = new TautomerCanonicalizer();
        StereoCleaner = new StereoCleaner();
        AromaticityModel = new AromaticityModel();
        Kekulizer = new Kekulizer();
        Serializer = new DeterministicSerializer();
    }

    /// <summary>
    /// Returns the default standardization policy.
    /// </summary>
    /// <returns>The default standardization policy.</returns>
    public StandardizationPolicy GetDefaultPolicy()
    {
        return StandardizationPolicy.None;
    }

    /// <summary>
    /// Runs the enabled cleanup stages and returns only the resulting molecule.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule StandardizeMolecule(Molecule? molecule, StandardizationOptions? options)
    {
        StandardizationResult result = Standardize(molecule, options);
        return result.Molecule;
    }

    /// <summary>
    /// Runs the enabled cleanup stages on a copy of the input molecule and returns changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public StandardizationResult Standardize(Molecule? molecule, StandardizationOptions? options)
    {
        StandardizationResult result = new StandardizationResult();
        if (molecule == null)
        {
            result.Success = false;
            result.Report.AddError("STANDARDIZER_NULL_MOLECULE", "Cannot standardize a null molecule.");
            return result;
        }

        StandardizationOptions safeOptions = NormalizeStandardizationOptions(options);
        result.Molecule = MoleculeBuilder.CloneMolecule(molecule);

        if (safeOptions.EnableSaltRemoval)
        {
            ApplySaltRemoval(result);
            if (!result.Success)
            {
                return result;
            }
        }

        if (safeOptions.EnableMetalDisconnection)
        {
            ApplyMetalDisconnection(result);
            if (!result.Success)
            {
                return result;
            }
        }

        if (safeOptions.EnableExplicitHydrogenNormalization)
        {
            ApplyExplicitHydrogenNormalization(result, safeOptions);
            if (!result.Success)
            {
                return result;
            }
        }

        if (safeOptions.EnableChargeNormalization)
        {
            ApplyChargeNormalization(result);
            if (!result.Success)
            {
                return result;
            }
        }

        if (safeOptions.EnableReionization)
        {
            ApplyReionization(result);
            if (!result.Success)
            {
                return result;
            }
        }

        if (safeOptions.EnableIsotopeCleanup)
        {
            ApplyIsotopeCleanup(result, safeOptions);
            if (!result.Success)
            {
                return result;
            }
        }

        if (safeOptions.EnableRadicalCleanup)
        {
            ApplyRadicalCleanup(result, safeOptions);
            if (!result.Success)
            {
                return result;
            }
        }

        if (safeOptions.EnableTautomerEnumeration || safeOptions.EnableTautomerCanonicalization)
        {
            ApplyTautomerCanonicalization(result, safeOptions);
            if (!result.Success)
            {
                return result;
            }
        }

        if (safeOptions.EnableAromaticityNormalization)
        {
            ApplyAromaticityNormalization(result, safeOptions);
            if (!result.Success)
            {
                return result;
            }
        }

        if (safeOptions.EnableStereoCleanup)
        {
            ApplyStereoCleanup(result);
        }

        return result;
    }

    internal void ApplySaltRemoval(StandardizationResult result)
    {
        Molecule before = MoleculeBuilder.CloneMolecule(result.Molecule);
        SaltRemovalResult step = SaltRemover.RemoveSaltsWithResult(result.Molecule);
        if (step.Success)
        {
            result.Molecule = step.Parent;
        }

        RegisterStep(result, "salt-removal", step.Changed, step.Success, step.RemovedFragmentCount.ToString(), step.Report, before, result.Molecule);
    }

    internal void ApplyMetalDisconnection(StandardizationResult result)
    {
        Molecule before = MoleculeBuilder.CloneMolecule(result.Molecule);
        MetalDisconnectionResult step = MetalDisconnector.DisconnectWithResult(result.Molecule);
        if (step.Success)
        {
            result.Molecule = step.Molecule;
        }

        RegisterStep(result, "metal-disconnection", step.Changed, step.Success, step.DisconnectionCount.ToString(), step.Report, before, result.Molecule);
    }

    internal void ApplyExplicitHydrogenNormalization(StandardizationResult result, StandardizationOptions options)
    {
        Molecule before = MoleculeBuilder.CloneMolecule(result.Molecule);
        HydrogenOperationResult step = HydrogenOperations.NormalizeHydrogens(result.Molecule, options.HydrogenNormalization);
        if (step.Success)
        {
            result.Molecule = step.Molecule;
        }

        string detail = step.RemovedHydrogenCount.ToString() + ":" + step.UpdatedImplicitHydrogenCount.ToString();
        RegisterStep(result, "explicit-hydrogen-normalization", step.Changed, step.Success, detail, step.Report, before, result.Molecule);
    }

    internal void ApplyChargeNormalization(StandardizationResult result)
    {
        Molecule before = MoleculeBuilder.CloneMolecule(result.Molecule);
        ChargeNormalizationResult step = ChargeNormalizer.NormalizeWithResult(result.Molecule);
        if (step.Success)
        {
            result.Molecule = step.Molecule;
        }

        StandardizationMutation mutation = RegisterStep(result, "charge-normalization", step.Changed, step.Success, step.AppliedRuleCount.ToString(), step.Report, before, result.Molecule);
        CopyRuleNames(step.AppliedRuleNames, mutation.RuleNames);
    }

    internal void ApplyReionization(StandardizationResult result)
    {
        Molecule before = MoleculeBuilder.CloneMolecule(result.Molecule);
        ReionizationResult step = Reionizer.ReionizeWithResult(result.Molecule);
        if (step.Success)
        {
            result.Molecule = step.Molecule;
        }

        RegisterStep(result, "reionization", step.Changed, step.Success, step.TransferCount.ToString(), step.Report, before, result.Molecule);
    }

    internal void ApplyIsotopeCleanup(StandardizationResult result, StandardizationOptions options)
    {
        Molecule before = MoleculeBuilder.CloneMolecule(result.Molecule);
        IsotopeCleanupResult step = CleanupIsotopesWithResult(result.Molecule, options.IsotopeCleanup);
        if (step.Success)
        {
            result.Molecule = step.Molecule;
        }

        RegisterStep(result, "isotope-cleanup", step.Changed, step.Success, step.ClearedIsotopeCount.ToString(), step.Report, before, result.Molecule);
    }

    internal void ApplyRadicalCleanup(StandardizationResult result, StandardizationOptions options)
    {
        Molecule before = MoleculeBuilder.CloneMolecule(result.Molecule);
        RadicalCleanupResult step = CleanupRadicalsWithResult(result.Molecule, options.RadicalCleanup);
        if (step.Success)
        {
            result.Molecule = step.Molecule;
        }

        RegisterStep(result, "radical-cleanup", step.Changed, step.Success, step.ClearedRadicalCount.ToString(), step.Report, before, result.Molecule);
    }

    internal void ApplyTautomerCanonicalization(StandardizationResult result, StandardizationOptions options)
    {
        Molecule beforeMolecule = MoleculeBuilder.CloneMolecule(result.Molecule);
        string before = Serializer.Serialize(result.Molecule);
        TautomerCanonicalizationResult step = TautomerCanonicalizer.CanonicalizeWithResult(result.Molecule, options.TautomerEnumeration);
        bool changed = false;
        if (step.Success)
        {
            result.Molecule = step.Molecule;
            changed = before != Serializer.Serialize(result.Molecule);
        }

        RegisterStep(result, "tautomer-canonicalization", changed, step.Success, step.SelectedIndex.ToString(), step.Report, beforeMolecule, result.Molecule);
    }

    internal void ApplyAromaticityNormalization(StandardizationResult result, StandardizationOptions options)
    {
        Molecule before = MoleculeBuilder.CloneMolecule(result.Molecule);
        AromaticityPerceptionResult step = AromaticityModel.PerceiveWithOptions(result.Molecule, options.AromaticityNormalization);
        bool useKekulizationFallback = options.EnableKekulizationFallback && (step.UsedFallback || !step.Success);
        if (step.Success)
        {
            result.Molecule = step.Molecule;
        }

        bool changed = Serializer.Serialize(before) != Serializer.Serialize(result.Molecule);
        MoleculeReport stageReport = step.Report;
        bool stageSuccess = step.Success;
        if (useKekulizationFallback && !step.Success)
        {
            stageReport = new MoleculeReport();
            stageSuccess = true;
        }

        RegisterStep(result, "aromaticity-normalization", changed, stageSuccess, step.AromaticRingIds.Count.ToString(), stageReport, before, result.Molecule);
        if (!stageSuccess)
        {
            return;
        }

        if (useKekulizationFallback)
        {
            ApplyKekulizationFallback(result, options, before);
        }
    }

    internal void ApplyKekulizationFallback(StandardizationResult result, StandardizationOptions options, Molecule source)
    {
        Molecule before = MoleculeBuilder.CloneMolecule(result.Molecule);
        KekulizationResult step = Kekulizer.KekulizeWithOptions(source, options.KekulizationFallback);
        if (step.Success)
        {
            result.Molecule = step.Molecule;
        }

        bool changed = Serializer.Serialize(before) != Serializer.Serialize(result.Molecule);
        RegisterStep(result, "kekule-fallback", changed, step.Success, step.KekulizedBondIds.Count.ToString(), step.Report, before, result.Molecule);
    }

    internal void ApplyStereoCleanup(StandardizationResult result)
    {
        Molecule before = MoleculeBuilder.CloneMolecule(result.Molecule);
        StereoCleanupResult step = StereoCleaner.Clean(result.Molecule);
        bool changed = step.RemovedRecordCount > 0 || step.RemovedGroupCount > 0;
        RegisterStep(result, "stereo-cleanup", changed, step.Success, step.RemovedRecordCount.ToString(), step.Report, before, result.Molecule);
    }

    internal StandardizationMutation RegisterStep(StandardizationResult result, string stepName, bool changed, bool success, string detail, MoleculeReport report, Molecule before, Molecule after)
    {
        StandardizationMutation mutation = new StandardizationMutation();
        mutation.StepName = stepName;
        mutation.Changed = changed;
        mutation.Success = success;
        mutation.Detail = detail;
        CollectMutationIds(mutation, before, after);
        result.Mutations.Add(mutation);
        result.StepCount = result.StepCount + 1;
        if (changed)
        {
            result.Changed = true;
            result.MutationCount = result.MutationCount + 1;
        }

        result.AtomMutationCount = result.AtomMutationCount + mutation.AtomMutationCount;
        result.BondMutationCount = result.BondMutationCount + mutation.BondMutationCount;

        if (!success)
        {
            result.Success = false;
        }

        CopyReport(report, result.Report);
        return mutation;
    }

    internal void CollectMutationIds(StandardizationMutation mutation, Molecule before, Molecule after)
    {
        int atomCount = before.Atoms.Count;
        if (after.Atoms.Count > atomCount)
        {
            atomCount = after.Atoms.Count;
        }

        int atomIndex = 0;
        while (atomIndex < atomCount)
        {
            if (atomIndex >= before.Atoms.Count || atomIndex >= after.Atoms.Count || AtomsDiffer(before.Atoms[atomIndex], after.Atoms[atomIndex]))
            {
                mutation.AtomIds.Add(atomIndex);
            }

            atomIndex = atomIndex + 1;
        }

        int bondCount = before.Bonds.Count;
        if (after.Bonds.Count > bondCount)
        {
            bondCount = after.Bonds.Count;
        }

        int bondIndex = 0;
        while (bondIndex < bondCount)
        {
            if (bondIndex >= before.Bonds.Count || bondIndex >= after.Bonds.Count || BondsDiffer(before.Bonds[bondIndex], after.Bonds[bondIndex]))
            {
                mutation.BondIds.Add(bondIndex);
            }

            bondIndex = bondIndex + 1;
        }

        mutation.AtomMutationCount = mutation.AtomIds.Count;
        mutation.BondMutationCount = mutation.BondIds.Count;
    }

    internal bool AtomsDiffer(Atom left, Atom right)
    {
        return left.AtomicNumber != right.AtomicNumber
            || left.IsotopeMassNumber != right.IsotopeMassNumber
            || left.FormalCharge != right.FormalCharge
            || left.ExplicitHydrogenCount != right.ExplicitHydrogenCount
            || left.ImplicitHydrogenCount != right.ImplicitHydrogenCount
            || left.RadicalElectronCount != right.RadicalElectronCount
            || left.AtomMapNumber != right.AtomMapNumber
            || left.IsAromatic != right.IsAromatic;
    }

    internal bool BondsDiffer(Bond left, Bond right)
    {
        return left.BeginAtomId != right.BeginAtomId
            || left.EndAtomId != right.EndAtomId
            || left.Order != right.Order
            || left.IsAromatic != right.IsAromatic
            || left.Stereo != right.Stereo
            || left.Direction != right.Direction
            || left.IsDative != right.IsDative;
    }

    internal void CopyRuleNames(System.Collections.Generic.List<string> source, System.Collections.Generic.List<string> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }

    internal void CopyReport(MoleculeReport? source, MoleculeReport? target)
    {
        if (source == null || target == null)
        {
            return;
        }

        int index = 0;
        while (index < source.Errors.Count)
        {
            target.Add(source.Errors[index]);
            index = index + 1;
        }
    }

    internal StandardizationOptions NormalizeStandardizationOptions(StandardizationOptions? options)
    {
        if (options == null)
        {
            return new StandardizationOptions();
        }

        if (options.IsotopeCleanup == null)
        {
            options.IsotopeCleanup = new IsotopeCleanupPolicy();
        }

        if (options.RadicalCleanup == null)
        {
            options.RadicalCleanup = new RadicalCleanupPolicy();
        }

        if (options.TautomerEnumeration == null)
        {
            options.TautomerEnumeration = new TautomerEnumerationOptions();
        }

        if (options.HydrogenNormalization == null)
        {
            options.HydrogenNormalization = new HydrogenOperationOptions();
        }

        if (options.AromaticityNormalization == null)
        {
            options.AromaticityNormalization = new AromaticityOptions();
        }

        if (options.KekulizationFallback == null)
        {
            options.KekulizationFallback = new KekulizationOptions();
        }

        return options;
    }

    /// <summary>
    /// Cleanup isotopes.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule CleanupIsotopes(Molecule? molecule, IsotopeCleanupPolicy? policy)
    {
        IsotopeCleanupResult result = CleanupIsotopesWithResult(molecule, policy);
        return result.Molecule;
    }

    /// <summary>
    /// Cleanup isotopess the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public IsotopeCleanupResult CleanupIsotopesWithResult(Molecule? molecule, IsotopeCleanupPolicy? policy)
    {
        IsotopeCleanupResult result = new IsotopeCleanupResult();
        if (molecule == null)
        {
            result.Success = false;
            result.Report.AddError("ISOTOPE_CLEANUP_NULL_MOLECULE", "Cannot clean isotopes for a null molecule.");
            return result;
        }

        IsotopeCleanupPolicy safePolicy = NormalizeIsotopePolicy(policy);
        result.Molecule = MoleculeBuilder.CloneMolecule(molecule);

        int atomIndex = 0;
        while (atomIndex < result.Molecule.Atoms.Count)
        {
            Atom atom = result.Molecule.Atoms[atomIndex];
            if (atom.IsotopeMassNumber > 0)
            {
                if (ShouldClearIsotope(atom, safePolicy))
                {
                    atom.IsotopeMassNumber = 0;
                    result.ClearedAtomIds.Add(atom.Id);
                    result.ClearedIsotopeCount = result.ClearedIsotopeCount + 1;
                    result.Report.AddInfo("ISOTOPE_CLEARED", atom.Id.ToString());
                }
                else
                {
                    result.PreservedAtomIds.Add(atom.Id);
                    result.PreservedIsotopeCount = result.PreservedIsotopeCount + 1;
                }
            }

            atomIndex = atomIndex + 1;
        }

        if (result.ClearedIsotopeCount > 0)
        {
            result.Changed = true;
            result.Molecule.ClearCaches();
        }

        return result;
    }

    internal bool ShouldClearIsotope(Atom atom, IsotopeCleanupPolicy policy)
    {
        if (policy.PreserveHydrogenIsotopes && IsPreservedHydrogenIsotope(atom))
        {
            return false;
        }

        if (policy.RemoveAllIsotopes)
        {
            return true;
        }

        if (policy.RemoveInvalidIsotopes && !IsSupportedIsotope(atom))
        {
            return true;
        }

        if (policy.RemoveMajorIsotopes && IsMajorIsotope(atom))
        {
            return true;
        }

        return false;
    }

    internal bool IsPreservedHydrogenIsotope(Atom atom)
    {
        if (atom.AtomicNumber != 1)
        {
            return false;
        }

        if (atom.IsotopeMassNumber == 2 || atom.IsotopeMassNumber == 3)
        {
            return true;
        }

        return false;
    }

    internal bool IsSupportedIsotope(Atom atom)
    {
        if (atom.IsotopeMassNumber <= 0)
        {
            return true;
        }

        if (IsPreservedHydrogenIsotope(atom))
        {
            return true;
        }

        Isotope isotope = Elements.GetIsotope(atom.AtomicNumber, atom.IsotopeMassNumber);
        return isotope.AtomicNumber != 0;
    }

    internal bool IsMajorIsotope(Atom atom)
    {
        if (atom.IsotopeMassNumber <= 0)
        {
            return false;
        }

        Isotope isotope = Elements.GetMajorIsotope(atom.AtomicNumber);
        if (isotope.AtomicNumber == 0)
        {
            return false;
        }

        return isotope.MassNumber == atom.IsotopeMassNumber;
    }

    internal IsotopeCleanupPolicy NormalizeIsotopePolicy(IsotopeCleanupPolicy? policy)
    {
        if (policy == null)
        {
            return new IsotopeCleanupPolicy();
        }

        return policy;
    }

    /// <summary>
    /// Cleanup radicals.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>The resulting molecule. Operations documented as copy-based do not mutate their input.</returns>
    public Molecule CleanupRadicals(Molecule? molecule, RadicalCleanupPolicy? policy)
    {
        RadicalCleanupResult result = CleanupRadicalsWithResult(molecule, policy);
        return result.Molecule;
    }

    /// <summary>
    /// Cleanup radicalss the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="policy">The identity or cleanup policy to apply.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public RadicalCleanupResult CleanupRadicalsWithResult(Molecule? molecule, RadicalCleanupPolicy? policy)
    {
        RadicalCleanupResult result = new RadicalCleanupResult();
        if (molecule == null)
        {
            result.Success = false;
            result.Report.AddError("RADICAL_CLEANUP_NULL_MOLECULE", "Cannot clean radicals for a null molecule.");
            return result;
        }

        RadicalCleanupPolicy safePolicy = NormalizeRadicalPolicy(policy);
        result.Molecule = MoleculeBuilder.CloneMolecule(molecule);

        int atomIndex = 0;
        while (atomIndex < result.Molecule.Atoms.Count)
        {
            Atom atom = result.Molecule.Atoms[atomIndex];
            if (atom.RadicalElectronCount != 0)
            {
                bool invalid = IsInvalidRadical(result.Molecule, atom, safePolicy);
                bool clear = ShouldClearRadical(result.Molecule, atom, safePolicy);
                if (invalid)
                {
                    RegisterInvalidRadical(result, atom.Id);
                }

                if (clear)
                {
                    atom.RadicalElectronCount = 0;
                    RegisterClearedRadical(result, atom.Id);
                }
                else
                {
                    result.PreservedAtomIds.Add(atom.Id);
                    result.PreservedRadicalCount = result.PreservedRadicalCount + 1;
                    if (invalid)
                    {
                        result.Success = false;
                    }
                }
            }

            atomIndex = atomIndex + 1;
        }

        if (result.ClearedRadicalCount > 0)
        {
            result.Changed = true;
            result.Molecule.ClearCaches();
        }

        return result;
    }

    internal bool ShouldClearRadical(Molecule molecule, Atom atom, RadicalCleanupPolicy policy)
    {
        if (policy.RemoveAllRadicals)
        {
            return true;
        }

        if (policy.RemoveInvalidRadicals && IsInvalidRadical(molecule, atom, policy))
        {
            return true;
        }

        return false;
    }

    internal bool IsInvalidRadical(Molecule molecule, Atom atom, RadicalCleanupPolicy policy)
    {
        if (atom.RadicalElectronCount == 0)
        {
            return false;
        }

        if (atom.RadicalElectronCount < 0)
        {
            return true;
        }

        if (policy.MaximumRadicalElectronCount > 0 && atom.RadicalElectronCount > policy.MaximumRadicalElectronCount)
        {
            return true;
        }

        if (policy.ValidateRadicalValence && !IsRadicalValenceAllowed(molecule, atom))
        {
            return true;
        }

        return false;
    }

    internal bool IsRadicalValenceAllowed(Molecule? molecule, Atom? atom)
    {
        if (molecule == null || atom == null)
        {
            return false;
        }

        if (!molecule.IsValidAtomId(atom.Id))
        {
            return false;
        }

        ValenceResult result = ValenceCalculator.CalculateAtomValence(molecule, atom.Id);
        return result.IsRadicalAdjustedAllowed;
    }

    internal RadicalCleanupPolicy NormalizeRadicalPolicy(RadicalCleanupPolicy? policy)
    {
        if (policy == null)
        {
            return new RadicalCleanupPolicy();
        }

        return policy;
    }

    internal void RegisterInvalidRadical(RadicalCleanupResult result, int atomId)
    {
        result.InvalidAtomIds.Add(atomId);
        result.InvalidRadicalCount = result.InvalidRadicalCount + 1;
        result.Report.AddWarning("RADICAL_INVALID", atomId.ToString());
    }

    internal void RegisterClearedRadical(RadicalCleanupResult result, int atomId)
    {
        result.ClearedAtomIds.Add(atomId);
        result.ClearedRadicalCount = result.ClearedRadicalCount + 1;
        result.Report.AddInfo("RADICAL_CLEARED", atomId.ToString());
    }
}
