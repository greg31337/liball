using System.Collections.Generic;

namespace image;

internal class TiffMakerNoteExtractor
{
    internal const int TagMakerNote = 37500;

    internal static void Extract(ImageMetadataDocument document, MetadataDirectory sourceDirectory, List<MetadataTag> tags, bool littleEndian, ImageMetadataReaderOptions options)
    {
        if (document == null || sourceDirectory == null || tags == null)
        {
            return;
        }

        if (!PreserveMakerNotes(options))
        {
            return;
        }

        int index = NextMakerNoteIndex(document);
        foreach (MetadataTag tag in tags)
        {
            if (IsMakerNoteTag(tag))
            {
                AddMakerNote(document, sourceDirectory, tag, index, littleEndian, options);
                index = index + 1;
            }
        }
    }

    internal static bool IsMakerNoteTag(MetadataTag tag)
    {
        return tag != null && tag.NumericId == TagMakerNote;
    }

    internal static void AddMakerNote(ImageMetadataDocument document, MetadataDirectory sourceDirectory, MetadataTag tag, int index, bool littleEndian, ImageMetadataReaderOptions options)
    {
        byte[] data = tag.RawValue;
        if (data == null)
        {
            data = new byte[0];
        }

        string group = "MakerNote" + index.ToString();
        string prefix = "MakerNote." + group;

        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.MakerNote, group, "MakerNote");
        directory.Path = "TIFF/" + group;
        directory.Offset = tag.Offset;
        directory.Length = tag.Length;
        directory.RepeatIndex = index;
        directory.AddTagWithValue(prefix + ".SourceFamily", "Source Family", MetadataValue.FromString(sourceDirectory.Family.ToString()));
        directory.AddTagWithValue(prefix + ".SourceGroup", "Source Group", MetadataValue.FromString(sourceDirectory.Group));
        directory.AddTagWithValue(prefix + ".SourceTag", "Source Tag", MetadataValue.FromString(tag.Key));
        directory.AddTagWithValue(prefix + ".Offset", "Offset", MetadataValue.FromInteger(tag.Offset));
        directory.AddTagWithValue(prefix + ".ByteLength", "Byte Length", MetadataValue.FromInteger(tag.Length));
        directory.AddTagWithValue(prefix + ".Data", "Data", MetadataValue.FromBytes(data));

        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.MakerNote;
        profile.Family = MetadataFamily.MakerNote;
        profile.Key = prefix;
        profile.Name = "MakerNote";
        profile.MimeType = "application/octet-stream";
        profile.Offset = tag.Offset;
        profile.Length = tag.Length;
        profile.Data = data;
        document.AddProfile(profile);

        AddRawBlock(document, tag.Offset, tag.Length, data, options);
        DecodeSelectedMakerNote(document, directory, data, littleEndian, options);
    }

    internal static void DecodeSelectedMakerNote(ImageMetadataDocument document, MetadataDirectory directory, byte[] data, bool sourceLittleEndian, ImageMetadataReaderOptions options)
    {
        if (!DecodeMakerNotes(options))
        {
            return;
        }

        string vendor = DetectVendor(document, data);
        if (vendor.Length > 0)
        {
            AddSummaryTag(directory, "Vendor", "Vendor", MetadataValue.FromString(vendor));
        }

        int decodedCount = 0;
        string format = "";

        if (IsNikonTiffMakerNote(data))
        {
            bool littleEndian = IsLittleEndianTiffHeader(data, 10);
            uint firstIfdOffset = TiffValueDecoder.ReadUnsigned32(data, 14, littleEndian);
            decodedCount = DecodeClassicIfd(document, directory, vendor, data, 10 + (long)firstIfdOffset, 10, littleEndian, options);
            format = "Nikon TIFF IFD";
        }
        else if (StartsWithAscii(data, "FUJIFILM", 0) && data.Length >= 12)
        {
            uint firstIfdOffset = TiffValueDecoder.ReadUnsigned32(data, 8, true);
            decodedCount = DecodeClassicIfd(document, directory, "Fujifilm", data, firstIfdOffset, 0, true, options);
            vendor = "Fujifilm";
            format = "Fujifilm IFD";
        }
        else if (StartsWithAscii(data, "OLYMPUS", 0) && data.Length >= 10)
        {
            decodedCount = DecodeClassicIfd(document, directory, "Olympus", data, 8, 0, true, options);
            vendor = "Olympus";
            format = "Olympus IFD";
        }
        else if (StartsWithAscii(data, "OLYMP", 0) && data.Length >= 8)
        {
            decodedCount = DecodeClassicIfd(document, directory, "Olympus", data, 6, 0, true, options);
            vendor = "Olympus";
            format = "Olympus IFD";
        }
        else if (StartsWithAscii(data, "Panasonic", 0) && data.Length >= 14)
        {
            decodedCount = DecodeClassicIfd(document, directory, "Panasonic", data, 12, 0, true, options);
            vendor = "Panasonic";
            format = "Panasonic IFD";
        }
        else if (CanDecodeDirectIfdVendor(vendor))
        {
            decodedCount = DecodeClassicIfd(document, directory, vendor, data, 0, 0, sourceLittleEndian, options);
            format = vendor + " IFD";
        }

        if (decodedCount > 0)
        {
            AddSummaryTag(directory, "Format", "Format", MetadataValue.FromString(format));
            AddSummaryTag(directory, "DecodedTagCount", "Decoded Tag Count", MetadataValue.FromInteger(decodedCount));
        }
    }

    internal static int DecodeClassicIfd(ImageMetadataDocument document, MetadataDirectory directory, string vendor, byte[] data, long ifdOffset, long valueBase, bool littleEndian, ImageMetadataReaderOptions options)
    {
        if (document == null || directory == null || data == null)
        {
            return 0;
        }

        if (ifdOffset < 0 || ifdOffset + 2 > data.Length)
        {
            return 0;
        }

        ushort tagCount = TiffValueDecoder.ReadUnsigned16(data, (int)ifdOffset, littleEndian);
        if (tagCount == 0)
        {
            return 0;
        }

        if (tagCount > TiffIfdTraverser.GetMaxTagCount(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "makernote.ifd.tag.count.limit", "MakerNote IFD entry count exceeded the supported tag count.", "MakerNote", directory.Offset + ifdOffset, tagCount);
            return 0;
        }

        long entriesOffset = ifdOffset + 2;
        long entriesLength = (long)tagCount * TiffIfdTraverser.ClassicEntrySize;
        if (entriesLength < 0 || entriesOffset + entriesLength > data.Length)
        {
            return 0;
        }

        int decodedCount = 0;
        int index = 0;
        while (index < tagCount)
        {
            long entryOffset = entriesOffset + (long)index * TiffIfdTraverser.ClassicEntrySize;
            if (DecodeClassicEntry(document, directory, vendor, data, entryOffset, valueBase, littleEndian))
            {
                decodedCount = decodedCount + 1;
            }

            index = index + 1;
        }

        return decodedCount;
    }

    internal static bool DecodeClassicEntry(ImageMetadataDocument document, MetadataDirectory directory, string vendor, byte[] data, long entryOffset, long valueBase, bool littleEndian)
    {
        int tagId = TiffValueDecoder.ReadUnsigned16(data, (int)entryOffset, littleEndian);
        int typeId = TiffValueDecoder.ReadUnsigned16(data, (int)entryOffset + 2, littleEndian);
        uint count = TiffValueDecoder.ReadUnsigned32(data, (int)entryOffset + 4, littleEndian);
        int elementSize = TiffValueDecoder.ElementByteSize(typeId);
        if (elementSize == 0)
        {
            return false;
        }

        ulong byteCount = (ulong)count * (ulong)elementSize;
        if (count != 0 && byteCount / count != (ulong)elementSize)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "makernote.tag.value.size.overflow", "MakerNote tag value byte size overflowed.", "MakerNote", directory.Offset + entryOffset, count);
            return false;
        }

        if (byteCount > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "makernote.tag.value.large", "MakerNote tag value byte size exceeded the supported buffer size.", "MakerNote", directory.Offset + entryOffset, (long)byteCount);
            return false;
        }

        long valueFieldOffset = entryOffset + 8;
        long valueOffset = valueFieldOffset;
        if (byteCount > 4)
        {
            uint referencedOffset = TiffValueDecoder.ReadUnsigned32(data, (int)valueFieldOffset, littleEndian);
            valueOffset = valueBase + (long)referencedOffset;
        }

        if (valueOffset < 0 || valueOffset + (long)byteCount > data.Length)
        {
            return false;
        }

        byte[] rawValue = TiffValueDecoder.CopyBytes(data, (int)valueOffset, (int)byteCount);
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.TypeId = typeId;
        entry.Count = count;
        entry.EntryOffset = directory.Offset + entryOffset;
        entry.ValueFieldOffset = directory.Offset + valueFieldOffset;
        entry.ValueField = TiffValueDecoder.CopyBytes(data, (int)valueFieldOffset, 4);
        entry.ValueOrOffset = (ulong)valueOffset;
        MetadataValue value = TiffValueDecoder.DecodeValue(document, directory, entry, rawValue, littleEndian);
        AddDecodedMakerNoteTag(directory, vendor, tagId, rawValue, value, directory.Offset + valueOffset);
        return true;
    }

    internal static void AddDecodedMakerNoteTag(MetadataDirectory directory, string vendor, int tagId, byte[] rawValue, MetadataValue value, long offset)
    {
        string keyName = MakerNoteNames.KeyNameFor(vendor, tagId);
        string displayName = MakerNoteNames.DisplayNameFor(vendor, tagId);
        MetadataTag tag = MetadataTag.Create("MakerNote." + directory.Group + "." + keyName, displayName, MetadataFamily.MakerNote, value);
        tag.Group = directory.Group;
        tag.NumericId = tagId;
        tag.RawValue = rawValue;
        tag.DisplayValue = ImageMetadataFormatterCore.FormatValue(value);
        tag.Offset = offset;
        tag.Length = TiffValueDecoder.ByteLength(rawValue);
        tag.Confidence = 80;
        directory.AddTag(tag);
    }

    internal static void AddSummaryTag(MetadataDirectory directory, string keyName, string displayName, MetadataValue value)
    {
        directory.AddTagWithValue("MakerNote." + directory.Group + "." + keyName, displayName, value);
    }

    internal static void AddRawBlock(ImageMetadataDocument document, long offset, long length, byte[] data, ImageMetadataReaderOptions options)
    {
        if (!CaptureRawBlocks(options))
        {
            return;
        }

        MetadataRawBlock rawBlock = new MetadataRawBlock();
        rawBlock.Family = MetadataFamily.MakerNote;
        rawBlock.BlockType = "MakerNote";
        rawBlock.Name = "MakerNote";
        rawBlock.Offset = offset;
        rawBlock.Length = length;
        if (CaptureRawBlockData(options))
        {
            rawBlock.Data = data;
        }

        document.AddRawBlock(rawBlock);
    }

    internal static int NextMakerNoteIndex(ImageMetadataDocument document)
    {
        if (document == null || document.Directories == null)
        {
            return 0;
        }

        int count = 0;
        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory != null && directory.Family == MetadataFamily.MakerNote)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static string DetectVendor(ImageMetadataDocument document, byte[] data)
    {
        if (IsNikonTiffMakerNote(data) || StartsWithAscii(data, "Nikon", 0))
        {
            return "Nikon";
        }

        if (StartsWithAscii(data, "FUJIFILM", 0))
        {
            return "Fujifilm";
        }

        if (StartsWithAscii(data, "OLYMPUS", 0) || StartsWithAscii(data, "OLYMP", 0))
        {
            return "Olympus";
        }

        if (StartsWithAscii(data, "Panasonic", 0))
        {
            return "Panasonic";
        }

        return NormalizeVendor(FindCameraMake(document));
    }

    internal static string FindCameraMake(ImageMetadataDocument document)
    {
        if (document == null || document.Directories == null)
        {
            return "";
        }

        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory != null && directory.Tags != null)
            {
                foreach (MetadataTag tag in directory.Tags)
                {
                    if (tag != null && tag.NumericId == 271 && tag.Value != null && tag.Value.Kind == MetadataValueKind.String)
                    {
                        return tag.Value.Text;
                    }
                }
            }
        }

        return "";
    }

    internal static string NormalizeVendor(string text)
    {
        if (text == null)
        {
            return "";
        }

        string upper = text.ToUpperInvariant();
        if (upper.Contains("APPLE"))
        {
            return "Apple";
        }

        if (upper.Contains("CANON"))
        {
            return "Canon";
        }

        if (upper.Contains("NIKON"))
        {
            return "Nikon";
        }

        if (upper.Contains("SONY"))
        {
            return "Sony";
        }

        if (upper.Contains("FUJIFILM") || upper.Contains("FUJI"))
        {
            return "Fujifilm";
        }

        if (upper.Contains("OLYMPUS") || upper.Contains("OM DIGITAL"))
        {
            return "Olympus";
        }

        if (upper.Contains("PANASONIC"))
        {
            return "Panasonic";
        }

        if (upper.Contains("LEICA"))
        {
            return "Leica";
        }

        if (upper.Contains("PENTAX"))
        {
            return "Pentax";
        }

        if (upper.Contains("SAMSUNG"))
        {
            return "Samsung";
        }

        if (upper.Contains("DJI"))
        {
            return "DJI";
        }

        if (upper.Contains("GOPRO"))
        {
            return "GoPro";
        }

        if (upper.Contains("RICOH"))
        {
            return "Ricoh";
        }

        if (upper.Contains("MINOLTA"))
        {
            return "Minolta";
        }

        if (upper.Contains("KODAK"))
        {
            return "Kodak";
        }

        if (upper.Contains("SIGMA"))
        {
            return "Sigma";
        }

        if (upper.Contains("FOVEON"))
        {
            return "Foveon";
        }

        if (upper.Contains("FLIR"))
        {
            return "FLIR";
        }

        if (upper.Contains("HASSELBLAD"))
        {
            return "Hasselblad";
        }

        return "";
    }

    internal static bool CanDecodeDirectIfdVendor(string vendor)
    {
        return vendor == "Apple"
            || vendor == "Canon"
            || vendor == "Nikon"
            || vendor == "Sony"
            || vendor == "Pentax"
            || vendor == "Samsung"
            || vendor == "DJI"
            || vendor == "GoPro"
            || vendor == "Ricoh"
            || vendor == "Minolta"
            || vendor == "Kodak"
            || vendor == "Sigma"
            || vendor == "Foveon"
            || vendor == "FLIR"
            || vendor == "Hasselblad"
            || vendor == "Leica";
    }

    internal static bool IsNikonTiffMakerNote(byte[] data)
    {
        return data != null
            && data.Length >= 18
            && StartsWithAscii(data, "Nikon", 0)
            && data[5] == 0
            && (data[10] == 73 || data[10] == 77)
            && (data[11] == 73 || data[11] == 77);
    }

    internal static bool IsLittleEndianTiffHeader(byte[] data, int offset)
    {
        return data != null && offset + 2 <= data.Length && data[offset] == 73 && data[offset + 1] == 73;
    }

    internal static bool StartsWithAscii(byte[] data, string text, int offset)
    {
        if (data == null || text == null || offset < 0 || offset + text.Length > data.Length)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            if (data[offset + index] != (byte)text[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool PreserveMakerNotes(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().PreserveMakerNotes;
        }

        return options.PreserveMakerNotes;
    }

    internal static bool DecodeMakerNotes(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().DecodeMakerNotes;
        }

        return options.DecodeMakerNotes;
    }

    internal static bool CaptureRawBlocks(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().CaptureRawBlocks;
        }

        return options.CaptureRawBlocks;
    }

    internal static bool CaptureRawBlockData(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().CaptureRawBlockData;
        }

        return options.CaptureRawBlockData;
    }
}
