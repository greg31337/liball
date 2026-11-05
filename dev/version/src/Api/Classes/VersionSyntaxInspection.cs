namespace version;

/// <summary>
/// Provides public inspection operations for version tokens and interval comparators.
/// </summary>
/// <remarks>
/// This class exposes public data results while keeping scanner and range implementation types internal.
/// </remarks>
public class VersionSyntaxInspection : IVersionSyntaxInspection
{
    /// <summary>
    /// Initializes a stateless version syntax inspection service.
    /// </summary>
    public VersionSyntaxInspection()
    {
    }

    /// <summary>
    /// Tokenizes version text with source locations preserved on every token.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The tokens read from the supplied text, including the final end token.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public VersionToken[] Tokenize(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        VersionScanner scanner = new VersionScanner(text);
        System.Collections.Generic.List<VersionToken> tokens = new System.Collections.Generic.List<VersionToken>();
        VersionToken token = scanner.ReadToken();

        while (token.GetKind() != VersionTokenKind.End)
        {
            tokens.Add(token);
            token = scanner.ReadToken();
        }

        tokens.Add(token);
        return tokens.ToArray();
    }

    /// <summary>
    /// Converts a version interval to its equivalent comparator set.
    /// </summary>
    /// <param name="interval">The interval used by the operation.</param>
    /// <returns>The resulting version comparator set.</returns>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public VersionComparatorSet ConvertInterval(VersionInterval interval)
    {
        System.ArgumentNullException.ThrowIfNull(interval);
        return interval.ToComparatorSet();
    }
}
