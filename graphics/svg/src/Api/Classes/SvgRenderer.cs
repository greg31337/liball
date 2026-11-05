namespace svg;

/// <summary>Renders complete SVG documents, normalized scenes, fragments, authored nodes, and use instances.</summary>
public class SvgRenderer : ISvgRenderer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgRenderer()
    {
    }

    /// <summary>Resolves and renders an editable document using deterministic defaults.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <returns>The complete raster result.</returns>
    public SvgRenderResult RenderWithSvgDocument(SvgDocument document)
    {
        return new SvgRenderer().RenderWithSvgDocumentAndSvgRenderOptions(document, new SvgRenderOptions());
    }

    /// <summary>Resolves and renders an editable document at the requested viewport, scale, background, and time.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The complete raster result.</returns>
    public SvgRenderResult RenderWithSvgDocumentAndSvgRenderOptions(SvgDocument? document, SvgRenderOptions options)
    {
        if (document == null || !new SvgRenderer().ValidOptions(options)) return new SvgRenderer().Failure(SvgRenderError.InvalidArgument, options, null);
        SvgResolveOptions resolveOptions = new SvgRenderer().ResolveOptions(options);
        SvgResolvedDocument resolved = new SvgDocumentResolver().ResolveWithOptions(document, resolveOptions);
        if (!resolved.Success)
        {
            SvgRenderError error = new SvgRenderer().HasDiagnostic(resolved.Diagnostics, SvgDiagnosticCode.Cancelled) ? SvgRenderError.Cancelled : SvgRenderError.ResolutionFailed;
            SvgRenderResult failed = new SvgRenderer().Failure(error, options, resolved);
            return failed;
        }
        return new SvgRenderer().RenderResolved(resolved, options, 0, "", null);
    }

    /// <summary>Renders a current resolved document using deterministic defaults.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <returns>The complete raster result.</returns>
    public SvgRenderResult RenderWithSvgResolvedDocument(SvgResolvedDocument document)
    {
        return new SvgRenderer().RenderWithSvgResolvedDocumentAndSvgRenderOptions(document, new SvgRenderOptions());
    }

    /// <summary>Renders a current resolved document.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The complete raster result.</returns>
    public SvgRenderResult RenderWithSvgResolvedDocumentAndSvgRenderOptions(SvgResolvedDocument document, SvgRenderOptions options)
    {
        return new SvgRenderer().RenderResolved(document, options, 0, "", null);
    }

    /// <summary>Renders a complete planned scene using deterministic defaults.</summary>
    /// <param name="scene">The current successful planned scene.</param>
    /// <returns>The complete raster result.</returns>
    public SvgRenderResult RenderWithScene(SvgScene scene)
    {
        return new SvgRenderer().RenderWithSceneAndOptions(scene, new SvgRenderOptions());
    }

    /// <summary>Renders a complete current planned scene.</summary>
    /// <param name="scene">The current successful planned scene.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The complete raster result.</returns>
    public SvgRenderResult RenderWithSceneAndOptions(SvgScene? scene, SvgRenderOptions options)
    {
        if (!new SvgRenderer().ValidOptions(options) || scene == null) return new SvgRenderer().Failure(SvgRenderError.InvalidArgument, options, null);
        if (!scene.IsCurrent()) return new SvgRenderer().Failure(SvgRenderError.StaleInput, options, null);
        if (!new SvgRenderer().ValidScene(scene)) return new SvgRenderer().Failure(SvgRenderError.SceneFailed, options, null);
        return new SvgRenderer().RenderScene(scene, null, options);
    }

    /// <summary>Renders one fragment ID from a current resolved document.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="fragment">The fragment identifier with or without a leading number sign.</param>
    /// <returns>The extracted target raster result.</returns>
    public SvgRenderResult RenderFragmentWithSvgResolvedDocumentAndString(SvgResolvedDocument document, string fragment)
    {
        return new SvgRenderer().RenderFragmentWithSvgResolvedDocumentAndStringAndSvgRenderOptions(document, fragment, new SvgRenderOptions());
    }

    /// <summary>Resolves an editable document and renders one fragment ID using deterministic defaults.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <param name="fragment">The fragment identifier with or without a leading number sign.</param>
    /// <returns>The extracted target raster result.</returns>
    public SvgRenderResult RenderFragmentWithSvgDocumentAndString(SvgDocument document, string fragment)
    {
        return new SvgRenderer().RenderFragmentWithSvgDocumentAndStringAndSvgRenderOptions(document, fragment, new SvgRenderOptions());
    }

    /// <summary>Resolves an editable document and renders one fragment ID.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <param name="fragment">The fragment identifier with or without a leading number sign.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The extracted target raster result.</returns>
    public SvgRenderResult RenderFragmentWithSvgDocumentAndStringAndSvgRenderOptions(SvgDocument document, string fragment, SvgRenderOptions options)
    {
        return new SvgRenderer().RenderDocumentTarget(document, options, 1, fragment, null);
    }

    /// <summary>Renders one fragment ID from a current resolved document.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="fragment">The fragment identifier with or without a leading number sign.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The extracted target raster result.</returns>
    public SvgRenderResult RenderFragmentWithSvgResolvedDocumentAndStringAndSvgRenderOptions(SvgResolvedDocument document, string fragment, SvgRenderOptions options)
    {
        return new SvgRenderer().RenderResolved(document, options, 1, fragment, null);
    }

    /// <summary>Renders one authored source node from a current resolved document.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="source">The exact authored source node.</param>
    /// <returns>The extracted target raster result.</returns>
    public SvgRenderResult RenderNodeWithSvgResolvedDocumentAndSvgNode(SvgResolvedDocument document, SvgNode source)
    {
        return new SvgRenderer().RenderNodeWithSvgResolvedDocumentAndSvgNodeAndSvgRenderOptions(document, source, new SvgRenderOptions());
    }

    /// <summary>Resolves an editable document and renders one authored source node using deterministic defaults.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <param name="source">The exact authored source node.</param>
    /// <returns>The extracted target raster result.</returns>
    public SvgRenderResult RenderNodeWithSvgDocumentAndSvgNode(SvgDocument document, SvgNode source)
    {
        return new SvgRenderer().RenderNodeWithSvgDocumentAndSvgNodeAndSvgRenderOptions(document, source, new SvgRenderOptions());
    }

    /// <summary>Resolves an editable document and renders one authored source node.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <param name="source">The exact authored source node.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The extracted target raster result.</returns>
    public SvgRenderResult RenderNodeWithSvgDocumentAndSvgNodeAndSvgRenderOptions(SvgDocument document, SvgNode source, SvgRenderOptions options)
    {
        return new SvgRenderer().RenderDocumentTarget(document, options, 2, "", source);
    }

    /// <summary>Renders one authored source node from a current resolved document.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="source">The exact authored source node.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The extracted target raster result.</returns>
    public SvgRenderResult RenderNodeWithSvgResolvedDocumentAndSvgNodeAndSvgRenderOptions(SvgResolvedDocument document, SvgNode source, SvgRenderOptions options)
    {
        return new SvgRenderer().RenderResolved(document, options, 2, "", source);
    }

    /// <summary>Renders one exact use-instance path from a current resolved document.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="instancePath">The exact nonempty global instance path.</param>
    /// <returns>The extracted target raster result.</returns>
    public SvgRenderResult RenderInstanceWithSvgResolvedDocumentAndString(SvgResolvedDocument document, string instancePath)
    {
        return new SvgRenderer().RenderInstanceWithSvgResolvedDocumentAndStringAndSvgRenderOptions(document, instancePath, new SvgRenderOptions());
    }

    /// <summary>Resolves an editable document and renders one exact use-instance path using deterministic defaults.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <param name="instancePath">The exact nonempty global instance path.</param>
    /// <returns>The extracted target raster result.</returns>
    public SvgRenderResult RenderInstanceWithSvgDocumentAndString(SvgDocument document, string instancePath)
    {
        return new SvgRenderer().RenderInstanceWithSvgDocumentAndStringAndSvgRenderOptions(document, instancePath, new SvgRenderOptions());
    }

    /// <summary>Resolves an editable document and renders one exact use-instance path.</summary>
    /// <param name="document">The current editable SVG document.</param>
    /// <param name="instancePath">The exact nonempty global instance path.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The extracted target raster result.</returns>
    public SvgRenderResult RenderInstanceWithSvgDocumentAndStringAndSvgRenderOptions(SvgDocument document, string instancePath, SvgRenderOptions options)
    {
        return new SvgRenderer().RenderDocumentTarget(document, options, 3, instancePath, null);
    }

    /// <summary>Renders one exact use-instance path from a current resolved document.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="instancePath">The exact nonempty global instance path.</param>
    /// <param name="options">The complete rendering request.</param>
    /// <returns>The extracted target raster result.</returns>
    public SvgRenderResult RenderInstanceWithSvgResolvedDocumentAndStringAndSvgRenderOptions(SvgResolvedDocument document, string instancePath, SvgRenderOptions options)
    {
        return new SvgRenderer().RenderResolved(document, options, 3, instancePath, null);
    }
SvgRenderResult RenderDocumentTarget(SvgDocument? document, SvgRenderOptions options, int target, string? text, SvgNode? source)
    {
        if (document == null || text == null || !new SvgRenderer().ValidOptions(options)) return new SvgRenderer().Failure(SvgRenderError.InvalidArgument, options, null);
        SvgResolvedDocument resolved = new SvgDocumentResolver().ResolveWithOptions(document, new SvgRenderer().ResolveOptions(options));
        if (!resolved.Success)
        {
            SvgRenderError error = new SvgRenderer().HasDiagnostic(resolved.Diagnostics, SvgDiagnosticCode.Cancelled) ? SvgRenderError.Cancelled : SvgRenderError.ResolutionFailed;
            return new SvgRenderer().Failure(error, options, resolved);
        }
        return new SvgRenderer().RenderResolved(resolved, options, target, text, source);
    }
SvgRenderResult RenderResolved(SvgResolvedDocument? document, SvgRenderOptions options, int target, string? text, SvgNode? source)
    {
        if (!new SvgRenderer().ValidOptions(options) || document == null || text == null) return new SvgRenderer().Failure(SvgRenderError.InvalidArgument, options, document);
        if (!document.IsCurrent()) return new SvgRenderer().Failure(SvgRenderError.StaleInput, options, document);
        if (!document.Success) return new SvgRenderer().Failure(SvgRenderError.ResolutionFailed, options, document);
        SvgScene? scene = document.Scene;
        if (!new SvgRenderer().ValidScene(scene))
        {
            SvgSceneBuildOptions buildOptions = new SvgSceneBuildOptions();
            buildOptions.MaxNodes = options.MaximumNodes;
            buildOptions.MaxResources = options.Limits.Resources.MaxCount;
            SvgSceneBuildResult built = new SvgSceneBuilder().BuildWithDocumentAndOptions(document, buildOptions);
            if (!built.Success)
            {
                SvgRenderResult failed = new SvgRenderer().Failure(SvgRenderError.SceneFailed, options, document);
                new SvgRenderer().CopyDiagnostics(built.Diagnostics, failed.Diagnostics);
                return failed;
            }
            scene = built.Scene;
        }
        if (target != 0)
        {
            SvgSceneExtractionOptions extractionOptions = new SvgSceneExtractionOptions();
            extractionOptions.MaximumNodes = options.MaximumNodes;
            extractionOptions.MaximumResources = options.Limits.Resources.MaxCount;
            SvgSceneExtractionResult extracted;
            if (target == 1) extracted = new SvgSceneExtractor().ExtractFragmentWithSceneAndDocumentAndFragmentAndOptions((scene ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), document, text, extractionOptions);
            else if (target == 2) extracted = new SvgSceneExtractor().ExtractSourceWithSceneAndDocumentAndSourceAndOptions((scene ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), document, source, extractionOptions);
            else extracted = new SvgSceneExtractor().ExtractInstanceWithSceneAndDocumentAndInstancePathAndOptions((scene ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), document, text, extractionOptions);
            if (!extracted.Success)
            {
                SvgRenderError error = extracted.Error == SvgSceneExtractionError.TargetNotFound ? SvgRenderError.TargetNotFound : extracted.Error == SvgSceneExtractionError.LimitExceeded ? SvgRenderError.WorkLimitExceeded : extracted.Error == SvgSceneExtractionError.InvalidArgument ? SvgRenderError.InvalidArgument : SvgRenderError.SceneFailed;
                return new SvgRenderer().Failure(error, options, document);
            }
            scene = extracted.Scene;
        }
        return new SvgRenderer().RenderScene(scene, document, options);
    }
SvgRenderResult RenderScene(SvgScene? scene, SvgResolvedDocument? document, SvgRenderOptions options)
    {
        SvgRenderResult result = new SvgRenderResult();
        result.Error = SvgRenderError.None;
        result.Scene = scene;
        result.Commands = (scene ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).RenderCommands;
        result.ResolvedDocument = document;
        result.DrawingBounds = scene.Bounds;
        result.AnimationTime = options.AnimationTime;
        if (document != null)
        {
            new SvgRenderer().CopyDiagnostics(document.Diagnostics, result.Diagnostics);
            result.Resources.RequestCount = document.Resources.RequestCount;
            result.Resources.LoadedCount = document.Resources.LoadedCount;
            result.Resources.EncodedBytes = document.Resources.EncodedBytes;
            result.FeatureReport = document.FeatureReport;
        }
        new SvgRenderer().CopyDiagnostics(scene.Diagnostics, result.Diagnostics);
        long widthValue = (long)System.Math.Ceiling(options.ViewportWidth * options.Scale);
        long heightValue = (long)System.Math.Ceiling(options.ViewportHeight * options.Scale);
        if (widthValue < 1 || heightValue < 1 || widthValue > int.MaxValue || heightValue > int.MaxValue)
        {
            new SvgRenderer().SetFailure(result, SvgRenderError.AllocationLimitExceeded, "The requested output dimensions cannot be represented.", null);
            return result;
        }
        SvgPixelBufferResult created = SvgPixelBuffer.Create((int)widthValue, (int)heightValue, SvgPixelColorSpace.Srgb, options.Limits.Raster);
        if (!created.Success)
        {
            new SvgRenderer().SetFailure(result, SvgRenderError.AllocationLimitExceeded, "The requested output exceeds raster allocation limits.", null);
            return result;
        }
        result.Pixels = created.Buffer;
        if (options.Background != null) (result.Pixels ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ClearWithPixel(options.Background);
        SvgMatrix pixelsFromWorld = new SvgMatrix(options.Scale, 0, 0, options.Scale, -options.ViewportX * options.Scale, -options.ViewportY * options.Scale);
        SvgRenderState state = new SvgRenderState(scene, result, options, pixelsFromWorld);
        if (!state.Run()) return result;
        if (options.EncodePng)
        {
            SvgPngEncodeOptions png = new SvgRenderer().CopyPngOptions(options.PngOptions, options.Limits.Raster);
            SvgPngEncodeResult encoded = new SvgPngCodec().EncodeWithPixelsAndOptions((result.Pixels ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), png);
            if (!encoded.Success)
            {
                new SvgRenderer().SetFailure(result, SvgRenderError.EncodingFailed, "PNG output encoding failed.", null);
                return result;
            }
            result.PngBytes = encoded.Bytes;
        }
        result.Success = true;
        result.Error = SvgRenderError.None;
        return result;
    }
bool ValidOptions(SvgRenderOptions? options)
    {
        if (options == null || options.PngOptions == null || options.ResolveOptions == null || options.Limits == null) return false;
        if (!options.Limits.Validate().Success || options.Limits.Raster == null || options.Limits.Filters == null || options.Limits.Geometry == null || options.Limits.References == null || options.Limits.Resources == null) return false;
        if (!SvgFiniteMath.IsFinite(options.ViewportX) || !SvgFiniteMath.IsFinite(options.ViewportY) || !SvgFiniteMath.IsFinite(options.ViewportWidth) || !SvgFiniteMath.IsFinite(options.ViewportHeight) || !SvgFiniteMath.IsFinite(options.Scale) || !SvgFiniteMath.IsFinite(options.AnimationTime)) return false;
        if (options.ViewportWidth <= 0 || options.ViewportHeight <= 0 || options.Scale <= 0 || options.SamplesPerAxis < 1 || options.SamplesPerAxis > 16 || options.MaximumNodes < 1 || options.MaximumPixels < 1) return false;
        if (options.Background != null && (options.Background.Red > options.Background.Alpha || options.Background.Green > options.Background.Alpha || options.Background.Blue > options.Background.Alpha)) return false;
        return true;
    }
bool ValidScene(SvgScene? scene)
    {
        return scene != null && scene.Success && scene.Root != null && scene.LayerPlan != null && scene.LayerPlan.Success && scene.RenderCommands != null && scene.RenderCommands.Success;
    }
SvgResolveOptions ResolveOptions(SvgRenderOptions options)
    {
        SvgResolveOptions source = options.ResolveOptions;
        SvgResolveOptions result = new SvgResolveOptions();
        result.ViewportWidth = options.ViewportWidth;
        result.ViewportHeight = options.ViewportHeight;
        result.FragmentIdentifier = source.FragmentIdentifier;
        result.LanguagePreferences = source.LanguagePreferences;
        result.Capabilities = source.Capabilities;
        result.Extensions = source.Extensions;
        result.CallerStyleSheets = source.CallerStyleSheets;
        result.StyleOptions = source.StyleOptions;
        result.ResourcePolicy = source.ResourcePolicy;
        result.ResourceProvider = source.ResourceProvider;
        result.AnimationTime = options.AnimationTime;
        result.Limits = options.Limits;
        result.DiagnosticBehavior = source.DiagnosticBehavior;
        result.MaxDiagnostics = source.MaxDiagnostics;
        result.MaxCacheEntries = source.MaxCacheEntries;
        result.ImageContentAvailable = source.ImageContentAvailable;
        result.ForeignContentCallbackAvailable = source.ForeignContentCallbackAvailable;
        result.HostContentCallback = source.HostContentCallback;
        result.ImageIntrinsicSize = source.ImageIntrinsicSize;
        result.ExternalDocument = source.ExternalDocument;
        result.CancellationToken = options.CancellationToken;
        return result;
    }
SvgPngEncodeOptions CopyPngOptions(SvgPngEncodeOptions source, SvgRasterLimits limits)
    {
        SvgPngEncodeOptions result = new SvgPngEncodeOptions();
        result.RasterLimits = limits;
        result.FilterMode = source.FilterMode;
        result.CompressionMode = source.CompressionMode;
        result.IncludeSrgbChunk = source.IncludeSrgbChunk;
        result.PixelsPerMeterX = source.PixelsPerMeterX;
        result.PixelsPerMeterY = source.PixelsPerMeterY;
        result.MaximumEncodedBytes = source.MaximumEncodedBytes;
        return result;
    }
SvgRenderResult Failure(SvgRenderError error, SvgRenderOptions? options, SvgResolvedDocument? document)
    {
        SvgRenderResult result = new SvgRenderResult();
        result.Error = error;
        result.ResolvedDocument = document;
        result.AnimationTime = options == null ? 0 : options.AnimationTime;
        if (document != null) new SvgRenderer().CopyDiagnostics(document.Diagnostics, result.Diagnostics);
        return result;
    }

    internal void SetFailure(SvgRenderResult result, SvgRenderError error, string message, SvgSourceSpan? source)
    {
        if (result.Error != SvgRenderError.None) return;
        result.Success = false;
        result.Error = error;
        SvgDiagnosticCode code = error == SvgRenderError.Cancelled ? SvgDiagnosticCode.Cancelled : error == SvgRenderError.WorkLimitExceeded || error == SvgRenderError.AllocationLimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.RenderingFailed;
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
    }
void CopyDiagnostics(System.Collections.Generic.List<SvgDiagnostic>? source, System.Collections.Generic.List<SvgDiagnostic> destination)
    {
        if (source == null) return;
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
bool HasDiagnostic(System.Collections.Generic.List<SvgDiagnostic>? diagnostics, SvgDiagnosticCode code)
    {
        if (diagnostics == null) return false;
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index] != null && diagnostics[index].Code == code) return true;
            index = index + 1;
        }
        return false;
    }
}
