namespace countrycodes;

/// <summary>
/// Defines subdivision catalog operations.
/// </summary>
public interface ISubdivisionCatalog
{
    /// <summary>
    /// Gets all subdivisions in stable full-code order.
    /// </summary>
    /// <returns>Detached subdivision records.</returns>
    Subdivision[] GetSubdivisions();

    /// <summary>
    /// Gets all subdivisions belonging to a country.
    /// </summary>
    /// <param name="countryAlpha2">The country's canonical alpha-2 code.</param>
    /// <returns>Detached subdivision records in stable full-code order.</returns>
    Subdivision[] GetSubdivisionsByCountry(string countryAlpha2);

    /// <summary>
    /// Finds a subdivision by its full code.
    /// </summary>
    /// <param name="value">The full subdivision code to find.</param>
    /// <returns>The structured lookup outcome.</returns>
    SubdivisionLookupResult FindSubdivisionByCode(string value);

    /// <summary>
    /// Finds a subdivision by its full code span.
    /// </summary>
    /// <param name="value">The full subdivision code span to find.</param>
    /// <returns>The structured lookup outcome.</returns>
    SubdivisionLookupResult FindSubdivisionByCodeSpan(global::System.ReadOnlySpan<char> value);

    /// <summary>
    /// Finds a subdivision by a country alpha-2 code and local suffix.
    /// </summary>
    /// <param name="countryAlpha2">The owning country alpha-2 code.</param>
    /// <param name="suffix">The local subdivision suffix.</param>
    /// <returns>The structured lookup outcome.</returns>
    SubdivisionLookupResult FindSubdivisionByCountryAndSuffix(
        string countryAlpha2,
        string suffix);

    /// <summary>
    /// Finds subdivisions by an exact canonical name or alias.
    /// </summary>
    /// <param name="name">The subdivision name to find.</param>
    /// <returns>The structured lookup outcome.</returns>
    SubdivisionLookupResult FindSubdivisionByName(string name);

    /// <summary>
    /// Finds subdivisions by name within one country.
    /// </summary>
    /// <param name="countryAlpha2">The country that constrains the lookup.</param>
    /// <param name="name">The subdivision name to find.</param>
    /// <returns>The structured lookup outcome.</returns>
    SubdivisionLookupResult FindSubdivisionByNameInCountry(
        string countryAlpha2,
        string name);

    /// <summary>
    /// Finds the direct parent of a subdivision.
    /// </summary>
    /// <param name="code">The full subdivision code.</param>
    /// <returns>The structured lookup outcome for the parent.</returns>
    SubdivisionLookupResult FindSubdivisionParent(string code);

    /// <summary>
    /// Gets the direct children of a subdivision.
    /// </summary>
    /// <param name="code">The full subdivision code.</param>
    /// <returns>Detached direct children in stable full-code order.</returns>
    Subdivision[] GetSubdivisionChildren(string code);

    /// <summary>
    /// Gets all ancestors of a subdivision.
    /// </summary>
    /// <param name="code">The full subdivision code.</param>
    /// <returns>Detached ancestors ordered from parent to root.</returns>
    /// <exception cref="SubdivisionNotFoundException">A referenced parent is missing.</exception>
    Subdivision[] GetSubdivisionAncestors(string code);

    /// <summary>
    /// Gets all descendants of a subdivision.
    /// </summary>
    /// <param name="code">The full subdivision code.</param>
    /// <returns>Detached descendants in stable full-code order.</returns>
    Subdivision[] GetSubdivisionDescendants(string code);

    /// <summary>
    /// Resolves the country that owns a subdivision code.
    /// </summary>
    /// <param name="code">The full subdivision code.</param>
    /// <returns>The structured country lookup outcome.</returns>
    CountryLookupResult ResolveCountryForSubdivision(string code);

    /// <summary>
    /// Gets a subdivision by full code or throws a focused exception.
    /// </summary>
    /// <param name="code">The required full subdivision code.</param>
    /// <returns>The detached matching subdivision.</returns>
    /// <exception cref="InvalidSubdivisionCodeException">The input is invalid.</exception>
    /// <exception cref="SubdivisionNotFoundException">No subdivision matches the code.</exception>
    Subdivision GetRequiredSubdivisionByCode(string code);

    /// <summary>
    /// Gets subdivisions having the requested lifecycle status.
    /// </summary>
    /// <param name="status">The subdivision status to select.</param>
    /// <returns>Detached subdivision records in stable code order.</returns>
    Subdivision[] GetSubdivisionsByStatus(SubdivisionStatus status);
}
