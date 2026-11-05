namespace ttf;

/// <summary>Defines hard resource limits for hyphenation dictionary loading and matching.</summary>
public class HyphenationLimits : IHyphenationLimits
{
    const int DefaultMaxInputBytes = 1048576;
    const int DefaultMaxPatterns = 50000;
    const int DefaultMaxPatternLength = 64;
    const int DefaultMaxExceptions = 10000;
    const int DefaultMaxTrieNodes = 500000;
    const int DefaultMaxWordLength = 256;

    int maxInputBytes;
    int maxPatterns;
    int maxPatternLength;
    int maxExceptions;
    int maxTrieNodes;
    int maxWordLength;

    /// <summary>Creates conservative default limits.</summary>
    public HyphenationLimits()
    {
        maxInputBytes = DefaultMaxInputBytes;
        maxPatterns = DefaultMaxPatterns;
        maxPatternLength = DefaultMaxPatternLength;
        maxExceptions = DefaultMaxExceptions;
        maxTrieNodes = DefaultMaxTrieNodes;
        maxWordLength = DefaultMaxWordLength;
    }

    /// <summary>Creates explicitly configured limits.</summary>
    /// <param name="maxInputBytes">The maximum encoded dictionary byte count.</param>
    /// <param name="maxPatterns">The maximum number of pattern declarations.</param>
    /// <param name="maxPatternLength">The maximum scalar count in one pattern.</param>
    /// <param name="maxExceptions">The maximum number of exception declarations.</param>
    /// <param name="maxTrieNodes">The maximum number of matcher trie nodes.</param>
    /// <param name="maxWordLength">The maximum scalar count accepted by one match.</param>
    /// <returns>A new configured limit set.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// A maximum is not positive, except <c>maxExceptions</c>, which may be zero.
    /// </exception>
    internal static HyphenationLimits Create(
        int maxInputBytes,
        int maxPatterns,
        int maxPatternLength,
        int maxExceptions,
        int maxTrieNodes,
        int maxWordLength)
    {
        if (maxInputBytes <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maxInputBytes");
        }

        if (maxPatterns <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maxPatterns");
        }

        if (maxPatternLength <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maxPatternLength");
        }

        if (maxExceptions < 0)
        {
            throw new System.ArgumentOutOfRangeException("maxExceptions");
        }

        if (maxTrieNodes <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maxTrieNodes");
        }

        if (maxWordLength <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maxWordLength");
        }

        HyphenationLimits limits = new HyphenationLimits();
        limits.maxInputBytes = maxInputBytes;
        limits.maxPatterns = maxPatterns;
        limits.maxPatternLength = maxPatternLength;
        limits.maxExceptions = maxExceptions;
        limits.maxTrieNodes = maxTrieNodes;
        limits.maxWordLength = maxWordLength;
        return limits;
    }

    /// <summary>Creates conservative default limits.</summary>
    /// <returns>A new default limit set.</returns>
    internal static HyphenationLimits Create()
    {
        return new HyphenationLimits();
    }

    /// <summary>Returns the maximum encoded dictionary byte count.</summary>
    /// <returns>The input byte limit.</returns>
    public int GetMaxInputBytes()
    {
        return maxInputBytes;
    }

    /// <summary>Returns the maximum number of pattern declarations.</summary>
    /// <returns>The pattern limit.</returns>
    public int GetMaxPatterns()
    {
        return maxPatterns;
    }

    /// <summary>Returns the maximum scalar count in one pattern.</summary>
    /// <returns>The pattern length limit.</returns>
    public int GetMaxPatternLength()
    {
        return maxPatternLength;
    }

    /// <summary>Returns the maximum number of exception declarations.</summary>
    /// <returns>The exception limit.</returns>
    public int GetMaxExceptions()
    {
        return maxExceptions;
    }

    /// <summary>Returns the maximum number of matcher trie nodes.</summary>
    /// <returns>The trie node limit.</returns>
    public int GetMaxTrieNodes()
    {
        return maxTrieNodes;
    }

    /// <summary>Returns the maximum scalar count accepted by one match.</summary>
    /// <returns>The word length limit.</returns>
    public int GetMaxWordLength()
    {
        return maxWordLength;
    }
}
