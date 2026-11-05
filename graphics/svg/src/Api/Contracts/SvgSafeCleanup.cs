namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgProcessJobs"/>.</summary>
public interface ISvgProcessJobs
{
    /// <summary>Creates a job that removes non-semantic comments.</summary>
    /// <returns>A new independent cleanup job.</returns>
    SvgProcessJob RemoveComments();
    /// <summary>Creates a job that removes recognized editor metadata.</summary>
    /// <returns>A new independent cleanup job.</returns>
    SvgProcessJob RemoveEditorMetadata();
    /// <summary>Creates a job that removes unused and redundant namespace declarations.</summary>
    /// <returns>A new independent cleanup job.</returns>
    SvgProcessJob RemoveUnusedNamespaces();
    /// <summary>Creates a job that removes safe default and redundant attributes.</summary>
    /// <returns>A new independent cleanup job.</returns>
    SvgProcessJob RemoveDefaultAttributes();
    /// <summary>Creates a job that removes semantically empty containers.</summary>
    /// <returns>A new independent cleanup job.</returns>
    SvgProcessJob RemoveEmptyContainers();
    /// <summary>Creates a job that removes unreferenced invisible graphics.</summary>
    /// <returns>A new independent cleanup job.</returns>
    SvgProcessJob RemoveUnreferencedInvisibleNodes();
    /// <summary>Creates a job that removes unreachable definition resources.</summary>
    /// <returns>A new independent cleanup job.</returns>
    SvgProcessJob RemoveUnusedDefinitions();
    /// <summary>Creates a job that deduplicates equivalent definition resources.</summary>
    /// <returns>A new independent cleanup job.</returns>
    SvgProcessJob DeduplicateDefinitions();
    /// <summary>Creates a job that collapses neutral groups and combines transform lists.</summary>
    /// <returns>A new independent cleanup job.</returns>
    SvgProcessJob CollapseGroups();
    /// <summary>Creates a job that safely hoists and sinks inherited attributes.</summary>
    /// <returns>A new independent cleanup job.</returns>
    SvgProcessJob HoistCommonAttributes();
    /// <summary>Creates a job that merges adjacent compatible path elements.</summary>
    /// <returns>A new independent cleanup job.</returns>
    SvgProcessJob MergeAdjacentPaths();
    /// <summary>Creates a job that cleans and consolidates static authored styles.</summary>
    /// <returns>A new independent cleanup job.</returns>
    SvgProcessJob CleanupStyles();
    /// <summary>Creates a job that inlines matched static stylesheet declarations.</summary>
    /// <returns>A new independent cleanup job.</returns>
    SvgProcessJob InlineStyles();
    /// <summary>Creates a job that resolves stable custom-property references.</summary>
    /// <returns>A new independent cleanup job.</returns>
    SvgProcessJob ResolveCustomProperties();
    /// <summary>Creates a job that canonically formats supported typed SVG values.</summary>
    /// <returns>A new independent canonicalization job.</returns>
    SvgProcessJob CanonicalizeValuesWithNoArguments();
    /// <summary>Creates a job that formats supported typed SVG values with explicit family settings.</summary>
    /// <param name="options">The independently configured value-family settings.</param>
    /// <returns>A new independent canonicalization job.</returns>
    SvgProcessJob CanonicalizeValuesWithOptions(SvgCanonicalizeOptions options);
    /// <summary>Creates a job that prefixes identifiers with a stable default prefix.</summary>
    /// <returns>A new independent identifier-prefixing job.</returns>
    SvgProcessJob PrefixIdsWithNoArguments();
    /// <summary>Creates a job that prefixes identifiers and rewrites every supported reference.</summary>
    /// <param name="options">The prefix, bounds, and additional IDREF attributes.</param>
    /// <returns>A new independent identifier-prefixing job.</returns>
    SvgProcessJob PrefixIdsWithOptions(SvgIdPrefixOptions options);
    /// <summary>Creates a job that assigns deterministic compact identifiers.</summary>
    /// <returns>A new independent identifier-minification job.</returns>
    SvgProcessJob MinifyIdsWithNoArguments();
    /// <summary>Creates a job that assigns deterministic compact identifiers with explicit bounds.</summary>
    /// <param name="options">The bounds and additional IDREF attributes.</param>
    /// <returns>A new independent identifier-minification job.</returns>
    SvgProcessJob MinifyIdsWithOptions(SvgIdMinifyOptions options);
    /// <summary>Creates a lossy job that converts supported basic shapes to path elements.</summary>
    /// <returns>A bounded shape-conversion job.</returns>
    SvgProcessJob ConvertShapesToPathsWithNoArguments();
    /// <summary>Creates a lossy shape-conversion job with explicit geometry bounds.</summary>
    /// <param name="options">The resolution and output bounds.</param>
    /// <returns>A bounded shape-conversion job.</returns>
    SvgProcessJob ConvertShapesToPathsWithOptions(SvgConvertShapesToPathsOptions options);
    /// <summary>Creates a lossy job that bakes local transforms into path coordinates.</summary>
    /// <returns>A bounded transform-baking job.</returns>
    SvgProcessJob BakeTransformsWithNoArguments();
    /// <summary>Creates a transform-baking job with explicit arc and command settings.</summary>
    /// <param name="options">The transform output settings.</param>
    /// <returns>A bounded transform-baking job.</returns>
    SvgProcessJob BakeTransformsWithOptions(SvgBakeTransformsOptions options);
    /// <summary>Creates a bounded lossy path-simplification and rounding job.</summary>
    /// <returns>A path-simplification job.</returns>
    SvgProcessJob SimplifyPathsWithNoArguments();
    /// <summary>Creates a bounded path-simplification job with explicit error settings.</summary>
    /// <param name="options">The simplification, rounding, and work bounds.</param>
    /// <returns>A path-simplification job.</returns>
    SvgProcessJob SimplifyPathsWithOptions(SvgPathSimplifyOptions options);
    /// <summary>Creates a caller-mediated text-outline conversion job.</summary>
    /// <returns>A text conversion job that waits for an outline provider.</returns>
    SvgProcessJob ConvertTextToPathsWithNoArguments();
    /// <summary>Creates a caller-mediated text-outline conversion job.</summary>
    /// <param name="options">The outline provider and output bound.</param>
    /// <returns>A bounded text conversion job.</returns>
    SvgProcessJob ConvertTextToPathsWithOptions(SvgTextToPathProcessOptions options);
    /// <summary>Creates a caller-mediated effect-flattening job.</summary>
    /// <returns>An effect job that waits for flattened imagery.</returns>
    SvgProcessJob FlattenEffectsWithNoArguments();
    /// <summary>Creates a caller-mediated effect-flattening job.</summary>
    /// <param name="options">The flattened-image provider and node bound.</param>
    /// <returns>A bounded effect-flattening job.</returns>
    SvgProcessJob FlattenEffectsWithOptions(SvgFlattenEffectsProcessOptions options);
    /// <summary>Creates a caller-mediated external-image embedding job.</summary>
    /// <returns>An image embedding job that waits for resolved bytes.</returns>
    SvgProcessJob EmbedImagesWithNoArguments();
    /// <summary>Creates a caller-mediated external-image embedding job.</summary>
    /// <param name="options">The byte provider and encoded-size bound.</param>
    /// <returns>A bounded image embedding job.</returns>
    SvgProcessJob EmbedImagesWithOptions(SvgEmbedImagesProcessOptions options);
    /// <summary>Creates a caller-mediated document recolor job.</summary>
    /// <returns>A bounded recolor job that waits for a color provider.</returns>
    SvgProcessJob RecolorWithNoArguments();
    /// <summary>Creates a caller-mediated document recolor job.</summary>
    /// <param name="options">The color provider, scopes, and work bounds.</param>
    /// <returns>A bounded document recolor job.</returns>
    SvgProcessJob RecolorWithOptions(SvgRecolorProcessOptions options);
    /// <summary>Creates a job that converts literal colors to <c>currentColor</c>.</summary>
    /// <returns>A bounded theme conversion job.</returns>
    SvgProcessJob ConvertColorsToCurrentColor();
    /// <summary>Creates a job that converts literal colors to a CSS custom property.</summary>
    /// <param name="customPropertyName">The custom-property name including leading hyphens.</param>
    /// <returns>A bounded theme conversion job retaining authored fallbacks.</returns>
    SvgProcessJob ConvertColorsToCustomProperty(string customPropertyName);
    /// <summary>Creates a literal-color theme conversion job.</summary>
    /// <param name="options">The source color, theme mechanism, scopes, and work bounds.</param>
    /// <returns>A bounded theme conversion job.</returns>
    SvgProcessJob ConvertThemeColors(SvgThemeColorProcessOptions options);
    /// <summary>Creates an inert view-box job until explicit resize settings are supplied.</summary>
    /// <returns>A bounded view-box job.</returns>
    SvgProcessJob ResizeViewBoxWithNoArguments();
    /// <summary>Creates a root viewport resize, fit, or crop job.</summary>
    /// <param name="options">The viewport and view-box operation settings.</param>
    /// <returns>A bounded root geometry job.</returns>
    SvgProcessJob ResizeViewBoxWithOptions(SvgViewBoxResizeOptions options);
    /// <summary>Creates an inert coordinate conversion job with an identity transform.</summary>
    /// <returns>A bounded coordinate-system job.</returns>
    SvgProcessJob ConvertCoordinateSystemWithNoArguments();
    /// <summary>Creates an affine root coordinate-system conversion job.</summary>
    /// <param name="options">The affine transform, target view box, and child bound.</param>
    /// <returns>A bounded coordinate-system job.</returns>
    SvgProcessJob ConvertCoordinateSystemWithOptions(SvgCoordinateSystemProcessOptions options);
    /// <summary>Creates a root view-box crop job using resolved drawing bounds.</summary>
    /// <returns>A bounded drawing-bounds crop job.</returns>
    SvgProcessJob CropToDrawingBoundsWithNoArguments();
    /// <summary>Creates a root view-box crop job using resolved drawing bounds.</summary>
    /// <param name="options">The resolution, bounds, padding, and viewport settings.</param>
    /// <returns>A bounded drawing-bounds crop job.</returns>
    SvgProcessJob CropToDrawingBoundsWithOptions(SvgDrawingBoundsCropOptions options);
    /// <summary>Creates a bounded use and resource-reference flattening job.</summary>
    /// <returns>A deterministic local-reference flattening job.</returns>
    SvgProcessJob FlattenReferencesWithNoArguments();
    /// <summary>Creates a bounded use and resource-reference flattening job.</summary>
    /// <param name="options">The use resolution, namespace, and traversal settings.</param>
    /// <returns>A deterministic local-reference flattening job.</returns>
    SvgProcessJob FlattenReferencesWithOptions(SvgFlattenReferencesProcessOptions options);
    /// <summary>Creates an inert fragment extraction job until an identifier is supplied.</summary>
    /// <returns>A bounded fragment extraction job.</returns>
    SvgProcessJob ExtractFragmentWithNoArguments();
    /// <summary>Creates a bounded fragment extraction job.</summary>
    /// <param name="options">The fragment, resource-retention, namespace, and traversal settings.</param>
    /// <returns>A deterministic fragment extraction job.</returns>
    SvgProcessJob ExtractFragmentWithOptions(SvgFragmentExtractionProcessOptions options);
    /// <summary>Creates a caller-output symbol splitting job.</summary>
    /// <returns>A bounded symbol splitting job.</returns>
    SvgProcessJob SplitSymbolsWithNoArguments();
    /// <summary>Creates a caller-output symbol splitting job.</summary>
    /// <param name="options">The output collection, namespace, and symbol bound.</param>
    /// <returns>A bounded symbol splitting job.</returns>
    SvgProcessJob SplitSymbolsWithOptions(SvgSymbolSplitProcessOptions options);
    /// <summary>Creates an empty deterministic multi-document merge job.</summary>
    /// <returns>A bounded merge job.</returns>
    SvgProcessJob MergeDocumentsWithNoArguments();
    /// <summary>Creates a deterministic multi-document merge job.</summary>
    /// <param name="options">The ordered inputs, namespace, and work bounds.</param>
    /// <returns>A collision-safe merge job.</returns>
    SvgProcessJob MergeDocumentsWithOptions(SvgDocumentMergeProcessOptions options);
    /// <summary>Creates a one-document sprite construction job.</summary>
    /// <returns>A deterministic sprite construction job.</returns>
    SvgProcessJob BuildSpriteWithNoArguments();
    /// <summary>Creates a deterministic multi-document sprite construction job.</summary>
    /// <param name="options">The current/imported inputs, symbol prefix, and work bounds.</param>
    /// <returns>A collision-safe sprite construction job.</returns>
    SvgProcessJob BuildSpriteWithOptions(SvgSpriteProcessOptions options);
    /// <summary>Creates a complete identifier namespace isolation job.</summary>
    /// <returns>A deterministic namespace isolation job.</returns>
    SvgProcessJob IsolateNamespaceWithNoArguments();
    /// <summary>Creates a complete identifier namespace isolation job.</summary>
    /// <param name="options">The complete identifier-prefixing settings.</param>
    /// <returns>A deterministic namespace isolation job.</returns>
    SvgProcessJob IsolateNamespaceWithOptions(SvgNamespaceIsolationProcessOptions options);
    /// <summary>Creates an implemented built-in job for one declarative operation.</summary>
    /// <param name="operation">The requested operation.</param>
    /// <returns>A new job, or <c>null</c> when a later task owns the implementation.</returns>
    SvgProcessJob? Create(SvgProcessOperation operation);
}
