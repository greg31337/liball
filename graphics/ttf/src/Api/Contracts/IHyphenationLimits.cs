namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="HyphenationLimits"/>.</summary>
public interface IHyphenationLimits
{
    /// <summary>Returns the maximum encoded dictionary byte count.</summary>
    /// <returns>The input byte limit.</returns>
    int GetMaxInputBytes();

    /// <summary>Returns the maximum number of pattern declarations.</summary>
    /// <returns>The pattern limit.</returns>
    int GetMaxPatterns();

    /// <summary>Returns the maximum scalar count in one pattern.</summary>
    /// <returns>The pattern length limit.</returns>
    int GetMaxPatternLength();

    /// <summary>Returns the maximum number of exception declarations.</summary>
    /// <returns>The exception limit.</returns>
    int GetMaxExceptions();

    /// <summary>Returns the maximum number of matcher trie nodes.</summary>
    /// <returns>The trie node limit.</returns>
    int GetMaxTrieNodes();

    /// <summary>Returns the maximum scalar count accepted by one match.</summary>
    /// <returns>The word length limit.</returns>
    int GetMaxWordLength();
}
