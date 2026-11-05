using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides kekulizer data and operations for molecular graphs.
/// </summary>
public class Kekulizer : IKekulizer
{
    internal AromaticityModel Aromaticity;

    /// <summary>
    /// Initializes a new instance of the <see cref="Kekulizer"/> class.
    /// </summary>
    public Kekulizer()
    {
        Aromaticity = new AromaticityModel();
    }

    /// <summary>
    /// Returns a copy whose supported aromatic bonds have deterministic alternating bond orders.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public KekulizationResult Kekulize(Molecule? molecule)
    {
        return KekulizeWithOptions(molecule, null);
    }

    /// <summary>
    /// Assigns deterministic alternating bond orders to supported aromatic systems in the supplied molecule.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public KekulizationResult KekulizeInPlace(Molecule? molecule)
    {
        return KekulizeInPlaceConfigured(molecule, new KekulizationOptions());
    }

    /// <summary>
    /// Returns a copy with Kekulé bond orders assigned under the selected aromaticity model and fallback policy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The Kekulization options; <c>null</c> uses safe defaults.</param>
    /// <returns>An object containing assigned bonds, failures, fallback state, and diagnostics.</returns>
    public KekulizationResult KekulizeWithOptions(Molecule? molecule, KekulizationOptions? options)
    {
        KekulizationResult result = new KekulizationResult();
        if (molecule == null)
        {
            result.Report.AddError("NULL_MOLECULE", "Cannot kekulize a null molecule.");
            return result;
        }

        Molecule clone = MoleculeBuilder.CloneMolecule(molecule);
        result = KekulizeInPlaceConfigured(clone, NormalizeOptions(options));
        result.Molecule = clone;
        return result;
    }

    internal KekulizationResult KekulizeInPlaceConfigured(Molecule? molecule, KekulizationOptions options)
    {
        KekulizationResult result = new KekulizationResult();
        result.Model = options.Model;
        if (molecule == null)
        {
            result.Report.AddError("NULL_MOLECULE", "Cannot kekulize a null molecule.");
            return result;
        }

        result.Molecule = molecule;
        if (!Aromaticity.ModelIsSupported(options.Model))
        {
            result.Report.AddError("KEKULIZATION_INVALID_MODEL", "The requested aromaticity model is not supported.");
            return result;
        }

        result.Success = true;

        AromaticityCandidateSet candidates = Aromaticity.FindCandidatesForModel(molecule, options.Model);
        RingSet ringSet = Aromaticity.Rings.EnsureRingCache(molecule);

        int ringIndex = 0;
        while (ringIndex < ringSet.Rings.Count)
        {
            Ring ring = ringSet.Rings[ringIndex];
            if (RingHasAromaticBond(molecule, ring))
            {
                AromaticRingCandidate? candidate = FindRingCandidate(candidates, ring.Id);
                if (candidate != null && candidate.IsCandidate)
                {
                    List<int> doubleBondIds = new List<int>();
                    bool found = FindExactDoubleBondPattern(molecule, ring, candidate, doubleBondIds);
                    if (found)
                    {
                        ApplyPattern(molecule, ring, doubleBondIds, result);
                    }
                    else
                    {
                        HandleFailedAssignment(molecule, ring, GetFallbackDoubleBondCount(candidate), "No exact Kekulé matching was found.", options, result);
                    }
                }
                else
                {
                    HandleFailedAssignment(molecule, ring, ring.Size / 2, "Ring was not a candidate under the selected aromaticity model.", options, result);
                }
            }

            ringIndex = ringIndex + 1;
        }

        ClearAromaticAtomsWithoutAromaticBonds(molecule);
        molecule.ClearCaches();
        return result;
    }

    internal KekulizationOptions NormalizeOptions(KekulizationOptions? options)
    {
        KekulizationOptions safeOptions = new KekulizationOptions();
        if (options != null)
        {
            safeOptions.Model = options.Model;
            safeOptions.UseDeterministicFallback = options.UseDeterministicFallback;
        }

        return safeOptions;
    }

    internal void HandleFailedAssignment(Molecule molecule, Ring ring, int fallbackDoubleBondCount, string reason, KekulizationOptions options, KekulizationResult result)
    {
        AddUniqueInt(result.FailedRingIds, ring.Id);
        if (options.UseDeterministicFallback)
        {
            result.UsedFallback = true;
            AddFallbackWarning(result.Report, ring, reason + " Deterministic fallback was used.");
            ApplyFallbackPattern(molecule, ring, fallbackDoubleBondCount, result);
        }
        else
        {
            result.Success = false;
            AddFailureError(result.Report, ring, reason + " Fallback is disabled.");
        }
    }

    internal void AddFailureError(MoleculeReport report, Ring ring, string message)
    {
        int bondId = -1;
        if (ring.BondIds.Count > 0)
        {
            bondId = ring.BondIds[0];
            int index = 1;
            while (index < ring.BondIds.Count)
            {
                if (ring.BondIds[index] < bondId)
                {
                    bondId = ring.BondIds[index];
                }

                index = index + 1;
            }
        }

        MoleculeError error = new MoleculeError("KEKULIZATION_FAILED", message, DiagnosticLocation.CreateEmpty(), -1, bondId, ErrorSeverity.Error);
        report.Add(error);
    }

    internal AromaticRingCandidate? FindRingCandidate(AromaticityCandidateSet? candidates, int ringId)
    {
        if (candidates == null)
        {
            return null;
        }

        int index = 0;
        while (index < candidates.RingCandidates.Count)
        {
            if (candidates.RingCandidates[index].RingId == ringId)
            {
                return candidates.RingCandidates[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool FindExactDoubleBondPattern(Molecule molecule, Ring ring, AromaticRingCandidate candidate, List<int> doubleBondIds)
    {
        List<int> oneElectronAtomIds = GetOneElectronAtomIds(candidate);
        if (oneElectronAtomIds.Count == 0 || oneElectronAtomIds.Count % 2 != 0)
        {
            return false;
        }

        List<int> eligibleBondIds = GetEligibleDoubleBondIds(molecule, ring, oneElectronAtomIds);
        SortInts(eligibleBondIds);
        List<int> matchedAtomIds = new List<int>();
        return SearchDoubleBondMatching(molecule, eligibleBondIds, 0, oneElectronAtomIds.Count, matchedAtomIds, doubleBondIds);
    }

    internal bool SearchDoubleBondMatching(Molecule molecule, List<int> eligibleBondIds, int bondIndex, int requiredAtomCount, List<int> matchedAtomIds, List<int> doubleBondIds)
    {
        if (matchedAtomIds.Count == requiredAtomCount)
        {
            return true;
        }

        if (bondIndex >= eligibleBondIds.Count)
        {
            return false;
        }

        int bondId = eligibleBondIds[bondIndex];
        Bond bond = molecule.Bonds[bondId];

        if (!ContainsInt(matchedAtomIds, bond.BeginAtomId) && !ContainsInt(matchedAtomIds, bond.EndAtomId))
        {
            matchedAtomIds.Add(bond.BeginAtomId);
            matchedAtomIds.Add(bond.EndAtomId);
            doubleBondIds.Add(bondId);

            if (SearchDoubleBondMatching(molecule, eligibleBondIds, bondIndex + 1, requiredAtomCount, matchedAtomIds, doubleBondIds))
            {
                return true;
            }

            doubleBondIds.RemoveAt(doubleBondIds.Count - 1);
            matchedAtomIds.RemoveAt(matchedAtomIds.Count - 1);
            matchedAtomIds.RemoveAt(matchedAtomIds.Count - 1);
        }

        return SearchDoubleBondMatching(molecule, eligibleBondIds, bondIndex + 1, requiredAtomCount, matchedAtomIds, doubleBondIds);
    }

    internal List<int> GetOneElectronAtomIds(AromaticRingCandidate candidate)
    {
        List<int> atomIds = new List<int>();
        int index = 0;
        while (index < candidate.AtomCandidates.Count)
        {
            if (candidate.AtomCandidates[index].ElectronContribution == 1)
            {
                atomIds.Add(candidate.AtomCandidates[index].AtomId);
            }

            index = index + 1;
        }

        return atomIds;
    }

    internal List<int> GetEligibleDoubleBondIds(Molecule molecule, Ring ring, List<int> oneElectronAtomIds)
    {
        List<int> bondIds = new List<int>();
        int index = 0;
        while (index < ring.BondIds.Count)
        {
            Bond bond = molecule.Bonds[ring.BondIds[index]];
            if (ContainsInt(oneElectronAtomIds, bond.BeginAtomId) && ContainsInt(oneElectronAtomIds, bond.EndAtomId))
            {
                bondIds.Add(bond.Id);
            }

            index = index + 1;
        }

        return bondIds;
    }

    internal int GetFallbackDoubleBondCount(AromaticRingCandidate candidate)
    {
        int oneElectronCount = 0;
        int index = 0;
        while (index < candidate.AtomCandidates.Count)
        {
            if (candidate.AtomCandidates[index].ElectronContribution == 1)
            {
                oneElectronCount = oneElectronCount + 1;
            }

            index = index + 1;
        }

        if (oneElectronCount > 0)
        {
            return oneElectronCount / 2;
        }

        return candidate.Size / 2;
    }

    internal void ApplyPattern(Molecule molecule, Ring ring, List<int> doubleBondIds, KekulizationResult result)
    {
        int index = 0;
        while (index < ring.BondIds.Count)
        {
            int bondId = ring.BondIds[index];
            Bond bond = molecule.Bonds[bondId];
            bond.IsAromatic = false;

            if (ContainsInt(doubleBondIds, bondId))
            {
                bond.Order = BondOrder.Double;
            }
            else
            {
                bond.Order = BondOrder.Single;
            }

            AddUniqueInt(result.KekulizedBondIds, bondId);
            index = index + 1;
        }

        ClearRingAtomAromaticFlags(molecule, ring);
    }

    internal void ApplyFallbackPattern(Molecule molecule, Ring ring, int doubleBondCount, KekulizationResult result)
    {
        List<int> sortedBondIds = new List<int>();
        CopyInts(ring.BondIds, sortedBondIds);
        SortInts(sortedBondIds);

        List<int> doubleBondIds = new List<int>();
        int index = 0;
        while (index < sortedBondIds.Count && doubleBondIds.Count < doubleBondCount)
        {
            doubleBondIds.Add(sortedBondIds[index]);
            index = index + 2;
        }

        index = 1;
        while (index < sortedBondIds.Count && doubleBondIds.Count < doubleBondCount)
        {
            doubleBondIds.Add(sortedBondIds[index]);
            index = index + 2;
        }

        ApplyPattern(molecule, ring, doubleBondIds, result);
    }

    internal bool RingHasAromaticBond(Molecule molecule, Ring ring)
    {
        int index = 0;
        while (index < ring.BondIds.Count)
        {
            Bond bond = molecule.Bonds[ring.BondIds[index]];
            if (bond.IsAromatic || bond.Order == BondOrder.Aromatic)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ClearRingAtomAromaticFlags(Molecule molecule, Ring ring)
    {
        int index = 0;
        while (index < ring.AtomIds.Count)
        {
            molecule.Atoms[ring.AtomIds[index]].IsAromatic = false;
            index = index + 1;
        }
    }

    internal void ClearAromaticAtomsWithoutAromaticBonds(Molecule molecule)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            bool hasAromaticBond = false;
            int bondIndex = 0;
            while (bondIndex < molecule.Bonds.Count)
            {
                Bond bond = molecule.Bonds[bondIndex];
                if (bond.BeginAtomId == atomIndex || bond.EndAtomId == atomIndex)
                {
                    if (bond.IsAromatic || bond.Order == BondOrder.Aromatic)
                    {
                        hasAromaticBond = true;
                    }
                }

                bondIndex = bondIndex + 1;
            }

            if (!hasAromaticBond)
            {
                molecule.Atoms[atomIndex].IsAromatic = false;
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void AddFallbackWarning(MoleculeReport report, Ring ring, string message)
    {
        int bondId = -1;
        if (ring.BondIds.Count > 0)
        {
            bondId = ring.BondIds[0];
            int index = 1;
            while (index < ring.BondIds.Count)
            {
                if (ring.BondIds[index] < bondId)
                {
                    bondId = ring.BondIds[index];
                }

                index = index + 1;
            }
        }

        MoleculeError warning = new MoleculeError("KEKULIZATION_FALLBACK", message, DiagnosticLocation.CreateEmpty(), -1, bondId, ErrorSeverity.Warning);
        report.Add(warning);
    }

    internal bool ContainsInt(List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddUniqueInt(List<int> values, int value)
    {
        if (!ContainsInt(values, value))
        {
            values.Add(value);
        }
    }

    internal void CopyInts(List<int> source, List<int> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }

    internal void SortInts(List<int> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            int value = values[index];
            int scan = index - 1;
            while (scan >= 0 && values[scan] > value)
            {
                values[scan + 1] = values[scan];
                scan = scan - 1;
            }

            values[scan + 1] = value;
            index = index + 1;
        }
    }
}
