namespace svg;

/// <summary>Creates built-in document-processing jobs as their implementations become available.</summary>
public class SvgProcessJobs : ISvgProcessJobs
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgProcessJobs()
    {
    }

    /// <summary>Creates a job that removes non-semantic comments.</summary>
    /// <returns>A new independent cleanup job.</returns>
    public SvgProcessJob RemoveComments()
    {
        return new SvgProcessJobs().Job("remove-comments", SvgProcessJobKind.RemoveComments, "Remove a non-semantic comment.", context => SvgSafeCleanup.RemoveComments(context));
    }

    /// <summary>Creates a job that removes recognized editor metadata.</summary>
    /// <returns>A new independent cleanup job.</returns>
    public SvgProcessJob RemoveEditorMetadata()
    {
        return new SvgProcessJobs().Job("remove-editor-metadata", SvgProcessJobKind.RemoveEditorMetadata, "Remove recognized editor metadata.", context => SvgSafeCleanup.RemoveEditorMetadata(context));
    }

    /// <summary>Creates a job that removes unused and redundant namespace declarations.</summary>
    /// <returns>A new independent cleanup job.</returns>
    public SvgProcessJob RemoveUnusedNamespaces()
    {
        return new SvgProcessJobs().Job("remove-unused-namespaces", SvgProcessJobKind.RemoveUnusedNamespaces, "Remove an unused namespace declaration.", context => SvgSafeCleanup.RemoveUnusedNamespaces(context));
    }

    /// <summary>Creates a job that removes safe default and redundant attributes.</summary>
    /// <returns>A new independent cleanup job.</returns>
    public SvgProcessJob RemoveDefaultAttributes()
    {
        return new SvgProcessJobs().Job("remove-default-attributes", SvgProcessJobKind.RemoveDefaultAttributes, "Remove a redundant attribute value.", context => SvgSafeCleanup.RemoveDefaultAttributes(context));
    }

    /// <summary>Creates a job that removes semantically empty containers.</summary>
    /// <returns>A new independent cleanup job.</returns>
    public SvgProcessJob RemoveEmptyContainers()
    {
        return new SvgProcessJobs().Job("remove-empty-containers", SvgProcessJobKind.RemoveEmptyContainers, "Remove a semantically empty container.", context => SvgSafeCleanup.RemoveEmptyContainers(context));
    }

    /// <summary>Creates a job that removes unreferenced invisible graphics.</summary>
    /// <returns>A new independent cleanup job.</returns>
    public SvgProcessJob RemoveUnreferencedInvisibleNodes()
    {
        return new SvgProcessJobs().Job("remove-unreferenced-invisible", SvgProcessJobKind.RemoveUnreferencedInvisibleNodes, "Remove an unreferenced invisible node.", context => SvgSafeCleanup.RemoveUnreferencedInvisibleNodes(context));
    }

    /// <summary>Creates a job that removes unreachable definition resources.</summary>
    /// <returns>A new independent cleanup job.</returns>
    public SvgProcessJob RemoveUnusedDefinitions()
    {
        return new SvgProcessJobs().Job("remove-unused-definitions", SvgProcessJobKind.RemoveUnusedDefinitions, "Remove an unreferenced definition.", context => SvgDefinitionCleanup.RemoveUnusedDefinitions(context));
    }

    /// <summary>Creates a job that deduplicates equivalent definition resources.</summary>
    /// <returns>A new independent cleanup job.</returns>
    public SvgProcessJob DeduplicateDefinitions()
    {
        return new SvgProcessJobs().Job("deduplicate-definitions", SvgProcessJobKind.DeduplicateDefinitions, "Deduplicate an equivalent definition and rewrite its references.", context => SvgDefinitionCleanup.DeduplicateDefinitions(context));
    }

    /// <summary>Creates a job that collapses neutral groups and combines transform lists.</summary>
    /// <returns>A new independent cleanup job.</returns>
    public SvgProcessJob CollapseGroups()
    {
        return new SvgProcessJobs().Job("collapse-groups", SvgProcessJobKind.CollapseGroups, "Collapse a structurally neutral group or combine equivalent transforms.", context => SvgStructuralCleanup.CollapseGroups(context));
    }

    /// <summary>Creates a job that safely hoists and sinks inherited attributes.</summary>
    /// <returns>A new independent cleanup job.</returns>
    public SvgProcessJob HoistCommonAttributes()
    {
        return new SvgProcessJobs().Job("hoist-common-attributes", SvgProcessJobKind.HoistCommonAttributes, "Move a context-stable inherited attribute without changing computed values.", context => SvgStructuralCleanup.HoistCommonAttributes(context));
    }

    /// <summary>Creates a job that merges adjacent compatible path elements.</summary>
    /// <returns>A new independent cleanup job.</returns>
    public SvgProcessJob MergeAdjacentPaths()
    {
        return new SvgProcessJobs().Job("merge-adjacent-paths", SvgProcessJobKind.MergeAdjacentPaths, "Merge adjacent paths with equivalent paint and disjoint geometry.", context => SvgStructuralCleanup.MergeAdjacentPaths(context));
    }

    /// <summary>Creates a job that cleans and consolidates static authored styles.</summary>
    /// <returns>A new independent cleanup job.</returns>
    public SvgProcessJob CleanupStyles()
    {
        return new SvgProcessJobs().Job("cleanup-styles", SvgProcessJobKind.CleanupStyles, "Clean static declarations, presentation attributes, rules, and selectors.", context => SvgStyleCleanup.CleanupStyles(context));
    }

    /// <summary>Creates a job that inlines matched static stylesheet declarations.</summary>
    /// <returns>A new independent cleanup job.</returns>
    public SvgProcessJob InlineStyles()
    {
        return new SvgProcessJobs().Job("inline-styles", SvgProcessJobKind.InlineStyles, "Inline matched static stylesheet declarations without changing the cascade.", context => SvgStyleCleanup.InlineStyles(context));
    }

    /// <summary>Creates a job that resolves stable custom-property references.</summary>
    /// <returns>A new independent cleanup job.</returns>
    public SvgProcessJob ResolveCustomProperties()
    {
        return new SvgProcessJobs().Job("resolve-custom-properties", SvgProcessJobKind.ResolveCustomProperties, "Resolve a stable custom-property reference at its authored use site.", context => SvgStyleCleanup.ResolveCustomProperties(context));
    }

    /// <summary>Creates a job that canonically formats supported typed SVG values.</summary>
    /// <returns>A new independent canonicalization job.</returns>
    public SvgProcessJob CanonicalizeValuesWithNoArguments()
    {
        return new SvgProcessJobs().CanonicalizeValuesWithOptions(new SvgCanonicalizeOptions());
    }

    /// <summary>Creates a job that formats supported typed SVG values with explicit family settings.</summary>
    /// <param name="options">The independently configured value-family settings.</param>
    /// <returns>A new independent canonicalization job.</returns>
    public SvgProcessJob CanonicalizeValuesWithOptions(SvgCanonicalizeOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        SvgCanonicalizeOptions copy = options.Copy();
        return new SvgProcessJobs().Job("canonicalize-values", SvgProcessJobKind.CanonicalizeValues, "Canonicalize a typed SVG value.", context => SvgValueCanonicalizer.Run(context, copy));
    }

    /// <summary>Creates a job that prefixes identifiers with a stable default prefix.</summary>
    /// <returns>A new independent identifier-prefixing job.</returns>
    public SvgProcessJob PrefixIdsWithNoArguments()
    {
        return new SvgProcessJobs().PrefixIdsWithOptions(new SvgIdPrefixOptions());
    }

    /// <summary>Creates a job that prefixes identifiers and rewrites every supported reference.</summary>
    /// <param name="options">The prefix, bounds, and additional IDREF attributes.</param>
    /// <returns>A new independent identifier-prefixing job.</returns>
    public SvgProcessJob PrefixIdsWithOptions(SvgIdPrefixOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        SvgIdPrefixOptions copy = options.Copy();
        return new SvgProcessJobs().Job("prefix-ids", SvgProcessJobKind.PrefixIds, "Prefix an identifier and rewrite its local references.", context => SvgIdRewriter.Prefix(context, copy));
    }

    /// <summary>Creates a job that assigns deterministic compact identifiers.</summary>
    /// <returns>A new independent identifier-minification job.</returns>
    public SvgProcessJob MinifyIdsWithNoArguments()
    {
        return new SvgProcessJobs().MinifyIdsWithOptions(new SvgIdMinifyOptions());
    }

    /// <summary>Creates a job that assigns deterministic compact identifiers with explicit bounds.</summary>
    /// <param name="options">The bounds and additional IDREF attributes.</param>
    /// <returns>A new independent identifier-minification job.</returns>
    public SvgProcessJob MinifyIdsWithOptions(SvgIdMinifyOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        SvgIdMinifyOptions copy = options.Copy();
        return new SvgProcessJobs().Job("minify-ids", SvgProcessJobKind.MinifyIds, "Minify an identifier and rewrite its local references.", context => SvgIdRewriter.Minify(context, copy));
    }

    /// <summary>Creates a lossy job that converts supported basic shapes to path elements.</summary>
    /// <returns>A bounded shape-conversion job.</returns>
    public SvgProcessJob ConvertShapesToPathsWithNoArguments()
    {
        return new SvgProcessJobs().ConvertShapesToPathsWithOptions(new SvgConvertShapesToPathsOptions());
    }

    /// <summary>Creates a lossy shape-conversion job with explicit geometry bounds.</summary>
    /// <param name="options">The resolution and output bounds.</param>
    /// <returns>A bounded shape-conversion job.</returns>
    public SvgProcessJob ConvertShapesToPathsWithOptions(SvgConvertShapesToPathsOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        return new SvgProcessJobs().LossyJob("convert-shapes-to-paths", SvgProcessJobKind.ConvertShapesToPaths, SvgProcessChangeSeverity.Representation, "Convert a basic shape to equivalent path geometry.", context => SvgLossyProcessJobs.ConvertShapes(context, options));
    }

    /// <summary>Creates a lossy job that bakes local transforms into path coordinates.</summary>
    /// <returns>A bounded transform-baking job.</returns>
    public SvgProcessJob BakeTransformsWithNoArguments()
    {
        return new SvgProcessJobs().BakeTransformsWithOptions(new SvgBakeTransformsOptions());
    }

    /// <summary>Creates a transform-baking job with explicit arc and command settings.</summary>
    /// <param name="options">The transform output settings.</param>
    /// <returns>A bounded transform-baking job.</returns>
    public SvgProcessJob BakeTransformsWithOptions(SvgBakeTransformsOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        return new SvgProcessJobs().LossyJob("bake-transforms", SvgProcessJobKind.BakeTransforms, SvgProcessChangeSeverity.Approximation, "Bake a local transform into path coordinates.", context => SvgLossyProcessJobs.BakeTransforms(context, options));
    }

    /// <summary>Creates a bounded lossy path-simplification and rounding job.</summary>
    /// <returns>A path-simplification job.</returns>
    public SvgProcessJob SimplifyPathsWithNoArguments()
    {
        return new SvgProcessJobs().SimplifyPathsWithOptions(new SvgPathSimplifyOptions());
    }

    /// <summary>Creates a bounded path-simplification job with explicit error settings.</summary>
    /// <param name="options">The simplification, rounding, and work bounds.</param>
    /// <returns>A path-simplification job.</returns>
    public SvgProcessJob SimplifyPathsWithOptions(SvgPathSimplifyOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        return new SvgProcessJobs().LossyJob("simplify-paths", SvgProcessJobKind.SimplifyPaths, SvgProcessChangeSeverity.Approximation, "Simplify and round path geometry within the configured error bound.", context => SvgLossyProcessJobs.SimplifyPaths(context, options));
    }

    /// <summary>Creates a caller-mediated text-outline conversion job.</summary>
    /// <returns>A text conversion job that waits for an outline provider.</returns>
    public SvgProcessJob ConvertTextToPathsWithNoArguments()
    {
        return new SvgProcessJobs().ConvertTextToPathsWithOptions(new SvgTextToPathProcessOptions());
    }

    /// <summary>Creates a caller-mediated text-outline conversion job.</summary>
    /// <param name="options">The outline provider and output bound.</param>
    /// <returns>A bounded text conversion job.</returns>
    public SvgProcessJob ConvertTextToPathsWithOptions(SvgTextToPathProcessOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        return new SvgProcessJobs().LossyJob("convert-text-to-paths", SvgProcessJobKind.ConvertTextToPaths, SvgProcessChangeSeverity.Destructive, "Replace authored text with caller-resolved outline geometry.", context => SvgLossyProcessJobs.ConvertText(context, options));
    }

    /// <summary>Creates a caller-mediated effect-flattening job.</summary>
    /// <returns>An effect job that waits for flattened imagery.</returns>
    public SvgProcessJob FlattenEffectsWithNoArguments()
    {
        return new SvgProcessJobs().FlattenEffectsWithOptions(new SvgFlattenEffectsProcessOptions());
    }

    /// <summary>Creates a caller-mediated effect-flattening job.</summary>
    /// <param name="options">The flattened-image provider and node bound.</param>
    /// <returns>A bounded effect-flattening job.</returns>
    public SvgProcessJob FlattenEffectsWithOptions(SvgFlattenEffectsProcessOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        return new SvgProcessJobs().LossyJob("flatten-effects", SvgProcessJobKind.FlattenEffects, SvgProcessChangeSeverity.Destructive, "Replace effect-bearing authored content with flattened imagery.", context => SvgLossyProcessJobs.FlattenEffects(context, options));
    }

    /// <summary>Creates a caller-mediated external-image embedding job.</summary>
    /// <returns>An image embedding job that waits for resolved bytes.</returns>
    public SvgProcessJob EmbedImagesWithNoArguments()
    {
        return new SvgProcessJobs().EmbedImagesWithOptions(new SvgEmbedImagesProcessOptions());
    }

    /// <summary>Creates a caller-mediated external-image embedding job.</summary>
    /// <param name="options">The byte provider and encoded-size bound.</param>
    /// <returns>A bounded image embedding job.</returns>
    public SvgProcessJob EmbedImagesWithOptions(SvgEmbedImagesProcessOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        SvgProcessJob result = new SvgProcessJobs().Job("embed-images", SvgProcessJobKind.EmbedImages, "Embed caller-resolved external image bytes as a data URL.", context => SvgLossyProcessJobs.EmbedImages(context, options));
        result.Severity = SvgProcessChangeSeverity.Representation;
        return result;
    }

    /// <summary>Creates a caller-mediated document recolor job.</summary>
    /// <returns>A bounded recolor job that waits for a color provider.</returns>
    public SvgProcessJob RecolorWithNoArguments()
    {
        return new SvgProcessJobs().RecolorWithOptions(new SvgRecolorProcessOptions());
    }

    /// <summary>Creates a caller-mediated document recolor job.</summary>
    /// <param name="options">The color provider, scopes, and work bounds.</param>
    /// <returns>A bounded document recolor job.</returns>
    public SvgProcessJob RecolorWithOptions(SvgRecolorProcessOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        return new SvgProcessJobs().LossyJob("recolor", SvgProcessJobKind.Recolor, SvgProcessChangeSeverity.Destructive, "Replace an authored literal color using the configured document palette mapping.", context => SvgDocumentTransformJobs.Recolor(context, options));
    }

    /// <summary>Creates a job that converts literal colors to <c>currentColor</c>.</summary>
    /// <returns>A bounded theme conversion job.</returns>
    public SvgProcessJob ConvertColorsToCurrentColor()
    {
        return new SvgProcessJobs().ConvertThemeColors(new SvgThemeColorProcessOptions());
    }

    /// <summary>Creates a job that converts literal colors to a CSS custom property.</summary>
    /// <param name="customPropertyName">The custom-property name including leading hyphens.</param>
    /// <returns>A bounded theme conversion job retaining authored fallbacks.</returns>
    public SvgProcessJob ConvertColorsToCustomProperty(string customPropertyName)
    {
        if (customPropertyName == null) throw new System.ArgumentNullException("customPropertyName");
        SvgThemeColorProcessOptions options = new SvgThemeColorProcessOptions();
        options.Mode = SvgThemeColorMode.CustomProperty;
        options.CustomPropertyName = customPropertyName;
        return new SvgProcessJobs().ConvertThemeColors(options);
    }

    /// <summary>Creates a literal-color theme conversion job.</summary>
    /// <param name="options">The source color, theme mechanism, scopes, and work bounds.</param>
    /// <returns>A bounded theme conversion job.</returns>
    public SvgProcessJob ConvertThemeColors(SvgThemeColorProcessOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        return new SvgProcessJobs().LossyJob("convert-theme-colors", SvgProcessJobKind.ConvertThemeColors, SvgProcessChangeSeverity.Representation, "Convert an authored literal color to a theme-controlled value.", context => SvgDocumentTransformJobs.ConvertThemeColors(context, options));
    }

    /// <summary>Creates an inert view-box job until explicit resize settings are supplied.</summary>
    /// <returns>A bounded view-box job.</returns>
    public SvgProcessJob ResizeViewBoxWithNoArguments()
    {
        return new SvgProcessJobs().ResizeViewBoxWithOptions(new SvgViewBoxResizeOptions());
    }

    /// <summary>Creates a root viewport resize, fit, or crop job.</summary>
    /// <param name="options">The viewport and view-box operation settings.</param>
    /// <returns>A bounded root geometry job.</returns>
    public SvgProcessJob ResizeViewBoxWithOptions(SvgViewBoxResizeOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        SvgProcessJob result = new SvgProcessJobs().LossyJob("resize-viewbox", SvgProcessJobKind.ResizeViewBox, SvgProcessChangeSeverity.Destructive, "Resize, fit, or crop root viewport coordinates.", context => SvgDocumentTransformJobs.ResizeViewBox(context, options));
        result.PreservesViewBox = false;
        return result;
    }

    /// <summary>Creates an inert coordinate conversion job with an identity transform.</summary>
    /// <returns>A bounded coordinate-system job.</returns>
    public SvgProcessJob ConvertCoordinateSystemWithNoArguments()
    {
        return new SvgProcessJobs().ConvertCoordinateSystemWithOptions(new SvgCoordinateSystemProcessOptions());
    }

    /// <summary>Creates an affine root coordinate-system conversion job.</summary>
    /// <param name="options">The affine transform, target view box, and child bound.</param>
    /// <returns>A bounded coordinate-system job.</returns>
    public SvgProcessJob ConvertCoordinateSystemWithOptions(SvgCoordinateSystemProcessOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        SvgProcessJob result = new SvgProcessJobs().LossyJob("convert-coordinate-system", SvgProcessJobKind.ConvertCoordinateSystem, SvgProcessChangeSeverity.Destructive, "Convert root graphical content to an affine coordinate system.", context => SvgDocumentTransformJobs.ConvertCoordinateSystem(context, options));
        result.PreservesViewBox = false;
        return result;
    }

    /// <summary>Creates a root view-box crop job using resolved drawing bounds.</summary>
    /// <returns>A bounded drawing-bounds crop job.</returns>
    public SvgProcessJob CropToDrawingBoundsWithNoArguments()
    {
        return new SvgProcessJobs().CropToDrawingBoundsWithOptions(new SvgDrawingBoundsCropOptions());
    }

    /// <summary>Creates a root view-box crop job using resolved drawing bounds.</summary>
    /// <param name="options">The resolution, bounds, padding, and viewport settings.</param>
    /// <returns>A bounded drawing-bounds crop job.</returns>
    public SvgProcessJob CropToDrawingBoundsWithOptions(SvgDrawingBoundsCropOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        SvgProcessJob result = new SvgProcessJobs().LossyJob("crop-drawing-bounds", SvgProcessJobKind.CropDrawingBounds, SvgProcessChangeSeverity.Destructive, "Crop the root view box to resolved drawing bounds.", context => SvgDocumentTransformJobs.CropDrawingBounds(context, options));
        result.PreservesViewBox = false;
        return result;
    }

    /// <summary>Creates a bounded use and resource-reference flattening job.</summary>
    /// <returns>A deterministic local-reference flattening job.</returns>
    public SvgProcessJob FlattenReferencesWithNoArguments()
    {
        return new SvgProcessJobs().FlattenReferencesWithOptions(new SvgFlattenReferencesProcessOptions());
    }

    /// <summary>Creates a bounded use and resource-reference flattening job.</summary>
    /// <param name="options">The use resolution, namespace, and traversal settings.</param>
    /// <returns>A deterministic local-reference flattening job.</returns>
    public SvgProcessJob FlattenReferencesWithOptions(SvgFlattenReferencesProcessOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        SvgProcessJob result = new SvgProcessJobs().Job("flatten-references", SvgProcessJobKind.FlattenReferences, "Expand a use shadow tree or inherited resource reference into authored content.", context => SvgCompositionProcessJobs.FlattenReferences(context, options));
        result.Severity = SvgProcessChangeSeverity.Representation;
        return result;
    }

    /// <summary>Creates an inert fragment extraction job until an identifier is supplied.</summary>
    /// <returns>A bounded fragment extraction job.</returns>
    public SvgProcessJob ExtractFragmentWithNoArguments()
    {
        return new SvgProcessJobs().ExtractFragmentWithOptions(new SvgFragmentExtractionProcessOptions());
    }

    /// <summary>Creates a bounded fragment extraction job.</summary>
    /// <param name="options">The fragment, resource-retention, namespace, and traversal settings.</param>
    /// <returns>A deterministic fragment extraction job.</returns>
    public SvgProcessJob ExtractFragmentWithOptions(SvgFragmentExtractionProcessOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        return new SvgProcessJobs().LossyJob("extract-fragment", SvgProcessJobKind.ExtractFragment, SvgProcessChangeSeverity.Destructive, "Retain one identified fragment as root graphical content.", context => SvgCompositionProcessJobs.ExtractFragment(context, options));
    }

    /// <summary>Creates a caller-output symbol splitting job.</summary>
    /// <returns>A bounded symbol splitting job.</returns>
    public SvgProcessJob SplitSymbolsWithNoArguments()
    {
        return new SvgProcessJobs().SplitSymbolsWithOptions(new SvgSymbolSplitProcessOptions());
    }

    /// <summary>Creates a caller-output symbol splitting job.</summary>
    /// <param name="options">The output collection, namespace, and symbol bound.</param>
    /// <returns>A bounded symbol splitting job.</returns>
    public SvgProcessJob SplitSymbolsWithOptions(SvgSymbolSplitProcessOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        SvgProcessJob result = new SvgProcessJobs().Job("split-symbols", SvgProcessJobKind.SplitSymbols, "Split every symbol into an independently isolated SVG document.", context => SvgCompositionProcessJobs.SplitSymbols(context, options));
        result.Severity = SvgProcessChangeSeverity.Representation;
        return result;
    }

    /// <summary>Creates an empty deterministic multi-document merge job.</summary>
    /// <returns>A bounded merge job.</returns>
    public SvgProcessJob MergeDocumentsWithNoArguments()
    {
        return new SvgProcessJobs().MergeDocumentsWithOptions(new SvgDocumentMergeProcessOptions());
    }

    /// <summary>Creates a deterministic multi-document merge job.</summary>
    /// <param name="options">The ordered inputs, namespace, and work bounds.</param>
    /// <returns>A collision-safe merge job.</returns>
    public SvgProcessJob MergeDocumentsWithOptions(SvgDocumentMergeProcessOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        return new SvgProcessJobs().LossyJob("merge-documents", SvgProcessJobKind.MergeDocuments, SvgProcessChangeSeverity.Destructive, "Import an isolated external document subtree into the current root.", context => SvgCompositionProcessJobs.MergeDocuments(context, options));
    }

    /// <summary>Creates a one-document sprite construction job.</summary>
    /// <returns>A deterministic sprite construction job.</returns>
    public SvgProcessJob BuildSpriteWithNoArguments()
    {
        return new SvgProcessJobs().BuildSpriteWithOptions(new SvgSpriteProcessOptions());
    }

    /// <summary>Creates a deterministic multi-document sprite construction job.</summary>
    /// <param name="options">The current/imported inputs, symbol prefix, and work bounds.</param>
    /// <returns>A collision-safe sprite construction job.</returns>
    public SvgProcessJob BuildSpriteWithOptions(SvgSpriteProcessOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        SvgProcessJob result = new SvgProcessJobs().Job("build-sprite", SvgProcessJobKind.BuildSprite, "Convert document roots into isolated reusable symbols.", context => SvgCompositionProcessJobs.BuildSprite(context, options));
        result.Severity = SvgProcessChangeSeverity.Representation;
        return result;
    }

    /// <summary>Creates a complete identifier namespace isolation job.</summary>
    /// <returns>A deterministic namespace isolation job.</returns>
    public SvgProcessJob IsolateNamespaceWithNoArguments()
    {
        return new SvgProcessJobs().IsolateNamespaceWithOptions(new SvgNamespaceIsolationProcessOptions());
    }

    /// <summary>Creates a complete identifier namespace isolation job.</summary>
    /// <param name="options">The complete identifier-prefixing settings.</param>
    /// <returns>A deterministic namespace isolation job.</returns>
    public SvgProcessJob IsolateNamespaceWithOptions(SvgNamespaceIsolationProcessOptions options)
    {
        if (options == null) throw new System.ArgumentNullException("options");
        return new SvgProcessJobs().Job("isolate-namespace", SvgProcessJobKind.IsolateNamespace, "Isolate every identifier and local reference in a document namespace.", context => SvgCompositionProcessJobs.IsolateNamespace(context, options));
    }

    /// <summary>Creates an implemented built-in job for one declarative operation.</summary>
    /// <param name="operation">The requested operation.</param>
    /// <returns>A new job, or <c>null</c> when a later task owns the implementation.</returns>
    public SvgProcessJob? Create(SvgProcessOperation operation)
    {
        if (operation == SvgProcessOperation.RemoveComments) return new SvgProcessJobs().RemoveComments();
        if (operation == SvgProcessOperation.RemoveEditorMetadata) return new SvgProcessJobs().RemoveEditorMetadata();
        if (operation == SvgProcessOperation.RemoveUnusedNamespaces) return new SvgProcessJobs().RemoveUnusedNamespaces();
        if (operation == SvgProcessOperation.RemoveDefaultAttributes) return new SvgProcessJobs().RemoveDefaultAttributes();
        if (operation == SvgProcessOperation.RemoveEmptyContainers) return new SvgProcessJobs().RemoveEmptyContainers();
        if (operation == SvgProcessOperation.RemoveUnreferencedInvisibleNodes) return new SvgProcessJobs().RemoveUnreferencedInvisibleNodes();
        if (operation == SvgProcessOperation.RemoveUnusedDefinitions) return new SvgProcessJobs().RemoveUnusedDefinitions();
        if (operation == SvgProcessOperation.DeduplicateDefinitions) return new SvgProcessJobs().DeduplicateDefinitions();
        if (operation == SvgProcessOperation.CollapseGroups) return new SvgProcessJobs().CollapseGroups();
        if (operation == SvgProcessOperation.HoistCommonAttributes) return new SvgProcessJobs().HoistCommonAttributes();
        if (operation == SvgProcessOperation.MergeAdjacentPaths) return new SvgProcessJobs().MergeAdjacentPaths();
        if (operation == SvgProcessOperation.CleanupStyles) return new SvgProcessJobs().CleanupStyles();
        if (operation == SvgProcessOperation.InlineStyles) return new SvgProcessJobs().InlineStyles();
        if (operation == SvgProcessOperation.ResolveCustomProperties) return new SvgProcessJobs().ResolveCustomProperties();
        if (operation == SvgProcessOperation.CanonicalizeValues) return new SvgProcessJobs().CanonicalizeValuesWithNoArguments();
        if (operation == SvgProcessOperation.PrefixIds) return new SvgProcessJobs().PrefixIdsWithNoArguments();
        if (operation == SvgProcessOperation.MinifyIds) return new SvgProcessJobs().MinifyIdsWithNoArguments();
        if (operation == SvgProcessOperation.ConvertShapesToPaths) return new SvgProcessJobs().ConvertShapesToPathsWithNoArguments();
        if (operation == SvgProcessOperation.BakeTransforms) return new SvgProcessJobs().BakeTransformsWithNoArguments();
        if (operation == SvgProcessOperation.SimplifyPaths) return new SvgProcessJobs().SimplifyPathsWithNoArguments();
        if (operation == SvgProcessOperation.ConvertTextToPaths) return new SvgProcessJobs().ConvertTextToPathsWithNoArguments();
        if (operation == SvgProcessOperation.FlattenEffects) return new SvgProcessJobs().FlattenEffectsWithNoArguments();
        if (operation == SvgProcessOperation.EmbedImages) return new SvgProcessJobs().EmbedImagesWithNoArguments();
        if (operation == SvgProcessOperation.Recolor) return new SvgProcessJobs().RecolorWithNoArguments();
        if (operation == SvgProcessOperation.CropViewBox) return new SvgProcessJobs().ResizeViewBoxWithNoArguments();
        if (operation == SvgProcessOperation.FlattenUses) return new SvgProcessJobs().FlattenReferencesWithNoArguments();
        if (operation == SvgProcessOperation.BuildSprite) return new SvgProcessJobs().BuildSpriteWithNoArguments();
        return null;
    }
SvgProcessJob Job(string id, SvgProcessJobKind kind, string reason, System.Action<SvgProcessContext> callback)
    {
        SvgProcessJob result = new SvgProcessJob(id, callback);
        result.Kind = kind;
        result.Reason = reason;
        result.PreservesAccessibility = true;
        result.PreservesViewBox = true;
        return result;
    }
SvgProcessJob LossyJob(string id, SvgProcessJobKind kind, SvgProcessChangeSeverity severity, string reason, System.Action<SvgProcessContext> callback)
    {
        SvgProcessJob result = new SvgProcessJobs().Job(id, kind, reason, callback);
        result.SemanticLoss = true;
        result.Severity = severity;
        return result;
    }
}
