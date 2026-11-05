using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace png;

internal class PngInflationResult
{
    internal bool IsValid;
    internal byte[] Data = new byte[0];
}

internal class PngTextLimitState
{
    internal long Bytes;
}

internal class PngDatastreamSemanticValidator
{
    internal static PngValidationResult Validate(PngChunk[] chunks, byte[] trailingBytes, PngDecoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(trailingBytes);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        PngValidationResult result = new PngValidationResult();
        List<PngError> errors = new List<PngError>();
        PngIhdr? ihdr = ReadIhdr(chunks, result, errors, options.Limits);
        PngChunk? paletteChunk = FindFirst(chunks, PngConstants.ChunkPLTE);
        int paletteEntries = ValidatePalette(errors, paletteChunk, ihdr);
        PngTextLimitState textLimits = new PngTextLimitState();
        ValidateChunks(errors, chunks, ihdr, paletteEntries, options, textLimits);
        ValidateColorMetadata(errors, chunks);
        ValidateAnimation(errors, chunks, ihdr, result.Info, options.Limits);
        ValidateImageData(errors, chunks, ihdr, paletteEntries, options.Limits);
        if (!options.IgnoreTrailingBytes && trailingBytes.Length > 0)
        {
            AddError(errors, "PNG_TRAILING_DATA", "PNG datastream contains bytes after IEND.", PngConstants.ChunkIEND, chunks.Length == 0 ? 0 : chunks[chunks.Length - 1].Offset + 12 + chunks[chunks.Length - 1].Length);
        }

        result.Errors = errors.ToArray();
        result.IsValid = errors.Count == 0;
        return result;
    }

    internal static PngIhdr? ReadIhdr(PngChunk[] chunks, PngValidationResult result, List<PngError> errors, PngSecurityLimits limits)
    {
        PngChunk? chunk = FindFirst(chunks, PngConstants.ChunkIHDR);
        if (chunk == null || chunk.Data.Length != PngIhdrParser.DataLength)
        {
            return null;
        }

        PngIhdr ihdr = PngIhdrParser.Parse(chunk.Data);
        PngValidationResult validation = PngIhdrValidator.Validate(ihdr, limits);
        AddErrors(errors, validation.Errors);
        if (validation.IsValid)
        {
            PngSecurityLimitEnforcer.EnforceIhdr(ihdr, limits);
            PngSecurityLimitEnforcer.EnforceAnimationChunks(chunks, ihdr, limits);
        }

        result.Info = validation.Info;
        return ihdr;
    }

    internal static int ValidatePalette(List<PngError> errors, PngChunk? paletteChunk, PngIhdr? ihdr)
    {
        if (ihdr == null)
        {
            if (paletteChunk != null)
            {
                AddErrors(errors, PngPlteValidator.Validate(paletteChunk).Errors);
            }

            return 0;
        }

        AddErrors(errors, PngPlteValidator.ValidatePresence(paletteChunk != null, ihdr).Errors);
        if (paletteChunk == null)
        {
            return 0;
        }

        AddErrors(errors, PngPlteValidator.Validate(paletteChunk, ihdr).Errors);
        if (paletteChunk.Data.Length == 0 || paletteChunk.Data.Length > PngPlteValidator.MaximumPayloadLength || paletteChunk.Data.Length % PngPlteParser.BytesPerEntry != 0)
        {
            return 0;
        }

        return paletteChunk.Data.Length / PngPlteParser.BytesPerEntry;
    }

    internal static void ValidateChunks(List<PngError> errors, PngChunk[] chunks, PngIhdr? ihdr, int paletteEntries, PngDecoderOptions options, PngTextLimitState textLimits)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            if (!chunk.IsKnown && chunk.IsCritical)
            {
                AddError(errors, "PNG_UNKNOWN_CRITICAL_CHUNK", "PNG datastream contains an unknown critical chunk.", chunk.Type, chunk.Offset);
            }

            if (chunk.Type == PngConstants.ChunkIEND)
            {
                ValidateExactLength(errors, chunk, 0, "PNG_IEND_LENGTH_INVALID", "IEND payload must be empty.");
            }
            else if (chunk.Type == PngConstants.ChunkTransparency)
            {
                ValidateTransparency(errors, chunk, ihdr, paletteEntries);
            }
            else if (chunk.Type == PngConstants.ChunkChromaticities)
            {
                ValidateExactLength(errors, chunk, PngChromaticitiesParser.DataLength, "PNG_CHRM_LENGTH_INVALID", "cHRM payload must contain exactly 32 bytes.");
            }
            else if (chunk.Type == PngConstants.ChunkGamma)
            {
                ValidateGamma(errors, chunk);
            }
            else if (chunk.Type == PngConstants.ChunkIccProfile)
            {
                ValidateIccProfile(errors, chunk, options.Limits);
            }
            else if (chunk.Type == PngConstants.ChunkSignificantBits)
            {
                ValidateSignificantBits(errors, chunk, ihdr);
            }
            else if (chunk.Type == PngConstants.ChunkStandardRgb)
            {
                ValidateStandardRgb(errors, chunk);
            }
            else if (chunk.Type == PngConstants.ChunkCodingIndependentCodePoints)
            {
                ValidateCicp(errors, chunk);
            }
            else if (chunk.Type == PngConstants.ChunkMasteringDisplayColorVolume)
            {
                ValidateExactLength(errors, chunk, PngMasteringDisplayColorVolumeParser.DataLength, "PNG_MDCV_LENGTH_INVALID", "mDCV payload must contain exactly 24 bytes.");
            }
            else if (chunk.Type == PngConstants.ChunkContentLightLevelInformation)
            {
                ValidateExactLength(errors, chunk, PngContentLightLevelInformationParser.DataLength, "PNG_CLLI_LENGTH_INVALID", "cLLI payload must contain exactly 8 bytes.");
            }
            else if (chunk.Type == PngConstants.ChunkText)
            {
                ValidateText(errors, chunk, options.Limits, textLimits);
            }
            else if (chunk.Type == PngConstants.ChunkCompressedText)
            {
                ValidateCompressedText(errors, chunk, options.Limits, textLimits);
            }
            else if (chunk.Type == PngConstants.ChunkInternationalText)
            {
                ValidateInternationalText(errors, chunk, options.Limits, textLimits);
            }
            else if (chunk.Type == PngConstants.ChunkBackgroundColor)
            {
                ValidateBackground(errors, chunk, ihdr, paletteEntries);
            }
            else if (chunk.Type == PngConstants.ChunkPaletteHistogram)
            {
                ValidateHistogram(errors, chunk, paletteEntries);
            }
            else if (chunk.Type == PngConstants.ChunkPhysicalPixels)
            {
                ValidatePhysicalPixels(errors, chunk);
            }
            else if (chunk.Type == PngConstants.ChunkSuggestedPalette)
            {
                ValidateSuggestedPalette(errors, chunk);
            }
            else if (chunk.Type == PngConstants.ChunkExif)
            {
                ValidateExif(errors, chunk);
            }
            else if (chunk.Type == PngConstants.ChunkTime)
            {
                ValidateTime(errors, chunk);
            }

            index += 1;
        }
    }

    internal static void ValidateTransparency(List<PngError> errors, PngChunk chunk, PngIhdr? ihdr, int paletteEntries)
    {
        if (ihdr == null)
        {
            return;
        }

        if (ihdr.ColorType == PngColorType.IndexedColor)
        {
            if (chunk.Data.Length < 1 || chunk.Data.Length > paletteEntries)
            {
                AddError(errors, "PNG_TRNS_INDEXED_ALPHA_LENGTH_INVALID", "Indexed tRNS payload must contain one alpha value per existing palette entry at most.", chunk.Type, chunk.Offset);
            }
        }
        else if (ihdr.ColorType == PngColorType.Grayscale)
        {
            AddErrors(errors, PngTransparencyValidator.ValidateGrayscale(chunk, ihdr).Errors);
        }
        else if (ihdr.ColorType == PngColorType.Truecolor)
        {
            AddErrors(errors, PngTransparencyValidator.ValidateTruecolor(chunk, ihdr).Errors);
        }
        else
        {
            AddError(errors, "PNG_TRNS_COLOR_TYPE_INVALID", "tRNS is not permitted for color types that already contain alpha.", chunk.Type, chunk.Offset);
        }
    }

    internal static void ValidateGamma(List<PngError> errors, PngChunk chunk)
    {
        if (!ValidateExactLength(errors, chunk, PngGammaParser.DataLength, "PNG_GAMA_LENGTH_INVALID", "gAMA payload must contain exactly 4 bytes."))
        {
            return;
        }

        if (PngBinaryReader.ReadUInt32(chunk.Data, 0) == 0)
        {
            AddError(errors, "PNG_GAMA_VALUE_INVALID", "gAMA value must be greater than zero.", chunk.Type, chunk.Offset);
        }
    }

    internal static void ValidateIccProfile(List<PngError> errors, PngChunk chunk, PngSecurityLimits limits)
    {
        int separator = FindNull(chunk.Data, 0);
        if (!ValidateKeyword(errors, chunk, separator, "PNG_ICCP_NAME_INVALID"))
        {
            return;
        }

        int methodOffset = separator + 1;
        if (methodOffset >= chunk.Data.Length || chunk.Data[methodOffset] != PngIccProfileParser.CompressionMethodDeflate)
        {
            AddError(errors, "PNG_ICCP_COMPRESSION_METHOD_INVALID", "iCCP must use compression method 0.", chunk.Type, chunk.Offset);
            return;
        }

        int compressedOffset = methodOffset + 1;
        if (compressedOffset >= chunk.Data.Length || !Inflate(chunk.Data, compressedOffset, chunk.Data.Length - compressedOffset, limits).IsValid)
        {
            AddError(errors, "PNG_ICCP_COMPRESSED_PROFILE_INVALID", "iCCP compressed profile is empty or is not a valid zlib stream.", chunk.Type, chunk.Offset);
        }
    }

    internal static void ValidateSignificantBits(List<PngError> errors, PngChunk chunk, PngIhdr? ihdr)
    {
        if (ihdr == null)
        {
            return;
        }

        int requiredLength = GetColorChannelCount(ihdr.ColorType);
        if (!ValidateExactLength(errors, chunk, requiredLength, "PNG_SBIT_LENGTH_INVALID", "sBIT payload length does not match the IHDR color type."))
        {
            return;
        }

        int maximum = (int)ihdr.BitDepth;
        if (ihdr.ColorType == PngColorType.IndexedColor)
        {
            maximum = 8;
        }

        int index = 0;
        while (index < chunk.Data.Length)
        {
            if (chunk.Data[index] == 0 || chunk.Data[index] > maximum)
            {
                AddError(errors, "PNG_SBIT_VALUE_INVALID", "sBIT values must be positive and must not exceed the source sample precision.", chunk.Type, chunk.Offset);
                return;
            }

            index += 1;
        }
    }

    internal static int GetColorChannelCount(PngColorType colorType)
    {
        switch (colorType)
        {
            case PngColorType.Grayscale:
                return 1;
            case PngColorType.IndexedColor:
            case PngColorType.Truecolor:
                return 3;
            case PngColorType.GrayscaleAlpha:
                return 2;
            case PngColorType.TruecolorAlpha:
                return 4;
            default:
                return 0;
        }
    }

    internal static void ValidateStandardRgb(List<PngError> errors, PngChunk chunk)
    {
        if (ValidateExactLength(errors, chunk, PngStandardRgbParser.DataLength, "PNG_SRGB_LENGTH_INVALID", "sRGB payload must contain exactly one byte.")
            && chunk.Data[0] > PngStandardRgbParser.MaximumRenderingIntent)
        {
            AddError(errors, "PNG_SRGB_RENDERING_INTENT_INVALID", "sRGB rendering intent must be 0 through 3.", chunk.Type, chunk.Offset);
        }
    }

    internal static void ValidateCicp(List<PngError> errors, PngChunk chunk)
    {
        if (ValidateExactLength(errors, chunk, PngCodingIndependentCodePointsParser.DataLength, "PNG_CICP_LENGTH_INVALID", "cICP payload must contain exactly four bytes.")
            && chunk.Data[3] > PngCodingIndependentCodePointsParser.MaximumVideoFullRangeFlag)
        {
            AddError(errors, "PNG_CICP_RANGE_FLAG_INVALID", "cICP video full range flag must be 0 or 1.", chunk.Type, chunk.Offset);
        }
    }

    internal static void ValidateText(List<PngError> errors, PngChunk chunk, PngSecurityLimits limits, PngTextLimitState textLimits)
    {
        int separator = FindNull(chunk.Data, 0);
        if (!ValidateKeyword(errors, chunk, separator, "PNG_TEXT_KEYWORD_INVALID"))
        {
            return;
        }

        if (FindNull(chunk.Data, separator + 1) >= 0)
        {
            AddError(errors, "PNG_TEXT_NULL_CHARACTER_INVALID", "tEXt text must not contain a null character.", chunk.Type, chunk.Offset);
            return;
        }

        textLimits.Bytes = PngSecurityLimitEnforcer.AddBytes(textLimits.Bytes, chunk.Data.Length - separator - 1, limits.MaxTextBytes);
    }

    internal static void ValidateCompressedText(List<PngError> errors, PngChunk chunk, PngSecurityLimits limits, PngTextLimitState textLimits)
    {
        int separator = FindNull(chunk.Data, 0);
        if (!ValidateKeyword(errors, chunk, separator, "PNG_ZTXT_KEYWORD_INVALID"))
        {
            return;
        }

        int methodOffset = separator + 1;
        if (methodOffset >= chunk.Data.Length || chunk.Data[methodOffset] != PngCompressedTextParser.CompressionMethodDeflate)
        {
            AddError(errors, "PNG_ZTXT_COMPRESSION_METHOD_INVALID", "zTXt must use compression method 0.", chunk.Type, chunk.Offset);
            return;
        }

        PngInflationResult inflated = Inflate(chunk.Data, methodOffset + 1, chunk.Data.Length - methodOffset - 1, limits);
        if (!inflated.IsValid)
        {
            AddError(errors, "PNG_ZTXT_COMPRESSED_TEXT_INVALID", "zTXt text must be a valid zlib stream.", chunk.Type, chunk.Offset);
        }
        else if (FindNull(inflated.Data, 0) >= 0)
        {
            AddError(errors, "PNG_ZTXT_NULL_CHARACTER_INVALID", "zTXt text must not contain a null character.", chunk.Type, chunk.Offset);
        }
        else
        {
            textLimits.Bytes = PngSecurityLimitEnforcer.AddBytes(textLimits.Bytes, inflated.Data.Length, limits.MaxTextBytes);
        }
    }

    internal static void ValidateInternationalText(List<PngError> errors, PngChunk chunk, PngSecurityLimits limits, PngTextLimitState textLimits)
    {
        int keywordEnd = FindNull(chunk.Data, 0);
        if (!ValidateKeyword(errors, chunk, keywordEnd, "PNG_ITXT_KEYWORD_INVALID"))
        {
            return;
        }

        int flagOffset = keywordEnd + 1;
        int methodOffset = flagOffset + 1;
        if (methodOffset >= chunk.Data.Length)
        {
            AddError(errors, "PNG_ITXT_FIELDS_MISSING", "iTXt must contain compression, language, translated keyword, and text fields.", chunk.Type, chunk.Offset);
            return;
        }

        byte flag = chunk.Data[flagOffset];
        byte method = chunk.Data[methodOffset];
        if (flag > 1)
        {
            AddError(errors, "PNG_ITXT_COMPRESSION_FLAG_INVALID", "iTXt compression flag must be 0 or 1.", chunk.Type, chunk.Offset);
            return;
        }

        if (flag == 1 && method != 0)
        {
            AddError(errors, "PNG_ITXT_COMPRESSION_METHOD_INVALID", "Compressed iTXt must use compression method 0.", chunk.Type, chunk.Offset);
            return;
        }

        int languageOffset = methodOffset + 1;
        int languageEnd = FindNull(chunk.Data, languageOffset);
        if (languageEnd < 0 || !IsAscii(chunk.Data, languageOffset, languageEnd - languageOffset))
        {
            AddError(errors, "PNG_ITXT_LANGUAGE_TAG_INVALID", "iTXt language tag must be null-terminated ASCII.", chunk.Type, chunk.Offset);
            return;
        }

        int translatedOffset = languageEnd + 1;
        int translatedEnd = FindNull(chunk.Data, translatedOffset);
        if (translatedEnd < 0 || !IsUtf8(chunk.Data, translatedOffset, translatedEnd - translatedOffset))
        {
            AddError(errors, "PNG_ITXT_TRANSLATED_KEYWORD_INVALID", "iTXt translated keyword must be null-terminated UTF-8.", chunk.Type, chunk.Offset);
            return;
        }

        int textOffset = translatedEnd + 1;
        PngInflationResult text = CreateUncompressedResult(chunk.Data, textOffset, chunk.Data.Length - textOffset);
        if (flag == 1)
        {
            text = Inflate(chunk.Data, textOffset, chunk.Data.Length - textOffset, limits);
        }

        if (!text.IsValid || !IsUtf8(text.Data, 0, text.Data.Length) || FindNull(text.Data, 0) >= 0)
        {
            AddError(errors, "PNG_ITXT_TEXT_ENCODING_INVALID", "iTXt text must be valid null-free UTF-8, and compressed text must use a valid zlib stream.", chunk.Type, chunk.Offset);
        }
        else
        {
            textLimits.Bytes = PngSecurityLimitEnforcer.AddBytes(textLimits.Bytes, text.Data.Length, limits.MaxTextBytes);
        }
    }

    internal static void ValidateBackground(List<PngError> errors, PngChunk chunk, PngIhdr? ihdr, int paletteEntries)
    {
        if (ihdr == null)
        {
            return;
        }

        int length = PngBackgroundColorParser.TruecolorDataLength;
        if (ihdr.ColorType == PngColorType.IndexedColor)
        {
            length = PngBackgroundColorParser.IndexedDataLength;
        }
        else if (ihdr.ColorType == PngColorType.Grayscale || ihdr.ColorType == PngColorType.GrayscaleAlpha)
        {
            length = PngBackgroundColorParser.GrayscaleDataLength;
        }

        if (!ValidateExactLength(errors, chunk, length, "PNG_BKGD_LENGTH_INVALID", "bKGD payload length does not match the IHDR color type."))
        {
            return;
        }

        if (ihdr.ColorType == PngColorType.IndexedColor)
        {
            if (chunk.Data[0] >= paletteEntries)
            {
                AddError(errors, "PNG_BKGD_PALETTE_INDEX_INVALID", "bKGD palette index must refer to an existing PLTE entry.", chunk.Type, chunk.Offset);
            }

            return;
        }

        if (ihdr.ColorType == PngColorType.Grayscale || ihdr.ColorType == PngColorType.GrayscaleAlpha)
        {
            if (PngBinaryReader.ReadUInt16(chunk.Data, 0) > GetMaximumSample(ihdr.BitDepth))
            {
                AddError(errors, "PNG_BKGD_SAMPLE_OUT_OF_RANGE", "bKGD grayscale sample must fit the IHDR bit depth.", chunk.Type, chunk.Offset);
            }
        }
        else if (ihdr.BitDepth != PngBitDepth.Sixteen)
        {
            int maximum = GetMaximumSample(ihdr.BitDepth);
            if (PngBinaryReader.ReadUInt16(chunk.Data, 0) > maximum
                || PngBinaryReader.ReadUInt16(chunk.Data, 2) > maximum
                || PngBinaryReader.ReadUInt16(chunk.Data, 4) > maximum)
            {
                AddError(errors, "PNG_BKGD_SAMPLE_OUT_OF_RANGE", "bKGD color samples must fit the IHDR bit depth.", chunk.Type, chunk.Offset);
            }
        }
    }

    internal static int GetMaximumSample(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.One:
                return 1;
            case PngBitDepth.Two:
                return 3;
            case PngBitDepth.Four:
                return 15;
            case PngBitDepth.Eight:
                return 255;
            default:
                return 65535;
        }
    }

    internal static void ValidateHistogram(List<PngError> errors, PngChunk chunk, int paletteEntries)
    {
        if (paletteEntries == 0 || chunk.Data.Length != paletteEntries * PngPaletteHistogramParser.BytesPerEntry)
        {
            AddError(errors, "PNG_HIST_LENGTH_INVALID", "hIST must contain exactly one frequency for every PLTE entry.", chunk.Type, chunk.Offset);
        }
    }

    internal static void ValidatePhysicalPixels(List<PngError> errors, PngChunk chunk)
    {
        if (ValidateExactLength(errors, chunk, PngPhysicalPixelsParser.DataLength, "PNG_PHYS_LENGTH_INVALID", "pHYs payload must contain exactly nine bytes.")
            && chunk.Data[8] > PngPhysicalPixelsParser.UnitMetre)
        {
            AddError(errors, "PNG_PHYS_UNIT_INVALID", "pHYs unit specifier must be 0 or 1.", chunk.Type, chunk.Offset);
        }
    }

    internal static void ValidateSuggestedPalette(List<PngError> errors, PngChunk chunk)
    {
        int separator = FindNull(chunk.Data, 0);
        if (!ValidateKeyword(errors, chunk, separator, "PNG_SPLT_NAME_INVALID"))
        {
            return;
        }

        int depthOffset = separator + 1;
        if (depthOffset >= chunk.Data.Length || (chunk.Data[depthOffset] != PngSuggestedPaletteParser.SampleDepth8 && chunk.Data[depthOffset] != PngSuggestedPaletteParser.SampleDepth16))
        {
            AddError(errors, "PNG_SPLT_SAMPLE_DEPTH_INVALID", "sPLT sample depth must be 8 or 16.", chunk.Type, chunk.Offset);
            return;
        }

        int bytesPerEntry = PngSuggestedPaletteParser.BytesPer8BitEntry;
        if (chunk.Data[depthOffset] == PngSuggestedPaletteParser.SampleDepth16)
        {
            bytesPerEntry = PngSuggestedPaletteParser.BytesPer16BitEntry;
        }

        int entryBytes = chunk.Data.Length - depthOffset - 1;
        if (entryBytes == 0 || entryBytes % bytesPerEntry != 0)
        {
            AddError(errors, "PNG_SPLT_ENTRY_LENGTH_INVALID", "sPLT must contain one or more complete entries for its sample depth.", chunk.Type, chunk.Offset);
        }
    }

    internal static void ValidateExif(List<PngError> errors, PngChunk chunk)
    {
        if (chunk.Data.Length < PngExifProfileParser.MinimumDataLength)
        {
            AddError(errors, "PNG_EXIF_HEADER_INVALID", "eXIf payload must begin with a TIFF byte-order marker and fixed 42 value.", chunk.Type, chunk.Offset);
            return;
        }

        bool littleEndian = chunk.Data[0] == 73 && chunk.Data[1] == 73 && chunk.Data[2] == 42 && chunk.Data[3] == 0;
        bool bigEndian = chunk.Data[0] == 77 && chunk.Data[1] == 77 && chunk.Data[2] == 0 && chunk.Data[3] == 42;
        if (!littleEndian && !bigEndian)
        {
            AddError(errors, "PNG_EXIF_HEADER_INVALID", "eXIf payload must begin with a valid TIFF header.", chunk.Type, chunk.Offset);
        }
    }

    internal static void ValidateTime(List<PngError> errors, PngChunk chunk)
    {
        if (!ValidateExactLength(errors, chunk, PngTimeParser.DataLength, "PNG_TIME_LENGTH_INVALID", "tIME payload must contain exactly seven bytes."))
        {
            return;
        }

        int month = chunk.Data[2];
        int day = chunk.Data[3];
        int hour = chunk.Data[4];
        int minute = chunk.Data[5];
        int second = chunk.Data[6];
        if (month < 1 || month > 12 || day < 1 || day > 31 || hour > 23 || minute > 59 || second > 60)
        {
            AddError(errors, "PNG_TIME_VALUE_INVALID", "tIME fields contain a reserved or out-of-range value.", chunk.Type, chunk.Offset);
        }
    }

    internal static void ValidateColorMetadata(List<PngError> errors, PngChunk[] chunks)
    {
        PngChunk? iccp = FindFirst(chunks, PngConstants.ChunkIccProfile);
        PngChunk? srgb = FindFirst(chunks, PngConstants.ChunkStandardRgb);
        if (iccp != null && srgb != null)
        {
            AddError(errors, "PNG_COLOR_METADATA_CONFLICT", "iCCP and sRGB are mutually exclusive color-space declarations.", srgb.Type, srgb.Offset);
        }
    }

    internal static void ValidateAnimation(List<PngError> errors, PngChunk[] chunks, PngIhdr? ihdr, PngImageInfo info, PngSecurityLimits limits)
    {
        PngChunk? animationControl = FindFirst(chunks, PngConstants.ChunkAnimationControl);
        int frameControlCount = CountChunks(chunks, PngConstants.ChunkFrameControl);
        int frameDataCount = CountChunks(chunks, PngConstants.ChunkFrameData);
        if (animationControl == null)
        {
            if (frameControlCount > 0 || frameDataCount > 0)
            {
                AddError(errors, "PNG_APNG_CONTROL_MISSING", "APNG frame chunks require an acTL chunk.", frameControlCount > 0 ? PngConstants.ChunkFrameControl : PngConstants.ChunkFrameData, 0);
            }

            return;
        }

        if (animationControl.Data.Length != PngAnimationControlParser.DataLength)
        {
            AddError(errors, "PNG_ACTL_LENGTH_INVALID", "acTL payload must contain exactly eight bytes.", animationControl.Type, animationControl.Offset);
            return;
        }

        uint declaredFrames = PngBinaryReader.ReadUInt32(animationControl.Data, 0);
        if (declaredFrames == 0)
        {
            AddError(errors, "PNG_ACTL_FRAME_COUNT_INVALID", "acTL frame count must be greater than zero.", animationControl.Type, animationControl.Offset);
        }
        else if (declaredFrames != frameControlCount)
        {
            AddError(errors, "PNG_APNG_FRAME_COUNT_MISMATCH", "acTL frame count must match the number of fcTL chunks.", animationControl.Type, animationControl.Offset);
        }

        if (declaredFrames > 0)
        {
            PngApngAnimationLimitEnforcer.EnforceFrameCount(declaredFrames, limits);
        }

        if (declaredFrames <= int.MaxValue)
        {
            info.IsAnimated = true;
            info.FrameCount = (int)declaredFrames;
        }

        uint playCount = PngBinaryReader.ReadUInt32(animationControl.Data, 4);
        if (playCount <= int.MaxValue)
        {
            info.PlayCount = (int)playCount;
        }

        int firstIdat = FindFirstIndex(chunks, PngConstants.ChunkIDAT);
        int lastFrameControl = -1;
        bool frameNeedsData = false;
        bool frameHasData = false;
        int controlsBeforeIdat = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            if (chunk.Type == PngConstants.ChunkFrameControl)
            {
                if (frameNeedsData && !frameHasData)
                {
                    AddError(errors, "PNG_APNG_FRAME_DATA_MISSING", "Every non-default APNG frame must contain fdAT image data.", PngConstants.ChunkFrameData, chunk.Offset);
                }

                ValidateFrameControl(errors, chunk, ihdr, index < firstIdat);
                if (index < firstIdat)
                {
                    controlsBeforeIdat += 1;
                    if (controlsBeforeIdat > 1)
                    {
                        AddError(errors, "PNG_APNG_DEFAULT_FRAME_CONTROL_DUPLICATE", "Only one fcTL chunk may appear before IDAT.", chunk.Type, chunk.Offset);
                    }
                }

                lastFrameControl = index;
                frameNeedsData = index > firstIdat;
                frameHasData = false;
            }
            else if (chunk.Type == PngConstants.ChunkFrameData)
            {
                if (chunk.Data.Length < PngFrameDataParser.SequenceNumberLength)
                {
                    AddError(errors, "PNG_FDAT_LENGTH_INVALID", "fdAT payload must contain a sequence number.", chunk.Type, chunk.Offset);
                }

                if (lastFrameControl < 0)
                {
                    AddError(errors, "PNG_APNG_FRAME_CONTROL_MISSING", "fdAT image data must follow an fcTL chunk.", chunk.Type, chunk.Offset);
                }

                if (index < firstIdat)
                {
                    AddError(errors, "PNG_APNG_FRAME_DATA_BEFORE_IDAT", "fdAT chunks must appear after the default IDAT image.", chunk.Type, chunk.Offset);
                }

                frameHasData = true;
            }

            index += 1;
        }

        if (frameNeedsData && !frameHasData)
        {
            AddError(errors, "PNG_APNG_FRAME_DATA_MISSING", "Every non-default APNG frame must contain fdAT image data.", PngConstants.ChunkFrameData, 0);
        }

        ValidateFrameImageData(errors, chunks, ihdr, firstIdat, limits);
    }

    internal static void ValidateFrameImageData(List<PngError> errors, PngChunk[] chunks, PngIhdr? canvas, int firstIdat, PngSecurityLimits limits)
    {
        if (canvas == null)
        {
            return;
        }

        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk control = chunks[index];
            if (control.Type == PngConstants.ChunkFrameControl && index > firstIdat && control.Data.Length == PngFrameControlParser.DataLength)
            {
                int end = index + 1;
                while (end < chunks.Length && chunks[end].Type == PngConstants.ChunkFrameData)
                {
                    end += 1;
                }

                byte[] compressed = ConcatenateFrameData(chunks, index + 1, end);
                if (compressed.Length > 0)
                {
                    PngInflationResult inflation = Inflate(compressed, 0, compressed.Length, limits);
                    if (!inflation.IsValid)
                    {
                        AddError(errors, "PNG_FDAT_ZLIB_INVALID", "APNG frame data is not a valid zlib stream.", PngConstants.ChunkFrameData, control.Offset);
                    }
                    else
                    {
                        PngIhdr frame = CreateFrameIhdr(control, canvas);
                        if (frame.Width > 0 && frame.Height > 0)
                        {
                            ValidateFrameScanlines(errors, inflation.Data, frame, control.Offset);
                        }
                    }
                }

                index = end;
                continue;
            }

            index += 1;
        }
    }

    internal static byte[] ConcatenateFrameData(PngChunk[] chunks, int start, int end)
    {
        int length = 0;
        int index = start;
        while (index < end)
        {
            if (chunks[index].Data.Length >= PngFrameDataParser.SequenceNumberLength)
            {
                length += chunks[index].Data.Length - PngFrameDataParser.SequenceNumberLength;
            }

            index += 1;
        }

        byte[] result = new byte[length];
        int destination = 0;
        index = start;
        while (index < end)
        {
            int source = PngFrameDataParser.SequenceNumberLength;
            while (source < chunks[index].Data.Length)
            {
                result[destination] = chunks[index].Data[source];
                destination += 1;
                source += 1;
            }

            index += 1;
        }

        return result;
    }

    internal static PngIhdr CreateFrameIhdr(PngChunk control, PngIhdr canvas)
    {
        PngIhdr frame = new PngIhdr();
        uint width = PngBinaryReader.ReadUInt32(control.Data, 4);
        uint height = PngBinaryReader.ReadUInt32(control.Data, 8);
        if (width <= int.MaxValue)
        {
            frame.Width = (int)width;
        }

        if (height <= int.MaxValue)
        {
            frame.Height = (int)height;
        }

        frame.BitDepth = canvas.BitDepth;
        frame.ColorType = canvas.ColorType;
        frame.CompressionMethod = canvas.CompressionMethod;
        frame.FilterMethod = canvas.FilterMethod;
        frame.InterlaceMethod = canvas.InterlaceMethod;
        return frame;
    }

    internal static void ValidateFrameScanlines(List<PngError> errors, byte[] scanlines, PngIhdr frame, long offset)
    {
        List<PngError> frameErrors = new List<PngError>();
        if (!ValidateScanlines(frameErrors, scanlines, frame))
        {
            int index = 0;
            while (index < frameErrors.Count)
            {
                PngError source = frameErrors[index];
                AddError(errors, "PNG_APNG_FRAME_" + source.Code, source.Message, PngConstants.ChunkFrameData, offset);
                index += 1;
            }
        }
    }

    internal static void ValidateFrameControl(List<PngError> errors, PngChunk chunk, PngIhdr? ihdr, bool isDefaultFrame)
    {
        if (chunk.Data.Length != PngFrameControlParser.DataLength)
        {
            AddError(errors, "PNG_FCTL_LENGTH_INVALID", "fcTL payload must contain exactly 26 bytes.", chunk.Type, chunk.Offset);
            return;
        }

        uint width = PngBinaryReader.ReadUInt32(chunk.Data, 4);
        uint height = PngBinaryReader.ReadUInt32(chunk.Data, 8);
        uint x = PngBinaryReader.ReadUInt32(chunk.Data, 12);
        uint y = PngBinaryReader.ReadUInt32(chunk.Data, 16);
        if (width == 0 || height == 0)
        {
            AddError(errors, "PNG_APNG_FRAME_DIMENSION_INVALID", "APNG frame dimensions must be greater than zero.", chunk.Type, chunk.Offset);
        }

        if (chunk.Data[24] > PngConstants.ApngDisposeOperationPrevious || chunk.Data[25] > PngConstants.ApngBlendOperationOver)
        {
            AddError(errors, "PNG_APNG_FRAME_OPERATION_INVALID", "APNG dispose and blend operations contain a reserved value.", chunk.Type, chunk.Offset);
        }

        if (ihdr != null && (x > (uint)ihdr.Width || y > (uint)ihdr.Height || width > (uint)ihdr.Width - x || height > (uint)ihdr.Height - y))
        {
            AddError(errors, "PNG_APNG_FRAME_RECTANGLE_OUT_OF_BOUNDS", "APNG frame rectangle must fit within the IHDR canvas.", chunk.Type, chunk.Offset);
        }

        if (ihdr != null && isDefaultFrame && (x != 0 || y != 0 || width != (uint)ihdr.Width || height != (uint)ihdr.Height))
        {
            AddError(errors, "PNG_APNG_DEFAULT_IMAGE_RECTANGLE_INVALID", "The default-image frame must cover the full IHDR canvas.", chunk.Type, chunk.Offset);
        }
    }

    internal static void ValidateImageData(List<PngError> errors, PngChunk[] chunks, PngIhdr? ihdr, int paletteEntries, PngSecurityLimits limits)
    {
        if (ihdr == null || !PngIhdrValidator.Validate(ihdr, limits).IsValid)
        {
            return;
        }

        byte[] compressed = Concatenate(chunks, PngConstants.ChunkIDAT, 0);
        if (compressed.Length == 0)
        {
            if (CountChunks(chunks, PngConstants.ChunkIDAT) > 0)
            {
                AddError(errors, "PNG_IDAT_ZLIB_INVALID", "Concatenated IDAT payload is not a valid zlib stream.", PngConstants.ChunkIDAT, 0);
            }

            return;
        }

        PngInflationResult inflation = Inflate(compressed, 0, compressed.Length, limits);
        if (!inflation.IsValid)
        {
            AddError(errors, "PNG_IDAT_ZLIB_INVALID", "Concatenated IDAT payload is not a valid zlib stream.", PngConstants.ChunkIDAT, 0);
            return;
        }

        if (!ValidateScanlines(errors, inflation.Data, ihdr))
        {
            return;
        }

        if (ihdr.ColorType == PngColorType.IndexedColor && paletteEntries > 0)
        {
            byte[] imageData = DecodeImageData(inflation.Data, ihdr);
            ValidatePaletteIndices(errors, imageData, ihdr, paletteEntries);
        }
    }

    internal static bool ValidateScanlines(List<PngError> errors, byte[] scanlines, PngIhdr ihdr)
    {
        if (ihdr.InterlaceMethod == PngInterlaceMethod.None)
        {
            PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(ihdr);
            if (scanlines.Length != layout.FilteredImageBytes)
            {
                AddError(errors, "PNG_SCANLINE_DECOMPRESSED_BYTE_COUNT_MISMATCH", "Decompressed scanline byte count does not match IHDR.", PngConstants.ChunkIDAT, 0);
                return false;
            }

            int row = 0;
            while (row < ihdr.Height)
            {
                int offset = (int)(row * layout.FilteredRowBytes);
                if (!PngRowFilterValidator.IsValidFilterType(scanlines[offset]))
                {
                    AddError(errors, "PNG_SCANLINE_FILTER_TYPE_INVALID", "PNG scanline filter byte must be 0 through 4.", PngConstants.ChunkIDAT, offset);
                    return false;
                }

                row += 1;
            }

            return true;
        }

        long expected = PngAdam7PassGeometryCalculator.CalculateFilteredImageBytes(ihdr);
        if (scanlines.Length != expected)
        {
            AddError(errors, "PNG_SCANLINE_DECOMPRESSED_BYTE_COUNT_MISMATCH", "Decompressed Adam7 scanline byte count does not match IHDR.", PngConstants.ChunkIDAT, 0);
            return false;
        }

        PngAdam7PassGeometry[] passes = PngAdam7PassGeometryCalculator.CalculatePasses(ihdr);
        int scanlineOffset = 0;
        int passIndex = 0;
        while (passIndex < passes.Length)
        {
            PngAdam7PassGeometry pass = passes[passIndex];
            int row = 0;
            while (row < pass.Height)
            {
                if (!PngRowFilterValidator.IsValidFilterType(scanlines[scanlineOffset]))
                {
                    AddError(errors, "PNG_SCANLINE_FILTER_TYPE_INVALID", "PNG Adam7 scanline filter byte must be 0 through 4.", PngConstants.ChunkIDAT, scanlineOffset);
                    return false;
                }

                scanlineOffset += (int)pass.FilteredRowBytes;
                row += 1;
            }

            passIndex += 1;
        }

        return true;
    }

    internal static byte[] DecodeImageData(byte[] scanlines, PngIhdr ihdr)
    {
        if (ihdr.InterlaceMethod == PngInterlaceMethod.Adam7)
        {
            return PngAdam7Decoder.Decode(scanlines, ihdr);
        }

        return PngNonInterlacedImageDecoder.Decode(scanlines, ihdr).Data;
    }

    internal static void ValidatePaletteIndices(List<PngError> errors, byte[] imageData, PngIhdr ihdr, int paletteEntries)
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(ihdr);
        int bits = (int)ihdr.BitDepth;
        int mask = (1 << bits) - 1;
        int y = 0;
        while (y < ihdr.Height)
        {
            int rowOffset = (int)(y * layout.RowDataBytes);
            int x = 0;
            while (x < ihdr.Width)
            {
                int bitOffset = x * bits;
                int index = (imageData[rowOffset + bitOffset / 8] >> (8 - bits - bitOffset % 8)) & mask;
                if (index >= paletteEntries)
                {
                    AddError(errors, "PNG_PALETTE_INDEX_OUT_OF_RANGE", "An indexed-color sample does not refer to an existing PLTE entry.", PngConstants.ChunkIDAT, y);
                    return;
                }

                x += 1;
            }

            y += 1;
        }
    }

    internal static byte[] Concatenate(PngChunk[] chunks, string chunkType, int skipBytes)
    {
        int length = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == chunkType && chunks[index].Data.Length >= skipBytes)
            {
                if (length > int.MaxValue - chunks[index].Data.Length + skipBytes)
                {
                    throw new OverflowException();
                }

                length += chunks[index].Data.Length - skipBytes;
            }

            index += 1;
        }

        byte[] result = new byte[length];
        int destination = 0;
        index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            if (chunk.Type == chunkType && chunk.Data.Length >= skipBytes)
            {
                int source = skipBytes;
                while (source < chunk.Data.Length)
                {
                    result[destination] = chunk.Data[source];
                    destination += 1;
                    source += 1;
                }
            }

            index += 1;
        }

        return result;
    }

    internal static PngInflationResult Inflate(byte[] data, int offset, int length, PngSecurityLimits limits)
    {
        PngInflationResult result = new PngInflationResult();
        if (length == 0)
        {
            return result;
        }

        Task<byte[]> task = Task.Run(() => PngZlibDecompressor.Decompress(data, offset, length, limits));
        while (!task.IsCompleted)
        {
            Thread.Yield();
        }

        if (task.Status == TaskStatus.RanToCompletion)
        {
            result.Data = task.Result;
            result.IsValid = HasValidZlibEnvelope(data, offset, length, result.Data);
        }

        return result;
    }

    internal static bool HasValidZlibEnvelope(byte[] data, int offset, int length, byte[] decompressed)
    {
        if (length < 6)
        {
            return false;
        }

        int compressionMethodAndInfo = data[offset];
        int flags = data[offset + 1];
        if ((compressionMethodAndInfo & 15) != 8
            || (compressionMethodAndInfo >> 4) > 7
            || ((compressionMethodAndInfo << 8) + flags) % 31 != 0
            || (flags & 32) != 0)
        {
            return false;
        }

        uint expectedAdler = PngBinaryReader.ReadUInt32(data, offset + length - 4);
        return expectedAdler == CalculateAdler32(decompressed);
    }

    internal static uint CalculateAdler32(byte[] data)
    {
        uint first = 1;
        uint second = 0;
        int index = 0;
        while (index < data.Length)
        {
            first = (first + data[index]) % 65521;
            second = (second + first) % 65521;
            index += 1;
        }

        return (second << 16) | first;
    }

    internal static PngInflationResult CreateUncompressedResult(byte[] data, int offset, int length)
    {
        PngInflationResult result = new PngInflationResult();
        result.IsValid = true;
        result.Data = new byte[length];
        int index = 0;
        while (index < length)
        {
            result.Data[index] = data[offset + index];
            index += 1;
        }

        return result;
    }

    internal static bool ValidateKeyword(List<PngError> errors, PngChunk chunk, int separator, string code)
    {
        if (separator < PngTextKeywordValidator.MinimumKeywordLength
            || separator > PngTextKeywordValidator.MaximumKeywordLength
            || !PngTextKeywordValidator.IsValidBytes(chunk.Data, 0, separator))
        {
            AddError(errors, code, "PNG text keyword or profile name must contain 1 through 79 valid Latin-1 bytes.", chunk.Type, chunk.Offset);
            return false;
        }

        return true;
    }

    internal static int FindNull(byte[] data, int offset)
    {
        int index = offset;
        while (index < data.Length)
        {
            if (data[index] == 0)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    internal static bool IsAscii(byte[] data, int offset, int length)
    {
        int index = 0;
        while (index < length)
        {
            if (data[offset + index] > 127)
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool IsUtf8(byte[] data, int offset, int length)
    {
        int end = offset + length;
        int index = offset;
        while (index < end)
        {
            int first = data[index];
            int continuationCount = 0;
            int minimumSecond = 128;
            int maximumSecond = 191;
            if (first <= 127)
            {
                index += 1;
                continue;
            }
            else if (first >= 194 && first <= 223)
            {
                continuationCount = 1;
            }
            else if (first >= 224 && first <= 239)
            {
                continuationCount = 2;
                if (first == 224)
                {
                    minimumSecond = 160;
                }
                else if (first == 237)
                {
                    maximumSecond = 159;
                }
            }
            else if (first >= 240 && first <= 244)
            {
                continuationCount = 3;
                if (first == 240)
                {
                    minimumSecond = 144;
                }
                else if (first == 244)
                {
                    maximumSecond = 143;
                }
            }
            else
            {
                return false;
            }

            if (index + continuationCount >= end || data[index + 1] < minimumSecond || data[index + 1] > maximumSecond)
            {
                return false;
            }

            int continuation = 2;
            while (continuation <= continuationCount)
            {
                int value = data[index + continuation];
                if (value < 128 || value > 191)
                {
                    return false;
                }

                continuation += 1;
            }

            index += continuationCount + 1;
        }

        return true;
    }

    internal static bool ValidateExactLength(List<PngError> errors, PngChunk chunk, int expected, string code, string message)
    {
        if (chunk.Data.Length == expected)
        {
            return true;
        }

        AddError(errors, code, message, chunk.Type, chunk.Offset);
        return false;
    }

    internal static PngChunk? FindFirst(PngChunk[] chunks, string type)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == type)
            {
                return chunks[index];
            }

            index += 1;
        }

        return null;
    }

    internal static int FindFirstIndex(PngChunk[] chunks, string type)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == type)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    internal static int CountChunks(PngChunk[] chunks, string type)
    {
        int count = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == type)
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }

    internal static void AddErrors(List<PngError> errors, PngError[] newErrors)
    {
        int index = 0;
        while (index < newErrors.Length)
        {
            if (!ContainsError(errors, newErrors[index]))
            {
                errors.Add(newErrors[index]);
            }

            index += 1;
        }
    }

    internal static bool ContainsError(List<PngError> errors, PngError finding)
    {
        int index = 0;
        while (index < errors.Count)
        {
            PngError error = errors[index];
            if (error.Code == finding.Code && error.ChunkType == finding.ChunkType && error.Offset == finding.Offset)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static void AddError(List<PngError> errors, string code, string message, string chunkType, long offset)
    {
        PngError error = new PngError();
        error.Code = code;
        error.Message = message;
        error.ChunkType = chunkType;
        error.Offset = offset;
        error.IsFatal = true;
        if (!ContainsError(errors, error))
        {
            errors.Add(error);
        }
    }
}
