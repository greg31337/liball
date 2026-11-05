namespace odf;

/// <summary>
/// Collects the typed components associated with an OpenDocument document.
/// </summary>
public class OdfDocument
{
    /// <summary>
    /// The load options associated with this document.
    /// </summary>
    public OdfLoadOptions LoadOptions = new OdfLoadOptions();
    /// <summary>
    /// The preferred options for subsequently saving this document.
    /// </summary>
    public OdfSaveOptions SaveOptions = new OdfSaveOptions();
    /// <summary>
    /// The document version information, when known.
    /// </summary>
    public OdfVersionInfo? VersionInfo;
    /// <summary>
    /// The document or template kind.
    /// </summary>
    public OdfDocumentKind Kind;
    /// <summary>
    /// The physical document representation.
    /// </summary>
    public OdfPackageKind PackageKind;
    /// <summary>
    /// The declared or inferred document media type.
    /// </summary>
    public string MediaType = "";
    /// <summary>
    /// The materialized standard XML parts.
    /// </summary>
    public OdfPart[] Parts = [];
    /// <summary>
    /// The standard document body section, when present.
    /// </summary>
    public OdfNode? Body;
    /// <summary>
    /// Direct root content retained for passthrough without a standard section mapping.
    /// </summary>
    public OdfNode[] PassthroughNodes = [];
    /// <summary>
    /// Whether any materialized XML part declares a future OpenDocument version.
    /// </summary>
    public bool IsFutureVersion;
    /// <summary>
    /// The package entries.
    /// </summary>
    public OdfPackageEntry[] PackageEntries = [];
    /// <summary>
    /// The package manifest, when one has been loaded.
    /// </summary>
    public OdfManifest? Manifest;
    /// <summary>
    /// The materialized XML nodes.
    /// </summary>
    public OdfNode[] Nodes = [];
    /// <summary>
    /// The SDK collaboration operations associated with this document.
    /// </summary>
    public OdfOperation[] Operations = [];
    /// <summary>
    /// The standard Dublin Core and OpenDocument metadata.
    /// </summary>
    public OdfDocumentMetadata Metadata = new OdfDocumentMetadata();
    /// <summary>
    /// The RDF triples materialized from RDFa and package metadata sources.
    /// </summary>
    public OdfRdfTriple[] RdfTriples = [];
    internal RdfaDocumentState? RdfaState;
    internal RdfXmlDocumentState? RdfXmlState;
    internal OdfDocumentPartTrees? PartTrees;
    internal OdfPackage? Package;
    internal byte[] OriginalData = [];
    /// <summary>
    /// The typed cell and formula values.
    /// </summary>
    public OdfCellValue[] CellValues = [];
    /// <summary>
    /// The document styles.
    /// </summary>
    public OdfStyle[] Styles = [];
    internal StyleDocumentState? StyleState;
    /// <summary>
    /// The document font-face declarations.
    /// </summary>
    public OdfFontFace[] FontFaces = [];
    internal FontFaceDocumentState? FontFaceState;
    /// <summary>
    /// The document page layouts.
    /// </summary>
    public OdfPageLayout[] PageLayouts = [];
    /// <summary>
    /// The document master pages.
    /// </summary>
    public OdfMasterPage[] MasterPages = [];
    internal PageLayoutDocumentState? PageLayoutState;
    /// <summary>
    /// The document number, date, time, boolean, and text data styles.
    /// </summary>
    public OdfDataStyle[] DataStyles = [];
    internal DataStyleDocumentState? DataStyleState;
    /// <summary>
    /// The spreadsheet cell-content validation rules.
    /// </summary>
    public OdfContentValidation[] ContentValidations = [];
    /// <summary>
    /// The conditional maps owned by ordinary styles.
    /// </summary>
    public OdfConditionalStyle[] ConditionalStyles = [];
    internal SpreadsheetFormattingState? SpreadsheetFormattingState;
    /// <summary>
    /// The document list and outline style definitions.
    /// </summary>
    public OdfListStyle[] ListStyles = [];
    internal ListStyleDocumentState? ListStyleState;
    /// <summary>
    /// The document gradients, hatches, images, markers, sounds, and presentation layouts.
    /// </summary>
    public OdfStyleResource[] StyleResources = [];
    internal StyleResourceDocumentState? StyleResourceState;
    /// <summary>
    /// The document paragraphs and headings in document order.
    /// </summary>
    public OdfTextBlock[] TextBlocks = [];
    internal TextDocumentState? TextState;
    /// <summary>
    /// The document bookmarks, reference marks, and index markers and ranges.
    /// </summary>
    public OdfTextRange[] TextRanges = [];
    /// <summary>
    /// The tracked text cursors created for this document.
    /// </summary>
    public OdfTextCursor[] TextCursors = [];
    /// <summary>
    /// The document text sections.
    /// </summary>
    public OdfTextSection[] TextSections = [];
    /// <summary>
    /// The anchored drawing frames in text content.
    /// </summary>
    public OdfTextFrame[] TextFrames = [];
    /// <summary>
    /// The explicit and style-driven text-flow breaks.
    /// </summary>
    public OdfTextBreak[] TextBreaks = [];
    /// <summary>
    /// The ordered, unordered, image, and outline lists in document content.
    /// </summary>
    public OdfTextList[] TextLists = [];
    /// <summary>
    /// The standalone numbered paragraphs in document content.
    /// </summary>
    public OdfNumberedParagraph[] NumberedParagraphs = [];
    /// <summary>
    /// The document formulas.
    /// </summary>
    public OdfFormula[] Formulas = [];
    /// <summary>
    /// The drawing objects.
    /// </summary>
    public OdfDrawingObject[] DrawingObjects = [];
    /// <summary>
    /// The package, external, and inline-base64 drawing images.
    /// </summary>
    public OdfImage[] Images = [];
    /// <summary>
    /// The passive audio, video, plugin, applet, and floating-frame descriptions.
    /// </summary>
    public OdfMediaResource[] MediaResources = [];
    /// <summary>
    /// The embedded OpenDocument, OLE, MathML, and chart objects.
    /// </summary>
    public OdfEmbeddedObject[] EmbeddedObjects = [];
    /// <summary>
    /// The package thumbnail images.
    /// </summary>
    public OdfThumbnail[] Thumbnails = [];
    /// <summary>
    /// Package resource paths that are not referenced by materialized document content.
    /// </summary>
    public string[] OrphanedResourcePaths = [];
    /// <summary>
    /// The charts.
    /// </summary>
    public OdfChart[] Charts = [];
    /// <summary>
    /// The presentation slides in document order.
    /// </summary>
    public OdfSlide[] Slides = [];
    /// <summary>
    /// The presentation handout master, when present.
    /// </summary>
    public OdfPresentationHandout? PresentationHandout;
    /// <summary>
    /// The reusable presentation header, footer, and date-time declarations.
    /// </summary>
    public OdfPresentationDeclaration[] PresentationDeclarations = [];
    /// <summary>
    /// The animations.
    /// </summary>
    public OdfAnimation[] Animations = [];
    /// <summary>
    /// The slide transitions.
    /// </summary>
    public OdfSlideTransition[] SlideTransitions = [];
    /// <summary>
    /// The named custom presentation shows.
    /// </summary>
    public OdfPresentationShow[] PresentationShows = [];
    /// <summary>
    /// The presentation playback settings.
    /// </summary>
    public OdfPresentationSettings PresentationSettings =
        new OdfPresentationSettings();
    /// <summary>
    /// The diagnostics associated with the document.
    /// </summary>
    public OdfDiagnostic[] Diagnostics = [];
    /// <summary>
    /// The encryption descriptions.
    /// </summary>
    public OdfEncryptionInfo[] EncryptionEntries = [];
    /// <summary>
    /// The package signatures.
    /// </summary>
    public OdfSignature[] Signatures = [];
    /// <summary>
    /// The parsed package signature container files.
    /// </summary>
    public OdfSignatureContainer[] SignatureContainers = [];
    /// <summary>
    /// The tracked changes.
    /// </summary>
    public OdfChange[] Changes = [];
    /// <summary>
    /// Whether text change recording is enabled by the document setting.
    /// </summary>
    public bool TrackTextChanges;
    internal TextChangeDocumentState? TextChangeState;
    /// <summary>
    /// The annotations.
    /// </summary>
    public OdfAnnotation[] Annotations = [];
    internal AnnotationDocumentState? AnnotationState;
    /// <summary>
    /// The text fields.
    /// </summary>
    public OdfTextField[] TextFields = [];
    /// <summary>
    /// The variable, user-field, sequence, and DDE declarations.
    /// </summary>
    public OdfTextFieldDeclaration[] TextFieldDeclarations = [];
    internal TextFieldDocumentState? TextFieldState;
    /// <summary>
    /// The document footnotes and endnotes.
    /// </summary>
    public OdfNote[] Notes = [];
    /// <summary>
    /// The footnote and endnote configurations.
    /// </summary>
    public OdfNoteConfiguration[] NoteConfigurations = [];
    internal NoteDocumentState? NoteState;
    /// <summary>
    /// The generated table-of-content, alphabetical, bibliography, user, table, illustration, and object indexes.
    /// </summary>
    public OdfTextIndex[] TextIndexes = [];
    /// <summary>
    /// The global bibliography configurations.
    /// </summary>
    public OdfBibliographyConfiguration[] BibliographyConfigurations = [];
    /// <summary>
    /// The bibliography marks used as bibliography entries.
    /// </summary>
    public OdfBibliographyEntry[] BibliographyEntries = [];
    internal TextIndexDocumentState? TextIndexState;
    /// <summary>
    /// The explicit master-page sequences.
    /// </summary>
    public OdfPageSequence[] PageSequences = [];
    /// <summary>
    /// The inferred first/title-page patterns.
    /// </summary>
    public OdfTitlePagePattern[] TitlePagePatterns = [];
    /// <summary>
    /// The linked subdocuments in a master text document.
    /// </summary>
    public OdfMasterSubdocument[] MasterSubdocuments = [];
    internal MasterTextDocumentState? MasterTextState;
    /// <summary>
    /// The document tables, including tables nested in cells.
    /// </summary>
    public OdfTable[] Tables = [];
    /// <summary>
    /// The spreadsheet calculation and view settings.
    /// </summary>
    public OdfSpreadsheetSettings SpreadsheetSettings = new OdfSpreadsheetSettings();
    internal SpreadsheetSettingsState? SpreadsheetSettingsState;
    /// <summary>
    /// The table formatting templates available to document tables.
    /// </summary>
    public OdfTableTemplate[] TableTemplates = [];
    /// <summary>
    /// The spreadsheet named ranges and named formula expressions.
    /// </summary>
    public OdfNamedExpression[] NamedExpressions = [];
    /// <summary>
    /// The spreadsheet row-label and column-label ranges.
    /// </summary>
    public OdfLabelRange[] LabelRanges = [];
    /// <summary>
    /// The spreadsheet database ranges and their sort, filter, and subtotal rules.
    /// </summary>
    public OdfDatabaseRange[] DatabaseRanges = [];
    /// <summary>
    /// The spreadsheet consolidation declarations.
    /// </summary>
    public OdfConsolidation[] Consolidations = [];
    /// <summary>
    /// The spreadsheet scenarios.
    /// </summary>
    public OdfScenario[] Scenarios = [];
    internal SpreadsheetDataState? SpreadsheetState;
    /// <summary>
    /// The document data-pilot or pivot tables.
    /// </summary>
    public OdfPivotTable[] PivotTables = [];
    internal PivotTableState? PivotTableState;
    /// <summary>
    /// The highlighted spreadsheet detective ranges.
    /// </summary>
    public OdfDetectiveRange[] DetectiveRanges = [];
    /// <summary>
    /// The spreadsheet detective trace operations.
    /// </summary>
    public OdfDetectiveOperation[] DetectiveOperations = [];
    /// <summary>
    /// Whether spreadsheet change tracking is enabled.
    /// </summary>
    public bool TrackSpreadsheetChanges;
    /// <summary>
    /// The tracked spreadsheet changes.
    /// </summary>
    public OdfSpreadsheetChange[] SpreadsheetChanges = [];
    internal SpreadsheetChangeState? SpreadsheetChangeState;
    /// <summary>
    /// The form controls.
    /// </summary>
    public OdfFormControl[] FormControls = [];
    internal FormDocumentState? FormState;
    /// <summary>
    /// The inert XForms models embedded in the document.
    /// </summary>
    public OdfXFormsModel[] XFormsModels = [];
    internal XFormsDocumentState? XFormsState;
    /// <summary>
    /// The inert database front-end document model, when present.
    /// </summary>
    public OdfDatabaseDocument? Database;
    internal DatabaseDocumentState? DatabaseState;
    /// <summary>
    /// All top-level application setting trees in document order.
    /// </summary>
    public OdfSetting[] ApplicationSettings = [];
    /// <summary>
    /// Application settings that affect portable document behavior.
    /// </summary>
    public OdfSetting[] PortableSettings = [];
    /// <summary>
    /// Application settings that affect only the document view.
    /// </summary>
    public OdfSetting[] ViewSettings = [];
    internal SettingDocumentState? SettingState;
    /// <summary>
    /// Embedded and package-resident scripts retained for inert inspection.
    /// </summary>
    public OdfScript[] Scripts = [];
    /// <summary>
    /// Script and presentation event listeners throughout the document.
    /// </summary>
    public OdfEventListener[] EventListeners = [];
    internal ScriptDocumentState? ScriptState;
    /// <summary>
    /// The edit reports retained by an operation.
    /// </summary>
    public OdfEditResult[] EditResults = [];
    internal TextFlowDocumentState? TextFlowState;
    internal TextListDocumentState? TextListState;
}
