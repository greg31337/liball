using System.IO;

namespace gltf;

/// <summary>Converts glTF containers, resource layouts, and document representations.</summary>
public interface IGltfConverter
{
    /// <summary>Converts an input file to an output file.</summary>
    /// <param name="inputPath">The input file path.</param>
    /// <param name="outputPath">The output file path.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The conversion result.</returns>
    GltfConversionResult ConvertFile(string inputPath, string outputPath, GltfConversionOptions options);

    /// <summary>Converts an input stream to an output stream.</summary>
    /// <param name="input">The input stream.</param>
    /// <param name="output">The output stream.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The conversion result.</returns>
    GltfConversionResult ConvertStream(Stream input, Stream output, GltfConversionOptions options);

    /// <summary>Converts input bytes to owned output data.</summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The conversion result.</returns>
    GltfConversionResult ConvertBytes(byte[] bytes, GltfConversionOptions options);

    /// <summary>Converts a constructed document.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The conversion result.</returns>
    GltfConversionResult ConvertDocument(GltfDocument document, GltfConversionOptions options);
}
