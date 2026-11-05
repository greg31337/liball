namespace version;

/// <summary>
/// Defines version formatting operations for dependency-injected consumers.
/// </summary>
public interface IVersionFormatting
{
    /// <summary>
    /// Formats the supplied value as original text.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public string FormatOriginal(UniversalVersion version);

    /// <summary>
    /// Formats the supplied value as normalized text.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public string FormatNormalized(UniversalVersion version);

    /// <summary>
    /// Formats the supplied value as canonical text.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public string FormatCanonical(UniversalVersion version);

    /// <summary>
    /// Formats the supplied value as display text.
    /// </summary>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public string FormatDisplay(UniversalVersion version);
}


