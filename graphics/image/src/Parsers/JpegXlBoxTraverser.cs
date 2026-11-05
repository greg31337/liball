using System.Collections.Generic;

namespace image;

internal class JpegXlBoxTraverser
{
    internal const int BasicHeaderSize = 8;
    internal const int LargeSizeFieldSize = 8;
    internal const int PartialIndexSize = 4;
    internal const int CodestreamPrefixLimit = 65536;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.reader.null", "JPEG XL parser received a null reader.", "JpegXlMetadataParser", -1, 0);
            return document;
        }

        document.ByteLength = reader.Length;
        if (HasNakedCodestreamMarker(reader))
        {
            byte[] prefix = ReadPrefix(reader, 0, reader.AccessibleLength());
            JpegXlCodestreamHeader header = ParseCodestreamHeader(document, prefix, false, 0);
            AddCodestreamDirectory(document, header, "Naked");
            JpegXlMetadataExtractor.ExtractCodestream(document, header, options);
        }
        else
        {
            List<JpegXlBox> boxes = ReadBoxes(document, reader, options);
            AddBoxDirectories(document, boxes, null, "JXL");
            ValidateContainer(document, reader, boxes);
            JpegXlCodestreamHeader? header = ExtractContainerCodestreamHeader(document, reader, boxes);
            if (header != null)
            {
                AddCodestreamDirectory(document, header, "Container");
                JpegXlMetadataExtractor.ExtractCodestream(document, header, options);
            }
            JpegXlMetadataExtractor.ExtractBoxes(document, reader, boxes, options);
        }

        Finish(document, reader);
        return document;
    }

    internal static bool HasNakedCodestreamMarker(BoundedStreamReader reader)
    {
        if (reader == null || !reader.SeekAbsolute(0) || !reader.CanRead(2))
        {
            return false;
        }

        byte[] marker = reader.ReadBytes(2);
        return marker.Length == 2 && marker[0] == 255 && marker[1] == 10;
    }

    internal static List<JpegXlBox> ReadBoxes(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options)
    {
        List<JpegXlBox> boxes = new List<JpegXlBox>();
        if (document == null || reader == null)
        {
            return boxes;
        }

        JpegXlTraversalState state = new JpegXlTraversalState();
        state.FileEnd = reader.AccessibleLength();
        return ReadBoxRange(document, reader, 0, state.FileEnd, 0, options, state);
    }

    internal static List<JpegXlBox> ReadBoxRange(ImageMetadataDocument document, BoundedStreamReader reader, long start, long end, int depth, ImageMetadataReaderOptions options, JpegXlTraversalState state)
    {
        List<JpegXlBox> boxes = new List<JpegXlBox>();
        if (document == null || reader == null || state == null || start < 0 || end < start)
        {
            return boxes;
        }

        if (!reader.SeekAbsolute(start))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.box.seek", "JPEG XL box range could not be reached.", "JXL", start, end - start);
            return boxes;
        }

        while (reader.Position < end && !state.CountLimited)
        {
            if (state.BoxCount >= GetMaxBoxCount(options))
            {
                state.CountLimited = true;
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jxl.box.count.limit", "JPEG XL box count exceeded the configured tag count limit.", "JXL", reader.Position, state.BoxCount);
                break;
            }

            if (end - reader.Position < BasicHeaderSize)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.box.header.truncated", "JPEG XL box header was truncated by its parent boundary.", "JXL", reader.Position, BasicHeaderSize);
                break;
            }

            JpegXlBox? box = ReadBoxHeader(document, reader, end, depth, state);
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
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jxl.box.length.limit", "JPEG XL box payload exceeded the configured segment limit.", box.Type, box.Offset, ToLong(box.Size));
            }

            ReadKnownBoxFields(document, reader, box, options);
            if (box.Type == "jumb" && !box.DataLimited)
            {
                ReadChildren(document, reader, box, options, state);
            }

            long next = box.Offset + (long)box.Size;
            if (!reader.SeekAbsolute(next))
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.box.seek", "JPEG XL parser could not seek to the next box.", box.Type, next, 0);
                break;
            }

            if (box.ExtendsToEnd)
            {
                break;
            }
        }

        return boxes;
    }

    internal static JpegXlBox? ReadBoxHeader(ImageMetadataDocument document, BoundedStreamReader reader, long parentEnd, int depth, JpegXlTraversalState state)
    {
        long offset = reader.Position;
        if (!reader.CanRead(BasicHeaderSize))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.box.header.truncated", "JPEG XL box header was truncated.", "JXL", offset, BasicHeaderSize);
            return null;
        }

        uint size32 = ReadUnsigned32(reader.ReadBytes(4));
        byte[] typeBytes = reader.ReadBytes(4);
        JpegXlBox box = new JpegXlBox();
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
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.box.type.invalid", "JPEG XL box type contained a non-printable byte.", box.Type, offset + 4, 4);
        }

        if (size32 == 1)
        {
            box.UsesLargeSize = true;
            if (parentEnd - reader.Position < LargeSizeFieldSize || !reader.CanRead(LargeSizeFieldSize))
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.box.large-size.truncated", "JPEG XL large box size was truncated.", box.Type, reader.Position, LargeSizeFieldSize);
                return null;
            }

            box.Size = ReadUnsigned64(reader.ReadBytes(LargeSizeFieldSize));
            box.HeaderSize = box.HeaderSize + LargeSizeFieldSize;
        }
        else if (size32 == 0)
        {
            box.ExtendsToEnd = true;
            box.Size = (ulong)(parentEnd - offset);
        }
        else
        {
            box.Size = size32;
        }

        if (box.Size < (ulong)box.HeaderSize)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.box.length.invalid", "JPEG XL box size was smaller than its header.", box.Type, offset, ToLong(box.Size));
            return null;
        }

        box.DataOffset = offset + box.HeaderSize;
        box.DataSize = box.Size - (ulong)box.HeaderSize;
        box.CodestreamOffset = -1;
        if (!BoxFitsRange(box, parentEnd))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.box.truncated", "JPEG XL box exceeded its parent boundary.", box.Type, offset, ToLong(box.Size));
        }

        return box;
    }

    internal static void ReadKnownBoxFields(ImageMetadataDocument document, BoundedStreamReader reader, JpegXlBox box, ImageMetadataReaderOptions options)
    {
        if (box.Type == "jxlc")
        {
            box.CodestreamOffset = box.DataOffset;
            box.CodestreamSize = box.DataSize;
        }
        else if (box.Type == "jxlp")
        {
            ReadPartialIndex(document, reader, box);
        }
        else if (box.Type == "ftyp")
        {
            ReadFileType(document, reader, box, options);
        }
    }

    internal static void ReadPartialIndex(ImageMetadataDocument document, BoundedStreamReader reader, JpegXlBox box)
    {
        if (box.DataSize < PartialIndexSize || !reader.SeekAbsolute(box.DataOffset) || !reader.CanRead(PartialIndexSize))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.jxlp.index.truncated", "JPEG XL partial codestream box index was truncated.", "jxlp", box.DataOffset, PartialIndexSize);
            return;
        }

        uint value = ReadUnsigned32(reader.ReadBytes(PartialIndexSize));
        box.PartialIndexValid = true;
        box.PartialIndex = value & 2147483647;
        box.PartialIsLast = (value & 2147483648) != 0;
        box.CodestreamOffset = box.DataOffset + PartialIndexSize;
        box.CodestreamSize = box.DataSize - PartialIndexSize;
    }

    internal static void ReadFileType(ImageMetadataDocument document, BoundedStreamReader reader, JpegXlBox box, ImageMetadataReaderOptions options)
    {
        if (box.DataSize < 8 || !reader.SeekAbsolute(box.DataOffset) || !reader.CanRead(8))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.ftyp.truncated", "JPEG XL file type box was truncated.", "ftyp", box.DataOffset, 8);
            return;
        }

        if (box.DataSize < 12)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.ftyp.truncated", "JPEG XL file type box did not contain a compatible brand.", "ftyp", box.DataOffset, 12);
        }

        box.MajorBrand = MetadataStringReader.DecodeLatin1(reader.ReadBytes(4));
        box.MinorVersion = ReadUnsigned32(reader.ReadBytes(4));
        ulong remaining = box.DataSize - 8;
        ulong readable = remaining;
        ulong brandLimit = ToUnsignedLength(GetMaxBoxCount(options)) * 4;
        if (readable > brandLimit)
        {
            readable = brandLimit;
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jxl.ftyp.brand.limit", "JPEG XL compatible brand count exceeded the configured tag count limit.", "ftyp", box.DataOffset + 8, ToLong(remaining));
        }

        while (readable >= 4 && reader.CanRead(4))
        {
            box.CompatibleBrands.Add(MetadataStringReader.DecodeLatin1(reader.ReadBytes(4)));
            readable = readable - 4;
        }

        if (remaining % 4 != 0)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.ftyp.brand.truncated", "JPEG XL compatible brand was not a complete four-byte value.", "ftyp", box.DataOffset + ToLong(box.DataSize - box.DataSize % 4), ToLong(box.DataSize % 4));
        }

        if (box.MajorBrand != "jxl ")
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.ftyp.brand.invalid", "JPEG XL file type box did not use the required jxl major brand.", "ftyp", box.DataOffset, 4);
        }

        if (box.MinorVersion > 1)
        {
            MetadataDiagnostic.AddUnsupported(document.Diagnostics, "jxl.ftyp.version.unsupported", "JPEG XL container version is newer than the supported version.", "ftyp");
        }
    }

    internal static void ReadChildren(ImageMetadataDocument document, BoundedStreamReader reader, JpegXlBox box, ImageMetadataReaderOptions options, JpegXlTraversalState state)
    {
        if (box.Depth >= GetMaxNestingDepth(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jxl.box.depth.limit", "JPEG XL nested box depth exceeded the configured limit.", box.Type, box.Offset, ToLong(box.Size));
            return;
        }

        long end = box.Offset + (long)box.Size;
        if (box.DataOffset < end)
        {
            box.Children = ReadBoxRange(document, reader, box.DataOffset, end, box.Depth + 1, options, state);
        }
    }

    internal static void ValidateContainer(ImageMetadataDocument document, BoundedStreamReader reader, List<JpegXlBox> boxes)
    {
        if (boxes.Count == 0)
        {
            return;
        }

        JpegXlBox signature = boxes[0];
        if (signature.Type != "JXL " || signature.Size != 12 || !HasSignaturePayload(reader, signature))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.signature.invalid", "JPEG XL container signature box was missing or invalid.", signature.Type, signature.Offset, ToLong(signature.Size));
        }

        if (boxes.Count < 2 || boxes[1].Type != "ftyp")
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.ftyp.position.invalid", "JPEG XL file type box must immediately follow the signature box.", "ftyp", 0, 0);
        }

        int jxlcCount = 0;
        int jxlpCount = 0;
        int index = 0;
        foreach (JpegXlBox box in boxes)
        {
            if (box.Type == "ftyp" && index != 1)
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.ftyp.position.invalid", "JPEG XL file type box appeared outside the required second position.", "ftyp", box.Offset, ToLong(box.Size));
            }
            else if (box.Type == "jxlc")
            {
                jxlcCount = jxlcCount + 1;
            }
            else if (box.Type == "jxlp")
            {
                jxlpCount = jxlpCount + 1;
            }

            index = index + 1;
        }

        if (jxlcCount > 1)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.jxlc.duplicate", "JPEG XL container contained more than one complete codestream box.", "jxlc", 0, jxlcCount);
        }

        if (jxlcCount > 0 && jxlpCount > 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.codestream.box.conflict", "JPEG XL container mixed complete and partial codestream boxes.", "JXL", 0, jxlcCount + jxlpCount);
        }
    }

    internal static bool HasSignaturePayload(BoundedStreamReader reader, JpegXlBox box)
    {
        if (reader == null || box == null || box.DataSize != 4 || !reader.SeekAbsolute(box.DataOffset) || !reader.CanRead(4))
        {
            return false;
        }

        byte[] value = reader.ReadBytes(4);
        return value[0] == 13 && value[1] == 10 && value[2] == 135 && value[3] == 10;
    }

    internal static JpegXlCodestreamHeader? ExtractContainerCodestreamHeader(ImageMetadataDocument document, BoundedStreamReader reader, List<JpegXlBox> boxes)
    {
        JpegXlBox? complete = null;
        List<JpegXlBox> partials = new List<JpegXlBox>();
        uint version = 0;
        foreach (JpegXlBox box in boxes)
        {
            if (box.Type == "ftyp")
            {
                version = box.MinorVersion;
            }
            else if (box.Type == "jxlc" && complete == null)
            {
                complete = box;
            }
            else if (box.Type == "jxlp" && box.PartialIndexValid)
            {
                partials.Add(box);
            }
        }

        if (complete != null)
        {
            byte[] prefix = ReadPrefix(reader, complete.CodestreamOffset, ToLong(complete.CodestreamSize));
            return ParseCodestreamHeader(document, prefix, false, complete.CodestreamOffset);
        }

        if (partials.Count > 0)
        {
            return ParsePartialCodestreamHeader(document, reader, partials, version);
        }

        return null;
    }

    internal static JpegXlCodestreamHeader ParsePartialCodestreamHeader(ImageMetadataDocument document, BoundedStreamReader reader, List<JpegXlBox> parts, uint version)
    {
        if (version == 0)
        {
            uint expectedPhysical = 0;
            foreach (JpegXlBox part in parts)
            {
                if (part.PartialIndex != expectedPhysical)
                {
                    MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.jxlp.order.invalid", "JPEG XL version 0 partial codestream boxes were not in index order.", "jxlp", part.Offset, ToLong(part.Size));
                    break;
                }

                expectedPhysical = expectedPhysical + 1;
            }
        }

        SortParts(parts);
        uint expected = 0;
        bool sawLast = false;
        List<byte> prefix = new List<byte>();
        long sourceOffset = parts[0].CodestreamOffset;
        foreach (JpegXlBox part in parts)
        {
            if (part.PartialIndex < expected)
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.jxlp.index.duplicate", "JPEG XL partial codestream index was duplicated.", "jxlp", part.Offset, ToLong(part.Size));
                continue;
            }

            if (part.PartialIndex > expected)
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.jxlp.index.missing", "JPEG XL partial codestream sequence skipped an index.", "jxlp", part.Offset, ToLong(part.Size));
                break;
            }

            if (sawLast)
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.jxlp.last.invalid", "JPEG XL partial codestream continued after a box marked as final.", "jxlp", part.Offset, ToLong(part.Size));
                break;
            }

            AppendPrefix(reader, part.CodestreamOffset, ToLong(part.CodestreamSize), prefix);
            sawLast = part.PartialIsLast;
            expected = expected + 1;
        }

        if (!sawLast)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.jxlp.last.missing", "JPEG XL partial codestream did not mark its final box.", "jxlp", parts[parts.Count - 1].Offset, ToLong(parts[parts.Count - 1].Size));
        }

        return ParseCodestreamHeader(document, prefix.ToArray(), true, sourceOffset);
    }

    internal static void SortParts(List<JpegXlBox> parts)
    {
        int index = 1;
        while (index < parts.Count)
        {
            JpegXlBox value = parts[index];
            int destination = index - 1;
            while (destination >= 0 && parts[destination].PartialIndex > value.PartialIndex)
            {
                parts[destination + 1] = parts[destination];
                destination = destination - 1;
            }

            parts[destination + 1] = value;
            index = index + 1;
        }
    }

    internal static byte[] ReadPrefix(BoundedStreamReader reader, long offset, long length)
    {
        List<byte> bytes = new List<byte>();
        AppendPrefix(reader, offset, length, bytes);
        return bytes.ToArray();
    }

    internal static void AppendPrefix(BoundedStreamReader reader, long offset, long length, List<byte> bytes)
    {
        if (reader == null || bytes == null || offset < 0 || length <= 0 || bytes.Count >= CodestreamPrefixLimit)
        {
            return;
        }

        int count = CodestreamPrefixLimit - bytes.Count;
        if (length < count)
        {
            count = (int)length;
        }

        if (!reader.SeekAbsolute(offset) || !reader.CanRead(count))
        {
            return;
        }

        byte[] source = reader.ReadBytes(count);
        foreach (byte item in source)
        {
            bytes.Add(item);
        }
    }

    internal static JpegXlCodestreamHeader ParseCodestreamHeader(ImageMetadataDocument document, byte[] bytes, bool fragmented, long sourceOffset)
    {
        JpegXlCodestreamHeader header = new JpegXlCodestreamHeader();
        header.Fragmented = fragmented;
        header.SourceOffset = sourceOffset;
        if (bytes == null || bytes.Length < 2)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.codestream.marker.truncated", "JPEG XL codestream marker was truncated.", "Codestream", sourceOffset, 2);
            return header;
        }

        if (bytes[0] != 255 || bytes[1] != 10)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jxl.codestream.marker.invalid", "JPEG XL codestream did not begin with the required marker.", "Codestream", sourceOffset, 2);
            return header;
        }

        header.MarkerValid = true;
        JpegXlBitReader bits = new JpegXlBitReader(bytes, 16);
        header.SmallSize = bits.ReadBits(1) != 0;
        header.Height = ReadSizeValue(bits, header.SmallSize);
        header.AspectRatioCode = (int)bits.ReadBits(3);
        if (header.AspectRatioCode == 0)
        {
            header.Width = ReadSizeValue(bits, header.SmallSize);
        }
        else
        {
            header.Width = WidthFromRatio(header.Height, header.AspectRatioCode);
        }

        if (!bits.Valid)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.codestream.size.truncated", "JPEG XL codestream size header was truncated.", "Codestream", sourceOffset, bytes.Length);
            return header;
        }

        header.SizeValid = true;
        header.HeaderBitLength = bits.BitPosition;
        header.HeaderByteLength = (bits.BitPosition + 7) / 8;
        JpegXlBasicInfoParser.Parse(document, bits, header);
        return header;
    }

    internal static uint ReadSizeValue(JpegXlBitReader bits, bool small)
    {
        if (small)
        {
            return (bits.ReadBits(5) + 1) * 8;
        }

        uint selector = bits.ReadBits(2);
        int count = 9;
        if (selector == 1)
        {
            count = 13;
        }
        else if (selector == 2)
        {
            count = 18;
        }
        else if (selector == 3)
        {
            count = 30;
        }

        return bits.ReadBits(count) + 1;
    }

    internal static uint WidthFromRatio(uint height, int code)
    {
        uint numerator = 1;
        uint denominator = 1;
        if (code == 2)
        {
            numerator = 12;
            denominator = 10;
        }
        else if (code == 3)
        {
            numerator = 4;
            denominator = 3;
        }
        else if (code == 4)
        {
            numerator = 3;
            denominator = 2;
        }
        else if (code == 5)
        {
            numerator = 16;
            denominator = 9;
        }
        else if (code == 6)
        {
            numerator = 5;
            denominator = 4;
        }
        else if (code == 7)
        {
            numerator = 2;
        }

        return (uint)(((ulong)height * numerator) / denominator);
    }

    internal static void AddBoxDirectories(ImageMetadataDocument document, List<JpegXlBox> boxes, MetadataDirectory? parent, string parentPath)
    {
        if (document == null || boxes == null)
        {
            return;
        }

        foreach (JpegXlBox box in boxes)
        {
            MetadataDirectory directory = MetadataDirectory.Create(MetadataFamily.JpegXl, box.Type, box.Type + " Box");
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

    internal static void AddBoxTags(MetadataDirectory directory, JpegXlBox box)
    {
        string prefix = "JpegXl." + box.Type;
        long sizeOffset = box.UsesLargeSize ? box.Offset + BasicHeaderSize : box.Offset;
        long sizeLength = box.UsesLargeSize ? LargeSizeFieldSize : 4;
        AddTag(directory, prefix + ".Index", "Index", MetadataValue.FromInteger(box.Index), box.Offset, BasicHeaderSize, "");
        AddTag(directory, prefix + ".Type", "Type", MetadataValue.FromString(box.Type), box.Offset + 4, 4, box.Type);
        AddTag(directory, prefix + ".Size", "Size", MetadataValue.FromUnsignedInteger(box.Size), sizeOffset, sizeLength, "");
        AddTag(directory, prefix + ".HeaderSize", "Header Size", MetadataValue.FromInteger(box.HeaderSize), box.Offset, box.HeaderSize, "");
        AddTag(directory, prefix + ".DataOffset", "Data Offset", MetadataValue.FromInteger(box.DataOffset), box.Offset, box.HeaderSize, "");
        AddTag(directory, prefix + ".DataSize", "Data Size", MetadataValue.FromUnsignedInteger(box.DataSize), box.DataOffset, ToLong(box.DataSize), "");
        AddTag(directory, prefix + ".Depth", "Depth", MetadataValue.FromInteger(box.Depth), box.Offset, box.HeaderSize, "");
        AddTag(directory, prefix + ".UsesLargeSize", "Uses Large Size", MetadataValue.FromBoolean(box.UsesLargeSize), box.Offset, 4, "");
        AddTag(directory, prefix + ".ExtendsToEnd", "Extends To End", MetadataValue.FromBoolean(box.ExtendsToEnd), box.Offset, 4, "");
        AddTag(directory, prefix + ".TypeValid", "Type Valid", MetadataValue.FromBoolean(box.TypeValid), box.Offset + 4, 4, "");
        AddTag(directory, prefix + ".DataLimited", "Data Limited", MetadataValue.FromBoolean(box.DataLimited), box.DataOffset, ToLong(box.DataSize), "");
        if (box.PartialIndexValid)
        {
            AddTag(directory, prefix + ".PartialIndex", "Partial Index", MetadataValue.FromUnsignedInteger(box.PartialIndex), box.DataOffset, PartialIndexSize, "");
            AddTag(directory, prefix + ".PartialIsLast", "Partial Is Last", MetadataValue.FromBoolean(box.PartialIsLast), box.DataOffset, PartialIndexSize, "");
        }

        if (box.Type == "ftyp" && box.MajorBrand.Length > 0)
        {
            AddTag(directory, prefix + ".MajorBrand", "Major Brand", MetadataValue.FromString(box.MajorBrand), box.DataOffset, 4, box.MajorBrand);
            AddTag(directory, prefix + ".MinorVersion", "Minor Version", MetadataValue.FromUnsignedInteger(box.MinorVersion), box.DataOffset + 4, 4, "");
            int index = 0;
            foreach (string brand in box.CompatibleBrands)
            {
                AddTag(directory, prefix + ".CompatibleBrand", "Compatible Brand", MetadataValue.FromString(brand), box.DataOffset + 8 + index * 4, 4, brand);
                index = index + 1;
            }
        }
    }

    internal static void AddCodestreamDirectory(ImageMetadataDocument document, JpegXlCodestreamHeader header, string containerKind)
    {
        if (document == null || header == null)
        {
            return;
        }

        MetadataDirectory directory = MetadataDirectory.Create(MetadataFamily.JpegXl, "Codestream", "JPEG XL Codestream Header");
        directory.Path = "JXL/Codestream";
        directory.Offset = header.SourceOffset;
        directory.Length = header.HeaderByteLength;
        document.AddDirectory(directory);
        AddTag(directory, "JpegXl.Codestream.ContainerKind", "Container Kind", MetadataValue.FromString(containerKind), header.SourceOffset, 0, containerKind);
        AddTag(directory, "JpegXl.Codestream.Fragmented", "Fragmented", MetadataValue.FromBoolean(header.Fragmented), header.SourceOffset, 0, "");
        AddTag(directory, "JpegXl.Codestream.MarkerValid", "Marker Valid", MetadataValue.FromBoolean(header.MarkerValid), header.SourceOffset, 2, "");
        if (header.MarkerValid)
        {
            AddTag(directory, "JpegXl.Codestream.Marker", "Marker", MetadataValue.FromBytes(new byte[] { 255, 10 }), header.SourceOffset, 2, "ff0a");
        }

        if (header.SizeValid)
        {
            AddTag(directory, "JpegXl.Codestream.SmallSizeHeader", "Small Size Header", MetadataValue.FromBoolean(header.SmallSize), header.SourceOffset + 2, header.HeaderByteLength - 2, "");
            AddTag(directory, "JpegXl.Codestream.AspectRatioCode", "Aspect Ratio Code", MetadataValue.FromInteger(header.AspectRatioCode), header.SourceOffset + 2, header.HeaderByteLength - 2, "");
            AddTag(directory, "JpegXl.Codestream.Width", "Encoded Width", MetadataValue.FromUnsignedInteger(header.Width), header.SourceOffset + 2, header.HeaderByteLength - 2, "");
            AddTag(directory, "JpegXl.Codestream.Height", "Encoded Height", MetadataValue.FromUnsignedInteger(header.Height), header.SourceOffset + 2, header.HeaderByteLength - 2, "");
            AddTag(directory, "JpegXl.Codestream.HeaderBitLength", "Header Bit Length", MetadataValue.FromInteger(header.HeaderBitLength), header.SourceOffset, header.HeaderByteLength, "");
            AddTag(directory, "JpegXl.Codestream.HeaderByteLength", "Header Byte Length", MetadataValue.FromInteger(header.HeaderByteLength), header.SourceOffset, header.HeaderByteLength, "");
        }

        if (header.MetadataValid)
        {
            AddTag(directory, "JpegXl.Codestream.MetadataAllDefault", "Metadata All Default", MetadataValue.FromBoolean(header.MetadataAllDefault), header.SourceOffset, header.MetadataByteLength, "");
            AddTag(directory, "JpegXl.Codestream.Orientation", "Orientation", MetadataValue.FromUnsignedInteger(header.Orientation), header.SourceOffset, header.MetadataByteLength, OrientationName(header.Orientation));
            AddTag(directory, "JpegXl.Codestream.BitsPerSample", "Bits Per Sample", MetadataValue.FromUnsignedInteger(header.BitsPerSample), header.SourceOffset, header.MetadataByteLength, "");
            AddTag(directory, "JpegXl.Codestream.ExponentBitsPerSample", "Exponent Bits Per Sample", MetadataValue.FromUnsignedInteger(header.ExponentBitsPerSample), header.SourceOffset, header.MetadataByteLength, "");
            AddTag(directory, "JpegXl.Codestream.FloatingPointSamples", "Floating Point Samples", MetadataValue.FromBoolean(header.FloatingPointSamples), header.SourceOffset, header.MetadataByteLength, "");
            AddTag(directory, "JpegXl.Codestream.ExtraChannelCount", "Extra Channel Count", MetadataValue.FromUnsignedInteger(header.ExtraChannelCount), header.SourceOffset, header.MetadataByteLength, "");
            AddTag(directory, "JpegXl.Codestream.HasAlpha", "Has Alpha", MetadataValue.FromBoolean(header.HasAlpha), header.SourceOffset, header.MetadataByteLength, "");
            if (header.HasAlpha)
            {
                AddTag(directory, "JpegXl.Codestream.AlphaBitsPerSample", "Alpha Bits Per Sample", MetadataValue.FromUnsignedInteger(header.AlphaBitsPerSample), header.SourceOffset, header.MetadataByteLength, "");
                AddTag(directory, "JpegXl.Codestream.AlphaPremultiplied", "Alpha Premultiplied", MetadataValue.FromBoolean(header.AlphaPremultiplied), header.SourceOffset, header.MetadataByteLength, "");
            }

            AddTag(directory, "JpegXl.Codestream.XybEncoded", "XYB Encoded", MetadataValue.FromBoolean(header.XybEncoded), header.SourceOffset, header.MetadataByteLength, "");
            AddTag(directory, "JpegXl.Codestream.WantIcc", "Embedded ICC Profile", MetadataValue.FromBoolean(header.WantIcc), header.SourceOffset, header.MetadataByteLength, "");
            AddTag(directory, "JpegXl.Codestream.ColorSpace", "Color Space", MetadataValue.FromUnsignedInteger(header.ColorSpace), header.SourceOffset, header.MetadataByteLength, ColorSpaceName(header.ColorSpace));
            if (!header.WantIcc)
            {
                AddTag(directory, "JpegXl.Codestream.WhitePoint", "White Point", MetadataValue.FromUnsignedInteger(header.WhitePoint), header.SourceOffset, header.MetadataByteLength, WhitePointName(header.WhitePoint));
                AddTag(directory, "JpegXl.Codestream.Primaries", "Primaries", MetadataValue.FromUnsignedInteger(header.Primaries), header.SourceOffset, header.MetadataByteLength, PrimariesName(header.Primaries));
                AddTag(directory, "JpegXl.Codestream.TransferFunction", "Transfer Function", MetadataValue.FromUnsignedInteger(header.TransferFunction), header.SourceOffset, header.MetadataByteLength, TransferFunctionName(header.TransferFunction, header.HaveGamma));
                if (header.HaveGamma)
                {
                    AddTag(directory, "JpegXl.Codestream.Gamma", "Gamma", MetadataValue.FromRational(header.Gamma, 10000000), header.SourceOffset, header.MetadataByteLength, "");
                }

                AddTag(directory, "JpegXl.Codestream.RenderingIntent", "Rendering Intent", MetadataValue.FromUnsignedInteger(header.RenderingIntent), header.SourceOffset, header.MetadataByteLength, RenderingIntentName(header.RenderingIntent));
            }
            AddTag(directory, "JpegXl.Codestream.HaveIntrinsicSize", "Has Intrinsic Size", MetadataValue.FromBoolean(header.HaveIntrinsicSize), header.SourceOffset, header.MetadataByteLength, "");
            if (header.HaveIntrinsicSize)
            {
                AddTag(directory, "JpegXl.Codestream.IntrinsicWidth", "Intrinsic Width", MetadataValue.FromUnsignedInteger(header.IntrinsicWidth), header.SourceOffset, header.MetadataByteLength, "");
                AddTag(directory, "JpegXl.Codestream.IntrinsicHeight", "Intrinsic Height", MetadataValue.FromUnsignedInteger(header.IntrinsicHeight), header.SourceOffset, header.MetadataByteLength, "");
            }

            AddTag(directory, "JpegXl.Codestream.HavePreview", "Has Preview", MetadataValue.FromBoolean(header.HavePreview), header.SourceOffset, header.MetadataByteLength, "");
            if (header.HavePreview)
            {
                AddTag(directory, "JpegXl.Codestream.PreviewWidth", "Preview Width", MetadataValue.FromUnsignedInteger(header.PreviewWidth), header.SourceOffset, header.MetadataByteLength, "");
                AddTag(directory, "JpegXl.Codestream.PreviewHeight", "Preview Height", MetadataValue.FromUnsignedInteger(header.PreviewHeight), header.SourceOffset, header.MetadataByteLength, "");
            }

            AddTag(directory, "JpegXl.Codestream.HaveAnimation", "Animated", MetadataValue.FromBoolean(header.HaveAnimation), header.SourceOffset, header.MetadataByteLength, "");
            if (header.HaveAnimation)
            {
                AddTag(directory, "JpegXl.Codestream.TicksPerSecondNumerator", "Ticks Per Second Numerator", MetadataValue.FromUnsignedInteger(header.TicksPerSecondNumerator), header.SourceOffset, header.MetadataByteLength, "");
                AddTag(directory, "JpegXl.Codestream.TicksPerSecondDenominator", "Ticks Per Second Denominator", MetadataValue.FromUnsignedInteger(header.TicksPerSecondDenominator), header.SourceOffset, header.MetadataByteLength, "");
                AddTag(directory, "JpegXl.Codestream.LoopCount", "Loop Count", MetadataValue.FromUnsignedInteger(header.LoopCount), header.SourceOffset, header.MetadataByteLength, header.LoopCount == 0 ? "Infinite" : header.LoopCount.ToString());
                AddTag(directory, "JpegXl.Codestream.HaveTimecodes", "Has Timecodes", MetadataValue.FromBoolean(header.HaveTimecodes), header.SourceOffset, header.MetadataByteLength, "");
            }
        }
    }

    internal static string OrientationName(uint value)
    {
        if (value == 1) return "Normal";
        if (value == 2) return "Mirror Horizontal";
        if (value == 3) return "Rotate 180";
        if (value == 4) return "Mirror Vertical";
        if (value == 5) return "Transpose";
        if (value == 6) return "Rotate 90 CW";
        if (value == 7) return "Anti-Transpose";
        if (value == 8) return "Rotate 270 CW";
        return "Unknown";
    }

    internal static string ColorSpaceName(uint value)
    {
        if (value == 0) return "RGB";
        if (value == 1) return "Gray";
        if (value == 2) return "XYB";
        return "Unknown";
    }

    internal static string WhitePointName(uint value)
    {
        if (value == 1) return "D65";
        if (value == 2) return "Custom";
        if (value == 10) return "E";
        if (value == 11) return "DCI";
        return "Unknown";
    }

    internal static string PrimariesName(uint value)
    {
        if (value == 1) return "sRGB";
        if (value == 2) return "Custom";
        if (value == 9) return "BT.2100";
        if (value == 11) return "P3";
        return "Unknown";
    }

    internal static string TransferFunctionName(uint value, bool gamma)
    {
        if (gamma) return "Gamma";
        if (value == 1) return "BT.709";
        if (value == 8) return "Linear";
        if (value == 13) return "sRGB";
        if (value == 16) return "PQ";
        if (value == 17) return "DCI";
        if (value == 18) return "HLG";
        return "Unknown";
    }

    internal static string RenderingIntentName(uint value)
    {
        if (value == 0) return "Perceptual";
        if (value == 1) return "Relative";
        if (value == 2) return "Saturation";
        if (value == 3) return "Absolute";
        return "Unknown";
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string displayValue)
    {
        MetadataTag tag = directory.AddTagWithValue(key, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = displayValue;
        return tag;
    }

    internal static bool BoxFitsRange(JpegXlBox box, long end)
    {
        if (box == null || box.Offset < 0 || end < box.Offset)
        {
            return false;
        }

        return box.Size <= (ulong)(end - box.Offset);
    }

    internal static int CountType(List<JpegXlBox> boxes, string type)
    {
        int count = 0;
        foreach (JpegXlBox box in boxes)
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

        return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
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
        return value < 0 ? 0 : (ulong)value;
    }

    internal static long ToLong(ulong value)
    {
        return value > 9223372036854775807 ? 9223372036854775807 : (long)value;
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
