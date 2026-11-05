using System.Xml;

namespace docx;

internal class DocxTemplate
{
    /// <summary>
    /// The default placeholder prefix identifier used in DOCX package data.
    /// </summary>
    public const string DefaultPlaceholderPrefix = "{{";
    /// <summary>
    /// The default placeholder suffix identifier used in DOCX package data.
    /// </summary>
    public const string DefaultPlaceholderSuffix = "}}";

    /// <summary>
    /// Replaces matching template placeholders and returns the number of replacements.
    /// </summary>
    /// <param name="document">The document to inspect or modify.</param>
    /// <param name="values">The named values to apply.</param>
    /// <returns>The number of affected or available items.</returns>
    public static int ReplacePlaceholders(DocxDocument document, Dictionary<string, string> values)
    {
        return ReplacePlaceholders(document, values, DefaultPlaceholderPrefix, DefaultPlaceholderSuffix);
    }

    /// <summary>
    /// Replaces matching template placeholders and returns the number of replacements.
    /// </summary>
    /// <param name="document">The document to inspect or modify.</param>
    /// <param name="values">The named values to apply.</param>
    /// <param name="prefix">The opening delimiter or namespace prefix, as required by the operation.</param>
    /// <param name="suffix">The closing delimiter.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int ReplacePlaceholders(DocxDocument document, Dictionary<string, string> values, string prefix, string suffix)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        int count = ReplacePlaceholders(document.Body, values, prefix, suffix);

        if (count > 0)
        {
            UpdateMainDocumentBody(document);
        }

        return count;
    }

    /// <summary>
    /// Replaces matching template placeholders and returns the number of replacements.
    /// </summary>
    /// <param name="body">The body used by the operation.</param>
    /// <param name="values">The named values to apply.</param>
    /// <returns>The number of affected or available items.</returns>
    public static int ReplacePlaceholders(DocxBody body, Dictionary<string, string> values)
    {
        return ReplacePlaceholders(body, values, DefaultPlaceholderPrefix, DefaultPlaceholderSuffix);
    }

    /// <summary>
    /// Replaces matching template placeholders and returns the number of replacements.
    /// </summary>
    /// <param name="body">The body used by the operation.</param>
    /// <param name="values">The named values to apply.</param>
    /// <param name="prefix">The opening delimiter or namespace prefix, as required by the operation.</param>
    /// <param name="suffix">The closing delimiter.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int ReplacePlaceholders(DocxBody body, Dictionary<string, string> values, string prefix, string suffix)
    {
        if (body == null)
        {
            throw new ArgumentNullException("body");
        }

        ValidatePlaceholderArguments(values, prefix, suffix);

        int count = 0;

        foreach (DocxBlock block in body.Blocks)
        {
            count = count + ReplacePlaceholders(block, values, prefix, suffix);
        }

        if (count > 0)
        {
            body.Xml = body.WriteXml();
            body.LoadBlocksFromXml();
            body.LoadSectionsFromXml();
        }

        return count;
    }

    /// <summary>
    /// Replaces matching template placeholders and returns the number of replacements.
    /// </summary>
    /// <param name="block">The block used by the operation.</param>
    /// <param name="values">The named values to apply.</param>
    /// <param name="prefix">The opening delimiter or namespace prefix, as required by the operation.</param>
    /// <param name="suffix">The closing delimiter.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int ReplacePlaceholders(DocxBlock block, Dictionary<string, string> values, string prefix, string suffix)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (block.Paragraph != null)
        {
            int count = ReplacePlaceholders(block.Paragraph, values, prefix, suffix);
            block.Xml = block.Paragraph.GetXml();
            return count;
        }

        if (block.Table != null)
        {
            int count = ReplacePlaceholders(block.Table, values, prefix, suffix);
            block.Xml = block.Table.WriteXml();
            return count;
        }

        if (block.ContentControl != null)
        {
            int count = ReplacePlaceholders(block.ContentControl, values, prefix, suffix);
            block.Xml = block.ContentControl.WriteXml();
            return count;
        }

        return 0;
    }

    /// <summary>
    /// Replaces matching template placeholders and returns the number of replacements.
    /// </summary>
    /// <param name="paragraph">The paragraph used by the operation.</param>
    /// <param name="values">The named values to apply.</param>
    /// <returns>The number of affected or available items.</returns>
    public static int ReplacePlaceholders(DocxParagraph paragraph, Dictionary<string, string> values)
    {
        return ReplacePlaceholders(paragraph, values, DefaultPlaceholderPrefix, DefaultPlaceholderSuffix);
    }

    /// <summary>
    /// Replaces matching template placeholders and returns the number of replacements.
    /// </summary>
    /// <param name="paragraph">The paragraph used by the operation.</param>
    /// <param name="values">The named values to apply.</param>
    /// <param name="prefix">The opening delimiter or namespace prefix, as required by the operation.</param>
    /// <param name="suffix">The closing delimiter.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int ReplacePlaceholders(DocxParagraph paragraph, Dictionary<string, string> values, string prefix, string suffix)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        ValidatePlaceholderArguments(values, prefix, suffix);

        int count = 0;

        foreach (KeyValuePair<string, string> value in values)
        {
            ValidatePlaceholderValue(value.Key, value.Value);
            count = count + paragraph.ReplaceText(CreatePlaceholder(value.Key, prefix, suffix), value.Value);
        }

        return count;
    }

    /// <summary>
    /// Replaces matching template placeholders and returns the number of replacements.
    /// </summary>
    /// <param name="run">The run used by the operation.</param>
    /// <param name="values">The named values to apply.</param>
    /// <param name="prefix">The opening delimiter or namespace prefix, as required by the operation.</param>
    /// <param name="suffix">The closing delimiter.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int ReplacePlaceholders(DocxRun run, Dictionary<string, string> values, string prefix, string suffix)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        ValidatePlaceholderArguments(values, prefix, suffix);

        int count = 0;

        foreach (KeyValuePair<string, string> value in values)
        {
            ValidatePlaceholderValue(value.Key, value.Value);
            count = count + run.ReplaceText(CreatePlaceholder(value.Key, prefix, suffix), value.Value);
        }

        return count;
    }

    /// <summary>
    /// Replaces matching template placeholders and returns the number of replacements.
    /// </summary>
    /// <param name="table">The table used by the operation.</param>
    /// <param name="values">The named values to apply.</param>
    /// <param name="prefix">The opening delimiter or namespace prefix, as required by the operation.</param>
    /// <param name="suffix">The closing delimiter.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int ReplacePlaceholders(DocxTable table, Dictionary<string, string> values, string prefix, string suffix)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        ValidatePlaceholderArguments(values, prefix, suffix);

        int count = 0;

        foreach (DocxTableRow row in table.Rows)
        {
            count = count + ReplacePlaceholders(row, values, prefix, suffix);
        }

        if (count > 0)
        {
            table.Xml = "";
        }

        return count;
    }

    /// <summary>
    /// Replaces matching template placeholders and returns the number of replacements.
    /// </summary>
    /// <param name="row">The row used by the operation.</param>
    /// <param name="values">The named values to apply.</param>
    /// <param name="prefix">The opening delimiter or namespace prefix, as required by the operation.</param>
    /// <param name="suffix">The closing delimiter.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int ReplacePlaceholders(DocxTableRow row, Dictionary<string, string> values, string prefix, string suffix)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        ValidatePlaceholderArguments(values, prefix, suffix);

        int count = 0;

        foreach (DocxTableCell cell in row.Cells)
        {
            count = count + ReplacePlaceholders(cell, values, prefix, suffix);
        }

        if (count > 0)
        {
            row.Xml = "";
        }

        return count;
    }

    /// <summary>
    /// Replaces matching template placeholders and returns the number of replacements.
    /// </summary>
    /// <param name="cell">The cell used by the operation.</param>
    /// <param name="values">The named values to apply.</param>
    /// <param name="prefix">The opening delimiter or namespace prefix, as required by the operation.</param>
    /// <param name="suffix">The closing delimiter.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int ReplacePlaceholders(DocxTableCell cell, Dictionary<string, string> values, string prefix, string suffix)
    {
        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }

        ValidatePlaceholderArguments(values, prefix, suffix);

        int count = 0;

        foreach (DocxParagraph paragraph in cell.Paragraphs)
        {
            count = count + ReplacePlaceholders(paragraph, values, prefix, suffix);
        }

        foreach (DocxTable table in cell.Tables)
        {
            count = count + ReplacePlaceholders(table, values, prefix, suffix);
        }

        if (count > 0)
        {
            cell.Xml = "";
        }

        return count;
    }

    /// <summary>
    /// Replaces matching template placeholders and returns the number of replacements.
    /// </summary>
    /// <param name="contentControl">The content control used by the operation.</param>
    /// <param name="values">The named values to apply.</param>
    /// <param name="prefix">The opening delimiter or namespace prefix, as required by the operation.</param>
    /// <param name="suffix">The closing delimiter.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int ReplacePlaceholders(DocxContentControl contentControl, Dictionary<string, string> values, string prefix, string suffix)
    {
        if (contentControl == null)
        {
            throw new ArgumentNullException("contentControl");
        }

        ValidatePlaceholderArguments(values, prefix, suffix);

        int count = 0;
        List<DocxXmlBuffer> replacedChildren = new List<DocxXmlBuffer>();

        foreach (DocxXmlBuffer child in contentControl.ContentChildren)
        {
            DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

            if (reader.MoveToContent() && DocxContentControl.IsSupported(reader))
            {
                DocxContentControl childControl = DocxContentControl.DocxContentControlFromBuffer(child);
                int replacedCount = ReplacePlaceholders(childControl, values, prefix, suffix);
                count = count + replacedCount;
                AddReplacedChild(replacedChildren, child, childControl.WriteXml(), replacedCount);
            }
            else if (reader.IsElement("p", DocxConstants.NamespaceWordprocessingMl))
            {
                DocxParagraph paragraph = DocxContentControl.DocxParagraphFromBuffer(child);
                int replacedCount = ReplacePlaceholders(paragraph, values, prefix, suffix);
                count = count + replacedCount;
                AddReplacedChild(replacedChildren, child, paragraph.WriteXml(), replacedCount);
            }
            else if (reader.IsElement("tbl", DocxConstants.NamespaceWordprocessingMl))
            {
                DocxTable table = DocxContentControl.DocxTableFromBuffer(child);
                int replacedCount = ReplacePlaceholders(table, values, prefix, suffix);
                count = count + replacedCount;
                AddReplacedChild(replacedChildren, child, table.WriteXml(), replacedCount);
            }
            else if (reader.IsElement("r", DocxConstants.NamespaceWordprocessingMl))
            {
                DocxRun run = DocxContentControl.DocxRunFromBuffer(child);
                int replacedCount = ReplacePlaceholders(run, values, prefix, suffix);
                count = count + replacedCount;
                AddReplacedChild(replacedChildren, child, run.WriteXml(), replacedCount);
            }
            else
            {
                replacedChildren.Add(child);
            }
        }

        if (count > 0)
        {
            contentControl.ContentXml = DocxContentControl.CreateContentXmlFromChildren(replacedChildren);
            contentControl.Xml = "";
            contentControl.LoadContentFromXml(contentControl.ContentXml);
        }

        return count;
    }

    /// <summary>
    /// Repeats table row in this template.
    /// </summary>
    /// <param name="table">The table used by the operation.</param>
    /// <param name="templateRowIndex">The template row index used by the operation.</param>
    /// <param name="data">The data used by the operation.</param>
    /// <returns>The number of affected or available items.</returns>
    public static int RepeatTableRow(DocxTable table, int templateRowIndex, DocxTableData data)
    {
        return RepeatTableRow(table, templateRowIndex, data, DefaultPlaceholderPrefix, DefaultPlaceholderSuffix);
    }

    /// <summary>
    /// Repeats table row in this template.
    /// </summary>
    /// <param name="table">The table used by the operation.</param>
    /// <param name="templateRowIndex">The template row index used by the operation.</param>
    /// <param name="data">The data used by the operation.</param>
    /// <param name="prefix">The opening delimiter or namespace prefix, as required by the operation.</param>
    /// <param name="suffix">The closing delimiter.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int RepeatTableRow(DocxTable table, int templateRowIndex, DocxTableData data, string prefix, string suffix)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        ValidateDelimiter("prefix", prefix);
        ValidateDelimiter("suffix", suffix);
        ValidateTableRowIndex(table, templateRowIndex);

        DocxTableRow templateRow = table.GetRow(templateRowIndex);
        string templateXml = templateRow.WriteXml();
        table.Rows.RemoveAt(templateRowIndex);
        int rowIndex = 0;

        foreach (Dictionary<string, string> rowValues in data.Rows)
        {
            DocxTableRow row = new DocxTableRow(templateXml);
            ReplacePlaceholders(row, rowValues, prefix, suffix);
            table.Rows.Insert(templateRowIndex + rowIndex, row);
            rowIndex = rowIndex + 1;
        }

        table.Xml = "";
        return data.RowCount();
    }

    /// <summary>
    /// Removes block from this template.
    /// </summary>
    /// <param name="document">The document to inspect or modify.</param>
    /// <param name="key">The key used by the operation.</param>
    /// <returns>The number of affected or available items.</returns>
    public static int RemoveBlock(DocxDocument document, string key)
    {
        return RemoveBlock(document, key, DefaultPlaceholderPrefix, DefaultPlaceholderSuffix);
    }

    /// <summary>
    /// Removes block from this template.
    /// </summary>
    /// <param name="document">The document to inspect or modify.</param>
    /// <param name="key">The key used by the operation.</param>
    /// <param name="prefix">The opening delimiter or namespace prefix, as required by the operation.</param>
    /// <param name="suffix">The closing delimiter.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int RemoveBlock(DocxDocument document, string key, string prefix, string suffix)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        int count = RemoveBlock(document.Body, key, prefix, suffix);

        if (count > 0)
        {
            UpdateMainDocumentBody(document);
        }

        return count;
    }

    /// <summary>
    /// Removes block from this template.
    /// </summary>
    /// <param name="body">The body used by the operation.</param>
    /// <param name="key">The key used by the operation.</param>
    /// <returns>The number of affected or available items.</returns>
    public static int RemoveBlock(DocxBody body, string key)
    {
        return RemoveBlock(body, key, DefaultPlaceholderPrefix, DefaultPlaceholderSuffix);
    }

    /// <summary>
    /// Removes block from this template.
    /// </summary>
    /// <param name="body">The body used by the operation.</param>
    /// <param name="key">The key used by the operation.</param>
    /// <param name="prefix">The opening delimiter or namespace prefix, as required by the operation.</param>
    /// <param name="suffix">The closing delimiter.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int RemoveBlock(DocxBody body, string key, string prefix, string suffix)
    {
        if (body == null)
        {
            throw new ArgumentNullException("body");
        }

        string marker = CreatePlaceholder(key, prefix, suffix);
        int count = 0;
        int index = body.Blocks.Count - 1;

        while (index >= 0)
        {
            if (BlockContainsText(body.Blocks[index], marker))
            {
                body.Blocks.RemoveAt(index);
                count = count + 1;
            }

            index = index - 1;
        }

        if (count > 0)
        {
            body.Xml = body.WriteXml();
            body.LoadBlocksFromXml();
            body.LoadSectionsFromXml();
        }

        return count;
    }

    internal static bool BlockContainsText(DocxBlock block, string text)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text == "")
        {
            throw new ArgumentException("Text is required.", "text");
        }

        return block.GetText().IndexOf(text, 0, StringComparison.Ordinal) >= 0;
    }

    internal static void ValidateTableRowIndex(DocxTable table, int index)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (index < 0 || index >= table.RowCount())
        {
            throw new ArgumentOutOfRangeException("index");
        }
    }

    internal static void AddReplacedChild(List<DocxXmlBuffer> children, DocxXmlBuffer originalChild, string replacementXml, int replacementCount)
    {
        if (children == null)
        {
            throw new ArgumentNullException("children");
        }

        if (originalChild == null)
        {
            throw new ArgumentNullException("originalChild");
        }

        if (replacementXml == null)
        {
            throw new ArgumentNullException("replacementXml");
        }

        if (replacementCount > 0)
        {
            children.Add(new DocxXmlBuffer(replacementXml));
        }
        else
        {
            children.Add(originalChild);
        }
    }

    /// <summary>
    /// Creates placeholder for this template.
    /// </summary>
    /// <param name="key">The key used by the operation.</param>
    /// <param name="prefix">The opening delimiter or namespace prefix, as required by the operation.</param>
    /// <param name="suffix">The closing delimiter.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty, malformed, or not supported.</exception>
    public static string CreatePlaceholder(string key, string prefix, string suffix)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        ValidateDelimiter("prefix", prefix);
        ValidateDelimiter("suffix", suffix);

        if (key == "")
        {
            throw new ArgumentException("Placeholder key is required.", "key");
        }

        return prefix + key + suffix;
    }

    internal static void ValidatePlaceholderArguments(Dictionary<string, string> values, string prefix, string suffix)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        ValidateDelimiter("prefix", prefix);
        ValidateDelimiter("suffix", suffix);

        foreach (KeyValuePair<string, string> value in values)
        {
            ValidatePlaceholderValue(value.Key, value.Value);
        }
    }

    internal static void ValidatePlaceholderValue(string key, string value)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (key == "")
        {
            throw new ArgumentException("Placeholder key is required.", "key");
        }
    }

    internal static void ValidateDelimiter(string name, string value)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (value == null)
        {
            throw new ArgumentNullException(name);
        }

        if (value == "")
        {
            throw new ArgumentException("Delimiter is required.", name);
        }
    }

    /// <summary>
    /// Serializes the edited body back into the main document part. Call this after direct body edits and before saving the package.
    /// </summary>
    /// <param name="document">The document to inspect or modify.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static void UpdateMainDocumentBody(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (document.MainDocumentPart == null)
        {
            return;
        }

        string partName = document.MainDocumentPart.PartName;
        string contentType = document.MainDocumentPart.ContentType;
        string bodyXml = document.Body.WriteXml();

        if (bodyXml != document.BodyModelXml)
        {
            string documentXml = ReplaceBodyXml(document.MainDocumentPart.GetText(), bodyXml);
            document.Package.ReplaceXmlPart(partName, contentType, documentXml);
            document.MainDocumentPart = document.Package.FindPart(partName);
            document.BodyModelXml = bodyXml;
        }

        document.UpdateHeaderFooterParts();
    }

    internal static string ReplaceBodyXml(string documentXml, string bodyXml)
    {
        if (documentXml == null)
        {
            throw new ArgumentNullException("documentXml");
        }

        if (bodyXml == null)
        {
            throw new ArgumentNullException("bodyXml");
        }

        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(documentXml);

        XmlNodeList bodies = document.GetElementsByTagName("body", DocxConstants.NamespaceWordprocessingMl);

        if (bodies.Count == 0)
        {
            return documentXml;
        }
        XmlNode? body = bodies[0];

        if (body == null || body.ParentNode == null)
        {
            return documentXml;
        }

        List<string> modeledChildNames = new List<string>();
        modeledChildNames.Add("p");
        modeledChildNames.Add("tbl");
        modeledChildNames.Add("sdt");
        modeledChildNames.Add("sectPr");
        string preservedBodyXml = DocxXmlPreservation.PreserveContainerContent(body.OuterXml, bodyXml, modeledChildNames);
        XmlDocument bodyDocument = new XmlDocument();
        bodyDocument.PreserveWhitespace = true;
        bodyDocument.LoadXml(preservedBodyXml);

        if (bodyDocument.DocumentElement == null)
        {
            return documentXml;
        }

        XmlNode importedBody = document.ImportNode(bodyDocument.DocumentElement, true);

        body.ParentNode.ReplaceChild(importedBody, body);
        return document.OuterXml;
    }
}
