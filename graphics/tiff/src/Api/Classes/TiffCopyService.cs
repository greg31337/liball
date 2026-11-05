using System.Threading;

namespace tiff;

/// <summary>Copies TIFF metadata and compatible raw image data.</summary>
public class TiffCopyService : ITiffCopyService
{
    /// <summary>Creates a TIFF copy service.</summary>
    public TiffCopyService()
    {
    }

    /// <summary>Copies metadata into a result.</summary>
    /// <param name="image">The source image.</param>
    /// <returns>The copy result.</returns>
    public TiffMetadataCopyResult CopyMetadata(TiffImage image)
    {
        return TiffCopy.CopyMetadata(image);
    }

    /// <summary>Copies metadata while preserving all unregistered tag values.</summary>
    /// <param name="image">The source image.</param>
    /// <returns>The copy result.</returns>
    public TiffMetadataCopyResult CopyMetadataPreservingUnknown(TiffImage image)
    {
        return TiffCopy.CopyMetadataPreservingUnknown(image);
    }

    /// <summary>Copies metadata into an image definition.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="destination">The destination definition.</param>
    /// <returns>The updated definition.</returns>
    public TiffImageDefinition CopyMetadataToDefinition(TiffImage image, TiffImageDefinition destination)
    {
        return TiffCopy.CopyMetadata(image, destination);
    }

    /// <summary>Copies metadata into a writer image.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="imageIndex">The destination image index.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics CopyMetadataToWriter(TiffImage image, TiffWriter writer, int imageIndex)
    {
        return TiffCopy.CopyMetadata(image, writer, imageIndex);
    }

    /// <summary>Copies raw image data into a result.</summary>
    /// <param name="image">The source image.</param>
    /// <returns>The copy result.</returns>
    public TiffRawImageDataCopyResult CopyRawImageData(TiffImage image)
    {
        return TiffCopy.CopyRawImageData(image);
    }

    /// <summary>Copies raw image data with segment progress and cancellation.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The copy result.</returns>
    /// <exception cref="System.OperationCanceledException">The cancellation token was canceled.</exception>
    public TiffRawImageDataCopyResult CopyRawImageDataWithCancellation(TiffImage image, ITiffProgress? progress, CancellationToken cancellationToken)
    {
        TiffOperationMonitor monitor = new TiffOperationMonitor(TiffProgressOperation.Copy, progress, cancellationToken);
        return TiffCopy.CopyRawImageDataWithOperation(image, monitor);
    }

    /// <summary>Copies raw image data into an image definition.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="destination">The destination definition.</param>
    /// <returns>The copy result.</returns>
    public TiffRawImageDataCopyResult CopyRawImageDataToDefinition(TiffImage image, TiffImageDefinition destination)
    {
        return TiffCopy.CopyRawImageData(image, destination);
    }

    /// <summary>Copies raw image data into a writer image.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="imageIndex">The destination image index.</param>
    /// <returns>Operation diagnostics.</returns>
    public TiffDiagnostics CopyRawImageDataToWriter(TiffImage image, TiffWriter writer, int imageIndex)
    {
        return TiffCopy.CopyRawImageData(image, writer, imageIndex);
    }

    /// <summary>Rewrites one image with metadata changes and optional encoded image-data pass-through.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="options">Metadata changes and preservation policy.</param>
    /// <returns>The rewrite compatibility and outcome.</returns>
    public TiffMetadataRewriteResult RewriteImageMetadata(TiffImage image, TiffWriter writer, TiffMetadataRewriteOptions options)
    {
        return TiffCopy.RewriteImageMetadata(image, writer, options);
    }
}
