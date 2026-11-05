namespace gltf;

/// <summary>Evaluates scene transforms, bounds, skins, and camera projections.</summary>
public interface IGltfSceneEvaluator
{
    /// <summary>Evaluates one scene.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="options">The evaluation options.</param>
    /// <returns>The evaluated scene pose.</returns>
    GltfScenePose EvaluateScene(GltfDocument document, GltfSceneEvaluationOptions options);

    /// <summary>Calculates the bounds of one scene.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="options">The evaluation options.</param>
    /// <returns>The calculated bounds.</returns>
    GltfBounds CalculateBounds(GltfDocument document, GltfSceneEvaluationOptions options);

    /// <summary>Calculates a camera projection.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="camera">The camera index.</param>
    /// <returns>The projection result.</returns>
    GltfProjectionResult CalculateProjection(GltfDocument document, int camera);
}
