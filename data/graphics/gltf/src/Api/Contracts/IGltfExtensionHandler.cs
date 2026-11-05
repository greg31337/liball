namespace gltf;

/// <summary>Describes, reads, writes, validates, and remaps one glTF extension.</summary>
public interface IGltfExtensionHandler
{
    /// <summary>Describes the supported extension.</summary>
    /// <returns>The extension descriptor.</returns>
    GltfExtensionDescriptor DescribeExtension();

    /// <summary>Reads extension data.</summary>
    /// <param name="context">The extension operation context.</param>
    /// <returns>The extension operation result.</returns>
    GltfExtensionResult ReadExtension(GltfExtensionContext context);

    /// <summary>Writes extension data.</summary>
    /// <param name="context">The extension operation context.</param>
    /// <returns>The extension operation result.</returns>
    GltfExtensionResult WriteExtension(GltfExtensionContext context);

    /// <summary>Validates extension data.</summary>
    /// <param name="context">The extension operation context.</param>
    /// <returns>The extension operation result.</returns>
    GltfExtensionResult ValidateExtension(GltfExtensionContext context);

    /// <summary>Remaps extension index references after a document edit.</summary>
    /// <param name="context">The extension operation context.</param>
    /// <param name="remap">The index remap.</param>
    /// <returns>The extension operation result.</returns>
    GltfExtensionResult RemapExtensionReferences(GltfExtensionContext context, GltfIndexRemap remap);
}
