namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgSceneBounds"/>.</summary>
public interface ISvgSceneBounds
{
    /// <summary>Precomputes a scene using deterministic bounded defaults.</summary>
    /// <param name="scene">The normalized scene to update.</param>
    /// <param name="document">The current resolved document that supplied scene identities.</param>
    /// <returns>The completed bounds outcome.</returns>
    SvgSceneBoundsResult ComputeWithSceneAndDocument(SvgScene scene, SvgResolvedDocument document);
    /// <summary>Precomputes a scene using explicit component and traversal options.</summary>
    /// <param name="scene">The normalized scene to update.</param>
    /// <param name="document">The current resolved document that supplied scene identities.</param>
    /// <param name="options">The component and traversal options.</param>
    /// <returns>The completed or failed bounds outcome.</returns>
    SvgSceneBoundsResult ComputeWithSceneAndDocumentAndOptions(SvgScene scene, SvgResolvedDocument document, SvgSceneBoundsOptions options);
}
