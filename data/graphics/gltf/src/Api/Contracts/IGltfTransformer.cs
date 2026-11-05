namespace gltf;

/// <summary>Executes ordered transactional document transformations.</summary>
public interface IGltfTransformer
{
    /// <summary>Transforms a document according to an ordered plan.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="plan">The transform plan.</param>
    /// <param name="options">The transform options.</param>
    /// <returns>The transform report.</returns>
    GltfTransformReport TransformDocument(GltfDocument document, GltfTransformPlan plan, GltfTransformOptions options);
}
