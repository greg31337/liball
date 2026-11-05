namespace unicode;

/// <summary>
/// Provides Unicode text segmentation operations.
/// </summary>
public interface IUnicodeSegments
{
    /// <summary>
    /// Returns UTF-16 offsets for every extended grapheme-cluster boundary, including both string ends.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    int[] GetGraphemeBreaks(string text);

    /// <summary>
    /// Returns UTF-16 offsets for every default Unicode word boundary, including both string ends.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    int[] GetWordBreaks(string text);

    /// <summary>
    /// Returns UTF-16 offsets for every default Unicode sentence boundary, including both string ends.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    int[] GetSentenceBreaks(string text);

    /// <summary>
    /// Returns UTF-16 offsets where the default Unicode line algorithm permits or requires a break.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    int[] GetLineBreaks(string text);

    /// <summary>
    /// Returns every permitted line-break position after the start of the text and identifies mandatory breaks.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing optional and mandatory line-break results.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodeLineBreakRecord[] GetLineBreakOpportunities(string text);

    /// <summary>
    /// Returns half-open UTF-16 ranges for extended grapheme clusters.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodeSegmentRecord[] GetGraphemeSegments(string text);

    /// <summary>
    /// Returns half-open UTF-16 ranges for default Unicode word segments.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodeSegmentRecord[] GetWordSegments(string text);

    /// <summary>
    /// Returns half-open UTF-16 ranges for default Unicode sentences.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodeSegmentRecord[] GetSentenceSegments(string text);

    /// <summary>
    /// Returns half-open UTF-16 ranges separated by default Unicode line-break opportunities.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodeSegmentRecord[] GetLineSegments(string text);

    /// <summary>
    /// Counts extended grapheme clusters rather than UTF-16 code units or scalars.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting integer value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    int GetGraphemeClusterCount(string text);

    /// <summary>
    /// Keeps at most the requested number of complete extended grapheme clusters.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="maxGraphemeClusters">The maximum number of complete grapheme clusters to keep.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maxGraphemeClusters</c> is outside the supported range.</exception>
    string TruncateGraphemeClusters(string text, int maxGraphemeClusters);

    /// <summary>
    /// Returns a substring selected by extended grapheme-cluster index and count.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="startGraphemeCluster">The zero-based index of the first grapheme cluster to return.</param>
    /// <param name="graphemeClusterCount">The maximum number of grapheme clusters to return.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when any of <c>startGraphemeCluster</c>, <c>graphemeClusterCount</c> is outside the supported range.</exception>
    string SliceGraphemeClusters(string text, int startGraphemeCluster, int graphemeClusterCount);

    /// <summary>
    /// Keeps the longest prefix within a UTF-16 limit without splitting a grapheme cluster.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="maxUtf16Length">The maximum UTF-16 length; the result never splits a grapheme cluster.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maxUtf16Length</c> is outside the supported range.</exception>
    string TruncateGraphemeUtf16(string text, int maxUtf16Length);

    /// <summary>
    /// Keeps at most the requested number of complete extended grapheme clusters from the end of the text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="maxGraphemeClusters">The maximum number of complete grapheme clusters to keep.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maxGraphemeClusters</c> is negative.</exception>
    string SuffixGraphemeClusters(string text, int maxGraphemeClusters);

    /// <summary>
    /// Returns the display-column width of complete extended grapheme clusters.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting display-column count.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    int GetGraphemeDisplayWidth(string text);

    /// <summary>
    /// Keeps the longest complete-grapheme prefix within a display-column limit.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="maxDisplayWidth">The maximum display-column width.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maxDisplayWidth</c> is negative.</exception>
    string TruncateGraphemeDisplayWidth(string text, int maxDisplayWidth);

    /// <summary>
    /// Returns the boundary class used by the extended grapheme-cluster algorithm.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeGraphemeBreakClass GetGraphemeBreakClass(int codePoint);

    /// <summary>
    /// Returns the boundary class used by the default word algorithm.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeWordBreakClass GetWordBreakClass(int codePoint);

    /// <summary>
    /// Returns the boundary class used by the default sentence algorithm.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeSentenceBreakClass GetSentenceBreakClass(int codePoint);

    /// <summary>
    /// Returns the Unicode line-breaking class used by this library.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    UnicodeLineBreakClass GetLineBreakClass(int codePoint);
}
