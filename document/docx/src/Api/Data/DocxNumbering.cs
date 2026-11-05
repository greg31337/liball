using System.Globalization;
using System.Text;

namespace docx;

/// <summary>
/// Represents numbering data used by a DOCX document.
/// </summary>
public class DocxNumbering
{
    internal string Xml;
    internal List<DocxXmlBuffer> Children;
    /// <summary>
    /// The abstract numbering definitions.
    /// </summary>
    public List<DocxAbstractNumbering> AbstractNumberings;
    /// <summary>
    /// The numbering instances.
    /// </summary>
    public List<DocxNumberingInstance> NumberingInstances;

    /// <summary>
    /// Creates a numbering instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxNumbering(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.Children = new List<DocxXmlBuffer>();
        this.AbstractNumberings = new List<DocxAbstractNumbering>();
        this.NumberingInstances = new List<DocxNumberingInstance>();
        LoadFromXml();
    }

    internal static DocxNumbering Create()
    {
        return new DocxNumbering("");
    }

    internal static DocxNumbering Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("numbering", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:numbering.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxNumbering(buffer.GetXml());
    }

    internal DocxNumberingInstance CreateDecimalList()
    {
        return CreateDecimalListWithIds(AllocateAbstractNumberingId(), AllocateNumberingId());
    }

    internal DocxNumberingInstance CreateDecimalListWithIds(string abstractNumberingId, string numberingId)
    {
        return CreateSingleLevelList(abstractNumberingId, numberingId, "decimal", "%1.");
    }

    internal DocxNumberingInstance CreateBulletList()
    {
        return CreateBulletListWithIds(AllocateAbstractNumberingId(), AllocateNumberingId());
    }

    internal DocxNumberingInstance CreateBulletListWithIds(string abstractNumberingId, string numberingId)
    {
        DocxNumberingInstance instance = CreateSingleLevelList(abstractNumberingId, numberingId, "bullet", "\u2022");
        DocxAbstractNumbering? abstractNumbering = FindAbstractNumberingById(abstractNumberingId);

        if (abstractNumbering != null && abstractNumbering.LevelCount() > 0)
        {
            abstractNumbering.GetLevel(0).SetRunFonts("Symbol", "Symbol", "", "");
        }

        return instance;
    }

    internal DocxNumberingInstance CreateSingleLevelList(string abstractNumberingId, string numberingId, string numberFormat, string levelText)
    {
        if (abstractNumberingId == null)
        {
            throw new ArgumentNullException("abstractNumberingId");
        }

        if (numberingId == null)
        {
            throw new ArgumentNullException("numberingId");
        }

        if (numberFormat == null)
        {
            throw new ArgumentNullException("numberFormat");
        }

        if (levelText == null)
        {
            throw new ArgumentNullException("levelText");
        }

        if (abstractNumberingId == "")
        {
            throw new ArgumentException("Abstract numbering id is required.", "abstractNumberingId");
        }

        if (numberingId == "")
        {
            throw new ArgumentException("Numbering id is required.", "numberingId");
        }

        if (numberFormat == "")
        {
            throw new ArgumentException("Number format is required.", "numberFormat");
        }

        if (ContainsAbstractNumberingId(abstractNumberingId))
        {
            throw new InvalidOperationException("Abstract numbering id already exists.");
        }

        if (ContainsNumberingId(numberingId))
        {
            throw new InvalidOperationException("Numbering id already exists.");
        }

        DocxNumberingLevel level = DocxNumberingLevel.Create("0", "1", numberFormat, levelText);
        level.LevelJustification = "left";
        level.SetParagraphIndent("720", "360");

        DocxAbstractNumbering abstractNumbering = DocxAbstractNumbering.CreateSingleLevel(abstractNumberingId, "singleLevel", level);
        DocxNumberingInstance instance = DocxNumberingInstance.Create(numberingId, abstractNumberingId);

        AddAbstractNumbering(abstractNumbering);
        AddNumberingInstance(instance);

        return instance;
    }

    internal string AllocateAbstractNumberingId()
    {
        int maxId = -1;

        foreach (DocxAbstractNumbering numbering in this.AbstractNumberings)
        {
            int parsed = 0;

            if (int.TryParse(numbering.AbstractNumberingId, out parsed) && parsed > maxId)
            {
                maxId = parsed;
            }
        }

        if (maxId == int.MaxValue)
        {
            throw new InvalidOperationException("No abstract numbering id is available.");
        }

        return (maxId + 1).ToString(CultureInfo.InvariantCulture);
    }

    internal string AllocateNumberingId()
    {
        int maxId = 0;

        foreach (DocxNumberingInstance instance in this.NumberingInstances)
        {
            int parsed = 0;

            if (int.TryParse(instance.NumberingId, out parsed) && parsed > maxId)
            {
                maxId = parsed;
            }
        }

        if (maxId == int.MaxValue)
        {
            throw new InvalidOperationException("No numbering id is available.");
        }

        return (maxId + 1).ToString(CultureInfo.InvariantCulture);
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool HasXml()
    {
        return this.Xml != "";
    }

    internal void LoadFromXml()
    {
        this.Children = new List<DocxXmlBuffer>();
        this.AbstractNumberings = new List<DocxAbstractNumbering>();
        this.NumberingInstances = new List<DocxNumberingInstance>();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("numbering", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int numberingDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == numberingDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == numberingDepth + 1)
            {
                bool isAbstractNumbering = reader.IsElement("abstractNum", DocxConstants.NamespaceWordprocessingMl);
                bool isNumberingInstance = reader.IsElement("num", DocxConstants.NamespaceWordprocessingMl);
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddParsedChild(child);

                if (isAbstractNumbering)
                {
                    AddParsedAbstractNumbering(DocxAbstractNumberingFromBuffer(child));
                }
                else if (isNumberingInstance)
                {
                    AddParsedNumberingInstance(DocxNumberingInstanceFromBuffer(child));
                }

                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void AddChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        this.Children.Add(child);
        ClearRawXml();
    }

    internal void AddParsedChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        this.Children.Add(child);
    }

    internal void AddAbstractNumbering(DocxAbstractNumbering numbering)
    {
        if (numbering == null)
        {
            throw new ArgumentNullException("numbering");
        }

        this.AbstractNumberings.Add(numbering);
        ClearRawXml();
    }

    internal void AddParsedAbstractNumbering(DocxAbstractNumbering numbering)
    {
        if (numbering == null)
        {
            throw new ArgumentNullException("numbering");
        }

        this.AbstractNumberings.Add(numbering);
    }

    internal void AddNumberingInstance(DocxNumberingInstance instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException("instance");
        }

        this.NumberingInstances.Add(instance);
        ClearRawXml();
    }

    internal void AddParsedNumberingInstance(DocxNumberingInstance instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException("instance");
        }

        this.NumberingInstances.Add(instance);
    }

    internal bool AddOrReplaceAbstractNumbering(DocxAbstractNumbering numbering)
    {
        if (numbering == null)
        {
            throw new ArgumentNullException("numbering");
        }

        if (numbering.AbstractNumberingId == "")
        {
            throw new ArgumentException("Abstract numbering id is required.", "numbering");
        }

        for (int index = 0; index < this.AbstractNumberings.Count; index = index + 1)
        {
            if (this.AbstractNumberings[index].AbstractNumberingId == numbering.AbstractNumberingId)
            {
                this.AbstractNumberings[index] = numbering;
                ClearRawXml();
                return true;
            }
        }

        AddAbstractNumbering(numbering);
        return false;
    }

    internal bool AddOrReplaceNumberingInstance(DocxNumberingInstance instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException("instance");
        }

        if (instance.NumberingId == "")
        {
            throw new ArgumentException("Numbering id is required.", "instance");
        }

        for (int index = 0; index < this.NumberingInstances.Count; index = index + 1)
        {
            if (this.NumberingInstances[index].NumberingId == instance.NumberingId)
            {
                this.NumberingInstances[index] = instance;
                ClearRawXml();
                return true;
            }
        }

        AddNumberingInstance(instance);
        return false;
    }

    internal DocxNumberingImportMap ImportNumbering(DocxNumbering numbering)
    {
        if (numbering == null)
        {
            throw new ArgumentNullException("numbering");
        }

        DocxNumberingImportMap map = new DocxNumberingImportMap();

        foreach (DocxAbstractNumbering abstractNumbering in numbering.AbstractNumberings)
        {
            string abstractNumberingId = GetImportedAbstractNumberingId(abstractNumbering.AbstractNumberingId);
            DocxAbstractNumbering importedAbstractNumbering = CloneAbstractNumbering(abstractNumbering);

            importedAbstractNumbering.AbstractNumberingId = abstractNumberingId;
            importedAbstractNumbering.Xml = "";
            AddAbstractNumbering(importedAbstractNumbering);
            map.AddAbstractNumberingId(abstractNumbering.AbstractNumberingId, abstractNumberingId);
        }

        foreach (DocxNumberingInstance instance in numbering.NumberingInstances)
        {
            string numberingId = GetImportedNumberingId(instance.NumberingId);
            string abstractNumberingId = map.GetAbstractNumberingId(instance.AbstractNumberingId);
            DocxNumberingInstance importedInstance = CloneNumberingInstance(instance);

            importedInstance.NumberingId = numberingId;
            importedInstance.AbstractNumberingId = abstractNumberingId;
            importedInstance.Xml = "";
            AddNumberingInstance(importedInstance);
            map.AddNumberingId(instance.NumberingId, numberingId);
        }

        return map;
    }

    internal string GetImportedAbstractNumberingId(string abstractNumberingId)
    {
        if (abstractNumberingId == null)
        {
            throw new ArgumentNullException("abstractNumberingId");
        }

        if (abstractNumberingId != "" && !ContainsAbstractNumberingId(abstractNumberingId))
        {
            return abstractNumberingId;
        }

        return AllocateAbstractNumberingId();
    }

    internal string GetImportedNumberingId(string numberingId)
    {
        if (numberingId == null)
        {
            throw new ArgumentNullException("numberingId");
        }

        if (numberingId != "" && !ContainsNumberingId(numberingId))
        {
            return numberingId;
        }

        return AllocateNumberingId();
    }

    internal static DocxAbstractNumbering CloneAbstractNumbering(DocxAbstractNumbering abstractNumbering)
    {
        if (abstractNumbering == null)
        {
            throw new ArgumentNullException("abstractNumbering");
        }

        return new DocxAbstractNumbering(abstractNumbering.WriteXml());
    }

    internal static DocxNumberingInstance CloneNumberingInstance(DocxNumberingInstance instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException("instance");
        }

        return new DocxNumberingInstance(instance.WriteXml());
    }

    internal static DocxAbstractNumbering DocxAbstractNumberingFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxAbstractNumbering.Parse(reader);
    }

    internal static DocxNumberingInstance DocxNumberingInstanceFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxNumberingInstance.Parse(reader);
    }

    internal void ClearRawXml()
    {
        this.Xml = "";
    }

    internal string WriteXml()
    {
        DocxXmlWriter writer = DocxXmlWriter.Create();
        WriteTo(writer);
        return writer.GetXml();
    }

    internal void WriteTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Xml != "")
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.Xml);
            buffer.WriteTo(writer);
            return;
        }

        writer.StartElement("w", "numbering", DocxConstants.NamespaceWordprocessingMl);
        WriteNonNumberingChildrenTo(writer);
        WriteAbstractNumberingsTo(writer);
        WriteNumberingInstancesTo(writer);
        writer.EndElement();
    }

    internal void WriteNonNumberingChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxXmlBuffer child in this.Children)
        {
            if (!IsNumberingDefinitionChild(child))
            {
                child.WriteTo(writer);
            }
        }
    }

    internal void WriteAbstractNumberingsTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxAbstractNumbering abstractNumbering in this.AbstractNumberings)
        {
            abstractNumbering.WriteTo(writer);
        }
    }

    internal void WriteNumberingInstancesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxNumberingInstance instance in this.NumberingInstances)
        {
            instance.WriteTo(writer);
        }
    }

    internal static bool IsNumberingDefinitionChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

        if (!reader.MoveToContent())
        {
            return false;
        }

        if (reader.NamespaceUri() != DocxConstants.NamespaceWordprocessingMl)
        {
            return false;
        }

        return reader.LocalName() == "abstractNum" || reader.LocalName() == "num";
    }

    internal int ChildCount()
    {
        return this.Children.Count;
    }

    internal string GetChildXml(int index)
    {
        return this.Children[index].GetXml();
    }

    internal int AbstractNumberingCount()
    {
        return this.AbstractNumberings.Count;
    }

    internal DocxAbstractNumbering GetAbstractNumbering(int index)
    {
        return this.AbstractNumberings[index];
    }

    internal int NumberingInstanceCount()
    {
        return this.NumberingInstances.Count;
    }

    internal DocxNumberingInstance GetNumberingInstance(int index)
    {
        return this.NumberingInstances[index];
    }

    internal DocxAbstractNumbering? FindAbstractNumberingById(string abstractNumberingId)
    {
        if (abstractNumberingId == null)
        {
            throw new ArgumentNullException("abstractNumberingId");
        }

        foreach (DocxAbstractNumbering numbering in this.AbstractNumberings)
        {
            if (numbering.AbstractNumberingId == abstractNumberingId)
            {
                return numbering;
            }
        }

        return null;
    }

    internal DocxNumberingInstance? FindNumberingInstanceById(string numberingId)
    {
        if (numberingId == null)
        {
            throw new ArgumentNullException("numberingId");
        }

        foreach (DocxNumberingInstance instance in this.NumberingInstances)
        {
            if (instance.NumberingId == numberingId)
            {
                return instance;
            }
        }

        return null;
    }

    internal bool ContainsAbstractNumberingId(string abstractNumberingId)
    {
        return FindAbstractNumberingById(abstractNumberingId) != null;
    }

    internal bool ContainsNumberingId(string numberingId)
    {
        return FindNumberingInstanceById(numberingId) != null;
    }

    internal void ApplyToParagraph(DocxParagraph paragraph, string numberingId, string levelIndex)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        if (numberingId == null)
        {
            throw new ArgumentNullException("numberingId");
        }

        if (levelIndex == null)
        {
            throw new ArgumentNullException("levelIndex");
        }

        if (!ContainsNumberingId(numberingId))
        {
            throw new InvalidOperationException("Numbering instance does not exist.");
        }

        paragraph.ApplyNumbering(numberingId, levelIndex);
    }

    internal void ApplyInstanceToParagraph(DocxParagraph paragraph, DocxNumberingInstance instance, string levelIndex)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        if (instance == null)
        {
            throw new ArgumentNullException("instance");
        }

        if (levelIndex == null)
        {
            throw new ArgumentNullException("levelIndex");
        }

        ApplyToParagraph(paragraph, instance.NumberingId, levelIndex);
    }

    internal DocxNumberingLevel? FindEffectiveLevel(string numberingId, string levelIndex)
    {
        if (numberingId == null)
        {
            throw new ArgumentNullException("numberingId");
        }

        if (levelIndex == null)
        {
            throw new ArgumentNullException("levelIndex");
        }

        DocxNumberingInstance? instance = FindNumberingInstanceById(numberingId);

        if (instance == null)
        {
            return null;
        }

        DocxNumberingLevelOverride? levelOverride = instance.FindLevelOverrideByIndex(levelIndex);

        if (levelOverride != null && levelOverride.HasLevel())
        {
            return levelOverride.Level;
        }

        DocxAbstractNumbering? abstractNumbering = FindAbstractNumberingById(instance.AbstractNumberingId);

        if (abstractNumbering == null)
        {
            return null;
        }

        return abstractNumbering.FindLevelByIndex(levelIndex);
    }

    internal string CalculateLabelForItem(string numberingId, string levelIndex, int itemIndex)
    {
        if (numberingId == null)
        {
            throw new ArgumentNullException("numberingId");
        }

        if (levelIndex == null)
        {
            throw new ArgumentNullException("levelIndex");
        }

        if (itemIndex < 0)
        {
            throw new ArgumentOutOfRangeException("itemIndex");
        }

        int startValue = GetLevelStartValue(numberingId, levelIndex);

        if (itemIndex > int.MaxValue - startValue)
        {
            throw new ArgumentOutOfRangeException("itemIndex");
        }

        return CalculateLabel(numberingId, levelIndex, startValue + itemIndex);
    }

    internal string CalculateLabel(string numberingId, string levelIndex, int value)
    {
        if (numberingId == null)
        {
            throw new ArgumentNullException("numberingId");
        }

        if (levelIndex == null)
        {
            throw new ArgumentNullException("levelIndex");
        }

        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        DocxNumberingLevel? level = FindEffectiveLevel(numberingId, levelIndex);

        if (level == null)
        {
            throw new InvalidOperationException("Numbering level does not exist.");
        }

        string formatted = FormatNumber(level.NumberFormat, value);
        return ApplyLevelText(level.NumberFormat, level.LevelText, level.LevelIndex, formatted);
    }

    internal int GetLevelStartValue(string numberingId, string levelIndex)
    {
        if (numberingId == null)
        {
            throw new ArgumentNullException("numberingId");
        }

        if (levelIndex == null)
        {
            throw new ArgumentNullException("levelIndex");
        }

        DocxNumberingInstance? instance = FindNumberingInstanceById(numberingId);

        if (instance == null)
        {
            throw new InvalidOperationException("Numbering instance does not exist.");
        }

        DocxNumberingLevelOverride? levelOverride = instance.FindLevelOverrideByIndex(levelIndex);

        if (levelOverride != null && levelOverride.StartOverride != "")
        {
            return ReadPositiveInt(levelOverride.StartOverride, "startOverride");
        }

        DocxNumberingLevel? level = FindEffectiveLevel(numberingId, levelIndex);

        if (level == null)
        {
            throw new InvalidOperationException("Numbering level does not exist.");
        }

        if (level.Start != "")
        {
            return ReadPositiveInt(level.Start, "start");
        }

        return 1;
    }

    internal static string FormatNumber(string numberFormat, int value)
    {
        if (numberFormat == null)
        {
            throw new ArgumentNullException("numberFormat");
        }

        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        if (numberFormat == "lowerLetter")
        {
            return FormatAlphabetic(value, false);
        }

        if (numberFormat == "upperLetter")
        {
            return FormatAlphabetic(value, true);
        }

        if (numberFormat == "lowerRoman")
        {
            return FormatRoman(value, false);
        }

        if (numberFormat == "upperRoman")
        {
            return FormatRoman(value, true);
        }

        if (numberFormat == "decimalZero")
        {
            if (value < 10)
            {
                return "0" + value.ToString(CultureInfo.InvariantCulture);
            }
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string FormatAlphabetic(int value, bool upperCase)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        StringBuilder builder = new StringBuilder();
        int current = value;

        while (current > 0)
        {
            current = current - 1;
            int remainder = current % 26;
            char letter = (char)('a' + remainder);

            if (upperCase)
            {
                letter = (char)('A' + remainder);
            }

            builder.Insert(0, letter);
            current = current / 26;
        }

        return builder.ToString();
    }

    internal static string FormatRoman(int value, bool upperCase)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        StringBuilder builder = new StringBuilder();
        int current = value;

        current = AppendRomanPart(builder, current, 1000, "M");
        current = AppendRomanPart(builder, current, 900, "CM");
        current = AppendRomanPart(builder, current, 500, "D");
        current = AppendRomanPart(builder, current, 400, "CD");
        current = AppendRomanPart(builder, current, 100, "C");
        current = AppendRomanPart(builder, current, 90, "XC");
        current = AppendRomanPart(builder, current, 50, "L");
        current = AppendRomanPart(builder, current, 40, "XL");
        current = AppendRomanPart(builder, current, 10, "X");
        current = AppendRomanPart(builder, current, 9, "IX");
        current = AppendRomanPart(builder, current, 5, "V");
        current = AppendRomanPart(builder, current, 4, "IV");
        AppendRomanPart(builder, current, 1, "I");

        if (upperCase)
        {
            return builder.ToString();
        }

        return builder.ToString().ToLowerInvariant();
    }

    internal static int AppendRomanPart(StringBuilder builder, int value, int unit, string symbol)
    {
        if (builder == null)
        {
            throw new ArgumentNullException("builder");
        }

        if (symbol == null)
        {
            throw new ArgumentNullException("symbol");
        }

        int current = value;

        while (current >= unit)
        {
            builder.Append(symbol);
            current = current - unit;
        }

        return current;
    }

    internal static string ApplyLevelText(string numberFormat, string levelText, string levelIndex, string formattedValue)
    {
        if (numberFormat == null)
        {
            throw new ArgumentNullException("numberFormat");
        }

        if (levelText == null)
        {
            throw new ArgumentNullException("levelText");
        }

        if (levelIndex == null)
        {
            throw new ArgumentNullException("levelIndex");
        }

        if (formattedValue == null)
        {
            throw new ArgumentNullException("formattedValue");
        }

        if (levelText == "")
        {
            if (numberFormat == "bullet")
            {
                return "\u2022";
            }

            return formattedValue;
        }

        string placeholder = GetLevelTextPlaceholder(levelIndex);

        if (levelText.IndexOf(placeholder, StringComparison.Ordinal) >= 0)
        {
            return levelText.Replace(placeholder, formattedValue);
        }

        if (numberFormat == "bullet")
        {
            return levelText;
        }

        if (levelText.IndexOf("%", StringComparison.Ordinal) < 0)
        {
            return levelText;
        }

        return formattedValue;
    }

    internal static string GetLevelTextPlaceholder(string levelIndex)
    {
        if (levelIndex == null)
        {
            throw new ArgumentNullException("levelIndex");
        }

        int parsed = 0;

        if (int.TryParse(levelIndex, out parsed))
        {
            long placeholder = (long)parsed + 1;
            return "%" + placeholder.ToString(CultureInfo.InvariantCulture);
        }

        return "%" + levelIndex;
    }

    internal static int ReadPositiveInt(string value, string name)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        int parsed = 0;

        if (!int.TryParse(value, out parsed))
        {
            throw new FormatException(name + " is not a valid integer.");
        }

        if (parsed < 1)
        {
            throw new FormatException(name + " must be positive.");
        }

        return parsed;
    }

    internal int CountChildElements(string localName, string namespaceUri)
    {
        DocxXmlReader.ValidateName(localName, namespaceUri);

        int count = 0;

        foreach (DocxXmlBuffer child in this.Children)
        {
            DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

            if (reader.MoveToContent() && reader.IsElement(localName, namespaceUri))
            {
                count = count + 1;
            }
        }

        return count;
    }
}
