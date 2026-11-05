using System;

namespace tiff;

/// <summary>Provides the public entry points for compression, raster conversion, metadata processing, pyramid detection, and tile lookup.</summary>
/// <remarks>Codec and conversion methods return result objects with diagnostics. Unsupported compression is reported explicitly; raw encoded bytes can still be preserved by the high-level copy APIs.</remarks>
internal static class TiffCodec
{
    internal static readonly ITiffCompressionCodec BuiltInCompressionCodec = new TiffBuiltInCompressionCodec();

    /// <summary>Decodes the supplied segment according to the compression and image layout in the codec context.</summary>
    /// <param name="context">The codec context describing compression, layout, segment size, and safety limits.</param>
    /// <param name="source">The source bytes or samples to process.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source)
    {
        return Decode(context, source, null);
    }

    /// <summary>Decodes the supplied segment with a caller-provided compression implementation when it supports the requested compression.</summary>
    /// <remarks>The caller-provided implementation takes precedence over a built-in implementation. When it does not support the requested compression, the built-in codecs remain available as a fallback.</remarks>
    /// <param name="context">The codec context describing compression, layout, segment size, and safety limits.</param>
    /// <param name="source">The source bytes or samples to process.</param>
    /// <param name="compressionCodec">The optional compression implementation to use before falling back to built-in codecs.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source, ITiffCompressionCodec? compressionCodec)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateDecodeContext(context, source, diagnostics))
        {
            return TiffCodecResult.Create(new byte[0], diagnostics);
        }

        byte[] normalizedSource = NormalizeDecodeSource(context, source);
        ReadOnlySpan<byte> codecSource = source;
        if (normalizedSource.Length > 0)
        {
            codecSource = normalizedSource;
        }

        ITiffCompressionCodec? selectedCodec = SelectDecoder(context.Info.Compression, compressionCodec);
        if (selectedCodec == null)
        {
            return CreateUnsupportedCompressionResult(context, source, "decode");
        }

        TiffCodecResult result = selectedCodec.Decode(context, codecSource);
        if (result == null)
        {
            return CreateInvalidCompressionCodecResult("decode");
        }

        result = ValidateDecodedCompressionCodecResult(context, result);
        return FinishDecode(context, result);
    }

    internal static TiffCodecResult FinishDecode(TiffCodecContext context, TiffCodecResult result)
    {
        if (!result.Success)
        {
            return result;
        }

        TiffDiagnostics diagnostics = new TiffDiagnostics();
        diagnostics.Merge(result.Diagnostics);
        byte[] currentBytes = TiffIfdEntry.CopyBytes(result.Bytes);
        if (ShouldReverseDecodedFillOrder(context))
        {
            currentBytes = ReverseBitsInBytes(currentBytes);
        }

        if (context.Info.Predictor == 2)
        {
            currentBytes = TiffPredictor.ReverseHorizontalInteger(context, currentBytes, diagnostics);
            if (diagnostics.HasErrors())
            {
                return TiffCodecResult.Create(new byte[0], diagnostics);
            }
        }

        if (context.Info.Predictor == 3)
        {
            currentBytes = TiffPredictor.ReverseFloatingPoint(context, currentBytes, diagnostics);
            if (diagnostics.HasErrors())
            {
                return TiffCodecResult.Create(new byte[0], diagnostics);
            }
        }

        currentBytes = ConvertFileSamplesToHostByteOrder(context, currentBytes, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffCodecResult.Create(new byte[0], diagnostics);
        }

        return TiffCodecResult.Create(currentBytes, diagnostics);
    }

    /// <summary>Encodes the supplied decoded segment according to the compression and image layout in the codec context.</summary>
    /// <param name="context">The codec context describing compression, layout, segment size, and safety limits.</param>
    /// <param name="source">The source bytes or samples to process.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source)
    {
        return Encode(context, source, null);
    }

    /// <summary>Encodes the supplied segment with a caller-provided compression implementation when it supports the requested compression.</summary>
    /// <remarks>The caller-provided implementation takes precedence over a built-in implementation. When it does not support the requested compression, the built-in codecs remain available as a fallback.</remarks>
    /// <param name="context">The codec context describing compression, layout, segment size, and safety limits.</param>
    /// <param name="source">The source bytes or samples to process.</param>
    /// <param name="compressionCodec">The optional compression implementation to use before falling back to built-in codecs.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source, ITiffCompressionCodec? compressionCodec)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateEncodeContext(context, diagnostics))
        {
            return TiffCodecResult.Create(new byte[0], diagnostics);
        }

        ITiffCompressionCodec? selectedCodec = SelectEncoder(context.Info.Compression, compressionCodec);
        if (selectedCodec == null)
        {
            return CreateUnsupportedCompressionResult(context, source, "encode");
        }

        byte[] preparedSource = PrepareEncode(context, source, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffCodecResult.Create(new byte[0], diagnostics);
        }

        TiffCodecResult result = selectedCodec.Encode(context, preparedSource);
        if (result == null)
        {
            return CreateInvalidCompressionCodecResult("encode");
        }

        result = ValidateEncodedCompressionCodecResult(context, result);
        return FinishEncode(context, result);
    }

    internal static byte[] PrepareEncode(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        byte[] currentBytes = ConvertHostSamplesToFileByteOrder(context, source, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        if (context.Info.Predictor == 2)
        {
            byte[] predictedBytes = TiffPredictor.ApplyHorizontalInteger(context, currentBytes, diagnostics);
            return PrepareFillOrderBytes(context, predictedBytes);
        }

        if (context.Info.Predictor == 3)
        {
            byte[] predictedBytes = TiffPredictor.ApplyFloatingPoint(context, currentBytes, diagnostics);
            return PrepareFillOrderBytes(context, predictedBytes);
        }

        return PrepareFillOrderBytes(context, currentBytes);
    }

    internal static TiffCodecResult FinishEncode(TiffCodecContext context, TiffCodecResult result)
    {
        if (!result.Success)
        {
            return result;
        }

        if (ShouldReverseEncodedFillOrder(context))
        {
            return TiffCodecResult.Create(ReverseBitsInBytes(result.Bytes), result.Diagnostics);
        }

        return result;
    }

    internal static byte[] DecodeNone(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffNoneCodec.DecodeBytes(context, source, diagnostics);
    }

    internal static byte[] EncodeNone(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffNoneCodec.EncodeBytes(context, source, diagnostics);
    }

    internal static byte[] DecodePackBits(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffPackBitsCodec.DecodeBytes(context, source, diagnostics);
    }

    internal static byte[] EncodePackBits(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffPackBitsCodec.EncodeBytes(context, source, diagnostics);
    }

    internal static byte[] DecodeDeflate(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffDeflateCodec.DecodeBytes(context, source, diagnostics);
    }

    internal static byte[] EncodeDeflate(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffDeflateCodec.EncodeBytes(context, source, diagnostics);
    }

    internal static byte[] DecodeLzw(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffLzwCodec.DecodeBytes(context, source, diagnostics);
    }

    internal static byte[] EncodeLzw(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffLzwCodec.EncodeBytes(context, source, diagnostics);
    }

    internal static byte[] DecodeCcittModifiedHuffman(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffCcittModifiedHuffmanCodec.DecodeBytes(context, source, diagnostics);
    }

    internal static byte[] EncodeCcittModifiedHuffman(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffCcittModifiedHuffmanCodec.EncodeBytes(context, source, diagnostics);
    }

    internal static byte[] DecodeCcittGroup3(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffCcittGroup3Codec.DecodeBytes(context, source, diagnostics);
    }

    internal static byte[] EncodeCcittGroup3(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffCcittGroup3Codec.EncodeBytes(context, source, diagnostics);
    }

    internal static byte[] DecodeCcittGroup4(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffCcittGroup4Codec.DecodeBytes(context, source, diagnostics);
    }

    internal static byte[] EncodeCcittGroup4(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffCcittGroup4Codec.EncodeBytes(context, source, diagnostics);
    }

    internal static TiffCodecResult CreateUnsupportedCompressionResult(TiffCodecContext context, ReadOnlySpan<byte> source, string operation)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        diagnostics.AddError(TiffErrorCode.UnsupportedCompression, TiffCompressionCapabilityReporter.CreateUnavailableDirectionMessage(context.Info.Compression, operation));
        if (CanPassThroughRawCompression(context.Info.Compression))
        {
            return TiffCodecResult.Create(TiffIfdEntry.CopyBytes(source), diagnostics);
        }

        return TiffCodecResult.Create(new byte[0], diagnostics);
    }

    internal static TiffCodecResult CreateInvalidCompressionCodecResult(string operation)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        diagnostics.AddError(TiffErrorCode.UnknownError, "The compression codec returned no result for the " + operation + " operation.");
        return TiffCodecResult.Create(new byte[0], diagnostics);
    }

    internal static TiffCodecResult ValidateDecodedCompressionCodecResult(TiffCodecContext context, TiffCodecResult result)
    {
        if (!result.Success)
        {
            return result;
        }

        if (result.Bytes != null && (ulong)result.Bytes.Length == context.ExpectedDecodedByteCount)
        {
            return result;
        }

        TiffDiagnostics diagnostics = new TiffDiagnostics();
        diagnostics.Merge(result.Diagnostics);
        diagnostics.AddError(TiffErrorCode.InvalidByteRange, "The compression codec decoded byte count does not match the expected segment byte count.");
        return TiffCodecResult.Create(new byte[0], diagnostics);
    }

    internal static TiffCodecResult ValidateEncodedCompressionCodecResult(TiffCodecContext context, TiffCodecResult result)
    {
        if (!result.Success)
        {
            return result;
        }

        if (result.Bytes == null)
        {
            TiffDiagnostics missingBytesDiagnostics = new TiffDiagnostics();
            missingBytesDiagnostics.Merge(result.Diagnostics);
            missingBytesDiagnostics.AddError(TiffErrorCode.InvalidByteRange, "The compression codec returned no encoded byte buffer.");
            return TiffCodecResult.Create(new byte[0], missingBytesDiagnostics);
        }

        int byteCount = result.Bytes.Length;
        TiffLimits limits = TiffTagValueLoader.NormalizeLimits(context.Limits);
        if (limits.AllowsAllocation(byteCount))
        {
            return result;
        }

        TiffDiagnostics diagnostics = new TiffDiagnostics();
        diagnostics.Merge(result.Diagnostics);
        diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "The compression codec encoded byte count exceeds the configured allocation limit.");
        return TiffCodecResult.Create(new byte[0], diagnostics);
    }

    internal static ITiffCompressionCodec? SelectDecoder(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        if (!TiffCompressionCapabilityReporter.IsValidIdentifier(compression))
        {
            return null;
        }

        if (compressionCodec != null && compressionCodec.CanDecode(compression))
        {
            return compressionCodec;
        }

        if (BuiltInCompressionCodec.CanDecode(compression))
        {
            return BuiltInCompressionCodec;
        }

        return null;
    }

    internal static ITiffCompressionCodec? SelectEncoder(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        if (!TiffCompressionCapabilityReporter.IsValidIdentifier(compression))
        {
            return null;
        }

        if (compressionCodec != null && compressionCodec.CanEncode(compression))
        {
            return compressionCodec;
        }

        if (BuiltInCompressionCodec.CanEncode(compression))
        {
            return BuiltInCompressionCodec;
        }

        return null;
    }

    internal static bool CanDecodeCompression(TiffCompression compression)
    {
        return SelectDecoder(compression, null) != null;
    }

    internal static bool CanDecodeCompressionWithCodec(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        return SelectDecoder(compression, compressionCodec) != null;
    }

    internal static bool CanEncodeCompression(TiffCompression compression)
    {
        return SelectEncoder(compression, null) != null;
    }

    internal static bool CanEncodeCompressionWithCodec(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        return SelectEncoder(compression, compressionCodec) != null;
    }

    internal static TiffCompressionCapabilityResult GetCompressionCapability(TiffCompression compression)
    {
        return TiffCompressionCapabilityReporter.CreateBuiltInReport(compression);
    }

    internal static TiffCompressionCapabilityResult GetCompressionCapabilityWithCodec(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        return TiffCompressionCapabilityReporter.CreateReportWithCodec(compression, compressionCodec);
    }

    /// <summary>Determines whether can pass through raw compression under the current TIFF data and policy.</summary>
    /// <param name="compression">The compression.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public static bool CanPassThroughRawCompression(TiffCompression compression)
    {
        return CanPassThroughRawCompression(compression, null);
    }

    /// <summary>Determines whether raw bytes can be preserved because the compression value is not supported for both decoding and encoding.</summary>
    /// <param name="compression">The TIFF compression value.</param>
    /// <param name="compressionCodec">The optional compression implementation to consider before the built-in codecs.</param>
    /// <returns><c>true</c> when the known compression value lacks decoding or encoding support and can be preserved as raw bytes; otherwise, <c>false</c>.</returns>
    public static bool CanPassThroughRawCompression(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        if (!TiffCompressionCapabilityReporter.IsValidIdentifier(compression))
        {
            return false;
        }

        if (CanDecodeCompressionWithCodec(compression, compressionCodec) && CanEncodeCompressionWithCodec(compression, compressionCodec))
        {
            return false;
        }

        return true;
    }

    /// <summary>Determines whether is compression supported under the current TIFF data and policy.</summary>
    /// <param name="compression">The compression.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public static bool IsCompressionSupported(TiffCompression compression)
    {
        return IsCompressionSupported(compression, null);
    }

    /// <summary>Determines whether the caller-provided or built-in codecs support both decoding and encoding for the supplied compression value.</summary>
    /// <param name="compression">The TIFF compression value.</param>
    /// <param name="compressionCodec">The optional compression implementation to consider before the built-in codecs.</param>
    /// <returns><c>true</c> when decoding and encoding are both supported; otherwise, <c>false</c>.</returns>
    public static bool IsCompressionSupported(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        return CanDecodeCompressionWithCodec(compression, compressionCodec) && CanEncodeCompressionWithCodec(compression, compressionCodec);
    }

    internal static byte[] NormalizeDecodeSource(TiffCodecContext context, ReadOnlySpan<byte> source)
    {
        if (ShouldReverseEncodedFillOrder(context))
        {
            return ReverseBitsInBytes(source);
        }

        return new byte[0];
    }

    internal static byte[] PrepareFillOrderBytes(TiffCodecContext context, ReadOnlySpan<byte> source)
    {
        if (ShouldReverseDecodedFillOrder(context))
        {
            return ReverseBitsInBytes(source);
        }

        return TiffIfdEntry.CopyBytes(source);
    }

    internal static byte[] UnpackSubByteSamples(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffSubByteSampleUnpacker.Unpack(context, source, diagnostics);
    }

    internal static byte[] PackSubByteSamples(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffSubByteSamplePacker.Pack(context, source, diagnostics);
    }

    /// <summary>Converts the samples to double while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="context">The codec context describing compression, layout, segment size, and safety limits.</param>
    /// <param name="source">The source bytes or samples to process.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static double[] ConvertSamplesToDouble(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return TiffSampleFormatConverter.ConvertToDoubleSamples(context, source, diagnostics);
    }

    /// <summary>Expands palette to RGB 16 into the requested output channel representation.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="paletteIndices">The palette indices.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static ushort[] ExpandPaletteToRgb16(TiffImageInfo info, ReadOnlySpan<double> paletteIndices, TiffDiagnostics diagnostics)
    {
        return TiffPaletteColorMapExpander.ExpandToRgb16(info, paletteIndices, diagnostics);
    }

    /// <summary>Expands palette to RGB 8 into the requested output channel representation.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="paletteIndices">The palette indices.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static byte[] ExpandPaletteToRgb8(TiffImageInfo info, ReadOnlySpan<double> paletteIndices, TiffDiagnostics diagnostics)
    {
        return TiffPaletteColorMapExpander.ExpandToRgb8(info, paletteIndices, diagnostics);
    }

    /// <summary>Converts the grayscale to RGB 16 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="grayscaleSamples">The grayscale samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static ushort[] ConvertGrayscaleToRgb16(TiffImageInfo info, ReadOnlySpan<double> grayscaleSamples, TiffDiagnostics diagnostics)
    {
        return TiffGrayscalePhotometricConverter.ConvertToRgb16(info, grayscaleSamples, diagnostics);
    }

    /// <summary>Converts the grayscale to RGB 8 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="grayscaleSamples">The grayscale samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static byte[] ConvertGrayscaleToRgb8(TiffImageInfo info, ReadOnlySpan<double> grayscaleSamples, TiffDiagnostics diagnostics)
    {
        return TiffGrayscalePhotometricConverter.ConvertToRgb8(info, grayscaleSamples, diagnostics);
    }

    /// <summary>Converts the RGB to RGB 8 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static byte[] ConvertRgbToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return TiffRgbPhotometricConverter.ConvertToRgb8(info, samples, diagnostics);
    }

    /// <summary>Converts the RGB to RGB 16 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static ushort[] ConvertRgbToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return TiffRgbPhotometricConverter.ConvertToRgb16(info, samples, diagnostics);
    }

    /// <summary>Converts the RGBA to RGBA 8 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static byte[] ConvertRgbaToRgba8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return TiffRgbPhotometricConverter.ConvertToRgba8(info, samples, diagnostics);
    }

    /// <summary>Converts the RGBA to RGBA 16 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static ushort[] ConvertRgbaToRgba16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return TiffRgbPhotometricConverter.ConvertToRgba16(info, samples, diagnostics);
    }

    /// <summary>Normalizes 8-bit RGBA alpha to the library's straight-alpha representation.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="rgba">The rgba.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static byte[] NormalizeRgbaAlpha8(TiffImageInfo info, ReadOnlySpan<byte> rgba, TiffDiagnostics diagnostics)
    {
        return TiffAlphaHandler.NormalizeRgba8(info, rgba, diagnostics);
    }

    /// <summary>Normalizes 16-bit RGBA alpha to the library's straight-alpha representation.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="rgba">The rgba.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static ushort[] NormalizeRgbaAlpha16(TiffImageInfo info, ReadOnlySpan<ushort> rgba, TiffDiagnostics diagnostics)
    {
        return TiffAlphaHandler.NormalizeRgba16(info, rgba, diagnostics);
    }

    /// <summary>Converts the CMYK to RGB 8 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static byte[] ConvertCmykToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return TiffCmykPhotometricConverter.ConvertToRgb8(info, samples, diagnostics);
    }

    /// <summary>Converts the CMYK to RGB 16 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static ushort[] ConvertCmykToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return TiffCmykPhotometricConverter.ConvertToRgb16(info, samples, diagnostics);
    }

    /// <summary>Converts the Y cb cr to RGB 8 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static byte[] ConvertYCbCrToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return TiffYCbCrPhotometricConverter.ConvertToRgb8(info, samples, diagnostics);
    }

    /// <summary>Converts the Y cb cr to RGB 16 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static ushort[] ConvertYCbCrToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return TiffYCbCrPhotometricConverter.ConvertToRgb16(info, samples, diagnostics);
    }

    /// <summary>Converts the lab to RGB 8 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static byte[] ConvertLabToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return TiffLabPhotometricConverter.ConvertToRgb8(info, samples, diagnostics);
    }

    /// <summary>Converts the lab to RGB 16 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static ushort[] ConvertLabToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return TiffLabPhotometricConverter.ConvertToRgb16(info, samples, diagnostics);
    }

    /// <summary>Converts the log L to RGB 8 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static byte[] ConvertLogLToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return TiffSgiLogPhotometricConverter.ConvertLogLToRgb8(info, samples, diagnostics);
    }

    /// <summary>Converts the log L to RGB 16 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static ushort[] ConvertLogLToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return TiffSgiLogPhotometricConverter.ConvertLogLToRgb16(info, samples, diagnostics);
    }

    /// <summary>Converts the log luv to RGB 8 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static byte[] ConvertLogLuvToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return TiffSgiLogPhotometricConverter.ConvertLogLuvToRgb8(info, samples, diagnostics);
    }

    /// <summary>Converts the log luv to RGB 16 while honoring the image sample layout and diagnostics policy.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A newly allocated converted sample buffer.</returns>
    public static ushort[] ConvertLogLuvToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return TiffSgiLogPhotometricConverter.ConvertLogLuvToRgb16(info, samples, diagnostics);
    }

    /// <summary>Applies orientation and returns the transformed data with diagnostics.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="source">The source bytes or samples to process.</param>
    /// <param name="bytesPerPixel">The bytes per pixel.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffOrientationTransformResult ApplyOrientation(TiffImageInfo info, ReadOnlySpan<byte> source, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        return TiffOrientationTransformer.Transform(info, source, bytesPerPixel, diagnostics);
    }

    /// <summary>Reads RGBA 8 from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="offsets">The offsets.</param>
    /// <param name="byteCounts">The byte counts.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffRgba8ReadResult ReadRgba8(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes)
    {
        return TiffRgba8Reader.Read(info, offsets, byteCounts, fileBytes);
    }

    /// <summary>Reads RGBA 8 from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffRgba8ReadResult ReadRgba8(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return TiffRgba8Reader.Read(directory, fileBytes, byteOrder);
    }

    /// <summary>Reads RGBA 16 from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="offsets">The offsets.</param>
    /// <param name="byteCounts">The byte counts.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffRgba16ReadResult ReadRgba16(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes)
    {
        return TiffRgba16Reader.Read(info, offsets, byteCounts, fileBytes);
    }

    /// <summary>Reads RGBA 16 from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffRgba16ReadResult ReadRgba16(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return TiffRgba16Reader.Read(directory, fileBytes, byteOrder);
    }

    /// <summary>Reads source raster from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="offsets">The offsets.</param>
    /// <param name="byteCounts">The byte counts.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffSourceRasterReadResult ReadSourceRaster(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes)
    {
        return TiffSourceRasterReader.Read(info, offsets, byteCounts, fileBytes);
    }

    /// <summary>Reads source raster from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffSourceRasterReadResult ReadSourceRaster(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return TiffSourceRasterReader.Read(directory, fileBytes, byteOrder);
    }

    /// <summary>Writes source raster to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="offsets">The offsets.</param>
    /// <param name="byteCounts">The byte counts.</param>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <param name="source">The source bytes or samples to process.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffSourceRasterWriteResult WriteSourceRaster(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<byte> source)
    {
        return TiffSourceRasterWriter.Write(info, offsets, byteCounts, destination, source);
    }

    /// <summary>Writes RGBA 8 to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="offsets">The offsets.</param>
    /// <param name="byteCounts">The byte counts.</param>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <param name="rgba">The rgba.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffSourceRasterWriteResult WriteRgba8(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<byte> rgba)
    {
        return TiffRgba8Writer.Write(info, offsets, byteCounts, destination, rgba);
    }

    /// <summary>Writes RGBA 16 to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="offsets">The offsets.</param>
    /// <param name="byteCounts">The byte counts.</param>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <param name="rgba">The rgba.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffSourceRasterWriteResult WriteRgba16(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<ushort> rgba)
    {
        return TiffRgba16Writer.Write(info, offsets, byteCounts, destination, rgba);
    }

    /// <summary>Decodes one TIFF directory entry into its typed public tag-value representation.</summary>
    /// <remarks>Inline values are read from the entry's value slot; external values are read from <c>fileBytes</c>. Allocation and byte-range failures are added to <c>diagnostics</c>.</remarks>
    /// <param name="entry">The TIFF directory entry to decode.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>The typed tag value, or an empty value when decoding fails.</returns>
    public static TiffTagValue ReadTagValue(TiffIfdEntry entry, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        return TiffExifMetadataReader.ReadValue(entry, fileBytes, byteOrder, new TiffLimits(), diagnostics);
    }

    /// <summary>Reads EXIF from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="header">The parsed TIFF header that supplies byte order and container kind.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffExifReadResult ReadExif(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        return TiffExifMetadataReader.Read(directory, fileBytes, header);
    }

    /// <summary>Reads EXIF from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="fileKind">The file kind.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffExifReadResult ReadExif(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind)
    {
        return TiffExifMetadataReader.Read(directory, fileBytes, byteOrder, fileKind);
    }

    /// <summary>Writes classic EXIF to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <param name="parentEntries">The parent entries.</param>
    /// <param name="exifValues">The exif values.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="parentOffset">The parent offset.</param>
    /// <param name="nextIfdOffset">The next IFD offset.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffExifWriteResult WriteClassicExif(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffTagValue[] exifValues, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset)
    {
        return TiffExifMetadataWriter.WriteClassic(destination, parentEntries, exifValues, byteOrder, parentOffset, nextIfdOffset);
    }

    /// <summary>Reads GPS from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="header">The parsed TIFF header that supplies byte order and container kind.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffGpsReadResult ReadGps(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        return TiffGpsMetadataReader.Read(directory, fileBytes, header);
    }

    /// <summary>Reads GPS from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="fileKind">The file kind.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffGpsReadResult ReadGps(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind)
    {
        return TiffGpsMetadataReader.Read(directory, fileBytes, byteOrder, fileKind);
    }

    /// <summary>Writes classic GPS to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <param name="parentEntries">The parent entries.</param>
    /// <param name="gpsValues">The gps values.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="parentOffset">The parent offset.</param>
    /// <param name="nextIfdOffset">The next IFD offset.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffGpsWriteResult WriteClassicGps(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffTagValue[] gpsValues, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset)
    {
        return TiffGpsMetadataWriter.WriteClassic(destination, parentEntries, gpsValues, byteOrder, parentOffset, nextIfdOffset);
    }

    /// <summary>Reads XMP from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffXmpReadResult ReadXmp(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffXmpMetadataReader.Read(directory, fileBytes);
    }

    /// <summary>Writes classic XMP to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <param name="values">The values.</param>
    /// <param name="xmpBytes">The xmp bytes.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next IFD offset.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffXmpWriteResult WriteClassicXmp(Span<byte> destination, TiffTagValue[] values, byte[] xmpBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return TiffXmpMetadataWriter.WriteClassic(destination, values, xmpBytes, byteOrder, directoryOffset, nextIfdOffset);
    }

    /// <summary>Reads ICC profile from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffIccProfileReadResult ReadIccProfile(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffIccProfileReader.Read(directory, fileBytes);
    }

    /// <summary>Writes classic ICC profile to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <param name="values">The values.</param>
    /// <param name="profileBytes">The profile bytes.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next IFD offset.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffIccProfileWriteResult WriteClassicIccProfile(Span<byte> destination, TiffTagValue[] values, byte[] profileBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return TiffIccProfileWriter.WriteClassic(destination, values, profileBytes, byteOrder, directoryOffset, nextIfdOffset);
    }

    /// <summary>Reads photoshop from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffPhotoshopReadResult ReadPhotoshop(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffPhotoshopMetadataReader.Read(directory, fileBytes);
    }

    /// <summary>Reads IPTC from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffIptcReadResult ReadIptc(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffIptcMetadataReader.Read(directory, fileBytes);
    }

    /// <summary>Reads image description from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffImageDescriptionReadResult ReadImageDescription(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffImageDescriptionReader.Read(directory, fileBytes);
    }

    /// <summary>Reads geographic model from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="header">The parsed TIFF header that supplies byte order and container kind.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffGeoModelReadResult ReadGeoModel(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        return TiffGeoModelMetadataReader.Read(directory, fileBytes, header);
    }

    /// <summary>Reads geographic model from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffGeoModelReadResult ReadGeoModel(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return TiffGeoModelMetadataReader.Read(directory, fileBytes, byteOrder);
    }

    /// <summary>Reads geographic key directory from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="header">The parsed TIFF header that supplies byte order and container kind.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffGeoKeyDirectoryReadResult ReadGeoKeyDirectory(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        return TiffGeoKeyDirectoryReader.Read(directory, fileBytes, header);
    }

    /// <summary>Reads geographic key directory from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffGeoKeyDirectoryReadResult ReadGeoKeyDirectory(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return TiffGeoKeyDirectoryReader.Read(directory, fileBytes, byteOrder);
    }

    /// <summary>Reads geographic parameters from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="header">The parsed TIFF header that supplies byte order and container kind.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffGeoParameterReadResult ReadGeoParameters(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        return TiffGeoParameterReader.Read(directory, fileBytes, header);
    }

    /// <summary>Reads geographic parameters from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffGeoParameterReadResult ReadGeoParameters(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return TiffGeoParameterReader.Read(directory, fileBytes, byteOrder);
    }

    /// <summary>Writes classic geographic TIFF to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <param name="values">The values.</param>
    /// <param name="modelMetadata">The model metadata.</param>
    /// <param name="keyDirectory">The key directory.</param>
    /// <param name="parameters">The parameters.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next IFD offset.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffGeoTiffWriteResult WriteClassicGeoTiff(Span<byte> destination, TiffTagValue[] values, TiffGeoModelMetadata modelMetadata, TiffGeoKeyDirectory keyDirectory, TiffGeoParameterMetadata parameters, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return TiffGeoTiffMetadataWriter.WriteClassic(destination, values, modelMetadata, keyDirectory, parameters, byteOrder, directoryOffset, nextIfdOffset);
    }

    /// <summary>Maps an image-space coordinate through the available GeoTIFF transformation or scale-and-tiepoint model.</summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="rasterI">The raster I.</param>
    /// <param name="rasterJ">The raster J.</param>
    /// <param name="rasterK">The raster K.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffGeoTransformResult ImageToGeoModel(TiffGeoModelMetadata metadata, double rasterI, double rasterJ, double rasterK)
    {
        return TiffGeoTransform.ImageToModel(metadata, rasterI, rasterJ, rasterK);
    }

    /// <summary>Maps a model-space coordinate back to image space when the GeoTIFF transform is invertible.</summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="modelX">The model X.</param>
    /// <param name="modelY">The model Y.</param>
    /// <param name="modelZ">The model Z.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffGeoTransformResult GeoModelToImage(TiffGeoModelMetadata metadata, double modelX, double modelY, double modelZ)
    {
        return TiffGeoTransform.ModelToImage(metadata, modelX, modelY, modelZ);
    }

    /// <summary>Maps a pixel index into model space using RasterPixelIsArea or RasterPixelIsPoint semantics.</summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="rasterType">The raster pixel semantics.</param>
    /// <param name="pixelColumn">The pixel column.</param>
    /// <param name="pixelRow">The pixel row.</param>
    /// <param name="rasterK">The raster K coordinate.</param>
    /// <returns>A result containing the model coordinate and diagnostics.</returns>
    public static TiffGeoTransformResult ImagePixelToGeoModel(TiffGeoModelMetadata metadata, TiffGeoRasterType rasterType, double pixelColumn, double pixelRow, double rasterK)
    {
        return TiffGeoTransform.ImagePixelToModel(metadata, rasterType, pixelColumn, pixelRow, rasterK);
    }

    /// <summary>Maps a model coordinate back to a pixel index using RasterPixelIsArea or RasterPixelIsPoint semantics.</summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="rasterType">The raster pixel semantics.</param>
    /// <param name="modelX">The model X.</param>
    /// <param name="modelY">The model Y.</param>
    /// <param name="modelZ">The model Z.</param>
    /// <returns>A result containing the pixel coordinate and diagnostics.</returns>
    public static TiffGeoTransformResult GeoModelToImagePixel(TiffGeoModelMetadata metadata, TiffGeoRasterType rasterType, double modelX, double modelY, double modelZ)
    {
        return TiffGeoTransform.ModelToImagePixel(metadata, rasterType, modelX, modelY, modelZ);
    }

    /// <summary>Reads GDAL metadata from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffGdalMetadataReadResult ReadGdalMetadata(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffGdalMetadataReader.Read(directory, fileBytes);
    }

    /// <summary>Writes classic GDAL metadata to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <param name="values">The values.</param>
    /// <param name="metadata">The metadata.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next IFD offset.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffGdalMetadataWriteResult WriteClassicGdalMetadata(Span<byte> destination, TiffTagValue[] values, TiffGdalMetadata metadata, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return TiffGdalMetadataWriter.WriteClassic(destination, values, metadata, byteOrder, directoryOffset, nextIfdOffset);
    }

    /// <summary>Reads vendor metadata from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffVendorMetadataReadResult ReadVendorMetadata(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffVendorMetadataReader.Read(directory, fileBytes);
    }

    /// <summary>Reads OME metadata from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffOmeMetadataReadResult ReadOmeMetadata(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffOmeMetadataReader.Read(directory, fileBytes);
    }

    /// <summary>Writes classic OME metadata to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <param name="values">The values.</param>
    /// <param name="omeXml">The ome xml.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next IFD offset.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffOmeMetadataWriteResult WriteClassicOmeMetadata(Span<byte> destination, TiffTagValue[] values, string omeXml, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return TiffOmeMetadataWriter.WriteClassic(destination, values, omeXml, byteOrder, directoryOffset, nextIfdOffset);
    }

    /// <summary>Reads image J metadata from the supplied TIFF data and reports format problems through diagnostics.</summary>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffImageJMetadataReadResult ReadImageJMetadata(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return TiffImageJMetadataReader.Read(directory, fileBytes);
    }

    /// <summary>Writes classic image J metadata to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <param name="values">The values.</param>
    /// <param name="metadata">The metadata.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next IFD offset.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffImageJMetadataWriteResult WriteClassicImageJMetadata(Span<byte> destination, TiffTagValue[] values, TiffImageJMetadata metadata, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return TiffImageJMetadataWriter.WriteClassic(destination, values, metadata, byteOrder, directoryOffset, nextIfdOffset);
    }

    /// <summary>Groups series into related multidimensional series.</summary>
    /// <param name="directories">The directories.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffSeriesGroupResult GroupSeries(TiffDirectory[] directories, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return TiffSeriesBuilder.Group(directories, fileBytes, byteOrder);
    }

    /// <summary>Locates tile and returns its file range and image coordinates.</summary>
    /// <param name="info">The normalized image layout and pixel information.</param>
    /// <param name="tileOffsets">The tile offsets.</param>
    /// <param name="tileByteCounts">The tile byte counts.</param>
    /// <param name="column">The column.</param>
    /// <param name="row">The row.</param>
    /// <param name="sampleIndex">The zero-based sample plane or channel index.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffTileLookupResult LookupTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ulong column, ulong row, int sampleIndex)
    {
        return TiffTileLookupBuilder.Lookup(info, tileOffsets, tileByteCounts, column, row, sampleIndex);
    }

    /// <summary>Locates tile level and returns its file range and image coordinates.</summary>
    /// <param name="levelInfos">The level infos.</param>
    /// <param name="tileOffsetsByLevel">The tile offsets by level.</param>
    /// <param name="tileByteCountsByLevel">The tile byte counts by level.</param>
    /// <param name="levelIndex">The level index.</param>
    /// <param name="column">The column.</param>
    /// <param name="row">The row.</param>
    /// <param name="sampleIndex">The zero-based sample plane or channel index.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffTileLookupResult LookupTileLevel(TiffImageInfo[] levelInfos, ulong[][] tileOffsetsByLevel, ulong[][] tileByteCountsByLevel, int levelIndex, ulong column, ulong row, int sampleIndex)
    {
        return TiffTileLookupBuilder.LookupLevel(levelInfos, tileOffsetsByLevel, tileByteCountsByLevel, levelIndex, column, row, sampleIndex);
    }

    /// <summary>Locates tile level from base coordinate and returns its file range and image coordinates.</summary>
    /// <param name="levelInfos">The level infos.</param>
    /// <param name="tileOffsetsByLevel">The tile offsets by level.</param>
    /// <param name="tileByteCountsByLevel">The tile byte counts by level.</param>
    /// <param name="levelIndex">The level index.</param>
    /// <param name="baseColumn">The base column.</param>
    /// <param name="baseRow">The base row.</param>
    /// <param name="sampleIndex">The zero-based sample plane or channel index.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffTileLookupResult LookupTileLevelFromBaseCoordinate(TiffImageInfo[] levelInfos, ulong[][] tileOffsetsByLevel, ulong[][] tileByteCountsByLevel, int levelIndex, ulong baseColumn, ulong baseRow, int sampleIndex)
    {
        return TiffTileLookupBuilder.LookupLevelFromBaseCoordinate(levelInfos, tileOffsetsByLevel, tileByteCountsByLevel, levelIndex, baseColumn, baseRow, sampleIndex);
    }

    /// <summary>Locates best tile level and returns its file range and image coordinates.</summary>
    /// <param name="levelInfos">The level infos.</param>
    /// <param name="tileOffsetsByLevel">The tile offsets by level.</param>
    /// <param name="tileByteCountsByLevel">The tile byte counts by level.</param>
    /// <param name="requestedWidth">The requested width.</param>
    /// <param name="requestedLength">The requested length.</param>
    /// <param name="column">The column.</param>
    /// <param name="row">The row.</param>
    /// <param name="sampleIndex">The zero-based sample plane or channel index.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffTileLookupResult LookupBestTileLevel(TiffImageInfo[] levelInfos, ulong[][] tileOffsetsByLevel, ulong[][] tileByteCountsByLevel, ulong requestedWidth, ulong requestedLength, ulong column, ulong row, int sampleIndex)
    {
        return TiffTileLookupBuilder.LookupBestLevel(levelInfos, tileOffsetsByLevel, tileByteCountsByLevel, requestedWidth, requestedLength, column, row, sampleIndex);
    }

    /// <summary>Finds the best tile level, or returns the type's documented missing-value result.</summary>
    /// <param name="levelInfos">The level infos.</param>
    /// <param name="requestedWidth">The requested width.</param>
    /// <param name="requestedLength">The requested length.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    /// <returns>The requested value or model.</returns>
    public static int FindBestTileLevel(TiffImageInfo[] levelInfos, ulong requestedWidth, ulong requestedLength, TiffDiagnostics diagnostics)
    {
        return TiffTileLookupBuilder.FindBestLevel(levelInfos, requestedWidth, requestedLength, diagnostics);
    }

    /// <summary>Detects pyramid from directory relationships and reduced-resolution tags.</summary>
    /// <param name="parent">The parent.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="fileKind">The file kind.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffPyramidDetectionResult DetectPyramid(TiffDirectory parent, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind)
    {
        return TiffPyramidDetector.Detect(parent, fileBytes, byteOrder, fileKind);
    }

    /// <summary>Detects pyramid from directory relationships and reduced-resolution tags.</summary>
    /// <param name="parent">The parent.</param>
    /// <param name="subIfds">The sub IFD s.</param>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffPyramidDetectionResult DetectPyramid(TiffDirectory parent, TiffSubIfd[] subIfds, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return TiffPyramidDetector.Detect(parent, subIfds, fileBytes, byteOrder);
    }

    /// <summary>Writes classic pyramid to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <param name="parentEntries">The parent entries.</param>
    /// <param name="overviewDirectories">The overview directories.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="parentOffset">The parent offset.</param>
    /// <param name="nextIfdOffset">The next IFD offset.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffPyramidWriteResult WriteClassicPyramid(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffIfdEntry[][] overviewDirectories, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset)
    {
        return TiffPyramidWriter.WriteClassic(destination, parentEntries, overviewDirectories, byteOrder, parentOffset, nextIfdOffset);
    }

    /// <summary>Writes big TIFF pyramid to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <param name="parentEntries">The parent entries.</param>
    /// <param name="overviewDirectories">The overview directories.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="parentOffset">The parent offset.</param>
    /// <param name="nextIfdOffset">The next IFD offset.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffPyramidWriteResult WriteBigTiffPyramid(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffIfdEntry[][] overviewDirectories, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset)
    {
        return TiffPyramidWriter.WriteBigTiff(destination, parentEntries, overviewDirectories, byteOrder, parentOffset, nextIfdOffset);
    }

    /// <summary>Returns the number of available calculate classic pyramid write byte entries.</summary>
    /// <param name="parentEntries">The parent entries.</param>
    /// <param name="overviewDirectories">The overview directories.</param>
    /// <returns>The calculated value; diagnostics describe invalid or overflowing input.</returns>
    public static int CalculateClassicPyramidWriteByteCount(TiffIfdEntry[] parentEntries, TiffIfdEntry[][] overviewDirectories)
    {
        return TiffPyramidWriter.CalculateClassicWriteByteCount(parentEntries, overviewDirectories);
    }

    /// <summary>Returns the number of available calculate big TIFF pyramid write byte entries.</summary>
    /// <param name="parentEntries">The parent entries.</param>
    /// <param name="overviewDirectories">The overview directories.</param>
    /// <returns>The calculated value; diagnostics describe invalid or overflowing input.</returns>
    public static int CalculateBigTiffPyramidWriteByteCount(TiffIfdEntry[] parentEntries, TiffIfdEntry[][] overviewDirectories)
    {
        return TiffPyramidWriter.CalculateBigTiffWriteByteCount(parentEntries, overviewDirectories);
    }

    /// <summary>Writes classic image description to the supplied destination and reports validation failures through diagnostics.</summary>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <param name="values">The values.</param>
    /// <param name="description">The description.</param>
    /// <param name="byteOrder">The byte order used by multibyte file values.</param>
    /// <param name="directoryOffset">The directory offset.</param>
    /// <param name="nextIfdOffset">The next IFD offset.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffImageDescriptionWriteResult WriteClassicImageDescription(Span<byte> destination, TiffTagValue[] values, string description, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return TiffImageDescriptionWriter.WriteClassic(destination, values, description, byteOrder, directoryOffset, nextIfdOffset);
    }

    internal static byte[] ConvertFileSamplesToHostByteOrder(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return ConvertSampleByteOrder(context, source, diagnostics);
    }

    internal static byte[] ConvertHostSamplesToFileByteOrder(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        return ConvertSampleByteOrder(context, source, diagnostics);
    }

    internal static byte[] ConvertSampleByteOrder(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        byte[] bytes = TiffIfdEntry.CopyBytes(source);
        if (!ShouldSwapSampleByteOrder(context, diagnostics))
        {
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            return bytes;
        }

        SwapSampleByteOrder(context, bytes, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        return bytes;
    }

    internal static bool ShouldSwapSampleByteOrder(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (context == null || context.Info == null)
        {
            return false;
        }

        if (!HasByteSwappableSamples(context, diagnostics))
        {
            return false;
        }

        if (context.ByteOrder == TiffByteOrder.BigEndian)
        {
            return true;
        }

        if (context.ByteOrder == TiffByteOrder.LittleEndian)
        {
            return false;
        }

        diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "A known byte order is required for 16, 32, or 64-bit samples.");
        return false;
    }

    internal static bool HasByteSwappableSamples(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        int samplesPerPixel = CalculateSampleByteOrderSamplesPerPixel(context, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        int separateSampleIndex = 0;
        if (context.Info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            separateSampleIndex = CalculateSampleByteOrderSampleIndex(context, diagnostics);
            if (diagnostics.HasErrors())
            {
                return false;
            }
        }

        int sampleIndex = 0;
        while (sampleIndex < samplesPerPixel)
        {
            int infoSampleIndex = sampleIndex;
            if (context.Info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
            {
                infoSampleIndex = separateSampleIndex;
            }

            int bitsPerSample = GetByteOrderBitsPerSample(context.Info, infoSampleIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return false;
            }

            if (IsByteSwappableSample(bitsPerSample))
            {
                return true;
            }

            sampleIndex = sampleIndex + 1;
        }

        return false;
    }

    internal static void SwapSampleByteOrder(TiffCodecContext context, byte[] bytes, TiffDiagnostics diagnostics)
    {
        int samplesPerPixel = CalculateSampleByteOrderSamplesPerPixel(context, diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        int separateSampleIndex = 0;
        if (context.Info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            separateSampleIndex = CalculateSampleByteOrderSampleIndex(context, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }
        }

        int pixelByteCount = CalculateSampleByteOrderPixelByteCount(context.Info, samplesPerPixel, separateSampleIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        if (pixelByteCount <= 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Sample byte order pixel layout must be positive.");
            return;
        }

        if (bytes.Length % pixelByteCount != 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Decoded byte count does not match the sample byte order layout.");
            return;
        }

        int pixelOffset = 0;
        while (pixelOffset < bytes.Length)
        {
            int sampleIndex = 0;
            int sampleOffset = pixelOffset;
            while (sampleIndex < samplesPerPixel)
            {
                int infoSampleIndex = sampleIndex;
                if (context.Info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
                {
                    infoSampleIndex = separateSampleIndex;
                }

                int bitsPerSample = GetByteOrderBitsPerSample(context.Info, infoSampleIndex, diagnostics);
                if (diagnostics.HasErrors())
                {
                    return;
                }

                int sampleByteCount = bitsPerSample / 8;
                if (IsByteSwappableSample(bitsPerSample))
                {
                    SwapOneSample(bytes, sampleOffset, sampleByteCount);
                }

                sampleOffset = sampleOffset + sampleByteCount;
                sampleIndex = sampleIndex + 1;
            }

            pixelOffset = pixelOffset + pixelByteCount;
        }
    }

    internal static int CalculateSampleByteOrderSamplesPerPixel(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (context.Info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            return 1;
        }

        if (context.Info.SamplesPerPixel <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SamplesPerPixel must be positive for sample byte order conversion.", context.Info.ImageIndex, TiffTagId.SamplesPerPixel);
            return 0;
        }

        return context.Info.SamplesPerPixel;
    }

    internal static int CalculateSampleByteOrderSampleIndex(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (context.Info.PlanarConfiguration != TiffPlanarConfiguration.Separate)
        {
            return 0;
        }

        if (context.Info.BitsPerSample != null && context.Info.BitsPerSample.Length == 1)
        {
            return 0;
        }

        ulong segmentsPerPlane = 0;
        if (context.IsTile)
        {
            segmentsPerPlane = TiffStrileLayout.CalculateTilesPerPlane(context.Info, diagnostics);
        }
        else
        {
            segmentsPerPlane = TiffStrileLayout.CalculateStripsPerPlane(context.Info, diagnostics);
        }

        if (segmentsPerPlane == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Sample byte order segment count per plane must be positive.");
            return 0;
        }

        ulong sampleIndex = context.SegmentIndex / segmentsPerPlane;
        if (sampleIndex > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Sample byte order sample index exceeds the supported range.");
            return 0;
        }

        return (int)sampleIndex;
    }

    internal static int CalculateSampleByteOrderPixelByteCount(TiffImageInfo info, int samplesPerPixel, int separateSampleIndex, TiffDiagnostics diagnostics)
    {
        long pixelByteCount = 0;
        int sampleIndex = 0;
        while (sampleIndex < samplesPerPixel)
        {
            int infoSampleIndex = sampleIndex;
            if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
            {
                infoSampleIndex = separateSampleIndex;
            }

            int bitsPerSample = GetByteOrderBitsPerSample(info, infoSampleIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return 0;
            }

            if (bitsPerSample <= 0 || bitsPerSample % 8 != 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Byte swapping requires byte-aligned positive BitsPerSample values.", info.ImageIndex, TiffTagId.BitsPerSample);
                return 0;
            }

            pixelByteCount = pixelByteCount + bitsPerSample / 8;
            if (pixelByteCount > int.MaxValue)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Sample byte order pixel layout exceeds the supported range.");
                return 0;
            }

            sampleIndex = sampleIndex + 1;
        }

        return (int)pixelByteCount;
    }

    internal static int GetByteOrderBitsPerSample(TiffImageInfo info, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (info.BitsPerSample == null || info.BitsPerSample.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample is required for sample byte order conversion.", info.ImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        if (info.BitsPerSample.Length == 1)
        {
            return info.BitsPerSample[0];
        }

        if (sampleIndex < 0 || sampleIndex >= info.BitsPerSample.Length)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample does not contain the sample required for byte order conversion.", info.ImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        return info.BitsPerSample[sampleIndex];
    }

    internal static bool IsByteSwappableSample(int bitsPerSample)
    {
        if (bitsPerSample == 16 || bitsPerSample == 32 || bitsPerSample == 64)
        {
            return true;
        }

        return false;
    }

    internal static void SwapOneSample(byte[] bytes, int offset, int sampleByteCount)
    {
        int left = 0;
        int right = sampleByteCount - 1;
        while (left < right)
        {
            byte temporary = bytes[offset + left];
            bytes[offset + left] = bytes[offset + right];
            bytes[offset + right] = temporary;
            left = left + 1;
            right = right - 1;
        }
    }

    internal static bool ShouldReverseDecodedFillOrder(TiffCodecContext context)
    {
        if (context == null || context.Info == null)
        {
            return false;
        }

        if (context.Info.FillOrder != 2)
        {
            return false;
        }

        return !UsesEncodedFillOrder(context.Info.Compression);
    }

    internal static bool ShouldReverseEncodedFillOrder(TiffCodecContext context)
    {
        if (context == null || context.Info == null)
        {
            return false;
        }

        if (context.Info.FillOrder != 2)
        {
            return false;
        }

        return UsesEncodedFillOrder(context.Info.Compression);
    }

    internal static bool UsesEncodedFillOrder(TiffCompression compression)
    {
        if (compression == TiffCompression.CcittRle || compression == TiffCompression.CcittGroup3 || compression == TiffCompression.CcittGroup4)
        {
            return true;
        }

        return false;
    }

    internal static byte[] ReverseBitsInBytes(ReadOnlySpan<byte> source)
    {
        byte[] destination = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            destination[index] = ReverseBits(source[index]);
            index = index + 1;
        }

        return destination;
    }

    internal static byte ReverseBits(byte value)
    {
        int input = value;
        int output = 0;
        int bitIndex = 0;
        while (bitIndex < 8)
        {
            output = output * 2 + (input & 1);
            input = input >> 1;
            bitIndex = bitIndex + 1;
        }

        return (byte)output;
    }

    internal static bool ValidateDecodeContext(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (context == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A codec context is required to decode image data.");
            return false;
        }

        if (context.Info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to decode image data.");
            return false;
        }

        if (!ValidateFillOrder(context, diagnostics))
        {
            return false;
        }

        return ValidateExpectedDecodedByteCount(context, diagnostics);
    }

    internal static bool ValidateDecodeContext(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (!ValidateDecodeContext(context, diagnostics))
        {
            return false;
        }

        return ValidateCodecExpansionLimit(context, source.Length, diagnostics);
    }

    internal static bool ValidateEncodeContext(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (context == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A codec context is required to encode image data.");
            return false;
        }

        if (context.Info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to encode image data.");
            return false;
        }

        if (!ValidateFillOrder(context, diagnostics))
        {
            return false;
        }

        return ValidateExpectedDecodedByteCount(context, diagnostics);
    }

    internal static bool ValidateFillOrder(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (context.Info.FillOrder == 1 || context.Info.FillOrder == 2)
        {
            return true;
        }

        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "FillOrder must be 1 or 2.", context.Info.ImageIndex, TiffTagId.FillOrder);
        return false;
    }

    internal static bool ValidateExpectedDecodedByteCount(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        TiffLimits limits = TiffTagValueLoader.NormalizeLimits(context.Limits);
        if (context.ExpectedDecodedByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Decoded byte count is too large to allocate.");
            return false;
        }

        if (!limits.AllowsDecodedBytes((long)context.ExpectedDecodedByteCount))
        {
            diagnostics.AddError(TiffErrorCode.CodecExpansionLimitExceeded, "Decoded byte count exceeds the configured expansion limit.");
            return false;
        }

        return true;
    }

    internal static bool ValidateCodecExpansionLimit(TiffCodecContext context, int encodedByteCount, TiffDiagnostics diagnostics)
    {
        TiffLimits limits = TiffTagValueLoader.NormalizeLimits(context.Limits);
        if (!limits.AllowsCodecExpansion((long)encodedByteCount, (long)context.ExpectedDecodedByteCount))
        {
            diagnostics.AddError(TiffErrorCode.CodecExpansionLimitExceeded, "Decoded byte count exceeds the configured codec expansion ratio.");
            return false;
        }

        return true;
    }
}
