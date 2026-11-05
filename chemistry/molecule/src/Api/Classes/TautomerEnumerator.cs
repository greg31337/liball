using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides tautomer enumerator data and operations for molecular graphs.
/// </summary>
public class TautomerEnumerator : ITautomerEnumerator
{
    internal TautomerRules Rules;
    internal DeterministicSerializer Serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="TautomerEnumerator"/> class.
    /// </summary>
    public TautomerEnumerator()
    {
        Rules = new TautomerRules();
        Serializer = new DeterministicSerializer();
    }

    /// <summary>
    /// Generates unique tautomers up to the configured candidate and transform limits.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<Molecule> Enumerate(Molecule? molecule, TautomerEnumerationOptions? options)
    {
        TautomerEnumerationResult result = EnumerateWithResult(molecule, options);
        return result.Tautomers;
    }

    /// <summary>
    /// Enumerates the molecule and returns detailed changes and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The options controlling the operation.</param>
    /// <returns>An object containing the outcome and any diagnostics produced by the operation.</returns>
    public TautomerEnumerationResult EnumerateWithResult(Molecule? molecule, TautomerEnumerationOptions? options)
    {
        TautomerEnumerationResult result = new TautomerEnumerationResult();
        if (molecule == null)
        {
            result.Success = false;
            result.Report.AddError("TAUTOMER_ENUMERATOR_NULL_MOLECULE", "Cannot enumerate tautomers for a null molecule.");
            return result;
        }

        TautomerEnumerationOptions safeOptions = NormalizeOptions(options);
        Molecule start = MoleculeBuilder.CloneMolecule(molecule);
        List<Molecule> queue = new List<Molecule>();
        List<string> seenKeys = new List<string>();

        if (safeOptions.IncludeInputMolecule && safeOptions.MaximumTautomerCount > 0)
        {
            AddUniqueTautomer(result, queue, seenKeys, start);
        }

        int queueIndex = 0;
        while (queueIndex < queue.Count)
        {
            if (result.TransformCount >= safeOptions.MaximumTransformCount)
            {
                result.TransformLimitReached = true;
                break;
            }

            Molecule current = queue[queueIndex];
            List<TautomerCandidate> candidates = GenerateOneStepCandidates(current);
            int candidateIndex = 0;
            while (candidateIndex < candidates.Count)
            {
                if (result.TransformCount >= safeOptions.MaximumTransformCount)
                {
                    result.TransformLimitReached = true;
                    break;
                }

                result.TransformCount = result.TransformCount + 1;
                TautomerCandidate candidate = candidates[candidateIndex];
                if (!HasSeenTautomer(seenKeys, candidate.Key))
                {
                    if (result.Tautomers.Count >= safeOptions.MaximumTautomerCount)
                    {
                        result.TautomerLimitReached = true;
                    }
                    else
                    {
                        AddKnownTautomer(result, queue, seenKeys, candidate);
                    }
                }

                candidateIndex = candidateIndex + 1;
            }

            queueIndex = queueIndex + 1;
        }

        if (!safeOptions.IncludeInputMolecule && result.Tautomers.Count == 0 && safeOptions.MaximumTautomerCount > 0)
        {
            List<TautomerCandidate> candidates = GenerateOneStepCandidates(start);
            int candidateIndex = 0;
            while (candidateIndex < candidates.Count && result.Tautomers.Count < safeOptions.MaximumTautomerCount && result.TransformCount < safeOptions.MaximumTransformCount)
            {
                result.TransformCount = result.TransformCount + 1;
                AddKnownTautomer(result, queue, seenKeys, candidates[candidateIndex]);
                candidateIndex = candidateIndex + 1;
            }

            if (candidateIndex < candidates.Count)
            {
                result.TransformLimitReached = result.TransformCount >= safeOptions.MaximumTransformCount;
                result.TautomerLimitReached = result.Tautomers.Count >= safeOptions.MaximumTautomerCount;
            }
        }

        return result;
    }

    internal List<TautomerCandidate> GenerateOneStepCandidates(Molecule molecule)
    {
        List<TautomerCandidate> candidates = new List<TautomerCandidate>();
        List<TautomerTransformRule> rules = Rules.GetOrderedTransformRules();
        int ruleIndex = 0;
        while (ruleIndex < rules.Count)
        {
            TautomerTransformRule rule = rules[ruleIndex];
            AddRuleCandidates(candidates, molecule, rule, false);
            if (rule.IsReversible)
            {
                AddRuleCandidates(candidates, molecule, rule, true);
            }

            ruleIndex = ruleIndex + 1;
        }

        return candidates;
    }

    internal void AddRuleCandidates(List<TautomerCandidate> candidates, Molecule molecule, TautomerTransformRule rule, bool reverse)
    {
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom donor = molecule.Atoms[atomIndex];
            if (AtomCanStartRule(donor, rule, reverse))
            {
                AddDonorCandidates(candidates, molecule, rule, reverse, donor.Id);
            }

            atomIndex = atomIndex + 1;
        }
    }

    internal void AddDonorCandidates(List<TautomerCandidate> candidates, Molecule molecule, TautomerTransformRule rule, bool reverse, int donorAtomId)
    {
        int donorBondIndex = 0;
        while (donorBondIndex < molecule.Bonds.Count)
        {
            Bond donorBond = molecule.Bonds[donorBondIndex];
            int pivotAtomId = GetOtherAtomId(donorBond, donorAtomId);
            if (pivotAtomId >= 0 && BondMatches(donorBond, GetDonorStartBondOrder(rule, reverse)) && AtomMatches(molecule.Atoms[pivotAtomId], rule.PivotAtomicNumber))
            {
                AddPivotCandidates(candidates, molecule, rule, reverse, donorAtomId, pivotAtomId, donorBond.Id);
            }

            donorBondIndex = donorBondIndex + 1;
        }
    }

    internal void AddPivotCandidates(List<TautomerCandidate> candidates, Molecule molecule, TautomerTransformRule rule, bool reverse, int donorAtomId, int pivotAtomId, int donorBondId)
    {
        int acceptorBondIndex = 0;
        while (acceptorBondIndex < molecule.Bonds.Count)
        {
            Bond acceptorBond = molecule.Bonds[acceptorBondIndex];
            int acceptorAtomId = GetOtherAtomId(acceptorBond, pivotAtomId);
            if (acceptorAtomId >= 0 && acceptorAtomId != donorAtomId)
            {
                if (BondMatches(acceptorBond, GetAcceptorStartBondOrder(rule, reverse)) && AtomMatches(molecule.Atoms[acceptorAtomId], GetAcceptorAtomicNumber(rule, reverse)))
                {
                    TautomerCandidate candidate = ApplyTransform(molecule, rule, reverse, donorAtomId, pivotAtomId, acceptorAtomId, donorBondId, acceptorBond.Id);
                    AddCandidateIfNew(candidates, candidate);
                }
            }

            acceptorBondIndex = acceptorBondIndex + 1;
        }
    }

    internal TautomerCandidate ApplyTransform(Molecule molecule, TautomerTransformRule rule, bool reverse, int donorAtomId, int pivotAtomId, int acceptorAtomId, int donorBondId, int acceptorBondId)
    {
        Molecule tautomer = MoleculeBuilder.CloneMolecule(molecule);
        Atom donor = tautomer.Atoms[donorAtomId];
        Atom acceptor = tautomer.Atoms[acceptorAtomId];
        RemoveOneHydrogen(donor);
        AddOneHydrogen(acceptor);
        tautomer.Bonds[donorBondId].Order = GetDonorProductBondOrder(rule, reverse);
        tautomer.Bonds[donorBondId].IsAromatic = tautomer.Bonds[donorBondId].Order == BondOrder.Aromatic;
        tautomer.Bonds[acceptorBondId].Order = GetAcceptorProductBondOrder(rule, reverse);
        tautomer.Bonds[acceptorBondId].IsAromatic = tautomer.Bonds[acceptorBondId].Order == BondOrder.Aromatic;
        tautomer.ClearCaches();

        TautomerCandidate candidate = new TautomerCandidate();
        candidate.Molecule = tautomer;
        candidate.RuleName = rule.Name;
        candidate.Reverse = reverse;
        candidate.DonorAtomId = donorAtomId;
        candidate.PivotAtomId = pivotAtomId;
        candidate.AcceptorAtomId = acceptorAtomId;
        candidate.Key = Serializer.Serialize(tautomer);
        return candidate;
    }

    internal void AddCandidateIfNew(List<TautomerCandidate> candidates, TautomerCandidate candidate)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            if (candidates[index].Key == candidate.Key)
            {
                return;
            }

            index = index + 1;
        }

        candidates.Add(candidate);
    }

    internal void AddUniqueTautomer(TautomerEnumerationResult result, List<Molecule> queue, List<string> seenKeys, Molecule molecule)
    {
        string key = Serializer.Serialize(molecule);
        if (HasSeenTautomer(seenKeys, key))
        {
            return;
        }

        seenKeys.Add(key);
        result.Tautomers.Add(molecule);
        queue.Add(molecule);
    }

    internal void AddKnownTautomer(TautomerEnumerationResult result, List<Molecule> queue, List<string> seenKeys, TautomerCandidate candidate)
    {
        seenKeys.Add(candidate.Key);
        result.Tautomers.Add(candidate.Molecule);
        queue.Add(candidate.Molecule);
        result.AppliedRuleNames.Add(candidate.RuleName);
        result.Report.AddInfo("TAUTOMER_TRANSFORM_APPLIED", candidate.RuleName);
    }

    internal bool HasSeenTautomer(List<string> seenKeys, string key)
    {
        int index = 0;
        while (index < seenKeys.Count)
        {
            if (seenKeys[index] == key)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool AtomCanStartRule(Atom atom, TautomerTransformRule rule, bool reverse)
    {
        if (!AtomMatches(atom, GetDonorAtomicNumber(rule, reverse)))
        {
            return false;
        }

        return HasHydrogen(atom);
    }

    internal bool AtomMatches(Atom atom, int atomicNumber)
    {
        return atom.AtomicNumber == atomicNumber;
    }

    internal bool BondMatches(Bond bond, BondOrder order)
    {
        if (bond.Order == order)
        {
            return true;
        }

        if (order == BondOrder.Aromatic && bond.IsAromatic)
        {
            return true;
        }

        return false;
    }

    internal bool HasHydrogen(Atom atom)
    {
        return atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount > 0;
    }

    internal void RemoveOneHydrogen(Atom atom)
    {
        if (atom.ExplicitHydrogenCount > 0)
        {
            atom.ExplicitHydrogenCount = atom.ExplicitHydrogenCount - 1;
            return;
        }

        if (atom.ImplicitHydrogenCount > 0)
        {
            atom.ImplicitHydrogenCount = atom.ImplicitHydrogenCount - 1;
        }
    }

    internal void AddOneHydrogen(Atom atom)
    {
        atom.ImplicitHydrogenCount = atom.ImplicitHydrogenCount + 1;
    }

    internal int GetDonorAtomicNumber(TautomerTransformRule rule, bool reverse)
    {
        if (reverse)
        {
            return rule.AcceptorAtomicNumber;
        }

        return rule.DonorAtomicNumber;
    }

    internal int GetAcceptorAtomicNumber(TautomerTransformRule rule, bool reverse)
    {
        if (reverse)
        {
            return rule.DonorAtomicNumber;
        }

        return rule.AcceptorAtomicNumber;
    }

    internal BondOrder GetDonorStartBondOrder(TautomerTransformRule rule, bool reverse)
    {
        if (reverse)
        {
            return rule.ProductAcceptorBondOrder;
        }

        return rule.DonorBondOrder;
    }

    internal BondOrder GetAcceptorStartBondOrder(TautomerTransformRule rule, bool reverse)
    {
        if (reverse)
        {
            return rule.ProductDonorBondOrder;
        }

        return rule.AcceptorBondOrder;
    }

    internal BondOrder GetDonorProductBondOrder(TautomerTransformRule rule, bool reverse)
    {
        if (reverse)
        {
            return rule.AcceptorBondOrder;
        }

        return rule.ProductDonorBondOrder;
    }

    internal BondOrder GetAcceptorProductBondOrder(TautomerTransformRule rule, bool reverse)
    {
        if (reverse)
        {
            return rule.DonorBondOrder;
        }

        return rule.ProductAcceptorBondOrder;
    }

    internal int GetOtherAtomId(Bond bond, int atomId)
    {
        if (bond.BeginAtomId == atomId)
        {
            return bond.EndAtomId;
        }

        if (bond.EndAtomId == atomId)
        {
            return bond.BeginAtomId;
        }

        return -1;
    }

    internal TautomerEnumerationOptions NormalizeOptions(TautomerEnumerationOptions? options)
    {
        if (options == null)
        {
            return new TautomerEnumerationOptions();
        }

        if (options.MaximumTautomerCount < 0)
        {
            options.MaximumTautomerCount = 0;
        }

        if (options.MaximumTransformCount < 0)
        {
            options.MaximumTransformCount = 0;
        }

        return options;
    }
}

