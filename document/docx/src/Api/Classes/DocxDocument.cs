using System.IO;

namespace docx;

/// <summary>
/// Represents a WordprocessingML document together with the package that stores it.
/// </summary>
public class DocxDocument : IDocxTextContent, IDocxDocumentModel
{
    internal DocxPackage Package;
    internal DocxPart? MainDocumentPart;
    internal DocxPart? StylesPart;
    internal DocxPart? NumberingPart;
    internal DocxPart? FootnotesPart;
    internal DocxPart? EndnotesPart;
    internal DocxPart? CommentsPart;
    internal DocxPart? SettingsPart;
    internal DocxPart? WebSettingsPart;
    internal DocxPart? FontTablePart;
    internal DocxPart? ThemePart;
    internal DocxPart? CorePropertiesPart;
    internal DocxPart? ExtendedPropertiesPart;
    internal DocxPart? CustomPropertiesPart;
    internal DocxBody Body;
    internal string BodyModelXml;
    internal DocxStyles Styles;
    internal DocxNumbering Numbering;
    internal DocxFootnotes Footnotes;
    internal DocxEndnotes Endnotes;
    internal DocxComments Comments;
    internal DocxSettings Settings;
    internal DocxCoreProperties CoreProperties;
    internal DocxExtendedProperties ExtendedProperties;
    internal DocxCustomProperties CustomProperties;
    internal DocxCustomXmlParts CustomXmlParts;
    internal DocxMedia Media;
    internal List<DocxHeader> Headers;
    internal List<DocxFooter> Footers;
    internal bool HasMainDocumentRoot;
    internal bool HasStylesRoot;
    internal bool HasNumberingRoot;
    internal bool HasFootnotesRoot;
    internal bool HasEndnotesRoot;
    internal bool HasCommentsRoot;
    internal bool HasSettingsRoot;
    internal bool HasCorePropertiesRoot;
    internal bool HasExtendedPropertiesRoot;
    internal bool HasCustomPropertiesRoot;

    /// <summary>
    /// Creates a document model and parses each supported part that can be reached from the supplied package.
    /// </summary>
    /// <param name="package">The package to inspect or modify.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxDocument(DocxPackage package)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        this.Package = package;
        this.MainDocumentPart = null;
        this.StylesPart = null;
        this.NumberingPart = null;
        this.FootnotesPart = null;
        this.EndnotesPart = null;
        this.CommentsPart = null;
        this.SettingsPart = null;
        this.WebSettingsPart = null;
        this.FontTablePart = null;
        this.ThemePart = null;
        this.CorePropertiesPart = null;
        this.ExtendedPropertiesPart = null;
        this.CustomPropertiesPart = null;
        this.Body = DocxBody.Create();
        this.BodyModelXml = "";
        this.Styles = DocxStyles.Create();
        this.Numbering = DocxNumbering.Create();
        this.Footnotes = DocxFootnotes.Create();
        this.Endnotes = DocxEndnotes.Create();
        this.Comments = DocxComments.Create();
        this.Settings = DocxSettings.Create();
        this.CoreProperties = DocxCoreProperties.Create();
        this.ExtendedProperties = DocxExtendedProperties.Create();
        this.CustomProperties = DocxCustomProperties.Create();
        this.CustomXmlParts = new DocxCustomXmlParts();
        this.Media = new DocxMedia();
        this.Headers = new List<DocxHeader>();
        this.Footers = new List<DocxFooter>();
        this.HasMainDocumentRoot = false;
        this.HasStylesRoot = false;
        this.HasNumberingRoot = false;
        this.HasFootnotesRoot = false;
        this.HasEndnotesRoot = false;
        this.HasCommentsRoot = false;
        this.HasSettingsRoot = false;
        this.HasCorePropertiesRoot = false;
        this.HasExtendedPropertiesRoot = false;
        this.HasCustomPropertiesRoot = false;
        ParseMainDocument();
        ParseCoreProperties();
        ParseExtendedProperties();
        ParseCustomProperties();
        ParseCustomXmlParts();
        ParseSettings();
        ParseWebSettings();
        ParseFontTable();
        ParseTheme();
        ParseStyles();
        ParseNumbering();
        ParseFootnotes();
        ParseEndnotes();
        ParseComments();
        ParseHeaders();
        ParseFooters();
        ParseMedia();
    }

    internal static DocxDocument OpenPackage(DocxPackage package)
    {
        return new DocxDocument(package);
    }

    internal static DocxDocument Create()
    {
        return new DocxDocument(DocxPackage.Create());
    }

    internal static string CreateMinimalMainDocumentXml()
    {
        DocxXmlWriter writer = DocxXmlWriter.Create();
        WriteMinimalMainDocument(writer);
        return writer.GetXml();
    }

    internal static void WriteMinimalMainDocument(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("w", "document", DocxConstants.NamespaceWordprocessingMl);
        writer.StartElement("w", "body", DocxConstants.NamespaceWordprocessingMl);
        writer.EndElement();
        writer.EndElement();
    }

    internal bool ParseMainDocument()
    {
        this.MainDocumentPart = this.Package.GetMainDocumentPart();
        this.Body = DocxBody.Create();
        this.BodyModelXml = "";
        this.HasMainDocumentRoot = false;

        if (this.MainDocumentPart == null)
        {
            return false;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.MainDocumentPart.GetText());

        if (!reader.MoveToContent())
        {
            throw new InvalidDataException("Main document XML is empty.");
        }

        if (!reader.IsElement("document", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidDataException("Main document root element is missing.");
        }

        this.HasMainDocumentRoot = true;

        if (!reader.ReadToChildElement("body", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidDataException("Main document body element is missing.");
        }

        this.Body = DocxBody.Parse(reader);
        this.BodyModelXml = this.Body.WriteXml();
        return true;
    }

    /// <summary>
    /// Returns the main document body.
    /// </summary>
    /// <returns>The main document body.</returns>
    public DocxBody GetBody()
    {
        return this.Body;
    }

    internal bool HasBody()
    {
        return this.Body.HasXml();
    }

    /// <summary>
    /// Builds plain text from the current content in document order.
    /// </summary>
    /// <returns>The resulting text.</returns>
    public string GetText()
    {
        DocxTextOptions options = new DocxTextOptions();
        return GetTextWithOptions(options);
    }

    /// <summary>
    /// Builds plain text from the current content in document order.
    /// </summary>
    /// <param name="options">The options that control this operation.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public string GetTextWithOptions(DocxTextOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return this.Body.GetTextWithOptions(options);
    }

    /// <summary>
    /// Finds text occurrences in the document.
    /// </summary>
    /// <param name="searchText">The text to find.</param>
    /// <returns>The matching text ranges.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>searchText</c> is null.</exception>
    public List<DocxTextMatch> FindText(string searchText)
    {
        return DocxTextFinder.Find(this, searchText);
    }

    internal List<DocxTextMatch> FindTextWithOptions(string searchText, DocxTextOptions options)
    {
        return DocxTextFinder.Find(this, searchText, options);
    }

    internal int ReplacePlaceholders(Dictionary<string, string> values)
    {
        return DocxTemplate.ReplacePlaceholders(this, values);
    }

    internal int ReplacePlaceholdersWithDelimiters(Dictionary<string, string> values, string prefix, string suffix)
    {
        return DocxTemplate.ReplacePlaceholders(this, values, prefix, suffix);
    }

    internal int RemoveBlock(string key)
    {
        return DocxTemplate.RemoveBlock(this, key);
    }

    internal int RemoveBlockWithDelimiters(string key, string prefix, string suffix)
    {
        return DocxTemplate.RemoveBlock(this, key, prefix, suffix);
    }

    internal int InsertAtBookmark(string bookmarkName, DocxDocument source)
    {
        return DocxMerger.InsertAtBookmark(this, bookmarkName, source);
    }

    internal int Append(DocxDocument source)
    {
        return DocxMerger.Append(this, source);
    }

    internal bool ParseCoreProperties()
    {
        this.CorePropertiesPart = this.Package.GetCorePropertiesPart();
        this.CoreProperties = DocxCoreProperties.Create();
        this.HasCorePropertiesRoot = false;

        if (this.CorePropertiesPart == null)
        {
            return false;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.CorePropertiesPart.GetText());

        if (!reader.MoveToContent())
        {
            return false;
        }

        if (!DocxCoreProperties.IsSupported(reader))
        {
            throw new InvalidDataException("Core properties root element is missing.");
        }

        this.CoreProperties = DocxCoreProperties.Parse(reader);
        this.HasCorePropertiesRoot = true;
        return true;
    }

    /// <summary>
    /// Returns the document core properties.
    /// </summary>
    /// <returns>The document core properties.</returns>
    public DocxCoreProperties GetCoreProperties()
    {
        return this.CoreProperties;
    }

    internal bool HasCoreProperties()
    {
        return this.HasCorePropertiesRoot;
    }

    internal DocxRelationshipSet GetOrCreatePackageRelationshipSet()
    {
        DocxRelationshipSet? relationshipSet = this.Package.GetPackageRelationshipSet();

        if (relationshipSet != null)
        {
            return relationshipSet;
        }

        relationshipSet = new DocxRelationshipSet("/");
        this.Package.RelationshipSets.Add(relationshipSet);
        return relationshipSet;
    }

    internal DocxCoreProperties GetOrCreateCoreProperties()
    {
        if (this.CorePropertiesPart != null)
        {
            return this.CoreProperties;
        }

        string partName = "/docProps/core.xml";
        DocxRelationshipSet relationshipSet = GetOrCreatePackageRelationshipSet();
        relationshipSet.CreateRelationship(DocxConstants.RelationshipTypeCoreProperties, "docProps/core.xml", false);
        WriteRelationshipSetPart(relationshipSet);

        this.CoreProperties = DocxCoreProperties.Create();
        this.Package.AddXmlPartWithContent(partName, DocxConstants.ContentTypeCoreProperties, this.CoreProperties.WriteXml());
        this.CorePropertiesPart = this.Package.FindPart(partName);
        this.HasCorePropertiesRoot = true;
        return this.CoreProperties;
    }

    internal void WriteCorePropertiesPart()
    {
        if (this.CorePropertiesPart == null)
        {
            throw new InvalidOperationException("Core properties part is missing.");
        }

        string partName = this.CorePropertiesPart.PartName;
        string contentType = this.CorePropertiesPart.ContentType;

        if (this.Package.ContainsPart(partName))
        {
            ReplaceModeledXmlPart(partName, contentType, this.CoreProperties.WriteXml());
        }
        else
        {
            this.Package.AddXmlPartWithContent(partName, contentType, this.CoreProperties.WriteXml());
        }

        this.CorePropertiesPart = this.Package.FindPart(partName);
    }

    internal bool ParseExtendedProperties()
    {
        this.ExtendedPropertiesPart = this.Package.GetExtendedPropertiesPart();
        this.ExtendedProperties = DocxExtendedProperties.Create();
        this.HasExtendedPropertiesRoot = false;

        if (this.ExtendedPropertiesPart == null)
        {
            return false;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.ExtendedPropertiesPart.GetText());

        if (!reader.MoveToContent())
        {
            return false;
        }

        if (!DocxExtendedProperties.IsSupported(reader))
        {
            throw new InvalidDataException("Extended properties root element is missing.");
        }

        this.ExtendedProperties = DocxExtendedProperties.Parse(reader);
        this.HasExtendedPropertiesRoot = true;
        return true;
    }

    /// <summary>
    /// Returns the document extended properties.
    /// </summary>
    /// <returns>The document extended properties.</returns>
    public DocxExtendedProperties GetExtendedProperties()
    {
        return this.ExtendedProperties;
    }

    internal bool HasExtendedProperties()
    {
        return this.HasExtendedPropertiesRoot;
    }

    internal DocxExtendedProperties GetOrCreateExtendedProperties()
    {
        if (this.ExtendedPropertiesPart != null)
        {
            return this.ExtendedProperties;
        }

        string partName = "/docProps/app.xml";
        DocxRelationshipSet relationshipSet = GetOrCreatePackageRelationshipSet();
        relationshipSet.CreateRelationship(DocxConstants.RelationshipTypeExtendedProperties, "docProps/app.xml", false);
        WriteRelationshipSetPart(relationshipSet);

        this.ExtendedProperties = DocxExtendedProperties.Create();
        this.Package.AddXmlPartWithContent(partName, DocxConstants.ContentTypeExtendedProperties, this.ExtendedProperties.WriteXml());
        this.ExtendedPropertiesPart = this.Package.FindPart(partName);
        this.HasExtendedPropertiesRoot = true;
        return this.ExtendedProperties;
    }

    internal void WriteExtendedPropertiesPart()
    {
        if (this.ExtendedPropertiesPart == null)
        {
            throw new InvalidOperationException("Extended properties part is missing.");
        }

        string partName = this.ExtendedPropertiesPart.PartName;
        string contentType = this.ExtendedPropertiesPart.ContentType;

        if (this.Package.ContainsPart(partName))
        {
            ReplaceModeledXmlPart(partName, contentType, this.ExtendedProperties.WriteXml());
        }
        else
        {
            this.Package.AddXmlPartWithContent(partName, contentType, this.ExtendedProperties.WriteXml());
        }

        this.ExtendedPropertiesPart = this.Package.FindPart(partName);
    }

    internal bool ParseCustomProperties()
    {
        this.CustomPropertiesPart = this.Package.GetCustomPropertiesPart();
        this.CustomProperties = DocxCustomProperties.Create();
        this.HasCustomPropertiesRoot = false;

        if (this.CustomPropertiesPart == null)
        {
            return false;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.CustomPropertiesPart.GetText());

        if (!reader.MoveToContent())
        {
            return false;
        }

        if (!DocxCustomProperties.IsSupported(reader))
        {
            throw new InvalidDataException("Custom properties root element is missing.");
        }

        this.CustomProperties = DocxCustomProperties.Parse(reader);
        this.HasCustomPropertiesRoot = true;
        return true;
    }

    /// <summary>
    /// Returns the document custom properties.
    /// </summary>
    /// <returns>The document custom properties.</returns>
    public DocxCustomProperties GetCustomProperties()
    {
        return this.CustomProperties;
    }

    internal bool HasCustomProperties()
    {
        return this.HasCustomPropertiesRoot;
    }

    internal DocxCustomProperties GetOrCreateCustomProperties()
    {
        if (this.CustomPropertiesPart != null)
        {
            return this.CustomProperties;
        }

        string partName = "/docProps/custom.xml";
        DocxRelationshipSet relationshipSet = GetOrCreatePackageRelationshipSet();
        relationshipSet.CreateRelationship(DocxConstants.RelationshipTypeCustomProperties, "docProps/custom.xml", false);
        WriteRelationshipSetPart(relationshipSet);

        this.CustomProperties = DocxCustomProperties.Create();
        this.Package.AddXmlPartWithContent(partName, DocxConstants.ContentTypeCustomProperties, this.CustomProperties.WriteXml());
        this.CustomPropertiesPart = this.Package.FindPart(partName);
        this.HasCustomPropertiesRoot = true;
        return this.CustomProperties;
    }

    internal void WriteCustomPropertiesPart()
    {
        if (this.CustomPropertiesPart == null)
        {
            throw new InvalidOperationException("Custom properties part is missing.");
        }

        string partName = this.CustomPropertiesPart.PartName;
        string contentType = this.CustomPropertiesPart.ContentType;

        if (this.Package.ContainsPart(partName))
        {
            ReplaceModeledXmlPart(partName, contentType, this.CustomProperties.WriteXml());
        }
        else
        {
            this.Package.AddXmlPartWithContent(partName, contentType, this.CustomProperties.WriteXml());
        }

        this.CustomPropertiesPart = this.Package.FindPart(partName);
    }

    internal int ParseCustomXmlParts()
    {
        this.CustomXmlParts = DocxCustomXmlParts.Discover(this.Package);
        return this.CustomXmlParts.Count();
    }

    /// <summary>
    /// Returns custom XML parts discovered through package relationships.
    /// </summary>
    /// <returns>The custom XML parts.</returns>
    public DocxCustomXmlParts GetCustomXmlParts()
    {
        return this.CustomXmlParts;
    }

    internal int CustomXmlPartCount()
    {
        return this.CustomXmlParts.Count();
    }

    internal DocxCustomXmlPart GetCustomXmlPart(int index)
    {
        return this.CustomXmlParts.GetPart(index);
    }

    internal bool ParseSettings()
    {
        this.SettingsPart = this.Package.GetSettingsPart();
        this.Settings = DocxSettings.Create();
        this.HasSettingsRoot = false;

        if (this.SettingsPart == null)
        {
            return false;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.SettingsPart.GetText());

        if (!reader.MoveToContent())
        {
            return false;
        }

        if (!DocxSettings.IsSupported(reader))
        {
            throw new InvalidDataException("Settings root element is missing.");
        }

        this.Settings = DocxSettings.Parse(reader);
        this.HasSettingsRoot = true;
        return true;
    }

    /// <summary>
    /// Returns the document settings.
    /// </summary>
    /// <returns>The document settings.</returns>
    public DocxSettings GetSettings()
    {
        return this.Settings;
    }

    internal bool HasSettings()
    {
        return this.HasSettingsRoot;
    }

    internal DocxSettings GetOrCreateSettings()
    {
        if (this.SettingsPart != null)
        {
            return this.Settings;
        }

        string partName = "/word/settings.xml";
        DocxRelationshipSet relationshipSet = GetOrCreateMainDocumentRelationshipSet();
        string target = GetMainDocumentRelativeTarget(partName);
        relationshipSet.CreateRelationship(DocxConstants.RelationshipTypeSettings, target, false);
        WriteRelationshipSetPart(relationshipSet);

        this.Settings = DocxSettings.Create();
        this.Package.AddXmlPartWithContent(partName, DocxConstants.ContentTypeSettings, this.Settings.WriteXml());
        this.SettingsPart = this.Package.FindPart(partName);
        this.HasSettingsRoot = true;
        return this.Settings;
    }

    internal void WriteSettingsPart()
    {
        if (this.SettingsPart == null)
        {
            throw new InvalidOperationException("Settings part is missing.");
        }

        string partName = this.SettingsPart.PartName;
        string contentType = this.SettingsPart.ContentType;

        if (this.Package.ContainsPart(partName))
        {
            ReplaceModeledXmlPart(partName, contentType, this.Settings.WriteXml());
        }
        else
        {
            this.Package.AddXmlPartWithContent(partName, contentType, this.Settings.WriteXml());
        }

        this.SettingsPart = this.Package.FindPart(partName);
    }

    internal bool ParseWebSettings()
    {
        this.WebSettingsPart = this.Package.GetWebSettingsPart();
        return this.WebSettingsPart != null;
    }

    /// <summary>
    /// Returns the web settings package part, or null when it is absent.
    /// </summary>
    /// <returns>The web settings part, or null.</returns>
    public DocxPart? GetWebSettingsPart()
    {
        return this.WebSettingsPart;
    }

    internal bool HasWebSettingsPart()
    {
        return this.WebSettingsPart != null;
    }

    internal bool ParseFontTable()
    {
        this.FontTablePart = this.Package.GetFontTablePart();
        return this.FontTablePart != null;
    }

    /// <summary>
    /// Returns the font table package part, or null when it is absent.
    /// </summary>
    /// <returns>The font table part, or null.</returns>
    public DocxPart? GetFontTablePart()
    {
        return this.FontTablePart;
    }

    internal bool HasFontTablePart()
    {
        return this.FontTablePart != null;
    }

    internal bool ParseTheme()
    {
        this.ThemePart = this.Package.GetThemePart();
        return this.ThemePart != null;
    }

    /// <summary>
    /// Returns the theme package part, or null when it is absent.
    /// </summary>
    /// <returns>The theme part, or null.</returns>
    public DocxPart? GetThemePart()
    {
        return this.ThemePart;
    }

    internal bool HasThemePart()
    {
        return this.ThemePart != null;
    }

    internal int AcceptRevisions()
    {
        if (this.MainDocumentPart == null)
        {
            return 0;
        }

        string xml = this.MainDocumentPart.GetText();
        int count = DocxRevision.CountRevisionElementsXml(xml);

        if (count == 0)
        {
            return 0;
        }

        string partName = this.MainDocumentPart.PartName;
        string contentType = this.MainDocumentPart.ContentType;
        string acceptedXml = DocxRevision.AcceptRevisionXml(xml);
        this.Package.ReplaceXmlPart(partName, contentType, acceptedXml);
        this.MainDocumentPart = this.Package.FindPart(partName);
        ParseMainDocument();
        return count;
    }

    internal int RejectRevisions()
    {
        if (this.MainDocumentPart == null)
        {
            return 0;
        }

        string xml = this.MainDocumentPart.GetText();
        int count = DocxRevision.CountRevisionElementsXml(xml);

        if (count == 0)
        {
            return 0;
        }

        string partName = this.MainDocumentPart.PartName;
        string contentType = this.MainDocumentPart.ContentType;
        string rejectedXml = DocxRevision.RejectRevisionXml(xml);
        this.Package.ReplaceXmlPart(partName, contentType, rejectedXml);
        this.MainDocumentPart = this.Package.FindPart(partName);
        ParseMainDocument();
        return count;
    }

    /// <summary>
    /// Returns the sections in document order.
    /// </summary>
    /// <returns>The document sections.</returns>
    public List<DocxSection> GetSections()
    {
        return this.Body.GetSections();
    }

    internal int SectionCount()
    {
        return this.Body.SectionCount();
    }

    internal int ParseMedia()
    {
        this.Media = DocxMedia.Discover(this.Package);
        return this.Media.ImageCount();
    }

    /// <summary>
    /// Returns the media discovered in the document package.
    /// </summary>
    /// <returns>The document media.</returns>
    public DocxMedia GetMedia()
    {
        return this.Media;
    }

    /// <summary>
    /// Returns every supported text story discovered through package relationships.
    /// </summary>
    /// <returns>A new list containing the discovered stories.</returns>
    public List<DocxStory> GetStories()
    {
        List<DocxStory> stories = new List<DocxStory>();
        DocxRelationshipSet? packageRelationships = this.Package.GetPackageRelationshipSet();
        DocxRelationship? mainDocumentRelationship = this.Package.FindMainDocumentRelationship();

        if (packageRelationships != null && mainDocumentRelationship != null && this.MainDocumentPart != null)
        {
            stories.Add(new DocxStory(
                DocxStoryType.MainDocument,
                "/",
                this.MainDocumentPart.PartName,
                mainDocumentRelationship.Id,
                this.Body));
        }

        foreach (DocxRelationshipSet relationshipSet in this.Package.RelationshipSets)
        {
            foreach (DocxRelationship relationship in relationshipSet.Relationships)
            {
                AddRelationshipStory(stories, relationshipSet, relationship);
            }
        }

        return stories;
    }

    /// <summary>
    /// Returns content controls discovered throughout the parsed body, headers, footers, notes, and comments.
    /// </summary>
    /// <returns>A new list containing the content controls in story and document order.</returns>
    public List<DocxContentControl> GetContentControls()
    {
        List<DocxContentControl> contentControls = new List<DocxContentControl>();
        AddContentControlsFromBlocks(contentControls, this.Body.Blocks);

        foreach (DocxHeader header in this.Headers)
        {
            AddContentControlsFromBlocks(contentControls, header.Blocks);
        }

        foreach (DocxFooter footer in this.Footers)
        {
            AddContentControlsFromBlocks(contentControls, footer.Blocks);
        }

        foreach (DocxFootnote footnote in this.Footnotes.Footnotes)
        {
            AddContentControlsFromBlocks(contentControls, footnote.Blocks);
        }

        foreach (DocxEndnote endnote in this.Endnotes.Endnotes)
        {
            AddContentControlsFromBlocks(contentControls, endnote.Blocks);
        }

        foreach (DocxComment comment in this.Comments.Comments)
        {
            AddContentControlsFromBlocks(contentControls, comment.Blocks);
        }

        return contentControls;
    }

    internal void AddContentControlsFromBlocks(
        List<DocxContentControl> contentControls,
        List<DocxBlock> blocks)
    {
        foreach (DocxBlock block in blocks)
        {
            AddContentControlsFromBlock(contentControls, block);
        }
    }

    internal void AddContentControlsFromBlock(
        List<DocxContentControl> contentControls,
        DocxBlock block)
    {
        if (block.ContentControl != null)
        {
            AddContentControlTree(contentControls, block.ContentControl);
            return;
        }

        if (block.Paragraph != null)
        {
            AddContentControlsFromParagraph(contentControls, block.Paragraph);
            return;
        }

        if (block.Table != null)
        {
            AddContentControlsFromTable(contentControls, block.Table);
        }
    }

    internal void AddContentControlsFromParagraph(
        List<DocxContentControl> contentControls,
        DocxParagraph paragraph)
    {
        foreach (DocxContentControl contentControl in paragraph.ContentControls)
        {
            AddContentControlTree(contentControls, contentControl);
        }
    }

    internal void AddContentControlTree(
        List<DocxContentControl> contentControls,
        DocxContentControl contentControl)
    {
        contentControls.Add(contentControl);
        AddContentControlsFromBlocks(contentControls, contentControl.Blocks);
    }

    internal void AddContentControlsFromTable(
        List<DocxContentControl> contentControls,
        DocxTable table)
    {
        foreach (DocxTableRow row in table.Rows)
        {
            foreach (DocxTableCell cell in row.Cells)
            {
                AddContentControlsFromCell(contentControls, cell);
            }
        }
    }

    internal void AddContentControlsFromCell(
        List<DocxContentControl> contentControls,
        DocxTableCell cell)
    {
        foreach (DocxParagraph paragraph in cell.Paragraphs)
        {
            AddContentControlsFromParagraph(contentControls, paragraph);
        }

        foreach (DocxTable table in cell.Tables)
        {
            AddContentControlsFromTable(contentControls, table);
        }
    }

    internal void AddRelationshipStory(
        List<DocxStory> stories,
        DocxRelationshipSet relationshipSet,
        DocxRelationship relationship)
    {
        if (relationship.IsExternal)
        {
            return;
        }

        bool isHeader = relationship.Type == DocxConstants.RelationshipTypeHeader;
        bool isFooter = relationship.Type == DocxConstants.RelationshipTypeFooter;
        bool isFootnotes = relationship.Type == DocxConstants.RelationshipTypeFootnotes;
        bool isEndnotes = relationship.Type == DocxConstants.RelationshipTypeEndnotes;
        bool isComments = relationship.Type == DocxConstants.RelationshipTypeComments;

        if (!isHeader && !isFooter && !isFootnotes && !isEndnotes && !isComments)
        {
            return;
        }

        string partName = relationshipSet.ResolveTargetPartName(relationship);
        DocxPart? part = this.Package.FindPart(partName);

        if (part == null)
        {
            return;
        }

        IDocxTextContent? content = null;
        DocxStoryType storyType = DocxStoryType.MainDocument;

        if (isHeader)
        {
            storyType = DocxStoryType.Header;
            content = FindHeaderByPartName(partName);

            if (content == null)
            {
                DocxHeader header = new DocxHeader(part.GetText());
                header.SetPartInfo(partName, relationship.Id);
                content = header;
            }
        }
        else if (isFooter)
        {
            storyType = DocxStoryType.Footer;
            content = FindFooterByPartName(partName);

            if (content == null)
            {
                DocxFooter footer = new DocxFooter(part.GetText());
                footer.SetPartInfo(partName, relationship.Id);
                content = footer;
            }
        }
        else if (isFootnotes)
        {
            storyType = DocxStoryType.Footnotes;
            content = CreateFootnotesStoryContent(part);
        }
        else if (isEndnotes)
        {
            storyType = DocxStoryType.Endnotes;
            content = CreateEndnotesStoryContent(part);
        }
        else if (isComments)
        {
            storyType = DocxStoryType.Comments;
            content = CreateCommentsStoryContent(part);
        }

        if (content == null)
        {
            return;
        }

        stories.Add(new DocxStory(
            storyType,
            relationshipSet.SourcePartName,
            partName,
            relationship.Id,
            content));
    }

    internal DocxHeader? FindHeaderByPartName(string partName)
    {
        foreach (DocxHeader header in this.Headers)
        {
            if (header.PartName == partName)
            {
                return header;
            }
        }

        return null;
    }

    internal DocxFooter? FindFooterByPartName(string partName)
    {
        foreach (DocxFooter footer in this.Footers)
        {
            if (footer.PartName == partName)
            {
                return footer;
            }
        }

        return null;
    }

    internal IDocxTextContent CreateFootnotesStoryContent(DocxPart part)
    {
        if (this.FootnotesPart != null && this.FootnotesPart.PartName == part.PartName)
        {
            return this.Footnotes;
        }

        return new DocxFootnotes(part.GetText());
    }

    internal IDocxTextContent CreateEndnotesStoryContent(DocxPart part)
    {
        if (this.EndnotesPart != null && this.EndnotesPart.PartName == part.PartName)
        {
            return this.Endnotes;
        }

        return new DocxEndnotes(part.GetText());
    }

    internal IDocxTextContent CreateCommentsStoryContent(DocxPart part)
    {
        if (this.CommentsPart != null && this.CommentsPart.PartName == part.PartName)
        {
            return this.Comments;
        }

        return new DocxComments(part.GetText());
    }

    internal int ImageCount()
    {
        return this.Media.ImageCount();
    }

    internal DocxImage GetImage(int index)
    {
        return this.Media.GetImage(index);
    }

    internal DocxImage? FindImage(string sourcePartName, string relationshipId)
    {
        return this.Media.FindImage(sourcePartName, relationshipId);
    }

    internal DocxImage AddImage(byte[] bytes, string contentType)
    {
        return this.Media.AddImage(bytes, contentType);
    }

    internal DocxImage AddImageWithRelationship(byte[] bytes, string contentType, string sourcePartName)
    {
        return this.Media.AddImageWithRelationship(bytes, contentType, sourcePartName);
    }

    internal DocxImage AddImageToMainDocument(byte[] bytes, string contentType)
    {
        string mainDocumentPartName = this.Package.GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            throw new InvalidOperationException("Main document part is missing.");
        }

        return AddImageWithRelationship(bytes, contentType, mainDocumentPartName);
    }

    internal DocxImage AddImageFromStream(Stream stream, string contentType)
    {
        return this.Media.AddImageFromStream(stream, contentType);
    }

    internal DocxRelationship CreateImageRelationship(DocxImage image, string sourcePartName)
    {
        return this.Media.CreateImageRelationship(image, sourcePartName);
    }

    internal DocxMediaImportMap ImportMedia(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        return ImportMediaCollection(document.Media);
    }

    internal DocxMediaImportMap ImportMediaToPart(DocxDocument document, string targetSourcePartName)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (targetSourcePartName == null)
        {
            throw new ArgumentNullException("targetSourcePartName");
        }

        return this.Media.ImportImages(document.Media, targetSourcePartName);
    }

    internal DocxMediaImportMap ImportMediaCollection(DocxMedia media)
    {
        if (media == null)
        {
            throw new ArgumentNullException("media");
        }

        string mainDocumentPartName = this.Package.GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            throw new InvalidOperationException("Main document part is missing.");
        }

        return ImportMediaCollectionToPart(media, mainDocumentPartName);
    }

    internal DocxMediaImportMap ImportMediaCollectionToPart(DocxMedia media, string targetSourcePartName)
    {
        if (media == null)
        {
            throw new ArgumentNullException("media");
        }

        if (targetSourcePartName == null)
        {
            throw new ArgumentNullException("targetSourcePartName");
        }

        return this.Media.ImportImages(media, targetSourcePartName);
    }

    internal bool ReplaceImage(DocxImage image, byte[] bytes, string contentType)
    {
        return this.Media.ReplaceImage(image, bytes, contentType);
    }

    internal bool ReplaceImageBytes(DocxImage image, byte[] bytes)
    {
        return this.Media.ReplaceImageBytes(image, bytes);
    }

    internal int ParseHeaders()
    {
        this.Headers = new List<DocxHeader>();

        foreach (DocxRelationshipSet relationshipSet in this.Package.RelationshipSets)
        {
            foreach (DocxRelationship relationship in relationshipSet.Relationships)
            {
                if (relationship.Type == DocxConstants.RelationshipTypeHeader && !relationship.IsExternal)
                {
                    AddHeaderFromRelationship(relationshipSet, relationship);
                }
            }
        }

        return this.Headers.Count;
    }

    /// <summary>
    /// Returns all headers discovered through package relationships.
    /// </summary>
    /// <returns>A new list containing the document headers.</returns>
    public List<DocxHeader> GetHeaders()
    {
        List<DocxHeader> headers = new List<DocxHeader>();

        foreach (DocxHeader header in this.Headers)
        {
            headers.Add(header);
        }

        return headers;
    }

    internal bool AddHeaderFromRelationship(DocxRelationshipSet relationshipSet, DocxRelationship relationship)
    {
        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        string partName = relationshipSet.ResolveTargetPartName(relationship);
        DocxPart? part = this.Package.FindPart(partName);

        if (part == null)
        {
            return false;
        }

        DocxHeader header = new DocxHeader(part.GetText());
        header.SetPartInfo(partName, relationship.Id);
        this.Headers.Add(header);
        return true;
    }

    internal int HeaderCount()
    {
        return this.Headers.Count;
    }

    internal DocxHeader GetHeader(int index)
    {
        return this.Headers[index];
    }

    internal DocxHeader CreateHeader()
    {
        string partName = AllocateHeaderPartName();
        DocxRelationshipSet relationshipSet = GetOrCreateMainDocumentRelationshipSet();
        string target = GetMainDocumentRelativeTarget(partName);
        DocxRelationship relationship = relationshipSet.CreateRelationship(DocxConstants.RelationshipTypeHeader, target, false);

        this.Package.AddXmlPartWithContent(partName, DocxConstants.ContentTypeHeader, DocxHeader.CreateMinimalXml());
        WriteRelationshipSetPart(relationshipSet);

        DocxHeader header = DocxHeader.Create();
        header.SetPartInfo(partName, relationship.Id);
        this.Headers.Add(header);
        return header;
    }

    internal DocxHeader CreateHeaderForSection(DocxSection section, string referenceType)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        DocxHeader header = CreateHeader();
        section.Format.SetHeaderReference(referenceType, header.RelationshipId);
        section.Xml = "";
        return header;
    }

    internal DocxHeader? FindHeaderByRelationshipId(string relationshipId)
    {
        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        if (relationshipId == "")
        {
            return null;
        }

        foreach (DocxHeader header in this.Headers)
        {
            if (header.RelationshipId == relationshipId)
            {
                return header;
            }
        }

        return null;
    }

    internal DocxHeader? FindDefaultHeader(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        return FindHeaderByRelationshipId(section.Format.DefaultHeaderRelationshipId);
    }

    internal DocxHeader? FindEvenHeader(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        return FindHeaderByRelationshipId(section.Format.EvenHeaderRelationshipId);
    }

    internal DocxHeader? FindFirstHeader(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        return FindHeaderByRelationshipId(section.Format.FirstHeaderRelationshipId);
    }

    internal DocxHeader? FindEffectiveDefaultHeader(DocxSection section)
    {
        return FindEffectiveHeader(section, DocxSectionFormat.HeaderFooterReferenceDefault);
    }

    internal DocxHeader? FindEffectiveEvenHeader(DocxSection section)
    {
        return FindEffectiveHeader(section, DocxSectionFormat.HeaderFooterReferenceEven);
    }

    internal DocxHeader? FindEffectiveFirstHeader(DocxSection section)
    {
        return FindEffectiveHeader(section, DocxSectionFormat.HeaderFooterReferenceFirst);
    }

    internal DocxHeader? FindEffectiveHeader(DocxSection section, string referenceType)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        DocxSection? currentSection = section;

        while (currentSection != null)
        {
            string relationshipId = currentSection.Format.GetHeaderReference(referenceType);

            if (relationshipId != "")
            {
                return FindHeaderByRelationshipId(relationshipId);
            }

            currentSection = FindPreviousSection(currentSection);
        }

        return null;
    }

    internal bool IsHeaderLinkedToPrevious(DocxSection section, string referenceType)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        return FindPreviousSection(section) != null
            && section.Format.IsHeaderLinkedToPrevious(referenceType);
    }

    internal void LinkHeaderToPrevious(DocxSection section, string referenceType)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        section.Format.ClearHeaderReference(referenceType);
        section.Xml = "";
    }

    internal int ParseFooters()
    {
        this.Footers = new List<DocxFooter>();

        foreach (DocxRelationshipSet relationshipSet in this.Package.RelationshipSets)
        {
            foreach (DocxRelationship relationship in relationshipSet.Relationships)
            {
                if (relationship.Type == DocxConstants.RelationshipTypeFooter && !relationship.IsExternal)
                {
                    AddFooterFromRelationship(relationshipSet, relationship);
                }
            }
        }

        return this.Footers.Count;
    }

    /// <summary>
    /// Returns all footers discovered through package relationships.
    /// </summary>
    /// <returns>A new list containing the document footers.</returns>
    public List<DocxFooter> GetFooters()
    {
        List<DocxFooter> footers = new List<DocxFooter>();

        foreach (DocxFooter footer in this.Footers)
        {
            footers.Add(footer);
        }

        return footers;
    }

    internal bool AddFooterFromRelationship(DocxRelationshipSet relationshipSet, DocxRelationship relationship)
    {
        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        string partName = relationshipSet.ResolveTargetPartName(relationship);
        DocxPart? part = this.Package.FindPart(partName);

        if (part == null)
        {
            return false;
        }

        DocxFooter footer = new DocxFooter(part.GetText());
        footer.SetPartInfo(partName, relationship.Id);
        this.Footers.Add(footer);
        return true;
    }

    internal int FooterCount()
    {
        return this.Footers.Count;
    }

    internal DocxFooter GetFooter(int index)
    {
        return this.Footers[index];
    }

    internal void UpdateHeaderFooterParts()
    {
        foreach (DocxHeader header in this.Headers)
        {
            if (header.PartName != "" && this.Package.ContainsPart(header.PartName))
            {
                ReplaceModeledXmlPart(header.PartName, DocxConstants.ContentTypeHeader, header.WriteXml());
            }
        }

        foreach (DocxFooter footer in this.Footers)
        {
            if (footer.PartName != "" && this.Package.ContainsPart(footer.PartName))
            {
                ReplaceModeledXmlPart(footer.PartName, DocxConstants.ContentTypeFooter, footer.WriteXml());
            }
        }
    }

    internal void ReplaceModeledXmlPart(string partName, string contentType, string xml)
    {
        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxPart? part = this.Package.FindPart(partName);

        if (part != null)
        {
            xml = DocxXmlPreservation.PreserveRootMetadata(part.GetText(), xml);
        }

        this.Package.ReplaceXmlPart(partName, contentType, xml);
    }

    internal DocxFooter CreateFooter()
    {
        string partName = AllocateFooterPartName();
        DocxRelationshipSet relationshipSet = GetOrCreateMainDocumentRelationshipSet();
        string target = GetMainDocumentRelativeTarget(partName);
        DocxRelationship relationship = relationshipSet.CreateRelationship(DocxConstants.RelationshipTypeFooter, target, false);

        this.Package.AddXmlPartWithContent(partName, DocxConstants.ContentTypeFooter, DocxFooter.CreateMinimalXml());
        WriteRelationshipSetPart(relationshipSet);

        DocxFooter footer = DocxFooter.Create();
        footer.SetPartInfo(partName, relationship.Id);
        this.Footers.Add(footer);
        return footer;
    }

    internal DocxFooter CreateFooterForSection(DocxSection section, string referenceType)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        DocxFooter footer = CreateFooter();
        section.Format.SetFooterReference(referenceType, footer.RelationshipId);
        section.Xml = "";
        return footer;
    }

    internal DocxFooter? FindFooterByRelationshipId(string relationshipId)
    {
        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        if (relationshipId == "")
        {
            return null;
        }

        foreach (DocxFooter footer in this.Footers)
        {
            if (footer.RelationshipId == relationshipId)
            {
                return footer;
            }
        }

        return null;
    }

    internal DocxFooter? FindDefaultFooter(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        return FindFooterByRelationshipId(section.Format.DefaultFooterRelationshipId);
    }

    internal DocxFooter? FindEvenFooter(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        return FindFooterByRelationshipId(section.Format.EvenFooterRelationshipId);
    }

    internal DocxFooter? FindFirstFooter(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        return FindFooterByRelationshipId(section.Format.FirstFooterRelationshipId);
    }

    internal DocxFooter? FindEffectiveDefaultFooter(DocxSection section)
    {
        return FindEffectiveFooter(section, DocxSectionFormat.HeaderFooterReferenceDefault);
    }

    internal DocxFooter? FindEffectiveEvenFooter(DocxSection section)
    {
        return FindEffectiveFooter(section, DocxSectionFormat.HeaderFooterReferenceEven);
    }

    internal DocxFooter? FindEffectiveFirstFooter(DocxSection section)
    {
        return FindEffectiveFooter(section, DocxSectionFormat.HeaderFooterReferenceFirst);
    }

    internal DocxFooter? FindEffectiveFooter(DocxSection section, string referenceType)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        DocxSection? currentSection = section;

        while (currentSection != null)
        {
            string relationshipId = currentSection.Format.GetFooterReference(referenceType);

            if (relationshipId != "")
            {
                return FindFooterByRelationshipId(relationshipId);
            }

            currentSection = FindPreviousSection(currentSection);
        }

        return null;
    }

    internal bool IsFooterLinkedToPrevious(DocxSection section, string referenceType)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        return FindPreviousSection(section) != null
            && section.Format.IsFooterLinkedToPrevious(referenceType);
    }

    internal void LinkFooterToPrevious(DocxSection section, string referenceType)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        section.Format.ClearFooterReference(referenceType);
        section.Xml = "";
    }

    internal DocxSection? FindPreviousSection(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        for (int index = 0; index < this.Body.Sections.Count; index = index + 1)
        {
            if (this.Body.Sections[index] == section)
            {
                if (index == 0)
                {
                    return null;
                }

                return this.Body.Sections[index - 1];
            }
        }

        return null;
    }

    internal void MarkSectionChanged(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        for (int index = 0; index < this.Body.Sections.Count; index = index + 1)
        {
            if (this.Body.Sections[index] == section)
            {
                this.Body.ClearSectionOwnerProperties(index);
                return;
            }
        }
    }

    internal DocxRelationshipSet GetOrCreateMainDocumentRelationshipSet()
    {
        string mainDocumentPartName = this.Package.GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            throw new InvalidOperationException("Main document part is missing.");
        }

        DocxRelationshipSet? relationshipSet = this.Package.FindRelationshipSet(mainDocumentPartName);

        if (relationshipSet != null)
        {
            return relationshipSet;
        }

        relationshipSet = new DocxRelationshipSet(mainDocumentPartName);
        this.Package.RelationshipSets.Add(relationshipSet);
        return relationshipSet;
    }

    internal string AllocateHeaderPartName()
    {
        return AllocateStoryPartName("header");
    }

    internal string AllocateFooterPartName()
    {
        return AllocateStoryPartName("footer");
    }

    internal string AllocateStoryPartName(string baseName)
    {
        if (baseName == null)
        {
            throw new ArgumentNullException("baseName");
        }

        if (baseName == "")
        {
            throw new ArgumentException("Part base name is required.", "baseName");
        }

        string mainDocumentPartName = this.Package.GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            throw new InvalidOperationException("Main document part is missing.");
        }

        string baseDirectory = DocxRelationshipSet.GetSourceBaseDirectory(mainDocumentPartName);
        int index = 1;

        while (true)
        {
            string partName = baseDirectory + baseName + index.ToString() + ".xml";

            if (!this.Package.ContainsPart(partName))
            {
                return partName;
            }

            index = index + 1;
        }
    }

    internal string GetMainDocumentRelativeTarget(string partName)
    {
        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        string mainDocumentPartName = this.Package.GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            throw new InvalidOperationException("Main document part is missing.");
        }

        string baseDirectory = DocxRelationshipSet.GetSourceBaseDirectory(mainDocumentPartName);
        string normalizedPartName = DocxPartName.Normalize(partName);

        if (!normalizedPartName.StartsWith(baseDirectory))
        {
            return normalizedPartName.Substring(1);
        }

        return normalizedPartName.Substring(baseDirectory.Length);
    }

    internal void WriteRelationshipSetPart(DocxRelationshipSet relationshipSet)
    {
        if (relationshipSet == null)
        {
            throw new ArgumentNullException("relationshipSet");
        }

        string relationshipPartName = "";

        if (relationshipSet.SourcePartName == "/")
        {
            relationshipPartName = "/_rels/.rels";
        }
        else
        {
            relationshipPartName = DocxRelationshipSet.GetRelationshipPartName(relationshipSet.SourcePartName);
        }

        string xml = relationshipSet.WriteXml();

        if (this.Package.ContainsPart(relationshipPartName))
        {
            DocxPart? part = this.Package.FindPart(relationshipPartName);

            if (part != null)
            {
                xml = DocxXmlPreservation.PreserveRelationshipContent(part.GetText(), xml);
            }

            this.Package.ReplaceXmlPart(relationshipPartName, DocxConstants.ContentTypeRelationships, xml);
        }
        else
        {
            this.Package.AddXmlPartWithContent(relationshipPartName, DocxConstants.ContentTypeRelationships, xml);
        }
    }

    internal bool ParseStyles()
    {
        this.StylesPart = this.Package.GetStylesPart();
        this.Styles = DocxStyles.Create();
        this.HasStylesRoot = false;

        if (this.StylesPart == null)
        {
            return false;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.StylesPart.GetText());

        if (!reader.MoveToContent())
        {
            throw new InvalidDataException("Styles XML is empty.");
        }

        if (!reader.IsElement("styles", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidDataException("Styles root element is missing.");
        }

        this.HasStylesRoot = true;
        this.Styles = DocxStyles.Parse(reader);
        return true;
    }

    /// <summary>
    /// Returns the document styles.
    /// </summary>
    /// <returns>The document styles.</returns>
    public DocxStyles GetStyles()
    {
        return this.Styles;
    }

    internal void WriteStylesPart()
    {
        if (this.StylesPart == null)
        {
            string partName = this.Package.GetStylesPartName();

            if (partName == "")
            {
                partName = AllocateNotesPartName("styles");
                DocxRelationshipSet relationshipSet = GetOrCreateMainDocumentRelationshipSet();
                string target = GetMainDocumentRelativeTarget(partName);
                relationshipSet.CreateRelationship(DocxConstants.RelationshipTypeStyles, target, false);
                WriteRelationshipSetPart(relationshipSet);
            }

            this.Package.AddXmlPartWithContent(partName, DocxConstants.ContentTypeStyles, this.Styles.WriteXml());
            this.StylesPart = this.Package.FindPart(partName);
            this.HasStylesRoot = true;
            return;
        }

        ReplaceModeledXmlPart(this.StylesPart.PartName, DocxConstants.ContentTypeStyles, this.Styles.WriteXml());
        this.StylesPart = this.Package.FindPart(this.StylesPart.PartName);
        this.HasStylesRoot = true;
    }

    internal bool HasStyles()
    {
        return this.Styles.HasXml();
    }

    internal bool ParseNumbering()
    {
        this.NumberingPart = this.Package.GetNumberingPart();
        this.Numbering = DocxNumbering.Create();
        this.HasNumberingRoot = false;

        if (this.NumberingPart == null)
        {
            return false;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.NumberingPart.GetText());

        if (!reader.MoveToContent())
        {
            throw new InvalidDataException("Numbering XML is empty.");
        }

        if (!reader.IsElement("numbering", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidDataException("Numbering root element is missing.");
        }

        this.HasNumberingRoot = true;
        this.Numbering = DocxNumbering.Parse(reader);
        return true;
    }

    /// <summary>
    /// Returns the document numbering definitions.
    /// </summary>
    /// <returns>The document numbering definitions.</returns>
    public DocxNumbering GetNumbering()
    {
        return this.Numbering;
    }

    internal bool HasNumbering()
    {
        return this.Numbering.HasXml();
    }

    internal DocxNumbering GetOrCreateNumbering()
    {
        if (this.NumberingPart != null)
        {
            return this.Numbering;
        }

        return CreateNumberingPart();
    }

    internal DocxNumbering CreateNumberingPart()
    {
        if (this.NumberingPart != null)
        {
            return this.Numbering;
        }

        string partName = this.Package.GetNumberingPartName();

        if (partName == "")
        {
            partName = AllocateNotesPartName("numbering");
            DocxRelationshipSet relationshipSet = GetOrCreateMainDocumentRelationshipSet();
            string target = GetMainDocumentRelativeTarget(partName);
            relationshipSet.CreateRelationship(DocxConstants.RelationshipTypeNumbering, target, false);
            WriteRelationshipSetPart(relationshipSet);
        }

        this.Numbering = DocxNumbering.Create();
        this.Package.AddXmlPartWithContent(partName, DocxConstants.ContentTypeNumbering, this.Numbering.WriteXml());
        this.NumberingPart = this.Package.FindPart(partName);
        this.HasNumberingRoot = true;
        return this.Numbering;
    }

    internal void WriteNumberingPart()
    {
        if (this.NumberingPart == null)
        {
            throw new InvalidOperationException("Numbering part is missing.");
        }

        if (this.Package.ContainsPart(this.NumberingPart.PartName))
        {
            ReplaceModeledXmlPart(this.NumberingPart.PartName, DocxConstants.ContentTypeNumbering, this.Numbering.WriteXml());
        }
        else
        {
            this.Package.AddXmlPartWithContent(this.NumberingPart.PartName, DocxConstants.ContentTypeNumbering, this.Numbering.WriteXml());
        }

        this.NumberingPart = this.Package.FindPart(this.NumberingPart.PartName);
    }

    internal DocxNumberingImportMap ImportNumbering(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        return this.Numbering.ImportNumbering(document.Numbering);
    }

    internal DocxNumberingImportMap ImportNumberingDefinitions(DocxNumbering numbering)
    {
        if (numbering == null)
        {
            throw new ArgumentNullException("numbering");
        }

        return this.Numbering.ImportNumbering(numbering);
    }

    internal bool ParseFootnotes()
    {
        this.FootnotesPart = this.Package.GetFootnotesPart();
        this.Footnotes = DocxFootnotes.Create();
        this.HasFootnotesRoot = false;

        if (this.FootnotesPart == null)
        {
            return false;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.FootnotesPart.GetText());

        if (!reader.MoveToContent())
        {
            throw new InvalidDataException("Footnotes XML is empty.");
        }

        if (!reader.IsElement("footnotes", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidDataException("Footnotes root element is missing.");
        }

        this.HasFootnotesRoot = true;
        this.Footnotes = DocxFootnotes.Parse(reader);
        return true;
    }

    /// <summary>
    /// Returns the document footnotes.
    /// </summary>
    /// <returns>The document footnotes.</returns>
    public DocxFootnotes GetFootnotes()
    {
        return this.Footnotes;
    }

    internal bool HasFootnotes()
    {
        return this.Footnotes.HasXml();
    }

    internal int FootnoteCount()
    {
        return this.Footnotes.FootnoteCount();
    }

    internal DocxFootnote GetFootnote(int index)
    {
        return this.Footnotes.GetFootnote(index);
    }

    internal DocxFootnote? FindFootnoteById(string id)
    {
        return this.Footnotes.FindFootnoteById(id);
    }

    internal DocxFootnotes GetOrCreateFootnotes()
    {
        if (this.FootnotesPart != null)
        {
            return this.Footnotes;
        }

        return CreateFootnotesPart();
    }

    internal DocxFootnotes CreateFootnotesPart()
    {
        if (this.FootnotesPart != null)
        {
            return this.Footnotes;
        }

        string partName = this.Package.GetFootnotesPartName();

        if (partName == "")
        {
            partName = AllocateNotesPartName("footnotes");
            DocxRelationshipSet relationshipSet = GetOrCreateMainDocumentRelationshipSet();
            string target = GetMainDocumentRelativeTarget(partName);
            relationshipSet.CreateRelationship(DocxConstants.RelationshipTypeFootnotes, target, false);
            WriteRelationshipSetPart(relationshipSet);
        }

        this.Footnotes = DocxFootnotes.Create();
        this.Package.AddXmlPartWithContent(partName, DocxConstants.ContentTypeFootnotes, this.Footnotes.WriteXml());
        this.FootnotesPart = this.Package.FindPart(partName);
        this.HasFootnotesRoot = true;
        return this.Footnotes;
    }

    internal DocxFootnote CreateFootnote(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        DocxFootnotes footnotes = GetOrCreateFootnotes();
        DocxFootnote footnote = footnotes.CreateFootnote(text);
        WriteFootnotesPart();
        return footnote;
    }

    internal DocxFootnote CreateFootnoteWithId(string id, string text)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (id == "")
        {
            throw new ArgumentException("Footnote id is required.", "id");
        }

        DocxFootnotes footnotes = GetOrCreateFootnotes();
        DocxFootnote footnote = footnotes.CreateFootnoteWithId(id, text);
        WriteFootnotesPart();
        return footnote;
    }

    internal void WriteFootnotesPart()
    {
        if (this.FootnotesPart == null)
        {
            throw new InvalidOperationException("Footnotes part is missing.");
        }

        if (this.Package.ContainsPart(this.FootnotesPart.PartName))
        {
            ReplaceModeledXmlPart(this.FootnotesPart.PartName, DocxConstants.ContentTypeFootnotes, this.Footnotes.WriteXml());
        }
        else
        {
            this.Package.AddXmlPartWithContent(this.FootnotesPart.PartName, DocxConstants.ContentTypeFootnotes, this.Footnotes.WriteXml());
        }

        this.FootnotesPart = this.Package.FindPart(this.FootnotesPart.PartName);
    }

    internal bool ParseEndnotes()
    {
        this.EndnotesPart = this.Package.GetEndnotesPart();
        this.Endnotes = DocxEndnotes.Create();
        this.HasEndnotesRoot = false;

        if (this.EndnotesPart == null)
        {
            return false;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.EndnotesPart.GetText());

        if (!reader.MoveToContent())
        {
            throw new InvalidDataException("Endnotes XML is empty.");
        }

        if (!reader.IsElement("endnotes", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidDataException("Endnotes root element is missing.");
        }

        this.HasEndnotesRoot = true;
        this.Endnotes = DocxEndnotes.Parse(reader);
        return true;
    }

    /// <summary>
    /// Returns the document endnotes.
    /// </summary>
    /// <returns>The document endnotes.</returns>
    public DocxEndnotes GetEndnotes()
    {
        return this.Endnotes;
    }

    internal bool HasEndnotes()
    {
        return this.Endnotes.HasXml();
    }

    internal int EndnoteCount()
    {
        return this.Endnotes.EndnoteCount();
    }

    internal DocxEndnote GetEndnote(int index)
    {
        return this.Endnotes.GetEndnote(index);
    }

    internal DocxEndnote? FindEndnoteById(string id)
    {
        return this.Endnotes.FindEndnoteById(id);
    }

    internal bool ParseComments()
    {
        this.CommentsPart = this.Package.GetCommentsPart();
        this.Comments = DocxComments.Create();
        this.HasCommentsRoot = false;

        if (this.CommentsPart == null)
        {
            return false;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.CommentsPart.GetText());

        if (!reader.MoveToContent())
        {
            throw new InvalidDataException("Comments XML is empty.");
        }

        if (!reader.IsElement("comments", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidDataException("Comments root element is missing.");
        }

        this.HasCommentsRoot = true;
        this.Comments = DocxComments.Parse(reader);
        return true;
    }

    /// <summary>
    /// Returns the document comments.
    /// </summary>
    /// <returns>The document comments.</returns>
    public DocxComments GetComments()
    {
        return this.Comments;
    }

    internal bool HasComments()
    {
        return this.Comments.HasXml();
    }

    internal int CommentCount()
    {
        return this.Comments.CommentCount();
    }

    internal DocxComment GetComment(int index)
    {
        return this.Comments.GetComment(index);
    }

    internal DocxComment? FindCommentById(string id)
    {
        return this.Comments.FindCommentById(id);
    }

    internal DocxComments GetOrCreateComments()
    {
        if (this.CommentsPart != null)
        {
            return this.Comments;
        }

        return CreateCommentsPart();
    }

    internal DocxComments CreateCommentsPart()
    {
        if (this.CommentsPart != null)
        {
            return this.Comments;
        }

        string partName = this.Package.GetCommentsPartName();

        if (partName == "")
        {
            partName = AllocateCommentsPartName();
            DocxRelationshipSet relationshipSet = GetOrCreateMainDocumentRelationshipSet();
            string target = GetMainDocumentRelativeTarget(partName);
            relationshipSet.CreateRelationship(DocxConstants.RelationshipTypeComments, target, false);
            WriteRelationshipSetPart(relationshipSet);
        }

        this.Comments = DocxComments.Create();
        this.Package.AddXmlPartWithContent(partName, DocxConstants.ContentTypeComments, this.Comments.WriteXml());
        this.CommentsPart = this.Package.FindPart(partName);
        this.HasCommentsRoot = true;
        return this.Comments;
    }

    internal DocxComment CreateComment(string author, string initials, string text)
    {
        if (author == null)
        {
            throw new ArgumentNullException("author");
        }

        if (initials == null)
        {
            throw new ArgumentNullException("initials");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        DocxComments comments = GetOrCreateComments();
        DocxComment comment = comments.CreateComment(author, initials, text);
        WriteCommentsPart();
        return comment;
    }

    internal DocxComment CreateCommentWithId(string id, string author, string initials, string date, string text)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (author == null)
        {
            throw new ArgumentNullException("author");
        }

        if (initials == null)
        {
            throw new ArgumentNullException("initials");
        }

        if (date == null)
        {
            throw new ArgumentNullException("date");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (id == "")
        {
            throw new ArgumentException("Comment id is required.", "id");
        }

        DocxComments comments = GetOrCreateComments();
        DocxComment comment = comments.CreateCommentWithId(id, author, initials, date, text);
        WriteCommentsPart();
        return comment;
    }

    internal void WriteCommentsPart()
    {
        if (this.CommentsPart == null)
        {
            throw new InvalidOperationException("Comments part is missing.");
        }

        if (this.Package.ContainsPart(this.CommentsPart.PartName))
        {
            ReplaceModeledXmlPart(this.CommentsPart.PartName, DocxConstants.ContentTypeComments, this.Comments.WriteXml());
        }
        else
        {
            this.Package.AddXmlPartWithContent(this.CommentsPart.PartName, DocxConstants.ContentTypeComments, this.Comments.WriteXml());
        }

        this.CommentsPart = this.Package.FindPart(this.CommentsPart.PartName);
    }

    internal string AllocateCommentsPartName()
    {
        return AllocateNotesPartName("comments");
    }

    internal DocxEndnotes GetOrCreateEndnotes()
    {
        if (this.EndnotesPart != null)
        {
            return this.Endnotes;
        }

        return CreateEndnotesPart();
    }

    internal DocxEndnotes CreateEndnotesPart()
    {
        if (this.EndnotesPart != null)
        {
            return this.Endnotes;
        }

        string partName = this.Package.GetEndnotesPartName();

        if (partName == "")
        {
            partName = AllocateNotesPartName("endnotes");
            DocxRelationshipSet relationshipSet = GetOrCreateMainDocumentRelationshipSet();
            string target = GetMainDocumentRelativeTarget(partName);
            relationshipSet.CreateRelationship(DocxConstants.RelationshipTypeEndnotes, target, false);
            WriteRelationshipSetPart(relationshipSet);
        }

        this.Endnotes = DocxEndnotes.Create();
        this.Package.AddXmlPartWithContent(partName, DocxConstants.ContentTypeEndnotes, this.Endnotes.WriteXml());
        this.EndnotesPart = this.Package.FindPart(partName);
        this.HasEndnotesRoot = true;
        return this.Endnotes;
    }

    internal DocxEndnote CreateEndnote(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        DocxEndnotes endnotes = GetOrCreateEndnotes();
        DocxEndnote endnote = endnotes.CreateEndnote(text);
        WriteEndnotesPart();
        return endnote;
    }

    internal DocxEndnote CreateEndnoteWithId(string id, string text)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (id == "")
        {
            throw new ArgumentException("Endnote id is required.", "id");
        }

        DocxEndnotes endnotes = GetOrCreateEndnotes();
        DocxEndnote endnote = endnotes.CreateEndnoteWithId(id, text);
        WriteEndnotesPart();
        return endnote;
    }

    internal void WriteEndnotesPart()
    {
        if (this.EndnotesPart == null)
        {
            throw new InvalidOperationException("Endnotes part is missing.");
        }

        if (this.Package.ContainsPart(this.EndnotesPart.PartName))
        {
            ReplaceModeledXmlPart(this.EndnotesPart.PartName, DocxConstants.ContentTypeEndnotes, this.Endnotes.WriteXml());
        }
        else
        {
            this.Package.AddXmlPartWithContent(this.EndnotesPart.PartName, DocxConstants.ContentTypeEndnotes, this.Endnotes.WriteXml());
        }

        this.EndnotesPart = this.Package.FindPart(this.EndnotesPart.PartName);
    }

    internal string AllocateNotesPartName(string baseName)
    {
        if (baseName == null)
        {
            throw new ArgumentNullException("baseName");
        }

        if (baseName == "")
        {
            throw new ArgumentException("Part base name is required.", "baseName");
        }

        string mainDocumentPartName = this.Package.GetMainDocumentPartName();

        if (mainDocumentPartName == "")
        {
            throw new InvalidOperationException("Main document part is missing.");
        }

        string baseDirectory = DocxRelationshipSet.GetSourceBaseDirectory(mainDocumentPartName);
        string partName = baseDirectory + baseName + ".xml";

        if (!this.Package.ContainsPart(partName))
        {
            return partName;
        }

        int index = 1;

        while (true)
        {
            partName = baseDirectory + baseName + index.ToString() + ".xml";

            if (!this.Package.ContainsPart(partName))
            {
                return partName;
            }

            index = index + 1;
        }
    }

    internal DocxStyle? FindStyleById(string styleId)
    {
        if (styleId == null)
        {
            throw new ArgumentNullException("styleId");
        }

        return this.Styles.FindById(styleId);
    }

    internal DocxStyle? FindStyleByName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        return this.Styles.FindByName(name);
    }

    internal DocxStyle CreateStyle(string type, string styleId, string name)
    {
        return this.Styles.CreateStyle(type, styleId, name);
    }

    internal DocxValidationReport GetStyleImportDiagnostics(DocxStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        return this.Styles.GetImportDiagnostics(style);
    }

    internal bool ImportStyle(DocxStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        return this.Styles.ImportStyle(style);
    }

    internal int ImportStyles(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        return this.Styles.ImportStyles(document.Styles);
    }

    internal int ImportStyleDefinitions(DocxStyles styles)
    {
        if (styles == null)
        {
            throw new ArgumentNullException("styles");
        }

        return this.Styles.ImportStyles(styles);
    }

    internal DocxValidationReport DeleteStyle(string styleId)
    {
        if (styleId == null)
        {
            throw new ArgumentNullException("styleId");
        }

        return this.Styles.DeleteStyle(styleId);
    }
}
