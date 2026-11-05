namespace csv;

/// <summary>
/// Supplies identifying information about the CSV library.
/// </summary>
public interface ICsvLibrary
{
    /// <summary>
    /// Returns the namespace containing the public API.
    /// </summary>
    /// <returns>The public namespace name.</returns>
    string GetNamespaceName();

    /// <summary>
    /// Returns the human-readable library name.
    /// </summary>
    /// <returns>The display name.</returns>
    string GetDisplayName();
}
