using System;
using System.IO;
using System.Threading;

namespace tiff;

/// <summary>Creates TIFF readers, writers, diagnostics, and tag registries.</summary>
public class TiffFactory : ITiffFactory
{
    /// <summary>Creates a TIFF factory.</summary>
    public TiffFactory()
    {
    }

    /// <summary>Creates a random-access source over a readable, seekable stream.</summary>
    /// <param name="stream">The stream to wrap.</param>
    /// <returns>The random-access source.</returns>
    public TiffStreamSource CreateStreamSource(Stream stream)
    {
        return new TiffStreamSource(stream);
    }

    /// <summary>Opens a file as a random-access source.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The random-access source.</returns>
    public TiffFileSource CreateFileSource(string path)
    {
        return new TiffFileSource(path);
    }

    /// <summary>Opens a random-access source for image access.</summary>
    /// <param name="source">The source to open.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    public TiffFile OpenReadSource(ITiffRandomAccessSource source, TiffReadOptions options)
    {
        return TiffFile.OpenReadSource(source, options);
    }

    /// <summary>Opens a random-access source with progress reporting and cancellation.</summary>
    /// <param name="source">The source to open.</param>
    /// <param name="options">The read options.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The opened file.</returns>
    /// <exception cref="System.OperationCanceledException">The cancellation token was canceled.</exception>
    public TiffFile OpenReadSourceWithCancellation(ITiffRandomAccessSource source, TiffReadOptions options, ITiffProgress? progress, CancellationToken cancellationToken)
    {
        TiffOperationMonitor monitor = new TiffOperationMonitor(TiffProgressOperation.Open, progress, cancellationToken);
        monitor.Start(1);
        TiffFile file = TiffFile.OpenReadSource(source, options);
        monitor.Complete(1);
        return file;
    }

    /// <summary>Opens a random-access source for metadata-only access.</summary>
    /// <param name="source">The source to open.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    public TiffFile OpenMetadataSource(ITiffRandomAccessSource source, TiffReadOptions options)
    {
        return TiffFile.OpenMetadataSource(source, options);
    }

    /// <summary>Opens complete file bytes for image access.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    public TiffFile OpenReadBytes(byte[] fileBytes, TiffReadOptions options)
    {
        return TiffFile.OpenRead(fileBytes, options);
    }

    /// <summary>Opens memory for image access.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    public TiffFile OpenReadMemory(ReadOnlySpan<byte> fileBytes, TiffReadOptions options)
    {
        return TiffFile.OpenRead(fileBytes, options);
    }

    /// <summary>Opens a path for image access.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    public TiffFile OpenReadPath(string path, TiffReadOptions options)
    {
        return TiffFile.OpenRead(path, options);
    }

    /// <summary>Opens a stream for image access.</summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    public TiffFile OpenReadStream(Stream stream, TiffReadOptions options)
    {
        return TiffFile.OpenRead(stream, options);
    }

    /// <summary>Opens complete file bytes for metadata-only access.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    public TiffFile OpenMetadataBytes(byte[] fileBytes, TiffReadOptions options)
    {
        return TiffFile.OpenMetadata(fileBytes, options);
    }

    /// <summary>Opens memory for metadata-only access.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    public TiffFile OpenMetadataMemory(ReadOnlySpan<byte> fileBytes, TiffReadOptions options)
    {
        return TiffFile.OpenMetadata(fileBytes, options);
    }

    /// <summary>Opens a path for metadata-only access.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    public TiffFile OpenMetadataPath(string path, TiffReadOptions options)
    {
        return TiffFile.OpenMetadata(path, options);
    }

    /// <summary>Opens a stream for metadata-only access.</summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The opened file.</returns>
    public TiffFile OpenMetadataStream(Stream stream, TiffReadOptions options)
    {
        return TiffFile.OpenMetadata(stream, options);
    }

    /// <summary>Resolves overview images from exactly the caller-supplied file paths.</summary>
    /// <param name="baseImage">The base image related to the external overviews.</param>
    /// <param name="paths">The explicit external TIFF paths to inspect.</param>
    /// <param name="options">The read options used to open the external TIFF files.</param>
    /// <returns>A closeable set containing the opened sources and resolved relationships.</returns>
    public TiffExternalOverviewSet ResolveExternalOverviewPaths(TiffImage baseImage, string[] paths, TiffReadOptions options)
    {
        return TiffExternalOverviewResolver.ResolvePaths(baseImage, paths, options);
    }

    /// <summary>Resolves overview images from exactly the caller-supplied streams.</summary>
    /// <param name="baseImage">The base image related to the external overviews.</param>
    /// <param name="streams">The explicit external TIFF streams to inspect.</param>
    /// <param name="options">The read options used to open the external TIFF streams.</param>
    /// <returns>A closeable set containing the opened sources and resolved relationships.</returns>
    public TiffExternalOverviewSet ResolveExternalOverviewStreams(TiffImage baseImage, Stream[] streams, TiffReadOptions options)
    {
        return TiffExternalOverviewResolver.ResolveStreams(baseImage, streams, options);
    }

    /// <summary>Creates an in-memory writer.</summary>
    /// <param name="options">The write options.</param>
    /// <returns>The writer.</returns>
    public TiffWriter CreateWriter(TiffWriteOptions options)
    {
        return TiffWriter.Create(options);
    }

    /// <summary>Creates a writer targeting a path.</summary>
    /// <param name="path">The output path.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The writer.</returns>
    public TiffWriter CreatePathWriter(string path, TiffWriteOptions options)
    {
        return TiffWriter.Create(path, options);
    }

    /// <summary>Creates a writer targeting a stream.</summary>
    /// <param name="stream">The output stream.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The writer.</returns>
    public TiffWriter CreateStreamWriter(Stream stream, TiffWriteOptions options)
    {
        return TiffWriter.Create(stream, options);
    }

    /// <summary>Creates a registry populated with standard TIFF definitions.</summary>
    /// <returns>The registry.</returns>
    public TiffTagRegistry CreateDefaultTagRegistry()
    {
        return TiffTagRegistry.CreateDefault();
    }
}
