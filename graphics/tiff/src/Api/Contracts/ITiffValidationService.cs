using System;
using System.IO;
using System.Threading;

namespace tiff;

/// <summary>Defines TIFF and Cloud Optimized GeoTIFF validation operations.</summary>
public interface ITiffValidationService
{
    /// <summary>Validates complete file bytes.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    TiffValidationResult ValidateBytes(byte[] fileBytes, TiffReadOptions options);
    /// <summary>Validates file memory.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    TiffValidationResult ValidateMemory(ReadOnlySpan<byte> fileBytes, TiffReadOptions options);
    /// <summary>Validates a file path.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    TiffValidationResult ValidatePath(string path, TiffReadOptions options);
    /// <summary>Validates a stream.</summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    TiffValidationResult ValidateStream(Stream stream, TiffReadOptions options);
    /// <summary>Validates an opened file.</summary>
    /// <param name="file">The opened file.</param>
    /// <returns>The validation result.</returns>
    TiffValidationResult ValidateFile(TiffFile file);
    /// <summary>Validates an opened file with progress reporting and cancellation.</summary>
    /// <param name="file">The opened file.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    /// <exception cref="System.OperationCanceledException">The cancellation token was canceled.</exception>
    TiffValidationResult ValidateFileWithCancellation(TiffFile file, ITiffProgress? progress, CancellationToken cancellationToken);
    /// <summary>Validates complete file bytes as a Cloud Optimized GeoTIFF.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    TiffCloudOptimizedGeoTiffValidationResult ValidateCloudOptimizedBytes(byte[] fileBytes, TiffReadOptions options);
    /// <summary>Validates file memory as a Cloud Optimized GeoTIFF.</summary>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    TiffCloudOptimizedGeoTiffValidationResult ValidateCloudOptimizedMemory(ReadOnlySpan<byte> fileBytes, TiffReadOptions options);
    /// <summary>Validates a path as a Cloud Optimized GeoTIFF.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    TiffCloudOptimizedGeoTiffValidationResult ValidateCloudOptimizedPath(string path, TiffReadOptions options);
    /// <summary>Validates a stream as a Cloud Optimized GeoTIFF.</summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    TiffCloudOptimizedGeoTiffValidationResult ValidateCloudOptimizedStream(Stream stream, TiffReadOptions options);
    /// <summary>Validates an opened file as a Cloud Optimized GeoTIFF.</summary>
    /// <param name="file">The opened file.</param>
    /// <returns>The validation result.</returns>
    TiffCloudOptimizedGeoTiffValidationResult ValidateCloudOptimizedFile(TiffFile file);
    /// <summary>Validates a random-access source as a Cloud Optimized GeoTIFF without materializing the complete source.</summary>
    /// <param name="source">The open random-access source.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The validation result.</returns>
    TiffCloudOptimizedGeoTiffValidationResult ValidateCloudOptimizedSource(ITiffRandomAccessSource source, TiffReadOptions options);
}
