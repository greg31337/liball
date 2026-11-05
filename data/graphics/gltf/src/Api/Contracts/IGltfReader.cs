using System.IO;

namespace gltf;

/// <summary>Reads JSON glTF and binary GLB inputs into owned document data.</summary>
public interface IGltfReader
{
    /// <summary>Reads a glTF or GLB file.</summary>
    /// <param name="path">The input file path.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The read result.</returns>
    GltfReadResult ReadFile(string path, GltfReadOptions options);

    /// <summary>Reads glTF or GLB data from a stream.</summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The read result.</returns>
    GltfReadResult ReadStream(Stream stream, GltfReadOptions options);

    /// <summary>Reads raw container data from a stream without constructing a document.</summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The read result containing raw container data.</returns>
    GltfReadResult ReadContainerStream(Stream stream, GltfReadOptions options);

    /// <summary>Reads glTF or GLB data from owned bytes.</summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The read result.</returns>
    GltfReadResult ReadBytes(byte[] bytes, GltfReadOptions options);

    /// <summary>Reads owned raw container data without constructing a document.</summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The read result containing raw container data.</returns>
    GltfReadResult ReadContainerBytes(byte[] bytes, GltfReadOptions options);

    /// <summary>Constructs a document from parsed container data.</summary>
    /// <param name="container">The parsed container data.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The read result.</returns>
    GltfReadResult ReadContainer(GltfContainerData container, GltfReadOptions options);
}
