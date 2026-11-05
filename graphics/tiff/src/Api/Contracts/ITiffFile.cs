using System;

namespace tiff;

/// <summary>Defines access to a parsed TIFF file and its images.</summary>
public interface ITiffFile
{
    /// <summary>Indicates whether the most recent operation succeeded.</summary>
    /// <returns><c>true</c> when no error diagnostic is present.</returns>
    bool GetSuccess();
    /// <summary>Indicates whether image access remains enabled.</summary>
    /// <returns><c>true</c> while the file is open.</returns>
    bool GetIsOpen();
    /// <summary>Indicates whether image payload reads are enabled.</summary>
    /// <returns><c>true</c> for a live full-reader handle.</returns>
    bool GetCanReadImageData();
    /// <summary>Closes the file handle.</summary>
    void Close();
    /// <summary>Returns a detached header.</summary>
    /// <returns>The parsed TIFF header.</returns>
    TiffHeader GetHeader();
    /// <summary>Returns the top-level image count.</summary>
    /// <returns>The image count.</returns>
    int GetImageCount();
    /// <summary>Returns one top-level image.</summary>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <returns>The requested image.</returns>
    TiffImage GetImage(int imageIndex);
    /// <summary>Returns all top-level images.</summary>
    /// <returns>Detached image handles.</returns>
    TiffImage[] GetImages();
    /// <summary>Returns the number of top-level images marked as reduced-resolution images.</summary>
    /// <returns>The reduced-resolution image count.</returns>
    int GetReducedResolutionImageCount();
    /// <summary>Returns one top-level reduced-resolution image.</summary>
    /// <param name="reducedImageIndex">The zero-based index within the reduced-resolution image collection.</param>
    /// <returns>The selected image.</returns>
    TiffImage GetReducedResolutionImage(int reducedImageIndex);
    /// <summary>Returns the number of top-level thumbnail or reduced-resolution images.</summary>
    /// <returns>The thumbnail count.</returns>
    int GetThumbnailCount();
    /// <summary>Returns one top-level thumbnail or reduced-resolution image.</summary>
    /// <param name="thumbnailIndex">The zero-based index within the thumbnail collection.</param>
    /// <returns>The selected image.</returns>
    TiffImage GetThumbnail(int thumbnailIndex);
    /// <summary>Groups related top-level images into navigable series.</summary>
    /// <returns>The grouped series and diagnostics.</returns>
    TiffSeriesGroupResult GetSeriesGroup();
    /// <summary>Returns the number of grouped image series.</summary>
    /// <returns>The series count.</returns>
    int GetSeriesCount();
    /// <summary>Returns one grouped image series.</summary>
    /// <param name="seriesIndex">The zero-based series index.</param>
    /// <returns>The selected series.</returns>
    TiffSeries GetSeriesByIndex(int seriesIndex);
    /// <summary>Returns the image at explicit channel, depth, and time coordinates in a series.</summary>
    /// <param name="seriesIndex">The zero-based series index.</param>
    /// <param name="channelIndex">The zero-based channel coordinate.</param>
    /// <param name="zIndex">The zero-based depth coordinate.</param>
    /// <param name="timeIndex">The zero-based time coordinate.</param>
    /// <returns>The selected plane image.</returns>
    TiffImage GetSeriesPlaneByCoordinates(int seriesIndex, int channelIndex, int zIndex, int timeIndex);
    /// <summary>Reads the OME dataset description and resolves its local pyramid relationships.</summary>
    /// <returns>The parsed OME dataset and diagnostics.</returns>
    TiffOmeMetadataReadResult GetOmeDataset();
    /// <summary>Returns the local TIFF image mapped to explicit OME channel, depth, and time coordinates.</summary>
    /// <param name="omeImageIndex">The zero-based OME image index.</param>
    /// <param name="channelIndex">The zero-based channel coordinate.</param>
    /// <param name="zIndex">The zero-based depth coordinate.</param>
    /// <param name="timeIndex">The zero-based time coordinate.</param>
    /// <returns>The mapped local TIFF image.</returns>
    TiffImage GetOmePlaneByCoordinates(int omeImageIndex, int channelIndex, int zIndex, int timeIndex);
    /// <summary>Returns one image directory.</summary>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <returns>The requested directory.</returns>
    TiffDirectory GetDirectory(int imageIndex);
    /// <summary>Returns all top-level image directories.</summary>
    /// <returns>Detached image directories.</returns>
    TiffDirectory[] GetDirectories();
    /// <summary>Returns collected diagnostics.</summary>
    /// <returns>A detached diagnostic collection.</returns>
    TiffDiagnostics GetDiagnostics();
    /// <summary>Returns the shared random-access source backing this file.</summary>
    /// <returns>The source used for deferred reads.</returns>
    ITiffRandomAccessSource GetSource();
    /// <summary>Returns the preserved trailing-data byte range.</summary>
    /// <returns>The range following all referenced TIFF structures and payloads.</returns>
    TiffByteRange GetTrailingDataRange();
    /// <summary>Returns preserved trailing bytes.</summary>
    /// <returns>A detached trailing-data byte array.</returns>
    byte[] GetTrailingData();
    /// <summary>Reads preserved trailing bytes into a caller-provided buffer.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics ReadTrailingData(Span<byte> destination);
    /// <summary>Returns the source bytes.</summary>
    /// <returns>A detached byte array.</returns>
    byte[] GetFileBytes();
    /// <summary>Returns the source byte count.</summary>
    /// <returns>The byte count.</returns>
    int GetFileByteCount();
}
