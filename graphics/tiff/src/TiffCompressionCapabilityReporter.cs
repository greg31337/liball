namespace tiff;

internal static class TiffCompressionCapabilityReporter
{
    internal static TiffCompressionCapabilityResult CreateBuiltInReport(TiffCompression compression)
    {
        return CreateReportWithCodec(compression, null);
    }

    internal static TiffCompressionCapabilityResult CreateReportWithCodec(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!IsValidIdentifier(compression))
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, CreateInvalidIdentifierMessage(compression));
            return TiffCompressionCapabilityResult.Create(compression, TiffCodecCapabilitySource.Unavailable, TiffCodecCapabilitySource.Unavailable, false, diagnostics);
        }

        TiffCodecCapabilitySource decodeSource = GetDecodeSource(compression, compressionCodec);
        TiffCodecCapabilitySource encodeSource = GetEncodeSource(compression, compressionCodec);
        if (decodeSource == TiffCodecCapabilitySource.Unavailable)
        {
            diagnostics.AddWarning(TiffErrorCode.UnsupportedCompression, CreateUnavailableDirectionMessage(compression, "decode"));
        }

        if (encodeSource == TiffCodecCapabilitySource.Unavailable)
        {
            diagnostics.AddWarning(TiffErrorCode.UnsupportedCompression, CreateUnavailableDirectionMessage(compression, "encode"));
        }

        bool canPassThroughRaw = decodeSource == TiffCodecCapabilitySource.Unavailable || encodeSource == TiffCodecCapabilitySource.Unavailable;
        return TiffCompressionCapabilityResult.Create(compression, decodeSource, encodeSource, canPassThroughRaw, diagnostics);
    }

    internal static bool IsValidIdentifier(TiffCompression compression)
    {
        int value = (int)compression;
        return value >= 1 && value <= 65535;
    }

    internal static string CreateUnavailableDirectionMessage(TiffCompression compression, string operation)
    {
        string direction = "decoder";
        if (operation == "encode")
        {
            direction = "encoder";
        }

        return GetDisplayName(compression) + " compression (identifier " + ((int)compression).ToString() + ") has no available " + direction + ". Supply an ITiffCompressionCodec for this direction or preserve the encoded bytes through raw pass-through.";
    }

    internal static string GetDisplayName(TiffCompression compression)
    {
        if (compression == TiffCompression.None)
        {
            return "None";
        }

        if (compression == TiffCompression.PackBits)
        {
            return "PackBits";
        }

        if (compression == TiffCompression.Deflate)
        {
            return "Deflate/ZLib";
        }

        if (compression == TiffCompression.AdobeDeflate)
        {
            return "Adobe Deflate/ZLib";
        }

        if (compression == TiffCompression.Lzw)
        {
            return "LZW";
        }

        if (compression == TiffCompression.CcittRle)
        {
            return "CCITT Modified Huffman";
        }

        if (compression == TiffCompression.CcittGroup3)
        {
            return "CCITT Group 3";
        }

        if (compression == TiffCompression.CcittGroup4)
        {
            return "CCITT Group 4";
        }

        if (compression == TiffCompression.CcittRleW)
        {
            return "CCITT word-aligned Modified Huffman";
        }

        if (compression == TiffCompression.ThunderScan)
        {
            return "ThunderScan";
        }

        if (compression == TiffCompression.NeXt)
        {
            return "NeXT 2-bit";
        }

        if (compression == TiffCompression.OldJpeg)
        {
            return "old-style JPEG";
        }

        if (compression == TiffCompression.Jpeg)
        {
            return "JPEG";
        }

        if (compression == TiffCompression.T85)
        {
            return "T.85";
        }

        if (compression == TiffCompression.T43)
        {
            return "T.43";
        }

        if (compression == TiffCompression.It8Ctpad)
        {
            return "IT8 CT padding";
        }

        if (compression == TiffCompression.It8Lw)
        {
            return "IT8 linework";
        }

        if (compression == TiffCompression.It8Mp)
        {
            return "IT8 monochrome picture";
        }

        if (compression == TiffCompression.It8Bl)
        {
            return "IT8 binary line art";
        }

        if (compression == TiffCompression.PixarFilm)
        {
            return "Pixar Film";
        }

        if (compression == TiffCompression.PixarLog)
        {
            return "Pixar Log";
        }

        if (compression == TiffCompression.Dcs)
        {
            return "Kodak DCS";
        }

        if (compression == TiffCompression.Jbig)
        {
            return "JBIG";
        }

        if (compression == TiffCompression.Sgilog)
        {
            return "SGILog";
        }

        if (compression == TiffCompression.Sgilog24)
        {
            return "SGILog24";
        }

        if (compression == TiffCompression.Jpeg2000)
        {
            return "JPEG 2000";
        }

        if (compression == TiffCompression.Lzma)
        {
            return "LZMA";
        }

        if (compression == TiffCompression.Zstd)
        {
            return "Zstandard";
        }

        if (compression == TiffCompression.WebP)
        {
            return "WebP";
        }

        if (compression == TiffCompression.Jxl)
        {
            return "JPEG XL";
        }

        return "external/private";
    }

    internal static TiffCodecCapabilitySource GetDecodeSource(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        if (compressionCodec != null && compressionCodec.CanDecode(compression))
        {
            return TiffCodecCapabilitySource.ApplicationProvided;
        }

        if (TiffCodec.BuiltInCompressionCodec.CanDecode(compression))
        {
            return TiffCodecCapabilitySource.BuiltIn;
        }

        return TiffCodecCapabilitySource.Unavailable;
    }

    internal static TiffCodecCapabilitySource GetEncodeSource(TiffCompression compression, ITiffCompressionCodec? compressionCodec)
    {
        if (compressionCodec != null && compressionCodec.CanEncode(compression))
        {
            return TiffCodecCapabilitySource.ApplicationProvided;
        }

        if (TiffCodec.BuiltInCompressionCodec.CanEncode(compression))
        {
            return TiffCodecCapabilitySource.BuiltIn;
        }

        return TiffCodecCapabilitySource.Unavailable;
    }

    internal static string CreateInvalidIdentifierMessage(TiffCompression compression)
    {
        if (compression == TiffCompression.Unknown)
        {
            return "Compression identifier 0 is unknown and cannot be decoded, encoded, or preserved through raw pass-through.";
        }

        return "Compression identifier " + ((int)compression).ToString() + " is outside the unsigned 16-bit range of the TIFF Compression tag.";
    }
}
