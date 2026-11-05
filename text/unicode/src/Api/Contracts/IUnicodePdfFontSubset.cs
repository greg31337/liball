namespace unicode;

/// <summary>
/// Provides Unicode scalar and script collection for PDF font subsetting.
/// </summary>
public interface IUnicodePdfFontSubset
{
    /// <summary>
    /// Returns each distinct valid Unicode scalar used by the text, in first-seen order.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    int[] CollectScalars(string text);

    /// <summary>
    /// Returns the significant scripts used by the text, in first-seen order.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodeScript[] CollectScripts(string text);

    /// <summary>
    /// Groups distinct input scalars by their resolved script for font-subset planning.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodePdfScriptScalarSet[] CollectScriptScalarSets(string text);

    /// <summary>
    /// Returns each distinct scalar assigned to the requested script, in first-seen order.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="script">The Unicode script to select.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    int[] CollectScalarsForScript(string text, UnicodeScript script);

    /// <summary>
    /// Creates a grapheme-safe PDF font-subset plan using caller-provided font coverage.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="coverage">The font coverage callback.</param>
    /// <returns>The cluster-safe subset plan.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when any argument is <c>null</c>.</exception>
    UnicodePdfFontSubsetPlan CreateSubsetPlan(string text, IUnicodePdfFontCoverage coverage);
}
