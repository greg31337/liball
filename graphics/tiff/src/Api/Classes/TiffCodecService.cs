using System;
using System.Threading;

namespace tiff;

/// <summary>Provides common TIFF compression, raster conversion, and tag decoding operations.</summary>
public class TiffCodecService : ITiffCodecService
{
    /// <summary>Creates a TIFF codec service.</summary>
    public TiffCodecService()
    {
    }

    /// <summary>Decodes one compressed segment with built-in codecs.</summary>
    /// <param name="context">The codec context.</param>
    /// <param name="source">The compressed bytes.</param>
    /// <returns>The codec result.</returns>
    public TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source)
    {
        return TiffCodec.Decode(context, source);
    }

    /// <summary>Decodes one compressed segment with an optional custom codec.</summary>
    /// <param name="context">The codec context.</param>
    /// <param name="source">The compressed bytes.</param>
    /// <param name="compressionCodec">The custom codec.</param>
    /// <returns>The codec result.</returns>
    public TiffCodecResult DecodeWithCompressionCodec(TiffCodecContext context, ReadOnlySpan<byte> source, ITiffCompressionCodec? compressionCodec)
    {
        return TiffCodec.Decode(context, source, compressionCodec);
    }

    /// <summary>Decodes one compressed segment with progress reporting and cancellation.</summary>
    /// <param name="context">The codec context.</param>
    /// <param name="source">The compressed bytes.</param>
    /// <param name="compressionCodec">The optional custom codec.</param>
    /// <param name="progress">The optional progress receiver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The codec result.</returns>
    /// <exception cref="System.OperationCanceledException">The cancellation token was canceled.</exception>
    public TiffCodecResult DecodeWithCancellation(TiffCodecContext context, ReadOnlySpan<byte> source, ITiffCompressionCodec? compressionCodec, ITiffProgress? progress, CancellationToken cancellationToken)
    {
        TiffOperationMonitor monitor = new TiffOperationMonitor(TiffProgressOperation.Decode, progress, cancellationToken);
        monitor.Start(1);
        TiffCodecResult result = TiffCodec.Decode(context, source, compressionCodec);
        monitor.Complete(1);
        return result;
    }

    /// <summary>Encodes one decoded segment with built-in codecs.</summary>
    /// <param name="context">The codec context.</param>
    /// <param name="source">The decoded bytes.</param>
    /// <returns>The codec result.</returns>
    public TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source)
    {
        return TiffCodec.Encode(context, source);
    }

    /// <summary>Encodes one decoded segment with an optional custom codec.</summary>
    /// <param name="context">The codec context.</param>
    /// <param name="source">The decoded bytes.</param>
    /// <param name="compressionCodec">The custom codec.</param>
    /// <returns>The codec result.</returns>
    public TiffCodecResult EncodeWithCompressionCodec(TiffCodecContext context, ReadOnlySpan<byte> source, ITiffCompressionCodec? compressionCodec)
    {
        return TiffCodec.Encode(context, source, compressionCodec);
    }

    /// <summary>Reports built-in directional codec and raw-pass-through capabilities.</summary>
    /// <param name="compression">The compression value.</param>
    /// <returns>The capability result and public diagnostics.</returns>
    public TiffCompressionCapabilityResult GetCompressionCapability(TiffCompression compression)
    {
        return TiffCodec.GetCompressionCapability(compression);
    }

    /// <summary>Reports directional codec and raw-pass-through capabilities including a custom codec.</summary>
    /// <param name="compression">The compression value.</param>
    /// <param name="compressionCodec">The custom codec.</param>
    /// <returns>The capability result and public diagnostics.</returns>
    public TiffCompressionCapabilityResult GetCompressionCapabilityWithCodec(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        return TiffCodec.GetCompressionCapabilityWithCodec(compression, compressionCodec);
    }

    /// <summary>Checks built-in compression decoding support.</summary>
    /// <param name="compression">The compression value.</param>
    /// <returns><c>true</c> when decoding is supported.</returns>
    public bool CanDecodeCompression(TiffCompression compression)
    {
        return TiffCodec.CanDecodeCompression(compression);
    }

    /// <summary>Checks compression decoding support including a custom codec.</summary>
    /// <param name="compression">The compression value.</param>
    /// <param name="compressionCodec">The custom codec.</param>
    /// <returns><c>true</c> when decoding is supported.</returns>
    public bool CanDecodeCompressionWithCodec(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        return TiffCodec.CanDecodeCompressionWithCodec(compression, compressionCodec);
    }

    /// <summary>Checks built-in compression encoding support.</summary>
    /// <param name="compression">The compression value.</param>
    /// <returns><c>true</c> when encoding is supported.</returns>
    public bool CanEncodeCompression(TiffCompression compression)
    {
        return TiffCodec.CanEncodeCompression(compression);
    }

    /// <summary>Checks compression encoding support including a custom codec.</summary>
    /// <param name="compression">The compression value.</param>
    /// <param name="compressionCodec">The custom codec.</param>
    /// <returns><c>true</c> when encoding is supported.</returns>
    public bool CanEncodeCompressionWithCodec(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        return TiffCodec.CanEncodeCompressionWithCodec(compression, compressionCodec);
    }

    /// <summary>Checks built-in raw-compression pass-through support.</summary>
    /// <param name="compression">The compression value.</param>
    /// <returns><c>true</c> when pass-through is supported.</returns>
    public bool CanPassThroughRawCompression(TiffCompression compression)
    {
        return TiffCodec.CanPassThroughRawCompression(compression);
    }

    /// <summary>Checks raw-compression pass-through support including a custom codec.</summary>
    /// <param name="compression">The compression value.</param>
    /// <param name="compressionCodec">The custom codec.</param>
    /// <returns><c>true</c> when pass-through is supported.</returns>
    public bool CanPassThroughRawCompressionWithCodec(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        return TiffCodec.CanPassThroughRawCompression(compression, compressionCodec);
    }

    /// <summary>Checks whether built-in codecs support both compression decoding and encoding.</summary>
    /// <param name="compression">The compression value.</param>
    /// <returns><c>true</c> when supported.</returns>
    public bool IsCompressionSupported(TiffCompression compression)
    {
        return TiffCodec.IsCompressionSupported(compression);
    }

    /// <summary>Checks whether custom and built-in codecs together support both compression decoding and encoding.</summary>
    /// <param name="compression">The compression value.</param>
    /// <param name="compressionCodec">The custom codec.</param>
    /// <returns><c>true</c> when supported.</returns>
    public bool IsCompressionSupportedWithCodec(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        return TiffCodec.IsCompressionSupported(compression, compressionCodec);
    }

    /// <summary>Converts source samples to normalized doubles.</summary>
    /// <param name="context">The codec context.</param>
    /// <param name="source">The sample bytes.</param>
    /// <param name="diagnostics">The diagnostic collection.</param>
    /// <returns>The converted samples.</returns>
    public double[] ConvertSamplesToDouble(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffCodec.ConvertSamplesToDouble(context, source, diagnostics);
    }

    /// <summary>Converts normalized source samples to an RGBA8 raster using the requested color and alpha controls.</summary>
    /// <param name="info">The source image information.</param>
    /// <param name="samples">The normalized source samples.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The raster result.</returns>
    public TiffRgba8ReadResult ConvertSamplesToRgba8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffRgbaReadOptions options)
    {
        return TiffSourceRgbaReader.ConvertSamplesToRgba8(info, samples, new TiffLimits(), options);
    }

    /// <summary>Converts normalized source samples to an RGBA16 raster using the requested color and alpha controls.</summary>
    /// <param name="info">The source image information.</param>
    /// <param name="samples">The normalized source samples.</param>
    /// <param name="options">The conversion options.</param>
    /// <returns>The raster result.</returns>
    public TiffRgba16ReadResult ConvertSamplesToRgba16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffRgbaReadOptions options)
    {
        return TiffSourceRgbaReader.ConvertSamplesToRgba16(info, samples, new TiffLimits(), options);
    }

    /// <summary>Applies the image orientation to interleaved pixels.</summary>
    /// <param name="info">The image information.</param>
    /// <param name="source">The source pixels.</param>
    /// <param name="bytesPerPixel">The bytes per pixel.</param>
    /// <param name="diagnostics">The diagnostic collection.</param>
    /// <returns>The transformation result.</returns>
    public TiffOrientationTransformResult ApplyOrientation(TiffImageInfo info, ReadOnlySpan<byte> source, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        return TiffCodec.ApplyOrientation(info, source, bytesPerPixel, diagnostics);
    }

    /// <summary>Reads and converts an image to RGBA8.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <returns>The raster result.</returns>
    public TiffRgba8ReadResult ReadRgba8(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return TiffCodec.ReadRgba8(directory, fileBytes, byteOrder);
    }

    /// <summary>Reads and converts an image to RGBA16.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <returns>The raster result.</returns>
    public TiffRgba16ReadResult ReadRgba16(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return TiffCodec.ReadRgba16(directory, fileBytes, byteOrder);
    }

    /// <summary>Reads an image in its source sample representation.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <returns>The raster result.</returns>
    public TiffSourceRasterReadResult ReadSourceRaster(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return TiffCodec.ReadSourceRaster(directory, fileBytes, byteOrder);
    }

    /// <summary>Decodes one tag value.</summary>
    /// <param name="entry">The IFD entry.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="diagnostics">The diagnostic collection.</param>
    /// <returns>The decoded tag value.</returns>
    public TiffTagValue ReadTagValue(TiffIfdEntry entry, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        return TiffCodec.ReadTagValue(entry, fileBytes, byteOrder, diagnostics);
    }

    /// <summary>Writes an image in its source sample representation.</summary>
    /// <param name="info">The image information.</param>
    /// <param name="offsets">The segment offsets.</param>
    /// <param name="byteCounts">The segment byte counts.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="source">The source samples.</param>
    /// <returns>The raster write result.</returns>
    public TiffSourceRasterWriteResult WriteSourceRaster(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<byte> source)
    {
        return TiffCodec.WriteSourceRaster(info, offsets, byteCounts, destination, source);
    }

    /// <summary>Reads EXIF metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="header">The file header.</param>
    /// <returns>The EXIF read result.</returns>
    public TiffExifReadResult ReadExif(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        return TiffCodec.ReadExif(directory, fileBytes, header);
    }

    /// <summary>Writes EXIF metadata to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="parentEntries">The parent directory entries.</param>
    /// <param name="exifValues">The EXIF values.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="parentOffset">The parent directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The EXIF write result.</returns>
    public TiffExifWriteResult WriteClassicExif(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffTagValue[] exifValues, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset)
    {
        return TiffCodec.WriteClassicExif(destination, parentEntries, exifValues, byteOrder, parentOffset, nextIfdOffset);
    }

    /// <summary>Reads GPS metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="header">The file header.</param>
    /// <returns>The GPS read result.</returns>
    public TiffGpsReadResult ReadGps(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        return TiffCodec.ReadGps(directory, fileBytes, header);
    }

    /// <summary>Writes GPS metadata to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="parentEntries">The parent directory entries.</param>
    /// <param name="gpsValues">The GPS values.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="parentOffset">The parent directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The GPS write result.</returns>
    public TiffGpsWriteResult WriteClassicGps(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffTagValue[] gpsValues, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset)
    {
        return TiffCodec.WriteClassicGps(destination, parentEntries, gpsValues, byteOrder, parentOffset, nextIfdOffset);
    }

    /// <summary>Reads XMP metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The XMP read result.</returns>
    public TiffXmpReadResult ReadXmp(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffCodec.ReadXmp(directory, fileBytes);
    }

    /// <summary>Writes XMP metadata to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="values">The directory values.</param>
    /// <param name="xmpBytes">The XMP packet bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The XMP write result.</returns>
    public TiffXmpWriteResult WriteClassicXmp(Span<byte> destination, TiffTagValue[] values, byte[] xmpBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return TiffCodec.WriteClassicXmp(destination, values, xmpBytes, byteOrder, directoryOffset, nextIfdOffset);
    }

    /// <summary>Reads an ICC profile.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The ICC profile read result.</returns>
    public TiffIccProfileReadResult ReadIccProfile(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffCodec.ReadIccProfile(directory, fileBytes);
    }

    /// <summary>Writes an ICC profile to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="values">The directory values.</param>
    /// <param name="profileBytes">The ICC profile bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The ICC profile write result.</returns>
    public TiffIccProfileWriteResult WriteClassicIccProfile(Span<byte> destination, TiffTagValue[] values, byte[] profileBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return TiffCodec.WriteClassicIccProfile(destination, values, profileBytes, byteOrder, directoryOffset, nextIfdOffset);
    }

    /// <summary>Reads Photoshop image resources.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The Photoshop metadata read result.</returns>
    public TiffPhotoshopReadResult ReadPhotoshop(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffCodec.ReadPhotoshop(directory, fileBytes);
    }

    /// <summary>Reads IPTC metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The IPTC metadata read result.</returns>
    public TiffIptcReadResult ReadIptc(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffCodec.ReadIptc(directory, fileBytes);
    }

    /// <summary>Reads image-description metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The image-description read result.</returns>
    public TiffImageDescriptionReadResult ReadImageDescription(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffCodec.ReadImageDescription(directory, fileBytes);
    }

    /// <summary>Writes an image description to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="values">The directory values.</param>
    /// <param name="description">The image description.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The image-description write result.</returns>
    public TiffImageDescriptionWriteResult WriteClassicImageDescription(Span<byte> destination, TiffTagValue[] values, string description, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return TiffCodec.WriteClassicImageDescription(destination, values, description, byteOrder, directoryOffset, nextIfdOffset);
    }

    /// <summary>Reads GeoTIFF model metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="header">The file header.</param>
    /// <returns>The geographic model read result.</returns>
    public TiffGeoModelReadResult ReadGeoModel(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        return TiffCodec.ReadGeoModel(directory, fileBytes, header);
    }

    /// <summary>Reads a GeoTIFF key directory.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="header">The file header.</param>
    /// <returns>The geographic key-directory read result.</returns>
    public TiffGeoKeyDirectoryReadResult ReadGeoKeyDirectory(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        return TiffCodec.ReadGeoKeyDirectory(directory, fileBytes, header);
    }

    /// <summary>Reads GeoTIFF parameter metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="header">The file header.</param>
    /// <returns>The geographic parameter read result.</returns>
    public TiffGeoParameterReadResult ReadGeoParameters(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        return TiffCodec.ReadGeoParameters(directory, fileBytes, header);
    }

    /// <summary>Decodes GeoTIFF ASCII parameter bytes.</summary>
    /// <param name="bytes">The ASCII parameter bytes.</param>
    /// <param name="diagnostics">The diagnostic collection.</param>
    /// <returns>The decoded geographic ASCII parameters.</returns>
    public TiffGeoAsciiParams DecodeGeoAsciiParams(byte[] bytes, TiffDiagnostics diagnostics)
    {
        return TiffGeoParameterReader.DecodeAsciiParams(bytes, diagnostics);
    }

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
    public TiffGeoTiffWriteResult WriteClassicGeoTiff(Span<byte> destination, TiffTagValue[] values, TiffGeoModelMetadata modelMetadata, TiffGeoKeyDirectory keyDirectory, TiffGeoParameterMetadata parameters, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return TiffCodec.WriteClassicGeoTiff(destination, values, modelMetadata, keyDirectory, parameters, byteOrder, directoryOffset, nextIfdOffset);
    }

    /// <summary>Maps an image coordinate into GeoTIFF model space.</summary>
    /// <param name="metadata">The geographic model metadata.</param>
    /// <param name="rasterI">The raster I coordinate.</param>
    /// <param name="rasterJ">The raster J coordinate.</param>
    /// <param name="rasterK">The raster K coordinate.</param>
    /// <returns>The geographic transform result.</returns>
    public TiffGeoTransformResult ImageToGeoModel(TiffGeoModelMetadata metadata, double rasterI, double rasterJ, double rasterK)
    {
        return TiffCodec.ImageToGeoModel(metadata, rasterI, rasterJ, rasterK);
    }

    /// <summary>Maps a GeoTIFF model coordinate back into raw raster-coordinate space.</summary>
    /// <param name="metadata">The geographic model metadata.</param>
    /// <param name="modelX">The model X coordinate.</param>
    /// <param name="modelY">The model Y coordinate.</param>
    /// <param name="modelZ">The model Z coordinate.</param>
    /// <returns>The geographic transform result.</returns>
    public TiffGeoTransformResult GeoModelToImage(TiffGeoModelMetadata metadata, double modelX, double modelY, double modelZ)
    {
        return TiffCodec.GeoModelToImage(metadata, modelX, modelY, modelZ);
    }

    /// <summary>Maps a pixel index into model space using the resolved raster pixel semantics.</summary>
    /// <param name="metadata">The geographic model metadata.</param>
    /// <param name="rasterType">The resolved raster pixel semantics.</param>
    /// <param name="pixelColumn">The zero-based pixel column.</param>
    /// <param name="pixelRow">The zero-based pixel row.</param>
    /// <param name="rasterK">The raster K coordinate.</param>
    /// <returns>The geographic transform result.</returns>
    public TiffGeoTransformResult ImagePixelToGeoModel(TiffGeoModelMetadata metadata, TiffGeoRasterType rasterType, double pixelColumn, double pixelRow, double rasterK)
    {
        return TiffCodec.ImagePixelToGeoModel(metadata, rasterType, pixelColumn, pixelRow, rasterK);
    }

    /// <summary>Maps a model coordinate back to a pixel index using the resolved raster pixel semantics.</summary>
    /// <param name="metadata">The geographic model metadata.</param>
    /// <param name="rasterType">The resolved raster pixel semantics.</param>
    /// <param name="modelX">The model X coordinate.</param>
    /// <param name="modelY">The model Y coordinate.</param>
    /// <param name="modelZ">The model Z coordinate.</param>
    /// <returns>The geographic transform result.</returns>
    public TiffGeoTransformResult GeoModelToImagePixel(TiffGeoModelMetadata metadata, TiffGeoRasterType rasterType, double modelX, double modelY, double modelZ)
    {
        return TiffCodec.GeoModelToImagePixel(metadata, rasterType, modelX, modelY, modelZ);
    }

    /// <summary>Reads GDAL metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The GDAL metadata read result.</returns>
    public TiffGdalMetadataReadResult ReadGdalMetadata(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffCodec.ReadGdalMetadata(directory, fileBytes);
    }

    /// <summary>Writes GDAL metadata to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="values">The directory values.</param>
    /// <param name="metadata">The GDAL metadata.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The GDAL metadata write result.</returns>
    public TiffGdalMetadataWriteResult WriteClassicGdalMetadata(Span<byte> destination, TiffTagValue[] values, TiffGdalMetadata metadata, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return TiffCodec.WriteClassicGdalMetadata(destination, values, metadata, byteOrder, directoryOffset, nextIfdOffset);
    }

    /// <summary>Reads recognized vendor metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The vendor metadata read result.</returns>
    public TiffVendorMetadataReadResult ReadVendorMetadata(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffCodec.ReadVendorMetadata(directory, fileBytes);
    }

    /// <summary>Reads OME metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The OME metadata read result.</returns>
    public TiffOmeMetadataReadResult ReadOmeMetadata(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffCodec.ReadOmeMetadata(directory, fileBytes);
    }

    /// <summary>Writes OME metadata to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="values">The directory values.</param>
    /// <param name="omeXml">The OME XML.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The OME metadata write result.</returns>
    public TiffOmeMetadataWriteResult WriteClassicOmeMetadata(Span<byte> destination, TiffTagValue[] values, string omeXml, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return TiffCodec.WriteClassicOmeMetadata(destination, values, omeXml, byteOrder, directoryOffset, nextIfdOffset);
    }

    /// <summary>Reads ImageJ metadata.</summary>
    /// <param name="directory">The image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <returns>The ImageJ metadata read result.</returns>
    public TiffImageJMetadataReadResult ReadImageJMetadata(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffCodec.ReadImageJMetadata(directory, fileBytes);
    }

    /// <summary>Writes ImageJ metadata to a classic TIFF directory.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="values">The directory values.</param>
    /// <param name="metadata">The ImageJ metadata.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The ImageJ metadata write result.</returns>
    public TiffImageJMetadataWriteResult WriteClassicImageJMetadata(Span<byte> destination, TiffTagValue[] values, TiffImageJMetadata metadata, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return TiffCodec.WriteClassicImageJMetadata(destination, values, metadata, byteOrder, directoryOffset, nextIfdOffset);
    }

    /// <summary>Groups related image directories into multidimensional series.</summary>
    /// <param name="directories">The image directories.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <returns>The series grouping result.</returns>
    public TiffSeriesGroupResult GroupSeries(TiffDirectory[] directories, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return TiffCodec.GroupSeries(directories, fileBytes, byteOrder);
    }

    /// <summary>Creates a normalized strip layout from image information and stored ranges.</summary>
    /// <param name="info">The normalized image information.</param>
    /// <param name="stripOffsets">The strip offsets.</param>
    /// <param name="stripByteCounts">The encoded strip byte counts.</param>
    /// <returns>The strip-layout result.</returns>
    public TiffStripLayoutResult CreateStripLayout(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts)
    {
        return TiffSegmentLayoutBuilder.CreateStripLayout(info, stripOffsets, stripByteCounts);
    }

    /// <summary>Creates an optional virtual chopped-strip view for a large uncompressed single strip per plane.</summary>
    /// <param name="info">The normalized image information.</param>
    /// <param name="stripOffsets">The strip offsets.</param>
    /// <param name="stripByteCounts">The encoded strip byte counts.</param>
    /// <param name="targetDecodedByteCount">The preferred maximum decoded byte count of each virtual strip.</param>
    /// <returns>The strip-layout result.</returns>
    public TiffStripLayoutResult CreateChoppedStripLayout(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ulong targetDecodedByteCount)
    {
        return TiffSegmentLayoutBuilder.CreateChoppedStripLayout(info, stripOffsets, stripByteCounts, targetDecodedByteCount);
    }

    /// <summary>Creates a normalized tile layout from image information and stored ranges.</summary>
    /// <param name="info">The normalized image information.</param>
    /// <param name="tileOffsets">The tile offsets.</param>
    /// <param name="tileByteCounts">The encoded tile byte counts.</param>
    /// <returns>The tile-layout result.</returns>
    public TiffTileLayoutResult CreateTileLayout(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts)
    {
        return TiffSegmentLayoutBuilder.CreateTileLayout(info, tileOffsets, tileByteCounts);
    }

    /// <summary>Locates a volumetric tile and its file range.</summary>
    /// <param name="info">The image information.</param>
    /// <param name="tileOffsets">The tile offsets.</param>
    /// <param name="tileByteCounts">The tile byte counts.</param>
    /// <param name="column">The image column.</param>
    /// <param name="row">The image row.</param>
    /// <param name="depth">The image depth coordinate.</param>
    /// <param name="sampleIndex">The sample plane index.</param>
    /// <returns>The tile lookup result.</returns>
    public TiffTileLookupResult LookupVolumeTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ulong column, ulong row, ulong depth, int sampleIndex)
    {
        return TiffTileLookupBuilder.LookupVolume(info, tileOffsets, tileByteCounts, column, row, depth, sampleIndex);
    }

    /// <summary>Locates a tile and its file range.</summary>
    /// <param name="info">The image information.</param>
    /// <param name="tileOffsets">The tile offsets.</param>
    /// <param name="tileByteCounts">The tile byte counts.</param>
    /// <param name="column">The image column.</param>
    /// <param name="row">The image row.</param>
    /// <param name="sampleIndex">The sample plane index.</param>
    /// <returns>The tile lookup result.</returns>
    public TiffTileLookupResult LookupTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ulong column, ulong row, int sampleIndex)
    {
        return TiffCodec.LookupTile(info, tileOffsets, tileByteCounts, column, row, sampleIndex);
    }

    /// <summary>Detects a reduced-resolution image pyramid.</summary>
    /// <param name="parent">The parent image directory.</param>
    /// <param name="fileBytes">The complete file bytes.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="fileKind">The file kind.</param>
    /// <returns>The pyramid detection result.</returns>
    public TiffPyramidDetectionResult DetectPyramid(TiffDirectory parent, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind)
    {
        return TiffCodec.DetectPyramid(parent, fileBytes, byteOrder, fileKind);
    }

    /// <summary>Writes a classic TIFF image pyramid.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="parentEntries">The parent directory entries.</param>
    /// <param name="overviewDirectories">The overview directory entries.</param>
    /// <param name="byteOrder">The file byte order.</param>
    /// <param name="parentOffset">The parent directory offset.</param>
    /// <param name="nextIfdOffset">The next directory offset.</param>
    /// <returns>The pyramid write result.</returns>
    public TiffPyramidWriteResult WriteClassicPyramid(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffIfdEntry[][] overviewDirectories, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset)
    {
        return TiffCodec.WriteClassicPyramid(destination, parentEntries, overviewDirectories, byteOrder, parentOffset, nextIfdOffset);
    }
}
