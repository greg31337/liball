namespace svg;

/// <summary>Identifies a document-processing job implementation.</summary>
public enum SvgProcessJobKind
{
    /// <summary>A caller-supplied processing callback.</summary>
    Custom = 0,
    /// <summary>Removal of non-semantic comments.</summary>
    RemoveComments = 1,
    /// <summary>Removal of recognized editor metadata.</summary>
    RemoveEditorMetadata = 2,
    /// <summary>Removal of unused namespace declarations.</summary>
    RemoveUnusedNamespaces = 3,
    /// <summary>Removal of safe default and redundant attributes.</summary>
    RemoveDefaultAttributes = 4,
    /// <summary>Removal of semantically empty containers.</summary>
    RemoveEmptyContainers = 5,
    /// <summary>Removal of unreferenced invisible graphics.</summary>
    RemoveUnreferencedInvisibleNodes = 6,
    /// <summary>Removal of unreachable definition resources.</summary>
    RemoveUnusedDefinitions = 7,
    /// <summary>Deduplication of equivalent definition resources.</summary>
    DeduplicateDefinitions = 8,
    /// <summary>Collapse of structurally neutral groups and transform lists.</summary>
    CollapseGroups = 9,
    /// <summary>Safe hoisting and sinking of inherited presentation attributes.</summary>
    HoistCommonAttributes = 10,
    /// <summary>Merging of adjacent compatible path elements.</summary>
    MergeAdjacentPaths = 11,
    /// <summary>Cleanup and consolidation of static authored styles.</summary>
    CleanupStyles = 12,
    /// <summary>Inlining of matched static stylesheet declarations.</summary>
    InlineStyles = 13,
    /// <summary>Resolution of stable custom-property references.</summary>
    ResolveCustomProperties = 14,
    /// <summary>Canonical and compact formatting of typed SVG values.</summary>
    CanonicalizeValues = 15,
    /// <summary>Collision-safe prefixing of identifiers and their references.</summary>
    PrefixIds = 16,
    /// <summary>Deterministic minification of identifiers and their references.</summary>
    MinifyIds = 17,
    /// <summary>Conversion of basic shape elements into path elements.</summary>
    ConvertShapesToPaths = 18,
    /// <summary>Baking of authored transforms into path coordinates.</summary>
    BakeTransforms = 19,
    /// <summary>Bounded path simplification and coordinate rounding.</summary>
    SimplifyPaths = 20,
    /// <summary>Replacement of text elements with caller-resolved outlines.</summary>
    ConvertTextToPaths = 21,
    /// <summary>Replacement of effect-bearing elements with flattened imagery.</summary>
    FlattenEffects = 22,
    /// <summary>Embedding of caller-resolved external image bytes.</summary>
    EmbedImages = 23,
    /// <summary>Document-wide replacement of authored literal colors.</summary>
    Recolor = 24,
    /// <summary>Conversion of literal colors to theme-controlled values.</summary>
    ConvertThemeColors = 25,
    /// <summary>Resize, fit, or crop of the root viewport and view box.</summary>
    ResizeViewBox = 26,
    /// <summary>Affine conversion of the root content coordinate system.</summary>
    ConvertCoordinateSystem = 27,
    /// <summary>Cropping of the root view box to resolved drawing bounds.</summary>
    CropDrawingBounds = 28,
    /// <summary>Expansion of use elements and inherited resource references.</summary>
    FlattenReferences = 29,
    /// <summary>Extraction of one identified fragment as root content.</summary>
    ExtractFragment = 30,
    /// <summary>Splitting of symbol elements into independent documents.</summary>
    SplitSymbols = 31,
    /// <summary>Merging of caller-supplied documents with isolated identifiers.</summary>
    MergeDocuments = 32,
    /// <summary>Construction of isolated symbol content for sprite use.</summary>
    BuildSprite = 33,
    /// <summary>Isolation of a document identifier namespace.</summary>
    IsolateNamespace = 34
}

/// <summary>Defines stable document-processing failures.</summary>
public enum SvgProcessError
{
    /// <summary>Processing completed without an operational failure.</summary>
    None = 0,
    /// <summary>The document or processing options are invalid.</summary>
    InvalidArgument = 1,
    /// <summary>Job identifiers or prerequisite relationships are invalid.</summary>
    InvalidJobGraph = 2,
    /// <summary>A processing callback explicitly reported failure.</summary>
    JobFailed = 3,
    /// <summary>The configured change bound was exceeded.</summary>
    ChangeLimitExceeded = 4,
    /// <summary>The configured pass bound was reached before convergence.</summary>
    ConvergenceLimitExceeded = 5,
    /// <summary>A callback changed the document without a recorded mutation.</summary>
    UntrackedMutation = 6,
    /// <summary>A job permits semantic loss that the selected process policy forbids.</summary>
    SemanticLossDenied = 7,
    /// <summary>A job does not preserve accessibility or viewBox content required by policy.</summary>
    PreservationRequired = 8,
    /// <summary>Processing was cancelled by the caller.</summary>
    Cancelled = 9
}
