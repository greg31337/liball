using System.Collections.Generic;

namespace image;

internal class IptcIimParser
{
    internal const int TagMarker = 28;
    internal const int StandardHeaderLength = 5;

    internal static List<MetadataDirectory> Parse(ImageMetadataDocument document, byte[] data, long baseOffset, string pathPrefix, string diagnosticPrefix, string blockType, ImageMetadataReaderOptions options)
    {
        List<MetadataDirectory> directories = new List<MetadataDirectory>();
        if (document == null)
        {
            return directories;
        }

        string prefix = diagnosticPrefix;
        if (prefix == null || prefix.Length == 0)
        {
            prefix = "iptc.iim";
        }

        string block = blockType;
        if (block == null || block.Length == 0)
        {
            block = "IPTC";
        }

        if (data == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, prefix + ".data.null", "IPTC IIM parser received null data.", block, baseOffset, 0);
            return directories;
        }

        if (data.Length > GetMaxSegmentBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, prefix + ".length.limit", "IPTC IIM data exceeded the configured segment limit.", block, baseOffset, data.Length);
            return directories;
        }

        int maximum = GetMaxTagCount(options);
        if (maximum < 0)
        {
            maximum = 0;
        }

        bool utf8 = false;
        int offset = 0;
        int tagCount = 0;
        while (offset < data.Length)
        {
            if (tagCount >= maximum)
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, prefix + ".dataset.count.limit", "IPTC IIM dataset count exceeded the configured tag limit.", block, baseOffset + offset, data.Length - offset);
                return directories;
            }

            if (data.Length - offset < StandardHeaderLength)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, prefix + ".dataset.truncated", "IPTC IIM dataset header was truncated.", block, baseOffset + offset, data.Length - offset);
                return directories;
            }

            if (data[offset] != TagMarker)
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, prefix + ".marker.invalid", "IPTC IIM dataset marker was invalid.", block, baseOffset + offset, 1);
                return directories;
            }

            int record = data[offset + 1];
            int dataset = data[offset + 2];
            int encodedLength = (data[offset + 3] << 8) | data[offset + 4];
            int headerLength = StandardHeaderLength;
            long valueLength = encodedLength;
            if ((encodedLength & 32768) != 0)
            {
                int descriptorLength = encodedLength & 32767;
                if (descriptorLength <= 0 || descriptorLength > 8)
                {
                    MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, prefix + ".extended.length.invalid", "IPTC IIM extended dataset length descriptor was invalid.", block, baseOffset + offset + 3, 2);
                    return directories;
                }

                if (descriptorLength > data.Length - offset - StandardHeaderLength)
                {
                    MetadataDiagnostic.AddTruncated(document.Diagnostics, prefix + ".extended.length.truncated", "IPTC IIM extended dataset length was truncated.", block, baseOffset + offset + StandardHeaderLength, descriptorLength);
                    return directories;
                }

                ulong extendedLength = 0;
                int lengthIndex = 0;
                while (lengthIndex < descriptorLength)
                {
                    extendedLength = (extendedLength << 8) | data[offset + StandardHeaderLength + lengthIndex];
                    lengthIndex = lengthIndex + 1;
                }

                if (extendedLength > 2147483647)
                {
                    MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, prefix + ".dataset.length.limit", "IPTC IIM dataset length exceeded the supported byte-array size.", block, baseOffset + offset, ClampLength(extendedLength));
                    return directories;
                }

                valueLength = (long)extendedLength;
                headerLength = StandardHeaderLength + descriptorLength;
            }

            int valueOffset = offset + headerLength;
            if (valueLength > data.Length - valueOffset)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, prefix + ".dataset.value.truncated", "IPTC IIM dataset value was truncated.", block, baseOffset + valueOffset, valueLength);
                return directories;
            }

            byte[] value = CopyRange(data, valueOffset, (int)valueLength);
            if (record == 1 && dataset == 90 && IsUtf8Designation(value))
            {
                utf8 = true;
            }

            MetadataDirectory directory = FindOrAddDirectory(document, directories, record, baseOffset, data.Length, pathPrefix);
            AddTag(document, directory, record, dataset, value, baseOffset + valueOffset, utf8, block);
            tagCount = tagCount + 1;
            offset = valueOffset + (int)valueLength;
        }

        return directories;
    }

    internal static MetadataDirectory FindOrAddDirectory(ImageMetadataDocument document, List<MetadataDirectory> directories, int record, long offset, long length, string pathPrefix)
    {
        string group = IptcDatasetNameTable.RecordGroup(record);
        foreach (MetadataDirectory existing in directories)
        {
            if (existing.Group == group)
            {
                return existing;
            }
        }

        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Iptc, group, IptcDatasetNameTable.RecordName(record));
        string prefix = pathPrefix == null ? "" : pathPrefix;
        directory.Path = prefix.Length == 0 ? "IPTC/" + group : prefix + "/" + group;
        directory.Offset = offset;
        directory.Length = length;
        directories.Add(directory);
        return directory;
    }

    internal static MetadataTag AddTag(ImageMetadataDocument document, MetadataDirectory directory, int record, int dataset, byte[] value, long offset, bool utf8, string blockType)
    {
        string name = IptcDatasetNameTable.NameFor(record, dataset);
        string key = "Iptc." + IptcDatasetNameTable.RecordGroup(record) + "." + name;
        MetadataValue decoded = DecodeValue(document, record, dataset, value, utf8, blockType);
        MetadataTag tag = directory.AddTagWithValue(key, name, decoded);
        tag.NumericId = dataset;
        tag.Offset = offset;
        tag.Length = value == null ? 0 : value.Length;
        tag.RawValue = value == null ? new byte[0] : value;
        tag.DisplayValue = record.ToString() + ":" + dataset.ToString();
        return tag;
    }

    internal static MetadataValue DecodeValue(ImageMetadataDocument document, int record, int dataset, byte[] value, bool utf8, string blockType)
    {
        if (record == 1 && dataset == 90)
        {
            if (IsUtf8Designation(value))
            {
                return MetadataValue.FromString("UTF-8");
            }

            return MetadataValue.FromString(MetadataStringReader.DecodeLatin1(value));
        }

        if (IptcDatasetNameTable.IsInteger(record, dataset))
        {
            if (value != null && value.Length <= 8)
            {
                return MetadataValue.FromUnsignedInteger(ReadUnsigned(value));
            }

            return MetadataValue.FromBytes(value == null ? new byte[0] : value);
        }

        if (IptcDatasetNameTable.IsBinary(record, dataset) || !IptcDatasetNameTable.IsKnown(record, dataset))
        {
            return MetadataValue.FromBytes(value == null ? new byte[0] : value);
        }

        string text;
        if (utf8)
        {
            text = MetadataStringReader.DecodeUtf8WithLatin1Fallback(value, document.Diagnostics, blockType);
        }
        else
        {
            text = MetadataStringReader.DecodeLatin1(value);
        }

        if (IptcDatasetNameTable.IsDate(record, dataset))
        {
            return MetadataValue.FromDateText(text);
        }

        return MetadataValue.FromString(text);
    }

    internal static ulong ReadUnsigned(byte[] value)
    {
        if (value == null || value.Length == 0 || value.Length > 8)
        {
            return 0;
        }

        ulong result = 0;
        foreach (byte item in value)
        {
            result = (result << 8) | item;
        }

        return result;
    }

    internal static byte[] CopyRange(byte[] data, int offset, int length)
    {
        if (data == null || offset < 0 || length <= 0 || offset > data.Length)
        {
            return new byte[0];
        }

        int actualLength = length;
        if (actualLength > data.Length - offset)
        {
            actualLength = data.Length - offset;
        }

        byte[] result = new byte[actualLength];
        int index = 0;
        while (index < actualLength)
        {
            result[index] = data[offset + index];
            index = index + 1;
        }

        return result;
    }

    internal static bool IsUtf8Designation(byte[] value)
    {
        if (value == null || value.Length < 3)
        {
            return false;
        }

        int index = 0;
        while (index <= value.Length - 3)
        {
            if (value[index] == 27 && value[index + 1] == 37 && value[index + 2] == 71)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static long ClampLength(ulong value)
    {
        return value > 9223372036854775807 ? 9223372036854775807 : (long)value;
    }

    internal static long GetMaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
    }

    internal static int GetMaxTagCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxTagCount : options.MaxTagCount;
    }
}
