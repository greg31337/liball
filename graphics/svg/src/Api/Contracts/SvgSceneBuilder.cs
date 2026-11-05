namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgSceneBuilder"/>.</summary>
public interface ISvgSceneBuilder
{
    /// <summary>Builds and attaches a normalized scene using bounded defaults.</summary>
    /// <param name="document">The current resolved document.</param>
    /// <returns>The complete scene construction outcome.</returns>
    SvgSceneBuildResult BuildWithDocument(SvgResolvedDocument document);
    /// <summary>Builds a normalized scene using explicit bounds and attachment behavior.</summary>
    /// <param name="document">The current resolved document.</param>
    /// <param name="options">The scene construction options.</param>
    /// <returns>The complete or bounded partial construction outcome.</returns>
    SvgSceneBuildResult BuildWithDocumentAndOptions(SvgResolvedDocument document, SvgSceneBuildOptions options);
}
