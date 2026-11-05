using System;
using System.IO;
using System.Threading;

namespace tiff;

/// <summary>Defines operations for building a TIFF file.</summary>
public interface ITiffWriter
{
    /// <summary>Indicates whether the most recent operation succeeded.</summary>
    /// <returns><c>true</c> when no error diagnostic is present.</returns>
    bool GetSuccess();
    /// <summary>Indicates whether the writer remains open.</summary>
    /// <returns><c>true</c> while operations are accepted.</returns>
    bool GetIsOpen();
    /// <summary>Adds a top-level image.</summary>
    /// <param name="image">The image definition.</param>
    /// <returns>The assigned image index.</returns>
    int AddImage(TiffImageDefinition image);
    /// <summary>Adds a top-level image from validated pixel and storage configuration.</summary>
    /// <param name="configuration">The source pixel, dimensions, and physical layout configuration.</param>
    /// <returns>The assigned image index, or <c>-1</c> when the configuration is invalid.</returns>
    int AddConfiguredImage(TiffWriterImageConfiguration configuration);
    /// <summary>Adds a child image.</summary>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="image">The child definition.</param>
    /// <returns>The assigned child index.</returns>
    int AddSubImage(int parentImageIndex, TiffImageDefinition image);
    /// <summary>Adds a configured child image stored through the parent's SubIFDs tag.</summary>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="configuration">The child pixel, dimensions, and physical layout configuration.</param>
    /// <returns>The assigned child index, or <c>-1</c> when validation fails.</returns>
    int AddConfiguredSubImage(int parentImageIndex, TiffWriterImageConfiguration configuration);
    /// <summary>Adds an ordered reduced-resolution pyramid level stored through the parent's SubIFDs tag.</summary>
    /// <param name="parentImageIndex">The configured base image index.</param>
    /// <param name="configuration">The overview pixel, dimensions, compression, and physical layout configuration.</param>
    /// <returns>The assigned child index, or <c>-1</c> when validation fails.</returns>
    int AddPyramidLevel(int parentImageIndex, TiffWriterImageConfiguration configuration);
    /// <summary>Adds a tiled georeferenced base image and ordered internal overviews using Cloud Optimized GeoTIFF layout.</summary>
    /// <param name="configuration">The base image, overview, and GeoTIFF metadata configuration.</param>
    /// <returns>The assigned base and child image indices with validation diagnostics.</returns>
    TiffCloudOptimizedGeoTiffWriteResult AddCloudOptimizedGeoTiff(TiffCloudOptimizedGeoTiffWriteConfiguration configuration);
    /// <summary>Writes or replaces one tag.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="value">The tag value.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteTag(int imageIndex, TiffTagValue value);
    /// <summary>Adds or replaces a batch of non-structural metadata tags atomically.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="values">The metadata values to add or replace.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics UpdateMetadata(int imageIndex, TiffTagValue[] values);
    /// <summary>Removes a batch of metadata tags without changing image-layout tags.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="tagIds">The metadata tag identifiers to remove.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics RemoveMetadata(int imageIndex, int[] tagIds);
    /// <summary>Writes or replaces the EXIF child IFD of an image.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="metadata">The EXIF metadata values.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteExif(int imageIndex, TiffExifMetadata metadata);
    /// <summary>Writes or replaces the GPS child IFD of an image.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="metadata">The GPS metadata values.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteGps(int imageIndex, TiffGpsMetadata metadata);
    /// <summary>Writes or replaces an XMP packet.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="packet">The XMP packet.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteXmp(int imageIndex, TiffXmpPacket packet);
    /// <summary>Writes or replaces an ICC profile.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="profile">The ICC profile.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteIccProfile(int imageIndex, TiffIccProfile profile);
    /// <summary>Writes or replaces Photoshop image resources.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="metadata">The Photoshop image-resource metadata.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WritePhotoshop(int imageIndex, TiffPhotoshopMetadata metadata);
    /// <summary>Writes or replaces IPTC datasets.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="metadata">The IPTC metadata.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteIptc(int imageIndex, TiffIptcMetadata metadata);
    /// <summary>Writes or replaces ImageDescription text.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="description">The description text.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteImageDescription(int imageIndex, string description);
    /// <summary>Writes or replaces an OME-XML ImageDescription.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="metadata">The OME metadata containing valid OME XML.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteOmeMetadata(int imageIndex, TiffOmeMetadata metadata);
    /// <summary>Writes or replaces an ImageJ ImageDescription.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="metadata">The ImageJ metadata.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteImageJMetadata(int imageIndex, TiffImageJMetadata metadata);
    /// <summary>Writes or replaces GDAL metadata and no-data tags.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="metadata">The GDAL metadata.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteGdalMetadata(int imageIndex, TiffGdalMetadata metadata);
    /// <summary>Writes or replaces GeoTIFF model, key-directory, and parameter tags.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="model">The geographic model metadata.</param>
    /// <param name="keyDirectory">The geographic key directory.</param>
    /// <param name="parameters">The geographic parameter pools.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteGeoTiffMetadata(int imageIndex, TiffGeoModelMetadata model, TiffGeoKeyDirectory keyDirectory, TiffGeoParameterMetadata parameters);
    /// <summary>Writes one raw strip.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="stripIndex">The strip index.</param>
    /// <param name="source">The source bytes.</param>
    /// <returns>The write result.</returns>
    TiffRawStripWriteResult WriteRawStrip(int imageIndex, int stripIndex, ReadOnlySpan<byte> source);
    /// <summary>Encodes and writes one strip.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="stripIndex">The strip index.</param>
    /// <param name="source">The decoded bytes.</param>
    /// <returns>The write result.</returns>
    TiffRawStripWriteResult WriteEncodedStrip(int imageIndex, int stripIndex, ReadOnlySpan<byte> source);
    /// <summary>Writes one raw tile.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="tileIndex">The tile index.</param>
    /// <param name="source">The source bytes.</param>
    /// <returns>The write result.</returns>
    TiffRawTileWriteResult WriteRawTile(int imageIndex, int tileIndex, ReadOnlySpan<byte> source);
    /// <summary>Encodes and writes one tile.</summary>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="tileIndex">The tile index.</param>
    /// <param name="source">The decoded bytes.</param>
    /// <returns>The write result.</returns>
    TiffRawTileWriteResult WriteEncodedTile(int imageIndex, int tileIndex, ReadOnlySpan<byte> source);
    /// <summary>Writes one complete source scanline in interleaved pixel order, independent of the configured physical planar organization.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="row">The zero-based image row.</param>
    /// <param name="source">The byte-aligned source samples in pixel and sample order.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteScanline(int imageIndex, ulong row, ReadOnlySpan<byte> source);
    /// <summary>Writes one sample from every pixel in a scanline in increasing column order.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="row">The zero-based image row.</param>
    /// <param name="sampleIndex">The zero-based source sample index.</param>
    /// <param name="source">The byte-aligned sample values in increasing column order.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteSampleScanline(int imageIndex, ulong row, int sampleIndex, ReadOnlySpan<byte> source);
    /// <summary>Writes a rectangular source region in row-major interleaved pixel order.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="region">The destination image bounds.</param>
    /// <param name="source">The byte-aligned source samples in row, pixel, and sample order.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteRegion(int imageIndex, TiffRegion region, ReadOnlySpan<byte> source);
    /// <summary>Writes one sample across a rectangular region in row-major pixel order.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="region">The destination image bounds.</param>
    /// <param name="sampleIndex">The zero-based source sample index.</param>
    /// <param name="source">The byte-aligned sample values in row-major pixel order.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteSampleRegion(int imageIndex, TiffRegion region, int sampleIndex, ReadOnlySpan<byte> source);
    /// <summary>Writes one complete sample plane in row-major pixel order.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="sampleIndex">The zero-based source sample index.</param>
    /// <param name="source">The byte-aligned sample values in row-major pixel order.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteSamplePlane(int imageIndex, int sampleIndex, ReadOnlySpan<byte> source);
    /// <summary>Writes the complete source raster in row-major interleaved pixel order.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="source">The byte-aligned source samples, with multibyte values in host byte order.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteSourceRaster(int imageIndex, ReadOnlySpan<byte> source);
    /// <summary>Writes the complete source raster with segment progress and cancellation.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="source">The byte-aligned source samples, with multibyte values in host byte order.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Operation diagnostics.</returns>
    /// <exception cref="System.OperationCanceledException">The cancellation token was canceled.</exception>
    TiffDiagnostics WriteSourceRasterWithCancellation(int imageIndex, ReadOnlySpan<byte> source, ITiffProgress? progress, CancellationToken cancellationToken);
    /// <summary>Converts and writes a complete row-major RGBA8 raster to the configured source pixel format.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="rgba">Red, green, blue, and alpha bytes for every pixel.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteRgba8(int imageIndex, ReadOnlySpan<byte> rgba);
    /// <summary>Converts and writes a complete row-major RGBA16 raster to the configured source pixel format.</summary>
    /// <param name="imageIndex">The configured image index.</param>
    /// <param name="rgba">Red, green, blue, and alpha samples for every pixel.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteRgba16(int imageIndex, ReadOnlySpan<ushort> rgba);
    /// <summary>Writes the complete source raster of a configured child image in row-major interleaved pixel order.</summary>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="subImageIndex">The configured child image index.</param>
    /// <param name="source">The byte-aligned source samples, with multibyte values in host byte order.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteSubImageSourceRaster(int parentImageIndex, int subImageIndex, ReadOnlySpan<byte> source);
    /// <summary>Converts and writes a complete row-major RGBA8 raster to a configured child image.</summary>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="subImageIndex">The configured child image index.</param>
    /// <param name="rgba">Red, green, blue, and alpha bytes for every pixel.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteSubImageRgba8(int parentImageIndex, int subImageIndex, ReadOnlySpan<byte> rgba);
    /// <summary>Converts and writes a complete row-major RGBA16 raster to a configured child image.</summary>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="subImageIndex">The configured child image index.</param>
    /// <param name="rgba">Red, green, blue, and alpha samples for every pixel.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics WriteSubImageRgba16(int parentImageIndex, int subImageIndex, ReadOnlySpan<ushort> rgba);
    /// <summary>Materializes the current file.</summary>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics Flush();
    /// <summary>Flushes and closes the writer.</summary>
    void Close();
    /// <summary>Returns the generated file bytes.</summary>
    /// <returns>A detached byte array.</returns>
    byte[] GetFileBytes();
    /// <summary>Returns the generated byte count.</summary>
    /// <returns>The byte count.</returns>
    int GetFileByteCount();
    /// <summary>Returns collected diagnostics.</summary>
    /// <returns>A detached diagnostic collection.</returns>
    TiffDiagnostics GetDiagnostics();
    /// <summary>Returns normalized options.</summary>
    /// <returns>Detached write options.</returns>
    TiffWriteOptions GetOptions();
    /// <summary>Returns the configured output path.</summary>
    /// <returns>The path, or an empty string.</returns>
    string GetPath();
    /// <summary>Returns the configured output stream.</summary>
    /// <returns>The stream, or <see cref="Stream.Null"/>.</returns>
    Stream GetStream();
    /// <summary>Returns the selected container kind.</summary>
    /// <returns>The container kind.</returns>
    TiffFileKind GetFileKind();
    /// <summary>Returns the selected byte order.</summary>
    /// <returns>The byte order.</returns>
    TiffByteOrder GetByteOrder();
}
