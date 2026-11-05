namespace units;

/// <summary>
/// Supplies the read-only unit metadata required by parsing, serialization, and conversion workflows.
/// </summary>
/// <remarks>
/// Implement this interface to provide unit metadata without exposing registry mutation. UnitRegistry is the built-in implementation.
/// </remarks>
public interface IUnitCatalog
{
    /// <summary>
    /// Finds the unit or units matching identity, name, symbol, or alias text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result that reports a unique match, ambiguity, or failure.</returns>
    public UnitLookupResult FindUnit(string text);

    /// <summary>
    /// Finds the unit matching the supplied stable identifier.
    /// </summary>
    /// <param name="id">The stable identifier to find.</param>
    /// <returns>A result that reports a unique match or failure.</returns>
    public UnitLookupResult FindUnitById(string id);

    /// <summary>
    /// Returns a copy of the registered prefixes.
    /// </summary>
    /// <returns>A copy of the registered prefix definitions.</returns>
    public PrefixDefinition[] GetPrefixes();

    /// <summary>
    /// Finds the quantity kind matching identity, name, or alias text.
    /// </summary>
    /// <param name="idOrName">The identifier, name, or alias to find.</param>
    /// <returns>A result that reports a unique match, ambiguity, or failure.</returns>
    public QuantityKindLookupResult FindQuantityKind(string idOrName);

    /// <summary>
    /// Finds the equivalency matching identity, name, or alias text.
    /// </summary>
    /// <param name="idOrName">The identifier, name, or alias to find.</param>
    /// <returns>The matching equivalency, or null when none matches.</returns>
    public EquivalencyDefinition? FindEquivalency(string idOrName);
}
