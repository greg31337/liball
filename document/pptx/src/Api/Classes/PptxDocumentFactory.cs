using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxDocument values.
/// </summary>
public class PptxDocumentFactory : IPptxDocumentFactory
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxDocumentFactory()
    {
    }

    /// <summary>
    /// Creates an empty package for low-level part and relationship editing.
    /// </summary>
    /// <remarks>The returned package does not yet contain the parts required for a valid presentation. Use <see cref="CreatePresentation"/> for a ready-to-edit deck.</remarks>
    /// <returns>The resulting document value.</returns>
    public PptxDocument Create()
    {
        return PptxDocument.Create();
    }

    /// <summary>
    /// Creates a valid minimal presentation containing the standard master, layout, theme, and one blank slide.
    /// </summary>
    /// <returns>The resulting document value.</returns>
    public PptxDocument CreateMinimumPresentation()
    {
        return PptxDocument.CreateMinimumPresentation();
    }

    /// <summary>
    /// Creates a valid minimal presentation ready for slide and shape editing.
    /// </summary>
    /// <returns>The resulting document value.</returns>
    public PptxDocument CreatePresentation()
    {
        return PptxDocument.CreatePresentation();
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from the supplied input.
    /// </summary>
    /// <param name="path">The path of the presentation to open.</param>
    /// <returns>The resulting document value.</returns>
    public PptxDocument OpenFile(string path)
    {
        return PptxDocument.OpenFile(path);
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from a file using configurable ZIP safety limits.
    /// </summary>
    /// <param name="path">The path of the presentation to open.</param>
    /// <param name="options">The ZIP safety limits to apply.</param>
    /// <returns>The resulting document value.</returns>
    public PptxDocument OpenFileWithOptions(string path, PptxZipReadOptions options)
    {
        return PptxDocument.OpenFileWithOptions(path, options);
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from the supplied input.
    /// </summary>
    /// <remarks>The caller retains ownership of the stream. This method does not dispose it, including when opening fails.</remarks>
    /// <param name="stream">The readable stream positioned at the ZIP package data.</param>
    /// <returns>The resulting document value.</returns>
    public PptxDocument OpenStream(Stream stream)
    {
        return PptxDocument.OpenStream(stream);
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from a stream using configurable ZIP safety limits.
    /// </summary>
    /// <remarks>The caller retains ownership of the stream. This method does not dispose it, including when opening fails.</remarks>
    /// <param name="stream">The readable stream positioned at the ZIP package data.</param>
    /// <param name="options">The ZIP safety limits to apply.</param>
    /// <returns>The resulting document value.</returns>
    public PptxDocument OpenStreamWithOptions(Stream stream, PptxZipReadOptions options)
    {
        return PptxDocument.OpenStreamWithOptions(stream, options);
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from the supplied input.
    /// </summary>
    /// <remarks>The caller retains ownership of the stream. This method does not dispose it, including when opening fails.</remarks>
    /// <param name="stream">The readable stream positioned at the ZIP package data.</param>
    /// <returns>The resulting document value.</returns>
    public PptxDocument Open(Stream stream)
    {
        return PptxDocument.Open(stream);
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from the supplied input.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting document value.</returns>
    public PptxDocument OpenBytes(byte[] bytes)
    {
        return PptxDocument.OpenBytes(bytes);
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from a byte array using configurable ZIP safety limits.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="options">The ZIP safety limits to apply.</param>
    /// <returns>The resulting document value.</returns>
    public PptxDocument OpenBytesWithOptions(byte[] bytes, PptxZipReadOptions options)
    {
        return PptxDocument.OpenBytesWithOptions(bytes, options);
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from a read-only byte span.
    /// </summary>
    /// <remarks>The package is fully detached from the span before this method returns.</remarks>
    /// <param name="bytes">The binary payload to use.</param>
    /// <returns>The resulting document value.</returns>
    public PptxDocument OpenSpan(System.ReadOnlySpan<byte> bytes)
    {
        return PptxDocument.OpenSpan(bytes);
    }

    /// <summary>
    /// Opens and parses a PowerPoint package from a read-only byte span using configurable ZIP safety limits.
    /// </summary>
    /// <remarks>The package is fully detached from the span before this method returns.</remarks>
    /// <param name="bytes">The binary payload to use.</param>
    /// <param name="options">The ZIP safety limits to apply.</param>
    /// <returns>The resulting document value.</returns>
    public PptxDocument OpenSpanWithOptions(System.ReadOnlySpan<byte> bytes, PptxZipReadOptions options)
    {
        return PptxDocument.OpenSpanWithOptions(bytes, options);
    }
}
