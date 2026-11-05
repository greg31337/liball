using System.IO;

namespace gltf;

/// <summary>Writes documents as deterministic JSON glTF or binary GLB output.</summary>
public interface IGltfWriter
{
    /// <summary>Writes a document to a file.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write result.</returns>
    GltfWriteResult WriteFile(GltfDocument document, string path, GltfWriteOptions options);

    /// <summary>Writes a document to a stream.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="stream">The output stream.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write result.</returns>
    GltfWriteResult WriteStream(GltfDocument document, Stream stream, GltfWriteOptions options);

    /// <summary>Writes raw container data to a stream without serializing a document.</summary>
    /// <param name="container">The raw container data to write.</param>
    /// <param name="stream">The output stream.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write result.</returns>
    GltfWriteResult WriteContainerStream(GltfContainerData container, Stream stream, GltfWriteOptions options);

    /// <summary>Writes a document to owned bytes.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write result.</returns>
    GltfWriteResult WriteBytes(GltfDocument document, GltfWriteOptions options);

    /// <summary>Writes raw container data without serializing a document.</summary>
    /// <param name="container">The raw container data to write.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write result containing owned primary bytes.</returns>
    GltfWriteResult WriteContainerBytes(GltfContainerData container, GltfWriteOptions options);

    /// <summary>Writes a document as JSON text.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write result containing JSON text.</returns>
    GltfWriteResult WriteJsonText(GltfDocument document, GltfWriteOptions options);
}
