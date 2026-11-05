using System;
using System.IO;
using System.Threading;

namespace tiff;

/// <summary>Defines creation operations for TIFF readers, writers, diagnostics, and tag registries.</summary>
public interface ITiffFactory
{
    /// <summary>Creates a random-access source over a readable, seekable stream.</summary>
    /// <param name="stream">The stream to wrap.</param>
    /// <returns>The random-access source.</returns>
    TiffStreamSource CreateStreamSource(Stream stream);
    /// <summary>Opens a file as a random-access source.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The random-access source.</returns>
    TiffFileSource CreateFileSource(string path);
    /// <summary>Opens a random-access source for image access.</summary>
    /// <param name="source">The source to open.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    TiffFile OpenReadSource(ITiffRandomAccessSource source, TiffReadOptions options);
    /// <summary>Opens a random-access source with progress reporting and cancellation.</summary>
    /// <param name="source">The source to open.</param>
    /// <param name="options">The read options.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The opened file.</returns>
    /// <exception cref="System.OperationCanceledException">The cancellation token was canceled.</exception>
    TiffFile OpenReadSourceWithCancellation(ITiffRandomAccessSource source, TiffReadOptions options, ITiffProgress? progress, CancellationToken cancellationToken);
    /// <summary>Opens a random-access source for metadata-only access.</summary>
    /// <param name="source">The source to open.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    TiffFile OpenMetadataSource(ITiffRandomAccessSource source, TiffReadOptions options);
    /// <summary>Opens complete file bytes for image access.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    TiffFile OpenReadBytes(byte[] fileBytes, TiffReadOptions options);
    /// <summary>Opens memory for image access.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    TiffFile OpenReadMemory(ReadOnlySpan<byte> fileBytes, TiffReadOptions options);
    /// <summary>Opens a path for image access.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    TiffFile OpenReadPath(string path, TiffReadOptions options);
    /// <summary>Opens a stream for image access.</summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    TiffFile OpenReadStream(Stream stream, TiffReadOptions options);
    /// <summary>Opens complete file bytes for metadata-only access.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    TiffFile OpenMetadataBytes(byte[] fileBytes, TiffReadOptions options);
    /// <summary>Opens memory for metadata-only access.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    TiffFile OpenMetadataMemory(ReadOnlySpan<byte> fileBytes, TiffReadOptions options);
    /// <summary>Opens a path for metadata-only access.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    TiffFile OpenMetadataPath(string path, TiffReadOptions options);
    /// <summary>Opens a stream for metadata-only access.</summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    TiffFile OpenMetadataStream(Stream stream, TiffReadOptions options);
    /// <summary>Resolves overview images from exactly the caller-supplied file paths.</summary>
    /// <param name="baseImage">The base image related to the external overviews.</param>
    /// <param name="paths">The explicit external TIFF paths to inspect.</param>
    /// <param name="options">The read options used to open the external TIFF files.</param>
    /// <returns>A closeable set containing the opened sources and resolved relationships.</returns>
    TiffExternalOverviewSet ResolveExternalOverviewPaths(TiffImage baseImage, string[] paths, TiffReadOptions options);
    /// <summary>Resolves overview images from exactly the caller-supplied streams.</summary>
    /// <param name="baseImage">The base image related to the external overviews.</param>
    /// <param name="streams">The explicit external TIFF streams to inspect.</param>
    /// <param name="options">The read options used to open the external TIFF streams.</param>
    /// <returns>A closeable set containing the opened sources and resolved relationships.</returns>
    TiffExternalOverviewSet ResolveExternalOverviewStreams(TiffImage baseImage, Stream[] streams, TiffReadOptions options);
    /// <summary>Creates an in-memory writer.</summary>
    /// <param name="options">The write options.</param>
    /// <returns>The writer.</returns>
    TiffWriter CreateWriter(TiffWriteOptions options);
    /// <summary>Creates a writer targeting a path.</summary>
    /// <param name="path">The output path.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The writer.</returns>
    TiffWriter CreatePathWriter(string path, TiffWriteOptions options);
    /// <summary>Creates a writer targeting a stream.</summary>
    /// <param name="stream">The output stream.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The writer.</returns>
    TiffWriter CreateStreamWriter(Stream stream, TiffWriteOptions options);
    /// <summary>Creates a registry populated with standard TIFF definitions.</summary>
    /// <returns>The registry.</returns>
    TiffTagRegistry CreateDefaultTagRegistry();
}
