namespace gltf;

/// <summary>Applies one application-defined transformation step.</summary>
public interface IGltfTransformHook
{
    /// <summary>Applies a custom transform to mutable working state.</summary>
    /// <param name="context">The transform context.</param>
    /// <returns>The transform step result.</returns>
    GltfTransformStepResult ApplyTransformHook(GltfTransformContext context);
}
