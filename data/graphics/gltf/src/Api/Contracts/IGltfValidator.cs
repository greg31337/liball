namespace gltf;

/// <summary>Validates glTF documents and container inputs.</summary>
public interface IGltfValidator
{
    /// <summary>Validates a constructed document.</summary>
    /// <param name="document">The document to validate.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation report.</returns>
    GltfValidationReport ValidateDocument(GltfDocument document, GltfValidationOptions options);

    /// <summary>Validates a glTF or GLB file.</summary>
    /// <param name="path">The input file path.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation report.</returns>
    GltfValidationReport ValidateFile(string path, GltfValidationOptions options);

    /// <summary>Validates glTF or GLB bytes.</summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation report.</returns>
    GltfValidationReport ValidateBytes(byte[] bytes, GltfValidationOptions options);

    /// <summary>Validates parsed container data.</summary>
    /// <param name="container">The parsed container data.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation report.</returns>
    GltfValidationReport ValidateContainer(GltfContainerData container, GltfValidationOptions options);
}
