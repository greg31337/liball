using System;
using System.Threading;

namespace tiff;

/// <summary>Defines common TIFF compression, raster conversion, and tag decoding operations.</summary>
public interface ITiffCodecService
{
    /// <summary>Decodes one compressed segment with built-in codecs.</summary>
    /// <param name="context">The codec context.</param>
    /// <param name="source">The compressed bytes.</param>
    /// <returns>The codec result.</returns>
    TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source);
    /// <summary>Decodes one compressed segment with an optional custom codec.</summary>
    /// <param name="context">The codec context.</param>
    /// <param name="source">The compressed bytes.</param>
    /// <param name="compressionCodec">The custom codec.</param>
    /// <returns>The codec result.</returns>
    TiffCodecResult DecodeWithCompressionCodec(TiffCodecContext context, ReadOnlySpan<byte> source, ITiffCompressionCodec? compressionCodec);
    /// <summary>Decodes one compressed segment with progress reporting and cancellation.</summary>
    /// <param name="context">The codec context.</param>
    /// <param name="source">The compressed bytes.</param>
    /// <param name="compressionCodec">The optional custom codec.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The codec result.</returns>
    /// <exception cref="System.OperationCanceledException">The cancellation token was canceled.</exception>
    TiffCodecResult DecodeWithCancellation(TiffCodecContext context, ReadOnlySpan<byte> source, ITiffCompressionCodec? compressionCodec, ITiffProgress? progress, CancellationToken cancellationToken);
    /// <summary>Encodes one decoded segment with built-in codecs.</summary>
    /// <param name="context">The codec context.</param>
    /// <param name="source">The decoded bytes.</param>
    /// <returns>The codec result.</returns>
    TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source);
    /// <summary>Encodes one decoded segment with an optional custom codec.</summary>
    /// <param name="context">The codec context.</param>
    /// <param name="source">The decoded bytes.</param>
    /// <param name="compressionCodec">The custom codec.</param>
    /// <returns>The codec result.</returns>
    TiffCodecResult EncodeWithCompressionCodec(TiffCodecContext context, ReadOnlySpan<byte> source, ITiffCompressionCodec? compressionCodec);
    /// <summary>Reports built-in directional codec and raw-pass-through capabilities.</summary>
    /// <param name="compression">The compression value.</param>
    /// <returns>The capability result and public diagnostics.</returns>
    TiffCompressionCapabilityResult GetCompressionCapability(TiffCompression compression);
    /// <summary>Reports directional codec and raw-pass-through capabilities including a custom codec.</summary>
    /// <param name="compression">The compression value.</param>
    /// <param name="compressionCodec">The custom codec.</param>
    /// <returns>The capability result and public diagnostics.</returns>
    TiffCompressionCapabilityResult GetCompressionCapabilityWithCodec(TiffCompression compression, ITiffCompressionCodec? compressionCodec);
    /// <summary>Checks built-in compression decoding support.</summary>
    /// <param name="compression">The compression value.</param>
    /// <returns><c>true</c> when decoding is supported.</returns>
    bool CanDecodeCompression(TiffCompression compression);
    /// <summary>Checks compression decoding support including a custom codec.</summary>
    /// <param name="compression">The compression value.</param>
    /// <param name="compressionCodec">The custom codec.</param>
    /// <returns><c>true</c> when decoding is supported.</returns>
    bool CanDecodeCompressionWithCodec(TiffCompression compression, ITiffCompressionCodec? compressionCodec);
    /// <summary>Checks built-in compression encoding support.</summary>
    /// <param name="compression">The compression value.</param>
    /// <returns><c>true</c> when encoding is supported.</returns>
    bool CanEncodeCompression(TiffCompression compression);
    /// <summary>Checks compression encoding support including a custom codec.</summary>
    /// <param name="compression">The compression value.</param>
    /// <param name="compressionCodec">The custom codec.</param>
    /// <returns><c>true</c> when encoding is supported.</returns>
    bool CanEncodeCompressionWithCodec(TiffCompression compression, ITiffCompressionCodec? compressionCodec);
    /// <summary>Checks built-in raw-compression pass-through support.</summary>
    /// <param name="compression">The compression value.</param>
    /// <returns><c>true</c> when pass-through is supported.</returns>
    bool CanPassThroughRawCompression(TiffCompression compression);
    /// <summary>Checks raw-compression pass-through support including a custom codec.</summary>
    /// <param name="compression">The compression value.</param>
    /// <param name="compressionCodec">The custom codec.</param>
    /// <returns><c>true</c> when pass-through is supported.</returns>
    bool CanPassThroughRawCompressionWithCodec(TiffCompression compression, ITiffCompressionCodec? compressionCodec);
    /// <summary>Checks whether built-in codecs support both compression decoding and encoding.</summary>
    /// <param name="compression">The compression value.</param>
    /// <returns><c>true</c> when supported.</returns>
    bool IsCompressionSupported(TiffCompression compression);
    /// <summary>Checks whether custom and built-in codecs together support both compression decoding and encoding.</summary>
    /// <param name="compression">The compression value.</param>
    /// <param name="compressionCodec">The custom codec.</param>
    /// <returns><c>true</c> when supported.</returns>
    bool IsCompressionSupportedWithCodec(TiffCompression compression, ITiffCompressionCodec? compressionCodec);
    /// <summary>Converts source samples to normalized doubles.</summary>
    /// <param name="context">The codec context.</param>
    /// <param name="source">The sample bytes.</param>
    /// <param name="diagnostics">The diagnostic collection.</param>
    /// <returns>The converted samples.</returns>
    double[] ConvertSamplesToDouble(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics);
    /// <summary>Converts normalized source samples to an RGBA8 raster using the requested color and alpha controls.</summary>
    /// <param name="info">The source image information.</param>
    /// <param name="samples">The normalized source samples.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The raster result.</returns>
    TiffRgba8ReadResult ConvertSamplesToRgba8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffRgbaReadOptions options);
    /// <summary>Converts normalized source samples to an RGBA16 raster using the requested color and alpha controls.</summary>
    /// <param name="info">The source image information.</param>
    /// <param name="samples">The normalized source samples.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The raster result.</returns>
    TiffRgba16ReadResult ConvertSamplesToRgba16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffRgbaReadOptions options);
    /// <summary>Applies the image orientation to interleaved pixels.</summary>
    /// <param name="info">The image information.</param>
    /// <param name="source">The source pixels.</param>
    /// <param name="bytesPerPixel">The bytes per pixel.</param>
    /// <param name="diagnostics">The diagnostic collection.</param>
    /// <returns>The transformation result.</returns>
    TiffOrientationTransformResult ApplyOrientation(TiffImageInfo info, ReadOnlySpan<byte> source, int bytesPerPixel, TiffDiagnostics diagnostics);
    /// <summary>Reads and converts an image to RGBA8.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <returns>The raster result.</returns>
    TiffRgba8ReadResult ReadRgba8(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder);
    /// <summary>Reads and converts an image to RGBA16.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <returns>The raster result.</returns>
    TiffRgba16ReadResult ReadRgba16(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder);
    /// <summary>Reads an image in its source sample representation.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <returns>The raster result.</returns>
    TiffSourceRasterReadResult ReadSourceRaster(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder);
    /// <summary>Decodes one tag value.</summary>
    /// <param name="entry">The IFD entry.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="diagnostics">The diagnostic collection.</param>
    /// <returns>The decoded tag value.</returns>
    TiffTagValue ReadTagValue(TiffIfdEntry entry, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffDiagnostics diagnostics);
    /// <summary>Writes an image in its source sample representation.</summary>
    /// <param name="info">The image information.</param>
    /// <param name="offsets">The segment offsets.</param>
    /// <param name="byteCounts">The segment byte counts.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="source">The source samples.</param>
    /// <returns>The raster write result.</returns>
    TiffSourceRasterWriteResult WriteSourceRaster(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<byte> source);
    /// <summary>Reads EXIF metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="header">The file header.</param>
    /// <returns>The EXIF read result.</returns>
    TiffExifReadResult ReadExif(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header);
    /// <summary>Writes EXIF metadata to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="parentEntries">The parent directory entries.</param>
    /// <param name="exifValues">The EXIF values.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="parentOffset">The parent directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The EXIF write result.</returns>
    TiffExifWriteResult WriteClassicExif(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffTagValue[] exifValues, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset);
    /// <summary>Reads GPS metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="header">The file header.</param>
    /// <returns>The GPS read result.</returns>
    TiffGpsReadResult ReadGps(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header);
    /// <summary>Writes GPS metadata to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="parentEntries">The parent directory entries.</param>
    /// <param name="gpsValues">The GPS values.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="parentOffset">The parent directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The GPS write result.</returns>
    TiffGpsWriteResult WriteClassicGps(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffTagValue[] gpsValues, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset);
    /// <summary>Reads XMP metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The XMP read result.</returns>
    TiffXmpReadResult ReadXmp(TiffDirectory directory, ReadOnlySpan<byte> fileBytes);
    /// <summary>Writes XMP metadata to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="values">The directory values.</param>
    /// <param name="xmpBytes">The XMP packet bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The XMP write result.</returns>
    TiffXmpWriteResult WriteClassicXmp(Span<byte> destination, TiffTagValue[] values, byte[] xmpBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset);
    /// <summary>Reads an ICC profile.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The ICC profile read result.</returns>
    TiffIccProfileReadResult ReadIccProfile(TiffDirectory directory, ReadOnlySpan<byte> fileBytes);
    /// <summary>Writes an ICC profile to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="values">The directory values.</param>
    /// <param name="profileBytes">The ICC profile bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The ICC profile write result.</returns>
    TiffIccProfileWriteResult WriteClassicIccProfile(Span<byte> destination, TiffTagValue[] values, byte[] profileBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset);
    /// <summary>Reads Photoshop image resources.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The Photoshop metadata read result.</returns>
    TiffPhotoshopReadResult ReadPhotoshop(TiffDirectory directory, ReadOnlySpan<byte> fileBytes);
    /// <summary>Reads IPTC metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The IPTC metadata read result.</returns>
    TiffIptcReadResult ReadIptc(TiffDirectory directory, ReadOnlySpan<byte> fileBytes);
    /// <summary>Reads image-description metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The image-description read result.</returns>
    TiffImageDescriptionReadResult ReadImageDescription(TiffDirectory directory, ReadOnlySpan<byte> fileBytes);
    /// <summary>Writes an image description to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="values">The directory values.</param>
    /// <param name="description">The image description.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The image-description write result.</returns>
    TiffImageDescriptionWriteResult WriteClassicImageDescription(Span<byte> destination, TiffTagValue[] values, string description, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset);
    /// <summary>Reads GeoTIFF model metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="header">The file header.</param>
    /// <returns>The geographic model read result.</returns>
    TiffGeoModelReadResult ReadGeoModel(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header);
    /// <summary>Reads a GeoTIFF key directory.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="header">The file header.</param>
    /// <returns>The geographic key-directory read result.</returns>
    TiffGeoKeyDirectoryReadResult ReadGeoKeyDirectory(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header);
    /// <summary>Reads GeoTIFF parameter metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="header">The file header.</param>
    /// <returns>The geographic parameter read result.</returns>
    TiffGeoParameterReadResult ReadGeoParameters(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header);
    /// <summary>Decodes GeoTIFF ASCII parameter bytes.</summary>
    /// <param name="bytes">The ASCII parameter bytes.</param>
    /// <param name="diagnostics">The diagnostic collection.</param>
    /// <returns>The decoded geographic ASCII parameters.</returns>
    TiffGeoAsciiParams DecodeGeoAsciiParams(byte[] bytes, TiffDiagnostics diagnostics);
    /// <summary>Writes GeoTIFF metadata to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="values">The directory values.</param>
    /// <param name="modelMetadata">The geographic model metadata.</param>
    /// <param name="keyDirectory">The geographic key directory.</param>
    /// <param name="parameters">The geographic parameters.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The GeoTIFF write result.</returns>
    TiffGeoTiffWriteResult WriteClassicGeoTiff(Span<byte> destination, TiffTagValue[] values, TiffGeoModelMetadata modelMetadata, TiffGeoKeyDirectory keyDirectory, TiffGeoParameterMetadata parameters, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset);
    /// <summary>Maps an image coordinate into GeoTIFF model space.</summary>
    /// <param name="metadata">The geographic model metadata.</param>
    /// <param name="rasterI">The raster I coordinate.</param>
    /// <param name="rasterJ">The raster J coordinate.</param>
    /// <param name="rasterK">The raster K coordinate.</param>
    /// <returns>The geographic transform result.</returns>
    TiffGeoTransformResult ImageToGeoModel(TiffGeoModelMetadata metadata, double rasterI, double rasterJ, double rasterK);
    /// <summary>Maps a GeoTIFF model coordinate back into raw raster-coordinate space.</summary>
    /// <param name="metadata">The geographic model metadata.</param>
    /// <param name="modelX">The model X coordinate.</param>
    /// <param name="modelY">The model Y coordinate.</param>
    /// <param name="modelZ">The model Z coordinate.</param>
    /// <returns>The geographic transform result.</returns>
    TiffGeoTransformResult GeoModelToImage(TiffGeoModelMetadata metadata, double modelX, double modelY, double modelZ);
    /// <summary>Maps a pixel index into model space using RasterPixelIsArea or RasterPixelIsPoint semantics.</summary>
    /// <param name="metadata">The geographic model metadata.</param>
    /// <param name="rasterType">The resolved raster pixel semantics.</param>
    /// <param name="pixelColumn">The zero-based pixel column.</param>
    /// <param name="pixelRow">The zero-based pixel row.</param>
    /// <param name="rasterK">The raster K coordinate.</param>
    /// <returns>The geographic transform result.</returns>
    TiffGeoTransformResult ImagePixelToGeoModel(TiffGeoModelMetadata metadata, TiffGeoRasterType rasterType, double pixelColumn, double pixelRow, double rasterK);
    /// <summary>Maps a model coordinate back to a pixel index using RasterPixelIsArea or RasterPixelIsPoint semantics.</summary>
    /// <param name="metadata">The geographic model metadata.</param>
    /// <param name="rasterType">The resolved raster pixel semantics.</param>
    /// <param name="modelX">The model X coordinate.</param>
    /// <param name="modelY">The model Y coordinate.</param>
    /// <param name="modelZ">The model Z coordinate.</param>
    /// <returns>The geographic transform result.</returns>
    TiffGeoTransformResult GeoModelToImagePixel(TiffGeoModelMetadata metadata, TiffGeoRasterType rasterType, double modelX, double modelY, double modelZ);
    /// <summary>Reads GDAL metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The GDAL metadata read result.</returns>
    TiffGdalMetadataReadResult ReadGdalMetadata(TiffDirectory directory, ReadOnlySpan<byte> fileBytes);
    /// <summary>Writes GDAL metadata to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="values">The directory values.</param>
    /// <param name="metadata">The GDAL metadata.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The GDAL metadata write result.</returns>
    TiffGdalMetadataWriteResult WriteClassicGdalMetadata(Span<byte> destination, TiffTagValue[] values, TiffGdalMetadata metadata, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset);
    /// <summary>Reads recognized vendor metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The vendor metadata read result.</returns>
    TiffVendorMetadataReadResult ReadVendorMetadata(TiffDirectory directory, ReadOnlySpan<byte> fileBytes);
    /// <summary>Reads OME metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The OME metadata read result.</returns>
    TiffOmeMetadataReadResult ReadOmeMetadata(TiffDirectory directory, ReadOnlySpan<byte> fileBytes);
    /// <summary>Writes OME metadata to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="values">The directory values.</param>
    /// <param name="omeXml">The OME XML.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The OME metadata write result.</returns>
    TiffOmeMetadataWriteResult WriteClassicOmeMetadata(Span<byte> destination, TiffTagValue[] values, string omeXml, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset);
    /// <summary>Reads ImageJ metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The ImageJ metadata read result.</returns>
    TiffImageJMetadataReadResult ReadImageJMetadata(TiffDirectory directory, ReadOnlySpan<byte> fileBytes);
    /// <summary>Writes ImageJ metadata to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="values">The directory values.</param>
    /// <param name="metadata">The ImageJ metadata.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The ImageJ metadata write result.</returns>
    TiffImageJMetadataWriteResult WriteClassicImageJMetadata(Span<byte> destination, TiffTagValue[] values, TiffImageJMetadata metadata, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset);
    /// <summary>Groups related image directories into multidimensional series.</summary>
    /// <param name="directories">The image directories.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <returns>The series grouping result.</returns>
    TiffSeriesGroupResult GroupSeries(TiffDirectory[] directories, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder);
    /// <summary>Creates a normalized strip layout from image information and stored ranges.</summary>
    /// <param name="info">The normalized image information.</param>
    /// <param name="stripOffsets">The strip offsets.</param>
    /// <param name="stripByteCounts">The encoded strip byte counts.</param>
    /// <returns>The strip-layout result.</returns>
    TiffStripLayoutResult CreateStripLayout(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts);
    /// <summary>Creates an optional virtual chopped-strip view for a large uncompressed single strip per plane.</summary>
    /// <param name="info">The normalized image information.</param>
    /// <param name="stripOffsets">The strip offsets.</param>
    /// <param name="stripByteCounts">The encoded strip byte counts.</param>
    /// <param name="targetDecodedByteCount">The preferred maximum decoded byte count of each virtual strip.</param>
    /// <returns>The strip-layout result.</returns>
    TiffStripLayoutResult CreateChoppedStripLayout(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ulong targetDecodedByteCount);
    /// <summary>Creates a normalized tile layout from image information and stored ranges.</summary>
    /// <param name="info">The normalized image information.</param>
    /// <param name="tileOffsets">The tile offsets.</param>
    /// <param name="tileByteCounts">The encoded tile byte counts.</param>
    /// <returns>The tile-layout result.</returns>
    TiffTileLayoutResult CreateTileLayout(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts);
    /// <summary>Locates a volumetric tile and its file range.</summary>
    /// <param name="info">The image information.</param>
    /// <param name="tileOffsets">The tile offsets.</param>
    /// <param name="tileByteCounts">The tile byte counts.</param>
    /// <param name="column">The image column.</param>
    /// <param name="row">The image row.</param>
    /// <param name="depth">The image depth coordinate.</param>
    /// <param name="sampleIndex">The sample plane index.</param>
    /// <returns>The tile lookup result.</returns>
    TiffTileLookupResult LookupVolumeTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ulong column, ulong row, ulong depth, int sampleIndex);
    /// <summary>Locates a tile and its file range.</summary>
    /// <param name="info">The image information.</param>
    /// <param name="tileOffsets">The tile offsets.</param>
    /// <param name="tileByteCounts">The tile byte counts.</param>
    /// <param name="column">The image column.</param>
    /// <param name="row">The image row.</param>
    /// <param name="sampleIndex">The sample plane index.</param>
    /// <returns>The tile lookup result.</returns>
    TiffTileLookupResult LookupTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ulong column, ulong row, int sampleIndex);
    /// <summary>Detects a reduced-resolution image pyramid.</summary>
    /// <param name="parent">The parent image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="fileKind">The file kind.</param>
    /// <returns>The pyramid detection result.</returns>
    TiffPyramidDetectionResult DetectPyramid(TiffDirectory parent, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind);
    /// <summary>Writes a classic TIFF image pyramid.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="parentEntries">The parent directory entries.</param>
    /// <param name="overviewDirectories">The overview directory entries.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="parentOffset">The parent directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The pyramid write result.</returns>
    TiffPyramidWriteResult WriteClassicPyramid(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffIfdEntry[][] overviewDirectories, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset);
}
