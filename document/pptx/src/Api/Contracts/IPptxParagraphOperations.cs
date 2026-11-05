using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxParagraphOperations"/>.
/// </summary>
public interface IPptxParagraphOperations
{
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int RunCount(PptxParagraph target);
    /// <summary>
    /// Returns the run without changing the paragraph.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting run value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxRun GetRun(PptxParagraph target, int index);
    /// <summary>
    /// Returns the text without changing the paragraph.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetText(PptxParagraph target);
    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetText(PptxParagraph target, string text);
    /// <summary>
    /// Adds the run and returns the created model when applicable.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <returns>The resulting run value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxRun AddRun(PptxParagraph target, string text);
    /// <summary>
    /// Adds the line break and returns the created model when applicable.
    /// </summary>
    /// <returns>The resulting run value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxRun AddLineBreak(PptxParagraph target);
    /// <summary>
    /// Adds the tab and returns the created model when applicable.
    /// </summary>
    /// <returns>The resulting run value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxRun AddTab(PptxParagraph target);
    /// <summary>
    /// Adds an editable DrawingML field.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="fieldId">The nonempty field identifier.</param>
    /// <param name="fieldType">The nonempty DrawingML field type.</param>
    /// <param name="text">The cached field text.</param>
    /// <returns>The created field run.</returns>
    PptxRun AddField(PptxParagraph target, string fieldId, string fieldType, string text);
    /// <summary>
    /// Adds a text run using a symbol typeface.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="symbol">The symbol text.</param>
    /// <param name="typeface">The symbol typeface.</param>
    /// <returns>The created symbol run.</returns>
    PptxRun AddSymbol(PptxParagraph target, string symbol, string typeface);
    /// <summary>
    /// Replaces text while preserving unrelated presentation data.
    /// </summary>
    /// <param name="search">The exact text to find. An empty value is not accepted.</param>
    /// <param name="replacement">The text that replaces each match.</param>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int ReplaceText(PptxParagraph target, string search, string replacement);
    /// <summary>
    /// Inspects direct typed paragraph formatting without changing the paragraph.
    /// </summary>
    /// <param name="target">The paragraph to inspect.</param>
    /// <returns>The direct paragraph format.</returns>
    PptxParagraphFormat GetFormat(PptxParagraph target);
    /// <summary>
    /// Updates horizontal paragraph alignment.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="alignment">A DrawingML alignment or supported friendly name.</param>
    void SetAlignment(PptxParagraph target, string alignment);
    /// <summary>
    /// Updates outline level, margins, and first-line indentation.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="level">The outline level from zero through eight.</param>
    /// <param name="marginLeft">The nonnegative left margin in English Metric Units.</param>
    /// <param name="marginRight">The nonnegative right margin in English Metric Units.</param>
    /// <param name="indent">The signed first-line indentation in English Metric Units.</param>
    void SetIndentation(PptxParagraph target, int level, long marginLeft, long marginRight, long indent);
    /// <summary>
    /// Updates line, before, and after spacing.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="line">Line spacing.</param>
    /// <param name="before">Spacing before the paragraph.</param>
    /// <param name="after">Spacing after the paragraph.</param>
    void SetSpacing(PptxParagraph target, PptxTextSpacing line, PptxTextSpacing before, PptxTextSpacing after);
    /// <summary>
    /// Replaces the direct tab-stop list while preserving other paragraph properties.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="tabs">The ordered tab stops.</param>
    void SetTabs(PptxParagraph target, List<PptxTextTab> tabs);
    /// <summary>
    /// Updates the default tab interval.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="size">The nonnegative interval in English Metric Units.</param>
    void SetDefaultTabSize(PptxParagraph target, long size);
    /// <summary>
    /// Updates direct default run properties.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="font">The default font formatting.</param>
    void SetDefaultRunProperties(PptxParagraph target, PptxFont font);
    /// <summary>
    /// Uses a character bullet while retaining bullet font, size, and color settings.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="character">The nonempty bullet character.</param>
    void SetCharacterBullet(PptxParagraph target, string character);
    /// <summary>
    /// Uses an automatic numbering scheme.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    /// <param name="numberingType">The nonempty DrawingML numbering scheme.</param>
    /// <param name="startAt">The starting number from one through 32767.</param>
    void SetAutomaticNumbering(PptxParagraph target, string numberingType, int startAt);
    /// <summary>
    /// Disables bullets and numbering.
    /// </summary>
    /// <param name="target">The paragraph to update.</param>
    void ClearBullet(PptxParagraph target);
}
