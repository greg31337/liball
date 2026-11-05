using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides aromaticity data and operations for molecular graphs.
/// </summary>
public class AromaticityModel : IAromaticityModel
{
    internal RingFinder Rings;

    /// <summary>
    /// Initializes a new instance of the <see cref="AromaticityModel"/> class.
    /// </summary>
    public AromaticityModel()
    {
        Rings = new RingFinder();
    }

    /// <summary>
    /// Returns the currently assigned aromaticity state for one atom.
    /// </summary>
    /// <param name="molecule">The molecule to inspect.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns>The assigned aromaticity state.</returns>
    public Aromaticity GetAromaticity(Molecule? molecule, int atomId)
    {
        if (molecule == null || !molecule.IsValidAtomId(atomId))
        {
            return Aromaticity.Unknown;
        }

        if (molecule.Atoms[atomId].IsAromatic)
        {
            return Aromaticity.Aromatic;
        }

        return Aromaticity.NonAromatic;
    }

    /// <summary>
    /// Runs valence, aromaticity, conjugation, hybridization, hydrogen, and stereo perception in dependency order.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public AromaticityPerceptionResult Perceive(Molecule? molecule)
    {
        return PerceiveWithOptions(molecule, null);
    }

    /// <summary>
    /// Perceives the in place.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public AromaticityPerceptionResult PerceiveInPlace(Molecule? molecule)
    {
        return PerceiveInPlaceConfigured(molecule, new AromaticityOptions());
    }

    /// <summary>
    /// Perceives aromaticity on a copy using the selected model and fallback policy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The aromaticity options; <c>null</c> uses safe defaults.</param>
    /// <returns>An object containing assignments, rejected rings, fallback state, and diagnostics.</returns>
    public AromaticityPerceptionResult PerceiveWithOptions(Molecule? molecule, AromaticityOptions? options)
    {
        AromaticityPerceptionResult result = new AromaticityPerceptionResult();
        if (molecule == null)
        {
            result.Report.AddError("NULL_MOLECULE", "Cannot perceive aromaticity for a null molecule.");
            return result;
        }

        Molecule clone = MoleculeBuilder.CloneMolecule(molecule);
        result = PerceiveInPlaceConfigured(clone, NormalizeOptions(options));
        result.Molecule = clone;
        return result;
    }

    /// <summary>
    /// Converts valid Kekulé ring systems to aromatic atoms and bonds on an independent copy.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The aromaticity options; <c>null</c> uses safe defaults.</param>
    /// <returns>An object containing assignments, rejected rings, fallback state, and diagnostics.</returns>
    public AromaticityPerceptionResult Dekekulize(Molecule? molecule, AromaticityOptions? options)
    {
        return PerceiveWithOptions(molecule, options);
    }

    /// <summary>
    /// Converts valid Kekulé ring systems to aromatic atoms and bonds in the supplied molecule.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The aromaticity options; <c>null</c> uses safe defaults.</param>
    /// <returns>An object containing assignments, rejected rings, fallback state, and diagnostics.</returns>
    public AromaticityPerceptionResult DekekulizeInPlace(Molecule? molecule, AromaticityOptions? options)
    {
        return PerceiveInPlaceConfigured(molecule, NormalizeOptions(options));
    }

    internal AromaticityPerceptionResult PerceiveInPlaceConfigured(Molecule? molecule, AromaticityOptions options)
    {
        AromaticityPerceptionResult result = new AromaticityPerceptionResult();
        result.Model = options.Model;
        if (molecule == null)
        {
            result.Report.AddError("NULL_MOLECULE", "Cannot perceive aromaticity for a null molecule.");
            return result;
        }

        result.Molecule = molecule;
        if (!ModelIsSupported(options.Model))
        {
            result.Report.AddError("AROMATICITY_INVALID_MODEL", "The requested aromaticity model is not supported.");
            return result;
        }

        result.Success = true;
        result.Candidates = FindCandidatesForModel(molecule, options.Model);
        RingSet ringSet = Rings.EnsureRingCache(molecule);

        int ringIndex = 0;
        while (ringIndex < ringSet.Rings.Count)
        {
            Ring ring = ringSet.Rings[ringIndex];
            AromaticRingCandidate? candidate = FindRingCandidate(result.Candidates, ring.Id);
            if (candidate != null && candidate.IsCandidate)
            {
                AssignAromaticRing(molecule, ring, result);
            }
            else
            {
                if (RingHasPotentialPiBond(molecule, ring))
                {
                    AddUniqueInt(result.RejectedRingIds, ring.Id);
                    if (options.UseDeterministicFallback)
                    {
                        result.UsedFallback = true;
                        AddRejectedAromaticityWarning(result.Report, ring);
                        if (RingHasAssignedAromaticBond(molecule, ring))
                        {
                            ClearRejectedRingAromaticity(molecule, ring);
                        }
                    }
                    else
                    {
                        result.Success = false;
                        AddRejectedAromaticityError(result.Report, ring);
                    }
                }
            }

            ringIndex = ringIndex + 1;
        }

        molecule.ClearCaches();
        return result;
    }

    internal AromaticityOptions NormalizeOptions(AromaticityOptions? options)
    {
        AromaticityOptions safeOptions = new AromaticityOptions();
        if (options != null)
        {
            safeOptions.Model = options.Model;
            safeOptions.UseDeterministicFallback = options.UseDeterministicFallback;
        }

        return safeOptions;
    }

    internal bool ModelIsSupported(AromaticityModelKind model)
    {
        return model == AromaticityModelKind.DaylightLike || model == AromaticityModelKind.MdlLike;
    }

    internal AromaticityCandidateSet FindCandidates(Molecule? molecule)
    {
        return FindCandidatesForModel(molecule, AromaticityModelKind.DaylightLike);
    }

    internal AromaticityCandidateSet FindCandidatesForModel(Molecule? molecule, AromaticityModelKind model)
    {
        AromaticityCandidateSet candidates = new AromaticityCandidateSet();

        if (molecule == null)
        {
            return candidates;
        }

        RingSet ringSet = Rings.EnsureRingCache(molecule);
        int ringIndex = 0;
        while (ringIndex < ringSet.Rings.Count)
        {
            AromaticRingCandidate ringCandidate = EvaluateRingForModel(molecule, ringSet.Rings[ringIndex], model);
            candidates.AddCandidateRing(ringCandidate);
            ringIndex = ringIndex + 1;
        }

        return candidates;
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

    internal void AssignAromaticRing(Molecule molecule, Ring ring, AromaticityPerceptionResult result)
    {
        ring.IsAromatic = true;
        AddUniqueInt(result.AromaticRingIds, ring.Id);

        int atomIndex = 0;
        while (atomIndex < ring.AtomIds.Count)
        {
            int atomId = ring.AtomIds[atomIndex];
            molecule.Atoms[atomId].IsAromatic = true;
            AddUniqueInt(result.AromaticAtomIds, atomId);
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < ring.BondIds.Count)
        {
            int bondId = ring.BondIds[bondIndex];
            Bond bond = molecule.Bonds[bondId];
            bond.IsAromatic = true;
            bond.Order = BondOrder.Aromatic;
            AddUniqueInt(result.AromaticBondIds, bondId);
            bondIndex = bondIndex + 1;
        }
    }

    internal void ClearRejectedRingAromaticity(Molecule molecule, Ring ring)
    {
        int atomIndex = 0;
        while (atomIndex < ring.AtomIds.Count)
        {
            molecule.Atoms[ring.AtomIds[atomIndex]].IsAromatic = false;
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < ring.BondIds.Count)
        {
            Bond bond = molecule.Bonds[ring.BondIds[bondIndex]];
            bond.IsAromatic = false;
            if (bond.Order == BondOrder.Aromatic)
            {
                bond.Order = BondOrder.Single;
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal bool RingHasAssignedAromaticBond(Molecule molecule, Ring ring)
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

    internal bool RingHasPotentialPiBond(Molecule molecule, Ring ring)
    {
        int index = 0;
        while (index < ring.BondIds.Count)
        {
            Bond bond = molecule.Bonds[ring.BondIds[index]];
            if (bond.IsAromatic || bond.Order == BondOrder.Aromatic || bond.Order == BondOrder.Double || bond.Order == BondOrder.Triple)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddRejectedAromaticityWarning(MoleculeReport report, Ring ring)
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

        MoleculeError warning = new MoleculeError("AROMATICITY_REJECTED", "Conjugated ring does not satisfy the selected aromaticity model; deterministic nonaromatic fallback was used.", DiagnosticLocation.CreateEmpty(), -1, bondId, ErrorSeverity.Warning);
        report.Add(warning);
    }

    internal void AddRejectedAromaticityError(MoleculeReport report, Ring ring)
    {
        int bondId = GetLowestBondId(ring);
        MoleculeError error = new MoleculeError("AROMATICITY_MODEL_REJECTED", "Conjugated ring does not satisfy the selected aromaticity model and fallback is disabled.", DiagnosticLocation.CreateEmpty(), -1, bondId, ErrorSeverity.Error);
        report.Add(error);
    }

    internal int GetLowestBondId(Ring ring)
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

        return bondId;
    }

    internal AromaticRingCandidate EvaluateRing(Molecule? molecule, Ring? ring)
    {
        return EvaluateRingForModel(molecule, ring, AromaticityModelKind.DaylightLike);
    }

    internal AromaticRingCandidate EvaluateRingForModel(Molecule? molecule, Ring? ring, AromaticityModelKind model)
    {
        AromaticRingCandidate result = new AromaticRingCandidate();

        if (molecule == null || ring == null)
        {
            result.RejectionReason = "missing ring";
            return result;
        }

        result.RingId = ring.Id;
        result.Size = ring.Size;
        CopyInts(ring.AtomIds, result.AtomIds);
        CopyInts(ring.BondIds, result.BondIds);

        if (ring.Size < 3)
        {
            result.RejectionReason = "ring too small";
            return result;
        }

        if (model == AromaticityModelKind.MdlLike && ring.Size != 6)
        {
            result.RejectionReason = "MDL-like model requires a six-member ring";
            return result;
        }

        bool hasPiBond = false;
        int atomIndex = 0;
        while (atomIndex < ring.AtomIds.Count)
        {
            int atomId = ring.AtomIds[atomIndex];
            AromaticAtomCandidate atomCandidate = EvaluateAtomForModel(molecule, ring, atomId, model);
            result.AtomCandidates.Add(atomCandidate);
            result.ElectronCount = result.ElectronCount + atomCandidate.ElectronContribution;

            if (!atomCandidate.IsCandidate)
            {
                result.RejectionReason = atomCandidate.Reason;
                return result;
            }

            if (atomCandidate.HasRingDoubleBond || atomCandidate.HasRingAromaticBond)
            {
                hasPiBond = true;
            }

            atomIndex = atomIndex + 1;
        }

        if (!hasPiBond)
        {
            result.RejectionReason = "no ring pi bond";
            return result;
        }

        if (!IsHuckelElectronCount(result.ElectronCount))
        {
            result.RejectionReason = "electron count is not 4n+2";
            return result;
        }

        result.IsCandidate = true;
        result.RejectionReason = "";
        return result;
    }

    internal AromaticAtomCandidate EvaluateAtom(Molecule? molecule, Ring? ring, int atomId)
    {
        AromaticAtomCandidate result = new AromaticAtomCandidate();
        result.AtomId = atomId;

        if (molecule == null || ring == null || !molecule.IsValidAtomId(atomId))
        {
            result.Reason = "invalid atom";
            return result;
        }

        Atom atom = molecule.Atoms[atomId];
        result.AtomicNumber = atom.AtomicNumber;
        int ringDoubleBondCount = CountRingBondsWithOrder(molecule, ring, atomId, BondOrder.Double);
        int ringTripleBondCount = CountRingBondsWithOrder(molecule, ring, atomId, BondOrder.Triple);
        int ringAromaticBondCount = CountRingAromaticBonds(molecule, ring, atomId);
        int ringBondCount = CountRingBonds(molecule, ring, atomId);
        result.HasRingDoubleBond = ringDoubleBondCount > 0;
        result.HasRingAromaticBond = ringAromaticBondCount > 0;

        if (ringTripleBondCount > 0)
        {
            result.Reason = "ring atom has triple bond";
            return result;
        }

        if (ringBondCount < 2)
        {
            result.Reason = "atom is not fully in ring";
            return result;
        }

        if (CanContributeOneElectron(atom.AtomicNumber))
        {
            if (ringDoubleBondCount > 0 || ringAromaticBondCount > 0 || atom.IsAromatic)
            {
                result.IsCandidate = true;
                result.ElectronContribution = 1;
                result.Reason = "pi atom";
                return result;
            }
        }

        if (CanContributeLonePair(atom, ring, ringDoubleBondCount, ringAromaticBondCount))
        {
            result.IsCandidate = true;
            result.ElectronContribution = 2;
            result.Reason = "lone pair atom";
            return result;
        }

        result.Reason = "atom cannot contribute pi electrons";
        return result;
    }

    internal AromaticAtomCandidate EvaluateAtomForModel(Molecule? molecule, Ring? ring, int atomId, AromaticityModelKind model)
    {
        AromaticAtomCandidate result = EvaluateAtom(molecule, ring, atomId);
        if (model == AromaticityModelKind.MdlLike && result.IsCandidate)
        {
            if (result.AtomicNumber != 6 && result.AtomicNumber != 7)
            {
                result.IsCandidate = false;
                result.ElectronContribution = 0;
                result.Reason = "atom is not supported by the MDL-like model";
            }
            else if (result.ElectronContribution != 1)
            {
                result.IsCandidate = false;
                result.ElectronContribution = 0;
                result.Reason = "MDL-like model requires a pi-bond contribution";
            }
        }

        return result;
    }

    internal bool CanContributeOneElectron(int atomicNumber)
    {
        if (atomicNumber == 5)
        {
            return true;
        }

        if (atomicNumber == 6)
        {
            return true;
        }

        if (atomicNumber == 7)
        {
            return true;
        }

        if (atomicNumber == 15)
        {
            return true;
        }

        return false;
    }

    internal bool CanContributeLonePair(Atom atom, Ring ring, int ringDoubleBondCount, int ringAromaticBondCount)
    {
        if (atom.FormalCharge > 0)
        {
            return false;
        }

        if (ringDoubleBondCount > 0)
        {
            return false;
        }

        if (ring.Size != 5)
        {
            return false;
        }

        if (atom.AtomicNumber == 7 || atom.AtomicNumber == 8 || atom.AtomicNumber == 15 || atom.AtomicNumber == 16)
        {
            return true;
        }

        return false;
    }

    internal bool IsHuckelElectronCount(int electronCount)
    {
        if (electronCount < 2)
        {
            return false;
        }

        return (electronCount - 2) % 4 == 0;
    }

    internal int CountRingBonds(Molecule molecule, Ring ring, int atomId)
    {
        int count = 0;
        int index = 0;
        while (index < ring.BondIds.Count)
        {
            Bond bond = molecule.Bonds[ring.BondIds[index]];
            if (bond.BeginAtomId == atomId || bond.EndAtomId == atomId)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountRingBondsWithOrder(Molecule molecule, Ring ring, int atomId, BondOrder order)
    {
        int count = 0;
        int index = 0;
        while (index < ring.BondIds.Count)
        {
            Bond bond = molecule.Bonds[ring.BondIds[index]];
            if ((bond.BeginAtomId == atomId || bond.EndAtomId == atomId) && bond.Order == order)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal int CountRingAromaticBonds(Molecule molecule, Ring ring, int atomId)
    {
        int count = 0;
        int index = 0;
        while (index < ring.BondIds.Count)
        {
            Bond bond = molecule.Bonds[ring.BondIds[index]];
            if ((bond.BeginAtomId == atomId || bond.EndAtomId == atomId) && (bond.IsAromatic || bond.Order == BondOrder.Aromatic))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
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
}
