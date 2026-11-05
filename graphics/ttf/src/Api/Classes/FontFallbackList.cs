namespace ttf;

/// <summary>Represents an immutable ordered sequence of resolved font matches.</summary>
/// <remarks>
/// The requested family appears first when it resolves, followed by configured
/// script-specific families and then set-wide caller fallback families. Missing
/// families are skipped and repeated face/location matches are omitted.
/// </remarks>
public class FontFallbackList : IFontFallbackList
{
    FontMatch[] matches;

    internal FontFallbackList(FontMatch[] matches)
    {
        this.matches = matches.AsSpan().ToArray();
    }

    /// <summary>Gets the number of resolved distinct matches.</summary>
    /// <returns>The match count.</returns>
    public int GetCount()
    {
        return matches.Length;
    }

    /// <summary>Gets one resolved match in configured priority order.</summary>
    /// <param name="index">The zero-based match index.</param>
    /// <returns>The resolved match.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the match range.
    /// </exception>
    public FontMatch GetMatch(int index)
    {
        if (index < 0 || index >= matches.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return matches[index];
    }
}
