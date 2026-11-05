using System.Collections.Generic;

namespace unicode;

/// <summary>
/// Finds Unicode grapheme, word, sentence, and line boundaries using UTF-16 offsets.
/// </summary>
public class UnicodeSegments : IUnicodeSegments
{
    internal static readonly UnicodeSegments Instance = new UnicodeSegments();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodeSegments()
    {
    }
    /// <summary>
    /// Returns UTF-16 offsets for every extended grapheme-cluster boundary, including both string ends.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public int[] GetGraphemeBreaks(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        UnicodeGraphemeBreakClass[] classes = new UnicodeGraphemeBreakClass[scalars.Length];
        int index = 0;

        while (index < scalars.Length)
        {
            classes[index] = GetGraphemeBreakClass(scalars[index].CodePoint);
            index = index + 1;
        }

        List<int> breaks = new List<int>();
        breaks.Add(0);
        index = 1;

        while (index < scalars.Length)
        {
            if (ShouldBreakGrapheme(scalars, classes, index))
            {
                breaks.Add(scalars[index].Utf16Index);
            }

            index = index + 1;
        }

        if (breaks.Count == 0)
        {
            breaks.Add(0);
        }

        if (breaks[breaks.Count - 1] != text.Length)
        {
            breaks.Add(text.Length);
        }

        return breaks.ToArray();
    }

    /// <summary>
    /// Returns UTF-16 offsets for every default Unicode word boundary, including both string ends.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public int[] GetWordBreaks(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        UnicodeWordBreakClass[] classes = new UnicodeWordBreakClass[scalars.Length];
        int index = 0;

        while (index < scalars.Length)
        {
            classes[index] = GetWordBreakClass(scalars[index].CodePoint);
            index = index + 1;
        }

        List<int> breaks = new List<int>();
        breaks.Add(0);
        index = 1;

        while (index < scalars.Length)
        {
            if (ShouldBreakWord(scalars, classes, index))
            {
                breaks.Add(scalars[index].Utf16Index);
            }

            index = index + 1;
        }

        if (breaks[breaks.Count - 1] != text.Length)
        {
            breaks.Add(text.Length);
        }

        return breaks.ToArray();
    }

    /// <summary>
    /// Returns UTF-16 offsets for every default Unicode sentence boundary, including both string ends.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public int[] GetSentenceBreaks(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        UnicodeSentenceBreakClass[] classes = new UnicodeSentenceBreakClass[scalars.Length];
        int index = 0;

        while (index < scalars.Length)
        {
            classes[index] = GetSentenceBreakClass(scalars[index].CodePoint);
            index = index + 1;
        }

        List<int> breaks = new List<int>();
        breaks.Add(0);
        index = 1;

        while (index < scalars.Length)
        {
            if (ShouldBreakSentence(scalars, classes, index))
            {
                breaks.Add(scalars[index].Utf16Index);
            }

            index = index + 1;
        }

        if (breaks[breaks.Count - 1] != text.Length)
        {
            breaks.Add(text.Length);
        }

        return breaks.ToArray();
    }

    /// <summary>
    /// Returns UTF-16 offsets where the default Unicode line algorithm permits or requires a break.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public int[] GetLineBreaks(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeLineBreakRecord[] opportunities = GetLineBreakOpportunities(text);
        List<int> breaks = new List<int>();
        breaks.Add(0);
        int index = 0;

        while (index < opportunities.Length)
        {
            if (opportunities[index].Utf16Index != 0)
            {
                breaks.Add(opportunities[index].Utf16Index);
            }

            index = index + 1;
        }

        return breaks.ToArray();
    }

    /// <summary>
    /// Returns every permitted line-break position after the start of the text and identifies mandatory breaks.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing optional and mandatory line-break results.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodeLineBreakRecord[] GetLineBreakOpportunities(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        UnicodeLineBreakClass[] originalClasses = new UnicodeLineBreakClass[scalars.Length];
        int index = 0;

        while (index < scalars.Length)
        {
            originalClasses[index] = GetLineBreakClass(scalars[index].CodePoint);
            index = index + 1;
        }

        UnicodeLineBreakClass[] classes = ResolveLineBreakClasses(scalars, originalClasses);
        List<UnicodeLineBreakRecord> results = new List<UnicodeLineBreakRecord>();
        index = 1;

        while (index < scalars.Length)
        {
            int status = GetLineBreakDecision(scalars, originalClasses, classes, index);
            if (status == 1)
            {
                results.Add(new UnicodeLineBreakRecord(scalars[index].Utf16Index, UnicodeLineBreakStatus.Optional));
            }
            else if (status == 2)
            {
                results.Add(new UnicodeLineBreakRecord(scalars[index].Utf16Index, UnicodeLineBreakStatus.Mandatory));
            }

            index = index + 1;
        }

        results.Add(new UnicodeLineBreakRecord(text.Length, UnicodeLineBreakStatus.Mandatory));
        return results.ToArray();
    }

    /// <summary>
    /// Returns half-open UTF-16 ranges for extended grapheme clusters.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodeSegmentRecord[] GetGraphemeSegments(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return CreateSegments(GetGraphemeBreaks(text));
    }

    /// <summary>
    /// Returns half-open UTF-16 ranges for default Unicode word segments.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodeSegmentRecord[] GetWordSegments(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return CreateSegments(GetWordBreaks(text));
    }

    /// <summary>
    /// Returns half-open UTF-16 ranges for default Unicode sentences.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodeSegmentRecord[] GetSentenceSegments(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return CreateSegments(GetSentenceBreaks(text));
    }

    /// <summary>
    /// Returns half-open UTF-16 ranges separated by default Unicode line-break opportunities.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodeSegmentRecord[] GetLineSegments(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return CreateSegments(GetLineBreaks(text));
    }

    internal static UnicodeSegmentRecord[] CreateSegments(int[] breaks)
    {
        System.ArgumentNullException.ThrowIfNull(breaks);

        if (breaks.Length < 2)
        {
            return new UnicodeSegmentRecord[]
            {
            };
        }

        UnicodeSegmentRecord[] segments = new UnicodeSegmentRecord[breaks.Length - 1];
        int index = 0;

        while (index < segments.Length)
        {
            segments[index] = new UnicodeSegmentRecord(breaks[index], breaks[index + 1]);
            index = index + 1;
        }

        return segments;
    }

    /// <summary>
    /// Counts extended grapheme clusters rather than UTF-16 code units or scalars.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting integer value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public int GetGraphemeClusterCount(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return GetGraphemeSegments(text).Length;
    }

    /// <summary>
    /// Keeps at most the requested number of complete extended grapheme clusters.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="maxGraphemeClusters">The maximum number of complete grapheme clusters to keep.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maxGraphemeClusters</c> is outside the supported range.</exception>
    public string TruncateGraphemeClusters(string text, int maxGraphemeClusters)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (maxGraphemeClusters < 0)
        {
            throw new System.ArgumentOutOfRangeException("maxGraphemeClusters");
        }

        if (maxGraphemeClusters == 0)
        {
            return "";
        }

        UnicodeSegmentRecord[] segments = GetGraphemeSegments(text);
        if (maxGraphemeClusters >= segments.Length)
        {
            return text;
        }

        return text.Substring(0, segments[maxGraphemeClusters - 1].EndUtf16Index);
    }

    /// <summary>
    /// Returns a substring selected by extended grapheme-cluster index and count.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="startGraphemeCluster">The zero-based index of the first grapheme cluster to return.</param>
    /// <param name="graphemeClusterCount">The maximum number of grapheme clusters to return.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when any of <c>startGraphemeCluster</c>, <c>graphemeClusterCount</c> is outside the supported range.</exception>
    public string SliceGraphemeClusters(string text, int startGraphemeCluster, int graphemeClusterCount)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (startGraphemeCluster < 0)
        {
            throw new System.ArgumentOutOfRangeException("startGraphemeCluster");
        }

        if (graphemeClusterCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("graphemeClusterCount");
        }

        if (graphemeClusterCount == 0)
        {
            return "";
        }

        UnicodeSegmentRecord[] segments = GetGraphemeSegments(text);
        if (startGraphemeCluster >= segments.Length)
        {
            return "";
        }

        int remainingClusterCount = segments.Length - startGraphemeCluster;
        int selectedClusterCount = graphemeClusterCount;
        if (selectedClusterCount > remainingClusterCount)
        {
            selectedClusterCount = remainingClusterCount;
        }

        int endCluster = startGraphemeCluster + selectedClusterCount;
        int start = segments[startGraphemeCluster].StartUtf16Index;
        int end = segments[endCluster - 1].EndUtf16Index;
        return text.Substring(start, end - start);
    }

    /// <summary>
    /// Keeps the longest prefix within a UTF-16 limit without splitting a grapheme cluster.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="maxUtf16Length">The maximum UTF-16 length; the result never splits a grapheme cluster.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maxUtf16Length</c> is outside the supported range.</exception>
    public string TruncateGraphemeUtf16(string text, int maxUtf16Length)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (maxUtf16Length < 0)
        {
            throw new System.ArgumentOutOfRangeException("maxUtf16Length");
        }

        if (maxUtf16Length == 0)
        {
            return "";
        }

        if (maxUtf16Length >= text.Length)
        {
            return text;
        }

        UnicodeSegmentRecord[] segments = GetGraphemeSegments(text);
        int end = 0;
        int index = 0;

        while (index < segments.Length)
        {
            if (segments[index].EndUtf16Index > maxUtf16Length)
            {
                break;
            }

            end = segments[index].EndUtf16Index;
            index = index + 1;
        }

        return text.Substring(0, end);
    }

    /// <summary>
    /// Keeps at most the requested number of complete extended grapheme clusters from the end of the text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="maxGraphemeClusters">The maximum number of complete grapheme clusters to keep.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maxGraphemeClusters</c> is negative.</exception>
    public string SuffixGraphemeClusters(string text, int maxGraphemeClusters)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (maxGraphemeClusters < 0)
        {
            throw new System.ArgumentOutOfRangeException("maxGraphemeClusters");
        }

        if (maxGraphemeClusters == 0)
        {
            return "";
        }

        UnicodeSegmentRecord[] segments = GetGraphemeSegments(text);
        if (maxGraphemeClusters >= segments.Length)
        {
            return text;
        }

        int first = segments.Length - maxGraphemeClusters;
        int start = segments[first].StartUtf16Index;
        return text.Substring(start);
    }

    /// <summary>
    /// Returns the display-column width of complete extended grapheme clusters.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting display-column count.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public int GetGraphemeDisplayWidth(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeSegmentRecord[] segments = GetGraphemeSegments(text);
        int width = 0;
        int index = 0;

        while (index < segments.Length)
        {
            width = width + GetSegmentDisplayWidth(text, segments[index]);
            index = index + 1;
        }

        return width;
    }

    /// <summary>
    /// Keeps the longest complete-grapheme prefix within a display-column limit.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="maxDisplayWidth">The maximum display-column width.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maxDisplayWidth</c> is negative.</exception>
    public string TruncateGraphemeDisplayWidth(string text, int maxDisplayWidth)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (maxDisplayWidth < 0)
        {
            throw new System.ArgumentOutOfRangeException("maxDisplayWidth");
        }

        UnicodeSegmentRecord[] segments = GetGraphemeSegments(text);
        int width = 0;
        int end = 0;
        int index = 0;

        while (index < segments.Length)
        {
            int segmentWidth = GetSegmentDisplayWidth(text, segments[index]);
            if (segmentWidth > maxDisplayWidth - width)
            {
                break;
            }

            width = width + segmentWidth;
            end = segments[index].EndUtf16Index;
            index = index + 1;
        }

        if (end == text.Length)
        {
            return text;
        }

        return text.Substring(0, end);
    }

    /// <summary>
    /// Returns the boundary class used by the extended grapheme-cluster algorithm.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeGraphemeBreakClass GetGraphemeBreakClass(int codePoint)
    {
        string name = LookupBoundaryName(codePoint, UnicodeBoundaryTables.GraphemeBreaks(), UnicodeBoundaryTables.GraphemeBreakNames(), "");
        if (name.Length > 0)
        {
            return GraphemeBreakClassFromName(name);
        }

        name = LookupBoundaryName(codePoint, UnicodeBoundaryTables.HangulSyllableTypes(), UnicodeBoundaryTables.HangulSyllableTypeNames(), "");
        if (name.Length > 0)
        {
            return GraphemeBreakClassFromName(name);
        }

        if (HasBoundaryProperty(codePoint, UnicodeBoundaryTables.EmojiBoundaryProperties(), UnicodeBoundaryTables.EmojiBoundaryPropertyNames(), "Extended_Pictographic"))
        {
            return UnicodeGraphemeBreakClass.ExtendedPictographic;
        }

        return UnicodeGraphemeBreakClass.Other;
    }

    /// <summary>
    /// Returns the boundary class used by the default word algorithm.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeWordBreakClass GetWordBreakClass(int codePoint)
    {
        string name = LookupBoundaryName(codePoint, UnicodeBoundaryTables.WordBreaks(), UnicodeBoundaryTables.WordBreakNames(), "");
        if (name.Length > 0)
        {
            return WordBreakClassFromName(name);
        }

        if (HasBoundaryProperty(codePoint, UnicodeBoundaryTables.EmojiBoundaryProperties(), UnicodeBoundaryTables.EmojiBoundaryPropertyNames(), "Extended_Pictographic"))
        {
            return UnicodeWordBreakClass.ExtendedPictographic;
        }

        return UnicodeWordBreakClass.Other;
    }

    /// <summary>
    /// Returns the boundary class used by the default sentence algorithm.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeSentenceBreakClass GetSentenceBreakClass(int codePoint)
    {
        string name = LookupBoundaryName(codePoint, UnicodeBoundaryTables.SentenceBreaks(), UnicodeBoundaryTables.SentenceBreakNames(), "");
        if (name.Length > 0)
        {
            return SentenceBreakClassFromName(name);
        }

        return UnicodeSentenceBreakClass.Other;
    }

    /// <summary>
    /// Returns the Unicode line-breaking class used by this library.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting classification or status.</returns>
    public UnicodeLineBreakClass GetLineBreakClass(int codePoint)
    {
        string name = LookupBoundaryName(codePoint, UnicodeBoundaryTables.LineBreaks(), UnicodeBoundaryTables.LineBreakNames(), "");
        if (name.Length > 0)
        {
            return LineBreakClassFromName(name);
        }

        return UnicodeLineBreakClass.Unknown;
    }

    internal static bool ShouldBreakGrapheme(UnicodeScalarRecord[] scalars, UnicodeGraphemeBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(classes);

        if (index <= 0)
        {
            return false;
        }

        if (index >= classes.Length)
        {
            return true;
        }

        UnicodeGraphemeBreakClass previous = classes[index - 1];
        UnicodeGraphemeBreakClass current = classes[index];

        if (previous == UnicodeGraphemeBreakClass.Cr)
        {
            if (current == UnicodeGraphemeBreakClass.Lf)
            {
                return false;
            }
        }

        if (IsGraphemeBreakControl(previous))
        {
            return true;
        }

        if (IsGraphemeBreakControl(current))
        {
            return true;
        }

        if (previous == UnicodeGraphemeBreakClass.L)
        {
            if (IsHangulAfterLeading(current))
            {
                return false;
            }
        }

        if (previous == UnicodeGraphemeBreakClass.Lv)
        {
            if (IsHangulAfterLvOrV(current))
            {
                return false;
            }
        }

        if (previous == UnicodeGraphemeBreakClass.V)
        {
            if (IsHangulAfterLvOrV(current))
            {
                return false;
            }
        }

        if (previous == UnicodeGraphemeBreakClass.Lvt)
        {
            if (current == UnicodeGraphemeBreakClass.T)
            {
                return false;
            }
        }

        if (previous == UnicodeGraphemeBreakClass.T)
        {
            if (current == UnicodeGraphemeBreakClass.T)
            {
                return false;
            }
        }

        if (current == UnicodeGraphemeBreakClass.Extend)
        {
            return false;
        }

        if (current == UnicodeGraphemeBreakClass.Zwj)
        {
            return false;
        }

        if (current == UnicodeGraphemeBreakClass.SpacingMark)
        {
            return false;
        }

        if (previous == UnicodeGraphemeBreakClass.Prepend)
        {
            return false;
        }

        if (IsIndicConjunctSequence(scalars, index))
        {
            return false;
        }

        if (IsEmojiZwjSequence(classes, index))
        {
            return false;
        }

        if (current == UnicodeGraphemeBreakClass.RegionalIndicator)
        {
            if (previous == UnicodeGraphemeBreakClass.RegionalIndicator)
            {
                if (CountPreviousRegionalIndicators(classes, index) % 2 == 1)
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal static bool ShouldBreakWord(UnicodeScalarRecord[] scalars, UnicodeWordBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(classes);

        if (index <= 0)
        {
            return false;
        }

        if (index >= classes.Length)
        {
            return true;
        }

        UnicodeWordBreakClass immediatePrevious = classes[index - 1];
        UnicodeWordBreakClass current = classes[index];

        if (immediatePrevious == UnicodeWordBreakClass.Cr)
        {
            if (current == UnicodeWordBreakClass.Lf)
            {
                return false;
            }
        }

        if (IsWordBreakControl(immediatePrevious))
        {
            return true;
        }

        if (IsWordBreakControl(current))
        {
            return true;
        }

        if (immediatePrevious == UnicodeWordBreakClass.Zwj)
        {
            if (UnicodeProperties.Instance.HasBinaryProperty(scalars[index].CodePoint, UnicodeBinaryProperty.ExtendedPictographic))
            {
                return false;
            }
        }

        if (IsWordIgnored(current))
        {
            return false;
        }

        int previousIndex = FindPreviousSignificantWordIndex(classes, index);
        if (previousIndex < 0)
        {
            return true;
        }

        UnicodeWordBreakClass previous = classes[previousIndex];

        if (immediatePrevious == UnicodeWordBreakClass.WSegSpace)
        {
            if (current == UnicodeWordBreakClass.WSegSpace)
            {
                return false;
            }
        }

        if (IsWordLetter(previous))
        {
            if (IsWordLetter(current))
            {
                return false;
            }
        }

        if (IsWordLetter(previous))
        {
            if (current == UnicodeWordBreakClass.Numeric)
            {
                return false;
            }
        }

        if (previous == UnicodeWordBreakClass.Numeric)
        {
            if (IsWordLetter(current))
            {
                return false;
            }
        }

        if (previous == UnicodeWordBreakClass.Numeric)
        {
            if (current == UnicodeWordBreakClass.Numeric)
            {
                return false;
            }
        }

        if (previous == UnicodeWordBreakClass.HebrewLetter)
        {
            if (current == UnicodeWordBreakClass.SingleQuote)
            {
                return false;
            }
        }

        if (IsWordLetter(previous))
        {
            if (IsWordMidLetter(current))
            {
                int nextIndex = FindNextSignificantWordIndex(classes, index + 1);
                if (nextIndex >= 0)
                {
                    if (IsWordLetter(classes[nextIndex]))
                    {
                        return false;
                    }
                }
            }
        }

        if (IsWordMidLetter(previous))
        {
            if (IsWordLetter(current))
            {
                int beforeMidIndex = FindPreviousSignificantWordIndex(classes, previousIndex);
                if (beforeMidIndex >= 0)
                {
                    if (IsWordLetter(classes[beforeMidIndex]))
                    {
                        return false;
                    }
                }
            }
        }

        if (previous == UnicodeWordBreakClass.HebrewLetter)
        {
            if (current == UnicodeWordBreakClass.DoubleQuote)
            {
                int nextHebrewIndex = FindNextSignificantWordIndex(classes, index + 1);
                if (nextHebrewIndex >= 0)
                {
                    if (classes[nextHebrewIndex] == UnicodeWordBreakClass.HebrewLetter)
                    {
                        return false;
                    }
                }
            }
        }

        if (previous == UnicodeWordBreakClass.DoubleQuote)
        {
            if (current == UnicodeWordBreakClass.HebrewLetter)
            {
                int beforeQuoteIndex = FindPreviousSignificantWordIndex(classes, previousIndex);
                if (beforeQuoteIndex >= 0)
                {
                    if (classes[beforeQuoteIndex] == UnicodeWordBreakClass.HebrewLetter)
                    {
                        return false;
                    }
                }
            }
        }

        if (previous == UnicodeWordBreakClass.Numeric)
        {
            if (IsWordMidNumber(current))
            {
                int nextNumberIndex = FindNextSignificantWordIndex(classes, index + 1);
                if (nextNumberIndex >= 0)
                {
                    if (classes[nextNumberIndex] == UnicodeWordBreakClass.Numeric)
                    {
                        return false;
                    }
                }
            }
        }

        if (IsWordMidNumber(previous))
        {
            if (current == UnicodeWordBreakClass.Numeric)
            {
                int beforeMidIndex = FindPreviousSignificantWordIndex(classes, previousIndex);
                if (beforeMidIndex >= 0)
                {
                    if (classes[beforeMidIndex] == UnicodeWordBreakClass.Numeric)
                    {
                        return false;
                    }
                }
            }
        }

        if (previous == UnicodeWordBreakClass.Katakana)
        {
            if (current == UnicodeWordBreakClass.Katakana)
            {
                return false;
            }
        }

        if (IsWordExtendNumLetLeft(previous))
        {
            if (current == UnicodeWordBreakClass.ExtendNumLet)
            {
                return false;
            }
        }

        if (previous == UnicodeWordBreakClass.ExtendNumLet)
        {
            if (IsWordExtendNumLetRight(current))
            {
                return false;
            }
        }

        if (current == UnicodeWordBreakClass.RegionalIndicator)
        {
            if (previous == UnicodeWordBreakClass.RegionalIndicator)
            {
                if (CountPreviousWordRegionalIndicators(classes, index) % 2 == 1)
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal static bool ShouldBreakSentence(UnicodeScalarRecord[] scalars, UnicodeSentenceBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(classes);

        if (index <= 0)
        {
            return false;
        }

        if (index >= classes.Length)
        {
            return true;
        }

        UnicodeSentenceBreakClass immediatePrevious = classes[index - 1];
        UnicodeSentenceBreakClass current = classes[index];

        if (immediatePrevious == UnicodeSentenceBreakClass.Cr)
        {
            if (current == UnicodeSentenceBreakClass.Lf)
            {
                return false;
            }
        }

        if (IsSentenceBreakSeparator(immediatePrevious))
        {
            return true;
        }

        if (IsSentenceIgnored(current))
        {
            return false;
        }

        int previousIndex = FindPreviousSignificantSentenceIndex(classes, index);
        if (previousIndex < 0)
        {
            return false;
        }

        UnicodeSentenceBreakClass previous = classes[previousIndex];

        if (previous == UnicodeSentenceBreakClass.ATerm)
        {
            if (current == UnicodeSentenceBreakClass.Numeric)
            {
                return false;
            }

            if (current == UnicodeSentenceBreakClass.Upper)
            {
                int beforeTermIndex = FindPreviousSignificantSentenceIndex(classes, previousIndex);
                if (beforeTermIndex >= 0)
                {
                    if (IsSentenceUpperOrLower(classes[beforeTermIndex]))
                    {
                        return false;
                    }
                }
            }
        }

        int terminalIndex = FindSentenceTerminalBefore(classes, index);
        if (terminalIndex >= 0)
        {
            if (classes[terminalIndex] == UnicodeSentenceBreakClass.ATerm)
            {
                if (HasSentenceLowerAhead(classes, index))
                {
                    return false;
                }
            }

            if (IsSentenceContinuation(current))
            {
                return false;
            }

            if (current == UnicodeSentenceBreakClass.Close)
            {
                if (!HasSentenceSpaceSinceTerminal(classes, index))
                {
                    return false;
                }
            }

            if (current == UnicodeSentenceBreakClass.Sp)
            {
                return false;
            }

            if (IsSentenceBreakSeparator(current))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    internal static bool ShouldAllowLineBreak(UnicodeScalarRecord[] scalars, UnicodeLineBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(classes);

        UnicodeLineBreakClass[] resolved = ResolveLineBreakClasses(scalars, classes);
        return GetLineBreakDecision(scalars, classes, resolved, index) != 0;
    }

    internal static bool IsIndicConjunctSequence(UnicodeScalarRecord[] scalars, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        if (index <= 0)
        {
            return false;
        }

        if (UnicodeProperties.Instance.GetIndicConjunctBreak(scalars[index].CodePoint) != UnicodeIndicConjunctBreak.Consonant)
        {
            return false;
        }

        bool foundLinker = false;
        int scanIndex = index - 1;
        while (scanIndex >= 0)
        {
            UnicodeIndicConjunctBreak value = UnicodeProperties.Instance.GetIndicConjunctBreak(scalars[scanIndex].CodePoint);
            if (value == UnicodeIndicConjunctBreak.Linker)
            {
                foundLinker = true;
                scanIndex = scanIndex - 1;
                continue;
            }

            if (value == UnicodeIndicConjunctBreak.Extend)
            {
                scanIndex = scanIndex - 1;
                continue;
            }

            if (value == UnicodeIndicConjunctBreak.Consonant)
            {
                return foundLinker;
            }

            return false;
        }

        return false;
    }

    internal static bool IsWordMidLetter(UnicodeWordBreakClass value)
    {
        if (value == UnicodeWordBreakClass.MidLetter)
        {
            return true;
        }

        if (value == UnicodeWordBreakClass.MidNumLet)
        {
            return true;
        }

        return value == UnicodeWordBreakClass.SingleQuote;
    }

    internal static bool IsWordMidNumber(UnicodeWordBreakClass value)
    {
        if (value == UnicodeWordBreakClass.MidNum)
        {
            return true;
        }

        if (value == UnicodeWordBreakClass.MidNumLet)
        {
            return true;
        }

        return value == UnicodeWordBreakClass.SingleQuote;
    }

    internal static bool IsWordExtendNumLetLeft(UnicodeWordBreakClass value)
    {
        if (IsWordLetter(value))
        {
            return true;
        }

        if (value == UnicodeWordBreakClass.Numeric)
        {
            return true;
        }

        if (value == UnicodeWordBreakClass.Katakana)
        {
            return true;
        }

        return value == UnicodeWordBreakClass.ExtendNumLet;
    }

    internal static bool IsWordExtendNumLetRight(UnicodeWordBreakClass value)
    {
        if (IsWordLetter(value))
        {
            return true;
        }

        if (value == UnicodeWordBreakClass.Numeric)
        {
            return true;
        }

        return value == UnicodeWordBreakClass.Katakana;
    }

    internal static bool IsSentenceUpperOrLower(UnicodeSentenceBreakClass value)
    {
        if (value == UnicodeSentenceBreakClass.Upper)
        {
            return true;
        }

        return value == UnicodeSentenceBreakClass.Lower;
    }

    internal static bool IsSentenceContinuation(UnicodeSentenceBreakClass value)
    {
        if (value == UnicodeSentenceBreakClass.SContinue)
        {
            return true;
        }

        if (value == UnicodeSentenceBreakClass.ATerm)
        {
            return true;
        }

        return value == UnicodeSentenceBreakClass.STerm;
    }

    internal static int FindSentenceTerminalBefore(UnicodeSentenceBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(classes);

        int scanIndex = FindPreviousSignificantSentenceIndex(classes, index);
        while (scanIndex >= 0)
        {
            UnicodeSentenceBreakClass value = classes[scanIndex];
            if (value == UnicodeSentenceBreakClass.Sp)
            {
                scanIndex = FindPreviousSignificantSentenceIndex(classes, scanIndex);
                continue;
            }

            break;
        }

        while (scanIndex >= 0)
        {
            if (classes[scanIndex] == UnicodeSentenceBreakClass.Close)
            {
                scanIndex = FindPreviousSignificantSentenceIndex(classes, scanIndex);
                continue;
            }

            break;
        }

        if (scanIndex >= 0)
        {
            if (classes[scanIndex] == UnicodeSentenceBreakClass.ATerm)
            {
                return scanIndex;
            }

            if (classes[scanIndex] == UnicodeSentenceBreakClass.STerm)
            {
                return scanIndex;
            }
        }

        return -1;
    }

    internal static bool HasSentenceLowerAhead(UnicodeSentenceBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(classes);

        int scanIndex = index;
        while (scanIndex < classes.Length)
        {
            UnicodeSentenceBreakClass value = classes[scanIndex];
            if (IsSentenceIgnored(value))
            {
                scanIndex = scanIndex + 1;
                continue;
            }

            if (value == UnicodeSentenceBreakClass.Lower)
            {
                return true;
            }

            if (value == UnicodeSentenceBreakClass.OLetter)
            {
                return false;
            }

            if (value == UnicodeSentenceBreakClass.Upper)
            {
                return false;
            }

            if (IsSentenceBreakSeparator(value))
            {
                return false;
            }

            if (value == UnicodeSentenceBreakClass.ATerm)
            {
                return false;
            }

            if (value == UnicodeSentenceBreakClass.STerm)
            {
                return false;
            }

            scanIndex = scanIndex + 1;
        }

        return false;
    }

    internal static bool HasSentenceSpaceSinceTerminal(UnicodeSentenceBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(classes);

        bool foundSpace = false;
        int scanIndex = index - 1;
        while (scanIndex >= 0)
        {
            UnicodeSentenceBreakClass value = classes[scanIndex];
            if (IsSentenceIgnored(value))
            {
                scanIndex = scanIndex - 1;
                continue;
            }

            if (value == UnicodeSentenceBreakClass.Sp)
            {
                foundSpace = true;
                scanIndex = scanIndex - 1;
                continue;
            }

            if (value == UnicodeSentenceBreakClass.Close)
            {
                scanIndex = scanIndex - 1;
                continue;
            }

            return foundSpace;
        }

        return foundSpace;
    }

    internal static UnicodeLineBreakClass[] ResolveLineBreakClasses(UnicodeScalarRecord[] scalars, UnicodeLineBreakClass[] originalClasses)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(originalClasses);

        UnicodeLineBreakClass[] classes = new UnicodeLineBreakClass[originalClasses.Length];
        int index = 0;
        while (index < classes.Length)
        {
            UnicodeLineBreakClass value = originalClasses[index];
            if (value == UnicodeLineBreakClass.Ambiguous)
            {
                value = UnicodeLineBreakClass.Alphabetic;
            }
            else if (value == UnicodeLineBreakClass.Surrogate)
            {
                value = UnicodeLineBreakClass.Alphabetic;
            }
            else if (value == UnicodeLineBreakClass.Unknown)
            {
                value = UnicodeLineBreakClass.Alphabetic;
            }
            else if (value == UnicodeLineBreakClass.ConditionalJapaneseStarter)
            {
                value = UnicodeLineBreakClass.Nonstarter;
            }
            else if (value == UnicodeLineBreakClass.ComplexContext)
            {
                UnicodeGeneralCategory category = UnicodeProperties.Instance.GetGeneralCategory(scalars[index].CodePoint);
                if (category == UnicodeGeneralCategory.NonspacingMark)
                {
                    value = UnicodeLineBreakClass.CombiningMark;
                }
                else if (category == UnicodeGeneralCategory.SpacingMark)
                {
                    value = UnicodeLineBreakClass.CombiningMark;
                }
                else
                {
                    value = UnicodeLineBreakClass.Alphabetic;
                }
            }

            classes[index] = value;
            index = index + 1;
        }

        index = 0;
        while (index < classes.Length)
        {
            UnicodeLineBreakClass original = originalClasses[index];
            if (IsOriginalLineCombiningMark(scalars[index].CodePoint, original) || original == UnicodeLineBreakClass.ZeroWidthJoiner)
            {
                if (index == 0)
                {
                    classes[index] = UnicodeLineBreakClass.Alphabetic;
                }
                else if (IsLineCombiningBaseExcluded(originalClasses[index - 1]))
                {
                    classes[index] = UnicodeLineBreakClass.Alphabetic;
                }
                else
                {
                    classes[index] = classes[index - 1];
                }
            }

            index = index + 1;
        }

        return classes;
    }

    internal static bool IsLineCombiningBaseExcluded(UnicodeLineBreakClass value)
    {
        if (IsLineHardBreak(value))
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.Space)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.ZeroWidthSpace;
    }

    internal static bool IsLineHardBreak(UnicodeLineBreakClass value)
    {
        if (value == UnicodeLineBreakClass.MandatoryBreak)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.CarriageReturn)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.LineFeed)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.NextLine;
    }

    internal static int GetLineBreakDecision(UnicodeScalarRecord[] scalars, UnicodeLineBreakClass[] original, UnicodeLineBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(original);
        System.ArgumentNullException.ThrowIfNull(classes);

        if (index <= 0)
        {
            return 0;
        }

        if (index >= classes.Length)
        {
            return 2;
        }

        UnicodeLineBreakClass previous = classes[index - 1];
        UnicodeLineBreakClass current = classes[index];
        UnicodeLineBreakClass originalPrevious = original[index - 1];
        UnicodeLineBreakClass originalCurrent = original[index];

        if (originalPrevious == UnicodeLineBreakClass.CarriageReturn && originalCurrent == UnicodeLineBreakClass.LineFeed)
        {
            return 0;
        }

        if (IsLineHardBreak(originalPrevious))
        {
            return 2;
        }

        if (IsLineHardBreak(originalCurrent))
        {
            return 0;
        }

        if (originalCurrent == UnicodeLineBreakClass.Space || originalCurrent == UnicodeLineBreakClass.ZeroWidthSpace)
        {
            return 0;
        }

        int previousNonSpace = FindPreviousNonSpaceLineIndex(classes, index);
        if (previousNonSpace >= 0)
        {
            if (classes[previousNonSpace] == UnicodeLineBreakClass.ZeroWidthSpace)
            {
                return 1;
            }
        }

        if (originalPrevious == UnicodeLineBreakClass.ZeroWidthJoiner)
        {
            return 0;
        }

        if (IsOriginalLineCombiningMark(scalars[index].CodePoint, originalCurrent) || originalCurrent == UnicodeLineBreakClass.ZeroWidthJoiner)
        {
            if (!IsLineCombiningBaseExcluded(originalPrevious))
            {
                return 0;
            }
        }

        if (previous == UnicodeLineBreakClass.WordJoiner || current == UnicodeLineBreakClass.WordJoiner)
        {
            return 0;
        }

        if (previous == UnicodeLineBreakClass.Glue)
        {
            return 0;
        }

        if (current == UnicodeLineBreakClass.Glue)
        {
            if (!IsLineBeforeGlueException(previous))
            {
                return 0;
            }
        }

        if (IsLineClosingClass(current))
        {
            return 0;
        }

        if (previousNonSpace >= 0)
        {
            if (classes[previousNonSpace] == UnicodeLineBreakClass.OpeningPunctuation)
            {
                return 0;
            }

            if (IsLineCloseOrParenthesis(classes[previousNonSpace]))
            {
                if (current == UnicodeLineBreakClass.Nonstarter)
                {
                    return 0;
                }
            }

            if (classes[previousNonSpace] == UnicodeLineBreakClass.BreakBoth)
            {
                if (current == UnicodeLineBreakClass.BreakBoth)
                {
                    return 0;
                }
            }
        }

        if (current == UnicodeLineBreakClass.Quotation)
        {
            UnicodeGeneralCategory quoteCategory = UnicodeProperties.Instance.GetGeneralCategory(scalars[index].CodePoint);
            if (quoteCategory == UnicodeGeneralCategory.FinalPunctuation)
            {
                if (index + 1 >= classes.Length)
                {
                    return 0;
                }

                if (IsLineFinalQuoteFollowingClass(classes[index + 1]))
                {
                    return 0;
                }
            }
        }

        if (IsLineInitialQuoteSpaceContext(scalars, classes, index))
        {
            return 0;
        }

        if (previous == UnicodeLineBreakClass.Space)
        {
            if (current == UnicodeLineBreakClass.InfixNumeric)
            {
                if (index + 1 < classes.Length)
                {
                    if (classes[index + 1] == UnicodeLineBreakClass.Numeric)
                    {
                        return 1;
                    }
                }

                return 0;
            }

            return 1;
        }

        if (IsLineQuoteProhibited(scalars, classes, index))
        {
            return 0;
        }

        if (current == UnicodeLineBreakClass.InfixNumeric)
        {
            return 0;
        }

        if (previous == UnicodeLineBreakClass.ContingentBreak || current == UnicodeLineBreakClass.ContingentBreak)
        {
            return 1;
        }

        if (IsLineInitialHyphenContext(scalars, classes, index))
        {
            return 0;
        }

        if (current == UnicodeLineBreakClass.BreakAfter || current == UnicodeLineBreakClass.UnambiguousHyphen || current == UnicodeLineBreakClass.Hyphen || current == UnicodeLineBreakClass.Nonstarter)
        {
            return 0;
        }

        if (previous == UnicodeLineBreakClass.BreakBefore)
        {
            return 0;
        }

        if (IsLineHebrewHyphenContext(classes, index))
        {
            return 0;
        }

        if (previous == UnicodeLineBreakClass.BreakSymbols && current == UnicodeLineBreakClass.HebrewLetter)
        {
            return 0;
        }

        if (current == UnicodeLineBreakClass.Inseparable)
        {
            return 0;
        }

        if (IsLineAlphaNumericPair(previous, current))
        {
            return 0;
        }

        if (IsLinePrefixIdeographPair(previous, current))
        {
            return 0;
        }

        if (IsLinePrefixAlphabeticPair(previous, current))
        {
            return 0;
        }

        if (IsLineNumericExpression(classes, index))
        {
            return 0;
        }

        if (IsLineHangulPair(previous, current))
        {
            return 0;
        }

        if (IsLineHangulNumericPair(previous, current))
        {
            return 0;
        }

        if (IsLineAlphabeticPair(previous, current))
        {
            return 0;
        }

        if (IsLineAksaraPair(scalars, classes, index))
        {
            return 0;
        }

        if (previous == UnicodeLineBreakClass.InfixNumeric)
        {
            if (IsLineAlphabetic(current))
            {
                return 0;
            }
        }

        if (IsLineNonEastAsianParenthesisPair(scalars, classes, index))
        {
            return 0;
        }

        if (current == UnicodeLineBreakClass.RegionalIndicator)
        {
            if (previous == UnicodeLineBreakClass.RegionalIndicator)
            {
                if (CountPreviousLineRegionalIndicators(scalars, classes, index) % 2 == 1)
                {
                    return 0;
                }
            }
        }

        if (current == UnicodeLineBreakClass.EmojiModifier)
        {
            if (previous == UnicodeLineBreakClass.EmojiBase)
            {
                return 0;
            }

            int emojiBaseIndex = FindPreviousLineBaseScalarIndex(scalars, index);
            if (emojiBaseIndex >= 0)
            {
                if (UnicodeProperties.Instance.HasBinaryProperty(scalars[emojiBaseIndex].CodePoint, UnicodeBinaryProperty.ExtendedPictographic))
                {
                    if (UnicodeProperties.Instance.GetGeneralCategory(scalars[emojiBaseIndex].CodePoint) == UnicodeGeneralCategory.OtherNotAssigned)
                    {
                        return 0;
                    }
                }
            }
        }

        return 1;
    }

    internal static bool IsOriginalLineCombiningMark(int codePoint, UnicodeLineBreakClass value)
    {
        if (value == UnicodeLineBreakClass.CombiningMark)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.ComplexContext)
        {
            UnicodeGeneralCategory category = UnicodeProperties.Instance.GetGeneralCategory(codePoint);
            if (category == UnicodeGeneralCategory.NonspacingMark)
            {
                return true;
            }

            return category == UnicodeGeneralCategory.SpacingMark;
        }

        return false;
    }

    internal static int FindPreviousNonSpaceLineIndex(UnicodeLineBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(classes);

        int scanIndex = index - 1;
        while (scanIndex >= 0)
        {
            if (classes[scanIndex] != UnicodeLineBreakClass.Space)
            {
                return scanIndex;
            }

            scanIndex = scanIndex - 1;
        }

        return -1;
    }

    internal static bool IsLineBeforeGlueException(UnicodeLineBreakClass value)
    {
        if (value == UnicodeLineBreakClass.Space)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.BreakAfter)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.Hyphen)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.UnambiguousHyphen;
    }

    internal static bool IsLineFinalQuoteFollowingClass(UnicodeLineBreakClass value)
    {
        if (value == UnicodeLineBreakClass.Space || value == UnicodeLineBreakClass.Glue || value == UnicodeLineBreakClass.WordJoiner)
        {
            return true;
        }

        if (IsLineClosingClass(value) || value == UnicodeLineBreakClass.Quotation || value == UnicodeLineBreakClass.InfixNumeric)
        {
            return true;
        }

        if (IsLineHardBreak(value) || value == UnicodeLineBreakClass.ZeroWidthSpace)
        {
            return true;
        }

        return false;
    }

    internal static bool IsLineClosingClass(UnicodeLineBreakClass value)
    {
        if (value == UnicodeLineBreakClass.ClosingPunctuation)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.ClosingParenthesis)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.Exclamation)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.BreakSymbols;
    }

    internal static bool IsLineCloseOrParenthesis(UnicodeLineBreakClass value)
    {
        if (value == UnicodeLineBreakClass.ClosingPunctuation)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.ClosingParenthesis;
    }

    internal static bool IsLineQuoteProhibited(UnicodeScalarRecord[] scalars, UnicodeLineBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(classes);

        UnicodeLineBreakClass previous = classes[index - 1];
        UnicodeLineBreakClass current = classes[index];
        if (current == UnicodeLineBreakClass.Quotation)
        {
            UnicodeGeneralCategory category = UnicodeProperties.Instance.GetGeneralCategory(scalars[index].CodePoint);
            if (category != UnicodeGeneralCategory.InitialPunctuation)
            {
                return true;
            }

            if (!IsEastAsianLineScalar(scalars[index - 1].CodePoint))
            {
                return true;
            }

            if (index + 1 >= classes.Length)
            {
                return true;
            }

            if (!IsEastAsianLineScalar(scalars[index + 1].CodePoint))
            {
                return true;
            }
        }

        if (previous == UnicodeLineBreakClass.Quotation)
        {
            UnicodeGeneralCategory category = UnicodeProperties.Instance.GetGeneralCategory(scalars[index - 1].CodePoint);
            if (category != UnicodeGeneralCategory.FinalPunctuation)
            {
                return true;
            }

            int beforeQuote = index - 2;
            if (beforeQuote < 0)
            {
                return true;
            }

            if (!IsEastAsianLineScalar(scalars[beforeQuote].CodePoint))
            {
                return true;
            }
        }

        int quoteIndex = FindPreviousNonSpaceLineIndex(classes, index);
        if (quoteIndex >= 0)
        {
            if (classes[quoteIndex] == UnicodeLineBreakClass.Quotation)
            {
                UnicodeGeneralCategory quoteCategory = UnicodeProperties.Instance.GetGeneralCategory(scalars[quoteIndex].CodePoint);
                if (quoteCategory == UnicodeGeneralCategory.InitialPunctuation)
                {
                    int beforeQuote = quoteIndex - 1;
                    if (beforeQuote < 0)
                    {
                        return true;
                    }

                    if (IsLineInitialQuoteContext(classes[beforeQuote]))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    internal static bool IsLineInitialQuoteSpaceContext(UnicodeScalarRecord[] scalars, UnicodeLineBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(classes);

        int quoteIndex = FindPreviousNonSpaceLineIndex(classes, index);
        if (quoteIndex < 0)
        {
            return false;
        }

        if (classes[quoteIndex] != UnicodeLineBreakClass.Quotation)
        {
            return false;
        }

        int quoteBaseIndex = FindPreviousLineBaseScalarIndex(scalars, quoteIndex + 1);
        if (quoteBaseIndex < 0)
        {
            return false;
        }

        UnicodeGeneralCategory category = UnicodeProperties.Instance.GetGeneralCategory(scalars[quoteBaseIndex].CodePoint);
        if (category != UnicodeGeneralCategory.InitialPunctuation)
        {
            return false;
        }

        int beforeQuote = quoteBaseIndex - 1;
        if (beforeQuote < 0)
        {
            return true;
        }

        return IsLineInitialQuoteContext(classes[beforeQuote]);
    }

    internal static bool IsLineInitialQuoteContext(UnicodeLineBreakClass value)
    {
        if (IsLineHardBreak(value))
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.OpeningPunctuation)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.Quotation)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.Glue)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.Space)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.ZeroWidthSpace;
    }

    internal static bool IsEastAsianLineScalar(int codePoint)
    {
        UnicodeEastAsianWidth width = UnicodeProperties.Instance.GetEastAsianWidth(codePoint);
        if (width == UnicodeEastAsianWidth.Fullwidth)
        {
            return true;
        }

        if (width == UnicodeEastAsianWidth.Wide)
        {
            return true;
        }

        return width == UnicodeEastAsianWidth.Halfwidth;
    }

    internal static bool IsLineInitialHyphenContext(UnicodeScalarRecord[] scalars, UnicodeLineBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(classes);

        int hyphenIndex = FindPreviousLineBaseScalarIndex(scalars, index);
        if (hyphenIndex < 0)
        {
            return false;
        }

        UnicodeLineBreakClass previous = classes[hyphenIndex];
        UnicodeLineBreakClass current = classes[index];
        if (previous != UnicodeLineBreakClass.Hyphen && previous != UnicodeLineBreakClass.UnambiguousHyphen)
        {
            return false;
        }

        if (!IsLineAlphabetic(current))
        {
            return false;
        }

        int before = hyphenIndex - 1;
        if (before < 0)
        {
            return true;
        }

        UnicodeLineBreakClass value = classes[before];
        if (IsLineHardBreak(value))
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.Space)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.ZeroWidthSpace)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.ContingentBreak)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.Glue;
    }

    internal static int FindPreviousLineBaseScalarIndex(UnicodeScalarRecord[] scalars, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        int scanIndex = index - 1;
        while (scanIndex >= 0)
        {
            int codePoint = scalars[scanIndex].CodePoint;
            UnicodeLineBreakClass value = UnicodeSegments.Instance.GetLineBreakClass(codePoint);
            if (IsOriginalLineCombiningMark(codePoint, value))
            {
                scanIndex = scanIndex - 1;
                continue;
            }

            if (value == UnicodeLineBreakClass.ZeroWidthJoiner)
            {
                scanIndex = scanIndex - 1;
                continue;
            }

            return scanIndex;
        }

        return -1;
    }

    internal static bool IsLineHebrewHyphenContext(UnicodeLineBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(classes);

        UnicodeLineBreakClass previous = classes[index - 1];
        if (previous != UnicodeLineBreakClass.Hyphen && previous != UnicodeLineBreakClass.UnambiguousHyphen)
        {
            return false;
        }

        if (index < 2)
        {
            return false;
        }

        return classes[index - 2] == UnicodeLineBreakClass.HebrewLetter && classes[index] != UnicodeLineBreakClass.HebrewLetter;
    }

    internal static bool IsLineAlphabetic(UnicodeLineBreakClass value)
    {
        if (value == UnicodeLineBreakClass.Alphabetic)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.HebrewLetter;
    }

    internal static bool IsLineAlphaNumericPair(UnicodeLineBreakClass previous, UnicodeLineBreakClass current)
    {
        if (IsLineAlphabetic(previous) && current == UnicodeLineBreakClass.Numeric)
        {
            return true;
        }

        return previous == UnicodeLineBreakClass.Numeric && IsLineAlphabetic(current);
    }

    internal static bool IsLineIdeographOrEmoji(UnicodeLineBreakClass value)
    {
        if (value == UnicodeLineBreakClass.Ideographic)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.EmojiBase)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.EmojiModifier;
    }

    internal static bool IsLinePrefixIdeographPair(UnicodeLineBreakClass previous, UnicodeLineBreakClass current)
    {
        if (previous == UnicodeLineBreakClass.PrefixNumeric && IsLineIdeographOrEmoji(current))
        {
            return true;
        }

        return IsLineIdeographOrEmoji(previous) && current == UnicodeLineBreakClass.PostfixNumeric;
    }

    internal static bool IsLinePrefixAlphabeticPair(UnicodeLineBreakClass previous, UnicodeLineBreakClass current)
    {
        if (previous == UnicodeLineBreakClass.PrefixNumeric || previous == UnicodeLineBreakClass.PostfixNumeric)
        {
            if (IsLineAlphabetic(current))
            {
                return true;
            }
        }

        if (IsLineAlphabetic(previous))
        {
            if (current == UnicodeLineBreakClass.PrefixNumeric || current == UnicodeLineBreakClass.PostfixNumeric)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsLineNumericExpression(UnicodeLineBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(classes);

        UnicodeLineBreakClass previous = classes[index - 1];
        UnicodeLineBreakClass current = classes[index];
        if (current == UnicodeLineBreakClass.Numeric)
        {
            if (previous == UnicodeLineBreakClass.Hyphen || previous == UnicodeLineBreakClass.InfixNumeric)
            {
                return true;
            }

            if (previous == UnicodeLineBreakClass.PrefixNumeric || previous == UnicodeLineBreakClass.PostfixNumeric)
            {
                return true;
            }

            int scanIndex = index - 1;
            while (scanIndex >= 0)
            {
                UnicodeLineBreakClass value = classes[scanIndex];
                if (value == UnicodeLineBreakClass.BreakSymbols || value == UnicodeLineBreakClass.InfixNumeric)
                {
                    scanIndex = scanIndex - 1;
                    continue;
                }

                return value == UnicodeLineBreakClass.Numeric;
            }
        }

        if (current == UnicodeLineBreakClass.PrefixNumeric || current == UnicodeLineBreakClass.PostfixNumeric)
        {
            int scanIndex = index - 1;
            if (classes[scanIndex] == UnicodeLineBreakClass.ClosingPunctuation || classes[scanIndex] == UnicodeLineBreakClass.ClosingParenthesis)
            {
                scanIndex = scanIndex - 1;
            }

            while (scanIndex >= 0)
            {
                UnicodeLineBreakClass value = classes[scanIndex];
                if (value == UnicodeLineBreakClass.BreakSymbols || value == UnicodeLineBreakClass.InfixNumeric)
                {
                    scanIndex = scanIndex - 1;
                    continue;
                }

                return value == UnicodeLineBreakClass.Numeric;
            }
        }

        if (previous == UnicodeLineBreakClass.PrefixNumeric || previous == UnicodeLineBreakClass.PostfixNumeric)
        {
            if (current == UnicodeLineBreakClass.OpeningPunctuation)
            {
                int next = index + 1;
                if (next < classes.Length)
                {
                    if (classes[next] == UnicodeLineBreakClass.InfixNumeric)
                    {
                        next = next + 1;
                    }

                    if (next < classes.Length)
                    {
                        return classes[next] == UnicodeLineBreakClass.Numeric;
                    }
                }
            }
        }

        return false;
    }

    internal static bool IsLineHangulPair(UnicodeLineBreakClass previous, UnicodeLineBreakClass current)
    {
        if (previous == UnicodeLineBreakClass.HangulLeadingJamo)
        {
            if (current == UnicodeLineBreakClass.HangulLeadingJamo || current == UnicodeLineBreakClass.HangulVowelJamo || current == UnicodeLineBreakClass.HangulLvSyllable || current == UnicodeLineBreakClass.HangulLvtSyllable)
            {
                return true;
            }
        }

        if (previous == UnicodeLineBreakClass.HangulVowelJamo || previous == UnicodeLineBreakClass.HangulLvSyllable)
        {
            if (current == UnicodeLineBreakClass.HangulVowelJamo || current == UnicodeLineBreakClass.HangulTrailingJamo)
            {
                return true;
            }
        }

        if (previous == UnicodeLineBreakClass.HangulTrailingJamo || previous == UnicodeLineBreakClass.HangulLvtSyllable)
        {
            return current == UnicodeLineBreakClass.HangulTrailingJamo;
        }

        return false;
    }

    internal static bool IsLineKorean(UnicodeLineBreakClass value)
    {
        if (value == UnicodeLineBreakClass.HangulLeadingJamo)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.HangulVowelJamo)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.HangulTrailingJamo)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.HangulLvSyllable)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.HangulLvtSyllable;
    }

    internal static bool IsLineHangulNumericPair(UnicodeLineBreakClass previous, UnicodeLineBreakClass current)
    {
        if (IsLineKorean(previous) && current == UnicodeLineBreakClass.PostfixNumeric)
        {
            return true;
        }

        return previous == UnicodeLineBreakClass.PrefixNumeric && IsLineKorean(current);
    }

    internal static bool IsLineAlphabeticPair(UnicodeLineBreakClass previous, UnicodeLineBreakClass current)
    {
        return IsLineAlphabetic(previous) && IsLineAlphabetic(current);
    }

    internal static bool IsLineAksaraBase(UnicodeLineBreakClass value)
    {
        if (value == UnicodeLineBreakClass.Aksara)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.AksaraStart;
    }

    internal static bool IsLineAksaraPair(UnicodeScalarRecord[] scalars, UnicodeLineBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(classes);

        UnicodeLineBreakClass previous = classes[index - 1];
        UnicodeLineBreakClass current = classes[index];
        bool currentBase = IsLineAksaraBase(current) || scalars[index].CodePoint == 0x25CC;
        int previousBaseIndex = FindPreviousAksaraBaseIndex(scalars, index);
        bool previousBase = previousBaseIndex >= 0;

        if (previous == UnicodeLineBreakClass.AksaraPrebase && currentBase)
        {
            return true;
        }

        if (previousBase)
        {
            if (current == UnicodeLineBreakClass.ViramaFinal || current == UnicodeLineBreakClass.Virama)
            {
                return true;
            }
        }

        if (previous == UnicodeLineBreakClass.Virama && currentBase)
        {
            int viramaIndex = FindPreviousLineBaseScalarIndex(scalars, index);
            if (viramaIndex > 0)
            {
                int baseIndex = FindPreviousLineBaseScalarIndex(scalars, viramaIndex);
                if (baseIndex >= 0)
                {
                    UnicodeLineBreakClass before = classes[baseIndex];
                    if (IsLineAksaraBase(before) || scalars[baseIndex].CodePoint == 0x25CC)
                    {
                        return true;
                    }
                }
            }
        }

        if (previousBase && currentBase)
        {
            if (index + 1 < classes.Length)
            {
                return classes[index + 1] == UnicodeLineBreakClass.ViramaFinal;
            }
        }

        return false;
    }

    internal static int FindPreviousAksaraBaseIndex(UnicodeScalarRecord[] scalars, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        int scanIndex = index - 1;
        while (scanIndex >= 0)
        {
            int codePoint = scalars[scanIndex].CodePoint;
            UnicodeLineBreakClass value = UnicodeSegments.Instance.GetLineBreakClass(codePoint);
            if (IsOriginalLineCombiningMark(codePoint, value))
            {
                scanIndex = scanIndex - 1;
                continue;
            }

            if (value == UnicodeLineBreakClass.Aksara || value == UnicodeLineBreakClass.AksaraStart || codePoint == 0x25CC)
            {
                return scanIndex;
            }

            return -1;
        }

        return -1;
    }

    internal static bool IsLineNonEastAsianParenthesisPair(UnicodeScalarRecord[] scalars, UnicodeLineBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(classes);

        UnicodeLineBreakClass previous = classes[index - 1];
        UnicodeLineBreakClass current = classes[index];
        if (current == UnicodeLineBreakClass.OpeningPunctuation)
        {
            if (!IsEastAsianLineScalar(scalars[index].CodePoint))
            {
                if (IsLineAlphabetic(previous) || previous == UnicodeLineBreakClass.Numeric)
                {
                    return true;
                }
            }
        }

        if (previous == UnicodeLineBreakClass.ClosingParenthesis)
        {
            if (!IsEastAsianLineScalar(scalars[index - 1].CodePoint))
            {
                if (IsLineAlphabetic(current) || current == UnicodeLineBreakClass.Numeric)
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static int CountPreviousLineRegionalIndicators(UnicodeScalarRecord[] scalars, UnicodeLineBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(classes);

        int count = 0;
        int scanIndex = index - 1;
        while (scanIndex >= 0)
        {
            int codePoint = scalars[scanIndex].CodePoint;
            UnicodeLineBreakClass original = UnicodeSegments.Instance.GetLineBreakClass(codePoint);
            if (IsOriginalLineCombiningMark(codePoint, original) || original == UnicodeLineBreakClass.ZeroWidthJoiner)
            {
                scanIndex = scanIndex - 1;
                continue;
            }

            if (classes[scanIndex] != UnicodeLineBreakClass.RegionalIndicator)
            {
                break;
            }

            count = count + 1;
            scanIndex = scanIndex - 1;
        }

        return count;
    }

    internal static int GetSegmentDisplayWidth(string text, UnicodeSegmentRecord segment)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(segment);

        string cluster = text.Substring(segment.StartUtf16Index, segment.Utf16Length);
        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(cluster);
        bool hasVisible = false;
        bool hasWide = false;
        bool hasEmojiPresentation = false;
        bool hasExtendedPictographic = false;
        bool hasEmojiVariation = false;
        int index = 0;

        while (index < scalars.Length)
        {
            int codePoint = scalars[index].CodePoint;
            UnicodeGeneralCategory category = UnicodeProperties.Instance.GetGeneralCategory(codePoint);
            UnicodeEastAsianWidth eastAsianWidth = UnicodeProperties.Instance.GetEastAsianWidth(codePoint);

            if (eastAsianWidth == UnicodeEastAsianWidth.Wide || eastAsianWidth == UnicodeEastAsianWidth.Fullwidth)
            {
                hasWide = true;
            }

            if (UnicodeProperties.Instance.HasBinaryProperty(codePoint, UnicodeBinaryProperty.EmojiPresentation))
            {
                hasEmojiPresentation = true;
            }

            if (UnicodeProperties.Instance.HasBinaryProperty(codePoint, UnicodeBinaryProperty.ExtendedPictographic))
            {
                hasExtendedPictographic = true;
            }

            if (codePoint == 0xFE0F)
            {
                hasEmojiVariation = true;
            }

            if (!IsZeroWidthDisplayScalar(codePoint, category))
            {
                hasVisible = true;
            }

            index = index + 1;
        }

        if (!hasVisible)
        {
            return 0;
        }

        if (hasWide || hasEmojiPresentation)
        {
            return 2;
        }

        if (hasExtendedPictographic && hasEmojiVariation)
        {
            return 2;
        }

        return 1;
    }

    internal static bool IsZeroWidthDisplayScalar(int codePoint, UnicodeGeneralCategory category)
    {
        if (category == UnicodeGeneralCategory.NonspacingMark)
        {
            return true;
        }

        if (category == UnicodeGeneralCategory.EnclosingMark)
        {
            return true;
        }

        if (category == UnicodeGeneralCategory.Format)
        {
            return true;
        }

        if (category == UnicodeGeneralCategory.Control)
        {
            return true;
        }

        if (UnicodeProperties.Instance.HasBinaryProperty(codePoint, UnicodeBinaryProperty.DefaultIgnorableCodePoint))
        {
            return true;
        }

        return false;
    }

    internal static bool IsEmojiZwjSequence(UnicodeGraphemeBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(classes);

        if (index < 2)
        {
            return false;
        }

        if (classes[index] != UnicodeGraphemeBreakClass.ExtendedPictographic)
        {
            return false;
        }

        if (classes[index - 1] != UnicodeGraphemeBreakClass.Zwj)
        {
            return false;
        }

        int scanIndex = index - 2;

        while (scanIndex >= 0)
        {
            if (classes[scanIndex] == UnicodeGraphemeBreakClass.Extend)
            {
                scanIndex = scanIndex - 1;
                continue;
            }

            return classes[scanIndex] == UnicodeGraphemeBreakClass.ExtendedPictographic;
        }

        return false;
    }

    internal static int CountPreviousRegionalIndicators(UnicodeGraphemeBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(classes);

        int count = 0;
        int scanIndex = index - 1;

        while (scanIndex >= 0)
        {
            if (classes[scanIndex] != UnicodeGraphemeBreakClass.RegionalIndicator)
            {
                break;
            }

            count = count + 1;
            scanIndex = scanIndex - 1;
        }

        return count;
    }

    internal static int CountPreviousWordRegionalIndicators(UnicodeWordBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(classes);

        int count = 0;
        int scanIndex = index - 1;

        while (scanIndex >= 0)
        {
            if (IsWordIgnored(classes[scanIndex]))
            {
                scanIndex = scanIndex - 1;
                continue;
            }

            if (classes[scanIndex] != UnicodeWordBreakClass.RegionalIndicator)
            {
                break;
            }

            count = count + 1;
            scanIndex = scanIndex - 1;
        }

        return count;
    }

    internal static int FindPreviousSignificantWordIndex(UnicodeWordBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(classes);

        int scanIndex = index - 1;

        while (scanIndex >= 0)
        {
            if (!IsWordIgnored(classes[scanIndex]))
            {
                return scanIndex;
            }

            scanIndex = scanIndex - 1;
        }

        return -1;
    }

    internal static int FindNextSignificantWordIndex(UnicodeWordBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(classes);

        int scanIndex = index;

        while (scanIndex < classes.Length)
        {
            if (!IsWordIgnored(classes[scanIndex]))
            {
                return scanIndex;
            }

            scanIndex = scanIndex + 1;
        }

        return -1;
    }

    internal static int FindPreviousSignificantSentenceIndex(UnicodeSentenceBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(classes);

        int scanIndex = index - 1;

        while (scanIndex >= 0)
        {
            if (!IsSentenceIgnored(classes[scanIndex]))
            {
                return scanIndex;
            }

            scanIndex = scanIndex - 1;
        }

        return -1;
    }

    internal static bool IsAfterSentenceTerminal(UnicodeSentenceBreakClass[] classes, int index)
    {
        System.ArgumentNullException.ThrowIfNull(classes);

        int scanIndex = index;

        while (scanIndex >= 0)
        {
            if (IsSentenceIgnored(classes[scanIndex]))
            {
                scanIndex = scanIndex - 1;
                continue;
            }

            if (classes[scanIndex] == UnicodeSentenceBreakClass.Sp)
            {
                scanIndex = scanIndex - 1;
                continue;
            }

            if (IsSentenceClose(classes[scanIndex]))
            {
                scanIndex = scanIndex - 1;
                continue;
            }

            if (classes[scanIndex] == UnicodeSentenceBreakClass.ATerm)
            {
                return true;
            }

            return classes[scanIndex] == UnicodeSentenceBreakClass.STerm;
        }

        return false;
    }

    internal static string LookupBoundaryName(int codePoint, UnicodeRangeValue[] ranges, string[] names, string defaultName)
    {
        System.ArgumentNullException.ThrowIfNull(ranges);
        System.ArgumentNullException.ThrowIfNull(names);
        System.ArgumentNullException.ThrowIfNull(defaultName);

        int index = UnicodeTableLookup.FindRangeValueIndex(ranges, codePoint);
        if (index >= 0)
        {
            int nameIndex = ranges[index].Value;
            if (nameIndex >= 0)
            {
                if (nameIndex < names.Length)
                {
                    return names[nameIndex];
                }
            }

            return defaultName;
        }

        return defaultName;
    }

    internal static bool HasBoundaryProperty(int codePoint, UnicodeRangeValue[] ranges, string[] names, string propertyName)
    {
        System.ArgumentNullException.ThrowIfNull(ranges);
        System.ArgumentNullException.ThrowIfNull(names);
        System.ArgumentNullException.ThrowIfNull(propertyName);

        int index = 0;
        while (index < ranges.Length)
        {
            if (ranges[index].Contains(codePoint))
            {
                int nameIndex = ranges[index].Value;
                if (nameIndex >= 0 && nameIndex < names.Length)
                {
                    if (names[nameIndex] == propertyName)
                    {
                        return true;
                    }
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal static UnicodeGraphemeBreakClass GraphemeBreakClassFromName(string name)
    {
        System.ArgumentNullException.ThrowIfNull(name);

        if (name == "CR")
        {
            return UnicodeGraphemeBreakClass.Cr;
        }

        if (name == "LF")
        {
            return UnicodeGraphemeBreakClass.Lf;
        }

        if (name == "Control")
        {
            return UnicodeGraphemeBreakClass.Control;
        }

        if (name == "Extend")
        {
            return UnicodeGraphemeBreakClass.Extend;
        }

        if (name == "Regional_Indicator")
        {
            return UnicodeGraphemeBreakClass.RegionalIndicator;
        }

        if (name == "Prepend")
        {
            return UnicodeGraphemeBreakClass.Prepend;
        }

        if (name == "SpacingMark")
        {
            return UnicodeGraphemeBreakClass.SpacingMark;
        }

        if (name == "L")
        {
            return UnicodeGraphemeBreakClass.L;
        }

        if (name == "V")
        {
            return UnicodeGraphemeBreakClass.V;
        }

        if (name == "T")
        {
            return UnicodeGraphemeBreakClass.T;
        }

        if (name == "LV")
        {
            return UnicodeGraphemeBreakClass.Lv;
        }

        if (name == "LVT")
        {
            return UnicodeGraphemeBreakClass.Lvt;
        }

        if (name == "ZWJ")
        {
            return UnicodeGraphemeBreakClass.Zwj;
        }

        if (name == "Extended_Pictographic")
        {
            return UnicodeGraphemeBreakClass.ExtendedPictographic;
        }

        return UnicodeGraphemeBreakClass.Other;
    }

    internal static UnicodeWordBreakClass WordBreakClassFromName(string name)
    {
        System.ArgumentNullException.ThrowIfNull(name);

        if (name == "CR")
        {
            return UnicodeWordBreakClass.Cr;
        }

        if (name == "LF")
        {
            return UnicodeWordBreakClass.Lf;
        }

        if (name == "Newline")
        {
            return UnicodeWordBreakClass.Newline;
        }

        if (name == "Extend")
        {
            return UnicodeWordBreakClass.Extend;
        }

        if (name == "Format")
        {
            return UnicodeWordBreakClass.Format;
        }

        if (name == "Katakana")
        {
            return UnicodeWordBreakClass.Katakana;
        }

        if (name == "ALetter")
        {
            return UnicodeWordBreakClass.ALetter;
        }

        if (name == "MidLetter")
        {
            return UnicodeWordBreakClass.MidLetter;
        }

        if (name == "MidNum")
        {
            return UnicodeWordBreakClass.MidNum;
        }

        if (name == "MidNumLet")
        {
            return UnicodeWordBreakClass.MidNumLet;
        }

        if (name == "Numeric")
        {
            return UnicodeWordBreakClass.Numeric;
        }

        if (name == "ExtendNumLet")
        {
            return UnicodeWordBreakClass.ExtendNumLet;
        }

        if (name == "Regional_Indicator")
        {
            return UnicodeWordBreakClass.RegionalIndicator;
        }

        if (name == "Hebrew_Letter")
        {
            return UnicodeWordBreakClass.HebrewLetter;
        }

        if (name == "Single_Quote")
        {
            return UnicodeWordBreakClass.SingleQuote;
        }

        if (name == "Double_Quote")
        {
            return UnicodeWordBreakClass.DoubleQuote;
        }

        if (name == "ZWJ")
        {
            return UnicodeWordBreakClass.Zwj;
        }

        if (name == "WSegSpace")
        {
            return UnicodeWordBreakClass.WSegSpace;
        }

        if (name == "Extended_Pictographic")
        {
            return UnicodeWordBreakClass.ExtendedPictographic;
        }

        return UnicodeWordBreakClass.Other;
    }

    internal static UnicodeSentenceBreakClass SentenceBreakClassFromName(string name)
    {
        System.ArgumentNullException.ThrowIfNull(name);

        if (name == "CR")
        {
            return UnicodeSentenceBreakClass.Cr;
        }

        if (name == "LF")
        {
            return UnicodeSentenceBreakClass.Lf;
        }

        if (name == "Sep")
        {
            return UnicodeSentenceBreakClass.Sep;
        }

        if (name == "Extend")
        {
            return UnicodeSentenceBreakClass.Extend;
        }

        if (name == "Format")
        {
            return UnicodeSentenceBreakClass.Format;
        }

        if (name == "Sp")
        {
            return UnicodeSentenceBreakClass.Sp;
        }

        if (name == "Lower")
        {
            return UnicodeSentenceBreakClass.Lower;
        }

        if (name == "Upper")
        {
            return UnicodeSentenceBreakClass.Upper;
        }

        if (name == "OLetter")
        {
            return UnicodeSentenceBreakClass.OLetter;
        }

        if (name == "Numeric")
        {
            return UnicodeSentenceBreakClass.Numeric;
        }

        if (name == "ATerm")
        {
            return UnicodeSentenceBreakClass.ATerm;
        }

        if (name == "SContinue")
        {
            return UnicodeSentenceBreakClass.SContinue;
        }

        if (name == "STerm")
        {
            return UnicodeSentenceBreakClass.STerm;
        }

        if (name == "Close")
        {
            return UnicodeSentenceBreakClass.Close;
        }

        return UnicodeSentenceBreakClass.Other;
    }

    internal static UnicodeLineBreakClass LineBreakClassFromName(string name)
    {
        System.ArgumentNullException.ThrowIfNull(name);

        if (name == "BK")
        {
            return UnicodeLineBreakClass.MandatoryBreak;
        }

        if (name == "CR")
        {
            return UnicodeLineBreakClass.CarriageReturn;
        }

        if (name == "LF")
        {
            return UnicodeLineBreakClass.LineFeed;
        }

        if (name == "CM")
        {
            return UnicodeLineBreakClass.CombiningMark;
        }

        if (name == "ZW")
        {
            return UnicodeLineBreakClass.ZeroWidthSpace;
        }

        if (name == "IN")
        {
            return UnicodeLineBreakClass.Inseparable;
        }

        if (name == "NS")
        {
            return UnicodeLineBreakClass.Nonstarter;
        }

        if (name == "OP")
        {
            return UnicodeLineBreakClass.OpeningPunctuation;
        }

        if (name == "CL")
        {
            return UnicodeLineBreakClass.ClosingPunctuation;
        }

        if (name == "CP")
        {
            return UnicodeLineBreakClass.ClosingParenthesis;
        }

        if (name == "EX")
        {
            return UnicodeLineBreakClass.Exclamation;
        }

        if (name == "QU")
        {
            return UnicodeLineBreakClass.Quotation;
        }

        if (name == "GL")
        {
            return UnicodeLineBreakClass.Glue;
        }

        if (name == "SP")
        {
            return UnicodeLineBreakClass.Space;
        }

        if (name == "BA")
        {
            return UnicodeLineBreakClass.BreakAfter;
        }

        if (name == "BB")
        {
            return UnicodeLineBreakClass.BreakBefore;
        }

        if (name == "B2")
        {
            return UnicodeLineBreakClass.BreakBoth;
        }

        if (name == "HY")
        {
            return UnicodeLineBreakClass.Hyphen;
        }

        if (name == "AL")
        {
            return UnicodeLineBreakClass.Alphabetic;
        }

        if (name == "NU")
        {
            return UnicodeLineBreakClass.Numeric;
        }

        if (name == "IS")
        {
            return UnicodeLineBreakClass.InfixNumeric;
        }

        if (name == "ID")
        {
            return UnicodeLineBreakClass.Ideographic;
        }

        if (name == "EB")
        {
            return UnicodeLineBreakClass.EmojiBase;
        }

        if (name == "EM")
        {
            return UnicodeLineBreakClass.EmojiModifier;
        }

        if (name == "ZWJ")
        {
            return UnicodeLineBreakClass.ZeroWidthJoiner;
        }

        if (name == "AI")
        {
            return UnicodeLineBreakClass.Ambiguous;
        }

        if (name == "AK")
        {
            return UnicodeLineBreakClass.Aksara;
        }

        if (name == "AP")
        {
            return UnicodeLineBreakClass.AksaraPrebase;
        }

        if (name == "AS")
        {
            return UnicodeLineBreakClass.AksaraStart;
        }

        if (name == "CB")
        {
            return UnicodeLineBreakClass.ContingentBreak;
        }

        if (name == "CJ")
        {
            return UnicodeLineBreakClass.ConditionalJapaneseStarter;
        }

        if (name == "H2")
        {
            return UnicodeLineBreakClass.HangulLvSyllable;
        }

        if (name == "H3")
        {
            return UnicodeLineBreakClass.HangulLvtSyllable;
        }

        if (name == "HH")
        {
            return UnicodeLineBreakClass.UnambiguousHyphen;
        }

        if (name == "HL")
        {
            return UnicodeLineBreakClass.HebrewLetter;
        }

        if (name == "JL")
        {
            return UnicodeLineBreakClass.HangulLeadingJamo;
        }

        if (name == "JT")
        {
            return UnicodeLineBreakClass.HangulTrailingJamo;
        }

        if (name == "JV")
        {
            return UnicodeLineBreakClass.HangulVowelJamo;
        }

        if (name == "NL")
        {
            return UnicodeLineBreakClass.NextLine;
        }

        if (name == "PO")
        {
            return UnicodeLineBreakClass.PostfixNumeric;
        }

        if (name == "PR")
        {
            return UnicodeLineBreakClass.PrefixNumeric;
        }

        if (name == "RI")
        {
            return UnicodeLineBreakClass.RegionalIndicator;
        }

        if (name == "SA")
        {
            return UnicodeLineBreakClass.ComplexContext;
        }

        if (name == "SG")
        {
            return UnicodeLineBreakClass.Surrogate;
        }

        if (name == "SY")
        {
            return UnicodeLineBreakClass.BreakSymbols;
        }

        if (name == "VF")
        {
            return UnicodeLineBreakClass.ViramaFinal;
        }

        if (name == "VI")
        {
            return UnicodeLineBreakClass.Virama;
        }

        if (name == "WJ")
        {
            return UnicodeLineBreakClass.WordJoiner;
        }

        return UnicodeLineBreakClass.Unknown;
    }

    internal static bool IsGraphemeBreakControl(UnicodeGraphemeBreakClass value)
    {
        if (value == UnicodeGraphemeBreakClass.Cr)
        {
            return true;
        }

        if (value == UnicodeGraphemeBreakClass.Lf)
        {
            return true;
        }

        return value == UnicodeGraphemeBreakClass.Control;
    }

    internal static bool IsWordBreakControl(UnicodeWordBreakClass value)
    {
        if (value == UnicodeWordBreakClass.Cr)
        {
            return true;
        }

        if (value == UnicodeWordBreakClass.Lf)
        {
            return true;
        }

        return value == UnicodeWordBreakClass.Newline;
    }

    internal static bool IsWordIgnored(UnicodeWordBreakClass value)
    {
        if (value == UnicodeWordBreakClass.Extend)
        {
            return true;
        }

        if (value == UnicodeWordBreakClass.Format)
        {
            return true;
        }

        return value == UnicodeWordBreakClass.Zwj;
    }

    internal static bool IsWordLetter(UnicodeWordBreakClass value)
    {
        if (value == UnicodeWordBreakClass.ALetter)
        {
            return true;
        }

        return value == UnicodeWordBreakClass.HebrewLetter;
    }

    internal static bool IsSentenceBreakSeparator(UnicodeSentenceBreakClass value)
    {
        if (value == UnicodeSentenceBreakClass.Cr)
        {
            return true;
        }

        if (value == UnicodeSentenceBreakClass.Lf)
        {
            return true;
        }

        return value == UnicodeSentenceBreakClass.Sep;
    }

    internal static bool IsSentenceIgnored(UnicodeSentenceBreakClass value)
    {
        if (value == UnicodeSentenceBreakClass.Extend)
        {
            return true;
        }

        return value == UnicodeSentenceBreakClass.Format;
    }

    internal static bool IsSentenceClose(UnicodeSentenceBreakClass value)
    {
        return value == UnicodeSentenceBreakClass.Close;
    }

    internal static bool IsLineMandatoryBreakBefore(UnicodeLineBreakClass value)
    {
        if (value == UnicodeLineBreakClass.CarriageReturn)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.LineFeed)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.MandatoryBreak;
    }

    internal static bool IsLineMandatoryBreakAfter(UnicodeLineBreakClass value)
    {
        if (value == UnicodeLineBreakClass.CarriageReturn)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.LineFeed)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.MandatoryBreak;
    }

    internal static bool IsLineBreakForbiddenBefore(UnicodeLineBreakClass value)
    {
        if (value == UnicodeLineBreakClass.ClosingPunctuation)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.ClosingParenthesis)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.Exclamation)
        {
            return true;
        }

        if (value == UnicodeLineBreakClass.WordJoiner)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.Glue;
    }

    internal static bool IsLineIdeographicLike(UnicodeLineBreakClass value)
    {
        if (value == UnicodeLineBreakClass.Ideographic)
        {
            return true;
        }

        return value == UnicodeLineBreakClass.EmojiBase;
    }

    internal static bool IsGraphemeControl(int codePoint)
    {
        if (codePoint < 0)
        {
            return false;
        }

        if (codePoint == 0x000D)
        {
            return false;
        }

        if (codePoint == 0x000A)
        {
            return false;
        }

        if (codePoint <= 0x001F)
        {
            return true;
        }

        if (codePoint < 0x007F)
        {
            return false;
        }

        return codePoint <= 0x009F;
    }

    internal static bool IsHangulAfterLeading(UnicodeGraphemeBreakClass value)
    {
        if (value == UnicodeGraphemeBreakClass.L)
        {
            return true;
        }

        if (value == UnicodeGraphemeBreakClass.V)
        {
            return true;
        }

        if (value == UnicodeGraphemeBreakClass.Lv)
        {
            return true;
        }

        return value == UnicodeGraphemeBreakClass.Lvt;
    }

    internal static bool IsHangulAfterLvOrV(UnicodeGraphemeBreakClass value)
    {
        if (value == UnicodeGraphemeBreakClass.V)
        {
            return true;
        }

        return value == UnicodeGraphemeBreakClass.T;
    }
}
