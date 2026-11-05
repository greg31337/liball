using System.Collections.Generic;
using System.Text;

namespace image;

internal class NetpbmToken
{
    internal string Text;
    internal long Offset;

    internal NetpbmToken()
    {
        Text = "";
        Offset = -1;
    }
}

internal class NetpbmHeader
{
    internal string Magic;
    internal string Variant;
    internal string Encoding;
    internal int Width;
    internal int Height;
    internal int Depth;
    internal int MaxValue;
    internal string TupleType;
    internal double ScaleFactor;
    internal string ByteOrder;
    internal long RasterOffset;
    internal long ExpectedRasterBytes;
    internal List<string> Comments;

    internal NetpbmHeader()
    {
        Magic = "";
        Variant = "";
        Encoding = "";
        Width = 0;
        Height = 0;
        Depth = 0;
        MaxValue = 0;
        TupleType = "";
        ScaleFactor = 0;
        ByteOrder = "";
        RasterOffset = 0;
        ExpectedRasterBytes = 0;
        Comments = new List<string>();
    }
}

internal class NetpbmMetadataParser
{
    internal const int MaximumMaxValue = 65535;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "netpbm.reader.null", "Netpbm parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        if (!reader.SeekAbsolute(0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "netpbm.header.seek", "Netpbm header could not be reached.", "Netpbm", 0, 0);
            return Finish(document, reader);
        }

        byte[] magicBytes = reader.ReadBytes(2);
        if (magicBytes.Length < 2)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "netpbm.magic.truncated", "Netpbm magic number was truncated.", "Netpbm", 0, 2);
            return Finish(document, reader);
        }

        string magic = new string(new char[] { (char)magicBytes[0], (char)magicBytes[1] });
        NetpbmHeader header = new NetpbmHeader();
        header.Magic = magic;
        bool parsed = false;
        if (magic == "P7")
        {
            parsed = ParsePam(document, reader, options, header);
        }
        else if (magic == "PF" || magic == "Pf")
        {
            parsed = ParsePfm(document, reader, options, header);
        }
        else if (magic.Length == 2 && magic[0] == 'P' && magic[1] >= '1' && magic[1] <= '6')
        {
            parsed = ParsePnm(document, reader, options, header);
        }
        else
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "netpbm.magic.invalid", "Netpbm magic number was invalid or unsupported.", "Netpbm", 0, 2);
        }

        if (parsed)
        {
            AddHeaderDirectory(document, header);
            AddFrame(document, header);
            CaptureHeader(document, reader, options, header.RasterOffset);
            ValidateRaster(document, reader, header);
            ImageMetadataNormalizerCore.BuildSummary(document);
        }
        return Finish(document, reader);
    }

    internal static bool ParsePnm(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, NetpbmHeader header)
    {
        if (!HasHeaderSeparator(document, reader))
        {
            return false;
        }
        int kind = header.Magic[1] - '0';
        header.Variant = kind == 1 || kind == 4 ? "PBM" : kind == 2 || kind == 5 ? "PGM" : "PPM";
        header.Encoding = kind <= 3 ? "Plain" : "Raw";
        header.Depth = kind == 3 || kind == 6 ? 3 : 1;
        header.MaxValue = kind == 1 || kind == 4 ? 1 : 0;

        NetpbmToken width = ReadToken(document, reader, options, header.Comments);
        NetpbmToken height = ReadToken(document, reader, options, header.Comments);
        if (!SetDimension(document, header, width, true) || !SetDimension(document, header, height, false))
        {
            return false;
        }

        if (kind != 1 && kind != 4)
        {
            NetpbmToken maxValue = ReadToken(document, reader, options, header.Comments);
            header.MaxValue = ParsePositiveInteger(maxValue.Text);
            if (header.MaxValue <= 0 || header.MaxValue > MaximumMaxValue)
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "netpbm.max-value.invalid", "Netpbm maximum sample value was invalid.", "Netpbm", maxValue.Offset, maxValue.Text.Length);
                return false;
            }
        }

        if (kind <= 3)
        {
            header.RasterOffset = FindPlainRasterOffset(document, reader, options, header.Comments);
            header.ExpectedRasterBytes = 0;
            return header.RasterOffset >= 0;
        }

        header.RasterOffset = reader.Position;
        header.ExpectedRasterBytes = ExpectedIntegerRasterBytes(document, header, kind == 4);
        return true;
    }

    internal static bool ParsePfm(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, NetpbmHeader header)
    {
        if (!HasHeaderSeparator(document, reader))
        {
            return false;
        }
        header.Variant = "PFM";
        header.Encoding = "FloatingPoint";
        header.Depth = header.Magic == "PF" ? 3 : 1;
        header.MaxValue = 0;
        NetpbmToken width = ReadToken(document, reader, options, header.Comments);
        NetpbmToken height = ReadToken(document, reader, options, header.Comments);
        if (!SetDimension(document, header, width, true) || !SetDimension(document, header, height, false))
        {
            return false;
        }
        NetpbmToken scale = ReadToken(document, reader, options, header.Comments);
        double signedScale = ParseDecimal(scale.Text);
        if (signedScale == 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "netpbm.pfm.scale.invalid", "PFM scale factor was invalid or zero.", "PFM", scale.Offset, scale.Text.Length);
            return false;
        }
        header.ByteOrder = signedScale < 0 ? "LittleEndian" : "BigEndian";
        header.ScaleFactor = signedScale < 0 ? -signedScale : signedScale;
        header.RasterOffset = reader.Position;
        header.ExpectedRasterBytes = SafeProduct(document, header.Width, header.Height, header.Depth, 4);
        return true;
    }

    internal static bool ParsePam(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, NetpbmHeader header)
    {
        header.Variant = "PAM";
        header.Encoding = "Raw";
        string? firstLine = ReadLine(document, reader, options);
        if (firstLine == null)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "netpbm.pam.header.truncated", "PAM header ended before its fields.", "PAM", reader.Position, 0);
            return false;
        }
        if (firstLine.Trim().Length != 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "netpbm.pam.magic-line.invalid", "PAM magic number was not followed by a line ending.", "PAM", 2, firstLine.Length);
            return false;
        }

        bool hasWidth = false;
        bool hasHeight = false;
        bool hasDepth = false;
        bool hasMaxValue = false;
        bool hasEnd = false;
        while (!hasEnd)
        {
            long lineOffset = reader.Position;
            string? line = ReadLine(document, reader, options);
            if (line == null)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "netpbm.pam.endhdr.missing", "PAM header ended before ENDHDR.", "PAM", lineOffset, 0);
                return false;
            }
            string trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }
            if (trimmed[0] == '#')
            {
                header.Comments.Add(trimmed.Substring(1).Trim());
                continue;
            }
            int separator = FirstWhitespace(trimmed);
            string key = separator < 0 ? trimmed : trimmed.Substring(0, separator);
            string value = separator < 0 ? "" : trimmed.Substring(separator).Trim();
            if (key == "ENDHDR")
            {
                if (value.Length != 0)
                {
                    MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "netpbm.pam.endhdr.invalid", "PAM ENDHDR line contained an unexpected value.", "PAM", lineOffset, line.Length);
                    return false;
                }
                hasEnd = true;
            }
            else if (key == "WIDTH")
            {
                if (!SetPamInteger(document, "width", value, lineOffset, hasWidth, header)) return false;
                hasWidth = true;
            }
            else if (key == "HEIGHT")
            {
                if (!SetPamInteger(document, "height", value, lineOffset, hasHeight, header)) return false;
                hasHeight = true;
            }
            else if (key == "DEPTH")
            {
                if (!SetPamInteger(document, "depth", value, lineOffset, hasDepth, header)) return false;
                hasDepth = true;
            }
            else if (key == "MAXVAL")
            {
                if (!SetPamInteger(document, "maxval", value, lineOffset, hasMaxValue, header)) return false;
                hasMaxValue = true;
            }
            else if (key == "TUPLTYPE")
            {
                if (value.Length == 0)
                {
                    MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "netpbm.pam.tuple-type.invalid", "PAM TUPLTYPE value was empty.", "PAM", lineOffset, line.Length);
                    return false;
                }
                header.TupleType = header.TupleType.Length == 0 ? value : header.TupleType + " " + value;
            }
        }

        if (!hasWidth || !hasHeight || !hasDepth || !hasMaxValue)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "netpbm.pam.field.missing", "PAM header omitted a required WIDTH, HEIGHT, DEPTH, or MAXVAL field.", "PAM", 0, reader.Position);
            return false;
        }
        if (header.MaxValue > MaximumMaxValue)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "netpbm.max-value.invalid", "PAM maximum sample value exceeded 65535.", "PAM", 0, reader.Position);
            return false;
        }
        header.RasterOffset = reader.Position;
        header.ExpectedRasterBytes = SafeProduct(document, header.Width, header.Height, header.Depth, header.MaxValue < 256 ? 1 : 2);
        return true;
    }

    internal static NetpbmToken ReadToken(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, List<string> comments)
    {
        NetpbmToken token = new NetpbmToken();
        if (!SkipWhitespaceAndComments(document, reader, options, comments))
        {
            return token;
        }
        token.Offset = reader.Position;
        StringBuilder text = new StringBuilder();
        while (reader.Position < reader.AccessibleLength())
        {
            if (!WithinHeaderLimit(document, reader, options))
            {
                return new NetpbmToken();
            }
            int value = reader.ReadByte();
            if (value < 0)
            {
                break;
            }
            if (IsWhitespace(value))
            {
                if (value == 13 && reader.Position < reader.AccessibleLength())
                {
                    long previous = reader.Position;
                    int next = reader.ReadByte();
                    if (next != 10)
                    {
                        reader.SeekAbsolute(previous);
                    }
                }
                break;
            }
            if (value == 35)
            {
                if (!ReadComment(document, reader, options, comments))
                {
                    return new NetpbmToken();
                }
                break;
            }
            if (value > 127)
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "netpbm.token.non-ascii", "Netpbm header token contained a non-ASCII byte.", "Netpbm", reader.Position - 1, 1);
                return new NetpbmToken();
            }
            text.Append((char)value);
        }
        token.Text = text.ToString();
        return token;
    }

    internal static bool SkipWhitespaceAndComments(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, List<string> comments)
    {
        while (reader.Position < reader.AccessibleLength())
        {
            if (!WithinHeaderLimit(document, reader, options))
            {
                return false;
            }
            long position = reader.Position;
            int value = reader.ReadByte();
            if (IsWhitespace(value))
            {
                continue;
            }
            if (value == 35)
            {
                if (!ReadComment(document, reader, options, comments)) return false;
                continue;
            }
            reader.SeekAbsolute(position);
            return true;
        }
        MetadataDiagnostic.AddTruncated(document.Diagnostics, "netpbm.header.truncated", "Netpbm header ended before all required values were read.", "Netpbm", reader.Position, 0);
        return false;
    }

    internal static bool ReadComment(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, List<string> comments)
    {
        StringBuilder comment = new StringBuilder();
        while (reader.Position < reader.AccessibleLength())
        {
            if (!WithinHeaderLimit(document, reader, options)) return false;
            int character = reader.ReadByte();
            if (character == 10 || character == 13 || character < 0)
            {
                if (character == 13 && reader.Position < reader.AccessibleLength())
                {
                    long previous = reader.Position;
                    int next = reader.ReadByte();
                    if (next != 10) reader.SeekAbsolute(previous);
                }
                break;
            }
            if (character <= 127) comment.Append((char)character);
        }
        comments.Add(comment.ToString().Trim());
        return true;
    }

    internal static bool HasHeaderSeparator(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (reader.Position >= reader.AccessibleLength())
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "netpbm.magic.separator.truncated", "Netpbm magic number was not followed by header whitespace.", "Netpbm", reader.Position, 1);
            return false;
        }
        long position = reader.Position;
        int value = reader.ReadByte();
        reader.SeekAbsolute(position);
        if (!IsWhitespace(value))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "netpbm.magic.separator.invalid", "Netpbm magic number was not followed by whitespace.", "Netpbm", position, 1);
            return false;
        }
        return true;
    }

    internal static long FindPlainRasterOffset(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, List<string> comments)
    {
        if (!SkipWhitespaceAndComments(document, reader, options, comments))
        {
            return -1;
        }
        return reader.Position;
    }

    internal static string? ReadLine(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options)
    {
        StringBuilder text = new StringBuilder();
        while (reader.Position < reader.AccessibleLength())
        {
            if (!WithinHeaderLimit(document, reader, options))
            {
                return null;
            }
            int value = reader.ReadByte();
            if (value == 10)
            {
                return text.ToString();
            }
            if (value == 13)
            {
                if (reader.Position < reader.AccessibleLength())
                {
                    long previous = reader.Position;
                    int next = reader.ReadByte();
                    if (next != 10) reader.SeekAbsolute(previous);
                }
                return text.ToString();
            }
            if (value > 127)
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "netpbm.header.non-ascii", "Netpbm header line contained a non-ASCII byte.", "Netpbm", reader.Position - 1, 1);
                return null;
            }
            text.Append((char)value);
        }
        return null;
    }

    internal static bool SetDimension(ImageMetadataDocument document, NetpbmHeader header, NetpbmToken token, bool width)
    {
        int value = ParsePositiveInteger(token.Text);
        if (value <= 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, width ? "netpbm.width.invalid" : "netpbm.height.invalid", width ? "Netpbm width was invalid." : "Netpbm height was invalid.", "Netpbm", token.Offset, token.Text.Length);
            return false;
        }
        if (width) header.Width = value;
        else header.Height = value;
        return true;
    }

    internal static bool SetPamInteger(ImageMetadataDocument document, string field, string text, long offset, bool duplicate, NetpbmHeader header)
    {
        if (duplicate)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "netpbm.pam.field.duplicate", "PAM header repeated the " + field.ToUpperInvariant() + " field.", "PAM", offset, text.Length);
            return false;
        }
        int value = ParsePositiveInteger(text);
        if (value <= 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "netpbm.pam.field.invalid", "PAM " + field.ToUpperInvariant() + " value was invalid.", "PAM", offset, text.Length);
            return false;
        }
        if (field == "width") header.Width = value;
        else if (field == "height") header.Height = value;
        else if (field == "depth") header.Depth = value;
        else header.MaxValue = value;
        return true;
    }

    internal static int ParsePositiveInteger(string text)
    {
        if (text == null || text.Length == 0)
        {
            return 0;
        }
        long value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9') return 0;
            int digit = character - '0';
            if (value > (2147483647 - digit) / 10) return 0;
            value = value * 10 + digit;
            index = index + 1;
        }
        return (int)value;
    }

    internal static double ParseDecimal(string text)
    {
        if (text == null || text.Length == 0) return 0;
        int index = 0;
        bool negative = false;
        if (text[index] == '+' || text[index] == '-')
        {
            negative = text[index] == '-';
            index = index + 1;
        }
        double value = 0;
        int digits = 0;
        while (index < text.Length && text[index] >= '0' && text[index] <= '9')
        {
            value = value * 10 + text[index] - '0';
            digits = digits + 1;
            index = index + 1;
        }
        if (index < text.Length && text[index] == '.')
        {
            index = index + 1;
            double divisor = 10;
            while (index < text.Length && text[index] >= '0' && text[index] <= '9')
            {
                value = value + (text[index] - '0') / divisor;
                divisor = divisor * 10;
                digits = digits + 1;
                index = index + 1;
            }
        }
        if (digits == 0) return 0;
        int exponent = 0;
        bool exponentNegative = false;
        if (index < text.Length && (text[index] == 'e' || text[index] == 'E'))
        {
            index = index + 1;
            if (index < text.Length && (text[index] == '+' || text[index] == '-'))
            {
                exponentNegative = text[index] == '-';
                index = index + 1;
            }
            int exponentDigits = 0;
            while (index < text.Length && text[index] >= '0' && text[index] <= '9')
            {
                if (exponent < 400) exponent = exponent * 10 + text[index] - '0';
                exponentDigits = exponentDigits + 1;
                index = index + 1;
            }
            if (exponentDigits == 0) return 0;
        }
        if (index != text.Length || exponent > 308) return 0;
        while (exponent > 0)
        {
            value = exponentNegative ? value / 10 : value * 10;
            exponent = exponent - 1;
        }
        return negative ? -value : value;
    }

    internal static long ExpectedIntegerRasterBytes(ImageMetadataDocument document, NetpbmHeader header, bool bitmap)
    {
        if (bitmap)
        {
            long rowBytes = ((long)header.Width + 7) / 8;
            return SafeProduct(document, rowBytes, header.Height, 1, 1);
        }
        return SafeProduct(document, header.Width, header.Height, header.Depth, header.MaxValue < 256 ? 1 : 2);
    }

    internal static long SafeProduct(ImageMetadataDocument document, long first, long second, long third, long fourth)
    {
        long value = first;
        if (second != 0 && value > long.MaxValue / second) return RasterOverflow(document);
        value = value * second;
        if (third != 0 && value > long.MaxValue / third) return RasterOverflow(document);
        value = value * third;
        if (fourth != 0 && value > long.MaxValue / fourth) return RasterOverflow(document);
        return value * fourth;
    }

    internal static long RasterOverflow(ImageMetadataDocument document)
    {
        MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "netpbm.raster.size.overflow", "Netpbm raster byte size overflowed the supported range.", "Netpbm", 0, 0);
        return -1;
    }

    internal static void ValidateRaster(ImageMetadataDocument document, BoundedStreamReader reader, NetpbmHeader header)
    {
        if (header.ExpectedRasterBytes <= 0)
        {
            return;
        }
        long available = reader.AccessibleLength() - header.RasterOffset;
        if (available < header.ExpectedRasterBytes)
        {
            if (reader.AccessibleLength() < reader.Length)
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "netpbm.raster.limit", "Netpbm binary raster exceeded the accessible reader limit.", "Raster", header.RasterOffset, header.ExpectedRasterBytes);
            }
            else
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "netpbm.raster.truncated", "Netpbm binary raster was shorter than its header dimensions require.", "Raster", header.RasterOffset, header.ExpectedRasterBytes);
            }
        }
    }

    internal static void AddHeaderDirectory(ImageMetadataDocument document, NetpbmHeader header)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Format, "Header", header.Variant + " Header");
        directory.Path = "Netpbm/Header";
        directory.Offset = 0;
        directory.Length = header.RasterOffset;
        AddTag(directory, "Magic", "Magic", MetadataValue.FromString(header.Magic));
        AddTag(directory, "Variant", "Variant", MetadataValue.FromString(header.Variant));
        AddTag(directory, "Encoding", "Encoding", MetadataValue.FromString(header.Encoding));
        AddTag(directory, "Width", "Width", MetadataValue.FromInteger(header.Width));
        AddTag(directory, "Height", "Height", MetadataValue.FromInteger(header.Height));
        AddTag(directory, "Depth", "Depth", MetadataValue.FromInteger(header.Depth));
        if (header.MaxValue > 0) AddTag(directory, "MaxValue", "Maximum Value", MetadataValue.FromInteger(header.MaxValue));
        if (header.TupleType.Length > 0) AddTag(directory, "TupleType", "Tuple Type", MetadataValue.FromString(header.TupleType));
        if (header.ScaleFactor > 0)
        {
            AddTag(directory, "ScaleFactor", "Scale Factor", MetadataValue.FromDouble(header.ScaleFactor));
            AddTag(directory, "ByteOrder", "Byte Order", MetadataValue.FromString(header.ByteOrder));
            AddTag(directory, "RowOrder", "Row Order", MetadataValue.FromString("BottomToTop"));
        }
        AddTag(directory, "RasterOffset", "Raster Offset", MetadataValue.FromInteger(header.RasterOffset));
        if (header.ExpectedRasterBytes > 0) AddTag(directory, "ExpectedRasterBytes", "Expected Raster Bytes", MetadataValue.FromInteger(header.ExpectedRasterBytes));
        if (header.Comments.Count > 0)
        {
            MetadataValue comments = new MetadataValue();
            comments.Kind = MetadataValueKind.StringList;
            foreach (string comment in header.Comments) comments.AddTextValue(comment);
            AddTag(directory, "Comments", "Comments", comments);
        }
    }

    internal static void AddTag(MetadataDirectory directory, string keyName, string name, MetadataValue value)
    {
        MetadataTag tag = directory.AddTagWithValue("Netpbm.Header." + keyName, name, value);
        tag.Confidence = 100;
    }

    internal static void AddFrame(ImageMetadataDocument document, NetpbmHeader header)
    {
        ImageMetadataFrame frame = document.AddFrameWithDimensions(header.Width, header.Height);
        frame.ChannelCount = header.Depth;
        frame.BitDepth = header.Variant == "PFM" ? 32 : BitDepth(header.MaxValue);
        frame.Compression = ImageCompressionKind.None;
        if (header.Variant == "PPM" || header.Magic == "PF" || StartsWith(header.TupleType, "RGB"))
        {
            frame.ColorSpace = ImageColorSpaceKind.Rgb;
        }
        else if (header.Variant == "PBM" || header.Variant == "PGM" || header.Magic == "Pf" || StartsWith(header.TupleType, "GRAYSCALE") || StartsWith(header.TupleType, "BLACKANDWHITE"))
        {
            frame.ColorSpace = ImageColorSpaceKind.Gray;
        }
        frame.HasAlpha = EndsWith(header.TupleType, "_ALPHA");
    }

    internal static int BitDepth(int maxValue)
    {
        int bits = 0;
        int value = maxValue;
        while (value > 0)
        {
            bits = bits + 1;
            value = value >> 1;
        }
        return bits;
    }

    internal static void CaptureHeader(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options, long length)
    {
        if (!CaptureRawBlocks(options)) return;
        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Format, "Header", "Netpbm Header", 0, length);
        if (!CaptureRawBlockData(options)) return;
        long maximum = MaxRawBlockBytes(options);
        if (length > maximum || length > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "netpbm.raw.block.limit", "Netpbm header exceeded the configured raw block limit.", "Header", 0, length);
            return;
        }
        block.Data = ReadRange(reader, 0, (int)length);
    }

    internal static byte[] ReadRange(BoundedStreamReader reader, long offset, int length)
    {
        if (!reader.SeekAbsolute(offset)) return new byte[0];
        return reader.ReadBytes(length);
    }

    internal static bool WithinHeaderLimit(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options)
    {
        long maximum = MaxSegmentBytes(options);
        if (maximum >= 0 && reader.Position >= maximum)
        {
            if (!HasDiagnostic(document, "netpbm.header.limit"))
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "netpbm.header.limit", "Netpbm header exceeded the configured segment limit.", "Header", 0, reader.Position);
            }
            return false;
        }
        return true;
    }

    internal static bool HasDiagnostic(ImageMetadataDocument document, string code)
    {
        foreach (MetadataDiagnostic diagnostic in document.Diagnostics)
        {
            if (diagnostic.Code == code) return true;
        }
        return false;
    }

    internal static int FirstWhitespace(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (char.IsWhiteSpace(text[index])) return index;
            index = index + 1;
        }
        return -1;
    }

    internal static bool IsWhitespace(int value)
    {
        return value == 32 || value == 9 || value == 10 || value == 11 || value == 12 || value == 13;
    }

    internal static bool StartsWith(string text, string prefix)
    {
        return text != null && text.StartsWith(prefix, System.StringComparison.Ordinal);
    }

    internal static bool EndsWith(string text, string suffix)
    {
        return text != null && text.EndsWith(suffix, System.StringComparison.Ordinal);
    }

    internal static long MaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
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

    internal static ImageMetadataDocument Finish(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (reader.Diagnostics != null)
        {
            foreach (MetadataDiagnostic diagnostic in reader.Diagnostics) document.AddDiagnostic(diagnostic);
        }
        reader.SeekAbsolute(0);
        return document;
    }
}
