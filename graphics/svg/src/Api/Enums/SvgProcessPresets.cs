namespace svg;

/// <summary>Identifies a named document-processing preset.</summary>
public enum SvgProcessPresetKind
{
    /// <summary>Caller-configured processing without a named preset.</summary>
    Custom = 0,
    /// <summary>Conservative semantics-preserving cleanup.</summary>
    SafeDefault = 1,
    /// <summary>Safe cleanup followed by deterministic value canonicalization.</summary>
    Canonicalize = 2,
    /// <summary>Size-oriented processing that permits explicitly logged semantic loss.</summary>
    Aggressive = 3,
    /// <summary>Safe preparation for isolated reusable sprite symbols.</summary>
    Sprite = 4,
    /// <summary>Secure-static sanitization and conservative cleanup.</summary>
    Sanitize = 5
}

/// <summary>Identifies a declarative operation selected by a process preset.</summary>
public enum SvgProcessOperation
{
    /// <summary>Apply secure-static sanitization.</summary>
    Sanitize = 0,
    /// <summary>Remove comments that have no semantic role.</summary>
    RemoveComments = 1,
    /// <summary>Remove recognized editor metadata.</summary>
    RemoveEditorMetadata = 2,
    /// <summary>Remove namespace declarations unused by retained content.</summary>
    RemoveUnusedNamespaces = 3,
    /// <summary>Remove attributes equivalent to their defaults.</summary>
    RemoveDefaultAttributes = 4,
    /// <summary>Remove semantically empty containers.</summary>
    RemoveEmptyContainers = 5,
    /// <summary>Remove invisible nodes that have no references.</summary>
    RemoveUnreferencedInvisibleNodes = 6,
    /// <summary>Remove unreferenced definition resources.</summary>
    RemoveUnusedDefinitions = 7,
    /// <summary>Deduplicate equivalent definition resources.</summary>
    DeduplicateDefinitions = 8,
    /// <summary>Collapse groups when inherited, stacking, and effect semantics permit.</summary>
    CollapseGroups = 9,
    /// <summary>Hoist or sink common attributes when semantics permit.</summary>
    HoistCommonAttributes = 10,
    /// <summary>Merge adjacent compatible paths.</summary>
    MergeAdjacentPaths = 11,
    /// <summary>Remove redundant declarations and combine equivalent style rules.</summary>
    CleanupStyles = 12,
    /// <summary>Inline matched style declarations.</summary>
    InlineStyles = 13,
    /// <summary>Resolve custom properties where their computed values are stable.</summary>
    ResolveCustomProperties = 14,
    /// <summary>Canonicalize numbers, colors, lists, paths, and transforms.</summary>
    CanonicalizeValues = 15,
    /// <summary>Prefix identifiers and rewrite every associated reference.</summary>
    PrefixIds = 16,
    /// <summary>Minify identifiers and rewrite every associated reference.</summary>
    MinifyIds = 17,
    /// <summary>Convert supported shapes into path data.</summary>
    ConvertShapesToPaths = 18,
    /// <summary>Bake transforms into supported geometry.</summary>
    BakeTransforms = 19,
    /// <summary>Simplify or round path geometry within configured error bounds.</summary>
    SimplifyPaths = 20,
    /// <summary>Convert text into resolved glyph paths.</summary>
    ConvertTextToPaths = 21,
    /// <summary>Flatten effects into replacement imagery or geometry.</summary>
    FlattenEffects = 22,
    /// <summary>Embed caller-resolved external images.</summary>
    EmbedImages = 23,
    /// <summary>Apply document recoloring or theme conversion.</summary>
    Recolor = 24,
    /// <summary>Crop or fit document coordinates while retaining viewBox semantics.</summary>
    CropViewBox = 25,
    /// <summary>Flatten use and other reusable references.</summary>
    FlattenUses = 26,
    /// <summary>Build isolated reusable sprite symbols.</summary>
    BuildSprite = 27
}
