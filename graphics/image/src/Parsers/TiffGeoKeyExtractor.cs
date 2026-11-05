using System.Collections.Generic;
using System.Globalization;

namespace image;

internal class TiffGeoKeyExtractor
{
    internal const int TagGeoKeyDirectory = 34735;
    internal const int TagGeoDoubleParams = 34736;
    internal const int TagGeoAsciiParams = 34737;

    internal static void Extract(ImageMetadataDocument document, MetadataDirectory sourceDirectory, List<MetadataTag> tags, bool littleEndian)
    {
        if (document == null || sourceDirectory == null || tags == null)
        {
            return;
        }

        MetadataTag? keyDirectoryTag = FindTag(tags, TagGeoKeyDirectory);
        if (keyDirectoryTag == null)
        {
            return;
        }

        byte[] raw = keyDirectoryTag.RawValue;
        if (TiffValueDecoder.ByteLength(raw) < 8)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "geotiff.key.directory.truncated", "GeoTIFF key directory header was truncated.", "GeoTIFF", keyDirectoryTag.Offset, keyDirectoryTag.Length);
            return;
        }

        ushort keyDirectoryVersion = TiffValueDecoder.ReadUnsigned16(raw, 0, littleEndian);
        ushort keyRevision = TiffValueDecoder.ReadUnsigned16(raw, 2, littleEndian);
        ushort minorRevision = TiffValueDecoder.ReadUnsigned16(raw, 4, littleEndian);
        ushort keyCount = TiffValueDecoder.ReadUnsigned16(raw, 6, littleEndian);
        int availableKeys = AvailableKeyCount(raw);
        if (availableKeys < keyCount)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "geotiff.key.directory.truncated", "GeoTIFF key directory entries were truncated.", "GeoTIFF", keyDirectoryTag.Offset, keyDirectoryTag.Length);
            keyCount = (ushort)availableKeys;
        }

        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Tiff, "GeoTIFF", "GeoTIFF Keys");
        directory.Path = "TIFF/GeoTIFF";
        directory.Offset = keyDirectoryTag.Offset;
        directory.Length = keyDirectoryTag.Length;
        directory.AddTagWithValue("Tiff.GeoTIFF.KeyDirectoryVersion", "Key Directory Version", MetadataValue.FromUnsignedInteger(keyDirectoryVersion));
        directory.AddTagWithValue("Tiff.GeoTIFF.KeyRevision", "Key Revision", MetadataValue.FromUnsignedInteger(keyRevision));
        directory.AddTagWithValue("Tiff.GeoTIFF.MinorRevision", "Minor Revision", MetadataValue.FromUnsignedInteger(minorRevision));
        directory.AddTagWithValue("Tiff.GeoTIFF.KeyCount", "Key Count", MetadataValue.FromUnsignedInteger(keyCount));

        MetadataTag? doubleParams = FindTag(tags, TagGeoDoubleParams);
        MetadataTag? asciiParams = FindTag(tags, TagGeoAsciiParams);

        int index = 0;
        while (index < keyCount)
        {
            int offset = 8 + index * 8;
            ushort keyId = TiffValueDecoder.ReadUnsigned16(raw, offset, littleEndian);
            ushort tagLocation = TiffValueDecoder.ReadUnsigned16(raw, offset + 2, littleEndian);
            ushort valueCount = TiffValueDecoder.ReadUnsigned16(raw, offset + 4, littleEndian);
            ushort valueOffset = TiffValueDecoder.ReadUnsigned16(raw, offset + 6, littleEndian);
            AddGeoKey(document, directory, keyId, tagLocation, valueCount, valueOffset, doubleParams, asciiParams, littleEndian, TiffValueDecoder.CopyBytes(raw, offset, 8));
            index = index + 1;
        }
    }

    internal static void AddGeoKey(ImageMetadataDocument document, MetadataDirectory directory, ushort keyId, ushort tagLocation, ushort valueCount, ushort valueOffset, MetadataTag? doubleParams, MetadataTag? asciiParams, bool littleEndian, byte[] rawKeyEntry)
    {
        MetadataValue value = GeoKeyValue(document, tagLocation, valueCount, valueOffset, doubleParams, asciiParams, littleEndian);
        string name = GeoKeyNameFor(keyId);
        MetadataTag tag = MetadataTag.Create("Tiff.GeoTIFF." + name, name, MetadataFamily.Tiff, value);
        tag.NumericId = keyId;
        tag.RawValue = rawKeyEntry;
        tag.DisplayValue = ImageMetadataFormatterCore.FormatValue(value);
        tag.Confidence = 100;
        directory.AddTag(tag);
    }

    internal static MetadataValue GeoKeyValue(ImageMetadataDocument document, ushort tagLocation, ushort valueCount, ushort valueOffset, MetadataTag? doubleParams, MetadataTag? asciiParams, bool littleEndian)
    {
        if (tagLocation == 0)
        {
            return MetadataValue.FromUnsignedInteger(valueOffset);
        }

        if (tagLocation == TagGeoDoubleParams)
        {
            return GeoDoubleValue(document, valueCount, valueOffset, doubleParams, littleEndian);
        }

        if (tagLocation == TagGeoAsciiParams)
        {
            return GeoAsciiValue(document, valueCount, valueOffset, asciiParams);
        }

        MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "geotiff.key.location.unsupported", "GeoTIFF key referenced an unsupported TIFF tag location.", "GeoTIFF", tagLocation, valueCount);
        return MetadataValue.FromUnsignedInteger(valueOffset);
    }

    internal static MetadataValue GeoDoubleValue(ImageMetadataDocument document, ushort valueCount, ushort valueOffset, MetadataTag? doubleParams, bool littleEndian)
    {
        if (doubleParams == null || doubleParams.RawValue == null)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "geotiff.double.params.missing", "GeoTIFF key referenced missing double parameters.", "GeoTIFF", valueOffset, valueCount);
            return new MetadataValue();
        }

        int byteOffset = valueOffset * 8;
        int byteCount = valueCount * 8;
        if (byteOffset < 0 || byteCount < 0 || byteOffset + byteCount > doubleParams.RawValue.Length)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "geotiff.double.params.truncated", "GeoTIFF double parameter range was truncated.", "GeoTIFF", valueOffset, valueCount);
            return new MetadataValue();
        }

        if (valueCount == 1)
        {
            return TiffValueDecoder.DecodeScalar(TiffValueDecoder.TypeDouble, doubleParams.RawValue, byteOffset, littleEndian);
        }

        return TiffValueDecoder.DecodeArray(TiffValueDecoder.TypeDouble, valueCount, TiffValueDecoder.CopyBytes(doubleParams.RawValue, byteOffset, byteCount), littleEndian);
    }

    internal static MetadataValue GeoAsciiValue(ImageMetadataDocument document, ushort valueCount, ushort valueOffset, MetadataTag? asciiParams)
    {
        if (asciiParams == null || asciiParams.RawValue == null)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "geotiff.ascii.params.missing", "GeoTIFF key referenced missing ASCII parameters.", "GeoTIFF", valueOffset, valueCount);
            return new MetadataValue();
        }

        if (valueOffset + valueCount > asciiParams.RawValue.Length)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "geotiff.ascii.params.truncated", "GeoTIFF ASCII parameter range was truncated.", "GeoTIFF", valueOffset, valueCount);
            return new MetadataValue();
        }

        string text = MetadataStringReader.DecodeAsciiRange(asciiParams.RawValue, valueOffset, valueCount, document.Diagnostics, "GeoTIFF");
        return MetadataValue.FromString(TrimGeoAscii(text));
    }

    internal static int AvailableKeyCount(byte[] raw)
    {
        int remaining = TiffValueDecoder.ByteLength(raw) - 8;
        if (remaining <= 0)
        {
            return 0;
        }

        return remaining / 8;
    }

    internal static string TrimGeoAscii(string text)
    {
        if (text == null)
        {
            return "";
        }

        int length = text.Length;
        while (length > 0 && (text[length - 1] == '\0' || text[length - 1] == '|'))
        {
            length = length - 1;
        }

        if (length == text.Length)
        {
            return text;
        }

        return text.Substring(0, length);
    }

    internal static MetadataTag? FindTag(List<MetadataTag> tags, int numericId)
    {
        foreach (MetadataTag tag in tags)
        {
            if (tag != null && tag.NumericId == numericId)
            {
                return tag;
            }
        }

        return null;
    }

    internal static string GeoKeyNameFor(int keyId)
    {
        switch (keyId)
        {
            case 1024:
                return "GTModelTypeGeoKey";
            case 1025:
                return "GTRasterTypeGeoKey";
            case 1026:
                return "GTCitationGeoKey";
            case 2048:
                return "GeographicTypeGeoKey";
            case 2049:
                return "GeogCitationGeoKey";
            case 2050:
                return "GeogGeodeticDatumGeoKey";
            case 2051:
                return "GeogPrimeMeridianGeoKey";
            case 2052:
                return "GeogLinearUnitsGeoKey";
            case 2054:
                return "GeogAngularUnitsGeoKey";
            case 2056:
                return "GeogEllipsoidGeoKey";
            case 2057:
                return "GeogSemiMajorAxisGeoKey";
            case 2059:
                return "GeogInvFlatteningGeoKey";
            case 3072:
                return "ProjectedCSTypeGeoKey";
            case 3073:
                return "PCSCitationGeoKey";
            case 3076:
                return "ProjLinearUnitsGeoKey";
            case 4096:
                return "VerticalCSTypeGeoKey";
            case 4097:
                return "VerticalCitationGeoKey";
            case 4099:
                return "VerticalUnitsGeoKey";
            default:
                return "GeoKey" + keyId.ToString(CultureInfo.InvariantCulture);
        }
    }
}
