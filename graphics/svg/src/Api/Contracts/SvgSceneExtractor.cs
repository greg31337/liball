namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgSceneExtractor"/>.</summary>
public interface ISvgSceneExtractor
{
    /// <summary>Extracts the scene node identified by a fragment ID using bounded defaults.</summary>
    /// <param name="scene">The complete normalized scene.</param>
    /// <param name="document">The current resolved document.</param>
    /// <param name="fragment">The ID with or without a leading number sign.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    SvgSceneExtractionResult ExtractFragmentWithSceneAndDocumentAndFragment(SvgScene scene, SvgResolvedDocument document, string fragment);
    /// <summary>Extracts a fragment under explicit node, resource, and command bounds.</summary>
    /// <param name="scene">The complete normalized scene.</param>
    /// <param name="document">The current resolved document.</param>
    /// <param name="fragment">The ID with or without a leading number sign.</param>
    /// <param name="options">The extraction bounds.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    SvgSceneExtractionResult ExtractFragmentWithSceneAndDocumentAndFragmentAndOptions(SvgScene scene, SvgResolvedDocument document, string? fragment, SvgSceneExtractionOptions options);
    /// <summary>Extracts one authored source node using bounded defaults.</summary>
    /// <param name="scene">The complete normalized scene.</param>
    /// <param name="document">The current resolved document.</param>
    /// <param name="source">The exact authored source node.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    SvgSceneExtractionResult ExtractSourceWithSceneAndDocumentAndSource(SvgScene scene, SvgResolvedDocument document, SvgNode source);
    /// <summary>Extracts one authored source node under explicit bounds.</summary>
    /// <param name="scene">The complete normalized scene.</param>
    /// <param name="document">The current resolved document.</param>
    /// <param name="source">The exact authored source node.</param>
    /// <param name="options">The extraction bounds.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    SvgSceneExtractionResult ExtractSourceWithSceneAndDocumentAndSourceAndOptions(SvgScene scene, SvgResolvedDocument document, SvgNode? source, SvgSceneExtractionOptions options);
    /// <summary>Extracts one exact use-instance path using bounded defaults.</summary>
    /// <param name="scene">The complete normalized scene.</param>
    /// <param name="document">The current resolved document.</param>
    /// <param name="instancePath">The exact nonempty global instance path.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    SvgSceneExtractionResult ExtractInstanceWithSceneAndDocumentAndInstancePath(SvgScene scene, SvgResolvedDocument document, string instancePath);
    /// <summary>Extracts one exact use-instance path under explicit bounds.</summary>
    /// <param name="scene">The complete normalized scene.</param>
    /// <param name="document">The current resolved document.</param>
    /// <param name="instancePath">The exact nonempty global instance path.</param>
    /// <param name="options">The extraction bounds.</param>
    /// <returns>The independently rooted extracted scene.</returns>
    SvgSceneExtractionResult ExtractInstanceWithSceneAndDocumentAndInstancePathAndOptions(SvgScene scene, SvgResolvedDocument document, string? instancePath, SvgSceneExtractionOptions options);
}
