namespace version;

/// <summary>
/// Defines low-level version token and interval inspection operations.
/// </summary>
public interface IVersionSyntaxInspection
{
    /// <summary>
    /// Tokenizes version text with source locations preserved on every token.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The tokens read from the supplied text.</returns>
    public VersionToken[] Tokenize(string text);

    /// <summary>
    /// Converts a version interval to its equivalent comparator set.
    /// </summary>
    /// <param name="interval">The interval used by the operation.</param>
    /// <returns>The resulting version comparator set.</returns>
    public VersionComparatorSet ConvertInterval(VersionInterval interval);
}
