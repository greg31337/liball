using System.Collections.Generic;
using System.Text;

namespace unicode;

/// <summary>
/// Provides Unicode normalization, quick-check, stream-safe, FCD, and FCC operations.
/// </summary>
public class UnicodeNormalization : IUnicodeNormalization
{
    internal static readonly UnicodeNormalization Instance = new UnicodeNormalization();
    internal static readonly Dictionary<long, int> CanonicalCompositionPairs = BuildCanonicalCompositionPairs();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodeNormalization()
    {
    }
    /// <summary>
    /// U+034F COMBINING GRAPHEME JOINER, inserted to make text stream-safe.
    /// </summary>
    internal const int CombiningGraphemeJoiner = 0x034F;
    /// <summary>
    /// The maximum consecutive non-starters permitted by Unicode stream-safe text format.
    /// </summary>
    internal const int StreamSafeMaxNonStarters = 30;

    /// <summary>
    /// Applies canonical or compatibility decomposition, ordering, and optional composition for the selected form.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="form">The Unicode normalization form to use.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>form</c> is outside the supported range.</exception>
    public string Normalize(string text, UnicodeNormalizationForm form)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (form == UnicodeNormalizationForm.Nfd)
        {
            return DecomposeAndOrderCanonical(text);
        }

        if (form == UnicodeNormalizationForm.Nfkd)
        {
            return DecomposeAndOrderCompatibility(text);
        }

        if (form == UnicodeNormalizationForm.Nfc)
        {
            return ComposeCanonical(DecomposeAndOrderCanonical(text));
        }

        if (form == UnicodeNormalizationForm.Nfkc)
        {
            return ComposeCanonical(DecomposeAndOrderCompatibility(text));
        }

        throw new System.ArgumentOutOfRangeException("form");
    }

    /// <summary>
    /// Normalizes text to canonical decomposition followed by canonical composition (NFC).
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string ToNfc(string text)
    {
        return Normalize(text, UnicodeNormalizationForm.Nfc);
    }

    /// <summary>
    /// Normalizes text to fully ordered canonical decomposition (NFD).
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string ToNfd(string text)
    {
        return Normalize(text, UnicodeNormalizationForm.Nfd);
    }

    /// <summary>
    /// Normalizes text with compatibility decomposition followed by canonical composition (NFKC).
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string ToNfkc(string text)
    {
        return Normalize(text, UnicodeNormalizationForm.Nfkc);
    }

    /// <summary>
    /// Normalizes text to fully ordered compatibility decomposition (NFKD).
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string ToNfkd(string text)
    {
        return Normalize(text, UnicodeNormalizationForm.Nfkd);
    }

    /// <summary>
    /// Concatenates two strings and repairs normalization only where their boundary can affect the selected form.
    /// </summary>
    /// <param name="first">The first value or normalized text segment.</param>
    /// <param name="second">The second value or text segment.</param>
    /// <param name="form">The Unicode normalization form to use.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>first</c>, <c>second</c> is <c>null</c>.</exception>
    public string Append(string first, string second, UnicodeNormalizationForm form)
    {
        System.ArgumentNullException.ThrowIfNull(first);
        System.ArgumentNullException.ThrowIfNull(second);

        return Normalize(first + second, form);
    }

    /// <summary>
    /// Concatenates all parts and normalizes the result in the selected form.
    /// </summary>
    /// <param name="parts">The text segments to concatenate and normalize.</param>
    /// <param name="form">The Unicode normalization form to use.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>parts</c> is <c>null</c>.</exception>
    public string Concat(string[] parts, UnicodeNormalizationForm form)
    {
        System.ArgumentNullException.ThrowIfNull(parts);

        StringBuilder builder = new StringBuilder();
        int index = 0;

        while (index < parts.Length)
        {
            System.ArgumentNullException.ThrowIfNull(parts[index]);
            builder.Append(parts[index]);
            index = index + 1;
        }

        return Normalize(builder.ToString(), form);
    }

    /// <summary>
    /// Inserts U+034F when needed to limit runs to 30 non-starters.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string ToStreamSafe(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        StringBuilder builder = new StringBuilder(text.Length);
        int nonStarterCount = 0;
        int index = 0;

        while (index < scalars.Length)
        {
            int codePoint = scalars[index].CodePoint;
            int[] decomposition = DecomposeCodePointCompatibility(codePoint);
            int initialNonStarters = GetInitialNonStarterCount(decomposition);
            if (nonStarterCount + initialNonStarters > StreamSafeMaxNonStarters)
            {
                builder.Append(UnicodeScalar.Instance.ToUtf16String(CombiningGraphemeJoiner));
                nonStarterCount = 0;
            }

            int trailingNonStarters = GetTrailingNonStarterCount(decomposition);
            if (trailingNonStarters == decomposition.Length)
            {
                nonStarterCount = nonStarterCount + decomposition.Length;
            }
            else
            {
                nonStarterCount = trailingNonStarters;
            }

            builder.Append(UnicodeScalar.Instance.ToUtf16String(codePoint));
            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Reports whether no sequence contains more than 30 consecutive non-starters.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public bool IsStreamSafe(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        int nonStarterCount = 0;
        int index = 0;

        while (index < scalars.Length)
        {
            if (scalars[index].IsReplacement)
            {
                return false;
            }

            int[] decomposition = DecomposeCodePointCompatibility(scalars[index].CodePoint);
            int decompositionIndex = 0;
            while (decompositionIndex < decomposition.Length)
            {
                int combiningClass = UnicodeProperties.Instance.GetCombiningClass(decomposition[decompositionIndex]);
                if (combiningClass == 0)
                {
                    nonStarterCount = 0;
                }
                else
                {
                    nonStarterCount = nonStarterCount + 1;
                    if (nonStarterCount > StreamSafeMaxNonStarters)
                    {
                        return false;
                    }
                }

                decompositionIndex = decompositionIndex + 1;
            }

            index = index + 1;
        }

        return true;
    }

    /// <summary>
    /// Reports whether adjacent canonical decompositions remain in canonical order.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public bool IsFcd(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        int replacementIndex = 0;
        while (replacementIndex < scalars.Length)
        {
            if (scalars[replacementIndex].IsReplacement)
            {
                return false;
            }

            replacementIndex = replacementIndex + 1;
        }

        if (scalars.Length < 2)
        {
            return true;
        }

        int previousTrailingClass = GetTrailingCanonicalCombiningClass(scalars[0].CodePoint);
        int index = 1;

        while (index < scalars.Length)
        {
            int leadingClass = GetLeadingCanonicalCombiningClass(scalars[index].CodePoint);
            if (leadingClass != 0)
            {
                if (previousTrailingClass > leadingClass)
                {
                    return false;
                }
            }

            previousTrailingClass = GetTrailingCanonicalCombiningClass(scalars[index].CodePoint);
            index = index + 1;
        }

        return true;
    }

    /// <summary>
    /// Returns text whose adjacent canonical decompositions are in canonical order.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string ToFcd(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (IsFcd(text))
        {
            return text;
        }

        return DecomposeAndOrderCanonical(text);
    }

    /// <summary>
    /// Reports whether text is in Fast C Contiguous form.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public bool IsFcc(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return string.Equals(text, ToFcc(text), System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns Fast C Contiguous text by applying the library's FCC transformation.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string ToFcc(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return ComposeContiguousCanonical(DecomposeAndOrderCanonical(text));
    }

    internal static int GetInitialNonStarterCount(int[] decomposition)
    {
        System.ArgumentNullException.ThrowIfNull(decomposition);

        int count = 0;
        while (count < decomposition.Length &&
            UnicodeProperties.Instance.GetCombiningClass(decomposition[count]) != 0)
        {
            count = count + 1;
        }

        return count;
    }

    internal static int GetTrailingNonStarterCount(int[] decomposition)
    {
        System.ArgumentNullException.ThrowIfNull(decomposition);

        int count = 0;
        int index = decomposition.Length - 1;
        while (index >= 0 && UnicodeProperties.Instance.GetCombiningClass(decomposition[index]) != 0)
        {
            count = count + 1;
            index = index - 1;
        }

        return count;
    }

    internal static int GetLeadingCanonicalCombiningClass(int codePoint)
    {
        int[] decomposition = DecomposeCodePointCanonical(codePoint);
        if (decomposition.Length == 0)
        {
            return 0;
        }

        return UnicodeProperties.Instance.GetCombiningClass(decomposition[0]);
    }

    internal static int GetTrailingCanonicalCombiningClass(int codePoint)
    {
        int[] decomposition = DecomposeCodePointCanonical(codePoint);
        if (decomposition.Length == 0)
        {
            return 0;
        }

        return UnicodeProperties.Instance.GetCombiningClass(decomposition[decomposition.Length - 1]);
    }

    /// <summary>
    /// Reports whether text is in the selected Unicode normalization form.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="form">The Unicode normalization form to use.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public bool IsNormalized(string text, UnicodeNormalizationForm form)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeQuickCheckResult quickCheck = QuickCheck(text, form);
        if (quickCheck == UnicodeQuickCheckResult.Yes)
        {
            return true;
        }

        if (quickCheck == UnicodeQuickCheckResult.No)
        {
            return false;
        }

        return string.Equals(text, Normalize(text, form), System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns Yes, No, or Maybe for the selected normalization form without always producing normalized text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="form">The Unicode normalization form to use.</param>
    /// <returns>The resulting classification or status.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodeQuickCheckResult QuickCheck(string text, UnicodeNormalizationForm form)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        string formName = GetNormalizationFormName(form);
        bool hasMaybe = false;
        int previousClass = 0;
        int index = 0;

        while (index < scalars.Length)
        {
            if (scalars[index].IsReplacement)
            {
                return UnicodeQuickCheckResult.No;
            }

            int currentClass = UnicodeProperties.Instance.GetCombiningClass(scalars[index].CodePoint);
            if (currentClass != 0 && previousClass > currentClass)
            {
                return UnicodeQuickCheckResult.No;
            }

            string quickCheckValue = GetQuickCheckValue(scalars[index].CodePoint, formName);
            if (quickCheckValue == "N")
            {
                return UnicodeQuickCheckResult.No;
            }

            if (quickCheckValue == "M")
            {
                hasMaybe = true;
            }

            previousClass = currentClass;
            index = index + 1;
        }

        if (hasMaybe)
        {
            return UnicodeQuickCheckResult.Maybe;
        }

        return UnicodeQuickCheckResult.Yes;
    }

    /// <summary>
    /// Returns the UTF-16 length of the longest prefix whose quick-check result is Yes.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="form">The Unicode normalization form to use.</param>
    /// <returns>The resulting integer value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public int SpanQuickCheckYes(string text, UnicodeNormalizationForm form)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        string formName = GetNormalizationFormName(form);
        int span = 0;
        int previousClass = 0;
        int index = 0;

        while (index < scalars.Length)
        {
            if (scalars[index].IsReplacement)
            {
                break;
            }

            int currentClass = UnicodeProperties.Instance.GetCombiningClass(scalars[index].CodePoint);
            if (currentClass != 0 && previousClass > currentClass)
            {
                break;
            }

            if (GetQuickCheckValue(scalars[index].CodePoint, formName) != "Y")
            {
                break;
            }

            span = scalars[index].Utf16Index + scalars[index].Utf16Length;
            previousClass = currentClass;
            index = index + 1;
        }

        return span;
    }

    internal static string GetNormalizationFormName(UnicodeNormalizationForm form)
    {
        if (form == UnicodeNormalizationForm.Nfc)
        {
            return "NFC";
        }

        if (form == UnicodeNormalizationForm.Nfd)
        {
            return "NFD";
        }

        if (form == UnicodeNormalizationForm.Nfkc)
        {
            return "NFKC";
        }

        if (form == UnicodeNormalizationForm.Nfkd)
        {
            return "NFKD";
        }

        throw new System.ArgumentOutOfRangeException("form");
    }

    internal static string GetQuickCheckValue(int codePoint, string formName)
    {
        System.ArgumentNullException.ThrowIfNull(formName);

        int[] starts = UnicodeNormalizationTables.QuickCheckStarts();
        int[] ends = UnicodeNormalizationTables.QuickCheckEnds();
        int[] formIndexes = UnicodeNormalizationTables.QuickCheckFormIndexes();
        int[] valueIndexes = UnicodeNormalizationTables.QuickCheckValueIndexes();
        string[] formNames = UnicodeNormalizationTables.QuickCheckFormNames();
        string[] valueNames = UnicodeNormalizationTables.QuickCheckValueNames();
        int index = 0;

        while (index < starts.Length)
        {
            if (formNames[formIndexes[index]] == formName)
            {
                if (codePoint >= starts[index])
                {
                    if (codePoint <= ends[index])
                    {
                        return valueNames[valueIndexes[index]];
                    }
                }
            }

            index = index + 1;
        }

        return "Y";
    }

    internal static string DecomposeCanonical(string text)
    {
        return Decompose(text, false);
    }

    internal static string DecomposeCompatibility(string text)
    {
        return Decompose(text, true);
    }

    internal static string DecomposeAndOrderCanonical(string text)
    {
        return OrderCanonical(DecomposeCanonical(text));
    }

    internal static string DecomposeAndOrderCompatibility(string text)
    {
        return OrderCanonical(DecomposeCompatibility(text));
    }

    internal static string ComposeCanonical(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(OrderCanonical(text));
        List<int> codePoints = new List<int>();
        int index = 0;

        while (index < scalars.Length)
        {
            codePoints.Add(scalars[index].CodePoint);
            index = index + 1;
        }

        ComposeCanonicalInPlace(codePoints);
        return CodePointsToString(codePoints);
    }

    internal static string ComposeContiguousCanonical(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(OrderCanonical(text));
        List<int> codePoints = new List<int>();
        int index = 0;
        while (index < scalars.Length)
        {
            codePoints.Add(scalars[index].CodePoint);
            index = index + 1;
        }

        ComposeContiguousInPlace(codePoints);
        return CodePointsToString(codePoints);
    }

    internal static int[] ComposeCanonicalCodePoints(int[] codePoints)
    {
        int[] ordered = OrderCanonicalCodePoints(codePoints);
        List<int> composed = new List<int>();
        int index = 0;

        while (index < ordered.Length)
        {
            composed.Add(ordered[index]);
            index = index + 1;
        }

        ComposeCanonicalInPlace(composed);
        return composed.ToArray();
    }

    internal static void ComposeCanonicalInPlace(List<int> codePoints)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);

        List<int> composed = new List<int>();
        int starterIndex = -1;
        int previousClass = 0;
        int index = 0;

        while (index < codePoints.Count)
        {
            int codePoint = codePoints[index];
            int currentClass = UnicodeProperties.Instance.GetCombiningClass(codePoint);
            int composite = -1;

            if (starterIndex >= 0)
            {
                if (previousClass == 0 || previousClass < currentClass)
                {
                    composite = ComposeCanonicalPair(composed[starterIndex], codePoint);
                }
            }

            if (composite >= 0)
            {
                composed[starterIndex] = composite;
            }
            else
            {
                if (currentClass == 0)
                {
                    starterIndex = composed.Count;
                }

                composed.Add(codePoint);
                previousClass = currentClass;
            }

            index = index + 1;
        }

        codePoints.Clear();
        index = 0;

        while (index < composed.Count)
        {
            codePoints.Add(composed[index]);
            index = index + 1;
        }
    }

    internal static void ComposeContiguousInPlace(List<int> codePoints)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);

        List<int> composed = new List<int>();
        int starterIndex = -1;
        int previousClass = 0;
        int index = 0;
        while (index < codePoints.Count)
        {
            int codePoint = codePoints[index];
            int currentClass = UnicodeProperties.Instance.GetCombiningClass(codePoint);
            int composite = -1;
            if (starterIndex >= 0 && previousClass == 0)
            {
                composite = ComposeCanonicalPair(composed[starterIndex], codePoint);
            }

            if (composite >= 0)
            {
                composed[starterIndex] = composite;
            }
            else
            {
                if (currentClass == 0)
                {
                    starterIndex = composed.Count;
                }

                composed.Add(codePoint);
                previousClass = currentClass;
            }

            index = index + 1;
        }

        codePoints.Clear();
        index = 0;
        while (index < composed.Count)
        {
            codePoints.Add(composed[index]);
            index = index + 1;
        }
    }

    internal static int ComposeCanonicalPair(int starter, int codePoint)
    {
        int hangul = UnicodeHangul.Instance.ComposePair(starter, codePoint);
        if (hangul >= 0)
        {
            return hangul;
        }

        long key = GetCompositionKey(starter, codePoint);
        if (CanonicalCompositionPairs.ContainsKey(key))
        {
            return CanonicalCompositionPairs[key];
        }

        return -1;
    }

    internal static Dictionary<long, int> BuildCanonicalCompositionPairs()
    {
        Dictionary<long, int> pairs = new Dictionary<long, int>();
        int[] codePoints = UnicodeNormalizationTables.DecompositionCodePoints();
        int[] typeIndexes = UnicodeNormalizationTables.DecompositionTypeIndexes();
        string[] typeNames = UnicodeNormalizationTables.DecompositionTypeNames();
        int[] offsets = UnicodeNormalizationTables.DecompositionMappingOffsets();
        int[] lengths = UnicodeNormalizationTables.DecompositionMappingLengths();
        int[] mappings = UnicodeNormalizationTables.DecompositionMappingCodePoints();
        int index = 0;
        while (index < codePoints.Length)
        {
            if (typeNames[typeIndexes[index]].Length == 0 && lengths[index] == 2 &&
                !IsCompositionExcluded(codePoints[index]))
            {
                int offset = offsets[index];
                long key = GetCompositionKey(mappings[offset], mappings[offset + 1]);
                if (!pairs.ContainsKey(key))
                {
                    pairs.Add(key, codePoints[index]);
                }
            }

            index = index + 1;
        }

        return pairs;
    }

    internal static long GetCompositionKey(int starter, int codePoint)
    {
        return ((long)starter << 21) | (long)codePoint;
    }

    internal static bool IsCanonicalPairForComposition(int decompositionIndex, int starter, int codePoint)
    {
        string decompositionType = GetDecompositionType(decompositionIndex);
        if (decompositionType.Length > 0)
        {
            return false;
        }

        int[] mapping = GetDecompositionMapping(decompositionIndex);
        if (mapping.Length != 2)
        {
            return false;
        }

        if (mapping[0] != starter)
        {
            return false;
        }

        return mapping[1] == codePoint;
    }

    internal static bool IsCompositionExcluded(int codePoint)
    {
        UnicodeRangeValue[] exclusions = UnicodeNormalizationTables.CompositionExclusions();
        int index = 0;

        while (index < exclusions.Length)
        {
            if (exclusions[index].Contains(codePoint))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string OrderCanonical(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        List<int> codePoints = new List<int>();
        int index = 0;

        while (index < scalars.Length)
        {
            codePoints.Add(scalars[index].CodePoint);
            index = index + 1;
        }

        SortCanonicalOrder(codePoints);
        return CodePointsToString(codePoints);
    }

    internal static int[] OrderCanonicalCodePoints(int[] codePoints)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);

        List<int> ordered = new List<int>();
        int index = 0;

        while (index < codePoints.Length)
        {
            ordered.Add(codePoints[index]);
            index = index + 1;
        }

        SortCanonicalOrder(ordered);
        return ordered.ToArray();
    }

    internal static void SortCanonicalOrder(List<int> codePoints)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);

        int index = 1;

        while (index < codePoints.Count)
        {
            int currentClass = UnicodeProperties.Instance.GetCombiningClass(codePoints[index]);

            if (currentClass != 0)
            {
                int bubbleIndex = index;

                while (bubbleIndex > 0)
                {
                    int previousClass = UnicodeProperties.Instance.GetCombiningClass(codePoints[bubbleIndex - 1]);
                    if (previousClass == 0)
                    {
                        break;
                    }

                    if (previousClass <= currentClass)
                    {
                        break;
                    }

                    Swap(codePoints, bubbleIndex - 1, bubbleIndex);
                    bubbleIndex = bubbleIndex - 1;
                }
            }

            index = index + 1;
        }
    }

    internal static void Swap(List<int> values, int firstIndex, int secondIndex)
    {
        int first = values[firstIndex];
        values[firstIndex] = values[secondIndex];
        values[secondIndex] = first;
    }

    internal static int[] DecomposeCodePointCanonical(int codePoint)
    {
        return DecomposeCodePoint(codePoint, false);
    }

    internal static int[] DecomposeCodePointCompatibility(int codePoint)
    {
        return DecomposeCodePoint(codePoint, true);
    }

    internal static int[] DecomposeCodePoint(int codePoint, bool compatibility)
    {
        List<int> decomposed = new List<int>();
        AppendDecomposition(decomposed, codePoint, compatibility);
        return decomposed.ToArray();
    }

    internal static string Decompose(string text, bool compatibility)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        List<int> decomposed = new List<int>();
        int index = 0;

        while (index < scalars.Length)
        {
            AppendDecomposition(decomposed, scalars[index].CodePoint, compatibility);
            index = index + 1;
        }

        return CodePointsToString(decomposed);
    }

    internal static void AppendDecomposition(List<int> decomposed, int codePoint, bool compatibility)
    {
        System.ArgumentNullException.ThrowIfNull(decomposed);

        int[] hangul = UnicodeHangul.Instance.DecomposeSyllable(codePoint);
        if (hangul.Length > 0)
        {
            AppendRecursiveMapping(decomposed, hangul, compatibility);
            return;
        }

        int decompositionIndex = FindDecompositionIndex(codePoint);
        if (decompositionIndex < 0)
        {
            decomposed.Add(codePoint);
            return;
        }

        string decompositionType = GetDecompositionType(decompositionIndex);
        if (!compatibility)
        {
            if (decompositionType.Length > 0)
            {
                decomposed.Add(codePoint);
                return;
            }
        }

        AppendRecursiveMapping(decomposed, GetDecompositionMapping(decompositionIndex), compatibility);
    }

    internal static int FindDecompositionIndex(int codePoint)
    {
        int[] codePoints = UnicodeNormalizationTables.DecompositionCodePoints();
        int low = 0;
        int high = codePoints.Length - 1;
        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            if (codePoints[middle] == codePoint)
            {
                return middle;
            }

            if (codePoints[middle] < codePoint)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return -1;
    }

    internal static string GetDecompositionType(int decompositionIndex)
    {
        int[] typeIndexes = UnicodeNormalizationTables.DecompositionTypeIndexes();
        string[] typeNames = UnicodeNormalizationTables.DecompositionTypeNames();
        return typeNames[typeIndexes[decompositionIndex]];
    }

    internal static int[] GetDecompositionMapping(int decompositionIndex)
    {
        int[] offsets = UnicodeNormalizationTables.DecompositionMappingOffsets();
        int[] lengths = UnicodeNormalizationTables.DecompositionMappingLengths();
        int[] codePoints = UnicodeNormalizationTables.DecompositionMappingCodePoints();
        int length = lengths[decompositionIndex];
        int offset = offsets[decompositionIndex];
        int[] mapping = new int[length];
        int index = 0;

        while (index < length)
        {
            mapping[index] = codePoints[offset + index];
            index = index + 1;
        }

        return mapping;
    }

    internal static void AppendRecursiveMapping(List<int> decomposed, int[] mapping, bool compatibility)
    {
        System.ArgumentNullException.ThrowIfNull(decomposed);
        System.ArgumentNullException.ThrowIfNull(mapping);

        int index = 0;

        while (index < mapping.Length)
        {
            AppendDecomposition(decomposed, mapping[index], compatibility);
            index = index + 1;
        }
    }

    internal static string CodePointsToString(List<int> codePoints)
    {
        System.ArgumentNullException.ThrowIfNull(codePoints);

        StringBuilder builder = new StringBuilder();
        int index = 0;

        while (index < codePoints.Count)
        {
            builder.Append(UnicodeScalar.Instance.ToUtf16String(codePoints[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static int[] DecomposeHangulSyllable(int codePoint)
    {
        return UnicodeHangul.Instance.DecomposeSyllable(codePoint);
    }

    internal static int ComposeHangulPair(int first, int second)
    {
        return UnicodeHangul.Instance.ComposePair(first, second);
    }

    internal static int ComposeHangulSyllable(int leadingJamo, int vowelJamo)
    {
        return UnicodeHangul.Instance.ComposeJamoPair(leadingJamo, vowelJamo);
    }

    internal static int ComposeHangulSyllable(int leadingJamo, int vowelJamo, int trailingJamo)
    {
        return UnicodeHangul.Instance.ComposeJamoSequence(leadingJamo, vowelJamo, trailingJamo);
    }
}
