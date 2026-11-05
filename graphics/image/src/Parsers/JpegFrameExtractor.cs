using System.Collections.Generic;

namespace image;

internal class JpegFrameExtractor
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);

        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "jpeg.reader.null", "JPEG parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        List<JpegMarkerSegment> segments = JpegMarkerScanner.Scan(reader, options);
        JpegApp0Extractor.Extract(document, reader, segments);
        JpegExifExtractor.Extract(document, reader, segments, options);
        JpegXmpExtractor.Extract(document, reader, segments, options);
        JpegIccExtractor.Extract(document, reader, segments, options);
        JpegPhotoshopExtractor.Extract(document, reader, segments, options);
        JpegAdobeExtractor.Extract(document, reader, segments);
        JpegCommentExtractor.Extract(document, reader, segments);
        JpegRawProfileExtractor.Extract(document, reader, segments, options);
        ExtractFrames(document, reader, segments, options);
        CopyReaderDiagnostics(document, reader);
        ImageMetadataNormalizerCore.BuildSummary(document);
        return document;
    }

    internal static void ExtractFrames(ImageMetadataDocument document, BoundedStreamReader reader, List<JpegMarkerSegment> segments, ImageMetadataReaderOptions options)
    {
        if (document == null || reader == null || segments == null)
        {
            return;
        }

        int maxFrames = GetMaxFrameCount(options);

        foreach (JpegMarkerSegment segment in segments)
        {
            if (segment != null && IsStartOfFrameMarker(segment.Marker))
            {
                if (document.Frames.Count >= maxFrames)
                {
                    MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.frame.limit", "JPEG frame count exceeded the configured frame limit.", segment.Name, segment.MarkerOffset, segment.SegmentLength);
                    return;
                }

                ExtractFrame(document, reader, segment);
            }
        }
    }

    internal static void ExtractFrame(ImageMetadataDocument document, BoundedStreamReader reader, JpegMarkerSegment segment)
    {
        if (document == null || reader == null || segment == null)
        {
            return;
        }

        if (segment.PayloadLength < 6)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.sof.truncated", "JPEG SOF payload was too short for frame dimensions.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return;
        }

        if (!reader.SeekAbsolute(segment.PayloadOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jpeg.sof.seek", "JPEG SOF payload could not be reached.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return;
        }

        int precision = reader.ReadByte();
        int height = ReadUInt16(reader);
        int width = ReadUInt16(reader);
        int components = reader.ReadByte();

        if (precision < 0 || height < 0 || width < 0 || components < 0)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.sof.truncated", "JPEG SOF payload ended before frame dimensions were complete.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return;
        }

        long expectedPayloadLength = 6 + (components * 3);
        if (expectedPayloadLength > segment.PayloadLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.sof.components.truncated", "JPEG SOF component table exceeded the segment payload.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
        }

        ImageMetadataFrame frame = new ImageMetadataFrame();
        frame.Width = width;
        frame.Height = height;
        frame.CanvasWidth = width;
        frame.CanvasHeight = height;
        frame.BitDepth = precision;
        frame.ChannelCount = components;
        frame.Compression = ImageCompressionKind.Jpeg;
        frame.ColorSpace = GuessColorSpace(components);
        frame.SourceColorType = frame.ColorSpace.ToString();
        frame.IsProgressive = IsProgressiveFrame(segment.Marker);
        ImageMetadataFrame added = document.AddFrame(frame);
        AddSofDirectory(added, segment, precision, width, height, components);
    }

    internal static void AddSofDirectory(ImageMetadataFrame frame, JpegMarkerSegment segment, int precision, int width, int height, int components)
    {
        if (frame == null || segment == null)
        {
            return;
        }

        MetadataDirectory directory = frame.AddDirectoryWithName(MetadataFamily.Jpeg, segment.Name, segment.Name + " Frame Header");
        directory.Path = "JPEG/" + segment.Name;
        directory.Offset = segment.MarkerOffset;
        directory.Length = segment.SegmentLength;
        directory.AddTagWithValue("Jpeg." + segment.Name + ".Precision", "Precision", MetadataValue.FromInteger(precision));
        directory.AddTagWithValue("Jpeg." + segment.Name + ".Height", "Height", MetadataValue.FromInteger(height));
        directory.AddTagWithValue("Jpeg." + segment.Name + ".Width", "Width", MetadataValue.FromInteger(width));
        directory.AddTagWithValue("Jpeg." + segment.Name + ".ComponentCount", "Component Count", MetadataValue.FromInteger(components));
        directory.AddTagWithValue("Jpeg." + segment.Name + ".Process", "Process", MetadataValue.FromString(ProcessName(segment.Marker)));
        directory.AddTagWithValue("Jpeg." + segment.Name + ".Progressive", "Progressive", MetadataValue.FromBoolean(IsProgressiveFrame(segment.Marker)));
    }

    internal static int ReadUInt16(BoundedStreamReader reader)
    {
        if (reader == null)
        {
            return -1;
        }

        int high = reader.ReadByte();
        int low = reader.ReadByte();

        if (high < 0 || low < 0)
        {
            return -1;
        }

        return (high * 256) + low;
    }

    internal static bool IsStartOfFrameMarker(int marker)
    {
        if (marker >= 192 && marker <= 195)
        {
            return true;
        }

        if (marker >= 197 && marker <= 199)
        {
            return true;
        }

        if (marker >= 201 && marker <= 203)
        {
            return true;
        }

        if (marker >= 205 && marker <= 207)
        {
            return true;
        }

        return false;
    }

    internal static bool IsProgressiveFrame(int marker)
    {
        return marker == 194 || marker == 198 || marker == 202 || marker == 206;
    }

    internal static string ProcessName(int marker)
    {
        switch (marker)
        {
            case 192:
                return "Baseline DCT";
            case 193:
                return "Extended Sequential DCT";
            case 194:
                return "Progressive DCT";
            case 195:
                return "Lossless";
            case 197:
                return "Differential Sequential DCT";
            case 198:
                return "Differential Progressive DCT";
            case 199:
                return "Differential Lossless";
            case 201:
                return "Extended Sequential Arithmetic";
            case 202:
                return "Progressive Arithmetic";
            case 203:
                return "Lossless Arithmetic";
            case 205:
                return "Differential Sequential Arithmetic";
            case 206:
                return "Differential Progressive Arithmetic";
            case 207:
                return "Differential Lossless Arithmetic";
            default:
                return "Unknown";
        }
    }

    internal static ImageColorSpaceKind GuessColorSpace(int components)
    {
        if (components == 1)
        {
            return ImageColorSpaceKind.Gray;
        }

        if (components == 3)
        {
            return ImageColorSpaceKind.YCbCr;
        }

        if (components == 4)
        {
            return ImageColorSpaceKind.Cmyk;
        }

        return ImageColorSpaceKind.Unknown;
    }

    internal static int GetMaxFrameCount(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxFrameCount;
        }

        return options.MaxFrameCount;
    }

    internal static void CopyReaderDiagnostics(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (document == null || reader == null || reader.Diagnostics == null)
        {
            return;
        }

        foreach (MetadataDiagnostic diagnostic in reader.Diagnostics)
        {
            document.Diagnostics.Add(diagnostic);
        }
    }
}
