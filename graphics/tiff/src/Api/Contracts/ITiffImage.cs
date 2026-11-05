using System;
using System.Threading;

namespace tiff;

/// <summary>Defines image-level TIFF reading operations.</summary>
public interface ITiffImage
{
    /// <summary>Indicates whether the most recent operation succeeded.</summary>
    /// <returns><c>true</c> when no error diagnostic is present.</returns>
    bool GetSuccess();
    /// <summary>Indicates whether the parent reader remains open.</summary>
    /// <returns><c>true</c> while source-backed operations are permitted.</returns>
    bool GetIsOpen();
    /// <summary>Indicates whether image payload reads are enabled.</summary>
    /// <returns><c>true</c> for an open image from a full-reader handle.</returns>
    bool GetCanReadImageData();
    /// <summary>Returns the image index.</summary>
    /// <returns>The zero-based image index.</returns>
    int GetIndex();
    /// <summary>Returns the source header.</summary>
    /// <returns>A detached header.</returns>
    TiffHeader GetHeader();
    /// <summary>Returns the image directory.</summary>
    /// <returns>A detached directory.</returns>
    TiffDirectory GetDirectory();
    /// <summary>Returns normalized image information.</summary>
    /// <returns>The image information.</returns>
    TiffImageInfo GetInfo();
    /// <summary>Returns all image tags.</summary>
    /// <returns>Detached tags.</returns>
    TiffIfdEntry[] GetTags();
    /// <summary>Returns one image tag.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns>The tag, or <c>null</c>.</returns>
    TiffIfdEntry? GetTag(int tagId);
    /// <summary>Returns one decoded tag value.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns>The decoded value, or <c>null</c> when the tag is absent.</returns>
    TiffTagValue? GetTagValue(int tagId);
    /// <summary>Returns every decoded tag value in its original directory order.</summary>
    /// <returns>Detached decoded values, including values for unregistered tags.</returns>
    TiffTagValue[] GetTagValues();
    /// <summary>Returns one tag's encoded bytes without semantic conversion.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns>The encoded bytes, or an empty array when the tag is absent or cannot be read.</returns>
    byte[] GetRawTagBytes(int tagId);
    /// <summary>Returns every tag's encoded bytes in its original directory order.</summary>
    /// <returns>Detached encoded byte arrays.</returns>
    byte[][] GetRawTagBytesInOriginalOrder();
    /// <summary>Indicates whether one tag value has been materialized from the source.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns><c>true</c> after the first raw or typed value access.</returns>
    bool GetIsTagValueLoaded(int tagId);
    /// <summary>Returns one scalar unsigned value from BYTE, SHORT, LONG, IFD, LONG8, or IFD8 storage.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns>The scalar value, or zero when the value cannot be returned.</returns>
    ulong GetUnsignedTagScalar(int tagId);
    /// <summary>Returns unsigned values from BYTE, SHORT, LONG, IFD, LONG8, or IFD8 storage.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns>Detached unsigned values.</returns>
    ulong[] GetUnsignedTagArray(int tagId);
    /// <summary>Returns one scalar signed value from SBYTE, SSHORT, SLONG, or SLONG8 storage.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns>The scalar value, or zero when the value cannot be returned.</returns>
    long GetSignedTagScalar(int tagId);
    /// <summary>Returns signed values from SBYTE, SSHORT, SLONG, or SLONG8 storage.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns>Detached signed values.</returns>
    long[] GetSignedTagArray(int tagId);
    /// <summary>Returns one scalar RATIONAL or SRATIONAL value.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns>The scalar rational, or zero over one when the value cannot be returned.</returns>
    TiffRational GetRationalTagScalar(int tagId);
    /// <summary>Returns RATIONAL or SRATIONAL values.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns>Detached rational values.</returns>
    TiffRational[] GetRationalTagArray(int tagId);
    /// <summary>Returns one scalar FLOAT or DOUBLE value.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns>The scalar value, or zero when the value cannot be returned.</returns>
    double GetFloatingTagScalar(int tagId);
    /// <summary>Returns FLOAT or DOUBLE values.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns>Detached floating-point values.</returns>
    double[] GetFloatingTagArray(int tagId);
    /// <summary>Returns one ASCII string.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns>The string, or an empty string when the value cannot be returned.</returns>
    string GetAsciiTagScalar(int tagId);
    /// <summary>Returns the NUL-separated strings stored by one ASCII tag.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns>Detached strings.</returns>
    string[] GetAsciiTagArray(int tagId);
    /// <summary>Returns a present or default tag value.</summary>
    /// <param name="tagId">The TIFF tag identifier.</param>
    /// <returns>The lookup result.</returns>
    TiffTagDefaultResult GetTagDefaulted(int tagId);
    /// <summary>Reads EXIF metadata using the image's directory and source.</summary>
    /// <returns>The EXIF metadata and diagnostics.</returns>
    TiffExifReadResult ReadExif();
    /// <summary>Reads GPS metadata using the image's directory and source.</summary>
    /// <returns>The GPS metadata and diagnostics.</returns>
    TiffGpsReadResult ReadGps();
    /// <summary>Reads the XMP packet associated with this image.</summary>
    /// <returns>The XMP packet and diagnostics.</returns>
    TiffXmpReadResult ReadXmp();
    /// <summary>Reads the ICC profile associated with this image.</summary>
    /// <returns>The ICC profile and diagnostics.</returns>
    TiffIccProfileReadResult ReadIccProfile();
    /// <summary>Reads Photoshop image-resource metadata associated with this image.</summary>
    /// <returns>The Photoshop metadata and diagnostics.</returns>
    TiffPhotoshopReadResult ReadPhotoshop();
    /// <summary>Reads IPTC metadata associated with this image.</summary>
    /// <returns>The IPTC metadata and diagnostics.</returns>
    TiffIptcReadResult ReadIptc();
    /// <summary>Reads the exact ImageDescription text, values, and encoded bytes.</summary>
    /// <returns>The image-description metadata and diagnostics.</returns>
    TiffImageDescriptionReadResult ReadImageDescription();
    /// <summary>Reads OME metadata from this image's ImageDescription.</summary>
    /// <returns>The OME metadata and diagnostics.</returns>
    TiffOmeMetadataReadResult ReadOmeMetadata();
    /// <summary>Reads ImageJ description, calibration, and private metadata.</summary>
    /// <returns>The ImageJ metadata and diagnostics.</returns>
    TiffImageJMetadataReadResult ReadImageJMetadata();
    /// <summary>Reads GDAL metadata and no-data values.</summary>
    /// <returns>The GDAL metadata and diagnostics.</returns>
    TiffGdalMetadataReadResult ReadGdalMetadata();
    /// <summary>Reads the GeoTIFF key directory.</summary>
    /// <returns>The geographic key directory and diagnostics.</returns>
    TiffGeoKeyDirectoryReadResult ReadGeoKeyDirectory();
    /// <summary>Reads GeoTIFF model scale, tiepoint, and transformation metadata.</summary>
    /// <returns>The geographic model metadata and diagnostics.</returns>
    TiffGeoModelReadResult ReadGeoModel();
    /// <summary>Reads GeoTIFF double and ASCII parameter pools.</summary>
    /// <returns>The geographic parameters and diagnostics.</returns>
    TiffGeoParameterReadResult ReadGeoParameters();
    /// <summary>Recognizes supported vendor metadata signatures and tags.</summary>
    /// <returns>The recognized vendor metadata and diagnostics.</returns>
    TiffVendorMetadataReadResult ReadVendorMetadata();
    /// <summary>Returns the subimage count.</summary>
    /// <returns>The subimage count.</returns>
    int GetSubImageCount();
    /// <summary>Returns one subimage.</summary>
    /// <param name="subImageIndex">The zero-based subimage index.</param>
    /// <returns>The requested subimage.</returns>
    TiffImage GetSubImage(int subImageIndex);
    /// <summary>Detects reduced-resolution SubIFD pyramid levels beneath this image.</summary>
    /// <returns>The detected pyramid and diagnostics.</returns>
    TiffPyramidDetectionResult GetPyramid();
    /// <summary>Returns the number of directly navigable pyramid levels, including the base image as level zero.</summary>
    /// <returns>The pyramid level count.</returns>
    int GetPyramidLevelCount();
    /// <summary>Returns one pyramid level, where level zero is the base image.</summary>
    /// <param name="levelIndex">The zero-based pyramid level index.</param>
    /// <returns>The selected level image.</returns>
    TiffImage GetPyramidLevel(int levelIndex);
    /// <summary>Looks up the tile containing a coordinate at a selected pyramid level.</summary>
    /// <param name="levelIndex">The zero-based pyramid level index, with zero identifying the base image.</param>
    /// <param name="column">The level-relative pixel column.</param>
    /// <param name="row">The level-relative pixel row.</param>
    /// <param name="depth">The level-relative depth coordinate.</param>
    /// <param name="sampleIndex">The zero-based sample plane for separate data.</param>
    /// <returns>The tile lookup and diagnostics.</returns>
    TiffTileLookupResult LookupPyramidTile(int levelIndex, ulong column, ulong row, ulong depth, int sampleIndex);
    /// <summary>Reads the stored bytes for the tile containing a coordinate at a selected pyramid level.</summary>
    /// <param name="levelIndex">The zero-based pyramid level index, with zero identifying the base image.</param>
    /// <param name="column">The level-relative pixel column.</param>
    /// <param name="row">The level-relative pixel row.</param>
    /// <param name="depth">The level-relative depth coordinate.</param>
    /// <param name="sampleIndex">The zero-based sample plane for separate data.</param>
    /// <returns>The selected raw tile and diagnostics.</returns>
    TiffRawTileReadResult ReadPyramidTile(int levelIndex, ulong column, ulong row, ulong depth, int sampleIndex);
    /// <summary>Returns the strip count.</summary>
    /// <returns>The strip count.</returns>
    ulong GetStripCount();
    /// <summary>Returns the tile count.</summary>
    /// <returns>The tile count.</returns>
    ulong GetTileCount();
    /// <summary>Returns the decoded scanline size.</summary>
    /// <returns>The byte count.</returns>
    ulong GetScanlineSize();
    /// <summary>Returns the decoded scanline size for one sample plane.</summary>
    /// <param name="sampleIndex">The zero-based sample index.</param>
    /// <returns>The byte count for the selected plane, or the interleaved byte count for contiguous data.</returns>
    ulong GetSampleScanlineSize(int sampleIndex);
    /// <summary>Reads and decodes one scanline into a caller-provided buffer.</summary>
    /// <param name="row">The zero-based image row.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics ReadScanlineToBuffer(ulong row, Span<byte> destination);
    /// <summary>Reads and decodes one scanline for a selected sample plane into a caller-provided buffer.</summary>
    /// <param name="row">The zero-based image row.</param>
    /// <param name="sampleIndex">The zero-based sample index. Contiguous data returns the complete interleaved row.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics ReadSampleScanlineToBuffer(ulong row, int sampleIndex, Span<byte> destination);
    /// <summary>Returns the output layout and caller-buffer size for a rectangular region read.</summary>
    /// <param name="options">The region bounds and output-layout selection.</param>
    /// <returns>The validated region-read information.</returns>
    TiffRegionReadInfo GetRegionReadInfo(TiffRegionReadOptions options);
    /// <summary>Reads and decodes a rectangular region into a caller-provided buffer.</summary>
    /// <param name="options">The region bounds and output-layout selection.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics ReadRegionToBuffer(TiffRegionReadOptions options, Span<byte> destination);
    /// <summary>Reads and decodes a rectangular region with row progress and cancellation.</summary>
    /// <param name="options">The region bounds and output-layout selection.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Operation diagnostics.</returns>
    /// <exception cref="System.OperationCanceledException">The cancellation token was canceled.</exception>
    TiffDiagnostics ReadRegionToBufferWithCancellation(TiffRegionReadOptions options, Span<byte> destination, ITiffProgress? progress, CancellationToken cancellationToken);
    /// <summary>Returns the exact output layout and caller-buffer size for a complete source-raster read.</summary>
    /// <returns>The source-raster read information.</returns>
    TiffRasterReadInfo GetSourceRasterReadInfo();
    /// <summary>Reads the complete decoded source raster into a caller-provided buffer.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics ReadSourceRasterToBuffer(Span<byte> destination);
    /// <summary>Returns the exact output layout and caller-buffer size for one source sample plane.</summary>
    /// <param name="sampleIndex">The zero-based source sample index.</param>
    /// <returns>The sample-plane read information.</returns>
    TiffRasterReadInfo GetSamplePlaneReadInfo(int sampleIndex);
    /// <summary>Reads one decoded source sample plane into a caller-provided buffer.</summary>
    /// <param name="sampleIndex">The zero-based source sample index.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics ReadSamplePlaneToBuffer(int sampleIndex, Span<byte> destination);
    /// <summary>Reads the complete image as interleaved red, green, blue, and alpha bytes.</summary>
    /// <param name="options">The display conversion controls.</param>
    /// <returns>The converted RGBA8 raster and diagnostics.</returns>
    TiffRgba8ReadResult ReadRgba8(TiffRgbaReadOptions options);
    /// <summary>Reads the complete image as interleaved red, green, blue, and alpha 16-bit samples.</summary>
    /// <param name="options">The display conversion controls.</param>
    /// <returns>The converted RGBA16 raster and diagnostics.</returns>
    TiffRgba16ReadResult ReadRgba16(TiffRgbaReadOptions options);
    /// <summary>Returns one decoded strip size.</summary>
    /// <param name="stripIndex">The zero-based strip index.</param>
    /// <returns>The byte count.</returns>
    ulong GetStripSize(int stripIndex);
    /// <summary>Returns one decoded tile size.</summary>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <returns>The byte count.</returns>
    ulong GetTileSize(int tileIndex);
    /// <summary>Returns the normalized strip layout, including a virtual chopped view when enabled by the read memory policy.</summary>
    /// <returns>The strip-layout result.</returns>
    TiffStripLayoutResult GetStripLayout();
    /// <summary>Returns an optional virtual chopped-strip view of a large uncompressed single strip per plane.</summary>
    /// <param name="targetDecodedByteCount">The preferred maximum decoded byte count of each virtual strip.</param>
    /// <returns>The strip-layout result.</returns>
    TiffStripLayoutResult GetChoppedStripLayout(ulong targetDecodedByteCount);
    /// <summary>Returns the normalized tile layout.</summary>
    /// <returns>The tile-layout result.</returns>
    TiffTileLayoutResult GetTileLayout();
    /// <summary>Reads one raw strip.</summary>
    /// <param name="stripIndex">The zero-based strip index.</param>
    /// <returns>The read result.</returns>
    TiffRawStripReadResult ReadRawStrip(int stripIndex);
    /// <summary>Reads one raw strip into a caller buffer.</summary>
    /// <param name="stripIndex">The zero-based strip index.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics ReadRawStripToBuffer(int stripIndex, Span<byte> destination);
    /// <summary>Reads and decodes one strip.</summary>
    /// <param name="stripIndex">The zero-based strip index.</param>
    /// <returns>The read result.</returns>
    TiffRawStripReadResult ReadEncodedStrip(int stripIndex);
    /// <summary>Reads and decodes one strip into a caller buffer.</summary>
    /// <param name="stripIndex">The zero-based strip index.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics ReadEncodedStripToBuffer(int stripIndex, Span<byte> destination);
    /// <summary>Reads one raw tile.</summary>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <returns>The read result.</returns>
    TiffRawTileReadResult ReadRawTile(int tileIndex);
    /// <summary>Reads one raw tile into a caller buffer.</summary>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics ReadRawTileToBuffer(int tileIndex, Span<byte> destination);
    /// <summary>Reads and decodes one tile.</summary>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <returns>The read result.</returns>
    TiffRawTileReadResult ReadEncodedTile(int tileIndex);
    /// <summary>Reads and decodes one tile into a caller buffer.</summary>
    /// <param name="tileIndex">The zero-based tile index.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>Operation diagnostics.</returns>
    TiffDiagnostics ReadEncodedTileToBuffer(int tileIndex, Span<byte> destination);
    /// <summary>Returns collected diagnostics.</summary>
    /// <returns>A detached diagnostic collection.</returns>
    TiffDiagnostics GetDiagnostics();
    /// <summary>Returns the shared random-access source backing this image.</summary>
    /// <returns>The source used for deferred reads.</returns>
    ITiffRandomAccessSource GetSource();
    /// <summary>Returns the source bytes.</summary>
    /// <returns>A detached byte array.</returns>
    byte[] GetFileBytes();
    /// <summary>Reads the image's SubIFD references.</summary>
    /// <returns>The SubIFD read result.</returns>
    TiffSubIfdReadResult ReadSubIfds();
    /// <summary>Reads nested EXIF, GPS, and interoperability IFD pointers.</summary>
    /// <returns>The pointer traversal result.</returns>
    TiffIfdPointerReadResult ReadIfdPointers();
}
