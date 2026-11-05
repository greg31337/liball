namespace docx;

/// <summary>
/// Provides numbering discovery, creation, editing, application, continuation, restart, and label operations.
/// </summary>
public interface IDocxNumberingManagement
{
    /// <summary>
    /// Returns all abstract numbering definitions in document order.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <returns>A new list containing the abstract definitions.</returns>
    List<DocxAbstractNumbering> GetAbstractNumberings(DocxNumbering numbering);

    /// <summary>
    /// Returns all numbering instances in document order.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <returns>A new list containing the numbering instances.</returns>
    List<DocxNumberingInstance> GetNumberingInstances(DocxNumbering numbering);

    /// <summary>
    /// Returns all levels from an abstract numbering definition.
    /// </summary>
    /// <param name="numbering">The abstract numbering definition to inspect.</param>
    /// <returns>A new list containing the levels.</returns>
    List<DocxNumberingLevel> GetLevels(DocxAbstractNumbering numbering);

    /// <summary>
    /// Returns all level overrides from a numbering instance.
    /// </summary>
    /// <param name="instance">The numbering instance to inspect.</param>
    /// <returns>A new list containing the overrides.</returns>
    List<DocxNumberingLevelOverride> GetLevelOverrides(DocxNumberingInstance instance);

    /// <summary>
    /// Finds an abstract numbering definition by identifier.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <param name="abstractNumberingId">The abstract numbering identifier.</param>
    /// <returns>The matching definition, or null.</returns>
    DocxAbstractNumbering? FindAbstractNumbering(DocxNumbering numbering, string abstractNumberingId);

    /// <summary>
    /// Finds a numbering instance by identifier.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <param name="numberingId">The numbering identifier.</param>
    /// <returns>The matching instance, or null.</returns>
    DocxNumberingInstance? FindNumberingInstance(DocxNumbering numbering, string numberingId);

    /// <summary>
    /// Finds a level by its zero-based level index.
    /// </summary>
    /// <param name="numbering">The abstract numbering definition to inspect.</param>
    /// <param name="levelIndex">The level index.</param>
    /// <returns>The matching level, or null.</returns>
    DocxNumberingLevel? FindLevel(DocxAbstractNumbering numbering, string levelIndex);

    /// <summary>
    /// Finds a level override by its zero-based level index.
    /// </summary>
    /// <param name="instance">The numbering instance to inspect.</param>
    /// <param name="levelIndex">The level index.</param>
    /// <returns>The matching override, or null.</returns>
    DocxNumberingLevelOverride? FindLevelOverride(DocxNumberingInstance instance, string levelIndex);

    /// <summary>
    /// Resolves the effective level after applying instance overrides.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <param name="numberingId">The numbering identifier.</param>
    /// <param name="levelIndex">The level index.</param>
    /// <returns>The effective level, or null.</returns>
    DocxNumberingLevel? FindEffectiveLevel(DocxNumbering numbering, string numberingId, string levelIndex);

    /// <summary>
    /// Returns definitions linked to a paragraph or numbering style identifier.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <param name="styleId">The style identifier to match.</param>
    /// <returns>A new list containing linked definitions.</returns>
    List<DocxAbstractNumbering> GetStyleLinkedNumberings(DocxNumbering numbering, string styleId);

    /// <summary>
    /// Returns preserved picture-bullet XML elements.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <returns>A new list containing picture-bullet XML.</returns>
    List<string> GetPictureBulletXml(DocxNumbering numbering);

    /// <summary>
    /// Creates and persists a single-level bullet list.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The new numbering instance.</returns>
    DocxNumberingInstance CreateBulletList(DocxDocument document);

    /// <summary>
    /// Creates and persists a single-level decimal list.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The new numbering instance.</returns>
    DocxNumberingInstance CreateDecimalList(DocxDocument document);

    /// <summary>
    /// Creates and persists a lowercase letter list.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The new numbering instance.</returns>
    DocxNumberingInstance CreateLowerLetterList(DocxDocument document);

    /// <summary>
    /// Creates and persists an uppercase letter list.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The new numbering instance.</returns>
    DocxNumberingInstance CreateUpperLetterList(DocxDocument document);

    /// <summary>
    /// Creates and persists a lowercase Roman numeral list.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The new numbering instance.</returns>
    DocxNumberingInstance CreateLowerRomanList(DocxDocument document);

    /// <summary>
    /// Creates and persists an uppercase Roman numeral list.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The new numbering instance.</returns>
    DocxNumberingInstance CreateUpperRomanList(DocxDocument document);

    /// <summary>
    /// Creates and persists a custom single-level list.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="numberFormat">The WordprocessingML number format.</param>
    /// <param name="levelText">The level text template.</param>
    /// <returns>The new numbering instance.</returns>
    DocxNumberingInstance CreateCustomSingleLevelList(DocxDocument document, string numberFormat, string levelText);

    /// <summary>
    /// Creates and persists a multilevel list from supplied level definitions.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="levels">The level definitions to copy into the document.</param>
    /// <returns>The new numbering instance.</returns>
    DocxNumberingInstance CreateMultiLevelList(DocxDocument document, List<DocxNumberingLevel> levels);

    /// <summary>
    /// Persists edits to an abstract numbering definition and its levels.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="numbering">The definition to add or replace.</param>
    /// <returns>True when an existing definition was replaced; otherwise false.</returns>
    bool UpdateAbstractNumbering(DocxDocument document, DocxAbstractNumbering numbering);

    /// <summary>
    /// Persists edits to a numbering instance and its overrides.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="instance">The instance to add or replace.</param>
    /// <returns>True when an existing instance was replaced; otherwise false.</returns>
    bool UpdateNumberingInstance(DocxDocument document, DocxNumberingInstance instance);

    /// <summary>
    /// Returns an existing instance for list continuation without allocating a new identifier.
    /// </summary>
    /// <param name="document">The document containing the list.</param>
    /// <param name="numberingId">The numbering identifier to continue.</param>
    /// <returns>The existing numbering instance.</returns>
    DocxNumberingInstance ContinueList(DocxDocument document, string numberingId);

    /// <summary>
    /// Creates and persists a new numbering instance that restarts one level.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="numberingId">The numbering identifier whose definition is reused.</param>
    /// <param name="levelIndex">The level index to restart.</param>
    /// <param name="startValue">The positive restart value.</param>
    /// <returns>The new restarted numbering instance.</returns>
    DocxNumberingInstance RestartList(DocxDocument document, string numberingId, string levelIndex, int startValue);

    /// <summary>
    /// Applies a numbering instance and level to a paragraph.
    /// </summary>
    /// <param name="document">The document containing the numbering instance.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="numberingId">The numbering identifier to apply.</param>
    /// <param name="levelIndex">The level index to apply.</param>
    void ApplyList(DocxDocument document, DocxParagraph paragraph, string numberingId, string levelIndex);

    /// <summary>
    /// Removes numbering properties from a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    void ClearList(DocxParagraph paragraph);

    /// <summary>
    /// Calculates a label for a zero-based item index at one level.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <param name="numberingId">The numbering identifier.</param>
    /// <param name="levelIndex">The level index.</param>
    /// <param name="itemIndex">The zero-based item index.</param>
    /// <returns>The calculated label.</returns>
    string CalculateListLabel(DocxNumbering numbering, string numberingId, string levelIndex, int itemIndex);

    /// <summary>
    /// Calculates a multilevel label using one positive value per supplied level.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <param name="numberingId">The numbering identifier.</param>
    /// <param name="levelIndex">The target level index.</param>
    /// <param name="levelValues">The positive values for levels zero through the target.</param>
    /// <returns>The calculated multilevel label.</returns>
    string CalculateMultiLevelLabel(DocxNumbering numbering, string numberingId, string levelIndex, List<int> levelValues);
}
