namespace uri;

/// <summary>
/// Separates URN assigned names and transfers q- and f-components to resolved locators.
/// </summary>
public interface IUrnLocatorUtility
{
    /// <summary>Applies a URN q-component and f-component to a resolved locator.</summary>
    /// <param name="urn">The complete source URN text.</param>
    /// <param name="locator">The resolved URI-reference text.</param>
    /// <param name="options">The collision and processing options.</param>
    /// <returns>The locator text after applying selected URN components.</returns>
    /// <exception cref="UriResolutionException">Validation, collision policy, or a configured limit fails.</exception>
    string ApplyUrnComponentsToLocator(
        string urn,
        string locator,
        UrnLocatorMergeOptions options);

    /// <summary>Returns the assigned-name portion without r-, q-, or f-components.</summary>
    /// <param name="urn">The complete source URN text.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The lossless assigned-name text.</returns>
    /// <exception cref="UrnSyntaxException">Validation or a configured limit fails.</exception>
    string SeparateAssignedName(string urn, UrnParseOptions options);
}
