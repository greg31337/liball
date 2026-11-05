using System;
using System.IO;

namespace image;

/// <summary>Detects image formats by examining their leading bytes and container signatures.</summary>
internal static class ImageFormatDetectorCore
{
    internal const int StreamProbeLength = 8192;

    /// <summary>Detects an image format from an in-memory byte sequence.</summary>
    /// <remarks>Detection uses signatures and container brands. An empty or unrecognized sequence returns <see cref="ImageFormatKind.Unknown"/>.</remarks>
    /// <param name="bytes">The bytes to inspect; a leading probe is sufficient for most formats. Null is treated as empty input.</param>
    /// <returns>A format description containing the detected kind and confidence.</returns>
    public static ImageFormatInfo DetectBytes(byte[]? bytes)
    {
        if (bytes == null)
        {
            return CreateUnknown("No image bytes were provided for format detection.");
        }

        return DetectSpan(bytes);
    }

    internal static ImageFormatInfo DetectBytesWithHint(byte[]? bytes, string? fileNameOrExtension)
    {
        if (bytes == null)
        {
            return ApplyHint(CreateUnknown("No image bytes were provided for format detection."), fileNameOrExtension);
        }

        return DetectSpanWithHint(bytes, fileNameOrExtension);
    }

    internal static ImageFormatInfo DetectSpan(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return CreateUnknown("No image bytes were provided for format detection.");
        }

        if (HasTgaFooter(bytes))
        {
            return Create(ImageFormatKind.Tga, "TGA", "", 100);
        }

        if (HasJpeg(bytes))
        {
            return Create(ImageFormatKind.Jpeg, "JPEG", "", 100);
        }

        if (HasPng(bytes))
        {
            return Create(ImageFormatKind.Png, "PNG", "", 100);
        }

        if (MatchesAscii(bytes, 0, "GIF87a") || MatchesAscii(bytes, 0, "GIF89a"))
        {
            return Create(ImageFormatKind.Gif, "GIF", "", 100);
        }

        if (MatchesAscii(bytes, 0, "RIFF") && MatchesAscii(bytes, 8, "WEBP"))
        {
            return Create(ImageFormatKind.WebP, "WebP", "", 100);
        }

        if (MatchesAscii(bytes, 0, "BM"))
        {
            return Create(ImageFormatKind.Bmp, "BMP", "", 100);
        }

        if (HasIco(bytes))
        {
            return Create(ImageFormatKind.Ico, "ICO", "", 100);
        }

        if (HasCur(bytes))
        {
            return Create(ImageFormatKind.Cur, "CUR", "", 100);
        }

        if (HasCr2(bytes))
        {
            return Create(ImageFormatKind.Cr2, "CR2", "", 100);
        }

        if (HasOrf(bytes))
        {
            return Create(ImageFormatKind.Orf, "ORF", "", 90);
        }

        if (HasRw2(bytes))
        {
            return Create(ImageFormatKind.Rw2, "RW2", "", 90);
        }

        if (HasCrw(bytes))
        {
            return Create(ImageFormatKind.Crw, "CRW", "", 100);
        }

        if (HasBigTiff(bytes))
        {
            return Create(ImageFormatKind.BigTiff, "BigTIFF", "", 100);
        }

        if (HasTiff(bytes))
        {
            return Create(ImageFormatKind.Tiff, "TIFF", "", 95);
        }

        if (MatchesAscii(bytes, 0, "FUJIFILMCCD-RAW"))
        {
            return Create(ImageFormatKind.Raf, "RAF", "", 100);
        }

        if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 77 && bytes[2] == 82 && bytes[3] == 77)
        {
            return Create(ImageFormatKind.Mrw, "MRW", "", 100);
        }

        if (HasJpeg2000Signature(bytes))
        {
            return Create(ImageFormatKind.Jpeg2000, "JPEG 2000", "", 100);
        }

        if (HasJpegXlSignature(bytes))
        {
            return Create(ImageFormatKind.JpegXl, "JPEG XL", "", 100);
        }

        if (HasJpegXlCodestream(bytes))
        {
            return Create(ImageFormatKind.JpegXl, "JPEG XL", "", 95);
        }

        ImageFormatInfo bmffInfo = DetectBmff(bytes);
        if (bmffInfo.DetectedByMagic)
        {
            return bmffInfo;
        }

        if (HasPsd(bytes))
        {
            return Create(ImageFormatKind.Psd, "PSD", "", 100);
        }

        if (HasPsb(bytes))
        {
            return Create(ImageFormatKind.Psb, "PSB", "", 100);
        }

        if (MatchesAscii(bytes, 0, "DDS "))
        {
            return Create(ImageFormatKind.Dds, "DDS", "", 100);
        }

        if (MatchesAscii(bytes, 0, "qoif"))
        {
            return Create(ImageFormatKind.Qoi, "QOI", "", 100);
        }

        if (MatchesAscii(bytes, 0, "#?RADIANCE") || MatchesAscii(bytes, 0, "#?RGBE"))
        {
            return Create(ImageFormatKind.RadianceHdr, "Radiance HDR", "", 100);
        }

        if (MatchesAscii(bytes, 0, "%!PS"))
        {
            return Create(ImageFormatKind.Eps, "EPS", "", 85);
        }

        if (bytes.Length >= 4 && bytes[0] == 197 && bytes[1] == 208 && bytes[2] == 211 && bytes[3] == 198)
        {
            return Create(ImageFormatKind.Eps, "EPS", "", 100);
        }

        if (HasNetpbm(bytes))
        {
            return Create(ImageFormatKind.Netpbm, "Netpbm", "", 90);
        }

        if (HasPcx(bytes))
        {
            return Create(ImageFormatKind.Pcx, "PCX", "", 90);
        }

        if (HasSvg(bytes))
        {
            return Create(ImageFormatKind.Svg, "SVG", "", 85);
        }

        if (HasXmp(bytes))
        {
            return Create(ImageFormatKind.Xmp, "XMP", "", 90);
        }

        if (HasGzip(bytes))
        {
            return Create(ImageFormatKind.Svgz, "SVGZ", "", 60);
        }

        return CreateUnknown("The image format was not recognized from the available bytes.");
    }

    internal static ImageFormatInfo DetectSpanWithHint(ReadOnlySpan<byte> bytes, string? fileNameOrExtension)
    {
        return ApplyHint(DetectSpan(bytes), fileNameOrExtension);
    }

    /// <summary>Detects an image format from the current position of a stream.</summary>
    /// <remarks>For a seekable stream, the original position is restored after probing. A non-seekable stream is consumed by up to the probe length.</remarks>
    /// <param name="stream">The readable stream to inspect, or null.</param>
    /// <returns>A format description containing the detected kind and confidence.</returns>
    public static ImageFormatInfo DetectStream(Stream? stream)
    {
        if (stream == null)
        {
            return CreateUnknown("No image stream was provided for format detection.");
        }

        long start = 0;
        if (stream.CanSeek)
        {
            start = stream.Position;
            long remainingForFooter = stream.Length - start;
            if (remainingForFooter >= 18)
            {
                stream.Seek(stream.Length - 18, SeekOrigin.Begin);
                byte[] footer = new byte[18];
                int footerRead = ReadUpTo(stream, footer, footer.Length);
                stream.Seek(start, SeekOrigin.Begin);
                if (footerRead == footer.Length && HasTgaFooter(footer))
                {
                    return Create(ImageFormatKind.Tga, "TGA", "", 100);
                }
            }
        }

        int count = StreamProbeLength;
        if (stream.CanSeek)
        {
            long remaining = stream.Length - stream.Position;
            if (remaining < count)
            {
                count = (int)remaining;
            }
        }

        byte[] bytes = new byte[count];
        int read = ReadUpTo(stream, bytes, count);

        if (stream.CanSeek)
        {
            stream.Seek(start, SeekOrigin.Begin);
        }

        return DetectSpan(new ReadOnlySpan<byte>(bytes, 0, read));
    }

    internal static ImageFormatInfo DetectStreamWithHint(Stream? stream, string? fileNameOrExtension)
    {
        return ApplyHint(DetectStream(stream), fileNameOrExtension);
    }

    internal static int ReadUpTo(Stream stream, byte[] buffer, int count)
    {
        if (stream == null || buffer == null || count <= 0)
        {
            return 0;
        }

        int maximum = count;
        if (maximum > buffer.Length)
        {
            maximum = buffer.Length;
        }

        int total = 0;
        while (total < maximum)
        {
            int read = stream.Read(buffer, total, maximum - total);
            if (read <= 0)
            {
                break;
            }

            total = total + read;
        }

        return total;
    }

    internal static ImageFormatInfo DetectBmff(ReadOnlySpan<byte> bytes)
    {
        BmffBrandInfo brandInfo = BmffBrandDetector.ReadSpan(bytes);
        if (!brandInfo.Valid)
        {
            return new ImageFormatInfo();
        }

        return Create(brandInfo.Kind, brandInfo.Name, brandInfo.MajorBrand, brandInfo.Confidence);
    }

    internal static ImageFormatInfo Create(ImageFormatKind kind, string name, string brand, int confidence)
    {
        ImageFormatInfo info = new ImageFormatInfo();
        info.Kind = kind;
        ImageFormatMapping.Apply(info);
        if (name.Length > 0)
        {
            info.Name = name;
        }
        info.ContainerBrand = brand;
        info.Confidence = confidence;
        info.DetectedByMagic = true;
        if (kind == ImageFormatKind.Unknown)
        {
            MetadataDiagnostic.AddUnsupported(info.Diagnostics, "format.unknown", "The container brand did not identify a supported image format.", "Format");
        }
        return info;
    }

    internal static ImageFormatInfo CreateUnknown(string message)
    {
        ImageFormatInfo info = new ImageFormatInfo();
        MetadataDiagnostic.AddUnsupported(info.Diagnostics, "format.unknown", message, "Format");
        return info;
    }

    internal static ImageFormatInfo ApplyHint(ImageFormatInfo detected, string? fileNameOrExtension)
    {
        ImageFormatKind hintedKind = ImageFormatMapping.KindFromFileNameOrExtension(fileNameOrExtension);
        if (hintedKind == ImageFormatKind.Unknown)
        {
            return detected;
        }

        if (detected.Kind == hintedKind)
        {
            return detected;
        }

        if (detected.Kind == ImageFormatKind.Unknown
            && detected.DetectedByMagic
            && detected.ContainerBrand.Length > 0
            && !ImageFormatMapping.IsBmffImage(hintedKind))
        {
            return detected;
        }

        if (detected.Kind != ImageFormatKind.Unknown
            && detected.Kind != ImageFormatKind.Tiff
            && detected.Kind != ImageFormatKind.BigTiff)
        {
            return detected;
        }

        if ((detected.Kind == ImageFormatKind.Tiff || detected.Kind == ImageFormatKind.BigTiff)
            && !ImageFormatMapping.IsTiffDerivedRaw(hintedKind))
        {
            return detected;
        }

        ImageFormatInfo hinted = ImageFormatMapping.CreateInfo(hintedKind);
        hinted.ContainerBrand = detected.ContainerBrand;
        hinted.Confidence = detected.Kind == ImageFormatKind.Unknown ? 40 : 75;
        hinted.DetectedByMagic = false;
        return hinted;
    }

    internal static bool HasJpeg(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255;
    }

    internal static bool HasPng(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 8
            && bytes[0] == 137
            && bytes[1] == 80
            && bytes[2] == 78
            && bytes[3] == 71
            && bytes[4] == 13
            && bytes[5] == 10
            && bytes[6] == 26
            && bytes[7] == 10;
    }

    internal static bool HasIco(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 1 && bytes[3] == 0;
    }

    internal static bool HasCur(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 2 && bytes[3] == 0;
    }

    internal static bool HasTiff(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 4
            && ((bytes[0] == 73 && bytes[1] == 73 && bytes[2] == 42 && bytes[3] == 0)
            || (bytes[0] == 77 && bytes[1] == 77 && bytes[2] == 0 && bytes[3] == 42));
    }

    internal static bool HasBigTiff(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 4
            && ((bytes[0] == 73 && bytes[1] == 73 && bytes[2] == 43 && bytes[3] == 0)
            || (bytes[0] == 77 && bytes[1] == 77 && bytes[2] == 0 && bytes[3] == 43));
    }

    internal static bool HasCr2(ReadOnlySpan<byte> bytes)
    {
        return HasTiff(bytes) && bytes.Length >= 12 && bytes[8] == 67 && bytes[9] == 82 && bytes[10] == 2 && bytes[11] == 0;
    }

    internal static bool HasOrf(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 4
            && bytes[0] == 73
            && bytes[1] == 73
            && ((bytes[2] == 82 && bytes[3] == 79) || (bytes[2] == 82 && bytes[3] == 83));
    }

    internal static bool HasRw2(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 4 && bytes[0] == 73 && bytes[1] == 73 && bytes[2] == 85 && bytes[3] == 0;
    }

    internal static bool HasCrw(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 14)
        {
            return false;
        }

        bool byteOrder = bytes[0] == 73 && bytes[1] == 73 || bytes[0] == 77 && bytes[1] == 77;
        return byteOrder && MatchesAscii(bytes, 6, "HEAPCCDR");
    }

    internal static bool HasJpeg2000Signature(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 12
            && bytes[0] == 0
            && bytes[1] == 0
            && bytes[2] == 0
            && bytes[3] == 12
            && bytes[4] == 106
            && bytes[5] == 80
            && bytes[6] == 32
            && bytes[7] == 32
            && bytes[8] == 13
            && bytes[9] == 10
            && bytes[10] == 135
            && bytes[11] == 10;
    }

    internal static bool HasJpegXlSignature(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 12
            && bytes[0] == 0
            && bytes[1] == 0
            && bytes[2] == 0
            && bytes[3] == 12
            && bytes[4] == 74
            && bytes[5] == 88
            && bytes[6] == 76
            && bytes[7] == 32
            && bytes[8] == 13
            && bytes[9] == 10
            && bytes[10] == 135
            && bytes[11] == 10;
    }

    internal static bool HasJpegXlCodestream(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 10;
    }

    internal static bool HasPsd(ReadOnlySpan<byte> bytes)
    {
        return MatchesAscii(bytes, 0, "8BPS") && bytes.Length >= 6 && bytes[4] == 0 && bytes[5] == 1;
    }

    internal static bool HasPsb(ReadOnlySpan<byte> bytes)
    {
        return MatchesAscii(bytes, 0, "8BPS") && bytes.Length >= 6 && bytes[4] == 0 && bytes[5] == 2;
    }

    internal static bool HasNetpbm(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2 || bytes[0] != 80)
        {
            return false;
        }

        byte second = bytes[1];
        if (second >= 49 && second <= 55)
        {
            return true;
        }

        if (second == 70 || second == 102)
        {
            return true;
        }

        return false;
    }

    internal static bool HasPcx(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 10 || bytes[2] != 1)
        {
            return false;
        }

        byte version = bytes[1];
        return version == 0 || version == 2 || version == 3 || version == 4 || version == 5;
    }

    internal static bool HasTgaFooter(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 18 && MatchesAscii(bytes, bytes.Length - 18, "TRUEVISION-XFILE.") && bytes[bytes.Length - 1] == 0;
    }

    internal static bool HasSvg(ReadOnlySpan<byte> bytes)
    {
        if (!ContainsAsciiIgnoreCase(bytes, "<svg", 512))
        {
            return false;
        }

        int index = FirstNonWhitespace(bytes);
        if (index < 0)
        {
            return false;
        }

        return bytes[index] == 60 || HasUtf8Bom(bytes);
    }

    internal static bool HasGzip(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 2 && bytes[0] == 31 && bytes[1] == 139;
    }

    internal static bool HasXmp(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return false;
        }

        string rdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        if (ContainsAsciiIgnoreCase(bytes, rdfNamespace, StreamProbeLength))
        {
            int first = FirstNonWhitespace(bytes);
            return first >= 0 && (bytes[first] == 60 || HasUtf8Bom(bytes));
        }

        if (bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 254)
        {
            return ContainsUtf16Ascii(bytes, rdfNamespace, true, StreamProbeLength) && ContainsUtf16Ascii(bytes, "rdf", true, StreamProbeLength);
        }

        if (bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == 255)
        {
            return ContainsUtf16Ascii(bytes, rdfNamespace, false, StreamProbeLength) && ContainsUtf16Ascii(bytes, "rdf", false, StreamProbeLength);
        }

        return false;
    }

    internal static bool ContainsUtf16Ascii(ReadOnlySpan<byte> bytes, string text, bool littleEndian, int maxBytes)
    {
        if (text == null || text.Length == 0)
        {
            return false;
        }

        int length = bytes.Length;
        if (length > maxBytes)
        {
            length = maxBytes;
        }

        int offset = 0;
        while (offset <= length - text.Length * 2)
        {
            int index = 0;
            while (index < text.Length)
            {
                int characterOffset = offset + index * 2;
                byte value = littleEndian ? bytes[characterOffset] : bytes[characterOffset + 1];
                byte zero = littleEndian ? bytes[characterOffset + 1] : bytes[characterOffset];
                if (zero != 0 || ToLowerAscii(value) != ToLowerAscii((byte)text[index]))
                {
                    break;
                }

                index = index + 1;
            }

            if (index == text.Length)
            {
                return true;
            }

            offset = offset + 1;
        }

        return false;
    }

    internal static bool MatchesAscii(ReadOnlySpan<byte> bytes, int offset, string text)
    {
        if (text == null || offset < 0)
        {
            return false;
        }

        if (bytes.Length - offset < text.Length)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            if (bytes[offset + index] != (byte)text[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ContainsAsciiIgnoreCase(ReadOnlySpan<byte> bytes, string text, int maxBytes)
    {
        if (text == null || text.Length == 0)
        {
            return false;
        }

        int length = bytes.Length;
        if (length > maxBytes)
        {
            length = maxBytes;
        }

        int index = 0;
        while (index <= length - text.Length)
        {
            if (MatchesAsciiIgnoreCase(bytes, index, text))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool MatchesAsciiIgnoreCase(ReadOnlySpan<byte> bytes, int offset, string text)
    {
        if (text == null || offset < 0 || bytes.Length - offset < text.Length)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            byte left = ToLowerAscii(bytes[offset + index]);
            byte right = ToLowerAscii((byte)text[index]);
            if (left != right)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static byte ToLowerAscii(byte value)
    {
        if (value >= 65 && value <= 90)
        {
            return (byte)(value + 32);
        }

        return value;
    }

    internal static int FirstNonWhitespace(ReadOnlySpan<byte> bytes)
    {
        int index = 0;
        if (HasUtf8Bom(bytes))
        {
            index = 3;
        }

        while (index < bytes.Length)
        {
            byte value = bytes[index];
            if (value != 32 && value != 9 && value != 10 && value != 13)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool HasUtf8Bom(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191;
    }

    internal static bool IsBrand(string left, string right)
    {
        return left == right;
    }

    internal static string ReadAscii(ReadOnlySpan<byte> bytes, int offset, int count)
    {
        if (offset < 0 || count < 0 || offset > bytes.Length || count > bytes.Length - offset)
        {
            return "";
        }

        char[] chars = new char[count];
        int index = 0;
        while (index < count)
        {
            chars[index] = (char)bytes[offset + index];
            index = index + 1;
        }

        return new string(chars);
    }
}
