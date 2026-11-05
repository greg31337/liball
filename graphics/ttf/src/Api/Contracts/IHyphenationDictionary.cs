namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="HyphenationDictionary"/>.</summary>
public interface IHyphenationDictionary
{
    /// <summary>Returns the effective minimum scalars retained before a break.</summary>
    /// <returns>The left-edge minimum.</returns>
    int GetLeftMinimum();

    /// <summary>Returns the effective minimum scalars retained after a break.</summary>
    /// <returns>The right-edge minimum.</returns>
    int GetRightMinimum();

    /// <summary>Returns the number of loaded pattern declarations.</summary>
    /// <returns>The pattern declaration count.</returns>
    int GetPatternCount();

    /// <summary>Returns the number of loaded exception declarations.</summary>
    /// <returns>The exception declaration count.</returns>
    int GetExceptionCount();

    /// <summary>Matches a word and returns its allowed internal hyphenation boundaries.</summary>
    /// <param name="word">The word to match.</param>
    /// <returns>Immutable hyphenation opportunities in ascending source order.</returns>
    /// <exception cref="System.ArgumentNullException"><c>word</c> is null.</exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// <c>word</c> contains malformed UTF-16.
    /// </exception>
    /// <exception cref="FontLimitException">The configured word length is exceeded.</exception>
    HyphenationMatch Match(string word);

    /// <summary>Matches a word and returns its allowed internal hyphenation boundaries.</summary>
    /// <param name="word">The word to match.</param>
    /// <returns>Immutable hyphenation opportunities in ascending source order.</returns>
    /// <exception cref="System.ArgumentNullException"><c>word</c> is null.</exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// <c>word</c> contains malformed UTF-16.
    /// </exception>
    /// <exception cref="FontLimitException">The configured word length is exceeded.</exception>
    HyphenationMatch FindHyphenationPoints(string word);
}
