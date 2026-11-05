using System.Collections.Generic;

namespace image;

internal class Jpeg2000BoxTraverser
{
    internal const int BasicHeaderSize = 8;
    internal const int ExtendedLengthFieldSize = 8;
    internal const int UuidSize = 16;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jp2.reader.null", "JPEG 2000 parser received a null reader.", "Jpeg2000MetadataParser", -1, 0);
            return document;
        }

        document.ByteLength = reader.Length;
        List<Jpeg2000Box> boxes = ReadBoxes(document, reader, options);
        AddBoxDirectories(document, boxes, null, "JP2");
        Jpeg2000MetadataExtractor.Extract(document, reader, boxes, options);
        Finish(document, reader);
        return document;
    }

    internal static List<Jpeg2000Box> ReadBoxes(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options)
    {
        List<Jpeg2000Box> boxes = new List<Jpeg2000Box>();
        if (document == null || reader == null)
        {
            return boxes;
        }

        Jpeg2000TraversalState state = new Jpeg2000TraversalState();
        state.FileEnd = reader.AccessibleLength();
        return ReadBoxRange(document, reader, 0, state.FileEnd, 0, options, state);
    }

    internal static List<Jpeg2000Box> ReadBoxRange(ImageMetadataDocument document, BoundedStreamReader reader, long start, long end, int depth, ImageMetadataReaderOptions options, Jpeg2000TraversalState state)
    {
        List<Jpeg2000Box> boxes = new List<Jpeg2000Box>();
        if (document == null || reader == null || state == null || start < 0 || end < start)
        {
            return boxes;
        }

        if (!reader.SeekAbsolute(start))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jp2.box.seek", "JPEG 2000 box range could not be reached.", "JP2", start, end - start);
            return boxes;
        }

        while (reader.Position < end && !state.CountLimited)
        {
            if (state.BoxCount >= GetMaxBoxCount(options))
            {
                state.CountLimited = true;
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jp2.box.count.limit", "JPEG 2000 box count exceeded the configured tag count limit.", "JP2", reader.Position, state.BoxCount);
                break;
            }

            if (end - reader.Position < BasicHeaderSize)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "jp2.box.header.truncated", "JPEG 2000 box header was truncated by its superbox boundary.", "JP2", reader.Position, BasicHeaderSize);
                break;
            }

            Jpeg2000Box? box = ReadBoxHeader(document, reader, end, depth, state);
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
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jp2.box.length.limit", "JPEG 2000 box payload exceeded the configured segment limit.", box.Type, box.Offset, ToLong(box.Size));
            }

            if (box.Type == "uuid" && !box.DataLimited)
            {
                ReadUuid(document, reader, box);
            }

            if (IsSuperbox(box.Type) && !box.DataLimited)
            {
                ReadChildren(document, reader, box, options, state);
            }

            long next = box.Offset + (long)box.Size;
            if (!reader.SeekAbsolute(next))
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jp2.box.seek", "JPEG 2000 parser could not seek to the next box.", box.Type, next, 0);
                break;
            }

            if (box.ExtendsToEnd)
            {
                break;
            }
        }

        return boxes;
    }

    internal static Jpeg2000Box? ReadBoxHeader(ImageMetadataDocument document, BoundedStreamReader reader, long parentEnd, int depth, Jpeg2000TraversalState state)
    {
        long offset = reader.Position;
        if (!reader.CanRead(BasicHeaderSize))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jp2.box.header.truncated", "JPEG 2000 box header was truncated.", "JP2", offset, BasicHeaderSize);
            return null;
        }

        uint length32 = ReadUnsigned32(reader.ReadBytes(4));
        byte[] typeBytes = reader.ReadBytes(4);
        Jpeg2000Box box = new Jpeg2000Box();
        box.Index = state.BoxCount;
        box.Type = MetadataStringReader.DecodeLatin1(typeBytes);
        box.TypeBytes = typeBytes;
        box.Offset = offset;
        box.HeaderSize = BasicHeaderSize;
        box.Depth = depth;
        box.TypeValid = IsValidBoxType(typeBytes);
        state.BoxCount = state.BoxCount + 1;

        if (!box.TypeValid)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jp2.box.type.invalid", "JPEG 2000 box type contained a non-printable byte.", box.Type, offset + 4, 4);
        }

        if (length32 == 1)
        {
            box.UsesExtendedLength = true;
            if (parentEnd - reader.Position < ExtendedLengthFieldSize || !reader.CanRead(ExtendedLengthFieldSize))
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "jp2.box.extended-length.truncated", "JPEG 2000 extended box length was truncated.", box.Type, reader.Position, ExtendedLengthFieldSize);
                return null;
            }

            box.Size = ReadUnsigned64(reader.ReadBytes(ExtendedLengthFieldSize));
            box.HeaderSize = box.HeaderSize + ExtendedLengthFieldSize;
        }
        else if (length32 == 0)
        {
            box.ExtendsToEnd = true;
            box.Size = (ulong)(state.FileEnd - offset);
        }
        else
        {
            box.Size = length32;
        }

        if (box.Size < (ulong)box.HeaderSize)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jp2.box.length.invalid", "JPEG 2000 box length was smaller than its header.", box.Type, offset, ToLong(box.Size));
            return null;
        }

        box.DataOffset = offset + box.HeaderSize;
        box.DataSize = box.Size - (ulong)box.HeaderSize;
        if (!BoxFitsRange(box, parentEnd))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jp2.box.truncated", "JPEG 2000 box exceeded its superbox boundary.", box.Type, offset, ToLong(box.Size));
        }

        return box;
    }

    internal static void ReadUuid(ImageMetadataDocument document, BoundedStreamReader reader, Jpeg2000Box box)
    {
        if (box.DataSize < UuidSize || !reader.SeekAbsolute(box.DataOffset) || !reader.CanRead(UuidSize))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jp2.uuid.identifier.truncated", "JPEG 2000 UUID identifier was truncated.", "uuid", box.DataOffset, UuidSize);
            return;
        }

        box.Uuid = reader.ReadBytes(UuidSize);
    }

    internal static void ReadChildren(ImageMetadataDocument document, BoundedStreamReader reader, Jpeg2000Box box, ImageMetadataReaderOptions options, Jpeg2000TraversalState state)
    {
        long childStart = GetChildStart(document, box);
        long end = box.Offset + (long)box.Size;
        if (childStart < 0 || childStart >= end)
        {
            return;
        }

        if (box.Depth >= GetMaxNestingDepth(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jp2.box.depth.limit", "JPEG 2000 nested box depth exceeded the configured limit.", box.Type, box.Offset, ToLong(box.Size));
            return;
        }

        box.Children = ReadBoxRange(document, reader, childStart, end, box.Depth + 1, options, state);
    }

    internal static long GetChildStart(ImageMetadataDocument document, Jpeg2000Box box)
    {
        int prefix = GetChildPrefix(box.Type);
        if (box.DataSize < (ulong)prefix)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jp2.box.superbox-prefix.truncated", "JPEG 2000 superbox fields before nested boxes were truncated.", box.Type, box.DataOffset, prefix);
            return -1;
        }

        return box.DataOffset + prefix;
    }

    internal static int GetChildPrefix(string type)
    {
        if (type == "cref" || type == "meta" || type == "phtx")
        {
            return 4;
        }

        return 0;
    }

    internal static bool IsSuperbox(string type)
    {
        return type == "jp2h"
            || type == "res "
            || type == "uinf"
            || type == "asoc"
            || type == "jplh"
            || type == "jpch"
            || type == "cgrp"
            || type == "comp"
            || type == "drep"
            || type == "jumb"
            || type == "meta"
            || type == "pcol"
            || type == "page"
            || type == "phtx"
            || type == "htxb"
            || type == "lobj"
            || type == "bclr"
            || type == "objc"
            || type == "ftbl"
            || type == "cref";
    }

    internal static void AddBoxDirectories(ImageMetadataDocument document, List<Jpeg2000Box> boxes, MetadataDirectory? parent, string parentPath)
    {
        if (document == null || boxes == null)
        {
            return;
        }

        foreach (Jpeg2000Box box in boxes)
        {
            MetadataDirectory directory = MetadataDirectory.Create(MetadataFamily.Jpeg2000, box.Type, box.Type + " Box");
            directory.Path = parentPath + "/" + box.Type + "/" + box.SiblingIndex.ToString();
            directory.Offset = box.Offset;
            directory.Length = ToLong(box.Size);
            directory.RepeatIndex = box.SiblingIndex;
            box.Directory = directory;
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

    internal static void AddBoxTags(MetadataDirectory directory, Jpeg2000Box box)
    {
        string prefix = "Jpeg2000." + box.Type;
        long lengthOffset = box.UsesExtendedLength ? box.Offset + BasicHeaderSize : box.Offset;
        long lengthSize = box.UsesExtendedLength ? ExtendedLengthFieldSize : 4;
        AddTag(directory, prefix + ".Index", "Index", MetadataValue.FromInteger(box.Index), box.Offset, BasicHeaderSize, "");
        AddTag(directory, prefix + ".Type", "Type", MetadataValue.FromString(box.Type), box.Offset + 4, 4, box.Type);
        AddTag(directory, prefix + ".Length", "Length", MetadataValue.FromUnsignedInteger(box.Size), lengthOffset, lengthSize, "");
        AddTag(directory, prefix + ".HeaderSize", "Header Size", MetadataValue.FromInteger(box.HeaderSize), box.Offset, box.HeaderSize, "");
        AddTag(directory, prefix + ".DataOffset", "Data Offset", MetadataValue.FromInteger(box.DataOffset), box.Offset, box.HeaderSize, "");
        AddTag(directory, prefix + ".DataLength", "Data Length", MetadataValue.FromUnsignedInteger(box.DataSize), box.DataOffset, ToLong(box.DataSize), "");
        AddTag(directory, prefix + ".Depth", "Depth", MetadataValue.FromInteger(box.Depth), box.Offset, box.HeaderSize, "");
        AddTag(directory, prefix + ".UsesExtendedLength", "Uses Extended Length", MetadataValue.FromBoolean(box.UsesExtendedLength), box.Offset, 4, "");
        AddTag(directory, prefix + ".ExtendsToEnd", "Extends To End", MetadataValue.FromBoolean(box.ExtendsToEnd), box.Offset, 4, "");
        AddTag(directory, prefix + ".TypeValid", "Type Valid", MetadataValue.FromBoolean(box.TypeValid), box.Offset + 4, 4, "");
        AddTag(directory, prefix + ".DataLimited", "Data Limited", MetadataValue.FromBoolean(box.DataLimited), box.DataOffset, ToLong(box.DataSize), "");
        if (box.Uuid.Length == UuidSize)
        {
            AddTag(directory, prefix + ".Identifier", "Identifier", MetadataValue.FromBytes(box.Uuid), box.DataOffset, UuidSize, HashHelper.ToHex(box.Uuid));
        }
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string displayValue)
    {
        MetadataTag tag = directory.AddTagWithValue(key, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = displayValue;
        return tag;
    }

    internal static bool BoxFitsRange(Jpeg2000Box box, long end)
    {
        if (box == null || box.Offset < 0 || end < box.Offset)
        {
            return false;
        }

        return box.Size <= (ulong)(end - box.Offset);
    }

    internal static int CountType(List<Jpeg2000Box> boxes, string type)
    {
        int count = 0;
        foreach (Jpeg2000Box box in boxes)
        {
            if (box != null && box.Type == type)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static bool IsValidBoxType(byte[] bytes)
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

    internal static long ToLong(ulong value)
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
