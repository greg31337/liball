using System.Globalization;

namespace docx;

/// <summary>
/// Provides numbering discovery, creation, editing, application, continuation, restart, and label operations.
/// </summary>
public class DocxNumberingManagement : IDocxNumberingManagement
{
    /// <summary>
    /// Creates a numbering management service.
    /// </summary>
    public DocxNumberingManagement()
    {
    }

    /// <summary>
    /// Returns all abstract numbering definitions in document order.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <returns>A new list containing the abstract definitions.</returns>
    public List<DocxAbstractNumbering> GetAbstractNumberings(DocxNumbering numbering)
    {
        ValidateNumbering(numbering);
        List<DocxAbstractNumbering> result = new List<DocxAbstractNumbering>();

        foreach (DocxAbstractNumbering abstractNumbering in numbering.AbstractNumberings)
        {
            result.Add(abstractNumbering);
        }

        return result;
    }

    /// <summary>
    /// Returns all numbering instances in document order.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <returns>A new list containing the numbering instances.</returns>
    public List<DocxNumberingInstance> GetNumberingInstances(DocxNumbering numbering)
    {
        ValidateNumbering(numbering);
        List<DocxNumberingInstance> result = new List<DocxNumberingInstance>();

        foreach (DocxNumberingInstance instance in numbering.NumberingInstances)
        {
            result.Add(instance);
        }

        return result;
    }

    /// <summary>
    /// Returns all levels from an abstract numbering definition.
    /// </summary>
    /// <param name="numbering">The abstract numbering definition to inspect.</param>
    /// <returns>A new list containing the levels.</returns>
    public List<DocxNumberingLevel> GetLevels(DocxAbstractNumbering numbering)
    {
        ValidateAbstractNumbering(numbering);
        List<DocxNumberingLevel> result = new List<DocxNumberingLevel>();

        for (int index = 0; index < numbering.LevelCount(); index = index + 1)
        {
            result.Add(numbering.GetLevel(index));
        }

        return result;
    }

    /// <summary>
    /// Returns all level overrides from a numbering instance.
    /// </summary>
    /// <param name="instance">The numbering instance to inspect.</param>
    /// <returns>A new list containing the overrides.</returns>
    public List<DocxNumberingLevelOverride> GetLevelOverrides(DocxNumberingInstance instance)
    {
        ValidateInstance(instance);
        List<DocxNumberingLevelOverride> result = new List<DocxNumberingLevelOverride>();

        for (int index = 0; index < instance.LevelOverrideCount(); index = index + 1)
        {
            result.Add(instance.GetLevelOverride(index));
        }

        return result;
    }

    /// <summary>
    /// Finds an abstract numbering definition by identifier.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <param name="abstractNumberingId">The abstract numbering identifier.</param>
    /// <returns>The matching definition, or null.</returns>
    public DocxAbstractNumbering? FindAbstractNumbering(DocxNumbering numbering, string abstractNumberingId)
    {
        ValidateNumbering(numbering);
        ValidateText(abstractNumberingId, "abstractNumberingId");
        return numbering.FindAbstractNumberingById(abstractNumberingId);
    }

    /// <summary>
    /// Finds a numbering instance by identifier.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <param name="numberingId">The numbering identifier.</param>
    /// <returns>The matching instance, or null.</returns>
    public DocxNumberingInstance? FindNumberingInstance(DocxNumbering numbering, string numberingId)
    {
        ValidateNumbering(numbering);
        ValidateText(numberingId, "numberingId");
        return numbering.FindNumberingInstanceById(numberingId);
    }

    /// <summary>
    /// Finds a level by its zero-based level index.
    /// </summary>
    /// <param name="numbering">The abstract numbering definition to inspect.</param>
    /// <param name="levelIndex">The level index.</param>
    /// <returns>The matching level, or null.</returns>
    public DocxNumberingLevel? FindLevel(DocxAbstractNumbering numbering, string levelIndex)
    {
        ValidateAbstractNumbering(numbering);
        ValidateText(levelIndex, "levelIndex");
        return numbering.FindLevelByIndex(levelIndex);
    }

    /// <summary>
    /// Finds a level override by its zero-based level index.
    /// </summary>
    /// <param name="instance">The numbering instance to inspect.</param>
    /// <param name="levelIndex">The level index.</param>
    /// <returns>The matching override, or null.</returns>
    public DocxNumberingLevelOverride? FindLevelOverride(DocxNumberingInstance instance, string levelIndex)
    {
        ValidateInstance(instance);
        ValidateText(levelIndex, "levelIndex");
        return instance.FindLevelOverrideByIndex(levelIndex);
    }

    /// <summary>
    /// Resolves the effective level after applying instance overrides.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <param name="numberingId">The numbering identifier.</param>
    /// <param name="levelIndex">The level index.</param>
    /// <returns>The effective level, or null.</returns>
    public DocxNumberingLevel? FindEffectiveLevel(DocxNumbering numbering, string numberingId, string levelIndex)
    {
        ValidateNumbering(numbering);
        ValidateText(numberingId, "numberingId");
        ValidateText(levelIndex, "levelIndex");
        return numbering.FindEffectiveLevel(numberingId, levelIndex);
    }

    /// <summary>
    /// Returns definitions linked to a paragraph or numbering style identifier.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <param name="styleId">The style identifier to match.</param>
    /// <returns>A new list containing linked definitions.</returns>
    public List<DocxAbstractNumbering> GetStyleLinkedNumberings(DocxNumbering numbering, string styleId)
    {
        ValidateNumbering(numbering);
        ValidateText(styleId, "styleId");
        List<DocxAbstractNumbering> result = new List<DocxAbstractNumbering>();

        foreach (DocxAbstractNumbering abstractNumbering in numbering.AbstractNumberings)
        {
            if (abstractNumbering.StyleLink == styleId || abstractNumbering.NumberingStyleLink == styleId)
            {
                result.Add(abstractNumbering);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns preserved picture-bullet XML elements.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <returns>A new list containing picture-bullet XML.</returns>
    public List<string> GetPictureBulletXml(DocxNumbering numbering)
    {
        ValidateNumbering(numbering);
        List<string> result = new List<string>();

        for (int index = 0; index < numbering.ChildCount(); index = index + 1)
        {
            string xml = numbering.GetChildXml(index);
            DocxXmlReader reader = DocxXmlReader.Create(xml);

            if (reader.MoveToContent() && reader.IsElement("numPicBullet", DocxConstants.NamespaceWordprocessingMl))
            {
                result.Add(xml);
            }
        }

        return result;
    }

    /// <summary>
    /// Creates and persists a single-level bullet list.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The new numbering instance.</returns>
    public DocxNumberingInstance CreateBulletList(DocxDocument document)
    {
        ValidateDocument(document);
        DocxNumbering numbering = document.GetOrCreateNumbering();
        DocxNumberingInstance instance = numbering.CreateBulletList();
        document.WriteNumberingPart();
        return instance;
    }

    /// <summary>
    /// Creates and persists a single-level decimal list.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The new numbering instance.</returns>
    public DocxNumberingInstance CreateDecimalList(DocxDocument document)
    {
        return CreateAndPersistSingleLevel(document, "decimal", "%1.");
    }

    /// <summary>
    /// Creates and persists a lowercase letter list.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The new numbering instance.</returns>
    public DocxNumberingInstance CreateLowerLetterList(DocxDocument document)
    {
        return CreateAndPersistSingleLevel(document, "lowerLetter", "%1.");
    }

    /// <summary>
    /// Creates and persists an uppercase letter list.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The new numbering instance.</returns>
    public DocxNumberingInstance CreateUpperLetterList(DocxDocument document)
    {
        return CreateAndPersistSingleLevel(document, "upperLetter", "%1.");
    }

    /// <summary>
    /// Creates and persists a lowercase Roman numeral list.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The new numbering instance.</returns>
    public DocxNumberingInstance CreateLowerRomanList(DocxDocument document)
    {
        return CreateAndPersistSingleLevel(document, "lowerRoman", "%1.");
    }

    /// <summary>
    /// Creates and persists an uppercase Roman numeral list.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The new numbering instance.</returns>
    public DocxNumberingInstance CreateUpperRomanList(DocxDocument document)
    {
        return CreateAndPersistSingleLevel(document, "upperRoman", "%1.");
    }

    /// <summary>
    /// Creates and persists a custom single-level list.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="numberFormat">The WordprocessingML number format.</param>
    /// <param name="levelText">The level text template.</param>
    /// <returns>The new numbering instance.</returns>
    public DocxNumberingInstance CreateCustomSingleLevelList(DocxDocument document, string numberFormat, string levelText)
    {
        ValidateText(numberFormat, "numberFormat");
        ValidateText(levelText, "levelText");

        if (numberFormat == "")
        {
            throw new ArgumentException("Number format is required.", "numberFormat");
        }

        return CreateAndPersistSingleLevel(document, numberFormat, levelText);
    }

    /// <summary>
    /// Creates and persists a multilevel list from supplied level definitions.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="levels">The level definitions to copy into the document.</param>
    /// <returns>The new numbering instance.</returns>
    public DocxNumberingInstance CreateMultiLevelList(DocxDocument document, List<DocxNumberingLevel> levels)
    {
        ValidateDocument(document);

        if (levels == null)
        {
            throw new ArgumentNullException("levels");
        }

        if (levels.Count < 1 || levels.Count > 9)
        {
            throw new ArgumentException("A multilevel list requires between one and nine levels.", "levels");
        }

        DocxNumbering numbering = document.GetOrCreateNumbering();
        string abstractNumberingId = numbering.AllocateAbstractNumberingId();
        string numberingId = numbering.AllocateNumberingId();
        DocxAbstractNumbering abstractNumbering = new DocxAbstractNumbering("");
        abstractNumbering.AbstractNumberingId = abstractNumberingId;
        abstractNumbering.MultiLevelType = "multilevel";
        List<string> levelIndexes = new List<string>();

        for (int index = 0; index < levels.Count; index = index + 1)
        {
            DocxNumberingLevel sourceLevel = levels[index];

            if (sourceLevel == null)
            {
                throw new ArgumentException("Level definitions cannot contain null.", "levels");
            }

            DocxNumberingLevel level = new DocxNumberingLevel(sourceLevel.WriteXml());
            level.LevelIndex = sourceLevel.LevelIndex;
            level.Start = sourceLevel.Start;
            level.NumberFormat = sourceLevel.NumberFormat;
            level.LevelText = sourceLevel.LevelText;
            level.LevelJustification = sourceLevel.LevelJustification;
            level.Suffix = sourceLevel.Suffix;
            level.Xml = "";

            if (level.LevelIndex == "")
            {
                level.LevelIndex = index.ToString(CultureInfo.InvariantCulture);
            }

            if (level.NumberFormat == "")
            {
                throw new ArgumentException("Each level requires a number format.", "levels");
            }

            if (levelIndexes.Contains(level.LevelIndex))
            {
                throw new ArgumentException("Level indexes must be unique.", "levels");
            }

            levelIndexes.Add(level.LevelIndex);
            abstractNumbering.AddLevel(level);
        }

        DocxNumberingInstance instance = DocxNumberingInstance.Create(numberingId, abstractNumberingId);
        numbering.AddAbstractNumbering(abstractNumbering);
        numbering.AddNumberingInstance(instance);
        document.WriteNumberingPart();
        return instance;
    }

    /// <summary>
    /// Persists edits to an abstract numbering definition and its levels.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="numbering">The definition to add or replace.</param>
    /// <returns>True when an existing definition was replaced; otherwise false.</returns>
    public bool UpdateAbstractNumbering(DocxDocument document, DocxAbstractNumbering numbering)
    {
        ValidateDocument(document);
        ValidateAbstractNumbering(numbering);
        DocxNumbering documentNumbering = document.GetOrCreateNumbering();
        numbering.ClearRawXml();
        bool replaced = documentNumbering.AddOrReplaceAbstractNumbering(numbering);
        document.WriteNumberingPart();
        return replaced;
    }

    /// <summary>
    /// Persists edits to a numbering instance and its overrides.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="instance">The instance to add or replace.</param>
    /// <returns>True when an existing instance was replaced; otherwise false.</returns>
    public bool UpdateNumberingInstance(DocxDocument document, DocxNumberingInstance instance)
    {
        ValidateDocument(document);
        ValidateInstance(instance);
        DocxNumbering numbering = document.GetOrCreateNumbering();

        if (numbering.FindAbstractNumberingById(instance.AbstractNumberingId) == null)
        {
            throw new InvalidOperationException("Abstract numbering definition does not exist.");
        }

        instance.ClearRawXml();
        bool replaced = numbering.AddOrReplaceNumberingInstance(instance);
        document.WriteNumberingPart();
        return replaced;
    }

    /// <summary>
    /// Returns an existing instance for list continuation without allocating a new identifier.
    /// </summary>
    /// <param name="document">The document containing the list.</param>
    /// <param name="numberingId">The numbering identifier to continue.</param>
    /// <returns>The existing numbering instance.</returns>
    public DocxNumberingInstance ContinueList(DocxDocument document, string numberingId)
    {
        ValidateDocument(document);
        ValidateText(numberingId, "numberingId");
        DocxNumberingInstance? instance = document.GetNumbering().FindNumberingInstanceById(numberingId);

        if (instance == null)
        {
            throw new InvalidOperationException("Numbering instance does not exist.");
        }

        return instance;
    }

    /// <summary>
    /// Creates and persists a new numbering instance that restarts one level.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="numberingId">The numbering identifier whose definition is reused.</param>
    /// <param name="levelIndex">The level index to restart.</param>
    /// <param name="startValue">The positive restart value.</param>
    /// <returns>The new restarted numbering instance.</returns>
    public DocxNumberingInstance RestartList(DocxDocument document, string numberingId, string levelIndex, int startValue)
    {
        ValidateDocument(document);
        ValidateText(numberingId, "numberingId");
        ValidateText(levelIndex, "levelIndex");

        if (startValue < 1)
        {
            throw new ArgumentOutOfRangeException("startValue");
        }

        DocxNumbering numbering = document.GetNumbering();
        DocxNumberingInstance? source = numbering.FindNumberingInstanceById(numberingId);

        if (source == null)
        {
            throw new InvalidOperationException("Numbering instance does not exist.");
        }

        if (numbering.FindEffectiveLevel(numberingId, levelIndex) == null)
        {
            throw new InvalidOperationException("Numbering level does not exist.");
        }

        DocxNumberingInstance instance = DocxNumberingInstance.Create(numbering.AllocateNumberingId(), source.AbstractNumberingId);
        DocxNumberingLevelOverride levelOverride = new DocxNumberingLevelOverride("");
        levelOverride.LevelIndex = levelIndex;
        levelOverride.StartOverride = startValue.ToString(CultureInfo.InvariantCulture);
        instance.AddLevelOverride(levelOverride);
        numbering.AddNumberingInstance(instance);
        document.WriteNumberingPart();
        return instance;
    }

    /// <summary>
    /// Applies a numbering instance and level to a paragraph.
    /// </summary>
    /// <param name="document">The document containing the numbering instance.</param>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="numberingId">The numbering identifier to apply.</param>
    /// <param name="levelIndex">The level index to apply.</param>
    public void ApplyList(DocxDocument document, DocxParagraph paragraph, string numberingId, string levelIndex)
    {
        ValidateDocument(document);

        if (document.GetNumbering().FindEffectiveLevel(numberingId, levelIndex) == null)
        {
            throw new InvalidOperationException("Numbering level does not exist.");
        }

        document.GetNumbering().ApplyToParagraph(paragraph, numberingId, levelIndex);
    }

    /// <summary>
    /// Removes numbering properties from a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    public void ClearList(DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        paragraph.ClearNumbering();
    }

    /// <summary>
    /// Calculates a label for a zero-based item index at one level.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <param name="numberingId">The numbering identifier.</param>
    /// <param name="levelIndex">The level index.</param>
    /// <param name="itemIndex">The zero-based item index.</param>
    /// <returns>The calculated label.</returns>
    public string CalculateListLabel(DocxNumbering numbering, string numberingId, string levelIndex, int itemIndex)
    {
        ValidateNumbering(numbering);
        return numbering.CalculateLabelForItem(numberingId, levelIndex, itemIndex);
    }

    /// <summary>
    /// Calculates a multilevel label using one positive value per supplied level.
    /// </summary>
    /// <param name="numbering">The numbering model to inspect.</param>
    /// <param name="numberingId">The numbering identifier.</param>
    /// <param name="levelIndex">The target level index.</param>
    /// <param name="levelValues">The positive values for levels zero through the target.</param>
    /// <returns>The calculated multilevel label.</returns>
    public string CalculateMultiLevelLabel(DocxNumbering numbering, string numberingId, string levelIndex, List<int> levelValues)
    {
        ValidateNumbering(numbering);
        ValidateText(numberingId, "numberingId");
        ValidateText(levelIndex, "levelIndex");

        if (levelValues == null)
        {
            throw new ArgumentNullException("levelValues");
        }

        int targetIndex = 0;

        if (!int.TryParse(levelIndex, out targetIndex) || targetIndex < 0 || targetIndex > 8)
        {
            throw new ArgumentException("Level index must be between zero and eight.", "levelIndex");
        }

        if (levelValues.Count <= targetIndex)
        {
            throw new ArgumentException("A value is required for every level through the target level.", "levelValues");
        }

        DocxNumberingLevel? targetLevel = numbering.FindEffectiveLevel(numberingId, levelIndex);

        if (targetLevel == null)
        {
            throw new InvalidOperationException("Numbering level does not exist.");
        }

        string label = targetLevel.LevelText;

        if (label == "")
        {
            string formatted = DocxNumbering.FormatNumber(targetLevel.NumberFormat, levelValues[targetIndex]);
            return DocxNumbering.ApplyLevelText(targetLevel.NumberFormat, label, targetLevel.LevelIndex, formatted);
        }

        for (int index = 0; index <= targetIndex; index = index + 1)
        {
            int value = levelValues[index];

            if (value < 1)
            {
                throw new ArgumentOutOfRangeException("levelValues");
            }

            string currentLevelIndex = index.ToString(CultureInfo.InvariantCulture);
            DocxNumberingLevel? currentLevel = numbering.FindEffectiveLevel(numberingId, currentLevelIndex);

            if (currentLevel == null)
            {
                throw new InvalidOperationException("Numbering level does not exist.");
            }

            string formatted = DocxNumbering.FormatNumber(currentLevel.NumberFormat, value);
            string placeholder = DocxNumbering.GetLevelTextPlaceholder(currentLevelIndex);
            label = label.Replace(placeholder, formatted);
        }

        return label;
    }

    internal static DocxNumberingInstance CreateAndPersistSingleLevel(DocxDocument document, string numberFormat, string levelText)
    {
        ValidateDocument(document);
        DocxNumbering numbering = document.GetOrCreateNumbering();
        DocxNumberingInstance instance = numbering.CreateSingleLevelList(
            numbering.AllocateAbstractNumberingId(),
            numbering.AllocateNumberingId(),
            numberFormat,
            levelText);
        document.WriteNumberingPart();
        return instance;
    }

    internal static void ValidateDocument(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
    }

    internal static void ValidateNumbering(DocxNumbering numbering)
    {
        if (numbering == null)
        {
            throw new ArgumentNullException("numbering");
        }
    }

    internal static void ValidateAbstractNumbering(DocxAbstractNumbering numbering)
    {
        if (numbering == null)
        {
            throw new ArgumentNullException("numbering");
        }
    }

    internal static void ValidateInstance(DocxNumberingInstance instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException("instance");
        }
    }

    internal static void ValidateText(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }
    }
}
