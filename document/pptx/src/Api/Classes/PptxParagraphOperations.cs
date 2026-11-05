using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxParagraph values.
/// </summary>
public class PptxParagraphOperations : IPptxParagraphOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxParagraphOperations()
    {
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int RunCount(PptxParagraph target)
    {
        return target.RunCount();
    }

    /// <summary>
    /// Returns the run without changing the paragraph.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting run value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxRun GetRun(PptxParagraph target, int index)
    {
        return target.GetRun(index);
    }

    /// <summary>
    /// Returns the text without changing the paragraph.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetText(PptxParagraph target)
    {
        return target.GetText();
    }

    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetText(PptxParagraph target, string text)
    {
        target.SetText(text);
    }

    /// <summary>
    /// Adds the run and returns the created model when applicable.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <returns>The resulting run value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxRun AddRun(PptxParagraph target, string text)
    {
        return target.AddRun(text);
    }

    /// <summary>
    /// Adds the line break and returns the created model when applicable.
    /// </summary>
    /// <returns>The resulting run value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxRun AddLineBreak(PptxParagraph target)
    {
        return target.AddLineBreak();
    }

    /// <summary>
    /// Adds the tab and returns the created model when applicable.
    /// </summary>
    /// <returns>The resulting run value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxRun AddTab(PptxParagraph target)
    {
        return target.AddTab();
    }

    /// <summary>
    /// Adds an editable DrawingML field.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="fieldId">The nonempty field identifier.</param>
    /// <param name="fieldType">The nonempty DrawingML field type.</param>
    /// <param name="text">The cached field text.</param>
    /// <returns>The created field run.</returns>
    public PptxRun AddField(PptxParagraph target, string fieldId, string fieldType, string text)
    {
        return target.AddField(fieldId, fieldType, text);
    }

    /// <summary>
    /// Adds a text run using a symbol typeface.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="symbol">The symbol text.</param>
    /// <param name="typeface">The symbol typeface.</param>
    /// <returns>The created symbol run.</returns>
    public PptxRun AddSymbol(PptxParagraph target, string symbol, string typeface)
    {
        return target.AddSymbol(symbol, typeface);
    }

    /// <summary>
    /// Replaces text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int ReplaceText(PptxParagraph target, string search, string replacement)
    {
        return target.ReplaceText(search, replacement);
    }

    /// <summary>
    /// Inspects direct typed paragraph formatting without changing the paragraph.
    /// </summary>
    /// <param name="target">The paragraph to inspect.</param>
    /// <returns>The direct paragraph format.</returns>
    public PptxParagraphFormat GetFormat(PptxParagraph target)
    {
        return target.GetFormat();
    }

    /// <summary>
    /// Updates horizontal paragraph alignment.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="alignment">A DrawingML alignment or supported friendly name.</param>
    public void SetAlignment(PptxParagraph target, string alignment)
    {
        target.SetAlignment(alignment);
    }

    /// <summary>
    /// Updates outline level, margins, and first-line indentation.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="level">The outline level from zero through eight.</param>
    /// <param name="marginLeft">The nonnegative left margin in English Metric Units.</param>
    /// <param name="marginRight">The nonnegative right margin in English Metric Units.</param>
    /// <param name="indent">The signed first-line indentation in English Metric Units.</param>
    public void SetIndentation(PptxParagraph target, int level, long marginLeft, long marginRight, long indent)
    {
        target.SetIndentation(level, marginLeft, marginRight, indent);
    }

    /// <summary>
    /// Updates line, before, and after spacing.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="line">Line spacing.</param>
    /// <param name="before">Spacing before the paragraph.</param>
    /// <param name="after">Spacing after the paragraph.</param>
    public void SetSpacing(PptxParagraph target, PptxTextSpacing line, PptxTextSpacing before, PptxTextSpacing after)
    {
        target.SetSpacing(line, before, after);
    }

    /// <summary>
    /// Replaces the direct tab-stop list while preserving other paragraph properties.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="tabs">The ordered tab stops.</param>
    public void SetTabs(PptxParagraph target, List<PptxTextTab> tabs)
    {
        target.SetTabs(tabs);
    }

    /// <summary>
    /// Updates the default tab interval.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="size">The nonnegative interval in English Metric Units.</param>
    public void SetDefaultTabSize(PptxParagraph target, long size)
    {
        target.SetDefaultTabSize(size);
    }

    /// <summary>
    /// Updates direct default run properties.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="font">The default font formatting.</param>
    public void SetDefaultRunProperties(PptxParagraph target, PptxFont font)
    {
        target.SetDefaultRunProperties(font);
    }

    /// <summary>
    /// Uses a character bullet while retaining bullet font, size, and color settings.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="character">The nonempty bullet character.</param>
    public void SetCharacterBullet(PptxParagraph target, string character)
    {
        target.SetCharacterBullet(character);
    }

    /// <summary>
    /// Uses an automatic numbering scheme.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="numberingType">The nonempty DrawingML numbering scheme.</param>
    /// <param name="startAt">The starting number from one through 32767.</param>
    public void SetAutomaticNumbering(PptxParagraph target, string numberingType, int startAt)
    {
        target.SetAutomaticNumbering(numberingType, startAt);
    }

    /// <summary>
    /// Disables bullets and numbering.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    public void ClearBullet(PptxParagraph target)
    {
        target.ClearBullet();
    }
}
