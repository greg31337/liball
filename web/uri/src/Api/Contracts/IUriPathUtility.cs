namespace uri;

/// <summary>
/// Provides bounded operations over URI path text without decoding reserved separators.
/// </summary>
public interface IUriPathUtility
{
    /// <summary>Removes literal dot segments using the RFC 3986 algorithm.</summary>
    /// <param name="path">The URI path text.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The path with literal dot segments removed.</returns>
    /// <exception cref="UriValidationException">Path validation or a configured limit fails.</exception>
    string RemoveDotSegments(string path, OperationLimits limits);

    /// <summary>Joins path tokens while sharing one literal separator at each seam.</summary>
    /// <param name="paths">The URI path tokens to join.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The joined URI path.</returns>
    /// <exception cref="UriValidationException">Path validation or a configured limit fails.</exception>
    string JoinPaths(string[] paths, OperationLimits limits);

    /// <summary>Splits a path at literal separators while preserving empty segments.</summary>
    /// <param name="path">The URI path text.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The ordered path segments.</returns>
    /// <exception cref="UriValidationException">Path validation or a configured limit fails.</exception>
    string[] SplitPathSegments(string path, OperationLimits limits);

    /// <summary>Finds the longest ordinal shared directory prefix.</summary>
    /// <param name="firstPath">The first URI path.</param>
    /// <param name="secondPath">The second URI path.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The shared base directory, ending in a literal separator when nonempty.</returns>
    /// <exception cref="UriValidationException">Path validation or a configured limit fails.</exception>
    string FindCommonBasePath(
        string firstPath,
        string secondPath,
        OperationLimits limits);
}
