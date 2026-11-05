namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="PrefixCatalog"/>.
/// </summary>
public interface IPrefixCatalog
{
    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    public int GetCount();
    /// <summary>
    /// Returns a copy of the prefixes.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public PrefixDefinition[] GetPrefixes();
    /// <summary>
    /// Returns the prefix matching the supplied identifier, name, or symbol, or null when none matches.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <returns>The resulting prefix definition.</returns>
    public PrefixDefinition? FindById(string? id);
    /// <summary>
    /// Returns the prefix matching the supplied identifier, name, or symbol, or null when none matches.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <returns>The resulting prefix definition.</returns>
    public PrefixDefinition? FindByName(string? name);
    /// <summary>
    /// Returns the prefix matching the supplied identifier, name, or symbol, or null when none matches.
    /// </summary>
    /// <param name="symbol">The unit or metadata symbol.</param>
    /// <returns>The resulting prefix definition.</returns>
    public PrefixDefinition? FindBySymbol(string? symbol);
}
