namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontFallbackList"/>.</summary>
public interface IFontFallbackList
{
    /// <summary>Gets the number of resolved distinct matches.</summary>
    /// <returns>The match count.</returns>
    int GetCount();

    /// <summary>Gets one resolved match in configured priority order.</summary>
    /// <param name="index">The zero-based match index.</param>
    /// <returns>The resolved match.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the match range.
    /// </exception>
    FontMatch GetMatch(int index);
}
