using System.Collections.Generic;
using System.Text;

namespace image;

internal class EpsDscComment
{
    internal string Name;
    internal string Value;
    internal long Offset;
    internal long Length;
    internal bool Trailer;

    internal EpsDscComment()
    {
        Name = "";
        Value = "";
        Offset = -1;
        Length = 0;
        Trailer = false;
    }
}

internal class EpsLine
{
    internal string Text;
    internal long Offset;
    internal long Length;
    internal long NextOffset;

    internal EpsLine()
    {
        Text = "";
        Offset = -1;
        Length = 0;
        NextOffset = -1;
    }
}

internal class EpsSourceRange
{
    internal long Offset;
    internal long Length;
    internal bool BinaryWrapper;
    internal long WmfOffset;
    internal long WmfLength;
    internal long TiffOffset;
    internal long TiffLength;
    internal int Checksum;
}

internal class EpsParserState
{
    internal byte[] Data;
    internal EpsSourceRange Source;
    internal string Signature;
    internal string XmpHint;
    internal long HeaderEnd;
    internal List<EpsDscComment> Comments;

    internal EpsParserState()
    {
        Data = new byte[0];
        Source = new EpsSourceRange();
        Signature = "";
        XmpHint = "";
        HeaderEnd = -1;
        Comments = new List<EpsDscComment>();
    }
}

internal class EpsMetadataParser
{
    internal const int BinaryHeaderLength = 30;
    internal const long BinaryMagic = 3335770309;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "eps.reader.null", "EPS parser received a null reader.");
            return document;
        }
        document.ByteLength = reader.Length;
        long maximum = MaxSegmentBytes(options);
        if (maximum < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "eps.segment.limit.negative", "EPS segment limit was negative.", "EPS", 0, maximum);
            return Finish(document, reader);
        }
        if (reader.AccessibleLength() > maximum || reader.AccessibleLength() > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "eps.source.limit", "EPS source exceeded the configured segment limit.", "EPS", 0, reader.AccessibleLength());
            return Finish(document, reader);
        }
        byte[] data = ReadRange(reader, 0, (int)reader.AccessibleLength());
        EpsSourceRange? source = ResolveSource(document, data);
        if (source == null)
        {
            return Finish(document, reader);
        }
        if (source.BinaryWrapper)
        {
            AddBinaryHeaderDirectory(document, source);
            CaptureBlock(document, reader, options, MetadataFamily.Format, "BinaryHeader", "EPS Binary Preview Header", 0, BinaryHeaderLength);
        }

        EpsParserState state = new EpsParserState();
        state.Data = data;
        state.Source = source;
        if (!ParseDsc(document, state, options))
        {
            return Finish(document, reader);
        }
        AddDscDirectory(document, state);
        AddFrame(document, state);
        CaptureDscHeader(document, reader, options, state);
        ExtractXmp(document, reader, state, options);
        ImageMetadataNormalizerCore.BuildSummary(document);
        return Finish(document, reader);
    }

    internal static EpsSourceRange? ResolveSource(ImageMetadataDocument document, byte[] data)
    {
        EpsSourceRange source = new EpsSourceRange();
        if (StartsWith(data, 0, "%!PS"))
        {
            source.Offset = 0;
            source.Length = data.Length;
            return source;
        }
        if (data.Length < BinaryHeaderLength || ReadUnsigned32LittleEndian(data, 0) != BinaryMagic)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "eps.signature.invalid", "EPS PostScript or binary-preview signature was invalid.", "EPS", 0, PrefixLength(data.Length, 4));
            return null;
        }
        source.BinaryWrapper = true;
        source.Offset = ReadUnsigned32LittleEndian(data, 4);
        source.Length = ReadUnsigned32LittleEndian(data, 8);
        source.WmfOffset = ReadUnsigned32LittleEndian(data, 12);
        source.WmfLength = ReadUnsigned32LittleEndian(data, 16);
        source.TiffOffset = ReadUnsigned32LittleEndian(data, 20);
        source.TiffLength = ReadUnsigned32LittleEndian(data, 24);
        source.Checksum = ReadUnsigned16LittleEndian(data, 28);
        if (!RangeFits(source.Offset, source.Length, data.Length) || source.Length < 4 || !StartsWith(data, (int)source.Offset, "%!PS"))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "eps.binary.postscript.range", "EPS binary wrapper did not contain a valid declared PostScript section.", "EPS Binary Header", source.Offset, source.Length);
            return null;
        }
        ValidatePreviewRange(document, data.Length, source.WmfOffset, source.WmfLength, "WMF");
        ValidatePreviewRange(document, data.Length, source.TiffOffset, source.TiffLength, "TIFF");
        return source;
    }

    internal static void ValidatePreviewRange(ImageMetadataDocument document, long boundary, long offset, long length, string type)
    {
        if (length != 0 && !RangeFits(offset, length, boundary))
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "eps.binary.preview.range", "EPS binary " + type + " preview range exceeded the source.", "EPS Binary Header", offset, length);
        }
    }

    internal static bool ParseDsc(ImageMetadataDocument document, EpsParserState state, ImageMetadataReaderOptions options)
    {
        long end = state.Source.Offset + state.Source.Length;
        EpsLine first = ReadLine(state.Data, state.Source.Offset, end);
        state.Signature = first.Text;
        if (!first.Text.StartsWith("%!PS-Adobe-", System.StringComparison.Ordinal))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "eps.postscript.signature.invalid", "EPS PostScript section did not begin with a DSC signature.", "EPS DSC", first.Offset, first.Length);
            return false;
        }
        if (first.Text.IndexOf("EPSF-", System.StringComparison.Ordinal) < 0)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "eps.signature.epsf.missing", "PostScript DSC signature did not declare EPSF conformance.", "EPS DSC", first.Offset, first.Length);
        }
        long position = first.NextOffset;
        bool trailer = false;
        EpsDscComment? previous = null;
        int count = 0;
        while (position < end)
        {
            EpsLine line = ReadLine(state.Data, position, end);
            position = line.NextOffset;
            if (line.Text == "%%Trailer") trailer = true;
            if (line.Text == "%%EndComments" && state.HeaderEnd < 0) state.HeaderEnd = line.NextOffset;
            if (line.Text.StartsWith("%ADO_ContainsXMP:", System.StringComparison.Ordinal))
            {
                state.XmpHint = line.Text.Substring(17).Trim();
            }
            if (line.Text.StartsWith("%%+", System.StringComparison.Ordinal))
            {
                if (previous == null)
                {
                    MetadataDiagnostic.AddWarningAt(document.Diagnostics, "eps.dsc.continuation.orphan", "EPS DSC continuation did not follow a DSC comment.", "EPS DSC", line.Offset, line.Length);
                }
                else
                {
                    string continuation = line.Text.Substring(3).Trim();
                    if (continuation.Length > 0) previous.Value = previous.Value + " " + continuation;
                    previous.Length = line.NextOffset - previous.Offset;
                }
                continue;
            }
            EpsDscComment? comment = ParseComment(line, trailer);
            if (comment != null)
            {
                if (count >= MaxTagCount(options))
                {
                    MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "eps.dsc.tag.limit", "EPS DSC comments exceeded the configured tag limit.", "EPS DSC", line.Offset, line.Length);
                    break;
                }
                state.Comments.Add(comment);
                previous = comment;
                count = count + 1;
                long skip = DataSkipLength(comment);
                if (skip > 0)
                {
                    if (!RangeFits(position, skip, end))
                    {
                        MetadataDiagnostic.AddTruncated(document.Diagnostics, "eps.dsc.data.truncated", "EPS DSC binary or data block exceeded the PostScript section.", "EPS DSC Data", position, skip);
                        position = end;
                    }
                    else
                    {
                        position = position + skip;
                    }
                }
                else if (IsLineData(comment))
                {
                    int lines = FirstPositiveInteger(comment.Value);
                    int index = 0;
                    while (index < lines && position < end)
                    {
                        EpsLine skipped = ReadLine(state.Data, position, end);
                        position = skipped.NextOffset;
                        index = index + 1;
                    }
                    if (index < lines)
                    {
                        MetadataDiagnostic.AddTruncated(document.Diagnostics, "eps.dsc.data.truncated", "EPS DSC line data block was truncated.", "EPS DSC Data", position, lines - index);
                    }
                }
            }
            else
            {
                previous = null;
            }
        }
        if (state.HeaderEnd < 0)
        {
            state.HeaderEnd = first.NextOffset;
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "eps.dsc.end-comments.missing", "EPS DSC header did not contain %%EndComments.", "EPS DSC", state.Source.Offset, first.Length);
        }
        return true;
    }

    internal static EpsDscComment? ParseComment(EpsLine line, bool trailer)
    {
        if (!line.Text.StartsWith("%%", System.StringComparison.Ordinal) || line.Text.Length <= 2)
        {
            return null;
        }
        int separator = line.Text.IndexOf(':');
        string name;
        string value;
        if (separator < 0)
        {
            name = line.Text.Substring(2).Trim();
            value = "";
        }
        else
        {
            name = line.Text.Substring(2, separator - 2).Trim();
            value = line.Text.Substring(separator + 1).Trim();
        }
        if (name.Length == 0)
        {
            return null;
        }
        EpsDscComment comment = new EpsDscComment();
        comment.Name = name;
        comment.Value = value;
        comment.Offset = line.Offset;
        comment.Length = line.Length;
        comment.Trailer = trailer;
        return comment;
    }

    internal static void AddBinaryHeaderDirectory(ImageMetadataDocument document, EpsSourceRange source)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "BinaryHeader", "EPS Binary Preview Header");
        directory.Path = "EPS/BinaryHeader";
        directory.Offset = 0;
        directory.Length = BinaryHeaderLength;
        AddTag(directory, "Binary.PostScriptOffset", "PostScript Offset", MetadataValue.FromInteger(source.Offset), 4, 4, "bytes");
        AddTag(directory, "Binary.PostScriptLength", "PostScript Length", MetadataValue.FromInteger(source.Length), 8, 4, "bytes");
        AddTag(directory, "Binary.WmfOffset", "WMF Preview Offset", MetadataValue.FromInteger(source.WmfOffset), 12, 4, "bytes");
        AddTag(directory, "Binary.WmfLength", "WMF Preview Length", MetadataValue.FromInteger(source.WmfLength), 16, 4, "bytes");
        AddTag(directory, "Binary.TiffOffset", "TIFF Preview Offset", MetadataValue.FromInteger(source.TiffOffset), 20, 4, "bytes");
        AddTag(directory, "Binary.TiffLength", "TIFF Preview Length", MetadataValue.FromInteger(source.TiffLength), 24, 4, "bytes");
        AddTag(directory, "Binary.Checksum", "Header Checksum", MetadataValue.FromInteger(source.Checksum), 28, 2, source.Checksum == 65535 ? "Not provided" : source.Checksum.ToString());
    }

    internal static void AddDscDirectory(ImageMetadataDocument document, EpsParserState state)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "DSC", "EPS Document Structuring Comments");
        directory.Path = "EPS/DSC";
        directory.Offset = state.Source.Offset;
        directory.Length = state.HeaderEnd - state.Source.Offset;
        AddTag(directory, "DSC.Signature", "DSC Signature", MetadataValue.FromString(state.Signature), state.Source.Offset, state.Signature.Length, state.Signature);
        if (state.XmpHint.Length > 0)
        {
            AddTag(directory, "XMP.LocationHint", "XMP Location Hint", MetadataValue.FromString(state.XmpHint), state.Source.Offset, directory.Length, state.XmpHint);
        }
        foreach (EpsDscComment comment in state.Comments)
        {
            MetadataValue value = MetadataValue.FromString(DecodePostScriptText(comment.Value));
            MetadataTag tag = AddTag(directory, "DSC.Comment." + SanitizeName(comment.Name), comment.Name, value, comment.Offset, comment.Length, comment.Trailer ? "Trailer: " + value.Text : value.Text);
            tag.Confidence = comment.Trailer ? 95 : 100;
        }
        AddEffectiveText(directory, state, "Title", "Title", false);
        AddEffectiveText(directory, state, "Creator", "Creator", false);
        AddEffectiveText(directory, state, "For", "Intended Recipient", false);
        AddEffectiveText(directory, state, "CreationDate", "Creation Date", true);
        AddEffectiveText(directory, state, "Orientation", "Orientation", false);
        AddEffectiveText(directory, state, "DocumentData", "Document Data", false);
        AddEffectiveInteger(document, directory, state, "Pages", "Page Count");
        AddEffectiveInteger(document, directory, state, "LanguageLevel", "PostScript Language Level");
        AddEffectiveResources(directory, state, "DocumentFonts", "Document Fonts");
        AddEffectiveResources(directory, state, "DocumentNeededResources", "Needed Resources");
        AddEffectiveResources(directory, state, "DocumentSuppliedResources", "Supplied Resources");
        AddBoundingBox(document, directory, state, "BoundingBox", false);
        AddBoundingBox(document, directory, state, "HiResBoundingBox", true);
    }

    internal static void AddEffectiveText(MetadataDirectory directory, EpsParserState state, string name, string displayName, bool date)
    {
        EpsDscComment? comment = FindEffective(state, name);
        if (comment == null) return;
        string text = DecodePostScriptText(comment.Value);
        MetadataValue value = date ? MetadataValue.FromDateText(text) : MetadataValue.FromString(text);
        AddTag(directory, "DSC." + name, displayName, value, comment.Offset, comment.Length, text);
    }

    internal static void AddEffectiveInteger(ImageMetadataDocument document, MetadataDirectory directory, EpsParserState state, string name, string displayName)
    {
        EpsDscComment? comment = FindEffective(state, name);
        if (comment == null) return;
        int value = FirstPositiveInteger(comment.Value);
        if (value <= 0)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "eps.dsc." + name.ToLowerInvariant() + ".invalid", "EPS DSC " + name + " value was invalid.", "EPS DSC", comment.Offset, comment.Length);
            return;
        }
        AddTag(directory, "DSC." + name, displayName, MetadataValue.FromInteger(value), comment.Offset, comment.Length, "");
    }

    internal static void AddEffectiveResources(MetadataDirectory directory, EpsParserState state, string name, string displayName)
    {
        EpsDscComment? comment = FindEffective(state, name);
        if (comment == null) return;
        string[] parts = comment.Value.Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        MetadataValue value = new MetadataValue();
        value.Kind = MetadataValueKind.StringList;
        int index = 0;
        while (index < parts.Length)
        {
            value.AddTextValue(DecodePostScriptText(parts[index]));
            index = index + 1;
        }
        AddTag(directory, "DSC." + name, displayName, value, comment.Offset, comment.Length, value.TextValues.Length.ToString() + " values");
    }

    internal static void AddBoundingBox(ImageMetadataDocument document, MetadataDirectory directory, EpsParserState state, string name, bool highResolution)
    {
        EpsDscComment? comment = FindEffective(state, name);
        if (comment == null) return;
        double[] values = RadianceHdrMetadataParser.ParseNumberList(comment.Value, 4);
        if (values.Length != 4 || values[2] <= values[0] || values[3] <= values[1])
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, highResolution ? "eps.dsc.hires-bounding-box.invalid" : "eps.dsc.bounding-box.invalid", "EPS DSC " + name + " value was invalid.", "EPS DSC", comment.Offset, comment.Length);
            return;
        }
        string prefix = "DSC." + name + ".";
        AddTag(directory, prefix + "LowerLeftX", "Lower Left X", MetadataValue.FromDouble(values[0]), comment.Offset, comment.Length, "points");
        AddTag(directory, prefix + "LowerLeftY", "Lower Left Y", MetadataValue.FromDouble(values[1]), comment.Offset, comment.Length, "points");
        AddTag(directory, prefix + "UpperRightX", "Upper Right X", MetadataValue.FromDouble(values[2]), comment.Offset, comment.Length, "points");
        AddTag(directory, prefix + "UpperRightY", "Upper Right Y", MetadataValue.FromDouble(values[3]), comment.Offset, comment.Length, "points");
        AddTag(directory, prefix + "Width", "Bounding Width", MetadataValue.FromDouble(values[2] - values[0]), comment.Offset, comment.Length, "points");
        AddTag(directory, prefix + "Height", "Bounding Height", MetadataValue.FromDouble(values[3] - values[1]), comment.Offset, comment.Length, "points");
    }

    internal static void AddFrame(ImageMetadataDocument document, EpsParserState state)
    {
        EpsDscComment? comment = FindEffective(state, "HiResBoundingBox");
        if (comment == null) comment = FindEffective(state, "BoundingBox");
        if (comment == null) return;
        double[] values = RadianceHdrMetadataParser.ParseNumberList(comment.Value, 4);
        if (values.Length != 4 || values[2] <= values[0] || values[3] <= values[1]) return;
        double width = values[2] - values[0];
        double height = values[3] - values[1];
        if (width > 2147483647 || height > 2147483647) return;
        ImageMetadataFrame frame = document.AddFrameWithDimensions((int)System.Math.Ceiling(width), (int)System.Math.Ceiling(height));
        frame.ColorSpace = ImageColorSpaceKind.Unknown;
        frame.Compression = ImageCompressionKind.None;
        frame.Orientation = ImageOrientation.Normal;
    }

    internal static EpsDscComment? FindEffective(EpsParserState state, string name)
    {
        EpsDscComment? result = null;
        foreach (EpsDscComment comment in state.Comments)
        {
            if (comment.Name == name && comment.Value != "(atend)") result = comment;
        }
        return result;
    }

    internal static void ExtractXmp(ImageMetadataDocument document, BoundedStreamReader reader, EpsParserState state, ImageMetadataReaderOptions? options)
    {
        if (options != null && !options.ParseXmp) return;
        List<long> starts = FindAll(state.Data, (int)state.Source.Offset, (int)(state.Source.Offset + state.Source.Length), "<?xpacket begin=");
        if (starts.Count == 0)
        {
            if (state.XmpHint == "MainFirst" || state.XmpHint == "MainLast")
            {
                MetadataDiagnostic.AddWarning(document.Diagnostics, "eps.xmp.packet.missing", "EPS XMP marker declared a main packet, but no XMP packet was found.");
            }
            return;
        }
        if (state.XmpHint == "NoMain") return;
        long start = state.XmpHint == "MainLast" ? starts[starts.Count - 1] : starts[0];
        long endingStart = FindBytes(state.Data, (int)start, (int)(state.Source.Offset + state.Source.Length), "<?xpacket end=");
        if (endingStart < 0)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "eps.xmp.packet.truncated", "EPS XMP packet did not contain an ending packet instruction.", "EPS XMP", start, state.Source.Offset + state.Source.Length - start);
            return;
        }
        long end = FindBytes(state.Data, (int)endingStart, (int)(state.Source.Offset + state.Source.Length), "?>");
        if (end < 0)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "eps.xmp.packet.truncated", "EPS XMP ending packet instruction was truncated.", "EPS XMP", endingStart, state.Source.Offset + state.Source.Length - endingStart);
            return;
        }
        long length = end + 2 - start;
        if (length > MaxProfileBytes(options) || length > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "eps.xmp.profile.limit", "EPS XMP packet exceeded the configured profile limit.", "EPS XMP", start, length);
            return;
        }
        byte[] packet = CopyBytes(state.Data, (int)start, (int)length);
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Xmp;
        profile.Family = MetadataFamily.Xmp;
        profile.Key = "Xmp.EPS";
        profile.Name = "EPS XMP Metadata";
        profile.MimeType = "application/rdf+xml";
        profile.Offset = start;
        profile.Length = length;
        profile.Data = packet;
        profile.Text = MetadataXmlReader.DecodeXmlBytes(packet, MaxXmlBytes(options), document.Diagnostics, "EPS XMP");
        document.AddProfile(profile);
        CaptureBlock(document, reader, options, MetadataFamily.Xmp, "XMP", "EPS XMP Metadata", start, (int)length);
        XmpPacketParser.ParseProfile(document, profile, options, "EPS/XMP");
    }

    internal static void CaptureDscHeader(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, EpsParserState state)
    {
        long length = state.HeaderEnd - state.Source.Offset;
        if (length < 0 || length > 2147483647) return;
        CaptureBlock(document, reader, options, MetadataFamily.Format, "DSCHeader", "EPS DSC Header", state.Source.Offset, (int)length);
    }

    internal static void CaptureBlock(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions? options, MetadataFamily family, string blockType, string name, long offset, int length)
    {
        if (!CaptureRawBlocks(options)) return;
        MetadataRawBlock block = document.AddRawBlockWithRange(family, blockType, name, offset, length);
        if (!CaptureRawBlockData(options)) return;
        if (length > MaxRawBlockBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "eps.raw.block.limit", "EPS raw block exceeded the configured raw block limit.", blockType, offset, length);
            return;
        }
        block.Data = ReadRange(reader, offset, length);
    }

    internal static EpsLine ReadLine(byte[] data, long offset, long end)
    {
        EpsLine line = new EpsLine();
        line.Offset = offset;
        int position = (int)offset;
        int boundary = (int)end;
        while (position < boundary && data[position] != 10 && data[position] != 13) position = position + 1;
        line.Text = Encoding.ASCII.GetString(data, (int)offset, position - (int)offset);
        line.Length = position - offset;
        if (position < boundary && data[position] == 13) position = position + 1;
        if (position < boundary && data[position] == 10) position = position + 1;
        line.NextOffset = position;
        return line;
    }

    internal static long DataSkipLength(EpsDscComment comment)
    {
        if (comment.Name == "BeginBinary") return FirstPositiveInteger(comment.Value);
        if (comment.Name == "BeginData" && comment.Value.IndexOf("Bytes", System.StringComparison.OrdinalIgnoreCase) >= 0) return FirstPositiveInteger(comment.Value);
        return 0;
    }

    internal static bool IsLineData(EpsDscComment comment)
    {
        return comment.Name == "BeginData" && comment.Value.IndexOf("Lines", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static int FirstPositiveInteger(string text)
    {
        if (text == null) return 0;
        int index = 0;
        while (index < text.Length && (text[index] < '0' || text[index] > '9')) index = index + 1;
        long value = 0;
        while (index < text.Length && text[index] >= '0' && text[index] <= '9')
        {
            int digit = text[index] - '0';
            if (value > (2147483647 - digit) / 10) return 0;
            value = value * 10 + digit;
            index = index + 1;
        }
        return (int)value;
    }

    internal static string DecodePostScriptText(string text)
    {
        if (text == null) return "";
        string value = text.Trim();
        if (value.Length >= 2 && value[0] == '(' && value[value.Length - 1] == ')')
        {
            value = value.Substring(1, value.Length - 2);
        }
        StringBuilder decoded = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\\' && index + 1 < value.Length)
            {
                char next = value[index + 1];
                if (next == 'n') decoded.Append('\n');
                else if (next == 'r') decoded.Append('\r');
                else if (next == 't') decoded.Append('\t');
                else decoded.Append(next);
                index = index + 2;
            }
            else
            {
                decoded.Append(value[index]);
                index = index + 1;
            }
        }
        return decoded.ToString();
    }

    internal static string SanitizeName(string name)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < name.Length)
        {
            char value = name[index];
            if ((value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z') || (value >= '0' && value <= '9') || value == '_' || value == '-') result.Append(value);
            else result.Append('_');
            index = index + 1;
        }
        return result.ToString();
    }

    internal static List<long> FindAll(byte[] data, int start, int end, string text)
    {
        List<long> positions = new List<long>();
        int position = start;
        while (position < end)
        {
            long found = FindBytes(data, position, end, text);
            if (found < 0) return positions;
            positions.Add(found);
            position = (int)found + text.Length;
        }
        return positions;
    }

    internal static long FindBytes(byte[] data, int start, int end, string text)
    {
        int position = start;
        while (position <= end - text.Length)
        {
            if (StartsWith(data, position, text)) return position;
            position = position + 1;
        }
        return -1;
    }

    internal static bool StartsWith(byte[] data, int offset, string text)
    {
        if (data == null || offset < 0 || offset > data.Length || text.Length > data.Length - offset) return false;
        int index = 0;
        while (index < text.Length)
        {
            if (data[offset + index] != (byte)text[index]) return false;
            index = index + 1;
        }
        return true;
    }

    internal static byte[] CopyBytes(byte[] data, int offset, int length)
    {
        byte[] copy = new byte[length];
        int index = 0;
        while (index < length)
        {
            copy[index] = data[offset + index];
            index = index + 1;
        }
        return copy;
    }

    internal static byte[] ReadRange(BoundedStreamReader reader, long offset, int length)
    {
        if (reader == null || length <= 0 || offset < 0 || offset > reader.AccessibleLength() || length > reader.AccessibleLength() - offset) return new byte[0];
        if (!reader.SeekAbsolute(offset)) return new byte[0];
        return reader.ReadBytes(length);
    }

    internal static bool RangeFits(long offset, long length, long boundary)
    {
        return offset >= 0 && length >= 0 && offset <= boundary && length <= boundary - offset;
    }

    internal static long ReadUnsigned32LittleEndian(byte[] data, int offset)
    {
        return (long)data[offset] | (long)data[offset + 1] << 8 | (long)data[offset + 2] << 16 | (long)data[offset + 3] << 24;
    }

    internal static int ReadUnsigned16LittleEndian(byte[] data, int offset)
    {
        return data[offset] | data[offset + 1] << 8;
    }

    internal static int PrefixLength(int length, int maximum)
    {
        return length < maximum ? length : maximum;
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string keyName, string name, MetadataValue value, long offset, long length, string display)
    {
        MetadataTag tag = directory.AddTagWithValue("Eps." + keyName, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = display;
        tag.Confidence = 100;
        return tag;
    }

    internal static long MaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
    }

    internal static long MaxProfileBytes(ImageMetadataReaderOptions? options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxProfileBytes : options.MaxProfileBytes;
    }

    internal static long MaxXmlBytes(ImageMetadataReaderOptions? options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxXmlBytes : options.MaxXmlBytes;
    }

    internal static long MaxRawBlockBytes(ImageMetadataReaderOptions? options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxRawBlockBytes : options.MaxRawBlockBytes;
    }

    internal static int MaxTagCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxTagCount : options.MaxTagCount;
    }

    internal static bool CaptureRawBlocks(ImageMetadataReaderOptions? options)
    {
        return options == null || options.CaptureRawBlocks;
    }

    internal static bool CaptureRawBlockData(ImageMetadataReaderOptions? options)
    {
        return options != null && options.CaptureRawBlockData;
    }

    internal static ImageMetadataDocument Finish(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (reader != null && reader.Diagnostics != null)
        {
            foreach (MetadataDiagnostic diagnostic in reader.Diagnostics) document.AddDiagnostic(diagnostic);
            reader.SeekAbsolute(0);
        }
        return document;
    }
}
