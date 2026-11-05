using System.Xml;

namespace docx;

/// <summary>
/// Traverses, creates, reads, binds, and fills supported content controls.
/// </summary>
public class DocxContentControlManagement : IDocxContentControlManagement
{
    /// <summary>Creates a content-control management service.</summary>
    public DocxContentControlManagement()
    {
    }

    /// <summary>Returns content controls from all supported document stories.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The controls in story and nesting order.</returns>
    public List<DocxContentControlInfo> GetDocumentContentControls(DocxDocument document)
    {
        ValidateDocument(document);
        List<DocxContentControlInfo> controls = new List<DocxContentControlInfo>();

        foreach (DocxContentControl contentControl in document.GetContentControls())
        {
            controls.Add(GetContentControlInfo(contentControl));
        }

        return controls;
    }

    /// <summary>Returns all controls nested below one content control.</summary>
    /// <param name="contentControl">The containing control.</param>
    /// <returns>The descendants in XML order.</returns>
    public List<DocxContentControlInfo> GetDescendantContentControls(DocxContentControl contentControl)
    {
        ValidateContentControl(contentControl);
        List<DocxContentControlInfo> controls = new List<DocxContentControlInfo>();
        XmlDocument document = LoadControlDocument(contentControl);

        if (document.DocumentElement != null)
        {
            AddDescendantControlNodes(controls, document.DocumentElement, true);
        }

        return controls;
    }

    /// <summary>Returns metadata and type-specific values for one control.</summary>
    /// <param name="contentControl">The control to inspect.</param>
    /// <returns>The content-control descriptor.</returns>
    public DocxContentControlInfo GetContentControlInfo(DocxContentControl contentControl)
    {
        ValidateContentControl(contentControl);
        XmlDocument document = LoadControlDocument(contentControl);
        XmlElement? properties = FindFirstElement(document.DocumentElement, "sdtPr");
        XmlElement? checkbox = FindFirstElement(properties, "checkbox");
        XmlElement? checkedElement = FindFirstElement(checkbox, "checked");
        XmlElement? date = FindFirstElement(properties, "date");
        XmlElement? dateFormat = FindFirstElement(date, "dateFormat");
        XmlElement? dateLanguage = FindFirstElement(date, "lid");
        string checkedValue = GetAttributeByLocalName(checkedElement, "val");
        List<DocxContentControlListItem> items = ReadListItems(properties);
        bool hasCheckedValue = checkedValue != "";
        bool isChecked = false;

        if (hasCheckedValue)
        {
            isChecked = DocxXmlAttributes.ParseBool(checkedValue);
        }

        return new DocxContentControlInfo(
            contentControl,
            GetKind(contentControl, properties),
            contentControl.Alias,
            contentControl.Tag,
            contentControl.DataBindingStoreItemId,
            contentControl.DataBindingXPath,
            contentControl.DataBindingPrefixMappings,
            contentControl.GetValue(),
            hasCheckedValue,
            isChecked,
            GetAttributeByLocalName(date, "fullDate"),
            GetAttributeByLocalName(dateFormat, "val"),
            GetAttributeByLocalName(dateLanguage, "val"),
            items);
    }

    /// <summary>Finds the first document control with a tag.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="tag">The tag to find.</param>
    /// <returns>The matching control, or null.</returns>
    public DocxContentControl? FindContentControlByTag(DocxDocument document, string tag)
    {
        ValidateDocument(document);
        ValidateString(tag, "tag");

        foreach (DocxContentControl contentControl in document.GetContentControls())
        {
            if (contentControl.Tag == tag)
            {
                return contentControl;
            }
        }

        return null;
    }

    /// <summary>Finds the first document control with an alias.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="alias">The alias to find.</param>
    /// <returns>The matching control, or null.</returns>
    public DocxContentControl? FindContentControlByAlias(DocxDocument document, string alias)
    {
        ValidateDocument(document);
        ValidateString(alias, "alias");

        foreach (DocxContentControl contentControl in document.GetContentControls())
        {
            if (contentControl.Alias == alias)
            {
                return contentControl;
            }
        }

        return null;
    }

    /// <summary>Sets the user-facing alias.</summary>
    /// <param name="contentControl">The control to edit.</param>
    /// <param name="alias">The alias value.</param>
    public void SetContentControlAlias(DocxContentControl contentControl, string alias)
    {
        SetPropertyElementAttributes(contentControl, "alias", new string[] { "val" }, new string[] { alias });
    }

    /// <summary>Sets the programmatic tag.</summary>
    /// <param name="contentControl">The control to edit.</param>
    /// <param name="tag">The tag value.</param>
    public void SetContentControlTag(DocxContentControl contentControl, string tag)
    {
        SetPropertyElementAttributes(contentControl, "tag", new string[] { "val" }, new string[] { tag });
    }

    /// <summary>Sets custom XML data-binding metadata.</summary>
    /// <param name="contentControl">The control to edit.</param>
    /// <param name="storeItemId">The custom XML store item identifier.</param>
    /// <param name="xpath">The binding XPath.</param>
    /// <param name="prefixMappings">The namespace prefix mappings.</param>
    public void SetContentControlDataBinding(DocxContentControl contentControl, string storeItemId, string xpath, string prefixMappings)
    {
        SetPropertyElementAttributes(
            contentControl,
            "dataBinding",
            new string[] { "storeItemID", "xpath", "prefixMappings" },
            new string[] { storeItemId, xpath, prefixMappings });
    }

    /// <summary>Removes custom XML data-binding metadata.</summary>
    /// <param name="contentControl">The control to edit.</param>
    public void ClearContentControlDataBinding(DocxContentControl contentControl)
    {
        RemovePropertyElement(contentControl, "dataBinding");
    }

    /// <summary>Returns a control's visible value.</summary>
    /// <param name="contentControl">The control to inspect.</param>
    /// <returns>The visible value.</returns>
    public string GetContentControlValue(DocxContentControl contentControl)
    {
        ValidateContentControl(contentControl);
        return contentControl.GetValue();
    }

    /// <summary>Replaces a control's visible value.</summary>
    /// <param name="contentControl">The control to edit.</param>
    /// <param name="value">The replacement value.</param>
    public void SetContentControlValue(DocxContentControl contentControl, string value)
    {
        ValidateContentControl(contentControl);
        contentControl.SetValue(value);
    }

    /// <summary>Fills tagged or aliased controls throughout all supported stories.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="values">Values keyed by tag, falling back to alias.</param>
    /// <returns>The number of filled controls.</returns>
    public int FillDocumentContentControls(DocxDocument document, Dictionary<string, string> values)
    {
        ValidateDocument(document);

        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        int count = document.Body.FillContentControls(values);

        foreach (DocxHeader header in document.Headers)
        {
            count = count + FillBlockList(header.Blocks, values);
        }

        foreach (DocxFooter footer in document.Footers)
        {
            count = count + FillBlockList(footer.Blocks, values);
        }

        foreach (DocxFootnote footnote in document.Footnotes.Footnotes)
        {
            count = count + FillBlockList(footnote.Blocks, values);
        }

        foreach (DocxEndnote endnote in document.Endnotes.Endnotes)
        {
            count = count + FillBlockList(endnote.Blocks, values);
        }

        foreach (DocxComment comment in document.Comments.Comments)
        {
            count = count + FillBlockList(comment.Blocks, values);
        }

        if (count > 0)
        {
            DocxTemplate.UpdateMainDocumentBody(document);

            if (document.FootnotesPart != null)
            {
                document.WriteFootnotesPart();
            }

            if (document.EndnotesPart != null)
            {
                document.WriteEndnotesPart();
            }

            if (document.CommentsPart != null)
            {
                document.WriteCommentsPart();
            }
        }

        return count;
    }

    /// <summary>Creates an inline plain-text control.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="value">The initial value.</param>
    /// <returns>The created control.</returns>
    public DocxContentControl CreatePlainTextControl(string alias, string tag, string value)
    {
        return CreateTypedTextControl(DocxContentControlKind.PlainText, alias, tag, value, false);
    }

    /// <summary>Creates a block-level rich-text control.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="blocks">The initial rich-text blocks.</param>
    /// <returns>The created control.</returns>
    public DocxContentControl CreateRichTextControl(string alias, string tag, List<DocxBlock> blocks)
    {
        string contentXml = CreateBlockContentXml(blocks);
        return CreateControl(DocxContentControlKind.RichText, alias, tag, contentXml, null, "", "", "", false);
    }

    /// <summary>Replaces rich-text control blocks.</summary>
    /// <param name="contentControl">The control to edit.</param>
    /// <param name="blocks">The replacement blocks.</param>
    public void SetRichTextBlocks(DocxContentControl contentControl, List<DocxBlock> blocks)
    {
        RequireKind(contentControl, DocxContentControlKind.RichText);
        contentControl.ContentXml = CreateBlockContentXml(blocks);
        contentControl.Xml = "";
        contentControl.LoadContentFromXml(contentControl.ContentXml);
    }

    /// <summary>Creates a checkbox control.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="isChecked">The initial checked state.</param>
    /// <returns>The created control.</returns>
    public DocxContentControl CreateCheckboxControl(string alias, string tag, bool isChecked)
    {
        string value = "☐";

        if (isChecked)
        {
            value = "☒";
        }

        return CreateControl(DocxContentControlKind.Checkbox, alias, tag, DocxContentControl.CreateRunValueContentXml(value), null, "", "", "", isChecked);
    }

    /// <summary>Sets a checkbox state and visible glyph.</summary>
    /// <param name="contentControl">The checkbox control.</param>
    /// <param name="isChecked">The checked state.</param>
    public void SetCheckboxValue(DocxContentControl contentControl, bool isChecked)
    {
        RequireKind(contentControl, DocxContentControlKind.Checkbox);
        XmlDocument document = LoadControlDocument(contentControl);
        XmlElement properties = GetOrCreateProperties(document);
        XmlElement? checkbox = FindFirstElement(properties, "checkbox");

        if (checkbox == null)
        {
            checkbox = GetOrCreateNamespacedChild(document, properties, "checkbox", "w14", DocxContentControl.NamespaceWordprocessingMl2010);
        }

        XmlElement checkedElement = GetOrCreateNamespacedChild(document, checkbox, "checked", checkbox.Prefix, checkbox.NamespaceURI);
        SetNamespacedAttribute(document, checkedElement, "val", checkbox.Prefix, checkbox.NamespaceURI, BoolText(isChecked));
        ApplyControlDocument(contentControl, document);

        if (isChecked)
        {
            contentControl.SetValue("☒");
        }
        else
        {
            contentControl.SetValue("☐");
        }
    }

    /// <summary>Creates an editable combo-box control.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="items">The available items.</param>
    /// <param name="selectedValue">The initially selected stored value.</param>
    /// <returns>The created control.</returns>
    public DocxContentControl CreateComboBoxControl(string alias, string tag, List<DocxContentControlListItem> items, string selectedValue)
    {
        string displayText = FindListDisplayText(items, selectedValue);
        return CreateControl(DocxContentControlKind.ComboBox, alias, tag, DocxContentControl.CreateRunValueContentXml(displayText), items, "", "", "", false);
    }

    /// <summary>Creates a drop-down-list control.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="items">The available items.</param>
    /// <param name="selectedValue">The initially selected stored value.</param>
    /// <returns>The created control.</returns>
    public DocxContentControl CreateDropDownListControl(string alias, string tag, List<DocxContentControlListItem> items, string selectedValue)
    {
        string displayText = FindListDisplayText(items, selectedValue);
        return CreateControl(DocxContentControlKind.DropDownList, alias, tag, DocxContentControl.CreateRunValueContentXml(displayText), items, "", "", "", false);
    }

    /// <summary>Selects an item by stored value.</summary>
    /// <param name="contentControl">The combo-box or drop-down-list control.</param>
    /// <param name="selectedValue">The stored value to select.</param>
    public void SetListSelection(DocxContentControl contentControl, string selectedValue)
    {
        DocxContentControlInfo info = GetContentControlInfo(contentControl);

        if (info.Kind != DocxContentControlKind.ComboBox && info.Kind != DocxContentControlKind.DropDownList)
        {
            throw new ArgumentException("Content control is not a list control.", "contentControl");
        }

        contentControl.SetValue(FindListDisplayText(info.ListItems, selectedValue));
    }

    /// <summary>Creates a date-picker control.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="dateValue">The full-date value.</param>
    /// <param name="displayText">The displayed date.</param>
    /// <param name="format">The display format.</param>
    /// <param name="language">The language identifier.</param>
    /// <returns>The created control.</returns>
    public DocxContentControl CreateDatePickerControl(string alias, string tag, string dateValue, string displayText, string format, string language)
    {
        ValidateString(displayText, "displayText");
        return CreateControl(DocxContentControlKind.DatePicker, alias, tag, DocxContentControl.CreateRunValueContentXml(displayText), null, dateValue, format, language, false);
    }

    /// <summary>Sets a date-picker value and displayed date.</summary>
    /// <param name="contentControl">The date-picker control.</param>
    /// <param name="dateValue">The full-date value.</param>
    /// <param name="displayText">The displayed date.</param>
    public void SetDatePickerValue(DocxContentControl contentControl, string dateValue, string displayText)
    {
        RequireKind(contentControl, DocxContentControlKind.DatePicker);
        XmlDocument document = LoadControlDocument(contentControl);
        XmlElement properties = GetOrCreateProperties(document);
        XmlElement date = GetOrCreateChildElement(document, properties, "date");
        SetWordAttribute(document, date, "fullDate", dateValue);
        ApplyControlDocument(contentControl, document);
        contentControl.SetValue(displayText);
    }

    /// <summary>Creates a picture control around a run containing picture markup.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="pictureRun">The run containing the picture.</param>
    /// <returns>The created control.</returns>
    public DocxContentControl CreatePictureControl(string alias, string tag, DocxRun pictureRun)
    {
        return CreateControl(DocxContentControlKind.Picture, alias, tag, CreateRunContentXml(pictureRun), null, "", "", "", false);
    }

    /// <summary>Replaces the run contained by a picture control.</summary>
    /// <param name="contentControl">The picture control.</param>
    /// <param name="pictureRun">The replacement picture run.</param>
    public void SetPictureContent(DocxContentControl contentControl, DocxRun pictureRun)
    {
        RequireKind(contentControl, DocxContentControlKind.Picture);
        contentControl.ContentXml = CreateRunContentXml(pictureRun);
        contentControl.Xml = "";
        contentControl.LoadContentFromXml(contentControl.ContentXml);
    }

    /// <summary>Creates a repeating section containing repeating-section items.</summary>
    /// <param name="alias">The alias.</param>
    /// <param name="tag">The tag.</param>
    /// <param name="itemValues">The initial item values.</param>
    /// <returns>The created repeating section.</returns>
    public DocxContentControl CreateRepeatingSectionControl(string alias, string tag, List<string> itemValues)
    {
        if (itemValues == null)
        {
            throw new ArgumentNullException("itemValues");
        }

        List<DocxXmlBuffer> children = new List<DocxXmlBuffer>();

        foreach (string value in itemValues)
        {
            DocxContentControl item = CreateTypedTextControl(DocxContentControlKind.RepeatingSectionItem, "", "", value, true);
            children.Add(new DocxXmlBuffer(item.WriteXml()));
        }

        return CreateControl(DocxContentControlKind.RepeatingSection, alias, tag, DocxContentControl.CreateContentXmlFromChildren(children), null, "", "", "", false);
    }

    /// <summary>Appends a repeating-section item.</summary>
    /// <param name="contentControl">The repeating-section control.</param>
    /// <param name="value">The item value.</param>
    /// <returns>The created item control.</returns>
    public DocxContentControl AddRepeatingSectionItem(DocxContentControl contentControl, string value)
    {
        RequireKind(contentControl, DocxContentControlKind.RepeatingSection);
        DocxContentControl item = CreateTypedTextControl(DocxContentControlKind.RepeatingSectionItem, "", "", value, true);
        List<DocxXmlBuffer> children = new List<DocxXmlBuffer>(contentControl.ContentChildren);
        children.Add(new DocxXmlBuffer(item.WriteXml()));
        contentControl.ContentXml = DocxContentControl.CreateContentXmlFromChildren(children);
        contentControl.Xml = "";
        contentControl.LoadContentFromXml(contentControl.ContentXml);
        return item;
    }

    internal static void ValidateDocument(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
    }

    internal static void ValidateContentControl(DocxContentControl contentControl)
    {
        if (contentControl == null)
        {
            throw new ArgumentNullException("contentControl");
        }
    }

    internal static void ValidateString(string value, string name)
    {
        if (value == null)
        {
            throw new ArgumentNullException(name);
        }
    }

    internal DocxContentControlKind GetKind(DocxContentControl contentControl, XmlElement? properties)
    {
        string type = contentControl.Type;

        if (type == "" && FindFirstElement(properties, "checkbox") != null)
        {
            type = "checkbox";
        }

        switch (type)
        {
            case "text":
                return DocxContentControlKind.PlainText;
            case "richText":
                return DocxContentControlKind.RichText;
            case "repeatingSection":
                return DocxContentControlKind.RepeatingSection;
            case "repeatingSectionItem":
                return DocxContentControlKind.RepeatingSectionItem;
            case "checkbox":
                return DocxContentControlKind.Checkbox;
            case "comboBox":
                return DocxContentControlKind.ComboBox;
            case "dropDownList":
                return DocxContentControlKind.DropDownList;
            case "date":
                return DocxContentControlKind.DatePicker;
            case "picture":
                return DocxContentControlKind.Picture;
            default:
                return DocxContentControlKind.Unknown;
        }
    }

    internal static XmlDocument LoadControlDocument(DocxContentControl contentControl)
    {
        string xml = contentControl.WriteXml();

        if (xml == "")
        {
            xml = "<w:sdt xmlns:w=\"" + DocxConstants.NamespaceWordprocessingMl + "\"><w:sdtPr /><w:sdtContent /></w:sdt>";
        }

        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(xml);
        return document;
    }

    internal static XmlElement? FindFirstElement(XmlNode? node, string localName)
    {
        if (node == null)
        {
            return null;
        }

        foreach (XmlNode child in node.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element && child.LocalName == localName)
            {
                return (XmlElement)child;
            }
        }

        return null;
    }

    internal static string GetAttributeByLocalName(XmlElement? element, string localName)
    {
        if (element == null)
        {
            return "";
        }

        foreach (XmlAttribute attribute in element.Attributes)
        {
            if (attribute.LocalName == localName)
            {
                return attribute.Value;
            }
        }

        return "";
    }

    internal static List<DocxContentControlListItem> ReadListItems(XmlElement? properties)
    {
        List<DocxContentControlListItem> items = new List<DocxContentControlListItem>();
        XmlElement? list = FindFirstElement(properties, "comboBox");

        if (list == null)
        {
            list = FindFirstElement(properties, "dropDownList");
        }

        if (list == null)
        {
            return items;
        }

        foreach (XmlNode child in list.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element && child.LocalName == "listItem")
            {
                XmlElement item = (XmlElement)child;
                items.Add(new DocxContentControlListItem(GetAttributeByLocalName(item, "displayText"), GetAttributeByLocalName(item, "value")));
            }
        }

        return items;
    }

    internal void AddDescendantControlNodes(List<DocxContentControlInfo> controls, XmlNode node, bool isRoot)
    {
        if (!isRoot && node.NodeType == XmlNodeType.Element && node.LocalName == "sdt" && node.NamespaceURI == DocxConstants.NamespaceWordprocessingMl)
        {
            DocxContentControl contentControl = DocxContentControl.ParseXml(DocxRevisionManagement.GetStandaloneNodeXml(node));
            controls.Add(GetContentControlInfo(contentControl));
        }

        foreach (XmlNode child in node.ChildNodes)
        {
            AddDescendantControlNodes(controls, child, false);
        }
    }

    internal static void SetPropertyElementAttributes(DocxContentControl contentControl, string localName, string[] attributeNames, string[] values)
    {
        ValidateContentControl(contentControl);

        if (attributeNames == null || values == null)
        {
            throw new ArgumentNullException("values");
        }

        if (attributeNames.Length != values.Length)
        {
            throw new ArgumentException("Attribute names and values must have equal lengths.", "values");
        }

        XmlDocument document = LoadControlDocument(contentControl);
        XmlElement properties = GetOrCreateProperties(document);
        XmlElement element = GetOrCreateChildElement(document, properties, localName);
        int index = 0;

        while (index < attributeNames.Length)
        {
            ValidateString(values[index], attributeNames[index]);
            SetWordAttribute(document, element, attributeNames[index], values[index]);
            index = index + 1;
        }

        ApplyControlDocument(contentControl, document);
    }

    internal static void RemovePropertyElement(DocxContentControl contentControl, string localName)
    {
        ValidateContentControl(contentControl);
        XmlDocument document = LoadControlDocument(contentControl);
        XmlElement? properties = FindFirstElement(document.DocumentElement, "sdtPr");
        XmlElement? element = FindFirstElement(properties, localName);

        if (element != null && properties != null)
        {
            properties.RemoveChild(element);
            ApplyControlDocument(contentControl, document);
        }
    }

    internal static XmlElement GetOrCreateProperties(XmlDocument document)
    {
        XmlElement? properties = FindFirstElement(document.DocumentElement, "sdtPr");

        if (properties != null)
        {
            return properties;
        }

        XmlElement created = document.CreateElement("w", "sdtPr", DocxConstants.NamespaceWordprocessingMl);
        XmlElement? root = document.DocumentElement;

        if (root == null)
        {
            throw new InvalidOperationException("Content control root is missing.");
        }

        if (root.FirstChild == null)
        {
            root.AppendChild(created);
        }
        else
        {
            root.InsertBefore(created, root.FirstChild);
        }

        return created;
    }

    internal static XmlElement GetOrCreateChildElement(XmlDocument document, XmlElement parent, string localName)
    {
        XmlElement? element = FindFirstElement(parent, localName);

        if (element != null)
        {
            return element;
        }

        element = document.CreateElement("w", localName, DocxConstants.NamespaceWordprocessingMl);
        parent.AppendChild(element);
        return element;
    }

    internal static void SetWordAttribute(XmlDocument document, XmlElement element, string localName, string value)
    {
        ValidateString(value, localName);
        XmlAttribute attribute = document.CreateAttribute("w", localName, DocxConstants.NamespaceWordprocessingMl);
        attribute.Value = value;
        element.Attributes.SetNamedItem(attribute);
    }

    internal static void ApplyControlDocument(DocxContentControl contentControl, XmlDocument document)
    {
        contentControl.Xml = document.OuterXml;
        contentControl.LoadFromXml();
    }

    internal static string BoolText(bool value)
    {
        if (value)
        {
            return "1";
        }

        return "0";
    }

    internal static int FillBlockList(List<DocxBlock> blocks, Dictionary<string, string> values)
    {
        int count = 0;

        foreach (DocxBlock block in blocks)
        {
            count = count + block.FillContentControls(values);
        }

        return count;
    }

    internal static void RequireKind(DocxContentControl contentControl, DocxContentControlKind kind)
    {
        ValidateContentControl(contentControl);
        DocxContentControlManagement management = new DocxContentControlManagement();

        if (management.GetContentControlInfo(contentControl).Kind != kind)
        {
            throw new ArgumentException("Content control has an incompatible type.", "contentControl");
        }
    }

    internal static DocxContentControl CreateTypedTextControl(DocxContentControlKind kind, string alias, string tag, string value, bool block)
    {
        ValidateString(value, "value");
        string contentXml;

        if (block)
        {
            contentXml = DocxContentControl.CreateParagraphValueContentXml(value);
        }
        else
        {
            contentXml = DocxContentControl.CreateRunValueContentXml(value);
        }

        return CreateControl(kind, alias, tag, contentXml, null, "", "", "", false);
    }

    internal static DocxContentControl CreateControl(DocxContentControlKind kind, string alias, string tag, string contentXml, List<DocxContentControlListItem>? items, string dateValue, string dateFormat, string dateLanguage, bool checkboxValue)
    {
        ValidateString(alias, "alias");
        ValidateString(tag, "tag");
        ValidateString(contentXml, "contentXml");
        DocxXmlWriter writer = DocxXmlWriter.Create();
        writer.StartElement("w", "sdt", DocxConstants.NamespaceWordprocessingMl);
        writer.StartElement("w", "sdtPr", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalProperty(writer, "alias", alias);
        WriteOptionalProperty(writer, "tag", tag);
        WriteTypeProperties(writer, kind, items, dateValue, dateFormat, dateLanguage, checkboxValue);
        writer.EndElement();
        new DocxXmlBuffer(contentXml).WriteTo(writer);
        writer.EndElement();
        return new DocxContentControl(writer.GetXml());
    }

    internal static void WriteOptionalProperty(DocxXmlWriter writer, string localName, string value)
    {
        if (value == "")
        {
            return;
        }

        writer.StartElement("w", localName, DocxConstants.NamespaceWordprocessingMl);
        writer.WriteAttribute("w", "val", DocxConstants.NamespaceWordprocessingMl, value);
        writer.EndElement();
    }

    internal static void WriteTypeProperties(DocxXmlWriter writer, DocxContentControlKind kind, List<DocxContentControlListItem>? items, string dateValue, string dateFormat, string dateLanguage, bool checkboxValue)
    {
        string localName = TypeLocalName(kind);

        if (kind == DocxContentControlKind.Checkbox)
        {
            writer.StartElement("w14", localName, DocxContentControl.NamespaceWordprocessingMl2010);
        }
        else
        {
            writer.StartElement("w", localName, DocxConstants.NamespaceWordprocessingMl);
        }

        if (kind == DocxContentControlKind.Checkbox)
        {
            writer.StartElement("w14", "checked", DocxContentControl.NamespaceWordprocessingMl2010);
            writer.WriteAttribute("w14", "val", DocxContentControl.NamespaceWordprocessingMl2010, BoolText(checkboxValue));
            writer.EndElement();
        }

        if (kind == DocxContentControlKind.ComboBox || kind == DocxContentControlKind.DropDownList)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            foreach (DocxContentControlListItem item in items)
            {
                if (item == null)
                {
                    throw new ArgumentException("List items cannot contain null.", "items");
                }

                writer.StartElement("w", "listItem", DocxConstants.NamespaceWordprocessingMl);
                writer.WriteAttribute("w", "displayText", DocxConstants.NamespaceWordprocessingMl, item.DisplayText);
                writer.WriteAttribute("w", "value", DocxConstants.NamespaceWordprocessingMl, item.Value);
                writer.EndElement();
            }
        }

        if (kind == DocxContentControlKind.DatePicker)
        {
            writer.WriteAttribute("w", "fullDate", DocxConstants.NamespaceWordprocessingMl, dateValue);
            WriteOptionalProperty(writer, "dateFormat", dateFormat);
            WriteOptionalProperty(writer, "lid", dateLanguage);
        }

        writer.EndElement();
    }

    internal static string TypeLocalName(DocxContentControlKind kind)
    {
        switch (kind)
        {
            case DocxContentControlKind.PlainText:
                return "text";
            case DocxContentControlKind.RichText:
                return "richText";
            case DocxContentControlKind.RepeatingSection:
                return "repeatingSection";
            case DocxContentControlKind.RepeatingSectionItem:
                return "repeatingSectionItem";
            case DocxContentControlKind.Checkbox:
                return "checkbox";
            case DocxContentControlKind.ComboBox:
                return "comboBox";
            case DocxContentControlKind.DropDownList:
                return "dropDownList";
            case DocxContentControlKind.DatePicker:
                return "date";
            case DocxContentControlKind.Picture:
                return "picture";
            default:
                throw new ArgumentOutOfRangeException("kind");
        }
    }

    internal static string CreateBlockContentXml(List<DocxBlock> blocks)
    {
        if (blocks == null)
        {
            throw new ArgumentNullException("blocks");
        }

        DocxXmlWriter writer = DocxXmlWriter.Create();
        writer.StartElement("w", "sdtContent", DocxConstants.NamespaceWordprocessingMl);

        foreach (DocxBlock block in blocks)
        {
            if (block == null)
            {
                throw new ArgumentException("Blocks cannot contain null.", "blocks");
            }

            block.WriteTo(writer);
        }

        writer.EndElement();
        return writer.GetXml();
    }

    internal static string CreateRunContentXml(DocxRun run)
    {
        if (run == null)
        {
            throw new ArgumentNullException("pictureRun");
        }

        DocxXmlWriter writer = DocxXmlWriter.Create();
        writer.StartElement("w", "sdtContent", DocxConstants.NamespaceWordprocessingMl);
        run.WriteTo(writer);
        writer.EndElement();
        return writer.GetXml();
    }

    internal static string FindListDisplayText(List<DocxContentControlListItem> items, string selectedValue)
    {
        if (items == null)
        {
            throw new ArgumentNullException("items");
        }

        ValidateString(selectedValue, "selectedValue");

        foreach (DocxContentControlListItem item in items)
        {
            if (item != null && item.Value == selectedValue)
            {
                return item.DisplayText;
            }
        }

        throw new ArgumentException("Selected value is not present in the list items.", "selectedValue");
    }

    internal static XmlElement GetOrCreateNamespacedChild(XmlDocument document, XmlElement parent, string localName, string prefix, string namespaceUri)
    {
        XmlElement? element = FindFirstElement(parent, localName);

        if (element != null)
        {
            return element;
        }

        element = document.CreateElement(prefix, localName, namespaceUri);
        parent.AppendChild(element);
        return element;
    }

    internal static void SetNamespacedAttribute(XmlDocument document, XmlElement element, string localName, string prefix, string namespaceUri, string value)
    {
        XmlAttribute attribute = document.CreateAttribute(prefix, localName, namespaceUri);
        attribute.Value = value;
        element.Attributes.SetNamedItem(attribute);
    }
}
