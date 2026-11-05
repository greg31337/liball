using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxRunOperations"/>.
/// </summary>
public interface IPptxRunOperations
{
    /// <summary>
    /// Reports whether the run is text.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsText(PptxRun target);
    /// <summary>
    /// Reports whether the run is field.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsField(PptxRun target);
    /// <summary>
    /// Reports whether the run is line break.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsLineBreak(PptxRun target);
    /// <summary>
    /// Reports whether the run is tab.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsTab(PptxRun target);
    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetText(PptxRun target, string text);
    /// <summary>
    /// Replaces text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int ReplaceText(PptxRun target, string search, string replacement);
    /// <summary>
    /// Returns a copy of the direct run font formatting.
    /// </summary>
    /// <param name="target">The run to inspect.</param>
    /// <returns>The direct font formatting.</returns>
    PptxFont GetFont(PptxRun target);
    /// <summary>
    /// Replaces all modeled direct run font formatting while preserving unmodeled children.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="font">The direct font formatting.</param>
    void SetFont(PptxRun target, PptxFont font);
    /// <summary>
    /// Updates Latin, East Asian, and complex-script families.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="latin">The Latin typeface.</param>
    /// <param name="eastAsian">The East Asian typeface.</param>
    /// <param name="complexScript">The complex-script typeface.</param>
    void SetFamilies(PptxRun target, string latin, string eastAsian, string complexScript);
    /// <summary>
    /// Updates the font size in points.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="points">The nonnegative font size.</param>
    void SetSize(PptxRun target, double points);
    /// <summary>
    /// Updates bold and italic emphasis.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="bold">Whether bold is enabled.</param>
    /// <param name="italic">Whether italic is enabled.</param>
    void SetEmphasis(PptxRun target, bool bold, bool italic);
    /// <summary>
    /// Updates underline and strike formatting.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="underline">The DrawingML underline style or a supported friendly value.</param>
    /// <param name="strike">The DrawingML strike style or a supported friendly value.</param>
    void SetDecorations(PptxRun target, string underline, string strike);
    /// <summary>
    /// Updates baseline in thousandths of one percent.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="baseline">The signed baseline adjustment.</param>
    void SetBaseline(PptxRun target, int baseline);
    /// <summary>
    /// Updates capitalization.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="capitalization"><c>none</c>, <c>small</c>, <c>all</c>, or an empty inherited value.</param>
    void SetCapitalization(PptxRun target, string capitalization);
    /// <summary>
    /// Updates minimum kerning size in hundredths of one point.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="kerning">The nonnegative kerning threshold.</param>
    void SetKerning(PptxRun target, int kerning);
    /// <summary>
    /// Updates signed character spacing in hundredths of one point.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="spacing">The signed character spacing.</param>
    void SetCharacterSpacing(PptxRun target, int spacing);
    /// <summary>
    /// Updates primary and alternative languages.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="language">The primary language tag.</param>
    /// <param name="alternativeLanguage">The alternative language tag.</param>
    void SetLanguage(PptxRun target, string language, string alternativeLanguage);
    /// <summary>
    /// Updates the complete direct text fill.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="fill">The text fill.</param>
    void SetFill(PptxRun target, PptxFill fill);
    /// <summary>
    /// Updates the direct highlight color, or clears it for an empty color.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="color">The highlight color.</param>
    void SetHighlight(PptxRun target, PptxColor color);
    /// <summary>
    /// Updates the symbol typeface.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="typeface">The symbol typeface, or an empty value to clear it.</param>
    void SetSymbol(PptxRun target, string typeface);
    /// <summary>
    /// Updates an existing field identifier and type.
    /// </summary>
    /// <param name="target">The field run to update.</param>
    /// <param name="fieldId">The nonempty field identifier.</param>
    /// <param name="fieldType">The nonempty field type.</param>
    void SetField(PptxRun target, string fieldId, string fieldType);
    /// <summary>
    /// Adds or updates an external run-level hyperlink.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="url">The external URL or file URI.</param>
    void SetExternalHyperlink(PptxRun target, string url);
    /// <summary>
    /// Returns the run-level hyperlink without changing it.
    /// </summary>
    /// <returns>The run hyperlink model.</returns>
    /// <param name="target">The run to inspect.</param>
    PptxHyperlink GetHyperlink(PptxRun target);
    /// <summary>
    /// Adds or updates an external file hyperlink.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="fileUri">The external file URI.</param>
    void SetFileHyperlink(PptxRun target, string fileUri);
    /// <summary>
    /// Adds or updates an internal slide hyperlink.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="slideIndex">The zero-based target slide index.</param>
    void SetSlideHyperlink(PptxRun target, int slideIndex);
    /// <summary>
    /// Adds or updates a slide navigation action.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="navigation">The navigation type.</param>
    void SetNavigationAction(PptxRun target, string navigation);
    /// <summary>
    /// Adds or updates a custom-show action.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="customShowName">The custom show name.</param>
    void SetCustomShowAction(PptxRun target, string customShowName);
    /// <summary>
    /// Adds or updates a raw preserved PowerPoint action.
    /// </summary>
    /// <param name="target">The run to update.</param>
    /// <param name="action">The complete action URI.</param>
    void SetAction(PptxRun target, string action);
    /// <summary>
    /// Removes a run-level hyperlink and its relationship.
    /// </summary>
    /// <param name="target">The run to update.</param>
    void RemoveHyperlink(PptxRun target);
    /// <summary>
    /// Normalizes line endings, invalid XML characters, and Unicode composition.
    /// </summary>
    /// <param name="text">The text to normalize.</param>
    /// <returns>The normalized text.</returns>
    string NormalizeText(string text);
    /// <summary>
    /// Normalizes the text of an editable run in place.
    /// </summary>
    /// <param name="target">The run to normalize.</param>
    void NormalizeRun(PptxRun target);
}
