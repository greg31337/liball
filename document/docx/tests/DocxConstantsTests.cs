using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxConstantsTests
{
    [Test]
    public void NamespaceConstants_ExposeCoreOpcAndWordprocessingUris()
    {
        Assert.That(global::docx.DocxConstants.NamespaceContentTypes, Is.EqualTo("http://schemas.openxmlformats.org/package/2006/content-types"));
        Assert.That(global::docx.DocxConstants.NamespacePackageRelationships, Is.EqualTo("http://schemas.openxmlformats.org/package/2006/relationships"));
        Assert.That(global::docx.DocxConstants.NamespaceRelationships, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships"));
        Assert.That(global::docx.DocxConstants.NamespaceWordprocessingMl, Is.EqualTo("http://schemas.openxmlformats.org/wordprocessingml/2006/main"));
        Assert.That(global::docx.DocxConstants.NamespaceXml, Is.EqualTo("http://www.w3.org/XML/1998/namespace"));
        Assert.That(global::docx.DocxConstants.NamespaceMarkupCompatibility, Is.EqualTo("http://schemas.openxmlformats.org/markup-compatibility/2006"));
        Assert.That(global::docx.DocxConstants.NamespaceDrawingMl, Is.EqualTo("http://schemas.openxmlformats.org/drawingml/2006/main"));
        Assert.That(global::docx.DocxConstants.NamespaceWordprocessingDrawing, Is.EqualTo("http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing"));
        Assert.That(global::docx.DocxConstants.NamespaceDrawingMlPicture, Is.EqualTo("http://schemas.openxmlformats.org/drawingml/2006/picture"));
        Assert.That(global::docx.DocxConstants.NamespaceDrawingMlChart, Is.EqualTo("http://schemas.openxmlformats.org/drawingml/2006/chart"));
        Assert.That(global::docx.DocxConstants.NamespaceDrawingMlDiagram, Is.EqualTo("http://schemas.openxmlformats.org/drawingml/2006/diagram"));
        Assert.That(global::docx.DocxConstants.NamespaceVml, Is.EqualTo("urn:schemas-microsoft-com:vml"));
        Assert.That(global::docx.DocxConstants.NamespaceOffice, Is.EqualTo("urn:schemas-microsoft-com:office:office"));
    }

    [Test]
    public void RelationshipTypeConstants_ExposeCoreDocxRelationshipUris()
    {
        Assert.That(global::docx.DocxConstants.RelationshipTypeOfficeDocument, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeStyles, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeNumbering, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/numbering"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeSettings, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/settings"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeWebSettings, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/webSettings"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeFontTable, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/fontTable"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeTheme, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeHeader, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/header"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeFooter, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeImage, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/image"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeHyperlink, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeFootnotes, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/footnotes"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeEndnotes, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/endnotes"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeComments, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/comments"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeCoreProperties, Is.EqualTo("http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeExtendedProperties, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeCustomProperties, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/custom-properties"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeCustomXml, Is.EqualTo("http://schemas.openxmlformats.org/officeDocument/2006/relationships/customXml"));
        Assert.That(global::docx.DocxConstants.RelationshipTypeVbaProject, Is.EqualTo("http://schemas.microsoft.com/office/2006/relationships/vbaProject"));
    }

    [Test]
    public void ContentTypeConstants_ExposeCoreDocxContentTypes()
    {
        Assert.That(global::docx.DocxConstants.ContentTypeRelationships, Is.EqualTo("application/vnd.openxmlformats-package.relationships+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeXml, Is.EqualTo("application/xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeMainDocument, Is.EqualTo("application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeMacroEnabledMainDocument, Is.EqualTo("application/vnd.ms-word.document.macroEnabled.main+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeVbaProject, Is.EqualTo("application/vnd.ms-office.vbaProject"));
        Assert.That(global::docx.DocxConstants.ContentTypeStyles, Is.EqualTo("application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeNumbering, Is.EqualTo("application/vnd.openxmlformats-officedocument.wordprocessingml.numbering+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeSettings, Is.EqualTo("application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeWebSettings, Is.EqualTo("application/vnd.openxmlformats-officedocument.wordprocessingml.webSettings+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeFontTable, Is.EqualTo("application/vnd.openxmlformats-officedocument.wordprocessingml.fontTable+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeTheme, Is.EqualTo("application/vnd.openxmlformats-officedocument.theme+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeHeader, Is.EqualTo("application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeFooter, Is.EqualTo("application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeFootnotes, Is.EqualTo("application/vnd.openxmlformats-officedocument.wordprocessingml.footnotes+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeEndnotes, Is.EqualTo("application/vnd.openxmlformats-officedocument.wordprocessingml.endnotes+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeComments, Is.EqualTo("application/vnd.openxmlformats-officedocument.wordprocessingml.comments+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeCoreProperties, Is.EqualTo("application/vnd.openxmlformats-package.core-properties+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeExtendedProperties, Is.EqualTo("application/vnd.openxmlformats-officedocument.extended-properties+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypeCustomProperties, Is.EqualTo("application/vnd.openxmlformats-officedocument.custom-properties+xml"));
        Assert.That(global::docx.DocxConstants.ContentTypePng, Is.EqualTo("image/png"));
        Assert.That(global::docx.DocxConstants.ContentTypeJpeg, Is.EqualTo("image/jpeg"));
        Assert.That(global::docx.DocxConstants.ContentTypeGif, Is.EqualTo("image/gif"));
    }
}
