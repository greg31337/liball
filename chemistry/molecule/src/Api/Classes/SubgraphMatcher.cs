using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Finds exact molecular subgraphs or SMARTS query graphs by deterministic backtracking.
/// </summary>
public class SubgraphMatcher : ISubgraphMatcher
{
    /// <summary>
    /// Initializes a deterministic subgraph matcher.
    /// </summary>
    public SubgraphMatcher()
    {
    }

    /// <summary>
    /// Determines whether the value contains subgraph.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool ContainsSubgraph(Molecule? target, Molecule? query)
    {
        return FindFirst(target, query).Success;
    }

    /// <summary>
    /// Determines whether the value contains query.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool ContainsQuery(Molecule? target, QueryMolecule? query)
    {
        return FindFirstQuery(target, query).Success;
    }

    /// <summary>
    /// Finds the first.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns>The resulting subgraph match.</returns>
    public SubgraphMatch FindFirst(Molecule? target, Molecule? query)
    {
        if (target == null || query == null || !PassesFingerprintScreen(target, query))
        {
            return new SubgraphMatch();
        }

        return FindFirstExact(target, query);
    }

    internal SubgraphMatch FindFirstExact(Molecule? target, Molecule? query)
    {
        SubgraphMatch result = new SubgraphMatch();
        if (target == null || query == null)
        {
            return result;
        }

        int[] mapping = CreateMapping(query.Atoms.Count);
        bool[] usedTargetAtoms = new bool[target.Atoms.Count];
        if (SearchExact(target, query, mapping, usedTargetAtoms, 0))
        {
            result.Success = true;
            result.AtomMapping = CopyMapping(mapping);
        }

        return result;
    }

    /// <summary>
    /// Returns every unique atom mapping that satisfies the target and query graphs.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<SubgraphMatch> FindAll(Molecule? target, Molecule? query)
    {
        List<SubgraphMatch> matches = new List<SubgraphMatch>();
        if (target == null || query == null || !PassesFingerprintScreen(target, query))
        {
            return matches;
        }

        int[] mapping = CreateMapping(query.Atoms.Count);
        bool[] usedTargetAtoms = new bool[target.Atoms.Count];
        SearchExactAll(target, query, mapping, usedTargetAtoms, 0, matches);
        return matches;
    }

    /// <summary>
    /// Finds the first.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns>The resulting subgraph match.</returns>
    public SubgraphMatch FindFirstQuery(Molecule? target, QueryMolecule? query)
    {
        SubgraphMatch result = new SubgraphMatch();
        if (target == null || query == null || !PassesQueryFingerprintScreen(target, query))
        {
            return result;
        }

        int[] mapping = CreateMapping(query.Atoms.Count);
        bool[] usedTargetAtoms = new bool[target.Atoms.Count];
        if (SearchQuery(target, query, mapping, usedTargetAtoms, 0))
        {
            result.Success = true;
            result.AtomMapping = CopyMapping(mapping);
        }

        return result;
    }

    /// <summary>
    /// Returns every unique atom mapping that satisfies the target and query graphs.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<SubgraphMatch> FindAllQuery(Molecule? target, QueryMolecule? query)
    {
        List<SubgraphMatch> matches = new List<SubgraphMatch>();
        if (target == null || query == null || !PassesQueryFingerprintScreen(target, query))
        {
            return matches;
        }

        int[] mapping = CreateMapping(query.Atoms.Count);
        bool[] usedTargetAtoms = new bool[target.Atoms.Count];
        SearchQueryAll(target, query, mapping, usedTargetAtoms, 0, matches);
        return matches;
    }

    internal bool PassesFingerprintScreen(Molecule? target, Molecule? query)
    {
        if (target == null || query == null)
        {
            return false;
        }

        if (query.Atoms.Count > target.Atoms.Count || query.Bonds.Count > target.Bonds.Count)
        {
            return false;
        }

        PathFingerprint generator = new PathFingerprint();
        FingerprintOptions options = CreateExactScreeningOptions();
        BitFingerprint targetFingerprint = generator.Create(target, options);
        options = CreateExactScreeningOptions();
        BitFingerprint queryFingerprint = generator.Create(query, options);
        return ContainsAllQueryBits(targetFingerprint, queryFingerprint);
    }

    internal bool PassesQueryFingerprintScreen(Molecule? target, QueryMolecule? query)
    {
        if (target == null || query == null)
        {
            return false;
        }

        if (query.Atoms.Count > target.Atoms.Count || query.Bonds.Count > target.Bonds.Count)
        {
            return false;
        }

        if (!CanBuildQueryScreeningMolecule(query))
        {
            return true;
        }

        Molecule screeningQuery = BuildQueryScreeningMolecule(query);
        PathFingerprint generator = new PathFingerprint();
        FingerprintOptions options = CreateQueryScreeningOptions();
        BitFingerprint targetFingerprint = generator.Create(target, options);
        options = CreateQueryScreeningOptions();
        BitFingerprint queryFingerprint = generator.Create(screeningQuery, options);
        return ContainsAllQueryBits(targetFingerprint, queryFingerprint);
    }

    internal bool ContainsAllQueryBits(BitFingerprint? targetFingerprint, BitFingerprint? queryFingerprint)
    {
        if (targetFingerprint == null || queryFingerprint == null)
        {
            return false;
        }

        int index = 0;
        while (index < queryFingerprint.Size)
        {
            if (queryFingerprint.GetBit(index) && !targetFingerprint.GetBit(index))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal FingerprintOptions CreateExactScreeningOptions()
    {
        FingerprintOptions options = new FingerprintOptions();
        options.BitCount = 4096;
        options.MinimumPathLength = 0;
        options.MaximumPathLength = 7;
        options.IncludeBondOrder = true;
        options.IncludeAromaticity = true;
        options.IncludeFormalCharge = true;
        return options;
    }

    internal FingerprintOptions CreateQueryScreeningOptions()
    {
        FingerprintOptions options = new FingerprintOptions();
        options.BitCount = 4096;
        options.MinimumPathLength = 0;
        options.MaximumPathLength = 7;
        options.IncludeBondOrder = false;
        options.IncludeAromaticity = false;
        options.IncludeFormalCharge = false;
        return options;
    }

    internal bool CanBuildQueryScreeningMolecule(QueryMolecule? query)
    {
        if (query == null)
        {
            return false;
        }

        int atomIndex = 0;
        while (atomIndex < query.Atoms.Count)
        {
            QueryAtom atom = query.Atoms[atomIndex];
            if (!atom.HasAtomicNumber || atom.UsesAtomicNumberList || atom.NegateAtomicNumber)
            {
                return false;
            }

            atomIndex = atomIndex + 1;
        }

        return true;
    }

    internal Molecule BuildQueryScreeningMolecule(QueryMolecule query)
    {
        MoleculeBuilder builder = new MoleculeBuilder();
        int atomIndex = 0;
        while (atomIndex < query.Atoms.Count)
        {
            QueryAtom atom = query.Atoms[atomIndex];
            builder.AddAtom(new AtomSpec(atom.AtomicNumber, 0, 0, 0, 0, 0, 0, false, "", ""));
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < query.Bonds.Count)
        {
            QueryBond bond = query.Bonds[bondIndex];
            builder.AddBond(bond.BeginQueryAtomId, bond.EndQueryAtomId, new BondSpec());
            bondIndex = bondIndex + 1;
        }

        return builder.Build();
    }

    internal bool SearchExact(Molecule target, Molecule query, int[] mapping, bool[] usedTargetAtoms, int queryAtomId)
    {
        if (queryAtomId >= query.Atoms.Count)
        {
            return true;
        }

        int targetAtomId = 0;
        while (targetAtomId < target.Atoms.Count)
        {
            if (!usedTargetAtoms[targetAtomId] && ExactAtomsMatch(query.Atoms[queryAtomId], target.Atoms[targetAtomId]) && ExactMappedBondsMatch(target, query, mapping, queryAtomId, targetAtomId))
            {
                mapping[queryAtomId] = targetAtomId;
                usedTargetAtoms[targetAtomId] = true;
                if (SearchExact(target, query, mapping, usedTargetAtoms, queryAtomId + 1))
                {
                    return true;
                }

                mapping[queryAtomId] = -1;
                usedTargetAtoms[targetAtomId] = false;
            }

            targetAtomId = targetAtomId + 1;
        }

        return false;
    }

    internal void SearchExactAll(Molecule target, Molecule query, int[] mapping, bool[] usedTargetAtoms, int queryAtomId, List<SubgraphMatch> matches)
    {
        if (queryAtomId >= query.Atoms.Count)
        {
            AddUniqueMatch(matches, mapping);
            return;
        }

        int targetAtomId = 0;
        while (targetAtomId < target.Atoms.Count)
        {
            if (!usedTargetAtoms[targetAtomId] && ExactAtomsMatch(query.Atoms[queryAtomId], target.Atoms[targetAtomId]) && ExactMappedBondsMatch(target, query, mapping, queryAtomId, targetAtomId))
            {
                mapping[queryAtomId] = targetAtomId;
                usedTargetAtoms[targetAtomId] = true;
                SearchExactAll(target, query, mapping, usedTargetAtoms, queryAtomId + 1, matches);
                mapping[queryAtomId] = -1;
                usedTargetAtoms[targetAtomId] = false;
            }

            targetAtomId = targetAtomId + 1;
        }
    }

    internal bool SearchQuery(Molecule target, QueryMolecule query, int[] mapping, bool[] usedTargetAtoms, int queryAtomId)
    {
        if (queryAtomId >= query.Atoms.Count)
        {
            return true;
        }

        int targetAtomId = 0;
        while (targetAtomId < target.Atoms.Count)
        {
            if (!usedTargetAtoms[targetAtomId] && query.Atoms[queryAtomId].MatchesAtomInMolecule(target, targetAtomId) && QueryMappedBondsMatch(target, query, mapping, queryAtomId, targetAtomId) && QueryComponentGroupMatches(target, query, mapping, queryAtomId, targetAtomId))
            {
                mapping[queryAtomId] = targetAtomId;
                usedTargetAtoms[targetAtomId] = true;
                if (SearchQuery(target, query, mapping, usedTargetAtoms, queryAtomId + 1))
                {
                    return true;
                }

                mapping[queryAtomId] = -1;
                usedTargetAtoms[targetAtomId] = false;
            }

            targetAtomId = targetAtomId + 1;
        }

        return false;
    }

    internal void SearchQueryAll(Molecule target, QueryMolecule query, int[] mapping, bool[] usedTargetAtoms, int queryAtomId, List<SubgraphMatch> matches)
    {
        if (queryAtomId >= query.Atoms.Count)
        {
            AddUniqueMatch(matches, mapping);
            return;
        }

        int targetAtomId = 0;
        while (targetAtomId < target.Atoms.Count)
        {
            if (!usedTargetAtoms[targetAtomId] && query.Atoms[queryAtomId].MatchesAtomInMolecule(target, targetAtomId) && QueryMappedBondsMatch(target, query, mapping, queryAtomId, targetAtomId) && QueryComponentGroupMatches(target, query, mapping, queryAtomId, targetAtomId))
            {
                mapping[queryAtomId] = targetAtomId;
                usedTargetAtoms[targetAtomId] = true;
                SearchQueryAll(target, query, mapping, usedTargetAtoms, queryAtomId + 1, matches);
                mapping[queryAtomId] = -1;
                usedTargetAtoms[targetAtomId] = false;
            }

            targetAtomId = targetAtomId + 1;
        }
    }

    internal bool ExactMappedBondsMatch(Molecule target, Molecule query, int[] mapping, int queryAtomId, int targetAtomId)
    {
        int otherQueryAtomId = 0;
        while (otherQueryAtomId < queryAtomId)
        {
            int queryBondId = query.GetBondIdBetween(queryAtomId, otherQueryAtomId);
            if (queryBondId >= 0)
            {
                int otherTargetAtomId = mapping[otherQueryAtomId];
                int targetBondId = target.GetBondIdBetween(targetAtomId, otherTargetAtomId);
                if (targetBondId < 0)
                {
                    return false;
                }

                if (!ExactBondsMatch(query.Bonds[queryBondId], target.Bonds[targetBondId]))
                {
                    return false;
                }
            }

            otherQueryAtomId = otherQueryAtomId + 1;
        }

        return true;
    }

    internal bool QueryMappedBondsMatch(Molecule target, QueryMolecule query, int[] mapping, int queryAtomId, int targetAtomId)
    {
        int otherQueryAtomId = 0;
        while (otherQueryAtomId < queryAtomId)
        {
            int queryBondId = query.GetBondIdBetween(queryAtomId, otherQueryAtomId);
            if (queryBondId >= 0)
            {
                int otherTargetAtomId = mapping[otherQueryAtomId];
                int targetBondId = target.GetBondIdBetween(targetAtomId, otherTargetAtomId);
                if (targetBondId < 0)
                {
                    return false;
                }

                if (!query.Bonds[queryBondId].MatchesBondInMolecule(target, targetBondId))
                {
                    return false;
                }
            }

            otherQueryAtomId = otherQueryAtomId + 1;
        }

        return true;
    }

    internal bool MatchesRecursiveQuery(Molecule? target, QueryMolecule? query, int targetAtomId)
    {
        if (target == null || query == null || query.Atoms.Count == 0 || !target.IsValidAtomId(targetAtomId))
        {
            return false;
        }

        if (!query.Atoms[0].MatchesAtomInMolecule(target, targetAtomId))
        {
            return false;
        }

        int[] mapping = CreateMapping(query.Atoms.Count);
        bool[] usedTargetAtoms = new bool[target.Atoms.Count];
        mapping[0] = targetAtomId;
        usedTargetAtoms[targetAtomId] = true;
        return SearchQuery(target, query, mapping, usedTargetAtoms, 1);
    }

    internal bool QueryComponentGroupMatches(Molecule target, QueryMolecule query, int[] mapping, int queryAtomId, int targetAtomId)
    {
        int groupId = query.GetComponentGroupId(queryAtomId);
        if (groupId < 0)
        {
            return true;
        }

        List<int> targetComponentIds = new Components().GetComponentIds(target);
        int otherQueryAtomId = 0;
        while (otherQueryAtomId < queryAtomId)
        {
            if (query.GetComponentGroupId(otherQueryAtomId) == groupId && mapping[otherQueryAtomId] >= 0)
            {
                if (targetComponentIds[mapping[otherQueryAtomId]] != targetComponentIds[targetAtomId])
                {
                    return false;
                }
            }

            otherQueryAtomId = otherQueryAtomId + 1;
        }

        return true;
    }

    internal bool ExactAtomsMatch(Atom query, Atom target)
    {
        if (query.AtomicNumber != target.AtomicNumber)
        {
            return false;
        }

        if (query.IsotopeMassNumber != target.IsotopeMassNumber || query.FormalCharge != target.FormalCharge)
        {
            return false;
        }

        if (query.ExplicitHydrogenCount != target.ExplicitHydrogenCount || query.ImplicitHydrogenCount != target.ImplicitHydrogenCount)
        {
            return false;
        }

        if (query.RadicalElectronCount != target.RadicalElectronCount || query.AtomMapNumber != target.AtomMapNumber)
        {
            return false;
        }

        if (query.IsAromatic != target.IsAromatic)
        {
            return false;
        }

        return true;
    }

    internal bool ExactBondsMatch(Bond query, Bond target)
    {
        if (query.Order != target.Order || query.IsAromatic != target.IsAromatic)
        {
            return false;
        }

        if (query.Stereo != target.Stereo || query.Direction != target.Direction)
        {
            return false;
        }

        if (query.IsDative != target.IsDative)
        {
            return false;
        }

        return true;
    }

    internal int[] CreateMapping(int count)
    {
        int[] mapping = new int[count];
        int index = 0;
        while (index < mapping.Length)
        {
            mapping[index] = -1;
            index = index + 1;
        }

        return mapping;
    }

    internal int[] CopyMapping(int[] mapping)
    {
        int[] copy = new int[mapping.Length];
        int index = 0;
        while (index < mapping.Length)
        {
            copy[index] = mapping[index];
            index = index + 1;
        }

        return copy;
    }

    internal void AddUniqueMatch(List<SubgraphMatch> matches, int[] mapping)
    {
        if (ContainsEquivalentTargetSet(matches, mapping))
        {
            return;
        }

        SubgraphMatch match = new SubgraphMatch();
        match.Success = true;
        match.AtomMapping = CopyMapping(mapping);
        matches.Add(match);
    }

    internal bool ContainsEquivalentTargetSet(List<SubgraphMatch> matches, int[] mapping)
    {
        int index = 0;
        while (index < matches.Count)
        {
            if (SameTargetSet(matches[index].AtomMapping, mapping))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool SameTargetSet(int[] left, int[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (!ContainsAtomId(right, left[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool ContainsAtomId(int[] mapping, int atomId)
    {
        int index = 0;
        while (index < mapping.Length)
        {
            if (mapping[index] == atomId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
