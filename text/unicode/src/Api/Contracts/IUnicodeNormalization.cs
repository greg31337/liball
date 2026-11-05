namespace unicode;

/// <summary>
/// Provides Unicode normalization and stream-safety operations.
/// </summary>
public interface IUnicodeNormalization
{
    /// <summary>
    /// Applies canonical or compatibility decomposition, ordering, and optional composition for the selected form.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="form">The Unicode normalization form to use.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>form</c> is outside the supported range.</exception>
    string Normalize(string text, UnicodeNormalizationForm form);

    /// <summary>
    /// Normalizes text to canonical decomposition followed by canonical composition (NFC).
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string ToNfc(string text);

    /// <summary>
    /// Normalizes text to fully ordered canonical decomposition (NFD).
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string ToNfd(string text);

    /// <summary>
    /// Normalizes text with compatibility decomposition followed by canonical composition (NFKC).
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string ToNfkc(string text);

    /// <summary>
    /// Normalizes text to fully ordered compatibility decomposition (NFKD).
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string ToNfkd(string text);

    /// <summary>
    /// Concatenates two strings and repairs normalization only where their boundary can affect the selected form.
    /// </summary>
    /// <param name="first">The first value or normalized text segment.</param>
    /// <param name="second">The second value or text segment.</param>
    /// <param name="form">The Unicode normalization form to use.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any of <c>first</c>, <c>second</c> is <c>null</c>.</exception>
    string Append(string first, string second, UnicodeNormalizationForm form);

    /// <summary>
    /// Concatenates all parts and normalizes the result in the selected form.
    /// </summary>
    /// <param name="parts">The text segments to concatenate and normalize.</param>
    /// <param name="form">The Unicode normalization form to use.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>parts</c> is <c>null</c>.</exception>
    string Concat(string[] parts, UnicodeNormalizationForm form);

    /// <summary>
    /// Inserts U+034F when needed to limit runs to 30 non-starters.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string ToStreamSafe(string text);

    /// <summary>
    /// Reports whether no sequence contains more than 30 consecutive non-starters.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    bool IsStreamSafe(string text);

    /// <summary>
    /// Reports whether adjacent canonical decompositions remain in canonical order.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    bool IsFcd(string text);

    /// <summary>
    /// Returns text whose adjacent canonical decompositions are in canonical order.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string ToFcd(string text);

    /// <summary>
    /// Reports whether text is in Fast C Contiguous form.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    bool IsFcc(string text);

    /// <summary>
    /// Returns Fast C Contiguous text by applying the library's FCC transformation.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string ToFcc(string text);

    /// <summary>
    /// Reports whether text is in the selected Unicode normalization form.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="form">The Unicode normalization form to use.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    bool IsNormalized(string text, UnicodeNormalizationForm form);

    /// <summary>
    /// Returns Yes, No, or Maybe for the selected normalization form without always producing normalized text.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="form">The Unicode normalization form to use.</param>
    /// <returns>The resulting classification or status.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodeQuickCheckResult QuickCheck(string text, UnicodeNormalizationForm form);

    /// <summary>
    /// Returns the UTF-16 length of the longest prefix whose quick-check result is Yes.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="form">The Unicode normalization form to use.</param>
    /// <returns>The resulting integer value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    int SpanQuickCheckYes(string text, UnicodeNormalizationForm form);
}

