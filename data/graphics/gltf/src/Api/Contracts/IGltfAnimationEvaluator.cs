namespace gltf;

/// <summary>Samples animation clips and applies sampled values to scene poses.</summary>
public interface IGltfAnimationEvaluator
{
    /// <summary>Samples one animation clip.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="options">The animation evaluation options.</param>
    /// <returns>The sampled animation pose.</returns>
    GltfAnimationPose SampleAnimation(GltfDocument document, GltfAnimationEvaluationOptions options);

    /// <summary>Applies sampled animation values while evaluating a scene.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="pose">The sampled animation pose.</param>
    /// <param name="options">The scene evaluation options.</param>
    /// <returns>The evaluated scene pose.</returns>
    GltfScenePose ApplyAnimationPose(GltfDocument document, GltfAnimationPose pose, GltfSceneEvaluationOptions options);

    /// <summary>Calculates the duration of one animation clip.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="animation">The animation index.</param>
    /// <returns>The duration in seconds.</returns>
    double CalculateAnimationDuration(GltfDocument document, int animation);
}
