using System.Collections.Generic;

namespace molecule;

internal class PatternFingerprint
{
    internal MaccsKeys Keys;
    internal SmartsMatcher Matcher;

    internal PatternFingerprint()
    {
        Keys = new MaccsKeys();
        Matcher = new SmartsMatcher();
    }

    internal BitFingerprint Create(Molecule? molecule, FingerprintOptions? options)
    {
        FingerprintOptions safeOptions = NormalizeOptions(options);
        BitFingerprint fingerprint = new BitFingerprint(safeOptions.BitCount);
        if (molecule == null)
        {
            return fingerprint;
        }

        List<MaccsKey> keys = Keys.GetKeys();
        int index = 0;
        while (index < keys.Count)
        {
            MaccsKey key = keys[index];
            if (MatchesKey(molecule, key))
            {
                fingerprint.SetBit(GetBitIndex(key, fingerprint.Size));
            }

            index = index + 1;
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

        List<MaccsKey> keys = Keys.GetKeys();
        int index = 0;
        while (index < keys.Count)
        {
            MaccsKey key = keys[index];
            int matchCount = CountMatches(molecule, key);
            if (matchCount > 0)
            {
                fingerprint.AddCount(GetBitIndex(key, fingerprint.Size), matchCount);
            }

            index = index + 1;
        }

        return fingerprint;
    }

    internal bool MatchesKey(Molecule molecule, MaccsKey? key)
    {
        if (key == null || key.Smarts.Length == 0)
        {
            return false;
        }

        return Matcher.IsMatch(molecule, key.Smarts);
    }

    internal int CountMatches(Molecule molecule, MaccsKey? key)
    {
        if (key == null || key.Smarts.Length == 0)
        {
            return 0;
        }

        return Matcher.FindAll(molecule, key.Smarts).Count;
    }

    internal int GetBitIndex(MaccsKey key, int size)
    {
        if (size < 1)
        {
            return 0;
        }

        int index = key.Index - 1;
        if (index < 0)
        {
            index = 0;
        }

        return index % size;
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

        return options;
    }
}
