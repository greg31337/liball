using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxDocumentNavigationApiTests
{
    [Test]
    public void DocumentModel_NavigatesRequestedPartsAndRelationshipDiscoveredStories()
    {
        global::docx.DocxPackage package = CreateNavigationPackage();
        global::docx.IDocxDocumentModel document = new global::docx.DocxDocument(package);

        Assert.That(document.GetBody().GetText(), Is.EqualTo("Body control"));
        Assert.That(document.GetCoreProperties().Title, Is.EqualTo("Navigation title"));
        Assert.That(document.GetCustomProperties().Properties.Single().Value, Is.EqualTo("Navigation value"));
        Assert.That(document.GetHeaders().Select(header => header.GetText()), Is.EqualTo(new string[]
        {
            "Header control",
            "Secondary header"
        }));
        Assert.That(document.GetFooters().Single().GetText(), Is.EqualTo("Footer text"));
        Assert.That(document.GetFootnotes().GetText(), Is.EqualTo("Footnote control"));
        Assert.That(document.GetEndnotes().GetText(), Is.EqualTo("Endnote text"));
        Assert.That(document.GetComments().GetText(), Is.EqualTo("Comment text"));
        Assert.That(document.GetCustomXmlParts().Parts.Select(part => part.PartName), Is.EqualTo(new string[]
        {
            "/customXml/item1.xml",
            "/customXml/item2.xml"
        }));
        Assert.That(document.GetWebSettingsPart()!.PartName, Is.EqualTo("/word/webSettings.xml"));
        Assert.That(document.GetFontTablePart()!.PartName, Is.EqualTo("/word/fontTable.xml"));
        Assert.That(document.GetThemePart()!.PartName, Is.EqualTo("/word/theme/theme1.xml"));

        List<global::docx.DocxStory> stories = document.GetStories();

        Assert.That(stories.Select(story => story.Type), Is.EqualTo(new global::docx.DocxStoryType[]
        {
            global::docx.DocxStoryType.MainDocument,
            global::docx.DocxStoryType.Header,
            global::docx.DocxStoryType.Footer,
            global::docx.DocxStoryType.Footnotes,
            global::docx.DocxStoryType.Endnotes,
            global::docx.DocxStoryType.Comments,
            global::docx.DocxStoryType.Header
        }));
        Assert.That(stories[0].SourcePartName, Is.EqualTo("/"));
        Assert.That(stories[0].PartName, Is.EqualTo("/word/document.xml"));
        Assert.That(stories[6].SourcePartName, Is.EqualTo("/word/other.xml"));
        Assert.That(stories[6].PartName, Is.EqualTo("/word/header2.xml"));
        Assert.That(stories[6].Content.GetText(), Is.EqualTo("Secondary header"));
    }

    [Test]
    public void DocumentModel_FindsNestedContentControlsAcrossParsedStories()
    {
        global::docx.IDocxDocumentModel document = new global::docx.DocxDocument(CreateNavigationPackage());

        List<global::docx.DocxContentControl> contentControls = document.GetContentControls();

        Assert.That(contentControls.Select(control => control.GetText()), Is.EqualTo(new string[]
        {
            "Body control",
            "Header control",
            "Footnote control"
        }));
    }

    [Test]
    public void DocumentModel_ReturnsIndependentNavigationListsAndEmptyOptionalParts()
    {
        global::docx.IDocxDocumentModel populated = new global::docx.DocxDocument(CreateNavigationPackage());
        List<global::docx.DocxHeader> headers = populated.GetHeaders();
        List<global::docx.DocxStory> stories = populated.GetStories();
        headers.Clear();
        stories.Clear();

        Assert.That(populated.GetHeaders(), Has.Count.EqualTo(2));
        Assert.That(populated.GetStories(), Has.Count.EqualTo(7));

        global::docx.IDocxDocumentModel empty = new global::docx.DocxPackagePersistence().CreateDocument();

        Assert.That(empty.GetHeaders(), Is.Empty);
        Assert.That(empty.GetFooters(), Is.Empty);
        Assert.That(empty.GetContentControls(), Is.Empty);
        Assert.That(empty.GetCustomXmlParts().Parts, Is.Empty);
        Assert.That(empty.GetFontTablePart(), Is.Null);
        Assert.That(empty.GetThemePart(), Is.Null);
        Assert.That(empty.GetWebSettingsPart(), Is.Null);
        Assert.That(empty.GetStories(), Has.Count.EqualTo(1));
    }

    [Test]
    public void StoryDescriptor_RejectsNullNavigationData()
    {
        global::docx.IDocxTextContent content = new global::docx.DocxBody("");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxStory(
            global::docx.DocxStoryType.MainDocument,
            null!,
            "/word/document.xml",
            "rId1",
            content));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxStory(
            global::docx.DocxStoryType.MainDocument,
            "/",
            null!,
            "rId1",
            content));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxStory(
            global::docx.DocxStoryType.MainDocument,
            "/",
            "/word/document.xml",
            null!,
            content));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxStory(
            global::docx.DocxStoryType.MainDocument,
            "/",
            "/word/document.xml",
            "rId1",
            null!));
    }

    static global::docx.DocxPackage CreateNavigationPackage()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackagePersistence().CreatePackage();
        package.ReplaceXmlPart(
            "/word/document.xml",
            global::docx.DocxConstants.ContentTypeMainDocument,
            CreateMainDocumentXml());

        AddRelatedXmlPart(
            package,
            "/",
            global::docx.DocxConstants.RelationshipTypeCoreProperties,
            "docProps/core.xml",
            "/docProps/core.xml",
            global::docx.DocxConstants.ContentTypeCoreProperties,
            "<cp:coreProperties xmlns:cp=\"" + global::docx.DocxConstants.NamespaceCoreProperties
                + "\" xmlns:dc=\"" + global::docx.DocxConstants.NamespaceDublinCore
                + "\"><dc:title>Navigation title</dc:title></cp:coreProperties>");
        AddRelatedXmlPart(
            package,
            "/",
            global::docx.DocxConstants.RelationshipTypeCustomProperties,
            "docProps/custom.xml",
            "/docProps/custom.xml",
            global::docx.DocxConstants.ContentTypeCustomProperties,
            "<Properties xmlns=\"" + global::docx.DocxConstants.NamespaceCustomProperties
                + "\" xmlns:vt=\"" + global::docx.DocxConstants.NamespaceDocPropsVTypes
                + "\"><property fmtid=\"" + global::docx.DocxCustomProperty.DefaultFormatId
                + "\" pid=\"2\" name=\"Navigation\"><vt:lpwstr>Navigation value</vt:lpwstr></property></Properties>");
        AddRelatedXmlPart(
            package,
            "/",
            global::docx.DocxConstants.RelationshipTypeCustomXml,
            "customXml/item1.xml",
            "/customXml/item1.xml",
            global::docx.DocxConstants.ContentTypeXml,
            "<item>one</item>");

        string mainPartName = "/word/document.xml";
        AddRelatedXmlPart(package, mainPartName, global::docx.DocxConstants.RelationshipTypeHeader, "header1.xml", "/word/header1.xml", global::docx.DocxConstants.ContentTypeHeader, CreateHeaderXml("Header control", true));
        AddRelatedXmlPart(package, mainPartName, global::docx.DocxConstants.RelationshipTypeFooter, "footer1.xml", "/word/footer1.xml", global::docx.DocxConstants.ContentTypeFooter, CreateFooterXml("Footer text"));
        AddRelatedXmlPart(package, mainPartName, global::docx.DocxConstants.RelationshipTypeFootnotes, "footnotes.xml", "/word/footnotes.xml", global::docx.DocxConstants.ContentTypeFootnotes, CreateFootnotesXml());
        AddRelatedXmlPart(package, mainPartName, global::docx.DocxConstants.RelationshipTypeEndnotes, "endnotes.xml", "/word/endnotes.xml", global::docx.DocxConstants.ContentTypeEndnotes, CreateEndnotesXml());
        AddRelatedXmlPart(package, mainPartName, global::docx.DocxConstants.RelationshipTypeComments, "comments.xml", "/word/comments.xml", global::docx.DocxConstants.ContentTypeComments, CreateCommentsXml());
        AddRelatedXmlPart(package, mainPartName, global::docx.DocxConstants.RelationshipTypeWebSettings, "webSettings.xml", "/word/webSettings.xml", global::docx.DocxConstants.ContentTypeWebSettings, "<w:webSettings xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl + "\" />");
        AddRelatedXmlPart(package, mainPartName, global::docx.DocxConstants.RelationshipTypeFontTable, "fontTable.xml", "/word/fontTable.xml", global::docx.DocxConstants.ContentTypeFontTable, "<w:fonts xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl + "\" />");
        AddRelatedXmlPart(package, mainPartName, global::docx.DocxConstants.RelationshipTypeTheme, "theme/theme1.xml", "/word/theme/theme1.xml", global::docx.DocxConstants.ContentTypeTheme, "<a:theme xmlns:a=\"" + global::docx.DocxConstants.NamespaceDrawingMl + "\" />");

        package.AddXmlPartWithContent("/word/other.xml", global::docx.DocxConstants.ContentTypeXml, "<other />");
        AddRelatedXmlPart(package, "/word/other.xml", global::docx.DocxConstants.RelationshipTypeHeader, "header2.xml", "/word/header2.xml", global::docx.DocxConstants.ContentTypeHeader, CreateHeaderXml("Secondary header", false));
        AddRelatedXmlPart(package, "/word/other.xml", global::docx.DocxConstants.RelationshipTypeCustomXml, "../customXml/item2.xml", "/customXml/item2.xml", global::docx.DocxConstants.ContentTypeXml, "<item>two</item>");
        return package;
    }

    static void AddRelatedXmlPart(
        global::docx.DocxPackage package,
        string sourcePartName,
        string relationshipType,
        string target,
        string partName,
        string contentType,
        string xml)
    {
        package.AddXmlPartWithContent(partName, contentType, xml);
        package.CreateRelationship(sourcePartName, relationshipType, target, false);
    }

    static string CreateMainDocumentXml()
    {
        return "<w:document xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\"><w:body>" + CreateBlockContentControlXml("Body control") + "</w:body></w:document>";
    }

    static string CreateHeaderXml(string text, bool contentControl)
    {
        string content = "<w:r><w:t>" + text + "</w:t></w:r>";

        if (contentControl)
        {
            content = CreateInlineContentControlXml(text);
        }

        return "<w:hdr xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\"><w:p>" + content + "</w:p></w:hdr>";
    }

    static string CreateFooterXml(string text)
    {
        return "<w:ftr xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\"><w:p><w:r><w:t>" + text + "</w:t></w:r></w:p></w:ftr>";
    }

    static string CreateFootnotesXml()
    {
        return "<w:footnotes xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\"><w:footnote w:id=\"1\"><w:p>" + CreateInlineContentControlXml("Footnote control")
            + "</w:p></w:footnote></w:footnotes>";
    }

    static string CreateEndnotesXml()
    {
        return "<w:endnotes xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\"><w:endnote w:id=\"1\"><w:p><w:r><w:t>Endnote text</w:t></w:r></w:p></w:endnote></w:endnotes>";
    }

    static string CreateCommentsXml()
    {
        return "<w:comments xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\"><w:comment w:id=\"1\" w:author=\"Author\"><w:p><w:r><w:t>Comment text</w:t></w:r></w:p></w:comment></w:comments>";
    }

    static string CreateBlockContentControlXml(string text)
    {
        return "<w:sdt><w:sdtPr><w:tag w:val=\"body\" /></w:sdtPr><w:sdtContent><w:p><w:r><w:t>"
            + text + "</w:t></w:r></w:p></w:sdtContent></w:sdt>";
    }

    static string CreateInlineContentControlXml(string text)
    {
        return "<w:sdt><w:sdtPr><w:tag w:val=\"inline\" /></w:sdtPr><w:sdtContent><w:r><w:t>"
            + text + "</w:t></w:r></w:sdtContent></w:sdt>";
    }
}
