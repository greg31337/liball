using System.IO;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace image;

/// <summary>Converts an extracted metadata document to stable text or JSON representations.</summary>
internal static class ImageMetadataFormatterCore
{
    /// <summary>Formats all document sections as indented, human-readable text.</summary>
    /// <param name="document">The metadata document to format.</param>
    /// <returns>The formatted document, or an empty string when <c>document</c> is null.</returns>
    public static string ToText(ImageMetadataDocument? document)
    {
        if (document == null)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        AppendDocumentHeader(builder, document);
        AppendFrames(builder, document);
        AppendDirectories(builder, document);
        AppendProfiles(builder, document);
        AppendRawBlocks(builder, document);
        AppendDiagnostics(builder, document);
        return builder.ToString();
    }

    /// <summary>Serializes all document sections to a JSON object.</summary>
    /// <remarks>A null document is represented by an empty JSON object. Binary values are written as Base64 text.</remarks>
    /// <param name="document">The metadata document to serialize.</param>
    /// <returns>A UTF-8 JSON document returned as a .NET string.</returns>
    public static string ToJson(ImageMetadataDocument? document)
    {
        using (MemoryStream memory = new MemoryStream())
        {
            using (Utf8JsonWriter writer = new Utf8JsonWriter(memory))
            {
                WriteDocumentJson(writer, document);
            }

            return Encoding.UTF8.GetString(memory.ToArray());
        }
    }

    internal static void WriteDocumentJson(Utf8JsonWriter writer, ImageMetadataDocument? document)
    {
        writer.WriteStartObject();

        if (document != null)
        {
            writer.WritePropertyName("format");
            WriteFormatJson(writer, document.Format);
            writer.WriteNumber("byteLength", document.ByteLength);
            writer.WriteBoolean("isCorrupt", document.IsCorrupt);
            writer.WriteBoolean("isIncomplete", document.IsIncomplete);

            writer.WritePropertyName("summary");
            WriteSummaryJson(writer, document.Summary);

            writer.WritePropertyName("frames");
            writer.WriteStartArray();
            foreach (ImageMetadataFrame frame in document.Frames)
            {
                WriteFrameJson(writer, frame);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("directories");
            writer.WriteStartArray();
            foreach (MetadataDirectory directory in document.Directories)
            {
                WriteDirectoryJson(writer, directory);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("profiles");
            writer.WriteStartArray();
            foreach (MetadataProfile profile in document.Profiles)
            {
                WriteProfileJson(writer, profile);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("rawBlocks");
            writer.WriteStartArray();
            foreach (MetadataRawBlock rawBlock in document.RawBlocks)
            {
                WriteRawBlockJson(writer, rawBlock);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("diagnostics");
            writer.WriteStartArray();
            foreach (MetadataDiagnostic diagnostic in document.Diagnostics)
            {
                WriteDiagnosticJson(writer, diagnostic);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    internal static void WriteFormatJson(Utf8JsonWriter writer, ImageFormatInfo format)
    {
        ImageFormatInfo actual = format;
        if (actual == null)
        {
            actual = new ImageFormatInfo();
        }

        writer.WriteStartObject();
        writer.WriteString("kind", actual.Kind.ToString());
        writer.WriteString("name", SafeText(actual.Name));
        writer.WriteString("mimeType", SafeText(actual.MimeType));
        writer.WriteString("preferredExtension", SafeText(actual.PreferredExtension));
        writer.WriteString("containerBrand", SafeText(actual.ContainerBrand));
        writer.WriteNumber("confidence", actual.Confidence);
        writer.WriteBoolean("detectedByMagic", actual.DetectedByMagic);
        writer.WriteEndObject();
    }

    internal static void WriteSummaryJson(Utf8JsonWriter writer, ImageMetadataSummary summary)
    {
        ImageMetadataSummary actual = summary;
        if (actual == null)
        {
            actual = new ImageMetadataSummary();
        }

        writer.WriteStartObject();
        writer.WriteNumber("width", actual.Width);
        writer.WriteNumber("height", actual.Height);
        writer.WriteNumber("frameCount", actual.FrameCount);
        writer.WriteNumber("pixelAspectRatio", actual.PixelAspectRatio);
        writer.WriteNumber("horizontalResolution", actual.HorizontalResolution);
        writer.WriteNumber("verticalResolution", actual.VerticalResolution);
        writer.WriteString("resolutionUnit", SafeText(actual.ResolutionUnit));
        writer.WriteString("orientation", actual.Orientation.ToString());
        writer.WriteString("cameraMake", SafeText(actual.CameraMake));
        writer.WriteString("cameraModel", SafeText(actual.CameraModel));
        writer.WriteString("lensModel", SafeText(actual.LensModel));
        writer.WriteString("captureTimeText", SafeText(actual.CaptureTimeText));
        writer.WritePropertyName("captureTime");
        WriteMetadataDateJson(writer, actual.CaptureTime);
        writer.WritePropertyName("gps");
        WriteGpsJson(writer, actual.Gps);
        writer.WriteString("author", SafeText(actual.Author));
        writer.WriteString("copyright", SafeText(actual.Copyright));
        writer.WriteString("title", SafeText(actual.Title));
        writer.WriteString("description", SafeText(actual.Description));
        writer.WritePropertyName("keywords");
        writer.WriteStartArray();
        if (actual.Keywords != null)
        {
            foreach (string keyword in actual.Keywords)
            {
                writer.WriteStringValue(SafeText(keyword));
            }
        }

        writer.WriteEndArray();
        writer.WriteNumber("rating", actual.Rating);
        writer.WriteBoolean("hasColorProfile", actual.HasColorProfile);
        writer.WriteString("colorProfileName", SafeText(actual.ColorProfileName));
        writer.WriteString("colorSpace", actual.ColorSpace.ToString());
        writer.WriteString("sourceColorType", SafeText(actual.SourceColorType));
        writer.WriteBoolean("isInterlaced", actual.IsInterlaced);
        writer.WriteBoolean("isProgressive", actual.IsProgressive);
        writer.WriteBoolean("isAnimated", actual.IsAnimated);
        writer.WriteNumber("animationLoopCount", actual.AnimationLoopCount);
        writer.WriteNumber("totalDurationMilliseconds", actual.TotalDurationMilliseconds);
        writer.WriteString("resolutionText", SafeText(actual.ResolutionText));
        writer.WriteString("software", SafeText(actual.Software));
        writer.WriteEndObject();
    }

    internal static void WriteMetadataDateJson(Utf8JsonWriter writer, MetadataDate date)
    {
        MetadataDate actual = date;
        if (actual == null)
        {
            actual = new MetadataDate();
        }

        writer.WriteStartObject();
        writer.WriteBoolean("isValid", actual.IsValid);
        writer.WriteString("originalText", SafeText(actual.OriginalText));
        writer.WriteString("precision", actual.Precision.ToString());
        writer.WriteNumber("year", actual.Year);
        writer.WriteNumber("month", actual.Month);
        writer.WriteNumber("day", actual.Day);
        writer.WriteNumber("hour", actual.Hour);
        writer.WriteNumber("minute", actual.Minute);
        writer.WriteNumber("second", actual.Second);
        writer.WriteString("fractionalSecondText", SafeText(actual.FractionalSecondText));
        writer.WriteBoolean("hasTimeZone", actual.HasTimeZone);
        writer.WriteNumber("timeZoneOffsetMinutes", actual.TimeZoneOffsetMinutes);
        writer.WriteEndObject();
    }

    internal static void WriteGpsJson(Utf8JsonWriter writer, GpsCoordinate gps)
    {
        GpsCoordinate actual = gps;
        if (actual == null)
        {
            actual = new GpsCoordinate();
        }

        writer.WriteStartObject();
        writer.WriteBoolean("isValid", actual.IsValid);
        writer.WriteNumber("latitude", actual.Latitude);
        writer.WriteNumber("longitude", actual.Longitude);
        writer.WriteNumber("altitude", actual.Altitude);
        writer.WriteString("latitudeReference", SafeText(actual.LatitudeReference));
        writer.WriteString("longitudeReference", SafeText(actual.LongitudeReference));
        writer.WriteString("altitudeReference", SafeText(actual.AltitudeReference));
        writer.WriteEndObject();
    }

    internal static void WriteFrameJson(Utf8JsonWriter writer, ImageMetadataFrame frame)
    {
        ImageMetadataFrame actual = frame;
        if (actual == null)
        {
            actual = new ImageMetadataFrame();
        }

        writer.WriteStartObject();
        writer.WriteNumber("index", actual.Index);
        writer.WriteString("role", actual.Role.ToString());
        writer.WriteNumber("containerItemId", actual.ContainerItemId);
        writer.WriteNumber("parentFrameIndex", actual.ParentFrameIndex);
        writer.WriteNumber("width", actual.Width);
        writer.WriteNumber("height", actual.Height);
        writer.WriteNumber("canvasWidth", actual.CanvasWidth);
        writer.WriteNumber("canvasHeight", actual.CanvasHeight);
        writer.WriteNumber("canvasX", actual.CanvasX);
        writer.WriteNumber("canvasY", actual.CanvasY);
        writer.WriteNumber("pixelAspectRatio", actual.PixelAspectRatio);
        writer.WriteNumber("horizontalResolution", actual.HorizontalResolution);
        writer.WriteNumber("verticalResolution", actual.VerticalResolution);
        writer.WriteString("resolutionUnit", SafeText(actual.ResolutionUnit));
        writer.WriteNumber("bitDepth", actual.BitDepth);
        writer.WriteNumber("channelCount", actual.ChannelCount);
        writer.WriteBoolean("hasAlpha", actual.HasAlpha);
        writer.WriteBoolean("isAnimated", actual.IsAnimated);
        writer.WriteNumber("durationMilliseconds", actual.DurationMilliseconds);
        writer.WriteString("compression", actual.Compression.ToString());
        writer.WriteString("colorSpace", actual.ColorSpace.ToString());
        writer.WriteString("sourceColorType", SafeText(actual.SourceColorType));
        writer.WriteBoolean("isInterlaced", actual.IsInterlaced);
        writer.WriteBoolean("isProgressive", actual.IsProgressive);
        writer.WriteString("orientation", actual.Orientation.ToString());
        writer.WritePropertyName("directories");
        writer.WriteStartArray();
        if (actual.Directories != null)
        {
            foreach (MetadataDirectory directory in actual.Directories)
            {
                WriteDirectoryJson(writer, directory);
            }
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    internal static void WriteDirectoryJson(Utf8JsonWriter writer, MetadataDirectory directory)
    {
        MetadataDirectory actual = directory;
        if (actual == null)
        {
            actual = new MetadataDirectory();
        }

        writer.WriteStartObject();
        writer.WriteString("family", actual.Family.ToString());
        writer.WriteString("group", SafeText(actual.Group));
        writer.WriteString("name", SafeText(actual.Name));
        writer.WriteString("path", SafeText(actual.Path));
        writer.WriteNumber("frameIndex", actual.FrameIndex);
        writer.WriteNumber("offset", actual.Offset);
        writer.WriteNumber("length", actual.Length);
        writer.WriteNumber("repeatIndex", actual.RepeatIndex);
        writer.WritePropertyName("tags");
        writer.WriteStartArray();
        if (actual.Tags != null)
        {
            foreach (MetadataTag tag in actual.Tags)
            {
                WriteTagJson(writer, tag);
            }
        }

        writer.WriteEndArray();
        writer.WritePropertyName("children");
        writer.WriteStartArray();
        if (actual.Children != null)
        {
            foreach (MetadataDirectory child in actual.Children)
            {
                WriteDirectoryJson(writer, child);
            }
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    internal static void WriteTagJson(Utf8JsonWriter writer, MetadataTag tag)
    {
        MetadataTag actual = tag;
        if (actual == null)
        {
            actual = new MetadataTag();
        }

        writer.WriteStartObject();
        writer.WriteString("key", SafeText(actual.Key));
        writer.WriteString("name", SafeText(actual.Name));
        writer.WriteString("family", actual.Family.ToString());
        writer.WriteString("group", SafeText(actual.Group));
        writer.WriteNumber("numericId", actual.NumericId);
        writer.WriteString("valueType", actual.ValueType.ToString());
        writer.WritePropertyName("value");
        WriteValueJson(writer, actual.Value);
        writer.WriteNumber("rawValueLength", ByteLength(actual.RawValue));
        writer.WriteString("displayValue", SafeText(actual.DisplayValue));
        writer.WriteNumber("offset", actual.Offset);
        writer.WriteNumber("length", actual.Length);
        writer.WriteNumber("confidence", actual.Confidence);
        writer.WriteNumber("repeatIndex", actual.RepeatIndex);
        writer.WriteEndObject();
    }

    internal static void WriteValueJson(Utf8JsonWriter writer, MetadataValue value)
    {
        MetadataValue actual = value;
        if (actual == null)
        {
            actual = new MetadataValue();
        }

        writer.WriteStartObject();
        writer.WriteString("kind", actual.Kind.ToString());

        switch (actual.Kind)
        {
            case MetadataValueKind.Integer:
                writer.WriteNumber("integerValue", actual.IntegerValue);
                break;
            case MetadataValueKind.UnsignedInteger:
                writer.WriteNumber("unsignedIntegerValue", actual.UnsignedIntegerValue);
                break;
            case MetadataValueKind.Rational:
            case MetadataValueKind.SignedRational:
                writer.WriteNumber("numerator", actual.Numerator);
                writer.WriteNumber("denominator", actual.Denominator);
                break;
            case MetadataValueKind.Double:
                writer.WriteNumber("doubleValue", actual.DoubleValue);
                break;
            case MetadataValueKind.IntegerArray:
                writer.WritePropertyName("integerValues");
                writer.WriteStartArray();
                if (actual.IntegerValues != null)
                {
                    foreach (long number in actual.IntegerValues) writer.WriteNumberValue(number);
                }
                writer.WriteEndArray();
                break;
            case MetadataValueKind.UnsignedIntegerArray:
                writer.WritePropertyName("unsignedIntegerValues");
                writer.WriteStartArray();
                if (actual.UnsignedIntegerValues != null)
                {
                    foreach (ulong number in actual.UnsignedIntegerValues) writer.WriteNumberValue(number);
                }
                writer.WriteEndArray();
                break;
            case MetadataValueKind.RationalArray:
            case MetadataValueKind.SignedRationalArray:
                writer.WritePropertyName("numerators");
                writer.WriteStartArray();
                if (actual.Numerators != null)
                {
                    foreach (long number in actual.Numerators) writer.WriteNumberValue(number);
                }
                writer.WriteEndArray();
                writer.WritePropertyName("denominators");
                writer.WriteStartArray();
                if (actual.Denominators != null)
                {
                    foreach (long number in actual.Denominators) writer.WriteNumberValue(number);
                }
                writer.WriteEndArray();
                break;
            case MetadataValueKind.DoubleArray:
                writer.WritePropertyName("doubleValues");
                writer.WriteStartArray();
                if (actual.DoubleValues != null)
                {
                    foreach (double number in actual.DoubleValues) writer.WriteNumberValue(number);
                }
                writer.WriteEndArray();
                break;
            case MetadataValueKind.String:
            case MetadataValueKind.DateText:
            case MetadataValueKind.XmlText:
                writer.WriteString("text", SafeText(actual.Text));
                break;
            case MetadataValueKind.StringList:
                writer.WritePropertyName("textValues");
                writer.WriteStartArray();
                if (actual.TextValues != null)
                {
                    foreach (string text in actual.TextValues)
                    {
                        writer.WriteStringValue(SafeText(text));
                    }
                }

                writer.WriteEndArray();
                break;
            case MetadataValueKind.ByteArray:
                writer.WriteNumber("byteLength", ByteLength(actual.Bytes));
                writer.WriteString("hex", HashHelper.ToHex(actual.Bytes));
                break;
            case MetadataValueKind.DirectoryReference:
                if (actual.DirectoryValue == null)
                {
                    writer.WriteNull("directoryName");
                }
                else
                {
                    writer.WriteString("directoryName", SafeText(actual.DirectoryValue.Name));
                }

                break;
            case MetadataValueKind.Boolean:
                writer.WriteBoolean("booleanValue", actual.BooleanValue);
                break;
            default:
                break;
        }

        writer.WriteEndObject();
    }

    internal static void WriteProfileJson(Utf8JsonWriter writer, MetadataProfile profile)
    {
        MetadataProfile actual = profile;
        if (actual == null)
        {
            actual = new MetadataProfile();
        }

        writer.WriteStartObject();
        writer.WriteNumber("index", actual.Index);
        writer.WriteString("role", actual.Role.ToString());
        writer.WriteNumber("containerItemId", actual.ContainerItemId);
        writer.WriteNumber("sourceFrameIndex", actual.SourceFrameIndex);
        writer.WriteString("kind", actual.Kind.ToString());
        writer.WriteString("family", actual.Family.ToString());
        writer.WriteString("key", SafeText(actual.Key));
        writer.WriteString("name", SafeText(actual.Name));
        writer.WriteString("mimeType", SafeText(actual.MimeType));
        writer.WriteNumber("byteLength", ByteLength(actual.Data));
        writer.WriteString("text", SafeText(actual.Text));
        writer.WriteNumber("offset", actual.Offset);
        writer.WriteNumber("length", actual.Length);
        writer.WriteNumber("sequenceNumber", actual.SequenceNumber);
        writer.WriteNumber("sequenceCount", actual.SequenceCount);
        writer.WriteEndObject();
    }

    internal static void WriteRawBlockJson(Utf8JsonWriter writer, MetadataRawBlock rawBlock)
    {
        MetadataRawBlock actual = rawBlock;
        if (actual == null)
        {
            actual = new MetadataRawBlock();
        }

        writer.WriteStartObject();
        writer.WriteString("family", actual.Family.ToString());
        writer.WriteString("blockType", SafeText(actual.BlockType));
        writer.WriteString("name", SafeText(actual.Name));
        writer.WriteNumber("offset", actual.Offset);
        writer.WriteNumber("length", actual.Length);
        writer.WriteNumber("byteLength", ByteLength(actual.Data));
        writer.WriteEndObject();
    }

    internal static void WriteDiagnosticJson(Utf8JsonWriter writer, MetadataDiagnostic diagnostic)
    {
        MetadataDiagnostic actual = diagnostic;
        if (actual == null)
        {
            actual = new MetadataDiagnostic();
        }

        writer.WriteStartObject();
        writer.WriteString("severity", actual.Severity.ToString());
        writer.WriteString("code", SafeText(actual.Code));
        writer.WriteString("message", SafeText(actual.Message));
        writer.WriteString("blockType", SafeText(actual.BlockType));
        writer.WriteNumber("offset", actual.Offset);
        writer.WriteNumber("length", actual.Length);
        writer.WriteEndObject();
    }

    internal static void AppendDocumentHeader(StringBuilder builder, ImageMetadataDocument document)
    {
        builder.Append("Format: ");
        builder.AppendLine(SafeText(document.Format.Name));
        builder.Append("Kind: ");
        builder.AppendLine(document.Format.Kind.ToString());
        builder.Append("MIME: ");
        builder.AppendLine(SafeText(document.Format.MimeType));
        builder.Append("Extension: ");
        builder.AppendLine(SafeText(document.Format.PreferredExtension));
        builder.Append("Byte length: ");
        builder.AppendLine(document.ByteLength.ToString(CultureInfo.InvariantCulture));
        builder.Append("Corrupt: ");
        builder.AppendLine(document.IsCorrupt.ToString());
        builder.Append("Incomplete: ");
        builder.AppendLine(document.IsIncomplete.ToString());
    }

    internal static void AppendFrames(StringBuilder builder, ImageMetadataDocument document)
    {
        builder.Append("Frames: ");
        builder.AppendLine(document.Frames.Count.ToString(CultureInfo.InvariantCulture));

        foreach (ImageMetadataFrame frame in document.Frames)
        {
            builder.Append("- Frame ");
            builder.Append(frame.Index.ToString(CultureInfo.InvariantCulture));
            builder.Append(" ");
            builder.Append(frame.Role.ToString());
            builder.Append(": ");
            builder.Append(frame.Width.ToString(CultureInfo.InvariantCulture));
            builder.Append("x");
            builder.Append(frame.Height.ToString(CultureInfo.InvariantCulture));
            builder.Append(" at ");
            builder.Append(frame.CanvasX.ToString(CultureInfo.InvariantCulture));
            builder.Append(",");
            builder.Append(frame.CanvasY.ToString(CultureInfo.InvariantCulture));
            builder.Append(", bits ");
            builder.Append(frame.BitDepth.ToString(CultureInfo.InvariantCulture));
            builder.Append(", channels ");
            builder.Append(frame.ChannelCount.ToString(CultureInfo.InvariantCulture));
            builder.Append(", color type ");
            builder.Append(SafeText(frame.SourceColorType));
            builder.Append(", aspect ");
            builder.AppendLine(frame.PixelAspectRatio.ToString(CultureInfo.InvariantCulture));
        }
    }

    internal static void AppendDirectories(StringBuilder builder, ImageMetadataDocument document)
    {
        builder.Append("Directories: ");
        builder.AppendLine(document.Directories.Count.ToString(CultureInfo.InvariantCulture));

        foreach (MetadataDirectory directory in document.Directories)
        {
            AppendDirectory(builder, directory, 0);
        }
    }

    internal static void AppendDirectory(StringBuilder builder, MetadataDirectory directory, int indent)
    {
        AppendIndent(builder, indent);
        builder.Append("[");
        builder.Append(directory.Family.ToString());
        if (directory.Group.Length > 0)
        {
            builder.Append("/");
            builder.Append(directory.Group);
        }

        builder.Append("] ");
        builder.AppendLine(SafeText(directory.Name));

        foreach (MetadataTag tag in directory.Tags)
        {
            AppendTag(builder, tag, indent + 2);
        }

        foreach (MetadataDirectory child in directory.Children)
        {
            AppendDirectory(builder, child, indent + 2);
        }
    }

    internal static void AppendTag(StringBuilder builder, MetadataTag tag, int indent)
    {
        AppendIndent(builder, indent);
        builder.Append(SafeText(tag.Key));
        if (tag.Name.Length > 0)
        {
            builder.Append(" (");
            builder.Append(tag.Name);
            builder.Append(")");
        }

        builder.Append(" = ");
        if (tag.DisplayValue.Length > 0)
        {
            builder.AppendLine(tag.DisplayValue);
        }
        else
        {
            builder.AppendLine(FormatValue(tag.Value));
        }
    }

    internal static void AppendProfiles(StringBuilder builder, ImageMetadataDocument document)
    {
        builder.Append("Profiles: ");
        builder.AppendLine(document.Profiles.Count.ToString(CultureInfo.InvariantCulture));

        foreach (MetadataProfile profile in document.Profiles)
        {
            builder.Append("- ");
            builder.Append(profile.Index.ToString(CultureInfo.InvariantCulture));
            builder.Append(" ");
            builder.Append(profile.Role.ToString());
            builder.Append(" ");
            builder.Append(profile.Kind.ToString());
            builder.Append(" ");
            builder.Append(SafeText(profile.Name));
            builder.Append(" ");
            builder.Append(profile.Length.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine(" bytes");
        }
    }

    internal static void AppendRawBlocks(StringBuilder builder, ImageMetadataDocument document)
    {
        builder.Append("Raw blocks: ");
        builder.AppendLine(document.RawBlocks.Count.ToString(CultureInfo.InvariantCulture));

        foreach (MetadataRawBlock rawBlock in document.RawBlocks)
        {
            builder.Append("- ");
            builder.Append(SafeText(rawBlock.BlockType));
            builder.Append(" ");
            builder.Append(SafeText(rawBlock.Name));
            builder.Append(" @");
            builder.Append(rawBlock.Offset.ToString(CultureInfo.InvariantCulture));
            builder.Append("+");
            builder.AppendLine(rawBlock.Length.ToString(CultureInfo.InvariantCulture));
        }
    }

    internal static void AppendDiagnostics(StringBuilder builder, ImageMetadataDocument document)
    {
        builder.Append("Diagnostics: ");
        builder.AppendLine(document.Diagnostics.Count.ToString(CultureInfo.InvariantCulture));

        foreach (MetadataDiagnostic diagnostic in document.Diagnostics)
        {
            builder.Append("- ");
            builder.Append(diagnostic.Severity.ToString());
            builder.Append(" ");
            builder.Append(SafeText(diagnostic.Code));
            builder.Append(": ");
            builder.AppendLine(SafeText(diagnostic.Message));
        }
    }

    internal static string FormatValue(MetadataValue? value)
    {
        if (value == null)
        {
            return "";
        }

        switch (value.Kind)
        {
            case MetadataValueKind.Integer:
                return value.IntegerValue.ToString(CultureInfo.InvariantCulture);
            case MetadataValueKind.UnsignedInteger:
                return value.UnsignedIntegerValue.ToString(CultureInfo.InvariantCulture);
            case MetadataValueKind.Rational:
            case MetadataValueKind.SignedRational:
                return value.Numerator.ToString(CultureInfo.InvariantCulture) + "/" + value.Denominator.ToString(CultureInfo.InvariantCulture);
            case MetadataValueKind.Double:
                return value.DoubleValue.ToString(CultureInfo.InvariantCulture);
            case MetadataValueKind.IntegerArray:
                return FormatIntegerArray(value.IntegerValues);
            case MetadataValueKind.UnsignedIntegerArray:
                return FormatUnsignedIntegerArray(value.UnsignedIntegerValues);
            case MetadataValueKind.RationalArray:
            case MetadataValueKind.SignedRationalArray:
                return FormatRationalArray(value.Numerators, value.Denominators);
            case MetadataValueKind.DoubleArray:
                return FormatDoubleArray(value.DoubleValues);
            case MetadataValueKind.String:
            case MetadataValueKind.DateText:
            case MetadataValueKind.XmlText:
                return SafeText(value.Text);
            case MetadataValueKind.StringList:
                return FormatStringList(value);
            case MetadataValueKind.ByteArray:
                return ByteLength(value.Bytes).ToString(CultureInfo.InvariantCulture) + " bytes";
            case MetadataValueKind.DirectoryReference:
                if (value.DirectoryValue == null)
                {
                    return "";
                }

                return SafeText(value.DirectoryValue.Name);
            case MetadataValueKind.Boolean:
                if (value.BooleanValue)
                {
                    return "true";
                }

                return "false";
            default:
                return "";
        }
    }

    internal static string FormatStringList(MetadataValue value)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;

        while (value.TextValues != null && index < value.TextValues.Length)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(SafeText(value.TextValues[index]));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string FormatIntegerArray(long[] values)
    {
        if (values == null) return "";
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0) builder.Append(", ");
            builder.Append(values[index].ToString(CultureInfo.InvariantCulture));
            index = index + 1;
        }
        return builder.ToString();
    }

    internal static string FormatUnsignedIntegerArray(ulong[] values)
    {
        if (values == null) return "";
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0) builder.Append(", ");
            builder.Append(values[index].ToString(CultureInfo.InvariantCulture));
            index = index + 1;
        }
        return builder.ToString();
    }

    internal static string FormatRationalArray(long[] numerators, long[] denominators)
    {
        if (numerators == null || denominators == null) return "";
        StringBuilder builder = new StringBuilder();
        int count = numerators.Length < denominators.Length ? numerators.Length : denominators.Length;
        int index = 0;
        while (index < count)
        {
            if (index > 0) builder.Append(", ");
            builder.Append(numerators[index].ToString(CultureInfo.InvariantCulture));
            builder.Append("/");
            builder.Append(denominators[index].ToString(CultureInfo.InvariantCulture));
            index = index + 1;
        }
        return builder.ToString();
    }

    internal static string FormatDoubleArray(double[] values)
    {
        if (values == null) return "";
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0) builder.Append(", ");
            builder.Append(values[index].ToString(CultureInfo.InvariantCulture));
            index = index + 1;
        }
        return builder.ToString();
    }

    internal static void AppendIndent(StringBuilder builder, int indent)
    {
        int index = 0;
        while (index < indent)
        {
            builder.Append(" ");
            index = index + 1;
        }
    }

    internal static string SafeText(string text)
    {
        if (text == null)
        {
            return "";
        }

        return text;
    }

    internal static int ByteLength(byte[] bytes)
    {
        if (bytes == null)
        {
            return 0;
        }

        return bytes.Length;
    }
}
