using System.Text;

namespace unicode;

/// <summary>
/// Provides Unicode case mapping, case folding, and caseless comparison operations.
/// </summary>
public class UnicodeCase : IUnicodeCase
{
    internal static readonly UnicodeCase Instance = new UnicodeCase();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodeCase()
    {
    }
    /// <summary>
    /// Applies full Unicode uppercase mapping to text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string ToUpper(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return MapFullCaseText(
            text,
            UnicodeCaseTables.SimpleUppercaseCodePoints(),
            UnicodeCaseTables.SimpleUppercaseMappings(),
            UnicodeCaseTables.SpecialCaseCodePoints(),
            UnicodeCaseTables.SpecialConditionOffsets(),
            UnicodeCaseTables.SpecialConditionLengths(),
            UnicodeCaseTables.SpecialConditionIndexes(),
            UnicodeCaseTables.SpecialConditionNames(),
            UnicodeCaseTables.SpecialUppercaseOffsets(),
            UnicodeCaseTables.SpecialUppercaseLengths(),
            UnicodeCaseTables.SpecialUppercaseMappings());
    }

    /// <summary>
    /// Applies full Unicode lowercase mapping to text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string ToLower(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return MapFullCaseText(
            text,
            UnicodeCaseTables.SimpleLowercaseCodePoints(),
            UnicodeCaseTables.SimpleLowercaseMappings(),
            UnicodeCaseTables.SpecialCaseCodePoints(),
            UnicodeCaseTables.SpecialConditionOffsets(),
            UnicodeCaseTables.SpecialConditionLengths(),
            UnicodeCaseTables.SpecialConditionIndexes(),
            UnicodeCaseTables.SpecialConditionNames(),
            UnicodeCaseTables.SpecialLowercaseOffsets(),
            UnicodeCaseTables.SpecialLowercaseLengths(),
            UnicodeCaseTables.SpecialLowercaseMappings());
    }

    /// <summary>
    /// Applies full uppercase mapping with Turkish, Azeri, or Lithuanian special casing when selected.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="locale">A locale tag; Turkish, Azeri, and Lithuanian language subtags enable Unicode special casing.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>locale</c> is <c>null</c>.</exception>
    public string ToUpperLocale(string text, string locale)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(locale);

        return MapLocaleCaseText(
            text,
            NormalizeLocale(locale),
            UnicodeCaseTables.SimpleUppercaseCodePoints(),
            UnicodeCaseTables.SimpleUppercaseMappings(),
            UnicodeCaseTables.SpecialCaseCodePoints(),
            UnicodeCaseTables.SpecialConditionOffsets(),
            UnicodeCaseTables.SpecialConditionLengths(),
            UnicodeCaseTables.SpecialConditionIndexes(),
            UnicodeCaseTables.SpecialConditionNames(),
            UnicodeCaseTables.SpecialUppercaseOffsets(),
            UnicodeCaseTables.SpecialUppercaseLengths(),
            UnicodeCaseTables.SpecialUppercaseMappings());
    }

    /// <summary>
    /// Applies full lowercase mapping with Turkish, Azeri, or Lithuanian special casing when selected.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="locale">A locale tag; Turkish, Azeri, and Lithuanian language subtags enable Unicode special casing.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>locale</c> is <c>null</c>.</exception>
    public string ToLowerLocale(string text, string locale)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(locale);

        return MapLocaleCaseText(
            text,
            NormalizeLocale(locale),
            UnicodeCaseTables.SimpleLowercaseCodePoints(),
            UnicodeCaseTables.SimpleLowercaseMappings(),
            UnicodeCaseTables.SpecialCaseCodePoints(),
            UnicodeCaseTables.SpecialConditionOffsets(),
            UnicodeCaseTables.SpecialConditionLengths(),
            UnicodeCaseTables.SpecialConditionIndexes(),
            UnicodeCaseTables.SpecialConditionNames(),
            UnicodeCaseTables.SpecialLowercaseOffsets(),
            UnicodeCaseTables.SpecialLowercaseLengths(),
            UnicodeCaseTables.SpecialLowercaseMappings());
    }

    /// <summary>
    /// Applies full Unicode titlecase mapping independently to every scalar in the text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string ToTitle(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return MapFullCaseText(
            text,
            UnicodeCaseTables.SimpleTitlecaseCodePoints(),
            UnicodeCaseTables.SimpleTitlecaseMappings(),
            UnicodeCaseTables.SpecialCaseCodePoints(),
            UnicodeCaseTables.SpecialConditionOffsets(),
            UnicodeCaseTables.SpecialConditionLengths(),
            UnicodeCaseTables.SpecialConditionIndexes(),
            UnicodeCaseTables.SpecialConditionNames(),
            UnicodeCaseTables.SpecialTitlecaseOffsets(),
            UnicodeCaseTables.SpecialTitlecaseLengths(),
            UnicodeCaseTables.SpecialTitlecaseMappings());
    }

    /// <summary>
    /// Applies full Unicode titlecase mapping with Turkish, Azeri, or Lithuanian special casing when selected.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="locale">A locale tag; Turkish, Azeri, and Lithuanian language subtags enable Unicode special casing.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>locale</c> is <c>null</c>.</exception>
    public string ToTitleLocale(string text, string locale)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(locale);

        return MapLocaleCaseText(
            text,
            NormalizeLocale(locale),
            UnicodeCaseTables.SimpleTitlecaseCodePoints(),
            UnicodeCaseTables.SimpleTitlecaseMappings(),
            UnicodeCaseTables.SpecialCaseCodePoints(),
            UnicodeCaseTables.SpecialConditionOffsets(),
            UnicodeCaseTables.SpecialConditionLengths(),
            UnicodeCaseTables.SpecialConditionIndexes(),
            UnicodeCaseTables.SpecialConditionNames(),
            UnicodeCaseTables.SpecialTitlecaseOffsets(),
            UnicodeCaseTables.SpecialTitlecaseLengths(),
            UnicodeCaseTables.SpecialTitlecaseMappings());
    }

    /// <summary>
    /// Titlecases the first cased character of each default Unicode word and lowercases subsequent characters in that word.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string ToTitleWords(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return MapBoundaryTitleText(text, "", true);
    }

    /// <summary>
    /// Titlecases default Unicode words with Turkish, Azeri, or Lithuanian special casing when selected.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="locale">A locale tag; Turkish, Azeri, and Lithuanian language subtags enable Unicode special casing.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>locale</c> is <c>null</c>.</exception>
    public string ToTitleWordsLocale(string text, string locale)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(locale);

        return MapBoundaryTitleText(text, NormalizeLocale(locale), true);
    }

    /// <summary>
    /// Titlecases the first cased character of each extended grapheme cluster and lowercases subsequent characters in that cluster.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string ToTitleGraphemes(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return MapBoundaryTitleText(text, "", false);
    }

    /// <summary>
    /// Titlecases extended grapheme clusters with Turkish, Azeri, or Lithuanian special casing when selected.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="locale">A locale tag; Turkish, Azeri, and Lithuanian language subtags enable Unicode special casing.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>locale</c> is <c>null</c>.</exception>
    public string ToTitleGraphemesLocale(string text, string locale)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(locale);

        return MapBoundaryTitleText(text, NormalizeLocale(locale), false);
    }

    /// <summary>
    /// Applies full default Unicode case folding to text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string Fold(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return FoldText(text, false);
    }

    /// <summary>
    /// Applies full Unicode Turkic case folding to text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string FoldTurkic(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return FoldText(text, true);
    }

    internal static string FoldText(string text, bool useTurkic)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        StringBuilder builder = new StringBuilder(text.Length);
        int[] codePoints = UnicodeCaseTables.CaseFoldCodePoints();
        int[] statusIndexes = UnicodeCaseTables.CaseFoldStatusIndexes();
        string[] statusNames = UnicodeCaseTables.CaseFoldStatusNames();
        int[] mappingOffsets = UnicodeCaseTables.CaseFoldMappingOffsets();
        int[] mappingLengths = UnicodeCaseTables.CaseFoldMappingLengths();
        int[] mappings = UnicodeCaseTables.CaseFoldMappings();
        int index = 0;

        while (index < scalars.Length)
        {
            int codePoint = scalars[index].CodePoint;
            int foldIndex = -1;
            if (useTurkic)
            {
                foldIndex = FindTurkicFoldIndex(codePoint, codePoints, statusIndexes, statusNames, mappingLengths);
            }

            if (foldIndex < 0)
            {
                foldIndex = FindFullFoldIndex(codePoint, codePoints, statusIndexes, statusNames, mappingLengths);
            }

            if (foldIndex >= 0)
            {
                AppendMapping(builder, mappings, mappingOffsets[foldIndex], mappingLengths[foldIndex]);
            }
            else
            {
                builder.Append(UnicodeScalar.Instance.ToUtf16String(codePoint));
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Applies NFKC case folding for compatibility-based caseless matching.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string ToNfkcCaseFold(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        int[] starts = UnicodeNormalizationTables.NfkcCaseFoldStarts();
        int[] ends = UnicodeNormalizationTables.NfkcCaseFoldEnds();
        int[] offsets = UnicodeNormalizationTables.NfkcCaseFoldOffsets();
        int[] lengths = UnicodeNormalizationTables.NfkcCaseFoldLengths();
        int[] mappings = UnicodeNormalizationTables.NfkcCaseFoldCodePoints();
        StringBuilder builder = new StringBuilder(text.Length);
        int index = 0;
        while (index < scalars.Length)
        {
            int mappingIndex = FindNfkcCaseFoldIndex(scalars[index].CodePoint, starts, ends);
            if (mappingIndex < 0)
            {
                builder.Append(UnicodeScalar.Instance.ToUtf16String(scalars[index].CodePoint));
            }
            else
            {
                AppendMapping(builder, mappings, offsets[mappingIndex], lengths[mappingIndex]);
            }

            index = index + 1;
        }

        return UnicodeNormalization.Instance.Normalize(builder.ToString(), UnicodeNormalizationForm.Nfc);
    }

    internal static int FindNfkcCaseFoldIndex(int codePoint, int[] starts, int[] ends)
    {
        System.ArgumentNullException.ThrowIfNull(starts);
        System.ArgumentNullException.ThrowIfNull(ends);

        int low = 0;
        int high = starts.Length - 1;
        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            if (codePoint < starts[middle])
            {
                high = middle - 1;
            }
            else if (codePoint > ends[middle])
            {
                low = middle + 1;
            }
            else
            {
                return middle;
            }
        }

        return -1;
    }

    /// <summary>
    /// Normalizes and folds text into a stable key suitable for ordinal caseless comparison.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="mode">The case-mapping or comparison-key mode to use.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>mode</c> is outside the supported range.</exception>
    public string CompareKey(string text, UnicodeCompareKeyMode mode)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (mode == UnicodeCompareKeyMode.OrdinalFold)
        {
            return Fold(text);
        }

        if (mode == UnicodeCompareKeyMode.NfcFold)
        {
            string normalized = UnicodeNormalization.Instance.Normalize(text, UnicodeNormalizationForm.Nfc);
            string folded = Fold(normalized);
            return UnicodeNormalization.Instance.Normalize(folded, UnicodeNormalizationForm.Nfc);
        }

        if (mode == UnicodeCompareKeyMode.NfkcFold)
        {
            return ToNfkcCaseFold(text);
        }

        throw new System.ArgumentOutOfRangeException("mode");
    }

    /// <summary>
    /// Reports whether two strings are ordinally equal after full Unicode case folding.
    /// </summary>
    /// <param name="left">The first UTF-16 string.</param>
    /// <param name="right">The second UTF-16 string.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>left</c>, <c>right</c> is <c>null</c>.</exception>
    public bool EqualsFolded(string left, string right)
    {
        System.ArgumentNullException.ThrowIfNull(left);
        System.ArgumentNullException.ThrowIfNull(right);

        string leftKey = CompareKey(left, UnicodeCompareKeyMode.OrdinalFold);
        string rightKey = CompareKey(right, UnicodeCompareKeyMode.OrdinalFold);
        return string.Equals(leftKey, rightKey, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Applies simple Unicode uppercase mapping to one scalar.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting integer value.</returns>
    public int ToUpperScalar(int codePoint)
    {
        return MapSimpleCodePoint(codePoint, UnicodeCaseTables.SimpleUppercaseCodePoints(), UnicodeCaseTables.SimpleUppercaseMappings());
    }

    /// <summary>
    /// Applies simple Unicode lowercase mapping to one scalar.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting integer value.</returns>
    public int ToLowerScalar(int codePoint)
    {
        return MapSimpleCodePoint(codePoint, UnicodeCaseTables.SimpleLowercaseCodePoints(), UnicodeCaseTables.SimpleLowercaseMappings());
    }

    /// <summary>
    /// Applies simple Unicode titlecase mapping to one scalar.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting integer value.</returns>
    public int ToTitleScalar(int codePoint)
    {
        return MapSimpleCodePoint(codePoint, UnicodeCaseTables.SimpleTitlecaseCodePoints(), UnicodeCaseTables.SimpleTitlecaseMappings());
    }

    /// <summary>
    /// Applies simple default Unicode case folding to one scalar.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting integer value.</returns>
    public int FoldScalar(int codePoint)
    {
        int[] codePoints = UnicodeCaseTables.CaseFoldCodePoints();
        int[] statusIndexes = UnicodeCaseTables.CaseFoldStatusIndexes();
        string[] statusNames = UnicodeCaseTables.CaseFoldStatusNames();
        int[] mappingOffsets = UnicodeCaseTables.CaseFoldMappingOffsets();
        int[] mappingLengths = UnicodeCaseTables.CaseFoldMappingLengths();
        int[] mappings = UnicodeCaseTables.CaseFoldMappings();
        int foldIndex = FindSimpleFoldIndex(codePoint, codePoints, statusIndexes, statusNames, mappingLengths);

        if (foldIndex < 0)
        {
            return codePoint;
        }

        return mappings[mappingOffsets[foldIndex]];
    }

    /// <summary>
    /// Applies simple Unicode Turkic case folding to one scalar.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting integer value.</returns>
    public int FoldScalarTurkic(int codePoint)
    {
        int[] codePoints = UnicodeCaseTables.CaseFoldCodePoints();
        int[] statusIndexes = UnicodeCaseTables.CaseFoldStatusIndexes();
        string[] statusNames = UnicodeCaseTables.CaseFoldStatusNames();
        int[] mappingOffsets = UnicodeCaseTables.CaseFoldMappingOffsets();
        int[] mappingLengths = UnicodeCaseTables.CaseFoldMappingLengths();
        int[] mappings = UnicodeCaseTables.CaseFoldMappings();
        int foldIndex = FindTurkicFoldIndex(codePoint, codePoints, statusIndexes, statusNames, mappingLengths);

        if (foldIndex >= 0)
        {
            if (mappingLengths[foldIndex] != 1)
            {
                foldIndex = -1;
            }
        }

        if (foldIndex < 0)
        {
            foldIndex = FindSimpleFoldIndex(codePoint, codePoints, statusIndexes, statusNames, mappingLengths);
        }

        if (foldIndex < 0)
        {
            return codePoint;
        }

        return mappings[mappingOffsets[foldIndex]];
    }

    internal static string MapSimpleText(string text, int[] codePoints, int[] mappings)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        StringBuilder builder = new StringBuilder(text.Length);
        int index = 0;

        while (index < scalars.Length)
        {
            int mapped = MapSimpleCodePoint(scalars[index].CodePoint, codePoints, mappings);
            builder.Append(UnicodeScalar.Instance.ToUtf16String(mapped));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string MapFullCaseText(
        string text,
        int[] simpleCodePoints,
        int[] simpleMappings,
        int[] specialCodePoints,
        int[] specialConditionOffsets,
        int[] specialConditionLengths,
        int[] specialConditionIndexes,
        string[] specialConditionNames,
        int[] specialMappingOffsets,
        int[] specialMappingLengths,
        int[] specialMappings)
    {
        return MapLocaleCaseText(
            text,
            "",
            simpleCodePoints,
            simpleMappings,
            specialCodePoints,
            specialConditionOffsets,
            specialConditionLengths,
            specialConditionIndexes,
            specialConditionNames,
            specialMappingOffsets,
            specialMappingLengths,
            specialMappings);
    }

    internal static string MapLocaleCaseText(
        string text,
        string localeName,
        int[] simpleCodePoints,
        int[] simpleMappings,
        int[] specialCodePoints,
        int[] specialConditionOffsets,
        int[] specialConditionLengths,
        int[] specialConditionIndexes,
        string[] specialConditionNames,
        int[] specialMappingOffsets,
        int[] specialMappingLengths,
        int[] specialMappings)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(localeName);
        System.ArgumentNullException.ThrowIfNull(simpleCodePoints);
        System.ArgumentNullException.ThrowIfNull(simpleMappings);
        System.ArgumentNullException.ThrowIfNull(specialCodePoints);
        System.ArgumentNullException.ThrowIfNull(specialConditionOffsets);
        System.ArgumentNullException.ThrowIfNull(specialConditionLengths);
        System.ArgumentNullException.ThrowIfNull(specialConditionIndexes);
        System.ArgumentNullException.ThrowIfNull(specialConditionNames);
        System.ArgumentNullException.ThrowIfNull(specialMappingOffsets);
        System.ArgumentNullException.ThrowIfNull(specialMappingLengths);
        System.ArgumentNullException.ThrowIfNull(specialMappings);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        StringBuilder builder = new StringBuilder(text.Length);
        int index = 0;

        while (index < scalars.Length)
        {
            AppendFullCaseMapping(
                builder,
                index,
                scalars,
                localeName,
                simpleCodePoints,
                simpleMappings,
                specialCodePoints,
                specialConditionOffsets,
                specialConditionLengths,
                specialConditionIndexes,
                specialConditionNames,
                specialMappingOffsets,
                specialMappingLengths,
                specialMappings);

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string MapBoundaryTitleText(string text, string localeName, bool useWordBoundaries)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        System.ArgumentNullException.ThrowIfNull(localeName);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        UnicodeSegmentRecord[] segments;
        if (useWordBoundaries)
        {
            segments = UnicodeSegments.Instance.GetWordSegments(text);
        }
        else
        {
            segments = UnicodeSegments.Instance.GetGraphemeSegments(text);
        }

        int[] titleSimpleCodePoints = UnicodeCaseTables.SimpleTitlecaseCodePoints();
        int[] titleSimpleMappings = UnicodeCaseTables.SimpleTitlecaseMappings();
        int[] lowerSimpleCodePoints = UnicodeCaseTables.SimpleLowercaseCodePoints();
        int[] lowerSimpleMappings = UnicodeCaseTables.SimpleLowercaseMappings();
        int[] specialCodePoints = UnicodeCaseTables.SpecialCaseCodePoints();
        int[] specialConditionOffsets = UnicodeCaseTables.SpecialConditionOffsets();
        int[] specialConditionLengths = UnicodeCaseTables.SpecialConditionLengths();
        int[] specialConditionIndexes = UnicodeCaseTables.SpecialConditionIndexes();
        string[] specialConditionNames = UnicodeCaseTables.SpecialConditionNames();
        int[] titleSpecialMappingOffsets = UnicodeCaseTables.SpecialTitlecaseOffsets();
        int[] titleSpecialMappingLengths = UnicodeCaseTables.SpecialTitlecaseLengths();
        int[] titleSpecialMappings = UnicodeCaseTables.SpecialTitlecaseMappings();
        int[] lowerSpecialMappingOffsets = UnicodeCaseTables.SpecialLowercaseOffsets();
        int[] lowerSpecialMappingLengths = UnicodeCaseTables.SpecialLowercaseLengths();
        int[] lowerSpecialMappings = UnicodeCaseTables.SpecialLowercaseMappings();
        StringBuilder builder = new StringBuilder(text.Length);
        int scalarIndex = 0;
        int segmentIndex = 0;
        bool foundCased = false;

        while (scalarIndex < scalars.Length)
        {
            while (segmentIndex < segments.Length)
            {
                if (scalars[scalarIndex].Utf16Index < segments[segmentIndex].EndUtf16Index)
                {
                    break;
                }

                segmentIndex = segmentIndex + 1;
                foundCased = false;
            }

            if (!foundCased)
            {
                if (IsCased(scalars[scalarIndex].CodePoint))
                {
                    AppendFullCaseMapping(
                        builder,
                        scalarIndex,
                        scalars,
                        localeName,
                        titleSimpleCodePoints,
                        titleSimpleMappings,
                        specialCodePoints,
                        specialConditionOffsets,
                        specialConditionLengths,
                        specialConditionIndexes,
                        specialConditionNames,
                        titleSpecialMappingOffsets,
                        titleSpecialMappingLengths,
                        titleSpecialMappings);
                    foundCased = true;
                }
                else
                {
                    builder.Append(UnicodeScalar.Instance.ToUtf16String(scalars[scalarIndex].CodePoint));
                }
            }
            else
            {
                AppendFullCaseMapping(
                    builder,
                    scalarIndex,
                    scalars,
                    localeName,
                    lowerSimpleCodePoints,
                    lowerSimpleMappings,
                    specialCodePoints,
                    specialConditionOffsets,
                    specialConditionLengths,
                    specialConditionIndexes,
                    specialConditionNames,
                    lowerSpecialMappingOffsets,
                    lowerSpecialMappingLengths,
                    lowerSpecialMappings);
            }

            scalarIndex = scalarIndex + 1;
        }

        return builder.ToString();
    }

    internal static void AppendFullCaseMapping(
        StringBuilder builder,
        int scalarIndex,
        UnicodeScalarRecord[] scalars,
        string localeName,
        int[] simpleCodePoints,
        int[] simpleMappings,
        int[] specialCodePoints,
        int[] specialConditionOffsets,
        int[] specialConditionLengths,
        int[] specialConditionIndexes,
        string[] specialConditionNames,
        int[] specialMappingOffsets,
        int[] specialMappingLengths,
        int[] specialMappings)
    {
        System.ArgumentNullException.ThrowIfNull(builder);
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(localeName);

        int codePoint = scalars[scalarIndex].CodePoint;
        int specialIndex = FindContextualSpecialCaseIndex(
            codePoint,
            scalarIndex,
            scalars,
            specialCodePoints,
            specialConditionOffsets,
            specialConditionLengths,
            specialConditionIndexes,
            specialConditionNames,
            specialMappingLengths,
            localeName);

        if (specialIndex < 0)
        {
            specialIndex = FindUnconditionalSpecialCaseIndex(codePoint, specialCodePoints, specialConditionLengths, specialMappingLengths);
        }

        if (specialIndex >= 0)
        {
            AppendMapping(builder, specialMappings, specialMappingOffsets[specialIndex], specialMappingLengths[specialIndex]);
        }
        else
        {
            int mapped = MapSimpleCodePoint(codePoint, simpleCodePoints, simpleMappings);
            builder.Append(UnicodeScalar.Instance.ToUtf16String(mapped));
        }
    }

    internal static int MapSimpleCodePoint(int codePoint, int[] codePoints, int[] mappings)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);
        System.ArgumentNullException.ThrowIfNull(mappings);

        int index = FindCodePointIndex(codePoint, codePoints);
        if (index < 0)
        {
            return codePoint;
        }

        if (index >= mappings.Length)
        {
            return codePoint;
        }

        return mappings[index];
    }

    internal static int FindCodePointIndex(int codePoint, int[] codePoints)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);

        int low = 0;
        int high = codePoints.Length - 1;

        while (low <= high)
        {
            int mid = low + ((high - low) / 2);
            int current = codePoints[mid];

            if (codePoint < current)
            {
                high = mid - 1;
                continue;
            }

            if (codePoint > current)
            {
                low = mid + 1;
                continue;
            }

            return mid;
        }

        return -1;
    }

    internal static int FindContextualSpecialCaseIndex(
        int codePoint,
        int scalarIndex,
        UnicodeScalarRecord[] scalars,
        int[] codePoints,
        int[] conditionOffsets,
        int[] conditionLengths,
        int[] conditionIndexes,
        string[] conditionNames,
        int[] mappingLengths)
    {
        return FindContextualSpecialCaseIndex(
            codePoint,
            scalarIndex,
            scalars,
            codePoints,
            conditionOffsets,
            conditionLengths,
            conditionIndexes,
            conditionNames,
            mappingLengths,
            "");
    }

    internal static int FindContextualSpecialCaseIndex(
        int codePoint,
        int scalarIndex,
        UnicodeScalarRecord[] scalars,
        int[] codePoints,
        int[] conditionOffsets,
        int[] conditionLengths,
        int[] conditionIndexes,
        string[] conditionNames,
        int[] mappingLengths,
        string localeName)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(codePoints);
        System.ArgumentNullException.ThrowIfNull(conditionOffsets);
        System.ArgumentNullException.ThrowIfNull(conditionLengths);
        System.ArgumentNullException.ThrowIfNull(conditionIndexes);
        System.ArgumentNullException.ThrowIfNull(conditionNames);
        System.ArgumentNullException.ThrowIfNull(mappingLengths);
        System.ArgumentNullException.ThrowIfNull(localeName);

        int index = 0;

        while (index < codePoints.Length)
        {
            if (codePoints[index] == codePoint)
            {
                if (index < conditionLengths.Length)
                {
                    if (index < mappingLengths.Length)
                    {
                        if (conditionLengths[index] > 0)
                        {
                            if (ConditionsMatch(scalarIndex, scalars, conditionOffsets[index], conditionLengths[index], conditionIndexes, conditionNames, localeName))
                            {
                                return index;
                            }
                        }
                    }
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindSimpleFoldIndex(int codePoint, int[] codePoints, int[] statusIndexes, string[] statusNames, int[] mappingLengths)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);
        System.ArgumentNullException.ThrowIfNull(statusIndexes);
        System.ArgumentNullException.ThrowIfNull(statusNames);
        System.ArgumentNullException.ThrowIfNull(mappingLengths);

        int index = 0;

        while (index < codePoints.Length)
        {
            if (codePoints[index] == codePoint)
            {
                if (index < statusIndexes.Length)
                {
                    int statusIndex = statusIndexes[index];
                    if (statusIndex >= 0)
                    {
                        if (statusIndex < statusNames.Length)
                        {
                            if (index < mappingLengths.Length)
                            {
                                if (mappingLengths[index] == 1)
                                {
                                    if (IsSimpleFoldStatus(statusNames[statusIndex]))
                                    {
                                        return index;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindFullFoldIndex(int codePoint, int[] codePoints, int[] statusIndexes, string[] statusNames, int[] mappingLengths)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);
        System.ArgumentNullException.ThrowIfNull(statusIndexes);
        System.ArgumentNullException.ThrowIfNull(statusNames);
        System.ArgumentNullException.ThrowIfNull(mappingLengths);

        int index = 0;

        while (index < codePoints.Length)
        {
            if (codePoints[index] == codePoint)
            {
                if (index < statusIndexes.Length)
                {
                    int statusIndex = statusIndexes[index];
                    if (statusIndex >= 0)
                    {
                        if (statusIndex < statusNames.Length)
                        {
                            if (index < mappingLengths.Length)
                            {
                                if (mappingLengths[index] > 0)
                                {
                                    if (IsFullFoldStatus(statusNames[statusIndex]))
                                    {
                                        return index;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindTurkicFoldIndex(int codePoint, int[] codePoints, int[] statusIndexes, string[] statusNames, int[] mappingLengths)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);
        System.ArgumentNullException.ThrowIfNull(statusIndexes);
        System.ArgumentNullException.ThrowIfNull(statusNames);
        System.ArgumentNullException.ThrowIfNull(mappingLengths);

        int index = 0;

        while (index < codePoints.Length)
        {
            if (codePoints[index] == codePoint)
            {
                if (index < statusIndexes.Length)
                {
                    int statusIndex = statusIndexes[index];
                    if (statusIndex >= 0)
                    {
                        if (statusIndex < statusNames.Length)
                        {
                            if (index < mappingLengths.Length)
                            {
                                if (mappingLengths[index] > 0)
                                {
                                    if (statusNames[statusIndex] == "T")
                                    {
                                        return index;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int FindUnconditionalSpecialCaseIndex(int codePoint, int[] codePoints, int[] conditionLengths, int[] mappingLengths)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);
        System.ArgumentNullException.ThrowIfNull(conditionLengths);
        System.ArgumentNullException.ThrowIfNull(mappingLengths);

        int index = 0;

        while (index < codePoints.Length)
        {
            if (codePoints[index] == codePoint)
            {
                if (index < conditionLengths.Length)
                {
                    if (index < mappingLengths.Length)
                    {
                        if (conditionLengths[index] == 0)
                        {
                            if (mappingLengths[index] > 0)
                            {
                                return index;
                            }
                        }
                    }
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal static void AppendMapping(StringBuilder builder, int[] mappings, int offset, int length)
    {
        System.ArgumentNullException.ThrowIfNull(builder);
        System.ArgumentNullException.ThrowIfNull(mappings);

        int index = 0;

        while (index < length)
        {
            int mappingIndex = offset + index;
            if (mappingIndex >= 0)
            {
                if (mappingIndex < mappings.Length)
                {
                    builder.Append(UnicodeScalar.Instance.ToUtf16String(mappings[mappingIndex]));
                }
            }

            index = index + 1;
        }
    }

    internal static bool ConditionsMatch(
        int scalarIndex,
        UnicodeScalarRecord[] scalars,
        int conditionOffset,
        int conditionLength,
        int[] conditionIndexes,
        string[] conditionNames)
    {
        return ConditionsMatch(scalarIndex, scalars, conditionOffset, conditionLength, conditionIndexes, conditionNames, "");
    }

    internal static bool ConditionsMatch(
        int scalarIndex,
        UnicodeScalarRecord[] scalars,
        int conditionOffset,
        int conditionLength,
        int[] conditionIndexes,
        string[] conditionNames,
        string localeName)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(conditionIndexes);
        System.ArgumentNullException.ThrowIfNull(conditionNames);
        System.ArgumentNullException.ThrowIfNull(localeName);

        int index = 0;

        while (index < conditionLength)
        {
            int conditionIndex = conditionOffset + index;
            if (conditionIndex < 0)
            {
                return false;
            }

            if (conditionIndex >= conditionIndexes.Length)
            {
                return false;
            }

            int nameIndex = conditionIndexes[conditionIndex];
            if (nameIndex < 0)
            {
                return false;
            }

            if (nameIndex >= conditionNames.Length)
            {
                return false;
            }

            if (!ConditionMatches(conditionNames[nameIndex], scalarIndex, scalars, localeName))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ConditionMatches(string conditionName, int scalarIndex, UnicodeScalarRecord[] scalars)
    {
        return ConditionMatches(conditionName, scalarIndex, scalars, "");
    }

    internal static bool ConditionMatches(string conditionName, int scalarIndex, UnicodeScalarRecord[] scalars, string localeName)
    {
        System.ArgumentNullException.ThrowIfNull(conditionName);
        System.ArgumentNullException.ThrowIfNull(scalars);
        System.ArgumentNullException.ThrowIfNull(localeName);

        if (IsLocaleCondition(conditionName))
        {
            return conditionName == localeName;
        }

        if (conditionName == "Final_Sigma")
        {
            return IsFinalSigmaContext(scalarIndex, scalars);
        }

        if (conditionName == "After_Soft_Dotted")
        {
            return IsAfterSoftDottedContext(scalarIndex, scalars);
        }

        if (conditionName == "More_Above")
        {
            return IsMoreAboveContext(scalarIndex, scalars);
        }

        if (conditionName == "After_I")
        {
            return IsAfterIContext(scalarIndex, scalars);
        }

        if (conditionName == "Before_Dot")
        {
            return IsBeforeDotContext(scalarIndex, scalars);
        }

        if (conditionName == "Not_Before_Dot")
        {
            return !IsBeforeDotContext(scalarIndex, scalars);
        }

        return false;
    }

    internal static string NormalizeLocale(string locale)
    {
        System.ArgumentNullException.ThrowIfNull(locale);

        string trimmed = locale.Trim();
        StringBuilder builder = new StringBuilder();
        int index = 0;

        while (index < trimmed.Length)
        {
            char value = trimmed[index];
            if (value == '-')
            {
                break;
            }

            if (value == '_')
            {
                break;
            }

            if (value >= 'A')
            {
                if (value <= 'Z')
                {
                    value = (char)(value + 32);
                }
            }

            builder.Append(value);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static bool IsLocaleCondition(string conditionName)
    {
        System.ArgumentNullException.ThrowIfNull(conditionName);

        if (conditionName == "tr")
        {
            return true;
        }

        if (conditionName == "az")
        {
            return true;
        }

        return conditionName == "lt";
    }

    internal static bool IsFinalSigmaContext(int scalarIndex, UnicodeScalarRecord[] scalars)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        if (!HasCasedBefore(scalarIndex, scalars))
        {
            return false;
        }

        return !HasCasedAfter(scalarIndex, scalars);
    }

    internal static bool HasCasedBefore(int scalarIndex, UnicodeScalarRecord[] scalars)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        int index = scalarIndex - 1;

        while (index >= 0)
        {
            int codePoint = scalars[index].CodePoint;

            if (IsCaseIgnorable(codePoint))
            {
                index = index - 1;
                continue;
            }

            return IsCased(codePoint);
        }

        return false;
    }

    internal static bool HasCasedAfter(int scalarIndex, UnicodeScalarRecord[] scalars)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        int index = scalarIndex + 1;

        while (index < scalars.Length)
        {
            int codePoint = scalars[index].CodePoint;

            if (IsCaseIgnorable(codePoint))
            {
                index = index + 1;
                continue;
            }

            return IsCased(codePoint);
        }

        return false;
    }

    internal static bool IsAfterSoftDottedContext(int scalarIndex, UnicodeScalarRecord[] scalars)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        int index = scalarIndex - 1;

        while (index >= 0)
        {
            int codePoint = scalars[index].CodePoint;
            int combiningClass = UnicodeProperties.Instance.GetCombiningClass(codePoint);

            if (combiningClass == 0)
            {
                return IsSoftDotted(codePoint);
            }

            if (combiningClass == 230)
            {
                return false;
            }

            index = index - 1;
        }

        return false;
    }

    internal static bool IsMoreAboveContext(int scalarIndex, UnicodeScalarRecord[] scalars)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        int index = scalarIndex + 1;

        while (index < scalars.Length)
        {
            int codePoint = scalars[index].CodePoint;
            int combiningClass = UnicodeProperties.Instance.GetCombiningClass(codePoint);

            if (combiningClass == 230)
            {
                return true;
            }

            if (combiningClass == 0)
            {
                return false;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsAfterIContext(int scalarIndex, UnicodeScalarRecord[] scalars)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        int index = scalarIndex - 1;

        while (index >= 0)
        {
            int codePoint = scalars[index].CodePoint;
            int combiningClass = UnicodeProperties.Instance.GetCombiningClass(codePoint);

            if (combiningClass == 0)
            {
                return codePoint == 0x0049;
            }

            if (combiningClass == 230)
            {
                return false;
            }

            index = index - 1;
        }

        return false;
    }

    internal static bool IsBeforeDotContext(int scalarIndex, UnicodeScalarRecord[] scalars)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        int index = scalarIndex + 1;

        while (index < scalars.Length)
        {
            int codePoint = scalars[index].CodePoint;

            if (codePoint == 0x0307)
            {
                return true;
            }

            int combiningClass = UnicodeProperties.Instance.GetCombiningClass(codePoint);
            if (combiningClass == 0)
            {
                return false;
            }

            if (combiningClass == 230)
            {
                return false;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsCased(int codePoint)
    {
        return UnicodeRangeSet.ContainsRanges(UnicodeCaseTables.Cased(), codePoint);
    }

    internal static bool IsCaseIgnorable(int codePoint)
    {
        return UnicodeRangeSet.ContainsRanges(UnicodeCaseTables.CaseIgnorable(), codePoint);
    }

    internal static bool IsSoftDotted(int codePoint)
    {
        return UnicodeRangeSet.ContainsRanges(UnicodeCaseTables.SoftDotted(), codePoint);
    }

    internal static bool IsSimpleFoldStatus(string status)
    {
        System.ArgumentNullException.ThrowIfNull(status);

        if (status == "C")
        {
            return true;
        }

        return status == "S";
    }

    internal static bool IsFullFoldStatus(string status)
    {
        System.ArgumentNullException.ThrowIfNull(status);

        if (status == "C")
        {
            return true;
        }

        return status == "F";
    }
}
