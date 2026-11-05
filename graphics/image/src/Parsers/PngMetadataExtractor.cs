using System.Collections.Generic;

namespace image;

internal class PngMetadataExtractor
{
    internal static void Extract(ImageMetadataDocument document, List<PngChunk> chunks, ImageMetadataReaderOptions options)
    {
        if (document == null || chunks == null)
        {
            return;
        }

        int colorType = FindColorType(chunks);
        foreach (PngChunk chunk in chunks)
        {
            if (chunk != null && !chunk.DataLimited && chunk.CrcValid)
            {
                ExtractChunk(document, chunk, options, colorType);
            }
        }

        ExtractAnimation(document, chunks, options);
    }

    internal static void ExtractChunk(ImageMetadataDocument document, PngChunk chunk, ImageMetadataReaderOptions options, int colorType)
    {
        switch (chunk.Type)
        {
            case "eXIf":
                ExtractExif(document, chunk, options);
                return;
            case "tEXt":
                ExtractText(document, chunk);
                return;
            case "zTXt":
                ExtractCompressedText(document, chunk, options);
                return;
            case "iTXt":
                ExtractInternationalText(document, chunk, options);
                return;
            case "pHYs":
                ExtractPhysicalDimensions(document, chunk);
                return;
            case "tIME":
                ExtractTime(document, chunk);
                return;
            case "tRNS":
                ExtractTransparency(document, chunk, colorType);
                return;
            case "bKGD":
                ExtractBackground(document, chunk, colorType);
                return;
            case "fdAT":
                ExtractFrameData(document, chunk);
                return;
            default:
                return;
        }
    }

    internal static void ExtractExif(ImageMetadataDocument document, PngChunk chunk, ImageMetadataReaderOptions options)
    {
        byte[] data = SafeData(chunk);
        MetadataDirectory directory = PngColorExtractor.FindDirectory(document, chunk);
        AddTag(directory, "Png.eXIf.ByteLength", "EXIF Byte Length", MetadataValue.FromInteger(data.Length), chunk, 0, data.Length, "");
        AddTag(directory, "Png.eXIf.ByteOrder", "TIFF Byte Order", MetadataValue.FromString(JpegExifExtractor.TiffByteOrderName(data)), chunk, 0, DataPrefixLength(data, 2), JpegExifExtractor.TiffByteOrderName(data));

        if (!JpegExifExtractor.HasTiffHeader(data))
        {
            AddInvalidField(document, chunk, "png.exif.tiff.header.invalid", "PNG eXIf payload did not begin with a recognized TIFF header.", 0, data.Length);
        }

        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Exif;
        profile.Family = MetadataFamily.Exif;
        profile.Key = "Exif.PNG.eXIf";
        profile.Name = "EXIF";
        profile.MimeType = "image/tiff";
        profile.Offset = chunk.DataOffset;
        profile.Length = data.Length;
        if (data.Length <= GetMaxProfileBytes(options))
        {
            profile.Data = PngColorExtractor.CopyRange(data, 0, data.Length);
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "png.exif.profile.limit", "PNG EXIF profile exceeded the configured profile limit.", "eXIf", chunk.DataOffset, data.Length);
        }

        document.AddProfile(profile);
    }

    internal static void ExtractText(ImageMetadataDocument document, PngChunk chunk)
    {
        byte[] data = SafeData(chunk);
        int separator = FindKeywordSeparator(document, chunk, data);
        if (separator < 0)
        {
            return;
        }

        string keyword = DecodeLatin1(data, 0, separator, document, chunk);
        string value = DecodeLatin1(data, separator + 1, data.Length - separator - 1, document, chunk);
        MetadataDirectory directory = PngColorExtractor.FindDirectory(document, chunk);
        AddTextTags(directory, chunk, keyword, value, false, 0, "", "");
    }

    internal static void ExtractCompressedText(ImageMetadataDocument document, PngChunk chunk, ImageMetadataReaderOptions options)
    {
        byte[] data = SafeData(chunk);
        int separator = FindKeywordSeparator(document, chunk, data);
        if (separator < 0)
        {
            return;
        }

        if (separator + 1 >= data.Length)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "png.ztxt.compression.truncated", "PNG zTXt compression method was missing.", "zTXt", chunk.DataOffset + separator + 1, 1);
            return;
        }

        string keyword = DecodeLatin1(data, 0, separator, document, chunk);
        int compressionMethod = data[separator + 1];
        MetadataDirectory directory = PngColorExtractor.FindDirectory(document, chunk);
        if (compressionMethod != 0)
        {
            AddTextTags(directory, chunk, keyword, "", true, compressionMethod, "", "");
            AddInvalidField(document, chunk, "png.ztxt.compression.invalid", "PNG zTXt compression method must be zero.", separator + 1, 1);
            return;
        }

        int compressedOffset = separator + 2;
        byte[] compressed = PngColorExtractor.CopyRange(data, compressedOffset, data.Length - compressedOffset);
        byte[] uncompressed = ZlibHelper.Decompress(compressed, GetMaxDecompressedBytes(options), document.Diagnostics, "zTXt");
        string value = DecodeLatin1(uncompressed, 0, uncompressed.Length, document, chunk);
        AddTextTags(directory, chunk, keyword, value, true, compressionMethod, "", "");
    }

    internal static void ExtractInternationalText(ImageMetadataDocument document, PngChunk chunk, ImageMetadataReaderOptions options)
    {
        byte[] data = SafeData(chunk);
        int keywordEnd = FindKeywordSeparator(document, chunk, data);
        if (keywordEnd < 0)
        {
            return;
        }

        if (keywordEnd + 3 > data.Length)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "png.itxt.header.truncated", "PNG iTXt compression fields were truncated.", "iTXt", chunk.DataOffset + keywordEnd + 1, 2);
            return;
        }

        int compressionFlag = data[keywordEnd + 1];
        int compressionMethod = data[keywordEnd + 2];
        int languageStart = keywordEnd + 3;
        int languageEnd = PngColorExtractor.FindNull(data, languageStart, data.Length - languageStart);
        if (languageEnd < 0)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "png.itxt.language.terminator.missing", "PNG iTXt language tag was not null-terminated.", "iTXt", chunk.DataOffset + languageStart, data.Length - languageStart);
            return;
        }

        int translatedStart = languageEnd + 1;
        int translatedEnd = PngColorExtractor.FindNull(data, translatedStart, data.Length - translatedStart);
        if (translatedEnd < 0)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "png.itxt.translated.terminator.missing", "PNG iTXt translated keyword was not null-terminated.", "iTXt", chunk.DataOffset + translatedStart, data.Length - translatedStart);
            return;
        }

        string keyword = DecodeLatin1(data, 0, keywordEnd, document, chunk);
        string language = MetadataStringReader.DecodeAsciiRange(data, languageStart, languageEnd - languageStart, document.Diagnostics, "iTXt");
        string translated = MetadataStringReader.DecodeUtf8RangeWithLatin1Fallback(data, translatedStart, translatedEnd - translatedStart, document.Diagnostics, "iTXt");
        int textStart = translatedEnd + 1;
        byte[] textBytes = PngColorExtractor.CopyRange(data, textStart, data.Length - textStart);
        MetadataDirectory directory = PngColorExtractor.FindDirectory(document, chunk);

        if (compressionFlag != 0 && compressionFlag != 1)
        {
            AddInvalidField(document, chunk, "png.itxt.compression.flag.invalid", "PNG iTXt compression flag must be zero or one.", keywordEnd + 1, 1);
            AddTextTags(directory, chunk, keyword, "", false, compressionMethod, language, translated);
            return;
        }

        if (compressionMethod != 0)
        {
            AddInvalidField(document, chunk, "png.itxt.compression.method.invalid", "PNG iTXt compression method must be zero.", keywordEnd + 2, 1);
            AddTextTags(directory, chunk, keyword, "", compressionFlag == 1, compressionMethod, language, translated);
            return;
        }

        if (compressionFlag == 1)
        {
            textBytes = ZlibHelper.Decompress(textBytes, GetMaxDecompressedBytes(options), document.Diagnostics, "iTXt");
        }

        string text = MetadataStringReader.DecodeUtf8WithLatin1Fallback(textBytes, document.Diagnostics, "iTXt");
        AddTextTags(directory, chunk, keyword, text, compressionFlag == 1, compressionMethod, language, translated);
        AddXmpProfile(document, chunk, keyword, textBytes, text, options);
    }

    internal static void AddTextTags(MetadataDirectory directory, PngChunk chunk, string keyword, string text, bool compressed, int compressionMethod, string language, string translatedKeyword)
    {
        if (directory == null)
        {
            return;
        }

        string prefix = "Png." + chunk.Type;
        directory.AddTagWithValue(prefix + ".Keyword", "Keyword", MetadataValue.FromString(keyword));
        directory.AddTagWithValue(prefix + ".Text", "Text", MetadataValue.FromString(text));
        if (text != null && text.Length > 0)
        {
            AddNormalizedTextTag(directory, keyword, text);
        }

        directory.AddTagWithValue(prefix + ".Compressed", "Compressed", MetadataValue.FromBoolean(compressed));
        if (chunk.Type == "zTXt" || chunk.Type == "iTXt")
        {
            directory.AddTagWithValue(prefix + ".CompressionMethod", "Compression Method", MetadataValue.FromInteger(compressionMethod));
        }

        if (chunk.Type == "iTXt")
        {
            directory.AddTagWithValue(prefix + ".LanguageTag", "Language Tag", MetadataValue.FromString(language));
            directory.AddTagWithValue(prefix + ".TranslatedKeyword", "Translated Keyword", MetadataValue.FromString(translatedKeyword));
        }
    }

    internal static void AddNormalizedTextTag(MetadataDirectory directory, string keyword, string text)
    {
        if (directory == null || keyword == null || text == null || text.Length == 0)
        {
            return;
        }

        if (keyword == "Creation Time")
        {
            directory.AddTagWithValue("Png.Text.CreationTime", "Creation Time", MetadataValue.FromDateText(text));
            return;
        }

        if (keyword == "Author") directory.AddTagWithValue("Png.Text.Author", "Author", MetadataValue.FromString(text));
        if (keyword == "Copyright") directory.AddTagWithValue("Png.Text.Copyright", "Copyright", MetadataValue.FromString(text));
        if (keyword == "Title") directory.AddTagWithValue("Png.Text.Title", "Title", MetadataValue.FromString(text));
        if (keyword == "Description") directory.AddTagWithValue("Png.Text.Description", "Description", MetadataValue.FromString(text));
        if (keyword == "Software") directory.AddTagWithValue("Png.Text.Software", "Software", MetadataValue.FromString(text));
        if (keyword == "Comment") directory.AddTagWithValue("Png.Text.Comment", "Comment", MetadataValue.FromString(text));
        if (keyword == "Keywords") directory.AddTagWithValue("Png.Text.Keywords", "Keywords", MetadataValue.FromString(text));
    }

    internal static void AddXmpProfile(ImageMetadataDocument document, PngChunk chunk, string keyword, byte[] data, string text, ImageMetadataReaderOptions options)
    {
        if (keyword != "XML:com.adobe.xmp" || !ShouldParseXmp(options))
        {
            return;
        }

        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Xmp;
        profile.Family = MetadataFamily.Xmp;
        profile.Key = "Xmp.PNG.iTXt";
        profile.Name = "XMP";
        profile.MimeType = "application/rdf+xml";
        profile.Offset = chunk.DataOffset;
        profile.Length = data == null ? 0 : data.Length;
        if (profile.Length <= GetMaxXmlBytes(options))
        {
            profile.Data = data == null ? new byte[0] : data;
            profile.Text = text;
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "png.xmp.profile.limit", "PNG XMP packet exceeded the configured XML limit.", "iTXt", chunk.DataOffset, profile.Length);
        }

        document.AddProfile(profile);
        XmpPacketParser.ParseProfile(document, profile, options, "PNG/iTXt/XMP");
    }

    internal static int FindKeywordSeparator(ImageMetadataDocument document, PngChunk chunk, byte[] data)
    {
        int separator = PngColorExtractor.FindNull(data, 0, 80);
        if (separator < 1 || separator > 79)
        {
            AddInvalidField(document, chunk, "png.text.keyword.invalid", "PNG text keyword must contain 1 to 79 bytes followed by a null separator.", 0, data.Length);
            return -1;
        }

        if (!PngColorExtractor.IsValidProfileName(data, separator))
        {
            AddInvalidField(document, chunk, "png.text.keyword.characters.invalid", "PNG text keyword contained invalid spacing or character bytes.", 0, separator);
        }

        return separator;
    }

    internal static void ExtractPhysicalDimensions(ImageMetadataDocument document, PngChunk chunk)
    {
        if (!PngColorExtractor.HasLength(document, chunk, 9, "png.phys.length.invalid"))
        {
            return;
        }

        uint x = PngColorExtractor.ReadUnsigned32(chunk.Data, 0);
        uint y = PngColorExtractor.ReadUnsigned32(chunk.Data, 4);
        int unit = chunk.Data[8];
        MetadataDirectory directory = PngColorExtractor.FindDirectory(document, chunk);
        AddTag(directory, "Png.pHYs.PixelsPerUnitX", "Pixels Per Unit X", MetadataValue.FromUnsignedInteger(x), chunk, 0, 4, "");
        AddTag(directory, "Png.pHYs.PixelsPerUnitY", "Pixels Per Unit Y", MetadataValue.FromUnsignedInteger(y), chunk, 4, 4, "");
        AddTag(directory, "Png.pHYs.UnitSpecifier", "Unit Specifier", MetadataValue.FromInteger(unit), chunk, 8, 1, unit == 1 ? "metre" : "unknown");
        AddTag(directory, "Png.pHYs.PixelAspectRatio", "Pixel Aspect Ratio", MetadataValue.FromRational(x, y), chunk, 0, 8, "");
        if (unit > 1)
        {
            AddInvalidField(document, chunk, "png.phys.unit.invalid", "PNG pHYs unit specifier must be zero or one.", 8, 1);
        }
    }

    internal static void ExtractTime(ImageMetadataDocument document, PngChunk chunk)
    {
        if (!PngColorExtractor.HasLength(document, chunk, 7, "png.time.length.invalid"))
        {
            return;
        }

        int year = PngColorExtractor.ReadUnsigned16(chunk.Data, 0);
        int month = chunk.Data[2];
        int day = chunk.Data[3];
        int hour = chunk.Data[4];
        int minute = chunk.Data[5];
        int second = chunk.Data[6];
        string text = year.ToString("D4") + "-" + month.ToString("D2") + "-" + day.ToString("D2") + "T" + hour.ToString("D2") + ":" + minute.ToString("D2") + ":" + second.ToString("D2") + "Z";
        MetadataDirectory directory = PngColorExtractor.FindDirectory(document, chunk);
        AddTag(directory, "Png.tIME.LastModificationTime", "Last Modification Time", MetadataValue.FromDateText(text), chunk, 0, 7, text);
        AddTag(directory, "Png.tIME.Year", "Year", MetadataValue.FromInteger(year), chunk, 0, 2, "");
        AddTag(directory, "Png.tIME.Month", "Month", MetadataValue.FromInteger(month), chunk, 2, 1, "");
        AddTag(directory, "Png.tIME.Day", "Day", MetadataValue.FromInteger(day), chunk, 3, 1, "");
        AddTag(directory, "Png.tIME.Hour", "Hour", MetadataValue.FromInteger(hour), chunk, 4, 1, "");
        AddTag(directory, "Png.tIME.Minute", "Minute", MetadataValue.FromInteger(minute), chunk, 5, 1, "");
        AddTag(directory, "Png.tIME.Second", "Second", MetadataValue.FromInteger(second), chunk, 6, 1, "");
        if (!IsValidTime(month, day, hour, minute, second))
        {
            AddInvalidField(document, chunk, "png.time.value.invalid", "PNG tIME fields contained an out-of-range value.", 0, 7);
        }
    }

    internal static bool IsValidTime(int month, int day, int hour, int minute, int second)
    {
        return month >= 1 && month <= 12 && day >= 1 && day <= 31 && hour <= 23 && minute <= 59 && second <= 60;
    }

    internal static void ExtractTransparency(ImageMetadataDocument document, PngChunk chunk, int colorType)
    {
        MetadataDirectory directory = PngColorExtractor.FindDirectory(document, chunk);
        byte[] data = SafeData(chunk);
        if (colorType == 0 && data.Length == 2)
        {
            AddTag(directory, "Png.tRNS.GraySample", "Transparent Gray Sample", MetadataValue.FromInteger(PngColorExtractor.ReadUnsigned16(data, 0)), chunk, 0, 2, "");
        }
        else if (colorType == 2 && data.Length == 6)
        {
            AddTag(directory, "Png.tRNS.RedSample", "Transparent Red Sample", MetadataValue.FromInteger(PngColorExtractor.ReadUnsigned16(data, 0)), chunk, 0, 2, "");
            AddTag(directory, "Png.tRNS.GreenSample", "Transparent Green Sample", MetadataValue.FromInteger(PngColorExtractor.ReadUnsigned16(data, 2)), chunk, 2, 2, "");
            AddTag(directory, "Png.tRNS.BlueSample", "Transparent Blue Sample", MetadataValue.FromInteger(PngColorExtractor.ReadUnsigned16(data, 4)), chunk, 4, 2, "");
        }
        else if (colorType == 3 && data.Length > 0 && data.Length <= 256)
        {
            AddTag(directory, "Png.tRNS.AlphaValues", "Palette Alpha Values", MetadataValue.FromBytes(PngColorExtractor.CopyRange(data, 0, data.Length)), chunk, 0, data.Length, "");
        }
        else
        {
            AddInvalidField(document, chunk, "png.trns.layout.invalid", "PNG tRNS payload was invalid for the IHDR color type.", 0, data.Length);
            return;
        }

        SetFramesHaveAlpha(document);
    }

    internal static void ExtractBackground(ImageMetadataDocument document, PngChunk chunk, int colorType)
    {
        MetadataDirectory directory = PngColorExtractor.FindDirectory(document, chunk);
        byte[] data = SafeData(chunk);
        if ((colorType == 0 || colorType == 4) && data.Length == 2)
        {
            AddTag(directory, "Png.bKGD.GraySample", "Background Gray Sample", MetadataValue.FromInteger(PngColorExtractor.ReadUnsigned16(data, 0)), chunk, 0, 2, "");
        }
        else if ((colorType == 2 || colorType == 6) && data.Length == 6)
        {
            AddTag(directory, "Png.bKGD.RedSample", "Background Red Sample", MetadataValue.FromInteger(PngColorExtractor.ReadUnsigned16(data, 0)), chunk, 0, 2, "");
            AddTag(directory, "Png.bKGD.GreenSample", "Background Green Sample", MetadataValue.FromInteger(PngColorExtractor.ReadUnsigned16(data, 2)), chunk, 2, 2, "");
            AddTag(directory, "Png.bKGD.BlueSample", "Background Blue Sample", MetadataValue.FromInteger(PngColorExtractor.ReadUnsigned16(data, 4)), chunk, 4, 2, "");
        }
        else if (colorType == 3 && data.Length == 1)
        {
            AddTag(directory, "Png.bKGD.PaletteIndex", "Background Palette Index", MetadataValue.FromInteger(data[0]), chunk, 0, 1, "");
        }
        else
        {
            AddInvalidField(document, chunk, "png.bkgd.layout.invalid", "PNG bKGD payload was invalid for the IHDR color type.", 0, data.Length);
        }
    }

    internal static void ExtractFrameData(ImageMetadataDocument document, PngChunk chunk)
    {
        if (chunk.Data == null || chunk.Data.Length < 4)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "png.fdat.truncated", "PNG fdAT payload was too short for a sequence number.", "fdAT", chunk.DataOffset, chunk.Length);
            return;
        }

        MetadataDirectory directory = PngColorExtractor.FindDirectory(document, chunk);
        AddTag(directory, "Png.fdAT.SequenceNumber", "Sequence Number", MetadataValue.FromUnsignedInteger(PngColorExtractor.ReadUnsigned32(chunk.Data, 0)), chunk, 0, 4, "");
        AddTag(directory, "Png.fdAT.FrameDataByteLength", "Frame Data Byte Length", MetadataValue.FromInteger(chunk.Data.Length - 4), chunk, 4, chunk.Data.Length - 4, "");
    }

    internal static int FindColorType(List<PngChunk> chunks)
    {
        PngChunk? header = PngColorExtractor.FindChunk(chunks, "IHDR");
        if (header == null || header.Data == null || header.Data.Length < 10)
        {
            return -1;
        }

        return header.Data[9];
    }

    internal static void SetFramesHaveAlpha(ImageMetadataDocument document)
    {
        if (document == null || document.Frames == null)
        {
            return;
        }

        foreach (ImageMetadataFrame frame in document.Frames)
        {
            if (frame != null)
            {
                frame.HasAlpha = true;
            }
        }
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, PngChunk chunk, int relativeOffset, long length, string displayValue)
    {
        return PngColorExtractor.AddTag(directory, key, name, value, chunk, relativeOffset, length, displayValue);
    }

    internal static void AddInvalidField(ImageMetadataDocument document, PngChunk chunk, string code, string message, int relativeOffset, long length)
    {
        PngColorExtractor.AddInvalidField(document, chunk, code, message, relativeOffset, length);
    }

    internal static byte[] SafeData(PngChunk chunk)
    {
        if (chunk == null || chunk.Data == null)
        {
            return new byte[0];
        }

        return chunk.Data;
    }

    internal static string DecodeLatin1(byte[] data, int offset, int count, ImageMetadataDocument document, PngChunk chunk)
    {
        return MetadataStringReader.DecodeLatin1Range(data, offset, count, document.Diagnostics, chunk.Type);
    }

    internal static int DataPrefixLength(byte[] data, int maximum)
    {
        if (data == null || data.Length < maximum)
        {
            return data == null ? 0 : data.Length;
        }

        return maximum;
    }

    internal static long GetMaxProfileBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxProfileBytes : options.MaxProfileBytes;
    }

    internal static long GetMaxXmlBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxXmlBytes : options.MaxXmlBytes;
    }

    internal static long GetMaxDecompressedBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxDecompressedBytes : options.MaxDecompressedBytes;
    }

    internal static bool ShouldParseXmp(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseXmp;
    }

    internal static void ExtractAnimation(ImageMetadataDocument document, List<PngChunk> chunks, ImageMetadataReaderOptions options)
    {
        PngChunk? control = PngColorExtractor.FindChunk(chunks, "acTL");
        if (control == null || !control.CrcValid || control.DataLimited)
        {
            return;
        }

        if (!PngColorExtractor.HasLength(document, control, 8, "png.actl.length.invalid"))
        {
            return;
        }

        uint declaredFrames = PngColorExtractor.ReadUnsigned32(control.Data, 0);
        uint plays = PngColorExtractor.ReadUnsigned32(control.Data, 4);
        MetadataDirectory directory = PngColorExtractor.FindDirectory(document, control);
        AddTag(directory, "Png.acTL.FrameCount", "Frame Count", MetadataValue.FromUnsignedInteger(declaredFrames), control, 0, 4, "");
        AddTag(directory, "Png.acTL.PlayCount", "Play Count", MetadataValue.FromUnsignedInteger(plays), control, 4, 4, plays == 0 ? "Infinite" : "");

        if (declaredFrames == 0)
        {
            AddInvalidField(document, control, "png.actl.frame-count.invalid", "PNG acTL frame count must be greater than zero.", 0, 4);
        }

        List<PngChunk> frameControls = FindChunks(chunks, "fcTL");
        if ((ulong)frameControls.Count != declaredFrames)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "png.actl.frame-count.mismatch", "PNG acTL frame count did not match the number of fcTL chunks.", "acTL", control.DataOffset, 4);
        }

        bool firstUsesDefault = frameControls.Count > 0 && frameControls[0].Index < FindChunkIndex(chunks, "IDAT");
        int frameIndex = 0;
        foreach (PngChunk frameControl in frameControls)
        {
            ExtractFrameControl(document, frameControl, options, frameIndex, firstUsesDefault && frameIndex == 0);
            frameIndex = frameIndex + 1;
        }

        foreach (ImageMetadataFrame frame in document.Frames)
        {
            if (frame != null)
            {
                frame.IsAnimated = true;
            }
        }
    }

    internal static void ExtractFrameControl(ImageMetadataDocument document, PngChunk chunk, ImageMetadataReaderOptions options, int animationIndex, bool reuseDefaultFrame)
    {
        if (!PngColorExtractor.HasLength(document, chunk, 26, "png.fctl.length.invalid"))
        {
            return;
        }

        uint sequence = PngColorExtractor.ReadUnsigned32(chunk.Data, 0);
        uint width = PngColorExtractor.ReadUnsigned32(chunk.Data, 4);
        uint height = PngColorExtractor.ReadUnsigned32(chunk.Data, 8);
        uint x = PngColorExtractor.ReadUnsigned32(chunk.Data, 12);
        uint y = PngColorExtractor.ReadUnsigned32(chunk.Data, 16);
        int delayNumerator = PngColorExtractor.ReadUnsigned16(chunk.Data, 20);
        int delayDenominator = PngColorExtractor.ReadUnsigned16(chunk.Data, 22);
        int dispose = chunk.Data[24];
        int blend = chunk.Data[25];
        MetadataDirectory directory = PngColorExtractor.FindDirectory(document, chunk);
        AddTag(directory, "Png.fcTL.SequenceNumber", "Sequence Number", MetadataValue.FromUnsignedInteger(sequence), chunk, 0, 4, "");
        AddTag(directory, "Png.fcTL.Width", "Frame Width", MetadataValue.FromUnsignedInteger(width), chunk, 4, 4, "");
        AddTag(directory, "Png.fcTL.Height", "Frame Height", MetadataValue.FromUnsignedInteger(height), chunk, 8, 4, "");
        AddTag(directory, "Png.fcTL.XOffset", "X Offset", MetadataValue.FromUnsignedInteger(x), chunk, 12, 4, "");
        AddTag(directory, "Png.fcTL.YOffset", "Y Offset", MetadataValue.FromUnsignedInteger(y), chunk, 16, 4, "");
        AddTag(directory, "Png.fcTL.Delay", "Frame Delay", MetadataValue.FromRational(delayNumerator, delayDenominator == 0 ? 100 : delayDenominator), chunk, 20, 4, "");
        AddTag(directory, "Png.fcTL.DisposeOperation", "Dispose Operation", MetadataValue.FromInteger(dispose), chunk, 24, 1, DisposeName(dispose));
        AddTag(directory, "Png.fcTL.BlendOperation", "Blend Operation", MetadataValue.FromInteger(blend), chunk, 25, 1, BlendName(blend));

        if (!PngIhdrExtractor.IsValidDimension(width) || !PngIhdrExtractor.IsValidDimension(height))
        {
            AddInvalidField(document, chunk, "png.fctl.dimensions.invalid", "PNG fcTL width and height must be greater than zero.", 4, 8);
            return;
        }

        if (!FitsCanvas(document, width, height, x, y))
        {
            AddInvalidField(document, chunk, "png.fctl.bounds.invalid", "PNG fcTL frame rectangle exceeded the image canvas.", 4, 16);
        }

        if (dispose > 2)
        {
            AddInvalidField(document, chunk, "png.fctl.dispose.invalid", "PNG fcTL disposal operation was invalid.", 24, 1);
        }

        if (blend > 1)
        {
            AddInvalidField(document, chunk, "png.fctl.blend.invalid", "PNG fcTL blend operation was invalid.", 25, 1);
        }

        ImageMetadataFrame? frame = null;
        if (reuseDefaultFrame && document.Frames.Count > 0)
        {
            frame = document.Frames[0];
        }
        else if (document.Frames.Count < GetMaxFrameCount(options))
        {
            frame = document.AddFrame(new ImageMetadataFrame());
            CopyDefaultFrameCharacteristics(document, frame);
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "png.frame.limit", "PNG animation frame count exceeded the configured frame limit.", "fcTL", chunk.Offset, 12 + chunk.Length);
        }

        if (frame != null)
        {
            frame.Width = (int)width;
            frame.Height = (int)height;
            frame.CanvasX = (int)x;
            frame.CanvasY = (int)y;
            frame.IsAnimated = true;
            frame.DurationMilliseconds = DelayMilliseconds(delayNumerator, delayDenominator);
            if (frame.CanvasWidth == 0 && document.Summary.Width > 0)
            {
                frame.CanvasWidth = document.Summary.Width;
                frame.CanvasHeight = document.Summary.Height;
            }
        }
    }

    internal static void CopyDefaultFrameCharacteristics(ImageMetadataDocument document, ImageMetadataFrame frame)
    {
        if (document == null || frame == null || document.Frames == null || document.Frames.Count == 0)
        {
            return;
        }

        ImageMetadataFrame source = document.Frames[0];
        if (source == null || source == frame)
        {
            return;
        }

        frame.CanvasWidth = source.CanvasWidth;
        frame.CanvasHeight = source.CanvasHeight;
        frame.CanvasX = source.CanvasX;
        frame.CanvasY = source.CanvasY;
        frame.PixelAspectRatio = source.PixelAspectRatio;
        frame.HorizontalResolution = source.HorizontalResolution;
        frame.VerticalResolution = source.VerticalResolution;
        frame.ResolutionUnit = source.ResolutionUnit;
        frame.BitDepth = source.BitDepth;
        frame.ChannelCount = source.ChannelCount;
        frame.HasAlpha = source.HasAlpha;
        frame.Compression = source.Compression;
        frame.ColorSpace = source.ColorSpace;
        frame.SourceColorType = source.SourceColorType;
        frame.IsInterlaced = source.IsInterlaced;
        frame.IsProgressive = source.IsProgressive;
        frame.Orientation = source.Orientation;
    }

    internal static int DelayMilliseconds(int numerator, int denominator)
    {
        int actualDenominator = denominator == 0 ? 100 : denominator;
        double milliseconds = ((double)numerator * 1000.0) / actualDenominator;
        if (milliseconds > 2147483647)
        {
            return 2147483647;
        }

        return (int)(milliseconds + 0.5);
    }

    internal static bool FitsCanvas(ImageMetadataDocument document, uint width, uint height, uint x, uint y)
    {
        if (document == null || document.Frames == null || document.Frames.Count == 0)
        {
            return true;
        }

        ImageMetadataFrame canvas = document.Frames[0];
        return (ulong)x + width <= (ulong)canvas.CanvasWidth && (ulong)y + height <= (ulong)canvas.CanvasHeight;
    }

    internal static List<PngChunk> FindChunks(List<PngChunk> chunks, string type)
    {
        List<PngChunk> matches = new List<PngChunk>();
        foreach (PngChunk chunk in chunks)
        {
            if (chunk != null && chunk.Type == type && chunk.CrcValid && !chunk.DataLimited)
            {
                matches.Add(chunk);
            }
        }

        return matches;
    }

    internal static int FindChunkIndex(List<PngChunk> chunks, string type)
    {
        PngChunk? chunk = PngColorExtractor.FindChunk(chunks, type);
        return chunk == null ? 2147483647 : chunk.Index;
    }

    internal static string DisposeName(int value)
    {
        switch (value)
        {
            case 0:
                return "None";
            case 1:
                return "Background";
            case 2:
                return "Previous";
            default:
                return "Unknown";
        }
    }

    internal static string BlendName(int value)
    {
        switch (value)
        {
            case 0:
                return "Source";
            case 1:
                return "Over";
            default:
                return "Unknown";
        }
    }

    internal static int GetMaxFrameCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxFrameCount : options.MaxFrameCount;
    }
}
