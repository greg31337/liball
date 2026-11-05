using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Finds deterministic maximum common molecular substructures with finite work and elapsed-time bounds.
/// </summary>
public class MaximumCommonSubstructure : IMaximumCommonSubstructure
{
    /// <summary>
    /// Initializes a bounded maximum-common-substructure service.
    /// </summary>
    public MaximumCommonSubstructure()
    {
    }

    /// <summary>
    /// Finds a deterministic maximum common connected substructure within caller-provided work and time limits.
    /// </summary>
    /// <param name="left">The first molecule.</param>
    /// <param name="right">The second molecule.</param>
    /// <param name="options">The atom, bond, connectivity, and search-limit options; <c>null</c> uses bounded defaults.</param>
    /// <returns>The best mapping found, its common graph, search counts, limits, and diagnostics.</returns>
    public MaximumCommonSubstructureResult Find(Molecule? left, Molecule? right, MaximumCommonSubstructureOptions? options)
    {
        MaximumCommonSubstructureResult result = new MaximumCommonSubstructureResult();
        if (left == null || right == null)
        {
            result.Report.AddError("MCS_NULL_MOLECULE", "Maximum common substructure requires two molecules.");
            return result;
        }

        MaximumCommonSubstructureOptions safeOptions = NormalizeOptions(options);
        if (!OptionsAreValid(safeOptions))
        {
            result.Report.AddError("MCS_OPTIONS_INVALID", "Maximum common substructure limits and minimum atom count must be nonnegative, with a positive work limit.");
            return result;
        }

        result.Success = true;
        if (safeOptions.MinimumAtomCount > left.Atoms.Count || safeOptions.MinimumAtomCount > right.Atoms.Count)
        {
            result.SearchCompleted = true;
            return result;
        }

        MaximumCommonSubstructureSearchState state = new MaximumCommonSubstructureSearchState();
        state.Left = left;
        state.Right = right;
        state.Options = safeOptions;
        state.Result = result;
        state.Mapping = new int[left.Atoms.Count];
        state.BestMapping = new int[left.Atoms.Count];
        state.UsedRightAtoms = new bool[right.Atoms.Count];
        InitializeMapping(state.Mapping);
        InitializeMapping(state.BestMapping);
        state.Timer.Start();
        Search(state, 0);
        state.Timer.Stop();
        result.ElapsedMilliseconds = state.Timer.ElapsedMilliseconds;
        result.SearchCompleted = !result.LimitReached;
        AddTerminationDiagnostic(result);
        if (result.Found)
        {
            CreateMappings(state);
            result.Molecule = BuildCommonMolecule(state);
        }

        return result;
    }

    internal MaximumCommonSubstructureOptions NormalizeOptions(MaximumCommonSubstructureOptions? options)
    {
        MaximumCommonSubstructureOptions normalized = new MaximumCommonSubstructureOptions();
        if (options != null)
        {
            normalized.MatchIsotopes = options.MatchIsotopes;
            normalized.MatchFormalCharges = options.MatchFormalCharges;
            normalized.MatchHydrogenCounts = options.MatchHydrogenCounts;
            normalized.MatchRadicals = options.MatchRadicals;
            normalized.MatchAtomMaps = options.MatchAtomMaps;
            normalized.MatchAromaticity = options.MatchAromaticity;
            normalized.MatchBondOrders = options.MatchBondOrders;
            normalized.MatchStereo = options.MatchStereo;
            normalized.RequireConnected = options.RequireConnected;
            normalized.MinimumAtomCount = options.MinimumAtomCount;
            normalized.MaximumSearchStepCount = options.MaximumSearchStepCount;
            normalized.MaximumElapsedMilliseconds = options.MaximumElapsedMilliseconds;
        }

        return normalized;
    }

    internal bool OptionsAreValid(MaximumCommonSubstructureOptions options)
    {
        return options.MinimumAtomCount >= 0
            && options.MaximumSearchStepCount > 0
            && options.MaximumElapsedMilliseconds >= 0;
    }

    internal void InitializeMapping(int[] mapping)
    {
        int index = 0;
        while (index < mapping.Length)
        {
            mapping[index] = -1;
            index = index + 1;
        }
    }

    internal void Search(MaximumCommonSubstructureSearchState state, int leftAtomId)
    {
        if (!TryBeginSearchStep(state))
        {
            return;
        }

        UpdateBest(state);
        if (leftAtomId >= state.Left.Atoms.Count)
        {
            return;
        }

        if (state.CurrentAtomCount + state.Left.Atoms.Count - leftAtomId < state.Result.AtomCount)
        {
            return;
        }

        int rightAtomId = 0;
        while (rightAtomId < state.Right.Atoms.Count)
        {
            if (!state.UsedRightAtoms[rightAtomId]
                && AtomsMatch(state.Left.Atoms[leftAtomId], state.Right.Atoms[rightAtomId], state.Options))
            {
                state.Mapping[leftAtomId] = rightAtomId;
                state.UsedRightAtoms[rightAtomId] = true;
                state.CurrentAtomCount = state.CurrentAtomCount + 1;
                Search(state, leftAtomId + 1);
                state.CurrentAtomCount = state.CurrentAtomCount - 1;
                state.UsedRightAtoms[rightAtomId] = false;
                state.Mapping[leftAtomId] = -1;
                if (state.Stop)
                {
                    return;
                }
            }

            rightAtomId = rightAtomId + 1;
        }

        Search(state, leftAtomId + 1);
    }

    internal bool TryBeginSearchStep(MaximumCommonSubstructureSearchState state)
    {
        if (state.Stop)
        {
            return false;
        }

        if (state.Timer.ElapsedMilliseconds >= state.Options.MaximumElapsedMilliseconds)
        {
            state.Result.TimedOut = true;
            state.Result.LimitReached = true;
            state.Stop = true;
            return false;
        }

        if (state.Result.SearchStepCount >= state.Options.MaximumSearchStepCount)
        {
            state.Result.WorkLimitReached = true;
            state.Result.LimitReached = true;
            state.Stop = true;
            return false;
        }

        state.Result.SearchStepCount = state.Result.SearchStepCount + 1;
        return true;
    }

    internal void UpdateBest(MaximumCommonSubstructureSearchState state)
    {
        if (state.CurrentAtomCount < state.Options.MinimumAtomCount)
        {
            return;
        }

        if (state.Options.RequireConnected && !MappingIsConnected(state))
        {
            return;
        }

        int bondCount = CountMappedBonds(state);
        if (!CurrentIsBetter(state, bondCount))
        {
            return;
        }

        CopyCurrentMapping(state);
        state.Result.AtomCount = state.CurrentAtomCount;
        state.Result.BondCount = bondCount;
        state.Result.Found = true;
    }

    internal int CountMappedBonds(MaximumCommonSubstructureSearchState state)
    {
        int count = 0;
        int bondIndex = 0;
        while (bondIndex < state.Left.Bonds.Count)
        {
            Bond leftBond = state.Left.Bonds[bondIndex];
            int rightBegin = state.Mapping[leftBond.BeginAtomId];
            int rightEnd = state.Mapping[leftBond.EndAtomId];
            if (rightBegin >= 0 && rightEnd >= 0)
            {
                int rightBondId = state.Right.GetBondIdBetween(rightBegin, rightEnd);
                if (rightBondId >= 0 && BondsMatch(leftBond, state.Right.Bonds[rightBondId], state.Options))
                {
                    count = count + 1;
                }
            }

            bondIndex = bondIndex + 1;
        }

        return count;
    }

    internal bool MappingIsConnected(MaximumCommonSubstructureSearchState state)
    {
        if (state.CurrentAtomCount <= 1)
        {
            return true;
        }

        bool[] visited = new bool[state.Left.Atoms.Count];
        List<int> queue = new List<int>();
        int start = 0;
        while (start < state.Mapping.Length && state.Mapping[start] < 0)
        {
            start = start + 1;
        }

        if (start >= state.Mapping.Length)
        {
            return false;
        }

        visited[start] = true;
        queue.Add(start);
        int visitedCount = 1;
        int queueIndex = 0;
        while (queueIndex < queue.Count)
        {
            int leftAtomId = queue[queueIndex];
            int candidate = 0;
            while (candidate < state.Mapping.Length)
            {
                if (state.Mapping[candidate] >= 0 && !visited[candidate])
                {
                    int leftBondId = state.Left.GetBondIdBetween(leftAtomId, candidate);
                    if (leftBondId >= 0)
                    {
                        int rightBondId = state.Right.GetBondIdBetween(state.Mapping[leftAtomId], state.Mapping[candidate]);
                        if (rightBondId >= 0 && BondsMatch(state.Left.Bonds[leftBondId], state.Right.Bonds[rightBondId], state.Options))
                        {
                            visited[candidate] = true;
                            queue.Add(candidate);
                            visitedCount = visitedCount + 1;
                        }
                    }
                }

                candidate = candidate + 1;
            }

            queueIndex = queueIndex + 1;
        }

        return visitedCount == state.CurrentAtomCount;
    }

    internal bool CurrentIsBetter(MaximumCommonSubstructureSearchState state, int bondCount)
    {
        if (!state.Result.Found || state.CurrentAtomCount > state.Result.AtomCount)
        {
            return true;
        }

        if (state.CurrentAtomCount < state.Result.AtomCount)
        {
            return false;
        }

        if (bondCount > state.Result.BondCount)
        {
            return true;
        }

        if (bondCount < state.Result.BondCount)
        {
            return false;
        }

        return MappingIsLexicographicallyEarlier(state.Mapping, state.BestMapping);
    }

    internal bool MappingIsLexicographicallyEarlier(int[] current, int[] best)
    {
        int index = 0;
        while (index < current.Length)
        {
            bool currentMapped = current[index] >= 0;
            bool bestMapped = best[index] >= 0;
            if (currentMapped && !bestMapped)
            {
                return true;
            }

            if (!currentMapped && bestMapped)
            {
                return false;
            }

            if (currentMapped && bestMapped && current[index] != best[index])
            {
                return current[index] < best[index];
            }

            index = index + 1;
        }

        return false;
    }

    internal void CopyCurrentMapping(MaximumCommonSubstructureSearchState state)
    {
        int index = 0;
        while (index < state.Mapping.Length)
        {
            state.BestMapping[index] = state.Mapping[index];
            index = index + 1;
        }
    }

    internal bool AtomsMatch(Atom left, Atom right, MaximumCommonSubstructureOptions options)
    {
        if (left.AtomicNumber != right.AtomicNumber)
        {
            return false;
        }

        if (options.MatchIsotopes && left.IsotopeMassNumber != right.IsotopeMassNumber)
        {
            return false;
        }

        if (options.MatchFormalCharges && left.FormalCharge != right.FormalCharge)
        {
            return false;
        }

        if (options.MatchHydrogenCounts && (left.ExplicitHydrogenCount != right.ExplicitHydrogenCount || left.ImplicitHydrogenCount != right.ImplicitHydrogenCount))
        {
            return false;
        }

        if (options.MatchRadicals && left.RadicalElectronCount != right.RadicalElectronCount)
        {
            return false;
        }

        if (options.MatchAtomMaps && left.AtomMapNumber != right.AtomMapNumber)
        {
            return false;
        }

        if (options.MatchAromaticity && left.IsAromatic != right.IsAromatic)
        {
            return false;
        }

        return true;
    }

    internal bool BondsMatch(Bond left, Bond right, MaximumCommonSubstructureOptions options)
    {
        if (options.MatchBondOrders && left.Order != right.Order)
        {
            return false;
        }

        if (options.MatchAromaticity && left.IsAromatic != right.IsAromatic)
        {
            return false;
        }

        if (options.MatchStereo && (left.Stereo != right.Stereo || left.Direction != right.Direction))
        {
            return false;
        }

        return true;
    }

    internal void AddTerminationDiagnostic(MaximumCommonSubstructureResult result)
    {
        if (result.TimedOut)
        {
            result.Report.AddWarning("MCS_TIMEOUT", "Maximum common substructure returned the deterministic best mapping found before the elapsed-time limit.");
        }
        else if (result.WorkLimitReached)
        {
            result.Report.AddWarning("MCS_WORK_LIMIT_REACHED", "Maximum common substructure returned the deterministic best mapping found before the search-step limit.");
        }
    }

    internal void CreateMappings(MaximumCommonSubstructureSearchState state)
    {
        int commonAtomId = 0;
        int leftAtomId = 0;
        while (leftAtomId < state.BestMapping.Length)
        {
            if (state.BestMapping[leftAtomId] >= 0)
            {
                CommonAtomMapping atomMapping = new CommonAtomMapping();
                atomMapping.LeftAtomId = leftAtomId;
                atomMapping.RightAtomId = state.BestMapping[leftAtomId];
                atomMapping.CommonAtomId = commonAtomId;
                state.Result.AtomMappings.Add(atomMapping);
                commonAtomId = commonAtomId + 1;
            }

            leftAtomId = leftAtomId + 1;
        }

        int commonBondId = 0;
        int leftBondId = 0;
        while (leftBondId < state.Left.Bonds.Count)
        {
            Bond leftBond = state.Left.Bonds[leftBondId];
            int rightBegin = state.BestMapping[leftBond.BeginAtomId];
            int rightEnd = state.BestMapping[leftBond.EndAtomId];
            if (rightBegin >= 0 && rightEnd >= 0)
            {
                int rightBondId = state.Right.GetBondIdBetween(rightBegin, rightEnd);
                if (rightBondId >= 0 && BondsMatch(leftBond, state.Right.Bonds[rightBondId], state.Options))
                {
                    CommonBondMapping bondMapping = new CommonBondMapping();
                    bondMapping.LeftBondId = leftBondId;
                    bondMapping.RightBondId = rightBondId;
                    bondMapping.CommonBondId = commonBondId;
                    state.Result.BondMappings.Add(bondMapping);
                    commonBondId = commonBondId + 1;
                }
            }

            leftBondId = leftBondId + 1;
        }

        state.Result.AtomCount = state.Result.AtomMappings.Count;
        state.Result.BondCount = state.Result.BondMappings.Count;
    }

    internal Molecule BuildCommonMolecule(MaximumCommonSubstructureSearchState state)
    {
        Molecule common = new Molecule();
        int mappingIndex = 0;
        while (mappingIndex < state.Result.AtomMappings.Count)
        {
            CommonAtomMapping mapping = state.Result.AtomMappings[mappingIndex];
            common.Atoms.Add(MoleculeBuilder.CloneAtom(state.Left.Atoms[mapping.LeftAtomId], mapping.CommonAtomId));
            mappingIndex = mappingIndex + 1;
        }

        mappingIndex = 0;
        while (mappingIndex < state.Result.BondMappings.Count)
        {
            CommonBondMapping mapping = state.Result.BondMappings[mappingIndex];
            Bond leftBond = state.Left.Bonds[mapping.LeftBondId];
            int beginCommonId = FindCommonAtomId(state.Result.AtomMappings, leftBond.BeginAtomId);
            int endCommonId = FindCommonAtomId(state.Result.AtomMappings, leftBond.EndAtomId);
            common.Bonds.Add(MoleculeBuilder.CloneBond(leftBond, mapping.CommonBondId, beginCommonId, endCommonId));
            mappingIndex = mappingIndex + 1;
        }

        common.ClearCaches();
        return common;
    }

    internal int FindCommonAtomId(List<CommonAtomMapping> mappings, int leftAtomId)
    {
        int index = 0;
        while (index < mappings.Count)
        {
            if (mappings[index].LeftAtomId == leftAtomId)
            {
                return mappings[index].CommonAtomId;
            }

            index = index + 1;
        }

        return -1;
    }
}
