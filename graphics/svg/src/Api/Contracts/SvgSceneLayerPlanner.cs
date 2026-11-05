namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgSceneLayerPlanner"/>.</summary>
public interface ISvgSceneLayerPlanner
{
    /// <summary>Plans a normalized scene using bounded defaults.</summary>
    /// <param name="scene">The current normalized scene.</param>
    /// <param name="document">The current resolved document supplying source context.</param>
    /// <returns>The deterministic layer plan.</returns>
    SvgSceneLayerPlan PlanWithSceneAndDocument(SvgScene scene, SvgResolvedDocument document);
    /// <summary>Plans a normalized scene under an explicit layer bound.</summary>
    /// <param name="scene">The current normalized scene.</param>
    /// <param name="document">The current resolved document supplying source context.</param>
    /// <param name="options">The layer traversal options.</param>
    /// <returns>The complete or failed deterministic layer plan.</returns>
    SvgSceneLayerPlan PlanWithSceneAndDocumentAndOptions(SvgScene scene, SvgResolvedDocument document, SvgSceneLayerOptions options);
}
