using System;

namespace tiff;

internal class TiffImageJMetadataReader
{
    internal static TiffImageJMetadataReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffImageDescriptionReadResult descriptionResult = TiffImageDescriptionReader.Read(directory, fileBytes);
        diagnostics.Merge(descriptionResult.Diagnostics);
        TiffImageJMetadata metadata = new TiffImageJMetadata();
        if (descriptionResult.Success && descriptionResult.Metadata.HasDescription)
        {
            metadata = Parse(descriptionResult.Metadata.Text, descriptionResult.Metadata.RawBytes);
        }

        ApplyTiffCalibration(metadata, directory, fileBytes, diagnostics);
        TiffImageJPrivateMetadataReader.Apply(metadata, directory, fileBytes, new TiffLimits(), diagnostics);
        return TiffImageJMetadataReadResult.Create(metadata, diagnostics);
    }

    internal static TiffImageJMetadata Parse(string description, byte[] rawBytes)
    {
        string text = TiffImageJMetadataEntry.NormalizeString(description);
        TiffImageJMetadataEntry[] entries = ParseEntries(text);
        string version = FindValue(entries, "ImageJ");
        if (version.Length == 0)
        {
            return TiffImageJMetadata.Create(false, false, "", 0, 0, 0, 0, "", "", "", text, rawBytes, new TiffImageJMetadataEntry[0]);
        }

        bool isHyperstack = IsTrueValue(FindValue(entries, "hyperstack"));
        int imageCount = ParseNonNegativeInt(FindValue(entries, "images"));
        int channels = ParseNonNegativeInt(FindValue(entries, "channels"));
        int slices = ParseNonNegativeInt(FindValue(entries, "slices"));
        int frames = ParseNonNegativeInt(FindValue(entries, "frames"));
        string mode = FindValue(entries, "mode");
        string unit = FindValue(entries, "unit");
        string spacing = FindValue(entries, "spacing");
        TiffImageJMetadata metadata = TiffImageJMetadata.Create(true, isHyperstack, version, imageCount, channels, slices, frames, mode, unit, spacing, text, rawBytes, entries);
        metadata.Calibration.Unit = unit;
        metadata.Calibration.PixelWidth = ParseNumber(FindValue(entries, "pixel_width"));
        metadata.Calibration.PixelHeight = ParseNumber(FindValue(entries, "pixel_height"));
        metadata.Calibration.PixelDepth = ParseNumber(spacing);
        metadata.Calibration.FrameInterval = ParseNumber(FindValue(entries, "finterval"));
        metadata.Calibration.TimeUnit = FindValue(entries, "tunit");
        metadata.Calibration.FramesPerSecond = ParseNumber(FindValue(entries, "fps"));
        metadata.Calibration.XOrigin = ParseNumber(FindValue(entries, "xorigin"));
        metadata.Calibration.YOrigin = ParseNumber(FindValue(entries, "yorigin"));
        metadata.Calibration.ZOrigin = ParseNumber(FindValue(entries, "zorigin"));
        return metadata;
    }

    internal static TiffImageJMetadataEntry[] ParseEntries(string description)
    {
        TiffImageJMetadataEntry[] entries = new TiffImageJMetadataEntry[0];
        string text = TiffImageJMetadataEntry.NormalizeString(description);
        int lineStart = 0;
        int index = 0;
        while (index <= text.Length)
        {
            if (index == text.Length || text[index] == '\n')
            {
                string line = TrimLineEnd(text.Substring(lineStart, index - lineStart));
                TiffImageJMetadataEntry entry = ParseEntry(line);
                if (entry.Key.Length > 0)
                {
                    entries = AppendEntry(entries, entry);
                }

                lineStart = index + 1;
            }

            index = index + 1;
        }

        return entries;
    }

    internal static TiffImageJMetadataEntry ParseEntry(string line)
    {
        string actualLine = TiffImageJMetadataEntry.NormalizeString(line);
        int equalsIndex = FindEquals(actualLine);
        if (equalsIndex <= 0)
        {
            return new TiffImageJMetadataEntry();
        }

        string key = Trim(actualLine.Substring(0, equalsIndex));
        string value = Trim(actualLine.Substring(equalsIndex + 1));
        return TiffImageJMetadataEntry.Create(key, value);
    }

    internal static string FindValue(TiffImageJMetadataEntry[] entries, string key)
    {
        if (entries == null)
        {
            return "";
        }

        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index] != null && TiffImageJMetadata.EqualsIgnoreCase(entries[index].Key, key))
            {
                return entries[index].Value;
            }

            index = index + 1;
        }

        return "";
    }

    internal static bool IsTrueValue(string value)
    {
        return TiffImageJMetadata.EqualsIgnoreCase(value, "true") || value == "1" || TiffImageJMetadata.EqualsIgnoreCase(value, "yes");
    }

    internal static int ParseNonNegativeInt(string text)
    {
        string value = Trim(TiffImageJMetadataEntry.NormalizeString(text));
        if (value.Length == 0)
        {
            return 0;
        }

        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            char digit = value[index];
            if (digit < '0' || digit > '9' || result > (int.MaxValue - (digit - '0')) / 10)
            {
                return 0;
            }

            result = (result * 10) + (digit - '0');
            index = index + 1;
        }

        return result;
    }

    internal static double ParseNumber(string text)
    {
        string value = Trim(TiffImageJMetadataEntry.NormalizeString(text));
        if (value.Length == 0)
        {
            return 0;
        }

        int sign = 1;
        int index = 0;
        if (value[index] == '-')
        {
            sign = -1;
            index = index + 1;
        }
        else if (value[index] == '+')
        {
            index = index + 1;
        }

        double result = 0;
        bool hasDigit = false;
        while (index < value.Length && value[index] >= '0' && value[index] <= '9')
        {
            result = result * 10d + (value[index] - '0');
            hasDigit = true;
            index = index + 1;
        }

        if (index < value.Length && value[index] == '.')
        {
            index = index + 1;
            double fraction = 0;
            double divisor = 1d;
            while (index < value.Length && value[index] >= '0' && value[index] <= '9')
            {
                fraction = fraction * 10d + (value[index] - '0');
                divisor = divisor * 10d;
                hasDigit = true;
                index = index + 1;
            }

            result = result + fraction / divisor;
        }

        if (!hasDigit)
        {
            return 0;
        }

        int exponent = 0;
        int exponentSign = 1;
        if (index < value.Length && (value[index] == 'e' || value[index] == 'E'))
        {
            index = index + 1;
            if (index < value.Length && value[index] == '-')
            {
                exponentSign = -1;
                index = index + 1;
            }
            else if (index < value.Length && value[index] == '+')
            {
                index = index + 1;
            }

            bool hasExponentDigit = false;
            while (index < value.Length && value[index] >= '0' && value[index] <= '9' && exponent < 309)
            {
                exponent = exponent * 10 + (value[index] - '0');
                hasExponentDigit = true;
                index = index + 1;
            }

            if (!hasExponentDigit)
            {
                return 0;
            }
        }

        if (index != value.Length)
        {
            return 0;
        }

        int scaleIndex = 0;
        while (scaleIndex < exponent)
        {
            if (exponentSign > 0)
            {
                result = result * 10d;
            }
            else
            {
                result = result / 10d;
            }

            scaleIndex = scaleIndex + 1;
        }

        return result * sign;
    }

    internal static void ApplyTiffCalibration(TiffImageJMetadata metadata, TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffDiagnostics diagnostics)
    {
        if (metadata == null || !metadata.HasImageJ || directory == null)
        {
            return;
        }

        TiffByteOrder byteOrder = DetectTiffByteOrder(fileBytes);
        if (byteOrder == TiffByteOrder.Unknown)
        {
            return;
        }

        TiffIfdEntry? xResolution = directory.FindEntry(TiffTagId.XResolution);
        TiffIfdEntry? yResolution = directory.FindEntry(TiffTagId.YResolution);
        if (xResolution != null)
        {
            metadata.Calibration.PixelWidth = ReadPixelSize(xResolution, fileBytes, byteOrder, directory.ImageIndex, diagnostics);
        }

        if (yResolution != null)
        {
            metadata.Calibration.PixelHeight = ReadPixelSize(yResolution, fileBytes, byteOrder, directory.ImageIndex, diagnostics);
        }

        if (metadata.Calibration.Unit.Length == 0)
        {
            TiffIfdEntry? resolutionUnit = directory.FindEntry(TiffTagId.ResolutionUnit);
            if (resolutionUnit != null)
            {
                metadata.Calibration.Unit = ReadResolutionUnit(resolutionUnit, fileBytes, byteOrder, directory.ImageIndex, diagnostics);
                metadata.Unit = metadata.Calibration.Unit;
            }
        }
    }

    internal static TiffByteOrder DetectTiffByteOrder(ReadOnlySpan<byte> fileBytes)
    {
        if (fileBytes.Length < 2)
        {
            return TiffByteOrder.Unknown;
        }

        if (fileBytes[0] == 0x49 && fileBytes[1] == 0x49)
        {
            return TiffByteOrder.LittleEndian;
        }

        if (fileBytes[0] == 0x4d && fileBytes[1] == 0x4d)
        {
            return TiffByteOrder.BigEndian;
        }

        return TiffByteOrder.Unknown;
    }

    internal static double ReadPixelSize(TiffIfdEntry entry, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (entry.DataType != TiffDataType.Rational || entry.Count != 1)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "ImageJ spatial calibration requires a single unsigned rational resolution value.", imageIndex, entry.TagId);
            return 0;
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, new TiffLimits());
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return 0;
        }

        TiffRationalCodecResult rationalResult = TiffRationalCodec.Decode(loadResult.Bytes, entry.DataType, byteOrder);
        diagnostics.Merge(rationalResult.Diagnostics);
        if (!rationalResult.Success || rationalResult.Values.Length == 0)
        {
            return 0;
        }

        double resolution = rationalResult.Values[0].ToDouble();
        if (resolution <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidRationalValue, "ImageJ spatial resolution must be positive.", imageIndex, entry.TagId);
            return 0;
        }

        return 1d / resolution;
    }

    internal static string ReadResolutionUnit(TiffIfdEntry entry, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (entry.DataType != TiffDataType.Short || entry.Count != 1)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "ImageJ resolution unit must contain one SHORT value.", imageIndex, entry.TagId);
            return "";
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, new TiffLimits());
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success || loadResult.Bytes.Length < 2)
        {
            return "";
        }

        ushort value = TiffBinaryReader.ReadUInt16(loadResult.Bytes.AsSpan(0, 2), byteOrder);
        if (value == (ushort)TiffResolutionUnit.Inch)
        {
            return "inch";
        }

        if (value == (ushort)TiffResolutionUnit.Centimeter)
        {
            return "cm";
        }

        return "";
    }

    internal static int FindEquals(string line)
    {
        int index = 0;
        while (index < line.Length)
        {
            if (line[index] == '=')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static string TrimLineEnd(string value)
    {
        string text = TiffImageJMetadataEntry.NormalizeString(value);
        if (text.Length > 0 && text[text.Length - 1] == '\r')
        {
            return text.Substring(0, text.Length - 1);
        }

        return text;
    }

    internal static string Trim(string value)
    {
        string text = TiffImageJMetadataEntry.NormalizeString(value);
        int start = 0;
        while (start < text.Length && char.IsWhiteSpace(text[start]))
        {
            start = start + 1;
        }

        int end = text.Length - 1;
        while (end >= start && char.IsWhiteSpace(text[end]))
        {
            end = end - 1;
        }

        if (end < start)
        {
            return "";
        }

        return text.Substring(start, end - start + 1);
    }

    internal static TiffImageJMetadataEntry[] AppendEntry(TiffImageJMetadataEntry[] entries, TiffImageJMetadataEntry entry)
    {
        TiffImageJMetadataEntry[] result = new TiffImageJMetadataEntry[entries.Length + 1];
        int index = 0;
        while (index < entries.Length)
        {
            result[index] = entries[index].Copy();
            index = index + 1;
        }

        result[index] = entry.Copy();
        return result;
    }
}
