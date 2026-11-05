namespace unicode;

/// <summary>
/// Provides Unicode case mapping, folding, and comparison operations.
/// </summary>
public interface IUnicodeCase
{
    /// <summary>
    /// Applies full Unicode uppercase mapping to text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string ToUpper(string text);

    /// <summary>
    /// Applies full Unicode lowercase mapping to text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string ToLower(string text);

    /// <summary>
    /// Applies full uppercase mapping with Turkish, Azeri, or Lithuanian special casing when selected.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="locale">A locale tag; Turkish, Azeri, and Lithuanian language subtags enable Unicode special casing.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>locale</c> is <c>null</c>.</exception>
    string ToUpperLocale(string text, string locale);

    /// <summary>
    /// Applies full lowercase mapping with Turkish, Azeri, or Lithuanian special casing when selected.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="locale">A locale tag; Turkish, Azeri, and Lithuanian language subtags enable Unicode special casing.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>locale</c> is <c>null</c>.</exception>
    string ToLowerLocale(string text, string locale);

    /// <summary>
    /// Applies full Unicode titlecase mapping independently to every scalar in the text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string ToTitle(string text);

    /// <summary>
    /// Applies full Unicode titlecase mapping with Turkish, Azeri, or Lithuanian special casing when selected.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="locale">A locale tag; Turkish, Azeri, and Lithuanian language subtags enable Unicode special casing.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>locale</c> is <c>null</c>.</exception>
    string ToTitleLocale(string text, string locale);

    /// <summary>
    /// Titlecases the first cased character of each default Unicode word and lowercases subsequent characters in that word.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string ToTitleWords(string text);

    /// <summary>
    /// Titlecases default Unicode words with Turkish, Azeri, or Lithuanian special casing when selected.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="locale">A locale tag; Turkish, Azeri, and Lithuanian language subtags enable Unicode special casing.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>locale</c> is <c>null</c>.</exception>
    string ToTitleWordsLocale(string text, string locale);

    /// <summary>
    /// Titlecases the first cased character of each extended grapheme cluster and lowercases subsequent characters in that cluster.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string ToTitleGraphemes(string text);

    /// <summary>
    /// Titlecases extended grapheme clusters with Turkish, Azeri, or Lithuanian special casing when selected.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="locale">A locale tag; Turkish, Azeri, and Lithuanian language subtags enable Unicode special casing.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>text</c>, <c>locale</c> is <c>null</c>.</exception>
    string ToTitleGraphemesLocale(string text, string locale);

    /// <summary>
    /// Applies full default Unicode case folding to text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string Fold(string text);

    /// <summary>
    /// Applies full Unicode Turkic case folding to text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string FoldTurkic(string text);

    /// <summary>
    /// Applies NFKC case folding for compatibility-based caseless matching.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string ToNfkcCaseFold(string text);

    /// <summary>
    /// Normalizes and folds text into a stable key suitable for ordinal caseless comparison.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="mode">The case-mapping or comparison-key mode to use.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>mode</c> is outside the supported range.</exception>
    string CompareKey(string text, UnicodeCompareKeyMode mode);

    /// <summary>
    /// Reports whether two strings are ordinally equal after full Unicode case folding.
    /// </summary>
    /// <param name="left">The first UTF-16 string.</param>
    /// <param name="right">The second UTF-16 string.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>left</c>, <c>right</c> is <c>null</c>.</exception>
    bool EqualsFolded(string left, string right);

    /// <summary>
    /// Applies simple Unicode uppercase mapping to one scalar.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting integer value.</returns>
    int ToUpperScalar(int codePoint);

    /// <summary>
    /// Applies simple Unicode lowercase mapping to one scalar.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting integer value.</returns>
    int ToLowerScalar(int codePoint);

    /// <summary>
    /// Applies simple Unicode titlecase mapping to one scalar.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting integer value.</returns>
    int ToTitleScalar(int codePoint);

    /// <summary>
    /// Applies simple default Unicode case folding to one scalar.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting integer value.</returns>
    int FoldScalar(int codePoint);

    /// <summary>
    /// Applies simple Unicode Turkic case folding to one scalar.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns>The resulting integer value.</returns>
    int FoldScalarTurkic(int codePoint);
}
