using System.Collections.Generic;

namespace image;

internal class Cr3MetadataExtractor
{
    internal const int TiffHeaderLength = 8;
    internal const int ThumbnailHeaderLength = 16;
    internal const int PreviewHeaderLength = 32;
    internal const string CanonMetadataUuidHex = "85c0b687820f11e08111f4ce462b6a48";
    internal const string CanonEditUuidHex = "210f1687914911e4811100242131fce4";
    internal const string XmpUuidHex = "be7acfcb97a942e89c71999491e3afac";
    internal const string PreviewUuidHex = "eaf42b5e1c984b88b9fbb7dc406e4d16";

    internal static void Extract(ImageMetadataDocument document, BoundedStreamReader reader, List<BmffBox> boxes, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || boxes == null)
        {
            return;
        }

        List<BmffBox> metadataBoxes = new List<BmffBox>();
        CollectMetadataBoxes(boxes, metadataBoxes);
        AddProfileDirectory(document, metadataBoxes);

        foreach (BmffBox box in metadataBoxes)
        {
            if (IsCmtType(box.Type))
            {
                ExtractCmtBox(document, reader, box, options);
            }
            else if (box.Type == "THMB")
            {
                ExtractThumbnailBox(document, reader, box, options);
            }
            else if (box.Type == "uuid" && IsUuid(box.UserType, XmpUuidHex))
            {
                ExtractXmpUuid(document, reader, box, options);
            }
            else if (box.Type == "uuid" && IsUuid(box.UserType, PreviewUuidHex))
            {
                ExtractPreviewUuid(document, reader, box, options);
            }
        }
    }

    internal static void CollectMetadataBoxes(List<BmffBox> boxes, List<BmffBox> result)
    {
        if (boxes == null || result == null)
        {
            return;
        }

        foreach (BmffBox box in boxes)
        {
            if (box == null)
            {
                continue;
            }

            if (IsCmtType(box.Type)
                || box.Type == "THMB"
                || box.Type == "uuid" && (IsUuid(box.UserType, XmpUuidHex) || IsUuid(box.UserType, PreviewUuidHex)))
            {
                result.Add(box);
            }

            CollectMetadataBoxes(box.Children, result);
        }
    }

    internal static void ExtractCmtBox(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, ImageMetadataReaderOptions options)
    {
        if (box.Type == "CMT3" && !PreserveMakerNotes(options))
        {
            return;
        }

        if (box.DataLimited)
        {
            return;
        }

        long length = BmffBoxTraverser.ToDiagnosticLength(box.DataSize);
        if (length < TiffHeaderLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "cr3.cmt.tiff.truncated", "CR3 Canon metadata TIFF header was truncated.", box.Type, box.DataOffset, TiffHeaderLength);
            return;
        }

        byte[] prefix = BmffMetadataExtractor.ReadRange(document, reader, box.DataOffset, TiffHeaderLength, TiffHeaderLength, "cr3.cmt.tiff.header.limit", box.Type);
        if (!JpegExifExtractor.HasTiffHeader(prefix))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "cr3.cmt.tiff.header.invalid", "CR3 Canon metadata box did not contain a recognized TIFF header.", box.Type, box.DataOffset, TiffHeaderLength);
            CaptureRawBlock(document, reader, box, options, box.Type + " Metadata");
            return;
        }

        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        if (!reader.SeekAbsolute(box.DataOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "cr3.cmt.seek", "CR3 Canon metadata box could not be reached.", box.Type, box.DataOffset, length);
            return;
        }

        long limit = length;
        if (reader.RemainingLimit() < limit)
        {
            limit = reader.RemainingLimit();
        }

        BoundedStreamReader embeddedReader = new BoundedStreamReader(reader.Source, length, limit, diagnostics);
        ImageMetadataDocument embedded = TiffMetadataParser.Parse(embeddedReader, ImageFormatMapping.CreateInfo(ImageFormatKind.Tiff), options);
        RawTiffProfileParser.RelocateDocument(embedded, box.DataOffset);
        TransformEmbeddedDirectories(embedded, box.Type);
        MergeEmbeddedDocument(document, embedded);
        AddCmtProfile(document, reader, box, options);
        CaptureRawBlock(document, reader, box, options, box.Type + " Metadata");
    }

    internal static void TransformEmbeddedDirectories(ImageMetadataDocument embedded, string type)
    {
        if (embedded == null)
        {
            return;
        }

        foreach (MetadataDirectory directory in embedded.Directories)
        {
            directory.Path = "BMFF/CR3/" + type + "/" + directory.Path;
            if (directory.Group == "Header")
            {
                TransformDirectory(directory, MetadataFamily.Raw, type + "Header", "CR3 " + type + " TIFF Header", type);
            }
            else if (directory.Group == "IFD0")
            {
                if (type == "CMT1")
                {
                    directory.Name = "CR3 IFD0";
                }
                else if (type == "CMT2")
                {
                    TransformDirectory(directory, MetadataFamily.Exif, "ExifIFD", "CR3 EXIF IFD", type);
                }
                else if (type == "CMT3")
                {
                    TransformDirectory(directory, MetadataFamily.MakerNote, "MakerNoteCanon", "CR3 Canon MakerNote", type);
                }
                else if (type == "CMT4")
                {
                    TransformDirectory(directory, MetadataFamily.Gps, "GPS", "CR3 GPS IFD", type);
                }
            }
        }
    }

    internal static void TransformDirectory(MetadataDirectory directory, MetadataFamily family, string group, string name, string type)
    {
        directory.Family = family;
        directory.Group = group;
        directory.Name = name;
        foreach (MetadataTag tag in directory.Tags)
        {
            tag.Family = family;
            tag.Group = group;
            string keyName = TiffTagNameTable.KeyNameFor(family, group, tag.NumericId);
            string displayName = TiffTagNameTable.DisplayNameFor(family, group, tag.NumericId);
            if (family == MetadataFamily.MakerNote)
            {
                keyName = MakerNoteNames.KeyNameFor("Canon", tag.NumericId);
                displayName = MakerNoteNames.DisplayNameFor("Canon", tag.NumericId);
            }

            if (family == MetadataFamily.Raw)
            {
                keyName = HeaderKeyName(tag.Key);
                displayName = tag.Name;
            }

            tag.Key = PrefixForFamily(family) + "." + group + "." + keyName;
            tag.Name = displayName;
        }

        foreach (MetadataDirectory child in directory.Children)
        {
            child.Path = "BMFF/CR3/" + type + "/" + child.Path;
        }
    }

    internal static void MergeEmbeddedDocument(ImageMetadataDocument document, ImageMetadataDocument embedded)
    {
        foreach (MetadataDirectory directory in embedded.Directories)
        {
            document.AddDirectory(directory);
        }

        foreach (MetadataProfile profile in embedded.Profiles)
        {
            document.AddProfile(profile);
        }

        foreach (MetadataRawBlock block in embedded.RawBlocks)
        {
            document.AddRawBlock(block);
        }

        foreach (MetadataDiagnostic diagnostic in embedded.Diagnostics)
        {
            document.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddCmtProfile(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, ImageMetadataReaderOptions options)
    {
        MetadataProfileKind kind = box.Type == "CMT3" ? MetadataProfileKind.MakerNote : MetadataProfileKind.Exif;
        MetadataFamily family = FamilyForCmt(box.Type);
        string prefix = box.Type == "CMT3" ? "MakerNote" : "Exif";
        MetadataProfile profile = BmffMetadataExtractor.CreateProfile(kind, family, prefix + ".CR3." + box.Type, "CR3 " + box.Type, "image/tiff", box.DataOffset, BmffBoxTraverser.ToDiagnosticLength(box.DataSize), new byte[0]);
        long maximum = MaxProfileBytes(options);
        if (maximum >= 0 && box.DataSize <= (ulong)maximum && box.DataSize <= 2147483647)
        {
            profile.Data = BmffMetadataExtractor.ReadRange(document, reader, box.DataOffset, profile.Length, MaxProfileBytes(options), "cr3.cmt.profile.limit", box.Type);
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "cr3.cmt.profile.limit", "CR3 Canon metadata profile exceeded the configured profile limit.", box.Type, box.DataOffset, profile.Length);
        }

        document.AddProfile(profile);
    }

    internal static void ExtractXmpUuid(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, ImageMetadataReaderOptions options)
    {
        if (!ParseXmp(options))
        {
            return;
        }

        long length = BmffBoxTraverser.ToDiagnosticLength(box.DataSize);
        byte[] data = BmffMetadataExtractor.ReadRange(document, reader, box.DataOffset, length, MaxXmlBytes(options), "cr3.xmp.profile.limit", "uuid");
        MetadataProfile profile = BmffMetadataExtractor.CreateProfile(MetadataProfileKind.Xmp, MetadataFamily.Xmp, "Xmp.CR3.uuid", "CR3 XMP", "application/rdf+xml", box.DataOffset, length, data);
        if (data.Length > 0 || length == 0)
        {
            profile.Text = System.Text.Encoding.UTF8.GetString(data);
        }
        document.AddProfile(profile);
        if (data.Length > 0 && !MetadataStringReader.IsValidUtf8(data))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "cr3.xmp.utf8.invalid", "CR3 XMP UUID was not valid UTF-8.", "uuid", box.DataOffset, length);
        }
        XmpPacketParser.ParseProfile(document, profile, options, "BMFF/CR3/XMP");
        CaptureRawBlock(document, reader, box, options, "CR3 XMP UUID");
    }

    internal static void ExtractThumbnailBox(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, ImageMetadataReaderOptions options)
    {
        long length = BmffBoxTraverser.ToDiagnosticLength(box.DataSize);
        if (length < ThumbnailHeaderLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "cr3.thumbnail.header.truncated", "CR3 thumbnail header was truncated.", "THMB", box.DataOffset, ThumbnailHeaderLength);
            return;
        }

        byte[] header = BmffMetadataExtractor.ReadRange(document, reader, box.DataOffset, ThumbnailHeaderLength, ThumbnailHeaderLength, "cr3.thumbnail.header.limit", "THMB");
        int width = ReadUnsigned16BigEndian(header, 4);
        int height = ReadUnsigned16BigEndian(header, 6);
        long imageLength = length - ThumbnailHeaderLength;
        AddPreviewDirectory(document, "THMB", "CR3 Thumbnail", box.DataOffset, length, width, height, ThumbnailHeaderLength, imageLength);
        AddPreviewProfile(document, reader, box, options, ThumbnailHeaderLength, imageLength, "Thumbnail.CR3.THMB", "CR3 Thumbnail");
        CaptureRawBlock(document, reader, box, options, "CR3 Thumbnail Box");
    }

    internal static void ExtractPreviewUuid(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, ImageMetadataReaderOptions options)
    {
        long length = BmffBoxTraverser.ToDiagnosticLength(box.DataSize);
        if (length < PreviewHeaderLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "cr3.preview.header.truncated", "CR3 preview header was truncated.", "uuid", box.DataOffset, PreviewHeaderLength);
            return;
        }

        byte[] header = BmffMetadataExtractor.ReadRange(document, reader, box.DataOffset, PreviewHeaderLength, PreviewHeaderLength, "cr3.preview.header.limit", "uuid");
        if (MetadataStringReader.DecodeLatin1Range(header, 12, 4, null, "") != "PRVW")
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "cr3.preview.signature.invalid", "CR3 preview UUID did not contain a PRVW signature.", "uuid", box.DataOffset + 12, 4);
            return;
        }

        int width = ReadUnsigned16BigEndian(header, 22);
        int height = ReadUnsigned16BigEndian(header, 24);
        uint declaredLength = BmffBoxTraverser.ReadUnsigned32(CopyRange(header, 28, 4));
        long available = length - PreviewHeaderLength;
        long imageLength = declaredLength;
        if (imageLength > available)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "cr3.preview.data.truncated", "CR3 preview image exceeded its UUID payload.", "PRVW", box.DataOffset + PreviewHeaderLength, imageLength);
            imageLength = available;
        }

        AddPreviewDirectory(document, "PRVW", "CR3 Preview", box.DataOffset, length, width, height, PreviewHeaderLength, imageLength);
        AddPreviewProfile(document, reader, box, options, PreviewHeaderLength, imageLength, "Thumbnail.CR3.PRVW", "CR3 Preview");
        CaptureRawBlock(document, reader, box, options, "CR3 Preview UUID");
    }

    internal static void AddPreviewDirectory(ImageMetadataDocument document, string group, string name, long offset, long length, int width, int height, int dataOffset, long dataLength)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Thumbnail, group, name);
        directory.Path = "BMFF/CR3/" + group;
        directory.Offset = offset;
        directory.Length = length;
        directory.AddTagWithValue("Thumbnail.CR3." + group + ".Width", "Width", MetadataValue.FromInteger(width));
        directory.AddTagWithValue("Thumbnail.CR3." + group + ".Height", "Height", MetadataValue.FromInteger(height));
        directory.AddTagWithValue("Thumbnail.CR3." + group + ".DataOffset", "Data Offset", MetadataValue.FromInteger(offset + dataOffset));
        directory.AddTagWithValue("Thumbnail.CR3." + group + ".DataLength", "Data Length", MetadataValue.FromInteger(dataLength));
    }

    internal static void AddPreviewProfile(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, ImageMetadataReaderOptions options, int relativeOffset, long length, string key, string name)
    {
        MetadataProfile profile = BmffMetadataExtractor.CreateProfile(MetadataProfileKind.Thumbnail, MetadataFamily.Thumbnail, key, name, "image/jpeg", box.DataOffset + relativeOffset, length, new byte[0]);
        profile.Role = name == "CR3 Preview" ? ImageMetadataRole.Preview : ImageMetadataRole.Thumbnail;
        if (length <= MaxProfileBytes(options))
        {
            profile.Data = BmffMetadataExtractor.ReadRange(document, reader, profile.Offset, length, MaxProfileBytes(options), "cr3.preview.profile.limit", box.Type);
        }
        else
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "cr3.preview.profile.limit", "CR3 preview profile exceeded the configured profile limit.", box.Type, profile.Offset, length);
        }
        document.AddProfile(profile);
    }

    internal static void CaptureRawBlock(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, ImageMetadataReaderOptions options, string name)
    {
        if (!CaptureRawBlocks(options))
        {
            return;
        }

        long length = BmffBoxTraverser.ToDiagnosticLength(box.DataSize);
        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Raw, box.Type, name, box.DataOffset, length);
        if (!CaptureRawBlockData(options))
        {
            return;
        }

        if (length > MaxRawBlockBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "cr3.raw.block.limit", "CR3 metadata block exceeded the configured raw block limit.", box.Type, box.DataOffset, length);
            return;
        }

        block.Data = BmffMetadataExtractor.ReadRange(document, reader, box.DataOffset, length, MaxRawBlockBytes(options), "cr3.raw.block.limit", box.Type);
    }

    internal static void AddProfileDirectory(ImageMetadataDocument document, List<BmffBox> boxes)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Raw, "Profile", "CR3 RAW Profile");
        directory.Path = "Raw/Profile";
        directory.Offset = 0;
        directory.Length = document.ByteLength;
        directory.AddTagWithValue("Raw.Profile.Format", "RAW Format", MetadataValue.FromString("CR3"));
        directory.AddTagWithValue("Raw.Profile.Container", "Container", MetadataValue.FromString("ISO BMFF"));
        directory.AddTagWithValue("Raw.Profile.HeaderVariant", "Header Variant", MetadataValue.FromString("CR3"));
        directory.AddTagWithValue("Raw.Profile.MetadataBoxCount", "Metadata Box Count", MetadataValue.FromInteger(boxes.Count));
    }

    internal static bool IsCanonMetadataUuid(byte[] userType)
    {
        return IsUuid(userType, CanonMetadataUuidHex) || IsUuid(userType, CanonEditUuidHex);
    }

    internal static bool IsUuid(byte[] bytes, string hex)
    {
        return bytes != null && bytes.Length == 16 && HashHelper.ToHex(bytes) == hex;
    }

    internal static bool IsCmtType(string type)
    {
        return type == "CMT1" || type == "CMT2" || type == "CMT3" || type == "CMT4";
    }

    internal static MetadataFamily FamilyForCmt(string type)
    {
        if (type == "CMT2")
        {
            return MetadataFamily.Exif;
        }
        if (type == "CMT3")
        {
            return MetadataFamily.MakerNote;
        }
        if (type == "CMT4")
        {
            return MetadataFamily.Gps;
        }
        return MetadataFamily.Tiff;
    }

    internal static string PrefixForFamily(MetadataFamily family)
    {
        if (family == MetadataFamily.Exif)
        {
            return "Exif";
        }
        if (family == MetadataFamily.Gps)
        {
            return "Gps";
        }
        if (family == MetadataFamily.MakerNote)
        {
            return "MakerNote";
        }
        if (family == MetadataFamily.Raw)
        {
            return "Raw";
        }
        return "Tiff";
    }

    internal static string HeaderKeyName(string key)
    {
        if (key == null)
        {
            return "";
        }
        int index = key.LastIndexOf('.');
        return index < 0 ? key : key.Substring(index + 1);
    }

    internal static int ReadUnsigned16BigEndian(byte[] bytes, int offset)
    {
        if (bytes == null || offset < 0 || offset + 2 > bytes.Length)
        {
            return 0;
        }
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    internal static byte[] CopyRange(byte[] bytes, int offset, int count)
    {
        byte[] result = new byte[count];
        int index = 0;
        while (index < count)
        {
            result[index] = bytes[offset + index];
            index = index + 1;
        }
        return result;
    }

    internal static long MaxProfileBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxProfileBytes : options.MaxProfileBytes;
    }
    internal static long MaxXmlBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxXmlBytes : options.MaxXmlBytes;
    }
    internal static long MaxRawBlockBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxRawBlockBytes : options.MaxRawBlockBytes;
    }
    internal static bool CaptureRawBlocks(ImageMetadataReaderOptions options)
    {
        return options == null || options.CaptureRawBlocks;
    }
    internal static bool CaptureRawBlockData(ImageMetadataReaderOptions options)
    {
        return options != null && options.CaptureRawBlockData;
    }
    internal static bool PreserveMakerNotes(ImageMetadataReaderOptions options)
    {
        return options == null || options.PreserveMakerNotes;
    }
    internal static bool ParseXmp(ImageMetadataReaderOptions options)
    {
        return options == null || options.ParseXmp;
    }
}
