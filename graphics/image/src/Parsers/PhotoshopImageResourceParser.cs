using System.Collections.Generic;

namespace image;

internal class PhotoshopImageResourceParser
{
    internal const int MinimumResourceLength = 12;

    internal static List<PhotoshopImageResource> Parse(ImageMetadataDocument document, BoundedStreamReader reader, long offset, long length, ImageMetadataReaderOptions options, string diagnosticPrefix, string blockType)
    {
        List<PhotoshopImageResource> resources = new List<PhotoshopImageResource>();
        if (document == null)
        {
            return resources;
        }

        string prefix = diagnosticPrefix;
        if (prefix == null || prefix.Length == 0)
        {
            prefix = "photoshop.irb";
        }

        string block = blockType;
        if (block == null || block.Length == 0)
        {
            block = "IRB";
        }

        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, prefix + ".reader.null", "Photoshop image resource parser received a null reader.", block, offset, length);
            return resources;
        }

        if (offset < 0 || length < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, prefix + ".range.invalid", "Photoshop image resource range was negative.", block, offset, length);
            return resources;
        }

        if (length > GetMaxSegmentBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, prefix + ".length.limit", "Photoshop image resource data exceeded the configured segment limit.", block, offset, length);
            return resources;
        }

        if (offset > long.MaxValue - length)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, prefix + ".range.overflow", "Photoshop image resource range overflowed.", block, offset, length);
            return resources;
        }

        long end = offset + length;
        long accessible = reader.AccessibleLength();
        if (offset > accessible)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, prefix + ".range.truncated", "Photoshop image resource range started beyond available data.", block, offset, length);
            return resources;
        }

        if (end > accessible)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, prefix + ".range.truncated", "Photoshop image resource range exceeded available data.", block, offset, length);
            end = accessible;
        }

        if (!reader.SeekAbsolute(offset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, prefix + ".seek", "Photoshop image resource range could not be reached.", block, offset, length);
            return resources;
        }

        int maximum = GetMaxTagCount(options);
        if (maximum < 0)
        {
            maximum = 0;
        }

        EndianBinaryReader endian = new EndianBinaryReader(reader, false);
        while (reader.Position < end)
        {
            if (resources.Count >= maximum)
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, prefix + ".count.limit", "Photoshop image resource count exceeded the configured tag limit.", block, reader.Position, end - reader.Position);
                return resources;
            }

            long resourceOffset = reader.Position;
            if (!CanRead(reader, end, MinimumResourceLength))
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, prefix + ".header.truncated", "Photoshop image resource block header was truncated.", block, resourceOffset, end - resourceOffset);
                return resources;
            }

            string signature = MetadataStringReader.DecodeLatin1(reader.ReadBytes(4));
            if (!IsResourceSignature(signature))
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, prefix + ".signature.invalid", "Photoshop image resource block signature was invalid.", block, resourceOffset, 4);
                return resources;
            }

            int resourceId = endian.ReadUnsigned16();
            int nameLength = reader.ReadByte();
            int nameStorageLength = PadEven(1 + nameLength);
            if (!CanRead(reader, end, nameStorageLength - 1 + 4))
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, prefix + ".name.truncated", "Photoshop image resource name was truncated.", block, reader.Position - 1, end - reader.Position + 1);
                return resources;
            }

            string name = MetadataStringReader.DecodeLatin1(reader.ReadBytes(nameLength));
            int namePadding = nameStorageLength - 1 - nameLength;
            if (namePadding > 0)
            {
                reader.SkipBytes(namePadding);
            }

            uint dataLength = endian.ReadUnsigned32();
            long dataOffset = reader.Position;
            PhotoshopImageResource resource = new PhotoshopImageResource();
            resource.Signature = signature;
            resource.ResourceId = resourceId;
            resource.Name = name;
            resource.ResourceOffset = resourceOffset;
            resource.DataOffset = dataOffset;
            resource.DeclaredDataLength = dataLength;
            resource.NameStorageLength = nameStorageLength;
            resource.ResourceLength = dataOffset - resourceOffset + PadEven((long)dataLength);
            resources.Add(resource);

            if (!CanRead(reader, end, dataLength))
            {
                resource.Truncated = true;
                resource.ResourceLength = end - resourceOffset;
                MetadataDiagnostic.AddTruncated(document.Diagnostics, prefix + ".data.truncated", "Photoshop image resource data was truncated.", block, dataOffset, dataLength);
                return resources;
            }

            if (dataLength > 2147483647)
            {
                resource.Truncated = true;
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, prefix + ".data.limit", "Photoshop image resource payload was too large to retain.", block, dataOffset, dataLength);
                return resources;
            }

            resource.Data = reader.ReadBytes((int)dataLength);
            if ((dataLength & 1) != 0)
            {
                if (!CanRead(reader, end, 1))
                {
                    resource.Truncated = true;
                    resource.ResourceLength = end - resourceOffset;
                    MetadataDiagnostic.AddTruncated(document.Diagnostics, prefix + ".padding.truncated", "Photoshop image resource data padding was truncated.", block, reader.Position, 1);
                    return resources;
                }

                reader.SkipBytes(1);
            }
        }

        return resources;
    }

    internal static MetadataDirectory AddDirectory(ImageMetadataDocument document, MetadataDirectory? parent, PhotoshopImageResource resource, string path)
    {
        if (document == null || resource == null)
        {
            return new MetadataDirectory();
        }

        MetadataDirectory directory;
        if (parent == null)
        {
            directory = document.AddDirectoryWithName(MetadataFamily.Photoshop, "IRB", ResourceName(resource.ResourceId));
        }
        else
        {
            directory = parent.AddChildWithName(MetadataFamily.Photoshop, "IRB", ResourceName(resource.ResourceId));
        }

        directory.Path = path == null ? "" : path;
        directory.Offset = resource.ResourceOffset;
        directory.Length = resource.ResourceLength;
        AddTag(directory, "Photoshop.IRB.Signature", "Signature", MetadataValue.FromString(resource.Signature), resource.ResourceOffset, 4, "");
        MetadataTag identifier = AddTag(directory, "Photoshop.IRB.ResourceId", "Resource ID", MetadataValue.FromInteger(resource.ResourceId), resource.ResourceOffset + 4, 2, ResourceName(resource.ResourceId));
        identifier.NumericId = resource.ResourceId;
        AddTag(directory, "Photoshop.IRB.Name", "Name", MetadataValue.FromString(resource.Name), resource.ResourceOffset + 6, resource.NameStorageLength, "");
        AddTag(directory, "Photoshop.IRB.DataOffset", "Data Offset", MetadataValue.FromInteger(resource.DataOffset), resource.DataOffset - 4, 4, "");
        AddTag(directory, "Photoshop.IRB.DataByteLength", "Data Byte Length", MetadataValue.FromInteger(resource.DeclaredDataLength), resource.DataOffset - 4, 4, "");
        AddTag(directory, "Photoshop.IRB.Truncated", "Truncated", MetadataValue.FromBoolean(resource.Truncated), resource.ResourceOffset, resource.ResourceLength, "");
        return directory;
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string displayValue)
    {
        MetadataTag tag = directory.AddTagWithValue(key, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = displayValue;
        return tag;
    }

    internal static bool IsResourceSignature(string signature)
    {
        return signature == "8BIM" || signature == "8B64";
    }

    internal static bool CanRead(BoundedStreamReader reader, long end, long length)
    {
        if (reader == null || length < 0 || reader.Position > end)
        {
            return false;
        }

        return length <= end - reader.Position && reader.CanRead(length);
    }

    internal static int PadEven(int value)
    {
        return (value & 1) == 0 ? value : value + 1;
    }

    internal static long PadEven(long value)
    {
        return (value & 1) == 0 ? value : value + 1;
    }

    internal static string ResourceName(int resourceId)
    {
        if (resourceId == 1005) return "Resolution Info";
        if (resourceId == 1028) return "IPTC-NAA";
        if (resourceId == 1033) return "Photoshop 4 Thumbnail";
        if (resourceId == 1034) return "Copyright Flag";
        if (resourceId == 1035) return "URL";
        if (resourceId == 1036) return "Thumbnail";
        if (resourceId == 1037) return "Global Angle";
        if (resourceId == 1039) return "ICC Profile";
        if (resourceId == 1041) return "ICC Untagged Profile";
        if (resourceId == 1045) return "Unicode Alpha Names";
        if (resourceId == 1046) return "Indexed Color Table Count";
        if (resourceId == 1047) return "Transparency Index";
        if (resourceId == 1057) return "Version Info";
        if (resourceId == 1058) return "EXIF Data 1";
        if (resourceId == 1059) return "EXIF Data 3";
        if (resourceId == 1060) return "XMP Metadata";
        if (resourceId == 1061) return "Caption Digest";
        if (resourceId == 1062) return "Print Scale";
        if (resourceId == 1064) return "Pixel Aspect Ratio";
        if (resourceId == 1069) return "Layer Selection IDs";
        if (resourceId == 1077) return "Display Info";
        if (resourceId >= 2000 && resourceId <= 2997) return "Path Information";
        if (resourceId == 2999) return "Clipping Path Name";
        if (resourceId >= 4000 && resourceId <= 4999) return "Plug-In Resource";
        return "Image Resource " + resourceId.ToString();
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
