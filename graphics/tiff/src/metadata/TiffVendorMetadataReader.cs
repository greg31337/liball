using System;
using System.Text;

namespace tiff;

internal class TiffVendorMetadataReader
{
    internal static TiffVendorMetadataReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return ReadWithByteOrder(directory, fileBytes, DetectByteOrder(fileBytes), new TiffLimits());
    }

    internal static TiffVendorMetadataReadResult ReadWithByteOrder(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffVendorMetadataMatch[] matches = new TiffVendorMetadataMatch[0];
        TiffVendorText[] texts = new TiffVendorText[0];
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read vendor metadata.");
            return TiffVendorMetadataReadResult.Create(CreateMetadata(matches, new TiffDngMetadata(), texts, new TiffVendorPrivatePayload[0]), diagnostics);
        }

        TiffImageDescriptionReadResult descriptionResult = TiffImageDescriptionReader.Read(directory, fileBytes, limits);
        diagnostics.Merge(descriptionResult.Diagnostics);
        if (descriptionResult.Success && descriptionResult.Metadata.HasDescription)
        {
            texts = AppendText(texts, TiffVendorText.Create("ImageDescription", TiffTagId.ImageDescription, descriptionResult.Metadata.Text));
            matches = AddTextMatches(matches, descriptionResult.Metadata.Text, "ImageDescription", TiffTagId.ImageDescription);
        }

        TiffXmpReadResult xmpResult = TiffXmpMetadataReader.Read(directory, fileBytes, limits);
        diagnostics.Merge(xmpResult.Diagnostics);
        if (xmpResult.Success && xmpResult.Packet.HasXmp)
        {
            texts = AppendText(texts, TiffVendorText.Create("XMP", TiffTagId.Xmp, xmpResult.Packet.Text));
            matches = AddTextMatches(matches, xmpResult.Packet.Text, "XMP", TiffTagId.Xmp);
        }

        int[] asciiTagIds = new int[] { TiffTagId.Make, TiffTagId.Model, TiffTagId.Software, TiffTagId.DocumentName, TiffTagId.PageName };
        int textIndex = 0;
        while (textIndex < asciiTagIds.Length)
        {
            int tagId = asciiTagIds[textIndex];
            string text = ReadAsciiTagText(directory, fileBytes, limits, tagId, diagnostics);
            if (text.Length > 0)
            {
                string source = GetTagName(tagId);
                texts = AppendText(texts, TiffVendorText.Create(source, tagId, text));
                matches = AddTextMatches(matches, text, source, tagId);
            }

            textIndex = textIndex + 1;
        }

        matches = AddKnownTagMatches(matches, directory);
        TiffDngMetadata dng = ReadDng(directory, fileBytes, byteOrder, limits, diagnostics);
        TiffVendorPrivatePayload[] privatePayloads = ReadPrivatePayloads(directory, fileBytes, limits, diagnostics);
        return TiffVendorMetadataReadResult.Create(CreateMetadata(matches, dng, texts, privatePayloads), diagnostics);
    }

    internal static TiffVendorMetadata CreateMetadata(TiffVendorMetadataMatch[] matches, TiffDngMetadata dng, TiffVendorText[] texts, TiffVendorPrivatePayload[] privatePayloads)
    {
        TiffVendorVariantMetadata[] variants = new TiffVendorVariantMetadata[8];
        variants[0] = BuildVariant(matches, texts, privatePayloads, "Aperio", "SVS", new string[] { "Aperio", "SVS", "ScanScope" }, true, false);
        variants[1] = BuildVariant(matches, texts, privatePayloads, "Hamamatsu", "NDPI", new string[] { "Hamamatsu", "NDPI", "NDP.scan" }, true, false);
        variants[2] = BuildVariant(matches, texts, privatePayloads, "Leica", "SCN", new string[] { "Leica", "SCN" }, true, false);
        variants[3] = BuildVariant(matches, texts, privatePayloads, "Philips", "Philips TIFF", new string[] { "Philips", "PIM_DP_SCANNED_IMAGES" }, true, false);
        variants[4] = BuildVariant(matches, texts, privatePayloads, "Roche/Ventana", "BIF", new string[] { "Roche", "Ventana", "BIF" }, true, false);
        variants[5] = BuildVariant(matches, texts, privatePayloads, "Zeiss", "LSM", new string[] { "Zeiss", "LSM", "Carl Zeiss" }, false, true);
        variants[6] = BuildVariant(matches, texts, privatePayloads, "Micro-Manager", "Micro-Manager", new string[] { "Micro-Manager", "MicroManager", "Micro-Manager Metadata" }, false, true);
        variants[7] = BuildVariant(matches, texts, privatePayloads, "Astro-TIFF", "Astro-TIFF", new string[] { "Astro-TIFF", "SIMPLE  =", "FITS", "PixInsight", "MaxIm DL", "AstroImageJ", "SBIG" }, false, true);
        return TiffVendorMetadata.Create(matches, dng, variants, privatePayloads);
    }

    internal static TiffVendorVariantMetadata BuildVariant(TiffVendorMetadataMatch[] matches, TiffVendorText[] texts, TiffVendorPrivatePayload[] privatePayloads, string vendor, string format, string[] patterns, bool isSlide, bool isScientific)
    {
        bool hasMetadata = HasMatch(matches, vendor, format);
        TiffVendorText[] selectedTexts = new TiffVendorText[0];
        TiffVendorField[] fields = new TiffVendorField[0];
        int index = 0;
        while (index < texts.Length)
        {
            if (FindPattern(texts[index].Text, patterns).Length > 0)
            {
                selectedTexts = AppendText(selectedTexts, texts[index]);
                fields = ParseFields(fields, texts[index]);
            }

            index = index + 1;
        }

        TiffVendorPrivatePayload[] payloads = new TiffVendorPrivatePayload[0];
        if (hasMetadata)
        {
            payloads = privatePayloads;
        }

        return TiffVendorVariantMetadata.Create(hasMetadata, vendor, format, isSlide, isScientific, selectedTexts, fields, payloads);
    }

    internal static TiffVendorField[] ParseFields(TiffVendorField[] fields, TiffVendorText text)
    {
        string normalized = text.Text.Replace('\r', '\n').Replace('|', '\n').Replace(';', '\n');
        string[] segments = normalized.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        bool jsonLike = text.Text.IndexOf("{", StringComparison.Ordinal) >= 0;
        int index = 0;
        while (index < segments.Length)
        {
            string segment = segments[index].Trim();
            int delimiter = segment.IndexOf("=", StringComparison.Ordinal);
            if (delimiter < 0 && jsonLike)
            {
                delimiter = segment.IndexOf(":", StringComparison.Ordinal);
            }

            if (delimiter > 0 && delimiter < segment.Length - 1)
            {
                string key = CleanFieldPart(segment.Substring(0, delimiter));
                string value = CleanFieldPart(segment.Substring(delimiter + 1));
                if (key.Length > 0)
                {
                    fields = AppendField(fields, TiffVendorField.Create(key, value, text.Source, text.TagId));
                }
            }

            index = index + 1;
        }

        return fields;
    }

    internal static string CleanFieldPart(string text)
    {
        string value = text.Trim();
        while (value.Length > 0 && (value[0] == '{' || value[0] == '[' || value[0] == '"' || value[0] == ','))
        {
            value = value.Substring(1).Trim();
        }

        while (value.Length > 0 && (value[value.Length - 1] == '}' || value[value.Length - 1] == ']' || value[value.Length - 1] == '"' || value[value.Length - 1] == ','))
        {
            value = value.Substring(0, value.Length - 1).Trim();
        }

        return value;
    }

    internal static TiffDngMetadata ReadDng(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffDngField[] fields = new TiffDngField[0];
        int[] version = new int[0];
        int[] backwardVersion = new int[0];
        string uniqueCameraModel = "";
        string localizedCameraModel = "";
        byte[] privateData = new byte[0];
        byte[] rawDataUniqueId = new byte[0];
        byte[] originalRawFileData = new byte[0];
        int index = 0;
        while (index < directory.Entries.Length)
        {
            TiffIfdEntry entry = directory.Entries[index];
            if (IsDngTag(entry.TagId))
            {
                TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
                diagnostics.Merge(loadResult.Diagnostics);
                if (loadResult.Success)
                {
                    TiffTagValue value = TiffExifMetadataReader.DecodeValue(entry, loadResult.Bytes, byteOrder, diagnostics);
                    fields = AppendDngField(fields, TiffDngField.Create(entry.TagId, GetTagName(entry.TagId), value, loadResult.Bytes));
                    if (entry.TagId == TiffTagId.DngVersion)
                    {
                        version = ConvertVersion(value.UnsignedValues);
                    }
                    else if (entry.TagId == TiffTagId.DngBackwardVersion)
                    {
                        backwardVersion = ConvertVersion(value.UnsignedValues);
                    }
                    else if (entry.TagId == TiffTagId.UniqueCameraModel)
                    {
                        uniqueCameraModel = value.Text;
                    }
                    else if (entry.TagId == TiffTagId.LocalizedCameraModel)
                    {
                        localizedCameraModel = Encoding.UTF8.GetString(loadResult.Bytes).TrimEnd('\0');
                    }
                    else if (entry.TagId == TiffTagId.DngPrivateData)
                    {
                        privateData = loadResult.Bytes;
                    }
                    else if (entry.TagId == TiffTagId.RawDataUniqueId)
                    {
                        rawDataUniqueId = loadResult.Bytes;
                    }
                    else if (entry.TagId == TiffTagId.OriginalRawFileData)
                    {
                        originalRawFileData = loadResult.Bytes;
                    }
                }
            }

            index = index + 1;
        }

        return TiffDngMetadata.Create(version, backwardVersion, uniqueCameraModel, localizedCameraModel, fields, privateData, rawDataUniqueId, originalRawFileData);
    }

    internal static TiffVendorPrivatePayload[] ReadPrivatePayloads(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffVendorPrivatePayload[] payloads = new TiffVendorPrivatePayload[0];
        TiffTagRegistry registry = TiffTagRegistry.CreateDefault();
        int index = 0;
        while (index < directory.Entries.Length)
        {
            TiffIfdEntry entry = directory.Entries[index];
            bool isPrivate = entry.TagId == TiffTagId.CzLsmInfo || entry.TagId == TiffTagId.DngPrivateData || entry.TagId == TiffTagId.OriginalRawFileData || entry.TagId >= 32768 && !registry.Contains(entry.TagId);
            if (isPrivate)
            {
                TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
                if (loadResult.Success)
                {
                    payloads = AppendPayload(payloads, TiffVendorPrivatePayload.Create(entry.TagId, entry.DataType, entry.Count, loadResult.Bytes));
                }
                else
                {
                    diagnostics.AddWarningForTag(TiffErrorCode.InvalidByteRange, "Vendor-private payload could not be preserved from the supplied byte range.", directory.ImageIndex, entry.TagId);
                }
            }

            index = index + 1;
        }

        return payloads;
    }

    internal static TiffVendorMetadataMatch[] AddTextMatches(TiffVendorMetadataMatch[] matches, string text, string source, int tagId)
    {
        matches = AddMatchForPatterns(matches, text, source, tagId, "Aperio", "SVS", new string[] { "Aperio", "SVS", "ScanScope" }, true, false);
        matches = AddMatchForPatterns(matches, text, source, tagId, "Hamamatsu", "NDPI", new string[] { "Hamamatsu", "NDPI", "NDP.scan" }, true, false);
        matches = AddMatchForPatterns(matches, text, source, tagId, "Leica", "SCN", new string[] { "Leica", "SCN" }, true, false);
        matches = AddMatchForPatterns(matches, text, source, tagId, "Philips", "Philips TIFF", new string[] { "Philips", "PIM_DP_SCANNED_IMAGES" }, true, false);
        matches = AddMatchForPatterns(matches, text, source, tagId, "Roche/Ventana", "BIF", new string[] { "Roche", "Ventana", "BIF" }, true, false);
        matches = AddMatchForPatterns(matches, text, source, tagId, "Zeiss", "LSM", new string[] { "Zeiss", "LSM", "Carl Zeiss" }, false, true);
        matches = AddMatchForPatterns(matches, text, source, tagId, "Micro-Manager", "Micro-Manager", new string[] { "Micro-Manager", "MicroManager", "Micro-Manager Metadata" }, false, true);
        matches = AddMatchForPatterns(matches, text, source, tagId, "Adobe", "DNG", new string[] { "DNG", "Digital Negative" }, false, true);
        matches = AddMatchForPatterns(matches, text, source, tagId, "Astro-TIFF", "Astro-TIFF", new string[] { "Astro-TIFF", "SIMPLE  =", "FITS", "PixInsight", "MaxIm DL", "AstroImageJ", "SBIG" }, false, true);
        return matches;
    }

    internal static TiffVendorMetadataMatch[] AddKnownTagMatches(TiffVendorMetadataMatch[] matches, TiffDirectory directory)
    {
        if (directory.FindEntry(TiffTagId.CzLsmInfo) != null)
        {
            matches = AppendUniqueMatch(matches, TiffVendorMetadataMatch.Create("Zeiss", "LSM", "TIFF tag", "CZ_LSMInfo", TiffTagId.CzLsmInfo, false, true));
        }

        int dngTagId = FindFirstTag(directory, GetDngTagIds());
        if (dngTagId != 0)
        {
            matches = AppendUniqueMatch(matches, TiffVendorMetadataMatch.Create("Adobe", "DNG", "TIFF tag", GetTagName(dngTagId), dngTagId, false, true));
        }

        return matches;
    }

    internal static TiffVendorMetadataMatch[] AddMatchForPatterns(TiffVendorMetadataMatch[] matches, string text, string source, int tagId, string vendor, string format, string[] patterns, bool isSlide, bool isScientific)
    {
        string evidence = FindPattern(text, patterns);
        if (evidence.Length == 0)
        {
            return matches;
        }

        return AppendUniqueMatch(matches, TiffVendorMetadataMatch.Create(vendor, format, source, evidence, tagId, isSlide, isScientific));
    }

    internal static TiffVendorMetadataMatch[] AppendUniqueMatch(TiffVendorMetadataMatch[] matches, TiffVendorMetadataMatch match)
    {
        if (HasMatch(matches, match.Vendor, match.Format))
        {
            return matches;
        }

        TiffVendorMetadataMatch[] result = new TiffVendorMetadataMatch[matches.Length + 1];
        int index = 0;
        while (index < matches.Length)
        {
            result[index] = matches[index].Copy();
            index = index + 1;
        }

        result[index] = match.Copy();
        return result;
    }

    internal static bool HasMatch(TiffVendorMetadataMatch[] matches, string vendor, string format)
    {
        int index = 0;
        while (matches != null && index < matches.Length)
        {
            if (StringEquals(matches[index].Vendor, vendor) && StringEquals(matches[index].Format, format))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string FindPattern(string text, string[] patterns)
    {
        if (text == null || text.Length == 0 || patterns == null)
        {
            return "";
        }

        int index = 0;
        while (index < patterns.Length)
        {
            if (patterns[index] != null && text.IndexOf(patterns[index], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return patterns[index];
            }

            index = index + 1;
        }

        return "";
    }

    internal static string ReadAsciiTagText(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffLimits limits, int tagId, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            return "";
        }

        if (entry.DataType != TiffDataType.Ascii)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Vendor metadata text tag must use ASCII data.", directory.ImageIndex, tagId);
            return "";
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return "";
        }

        TiffAsciiResult asciiResult = TiffAscii.Decode(loadResult.Bytes);
        diagnostics.Merge(asciiResult.Diagnostics);
        if (!asciiResult.Success)
        {
            return "";
        }

        return asciiResult.Text;
    }

    internal static int[] GetDngTagIds()
    {
        return new int[] { TiffTagId.DngVersion, TiffTagId.DngBackwardVersion, TiffTagId.UniqueCameraModel, TiffTagId.LocalizedCameraModel, TiffTagId.CFAPlaneColor, TiffTagId.CFALayout, TiffTagId.LinearizationTable, TiffTagId.BlackLevelRepeatDim, TiffTagId.BlackLevel, TiffTagId.BlackLevelDeltaH, TiffTagId.BlackLevelDeltaV, TiffTagId.WhiteLevel, TiffTagId.DefaultScale, TiffTagId.DefaultCropOrigin, TiffTagId.DefaultCropSize, TiffTagId.ColorMatrix1, TiffTagId.ColorMatrix2, TiffTagId.CameraCalibration1, TiffTagId.CameraCalibration2, TiffTagId.ReductionMatrix1, TiffTagId.ReductionMatrix2, TiffTagId.AnalogBalance, TiffTagId.AsShotNeutral, TiffTagId.AsShotWhiteXY, TiffTagId.BaselineExposure, TiffTagId.BaselineNoise, TiffTagId.BaselineSharpness, TiffTagId.BayerGreenSplit, TiffTagId.LinearResponseLimit, TiffTagId.CameraSerialNumber, TiffTagId.LensInfo, TiffTagId.ChromaBlurRadius, TiffTagId.AntiAliasStrength, TiffTagId.ShadowScale, TiffTagId.DngPrivateData, TiffTagId.MakerNoteSafety, TiffTagId.CalibrationIlluminant1, TiffTagId.CalibrationIlluminant2, TiffTagId.BestQualityScale, TiffTagId.RawDataUniqueId, TiffTagId.OriginalRawFileName, TiffTagId.OriginalRawFileData, TiffTagId.ActiveArea, TiffTagId.MaskedAreas, TiffTagId.AsShotIccProfile, TiffTagId.AsShotPreProfileMatrix, TiffTagId.CurrentIccProfile, TiffTagId.CurrentPreProfileMatrix, TiffTagId.DepthFormat, TiffTagId.DepthNear, TiffTagId.DepthFar, TiffTagId.DepthUnits, TiffTagId.DepthMeasureType, TiffTagId.EnhanceParams };
    }

    internal static bool IsDngTag(int tagId)
    {
        int[] tagIds = GetDngTagIds();
        int index = 0;
        while (index < tagIds.Length)
        {
            if (tagIds[index] == tagId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int FindFirstTag(TiffDirectory directory, int[] tagIds)
    {
        int index = 0;
        while (tagIds != null && index < tagIds.Length)
        {
            if (directory.FindEntry(tagIds[index]) != null)
            {
                return tagIds[index];
            }

            index = index + 1;
        }

        return 0;
    }

    internal static string GetTagName(int tagId)
    {
        TiffTagDefinition? definition = TiffTagRegistry.CreateDefault().Find(tagId);
        if (definition != null)
        {
            return definition.Name;
        }

        return "TIFF tag";
    }

    internal static TiffVendorText[] AppendText(TiffVendorText[] values, TiffVendorText value)
    {
        TiffVendorText[] result = new TiffVendorText[values.Length + 1];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index].Copy();
            index = index + 1;
        }

        result[index] = value.Copy();
        return result;
    }

    internal static TiffVendorField[] AppendField(TiffVendorField[] values, TiffVendorField value)
    {
        TiffVendorField[] result = new TiffVendorField[values.Length + 1];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index].Copy();
            index = index + 1;
        }

        result[index] = value.Copy();
        return result;
    }

    internal static TiffDngField[] AppendDngField(TiffDngField[] values, TiffDngField value)
    {
        TiffDngField[] result = new TiffDngField[values.Length + 1];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index].Copy();
            index = index + 1;
        }

        result[index] = value.Copy();
        return result;
    }

    internal static TiffVendorPrivatePayload[] AppendPayload(TiffVendorPrivatePayload[] values, TiffVendorPrivatePayload value)
    {
        TiffVendorPrivatePayload[] result = new TiffVendorPrivatePayload[values.Length + 1];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index].Copy();
            index = index + 1;
        }

        result[index] = value.Copy();
        return result;
    }

    internal static int[] ConvertVersion(ulong[] values)
    {
        int[] result = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = (int)values[index];
            index = index + 1;
        }

        return result;
    }

    internal static TiffByteOrder DetectByteOrder(ReadOnlySpan<byte> fileBytes)
    {
        if (fileBytes.Length >= 2 && fileBytes[0] == 0x4d && fileBytes[1] == 0x4d)
        {
            return TiffByteOrder.BigEndian;
        }

        return TiffByteOrder.LittleEndian;
    }

    internal static bool StringEquals(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
