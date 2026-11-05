using System.Globalization;
using System.Text;

namespace pptx;

internal static class PptxImageDetectorCore
{
    internal static PptxImageInfo Detect(byte[] bytes)
    {
        if (bytes == null)
        {
            return new PptxImageInfo();
        }

        return DetectSpan(bytes);
    }
    internal static PptxImageInfo DetectSpan(System.ReadOnlySpan<byte> bytes)
    {
        PptxImageInfo info = new PptxImageInfo();

        if (LoadPng(bytes, info))
        {
            return info;
        }

        if (LoadJpeg(bytes, info))
        {
            return info;
        }

        if (LoadGif(bytes, info))
        {
            return info;
        }

        if (LoadBmp(bytes, info))
        {
            return info;
        }

        if (LoadTiff(bytes, info))
        {
            return info;
        }

        if (LoadEmf(bytes, info))
        {
            return info;
        }

        if (LoadWmf(bytes, info))
        {
            return info;
        }

        if (LoadSvg(bytes, info))
        {
            return info;
        }

        return info;
    }
    internal static PptxImageInfo DetectPart(PptxPart part)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        return Detect(part.GetBytes());
    }

    internal static bool LoadPng(System.ReadOnlySpan<byte> bytes, PptxImageInfo info)
    {
        if (bytes.Length < 24)
        {
            return false;
        }

        if (bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78 || bytes[3] != 71 || bytes[4] != 13 || bytes[5] != 10 || bytes[6] != 26 || bytes[7] != 10)
        {
            return false;
        }

        info.Type = PptxImageType.Png;
        info.ContentType = PptxContentTypes.Png;
        info.Extension = "png";
        info.WidthPixels = ReadInt32Big(bytes, 16);
        info.HeightPixels = ReadInt32Big(bytes, 20);
        return true;
    }

    internal static bool LoadJpeg(System.ReadOnlySpan<byte> bytes, PptxImageInfo info)
    {
        if (bytes.Length < 4)
        {
            return false;
        }

        if (bytes[0] != 255 || bytes[1] != 216)
        {
            return false;
        }

        info.Type = PptxImageType.Jpeg;
        info.ContentType = PptxContentTypes.Jpeg;
        info.Extension = "jpg";
        int index = 2;
        while (index <= bytes.Length - 4)
        {
            if (bytes[index] != 255)
            {
                index = index + 1;
                continue;
            }

            int marker = bytes[index + 1];
            if (marker == 217 || marker == 218)
            {
                break;
            }

            int length = ReadUInt16Big(bytes, index + 2);
            if (length < 2 || length >= bytes.Length - index - 1)
            {
                break;
            }

            if (IsJpegStartOfFrame(marker) && length >= 7)
            {
                info.HeightPixels = ReadUInt16Big(bytes, index + 5);
                info.WidthPixels = ReadUInt16Big(bytes, index + 7);
                return true;
            }

            index = index + 2 + length;
        }

        return true;
    }

    internal static bool IsJpegStartOfFrame(int marker)
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

    internal static bool LoadGif(System.ReadOnlySpan<byte> bytes, PptxImageInfo info)
    {
        if (bytes.Length < 10)
        {
            return false;
        }

        if (!StartsWithAscii(bytes, "GIF87a") && !StartsWithAscii(bytes, "GIF89a"))
        {
            return false;
        }

        info.Type = PptxImageType.Gif;
        info.ContentType = PptxContentTypes.Gif;
        info.Extension = "gif";
        info.WidthPixels = ReadUInt16Little(bytes, 6);
        info.HeightPixels = ReadUInt16Little(bytes, 8);
        return true;
    }

    internal static bool LoadBmp(System.ReadOnlySpan<byte> bytes, PptxImageInfo info)
    {
        if (bytes.Length < 26)
        {
            return false;
        }

        if (bytes[0] != 66 || bytes[1] != 77)
        {
            return false;
        }

        int width = ReadInt32Little(bytes, 18);
        int height = ReadInt32Little(bytes, 22);
        if (height < 0)
        {
            height = AbsoluteImageDimension(height);
        }

        info.Type = PptxImageType.Bmp;
        info.ContentType = PptxContentTypes.Bmp;
        info.Extension = "bmp";
        info.WidthPixels = width;
        info.HeightPixels = height;
        return true;
    }

    internal static bool LoadTiff(System.ReadOnlySpan<byte> bytes, PptxImageInfo info)
    {
        if (bytes.Length < 8)
        {
            return false;
        }

        bool little = false;
        if (bytes[0] == 73 && bytes[1] == 73 && ReadUInt16Little(bytes, 2) == 42)
        {
            little = true;
        }
        else
        {
            if (bytes[0] != 77 || bytes[1] != 77 || ReadUInt16Big(bytes, 2) != 42)
            {
                return false;
            }
        }

        info.Type = PptxImageType.Tiff;
        info.ContentType = PptxContentTypes.Tiff;
        info.Extension = "tif";
        int offset = ReadInt32Endian(bytes, 4, little);
        if (offset < 0 || offset > bytes.Length - 2)
        {
            return true;
        }

        int count = ReadUInt16Endian(bytes, offset, little);
        for (int index = 0; index < count; index++)
        {
            long entryOffset = (long)offset + 2L + (long)index * 12L;
            if (entryOffset > (long)bytes.Length - 12L)
            {
                break;
            }

            int entry = (int)entryOffset;
            int tag = ReadUInt16Endian(bytes, entry, little);
            int type = ReadUInt16Endian(bytes, entry + 2, little);
            int value = ReadTiffInlineValue(bytes, entry, little, type);
            if (tag == 256)
            {
                info.WidthPixels = value;
            }

            if (tag == 257)
            {
                info.HeightPixels = value;
            }
        }

        return true;
    }

    internal static int ReadTiffInlineValue(System.ReadOnlySpan<byte> bytes, int entry, bool little, int type)
    {
        if (type == 3)
        {
            return ReadUInt16Endian(bytes, entry + 8, little);
        }

        if (type == 4)
        {
            return ReadInt32Endian(bytes, entry + 8, little);
        }

        return 0;
    }

    internal static bool LoadSvg(System.ReadOnlySpan<byte> bytes, PptxImageInfo info)
    {
        if (bytes.Length == 0)
        {
            return false;
        }

        string xml = Encoding.UTF8.GetString(bytes);
        int svgIndex = FindSvgStartTag(xml);
        if (svgIndex < 0)
        {
            return false;
        }

        int svgEnd = xml.IndexOf(">", svgIndex, System.StringComparison.Ordinal);
        if (svgEnd < 0)
        {
            return false;
        }

        string svgTag = xml.Substring(svgIndex, svgEnd - svgIndex + 1);

        info.Type = PptxImageType.Svg;
        info.ContentType = PptxContentTypes.Svg;
        info.Extension = "svg";
        double width = ParseSvgAttributeNumber(svgTag, "width");
        double height = ParseSvgAttributeNumber(svgTag, "height");
        if (width <= 0.0 || height <= 0.0)
        {
            LoadSvgViewBox(svgTag, info);
            return true;
        }

        info.WidthPixels = RoundImageDimension(width);
        info.HeightPixels = RoundImageDimension(height);
        return true;
    }

    internal static void LoadSvgViewBox(string xml, PptxImageInfo info)
    {
        string viewBox = GetSvgAttribute(xml, "viewBox");
        if (viewBox.Length == 0)
        {
            return;
        }

        string[] parts = viewBox.Split(new char[] { ' ', '\t', '\r', '\n', ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
        {
            return;
        }

        double width = ParseDouble(parts[2]);
        double height = ParseDouble(parts[3]);
        if (width > 0.0 && height > 0.0)
        {
            info.WidthPixels = RoundImageDimension(width);
            info.HeightPixels = RoundImageDimension(height);
        }
    }

    internal static bool LoadEmf(System.ReadOnlySpan<byte> bytes, PptxImageInfo info)
    {
        if (bytes.Length < 44)
        {
            return false;
        }

        if (ReadInt32Little(bytes, 0) != 1)
        {
            return false;
        }

        if (bytes[40] != 32 || bytes[41] != 69 || bytes[42] != 77 || bytes[43] != 70)
        {
            return false;
        }

        int left = ReadInt32Little(bytes, 8);
        int top = ReadInt32Little(bytes, 12);
        int right = ReadInt32Little(bytes, 16);
        int bottom = ReadInt32Little(bytes, 20);
        info.Type = PptxImageType.Emf;
        info.ContentType = PptxContentTypes.Emf;
        info.Extension = "emf";
        info.WidthPixels = ImageDimensionDifference(left, right);
        info.HeightPixels = ImageDimensionDifference(top, bottom);
        return true;
    }

    internal static bool LoadWmf(System.ReadOnlySpan<byte> bytes, PptxImageInfo info)
    {
        if (bytes.Length < 18)
        {
            return false;
        }

        if (bytes[0] != 215 || bytes[1] != 205 || bytes[2] != 198 || bytes[3] != 154)
        {
            return false;
        }

        int left = ReadInt16Little(bytes, 6);
        int top = ReadInt16Little(bytes, 8);
        int right = ReadInt16Little(bytes, 10);
        int bottom = ReadInt16Little(bytes, 12);
        info.Type = PptxImageType.Wmf;
        info.ContentType = PptxContentTypes.Wmf;
        info.Extension = "wmf";
        info.WidthPixels = ImageDimensionDifference(left, right);
        info.HeightPixels = ImageDimensionDifference(top, bottom);
        return true;
    }

    internal static int AbsoluteImageDimension(int value)
    {
        if (value == int.MinValue)
        {
            return 0;
        }

        if (value < 0)
        {
            return 0 - value;
        }

        return value;
    }

    internal static int ImageDimensionDifference(int start, int end)
    {
        long difference = (long)end - (long)start;
        if (difference <= 0L || difference > int.MaxValue)
        {
            return 0;
        }

        return (int)difference;
    }

    internal static int RoundImageDimension(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0 || value > int.MaxValue)
        {
            return 0;
        }

        return PptxUnitsCore.RoundToInt(value);
    }

    internal static int FindSvgStartTag(string xml)
    {
        int searchIndex = 0;
        while (searchIndex <= xml.Length - 4)
        {
            int index = xml.IndexOf("<svg", searchIndex, System.StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return -1;
            }

            int boundaryIndex = index + 4;
            if (boundaryIndex < xml.Length && IsSvgTagBoundary(xml[boundaryIndex]) && !IsInsideXmlComment(xml, index))
            {
                return index;
            }

            searchIndex = boundaryIndex;
        }

        return -1;
    }

    internal static bool IsSvgTagBoundary(char value)
    {
        return char.IsWhiteSpace(value) || value == '>' || value == '/';
    }

    internal static bool IsInsideXmlComment(string xml, int index)
    {
        int commentStart = xml.LastIndexOf("<!--", index, System.StringComparison.Ordinal);
        if (commentStart < 0)
        {
            return false;
        }

        int commentEnd = xml.LastIndexOf("-->", index, System.StringComparison.Ordinal);
        return commentEnd < commentStart;
    }

    internal static string GetSvgAttribute(string xml, string name)
    {
        int searchIndex = 0;
        while (searchIndex <= xml.Length - name.Length)
        {
            int index = xml.IndexOf(name, searchIndex, System.StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return "";
            }

            int position = index + name.Length;
            bool beforeName = index == 0 || char.IsWhiteSpace(xml[index - 1]) || xml[index - 1] == '<' || xml[index - 1] == '/';
            bool afterName = position >= xml.Length || char.IsWhiteSpace(xml[position]) || xml[position] == '=';
            if (beforeName && afterName)
            {
                while (position < xml.Length && char.IsWhiteSpace(xml[position]))
                {
                    position = position + 1;
                }

                if (position < xml.Length && xml[position] == '=')
                {
                    position = position + 1;
                    while (position < xml.Length && char.IsWhiteSpace(xml[position]))
                    {
                        position = position + 1;
                    }

                    if (position < xml.Length && (xml[position] == '\"' || xml[position] == '\''))
                    {
                        char quote = xml[position];
                        int start = position + 1;
                        int end = xml.IndexOf(quote, start);
                        if (end >= start)
                        {
                            return xml.Substring(start, end - start);
                        }
                    }
                }
            }

            searchIndex = index + name.Length;
        }

        return "";
    }

    internal static double ParseSvgAttributeNumber(string xml, string name)
    {
        return ParseSvgNumber(GetSvgAttribute(xml, name));
    }

    internal static double ParseSvgNumber(string value)
    {
        string clean = PptxDiagnostic.CleanText(value).Trim();
        if (clean.Length == 0)
        {
            return 0.0;
        }

        int length = 0;
        while (length < clean.Length)
        {
            char current = clean[length];
            if ((current >= '0' && current <= '9') || current == '.' || current == '-' || current == '+')
            {
                length = length + 1;
            }
            else
            {
                break;
            }
        }

        if (length == 0)
        {
            return 0.0;
        }

        return ParseDouble(clean.Substring(0, length));
    }

    internal static double ParseDouble(string value)
    {
        double result = 0.0;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        return 0.0;
    }

    internal static bool StartsWithAscii(System.ReadOnlySpan<byte> bytes, string value)
    {
        if (bytes.Length < value.Length)
        {
            return false;
        }

        for (int index = 0; index < value.Length; index++)
        {
            if (bytes[index] != (byte)value[index])
            {
                return false;
            }
        }

        return true;
    }

    internal static int ReadUInt16Little(System.ReadOnlySpan<byte> bytes, int offset)
    {
        return bytes[offset] + bytes[offset + 1] * 256;
    }

    internal static int ReadUInt16Big(System.ReadOnlySpan<byte> bytes, int offset)
    {
        return bytes[offset] * 256 + bytes[offset + 1];
    }

    internal static int ReadInt16Little(System.ReadOnlySpan<byte> bytes, int offset)
    {
        int value = ReadUInt16Little(bytes, offset);
        if (value >= 32768)
        {
            value = value - 65536;
        }

        return value;
    }

    internal static int ReadInt32Little(System.ReadOnlySpan<byte> bytes, int offset)
    {
        long value = bytes[offset] + bytes[offset + 1] * 256L + bytes[offset + 2] * 65536L + bytes[offset + 3] * 16777216L;
        if (value >= 2147483648L)
        {
            value = value - 4294967296L;
        }

        return (int)value;
    }

    internal static int ReadInt32Big(System.ReadOnlySpan<byte> bytes, int offset)
    {
        long value = bytes[offset] * 16777216L + bytes[offset + 1] * 65536L + bytes[offset + 2] * 256L + bytes[offset + 3];
        if (value >= 2147483648L)
        {
            value = value - 4294967296L;
        }

        return (int)value;
    }

    internal static int ReadUInt16Endian(System.ReadOnlySpan<byte> bytes, int offset, bool little)
    {
        if (little)
        {
            return ReadUInt16Little(bytes, offset);
        }

        return ReadUInt16Big(bytes, offset);
    }

    internal static int ReadInt32Endian(System.ReadOnlySpan<byte> bytes, int offset, bool little)
    {
        if (little)
        {
            return ReadInt32Little(bytes, offset);
        }

        return ReadInt32Big(bytes, offset);
    }
}
