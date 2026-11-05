namespace units;

/// <summary>
/// Provides lookup over a fixed set of unit prefixes.
/// </summary>
public class PrefixCatalog : IPrefixCatalog
{
    internal PrefixDefinition[] _prefixes;

    /// <summary>
    /// Initializes a new prefix catalog from the supplied metadata.
    /// </summary>
    /// <param name="prefixes">The prefix definitions in the catalog.</param>
    public PrefixCatalog(PrefixDefinition[] prefixes)
    {
        ValidatePrefixes(prefixes);
        _prefixes = CopyPrefixes(prefixes);
    }

    internal static PrefixCatalog CreateSi()
    {
        return new PrefixCatalog(
            new PrefixDefinition[]
            {
                CreatePowerOfTen("quetta", "quetta", "Q", 30),
                CreatePowerOfTen("ronna", "ronna", "R", 27),
                CreatePowerOfTen("yotta", "yotta", "Y", 24),
                CreatePowerOfTen("zetta", "zetta", "Z", 21),
                CreatePowerOfTen("exa", "exa", "E", 18),
                CreatePowerOfTen("peta", "peta", "P", 15),
                CreatePowerOfTen("tera", "tera", "T", 12),
                CreatePowerOfTen("giga", "giga", "G", 9),
                CreatePowerOfTen("mega", "mega", "M", 6),
                CreatePowerOfTen("kilo", "kilo", "k", 3),
                CreatePowerOfTen("hecto", "hecto", "h", 2),
                CreatePowerOfTen("deca", "deca", "da", 1),
                CreatePowerOfTen("deci", "deci", "d", -1),
                CreatePowerOfTen("centi", "centi", "c", -2),
                CreatePowerOfTen("milli", "milli", "m", -3),
                CreatePowerOfTen("micro", "micro", "\u03bc", -6),
                CreatePowerOfTen("nano", "nano", "n", -9),
                CreatePowerOfTen("pico", "pico", "p", -12),
                CreatePowerOfTen("femto", "femto", "f", -15),
                CreatePowerOfTen("atto", "atto", "a", -18),
                CreatePowerOfTen("zepto", "zepto", "z", -21),
                CreatePowerOfTen("yocto", "yocto", "y", -24),
                CreatePowerOfTen("ronto", "ronto", "r", -27),
                CreatePowerOfTen("quecto", "quecto", "q", -30)
            });
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetCount()
    {
        return _prefixes.Length;
    }

    /// <summary>
    /// Returns a copy of the prefixes.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public PrefixDefinition[] GetPrefixes()
    {
        return CopyPrefixes(_prefixes);
    }

    /// <summary>
    /// Returns the prefix matching the supplied identifier, name, or symbol, or null when none matches.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <returns>The resulting prefix definition.</returns>
    public PrefixDefinition? FindById(string? id)
    {
        return FindByText(id, 0);
    }

    /// <summary>
    /// Returns the prefix matching the supplied identifier, name, or symbol, or null when none matches.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <returns>The resulting prefix definition.</returns>
    public PrefixDefinition? FindByName(string? name)
    {
        return FindByText(name, 1);
    }

    /// <summary>
    /// Returns the prefix matching the supplied identifier, name, or symbol, or null when none matches.
    /// </summary>
    /// <param name="symbol">The unit or metadata symbol.</param>
    /// <returns>The resulting prefix definition.</returns>
    public PrefixDefinition? FindBySymbol(string? symbol)
    {
        return FindByText(symbol, 2);
    }

    internal PrefixDefinition? FindByText(string? value, int field)
    {
        if (value == null)
        {
            return null;
        }

        int index = 0;
        while (index < _prefixes.Length)
        {
            PrefixDefinition prefix = _prefixes[index];
            if (field == 0 && string.Equals(prefix.GetId(), value, System.StringComparison.Ordinal))
            {
                return prefix;
            }

            if (field == 1 && string.Equals(prefix.GetName(), value, System.StringComparison.Ordinal))
            {
                return prefix;
            }

            if (field == 2 && string.Equals(prefix.GetSymbol(), value, System.StringComparison.Ordinal))
            {
                return prefix;
            }

            index = index + 1;
        }

        return null;
    }

    internal static PrefixDefinition CreatePowerOfTen(string id, string name, string symbol, int exponent)
    {
        return new PrefixDefinition(id, name, symbol, PowerOfTen(exponent));
    }

    internal static RationalNumber PowerOfTen(int exponent)
    {
        if (exponent >= 0)
        {
            return RationalNumber.FromBigIntegers(System.Numerics.BigInteger.Pow(new System.Numerics.BigInteger(10), exponent), new System.Numerics.BigInteger(1));
        }

        return RationalNumber.FromBigIntegers(new System.Numerics.BigInteger(1), System.Numerics.BigInteger.Pow(new System.Numerics.BigInteger(10), 0 - exponent));
    }

    internal static void ValidatePrefixes(PrefixDefinition[] prefixes)
    {
        if (prefixes == null)
        {
            throw new System.ArgumentNullException("prefixes");
        }

        int index = 0;
        while (index < prefixes.Length)
        {
            if (prefixes[index] == null)
            {
                throw new System.ArgumentNullException("prefixes");
            }

            ValidateNoDuplicate(prefixes, index);
            index = index + 1;
        }
    }

    internal static void ValidateNoDuplicate(PrefixDefinition[] prefixes, int index)
    {
        int other = 0;
        while (other < index)
        {
            if (string.Equals(prefixes[other].GetId(), prefixes[index].GetId(), System.StringComparison.Ordinal))
            {
                throw new System.ArgumentException("Duplicate prefix id.");
            }

            if (string.Equals(prefixes[other].GetName(), prefixes[index].GetName(), System.StringComparison.Ordinal))
            {
                throw new System.ArgumentException("Duplicate prefix name.");
            }

            if (string.Equals(prefixes[other].GetSymbol(), prefixes[index].GetSymbol(), System.StringComparison.Ordinal))
            {
                throw new System.ArgumentException("Duplicate prefix symbol.");
            }

            other = other + 1;
        }
    }

    internal static PrefixDefinition[] CopyPrefixes(PrefixDefinition[] prefixes)
    {
        PrefixDefinition[] copy = new PrefixDefinition[prefixes.Length];
        int index = 0;

        while (index < prefixes.Length)
        {
            copy[index] = prefixes[index];
            index = index + 1;
        }

        return copy;
    }
}
