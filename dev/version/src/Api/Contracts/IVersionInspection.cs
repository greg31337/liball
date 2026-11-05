namespace version;

/// <summary>
/// Defines version state inspection and explicit equality operations for dependency-injected consumers.
/// </summary>
public interface IVersionInspection
{
    /// <summary>
    /// Gets the original version text.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public string GetOriginalText(UniversalVersion version);

    /// <summary>
    /// Gets the dialect that produced the version.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting version dialect.</returns>
    public VersionDialect GetDialect(UniversalVersion version);

    /// <summary>
    /// Reports whether the version is stable.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the version is stable; otherwise, <c>false</c>.</returns>
    public bool IsStable(UniversalVersion version);

    /// <summary>
    /// Reports whether the version is a prerelease.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the version is a prerelease; otherwise, <c>false</c>.</returns>
    public bool IsPrerelease(UniversalVersion version);

    /// <summary>
    /// Reports whether the version has build metadata.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the version has build metadata; otherwise, <c>false</c>.</returns>
    public bool HasBuildMetadata(UniversalVersion version);

    /// <summary>
    /// Gets the number of numeric components.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The requested numeric value.</returns>
    public int GetNumericComponentCount(UniversalVersion version);

    /// <summary>
    /// Gets a numeric component by zero-based index.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="componentIndex">The zero-based component index.</param>
    /// <returns>The resulting text.</returns>
    public string GetNumericComponent(UniversalVersion version, int componentIndex);

    /// <summary>
    /// Gets all numeric components.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change the supplied version.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>A new array containing the numeric components.</returns>
    public string[] GetNumericComponents(UniversalVersion version);

    /// <summary>
    /// Gets the number of prerelease identifiers.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The requested numeric value.</returns>
    public int GetPrereleaseIdentifierCount(UniversalVersion version);

    /// <summary>
    /// Gets a prerelease identifier by zero-based index.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="identifierIndex">The zero-based identifier index.</param>
    /// <returns>The resulting version identifier.</returns>
    public VersionIdentifier GetPrereleaseIdentifier(UniversalVersion version, int identifierIndex);

    /// <summary>
    /// Gets all prerelease identifiers.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change the supplied version.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>A new array containing the prerelease identifiers.</returns>
    public VersionIdentifier[] GetPrereleaseIdentifiers(UniversalVersion version);

    /// <summary>
    /// Gets the number of build identifiers.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The requested numeric value.</returns>
    public int GetBuildIdentifierCount(UniversalVersion version);

    /// <summary>
    /// Gets a build identifier by zero-based index.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="identifierIndex">The zero-based identifier index.</param>
    /// <returns>The resulting version identifier.</returns>
    public VersionIdentifier GetBuildIdentifier(UniversalVersion version, int identifierIndex);

    /// <summary>
    /// Gets all build identifiers.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change the supplied version.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>A new array containing the build identifiers.</returns>
    public VersionIdentifier[] GetBuildIdentifiers(UniversalVersion version);

    /// <summary>
    /// Gets the number of numeric components omitted by shorthand input.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The requested numeric value.</returns>
    public int GetMissingComponentCount(UniversalVersion version);

    /// <summary>
    /// Reports whether the original version used a leading <c>v</c> prefix.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the original version used a leading <c>v</c>; otherwise, <c>false</c>.</returns>
    public bool HasLeadingVPrefix(UniversalVersion version);

    /// <summary>
    /// Reports whether the version has a revision component.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the version has a revision component; otherwise, <c>false</c>.</returns>
    public bool HasRevisionComponent(UniversalVersion version);

    /// <summary>
    /// Gets the revision component, or an empty string when no revision is present.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public string GetRevisionComponent(UniversalVersion version);

    /// <summary>
    /// Reports whether the version has Python-specific metadata.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns><c>true</c> when the version has Python-specific metadata; otherwise, <c>false</c>.</returns>
    public bool HasPythonVersion(UniversalVersion version);

    /// <summary>
    /// Gets the Python-specific version metadata.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting Python version.</returns>
    /// <exception cref="System.InvalidOperationException">The supplied version has no Python-specific metadata.</exception>
    public PythonVersion GetPythonVersion(UniversalVersion version);

    /// <summary>
    /// Reports whether both versions have equal precedence, ignoring identity-only build metadata.
    /// </summary>
    /// <param name="left">The left version used by the operation.</param>
    /// <param name="right">The right version used by the operation.</param>
    /// <returns><c>true</c> when both versions have equal precedence; otherwise, <c>false</c>.</returns>
    public bool EqualsByPrecedence(UniversalVersion left, UniversalVersion right);

    /// <summary>
    /// Reports whether both versions have the same complete identity, including build metadata.
    /// </summary>
    /// <param name="left">The left version used by the operation.</param>
    /// <param name="right">The right version used by the operation.</param>
    /// <returns><c>true</c> when both versions have the same complete identity; otherwise, <c>false</c>.</returns>
    public bool EqualsByIdentity(UniversalVersion left, UniversalVersion right);

    /// <summary>
    /// Reports whether both versions have the same normalized package identity.
    /// </summary>
    /// <param name="left">The left version used by the operation.</param>
    /// <param name="right">The right version used by the operation.</param>
    /// <returns><c>true</c> when both versions have the same normalized package identity; otherwise, <c>false</c>.</returns>
    public bool EqualsByNormalizedIdentity(UniversalVersion left, UniversalVersion right);
}
