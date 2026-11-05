namespace icc;

internal static class PngImageInspector
{
    internal const uint Ihdr = 0x49484452;
    internal const uint Plte = 0x504c5445;
    internal const uint Idat = 0x49444154;
    internal const uint Iend = 0x49454e44;

    internal static EmbeddedImageInspection InspectPng(System.ReadOnlyMemory<byte> bytes, bool heightMap, int maximumExpandedBytes)
    {
        EmbeddedImageInspection result = new EmbeddedImageInspection();
        if (!HasPngSignature(bytes))
        {
            result.Error = "Embedded PNG image data does not begin with the PNG signature.";
            return result;
        }

        int width = 0;
        int height = 0;
        int components = 0;
        int bitDepth = 0;
        int interlace = 0;
        int idatBytes = 0;
        bool sawHeader = false;
        bool sawData = false;
        bool dataEnded = false;
        bool sawEnd = false;
        int offset = 8;
        while (offset < bytes.Length)
        {
            if (offset > bytes.Length - 12)
            {
                result.Error = "Embedded PNG image data contains a truncated chunk header.";
                return result;
            }

            uint encodedLength = ReadPngUInt32(bytes, offset);
            if (encodedLength > int.MaxValue)
            {
                result.Error = "An embedded PNG chunk exceeds the supported managed size.";
                return result;
            }

            int length = (int)encodedLength;
            if (length > bytes.Length - offset - 12)
            {
                result.Error = "Embedded PNG image data contains a chunk that extends beyond the image bytes.";
                return result;
            }

            uint chunkType = ReadPngUInt32(bytes, offset + 4);
            uint expectedCrc = ReadPngUInt32(bytes, offset + 8 + length);
            uint actualCrc = ComputePngCrc(bytes.Slice(offset + 4, length + 4));
            if (expectedCrc != actualCrc)
            {
                result.Error = "An embedded PNG chunk has an invalid CRC-32 checksum.";
                return result;
            }

            if (!sawHeader)
            {
                if (chunkType != Ihdr || length != 13)
                {
                    result.Error = "The first embedded PNG chunk shall be a 13-byte IHDR chunk.";
                    return result;
                }

                uint encodedWidth = ReadPngUInt32(bytes, offset + 8);
                uint encodedHeight = ReadPngUInt32(bytes, offset + 12);
                if (encodedWidth == 0 || encodedHeight == 0 || encodedWidth > int.MaxValue || encodedHeight > int.MaxValue)
                {
                    result.Error = "Embedded PNG dimensions shall be positive and fit the managed image model.";
                    return result;
                }

                width = (int)encodedWidth;
                height = (int)encodedHeight;
                bitDepth = bytes.Span[offset + 16];
                int colorType = bytes.Span[offset + 17];
                if (bytes.Span[offset + 18] != 0 || bytes.Span[offset + 19] != 0
                    || (bytes.Span[offset + 20] != 0 && bytes.Span[offset + 20] != 1))
                {
                    result.Error = "Embedded PNG compression, filtering, or interlace metadata is invalid.";
                    return result;
                }

                interlace = bytes.Span[offset + 20];
                if (heightMap)
                {
                    components = 1;
                    if (colorType != 0 || !IsValidGrayBitDepth(bitDepth))
                    {
                        result.Error = "An embedded height PNG shall contain grayscale pixels with a valid grayscale bit depth.";
                        return result;
                    }
                }
                else
                {
                    components = 3;
                    if (colorType != 2 || (bitDepth != 8 && bitDepth != 16))
                    {
                        result.Error = "An embedded normal PNG shall contain RGB pixels with an 8-bit or 16-bit component depth.";
                        return result;
                    }
                }

                sawHeader = true;
            }
            else if (chunkType == Ihdr)
            {
                result.Error = "An embedded PNG shall contain exactly one IHDR chunk.";
                return result;
            }
            else if (chunkType == Idat)
            {
                if (dataEnded)
                {
                    result.Error = "Embedded PNG IDAT chunks shall be consecutive.";
                    return result;
                }

                if (length > int.MaxValue - idatBytes)
                {
                    result.Error = "The combined embedded PNG compressed stream exceeds the supported managed size.";
                    return result;
                }

                idatBytes = idatBytes + length;
                sawData = true;
            }
            else
            {
                if (sawData)
                {
                    dataEnded = true;
                }

                if (chunkType == Iend)
                {
                    if (length != 0 || !sawData)
                    {
                        result.Error = "An embedded PNG IEND chunk shall be empty and follow image data.";
                        return result;
                    }

                    sawEnd = true;
                }
                else if (IsUnknownCriticalChunk(bytes.Span[offset + 4], chunkType))
                {
                    result.Error = "Embedded PNG image data contains an unsupported critical chunk.";
                    return result;
                }
            }

            offset = offset + 12 + length;
            if (sawEnd && offset != bytes.Length)
            {
                result.Error = "Embedded PNG image data contains bytes after the IEND chunk.";
                return result;
            }
        }

        if (!sawHeader || !sawData || !sawEnd || idatBytes == 0)
        {
            result.Error = "Embedded PNG image data is missing required IHDR, IDAT, or IEND content.";
            return result;
        }

        long expandedSize = ComputePngExpandedSize(width, height, components * bitDepth, interlace);
        if (expandedSize > maximumExpandedBytes)
        {
            result.LimitExceeded = true;
            result.Error = $"The embedded PNG expands to {expandedSize} scanline bytes, exceeding the configured maximum of {maximumExpandedBytes} bytes.";
            return result;
        }

        byte[] compressed = CollectPngData(bytes, idatBytes);
        ZlibDecodeResult decompressed = ZlibCodec.Decode(compressed, maximumExpandedBytes);
        if (!decompressed.Success)
        {
            result.LimitExceeded = decompressed.LimitExceeded;
            result.Error = $"The embedded PNG compressed stream is invalid: {decompressed.Error}";
            return result;
        }

        if (decompressed.Bytes.Length != expandedSize)
        {
            result.Error = $"The embedded PNG expands to {decompressed.Bytes.Length} bytes, but its dimensions require {expandedSize} scanline bytes.";
            return result;
        }

        string filterError = ValidatePngFilters(decompressed.Bytes, width, height, components * bitDepth, interlace);
        if (filterError.Length != 0)
        {
            result.Error = filterError;
            return result;
        }

        result.Success = true;
        result.Width = width;
        result.Height = height;
        result.Components = components;
        result.IsCompressed = true;
        return result;
    }

    internal static bool HasPngSignature(System.ReadOnlyMemory<byte> bytes)
    {
        return bytes.Length >= 8
            && bytes.Span[0] == 137 && bytes.Span[1] == 80 && bytes.Span[2] == 78 && bytes.Span[3] == 71
            && bytes.Span[4] == 13 && bytes.Span[5] == 10 && bytes.Span[6] == 26 && bytes.Span[7] == 10;
    }

    internal static bool IsValidGrayBitDepth(int bitDepth)
    {
        return bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8 || bitDepth == 16;
    }

    internal static bool IsUnknownCriticalChunk(byte firstTypeByte, uint chunkType)
    {
        bool critical = (firstTypeByte & 32) == 0;
        return critical && chunkType != Plte && chunkType != Idat && chunkType != Iend;
    }

    internal static long ComputePngExpandedSize(int width, int height, int bitsPerPixel, int interlace)
    {
        if (interlace == 0)
        {
            long rowBytes = (((long)width * bitsPerPixel) + 7L) / 8L;
            return (rowBytes + 1L) * height;
        }

        long size = 0;
        int pass = 0;
        while (pass < 7)
        {
            int passWidth = ComputePassLength(width, GetPassStartX(pass), GetPassStepX(pass));
            int passHeight = ComputePassLength(height, GetPassStartY(pass), GetPassStepY(pass));
            if (passWidth != 0 && passHeight != 0)
            {
                long rowBytes = (((long)passWidth * bitsPerPixel) + 7L) / 8L;
                size = size + ((rowBytes + 1L) * passHeight);
            }

            pass = pass + 1;
        }

        return size;
    }

    internal static string ValidatePngFilters(byte[] expanded, int width, int height, int bitsPerPixel, int interlace)
    {
        int position = 0;
        int passCount = interlace == 0 ? 1 : 7;
        int pass = 0;
        while (pass < passCount)
        {
            int passWidth = interlace == 0 ? width : ComputePassLength(width, GetPassStartX(pass), GetPassStepX(pass));
            int passHeight = interlace == 0 ? height : ComputePassLength(height, GetPassStartY(pass), GetPassStepY(pass));
            int rowBytes = (int)((((long)passWidth * bitsPerPixel) + 7L) / 8L);
            int row = 0;
            while (row < passHeight && passWidth != 0)
            {
                if (expanded[position] > 4)
                {
                    return "An embedded PNG scanline uses an invalid filter method.";
                }

                position = position + rowBytes + 1;
                row = row + 1;
            }

            pass = pass + 1;
        }

        return position == expanded.Length ? string.Empty : "The embedded PNG scanline layout is inconsistent with its dimensions.";
    }

    internal static int ComputePassLength(int total, int start, int step)
    {
        if (total <= start)
        {
            return 0;
        }

        return (total - start + step - 1) / step;
    }

    internal static int GetPassStartX(int pass)
    {
        switch (pass)
        {
            case 0:
            case 2:
            case 4:
            case 6:
                return 0;
            case 1:
                return 4;
            case 3:
                return 2;
            default:
                return 1;
        }
    }

    internal static int GetPassStartY(int pass)
    {
        switch (pass)
        {
            case 0:
            case 1:
            case 3:
                return 0;
            case 2:
                return 4;
            case 4:
                return 2;
            default:
                return 1;
        }
    }

    internal static int GetPassStepX(int pass)
    {
        switch (pass)
        {
            case 0:
            case 1:
                return 8;
            case 2:
            case 3:
                return 4;
            case 4:
            case 5:
                return 2;
            default:
                return 1;
        }
    }

    internal static int GetPassStepY(int pass)
    {
        switch (pass)
        {
            case 0:
            case 1:
            case 2:
                return 8;
            case 3:
            case 4:
                return 4;
            default:
                return 2;
        }
    }

    internal static byte[] CollectPngData(System.ReadOnlyMemory<byte> bytes, int count)
    {
        byte[] data = new byte[count];
        int destination = 0;
        int offset = 8;
        while (offset < bytes.Length)
        {
            int length = (int)ReadPngUInt32(bytes, offset);
            uint chunkType = ReadPngUInt32(bytes, offset + 4);
            if (chunkType == Idat)
            {
                bytes.Span.Slice(offset + 8, length).CopyTo(new System.Span<byte>(data, destination, length));
                destination = destination + length;
            }

            offset = offset + length + 12;
        }

        return data;
    }

    internal static uint ReadPngUInt32(System.ReadOnlyMemory<byte> bytes, int offset)
    {
        return ((uint)bytes.Span[offset] << 24) | ((uint)bytes.Span[offset + 1] << 16)
            | ((uint)bytes.Span[offset + 2] << 8) | bytes.Span[offset + 3];
    }

    internal static uint ComputePngCrc(System.ReadOnlyMemory<byte> bytes)
    {
        uint crc = uint.MaxValue;
        int index = 0;
        while (index < bytes.Length)
        {
            crc = crc ^ bytes.Span[index];
            int bit = 0;
            while (bit < 8)
            {
                crc = (crc & 1) == 0 ? crc >> 1 : (crc >> 1) ^ 0xedb88320u;
                bit = bit + 1;
            }

            index = index + 1;
        }

        return crc ^ uint.MaxValue;
    }
}
