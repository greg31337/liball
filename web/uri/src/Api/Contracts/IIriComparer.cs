namespace uri;

/// <summary>
/// Compares IRI references exactly, after NFC, or after generic URI mapping.
/// </summary>
public interface IIriComparer
{
    /// <summary>Compares lossless Unicode component text and delimiter presence.</summary>
    /// <param name="left">The first IRI-reference text.</param>
    /// <param name="right">The second IRI-reference text.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The comparison outcome.</returns>
    UriComparisonResult CompareIriExact(
        string left,
        string right,
        UriComparisonOptions options);

    /// <summary>Compares IRI components after Unicode Normalization Form C.</summary>
    /// <param name="left">The first IRI-reference text.</param>
    /// <param name="right">The second IRI-reference text.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The comparison outcome.</returns>
    UriComparisonResult CompareIriUnicode(
        string left,
        string right,
        UriComparisonOptions options);

    /// <summary>Compares after mapping to and syntax-normalizing URI references.</summary>
    /// <param name="left">The first IRI-reference text.</param>
    /// <param name="right">The second IRI-reference text.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The comparison outcome.</returns>
    UriComparisonResult CompareIriAsUri(
        string left,
        string right,
        UriComparisonOptions options);
}
