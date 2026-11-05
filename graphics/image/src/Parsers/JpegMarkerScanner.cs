using System.Collections.Generic;

namespace image;

internal class JpegMarkerScanner
{
    internal const int MarkerPrefix = 255;
    internal const int StuffedZero = 0;
    internal const int MarkerStartOfImage = 216;
    internal const int MarkerEndOfImage = 217;
    internal const int MarkerStartOfScan = 218;

    internal static List<JpegMarkerSegment> Scan(BoundedStreamReader reader, ImageMetadataReaderOptions options)
    {
        List<JpegMarkerSegment> segments = new List<JpegMarkerSegment>();

        if (reader == null)
        {
            return segments;
        }

        if (!ReadStartOfImage(reader, segments))
        {
            return segments;
        }

        while (!reader.EndOfRange())
        {
            JpegMarkerSegment? marker = ReadMarker(reader, false);
            if (marker == null)
            {
                MetadataDiagnostic.AddTruncated(reader.Diagnostics, "jpeg.marker.missing", "JPEG marker prefix was not found before end of data.", "JPEG", reader.Position, reader.Remaining());
                return segments;
            }

            bool keepScanning = ProcessMarker(reader, options, segments, marker);
            if (!keepScanning)
            {
                return segments;
            }
        }

        MetadataDiagnostic.AddTruncated(reader.Diagnostics, "jpeg.eoi.missing", "JPEG stream ended before an EOI marker was found.", "JPEG", reader.Position, 0);
        return segments;
    }

    internal static bool ReadStartOfImage(BoundedStreamReader reader, List<JpegMarkerSegment> segments)
    {
        if (reader == null)
        {
            return false;
        }

        long offset = reader.Position;
        int first = reader.ReadByte();
        int second = reader.ReadByte();

        if (first != MarkerPrefix || second != MarkerStartOfImage)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(reader.Diagnostics, "jpeg.soi.invalid", "JPEG stream did not start with an SOI marker.", "SOI", offset, 2);
            return false;
        }

        JpegMarkerSegment segment = CreateSegment(MarkerStartOfImage, offset);
        segment.IsStandalone = true;
        segment.SegmentLength = 2;
        segments.Add(segment);
        return true;
    }

    internal static JpegMarkerSegment? ReadMarker(BoundedStreamReader reader, bool entropyData)
    {
        while (!reader.EndOfRange())
        {
            int value = reader.ReadByte();
            if (value < 0)
            {
                return null;
            }

            if (value == MarkerPrefix)
            {
                long markerOffset = reader.Position - 1;
                int marker = ReadMarkerCode(reader);

                if (marker < 0)
                {
                    return null;
                }

                if (entropyData && marker == StuffedZero)
                {
                    continue;
                }

                if (!entropyData && marker == StuffedZero)
                {
                    MetadataDiagnostic.AddRecoverableErrorAt(reader.Diagnostics, "jpeg.marker.stuffed.outside.scan", "Stuffed JPEG byte was found outside entropy-coded data.", "JPEG", markerOffset, 2);
                    continue;
                }

                return CreateSegment(marker, markerOffset);
            }
        }

        return null;
    }

    internal static int ReadMarkerCode(BoundedStreamReader reader)
    {
        while (!reader.EndOfRange())
        {
            int value = reader.ReadByte();
            if (value < 0)
            {
                return -1;
            }

            if (value != MarkerPrefix)
            {
                return value;
            }
        }

        return -1;
    }

    internal static bool ProcessMarker(BoundedStreamReader reader, ImageMetadataReaderOptions options, List<JpegMarkerSegment> segments, JpegMarkerSegment segment)
    {
        if (reader == null || segment == null)
        {
            return false;
        }

        if (IsStandaloneMarker(segment.Marker))
        {
            segment.IsStandalone = true;
            segment.SegmentLength = 2;
            segments.Add(segment);
            return !segment.IsEndOfImage;
        }

        if (!ReadSegmentLength(reader, options, segment))
        {
            segments.Add(segment);
            return false;
        }

        segments.Add(segment);

        if (!reader.SkipBytes(segment.PayloadLength))
        {
            segment.IsTruncated = true;
            return false;
        }

        if (segment.IsStartOfScan)
        {
            return ScanEntropyData(reader, options, segments);
        }

        return true;
    }

    internal static bool ReadSegmentLength(BoundedStreamReader reader, ImageMetadataReaderOptions options, JpegMarkerSegment segment)
    {
        if (reader == null || segment == null)
        {
            return false;
        }

        long lengthOffset = reader.Position;
        int high = reader.ReadByte();
        int low = reader.ReadByte();

        if (high < 0 || low < 0)
        {
            segment.IsTruncated = true;
            MetadataDiagnostic.AddTruncated(reader.Diagnostics, "jpeg.segment.length.truncated", "JPEG segment length field was truncated.", segment.Name, lengthOffset, 2);
            return false;
        }

        long declaredLength = (high * 256) + low;
        segment.HasLength = true;
        segment.DeclaredLength = declaredLength;
        segment.PayloadOffset = reader.Position;

        if (declaredLength < 2)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(reader.Diagnostics, "jpeg.segment.length.invalid", "JPEG segment length was smaller than the length field.", segment.Name, lengthOffset, declaredLength);
            return false;
        }

        segment.PayloadLength = declaredLength - 2;
        segment.SegmentLength = declaredLength + 2;

        if (segment.PayloadLength > GetMaxSegmentBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(reader.Diagnostics, "jpeg.segment.limit", "JPEG segment payload exceeded the configured segment limit.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
        }

        if (segment.PayloadLength > reader.Remaining())
        {
            segment.IsTruncated = true;
            MetadataDiagnostic.AddTruncated(reader.Diagnostics, "jpeg.segment.truncated", "JPEG segment payload exceeded the available data.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
        }

        return true;
    }

    internal static bool ScanEntropyData(BoundedStreamReader reader, ImageMetadataReaderOptions options, List<JpegMarkerSegment> segments)
    {
        while (!reader.EndOfRange())
        {
            JpegMarkerSegment? marker = ReadMarker(reader, true);
            if (marker == null)
            {
                MetadataDiagnostic.AddTruncated(reader.Diagnostics, "jpeg.scan.truncated", "JPEG scan data ended before an EOI marker was found.", "SOS", reader.Position, 0);
                return false;
            }

            if (marker.IsRestartMarker)
            {
                marker.IsStandalone = true;
                marker.SegmentLength = 2;
                segments.Add(marker);
                continue;
            }

            return ProcessMarker(reader, options, segments, marker);
        }

        MetadataDiagnostic.AddTruncated(reader.Diagnostics, "jpeg.scan.truncated", "JPEG scan data ended before an EOI marker was found.", "SOS", reader.Position, 0);
        return false;
    }

    internal static JpegMarkerSegment CreateSegment(int marker, long markerOffset)
    {
        JpegMarkerSegment segment = new JpegMarkerSegment();
        segment.Marker = marker;
        segment.Name = MarkerName(marker);
        segment.MarkerOffset = markerOffset;
        segment.IsStartOfScan = marker == MarkerStartOfScan;
        segment.IsEndOfImage = marker == MarkerEndOfImage;
        segment.IsRestartMarker = IsRestartMarker(marker);
        return segment;
    }

    internal static bool IsStandaloneMarker(int marker)
    {
        if (marker == MarkerStartOfImage || marker == MarkerEndOfImage)
        {
            return true;
        }

        if (marker == 1)
        {
            return true;
        }

        return IsRestartMarker(marker);
    }

    internal static bool IsRestartMarker(int marker)
    {
        return marker >= 208 && marker <= 215;
    }

    internal static long GetMaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxSegmentBytes;
        }

        return options.MaxSegmentBytes;
    }

    internal static string MarkerName(int marker)
    {
        if (marker >= 224 && marker <= 239)
        {
            return "APP" + (marker - 224).ToString();
        }

        if (marker >= 208 && marker <= 215)
        {
            return "RST" + (marker - 208).ToString();
        }

        switch (marker)
        {
            case 1:
                return "TEM";
            case 192:
                return "SOF0";
            case 193:
                return "SOF1";
            case 194:
                return "SOF2";
            case 195:
                return "SOF3";
            case 196:
                return "DHT";
            case 197:
                return "SOF5";
            case 198:
                return "SOF6";
            case 199:
                return "SOF7";
            case 200:
                return "JPG";
            case 201:
                return "SOF9";
            case 202:
                return "SOF10";
            case 203:
                return "SOF11";
            case 204:
                return "DAC";
            case 205:
                return "SOF13";
            case 206:
                return "SOF14";
            case 207:
                return "SOF15";
            case 216:
                return "SOI";
            case 217:
                return "EOI";
            case 218:
                return "SOS";
            case 219:
                return "DQT";
            case 220:
                return "DNL";
            case 221:
                return "DRI";
            case 222:
                return "DHP";
            case 223:
                return "EXP";
            case 254:
                return "COM";
            default:
                return "FF" + HashHelper.ToHex(new byte[] { (byte)marker });
        }
    }
}
