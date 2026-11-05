using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxRun values.
/// </summary>
public class PptxRunOperations : IPptxRunOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxRunOperations()
    {
    }

    /// <summary>
    /// Reports whether the run is text.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsText(PptxRun target)
    {
        return target.IsText();
    }

    /// <summary>
    /// Reports whether the run is field.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsField(PptxRun target)
    {
        return target.IsField();
    }

    /// <summary>
    /// Reports whether the run is line break.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsLineBreak(PptxRun target)
    {
        return target.IsLineBreak();
    }

    /// <summary>
    /// Reports whether the run is tab.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsTab(PptxRun target)
    {
        return target.IsTab();
    }

    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetText(PptxRun target, string text)
    {
        target.SetText(text);
    }

    /// <summary>
    /// Replaces text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int ReplaceText(PptxRun target, string search, string replacement)
    {
        return target.ReplaceText(search, replacement);
    }

    /// <summary>Returns a copy of direct run font formatting.</summary>
    /// <param name="target">The run to inspect.</param>
    /// <returns>The direct font formatting.</returns>
    public PptxFont GetFont(PptxRun target)
    {
        return PptxRunFormatCore.CloneFont(target.Font);
    }

    /// <summary>Replaces modeled direct run font formatting.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="font">The direct font formatting.</param>
    public void SetFont(PptxRun target, PptxFont font)
    {
        PptxRunFormatCore.SetFont(target, font);
    }

    /// <summary>Updates Latin, East Asian, and complex-script families.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="latin">The Latin typeface.</param>
    /// <param name="eastAsian">The East Asian typeface.</param>
    /// <param name="complexScript">The complex-script typeface.</param>
    public void SetFamilies(PptxRun target, string latin, string eastAsian, string complexScript)
    {
        PptxRunFormatCore.SetFamilies(target, latin, eastAsian, complexScript);
    }

    /// <summary>Updates font size in points.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="points">The nonnegative font size.</param>
    public void SetSize(PptxRun target, double points)
    {
        PptxRunFormatCore.SetSize(target, points);
    }

    /// <summary>Updates bold and italic emphasis.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="bold">Whether bold is enabled.</param>
    /// <param name="italic">Whether italic is enabled.</param>
    public void SetEmphasis(PptxRun target, bool bold, bool italic)
    {
        PptxRunFormatCore.SetEmphasis(target, bold, italic);
    }

    /// <summary>Updates underline and strike formatting.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="underline">The underline style.</param>
    /// <param name="strike">The strike style.</param>
    public void SetDecorations(PptxRun target, string underline, string strike)
    {
        PptxRunFormatCore.SetDecorations(target, underline, strike);
    }

    /// <summary>Updates baseline.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="baseline">The signed baseline adjustment.</param>
    public void SetBaseline(PptxRun target, int baseline)
    {
        PptxRunFormatCore.SetBaseline(target, baseline);
    }

    /// <summary>Updates capitalization.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="capitalization">The capitalization mode.</param>
    public void SetCapitalization(PptxRun target, string capitalization)
    {
        PptxRunFormatCore.SetCapitalization(target, capitalization);
    }

    /// <summary>Updates minimum kerning size.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="kerning">The nonnegative kerning threshold.</param>
    public void SetKerning(PptxRun target, int kerning)
    {
        PptxRunFormatCore.SetKerning(target, kerning);
    }

    /// <summary>Updates signed character spacing.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="spacing">The signed character spacing.</param>
    public void SetCharacterSpacing(PptxRun target, int spacing)
    {
        PptxRunFormatCore.SetCharacterSpacing(target, spacing);
    }

    /// <summary>Updates primary and alternative languages.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="language">The primary language tag.</param>
    /// <param name="alternativeLanguage">The alternative language tag.</param>
    public void SetLanguage(PptxRun target, string language, string alternativeLanguage)
    {
        PptxRunFormatCore.SetLanguage(target, language, alternativeLanguage);
    }

    /// <summary>Updates the complete direct text fill.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="fill">The text fill.</param>
    public void SetFill(PptxRun target, PptxFill fill)
    {
        PptxRunFormatCore.SetFill(target, fill);
    }

    /// <summary>Updates or clears the direct highlight color.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="color">The highlight color.</param>
    public void SetHighlight(PptxRun target, PptxColor color)
    {
        PptxRunFormatCore.SetHighlight(target, color);
    }

    /// <summary>Updates the symbol typeface.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="typeface">The symbol typeface.</param>
    public void SetSymbol(PptxRun target, string typeface)
    {
        PptxRunFormatCore.SetSymbol(target, typeface);
    }

    /// <summary>Updates an existing field identifier and type.</summary>
    /// <param name="target">The field run to update.</param>
    /// <param name="fieldId">The nonempty field identifier.</param>
    /// <param name="fieldType">The nonempty field type.</param>
    public void SetField(PptxRun target, string fieldId, string fieldType)
    {
        PptxRunFormatCore.SetField(target, fieldId, fieldType);
    }

    /// <summary>Adds or updates an external run-level hyperlink.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="url">The external URL or file URI.</param>
    public void SetExternalHyperlink(PptxRun target, string url)
    {
        PptxRunFormatCore.SetExternalHyperlink(target, url);
    }
    /// <summary>Returns the run-level hyperlink without changing it.</summary>
    /// <returns>The run hyperlink model.</returns>
    /// <param name="target">The run to inspect.</param>
    public PptxHyperlink GetHyperlink(PptxRun target)
    {
        return target.GetHyperlink();
    }
    /// <summary>Adds or updates an external file hyperlink.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="fileUri">The external file URI.</param>
    public void SetFileHyperlink(PptxRun target, string fileUri)
    {
        PptxRunFormatCore.SetFileHyperlink(target, fileUri);
    }
    /// <summary>Adds or updates an internal slide hyperlink.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="slideIndex">The zero-based target slide index.</param>
    public void SetSlideHyperlink(PptxRun target, int slideIndex)
    {
        PptxRunFormatCore.SetSlideHyperlink(target, slideIndex);
    }
    /// <summary>Adds or updates a slide navigation action.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="navigation">The navigation type.</param>
    public void SetNavigationAction(PptxRun target, string navigation)
    {
        PptxRunFormatCore.SetNavigationAction(target, navigation);
    }
    /// <summary>Adds or updates a custom-show action.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="customShowName">The custom show name.</param>
    public void SetCustomShowAction(PptxRun target, string customShowName)
    {
        PptxRunFormatCore.SetCustomShowAction(target, customShowName);
    }
    /// <summary>Adds or updates a raw preserved PowerPoint action.</summary>
    /// <param name="target">The run to update.</param>
    /// <param name="action">The complete action URI.</param>
    public void SetAction(PptxRun target, string action)
    {
        PptxRunFormatCore.SetAction(target, action);
    }

    /// <summary>Removes a run-level hyperlink and its relationship.</summary>
    /// <param name="target">The run to update.</param>
    public void RemoveHyperlink(PptxRun target)
    {
        PptxRunFormatCore.RemoveHyperlink(target);
    }

    /// <summary>Normalizes line endings, invalid XML characters, and Unicode composition.</summary>
    /// <param name="text">The text to normalize.</param>
    /// <returns>The normalized text.</returns>
    public string NormalizeText(string text)
    {
        return PptxRunFormatCore.NormalizeTextValue(text);
    }

    /// <summary>Normalizes editable run text in place.</summary>
    /// <param name="target">The run to normalize.</param>
    public void NormalizeRun(PptxRun target)
    {
        PptxRunFormatCore.NormalizeRun(target);
    }
}
