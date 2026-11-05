using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxDocumentFactory"/>.
/// </summary>
public interface IPptxDocumentFactory
{
    /// <summary>
    /// Creates an empty package for low-level part and relationship editing.
    /// </summary>
    /// <remarks>The returned package does not yet contain the parts required for a valid presentation. Use <see cref="CreatePresentation"/> for a ready-to-edit deck.</remarks>
    /// <returns>The resulting document value.</returns>
    PptxDocument Create();
    /// <summary>
    /// Creates a valid minimal presentation containing the standard master, layout, theme, and one blank slide.
    /// </summary>
    /// <returns>The resulting document value.</returns>
    PptxDocument CreateMinimumPresentation();
    /// <summary>
    /// Creates a valid minimal presentation ready for slide and shape editing.
    /// </summary>
    /// <returns>The resulting document value.</returns>
    PptxDocument CreatePresentation();
    /// <summary>
    /// Opens and parses a PowerPoint package from the supplied input.
    /// </summary>
    /// <param name="path">The path of the presentation to open.</param>
    /// <returns>The resulting document value.</returns>
    PptxDocument OpenFile(string path);
    /// <summary>
    /// Opens and parses a PowerPoint package from a file using configurable ZIP safety limits.
    /// </summary>
    /// <param name="path">The path of the presentation to open.</param>
    /// <param name="options">The ZIP safety limits to apply.</param>
    /// <returns>The resulting document value.</returns>
    PptxDocument OpenFileWithOptions(string path, PptxZipReadOptions options);
    /// <summary>
    /// Opens and parses a PowerPoint package from the supplied input.
    /// </summary>
    /// <remarks>The caller retains ownership of the stream. This method does not dispose it, including when opening fails.</remarks>
    /// <param name="stream">The readable stream positioned at the ZIP package data.</param>
    /// <returns>The resulting document value.</returns>
    PptxDocument OpenStream(Stream stream);
    /// <summary>
    /// Opens and parses a PowerPoint package from a stream using configurable ZIP safety limits.
    /// </summary>
    /// <remarks>The caller retains ownership of the stream. This method does not dispose it, including when opening fails.</remarks>
    /// <param name="stream">The readable stream positioned at the ZIP package data.</param>
    /// <param name="options">The ZIP safety limits to apply.</param>
    /// <returns>The resulting document value.</returns>
    PptxDocument OpenStreamWithOptions(Stream stream, PptxZipReadOptions options);
    /// <summary>
    /// Opens and parses a PowerPoint package from the supplied input.
    /// </summary>
    /// <remarks>The caller retains ownership of the stream. This method does not dispose it, including when opening fails.</remarks>
    /// <param name="stream">The readable stream positioned at the ZIP package data.</param>
    /// <returns>The resulting document value.</returns>
    PptxDocument Open(Stream stream);
    /// <summary>
    /// Opens and parses a PowerPoint package from the supplied input.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting document value.</returns>
    PptxDocument OpenBytes(byte[] bytes);
    /// <summary>
    /// Opens and parses a PowerPoint package from a byte array using configurable ZIP safety limits.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <param name="options">The ZIP safety limits to apply.</param>
    /// <returns>The resulting document value.</returns>
    PptxDocument OpenBytesWithOptions(byte[] bytes, PptxZipReadOptions options);
    /// <summary>
    /// Opens and parses a PowerPoint package from a read-only byte span.
    /// </summary>
    /// <remarks>The package is fully detached from the span before this method returns.</remarks>
    /// <param name="bytes">The binary payload to use.</param>
    /// <returns>The resulting document value.</returns>
    PptxDocument OpenSpan(System.ReadOnlySpan<byte> bytes);
    /// <summary>
    /// Opens and parses a PowerPoint package from a read-only byte span using configurable ZIP safety limits.
    /// </summary>
    /// <remarks>The package is fully detached from the span before this method returns.</remarks>
    /// <param name="bytes">The binary payload to use.</param>
    /// <param name="options">The ZIP safety limits to apply.</param>
    /// <returns>The resulting document value.</returns>
    PptxDocument OpenSpanWithOptions(System.ReadOnlySpan<byte> bytes, PptxZipReadOptions options);
}
