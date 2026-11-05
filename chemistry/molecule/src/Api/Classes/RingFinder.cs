using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Provides ring finder data and operations for molecular graphs.
/// </summary>
public class RingFinder : IRingFinder
{
    /// <summary>
    /// Initializes a deterministic ring finder.
    /// </summary>
    public RingFinder()
    {
    }

    /// <summary>
    /// Finds the rings.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting ring set.</returns>
    public RingSet FindRings(Molecule? molecule)
    {
        RingSet ringSet = new RingSet();

        if (molecule == null)
        {
            return ringSet;
        }

        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (molecule.IsValidAtomId(bond.BeginAtomId) && molecule.IsValidAtomId(bond.EndAtomId) && bond.BeginAtomId != bond.EndAtomId)
            {
                RingPath path = FindShortestPathExcludingBond(molecule, bond.BeginAtomId, bond.EndAtomId, bond.Id);
                if (path.Found)
                {
                    Ring ring = CreateRing(molecule, path, bond.Id);
                    if (!ringSet.HasRingKey(ring.Key))
                    {
                        ringSet.AddRing(ring);
                    }
                }
            }

            bondIndex = bondIndex + 1;
        }

        ringSet.BuildRingSystems();
        molecule.RingCache = ringSet;
        molecule.IsRingCacheValid = true;
        return ringSet;
    }

    /// <summary>
    /// Finds a configurable cycle basis and bounded set of all small rings.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="options">The ring-finding options; <c>null</c> uses the default cycle-basis policy.</param>
    /// <returns>The requested ring sets, membership, systems, limits, and diagnostics.</returns>
    public RingFindingResult FindRingsWithOptions(Molecule? molecule, RingFindingOptions? options)
    {
        RingFindingResult result = new RingFindingResult();
        if (molecule == null)
        {
            result.Success = false;
            result.Report.AddError("RING_NULL_MOLECULE", "Cannot find rings in a null molecule.");
            return result;
        }

        RingFindingOptions safeOptions = NormalizeOptions(options);
        if (!OptionsAreValid(safeOptions))
        {
            result.Success = false;
            result.Report.AddError("RING_INVALID_OPTIONS", "Ring limits require a maximum size of at least three and positive count and search limits.");
            return result;
        }

        if (safeOptions.FindCycleBasis)
        {
            result.CycleBasis = FindDeterministicCycleBasis(molecule);
            CopyRings(result.CycleBasis, result.Rings);
        }

        if (safeOptions.FindAllSmallRings)
        {
            EnumerateAllSmallRings(molecule, safeOptions, result);
            CopyRings(result.AllSmallRings, result.Rings);
            if (result.LimitReached)
            {
                result.Report.AddWarning("RING_SEARCH_LIMIT_REACHED", "Small-ring enumeration stopped at a caller-provided limit.");
            }
        }

        SortRingSet(result.Rings);
        result.Rings.BuildRingSystems();
        return result;
    }

    internal RingFindingOptions NormalizeOptions(RingFindingOptions? options)
    {
        RingFindingOptions safeOptions = new RingFindingOptions();
        if (options != null)
        {
            safeOptions.FindCycleBasis = options.FindCycleBasis;
            safeOptions.FindAllSmallRings = options.FindAllSmallRings;
            safeOptions.MaximumRingSize = options.MaximumRingSize;
            safeOptions.MaximumSmallRingCount = options.MaximumSmallRingCount;
            safeOptions.MaximumSearchStepCount = options.MaximumSearchStepCount;
        }

        return safeOptions;
    }

    internal bool OptionsAreValid(RingFindingOptions options)
    {
        if (!options.FindAllSmallRings)
        {
            return true;
        }

        return options.MaximumRingSize >= 3 && options.MaximumSmallRingCount > 0 && options.MaximumSearchStepCount > 0;
    }

    internal RingSet EnsureRingCache(Molecule? molecule)
    {
        if (molecule == null)
        {
            return new RingSet();
        }

        if (molecule.IsRingCacheValid)
        {
            return molecule.RingCache;
        }

        return FindRings(molecule);
    }

    internal RingSet FindDeterministicCycleBasis(Molecule molecule)
    {
        RingSet ringSet = new RingSet();
        bool[] treeBondFlags = BuildSpanningForest(molecule);
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            if (!treeBondFlags[bondIndex] && molecule.IsValidAtomId(bond.BeginAtomId) && molecule.IsValidAtomId(bond.EndAtomId) && bond.BeginAtomId != bond.EndAtomId)
            {
                RingPath path = FindPathUsingTreeBonds(molecule, bond.BeginAtomId, bond.EndAtomId, treeBondFlags);
                if (path.Found && path.AtomIds.Count >= 3)
                {
                    Ring ring = CreateRing(molecule, path, bond.Id);
                    if (!ringSet.HasRingKey(ring.Key))
                    {
                        ringSet.AddRing(ring);
                    }
                }
            }

            bondIndex = bondIndex + 1;
        }

        SortRingSet(ringSet);
        ringSet.BuildRingSystems();
        return ringSet;
    }

    internal bool[] BuildSpanningForest(Molecule molecule)
    {
        bool[] treeBondFlags = new bool[molecule.Bonds.Count];
        bool[] visitedAtoms = new bool[molecule.Atoms.Count];
        int startAtomId = 0;
        while (startAtomId < molecule.Atoms.Count)
        {
            if (!visitedAtoms[startAtomId])
            {
                List<int> queue = new List<int>();
                visitedAtoms[startAtomId] = true;
                queue.Add(startAtomId);
                int queueIndex = 0;
                while (queueIndex < queue.Count)
                {
                    int atomId = queue[queueIndex];
                    int bondIndex = 0;
                    while (bondIndex < molecule.Bonds.Count)
                    {
                        Bond bond = molecule.Bonds[bondIndex];
                        int neighborAtomId = GetOtherAtomId(bond, atomId);
                        if (neighborAtomId >= 0 && molecule.IsValidAtomId(neighborAtomId) && neighborAtomId != atomId && !visitedAtoms[neighborAtomId])
                        {
                            visitedAtoms[neighborAtomId] = true;
                            treeBondFlags[bondIndex] = true;
                            queue.Add(neighborAtomId);
                        }

                        bondIndex = bondIndex + 1;
                    }

                    queueIndex = queueIndex + 1;
                }
            }

            startAtomId = startAtomId + 1;
        }

        return treeBondFlags;
    }

    internal RingPath FindPathUsingTreeBonds(Molecule molecule, int startAtomId, int endAtomId, bool[] treeBondFlags)
    {
        RingPath path = new RingPath();
        bool[] visited = new bool[molecule.Atoms.Count];
        int[] previousAtomIds = new int[molecule.Atoms.Count];
        int[] previousBondIds = new int[molecule.Atoms.Count];
        List<int> queue = new List<int>();
        int index = 0;
        while (index < molecule.Atoms.Count)
        {
            previousAtomIds[index] = -1;
            previousBondIds[index] = -1;
            index = index + 1;
        }

        visited[startAtomId] = true;
        queue.Add(startAtomId);
        int queueIndex = 0;
        while (queueIndex < queue.Count && !visited[endAtomId])
        {
            int atomId = queue[queueIndex];
            int bondIndex = 0;
            while (bondIndex < molecule.Bonds.Count)
            {
                if (treeBondFlags[bondIndex])
                {
                    Bond bond = molecule.Bonds[bondIndex];
                    int neighborAtomId = GetOtherAtomId(bond, atomId);
                    if (neighborAtomId >= 0 && molecule.IsValidAtomId(neighborAtomId) && !visited[neighborAtomId])
                    {
                        visited[neighborAtomId] = true;
                        previousAtomIds[neighborAtomId] = atomId;
                        previousBondIds[neighborAtomId] = bond.Id;
                        queue.Add(neighborAtomId);
                    }
                }

                bondIndex = bondIndex + 1;
            }

            queueIndex = queueIndex + 1;
        }

        FillPath(path, visited, previousAtomIds, previousBondIds, startAtomId, endAtomId);
        return path;
    }

    /// <summary>
    /// Atom is in ring.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="atomId">The zero-based atom identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool AtomIsInRing(Molecule? molecule, int atomId)
    {
        return EnsureRingCache(molecule).AtomIsInRing(atomId);
    }

    /// <summary>
    /// Bond is in ring.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <param name="bondId">The zero-based bond identifier.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool BondIsInRing(Molecule? molecule, int bondId)
    {
        return EnsureRingCache(molecule).BondIsInRing(bondId);
    }

    internal void EnumerateAllSmallRings(Molecule molecule, RingFindingOptions options, RingFindingResult result)
    {
        int startAtomId = 0;
        while (startAtomId < molecule.Atoms.Count && !result.LimitReached)
        {
            bool[] visited = new bool[molecule.Atoms.Count];
            List<int> atomPath = new List<int>();
            List<int> bondPath = new List<int>();
            visited[startAtomId] = true;
            atomPath.Add(startAtomId);
            EnumerateFromAtom(molecule, options, result, startAtomId, startAtomId, visited, atomPath, bondPath);
            startAtomId = startAtomId + 1;
        }

        SortRingSet(result.AllSmallRings);
        result.AllSmallRings.BuildRingSystems();
    }

    internal void EnumerateFromAtom(Molecule molecule, RingFindingOptions options, RingFindingResult result, int startAtomId, int currentAtomId, bool[] visited, List<int> atomPath, List<int> bondPath)
    {
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count && !result.LimitReached)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int neighborAtomId = GetOtherAtomId(bond, currentAtomId);
            if (neighborAtomId >= 0 && molecule.IsValidAtomId(neighborAtomId) && neighborAtomId != currentAtomId)
            {
                if (result.SearchStepCount >= options.MaximumSearchStepCount)
                {
                    result.LimitReached = true;
                }
                else
                {
                    result.SearchStepCount = result.SearchStepCount + 1;
                    if (neighborAtomId == startAtomId && atomPath.Count >= 3)
                    {
                        AddEnumeratedRing(molecule, options, result, atomPath, bondPath, bond.Id);
                    }
                    else if (neighborAtomId > startAtomId && !visited[neighborAtomId] && atomPath.Count < options.MaximumRingSize)
                    {
                        visited[neighborAtomId] = true;
                        atomPath.Add(neighborAtomId);
                        bondPath.Add(bond.Id);
                        EnumerateFromAtom(molecule, options, result, startAtomId, neighborAtomId, visited, atomPath, bondPath);
                        bondPath.RemoveAt(bondPath.Count - 1);
                        atomPath.RemoveAt(atomPath.Count - 1);
                        visited[neighborAtomId] = false;
                    }
                }
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal void AddEnumeratedRing(Molecule molecule, RingFindingOptions options, RingFindingResult result, List<int> atomPath, List<int> bondPath, int closingBondId)
    {
        if (atomPath.Count > options.MaximumRingSize)
        {
            return;
        }

        Ring ring = CreateRingFromMembers(molecule, atomPath, bondPath, closingBondId);
        if (result.AllSmallRings.HasRingKey(ring.Key))
        {
            return;
        }

        if (result.AllSmallRings.Rings.Count >= options.MaximumSmallRingCount)
        {
            result.LimitReached = true;
            return;
        }

        result.AllSmallRings.AddRing(ring);
    }

    internal Ring CreateRingFromMembers(Molecule molecule, List<int> atomIds, List<int> bondIds, int closingBondId)
    {
        RingPath path = new RingPath();
        int index = 0;
        while (index < atomIds.Count)
        {
            path.AtomIds.Add(atomIds[index]);
            index = index + 1;
        }

        index = 0;
        while (index < bondIds.Count)
        {
            path.BondIds.Add(bondIds[index]);
            index = index + 1;
        }

        path.Found = true;
        return CreateRing(molecule, path, closingBondId);
    }

    internal RingPath FindShortestPathExcludingBond(Molecule molecule, int startAtomId, int endAtomId, int excludedBondId)
    {
        RingPath path = new RingPath();
        bool[] visited = new bool[molecule.Atoms.Count];
        int[] previousAtomIds = new int[molecule.Atoms.Count];
        int[] previousBondIds = new int[molecule.Atoms.Count];
        List<int> queue = new List<int>();
        int index = 0;

        while (index < molecule.Atoms.Count)
        {
            previousAtomIds[index] = -1;
            previousBondIds[index] = -1;
            index = index + 1;
        }

        visited[startAtomId] = true;
        queue.Add(startAtomId);

        int queueIndex = 0;
        while (queueIndex < queue.Count && !visited[endAtomId])
        {
            int atomId = queue[queueIndex];
            int bondIndex = 0;
            while (bondIndex < molecule.Bonds.Count)
            {
                Bond bond = molecule.Bonds[bondIndex];
                if (bond.Id != excludedBondId)
                {
                    int neighborAtomId = -1;
                    if (bond.BeginAtomId == atomId)
                    {
                        neighborAtomId = bond.EndAtomId;
                    }

                    if (bond.EndAtomId == atomId)
                    {
                        neighborAtomId = bond.BeginAtomId;
                    }

                    if (neighborAtomId >= 0 && molecule.IsValidAtomId(neighborAtomId) && !visited[neighborAtomId])
                    {
                        visited[neighborAtomId] = true;
                        previousAtomIds[neighborAtomId] = atomId;
                        previousBondIds[neighborAtomId] = bond.Id;
                        queue.Add(neighborAtomId);
                    }
                }

                bondIndex = bondIndex + 1;
            }

            queueIndex = queueIndex + 1;
        }

        if (!visited[endAtomId])
        {
            return path;
        }

        path.Found = true;
        int currentAtomId = endAtomId;
        while (currentAtomId != -1)
        {
            path.AtomIds.Add(currentAtomId);
            if (previousBondIds[currentAtomId] >= 0)
            {
                path.BondIds.Add(previousBondIds[currentAtomId]);
            }

            if (currentAtomId == startAtomId)
            {
                currentAtomId = -1;
            }
            else
            {
                currentAtomId = previousAtomIds[currentAtomId];
            }
        }

        ReverseInts(path.AtomIds);
        ReverseInts(path.BondIds);
        return path;
    }

    internal Ring CreateRing(Molecule molecule, RingPath path, int closingBondId)
    {
        Ring ring = new Ring();
        int atomIndex = 0;
        while (atomIndex < path.AtomIds.Count)
        {
            ring.AtomIds.Add(path.AtomIds[atomIndex]);
            atomIndex = atomIndex + 1;
        }

        int bondIndex = 0;
        while (bondIndex < path.BondIds.Count)
        {
            ring.BondIds.Add(path.BondIds[bondIndex]);
            bondIndex = bondIndex + 1;
        }

        ring.BondIds.Add(closingBondId);
        ring.Size = ring.AtomIds.Count;
        ring.Key = CreateBondKey(ring.BondIds);
        ring.IsAromatic = AllBondsAromatic(molecule, ring.BondIds);
        return ring;
    }

    internal void CopyRings(RingSet source, RingSet target)
    {
        int index = 0;
        while (index < source.Rings.Count)
        {
            Ring ring = CloneRing(source.Rings[index]);
            if (!target.HasRingKey(ring.Key))
            {
                target.AddRing(ring);
            }

            index = index + 1;
        }
    }

    internal Ring CloneRing(Ring source)
    {
        Ring ring = new Ring();
        ring.Size = source.Size;
        ring.IsAromatic = source.IsAromatic;
        ring.Key = source.Key;
        int index = 0;
        while (index < source.AtomIds.Count)
        {
            ring.AtomIds.Add(source.AtomIds[index]);
            index = index + 1;
        }

        index = 0;
        while (index < source.BondIds.Count)
        {
            ring.BondIds.Add(source.BondIds[index]);
            index = index + 1;
        }

        return ring;
    }

    internal void SortRingSet(RingSet ringSet)
    {
        List<Ring> sorted = new List<Ring>();
        int index = 0;
        while (index < ringSet.Rings.Count)
        {
            sorted.Add(CloneRing(ringSet.Rings[index]));
            index = index + 1;
        }

        index = 1;
        while (index < sorted.Count)
        {
            Ring value = sorted[index];
            int scan = index - 1;
            while (scan >= 0 && CompareRings(sorted[scan], value) > 0)
            {
                sorted[scan + 1] = sorted[scan];
                scan = scan - 1;
            }

            sorted[scan + 1] = value;
            index = index + 1;
        }

        ringSet.Rings.Clear();
        ringSet.RingAtomIds.Clear();
        ringSet.RingBondIds.Clear();
        ringSet.RingSystems.Clear();
        index = 0;
        while (index < sorted.Count)
        {
            ringSet.AddRing(sorted[index]);
            index = index + 1;
        }

        SortInts(ringSet.RingAtomIds);
        SortInts(ringSet.RingBondIds);
    }

    internal int CompareRings(Ring left, Ring right)
    {
        if (left.Size != right.Size)
        {
            return left.Size - right.Size;
        }

        return string.CompareOrdinal(left.Key, right.Key);
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

    internal void FillPath(RingPath path, bool[] visited, int[] previousAtomIds, int[] previousBondIds, int startAtomId, int endAtomId)
    {
        if (!visited[endAtomId])
        {
            return;
        }

        path.Found = true;
        int currentAtomId = endAtomId;
        while (currentAtomId != -1)
        {
            path.AtomIds.Add(currentAtomId);
            if (previousBondIds[currentAtomId] >= 0)
            {
                path.BondIds.Add(previousBondIds[currentAtomId]);
            }

            if (currentAtomId == startAtomId)
            {
                currentAtomId = -1;
            }
            else
            {
                currentAtomId = previousAtomIds[currentAtomId];
            }
        }

        ReverseInts(path.AtomIds);
        ReverseInts(path.BondIds);
    }

    internal bool AllBondsAromatic(Molecule molecule, List<int> bondIds)
    {
        if (bondIds.Count == 0)
        {
            return false;
        }

        int index = 0;
        while (index < bondIds.Count)
        {
            Bond bond = molecule.Bonds[bondIds[index]];
            if (!bond.IsAromatic && bond.Order != BondOrder.Aromatic)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal string CreateBondKey(List<int> bondIds)
    {
        List<int> sorted = new List<int>();
        int index = 0;
        while (index < bondIds.Count)
        {
            sorted.Add(bondIds[index]);
            index = index + 1;
        }

        SortInts(sorted);

        StringBuilder builder = new StringBuilder();
        index = 0;
        while (index < sorted.Count)
        {
            if (index > 0)
            {
                builder.Append(".");
            }

            builder.Append(NumberFormatting.FormatInt(sorted[index]));
            index = index + 1;
        }

        return builder.ToString();
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

    internal void ReverseInts(List<int> values)
    {
        int left = 0;
        int right = values.Count - 1;
        while (left < right)
        {
            int value = values[left];
            values[left] = values[right];
            values[right] = value;
            left = left + 1;
            right = right - 1;
        }
    }
}
