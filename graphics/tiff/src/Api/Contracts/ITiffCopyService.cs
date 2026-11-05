using System.Threading;

namespace tiff;

/// <summary>Defines metadata and raw image-data copy operations.</summary>
public interface ITiffCopyService
{
    /// <summary>Copies metadata into a result.</summary>
    /// <param name="image">The source image.</param>
    /// <returns>The copy result.</returns>
    TiffMetadataCopyResult CopyMetadata(TiffImage image);
    /// <summary>Copies metadata while preserving unregistered tag values regardless of the source reader option.</summary>
    /// <param name="image">The source image.</param>
    /// <returns>The copy result.</returns>
    TiffMetadataCopyResult CopyMetadataPreservingUnknown(TiffImage image);
    /// <summary>Copies metadata into an image definition.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="destination">The destination definition.</param>
    /// <returns>The updated definition.</returns>
    TiffImageDefinition CopyMetadataToDefinition(TiffImage image, TiffImageDefinition destination);
    /// <summary>Copies metadata into a writer image.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="imageIndex">The destination image index.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics CopyMetadataToWriter(TiffImage image, TiffWriter writer, int imageIndex);
    /// <summary>Copies raw image data into a result.</summary>
    /// <param name="image">The source image.</param>
    /// <returns>The copy result.</returns>
    TiffRawImageDataCopyResult CopyRawImageData(TiffImage image);
    /// <summary>Copies raw image data with segment progress and cancellation.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The copy result.</returns>
    /// <exception cref="System.OperationCanceledException">The cancellation token was canceled.</exception>
    TiffRawImageDataCopyResult CopyRawImageDataWithCancellation(TiffImage image, ITiffProgress? progress, CancellationToken cancellationToken);
    /// <summary>Copies raw image data into an image definition.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="destination">The destination definition.</param>
    /// <returns>The copy result.</returns>
    TiffRawImageDataCopyResult CopyRawImageDataToDefinition(TiffImage image, TiffImageDefinition destination);
    /// <summary>Copies raw image data into a writer image.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="imageIndex">The destination image index.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics CopyRawImageDataToWriter(TiffImage image, TiffWriter writer, int imageIndex);
    /// <summary>Rewrites one image with metadata changes and optional encoded image-data pass-through.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="options">Metadata changes and preservation policy.</param>
    /// <returns>The rewrite compatibility and outcome.</returns>
    TiffMetadataRewriteResult RewriteImageMetadata(TiffImage image, TiffWriter writer, TiffMetadataRewriteOptions options);
}
