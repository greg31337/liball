using System;
using System.Globalization;
using System.Text;

namespace avif;

internal static class XmpReconciler
{
    internal static string GetExifValue(
        AvifExif exif,
        string namespaceUri,
        string name)
    {
        if (exif == null || exif.GetCode() != AvifResultCode.Success
            || namespaceUri == null || name == null)
        {
            return string.Empty;
        }
        int identifier = GetTagIdentifier(namespaceUri, name);
        if (identifier == 0)
        {
            return string.Empty;
        }
        ExifParsedData? parsed = exif.GetParsedData();
        int index = 0;
        while (index < (parsed ?? throw new InvalidOperationException("Invalid internal state.")).Tags.Count)
        {
            ExifTag tag = parsed.Tags[index];
            if (tag.Identifier == identifier)
            {
                return ReadValue(parsed, tag);
            }
            index++;
        }
        return string.Empty;
    }

    internal static bool ValuesConflict(
        string xmpValue,
        string exifValue)
    {
        return xmpValue != null && exifValue != null
            && xmpValue.Length != 0 && exifValue.Length != 0
            && xmpValue.Trim() != exifValue.Trim();
    }

    static int GetTagIdentifier(string namespaceUri, string name)
    {
        if (namespaceUri == AvifXmp.TiffNamespace)
        {
            if (name == "ImageWidth")
            {
                return 256;
            }
            if (name == "ImageLength")
            {
                return 257;
            }
            if (name == "Make")
            {
                return 271;
            }
            if (name == "Model")
            {
                return 272;
            }
            if (name == "Orientation")
            {
                return 274;
            }
            if (name == "Software")
            {
                return 305;
            }
            if (name == "DateTime")
            {
                return 306;
            }
        }
        if (namespaceUri == AvifXmp.ExifNamespace)
        {
            if (name == "ExposureTime")
            {
                return 33434;
            }
            if (name == "FNumber")
            {
                return 33437;
            }
            if (name == "DateTimeOriginal")
            {
                return 36867;
            }
            if (name == "DateTimeDigitized")
            {
                return 36868;
            }
            if (name == "PixelXDimension")
            {
                return 40962;
            }
            if (name == "PixelYDimension")
            {
                return 40963;
            }
        }
        return 0;
    }

    static string ReadValue(ExifParsedData parsed, ExifTag tag)
    {
        if (tag.Type == 2)
        {
            int length = tag.ValueLength;
            if (length != 0
                && parsed.Data[tag.ValueOffset + length - 1] == 0)
            {
                length--;
            }
            return Encoding.ASCII.GetString(
                parsed.Data, tag.ValueOffset, length);
        }
        if (tag.Count != 1)
        {
            return string.Empty;
        }
        if (tag.Type == 3)
        {
            return ExifParser.ReadUInt16(
                parsed.Data, tag.ValueOffset, parsed.LittleEndian)
                .ToString(CultureInfo.InvariantCulture);
        }
        if (tag.Type == 4)
        {
            return ExifParser.ReadUInt32(
                parsed.Data, tag.ValueOffset, parsed.LittleEndian)
                .ToString(CultureInfo.InvariantCulture);
        }
        if (tag.Type == 8)
        {
            short value = IntegerConversion.ToInt16(
                ExifParser.ReadUInt16(
                    parsed.Data, tag.ValueOffset, parsed.LittleEndian));
            return value.ToString(CultureInfo.InvariantCulture);
        }
        if (tag.Type == 9)
        {
            int value = IntegerConversion.ToInt32(
                ExifParser.ReadUInt32(
                    parsed.Data, tag.ValueOffset, parsed.LittleEndian));
            return value.ToString(CultureInfo.InvariantCulture);
        }
        if (tag.Type == 5 || tag.Type == 10)
        {
            uint numeratorValue = ExifParser.ReadUInt32(
                parsed.Data, tag.ValueOffset, parsed.LittleEndian);
            uint denominatorValue = ExifParser.ReadUInt32(
                parsed.Data, tag.ValueOffset + 4, parsed.LittleEndian);
            double value = tag.Type == 5
                ? numeratorValue / (double)denominatorValue
                : (int)numeratorValue / (double)(int)denominatorValue;
            return value.ToString("G17", CultureInfo.InvariantCulture);
        }
        return string.Empty;
    }
}
