namespace docx;

/// <summary>
/// Provides standard namespace, relationship, and content-type identifiers used by DOCX files.
/// </summary>
public static class DocxConstants
{
    /// <summary>
    /// The default opening delimiter for document template placeholders.
    /// </summary>
    public const string DefaultTemplatePlaceholderPrefix = "{{";
    /// <summary>
    /// The default closing delimiter for document template placeholders.
    /// </summary>
    public const string DefaultTemplatePlaceholderSuffix = "}}";
    /// <summary>
    /// The namespace content types identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";
    /// <summary>
    /// The namespace package relationships identifier used in DOCX package data.
    /// </summary>
    public const string NamespacePackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    /// <summary>
    /// The namespace relationships identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    /// <summary>
    /// The namespace core properties identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceCoreProperties = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
    /// <summary>
    /// The namespace dublin core identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceDublinCore = "http://purl.org/dc/elements/1.1/";
    /// <summary>
    /// The namespace dublin core terms identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceDublinCoreTerms = "http://purl.org/dc/terms/";
    /// <summary>
    /// The namespace dublin core type identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceDublinCoreType = "http://purl.org/dc/dcmitype/";
    /// <summary>
    /// The namespace xml schema instance identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceXmlSchemaInstance = "http://www.w3.org/2001/XMLSchema-instance";
    /// <summary>
    /// The namespace extended properties identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceExtendedProperties = "http://schemas.openxmlformats.org/officeDocument/2006/extended-properties";
    /// <summary>
    /// The namespace doc props v types identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceDocPropsVTypes = "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes";
    /// <summary>
    /// The namespace custom properties identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceCustomProperties = "http://schemas.openxmlformats.org/officeDocument/2006/custom-properties";
    /// <summary>
    /// The namespace wordprocessing ml identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceWordprocessingMl = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    /// <summary>
    /// The namespace strict wordprocessing ml identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceStrictWordprocessingMl = "http://purl.oclc.org/ooxml/wordprocessingml/main";
    /// <summary>
    /// The namespace markup compatibility identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceMarkupCompatibility = "http://schemas.openxmlformats.org/markup-compatibility/2006";
    /// <summary>
    /// The namespace drawing ml identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceDrawingMl = "http://schemas.openxmlformats.org/drawingml/2006/main";
    /// <summary>
    /// The namespace strict drawing ml identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceStrictDrawingMl = "http://purl.oclc.org/ooxml/drawingml/main";
    /// <summary>
    /// The namespace wordprocessing drawing identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceWordprocessingDrawing = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
    /// <summary>
    /// The namespace strict wordprocessing drawing identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceStrictWordprocessingDrawing = "http://purl.oclc.org/ooxml/drawingml/wordprocessingDrawing";
    /// <summary>
    /// The namespace drawing ml picture identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceDrawingMlPicture = "http://schemas.openxmlformats.org/drawingml/2006/picture";
    /// <summary>
    /// The namespace strict drawing ml picture identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceStrictDrawingMlPicture = "http://purl.oclc.org/ooxml/drawingml/picture";
    /// <summary>
    /// The namespace drawing ml chart identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceDrawingMlChart = "http://schemas.openxmlformats.org/drawingml/2006/chart";
    /// <summary>
    /// The namespace strict drawing ml chart identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceStrictDrawingMlChart = "http://purl.oclc.org/ooxml/drawingml/chart";
    /// <summary>
    /// The namespace drawing ml diagram identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceDrawingMlDiagram = "http://schemas.openxmlformats.org/drawingml/2006/diagram";
    /// <summary>
    /// The namespace strict drawing ml diagram identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceStrictDrawingMlDiagram = "http://purl.oclc.org/ooxml/drawingml/diagram";
    /// <summary>
    /// The namespace vml identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceVml = "urn:schemas-microsoft-com:vml";
    /// <summary>
    /// The namespace office identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceOffice = "urn:schemas-microsoft-com:office:office";
    /// <summary>
    /// The namespace xml identifier used in DOCX package data.
    /// </summary>
    public const string NamespaceXml = "http://www.w3.org/XML/1998/namespace";

    /// <summary>
    /// The relationship type prefix identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypePrefix = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/";
    /// <summary>
    /// The relationship type strict prefix identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeStrictPrefix = "http://purl.oclc.org/ooxml/officeDocument/relationships/";
    /// <summary>
    /// The relationship type office document identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeOfficeDocument = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
    /// <summary>
    /// The relationship type styles identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeStyles = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles";
    /// <summary>
    /// The relationship type numbering identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeNumbering = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/numbering";
    /// <summary>
    /// The relationship type settings identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeSettings = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/settings";
    /// <summary>
    /// The relationship type web settings identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeWebSettings = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/webSettings";
    /// <summary>
    /// The relationship type font table identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeFontTable = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/fontTable";
    /// <summary>
    /// The relationship type theme identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeTheme = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme";
    /// <summary>
    /// The relationship type header identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeHeader = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/header";
    /// <summary>
    /// The relationship type footer identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeFooter = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer";
    /// <summary>
    /// The relationship type image identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeImage = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image";
    /// <summary>
    /// The relationship type hyperlink identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeHyperlink = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink";
    /// <summary>
    /// The relationship type footnotes identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeFootnotes = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/footnotes";
    /// <summary>
    /// The relationship type endnotes identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeEndnotes = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/endnotes";
    /// <summary>
    /// The relationship type comments identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeComments = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/comments";
    /// <summary>
    /// The relationship type core properties identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeCoreProperties = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties";
    /// <summary>
    /// The relationship type extended properties identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeExtendedProperties = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties";
    /// <summary>
    /// The relationship type custom properties identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeCustomProperties = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/custom-properties";
    /// <summary>
    /// The relationship type custom xml identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeCustomXml = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/customXml";
    /// <summary>
    /// The relationship type custom XML properties identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeCustomXmlProperties = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/customXmlProps";
    /// <summary>
    /// The relationship type vba project identifier used in DOCX package data.
    /// </summary>
    public const string RelationshipTypeVbaProject = "http://schemas.microsoft.com/office/2006/relationships/vbaProject";

    /// <summary>
    /// The content type relationships identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeRelationships = "application/vnd.openxmlformats-package.relationships+xml";
    /// <summary>
    /// The content type xml identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeXml = "application/xml";
    /// <summary>
    /// The content type custom XML properties identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeCustomXmlProperties = "application/vnd.openxmlformats-officedocument.customXmlProperties+xml";
    /// <summary>
    /// The content type main document identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeMainDocument = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml";
    /// <summary>
    /// The content type template main document identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeTemplateMainDocument = "application/vnd.openxmlformats-officedocument.wordprocessingml.template.main+xml";
    /// <summary>
    /// The content type macro enabled main document identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeMacroEnabledMainDocument = "application/vnd.ms-word.document.macroEnabled.main+xml";
    /// <summary>
    /// The content type macro enabled template main document identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeMacroEnabledTemplateMainDocument = "application/vnd.ms-word.template.macroEnabledTemplate.main+xml";
    /// <summary>
    /// The content type vba project identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeVbaProject = "application/vnd.ms-office.vbaProject";
    /// <summary>
    /// The content type styles identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeStyles = "application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml";
    /// <summary>
    /// The content type numbering identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeNumbering = "application/vnd.openxmlformats-officedocument.wordprocessingml.numbering+xml";
    /// <summary>
    /// The content type settings identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeSettings = "application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml";
    /// <summary>
    /// The content type web settings identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeWebSettings = "application/vnd.openxmlformats-officedocument.wordprocessingml.webSettings+xml";
    /// <summary>
    /// The content type font table identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeFontTable = "application/vnd.openxmlformats-officedocument.wordprocessingml.fontTable+xml";
    /// <summary>
    /// The content type theme identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeTheme = "application/vnd.openxmlformats-officedocument.theme+xml";
    /// <summary>
    /// The content type header identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeHeader = "application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml";
    /// <summary>
    /// The content type footer identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeFooter = "application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml";
    /// <summary>
    /// The content type footnotes identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeFootnotes = "application/vnd.openxmlformats-officedocument.wordprocessingml.footnotes+xml";
    /// <summary>
    /// The content type endnotes identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeEndnotes = "application/vnd.openxmlformats-officedocument.wordprocessingml.endnotes+xml";
    /// <summary>
    /// The content type comments identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeComments = "application/vnd.openxmlformats-officedocument.wordprocessingml.comments+xml";
    /// <summary>
    /// The content type core properties identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeCoreProperties = "application/vnd.openxmlformats-package.core-properties+xml";
    /// <summary>
    /// The content type extended properties identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeExtendedProperties = "application/vnd.openxmlformats-officedocument.extended-properties+xml";
    /// <summary>
    /// The content type custom properties identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeCustomProperties = "application/vnd.openxmlformats-officedocument.custom-properties+xml";
    /// <summary>
    /// The content type png identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypePng = "image/png";
    /// <summary>
    /// The content type jpeg identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeJpeg = "image/jpeg";
    /// <summary>
    /// The content type gif identifier used in DOCX package data.
    /// </summary>
    public const string ContentTypeGif = "image/gif";
}
