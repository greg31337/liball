using System;
using System.IO;

namespace pdf;

internal class PdfJpegParseState
{
    internal int Width;
    internal int Height;
    internal int ComponentCount;
    internal int BitsPerComponent;
    internal int[] ComponentIdentifiers;
    internal bool HasFrame;
    internal bool HasScan;
    internal bool HasEndMarker;
    internal bool HasJfifDpi;
    internal double JfifHorizontalDpi;
    internal double JfifVerticalDpi;
    internal bool HasExifDpi;
    internal double ExifHorizontalDpi;
    internal double ExifVerticalDpi;
    internal bool HasAdobeMarker;
    internal int AdobeTransform;

    internal PdfJpegParseState()
    {
        ComponentIdentifiers = new int[0];
        AdobeTransform = -1;
    }
}

internal static class PdfJpegParser
{
    internal static PdfJpegData Parse(byte[] data)
    {
        if (data.Length < 4 || data[0] != 255 || data[1] != 216)
        {
            throw new InvalidDataException("JPEG data must begin with an SOI marker.");
        }

        PdfJpegParseState state = new PdfJpegParseState();
        int offset = 2;
        while (offset < data.Length)
        {
            if (data[offset] != 255)
            {
                throw new InvalidDataException("JPEG marker prefix is missing.");
            }

            int markerOffset = offset;
            while (offset < data.Length && data[offset] == 255)
            {
                offset++;
            }

            if (offset >= data.Length)
            {
                throw new InvalidDataException("JPEG marker is truncated.");
            }

            int marker = data[offset];
            offset++;
            if (marker == 0)
            {
                throw new InvalidDataException("A stuffed JPEG byte was found outside scan data.");
            }

            if (marker == 217)
            {
                state.HasEndMarker = true;
                break;
            }

            if (marker == 216)
            {
                throw new InvalidDataException("JPEG data contains an unexpected SOI marker.");
            }

            if (marker == 1)
            {
                continue;
            }

            if (marker >= 208 && marker <= 215)
            {
                throw new InvalidDataException("A JPEG restart marker was found outside scan data.");
            }

            if (offset + 2 > data.Length)
            {
                throw new InvalidDataException("JPEG segment length is truncated.");
            }

            int segmentLength = ReadUInt16(data, offset);
            if (segmentLength < 2)
            {
                throw new InvalidDataException("JPEG segment length is invalid.");
            }

            int segmentEnd = offset + segmentLength;
            if (segmentEnd < offset || segmentEnd > data.Length)
            {
                throw new InvalidDataException("JPEG segment extends beyond the available data.");
            }

            int payloadOffset = offset + 2;
            int payloadLength = segmentLength - 2;
            if (marker == 224)
            {
                ParseJfif(data, payloadOffset, payloadLength, state);
            }
            else if (marker == 225)
            {
                ParseExif(data, payloadOffset, payloadLength, state);
            }
            else if (marker == 238)
            {
                ParseAdobe(data, payloadOffset, payloadLength, state);
            }
            else if (IsStartOfFrame(marker))
            {
                if (marker != 192 && marker != 193 && marker != 194)
                {
                    throw new InvalidDataException("The JPEG frame encoding is not supported.");
                }

                ParseFrame(data, payloadOffset, payloadLength, state);
            }
            else if (marker == 218)
            {
                ParseScan(data, payloadOffset, payloadLength, state);
            }

            offset = segmentEnd;
            if (marker == 218)
            {
                offset = FindMarkerAfterScan(data, offset);
            }

            if (offset == markerOffset)
            {
                throw new InvalidDataException("JPEG marker parsing did not advance.");
            }
        }

        if (!state.HasFrame)
        {
            throw new InvalidDataException("JPEG data does not contain a supported frame header.");
        }

        if (!state.HasScan)
        {
            throw new InvalidDataException("JPEG data does not contain scan data.");
        }

        if (!state.HasEndMarker)
        {
            throw new InvalidDataException("JPEG data does not end with an EOI marker.");
        }

        ValidateAdobeTransform(state);
        PdfJpegColorSpace colorSpace = GetColorSpace(state.ComponentCount);
        bool hasDpi = state.HasJfifDpi || state.HasExifDpi;
        double horizontalDpi = 0;
        double verticalDpi = 0;
        if (state.HasJfifDpi)
        {
            horizontalDpi = state.JfifHorizontalDpi;
            verticalDpi = state.JfifVerticalDpi;
        }
        else if (state.HasExifDpi)
        {
            horizontalDpi = state.ExifHorizontalDpi;
            verticalDpi = state.ExifVerticalDpi;
        }

        bool invertedCmyk = state.ComponentCount == 4 && state.HasAdobeMarker;
        return new PdfJpegData(state.Width, state.Height, state.ComponentCount, state.BitsPerComponent, colorSpace, hasDpi, horizontalDpi, verticalDpi, invertedCmyk, data);
    }

    internal static int ReadUInt16(byte[] data, int offset)
    {
        return data[offset] * 256 + data[offset + 1];
    }

    internal static bool IsStartOfFrame(int marker)
    {
        if (marker < 192 || marker > 207)
        {
            return false;
        }

        return marker != 196 && marker != 200 && marker != 204;
    }

    internal static void ParseFrame(byte[] data, int offset, int length, PdfJpegParseState state)
    {
        if (state.HasFrame)
        {
            throw new InvalidDataException("JPEG data contains more than one frame header.");
        }

        if (length < 6)
        {
            throw new InvalidDataException("JPEG frame header is truncated.");
        }

        int precision = data[offset];
        int height = ReadUInt16(data, offset + 1);
        int width = ReadUInt16(data, offset + 3);
        int componentCount = data[offset + 5];
        if (precision != 8)
        {
            throw new InvalidDataException("JPEG component precision must be 8 bits.");
        }

        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException("JPEG frame dimensions must be positive.");
        }

        if (componentCount != 1 && componentCount != 3 && componentCount != 4)
        {
            throw new InvalidDataException("JPEG images must have one, three, or four components.");
        }

        if (length != 6 + componentCount * 3)
        {
            throw new InvalidDataException("JPEG frame component data has an invalid length.");
        }

        int[] identifiers = new int[componentCount];
        for (int index = 0; index < componentCount; index++)
        {
            int componentOffset = offset + 6 + index * 3;
            int identifier = data[componentOffset];
            int sampling = data[componentOffset + 1];
            if (identifier == 0)
            {
                throw new InvalidDataException("JPEG component identifiers must be nonzero.");
            }

            if ((sampling >> 4) == 0 || (sampling & 15) == 0)
            {
                throw new InvalidDataException("JPEG sampling factors must be positive.");
            }

            for (int previous = 0; previous < index; previous++)
            {
                if (identifiers[previous] == identifier)
                {
                    throw new InvalidDataException("JPEG component identifiers must be unique.");
                }
            }

            identifiers[index] = identifier;
        }

        state.Width = width;
        state.Height = height;
        state.ComponentCount = componentCount;
        state.BitsPerComponent = precision;
        state.ComponentIdentifiers = identifiers;
        state.HasFrame = true;
    }

    internal static void ParseScan(byte[] data, int offset, int length, PdfJpegParseState state)
    {
        if (!state.HasFrame)
        {
            throw new InvalidDataException("JPEG scan data appears before its frame header.");
        }

        if (length < 4)
        {
            throw new InvalidDataException("JPEG scan header is truncated.");
        }

        int scanComponents = data[offset];
        if (scanComponents <= 0 || scanComponents > state.ComponentCount || length != 4 + scanComponents * 2)
        {
            throw new InvalidDataException("JPEG scan component data has an invalid length.");
        }

        int[] usedIdentifiers = new int[scanComponents];
        for (int index = 0; index < scanComponents; index++)
        {
            int identifier = data[offset + 1 + index * 2];
            if (!ContainsIdentifier(state.ComponentIdentifiers, identifier))
            {
                throw new InvalidDataException("JPEG scan references an unknown component.");
            }

            for (int previous = 0; previous < index; previous++)
            {
                if (usedIdentifiers[previous] == identifier)
                {
                    throw new InvalidDataException("JPEG scan component identifiers must be unique.");
                }
            }

            usedIdentifiers[index] = identifier;
        }

        state.HasScan = true;
    }

    internal static bool ContainsIdentifier(int[] identifiers, int identifier)
    {
        for (int index = 0; index < identifiers.Length; index++)
        {
            if (identifiers[index] == identifier)
            {
                return true;
            }
        }

        return false;
    }

    internal static int FindMarkerAfterScan(byte[] data, int offset)
    {
        while (offset < data.Length)
        {
            if (data[offset] != 255)
            {
                offset++;
                continue;
            }

            int markerOffset = offset;
            while (offset < data.Length && data[offset] == 255)
            {
                offset++;
            }

            if (offset >= data.Length)
            {
                throw new InvalidDataException("JPEG scan data is truncated.");
            }

            int marker = data[offset];
            if (marker == 0 || marker >= 208 && marker <= 215)
            {
                offset++;
                continue;
            }

            return markerOffset;
        }

        throw new InvalidDataException("JPEG scan data does not contain a following marker.");
    }

    internal static void ParseJfif(byte[] data, int offset, int length, PdfJpegParseState state)
    {
        if (!MatchesAscii(data, offset, length, "JFIF\0"))
        {
            return;
        }

        if (length < 14)
        {
            throw new InvalidDataException("JFIF metadata is truncated.");
        }

        int thumbnailWidth = data[offset + 12];
        int thumbnailHeight = data[offset + 13];
        long expectedLength = 14 + (long)thumbnailWidth * thumbnailHeight * 3;
        if (length < expectedLength)
        {
            throw new InvalidDataException("JFIF thumbnail data is truncated.");
        }

        int units = data[offset + 7];
        if (units < 0 || units > 2)
        {
            throw new InvalidDataException("JFIF density units are invalid.");
        }

        if (units == 0)
        {
            return;
        }

        int horizontalDensity = ReadUInt16(data, offset + 8);
        int verticalDensity = ReadUInt16(data, offset + 10);
        if (horizontalDensity == 0 || verticalDensity == 0)
        {
            throw new InvalidDataException("JFIF physical density values must be positive.");
        }

        state.HasJfifDpi = true;
        if (units == 1)
        {
            state.JfifHorizontalDpi = horizontalDensity;
            state.JfifVerticalDpi = verticalDensity;
        }
        else
        {
            state.JfifHorizontalDpi = horizontalDensity * 2.54;
            state.JfifVerticalDpi = verticalDensity * 2.54;
        }
    }

    internal static void ParseExif(byte[] data, int offset, int length, PdfJpegParseState state)
    {
        if (!MatchesAscii(data, offset, length, "Exif\0\0"))
        {
            return;
        }

        int tiffOffset = offset + 6;
        int tiffLength = length - 6;
        if (tiffLength < 8)
        {
            throw new InvalidDataException("Exif TIFF metadata is truncated.");
        }

        bool littleEndian;
        if (data[tiffOffset] == 73 && data[tiffOffset + 1] == 73)
        {
            littleEndian = true;
        }
        else if (data[tiffOffset] == 77 && data[tiffOffset + 1] == 77)
        {
            littleEndian = false;
        }
        else
        {
            throw new InvalidDataException("Exif byte order is invalid.");
        }

        if (ReadTiffUInt16(data, tiffOffset + 2, littleEndian) != 42)
        {
            throw new InvalidDataException("Exif TIFF signature is invalid.");
        }

        long directoryRelativeOffset = ReadTiffUInt32(data, tiffOffset + 4, littleEndian);
        if (!HasTiffRange(tiffLength, directoryRelativeOffset, 2))
        {
            throw new InvalidDataException("Exif image directory offset is invalid.");
        }

        int directoryOffset = tiffOffset + (int)directoryRelativeOffset;
        int entryCount = ReadTiffUInt16(data, directoryOffset, littleEndian);
        long directoryLength = 2 + (long)entryCount * 12;
        if (!HasTiffRange(tiffLength, directoryRelativeOffset, directoryLength))
        {
            throw new InvalidDataException("Exif image directory is truncated.");
        }

        double horizontalResolution = 0;
        double verticalResolution = 0;
        int resolutionUnit = 0;
        for (int index = 0; index < entryCount; index++)
        {
            int entryOffset = directoryOffset + 2 + index * 12;
            int tag = ReadTiffUInt16(data, entryOffset, littleEndian);
            int type = ReadTiffUInt16(data, entryOffset + 2, littleEndian);
            long count = ReadTiffUInt32(data, entryOffset + 4, littleEndian);
            if (tag == 282 || tag == 283)
            {
                if (type != 5 || count != 1)
                {
                    throw new InvalidDataException("Exif resolution metadata has an invalid type.");
                }

                long valueRelativeOffset = ReadTiffUInt32(data, entryOffset + 8, littleEndian);
                if (!HasTiffRange(tiffLength, valueRelativeOffset, 8))
                {
                    throw new InvalidDataException("Exif resolution value is outside the TIFF data.");
                }

                int valueOffset = tiffOffset + (int)valueRelativeOffset;
                long numerator = ReadTiffUInt32(data, valueOffset, littleEndian);
                long denominator = ReadTiffUInt32(data, valueOffset + 4, littleEndian);
                if (numerator == 0 || denominator == 0)
                {
                    throw new InvalidDataException("Exif resolution values must be positive.");
                }

                double resolution = (double)numerator / denominator;
                if (tag == 282)
                {
                    horizontalResolution = resolution;
                }
                else
                {
                    verticalResolution = resolution;
                }
            }
            else if (tag == 296)
            {
                if (type != 3 || count != 1)
                {
                    throw new InvalidDataException("Exif resolution units have an invalid type.");
                }

                resolutionUnit = ReadTiffUInt16(data, entryOffset + 8, littleEndian);
            }
        }

        if (horizontalResolution > 0 && verticalResolution > 0 && (resolutionUnit == 2 || resolutionUnit == 3))
        {
            state.HasExifDpi = true;
            if (resolutionUnit == 2)
            {
                state.ExifHorizontalDpi = horizontalResolution;
                state.ExifVerticalDpi = verticalResolution;
            }
            else
            {
                state.ExifHorizontalDpi = horizontalResolution * 2.54;
                state.ExifVerticalDpi = verticalResolution * 2.54;
            }
        }
    }

    internal static void ParseAdobe(byte[] data, int offset, int length, PdfJpegParseState state)
    {
        if (!MatchesAscii(data, offset, length, "Adobe"))
        {
            return;
        }

        if (length < 12)
        {
            throw new InvalidDataException("Adobe JPEG metadata is truncated.");
        }

        int transform = data[offset + 11];
        if (transform < 0 || transform > 2)
        {
            throw new InvalidDataException("Adobe JPEG color transform is invalid.");
        }

        state.HasAdobeMarker = true;
        state.AdobeTransform = transform;
    }

    internal static bool MatchesAscii(byte[] data, int offset, int length, string value)
    {
        if (length < value.Length)
        {
            return false;
        }

        for (int index = 0; index < value.Length; index++)
        {
            if (data[offset + index] != value[index])
            {
                return false;
            }
        }

        return true;
    }

    internal static int ReadTiffUInt16(byte[] data, int offset, bool littleEndian)
    {
        if (littleEndian)
        {
            return data[offset] + data[offset + 1] * 256;
        }

        return data[offset] * 256 + data[offset + 1];
    }

    internal static long ReadTiffUInt32(byte[] data, int offset, bool littleEndian)
    {
        if (littleEndian)
        {
            return data[offset] + (long)data[offset + 1] * 256 + (long)data[offset + 2] * 65536 + (long)data[offset + 3] * 16777216;
        }

        return (long)data[offset] * 16777216 + (long)data[offset + 1] * 65536 + (long)data[offset + 2] * 256 + data[offset + 3];
    }

    internal static bool HasTiffRange(int tiffLength, long relativeOffset, long length)
    {
        if (relativeOffset < 0 || length < 0 || relativeOffset > tiffLength)
        {
            return false;
        }

        return length <= tiffLength - relativeOffset;
    }

    internal static PdfJpegColorSpace GetColorSpace(int componentCount)
    {
        if (componentCount == 1)
        {
            return PdfJpegColorSpace.Grayscale;
        }

        if (componentCount == 3)
        {
            return PdfJpegColorSpace.Rgb;
        }

        return PdfJpegColorSpace.Cmyk;
    }

    internal static void ValidateAdobeTransform(PdfJpegParseState state)
    {
        if (!state.HasAdobeMarker)
        {
            return;
        }

        if (state.ComponentCount == 1 && state.AdobeTransform != 0)
        {
            throw new InvalidDataException("Adobe grayscale JPEG data must use color transform zero.");
        }

        if (state.ComponentCount == 3 && state.AdobeTransform != 0 && state.AdobeTransform != 1)
        {
            throw new InvalidDataException("Adobe RGB JPEG data has an incompatible color transform.");
        }

        if (state.ComponentCount == 4 && state.AdobeTransform != 0 && state.AdobeTransform != 2)
        {
            throw new InvalidDataException("Adobe CMYK JPEG data has an incompatible color transform.");
        }
    }
}
