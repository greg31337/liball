using System.Collections.Generic;

namespace molecule;

internal class CircularFingerprint
{
    internal BitFingerprint Create(Molecule? molecule, FingerprintOptions? options)
    {
        FingerprintOptions safeOptions = NormalizeOptions(options);
        BitFingerprint fingerprint = new BitFingerprint(safeOptions.BitCount);
        if (molecule == null)
        {
            return fingerprint;
        }

        long[] identifiers = BuildInitialIdentifiers(molecule, safeOptions);
        SetIdentifierBits(fingerprint, identifiers, 0);

        int radius = 1;
        while (radius <= safeOptions.CircularRadius)
        {
            identifiers = BuildNextIdentifiers(molecule, safeOptions, identifiers, radius);
            SetIdentifierBits(fingerprint, identifiers, radius);
            radius = radius + 1;
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

        long[] identifiers = BuildInitialIdentifiers(molecule, safeOptions);
        IncrementIdentifierCounts(fingerprint, identifiers, 0);

        int radius = 1;
        while (radius <= safeOptions.CircularRadius)
        {
            identifiers = BuildNextIdentifiers(molecule, safeOptions, identifiers, radius);
            IncrementIdentifierCounts(fingerprint, identifiers, radius);
            radius = radius + 1;
        }

        return fingerprint;
    }

    internal long[] BuildInitialIdentifiers(Molecule molecule, FingerprintOptions options)
    {
        long[] identifiers = new long[molecule.Atoms.Count];
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[atomIndex];
            long value = 17;
            value = AddHashValue(value, atom.AtomicNumber);
            value = AddHashValue(value, atom.IsotopeMassNumber);
            if (options.IncludeFormalCharge)
            {
                value = AddHashValue(value, atom.FormalCharge + 128);
            }

            value = AddHashValue(value, atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount);
            value = AddHashValue(value, molecule.GetDegree(atomIndex));
            if (options.IncludeAromaticity && atom.IsAromatic)
            {
                value = AddHashValue(value, 1);
            }
            else
            {
                value = AddHashValue(value, 0);
            }

            identifiers[atomIndex] = value;
            atomIndex = atomIndex + 1;
        }

        return identifiers;
    }

    internal long[] BuildNextIdentifiers(Molecule molecule, FingerprintOptions options, long[] previousIdentifiers, int radius)
    {
        long[] identifiers = new long[molecule.Atoms.Count];
        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            identifiers[atomIndex] = BuildEnvironmentIdentifier(molecule, options, previousIdentifiers, atomIndex, radius);
            atomIndex = atomIndex + 1;
        }

        return identifiers;
    }

    internal long BuildEnvironmentIdentifier(Molecule molecule, FingerprintOptions options, long[] previousIdentifiers, int atomId, int radius)
    {
        List<long> neighborValues = new List<long>();
        int bondIndex = 0;
        while (bondIndex < molecule.Bonds.Count)
        {
            Bond bond = molecule.Bonds[bondIndex];
            int neighborAtomId = GetOtherAtomId(bond, atomId);
            if (neighborAtomId >= 0)
            {
                long neighborValue = previousIdentifiers[neighborAtomId];
                long bondValue = GetBondValue(bond, options);
                neighborValues.Add(AddHashValue(neighborValue, bondValue));
            }

            bondIndex = bondIndex + 1;
        }

        SortLongs(neighborValues);

        long value = AddHashValue(previousIdentifiers[atomId], radius);
        int index = 0;
        while (index < neighborValues.Count)
        {
            value = AddHashValue(value, neighborValues[index]);
            index = index + 1;
        }

        return value;
    }

    internal void SetIdentifierBits(BitFingerprint fingerprint, long[] identifiers, int radius)
    {
        int index = 0;
        while (index < identifiers.Length)
        {
            long value = AddHashValue(identifiers[index], radius);
            fingerprint.SetBit(GetFeatureIndex(value, fingerprint.Size));
            index = index + 1;
        }
    }

    internal void IncrementIdentifierCounts(CountFingerprint fingerprint, long[] identifiers, int radius)
    {
        int index = 0;
        while (index < identifiers.Length)
        {
            long value = AddHashValue(identifiers[index], radius);
            fingerprint.Increment(GetFeatureIndex(value, fingerprint.Size));
            index = index + 1;
        }
    }

    internal long GetBondValue(Bond bond, FingerprintOptions options)
    {
        long value = 1;
        if (options.IncludeBondOrder)
        {
            value = (int)bond.Order;
        }

        if (options.IncludeAromaticity && (bond.IsAromatic || bond.Order == BondOrder.Aromatic))
        {
            value = value + 100;
        }

        return value;
    }

    internal void SortLongs(List<long> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            long value = values[index];
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

    internal long AddHashValue(long current, long value)
    {
        long normalized = value % 2147483647;
        if (normalized < 0)
        {
            normalized = -normalized;
        }

        return (current * 131 + normalized + 97) % 2147483647;
    }

    internal int GetFeatureIndex(long value, int size)
    {
        if (size < 1)
        {
            return 0;
        }

        long normalized = value % size;
        if (normalized < 0)
        {
            normalized = -normalized;
        }

        return (int)normalized;
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

        if (options.CircularRadius < 0)
        {
            options.CircularRadius = 0;
        }

        return options;
    }
}
