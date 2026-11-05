using System;
using System.IO;
using System.Threading;

namespace tiff;

/// <summary>Validates TIFF and Cloud Optimized GeoTIFF structure.</summary>
public class TiffValidationService : ITiffValidationService
{
    /// <summary>Creates a TIFF validation service.</summary>
    public TiffValidationService()
    {
    }

    /// <summary>Validates complete file bytes.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    public TiffValidationResult ValidateBytes(byte[] fileBytes, TiffReadOptions options)
    {
        return TiffValidator.Validate(fileBytes, options);
    }

    /// <summary>Validates file memory.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    public TiffValidationResult ValidateMemory(ReadOnlySpan<byte> fileBytes, TiffReadOptions options)
    {
        return TiffValidator.Validate(fileBytes, options);
    }

    /// <summary>Validates a file path.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    public TiffValidationResult ValidatePath(string path, TiffReadOptions options)
    {
        return TiffValidator.Validate(path, options);
    }

    /// <summary>Validates a stream.</summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    public TiffValidationResult ValidateStream(Stream stream, TiffReadOptions options)
    {
        return TiffValidator.Validate(stream, options);
    }

    /// <summary>Validates an opened file.</summary>
    /// <param name="file">The opened file.</param>
    /// <returns>The validation result.</returns>
    public TiffValidationResult ValidateFile(TiffFile file)
    {
        return TiffValidator.Validate(file);
    }

    /// <summary>Validates an opened file with progress reporting and cancellation.</summary>
    /// <param name="file">The opened file.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    /// <exception cref="System.OperationCanceledException">The cancellation token was canceled.</exception>
    public TiffValidationResult ValidateFileWithCancellation(TiffFile file, ITiffProgress? progress, CancellationToken cancellationToken)
    {
        TiffOperationMonitor monitor = new TiffOperationMonitor(TiffProgressOperation.Validation, progress, cancellationToken);
        return TiffValidator.ValidateWithOperation(file, monitor);
    }

    /// <summary>Validates complete file bytes as a Cloud Optimized GeoTIFF.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    public TiffCloudOptimizedGeoTiffValidationResult ValidateCloudOptimizedBytes(byte[] fileBytes, TiffReadOptions options)
    {
        return TiffCloudOptimizedGeoTiffValidator.Validate(fileBytes, options);
    }

    /// <summary>Validates file memory as a Cloud Optimized GeoTIFF.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    public TiffCloudOptimizedGeoTiffValidationResult ValidateCloudOptimizedMemory(ReadOnlySpan<byte> fileBytes, TiffReadOptions options)
    {
        return TiffCloudOptimizedGeoTiffValidator.Validate(fileBytes, options);
    }

    /// <summary>Validates a path as a Cloud Optimized GeoTIFF.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    public TiffCloudOptimizedGeoTiffValidationResult ValidateCloudOptimizedPath(string path, TiffReadOptions options)
    {
        return TiffCloudOptimizedGeoTiffValidator.Validate(path, options);
    }

    /// <summary>Validates a stream as a Cloud Optimized GeoTIFF.</summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    public TiffCloudOptimizedGeoTiffValidationResult ValidateCloudOptimizedStream(Stream stream, TiffReadOptions options)
    {
        return TiffCloudOptimizedGeoTiffValidator.Validate(stream, options);
    }

    /// <summary>Validates an opened file as a Cloud Optimized GeoTIFF.</summary>
    /// <param name="file">The opened file.</param>
    /// <returns>The validation result.</returns>
    public TiffCloudOptimizedGeoTiffValidationResult ValidateCloudOptimizedFile(TiffFile file)
    {
        return TiffCloudOptimizedGeoTiffValidator.Validate(file);
    }

    /// <summary>Validates a random-access source as a Cloud Optimized GeoTIFF without materializing the complete source.</summary>
    /// <param name="source">The open random-access source.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    public TiffCloudOptimizedGeoTiffValidationResult ValidateCloudOptimizedSource(ITiffRandomAccessSource source, TiffReadOptions options)
    {
        TiffFile file = TiffFile.OpenMetadataSource(source, options);
        return TiffCloudOptimizedGeoTiffValidator.ValidateAndClose(file);
    }
}
