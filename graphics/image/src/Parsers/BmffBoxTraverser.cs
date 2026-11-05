using System.Collections.Generic;

namespace image;

internal class BmffBoxTraverser
{
    internal const int BasicHeaderSize = 8;
    internal const int LargeSizeFieldSize = 8;
    internal const int UserTypeSize = 16;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);

        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmff.reader.null", "ISO BMFF parser received a null reader.", "BmffMetadataParser", -1, 0);
            return document;
        }

        document.ByteLength = reader.Length;
        List<BmffBox> boxes = ReadBoxes(document, reader, options);
        BmffBrandDetector.Extract(document, reader, boxes, options);
        AddBoxDirectories(document, boxes, null, "BMFF");
        BmffMetadataExtractor.Extract(document, reader, boxes, options);
        Finish(document, reader);
        return document;
    }

    internal static List<BmffBox> ReadBoxes(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options)
    {
        List<BmffBox> boxes = new List<BmffBox>();
        if (document == null || reader == null)
        {
            return boxes;
        }

        BmffTraversalState state = new BmffTraversalState();
        state.FileEnd = reader.AccessibleLength();
        if (!reader.SeekAbsolute(0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmff.box.seek", "ISO BMFF box stream could not be reached.", "BMFF", 0, 0);
            return boxes;
        }

        return ReadBoxRange(document, reader, 0, state.FileEnd, 0, options, state);
    }

    internal static List<BmffBox> ReadBoxRange(ImageMetadataDocument document, BoundedStreamReader reader, long start, long end, int depth, ImageMetadataReaderOptions options, BmffTraversalState state)
    {
        List<BmffBox> boxes = new List<BmffBox>();
        if (document == null || reader == null || state == null || start < 0 || end < start)
        {
            return boxes;
        }

        if (!reader.SeekAbsolute(start))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmff.box.seek", "ISO BMFF box range could not be reached.", "BMFF", start, end - start);
            return boxes;
        }

        while (reader.Position < end && !state.CountLimited)
        {
            if (state.BoxCount >= GetMaxBoxCount(options))
            {
                state.CountLimited = true;
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmff.box.count.limit", "ISO BMFF box count exceeded the configured tag count limit.", "BMFF", reader.Position, state.BoxCount);
                break;
            }

            long remaining = end - reader.Position;
            if (remaining < BasicHeaderSize)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff.box.header.truncated", "ISO BMFF box header was truncated by its container boundary.", "BMFF", reader.Position, BasicHeaderSize);
                break;
            }

            BmffBox? box = ReadBoxHeader(document, reader, end, depth, state);
            if (box == null)
            {
                break;
            }

            box.SiblingIndex = CountType(boxes, box.Type);
            boxes.Add(box);
            if (!BoxFitsRange(box, end))
            {
                break;
            }

            if (box.DataSize > ToUnsignedLength(GetMaxSegmentBytes(options)))
            {
                box.DataLimited = true;
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmff.box.length.limit", "ISO BMFF box payload exceeded the configured segment limit.", box.Type, box.Offset, ToDiagnosticLength(box.Size));
            }

            if (IsContainerBox(box) && !box.DataLimited)
            {
                ReadChildren(document, reader, box, options, state);
            }

            long next = box.Offset + (long)box.Size;
            if (!reader.SeekAbsolute(next))
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmff.box.seek", "ISO BMFF parser could not seek to the next box.", box.Type, next, 0);
                break;
            }

            if (box.ExtendsToEnd)
            {
                break;
            }
        }

        return boxes;
    }

    internal static BmffBox? ReadBoxHeader(ImageMetadataDocument document, BoundedStreamReader reader, long parentEnd, int depth, BmffTraversalState state)
    {
        long offset = reader.Position;
        if (!reader.CanRead(BasicHeaderSize))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff.box.header.truncated", "ISO BMFF box header was truncated.", "BMFF", offset, BasicHeaderSize);
            return null;
        }

        byte[] sizeBytes = reader.ReadBytes(4);
        byte[] typeBytes = reader.ReadBytes(4);
        uint size32 = ReadUnsigned32(sizeBytes);
        string type = MetadataStringReader.DecodeLatin1(typeBytes);
        BmffBox box = new BmffBox();
        box.Index = state.BoxCount;
        box.Type = type;
        box.TypeBytes = typeBytes;
        box.Offset = offset;
        box.HeaderSize = BasicHeaderSize;
        box.Depth = depth;
        box.TypeValid = IsValidFourCc(typeBytes);
        state.BoxCount = state.BoxCount + 1;

        if (!box.TypeValid)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmff.box.type.invalid", "ISO BMFF box type contained a non-printable byte.", type, offset + 4, 4);
        }

        if (size32 == 1)
        {
            box.UsesLargeSize = true;
            if (parentEnd - reader.Position < LargeSizeFieldSize || !reader.CanRead(LargeSizeFieldSize))
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff.box.largesize.truncated", "ISO BMFF largesize field was truncated.", type, reader.Position, LargeSizeFieldSize);
                return null;
            }

            box.Size = ReadUnsigned64(reader.ReadBytes(LargeSizeFieldSize));
            box.HeaderSize = box.HeaderSize + LargeSizeFieldSize;
        }
        else if (size32 == 0)
        {
            box.ExtendsToEnd = true;
            box.Size = (ulong)(state.FileEnd - offset);
        }
        else
        {
            box.Size = size32;
        }

        int requiredHeaderSize = box.HeaderSize;
        if (type == "uuid")
        {
            requiredHeaderSize = requiredHeaderSize + UserTypeSize;
        }

        if (box.Size < (ulong)requiredHeaderSize)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "bmff.box.size.invalid", "ISO BMFF box size was smaller than its header.", type, offset, ToDiagnosticLength(box.Size));
            return null;
        }

        if (type == "uuid")
        {
            if (parentEnd - reader.Position < UserTypeSize || !reader.CanRead(UserTypeSize))
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff.box.usertype.truncated", "ISO BMFF UUID user type was truncated.", type, reader.Position, UserTypeSize);
                return null;
            }

            box.UserType = reader.ReadBytes(UserTypeSize);
            box.HeaderSize = box.HeaderSize + UserTypeSize;
        }

        box.DataOffset = offset + box.HeaderSize;
        box.DataSize = box.Size - (ulong)box.HeaderSize;
        if (!BoxFitsRange(box, parentEnd))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff.box.truncated", "ISO BMFF box exceeded its container boundary.", type, offset, ToDiagnosticLength(box.Size));
        }

        return box;
    }

    internal static void ReadChildren(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box, ImageMetadataReaderOptions options, BmffTraversalState state)
    {
        long childStart = GetChildStart(document, reader, box);
        long childEnd = box.Offset + (long)box.Size;
        if (childStart < 0 || childStart >= childEnd)
        {
            return;
        }

        if (box.Depth >= GetMaxNestingDepth(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "bmff.box.depth.limit", "ISO BMFF nested box depth exceeded the configured limit.", box.Type, box.Offset, ToDiagnosticLength(box.Size));
            return;
        }

        box.Children = ReadBoxRange(document, reader, childStart, childEnd, box.Depth + 1, options, state);
    }

    internal static long GetChildStart(ImageMetadataDocument document, BoundedStreamReader reader, BmffBox box)
    {
        int prefix = GetFixedChildPrefix(box.Type);
        if (box.Type == "iinf")
        {
            if (box.DataSize < 1 || !reader.SeekAbsolute(box.DataOffset) || !reader.CanRead(1))
            {
                AddContainerPrefixTruncated(document, box, 1);
                return -1;
            }

            int version = reader.ReadByte();
            prefix = version == 0 ? 6 : 8;
        }

        if (box.DataSize < (ulong)prefix)
        {
            AddContainerPrefixTruncated(document, box, prefix);
            return -1;
        }

        return box.DataOffset + prefix;
    }

    internal static void AddContainerPrefixTruncated(ImageMetadataDocument document, BmffBox box, int prefix)
    {
        MetadataDiagnostic.AddTruncated(document.Diagnostics, "bmff.box.container-prefix.truncated", "ISO BMFF container fields before nested boxes were truncated.", box.Type, box.DataOffset, prefix);
    }

    internal static int GetFixedChildPrefix(string type)
    {
        if (type == "meta" || type == "iref" || type == "grpl")
        {
            return 4;
        }

        if (type == "dref" || type == "stsd")
        {
            return 8;
        }

        if (type == "ipro")
        {
            return 6;
        }

        return 0;
    }

    internal static bool IsContainer(string type)
    {
        return type == "moov"
            || type == "trak"
            || type == "mdia"
            || type == "minf"
            || type == "dinf"
            || type == "stbl"
            || type == "edts"
            || type == "udta"
            || type == "tref"
            || type == "meta"
            || type == "moof"
            || type == "traf"
            || type == "mfra"
            || type == "mvex"
            || type == "iprp"
            || type == "ipco"
            || type == "iinf"
            || type == "iref"
            || type == "grpl"
            || type == "dref"
            || type == "stsd"
            || type == "ipro"
            || type == "sinf"
            || type == "schi"
            || type == "fiin"
            || type == "paen"
            || type == "meco"
            || type == "strk"
            || type == "jumb";
    }

    internal static bool IsContainerBox(BmffBox box)
    {
        if (box == null)
        {
            return false;
        }

        return IsContainer(box.Type) || box.Type == "uuid" && Cr3MetadataExtractor.IsCanonMetadataUuid(box.UserType);
    }

    internal static void AddBoxDirectories(ImageMetadataDocument document, List<BmffBox> boxes, MetadataDirectory? parent, string parentPath)
    {
        if (document == null || boxes == null)
        {
            return;
        }

        foreach (BmffBox box in boxes)
        {
            MetadataDirectory directory = MetadataDirectory.Create(MetadataFamily.Bmff, box.Type, box.Type + " Box");
            directory.Path = parentPath + "/" + box.Type + "/" + box.SiblingIndex.ToString();
            directory.Offset = box.Offset;
            directory.Length = ToDiagnosticLength(box.Size);
            directory.RepeatIndex = box.SiblingIndex;
            if (parent == null)
            {
                document.AddDirectory(directory);
            }
            else
            {
                parent.AddChild(directory);
            }

            AddBoxTags(directory, box);
            AddBoxDirectories(document, box.Children, directory, directory.Path);
        }
    }

    internal static void AddBoxTags(MetadataDirectory directory, BmffBox box)
    {
        string prefix = "Bmff." + box.Type;
        long sizeOffset = box.UsesLargeSize ? box.Offset + BasicHeaderSize : box.Offset;
        long sizeLength = box.UsesLargeSize ? LargeSizeFieldSize : 4;
        AddTag(directory, prefix + ".Index", "Index", MetadataValue.FromInteger(box.Index), box.Offset, 8, "");
        AddTag(directory, prefix + ".Type", "Type", MetadataValue.FromString(box.Type), box.Offset + 4, 4, box.Type);
        AddTag(directory, prefix + ".Size", "Size", MetadataValue.FromUnsignedInteger(box.Size), sizeOffset, sizeLength, "");
        AddTag(directory, prefix + ".HeaderSize", "Header Size", MetadataValue.FromInteger(box.HeaderSize), box.Offset, box.HeaderSize, "");
        AddTag(directory, prefix + ".DataOffset", "Data Offset", MetadataValue.FromInteger(box.DataOffset), box.Offset, box.HeaderSize, "");
        AddTag(directory, prefix + ".DataSize", "Data Size", MetadataValue.FromUnsignedInteger(box.DataSize), box.DataOffset, ToDiagnosticLength(box.DataSize), "");
        AddTag(directory, prefix + ".Depth", "Depth", MetadataValue.FromInteger(box.Depth), box.Offset, box.HeaderSize, "");
        AddTag(directory, prefix + ".UsesLargeSize", "Uses Large Size", MetadataValue.FromBoolean(box.UsesLargeSize), box.Offset, 4, "");
        AddTag(directory, prefix + ".ExtendsToEnd", "Extends To End", MetadataValue.FromBoolean(box.ExtendsToEnd), box.Offset, 4, "");
        AddTag(directory, prefix + ".TypeValid", "Type Valid", MetadataValue.FromBoolean(box.TypeValid), box.Offset + 4, 4, "");
        AddTag(directory, prefix + ".DataLimited", "Data Limited", MetadataValue.FromBoolean(box.DataLimited), box.DataOffset, ToDiagnosticLength(box.DataSize), "");
        if (box.UserType.Length == UserTypeSize)
        {
            string userType = HashHelper.ToHex(box.UserType);
            AddTag(directory, prefix + ".UserType", "User Type", MetadataValue.FromBytes(box.UserType), box.Offset + box.HeaderSize - UserTypeSize, UserTypeSize, userType);
        }

        if (box.BrandInfo != null && box.BrandInfo.Valid)
        {
            AddFileTypeTags(directory, box, box.BrandInfo);
        }
    }

    internal static void AddFileTypeTags(MetadataDirectory directory, BmffBox box, BmffBrandInfo info)
    {
        long offset = box.DataOffset;
        AddTag(directory, "Bmff.ftyp.MajorBrand", "Major Brand", MetadataValue.FromString(info.MajorBrand), offset, 4, info.MajorBrand);
        AddTag(directory, "Bmff.ftyp.MinorVersion", "Minor Version", MetadataValue.FromUnsignedInteger(info.MinorVersion), offset + 4, 4, "");
        MetadataValue compatible = new MetadataValue();
        compatible.Kind = MetadataValueKind.StringList;
        foreach (string brand in info.CompatibleBrands)
        {
            compatible.AddTextValue(brand);
        }

        AddTag(directory, "Bmff.ftyp.CompatibleBrands", "Compatible Brands", compatible, offset + 8, info.CompatibleBrands.Count * 4, "");
        AddTag(directory, "Bmff.ftyp.DetectedFormat", "Detected Format", MetadataValue.FromString(info.Name), offset, ToDiagnosticLength(box.DataSize), info.Name);
        AddTag(directory, "Bmff.ftyp.ClassifiedByCompatibleBrand", "Classified By Compatible Brand", MetadataValue.FromBoolean(info.ClassifiedByCompatibleBrand), offset + 8, info.CompatibleBrands.Count * 4, "");
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string displayValue)
    {
        MetadataTag tag = directory.AddTagWithValue(key, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = displayValue;
        return tag;
    }

    internal static bool BoxFitsRange(BmffBox box, long end)
    {
        if (box == null || box.Offset < 0 || end < box.Offset)
        {
            return false;
        }

        return box.Size <= (ulong)(end - box.Offset);
    }

    internal static int CountType(List<BmffBox> boxes, string type)
    {
        int count = 0;
        foreach (BmffBox box in boxes)
        {
            if (box != null && box.Type == type)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static bool IsValidFourCc(byte[] bytes)
    {
        if (bytes == null || bytes.Length != 4)
        {
            return false;
        }

        foreach (byte value in bytes)
        {
            if (value < 32 || value > 126)
            {
                return false;
            }
        }

        return true;
    }

    internal static uint ReadUnsigned32(byte[] bytes)
    {
        if (bytes == null || bytes.Length != 4)
        {
            return 0;
        }

        return ((uint)bytes[0] << 24)
            | ((uint)bytes[1] << 16)
            | ((uint)bytes[2] << 8)
            | bytes[3];
    }

    internal static ulong ReadUnsigned64(byte[] bytes)
    {
        if (bytes == null || bytes.Length != 8)
        {
            return 0;
        }

        ulong value = 0;
        foreach (byte item in bytes)
        {
            value = (value << 8) | item;
        }

        return value;
    }

    internal static ulong ToUnsignedLength(long value)
    {
        if (value < 0)
        {
            return 0;
        }

        return (ulong)value;
    }

    internal static long ToDiagnosticLength(ulong value)
    {
        if (value > 9223372036854775807)
        {
            return 9223372036854775807;
        }

        return (long)value;
    }

    internal static long GetMaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
    }

    internal static int GetMaxBoxCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxTagCount : options.MaxTagCount;
    }

    internal static int GetMaxNestingDepth(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxNestingDepth : options.MaxNestingDepth;
    }

    internal static void Finish(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (reader != null && reader.Source != null && reader.Source.CanSeek)
        {
            reader.SeekAbsolute(0);
        }

        if (reader != null && reader.Diagnostics != null)
        {
            foreach (MetadataDiagnostic diagnostic in reader.Diagnostics)
            {
                document.Diagnostics.Add(diagnostic);
            }
        }

        ImageMetadataNormalizerCore.BuildSummary(document);
    }
}
