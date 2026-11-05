using System.Collections.Generic;
using System.Text;

namespace molecule;

internal class PathFingerprint
{
    internal BitFingerprint Create(Molecule? molecule, FingerprintOptions? options)
    {
        FingerprintOptions safeOptions = NormalizeOptions(options);
        BitFingerprint fingerprint = new BitFingerprint(safeOptions.BitCount);
        if (molecule == null)
        {
            return fingerprint;
        }

        if (safeOptions.MaximumPathLength < safeOptions.MinimumPathLength)
        {
            return fingerprint;
        }

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            bool[] visited = new bool[molecule.Atoms.Count];
            List<int> atomIds = new List<int>();
            List<int> bondIds = new List<int>();
            atomIds.Add(atomIndex);
            visited[atomIndex] = true;
            ExplorePaths(molecule, safeOptions, fingerprint, atomIndex, visited, atomIds, bondIds);
            atomIndex = atomIndex + 1;
        }

        return fingerprint;
    }

    internal CountFingerprint CreateCount(Molecule? molecule, FingerprintOptions? options)
    {
        FingerprintOptions safeOptions = NormalizeOptions(options);
        CountFingerprint fingerprint = new CountFingerprint(safeOptions.BitCount);
        if (molecule == null)
        {
            return fingerprint;
        }

        if (safeOptions.MaximumPathLength < safeOptions.MinimumPathLength)
        {
            return fingerprint;
        }

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            bool[] visited = new bool[molecule.Atoms.Count];
            List<int> atomIds = new List<int>();
            List<int> bondIds = new List<int>();
            atomIds.Add(atomIndex);
            visited[atomIndex] = true;
            ExploreCountPaths(molecule, safeOptions, fingerprint, atomIndex, visited, atomIds, bondIds);
            atomIndex = atomIndex + 1;
        }

        return fingerprint;
    }

    internal void ExplorePaths(Molecule molecule, FingerprintOptions options, BitFingerprint fingerprint, int currentAtomId, bool[] visited, List<int> atomIds, List<int> bondIds)
    {
        if (bondIds.Count >= options.MinimumPathLength)
        {
            string feature = BuildCanonicalPathFeature(molecule, options, atomIds, bondIds);
            SetFeatureBit(fingerprint, feature);
        }

        if (bondIds.Count >= options.MaximumPathLength)
        {
            return;
        }

        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int nextAtomId = GetOtherAtomId(bond, currentAtomId);
            if (nextAtomId >= 0 && !visited[nextAtomId])
            {
                visited[nextAtomId] = true;
                atomIds.Add(nextAtomId);
                bondIds.Add(bond.Id);
                ExplorePaths(molecule, options, fingerprint, nextAtomId, visited, atomIds, bondIds);
                bondIds.RemoveAt(bondIds.Count - 1);
                atomIds.RemoveAt(atomIds.Count - 1);
                visited[nextAtomId] = false;
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal void ExploreCountPaths(Molecule molecule, FingerprintOptions options, CountFingerprint fingerprint, int currentAtomId, bool[] visited, List<int> atomIds, List<int> bondIds)
    {
        if (bondIds.Count >= options.MinimumPathLength)
        {
            string feature = BuildCanonicalPathFeature(molecule, options, atomIds, bondIds);
            IncrementFeatureCount(fingerprint, feature);
        }

        if (bondIds.Count >= options.MaximumPathLength)
        {
            return;
        }

        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int nextAtomId = GetOtherAtomId(bond, currentAtomId);
            if (nextAtomId >= 0 && !visited[nextAtomId])
            {
                visited[nextAtomId] = true;
                atomIds.Add(nextAtomId);
                bondIds.Add(bond.Id);
                ExploreCountPaths(molecule, options, fingerprint, nextAtomId, visited, atomIds, bondIds);
                bondIds.RemoveAt(bondIds.Count - 1);
                atomIds.RemoveAt(atomIds.Count - 1);
                visited[nextAtomId] = false;
            }

            bondIndex = bondIndex + 1;
        }
    }

    internal string BuildCanonicalPathFeature(Molecule molecule, FingerprintOptions options, List<int> atomIds, List<int> bondIds)
    {
        string forward = BuildPathFeature(molecule, options, atomIds, bondIds, false);
        string reverse = BuildPathFeature(molecule, options, atomIds, bondIds, true);
        if (string.CompareOrdinal(forward, reverse) <= 0)
        {
            return forward;
        }

        return reverse;
    }

    internal string BuildPathFeature(Molecule molecule, FingerprintOptions options, List<int> atomIds, List<int> bondIds, bool reverse)
    {
        StringBuilder builder = new StringBuilder();
        int atomCount = atomIds.Count;
        int index = 0;
        while (index < atomCount)
        {
            int atomListIndex = index;
            if (reverse)
            {
                atomListIndex = atomCount - 1 - index;
            }

            if (index > 0)
            {
                int bondListIndex = index - 1;
                if (reverse)
                {
                    bondListIndex = bondIds.Count - index;
                }

                builder.Append(BondToken(molecule.Bonds[bondIds[bondListIndex]], options));
            }

            builder.Append(AtomToken(molecule.Atoms[atomIds[atomListIndex]], options));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal string AtomToken(Atom atom, FingerprintOptions options)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("A");
        builder.Append(atom.AtomicNumber);
        if (options.IncludeFormalCharge)
        {
            builder.Append("c");
            builder.Append(atom.FormalCharge);
        }

        if (options.IncludeAromaticity && atom.IsAromatic)
        {
            builder.Append("a");
        }

        return builder.ToString();
    }

    internal string BondToken(Bond bond, FingerprintOptions options)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("-");
        if (options.IncludeBondOrder)
        {
            builder.Append((int)bond.Order);
        }
        else
        {
            builder.Append("b");
        }

        if (options.IncludeAromaticity && (bond.IsAromatic || bond.Order == BondOrder.Aromatic))
        {
            builder.Append("a");
        }

        builder.Append("-");
        return builder.ToString();
    }

    internal void SetFeatureBit(BitFingerprint fingerprint, string feature)
    {
        int index = GetFeatureIndex(feature, fingerprint.Size);
        fingerprint.SetBit(index);
    }

    internal void IncrementFeatureCount(CountFingerprint fingerprint, string feature)
    {
        int index = GetFeatureIndex(feature, fingerprint.Size);
        fingerprint.Increment(index);
    }

    internal int GetFeatureIndex(string feature, int size)
    {
        if (size < 1)
        {
            return 0;
        }

        long hash = 17;
        int index = 0;
        while (index < feature.Length)
        {
            hash = (hash * 31 + feature[index]) % 2147483647;
            index = index + 1;
        }

        return (int)(hash % size);
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

    internal FingerprintOptions NormalizeOptions(FingerprintOptions? options)
    {
        if (options == null)
        {
            return new FingerprintOptions();
        }

        if (options.BitCount < 1)
        {
            options.BitCount = 1;
        }

        if (options.MinimumPathLength < 0)
        {
            options.MinimumPathLength = 0;
        }

        if (options.MaximumPathLength < 0)
        {
            options.MaximumPathLength = 0;
        }

        return options;
    }
}
